Imports System.Text


Namespace Kas
    Partial Public Class KasJournalParamControl



        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().

        End Sub





        '================= просмотр журнала из 4 отчета =========================

        Private Async Sub btnLoadOfflineJournal_Click(sender As Object, e As RoutedEventArgs)
            ' 1. Открываем диалог выбора файла
            Dim dlg As New Microsoft.Win32.OpenFileDialog() With {
                .Filter = "HTML файлы КАСАНТ|*.html;*.htm|Все файлы|*.*",
                .Title = "Выберите сохранённый журнал 'Список отказов'",
                .InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            }

            If dlg.ShowDialog() <> True Then Exit Sub
            btnLoadOfflineJournal.IsEnabled = False

            Try
                ' 2. Чтение и парсинг в фоновом потоке (UI не блокируется)
                Dim records As List(Of JournalRecord) = Await System.Threading.Tasks.Task.Run(Function()
                                                                                                  ' Читаем с родной кодировкой РЖД
                                                                                                  Dim html As String = System.IO.File.ReadAllText(dlg.FileName, Encoding.GetEncoding("windows-1251"))
                                                                                                  Return Fetcher.ParseJournalList(html)
                                                                                              End Function)

                ' 3. Автоматический возврат в UI-поток (благодаря Await)

                ShowJournalPopup(records, btnLoadOfflineJournal, "88")

            Catch ex As Exception
                ShowMSG(MW, $"Ошибка при обработке файла:{vbCrLf}{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error)
            Finally
                btnLoadOfflineJournal.IsEnabled = True
            End Try
        End Sub


        Private Async Sub btnLoadOnlineJournal_Click(sender As Object, e As RoutedEventArgs)

            ' 1. Сохраняем исходный текст кнопки и меняем его на время загрузки
            Dim originalText As String = btnLoadOnlineJournal.Content.ToString()
            btnLoadOnlineJournal.Content = "⏳ Загрузка..."

            ' 2. Меняем курсор на всем окне на "Ожидание"
            Me.Cursor = System.Windows.Input.Cursors.Wait
            btnLoadOnlineJournal.IsEnabled = False

            MW.InfoBLOK.AddItem($" {ParamPeriodCTL.DorTBlock.SelectedValue}")

            ' ✅ ШАГ 0: ПРИНУДИТЕЛЬНАЯ ПРОВЕРКА/УСТАНОВЛЕНИЕ СЕССИИ
            If Not Await Fetcher.EnsureConnectedAsync() Then
                ' Возвращаем всё как было при неудачном подключении
                btnLoadOnlineJournal.Content = originalText
                btnLoadOnlineJournal.IsEnabled = True
                Me.Cursor = System.Windows.Input.Cursors.Arrow
                Return
            End If

            Dim DK As New RailwayCodeItem(ParamPeriodCTL.DorTBlock.SelectedItem.ToString, CInt(ParamPeriodCTL.DorTBlock.SelectedValue))
            ' Ссылка из строки "ВСЕГО" (294 отказа)
            Dim journalUrl = BuildJournalUrl(Fetcher.NachDat, Fetcher.KonDat, Fetcher.NachTim, Fetcher.KonTim, DK.Code, Fetcher.KonMinut)

            Try
                ' 1. Загружаем все страницы
                Dim allRecords = Await Fetcher.FetchAllJournalPagesAsync(journalUrl)

                ' 2. Показываем в попупе (ваш готовый метод)
                ShowJournalPopup(allRecords, btnLoadOnlineJournal, DK.Code)

            Catch ex As Exception
                ShowMSG(MW, $"Ошибка загрузки: {ex.Message}", "КАСАНТ", MessageBoxButton.OK, MessageBoxImage.Error)
            Finally
                ' 3. ГАРАНТИРОВАННО возвращаем исходное состояние, даже если была ошибка
                btnLoadOnlineJournal.Content = originalText
                btnLoadOnlineJournal.IsEnabled = True
                Me.Cursor = System.Windows.Input.Cursors.Arrow
            End Try


        End Sub

        Private Async Sub btnLoadOnlineSLDJournal_Click(sender As Object, e As RoutedEventArgs)

            ' 1. Сохраняем исходный текст и меняем UI на состояние "Загрузка"
            Dim originalText As String = btnLoadOnlineSLDJournal.Content.ToString()
            btnLoadOnlineSLDJournal.Content = "⏳ Загрузка..."

            Me.Cursor = System.Windows.Input.Cursors.Wait
            btnLoadOnlineSLDJournal.IsEnabled = False

            Try
                ' 2. Выполняем основную логику
                Dim r341 As New Kas.Report341Fetcher()
                Await r341.ShowDepotReportAsync()

            Catch ex As Exception
                ' 3. Обрабатываем ошибку, чтобы программа не упала, а пользователь понял, что случилось
                ShowMSG(MW, $"Ошибка при формировании отчета СЛД: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error)

            Finally
                ' 4. ГАРАНТИРОВАННО возвращаем кнопку в исходное состояние
                btnLoadOnlineSLDJournal.Content = originalText
                btnLoadOnlineSLDJournal.IsEnabled = True
                Me.Cursor = System.Windows.Input.Cursors.Arrow
            End Try

        End Sub

        Private Sub BtnTest_WithPeriod2_Click(sender As Object, e As RoutedEventArgs)
            ' === СЦЕНАРИЙ: День < 15 (Есть 4 столбца данных) ===
            Dim testData = GenerateTestDepotData()

            ' Создаем окно для отображения
            Dim win As New Window() With {
                .Title = "ТЕСТ: Периода 2 НЕТ (2 столбца данных)",
                .WindowStartupLocation = WindowStartupLocation.CenterScreen,
                .SizeToContent = SizeToContent.WidthAndHeight
            }

            Dim viewer As New DepotReportViewerControl()
            ' Передаем period2Title, чтобы контрол показал 4-й и 5-й столбцы
            viewer.LoadData(testData, "1–14 июня", "1–14 июня")

            win.Content = viewer
            win.Show()
        End Sub

        Private Sub BtnTest_WithoutPeriod2_Click(sender As Object, e As RoutedEventArgs)
            ' === СЦЕНАРИЙ: День >= 15 (Есть только 2 столбца данных) ===
            Dim testData = GenerateTestDepotData()

            Dim win As New Window() With {
                .Title = "ТЕСТ: Периода 2 НЕТ (2 столбца данных)",
                .WindowStartupLocation = WindowStartupLocation.CenterScreen,
                .SizeToContent = SizeToContent.WidthAndHeight
            }
            '.Width = 600, ' Окно уже, так как колонок меньше
            '.Height = 400,


            Dim viewer As New DepotReportViewerControl()
            ' Передаем Nothing вместо period2Title, чтобы контрол СКРЫЛ 4-й и 5-й столбцы
            viewer.LoadData(testData, "15–30 июня", Nothing)

            win.Content = viewer
            win.Show()
        End Sub

        Private Async Sub btnLoadOverdueJournal_Click(sender As Object, e As RoutedEventArgs)

            ' 1. Сохраняем исходный текст кнопки и меняем его на время загрузки
            Dim originalText As String = btnLoadOverdueJournal.Content.ToString()
            btnLoadOverdueJournal.Content = "⏳ Загрузка ..."

            ' 2. Меняем курсор на всем окне на "Ожидание"
            'Me.Cursor = System.Windows.Input.Cursors.Wait
            btnLoadOverdueJournal.IsEnabled = False

            MW.InfoBLOK.AddItem($" [Просроченные] {ParamPeriodCTL.DorTBlock.SelectedValue}")

            ' ✅ ШАГ 0: ПРИНУДИТЕЛЬНАЯ ПРОВЕРКА/УСТАНОВЛЕНИЕ СЕССИИ
            If Not Await Fetcher.EnsureConnectedAsync() Then
                ' Возвращаем всё как было при неудачном подключении
                btnLoadOverdueJournal.Content = originalText
                btnLoadOverdueJournal.IsEnabled = True
                'Me.Cursor = System.Windows.Input.Cursors.Arrow
                Return
            End If

            Dim DK As New RailwayCodeItem(ParamPeriodCTL.DorTBlock.SelectedItem.ToString, CInt(ParamPeriodCTL.DorTBlock.SelectedValue))

            '--------------------------------------------------------
            'это для вставки в проверку ОТСЛист на просрочку
            'Dim overdueUrl = BuildOverdueJournalUrl(Fetcher.NachDat, Fetcher.KonDat, Fetcher.NachTim, Fetcher.KonTim, 88, Fetcher.KonMinut)
            'Dim webRecords = Await Fetcher.FetchAllJournalPagesAsync(overdueUrl)
            '--------------------------------------------------------


            ' Ссылка для журнала просроченных отказов
            Dim overdueUrl = BuildOverdueJournalUrl(Fetcher.NachDat, Fetcher.KonDat, Fetcher.NachTim, Fetcher.KonTim, DK.Code, Fetcher.KonMinut)

            Try
                ' 1. Загружаем все страницы
                Dim overdueRecords = Await Fetcher.FetchAllJournalPagesAsync(overdueUrl)

                ' 2. Показываем в попупе (тот же метод, что и для обычного журнала)
                ShowJournalPopup(overdueRecords, btnLoadOverdueJournal, DK.Code, OverDue:=True)

            Catch ex As Exception
                ShowMSG(MW, $"Ошибка загрузки просроченных: {ex.Message}", "КАСАНТ - Просроченные", MessageBoxButton.OK, MessageBoxImage.Error)
            Finally
                ' 3. ГАРАНТИРОВАННО возвращаем исходное состояние, даже если была ошибка
                btnLoadOverdueJournal.Content = originalText
                btnLoadOverdueJournal.IsEnabled = True
                'Me.Cursor = System.Windows.Input.Cursors.Arrow
            End Try

        End Sub

        Private Async Sub btnLoadDangerousJournal_Click(sender As Object, e As RoutedEventArgs)
            Dim originalText As String = btnLoadDangerousJournal.Content.ToString()
            btnLoadDangerousJournal.Content = "⏳ Загрузка..."
            btnLoadDangerousJournal.IsEnabled = False

            MW.InfoBLOK.AddItem($" [Опасные без 5.15] {ParamPeriodCTL.DorTBlock.SelectedValue}")

            If Not Await Fetcher.EnsureConnectedAsync() Then
                btnLoadDangerousJournal.Content = originalText
                btnLoadDangerousJournal.IsEnabled = True
                Return
            End If

            Dim DK As New RailwayCodeItem(ParamPeriodCTL.DorTBlock.SelectedItem.ToString, CInt(ParamPeriodCTL.DorTBlock.SelectedValue))
            Dim dangerousUrl = BuildDangerousJournalUrl(Fetcher.NachDat, Fetcher.KonDat, Fetcher.NachTim, Fetcher.KonTim, DK.Code, Fetcher.KonMinut)

            Try
                Dim dangerousRecords = Await Fetcher.FetchAllJournalPagesAsync(dangerousUrl)
                ShowJournalPopup(dangerousRecords, btnLoadDangerousJournal, DK.Code, Dangerous:=True)
            Catch ex As Exception
                ShowMSG(MW, $"Ошибка загрузки опасных отказов: {ex.Message}", "КАСАНТ", MessageBoxButton.OK, MessageBoxImage.Error)
            Finally
                btnLoadDangerousJournal.Content = originalText
                btnLoadDangerousJournal.IsEnabled = True
            End Try
        End Sub










        Private Async Sub btnLoadInvestigationReport_Click(sender As Object, e As RoutedEventArgs)


            Dim originalText = btnLoadInvestigationReport.Content.ToString()
            btnLoadInvestigationReport.Content = "⏳ Загрузка ..."
            btnLoadInvestigationReport.IsEnabled = False

            MW.InfoBLOK.AddItem($" [Отчёт] {ParamPeriodCTL.DorTBlock.SelectedValue}")

            If Not Await Fetcher.EnsureConnectedAsync() Then
                btnLoadInvestigationReport.Content = originalText
                btnLoadInvestigationReport.IsEnabled = True
                Return
            End If

            ' Dim DK As New RailwayCodeItem(DorTBlock.SelectedItem.ToString, CInt(DorTBlock.SelectedValue))


            Try
                Dim currentUrl = BuildInvestigationReportUrl(Fetcher.NachDat, Fetcher.KonDat, Fetcher.NachTim, Fetcher.KonTim, Fetcher.KonMinut)
                Dim previousYearUrl = BuildInvestigationReportUrl(Fetcher.NachDat.AddYears(-1), Fetcher.KonDat.AddYears(-1), Fetcher.NachTim, Fetcher.KonTim, Fetcher.KonMinut)

                ' 🔑 ЖДЁМ формирования отчётов (до 60 секунд)
                MW.InfoBLOK.AddItem("  [Отчёт] Ожидание формирования...")

                Dim currentData As List(Of InvestigationReportItem) = Nothing
                Dim previousYearData As List(Of InvestigationReportItem) = Nothing
                Dim startTime = DateTime.Now

                While (DateTime.Now - startTime).TotalSeconds < 60
                    currentData = Await Fetcher.FetchInvestigationReportAsync(currentUrl)
                    previousYearData = Await Fetcher.FetchInvestigationReportAsync(previousYearUrl)

                    ' Если оба отчёта содержат данные — выходим из цикла
                    If currentData.Count > 0 OrElse previousYearData.Count > 0 Then
                        Exit While
                    End If

                    ' Ждём 0.5 секунды
                    Await Task.Delay(500)
                End While

                If currentData.Count = 0 AndAlso previousYearData.Count = 0 Then
                    ShowMSG(MW, "Отчёт не сформировался за 60 секунд", "КАСАНТ - Отчёт", MessageBoxButton.OK, MessageBoxImage.Warning)
                    Return
                End If

                Dim curYear = Fetcher.NachDat.Year
                Dim prevYear = Fetcher.NachDat.AddYears(-1).Year

                MergeAndShowReport(currentData, previousYearData, curYear, prevYear, btnLoadInvestigationReport)

            Catch ex As Exception
                ShowMSG(MW, $"Ошибка загрузки отчёта: {ex.Message}", "КАСАНТ - Отчёт", MessageBoxButton.OK, MessageBoxImage.Error)
            Finally
                btnLoadInvestigationReport.Content = originalText
                btnLoadInvestigationReport.IsEnabled = True
            End Try
        End Sub









        Private Sub btnLoadInvestigationFile_Click(sender As Object, e As RoutedEventArgs)
            Dim dlg As New Microsoft.Win32.OpenFileDialog()
            dlg.Filter = "HTML файлы|*.html|Все файлы|*.*"
            dlg.Title = "Выберите файл отчёта"

            If dlg.ShowDialog() <> True Then Return

            Dim originalText = btnLoadInvestigationFile.Content.ToString()
            btnLoadInvestigationFile.Content = "⏳ Загрузка ..."
            btnLoadInvestigationFile.IsEnabled = False

            Try
                ' Читаем файл
                Dim html As String = System.IO.File.ReadAllText(dlg.FileName, Encoding.GetEncoding("windows-1251"))

                ' Извлекаем даты периода из HTML
                Dim dateMatch = System.Text.RegularExpressions.Regex.Match(html, "за период с (\d{2}\.\d{2}\.\d{4}).*?по (\d{2}\.\d{2}\.\d{4})")
                Dim curYear As Integer
                If dateMatch.Success Then
                    Dim endDate = DateTime.ParseExact(dateMatch.Groups(2).Value, "dd.MM.yyyy", Nothing)
                    curYear = endDate.Year
                Else
                    curYear = DateTime.Now.Year
                End If

                ' Парсим HTML
                Dim currentData = ParseInvestigationReport(html)

                If currentData Is Nothing OrElse currentData.Count = 0 Then
                    ShowMSG(MW, "Не удалось распарсить файл или данные отсутствуют", "КАСАНТ - Отчёт", MessageBoxButton.OK, MessageBoxImage.Warning)
                    Return
                End If

                ' Для файла данные ПГ = пустой список
                Dim previousYearData As New List(Of InvestigationReportItem)

                Dim DK As New RailwayCodeItem("Из файла", 88)

                ' Используем общую процедуру
                MergeAndShowReport(currentData, previousYearData, curYear, curYear - 1, btnLoadInvestigationFile)

                MW.InfoBLOK.AddItem($" [Отчёт из файла] {System.IO.Path.GetFileName(dlg.FileName)} - {currentData.Count} строк")

            Catch ex As Exception
                ShowMSG(MW, $"Ошибка загрузки файла: {ex.Message}", "КАСАНТ - Отчёт", MessageBoxButton.OK, MessageBoxImage.Error)
            Finally
                btnLoadInvestigationFile.Content = originalText
                btnLoadInvestigationFile.IsEnabled = True
            End Try
        End Sub

        Private Async Sub btnLoadDepotWeb_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            ' Вызываем асинхронный метод из модуля KasAntLoader
            Await KasAntLoader.LoadDepotReportAsync(btn)
        End Sub

        Private Sub btnLoadDepotFile_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            ' Вызываем метод чтения из файла
            KasAntLoader.LoadDepotReportFromFile(btn)
        End Sub

        Private Async Sub btnLoadSLDWeb_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            Await KasAntLoader.LoadSLDReportAsync(btn)
        End Sub

        Private Async Sub btnCorpViolationJournal_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            Await KasAntLoader.LoadCorpViolationJournalAsync(btn)
        End Sub

        Private Async Sub btnEvents_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            Await KasAntLoader.LoadEventsJournalAsync(btn)
        End Sub


        'Private Sub btnS24Show_Click(sender As Object, e As RoutedEventArgs)



        'If My.Settings.OldYJSON = "" Then Exit Sub
        'Dim oldY As List(Of Otkaz) = StorageModule.LoadFromJson(My.Settings.OldYJSON)

        'Dim fullCurY = GetFullCurrentYearOtkazy(OTSList, oldY, Fetcher.NachDat, Fetcher.KonDat)

        '' спросили ОДИН раз
        'Dim useOneTable As Boolean = AskOneTable()

        'Dim tables1 = BuildTableList(Of S24Table1)(
        '    Function(st, en) New S24Table1(fullCurY, oldY, st, en),
        '    useOneTable)

        'Dim tables3 = BuildTableList(Of S24Table3)(
        '    Function(st, en) New S24Table3(fullCurY, oldY, st, en),
        '    useOneTable)

        'Dim tables4 = BuildTableList(Of S24Table4)(
        '    Function(st, en) New S24Table4(fullCurY, st, en),
        '    useOneTable)

        '' ▼▼▼ ВОТ ТУТ, в кнопке: табл.1 своей строкой, табл.3+табл.4 парой в один ряд ▼▼▼
        'Dim all As New List(Of FrameworkElement)
        'Dim maxN = Math.Max(tables1.Count, Math.Max(tables3.Count, tables4.Count))

        'For i = 0 To maxN - 1
        '    If i < tables1.Count Then all.Add(tables1(i))

        '    Dim pair As New StackPanel With {
        '        .Orientation = Orientation.Horizontal,
        '        .VerticalAlignment = VerticalAlignment.Top,
        '        .Margin = New Thickness(0, 10, 0, 0)
        '    }
        '    If i < tables3.Count Then pair.Children.Add(tables3(i))
        '    If i < tables4.Count Then
        '        tables4(i).Margin = New Thickness(20, 0, 0, 0)
        '        pair.Children.Add(tables4(i))
        '    End If
        '    If pair.Children.Count > 0 Then all.Add(pair)
        'Next

        'ShowInWindow(all, $"Справка {Fetcher.NachDat.ToString("yy")}")


        'End Sub



        'Private Sub btnS24T2Show_Click(sender As Object, e As RoutedEventArgs)


        'If My.Settings.OldYJSON = "" Then Exit Sub
        'Dim oldY As List(Of Otkaz) = StorageModule.LoadFromJson(My.Settings.OldYJSON)

        '' Для основных таблиц: фильтруем по периоду
        'Dim fullCurY = GetFullCurrentYearOtkazy(OTSList, oldY, Fetcher.NachDat, Fetcher.KonDat)
        '' Для "с начала года": берём всё без фильтрации
        'Dim ytdOtkazy = GetYearToDateOtkazy(OTSList, oldY)

        '' ▼▼▼ спросили один раз ▼▼▼
        'Dim useOneTable As Boolean = AskOneTable()

        'Dim tables = BuildTableList(Of S24Table2)(
        '    Function(st, en) New S24Table2(fullCurY, oldY, st, en, ytdOtkazy),
        '    useOneTable)

        '' ▼▼▼ вот тут: перекладываем в List(Of FrameworkElement) ▼▼▼
        'Dim all As New List(Of FrameworkElement)
        'all.AddRange(tables)

        'ShowInWindow(all, $"Отказы и часы {Fetcher.NachDat.ToString("yy")}")

        'End Sub



        'Private Function GetMonthPeriods() As List(Of (Start As Date, [End] As Date))
        '    Dim result As New List(Of (Start As Date, [End] As Date))

        '    Dim curStart As Date = Fetcher.NachDat

        '    While curStart <= Fetcher.KonDat
        '        Dim curEnd As Date = New Date(
        '    curStart.Year,
        '    curStart.Month,
        '    DateTime.DaysInMonth(curStart.Year, curStart.Month)
        ')

        '        If curEnd > Fetcher.KonDat Then curEnd = Fetcher.KonDat

        '        result.Add((curStart, curEnd))
        '        curStart = curEnd.AddDays(1)
        '    End While

        '    Return result
        'End Function



        'Private Function AskOneTable() As Boolean
        '    Dim differentMonths As Boolean =
        'Fetcher.NachDat.Year <> Fetcher.KonDat.Year OrElse
        'Fetcher.NachDat.Month <> Fetcher.KonDat.Month

        '    ' месяцы одинаковые — и спрашивать нечего
        '    If Not differentMonths Then Return True

        '    Return ShowMSG(
        'owner:=MW,
        '$"Начало и конец периода находятся в разных месяцах{vbCrLf}Показать одну таблицу за весь период [OK]{vbCrLf}Разбить период на месяцы [ОТМЕНА]",
        '"Справка", MsgButtons.OKCancel)
        'End Function


        'Private Function BuildTableList(Of T As FrameworkElement)(create As Func(Of Date, Date, T), useOneTable As Boolean) As List(Of T)


        '    Dim result As New List(Of T)

        '    If useOneTable Then
        '        result.Add(create(Fetcher.NachDat, Fetcher.KonDat))
        '    Else
        '        For Each p In GetMonthPeriods()
        '            result.Add(create(p.Start, p.End))
        '        Next
        '    End If

        '    Return result
        'End Function

        '    Private Sub ShowInWindow(controls As List(Of FrameworkElement), title As String)
        '        Dim panel As New StackPanel() With {
        '    .HorizontalAlignment = HorizontalAlignment.Stretch
        '}

        '        For Each c In controls
        '            c.Margin = New Thickness(2, 15, 2, 15)
        '            panel.Children.Add(c)
        '        Next

        '        Dim scroller As New ScrollViewer() With {
        '        .Margin = New Thickness(15),
        '        .Content = panel,
        '    .VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        '    .HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        '}

        '        ' ▼▼▼ заставляем внешний скроллер реагировать на колесо ЛЮБОЙ точкой мыши ▼▼▼
        '        AddHandler scroller.PreviewMouseWheel, Sub(s As Object, e As MouseWheelEventArgs)
        '                                                   Dim sv = DirectCast(s, ScrollViewer)
        '                                                   sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta / 3)
        '                                                   e.Handled = True
        '                                               End Sub
        '        ' ▲▲▲ ▲▲▲


        '        Dim win As New Window() With {
        '        .Owner = MW,
        '    .Title = title,
        '    .WindowStartupLocation = WindowStartupLocation.CenterScreen,
        '    .SizeToContent = SizeToContent.WidthAndHeight,
        '    .MaxWidth = SystemParameters.PrimaryScreenWidth,
        '    .MaxHeight = SystemParameters.PrimaryScreenHeight / 2
        '}

        '        win.Content = scroller
        '        win.Show()
        '    End Sub

        '================================================================================


    End Class

    ' В том же файле или в отдельном модуле
    Public Class RailwayCodeItem
        Public Property Name As String
        Public Property Code As Integer
        Public Sub New(n As String, c As Integer)
            Name = n : Code = c
        End Sub
    End Class



End Namespace

