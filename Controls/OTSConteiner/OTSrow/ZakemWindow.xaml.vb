Namespace Kas
    Partial Public Class ZakemWindow

        ' Словарь для текущего экземпляра (Key -> Value)
        Private _zakemDict As Dictionary(Of String, String)
        ' Обратный словарь (Value -> Key) для поиска кода по названию
        Private _reverseZakemDict As Dictionary(Of String, String)

        ' Флаг: нужно ли выделять первый элемент при отмене
        Private _resetOnCancel As Boolean

        ' Текущий выбор
        Private _selectedKey As String = ""
        Private _selectedButton As Button = Nothing

        ' Результат для внешнего мира
        Public Property SelectedZakem As String


        ' --- КОНСТРУКТОРЫ ---

        ' Конструктор без параметров (обязателен для XAML дизайнера)
        Sub New()
            InitializeComponent()
            ' Значения по умолчанию
            _zakemDict = New Dictionary(Of String, String)
            _reverseZakemDict = New Dictionary(Of String, String)
            _resetOnCancel = False
            OkButton.IsEnabled = False
        End Sub

        ' Метод инициализации (вызывать после New)
        Public Sub Initialize(title As String, zakemDict As Dictionary(Of String, String), Optional resetOnCancel As Boolean = False)
            Zagolovok.Text = $"{title}{vbCrLf}{PointedOtkaz.MestoOTS}"
            _resetOnCancel = resetOnCancel
            _zakemDict = zakemDict
            _reverseZakemDict = _zakemDict.ToDictionary(Function(kvp) kvp.Value, Function(kvp) kvp.Key)

            ' --- Инициализация чекбокса и текстбокса из PointedOtkaz ---
            If PointedOtkaz IsNot Nothing AndAlso Not String.IsNullOrEmpty(PointedOtkaz.AlienSLD) Then
                ChkAlienSLD.IsChecked = True
                TxtAlienSLD.Text = PointedOtkaz.AlienSLD
            Else
                ChkAlienSLD.IsChecked = False
                TxtAlienSLD.Text = ""
            End If
            ' -----------------------------------------------------------


            PopulateButtons()
            OkButton.IsEnabled = False
        End Sub

        ' Перегрузка для удобства (с фильтром)
        Public Sub Initialize(title As String, filter As Otkaz.ZakemFilter, Optional resetOnCancel As Boolean = False)
            Initialize(title, Otkaz.GetZakemByFilter(filter), resetOnCancel)
        End Sub

        ' --- ЛОГИКА КНОПОК ---

        ' Заполнение панели кнопками
        Private Sub PopulateButtons()
            RadioPanel.Children.Clear()

            If _zakemDict Is Nothing OrElse _zakemDict.Count = 0 Then
                Dim tb As New TextBlock() With {
                    .Text = "Нет доступных вариантов",
                    .HorizontalAlignment = HorizontalAlignment.Center,
                    .VerticalAlignment = VerticalAlignment.Center,
                    .Foreground = Brushes.Gray,
                    .Margin = New Thickness(10)
                }
                RadioPanel.Children.Add(tb)
                Return
            End If

            For Each kvp In _zakemDict
                CreateZakemButton(RadioPanel, kvp.Key, kvp.Value)
            Next
        End Sub

        ' Создание одной кнопки
        Private Sub CreateZakemButton(parentPanel As StackPanel, key As String, fullName As String)
            Dim btn As New Button() With {
                .Content = fullName,
                .Tag = key,
                .Height = 30,
                .Margin = New Thickness(2)
            }

            ' Пытаемся применить стиль из ресурсов (если есть)
            Dim popupStyle = TryCast(FindResource("PopupStyleButton"), Style)
            If popupStyle IsNot Nothing Then btn.Style = popupStyle

            ' Обработчик клика
            AddHandler btn.Click,
                Sub(sender As Object, e As RoutedEventArgs)
                    Dim clickedBtn = DirectCast(sender, Button)
                    HighlightButton(clickedBtn)
                    OkButton.IsEnabled = True
                End Sub

            parentPanel.Children.Add(btn)
        End Sub

        ' Подсветка выбранной кнопки
        Private Sub HighlightButton(selectedBtn As Button)
            ClearButtonSelection()
            If selectedBtn IsNot Nothing Then
                selectedBtn.Background = Brushes.LightBlue
                selectedBtn.Foreground = Brushes.DarkBlue
                selectedBtn.BringIntoView()

                _selectedButton = selectedBtn
                _selectedKey = selectedBtn.Tag.ToString()

                ' Показываем панель только если текст содержит "СЛД"
                Dim txt = selectedBtn.Content?.ToString()
                If txt IsNot Nothing AndAlso txt.Contains("СЛД") Then
                    PanelAlienSLD.Visibility = Visibility.Visible
                Else
                    PanelAlienSLD.Visibility = Visibility.Collapsed
                End If

            End If
        End Sub

        ' Сброс выделения всех кнопок
        Private Sub ClearButtonSelection()
            For Each btn As Button In RadioPanel.Children.OfType(Of Button)()
                btn.ClearValue(Button.BackgroundProperty)
                btn.ClearValue(Button.ForegroundProperty)
            Next
            _selectedButton = Nothing
            _selectedKey = ""
            OkButton.IsEnabled = False
            PanelAlienSLD.Visibility = Visibility.Collapsed
        End Sub

        ' --- СОБЫТИЯ ---

        Private Sub OkButton_Click(sender As Object, e As RoutedEventArgs)
            If String.IsNullOrEmpty(_selectedKey) Then
                MessageBox.Show("Выберите вариант", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            SelectedZakem = _selectedKey

            ' --- Сохранение AlienSLD в PointedOtkaz ---
            If PointedOtkaz IsNot Nothing Then
                If PanelAlienSLD.Visibility = Visibility.Visible AndAlso ChkAlienSLD.IsChecked = True Then
                    ' Если панель видима и галочка стоит — пишем текст из TextBox
                    PointedOtkaz.AlienSLD = TxtAlienSLD.Text
                Else
                    ' Иначе — очищаем
                    PointedOtkaz.AlienSLD = ""
                End If
            End If
            ' ------------------------------------------



            DialogResult = True
            Close()
        End Sub

        Private Sub CancelButton_Click(sender As Object, e As RoutedEventArgs)
            If _resetOnCancel AndAlso RadioPanel.Children.OfType(Of Button)().Any() Then
                HighlightButton(DirectCast(RadioPanel.Children(0), Button))
            End If

            DialogResult = False
            Close()
        End Sub

        Private Sub ChkAlienSLD_CheckedChanged(sender As Object, e As RoutedEventArgs)
            If ChkAlienSLD.IsChecked = True Then
                TxtAlienSLD.Visibility = Visibility.Visible
                TxtAlienSLD.Focus() ' сразу ставим фокус в текстбокс для удобства
            Else
                TxtAlienSLD.Visibility = Visibility.Collapsed
                TxtAlienSLD.Text = "" ' очищаем при снятии галочки
            End If
        End Sub
    End Class
End Namespace

