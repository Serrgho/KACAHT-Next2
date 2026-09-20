

Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Net.Http
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Windows
Imports System.Windows.Controls.Primitives
Imports System.Windows.Forms
Imports System.Windows.Media
Imports System.Windows.Media.Imaging
Imports System.Windows.Threading
Imports Microsoft.Office.Interop.Word


Namespace Kas

    Module Module1
        Public MW As MainWindow
        Public InformLB As InfoLBoxControl

        Public ReadOnly Ru As New System.Globalization.CultureInfo("ru-RU")


        ' К этой переменной теперь можно обратиться из любой точки программы
        Public ReadOnly Fetcher As New KasantFetcher()
        'Глобальный экземпляр загрузчика для центральной сессии
        Public CentralFetcher As New Report341Fetcher()


        Public OTSList As New List(Of Otkaz)
        'Public RezervList As New List(Of Otkaz)


        ' Инициализируем таймер
        'Public Stopwatch As New Stopwatch()

        Public IstRails As New List(Of String)({"ГИД УРАЛ", "ВСЖД", "ЗАБЖД", "ДВЖД", "ЗСЖД", "РУЧНОЙ ВВОД", "ПРИВЖД", "СКАВЖД", "ЮВЖД", "ЮУРЖД", "КБШЖД", "ГОРЖД", "СВРДЖД", "ОКТЖД", "КЛНГЖД", "СЕВЖД", "САХЖД"})


