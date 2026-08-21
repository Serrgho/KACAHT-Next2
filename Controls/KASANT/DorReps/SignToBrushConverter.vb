
Imports System.Globalization

Namespace Kas
    Public Class SignToBrushConverter
        Implements IValueConverter

        ' ✅ Явный публичный конструктор по умолчанию
        Public Sub New()
        End Sub

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            If value Is Nothing Then Return System.Windows.Media.Brushes.Black

            Dim num As Integer
            If Not Integer.TryParse(value.ToString(), num) Then Return System.Windows.Media.Brushes.Black

            If num = 0 Then Return System.Windows.Media.Brushes.Gray

            Dim inverse = (parameter IsNot Nothing AndAlso parameter.ToString().Equals("inverse", StringComparison.OrdinalIgnoreCase))

            ' Для "Просрочено" (inverse=True): уменьшение = хорошо (зелёный), увеличение = плохо (красный)
            ' Для остальных (inverse=False): увеличение = хорошо (зелёный), уменьшение = плохо (красный)
            Dim isGood = If(inverse, num < 0, num > 0)

            Return If(isGood, System.Windows.Media.Brushes.DarkGreen, System.Windows.Media.Brushes.DarkRed)
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class


    ''' <summary>
    ''' Конвертер числа в строку со знаком "+" для положительных значений.
    ''' Пример: 5 → "+5", -3 → "-3", 0 → "0"
    ''' </summary>
    Public Class DeltaToStringConverter
        Implements IValueConverter

        Public Sub New()
        End Sub

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            If value Is Nothing Then Return "0"

            Dim num As Integer
            If Not Integer.TryParse(value.ToString(), num) Then Return "0"

            If num > 0 Then
                Return "+" & num.ToString()
            ElseIf num < 0 Then
                Return num.ToString()
            Else
                Return "0"
            End If
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class

End Namespace

