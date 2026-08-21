
Imports System.Reflection
Imports System.Windows
Imports System.Windows.Controls
Namespace Kas


    Partial Public Class GoalsEditor
            Private _goal As MonthlyGoal
            Private _allGoals As List(Of MonthlyGoal)   ' кэш данных из JSON
        Private Shared ReadOnly ValueProps As List(Of PropertyInfo) =
    GetType(MonthlyGoal).GetProperties() _
        .Where(Function(p) p.CanWrite AndAlso p.Name <> "Year" AndAlso p.Name <> "Month" AndAlso
                           (p.PropertyType Is GetType(Integer) OrElse
                            p.PropertyType Is GetType(Double) OrElse
                            p.PropertyType Is GetType(Decimal))) _
        .ToList()


        Public Sub New()

            InitializeComponent()

            ' Сразу создаём пустую цель для текущего года/месяца
            Dim currentYear As Integer = DateTime.Now.Year

            Dim currentMonth As Integer = DateTime.Now.Month


            _goal = New MonthlyGoal()

            _goal.Year = currentYear

            _goal.Month = currentMonth


            ' Устанавливаем DataContext сразу - биндинги заработают!
            DataContext = _goal


            InitializeComboBoxes()


            AddHandler CmbYear.SelectionChanged, AddressOf CmbYear_SelectionChanged
            AddHandler CmbMonth.SelectionChanged, AddressOf CmbMonth_SelectionChanged


            RefreshDataCache()
            RefreshHighlights()
            SyncFormWithSelection()
            UpdateLoadButtonVisibility()

        End Sub

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

            ' ---------- Хелперы получения выбранных значений ----------
            Private Function GetSelectedYear() As Integer
                Return CInt(CType(CmbYear.SelectedItem, ComboBoxItem).Content)
            End Function

            Private Function GetSelectedMonth() As Integer
                Return CInt(CType(CmbMonth.SelectedItem, ComboBoxItem).Content)
            End Function

            ' ---------- Кэш данных и видимость кнопки "Загрузить" ----------

            Private Sub RefreshDataCache()
                _allGoals = GoalsStore.Load()
                If _allGoals Is Nothing Then _allGoals = New List(Of MonthlyGoal)()
            End Sub

            ''' <summary>Кнопка "Загрузить" видна только если для выбранных года+месяца есть данные.</summary>
            Private Sub UpdateLoadButtonVisibility()
            If CmbYear.SelectedItem Is Nothing OrElse CmbMonth.SelectedItem Is Nothing Then Return

            Dim year As Integer = GetSelectedYear()
            Dim month As Integer = GetSelectedMonth()
            Dim exists As Boolean = _allGoals.Any(Function(g) g.Year = year AndAlso
                                                   g.Month = month AndAlso HasData(g))

            BtnLoad.Visibility = If(exists, Visibility.Visible, Visibility.Collapsed)
        End Sub

            ' ---------- Смена выбора ----------

            Private Sub CmbYear_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            If CmbYear.SelectedItem Is Nothing Then Return
            RefreshMonthHighlights()
            SyncFormWithSelection()      ' автозагрузка или обнуление
            UpdateLoadButtonVisibility()
        End Sub

            Private Sub CmbMonth_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            If CmbMonth.SelectedItem Is Nothing Then Return
            SyncFormWithSelection()      ' автозагрузка или обнуление
            UpdateLoadButtonVisibility()
        End Sub

        ' ---------- Загрузка и сохранение ----------

        Private Sub BtnLoad_Click(sender As Object, e As RoutedEventArgs)

            ' Сбрасываем мои правки и возвращаем сохранённые значения
            Dim year As Integer = GetSelectedYear()
            Dim month As Integer = GetSelectedMonth()

            _goal.Year = year
            _goal.Month = month

            Dim saved = _allGoals.FirstOrDefault(Function(g) g.Year = year AndAlso
                                                           g.Month = month AndAlso HasData(g))
            If saved IsNot Nothing Then
                ApplyGoalValues(saved)
            Else
                ' Страховка: кнопка была видна, но данные исчезли
                ResetGoalFields()
                MessageBox.Show($"Цель для {month}.{year} не найдена. Введите новые данные.", "Информация")
            End If


            '    Dim year As Integer = GetSelectedYear()
            '    Dim month As Integer = GetSelectedMonth()

            '    ' Обновляем год и месяц в текущем объекте
            '    _goal.Year = year
            '    _goal.Month = month

            '    Dim allGoals = GoalsStore.Load()
            '    Dim loadedGoal = allGoals.FirstOrDefault(Function(g) g.Year = year AndAlso g.Month = month)

            'If loadedGoal IsNot Nothing Then
            '    _goal.T12 = loadedGoal.T12
            '    _goal.T3 = loadedGoal.T3
            '    _goal.TR12 = loadedGoal.TR12
            '    _goal.TR3 = loadedGoal.TR3
            '    _goal.SLD12 = loadedGoal.SLD12
            '    _goal.SLD3 = loadedGoal.SLD3
            '    _goal.Factory12 = loadedGoal.Factory12
            '    _goal.Factory3 = loadedGoal.Factory3
            'Else
            '    ResetGoalFields()   ' крайний случай: данных не оказалось
            '    MessageBox.Show($"Цель для {month}.{year} не найдена. Введите новые данные.", "Информация")
            'End If

        End Sub

        Private Sub BtnSave_Click(sender As Object, e As RoutedEventArgs)
                If _goal Is Nothing Then Return

                Dim year As Integer = GetSelectedYear()
                Dim month As Integer = GetSelectedMonth()

            Dim allGoals = GoalsStore.Load()
            Dim existing = allGoals.FirstOrDefault(Function(g) g.Year = year AndAlso g.Month = month)



            ' ---------- Защита от случайного сохранения пустой формы ----------

            If GetTotalValue() = 0 Then
                If existing Is Nothing Then
                    MessageBox.Show($"Данных за {month}.{year} нет — сохранять нечего.", "Информация")
                    Return
                End If

                If Not ShowMSG(MW, $"Все значения для {month}.{year} равны 0." & vbCrLf & "Сохранить пустую цель?",
            "Внимание", MessageBoxButton.OKCancel, MessageBoxImage.Question) Then Return

                allGoals.Remove(existing)
                GoalsStore.Save(allGoals)

                RefreshDataCache()
                RefreshHighlights()
                UpdateLoadButtonVisibility()

                ShowMSG(MW, $"Запись за {month}.{year} удалена.", "Информация")
                Return
            End If
            ' ------------------------------------------------------------------



            ' ---------- Ненулевая форма: обновляем или добавляем ----------
            If existing Is Nothing Then
                allGoals.Add(_goal)
            Else
                CopyValuesTo(existing)
            End If

            GoalsStore.Save(allGoals)

            RefreshDataCache()
            RefreshHighlights()
            UpdateLoadButtonVisibility()

            ShowMSG(MW, $"Цель для {month}.{year} сохранена!", "Успех")
        End Sub

            ' ---------- Обработчики TextBox (без изменений) ----------

            Private Sub TextBox_SelectAll_OnPreviewMouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
                Dim tb As Controls.TextBox = TryCast(sender, Controls.TextBox)

                If tb IsNot Nothing AndAlso Not tb.IsKeyboardFocused Then
                    tb.SelectAll()
                    tb.Focus()
                    e.Handled = True
                End If
            End Sub

            Private Sub TextBox_GotKeyboardFocus(sender As Object, e As KeyboardFocusChangedEventArgs)
                Dim tb As Controls.TextBox = TryCast(sender, Controls.TextBox)

                If tb IsNot Nothing Then
                    tb.SelectAll()
                End If
            End Sub

        ' ---------- Подсветка лет/месяцев с данными ----------
        ''' <summary>True, если в записи есть хотя бы одно ненулевое значение.</summary>
        Private Function HasData(g As MonthlyGoal) As Boolean
            Return ValueProps.Any(Function(p) Convert.ToDouble(p.GetValue(g)) <> 0)
        End Function

        Private Sub RefreshHighlights()
            Dim yearsWithData As List(Of String) = _allGoals.Where(Function(g) HasData(g)).Select(Function(g) g.Year.ToString()).Distinct().ToList()
            MarkItemsWithData(CmbYear, yearsWithData)

            RefreshMonthHighlights()
        End Sub

            Private Sub RefreshMonthHighlights()
            If CmbYear.SelectedItem Is Nothing Then Return

            Dim selYear As Integer = GetSelectedYear()
            Dim monthsWithData As List(Of String) = _allGoals _
        .Where(Function(g) g.Year = selYear AndAlso HasData(g)).Select(Function(g) g.Month.ToString()).Distinct().ToList()
            MarkItemsWithData(CmbMonth, monthsWithData)
        End Sub

            ''' <summary>Подсвечивает MyComboBlue пункты, чей Content входит в valuesWithData.</summary>
            Private Sub MarkItemsWithData(cmb As ComboBox, valuesWithData As IEnumerable(Of String))
                Dim present As New HashSet(Of String)(valuesWithData)

                For Each obj As Object In cmb.Items
                    Dim item As ComboBoxItem = TryCast(obj, ComboBoxItem)
                    If item Is Nothing Then Continue For

                    If present.Contains(CStr(item.Content)) Then
                        item.Style = CType(FindResource("MyComboBlue"), Style)
                    Else
                        item.Style = Nothing   ' возвращаем обычный неявный стиль
                    End If
                Next
            End Sub

        ''' <summary>Обнуляет поля цели (стирает значения предыдущего периода).</summary>
        Private Sub ResetGoalFields()
            For Each p In ValueProps
                p.SetValue(_goal, Convert.ChangeType(0, p.PropertyType))
            Next
        End Sub

        ''' <summary>Синхронизирует _goal с новым выбором и стирает старые значения.</summary>
        Private Sub SyncGoalWithSelection()
            _goal.Year = GetSelectedYear()
            _goal.Month = GetSelectedMonth()
            ResetGoalFields()
        End Sub

        ''' <summary>Копирует значения из сохранённой записи в текущий _goal.</summary>
        Private Sub ApplyGoalValues(source As MonthlyGoal)
            For Each p In ValueProps
                p.SetValue(_goal, p.GetValue(source))
            Next
        End Sub


        ''' <summary>Копирует ВСЕ числовые поля из _goal в приёмник.</summary>
        Private Sub CopyValuesTo(dst As MonthlyGoal)
            For Each p In ValueProps
                p.SetValue(dst, p.GetValue(_goal))
            Next
        End Sub

        Private Function GetTotalValue() As Double
            Dim total As Double = 0
            For Each p In ValueProps
                total += Convert.ToDouble(p.GetValue(_goal))
            Next
            Return total
        End Function



        ''' <summary>Приводит форму в соответствие с выбором:
        ''' есть данные по паре год+месяц — загружаем, нет — обнуляем.</summary>
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

        Private Sub BtnClearAll_Click(sender As Object, e As RoutedEventArgs)
            ResetGoalFields()
        End Sub
    End Class
End Namespace

