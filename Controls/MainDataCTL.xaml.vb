

Imports System.Globalization
Imports System.IO
Imports System.Windows.Threading

Namespace Kas

	Partial Public Class MainDataCTL

        Sub New()

			' Этот вызов является обязательным для конструктора.
			InitializeComponent()


        End Sub


        ''' <summary>
        ''' Безопасное обновление текста статуса в заголовке Expander
        ''' </summary>
        Private Sub SetStatus(message As String)
            Dim action As Action = Sub()
                                       If String.IsNullOrWhiteSpace(message) Then
                                           ' Если сообщения нет, скрываем и текст, и разделитель
                                           LblProcessStatus.Text = ""
                                           TxtSeparator.Visibility = Visibility.Collapsed
                                       Else
                                           ' Если сообщение есть, показываем разделитель и текст
                                           TxtSeparator.Visibility = Visibility.Visible
                                           LblProcessStatus.Text = message
                                           ' ПРИНУДИТЕЛЬНО убираем жирность из кода
                                           ' 1. Сначала очищаем любое локальное значение
                                           LblProcessStatus.ClearValue(TextBlock.FontWeightProperty)

                                           ' 2. Затем явно задаем Normal
                                           LblProcessStatus.FontWeight = FontWeights.Normal
                                       End If
                                   End Sub

            If LblProcessStatus.Dispatcher.CheckAccess() Then
                action()
            Else
                LblProcessStatus.Dispatcher.Invoke(action)
            End If
        End Sub



        Private Async Sub BtnAddFromJRNL_Click(sender As Object, e As RoutedEventArgs)

            ' Сохраняем исходное состояние кнопки
            Dim originalButtonText As String = BtnAddFromJRNL.Content.ToString()

            ' Блокируем кнопку и показываем статус
            BtnAddFromJRNL.Content = "⏳ Загрузка..."
            BtnAddFromJRNL.IsEnabled = False
            SetStatus("Подключение...")

            ' Проверка подключения к серверу
            If Not Await Fetcher.EnsureConnectedAsync() Then
                BtnAddFromJRNL.Content = originalButtonText
                SetStatus("")
                BtnAddFromJRNL.IsEnabled = True
                Return
            End If

            Try
                ' --- ШАГ 1: Формирование ссылки (Синхронно, быстро) ---
                SetStatus("Загрузка 4 отчета...")
                Dim journalUrl = BuildJournalUrl(Fetcher.NachDat, Fetcher.KonDat, Fetcher.NachTim, endHour:=23, 88, endMin:=59)

                ' Небольшая пауза, чтобы UI успел показать текст статуса перед тяжелой загрузкой
                Await Task.Delay(50)

                ' --- ШАГ 2: Загрузка журнала (Асинхронно, долго) ---
                SetStatus("Загрузка токенов ген. отч...")
                Dim webRecords As List(Of JournalRecord) = Await Fetcher.FetchAllJournalPagesAsync(journalUrl, saveExcel:=True)

                ' --- ШАГ 3: Загрузка Генерального отчета (Асинхронно, долго) ---
                SetStatus("Загрузка экселя ген. отч....")
                Dim Rows As List(Of GenReportRow) = Await GetGenOtchRows(True)

                ' --- ШАГ 4: Обработка данных журнала (Синхронно, может быть тяжело) ---
                SetStatus("Обработка отказов из 4 отчета...")
                ProcessWebJournalData(webRecords)

                ' --- ШАГ 5: Синхронизация с генеральным отчетом (Синхронно) ---
                SetStatus("Обработка отказов из ген. отч...")
                SyncGenOTSReport(Rows)

                ' Добавляем в основной список
                AddOTSToContainer(OTSList)

                ' --- ШАГ 6: Обновление фильтров и итогов (Синхронно) ---
                SetStatus("Обновление списка отказов...")
                With MW.TRowsContainer
                    .ClearAll0LevelFilters()
                    .THed.ClearAllFilters()
                    .UpdateTotal()
                End With

                SetStatus("Готово!")

            Catch ex As Exception
                ShowMSG(MW, $"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error)
                SetStatus("Ошибка")
            Finally
                ' Возвращаем кнопку в исходное состояние
                BtnAddFromJRNL.Content = originalButtonText
                BtnAddFromJRNL.IsEnabled = True

                ' !!! КЛЮЧЕВОЕ ИЗМЕНЕНИЕ: Принудительная очистка памяти после завершения всех операций
                ' 1. Очищаем KasantFetcher (тот, что грузил журнал и HTML)
                Fetcher.ForceCleanup()

                ' 2. Очищаем CentralFetcher (тот, что грузил тяжелые Excel отчеты)
                ' Убедитесь, что метод ForceCleanup добавлен и в класс Report341Fetcher (CentralFetcher)
                CentralFetcher.ForceCleanup()

                ' Создаем таймер, который сработает один раз через 2 секунды
                Dim timer As New DispatcherTimer()
                timer.Interval = TimeSpan.FromSeconds(2)

                ' Подписываемся на событие тика
                ' Используем s и args вместо sender и e, чтобы избежать конфликта имен
                AddHandler timer.Tick, Sub(s As Object, args As EventArgs)
                                           timer.Stop()
                                           RemoveHandler timer.Tick, Nothing
                                           SetStatus("")
                                       End Sub

                timer.Start()
            End Try

        End Sub



        Private Sub SaveDataToJSON_Click(sender As Object, e As RoutedEventArgs)




            ' 1. Получаем текущий источник данных из UI
            Dim currentSource = TryCast(MW.TRowsContainer.OTSContainer.ItemsSource, IEnumerable(Of Otkaz))
            If currentSource Is Nothing OrElse currentSource.Count < 1 Then
                ShowMSG(MW, "Нет данных для сохранения.", "Ошибка")
                Return
            End If

            Dim otkazList As List(Of Otkaz) = currentSource.ToList()

            ' 3. Получаем папку данных
            Dim dataPath As String = GetOrCreateDataFolderPath()
            'папка в параметрах есть или создана/указана пользователем, но на всякий случай
            If String.IsNullOrEmpty(dataPath) Then Return

            ' 2. Формируем имя по периоду ИЗ НАСТРОЕК (а не по данным!)
            Dim startDate As Date = My.Settings.NachPeriod
            Dim endDate As Date = My.Settings.KonPeriod

            Dim startStr As String = ""
            Dim endStr As String = ""
            Dim fileName As String

            If startDate = Date.MinValue OrElse endDate = Date.MinValue Then
                ' Период не задан → используем даты из данных 
                Dim validDates = otkazList.Where(Function(o) o.Nach <> Date.MinValue).Select(Function(o) o.Nach).ToList()

                startStr = validDates.Min().ToString("dd.MM.yy") ', CultureInfo.InvariantCulture)
                endStr = validDates.Max().ToString("dd.MM.yy") ', CultureInfo.InvariantCulture)
                fileName = $"ots_{startStr}-{endStr}.json"
            Else
                ' Период задан → используем его
                startStr = startDate.ToString("dd.MM.yy", CultureInfo.InvariantCulture)
                endStr = endDate.ToString("dd.MM.yy", CultureInfo.InvariantCulture)
                fileName = $"ots_{startStr}-{endStr}.json"
            End If

            Dim jsonPath As String = Path.Combine(dataPath, fileName)

            ' 4. Проверка перезаписи
            If File.Exists(jsonPath) Then
                Dim message = $"Файл уже существует:{vbCrLf}{fileName}{vbCrLf}{vbCrLf}Перезаписать?"
                Dim result = ModernMessageBox.MsgShow(MW, "Подтверждение", message, showCancel:=True)
                If result = False OrElse result Is Nothing Then
                    'если нет - выходим
                    Return
                End If
            End If

            ' 5. Сохраняем — именно текущий фильтр!
            Try
                StorageModule.SaveToJson(otkazList, jsonPath)
                'подготовка текста для сообщения
                Dim periodInfo As String
                If startDate = Date.MinValue OrElse endDate = Date.MinValue Then
                    periodInfo = "не задан"
                Else
                    periodInfo = $"{startStr} — {endStr}"
                End If

                MW.InfoBLOK.AddItem($"✅ Сохранено {otkazList.Count} записей{vbCrLf}Период: {periodInfo}{vbCrLf}Файл: {fileName}")
                MW.InfoBLOK.ScrollToEnd()
                otkazList = Nothing
                currentSource = Nothing
            Catch ex As Exception
                ShowMSG(MW, $"❌ Ошибка: {ex.Message}", "Ошибка")
            End Try
        End Sub

        Private Sub BtnShowInExplorer_Click(sender As Object, e As RoutedEventArgs)

            Dim folderPath = GetOrCreateDataFolderPath()
            If Not String.IsNullOrEmpty(folderPath) Then
                Process.Start(New ProcessStartInfo() With {.FileName = folderPath, .UseShellExecute = True})
            End If
        End Sub

        Private Sub AppendDataToJSON_Click(sender As Object, e As RoutedEventArgs)
            ' 1. Показать диалог выбора JSON-файла
            Dim openFileDialog As New Microsoft.Win32.OpenFileDialog()
            openFileDialog.Filter = "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*"
            openFileDialog.Title = "Выберите JSON-файл для добавления данных"
            openFileDialog.InitialDirectory = GetDataFolderPath()

            If openFileDialog.ShowDialog() <> True Then
                ' Пользователь отменил выбор
                Return
            End If

            Dim jsonPath As String = openFileDialog.FileName

            ' 2. Устанавливаем начальную папку — из настроек (если есть)
            Dim defaultPath As String = StorageModule.GetDataFolderPath()
            If Directory.Exists(defaultPath) Then
                openFileDialog.InitialDirectory = defaultPath
            Else
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            End If

            ' 3. Проверить, существует ли файл
            If Not File.Exists(jsonPath) Then
                ShowMSG(MW, "❌ Выбранный файл не существует.", "Ошибка")
                Return
            End If

            ' 4. Загрузить существующие данные из JSON
            Dim existingOtkazList As List(Of Otkaz) = Nothing
            Try
                ' Предполагается, что StorageModule.LoadFromJson возвращает IEnumerable(Of Otkaz)
                ' и может десериализовать список Otkaz из JSON.
                Dim loadedData = StorageModule.LoadFromJson(jsonPath) ' <-- Замените на ваш метод
                If loadedData IsNot Nothing Then
                    existingOtkazList = loadedData.ToList()
                Else
                    ' Если файл пустой или содержит null, создаем новый список
                    existingOtkazList = New List(Of Otkaz)()
                    MW.InfoBLOK.AddItem($"⚠️ Файл {Path.GetFileName(jsonPath)} был пуст или содержал null. Создан новый список для добавления.")
                End If
            Catch ex As Exception
                ShowMSG(MW, $"❌ Ошибка при загрузке данных из файла:{vbCrLf}{ex.Message}", "Ошибка")
                Return
            End Try

            ' 5. Получаем текущий источник данных из UI (например, отфильтрованные данные)
            Dim currentSource = TryCast(MW.TRowsContainer.ItemsSource, IEnumerable(Of Otkaz))
            If currentSource Is Nothing Then
                ShowMSG(MW, "Нет данных для добавления.", "Ошибка")
                Return
            End If

            Dim newOtkazList As List(Of Otkaz) = currentSource.ToList()
            If newOtkazList.Count = 0 Then
                ShowMSG(MW, "Текущий источник данных пуст.", "Внимание")
                Return
            End If

            ' 6. Проверка перезаписи (опционально)
            ' Пользователь может захотеть подтвердить, что хочет изменить существующий файл.
            Dim confirmMessage = $"Вы уверены, что хотите добавить {newOtkazList.Count} записей к файлу:{vbCrLf}{Path.GetFileName(jsonPath)}?"
            Dim confirmResult = ModernMessageBox.MsgShow(MW, "Подтвердить добавление", confirmMessage, showCancel:=True)
            If confirmResult = False OrElse confirmResult Is Nothing Then
                Return
            End If

            ' 7. Объединить списки: существующие + новые
            ' Concat объединяет последовательности. ToList() создает новый список.
            Dim combinedList As List(Of Otkaz) = existingOtkazList.Concat(newOtkazList).ToList()

            ' 8. Сохранить объединённый список обратно в файл
            Try

                ' ← Объявляем ДО If
                Dim startStr As String = ""
                Dim endStr As String = ""
                Dim fileName As String


                Dim validDates = combinedList.Where(Function(o) o.Nach <> Date.MinValue).Select(Function(o) o.Nach).ToList()
                startStr = validDates.Min().ToString("dd.MM.yy") ', CultureInfo.InvariantCulture)
                endStr = validDates.Max().ToString("dd.MM.yy") ', CultureInfo.InvariantCulture)
                fileName = $"ots_{startStr}-{endStr}.json"

                Dim EndPat As String = Path.Combine(Path.GetDirectoryName(jsonPath), fileName)

                StorageModule.SaveToJson(combinedList, EndPat)

                ' Подготовка текста для сообщения
                MW.InfoBLOK.AddItem($"✅ Добавлено {newOtkazList.Count} записей к файлу: {Path.GetFileName(EndPat)}")
                MW.InfoBLOK.AddItem($"   Итого записей в файле: {combinedList.Count}")
                MW.InfoBLOK.ScrollToEnd()


            Catch ex As Exception
                ShowMSG(MW, $"❌ Ошибка при сохранении данных:{vbCrLf}{ex.Message}", "Ошибка")
            Finally
                existingOtkazList = Nothing
                combinedList = Nothing
                currentSource = Nothing
                newOtkazList = Nothing
                openFileDialog = Nothing
            End Try
        End Sub

        Private Sub LoadDataFromJSON_Click(sender As Object, e As RoutedEventArgs)

            If GetJSONList() Then
                ResetPeriodSUB()
            End If

        End Sub


        ''' <summary>
        ''' Загружает данные из JSON файла. Возвращает True при успехе, False при ошибке или отмене.
        ''' Данные автоматически присваиваются в OTSList.
        ''' </summary>
        Private Function GetJSONList(Optional Self As Boolean = False) As Boolean
            Dim selectedFilePath As String = ""

            ' Если вызывается кнопкой "Последний файл" (Self=True), берем путь из настроек
            If Self Then
                selectedFilePath = My.Settings.LastUsedFile

                ' Проверка существования файла для режима Self
                If String.IsNullOrEmpty(selectedFilePath) OrElse Not System.IO.File.Exists(selectedFilePath) Then
                    MW.InfoBLOK.AddItem("Файл не найден по сохраненному пути.")
                    Return False
                End If
            Else
                ' Обычный режим выбора файла через диалог
                Dim openFileDialog As New Microsoft.Win32.OpenFileDialog With {
            .Filter = "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*",
            .Title = "Выберите файл с данными отказов",
            .Multiselect = False
        }

                ' Устанавливаем начальную папку
                Dim defaultPath As String = StorageModule.GetDataFolderPath()
                If Directory.Exists(defaultPath) Then
                    openFileDialog.InitialDirectory = defaultPath
                Else
                    openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                End If

                ' Показываем диалог
                If openFileDialog.ShowDialog() = True Then
                    selectedFilePath = openFileDialog.FileName
                Else
                    ' Пользователь нажал Отмена
                    MW.InfoBLOK.AddItem("Выбор файла JSON отменен.")
                    Return False
                End If
            End If

            Try
                ' Загружаем данные напрямую в глобальный список
                Dim loadedList As List(Of Otkaz) = StorageModule.LoadFromJson(selectedFilePath)

                If loadedList Is Nothing OrElse loadedList.Count = 0 Then
                    ShowMSG(MW, "Файл пуст или содержит некорректные данные.", "Внимание")
                    Return False
                End If

                ' Присваиваем результат
                OTSList = loadedList

                ' Обновляем настройки и тултип
                UpdateCurrentFileAndTooltip(selectedFilePath)

                Return True

            Catch ex As Exception
                ShowMSG(MW, $"❌ Ошибка при загрузке JSON:{Environment.NewLine}{ex.Message}", "Ошибка")
                Return False
            End Try
        End Function


        Private Sub LoadData_Click(sender As Object, e As RoutedEventArgs)
            If ImportOTS() Then
                ResetPeriodSUB()
            End If
        End Sub


        Function ImportOTS(Optional Self As Boolean = False) As Boolean


            Dim selectedFilePath As String = ""

            ' 1. Определяем путь к файлу
            If Self Then
                ' Режим загрузки последнего файла: берем путь из настроек
                selectedFilePath = My.Settings.LastUsedFile

                ' Проверка: если путь пустой или файл удален с диска — выходим с ошибкой
                If String.IsNullOrEmpty(selectedFilePath) OrElse Not System.IO.File.Exists(selectedFilePath) Then
                    MW.InfoBLOK.AddItem("Нет сохраненного пути к последнему Excel-файлу или он был перемещен.")
                    Return False
                End If
            Else
                ' Обычный режим: диалог выбора файла
                Dim Desk As String = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                Dim openFileDialog As New Microsoft.Win32.OpenFileDialog()

                openFileDialog.Filter = "Excel Files (*.xlsm;*.xlsx)|*.xlsm;*.xlsx|All Files (*.*)|*.*"
                openFileDialog.Title = "Выберите файл Excel для импорта"
                openFileDialog.InitialDirectory = Desk

                If openFileDialog.ShowDialog() = True Then
                    selectedFilePath = openFileDialog.FileName
                Else
                    MW.InfoBLOK.AddItem("Файл Excel не выбран. Импорт отменен.")
                    Return False
                End If
            End If

            ' 2. Загружаем данные
            Try
                OTSList = LoadOTSFromExcel(selectedFilePath)

                ' Если загрузка вернула Nothing или пустой список (зависит от реализации LoadOTSFromExcel), 
                ' можно добавить проверку, но обычно она внутри. 

                ' 3. Обновляем настройки и тултип ТОЛЬКО при успешной загрузке
                UpdateCurrentFileAndTooltip(selectedFilePath)

                Return True
            Catch ex As Exception
                ShowMSG(MW, $"Ошибка при чтении Excel:{Environment.NewLine}{ex.Message}", "Ошибка импорта")
                Return False
            End Try

        End Function

        ' Поле для хранения полного пути последнего загруженного файла
        Private _currentFilePath As String = ""

        ' Свойство для отображения в ToolTip (Имя + Расширение)
        Public ReadOnly Property CurrentFileNameForTooltip() As String
            Get
                If String.IsNullOrEmpty(_currentFilePath) Then
                    Return "Файл не выбран"
                Else
                    ' Извлекаем только имя файла с расширением из полного пути
                    Return System.IO.Path.GetFileName(_currentFilePath)
                End If
            End Get
        End Property

        Private Sub ResetToOTSList_Click(sender As Object, e As RoutedEventArgs)

            ' Просто определяем тип по сохраненному пути и вызываем соответствующую функцию
            Dim lastFilePath As String = My.Settings.LastUsedFile

            If String.IsNullOrEmpty(lastFilePath) Then
                MW.InfoBLOK.AddItem("Нет сохраненного файла.")
                Return
            End If

            Dim extension As String = System.IO.Path.GetExtension(lastFilePath).ToLowerInvariant()

            Select Case extension
                Case ".xlsm", ".xlsx"
                    ' Вызываем Excel-импорт в режиме Self (без диалога)
                    If ImportOTS(Self:=True) Then
                        ResetPeriodSUB()
                    End If

                Case ".json"
                    ' Вызываем JSON-импорт в режиме Self (без диалога)
                    If GetJSONList(Self:=True) Then
                        ResetPeriodSUB()
                    End If

                Case Else
                    MW.InfoBLOK.AddItem($"Неподдерживаемый формат: {extension}")
            End Select

        End Sub

        ''' <summary>
        ''' Устанавливает путь к текущему файлу и обновляет подсказку кнопки.
        ''' </summary>
        ''' <param name="path">Полный путь к файлу.</param>
        Private Sub UpdateCurrentFileAndTooltip(path As String)
            _currentFilePath = path

            ' Сохраняем в настройки
            My.Settings.LastUsedFile = path
            My.Settings.Save() ' Не забываем сохранить, иначе потеряется при закрытии

            ' Обновляем ToolTip кнопки напрямую
            Dim fileNameOnly As String = ""

            If Not String.IsNullOrEmpty(path) AndAlso System.IO.File.Exists(path) Then
                fileNameOnly = System.IO.Path.GetFileName(path)
                ResetToOTSList.ToolTip = $"{fileNameOnly}"

                ' Опционально: включаем кнопку, если она была заблокирована
                ResetToOTSList.IsEnabled = True
            Else
                ResetToOTSList.ToolTip = ""
                ResetToOTSList.IsEnabled = False ' Блокируем кнопку, если файла нет
            End If
        End Sub






        ''' <summary>
        ''' Загружает данные из Excel по указанному полному пути без открытия диалога.
        ''' </summary>
        ''' <param name="filePath">Полный путь к файлу .xlsm или .xlsx.</param>
        Private Sub LoadSpecificExcelFile(filePath As String)
            Try
                ' Используем вашу существующую логику парсинга
                ' Предполагается, что OTSList - это поле класса или свойство доступное здесь
                Dim newList As List(Of Otkaz) = LoadOTSFromExcel(filePath)

                If newList IsNot Nothing AndAlso newList.Count > 0 Then
                    OTSList = newList

                    ' Обновляем статус интерфейса (как в оригинальном коде)
                    ResetPeriodSUB()

                    MW.InfoBLOK.AddItem($"Успешно загружен файл Excel: {System.IO.Path.GetFileName(filePath)}")
                Else
                    MW.InfoBLOK.AddItem("Файл Excel пуст или содержит некорректные данные.")
                End If

            Catch ex As Exception
                ShowMSG(MW, $"Ошибка чтения Excel:{Environment.NewLine}{ex.Message}", "Ошибка импорта")
            End Try
        End Sub


        ''' <summary>
        ''' Загружает данные из JSON по указанному полному пути без открытия диалога.
        ''' </summary>
        ''' <param name="filePath">Полный путь к файлу .json.</param>
        Private Sub LoadSpecificJsonFile(filePath As String)
            Try
                ' Используем ваш StorageModule напрямую
                Dim loadedList As List(Of Otkaz) = StorageModule.LoadFromJson(filePath)

                If loadedList IsNot Nothing AndAlso loadedList.Count > 0 Then
                    OTSList = loadedList

                    ' Сброс периодов после успешной загрузки
                    ResetPeriodSUB()

                    MW.InfoBLOK.AddItem($"Успешно загружен файл JSON: {System.IO.Path.GetFileName(filePath)}")
                Else
                    MW.InfoBLOK.AddItem("Файл JSON пуст или содержит некорректные данные.")
                End If

            Catch ex As Exception
                ShowMSG(MW, $"Ошибка чтения JSON:{Environment.NewLine}{ex.Message}", "Ошибка импорта")
            End Try
        End Sub






















        Private Sub FilterWithTimeOTSList_Click(sender As Object, e As RoutedEventArgs)

            Dim ExclusivePeriod As Func(Of Otkaz, Boolean) = (Function(o) o.Nach.IsInRangeWithTime AndAlso Not (o.KtoZakryl.ToLower.Contains("трп")))

            'MW.expPoyasnilka.btnDailyPoyasnShow.Visibility = Visibility.Visible
            OTSList = OTSList.Where(ExclusivePeriod).OrderBy(Function(o) o.Nach).ToList
            ResetPeriodSUB()
            InitFirst()
            'YarCon.UpdateYarlykInfo()
        End Sub


    End Class



End Namespace

