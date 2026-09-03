Module FilterDisplayNames
    ' Словарь для сопоставления propertyName и отображаемого текста
    Private ReadOnly _displayNameMap As New Dictionary(Of String, String) From {
        {"PripLok", "Приписка локомотива"},
        {"Kat", "Категория"},
        {"Nach", "Начало"},
        {"Postup", "Поступил"},
        {"VernulsaOTS", "Вернулся"},
        {"Zakryt", "Закрыт"},
        {"Peredan", "Передан"},
        {"PCh", "Часы"},
        {"Dlit", "Продолжительность ОТС"},
        {"Istochnik", "Источник"},
        {"MestoOTS_Dor", "Дорога отказа"},
        {"Marked", "Помеченные отказы"},
        {"MestoOTS", "Станция/перегон отказа"},
        {"KtoZakryl", "Кто расследовал"},
        {"Zakem_TXT", "Отнесен на"},
        {"KomplexAsInt", "Комплекс депо"},
        {"SerLokExact", "Серия локомотива"},
        {"SerLok", "Объединенные серии локомотивов"},
        {"VidT", "Вид тяги"},
        {"NumLok", "Номер локомотива"},
        {"PripMash", "Приписка машиниста"},
        {"DaysOnRassled", "Дней в расследовании"},
        {"IsStation", "Станция/Перегон"},
        {"MyKlasLev1", "Оборудование Ур.1"},
        {"MyKlasLev2", "Оборудование Ур.2"},
        {"MyKlasLev3", "Оборудование Ур.3"},
        {"Sozdan", "Дата создания отказа"},
        {"KorDate", "Дата последней корректировки"},
        {"PlanListText", "План отнесения отказа"},
        {"UpdateNotes", "Обновления из КАС АНТ"},
        {"SerLokNumLokTXT", "Серия и номер локомотива"},
        {"KrasREG", "Регион отказа"}
            }
    'Dlit
    ' Метод для получения отображаемого имени по propertyName
    Public Function GetDisplayName(propName As String) As String
        If _displayNameMap.ContainsKey(propName) Then
            Return _displayNameMap(propName)
        Else
            ' Если имя не найдено, возвращаем оригинальное propName 
            Return propName
        End If
    End Function


End Module
