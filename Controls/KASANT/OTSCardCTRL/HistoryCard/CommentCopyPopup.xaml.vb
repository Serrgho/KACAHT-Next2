
Namespace Kas
	Partial Public Class CommentCopyPopup
        Inherits UserControl

        Private _targetTb As TextBlock
        Private _data As CardCopyData
        Private Shared ReadOnly Presets As New List(Of String) From {"---", "На закрытие", "Для расследования", "Корректировка", "Предоставление документов", "Корректировка документов", "Объединение дубликатов", "Особое мнение", "Не согласовано с СЛД"}

        Public Sub New()
            InitializeComponent()
            BuildPresets()

            ' Привязка ховера к "Копировать как есть" (один раз)
            AddHandler BtnCopyAsIs.MouseEnter, Sub(s, e) UpdatePreview(Nothing)
        End Sub

        Private Sub BuildPresets()
            PresetStack.Children.Clear()
            For Each p In Presets
                Dim tb As New TextBlock With {
                    .Text = p, .FontSize = 13, .Style = TryFindResource("PopupItemStyle")
                }
                AddHandler tb.MouseEnter, Sub(s, e) UpdatePreview(p)
                AddHandler tb.MouseLeave, Sub(s, e) UpdatePreview(Nothing)
                AddHandler tb.MouseLeftButtonUp, Sub(s, e) DoCopy(p)
                PresetStack.Children.Add(tb)
            Next
        End Sub

        Public Sub Show(target As TextBlock, data As CardCopyData)
            _targetTb = target
            _data = data

            UpdatePreview(Nothing)
            UpdateHistory()

            MainPopup.PlacementTarget = _targetTb
            MainPopup.IsOpen = True
        End Sub

        Private Sub UpdateHistory()
            HistoryStack.Children.Clear()
            Dim others = GetOtherComments()

            If others.Any() Then
                DynSeparator.Visibility = Visibility.Visible
                HistoryHeader.Visibility = Visibility.Visible
                For Each c In others
                    Dim tb As New TextBlock With {
                        .Text = c, .FontSize = 13, .Foreground = Brushes.DarkGreen,
                        .Style = TryFindResource("PopupItemStyle")
                    }
                    AddHandler tb.MouseEnter, Sub(s, e) UpdatePreview(c)
                    AddHandler tb.MouseLeave, Sub(s, e) UpdatePreview(Nothing)
                    AddHandler tb.MouseLeftButtonUp, Sub(s, e) DoCopy(c)
                    HistoryStack.Children.Add(tb)
                Next
            Else
                DynSeparator.Visibility = Visibility.Collapsed
                HistoryHeader.Visibility = Visibility.Collapsed
            End If
        End Sub

        Private Sub UpdatePreview(commentOverride As String)
            If _data Is Nothing Then Return

            ' Если override пустой, берем оригинальный комментарий из записи
            Dim effectiveComment = If(String.IsNullOrWhiteSpace(commentOverride), _data.Comment, commentOverride)
            Dim hasComment = Not String.IsNullOrWhiteSpace(effectiveComment)

            ' Формируем текст явно: База + (Коммент) или просто База
            Dim txt = If(hasComment, $"{_data.BaseText.Trim()} ({effectiveComment})".Trim(), _data.BaseText.Trim())

            PreviewText.Text = txt
            PreviewText.ToolTip = txt

            ' Визуальный акцент: выделяем только если пользователь навел на пункт меню (override задан)
            Dim isUserHover As Boolean = Not String.IsNullOrWhiteSpace(commentOverride)
            PreviewText.Foreground = If(isUserHover, Brushes.Black, Brushes.DimGray)
            PreviewText.FontStyle = If(isUserHover, FontStyles.Normal, FontStyles.Italic)

            'If _data Is Nothing Then Return

            'Dim isChanged As Boolean = Not String.IsNullOrWhiteSpace(commentOverride)
            'Dim txt = If(isChanged, $"{_data.BaseText} ({commentOverride})".Trim(), _data.FullText)

            'PreviewText.Text = txt
            'PreviewText.ToolTip = txt
            'PreviewText.Foreground = If(isChanged, Brushes.Black, Brushes.DimGray)
            'PreviewText.FontStyle = If(isChanged, FontStyles.Normal, FontStyles.Italic)
        End Sub

        Private Function GetOtherComments() As List(Of String)
            Dim res As New List(Of String)
            Dim win = TryCast(Window.GetWindow(_targetTb), KasAntWin)
            If win Is Nothing OrElse win.HistoryPanel Is Nothing Then Return res

            Dim myCmt = If(_data?.Comment, "").ToLower()
            For Each child In win.HistoryPanel.Children
                Dim card = TryCast(child, HistoryCard)
                If card Is Nothing OrElse card.tbMain Is _targetTb Then Continue For

                Dim d = TryCast(card.tbMain?.Tag, CardCopyData)
                If d IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(d.Comment) Then
                    Dim c = d.Comment.Trim()
                    If c.ToLower() <> myCmt AndAlso Not res.Contains(c, StringComparer.OrdinalIgnoreCase) Then res.Add(c)
                End If
            Next
            Return res
        End Function

        Private Sub DoCopy(commentOverride As String)


            If _data Is Nothing Then Return

            Dim chosen = If(String.IsNullOrWhiteSpace(commentOverride), _data.Comment, commentOverride)
            Dim final = If(String.IsNullOrWhiteSpace(chosen), _data.BaseText, $"{_data.BaseText} ({chosen})").Trim()

            Try
                Clipboard.SetText(final)
                NotifyParent($"✓ Скопировано: {final}", Brushes.DarkGreen)
            Catch ex As Exception
                NotifyParent($"Ошибка буфера: {ex.Message}", Brushes.Red)
                MainPopup.IsOpen = False
                Return
            End Try


            ' 🔥 Используем глобальное KasAntWND
            If KasAntWND IsNot Nothing AndAlso PointedOtkaz IsNot Nothing AndAlso PointedOtkaz.Id = KasAntWND.txtViolId.Text AndAlso _data.EventDate <> DateTime.MinValue Then
                PointedOtkaz.AddHistoryEntry(_data.EventDate, final.Replace(_data.EventDate.ToString("dd.MM.yy "), "").Replace(_data.EventDate.ToString("dd.MM.yyyy "), "").Trim())
            End If

            '' 🔥 Дата уже внутри _data
            'If _data.EventDate <> DateTime.MinValue AndAlso PointedOtkaz IsNot Nothing Then
            '    PointedOtkaz.AddHistoryEntry(_data.EventDate, final)
            'End If

            MainPopup.IsOpen = False



            'If _data Is Nothing Then Return
            'Dim chosen = If(String.IsNullOrWhiteSpace(commentOverride), _data.Comment, commentOverride)
            'Dim final = If(String.IsNullOrWhiteSpace(chosen), _data.BaseText, $"{_data.BaseText} ({chosen})").Trim()

            'Try
            '    Clipboard.SetText(final)
            '    NotifyParent($"✓ Скопировано: {final}", Brushes.DarkGreen)

            'Catch ex As Exception
            '    NotifyParent($"Ошибка буфера: {ex.Message}", Brushes.Red)
            'End Try

            'MainPopup.IsOpen = False
        End Sub

        Private Sub BtnCopyAsIs_Click(s As Object, e As MouseButtonEventArgs)
            DoCopy(Nothing)
        End Sub

        ' 🔽 Безопасный вызов родительского SetStatus
        Private Sub NotifyParent(text As String, color As Brush)
            KasAntWND?.SetStatus(text, color)
        End Sub

    End Class
End Namespace


