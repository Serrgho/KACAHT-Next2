Imports System.Collections.ObjectModel
Imports System.Globalization
Imports System.Windows.Controls.Primitives
Imports System.Windows.Threading

Namespace Kas

    Partial Public Class ActionsUserControl
        Inherits UserControl
        Private _isDialogOpen As Boolean = False  ' Флаг открытого диалога

        '' 1. Добавляем переменную для хранения ссылки на контрол, открывший календарь
        Private Shared _activeControlContext As ActionsUserControl = Nothing


        Private Sub OnCalendarDateSelected(sender As Object, e As DateSelectedEventArgs)
            ' 2. ВАЖНО: Если контекст не сохранен, выходим, чтобы не сработал "чужой" контрол
            If _activeControlContext Is Nothing Then Return


            If PointedOtkaz Is Nothing Then Return

            ' Используем сохраненный контекст вместо Me, если нужно обратиться к данным UI
            ' Но для PointedOtkaz лучше использовать контекст, так как он привязан к конкретному экземпляру
            Dim currentUC As ActionsUserControl = _activeControlContext


            ' Используем EditMode из аргументов события
            Select Case e.EditMode
                Case "Postup"
                    Dim eventDate As Date = e.SelectedDate
                    'если раньше чем закрыт (на случай изменения в процессе работы) и если позже чем начало - то норм

                    PointedOtkaz.Postup = eventDate
                    'тут проверяем установилось ли свойство в результате его внутренних проверок
                    If PointedOtkaz.Postup = eventDate Then
                        PointedOtkaz.RemoveItemUpdateNote(NewOTSTB.Text)
                        'e.Handled = True
                    End If
                    '======================================================
                Case "Zakryt"
                    Dim eventDate As Date = e.SelectedDate
                    Dim desc As String = "закрыт"
                    Dim Entry As PlanEntry
                    'если поступил раньше чем закрыт то норм
                    If PointedOtkaz.Postup <= eventDate Then
                        If Not PointedOtkaz.KtoZakryl.ToLower.Contains("трп") Then
                            Dim Zakm As String = SetZakem()
                            If Zakm <> "" Then
                                PointedOtkaz.ZaKem = Zakm
                                Dim desc1 As String
                                If PointedOtkaz.AlienSLD <> "" Then
                                    desc1 = $" за {PointedOtkaz.AlienSLD}"
                                Else
                                    desc1 = $" за {PointedOtkaz.ZaKem}"
                                End If
                                '=================================================
                                If Zakm.ToLower.Contains("завод") OrElse Zakm.ToLower.Contains("проч") Then
                                    Entry = PointedOtkaz.GetPlanEntryByDescription("За заводом")
                                ElseIf Zakm.ToLower.Contains("слд") Then
                                    Entry = PointedOtkaz.GetPlanEntryByDescription("За СЛД")
                                ElseIf Zakm.ToLower.Contains("тч") Then
                                    Entry = PointedOtkaz.GetPlanEntryByDescription("за ТЧЭ")
                                End If

                                If Not IsNothing(Entry) Then
                                    PointedOtkaz.DeletePlan(Entry)
                                End If
                                '=================================================
                                'сначала за кем, потом в историю, а потом уже убираем ItemUpdateNote
                                desc += desc1
                                PointedOtkaz.Zakryt = eventDate
                                PointedOtkaz.AddHistoryEntry(eventDate, desc)
                                PointedOtkaz.RemoveItemUpdateNote("уже закрыт")
                            Else
                                PointedOtkaz.Zakryt = Date.MinValue

                            End If
                        Else
                            PointedOtkaz.Zakryt = eventDate
                            PointedOtkaz.RemoveItemUpdateNote("уже закрыт")
                        End If

                    End If

                    '======================================================

                Case "SavedItem"
                    Dim Entry As PlanEntry
                    Dim eventDate As Date = e.SelectedDate
                    'если поступил раньше чем закрыт то норм
                    If PointedOtkaz.Postup <= eventDate Then
                        Dim Zakm As String = SetSavedZakem()
                        If Zakm <> "" Then
                            PointedOtkaz.ZaKem = Zakm
                            'проверяем все ли условия для обозначения сохраненного
                            If PointedOtkaz.IsSaved Then
                                Dim desc As String
                                If PointedOtkaz.AlienSLD <> "" Then
                                    desc = $"сохранен за {PointedOtkaz.AlienSLD}"
                                Else
                                    desc = $"сохранен за {PointedOtkaz.ZaKem}"
                                End If

                                '=================================================
                                If Zakm.ToLower.Contains("завод") OrElse Zakm.ToLower.Contains("проч") Then
                                    Entry = PointedOtkaz.GetPlanEntryByDescription("За заводом")
                                ElseIf Zakm.ToLower.Contains("слд") Then
                                    Entry = PointedOtkaz.GetPlanEntryByDescription("За СЛД")
                                ElseIf Zakm.ToLower.Contains("тч") Then
                                    Entry = PointedOtkaz.GetPlanEntryByDescription("за ТЧЭ")
                                End If

                                If Not IsNothing(Entry) Then
                                    PointedOtkaz.DeletePlan(Entry)
                                End If
                                '=================================================



                                PointedOtkaz.AddHistoryEntry(eventDate, desc)
                                PointedOtkaz.RemoveItemUpdateNote(SavedTB.Text)
                            Else
                                ShowMSG(MW, "не все условия для признака сохранен")
                            End If
                        End If

                    End If

                Case "Vernulsa"
                    Dim eventDate As Date = e.SelectedDate
                    If PointedOtkaz.Postup <= eventDate Then
                        ' 3. Передаем сохраненный контекст, а не Me
                        Vernulsa_SUB(currentUC, "поступил", e.SelectedDate)
                    End If

                Case "Vosstanovlen"
                    Dim eventDate As Date = e.SelectedDate

                    PointedOtkaz.Zakryt = Nothing
                    If PointedOtkaz.AlienSLD <> "" Then
                        PointedOtkaz.AlienSLD = ""
                    End If
                    'If Not PointedOtkaz.KtoZakryl.ToLower.Contains("трп") Then
                    '    PointedOtkaz.ZaKem = ""
                    'End If

                    Dim desc As String = $"восстановлен"
                    PointedOtkaz.ZaKem = ""
                    PointedOtkaz.AddHistoryEntry(eventDate, desc)
                    PointedOtkaz.RemoveItemUpdateNote(RestoredTB.Text)


                Case "OutFromRail"
                    Dim eventDate As Date = e.SelectedDate
                    Dim Zakm As String = SeToOutZakem()
                    Dim PreText As String = ""
                    Dim Entry As PlanEntry

                    If Zakm <> "" Then
                        PointedOtkaz.ZaKem = Zakm
                        If Zakm.ToLower.Contains("ублик") OrElse Zakm.ToLower.Contains("ехнолог") Then
                            PreText = $"переведен в {PointedOtkaz.ZaKem}"
                            PointedOtkaz.Zakryt = eventDate
                        ElseIf Zakm.ToLower = ("тч9") Then
                            PreText = $"отнесен на {PointedOtkaz.ZaKem}"
                        ElseIf Zakm.ToLower = ("др дорога") Then
                            PreText = $"{PointedOtkaz.ZaKem}"
                            Dim DorPr As List(Of String) = GetDoroga_Prichina()
                            If DorPr Is Nothing Then Return


                            Entry = PointedOtkaz.GetPlanEntryByDescription("На др дорогу")
                            If IsNothing(Entry) Then
                                Entry = PointedOtkaz.GetPlanEntryByDescription("письмо НЗ-1")
                                If Not IsNothing(Entry) Then
                                    PointedOtkaz.DeletePlan(Entry)
                                End If
                            Else
                                PointedOtkaz.DeletePlan(Entry)
                            End If

                            PreText += $"  ({DorPr(0)}, {DorPr(1)})"

                        ElseIf Zakm.ToLower.Contains("дорога/") Then

                            Entry = PointedOtkaz.GetPlanEntryByDescription("На др дорогу")
                            If Not IsNothing(Entry) Then
                                PointedOtkaz.DeletePlan(Entry)
                            End If

                            PreText = $"{PointedOtkaz.ZaKem}"
                        End If
                        PointedOtkaz.Peredan = eventDate
                        PointedOtkaz.AddHistoryEntry(eventDate, PreText)
                        PointedOtkaz.RemoveItemUpdateNote(QuestionMarksTB.Text)
                    End If
                Case Else
                    'SeToOutZakem OutFromRail "дубликат", "тч9", "технология", "др дорога"

            End Select

            ' Дополнительно можно проверить, какая дата изменилась
            ' через сравнение с текущими значениями
            'If e.EditMode = "Postup" AndAlso PointedOtkaz.Postup <> e.SelectedDate Then
            '    ' Дата создания действительно изменилась
            'End If

            ' 4. Очищаем контекст после обработки
            _activeControlContext = Nothing

        End Sub








        Public Sub Vernulsa_SUB(ActUC As ActionsUserControl, FromTo As String, Dat As Date)

            Dim DorPr As List(Of String) = GetDoroga_Prichina()
            If DorPr Is Nothing Then Return

            Dim Koment As String = FromTo
            Dim NewDepo As String = ActUC.DepoChangeTB.Text.ToLower.Replace("за ", "").Replace("?", "").ToUpper
            'исходим из того, что уже все проверено при импорте отчета и KtoZakryl и NewDepo разные
            'на кого вернулся проверить 
            If ActUC.DepoChangeItem.Visibility = Visibility.Visible Then
                'тогда просто ставим Ктозакрыл принудительно ну может спросим пользователя
                Dim confirmResult = ModernMessageBox.MsgShow(MW, "ВОПРОС", $"установить указанное депо {NewDepo}?", showCancel:=True)
                'эсли ДА. Иначе пусть висит для последующей обработки
                If Not (confirmResult = False OrElse confirmResult Is Nothing) Then
                    Koment += $" на {NewDepo}"
                    PointedOtkaz.KtoZakryl = NewDepo
                    'и скрываем элемент "За ...."
                    PointedOtkaz.RemoveItemUpdateNote(ActUC.DepoChangeTB.Text)
                Else
                    Koment += $" на {PointedOtkaz.KtoZakryl}"
                End If
            End If
            'остается с элементом "Вернулся разобраться и с историей ОТС
            ' добавить ветку проверки вохзврата от др служб "передан на"
            Koment += $" с другой дороги ({DorPr(0)}, {DorPr(1)})"

            PointedOtkaz.ZaKem = ""
            PointedOtkaz.Peredan = Date.MinValue
            ' === ЗАПИСЬ В ИСТОРИЮ ===
            ' Дата передается объектом, текст отдельно
            PointedOtkaz.AddHistoryEntry(Dat, Koment)

            ' === СБРОС ДАННЫХ ===

            PointedOtkaz.RemoveItemUpdateNote(ActUC.ReturnedTB.Text)

        End Sub


        Function GetDoroga_Prichina() As List(Of String)
            Dim win As New RoadSourceWindow()
            win.Owner = MW

            If win.ShowDialog() = True Then
                Return New List(Of String) From {win.SelectedRoad, win.SelectedPurpose}
            Else
                Return Nothing
            End If
        End Function


        Function SetZakem()
            Dim win As New ZakemWindow()
            win.Owner = MW
            win.Initialize("За кем расследован отказ", Otkaz.ZakemFilter.RassledZa)

            Return IIf(win.ShowDialog() = True, win.SelectedZakem, Nothing)

        End Function

        Function SetSavedZakem()
            Dim win As New ZakemWindow()
            win.Owner = MW
            win.Initialize("За кем сохранен отказ", Otkaz.ZakemFilter.SavedOnly)

            Return If(win.ShowDialog() = True, win.SelectedZakem, Nothing)


        End Function

        Function SeToOutZakem()
            Dim win As New ZakemWindow()
            win.Owner = MW
            win.Initialize("Отнесение отказа:", Otkaz.ZakemFilter.ToOut)

            Return If(win.ShowDialog() = True, win.SelectedZakem, Nothing)


        End Function


        Private Sub UserControl_Unloaded(sender As Object, e As RoutedEventArgs)
            'RemoveHandler MW.SharedCalendar.DateSelected, AddressOf OnCalendarDateSelected
        End Sub

        Public Sub New()
            InitializeComponent()
            ' ✅ Защищаемся от доступа к неинициализированному MW
            If MW?.SharedCalendar IsNot Nothing Then
                AddHandler MW.SharedCalendar.DateSelected, AddressOf OnCalendarDateSelected
            End If
            'AddHandler MW.SharedCalendar.DateSelected, AddressOf OnCalendarDateSelected
        End Sub

        Public Sub OpenCalendarForProperty(propertyName As String)
            ' 5. ПРИ ОТКРЫТИИ запоминаем, кто открыл (Me - это текущий экземпляр строки)
            _activeControlContext = Me
            ' Используем propertyName и как режим, и как имя свойства
            MW.SharedCalendar.EditMode = propertyName  ' "Postup" или "Zakryt"

            BindingOperations.ClearBinding(MW.SharedCalendar, SingleCalendarControl.TargetDateProperty)

            'сразу привязываем к целевому свойству (при указании даты - она сразу установится через SingleCalendarControl в свойство PointedOtkaz-а
            MW.SharedCalendar.SetBinding(SingleCalendarControl.TargetDateProperty,
        New Binding(propertyName) With {
            .Source = PointedOtkaz,
            .Mode = BindingMode.TwoWay
        })

            MW.SharedCalendar.CalendarPopup.PlacementTarget = MW
            MW.SharedCalendar.CalendarPopup.Placement = PlacementMode.Center
            MW.SharedCalendar.IsOpen = True

        End Sub

        Sub StandartMsage(elementText)
            MessageBox.Show(
            $"Клик по: {elementText}{vbCrLf}Отказ: #{PointedOtkaz.Id}" & vbCrLf &
            $"UpdateNotes: {PointedOtkaz.UpdateNotes}",
            "Тест клика",
            MessageBoxButton.OK,
            MessageBoxImage.Information)
        End Sub


        Private Sub NoteItem_MouseDown(sender As Object, e As MouseButtonEventArgs)
            Dim border = TryCast(sender, Border)
            If border Is Nothing OrElse PointedOtkaz Is Nothing Then Return

            e.Handled = True

            If {"IstocnikItem", "ManualInputItem"}.Contains(border.Name) Then
                MW.IstochPopup.IsOpen = True
            ElseIf border.Name = "IsNewOTSItem" Then
                OpenCalendarForProperty("Postup")
                e.Handled = True
            ElseIf border.Name = "SavedItem" Then
                OpenCalendarForProperty("SavedItem")
                e.Handled = True
            ElseIf border.Name = "ClosedItem" Then
                OpenCalendarForProperty("Zakryt")
                e.Handled = True
            ElseIf border.Name = "DepoChangeItem" Then
                KtoZakr_SUB()
                e.Handled = True
            ElseIf border.Name = "ReturnedItem" Then
                OpenCalendarForProperty("Vernulsa")
                e.Handled = True
            ElseIf border.Name = "RestoredItem" Then
                OpenCalendarForProperty("Vosstanovlen")
                e.Handled = True
            ElseIf border.Name = "QuestionMarksItem" Then
                OpenCalendarForProperty("OutFromRail")
                e.Handled = True
            ElseIf border.Name = "CategoryChangeItem" Then
                '"Восстановлен" RestoredItem RestoredTB.Text Vosstanovlen
                Dim NKat As String = Replace(CategoryChangeTB.Text, "кат ", "")
                'PointedOtkaz.History.Add(New HistoryEntry With {.ShowDate = False, .EventDate = Now, .Description = $"Изменена категория с {PointedOtkaz.Kat} на {NKat}"})
                PointedOtkaz.Kat = CInt(NKat)
                PointedOtkaz.RemoveItemUpdateNote(CategoryChangeTB.Text)
                e.Handled = True
            ElseIf border.Name = "PCHChangeItem" Then
                Dim NPCH As Single = CSng(Replace(PCHChangeTB.Text, "п/ч ", ""))
                PointedOtkaz.PCh = NPCH
                PointedOtkaz.RemoveItemUpdateNote(PCHChangeTB.Text)
                e.Handled = True
            Else
                StandartMsage(border.Tag?.ToString())
                'PointedOtkaz.AddHistoryEntry(eventDate, desc)
            End If


            'If ClearNotesItem.Visibility = Visibility.Visible Then
            '    PointedOtkaz.ClearAllItemUpdateNotes()
            'End If
        End Sub

        Private Sub EmptyFieldItem_MouseDown(sender As Object, e As MouseButtonEventArgs)
            Dim border = TryCast(sender, Border)
            If border Is Nothing OrElse PointedOtkaz Is Nothing Then Return
            e.Handled = True

            Select Case border.Name
                Case "NoSerLokItem"
                    MW.SerLokPopup.IsOpen = True
                Case "NoMyKlasLev1Item"
                    MW.EquipmentContent.ResetSelection()
                    MW.EquipmentPopup.IsOpen = True
                Case "NoMashItem"
                    MW.PripMashPopup.Istochnik = PointedOtkaz?.Istochnik
                    MW.PripisMash.IsOpen = True
                Case "ChangeIdItem"
                    ShowChangeFRM(PointedOtkaz?.Id, PointedOtkaz?.NewId, PointedOtkaz)




            End Select
        End Sub




        Private Sub DeleteItem(sender As Object, e As MouseButtonEventArgs)
            e.Handled = True
            ' sender - это сам TextBlock с крестиком
            Dim cross = TryCast(sender, TextBlock)

            ' Если что-то не так или нет объекта отказа - выходим
            If cross Is Nothing OrElse PointedOtkaz Is Nothing Then Return

            ' Достаем текст прямо из Tag крестика (благодаря биндингу)
            Dim textToRemove As String = TryCast(cross.Tag, String)

            If String.IsNullOrEmpty(textToRemove) Then Return

            ' Удаляем
            PointedOtkaz.RemoveItemUpdateNote(textToRemove)

        End Sub

        Private Sub ClearNotesItem_PreviewMouseUp(sender As Object, e As MouseButtonEventArgs)
            'ClearNotesItem.Visibility = Visibility.Collapsed
            PointedOtkaz.ClearAllItemUpdateNotes()
        End Sub

        Private Sub PlaceChangeItem_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs)
            Clipboard.SetText($"{PointedOtkaz.PreviousMestoOTS_TXT}{vbCrLf}{vbCrLf}{PointedOtkaz.MestoOTS_TXT}")
        End Sub


    End Class
End Namespace