#Region "Анимация значка в трэе"
        '''' <summary>
        '''' Анимация "бегущей полоски" на иконке в панели задач.
        '''' Базовая иконка берётся из самого окна (та, что указана в XAML).
        '''' </summary>

        'Private _frames As New List(Of ImageSource)
        'Private _timer As DispatcherTimer
        'Private _frameIndex As Integer = 0
        'Private _baseIcon As BitmapSource
        'Private _targetWindow As Window
        'Private _isAnimating As Boolean = False
        'Private _isInitialized As Boolean = False

        '' --- Настройки ---
        'Public Property FrameIntervalMs As Integer = 80        ' ~12 FPS
        'Public Property BarColor As Color = Color.FromRgb(220, 50, 50)
        'Public Property BarWidth As Double = 10
        'Public Property BarHeight As Double = 5
        'Public Property IconSize As Integer = 32
        'Public Property TotalFrames As Integer = 32

        '''' <summary>
        '''' Инициализация. Иконка берётся из свойства Window.Icon (из XAML).
        '''' </summary>
        'Public Sub AnimIconInitialize(targetWindow As Window)
        '    If _isInitialized Then Return
        '    If targetWindow Is Nothing Then Throw New ArgumentNullException(NameOf(targetWindow))

        '    _targetWindow = targetWindow

        '    ' Берём иконку, которая УЖЕ установлена в XAML (Icon="favicon.ico")
        '    _baseIcon = TryCast(_targetWindow.Icon, BitmapSource)

        '    GenerateAllFrames()
        '    _isInitialized = True

        '    Debug.WriteLine("TaskbarAnimator: инициализирован, кадров: " & _frames.Count)
        'End Sub

        'Public Sub Start()
        '    If Not _isInitialized OrElse _isAnimating Then Return

        '    _frameIndex = 0
        '    _timer = New DispatcherTimer()
        '    _timer.Interval = TimeSpan.FromMilliseconds(FrameIntervalMs)
        '    AddHandler _timer.Tick, AddressOf OnTick
        '    _timer.Start()
        '    _isAnimating = True
        'End Sub

        'Public Sub [Stop]()
        '    If _timer IsNot Nothing Then
        '        _timer.Stop()
        '        RemoveHandler _timer.Tick, AddressOf OnTick
        '        _timer = Nothing
        '    End If

        '    ' Возвращаем исходную иконку
        '    If _targetWindow IsNot Nothing AndAlso _baseIcon IsNot Nothing Then
        '        _targetWindow.Icon = _baseIcon
        '    End If

        '    _isAnimating = False
        'End Sub

        'Public Sub AnimIconDispose()
        '    [Stop]()
        '    _frames.Clear()
        '    _baseIcon = Nothing
        '    _targetWindow = Nothing
        '    _isInitialized = False
        'End Sub

        'Public ReadOnly Property IsAnimating As Boolean
        '    Get
        '        Return _isAnimating
        '    End Get
        'End Property

        ' ==================== ВНУТРЕННОЕ ====================

        'Private Sub GenerateAllFrames()
        '    _frames.Clear()

        '    Dim half As Integer = TotalFrames \ 2

        '    For i As Integer = 0 To TotalFrames - 1
        '        Dim rtb As New RenderTargetBitmap(IconSize, IconSize, 96, 96, PixelFormats.Pbgra32)
        '        Dim dv As New DrawingVisual()

        '        Using dc As DrawingContext = dv.RenderOpen()

        '            ' 1. Базовая иконка (favicon.ico)
        '            If _baseIcon IsNot Nothing Then
        '                dc.DrawImage(_baseIcon, New Rect(0, 0, IconSize, IconSize))
        '            End If

        '            ' 2. Полоска в две фазы:
        '            '    Фаза 1 (i < half):  заполняется слева направо (левый край стоит, правый едет)
        '            '    Фаза 2 (i >= half): убывает слева направо (правый край стоит, левый догоняет)
        '            Dim barLeft As Double
        '            Dim barRight As Double
        '            Dim p As Double

        '            If i < half Then
        '                p = (i + 1) / half
        '                barLeft = 0
        '                barRight = p * IconSize
        '            Else
        '                p = (i - half + 1) / half
        '                barLeft = p * IconSize
        '                barRight = IconSize
        '            End If

        '            Dim barY As Double = IconSize - BarHeight - 1
        '            Dim brush As New SolidColorBrush(BarColor)
        '            brush.Freeze()

        '            ' Рисуем только если есть что рисовать (последний кадр — пустой,
        '            ' даёт короткую паузу между циклами, как у настоящего индикатора)
        '            If barRight - barLeft > 0.5 Then
        '                dc.DrawRectangle(brush, Nothing, New Rect(barLeft, barY, barRight - barLeft, BarHeight))
        '            End If
        '        End Using

        '        rtb.Render(dv)
        '        rtb.Freeze()
        '        _frames.Add(rtb)
        '    Next

        'End Sub

        'Private Sub OnTick(sender As Object, e As EventArgs)
        '    If _targetWindow Is Nothing OrElse _frames.Count = 0 Then Return
        '    _frameIndex = (_frameIndex + 1) Mod _frames.Count
        '    _targetWindow.Icon = _frames(_frameIndex)
        'End Sub






#End Region







        Private ReadOnly HoursRegex As New Regex("(\d+)\s*ч", RegexOptions.Compiled)
        Private ReadOnly MinsRegex As New Regex("(\d+)\s*м", RegexOptions.Compiled)

        ''' <summary>
        ''' "1ч 34м" → 1.6 | "0ч 19м" → 0.3 | "2ч 45м" → 2.8
        ''' </summary>
        Public Function ToDecimalHours(text As String) As Double
            If String.IsNullOrWhiteSpace(text) Then Return 0

            Dim hMatch = HoursRegex.Match(text)
            Dim mMatch = MinsRegex.Match(text)

            Dim hours As Double = If(hMatch.Success, CDbl(hMatch.Groups(1).Value), 0)
            Dim mins As Double = If(mMatch.Success, CDbl(mMatch.Groups(1).Value), 0)

            Return Math.Round(hours + mins / 60, 2)
        End Function



        Public Function IsBetween(d As Date, minDate As Date, maxDate As Date) As Boolean
            Return d >= minDate AndAlso d <= maxDate
        End Function

        ' Для Nullable Date (Date?), если Zakryt может быть пустым
        <Extension()>
        Public Function IsBetween(d As Date?, minDate As Date, maxDate As Date) As Boolean
            If Not d.HasValue Then Return False
            Return d.Value >= minDate AndAlso d.Value <= maxDate
        End Function


        ' ✅ Новый метод: сам берет даты из Fetcher
        <Extension()>
        Public Function IsInPeriod(d As Date) As Boolean
            Dim nachDateTime = Fetcher.NachDat.Date.AddHours(Fetcher.NachTim)
            Dim konDateTime = Fetcher.KonDat.Date.AddHours(Fetcher.KonTim).AddMinutes(Fetcher.KonMinut)
            Return d >= nachDateTime AndAlso d <= konDateTime
            'Return d >= Fetcher.NachDat AndAlso d <= Fetcher.KonDat
        End Function

        ' ✅ И его версия для Nullable Date (на всякий случай)
        <Extension()>
        Public Function IsInPeriod(d As Date?) As Boolean
            If Not d.HasValue Then Return False
            Return d.Value >= Fetcher.NachDat AndAlso d.Value <= Fetcher.KonDat
        End Function

        <Extension()>
        Public Function IsInRangeWithTime(d As Date) As Boolean
            ' Создаем полные DateTime для границ периода, подставляя часы из свойств Fetcher
            Dim startDateTime As New Date(Fetcher.NachDat.Year, Fetcher.NachDat.Month, Fetcher.NachDat.Day,
                                  Fetcher.NachTim, 0, 0)

            Dim endDateTime As New Date(Fetcher.KonDat.Year, Fetcher.KonDat.Month, Fetcher.KonDat.Day,
                                Fetcher.KonTim, 0, 0)

            ' Возвращаем результат проверки попадания даты d в полный диапазон [startDateTime; endDateTime]
            Return d >= startDateTime AndAlso d <= endDateTime
        End Function

        Public Function AddTimeToDate(baseDate As Date, hours As Integer, Optional minutes As Integer = 0) As Date
            Return baseDate.Date.AddHours(hours).AddMinutes(minutes)
        End Function


        '(Base64 + Reverse)
        Public Function DecryptPassword(encrypted As String) As String
            If String.IsNullOrWhiteSpace(encrypted) Then Return ""
            Try
                Dim bytes = Convert.FromBase64String(encrypted)
                Dim reversed = bytes.Reverse().ToArray()
                Return Encoding.UTF8.GetString(reversed)
            Catch
                Return ""
            End Try
        End Function

        Public Function EncryptPassword(password As String) As String
            If String.IsNullOrWhiteSpace(password) Then Return ""
            Dim bytes = Encoding.UTF8.GetBytes(password)
            Dim reversed = bytes.Reverse().ToArray()
            Return Convert.ToBase64String(reversed)
        End Function

        ''' <summary>
        ''' Превращает код статуса в читаемый текст. Безопасно работает с Nothing.
        ''' </summary>
        Public Function GetStatusText(code As Object) As String
            If code Is Nothing Then Return ""

            Dim c As String = code.ToString().Trim()
            Select Case c
                Case "1" : Return "Принят к учету"
                Case "10" : Return "Начато расследование"
                Case "2" : Return "Расследован"
                Case "6" : Return "Передан другой службе/подразделению"
                Case "7" : Return "Назначен"
                Case Else : Return c ' Возвращаем как есть, если код неизвестен
            End Select
        End Function

        ''' <summary>
        ''' Заменяет визуально похожие латинские буквы на кириллические
        ''' </summary>
        Public Function FixCyrillicLatinity(text As String) As String
            If String.IsNullOrWhiteSpace(text) Then Return text

            ' Карта замены: латинская → кириллическая
            Dim charMap As New Dictionary(Of Char, Char) From {
                {"A"c, "А"c}, {"B"c, "В"c}, {"C"c, "С"c}, {"E"c, "Е"c},
                {"H"c, "Н"c}, {"K"c, "К"c}, {"M"c, "М"c}, {"O"c, "О"c},
                {"P"c, "Р"c}, {"T"c, "Т"c}, {"X"c, "Х"c}, {"Y"c, "У"c},
                {"a"c, "а"c}, {"c"c, "с"c}, {"e"c, "е"c}, {"o"c, "о"c},
                {"p"c, "р"c}, {"x"c, "х"c}, {"y"c, "у"c}
            }

            Dim result As New System.Text.StringBuilder(text.Length)

            For Each ch As Char In text
                If charMap.ContainsKey(ch) Then
                    result.Append(charMap(ch))  ' Заменяем
                Else
                    result.Append(ch)            ' Оставляем как есть
                End If
            Next

            Return result.ToString()
        End Function

        'Public DepNum As New List(Of Integer)({1, 2, 3, 5, 7})
        'Public TRNum As New List(Of Integer)({4, 9, 10, 11, 12})




        '        '========================================================================================================
        '        '========================================================================================================
        '        '========================================================================================================
        '        '========================================================================================================

        Public Function NormFam(surname As String) As String
            If String.IsNullOrWhiteSpace(surname) Then Return surname

            ' Убираем лишние пробелы
            surname = surname.Trim()

            ' Первая буква заглавная, остальные строчные
            If surname.Length >= 1 Then
                surname = Char.ToUpper(surname(0)) & If(surname.Length > 1, surname.Substring(1).ToLower(), "")
            End If

            Return surname
        End Function

        Public Sub Swap(Of T)(ByRef x As T, ByRef y As T)
            Dim temp As T = x
            x = y
            y = temp
        End Sub


        ''' <summary>
        ''' Без параметров - сохраняет в DataFolderPath
        ''' </summary>
        ''' <param name="ForParams"></param>
        ''' <param name="ForDocs"></param>
        ''' <param name="ForReports"></param>
        ''' <returns></returns>
        Public Function GetOrCreateDataFolderPath(Optional ForParams As Boolean = False, Optional ForDocs As Boolean = False, Optional ForReports As Boolean = False) As String
            Dim pth As String

            If ForDocs Then
                pth = My.Settings.DocFolder
            ElseIf ForParams Then
                pth = My.Settings.SetsFolder
            ElseIf ForReports Then
                pth = My.Settings.ReportFolderPath
            Else
                pth = My.Settings.DataFolderPath
            End If

            ' Если путь не задан — используем путь по умолчанию
            If String.IsNullOrWhiteSpace(pth) Then
                pth = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop))
                Dim folderDialog As New FolderBrowserDialog
                With folderDialog
                    If ForDocs Then
                        .Description = "Выберите папку для сохранения документов"
                    ElseIf ForParams Then
                        .Description = "Выберите папку для сохранения параметров"
                    ElseIf ForReports Then
                        .Description = "Выберите папку для сохранения отчетов"
                    Else
                        .Description = "Выберите папку для хранения данных (JSON-файлов)"
                    End If

                    '.Description = If(ForDocs, "Выберите папку для сохранения документов", "Выберите папку для хранения данных (JSON-файлов)")
                    .ShowNewFolderButton = True
                    .InitialDirectory = pth
                End With
                If folderDialog.ShowDialog() = DialogResult.OK Then
                    'сразу же сохраняем путь в параметрах
                    If ForDocs Then
                        My.Settings.DocFolder = folderDialog.SelectedPath
                    ElseIf ForReports Then
                        My.Settings.ReportFolderPath = folderDialog.SelectedPath
                    ElseIf ForParams Then
                        My.Settings.SetsFolder = folderDialog.SelectedPath
                    Else
                        My.Settings.DataFolderPath = folderDialog.SelectedPath
                    End If

                    My.Settings.Save()

                    Return folderDialog.SelectedPath
                Else
                    'тут остается вариант - пути в параметрах нет и папку пользователь не указал
                    ShowMSG(MW, "Папка не выбрана. Сохранение данных невозможно.", "Ошибка")
                    Return Nothing
                End If

            End If
            'если папка в параметрах есть или создана/указана пользователем
            If Not Directory.Exists(pth) Then
                Directory.CreateDirectory(pth)
            End If

            Return pth

        End Function





        Function CanChangeKtoZakryl(OTS As Otkaz) As Boolean
            Dim rez As Boolean = False

            rez = (Not OTS.Uslovie2) AndAlso (Not OTS.Uslovie1) AndAlso (Not OTS.Uslovie3)

            Return rez
        End Function

        Public Sub KtoZakr_SUB()
            If Not CanChangeKtoZakryl(PointedOtkaz) Then
                Dim Tx As String = $"Невозможно изменить расследовавшее предприятие в отказе {PointedOtkaz.Id}{vbCrLf}{PointedOtkaz.MestoOTS}{vbCrLf}"
                If PointedOtkaz.Uslovie2 Then
                    Tx += "отказ отнесен на виновное предприятие"
                ElseIf PointedOtkaz.Uslovie1 Then
                    Tx += "это дубликат или технологическое нарушение"
                ElseIf PointedOtkaz.Uslovie3 Then
                    Tx += "отказ закрыт или передан на другую дорогу"
                End If
                MW.InfoBLOK.AddItem($"{Tx}{vbCrLf}")
                MW.InfoBLOK.ScrollToEnd()
                Exit Sub
            End If

            With MW.PeredachaPopContent
                .OldPred = PointedOtkaz.KtoZakryl
                .EventDate = DateTime.Now
                '.Comment = "для завершения расследования"
                .HistoryCheckBox.IsChecked = False
                .ResetOldSelectOfButton()
            End With
            ' Открываем попап
            MW.PeredachaPopup.IsOpen = True
        End Sub





        ''' <summary>
        ''' Универсальный помощник для безопасного отписывания от событий UI-элементов
        ''' </summary>


        ''' <summary>
        ''' ОТПИСЫВАЕТ ВСЕ ОБРАБОТЧИКИ ОТ ВСЕХ ROUTED EVENT'ОВ
        ''' </summary>
        Public Sub UnsubscribeAllEvents(container As DependencyObject)
            If container Is Nothing Then Return

            Dim ui = TryCast(container, UIElement)
            If ui IsNot Nothing Then
                ' ПОЛУЧАЕМ ВСЕ ROUTED EVENT'ы из типа UIElement
                Dim routedEvents = GetType(UIElement).GetFields(BindingFlags.Public Or BindingFlags.Static) _
                    .Where(Function(f) f.FieldType Is GetType(RoutedEvent)) _
                    .Select(Function(f) DirectCast(f.GetValue(Nothing), RoutedEvent)).ToList()

                ' ДЛЯ КАЖДОГО СОБЫТИЯ ПЫТАЕМСЯ УДАЛИТЬ ВСЕ ОБРАБОТЧИКИ
                For Each ev In routedEvents
                    Try
                        Dim handlers = GetHandlers(ui, ev)
                        For Each handler In handlers
                            ui.RemoveHandler(ev, handler)
                        Next
                    Catch
                        ' ИГНОРИРУЕМ ОШИБКИ ПРИ УДАЛЕНИИ
                    End Try
                Next
            End If

            ' РЕКУРСИЯ ПО ПОТОМКАМ ВИЗУАЛЬНОГО ДЕРЕВА
            Dim count = VisualTreeHelper.GetChildrenCount(container)
            For i As Integer = 0 To count - 1
                UnsubscribeAllEvents(VisualTreeHelper.GetChild(container, i))
            Next
        End Sub

        ''' <summary>
        ''' ПОЛУЧАЕТ СПИСОК ОБРАБОТЧИКОВ ДЛЯ КОНКРЕТНОГО СОБЫТИЯ ЧЕРЕЗ ВНУТРЕННИЙ КЭШ WPF
        ''' </summary>
        Private Function GetHandlers(element As UIElement, routedEvent As RoutedEvent) As List(Of [Delegate])
            Dim handlers As New List(Of [Delegate])()

            Try
                ' ДОСТУП К ВНУТРЕННЕМУ ПОЛЮ _eventHandlersStore
                Dim eventHandlersStoreField = GetType(UIElement).GetField("_eventHandlersStore",
                    BindingFlags.NonPublic Or BindingFlags.Instance)

                If eventHandlersStoreField IsNot Nothing Then
                    Dim store = eventHandlersStoreField.GetValue(element)
                    If store IsNot Nothing Then
                        ' ВЫЗЫВАЕМ МЕТОД GetHandlers(store, RoutedEvent)
                        Dim getHandlersMethod = store.GetType().GetMethod("GetHandlers",
                            BindingFlags.Public Or BindingFlags.Instance)

                        If getHandlersMethod IsNot Nothing Then
                            Dim result = getHandlersMethod.Invoke(store, New Object() {routedEvent})
                            If result IsNot Nothing Then
                                Dim array = TryCast(result, Array)
                                If array IsNot Nothing Then
                                    For Each item In array
                                        If item IsNot Nothing Then
                                            Dim handler = TryCast(item, [Delegate])
                                            If handler IsNot Nothing Then
                                                handlers.Add(handler)
                                            End If
                                        End If
                                    Next
                                End If
                            End If
                        End If
                    End If
                End If
            Catch
                ' ЕСЛИ ВНУТРЕННЯЯ СТРУКТУРА ИЗМЕНИТСЯ В НОВОЙ ВЕРСИИ .NET - ПРОСТО ВЕРНЕМ ПУСТОЙ СПИСОК
            End Try

            Return handlers
        End Function

        ''' <summary>
        ''' Очищает список выделенных ячеек и сбрасывает их фон
        ''' </summary>
        Public Sub ClearSelection(selectedCells As List(Of TextBlock), defaultBrush As Brush)
            If selectedCells Is Nothing Then Return
            For Each cell In selectedCells
                cell.Background = defaultBrush
            Next
            selectedCells.Clear()
        End Sub




        Sub ResignOTSList(Lst As List(Of Otkaz))

            OTSList = Lst
            ResetPeriodSUB()
            InitFirst()
            'YarCon.UpdateYarlykInfo()
        End Sub


        Public Sub AddOTSToContainer(Optional LST As List(Of Otkaz) = Nothing)


            'Dim elapsedTime, elapsedTime1 As TimeSpan
            Dim TempLST As New List(Of Otkaz)

            If Not IsNothing(PointedYarlyk) AndAlso Not IsNothing(PointedYarlyk.Zapros) Then
                'Stopwatch.Reset()
                'Stopwatch.Start()

                If LST IsNot Nothing Then
                    TempLST = LST.Where(PointedYarlyk.Zapros).ToList
                Else
                    TempLST = OTSList.Where(PointedYarlyk.Zapros).ToList
                End If

                'Stopwatch.Stop()
                'elapsedTime = Stopwatch.Elapsed
            Else
                MW.InfoBLOK.AddItem($"Запрос не указан")
                MW.InfoBLOK.ScrollToEnd()
                Return ' ⚠️ ВАЖНО: выходим, если запроса нет, чтобы не выполнять лишнюю работу
            End If

            ' === КЛЮЧЕВОЕ ИЗМЕНЕНИЕ: ОЧИСТКА СТАРОГО СПИСКА ПЕРЕД ЗАГРУЗКОЙ НОВОГО ===

            ' 1. Сбрасываем ItemsSource в Nothing, чтобы WPF начал уничтожать старые OTSRowControl
            MW.TRowsContainer.ItemsSource = Nothing

            ' 2. Принудительно собираем мусор ПОСЛЕ сброса, но ДО создания нового списка
            '    Это освобождает память от старых контролов и фильтрованного TempLST предыдущего вызова
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking:=True)
            GC.WaitForPendingFinalizers()

            ' ================================================================

            'Stopwatch.Reset()
            'Stopwatch.Start()

            ' 3. Теперь безопасно присваиваем новый источник
            MW.TRowsContainer.ItemsSource = TempLST.OrderBy(Function(u) u.Nach)
            MW.YarlykContainer.UpdateYarlykInfo()
            TempLST = Nothing

            'Stopwatch.Stop()
            'elapsedTime1 = Stopwatch.Elapsed

            If Not IsNothing(PointedOtkaz) Then
                MW.TRowsContainer.ScrollToPointedOTS()
            Else
                MW.TRowsContainer.ScrollToEnd()
            End If

            'MW.InfoBLOK.AddItem($" {TempLST.Count} элементов отфильтровано{vbCrLf}за {elapsedTime.TotalMilliseconds:F0} мс")
            'MW.InfoBLOK.AddItem($" {TempLST.Count} элементов передано в контейнер и отсортировано{vbCrLf}за {elapsedTime1.TotalMilliseconds:F0} мс")
            'MW.InfoBLOK.ScrollToEnd()




        End Sub

        Public Async Function GetGenOtchRows(SaveFilteredItog As Boolean) As Task(Of List(Of GenReportRow))
            Log("📊 Запрос и отображение ГенОтчета...")
            'Получаем готовый список строк одной строкой
            Dim rows As List(Of GenReportRow)
            rows = Await DownloadAndParseGenReportAsync(Fetcher.NachDat, Fetcher.KonDat, SaveFilteredItog:=SaveFilteredItog)

            'тут если из сети ничего не выловилось
            If rows Is Nothing Then
                Dim dlg As New Microsoft.Win32.OpenFileDialog() With {.Filter = "Excel файлы|*.xls;*.xlsx|Все файлы|*.*",
        .Title = "Выберите файл ГенОтчета", .InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}

                ' Если пользователь нажал "Отмена", просто выходим из метода
                If dlg.ShowDialog() <> True Then
                    Return Nothing ' В Function используем Return, а не Exit Sub
                End If
                Log($"📂 Чтение локального файла: {dlg.FileName}")
                rows = ParseExcelGenReport(dlg.FileName, SaveFilteredItog:=SaveFilteredItog)
            End If
            Return rows

        End Function


        Sub ResetPeriodSUB()
            ' 1. Сбрасываем период в настройках
            With My.Settings
                .NachPeriod = Date.MinValue
                .KonPeriod = Date.MinValue
                .nachOLDPeriod = Date.MinValue
                .konOLDPeriod = Date.MinValue
                .TimeMode = "From00To2359"
                .Save()
            End With

            MW.DContrPop.PeriodBC.Nach = Nothing
            MW.DContrPop.PeriodBC.Kon = Nothing

            MW.InfoBLOK.AddItem($"ПЕРИОД Сброшен:{vbCrLf}My.Settings.NachPeriod = Date.MinValue{vbCrLf}My.Settings.KonPeriod = Date.MinValue{vbCrLf}My.Settings.nachOLDPeriod = Date.MinValue{vbCrLf}My.Settings.konOLDPeriod = Date.MinValue{vbCrLf}DContrPop.PeriodBC.Nach = Nothing{vbCrLf}DContrPop.PeriodBC.Kon = Nothing")

            ' 3. Обновляем UI

            InitFirst()                  ' ← ваша глобальная инициализация
            'YarCon.UpdateYarlykInfo()   ' ← обновление ярлыков

            MW.InfoBLOK.ScrollToEnd()


        End Sub


        Sub InitFirst()

            MW.TRowsContainer.ClearAll0LevelFilters()
            MW.TRowsContainer.THed.ClearAllFilters()
            ChangeFilters()
            AddOTSToContainer()

            With My.Settings
                If .NachPeriod.Date = Date.MinValue.Date OrElse .KonPeriod.Date = Date.MinValue.Date Then
                    MW.InfoBLOK.AddItem($"Период не задан")
                Else
                    MW.InfoBLOK.AddItem($"Период{vbCrLf}начало - { .NachPeriod:dd.MM.yy HH:mm}{vbCrLf}окончание - { .KonPeriod:dd.MM.yy HH:mm}")
                End If
                MW.InfoBLOK.ScrollToEnd()
            End With

        End Sub

        Public Sub ChangeFilters(Optional ExclOTS As List(Of Otkaz) = Nothing)

            Dim THIS_OTS As List(Of Otkaz)
            If Not IsNothing(ExclOTS) Then
                THIS_OTS = ExclOTS
            Else
                THIS_OTS = OTSList
            End If
            ' Формируем базовый список с учётом ярлыка
            Dim baseList As List(Of Otkaz)


            If PointedYarlyk?.Zapros IsNot Nothing Then
                baseList = THIS_OTS.Where(PointedYarlyk.Zapros).ToList()
            Else
                baseList = THIS_OTS
            End If

            ' Применяем Level0-фильтры
            Dim level0Pred = SafePredicate(MW.TRowsContainer.BuildLevel0CombinedPredicate())
            baseList = baseList.Where(level0Pred).ToList()

            ' Применяем THed-фильтры
            Dim mainPred = SafePredicate(MW.TRowsContainer.THed.BuildCombinedPredicate())
            baseList = baseList.Where(mainPred).ToList()

            ' 🔥 Применяем поиск по Opis
            If Not String.IsNullOrEmpty(MW.CurrentSearchTerm) Then
                baseList = baseList.Where(Function(o) o.ContainsPhrase(MW.CurrentSearchTerm)).ToList()
            End If

            ' 🔥 Применяем фильтр NumOTSUserTB (если есть)
            If MW._filterNumbers IsNot Nothing AndAlso MW._filterNumbers.Any() Then
                If MW._isLokFilter Then
                    baseList = baseList.Where(Function(o) o IsNot Nothing AndAlso Not String.IsNullOrEmpty(o.NumLok) AndAlso MW._filterNumbers.Any(Function(f) o.NumLok.Contains(f))).ToList()
                Else
                    baseList = baseList.Where(Function(o) o IsNot Nothing AndAlso MW._filterNumbers.Contains(o.Id)).ToList()
                End If
            End If

            ' Сортируем и устанавливаем
            MW.TRowsContainer.ItemsSource = baseList.OrderBy(Function(o) o.Nach).ToList()
            baseList = Nothing
            THIS_OTS = Nothing
        End Sub

        Public Function SafePredicate(p As Func(Of Otkaz, Boolean)) As Func(Of Otkaz, Boolean)
            Return If(p Is Nothing, Function(o) True, p)
        End Function



        Sub ClearAllUpdateNotes(OTSLST As List(Of Otkaz))
            For Each OTS In OTSLST
                OTS.ClearAllItemUpdateNotes()
            Next
        End Sub



    End Module



End Namespace


