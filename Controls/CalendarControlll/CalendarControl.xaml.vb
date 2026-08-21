Imports System.ComponentModel
Imports System.Globalization
Imports System.Windows.Controls.Primitives
Imports OfficeOpenXml.ExcelErrorValue

Namespace Kas

    Partial Public Class CalendarControl
        Implements INotifyPropertyChanged

        ' Регистрация DependencyProperty (нужна для привязки к свойствам через ХАМЛ)
        Public Shared ReadOnly ParPopProperty As DependencyProperty = DependencyProperty.Register("ParPop", GetType(Popup), GetType(CalendarControl), New PropertyMetadata(Nothing))

        ' Обёртка для удобства доступа к свойству
        Public Property ParPop As Popup
            Get
                Return CType(GetValue(ParPopProperty), Popup)
            End Get
            Set(value As Popup)
                SetValue(ParPopProperty, value)
            End Set
        End Property


        Public Shared ReadOnly IsSingleModeProperty As DependencyProperty =
    DependencyProperty.Register("IsSingleMode", GetType(Boolean), GetType(CalendarControl), New PropertyMetadata(True))

        Public Property IsSingleMode As Boolean
            Get
                Return CType(GetValue(IsSingleModeProperty), Boolean)
            End Get
            Set(value As Boolean)
                SetValue(IsSingleModeProperty, value)
            End Set
        End Property


        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
        Protected Sub MyPropertyChanged(ByVal propertyName As String)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
        End Sub


#Region "Обработка выбора даты"

        Private _SelectedDate As Date = Nothing ' Для хранения выбранной даты
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
                    'здесь наружу выдавать не надо - за 1 нажатие 3 раза будет выдавать
                    'OnSelectedDateChanged(Value)
                End If
            End Set
        End Property

        ' Объявление события
        Public Event SelectedDateChanged As EventHandler(Of SelectedDateChangedEventArgs)

        ' Метод для вызова события
        Protected Sub OnSelectedDateChanged(newDate As DateTime)
            RaiseEvent SelectedDateChanged(Me, New SelectedDateChangedEventArgs(newDate))
        End Sub

        ' Класс аргументов события
        Public Class SelectedDateChangedEventArgs
            Inherits EventArgs

            Public Property NewDate As DateTime

            Public Sub New(newDate As DateTime)
                Me.NewDate = newDate
            End Sub
        End Class

#End Region



        Private Dayz As String() = {"пн", "вт", "ср", "чт", "пт", "сб", "вс"}
        Private Monts As IEnumerable(Of String) = (New String() {"Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь"})


        'для исключения конфликтов при изменении индекса элемента в комбобоксах и обновлении календаря
        Private IsInitializing As Boolean = False

        Sub New()
            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()
            ' Добавить код инициализации после вызова InitializeComponent().

        End Sub

        Private Sub UserControl_Loaded(sender As Object, e As RoutedEventArgs)
            Me.DataContext = Me
            SelectedDate = Today
            TodayButton.Text = $"Сегодня: {Today.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("ru-RU"))}"

            ' 1) Устанавливаем флаг инициализации
            IsInitializing = True
            ' Заполняем ComboBox названиями месяцев
            MonthSelector.ItemsSource = Monts
            ' Заполняем ComboBox годами
            YearLabel.ItemsSource = SetYearsList()
            ' Устанавливаем  индекс месяца
            MonthSelector.SelectedIndex = SelectedDate.Month - 1
            ' Устанавливаем  индекс года (обязательно сравнение через ToString)
            YearLabel.SelectedItem = SelectedDate.Year.ToString
            ' 2) Снимаем флаг инициализации
            IsInitializing = False
            If IsSingleMode Then
                SelecteddayButton.Visibility = Visibility.Visible
            Else
                SelecteddayButton.Visibility = Visibility.Hidden
            End If
            UpdateCalendar()
        End Sub


