Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Printing
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media
Imports System.Windows.Xps
Imports iText.IO.Image
Imports iText.Kernel.Geom
Imports iText.Kernel.Pdf
Imports iText.Layout
Imports Path = System.IO.Path
Imports PdfImage = iText.Layout.Element.Image   ' ← алиас, чтобы не конфликтовать с WPF Image

Namespace Kas

	Partial Public Class S24Table2



        Private _cur As List(Of Otkaz)
        Private _prev As List(Of Otkaz)
        Private _curRaw As List(Of Otkaz)
        Private _prevRaw As List(Of Otkaz)
        Private _periodStart As Date?
        Private _periodEnd As Date?
        Private _ytdRaw As List(Of Otkaz)



        Private Shared ReadOnly Komplexes As Integer() = {1, 2, 3, 5, 7}
        Private Shared ReadOnly Ru As CultureInfo = CultureInfo.GetCultureInfo("ru-RU")




        Private ReadOnly Property MonthsCount As Integer
            Get
                If _periodStart Is Nothing OrElse _periodEnd Is Nothing Then Return 1
                Return (_periodEnd.Value.Year - _periodStart.Value.Year) * 12 +
               (_periodEnd.Value.Month - _periodStart.Value.Month) + 1
            End Get
        End Property

        Private ReadOnly Property IsMultiMonth As Boolean
            Get
                Return MonthsCount > 1
            End Get
        End Property

        Private Function MonthPlural(n As Integer) As String
            Dim d = n Mod 100
            If d >= 11 AndAlso d <= 14 Then Return "месяцев"
            Select Case n Mod 10
                Case 1 : Return "месяц"
                Case 2, 3, 4 : Return "месяца"
                Case Else : Return "месяцев"
            End Select
        End Function


        Public Sub New(currentYearOtkazy As List(Of Otkaz),
                       previousYearOtkazy As List(Of Otkaz),
                       Optional periodStart As Date? = Nothing,
                       Optional periodEnd As Date? = Nothing,
               Optional ytdOtkazy As List(Of Otkaz) = Nothing)

            InitializeComponent()


            _periodStart = periodStart
            _periodEnd = periodEnd

            ' «с начала года» — сырой объединённый список, БЕЗ фильтра по периоду
            _ytdRaw = If(ytdOtkazy, currentYearOtkazy)
            _prevRaw = previousYearOtkazy

            ' ===== ФИЛЬТРАЦИЯ ТЕКУЩЕГО ГОДА по periodStart/periodEnd =====
            If periodStart IsNot Nothing OrElse periodEnd IsNot Nothing Then
                Dim nextDayAfterEnd = If(periodEnd.HasValue,
                         periodEnd.Value.Date.AddDays(1),
                         Date.MaxValue)
                'Dim nextDayAfterEnd = If(periodEnd, Date.MaxValue).Date.AddDays(1)

                _cur = currentYearOtkazy.Where(Function(o)
                                                   Dim d = o.Nach.Date
                                                   If periodStart IsNot Nothing AndAlso d < periodStart.Value.Date Then Return False
                                                   If d >= nextDayAfterEnd Then Return False
                                                   Return True
                                               End Function).ToList()

                ' ===== ФИЛЬТРАЦИЯ ПРОШЛОГО ГОДА: ручной период или авто =====
                Dim oldP = GetPrevPeriod(If(periodStart, periodEnd).Value.Date,
                                 If(periodEnd, periodStart).Value.Date,
                                 previousYearOtkazy)

                Dim nextDayAfterOldEnd = oldP.End.Date.AddDays(1)

                _prev = previousYearOtkazy.Where(Function(o)
                                                     Dim d = o.Nach.Date
                                                     If d < oldP.Start.Date Then Return False
                                                     If d >= nextDayAfterOldEnd Then Return False
                                                     Return True
                                                 End Function).ToList()
            Else
                _cur = currentYearOtkazy
                _prev = previousYearOtkazy
            End If

            ' для дорог и правой панели — то же, что в таблицах
            _curRaw = _cur

        End Sub

        Private Sub S24Table2_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
            Try
                If _cur Is Nothing OrElse _prev Is Nothing Then Return
                If _curRaw Is Nothing Then _curRaw = New List(Of Otkaz)()
                If _prevRaw Is Nothing Then _prevRaw = New List(Of Otkaz)()

                FillCaption()
                FillLeftTable()
                FillPlanTable()
                FillRightPanel()

            Catch ex As Exception
                MessageBox.Show(ex.ToString(), "Ошибка в S24Table2")
            End Try
        End Sub

        ' ==================== АГРЕГАЦИЯ ====================

        Private Function Sel(list As List(Of Otkaz), k As Integer?, cat As Integer?) As IEnumerable(Of Otkaz)
            Return list.Where(Function(o)
                                  If Not o.Uchet Then Return False
                                  If o.KomplexAsInt <= 0 Then Return False
                                  If k.HasValue AndAlso o.KomplexAsInt <> k.Value Then Return False
                                  If cat.HasValue AndAlso o.Kat <> cat.Value Then Return False
                                  Return True
                              End Function)
        End Function

        Private Function Ots(k As Integer?, cat As Integer?) As Integer
            Return Sel(_cur, k, cat).Count()
        End Function

        Private Function OtsPrev(k As Integer?, cat As Integer?) As Integer
            Return Sel(_prev, k, cat).Count()
        End Function

        Private Function Hours(k As Integer?, cat As Integer?) As Double
            Return Sel(_cur, k, cat).Sum(Function(o) CSng(o.PCh))
        End Function

        Private Function HoursPrev(k As Integer?, cat As Integer?) As Double
            Return Sel(_prev, k, cat).Sum(Function(o) CSng(o.PCh))
        End Function

        Private Function F2(v As Double) As String
            Return v.ToString("F2", Ru)
        End Function

        Private Function PairOts(k As Integer?, cat As Integer?) As String
            Dim c = Ots(k, cat) : Dim p = OtsPrev(k, cat)
            If c = 0 AndAlso p = 0 Then Return ""
            Return $"{c}/{p}"
        End Function

        Private Function PairHours(k As Integer?, cat As Integer?) As String
            Dim c = Hours(k, cat) : Dim p = HoursPrev(k, cat)
            If c = 0 AndAlso p = 0 Then Return ""
            Return $"{F2(c)}/{F2(p)}"
        End Function

        Private Function DiffOts(k As Integer?, cat As Integer?) As String
            Dim c = Ots(k, cat) : Dim p = OtsPrev(k, cat)
            If c = 0 AndAlso p = 0 Then Return ""
            Dim d = c - p
            Return If(d > 0, "+" & d, d.ToString())
        End Function

        Private Function DiffHours(k As Integer?, cat As Integer?) As String
            Dim c = Hours(k, cat) : Dim p = HoursPrev(k, cat)
            If c = 0 AndAlso p = 0 Then Return ""
            Dim d = c - p
            Return If(d > 0, "+", "") & F2(d)
        End Function

        ' ==================== ЗАПОЛНЕНИЕ ====================

        Private Sub FillCaption()
            If _periodEnd Is Nothing Then
                LblCaption.Text = "Отказы и часы (тек год/прош год)"
                Return
            End If

            If IsMultiMonth Then
                LblCaption.Text = $"Отказы и часы за {MonthsCount} {MonthPlural(MonthsCount)} " &
                                  $"({_periodStart.Value:dd.MM} - {_periodEnd.Value:dd.MM.yy}) " &
                                  $"на {Today:dd.MM.yy} (тек год/прош год)"
            Else
                Dim monthName = Ru.DateTimeFormat.GetMonthName(_periodEnd.Value.Month)
                LblCaption.Text = $"Отказы и часы за {monthName} на {Today:dd.MM.yy} (тек год/прош год)"
            End If



        End Sub

        Private Sub FillLeftTable()
            ' Названия ТЧЭ
            LblTche1.Text = Otkaz.GetTCHE_Name(1)
            LblTche2.Text = Otkaz.GetTCHE_Name(2)
            LblTche3.Text = Otkaz.GetTCHE_Name(3)
            LblTche5.Text = Otkaz.GetTCHE_Name(5)
            LblTche7.Text = Otkaz.GetTCHE_Name(7)

            ' Данные ТЧЭ
            FillDataRow(2, 1)
            FillDataRow(3, 2)
            FillDataRow(4, 3)
            FillDataRow(5, 5)
            FillDataRow(6, 7)

            ' Итоговая строка
            R7C1.Text = PairOts(Nothing, 1)
            R7C2.Text = PairHours(Nothing, 1)
            R7C3.Text = PairOts(Nothing, 2)
            R7C4.Text = PairHours(Nothing, 2)
            R7C5.Text = PairOts(Nothing, 3)
            R7C6.Text = PairHours(Nothing, 3)
            R7C7.Text = PairOts(Nothing, Nothing)
            R7C8.Text = PairHours(Nothing, Nothing)

            ' Названия для разницы
            LblDiff1.Text = Otkaz.GetTCHE_Name(1)
            LblDiff2.Text = Otkaz.GetTCHE_Name(2)
            LblDiff3.Text = Otkaz.GetTCHE_Name(3)
            LblDiff5.Text = Otkaz.GetTCHE_Name(5)
            LblDiff7.Text = Otkaz.GetTCHE_Name(7)

            ' Разница
            FillDiffRow(9, 1)
            FillDiffRow(10, 2)
            FillDiffRow(11, 3)
            FillDiffRow(12, 5)
            FillDiffRow(13, 7)

            ' Итоговая разница
            R14C1.Text = DiffOts(Nothing, 1)
            R14C2.Text = DiffHours(Nothing, 1)
            R14C3.Text = DiffOts(Nothing, 2)
            R14C4.Text = DiffHours(Nothing, 2)
            R14C5.Text = DiffOts(Nothing, 3)
            R14C6.Text = DiffHours(Nothing, 3)
            R14C7.Text = DiffOts(Nothing, Nothing)
            R14C8.Text = DiffHours(Nothing, Nothing)
        End Sub

        Private Sub FillDataRow(row As Integer, k As Integer)
            Dim prefix = $"R{row}C"
            FindName(prefix & "1").Text = PairOts(k, 1)
            FindName(prefix & "2").Text = PairHours(k, 1)
            FindName(prefix & "3").Text = PairOts(k, 2)
            FindName(prefix & "4").Text = PairHours(k, 2)
            FindName(prefix & "5").Text = PairOts(k, 3)
            FindName(prefix & "6").Text = PairHours(k, 3)
            FindName(prefix & "7").Text = PairOts(k, Nothing)
            FindName(prefix & "8").Text = PairHours(k, Nothing)
        End Sub

        Private Sub FillDiffRow(row As Integer, k As Integer)
            Dim prefix = $"R{row}C"
            FindName(prefix & "1").Text = DiffOts(k, 1)
            FindName(prefix & "2").Text = DiffHours(k, 1)
            FindName(prefix & "3").Text = DiffOts(k, 2)
            FindName(prefix & "4").Text = DiffHours(k, 2)
            FindName(prefix & "5").Text = DiffOts(k, 3)
            FindName(prefix & "6").Text = DiffHours(k, 3)
            FindName(prefix & "7").Text = DiffOts(k, Nothing)
            FindName(prefix & "8").Text = DiffHours(k, Nothing)
        End Sub

        Private Sub FillRightPanel()


            If _periodEnd Is Nothing Then Return

            Dim periodEndDate As Date = _periodEnd.Value
            Dim currentYear As Integer = periodEndDate.Year

            ' прошлый период — ПО ГРАНИЦАМ ПЕРИОДА, а не по одному дню
            Dim prevPeriod = GetPrevPeriod(If(_periodStart, _periodEnd).Value.Date,
                                     If(_periodEnd, _periodStart).Value.Date, _prevRaw)
            Dim previousYear As Integer = prevPeriod.End.Year

            Dim lastMonthOfPeriod As Integer = periodEndDate.Month

            ' ===== окна сравнения «Nм» =====
            Dim monthNumber As Integer
            Dim curWindowStart As Date
            Dim curWindowEnd As Date
            Dim prevWindowStart As Date
            Dim prevWindowEnd As Date

            If IsMultiMonth Then
                ' несколько месяцев: с начала периода до конца последнего месяца периода (ТГ и ПГ)
                monthNumber = MonthsCount
                curWindowStart = _periodStart.Value.Date
                curWindowEnd = New Date(periodEndDate.Year, lastMonthOfPeriod, Date.DaysInMonth(periodEndDate.Year, lastMonthOfPeriod))
                prevWindowStart = prevPeriod.Start.Date
                prevWindowEnd = New Date(previousYear, prevPeriod.End.Month, Date.DaysInMonth(previousYear, prevPeriod.End.Month))
            Else
                ' один месяц: с начала года до конца месяца периода
                monthNumber = periodEndDate.Month
                curWindowStart = New Date(currentYear, 1, 1)
                curWindowEnd = periodEndDate.Date
                prevWindowStart = New Date(previousYear, 1, 1)
                prevWindowEnd = New Date(previousYear, lastMonthOfPeriod, Date.DaysInMonth(previousYear, lastMonthOfPeriod))
            End If

            Dim monthNameShort = Ru.DateTimeFormat.GetAbbreviatedMonthName(periodEndDate.Month).TrimEnd("."c).ToUpper()

            ' ===== блок «с учетом плана передачи» =====
            Dim totalHoursAll = HoursSum(_cur, excludePlan:=False)
            Dim hoursWithoutTransferPlan = HoursSum(_cur, excludePlan:=True)
            Dim transferPlanHours = totalHoursAll - hoursWithoutTransferPlan

            ' прошлый год для «П.Г.»: 1 месяц — весь месяц; несколько — тот же период прошлого года
            Dim prevYearMonthHours As Single
            If IsMultiMonth Then
                prevYearMonthHours = _prevRaw.Where(Function(o) o.KomplexAsInt > 0 AndAlso
                                            o.Nach.Date >= prevPeriod.Start.Date AndAlso
                                            o.Nach.Date <= prevPeriod.End.Date) _
                                 .Sum(Function(o) CSng(o.PCh))
            Else
                Dim prevYearMonthStart = New Date(previousYear, lastMonthOfPeriod, 1)
                Dim prevYearMonthEnd = New Date(previousYear, lastMonthOfPeriod, Date.DaysInMonth(previousYear, lastMonthOfPeriod))
                prevYearMonthHours = _prevRaw.Where(Function(o) o.KomplexAsInt > 0 AndAlso
                                            o.Nach.Date >= prevYearMonthStart AndAlso
                                            o.Nach.Date <= prevYearMonthEnd) _
                                 .Sum(Function(o) CSng(o.PCh))
            End If

            MonthHoursLab.Text = If(IsMultiMonth, "период часы", "месяц часы")
            PgLab.Text = If(IsMultiMonth, "П.Г. (период)", "П.Г.")

            TxtMonthHoursCur.Text = F2(hoursWithoutTransferPlan)
            TxtMonthHoursDiff.Text = F2(hoursWithoutTransferPlan - prevYearMonthHours)
            TxtMonthHoursPrev.Text = F2(prevYearMonthHours)
            TxtMonthHoursDiff.ToolTip = $"{F2(totalHoursAll)} − {F2(transferPlanHours)} (план) − {F2(prevYearMonthHours)}"

            ' ===== блок «Nм ТГ / Nм ПГ» =====


            Dim currentWindow = _ytdRaw.Where(Function(o) o.KomplexAsInt > 0 AndAlso
                              o.Nach.Date >= curWindowStart AndAlso
                              o.Nach.Date <= curWindowEnd)
            Dim previousYearWindow = _prevRaw.Where(Function(o) o.KomplexAsInt > 0 AndAlso
                                o.Nach.Date >= prevWindowStart AndAlso
                                o.Nach.Date <= prevWindowEnd)

            Dim currentWindowHoursAll = currentWindow.Sum(Function(o) CSng(o.PCh))
            Dim currentWindowPlanHours = currentWindow.Where(Function(o) HasTransferPlan(o)).Sum(Function(o) CSng(o.PCh))
            Dim previousYearWindowHours = previousYearWindow.Sum(Function(o) CSng(o.PCh))

            ' ТГ ост = все часы − план передачи − прошлый год  (как в верхнем блоке)
            Dim ytdDiff = currentWindowHoursAll - currentWindowPlanHours - previousYearWindowHours

            TxtYtdMonthCur.Text = $"{monthNumber}м ТГ"
            TxtYtdMonthPrev.Text = $"{monthNumber}м ПГ"
            TxtYtdHoursCur.Text = F2(currentWindowHoursAll - currentWindowPlanHours)   ' часы без плана
            TxtYtdHoursPrev.Text = F2(previousYearWindowHours)
            TxtYtdHoursDiff.Text = F2(ytdDiff)
            TxtYtdHoursDiff.ToolTip = $"{F2(currentWindowHoursAll)} ({curWindowStart:dd.MM.yy}-{curWindowEnd:dd.MM.yy}) " &
                              $"− {F2(currentWindowPlanHours)} (план) " &
                              $"− {F2(previousYearWindowHours)} ({prevWindowStart:dd.MM.yy}-{prevWindowEnd:dd.MM.yy}) " &
                              $"= {F2(ytdDiff)}"



            'Dim currentWindow = _ytdRaw.Where(Function(o) o.KomplexAsInt > 0 AndAlso
            '                          o.Nach.Date >= curWindowStart AndAlso
            '                          o.Nach.Date <= curWindowEnd)
            'Dim previousYearWindow = _prevRaw.Where(Function(o) o.KomplexAsInt > 0 AndAlso
            '                            o.Nach.Date >= prevWindowStart AndAlso
            '                            o.Nach.Date <= prevWindowEnd)

            'Dim currentWindowHoursAll = currentWindow.Sum(Function(o) CSng(o.PCh))
            'Dim currentWindowPlanHours = currentWindow.Where(Function(o) HasTransferPlan(o)).Sum(Function(o) CSng(o.PCh))
            'Dim currentWindowHoursWithoutPlan = currentWindowHoursAll - currentWindowPlanHours
            'Dim previousYearWindowHours = previousYearWindow.Sum(Function(o) CSng(o.PCh))

            'TxtYtdMonthCur.Text = $"{monthNumber}м ТГ"
            'TxtYtdMonthPrev.Text = $"{monthNumber}м ПГ"
            'TxtYtdHoursCur.Text = F2(currentWindowHoursWithoutPlan)
            'TxtYtdHoursPrev.Text = F2(previousYearWindowHours)
            'TxtYtdHoursDiff.Text = F2(currentWindowHoursWithoutPlan - previousYearWindowHours)
            'TxtYtdHoursDiff.ToolTip = $"{F2(currentWindowHoursAll)} ({curWindowStart:dd.MM.yy}-{curWindowEnd:dd.MM.yy}) − {F2(currentWindowPlanHours)} (план) − {F2(previousYearWindowHours)} ({prevWindowStart:dd.MM.yy}-{prevWindowEnd:dd.MM.yy})"

            ' ===== цели по количеству =====
            TxtOtsMonthPrev.Text = $"ОТС {monthNameShort} ПГ"
            TxtOtsMonthPrevVal.Text = _prev.Where(Function(o) o.KomplexAsInt > 0).Count.ToString()
            TxtOtsMonthGoal.Text = $"Цель {monthNameShort} ТГ"
            TxtOtsMonthGoalVal.Text = GoalCountSum(New Date(currentYear, periodEndDate.Month, 1), periodEndDate).ToString()

            TxtOtsYtdPrev.Text = $"ОТС {monthNumber}м ПГ"
            TxtOtsYtdPrevVal.Text = previousYearWindow.Count().ToString()
            TxtOtsYtdGoal.Text = $"Цель {monthNumber}м ТГ"
            TxtOtsYtdGoalVal.Text = GoalCountSum(curWindowStart, curWindowEnd).ToString()

            ' ===== цели по часам =====
            Dim monthGoalHours = GoalHoursSum(New Date(currentYear, periodEndDate.Month, 1), periodEndDate)
            Dim periodGoalHours = GoalHoursSum(curWindowStart, curWindowEnd)

            TxtHoursMonthGoalVal.Text = If(monthGoalHours = 0, "", F2(monthGoalHours))
            TxtHoursYtdGoalVal.Text = If(periodGoalHours = 0, "", F2(periodGoalHours))

            ' ===== дороги: цель и факт из JSON =====
            Dim roadGoals = RoadGoalsStore.Load()
            Dim roadGoalsForMonth = roadGoals?.FirstOrDefault(Function(x) x.Year = periodEndDate.Year AndAlso x.Month = periodEndDate.Month)

            TxtRoadVsjdCelOts.Text = If(roadGoalsForMonth Is Nothing OrElse roadGoalsForMonth.VsjdOtsGoal = 0, "", roadGoalsForMonth.VsjdOtsGoal.ToString())
            TxtRoadZabdCelOts.Text = If(roadGoalsForMonth Is Nothing OrElse roadGoalsForMonth.ZabdOtsGoal = 0, "", roadGoalsForMonth.ZabdOtsGoal.ToString())
            TxtRoadDvdCelOts.Text = If(roadGoalsForMonth Is Nothing OrElse roadGoalsForMonth.DvdOtsGoal = 0, "", roadGoalsForMonth.DvdOtsGoal.ToString())

            TxtRoadVsjd.Text = If(roadGoalsForMonth Is Nothing OrElse roadGoalsForMonth.VsjdOtsFact = 0, "", roadGoalsForMonth.VsjdOtsFact.ToString())
            TxtRoadZabd.Text = If(roadGoalsForMonth Is Nothing OrElse roadGoalsForMonth.ZabdOtsFact = 0, "", roadGoalsForMonth.ZabdOtsFact.ToString())
            TxtRoadDvd.Text = If(roadGoalsForMonth Is Nothing OrElse roadGoalsForMonth.DvdOtsFact = 0, "", roadGoalsForMonth.DvdOtsFact.ToString())

            TxtRoadVsjdCelPch.Text = If(roadGoalsForMonth Is Nothing OrElse roadGoalsForMonth.VsjdHoursGoal = 0, "", F2(roadGoalsForMonth.VsjdHoursGoal))
            TxtRoadZabdCelPch.Text = If(roadGoalsForMonth Is Nothing OrElse roadGoalsForMonth.ZabdHoursGoal = 0, "", F2(roadGoalsForMonth.ZabdHoursGoal))
            TxtRoadDvdCelPch.Text = If(roadGoalsForMonth Is Nothing OrElse roadGoalsForMonth.DvdHoursGoal = 0, "", F2(roadGoalsForMonth.DvdHoursGoal))

            TxtRoadHoursVsjd.Text = If(roadGoalsForMonth Is Nothing OrElse roadGoalsForMonth.VsjdHoursFact = 0, "", F2(roadGoalsForMonth.VsjdHoursFact))
            TxtRoadHoursZabd.Text = If(roadGoalsForMonth Is Nothing OrElse roadGoalsForMonth.ZabdHoursFact = 0, "", F2(roadGoalsForMonth.ZabdHoursFact))
            TxtRoadHoursDvd.Text = If(roadGoalsForMonth Is Nothing OrElse roadGoalsForMonth.DvdHoursFact = 0, "", F2(roadGoalsForMonth.DvdHoursFact))

        End Sub


        Private Function HasTransferPlan(o As Otkaz) As Boolean
            Return o.Plan IsNot Nothing AndAlso
                   o.Plan.Any(Function(p) String.Equals(p.Description, "На др дорогу", StringComparison.OrdinalIgnoreCase))
        End Function

        Private Function HoursSum(list As IEnumerable(Of Otkaz), excludePlan As Boolean) As Double
            Dim q = list.Where(Function(o) o.KomplexAsInt > 0)      ' ← только отнесённые на комплексы
            If excludePlan Then q = q.Where(Function(o) Not HasTransferPlan(o))
            Return q.Sum(Function(o) CSng(o.PCh))
        End Function

        Private Function GoalCountSum(startDate As Date, endDate As Date) As Integer
            Dim goals = GoalsStore.Load()
            If goals Is Nothing Then Return 0

            Dim sum As Integer = 0
            Dim cur = New Date(startDate.Year, startDate.Month, 1)
            Dim last = New Date(endDate.Year, endDate.Month, 1)

            While cur <= last
                Dim y = cur.Year, m = cur.Month
                For Each g In goals.Where(Function(x) x.Year = y AndAlso x.Month = m)
                    sum += g.Complex13
                Next
                cur = cur.AddMonths(1)
            End While
            Return sum
        End Function



        Private Function GoalHoursSum(startDate As Date, endDate As Date) As Single
            Dim goals = GoalHoursStore.Load()
            If goals Is Nothing Then Return 0

            Dim sum As Single = 0
            Dim cur = New Date(startDate.Year, startDate.Month, 1)
            Dim last = New Date(endDate.Year, endDate.Month, 1)

            While cur <= last
                For Each g In goals.Where(Function(x) x.Year = cur.Year AndAlso x.Month = cur.Month)
                    sum += g.TotalAll
                Next
                cur = cur.AddMonths(1)
            End While
            Return sum
        End Function






