Imports System.Globalization
Imports System.Text.RegularExpressions

Namespace Kas

    ''' <summary>
    ''' Данные о ремонте локомотива
    ''' </summary>
    Public Class Remont
        Public Property RepairType As String = String.Empty

        ''' <summary>
        ''' Вид ремонта (ТР-1, ТО-2, КР и т.д.)
        ''' </summary>
        Private _repairPlace As String = String.Empty
        ''' <summary>Место проведения ремонта</summary>
        Public Property RepairPlace As String
            Get
                Return _repairPlace
            End Get
            Set(value As String)
                If String.IsNullOrWhiteSpace(value) Then
                    _repairPlace = String.Empty
                    Return
                End If

                Dim cleaned = value.Trim()
                ' Только мусор по краям: точки, запятые, двоеточия, тире
                cleaned = Regex.Replace(cleaned, "^[.,;:\-\(\)\[\]\s]+|[.,;:\-\(\)\[\]\s]+$", "", RegexOptions.IgnoreCase)
                ' Нормализуем пробелы
                cleaned = Regex.Replace(cleaned, "\s+", " ").Trim()
                _repairPlace = cleaned
            End Set
        End Property

        Public Property Mileage As Integer?

        Public Overrides Function ToString() As String
            Dim mileageStr = If(Mileage.HasValue, $" {Mileage.Value} км", "")
            Return $"{RepairType}{mileageStr} {RepairPlace}".Trim()
        End Function
    End Class


    Public Class MileageToVisibilityConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            ' Скрываем, если Nothing или 0
            Return If(value IsNot Nothing AndAlso CInt(value) > 0, Visibility.Visible, Visibility.Collapsed)
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class



End Namespace


