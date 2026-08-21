Imports System.Linq.Expressions
Imports System.Net
Imports System.Net.Http
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Windows.Controls.Primitives
Imports System.Windows.Input

Imports System.Windows.Media
Imports HtmlAgilityPack
Imports Microsoft.SqlServer
Imports Microsoft.VisualBasic.Logging

Namespace Kas

    ' 1. Класс для хранения данных депо
    Public Class DepotItem
        Public Property Name As String
        Public Property Id As Integer
    End Class


    Partial Public Class KasAntWin
        Inherits Window
        Public Property OTSNUM As String = ""
        Public Property DorKod As Integer
        Public Event ConnectionStateChanged()

        ' В классе KasAntWin, после объявления HistSP:
        Public ReadOnly Property HistoryPanel As StackPanel
            Get
                Return HistSP
            End Get
        End Property

        ' 2. Инициализация списка депо (ВЫЗОВИ ЭТОТ МЕТОД В Window_Loaded)
        Private Sub InitializeDepotComboBox()
            cmbDepots.Items.Clear()
            ' ID взяты точно из ответа сервера preds.jsp
            cmbDepots.Items.Add(New DepotItem With {.Name = "ТЧЭ-1", .Id = 340})
            cmbDepots.Items.Add(New DepotItem With {.Name = "ТЧЭ-2", .Id = 309})
            cmbDepots.Items.Add(New DepotItem With {.Name = "ТЧЭ-3", .Id = 233})
            cmbDepots.Items.Add(New DepotItem With {.Name = "ТЧЭ-5", .Id = 296})
            cmbDepots.Items.Add(New DepotItem With {.Name = "ТЧЭ-7", .Id = 351})

            ' Выбираем первое по умолчанию
            If cmbDepots.Items.Count > 0 Then cmbDepots.SelectedIndex = 0
        End Sub

        ' ← ← ← ТАЙМЕР ДЛЯ ОЧИСТКИ СТАТУСА
        Private _statusClearTimer As New System.Windows.Threading.DispatcherTimer()
        Private _statusText As String = ""  ' ← Храним текст отдельно


        Public Sub New()
            InitializeComponent()

            ' ← ← ← НАСТРОЙКА ТАЙМЕРА
            _statusClearTimer.Interval = TimeSpan.FromSeconds(7)
            AddHandler _statusClearTimer.Tick, AddressOf OnStatusTimerTick
            LoadSettings()
            InitializeDepotComboBox()
        End Sub

        Public Sub New(violId As String, DKod As Integer)
            InitializeComponent()

            ' ← ← ← НАСТРОЙКА ТАЙМЕРА
            _statusClearTimer.Interval = TimeSpan.FromSeconds(7)
            AddHandler _statusClearTimer.Tick, AddressOf OnStatusTimerTick
            OTSNUM = violId  ' ← Сначала устанавливаем значение
            DorKod = DKod
            txtViolId.Text = violId
            txtDorKod.Text = DKod
            'DORCODE = DORKod
            LoadSettings()  ' ← Потом загружаем настройки
            PrepAndLoad()
            InitializeDepotComboBox()
        End Sub

        Private Sub RestoreWindowGeometry()
            With My.Settings
                Me.WindowStartupLocation = WindowStartupLocation.Manual

                ' --- Позиция ---
                Dim savedPoint As New System.Drawing.Point(.ParcerLeft, .ParcerTop)
                Dim isVisible As Boolean = False
                For Each scr In System.Windows.Forms.Screen.AllScreens
                    If scr.WorkingArea.Contains(savedPoint) Then
                        isVisible = True
                        Exit For
                    End If
                Next

                If isVisible Then
                    Me.Top = .ParcerTop
                    Me.Left = .ParcerLeft
                Else
                    Me.WindowStartupLocation = WindowStartupLocation.CenterScreen
                End If

                ' --- Размеры ---
                If .ParcerWidth > 100 Then Me.Width = .ParcerWidth
                If .ParcerHeight > 100 Then Me.Height = .ParcerHeight

                ' --- Состояние (Normal / Maximized) ---
                If .ParcerWindowState = WindowState.Normal OrElse
           .ParcerWindowState = WindowState.Maximized Then
                    Me.WindowState = CType(.ParcerWindowState, WindowState)
                End If
            End With
        End Sub



        ' ← ← ← ОБРАБОТЧИК ТАЙМЕРА
        Private Sub OnStatusTimerTick(sender As Object, e As EventArgs)
            _statusClearTimer.Stop()  ' Останавливаем таймер
            lblStatus.Text = ""       ' Очищаем текст
        End Sub

        ' ← ← ← МЕТОД ДЛЯ УСТАНОВКИ СТАТУСА ()
        Public Sub SetStatus(text As String, Optional color As Brush = Nothing)
            _statusText = text
            lblStatus.Text = text

            If color IsNot Nothing Then
                lblStatus.Foreground = color
            End If

            ' ← ← ← СБРАСЫВАЕМ ТАЙМЕР
            _statusClearTimer.Stop()
            _statusClearTimer.Start()
        End Sub


        Private Sub LoadSettings()
            Try
                If Not String.IsNullOrWhiteSpace(OTSNUM) Then
                    txtViolId.Text = OTSNUM
                    txtDorKod.Text = DorKod.ToString
                End If
                RestoreWindowGeometry()

            Catch ex As Exception
            End Try
        End Sub


        Private Sub btnTestFile_Click(sender As Object, e As RoutedEventArgs)

            Dim openFileDialog As New Microsoft.Win32.OpenFileDialog()
            openFileDialog.Filter = "HTML файлы|*.html|Все файлы|*.*"
            openFileDialog.Title = "Выберите HTML файл отказа"

            If openFileDialog.ShowDialog() = True Then
                btnLoad.IsEnabled = False

                SetStatus("Парсинг локального файла...")
                Try
                    If PointedOtkaz IsNot Nothing Then
                        txtViolId.Text = PointedOtkaz.Id
                        txtDorKod.Text = "88"
                    End If

                    Dim data = Fetcher.ParseLocalHtmlFile(openFileDialog.FileName, txtViolId.Text)
                    OutToControl(data)

                    'SetStatus("Готово!")
                Catch ex As Exception
                    MessageBox.Show($"Ошибка: {ex.Message}")
                Finally
                    btnLoad.IsEnabled = True
                End Try
            End If
            e.Handled = True
        End Sub

        Private _callCount As Integer = 0  ' ← ← ← Поле класса, не Static!
        Private _isLoading As Boolean = False

        Private Sub btnLoad_Click(sender As Object, e As RoutedEventArgs)
            PrepAndLoad()
            e.Handled = True
        End Sub

        Async Sub PrepAndLoad()
            _isLoading = True
            btnLoad.IsEnabled = False
            SetStatus("Проверка сессии...")

            Try


                ' --- ШАГ 1: ПРОВЕРЯЕМ, НУЖЕН ЛИ ВХОД ---
                Dim alreadyLogged = Await Fetcher.IsLoggedInAsync()

                If Not alreadyLogged Then
                    SetStatus("Авторизация...")
                    Dim username = MW.AuthCtrl.txtLogin.Text.Trim()
                    Dim password = MW.AuthCtrl.txtPassword.Password

                    Dim loginSuccess = Await Fetcher.LoginAsync(username, password)

                    If loginSuccess Then
                        SaveSettings()
                        ' Оповещаем другие окна, что мы вошли
                        RaiseEvent ConnectionStateChanged()
                    Else
                        Throw New Exception("Не удалось войти...")
                    End If
                    SaveSettings()
                End If

                ' --- ШАГ 2: ЗАГРУЖАЕМ ДАННЫЕ (сессия уже точно есть) ---
                SetStatus("Загрузка данных...")
                Dim data = Await Fetcher.FetchOtsDataAsync(txtViolId.Text, txtDorKod.Text)

                OutToControl(data)
                'SetStatus("Готово!", Brushes.Green)

            Catch ex As Exception
                SetStatus($"Ошибка: {ex.Message}", Brushes.Red)
                ShowMSG(Me, $"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error)
            Finally
                _isLoading = False
                btnLoad.IsEnabled = True
            End Try
        End Sub


        Sub OutToControl(data As OtsData)
            Try


                ' ← ← ← ОЧИСТКА ВСЕХ ПАНЕЛЕЙ
                HeaderSPan.Children.Clear()       ' Заголовок
                MestoSP.Children.Clear()        ' Параметры отказа
                pnlTrainInfo.Children.Clear()   ' Задержанные поезда
                HistSP.Children.Clear()         ' История
                DocsSP.Children.Clear()         ' Документы


                ' Выводим отладочную информацию
                'SetStatus($"Получено: Место='{data.Location}', Поездов={data.DelayedTrains.Count}", Brushes.Blue)


                ' ================= МЕСТО ОТКАЗА =================
                If Not String.IsNullOrWhiteSpace(data.Location) Then
                    Dim Texxtt As String = $"{data.Location}{vbCrLf}начало: {If(data.StartTime, "")} - {If(data.EndTime, "")}  ({If(data.Duration, "")})"
                    Dim Elem As RichTextBox = CreateLabel(Texxtt, 16, FontWeights.Bold,, Brushes.DarkBlue)
                    Elem.HorizontalAlignment = HorizontalAlignment.Stretch
                    'Elem.HorizontalContentAlignment = HorizontalAlignment.Center
                    ' ✅ ЦЕНТРИРУЕМ ТЕКСТ ВНУТРИ ДОКУМЕНТА
                    Dim p As Paragraph = TryCast(Elem.Document.Blocks.FirstBlock, Paragraph)
                    If p IsNot Nothing Then
                        p.TextAlignment = TextAlignment.Center
                    End If
                    Elem.Background = CType(FindResource("ScrolBarBrush"), Brush)
                    HeaderSPan.Children.Add(Elem)
                End If

                ' ================= КАТЕГОРИИ =================
                If Not String.IsNullOrWhiteSpace(data.Category775) Then
                    HeaderSPan.Children.Add(CreateLabel(data.Category775, 16, FontWeights.SemiBold,, IIf(data.Category775 = "3", Brushes.DarkGreen, Brushes.Red), "Категория 775р: ", IIf(data.Category775 = "3", Brushes.DarkGreen, Brushes.Red)))
                End If

                If Not String.IsNullOrWhiteSpace(data.Category1052) Then
                    HeaderSPan.Children.Add(CreateLabel(data.Category1052, 16, FontWeights.SemiBold,, IIf(data.Category1052 = "3", Brushes.DarkGreen, Brushes.Red), "Категория 1052р: ", IIf(data.Category1052 = "3", Brushes.DarkGreen, Brushes.Red)))
                End If

                If Not String.IsNullOrWhiteSpace(data.Category1915) Then
                    HeaderSPan.Children.Add(CreateLabel(data.Category1915, 16, FontWeights.SemiBold,, IIf(data.Category1915 = "3", Brushes.DarkGreen, Brushes.Red), "Категория 1915р: ", IIf(data.Category1915 = "3", Brushes.DarkGreen, Brushes.Red)))
                End If
                ' ================= ПРИЗНАК "ПЕРЕДАН" =================
                If Not String.IsNullOrWhiteSpace(data.InvestigationStatus) Then
                    HeaderSPan.Children.Add(CreateLabel(data.InvestigationStatus, 16, FontWeights.Bold,, Brushes.DarkGreen))
                End If
                ' ================= ПРИЗНАК "СОБЫТИЕ" =================
                If Not String.IsNullOrWhiteSpace(data.Consequences) Then

                    Dim consLabel As New TextBlock With {
                        .Text = data.Consequences,
                        .TextWrapping = TextWrapping.Wrap,
                        .FontSize = 13,
                        .FontWeight = FontWeights.Normal,
                        .Foreground = Brushes.DarkRed,
                        .Cursor = Cursors.Hand,
                        .ToolTip = "Нажмите, чтобы установить признак события"
                    }
                    AddHandler consLabel.MouseLeftButtonUp, Sub(s, e)
                                                                SetPointedOtkazProperty(data.ViolId, Function(p) p.ISSobyt, True)
                                                            End Sub

                    HeaderSPan.Children.Add(consLabel)
                End If
                ' ================= ПРИЗНАК "КОРП НАРУШ" =================
                If Not String.IsNullOrWhiteSpace(data.Korporativ) Then
                    Dim korpLabel As New TextBlock With {
                        .Text = data.Korporativ,
                        .TextWrapping = TextWrapping.Wrap,
                        .FontSize = 13,
                        .FontWeight = FontWeights.Normal,
                        .Foreground = Brushes.DarkRed,
                        .Cursor = Cursors.Hand,
                        .ToolTip = "Нажмите, чтобы установить признак Корпоратив. нарушения"
                    }
                    AddHandler korpLabel.MouseLeftButtonUp, Sub(s, e)
                                                                SetPointedOtkazProperty(data.ViolId, Function(p) p.ISKorp, True)
                                                            End Sub

                    HeaderSPan.Children.Add(korpLabel)
                End If
                ' ================= ПРИЗНАК "ОПАСНЫЙ" =================
                If Not String.IsNullOrWhiteSpace(data.DangerStatus) Then
                    Dim DangLabel As New TextBlock With {
                        .Text = data.DangerStatus,
                        .TextWrapping = TextWrapping.Wrap,
                        .FontSize = 15,
                        .FontWeight = FontWeights.Bold,
                        .Foreground = Brushes.Red,
                        .Cursor = Cursors.Hand,
                        .ToolTip = "Нажмите, чтобы установить признак Опасного отказа"
                    }
                    AddHandler DangLabel.MouseLeftButtonUp, Sub(s, e)
                                                                SetPointedOtkazProperty(data.ViolId, Function(p) p.ISDanger, True)
                                                            End Sub

                    HeaderSPan.Children.Add(DangLabel)
                End If

                ' ================= ФАКТИЧЕСКОЕ ВРЕМЯ НАЧАЛА =================
                If Not String.IsNullOrWhiteSpace(data.FactStartTime) Then
                    Dim timeText = $" {data.FactStartTime} - {data.FactEndTime}"
                    If Not String.IsNullOrWhiteSpace(data.FactDuration) Then
                        timeText += $" ({data.FactDuration})"
                    End If
                    'MestoSP.Children.Add(CreateLabel("Фактическое время:", 14, FontWeights.SemiBold, Brushes.Red))
                    MestoSP.Children.Add(CreateLabel(timeText, 14, FontWeights.Normal,, Brushes.Red, "Фактическое время:"))

                    'MestoSP.Children.Add(CreateLabel(data.FactStartTime, 14, FontWeights.Bold, Brushes.Red))
                End If

                ' ================= КОММЕНТАРИЙ =================
                If Not String.IsNullOrWhiteSpace(data.CharacterComment) Then
                    MestoSP.Children.Add(CreateLabel(data.CharacterComment, 14, FontWeights.SemiBold,, Brushes.DarkRed, "Комментарий: "))
                    'MestoSP.Children.Add(CreateLabel($" {data.CharacterComment}", 14, FontWeights.Normal, Brushes.DarkRed))
                End If

                ' ================= 5.1. ВРЕМЯ К УЧЕТУ (PCHasy) =================
                If Not String.IsNullOrWhiteSpace(data.PCHasy) Then
                    Dim pchLabel = CreateLabel(data.PCHasy, 14, FontWeights.Bold,, Brushes.Red, "Потери поездо-часов:")
                    With pchLabel
                        .Cursor = Cursors.Hand
                        .Tag = data.PCHasy
                    End With
                    'Dim pchLabel As New TextBlock With {
                    '    .Text = $"Потери поездо-часов: {data.PCHasy}",
                    '    .Foreground = Brushes.Red,
                    '    .FontSize = 14,
                    '    .FontWeight = FontWeights.Bold,
                    '    .Cursor = Cursors.Hand,
                    '    .Tag = data.PCHasy,
                    '    .Margin = New Thickness(5, 2, 0, 2)
                    '}
                    ' ← ← ← ВОТ ОБРАБОТЧИК НАЖАТИЯ
                    AddHandler pchLabel.PreviewMouseLeftButtonUp, Sub(s, e)
                                                                      Dim Cha As Single = ParsePCHasyToDecimal(data.PCHasy)
                                                                      SetPointedOtkazProperty(data.ViolId, Function(p) p.PCh, Cha)
                                                                      Clipboard.SetText(Cha)
                                                                      Dim F As String
                                                                      If Not IsNothing(PointedOtkaz) AndAlso PointedOtkaz.Id = txtViolId.Text Then
                                                                          F = $"** Время отказа {PointedOtkaz.Id} ({Cha} ч.) скопировано **"
                                                                      Else
                                                                          F = $"** Время отказа {Cha} ч. скопировано **"
                                                                      End If

                                                                      MW.InfoBLOK.AddItem(F)
                                                                      MW.InfoBLOK.ScrollToEnd()
                                                                  End Sub
                    MestoSP.Children.Add(pchLabel)
                End If


                If Not String.IsNullOrWhiteSpace(data.Is_5_15_OTS) Then
                    Dim Lab515 As New TextBlock With {
                       .Text = data.Is_5_15_OTS,
                       .TextWrapping = TextWrapping.Wrap,
                       .FontSize = 13,
                       .FontWeight = FontWeights.Bold,
                       .Foreground = Brushes.Red
                    }

                    MestoSP.Children.Add(Lab515)
                End If


                ' ================= ОБОРУДОВАНИЕ =================
                If Not String.IsNullOrWhiteSpace(data.Oborud) Then

                    'Dim equipRtb = CreateLabel(data.Oborud, 14, FontWeights.SemiBold, , Brushes.Green, "Оборудование: ")

                    '' Включаем обработку кликов, даже если клик попал на текст
                    'equipRtb.IsHitTestVisible = True
                    'equipRtb.Cursor = If(data.IsKasantHierarchy, Cursors.Hand, Cursors.Arrow)

                    If data.IsKasantHierarchy Then
                        If PointedOtkaz IsNot Nothing AndAlso data.ViolId = PointedOtkaz.Id Then
                            ApplyKasantLevels(PointedOtkaz, data.Oborud)
                            SetStatus("✓ КАСАНТ-оборудование применено", Brushes.DarkBlue)
                        End If
                        '' Обработчик только если парсинг прошёл через <b>-ветку
                        'AddHandler equipRtb.MouseLeftButtonDown, Sub(s, e)
                        '                                             e.Handled = True
                        '                                             If PointedOtkaz IsNot Nothing AndAlso data.ViolId = PointedOtkaz.Id Then
                        '                                                 ApplyKasantLevels(PointedOtkaz, data.Oborud)
                        '                                             End If
                        '                                         End Sub
                        'equipRtb.ToolTip = "Кликните, чтобы применить уровни КАСАНТ"
                    End If

                    ''' Опционально: визуальный отклик при наведении (учитывая ваши предпочтения #11, #17)
                    ''If data.IsKasantHierarchy Then
                    ''    AddHandler equipRtb.MouseEnter, Sub(s, e) equipRtb.Background = New SolidColorBrush(Colors.LightYellow)
                    ''    AddHandler equipRtb.MouseLeave, Sub(s, e) equipRtb.Background = Brushes.Transparent
                    ''End If

                    'MestoSP.Children.Add(equipRtb)
                    MestoSP.Children.Add(CreateLabel(data.Oborud, 14, FontWeights.SemiBold,, Brushes.Green, "Оборудование: "))
                End If


                ' ================= ПЕРЕДАН НА ДР ДОРОГУ =================
                If Not String.IsNullOrWhiteSpace(data.PeredanOnOtherDor) Then
                    MestoSP.Children.Add(CreateLabel(data.PeredanOnOtherDor, 14, FontWeights.Bold,, Brushes.Red))
                End If


                '================= Сторонняя организация =================
                If Not String.IsNullOrWhiteSpace(data.ThirdPartyOrg) Then
                    'MestoSP.Children.Add(CreateLabel("Виновная организация:", 14, FontWeights.Bold, Brushes.Red))
                    MestoSP.Children.Add(CreateLabel(data.ThirdPartyOrg, 14, FontWeights.Normal,, Brushes.Red, "Виновная организация: "))
                End If

                ' ================= ХАРАКТЕР ОТКАЗА =================
                If Not String.IsNullOrWhiteSpace(data.CharacterText) Then
                    'MestoSP.Children.Add(CreateLabel("Общее описание отказа:", 14, FontWeights.SemiBold, Nothing))
                    MestoSP.Children.Add(CreateLabel(data.CharacterText, 14, FontWeights.Normal,, , "Общее описание отказа: "))
                End If


                ' ================= ДОКУМЕНТЫ =================
                If data.AttachedFiles.Count > 0 Then

                    Dim counter As Integer = 1

                    For Each filee In data.AttachedFiles
                        ' Создаем экземпляр UserControl
                        Dim card As New DocumentCard With {
                            .FileData = filee,            ' Данные файла
                            .Counter = counter,           ' Номер в списке
                            .ViolId = txtViolId.Text,     ' ID отказа для имени файла
                            .StatusCallback = AddressOf SetStatus, ' Ссылка на ваш метод статуса
                            .Margin = New Thickness(0)
        }

                        DocsSP.Children.Add(card)
                        counter += 1
                    Next


                    DocScroll.ScrollToEnd()
                End If

                ' ================= ЗАДЕРЖАННЫЕ ПОЕЗДА =================
                If data.DelayedTrains.Count > 0 Then
                    For Each train In data.DelayedTrains
                        ' Заголовок поезда
                        Dim trainHeader As New TextBlock()
                        trainHeader.Text = $"Поезд № {train.TrainNumber} ({train.TrainType})"
                        trainHeader.FontSize = 14
                        trainHeader.FontWeight = FontWeights.Bold
                        trainHeader.Margin = New Thickness(0, 15, 0, 5)
                        If train.IsFirst Then
                            trainHeader.Foreground = Brushes.Red
                            trainHeader.Text += " (*)"
                        End If
                        pnlTrainInfo.Children.Add(trainHeader)

                        ' Задержка
                        Dim delayText As New TextBlock()
                        delayText.Text = $"Задержка: {train.DelayMinutes} мин."
                        delayText.FontSize = 14
                        delayText.Margin = New Thickness(0, 0, 0, 10)
                        pnlTrainInfo.Children.Add(delayText)

                        ' ← ← ← ДОБАВИТЬ: Маршрут и время (если есть)
                        If Not String.IsNullOrWhiteSpace(train.RouteInfo) Then
                            Dim routeLabel As New TextBlock With {
                                .Text = $"  🛤 {train.RouteInfo}",
                                .FontSize = 13,
                                .Foreground = Brushes.DarkBlue,
                                .Margin = New Thickness(10, 0, 0, 2)
    }
                            pnlTrainInfo.Children.Add(routeLabel)
                        End If


                        ' Локомотивы
                        If train.Locomotives.Count > 0 Then
                            Dim locoHeader As New TextBlock()
                            locoHeader.Text = "Локомотивы:"
                            locoHeader.FontSize = 14
                            locoHeader.FontWeight = FontWeights.SemiBold
                            locoHeader.Margin = New Thickness(0, 10, 0, 5)
                            pnlTrainInfo.Children.Add(locoHeader)

                            For Each loco In train.Locomotives
                                'Dim border As New Border()
                                'border.BorderBrush = Brushes.DarkBlue
                                'border.BorderThickness = New Thickness(0.5)
                                'border.Padding = New Thickness(10)
                                'border.Margin = New Thickness(50, 0, 0, 5)
                                'border.CornerRadius = New CornerRadius(5)

                                'Dim stack As New StackPanel()

                                '' Серия и номер
                                'If Not String.IsNullOrWhiteSpace(loco.Series) OrElse Not String.IsNullOrWhiteSpace(loco.Number) Then
                                'Stack.Children.Add(CreateLabel($"{loco.Series} №{loco.Number}  ({loco.RealNumber})", 12, FontWeights.SemiBold,, Brushes.DarkRed))
                                'End If
                                'If Not String.IsNullOrWhiteSpace(loco.Road) OrElse Not String.IsNullOrWhiteSpace(loco.Depot) Then

                                '    Dim LocoText As String = "Приписка локомотива: "
                                '    ' Депо локомотива
                                '    If Not String.IsNullOrWhiteSpace(loco.Road) Then LocoText += loco.Road.Trim()
                                '    ' Дорога локомотива
                                '    If Not String.IsNullOrWhiteSpace(loco.Depot) Then
                                '        If Not String.IsNullOrWhiteSpace(loco.CrewRoad) Then LocoText += ", "
                                '        LocoText += loco.Depot.Trim()
                                '    End If
                                '    stack.Children.Add(CreateLabel(LocoText, 12, FontWeights.Normal, Nothing))
                                'End If

                                '' Машинист
                                'If Not String.IsNullOrWhiteSpace(loco.Driver) Then
                                '    stack.Children.Add(CreateLabel($"Машинист: {NormFam(FixCyrillicLatinity(loco.Driver))}", 12, FontWeights.SemiBold,, Brushes.DarkRed))
                                'End If

                                '' Бригада (дорога и депо приписки)
                                'If Not String.IsNullOrWhiteSpace(loco.CrewRoad) OrElse Not String.IsNullOrWhiteSpace(loco.CrewDepot) Then
                                '    Dim crewText As String = "Приписка бригады: "
                                '    If Not String.IsNullOrWhiteSpace(loco.CrewRoad) Then crewText += loco.CrewRoad.Trim()
                                '    If Not String.IsNullOrWhiteSpace(loco.CrewDepot) Then
                                '        If Not String.IsNullOrWhiteSpace(loco.CrewRoad) Then crewText += ", "
                                '        crewText += loco.CrewDepot.Trim()
                                '    End If
                                '    stack.Children.Add(CreateLabel(crewText, 12, FontWeights.Normal, Nothing))
                                'End If


                                'border.Child = stack

                                '' ========== ВЫЖИМКА В БУФЕР ОБМЕНА (ОТДЕЛЬНО) ==========
                                '' 1. Серия и номер
                                'Dim sLoco = $"{loco.Series} №{loco.Number}".Trim()
                                'If Not String.IsNullOrWhiteSpace(loco.RealNumber) Then
                                '    If loco.Number.Contains(loco.RealNumber) Then
                                '        sLoco = $"{loco.Series} №{loco.RealNumber}".Trim()
                                '    Else
                                '        sLoco = $"_ №{loco.RealNumber}".Trim()
                                '    End If

                                'End If

                                '' 2. Приписка локомотива
                                'Dim sLocoAttach = ""
                                '' If Not String.IsNullOrWhiteSpace(loco.Road) Then sLocoAttach = $"({loco.Road.Trim()})"
                                'If Not String.IsNullOrWhiteSpace(loco.Depot) AndAlso Not String.IsNullOrWhiteSpace(loco.Road) Then
                                '    sLocoAttach = $"{loco.Road.Trim()}, {loco.Depot.Trim()}"
                                'End If

                                '' 3. Машинист
                                'Dim sDriver = ""
                                'If Not String.IsNullOrWhiteSpace(loco.Driver) Then
                                '    sDriver = NormFam(FixCyrillicLatinity(loco.Driver))
                                'End If

                                '' 4. Приписка машиниста (бригады)
                                'Dim sCrewAttach = ""
                                ''If Not String.IsNullOrWhiteSpace(loco.CrewRoad) Then sCrewAttach = loco.CrewRoad.Trim()
                                'If Not String.IsNullOrWhiteSpace(loco.CrewDepot) AndAlso Not String.IsNullOrWhiteSpace(loco.CrewRoad) Then
                                '    ' If Not String.IsNullOrEmpty(sCrewAttach) Then sCrewAttach &= ", "'
                                '    sCrewAttach = $"{loco.CrewRoad.Trim()}, {loco.CrewDepot.Trim()}"
                                'End If

                                '' Собираем по шаблону и чистим артефакты
                                'Dim copyText = $"{sLoco} ({sLocoAttach}), машинист {sDriver} ({sCrewAttach})"
                                'copyText = copyText.Replace("  ", " ").Trim()

                                '' 5. Вешаем обработчик
                                'border.Cursor = System.Windows.Input.Cursors.Hand
                                ''border.ToolTip = "ЛКМ: скопировать данные в буфер"
                                'AddHandler border.MouseLeftButtonUp, Sub(s, e)
                                '                                         System.Windows.Clipboard.SetText(copyText)
                                '                                         e.Handled = True
                                '                                     End Sub
                                '' =========================================================


                                'border.Background = New SolidColorBrush(Color.FromArgb(0, 0, 0, 0))
                                'pnlTrainInfo.Children.Add(border)
                                Dim locoCard As New LocoInfoCard(loco)
                                pnlTrainInfo.Children.Add(locoCard)
                            Next
                        Else
                            pnlTrainInfo.Children.Add(CreateLabel("  Нет данных о локомотивах", 12, FontWeights.Normal,, Brushes.Gray))
                        End If
                    Next
                Else
                    pnlTrainInfo.Children.Add(CreateLabel("Задержанные поезда не найдены", 12, FontWeights.Normal,, Brushes.Red))
                End If

                ' ================= ИСТОРИЯ ОТКАЗА =================

                If data.History.Count > 0 Then


                    ' 1. Создаем хранилище для уникальных записей (учитываем дату, тип и текст)
                    Dim seenEntries As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

                    For Each h In data.History
                        ' 3. Создание и добавление карточки (вся визуализация — в отдельном классе)
                        Dim card As New HistoryCard
                        ' 1. Быстрая фильтрация (вынесено в фабрику)
                        If ShouldSkipEntry(h) Then Continue For
                        ' 2. Дедупликация
                        Dim key = GetUniqueKey(h)
                        If Not seenEntries.Add(key) Then Continue For
                        card.SetData(h) ' Вёрстка уже в XAML, тут только данные
                        HistSP.Children.Add(card)
                    Next
                    HistScroller.ScrollToEnd()
                End If
                ' =================


                'SetStatus("Готово!")
                lblStatus.Foreground = Brushes.Green

            Catch ex As Exception
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка подключения", MessageBoxButton.OK, MessageBoxImage.Error)

                SetStatus($"Ошибка загрузки: {ex.Message}")
                lblStatus.Foreground = Brushes.Red

                ' Добавляем отладочную информацию в интерфейс
                HistSP.Children.Add(CreateLabel($"ДЕТАЛИ ОШИБКИ:", 14, FontWeights.Bold,, Brushes.Red))
                HistSP.Children.Add(CreateLabel(ex.Message, 12, FontWeights.Normal,, Brushes.Red))
                HistSP.Children.Add(CreateLabel(ex.StackTrace, 10, FontWeights.Normal,, Brushes.Gray))
            Finally
                btnLoad.IsEnabled = True
            End Try
        End Sub

        ''' <summary>
        ''' Применяет иерархию оборудования к глобальному PointedOtkaz
        ''' </summary>
        Private Sub ApplyKasantLevels(otkaz As Otkaz, oborudString As String)
            Dim parts = oborudString.Split(New String() {" → "}, StringSplitOptions.None)

            If parts.Length >= 1 AndAlso Not String.IsNullOrWhiteSpace(parts(0)) Then
                otkaz.OTSLev1 = parts(0).Trim()
            End If
            If parts.Length >= 2 AndAlso Not String.IsNullOrWhiteSpace(parts(1)) Then
                otkaz.OTSLev2 = parts(1).Trim()
            End If
            If parts.Length >= 3 AndAlso Not String.IsNullOrWhiteSpace(parts(2)) Then
                otkaz.OTSLev3 = parts(2).Trim()
            End If

            ' UI обновится автоматически благодаря OnPropertyChanged в свойствах
            ' Можно добавить подтверждение, если нужно:
            ' MW.InfoBLOK($"✓ Применено: {String.Join(" → ", parts.Take(3))}", "green") ' с учётом вашего предпочтения #44
        End Sub



        Private Function CreateLabel(text As String, fontSize As Double, fontWeight As FontWeight, Optional Ftyle As FontStyle = Nothing, Optional color As Brush = Nothing, Optional header As String = "", Optional HeaderBrush As Brush = Nothing) As RichTextBox
            ' 1. Создаем контейнер документа с нулевыми отступами
            Dim docContainer As New FlowDocument With {
                            .PagePadding = New Thickness(0),
                            .LineHeight = 1 ' Можно чуть увеличить (например 18), если строки слишком слиплись
                                            }
            '
            Dim txt As New RichTextBox With {
                .Document = docContainer,
                .IsReadOnly = True,
                .IsDocumentEnabled = True,
                .IsReadOnlyCaretVisible = False,
                .Background = Brushes.Transparent,
                .Foreground = IIf(color IsNot Nothing, color, Brushes.Black),
                .FontStyle = IIf(Not IsNothing(Ftyle), Ftyle, FontStyles.Normal),
                .BorderThickness = New Thickness(0),
                .Margin = New Thickness(1),
                .Padding = New Thickness(0),
                .FontSize = fontSize,
                .FontWeight = fontWeight,
                .HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, ' Это заменяет TextWrapping!
                .VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
}
            ' Чтобы текст переносился, нужно явно задать отступ у параграфа, если нужно
            Dim p As New Paragraph() With {.Margin = New Thickness(0)}

            If Not String.IsNullOrWhiteSpace(header) Then
                p.Inlines.Add(New Run($"{header.Trim} ") With {.FontWeight = FontWeights.Bold, .Foreground = IIf(HeaderBrush IsNot Nothing, HeaderBrush, Brushes.Black)})
            End If
            If Not String.IsNullOrWhiteSpace(text) Then
                p.Inlines.Add(New Run(text) With {.FontWeight = fontWeight})
            End If
            txt.Document.Blocks.Add(p)
            Return txt
        End Function



        'Private Function EncryptPassword(password As String) As String
        '    If String.IsNullOrWhiteSpace(password) Then Return ""
        '    Dim bytes = Encoding.UTF8.GetBytes(password)
        '    Dim reversed = bytes.Reverse().ToArray()
        '    Return Convert.ToBase64String(reversed)
        'End Function

        'Private Function DecryptPassword(encrypted As String) As String
        '    If String.IsNullOrWhiteSpace(encrypted) Then Return ""
        '    Try
        '        Dim bytes = Convert.FromBase64String(encrypted)
        '        Dim reversed = bytes.Reverse().ToArray()
        '        Return Encoding.UTF8.GetString(reversed)
        '    Catch
        '        Return ""
        '    End Try
        'End Function

        ' Конвертирует строку вида "1ч 56м" в десятичные часы (1.93)
        Private Function ParsePCHasyToDecimal(text As String) As Single
            If String.IsNullOrWhiteSpace(text) Then Return 0

            Dim hours As Double = 0
            Dim minutes As Double = 0

            ' Ищем часы
            Dim hoursMatch = Regex.Match(text, "(\d+)\s*ч")
            If hoursMatch.Success Then
                hours = CDbl(hoursMatch.Groups(1).Value)
            End If

            ' Ищем минуты
            Dim minutesMatch = Regex.Match(text, "(\d+)\s*м")
            If minutesMatch.Success Then
                minutes = CDbl(minutesMatch.Groups(1).Value)
            End If

            ' Конвертируем в десятичные часы
            Return Math.Round(hours + (minutes / 60), 2)
        End Function

        '''' <summary>
        '''' Заменяет визуально похожие латинские буквы на кириллические
        '''' </summary>
        'Public Function FixCyrillicLatinity(text As String) As String
        '    If String.IsNullOrWhiteSpace(text) Then Return text

        '    ' Карта замены: латинская → кириллическая
        '    Dim charMap As New Dictionary(Of Char, Char) From {
        '        {"A"c, "А"c}, {"B"c, "В"c}, {"C"c, "С"c}, {"E"c, "Е"c},
        '        {"H"c, "Н"c}, {"K"c, "К"c}, {"M"c, "М"c}, {"O"c, "О"c},
        '        {"P"c, "Р"c}, {"T"c, "Т"c}, {"X"c, "Х"c}, {"Y"c, "У"c},
        '        {"a"c, "а"c}, {"c"c, "с"c}, {"e"c, "е"c}, {"o"c, "о"c},
        '        {"p"c, "р"c}, {"x"c, "х"c}, {"y"c, "у"c}
        '    }

        '    Dim result As New System.Text.StringBuilder(text.Length)

        '    For Each ch As Char In text
        '        If charMap.ContainsKey(ch) Then
        '            result.Append(charMap(ch))  ' Заменяем
        '        Else
        '            result.Append(ch)            ' Оставляем как есть
        '        End If
        '    Next

        '    Return result.ToString()
        'End Function

        ''' <summary>
        ''' Универсальная процедура присвоения свойства через Expression (с IntelliSense!)
        ''' </summary>
        Private Sub SetPointedOtkazProperty(Of T)(currentViolId As String,
                                                   propertyExpr As Expression(Of Func(Of Otkaz, T)),
                                                   value As T)
            If PointedOtkaz Is Nothing Then
                SetStatus($"⚠ [SetProperty] PointedOtkaz = Nothing")
                Return
            End If

            If PointedOtkaz.Id <> currentViolId Then
                SetStatus($"⚠ [SetProperty] НЕСОВПАДЕНИЕ ViolId!")
                Return
            End If

            Try
                ' Извлекаем PropertyInfo из Expression
                Dim memberExpr = DirectCast(propertyExpr.Body, MemberExpression)
                Dim propInfo = DirectCast(memberExpr.Member, Reflection.PropertyInfo)

                propInfo.SetValue(PointedOtkaz, value)
                SetStatus($"✓ [SetProperty] {propInfo.Name} = {value}")
            Catch ex As Exception
                SetStatus($"❌ [SetProperty] Ошибка: {ex.Message}")
            End Try
        End Sub

        Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)

        End Sub

        Private Sub Window_Closed(sender As Object, e As EventArgs)
            SaveSettings()
        End Sub

        Private Sub btnSaveSettings_Click(sender As Object, e As RoutedEventArgs)
            SaveSettings()
        End Sub


        Private Sub SaveSettings()
            With My.Settings
                .ParcerTop = Me.Top
                .ParcerLeft = Me.Left
                .ParcerWidth = Me.Width
                .ParcerHeight = Me.Height
                ' Если свёрнуто — сохраняем Normal, чтобы не открывалось свёрнутым
                .ParcerWindowState = If(Me.WindowState = WindowState.Minimized,
                                        WindowState.Normal,
                                        Me.WindowState)

            End With
        End Sub

        'фильтры и ключи 
        Public Shared Function ShouldSkipEntry(h As HistoryRecord) As Boolean
            Dim actionLower = h.ActionType.ToLower()
            If actionLower.Contains("ввод данных") OrElse
               actionLower.Contains("доп.материала") OrElse
               actionLower.Contains("еасапр ржд") Then Return True
            Dim abType = Regex.Replace(h.ActionType, "\s+", "").ToLower()
            If abType.Contains("выявленосредствамидиагностики") Then Return True
            Return False
        End Function

        Public Shared Function GetUniqueKey(h As HistoryRecord) As String
            Return $"{h.DateTime}_{h.ActionType}_{h.ToDest}_{h.Comment}".Trim()
        End Function

        Private Async Sub btnDelegate_Click(sender As Object, e As RoutedEventArgs)
            ' === ПРОВЕРКА ВХОДНЫХ ДАННЫХ ===
            If Not Integer.TryParse(txtViolId.Text, Nothing) Then
                ShowMSG(Me, "Введите корректный номер отказа!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            Dim selectedDepot As DepotItem = TryCast(cmbDepots.SelectedItem, DepotItem)
            If selectedDepot Is Nothing Then
                ShowMSG(Me, "Выберите депо из списка!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            Dim violId As Integer = CInt(txtViolId.Text)
            Dim dorKod As Integer = 88
            Integer.TryParse(txtDorKod.Text, dorKod)

            ' ВНИМАНИЕ: Замени 172450 на реальное свойство твоего объекта отказа (ID случая / gr_id)
            ' В логах это параметр id_sl. Пока ставим заглушку для теста.
            Dim idSl As Integer = 172450

            Dim depotName As String = selectedDepot.Name
            Dim depotId As Integer = selectedDepot.Id

            ' === UI: СТАТУС ЗАГРУЗКИ ===
            Dim originalText As String = btnDelegate.Content.ToString()
            btnDelegate.Content = "⏳ Назначение..."
            Me.Cursor = Cursors.Wait
            btnDelegate.IsEnabled = False

            ' === ПРОВЕРКА/УСТАНОВЛЕНИЕ СЕССИИ ===
            If Not Await Fetcher.EnsureConnectedAsync() Then
                ' Если сессия не активна и восстановить не удалось - откатываем UI и выходим
                btnDelegate.Content = originalText
                btnDelegate.IsEnabled = True
                Me.Cursor = Cursors.Arrow
                Return
            End If



            Try
                ' Генерация технических параметров (как в браузере)
                Dim rndVal As String = New Random().NextDouble().ToString("F16")
                Dim timestamp As String = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
                Dim baseUrl As String = "http://kasant.gvc.oao.rzd:8888/kasant"

                ' В логах фигурирует tab=1293052423. Если у тебя он динамический, подставь свою переменную.
                Dim tabId As String = "1293052423"

                ' ==========================================================
                ' ШАГ 1: delegateBreakDown (Само назначение отказа)
                ' ==========================================================
                Dim urlDelegate As String = $"{baseUrl}/delegateBreakDown?rand={rndVal}&viol={violId}&id_sl={idSl}&pred_id={depotId}&dor_kod={dorKod}"
                Dim contentDelegate As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
                    {"", "undefined"},
                    {"rndval", timestamp}
                })
                Dim resp1 = Await Fetcher.HttpClient.PostAsync(urlDelegate, contentDelegate)
                If Not resp1.IsSuccessStatusCode Then Throw New Exception("Ошибка на этапе delegateBreakDown. Код: " & resp1.StatusCode)

                ' ==========================================================
                ' ШАГ 2: tabSaveStatus (Обновление статуса строки на 6)
                ' ==========================================================
                Dim urlStatus As String = $"{baseUrl}/tabSaveStatus"
                Dim bodyStatus1 As String = $"tab={tabId}&active_id={violId}&active_dor_kod={dorKod}&dublicates_count=0&our=true&status=6&operation=update_journal_table_commit()&_="
                Dim contentStatus1 As New StringContent(bodyStatus1, Encoding.UTF8, "application/x-www-form-urlencoded")
                Await Fetcher.HttpClient.PostAsync(urlStatus, contentStatus1)

                ' ==========================================================
                ' ШАГ 3: tabSaveStatus (Фиксация ответственного депо)
                ' ==========================================================
                ' ВАЖНО: Uri.EscapeDataString превратит "ТЧЭ-1" в "%D0%A2%D0%A7%D0%AD-1", как требует сервер!
                Dim encodedDepotName As String = Uri.EscapeDataString(depotName)
                Dim bodyStatus2 As String = $"responsible_pred_id={depotId}&responsible_pred={encodedDepotName}&operation=update_journal_table_commit()&_="
                Dim contentStatus2 As New StringContent(bodyStatus2, Encoding.UTF8, "application/x-www-form-urlencoded")
                Await Fetcher.HttpClient.PostAsync(urlStatus, contentStatus2)

                ' === УСПЕХ ===
                ShowMSG(Me, $"Отказ №{violId} успешно передан в {depotName}!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information)

                ' Опционально: можно вызвать btnLoad_Click(sender, e), чтобы обновить карточку и увидеть изменения

            Catch ex As Exception
                ShowMSG(Me, $"Ошибка при назначении: {ex.Message}", "Ошибка КАСАНТ", MessageBoxButton.OK, MessageBoxImage.Error)
            Finally
                ' === UI: ГАРАНТИРОВАННЫЙ ВОЗВРАТ В ИСХОДНОЕ СОСТОЯНИЕ ===
                btnDelegate.Content = originalText
                btnDelegate.IsEnabled = True
                Me.Cursor = Cursors.Arrow
            End Try
        End Sub
    End Class

End Namespace

