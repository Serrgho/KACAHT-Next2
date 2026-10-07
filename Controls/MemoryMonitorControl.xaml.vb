
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Windows.Threading
Imports System.Windows.Input
Imports System.Windows.Media

Namespace Kas

    ''' <summary>
    ''' Мониторинг памяти: Working Set, managed heap, Private Bytes.
    ''' Клик по основной строке — форсированный GC + запись замера.
    ''' </summary>
    Partial Public Class MemoryMonitorControl

        ' --- Замеры ---
        Private _previousMemoryMB As Long = 0
        Private _minMemoryMB As Long = Long.MaxValue
        Private _maxMemoryMB As Long = 0
        Private _measureCount As Integer = 0

        ' --- История за сессию ---
        Private Class MemSample
            Public Property Time As DateTime
            Public Property WsMB As Long
            Public Property GcMB As Long
            Public Property PrivMB As Long
        End Class

        Private ReadOnly _log As New List(Of MemSample)()

        ' --- Таймер ---
        Private _timer As DispatcherTimer
        Private _updateInterval As Integer = 1000
        Private _isMonitoring As Boolean = False

        ' --- Счётчик производительности ---
        Private _ramCounter As PerformanceCounter
        Private _processName As String

        ''' <summary>Интервал обновления в мс.</summary>
        Public Property UpdateInterval As Integer
            Get
                Return _updateInterval
            End Get
            Set(value As Integer)
                If value > 0 Then
                    _updateInterval = value
                    If _timer IsNot Nothing Then
                        _timer.Interval = TimeSpan.FromMilliseconds(_updateInterval)
                    End If
                End If
            End Set
        End Property

        Public Sub New()
            InitializeComponent()



            _timer = New DispatcherTimer(DispatcherPriority.Background)
            _timer.Interval = TimeSpan.FromMilliseconds(_updateInterval)
            AddHandler _timer.Tick, AddressOf Timer_Tick

            AddHandler Me.Unloaded, AddressOf MemoryMonitorControl_Unloaded

            Dispatcher.BeginInvoke(New Action(Sub()
                                                  InitPerformanceCounter()
                                                  UpdateDisplay()
                                              End Sub), DispatcherPriority.ApplicationIdle)

            StartMonitoring()
        End Sub

        Private Sub InitPerformanceCounter()
            Try
                _processName = Process.GetCurrentProcess().ProcessName
                _ramCounter = New PerformanceCounter(
                    categoryName:="Process",
                    counterName:="Working Set - Private",
                    instanceName:=_processName,
                    readOnly:=True)
                _ramCounter.NextValue() ' прогрев
            Catch
                _ramCounter = Nothing
            End Try
        End Sub

        Public Sub StartMonitoring()
            If Not _isMonitoring AndAlso _timer IsNot Nothing Then
                _isMonitoring = True
                _timer.Start()
                UpdateDisplay()
            End If
        End Sub

        Public Sub StopMonitoring()
            If _isMonitoring AndAlso _timer IsNot Nothing Then
                _isMonitoring = False
                _timer.Stop()
            End If
        End Sub

        ' --- Измерения ---

        Private Function GetWsMB() As Long
            Try
                If _ramCounter IsNot Nothing Then
                    Return CLng(_ramCounter.NextValue()) \ (1024 * 1024)
                End If
            Catch
                InitPerformanceCounter()
            End Try
            Return 0
        End Function

        Private Shared Function GetManagedMB() As Long
            Return GC.GetTotalMemory(False) \ (1024 * 1024)
        End Function

        Private Shared Function GetPrivateMB() As Long
            Try
                Return Process.GetCurrentProcess().PrivateMemorySize64 \ (1024 * 1024)
            Catch
                Return 0
            End Try
        End Function

        ' --- Тик ---

        Private Sub Timer_Tick(sender As Object, e As EventArgs)
            ' Не дёргаем GC каждую секунду — только читаем
            Dim ws = GetWsMB()
            Dim gc = GetManagedMB()
            Dim pb = GetPrivateMB()

            ' Обновляем UI в потоке диспетчера
            Dispatcher.BeginInvoke(New Action(Sub()
                                                  UpdateDisplayWithTrend(ws, gc, pb)
                                              End Sub))
        End Sub

        ' --- Обновление UI ---

        Private Sub UpdateDisplay()
            UpdateDisplayWithTrend(GetWsMB(), GetManagedMB(), GetPrivateMB())
        End Sub

        Private Sub UpdateDisplayWithTrend(wsMB As Long, gcMB As Long, privMB As Long)
            Try
                ' Min / Max только по WS
                If wsMB > 0 Then
                    If wsMB < _minMemoryMB Then _minMemoryMB = wsMB
                    If wsMB > _maxMemoryMB Then _maxMemoryMB = wsMB
                End If

                ' Тренд
                Dim trend As String
                Dim brush As Brush
                If _previousMemoryMB = 0 Then
                    trend = "▶" : brush = Brushes.Gray
                ElseIf wsMB > _previousMemoryMB Then
                    trend = "▲" : brush = Brushes.Red
                ElseIf wsMB < _previousMemoryMB Then
                    trend = "▼" : brush = Brushes.Green
                Else
                    trend = "▶" : brush = Brushes.Gray
                End If

                TxtMemory.Text = $"Mem: {wsMB} MB {trend}"
                TxtMemory.Foreground = brush

                TxtManaged.Text = $"GC: {gcMB} · Priv: {privMB}"

                Dim minShow = If(_minMemoryMB = Long.MaxValue, 0, _minMemoryMB)
                TxtPeak.Text = $"Min: {minShow} / Max: {_maxMemoryMB} ({_measureCount} зам.)"

                _previousMemoryMB = wsMB

            Catch
                TxtMemory.Text = "Mem: Error"
                TxtMemory.Foreground = Brushes.Red
            End Try
        End Sub

        ' --- Клики ---

        ''' <summary>Клик по основной строке — форсированный GC + замер в лог.</summary>
        Private Sub TxtMemory_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
            ForceGCAndLog("manual")
        End Sub

        ''' <summary>Клик по строке GC — просто замер без GC.</summary>
        Private Sub TxtManaged_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
            LogCurrent("read")
        End Sub

        ''' <summary>Клик по Min/Max — сброс + экспорт лога.</summary>
        Private Sub TxtPeak_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
            If _log.Count > 0 Then
                ExportLog()
                ResetStats()
            Else
                ResetStats()
            End If
        End Sub

        ' --- GC + лог ---

        Private Sub ForceGCAndLog(tag As String)
            Try
                GC.Collect()
                GC.WaitForPendingFinalizers()
                GC.Collect()
                GC.WaitForPendingFinalizers()
            Catch
            End Try

            LogCurrent(tag)
        End Sub

        Private Sub LogCurrent(tag As String)
            Dim ws = GetWsMB()
            Dim gc = GetManagedMB()
            Dim pb = GetPrivateMB()

            _log.Add(New MemSample With {
                .Time = DateTime.Now,
                .WsMB = ws,
                .GcMB = gc,
                .PrivMB = pb
            })
            _measureCount += 1

            ' Обновляем UI
            UpdateDisplayWithTrend(ws, gc, pb)

            ' Пишем в InfoBLOK, если доступен
            Try
                Dim deltaWs As Long = 0
                If _log.Count >= 2 Then
                    deltaWs = ws - _log(_log.Count - 2).WsMB
                End If

                Dim msg = $"[MEM {tag}] WS: {ws} MB ({deltaWs:+#;-#;0}), GC: {gc} MB, Priv: {pb} MB"
                MW.InfoBLOK?.AddItem(msg)
            Catch
                Debug.WriteLine($"[MEM {tag}] WS: {ws} MB, GC: {gc} MB, Priv: {pb} MB")
            End Try
        End Sub

        ' --- Сброс и экспорт ---

        Public Sub ResetStats()
            _minMemoryMB = Long.MaxValue
            _maxMemoryMB = 0
            _measureCount = 0
            _log.Clear()
            TxtPeak.Text = "Min: 0 / Max: 0 (0 зам.)"
        End Sub

        Private Sub ExportLog()
            Try
                ' Папка рядом с данными приложения
                Dim basePath As String = My.Settings.DataFolderPath
                If String.IsNullOrWhiteSpace(basePath) Then
                    ' На случай, если настройка пустая — падаем в LocalAppData
                    basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                End If

                Dim dir = Path.Combine(basePath, "memlogs")
                Directory.CreateDirectory(dir)

                Dim fn = Path.Combine(dir, $"mem_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt")

                Dim sb As New StringBuilder()
                sb.AppendLine($"# Memory log {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                sb.AppendLine("# Time`tWS`tGC`tPriv`tdWS`tManaged")
                sb.AppendLine("# --------------------------------------------------")

                Dim prevWs As Long = 0
                For Each s In _log
                    Dim d = If(prevWs = 0, 0, s.WsMB - prevWs)
                    sb.AppendLine($"{s.Time:HH:mm:ss}`t{s.WsMB}`t{s.GcMB}`t{s.PrivMB}`t{d:+#;-#;0}`t{s.GcMB}")
                    prevWs = s.WsMB
                Next

                File.WriteAllText(fn, sb.ToString(), Encoding.UTF8)

                Try
                    MW.InfoBLOK?.AddItem($"[MEM] Лог сохранён: {fn}")
                Catch
                    Debug.WriteLine($"[MEM] Лог сохранён: {fn}")
                End Try

            Catch ex As Exception
                Debug.WriteLine($"[MEM] Ошибка экспорта: {ex.Message}")
            End Try
        End Sub

        Private Sub MemoryMonitorControl_Unloaded(sender As Object, e As RoutedEventArgs)
            StopMonitoring()

            If _ramCounter IsNot Nothing Then
                Try
                    _ramCounter.Close()
                    _ramCounter.Dispose()
                Catch
                End Try
                _ramCounter = Nothing
            End If
        End Sub

    End Class

End Namespace









'Imports System.Diagnostics
'Imports System.IO
'Imports System.Text.Json
'Imports System.Threading
'Imports System.Windows.Threading

'Imports System.Runtime.InteropServices


'Namespace Kas



'    ''' <summary>
'    ''' UserControl для мониторинга потребления памяти приложением
'    ''' </summary>
'    Partial Public Class MemoryMonitorControl

'        ' Замер памяти
'        Private _measureStartMB As Long = 0
'        Private _measureStartTime As DateTime
'        Private _measureName As String = ""
'        Private _isMeasuring As Boolean = False


'        ''' <summary>
'        ''' Сбросить замер без записи в историю.
'        ''' Используется при исключениях, чтобы не «залипнуть» в режиме измерения.
'        ''' </summary>
'        Public Sub CancelMeasure()
'            _isMeasuring = False
'            _measureName = ""
'            _measureStartMB = 0
'        End Sub


'        Private _timer As DispatcherTimer
'        Private _updateInterval As Integer = 1000
'        Private _previousMemoryMB As Long = 0
'        Private _isMonitoring As Boolean = False
'        Private _maxExpectedMemory As Long = 2000
'        Private _historyFilePath As String = ""
'        'Private _memoryHistory As New List(Of MemoryRecord)

'        ' Кэшируем счётчик производительности — создавать его каждый тик дорого
'        Private _ramCounter As PerformanceCounter
'        Private _processName As String

'        ''' <summary>
'        ''' Интервал обновления в миллисекундах
'        ''' </summary>
'        Public Property UpdateInterval As Integer
'            Get
'                Return _updateInterval
'            End Get
'            Set(value As Integer)
'                If value > 0 Then
'                    _updateInterval = value
'                    If _timer IsNot Nothing Then
'                        _timer.Interval = TimeSpan.FromMilliseconds(_updateInterval)
'                    End If
'                End If
'            End Set
'        End Property

'        ''' <summary>
'        ''' Максимальное значение для шкалы индикатора (МБ)
'        ''' </summary>
'        Public Property MaxExpectedMemory As Long
'            Get
'                Return _maxExpectedMemory
'            End Get
'            Set(value As Long)
'                If value > 0 Then
'                    _maxExpectedMemory = value
'                End If
'            End Set
'        End Property



'        Public Sub New()
'            InitializeComponent()

'            _timer = New DispatcherTimer(DispatcherPriority.Background)
'            _timer.Interval = TimeSpan.FromMilliseconds(_updateInterval)
'            AddHandler _timer.Tick, AddressOf Timer_Tick

'            AddHandler Me.Unloaded, AddressOf MemoryMonitorControl_Unloaded

'            ' Запускаем инициализацию, когда UI освободится
'            Dispatcher.BeginInvoke(New Action(Sub()
'                                                  InitPerformanceCounter()
'                                              End Sub), DispatcherPriority.ApplicationIdle)

'            StartMonitoring()
'        End Sub

'        ''' <summary>
'        ''' Инициализация счётчика производительности "Working Set - Private".
'        ''' Именно это значение показывает Диспетчер задач в колонке "Память".
'        ''' </summary>
'        Private Sub InitPerformanceCounter()
'            Try
'                _processName = Process.GetCurrentProcess().ProcessName

'                _ramCounter = New PerformanceCounter(
'                    categoryName:="Process",
'                    counterName:="Working Set - Private",
'                    instanceName:=_processName,
'                    readOnly:=True)

'                ' Первый вызов NextValue() часто возвращает 0 — прогреваем счётчик,
'                ' чтобы избежать скачка при первом отображении.
'                _ramCounter.NextValue()
'            Catch ex As Exception
'                _ramCounter = Nothing
'            End Try
'        End Sub

'        ''' <summary>
'        ''' Запуск мониторинга памяти
'        ''' </summary>
'        Public Sub StartMonitoring()
'            If Not _isMonitoring AndAlso _timer IsNot Nothing Then
'                _isMonitoring = True
'                _timer.Start()
'                UpdateDisplay()
'            End If
'        End Sub

'        ''' <summary>
'        ''' Остановка мониторинга памяти
'        ''' </summary>
'        Public Sub StopMonitoring()
'            If _isMonitoring AndAlso _timer IsNot Nothing Then
'                _isMonitoring = False
'                _timer.Stop()
'                'SaveHistoryToJson()
'            End If
'        End Sub



'        ''' <summary>
'        ''' Возвращает текущее потребление памяти в МБ (как в Диспетчере задач).
'        ''' </summary>
'        Private Function GetCurrentMemoryMB() As Long
'            Try
'                If _ramCounter IsNot Nothing Then
'                    Dim bytes As Long = CLng(_ramCounter.NextValue())
'                    Return bytes \ (1024 * 1024)
'                End If
'            Catch ex As Exception
'                ' Если счётчик стал недоступен (например, процесс перезапустился)
'                ' — пробуем пересоздать.
'                InitPerformanceCounter()
'            End Try

'            Return 0
'        End Function

'        Private Sub Timer_Tick(sender As Object, e As EventArgs)
'            Task.Run(Sub()
'                         Try
'                             Dim memoryMB As Long = GetCurrentMemoryMB()

'                             Dispatcher.Invoke(Sub()
'                                                   UpdateDisplayWithTrend(memoryMB)
'                                               End Sub)
'                         Catch ex As Exception
'                             Dispatcher.Invoke(Sub()
'                                                   TxtMemory.Text = "Mem: Error"
'                                                   TxtMemory.Foreground = Brushes.Red
'                                               End Sub)
'                         End Try
'                     End Sub)
'        End Sub

'        Private Sub UpdateDisplayWithTrend(currentMemoryMB As Long)
'            Try
'                Dim trend As String = ""
'                Dim indicatorColor As String

'                If _previousMemoryMB = 0 Then
'                    trend = "▶"
'                    indicatorColor = "Gray"
'                ElseIf currentMemoryMB > _previousMemoryMB Then
'                    trend = "▲"
'                    indicatorColor = "Red"
'                ElseIf currentMemoryMB < _previousMemoryMB Then
'                    trend = "▼"
'                    indicatorColor = "Green"
'                Else
'                    trend = "▶"
'                    indicatorColor = "Gray"
'                End If

'                TxtMemory.Text = $"Mem: {currentMemoryMB} MB {trend}"
'                TxtMemory.Foreground = New SolidColorBrush(CType(System.Windows.Media.ColorConverter.ConvertFromString(indicatorColor), System.Windows.Media.Color))


'                _previousMemoryMB = currentMemoryMB

'            Catch ex As Exception
'                TxtMemory.Text = "Mem: Error"
'                TxtMemory.Foreground = Brushes.Red
'            End Try
'        End Sub

'        Private Sub UpdateDisplay()
'            Try
'                Dim memoryMB As Long = GetCurrentMemoryMB()
'                UpdateDisplayWithTrend(memoryMB)
'            Catch ex As Exception
'                TxtMemory.Text = "Mem: Error"
'                TxtMemory.Foreground = Brushes.Red
'            End Try
'        End Sub



'        Private Sub MemoryMonitorControl_Unloaded(sender As Object, e As RoutedEventArgs)
'            StopMonitoring()

'            ' Освобождаем счётчик производительности
'            If _ramCounter IsNot Nothing Then
'                Try
'                    _ramCounter.Close()
'                    _ramCounter.Dispose()
'                Catch
'                End Try
'                _ramCounter = Nothing
'            End If
'        End Sub

'    End Class


'End Namespace


