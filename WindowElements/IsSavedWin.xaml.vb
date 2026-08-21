Namespace Kas
    Partial Public Class IsSavedWin
        'Private ReadOnly ZakemDict As New Dictionary(Of String, String) From
        '   {
        '   {"слд1", "СЛД Боготол"},
        '   {"слд2", "СЛД Красноярск"},
        '   {"слд3", "СЛД Иланская"},
        '   {"слд5", "СЛД Ачинск"},
        '   {"слд7", "СЛД Абакан"},
        '   {"ЛокоРемЗавод", "Локомотиворемонтный завод"},
        '   {"локостройЗавод", "Локомотивостроительный завод"},
        '   {"прочие", "Прочие предприятия"}
        '}

        '' Обратный словарь (полное имя -> сокращение)
        'Private ReadOnly ReverseZakemDict As Dictionary(Of String, String)

        'Public Property SelectedZakem As String

        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().
            ' Создаем обратный словарь
            'ReverseZakemDict = ZakemDict.ToDictionary(Function(kvp) kvp.Value, Function(kvp) kvp.Key)
            'RadioPanel.Children.Clear()
            '' Заполняем RadioPanel полными именами
            'For Each fullName In ZakemDict.Values
            '    Dim rb As New RadioButton() With {
            '        .Content = fullName,
            '        .Margin = New Thickness(5, 2, 5, 2)
            '    }
            '    RadioPanel.Children.Add(rb)
            'Next

            '' Первый выбран по умолчанию
            'SetFirstChked()
        End Sub

        'Sub SetFirstChked()
        '    If RadioPanel.Children.Count > 0 Then
        '        DirectCast(RadioPanel.Children(0), RadioButton).IsChecked = True
        '    End If
        'End Sub

        'Private Sub OkButton_Click(sender As Object, e As RoutedEventArgs)

        '    Dim selectedRb = RadioPanel.Children.OfType(Of RadioButton)() _
        '.FirstOrDefault(Function(rb) rb.IsChecked)

        '    If selectedRb Is Nothing Then
        '        MessageBox.Show("Выберите вариант", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning)
        '        Return
        '    End If

        '    ' Быстрый поиск по обратному словарю
        '    SelectedZakem = ReverseZakemDict(selectedRb.Content.ToString())

        '    DialogResult = True
        '    Close()
        'End Sub

        'Private Sub CancelButton_Click(sender As Object, e As RoutedEventArgs)
        '    ' Первый выбран по умолчанию
        '    SetFirstChked()
        '    DialogResult = False
        '    Close()
        'End Sub
    End Class
End Namespace

