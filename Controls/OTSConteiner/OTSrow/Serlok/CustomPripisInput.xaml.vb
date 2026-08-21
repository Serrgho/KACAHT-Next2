Namespace Kas

    Partial Public Class CustomPripisInput
        Public Event OkClicked As EventHandler
        Public Event CancelClicked As EventHandler

        Public ReadOnly Property SelectedPripis As String
            Get
                Return PripisTextBox.Text.Trim()
            End Get
        End Property

        Public ReadOnly Property SelectedRoad As String
            Get
                Dim selected = RadioButtonsPanel.Children.OfType(Of RadioButton)().
                               FirstOrDefault(Function(rb) rb.IsChecked = True)
                Return If(selected IsNot Nothing, selected.Content.ToString(), "")
            End Get
        End Property

        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().
            ' Заполняем дороги
            For Each road In PripLokModule.AllRoads
                Dim rb As New RadioButton() With {
                    .Content = road,
                    .Margin = New Thickness(5),
                    .GroupName = "RoadGroup",
                    .FontSize = 14
                }
                AddHandler rb.Checked, AddressOf OnRoadSelected
                RadioButtonsPanel.Children.Add(rb)
            Next

            'AddHandler OkButton.Click, AddressOf OnOkClick
            'AddHandler CancelButton.Click, AddressOf OnCancelClick
        End Sub

        Public Sub Reset()
            PripisTextBox.Text = ""
            For Each rb In RadioButtonsPanel.Children.OfType(Of RadioButton)()
                rb.IsChecked = False
            Next
        End Sub

        Public Sub FocusOnPripisInput()
            PripisTextBox.Focus()
        End Sub

        Private Sub PripisTextBox_TextChanged(sender As Object, e As TextChangedEventArgs)
            ValidateInput()
        End Sub

        Private Sub OnRoadSelected(sender As Object, e As RoutedEventArgs)
            ValidateInput()
        End Sub

        Private Sub ValidateInput()
            OkButton.IsEnabled = Not String.IsNullOrWhiteSpace(PripisTextBox.Text) AndAlso
                                  RadioButtonsPanel.Children.OfType(Of RadioButton)().
                                  Any(Function(rb) rb.IsChecked = True)
        End Sub



        Private Sub OkButton_Click(sender As Object, e As RoutedEventArgs)
            RaiseEvent OkClicked(Me, EventArgs.Empty)
        End Sub

        Private Sub CancelButton_Click(sender As Object, e As RoutedEventArgs)
            RaiseEvent CancelClicked(Me, EventArgs.Empty)
        End Sub
    End Class

End Namespace

