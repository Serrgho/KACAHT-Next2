
Namespace Kas


	Partial Public Class S24Table4
		Friend Shared ReadOnly Ru As New System.Globalization.CultureInfo("ru-RU")
		Private _periodStart As Date?
		Private _periodEnd As Date?
		Private _cur As List(Of Otkaz)
		Private _cells(7, 12) As TextBlock

		' «Заводы»: точный список ZaKem
		Private Shared ReadOnly ZavKemList As New List(Of String) From
			{"Завод", "прочие"}

		Public Sub New(currentYearOtkazy As List(Of Otkaz),
					   Optional periodStart As Date? = Nothing,
					   Optional periodEnd As Date? = Nothing)

			InitializeComponent()

			_periodStart = periodStart
			_periodEnd = periodEnd

			' фильтрация по периоду — как в таблицах 1/2/3
			If periodStart IsNot Nothing OrElse periodEnd IsNot Nothing Then
				Dim nextDayAfterEnd = If(periodEnd.HasValue, periodEnd.Value.Date.AddDays(1), Date.MaxValue)

				_cur = currentYearOtkazy.Where(Function(o)
												   Dim d = o.Nach.Date
												   If periodStart IsNot Nothing AndAlso d < periodStart.Value.Date Then Return False
												   If d >= nextDayAfterEnd Then Return False
												   Return True
											   End Function).ToList()
			Else
				_cur = currentYearOtkazy
			End If

			' ▼▼▼ целевые по часам: грузим ДО заполнения строк ▼▼▼
			_allGoalHours = GoalHoursStore.Load()
			If _allGoalHours Is Nothing Then _allGoalHours = New List(Of MonthlyGoalHours)()

			BuildDataRows()
			FillAll()
		End Sub

		' ===== ПРЕДИКАТЫ =====
		Private Function IsTrpu(o As Otkaz) As Boolean
			Return o.KtoZakryl IsNot Nothing AndAlso o.KtoZakryl.ToLower().Contains("трп")
		End Function



		' ===== ФАКТ, часы =====


		Private Function IsFact(o As Otkaz, k As Integer?, group As String) As Boolean
			If k.HasValue AndAlso o.KomplexAsInt <> k.Value Then Return False
			Select Case group
				Case "tr"   ' ТРПУшные
					Return IsTrpu(o) AndAlso o.Uchet
				Case "sld"  ' расследован/сохранен за СЛД — в колонку СЛД своего комплекса
					Return o.ZaKem IsNot Nothing AndAlso o.ZaKem.ToLower.Contains("слд")
				Case "zav"  ' заводы: точный список
					Return o.ZaKem IsNot Nothing AndAlso (o.ZaKem.ToLower.Contains("авод") OrElse o.ZaKem.ToLower.Contains("проч"))
				Case Else   ' ТЧЭ: в расследовании (ZaKem ""/"!") или отнесён на ТЧЭ (ZaKem "тч")
					Return Not IsTrpu(o) AndAlso (o.VRassled OrElse (o.ZaKem IsNot Nothing AndAlso o.ZaKem.ToLower().Contains("тч")))
			End Select
		End Function

		Private Function FactHours(k As Integer?, group As String) As Single
			Return _cur.Where(Function(o) IsFact(o, k, group)).Sum(Function(o) o.PCh)
		End Function

		Private Function FactIdList(k As Integer?, group As String) As List(Of Otkaz)
			Return _cur.Where(Function(o) IsFact(o, k, group)).ToList()
		End Function


		' ===== ЯЧЕЙКИ =====
		Private Sub BuildDataRows()
			For row As Integer = 2 To 7
				For col As Integer = 1 To 12
					Dim b As New Border()
					'b.Style = CType(FindResource(If(col = 5 OrElse col = 10, "TotalBorder", "CellBorder")), Style)
					b.Style = CType(FindResource(If(row = 7 OrElse col = 5 OrElse col = 10, "TotalBorder", "CellBorder")), Style)

					Dim t As New TextBlock()
					t.Style = CType(FindResource("CellStyle"), Style)
					AddHandler t.MouseLeftButtonDown, AddressOf FactCell_Click
					b.Child = t
					Grid.SetRow(b, row)
					Grid.SetColumn(b, col)
					MainGrid.Children.Add(b)
					_cells(row, col) = t
				Next
			Next
		End Sub


		Private Sub FactCell_Click(sender As Object, e As MouseButtonEventArgs)
			Dim tb = TryCast(sender, TextBlock) : If tb Is Nothing Then Return
			Dim list = TryCast(tb.Tag, List(Of Otkaz))
			If list Is Nothing OrElse list.Count = 0 Then Return
			Clipboard.SetText(String.Join(",", list.Select(Function(o) o.Id)))
			MW.ShowOtkazyList(list)
			e.Handled = True
		End Sub








		Private Sub SetH(row As Integer, col As Integer, v As Single)
			_cells(row, col).Text = If(v = 0, "", v.ToString("F2", Ru))
		End Sub

		' ===== ЗАПОЛНЕНИЕ =====
		Private Sub FillAll()
			If _periodEnd Is Nothing Then Return

			LblCaption.Text = $"Целевое задание по снижению потерь поездо-часов за " &
							  $"{Ru.DateTimeFormat.GetMonthName(_periodEnd.Value.Month)} от отказов всех категорий"

			Dim kmp = {1, 2, 3, 5, 7}
			For i = 0 To 4
				CType(FindName($"LblDep{i + 1}"), TextBlock).Text = Otkaz.GetTCHE_Name(kmp(i))
				FillRow(i + 2, kmp(i))
			Next
			FillRow(7, Nothing)   ' Всего
		End Sub

		Private Sub FillRow(row As Integer, k As Integer?)

			' ФАКТ + списки отказов для drill-down
			Dim listT = FactIdList(k, "tche")
			Dim listS = FactIdList(k, "sld")
			Dim listZ = FactIdList(k, "zav")
			Dim listR = FactIdList(k, "tr")
			Dim listAll = listT.Concat(listS).Concat(listZ).Concat(listR).ToList()

			SetFactCell(row, 1, listT.Sum(Function(o) o.PCh), listT)
			SetFactCell(row, 2, listS.Sum(Function(o) o.PCh), listS)
			SetFactCell(row, 3, listZ.Sum(Function(o) o.PCh), listZ)
			SetFactCell(row, 4, listR.Sum(Function(o) o.PCh), listR)
			SetFactCell(row, 5, listAll.Sum(Function(o) o.PCh), listAll)

			' ЦЕЛЬ из хранилища (нули гасятся в пусто через SetH)
			Dim gT = GetGoalHours(k, "tche")
			Dim gS = GetGoalHours(k, "sld")
			Dim gZ = GetGoalHours(k, "zav")
			Dim gR = GetGoalHours(k, "tr")
			Dim gAll = gT + gS + gZ + gR

			SetH(row, 6, gT)
			SetH(row, 7, gS)
			SetH(row, 8, gZ)
			SetH(row, 9, gR)
			SetH(row, 10, gAll)

			' ИЗМЕНЕНИЕ: часы (0 → пусто, положительные с «+»)
			Dim ch = gAll + (listAll.Sum(Function(o) o.PCh)) - 2 * gAll   ' = fAll - gAll
			ch = listAll.Sum(Function(o) o.PCh) - gAll
			_cells(row, 11).Text = If(ch = 0, "", ch.ToString("+0.00;-0.00;0.00", Ru))

			' ИЗМЕНЕНИЕ: % / разы
			Dim fAll = listAll.Sum(Function(o) o.PCh)
			Dim pctText As String = ""
			If gAll > 0 Then
				Dim pct = (fAll - gAll) / gAll * 100
				If pct > 100 Then
					pctText = (fAll / gAll).ToString("0.0", Ru) & " р."
				Else
					pctText = pct.ToString("+0.0;-0.0;0.0", Ru)
				End If
			ElseIf fAll <> 0 Then
				pctText = "+100,0"
			End If
			_cells(row, 12).Text = pctText

			'Dim tb = _cells(row, col)
			'tb.Text = If(hours = 0, "", hours.ToString("F2", Ru))

			'If List Is Nothing OrElse List.Count = 0 Then
			'	tb.Tag = Nothing
			'	tb.Cursor = Cursors.Arrow
			'	tb.FontWeight = FontWeights.Normal
			'	tb.Foreground = Brushes.Black
			'Else
			'	tb.Tag = String.Join(",", List.Select(Function(o) o.Id))   ' без пробелов
			'	tb.Cursor = Cursors.Hand
			'	tb.FontWeight = FontWeights.Bold
			'	tb.Foreground = Brushes.DarkBlue
			'End If

			'' факт: ТЧЭ / СЛД / Заводы / ТР / Всего
			'Dim fT = FactHours(k, "tche")
			'Dim fS = FactHours(k, "sld")
			'Dim fZ = FactHours(k, "zav")
			'Dim fR = FactHours(k, "tr")
			'Dim fAll = fT + fS + fZ + fR

			'SetH(row, 1, fT)
			'SetH(row, 2, fS)
			'SetH(row, 3, fZ)
			'SetH(row, 4, fR)
			'SetH(row, 5, fAll)


			'' ЦЕЛЬ: пока 0 — колонки 6-10 не пишем вообще
			'Dim gT = GetGoalHours(k, "tche")
			'Dim gS = GetGoalHours(k, "sld")
			'Dim gZ = GetGoalHours(k, "zav")
			'Dim gR = GetGoalHours(k, "tr")
			'Dim gAll = gT + gS + gZ + gR

			'SetH(row, 6, gT)
			'SetH(row, 7, gS)
			'SetH(row, 8, gZ)
			'SetH(row, 9, gR)
			'SetH(row, 10, gAll)

			'' ИЗМЕНЕНИЕ: считается ВСЕГДА
			'Dim ch = fAll - gAll
			'_cells(row, 11).Text = If(ch = 0, "", ch.ToString("+0.00;-0.00;0.00", Ru))

			'' ИЗМЕНЕНИЕ: % / разы
			'Dim pctText As String = ""
			'If gAll > 0 Then
			'	Dim pct = ch / gAll * 100
			'	If pct > 100 Then
			'		pctText = (fAll / gAll).ToString("0.0", Ru) & " р."
			'	Else
			'		pctText = pct.ToString("+0.0;-0.0;0.0", Ru)
			'	End If
			'ElseIf ch <> 0 Then
			'	pctText = "+100,0"
			'End If
			'_cells(row, 12).Text = pctText

		End Sub



		Private Sub SetFactCell(row As Integer, col As Integer, hours As Single, list As List(Of Otkaz))
			Dim tb = _cells(row, col)
			tb.Text = If(hours = 0, "", hours.ToString("F2", Ru))
			If list Is Nothing OrElse list.Count = 0 Then
				tb.Tag = Nothing
				tb.Cursor = Cursors.Arrow
				tb.FontWeight = FontWeights.Normal
				tb.Foreground = Brushes.Black
			Else
				tb.Tag = list
				tb.Cursor = Cursors.Hand
				tb.FontWeight = FontWeights.Bold
				tb.Foreground = Brushes.DarkBlue
			End If
		End Sub






		Private _allGoalHours As List(Of MonthlyGoalHours)

		Private Function GetGoalHours(k As Integer?, group As String) As Single
			If _periodStart Is Nothing OrElse _periodEnd Is Nothing Then Return 0
			If _allGoalHours Is Nothing OrElse _allGoalHours.Count = 0 Then Return 0

			Dim suffix As String
			Select Case group
				Case "tche" : suffix = "Tche"
				Case "sld" : suffix = "Sld"
				Case "zav" : suffix = "Zav"
				Case Else : suffix = "Tr"
			End Select

			Dim st = New Date(If(_periodStart, _periodEnd).Value.Year, If(_periodStart, _periodEnd).Value.Month, 1)
			Dim en = New Date(_periodEnd.Value.Year, _periodEnd.Value.Month, 1)

			Dim sum As Single = 0
			Dim cur = st
			While cur <= en
				Dim g = _allGoalHours.FirstOrDefault(Function(x) x.Year = cur.Year AndAlso x.Month = cur.Month)
				If g IsNot Nothing Then
					If k.HasValue Then
						Dim p = GetType(MonthlyGoalHours).GetProperty($"D{k.Value}{suffix}")
						sum += CSng(p.GetValue(g))
					Else
						Select Case group
							Case "tche" : sum += g.TotalTche
							Case "sld" : sum += g.TotalSld
							Case "zav" : sum += g.TotalZav
							Case Else : sum += g.TotalTr
						End Select
					End If
				End If
				cur = cur.AddMonths(1)
			End While
			Return sum
		End Function


		Sub New()

			' Этот вызов является обязательным для конструктора.
			InitializeComponent()

			' Добавить код инициализации после вызова InitializeComponent().

		End Sub

	End Class

End Namespace


