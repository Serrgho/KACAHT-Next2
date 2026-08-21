Namespace Kas
    Partial Public Class TitleBar

        ' Не используем Shared, чтобы избежать конфликтов между экземплярами TitleBar
        ' Каждый экземпляр TitleBar должен иметь свою собственную ссылку на родительское окно,
        ' иначе при закрытии дочернего окна функционал родительского может сломаться.
        Public Property ParentWin As Window
        Public Property NoMaximize As Boolean = False

        Private _BorderLineColor As Brush = Brushes.Black
        Public Property BorderLineColor As Brush
            Get
                Return _BorderLineColor
            End Get
            Set(value As Brush)
                _BorderLineColor = value
                MainBorder.BorderBrush = _BorderLineColor
            End Set
        End Property

        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().

        End Sub

        ' Конструктор с параметром для программного использования
        Public Sub New(owner As Window)
            InitializeComponent()
            ParentWin = owner
        End Sub


        Private Sub OnLoaded(sender As Object, e As RoutedEventArgs)
            If ParentWin Is Nothing Then
                ParentWin = Window.GetWindow(Me)
            End If

            If ParentWin Is Nothing Then
                'Debug.WriteLine("❌ Не удалось найти родительское окно!")
            Else
                'Debug.WriteLine($"✅ Найдено окно: {ParentWin.Title}")
                WinTitle.Text = ParentWin.Title
                TBarIcon.Source = ParentWin.Icon
            End If

            If NoMaximize Then
                MaxBu.Visibility = Visibility.Collapsed
            End If
        End Sub

        Private Sub TBAR_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
            ParentWin.DragMove()
        End Sub

        Private Sub Minimize_Click(sender As Object, e As MouseButtonEventArgs)
            If ParentWin IsNot Nothing Then
                ParentWin.WindowState = WindowState.Minimized
            End If

        End Sub

        Private Sub Maximize_Click(sender As Object, e As MouseButtonEventArgs)

            If ParentWin IsNot Nothing Then
                If ParentWin.WindowState = WindowState.Maximized Then
                    ParentWin.WindowState = WindowState.Normal
                Else
                    ParentWin.WindowState = WindowState.Maximized
                End If
            End If
        End Sub

        Private Sub Close_Click(sender As Object, e As MouseButtonEventArgs)
            ParentWin?.Close()
        End Sub
    End Class
End Namespace


