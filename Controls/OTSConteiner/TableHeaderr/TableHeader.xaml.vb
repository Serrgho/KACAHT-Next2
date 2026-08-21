Imports System.Windows.Controls.Primitives


Namespace Kas
    Partial Public Class TableHeader


        Inherits UserControl

        Public _currentPopup As Popup

        ' В начале класса TableHeader
        Public Event FiltersChanged As EventHandler

        Public Shared ReadOnly ItemsSourceProperty As DependencyProperty =
        DependencyProperty.Register(
            "ItemsSource",
            GetType(IEnumerable(Of Otkaz)),
            GetType(TableHeader),
            New PropertyMetadata(Nothing)
        )





        Public _filterState As New FilterState()

        Public Property ItemsSource As IEnumerable(Of Otkaz)
            Get
                Return DirectCast(GetValue(ItemsSourceProperty), IEnumerable(Of Otkaz))
            End Get
            Set(value As IEnumerable(Of Otkaz))
                SetValue(ItemsSourceProperty, value)
            End Set
        End Property

        '' Также добавим событие фильтрации — чтобы OTSBlockControl мог на него подписаться
        'Public Event FilterRequested As EventHandler(Of FilterEventArgs)

        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().

            'Me.DataContext = Me  ' ← даёт FilterIndicator доступ к _filterState

            ' --- НОВОЕ: Присваиваем ссылку на себя (TableHeader) индикатору ---
            If KatIndicator IsNot Nothing Then
                KatIndicator.HeaderRef = Me ' <-- Это ключевая строка!
            End If
            If NachIndicator IsNot Nothing Then
                NachIndicator.HeaderRef = Me ' <-- Это ключевая строка!
            End If

            If IstochnikIndicator IsNot Nothing Then
                IstochnikIndicator.HeaderRef = Me
            End If
            If KtoZakrylIndicator IsNot Nothing Then
                KtoZakrylIndicator.HeaderRef = Me
            End If
            'If ZaKemIndicator IsNot Nothing Then
            '    ZaKemIndicator.HeaderRef = Me
            'End If
            If Zakem_TXTIndicator IsNot Nothing Then
                Zakem_TXTIndicator.HeaderRef = Me
            End If
            'If KomplexAsIntIndicator IsNot Nothing Then
            '    KomplexAsIntIndicator.HeaderRef = Me
            'End If
            If PostupIndicator IsNot Nothing Then
                PostupIndicator.HeaderRef = Me
            End If

            If VernulsaOTSIndicator IsNot Nothing Then
                VernulsaOTSIndicator.HeaderRef = Me
            End If

            If DaysOnRassledIndicator IsNot Nothing Then
                DaysOnRassledIndicator.HeaderRef = Me
            End If
            If UpdateNotesIndicator IsNot Nothing Then
                UpdateNotesIndicator.HeaderRef = Me
            End If
            If MarkedIndicator IsNot Nothing Then
                MarkedIndicator.HeaderRef = Me
            End If

            'If ZakrytIndicator IsNot Nothing Then
            '    ZakrytIndicator.HeaderRef = Me
            'End If
            'If PeredanIndicator IsNot Nothing Then
            '    PeredanIndicator.HeaderRef = Me
            'End If

            ' Если есть другие индикаторы (SeriaIndicator и т.д.), сделай то же самое
            ' If SeriaIndicator IsNot Nothing Then
            '     SeriaIndicator.HeaderRef = Me
            ' End If
            ' --------------------------


        End Sub




        Public Sub ClearAllFilters()
            _filterState.Clear()
            'и снимаем индикаторы
            Dim knownProperties As String() = {"Kat", "Nach", "PCh", "Istochnik", "KtoZakryl", "ZaKem", "Zakem_TXT", "KomplexAsInt", "Postup", "DaysOnRassled", "Peredan", "UpdateNotes", "Marked"}

            For Each prop In knownProperties
                UpdateIndicatorForProperty(prop)
            Next
            ' -------------------------------

        End Sub

        Public Function HasActiveFilters() As Boolean
            'Return _activeFilters.Count > 0
            Return _filterState.HasActiveFilters()
        End Function

        Public Function BuildCombinedPredicate() As Func(Of Otkaz, Boolean)
            Return _filterState.BuildPredicate()
        End Function






        Private Sub UpdateIndicatorForProperty(propertyName As String)
            ' Формируем имя индикатора по соглашению: {propertyName}Indicator
            Dim indicatorName = propertyName & "Indicator"

            ' Ищем элемент по имени в визуальном дереве TableHeader
            Dim indicatorElement = FindName(indicatorName)

            ' --- НЕТ СИ ---
            Dim findNameResult As String
            If indicatorElement IsNot Nothing Then
                findNameResult = indicatorElement.ToString()
            Else
                findNameResult = "Nothing"
            End If
            'Debug.WriteLine($"FindName result for {indicatorName}: " & findNameResult)
            ' --- КОНЕЦ ---

            ' --- НЕТ СИ ---
            Dim findNameTypeResult As String
            If indicatorElement IsNot Nothing Then
                findNameTypeResult = indicatorElement.GetType().Name
            Else
                findNameTypeResult = "N/A"
            End If
            'Debug.WriteLine($"FindName result type: " & findNameTypeResult)
            ' --- КОНЕЦ ---

            ' Проверяем, нашли ли и является ли он FilterIndicator
            Dim indicator As FilterIndicator = TryCast(indicatorElement, FilterIndicator)

            ' --- НЕТ СИ ---
            Dim tryCastResult As String
            If indicator IsNot Nothing Then
                tryCastResult = indicator.ToString()
            Else
                tryCastResult = "Nothing"
            End If
            'Debug.WriteLine($"TryCast to FilterIndicator result: " & tryCastResult)
            ' --- КОНЕЦ ---

            If indicator IsNot Nothing Then
                ' Если нашли, вызываем UpdateIsFiltered
                indicator.UpdateIsFiltered()
            Else
                ' Необязательно: логировать, если индикатор не найден
                'Debug.WriteLine($"Индикатор для {indicatorName} не найден.")
            End If
        End Sub


        ' Событие — чтобы OTSBlockControl мог на него подписаться
        Public Event ResetRequested As EventHandler
        Public Sub ResetRequest_MouseDown(sender As Object, e As MouseButtonEventArgs)
            RaiseEvent ResetRequested(Me, e)
        End Sub

        Private Sub TextBlock_PreviewMouseDown(sender As Object, e As MouseButtonEventArgs)
            ResetRequest_MouseDown(sender, e)
        End Sub

        Private Sub OnHeaderRightClick(sender As Object, e As MouseButtonEventArgs)
            ' Получаем имя свойства из Tag
            Dim propName As String = TryCast(sender, FrameworkElement)?.Tag?.ToString()
            If String.IsNullOrEmpty(propName) Then Return
            ' Убираем фильтр для этого propName в _filterState
            _filterState.ClearFilterForProperty(propName)
            ' Обновляем индикатор для этого свойства
            UpdateIndicatorForProperty(propName)
            ' Вызываем FiltersChanged, чтобы OTSBlockControl перестроил отображение
            RaiseEvent FiltersChanged(Me, EventArgs.Empty)
        End Sub


        Private Sub OnHeaderMouseDown(sender As Object, e As MouseButtonEventArgs)
            Dim border As Border = TryCast(sender, Border)
            If border Is Nothing Then Return
            Dim propertyName As String = CStr(border.Tag)
            ' --- Читаем FieldType и FilterLevel из XAML ---
            Dim fieldType As String = FilterProperties.GetFieldType(border)
            ' Если FieldType не задан или пустой, можно выдать ошибку или вернуть "String"
            If String.IsNullOrEmpty(fieldType) Then
                'Debug.WriteLine($"OnHeaderMouseDown: FieldType not set for property '{propertyName}', defaulting to 'String'.")
                fieldType = "String"
            End If
            ShowUniversalFilterPopup(sender, propertyName, fieldType)
        End Sub

        Private Sub ShowUniversalFilterPopup(sender As Object, propName As String, fieldType As String)
            ' Определяем, какой тип фильтра показать
            Select Case fieldType.ToLowerInvariant()
                Case "boolean", "string", "text"
                    ShowTextualFilterPopup(sender, propName, fieldType) ' Объединяем булево и текст
                Case "number", "numeric"
                    ShowNumericFilterPopupNew(sender, propName) '  логика для чисел
                Case "date"
                    ShowDateFilterPopupNew(sender, propName) '  логика для дат
                Case Else
                    ShowTextualFilterPopup(sender, propName, "String") ' По умолчанию - текст
            End Select
        End Sub

        Private Sub ShowTextualFilterPopup(sender As Object, propName As String, fieldType As String)

            ' Определяем, булевое ли это поле
            Dim isBooleanField = (fieldType.ToLowerInvariant() = "boolean")
            Dim popupCtrl As New FilterPopup()

            ' --- БЕРЕМ ИСТОЧНИК ДАННЫХ: для 0-уровня фильтра - это результат 0-уровня фильтрации, но до 1-го (THed)
            Dim currentSource As IEnumerable(Of Otkaz) = MW.TRowsContainer.OTSContainer.ItemsSource ' 'Me._level0Filtered <-- ИСПРАВЛЕНО: Используем _level0Filtered
            If currentSource Is Nothing Then
                ' Если нет данных, можно показать пустой список или "[Все]"
                popupCtrl.PropertyName = propName
                popupCtrl.FieldType = fieldType
                popupCtrl.FilterListBox.ItemsSource = If(isBooleanField, New List(Of String) From {"[Все]"}, New List(Of String) From {"[Все]"})
                ' ... (логика отображения popupCtrl)
                Return
            End If

            Dim values As List(Of String) = Nothing

            If isBooleanField Then ' Получаем значения для булевого поля
                values = GetBooleanValuesForProperty(currentSource, propName)
                ' Если ни один бул не встречается (хотя source не пуст), всё равно добавим "[Все]"
                If values.Count = 0 Then values.Add("[Все]")
            Else ' Считаем строковым ' Получаем значения для строкового поля
                values = GetTextualValuesForProperty(currentSource, propName)
                ' Сортируем значения в зависимости от состояния переключателя
                Dim isSortByCount As Boolean = MW.TRowsContainer.SortOrderToggleUC.MYToggle.IsChecked = True
                values = SortValuesByToggle(values, propName, isSortByCount)
            End If

            popupCtrl.PropertyName = propName
            popupCtrl.FieldType = fieldType
            popupCtrl.FilterListBox.ItemsSource = Nothing ' <-- Очистить
            popupCtrl.FilterListBox.ItemsSource = values

            ' --- ОБНОВЛЁННОЕ восстановление выделения ---
            Dim saved = _filterState.GetFilter(propName)
            ' Вызываем вспомогательный метод
            RestoreSelection(popupCtrl, saved, isBooleanField, values)

            ' --- Создаём Popup ---
            Dim popup As New Popup With {
        .Placement = PlacementMode.Bottom,
        .PlacementTarget = sender,
        .StaysOpen = False,
        .AllowsTransparency = True,
        .Child = popupCtrl
    }


            ' Автоматически включаем/выключаем кнопку КОПИРОВАТЬ в зависимости от наличия данных
            If values Is Nothing OrElse values.Count = 0 Then
                popupCtrl.CopyButton.IsEnabled = False
                popupCtrl.CopyButton.ToolTip = "Нет данных для копирования"
            Else
                popupCtrl.CopyButton.IsEnabled = True
                popupCtrl.CopyButton.ToolTip = $"Скопировать {values.Count} значений в буфер обмена"
            End If


            ' --- ОБНОВЛЁННЫЙ обработчик OK ---
            ' Подписываемся на событие, передавая нужные аргументы через замыкание (closure)
            AddHandler popupCtrl.OkButt.Click, Sub(s, args) OnPopupOkClicked(s, args, popupCtrl, popup, propName, isBooleanField)

            ' --- Открываем Popup ---
            If _currentPopup IsNot Nothing Then _currentPopup.IsOpen = False
            _currentPopup = popup
            popup.IsOpen = True

        End Sub


        '--- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ (аналогично OTSBlockControl, можно вынести в общий модуль, если используются в обоих) ---
        ' Получает список строк "Значение [Количество]" из источника для строкового свойства
        Private Function GetTextualValuesForProperty(currentSource As IEnumerable(Of Otkaz), propName As String) As List(Of String)

            Dim selector = OtkazFilterConfig.GetDisplaySelector(propName)
            If selector Is Nothing Then Return {"[Все]"}.ToList()

            Dim allValues = currentSource.Select(selector).ToList()
            Dim emptyCount = allValues.Where(Function(v) String.IsNullOrWhiteSpace(CStr(v))).Count
            Dim nonEmptyGroups = allValues.
    Where(Function(v) Not String.IsNullOrWhiteSpace(CStr(v))).
                GroupBy(Function(v) v).
                Select(Function(g) $"{g.Key} ({g.Count()})")

            Dim result As New List(Of String) From {"[Все]"}
            If emptyCount > 0 Then
                result.Add($"[ПУСТЫЕ] [{emptyCount}]")
            End If
            result.AddRange(nonEmptyGroups.OrderBy(Function(s) s))
            Return result


            'Dim selector = OtkazFilterConfig.GetDisplaySelector(propName)
            'If selector Is Nothing Then Return New List(Of String)

            'Dim grouped = currentSource.GroupBy(selector).Where(Function(g) Not String.IsNullOrEmpty(g.Key))
            'Dim values = grouped.Select(Function(g) $"{g.Key} [{g.Count()}]").ToList()

            '' ВСЕГДА добавляем [Все] в начало перед сортировкой
            'values.Insert(0, "[Все]")
            'Return values
        End Function

        ' Получает список строк "Да [N]", "Нет [N]" из источника для булевого свойства
        Private Function GetBooleanValuesForProperty(currentSource As IEnumerable(Of Otkaz), propName As String) As List(Of String)
            Dim getter As Func(Of Otkaz, Boolean) = OtkazFilterConfig.GetBooleanGetter(propName)
            If getter Is Nothing Then Return New List(Of String)

            Dim grouped = currentSource.GroupBy(Function(o) getter(o))
            Dim values = New List(Of String)
            For Each g In grouped
                Dim displayText As String = If(g.Key, "Да", "Нет")
                values.Add($"{displayText} [{g.Count()}]")
            Next
            values.Sort() ' Сортировка: "Да [N]" и "Нет [N]" в алфавитном порядке

            If values.Count > 0 Then
                values.Insert(0, "[Все]")
            Else
                values.Add("[Все]") ' Если других значений нет, просто добавим "[Все]"
            End If
            Return values
        End Function

        ' Сортирует список строк "Значение [Количество]" в зависимости от флага isSortByCount
        Private Function SortValuesByToggle(values As List(Of String), propName As String, isSortByCount As Boolean) As List(Of String)
            ' Убираем "[Все]" из сортировки, если он есть
            Dim allItem As String = Nothing
            If values.Count > 0 AndAlso values(0) = "[Все]" Then
                allItem = values(0)
                values.RemoveAt(0)
            End If

            If isSortByCount Then ' Сортировка по количеству (убыванию, затем по имени)
                values = values.OrderByDescending(Function(s)
                                                      Dim startIdx = s.IndexOf(" (")
                                                      Dim endIdx = s.IndexOf(")")
                                                      If startIdx >= 0 AndAlso endIdx > startIdx Then
                                                          Dim countStr = s.Substring(startIdx + 2, endIdx - startIdx - 2)
                                                          Dim countValue As Integer
                                                          If Integer.TryParse(countStr, countValue) Then
                                                              Return countValue
                                                          Else
                                                              Return 0
                                                          End If
                                                      Else
                                                          Return 0
                                                      End If
                                                  End Function).
            ThenBy(Function(s) s).
            ToList()
            Else ' Сортировка по имени (алфавиту)
                values = values.OrderBy(Function(s) s).ToList()

                ' Специфическая сортировка для ZaKem
                If propName = "ZaKem" OrElse propName = "Zakem_TXT" Then
                    values = values.OrderBy(Function(s) If(s.StartsWith(OtkazFilterConfig.UNASSIGNED_MARKER), 0, 1)).ThenBy(Function(s) s).ToList()
                ElseIf {"DaysOnRassled", "KomplexAsInt", "Kat"}.Contains(propName) Then
                    ' Для числовых - сортировка по числовому значению до "["
                    values = values.OrderBy(Function(s) Val(If(s.Contains("("), s.Substring(0, s.IndexOf("(")), s))).ToList()
                End If
            End If

            ' Возвращаем "[Все]" в начало списка
            If allItem IsNot Nothing Then
                values.Insert(0, allItem)
            End If

            Return values
        End Function

        ' Восстанавливает выделение в ListBox на основе сохранённого состояния фильтра
        Private Sub RestoreSelection(popupCtrl As FilterPopup, savedFilterValues As List(Of Object), isBooleanField As Boolean, allValues As List(Of String))
            SafeClearSelection(popupCtrl.FilterListBox)

            If isBooleanField Then
                For Each item In savedFilterValues
                    Dim itemStr = item?.ToString()
                    If Not String.IsNullOrEmpty(itemStr) Then
                        Dim match = allValues.FirstOrDefault(Function(v) (itemStr.Equals("True", StringComparison.OrdinalIgnoreCase) AndAlso v.StartsWith("Да [")) OrElse
        (itemStr.Equals("False", StringComparison.OrdinalIgnoreCase) AndAlso v.StartsWith("Нет [")))
                        If match IsNot Nothing Then
                            SafeAddToSelection(popupCtrl.FilterListBox, match)
                        End If
                    End If
                Next
            Else ' строковое поле
                For Each item In savedFilterValues
                    Dim originalItem = item?.ToString()
                    Dim match = allValues.FirstOrDefault(Function(v) v.StartsWith(originalItem & " ("))
                    If match IsNot Nothing Then
                        SafeAddToSelection(popupCtrl.FilterListBox, match)
                    End If
                Next
            End If
        End Sub

        ' --- ВСПОМОГАТЕЛЬНЫЙ МЕТОД: Обработчик нажатия OK в фильтре ---
        Private Sub OnPopupOkClicked(sender As Object, e As RoutedEventArgs, popupCtrl As FilterPopup, popup As Popup, propName As String, isBooleanField As Boolean)
            Dim selected = SafeGetSelectedItems(popupCtrl.FilterListBox)
            Dim originalValues = selected.Select(Function(item)
                                                     Dim str = item?.ToString()
                                                     If (str?.Contains(" (") AndAlso str.EndsWith(")")) OrElse (str?.Contains(" [") AndAlso str.EndsWith("]")) Then
                                                         If isBooleanField Then
                                                             If str.StartsWith("Да ") Then
                                                                 Return "True"
                                                             ElseIf str.StartsWith("Нет ") Then
                                                                 Return "False"
                                                             Else
                                                                 Return str.Substring(0, str.IndexOf(" ["))
                                                             End If
                                                         Else
                                                             ' Получаем индексы, если не найдено - используем максимальное значение
                                                             Dim index1 As Integer = str.IndexOf(" (")
                                                             Dim index2 As Integer = str.IndexOf(" [")
                                                             Dim endIndex As Integer = Math.Min(If(index1 >= 0, index1, Integer.MaxValue), If(index2 >= 0, index2, Integer.MaxValue)
)
                                                             ' Если найден хотя бы один разделитель
                                                             If endIndex <> Integer.MaxValue Then
                                                                 Return str.Substring(0, endIndex)
                                                             End If

                                                             ' Если разделители не найдены
                                                             Return str
                                                             'Return str.Substring(0, str.IndexOf(" ("))
                                                         End If
                                                     Else
                                                         Return str
                                                     End If
                                                 End Function).ToList()

            _filterState.SetFilter(propName, originalValues)
            ' ВАЖНО: Вызываем событие FiltersChanged у THed, которое OTSBlockControl (или MainWindow) будет слушать
            RaiseEvent FiltersChanged(Me, EventArgs.Empty)
            popup.IsOpen = False
            UpdateIndicatorForProperty(propName)
        End Sub


        ' Вспомогательный метод: безопасно очищает выделение
        Private Sub SafeClearSelection(listBox As ListBox)
            If listBox.SelectionMode = SelectionMode.Multiple Then
                listBox.SelectedItems.Clear()
            Else
                listBox.SelectedItem = Nothing
            End If
        End Sub

        ' Вспомогательный метод: безопасно добавляет элемент в выделение
        Private Sub SafeAddToSelection(listBox As ListBox, item As Object)
            If listBox.SelectionMode = SelectionMode.Multiple Then
                listBox.SelectedItems.Add(item)
            Else
                listBox.SelectedItem = item
            End If
        End Sub

        ' Вспомогательный метод: безопасно получает список выделенных
        Private Function SafeGetSelectedItems(listBox As ListBox) As List(Of Object)
            If listBox.SelectionMode = SelectionMode.Multiple Then
                Return listBox.SelectedItems.Cast(Of Object)().ToList()
            Else
                If listBox.SelectedItem IsNot Nothing Then
                    Return New List(Of Object) From {listBox.SelectedItem}
                Else
                    Return New List(Of Object)
                End If
            End If
        End Function


        Private Sub ShowDateFilterPopupNew(sender As Object, propName As String)
            Dim currentSource As IEnumerable(Of Otkaz) = TryCast(MW.TRowsContainer.OTSContainer.ItemsSource, IEnumerable(Of Otkaz)) 'TryCast(Me.ItemsSource, IEnumerable(Of Otkaz))
            If currentSource Is Nothing Then Return
            Dim previouslySelectedDates As HashSet(Of Date) = Nothing
            Dim filterValues As List(Of Object) = _filterState.GetFilter(propName)
            If filterValues?.Count > 0 Then
                previouslySelectedDates = New HashSet(Of Date)(
    filterValues.OfType(Of FilterValue)().Where(Function(fv) fv.Value IsNot Nothing).Select(Function(fv) CType(fv.Value, Date))
)
            End If
            ' Создаём popup
            Dim popup As New Popup With {
.Placement = PlacementMode.Bottom,
.PlacementTarget = sender,
.StaysOpen = False,
.AllowsTransparency = True,
.PopupAnimation = PopupAnimation.Slide
}
            ' Контрол дат
            Dim ctrl As New DateFilterPopup()
            ctrl.SourcePropertyName = propName
            ctrl.PopulateDates(currentSource, propName, previouslySelectedDates) ' <-- Передаём currentSource вместо ItemsSource
            popup.Child = ctrl
            ' Обработка применения
            AddHandler ctrl.FilterApplied, Sub(s As Object, e As DateFilterAppliedEventArgs)
                                               ' Собираем выбранные даты → предикат
                                               Dim selectedDates = e.SelectedDates
                                               Dim predicate As Func(Of Otkaz, Boolean) = Function(o As Otkaz) True

                                               If selectedDates?.Count > 0 Then
                                                   Select Case propName
                                                       Case "Nach" : predicate = Function(o As Otkaz) o.Nach > Date.MinValue AndAlso selectedDates.Contains(o.Nach.Date)
                                                       Case "Postup" : predicate = Function(o As Otkaz) o.Postup > Date.MinValue AndAlso selectedDates.Contains(o.Postup.Date)
                                                       Case "Zakryt" : predicate = Function(o As Otkaz) o.Zakryt > Date.MinValue AndAlso selectedDates.Contains(o.Zakryt.Date)
                                                       Case "Sozdan" : predicate = Function(o As Otkaz) o.Sozdan > Date.MinValue AndAlso selectedDates.Contains(o.Sozdan.Date)
                                                       Case "KorDate" : predicate = Function(o As Otkaz) o.KorDate > Date.MinValue AndAlso selectedDates.Contains(o.KorDate.Date)
                                                       Case "Peredan" : predicate = Function(o As Otkaz) o.Peredan > Date.MinValue AndAlso selectedDates.Contains(o.Peredan.Date)
                                                   End Select
                                               End If
                                               ' KorDate
                                               Dim filtVal As New List(Of FilterValue)
                                               For Each dt In selectedDates
                                                   filtVal.Add(New FilterValue(dt, "="))
                                               Next
                                               _filterState.SetFilterWithOperator(propName, filtVal)
                                               UpdateIndicatorForProperty(propName)
                                               RaiseEvent FiltersChanged(Me, EventArgs.Empty) ' <-- ДОБАВИТЬ ЭТУ СТРОКУ
                                               popup.IsOpen = False
                                           End Sub
            If _currentPopup IsNot Nothing Then _currentPopup.IsOpen = False
            _currentPopup = popup
            popup.IsOpen = True
        End Sub

        Private Sub ShowNumericFilterPopupNew(sender As Object, propName As String)

            Dim popup As New Popup With {
.Placement = PlacementMode.Bottom,
.PlacementTarget = sender,
.StaysOpen = False,
.AllowsTransparency = True,
.PopupAnimation = PopupAnimation.Slide
}

            Dim ctrl As New NumericFilterPopup() ' <-- Если NumericFilterPopup не зависит от source
            popup.Child = ctrl
            AddHandler ctrl.FilterApplied, Sub(s As Object, e As FilterAppliedEventArgs)
                                               Dim currentFilterValues As List(Of FilterValue) = Nothing
                                               Dim currentObjects = _filterState.GetFilter(propName)
                                               currentFilterValues = currentObjects.OfType(Of FilterValue)().ToList()
                                               currentFilterValues = currentFilterValues.Where(Function(fv) fv.Operatr <> e.Operatr).ToList()
                                               If e.Value.HasValue Then
                                                   currentFilterValues.Add(New FilterValue(e.Value.Value, e.Operatr))
                                               End If

                                               _filterState.SetFilterWithOperator(propName, currentFilterValues)
                                               UpdateIndicatorForProperty(propName) ' <-- ВАЖНО
                                               RaiseEvent FiltersChanged(Me, EventArgs.Empty) ' <-- ВАЖНО
                                               popup.IsOpen = False
                                           End Sub

            If _currentPopup IsNot Nothing Then _currentPopup.IsOpen = False
            _currentPopup = popup
            popup.IsOpen = True
        End Sub

    End Class

End Namespace

