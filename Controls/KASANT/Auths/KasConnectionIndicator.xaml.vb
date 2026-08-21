
Imports System.Net.Http
Imports System.Text
Imports System.Windows.Threading

Namespace Kas
    Partial Public Class KasConnectionIndicator

        Private ReadOnly _timer As New DispatcherTimer()
        Private _fetcherClient As HttpClient ' Используем общий клиент
        Private ReadOnly _checkUrl As String = "http://kasant.gvc.oao.rzd"
        Private _isChecking As Boolean = False

        Public Sub New()
            InitializeComponent()
        End Sub

        ' Инициализация из MainWindow после создания Fetcher
        Public Async Sub Initialize()
            ' Сразу берем клиент из твоего глобального модуля
            _fetcherClient = Fetcher.HttpClient

            ' Подписываемся на событие твоего ГЛОБАЛЬНОГО парсера
            AddHandler Fetcher.ConnectionChanged, Async Sub() Await CheckNow()

            SetupTimer()
            Await CheckNow()


        End Sub

        Private Sub SetupTimer()
            _timer.Interval = TimeSpan.FromSeconds(30)
            AddHandler _timer.Tick, Async Sub(s, e) Await CheckNow()
            _timer.Start()
        End Sub

        Public Async Function CheckNow() As Task

            If _fetcherClient Is Nothing OrElse _isChecking Then Return

            _isChecking = True
            Try
                ' Используем базовый адрес из твоего глобального Fetcher
                Dim checkUrl As String = $"{Fetcher._baseUrl}/index.jsp"

                Dim response = Await _fetcherClient.GetAsync(checkUrl)
                Dim bytes = Await response.Content.ReadAsByteArrayAsync()
                Dim html = Encoding.GetEncoding("windows-1251").GetString(bytes)

                ' Проверка маркеров (теперь точно КАСАНТ, а не оао ржд)
                Dim isLogged As Boolean = response.IsSuccessStatusCode AndAlso
                                 Not html.Contains("anauth_panel") AndAlso
                                 (html.Contains("Журналы") Or html.Contains("Смена"))

                TbStatus.Foreground = If(isLogged, Brushes.DarkGreen, Brushes.Red)
                TbProcc.Text = If(isLogged, "Подключено к КАСАНТ", "Сессия прервана")

            Catch ex As Exception
                TbStatus.Foreground = Brushes.Red
                TbStatus.ToolTip = "Ошибка: " & ex.Message
                TbProcc.Text = "Ошибка сети"
            Finally
                _isChecking = False
            End Try
        End Function

        Public Sub StpTimer()
            _timer.Stop()
        End Sub

        Private Async Sub TbStatus_MouseDown(sender As Object, e As MouseButtonEventArgs)
            ' Мигаем серым при клике
            Dim oldBrush = TbStatus.Foreground
            TbStatus.Foreground = Brushes.Gray
            TbProcc.Text = "Проверка..."

            ' Запускаем проверку
            Await CheckNow()

            ' Если статус не обновился (например, всё еще ошибка), 
            ' CheckNow сам поставит красный, так что возвращать старый цвет не обязательно,
            ' но для красоты можно, если CheckNow отработал слишком быстро.
        End Sub



    End Class
End Namespace



