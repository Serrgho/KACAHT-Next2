Namespace Kas
    Public Class NotesToVisibilityConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.Convert



            Dim notes = CType(value, String)
            Dim searchFor = CType(parameter, String)

            If String.IsNullOrEmpty(notes) OrElse String.IsNullOrEmpty(searchFor) Then
                If targetType Is GetType(String) Then
                    Return ""
                Else
                    Return Visibility.Collapsed
                End If
            End If

            ' Разбиваем на части по ";"
            Dim parts = notes.Split(";"c).Select(Function(part) part.Trim()).Where(Function(s) s.Length > 0).ToList()

            ' Ищем подстроку (например, "за ")
            For Each part In parts
                If part.StartsWith(searchFor, StringComparison.OrdinalIgnoreCase) Then
                    If targetType Is GetType(String) Then
                        Return part  ' ← Возвращаем найденную часть
                    Else
                        Return Visibility.Visible  ' ← Для Visibility
                    End If
                End If

            Next

            ' Ищем точное совпадение (для других случаев)
            For Each part In parts
                If String.Equals(part, searchFor, StringComparison.OrdinalIgnoreCase) Then
                    If targetType Is GetType(String) Then
                        Return part  ' ← Возвращаем найденную часть
                    Else
                        Return Visibility.Visible  ' ← Для Visibility
                    End If
                End If
            Next

            If targetType Is GetType(String) Then
                Return ""
            Else
                Return Visibility.Collapsed
            End If
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As Globalization.CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class

End Namespace

