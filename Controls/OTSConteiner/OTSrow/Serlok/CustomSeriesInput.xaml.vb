Namespace Kas

    Partial Public Class CustomSeriesInput

        Public Event OkClicked As EventHandler
        Public Event CancelClicked As EventHandler

        Public Property SelectedSeries As String
        Public Property SelectedType As String

        Private _radioButtons As New Dictionary(Of String, RadioButton)

        Public Sub New()
            InitializeComponent()
            LoadRadioButtons()
            UpdateOkButtonState()
        End Sub

        Private Sub LoadRadioButtons()
            ' Получаем все типы серий
            Dim seriesTypes = GetAllSeriesTypes()

            ' Создаем радиокнопки для каждого типа
            For Each kvp In seriesTypes
                Dim radioButton As New RadioButton()
                radioButton.Content = kvp.Value
                radioButton.Tag = kvp.Key
                radioButton.Margin = New Thickness(5, 2, 5, 2)
                radioButton.GroupName = "SeriesType"

                ' Добавляем в словарь для быстрого доступа
                _radioButtons.Add(kvp.Key, radioButton)

                ' Добавляем на панель
                RadioButtonsPanel.Children.Add(radioButton)
            Next

            ' Выбираем первую радиокнопку по умолчанию
            If _radioButtons.Count > 0 Then
                Dim firstKey = _radioButtons.Keys.First()
                _radioButtons(firstKey).IsChecked = True
                SelectedType = firstKey
            End If
        End Sub

        Private Sub SeriesTextBox_TextChanged(sender As Object, e As TextChangedEventArgs)
            SelectedSeries = SeriesTextBox.Text.Trim()
            UpdateOkButtonState()
        End Sub

        Private Sub UpdateOkButtonState()
            ' Кнопка "Добавить" активна только если введена серия
            OkButton.IsEnabled = Not String.IsNullOrWhiteSpace(SelectedSeries)
        End Sub

        Private Sub OkButton_Click(sender As Object, e As RoutedEventArgs)
            ' Получаем выбранную серию
            SelectedSeries = SeriesTextBox.Text.Trim()

            ' Находим выбранный тип
            For Each kvp In _radioButtons
                If kvp.Value.IsChecked = True Then
                    SelectedType = kvp.Key
                    Exit For
                End If
            Next

            RaiseEvent OkClicked(Me, EventArgs.Empty)
        End Sub

        Private Sub CancelButton_Click(sender As Object, e As RoutedEventArgs)
            RaiseEvent CancelClicked(Me, EventArgs.Empty)
        End Sub

        ''' <summary>
        ''' Устанавливает фокус на поле ввода
        ''' </summary>
        Public Sub FocusOnSeriesInput()
            SeriesTextBox.Focus()
            SeriesTextBox.SelectAll()
        End Sub

        ''' <summary>
        ''' Сбрасывает состояние контрола
        ''' </summary>
        Public Sub Reset()
            SeriesTextBox.Text = String.Empty

            ' Выбираем первую радиокнопку
            If _radioButtons.Count > 0 Then
                Dim firstKey = _radioButtons.Keys.First()
                _radioButtons(firstKey).IsChecked = True
                SelectedType = firstKey
            End If

            UpdateOkButtonState()
            FocusOnSeriesInput()
        End Sub

    End Class

End Namespace

