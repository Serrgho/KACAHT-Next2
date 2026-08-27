Namespace Kas



    Partial Public Class PeriodBlockControl

        Public Enum TimeMode
            From18To18
            From00To2359
        End Enum

        Private _Nach As DateTime?
        Private _Kon As DateTime?
        Private _HasKoyak As Boolean
        Private _CurrentTimeMode As TimeMode = TimeMode.From18To18 ' По умолчанию — "С 18:00 до 18:00"

        Public Property CurrentTimeMode As TimeMode
            Get
                Return _CurrentTimeMode
            End Get
            Set(value As TimeMode)
                _CurrentTimeMode = value
                UpdateDisplayedDates() ' Обновляем отображаемые даты при изменении режима
            End Set
        End Property

        Public Property Nach As DateTime?
            Get
                Return _Nach
            End Get
            Set
                _Nach = Value
                UpdateDisplayedDates() ' Обновляем отображаемые даты
            End Set
        End Property

        Public Property Kon As Date?
            Get
                Return _Kon
            End Get
            Set
                _Kon = Value
                UpdateDisplayedDates() ' Обновляем отображаемые даты
            End Set
        End Property

        Public ReadOnly Property HasKoyak As Boolean
            Get
                Return _Nach > _Kon
            End Get

        End Property

        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().

        End Sub

        Private Sub UserControl_Loaded(sender As Object, e As RoutedEventArgs)

            ' Восстанавливаем режим из настроек
            Dim modeStr As String = My.Settings.TimeMode
            If Not String.IsNullOrEmpty(modeStr) AndAlso
               [Enum].TryParse(Of TimeMode)(modeStr, CurrentTimeMode) Then
                ' OK — режим восстановлен
            Else
                CurrentTimeMode = TimeMode.From00To2359
                My.Settings.TimeMode = "From00To2359"
            End If

            ' Теперь обновляем UI (без вызова событий!)
            ' Отключаем обработчики на время установки
            RemoveHandler But18.Checked, AddressOf But18_Checked
            RemoveHandler But23.Checked, AddressOf But23_Checked

            Select Case CurrentTimeMode
                Case TimeMode.From18To18
                    But18.IsChecked = True
                Case TimeMode.From00To2359
                    But23.IsChecked = True
            End Select

            AddHandler But18.Checked, AddressOf But18_Checked
            AddHandler But23.Checked, AddressOf But23_Checked

            ' Обновляем отображение дат (если Nach/Kon уже заданы)
            UpdateDisplayedDates()
        End Sub

        Private Sub UpdateDisplayedDates()


            ' 🔒 Защита от дизайнер-режима (обязательно!)
            If System.ComponentModel.DesignerProperties.GetIsInDesignMode(Me) Then
                Return
            End If

            ' 🔒 Проверка на Nothing (если убрать - вообще не запустится...)
            If _Nach Is Nothing OrElse _Kon Is Nothing Then
                ' Например, очистить отображение
                DNach.Text = ""
                DKon.Text = ""
                Exit Sub
            End If

            Dim adjustedDates = ApplyTimeMode(_Nach, _Kon)
            DNach.Text = $"{adjustedDates.Item1:dd.MM.yyyy HH:mm}"
            DKon.Text = $"{adjustedDates.Item2:dd.MM.yyyy HH:mm}"

            ' Проверка на пересечение дат
            If _Nach > _Kon Then
                DNach.Foreground = Brushes.Red
                DKon.Foreground = Brushes.Red
            Else
                DNach.Foreground = Brushes.Black
                DKon.Foreground = Brushes.Black
            End If
        End Sub

        Private Sub But18_Checked(sender As Object, e As RoutedEventArgs)
            CurrentTimeMode = TimeMode.From18To18
        End Sub

        Private Sub But23_Checked(sender As Object, e As RoutedEventArgs)
            CurrentTimeMode = TimeMode.From00To2359
            Fetcher.KonTim = 23
            Fetcher.KonMinut = 59
        End Sub

    End Class
End Namespace

