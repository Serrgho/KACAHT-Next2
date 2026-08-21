Imports System.Collections.ObjectModel
Imports KACAHT_Next2.Kas.Otkaz

Namespace Kas

    Partial Public Class HistoryEditor
        ' DependencyProperty для коллекции
        Public Shared ReadOnly HistoryEntriesProperty As DependencyProperty =
        DependencyProperty.Register("HistoryEntries", GetType(ObservableCollection(Of HistoryEntry)),
                                    GetType(HistoryEditor), New PropertyMetadata(New ObservableCollection(Of HistoryEntry)))

        Public Property HistoryEntries As ObservableCollection(Of HistoryEntry)
            Get
                Return CType(GetValue(HistoryEntriesProperty), ObservableCollection(Of HistoryEntry))
            End Get
            Set(value As ObservableCollection(Of HistoryEntry))
                SetValue(HistoryEntriesProperty, value)
            End Set
        End Property

        ' Выбранная запись
        Public Shared ReadOnly SelectedEntryProperty As DependencyProperty =
        DependencyProperty.Register("SelectedEntry", GetType(HistoryEntry),
                                    GetType(HistoryEditor), New PropertyMetadata(Nothing, AddressOf OnSelectedEntryChanged))

        Public Property SelectedEntry As HistoryEntry
            Get
                Return CType(GetValue(SelectedEntryProperty), HistoryEntry)
            End Get
            Set(value As HistoryEntry)
                SetValue(SelectedEntryProperty, value)
            End Set
        End Property

        Private Shared Sub OnSelectedEntryChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim ctrl = CType(d, HistoryEditor)
            ctrl.UpdateButtonStates()
        End Sub

        Private Sub UpdateButtonStates()
            Dim hasSelection = SelectedEntry IsNot Nothing
            Dim idx = If(hasSelection, HistoryEntries.IndexOf(SelectedEntry), -1)
            BtnUp.IsEnabled = hasSelection AndAlso idx > 0
            BtnDown.IsEnabled = hasSelection AndAlso idx >= 0 AndAlso idx < HistoryEntries.Count - 1
            BtnDel.IsEnabled = hasSelection
        End Sub

        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().

        End Sub

        ' Кнопки
        Private Sub BtnMoveUp_Click(sender As Object, e As RoutedEventArgs)
            If SelectedEntry Is Nothing Then Return
            Dim idx = HistoryEntries.IndexOf(SelectedEntry)
            If idx > 0 Then
                Swap(Of HistoryEntry)(HistoryEntries(idx), HistoryEntries(idx - 1))
                SelectedEntry = HistoryEntries(idx - 1)
                UpdateButtonStates()
            End If
        End Sub

        Private Sub BtnMoveDown_Click(sender As Object, e As RoutedEventArgs)
            If SelectedEntry Is Nothing Then Return
            Dim idx = HistoryEntries.IndexOf(SelectedEntry)
            If idx >= 0 AndAlso idx < HistoryEntries.Count - 1 Then
                Swap(Of HistoryEntry)(HistoryEntries(idx), HistoryEntries(idx + 1))
                SelectedEntry = HistoryEntries(idx + 1)
                UpdateButtonStates()
            End If
        End Sub

        Private Sub BtnAdd_Click(sender As Object, e As RoutedEventArgs)

            AddHistoryEntry($"новое событие {Now.Second}сек")

            'Dim newEntry = New HistoryEntry With {
            '    .EventDate = DateTime.Now,
            '    .Description = $"новое событие {Now.Second}сек",
            '    .ShowDate = True ' ← по умолчанию дата отображается
            '}
            'HistoryEntries.Add(newEntry)

            'SelectedEntry = newEntry
            'UpdateButtonStates()
        End Sub


        '        ' Добавить запись с текущей датой
        'HistoryEditor.AddHistoryEntry("отказ закрыт за СЛД Боготол")

        '' Добавить запись с заданной датой
        'HistoryEditor.AddHistoryEntry(#2026-01-30#, "изменена категория с 2 на 3")

        '' Добавить запись без даты (только текст)
        'HistoryEditor.AddHistoryEntry("перемещение поездного локомотива", showDate:=False)




        ''' <summary>
        ''' Добавляет новую запись в историю изменений
        ''' </summary>
        ''' <param name="eventDate">Дата события (по умолчанию — текущая)</param>
        ''' <param name="description">Описание события</param>
        ''' <param name="showDate">Показывать дату в строке (по умолчанию — True)</param>
        Public Sub AddHistoryEntry(ByVal description As String, Optional eventDate As DateTime? = Nothing, Optional showDate As Boolean = True)

            ' Если дата не указана — используем текущую
            Dim actualDate As DateTime = If(eventDate.HasValue, eventDate.Value, DateTime.Now)

            Dim newEntry = New HistoryEntry With {
        .EventDate = actualDate,
        .Description = description,
        .ShowDate = showDate
    }

            HistoryEntries.Add(newEntry)
            SelectedEntry = newEntry
            UpdateButtonStates()
        End Sub





        Private Sub BtnRemove_Click(sender As Object, e As RoutedEventArgs)
            If SelectedEntry IsNot Nothing AndAlso HistoryEntries.Contains(SelectedEntry) Then
                HistoryEntries.Remove(SelectedEntry)
                SelectedEntry = Nothing
                UpdateButtonStates()
            End If
        End Sub


    End Class
End Namespace

