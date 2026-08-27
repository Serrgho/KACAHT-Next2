Imports KACAHT_Next2.Kas.CalendarControl

Namespace Kas


    Partial Public Class DoubleCalendarControl
        Private _IsOpen As Boolean = True
        Private _PlaceTarget As UIElement
        Private PeriodOKPressed As Boolean = False

        Public Property PlaceTarget As UIElement
            Get
                Return _PlaceTarget
            End Get
            Set
                _PlaceTarget = Value
                CalendarPopup.PlacementTarget = PlaceTarget
            End Set
        End Property

        Public Property IsOpen As Boolean
            Get
                Return _IsOpen
            End Get
            Set
                _IsOpen = Value
                CalendarPopup.IsOpen = Value
            End Set
        End Property

        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()


        End Sub

        Private Sub UserControl_Loaded(sender As Object, e As RoutedEventArgs)
            ' Добавить код инициализации после вызова InitializeComponent().
            AddHandler MyCalendar1.SelectedDateChanged, AddressOf OnSelectedNachDateChanged
            AddHandler MyCalendar2.SelectedDateChanged, AddressOf OnSelectedKonDateChanged

            ' Подписка на события смены месяца (стрелки/комбобоксы)
            AddHandler MyCalendar1.MonthChanged, AddressOf OnCalendar1MonthChanged
            AddHandler MyCalendar2.MonthChanged, AddressOf OnCalendar2MonthChanged

            PeriodOKPressed = False
        End Sub


        ''' <summary>
        ''' Обработчик смены месяца в ЛЕВОМ календаре (Начало периода)
        ''' Устанавливает дату на 1-е число выбранного месяца
        ''' </summary>
        Private Sub OnCalendar1MonthChanged(sender As Object, e As CalendarControl.MonthChangedEventArgs)
            Dim newDate As New DateTime(e.Year, e.MonthIndex + 1, 1)

            ' Используем штатный метод установки даты
            MyCalendar1.SetSelectedDate(newDate)
        End Sub

        ''' <summary>
        ''' Обработчик смены месяца в ПРАВОМ календаре (Конец периода)
        ''' Устанавливает дату на последнее число выбранного месяца
        ''' </summary>
        Private Sub OnCalendar2MonthChanged(sender As Object, e As CalendarControl.MonthChangedEventArgs)

            Dim newDate As Date

            ' Если выбранный месяц НЕ равен текущему системному, ставим последнее число
            If e.Year <> Now.Year OrElse (e.MonthIndex + 1) <> Now.Month Then
                Dim daysInMonth As Integer = DateTime.DaysInMonth(e.Year, e.MonthIndex + 1)
                newDate = New DateTime(e.Year, e.MonthIndex + 1, daysInMonth)
            Else
                ' Если месяц ТЕКУЩИЙ, ставим СЕГОДНЯШНЮЮ дату
                newDate = Today
            End If

            MyCalendar2.SetSelectedDate(newDate)



        End Sub

        Private Sub CalendarPopup_Opened(sender As Object, e As EventArgs)
            Dim Parr As Button = DirectCast(PlaceTarget, Button)
            If Parr Is MW.expPoyasnilka.SelectPeriod OrElse Parr Is MW.expPoyasnilka.SelectOldPeriod Then
                PeriodBC.Visibility = Visibility.Visible
                With My.Settings
                    ' 🔧 ТЕПЕРЬ ЗАДАЁМ РЕЖИМ ЗДЕСЬ (вместо UserControl_Loaded)
                    If Not String.IsNullOrEmpty(.TimeMode) Then
                        PeriodBC.CurrentTimeMode = [Enum].Parse(Of PeriodBlockControl.TimeMode)(.TimeMode)
                    Else
                        PeriodBC.CurrentTimeMode = PeriodBlockControl.TimeMode.From00To2359
                    End If

                    If .NachPeriod > Date.MinValue Then
                        MyCalendar1.SelectedDate = Nothing ' Сброс
                        MyCalendar1.SetSelectedDate(.NachPeriod.Date) ' Устанавливаем дату без времени
                        MyCalendar1.InvalidateVisual()
                    End If
                    If .KonPeriod > Date.MinValue Then
                        MyCalendar2.SelectedDate = Nothing ' Сброс
                        MyCalendar2.SetSelectedDate(.KonPeriod.Date) ' Устанавливаем дату без времени
                        MyCalendar2.InvalidateVisual()
                    End If


                    If PlaceTarget Is MW.expPoyasnilka.SelectOldPeriod Then
                        PeriodBC.Visibility = Visibility.Visible
                        ' Для прошлого периода режим обычно такой же, как текущий
                        If Not String.IsNullOrEmpty(.TimeMode) Then
                            PeriodBC.CurrentTimeMode = [Enum].Parse(Of PeriodBlockControl.TimeMode)(.TimeMode)
                        Else
                            PeriodBC.CurrentTimeMode = PeriodBlockControl.TimeMode.From00To2359
                        End If

                        If .nachOLDPeriod > Date.MinValue Then
                            MyCalendar1.SelectedDate = Nothing ' Сброс
                            MyCalendar1.SetSelectedDate(.nachOLDPeriod.Date) ' Устанавливаем дату без времени
                            MyCalendar1.InvalidateVisual()
                        End If
                        If .konOLDPeriod > Date.MinValue Then
                            MyCalendar2.SelectedDate = Nothing ' Сброс
                            MyCalendar2.SetSelectedDate(.konOLDPeriod.Date) ' Устанавливаем дату без времени
                            MyCalendar2.InvalidateVisual()
                        End If
                    End If
                End With
                ' =========================================================================
                ' 2. НОВАЯ ЛОГИКА для кнопки журнала КАСАНТ
                ' =========================================================================
            ElseIf Parr Is MW.JourParam.ParamPeriodCTL.btnSetPeriodJournal Then
                PeriodBC.Visibility = Visibility.Collapsed
                ' Устанавливаем даты из Fetcher в календарь (если они не MinValue)
                If Fetcher.NachDat > Date.MinValue Then
                    MyCalendar1.SelectedDate = Nothing
                    MyCalendar1.SetSelectedDate(Fetcher.NachDat.Date)
                    MyCalendar1.InvalidateVisual()
                End If

                If Fetcher.KonDat > Date.MinValue Then
                    MyCalendar2.SelectedDate = Nothing
                    MyCalendar2.SetSelectedDate(Fetcher.KonDat.Date)
                    MyCalendar2.InvalidateVisual()
                End If
            End If




        End Sub

        ' Обработчик события SelectedDateChanged
        Private Sub OnSelectedNachDateChanged(sender As Object, e As SelectedDateChangedEventArgs)
            PeriodBC.Nach = e.NewDate
        End Sub

        Private Sub OnSelectedKonDateChanged(sender As Object, e As SelectedDateChangedEventArgs)
            PeriodBC.Kon = e.NewDate
        End Sub



        Private Sub OKBU_Click(sender As Object, e As RoutedEventArgs)
            Dim Parr As Button = DirectCast(PlaceTarget, Button)
            Dim Tx As String = ""
            If PeriodBC.Nach Is Nothing Or PeriodBC.Kon Is Nothing Then
                Tx = ($"Период не установлен")
                MW.InfoBLOK.AddItem(Tx)
                MW.InfoBLOK.ScrollToEnd()
                MyCalendar1.ParPop.IsOpen = False
                Exit Sub
            End If

            Dim adjustedDates = ApplyTimeMode(PeriodBC.Nach, PeriodBC.Kon)
            If PeriodBC?.HasKoyak Then
                Tx = ($"Период не установлен: {adjustedDates.Item1:dd.MM.yyyy HH:mm} > чем {adjustedDates.Item2:dd.MM.yyyy HH:mm}")
            Else
                'если кнопки для установки периода
                If Parr Is MW.expPoyasnilka.SelectPeriod Then
                    With My.Settings
                        .NachPeriod = adjustedDates.Item1
                        .KonPeriod = adjustedDates.Item2
                        .TimeMode = PeriodBC.CurrentTimeMode.ToString
                        .nachOLDPeriod = .NachPeriod.AddYears(-1)
                        .konOLDPeriod = .KonPeriod.AddYears(-1)
                        PeriodOKPressed = True
                        ' Логируем результат
                        Tx = ($"В My.Settings установлен период{vbCrLf}с { .NachPeriod:dd.MM.yyyy HH:mm}{vbCrLf}по { .KonPeriod:dd.MM.yyyy HH:mm}")
                        'Обновляем ЧЯрлыки ну и запросы
                        'YarCon.UpdateYarlykInfo()
                        InitFirst()
                        MW.InfoBLOK.AddItem(Tx)
                        MW.InfoBLOK.ScrollToEnd()
                    End With
                ElseIf Parr Is MW.expPoyasnilka.SelectOldPeriod Then
                    With My.Settings
                        .nachOLDPeriod = adjustedDates.Item1
                        .konOLDPeriod = adjustedDates.Item2
                        PeriodOKPressed = True
                        ' Логируем результат
                        Tx = ($"В My.Settings установлен ПРОШЛЫЙ период{vbCrLf}с { .nachOLDPeriod:dd.MM.yyyy HH:mm}{vbCrLf}по { .konOLDPeriod:dd.MM.yyyy HH:mm}")
                        InitFirst()
                        MW.InfoBLOK.AddItem(Tx)
                        MW.InfoBLOK.ScrollToEnd()
                    End With

                    'сюда для других вызывающих кнопок
                    ' =========================================================================
                    ' 🔽 ВОТ ЭТОТ БЛОК ДОБАВИТЬ: Обработка кнопки журнала КАСАНТ 🔽
                    ' =========================================================================
                ElseIf Parr Is MW.JourParam.ParamPeriodCTL.btnSetPeriodJournal Then
                    ' Присваиваем даты напрямую в свойства Fetcher
                    ' Благодаря INotifyPropertyChanged текст на кнопке обновится АВТОМАТИЧЕСКИ
                    Fetcher.NachDat = adjustedDates.Item1
                    Fetcher.KonDat = adjustedDates.Item2
                    My.Settings.TimeMode = "From00To2359"
                    'Fetcher.KonMinut = 0
                    ' Логируем (опционально)
                    Tx = $"Период для КАСАНТ обновлен: {Fetcher.NachKon_TXT} {Fetcher.NachTim}-{Fetcher.KonTim}"
                    MW.InfoBLOK.AddItem(Tx)
                    MW.InfoBLOK.ScrollToEnd()

                    ' =========================================================================

                End If


            End If
            MyCalendar1.ParPop.IsOpen = False
        End Sub

        Private Sub CalendarPopup_Closed(sender As Object, e As EventArgs)
            If PeriodOKPressed = False Then
                PeriodBC.Nach = Nothing
                PeriodBC.Kon = Nothing
            End If
            PeriodOKPressed = False
            IsOpen = False
        End Sub

    End Class
End Namespace

