
Namespace Kas

    Public Class FilterProperties

        ' Attached Property для типа поля
        Public Shared Function GetFieldType(element As DependencyObject) As String
            Return CStr(element.GetValue(FieldTypeProperty))
        End Function

        Public Shared Sub SetFieldType(element As DependencyObject, value As String)
            element.SetValue(FieldTypeProperty, value)
        End Sub

        Public Shared ReadOnly FieldTypeProperty As DependencyProperty =
            DependencyProperty.RegisterAttached("FieldType", GetType(String), GetType(FilterProperties),
                                                New PropertyMetadata("String")) ' По умолчанию - String

        ' Attached Property для уровня фильтра
        Public Shared Function GetFilterLevel(element As DependencyObject) As String
            Return CStr(element.GetValue(FilterLevelProperty))
        End Function

        Public Shared Sub SetFilterLevel(element As DependencyObject, value As String)
            element.SetValue(FilterLevelProperty, value)
        End Sub

        Public Shared ReadOnly FilterLevelProperty As DependencyProperty =
            DependencyProperty.RegisterAttached("FilterLevel", GetType(String), GetType(FilterProperties),
                                                New PropertyMetadata("Main")) ' По умолчанию - Main

    End Class
End Namespace

