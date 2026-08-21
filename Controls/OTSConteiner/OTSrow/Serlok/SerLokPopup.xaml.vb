Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Controls.Primitives
Imports System.Windows.Input
Imports System.Windows.Media

Namespace Kas

    Partial Public Class SerLokPopup
        Inherits UserControl

        Public Event SeriesSelected As EventHandler(Of String)
        Public Event Cancelled As EventHandler

        Private _selectedSeries As String = Nothing
        Private _selectedPripisLok As String = Nothing   ' ← ЭТА СТРОЧКА

        Sub New()
            InitializeComponent()

        End Sub

        Private Sub InitializeCustomSeriesInput()
            AddHandler CustomSeriesInputControl.OkClicked, AddressOf CustomSeriesInput_OkClicked
            AddHandler CustomSeriesInputControl.CancelClicked, AddressOf CustomSeriesInput_CancelClicked
            AddHandler CustomPripisInputControl.OkClicked, AddressOf CustomPripisInput_OkClicked
            AddHandler CustomPripisInputControl.CancelClicked, AddressOf CustomPripisInput_CancelClicked
        End Sub

        Private Sub UserControl_Loaded(sender As Object, e As RoutedEventArgs)
            Me.DataContext = PointedOtkaz
            SeriesTabControl.Items.Clear()
            AssignmentTabControl.Items.Clear()   ' ← ДОБАВИЛИ
            NumLokTextBox.Text = ""
            LoadSeriesByCategories()
            LoadAssignmentsByCategories()        ' ← ДОБАВИЛИ
            InitializeCustomSeriesInput()
        End Sub





        ' Обработчики:


        Private Sub ShowCustomPripisInput()
            MainGrid.Visibility = Visibility.Collapsed
            CustomPripisInputControl.Visibility = Visibility.Visible
            CustomPripisInputControl.Reset()
            CustomPripisInputControl.FocusOnPripisInput()
        End Sub

        Private Sub HideCustomPripisInput()
            MainGrid.Visibility = Visibility.Visible
            CustomPripisInputControl.Visibility = Visibility.Collapsed
        End Sub

        Private Sub CustomPripisInput_OkClicked(sender As Object, e As EventArgs)

            Dim pripis = CustomPripisInputControl.SelectedPripis
            Dim road = CustomPripisInputControl.SelectedRoad

            If Not String.IsNullOrWhiteSpace(pripis) AndAlso Not String.IsNullOrEmpty(road) Then
                ProcessNewPripis(pripis, road)
            End If
            HideCustomPripisInput()
        End Sub

        Private Sub CustomPripisInput_CancelClicked(sender As Object, e As EventArgs)
            HideCustomPripisInput()
        End Sub

        Private Sub ProcessNewPripis(pripis As String, road As String)
            Dim s = pripis.Trim()

            If PripLokModule.AllPripis.Contains(s) Then
                _selectedPripisLok = s
                DeleteAssignmentButton.IsEnabled = True
                SelectAndHighlightPripisButton(s)
                OkButton.IsEnabled = True
            Else
                PripLokModule.AddPripis(s)

                ' ← ОБНОВИТЬ СЛОВАРЬ
                If PripLokModule.RoadToPripis.ContainsKey(road) Then
                    Dim list = PripLokModule.RoadToPripis(road)
                    If Not list.Contains(s) Then
                        list.Add(s)
                    End If
                End If
                RefreshAssignmentTab(road)
                _selectedPripisLok = s
                DeleteAssignmentButton.IsEnabled = True
                SelectAndHighlightPripisButton(s)
                OkButton.IsEnabled = True
            End If
        End Sub

        Private Sub SelectAndHighlightPripisButton(pripis As String)
            For Each tabItem In AssignmentTabControl.Items.OfType(Of TabItem)()
                Dim wrapPanel = TryCast(TryCast(tabItem.Content, ScrollViewer)?.Content, WrapPanel)
                If wrapPanel IsNot Nothing Then
                    For Each btn As Button In wrapPanel.Children.OfType(Of Button)()
                        If btn.Tag?.ToString() = pripis Then
                            HighlightAssignmentButton(btn)
                            Return
                        End If
                    Next
                End If
            Next
        End Sub


        Private Sub LoadAssignmentsByCategories()
            AssignmentTabControl.Items.Clear()
            For Each road In PripLokModule.AllRoads
                CreateTabForAssignmentType(road, road)
            Next

            If AssignmentTabControl.Items.Count > 0 Then
                AssignmentTabControl.SelectedIndex = 0
            End If
        End Sub

        Private Sub CreateTabForAssignmentType(typeKey As String, typeName As String)
            Dim tabItem As New TabItem() With {.Header = typeName, .Tag = typeKey}

            Dim scrollViewer As New ScrollViewer() With {.VerticalScrollBarVisibility = ScrollBarVisibility.Auto, .HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled}

            Dim scrollBarStyle = TryCast(Me.FindResource("CustomScrollBarStyle"), Style)
            If scrollBarStyle IsNot Nothing Then
                scrollViewer.Resources.Add(GetType(ScrollBar), scrollBarStyle)
            End If

            Dim wrapPanel As New WrapPanel() With {
                .Orientation = Orientation.Horizontal,
                .Margin = New Thickness(2)
            }

            ' ← основное отличие: берём из модуля
            Dim pripisList = GetAllPripisForRoad(typeKey)

            For Each pripis In pripisList
                CreateAssignmentButton(wrapPanel, pripis)
            Next

            If pripisList.Count = 0 Then
                wrapPanel.Children.Add(New TextBlock() With {
                    .Text = "Нет приписок",
                    .HorizontalAlignment = HorizontalAlignment.Center,
                    .VerticalAlignment = VerticalAlignment.Center,
                    .Foreground = Brushes.Gray
                })
            End If

            scrollViewer.Content = wrapPanel
            tabItem.Content = scrollViewer
            AssignmentTabControl.Items.Add(tabItem)
        End Sub


        Public Function GetAllPripisForRoad(road As String) As List(Of String)
            Dim list As New List(Of String)

            ' 1. Базовые для этой дороги
            If RoadToPripis.ContainsKey(road) Then
                list.AddRange(RoadToPripis(road))
            End If

            ' 2. Пользовательские — ВСЕ, потому что у нас нет привязки к дороге
            list.AddRange(LoadUserPripis())

            Return list.Distinct().OrderBy(Function(x) x).ToList()
        End Function


        Private Sub CreateAssignmentButton(parentPanel As WrapPanel, assignment As String)
            Dim btn As New Button() With {
       .Content = assignment,
       .Tag = assignment,
       .Style = TryCast(FindResource("PopupStyleButton"), Style),
       .Height = 30,
       .Margin = New Thickness(2)
   }

            AddHandler btn.Click, Sub(s, e)
                                      Dim b = DirectCast(s, Button)
                                      _selectedPripisLok = b.Tag.ToString()
                                      DeleteAssignmentButton.IsEnabled = True
                                      HighlightAssignmentButton(b)
                                  End Sub

            parentPanel.Children.Add(btn)

        End Sub


        Private Sub HighlightAssignmentButton(selectedButton As Button)
            ClearAssignmentButtonSelection()
            If selectedButton IsNot Nothing Then
                selectedButton.Background = Brushes.LightGreen
                selectedButton.Foreground = Brushes.DarkGreen
                selectedButton.BringIntoView()
            End If
        End Sub

        Private Sub ClearAssignmentButtonSelection()
            For Each tabItem In AssignmentTabControl.Items.OfType(Of TabItem)()
                Dim wrapPanel = TryCast(TryCast(tabItem.Content, ScrollViewer)?.Content, WrapPanel)
                If wrapPanel Is Nothing Then Continue For

                For Each btn As Button In wrapPanel.Children.OfType(Of Button)()
                    btn.ClearValue(Button.BackgroundProperty)
                    btn.ClearValue(Button.ForegroundProperty)
                Next
            Next
        End Sub


        Private Sub RefreshAssignmentTab(roadCode As String)
            Dim tabItem = AssignmentTabControl.Items.OfType(Of TabItem)().
        FirstOrDefault(Function(t) t.Tag.ToString() = roadCode)

            If tabItem IsNot Nothing Then
                Dim scrollViewer = TryCast(tabItem.Content, ScrollViewer)
                If scrollViewer IsNot Nothing Then
                    Dim wrapPanel = TryCast(scrollViewer.Content, WrapPanel)
                    If wrapPanel IsNot Nothing Then
                        wrapPanel.Children.Clear()

                        Dim pripisList = If(PripLokModule.RoadToPripis.ContainsKey(roadCode),
                                    PripLokModule.RoadToPripis(roadCode),
                                    New List(Of String))

                        For Each pripis In pripisList
                            CreateAssignmentButton(wrapPanel, pripis)
                        Next

                        If pripisList.Count = 0 Then
                            wrapPanel.Children.Add(New TextBlock() With {
                        .Text = "Нет приписок",
                        .HorizontalAlignment = HorizontalAlignment.Center,
                        .VerticalAlignment = VerticalAlignment.Center,
                        .Foreground = Brushes.Gray
                    })
                        End If
                    End If
                End If
            End If
        End Sub









        Private Sub LoadSeriesByCategories()
            Dim seriesTypes = GetAllSeriesTypes()

            For Each kvp In seriesTypes
                CreateTabForSeriesType(kvp.Key, kvp.Value)
            Next

            If SeriesTabControl.Items.Count > 0 Then
                SeriesTabControl.SelectedIndex = 0
            End If
        End Sub

        Private Sub CreateTabForSeriesType(typeKey As String, typeName As String)
            Dim tabItem As New TabItem()
            tabItem.Header = typeName
            tabItem.Tag = typeKey

            Dim scrollViewer As New ScrollViewer()
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled

            Dim scrollBarStyle = TryCast(Me.FindResource("CustomScrollBarStyle"), Style)
            If scrollBarStyle IsNot Nothing Then
                scrollViewer.Resources.Add(GetType(System.Windows.Controls.Primitives.ScrollBar), scrollBarStyle)
            End If

            Dim wrapPanel As New WrapPanel()
            wrapPanel.Orientation = Orientation.Horizontal
            wrapPanel.Margin = New Thickness(2)


            Dim seriesList = GetSeriesByType(typeKey)

            For Each series In seriesList
                CreateSeriesButton(wrapPanel, series)
            Next

            If seriesList.Count = 0 Then
                Dim textBlock As New TextBlock()
                textBlock.Text = "Нет доступных серий"
                textBlock.HorizontalAlignment = HorizontalAlignment.Center
                textBlock.VerticalAlignment = VerticalAlignment.Center
                textBlock.Foreground = Brushes.Gray
                wrapPanel.Children.Add(textBlock)
            End If

            scrollViewer.Content = wrapPanel
            tabItem.Content = scrollViewer

            SeriesTabControl.Items.Add(tabItem)
        End Sub

        Private Sub CreateSeriesButton(parentPanel As WrapPanel, series As String)
            Dim button As New Button()
            button.Content = series
            button.Tag = series
            button.Style = TryCast(FindResource("PopupStyleButton"), Style)
            'button.Width = 85
            button.Height = 30
            button.Margin = New Thickness(2)
            'button.ToolTip = "Выбрать серию: " & series

            ' Обработчик клика - выбираем серию
            AddHandler button.Click,
               Sub(sender As Object, e As RoutedEventArgs)
                   Dim btn = DirectCast(sender, Button)
                   _selectedSeries = btn.Tag.ToString()
                   OkButton.IsEnabled = True
                   DeleteButton.IsEnabled = True
                   HighlightButton(btn)
               End Sub

            parentPanel.Children.Add(button)
        End Sub

        Private Sub ClearButtonSelection()
            For Each tabItem In SeriesTabControl.Items.OfType(Of TabItem)()
                Dim wrapPanel = TryCast(TryCast(tabItem.Content, ScrollViewer)?.Content, WrapPanel)
                If wrapPanel Is Nothing Then Continue For

                For Each btn As Button In wrapPanel.Children.OfType(Of Button)()
                    btn.ClearValue(Button.BackgroundProperty)
                    btn.ClearValue(Button.ForegroundProperty)
                Next
            Next

        End Sub



        Private Sub OkButton_Click(sender As Object, e As RoutedEventArgs)
            If Not String.IsNullOrEmpty(_selectedSeries) Then
                PointedOtkaz.SerLokExact = _selectedSeries
                RaiseEvent SeriesSelected(Me, _selectedSeries)
            End If
            If Not String.IsNullOrEmpty(_selectedPripisLok) Then
                PointedOtkaz.PripLok = _selectedPripisLok  ' ← добавили
            End If
            If Not String.IsNullOrEmpty(NumLokTextBox.Text.Trim()) Then
                PointedOtkaz.NumLok = NumLokTextBox.Text.Trim()
            End If
            ' ← УСТАНАВЛИВАЕМ VidT по серии
            PointedOtkaz.VidT = SerLokModule.GetVidTBySeries(_selectedSeries)
            RaiseEvent SeriesSelected(Me, _selectedSeries)
        End Sub



        Private Sub CancelButton_Click(sender As Object, e As RoutedEventArgs)
            RaiseEvent Cancelled(Me, EventArgs.Empty)
        End Sub
        Private Sub CustomSeriesButton_Click(sender As Object, e As RoutedEventArgs)
            ShowCustomSeriesInput()
        End Sub

        Private Sub ShowCustomSeriesInput()
            MainGrid.Visibility = Visibility.Collapsed
            CustomSeriesInputControl.Visibility = Visibility.Visible
            CustomSeriesInputControl.Reset()
            CustomSeriesInputControl.FocusOnSeriesInput()
        End Sub

        Private Sub HideCustomSeriesInput()
            MainGrid.Visibility = Visibility.Visible
            CustomSeriesInputControl.Visibility = Visibility.Collapsed
        End Sub

        Private Sub CustomSeriesInput_OkClicked(sender As Object, e As EventArgs)
            Dim customInput = TryCast(sender, CustomSeriesInput)
            If customInput IsNot Nothing Then
                ProcessNewSeries(customInput.SelectedSeries, customInput.SelectedType)
                HideCustomSeriesInput()
            End If
        End Sub

        Private Sub ProcessNewSeries(series As String, seriesType As String)
            Dim trimmedSeries = series.Trim().ToUpper()

            If SeriesExists(trimmedSeries) Then
                ' Серия уже есть - просто выбираем ее
                _selectedSeries = trimmedSeries
                OkButton.IsEnabled = True

                ' Находим и выделяем кнопку
                SelectAndHighlightButton(trimmedSeries)
            Else
                ' Добавляем новую серию
                AddNewSeries(trimmedSeries, seriesType)

                ' Обновляем нужную вкладку
                RefreshSeriesTab(seriesType)

                ' Выбираем добавленную серию
                _selectedSeries = trimmedSeries
                OkButton.IsEnabled = True

                ' Находим и выделяем новую кнопку
                SelectAndHighlightButton(trimmedSeries)
            End If
        End Sub



        Private Function DetermineSeriesTypeForRemoval(series As String) As String
            Dim trimmedSeries = series.Trim().ToUpper()

            If _lokElektrovozList IsNot Nothing AndAlso _lokElektrovozList.Contains(trimmedSeries) Then
                Return "Elektrovoz"
            ElseIf _lokTeplovozList IsNot Nothing AndAlso _lokTeplovozList.Contains(trimmedSeries) Then
                Return "Teplovoz"
            ElseIf _lokParovozList IsNot Nothing AndAlso _lokParovozList.Contains(trimmedSeries) Then
                Return "Parovoz"
            ElseIf _lokMVPSList IsNot Nothing AndAlso _lokMVPSList.Contains(trimmedSeries) Then
                Return "MVPS"
            ElseIf _lokSSPSList IsNot Nothing AndAlso _lokSSPSList.Contains(trimmedSeries) Then
                Return "SSPS"
            End If

            Return ""
        End Function



        Private Sub SelectAndHighlightButton(series As String)
            ' Находим кнопку с нужной серией
            Dim targetButton As Button = Nothing

            For Each tabItem In SeriesTabControl.Items.OfType(Of TabItem)()
                Dim scrollViewer = TryCast(tabItem.Content, ScrollViewer)
                If scrollViewer IsNot Nothing Then
                    Dim wrapPanel = TryCast(scrollViewer.Content, WrapPanel)
                    If wrapPanel IsNot Nothing Then
                        For Each child In wrapPanel.Children
                            Dim btn = TryCast(child, Button)
                            If btn IsNot Nothing AndAlso btn.Tag?.ToString() = series Then
                                targetButton = btn
                                Exit For
                            End If
                        Next
                        If targetButton IsNot Nothing Then Exit For
                    End If
                End If
            Next

            ' Выделяем найденную кнопку (или ничего, если не найдено)
            HighlightButton(targetButton)
        End Sub

        Private Sub CustomSeriesInput_CancelClicked(sender As Object, e As EventArgs)
            HideCustomSeriesInput()
        End Sub

        Private Sub RefreshSeriesTab(seriesType As String)
            Dim tabItem = SeriesTabControl.Items.OfType(Of TabItem)().
                FirstOrDefault(Function(t) t.Tag.ToString().Equals(seriesType, StringComparison.OrdinalIgnoreCase))

            If tabItem IsNot Nothing Then
                Dim scrollViewer = TryCast(tabItem.Content, ScrollViewer)
                If scrollViewer IsNot Nothing Then
                    Dim wrapPanel = TryCast(scrollViewer.Content, WrapPanel)
                    If wrapPanel IsNot Nothing Then
                        wrapPanel.Children.Clear()

                        Dim seriesList = GetSeriesByType(seriesType)

                        For Each series In seriesList
                            CreateSeriesButton(wrapPanel, series)
                        Next

                        If seriesList.Count = 0 Then
                            Dim textBlock As New TextBlock()
                            textBlock.Text = "Нет доступных серий"
                            textBlock.HorizontalAlignment = HorizontalAlignment.Center
                            textBlock.VerticalAlignment = VerticalAlignment.Center
                            textBlock.Foreground = Brushes.Gray
                            wrapPanel.Children.Add(textBlock)
                        End If
                    End If
                End If
            End If
        End Sub

        Private Sub DeleteButton_Click(sender As Object, e As RoutedEventArgs)
            If String.IsNullOrEmpty(_selectedSeries) Then Return

            ' Подтверждение удаления
            Dim result = MessageBox.Show($"Вы уверены, что хотите удалить серию '{_selectedSeries}'?",
                                        "Подтверждение удаления",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Warning)

            If result = MessageBoxResult.Yes Then
                ' Определяем тип серии
                Dim seriesType = DetermineSeriesTypeForRemoval(_selectedSeries)

                If Not String.IsNullOrEmpty(seriesType) Then
                    ' Удаляем серию
                    If RemoveSeries(_selectedSeries, seriesType) Then
                        ' Обновляем вкладку
                        RefreshSeriesTab(seriesType)

                        ' Сбрасываем выбор
                        _selectedSeries = Nothing
                        OkButton.IsEnabled = False
                        DeleteButton.IsEnabled = False
                        ClearButtonSelection()

                        MessageBox.Show($"Серия '{_selectedSeries}' успешно удалена.",
                                       "Удалено",
                                       MessageBoxButton.OK,
                                       MessageBoxImage.Information)
                    Else
                        MessageBox.Show($"Не удалось удалить серию '{_selectedSeries}'.",
                                       "Ошибка",
                                       MessageBoxButton.OK,
                                       MessageBoxImage.Error)
                    End If
                End If
            End If
        End Sub

        Private Sub HighlightButton(selectedButton As Button)
            ClearButtonSelection()
            If selectedButton IsNot Nothing Then
                selectedButton.Background = Brushes.LightBlue
                selectedButton.Foreground = Brushes.DarkBlue
                selectedButton.BringIntoView()
            End If
        End Sub

        Private Sub CustomAssignmentButton_Click(sender As Object, e As RoutedEventArgs)
            ShowCustomPripisInput()
            '     Dim newPripis = Microsoft.VisualBasic.Interaction.InputBox(
            '"Введите новую приписку:", "Новая приписка", "")

            '     If Not String.IsNullOrWhiteSpace(newPripis) Then
            '         PripLokModule.AddPripis(newPripis.Trim())
            '         ' Обновим все вкладки (или одну — по желанию)
            '         LoadAssignmentsByCategories()  ' ← проще перезагрузить всё
            '         ' Или: RefreshAssignmentTab("ППЖТ") и т.д.
            '     End If
        End Sub

        Private Sub DeleteAssignmentButton_Click(sender As Object, e As RoutedEventArgs)
            If String.IsNullOrEmpty(_selectedPripisLok) Then Return

            Dim result = MessageBox.Show(
        $"Удалить приписку '{_selectedPripisLok}'?",
        "Подтверждение",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning)

            If result = MessageBoxResult.Yes Then
                ' ← Удаляем
                If PripLokModule.RemovePripis(_selectedPripisLok) Then
                    ' ← Обновляем ВСЕ вкладки (или одну — если знаете road)
                    LoadAssignmentsByCategories()  ' ← Просто перезагрузите
                    _selectedPripisLok = Nothing
                    DeleteAssignmentButton.IsEnabled = False
                    ClearAssignmentButtonSelection()
                End If
            End If
        End Sub

        Private Sub NumLokTextBox_TextChanged(sender As Object, e As TextChangedEventArgs)
            OkButton.IsEnabled = Not String.IsNullOrEmpty(_selectedSeries) OrElse Not String.IsNullOrEmpty(NumLokTextBox.Text.Trim())
        End Sub

        Private Sub NumLokTextBox_PreviewTextInput(sender As Object, e As TextCompositionEventArgs)
            e.Handled = Not IsNumer(e.Text)
        End Sub
        Private Function IsNumer(text As String) As Boolean
            Return text.All(Function(c) Char.IsDigit(c) Or c = "/")
        End Function


    End Class


End Namespace

