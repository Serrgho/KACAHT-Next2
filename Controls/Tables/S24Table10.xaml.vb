Imports System.Globalization
Imports System.Net.Http
Imports System.Text
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Windows.Media
Imports HtmlAgilityPack

Namespace Kas

    Partial Public Class S24Table10

        Private Shared ReadOnly Ru As CultureInfo = CultureInfo.GetCultureInfo("ru-RU")

        ' Внутренний класс для хранения распарсенных данных периода
        Private Class PeriodData
            Public Property DepotOts As Dictionary(Of String, Integer)
            Public Property DepotSld As Dictionary(Of String, Integer)
        End Class

        Private _periodStart As Date?
        Private _periodEnd As Date?

        Public Sub New(Optional periodStart As Date? = Nothing,
                   Optional periodEnd As Date? = Nothing)
            InitializeComponent()
            _periodStart = periodStart
            _periodEnd = periodEnd
        End Sub

        Private Sub S24Table10_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
            Try
                LblCaption.Text = $"Балловая оценка отказов 1 категории (сетевой учёт)"
                BuildAndFillRowsAsync()
            Catch ex As Exception
                SafeShowMessage(ex.Message, "Ошибка инициализации")
            End Try
        End Sub


        ' ' === ГЛОБАЛЬНЫЙ ФЛАГ БЛОКИРОВКИ ДЛЯ ВСЕХ ЭКЗЕМПЛЯРОВ ТАБЛИЦЫ ===
        ' Если сообщение уже висит на экране, новые попытки его открыть игнорируются
        Private Shared _isMsgBoxActive As Boolean = False

        Private Async Sub BuildAndFillRowsAsync()
            Try
                If _isMsgBoxActive Then Return

                ' === ПРОВЕРКА ВХОДНЫХ ПАРАМЕТРОВ ===
                If Not _periodStart.HasValue OrElse Not _periodEnd.HasValue Then
                    SafeShowMessage("Не задан период для отчета!", "Ошибка Таблицы 10")
                    Return
                End If

                Dim parentWin As Window = Window.GetWindow(Me)
                If parentWin Is Nothing Then parentWin = Application.Current.MainWindow

                ' === ПРОВЕРКА СОЕДИНЕНИЯ ДЛЯ ОБОИХ УРОВНЕЙ ===

                ' 1. Центральный уровень (для СЛД)
                If Not Await CentralFetcher.EnsureConnectedAsync() Then
                    SafeShowMessage("Нет соединения с Центральным уровнем КАСАНТ!", "Ошибка", parentWin)
                    Return
                End If

                ' 2. Дорожный уровень (для ТЧЭ) - ВАЖНО!
                If Not Await Fetcher.EnsureConnectedAsync() Then
                    SafeShowMessage("Нет соединения с Дорожным уровнем КАСАНТ!", "Ошибка", parentWin)
                    Return
                End If



                ' === УНИВЕРСАЛЬНОЕ ФОРМИРОВАНИЕ ПЕРИОДОВ ===


                Dim periods As New List(Of ReportPeriod)
                Dim prevYear As Integer = _periodStart.Value.Year - 1

                ' Индекс 0: Тек. год (выбранный пользователем диапазон)
                periods.Add(New ReportPeriod With {
            .DateFrom = _periodStart.Value.Date,
            .DateTo = _periodEnd.Value.Date,
            .Title = $"Тек. год ({_periodStart.Value:dd.MM} - {_periodEnd.Value:dd.MM})"
        })

                ' Индекс 1: Прош. год (весь период) 
                ' ЛОГИКА: Берем полный аналогичный диапазон прошлого года.
                ' Если выбрано 01.01-26.09 -> берем ВЕСЬ диапазон с 01.01 по ПОСЛЕДНИЙ ДЕНЬ СЕНТЯБРЯ прошлого года.
                ' Если выбрано 01.09-26.09 -> берем ВЕСЬ СЕНТЯБРЬ прошлого года.
                Dim prevFullStart As Date = _periodStart.Value.AddYears(-1)
                Dim prevFullEnd As Date = New Date(prevYear, _periodEnd.Value.Month,
                                           DateTime.DaysInMonth(prevYear, _periodEnd.Value.Month))

                periods.Add(New ReportPeriod With {
            .DateFrom = prevFullStart,
            .DateTo = prevFullEnd,
            .Title = "Прош. год (весь период)"
        })

                ' Индекс 2: Прош. год (с нач. периода)
                ' ЛОГИКА: Точная копия выбранного диапазона в прошлом году.
                ' 01.01-26.09 текущего -> 01.01-26.09 прошлого
                Dim prevCumStart As Date = _periodStart.Value.AddYears(-1)
                Dim prevCumEnd As Date = _periodEnd.Value.AddYears(-1)

                ' Защита от високосных годов/коротких месяцев для конца периода
                If prevCumEnd.Day > DateTime.DaysInMonth(prevCumEnd.Year, prevCumEnd.Month) Then
                    prevCumEnd = New Date(prevCumEnd.Year, prevCumEnd.Month,
                                  DateTime.DaysInMonth(prevCumEnd.Year, prevCumEnd.Month))
                End If

                periods.Add(New ReportPeriod With {
            .DateFrom = prevCumStart,
            .DateTo = prevCumEnd,
            .Title = "Прош. год (с нач. периода)"
        })
                ' ============================================================







                '                Dim periods As New List(Of ReportPeriod)
                '                Dim prevYear As Integer = _periodStart.Value.Year - 1

                '                ' Базовая дата начала прошлого года (сдвиг на год назад)
                '                Dim prevBaseDate As Date = _periodStart.Value.AddYears(-1)

                '                ' Длительность выбранного периода в днях
                '                Dim periodDuration As TimeSpan = _periodEnd.Value.Date - _periodStart.Value.Date

                '                ' Индекс 0: Тек. год (выбранный пользователем диапазон)
                '                periods.Add(New ReportPeriod With {
                '            .DateFrom = _periodStart.Value.Date,
                '            .DateTo = _periodEnd.Value.Date,
                '            .Title = $"Тек. год ({_periodStart.Value:dd.MM} - {_periodEnd.Value:dd.MM})"
                '        })




                '                ' Индекс 1: Прош. год (весь период) 
                '                ' Логика: берем ПОЛНЫЙ аналогичный месяц прошлого года, независимо от текущей даты.
                '                ' Пример: Если сейчас 01.09-08.09.2026 -> берем 01.09.2025 - 30.09.2025
                '                Dim prevYear0 As Integer = _periodStart.Value.Year - 1
                '                Dim targetMonth As Integer = _periodStart.Value.Month

                '                ' Начало: 1-е число нужного месяца прошлого года
                '                Dim prevFullStart As New Date(prevYear0, targetMonth, 1)

                '                ' Конец: ПОСЛЕДНИЙ день этого месяца в прошлом году
                '                Dim daysInTargetMonth As Integer = DateTime.DaysInMonth(prevYear0, targetMonth)
                '                Dim prevFullEnd As New Date(prevYear0, targetMonth, daysInTargetMonth)

                '                periods.Add(New ReportPeriod With {
                '    .DateFrom = prevFullStart,
                '    .DateTo = prevFullEnd,
                '    .Title = "Прош. год (весь период)"
                '})



                '                ' Индекс 1: Прош. год (весь период) 
                '                ' Логика: берем ПОЛНЫЙ аналогичный диапазон прошлого года.
                '                ' Пример: 01.04-07.06 текущего -> 01.04-30.06 прошлого (полный апрель, май, июнь)
                '                Dim prevFullStart As Date = _periodStart.Value.AddYears(-1)
                '                Dim prevFullEnd As Date = _periodEnd.Value.AddYears(-1)

                '                ' Корректировка конца периода: если в прошлом году в этом месяце меньше дней, берем последний день того месяца
                '                If prevFullEnd.Day > DateTime.DaysInMonth(prevFullEnd.Year, prevFullEnd.Month) Then
                '                    prevFullEnd = New Date(prevFullEnd.Year, prevFullEnd.Month,
                '                           DateTime.DaysInMonth(prevFullEnd.Year, prevFullEnd.Month))
                '                End If

                '                periods.Add(New ReportPeriod With {
                '    .DateFrom = prevFullStart,
                '    .DateTo = prevFullEnd,
                '    .Title = "Прош. год (весь период)"
                '})





                '        ' Индекс 2: Прош. год (с нач. периода)
                '        ' Логика: начинаем с того же числа/месяца прошлого года, что и старт текущего периода,
                '        ' но заканчиваем ровно через столько же дней, сколько длится текущий период.
                '        ' Например: 01.04-07.06 (67 дней) -> 01.04.прошлого + 67 дней = 07.06.прошлого
                '        Dim prevCumStart As Date = prevBaseDate
                '        Dim prevCumEnd As Date = prevCumStart.AddDays(periodDuration.TotalDays)

                '        periods.Add(New ReportPeriod With {
                '    .DateFrom = prevCumStart,
                '    .DateTo = prevCumEnd,
                '    .Title = "Прош. год (с нач. периода)"
                '})

                ' === ЗАГРУЗКА ДАННЫХ ПО ИНДЕКСАМ ===
                Dim dataCurrent As PeriodData = Await FetchCombinedDataAsync(periods(0))
                Dim dataPrevFull As PeriodData = Await FetchCombinedDataAsync(periods(1))
                Dim dataPrevCum As PeriodData = Await FetchCombinedDataAsync(periods(2))

                BuildGrid(dataCurrent, dataPrevFull, dataPrevCum)

            Catch ex As Exception
                If Not _isMsgBoxActive Then
                    SafeShowMessage(ex.Message, "Ошибка Таблицы 10")
                End If
            End Try
        End Sub




        ''' <summary>
        ''' Безопасный вывод сообщения. Блокирует повторные вызовы до закрытия окна.
        ''' </summary>
        Private Sub SafeShowMessage(message As String, title As String, Optional owner As Window = Nothing)
            If _isMsgBoxActive Then Return

            If owner Is Nothing Then
                owner = Window.GetWindow(Me)
                If owner Is Nothing Then owner = Application.Current.MainWindow
            End If

            _isMsgBoxActive = True
            Try
                ShowMSG(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Error)
            Finally
                _isMsgBoxActive = False
            End Try
        End Sub






        ''' <summary>
        ''' Загружает и объединяет данные ТЧ и СЛД за один период, 
        ''' используя ТОЛЬКО глобальный CentralFetcher
        ''' </summary>
        Private Async Function FetchCombinedDataAsync(period As ReportPeriod) As Task(Of PeriodData)



            If period Is Nothing Then Return New PeriodData With {
                .DepotOts = New Dictionary(Of String, Integer),
                .DepotSld = New Dictionary(Of String, Integer)
            }

            Dim result As New PeriodData With {
                .DepotOts = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase),
                .DepotSld = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
            }

            Try
                ' --- ДАННЫЕ СЛД: используем готовый метод глобального фетчера ---
                Dim urlSld As String = CentralFetcher.BuildReportUrl(period.DateFrom, period.DateTo, 0, 23, 59)
                Dim sldData As Dictionary(Of String, Integer) = Await CentralFetcher.FetchDepotTotalsAsync(urlSld)

                For Each kvp In sldData
                    result.DepotSld(kvp.Key) = kvp.Value
                Next

                ' --- ДАННЫЕ ТЧ: используем НОВЫЙ публичный метод глобального фетчера ---
                Dim urlTch As String = BuildTchReportUrl(period.DateFrom, period.DateTo)
                'Dim urlTch As String = KasReport341Module.CentralFetcher.BuildTchReportUrl(period.DateFrom, period.DateTo)


                Dim response As HttpResponseMessage = Await Fetcher.HttpClient.GetAsync(urlTch)

                If response.IsSuccessStatusCode Then
                    Dim bytes As Byte() = Await response.Content.ReadAsByteArrayAsync()
                    Dim html As String = Encoding.GetEncoding("windows-1251").GetString(bytes)
                    ParseTchData(html, result.DepotOts)

                End If

            Catch ex As Exception
                CentralFetcher.LogWrite($"Ошибка загрузки данных за {period.Title}: {ex.Message}")
            End Try

            Return result
        End Function

        ''' <summary>
        ''' Парсит HTML отчета ТЧ и заполняет словарь ОТС
        ''' Адаптируй селекторы под реальную верстку отчета ТЧ!
        ''' </summary>
        Private Sub ParseTchData(html As String, ByRef otsDict As Dictionary(Of String, Integer))


            Try
                Dim doc As New HtmlAgilityPack.HtmlDocument()
                doc.LoadHtml(html)

                Dim rows = doc.DocumentNode.SelectNodes("//tr")
                If rows Is Nothing Then Return

                For Each row In rows
                    Dim cells = row.SelectNodes("./td")

                    ' Таблица ТЧ имеет ровно 6 колонок. Проверяем это для надежности
                    If cells IsNot Nothing AndAlso cells.Count = 6 Then

                        ' Колонка 0: Название депо (ТЧЭ-X КРАС)
                        Dim depotName As String = CleanText(cells(0).InnerText)

                        ' Пропускаем служебные строки
                        If String.IsNullOrWhiteSpace(depotName) OrElse
                   depotName.Contains("Наименование") OrElse
                   depotName.ToUpper() = "ИТОГО" OrElse
                   depotName.ToUpper() = "ВСЕГО" Then
                            Continue For
                        End If

                        ' Колонка 1: Значение "Всего" (ссылка с числом внутри)
                        ' Берем InnerText ячейки, чистим от пробелов
                        Dim valText As String = CleanText(cells(1).InnerText)
                        Dim val As Integer = 0
                        Integer.TryParse(valText, val)

                        ' Сохраняем в словарь под оригинальным названием из отчета ТЧ
                        otsDict(depotName) = val

                        CentralFetcher.LogWrite($" ТЧ: {depotName} = {val}")
                    End If
                Next

            Catch ex As Exception
                CentralFetcher.LogWrite($"Ошибка парсинга ТЧ: {ex.Message}")
            End Try


        End Sub





        Private Sub BuildGrid(d26 As PeriodData, d25F As PeriodData, d25C As PeriodData)
            ' === СПИСОК ДЛЯ ПОИСКА ДАННЫХ СЛД (из отчета СЛД) ===
            Dim depoSLD As New List(Of (DisplayName As String, Keywords As String())) From {
        ("Боготол", {"Богото"}),
        ("Красноярск", {"Красноярс"}),
        ("Иланская", {"Иланск"}),
        ("Ачинск", {"Ачинс"}),
        ("Абакан", {"Абака"})
    }

            ' === СПИСОК ДЛЯ ПОИСКА ДАННЫХ ТЧЭ (из отчета Дирекции Т) ===
            Dim depoTCH As New List(Of (DisplayName As String, Keywords As String())) From {
        ("Боготол", {"ТЧЭ-1"}),
        ("Красноярск", {"ТЧЭ-2"}),
        ("Иланская", {"ТЧЭ-3"}),
        ("Ачинск", {"ТЧЭ-5"}),
        ("Абакан", {"ТЧЭ-7"})
    }


            ' === ИСПРАВЛЕНИЕ: Суммы по Т теперь Integer ===
            Dim sumY26T As Integer = 0      ' Было: sumY26T_B As Double
            Dim sumY26S_O As Integer = 0, sumY26S_B As Double = 0
            Dim sumY26Total As Double = 0

            Dim sumY25FT As Integer = 0     ' Было: sumY25FT_B As Double
            Dim sumY25FS_O As Integer = 0, sumY25FS_B As Double = 0
            Dim sumY25FTotal As Double = 0

            Dim sumY25CT As Integer = 0     ' Было: sumY25CT_B As Double
            Dim sumY25CS_O As Integer = 0, sumY25CS_B As Double = 0
            Dim sumY25CTotal As Double = 0

            For i As Integer = 0 To depoSLD.Count - 1
                Dim depotName As String = depoSLD(i).DisplayName
                Dim rIdx As Integer = MainGrid.RowDefinitions.Count
                MainGrid.RowDefinitions.Add(New RowDefinition With {.Height = GridLength.Auto})

                AddCell(rIdx, 0, depotName, "HeaderBorder", "HeaderTextStyle", isBold:=True)

                ' --- Ищем значения по соответствующим ключам ---
                Dim v26S As Integer = GetVal(d26?.DepotSld, depoSLD(i).Keywords)
                Dim v25FS As Integer = GetVal(d25F?.DepotSld, depoSLD(i).Keywords)
                Dim v25CS As Integer = GetVal(d25C?.DepotSld, depoSLD(i).Keywords)

                Dim v26O As Integer = GetVal(d26?.DepotOts, depoTCH(i).Keywords)
                Dim v25FO As Integer = GetVal(d25F?.DepotOts, depoTCH(i).Keywords)
                Dim v25CO As Integer = GetVal(d25C?.DepotOts, depoTCH(i).Keywords)

                ' --- Заполнение строк (индексы колонок под новый XAML) ---
                ' Передаем Integer вместо Double для сумм по Т
                FillRowValues(rIdx, 1, v26O, v26S, sumY26T, sumY26S_O, sumY26S_B, sumY26Total)
                FillRowValues(rIdx, 5, v25FO, v25FS, sumY25FT, sumY25FS_O, sumY25FS_B, sumY25FTotal)
                FillRowValues(rIdx, 9, v25CO, v25CS, sumY25CT, sumY25CS_O, sumY25CS_B, sumY25CTotal)
            Next

            ' Итоговая строка
            Dim tIdx As Integer = MainGrid.RowDefinitions.Count
            MainGrid.RowDefinitions.Add(New RowDefinition With {.Height = GridLength.Auto})
            AddCell(tIdx, 0, "ФАКТ:", "TotalBorder", "HeaderTextStyle", isBold:=True)

            ' Вызываем обновленную AddTotalCells с Integer для Т
            AddTotalCells(tIdx, 1, sumY26T, sumY26S_O, sumY26S_B, sumY26Total)
            AddTotalCells(tIdx, 5, sumY25FT, sumY25FS_O, sumY25FS_B, sumY25FTotal)
            AddTotalCells(tIdx, 9, sumY25CT, sumY25CS_O, sumY25CS_B, sumY25CTotal)







        End Sub


        Private Sub FillRowValues(row As Integer, startCol As Integer,
                          otsTch As Integer, otsSld As Integer,
                          ByRef sumTB As Double, ByRef sumSO As Integer, ByRef sumSB As Double, ByRef sumTotal As Double)


            ' Расчет баллов (Double) ТОЛЬКО там, где это реально нужно
            Dim bS As Double = Math.Round(otsSld * 0.5, 1)
            Dim bTot As Double = Math.Round(CDbl(otsTch) + bS, 1)

            ' ПЕРЕДАЧА В ЯЧЕЙКИ:
            ' otsTch -> INTEGER (черный, без дробей)
            ' otsSld -> INTEGER (черный, без дробей)
            ' bS     -> DOUBLE  (КРАСНЫЙ, F1)
            ' bTot   -> DOUBLE  (КРАСНЫЙ, F1)
            AddCellValue(row, startCol, otsTch)          ' T Баллы/ОТС (Черный, Integer)
            AddCellValue(row, startCol + 1, otsSld)      ' SLD ОТС (Черный, Integer)
            AddCellValue(row, startCol + 2, bS, True)    ' SLD Баллы (Красный, Double)
            AddCellValue(row, startCol + 3, bTot, True)  ' Всего (Красный, Double)

            ' Накопление итогов
            sumTB += otsTch
            sumSO += otsSld
            sumSB += bS
            sumTotal += bTot




        End Sub




        Private Sub AddTotalCells(row As Integer, startCol As Integer,
                          sumT As Integer,            ' <--- ТЕПЕРЬ INTEGER
                          sumSO As Integer,
                          sumSB As Double,
                          sumTotal As Double)

            ' sumT.ToString() даст чистое целое число без ".0"
            AddCell(row, startCol, sumT.ToString(), "TotalBorder", "CellStyle", isBold:=True)
            AddCell(row, startCol + 1, sumSO.ToString(), "TotalBorder", "CellStyle", isBold:=True)
            AddCell(row, startCol + 2, sumSB.ToString("F1"), "TotalBorder", "CellStyle", isBold:=True, isRed:=True)
            AddCell(row, startCol + 3, sumTotal.ToString("F1"), "TotalBorder", "CellStyle", isBold:=True, isRed:=True)
        End Sub






        Private Sub AddCell(row As Integer, col As Integer, text As String,
                    borderStyleKey As String, textStyleKey As String,
                    Optional isBold As Boolean = False,
                    Optional isRed As Boolean = False)
            Dim b As New Border()
            b.Style = CType(FindResource(borderStyleKey), Style)

            Dim t As New TextBlock()
            t.Style = CType(FindResource(textStyleKey), Style)
            t.Text = text

            If isBold Then t.FontWeight = FontWeights.Bold
            If isRed Then t.Foreground = Brushes.Red  ' <--- КРАСНЫЙ ЦВЕТ ДЛЯ БАЛЛОВ

            ' Подписка на события только для ячеек данных (не заголовков и не итогов)
            If row > 2 AndAlso Not text.StartsWith("ФАКТ") Then
                t.Background = Brushes.Transparent
                AddHandler t.MouseEnter, AddressOf Cell_MouseEnterForSelection
                AddHandler t.MouseLeave, AddressOf Cell_MouseLeaveForSelection
                AddHandler t.PreviewMouseLeftButtonDown, AddressOf Cell_PreviewMouseDownForSelection
                AddHandler t.MouseRightButtonDown, AddressOf CopyColumnToClipboard_Click
            End If

            b.Child = t
            Grid.SetRow(b, row)
            Grid.SetColumn(b, col)
            MainGrid.Children.Add(b)
        End Sub








        Private Sub AddCellValue(row As Integer, col As Integer, value As Object, Optional isRed As Boolean = False)
            Dim txt As String = ""
            If TypeOf value Is Integer Then
                Dim v As Integer = CInt(value)
                txt = If(v > 0, v.ToString(), "")
            ElseIf TypeOf value Is Double Then
                Dim v As Double = CDbl(value)
                txt = If(v > 0, v.ToString("F1"), "")
            End If

            ' Если значение есть и нужен красный цвет — используем специальный стиль или меняем Foreground
            If Not String.IsNullOrEmpty(txt) AndAlso isRed Then
                AddCell(row, col, txt, "CellBorder", "CellStyle", isBold:=False, isRed:=True)
            Else
                AddCell(row, col, txt, "CellBorder", "CellStyle")
            End If
        End Sub




        Private Function GetVal(dict As Dictionary(Of String, Integer), keywords As String()) As Integer
            If dict Is Nothing Then Return 0
            For Each kvp In dict
                For Each kw In keywords
                    If kvp.Key.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0 Then Return kvp.Value
                Next
            Next
            Return 0
        End Function

        Private Function CleanText(text As String) As String
            If String.IsNullOrWhiteSpace(text) Then Return ""
            text = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", " ")
            text = text.Replace("&nbsp;", " ").Replace("&amp;", "&")
            Return System.Text.RegularExpressions.Regex.Replace(text, "\s+", " ").Trim()
        End Function

        ' ==================== SELECTION & CLIPBOARD ====================

        Private _selectedCells As New List(Of TextBlock)()
        Private ReadOnly SelectionBrush As New SolidColorBrush(Color.FromArgb(60, 128, 128, 128))
        Private ReadOnly DefaultBrush As New SolidColorBrush(Colors.Transparent)


        Private Sub HighlightColumn(col As Integer)
            ' Сброс предыдущего выделения
            For Each c In _selectedCells : c.Background = DefaultBrush : Next
            _selectedCells.Clear()

            ' Проверка границ для новой структуры (0-12)
            If col < 0 OrElse col > 12 Then Return

            ' Проходим по строкам данных (начиная с 3, т.к. 0-2 заняты заголовками)
            For r As Integer = 3 To MainGrid.RowDefinitions.Count - 1
                Dim border As Border = FindChildByGridCoords(MainGrid, r, col)
                If border IsNot Nothing Then
                    Dim tb As TextBlock = TryCast(border.Child, TextBlock)
                    If tb IsNot Nothing Then
                        tb.Background = SelectionBrush
                        _selectedCells.Add(tb)
                    End If
                End If
            Next

            ' Сортировка по порядку строк для корректного копирования
            _selectedCells.Sort(Function(a, b) Grid.GetRow(TryCast(a.Parent, Border)).CompareTo(Grid.GetRow(TryCast(b.Parent, Border))))
        End Sub



        Private Function FindChildByGridCoords(grid As Grid, row As Integer, col As Integer) As Border
            For Each child In grid.Children
                If TypeOf child Is Border Then
                    Dim b As Border = CType(child, Border)
                    If Grid.GetRow(b) = row AndAlso Grid.GetColumn(b) = col Then Return b
                End If
            Next
            Return Nothing
        End Function



        Private Sub Cell_MouseEnterForSelection(sender As Object, e As MouseEventArgs)
            Dim tb As TextBlock = TryCast(sender, TextBlock)
            If tb Is Nothing Then Return

            Dim parentBorder As Border = TryCast(tb.Parent, Border)
            If parentBorder Is Nothing Then Return

            Dim col As Integer = Grid.GetColumn(parentBorder)

            ' РАЗРЕШЕННЫЕ КОЛОНКИ ДЛЯ ПОДСВЕТКИ И КОПИРОВАНИЯ:
            ' 1, 5, 9 - Т Баллы (фактически ТЧЭ ОТС)
            ' 2, 6, 10 - СЛД ОТС
            Dim allowedCols As Integer() = {1, 2, 5, 6, 9, 10}

            If Not allowedCols.Contains(col) Then Return

            HighlightColumn(col)
        End Sub


        Private Sub Cell_MouseLeaveForSelection(sender As Object, e As MouseEventArgs)
            If _selectedCells.Count = 0 Then Return
            Dim mousePos As Point = Mouse.GetPosition(MainGrid)
            Dim firstCell As TextBlock = _selectedCells.First()
            Dim lastCell As TextBlock = _selectedCells.Last()
            Dim pFirst As Border = TryCast(firstCell.Parent, Border)
            Dim pLast As Border = TryCast(lastCell.Parent, Border)
            If pFirst Is Nothing OrElse pLast Is Nothing Then Return

            Dim topLeft As Point = pFirst.TranslatePoint(New Point(0, 0), MainGrid)
            Dim bottomRight As Point = pLast.TranslatePoint(New Point(pLast.ActualWidth, pLast.ActualHeight), MainGrid)
            Dim rect As New Rect(topLeft, bottomRight)

            If Not rect.Contains(mousePos) Then
                For Each c In _selectedCells : c.Background = DefaultBrush : Next
                _selectedCells.Clear()
            End If
        End Sub

        Private Sub Cell_PreviewMouseDownForSelection(sender As Object, e As MouseButtonEventArgs)
            Dim tb As TextBlock = TryCast(sender, TextBlock)
            If tb Is Nothing Then Return

            Dim parentBorder As Border = TryCast(tb.Parent, Border)
            If parentBorder Is Nothing Then Return

            Dim col As Integer = Grid.GetColumn(parentBorder)

            ' ТОТ ЖЕ ФИЛЬТР, ЧТО И В MOUSEENTER:
            ' Разрешаем выделение кликом только для колонок с количеством
            Dim allowedCols As Integer() = {1, 2, 5, 6, 9, 10}

            If Not allowedCols.Contains(col) Then Return

            HighlightColumn(col)
        End Sub



        Private Sub CopyColumnToClipboard_Click(sender As Object, e As MouseButtonEventArgs)
            If _selectedCells.Count = 0 Then
                SafeShowMessage("Выделите столбец мышью!", "Внимание")
                'MessageBox.Show("Выделите столбец мышью!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning)
                e.Handled = True
                Return
            End If

            ' Определяем индекс выделенной колонки
            Dim colIndex As Integer = Grid.GetColumn(TryCast(_selectedCells.First().Parent, Border))

            ' РАЗРЕШЕННЫЕ КОЛОНКИ ДЛЯ КОПИРОВАНИЯ (только количество):
            ' 1, 5, 9 - Т Баллы (ТЧЭ ОТС)
            ' 2, 6, 10 - СЛД ОТС
            ' ЗАПРЕЩЕНО: 3, 7, 11 (СЛД Баллы) и 4, 8, 12 (Всего баллов)
            Dim allowedCols As Integer() = {1, 2, 5, 6, 9, 10}

            If Not allowedCols.Contains(colIndex) Then
                SafeShowMessage("Копирование доступно только для столбцов с количеством отказов!", "Внимание")

                'MessageBox.Show("Копирование доступно только для столбцов с количеством отказов!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information)
                e.Handled = True
                Return
            End If

            ' Формируем текст для буфера обмена (только значения ячеек)
            Dim sb As New System.Text.StringBuilder()
            For Each cell In _selectedCells
                sb.AppendLine(cell.Text)
            Next

            Clipboard.SetText(sb.ToString())
            e.Handled = True
        End Sub




    End Class

End Namespace