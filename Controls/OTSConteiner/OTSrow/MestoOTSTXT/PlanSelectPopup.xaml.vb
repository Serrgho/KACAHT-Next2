
Imports System.Windows.Controls.Primitives

Namespace Kas
    Partial Public Class PlanSelectPopup
        Inherits UserControl

        Private _otkaz As Otkaz

        Public Sub ShowPlans(otkaz As Otkaz)


            _otkaz = otkaz
            PlansStack.Children.Clear()

            Dim itemStyle = CType(Me.FindResource("PopupItemStyle"), Style)
            Dim itemGreenStyle = CType(Me.FindResource("PopupGreenStyle"), Style)


            For Each reason In PlanEntry.ReferenceReasonsList
                Dim tb As New TextBlock With {.Text = reason}

                'Проверяем, есть ли уже такой план (обычным циклом, без LINQ)
                Dim alreadyExists As Boolean = False
                Dim PZ = New PlanEntry With {.Description = reason}
                For Each p In otkaz.Plan
                    If String.Equals(p.Description, reason, StringComparison.OrdinalIgnoreCase) Then
                        PZ = p
                        alreadyExists = True
                        Exit For
                    End If
                Next

                If alreadyExists Then
                    ' Уже добавлен — серый, некликабельный
                    tb.Opacity = 0.4
                    tb.Padding = New Thickness(6, 4, 6, 4)
                    tb.TextWrapping = TextWrapping.Wrap
                Else
                    If PZ.IsGreen Then
                        ' Доступен для добавления
                        tb.Style = itemGreenStyle
                        'AddHandler tb.MouseLeftButtonUp, AddressOf PlanItem_Click
                        'AddHandler tb.MouseEnter, AddressOf 
                    Else
                        ' Доступен для добавления
                        tb.Style = itemStyle
                        'AddHandler tb.MouseLeftButtonUp, AddressOf PlanItem_Click
                    End If

                    ' ✅ Только два обработчика, без дублей
                    AddHandler tb.MouseEnter, Sub(s, e) SetHistoryMoused(DirectCast(s, TextBlock).Text, True)
                    AddHandler tb.MouseLeave, Sub(s, e) SetHistoryMoused(DirectCast(s, TextBlock).Text, False)
                    AddHandler tb.MouseLeftButtonUp, AddressOf PlanItem_Click


                End If

                PlansStack.Children.Add(tb)

            Next

            MainPopup.IsOpen = True
        End Sub


        Private Sub SetHistoryMoused(text As String, isMoused As Boolean)
            If _otkaz Is Nothing Then Return

            ' Ищем элемент истории по описанию
            Dim entry As HistoryEntry = _otkaz.GetHistoryEntryByDescription(text)

            ' Если по Description не нашлось, попробуем по DisplayText
            If entry Is Nothing Then
                entry = _otkaz.GetHistoryEntryByDisplayText(text)
            End If

            If entry IsNot Nothing Then
                entry.isMoused = isMoused
            End If
        End Sub


        Private Sub PlanItem_Click(sender As Object, e As MouseButtonEventArgs)
            Dim tb = CType(sender, TextBlock)

            If _otkaz IsNot Nothing Then
                Dim PlEnt As New PlanEntry
                PlEnt.Description = tb.Text
                _otkaz.Plan.Add(PlEnt)

                Dim HEnt As New HistoryEntry

                HEnt.ShowDate = False
                HEnt.Description = PlEnt.DisplayText 'tb.Text

                If PlEnt.IsGreen Then
                    'на будущее оставим
                End If


                If _otkaz.HasHistoryEntry(PlEnt.DisplayText) Or _otkaz.HasHistoryEntry(PlEnt.Description) Then

                    'НАДО: найти, подправить DisplayText (если без слова ПЛАН) и выделить элемент
                    Dim entry As HistoryEntry = _otkaz.GetHistoryEntryByDisplayText(PlEnt.Description)

                    If entry IsNot Nothing Then
                        ' уже найден тот у которого совпадают DisplayText и Description, значит слова ПЛАН нет
                        entry.Description = PlEnt.DisplayText
                        _otkaz.History.Move(_otkaz.History.IndexOf(entry), 0)
                    End If

                    'entry = _otkaz.GetHistoryEntryByDescription(PlEnt.Description)
                    ''только подсветка
                    'If entry IsNot Nothing Then
                    '    ' уже найден тот у которого совпадают Description и Description
                    '    entry.IsHighlighted = True

                    'End If
                Else
                    _otkaz.History.Insert(0, HEnt)
                End If

            End If

            MainPopup.IsOpen = False
        End Sub




    End Class
End Namespace

