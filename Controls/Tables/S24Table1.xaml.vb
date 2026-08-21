

Imports System.Linq
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Documents
Imports System.Windows.Input
Imports System.Windows.Media

Namespace Kas


    Public Class S24Table1

        ' ==================== ПОЛЯ ====================

        Private _currentYearOtkazy As List(Of Otkaz)
        Private _previousYearOtkazy As List(Of Otkaz)
        Private _previousYearOtkazyRaw As List(Of Otkaz)  ' ← Исходный список прошлого года (без фильтрации)
        Private _periodStart As Date?
        Private _periodEnd As Date?                        ' ← Запоминаем дату конца периода
        Private RowCellsMap As New Dictionary(Of Integer, TextBlock())

        ' ==================== КОНСТРУКТОРЫ ====================

        Public Sub New()
            InitializeComponent()
        End Sub

        Public Sub New(
        currentYearOtkazy As List(Of Otkaz),
        previousYearOtkazy As List(Of Otkaz),
        Optional periodStart As Date? = Nothing,
        Optional periodEnd As Date? = Nothing
    )
            InitializeComponent()
            ' Сохраняем дату конца периода (для строки 10)
            _periodStart = periodStart  ' ← ДОБАВИТЬ
            _periodEnd = periodEnd

            ' Сохраняем исходный список прошлого года (для строки 10)
            _previousYearOtkazyRaw = previousYearOtkazy

            ' ===== ФИЛЬТРАЦИЯ ТЕКУЩЕГО ГОДА =====
            If periodStart IsNot Nothing OrElse periodEnd IsNot Nothing Then
                Dim nextDayAfterEnd = If(periodEnd.HasValue,
                         periodEnd.Value.Date.AddDays(1),
                         Date.MaxValue)
                'Dim nextDayAfterEnd = If(periodEnd, Date.MaxValue).Date.AddDays(1)

                _currentYearOtkazy = currentYearOtkazy.Where(Function(o)
                                                                 Dim d = o.Nach.Date
                                                                 If periodStart IsNot Nothing AndAlso d < periodStart.Value.Date Then Return False
                                                                 If d >= nextDayAfterEnd Then Return False
                                                                 Return True
                                                             End Function).ToList()
            Else
                _currentYearOtkazy = currentYearOtkazy
            End If

            ' ===== ФИЛЬТРАЦИЯ ПРОШЛОГО ГОДА =====

            If periodStart IsNot Nothing OrElse periodEnd IsNot Nothing Then

                ' прошлый период: ручной из настроек (если попап ставил) или авто-сдвиг
                Dim oldP = GetPrevPeriod(If(periodStart, periodEnd).Value.Date,
                                         If(periodEnd, periodStart).Value.Date,
                                         previousYearOtkazy)

                Dim nextDayAfterOldEnd = oldP.End.Date.AddDays(1)

                _previousYearOtkazy = previousYearOtkazy.Where(Function(o)
                                                                   Dim d = o.Nach.Date
                                                                   If d < oldP.Start.Date Then Return False
                                                                   If d >= nextDayAfterOldEnd Then Return False
                                                                   Return True
                                                               End Function).ToList()
            Else
                _previousYearOtkazy = previousYearOtkazy
            End If





        End Sub

        ' ==================== ИНИЦИАЛИЗАЦИЯ ====================

        Private Sub InitializeRowCells()
            ' Полные строки (3, 4) — 20 ячеек
            RowCellsMap(3) = {
            R3C3, R3C4, R3C5, R3C6,
            R3C7, R3C8, R3C9, R3C10,
            R3C11, R3C12, R3C13, R3C14,
            R3C15, R3C16, R3C17, R3C18,
            R3C19, R3C20, R3C21, R3C22
        }

            RowCellsMap(4) = {
            R4C3, R4C4, R4C5, R4C6,
            R4C7, R4C8, R4C9, R4C10,
            R4C11, R4C12, R4C13, R4C14,
            R4C15, R4C16, R4C17, R4C18,
            R4C19, R4C20, R4C21, R4C22
        }

            ' Merged строки (5-10) — 15 ячеек
            RowCellsMap(5) = {
            R5C3, R5C4, R5C5,
            R5C6, R5C7, R5C8,
            R5C9, R5C10, R5C11,
            R5C12, R5C13, R5C14,
            R5C15, R5C16, R5C17
        }

            RowCellsMap(6) = {
            R6C3, R6C4, R6C5,
            R6C6, R6C7, R6C8,
            R6C9, R6C10, R6C11,
            R6C12, R6C13, R6C14,
            R6C15, R6C16, R6C17
        }

            RowCellsMap(7) = {
            R7C3, R7C4, R7C5,
            R7C6, R7C7, R7C8,
            R7C9, R7C10, R7C11,
            R7C12, R7C13, R7C14,
            R7C15, R7C16, R7C17
        }

            RowCellsMap(8) = {
            R8C3, R8C4, R8C5,
            R8C6, R8C7, R8C8,
            R8C9, R8C10, R8C11,
            R8C12, R8C13, R8C14,
            R8C15, R8C16, R8C17
        }

            RowCellsMap(9) = {
            R9C3, R9C4, R9C5,
            R9C6, R9C7, R9C8,
            R9C9, R9C10, R9C11,
            R9C12, R9C13, R9C14,
            R9C15, R9C16, R9C17
        }

            RowCellsMap(10) = {
            R10C3, R10C4, R10C5,
            R10C6, R10C7, R10C8,
            R10C9, R10C10, R10C11,
            R10C12, R10C13, R10C14,
            R10C15, R10C16, R10C17
        }

            ' Навешиваем обработчик клика на все ячейки с данными
            For Each kvp In RowCellsMap
                For Each tb In kvp.Value
                    AddHandler tb.MouseLeftButtonDown, AddressOf DataCell_MouseLeftButtonDown
                Next
            Next
        End Sub

        ' ==================== ЗАПОЛНЕНИЕ ====================

        Private Sub FillRow(rowIndex As Integer, data As TableRowData)
            If Not RowCellsMap.ContainsKey(rowIndex) Then Return

            Dim rowCells = RowCellsMap(rowIndex)

            Dim lists As List(Of Otkaz)() = {
            data.Complex1List, data.Complex2List, data.Complex3List, data.Complex13List,
            data.T1List, data.T2List, data.T3List, data.T13List,
            data.TR1List, data.TR2List, data.TR3List, data.TR13List,
            data.SLD1List, data.SLD2List, data.SLD3List, data.SLD13List,
            data.Factory1List, data.Factory2List, data.Factory3List, data.Factory13List
        }

            For i As Integer = 0 To Math.Min(rowCells.Length - 1, lists.Length - 1)
                Dim list = lists(i)

                rowCells(i).Text = If(list IsNot Nothing AndAlso list.Count > 0, list.Count.ToString(), "")
                'rowCells(i).Text = If(list?.Count.ToString(), "")

                rowCells(i).Tag = list
                SetClickableStyle(rowCells(i), list)
            Next
        End Sub

        Private Sub FillRowMerged(rowIndex As Integer, data As TableRowData, Optional allNormal As Boolean = False)


            If Not RowCellsMap.ContainsKey(rowIndex) Then Return

            Dim rowCells = RowCellsMap(rowIndex)

            Dim lists As List(Of Otkaz)() = {
                data.Complex12List, data.Complex3List, data.Complex13List,
                data.T12List, data.T3List, data.T13List,
                data.TR12List, data.TR3List, data.TR13List,
                data.SLD12List, data.SLD3List, data.SLD13List,
                data.Factory12List, data.Factory3List, data.Factory13List
            }

            ' Индексы, где НЕ нужен Tag (3кат и 1-3кат)
            Dim noTagIndices As New HashSet(Of Integer) From {1, 2, 4, 5, 7, 8, 10, 11, 13, 14}

            For i As Integer = 0 To Math.Min(rowCells.Length - 1, lists.Length - 1)
                Dim list = lists(i)

                rowCells(i).Text = If(list IsNot Nothing AndAlso list.Count > 0, list.Count.ToString(), "")
                'rowCells(i).Text = If(list?.Count.ToString(), "")

                If allNormal Then
                    ' ВСЕ ячейки - обычные, без drill-down
                    rowCells(i).Tag = Nothing
                    rowCells(i).Cursor = Cursors.Arrow
                    rowCells(i).FontWeight = FontWeights.Normal
                    rowCells(i).Foreground = Brushes.Black
                ElseIf noTagIndices.Contains(i) Then
                    ' Для 3кат и 1-3кат — без drill-down
                    rowCells(i).Tag = Nothing
                    rowCells(i).Cursor = Cursors.Arrow
                    rowCells(i).FontWeight = FontWeights.Normal
                    rowCells(i).Foreground = Brushes.Black
                Else
                    ' Для 1+2кат — с drill-down
                    rowCells(i).Tag = list
                    SetClickableStyle(rowCells(i), list)
                End If
            Next

        End Sub

        ' ==================== СТИЛИ ====================

        Private Sub SetClickableStyle(tb As TextBlock, list As List(Of Otkaz))
            If list IsNot Nothing AndAlso list.Count > 0 Then
                tb.Cursor = Cursors.Hand
                tb.FontWeight = FontWeights.Bold
                tb.Foreground = Brushes.DarkBlue
            Else
                tb.Cursor = Cursors.Arrow
                tb.FontWeight = FontWeights.Normal
                tb.Foreground = Brushes.Black
            End If
        End Sub

        ' ==================== ПОСТРОЕНИЕ ДАННЫХ ====================

        Private Function BuildTableRowData(
        otkazy As List(Of Otkaz),
        tPred As Func(Of Otkaz, Boolean),
        trPred As Func(Of Otkaz, Boolean),
        sldPred As Func(Of Otkaz, Boolean),
        factoryPred As Func(Of Otkaz, Boolean),
        cat1Pred As Func(Of Otkaz, Boolean),
        cat2Pred As Func(Of Otkaz, Boolean),
        cat3Pred As Func(Of Otkaz, Boolean)
    ) As TableRowData

            Dim data As New TableRowData()

            Dim tList = otkazy.Where(tPred).ToList()
            data.T1List = tList.Where(cat1Pred).ToList()
            data.T2List = tList.Where(cat2Pred).ToList()
            data.T3List = tList.Where(cat3Pred).ToList()

            Dim trList = otkazy.Where(trPred).ToList()
            data.TR1List = trList.Where(cat1Pred).ToList()
            data.TR2List = trList.Where(cat2Pred).ToList()
            data.TR3List = trList.Where(cat3Pred).ToList()

            Dim sldList = otkazy.Where(sldPred).ToList()
            data.SLD1List = sldList.Where(cat1Pred).ToList()
            data.SLD2List = sldList.Where(cat2Pred).ToList()
            data.SLD3List = sldList.Where(cat3Pred).ToList()

            Dim fList = otkazy.Where(factoryPred).ToList()
            data.Factory1List = fList.Where(cat1Pred).ToList()
            data.Factory2List = fList.Where(cat2Pred).ToList()
            data.Factory3List = fList.Where(cat3Pred).ToList()

            Return data
        End Function

        ' ==================== ЗАГРУЗКА ====================

        Private Sub S24Table1_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
            If _currentYearOtkazy Is Nothing OrElse _previousYearOtkazy Is Nothing Then
                Return
            End If
            UpdateHeaders()
            InitializeRowCells()

            Dim isCat1 As Func(Of Otkaz, Boolean) = Function(o) o.Kat = 1
            Dim isCat2 As Func(Of Otkaz, Boolean) = Function(o) o.Kat = 2
            Dim isCat3 As Func(Of Otkaz, Boolean) = Function(o) o.Kat = 3

            ' ▼▼▼ ОБЪЕДИНЁННЫЙ ПРЕДИКАТ ПО Т ▼▼▼
            Dim tchCombined As Func(Of Otkaz, Boolean) = Function(o) SignedOnTCH(o) OrElse VRassForTCH(o)
            ' ▲▲▲ ============ ▲▲▲

            ' Текущий год: строки 3 (полная) и 5 (merged)
            Dim rowCurrent = BuildTableRowData(
            _currentYearOtkazy,
             tchCombined, SignedOnTRPU, SignedOnSLD, SignedOnZav,
            isCat1, isCat2, isCat3)
            FillRow(3, rowCurrent)
            FillRowMerged(5, rowCurrent)

            ' Прошлый год: строки 4 (полная) и 6 (merged)
            Dim rowPrevious = BuildTableRowData(
            _previousYearOtkazy,
            tchCombined, SignedOnTRPU, SignedOnSLD, SignedOnZav,
            isCat1, isCat2, isCat3)
            FillRow(4, rowPrevious)
            FillRowMerged(6, rowPrevious)



            ' ▼▼▼ Строка 7: Целевые показатели ▼▼▼
            If _periodEnd IsNot Nothing Then
                Dim startDate As Date = If(_periodStart, _periodEnd).Value
                Dim endDate As Date = _periodEnd.Value

                If startDate > endDate Then
                    Dim tmp As Date = startDate
                    startDate = endDate
                    endDate = tmp
                End If

                Dim allGoals = GoalsStore.Load()
                Dim goalForPeriod = GetGoalForPeriod(allGoals, startDate, endDate)

                FillRowGoal(7, goalForPeriod)
            Else
                FillRowGoal(7, New MonthlyGoal())
            End If
            ' Строка 8: Изменения случ. (абсолютная разница)
            Dim changesData = CalculateChanges(rowCurrent, rowPrevious)
            FillRowNumeric(8, changesData)

            ' Строка 9: % изменений (процентная разница)
            Dim percentData = CalculatePercentChanges(rowCurrent, rowPrevious)
            FillRowNumeric(9, percentData)

            ' ▼▼▼ Строка 10 - полный месяц прошлого года ▼▼▼
            If _periodEnd IsNot Nothing AndAlso _previousYearOtkazyRaw IsNot Nothing Then
                Dim oldP = GetPrevPeriod(If(_periodStart, _periodEnd).Value.Date,
                             _periodEnd.Value.Date,
                             _previousYearOtkazyRaw)

                Dim actualPrevYear As Integer = oldP.End.Year
                Dim targetMon As Integer = oldP.End.Month        ' ← февраль из ручного периода!

                Dim monthStart As New Date(actualPrevYear, targetMon, 1)
                Dim monthEnd As Date = monthStart.AddMonths(1).AddDays(-1)

                Dim fullMonthOtkazy = _previousYearOtkazyRaw.Where(Function(o) o.Nach.Year = actualPrevYear AndAlso
                                                                  o.Nach.Date >= monthStart.Date AndAlso
                                                                  o.Nach.Date <= monthEnd.Date).ToList()

                Dim rowFullMonth = BuildTableRowData(fullMonthOtkazy, tchCombined, SignedOnTRPU, SignedOnSLD, SignedOnZav, isCat1, isCat2, isCat3)
                FillRowMerged(10, rowFullMonth, allNormal:=True)
            End If

        End Sub


        ''' <summary>
        ''' Возвращает суммарную цель за все месяцы, попавшие в период startDate...endDate
        ''' </summary>
        Private Function GetGoalForPeriod(
    goals As List(Of MonthlyGoal),
    startDate As Date,
    endDate As Date
) As MonthlyGoal

            Dim result As New MonthlyGoal()

            If goals Is Nothing Then
                Return result
            End If

            Dim currentMonth As New Date(startDate.Year, startDate.Month, 1)
            Dim lastMonth As New Date(endDate.Year, endDate.Month, 1)

            While currentMonth <= lastMonth
                Dim y As Integer = currentMonth.Year
                Dim m As Integer = currentMonth.Month

                Dim monthGoals = goals.Where(Function(g) g.Year = y AndAlso g.Month = m).ToList()

                For Each goal In monthGoals
                    'result.Complex12 += goal.Complex12
                    'result.Complex3 += goal.Complex3
                    'result.Complex13 += goal.Complex13

                    result.T12 += goal.T12
                    result.T3 += goal.T3
                    'result.T13 += goal.T13

                    result.TR12 += goal.TR12
                    result.TR3 += goal.TR3
                    'result.TR13 += goal.TR13

                    result.SLD12 += goal.SLD12
                    result.SLD3 += goal.SLD3
                    'result.SLD13 += goal.SLD13

                    result.Factory12 += goal.Factory12
                    result.Factory3 += goal.Factory3
                    'result.Factory13 += goal.Factory13
                Next

                currentMonth = currentMonth.AddMonths(1)
            End While

            Return result
        End Function



        ' ==================== ОБРАБОТЧИКИ ====================

        Private Sub DataCell_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
            Dim tb = TryCast(sender, TextBlock)
            If tb Is Nothing Then Return

            Dim clickedOtkazy = TryCast(tb.Tag, List(Of Otkaz))
            If clickedOtkazy IsNot Nothing AndAlso clickedOtkazy.Count > 0 Then
                Dim ids = String.Join(",", clickedOtkazy.Select(Function(o) o.Id))
                Clipboard.SetText(ids)
                MW.ShowOtkazyList(clickedOtkazy.OrderBy(Function(o) o.Nach))
            End If

            e.Handled = True
        End Sub


        ' ==================== ВЫЧИСЛЕНИЕ ИЗМЕНЕНИЙ ====================

        ' ==================== ВЫЧИСЛЕНИЕ ИЗМЕНЕНИЙ ====================

        ''' <summary>
        ''' Абсолютная разница (строка 8: "Изменения случ.")
        ''' </summary>
        Private Function CalculateChanges(current As TableRowData, previous As TableRowData) As TableRowNumericData
            Dim data As New TableRowNumericData()

            ' Комплекс
            data.Complex12 = FormatChange(current.Complex12List?.Count, previous.Complex12List?.Count)
            data.Complex3 = FormatChange(current.Complex3List?.Count, previous.Complex3List?.Count)
            data.Complex13 = FormatChange(current.Complex13List?.Count, previous.Complex13List?.Count)

            ' Т
            data.T12 = FormatChange(current.T12List?.Count, previous.T12List?.Count)
            data.T3 = FormatChange(current.T3List?.Count, previous.T3List?.Count)
            data.T13 = FormatChange(current.T13List?.Count, previous.T13List?.Count)

            ' ТР
            data.TR12 = FormatChange(current.TR12List?.Count, previous.TR12List?.Count)
            data.TR3 = FormatChange(current.TR3List?.Count, previous.TR3List?.Count)
            data.TR13 = FormatChange(current.TR13List?.Count, previous.TR13List?.Count)

            ' СЛД
            data.SLD12 = FormatChange(current.SLD12List?.Count, previous.SLD12List?.Count)
            data.SLD3 = FormatChange(current.SLD3List?.Count, previous.SLD3List?.Count)
            data.SLD13 = FormatChange(current.SLD13List?.Count, previous.SLD13List?.Count)

            ' Заводы
            data.Factory12 = FormatChange(current.Factory12List?.Count, previous.Factory12List?.Count)
            data.Factory3 = FormatChange(current.Factory3List?.Count, previous.Factory3List?.Count)
            data.Factory13 = FormatChange(current.Factory13List?.Count, previous.Factory13List?.Count)

            Return data
        End Function

        ''' <summary>
        ''' Процентная разница (строка 9: "% изменений")
        ''' </summary>
        Private Function CalculatePercentChanges(current As TableRowData, previous As TableRowData) As TableRowNumericData
            Dim data As New TableRowNumericData()

            ' Комплекс
            data.Complex12 = FormatPercentChange(current.Complex12List?.Count, previous.Complex12List?.Count)
            data.Complex3 = FormatPercentChange(current.Complex3List?.Count, previous.Complex3List?.Count)
            data.Complex13 = FormatPercentChange(current.Complex13List?.Count, previous.Complex13List?.Count)

            ' Т
            data.T12 = FormatPercentChange(current.T12List?.Count, previous.T12List?.Count)
            data.T3 = FormatPercentChange(current.T3List?.Count, previous.T3List?.Count)
            data.T13 = FormatPercentChange(current.T13List?.Count, previous.T13List?.Count)

            ' ТР
            data.TR12 = FormatPercentChange(current.TR12List?.Count, previous.TR12List?.Count)
            data.TR3 = FormatPercentChange(current.TR3List?.Count, previous.TR3List?.Count)
            data.TR13 = FormatPercentChange(current.TR13List?.Count, previous.TR13List?.Count)

            ' СЛД
            data.SLD12 = FormatPercentChange(current.SLD12List?.Count, previous.SLD12List?.Count)
            data.SLD3 = FormatPercentChange(current.SLD3List?.Count, previous.SLD3List?.Count)
            data.SLD13 = FormatPercentChange(current.SLD13List?.Count, previous.SLD13List?.Count)

            ' Заводы
            data.Factory12 = FormatPercentChange(current.Factory12List?.Count, previous.Factory12List?.Count)
            data.Factory3 = FormatPercentChange(current.Factory3List?.Count, previous.Factory3List?.Count)
            data.Factory13 = FormatPercentChange(current.Factory13List?.Count, previous.Factory13List?.Count)

            Return data
        End Function

        ' ==================== ФОРМАТИРОВАНИЕ ====================

        ''' <summary>
        ''' Форматирует абсолютную разницу: "+3", "-2", "0"
        ''' </summary>
        Private Function FormatChange(currentCount As Integer?, previousCount As Integer?) As String
            Dim curr = If(currentCount, 0)
            Dim prev = If(previousCount, 0)
            Dim diff = curr - prev

            If diff > 0 Then
                Return "+" & diff.ToString()
            ElseIf diff < 0 Then
                Return diff.ToString()
            Else
                Return "0"
            End If
        End Function

        ''' <summary>
        ''' Форматирует процентную разницу: "+42.86%", "-15.38%", "0%", "N/A"
        ''' </summary>
        Private Function FormatPercentChange(currentCount As Integer?, previousCount As Integer?) As String
            Dim curr = If(currentCount, 0)
            Dim prev = If(previousCount, 0)

            If prev = 0 Then
                If curr = 0 Then
                    Return "0%"
                Else
                    Return "+100%"  ' было "N/A"
                End If
            End If

            Dim percent = (curr - prev) / prev * 100

            If percent > 0 Then
                If percent > 100 Then
                    Return $"в +{Math.Round(curr / prev, 1)}р"
                Else
                    Return $"+ {Math.Round(percent, 1).ToString("F1")}%"
                End If

            ElseIf percent < 0 Then

                Return $"{Math.Round(percent, 1).ToString("F1")}%" 'percent.ToString("F1") & "%"
            Else
                Return "0%"
            End If

        End Function

        ' ==================== ЗАПОЛНЕНИЕ ЧИСЛОВЫХ СТРОК ====================

        ''' <summary>
        ''' Заполняет строку числовыми значениями (без drill-down)
        ''' </summary>
        Private Sub FillRowNumeric(rowIndex As Integer, data As TableRowNumericData)

            If Not RowCellsMap.ContainsKey(rowIndex) Then Return

            Dim rowCells = RowCellsMap(rowIndex)

            Dim values As String() = {
                data.Complex12, data.Complex3, data.Complex13,
                data.T12, data.T3, data.T13,
                data.TR12, data.TR3, data.TR13,
                data.SLD12, data.SLD3, data.SLD13,
                data.Factory12, data.Factory3, data.Factory13
            }

            For i As Integer = 0 To Math.Min(rowCells.Length - 1, values.Length - 1)
                Dim value = values(i)
                rowCells(i).Text = value
                rowCells(i).Tag = Nothing
                rowCells(i).Cursor = Cursors.Arrow
                rowCells(i).FontWeight = FontWeights.Normal
                rowCells(i).Foreground = GetColorForChange(value)
            Next


        End Sub

        ''' <summary>
        ''' Возвращает цвет для значения изменения:
        ''' "+" → красный, "-" → зелёный, иначе → чёрный
        ''' </summary>
        Private Function GetColorForChange(value As String) As Brush
            If String.IsNullOrEmpty(value) Then Return Brushes.Black

            If value.Contains("+") Then
                Return Brushes.Red
            ElseIf value.StartsWith("-") Then
                Return Brushes.Green
            Else
                Return Brushes.Black
            End If
        End Function

        Private Sub UpdateHeaders()

            If _periodEnd Is Nothing Then Return

            Dim currentYear As Integer = _periodEnd.Value.Year

            ' ▼▼▼ прошлый период — ручной или авто, один источник правды ▼▼▼
            Dim oldP = GetPrevPeriod(If(_periodStart, _periodEnd).Value.Date,
                                     _periodEnd.Value.Date,
                                     _previousYearOtkazyRaw)

            LblRow2Period.Text = $"{_periodStart.Value:dd.MM} - {_periodEnd.Value:dd.MM.yy}"
            LblRow2.Text = currentYear.ToString()
            LblRow3.Text = oldP.End.Year.ToString()

            LblRow4.Text = currentYear.ToString()
            LblRow5.Text = oldP.End.Year.ToString()
            LblGoal.Text = "Цель"

            LblFullPeriodPrev.Text = $"{oldP.Start:dd.MM} - {oldP.End:dd.MM.yy}"

        End Sub

        ''' <summary>
        ''' Заполняет строку целевыми показателями
        ''' </summary>
        Private Sub FillRowGoal(rowIndex As Integer, goal As MonthlyGoal)
            If Not RowCellsMap.ContainsKey(rowIndex) Then Return

            Dim rowCells = RowCellsMap(rowIndex)

            Dim values As String() = {
                goal.Complex12.ToString(), goal.Complex3.ToString(), goal.Complex13.ToString(),
                goal.T12.ToString(), goal.T3.ToString(), goal.T13.ToString(),
                goal.TR12.ToString(), goal.TR3.ToString(), goal.TR13.ToString(),
                goal.SLD12.ToString(), goal.SLD3.ToString(), goal.SLD13.ToString(),
                goal.Factory12.ToString(), goal.Factory3.ToString(), goal.Factory13.ToString()
            }

            For i As Integer = 0 To Math.Min(rowCells.Length - 1, values.Length - 1)
                rowCells(i).Text = If(values(i) = "0", "", values(i))
                rowCells(i).Tag = Nothing
                rowCells(i).Cursor = Cursors.Arrow
                'rowCells(i).FontWeight = FontWeights.Bold ' Цели выделяем жирным
                'rowCells(i).Foreground = Brushes.Black
            Next
        End Sub

    End Class


    Public Class TableRowData

        ' ===== БАЗОВЫЕ СПИСКИ (заполняются вручную) =====
        ' Т
        Public Property T1List As List(Of Otkaz) = Nothing
        Public Property T2List As List(Of Otkaz) = Nothing
        Public Property T3List As List(Of Otkaz) = Nothing

        ' ТР
        Public Property TR1List As List(Of Otkaz) = Nothing
        Public Property TR2List As List(Of Otkaz) = Nothing
        Public Property TR3List As List(Of Otkaz) = Nothing

        ' СЛД
        Public Property SLD1List As List(Of Otkaz) = Nothing
        Public Property SLD2List As List(Of Otkaz) = Nothing
        Public Property SLD3List As List(Of Otkaz) = Nothing

        ' Заводы
        Public Property Factory1List As List(Of Otkaz) = Nothing
        Public Property Factory2List As List(Of Otkaz) = Nothing
        Public Property Factory3List As List(Of Otkaz) = Nothing

        ' ===== ВЫЧИСЛЯЕМЫЕ СПИСКИ (ReadOnly) =====

        ' ----- Т -----
        Public ReadOnly Property T12List As List(Of Otkaz)
            Get
                Return T1List?.Concat(T2List).ToList()
            End Get
        End Property

        Public ReadOnly Property T13List As List(Of Otkaz)
            Get
                Return T1List?.Concat(T2List)?.Concat(T3List).ToList()
            End Get
        End Property

        ' ----- ТР -----
        Public ReadOnly Property TR12List As List(Of Otkaz)
            Get
                Return TR1List?.Concat(TR2List).ToList()
            End Get
        End Property

        Public ReadOnly Property TR13List As List(Of Otkaz)
            Get
                Return TR1List?.Concat(TR2List)?.Concat(TR3List).ToList()
            End Get
        End Property

        ' ----- СЛД -----
        Public ReadOnly Property SLD12List As List(Of Otkaz)
            Get
                Return SLD1List?.Concat(SLD2List).ToList()
            End Get
        End Property

        Public ReadOnly Property SLD13List As List(Of Otkaz)
            Get
                Return SLD1List?.Concat(SLD2List)?.Concat(SLD3List).ToList()
            End Get
        End Property

        ' ----- Заводы -----
        Public ReadOnly Property Factory12List As List(Of Otkaz)
            Get
                Return Factory1List?.Concat(Factory2List).ToList()
            End Get
        End Property

        Public ReadOnly Property Factory13List As List(Of Otkaz)
            Get
                Return Factory1List?.Concat(Factory2List).Concat(Factory3List).ToList()
            End Get
        End Property

        ' ----- КОМПЛЕКС (агрегат) -----
        Public ReadOnly Property Complex1List As List(Of Otkaz)
            Get
                Return T1List?.Concat(TR1List)?.Concat(SLD1List)?.Concat(Factory1List)?.ToList()
            End Get
        End Property

        Public ReadOnly Property Complex2List As List(Of Otkaz)
            Get
                Return T2List?.Concat(TR2List)?.Concat(SLD2List)?.Concat(Factory2List)?.ToList()
            End Get
        End Property

        Public ReadOnly Property Complex3List As List(Of Otkaz)
            Get
                Return T3List?.Concat(TR3List)?.Concat(SLD3List)?.Concat(Factory3List)?.ToList()
            End Get
        End Property

        Public ReadOnly Property Complex12List As List(Of Otkaz)
            Get
                Return Complex1List?.Concat(Complex2List).ToList()
            End Get
        End Property

        Public ReadOnly Property Complex13List As List(Of Otkaz)
            Get
                Return Complex1List?.Concat(Complex2List)?.Concat(Complex3List)?.ToList()
            End Get
        End Property
    End Class

    'Этот класс отличается от TableRowData тем, что хранит готовые строковые значения (например, "+3", "-15.38%"), а не списки отказов. Он используется только для строк 8 и 9, где drill-down не нужен
    Public Class TableRowNumericData
        ' ===== Комплекс =====
        Public Property Complex12 As String = ""
        Public Property Complex3 As String = ""
        Public Property Complex13 As String = ""

        ' ===== Т =====
        Public Property T12 As String = ""
        Public Property T3 As String = ""
        Public Property T13 As String = ""

        ' ===== ТР =====
        Public Property TR12 As String = ""
        Public Property TR3 As String = ""
        Public Property TR13 As String = ""

        ' ===== СЛД =====
        Public Property SLD12 As String = ""
        Public Property SLD3 As String = ""
        Public Property SLD13 As String = ""

        ' ===== Заводы =====
        Public Property Factory12 As String = ""
        Public Property Factory3 As String = ""
        Public Property Factory13 As String = ""
    End Class




End Namespace