#Region "Выбор даты"

        'нажимаем текстблок СЕГОДНЯ..
        Private Sub TodayButton_PreviewMouseDown(sender As Object, e As MouseButtonEventArgs)
            SetSelectedDate(DateTime.Today)
        End Sub

        'выбираем дату в календаре
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


        Sub SetSelectedDate(curdaT As DateTime)
            'здесь уже можно без флага ибо все данные загружены
            ' Устанавливаем SelectedDate на сегодняшнюю дату
            SelectedDate = curdaT
            ' Обновляем выбранный месяц в ComboBox
            MonthSelector.SelectedIndex = curdaT.Month - 1 'CurrentMonthIndex
            ' Обновляем год
            YearLabel.SelectedItem = curdaT.Year.ToString()
            ' Обновляем календарь - этот надо иначе день не выбирается
            UpdateCalendar()

            'MW.InfoBLOK.AddItem($"нажат {curdaT}")
            'а вот тут надо - будет выдавать 1 событие на 1 нажатие а не 3 события при изменении свойства
            OnSelectedDateChanged(curdaT)

        End Sub

#End Region


#Region "Выбор месяца стрелками"

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
            'If Idx - 1 < 0 Then
            If Idx < 0 Then
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

#End Region






#Region "Основная логика - обновл календаря и ограничения"


#Region "ограничения по обновлению календаря"

        'ограничение на обновление календаря через флаг инициализации
        Private Sub MonthSelector_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            ' Если идёт инициализация, пропускаем выполнение
            If IsInitializing Then Return
            UpdateCalendar()
        End Sub

        'ограничение на обновление календаря через флаг инициализации
        Private Sub YearLabel_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            ' Если идёт инициализация, пропускаем выполнение
            If IsInitializing Then Return
            UpdateCalendar()
        End Sub

#End Region

#Region "Обновление календаря 'ГЛАААВНЫЙ ЛОГИКА"

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

                    Dim Fff As Integer = CDate(.Tag).DayOfWeek
                    If Fff = 0 Or Fff = 6 Then
                        .Style = CType(FindResource("GrayDayWeekEndButtonStyle"), Style)
                    Else
                        .Style = CType(FindResource("GrayDayButtonStyle"), Style)
                    End If


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
                    ' Сохраняем дату в Tag
                    .Tag = New DateTime(YearLabel.SelectedItem, MonthSelector.SelectedIndex + 1, day)

                    Dim Fff As Integer = CDate(.Tag).DayOfWeek

                    If SelectedDate = CType(.Tag, DateTime) Then
                        'выбранный день
                        '.Style = CType(FindResource("SelectedDayButtonStyle"), Style)
                        If Fff = 0 Or Fff = 6 Then
                            'выбранный выходной
                            .Style = CType(FindResource("SelectedDayWeekEndButtonStyle"), Style)
                        Else
                            'выбранный рабочий день
                            .Style = CType(FindResource("SelectedDayButtonStyle"), Style)
                        End If
                    Else
                        If Fff = 0 Or Fff = 6 Then
                            'невыбранный выходной
                            .Style = CType(FindResource("DayWeekEndButtonStyle"), Style)
                        Else
                            'невыбранный рабочий день
                            .Style = CType(FindResource("DayButtonStyle"), Style)
                        End If

                    End If
                    'обработчик нужен (только невыбранным) ВСЕМ дням, иначе текущий день для периода криво устанавливается
                    AddHandler .PreviewMouseDown, AddressOf DayButton_Click
                End With

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

                        Dim Fff As Integer = CDate(.Tag).DayOfWeek
                        If Fff = 0 Or Fff = 6 Then
                            .Style = CType(FindResource("GrayDayWeekEndButtonStyle"), Style)
                        Else
                            .Style = CType(FindResource("GrayDayButtonStyle"), Style)
                        End If

                    End With

                    DaysGrid.Children.Add(Gg)
                Next
            End If
        End Sub


#End Region







#End Region



        'устанавливаем список годов +- 10 от текущего
        Function SetYearsList() As List(Of String)
            Dim Rez As New List(Of String)
            Dim Ye As Integer = Today.Year
            For i = Ye - 10 To Ye + 10
                Rez.Add(i.ToString)
            Next
            Return Rez
        End Function


        Private Sub OKButton_Click(sender As Object, e As RoutedEventArgs)
            'а тут будет передача данных наружу

            ParPop.IsOpen = False
        End Sub
    End Class
End Namespace

