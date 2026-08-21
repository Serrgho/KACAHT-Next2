Imports System.Text
Imports System.Text.RegularExpressions

Public Class TextProcessor
    ' Разделение текста на две части
    Public Shared Function SplitText(input As String) As Tuple(Of String, String)


        If String.IsNullOrEmpty(input) Then Return New Tuple(Of String, String)("", "")

        ' Разбиваем на строки с сохранением оригинальных переносов
        Dim lines() As String = Regex.Split(input, "(\r\n|\n\r|\n|\r)")

        Dim part1Builder As New System.Text.StringBuilder()
        Dim part2Builder As New System.Text.StringBuilder()
        Dim foundEvent As Boolean = False

        For i As Integer = 0 To lines.Length - 1
            Dim line As String = lines(i)
            Dim trimmed As String = line.Trim()

            ' === 1. Пустая строка = начало истории (двойной перенос как визуальный разделитель) ===
            Dim isEmptyLine As Boolean = String.IsNullOrWhiteSpace(line)

            ' === 2. Строка-событие: дефис, дата или ключевая фраза в начале ===
            Dim isEventLine As Boolean = (
                trimmed.StartsWith("-") OrElse
                Regex.IsMatch(trimmed, "^\d{2}\.\d{2}\.\d{4}") OrElse
                trimmed.StartsWith("изменено количество поездов", StringComparison.OrdinalIgnoreCase)
            )

            ' Триггерим историю: пустая строка ИЛИ строка-событие (но только если ещё не в истории)
            If Not foundEvent AndAlso (isEmptyLine OrElse isEventLine) Then
                foundEvent = True
            End If

            ' Распределяем строку
            If foundEvent Then
                part2Builder.Append(line)
            Else
                part1Builder.Append(line)
            End If
        Next

        Return New Tuple(Of String, String)(
            part1Builder.ToString().Trim(),
            part2Builder.ToString().Trim()
        )




    End Function




    ' Разделение второй части на "события"
    Public Shared Function SplitEvents(input As String) As List(Of String)

        If String.IsNullOrEmpty(input) Then Return New List(Of String)

        ' Разбиваем по ВСЕМ типам переносов строк (vbCrLf, vbLf, vbCr)
        Dim lines As String() = Regex.Split(input, "\r?\n|\r")

        Dim eventsList As New List(Of String)()

        For Each line As String In lines
            ' 1. Убираем пробелы по краям
            Dim trimmed = line.Trim()

            ' 2. Удаляем ВСЕ начальные дефисы + пробелы после них
            ' Примеры:
            '   "-корректировка"      → "корректировка"
            '   "- 01.01.2024"        → "01.01.2024"
            '   "-- примечание"       → "примечание"
            '   "ТО-2 проходил"       → "ТО-2 проходил" (дефис внутри — не удаляется!)
            trimmed = trimmed.TrimStart("-"c).TrimStart()

            ' 3. Добавляем только непустые строки
            If Not String.IsNullOrEmpty(trimmed) Then
                eventsList.Add(trimmed)
            End If
        Next

        Return eventsList



    End Function

End Class
