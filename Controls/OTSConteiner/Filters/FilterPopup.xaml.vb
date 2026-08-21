
Imports System.Globalization
Imports System.Windows.Media
Imports System.Windows.Threading

Namespace Kas
    Partial Public Class FilterPopup
        Inherits UserControl
        Private _fieldType As String = "String" ' Новое свойство: тип поля
        Private _propertyName As String = ""
        Private _copyButtonOriginalText As String = "🗒️ копировать"

        ' Новое свойство для типа поля
        Public Property FieldType As String
            Get
                Return _fieldType
            End Get
            Set(value As String)
                _fieldType = value.ToLowerInvariant()
                UpdateUIForFieldType() ' Обновляем UI при изменении типа
            End Set
        End Property

        Public Property PropertyName As String
            Get
                Return _propertyName
            End Get
            Set(value As String)
                _propertyName = value
            End Set
        End Property

        Sub New()
            InitializeComponent()
            _copyButtonOriginalText = CopyButton.Content.ToString()
            'CopyAsText.IsEnabled = MW.TRowsContainer.SortOrderToggleUC.SortOrderToggle.IsChecked
        End Sub

        ' Вспомогательный метод для настройки UI в зависимости от типа поля
        Private Sub UpdateUIForFieldType()
            Select Case _fieldType
                Case "boolean"
                    FilterListBox.SelectionMode = SelectionMode.Single
                    ' Можно добавить особую логику для булевых значений
                    ' Например, заполнить только "True" / "False" (или "Да" / "Нет")
                Case "string", "text"
                    FilterListBox.SelectionMode = SelectionMode.Multiple
                Case "number", "numeric"
                    FilterListBox.SelectionMode = SelectionMode.Multiple
                    ' Возможно, показать специальный интерфейс для чисел (диапазон, операторы)
                    ' Пока оставим стандартный список
                Case "date"
                    FilterListBox.SelectionMode = SelectionMode.Multiple
                    ' Возможно, показать календарь или интерфейс для дат
                    ' Пока оставим стандартный список
                Case Else ' По умолчанию - строка
                    FilterListBox.SelectionMode = SelectionMode.Multiple
                    'Debug.WriteLine($"FilterPopup.UpdateUIForFieldType: Unknown FieldType '{_fieldType}', defaulting to Multiple SelectionMode")
            End Select
        End Sub

        ' Вариант 2: Если нужно копировать только выбранные элементы
        Private Sub CopyButton_Click_SelectedOnly(sender As Object, e As RoutedEventArgs)
            Try
                Dim originalText = CopyButton.Content.ToString()
                If FilterListBox.SelectedItems.Count > 0 Then
                    ' Копируем выбранные элементы
                    Dim selectedItems As List(Of String) = SelctedVals()
                    Dim textToCopy As String = String.Join(Environment.NewLine, selectedItems)
                    Clipboard.SetText(textToCopy)

                    CopyButton.Content = $"Сделано ({selectedItems.Count})"
                Else
                    ' Если ничего не выбрано, копируем все
                    Dim allItems As List(Of String) = AllVals()
                    Dim textToCopy As String = String.Join(Environment.NewLine, allItems)
                    Clipboard.SetText(textToCopy)

                    CopyButton.Content = "Сделано"
                End If

                RestoreButtonAfterDelay(CopyButton, originalText)

            Catch ex As Exception
                ShowMSG(MW, $"Ошибка при копировании:{vbCrLf}{ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End Sub

        Private Function SelctedVals() As List(Of String)
            Return FilterListBox.SelectedItems.Cast(Of Object)().OrderBy(Function(item) FilterListBox.Items.IndexOf(item)).Select(Function(item) item.ToString()).Where(Function(item) item <> "[Все]").ToList()
        End Function

        Private Function AllVals() As List(Of String)
            Dim allItems As New List(Of String)
            For Each item In FilterListBox.Items
                Dim itemText As String = item.ToString()
                ' Пропускаем пункт "[Все]" 
                If itemText <> "[Все]" Then
                    allItems.Add(itemText)
                End If
            Next
            Return allItems
        End Function

        ' Общий метод для восстановления кнопки после копирования
        Private Sub RestoreButtonAfterDelay(button As Button, originalText As String)
            button.IsEnabled = False

            Dim timer As New DispatcherTimer()
            timer.Interval = TimeSpan.FromMilliseconds(1500)
            AddHandler timer.Tick,
        Sub(s, args)
            button.Content = originalText
            button.IsEnabled = True
            timer.Stop()
        End Sub
            timer.Start()
        End Sub

        Private Sub CopyAsText_Click(sender As Object, e As RoutedEventArgs)
            Try
                Dim originalText = CopyAsText.Content.ToString()
                Dim selectedItems As List(Of String)

                If FilterListBox.SelectedItems.Count > 0 Then
                    ' Берем выбранные элементы
                    selectedItems = SelctedVals()
                Else
                    ' Если ничего не выбрано, берем все элементы
                    selectedItems = AllVals()
                End If

                If selectedItems.Count = 0 Then
                    ShowMSG(MW, "Нет элементов для копирования", "Информация",
                            MessageBoxButton.OK, MessageBoxImage.Information)
                    Return
                End If

                ' Разделяем на две группы:
                ' 1. Элементы с количеством > 1 (в скобках)
                ' 2. Элементы с количеством = 1 (в скобках)
                Dim itemsMoreThanOne As New List(Of String)
                Dim itemsExactlyOne As New List(Of String)

                For Each item As String In selectedItems
                    ' Парсим элемент вида "Название (число)"
                    Dim match = System.Text.RegularExpressions.Regex.Match(item, "^(.*?)\s*\((\d+)\)$")

                    If match.Success Then
                        Dim name = match.Groups(1).Value.Trim()
                        Dim count = Integer.Parse(match.Groups(2).Value)

                        If count > 1 Then
                            itemsMoreThanOne.Add(item)
                        Else
                            itemsExactlyOne.Add(name)
                        End If
                    Else
                        ' Если нет скобок с числом (неожиданный формат), добавляем как есть
                        itemsMoreThanOne.Add(item)
                    End If
                Next

                ' Формируем итоговый текст с заголовками
                Dim textToCopy As New System.Text.StringBuilder()

                ' Добавляем элементы с количеством > 1 с заголовком
                If itemsMoreThanOne.Count > 0 Then
                    textToCopy.AppendLine("Более 1 случая:")
                    textToCopy.AppendLine(String.Join(", ", itemsMoreThanOne))
                End If

                ' Добавляем элементы с количеством = 1 с заголовком
                If itemsExactlyOne.Count > 0 Then
                    If textToCopy.Length > 0 Then
                        textToCopy.AppendLine() ' Пустая строка между группами
                        textToCopy.AppendLine() ' Еще одна пустая строка
                    End If
                    textToCopy.AppendLine("По 1 случаю:")
                    textToCopy.Append(String.Join(", ", itemsExactlyOne))
                End If

                ' Копируем в буфер обмена
                Clipboard.SetText(textToCopy.ToString())
                CopyAsText.IsEnabled = False

                Dim totalCount = itemsMoreThanOne.Count + itemsExactlyOne.Count

                ' Сохраняем оригинальный текст кнопки и восстанавливаем через таймер
                CopyAsText.Content = $"Сделано ({totalCount})"
                RestoreButtonAfterDelay(CopyAsText, originalText)

            Catch ex As Exception
                ShowMSG(MW, $"Ошибка при копировании:{vbCrLf}{ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End Sub

        Private Sub FilterListBox_PreviewMouseDown(sender As Object, e As MouseButtonEventArgs)
            Dim listBox As ListBox = DirectCast(sender, ListBox)

            ' Найдём кликнутый ListBoxItem
            Dim hitTestResult = VisualTreeHelper.HitTest(listBox, e.GetPosition(listBox))
            If hitTestResult Is Nothing Then Return

            Dim obj As DependencyObject = hitTestResult.VisualHit
            Dim itemContainer As ListBoxItem = Nothing

            While obj IsNot Nothing
                If TypeOf obj Is ListBoxItem Then
                    itemContainer = DirectCast(obj, ListBoxItem)
                    Exit While
                End If
                obj = VisualTreeHelper.GetParent(obj)
            End While

            If itemContainer Is Nothing Then Return

            Dim clickedValue As String = itemContainer.Content?.ToString()
            If clickedValue <> "[Все]" Then Return

            e.Handled = True

            ' Теперь используем SelectionMode, установленный через FieldType
            If listBox.SelectionMode = SelectionMode.Multiple Then
                Dim realItemCount As Integer = listBox.Items.Count - 1
                Dim selectedRealCount As Integer = listBox.SelectedItems.Count

                If realItemCount > 0 AndAlso selectedRealCount = realItemCount Then
                    listBox.UnselectAll()
                Else
                    listBox.UnselectAll()
                    For i As Integer = 1 To listBox.Items.Count - 1
                        listBox.SelectedItems.Add(listBox.Items(i))
                    Next
                End If
            Else
                ' Для Single — сбрасываем SelectedItem
                listBox.SelectedItem = Nothing
            End If

            listBox.ScrollIntoView(listBox.Items(0))
        End Sub

        Private Sub OkButt_Click(sender As Object, e As RoutedEventArgs)
            ' Здесь будет логика сохранения выбранных значений
            ' В зависимости от FieldType будет формироваться либо List(Of String), либо List(Of FilterValue)
            ' Пока оставим пустым
        End Sub


    End Class


End Namespace

