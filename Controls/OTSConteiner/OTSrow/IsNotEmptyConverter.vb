Imports System.Globalization

Namespace Kas

    Public Class IsNotEmptyConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            ' 1. Null/UnsetValue
            If value Is Nothing OrElse value Is DependencyProperty.UnsetValue Then Return False

            ' 2. Дата
            If TypeOf value Is DateTime Then
                Return DirectCast(value, DateTime) <> DateTime.MinValue
            End If

            ' 3. Строка
            Dim strValue As String = TryCast(value, String)
            If strValue IsNot Nothing Then
                If String.IsNullOrWhiteSpace(strValue) OrElse
                   strValue = "!" OrElse
                   strValue.Contains("---") Then
                    Return False
                End If
                Return True
            End If

            ' 4. Числовые типы — 0 считается пустым
            If TypeOf value Is Integer Then
                Return DirectCast(value, Integer) <> 0
            End If
            If TypeOf value Is Long Then
                Return DirectCast(value, Long) <> 0
            End If
            If TypeOf value Is Double Then
                Return DirectCast(value, Double) <> 0
            End If
            If TypeOf value Is Single Then
                Return DirectCast(value, Single) <> 0
            End If

            ' 5. Boolean
            If TypeOf value Is Boolean Then
                Return DirectCast(value, Boolean)
            End If

            ' 6. Остальные типы
            Return True
        End Function
        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotImplementedException() ' Односторонний конвертер
        End Function

    End Class
End Namespace

