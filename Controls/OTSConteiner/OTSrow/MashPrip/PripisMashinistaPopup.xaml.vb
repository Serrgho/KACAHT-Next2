Namespace Kas
    Partial Public Class PripisMashinistaPopup
        Inherits UserControl

        Public Event Confirmed As EventHandler
        Public Event Cancelled As EventHandler

        Private _selectedPripis As String = Nothing

        ' Списки приписок (только нужные)
        Private ReadOnly _KrasPripisList As New List(Of String) From {"ТЧЭ Боготол", "ТЧЭ Красноярск", "ТЧЭ Иланская", "ТЧЭ Ачинск", "ТЧЭ Абакан"}

        Private ReadOnly _VSIBPripisList As New List(Of String) From {"ТЧЭ Тайшет", "ТЧЭ Нижнеудинск", "ТЧЭ Вихоревка"}

        Private ReadOnly _ZSIBPripisList As New List(Of String) From {"ТЧЭ Тайга", "ТЧЭ Новосибирск", "ТЧЭ Курган"}

        Public Property Istochnik As String
            Get
                Return _istochnik
            End Get
            Set(value As String)
                _istochnik = value
                LoadAssignments() ' Перезагружаем вкладки при изменении источника
            End Set
        End Property
        Private _istochnik As String = ""





        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().

        End Sub

        Private Sub UserControl_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
            Me.DataContext = PointedOtkaz
            LoadAssignments()
            SurnameTextBox.Text = ""
            SurnameTextBox.Focus()
        End Sub

        Private Sub LoadAssignments()
            AssignmentTabControl.Items.Clear()

            Dim showKras As Boolean = False
            Dim showVSIB As Boolean = False
            Dim showZSIB As Boolean = False

            Select Case _istochnik?.Trim()
                Case "ВСЖД"
                    showVSIB = True
                Case "ЗСЖД"
                    showZSIB = True
                Case "ГИД УРАЛ"
                    showKras = True
                Case Else
                    showZSIB = True
                    showKras = True
                    showVSIB = True
            End Select

            If showKras Then
                CreateTab("Красноярская", _KrasPripisList)
            End If

            If showVSIB Then
                CreateTab("Восточно-Сибирская", _VSIBPripisList)
            End If

            If showZSIB Then
                CreateTab("Западно-Сибирская", _ZSIBPripisList)
            End If

            ' Если хотя бы одна вкладка есть — выбираем первую
            If AssignmentTabControl.Items.Count > 0 Then
                AssignmentTabControl.SelectedIndex = 0
            End If

        End Sub

        Private Sub CreateTab(roadName As String, pripisList As List(Of String))
            Dim tabItem As New TabItem() With {
                .Header = roadName,
                .Tag = roadName
            }

            Dim scrollViewer As New ScrollViewer() With {
                .VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                .HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            }

            Dim wrapPanel As New WrapPanel() With {
                .Orientation = Orientation.Horizontal,
                .Margin = New Thickness(5)
            }

            For Each pripis In pripisList
                CreatePripisButton(wrapPanel, pripis)
            Next

            scrollViewer.Content = wrapPanel
            tabItem.Content = scrollViewer
            AssignmentTabControl.Items.Add(tabItem)
        End Sub

        Private Sub CreatePripisButton(parentPanel As WrapPanel, pripis As String)
            Dim btn As New Button() With {
                .Content = pripis,
                .Tag = pripis,
                .Style = TryCast(FindResource("PopupStyleButton"), Style),
                .Height = 32,
                .MinWidth = 120,
                .Margin = New Thickness(3),
                .FontSize = 13
            }

            AddHandler btn.Click, Sub(s, e)
                                      Dim b = DirectCast(s, Button)
                                      _selectedPripis = b.Tag.ToString()
                                      HighlightButton(b)
                                      ValidateInput()
                                  End Sub

            parentPanel.Children.Add(btn)
        End Sub

        Private Sub HighlightButton(selectedButton As Button)
            ClearSelection()
            If selectedButton IsNot Nothing Then
                selectedButton.Background = Brushes.LightGreen
                selectedButton.Foreground = Brushes.DarkGreen
                selectedButton.BringIntoView()
            End If
        End Sub

        Private Sub ClearSelection()
            For Each tabItem In AssignmentTabControl.Items.OfType(Of TabItem)()
                Dim wrapPanel = TryCast(TryCast(tabItem.Content, ScrollViewer)?.Content, WrapPanel)
                If wrapPanel IsNot Nothing Then
                    For Each btn As Button In wrapPanel.Children.OfType(Of Button)()
                        btn.ClearValue(Button.BackgroundProperty)
                        btn.ClearValue(Button.ForegroundProperty)
                    Next
                End If
            Next
        End Sub

        Private Sub Input_TextChanged(sender As Object, e As TextChangedEventArgs)
            ValidateInput()
        End Sub

        Private Sub ValidateInput()
            OkButton.IsEnabled = Not String.IsNullOrWhiteSpace(SurnameTextBox.Text.Trim()) AndAlso
                                 Not String.IsNullOrEmpty(_selectedPripis)
        End Sub

        Public ReadOnly Property SelectedSurname As String
            Get
                Return SurnameTextBox.Text.Trim()
            End Get
        End Property

        Public ReadOnly Property SelectedPripis As String
            Get
                Return _selectedPripis
            End Get
        End Property

        Private Sub OkButton_Click(sender As Object, e As RoutedEventArgs)
            RaiseEvent Confirmed(Me, EventArgs.Empty)
        End Sub

        Private Sub CancelButton_Click(sender As Object, e As RoutedEventArgs)
            RaiseEvent Cancelled(Me, EventArgs.Empty)
        End Sub


    End Class
End Namespace

