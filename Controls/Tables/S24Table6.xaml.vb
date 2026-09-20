Imports System.Globalization
Imports System.Windows
Imports System.Windows.Controls

Namespace Kas

    Partial Public Class S24Table6
        Private _cur As List(Of Otkaz)
        Private _periodEnd As Date?
        Private Shared ReadOnly Ru As CultureInfo = CultureInfo.GetCultureInfo("ru-RU")

        ' Список ячеек текущего выделенного столбца
        Private _selectedCells As New List(Of TextBlock)()

        ' Цвет выделения (синий, полупрозрачный)
        Private ReadOnly SelectionBrush As New SolidColorBrush(Color.FromArgb(60, 128, 128, 128))
        Private ReadOnly DefaultBrush As New SolidColorBrush(Colors.Transparent)


        Public Sub New(currentYearOtkazy As List(Of Otkaz),
                       Optional periodStart As Date? = Nothing,
                       Optional periodEnd As Date? = Nothing)

            InitializeComponent()
            _periodEnd = periodEnd

            ' Фильтрация по периоду
            If periodStart.HasValue AndAlso periodEnd.HasValue Then
                Dim nextDayAfterEnd = periodEnd.Value.Date.AddDays(1)
                _cur = currentYearOtkazy.Where(Function(o)
                                                   Dim d = o.Nach.Date
                                                   Return d >= periodStart.Value.Date AndAlso d < nextDayAfterEnd
                                               End Function).ToList()
            Else
                _cur = currentYearOtkazy
            End If
        End Sub

        Private Sub S24Table6_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
            Try
                If _cur Is Nothing Then Return

                ' Обновляем заголовок
                If _periodEnd.HasValue Then
                    Dim monthName = Ru.DateTimeFormat.GetMonthName(_periodEnd.Value.Month)
                    LblCaption.Text = $"План корректировки за {monthName} {_periodEnd.Value.Year}"
                End If

                FillPlanTable()
            Catch ex As Exception
                MessageBox.Show(ex.ToString(), "Ошибка в S24Table6")
            End Try
        End Sub

