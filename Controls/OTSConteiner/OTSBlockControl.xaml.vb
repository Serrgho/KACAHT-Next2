Imports System.ComponentModel
Imports System.Data.Common
Imports System.Windows.Controls.Primitives
Imports Windows.Gaming.Preview.GamesEnumeration

Namespace Kas
    Partial Public Class OTSBlockControl
        Private _baseContext As List(Of Otkaz) ' <-- НУЖНО: Исходный список данных. Используется для RebuildLevel0Filtered.
        Private _level0Filtered As IEnumerable(Of Otkaz) ' <-- НУЖНО: Результат Level0 фильтрации. Используется в RebuildCurrentView.
        Private _currentView As IEnumerable(Of Otkaz) ' <-- НУЖНО: Результат Level0 + Main фильтрации. Устанавливается в 
        Private _currentPopup As Popup ' <-- НУЖНО: Для отображения FilterPopup.
        Private _level0FilterState As New FilterState() ' <-- НУЖНО: Хранит состояние Level0 фильтров.
        Public Event FiltersChanged(sender As Object, e As EventArgs) ' <-- НУЖНО: Для уведомления MW об изменении Level0 фильтров.
        Public Event OpisSearchRequested As EventHandler(Of OpisSearchEventArgs)


        Private Sub SearchOpisBox_TextChanged(sender As Object, e As TextChangedEventArgs)
            RaiseEvent OpisSearchRequested(Me, New OpisSearchEventArgs(SearchOpisBox.Text.Trim()))
        End Sub

        Private Sub ClearSearchButton_Click(sender As Object, e As RoutedEventArgs)
            SearchOpisBox.Clear()
            MW.InfoBLOK.ClearItems()
        End Sub


        Public Sub UpdateAllSearchTerms(searchTerm As String)
            'For Each item In OTSContainer.Items
            '    Dim row = TryCast(item, OTSRowControl)
            '    If row IsNot Nothing Then
            '        row.SearchTerm = searchTerm
            '    End If
            'Next
        End Sub




        Public ReadOnly Property OTSCardHeader As String
            Get
                Return If(PointedOtkaz?.Id, "-----")
            End Get
        End Property


        Private Sub WarningFilters_FilterApplied(filteredItems As List(Of Otkaz))
            ' 1. Применяем отфильтрованные данные
            AddOTSToContainer(filteredItems)

            ' 2. Обновляем интерфейс (твоя существующая логика)
            UpdateTotal()

            ' Если нужно обновить ярлыки, раскомментируй:
            ' YarCon.UpdateYarlykInfo()
        End Sub



        Private Function GetNoteNameForProperty(propName As String) As String
            ' ТОЛЬКО для конкретных свойств
            Select Case propName
                Case "SerLokExact" : Return "нет_серии_локомотива"
                Case "NumLok" : Return "нет_номера_локомотива"
                    ' Добавлять ТОЛЬКО если есть соответствующий элемент в XAML
                Case Else : Return "" ' Для остальных - ничего не добавляем!
            End Select
        End Function

        Private Sub UpdateOtkazNotes(otkaz As Otkaz, notesToAdd As List(Of String))
            If notesToAdd.Count = 0 Then Return

            Dim existingNotes = If(otkaz.UpdateNotes, "")

            ' Добавляем только новые пометки
            For Each note In notesToAdd
                If Not existingNotes.Contains(note) Then
                    If String.IsNullOrEmpty(existingNotes) Then
                        existingNotes = note
                    Else
                        existingNotes &= "; " & note
                    End If
                End If
            Next

            otkaz.UpdateNotes = existingNotes
        End Sub

        Private Sub RemoveEmptyFieldNotes(otkaz As Otkaz, propsForNotes As List(Of String))
            If String.IsNullOrEmpty(otkaz.UpdateNotes) Then Return

            ' Убираем только пометки для проверяемых свойств
            For Each propName In propsForNotes
                Dim noteToRemove = GetNoteNameForProperty(propName)
                If Not String.IsNullOrEmpty(noteToRemove) AndAlso otkaz.UpdateNotes.Contains(noteToRemove) Then
                    otkaz.UpdateNotes = otkaz.UpdateNotes.Replace(noteToRemove, "").Replace(";;", ";").Trim("; ".ToCharArray())
                End If
            Next
        End Sub







        Private Sub OnLevel0FilterMouseDown(sender As Object, e As MouseButtonEventArgs)
            Dim border As Border = TryCast(sender, Border)
            If border Is Nothing Then Return

            Dim propertyName As String = CStr(border.Tag)
            ' --- Читаем FieldType и FilterLevel из XAML ---
            Dim fieldType As String = FilterProperties.GetFieldType(border)
            If String.IsNullOrEmpty(fieldType) Then
                'Debug.WriteLine($"OnLevel0FilterMouseDown: FieldType not set for property '{propertyName}', defaulting to 'String'.")
                fieldType = "String"
            End If
            ' ------------------------------------------
            ShowUniversalFilterPopup(sender, propertyName, fieldType)
        End Sub

        Private Sub ShowUniversalFilterPopup(sender As Object, propName As String, fieldType As String)
            ' Определяем, какой тип фильтра показать
            Select Case fieldType.ToLowerInvariant()
                Case "boolean", "string", "text"
                    ShowTextualFilterPopup(sender, propName, fieldType) ' Объединяем булево и текст
                Case "number", "numeric"
                    ShowNumericFilterPopupNew(sender, propName) ' Твоя старая логика для чисел
                Case "date"
                    ShowDateFilterPopupNew(sender, propName) ' Твоя старая логика для дат
                Case Else
                    'Debug.WriteLine($"ShowUniversalFilterPopup: Unknown FieldType '{fieldType}' for property '{propName}', defaulting to String.")
                    ShowTextualFilterPopup(sender, propName, "String") ' По умолчанию - текст
            End Select
        End Sub


        ' Метод, который THed может вызвать, чтобы получить текущее состояние сортировки
        Public Function GetIsSortByCountForFilters() As Boolean
            ' Возвращаем состояние переключателя через его ToggleButton
            Return Me.SortOrderToggleUC.MYToggle.IsChecked = True
        End Function


        Private Sub ShowTextualFilterPopup(sender As Object, propName As String, fieldType As String)
            Dim source = MW.TRowsContainer.OTSContainer.ItemsSource
            Dim isSorted = Me.SortOrderToggleUC.MYToggle.IsChecked = True

            ' Получаем готовый попап
            Dim result = FilterPopupHelper.PrepareTextualFilter(sender, propName, fieldType, source, _level0FilterState, isSorted)
            Dim popup = result.Item1
            Dim values = result.Item2

            ' Кнопка копирования (твоя локальная логика)
            Dim popupCtrl As FilterPopup = CType(popup.Child, FilterPopup)
            If values Is Nothing OrElse values.Count = 0 Then
                popupCtrl.CopyButton.IsEnabled = False
            Else
                popupCtrl.CopyButton.IsEnabled = True
                popupCtrl.CopyButton.ToolTip = $"Скопировать {values.Count} значений"
            End If

            ' Твой стандартный обработчик OK (без всяких Action)
            AddHandler popupCtrl.OkButt.Click, Sub(s, args)
                                                   Dim selected = OtkazFilterConfig.SafeGetSelectedItems(popupCtrl.FilterListBox)
                                                   Dim isBoolean = (fieldType.ToLowerInvariant() = "boolean")

                                                   ' Парсинг (можно тоже вынести в конфиг, но пусть будет тут для наглядности)
                                                   Dim originalValues = selected.Select(Function(item)
                                                                                            Dim str = item?.ToString()
                                                                                            If String.IsNullOrEmpty(str) Then Return str
                                                                                            If str.Contains(" [") AndAlso str.EndsWith("]") Then
                                                                                                If isBoolean Then
                                                                                                    If str.StartsWith("Да ") Then Return "True"
                                                                                                    If str.StartsWith("Нет ") Then Return "False"
                                                                                                End If
                                                                                                Return str.Substring(0, str.IndexOf(" ["))
                                                                                            ElseIf str.Contains(" (") AndAlso str.EndsWith(")") Then
                                                                                                Return str.Substring(0, str.IndexOf(" ("))
                                                                                            End If
                                                                                            Return str
                                                                                        End Function).ToList()

                                                   _level0FilterState.SetFilter(propName, originalValues)
                                                   RaiseEvent FiltersChanged(Me, EventArgs.Empty)
                                                   UpdateIndicatorForProperty(propName)
                                                   popup.IsOpen = False
                                               End Sub

            ' Управление открытием
            If _currentPopup IsNot Nothing Then _currentPopup.IsOpen = False
            _currentPopup = popup
            popup.IsOpen = True
        End Sub



        ' --- ВСПОМОГАТЕЛЬНЫЙ МЕТОД: Обработчик нажатия OK в фильтре ---
        Private Sub OnPopupOkClicked(sender As Object, e As RoutedEventArgs, popupCtrl As FilterPopup, popup As Popup, propName As String, isBooleanField As Boolean)
            ' 1. Получить выбранные элементы
            Dim selected = SafeGetSelectedItems(popupCtrl.FilterListBox)

            ' 2. Извлечь оригинальные значения из строк "Значение [N]" или "Да [N]", "Нет [N]"
            Dim originalValues = selected.Select(Function(item)
                                                     Dim str = item?.ToString()
                                                     If (str?.Contains(" (") AndAlso str.EndsWith(")")) OrElse (str?.Contains(" [") AndAlso str.EndsWith("]")) Then
                                                         If isBooleanField Then
                                                             ' Для булевых: "Да [N]" -> "True", "Нет [N]" -> "False"
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

            ' 3. Сохранить выбранные значения в состояние фильтра
            _level0FilterState.SetFilter(propName, originalValues)

            ' 4. Вызвать событие изменения фильтров
            RaiseEvent FiltersChanged(Me, EventArgs.Empty)

            ' 5. Закрыть popup
            popup.IsOpen = False

            ' 6. Обновить индикатор для свойства
            UpdateIndicatorForProperty(propName)
        End Sub


        ' --- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ---
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
        End Function

        ' Получает список строк "Да [N]", "Нет [N]" из источника для булевого свойства
        Private Function GetBooleanValuesForProperty(currentSource As IEnumerable(Of Otkaz), propName As String) As List(Of String)
            Dim getter As Func(Of Otkaz, Boolean) = OtkazFilterConfig.GetBooleanGetter(propName)
            If getter Is Nothing Then Return New List(Of String) ' Возвращаем пустой список, если геттера нет

            ' --- ГРУППИРУЕМ значения и добавляем [количество] ---
            Dim grouped = currentSource.GroupBy(Function(o) getter(o))

            ' Формируем строки "Да [N]" и "Нет [N]"
            Dim values = New List(Of String)
            For Each g In grouped
                Dim displayText As String = If(g.Key, "Да", "Нет")
                values.Add($"{displayText} [{g.Count()}]")
            Next

            ' Сортировка: "Да [N]" и "Нет [N]" в алфавитном порядке
            values.Sort()
            If values.Count > 0 Then
                values.Insert(0, "[Все]")
            Else
                values.Add("[Все]") ' Если других значений нет, просто добавим "[Все]"
            End If


            Return values
        End Function

        ' Сортирует список строк "Значение [Количество]" в зависимости от переключателя
        Private Function SortValuesByToggle(values As List(Of String), propName As String, isSortByCount As Boolean) As List(Of String)
            ' Убираем "[Все]" из сортировки, если он есть (его нужно вставить в начало позже)
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
            'Zakem_TXT
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
                        ' Ищем строку в allValues, которая начинается с "Да [N]" или "Нет [N]"
                        Dim match = allValues.FirstOrDefault(Function(v) (itemStr.Equals("True", StringComparison.OrdinalIgnoreCase) AndAlso v.StartsWith("Да [")) OrElse
        (itemStr.Equals("False", StringComparison.OrdinalIgnoreCase) AndAlso v.StartsWith("Нет ["))
    )
                        If match IsNot Nothing Then
                            SafeAddToSelection(popupCtrl.FilterListBox, match)
                        End If
                    End If
                Next
            Else ' строковое поле
                For Each item In savedFilterValues
                    ' Извлекаем оригинальное значение из "Значение [N]"
                    Dim originalItem = item?.ToString()
                    ' Ищем строку в allValues, которая начинается с этого значения
                    Dim match = allValues.FirstOrDefault(Function(v) v.StartsWith(originalItem & " ("))
                    If match IsNot Nothing Then
                        SafeAddToSelection(popupCtrl.FilterListBox, match)
                    End If
                Next
            End If
        End Sub



        Private Sub ShowDateFilterPopupNew(sender As Object, propName As String)
            Dim currentSource As IEnumerable(Of Otkaz) = TryCast(OTSContainer.ItemsSource, IEnumerable(Of Otkaz))
            If currentSource Is Nothing Then Return
            Dim previouslySelectedDates As HashSet(Of Date) = Nothing
            Dim filterValues As List(Of Object) = _level0FilterState.GetFilter(propName)
            If filterValues?.Count > 0 Then
                previouslySelectedDates = New HashSet(Of Date)(
    filterValues.OfType(Of FilterValue)().Where(Function(fv) fv.Value IsNot Nothing).Select(Function(fv) CType(fv.Value, Date))
)
            End If
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
            ctrl.PopulateDates(currentSource, propName, previouslySelectedDates) ' <-- 
            popup.Child = ctrl

            AddHandler ctrl.FilterApplied, Sub(s As Object, e As DateFilterAppliedEventArgs)
                                               ' Собираем выбранные даты → предикат
                                               Dim selectedDates = e.SelectedDates
                                               Dim predicate As Func(Of Otkaz, Boolean) = Function(o As Otkaz) True

                                               If selectedDates?.Count > 0 Then
                                                   Select Case propName
                                                       Case "Nach" : predicate = Function(o As Otkaz) o.Nach > Date.MinValue AndAlso selectedDates.Contains(o.Nach.Date)
                                                       Case "Postup" : predicate = Function(o As Otkaz) o.Postup > Date.MinValue AndAlso selectedDates.Contains(o.Postup.Date)
                                                       Case "VernulsaOTS" : predicate = Function(o As Otkaz) o.VernulsaOTS > Date.MinValue AndAlso selectedDates.Contains(o.VernulsaOTS.Date)
                                                       Case "Zakryt" : predicate = Function(o As Otkaz) o.Zakryt > Date.MinValue AndAlso selectedDates.Contains(o.Zakryt.Date)
                                                       Case "Peredan" : predicate = Function(o As Otkaz) o.Peredan > Date.MinValue AndAlso selectedDates.Contains(o.Peredan.Date)
                                                       Case "Sozdan" : predicate = Function(o As Otkaz) o.Sozdan > Date.MinValue AndAlso selectedDates.Contains(o.Sozdan.Date)
                                                       Case "KorDate" : predicate = Function(o As Otkaz) o.KorDate > Date.MinValue AndAlso selectedDates.Contains(o.KorDate.Date)
                                                   End Select
                                               End If
                                               'KorDate
                                               Dim filtVal As New List(Of FilterValue)
                                               For Each dt In selectedDates
                                                   filtVal.Add(New FilterValue(dt, "="))
                                               Next
                                               _level0FilterState.SetFilterWithOperator(propName, filtVal)
                                               UpdateIndicatorForProperty(propName)
                                               RaiseEvent FiltersChanged(Me, EventArgs.Empty) ' <-- ДОБАВИТЬ ЭТУ СТРОКУ
                                               popup.IsOpen = False
                                           End Sub

            ' VernulsaOTS
            If _currentPopup IsNot Nothing Then _currentPopup.IsOpen = False
            _currentPopup = popup
            popup.IsOpen = True
        End Sub

        '================================================================================================
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

        '================================================================================================

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
                                               Dim currentObjects = _level0FilterState.GetFilter(propName)
                                               currentFilterValues = currentObjects.OfType(Of FilterValue)().ToList()

                                               currentFilterValues = currentFilterValues.Where(Function(fv) fv.Operatr <> e.Operatr).ToList()

                                               If e.Value.HasValue Then
                                                   currentFilterValues.Add(New FilterValue(e.Value.Value, e.Operatr))
                                               End If

                                               _level0FilterState.SetFilterWithOperator(propName, currentFilterValues)
                                               UpdateIndicatorForProperty(propName) ' <-- ВАЖНО
                                               RaiseEvent FiltersChanged(Me, EventArgs.Empty) ' <-- ВАЖНО
                                               popup.IsOpen = False
                                           End Sub

            If _currentPopup IsNot Nothing Then _currentPopup.IsOpen = False
            _currentPopup = popup
            popup.IsOpen = True
        End Sub

        Private Sub UpdateIndicatorForProperty(propertyName As String)
            ' Формируем имя индикатора по соглашению: {propertyName}0LevelIndicator

            Dim indicatorName = propertyName & "0LevelIndicator"
            ' Ищем элемент по имени в визуальном дереве OTSBlockControl
            Dim indicatorElement = FindName(indicatorName)
            If indicatorElement IsNot Nothing Then
                ' Проверяем, нашли ли и является ли он Level0FilterIndicator
                Dim indicator As Level0FilterIndicator = TryCast(indicatorElement, Level0FilterIndicator)
                If indicator IsNot Nothing Then
                    ' --- ПРАВИЛЬНО: Передаём _level0FilterState ---
                    indicator.UpdateIsFiltered(_level0FilterState)
                    ' --------------------------------------------
                Else
                    '' Необязательно: логировать, если индикатор не найден или неправильного типа
                    'Debug.WriteLine($"Level0FilterIndicator для {indicatorName} не найден или неправильного типа.")
                End If
            Else
                '' Необязательно: логировать, если индикатор не найден
                'Debug.WriteLine($"Level0FilterIndicator для {indicatorName} не найден в визуальном дереве.")
            End If
        End Sub

        Public Function BuildLevel0CombinedPredicate() As Func(Of Otkaz, Boolean)
            Return _level0FilterState.BuildPredicate() ' FilterState уже умеет строить предикат
        End Function


        Private Sub RebuildLevel0Filtered()
            ' 1. Получаем базовый список с учётом ярлыка
            Dim baseList As List(Of Otkaz)
            If PointedYarlyk?.Zapros IsNot Nothing Then
                baseList = OTSList?.Where(PointedYarlyk.Zapros).ToList()
            Else
                baseList = OTSList
            End If

            ' 2. Применяем Level0-фильтры
            If _level0FilterState.HasActiveFilters() Then
                Dim pred = _level0FilterState.BuildPredicate()
                baseList = baseList.Where(pred).ToList()
            End If

            ' 3. Сохраняем результат
            _level0Filtered = baseList
            _baseContext = baseList
            THed.ItemsSource = _level0Filtered

        End Sub

        Public Property ItemsSource As IEnumerable(Of Otkaz)
            Get
                Return _baseContext
            End Get
            Set
                _baseContext = If(Value, Enumerable.Empty(Of Otkaz)()).ToList()
                ' Устанавливаем начальное состояние для фильтрации 0-уровня
                _level0Filtered = _baseContext ' <-- Нужно, чтобы _level0Filtered указывал на начальные данные
                '     и мог быть изменён позже RebuildLevel0Filtered.

                ' Устанавливаем начальное состояние для текущего отображения
                _currentView = _baseContext    ' <-- Нужно, чтобы _currentView указывал на начальные данные
                '     и мог быть изменён позже RebuildCurrentView/OnFilterRequested.

                OTSContainer.ItemsSource = _currentView ' <-- Не используется напрямую, см. ниже
                THed.ItemsSource = _level0Filtered ' <-- Вот эта строка важна, и теперь она использует _level0Filtered,
                '     что делает её корректной в будущем, после применения 0-фильтров.

                UpdateTotal()
                ScrollToPointedOTS()
            End Set
        End Property


        Public ReadOnly Property Items As ItemCollection
            Get
                Return OTSContainer.Items
            End Get

        End Property

        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().

            AddHandler THed.ResetRequested, AddressOf OnResetFilter
            AddHandler THed.FiltersChanged, AddressOf OnTHedFiltersChanged ' <-- Добавляем
            InvisibleBottomButtons()
        End Sub


        Private Sub OnTHedFiltersChanged(sender As Object, e As EventArgs)
            RebuildCurrentView() ' <-- Перестраиваем всё, учитывая все фильтры
            UpdateTotal()
            ScrollToPointedOTS()
        End Sub

        ''' <summary>
        ''' Применяет фильтр к текущему отображаемому набору (сужающая фильтрация).
        ''' </summary>
        Public Sub ApplyFilter(predicate As Func(Of Otkaz, Boolean))

            ' РАБОТАЕМ С _baseContext, А НЕ С ТЕКУЩИМ ItemsSource
            ' Это гарантирует, что фильтр применяется к полному набору данных
            If _baseContext Is Nothing Then Return ' Проверяем исходный источник
            Dim filtered As IEnumerable(Of Otkaz)
            If predicate Is Nothing Then
                ' Если предикат пустой, фильтрация не нужна - возвращаем исходный источник
                filtered = _baseContext
            Else
                ' Применяем фильтр к исходному источнику
                filtered = _baseContext.Where(predicate)
            End If

            ' ВАЖНО: сначала обнуляем, чтобы сбросить вью-генерацию
            OTSContainer.ItemsSource = Nothing
            ' ПРИСВАИВАЕМ НОВЫЙ ОТФИЛЬТРОВАННЫЙ СПИСОК
            OTSContainer.ItemsSource = filtered
            THed.ItemsSource = filtered  ' ← для корректной работы Popup в заголовке (по вашему комментарию)
            UpdateTotal()
            ScrollToPointedOTS()
        End Sub


        Private Sub OnResetFilter(sender As Object, e As EventArgs)
            ResetAllFilters()
        End Sub


        Public Sub ResetAllFilters()
            ' Сбрасываем фильтры в THed (Main уровень)
            THed.ClearAllFilters()  ' ← Это правильно - сброс Main
            ' Сбрасываем Level0-фильтры
            ClearAll0LevelFilters()

            ' 🔥 Сбрасываем состояние фильтра NumOTSUserTB
            MW._isLokFilter = False  ' Важно сбросить флаг!

            MW._filterNumbers?.Clear()

            RebuildCurrentView()
            UpdateTotal()
            ScrollToPointedOTS()
        End Sub


        Private Sub RebuildCurrentView()

            ' 1. Базовый список = результат Level0-фильтров (уже включает ярлык)
            Dim source = _level0Filtered?.ToList()

            ' 2. Применяем фильтры из заголовка (THed)
            Dim mainPred = THed.BuildCombinedPredicate()
            If mainPred IsNot Nothing Then
                source = source?.Where(mainPred).ToList()
            End If

            ' 3. Применяем поиск по Opis
            If Not String.IsNullOrEmpty(MW.CurrentSearchTerm) Then
                source = source.Where(Function(o) o.ContainsPhrase(MW.CurrentSearchTerm)).ToList()
            End If

            ' 🔥 Применяем фильтр NumOTSUserTB (если есть)
            If MW._filterNumbers IsNot Nothing AndAlso MW._filterNumbers.Any() Then
                If MW._isLokFilter Then
                    source = source.Where(Function(o) o IsNot Nothing AndAlso Not String.IsNullOrEmpty(o.NumLok) AndAlso MW._filterNumbers.Any(Function(f) o.NumLok.Contains(f))).ToList()
                Else
                    source = source.Where(Function(o) o IsNot Nothing AndAlso MW._filterNumbers.Contains(o.Id)).ToList()
                End If
            End If

            ' 4. Сортируем
            source = source.OrderBy(Function(o) o.Nach).ToList()

            ' 5. Устанавливаем
            OTSContainer.ItemsSource = source
            THed.ItemsSource = source

            ' 6. Обновляем Total
            UpdateTotal()

        End Sub


        Public Sub UpdateTotal()

            With My.Settings
                If .NachPeriod.Date = Date.MinValue.Date OrElse .KonPeriod.Date = Date.MinValue.Date Then
                    PeriodOTS.Text = $"Текущий период не задан"
                Else
                    PeriodOTS.Text = $"Текущий период с { .NachPeriod:dd.MM.yyyy HH:mm} по { .KonPeriod:dd.MM.yyyy HH:mm}"
                End If
                If .nachOLDPeriod.Date = Date.MinValue.Date OrElse .konOLDPeriod.Date = Date.MinValue.Date Then
                    PeriodOLDOTS.Text = $"Прошлый период не задан"
                Else
                    PeriodOLDOTS.Text = $"Прошлый период с { .nachOLDPeriod:dd.MM.yyyy HH:mm} по { .konOLDPeriod:dd.MM.yyyy HH:mm}"
                End If
            End With
            SumKorr.Text = ""
            FirstOTS.Text = $"Отказы не найдены"
            LastOTS.Text = $"Отказы не найдены"
            TotalOTS.Text = "Отказы не найдены"

            InvisibleBottomButtons()


            Dim TotCnt As Integer = DirectCast(OTSContainer.ItemsSource, IEnumerable(Of Otkaz)).Where(Function(o) True).Count
            Dim TotPCh As Single = DirectCast(OTSContainer.ItemsSource, IEnumerable(Of Otkaz)).Sum(Function(o) o.PCh)
            If TotCnt > 0 Then
                Dim Fots, Lots As Date
                Fots = DirectCast(OTSContainer.ItemsSource, IEnumerable(Of Otkaz)).First.Nach
                Lots = DirectCast(OTSContainer.ItemsSource, IEnumerable(Of Otkaz)).Last.Nach
                FirstOTS.Text = $"Первый отказ от {Fots:dd.MM.yyyy HH:mm} "
                LastOTS.Text = $"Последний отказ от {Lots:dd.MM.yyyy HH:mm} "
                TotalOTS.Text = $"Найдено отказов - {TotCnt} на {TotPCh:F2} ч."
                OTSNumbersBU.Visibility = Visibility.Visible
                SumKorr.Text = GetSumKor_TXT(ForWord:=False)

                UpdateBtnToExcelReportButtonVisibility()
                WarningFilters.CheckForEmptyFields()
                UpdateShowDemarkButtonVisibility()
                UpdateShowMarkAllButtonVisibility()
                UpdateRemoveVoprosButtonVisibility()
                UpdateShowFiltersButtonVisibility()
                ChkOK()
                UpdateActionNotesVisibility()
            End If


        End Sub

        Public Sub InvisibleBottomButtons()
            ActiveFiltersButton.Visibility = Visibility.Hidden
            OTSNumbersBU.Visibility = Visibility.Hidden
            RemoveVoprosBU.Visibility = Visibility.Hidden
            MarkAllBU.Visibility = Visibility.Hidden
            DemarkBU.Visibility = Visibility.Hidden
            DelOKBU.Visibility = Visibility.Hidden
            DelAllActionBUBU.Visibility = Visibility.Hidden
            BtnToExcelReport.Visibility = Visibility.Hidden
        End Sub

        Private Sub UpdateActionNotesVisibility()
            ' Аня (Any) проверяет, есть ли хоть одна плашка в списке
            ' Берем список из ItemsSource, чтобы учитывать текущие фильтры в контейнере
            Dim source = TryCast(OTSContainer.ItemsSource, IEnumerable(Of Otkaz))

            If source IsNot Nothing Then
                ' Если Аня нашла хоть одну строку, где UpdateNotes не пустая
                Dim hasNotes As Boolean = source.Any(Function(o) Not String.IsNullOrEmpty(o.UpdateNotes))

                ' Показываем или прячем кнопку (DelAllActionBUBU - это имя твоей большой кнопки)
                DelAllActionBUBU.Visibility = If(hasNotes, Visibility.Visible, Visibility.Collapsed)
            End If
        End Sub


        Sub ChkOK()
            If OTSList.Any(Function(u) u.UpdateNotesList.Contains("OK")) Then
                DelOKBU.Visibility = Visibility.Visible
            Else
                DelOKBU.Visibility = Visibility.Collapsed
            End If
        End Sub


        ' Метод для обновления видимости кнопки "Показать активные фильтры"
        Private Sub UpdateShowFiltersButtonVisibility()
            Dim hasLevel0Filters = _level0FilterState.HasActiveFilters() ' <-- Убедись, что FilterState.HasActiveFilters() реализован
            Dim hasTHedFilters = THed.HasActiveFilters() ' <-- Убедись, что TableHeader.HasActiveFilters() реализован
            ActiveFiltersButton.Visibility = If(hasLevel0Filters OrElse hasTHedFilters, Visibility.Visible, Visibility.Collapsed)
        End Sub


        Public Sub UpdateShowDemarkButtonVisibility()
            Dim hasMarked = OTSContainer.Items.OfType(Of Otkaz).Any(Function(x) x.Marked)
            DemarkBU.Visibility = If(hasMarked, Visibility.Visible, Visibility.Collapsed)
        End Sub




        Public Sub ScrollToPointedOTS()
            If PointedOtkaz IsNot Nothing AndAlso OTSContainer.ItemsSource IsNot Nothing Then
                ' Проверяем, содержится ли PointedOtkaz в текущем ItemsSource
                Dim items = TryCast(OTSContainer.ItemsSource, IEnumerable)
                If items IsNot Nothing AndAlso items.Cast(Of Object)().Contains(PointedOtkaz) Then
                    OTSContainer.ScrollIntoView(PointedOtkaz)
                    Exit Sub
                End If
            End If

            ' Если нет — прокручиваем к последнему элементу
            ScrollToEnd()
        End Sub

        Public Sub ScrollToEnd()

            Dim items = TryCast(OTSContainer.ItemsSource, IEnumerable)
            If items Is Nothing Then Return

            ' Пробуем быстро — если IList (обычно бывает до фильтров)
            Dim asList = TryCast(items, IList)
            If asList IsNot Nothing AndAlso asList.Count > 0 Then
                OTSContainer.ScrollIntoView(asList(asList.Count - 1))
                Return
            End If

            ' Если не IList (например, после .Where) — перебираем
            Dim lastItem As Object = Nothing
            For Each item In items
                lastItem = item
            Next

            If lastItem IsNot Nothing Then
                OTSContainer.ScrollIntoView(lastItem)
            End If

        End Sub


        Private Const INNER_SCROLLVIEWER_NAME As String = "InnerScrollViewer" ' <-- Константа для имени
        Private Sub OTSContainer_PreviewMouseWheel(sender As Object, e As MouseWheelEventArgs)
            ' Настройка шага прокрутки из Settings
            Const scrollStep As Double = 1.1 ' <-- Предположим, у тебя есть Settings.ScrollStep As Double = 1.0
            ' Если Settings нет, можно использовать локальную переменную или константу: Const scrollStep As Double = 1.0
            Dim innerScroll = TryCast(OTSContainer.Template.FindName(INNER_SCROLLVIEWER_NAME, OTSContainer), ScrollViewer)

            If innerScroll IsNot Nothing Then
                ' Проверяем, чтобы e.Delta не был 0 (хотя обычно не бывает)
                If e.Delta <> 0 Then
                    ' Прокручиваем ScrollViewer
                    ' e.Delta обычно +/-120 за одно "щёлчок" колёсика
                    ' (e.Delta / 120) даёт количество щелчков
                    ' Умножаем на scrollStep, чтобы получить желаемый сдвиг
                    Dim offsetChange As Double = (e.Delta / 120.0) * scrollStep ' <-- 120.0 для точности Double
                    innerScroll.ScrollToVerticalOffset(innerScroll.VerticalOffset - offsetChange) ' <-- Отрицательный offset для прокрутки вниз при положительном Delta
                End If
            End If
            ' Отключаем дальнейшую обработку события (важно, чтобы не прокручивался родительский элемент)
            e.Handled = True

        End Sub

        Private Sub Button_Click_5(sender As Object, e As RoutedEventArgs)
            CopyOTSNums()
        End Sub

        Sub CopyOTSNums()
            Dim items = TryCast(OTSContainer.ItemsSource, IEnumerable(Of Otkaz))
            ' Проверяем, что ItemsSource не пустой
            If items.Count > 0 Then
                ' Преобразуем ItemsSource в список объектов Otkaz
                If items IsNot Nothing Then
                    ' Собираем все Id через запятую
                    Dim ids = String.Join(",", items.Select(Function(otkaz) otkaz.Id.ToString()))
                    ' Устанавливаем текст в TextBlock
                    IdListTextBlock.Text = ids
                    ' Открываем Popup
                    IdListPopup.IsOpen = True
                    InformLB.AddItem($"Номера скопированы в буфер обмена") '{vbCrLf}{ids}")
                    InformLB.AddItem(InformLB.EndOfMSG)
                    InformLB.ScrollToEnd()
                    Clipboard.SetText(ids)
                Else
                    ShowMSG(MW, "Список отказов не содержит объектов нужного типа")
                End If
            Else
                ShowMSG(MW, "Список отказов пустой")
            End If
        End Sub




        Private Sub ClearFilters_MouseDown(sender As Object, e As MouseButtonEventArgs)
            ClearAll0LevelFilters()
            ' Перестраиваем текущий вид с учётом активных THed-фильтров
            RebuildCurrentView()
            UpdateTotal()
            ScrollToPointedOTS()
        End Sub




        Public Sub ClearAll0LevelFilters()
            ' Сбрасываем Level0-фильтры
            _level0FilterState.Clear()
            ' Перестраиваем Level0-отфильтрованный список
            RebuildLevel0Filtered()
            ' Перестраиваем текущий вид (основной список)
            RebuildCurrentView()



            ' 🔥 Обновляем источник для THed
            THed.ItemsSource = _level0Filtered
            ' Обновляем индикаторы, передавая сброшенное состояние
            PripLok0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
            SerLokExact0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
            SerLok0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
            SerLokNumLokTXT0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
            PlanListText0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
            VidT0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
            NumLok0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
            PripMash0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
            MestoOTS_Dor0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
            KrasREG0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
            IsStation0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
            MestoOTS0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
            MyKlasLev10LevelIndicator?.UpdateIsFiltered(_level0FilterState)
            MyKlasLev20LevelIndicator?.UpdateIsFiltered(_level0FilterState)
            MyKlasLev30LevelIndicator?.UpdateIsFiltered(_level0FilterState)
            Zakryt0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
            Sozdan0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
            KorDate0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
            Peredan0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
            KomplexAsInt0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
            PCh0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
            Dlit0LevelIndicator?.UpdateIsFiltered(_level0FilterState)

            ' Добавь другие индикаторы
            ' ------------------------------- KorDate

            ' 🔥 Уведомляем внешний мир об изменении фильтров
            RaiseEvent FiltersChanged(Me, EventArgs.Empty)
        End Sub

        Private Sub OnLevel0FilterMouseRightDown(sender As Object, e As MouseButtonEventArgs)
            ' Получаем имя свойства из Tag
            Dim propName As String = TryCast(sender, FrameworkElement)?.Tag?.ToString()
            If String.IsNullOrEmpty(propName) Then Return
            ' Убираем фильтр для этого propName в _level0FilterState
            _level0FilterState.ClearFilterForProperty(propName) ' <-- Используем тот же метод, что и для THed
            ' Обновляем синюю точку для этого свойства
            ' Нужно найти соответствующий Level0FilterIndicator и вызвать у него UpdateIsFiltered
            ' Лучше всего использовать соглашение по именованию: {propName}0LevelIndicator
            ' И вызвать его напрямую, передав _level0FilterState
            Select Case propName
                Case "PripLok"
                    PripLok0LevelIndicator?.UpdateIsFiltered(_level0FilterState) ' <-- Передаём _level0FilterState
                Case "SerLokExact"
                    SerLokExact0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
                Case "SerLok"
                    SerLok0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
                Case "VidT"
                    VidT0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
                Case "NumLok"
                    NumLok0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
                Case "MestoOTS_Dor"
                    MestoOTS_Dor0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
                Case "KrasREG"
                    KrasREG0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
                Case "MestoOTS"
                    MestoOTS0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
                Case "PripMash"
                    PripMash0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
                Case "IsStation"
                    IsStation0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
                Case "MyKlasLev1"
                    MyKlasLev10LevelIndicator?.UpdateIsFiltered(_level0FilterState)
                Case "MyKlasLev2"
                    MyKlasLev20LevelIndicator?.UpdateIsFiltered(_level0FilterState)
                Case "MyKlasLev3"
                    MyKlasLev30LevelIndicator?.UpdateIsFiltered(_level0FilterState)
                Case "Zakryt"
                    Zakryt0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
                Case "Peredan"
                    Peredan0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
                Case "Sozdan"
                    Sozdan0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
                Case "KorDate"
                    KorDate0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
                Case "KomplexAsInt"
                    KomplexAsInt0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
                Case "SerLokNumLokTXT"
                    SerLokNumLokTXT0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
                Case "PlanListText"
                    PlanListText0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
                Case "PCh"
                    PCh0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
                Case "Dlit"
                    Dlit0LevelIndicator?.UpdateIsFiltered(_level0FilterState)
                    ' Добавь другие случаи KorDate
            End Select
            e.Handled = True
            ' Перестраиваем уровень 0 и текущий вид
            RebuildLevel0Filtered() ' <-- Обновит _level0Filtered
            RebuildCurrentView()    ' <-- Обновит _currentView и OTSContainer.ItemsSource, THed.ItemsSource
            ' Обновляем Total и прокрутку
            UpdateTotal()
            ScrollToPointedOTS()
        End Sub

        Private Function GetActiveFiltersDescription() As String
            Dim description As New System.Text.StringBuilder()
            ' --- 0-уровень ---
            description.AppendLine("Активные фильтры 0 уровня:")
            ' Вызываем вспомогательный метод для _level0FilterState
            AppendFiltersToDescription(description, _level0FilterState, isHedLevel:=False)
            ' Проверяем, были ли добавлены какие-либо фильтры 0-уровня
            If description.ToString().EndsWith("Активные фильтры 0 уровня:" & vbCrLf) Then
                description.AppendLine("  Нет активных фильтров.")
            End If
            ' --- Конец 0-уровня ---

            ' --- 1 уровень (THed) ---
            description.AppendLine() ' Новая строка перед THed
            description.AppendLine("Активные фильтры 1 уровня:")
            ' Вызываем вспомогательный метод для THed._filterState
            AppendFiltersToDescription(description, THed._filterState, isHedLevel:=True)
            ' Проверяем, были ли добавлены какие-либо фильтры THed
            Dim currentDesc = description.ToString()
            ' Проверяем, начинается ли последняя секция с "Активные фильтры 1 уровня:" и не содержит других строк после
            If Not currentDesc.Substring(currentDesc.LastIndexOf("Активные фильтры 1 уровня:" & vbCrLf)).Trim().Length > "Активные фильтры 1 уровня:".Length Then
                description.AppendLine("  Нет активных фильтров.")
            End If
            ' --- Конец THed-уровня ---

            Return description.ToString()
        End Function

        ' Вспомогательный метод для добавления фильтров из FilterState в StringBuilder
        ' isHedLevel: True - обрабатываем как THed-уровень (учитываем FilterValue), False - как 0-уровень (только строки)
        Private Sub AppendFiltersToDescription(description As System.Text.StringBuilder, filterState As FilterState, isHedLevel As Boolean)

            For Each kvp In filterState.GetAllFilters()
                Dim propName As String = kvp.Key
                Dim displayName As String = FilterDisplayNames.GetDisplayName(propName)
                Dim values As List(Of Object) = kvp.Value

                If isHedLevel Then
                    ' Для THed: проверяем и строки, и FilterValue
                    Dim activeStringValues = values.Where(Function(v) TypeOf v Is String AndAlso v.ToString() <> "[Все]").ToList()
                    Dim activeFilterValues = values.OfType(Of FilterValue)().Where(Function(fv) fv.Value IsNot Nothing).ToList()

                    If activeStringValues.Count > 0 Then
                        description.Append($"  - {displayName}: ")
                        Dim valueList = activeStringValues.Select(Function(v) v.ToString()).ToList()
                        description.AppendLine(String.Join(", ", valueList))
                    End If

                    If activeFilterValues.Count > 0 Then
                        If {"Nach", "Postup", "Zakryt", "Postup", "Peredan", "Sozdan", "KorDate", "VernulsaOTS"}.Contains(propName) Then
                            description.Append($"  - {displayName}: ")
                            ' --- ВЫВОДИМ ТОЛЬКО ДАТУ В ФОРМАТЕ dd.MM.yyyy, БЕЗ "=" ---
                            Dim valueList = activeFilterValues.Select(Function(fv) CType(fv.Value, Date).Date.ToString("dd.MM.yy")).ToList()
                            description.AppendLine(String.Join(", ", valueList))
                        Else
                            ' Для других свойств (не дат) просто выводим как обычно
                            description.Append($"  - {displayName}: ")
                            Dim valueList = activeFilterValues.Select(Function(fv) $"{fv.Operatr} {fv.Value}").ToList()
                            description.AppendLine(String.Join(", ", valueList))
                        End If
                    End If
                Else
                    ' KorDate Для 0-уровня: проверяем только строки

                    ' Для 0-уровня: проверяем СТРОКИ (например, "[Все]" для текстовых/булевых)
                    ' и ПРОВЕРЯЕМ FilterValue (для дат/чисел)
                    Dim activeStringValues = values.Where(Function(v) TypeOf v Is String AndAlso v.ToString() <> "[Все]").ToList()
                    Dim activeFilterValues = values.OfType(Of FilterValue)().Where(Function(fv) fv.Value IsNot Nothing).ToList()

                    ' Вывод для активных строк (например, текстовые или булевые фильтры 0-уровня)
                    If activeStringValues.Count > 0 Then
                        description.Append($"  - {displayName}: ")
                        Dim valueList = activeStringValues.Select(Function(v) v.ToString()).ToList()
                        description.AppendLine(String.Join(", ", valueList))
                    End If

                    ' Вывод для активных FilterValue (например, датовые фильтры 0-уровня)
                    If activeFilterValues.Count > 0 Then
                        description.Append($"  - {displayName}: ")
                        ' Особая логика вывода для дат
                        If {"Nach", "Postup", "Zakryt", "Peredan", "Sozdan", "KorDate", "VernulsaOTS"}.Contains(propName) Then
                            Dim valueList = activeFilterValues.Select(Function(fv) CType(fv.Value, Date).Date.ToString("dd.MM.yy")).ToList()
                            description.AppendLine(String.Join(", ", valueList))
                        Else
                            ' Вывод для других типов FilterValue (чисел и т.п.)
                            Dim valueList = activeFilterValues.Select(Function(fv) $"{fv.Operatr} {fv.Value}").ToList()
                            description.AppendLine(String.Join(", ", valueList))
                        End If
                    End If
                End If
            Next
        End Sub
        ' KorDate
        Private Sub ShowActiveFiltersButton_Click(sender As Object, e As RoutedEventArgs) ' <-- Пример имени
            ' Получаем описание
            Dim filterDescription = GetActiveFiltersDescription() ' <-- Используем твой метод
            ' Заполняем Popup
            ActiveFiltersContentControl.PopulateFilters(filterDescription)
            ' Открываем Popup
            ActiveFiltersPopup.IsOpen = True
            '' Получаем описание
            'MW.InfoBLOK.ClearItems()
            'MW.InfoBLOK.AddItem("****************")
            'MW.InfoBLOK.AddItem(filterDescription) ' <-- Вызов метода, который добавляет строку в лог
            'MW.InfoBLOK.AddItem("****************") ' <-- Если нужно добавить разделитель
            'MW.InfoBLOK.ScrollToEnd() ' <-- Прокрутка в конец
        End Sub


        Private Sub PerDOTS_PreviewMouseDown(sender As Object, e As MouseButtonEventArgs)
            Dim Msg As String = ""
            With My.Settings
                If .nachOLDPeriod <= Date.MinValue Then
                    Msg += $"⚠️ ОШИБКА. Не задана начальная дата предыдущего периода {vbCrLf}"
                End If
                If .konOLDPeriod <= Date.MinValue Then
                    Msg += $"⚠️ ОШИБКА. Не задана конечная дата предыдущего периода {vbCrLf}"
                End If
                If OTSList.Count <= 0 Then
                    Msg += $"⚠️ ОШИБКА. Не загружены данные по отказам {vbCrLf}"
                End If


                If Msg <> "" Then
                    MW.InfoBLOK.AddItem(Msg)
                    MW.InfoBLOK.ScrollToEnd()
                    Exit Sub
                End If

                'если нет ошибок, то...
                Swap(.nachOLDPeriod, .NachPeriod)
                Swap(.konOLDPeriod, .KonPeriod)
            End With

            InitFirst()
            'YarCon.UpdateYarlykInfo()
            UpdateTotal()
        End Sub

        Private Sub RemoveVoprosBU_Click(sender As Object, e As RoutedEventArgs)
            ' Удаляем "???" у ВСЕХ отказов в списке
            RemoveAllVoprosMarks((OTSContainer.Items.OfType(Of Otkaz)))
            UpdateRemoveVoprosButtonVisibility()
        End Sub

        ' Удаляет "???" из UpdateNotes у всех ОТФИЛЬТРОВАННЫХ отказов
        Public Sub RemoveAllVoprosMarks(otkazy As IEnumerable(Of Otkaz))
            For Each o In otkazy
                o.RemoveItemUpdateNote("???")
            Next
        End Sub

        Private Sub UpdateRemoveVoprosButtonVisibility()
            Dim hasVopros = OTSList.Any(Function(o) Not String.IsNullOrEmpty(o.UpdateNotes) AndAlso o.UpdateNotes.Contains("???"))
            RemoveVoprosBU.Visibility = If(hasVopros, Visibility.Visible, Visibility.Collapsed)
        End Sub

        Private Sub DemarkBU_Click(sender As Object, e As RoutedEventArgs)
            RemoveMarks(OTSContainer.Items.OfType(Of Otkaz))
        End Sub

        Sub RemoveMarks(OTS As IEnumerable(Of Otkaz))
            OTS.Where(Function(x) x.Marked).ToList().ForEach(Sub(x) x.Marked = False)
            UpdateShowDemarkButtonVisibility()
            UpdateShowMarkAllButtonVisibility()
        End Sub

        'Private Sub TableVisToggleUC_Click(sender As Object, e As RoutedEventArgs)
        '    'Dim ctrl = TryCast(sender, Kas.MYToggleControl)
        '    'If ctrl IsNot Nothing Then

        '    '    MW.TabSummary.Visibility = If(ctrl.IsChecked, Visibility.Visible, Visibility.Collapsed)
        '    'End If
        'End Sub

        'Private Sub NumSearchToggleUC_Click(sender As Object, e As RoutedEventArgs)
        '    'пока оставим для обработки видимости строки поиска по №№ ОТС
        'End Sub

        Private Sub NumOTSUserTB_FilterRequested(sender As Object, e As EventArgs)
            FindIDOTS()
        End Sub

        Private Sub FindOTS_LokRequested(sender As Object, e As EventArgs)
            FindIDOTS(True)
        End Sub

        Sub FindIDOTS(Optional IsLokNeed As Boolean = False)
            ' 1. Получаем данные — безопасно
            Dim source = OTSList
            If source Is Nothing Then Return

            Dim items As IEnumerable(Of Otkaz)
            Try
                items = CType(source, IEnumerable(Of Otkaz))
            Catch
                items = source.Cast(Of Otkaz)()
            End Try

            ' 2. Получаем номера из контрола и сразу убираем Nothing/пустые
            Dim filterNumbers = FindOTS.GetItems() _
                               .Where(Function(n) Not String.IsNullOrEmpty(n)) _
                               .ToList()
            If Not filterNumbers.Any() Then Return



            ' 2. Сохраняем состояние фильтра для ChangeFilters
            MW._filterNumbers = filterNumbers
            MW._isLokFilter = IsLokNeed

            ' 3. 🔥 ВАЖНО: Обновляем текстбокс (найденные/не найденные номера)
            If filterNumbers.Any() Then
                UpdateFoundStatus(filterNumbers, IsLokNeed)
            End If

            ' 4. Вызываем централизованную фильтрацию
            ChangeFilters()
        End Sub



        Private Sub UpdateFoundStatus(filterNumbers As List(Of String), IsLokNeed As Boolean)
            Dim source = OTSList
            If source Is Nothing Then Return

            Dim items As IEnumerable(Of Otkaz)
            Try
                items = CType(source, IEnumerable(Of Otkaz))
            Catch
                items = source.Cast(Of Otkaz)()
            End Try

            ' Находим поисковые строки, которые были найдены в списке отказов
            Dim foundNumbers As HashSet(Of String)
            If IsLokNeed Then
                foundNumbers = items _
                .Where(Function(o) o IsNot Nothing) _
                .SelectMany(Function(o) filterNumbers.Where(Function(f) Not String.IsNullOrEmpty(o.NumLok) AndAlso o.NumLok.Contains(f))) _
                .Distinct() _
                .ToHashSet()
            Else
                foundNumbers = items _
                .Where(Function(o) o IsNot Nothing) _
                .Where(Function(o) filterNumbers.Contains(o.Id)) _
                .Select(Function(o) o.Id) _
                .Distinct() _
                .ToHashSet()
            End If

            ' Оставляем только те номера из поиска, которых НЕТ в найденных
            Dim notFound = filterNumbers.Where(Function(n) Not foundNumbers.Contains(n)).ToList()

            ' Обновляем текстбокс (только если есть изменения)
            If notFound.Count <> filterNumbers.Count Then
                FindOTS.SetItems(notFound)
            End If
        End Sub






        Private Sub NumOTSUserTB_Cleared(sender As Object, e As EventArgs)

        End Sub

        Private Sub DelOKBU_Click(sender As Object, e As RoutedEventArgs)
            OTSList.ForEach(Sub(o) o.RemoveItemUpdateNote("OK"))
            UpdateTotal()
        End Sub

        Private Sub DelAllActionBUBU_Click(sender As Object, e As RoutedEventArgs)
            OTSList.ForEach(Sub(o) o.UpdateNotes = "")
            UpdateTotal()
        End Sub


        Private Sub MarkAllBU_Click(sender As Object, e As RoutedEventArgs)
            MarkAll(OTSContainer.Items.OfType(Of Otkaz))
        End Sub

        Sub MarkAll(OTS As IEnumerable(Of Otkaz))
            OTS.Where(Function(x) Not x.Marked).ToList().ForEach(Sub(x) x.Marked = True)
            UpdateShowMarkAllButtonVisibility()
        End Sub

        Public Sub UpdateShowMarkAllButtonVisibility()
            Dim hasMarked = OTSContainer.Items.OfType(Of Otkaz).Any(Function(x) Not x.Marked)
            MarkAllBU.Visibility = If(hasMarked, Visibility.Visible, Visibility.Collapsed)
        End Sub

        Private Sub ToUpButton_Click(sender As Object, e As RoutedEventArgs)
            Dim scrollViewer As ScrollViewer = TryCast(OTSContainer.Template?.FindName("InnerScrollViewer", OTSContainer), ScrollViewer)

            If scrollViewer IsNot Nothing Then
                scrollViewer.ScrollToTop()
            End If
        End Sub

        Private Sub ToDownButton_Click(sender As Object, e As RoutedEventArgs)
            Dim scrollViewer As ScrollViewer = TryCast(OTSContainer.Template?.FindName("InnerScrollViewer", OTSContainer), ScrollViewer)

            If scrollViewer IsNot Nothing Then
                scrollViewer.ScrollToEnd()
            End If
        End Sub

        Private Sub BtnToExcelReport_Click(sender As Object, e As RoutedEventArgs)
            ExportOTSListToExcel(OTSContainer.ItemsSource)
        End Sub

        Public Sub UpdateBtnToExcelReportButtonVisibility()
            Dim ToExcell = OTSContainer.Items.OfType(Of Otkaz).Any
            BtnToExcelReport.Visibility = If(ToExcell, Visibility.Visible, Visibility.Collapsed)
        End Sub


    End Class



    Public Class OpisSearchEventArgs
        Inherits EventArgs
        Public Property SearchTerm As String
        Public Sub New(term As String)
            SearchTerm = term
        End Sub
    End Class





End Namespace