#Region "ПЛАНЫ"

        Private Sub FillPlanTable()
            Dim kmp = {1, 2, 3, 5, 7}

            For i = 0 To 4
                CType(FindName($"LblPlanR{i + 1}"), TextBlock).Text = Otkaz.GetTCHE_Name(kmp(i))
                FillPlanRow(i + 1, _cur.Where(Function(o) o.KomplexAsInt = kmp(i)).ToList())
            Next

            ' Всего — по всем комплексным
            FillPlanRow(6, _cur.Where(Function(o) o.KomplexAsInt > 0).ToList())
        End Sub

        Private Sub FillPlanRow(row As Integer, list As List(Of Otkaz))
            CType(FindName($"PlanR{row}C1"), TextBlock).Text = PlanPair(list, "др дорогу")
            CType(FindName($"PlanR{row}C2"), TextBlock).Text = PlanPair(list, "технолог")
            CType(FindName($"PlanR{row}C3"), TextBlock).Text = PlanPair(list, "корректировка")
            CType(FindName($"PlanR{row}C4"), TextBlock).Text = PlanPairAny(list)
        End Sub

        Private Function PlanPair(list As List(Of Otkaz), pattern As String) As String
            Dim q = list.Where(Function(o) HasPlan(o, pattern)).ToList()
            If q.Count = 0 Then Return ""
            Return $"{q.Count}/{F2(q.Sum(Function(o) CSng(o.PCh)))}"
        End Function

        Private Function PlanPairAny(list As List(Of Otkaz)) As String
            Dim q = list.Where(Function(o) HasPlan(o, "др дорогу") OrElse
                                           HasPlan(o, "технолог") OrElse
                                           HasPlan(o, "корректировка")).ToList()
            If q.Count = 0 Then Return ""
            Return $"{q.Count}/{F2(q.Sum(Function(o) CSng(o.PCh)))}"
        End Function

        Private Function HasPlan(o As Otkaz, pattern As String) As Boolean
            Return o.Plan IsNot Nothing AndAlso
                   o.Plan.Any(Function(p) p.Description IsNot Nothing AndAlso
                                          p.Description.ToLower().Contains(pattern))
        End Function


