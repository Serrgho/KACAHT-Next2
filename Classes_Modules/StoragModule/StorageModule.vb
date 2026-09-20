Imports System.Globalization
Imports System.IO
Imports KACAHT_Next2.Kas
Imports Newtonsoft.Json

Module StorageModule
    Private ReadOnly _settings As JsonSerializerSettings = New JsonSerializerSettings With {
        .NullValueHandling = NullValueHandling.Ignore,
        .DateTimeZoneHandling = DateTimeZoneHandling.Local,
        .Converters = {New VBDateConverter()}
    }




    'Public Function LoadFilteredFromJson(filePath As String, dateFrom As Date, dateTo As Date) As List(Of Otkaz)
    '    Dim result As New List(Of Otkaz)()
    '    If Not File.Exists(filePath) Then Return result

    '    ' Настройки сериализатора (должны совпадать с теми, что используете при сохранении)
    '    Dim serializerSettings As New JsonSerializerSettings With {
    '    .NullValueHandling = NullValueHandling.Ignore,
    '    .MissingMemberHandling = MissingMemberHandling.Ignore,
    '    .DateFormatHandling = DateFormatHandling.IsoDateFormat
    '}
    '    Dim serializer = JsonSerializer.Create(serializerSettings)

    '    ' 64 КБ буфер + SequentialScan ускоряют чтение больших файлов в 2-3 раза
    '    Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan)
    '        Using sr As New StreamReader(fs)
    '            Using reader As New JsonTextReader(sr)
    '                ' Ожидается структура JSON: [ { ... }, { ... }, ... ]
    '                If reader.Read() AndAlso reader.TokenType = JsonToken.StartArray Then
    '                    While reader.Read()
    '                        ' Пропускаем всё, кроме начала объектов
    '                        If reader.TokenType = JsonToken.StartObject Then
    '                            Try
    '                                Dim item = serializer.Deserialize(Of Otkaz)(reader)
    '                                If item IsNot Nothing Then
    '                                    ' 🔍 Фильтрация по дате начала (.Date отбрасывает время)
    '                                    If item.Nach.Date >= dateFrom.Date AndAlso item.Nach.Date <= dateTo.Date Then
    '                                        result.Add(item)
    '                                    End If
    '                                End If
    '                            Catch ex As Exception
    '                                ' Битая запись не должна ломать загрузку всего файла
    '                                ' System.Diagnostics.Debug.WriteLine($"⚠️ Пропуск записи: {ex.Message}")
    '                            End Try
    '                        End If
    '                    End While
    '                End If
    '            End Using
    '        End Using
    '    End Using
    '    Return result
    'End Function



    ''' <summary>
    ''' Сохраняет список отказов в JSON-файл
    ''' </summary>
    Public Sub SaveToJson(otkazy As List(Of Otkaz), filePath As String)
        Try
            Dim json As String = JsonConvert.SerializeObject(otkazy, Formatting.None, _settings)
            File.WriteAllText(filePath, json, System.Text.Encoding.UTF8)
            MW.InfoBLOK.AddItem($"✅ Сохранено {otkazy.Count} записей в {filePath}")
        Catch ex As Exception
            MW.InfoBLOK.AddItem($"❌ Ошибка сохранения JSON: {ex.Message}")
            Throw
        End Try
    End Sub

    ''' <summary>
    ''' Загружает список отказов из JSON-файла
    ''' </summary>
    Public Function LoadFromJson(filePath As String) As List(Of Otkaz)
        Try
            If Not File.Exists(filePath) Then
                'Debug.WriteLine($"⚠️ Файл не найден: {filePath}")
                Return New List(Of Otkaz)
            End If

            Dim json As String = File.ReadAllText(filePath, System.Text.Encoding.UTF8)
            Dim result As List(Of Otkaz) = JsonConvert.DeserializeObject(Of List(Of Otkaz))(json, _settings)

            Return If(result, New List(Of Otkaz))
        Catch ex As Exception
            'Debug.WriteLine($"❌ Ошибка загрузки JSON: {ex.Message}")
            Throw
        End Try
    End Function

    ''' <summary>
    ''' Генерирует имя JSON-файла по периоду дат .Nach: "ots_dd.MM.yyyy-dd.MM.yyyy.json"
    ''' Возвращает Nothing, если нет валидных дат.
    ''' </summary>
    Public Function GenerateJsonFileName(otkazy As List(Of Otkaz)) As String
        If otkazy Is Nothing OrElse otkazy.Count = 0 Then Return Nothing

        Dim validDates = otkazy.Where(Function(o) o.Nach <> Date.MinValue).Select(Function(o) o.Nach).ToList()
        If validDates.Count = 0 Then Return Nothing

        Dim first = validDates.Min()
        Dim last = validDates.Max()

        Dim fmt = "dd.MM.yyyy"
        Dim firstStr = first.ToString(fmt) ', CultureInfo.InvariantCulture)
        Dim lastStr = last.ToString(fmt) ', CultureInfo.InvariantCulture)

        Return $"ots_{firstStr}-{lastStr}.json"
    End Function

    ''' <summary>
    ''' Возвращает путь к папке данных (из My.Settings или по умолчанию).
    ''' Не создаёт папку — только возвращает путь.
    ''' </summary>
    Public Function GetDataFolderPath() As String
        Dim pth As String = My.Settings.DataFolderPath
        If String.IsNullOrWhiteSpace(pth) Then
            pth = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                 "Data"
            )
        End If
        Return pth
    End Function


End Module
