Imports System.Globalization
Imports System.Reflection
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input

Namespace Kas

	Partial Public Class RoadGoalsEditor

		Private _goal As MonthlyRoadGoals
		Private _allGoals As List(Of MonthlyRoadGoals)

		' все редактируемые числовые поля (Integer/Single), кроме Year/Month
		Private Shared ReadOnly ValueProps As List(Of PropertyInfo) =
			GetType(MonthlyRoadGoals).GetProperties() _
				.Where(Function(p) p.CanWrite AndAlso
								   p.Name <> "Year" AndAlso p.Name <> "Month" AndAlso
								   (p.PropertyType Is GetType(Single) OrElse p.PropertyType Is GetType(Integer))) _
				.ToList()

		' ==================== КОНСТРУКТОР ====================

		Public Sub New()
			InitializeComponent()

			_goal = New MonthlyRoadGoals() With {.Year = Date.Today.Year, .Month = Date.Today.Month}
			DataContext = _goal

			InitializeComboBoxes()

			AddHandler CmbYear.SelectionChanged, AddressOf CmbYear_SelectionChanged
			AddHandler CmbMonth.SelectionChanged, AddressOf CmbMonth_SelectionChanged

			RefreshDataCache()
			RefreshHighlights()
			SyncFormWithSelection()
		End Sub

		' ==================== КОМБОБОКСЫ ====================

		Private Sub InitializeComboBoxes()
			Dim currentYear As Integer = Date.Today.Year
			Dim currentMonth As Integer = Date.Today.Month

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

		' ==================== КЭШ ====================

		Private Sub RefreshDataCache()
			_allGoals = RoadGoalsStore.Load()
			If _allGoals Is Nothing Then _allGoals = New List(Of MonthlyRoadGoals)()
		End Sub

		' ==================== СМЕНА ВЫБОРА ====================

		Private Sub CmbYear_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
			If CmbYear.SelectedItem Is Nothing Then Return
			RefreshMonthHighlights()
			SyncFormWithSelection()
		End Sub

		Private Sub CmbMonth_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
			If CmbMonth.SelectedItem Is Nothing Then Return
			SyncFormWithSelection()
		End Sub

		' ==================== ПОДСВЕТКА ====================

		Private Function HasData(g As MonthlyRoadGoals) As Boolean
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

		Private Sub MarkItemsWithData(cmb As ComboBox, valuesWithData As IEnumerable(Of String))
			Dim present As New HashSet(Of String)(valuesWithData)

			For Each obj As Object In cmb.Items
				Dim item = TryCast(obj, ComboBoxItem)
				If item Is Nothing Then Continue For

				If present.Contains(CStr(item.Content)) Then
					item.Style = CType(FindResource("MyComboBlue"), Style)
				Else
					item.Style = Nothing   ' возвращается к базовому MyCombo из XAML
				End If
			Next
		End Sub

		' ==================== СОХРАНЕНИЕ / ЗАГРУЗКА / ОЧИСТКА ====================

		Private Sub SyncFormWithSelection()
			_goal.Year = GetSelectedYear()
			_goal.Month = GetSelectedMonth()

			Dim saved = _allGoals.FirstOrDefault(Function(g) g.Year = _goal.Year AndAlso
												 g.Month = _goal.Month AndAlso HasData(g))
			If saved IsNot Nothing Then
				For Each p In ValueProps
					p.SetValue(_goal, p.GetValue(saved))
				Next
			Else
				For Each p In ValueProps
					p.SetValue(_goal, Convert.ChangeType(0, p.PropertyType))
				Next
			End If
		End Sub

		Private Sub BtnLoad_Click(sender As Object, e As RoutedEventArgs)
			SyncFormWithSelection()
		End Sub

		Private Sub BtnSave_Click(sender As Object, e As RoutedEventArgs)
			If _goal Is Nothing Then Return

			Dim year As Integer = GetSelectedYear()
			Dim month As Integer = GetSelectedMonth()

			Dim allGoals = RoadGoalsStore.Load()
			Dim existing = allGoals.FirstOrDefault(Function(g) g.Year = year AndAlso g.Month = month)

			Dim allZero = Not ValueProps.Any(Function(p) Convert.ToSingle(p.GetValue(_goal)) <> 0)

			If allZero Then
				If existing Is Nothing Then
					MessageBox.Show($"Данных за {month}.{year} нет — сохранять нечего.", "Информация")
					Return
				End If
				If Not ShowMSG(MW, $"Все значения для {month}.{year} равны 0." & vbCrLf & "Сохранить пустую запись?",
							   "Внимание", MessageBoxButton.OKCancel, MessageBoxImage.Question) Then Return

				allGoals.Remove(existing)
				RoadGoalsStore.Save(allGoals)
				RefreshDataCache()
				RefreshHighlights()
				SyncFormWithSelection()
				ShowMSG(MW, $"Запись за {month}.{year} удалена.", "Информация")
				Return
			End If

			If existing Is Nothing Then
				Dim newItem As New MonthlyRoadGoals() With {.Year = year, .Month = month}
				For Each p In ValueProps
					p.SetValue(newItem, p.GetValue(_goal))
				Next
				allGoals.Add(newItem)
			Else
				For Each p In ValueProps
					p.SetValue(existing, p.GetValue(_goal))
				Next
			End If

			RoadGoalsStore.Save(allGoals)
			RefreshDataCache()
			RefreshHighlights()

			ShowMSG(MW, $"Цели по дорогам за {month}.{year} сохранены!", "Успех")
		End Sub

		Private Sub BtnClearAll_Click(sender As Object, e As RoutedEventArgs)
			For Each p In ValueProps
				p.SetValue(_goal, Convert.ChangeType(0, p.PropertyType))
			Next
		End Sub

		' ==================== ВСТАВКА ИЗ БУФЕРА ====================

		Private Sub BtnPaste_Click(sender As Object, e As RoutedEventArgs)
			Dim text = Clipboard.GetText()
			If String.IsNullOrWhiteSpace(text) Then
				MessageBox.Show("Буфер обмена пуст.", "Информация")
				Return
			End If

			Dim lines = text.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
			If lines.Length < 3 Then
				MessageBox.Show("Недостаточно строк в буфере (нужно 3).", "Информация")
				Return
			End If

			Dim roads = {"Vsjd", "Zabd", "Dvd"}
			For i As Integer = 0 To Math.Min(lines.Length, 3) - 1
				Dim cells = lines(i).Split(ControlChars.Tab)
				If cells.Length < 4 Then Continue For

				GetType(MonthlyRoadGoals).GetProperty(roads(i) & "OtsGoal").SetValue(_goal, ParseInt(cells(0)))
				GetType(MonthlyRoadGoals).GetProperty(roads(i) & "OtsFact").SetValue(_goal, ParseInt(cells(1)))
				GetType(MonthlyRoadGoals).GetProperty(roads(i) & "HoursGoal").SetValue(_goal, ParseSingle(cells(2)))
				GetType(MonthlyRoadGoals).GetProperty(roads(i) & "HoursFact").SetValue(_goal, ParseSingle(cells(3)))
			Next
		End Sub

		Private Function ParseInt(s As String) As Integer
			Dim d As Integer
			If Integer.TryParse(s.Trim().Replace(",", "."), NumberStyles.Any,
							   CultureInfo.InvariantCulture, d) Then Return d
			Return 0
		End Function

		Private Function ParseSingle(s As String) As Single
			Dim d As Double
			If Double.TryParse(s.Trim().Replace(",", "."), NumberStyles.Any,
							  CultureInfo.InvariantCulture, d) Then Return CSng(d)
			Return 0
		End Function

		' ==================== TEXTBOX ХЭНДЛЕРЫ ====================

		Private Sub TextBox_SelectAll_OnPreviewMouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
			Dim tb = TryCast(sender, TextBox)
			If tb IsNot Nothing AndAlso Not tb.IsKeyboardFocused Then
				tb.SelectAll()
				tb.Focus()
				e.Handled = True
			End If
		End Sub

		Private Sub TextBox_GotKeyboardFocus(sender As Object, e As KeyboardFocusChangedEventArgs)
			Dim tb = TryCast(sender, TextBox)
			If tb IsNot Nothing Then tb.SelectAll()
		End Sub
	End Class
End Namespace