Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Data
Imports System.Globalization
Imports System.IO
Imports System.Reflection
Imports System.Text
Imports System.Threading.Tasks
Imports System.Windows.Controls.Primitives
Imports System.Windows.Forms
Imports System.Windows.Forms.VisualStyles.VisualStyleElement
Imports System.Windows.Threading
Imports KACAHT_Next2.Kas.CalendarControl
Imports Microsoft.Office.Interop.Excel
Imports Microsoft.Office.Interop.Word
Imports Microsoft.Win32
Imports Newtonsoft.Json.Linq
Imports OfficeOpenXml.Utils


Namespace Kas
    Partial Class MainWindow
        'Public CurrentSearchTerm As String = ""
        ' Объявить интерфейс
        Implements INotifyPropertyChanged
        'Private gameIsOpen As Boolean = False

        Private _currentSearchTerm As String = ""



        Public Property CurrentSearchTerm As String
            Get
                Return _currentSearchTerm
            End Get
            Set(value As String)
                _currentSearchTerm = value
                RaisePropertyChanged(NameOf(CurrentSearchTerm)) ' ← Вот так!
            End Set
        End Property

        Private _currentSearchTermForDebounce As String = ""

        Public Property CurrentSearchTermForDebounce As String
            Get
                Return _currentSearchTermForDebounce
            End Get
            Set(value As String)
                _currentSearchTermForDebounce = value
                ' Уведомляем WPF об изменении
                RaisePropertyChanged("CurrentSearchTermForDebounce")
            End Set
        End Property

        ' Для INotifyPropertyChanged
        Private Sub RaisePropertyChanged(propertyName As String)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
        End Sub


        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged


        Private SearchDebounceTimer As New DispatcherTimer()

        Private _isSummaryReportBuilt As Boolean = False


        ''' <summary>
        ''' Показывает в списке отказов только отказы из переданного списка.
        ''' Сбрасывает предыдущие фильтры, чтобы результат был чистым.
        ''' </summary>
        Public Sub ShowOtkazyList(list As IEnumerable(Of Otkaz))
            If list Is Nothing OrElse Not list.Any() Then Return

            ResignOTSList(list.ToList)

        End Sub


        Private Sub LogRecordsWithoutSearchTerm(searchTerm As String)
            If String.IsNullOrEmpty(searchTerm) Then Return

            ' Получаем текущий полный список (с учётом ярлыка и фильтров)
            Dim fullList = GetCurrentFilteredOtkazList()

            ' Находим записи, где НЕТ искомого слова
            Dim withoutTerm = fullList.Where(Function(o) Not o.ContainsPhrase(searchTerm)).ToList()

            ' Выводим в InfoBLOK
            If withoutTerm.Count > 0 Then
                InfoBLOK.ClearItems()
                InfoBLOK.AddItem($"--- Отказы БЕЗ '{searchTerm}' ({withoutTerm.Count} шт):")
                For Each o In withoutTerm.Take(10) ' ограничим 20-ю, чтобы не засорять
                    InfoBLOK.AddItem($"  № {o.Id}, начало - {o.Nach.ToString("g")}")
                Next
                If withoutTerm.Count > 10 Then
                    InfoBLOK.AddItem($"  ... и ещё {withoutTerm.Count - 10} записей")
                End If
            Else
                InfoBLOK.AddItem($"--- Все записи содержат '{searchTerm}'")
            End If

            InfoBLOK.ScrollToEnd()
        End Sub

        ' 1. Свойство-обёртка, которое отдаёт глобальный Fetcher из модуля
        Public ReadOnly Property MWFetcher As KasantFetcher
            Get
                Return Fetcher ' Или просто Fetcher, если Namespace импортирован
            End Get
        End Property


        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()
            'задается здесь - для маинвиндов конкретно цвета
            AppTBar.MainBorder.Background = CType(FindResource("HeaderBackBrush"), Brush)
            AppTBar.BorderLineColor = CType(FindResource("HeaderBorderBrush"), Brush)
            Me.DataContext = Me
            MW = Me

            'InitializePointedOTSHandler()
            SearchDebounceTimer.Interval = TimeSpan.FromMilliseconds(300)
            AddHandler SearchDebounceTimer.Tick, AddressOf OnSearchDebounceTick

            AddHandler MW.SerLokContent.SeriesSelected, Sub(sender, series)
                                                            SerLokPopup.IsOpen = False

                                                        End Sub
            AddHandler MW.SerLokContent.Cancelled, Sub(sender, e)
                                                       SerLokPopup.IsOpen = False
                                                   End Sub

            AddHandler MW.IstochContent.IstochSelected, Sub(sender, series)
                                                            IstochPopup.IsOpen = False

                                                        End Sub
            AddHandler MW.IstochContent.IstochCancelled, Sub(sender, series)
                                                             IstochPopup.IsOpen = False
                                                         End Sub

            AddHandler MW.PripMashPopup.Confirmed, AddressOf PripMashPopup_Confirmed
            AddHandler MW.PripMashPopup.Cancelled, AddressOf PripMashPopup_Cancelled

            Fetcher.KonMinut = 0

        End Sub



        Private Sub PripMashPopup_Confirmed(sender As Object, e As EventArgs)
            If PointedOtkaz IsNot Nothing AndAlso
       Not String.IsNullOrEmpty(PripMashPopup.SelectedSurname) AndAlso
       Not String.IsNullOrEmpty(PripMashPopup.SelectedPripis) Then

                PointedOtkaz.Mash = PripMashPopup.SelectedSurname
                PointedOtkaz.PripMash = PripMashPopup.SelectedPripis
                PripisMash.IsOpen = False
                TRowsContainer.UpdateTotal()
                TRowsContainer.ScrollToPointedOTS()
            End If
        End Sub

        Private Sub PripMashPopup_Cancelled(sender As Object, e As EventArgs)
            PripisMash.IsOpen = False
        End Sub


        Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
            RestoreSizes()
            With My.Settings

                If String.IsNullOrEmpty(.DataFolderPath) Then
                    Dim f = GetOrCreateDataFolderPath()
                    f = Nothing
                End If
                If String.IsNullOrEmpty(.DocFolder) Then
                    Dim f = GetOrCreateDataFolderPath(ForDocs:=True)
                    f = Nothing
                End If
                If String.IsNullOrEmpty(.SetsFolder) Then
                    Dim f = GetOrCreateDataFolderPath(ForParams:=True)
                    f = Nothing
                End If
                If String.IsNullOrEmpty(.ReportFolderPath) Then
                    Dim f = GetOrCreateDataFolderPath(ForReports:=True)
                    f = Nothing
                End If

            End With

            ' 0. Сначала настройки:
            BarHeight = 6        ' толщина полоски
            BarColor = Color.FromRgb(0, 255, 0)
            FrameIntervalMs = 40 ' скорость (~12 FPS)
            TotalFrames = 48
            ' 1. Инициализируем аниматор при старте
            AnimIconInitialize(Me)
            ' 2. Запускаем анимацию (например, при начале обновления)
            Start()




            ResetPeriodSUB()
            InfoBLOK.AddItem("=== Начальная загрузка ===")
            PointedYarlyk = YarCon.AllOTSYAR
            InitFirst()
            InfoBLOK.AddItem("===================")
            InfoBLOK.ScrollToEnd()
            ChangeFilters()
            ' Регистрируем кодировки (включая 1251)
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance)
            AddHandler TRowsContainer.THed.FiltersChanged, AddressOf OnFiltersChanged
            AddHandler TRowsContainer.FiltersChanged, AddressOf OnFiltersChanged

            AddHandler TRowsContainer.OpisSearchRequested, AddressOf HandleOpisSearch

            AddHandler OTSControlModule.PointedOtkazChanged, AddressOf OnPointedOtkazChanged

            ' Передаем общий клиент в индикатор
            ConnIndicator.Initialize()
            CentralConnectionIndicator.Initialize()

        End Sub

        Private Sub MainWindow_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
            Module1.AnimIconDispose()
        End Sub

        Sub RestoreSizes()
            ' Восстановление состояния окна
            Dim savedState As WindowState = WindowState.Normal
            If [Enum].TryParse(Of WindowState)(My.Settings.WindowState, savedState) Then
                Me.WindowState = savedState
            End If

            ' Размеры — только если окно не развёрнуто
            If Me.WindowState = WindowState.Normal Then
                If My.Settings.WindowWidth > 100 AndAlso My.Settings.WindowHeight > 100 Then
                    Me.Width = My.Settings.WindowWidth
                    Me.Height = My.Settings.WindowHeight
                End If

                If My.Settings.WindowLeft >= 0 AndAlso My.Settings.WindowTop >= 0 Then
                    Me.Left = My.Settings.WindowLeft
                    Me.Top = My.Settings.WindowTop
                End If
            End If

            ' Ширина левой панели
            Dim width = My.Settings.LeftPanelWidth
            If width >= 180 AndAlso width <= 800 Then
                LeftColumn.Width = New GridLength(width)
            Else
                LeftColumn.Width = New GridLength(300)
            End If

        End Sub


        Private Sub HandleOpisSearch(sender As Object, e As OpisSearchEventArgs)

            ' Сохраняем последний запрос
            CurrentSearchTermForDebounce = e.SearchTerm

            ' Сбрасываем таймер
            SearchDebounceTimer.Stop()
            SearchDebounceTimer.Start()

        End Sub


        Private Sub OnSearchDebounceTick(sender As Object, e As EventArgs)
            SearchDebounceTimer.Stop()
            ' Устанавливаем свойство (это вызовет PropertyChanged)
            CurrentSearchTerm = CurrentSearchTermForDebounce.Trim()
            ' Выполняем поиск с последним сохранённым значением
            PerformSearch(CurrentSearchTermForDebounce)
        End Sub


        Private Sub PerformSearch(searchTerm As String)
            If String.IsNullOrEmpty(searchTerm) Then
                ' Возвращаем список без поиска
                ChangeFilters()
            Else
                ' Получаем актуальный список с учётом ВСЕХ фильтров
                Dim baseList = GetCurrentFilteredOtkazList()

                ' Применяем поиск
                Dim filtered = baseList.Where(Function(o) o.ContainsPhrase(searchTerm)).ToList()
                TRowsContainer.ItemsSource = filtered

                '' 🔥 Выводим отчёты без искомого слова (ВКЛЮЧАЕМ ПРИ НЕОБХОДИМОСТИ)
                LogRecordsWithoutSearchTerm(searchTerm)

            End If
        End Sub


        Private Function GetCurrentFilteredOtkazList() As List(Of Otkaz)

            ' 1. Ярлык
            Dim baseList As List(Of Otkaz)
            If PointedYarlyk?.Zapros IsNot Nothing Then
                baseList = OTSList.Where(PointedYarlyk.Zapros).ToList()
            Else
                baseList = OTSList
            End If

            ' 2. Level0-фильтры ← ДОБАВЬ ЭТО!
            Dim level0Pred = SafePredicate(TRowsContainer.BuildLevel0CombinedPredicate())
            baseList = baseList.Where(level0Pred).ToList()

            ' 3. THed-фильтры
            Dim mainPred = SafePredicate(TRowsContainer.THed.BuildCombinedPredicate())
            baseList = baseList.Where(mainPred).ToList()

            ' 4. Поиск ← УБЕРИ ЭТОТ БЛОК!
            ' Потому что поиск уже применяется в PerformSearch

            Return baseList.OrderBy(Function(o) o.Nach).ToList()

        End Function


        Private Sub OnPointedOtkazChanged(sender As Object, e As EventArgs)
            ' Обновляем DataContext карточки
            OTSInfoCard1.CurrentOtkaz = OTSControlModule.PointedOtkaz
        End Sub


        Private Sub OnFiltersChanged(sender As Object, e As EventArgs)
            ChangeFilters()

        End Sub

        ' В классе, где находится ChangeFilters
        Public _isLokFilter As Boolean = False
        Public _filterNumbers As List(Of String) = Nothing