#Region "ЛОГИКА ПЛАНОВ (ТОЛЬКО ЧАСЫ)"

        Private Sub FillPlanTable()
            Dim kmp = {1, 2, 3, 5, 7}

            For i = 0 To 4
                CType(FindName($"LblPlanR{i + 1}"), TextBlock).Text = Otkaz.GetTCHE_Name(kmp(i))
                FillPlanRow(i + 1, _cur.Where(Function(o) o.KomplexAsInt = kmp(i)).ToList())
            Next

            ' Всего — по всем комплексным
            FillPlanRow(6, _cur.Where(Function(o) o.KomplexAsInt > 0).ToList())
        End Sub

        Private Sub FillPlanRow(row As Integer, list As List(Of Otkaz))
            ' Проходим по колонкам 1-4 (данные планов)
            For col As Integer = 1 To 4
                Dim tbName As String = $"PlanR{row}C{col}"
                Dim tb = TryCast(FindName(tbName), TextBlock)

                If tb IsNot Nothing Then
                    ' Заполняем данные (только часы)
                    Select Case col
                        Case 1 : tb.Text = PlanHours(list, "дорог")
                        Case 2 : tb.Text = PlanHours(list, "технолог")
                        Case 3 : tb.Text = PlanHours(list, "корректировка")
                        Case 4 : tb.Text = PlanHoursAny(list)
                    End Select

                    ' === НОВОЕ: Подготовка к выделению столбцов (ТОЛЬКО строки 1-5) ===
                    If row >= 1 AndAlso row <= 5 Then
                        tb.Background = Brushes.Transparent ' Обязательно для MouseEnter!

                        AddHandler tb.MouseEnter, AddressOf Cell_MouseEnterForSelection
                        AddHandler tb.MouseLeave, AddressOf Cell_MouseLeaveForSelection
                        AddHandler tb.PreviewMouseLeftButtonDown, AddressOf Cell_PreviewMouseDownForSelection
                        AddHandler tb.MouseRightButtonDown, AddressOf CopyColumnToClipboard_Click
                    End If
                    ' ================================================================
                End If
            Next

        End Sub






        ''' <summary>
        ''' Подсвечивает весь столбец в диапазоне строк 1-5 указанной колонки
        ''' </summary>
        Private Sub HighlightColumn(col As Integer)
            ' Снимаем предыдущее выделение
            For Each cell In _selectedCells
                cell.Background = DefaultBrush
            Next
            _selectedCells.Clear()

            ' === ЖЕСТКИЙ ДИАПАЗОН: Строки 1-5, Колонка col ===
            ' Если колонка не входит в диапазон (1-4) - выходим сразу
            If col < 1 OrElse col > 3 Then Return

            For r As Integer = 1 To 5
                Dim tbName As String = $"PlanR{r}C{col}"
                Dim tb = TryCast(FindName(tbName), TextBlock)

                If tb IsNot Nothing Then
                    tb.Background = SelectionBrush
                    _selectedCells.Add(tb)
                End If
            Next
            ' ================================================
        End Sub

        ''' <summary>
        ''' Полная очистка выделения
        ''' </summary>
        Private Sub ClearSelection()
            For Each cell In _selectedCells
                cell.Background = DefaultBrush
            Next
            _selectedCells.Clear()
        End Sub

        ''' <summary>
        ''' При наведении на ячейку - подсвечиваем её столбец
        ''' </summary>
        Private Sub Cell_MouseEnterForSelection(sender As Object, e As MouseEventArgs)
            Dim tb = TryCast(sender, TextBlock)
            If tb Is Nothing Then Return

            ' Определяем колонку по имени ячейки (PlanR{row}C{col})
            Dim name = tb.Name
            If name.StartsWith("PlanR") AndAlso name.Contains("C") Then
                Dim colStr = name.Substring(name.LastIndexOf("C") + 1)
                Dim col As Integer = 0
                If Integer.TryParse(colStr, col) Then
                    HighlightColumn(col)
                End If
            End If
        End Sub

        ''' <summary>
        ''' При уходе мыши с таблицы - снимаем выделение
        ''' </summary>
        Private Sub Cell_MouseLeaveForSelection(sender As Object, e As MouseEventArgs)
            ' Если выделения нет - выходить нечего
            If _selectedCells.Count = 0 Then Return

            Dim mousePos = Mouse.GetPosition(PlanGrid)

            ' Берем первую и последнюю ячейку из текущего выделения
            Dim firstCell = _selectedCells.First()
            Dim lastCell = _selectedCells.Last()

            Dim parentFirst = TryCast(firstCell.Parent, Border)
            Dim parentLast = TryCast(lastCell.Parent, Border)

            If parentFirst Is Nothing OrElse parentLast Is Nothing Then Return

            ' Вычисляем реальные границы выделенного блока в координатах PlanGrid
            Dim topLeft = parentFirst.TranslatePoint(New Point(0, 0), PlanGrid)
            Dim bottomRight = parentLast.TranslatePoint(
        New Point(parentLast.ActualWidth, parentLast.ActualHeight), PlanGrid)

            Dim blockRect = New Rect(topLeft, bottomRight)

            ' Красим обратно в прозрачный ТОЛЬКО если курсор реально покинул весь блок
            If Not blockRect.Contains(mousePos) Then
                For Each cell In _selectedCells
                    cell.Background = DefaultBrush
                Next
                _selectedCells.Clear()
            End If
        End Sub

        ''' <summary>
        ''' Фиксируем выделение столбца при клике ЛКМ
        ''' </summary>
        Private Sub Cell_PreviewMouseDownForSelection(sender As Object, e As MouseButtonEventArgs)
            Dim tb = TryCast(sender, TextBlock)
            If tb Is Nothing Then Return

            ' Определяем колонку по имени ячейки
            Dim name = tb.Name
            If name.StartsWith("PlanR") AndAlso name.Contains("C") Then
                Dim colStr = name.Substring(name.LastIndexOf("C") + 1)
                Dim col As Integer = 0
                If Integer.TryParse(colStr, col) Then
                    HighlightColumn(col)
                End If
            End If
        End Sub

        ''' <summary>
        ''' Копирует ВЫДЕЛЕННЫЙ СТОЛБЕЦ в буфер обмена с эмуляцией объединения (значение + TAB + пусто)
        ''' </summary>
        Private Sub CopyColumnToClipboard_Click(sender As Object, e As MouseButtonEventArgs)
            CopySelectedColumn()

        End Sub

        ''' <summary>
        ''' Вынесенная логика копирования (без зависимости от MouseEventArgs)
        ''' </summary>
        Private Sub CopySelectedColumn()
            If _selectedCells.Count = 0 Then
                MessageBox.Show("Сначала выделите столбец мышью!", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            Dim sb As New System.Text.StringBuilder()

            For i As Integer = 0 To _selectedCells.Count - 1
                Dim cellText As String = _selectedCells(i).Text
                sb.Append(cellText & vbTab & "")

                If i < _selectedCells.Count - 1 Then sb.Append(vbCrLf)
            Next

            Clipboard.SetText(sb.ToString())
        End Sub


        ''' <summary>
        ''' Возвращает ТОЛЬКО сумму часов. Если 0 - возвращает пустую строку.
        ''' </summary>
        Private Function PlanHours(list As List(Of Otkaz), pattern As String) As String
            Dim q = list.Where(Function(o) HasPlan(o, pattern)).ToList()

            If q.Count = 0 Then Return ""

            Dim totalHours As Double = q.Sum(Function(o) CSng(o.PCh))

            ' === ИСПРАВЛЕНИЕ: Скрываем нули ===
            If Math.Abs(totalHours) < 0.005 Then Return ""

            Return F2(totalHours)
        End Function

        ''' <summary>
        ''' Сумма часов по ЛЮБОМУ плану. Если 0 - пустая строка.
        ''' </summary>
        Private Function PlanHoursAny(list As List(Of Otkaz)) As String
            Dim q = list.Where(Function(o) HasPlan(o, "др дорогу") OrElse
                                           HasPlan(o, "технолог") OrElse
                                           HasPlan(o, "корректировка")).ToList()

            If q.Count = 0 Then Return ""

            Dim totalHours As Double = q.Sum(Function(o) CSng(o.PCh))

            ' === ИСПРАВЛЕНИЕ: Скрываем нули ===
            If Math.Abs(totalHours) < 0.005 Then Return ""

            Return F2(totalHours)
        End Function

        Private Function HasPlan(o As Otkaz, pattern As String) As Boolean
            Return o.Plan IsNot Nothing AndAlso
                   o.Plan.Any(Function(p) p.Description IsNot Nothing AndAlso
                                          p.Description.ToLower().Contains(pattern))
        End Function

        ''' <summary>
        ''' Форматирование: 2 знака после запятой, русская локаль
        ''' </summary>
        Private Function F2(v As Double) As String
            Return v.ToString("F2", Ru)
        End Function

        Private Sub S24Table6_Unloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded

            _cur = Nothing

            ' ВЫЗЫВАЕМ ТВОЙ УНИВЕРСАЛЬНЫЙ ОТПИСЧИК
            UnsubscribeAllEvents(Me)



            ' ОТПИСЫВАЕМСЯ ОТ Unloaded (ЧТОБЫ НЕ БЫЛО ЦИКЛИЧЕСКИХ ССЫЛОК)
            RemoveHandler Me.Unloaded, AddressOf S24Table6_Unloaded

            GC.Collect()
        End Sub

#End Region






    End Class

End Namespace