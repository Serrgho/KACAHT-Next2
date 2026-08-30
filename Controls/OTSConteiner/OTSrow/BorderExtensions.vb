Namespace Kas
    Public Class BorderExtensions
        ' ✅ Было: GetType(Object) → Стало: GetType(Boolean)
        Public Shared ReadOnly HasTextProperty As DependencyProperty =
        DependencyProperty.RegisterAttached(
            "HasText",
            GetType(Boolean), ' <-- Ключевое исправление
            GetType(BorderExtensions),
            New FrameworkPropertyMetadata(False, FrameworkPropertyMetadataOptions.Inherits)
        )

        Public Shared Sub SetHasText(element As DependencyObject, value As Boolean)
            element.SetValue(HasTextProperty, value)
        End Sub

        Public Shared Function GetHasText(element As DependencyObject) As Boolean
            Return DirectCast(element.GetValue(HasTextProperty), Boolean)
        End Function
    End Class

End Namespace


