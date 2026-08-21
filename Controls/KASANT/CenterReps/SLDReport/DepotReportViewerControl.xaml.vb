Imports System.ComponentModel
Imports System.Runtime.CompilerServices

Namespace Kas
    Partial Public Class DepotReportViewerControl
        Implements INotifyPropertyChanged

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        ' ═══════════ СВОЙСТВА ДЛЯ БИНДИНГА ═══════════
        Private _period1CurrentHeader As String = ""
        Public Property Period1CurrentHeader As String
            Get
                Return _period1CurrentHeader
            End Get
            Set(value As String)
                If _period1CurrentHeader <> value Then
                    _period1CurrentHeader = value
                    RaisePropertyChanged()
                End If
            End Set
        End Property

        Private _period1PrevYearHeader As String = ""
        Public Property Period1PrevYearHeader As String
            Get
                Return _period1PrevYearHeader
            End Get
            Set(value As String)
                If _period1PrevYearHeader <> value Then
                    _period1PrevYearHeader = value
                    RaisePropertyChanged()
                End If
            End Set
        End Property

        Private _period2CurrentHeader As String = ""
        Public Property Period2CurrentHeader As String
            Get
                Return _period2CurrentHeader
            End Get
            Set(value As String)
                If _period2CurrentHeader <> value Then
                    _period2CurrentHeader = value
                    RaisePropertyChanged()
                End If
            End Set
        End Property

        Private _period2PrevYearHeader As String = ""
        Public Property Period2PrevYearHeader As String
            Get
                Return _period2PrevYearHeader
            End Get
            Set(value As String)
                If _period2PrevYearHeader <> value Then
                    _period2PrevYearHeader = value
                    RaisePropertyChanged()
                End If
            End Set
        End Property

        Private _hasPeriod2 As Boolean = False
        Public Property HasPeriod2 As Boolean
            Get
                Return _hasPeriod2
            End Get
            Set(value As Boolean)
                If _hasPeriod2 <> value Then
                    _hasPeriod2 = value
                    RaisePropertyChanged()
                End If
            End Set
        End Property

        Private Sub RaisePropertyChanged(<CallerMemberName> Optional propName As String = Nothing)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
        End Sub

        ' ═══════════ КОНСТРУКТОР ═══════════
        Sub New()
            InitializeComponent()
            Me.DataContext = Me
        End Sub

        ' ═══════════ ЗАГРУЗКА ДАННЫХ ═══════════
        Public Sub LoadData(rows As List(Of DepotReportRow),
                            period1Title As String,
                            Optional period2Title As String = Nothing)

            Period1CurrentHeader = $"{period1Title} {Date.Today.Year}"
            Period1PrevYearHeader = $"{period1Title} {Date.Today.Year - 1}"

            HasPeriod2 = Not String.IsNullOrEmpty(period2Title)
            If HasPeriod2 Then
                Period2CurrentHeader = $"{period2Title} {Date.Today.Year}"
                Period2PrevYearHeader = $"{period2Title} {Date.Today.Year - 1}"
            Else
                Period2CurrentHeader = ""
                Period2PrevYearHeader = ""
            End If

            dgReport.ItemsSource = rows


            ' ═══════════ СКРЫТИЕ КОЛОНОК ПЕРИОДА 2 ═══════════
            ' Находим колонки по имени свойства (Binding Path)
            For Each col In dgReport.Columns
                If TypeOf col Is DataGridTextColumn Then
                    Dim textCol = CType(col, DataGridTextColumn)
                    Dim binding = TryCast(textCol.Binding, Binding)
                    If binding IsNot Nothing AndAlso binding.Path IsNot Nothing Then
                        Dim propName = binding.Path.Path
                        If propName = "Period2Current" OrElse propName = "Period2PrevYear" Then
                            col.Visibility = If(HasPeriod2, Visibility.Visible, Visibility.Collapsed)
                        End If
                    End If
                End If
            Next
            ' ═══════════ КОНЕЦ СКРЫТИЯ ═══════════


            Dim totalRow = rows.FirstOrDefault(Function(r) r.IsTotalRow)
            'If totalRow IsNot Nothing Then
            '    txtStats.Text = $"Всего отказов: {totalRow.Period1Current} (тек.год) / {totalRow.Period1PrevYear} (прош.год)"
            'Else
            '    txtStats.Text = $"Загружено {rows.Count} строк | Обновлено {Date.Now:dd.MM.yyyy HH:mm}"
            'End If
        End Sub

        ' ═══════════ ВЫДЕЛЕНИЕ ИТОГОВОЙ СТРОКИ ═══════════
        Private Sub dgReport_LoadingRow(sender As Object, e As DataGridRowEventArgs)
            Dim row = TryCast(e.Row.DataContext, DepotReportRow)
            If row IsNot Nothing AndAlso row.IsTotalRow Then
                e.Row.FontWeight = FontWeights.Bold
                e.Row.FontSize = 13
                e.Row.Background = New SolidColorBrush(Color.FromArgb(255, 255, 255, 153))
                e.Row.Foreground = New SolidColorBrush(Colors.Black)
            End If
        End Sub

        ' ═══════════ КОПИРОВАНИЕ: ОБРАБОТЧИКИ ═══════════

        ''' <summary>
        ''' Перехват Ctrl+C
        ''' </summary>
        Private Sub DgReport_PreviewKeyDown(sender As Object, e As KeyEventArgs)
            If e.Key = Key.C AndAlso (Keyboard.Modifiers And ModifierKeys.Control) = ModifierKeys.Control Then
                CopyDataGridCells(dgReport)
                e.Handled = True
            End If
        End Sub

        ''' <summary>
        ''' Обработчик контекстного меню "Копировать"
        ''' </summary>
        Private Sub MenuItem_Copy_Click(sender As Object, e As RoutedEventArgs)
            CopyDataGridCells(dgReport)
        End Sub

        ' ═══════════ ОСНОВНАЯ ЛОГИКА КОПИРОВАНИЯ ═══════════

        ''' <summary>
        ''' Копирует выделенные ячейки DataGrid в буфер обмена в TSV-формате
        ''' </summary>
        Private Sub CopyDataGridCells(grid As DataGrid)


            Dim selectedCells = grid.SelectedCells
            If selectedCells.Count = 0 Then Return

            Dim rowsData = selectedCells _
        .GroupBy(Function(cell) cell.Item) _
        .OrderBy(Function(group) grid.Items.IndexOf(group.Key)) _
        .Select(Function(group)
                    Dim cellsInRow = group _
                        .OrderBy(Function(c) c.Column.DisplayIndex) _
                        .Select(Function(cellInfo)
                                    ' Берём только текстовые колонки
                                    If TypeOf cellInfo.Column Is DataGridTextColumn Then
                                        Dim textCol = CType(cellInfo.Column, DataGridTextColumn)
                                        Dim binding = TryCast(textCol.Binding, Binding)
                                        If binding?.Path?.Path IsNot Nothing Then
                                            Dim propName = binding.Path.Path
                                            Dim prop = cellInfo.Item.GetType().GetProperty(propName)
                                            If prop IsNot Nothing Then
                                                Dim rawValue = prop.GetValue(cellInfo.Item)
                                                ' Nothing → пустая строка (как TargetNullValue='' в XAML)
                                                If rawValue Is Nothing Then Return ""
                                                Return rawValue.ToString()
                                            End If
                                        End If
                                    End If
                                    Return ""
                                End Function)
                    Return String.Join(vbTab, cellsInRow)
                End Function)

            ' Склеиваем строки через CRLF
            Dim result = String.Join(vbCrLf, rowsData)

            ' 🔧 Убираем лишний перенос строки в конце (если вдруг появился)
            If result.EndsWith(vbCrLf) Then
                result = result.Substring(0, result.Length - vbCrLf.Length)
            End If

            If Not String.IsNullOrEmpty(result) Then
                Try
                    Clipboard.SetText(result)
                    MW.InfoBLOK?.AddItem("📋 Скопировано в буфер")
                Catch ex As Exception
                    System.Diagnostics.Debug.WriteLine($"Clipboard error: {ex.Message}")
                End Try
            End If

        End Sub



    End Class
End Namespace