#Region "МышеКнопки"

        'копирование номеров ОТС
        Private Sub Button_Click_5(sender As Object, e As RoutedEventArgs)

        End Sub

        'Private Sub LoadData_Click(sender As Object, e As RoutedEventArgs)


        '    'PointedOtkaz = Nothing
        '    'InitFirst()
        '    'YarCon.UpdateYarlykInfo()
        'End Sub




        '============================================================
        Private Sub SelectPeriod_Click(sender As Object, e As RoutedEventArgs)
            ' Открываем окно
            DContrPop.PlaceTarget = sender
            DContrPop.IsOpen = True

        End Sub


        Private Sub MaiWinn_PreviewMouseDown(sender As Object, e As MouseButtonEventArgs)
            If e.ChangedButton = MouseButton.XButton1 Then
                ' Кнопка "Назад" на мыши
                If OTSCard IsNot Nothing AndAlso OTSCard.Visibility = Visibility.Visible Then
                    'PointedOtkaz = Nothing
                    MainTabControl.SelectedItem = MainPage
                    TRowsContainer.ScrollToPointedOTS()
                    e.Handled = True
                    Return
                End If
            End If
        End Sub

        Private Sub MaiWinn_Closing(sender As Object, e As CancelEventArgs)
            ' === Окно ===
            If Me.WindowState = WindowState.Normal Then
                My.Settings.WindowLeft = Me.Left
                My.Settings.WindowTop = Me.Top
                My.Settings.WindowWidth = Me.Width
                My.Settings.WindowHeight = Me.Height
            End If
            My.Settings.WindowState = Me.WindowState.ToString()

            ' === Ширина левой панели ===
            If LeftColumn IsNot Nothing AndAlso LeftColumn.Width.IsAbsolute Then
                My.Settings.LeftPanelWidth = LeftColumn.Width.Value
            End If

            My.Settings.Save()
            CleanDocsFolder()
        End Sub

        Sub CleanDocsFolder()
            Try

                ClearFiles(My.Settings.DocFolder)
                ClearFiles(My.Settings.ReportFolderPath)

                ' 🔹 2. Очистка папки логов КАСАНТ на рабочем столе 
                Dim kasDebugPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "KasAntDebug")
                ClearFiles(kasDebugPath)

            Catch : End Try
        End Sub

        Sub ClearFiles(Pth As String)

            If String.IsNullOrWhiteSpace(Pth) Then Return
            If Not System.IO.Directory.Exists(Pth) Then Return

            Try
                ' 1. Удаляем все файлы в текущей папке
                For Each f As String In System.IO.Directory.GetFiles(Pth)
                    Try
                        System.IO.File.Delete(f)
                    Catch : End Try  ' Игнорируем занятые файлы
                Next

                ' 2. Рекурсивно удаляем все вложенные папки
                For Each subDir As String In System.IO.Directory.GetDirectories(Pth)
                    Try
                        System.IO.Directory.Delete(subDir, recursive:=True)
                    Catch : End Try  ' Игнорируем, если не удалось
                Next

            Catch : End Try

        End Sub


        Public Sub PeredachaPopContent_Confirmed(pred As String, addToHistory As Boolean, eventDate As Date?, comment As String, oldPred As String)
            If PointedOtkaz Is Nothing Then Return

            PointedOtkaz.KtoZakryl = pred

            If addToHistory AndAlso Not String.IsNullOrEmpty(oldPred) AndAlso oldPred <> pred Then
                Dim desc = $"передан от {oldPred} на {pred}"
                If Not String.IsNullOrEmpty(comment) Then desc &= $" ({comment})"
                PointedOtkaz.AddHistoryEntry(eventDate, desc)
            End If

            If pred.ToLower.Contains("трп") Then
                PointedOtkaz.ZaKemCode = "тр"
            ElseIf pred.ToLower.Contains("тч") Then
                With PointedOtkaz
                    If .SerLok = "" OrElse .Mash = "" OrElse .Opis = "" Then
                        .ZaKemCode = "!"
                    Else
                        .ZaKemCode = ""
                    End If
                End With

            End If

            Dim Itm As String = $"за {pred}?"
            If PointedOtkaz.UpdateNotes.Contains(Itm) Then
                PointedOtkaz.RemoveItemUpdateNote(Itm)
            End If
            PeredachaPopup.IsOpen = False
        End Sub

        Public Sub PeredachaPopContent_Canceled()
            PeredachaPopup.IsOpen = False
        End Sub



        Private Sub TabSummary_IsVisibleChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
            'If Not TabSummary.IsVisible Then Exit Sub
            'If My.Settings.OldYJSON = "" Then Exit Sub
            'Dim oldY As List(Of Otkaz) = StorageModule.LoadFromJson(My.Settings.OldYJSON)

            '' С фильтрацией по периоду
            'Dim table As New S24Table1(
            '    OTSList,
            '    oldY,
            '    Fetcher.NachDat,
            '    Fetcher.KonDat)


            'TabSummary.Content = table

        End Sub



        Private Sub BtnZmeyka_Click(sender As Object, e As RoutedEventArgs)
            ''        🔹 Дочерний проект (который вставляем)
            ''- Свойства → Тип вывода (Output type) = «Библиотека классов» (для WPF — «Библиотека классов WPF»). Объект запуска при этом сам станет «(Нет)».
            ''- Удалить Application.xaml(и его .vb) — это точка входа приложения с StartupUri, в библиотеке ей места нет.
            ''- Удалить App.config и иконку (favicon.ico) — в DLL они не нужны (не критично, но мусор).
            ''- Класс окна, которое вызываем снаружи — Public
            ''- Никаких Application.Current.Shutdown() внутри библиотеки — только Me.Close(). Иначе модуль будет убивать всё приложение-хозяин.

            ''       🔹 Родительский проект (куда вставляем)
            ''- ПКМ по Зависимости (Ссылки) → Добавить ссылку на проект... → галка на дочернем проекте → ОК.
            ''- Вызывать окно полным именем (без Imports, чтобы не было конфликтов имён)
            ''               Dim game As New Zmeyka.MainWindow()
            ''               game.ShowDialog()   ' или .Show()
            ''- Сборка → Перестроить решение (Rebuild Solution).
            ''- Целевые платформы совместимые: оба проекта.NET Framework 4.8 (или библиотека — .NET Standard). В 4.8 нельзя воткнуть библиотеку на .NET 8.




            '' Защита: не открываем второе окно, пока открыто первое
            'If gameIsOpen Then Return
            'gameIsOpen = True
            'BtnZmeyka.IsEnabled = False

            'Dim game As New Zmeyka.MainWindow()
            'AddHandler game.Closed, AddressOf GameWindow_Closed
            'game.Show()
        End Sub

        ' Срабатывает, когда окно игры закрылось (крестиком или кнопкой "Выход")
        Private Sub GameWindow_Closed(sender As Object, e As EventArgs)
            'gameIsOpen = False
            'BtnZmeyka.IsEnabled = True
        End Sub



        Private Sub BtnSmoke_Click(sender As Object, e As RoutedEventArgs)
            '    Dim btn As FrameworkElement = CType(expPoyasnilka, FrameworkElement)
            '    ' Dim mw As System.Windows.Window = Application.Current.MainWindow


            '    Dim winPos As System.Windows.Point = mw.PointToScreen(New System.Windows.Point(0, 0))
            '    Dim expPos As System.Windows.Point = btn.PointToScreen(New System.Windows.Point(0, 0))
            '    Dim splPos As System.Windows.Point = MainWinSplitter.PointToScreen(New System.Windows.Point(0, 0))
            '    Dim connPos As System.Windows.Point = ConnIndicator.PointToScreen(New System.Windows.Point(0, 0))


            '    ' Координаты относительно окна
            '    Dim expLeft As Double = expPos.X - winPos.X
            '    Dim expTop As Double = expPos.Y - winPos.Y
            '    Dim splLeft As Double = splPos.X - winPos.X
            '    Dim connTop As Double = connPos.Y - winPos.Y

            '    ' Границы канваса: верх и низ задаём явно
            '    Dim topY As Double = connTop - 20        ' верх: на 20px выше ConnIndicator
            '    Dim bottomY As Double = expTop + 12      ' низ: нахлёст 12px на кнопку (0 = встык)
            '    Dim popWidth As Double = Math.Max(100, splLeft - expLeft)
            '    Dim popHeight As Double = Math.Max(100, bottomY - topY)

            '    Dim wa = SystemParameters.WorkArea
            '    If popWidth > wa.Width - 20 Then popWidth = wa.Width - 20
            '    If popHeight > wa.Height - 20 Then popHeight = wa.Height - 20

            '    Dim Kanv As New SmokeCanvas With {.Width = popWidth, .Height = popHeight}
            '    SmokeParamsStore.LoadParams(Kanv)



            '    Dim Pop As New Popup With {
            '.AllowsTransparency = True, .StaysOpen = False,
            '.PlacementTarget = mw,
            '.Placement = Primitives.PlacementMode.Relative,
            '.HorizontalOffset = expLeft,
            '.VerticalOffset = topY,
            '.Child = Kanv, .IsOpen = True}

            '    Kanv.StartSmoke()
            '    Dispatcher.BeginInvoke(Sub() Pop.IsOpen = True, Threading.DispatcherPriority.Loaded)
        End Sub




#End Region

    End Class

End Namespace

