Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input


Namespace Kas
    Partial Public Class JournalViewerControl

        Public Property DKodd As Integer = 88

        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().

        End Sub

        Sub New(DKod As Integer)

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().
            DKodd = DKod
        End Sub

        ''' <summary>
        ''' Загружает список отказов в таблицу
        ''' </summary>
        Public Sub LoadData(records As List(Of JournalRecord), Optional showCopyButton As Boolean = False)
            dgJournal.ItemsSource = records
            ' Показываем/скрываем кнопку копирования номеров
            btnCopyViolIds.Visibility = If(showCopyButton, Visibility.Visible, Visibility.Collapsed)

            ' Обновляем статистику
            UpdateStats(records)
        End Sub

        Private Sub UpdateStats(records As List(Of JournalRecord))
            Dim count = If(records?.Count, 0)
            Dim trains = If(records?.Sum(Function(r) r.TrainCount), 0)
            txtStats.Text = $"Всего отказов: {count}  |  Задержано поездов: {trains}"
        End Sub

        ' 🔽 Обработчик двойного клика (срабатывает ТОЛЬКО по колонке ViolId)
        Private Sub DgJournal_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs)

            MW.InfoBLOK.AddItem(DKodd)

            ' 1️⃣ Получаем запись под курсором (работает при SelectionUnit="Cell")
            Dim rec As JournalRecord = Nothing

            ' Пробуем стандартные свойства (на случай, если сработают)
            rec = TryCast(dgJournal.SelectedItem, JournalRecord)
            If rec Is Nothing Then rec = TryCast(dgJournal.CurrentItem, JournalRecord)

            ' Если всё ещё Nothing — ищем визуально (гарантированно сработает)
            If rec Is Nothing Then
                Dim hit = dgJournal.InputHitTest(e.GetPosition(dgJournal))
                Dim depObj = TryCast(hit, DependencyObject)

                ' Поднимаемся по дереву до DataGridRow
                While depObj IsNot Nothing AndAlso Not TypeOf depObj Is DataGridRow
                    depObj = System.Windows.Media.VisualTreeHelper.GetParent(depObj)
                End While

                Dim row = TryCast(depObj, DataGridRow)
                If row IsNot Nothing Then rec = TryCast(row.DataContext, JournalRecord)
            End If

            If rec Is Nothing Then Return

            ' 2️⃣ Проверяем, что клик именно по колонке "№ отказа" (ViolId)
            ' В твоём XAML у этой колонки Binding="{Binding ViolId}"
            Dim col = dgJournal.CurrentColumn
            If col IsNot Nothing Then
                Dim textCol = TryCast(col, DataGridTextColumn)
                If textCol IsNot Nothing Then
                    Dim binding = TryCast(textCol.Binding, Binding)
                    If binding?.Path?.Path <> "ViolId" Then Return
                End If
            End If
            'Dim dorKod As String = If(Fetcher.DorOfOTS > 0, Fetcher.DorOfOTS.ToString("00"), "88")
            ' 3️⃣ Открываем окно
            OpenKasAntWindow(rec.ViolId, DKodd)
        End Sub

        ' 🔽 Отдельный метод для управления окном (уже исправленный)
        Private Sub OpenKasAntWindow(violId As String, dorkod As Integer)
            ' ✅ AndAlso предотвращает NullReferenceException при закрытом окне
            If KasAntWND IsNot Nothing AndAlso KasAntWND.IsLoaded Then
                KasAntWND.Close()
            End If

            KasAntWND = New KasAntWin(violId, DKodd) With {.Owner = MW}
            KasAntWND.Show()

            '' 🔹 Опционально: активируем окно, если оно было свёрнуто
            'If KasAntWND.WindowState = WindowState.Minimized Then
            '    KasAntWND.WindowState = WindowState.Normal
            'End If
            KasAntWND.Activate()
        End Sub

        Private Sub DgJournal_PreviewKeyDown(sender As Object, e As KeyEventArgs)
            If e.Key = Key.C AndAlso (Keyboard.Modifiers And ModifierKeys.Control) = ModifierKeys.Control Then
                CopyDataGridCells(dgJournal)
                e.Handled = True
            End If
        End Sub

        Private Sub MenuItem_Copy_Click(sender As Object, e As RoutedEventArgs)
            CopyDataGridCells(dgJournal)
        End Sub

        ' 🔽 Логика копирования (без изменений)
        Private Sub CopyDataGridCells(grid As DataGrid)



            Dim selectedCells = grid.SelectedCells
            If selectedCells.Count = 0 Then Return

            Dim headerToProp As New Dictionary(Of String, String) From {
                {"№ отказа", "ViolId"},
                {"АСУ", "ASU"},
                {"Кат.", "Category"},
                {"Начало", "StartTime"},
                {"Статус", "Status"},
                {"Откуда", "FromDept"},
                {"Куда", "ToDept"},
                {"Место отказа", "Location"},
                {"Тех. средство", "Equipment"},
                {"Поезда", "TrainCount"}
            }

            Dim rowsData = selectedCells _
                .GroupBy(Function(cell) cell.Item) _
                .OrderBy(Function(group) grid.Items.IndexOf(group.Key)) _
                .Select(Function(group)
                            Dim cellsInRow = group _
                                .OrderBy(Function(c) c.Column.DisplayIndex) _
                                .Select(Function(cellInfo)
                                            Dim item = cellInfo.Item
                                            If item Is Nothing Then Return ""

                                            Dim propName As String = ""

                                            If TypeOf cellInfo.Column Is DataGridTextColumn Then
                                                Dim textCol = CType(cellInfo.Column, DataGridTextColumn)
                                                Dim binding = TryCast(textCol.Binding, Binding)
                                                If binding IsNot Nothing AndAlso binding.Path IsNot Nothing Then
                                                    propName = binding.Path.Path
                                                End If
                                            ElseIf TypeOf cellInfo.Column Is DataGridTemplateColumn Then
                                                Dim header = TryCast(cellInfo.Column.Header, String)?.Trim()
                                                If header IsNot Nothing AndAlso headerToProp.ContainsKey(header) Then
                                                    propName = headerToProp(header)
                                                End If
                                            End If

                                            If Not String.IsNullOrEmpty(propName) Then
                                                Dim prop = item.GetType().GetProperty(propName)
                                                If prop IsNot Nothing Then
                                                    Dim rawValue = prop.GetValue(item)
                                                    Dim valStr = rawValue?.ToString()

                                                    ' 🔧 FIX: Если это статус — конвертируем через твою существующую функцию
                                                    If propName = "Status" AndAlso Not String.IsNullOrEmpty(valStr) Then
                                                        valStr = GetStatusText(valStr)
                                                    End If

                                                    Return valStr
                                                End If
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

        Private Sub BtnCopyViolIds_Click(sender As Object, e As RoutedEventArgs)
            Dim records = TryCast(dgJournal.ItemsSource, List(Of JournalRecord))
            If records Is Nothing OrElse records.Count = 0 Then
                MessageBox.Show("Список отказов пуст", "Копирование", MessageBoxButton.OK, MessageBoxImage.Information)
                Return
            End If

            ' Собираем все ViolId через запятую
            Dim violIds = String.Join(", ", records.Select(Function(r) r.ViolId))

            Try
                Clipboard.SetText(violIds)
                MW.InfoBLOK?.AddItem($"📋 Скопировано {records.Count} номеров отказов")
            Catch ex As Exception
                MessageBox.Show($"Ошибка копирования: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End Sub
    End Class
End Namespace

