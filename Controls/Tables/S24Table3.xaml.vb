

Imports System.Text

Namespace Kas

	Partial Public Class S24Table3

		Private _periodStart As Date?
		Private _periodEnd As Date?
		Private _cur As List(Of Otkaz)
		Private _prev As List(Of Otkaz)
		Private _cells(7, 8) As TextBlock

		Public Sub New(currentYearOtkazy As List(Of Otkaz),
						   previousYearOtkazy As List(Of Otkaz),
						   Optional periodStart As Date? = Nothing,
						   Optional periodEnd As Date? = Nothing)

			InitializeComponent()

			_periodStart = periodStart
			_periodEnd = periodEnd

			' ===== ФИЛЬТРАЦИЯ — один в один как в таблицах 1/2 =====
			If periodStart IsNot Nothing OrElse periodEnd IsNot Nothing Then
				Dim nextDayAfterEnd = If(periodEnd.HasValue, periodEnd.Value.Date.AddDays(1), Date.MaxValue)

				_cur = currentYearOtkazy.Where(Function(o)
												   Dim d = o.Nach.Date
												   If periodStart IsNot Nothing AndAlso d < periodStart.Value.Date Then Return False
												   If d >= nextDayAfterEnd Then Return False
												   Return True
											   End Function).ToList()

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

			BuildDataRows()
			FillAll()
		End Sub

		' ===== ПРЕДИКАТЫ =====
		'

		' в расследовании: не закрыт и не передан, ВКЛЮЧАЯ сохраненные за СЛД/Заводы
		Private Function IsRassled(o As Otkaz) As Boolean
			Return (o.VRassled OrElse o.IsSaved)
		End Function

		' левая часть: отнесенные на ТЧЭ + находящиеся в расследовании (три+ состояния ZaKem)
		Private Function IsTchState(o As Otkaz) As Boolean
			Return o.VRassled 'OrElse o.IsSaved 
		End Function


		' средняя: в расследовании и более N суток с момента Nach НА СЕГОДНЯ
		Private Function IsOverDays(o As Otkaz, days As Integer) As Boolean
			Return o.DaysOnRassled > days AndAlso Not (o.KtoZakryl.ToLower.Contains("трп"))
			'Return o.VRassled AndAlso o.Zakryt = Date.MinValue AndAlso
			'  o.DaysOnRassled > days
		End Function

		' правая: учтено за депо
		Private Function IsUchteno(o As Otkaz) As Boolean
			Return o.ZaKem IsNot Nothing AndAlso o.ZaKem.ToLower().Contains("тч")
		End Function

		Private Function ByDepo(list As List(Of Otkaz), k As Integer?) As IEnumerable(Of Otkaz)
			If Not k.HasValue Then Return list
			Return list.Where(Function(o) o.KomplexAsInt = k.Value)
		End Function

		Private Function CntInv(k As Integer?, cat As Func(Of Otkaz, Boolean)) As Integer
			Return ByDepo(_cur, k).Count(Function(o) IsTchState(o) AndAlso (cat Is Nothing OrElse cat(o)))
		End Function

		Private Function ListInv(k As Integer?, cat As Func(Of Otkaz, Boolean)) As List(Of Otkaz)
			Return ByDepo(_cur, k).Where(Function(o) IsTchState(o) AndAlso (cat Is Nothing OrElse cat(o))).ToList()
		End Function



		'Private Function CntDays(k As Integer?, days As Integer) As Integer
		'	Return ByDepo(_cur, k).Count(Function(o) IsOverDays(o, days))
		'End Function

		Private Function CntAttr(k As Integer?, isCurrentYear As Boolean, cat As Func(Of Otkaz, Boolean)) As Integer
			Dim list = If(isCurrentYear, _cur, _prev)
			Return ByDepo(list, k).Count(Function(o) IsUchteno(o) AndAlso (cat Is Nothing OrElse cat(o)))
		End Function

		Private Function ListAttr(k As Integer?, isCurrentYear As Boolean, cat As Func(Of Otkaz, Boolean)) As List(Of Otkaz)
			Dim list = If(isCurrentYear, _cur, _prev)
			Return ByDepo(list, k).Where(Function(o) IsUchteno(o) AndAlso (cat Is Nothing OrElse cat(o))).ToList()
		End Function



		Private Shared ReadOnly Cat12 As Func(Of Otkaz, Boolean) = Function(o) o.Kat <= 2
		Private Shared ReadOnly Cat3 As Func(Of Otkaz, Boolean) = Function(o) o.Kat = 3

		' ===== ЯЧЕЙКИ ДАННЫХ (создаются кодом, шапка — в XAML) =====
		Private Sub BuildDataRows()
			For row As Integer = 2 To 7
				For col As Integer = 1 To 8
					Dim b As New Border()
					b.Style = CType(FindResource(If(row = 7, "TotalBorder", "CellBorder")), Style)
					Dim t As New TextBlock()
					t.Style = CType(FindResource("CellStyle"), Style)
					AddHandler t.MouseLeftButtonDown, AddressOf AttrCell_Click
					b.Child = t
					Grid.SetRow(b, row)
					Grid.SetColumn(b, col)
					MainGrid.Children.Add(b)
					_cells(row, col) = t
				Next
			Next
		End Sub

		Private Sub AttrCell_Click(sender As Object, e As MouseButtonEventArgs)
			Dim tb = TryCast(sender, TextBlock)
			If tb Is Nothing Then Return

			Dim list = TryCast(tb.Tag, List(Of Otkaz))
			If list Is Nothing OrElse list.Count = 0 Then Return

			Clipboard.SetText(String.Join(",", list.Select(Function(o) o.Id)))
			MW.ShowOtkazyList(list)
			e.Handled = True
		End Sub



		Private Sub SetCell(row As Integer, col As Integer, v As Integer)
			_cells(row, col).Text = If(v = 0, "", v.ToString())          ' ноль — пустая ячейка
		End Sub

		Private Sub SetCellList(row As Integer, col As Integer, list As List(Of Otkaz))
			Dim tb = _cells(row, col)
			tb.Text = If(list.Count = 0, "", list.Count.ToString())
			If list.Count > 0 Then
				tb.Tag = list
				tb.Cursor = Cursors.Hand
				tb.FontWeight = FontWeights.Bold
				tb.Foreground = Brushes.DarkBlue
			Else
				tb.Tag = Nothing
				tb.Cursor = Cursors.Arrow
				tb.FontWeight = FontWeights.Normal
				tb.Foreground = Brushes.Black
			End If
		End Sub



		Private Sub SetCellPair(row As Integer, col As Integer, curList As List(Of Otkaz), prev As Integer)
			Dim tb = _cells(row, col)
			Dim cur = curList.Count
			tb.Text = If(cur = 0 AndAlso prev = 0, "", $"{cur}/{prev}")
			If cur > 0 Then
				tb.Tag = curList
				tb.Cursor = Cursors.Hand
				tb.FontWeight = FontWeights.Bold
				tb.Foreground = Brushes.DarkBlue
			Else
				tb.Tag = Nothing
				tb.Cursor = Cursors.Arrow
				tb.FontWeight = FontWeights.Normal
				tb.Foreground = Brushes.Black
			End If
		End Sub

		' ===== ЗАПОЛНЕНИЕ =====
		Private Sub FillAll()
			If _periodEnd Is Nothing Then Return

			LblCaption.Text = $"В расследовании и учтено за депо на {_periodEnd.Value:dd.MM.yy}"

			Dim kmp = {1, 2, 3, 5, 7}
			For i = 0 To 4
				CType(FindName($"LblDep{i + 1}"), TextBlock).Text = Otkaz.GetTCHE_Name(kmp(i))
				FillRow(i + 2, kmp(i))
			Next
			FillRow(7, Nothing)   ' Всего
		End Sub

		' Добавлен новый метод для получения списка отказов, превышающих N суток
		Private Function ListDays(k As Integer?, days As Integer) As List(Of Otkaz)
			Return ByDepo(_cur, k).Where(Function(o) IsOverDays(o, days)).ToList()
		End Function


		Private Sub FillRow(row As Integer, k As Integer?)
			' в расследовании: 1,2 кат / 3 кат / 1-3 кат
			SetCellList(row, 1, ListInv(k, Cat12))
			SetCellList(row, 2, ListInv(k, Cat3))
			SetCellList(row, 3, ListInv(k, Nothing))

			' более 3-х / более 10 суток
			SetCellList(row, 4, ListDays(k, 3))
			SetCellList(row, 5, ListDays(k, 10))
			' учтено за депо: тек год / прошлый год
			SetCellPair(row, 6, ListAttr(k, True, Cat12), CntAttr(k, False, Cat12))
			SetCellPair(row, 7, ListAttr(k, True, Cat3), CntAttr(k, False, Cat3))
			SetCellPair(row, 8, ListAttr(k, True, Nothing), CntAttr(k, False, Nothing))

			' ======================================

			' Подписка на выделение столбцов для ВСЕХ ячеек данных
			If row >= 2 AndAlso row <= 6 Then
				For col As Integer = 1 To 8
					Dim border = FindChildByGridCoords(MainGrid, row, col)
					If border IsNot Nothing Then
						Dim tb = TryCast(border.Child, TextBlock)
						If tb IsNot Nothing Then
							' Обязательно для корректной работы MouseEnter
							tb.Background = Brushes.Transparent

							AddHandler tb.MouseEnter, AddressOf Cell_MouseEnterForSelection
							AddHandler tb.MouseLeave, AddressOf Cell_MouseLeaveForSelection
							AddHandler tb.PreviewMouseLeftButtonDown, AddressOf Cell_PreviewMouseDownForSelection
							AddHandler tb.MouseRightButtonDown, AddressOf CopyColumnToClipboard_Click
						End If
					End If
				Next
			End If
		End Sub

		' Список ячеек текущего выделенного столбца
		Private _selectedCells As New List(Of TextBlock)()

		' Цвет выделения (синий, полупрозрачный, как в Excel)
		Private ReadOnly SelectionBrush As New SolidColorBrush(Color.FromArgb(60, 128, 128, 128))
		Private ReadOnly DefaultBrush As New SolidColorBrush(Colors.Transparent)


		''' <summary>
		''' Ищет Border в MainGrid по заданным строке и колонке
		''' </summary>
		Private Function FindChildByGridCoords(grid As Grid, row As Integer, col As Integer) As Border
			For Each child In grid.Children
				If TypeOf child Is Border Then
					Dim b = CType(child, Border)
					If Grid.GetRow(b) = row AndAlso Grid.GetColumn(b) = col Then
						Return b
					End If
				End If
			Next
			Return Nothing
		End Function










		''' <summary>
		''' Копирует ВЫДЕЛЕННЫЙ СТОЛБЕЦ в буфер обмена (вертикальный список)
		''' </summary>
		Private Sub CopyColumnToClipboard_Click(sender As Object, e As MouseButtonEventArgs)


			If _selectedCells.Count = 0 Then
				MessageBox.Show("Сначала выделите столбец мышью!", "Внимание",
						MessageBoxButton.OK, MessageBoxImage.Warning)
				e.Handled = True
				Return
			End If

			' Определяем номер выделенной колонки
			Dim firstCell = _selectedCells.FirstOrDefault()
			Dim parentBorder = TryCast(firstCell?.Parent, Border)
			Dim selectedCol As Integer = If(parentBorder IsNot Nothing, Grid.GetColumn(parentBorder), -1)

			Dim sb As New System.Text.StringBuilder()

			Dim sortedCells = _selectedCells.OrderBy(Function(c)
														 Dim b = TryCast(c.Parent, Border)
														 If b IsNot Nothing Then Return Grid.GetRow(b)
														 Return 0
													 End Function).ToList()

			For i As Integer = 0 To sortedCells.Count - 1
				Dim cellText As String = sortedCells(i).Text

				' === ИСПРАВЛЕНИЕ: ЗНАЧЕНИЕ + ПУСТОЙ TAB ДЛЯ ОБЪЕДИНЕННЫХ КОЛОНОК ===
				If selectedCol = 1 OrElse selectedCol = 2 Then
					' Одно значение + таб + пустота. Excel не разъединяет ячейку!
					sb.Append(cellText & vbTab)
				Else
					' Обычные колонки - просто значение
					sb.Append(cellText)
				End If
				' ================================================================

				If i < sortedCells.Count - 1 Then sb.Append(vbCrLf)
			Next

			Clipboard.SetText(sb.ToString())
			e.Handled = True
		End Sub




		''' <summary>
		''' Подсвечивает весь столбец (строки 2-6) указанной колонки
		''' </summary>
		Private Sub HighlightColumn(col As Integer)
			' Снимаем предыдущее выделение
			For Each cell In _selectedCells
				cell.Background = DefaultBrush
			Next
			_selectedCells.Clear()

			' Если колонка вне диапазона данных - выходим
			If col < 1 OrElse col > 8 Then Return

			' Собираем и красим ячейки нового столбца
			For r As Integer = 2 To 6
				Dim border = FindChildByGridCoords(MainGrid, r, col)
				If border IsNot Nothing Then
					Dim tb = TryCast(border.Child, TextBlock)
					If tb IsNot Nothing Then
						tb.Background = SelectionBrush
						_selectedCells.Add(tb)
					End If
				End If
			Next
		End Sub

		''' <summary>
		''' Полная очистка выделения
		''' </summary>
		Private Sub ClearSelection()
			For Each cell In _selectedCells
				cell.Background = DefaultBrush
			Next
			_selectedCells.Clear()
		End Sub

		''' <summary>
		''' При наведении на любую ячейку - подсвечиваем её столбец
		''' </summary>
		Private Sub Cell_MouseEnterForSelection(sender As Object, e As MouseEventArgs)
			Dim tb = TryCast(sender, TextBlock)
			If tb Is Nothing Then Return

			Dim parentBorder = TryCast(tb.Parent, Border)
			If parentBorder Is Nothing Then Return

			Dim col = Grid.GetColumn(parentBorder)
			HighlightColumn(col)
		End Sub

		''' <summary>
		''' При уходе мыши с таблицы - снимаем выделение
		''' </summary>
		Private Sub Cell_MouseLeaveForSelection(sender As Object, e As MouseEventArgs)

			' Если выделения нет - выходить нечего
			If _selectedCells.Count = 0 Then Return

			Dim mousePos = Mouse.GetPosition(MainGrid)

			' Берем первую (верхнюю) и последнюю (нижнюю) ячейку из текущего выделения
			' _selectedCells уже отсортирован по строкам в HighlightColumn
			Dim firstCell = _selectedCells.First()
			Dim lastCell = _selectedCells.Last()

			Dim parentFirst = TryCast(firstCell.Parent, Border)
			Dim parentLast = TryCast(lastCell.Parent, Border)

			If parentFirst Is Nothing OrElse parentLast Is Nothing Then Return

			' Вычисляем реальные границы выделенного блока в координатах MainGrid
			Dim topLeft = parentFirst.TranslatePoint(New Point(0, 0), MainGrid)
			Dim bottomRight = parentLast.TranslatePoint(
				New Point(parentLast.ActualWidth, parentLast.ActualHeight), MainGrid)

			Dim blockRect = New Rect(topLeft, bottomRight)

			' Красим обратно в прозрачный ТОЛЬКО если курсор реально покинул весь блок
			If Not blockRect.Contains(mousePos) Then
				For Each cell In _selectedCells
					cell.Background = DefaultBrush
				Next
				_selectedCells.Clear()
			End If



			'Dim pos = Mouse.GetPosition(MainGrid)
			'' Проверяем, действительно ли курсор покинул границы Grid
			'If pos.X < 0 OrElse pos.Y < 0 OrElse
			' pos.X > MainGrid.ActualWidth OrElse pos.Y > MainGrid.ActualHeight Then
			'	ClearSelection()
			'End If
		End Sub

		''' <summary>
		''' Фиксируем выделение столбца при клике ЛКМ
		''' </summary>
		Private Sub Cell_PreviewMouseDownForSelection(sender As Object, e As MouseButtonEventArgs)
			Dim tb = TryCast(sender, TextBlock)
			If tb Is Nothing Then Return

			Dim parentBorder = TryCast(tb.Parent, Border)
			If parentBorder Is Nothing Then Return

			Dim col = Grid.GetColumn(parentBorder)
			HighlightColumn(col)
		End Sub



























		Sub New()
			' Этот вызов является обязательным для конструктора.
			InitializeComponent()
			' Добавить код инициализации после вызова InitializeComponent().
		End Sub



	End Class
End Namespace


