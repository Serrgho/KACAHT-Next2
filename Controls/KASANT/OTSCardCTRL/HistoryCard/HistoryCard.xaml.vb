


Imports System.Globalization
Imports System.Text.RegularExpressions

Namespace Kas
    Partial Public Class HistoryCard

        Private Shared ReadOnly AlienNumRegex As New Regex("#\s*(\d+)", RegexOptions.Compiled)
        Private Shared ReadOnly ValidCommentRegex As New Regex("[\p{L}\p{Nd}]", RegexOptions.Compiled)

        ' Один экземпляр попупа на карточку
        Private _copyPopup As New CommentCopyPopup()

        ''' <summary>
        ''' Безопасно извлекает CardCopyData из Tag TextBlock (с фоллбэком на String)
        ''' </summary>
        Private Shared Function ExtractCopyData(tb As TextBlock) As CardCopyData
            If tb Is Nothing OrElse tb.Tag Is Nothing Then Return Nothing

            ' Если в Tag уже лежит нужный тип - возвращаем сразу
            If TypeOf tb.Tag Is CardCopyData Then
                Return DirectCast(tb.Tag, CardCopyData)
            End If

            ' Фоллбэк для обратной совместимости (если вдруг где-то осталась строка)
            If TypeOf tb.Tag Is String Then
                Dim txt As String = DirectCast(tb.Tag, String).Trim()
                If String.IsNullOrEmpty(txt) Then Return Nothing
                Return New CardCopyData With {.BaseText = txt, .Comment = Nothing}
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Присваивает данные карточке. Вёрстка уже настроена в XAML.
        ''' </summary>
        Public Sub SetData(h As HistoryRecord)
            Dim evtDate As DateTime = DateTime.MinValue
            DateTime.TryParseExact(h.DateTime, "dd.MM.yyyy HH:mm", Globalization.CultureInfo.InvariantCulture, Globalization.DateTimeStyles.None, evtDate)

            ' 1. AlienNum
            Dim alienText As String = h.AlienNum
            If Not String.IsNullOrWhiteSpace(h.From) Then
                Dim m = AlienNumRegex.Match(h.From)
                If m.Success Then alienText = m.Groups(1).Value.Replace(" ", "").Trim()
            End If


            If Not String.IsNullOrWhiteSpace(alienText) Then
                tbAlienNum.Text = alienText
                tbAlienNum.Tag = New CardCopyData With {
                    .BaseText = alienText,
                    .Comment = Nothing,
                    .EventDate = evtDate   ' 🔥
                }
                tbAlienNum.Visibility = Visibility.Visible
            Else
                tbAlienNum.Visibility = Visibility.Collapsed
            End If

            ' 2. ByWhom
            If Not String.IsNullOrWhiteSpace(h.ByWhom) Then
                tbByWhom.Text = $"👤   {h.ByWhom}"
                tbByWhom.Visibility = Visibility.Visible
            Else
                tbByWhom.Visibility = Visibility.Collapsed
            End If

            ' 3. Основная строка
            BuildMainLine(h)
            tbMain.Visibility = Visibility.Visible

            ' 4. Привязка правого клика к попупу
            AddHandler tbMain.PreviewMouseRightButtonUp, Sub(s, e)
                                                             _copyPopup.Show(tbMain, ExtractCopyData(tbMain))
                                                             e.Handled = True
                                                         End Sub
        End Sub

        Private Sub BuildMainLine(hrec As HistoryRecord)
            tbMain.Inlines.Clear()
            Dim baseParts As New List(Of String) From {hrec.DateTime, hrec.ActionType}

            Dim cleanFrom = Regex.Replace(hrec.From, "#\s*\d+", "").Trim().Replace("  ", " ")
            If Not String.IsNullOrWhiteSpace(cleanFrom) Then baseParts.Add(cleanFrom)

            If Not String.IsNullOrWhiteSpace(hrec.ToDest) Then
                baseParts.Add("=>") : baseParts.Add(hrec.ToDest)
            End If

            Dim actColor = Brushes.Black, actWeight = FontWeights.Normal
            Select Case True
                Case hrec.ActionType.ToLower().Contains("восстановлен")
                    actColor = Brushes.DarkGreen : actWeight = FontWeights.Bold
                Case hrec.ActionType.ToLower().Contains("назначен")
                    actColor = Brushes.DarkViolet : actWeight = FontWeights.Bold
                Case Regex.IsMatch(hrec.ActionType.ToLower(), "^расследован$")
                    actColor = Brushes.Red : actWeight = FontWeights.Bold
                Case Regex.IsMatch(hrec.ActionType.ToLower(), "^передан\b")
                    actColor = Brushes.DarkBlue : actWeight = FontWeights.Bold
            End Select

            tbMain.Inlines.Add(New Run($"{hrec.DateTime} ") With {.Foreground = Brushes.Black})
            tbMain.Inlines.Add(New Run($"{hrec.ActionType} ") With {.Foreground = actColor, .FontWeight = actWeight})
            If Not String.IsNullOrWhiteSpace(cleanFrom) Then tbMain.Inlines.Add(New Run(cleanFrom) With {.Foreground = Brushes.DarkBlue})
            If Not String.IsNullOrWhiteSpace(hrec.ToDest) Then
                tbMain.Inlines.Add(New Run(" => ") With {.Foreground = Brushes.Black})
                tbMain.Inlines.Add(New Run(hrec.ToDest) With {.Foreground = Brushes.Firebrick, .FontWeight = FontWeights.SemiBold})
            End If

            Dim rawComment As String = Nothing
            If Not String.IsNullOrWhiteSpace(hrec.Comment) AndAlso ValidCommentRegex.IsMatch(hrec.Comment) Then
                rawComment = Regex.Replace(hrec.Comment, "\s+", " ").Replace(vbCrLf, "").Trim()
                tbMain.Inlines.Add(New Run($" ({rawComment})") With {
                    .Foreground = Brushes.DarkGreen,
                    .FontStyle = FontStyles.Italic,
                    .FontWeight = FontWeights.SemiBold,
                    .FontSize = 14
                })
            End If

            Dim evtDate As DateTime = DateTime.MinValue
            DateTime.TryParseExact(hrec.DateTime, "dd.MM.yyyy HH:mm", Globalization.CultureInfo.InvariantCulture, Globalization.DateTimeStyles.None, evtDate)

            tbMain.Tag = New CardCopyData With {
                .BaseText = Regex.Replace(String.Join(" ", baseParts), "\s+", " ").Trim(),
                .Comment = rawComment,
                .EventDate = evtDate   ' 🔥
            }

        End Sub

        'Private Shared ReadOnly PresetComments As New List(Of String) From {"---", "Объединение дубликатов", "Для расследования", "На закрытие", "Корректировка", "Предоставление документов"}

        Private Function GetOtherComments() As List(Of String)
            Dim result As New List(Of String)

            Dim win = TryCast(Window.GetWindow(Me), KasAntWin)
            If win Is Nothing OrElse win.HistoryPanel Is Nothing Then Return result

            Dim myData = ExtractCopyData(tbMain)
            Dim myComment = If(myData?.Comment, "").ToLower()

            For Each Chld In win.HistoryPanel.Children
                Dim card = TryCast(Chld, HistoryCard)
                If card Is Nothing OrElse card Is Me Then Continue For

                Dim data = ExtractCopyData(card.tbMain)
                If data IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(data.Comment) Then
                    Dim cmt = data.Comment.Trim()
                    If cmt.ToLower() <> myComment AndAlso Not result.Contains(cmt, StringComparer.OrdinalIgnoreCase) Then
                        result.Add(cmt)
                    End If
                End If
            Next

            Return result
        End Function


        Private Sub OnCopyClick2(sender As Object, e As MouseButtonEventArgs)
            Dim tb = DirectCast(sender, TextBlock)
            Dim data = ExtractCopyData(tb)
            If data Is Nothing Then Return

            Dim final = data.FullText
            Try
                Clipboard.SetText(final)

                If KasAntWND IsNot Nothing Then
                    KasAntWND.SetStatus($"✓ Скопировано: {final}", Brushes.DarkGreen)
                End If
            Catch ex As Exception
                If KasAntWND IsNot Nothing Then
                    KasAntWND.SetStatus($"Ошибка буфера: {ex.Message}", Brushes.Red)
                End If

            End Try
            e.Handled = True
        End Sub



        Public Sub OnCopyClick(sender As Object, e As MouseButtonEventArgs)

            Dim tb = DirectCast(sender, TextBlock)
            Dim data = ExtractCopyData(tb)
            If data Is Nothing Then Return

            Dim final = data.FullText
            Try
                Clipboard.SetText(final)
                ' 🔥 Используем глобальное KasAntWND
                If KasAntWND IsNot Nothing Then
                    KasAntWND.SetStatus($"✓ Скопировано: {final}", Brushes.DarkGreen)

                    ' 🔥 🔥 🔥 НОВАЯ ПРОВЕРКА: только если Id совпадает с текущим окном
                    If PointedOtkaz IsNot Nothing AndAlso
                       PointedOtkaz.Id = KasAntWND.txtViolId.Text AndAlso
                       data.EventDate <> DateTime.MinValue Then
                        PointedOtkaz.AddHistoryEntry(data.EventDate, final.Replace(data.EventDate.ToString("dd.MM.yy "), "").Replace(data.EventDate.ToString("dd.MM.yyyy "), "").Trim())
                    End If
                End If
            Catch ex As Exception
                If KasAntWND IsNot Nothing Then
                    KasAntWND.SetStatus($"Ошибка буфера: {ex.Message}", Brushes.Red)
                End If
            End Try
            e.Handled = True
        End Sub


    End Class

    ''' <summary>
    ''' Модель данных для копирования содержимого карточки в буфер обмена
    ''' </summary>
    Public Class CardCopyData

        Public Property BaseText As String       ' Текст БЕЗ комментария
        Public Property Comment As String        ' Только текст комментария
        Public Property EventDate As DateTime    ' 🔥 Дата события из записи

        Public ReadOnly Property FullText As String
            Get
                Return If(String.IsNullOrWhiteSpace(Comment),
                     BaseText.Trim(),
                     $"{BaseText.Trim()} ({Comment})").Trim()
            End Get
        End Property

    End Class

End Namespace





