Imports System.Globalization
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media

Namespace Kas

    Partial Public Class S24Table9

        Private _cur As List(Of Otkaz)
        Private _periodEnd As Date?
        Private Shared ReadOnly Ru As CultureInfo = CultureInfo.GetCultureInfo("ru-RU")
        Private Shared ReadOnly Komplexes As Integer() = {1, 2, 3, 5, 7}

        Public Sub New(currentYearOtkazy As List(Of Otkaz),
                       Optional periodStart As Date? = Nothing,
                       Optional periodEnd As Date? = Nothing)
            InitializeComponent()
            _periodEnd = periodEnd

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

        Private Sub S24Table9_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
            Try
                If _periodEnd.HasValue Then
                    Dim monthName = Ru.DateTimeFormat.GetMonthName(_periodEnd.Value.Month)
                    LblCaption.Text = $"ОТС 1 кат. (Т/СЛД) на КРАС"
                End If

                BuildAndFillRows()
            Catch ex As Exception
                MessageBox.Show(ex.ToString(), "Ошибка в S24Table9")
            End Try
        End Sub

        Private Sub BuildAndFillRows()

            MainGrid.RowDefinitions.Add(New RowDefinition With {.Height = GridLength.Auto})

            Dim totalT As Integer = 0
            Dim totalSLD As Integer = 0

            For i As Integer = 0 To Komplexes.Length - 1
                Dim k As Integer = Komplexes(i)

                MainGrid.RowDefinitions.Add(New RowDefinition With {.Height = GridLength.Auto})
                Dim rowIdx As Integer = MainGrid.RowDefinitions.Count - 1

                AddCell(rowIdx, 0, Otkaz.GetTCHE_Name(k), "HeaderBorder", "HeaderTextStyle", True)

                Dim tCount = CountTch(k)
                Dim sldCount = CountSld(k)

                Dim tText As String = If(tCount > 0, tCount.ToString(), "")
                Dim sldText As String = If(sldCount > 0, sldCount.ToString(), "")

                AddCell(rowIdx, 1, tText, "CellBorder", "CellStyle")
                AddCell(rowIdx, 2, sldText, "CellBorder", "CellStyle")

                totalT += tCount
                totalSLD += sldCount
            Next

            MainGrid.RowDefinitions.Add(New RowDefinition With {.Height = GridLength.Auto})
            Dim totalRowIdx As Integer = MainGrid.RowDefinitions.Count - 1

            AddCell(totalRowIdx, 0, "Всего:", "TotalBorder", "HeaderTextStyle", True)
            AddCell(totalRowIdx, 1, totalT.ToString(), "TotalBorder", "CellStyle", isBold:=True)
            AddCell(totalRowIdx, 2, totalSLD.ToString(), "TotalBorder", "CellStyle", isBold:=True)
        End Sub

        ''' <summary>
        ''' ТЧЭ: отказы 1 кат в расследовании (VRassled) для данного комплекса
        ''' </summary>
        Private Function CountTch(komplex As Integer) As Integer
            Return _cur.Where(Function(o)
                                  Return o.KomplexAsInt = komplex AndAlso (o.VRassled OrElse o.ZaKem?.ToLower = "тч")
                              End Function).Count
        End Function

        ''' <summary>
        ''' СЛД: отказы 1 кат, отнесённые на СЛД данного комплекса
        ''' Ключи: слд1=Боготол(k=1), слд2=Красноярск(k=2), слд3=Иланская(k=3), слд5=Ачинск(k=5), слд7=Абакан(k=7)
        ''' </summary>
        Private Function CountSld(komplex As Integer) As Integer
            Dim sldKey As String = $"слд{komplex}"

            If Not Otkaz.SafeName.ContainsKey(sldKey) Then Return 0

            Dim sldFullName As String = Otkaz.SafeName(sldKey).ToLower()

            Return _cur.Where(Function(o)
                                  If o.ZaKem Is Nothing Then Return False
                                  Return o.ZaKem.ToLower().Contains(sldFullName)
                              End Function).Count()
        End Function

        ' ==================== SELECTION ====================

        Private _selectedCells As New List(Of TextBlock)()
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

            If row > 0 AndAlso Not text.StartsWith("Всего") Then
                t.Background = Brushes.Transparent

                AddHandler t.MouseEnter, AddressOf Cell_MouseEnterForSelection
                AddHandler t.MouseLeave, AddressOf Cell_MouseLeaveForSelection
                AddHandler t.PreviewMouseLeftButtonDown, AddressOf Cell_PreviewMouseDownForSelection
                AddHandler t.MouseRightButtonDown, AddressOf CopyColumnToClipboard_Click
            End If

            b.Child = t
            Grid.SetRow(b, row)
            Grid.SetColumn(b, col)
            MainGrid.Children.Add(b)
        End Sub

        Private Sub HighlightColumn(col As Integer)
            For Each cell In _selectedCells
                cell.Background = DefaultBrush
            Next
            _selectedCells.Clear()

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

            _selectedCells.Sort(Function(a, b)
                                    Dim rowA = Grid.GetRow(TryCast(a.Parent, Border))
                                    Dim rowB = Grid.GetRow(TryCast(b.Parent, Border))
                                    Return rowA.CompareTo(rowB)
                                End Function)
        End Sub

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

        Private Sub Cell_MouseEnterForSelection(sender As Object, e As MouseEventArgs)
            Dim tb = TryCast(sender, TextBlock)
            If tb Is Nothing Then Return

            Dim parentBorder = TryCast(tb.Parent, Border)
            If parentBorder Is Nothing Then Return

            Dim col = Grid.GetColumn(parentBorder)
            HighlightColumn(col)
        End Sub

        Private Sub Cell_MouseLeaveForSelection(sender As Object, e As MouseEventArgs)
            If _selectedCells.Count = 0 Then Return

            Dim mousePos = Mouse.GetPosition(MainGrid)

            Dim firstCell = _selectedCells.First()
            Dim lastCell = _selectedCells.Last()

            Dim parentFirst = TryCast(firstCell.Parent, Border)
            Dim parentLast = TryCast(lastCell.Parent, Border)

            If parentFirst Is Nothing OrElse parentLast Is Nothing Then Return

            Dim topLeft = parentFirst.TranslatePoint(New Point(0, 0), MainGrid)
            Dim bottomRight = parentLast.TranslatePoint(
                New Point(parentLast.ActualWidth, parentLast.ActualHeight), MainGrid)

            Dim blockRect = New Rect(topLeft, bottomRight)

            If Not blockRect.Contains(mousePos) Then
                For Each cell In _selectedCells
                    cell.Background = DefaultBrush
                Next
                _selectedCells.Clear()
            End If
        End Sub

        Private Sub Cell_PreviewMouseDownForSelection(sender As Object, e As MouseButtonEventArgs)
            Dim tb = TryCast(sender, TextBlock)
            If tb Is Nothing Then Return

            Dim parentBorder = TryCast(tb.Parent, Border)
            If parentBorder Is Nothing Then Return

            Dim col = Grid.GetColumn(parentBorder)
            HighlightColumn(col)
        End Sub

        Private Sub CopyColumnToClipboard_Click(sender As Object, e As MouseButtonEventArgs)
            If _selectedCells.Count = 0 Then
                MessageBox.Show("Выделите столбец мышью!", "Внимание",
                        MessageBoxButton.OK, MessageBoxImage.Warning)
                e.Handled = True
                Return
            End If

            Dim sb As New System.Text.StringBuilder()

            For i As Integer = 0 To _selectedCells.Count - 1
                Dim cellText As String = _selectedCells(i).Text
                sb.Append(cellText & vbTab)
                If i < _selectedCells.Count - 1 Then sb.Append(vbCrLf)
            Next

            Clipboard.SetText(sb.ToString())
            e.Handled = True
        End Sub

        Private Sub S24Table9_Unloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded
            _cur = Nothing



            ' ВЫЗЫВАЕМ ТВОЙ УНИВЕРСАЛЬНЫЙ ОТПИСЧИК
            UnsubscribeAllEvents(Me)



            ' ОТПИСЫВАЕМСЯ ОТ Unloaded (ЧТОБЫ НЕ БЫЛО ЦИКЛИЧЕСКИХ ССЫЛОК)
            RemoveHandler Me.Unloaded, AddressOf S24Table9_Unloaded

            GC.Collect()
        End Sub
    End Class

End Namespace
