Imports System.Globalization
Imports System.Windows
Imports System.Windows.Data

Namespace Kas

    Public Class MileageToVisibilityConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            ' Если ничего нет - скрываем
            If value Is Nothing OrElse value Is DBNull.Value Then
                Return Visibility.Collapsed
            End If

            ' Пытаемся привести к числу
            Dim mileage As Integer
            If Integer.TryParse(value.ToString(), mileage) Then
                If mileage > 0 Then
                    Return Visibility.Visible
                End If
            End If

            Return Visibility.Collapsed
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Return Nothing
        End Function


        'Implements IValueConverter

        'Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
        '    ' Скрываем, если Nothing или 0
        '    Return If(value IsNot Nothing AndAlso CInt(value) > 0, Visibility.Visible, Visibility.Collapsed)
        'End Function

        'Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        '    Throw New NotImplementedException()
        'End Function
    End Class

End Namespace


