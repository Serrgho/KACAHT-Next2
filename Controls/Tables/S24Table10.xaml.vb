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

                Dim parentWin As Window = Window.GetWindow(Me)
                If parentWin Is Nothing Then parentWin = Application.Current.MainWindow

                If Not Await KasReport341Module.CentralFetcher.EnsureConnectedAsync() Then
                    SafeShowMessage("Нет соединения с КАСАНТ!", "Ошибка", parentWin)
                    Return
                End If

                ' === ИСПРАВЛЕНИЕ: используем переданные даты, а не GetReportPeriods ===
                Dim periods As New List(Of ReportPeriod)

                If _periodStart.HasValue AndAlso _periodEnd.HasValue Then
                    periods.Add(New ReportPeriod With {
                        .DateFrom = _periodStart.Value.Date,
                        .DateTo = _periodEnd.Value.Date,
                        .Title = $"{_periodStart.Value:dd.MM.yyyy} - {_periodEnd.Value:dd.MM.yyyy}",
                        .IsCurrentYear = (_periodStart.Value.Year = Date.Today.Year),
                        .PeriodNumber = 1
                    })
                Else
                    ' Фолбэк: если даты не переданы, берем как раньше
                    periods = KasReport341Module.CentralFetcher.GetReportPeriods()
                End If
                ' ============================================================

                Dim p2026 = periods.FirstOrDefault(Function(p) p.IsCurrentYear AndAlso p.PeriodNumber = 1)
                Dim p2025Full = periods.FirstOrDefault(Function(p) Not p.IsCurrentYear AndAlso p.PeriodNumber = 1)
                Dim p2025Cum = If(Date.Today.Day < 15,
                                  periods.FirstOrDefault(Function(p) Not p.IsCurrentYear AndAlso p.PeriodNumber = 2),
                                  p2025Full)

                Dim data2026 As PeriodData = Await FetchCombinedDataAsync(p2026)
                Dim data2025Full As PeriodData = Await FetchCombinedDataAsync(p2025Full)
                Dim data2025Cum As PeriodData = Await FetchCombinedDataAsync(p2025Cum)

                BuildGrid(data2026, data2025Full, data2025Cum)

            Catch ex As Exception
                If Not _isMsgBoxActive Then
                    SafeShowMessage(ex.Message, "Ошибка Таблицы 10")
                End If
            End Try











            'Try
            '    ' Если сообщение уже висит на экране — выходим молча
            '    If _isMsgBoxActive Then Return

            '    Dim parentWin As Window = Window.GetWindow(Me)
            '    If parentWin Is Nothing Then parentWin = Application.Current.MainWindow

            '    If Not Await KasReport341Module.CentralFetcher.EnsureConnectedAsync() Then
            '        SafeShowMessage("Нет соединения с КАСАНТ!", "Ошибка", parentWin)
            '        Return
            '    End If

            '    Dim periods As List(Of ReportPeriod) = KasReport341Module.CentralFetcher.GetReportPeriods()

            '    ' Определяем нужные периоды
            '    Dim p2026 = periods.FirstOrDefault(Function(p) p.IsCurrentYear AndAlso p.PeriodNumber = 1)
            '    Dim p2025Full = periods.FirstOrDefault(Function(p) Not p.IsCurrentYear AndAlso p.PeriodNumber = 1)
            '    Dim p2025Cum = If(Date.Today.Day < 15,
            '                      periods.FirstOrDefault(Function(p) Not p.IsCurrentYear AndAlso p.PeriodNumber = 2),
            '                      p2025Full)

            '    ' Загружаем данные
            '    Dim data2026 As PeriodData = Await FetchCombinedDataAsync(p2026)
            '    Dim data2025Full As PeriodData = Await FetchCombinedDataAsync(p2025Full)
            '    Dim data2025Cum As PeriodData = Await FetchCombinedDataAsync(p2025Cum)

            '    BuildGrid(data2026, data2025Full, data2025Cum)

            'Catch ex As Exception
            '    Dim parentWin As Window = Window.GetWindow(Me)
            '    If parentWin Is Nothing Then parentWin = Application.Current.MainWindow

            '    ' Показываем ошибку только если проблема НЕ в отсутствии соединения
            '    If Not _isMsgBoxActive Then
            '        SafeShowMessage(ex.Message, "Ошибка Таблицы 10")
            '    End If
            'End Try
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
                Dim urlSld As String = KasReport341Module.CentralFetcher.BuildReportUrl(period.DateFrom, period.DateTo, 0, 23, 59)
                Dim sldData As Dictionary(Of String, Integer) = Await KasReport341Module.CentralFetcher.FetchDepotTotalsAsync(urlSld)

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
                KasReport341Module.CentralFetcher.LogWrite($"Ошибка загрузки данных за {period.Title}: {ex.Message}")
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

                        KasReport341Module.CentralFetcher.LogWrite($" ТЧ: {depotName} = {val}")
                    End If
                Next

            Catch ex As Exception
                KasReport341Module.CentralFetcher.LogWrite($"Ошибка парсинга ТЧ: {ex.Message}")
            End Try







            'Try
            '    Dim doc As New HtmlAgilityPack.HtmlDocument()
            '    doc.LoadHtml(html)

            '    Dim rows = doc.DocumentNode.SelectNodes("//tr")
            '    If rows Is Nothing Then Return

            '    For Each row In rows
            '        Dim cells = row.SelectNodes("./td")
            '        If cells IsNot Nothing AndAlso cells.Count >= 2 Then
            '            Dim name As String = CleanText(cells(0).InnerText)

            '            ' Фильтруем нужные депо
            '            If String.IsNullOrWhiteSpace(name) OrElse
            '               name.Contains("Наименование") OrElse
            '               name.ToUpper() = "ВСЕГО" Then Continue For

            '            ' Ищем значение ОТС (обычно во 2-й колонке, индекс 1)
            '            Dim val As Integer = 0
            '            Integer.TryParse(CleanText(cells(1).InnerText), val)

            '            otsDict(name) = val
            '        End If
            '    Next
            'Catch ex As Exception
            '    KasReport341Module.CentralFetcher.LogWrite($"Ошибка парсинга ТЧ: {ex.Message}")
            'End Try
        End Sub

        ' ==================== ПОСТРОЕНИЕ ТАБЛИЦЫ ====================
        ' (Остальной код BuildGrid, AddHeaderRow, Selection и Clipboard остается БЕЗ ИЗМЕНЕНИЙ)

        Private Sub BuildGrid(d26 As PeriodData, d25F As PeriodData, d25C As PeriodData)
            Dim depots As New List(Of (DisplayName As String, Keywords As String())) From {
                ("Боготол", {"ТЧЭ-1"}),      ' Было: {"Богото"}
                ("Красноярск", {"ТЧЭ-2"}),   ' Было: {"Красноярс"}
                ("Иланская", {"ТЧЭ-3"}),     ' Было: {"Иланск"}
                ("Ачинск", {"ТЧЭ-5"}),       ' Было: {"Ачинс"}
                ("Абакан", {"ТЧЭ-7"})        ' Было: {"Абака"}
            }

            AddHeaderRow()

            Dim sumY26T_O As Integer = 0, sumY26T_B As Double = 0
            Dim sumY26S_O As Integer = 0, sumY26S_B As Double = 0
            Dim sumY25FT_O As Integer = 0, sumY25FT_B As Double = 0
            Dim sumY25FS_O As Integer = 0, sumY25FS_B As Double = 0
            Dim sumY25CT_O As Integer = 0, sumY25CT_B As Double = 0
            Dim sumY25CS_O As Integer = 0, sumY25CS_B As Double = 0

            For i As Integer = 0 To depots.Count - 1
                Dim depot = depots(i)
                Dim rIdx As Integer = MainGrid.RowDefinitions.Count
                MainGrid.RowDefinitions.Add(New RowDefinition With {.Height = GridLength.Auto})

                AddCell(rIdx, 0, depot.DisplayName, "HeaderBorder", "HeaderTextStyle", isBold:=True)

                ' --- 2026 ---
                Dim v26O As Integer = GetVal(d26?.DepotOts, depot.Keywords)
                Dim v26S As Integer = GetVal(d26?.DepotSld, depot.Keywords)
                FillRowValues(rIdx, 1, v26O, v26S, sumY26T_O, sumY26T_B, sumY26S_O, sumY26S_B)
                UpdateSums(v26O, v26S, sumY26T_O, sumY26T_B, sumY26S_O, sumY26S_B)

                ' --- 2025 Full ---
                Dim v25FO As Integer = GetVal(d25F?.DepotOts, depot.Keywords)
                Dim v25FS As Integer = GetVal(d25F?.DepotSld, depot.Keywords)
                FillRowValues(rIdx, 6, v25FO, v25FS, sumY25FT_O, sumY25FT_B, sumY25FS_O, sumY25FS_B)
                UpdateSums(v25FO, v25FS, sumY25FT_O, sumY25FT_B, sumY25FS_O, sumY25FS_B)

                ' --- 2025 Cum ---
                Dim v25CO As Integer = GetVal(d25C?.DepotOts, depot.Keywords)
                Dim v25CS As Integer = GetVal(d25C?.DepotSld, depot.Keywords)
                FillRowValues(rIdx, 11, v25CO, v25CS, sumY25CT_O, sumY25CT_B, sumY25CS_O, sumY25CS_B)
                UpdateSums(v25CO, v25CS, sumY25CT_O, sumY25CT_B, sumY25CS_O, sumY25CS_B)
            Next

            ' Итоговая строка
            Dim tIdx As Integer = MainGrid.RowDefinitions.Count
            MainGrid.RowDefinitions.Add(New RowDefinition With {.Height = GridLength.Auto})
            AddCell(tIdx, 0, "ФАКТ:", "TotalBorder", "HeaderTextStyle", isBold:=True)

            AddTotalCells(tIdx, 1, sumY26T_O, sumY26T_B, sumY26S_O, sumY26S_B)
            AddTotalCells(tIdx, 6, sumY25FT_O, sumY25FT_B, sumY25FS_O, sumY25FS_B)
            AddTotalCells(tIdx, 11, sumY25CT_O, sumY25CT_B, sumY25CS_O, sumY25CS_B)
        End Sub

        Private Sub FillRowValues(row As Integer, startCol As Integer,
                                  ots As Integer, sld As Integer,
                                  ByRef sumTO As Integer, ByRef sumTB As Double,
                                  ByRef sumSO As Integer, ByRef sumSB As Double)
            Dim bT As Double = Math.Round(ots * 0.5, 1)
            Dim bS As Double = Math.Round(sld * 0.5, 1)
            Dim bTot As Double = Math.Round((ots + sld) * 0.5, 1)

            AddCellValue(row, startCol, ots)       ' Т ОТС
            AddCellValue(row, startCol + 1, bT)    ' Т Баллы
            AddCellValue(row, startCol + 2, sld)   ' СЛД ОТС
            AddCellValue(row, startCol + 3, bS)    ' СЛД Баллы
            AddCellValue(row, startCol + 4, bTot)  ' Всего
        End Sub

        Private Sub UpdateSums(ots As Integer, sld As Integer,
                               ByRef sumTO As Integer, ByRef sumTB As Double,
                               ByRef sumSO As Integer, ByRef sumSB As Double)
            sumTO += ots : sumTB += Math.Round(ots * 0.5, 1)
            sumSO += sld : sumSB += Math.Round(sld * 0.5, 1)
        End Sub

        Private Sub AddTotalCells(row As Integer, startCol As Integer,
                                  sumTO As Integer, sumTB As Double,
                                  sumSO As Integer, sumSB As Double)
            Dim bTotT As Double = Math.Round(sumTB, 1)
            Dim bTotS As Double = Math.Round(sumSB, 1)
            Dim bGrand As Double = Math.Round(sumTB + sumSB, 1)

            AddCell(row, startCol, sumTO.ToString(), "TotalBorder", "CellStyle", isBold:=True)
            AddCell(row, startCol + 1, bTotT.ToString("F1"), "TotalBorder", "CellStyle", isBold:=True)
            AddCell(row, startCol + 2, sumSO.ToString(), "TotalBorder", "CellStyle", isBold:=True)
            AddCell(row, startCol + 3, bTotS.ToString("F1"), "TotalBorder", "CellStyle", isBold:=True)
            AddCell(row, startCol + 4, bGrand.ToString("F1"), "TotalBorder", "CellStyle", isBold:=True)
        End Sub

        ' ==================== ЗАГОЛОВКИ ====================

        Private Sub AddHeaderRow()
            AddMergedHeader(0, 1, 5, "2026 год")
            AddMergedHeader(0, 6, 10, "2025 год (весь период)")
            AddMergedHeader(0, 11, 15, "2025 год (нараст. итогом)")

            Dim offsets As Integer() = {1, 6, 11}
            For Each off In offsets
                AddMergedHeader(1, off, off + 1, "Т")
                AddMergedHeader(1, off + 2, off + 3, "СЛД")
                AddCell(1, off + 4, "Всего" & vbCrLf & "баллов", "HeaderBorder", "HeaderTextStyle")

                AddCell(2, off, "ОТС", "HeaderBorder", "HeaderTextStyle")
                AddCell(2, off + 1, "Баллы", "HeaderBorder", "HeaderTextStyle")
                AddCell(2, off + 2, "ОТС", "HeaderBorder", "HeaderTextStyle")
                AddCell(2, off + 3, "Баллы", "HeaderBorder", "HeaderTextStyle")
            Next
        End Sub

        Private Sub AddMergedHeader(row As Integer, fromCol As Integer, toCol As Integer, text As String)
            Dim b As New Border()
            b.Style = CType(FindResource("HeaderBorder"), Style)
            b.SetValue(Grid.ColumnSpanProperty, toCol - fromCol + 1)
            Dim t As New TextBlock()
            t.Style = CType(FindResource("HeaderTextStyle"), Style)
            t.Text = text
            b.Child = t
            Grid.SetRow(b, row)
            Grid.SetColumn(b, fromCol)
            MainGrid.Children.Add(b)
        End Sub

        Private Sub AddCell(row As Integer, col As Integer, text As String,
                            borderStyleKey As String, textStyleKey As String,
                            Optional isBold As Boolean = False)
            Dim b As New Border()
            b.Style = CType(FindResource(borderStyleKey), Style)
            Dim t As New TextBlock()
            t.Style = CType(FindResource(textStyleKey), Style)
            t.Text = text
            If isBold Then t.FontWeight = FontWeights.Bold

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

        Private Sub AddCellValue(row As Integer, col As Integer, value As Object)
            Dim txt As String = ""
            If TypeOf value Is Integer Then
                Dim v As Integer = CInt(value)
                txt = If(v > 0, v.ToString(), "")
            ElseIf TypeOf value Is Double Then
                Dim v As Double = CDbl(value)
                txt = If(v > 0, v.ToString("F1"), "")
            End If
            AddCell(row, col, txt, "CellBorder", "CellStyle")
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
            For Each c In _selectedCells : c.Background = DefaultBrush : Next
            _selectedCells.Clear()
            If col < 1 OrElse col > 15 Then Return

            For r As Integer = 3 To MainGrid.RowDefinitions.Count - 2
                Dim border As Border = FindChildByGridCoords(MainGrid, r, col)
                If border IsNot Nothing Then
                    Dim tb As TextBlock = TryCast(border.Child, TextBlock)
                    If tb IsNot Nothing Then
                        tb.Background = SelectionBrush
                        _selectedCells.Add(tb)
                    End If
                End If
            Next
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
            HighlightColumn(Grid.GetColumn(parentBorder))
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
            HighlightColumn(Grid.GetColumn(parentBorder))
        End Sub

        Private Sub CopyColumnToClipboard_Click(sender As Object, e As MouseButtonEventArgs)
            If _selectedCells.Count = 0 Then
                MessageBox.Show("Выделите столбец мышью!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning)
                e.Handled = True
                Return
            End If
            Dim sb As New System.Text.StringBuilder()
            For i As Integer = 0 To _selectedCells.Count - 1
                sb.Append(_selectedCells(i).Text & vbTab)
                If i < _selectedCells.Count - 1 Then sb.Append(vbCrLf)
            Next
            Clipboard.SetText(sb.ToString())
            e.Handled = True
        End Sub

    End Class

End Namespace