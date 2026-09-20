Imports System.Globalization
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media

Namespace Kas

    Partial Public Class S24Table7

        Private _cur As List(Of Otkaz)
        Private _periodEnd As Date?
        Private Shared ReadOnly Ru As CultureInfo = CultureInfo.GetCultureInfo("ru-RU")
        Private Shared ReadOnly Komplexes As Integer() = {1, 2, 3, 5, 7}

        Public Sub New(currentYearOtkazy As List(Of Otkaz),
                       Optional periodStart As Date? = Nothing,
                       Optional periodEnd As Date? = Nothing)
            InitializeComponent()
            _periodEnd = periodEnd

            ' Фильтрация: только учтенные отказы 1 категории за период
            If periodStart.HasValue AndAlso periodEnd.HasValue Then
                Dim nextDayAfterEnd = periodEnd.Value.Date.AddDays(1)
                _cur = currentYearOtkazy.Where(Function(o)
                                                   Return o.Kat = 1 AndAlso
                                                          o.Uchet AndAlso
                                                          o.Nach.Date >= periodStart.Value.Date AndAlso
                                                          o.Nach.Date < nextDayAfterEnd
                                               End Function).ToList()
            Else
                _cur = currentYearOtkazy.Where(Function(o) o.Kat = 1 AndAlso o.Uchet).ToList()
            End If
        End Sub

        Private Sub S24Table7_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
            Try
                If _periodEnd.HasValue Then
                    Dim monthName = Ru.DateTimeFormat.GetMonthName(_periodEnd.Value.Month)
                    LblCaption.Text = $"ОТС 1 категории за {monthName} {_periodEnd.Value.Year}"
                End If

                BuildAndFillRows()
            Catch ex As Exception
                MessageBox.Show(ex.ToString(), "Ошибка в S24Table7")
            End Try
        End Sub

        Private Sub BuildAndFillRows()

            ' === ВАЖНО: Добавляем RowDefinition для ШАПКИ (строка 0) ===
            MainGrid.RowDefinitions.Add(New RowDefinition With {.Height = GridLength.Auto})

            Dim totalInBounds As Integer = 0
            Dim totalOther As Integer = 0

            ' Строки для 5 комплексов
            For i As Integer = 0 To Komplexes.Length - 1
                Dim k As Integer = Komplexes(i)

                MainGrid.RowDefinitions.Add(New RowDefinition With {.Height = GridLength.Auto})
                Dim rowIdx As Integer = MainGrid.RowDefinitions.Count - 1

                ' Название депо добавляем всегда
                AddCell(rowIdx, 0, Otkaz.GetTCHE_Name(k), "HeaderBorder", "HeaderTextStyle", True)

                Dim inBounds = CountOts(k, isLocal:=True)
                Dim otherRoads = CountOts(k, isLocal:=False)

                ' === ИСПРАВЛЕНИЕ: Не добавляем текст, если значение = 0 ===
                Dim inBoundsText As String = If(inBounds > 0, inBounds.ToString(), "")
                Dim otherRoadsText As String = If(otherRoads > 0, otherRoads.ToString(), "")

                AddCell(rowIdx, 1, inBoundsText, "CellBorder", "CellStyle")
                AddCell(rowIdx, 2, otherRoadsText, "CellBorder", "CellStyle")
                ' ============================================================

                totalInBounds += inBounds
                totalOther += otherRoads
            Next

            ' Итоговая строка идет СРАЗУ после последнего депо
            MainGrid.RowDefinitions.Add(New RowDefinition With {.Height = GridLength.Auto})
            Dim totalRowIdx As Integer = MainGrid.RowDefinitions.Count - 1

            AddCell(totalRowIdx, 0, "Всего:", "TotalBorder", "HeaderTextStyle", True)

            ' Для итогов тоже можно скрывать нули, но обычно "Всего" показывают всегда.
            ' Если нужно скрывать и итоги - раскомментируй проверку ниже:
            ' Dim totalInBoundsText As String = If(totalInBounds > 0, totalInBounds.ToString(), "")
            ' Dim totalOtherText As String = If(totalOther > 0, totalOther.ToString(), "")
            AddCell(totalRowIdx, 1, totalInBounds.ToString(), "TotalBorder", "CellStyle", isBold:=True)
            AddCell(totalRowIdx, 2, totalOther.ToString(), "TotalBorder", "CellStyle", isBold:=True)
        End Sub


        Private Function CountOts(komplex As Integer, isLocal As Boolean) As Integer
            Return _cur.Where(Function(o)
                                  If o.KomplexAsInt <> komplex Then Return False

                                  Dim dorName As String = If(o.MestoOTS_Dor, "").ToLower()
                                  Dim isKrasnoyarsk As Boolean = dorName.Contains("красноярск")

                                  If isLocal Then
                                      Return isKrasnoyarsk
                                  Else
                                      Return Not isKrasnoyarsk
                                  End If
                              End Function).Count()
        End Function

        ' Список ячеек текущего выделенного столбца
        Private _selectedCells As New List(Of TextBlock)()

        ' Цвет выделения (синий, полупрозрачный)
        Private ReadOnly SelectionBrush As New SolidColorBrush(Color.FromArgb(60, 128, 128, 128))
        Private ReadOnly DefaultBrush As New SolidColorBrush(Colors.Transparent)



        Private Sub AddCell(row As Integer, col As Integer, text As String,
                            borderStyleKey As String, textStyleKey As String,
                            Optional isBold As Boolean = False)



            Dim b As New Border()
            b.Style = CType(FindResource(borderStyleKey), Style)

            Dim t As New TextBlock()
            t.Style = CType(FindResource(textStyleKey), Style)
            t.Text = text
            If isBold Then t.FontWeight = FontWeights.Bold

            ' === ИСПРАВЛЕНИЕ: Исключаем шапку (row=0) и итоги ("Всего") ===
            ' Обработчики вешаются ТОЛЬКО на ячейки данных комплексов (строки 1-5)
            If row > 0 AndAlso Not text.StartsWith("Всего") Then
                t.Background = Brushes.Transparent

                AddHandler t.MouseEnter, AddressOf Cell_MouseEnterForSelection
                AddHandler t.MouseLeave, AddressOf Cell_MouseLeaveForSelection
                AddHandler t.PreviewMouseLeftButtonDown, AddressOf Cell_PreviewMouseDownForSelection
                AddHandler t.MouseRightButtonDown, AddressOf CopyColumnToClipboard_Click
            End If
            ' ================================================================

            b.Child = t
            Grid.SetRow(b, row)
            Grid.SetColumn(b, col)
            MainGrid.Children.Add(b)


            'Dim b As New Border()
            'b.Style = CType(FindResource(borderStyleKey), Style)

            'Dim t As New TextBlock()
            't.Style = CType(FindResource(textStyleKey), Style)
            't.Text = text
            'If isBold Then t.FontWeight = FontWeights.Bold

            'b.Child = t
            'Grid.SetRow(b, row)
            'Grid.SetColumn(b, col)
            'MainGrid.Children.Add(b)
        End Sub




        ''' <summary>
        ''' Подсвечивает весь столбец (все строки данных указанной колонки)
        ''' </summary>
        Private Sub HighlightColumn(col As Integer)
            ' Снимаем предыдущее выделение
            For Each cell In _selectedCells
                cell.Background = DefaultBrush
            Next
            _selectedCells.Clear()

            ' === ЖЕСТКИЙ ДИАПАЗОН: Строки 1-5, Колонка col ===
            ' Если колонка не входит в диапазон (1 или 2) - выходим сразу
            If col < 1 OrElse col > 2 Then Return

            For r As Integer = 1 To 5
                Dim border = FindChildByGridCoords(MainGrid, r, col)
                If border IsNot Nothing Then
                    Dim tb = TryCast(border.Child, TextBlock)
                    If tb IsNot Nothing Then
                        tb.Background = SelectionBrush
                        _selectedCells.Add(tb)
                    End If
                End If
            Next
            ' ================================================

            ' Сортируем выделенные ячейки сверху вниз (на всякий случай)
            _selectedCells.Sort(Function(a, b)
                                    Dim rowA = Grid.GetRow(TryCast(a.Parent, Border))
                                    Dim rowB = Grid.GetRow(TryCast(b.Parent, Border))
                                    Return rowA.CompareTo(rowB)
                                End Function)
        End Sub

        ''' <summary>
        ''' Ищет Border в MainGrid по заданным строке и колонке
        ''' </summary>
        Private Function FindChildByGridCoords(grid As Grid, row As Integer, col As Integer) As Border
            For Each child In grid.Children
                If TypeOf child Is Border Then
                    Dim b = CType(child, Border)
                    If Grid.GetRow(b) = row AndAlso Grid.GetColumn(b) = col Then
                        Return b
                    End If
                End If
            Next
            Return Nothing
        End Function


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

            Dim parentBorder = TryCast(tb.Parent, Border)
            If parentBorder Is Nothing Then Return

            Dim col = Grid.GetColumn(parentBorder)
            HighlightColumn(col)
        End Sub

        ''' <summary>
        ''' При уходе мыши с таблицы - снимаем выделение
        ''' </summary>
        Private Sub Cell_MouseLeaveForSelection(sender As Object, e As MouseEventArgs)
            ' Если выделения нет - выходить нечего
            If _selectedCells.Count = 0 Then Return

            Dim mousePos = Mouse.GetPosition(MainGrid)

            ' Берем первую (верхнюю) и последнюю (нижнюю) ячейку из текущего выделения
            ' _selectedCells уже отсортирован по строкам в HighlightColumn
            Dim firstCell = _selectedCells.First()
            Dim lastCell = _selectedCells.Last()

            Dim parentFirst = TryCast(firstCell.Parent, Border)
            Dim parentLast = TryCast(lastCell.Parent, Border)

            If parentFirst Is Nothing OrElse parentLast Is Nothing Then Return

            ' Вычисляем реальные границы выделенного блока в координатах MainGrid
            Dim topLeft = parentFirst.TranslatePoint(New Point(0, 0), MainGrid)
            Dim bottomRight = parentLast.TranslatePoint(
        New Point(parentLast.ActualWidth, parentLast.ActualHeight), MainGrid)

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

            Dim parentBorder = TryCast(tb.Parent, Border)
            If parentBorder Is Nothing Then Return

            Dim col = Grid.GetColumn(parentBorder)
            HighlightColumn(col)
        End Sub

        ''' <summary>
        ''' Копирует ВЫДЕЛЕННЫЙ СТОЛБЕЦ в буфер обмена с эмуляцией объединения (значение + TAB + пусто)
        ''' </summary>
        Private Sub CopyColumnToClipboard_Click(sender As Object, e As MouseButtonEventArgs)
            If _selectedCells.Count = 0 Then
                MessageBox.Show("Сначала выделите столбец мышью!", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning)
                e.Handled = True
                Return
            End If

            Dim sb As New System.Text.StringBuilder()

            ' Ячейки уже отсортированы сверху вниз в HighlightColumn
            For i As Integer = 0 To _selectedCells.Count - 1
                Dim cellText As String = _selectedCells(i).Text

                ' === ЭМУЛЯЦИЯ ОБЪЕДИНЕННОЙ ЯЧЕЙКИ ШИРИНОЙ 2 СТОЛБЦА ===
                ' Значение + TAB + ПУСТОТА. Excel воспринимает это как одну объединенную ячейку
                ' и НЕ РАЗЪЕДИНЯЕТ её при вставке!
                sb.Append(cellText & vbTab)
                ' ========================================================

                ' Перенос строки после каждой строки данных, кроме последней
                If i < _selectedCells.Count - 1 Then sb.Append(vbCrLf)
            Next

            Clipboard.SetText(sb.ToString())
            e.Handled = True
        End Sub

        Private Sub S24Table7_Unloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded
            _cur = Nothing

            ' ВЫЗЫВАЕМ ТВОЙ УНИВЕРСАЛЬНЫЙ ОТПИСЧИК
            UnsubscribeAllEvents(Me)



            ' ОТПИСЫВАЕМСЯ ОТ Unloaded (ЧТОБЫ НЕ БЫЛО ЦИКЛИЧЕСКИХ ССЫЛОК)
            RemoveHandler Me.Unloaded, AddressOf S24Table7_Unloaded

            GC.Collect()
        End Sub
    End Class

End Namespace