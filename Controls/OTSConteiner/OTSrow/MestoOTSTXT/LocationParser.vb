Imports System.Globalization
Imports System.Text.RegularExpressions

Namespace Kas
    ''' <summary>
    ''' модуль парсинга MESTO OTS TXT + DiffTextConverter, PlaceInfoFormatterConverter
    ''' </summary>
    Public Module LocationParser

        ''' <summary>
        ''' Удаляет дубликаты номеров поездов и сортирует их по возрастанию.
        ''' ИЗМЕНЯЕТ ИСХОДНЫЙ ТЕКСТ
        ''' </summary>
        Public Function RemoveDoubleTrains(raw As String) As String
            If String.IsNullOrEmpty(raw) Then Return raw

            ' Ищем "поезда №" и извлекаем номера
            Dim pattern = "поезда?\s*№\s*([\d,\s]+)"
            Dim match = Regex.Match(raw, pattern, RegexOptions.IgnoreCase)

            If match.Success Then
                Dim prefix = raw.Substring(0, match.Index)
                Dim numbersPart = match.Groups(1).Value

                ' Извлекаем все числа
                Dim numbers = Regex.Matches(numbersPart, "\d+").Cast(Of Match)().
                              Select(Function(m) Integer.Parse(m.Value)).
                              Distinct().
                              OrderBy(Function(n) n).
                              ToList()

                If numbers.Any() Then
                    ' Восстанавливаем с сохранением формата "поезда №"
                    Dim newSuffix = "поезда №" & String.Join(", ", numbers)
                    Return prefix & newSuffix
                End If
            End If

            Return raw
        End Function

        ''' <summary>
        ''' Парсит текст места отказа на само место и дорогу.
        ''' НЕ ИЗМЕНЯЕТ ИСХОДНЫЙ ТЕКСТ
        ''' </summary>
        Public Function ParseLocationAndRoad(source As String) As Tuple(Of String, String)

            If String.IsNullOrEmpty(source) Then
                Return Tuple.Create("", "Красноярская")
            End If

            Dim text = source.Trim()
            Dim place As String = ""
            Dim road As String = "Красноярская"

            ' Ищем позицию "РЕГ"
            Dim regPos = text.IndexOf("РЕГ", StringComparison.OrdinalIgnoreCase)
            If regPos >= 0 Then
                ' Определяем дорогу (всё до "РЕГ")
                If regPos > 0 Then
                    road = text.Substring(0, regPos).Trim().TrimEnd(","c)
                End If

                ' Ищем запятую после "РЕГ"
                Dim commaPos = text.IndexOf(","c, regPos)
                If commaPos >= 0 Then
                    ' Пропускаем все пробелы после запятой (1, 2, 3 или 0)
                    Dim startPos = commaPos + 1
                    While startPos < text.Length AndAlso Char.IsWhiteSpace(text(startPos))
                        startPos += 1
                    End While

                    ' Ищем "поезда" или конец строки
                    Dim endPos = text.IndexOf("поезда", startPos, StringComparison.OrdinalIgnoreCase)
                    If endPos < 0 Then endPos = text.Length

                    place = text.Substring(startPos, endPos - startPos).Trim()

                    ' Если в месте есть "путь", чистим пробелы вокруг тире (только для места!)
                    Dim pathIndex As Integer = place.IndexOf("путь", StringComparison.OrdinalIgnoreCase)
                    If pathIndex >= 0 Then
                        Dim afterPath As String = place.Substring(pathIndex + 4)
                        afterPath = Regex.Replace(afterPath, "\s*-\s*", "-")
                        place = place.Substring(0, pathIndex + 4) & afterPath
                    End If
                End If
            Else
                ' Если нет "РЕГ", берём всё до первой запятой
                Dim commaPos = text.IndexOf(",")
                place = If(commaPos > 0, text.Substring(0, commaPos).Trim(), text)
            End If

            Return Tuple.Create(place, road)

            'If String.IsNullOrEmpty(source) Then
            '    Return Tuple.Create("", "Красноярская")
            'End If

            'Dim text = source.Trim()
            'Dim place As String = ""
            'Dim road As String = "Красноярская"
            'Dim regMarker As String = ""  ' ← НОВОЕ: храним "РЕГ-2,  " целиком

            'Dim regPos = text.IndexOf("РЕГ", StringComparison.OrdinalIgnoreCase)
            'If regPos >= 0 Then
            '    ' Дорога (всё до "РЕГ")
            '    If regPos > 0 Then
            '        road = text.Substring(0, regPos).Trim().TrimEnd(","c)
            '    End If

            '    ' Ищем запятую после "РЕГ"
            '    Dim commaPos = text.IndexOf(","c, regPos)
            '    If commaPos >= 0 Then
            '        ' ★ Запоминаем маркер ОТ "РЕГ" ДО первого непробельного символа
            '        Dim markerEnd = commaPos + 1
            '        While markerEnd < text.Length AndAlso Char.IsWhiteSpace(text(markerEnd))
            '            markerEnd += 1
            '        End While
            '        regMarker = text.Substring(regPos, markerEnd - regPos)

            '        ' Место
            '        Dim startPos = markerEnd
            '        Dim endPos = text.IndexOf("поезда", startPos, StringComparison.OrdinalIgnoreCase)
            '        If endPos < 0 Then endPos = text.Length

            '        place = text.Substring(startPos, endPos - startPos).Trim()

            '        ' Чистка пробелов вокруг тире в "путь"
            '        Dim pathIndex = place.IndexOf("путь", StringComparison.OrdinalIgnoreCase)
            '        If pathIndex >= 0 Then
            '            Dim afterPath = place.Substring(pathIndex + 4)
            '            afterPath = Regex.Replace(afterPath, "\s*-\s*", "-")
            '            place = place.Substring(0, pathIndex + 4) & afterPath
            '        End If
            '    Else
            '        ' Запятой нет — берём всё от "РЕГ" до конца как маркер
            '        regMarker = text.Substring(regPos)
            '    End If
            'Else
            '    Dim commaPos = text.IndexOf(",")
            '    place = If(commaPos > 0, text.Substring(0, commaPos).Trim(), text)
            'End If

            'Return Tuple.Create(place, road)
        End Function

        ''' <summary>
        ''' Проверяет, является ли номер поезда пассажирским или пригородным.
        ''' </summary>
        Public Function IsPassengerOrSuburban(number As Integer) As Boolean
            Return number < 700 OrElse (number >= 6000 AndAlso number < 7000)
        End Function

        ''' <summary>
        ''' Проверяет, есть ли в тексте пассажирские/пригородные поезда.
        ''' </summary>
        Public Function HasPassInText(text As String) As Boolean
            If String.IsNullOrEmpty(text) Then Return False

            ' Сначала удаляем дубли для чистоты проверки
            text = RemoveDoubleTrains(text)

            Dim trainPos = text.IndexOf("поезда", StringComparison.OrdinalIgnoreCase)
            If trainPos < 0 Then Return False

            Dim hashPos = text.IndexOf("№", trainPos, StringComparison.OrdinalIgnoreCase)
            If hashPos < 0 Then Return False

            Dim numbersPart = text.Substring(hashPos + 1).Trim()
            Dim parts = numbersPart.Split(","c)

            For Each part In parts
                Dim numStr = part.Trim()
                Dim num As Integer
                If Integer.TryParse(numStr, num) Then
                    If IsPassengerOrSuburban(num) Then
                        Return True
                    End If
                End If
            Next

            Return False
        End Function

    End Module

    Public Class PlaceInfoFormatterConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert

            Dim text As String = TryCast(value, String)

            Dim tb As New TextBlock() With {
                .TextWrapping = TextWrapping.Wrap,
                .VerticalAlignment = VerticalAlignment.Center,
                .TextAlignment = TextAlignment.Center
            }

            If String.IsNullOrWhiteSpace(text) Then Return tb

            ' === ЕДИНСТВЕННОЕ ИЗМЕНЕНИЕ ИСХОДНОГО ТЕКСТА ===
            text = LocationParser.RemoveDoubleTrains(text)

            ' === ИСПОЛЬЗУЕМ ГОТОВЫЙ ПАРСИНГ ===
            Dim parsed = LocationParser.ParseLocationAndRoad(text)
            Dim place As String = parsed.Item1
            Dim road As String = parsed.Item2

            ' Если парсинг не дал результата, выводим как есть
            If String.IsNullOrEmpty(place) Then
                tb.Inlines.Add(New Run(text))
                Return tb
            End If

            ' === ФОРМИРУЕМ ВЫВОД С СОХРАНЕНИЕМ ВСЕХ ПРОБЕЛОВ ===

            ' Находим позицию "РЕГ"
            Dim regPos = text.IndexOf("РЕГ", StringComparison.OrdinalIgnoreCase)
            If regPos < 0 Then
                tb.Inlines.Add(New Run(text))
                Return tb
            End If

            ' 1. Всё что до "РЕГ" (обычный текст)
            Dim beforeReg As String = text.Substring(0, regPos).TrimEnd()
            If Not String.IsNullOrWhiteSpace(beforeReg) Then
                tb.Inlines.Add(New Run(beforeReg))
                tb.Inlines.Add(New LineBreak())
            End If

            ' 2. Находим запятую после "РЕГ" и считаем ВСЕ пробелы за ней
            Dim commaPos = text.IndexOf(","c, regPos)
            If commaPos < 0 Then
                tb.Inlines.Add(New Run(text.Substring(regPos)))
                Return tb
            End If

            Dim markerEnd = commaPos + 1
            While markerEnd < text.Length AndAlso Char.IsWhiteSpace(text(markerEnd))
                markerEnd += 1
            End While

            ' 3. "РЕГ-2,  " — берём из исходника с оригинальными пробелами
            Dim regWithComma As String = text.Substring(regPos, markerEnd - regPos)
            tb.Inlines.Add(New Run(regWithComma))

            ' 4. МЕСТО (жирный шрифт) - используем распарсенное место
            tb.Inlines.Add(New Run(place) With {.FontWeight = FontWeights.Bold})

            ' 5. Всё что после места (обычный текст)
            '    Ищем реальную позицию place в исходнике ОТ markerEnd
            Dim placeInSource = text.IndexOf(place, markerEnd, StringComparison.OrdinalIgnoreCase)
            Dim afterPlaceStart As Integer
            If placeInSource >= 0 Then
                afterPlaceStart = placeInSource + place.Length
            Else
                ' Fallback: если place не найден (почищенные тире у путей)
                afterPlaceStart = markerEnd + place.Length
            End If

            If afterPlaceStart < text.Length Then
                Dim afterPlace As String = text.Substring(afterPlaceStart)

                ' Проверяем, есть ли "поезда"
                If afterPlace.Trim().StartsWith("поезда", StringComparison.OrdinalIgnoreCase) Then
                    tb.Inlines.Add(New LineBreak())

                    ' Разбираем поезда
                    Dim trainsMatch = Regex.Match(afterPlace, "(поезда\s*№\s*)([\d,\s]+)", RegexOptions.IgnoreCase)
                    If trainsMatch.Success Then
                        Dim labelPart As String = trainsMatch.Groups(1).Value
                        Dim numbersPart As String = trainsMatch.Groups(2).Value

                        tb.Inlines.Add(New Run(labelPart))

                        ' Раскрашиваем номера
                        Dim numberStrings = numbersPart.Split(","c)
                        For i As Integer = 0 To numberStrings.Length - 1
                            Dim numStr = numberStrings(i).Trim()
                            Dim run = New Run(numStr)

                            Dim number As Integer
                            If Integer.TryParse(numStr, number) Then
                                If LocationParser.IsPassengerOrSuburban(number) Then
                                    run.Foreground = Brushes.Red
                                End If
                            End If

                            tb.Inlines.Add(run)

                            If i < numberStrings.Length - 1 Then
                                tb.Inlines.Add(New Run(", "))
                            End If
                        Next
                    Else
                        tb.Inlines.Add(New Run(afterPlace))
                    End If
                Else
                    tb.Inlines.Add(New Run(afterPlace))
                End If
            End If

            Return tb





            'Dim text As String = TryCast(value, String)

            'Dim tb As New TextBlock() With {
            '    .TextWrapping = TextWrapping.Wrap,
            '    .VerticalAlignment = VerticalAlignment.Center,
            '    .TextAlignment = TextAlignment.Center
            '}

            'If String.IsNullOrWhiteSpace(text) Then Return tb

            '' === ЕДИНСТВЕННОЕ ИЗМЕНЕНИЕ ИСХОДНОГО ТЕКСТА ===
            'text = LocationParser.RemoveDoubleTrains(text)

            '' === ИСПОЛЬЗУЕМ ГОТОВЫЙ ПАРСИНГ ===
            'Dim parsed = LocationParser.ParseLocationAndRoad(text)
            'Dim place As String = parsed.Item1
            'Dim road As String = parsed.Item2

            '' Если парсинг не дал результата, выводим как есть
            'If String.IsNullOrEmpty(place) Then
            '    tb.Inlines.Add(New Run(text))
            '    Return tb
            'End If

            '' === ФОРМИРУЕМ ВЫВОД С СОХРАНЕНИЕМ ВСЕХ ПРОБЕЛОВ ===

            '' Находим позицию "РЕГ"
            'Dim regPos = text.IndexOf("РЕГ", StringComparison.OrdinalIgnoreCase)
            'If regPos < 0 Then
            '    tb.Inlines.Add(New Run(text))
            '    Return tb
            'End If

            '' 1. Всё что до "РЕГ" (обычный текст)
            'Dim beforeReg As String = text.Substring(0, regPos).TrimEnd()
            'If Not String.IsNullOrWhiteSpace(beforeReg) Then
            '    tb.Inlines.Add(New Run(beforeReg))
            '    tb.Inlines.Add(New LineBreak())
            'End If

            '' 2. Находим ",  " после "РЕГ"

            'Dim markerPos = text.IndexOf(", ", regPos)
            'If markerPos < 0 Then
            '    tb.Inlines.Add(New Run(text.Substring(regPos)))
            '    Return tb
            'End If

            '' 3. "РЕГ" + ",  " (обычный текст)
            'Dim regWithComma As String = text.Substring(regPos, markerPos + 3 - regPos)
            'tb.Inlines.Add(New Run(regWithComma))



            '' 4. МЕСТО (жирный шрифт) - используем распарсенное место
            'tb.Inlines.Add(New Run(place) With {.FontWeight = FontWeights.Bold})

            '' 5. Всё что после места (обычный текст)
            'Dim afterPlaceStart = regPos + regWithComma.Length + place.Length
            'If afterPlaceStart < text.Length Then
            '    Dim afterPlace As String = text.Substring(afterPlaceStart)

            '    ' Проверяем, есть ли "поезда"
            '    If afterPlace.Trim().StartsWith("поезда", StringComparison.OrdinalIgnoreCase) Then
            '        tb.Inlines.Add(New LineBreak())

            '        ' Разбираем поезда
            '        Dim trainsMatch = Regex.Match(afterPlace, "(поезда\s*№\s*)([\d,\s]+)", RegexOptions.IgnoreCase)
            '        If trainsMatch.Success Then
            '            Dim labelPart As String = trainsMatch.Groups(1).Value
            '            Dim numbersPart As String = trainsMatch.Groups(2).Value

            '            tb.Inlines.Add(New Run(labelPart))

            '            ' Раскрашиваем номера
            '            Dim numberStrings = numbersPart.Split(","c)
            '            For i As Integer = 0 To numberStrings.Length - 1
            '                Dim numStr = numberStrings(i).Trim()
            '                Dim run = New Run(numStr)

            '                Dim number As Integer
            '                If Integer.TryParse(numStr, number) Then
            '                    If LocationParser.IsPassengerOrSuburban(number) Then
            '                        run.Foreground = Brushes.Red
            '                    End If
            '                End If

            '                tb.Inlines.Add(run)

            '                If i < numberStrings.Length - 1 Then
            '                    tb.Inlines.Add(New Run(", "))
            '                End If
            '            Next
            '        Else
            '            tb.Inlines.Add(New Run(afterPlace))
            '        End If
            '    Else
            '        tb.Inlines.Add(New Run(afterPlace))
            '    End If
            'End If

            'Return tb

        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Return DependencyProperty.UnsetValue
        End Function
    End Class

    Public Class DiffTextConverter
        Implements IValueConverter
        Private Shared ReadOnly _tokenRegex As New Regex("\b[\p{L}\d]+(?:-[\p{L}\d]+)*\b", RegexOptions.Compiled)
        Private Shared ReadOnly _trainRegex As New Regex("поезда\s*№", RegexOptions.Compiled Or RegexOptions.IgnoreCase)

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            Dim otkaz = TryCast(value, Otkaz)

            If otkaz Is Nothing Then
                Return CreateTextBlock("Нет данных")
            End If

            If String.IsNullOrEmpty(otkaz.PreviousMestoOTS_TXT) Then
                Return CreateTextBlock("Нет данных об изменениях")
            End If

            ' Извлекаем токены для сравнения
            Dim oldTokens = ExtractTokens(otkaz.PreviousMestoOTS_TXT)
            Dim newTokens = ExtractTokens(otkaz.MestoOTS_TXT)

            ' Сравниваем и получаем изменения
            Dim removedDict = GetRemovedTokens(oldTokens, newTokens)
            Dim addedDict = GetAddedTokens(oldTokens, newTokens)

            ' Формируем результат
            Dim tb As New TextBlock() With {
                .FontSize = 12,
                .TextWrapping = TextWrapping.Wrap
            }

            ' БЫЛО
            tb.Inlines.Add(New Run("Было: ") With {.FontWeight = FontWeights.Bold})
            AppendHighlightedText(tb.Inlines, otkaz.PreviousMestoOTS_TXT, removedDict, isRemoved:=True)

            tb.Inlines.Add(New LineBreak())

            ' СТАЛО
            tb.Inlines.Add(New Run("Стало: ") With {.FontWeight = FontWeights.Bold})
            AppendHighlightedText(tb.Inlines, otkaz.MestoOTS_TXT, addedDict, isRemoved:=False)

            Return tb
        End Function

        Private Function CreateTextBlock(message As String) As TextBlock
            Dim tb As New TextBlock()
            tb.Inlines.Add(New Run(message))
            Return tb
        End Function

        ''' <summary>
        ''' Извлекает значимые токены из текста
        ''' </summary>
        Private Function ExtractTokens(text As String) As List(Of String)
            If String.IsNullOrEmpty(text) Then Return New List(Of String)

            ' Убираем "поезда №" чтобы номера поездов не цеплялись за этот текст
            Dim cleanText = _trainRegex.Replace(text, " ")

            Dim tokens As New List(Of String)
            Dim matches = _tokenRegex.Matches(cleanText)

            For Each m As Match In matches
                Dim token = m.Value
                ' Пропускаем служебные слова
                If Not String.Equals(token, "поезда", StringComparison.OrdinalIgnoreCase) AndAlso
                   Not String.Equals(token, "№", StringComparison.OrdinalIgnoreCase) Then
                    tokens.Add(token)
                End If
            Next

            Return tokens
        End Function

        ''' <summary>
        ''' Возвращает словарь удаленных токенов с их количеством
        ''' </summary>
        Private Function GetRemovedTokens(oldTokens As List(Of String), newTokens As List(Of String)) As Dictionary(Of String, Integer)
            Dim oldFreq = GetTokenFrequencies(oldTokens)
            Dim newFreq = GetTokenFrequencies(newTokens)

            Dim result As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)

            For Each kvp In oldFreq
                Dim oldCount = kvp.Value
                Dim newCount = If(newFreq.TryGetValue(kvp.Key, 0), newFreq(kvp.Key), 0)
                Dim diff = oldCount - newCount
                If diff > 0 Then
                    result(kvp.Key) = diff
                End If
            Next

            Return result
        End Function

        ''' <summary>
        ''' Возвращает словарь добавленных токенов с их количеством
        ''' </summary>
        Private Function GetAddedTokens(oldTokens As List(Of String), newTokens As List(Of String)) As Dictionary(Of String, Integer)
            Dim oldFreq = GetTokenFrequencies(oldTokens)
            Dim newFreq = GetTokenFrequencies(newTokens)

            Dim result As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)

            For Each kvp In newFreq
                Dim oldCount = If(oldFreq.TryGetValue(kvp.Key, 0), oldFreq(kvp.Key), 0)
                Dim diff = kvp.Value - oldCount
                If diff > 0 Then
                    result(kvp.Key) = diff
                End If
            Next

            Return result
        End Function

        ''' <summary>
        ''' Считает частоту токенов
        ''' </summary>
        Private Function GetTokenFrequencies(tokens As List(Of String)) As Dictionary(Of String, Integer)
            Dim freq As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
            For Each t In tokens
                Dim count As Integer = 0
                If freq.TryGetValue(t, count) Then
                    freq(t) = count + 1
                Else
                    freq(t) = 1
                End If
            Next
            Return freq
        End Function

        ''' <summary>
        ''' Добавляет текст с выделением изменений
        ''' </summary>
        Private Sub AppendHighlightedText(inlines As InlineCollection, fullText As String, highlightDict As Dictionary(Of String, Integer), isRemoved As Boolean)
            If String.IsNullOrEmpty(fullText) Then
                inlines.Add(New Run(fullText))
                Return
            End If

            ' Создаем копию словаря для мутации
            Dim remainingHighlights As New Dictionary(Of String, Integer)(highlightDict, StringComparer.OrdinalIgnoreCase)

            Dim matches = _tokenRegex.Matches(fullText)
            Dim lastIndex = 0

            For Each m As Match In matches
                ' Текст до найденного токена
                If m.Index > lastIndex Then
                    inlines.Add(New Run(fullText.Substring(lastIndex, m.Index - lastIndex)))
                End If

                Dim word = m.Value

                ' Проверяем нужно ли выделять
                Dim count As Integer = 0
                If remainingHighlights.TryGetValue(word, count) AndAlso count > 0 Then
                    Dim run As New Run(word) With {
                        .FontWeight = FontWeights.Bold,
                        .FontSize = 14
                    }

                    If isRemoved Then
                        run.Foreground = Brushes.Blue
                        run.TextDecorations = TextDecorations.Strikethrough
                    Else
                        run.Foreground = Brushes.Red
                    End If

                    inlines.Add(run)

                    ' Уменьшаем счетчик
                    remainingHighlights(word) = count - 1
                Else
                    inlines.Add(New Run(word))
                End If

                lastIndex = m.Index + m.Length
            Next

            ' Оставшийся текст
            If lastIndex < fullText.Length Then
                inlines.Add(New Run(fullText.Substring(lastIndex)))
            End If
        End Sub

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotSupportedException("Этот конвертер предназначен только для OneWay-привязки.")
        End Function
    End Class

End Namespace
