
Imports System.Runtime.InteropServices
Imports Microsoft.Office.Interop

Namespace Kas

    Module DailyExplan


        Sub DailyPoyasnilka(LST As List(Of Otkaz))

            If LST Is Nothing OrElse LST.Count = 0 Then Return

            Dim ctParas As List(Of WordPara) = BuildCtReport(LST)
            Dim wordParas As List(Of WordPara) = BuildWordDescription(LST)

            Dim allParas As New List(Of WordPara)
            allParas.AddRange(ctParas)
            allParas.Add(New WordPara())      ' пустая строка-разделитель
            allParas.AddRange(wordParas)

            InsertParagraphsToWord(allParas)

        End Sub




        Public Function BuildCtReport(LST As List(Of Otkaz)) As List(Of WordPara)

            Dim paras As New List(Of WordPara)

            If LST Is Nothing Then
                paras.Add(New WordPara("Нет данных для отчета."))
                Return paras
            End If



            Dim reportDate As Date = Fetcher.KonDatTim
            If reportDate = Date.MinValue Then
                paras.Add(New WordPara("Ошибка: не определена дата конца периода (Fetcher)."))
                Return paras
            End If

            Dim ruCulture = Globalization.CultureInfo.GetCultureInfo("ru-RU")

            ' === 1. СУТОЧНЫЕ ПОКАЗАТЕЛИ ===
            'paras.Add(BuildDailyLine(LST, reportDate))


            Dim actualCount As Integer = LST.Count
            Dim actualPCh As Single = LST.Sum(Function(o) o.PCh)

            Dim cat12 As Integer = LST.Where(Function(o) o.Kat <= 2).Count
            Dim cat3 As Integer = LST.Where(Function(o) o.Kat = 3).Count

            Dim allGoals = GoalsStore.Load()
            Dim allHourGoals = GoalHoursStore.Load()
            Dim goalCount = allGoals.FirstOrDefault(Function(g) g.Year = reportDate.Year AndAlso g.Month = reportDate.Month)
            Dim goalHours = allHourGoals.FirstOrDefault(Function(g) g.Year = reportDate.Year AndAlso g.Month = reportDate.Month)

            Dim dailyThreshCount As Integer = If(goalCount IsNot Nothing, goalCount.DailyThreshold, 0)
            Dim dailyThreshHours As Single = If(goalHours IsNot Nothing, goalHours.DailyThreshold, 0)

            Dim diffC As Integer = actualCount - dailyThreshCount
            Dim diffH As Single = actualPCh - dailyThreshHours
            Dim signC As String = If(diffC > 0, "+", "")
            Dim signH As String = If(diffH > 0, "+", "")



            ' Один абзац: жирный лид + всё остальное обычным
            paras.Add(New WordPara(
        $"За сутки {reportDate:dd.MM.yy}г.",
        $" допущено {actualCount} ОТС на {actualPCh.ToString("F2", ruCulture)}ч. " &
        $"({signC}{diffC} к суточному пороговому {dailyThreshCount} ОТС и " &
        $"{signH}{diffH.ToString("F2", ruCulture)}ч. к суточному пороговому {dailyThreshHours}ч). " &
        $"Из них {cat12} ОТС 1,2 категории, {cat3} ОТС 3 категории."))

            'paras.Add(New WordPara(
            '"Суточные пороговые значения по количеству отказов и потерь поездо-часов " &
            '"установлены распоряжением от 31.07.26г. №ЦТ-232/р."))

            paras.Add(New WordPara()) ' пустая строка



            ' === 2. СПИСОК МЕСТ ОТКАЗОВ ===
            paras.Add(New WordPara("Отказы допущены на следующих перегонах и станциях:", boldOnly:=True))

            For Each o In LST
                If String.IsNullOrEmpty(o.Id) Then Continue For
                Dim placeName As String = If(String.IsNullOrWhiteSpace(o.MestoOTS), "НЕ УКАЗАНО", o.MestoOTS.ToUpper())
                paras.Add(New WordPara($"{placeName} – {o.PCh.ToString("F2", ruCulture)}ч"))
            Next

            paras.Add(New WordPara()) ' пустая строка

            ' === ЗАГРУЗКА ПОЛНОГО СПИСКА ===
            Dim tempFullList As New List(Of Otkaz)
            If Not String.IsNullOrEmpty(My.Settings.OldYJSON) Then
                Dim oldData = StorageModule.LoadFromJson(My.Settings.OldYJSON)
                If oldData IsNot Nothing Then tempFullList.AddRange(oldData)
            End If
            If Not String.IsNullOrEmpty(My.Settings.THISYJSON) Then
                Dim thisYData = StorageModule.LoadFromJson(My.Settings.THISYJSON)
                If thisYData IsNot Nothing Then tempFullList.AddRange(thisYData)
            End If
            If OTSList IsNot Nothing Then tempFullList.AddRange(OTSList)

            Dim cleanList = tempFullList.GroupBy(Function(o) o.Id).Select(Function(g) g.First()).ToList()

            ' === 3. НАРАСТАЮЩИЙ ИТОГ ЗА ТЕКУЩИЙ МЕСЯЦ ===
            Dim startOfMonth As New Date(reportDate.Year, reportDate.Month, 1)
            Dim endOfCurrentDay As Date = reportDate

            Dim monthList = cleanList.Where(Function(o)
                                                Return o.Nach >= startOfMonth AndAlso
                                               o.Nach <= endOfCurrentDay AndAlso
                                               o.KomplexAsInt > 0
                                            End Function).ToList()

            Dim cumCount As Integer = monthList.Count
            Dim cumPCh As Single = monthList.Sum(Function(o) o.PCh)

            Dim daysPassed As Integer = (reportDate.Day - startOfMonth.Day) + 1
            Dim planCumCount As Integer = dailyThreshCount * daysPassed
            Dim planCumHours As Single = dailyThreshHours * daysPassed

            Dim cumDiffC As Integer = cumCount - planCumCount
            Dim cumDiffH As Single = cumPCh - planCumHours
            Dim signCC As String = If(cumDiffC > 0, "+", "")
            Dim signCH As String = If(cumDiffH > 0, "+", "")

            Dim currentMonthName As String = reportDate.ToString("MMMM yyyy", ruCulture)

            paras.Add(New WordPara(
        $"За {currentMonthName}",
        $" допущено {cumCount} ОТС на {cumPCh.ToString("F2", ruCulture)}ч. " &
        $"({signCC}{cumDiffC} к целевому значению {planCumCount} ОТС и " &
        $"{signCH}{cumDiffH.ToString("F2", ruCulture)}ч. к целевому значению {planCumHours}ч)."))



            ' === АНАЛОГИЧНЫЙ МЕСЯЦ ПРОШЛОГО ГОДА ===
            Dim prevYear As Integer = reportDate.Year - 1
            Dim prevYearStart As New Date(prevYear, reportDate.Month, 1)
            Dim prevYearEnd As Date = prevYearStart.AddMonths(1).AddTicks(-1)

            Dim prevYearList = cleanList.Where(Function(o)
                                                   Return o.Nach >= prevYearStart AndAlso
                                                  o.Nach <= prevYearEnd AndAlso
                                                  o.KomplexAsInt > 0
                                               End Function).ToList()

            Dim prevYearCount As Integer = prevYearList.Count
            Dim prevYearPCh As Single = prevYearList.Sum(Function(o) o.PCh)
            Dim prevYearMonthName As String = prevYearStart.ToString("MMMM yyyy", ruCulture)

            paras.Add(New WordPara(
        $"За {prevYearMonthName}",
        $" допущено {prevYearCount} ОТС на {prevYearPCh.ToString("F2", ruCulture)}ч"))

            paras.Add(New WordPara())

            ' === ЕСЛИ ДЕНЬ <= 13, ДОБАВЛЯЕМ ПРОШЛЫЙ МЕСЯЦ ===
            If reportDate.Day <= 13 Then
                Dim prevDate As Date = startOfMonth.AddMonths(-1)
                Dim prevStart As New Date(prevDate.Year, prevDate.Month, 1)
                Dim prevEnd As Date = prevStart.AddMonths(1).AddTicks(-1)

                Dim prevGoalCount = allGoals.FirstOrDefault(Function(g) g.Year = prevDate.Year AndAlso g.Month = prevDate.Month)
                Dim prevGoalHours = allHourGoals.FirstOrDefault(Function(g) g.Year = prevDate.Year AndAlso g.Month = prevDate.Month)

                Dim prevMonthList = cleanList.Where(Function(o)
                                                        Return o.Nach >= prevStart AndAlso
                                                   o.Nach <= prevEnd AndAlso
                                                   o.KomplexAsInt > 0
                                                    End Function).ToList()

                Dim prevCount As Integer = prevMonthList.Count
                Dim prevPCh As Single = prevMonthList.Sum(Function(o) o.PCh)

                Dim prevPlanCount As Integer = If(prevGoalCount IsNot Nothing, prevGoalCount.Complex13, 0)
                Dim prevPlanHours As Single = If(prevGoalHours IsNot Nothing, prevGoalHours.TotalAll, 0)

                Dim prevDiffC As Integer = prevCount - prevPlanCount
                Dim prevDiffH As Single = prevPCh - prevPlanHours
                Dim prevSignC As String = If(prevDiffC > 0, "+", "")
                Dim prevSignH As String = If(prevDiffH > 0, "+", "")

                Dim prevMonthName As String = prevDate.ToString("MMMM yyyy", ruCulture)

                Dim tail As String = ""
                If prevPlanCount > 0 OrElse prevPlanHours > 0 Then
                    tail = $" ({prevSignC}{prevDiffC} к целевому значению {prevPlanCount} ОТС и " &
                   $"{prevSignH}{prevDiffH.ToString("F2", ruCulture)}ч. к целевому значению {prevPlanHours}ч)"
                End If

                paras.Add(New WordPara(
            $"За {prevMonthName}",
            $" допущено {prevCount} ОТС на {prevPCh.ToString("F2", ruCulture)}ч{tail}"))

                ' Аналог прошлого года для прошлого месяца
                Dim prevYearPrevMonthDate As Date = prevDate.AddYears(-1)
                Dim prevYearPrevMonthStart As New Date(prevYearPrevMonthDate.Year, prevYearPrevMonthDate.Month, 1)
                Dim prevYearPrevMonthEnd As Date = prevYearPrevMonthStart.AddMonths(1).AddTicks(-1)

                Dim prevYearPrevMonthList = cleanList.Where(Function(o)
                                                                Return o.Nach >= prevYearPrevMonthStart AndAlso
                                                           o.Nach <= prevYearPrevMonthEnd AndAlso
                                                           o.KomplexAsInt > 0
                                                            End Function).ToList()

                Dim prevYearPrevMonthCount As Integer = prevYearPrevMonthList.Count
                Dim prevYearPrevMonthPCh As Single = prevYearPrevMonthList.Sum(Function(o) o.PCh)
                Dim prevYearPrevMonthName As String = prevYearPrevMonthDate.ToString("MMMM yyyy", ruCulture)

                paras.Add(New WordPara(
            $"За {prevYearPrevMonthName}",
            $" допущено {prevYearPrevMonthCount} ОТС на {prevYearPrevMonthPCh.ToString("F2", ruCulture)}ч"))
            End If

            Return paras

        End Function


        '''' <summary>
        '''' Формирует абзац «За сутки ... допущено ...» с учётом суточных порогов.
        '''' </summary>
        'Private Function BuildDailyLine(LST As List(Of Otkaz), reportDate As Date) As WordPara

        '    Dim ruCulture = Globalization.CultureInfo.GetCultureInfo("ru-RU")

        '    Dim actualCount As Integer = LST.Count
        '    Dim actualPCh As Single = LST.Sum(Function(o) o.PCh)

        '    Dim cat12 As Integer = LST.Where(Function(o) o.Kat <= 2).Count
        '    Dim cat3 As Integer = LST.Where(Function(o) o.Kat = 3).Count

        '    Dim allGoals = GoalsStore.Load()
        '    Dim allHourGoals = GoalHoursStore.Load()
        '    Dim goalCount = allGoals.FirstOrDefault(Function(g) g.Year = reportDate.Year AndAlso g.Month = reportDate.Month)
        '    Dim goalHours = allHourGoals.FirstOrDefault(Function(g) g.Year = reportDate.Year AndAlso g.Month = reportDate.Month)

        '    Dim dailyThreshCount As Integer = If(goalCount IsNot Nothing, goalCount.DailyThreshold, 0)
        '    Dim dailyThreshHours As Single = If(goalHours IsNot Nothing, goalHours.DailyThreshold, 0)

        '    Dim diffC As Integer = actualCount - dailyThreshCount
        '    Dim diffH As Single = actualPCh - dailyThreshHours
        '    Dim signC As String = If(diffC > 0, "+", "")
        '    Dim signH As String = If(diffH > 0, "+", "")

        '    Return New WordPara(
        '$"За сутки {reportDate:dd.MM.yy}г.",
        '$" допущено {actualCount} ОТС на {actualPCh.ToString("F2", ruCulture)}ч. " &
        '$"({signC}{diffC} к суточному пороговому {dailyThreshCount} ОТС и " &
        '$"{signH}{diffH.ToString("F2", ruCulture)}ч. к суточному пороговому {dailyThreshHours}ч). " &
        '$"Из них {cat12} ОТС 1,2 категории, {cat3} ОТС 3 категории.")
        'End Function





        Public Function BuildWordDescription(LST As List(Of Otkaz)) As List(Of WordPara)

            Dim paras As New List(Of WordPara)

            If LST Is Nothing OrElse LST.Count = 0 Then
                paras.Add(New WordPara("Нет данных для описания."))
                Return paras
            End If

            Dim ruCulture = Globalization.CultureInfo.GetCultureInfo("ru-RU")

            ' --- Заголовок СПРАВОЧНО: жирный, по центру ---
            paras.Add(New WordPara("СПРАВОЧНО", boldOnly:=True,
                           align:=Word.WdParagraphAlignment.wdAlignParagraphCenter))
            paras.Add(New WordPara())

            For Each o In LST
                If String.IsNullOrEmpty(o.Id) Then Continue For

                ' --- Заголовок места: жирный, по центру ---
                paras.Add(New WordPara($"{o.MestoOTS} – {o.PCh.ToString("F2", ruCulture)}ч",
                               boldOnly:=True,
                               align:=Word.WdParagraphAlignment.wdAlignParagraphCenter))

                ' --- Описание отказа: обычным, слева ---
                Dim Look As String = If(o.SerLokPripLokInRowTXT <> "", $"{o.SerLokPripLokInRowTXT},", "")
                Dim Maash As String = If(o.MashPripInRowTXT <> "", $"машинист {o.MashPripInRowTXT},", "")

                Dim descLine As String = $"{Look} {Maash} {o.Opis}".Trim()

                Try
                    Dim lokData As String = FormatLokData(o.DaNaLok)
                    If Not String.IsNullOrWhiteSpace(lokData) Then
                        descLine &= $" {lokData}"
                    End If
                Catch ex As Exception
                    ' Игнорируем ошибки форматирования ремонтов
                End Try

                paras.Add(New WordPara(descLine))
                paras.Add(New WordPara()) ' пустая строка между отказами
            Next

            Return paras

        End Function







        ''' <summary>
        ''' Создаёт новый документ Word, вставляет в него абзацы и оставляет открытым.
        ''' Word и документ НЕ закрываются — пользователь сам сохранит/закроет.
        ''' </summary>
        Public Sub InsertParagraphsToWord(paras As List(Of WordPara))
            If paras Is Nothing OrElse paras.Count = 0 Then Return

            Dim app As Word.Application = Nothing
            Dim doc As Word.Document = Nothing

            Try
                app = New Word.Application()
                doc = app.Documents.Add()

                ' === Поля 5 мм со всех сторон ===
                With doc.PageSetup
                    .LeftMargin = app.CentimetersToPoints(0.5)   ' 5 мм
                    .RightMargin = app.CentimetersToPoints(0.5)
                    .TopMargin = app.CentimetersToPoints(0.5)
                    .BottomMargin = app.CentimetersToPoints(0.5)
                End With




                For Each p In paras

                    ' --- Пустой абзац ---
                    If String.IsNullOrEmpty(p.Lead) AndAlso String.IsNullOrEmpty(p.Text) Then
                        Dim rngEmpty As Word.Range = doc.Content
                        rngEmpty.Collapse(Word.WdCollapseDirection.wdCollapseEnd)

                        If p.PageBreakBefore Then
                            ' Настоящий разрыв страницы — без лишнего пустого абзаца
                            rngEmpty.InsertBreak(Word.WdBreakType.wdPageBreak)
                        Else
                            rngEmpty.InsertParagraphAfter()
                            If p.FirstLineIndent > 0 Then
                                rngEmpty.ParagraphFormat.FirstLineIndent = p.FirstLineIndent
                            End If
                            If p.LeftIndent > 0 Then
                                rngEmpty.ParagraphFormat.LeftIndent = p.LeftIndent
                            End If
                        End If

                        Marshal.ReleaseComObject(rngEmpty)
                        Continue For
                    End If

                    Dim rngStart As Word.Range = doc.Content
                    rngStart.Collapse(Word.WdCollapseDirection.wdCollapseEnd)
                    Dim startPos As Integer = rngStart.End
                    Marshal.ReleaseComObject(rngStart)

                    ' --- Жирный лид ---
                    If Not String.IsNullOrEmpty(p.Lead) Then
                        Dim rng As Word.Range = doc.Content
                        rng.Collapse(Word.WdCollapseDirection.wdCollapseEnd)
                        rng.Text = p.Lead
                        rng.Bold = 1
                        rng.Font.Size = p.FontSize
                        Marshal.ReleaseComObject(rng)
                    End If

                    ' --- Основной текст ---
                    If Not String.IsNullOrEmpty(p.Text) Then
                        Dim rng2 As Word.Range = doc.Content
                        rng2.Collapse(Word.WdCollapseDirection.wdCollapseEnd)
                        rng2.Text = p.Text
                        rng2.Bold = If(p.BoldOnly, 1, 0)
                        rng2.Italic = If(p.ItalicOnly, 1, 0)
                        rng2.Font.Size = p.FontSize
                        Marshal.ReleaseComObject(rng2)
                    End If

                    ' --- Перевод строки ---
                    Dim rngEnd As Word.Range = doc.Content
                    rngEnd.Collapse(Word.WdCollapseDirection.wdCollapseEnd)
                    rngEnd.InsertParagraphAfter()
                    Dim endPos As Integer = rngEnd.End
                    Marshal.ReleaseComObject(rngEnd)

                    ' --- Выравнивание + разрыв страницы ---
                    Dim rngFmt As Word.Range = doc.Range(startPos, endPos)

                    If p.Alignment <> Word.WdParagraphAlignment.wdAlignParagraphLeft Then
                        rngFmt.ParagraphFormat.Alignment = p.Alignment
                    End If
                    If p.PageBreakBefore Then
                        rngFmt.ParagraphFormat.PageBreakBefore = True
                    End If
                    If p.FirstLineIndent > 0 Then
                        rngFmt.ParagraphFormat.FirstLineIndent = p.FirstLineIndent
                    End If
                    If p.LeftIndent > 0 Then
                        rngFmt.ParagraphFormat.LeftIndent = p.LeftIndent
                    End If

                    Marshal.ReleaseComObject(rngFmt)
                Next
                app.Visible = True
                doc.Activate()

            Finally
                If doc IsNot Nothing Then Marshal.ReleaseComObject(doc)
                If app IsNot Nothing Then Marshal.ReleaseComObject(app)
            End Try


        End Sub
    End Module

End Namespace



