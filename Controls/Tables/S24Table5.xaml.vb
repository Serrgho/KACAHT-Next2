Imports System.Globalization
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media

Namespace Kas

    Partial Public Class S24Table5

        Private _cur As List(Of Otkaz)
        Private _prev As List(Of Otkaz)
        Private _periodStart As Date?
        Private _periodEnd As Date?
        Private _prevRaw As List(Of Otkaz) ' Сырой список ПГ для расчета полного месяца

        Private Shared ReadOnly Ru As CultureInfo = CultureInfo.GetCultureInfo("ru-RU")

        ' ==================== КОНСТРУКТОР ====================

        Public Sub New(currentYearOtkazy As List(Of Otkaz),
                       previousYearOtkazy As List(Of Otkaz),
                       Optional periodStart As Date? = Nothing,
                       Optional periodEnd As Date? = Nothing)

            InitializeComponent()

            _periodStart = periodStart
            _periodEnd = periodEnd
            _prevRaw = previousYearOtkazy ' Сохраняем сырой список для корректного расчета ПГ

            ' ===== ФИЛЬТРАЦИЯ ТЕКУЩЕГО ГОДА =====
            If periodStart.HasValue AndAlso periodEnd.HasValue Then
                Dim nextDayAfterEnd = periodEnd.Value.Date.AddDays(1)
                _cur = currentYearOtkazy.Where(Function(o)
                                                   Dim d = o.Nach.Date
                                                   Return d >= periodStart.Value.Date AndAlso d < nextDayAfterEnd
                                               End Function).ToList()

                ' ===== ФИЛЬТРАЦИЯ ПРОШЛОГО ГОДА: ЛОГИКА "ПОЛНОГО МЕСЯЦА" КАК В T2 =====
                Dim oldP = GetPrevPeriod(periodStart.Value.Date, periodEnd.Value.Date, previousYearOtkazy)
                Dim prevYear As Integer = oldP.End.Year
                Dim isMultiMonth As Boolean = (periodEnd.Value.Month - periodStart.Value.Month +
                                              (periodEnd.Value.Year - periodStart.Value.Year) * 12) > 1

                Dim prevWindowStart As Date
                Dim prevWindowEnd As Date

                If isMultiMonth Then
                    ' Несколько месяцев: берем аналогичный период прошлого года
                    prevWindowStart = oldP.Start.Date
                    prevWindowEnd = New Date(prevYear, oldP.End.Month, DateTime.DaysInMonth(prevYear, oldP.End.Month))
                Else
                    ' Один месяц: берем ВЕСЬ месяц прошлого года (как TxtMonthHoursPrev в T2)
                    Dim targetMonth As Integer = periodEnd.Value.Month
                    prevWindowStart = New Date(prevYear, targetMonth, 1)
                    prevWindowEnd = New Date(prevYear, targetMonth, DateTime.DaysInMonth(prevYear, targetMonth))
                End If

                Dim nextDayAfterOldEnd = prevWindowEnd.Date.AddDays(1)
                _prev = previousYearOtkazy.Where(Function(o)
                                                     Dim d = o.Nach.Date
                                                     Return d >= prevWindowStart AndAlso d < nextDayAfterOldEnd
                                                 End Function).ToList()
            Else
                _cur = currentYearOtkazy
                _prev = previousYearOtkazy
            End If

        End Sub

        ' ==================== ЗАГРУЗКА ====================

        Private Sub S24Table5_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
            Try
                If _cur Is Nothing OrElse _prev Is Nothing Then Return
                FillCaption()
                FillDataRows()
            Catch ex As Exception
                MessageBox.Show(ex.ToString(), "Ошибка в S24Table5")
            End Try
        End Sub

        ' ==================== ПРЕДИКАТЫ (ИЗ S24TABLE4) ====================

        Private Function IsTrpu(o As Otkaz) As Boolean
            Return o.KtoZakryl IsNot Nothing AndAlso o.KtoZakryl.ToLower().Contains("трп")
        End Function

        Private Function IsFact(o As Otkaz, k As Integer?, group As String) As Boolean
            If k.HasValue AndAlso o.KomplexAsInt <> k.Value Then Return False
            Select Case group
                Case "tr"
                    Return IsTrpu(o) AndAlso o.Uchet
                Case "sld"
                    Return o.ZaKem IsNot Nothing AndAlso o.ZaKem.ToLower.Contains("слд")
                Case "zav"
                    Return o.ZaKem IsNot Nothing AndAlso (o.ZaKem.ToLower.Contains("авод") OrElse o.ZaKem.ToLower.Contains("проч"))
                Case Else ' tche
                    Return Not IsTrpu(o) AndAlso (o.VRassled OrElse (o.ZaKem IsNot Nothing AndAlso o.ZaKem.ToLower().Contains("тч")))
            End Select
        End Function

        Private Function FactHours(list As List(Of Otkaz)) As Double
            If list Is Nothing Then Return 0
            Return list.Sum(Function(o) CSng(o.PCh))
        End Function

        Private Function F2(v As Double) As String
            Return v.ToString("F2", Ru)
        End Function

        Private Function DiffText(curVal As Double, prevVal As Double) As String
            If curVal = 0 AndAlso prevVal = 0 Then Return ""
            Dim diff = curVal - prevVal
            Dim pct As Double = 0
            If prevVal <> 0 Then pct = diff / prevVal * 100
            Return $"{F2(diff)} ({pct:F1} %)"
        End Function

        ' ==================== ЗАПОЛНЕНИЕ ИНТЕРФЕЙСА ====================

        Private Sub FillCaption()
            If _periodEnd Is Nothing Then
                LblCaption.Text = "Потери поездо-часов за период из-за ОТС всех категорий"
                Return
            End If

            Dim periodText As String = ""
            If _periodStart.HasValue AndAlso _periodEnd.HasValue Then
                If _periodStart.Value.Month = _periodEnd.Value.Month Then
                    Dim monthName = Ru.DateTimeFormat.GetMonthName(_periodStart.Value.Month)
                    periodText = $"{monthName} {_periodStart.Value.Year}"
                Else
                    Dim startMonth = Ru.DateTimeFormat.GetMonthName(_periodStart.Value.Month).ToLower()
                    Dim endMonth = Ru.DateTimeFormat.GetMonthName(_periodEnd.Value.Month).ToLower()
                    periodText = $"{startMonth} - {endMonth} {_periodEnd.Value.Year}"
                End If
            Else
                Dim monthName = Ru.DateTimeFormat.GetMonthName(_periodEnd.Value.Month)
                periodText = $"{monthName} {_periodEnd.Value.Year}"
            End If

            LblCaption.Text = $"Потери поездо-часов за {periodText} из-за ОТС всех категорий"
        End Sub

        Private Sub FillDataRows()
            ' ===== 1. ЗАПОЛНЕНИЕ ЛЕЙБЛОВ С ГОДАМИ =====
            If _periodEnd.HasValue Then
                ' Текущий год: берем из конца выбранного периода
                Dim curYear As Integer = _periodEnd.Value.Year
                LblCurYear.Text = $"{curYear} год"

                ' Прошлый год: вычисляем на основе реального окна сравнения (_prev)
                ' Если _prev пуст, fallback на curYear - 1
                Dim prevYear As Integer = curYear - 1
                If _prev IsNot Nothing AndAlso _prev.Count > 0 Then
                    ' Берем год первого элемента в отфильтрованном списке прошлого периода
                    prevYear = _prev.First().Nach.Year
                End If
                LblPrevYear.Text = $"{prevYear} год"
            Else
                LblCurYear.Text = "Текущий период"
                LblPrevYear.Text = "Прошлый период"
            End If

            ' ===== 2. АГРЕГАЦИЯ ДАННЫХ =====
            Dim groups As String() = {"tche", "tr", "sld", "zav"}

            ' Текущий год
            Dim listT = _cur.Where(Function(o) IsFact(o, Nothing, "tche")).ToList()
            Dim listTR = _cur.Where(Function(o) IsFact(o, Nothing, "tr")).ToList()
            Dim listSLD = _cur.Where(Function(o) IsFact(o, Nothing, "sld")).ToList()
            Dim listZav = _cur.Where(Function(o) IsFact(o, Nothing, "zav")).ToList()

            R1C1.Text = F2(FactHours(listT) + FactHours(listTR) + FactHours(listSLD) + FactHours(listZav))
            R1C2.Text = F2(FactHours(listT))
            R1C3.Text = F2(FactHours(listTR))
            R1C4.Text = F2(FactHours(listSLD))
            R1C5.Text = F2(FactHours(listZav))

            ' Прошлый год
            Dim prevListT = _prev.Where(Function(o) IsFact(o, Nothing, "tche")).ToList()
            Dim prevListTR = _prev.Where(Function(o) IsFact(o, Nothing, "tr")).ToList()
            Dim prevListSLD = _prev.Where(Function(o) IsFact(o, Nothing, "sld")).ToList()
            Dim prevListZav = _prev.Where(Function(o) IsFact(o, Nothing, "zav")).ToList()

            R2C1.Text = F2(FactHours(prevListT) + FactHours(prevListTR) + FactHours(prevListSLD) + FactHours(prevListZav))
            R2C2.Text = F2(FactHours(prevListT))
            R2C3.Text = F2(FactHours(prevListTR))
            R2C4.Text = F2(FactHours(prevListSLD))
            R2C5.Text = F2(FactHours(prevListZav))

            ' ===== 3. ИТОГОВАЯ СТРОКА (Разница) =====
            Dim curTotal = FactHours(listT) + FactHours(listTR) + FactHours(listSLD) + FactHours(listZav)
            Dim prevTotal = FactHours(prevListT) + FactHours(prevListTR) + FactHours(prevListSLD) + FactHours(prevListZav)

            SetFinalRowCell("R3C1", FormatDiffWithPercent(curTotal, prevTotal))
            SetFinalRowCell("R3C2", FormatDiffWithPercent(FactHours(listT), FactHours(prevListT)))
            SetFinalRowCell("R3C3", FormatDiffWithPercent(FactHours(listTR), FactHours(prevListTR)))
            SetFinalRowCell("R3C4", FormatDiffWithPercent(FactHours(listSLD), FactHours(prevListSLD)))
            SetFinalRowCell("R3C5", FormatDiffWithPercent(FactHours(listZav), FactHours(prevListZav)))
        End Sub


        ' ===== НОВАЯ ЛОГИКА ФОРМАТИРОВАНИЯ (КАК В T1) =====
        ''' <summary>
        ''' Формат: "-81 (-35,5%)" или "в +2,1р" если рост > 100%
        ''' </summary>
        Private Function FormatDiffWithPercent(curVal As Double, prevVal As Double) As String
            If curVal = 0 AndAlso prevVal = 0 Then Return ""

            Dim diff As Double = curVal - prevVal

            ' Форматируем абсолютную разницу со знаком
            Dim diffStr As String = If(diff >= 0, $"+{F2(diff)}", F2(diff))

            ' Если база 0
            If prevVal = 0 Then
                If curVal > 0 Then
                    Return $"{diffStr} (+100%)"
                End If
                Return ""
            End If

            Dim pct As Double = diff / prevVal * 100

            ' Если рост больше 100% — показываем в разах
            If pct > 100 Then
                Dim ratio As Double = curVal / prevVal
                Return $"{diffStr} (в +{Math.Round(ratio, 1).ToString("F1", Ru)}р)"
            End If

            ' Обычный формат: число (процент)
            Dim sign As String = If(pct >= 0, "+", "")
            Return $"{diffStr} ({sign}{Math.Round(pct, 1).ToString("F1", Ru)}%)"
        End Function

        ' ===== РАСКРАСКА ЯЧЕЕК (КАК В T1) =====

        Private Sub SetColoredText(tb As TextBlock, value As String)
            tb.Text = value
            If String.IsNullOrEmpty(value) Then
                tb.Foreground = Brushes.Black
                Return
            End If

            ' Логика из GetColorForChange в S24Table1
            If value.Contains("+") Then
                tb.Foreground = Brushes.Red      ' Рост / Ухудшение
            ElseIf value.StartsWith("-") Then
                tb.Foreground = Brushes.Green    ' Снижение / Улучшение
            Else
                tb.Foreground = Brushes.Black    ' Ноль или текст без знака
            End If
        End Sub


        ''' <summary>
        ''' Применяет цвет и ЖИРНОСТЬ к ячейке итоговой строки
        ''' </summary>
        Private Sub SetFinalRowCell(cellName As String, value As String)
            Dim tb = TryCast(FindName(cellName), TextBlock)
            If tb Is Nothing Then Return

            tb.Text = value
            tb.FontWeight = FontWeights.Bold ' <-- ЖИРНЫЙ ТЕКСТ КАК В T1 СТРОКА 7

            If String.IsNullOrEmpty(value) Then
                tb.Foreground = Brushes.Black
                Return
            End If

            ' Раскраска по знаку (как в GetColorForChange из T1)
            If value.Contains("+") Then
                tb.Foreground = Brushes.Red      ' Рост потерь - плохо
            ElseIf value.StartsWith("-") Then
                tb.Foreground = Brushes.Green    ' Снижение потерь - хорошо
            Else
                tb.Foreground = Brushes.Black
            End If
        End Sub

        Private Sub S24Table5_Unloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded

            _cur = Nothing
            _prev = Nothing
            _prevRaw = Nothing

            ' ВЫЗЫВАЕМ ТВОЙ УНИВЕРСАЛЬНЫЙ ОТПИСЧИК
            UnsubscribeAllEvents(Me)



            ' ОТПИСЫВАЕМСЯ ОТ Unloaded (ЧТОБЫ НЕ БЫЛО ЦИКЛИЧЕСКИХ ССЫЛОК)
            RemoveHandler Me.Unloaded, AddressOf S24Table5_Unloaded

            GC.Collect()
        End Sub
    End Class

End Namespace