#End Region



        ' ==========================================
        ' СВОЙСТВА И МЕТОДЫ ДЛЯ ЭКСПОРТА В PDF
        ' ==========================================

        Private _isExportEnabled As Boolean = True
        Public Property IsExportEnabled As Boolean
            Get
                Return _isExportEnabled
            End Get
            Set(valueToSet As Boolean)
                _isExportEnabled = valueToSet
                ' Обновляем состояние кнопки, если она уже инициализирована
                If BtnExportToPdf IsNot Nothing Then
                    BtnExportToPdf.IsEnabled = valueToSet
                End If
            End Set
        End Property

        ''' <summary>
        ''' Обработчик нажатия на кнопку "Выдать в PDF"
        ''' </summary>
        Private Sub BtnExportToPdf_Click(sender As Object, e As RoutedEventArgs)
            Dim folderPath As String = My.Settings.ReportFolderPath

            If String.IsNullOrEmpty(folderPath) Then
                MessageBox.Show("Папка для отчётов не задана в настройках.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error)
                Return
            End If

            If Not Directory.Exists(folderPath) Then
                Directory.CreateDirectory(folderPath)
            End If

            Dim filePath As String = Path.Combine(folderPath, $"Справка по комплексам_{DateTime.Now:yyyyMMdd_HHmmss}.pdf")

            ButtonSPanel.Visibility = Visibility.Collapsed
            Me.UpdateLayout()

            Try
                SaveVisualsToPdf(filePath, Me)
            Finally
                ButtonSPanel.Visibility = Visibility.Visible
                Me.UpdateLayout()
            End Try
        End Sub






        ''' <summary>
        ''' Рендерит указанные объекты, объединяет их в одну картинку и кладёт на ОДНУ страницу PDF.
        ''' </summary>
        Private Sub SaveVisualsToPdf(destinationPath As String, targetVisual As Visual)


            Try
                Dim targetElement As FrameworkElement = TryCast(targetVisual, FrameworkElement)
                If targetElement Is Nothing Then Return

                ' 1. Измеряем элемент полностью (игнорирует ограничения родителя)
                targetElement.Measure(New Size(Double.PositiveInfinity, Double.PositiveInfinity))
                targetElement.Arrange(New Rect(0, 0, targetElement.DesiredSize.Width, targetElement.DesiredSize.Height))

                Dim elementWidth As Double = targetElement.DesiredSize.Width
                Dim elementHeight As Double = targetElement.DesiredSize.Height

                If elementWidth <= 0 OrElse elementHeight <= 0 Then Return

                ' 2. Рендерим в битмап с высоким DPI (300) для максимальной чёткости
                Dim dpi As Double = 300.0
                Dim pixelWidth As Integer = CInt(Math.Ceiling(elementWidth * dpi / 96.0))
                Dim pixelHeight As Integer = CInt(Math.Ceiling(elementHeight * dpi / 96.0))

                Dim bitmap As New RenderTargetBitmap(pixelWidth, pixelHeight, dpi, dpi, PixelFormats.Pbgra32)
                bitmap.Render(targetElement)

                ' 3. Кодируем в PNG
                Dim pngEncoder As New PngBitmapEncoder()
                pngEncoder.Frames.Add(BitmapFrame.Create(bitmap))

                Dim imageBytes As Byte()
                Using memoryStream As New MemoryStream()
                    pngEncoder.Save(memoryStream)
                    imageBytes = memoryStream.ToArray()
                End Using

                ' 4. Создаём PDF A4 альбомный
                Using writerStream As New FileStream(destinationPath, FileMode.Create, FileAccess.Write)
                    Dim pdfWriter As New PdfWriter(writerStream)
                    Dim pdfDocument As New PdfDocument(pdfWriter)
                    pdfDocument.SetDefaultPageSize(PageSize.A4.Rotate())

                    Dim document As New Document(pdfDocument)
                    document.SetMargins(10, 10, 10, 10)

                    Dim imageData As ImageData = ImageDataFactory.Create(imageBytes)
                    Dim pdfImage As New PdfImage(imageData)

                    Dim availableWidth As Single = pdfDocument.GetDefaultPageSize().GetWidth() - 20
                    Dim availableHeight As Single = pdfDocument.GetDefaultPageSize().GetHeight() - 20
                    pdfImage.ScaleToFit(availableWidth, availableHeight)

                    document.Add(pdfImage)
                    document.Close()
                End Using

                ' 5. Открываем папку с сохранённым файлом
                Dim folderPath As String = System.IO.Path.GetDirectoryName(destinationPath)
                If Not String.IsNullOrEmpty(folderPath) AndAlso System.IO.Directory.Exists(folderPath) Then
                    Dim processInfo As New ProcessStartInfo()
                    processInfo.FileName = folderPath
                    processInfo.UseShellExecute = True
                    Process.Start(processInfo)
                End If

            Catch ex As Exception
                MessageBox.Show($"Не удалось сохранить PDF." & Environment.NewLine & "Ошибка: {ex.Message}",
                        "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End Sub





    End Class

End Namespace


