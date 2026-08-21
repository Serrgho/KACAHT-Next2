Imports System.Text
Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.IO
Imports System.Linq

Imports System.Windows.Forms
Namespace Kas
    Module SerLokModule
        '========================================================================================================
        '========================================================================================================
        '========================================================================================================
        '========================================================================================================

        ' Файлы для хранения пользовательских серий по типам тяги
        Friend ReadOnly _seriesElektrovozFileName As String = "user_Elektrovoz_series.txt"
        Friend ReadOnly _seriesTeplovozFileName As String = "user_Teplovoz_series.txt"
        Friend ReadOnly _seriesParovozFileName As String = "user_Parovoz_series.txt"
        Friend ReadOnly _seriesMVPSFileName As String = "user_MVPS_series.txt"
        Friend ReadOnly _seriesSSPSFileName As String = "user_SSPS_series.txt"

        ' Списки для хранения серий по типам
        Friend _lokElektrovozList As List(Of String)
        Friend _lokTeplovozList As List(Of String)
        Friend _lokParovozList As List(Of String)
        Friend _lokMVPSList As List(Of String)
        Friend _lokSSPSList As List(Of String)

        ' Базовые серии
        Public ReadOnly _baseElektrovozSeries As New List(Of String) From {
    "3ЭС5К", "2ЭС5К", "1,5ВЛ80Р", "ВЛ80Р", "ВЛ85", "ЭП1", "ЭП1П", "1,5ВЛ80С", "1,5ВЛ80СК", "1,5ВЛ80ТК",
     "2ЭС6", "2ЭС6Б", "3ЭС6", "4ЭС5К", "ВЛ10", "ВЛ65", "ВЛ80С", "ВЛ80Т", "ВЛ80ТК", "Э5К", "ЭП2К"
}

        Public ReadOnly _baseTeplovozSeries As New List(Of String) From {
    "2ТЭ10М", "2ТЭ10У", "ТЭМ18Д", "ТЭМ18ДМ", "ТЭМ2", "3ТЭ10М", "2ТЭ10С",
     "3ТЭ10У", "3ТЭ10УК", "ТГМ6Д", "ТЭМ7А"
}

        Public ReadOnly _baseParovozSeries As New List(Of String) From {
    "Л", "П36"
}

        Public ReadOnly _baseMVPSSeries As New List(Of String) From {
    "ЭД9М", "ЭД9МК", "ЭП3Д"
}

        Public ReadOnly _baseSSPSSeries As New List(Of String) From {
    "МПТ"
}

        ''' <summary>
        ''' Полный путь к файлу с пользовательскими сериями по типу
        ''' </summary>
        Private Function GetSeriesFilePath(fileName As String) As String
            Dim dataFolder = GetOrCreateDataFolderPath(ForParams:=True)
            If String.IsNullOrEmpty(dataFolder) Then
                Return Nothing
            End If
            Return Path.Combine(dataFolder, fileName)
        End Function

        ''' <summary>
        ''' Свойства для доступа к спискам серий
        ''' </summary>
        Public ReadOnly Property LokElektrovozList As List(Of String)
            Get
                'If _lokElektrovozList Is Nothing Then LoadSeriesByType()
                'Return _lokElektrovozList
                If _lokElektrovozList Is Nothing Then
                    _lokElektrovozList = New List(Of String)()
                    LoadOrCreateSeries(_seriesElektrovozFileName, _lokElektrovozList, _baseElektrovozSeries)
                    '_lokElektrovozList.Sort()
                End If
                Return _lokElektrovozList
            End Get
        End Property

        Public ReadOnly Property LokTeplovozList As List(Of String)
            Get
                'If _lokTeplovozList Is Nothing Then LoadSeriesByType()
                'Return _lokTeplovozList
                If _lokTeplovozList Is Nothing Then
                    _lokTeplovozList = New List(Of String)()
                    LoadOrCreateSeries(_seriesTeplovozFileName, _lokTeplovozList, _baseTeplovozSeries)
                    '_lokTeplovozList.Sort()
                End If
                Return _lokTeplovozList
            End Get
        End Property

        Public ReadOnly Property LokParovozList As List(Of String)
            Get
                'If _lokParovozList Is Nothing Then LoadSeriesByType()
                'Return _lokParovozList
                If _lokParovozList Is Nothing Then
                    _lokParovozList = New List(Of String)()
                    LoadOrCreateSeries(_seriesParovozFileName, _lokParovozList, _baseParovozSeries)
                    '_lokParovozList.Sort()
                End If
                Return _lokParovozList
            End Get
        End Property

        Public ReadOnly Property LokMVPSList As List(Of String)
            Get
                'If _lokMVPSList Is Nothing Then LoadSeriesByType()
                'Return _lokMVPSList
                If _lokMVPSList Is Nothing Then
                    _lokMVPSList = New List(Of String)()
                    LoadOrCreateSeries(_seriesMVPSFileName, _lokMVPSList, _baseMVPSSeries)
                    '_lokMVPSList.Sort()
                End If
                Return _lokMVPSList
            End Get
        End Property

        Public ReadOnly Property LokSSPSList As List(Of String)
            Get
                'If _lokSSPSList Is Nothing Then LoadSeriesByType()
                'Return _lokSSPSList
                If _lokSSPSList Is Nothing Then
                    _lokSSPSList = New List(Of String)()
                    LoadOrCreateSeries(_seriesSSPSFileName, _lokSSPSList, _baseSSPSSeries)
                    '_lokSSPSList.Sort()
                End If
                Return _lokSSPSList
            End Get
        End Property



        ''' <summary>
        ''' Загружает серии из файла или создает файл с базовыми сериями если его нет
        ''' </summary>
        Private Sub LoadOrCreateSeries(fileName As String, ByRef seriesList As List(Of String), baseSeries As List(Of String))
            Dim filePath = GetSeriesFilePath(fileName)
            ' 1. Добавляем БАЗОВЫЕ — всегда
            seriesList.AddRange(baseSeries)

            ' 2. Читаем ПОЛЬЗОВАТЕЛЬСКИЕ из файла (если есть)
            If filePath IsNot Nothing AndAlso File.Exists(filePath) Then
                Try
                    Dim lines = File.ReadAllLines(filePath, Encoding.UTF8)
                    For Each line In lines
                        Dim s = line.Trim()
                        If Not String.IsNullOrWhiteSpace(s) AndAlso
                           Not baseSeries.Contains(s) AndAlso        ' не базовая
                           Not seriesList.Contains(s) Then            ' не дубль
                            seriesList.Add(s)
                        End If
                    Next
                    Debug.WriteLine($"Загружено пользовательских серий из {fileName}: {seriesList.Count - baseSeries.Count}")
                Catch ex As Exception
                    Debug.WriteLine($"Ошибка загрузки {fileName}: {ex.Message}")
                End Try
            End If

            ' 3. Сортируем
            'seriesList.Sort()

        End Sub


        ''' <summary>
        ''' Добавляет новую серию в указанный тип и сохраняет в файл
        ''' </summary>
        Public Sub AddNewSeries(newSeries As String, seriesType As String)

            Dim trimmedSeries = newSeries.Trim().ToUpper()

            ' Определяем, в какой список добавлять
            Dim targetList As List(Of String) = Nothing
            Dim fileName As String = ""
            Dim baseList As List(Of String) = Nothing  ' ← ДОБАВЛЕНО

            Select Case seriesType.ToLower()
                Case "elektrovoz", "электровоз"
                    targetList = LokElektrovozList
                    fileName = _seriesElektrovozFileName
                    baseList = _baseElektrovozSeries  ' ← ДОБАВЛЕНО
                Case "teplovoz", "тепловоз"
                    targetList = LokTeplovozList
                    fileName = _seriesTeplovozFileName
                    baseList = _baseTeplovozSeries  ' ← ДОБАВЛЕНО
                Case "parovoz", "паровоз"
                    targetList = LokParovozList
                    fileName = _seriesParovozFileName
                    baseList = _baseParovozSeries  ' ← ДОБАВЛЕНО
                Case "mvps", "мвпс"
                    targetList = LokMVPSList
                    fileName = _seriesMVPSFileName
                    baseList = _baseMVPSSeries  ' ← ДОБАВЛЕНО
                Case "ssps", "сспс"
                    targetList = LokSSPSList
                    fileName = _seriesSSPSFileName
                    baseList = _baseSSPSSeries  ' ← ДОБАВЛЕНО
                Case Else
                    Return
            End Select

            ' Проверяем дубликат
            If targetList.Contains(trimmedSeries) Then
                Debug.WriteLine($"Серия {trimmedSeries} уже существует в списке {seriesType}")
                Return
            End If

            ' Добавляем
            targetList.Add(trimmedSeries)
            ' targetList.Sort()

            ' Сохраняем ТОЛЬКО пользовательские серии (без базовых)
            SaveSeriesToFile(fileName, targetList, baseList)  ' ← ИЗМЕНЕНО: + baseList

            Debug.WriteLine($"Добавлена новая серия: {trimmedSeries} в тип: {seriesType}")

        End Sub




        ''' <summary>
        ''' Сохраняет ВСЕ серии указанного типа в файл
        ''' </summary>
        Private Sub SaveSeriesToFile(fileName As String, seriesList As List(Of String), baseList As List(Of String))
            Dim filePath = GetSeriesFilePath(fileName)
            If filePath Is Nothing Then Return
            Try
                ' Фильтруем: только НЕ базовые
                Dim userOnly As New List(Of String)
                For Each s In seriesList
                    If Not baseList.Contains(s) Then
                        userOnly.Add(s)
                    End If
                Next

                File.WriteAllLines(filePath, userOnly, Encoding.UTF8)
                Debug.WriteLine($"Сохранено {userOnly.Count} пользовательских серий в {fileName}")
            Catch ex As Exception
                Debug.WriteLine($"Ошибка сохранения {fileName}: {ex.Message}")
            End Try

        End Sub


        ''' <summary>
        ''' Получить список серий по типу
        ''' </summary>
        Public Function GetSeriesByType(seriesType As String) As List(Of String)
            Select Case seriesType.ToLower()
                Case "elektrovoz", "электровоз"
                    Return New List(Of String)(LokElektrovozList)
                Case "teplovoz", "тепловоз"
                    Return New List(Of String)(LokTeplovozList)
                Case "parovoz", "паровоз"
                    Return New List(Of String)(LokParovozList)
                Case "mvps", "мвпс"
                    Return New List(Of String)(LokMVPSList)
                Case "ssps", "сспс"
                    Return New List(Of String)(LokSSPSList)
                Case Else
                    Return New List(Of String)()
            End Select
        End Function



        ''' <summary>
        ''' Получить все типы серий для TabControl
        ''' </summary>
        Public Function GetAllSeriesTypes() As Dictionary(Of String, String)
            Return New Dictionary(Of String, String) From {
        {"Elektrovoz", "Электровозы"},
        {"Teplovoz", "Тепловозы"},
        {"Parovoz", "Паровозы"},
        {"MVPS", "МВПС"},
        {"SSPS", "ССПС"}
    }
        End Function

        ''' <summary>
        ''' Получить все серии из всех категорий
        ''' </summary>
        Public Function GetAllSeries() As List(Of String)
            Dim allSeries As New List(Of String)

            allSeries.AddRange(LokElektrovozList)
            allSeries.AddRange(LokTeplovozList)
            allSeries.AddRange(LokParovozList)
            allSeries.AddRange(LokMVPSList)
            allSeries.AddRange(LokSSPSList)

            allSeries.Sort()
            Return allSeries
        End Function

        ''' <summary>
        ''' Проверить, есть ли серия в списках
        ''' </summary>
        Public Function SeriesExists(series As String) As Boolean
            Dim trimmedSeries = series.Trim()

            Return LokElektrovozList.Contains(trimmedSeries) OrElse
           LokTeplovozList.Contains(trimmedSeries) OrElse
           LokParovozList.Contains(trimmedSeries) OrElse
           LokMVPSList.Contains(trimmedSeries) OrElse
           LokSSPSList.Contains(trimmedSeries)
        End Function

        ''' <summary>
        ''' Удаляет серию из указанного типа и обновляет файл
        ''' </summary>
        Public Function RemoveSeries(series As String, seriesType As String) As Boolean
            If String.IsNullOrWhiteSpace(series) OrElse String.IsNullOrWhiteSpace(seriesType) Then Return False

            Dim trimmedSeries = series.Trim().ToUpper()

            ' Определяем, из какого списка удалять
            Dim targetList As List(Of String) = Nothing
            Dim baseList As List(Of String) = Nothing
            Dim fileName As String = ""

            Select Case seriesType.ToLower()
                Case "elektrovoz", "электровоз"
                    targetList = _lokElektrovozList
                    baseList = _baseElektrovozSeries
                    fileName = _seriesElektrovozFileName
                Case "teplovoz", "тепловоз"
                    targetList = _lokTeplovozList
                    baseList = _baseTeplovozSeries
                    fileName = _seriesTeplovozFileName
                Case "parovoz", "паровоз"
                    targetList = _lokParovozList
                    baseList = _baseParovozSeries
                    fileName = _seriesParovozFileName
                Case "mvps", "мвпс"
                    targetList = _lokMVPSList
                    baseList = _baseMVPSSeries
                    fileName = _seriesMVPSFileName
                Case "ssps", "сспс"
                    targetList = _lokSSPSList
                    baseList = _baseSSPSSeries
                    fileName = _seriesSSPSFileName
                Case Else
                    Return False
            End Select

            ' Проверяем, что список инициализирован
            If targetList Is Nothing Then Return False

            ' Нельзя удалять базовые серии
            If baseList.Contains(trimmedSeries) Then
                Debug.WriteLine($"Нельзя удалить базовую серию: {trimmedSeries}")
                Return False
            End If

            ' Удаляем из списка если есть
            Dim removed = targetList.Remove(trimmedSeries)

            If removed Then
                ' Сохраняем обновленный список в файл
                SaveSeriesToFile(fileName, targetList, baseList)
                'SaveSeriesToFile(fileName, targetList)
                Debug.WriteLine($"Удалена серия: {trimmedSeries} из типа: {seriesType}")
            Else
                Debug.WriteLine($"Серия {trimmedSeries} не найдена в списке {seriesType}")
            End If

            Return removed
        End Function

        Public Function GetVidTBySeries(series As String) As String
            Dim s = series?.Trim().ToUpper()

            If LokElektrovozList.Contains(s) Then Return "э"
            If LokTeplovozList.Contains(s) Then Return "т"
            If LokParovozList.Contains(s) Then Return "п"
            If LokMVPSList.Contains(s) Then Return "мвпс"
            If LokSSPSList.Contains(s) Then Return "сспс"

            Return ""  ' ← если не найдена
        End Function


        '========================================================================================================
        '========================================================================================================
        '========================================================================================================
        '========================================================================================================
    End Module
End Namespace

