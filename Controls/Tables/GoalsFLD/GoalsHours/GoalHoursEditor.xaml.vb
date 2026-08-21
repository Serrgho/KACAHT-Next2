Imports System.Globalization
Imports System.Reflection
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input

Namespace Kas

	Partial Public Class GoalHoursEditor

		Private _goal As MonthlyGoalHours
		Private _allGoals As List(Of MonthlyGoalHours)   ' кэш данных из JSON

		' все редактируемые числовые поля (Single), кроме Year/Month
		Private Shared ReadOnly ValueProps As List(Of PropertyInfo) =
			GetType(MonthlyGoalHours).GetProperties() _
				.Where(Function(p) p.CanWrite AndAlso
								   p.Name <> "Year" AndAlso p.Name <> "Month" AndAlso
								   p.PropertyType Is GetType(Single)) _
				.ToList()

		' ==================== КОНСТРУКТОР ====================

		Public Sub New()
			InitializeComponent()

			' сразу создаём пустую цель для текущих года/месяца
			_goal = New MonthlyGoalHours()
			_goal.Year = DateTime.Now.Year
			_goal.Month = DateTime.Now.Month

			' DataContext сразу — биндинги заработают
			DataContext = _goal

			InitializeComboBoxes()

			AddHandler CmbYear.SelectionChanged, AddressOf CmbYear_SelectionChanged
			AddHandler CmbMonth.SelectionChanged, AddressOf CmbMonth_SelectionChanged

			RefreshDataCache()
			RefreshHighlights()
			SyncFormWithSelection()
			UpdateLoadButtonVisibility()
		End Sub

		' ==================== КОМБОБОКСЫ ====================

		Private Sub InitializeComboBoxes()
			Dim currentYear As Integer = DateTime.Now.Year
			Dim currentMonth As Integer = DateTime.Now.Month

			For year As Integer = currentYear - 5 To currentYear + 1
				CmbYear.Items.Add(New ComboBoxItem With {.Content = year})
			Next
			CmbYear.SelectedItem = CmbYear.Items.OfType(Of ComboBoxItem)() _
				.First(Function(i) CInt(i.Content) = currentYear)

			For month As Integer = 1 To 12
				CmbMonth.Items.Add(New ComboBoxItem With {.Content = month})
			Next
			CmbMonth.SelectedItem = CmbMonth.Items.OfType(Of ComboBoxItem)() _
				.First(Function(i) CInt(i.Content) = currentMonth)
		End Sub

		Private Function GetSelectedYear() As Integer
			Return CInt(CType(CmbYear.SelectedItem, ComboBoxItem).Content)
		End Function

		Private Function GetSelectedMonth() As Integer
			Return CInt(CType(CmbMonth.SelectedItem, ComboBoxItem).Content)
		End Function

		' ==================== КЭШ И ВИДИМОСТЬ КНОПКИ ====================

		Private Sub RefreshDataCache()
			_allGoals = GoalHoursStore.Load()
			If _allGoals Is Nothing Then _allGoals = New List(Of MonthlyGoalHours)()
		End Sub

		''' <summary>Кнопка "Перезагрузить" видна, только если для выбранных года+месяца есть данные.</summary>
		Private Sub UpdateLoadButtonVisibility()
			If CmbYear.SelectedItem Is Nothing OrElse CmbMonth.SelectedItem Is Nothing Then Return

			Dim year As Integer = GetSelectedYear()
			Dim month As Integer = GetSelectedMonth()

			Dim exists As Boolean = _allGoals.Any(Function(g) g.Year = year AndAlso
												  g.Month = month AndAlso HasData(g))
			BtnLoad.Visibility = If(exists, Visibility.Visible, Visibility.Collapsed)
		End Sub

		' ==================== СМЕНА ВЫБОРА ====================

		Private Sub CmbYear_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
			If CmbYear.SelectedItem Is Nothing Then Return
			RefreshMonthHighlights()
			SyncFormWithSelection()
			UpdateLoadButtonVisibility()
		End Sub

		Private Sub CmbMonth_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
			If CmbMonth.SelectedItem Is Nothing Then Return
			SyncFormWithSelection()
			UpdateLoadButtonVisibility()
		End Sub

		' ==================== ЗАГРУЗКА И СОХРАНЕНИЕ ====================

		Private Sub BtnLoad_Click(sender As Object, e As RoutedEventArgs)
			Dim year As Integer = GetSelectedYear()
			Dim month As Integer = GetSelectedMonth()

			_goal.Year = year
			_goal.Month = month

			Dim saved = _allGoals.FirstOrDefault(Function(g) g.Year = year AndAlso
												 g.Month = month AndAlso HasData(g))
			If saved IsNot Nothing Then
				ApplyGoalValues(saved)
			Else
				ResetGoalFields()
				MessageBox.Show($"Цель для {month}.{year} не найдена. Введите новые данные.", "Информация")
			End If
		End Sub

		Private Sub BtnSave_Click(sender As Object, e As RoutedEventArgs)
			If _goal Is Nothing Then Return

			Dim year As Integer = GetSelectedYear()
			Dim month As Integer = GetSelectedMonth()

			Dim allGoals = GoalHoursStore.Load()
			Dim existing = allGoals.FirstOrDefault(Function(g) g.Year = year AndAlso g.Month = month)

			' ---------- защита от случайного сохранения пустой формы ----------
			If GetTotalValue() = 0 Then
				If existing Is Nothing Then
					MessageBox.Show($"Данных за {month}.{year} нет — сохранять нечего.", "Информация")
					Return
				End If

				If Not ShowMSG(MW, $"Все значения для {month}.{year} равны 0." & vbCrLf & "Сохранить пустую цель?",
							   "Внимание", MessageBoxButton.OKCancel, MessageBoxImage.Question) Then Return

				allGoals.Remove(existing)
				GoalHoursStore.Save(allGoals)

				RefreshDataCache()
				RefreshHighlights()
				UpdateLoadButtonVisibility()

				ShowMSG(MW, $"Запись за {month}.{year} удалена.", "Информация")
				Return
			End If
			' ------------------------------------------------------------------

			' ---------- ненулевая форма: обновляем или добавляем ----------
			If existing Is Nothing Then
				allGoals.Add(_goal)
			Else
				CopyValuesTo(existing)
			End If

			GoalHoursStore.Save(allGoals)

			RefreshDataCache()
			RefreshHighlights()
			UpdateLoadButtonVisibility()

			ShowMSG(MW, $"Цель для {month}.{year} сохранена!", "Успех")
		End Sub

		Private Sub BtnClearAll_Click(sender As Object, e As RoutedEventArgs)
			ResetGoalFields()
		End Sub

		' ==================== ОБРАБОТЧИКИ TEXTBOX ====================

		Private Sub TextBox_SelectAll_OnPreviewMouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
			Dim tb = TryCast(sender, Controls.TextBox)
			If tb IsNot Nothing AndAlso Not tb.IsKeyboardFocused Then
				tb.SelectAll()
				tb.Focus()
				e.Handled = True
			End If
		End Sub

		Private Sub TextBox_GotKeyboardFocus(sender As Object, e As KeyboardFocusChangedEventArgs)
			Dim tb = TryCast(sender, Controls.TextBox)
			If tb IsNot Nothing Then
				tb.SelectAll()
			End If
		End Sub

		' ==================== ПОДСВЕТКА И СИНХРОНИЗАЦИЯ ====================

		''' <summary>True, если в записи есть хотя бы одно ненулевое значение.</summary>
		Private Function HasData(g As MonthlyGoalHours) As Boolean
			Return ValueProps.Any(Function(p) Convert.ToSingle(p.GetValue(g)) <> 0)
		End Function

		Private Sub RefreshHighlights()
			Dim yearsWithData = _allGoals.Where(Function(g) HasData(g)) _
				.Select(Function(g) g.Year.ToString()).Distinct().ToList()
			MarkItemsWithData(CmbYear, yearsWithData)

			RefreshMonthHighlights()
		End Sub

		Private Sub RefreshMonthHighlights()
			If CmbYear.SelectedItem Is Nothing Then Return

			Dim selYear As Integer = GetSelectedYear()
			Dim monthsWithData = _allGoals _
				.Where(Function(g) g.Year = selYear AndAlso HasData(g)) _
				.Select(Function(g) g.Month.ToString()).Distinct().ToList()
			MarkItemsWithData(CmbMonth, monthsWithData)
		End Sub

		''' <summary>Подсвечивает пункты, чей Content входит в valuesWithData.</summary>
		Private Sub MarkItemsWithData(cmb As ComboBox, valuesWithData As IEnumerable(Of String))
			Dim present As New HashSet(Of String)(valuesWithData)

			For Each obj As Object In cmb.Items
				Dim item = TryCast(obj, ComboBoxItem)
				If item Is Nothing Then Continue For

				If present.Contains(CStr(item.Content)) Then
					item.Style = CType(FindResource("MyComboBlue"), Style)
				Else
					item.Style = Nothing
				End If
			Next
		End Sub

		''' <summary>Обнуляет все поля цели.</summary>
		Private Sub ResetGoalFields()
			For Each p In ValueProps
				p.SetValue(_goal, Convert.ChangeType(0.0F, p.PropertyType))
			Next
		End Sub

		''' <summary>Копирует значения из сохранённой записи в текущий _goal.</summary>
		Private Sub ApplyGoalValues(source As MonthlyGoalHours)
			For Each p In ValueProps
				p.SetValue(_goal, p.GetValue(source))
			Next
		End Sub

		''' <summary>Копирует ВСЕ числовые поля из _goal в приёмник.</summary>
		Private Sub CopyValuesTo(dst As MonthlyGoalHours)
			For Each p In ValueProps
				p.SetValue(dst, p.GetValue(_goal))
			Next
		End Sub

		Private Function GetTotalValue() As Single
			Dim total As Single = 0
			For Each p In ValueProps
				total += Convert.ToSingle(p.GetValue(_goal))
			Next
			Return total
		End Function

		''' <summary>Приводит форму в соответствие с выбором: есть данные — загружаем, нет — обнуляем.</summary>
		Private Sub SyncFormWithSelection()
			_goal.Year = GetSelectedYear()
			_goal.Month = GetSelectedMonth()

			Dim saved = _allGoals.FirstOrDefault(Function(g) g.Year = _goal.Year AndAlso
												 g.Month = _goal.Month AndAlso HasData(g))
			If saved IsNot Nothing Then
				ApplyGoalValues(saved)
			Else
				ResetGoalFields()
			End If
		End Sub

		' порядок строк и суффиксы колонок — как в сетке формы
		Private Shared ReadOnly DepotKeys As Integer() = {1, 2, 3, 5, 7}   ' Боготол, Красноярск, Иланская, Ачинск, Абакан

		Private Shared Function GroupSuffix(c As Integer) As String
			Select Case c
				Case 0 : Return "Tche"
				Case 1 : Return "Sld"
				Case 2 : Return "Zav"
				Case 3 : Return "Tr"
			End Select
			Return ""
		End Function

		Private Shared Function Norm(s As String) As String
			Return If(s, "").Trim().Replace(",", ".")
		End Function

		Private Shared Function ParseSingle(s As String) As Single
			Dim d As Double
			If Double.TryParse(Norm(s), System.Globalization.NumberStyles.Any,
					   System.Globalization.CultureInfo.InvariantCulture, d) Then
				Return CSng(d)
			End If
			Return 0
		End Function

		Private Sub BtnPaste_Click(sender As Object, e As RoutedEventArgs)
			Dim text As String = Clipboard.GetText()
			If String.IsNullOrWhiteSpace(text) Then
				MessageBox.Show("Буфер обмена пуст.", "Информация")
				Return
			End If

			Dim lines = text.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)

			' 5 строк депо; колонок в буфере может быть больше — лишние справа (Всего) отбросим
			For r As Integer = 0 To Math.Min(lines.Length, 5) - 1
				Dim cells = lines(r).Split(ControlChars.Tab).ToList()

				' если колонок больше 4 и первая — не число (имя депо), убираем её
				If cells.Count > 4 Then
					Dim dummy As Double
					If Not Double.TryParse(Norm(cells(0)), System.Globalization.NumberStyles.Any,
								   System.Globalization.CultureInfo.InvariantCulture, dummy) Then
						cells.RemoveAt(0)
					End If
				End If

				' первые 4 ячейки → ТЧЭ, СЛД, Заводы, ТР; пустые → 0
				For c As Integer = 0 To 3
					Dim s As String = If(cells.Count > c, cells(c), "")
					Dim p = GetType(MonthlyGoalHours).GetProperty($"D{DepotKeys(r)}{GroupSuffix(c)}")
					p.SetValue(_goal, ParseSingle(s))
				Next
			Next
		End Sub
	End Class
	Public Class ZeroToEmptyConverterSng
		Implements IValueConverter

		Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
			If value Is Nothing Then Return ""

			' дробные (часы): ноль → пусто, иначе два знака с запятой
			If TypeOf value Is Single OrElse TypeOf value Is Double OrElse TypeOf value Is Decimal Then
				Dim d = CDbl(value)
				If d = 0 Then Return ""
				Return d.ToString("F2", New CultureInfo("ru-RU"))
			End If

			' целые (количества) — как раньше
			Dim i As Integer
			If Integer.TryParse(value.ToString(), i) AndAlso i = 0 Then Return ""
			Return value.ToString()
		End Function

		Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
			Dim s = If(value?.ToString(), "").Trim()
			If String.IsNullOrWhiteSpace(s) Then
				Return If(targetType Is GetType(Single), CSng(0), CInt(0))
			End If

			' запятая и точка — равноправные разделители
			s = s.Replace(",", ".")

			Dim d As Double
			If Double.TryParse(s, System.Globalization.NumberStyles.Any,
							   System.Globalization.CultureInfo.InvariantCulture, d) Then
				If targetType Is GetType(Single) Then Return CSng(d)
				If targetType Is GetType(Double) Then Return d
				Return CInt(Math.Round(d))
			End If

			Return If(targetType Is GetType(Single), CSng(0), CInt(0))
		End Function
	End Class
End Namespace
