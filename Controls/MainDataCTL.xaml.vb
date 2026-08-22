

Imports System.Globalization
Imports System.IO

Namespace Kas

	Partial Public Class MainDataCTL

        Sub New()

			' Этот вызов является обязательным для конструктора.
			InitializeComponent()


        End Sub

        Private Async Sub BtnAddFromJRNL_Click(sender As Object, e As RoutedEventArgs)

            ' 1. Сохраняем исходный текст кнопки и меняем его
            Dim originalText As String = BtnAddFromJRNL.Content.ToString()
            BtnAddFromJRNL.Content = "⏳ Загрузка..."

            '' 2. Меняем курсор на всем окне на "Ожидание" (опционально, но очень полезно)
            'Me.Cursor = System.Windows.Input.Cursors.Wait
            BtnAddFromJRNL.IsEnabled = False

            If Not Await Fetcher.EnsureConnectedAsync() Then
                ' Возвращаем всё как было при неудачном подключении
                BtnAddFromJRNL.Content = originalText
                BtnAddFromJRNL.IsEnabled = True
                Me.Cursor = System.Windows.Input.Cursors.Arrow
                Return
            End If

            Try
                ' ЗАГРУЗКА 4 ОТЧЕТА

                Dim journalUrl = BuildJournalUrl(Fetcher.NachDat, Fetcher.KonDat, Fetcher.NachTim, Fetcher.KonTim, 88, Fetcher.KonMinut)
                Dim webRecords As List(Of JournalRecord) = Await Fetcher.FetchAllJournalPagesAsync(journalUrl, saveExcel:=True)



                '' ЗАГРУЗКА ГЕН ОТЧЕТА
                Dim Rows As List(Of GenReportRow) = Await GetGenOtchRows(True)


                ' ВЫВОД 4 ОТЧЕТА
                ProcessWebJournalData(webRecords)


                ' ВЫВОД ГЕН ОТЧЕТА
                SyncGenOTSReport(Rows)

                AddOTSToContainer(OTSList)
                ' ШАГ 2: ОБНОВЛЕНИЕ UI
                With MW.TRowsContainer
                    .ClearAll0LevelFilters()
                    .THed.ClearAllFilters()
                    .UpdateTotal()
                End With

            Catch ex As Exception
                ShowMSG(MW, $"Ошибка загрузки журнала: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error)
            Finally
                ' 3. ГАРАНТИРОВАННО возвращаем исходное состояние, даже если была ошибка
                BtnAddFromJRNL.Content = originalText
                BtnAddFromJRNL.IsEnabled = True
                Me.Cursor = System.Windows.Input.Cursors.Arrow ' Или Cursors.None, чтобы сбросить в дефолт
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
            End Try
        End Sub

		Private Sub LoadDataFromJSON_Click(sender As Object, e As RoutedEventArgs)
            Try
                Dim loadedList As List(Of Otkaz)
                loadedList = GetJSONList()
                RezervList = loadedList
                OTSList = loadedList
                ResetPeriodSUB()
                InitFirst()
                'YarCon.UpdateYarlykInfo()

            Catch ex As Exception
                'ShowMSG(MW, $"❌ Ошибка при загрузке:{vbCrLf}{ex.Message}", "Ошибка")
            End Try
        End Sub

        Private Function GetJSONList() As List(Of Otkaz)
            Dim loadedList As List(Of Otkaz)
            ' 1. Создаём диалог выбора файла
            Dim openFileDialog As New Microsoft.Win32.OpenFileDialog With {
                .Filter = "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*",
                .Title = "Выберите файл с данными отказов",
                .Multiselect = False
            }

            ' 2. Устанавливаем начальную папку — из настроек (если есть)
            Dim defaultPath As String = StorageModule.GetDataFolderPath()
            If Directory.Exists(defaultPath) Then
                openFileDialog.InitialDirectory = defaultPath
            Else
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            End If

            ' 3. Открываем диалог
            Dim result As Boolean? = openFileDialog.ShowDialog()
            If result <> True Then Return Nothing ' пользователь нажал "Отмена"

            Dim jsonPath As String = openFileDialog.FileName

            ' 4. Загружаем данные
            Try
                loadedList = StorageModule.LoadFromJson(jsonPath)

                If loadedList Is Nothing OrElse loadedList.Count = 0 Then
                    ShowMSG(MW, "Файл пуст или содержит некорректные данные.", "Внимание")
                    Return Nothing
                End If
            Catch ex As Exception
                ShowMSG(MW, $"❌ Ошибка при загрузке:{vbCrLf}{ex.Message}", "Ошибка")
                Return Nothing
            End Try
            RezervList = loadedList
            Return loadedList
        End Function







        Private Sub LoadData_Click(sender As Object, e As RoutedEventArgs)
            ImportOTS()
            ResetPeriodSUB()
            RezervList = OTSList
        End Sub

        Sub ImportOTS(Optional Self As Boolean = False)
            Dim selectedFilePath As String = ""
            Dim Desk As String = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            ' Создаем диалоговое окно для выбора файла
            Dim openFileDialog As New Microsoft.Win32.OpenFileDialog()

            openFileDialog.Filter = "Excel Files (*.xlsm;*.xlsx)|*.xlsm;*.xlsx|All Files (*.*)|*.*"
            openFileDialog.Title = "Выберите файл Excel для импорта"

            ' Устанавливаем начальную директорию как Рабочий стол пользователя
            openFileDialog.InitialDirectory = Desk

            If Self Then
                selectedFilePath = IO.Path.Combine(Desk, "2025 КАСАНТ год нараст все-все.xlsm")
            Else
                ' Проверяем, что пользователь выбрал файл
                If openFileDialog.ShowDialog() = True Then
                    selectedFilePath = openFileDialog.FileName

                Else
                    ' Если пользователь отменил выбор, выводим сообщение или выполняем другое действие
                    MW.InfoBLOK.AddItem("Файл Excel не выбран. Импорт отменен.")
                    Exit Sub
                End If
            End If

            Stopwatch.Reset()
            Stopwatch.Start()

            ' Загружаем данные из выбранного файла
            OTSList = LoadOTSFromExcel(selectedFilePath)
            RezervList = OTSList
            ' Останавливаем таймер
            Stopwatch.Stop()

            ' Счятаем время выполнения
            Dim elapsedTime As TimeSpan = Stopwatch.Elapsed


            'InfoBLOK.ClearItems()
            MW.InfoBLOK.AddItem($"Импорт EXCEL завершен за {elapsedTime.TotalMilliseconds:F0} мс.")
            MW.InfoBLOK.AddItem("***")
            MW.InfoBLOK.ScrollToEnd()
        End Sub

        Private Sub ResetToOTSList_Click(sender As Object, e As RoutedEventArgs)
            If RezervList IsNot Nothing Then
                ResignOTSList(RezervList)
                MW.JourParam.ParamTimeCTL.But23.IsChecked = True
                'MW.expPoyasnilka.btnDailyPoyasnShow.Visibility = Visibility.Collapsed
            End If
        End Sub

        Private Sub UserControl_Loaded(sender As Object, e As RoutedEventArgs) Handles MyBase.Loaded

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

