
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Windows.Documents

Namespace Kas
    Public Class LocationTextBlock
        '    Inherits TextBlock

        '    Public Shared ReadOnly SavedProperty As DependencyProperty =
        'DependencyProperty.Register("Saved", GetType(Boolean), GetType(LocationTextBlock),
        '    New PropertyMetadata(False, AddressOf OnIsSavedChanged))

        '    Public Property Saved As Boolean
        '        Get
        '            Return CBool(GetValue(SavedProperty))
        '        End Get
        '        Set(value As Boolean)
        '            SetValue(SavedProperty, value)
        '        End Set
        '    End Property

        '    Private Shared Sub OnIsSavedChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
        '        Dim ctrl = DirectCast(d, LocationTextBlock)
        '        ctrl.UpdateInlines(ctrl.SourceText)
        '    End Sub

        '''' <summary>
        '''' Проверяет, есть ли в тексте пассажирские/пригородные поезда:
        '''' номер < 600 или 6000 ≤ № < 7000.
        '''' Работает на той же логике, что и UpdateInlines.
        '''' </summary>
        'Public Shared Function HasPassInText(text As String) As Boolean
        '    If String.IsNullOrEmpty(text) Then Return False

        '    Dim trainPos = text.IndexOf("поезда", StringComparison.OrdinalIgnoreCase)
        '    If trainPos < 0 Then Return False

        '    Dim afterTrains = text.Substring(trainPos)
        '    Dim hashPos = afterTrains.IndexOf("№", StringComparison.OrdinalIgnoreCase)
        '    If hashPos < 0 Then Return False

        '    Dim numbersPart = afterTrains.Substring(hashPos + 1).Trim()
        '    Dim parts = numbersPart.Split(","c)

        '    For Each part In parts
        '        Dim numStr = part.Trim()
        '        Dim num As Integer
        '        If Integer.TryParse(numStr, num) Then
        '            If num < 700 OrElse (num >= 6000 AndAlso num < 7000) Then
        '                Return True
        '            End If
        '        End If
        '    Next

        '    Return False
        'End Function




        '    Public Shared ReadOnly PlaceTextProperty As DependencyProperty =
        'DependencyProperty.Register("PlaceText", GetType(String), GetType(LocationTextBlock),
        '    New PropertyMetadata(Nothing, AddressOf OnSourceTextChanged))

        '    Public Property PlaceText As String
        '        Get
        '            Return CType(GetValue(PlaceTextProperty), String)
        '        End Get
        '        Set(value As String)
        '            SetValue(PlaceTextProperty, value)
        '        End Set
        '    End Property





        '    Public Shared ReadOnly SourceTextProperty As DependencyProperty =
        '        DependencyProperty.Register("SourceText", GetType(String), GetType(LocationTextBlock),
        '            New PropertyMetadata(Nothing, AddressOf OnSourceTextChanged))

        '    Public Property SourceText As String
        '        Get
        '            Return CType(GetValue(SourceTextProperty), String)
        '        End Get
        '        Set(value As String)
        '            SetValue(SourceTextProperty, value)
        '        End Set
        '    End Property

        '    Private Shared Sub OnSourceTextChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
        '        Dim ctrl = DirectCast(d, LocationTextBlock)
        '        Dim source = CType(e.NewValue, String)
        '        ctrl.UpdateInlines(source) ', place)
        '    End Sub

        '    Private Sub UpdateInlines(source As String) ', place As String)

        '        Me.Inlines.Clear()
        '        If String.IsNullOrEmpty(source) Then
        '            Me.Inlines.Add(New Run("---"))
        '            Return
        '        End If

        '        Dim text = source.Trim()

        '        ' === 1. Находим "РЕГ" ===
        '        Dim regPos = text.IndexOf("РЕГ", StringComparison.OrdinalIgnoreCase)
        '        If regPos < 0 Then
        '            Me.Inlines.Add(New Run(text))
        '            Return
        '        End If

        '        ' === 2. Дорога — всё до "РЕГ" ===
        '        Dim road = text.Substring(0, regPos).Trim().TrimEnd(","c)

        '        ' === 3. Ищем начало места: ",  " после "РЕГ" ===
        '        Dim markerPos = text.IndexOf(",  ", regPos)
        '        If markerPos < 0 Then
        '            ' fallback: просто после РЕГ
        '            markerPos = regPos + "РЕГ".Length
        '            While markerPos < text.Length AndAlso Char.IsDigit(text(markerPos))
        '                markerPos += 1
        '            End While
        '            ' Пропускаем возможные пробелы и запятую
        '            While markerPos < text.Length AndAlso (text(markerPos) = ","c OrElse Char.IsWhiteSpace(text(markerPos)))
        '                markerPos += 1
        '            End While
        '        Else
        '            markerPos += 3 ' длина ",  "
        '        End If

        '        ' === 4. Ищем конец места: "поезда", предваряемое >=2 пробелами (включая \r\n) ===
        '        Dim placeEndPos = text.Length
        '        Dim afterRegText = text.Substring(markerPos)
        '        '   "\s{2,}поезда"
        '        Dim trainMatch = System.Text.RegularExpressions.Regex.Match(afterRegText, "поезда",
        '            System.Text.RegularExpressions.RegexOptions.IgnoreCase Or System.Text.RegularExpressions.RegexOptions.Singleline)

        '        If trainMatch.Success Then
        '            placeEndPos = markerPos + trainMatch.Index
        '        End If

        '        ' === 5. Извлекаем место ===
        '        Dim place = If(markerPos < placeEndPos,
        '                       text.Substring(markerPos, placeEndPos - markerPos).Trim(),
        '                       "")

        '        ' === 6. Выводим дорогу ===
        '        If Not String.IsNullOrEmpty(road) Then
        '            Me.Inlines.Add(New Run(road & ","))
        '            Me.Inlines.Add(New LineBreak())
        '        End If

        '        ' === 7. Выводим всё от "РЕГ" до конца места — но БЕЗ жирного у "РЕГ" ===
        '        ' Сначала находим конец "РЕГ-X"
        '        Dim regEndPos = regPos
        '        While regEndPos < text.Length AndAlso (Char.IsLetterOrDigit(text(regEndPos)) OrElse text(regEndPos) = "-"c)
        '            regEndPos += 1
        '        End While
        '        ' Пропускаем запятые и пробелы после РЕГ
        '        Dim regPartEnd = regEndPos
        '        While regPartEnd < text.Length AndAlso (text(regPartEnd) = ","c OrElse Char.IsWhiteSpace(text(regPartEnd)))
        '            regPartEnd += 1
        '        End While

        '        ' Выводим "РЕГ-X," как обычный текст
        '        Dim regDisplay = text.Substring(regPos, Math.Min(regPartEnd - regPos, placeEndPos - regPos)).Trim()
        '        If Not String.IsNullOrEmpty(regDisplay) Then
        '            Me.Inlines.Add(New Run(regDisplay))
        '        End If

        '        ' Выводим оставшуюся часть места (если есть) — ЖИРНОЙ
        '        If regPartEnd < placeEndPos Then
        '            Dim remainingPlace = text.Substring(regPartEnd, placeEndPos - regPartEnd).Trim()
        '            If Not String.IsNullOrEmpty(remainingPlace) Then
        '                Me.Inlines.Add(New Bold(New Run(" " & remainingPlace)))
        '            End If
        '        End If

        '        ' === 8. Выводим поезда (если есть) ===
        '        If trainMatch.Success Then
        '            Me.Inlines.Add(New LineBreak())

        '            Dim trainsStart = markerPos + trainMatch.Index + trainMatch.Length - "поезда".Length
        '            Dim trainsText = text.Substring(trainsStart).Trim()

        '            ' Находим "поезда" в trainsText
        '            Dim prefix = "поезда"
        '            Dim prefixIndex = trainsText.IndexOf(prefix, StringComparison.OrdinalIgnoreCase)
        '            If prefixIndex >= 0 Then
        '                Me.Inlines.Add(New Run(prefix & " "))
        '                Dim afterPrefix = trainsText.Substring(prefixIndex + prefix.Length).Trim()

        '                Dim firstHashIndex = afterPrefix.IndexOf("№", StringComparison.OrdinalIgnoreCase)
        '                If firstHashIndex >= 0 Then
        '                    If firstHashIndex > 0 Then
        '                        Me.Inlines.Add(New Run(afterPrefix.Substring(0, firstHashIndex)))
        '                    End If
        '                    Me.Inlines.Add(New Run("№"))

        '                    Dim numbersPart = afterPrefix.Substring(firstHashIndex + 1).Trim()
        '                    Dim numberStrings = numbersPart.Split(","c)

        '                    Dim shouldBold As Boolean = LocationTextBlock.HasPassInText(trainsText)

        '                    For i As Integer = 0 To numberStrings.Length - 1
        '                        Dim numStr = numberStrings(i).Trim()
        '                        Dim number As Integer

        '                        If Integer.TryParse(numStr, number) Then
        '                            Dim run = New Run(numStr)
        '                            If shouldBold AndAlso (number < 700 OrElse (number >= 6000 AndAlso number < 7000)) Then
        '                                If Me.Saved Then
        '                                    run.FontWeight = FontWeights.Bold
        '                                Else
        '                                    run.Foreground = Brushes.Red
        '                                End If
        '                            End If
        '                            Me.Inlines.Add(run)
        '                        Else
        '                            Me.Inlines.Add(New Run(numStr))
        '                        End If

        '                        If i < numberStrings.Length - 1 Then
        '                            Me.Inlines.Add(New Run(", "))
        '                        End If
        '                    Next
        '                Else
        '                    Me.Inlines.Add(New Run(afterPrefix))
        '                End If
        '            Else
        '                Me.Inlines.Add(New Run(trainsText))
        '            End If
        '        End If

        '    End Sub


        '    Private Shared Function SplitByCommaAndKeepDelimiters(input As String) As List(Of String)
        '        Dim result As New List(Of String)
        '        Dim current As New StringBuilder()
        '        Dim inQuote As Boolean = False

        '        For i As Integer = 0 To input.Length - 1
        '            Dim c = input(i)
        '            If c = """"c Then
        '                inQuote = Not inQuote
        '            ElseIf c = ","c AndAlso Not inQuote Then
        '                result.Add(current.ToString())
        '                current.Clear()
        '            Else
        '                current.Append(c)
        '            End If
        '        Next

        '        If current.Length > 0 Then
        '            result.Add(current.ToString())
        '        End If

        '        Return result
        '    End Function


    End Class
End Namespace
