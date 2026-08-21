Imports System.Text.RegularExpressions
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input

Namespace Kas
    Partial Public Class JournalViewerRestControl


        'Implements IDisposable

        '' 🔓 Публичный доступ к внутреннему фетчеру (для авторизации извне)
        'Public ReadOnly Property RestFetcher As KasAntRestLoader.KasantRestFetcher
        '    Get
        '        Return _fetcher
        '    End Get
        'End Property

        'Public Property DKodd As Integer = 0
        'Private _fetcher As KasAntRestLoader.KasantRestFetcher
        'Private _isDisposed As Boolean = False

        'Sub New()
        '    InitializeComponent()
        '    InitFetcher()
        'End Sub

        'Sub New(DKod As Integer)
        '    InitializeComponent()
        '    DKodd = DKod
        '    InitFetcher()
        'End Sub

        'Private Sub InitFetcher()
        '    _fetcher = New KasAntRestLoader.KasantRestFetcher()
        '    _fetcher.DorKod = DKodd
        '    _fetcher.DateFrom = Date.Today.AddDays(-1)
        '    _fetcher.DateTo = Date.Today
        '    _fetcher.PageSize = 10
        'End Sub

        '' ===================================================================
        '' 🔥 ПУБЛИЧНЫЕ МЕТОДЫ
        '' ===================================================================
        'Public Async Function LoadAllAsync(username As String, password As String) As Task
        '    If _isDisposed Then
        '        Return
        '    End If
        '    ShowLoading(True, "Авторизация в КАСАНТ...")

        '    Try
        '        _fetcher.DorKod = DKodd

        '        ' 🔑 1. Сначала обязательная авторизация
        '        Dim loggedIn = Await _fetcher.LoginAsync(username, password)
        '        If Not loggedIn Then
        '            MW.InfoBLOK?.AddItem("❌ Ошибка авторизации. Проверьте логин/пароль.")
        '            ShowLoading(False)
        '            Return
        '        End If

        '        ShowLoading(True, "Загрузка данных...")
        '        Dim progress = New Progress(Of Integer)(Sub(count)
        '                                                    txtStats.Text = $"📥 Загружено: {count}..."
        '                                                End Sub)

        '        ' 🔑 2. Загружаем данные — тип List(Of JournalRecordRest)
        '        Dim records = Await _fetcher.FetchAllAsync(progress)

        '        ' 🔑 3. Биндим напрямую — конвертер НЕ нужен!
        '        dgJournal.ItemsSource = records

        '        UpdateStats(records)
        '        MW.InfoBLOK?.AddItem($"✅ REST: Загружено {records.Count} отказов")

        '    Catch ex As Exception
        '        MW.InfoBLOK?.AddItem($"❌ Ошибка: {ex.Message}")
        '    Finally
        '        ShowLoading(False)
        '    End Try
        'End Function

        'Public Sub LoadData(records As List(Of KasAntRestLoader.JournalRecordRest))
        '    dgJournal.ItemsSource = records
        '    UpdateStats(records)
        'End Sub

        'Public Sub SetDateRange(dateFrom As DateTime, dateTo As DateTime)
        '    If _fetcher IsNot Nothing Then
        '        _fetcher.DateFrom = dateFrom
        '        _fetcher.DateTo = dateTo
        '    End If
        'End Sub

        'Public Sub SetStatusFilter(statuses As String)
        '    If _fetcher IsNot Nothing Then
        '        _fetcher.StatusFilter = statuses
        '    End If
        'End Sub

        ' ===================================================================
        ' 🔽 ОБРАБОТЧИКИ
        ' ===================================================================
        Private Async Sub BtnRefresh_Click(sender As Object, e As RoutedEventArgs)
            '    Try
            '        With My.Settings
            '            Await LoadAllAsync(.CentralLogin, DecryptPassword(.CentralPassword)) ' ← Замените на реальные логин/пароль или вынесите в свойства
            '        End With


            '    Catch ex As Exception
            '        MW.InfoBLOK?.AddItem($"❌ Ошибка обновления: {ex.Message}")
            '    End Try
        End Sub

        Private Sub DgJournal_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs)
            '    Dim rec = GetRecordUnderMouse(e)
            '    If rec Is Nothing OrElse Not IsClickOnViolIdColumn() Then
            '        Return
            '    End If
            '    OpenKasAntWindow(rec.ViolId, DKodd)
        End Sub

        'Private Function GetRecordUnderMouse(e As MouseButtonEventArgs) As KasAntRestLoader.JournalRecordRest
        '    Dim rec = TryCast(dgJournal.SelectedItem, KasAntRestLoader.JournalRecordRest)
        '    If rec Is Nothing Then
        '        rec = TryCast(dgJournal.CurrentItem, KasAntRestLoader.JournalRecordRest)
        '    End If

        '    If rec Is Nothing Then
        '        Dim hit = dgJournal.InputHitTest(e.GetPosition(dgJournal))
        '        Dim depObj = TryCast(hit, DependencyObject)
        '        While depObj IsNot Nothing AndAlso Not TypeOf depObj Is DataGridRow
        '            depObj = System.Windows.Media.VisualTreeHelper.GetParent(depObj)
        '        End While
        '        Dim row = TryCast(depObj, DataGridRow)
        '        If row IsNot Nothing Then
        '            rec = TryCast(row.DataContext, KasAntRestLoader.JournalRecordRest)
        '        End If
        '    End If
        '    Return rec
        'End Function

        'Private Function IsClickOnViolIdColumn() As Boolean
        '    Dim col = dgJournal.CurrentColumn
        '    If col Is Nothing Then
        '        Return False
        '    End If
        '    Dim textCol = TryCast(col, DataGridTextColumn)
        '    If textCol Is Nothing Then
        '        Return False
        '    End If
        '    Dim binding = TryCast(textCol.Binding, Binding)
        '    Return binding?.Path?.Path = "ViolId"
        'End Function

        'Private Sub OpenKasAntWindow(violId As String, dorkod As Integer)
        '    If KasAntWND IsNot Nothing AndAlso KasAntWND.IsLoaded Then
        '        KasAntWND.Close()
        '    End If
        '    KasAntWND = New KasAntWin(violId, dorkod) With {.Owner = MW}
        '    KasAntWND.Show()
        '    If KasAntWND.WindowState = WindowState.Minimized Then
        '        KasAntWND.WindowState = WindowState.Normal
        '    End If
        '    KasAntWND.Activate()
        'End Sub

        Private Sub DgJournal_PreviewKeyDown(sender As Object, e As KeyEventArgs)
            '    If e.Key = Key.C AndAlso (Keyboard.Modifiers And ModifierKeys.Control) = ModifierKeys.Control Then
            '        CopyDataGridCells(dgJournal)
            '        e.Handled = True
            '    End If
        End Sub

        Private Sub MenuItem_Copy_Click(sender As Object, e As RoutedEventArgs)
            '    CopyDataGridCells(dgJournal)
        End Sub

        '' ===================================================================
        '' 🔽 ВСПОМОГАТЕЛЬНЫЕ
        '' ===================================================================
        'Private Sub UpdateStats(records As List(Of KasAntRestLoader.JournalRecordRest))
        '    Dim count = If(records?.Count, 0)
        '    Dim trainCnt = 0
        '    If records IsNot Nothing Then
        '        For Each r In records
        '            If Not String.IsNullOrWhiteSpace(r.Train) Then
        '                Dim parts = r.Train.Split(","c).Where(Function(t) Not String.IsNullOrWhiteSpace(t.Trim())).Count()
        '                trainCnt += parts
        '            End If
        '        Next
        '    End If
        '    txtStats.Text = $"Всего отказов: {count}  |  Упоминаний поездов: {trainCnt}"
        'End Sub

        'Private Sub ShowLoading(show As Boolean, Optional message As String = "Загрузка...")
        '    txtLoading.Text = message
        '    grdLoading.Visibility = If(show, Visibility.Visible, Visibility.Collapsed)
        '    dgJournal.IsEnabled = Not show
        '    btnRefresh.IsEnabled = Not show
        'End Sub

        'Private Sub CopyDataGridCells(grid As DataGrid)
        '    Dim selectedCells = grid.SelectedCells
        '    If selectedCells.Count = 0 Then
        '        Return
        '    End If

        '    Dim headerToProp As New Dictionary(Of String, String) From {
        '        {"№", "ViolId"},
        '        {"Кат.", "Category"},
        '        {"Начало", "DateFrom"},
        '        {"Окончание", "DateTo"},
        '        {"Место", "PlaceInfo"},
        '        {"Расследование", "Investigation"},
        '        {"Виновный", "Guilty"},
        '        {"Поезда", "Train"},
        '        {"Тех. средство", "ReasonObjectTop"},
        '        {"Группа причин", "GroupReason"},
        '        {"Причина", "Reason"}
        '    }

        '    Dim rowsData = selectedCells _
        '        .GroupBy(Function(cell) cell.Item) _
        '        .OrderBy(Function(group) grid.Items.IndexOf(group.Key)) _
        '        .Select(Function(group)
        '                    Dim cellsInRow = group _
        '                        .OrderBy(Function(c) c.Column.DisplayIndex) _
        '                        .Select(Function(cellInfo)
        '                                    Dim record = TryCast(cellInfo.Item, KasAntRestLoader.JournalRecordRest)
        '                                    If record Is Nothing Then
        '                                        Return ""
        '                                    End If

        '                                    Dim propName As String = ""

        '                                    If TypeOf cellInfo.Column Is DataGridTextColumn Then
        '                                        Dim textCol = CType(cellInfo.Column, DataGridTextColumn)
        '                                        Dim binding = TryCast(textCol.Binding, Binding)
        '                                        propName = binding?.Path?.Path
        '                                    ElseIf TypeOf cellInfo.Column Is DataGridTemplateColumn Then
        '                                        Dim header = TryCast(cellInfo.Column.Header, String)?.Trim()
        '                                        If header IsNot Nothing AndAlso headerToProp.ContainsKey(header) Then
        '                                            propName = headerToProp(header)
        '                                        End If
        '                                    End If

        '                                    If Not String.IsNullOrEmpty(propName) Then
        '                                        Dim prop = record.GetType().GetProperty(propName)
        '                                        If prop IsNot Nothing Then
        '                                            Dim rawValue = prop.GetValue(record)?.ToString()
        '                                            Return CleanHtml(rawValue)
        '                                        End If
        '                                    End If
        '                                    Return ""
        '                                End Function)
        '                    Return String.Join(vbTab, cellsInRow)
        '                End Function)

        '    Dim result = String.Join(vbCrLf, rowsData)
        '    If Not String.IsNullOrEmpty(result) Then
        '        Try
        '            Clipboard.SetText(result)
        '            MW.InfoBLOK?.AddItem("📋 Скопировано в буфер")
        '        Catch ex As Exception
        '            System.Diagnostics.Debug.WriteLine($"Clipboard error: {ex.Message}")
        '        End Try
        '    End If
        'End Sub

        'Private Function CleanHtml(text As String) As String
        '    If String.IsNullOrWhiteSpace(text) Then
        '        Return ""
        '    End If
        '    text = Regex.Replace(text, "<[^>]+>", " ")
        '    text = text.Replace("&nbsp;", " ").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&")
        '    text = Regex.Replace(text, "\s+", " ")
        '    Return text.Trim()
        'End Function

        '' ===================================================================
        '' IDisposable
        '' ===================================================================
        'Protected Overridable Sub Dispose(disposing As Boolean)
        '    If Not _isDisposed Then
        '        _isDisposed = True
        '    End If
        'End Sub

        'Public Sub Dispose() Implements IDisposable.Dispose
        '    Dispose(True)
        '    GC.SuppressFinalize(Me)
        'End Sub


    End Class
End Namespace