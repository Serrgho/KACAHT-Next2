Imports System.Windows.Controls.Primitives

Namespace Kas
    Public Class FilterPopupHelper

        ''' <summary>
        ''' Создает и настраивает Popup, но НЕ открывает его и НЕ подписывает события.
        ''' Возвращает готовый Popup и список значений.
        ''' </summary>
        Public Shared Function PrepareTextualFilter(sender As Object, propName As String, fieldType As String, source As IEnumerable(Of Otkaz), filterState As FilterState, isSortByCount As Boolean) As Tuple(Of Popup, List(Of String))

            Dim isBooleanField As Boolean = (fieldType.ToLowerInvariant() = "boolean")
            Dim popupCtrl As New FilterPopup()

            ' 1. Получаем значения через конфиг
            Dim values As List(Of String)
            If source Is Nothing Then
                values = New List(Of String) From {"[Все]"}
            Else
                If isBooleanField Then
                    values = OtkazFilterConfig.GetBooleanValuesForProperty(source, propName)
                Else
                    values = OtkazFilterConfig.GetTextualValuesForProperty(source, propName)
                    values = OtkazFilterConfig.SortValuesByToggle(values, propName, isSortByCount)
                End If
            End If

            ' 2. Настраиваем контрол
            popupCtrl.PropertyName = propName
            popupCtrl.FieldType = fieldType
            popupCtrl.FilterListBox.ItemsSource = values

            ' 3. Восстанавливаем выделение
            Dim saved = filterState.GetFilter(propName)
            OtkazFilterConfig.RestoreSelection(popupCtrl, saved, isBooleanField, values)

            ' 4. Создаем Popup
            Dim newPopup As New Popup With {
                .Placement = PlacementMode.Bottom,
                .PlacementTarget = sender,
                .StaysOpen = False,
                .AllowsTransparency = True,
                .Child = popupCtrl
            }

            Return New Tuple(Of Popup, List(Of String))(newPopup, values)
        End Function

    End Class
End Namespace
