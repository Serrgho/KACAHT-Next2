Namespace Kas
    Module OtkazFilterConfig
        Public Const UNASSIGNED_MARKER As String = "[не отнесен]"

        ' Существующий код...
        Private ReadOnly BooleanProperties As HashSet(Of String) = New HashSet(Of String) From {
            "IsStation", "ISDanger", "ISKorp", "ISSobyt", "NarushSroka", "OkaPom", "Marked"
        }

        Public Function IsBooleanProperty(propName As String) As Boolean
            Return BooleanProperties.Contains(propName)
        End Function

        Public Function GetBooleanGetter(propName As String) As Func(Of Otkaz, Boolean)
            Select Case propName
                Case "IsStation" : Return Function(o As Otkaz) o.IsStation
                Case "ISDanger" : Return Function(o As Otkaz) o.ISDanger
                Case "ISKorp" : Return Function(o As Otkaz) o.ISKorp
                Case "ISSobyt" : Return Function(o As Otkaz) o.ISSobyt
                Case "NarushSroka" : Return Function(o As Otkaz) o.NarushSroka
                Case "OkaPom" : Return Function(o As Otkaz) o.OkaPom
                Case "Marked" : Return Function(o As Otkaz) o.Marked
                Case Else : Return Nothing
            End Select
        End Function


        ''' <summary>
        ''' Возвращает селектор для отображения значения свойства (безопасно для Nothing/регистра)
        ''' </summary>
        Public Function GetDisplaySelector(propName As String) As Func(Of Otkaz, String)
            If String.IsNullOrEmpty(propName) Then Return Nothing
            Dim p = propName
            Select Case p
                Case "Kat" : Return Function(o) o.Kat
                Case "Nach" : Return Function(o) o.Nach '???
                Case "Zakryt" : Return Function(o) o.Zakryt
                Case "Peredan" : Return Function(o) o.Peredan
                Case "Postup" : Return Function(o) o.Postup
                Case "VernulsaOTS" : Return Function(o) o.VernulsaOTS
                Case "UpdateNotes" : Return Function(o) o.UpdateNotes
                Case "Sozdan" : Return Function(o) o.Sozdan
                Case "KorDate" : Return Function(o) o.KorDate
                Case "PlanListText" : Return Function(o) o.PlanListText
                Case "PCh" : Return Function(o) o.PCh
                Case "Dlit" : Return Function(o) o.Dlit
                Case "PripMash" : Return Function(o) o.PripMash
                Case "KomplexAsInt" : Return Function(o) o.KomplexAsInt
                Case "ZaKem" : Return Function(o) If(String.IsNullOrEmpty(o.ZaKem), (UNASSIGNED_MARKER), o.ZaKem)
                Case "Zakem_TXT" : Return Function(o) If(String.IsNullOrEmpty(o.Zakem_TXT), (UNASSIGNED_MARKER), o.Zakem_TXT)
                Case "KtoZakryl" : Return Function(o) o.KtoZakryl
                Case "Istochnik" : Return Function(o) o.Istochnik
                Case "MestoOTS_Dor" : Return Function(o) o.MestoOTS_Dor
                Case "KrasREG" : Return Function(o) o.KrasREG
                Case "MestoOTS" : Return Function(o) o.MestoOTS
                Case "Marked" : Return Function(o) o.Marked
                Case "DaysOnRassled" : Return Function(o) o.DaysOnRassled
                Case "SerLokExact" : Return Function(o) o.SerLokExact
                Case "SerLok" : Return Function(o) o.SerLok
                Case "VidT" : Return Function(o) o.VidT
                Case "NumLok" : Return Function(o) o.NumLok
                Case "SerLokNumLokTXT" : Return Function(o) o.SerLokNumLokTXT
                Case "PripLok" : Return Function(o) o.PripLok
                Case "MyKlasLev1" : Return Function(o) o.MyKlasLev1
                Case "MyKlasLev2" : Return Function(o) o.MyKlasLev2
                Case "MyKlasLev3" : Return Function(o) o.MyKlasLev3
                Case Else : Return Nothing
            End Select
        End Function
        'Dlit
        '''' <summary>
        '''' Строит предикат фильтрации (безопасно для Nothing и Object)
        '''' </summary>
        Public Function BuildingSinglePropertyPredicate(propName As String, selectedValues As IEnumerable(Of Object)) As Func(Of Otkaz, Boolean)
            ' Преобразуем в List(Of Object) для удобства
            Dim selectedList As List(Of Object) = If(selectedValues, Enumerable.Empty(Of Object)()).ToList()

            ' Защита от Nothing и пустого выбора
            If selectedList Is Nothing OrElse selectedList.Count = 0 Then
                Return Function(o) True
            End If

            ' --- НОВОЕ: Проверим, есть ли FilterValue с определённым FieldType ---
            ' Это позволяет быстрее определить, что это за тип фильтра
            Dim firstTypedFV = selectedList.OfType(Of FilterValue)().FirstOrDefault(Function(fv) fv.FieldType <> "Unknown")

            If firstTypedFV IsNot Nothing Then
                ' Теперь мы знаем тип поля из FilterValue
                Select Case firstTypedFV.FieldType.ToLowerInvariant()
                    Case "boolean"
                        ' Обработка булева (как раньше, но можно уточнить)
                        ' selectedList содержит "True", "False"
                        Dim boolSet As New HashSet(Of Boolean)
                        For Each item In selectedList
                            Dim fv = TryCast(item, FilterValue)
                            If fv IsNot Nothing Then
                                ' Предполагаем, что Boolean FilterValue хранит True/False в Value
                                If TypeOf fv.Value Is Boolean Then
                                    boolSet.Add(CType(fv.Value, Boolean))
                                End If
                            Else
                                ' На всякий случай, если пришли строки
                                Dim s = item?.ToString()
                                If s IsNot Nothing Then
                                    If s.Equals("True", StringComparison.OrdinalIgnoreCase) Then
                                        boolSet.Add(True)
                                    ElseIf s.Equals("False", StringComparison.OrdinalIgnoreCase) Then
                                        boolSet.Add(False)
                                    End If
                                End If
                            End If
                        Next

                        Dim getter As Func(Of Otkaz, Boolean) = GetBooleanGetter(propName)
                        If getter IsNot Nothing Then
                            Return Function(o) boolSet.Contains(getter(o))
                        End If
                        Return Function(o) True ' Если геттер не найден

                    Case "number", "numeric"
                        ' Обработка чисел
                        Dim filterValues = selectedList.OfType(Of FilterValue)().Where(Function(fv) fv.Value IsNot Nothing).ToList()
                        If filterValues.Count > 0 Then
                            Return BuildNumericOrDatePredicate(propName, filterValues, "number")
                        End If
                        Return Function(o) True ' Если нет FilterValue

                    Case "date"
                        ' Обработка дат
                        Dim filterValues = selectedList.OfType(Of FilterValue)().Where(Function(fv) fv.Value IsNot Nothing).ToList()
                        If filterValues.Count > 0 Then
                            Return BuildNumericOrDatePredicate(propName, filterValues, "date")
                        End If
                        Return Function(o) True ' Если нет FilterValue

                    Case "string", "text"
                        ' Обработка строк
                        ' Преобразуем *только строки* в HashSet, отбрасывая FilterValue
                        Dim allowd As New HashSet(Of String)
                        For Each item In selectedList
                            If item IsNot Nothing AndAlso TypeOf item Is String Then
                                Dim s = item.ToString()
                                If Not String.IsNullOrEmpty(s) AndAlso s <> "[Все]" Then
                                    allowd.Add(s)
                                End If
                            End If
                        Next
                        Return BuildStringPredicate(propName, allowd)

                    Case Else ' Неизвестный FieldType
                        ' Возвращаемся к старой логике, если FieldType неизвестен
                        'Debug.WriteLine($"OtkazFilterConfig.BuildingSinglePropertyPredicate: Unknown FieldType '{firstTypedFV.FieldType}' from FilterValue, falling back to old logic.")
                End Select
            End If

            ' --- СТАРАЯ ЛОГИКА: если FieldType не был определён через FilterValue ---

            ' Проверим, является ли поле булевым через конфиг
            If OtkazFilterConfig.IsBooleanProperty(propName) Then
                ' Обрабатываем булевые значения (как раньше)
                Dim boolSet As New HashSet(Of Boolean)
                For Each item In selectedList
                    Dim s = item?.ToString()
                    If s IsNot Nothing Then
                        If s.Equals("True", StringComparison.OrdinalIgnoreCase) Then
                            boolSet.Add(True)
                        ElseIf s.Equals("False", StringComparison.OrdinalIgnoreCase) Then
                            boolSet.Add(False)
                        End If
                    End If
                Next

                Dim getter As Func(Of Otkaz, Boolean) = GetBooleanGetter(propName)
                If getter IsNot Nothing Then
                    Return Function(o) boolSet.Contains(getter(o))
                End If

                Return Function(o) True ' Если геттер не найден
            End If

            ' Сначала проверим, есть ли FilterValue (для дат, чисел) - старая логика
            Dim filterValuesOld = selectedList.OfType(Of FilterValue)().Where(Function(fv) fv.Value IsNot Nothing).ToList()
            'Debug.WriteLine($"BuildSinglePropertyPredicate: Found {filterValuesOld.Count} FilterValues via old logic")
            If filterValuesOld.Count > 0 Then
                ' Определяем тип по propName (старый способ)
                If {"PCh"}.Contains(propName) Then
                    Return BuildNumericOrDatePredicate(propName, filterValuesOld, "number")
                ElseIf {"Dlit"}.Contains(propName) Then
                    Return BuildNumericOrDatePredicate(propName, filterValuesOld, "number")
                ElseIf {"Nach", "Postup", "VernulsaOTS", "Zakryt", "Peredan", "Sozdan", "KorDate"}.Contains(propName) Then
                    Return BuildNumericOrDatePredicate(propName, filterValuesOld, "date")
                Else
                    ' Неизвестное числовое/датовое свойство
                    'Debug.WriteLine($"OtkazFilterConfig.BuildingSinglePropertyPredicate: Unknown numeric/date property '{propName}', returning True")
                    Return Function(o) True
                End If
            End If

            ' KorDate   Debug.WriteLine($"BuildSinglePropertyPredicate: No FilterValues, processing as strings")
            ' Преобразуем *только строки* в HashSet, отбрасывая Nothing и FilterValue
            Dim allowed As New HashSet(Of String)
            For Each item In selectedList ' <-- Теперь это List(Of Object)
                If item IsNot Nothing AndAlso TypeOf item Is String Then ' <-- Отсеиваем FilterValue
                    Dim s = item.ToString()
                    If Not String.IsNullOrEmpty(s) AndAlso s <> "[Все]" Then
                        allowed.Add(s)
                    End If
                End If
            Next

            ' === ДОБАВЛЕНО: поддержка [ПУСТЫЕ] ===
            If selectedList.OfType(Of String).Any(Function(s) s.StartsWith("[ПУСТЫЕ]")) Then
                Dim selector = OtkazFilterConfig.GetDisplaySelector(propName)
                If selector IsNot Nothing Then
                    Return Function(o) String.IsNullOrWhiteSpace(CStr(selector(o)))
                End If
            End If
            ' ===================================


            ' Вызываем вспомогательный метод для строк
            Return BuildStringPredicate(propName, allowed)
        End Function


        ' --- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ---

        ' Метод для обработки FilterValue (для чисел и дат) - обновлён
        ' Добавим параметр expectedType для уточнения типа
        Private Function BuildNumericOrDatePredicate(propName As String, filterValues As List(Of FilterValue), expectedType As String) As Func(Of Otkaz, Boolean)
            ' Это фильтр для числа или даты
            'Debug.WriteLine($"OtkazFilterConfig.BuildNumericOrDatePredicate: Processing FilterValue for property '{propName}', expectedType='{expectedType}'")

            Select Case expectedType.ToLowerInvariant()
                Case "number"
                    Select Case propName
                        Case "PCh"
                            ' Создаём список предикатов для каждого FilterValue
                            Dim predicateFuncs As New List(Of Func(Of Otkaz, Boolean))
                            For Each FVal In filterValues
                                If FVal.Value IsNot Nothing Then
                                    ' Проверим тип Value
                                    If TypeOf FVal.Value Is Single OrElse TypeOf FVal.Value Is Double OrElse TypeOf FVal.Value Is Integer Then
                                        Dim targetValue As Single = Convert.ToSingle(FVal.Value)
                                        Dim op As String = FVal.Operatr ' Используем твоё свойство
                                        Select Case op
                                            Case "="
                                                predicateFuncs.Add(Function(o) o.PCh = targetValue)
                                            Case "<>"
                                                predicateFuncs.Add(Function(o) o.PCh <> targetValue)
                                            Case ">"
                                                predicateFuncs.Add(Function(o) o.PCh > targetValue)
                                            Case ">="
                                                predicateFuncs.Add(Function(o) o.PCh >= targetValue)
                                            Case "<"
                                                predicateFuncs.Add(Function(o) o.PCh < targetValue)
                                            Case "<="
                                                predicateFuncs.Add(Function(o) o.PCh <= targetValue)
                                            Case Else

                                                ' Пропускаем условие с неизвестным оператором
                                        End Select
                                    Else

                                    End If
                                End If
                            Next

                            If predicateFuncs.Count = 0 Then
                                Return Function(o) True
                            End If

                            Return Function(o) predicateFuncs.All(Function(pf) pf(o))



                        Case "Dlit"
                            ' Создаём список предикатов для каждого FilterValue
                            Dim predicateFuncs As New List(Of Func(Of Otkaz, Boolean))
                            For Each FVal In filterValues
                                If FVal.Value IsNot Nothing Then
                                    ' Проверим тип Value
                                    If TypeOf FVal.Value Is Single OrElse TypeOf FVal.Value Is Double OrElse TypeOf FVal.Value Is Integer Then
                                        Dim targetValue As Single = Convert.ToSingle(FVal.Value)
                                        Dim op As String = FVal.Operatr ' Используем твоё свойство
                                        Select Case op
                                            Case "="
                                                predicateFuncs.Add(Function(o) o.Dlit = targetValue)
                                            Case "<>"
                                                predicateFuncs.Add(Function(o) o.Dlit <> targetValue)
                                            Case ">"
                                                predicateFuncs.Add(Function(o) o.Dlit > targetValue)
                                            Case ">="
                                                predicateFuncs.Add(Function(o) o.Dlit >= targetValue)
                                            Case "<"
                                                predicateFuncs.Add(Function(o) o.Dlit < targetValue)
                                            Case "<="
                                                predicateFuncs.Add(Function(o) o.Dlit <= targetValue)
                                            Case Else

                                        End Select
                                    Else

                                    End If
                                End If
                            Next

                            If predicateFuncs.Count = 0 Then
                                Return Function(o) True
                            End If

                            Return Function(o) predicateFuncs.All(Function(pf) pf(o))


                        Case Else

                            Return Function(o) True
                    End Select

                Case "date"
                    Select Case propName
                        Case "Nach", "Postup", "Zakryt", "Peredan", "Sozdan", "KorDate", "VernulsaOTS"
                            ' FilterValue.Value - это Date
                            ' Пока обрабатываем только оператор "="
                            If filterValues.All(Function(fv) fv.Operatr = "=") Then ' Проверим, что все операторы "="
                                ' Создаём HashSet для быстрого поиска
                                Dim targetDates As New HashSet(Of Date)(filterValues.Where(Function(fv) TypeOf fv.Value Is Date).Select(Function(fv) CType(fv.Value, Date)))

                                Select Case propName
                                    Case "Nach"
                                        Return Function(o) o.Nach > Date.MinValue AndAlso targetDates.Contains(o.Nach.Date)
                                    Case "Postup"
                                        Return Function(o) o.Postup > Date.MinValue AndAlso targetDates.Contains(o.Postup.Date)
                                    Case "VernulsaOTS"
                                        Return Function(o) o.VernulsaOTS > Date.MinValue AndAlso targetDates.Contains(o.VernulsaOTS.Date)
                                    Case "Zakryt"
                                        Return Function(o) o.Zakryt > Date.MinValue AndAlso targetDates.Contains(o.Zakryt.Date)
                                    Case "Peredan"
                                        Return Function(o) o.Peredan > Date.MinValue AndAlso targetDates.Contains(o.Peredan.Date)
                                    Case "Sozdan"
                                        Return Function(o) o.Sozdan > Date.MinValue AndAlso targetDates.Contains(o.Sozdan.Date)
                                    Case "KorDate"
                                        Return Function(o) o.KorDate > Date.MinValue AndAlso targetDates.Contains(o.KorDate.Date)
                                    Case Else ' Сюда не должны попасть, но на всякий случай
                                        Return Function(o) True
                                End Select
                            Else
                                ' KorDate Если есть другие операторы, вернём всё пропустить или обработаем сложнее
                                'Debug.WriteLine($"OtkazFilterConfig.BuildNumericOrDatePredicate: Date filter has non-'=' operators, returning True")
                                Return Function(o) True ' Пока так
                            End If

                        Case Else
                            'Debug.WriteLine($"OtkazFilterConfig.BuildNumericOrDatePredicate: Unknown date property '{propName}', returning True")
                            Return Function(o) True
                    End Select

                Case Else
                    'Debug.WriteLine($"OtkazFilterConfig.BuildNumericOrDatePredicate: Unexpected expectedType '{expectedType}', returning True")
                    Return Function(o) True
            End Select
        End Function

        ' Метод для обработки строк - без изменений
        Private Function BuildStringPredicate(propName As String, allowedStrings As HashSet(Of String)) As Func(Of Otkaz, Boolean)


            Try
                Debug.WriteLine($"OtkazFilterConfig.BuildStringPredicate: Processing strings for property '{propName}', count: {allowedStrings.Count}")

                If allowedStrings.Count > 0 Then
                    Select Case propName
                        Case "Kat", "PripMash", "MyKlasLev1", "MyKlasLev2", "MyKlasLev3", "Istochnik", "MestoOTS_Dor", "MestoOTS", "KtoZakryl", "ZaKem", "Zakem_TXT", "KomplexAsInt", "PripLok", "SerLokExact", "SerLok", "VidT", "NumLok", "DaysOnRassled", "IsStation", "KrasREG", "UpdateNotes", "SerLokNumLokTXT", "Marked", "PCh", "Dlit", "PlanListText"
                            Dim selector = GetDisplaySelector(propName)
                            If selector IsNot Nothing Then
                                If propName?.ToLowerInvariant() = "zakem" OrElse propName?.ToLowerInvariant() = "zakem_txt" Then
                                    Return Function(o)
                                               Dim v = If(propName?.ToLowerInvariant() = "zakem", o.ZaKem, o.Zakem_TXT)
                                               If allowedStrings.Contains(UNASSIGNED_MARKER) AndAlso (String.IsNullOrEmpty(v) OrElse v = "!") Then '
                                                   Return True
                                               End If
                                               If allowedStrings.Contains("!") AndAlso v = "!" Then
                                                   Return True
                                               End If
                                               Return allowedStrings.Contains(selector(o))
                                           End Function
                                Else
                                    Return Function(o) allowedStrings.Contains(selector(o))

                                End If
                            Else
                                'Zakem_TXT
                                Return Function(o) True
                            End If
                        Case Else
                            'Debug.WriteLine($"OtkazFilterConfig.BuildStringPredicate: Unknown property '{propName}' for string values, returning True")
                            Return Function(o) True
                    End Select
                Else
                    'Debug.WriteLine($"OtkazFilterConfig.BuildStringPredicate: No valid string values, returning True")
                    Return Function(o) True
                End If
            Catch ex As Exception
                Debug.WriteLine($"[BuildStringPredicate] CRASH: {ex.Message}")
                Debug.WriteLine(ex.StackTrace)
                Return Function(o) True
            End Try

        End Function

        ''' <summary>
        ''' Получает список строк "Значение (Количество)" для текстового свойства.
        ''' Используется для заполнения ListBox в фильтре.
        ''' </summary>
        Public Function GetTextualValuesForProperty(currentSource As IEnumerable(Of Otkaz), propName As String) As List(Of String)
            Dim selector = GetDisplaySelector(propName)
            If selector Is Nothing Then Return New List(Of String) From {"[Все]"}

            Dim allValues = currentSource.Select(selector).ToList()

            ' Подсчет пустых значений
            Dim emptyCount = allValues.Where(Function(v) String.IsNullOrWhiteSpace(CStr(v))).Count

            ' Группировка непустых значений с подсчетом количества
            Dim nonEmptyGroups = allValues.
                Where(Function(v) Not String.IsNullOrWhiteSpace(CStr(v))).
                GroupBy(Function(v) v).
                Select(Function(g) $"{g.Key} ({g.Count()})")

            Dim result As New List(Of String) From {"[Все]"}

            If emptyCount > 0 Then
                result.Add($"[ПУСТЫЕ] [{emptyCount}]")
            End If

            ' Добавляем отсортированные значения
            result.AddRange(nonEmptyGroups.OrderBy(Function(s) s))

            Return result
        End Function

        ''' <summary>
        ''' Получает список строк "Да [N]", "Нет [N]" для булевого свойства.
        ''' </summary>
        Public Function GetBooleanValuesForProperty(currentSource As IEnumerable(Of Otkaz), propName As String) As List(Of String)
            Dim getter As Func(Of Otkaz, Boolean) = GetBooleanGetter(propName)
            If getter Is Nothing Then Return New List(Of String)

            Dim grouped = currentSource.GroupBy(Function(o) getter(o))
            Dim values As New List(Of String)

            For Each g In grouped
                Dim displayText As String = If(g.Key, "Да", "Нет")
                values.Add($"{displayText} [{g.Count()}]")
            Next

            ' Сортировка по алфавиту (Да перед Нет)
            values.Sort()

            If values.Count > 0 Then
                values.Insert(0, "[Все]")
            Else
                values.Add("[Все]")
            End If

            Return values
        End Function

        ''' <summary>
        ''' Сортирует список значений в зависимости от флага isSortByCount.
        ''' </summary>
        Public Function SortValuesByToggle(values As List(Of String), propName As String, isSortByCount As Boolean) As List(Of String)
            ' Убираем "[Все]" временно, чтобы не мешал сортировке
            Dim allItem As String = Nothing
            If values.Count > 0 AndAlso values(0) = "[Все]" Then
                allItem = values(0)
                values.RemoveAt(0)
            End If

            If isSortByCount Then
                ' Сортировка по количеству (убывание), затем по имени
                values = values.OrderByDescending(Function(s)
                                                      Dim startIdx = s.IndexOf(" (")
                                                      Dim endIdx = s.IndexOf(")")
                                                      If startIdx >= 0 AndAlso endIdx > startIdx Then
                                                          Dim countStr = s.Substring(startIdx + 2, endIdx - startIdx - 2)
                                                          Dim countValue As Integer
                                                          If Integer.TryParse(countStr, countValue) Then
                                                              Return countValue
                                                          End If
                                                      End If
                                                      Return 0
                                                  End Function).
                                  ThenBy(Function(s) s).
                                  ToList()
            Else
                ' Сортировка по имени (алфавит)
                values = values.OrderBy(Function(s) s).ToList()

                ' Специфическая логика для определенных полей (как было в вашем оригинальном коде)
                If propName = "ZaKem" OrElse propName = "Zakem_TXT" Then
                    values = values.OrderBy(Function(s) If(s.StartsWith(UNASSIGNED_MARKER), 0, 1)).ThenBy(Function(s) s).ToList()
                ElseIf {"DaysOnRassled", "KomplexAsInt", "Kat"}.Contains(propName) Then
                    ' Для числовых полей сортируем по числу до скобки
                    values = values.OrderBy(Function(s) Val(If(s.Contains("("), s.Substring(0, s.IndexOf("(")), s))).ToList()
                End If
            End If

            ' Возвращаем "[Все]" на первое место
            If allItem IsNot Nothing Then
                values.Insert(0, allItem)
            End If

            Return values
        End Function

        ''' <summary>
        ''' Безопасно очищает выделение в ListBox.
        ''' </summary>
        Public Sub SafeClearSelection(listBox As ListBox)
            If listBox.SelectionMode = SelectionMode.Multiple Then
                listBox.SelectedItems.Clear()
            Else
                listBox.SelectedItem = Nothing
            End If
        End Sub

        ''' <summary>
        ''' Безопасно добавляет элемент в выделение ListBox.
        ''' </summary>
        Public Sub SafeAddToSelection(listBox As ListBox, item As Object)
            If listBox.SelectionMode = SelectionMode.Multiple Then
                listBox.SelectedItems.Add(item)
            Else
                listBox.SelectedItem = item
            End If
        End Sub

        ''' <summary>
        ''' Безопасно получает список выделенных элементов.
        ''' </summary>
        Public Function SafeGetSelectedItems(listBox As ListBox) As List(Of Object)
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

        ''' <summary>
        ''' Восстанавливает выделение в Popup на основе сохраненного состояния фильтра.
        ''' </summary>
        Public Sub RestoreSelection(popupCtrl As FilterPopup, savedFilterValues As List(Of Object), isBooleanField As Boolean, allValues As List(Of String))
            SafeClearSelection(popupCtrl.FilterListBox)

            If isBooleanField Then
                For Each item In savedFilterValues
                    Dim itemStr = item?.ToString()
                    If Not String.IsNullOrEmpty(itemStr) Then
                        ' Ищем "Да [...]" или "Нет [...]"
                        Dim match = allValues.FirstOrDefault(Function(v) (itemStr.Equals("True", StringComparison.OrdinalIgnoreCase) AndAlso v.StartsWith("Да [")) OrElse
                        (itemStr.Equals("False", StringComparison.OrdinalIgnoreCase) AndAlso v.StartsWith("Нет ["))
                    )
                        If match IsNot Nothing Then SafeAddToSelection(popupCtrl.FilterListBox, match)
                    End If
                Next
            Else
                ' Строковое поле: ищем совпадение по началу строки "Значение (...)"
                For Each item In savedFilterValues
                    Dim originalItem = item?.ToString()
                    ' Ищем строку, которая начинается с исходного значения и имеет пробел+скобку
                    Dim match = allValues.FirstOrDefault(Function(v) v.StartsWith(originalItem & " ("))
                    If match IsNot Nothing Then
                        SafeAddToSelection(popupCtrl.FilterListBox, match)
                    End If
                Next
            End If
        End Sub



    End Module


End Namespace

