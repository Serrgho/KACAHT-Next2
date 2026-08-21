Namespace Kas

    Partial Public Class IstochnikControl
        Private _selectedIstoch As String = ""
        Public Event IstochSelected As EventHandler(Of String)
        Public Event IstochCancelled As EventHandler

        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().

        End Sub


        Sub AddButtons()
            WPan.Children.Clear()

            For Each Istoch In IstRails
                CreateIstochButton(WPan, Istoch)
            Next

            If IstRails.Count = 0 Then
                Dim textBlock As New TextBlock()
                textBlock.Text = "Нет доступного списка"
                textBlock.HorizontalAlignment = HorizontalAlignment.Center
                textBlock.VerticalAlignment = VerticalAlignment.Center
                textBlock.Foreground = Brushes.Gray
                WPan.Children.Add(textBlock)
            End If
        End Sub

        Private Sub CreateIstochButton(parentPanel As WrapPanel, istoch As String)
            Dim button As New Button()
            button.Content = istoch
            button.Tag = istoch
            button.Style = TryCast(FindResource("PopupStyleButton"), Style)

            button.Height = 30
            button.Margin = New Thickness(2)


            ' Обработчик клика - выбираем дорогу
            AddHandler button.Click,
               Sub(sender As Object, e As RoutedEventArgs)
                   Dim btn = DirectCast(sender, Button)
                   HighlightButton(btn)
                   _selectedIstoch = btn.Tag.ToString()
                   OkButton.IsEnabled = True
               End Sub
            parentPanel.Children.Add(button)
        End Sub




        Private Sub HighlightButton(selectedButton As Button)
            ClearButtonSelection()
            If selectedButton IsNot Nothing Then
                selectedButton.Background = Brushes.LightBlue
                selectedButton.Foreground = Brushes.DarkBlue
                selectedButton.BringIntoView()
            End If
        End Sub
        Private Sub ClearButtonSelection()
            For Each btn As Button In WPan.Children.OfType(Of Button)()
                btn.ClearValue(Button.BackgroundProperty)
                btn.ClearValue(Button.ForegroundProperty)
                OkButton.IsEnabled = False
                _selectedIstoch = ""
            Next
        End Sub

        Private Sub UserControl_Loaded(sender As Object, e As RoutedEventArgs)
            Me.DataContext = PointedOtkaz
            AddButtons()

        End Sub

        Private Sub CancelButton_Click(sender As Object, e As RoutedEventArgs)
            _selectedIstoch = ""
            OkButton.IsEnabled = False
            RaiseEvent IstochCancelled(Me, EventArgs.Empty)
        End Sub


        'лучше через ОК - чтобы снизить шанс ошибочного выбора
        Private Sub OkButton_Click(sender As Object, e As RoutedEventArgs)
            If Not String.IsNullOrEmpty(_selectedIstoch) Then
                ' ← УСТАНАВЛИВАЕМ Istochnik в отказе
                PointedOtkaz.Istochnik = _selectedIstoch
                ' 2. УБИРАЕМ маркер "Источник" из UpdateNotes
                PointedOtkaz.RemoveItemUpdateNote("От кого ОТС")
                PointedOtkaz.RemoveItemUpdateNote("Ручной ввод")
                'чтобы закрыть попуп через OTSBlockControl
                RaiseEvent IstochSelected(Me, _selectedIstoch)
            End If

        End Sub

        Private Sub WPan_PreviewKeyDown(sender As Object, e As KeyEventArgs)
            If e.Key = Key.Enter AndAlso OkButton.IsEnabled Then
                OkButton_Click(Nothing, Nothing)
                e.Handled = True ' Останавливаем распространение события
            ElseIf e.Key = Key.Escape Then
                CancelButton_Click(Nothing, Nothing)
                e.Handled = True
            End If
        End Sub
    End Class
End Namespace

