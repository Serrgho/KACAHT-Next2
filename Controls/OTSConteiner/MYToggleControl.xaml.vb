Namespace Kas
    Partial Public Class MYToggleControl

        '' Событие, которое будет вызываться при изменении положения переключателя
        '' Это позволяет сообщить родительскому окну (MainWindow) о необходимости сброса фильтров
        'Public Event MYToggleControlChanged As EventHandler(Of SortOrderChangedEventArgs)

        Public Property MYToggleZagolovok As String
        ' ===== РЕГИСТРАЦИЯ МАРШРУТИЗИРОВАННЫХ СОБЫТИЙ =====

        ' 🔑 Событие Click (для обработки в разметке)
        Public Shared ReadOnly ClickEvent As RoutedEvent = EventManager.RegisterRoutedEvent(
            "Click",
            RoutingStrategy.Bubble,
            GetType(RoutedEventHandler),
            GetType(MYToggleControl)
        )

        ' Обёртка для события Click
        Public Custom Event Click As RoutedEventHandler
            AddHandler(value As RoutedEventHandler)
                MyBase.AddHandler(ClickEvent, value)
            End AddHandler
            RemoveHandler(value As RoutedEventHandler)
                MyBase.RemoveHandler(ClickEvent, value)
            End RemoveHandler
            RaiseEvent(sender As Object, e As RoutedEventArgs)
                MyBase.RaiseEvent(New RoutedEventArgs(ClickEvent, sender))
            End RaiseEvent
        End Event

        Public Shared ReadOnly IsCheckedProperty As DependencyProperty =
            DependencyProperty.Register(
                "IsChecked",
                GetType(Boolean),
                GetType(MYToggleControl),
                New FrameworkPropertyMetadata(
                    False,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    AddressOf OnIsCheckedChanged
                )
            )

        Public Property IsChecked As Boolean
            Get
                Return CBool(GetValue(IsCheckedProperty))
            End Get
            Set(value As Boolean)
                SetValue(IsCheckedProperty, value)
            End Set
        End Property

        Private Shared Sub OnIsCheckedChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim ctrl = CType(d, MYToggleControl)
            If ctrl.MYToggle IsNot Nothing AndAlso ctrl.MYToggle.IsChecked <> CBool(e.NewValue) Then
                ctrl.MYToggle.IsChecked = CBool(e.NewValue)
            End If
        End Sub


        Sub New()
            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()
        End Sub
        Private Sub UserControl_Loaded(sender As Object, e As RoutedEventArgs)
            'MYToggle.IsChecked = False
            Zagolovok.Text = MYToggleZagolovok
        End Sub

        Private Sub MYToggle_Checked(sender As Object, e As RoutedEventArgs)
            ' Вызываем наше пользовательское событие, передавая новый режим
            ' True означает "сортировка по количеству"
            'RaiseEvent MYToggleControlChanged(Me, New SortOrderChangedEventArgs(True))
            IsChecked = True
        End Sub

        Private Sub MYToggle_Unchecked(sender As Object, e As RoutedEventArgs)
            ' False означает "сортировка по имени"
            'RaiseEvent MYToggleControlChanged(Me, New SortOrderChangedEventArgs(False))
            IsChecked = False
        End Sub

        ' 🔑 НОВОЕ: Пробрасываем клик от внутреннего ToggleButton наружу
        Private Sub MYToggle_Click(sender As Object, e As RoutedEventArgs) Handles MYToggle.Click
            RaiseEvent Click(Me, e)
        End Sub
    End Class




    '' Класс для аргументов нашего пользовательского события
    '' Содержит информацию о новом выбранном состоянии переключателя (IsChecked)
    'Public Class SortOrderChangedEventArgs
    '    Inherits EventArgs

    '    Public Property IsChecked As Boolean
    '    ' Конструктор класса
    '    Public Sub New(isChecked As Boolean)
    '        Me.IsChecked = isChecked
    '    End Sub
    'End Class

End Namespace

