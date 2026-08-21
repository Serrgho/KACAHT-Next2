Imports System.Linq
Imports System.Windows.Threading
Namespace Kas
    Partial Public Class OtkazInspectorWindow

        ' Память о последнем выбранном свойстве (общая для всех экземпляров окна)
        Private Shared _lastSelectedPropertyName As String = Nothing

        Public Sub New(otkaz As Otkaz)
            InitializeComponent()
            UpdateData(otkaz)
        End Sub

        Public Sub UpdateData(otkaz As Otkaz)
            If otkaz IsNot Nothing Then
                Me.Title = $"Инспектор отказа #{otkaz.Id}"
                Dim data = otkaz.GetInspectorData()
                dgInspector.ItemsSource = data

                ' Статистика
                Dim propsCount As Integer = 0
                Dim methodsCount As Integer = 0
                For Each item In data
                    If item.Category = "Свойство" Then propsCount += 1
                    If item.Category = "Метод" Then methodsCount += 1
                Next
                txtStats.Text = $"Отказ № {otkaz.Id}  |  Место: {otkaz.MestoOTS}"
                RestoreSelection(data)
                '' Отложенный вызов - ждём пока DataGrid отрисует строки
                'Dispatcher.BeginInvoke(DispatcherPriority.Background, Sub()
                '                                                          RestoreSelection(data)
                '                                                      End Sub)
            End If
        End Sub

        Private Sub RestoreSelection(data As List(Of InspectorItem))
            If String.IsNullOrEmpty(_lastSelectedPropertyName) Then Return

            Dim targetIndex = data.FindIndex(Function(i) i.Name = _lastSelectedPropertyName)
            If targetIndex < 0 Then Return

            Dim row = TryCast(dgInspector.ItemContainerGenerator.ContainerFromIndex(targetIndex), DataGridRow)

            If row IsNot Nothing Then
                row.IsSelected = True
                row.Focus()
                dgInspector.ScrollIntoView(row)
            Else
                dgInspector.SelectedIndex = targetIndex
                dgInspector.ScrollIntoView(dgInspector.Items(targetIndex))
            End If
        End Sub

        Private Sub DgInspector_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            Dim selectedItem = TryCast(dgInspector.SelectedItem, InspectorItem)
            If selectedItem IsNot Nothing Then
                _lastSelectedPropertyName = selectedItem.Name
            End If
        End Sub

    End Class
End Namespace


