Imports System.Collections.ObjectModel
Imports System.Text.RegularExpressions
Imports System.Windows.Controls.Primitives
Imports System.Windows.Forms
Imports System.Windows.Threading

Namespace Kas
    Partial Public Class OTSRowControl
        Public Shared ReadOnly SearchTermProperty As DependencyProperty =
    DependencyProperty.Register("SearchTerm", GetType(String), GetType(OTSRowControl),
        New PropertyMetadata("", AddressOf OnSearchTermChanged))

        Public Property SearchTerm As String
            Get
                Return CType(GetValue(SearchTermProperty), String)
            End Get
            Set(value As String)
                SetValue(SearchTermProperty, value)
            End Set
        End Property

        Private Shared Sub OnSearchTermChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim control = TryCast(d, OTSRowControl)
            If control IsNot Nothing AndAlso control.DataContext IsNot Nothing Then
                control.UpdateHighlightedText()
            End If
        End Sub

        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().
            'AddHandler Me.DataContextChanged, AddressOf OnDataContextChanged

        End Sub

        'Private Sub OnDataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
        '    UpdateHighlightedText()
        'End Sub


        Private Sub TableRowBorder_PreviewMouseDown(sender As Object, e As MouseButtonEventArgs)
            'If e.ChangedButton = MouseButton.Left Then
            '    ' Получаем данные строки
            '    Dim border = TryCast(sender, Border)
            '    Dim dataContext = TryCast(sender?.DataContext, Otkaz)
            '    SetPointer(dataContext)
            'End If
        End Sub


        Private Sub OpenCalendarForDateField(sender As Object, e As MouseButtonEventArgs)
            If sender.name = "PostupField" Then
                ActionUC.OpenCalendarForProperty("Postup")
                'OpenCalendarForProperty("Postup", ActionUC)
                e.Handled = True
            End If



        End Sub



        ' Обновляем ссылку на текущий выделенный объект
        Sub SetPointer(sender As Otkaz)
            If sender IsNot Nothing Then
                If PointedOtkaz Is sender Then
                    'PointedOtkaz = Nothing
                Else
                    PointedOtkaz = sender

                End If
            End If
        End Sub





        Private Sub Label_PreviewMouseRightButtonDown(sender As Object, e As MouseButtonEventArgs)

            Dim dataContext As Otkaz = TryCast(sender?.DataContext, Otkaz)
            Clipboard.SetText(dataContext.Id)
            Dim F As String = $"** Номер отказа {dataContext.Id} скопирован **"
            MW.InfoBLOK.AddItem(F)
            MW.InfoBLOK.ScrollToEnd()
            If e.ClickCount = 2 Then
                ' Если окно не создано ИЛИ оно было закрыто ( IsLoaded = False )
                If Not IsNothing(KasAntWND) OrElse KasAntWND?.IsLoaded Then
                    ' Если окно уже открыто, просто закрываем его
                    KasAntWND.Close()
                End If
                ' Создаём экземпляр окна KasAntWin с параметром
                KasAntWND = New KasAntWin(dataContext.Id, "88") With {.Owner = MW}
                KasAntWND.Show()
            End If
            e.Handled = True
        End Sub

        Private Sub Label_PreviewMouseRightButtonUp(sender As Object, e As MouseButtonEventArgs)


            If PointedOtkaz IsNot Nothing Then

                ' 1. Ищем среди открытых окон уже существующее окно инспектора
                Dim existingWin As OtkazInspectorWindow = Nothing
                For Each w In Application.Current.Windows
                    If TypeOf w Is OtkazInspectorWindow Then
                        existingWin = CType(w, OtkazInspectorWindow)
                        Exit For
                    End If
                Next

                ' 2. Если окно УЖЕ открыто — просто обновляем данные и выводим его на передний план
                If existingWin IsNot Nothing Then
                    existingWin.UpdateData(PointedOtkaz)
                    existingWin.Activate() ' Делает окно активным и выводит его поверх остальных
                Else
                    ' 3. Если окна НЕТ — создаем и показываем новое (оно встанет по центру, как и положено при первом открытии)
                    Dim win As New OtkazInspectorWindow(PointedOtkaz)
                    win.Owner = Application.Current.MainWindow
                    win.Show()
                End If

            Else
                ShowMSG(MW, "Отказ не выбран", "Внимание", MessageBoxButton.OK)
            End If

        End Sub




        Private Sub Grid_PreviewMouseDown(sender As Object, e As MouseButtonEventArgs)
            ' Получаем данные строки
            Dim border = TryCast(sender, Border)
            Dim dataContext = TryCast(sender?.DataContext, Otkaz)
            SetPointer(dataContext)
        End Sub

        Private Sub UpdateNotes_MouseDown(sender As Object, e As MouseButtonEventArgs)

        End Sub

        Private Sub IstochTBlock_PreviewMouseRightButtonDown(sender As Object, e As MouseButtonEventArgs)
            MW.IstochPopup.IsOpen = True
        End Sub

        Public Sub KtoZakTBlock_PreviewMouseRightButtonUp(sender As Object, e As MouseButtonEventArgs)
            KtoZakr_SUB()
            e.Handled = True
        End Sub

        Private Sub MarkedCheckBox_Changed(sender As Object, e As RoutedEventArgs)
            MW.TRowsContainer.UpdateShowDemarkButtonVisibility()
            MW.TRowsContainer.UpdateShowMarkAllButtonVisibility()
        End Sub



        Private Sub OborudTB_PreviewMouseRightButtonUp(sender As Object, e As MouseButtonEventArgs)
            MW.EquipmentContent.ResetSelection()
            MW.EquipmentPopup.IsOpen = True
        End Sub

        Private Sub IsStationPAN_PreviewMouseRightButtonUp(sender As Object, e As MouseButtonEventArgs)
            PointedOtkaz.IsStation = Not PointedOtkaz.IsStation
        End Sub

        Private Sub LokTB_PreviewMouseRightButtonUp(sender As Object, e As MouseButtonEventArgs)
            MW.SerLokPopup.IsOpen = True
        End Sub

        Private Sub MashTB_PreviewMouseRightButtonUp(sender As Object, e As MouseButtonEventArgs)
            MW.PripMashPopup.Istochnik = PointedOtkaz?.Istochnik 'чтобы знать, нужен машинист или нет
            MW.PripisMash.PlacementTarget = MW
            MW.PripisMash.IsOpen = True
        End Sub



        Private Sub MashFamTB_MouseDown(sender As Object, e As MouseButtonEventArgs)
            If e.ClickCount = 2 Then
                With PointedOtkaz
                    .Mash = ""
                    .PripMash = ""

                End With
            End If
        End Sub

        Private Sub ZakemTBlock_PreviewMouseUp(sender As Object, e As MouseButtonEventArgs)
            If e.ChangedButton = MouseButton.Right Then
                PointedOtkaz.ClearAllItemUpdateNotes()
                PointedOtkaz.SetUpdateNotesList()
                e.Handled = True
                'ActionUC.ClearNotesItem.Visibility = Visibility.Visible
            End If

        End Sub

        Private Sub LokPrip_MouseDown(sender As Object, e As MouseButtonEventArgs)
            If e.ClickCount = 2 Then
                With PointedOtkaz
                    .SerLokExact = ""
                    .PripLok = ""

                End With
            End If
        End Sub



        Private Async Sub PlanTxtBLK_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs)
            'Код стал брать данные прямо из-под мышки в момент клика, а не из старой глобальной переменной PointedOtkaz. Поэтому проверка на «трп» сразу ожила.
            ' 1. Приводим к FrameworkElement (у него 100% есть DataContext и компилятор не будет ругаться)
            Dim element = TryCast(sender, FrameworkElement)
            If element Is Nothing Then Return

            ' 2. Берем Otkaz напрямую из DataContext этого ListBox
            Dim actualOtkaz = TryCast(element.DataContext, Otkaz)


            '3. Проверяем условия для ТОГО элемента, на который реально нажали
            If actualOtkaz Is Nothing Then Return

            If Not PointedOtkaz.Equals(actualOtkaz) Then
                PointedOtkaz = actualOtkaz
            End If

            With actualOtkaz
                If .Zakryt > Date.MinValue Then Return
                If Not .VRassled Then Return
                If .KtoZakryl.ToLower.Contains("трп") Then Return

            End With


            'e.Handled = True ' Гасим клик

            ' 4. Принудительно гасим старый попап с паузой для сброса StaysOpen="False"
            If PlanPopup.MainPopup.IsOpen Then
                PlanPopup.MainPopup.IsOpen = False
                Await Task.Delay(50) ' Даем WPF 50 миллисекунд на полное закрытие
            End If

            ' 5. Открываем попап со свежими данными
            PlanPopup.ShowPlans(actualOtkaz)


        End Sub




        Private Sub OpenPlanPopup()
            If PointedOtkaz IsNot Nothing Then
                PlanPopup.ShowPlans(PointedOtkaz)
            End If
        End Sub

        Private Sub PlanItem_MouseEnter(sender As Object, e As Input.MouseEventArgs)
            Dim item = TryCast(sender, ListBoxItem)
            If item Is Nothing Then Return

            Dim planEntry = TryCast(item.DataContext, PlanEntry)
            If planEntry Is Nothing Then Return

            SetHistoryMoused(planEntry.Description, True)
        End Sub

        Private Sub PlanItem_MouseLeave(sender As Object, e As Input.MouseEventArgs)
            Dim item = TryCast(sender, ListBoxItem)
            If item Is Nothing Then Return

            Dim planEntry = TryCast(item.DataContext, PlanEntry)
            If planEntry Is Nothing Then Return

            SetHistoryMoused(planEntry.Description, False)
        End Sub

        Private Sub SetHistoryMoused(text As String, isMoused As Boolean)
            ' Получаем Otkaz из DataContext списка
            Dim otkaz = TryCast(PlanTxtBLK.DataContext, Otkaz)
            If otkaz Is Nothing Then Return
            Dim PLEn As New PlanEntry
            PLEn.Description = text
            ' Ищем элемент истории по описанию плана
            Dim entry As HistoryEntry = otkaz.GetHistoryEntryByDescription(PLEn.Description)

            ' Если по Description не нашлось, пробуем по DisplayText
            If entry IsNot Nothing Then
                entry.IsMoused = isMoused

            End If
            entry = otkaz.GetHistoryEntryByDisplayText(PLEn.DisplayText)

            If entry IsNot Nothing Then
                entry.IsMoused = isMoused

            End If
        End Sub

        Private Sub UpdateHighlightedText()
            TxtBLK.Inlines.Clear()

            Dim otkaz = TryCast(DataContext, Otkaz)
            If otkaz Is Nothing Then Return

            ' 🔥 Защита от Nothing в Opis
            Dim opisText As String = If(otkaz.Opis, "")

            Dim searchTerm = Me.SearchTerm?.Trim()
            If String.IsNullOrEmpty(searchTerm) Then
                ' Просто отображаем текст с переносами
                Dim lines0 = opisText.Split({vbCrLf, vbCr, vbLf}, StringSplitOptions.None)
                For i As Integer = 0 To lines0.Length - 1
                    TxtBLK.Inlines.Add(New Run(lines0(i)))
                    If i < lines0.Length - 1 Then
                        TxtBLK.Inlines.Add(New LineBreak())
                    End If
                Next
                Return
            End If

            ' Разбиваем на строки
            Dim lines = opisText.Split({vbCrLf, vbCr, vbLf}, StringSplitOptions.None)

            For lineIndex As Integer = 0 To lines.Length - 1
                Dim line = lines(lineIndex)
                If String.IsNullOrEmpty(line) Then
                    ' Пустая строка — просто LineBreak
                    TxtBLK.Inlines.Add(New LineBreak())
                    Continue For
                End If

                Dim cleanLine = line
                Dim cleanSearch = searchTerm.ToLower()
                Dim startIndex = 0

                While startIndex < cleanLine.Length
                    Dim remainingText = cleanLine.Substring(startIndex).ToLower()
                    Dim index = remainingText.IndexOf(cleanSearch)

                    If index = -1 Then
                        TxtBLK.Inlines.Add(New Run(cleanLine.Substring(startIndex)))
                        Exit While
                    End If

                    ' Часть до совпадения
                    If index > 0 Then
                        TxtBLK.Inlines.Add(New Run(cleanLine.Substring(startIndex, index)))
                    End If

                    ' Совпадение
                    Dim matchStart = startIndex + index
                    Dim matchLength = Math.Min(searchTerm.Length, cleanLine.Length - matchStart)
                    Dim matchText = cleanLine.Substring(matchStart, matchLength)
                    TxtBLK.Inlines.Add(New Run(matchText) With {
                        .FontWeight = FontWeights.Bold,
                        .Foreground = Brushes.Blue
                    })

                    startIndex = matchStart + matchLength
                End While

                ' Добавляем перенос, кроме последней строки
                If lineIndex < lines.Length - 1 Then
                    TxtBLK.Inlines.Add(New LineBreak())
                End If
            Next

        End Sub

        Private Sub PlanItem_MouseRightButtonDown(sender As Object, e As MouseButtonEventArgs)
            Dim lbi = CType(sender, ListBoxItem)
            Dim dc = lbi.DataContext

            ' ===== ПРАВЫЙ КЛИК ПО ПЛАНУ: спросить и удалить =====
            If TypeOf dc Is PlanEntry Then
                Dim plan = CType(dc, PlanEntry)
                Dim otkaz = TryCast(PlanTxtBLK.DataContext, Otkaz)

                If otkaz IsNot Nothing Then

                    If ShowMSG(MW, $"Удалить план ""{plan.Description}""?",
                                      "Удаление плана",
                                      MessageBoxButton.OKCancel,
                                      MessageBoxImage.Question) Then

                        DeletePlan(otkaz, plan)

                    End If
                End If

                e.Handled = True ' ← ГАСИМ событие, чтобы не открылся попап добавления!
                Return
            End If


            e.Handled = True ' ✅ Останавливаем всплытие к родителю
        End Sub



        Private _remontPopup As Popup
        Private _remontEditor As RemontEditor

        Private Sub DaNaLok_MouseRightButtonUp(sender As Object, e As MouseButtonEventArgs)

            If PointedOtkaz Is Nothing Then Return

            If _remontPopup IsNot Nothing AndAlso _remontPopup.IsOpen Then
                CloseRemontPopup()
                Return
            End If

            _remontEditor = New RemontEditor()

            If PointedOtkaz.DaNaLok Is Nothing Then
                PointedOtkaz.DaNaLok = New ObservableCollection(Of Remont)()
            End If
            _remontEditor.BindTo(PointedOtkaz.DaNaLok)

            AddHandler _remontEditor.Cancelled, AddressOf RemontEditor_Cancelled

            _remontPopup = New Popup With {
                .Child = _remontEditor,
                .Placement = PlacementMode.MousePoint,
                .StaysOpen = False,
                .AllowsTransparency = True
            }

            ' Ключевая строка — ловим закрытие по клику мимо
            AddHandler _remontPopup.Closed, AddressOf RemontPopup_Closed

            _remontPopup.IsOpen = True
            e.Handled = True
        End Sub

        Private Sub RemontPopup_Closed(sender As Object, e As EventArgs)
            CloseRemontPopup()
        End Sub

        Private Sub RemontEditor_Cancelled(sender As Object, e As EventArgs)
            CloseRemontPopup()
        End Sub

        Private Sub CloseRemontPopup()
            If _remontPopup IsNot Nothing Then
                RemoveHandler _remontPopup.Closed, AddressOf RemontPopup_Closed
                If _remontEditor IsNot Nothing Then
                    RemoveHandler _remontEditor.Cancelled, AddressOf RemontEditor_Cancelled
                End If
                _remontPopup.IsOpen = False
                _remontPopup = Nothing
                _remontEditor = Nothing
            End If
        End Sub

        '        If PointedOtkaz Is Nothing Then Return

        '        ' Если уже открыт — закрываем
        '        If _remontPopup IsNot Nothing AndAlso _remontPopup.IsOpen Then
        '            CloseRemontPopup()
        '            Return
        '        End If

        '        ' Создаём редактор
        '        _remontEditor = New RemontEditor()

        '        ' Привязываем к DaNaLok текущего отказа
        '        If PointedOtkaz.DaNaLok Is Nothing Then
        '            PointedOtkaz.DaNaLok = New ObservableCollection(Of Remont)()
        '        End If
        '        _remontEditor.BindTo(PointedOtkaz.DaNaLok)

        '        AddHandler _remontEditor.Cancelled, AddressOf RemontEditor_Cancelled

        '        ' Popup с закрытием по клику мимо
        '        _remontPopup = New Popup With {
        '    .Child = _remontEditor,
        '    .Placement = PlacementMode.MousePoint,
        '    .StaysOpen = False,
        '    .AllowsTransparency = True
        '}

        '        _remontPopup.IsOpen = True
        '        e.Handled = True
        'End Sub

        'Private Sub RemontEditor_Cancelled(sender As Object, e As EventArgs)
        '    CloseRemontPopup()
        'End Sub

        'Private Sub CloseRemontPopup()
        '    If _remontPopup IsNot Nothing Then
        '        If _remontEditor IsNot Nothing Then
        '            RemoveHandler _remontEditor.Cancelled, AddressOf RemontEditor_Cancelled
        '        End If
        '        _remontPopup.IsOpen = False
        '        _remontPopup = Nothing
        '        _remontEditor = Nothing
        '    End If
        'End Sub








    End Class
End Namespace

