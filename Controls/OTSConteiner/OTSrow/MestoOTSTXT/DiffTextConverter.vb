Imports System.Globalization
Imports System.Text.RegularExpressions

Namespace Kas
    'Public Class DiffTextConverter
    'Implements IValueConverter


    'Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
    '    Dim o = TryCast(value, Otkaz)
    '    If o Is Nothing OrElse String.IsNullOrEmpty(o.PreviousMestoOTS_TXT) Then
    '        Dim tbl = New TextBlock()
    '        tbl.Inlines.Add(New Run("Нет данных об изменениях"))
    '        Return tbl
    '    End If

    '    Dim oldTokens = ExtractMeaningfulTokens(o.PreviousMestoOTS_TXT)
    '    Dim newTokens = ExtractMeaningfulTokens(o.MestoOTS_TXT)

    '    Dim diffResult = CompareTokenLists(oldTokens, newTokens)

    '    Dim removedCounts = GetTokenFrequencies(diffResult.Removed)
    '    Dim addedCounts = GetTokenFrequencies(diffResult.Added)

    '    Dim tb = New TextBlock()
    '    tb.FontSize = 12
    '    tb.TextWrapping = TextWrapping.Wrap

    '    ' БЫЛО
    '    tb.Inlines.Add(New Run("Было: ") With {.FontWeight = FontWeights.Bold})
    '    AppendHighlightedText(tb.Inlines, o.PreviousMestoOTS_TXT, removedCounts, isRemovedContext:=True)

    '    tb.Inlines.Add(New LineBreak())

    '    ' СТАЛО
    '    tb.Inlines.Add(New Run("Стало: ") With {.FontWeight = FontWeights.Bold})
    '    AppendHighlightedText(tb.Inlines, o.MestoOTS_TXT, addedCounts, isRemovedContext:=False)

    '    Return tb
    'End Function

    '' --- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ---

    '''' <summary>
    '''' Извлекает значимые токены (слова, номера, РЕГ-2) как список — с дублями!
    '''' </summary>
    'Private Function ExtractMeaningfulTokens(text As String) As List(Of String)
    '    If String.IsNullOrEmpty(text) Then Return New List(Of String)

    '    ' Убираем "поезда №", чтобы не мешало
    '    Dim cleanText = Regex.Replace(text, "поезда\s*№", " ", RegexOptions.IgnoreCase)

    '    ' Извлекаем слова, которые могут содержать буквы, цифры и внутренние дефисы
    '    Dim matches = Regex.Matches(cleanText, "\b[\p{L}\d]+(?:-[\p{L}\d]+)*\b")
    '    Dim tokens As New List(Of String)
    '    For Each m As Match In matches
    '        tokens.Add(m.Value)
    '    Next
    '    Return tokens
    'End Function

    '''' <summary>
    '''' Сравнивает два списка токенов с учётом количества (частоты)
    '''' </summary>
    'Private Function CompareTokenLists(oldTokens As List(Of String), newTokens As List(Of String)) As (Removed As List(Of String), Added As List(Of String))
    '    Dim oldFreq = GetTokenFrequencies(oldTokens)
    '    Dim newFreq = GetTokenFrequencies(newTokens)

    '    Dim removed As New List(Of String)
    '    Dim added As New List(Of String)

    '    ' Что было, но стало меньше → удалено
    '    For Each key In oldFreq.Keys
    '        Dim oldCount = oldFreq(key)
    '        Dim newCount = If(newFreq.ContainsKey(key), newFreq(key), 0)
    '        For i As Integer = 1 To oldCount - newCount
    '            removed.Add(key)
    '        Next
    '    Next

    '    ' Что стало больше → добавлено
    '    For Each key In newFreq.Keys
    '        Dim oldCount = If(oldFreq.ContainsKey(key), oldFreq(key), 0)
    '        Dim newCount = newFreq(key)
    '        For i As Integer = 1 To newCount - oldCount
    '            added.Add(key)
    '        Next
    '    Next

    '    Return (removed, added)
    'End Function

    '''' <summary>
    '''' Считает частоту каждого токена
    '''' </summary>
    'Private Function GetTokenFrequencies(tokens As List(Of String)) As Dictionary(Of String, Integer)
    '    Dim freq As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
    '    For Each t In tokens
    '        If freq.ContainsKey(t) Then
    '            freq(t) += 1
    '        Else
    '            freq(t) = 1
    '        End If
    '    Next
    '    Return freq
    'End Function

    '''' <summary>
    '''' Формирует InlineCollection с выделением указанных токенов
    '''' </summary>
    'Private Sub AppendHighlightedText(inlines As InlineCollection, fullText As String, highlightCounts As Dictionary(Of String, Integer), isRemovedContext As Boolean)
    '    If String.IsNullOrEmpty(fullText) Then
    '        inlines.Add(New Run(fullText))
    '        Return
    '    End If

    '    Dim matches = Regex.Matches(fullText, "\b[\p{L}\d]+(?:-[\p{L}\d]+)*\b")
    '    Dim lastIndex = 0

    '    For Each m As Match In matches
    '        If m.Index > lastIndex Then
    '            inlines.Add(New Run(fullText.Substring(lastIndex, m.Index - lastIndex)))
    '        End If

    '        Dim word = m.Value
    '        Dim run = New Run(word)

    '        ' Проверяем, нужно ли выделять этот токен
    '        If highlightCounts.ContainsKey(word) AndAlso highlightCounts(word) > 0 Then
    '            run.FontWeight = FontWeights.Bold
    '            run.FontSize = 14
    '            If isRemovedContext Then
    '                run.Foreground = Brushes.Blue
    '                run.TextDecorations = TextDecorations.Strikethrough
    '            Else
    '                run.Foreground = Brushes.Red
    '            End If

    '            ' Уменьшаем счётчик — чтобы не выделять больше, чем нужно
    '            highlightCounts(word) -= 1
    '        End If

    '        inlines.Add(run)
    '        lastIndex = m.Index + m.Length
    '    Next

    '    If lastIndex < fullText.Length Then
    '        inlines.Add(New Run(fullText.Substring(lastIndex)))
    '    End If
    'End Sub

    'Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
    '    Throw New NotSupportedException("Этот конвертер предназначен только для OneWay-привязки.")
    'End Function
    'End Class
End Namespace

