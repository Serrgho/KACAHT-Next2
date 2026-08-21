Imports System.Collections.ObjectModel
Imports System.IO
Imports System.Text
Imports System.Windows

Namespace Kas
	Partial Public Class GenReportWindow
		Inherits Window
        Private _searchTimer As New System.Windows.Threading.DispatcherTimer()
        Private _allData As List(Of Kas.KasGenReportModule.GenReportRow)
		Private _filteredData As ObservableCollection(Of Kas.KasGenReportModule.GenReportRow)
		Sub New()

			' Этот вызов является обязательным для конструктора.
			InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().
            ' Настройка таймера задержки поиска
            _searchTimer.Interval = TimeSpan.FromMilliseconds(300)
            AddHandler _searchTimer.Tick, AddressOf OnSearchTimerTick
            Try
				Dim appBackBrush = TryCast(Application.Current.TryFindResource("AppBackBrush"), SolidColorBrush)
				If appBackBrush IsNot Nothing Then Me.Background = appBackBrush
			Catch
			End Try
		End Sub

        Private Sub OnSearchTimerTick(sender As Object, e As EventArgs)
            _searchTimer.Stop()   ' Останавливаем, чтобы не срабатывал повторно
            ApplyFilter()
        End Sub


        Public Sub LoadData(data As List(Of Kas.KasGenReportModule.GenReportRow))
            _allData = data
            _filteredData = New ObservableCollection(Of Kas.KasGenReportModule.GenReportRow)(data)
            dgReport.ItemsSource = _filteredData
            UpdateCount()
            txtStatus.Text = $"✅ Загружено: {data.Count} записей в {DateTime.Now:HH:mm:ss}"
            If KasGenReportModule.LastFilteredOut > 0 Then
                txtRowCount.Text &= $" | Отсеяно: {KasGenReportModule.LastFilteredOut}"
            End If
        End Sub

        Private Sub UpdateCount()
            If _allData Is Nothing Then
                txtRowCount.Text = "Записей: 0"
            ElseIf _filteredData.Count = _allData.Count Then
                txtRowCount.Text = $"Записей: {_allData.Count}"
            Else
                txtRowCount.Text = $"Показано: {_filteredData.Count} из {_allData.Count}"
            End If
        End Sub

        ' Поиск
        Private Sub txtSearch_TextChanged(sender As Object, e As TextChangedEventArgs)
            ' Показываем кнопку сброса, если есть текст
            btnClearSearch.Visibility = If(String.IsNullOrWhiteSpace(txtSearch.Text),
                                   Visibility.Collapsed,
                                   Visibility.Visible)
            ' Перезапускаем таймер — фильтрация сработает через 300 мс после последнего символа
            _searchTimer.Stop()
            _searchTimer.Start()
        End Sub

        Private Sub ApplyFilter()
            If _allData Is Nothing Then Return

            Dim term = If(txtSearch.Text, "").Trim().ToLower()

            Dim filtered As IEnumerable(Of Kas.KasGenReportModule.GenReportRow)

            If String.IsNullOrEmpty(term) Then
                filtered = _allData
            Else
                filtered = _allData.Where(Function(r)
                                              Return (r.ViolId?.Contains(term)) OrElse
                       (r.Location?.ToLower().Contains(term)) OrElse
                       (r.Road?.ToLower().Contains(term)) OrElse
                       (r.Investigator?.ToLower().Contains(term)) OrElse
                       (r.GuiltyDepot?.ToLower().Contains(term)) OrElse
                       (r.OTSLev1?.ToLower().Contains(term)) OrElse
                       (r.OTSLev2?.ToLower().Contains(term)) OrElse
                       (r.OTSLev3?.ToLower().Contains(term)) OrElse
                       (r.Category?.ToString() = term)
                                          End Function)
            End If

            _filteredData = New ObservableCollection(Of Kas.KasGenReportModule.GenReportRow)(filtered)
            dgReport.ItemsSource = _filteredData
            UpdateCount()
        End Sub


        '' Двойной клик по строке — можно открыть карточку отказа (открыть отказ)
        'Private Sub dgReport_MouseDoubleClick(sender As Object, e As Input.MouseButtonEventArgs)
        '    Dim row = TryCast(dgReport.SelectedItem, Kas.KasGenReportModule.GenReportRow)
        '    If row Is Nothing OrElse row.ViolId Is Nothing Then Return

        '    ' Копируем ID отказа в буфер (можно заменить на открытие карточки)
        '    Clipboard.SetText(row.ViolId.ToString())
        '    txtStatus.Text = $"📋 Скопирован ID отказа: {row.ViolId}"
        'End Sub

        Private Sub dgReport_MouseDoubleClick(sender As Object, e As Input.MouseButtonEventArgs)
            ' 1️⃣ Получаем запись из текущей ячейки (работает при SelectionUnit="Cell")
            Dim rec = TryCast(dgReport.CurrentCell.Item, Kas.KasGenReportModule.GenReportRow)
            If rec Is Nothing OrElse rec.ViolId Is Nothing Then Return

            ' 2️⃣ Проверяем, что клик именно по колонке "ID отказа" (ViolId)
            Dim col = dgReport.CurrentColumn
            If col IsNot Nothing Then
                Dim textCol = TryCast(col, DataGridTextColumn)
                If textCol IsNot Nothing Then
                    Dim binding = TryCast(textCol.Binding, Binding)
                    If binding?.Path?.Path <> "ViolId" Then Return
                End If
            End If

            ' 3️ Открываем карточку отказа (88 — код Красноярской дороги)
            OpenKasAntWindow(rec.ViolId, 88)
            txtStatus.Text = $"📋 Открыта карточка отказа: {rec.ViolId}"
        End Sub

        ' 🔽 Отдельный метод для управления окном
        Private Sub OpenKasAntWindow(violId As String, dorkod As Integer)
            If KasAntWND IsNot Nothing AndAlso KasAntWND.IsLoaded Then
                KasAntWND.Close()
            End If

            KasAntWND = New KasAntWin(violId, dorkod) With {.Owner = Me}
            KasAntWND.Show()
            KasAntWND.Activate()
        End Sub



        Private Sub btnClearSearch_Click(sender As Object, e As RoutedEventArgs)
            txtSearch.Text = ""
            txtSearch.Focus()
        End Sub

        Private Sub btnCopyIds_Click(sender As Object, e As RoutedEventArgs)
            If _filteredData Is Nothing OrElse _filteredData.Count = 0 Then
                txtStatus.Text = "⚠ Нет данных для копирования"
                Return
            End If

            ' Собираем все ID через запятую
            Dim ids = String.Join(",", _filteredData.Select(Function(r) r.ViolId))

            Clipboard.SetText(ids)

            txtStatus.Text = $"📋 Скопировано {_filteredData.Count} ID в буфер обмена"
        End Sub
    End Class

End Namespace

