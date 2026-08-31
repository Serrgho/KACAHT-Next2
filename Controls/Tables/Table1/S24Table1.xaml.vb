

Imports System.Linq
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Documents
Imports System.Windows.Input
Imports System.Windows.Media

Namespace Kas


    Public Class S24Table1

        ' ==================== ПОЛЯ ====================

        Private _currentYearOtkazy As List(Of Otkaz)
        Private _previousYearOtkazy As List(Of Otkaz)
        Private _previousYearOtkazyRaw As List(Of Otkaz)  ' ← Исходный список прошлого года (без фильтрации)
        Private _periodStart As Date?
        Private _periodEnd As Date?                        ' ← Запоминаем дату конца периода
        Private RowCellsMap As New Dictionary(Of Integer, TextBlock())

        ' ==================== КОНСТРУКТОРЫ ====================

        Public Sub New()
            InitializeComponent()
        End Sub

        Public Sub New(
        currentYearOtkazy As List(Of Otkaz),
        previousYearOtkazy As List(Of Otkaz),
        Optional periodStart As Date? = Nothing,
        Optional periodEnd As Date? = Nothing
    )
            InitializeComponent()
            ' Сохраняем дату конца периода (для строки 10)
            _periodStart = periodStart  ' ← ДОБАВИТЬ
            _periodEnd = periodEnd

            ' Сохраняем исходный список прошлого года (для строки 10)
            _previousYearOtkazyRaw = previousYearOtkazy

            ' ===== ФИЛЬТРАЦИЯ ТЕКУЩЕГО ГОДА =====
            If periodStart IsNot Nothing OrElse periodEnd IsNot Nothing Then
                Dim nextDayAfterEnd = If(periodEnd.HasValue,
                         periodEnd.Value.Date.AddDays(1),
                         Date.MaxValue)
                'Dim nextDayAfterEnd = If(periodEnd, Date.MaxValue).Date.AddDays(1)

                _currentYearOtkazy = currentYearOtkazy.Where(Function(o)
                                                                 Dim d = o.Nach.Date
                                                                 If periodStart IsNot Nothing AndAlso d < periodStart.Value.Date Then Return False
                                                                 If d >= nextDayAfterEnd Then Return False
                                                                 Return True
                                                             End Function).ToList()
            Else
                _currentYearOtkazy = currentYearOtkazy
            End If

            ' ===== ФИЛЬТРАЦИЯ ПРОШЛОГО ГОДА =====

            If periodStart IsNot Nothing OrElse periodEnd IsNot Nothing Then

                ' прошлый период: ручной из настроек (если попап ставил) или авто-сдвиг
                Dim oldP = GetPrevPeriod(If(periodStart, periodEnd).Value.Date,
                                         If(periodEnd, periodStart).Value.Date,
                                         previousYearOtkazy)

                Dim nextDayAfterOldEnd = oldP.End.Date.AddDays(1)

                _previousYearOtkazy = previousYearOtkazy.Where(Function(o)
                                                                   Dim d = o.Nach.Date
                                                                   If d < oldP.Start.Date Then Return False
                                                                   If d >= nextDayAfterOldEnd Then Return False
                                                                   Return True
                                                               End Function).ToList()
            Else
                _previousYearOtkazy = previousYearOtkazy
            End If





        End Sub

        ' ==================== ИНИЦИАЛИЗАЦИЯ ====================

        Private Sub InitializeRowCells()
            ' Полные строки (3, 4) — 20 ячеек
            RowCellsMap(3) = {
            R3C3, R3C4, R3C5, R3C6,
            R3C7, R3C8, R3C9, R3C10,
            R3C11, R3C12, R3C13, R3C14,
            R3C15, R3C16, R3C17, R3C18,
            R3C19, R3C20, R3C21, R3C22
        }

            RowCellsMap(4) = {
            R4C3, R4C4, R4C5, R4C6,
            R4C7, R4C8, R4C9, R4C10,
            R4C11, R4C12, R4C13, R4C14,
            R4C15, R4C16, R4C17, R4C18,
            R4C19, R4C20, R4C21, R4C22
        }

            ' Merged строки (5-10) — 15 ячеек
            RowCellsMap(5) = {
            R5C3, R5C4, R5C5,
            R5C6, R5C7, R5C8,
            R5C9, R5C10, R5C11,
            R5C12, R5C13, R5C14,
            R5C15, R5C16, R5C17
        }

            RowCellsMap(6) = {
            R6C3, R6C4, R6C5,
            R6C6, R6C7, R6C8,
            R6C9, R6C10, R6C11,
            R6C12, R6C13, R6C14,
            R6C15, R6C16, R6C17
        }

            RowCellsMap(7) = {
            R7C3, R7C4, R7C5,
            R7C6, R7C7, R7C8,
            R7C9, R7C10, R7C11,
            R7C12, R7C13, R7C14,
            R7C15, R7C16, R7C17
        }

            RowCellsMap(8) = {
            R8C3, R8C4, R8C5,
            R8C6, R8C7, R8C8,
            R8C9, R8C10, R8C11,
            R8C12, R8C13, R8C14,
            R8C15, R8C16, R8C17
        }

            RowCellsMap(9) = {
            R9C3, R9C4, R9C5,
            R9C6, R9C7, R9C8,
            R9C9, R9C10, R9C11,
            R9C12, R9C13, R9C14,
            R9C15, R9C16, R9C17
        }

            RowCellsMap(10) = {
            R10C3, R10C4, R10C5,
            R10C6, R10C7, R10C8,
            R10C9, R10C10, R10C11,
            R10C12, R10C13, R10C14,
            R10C15, R10C16, R10C17
        }

            ' Навешиваем обработчик клика на все ячейки с данными
            For Each kvp In RowCellsMap
                For Each tb In kvp.Value
                    AddHandler tb.MouseLeftButtonDown, AddressOf DataCell_MouseLeftButtonDown
                Next
            Next
        End Sub

        ' ==================== ЗАПОЛНЕНИЕ ====================

        Private Sub FillRow(rowIndex As Integer, data As TableRowData)
            If Not RowCellsMap.ContainsKey(rowIndex) Then Return

            Dim rowCells = RowCellsMap(rowIndex)

            Dim lists As List(Of Otkaz)() = {
            data.Complex1List, data.Complex2List, data.Complex3List, data.Complex13List,
            data.T1List, data.T2List, data.T3List, data.T13List,
            data.TR1List, data.TR2List, data.TR3List, data.TR13List,
            data.SLD1List, data.SLD2List, data.SLD3List, data.SLD13List,
            data.Factory1List, data.Factory2List, data.Factory3List, data.Factory13List
        }

            For i As Integer = 0 To Math.Min(rowCells.Length - 1, lists.Length - 1)
                Dim list = lists(i)

                rowCells(i).Text = If(list IsNot Nothing AndAlso list.Count > 0, list.Count.ToString(), "")
                'rowCells(i).Text = If(list?.Count.ToString(), "")

                rowCells(i).Tag = list
                SetClickableStyle(rowCells(i), list)
            Next
        End Sub

        Private Sub FillRowMerged(rowIndex As Integer, data As TableRowData, Optional allNormal As Boolean = False)


            If Not RowCellsMap.ContainsKey(rowIndex) Then Return

            Dim rowCells = RowCellsMap(rowIndex)

            Dim lists As List(Of Otkaz)() = {
                data.Complex12List, data.Complex3List, data.Complex13List,
                data.T12List, data.T3List, data.T13List,
                data.TR12List, data.TR3List, data.TR13List,
                data.SLD12List, data.SLD3List, data.SLD13List,
                data.Factory12List, data.Factory3List, data.Factory13List
            }

            ' Индексы, где НЕ нужен Tag (3кат и 1-3кат)
            Dim noTagIndices As New HashSet(Of Integer) From {1, 2, 4, 5, 7, 8, 10, 11, 13, 14}

            For i As Integer = 0 To Math.Min(rowCells.Length - 1, lists.Length - 1)
                Dim list = lists(i)

                rowCells(i).Text = If(list IsNot Nothing AndAlso list.Count > 0, list.Count.ToString(), "")
                'rowCells(i).Text = If(list?.Count.ToString(), "")

                If allNormal Then
                    ' ВСЕ ячейки - обычные, без drill-down
                    rowCells(i).Tag = Nothing
                    rowCells(i).Cursor = Cursors.Arrow
                    rowCells(i).FontWeight = FontWeights.Normal
                    rowCells(i).Foreground = Brushes.Black
                ElseIf noTagIndices.Contains(i) Then
                    ' Для 3кат и 1-3кат — без drill-down
                    rowCells(i).Tag = Nothing
                    rowCells(i).Cursor = Cursors.Arrow
                    rowCells(i).FontWeight = FontWeights.Normal
                    rowCells(i).Foreground = Brushes.Black
                Else
                    ' Для 1+2кат — с drill-down
                    rowCells(i).Tag = list
                    SetClickableStyle(rowCells(i), list)
                End If
            Next

        End Sub

        ' ==================== СТИЛИ ====================

        Private Sub SetClickableStyle(tb As TextBlock, list As List(Of Otkaz))
            If list IsNot Nothing AndAlso list.Count > 0 Then
                tb.Cursor = Cursors.Hand
                tb.FontWeight = FontWeights.Bold
                tb.Foreground = Brushes.DarkBlue
            Else
                tb.Cursor = Cursors.Arrow
                tb.FontWeight = FontWeights.Normal
                tb.Foreground = Brushes.Black
            End If
        End Sub

        ' ==================== ПОСТРОЕНИЕ ДАННЫХ ====================

        Private Function BuildTableRowData(
        otkazy As List(Of Otkaz),
        tPred As Func(Of Otkaz, Boolean),
        trPred As Func(Of Otkaz, Boolean),
        sldPred As Func(Of Otkaz, Boolean),
        factoryPred As Func(Of Otkaz, Boolean),
        cat1Pred As Func(Of Otkaz, Boolean),
        cat2Pred As Func(Of Otkaz, Boolean),
        cat3Pred As Func(Of Otkaz, Boolean)
    ) As TableRowData

            Dim data As New TableRowData()

            Dim tList = otkazy.Where(tPred).ToList()
            data.T1List = tList.Where(cat1Pred).ToList()
            data.T2List = tList.Where(cat2Pred).ToList()
            data.T3List = tList.Where(cat3Pred).ToList()

            Dim trList = otkazy.Where(trPred).ToList()
            data.TR1List = trList.Where(cat1Pred).ToList()
            data.TR2List = trList.Where(cat2Pred).ToList()
            data.TR3List = trList.Where(cat3Pred).ToList()

            Dim sldList = otkazy.Where(sldPred).ToList()
            data.SLD1List = sldList.Where(cat1Pred).ToList()
            data.SLD2List = sldList.Where(cat2Pred).ToList()
            data.SLD3List = sldList.Where(cat3Pred).ToList()

            Dim fList = otkazy.Where(factoryPred).ToList()
            data.Factory1List = fList.Where(cat1Pred).ToList()
            data.Factory2List = fList.Where(cat2Pred).ToList()
            data.Factory3List = fList.Where(cat3Pred).ToList()

            Return data
        End Function



        ' Список ячеек текущей активной группы для подсветки
        Private _highlightGroup As New List(Of TextBlock)()
        ' Словарь: TextBlock -> индекс группы в HighlightGroups (для мгновенного поиска)
        Private _cellToGroupIndex As New Dictionary(Of TextBlock, Integer)()

        ' Цвет подсветки (серый, полупрозрачный)
        Private ReadOnly GroupHighlightBrush As New SolidColorBrush(Color.FromArgb(60, 128, 128, 128))
        Private ReadOnly DefaultBrush As New SolidColorBrush(Colors.Transparent)

        ' Описание групп: (StartRow, StartCol, EndRow, EndCol) - КООРДИНАТЫ GRID
        Private ReadOnly HighlightGroups As Integer(,) = {
    {2, 6, 3, 8},   ' Т
    {2, 10, 3, 12}, ' ТР
    {2, 14, 3, 16}, ' СЛД
    {2, 18, 3, 20}  ' Заводы
}

        ' Добавь поле для хранения границ текущей активной группы
        Private _currentGroupBounds As Rect?


        Private Sub InitGroupHighlighting()


            _highlightGroup.Clear()
            _cellToGroupIndex.Clear()
            _currentGroupBounds = Nothing

            RemoveHandler MainGrid.MouseLeave, AddressOf MainGrid_MouseLeave
            AddHandler MainGrid.MouseLeave, AddressOf MainGrid_MouseLeave

            RemoveHandler MainGrid.MouseMove, AddressOf MainGrid_MouseMove
            AddHandler MainGrid.MouseMove, AddressOf MainGrid_MouseMove

            For g As Integer = 0 To HighlightGroups.GetUpperBound(0)
                Dim gridR1 = HighlightGroups(g, 0)
                Dim gridC1 = HighlightGroups(g, 1)
                Dim gridR2 = HighlightGroups(g, 2)
                Dim gridC2 = HighlightGroups(g, 3)

                Dim mapR1 = gridR1 + 1
                Dim mapR2 = gridR2 + 1
                Dim mapC1 = gridC1 - 2
                Dim mapC2 = gridC2 - 2

                Dim groupCells As New List(Of TextBlock)()

                For mapRow As Integer = mapR1 To mapR2
                    If RowCellsMap.ContainsKey(mapRow) Then
                        Dim cellsInRow = RowCellsMap(mapRow)
                        For idx As Integer = mapC1 To mapC2
                            If idx >= 0 AndAlso idx < cellsInRow.Length Then
                                Dim tb = cellsInRow(idx)
                                If tb IsNot Nothing Then
                                    If Not _cellToGroupIndex.ContainsKey(tb) Then
                                        tb.Background = Brushes.Transparent
                                        AddHandler tb.MouseEnter, AddressOf Group_MouseEnter
                                        AddHandler tb.MouseLeave, AddressOf Group_MouseLeave
                                        AddHandler tb.MouseRightButtonDown, AddressOf CopyGroupToClipboard_Click
                                    End If

                                    groupCells.Add(tb)
                                    _cellToGroupIndex(tb) = g
                                End If
                            End If
                        Next
                    End If
                Next
            Next





            '_highlightGroup.Clear()
            '_cellToGroupIndex.Clear()

            '' === ДОБАВИТЬ: Подписка на уход мыши со ВСЕГО Grid'а ===
            'AddHandler MainGrid.MouseLeave, AddressOf MainGrid_MouseLeave
            '' =======================================================

            'For g As Integer = 0 To HighlightGroups.GetUpperBound(0)
            '    Dim gridR1 = HighlightGroups(g, 0)
            '    Dim gridC1 = HighlightGroups(g, 1)
            '    Dim gridR2 = HighlightGroups(g, 2)
            '    Dim gridC2 = HighlightGroups(g, 3)

            '    ' Перевод координат Grid в индексы RowCellsMap
            '    Dim mapR1 = gridR1 + 1
            '    Dim mapR2 = gridR2 + 1
            '    Dim mapC1 = gridC1 - 2
            '    Dim mapC2 = gridC2 - 2

            '    Dim groupCells As New List(Of TextBlock)()

            '    For mapRow As Integer = mapR1 To mapR2
            '        If RowCellsMap.ContainsKey(mapRow) Then
            '            Dim cellsInRow = RowCellsMap(mapRow)
            '            For idx As Integer = mapC1 To mapC2
            '                If idx >= 0 AndAlso idx < cellsInRow.Length Then
            '                    Dim tb = cellsInRow(idx)
            '                    If tb IsNot Nothing Then
            '                        ' Защита от дубликатов
            '                        If Not _cellToGroupIndex.ContainsKey(tb) Then
            '                            tb.Background = Brushes.Transparent
            '                            AddHandler tb.MouseEnter, AddressOf Group_MouseEnter
            '                            AddHandler tb.MouseLeave, AddressOf Group_MouseLeave
            '                            AddHandler tb.MouseRightButtonDown, AddressOf CopyGroupToClipboard_Click



            '                            ' =========================================================

            '                        End If

            '                        groupCells.Add(tb)
            '                        _cellToGroupIndex(tb) = g ' Запоминаем ИНДЕКС ГРУППЫ
            '                    End If
            '                End If
            '            Next
            '        End If
            '    Next
            'Next
        End Sub

        ''' <summary>
        ''' При уходе мыши за пределы ВСЕЙ таблицы - принудительно гасим подсветку
        ''' </summary>
        Private Sub MainGrid_MouseLeave(sender As Object, e As MouseEventArgs)
            If _highlightGroup.Count = 0 Then Return

            For Each cell In _highlightGroup
                cell.Background = DefaultBrush
            Next
            _highlightGroup.Clear()
            _currentGroupBounds = Nothing




            'If _highlightGroup.Count = 0 Then Return

            'For Each cell In _highlightGroup
            '    cell.Background = DefaultBrush
            'Next
            '_highlightGroup.Clear()
        End Sub

        ''' <summary>
        ''' При движении мыши по Grid'у: если курсор не над ячейкой активной группы - гасим подсветку
        ''' </summary>
        Private Sub MainGrid_MouseMove(sender As Object, e As MouseEventArgs)

            If _highlightGroup.Count = 0 OrElse Not _currentGroupBounds.HasValue Then Return

            Dim mousePos = Mouse.GetPosition(MainGrid)

            ' Если курсор ВЫШЕЛ за пределы кэшированного прямоугольника группы - гасим
            If Not _currentGroupBounds.Value.Contains(mousePos) Then
                For Each cell In _highlightGroup
                    cell.Background = DefaultBrush
                Next
                _highlightGroup.Clear()
                _currentGroupBounds = Nothing
            End If


            'If _highlightGroup.Count = 0 Then Return

            'Dim tb = TryCast(e.OriginalSource, TextBlock)

            '' Если мышь не над TextBlock ИЛИ этот TextBlock не входит в текущую подсвеченную группу - гасим
            'If tb Is Nothing OrElse Not _highlightGroup.Contains(tb) Then
            '    For Each cell In _highlightGroup
            '        cell.Background = DefaultBrush
            '    Next
            '    _highlightGroup.Clear()
            'End If
        End Sub


        ''' <summary>
        ''' При наведении - подсвечиваем группу ПО ИНДЕКСУ ИЗ МАССИВА
        ''' </summary>
        Private Sub Group_MouseEnter(sender As Object, e As MouseEventArgs)


            Dim tb = TryCast(sender, TextBlock)
            If tb Is Nothing Then Return

            Dim groupIdx As Integer = -1
            If _cellToGroupIndex.TryGetValue(tb, groupIdx) Then
                ' Гасим предыдущую подсветку
                For Each cell In _highlightGroup
                    cell.Background = DefaultBrush
                Next

                ' Собираем ячейки группы
                _highlightGroup.Clear()
                Dim r1 = HighlightGroups(groupIdx, 0) + 1
                Dim c1 = HighlightGroups(groupIdx, 1) - 2
                Dim r2 = HighlightGroups(groupIdx, 2) + 1
                Dim c2 = HighlightGroups(groupIdx, 3) - 2

                For mapRow As Integer = r1 To r2
                    If RowCellsMap.ContainsKey(mapRow) Then
                        Dim cellsInRow = RowCellsMap(mapRow)
                        For idx As Integer = c1 To c2
                            If idx >= 0 AndAlso idx < cellsInRow.Length Then
                                Dim cellTb = cellsInRow(idx)
                                If cellTb IsNot Nothing Then
                                    cellTb.Background = GroupHighlightBrush
                                    _highlightGroup.Add(cellTb)
                                End If
                            End If
                        Next
                    End If
                Next

                ' === КАЛИБРУЕМ ГРАНИЦЫ ПРЯМО ЗДЕСЬ ===
                If _highlightGroup.Count > 0 Then
                    Dim firstCell = _highlightGroup(0)
                    Dim lastCell = _highlightGroup(_highlightGroup.Count - 1)
                    Dim parentFirst = TryCast(firstCell.Parent, Border)
                    Dim parentLast = TryCast(lastCell.Parent, Border)

                    If parentFirst IsNot Nothing AndAlso parentLast IsNot Nothing Then
                        Dim topLeft = parentFirst.TranslatePoint(New Point(0, 0), MainGrid)
                        Dim bottomRight = parentLast.TranslatePoint(
                            New Point(parentLast.ActualWidth, parentLast.ActualHeight), MainGrid)
                        _currentGroupBounds = New Rect(topLeft, bottomRight)
                    End If
                End If
                ' ======================================
            End If






            'Dim tb = TryCast(sender, TextBlock)
            'If tb Is Nothing Then Return

            'Dim groupIdx As Integer = -1
            'If _cellToGroupIndex.TryGetValue(tb, groupIdx) Then
            '    ' Гасим предыдущую подсветку
            '    For Each cell In _highlightGroup
            '        cell.Background = DefaultBrush
            '    Next

            '    ' Собираем ячейки заново по индексу группы (гарантированно правильный набор)
            '    _highlightGroup.Clear()
            '    Dim r1 = HighlightGroups(groupIdx, 0) + 1
            '    Dim c1 = HighlightGroups(groupIdx, 1) - 2
            '    Dim r2 = HighlightGroups(groupIdx, 2) + 1
            '    Dim c2 = HighlightGroups(groupIdx, 3) - 2

            '    For mapRow As Integer = r1 To r2
            '        If RowCellsMap.ContainsKey(mapRow) Then
            '            Dim cellsInRow = RowCellsMap(mapRow)
            '            For idx As Integer = c1 To c2
            '                If idx >= 0 AndAlso idx < cellsInRow.Length Then
            '                    Dim cellTb = cellsInRow(idx)
            '                    If cellTb IsNot Nothing Then
            '                        cellTb.Background = GroupHighlightBrush
            '                        _highlightGroup.Add(cellTb)
            '                    End If
            '                End If
            '            Next
            '        End If
            '    Next
            'End If
        End Sub

        ''' <summary>
        ''' При уходе мыши - БЕЗОПАСНАЯ очистка без обращений к пустым коллекциям
        ''' </summary>
        Private Sub Group_MouseLeave(sender As Object, e As MouseEventArgs)
            ' ПРОВЕРКА НА ПУСТОТУ ПЕРЕД ЛЮБЫМИ ДЕЙСТВИЯМИ
            If _highlightGroup.Count = 0 Then Return

            Dim mousePos = Mouse.GetPosition(MainGrid)

            ' Берем границы из МАССИВА, а не из коллекции
            ' Находим индекс текущей группы через любую ячейку
            Dim firstCell = _highlightGroup(0) ' Безопасно, т.к. проверили Count > 0
            Dim groupIdx As Integer = -1
            If Not _cellToGroupIndex.TryGetValue(firstCell, groupIdx) Then Return

            Dim gridR1 = HighlightGroups(groupIdx, 0)
            Dim gridC1 = HighlightGroups(groupIdx, 1)
            Dim gridR2 = HighlightGroups(groupIdx, 2)
            Dim gridC2 = HighlightGroups(groupIdx, 3)

            ' Находим реальные Border'ы для расчета границ (используем Map-индексы)
            Dim mapR1 = gridR1 + 1
            Dim mapC1 = gridC1 - 2
            Dim mapR2 = gridR2 + 1
            Dim mapC2 = gridC2 - 2

            Dim topLeftBorder As Border = Nothing
            Dim bottomRightBorder As Border = Nothing

            If RowCellsMap.ContainsKey(mapR1) AndAlso RowCellsMap.ContainsKey(mapR2) Then
                Dim row1Cells = RowCellsMap(mapR1)
                Dim row2Cells = RowCellsMap(mapR2)

                If mapC1 >= 0 AndAlso mapC1 < row1Cells.Length Then
                    topLeftBorder = TryCast(row1Cells(mapC1).Parent, Border)
                End If
                If mapC2 >= 0 AndAlso mapC2 < row2Cells.Length Then
                    bottomRightBorder = TryCast(row2Cells(mapC2).Parent, Border)
                End If
            End If

            If topLeftBorder IsNot Nothing AndAlso bottomRightBorder IsNot Nothing Then
                Dim topLeft = topLeftBorder.TranslatePoint(New Point(0, 0), MainGrid)
                Dim bottomRight = bottomRightBorder.TranslatePoint(
            New Point(bottomRightBorder.ActualWidth, bottomRightBorder.ActualHeight), MainGrid)

                Dim blockRect = New Rect(topLeft, bottomRight)

                ' Красим обратно ТОЛЬКО если курсор реально покинул блок
                If Not blockRect.Contains(mousePos) Then
                    For Each cell In _highlightGroup
                        cell.Background = DefaultBrush
                    Next
                    _highlightGroup.Clear()
                End If
            Else
                ' Если границы не удалось получить - просто гасим подсветку
                For Each cell In _highlightGroup
                    cell.Background = DefaultBrush
                Next
                _highlightGroup.Clear()
            End If
        End Sub

        ''' <summary>
        ''' Копирует ТЕКУЩУЮ ПОДСВЕЧЕННУЮ ГРУППУ в буфер обмена
        ''' Формат: TSV (табуляция между столбцами, перенос между строками)
        ''' </summary>
        Private Sub CopyGroupToClipboard_Click(sender As Object, e As MouseButtonEventArgs)
            If _highlightGroup.Count = 0 Then
                MessageBox.Show("Наведите мышь на группу данных (Комплекс, Т, ТР...) перед копированием!",
                        "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning)
                e.Handled = True
                Return
            End If

            Dim sb As New System.Text.StringBuilder()

            ' Сортируем ячейки: сначала по строке, потом по колонке
            Dim sortedCells = _highlightGroup.OrderBy(Function(c)
                                                          Dim b = TryCast(c.Parent, Border)
                                                          If b IsNot Nothing Then
                                                              Return Grid.GetRow(b) * 100 + Grid.GetColumn(b)
                                                          End If
                                                          Return 0
                                                      End Function).ToList()

            ' Определяем границы группы для правильного форматирования
            Dim minRow = sortedCells.Min(Function(c) Grid.GetRow(TryCast(c.Parent, Border)))
            Dim maxRow = sortedCells.Max(Function(c) Grid.GetRow(TryCast(c.Parent, Border)))
            Dim minCol = sortedCells.Min(Function(c) Grid.GetColumn(TryCast(c.Parent, Border)))
            Dim maxCol = sortedCells.Max(Function(c) Grid.GetColumn(TryCast(c.Parent, Border)))

            ' Формируем TSV матрицу
            For r As Integer = minRow To maxRow
                Dim rowStarted As Boolean = False
                For c As Integer = minCol To maxCol
                    ' Находим ячейку в отсортированном списке
                    Dim cell = sortedCells.FirstOrDefault(Function(x)
                                                              Dim b = TryCast(x.Parent, Border)
                                                              Return b IsNot Nothing AndAlso
                                                             Grid.GetRow(b) = r AndAlso
                                                             Grid.GetColumn(b) = c
                                                          End Function)

                    Dim text As String = If(cell IsNot Nothing, cell.Text, "")

                    If rowStarted Then sb.Append(vbTab)
                    sb.Append(text)
                    rowStarted = True
                Next
                If r < maxRow Then sb.Append(vbCrLf)
            Next

            Clipboard.SetText(sb.ToString())
            e.Handled = True
        End Sub


        ' ==================== ЗАГРУЗКА ====================

        Private Sub S24Table1_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
            If _currentYearOtkazy Is Nothing OrElse _previousYearOtkazy Is Nothing Then
                Return
            End If
            UpdateHeaders()
            InitializeRowCells()

            Dim isCat1 As Func(Of Otkaz, Boolean) = Function(o) o.Kat = 1
            Dim isCat2 As Func(Of Otkaz, Boolean) = Function(o) o.Kat = 2
            Dim isCat3 As Func(Of Otkaz, Boolean) = Function(o) o.Kat = 3

            ' ▼▼▼ ОБЪЕДИНЁННЫЙ ПРЕДИКАТ ПО Т ▼▼▼
            Dim tchCombined As Func(Of Otkaz, Boolean) = Function(o) SignedOnTCH(o) OrElse VRassForTCH(o)
            ' ▲▲▲ ============ ▲▲▲

            ' Текущий год: строки 3 (полная) и 5 (merged)
            Dim rowCurrent = BuildTableRowData(
            _currentYearOtkazy,
             tchCombined, SignedOnTRPU, SignedOnSLD, SignedOnZav,
            isCat1, isCat2, isCat3)
            FillRow(3, rowCurrent)
            FillRowMerged(5, rowCurrent)

            ' Прошлый год: строки 4 (полная) и 6 (merged)
            Dim rowPrevious = BuildTableRowData(
            _previousYearOtkazy,
            tchCombined, SignedOnTRPU, SignedOnSLD, SignedOnZav,
            isCat1, isCat2, isCat3)
            FillRow(4, rowPrevious)
            FillRowMerged(6, rowPrevious)



            ' ▼▼▼ Строка 7: Целевые показатели ▼▼▼
            If _periodEnd IsNot Nothing Then
                Dim startDate As Date = If(_periodStart, _periodEnd).Value
                Dim endDate As Date = _periodEnd.Value

                If startDate > endDate Then
                    Dim tmp As Date = startDate
                    startDate = endDate
                    endDate = tmp
                End If

                Dim allGoals = GoalsStore.Load()
                Dim goalForPeriod = GetGoalForPeriod(allGoals, startDate, endDate)

                FillRowGoal(7, goalForPeriod)
            Else
                FillRowGoal(7, New MonthlyGoal())
            End If
            ' Строка 8: Изменения случ. (абсолютная разница)
            Dim changesData = CalculateChanges(rowCurrent, rowPrevious)
            FillRowNumeric(8, changesData)

            ' Строка 9: % изменений (процентная разница)
            Dim percentData = CalculatePercentChanges(rowCurrent, rowPrevious)
            FillRowNumeric(9, percentData)

            ' ▼▼▼ Строка 10 - полный месяц прошлого года ▼▼▼
            If _periodEnd IsNot Nothing AndAlso _previousYearOtkazyRaw IsNot Nothing Then
                Dim oldP = GetPrevPeriod(If(_periodStart, _periodEnd).Value.Date,
                             _periodEnd.Value.Date,
                             _previousYearOtkazyRaw)

                Dim actualPrevYear As Integer = oldP.End.Year
                Dim targetMon As Integer = oldP.End.Month        ' ← февраль из ручного периода!

                Dim monthStart As New Date(actualPrevYear, targetMon, 1)
                Dim monthEnd As Date = monthStart.AddMonths(1).AddDays(-1)

                Dim fullMonthOtkazy = _previousYearOtkazyRaw.Where(Function(o) o.Nach.Year = actualPrevYear AndAlso
                                                                  o.Nach.Date >= monthStart.Date AndAlso
                                                                  o.Nach.Date <= monthEnd.Date).ToList()

                Dim rowFullMonth = BuildTableRowData(fullMonthOtkazy, tchCombined, SignedOnTRPU, SignedOnSLD, SignedOnZav, isCat1, isCat2, isCat3)
                FillRowMerged(10, rowFullMonth, allNormal:=True)
            End If
            ' ▼▼▼ ИНИЦИАЛИЗАЦИЯ ПОДСВЕТКИ ГРУПП (В САМОМ КОНЦЕ!) ▼▼▼
            InitGroupHighlighting()
        End Sub


        ''' <summary>
        ''' Возвращает суммарную цель за все месяцы, попавшие в период startDate...endDate
        ''' </summary>
        Private Function GetGoalForPeriod(
    goals As List(Of MonthlyGoal),
    startDate As Date,
    endDate As Date
) As MonthlyGoal

            Dim result As New MonthlyGoal()

            If goals Is Nothing Then
                Return result
            End If

            Dim currentMonth As New Date(startDate.Year, startDate.Month, 1)
            Dim lastMonth As New Date(endDate.Year, endDate.Month, 1)

            While currentMonth <= lastMonth
                Dim y As Integer = currentMonth.Year
                Dim m As Integer = currentMonth.Month

                Dim monthGoals = goals.Where(Function(g) g.Year = y AndAlso g.Month = m).ToList()

                For Each goal In monthGoals
                    'result.Complex12 += goal.Complex12
                    'result.Complex3 += goal.Complex3
                    'result.Complex13 += goal.Complex13

                    result.T12 += goal.T12
                    result.T3 += goal.T3
                    'result.T13 += goal.T13

                    result.TR12 += goal.TR12
                    result.TR3 += goal.TR3
                    'result.TR13 += goal.TR13

                    result.SLD12 += goal.SLD12
                    result.SLD3 += goal.SLD3
                    'result.SLD13 += goal.SLD13

                    result.Factory12 += goal.Factory12
                    result.Factory3 += goal.Factory3
                    'result.Factory13 += goal.Factory13
                Next

                currentMonth = currentMonth.AddMonths(1)
            End While

            Return result
        End Function



        ' ==================== ОБРАБОТЧИКИ ====================

        Private Sub DataCell_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
            Dim tb = TryCast(sender, TextBlock)
            If tb Is Nothing Then Return

            Dim clickedOtkazy = TryCast(tb.Tag, List(Of Otkaz))
            If clickedOtkazy IsNot Nothing AndAlso clickedOtkazy.Count > 0 Then
                Dim ids = String.Join(",", clickedOtkazy.Select(Function(o) o.Id))
                Clipboard.SetText(ids)
                MW.ShowOtkazyList(clickedOtkazy.OrderBy(Function(o) o.Nach))
            End If

            e.Handled = True
        End Sub


        ' ==================== ВЫЧИСЛЕНИЕ ИЗМЕНЕНИЙ ====================

        ' ==================== ВЫЧИСЛЕНИЕ ИЗМЕНЕНИЙ ====================

        ''' <summary>
        ''' Абсолютная разница (строка 8: "Изменения случ.")
        ''' </summary>
        Private Function CalculateChanges(current As TableRowData, previous As TableRowData) As TableRowNumericData
            Dim data As New TableRowNumericData()

            ' Комплекс
            data.Complex12 = FormatChange(current.Complex12List?.Count, previous.Complex12List?.Count)
            data.Complex3 = FormatChange(current.Complex3List?.Count, previous.Complex3List?.Count)
            data.Complex13 = FormatChange(current.Complex13List?.Count, previous.Complex13List?.Count)

            ' Т
            data.T12 = FormatChange(current.T12List?.Count, previous.T12List?.Count)
            data.T3 = FormatChange(current.T3List?.Count, previous.T3List?.Count)
            data.T13 = FormatChange(current.T13List?.Count, previous.T13List?.Count)

            ' ТР
            data.TR12 = FormatChange(current.TR12List?.Count, previous.TR12List?.Count)
            data.TR3 = FormatChange(current.TR3List?.Count, previous.TR3List?.Count)
            data.TR13 = FormatChange(current.TR13List?.Count, previous.TR13List?.Count)

            ' СЛД
            data.SLD12 = FormatChange(current.SLD12List?.Count, previous.SLD12List?.Count)
            data.SLD3 = FormatChange(current.SLD3List?.Count, previous.SLD3List?.Count)
            data.SLD13 = FormatChange(current.SLD13List?.Count, previous.SLD13List?.Count)

            ' Заводы
            data.Factory12 = FormatChange(current.Factory12List?.Count, previous.Factory12List?.Count)
            data.Factory3 = FormatChange(current.Factory3List?.Count, previous.Factory3List?.Count)
            data.Factory13 = FormatChange(current.Factory13List?.Count, previous.Factory13List?.Count)

            Return data
        End Function

        ''' <summary>
        ''' Процентная разница (строка 9: "% изменений")
        ''' </summary>
        Private Function CalculatePercentChanges(current As TableRowData, previous As TableRowData) As TableRowNumericData
            Dim data As New TableRowNumericData()

            ' Комплекс
            data.Complex12 = FormatPercentChange(current.Complex12List?.Count, previous.Complex12List?.Count)
            data.Complex3 = FormatPercentChange(current.Complex3List?.Count, previous.Complex3List?.Count)
            data.Complex13 = FormatPercentChange(current.Complex13List?.Count, previous.Complex13List?.Count)

            ' Т
            data.T12 = FormatPercentChange(current.T12List?.Count, previous.T12List?.Count)
            data.T3 = FormatPercentChange(current.T3List?.Count, previous.T3List?.Count)
            data.T13 = FormatPercentChange(current.T13List?.Count, previous.T13List?.Count)

            ' ТР
            data.TR12 = FormatPercentChange(current.TR12List?.Count, previous.TR12List?.Count)
            data.TR3 = FormatPercentChange(current.TR3List?.Count, previous.TR3List?.Count)
            data.TR13 = FormatPercentChange(current.TR13List?.Count, previous.TR13List?.Count)

            ' СЛД
            data.SLD12 = FormatPercentChange(current.SLD12List?.Count, previous.SLD12List?.Count)
            data.SLD3 = FormatPercentChange(current.SLD3List?.Count, previous.SLD3List?.Count)
            data.SLD13 = FormatPercentChange(current.SLD13List?.Count, previous.SLD13List?.Count)

            ' Заводы
            data.Factory12 = FormatPercentChange(current.Factory12List?.Count, previous.Factory12List?.Count)
            data.Factory3 = FormatPercentChange(current.Factory3List?.Count, previous.Factory3List?.Count)
            data.Factory13 = FormatPercentChange(current.Factory13List?.Count, previous.Factory13List?.Count)

            Return data
        End Function

        ' ==================== ФОРМАТИРОВАНИЕ ====================

        ''' <summary>
        ''' Форматирует абсолютную разницу: "+3", "-2", "0"
        ''' </summary>
        Private Function FormatChange(currentCount As Integer?, previousCount As Integer?) As String
            Dim curr = If(currentCount, 0)
            Dim prev = If(previousCount, 0)
            Dim diff = curr - prev

            If diff > 0 Then
                Return "+" & diff.ToString()
            ElseIf diff < 0 Then
                Return diff.ToString()
            Else
                Return "0"
            End If
        End Function

        ''' <summary>
        ''' Форматирует процентную разницу: "+42.86%", "-15.38%", "0%", "N/A"
        ''' </summary>
        Private Function FormatPercentChange(currentCount As Integer?, previousCount As Integer?) As String
            Dim curr = If(currentCount, 0)
            Dim prev = If(previousCount, 0)

            If prev = 0 Then
                If curr = 0 Then
                    Return "0%"
                Else
                    Return "+100%"  ' было "N/A"
                End If
            End If

            Dim percent = (curr - prev) / prev * 100

            If percent > 0 Then
                If percent > 100 Then
                    Return $"в +{Math.Round(curr / prev, 1)}р"
                Else
                    Return $"+ {Math.Round(percent, 1).ToString("F1")}%"
                End If

            ElseIf percent < 0 Then

                Return $"{Math.Round(percent, 1).ToString("F1")}%" 'percent.ToString("F1") & "%"
            Else
                Return "0%"
            End If

        End Function

        ' ==================== ЗАПОЛНЕНИЕ ЧИСЛОВЫХ СТРОК ====================

        ''' <summary>
        ''' Заполняет строку числовыми значениями (без drill-down)
        ''' </summary>
        Private Sub FillRowNumeric(rowIndex As Integer, data As TableRowNumericData)

            If Not RowCellsMap.ContainsKey(rowIndex) Then Return

            Dim rowCells = RowCellsMap(rowIndex)

            Dim values As String() = {
                data.Complex12, data.Complex3, data.Complex13,
                data.T12, data.T3, data.T13,
                data.TR12, data.TR3, data.TR13,
                data.SLD12, data.SLD3, data.SLD13,
                data.Factory12, data.Factory3, data.Factory13
            }

            For i As Integer = 0 To Math.Min(rowCells.Length - 1, values.Length - 1)
                Dim value = values(i)
                rowCells(i).Text = value
                rowCells(i).Tag = Nothing
                rowCells(i).Cursor = Cursors.Arrow
                rowCells(i).FontWeight = FontWeights.Normal
                rowCells(i).Foreground = GetColorForChange(value)
            Next


        End Sub

        ''' <summary>
        ''' Возвращает цвет для значения изменения:
        ''' "+" → красный, "-" → зелёный, иначе → чёрный
        ''' </summary>
        Private Function GetColorForChange(value As String) As Brush
            If String.IsNullOrEmpty(value) Then Return Brushes.Black

            If value.Contains("+") Then
                Return Brushes.Red
            ElseIf value.StartsWith("-") Then
                Return Brushes.Green
            Else
                Return Brushes.Black
            End If
        End Function

        Private Sub UpdateHeaders()

            If _periodEnd Is Nothing Then Return

            Dim currentYear As Integer = _periodEnd.Value.Year

            ' ▼▼▼ прошлый период — ручной или авто, один источник правды ▼▼▼
            Dim oldP = GetPrevPeriod(If(_periodStart, _periodEnd).Value.Date,
                                     _periodEnd.Value.Date,
                                     _previousYearOtkazyRaw)

            LblRow2Period.Text = $"{_periodStart.Value:dd.MM} - {_periodEnd.Value:dd.MM.yy}"
            LblRow2.Text = currentYear.ToString()
            LblRow3.Text = oldP.End.Year.ToString()

            LblRow4.Text = currentYear.ToString()
            LblRow5.Text = oldP.End.Year.ToString()
            LblGoal.Text = "Цель"

            LblFullPeriodPrev.Text = $"{oldP.Start:dd.MM} - {oldP.End:dd.MM.yy}"

        End Sub

        ''' <summary>
        ''' Заполняет строку целевыми показателями
        ''' </summary>
        Private Sub FillRowGoal(rowIndex As Integer, goal As MonthlyGoal)
            If Not RowCellsMap.ContainsKey(rowIndex) Then Return

            Dim rowCells = RowCellsMap(rowIndex)

            Dim values As String() = {
                goal.Complex12.ToString(), goal.Complex3.ToString(), goal.Complex13.ToString(),
                goal.T12.ToString(), goal.T3.ToString(), goal.T13.ToString(),
                goal.TR12.ToString(), goal.TR3.ToString(), goal.TR13.ToString(),
                goal.SLD12.ToString(), goal.SLD3.ToString(), goal.SLD13.ToString(),
                goal.Factory12.ToString(), goal.Factory3.ToString(), goal.Factory13.ToString()
            }

            For i As Integer = 0 To Math.Min(rowCells.Length - 1, values.Length - 1)
                rowCells(i).Text = If(values(i) = "0", "", values(i))
                rowCells(i).Tag = Nothing
                rowCells(i).Cursor = Cursors.Arrow

            Next
        End Sub

    End Class









End Namespace

