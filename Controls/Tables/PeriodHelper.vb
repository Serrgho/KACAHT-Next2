Imports KACAHT_Next2.Kas

Module PeriodHelper

    ' ===== РУЧНОЙ ПРОШЛЫЙ ПЕРИОД =====
    Private _oldNach As Date = Date.MinValue
    Private _oldKon As Date = Date.MinValue


    ''' <summary>Прошлый период: из настроек, если задан, иначе авто-сдвиг</summary>
    Public Function GetPrevPeriod(periodStart As Date, periodEnd As Date,
                              oldY As List(Of Otkaz)) As (Start As Date, [End] As Date)

        If HasOldPeriod Then
            Return (My.Settings.nachOLDPeriod, My.Settings.konOLDPeriod)
        End If

        Return AutoPrevPeriod(periodStart, periodEnd, oldY)
    End Function

    Public ReadOnly Property HasOldPeriod As Boolean
        Get
            Return _oldNach <> Date.MinValue AndAlso _oldKon <> Date.MinValue
        End Get
    End Property


    ''' <summary>Автоматический сдвиг на прошлый год (как раньше)</summary>
    Public Function AutoPrevPeriod(periodStart As Date, periodEnd As Date,
                               oldY As List(Of Otkaz)) As (Start As Date, [End] As Date)

        Dim prevYearData As Integer = oldY _
            .Where(Function(o) o.Nach.Year < periodStart.Year) _
            .Select(Function(o) o.Nach.Year) _
            .DefaultIfEmpty(periodStart.Year - 1).Max()

        Dim yearOffset As Integer = prevYearData - periodStart.Year

        Return (periodStart.AddYears(yearOffset), ShiftYearKeepMonthEnd(periodEnd, yearOffset))
    End Function


    ''' <summary>
    ''' Сдвигает дату на другой год; если дата — последний день месяца,
    ''' то и в целевом году берёт последний день месяца (високосная страховка)
    ''' </summary>
    Public Function ShiftYearKeepMonthEnd(d As Date, years As Integer) As Date
        Dim shifted As Date = d.AddYears(years)

        If d.Day = DateTime.DaysInMonth(d.Year, d.Month) Then
            shifted = New Date(shifted.Year, shifted.Month,
                               DateTime.DaysInMonth(shifted.Year, shifted.Month))
        End If

        Return shifted
    End Function



    ''' <summary>
    ''' Отказы за период с учетом o.Uchet
    ''' </summary>
    Public Function GetUchetOtkazy(list As List(Of Otkaz), st As Date, en As Date) As List(Of Otkaz)
        If list Is Nothing Then Return New List(Of Otkaz)()

        Return list.Where(Function(o)
                              Dim d = o.Nach.Date
                              Return d >= st.Date AndAlso d <= en.Date AndAlso o.Uchet
                          End Function).ToList()
    End Function


    Public Function GetFullCurrentYearOtkazy(otsList As List(Of Otkaz), oldY As List(Of Otkaz),
                                          periodStart As Date, periodEnd As Date) As List(Of Otkaz)

        Dim combined As New List(Of Otkaz)()

        If oldY IsNot Nothing Then combined.AddRange(oldY)                 ' ← история (23–25)

        If Not String.IsNullOrEmpty(My.Settings.THISYJSON) Then
            Dim thisY = StorageModule.LoadFromJson(My.Settings.THISYJSON)
            If thisY IsNot Nothing Then combined.AddRange(thisY)
        End If

        If otsList IsNot Nothing Then combined.AddRange(otsList)

        Return combined.GroupBy(Function(o) o.Id) _
                      .Select(Function(g) g.First()) _
                      .Where(Function(o)
                                 Dim d = o.Nach
                                 Return d >= periodStart AndAlso d <= periodEnd
                             End Function).ToList()


        'Return combined.GroupBy(Function(o) o.Id) _
        '               .Select(Function(g) g.First()) _
        '               .Where(Function(o)
        '                          Dim d = o.Nach.Date
        '                          Return d >= periodStart.Date AndAlso d <= periodEnd.Date
        '                      End Function).ToList()
    End Function

    Public Function GetYearToDateOtkazy(otsList As List(Of Otkaz), oldY As List(Of Otkaz)) As List(Of Otkaz)
        Dim combined As New List(Of Otkaz)()

        If oldY IsNot Nothing Then combined.AddRange(oldY)

        If Not String.IsNullOrEmpty(My.Settings.THISYJSON) Then
            Dim thisY = StorageModule.LoadFromJson(My.Settings.THISYJSON)
            If thisY IsNot Nothing Then combined.AddRange(thisY)
        End If

        If otsList IsNot Nothing Then combined.AddRange(otsList)

        Return combined.GroupBy(Function(o) o.Id) _
                       .Select(Function(g) g.First()) _
                       .ToList()
    End Function





End Module
