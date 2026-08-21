Imports Newtonsoft.Json
Imports Newtonsoft.Json.Converters
Imports System.Globalization

Public Class VBDateConverter
    Inherits DateTimeConverterBase

    Private Const DateFormat As String = "dd.MM.yyyy HH:mm"

    Public Overrides Function ReadJson(reader As JsonReader, objectType As Type, existingValue As Object, serializer As JsonSerializer) As Object
        If reader.TokenType = JsonToken.Null OrElse reader.Value Is Nothing Then
            Return Date.MinValue
        End If

        Dim s As String = reader.Value.ToString().Trim()

        If s = "01.01.0001 00:00" OrElse String.IsNullOrEmpty(s) Then
            Return Date.MinValue
        End If

        Dim dt As Date  ' ← вот она — объявляем переменную

        ' Пробуем ваш формат
        If Date.TryParseExact(s, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, dt) Then
            Return dt
        End If

        ' Fallback на стандартные форматы
        If Date.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, dt) Then
            Return dt
        End If

        Return Date.MinValue
    End Function

    Public Overrides Sub WriteJson(writer As JsonWriter, value As Object, serializer As JsonSerializer)
        Dim dt As Date = If(value Is Nothing, Date.MinValue, CType(value, Date))

        If dt = Date.MinValue Then
            writer.WriteValue("01.01.0001 00:00")
        Else
            writer.WriteValue(dt.ToString(DateFormat, CultureInfo.InvariantCulture))
        End If
    End Sub
End Class
