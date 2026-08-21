Namespace Kas
    Partial Public Class ActiveFiltersPopup
        Inherits UserControl

        Public Sub New()
            InitializeComponent()
        End Sub

        ' Метод для заполнения панели фильтрами
        Public Sub PopulateFilters(filterDescription As String)

            FiltersPanel.Children.Clear() ' Очищаем предыдущее содержимое

            ' Разбиваем строку по строкам
            Dim lines = filterDescription.Split({vbCrLf, vbLf}, StringSplitOptions.None)

            For Each line In lines
                Dim tb As New TextBlock()
                tb.TextWrapping = TextWrapping.Wrap ' Перенос строк

                ' --- НОВОЕ: Разбор строки и формирование Inlines ---
                ' Пример строки: "  - Приписка локомотива: ТЧЭ Абакан, ТЧЭ Красноярск"
                ' Нужно выделить "Приписка локомотива:" как жирный текст

                If line.Contains(": ") Then ' Если строка содержит ": ", значит, есть имя свойства и значения
                    Dim parts = line.Split(New String() {": "}, 2, StringSplitOptions.None) ' Разбиваем на 2 части: до ": " и после
                    If parts.Length = 2 Then
                        ' Первая часть (до ": ") - это имя свойства (название фильтра)
                        Dim propNameRun As New Run(parts(0) & ": ") ' Добавляем ": " обратно к названию
                        propNameRun.FontWeight = FontWeights.SemiBold
                        propNameRun.Foreground = System.Windows.Media.Brushes.Black ' Или используй StaticResource, если определено в XAML Resources
                        propNameRun.FontSize = 15 ' Или какой размер тебе нравится

                        ' Вторая часть (после ": ") - это значения
                        Dim valuesRun As New Run(parts(1))
                        valuesRun.FontWeight = FontWeights.Normal
                        valuesRun.Foreground = System.Windows.Media.Brushes.Black ' Или используй StaticResource
                        valuesRun.FontSize = 15

                        tb.Inlines.Add(propNameRun)
                        tb.Inlines.Add(valuesRun)
                    Else
                        ' Если строка не содержит ": ", просто добавляем её как обычный текст
                        tb.Text = line
                        ' Применяем стили в зависимости от содержимого строки (как раньше)
                        ApplyStyleToTextBlock(tb, line)
                    End If
                Else
                    ' Если строка не содержит ": ", просто добавляем её как обычный текст
                    tb.Text = line
                    ' Применяем стили в зависимости от содержимого строки (как раньше)
                    ApplyStyleToTextBlock(tb, line)
                End If
                ' --- КОНЕЦ НОВОГО ---

                FiltersPanel.Children.Add(tb)
            Next
        End Sub

        ' Вспомогательный метод для применения стилей к TextBlock (для заголовков, "нет фильтров" и т.д.)
        Private Sub ApplyStyleToTextBlock(tb As TextBlock, line As String)
            If line.Contains("Активные фильтры") Then ' <-- Заголовок
                tb.FontWeight = FontWeights.Bold
                tb.Foreground = System.Windows.Media.Brushes.DarkBlue ' Или используй StaticResource, если определено в XAML Resources
                tb.FontSize = 20
                tb.Margin = New Thickness(0, 5, 0, 2)
            ElseIf line.Contains("Нет активных фильтров") Then ' <-- Сообщение "нет фильтров"
                tb.FontStyle = FontStyles.Italic
                tb.Foreground = System.Windows.Media.Brushes.Gray ' Или используй StaticResource
                tb.FontSize = 15
                tb.Margin = New Thickness(10, 0, 0, 2)
            Else ' <-- Обычная строка фильтра (предполагаем, начинается с "  - ", но уже обработана выше)
                ' Эти строки уже обработаны в PopulateFilters через Inlines
                ' Но если строка не содержит ": ", она попадёт сюда
                tb.FontWeight = FontWeights.Normal
                tb.Foreground = System.Windows.Media.Brushes.Black ' Или используй StaticResource
                tb.FontSize = 15
                tb.Margin = New Thickness(20, 0, 0, 2) ' Отступ для вложенных элементов
                tb.TextWrapping = TextWrapping.Wrap
            End If
        End Sub

    End Class
End Namespace
