Imports System.ComponentModel
Imports System.Globalization
Namespace Kas
    Partial Public Class PeriodWindow
        Implements INotifyPropertyChanged

        Private IsInitializing As Boolean = False

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
        Protected Sub MyPropertyChanged(ByVal propertyName As String)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
        End Sub

        'надо эту дату наружу выносить и при запуске ее и присваивать в селектеддате
        Public Property SelectedDate As DateTime
            Get
                Return _SelectedDate
            End Get
            Set
                If _SelectedDate <> Value Then
                    _SelectedDate = Value
                    MyPropertyChanged(NameOf(SelectedDate))
                    SelecteddayButton.Text = $"Выбрано: {Value.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("ru-RU"))}"
                End If
            End Set
        End Property

        Private Dayz As String() = {"пн", "вт", "ср", "чт", "пт", "сб", "вс"}
        Private Monts As IEnumerable(Of String) = (New String() {"Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь"})
        Private _SelectedDate As Date = Nothing ' Для хранения выбранной даты


        Sub New()
            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()
            ' Добавить код инициализации после вызова InitializeComponent().

        End Sub


        Private Sub DTPicker_Loaded(sender As Object, e As RoutedEventArgs)
            Me.DataContext = Me
            SelectedDate = Today
            TodayButton.Text = $"Сегодня: {Today.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("ru-RU"))}"

            ' Устанавливаем флаг инициализации
            IsInitializing = True

            ' Заполняем ComboBox названиями месяцев
            MonthSelector.ItemsSource = Monts

            ' Заполняем ComboBox годами
            YearLabel.ItemsSource = SetYearsList()

            ' Устанавливаем  индекс месяца
            MonthSelector.SelectedIndex = SelectedDate.Month - 1

            ' Устанавливаем  индекс года
            YearLabel.SelectedItem = SelectedDate.Year.ToString

            ' Снимаем флаг инициализации
            IsInitializing = False
            UpdateCalendar()
        End Sub

        Function SetYearsList() As List(Of String)
            Dim Rez As New List(Of String)
            Dim Ye As Integer = Today.Year

            For i = Ye - 10 To Ye + 10
                Rez.Add(i.ToString)
            Next

            Return Rez
        End Function



        Private Sub UpdateCalendar()
            ' Очищаем сетку дней
            DaysGrid.Children.Clear()
            ' Добавляем рабочие дни недели
            For i As Integer = 0 To 4
                Dim dayOfWeek As String = Dayz(i)
                Dim Gg As New Label
                With Gg
                    .Content = dayOfWeek
                    .Style = CType(FindResource("DayWeekButtonStyle"), Style)
                End With
                DaysGrid.Children.Add(Gg)
            Next

            ' Добавляем выходные дни недели
            For i As Integer = 5 To 6
                Dim dayOfWeek As String = Dayz(i)
                Dim Gg As New Label
                With Gg
                    .Content = dayOfWeek
                    .Style = CType(FindResource("WeekEndButtonStyle"), Style)
                End With
                DaysGrid.Children.Add(Gg)
            Next
            '======================================================================================================
            ' Получаем первый день месяца
            Dim firstDayOfMonth = New DateTime(YearLabel.SelectedItem, MonthSelector.SelectedIndex + 1, 1)
            '======================================================================================================
            ' Добавляем  ячейки для дней предыдущего месяца
            Dim startOffset = CInt(firstDayOfMonth.DayOfWeek)
            If startOffset = 0 Then startOffset = 7 ' Воскресенье = 0
            Dim Yea As Integer = firstDayOfMonth.AddDays(-1).Year
            For i As Integer = 1 To startOffset - 1
                Dim Gg As New Label
                With Gg
                    If Yea >= CInt(YearLabel.Items(0)) Then
                        .Content = firstDayOfMonth.AddDays(-(startOffset - i)).Day
                        .Tag = New DateTime(firstDayOfMonth.AddDays(-1).Year, firstDayOfMonth.AddMonths(-1).Month, .Content)
                        AddHandler Gg.PreviewMouseDown, AddressOf DayButton_Click
                    End If
                    .Style = CType(FindResource("GrayDayButtonStyle"), Style)
                End With
                DaysGrid.Children.Add(Gg)

            Next
            '======================================================================================================
            ' Добавляем  ячейки для дней текущего месяца
            Dim daysInMonth = DateTime.DaysInMonth(YearLabel.SelectedItem, MonthSelector.SelectedIndex + 1)
            ' Добавляем дни текущего месяца
            For day As Integer = 1 To daysInMonth

                Dim Gg As New Label
                With Gg
                    .Content = day.ToString()
                    .Tag = New DateTime(YearLabel.SelectedItem, MonthSelector.SelectedIndex + 1, day) ' Сохраняем дату в Tag
                End With

                ' Устанавливаем стиль в зависимости от выбранной даты
                Gg.Style = If(SelectedDate = CType(Gg.Tag, DateTime),
                      CType(FindResource("SelectedDayButtonStyle"), Style),
                      CType(FindResource("DayButtonStyle"), Style))
                AddHandler Gg.PreviewMouseDown, AddressOf DayButton_Click
                DaysGrid.Children.Add(Gg)
            Next


            '======================================================================================================
            ' Добавляем  ячейки для дней следующего месяца
            startOffset = New DateTime(YearLabel.SelectedItem, MonthSelector.SelectedIndex + 1, daysInMonth).DayOfWeek
            Yea = firstDayOfMonth.AddMonths(1).Year
            ' Воскресенье = 0/ Если не воскресенье - крайний день в пустых ячейках календаря после последнего числа месяца
            If startOffset > 0 Then
                Dim C = 1
                For i As Integer = startOffset + 1 To 7
                    Dim Gg As New Label
                    With Gg
                        If Yea <= CInt(YearLabel.Items(YearLabel.Items.Count - 1)) Then
                            .Content = C
                            C += 1
                            .Tag = New DateTime(firstDayOfMonth.AddMonths(1).Year, firstDayOfMonth.AddMonths(1).Month, .Content)
                            AddHandler Gg.PreviewMouseDown, AddressOf DayButton_Click
                        End If
                        .Style = CType(FindResource("GrayDayButtonStyle"), Style)
                    End With

                    DaysGrid.Children.Add(Gg)
                Next
            End If
        End Sub

        Private Sub TodayButton_PreviewMouseDown(sender As Object, e As MouseButtonEventArgs)
            SetSelectedDate(DateTime.Today)
        End Sub

        Sub SetSelectedDate(curdaT As DateTime)
            ' Устанавливаем SelectedDate на сегодняшнюю дату
            SelectedDate = curdaT
            ' Обновляем выбранный месяц в ComboBox
            MonthSelector.SelectedIndex = curdaT.Month - 1 'CurrentMonthIndex
            ' Обновляем год
            YearLabel.SelectedItem = curdaT.Year.ToString()
            ' Обновляем календарь
            UpdateCalendar()
        End Sub



        Private Sub DayButton_Click(sender As Object, e As MouseButtonEventArgs)
            ' Обработка клика по дню
            Dim TBlok As Label = CType(sender, Label)
            Dim selectedDay As DateTime = CType(TBlok.Tag, DateTime)
            If selectedDay.Year < CInt(YearLabel.Items(0)) Then
                Exit Sub
            End If
            If selectedDay.Year > CInt(YearLabel.Items(YearLabel.Items.Count - 1)) Then
                Exit Sub
            End If
            SetSelectedDate(selectedDay)
        End Sub

        Private Sub MonthSelector_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            ' Если идёт инициализация, пропускаем выполнение
            If IsInitializing Then Return
            UpdateCalendar()
        End Sub

        Private Sub YearLabel_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            ' Если идёт инициализация, пропускаем выполнение
            If IsInitializing Then Return
            UpdateCalendar()
        End Sub

        Private Sub NextMNTArrow_MouseDown(sender As Object, e As MouseButtonEventArgs)
            ' Переход к следующему месяцу
            Dim Idx As Integer = MonthSelector.SelectedIndex
            Idx += 1
            If Idx > 11 Then
                Idx = 0
                If YearLabel.SelectedIndex + 1 < YearLabel.Items.Count - 1 Then

                    YearLabel.SelectedIndex = YearLabel.SelectedIndex + 1
                Else
                    Exit Sub
                End If
            End If
            MonthSelector.SelectedIndex = Idx
            UpdateCalendar()
        End Sub

        Private Sub PrevMNTArrow_MouseDown(sender As Object, e As MouseButtonEventArgs)
            ' Переход к предыдущему месяцу
            Dim Idx As Integer = MonthSelector.SelectedIndex
            Idx -= 1
            If Idx - 1 < 0 Then
                Idx = 11
                If YearLabel.SelectedIndex - 1 > 0 Then
                    YearLabel.SelectedIndex = YearLabel.SelectedIndex - 1
                Else
                    Exit Sub
                End If

            End If
            MonthSelector.SelectedIndex = Idx
            UpdateCalendar()
        End Sub
    End Class
End Namespace

