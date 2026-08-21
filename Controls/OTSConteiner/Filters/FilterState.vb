Imports KACAHT_Next2.Kas

Public Class FilterState
    Private _filters As New Dictionary(Of String, List(Of Object))

    ' Сохранить выбор (универсальный метод)
    Public Sub SetFilter(propName As String, selected As IEnumerable(Of Object))
        If selected Is Nothing Then
            _filters(propName) = New List(Of Object)
        Else
            _filters(propName) = selected.ToList()
        End If
    End Sub

    ' Получить выбор
    Public Function GetFilter(propName As String) As List(Of Object)
        Dim lst As List(Of Object) = Nothing
        If _filters.TryGetValue(propName, lst) Then
            Return lst
        End If
        Return New List(Of Object)
    End Function

    ' Есть ли активные фильтры? (обновленная логика)
    Public Function HasActiveFilters() As Boolean
        For Each kvp In _filters
            Dim list As List(Of Object) = kvp.Value

            ' Проверяем, пустой ли список
            If list Is Nothing OrElse list.Count = 0 Then
                Continue For ' Пропускаем пустой фильтр
            End If

            ' Проверяем, состоит ли список *только* из "[Все]"
            If list.Count = 1 Then
                Dim firstItem = list(0)
                If TypeOf firstItem Is String AndAlso CStr(firstItem) = "[Все]" Then
                    Continue For ' Пропускаем "[Все]"
                End If
            End If

            ' Теперь проверяем содержимое списка
            For Each item In list
                If item Is Nothing Then Continue For ' Пропускаем Nothing

                ' Проверяем FilterValue
                Dim fv = TryCast(item, FilterValue)
                If fv IsNot Nothing Then
                    ' FilterValue считается активным, если Value не Nothing
                    If fv.Value IsNot Nothing Then
                        Return True ' Нашли активный FilterValue
                    End If
                    ' Если Value = Nothing, продолжаем проверку
                    Continue For
                End If

                ' Проверяем строку (не "[Все]", так как список не из одного "[Все]")
                Dim str = TryCast(item, String)
                If str IsNot Nothing AndAlso str <> "[Все]" Then
                    Return True ' Нашли активную строку
                End If

                ' Проверяем булево значение (если используется)
                ' Предположим, могут быть переданы Boolean значения напрямую
                ' Если item - Boolean, и он активен (True), считаем активным?
                ' Пока оставим так, как есть, если Boolean передаётся как строка ("True"/"False")

            Next
        Next

        Return False ' Ни один фильтр не активен
    End Function

    ' Собрать общий предикат (обновлённый)
    Public Function BuildPredicate() As Func(Of Otkaz, Boolean)
        Dim predicates As New List(Of Func(Of Otkaz, Boolean))

        For Each kvp In _filters
            Dim propName As String = kvp.Key
            Dim selectedList As List(Of Object) = kvp.Value.Where(Function(o) o IsNot Nothing).ToList()

            ' Проверяем, состоит ли список *только* из "[Все]" (для строк)
            If selectedList.Count = 1 Then
                Dim firstItem = selectedList(0)
                If TypeOf firstItem Is String AndAlso CStr(firstItem) = "[Все]" Then
                    Continue For ' Пропускаем этот фильтр
                End If
            End If

            ' Только если есть что-то кроме "[Все]"
            If selectedList.Count > 0 Then
                ' Важно: теперь мы не знаем тип поля здесь, но OtkazFilterConfig должен его определять
                ' Поэтому передаём всё как есть, а внутри будет логика
                'Debug.WriteLine($"FilterState.BuildPredicate: PropertyName='{propName}', Selected Count={selectedList.Count}")
                For Each sel In selectedList
                    'Debug.WriteLine($"  - Selected Item: Type={sel.GetType().Name}, Value={sel}")
                Next

                ' Вызываем старый метод (его потом обновим)
                Dim pred = OtkazFilterConfig.BuildingSinglePropertyPredicate(propName, selectedList)
                'Debug.WriteLine($"FilterState.BuildPredicate: Predicate for '{propName}' is Nothing: {pred Is Nothing}")

                If pred IsNot Nothing Then ' Защита
                    predicates.Add(pred)
                End If
            End If
        Next

        ' Если нет активных фильтров
        If predicates.Count = 0 Then
            Return Nothing
        End If

        ' Возвращаем объединённый предикат (AND между всеми условиями)
        Return Function(o) predicates.All(Function(p) p(o))
    End Function

    ' Сброс
    Public Sub Clear()
        _filters.Clear()
    End Sub

    ' Устаревший метод - можно оставить для совместимости
    Public Sub SetFilterWithOperator(propName As String, values As IEnumerable(Of FilterValue))
        If values Is Nothing Then
            _filters(propName) = New List(Of Object)
        Else
            _filters(propName) = values.Cast(Of Object).ToList()
        End If
    End Sub

    ' Метод для сброса фильтра по конкретному свойству
    Public Sub ClearFilterForProperty(propName As String)
        If _filters.ContainsKey(propName) Then
            _filters.Remove(propName)
        End If
    End Sub

    ' Метод для получения всех фильтров
    Public Function GetAllFilters() As Dictionary(Of String, List(Of Object))
        Dim copy As New Dictionary(Of String, List(Of Object))
        For Each kvp In _filters
            copy(kvp.Key) = kvp.Value.ToList() ' Копируем список
        Next
        Return copy
    End Function

    ' НОВОЕ: Метод для получения типа поля по его значениям (если известен)
    ' Полезно, если тип поля не задан в XAML, но мы хотим его определить
    Public Function GetInferredFieldType(propName As String) As String
        Dim list = GetFilter(propName)
        If list IsNot Nothing AndAlso list.Count > 0 Then
            ' Берём первый не-Nothing элемент и определяем тип
            For Each item In list
                If item Is Nothing Then Continue For
                If TypeOf item Is FilterValue Then
                    Dim fv = DirectCast(item, FilterValue)
                    If fv.FieldType <> "Unknown" Then Return fv.FieldType
                End If
                ' Или определяем по типу самого объекта
                Select Case True
                    Case TypeOf item Is String
                        ' Если это "[Все]", пробуем следующий
                        If CStr(item) <> "[Все]" Then Return "String"
                    Case TypeOf item Is Single, TypeOf item Is Double, TypeOf item Is Integer
                        Return "Number"
                    Case TypeOf item Is Date
                        Return "Date"
                    Case TypeOf item Is Boolean
                        Return "Boolean"
                End Select
            Next
        End If
        Return "Unknown" ' Не удалось определить
    End Function

End Class



Public Class FilterValue
    ' Основные свойства
    Public Property Value As Object ' Оставляем Object для гибкости (Single, Date, String и т.п.)
    Public Property Operatr As String = "=" ' Исправим опечатку
    Public Property FieldType As String = "Unknown" ' Новый: тип поля

    ' Конструкторы
    Public Sub New(value As Object, [operator] As String)
        Me.Value = value
        Me.Operatr = [operator]
    End Sub

    Public Sub New(value As Object, [operator] As String, fieldType As String)
        Me.Value = value
        Me.Operatr = [operator]
        Me.FieldType = fieldType.ToLowerInvariant()
    End Sub

    ' Переопределим ToString для удобства отладки
    Public Overrides Function ToString() As String
        Return $"[{FieldType}] {Operatr} {Value}"
    End Function
End Class