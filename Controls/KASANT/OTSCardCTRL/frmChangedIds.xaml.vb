

Imports System.Collections.ObjectModel
Imports System.Windows
Imports System.Windows.Input
Imports KACAHT_Next2.Kas.KasAntLoader

Namespace Kas
    Partial Public Class FrmChangedIds
        Private _list As ObservableCollection(Of IdMappingInfo)
        Private _onIdsSelected As Action(Of String, String)
        Private _onButtonClick As Action(Of String, String) ' Команда для кнопки OK
        Private _onCancelClick As Action(Of String, String) ' Команда для кнопки Отмена

        Public Sub New(mappingList As ObservableCollection(Of IdMappingInfo),
                       onIdsSelectedAction As Action(Of String, String),
                       onButtonClickAction As Action(Of String, String),
                       onCancelClickAction As Action(Of String, String))
            InitializeComponent()
            _list = mappingList
            _onIdsSelected = onIdsSelectedAction
            _onButtonClick = onButtonClickAction
            _onCancelClick = onCancelClickAction
            dgvIds.ItemsSource = _list
        End Sub

        Private Sub DataGrid_MouseUp(sender As Object, e As MouseButtonEventArgs)
            Dim selectedItem = TryCast(dgvIds.SelectedItem, IdMappingInfo)
            If selectedItem Is Nothing Then Return

            ' Вызываем переданное действие (которое отфильтрует список и вызовет AddOTSToContainer)
            If _onIdsSelected IsNot Nothing Then
                _onIdsSelected.Invoke(selectedItem.OldId, selectedItem.NewId)
                selectedItem.IsReady = True
            End If

            ' Окно остается открытым для выбора следующей пары
        End Sub

        Private Sub Button_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            If btn Is Nothing Then Return

            ' Получаем объект строки из DataContext кнопки 
            Dim selectedItem = TryCast(btn.DataContext, IdMappingInfo)
            If selectedItem Is Nothing Then Return

            If _onButtonClick IsNot Nothing Then
                If ShowMSG(MW, $"У старого отказа номер  {selectedItem.OldId} будет изменен на {selectedItem.NewId}.{vbCrLf}Новый отказ № {selectedItem.NewId} будет удален", "ВНИМАНИЕ!", MsgButtons.OKCancel) Then

                    _onButtonClick.Invoke(selectedItem.OldId, selectedItem.NewId)

                    ' Удаляем обработанную пару из коллекции — строка исчезнет автоматически
                    _list.Remove(selectedItem)

                    If _list.Count < 1 Then
                        Me.Close()
                    End If
                End If
            End If
        End Sub

        Private Sub Cancel_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = TryCast(sender, Button)
            If btn Is Nothing Then Return

            Dim item = TryCast(btn.DataContext, IdMappingInfo)
            If item Is Nothing Then Return

            ' 1. Вызываем внешний колбэк — он очистит NewId и снимет пометку у старого отказа
            If _onCancelClick IsNot Nothing Then
                _onCancelClick.Invoke(item.OldId, item.NewId)
            End If

            ' 2. Удаляем строку из коллекции — DataGrid обновится сам
            _list.Remove(item)

            ' 3. Если список пуст — закрываем окно
            If _list.Count < 1 Then
                Me.Close()
            End If
        End Sub
    End Class
End Namespace














'Imports System.Collections.ObjectModel
'Imports System.Windows
'Imports System.Windows.Input
'Imports KACAHT_Next2.Kas.KasAntLoader
'Namespace Kas
'    Partial Public Class FrmChangedIds
'        Private _list As ObservableCollection(Of IdMappingInfo)
'        Private _onIdsSelected As Action(Of String, String)
'        Private _onButtonClick As Action(Of String, String) ' Новая команда для кнопки

'        Public Sub New(mappingList As ObservableCollection(Of IdMappingInfo), onIdsSelectedAction As Action(Of String, String), onButtonClickAction As Action(Of String, String))
'            InitializeComponent()
'            _list = mappingList
'            _onIdsSelected = onIdsSelectedAction
'            _onButtonClick = onButtonClickAction
'            dgvIds.ItemsSource = _list
'        End Sub

'        Private Sub DataGrid_MouseUp(sender As Object, e As MouseButtonEventArgs)
'            Dim selectedItem = TryCast(dgvIds.SelectedItem, IdMappingInfo)
'            If selectedItem Is Nothing Then Return

'            ' Вызываем переданное действие (которое отфильтрует список и вызовет AddOTSToContainer)
'            If _onIdsSelected IsNot Nothing Then
'                _onIdsSelected.Invoke(selectedItem.OldId, selectedItem.NewId)
'                selectedItem.IsReady = True
'            End If

'            ' Окно остается открытым для выбора следующей пары
'        End Sub
'        Private Sub Button_Click(sender As Object, e As RoutedEventArgs)
'            Dim btn = TryCast(sender, Button)
'            If btn Is Nothing Then Return



'            ' Получаем объект строки из DataContext кнопки 
'            Dim selectedItem = TryCast(btn.DataContext, IdMappingInfo)
'            If selectedItem Is Nothing Then Return

'            If _onButtonClick IsNot Nothing Then
'                '_onIdsSelected.Invoke(selectedItem.OldId, selectedItem.NewId)
'                If ShowMSG(MW, $"У старого отказа номер  {selectedItem.OldId} будет изменен на {selectedItem.NewId}.{vbCrLf}Новый отказ № {selectedItem.NewId} будет удален", "ВНИМАНИЕ!", MsgButtons.OKCancel) Then

'                    _onButtonClick.Invoke(selectedItem.OldId, selectedItem.NewId)

'                End If
'                If _list.Count < 1 Then
'                    Me.Close()
'                End If

'            End If
'        End Sub
'    End Class
'End Namespace
