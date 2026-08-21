Imports System.Windows.Threading

Namespace Kas
    Partial Public Class InfoLBoxControl

        ' Объявляем таймер
        Private ReadOnly _clearTimer As New DispatcherTimer()

        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().
            InformLB = Me

            ' --- НАСТРОЙКА ТАЙМЕРА ---
            _clearTimer.Interval = TimeSpan.FromSeconds(300) ' Интервал 20 секунд
            AddHandler _clearTimer.Tick, AddressOf ClearTimer_Tick

            ' -------------------------

            ' Подписываемся на событие выгрузки, чтобы остановить таймер (хорошая практика)
            AddHandler Me.Unloaded, AddressOf InfoLBoxControl_Unloaded

        End Sub


        ' Обработчик события таймера
        Private Sub ClearTimer_Tick(sender As Object, e As EventArgs)
            ClearItems()
        End Sub

        ' Остановка таймера при удалении контроля из памяти
        Private Sub InfoLBoxControl_Unloaded(sender As Object, e As RoutedEventArgs)
            _clearTimer.Stop()
            RemoveHandler _clearTimer.Tick, AddressOf ClearTimer_Tick
            RemoveHandler Me.Unloaded, AddressOf InfoLBoxControl_Unloaded
        End Sub




        Private Sub ClearText_Click(sender As Object, e As RoutedEventArgs)
            ClearItems()
        End Sub

        Sub AddItem(item As Object, Optional ToEnd As Boolean = False)
            InfoLB.Items.Add(item)

            If ToEnd Then
                InfoLB.ScrollIntoView(InfoLB.Items(InfoLB.Items.Count - 1))
            End If
            _clearTimer.Stop()
            _clearTimer.Start()
        End Sub

        Sub ClearItems()
            InfoLB.ItemsSource = Nothing
            InfoLB.Items.Clear()
            _clearTimer.Stop()
        End Sub

        Public Sub ScrollToEnd()
            InfoLB.ScrollIntoView(InfoLB.Items(InfoLB.Items.Count - 1))
        End Sub

        Public Function EndOfMSG() As String
            Return $"============ {DateTime.Now.ToString("HH:mm:ss.fff")}"
        End Function

    End Class
End Namespace

