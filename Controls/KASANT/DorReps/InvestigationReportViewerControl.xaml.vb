Imports System.ComponentModel
Imports System.Globalization

Namespace Kas

	Partial Public Class InvestigationReportViewerControl

        ' 🔑 Год отчёта (берётся из даты, а не из DateTime.Now)
        Private _reportYear As Integer = DateTime.Now.Year

        Public Property ReportYear As Integer
            Get
                Return _reportYear
            End Get
            Set(value As Integer)
                If _reportYear <> value Then
                    _reportYear = value
                End If
            End Set
        End Property

        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().
            Me.DataContext = Me '
        End Sub

        Public Sub LoadData(records As List(Of InvestigationReportItem),
                    Optional curYear As Integer = 0,
                    Optional prevYear As Integer = 0)


            If curYear = 0 Then curYear = DateTime.Now.Year
            If prevYear = 0 Then prevYear = curYear - 1


            ' 🔑 Меняем только годы в заголовках
            hdrTotal.Text = $"{curYear}г"
            hdrTotalPY.Text = $"{prevYear}г"
            hdrAccepted.Text = $"{curYear}г"
            hdrAcceptedPY.Text = $"{prevYear}г"
            hdrOverdue.Text = $"{curYear}г"
            hdrOverduePY.Text = $"{prevYear}г"
            hdrInvestigated.Text = $"{curYear}г"
            hdrInvestigatedPY.Text = $"{prevYear}г"
            hdrNotAccepted.Text = $"{curYear}г"
            hdrNotAcceptedPY.Text = $"{prevYear}г"
            For Each item In records
                item.IsTotalRow = item.Name.ToUpper().Contains("ВСЕГО") OrElse
                          item.Name.ToUpper().Contains("ИТОГО")
            Next

            dgReport.ItemsSource = records


        End Sub


        ''' <summary>
        ''' Создаёт двухуровневый заголовок для колонки
        ''' </summary>
        Private Function CreateGroupHeader(groupName As String, subName As String) As Object
            Dim grid As New Grid()
            grid.RowDefinitions.Add(New RowDefinition() With {.Height = New GridLength(1, GridUnitType.Star)})
            grid.RowDefinitions.Add(New RowDefinition() With {.Height = New GridLength(1, GridUnitType.Star)})

            ' 🔑 Явно задаем выравнивание
            grid.HorizontalAlignment = HorizontalAlignment.Stretch


            ' Верхняя строка
            Dim topText As New TextBlock() With {
        .Text = groupName,
        .FontWeight = FontWeights.SemiBold,
        .FontSize = 11,
        .HorizontalAlignment = HorizontalAlignment.Center,
        .VerticalAlignment = VerticalAlignment.Center,
        .TextAlignment = TextAlignment.Center,
        .Padding = New Thickness(2, 2, 2, 0)
    }
            Grid.SetRow(topText, 0)
            grid.Children.Add(topText)

            ' Нижняя строка
            Dim bottomText As New TextBlock() With {
        .Text = subName,
        .FontSize = 10,
        .Foreground = If(String.IsNullOrEmpty(groupName), Brushes.Gray, Brushes.Black),
        .HorizontalAlignment = HorizontalAlignment.Center,
        .VerticalAlignment = VerticalAlignment.Center,
        .TextAlignment = TextAlignment.Center,
        .Padding = New Thickness(2, 0, 2, 2)
    }
            Grid.SetRow(bottomText, 1)
            grid.Children.Add(bottomText)

            Return grid
        End Function



        Private Sub MenuItem_Copy_Click(sender As Object, e As RoutedEventArgs)
            CopySelectedCells()
        End Sub

        Private Sub BtnCopyAll_Click(sender As Object, e As RoutedEventArgs)
            Dim records = TryCast(dgReport.ItemsSource, List(Of InvestigationReportItem))
            If records Is Nothing OrElse records.Count = 0 Then
                MW.InfoBLOK?.AddItem("⚠ Нечего копировать")
                Return
            End If

            Dim sb As New System.Text.StringBuilder()

            ' Заголовки (16 колонок)
            sb.AppendLine(
            "Регион / Подразделение" & vbTab &
            "Всего" & vbTab & "Всего (ПГ)" & vbTab & "Δ Всего" & vbTab &
            "Приняты" & vbTab & "Приняты (ПГ)" & vbTab & "Δ Приняты" & vbTab &
            "Просрочено" & vbTab & "Просрочено (ПГ)" & vbTab & "Δ Просрочено" & vbTab &
            "Расследованы" & vbTab & "Расследованы (ПГ)" & vbTab & "Δ Расследованы" & vbTab &
            "Не приняты" & vbTab & "Не приняты (ПГ)" & vbTab & "Δ Не приняты")

            ' Данные с форматированием дельт (+/-)
            Dim FmtDelta = Function(v As Integer) As String
                               If v > 0 Then Return "+" & v.ToString()
                               If v < 0 Then Return v.ToString()
                               Return "0"
                           End Function

            For Each item In records
                sb.AppendLine(
                $"{item.Name}" & vbTab &
                $"{item.Total}" & vbTab & $"{item.Total_PY}" & vbTab & $"{FmtDelta(item.Delta_Total)}" & vbTab &
                $"{item.Accepted}" & vbTab & $"{item.Accepted_PY}" & vbTab & $"{FmtDelta(item.Delta_Accepted)}" & vbTab &
                $"{item.Overdue}" & vbTab & $"{item.Overdue_PY}" & vbTab & $"{FmtDelta(item.Delta_Overdue)}" & vbTab &
                $"{item.Investigated}" & vbTab & $"{item.Investigated_PY}" & vbTab & $"{FmtDelta(item.Delta_Investigated)}" & vbTab &
                $"{item.NotAccepted}" & vbTab & $"{item.NotAccepted_PY}" & vbTab & $"{FmtDelta(item.Delta_NotAccepted)}")
            Next

            Try
                Clipboard.SetText(sb.ToString())
                MW.InfoBLOK?.AddItem($"📋 Скопировано {records.Count} строк отчёта")
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Clipboard error: {ex.Message}")
            End Try
        End Sub

        Private Sub DgReport_PreviewKeyDown(sender As Object, e As Input.KeyEventArgs)
            If e.Key = Input.Key.C AndAlso (Input.Keyboard.Modifiers And Input.ModifierKeys.Control) = Input.ModifierKeys.Control Then
                CopySelectedCells()
                e.Handled = True
            End If
        End Sub

        Private Sub CopySelectedCells()
            Dim selectedCells = dgReport.SelectedCells
            If selectedCells.Count = 0 Then Return

            Dim rowsData = selectedCells _
            .GroupBy(Function(cell) cell.Item) _
            .OrderBy(Function(group) dgReport.Items.IndexOf(group.Key)) _
            .Select(Function(group)
                        Dim cellsInRow = group _
                            .OrderBy(Function(c) c.Column.DisplayIndex) _
                            .Select(Function(cellInfo)
                                        Dim item = TryCast(cellInfo.Item, InvestigationReportItem)
                                        If item Is Nothing Then Return ""

                                        ' DataGridTextColumn
                                        Dim col = TryCast(cellInfo.Column, DataGridTextColumn)
                                        Dim binding = TryCast(col?.Binding, Binding)
                                        If binding?.Path?.Path IsNot Nothing Then
                                            Dim propName = binding.Path.Path
                                            Dim prop = item.GetType().GetProperty(propName)
                                            If prop IsNot Nothing Then
                                                Return If(prop.GetValue(item)?.ToString(), "")
                                            End If
                                        End If

                                        ' DataGridTemplateColumn (для дельт)
                                        Dim tplCol = TryCast(cellInfo.Column, DataGridTemplateColumn)
                                        If tplCol IsNot Nothing Then
                                            ' Определяем дельту по индексу колонки (DisplayIndex)
                                            ' 0=Name, 1-3=Всего, 4-6=Приняты, 7-9=Просрочено, 10-12=Расследованы, 13-15=Не приняты
                                            Dim idx = tplCol.DisplayIndex
                                            Select Case idx
                                                Case 3 : Return item.Delta_Total.ToString()
                                                Case 6 : Return item.Delta_Accepted.ToString()
                                                Case 9 : Return item.Delta_Overdue.ToString()
                                                Case 12 : Return item.Delta_Investigated.ToString()
                                                Case 15 : Return item.Delta_NotAccepted.ToString()
                                            End Select
                                        End If

                                        Return ""
                                    End Function)
                        Return String.Join(vbTab, cellsInRow)
                    End Function)

            Dim result = String.Join(vbCrLf, rowsData)
            If Not String.IsNullOrEmpty(result) Then
                Try
                    Clipboard.SetText(result)
                    MW.InfoBLOK?.AddItem("📋 Скопировано в буфер")
                Catch ex As Exception
                    System.Diagnostics.Debug.WriteLine($"Clipboard error: {ex.Message}")
                End Try
            End If
        End Sub

        Private Sub dgReport_LoadingRow(sender As Object, e As DataGridRowEventArgs)
            ' Проверяем, является ли строка итоговой
            Dim item = TryCast(e.Row.DataContext, InvestigationReportItem)
            If item IsNot Nothing AndAlso item.IsTotalRow Then
                ' Устанавливаем жёлтый фон для всех ячеек
                e.Row.Background = New SolidColorBrush(Color.FromRgb(255, 255, 153))
                e.Row.Foreground = Brushes.Black
            End If
        End Sub
    End Class

    Public Class HeaderData
        Public Property GroupName As String
        Public Property Year As String
        Public Property YearForeground As Brush
    End Class

    ''' <summary>
    ''' Надежный конвертер цвета для дельт. 
    ''' > 0 = Красный (рост плохих показателей), < 0 = Зеленый (снижение), 0 = Черный.
    ''' Если передан параметр "inverse", логика зеркалится (например, для "Расследованы", где рост = хорошо).
    ''' </summary>
    Public Class DeltaColorConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            Dim delta As Integer = 0
            If Integer.TryParse(value?.ToString(), delta) Then
                Dim isInverse As Boolean = (parameter?.ToString()?.ToLower() = "inverse")

                If isInverse Then
                    ' Инверсия: рост (>0) это хорошо (Зеленый), падение (<0) это плохо (Красный)
                    If delta > 0 Then Return Brushes.Green
                    If delta < 0 Then Return Brushes.Red
                Else
                    ' Стандарт: рост (>0) это плохо (Красный), падение (<0) это хорошо (Зеленый)
                    If delta > 0 Then Return Brushes.Red
                    If delta < 0 Then Return Brushes.Green
                End If
            End If
            Return Brushes.Black ' Для 0 или если значение не число
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class

End Namespace


