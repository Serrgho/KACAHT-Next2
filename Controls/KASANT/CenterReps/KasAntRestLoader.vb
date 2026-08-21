Imports System.ComponentModel
Imports System.Globalization
Imports System.Net
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Runtime.CompilerServices
Imports System.Text
Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports System.Text.RegularExpressions

Namespace Kas

    Public Module KasAntRestLoader

        ''=======================================================================
        '' КЛАССЫ ДЛЯ ДЕСЕРИАЛИЗАЦИИ ОТВЕТА КАСАНТ REST API
        ''=======================================================================
        'Public Class JournalItemInfo
        '    <JsonPropertyName("columnName")> Public Property ColumnName As String
        '    <JsonPropertyName("text")> Public Property Text As String
        'End Class

        'Public Class JournalRecordRest
        '    <JsonPropertyName("info")> Public Property Info As List(Of JournalItemInfo)

        '    Public Function GetFieldValue(columnName As String) As String
        '        Dim item = Info?.FirstOrDefault(Function(x) String.Equals(x.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
        '        Return If(item?.Text, "").Trim()
        '    End Function

        '    Public ReadOnly Property Category As String
        '        Get
        '            Return GetFieldValue("CATEGORY")
        '        End Get
        '    End Property

        '    Public ReadOnly Property ViolId As String
        '        Get
        '            Return GetFieldValue("ID")
        '        End Get
        '    End Property

        '    Public ReadOnly Property DateFrom As String
        '        Get
        '            Return GetFieldValue("DATEFROM")
        '        End Get
        '    End Property

        '    Public ReadOnly Property DateTo As String
        '        Get
        '            Return GetFieldValue("DATETO")
        '        End Get
        '    End Property

        '    Public ReadOnly Property PlaceInfo As String
        '        Get
        '            Return GetFieldValue("PLACEINFO")
        '        End Get
        '    End Property

        '    Public ReadOnly Property Investigation As String
        '        Get
        '            Return GetFieldValue("INVESTIGATION")
        '        End Get
        '    End Property

        '    Public ReadOnly Property Guilty As String
        '        Get
        '            Return GetFieldValue("GUILTY")
        '        End Get
        '    End Property

        '    Public ReadOnly Property Train As String
        '        Get
        '            Return GetFieldValue("TRAIN")
        '        End Get
        '    End Property

        '    Public ReadOnly Property ReasonObjectTop As String
        '        Get
        '            Return GetFieldValue("REASONOBJECTTOP")
        '        End Get
        '    End Property

        '    Public ReadOnly Property GroupReason As String
        '        Get
        '            Return GetFieldValue("GROUPREASON")
        '        End Get
        '    End Property

        '    Public ReadOnly Property Reason As String
        '        Get
        '            Return GetFieldValue("REASON")
        '        End Get
        '    End Property
        'End Class

        'Public Class FinalJournalInfo
        '    <JsonPropertyName("finalInfo")> Public Property FinalInfo As List(Of String)
        'End Class

        'Public Class JournalApiResponse
        '    <JsonPropertyName("finalJournalInfo")> Public Property FinalJournalInfo As FinalJournalInfo
        '    <JsonPropertyName("listValue")> Public Property ListValue As List(Of JournalRecordRest)
        '    <JsonPropertyName("numberOfElements")> Public Property NumberOfElements As Integer
        '    <JsonPropertyName("totalElements")> Public Property TotalElements As Integer
        '    <JsonPropertyName("textMessageForView")> Public Property TextMessageForView As String
        'End Class

        '=======================================================================
        ' ОСНОВНОЙ КЛАСС FETCHER
        '=======================================================================
        'Public Class KasantRestFetcher
        '    Implements INotifyPropertyChanged

        '    Private ReadOnly _httpClient As HttpClient
        '    Private ReadOnly _cookieContainer As New CookieContainer()
        '    Public ReadOnly _baseUrl As String = "http://kasant.gvc.oao.rzd:8888/kasant"

        '    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        '    Private _dorKod As Integer = 0
        '    Private _dateFrom As DateTime = DateTime.Today.AddDays(-1)
        '    Private _dateTo As DateTime = DateTime.Today
        '    Private _statusFilter As String = "0,1,2,6,7,10"
        '    Private _pageSize As Integer = 10
        '    Private _sessionInitialized As Boolean = False

        '    ' Настройки нового API
        '    Public Property ReportName As String = "DetailReportInfo2755"
        '    Public Property CriptFilterInfo As String = ""
        '    Public Property Presentation As String = "html"
        '    Public Property ReportLevel As String = "CENTER"

        '    Public Property DorKod As Integer
        '        Get
        '            Return _dorKod
        '        End Get
        '        Set(value As Integer)
        '            If _dorKod <> value Then
        '                _dorKod = value
        '                RaisePropertyChanged()
        '            End If
        '        End Set
        '    End Property

        '    Public Property DateFrom As DateTime
        '        Get
        '            Return _dateFrom
        '        End Get
        '        Set(value As DateTime)
        '            If _dateFrom <> value Then
        '                _dateFrom = value
        '                RaisePropertyChanged()
        '                RaisePropertyChanged(NameOf(DateRangeText))
        '            End If
        '        End Set
        '    End Property

        '    Public Property DateTo As DateTime
        '        Get
        '            Return _dateTo
        '        End Get
        '        Set(value As DateTime)
        '            If _dateTo <> value Then
        '                _dateTo = value
        '                RaisePropertyChanged()
        '                RaisePropertyChanged(NameOf(DateRangeText))
        '            End If
        '        End Set
        '    End Property

        '    Public ReadOnly Property DateRangeText As String
        '        Get
        '            Return $"{DateFrom:dd.MM.yy} - {DateTo:dd.MM.yy}"
        '        End Get
        '    End Property

        '    Public Property StatusFilter As String
        '        Get
        '            Return _statusFilter
        '        End Get
        '        Set(value As String)
        '            If _statusFilter <> value Then
        '                _statusFilter = value
        '                RaisePropertyChanged()
        '            End If
        '        End Set
        '    End Property

        '    Public Property PageSize As Integer
        '        Get
        '            Return _pageSize
        '        End Get
        '        Set(value As Integer)
        '            If _pageSize <> value AndAlso value > 0 Then
        '                _pageSize = value
        '                RaisePropertyChanged()
        '            End If
        '        End Set
        '    End Property

        '    Public Sub New(Optional handler As HttpClientHandler = Nothing)
        '        If handler Is Nothing Then
        '            handler = New HttpClientHandler()
        '            handler.CookieContainer = _cookieContainer
        '            handler.AllowAutoRedirect = True
        '            handler.UseCookies = True
        '        End If
        '        _httpClient = New HttpClient(handler)
        '        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.216 YaBrowser/21.5.4.610 Yowser/2.5 Safari/537.36")
        '        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*")
        '        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "ru,en;q=0.9")
        '        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
        '    End Sub

        '    Protected Sub RaisePropertyChanged(<CallerMemberName> Optional propertyName As String = Nothing)
        '        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
        '    End Sub

        '    ' ===================================================================
        '    ' 🔥 ЛОГИРОВАНИЕ
        '    ' ===================================================================
        '    Private Function GetLogPath() As String
        '        Return System.IO.Path.Combine(GetDebugFolder(), "rest_fetch_all.txt")
        '    End Function

        '    Private Sub Log(message As String, Optional level As String = "INFO")
        '        Try
        '            Dim logPath = GetLogPath()
        '            Dim ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
        '            System.IO.File.AppendAllText(logPath, $"[{ts}] [{level}] {message}{vbCrLf}", Encoding.UTF8)
        '        Catch ex As Exception
        '            ' Игнорируем ошибки логирования
        '        End Try
        '    End Sub

        '    Private Sub LogInfo(msg As String)
        '        Log(msg, "INFO")
        '    End Sub

        '    Private Sub LogWarn(msg As String)
        '        Log(msg, "WARN")
        '    End Sub

        '    Private Sub LogError(msg As String, Optional ex As Exception = Nothing)
        '        Dim full = msg
        '        If ex IsNot Nothing Then
        '            full &= $"{vbCrLf}  Exception: {ex.GetType().Name} | {ex.Message}"
        '            If ex.InnerException IsNot Nothing Then
        '                full &= $"{vbCrLf}  Inner: {ex.InnerException.Message}"
        '            End If
        '            full &= $"{vbCrLf}  Stack: {ex.StackTrace}"
        '        End If
        '        Log(full, "ERROR")
        '    End Sub

        '    ' ===================================================================
        '    ' 🔐 АВТОРИЗАЦИЯ (ЖЁСТКАЯ КОДИРОВКА WINDOWS-1251)
        '    ' ===================================================================
        '    Public Async Function LoginAsync(username As String, password As String) As Task(Of Boolean)

        '        Try
        '            LogInfo($"🔐 LoginAsync: Вход для '{username}'")

        '            ' 1. Получаем начальные куки (JSESSIONID)
        '            Await _httpClient.GetAsync($"{_baseUrl}/index.jsp")
        '            LogInfo("📄 index.jsp: загружен для кук")

        '            ' 2. Формируем параметры
        '            'Dim dorVal As String = ""
        '            'If DorKod > 0 Then
        '            '    dorVal = DorKod.ToString()
        '            'End If

        '            ' Возвращаем FormUrlEncodedContent, он стабильнее для этого Tomcat-сервера
        '            Dim content = New FormUrlEncodedContent(New Dictionary(Of String, String) From {
        '                                                    {"id_prog", "47"},
        '                                                    {"action", "full_card.jsp"},
        '                                                    {"dor_user", "100"},
        '                                                    {"login", username},
        '                                                    {"pass", password},
        '                                                    {"save_password", "on"}
        '                                                    })

        '            content.Headers.ContentType = New Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded")
        '            content.Headers.ContentType.CharSet = "windows-1251"

        '            _httpClient.DefaultRequestHeaders.Referrer = New Uri($"{_baseUrl}/index.jsp")
        '            LogInfo($"📤 POST /login: dor_user='100' (фиксированный), login={username}")

        '            ' 3. Отправляем и читаем ответ
        '            Dim resp As HttpResponseMessage = Await _httpClient.PostAsync($"{_baseUrl}/login", content)
        '            Dim htmlBytes As Byte() = Await resp.Content.ReadAsByteArrayAsync()

        '            LogInfo($"📦 Получено байт ответа: {htmlBytes.Length}")

        '            Dim html As String = Encoding.GetEncoding("windows-1251").GetString(htmlBytes)
        '            LogInfo($"📄 Длина HTML: {html.Length}")

        '            ' 4. Жесткая проверка
        '            If html.Length = 0 Then
        '                LogWarn("⚠️ Сервер вернул пустой ответ (Length=0). Проверьте логин/пароль или сеть.")
        '                Return False
        '            End If

        '            If html.Contains("anauth_panel") Then
        '                Dim debugPath As String = System.IO.Path.Combine(GetDebugFolder(), "login_failed.html")
        '                System.IO.File.WriteAllText(debugPath, html, Encoding.UTF8)
        '                LogWarn($"❌ Вход не удался (найден anauth_panel). Ответ сохранён: {debugPath}")
        '                Return False
        '            End If

        '            LogInfo("✅ Вход успешен")
        '            Return True
        '        Catch ex As Exception
        '            LogError("💥 LoginAsync FAILED", ex)
        '            Return False
        '        End Try

        '    End Function

        '    ' ===================================================================
        '    ' 🔥 ИНИЦИАЛИЗАЦИЯ СЕССИИ (БЕЗ journalHeader)
        '    ' ===================================================================
        '    Public Async Function InitSessionAsync() As Task
        '        If _sessionInitialized Then
        '            Return
        '        End If

        '        Try
        '            LogInfo("🔐 InitSessionAsync: начало")
        '            Dim r1 As HttpResponseMessage = Await _httpClient.GetAsync($"{_baseUrl}/index2560R.html")
        '            LogInfo($"📄 index2560R.html: Status={r1.StatusCode}")
        '            r1.EnsureSuccessStatusCode()

        '            _sessionInitialized = True
        '            LogInfo("✅ Сессия инициализирована")
        '        Catch ex As Exception
        '            _sessionInitialized = False
        '            LogError("❌ InitSessionAsync FAILED", ex)
        '        End Try
        '    End Function

        '    ' ===================================================================
        '    ' 🔥 АВТОМАТИЧЕСКОЕ ПОЛУЧЕНИЕ CRIPTFILTERINFO ОТ СЕРВЕРА
        '    ' ===================================================================
        '    Private Async Function GetFreshCriptFilterInfoAsync() As Task(Of String)
        '        Try
        '            LogInfo("🔍 Запрашиваю свежий criptFilterInfo у сервера...")

        '            ' Формируем строку фильтров так, как это делает JavaScript фронтенда
        '            Dim dorVal As String = If(DorKod > 0, DorKod.ToString("00"), "00")

        '            ' Собираем параметры, которые вы реально меняете или используете
        '            Dim filterString As String = $"dorKodPlace={dorVal}"
        '            filterString &= $"&dateStart={DateFrom:dd.MM.yyyy}"
        '            filterString &= $"&dateEnd={DateTo:dd.MM.yyyy}"
        '            filterString &= $"&statusParam={StatusFilter}"

        '            ' Если нужно добавить другие дефолтные фильтры из фильтр.txt, можно дописать их сюда, 
        '            ' но обычно серверу достаточно этих основных для генерации токена.

        '            ' Формируем JSON-запрос, как в скриптах: { reportName, dorVariableParam, criptFilterInfo }
        '            Dim payloadObj = New With {
        '    .reportName = ReportName,
        '    .dorVariableParam = "",
        '    .criptFilterInfo = filterString
        '}

        '            Dim payloadJson As String = JsonSerializer.Serialize(payloadObj)
        '            Dim content As New StringContent(payloadJson, Encoding.UTF8, "application/json")

        '            ' Эндпоинт, который генерирует токен (найден в скриптах)
        '            Dim url As String = $"{_baseUrl}/loadCriptFilter"

        '            LogInfo($"📤 POST {url} | Фильтры: {filterString}")

        '            Dim resp As HttpResponseMessage = Await _httpClient.PostAsync(url, content)
        '            Dim respText As String = Await resp.Content.ReadAsStringAsync()

        '            If Not resp.IsSuccessStatusCode Then
        '                LogWarn($"⚠️ Не удалось получить токен. Статус: {resp.StatusCode}")
        '                Return ""
        '            End If

        '            ' Сервер обычно возвращает объект вида: {"data": "длинная_строка_токена"}
        '            ' Попробуем распарсить. Если придет просто строка, обработаем это.
        '            Dim options As New JsonSerializerOptions() With {.PropertyNameCaseInsensitive = True}

        '            ' Пробуем распарсить как объект с полем data
        '            Dim resultObj = JsonSerializer.Deserialize(Of JsonElement)(respText, options)
        '            If resultObj.TryGetProperty("data", Nothing) Then
        '                Dim token As String = resultObj.GetProperty("data").GetString()
        '                LogInfo($"✅ Успешно получен свежий criptFilterInfo (длина: {token.Length})")
        '                Return token
        '            Else
        '                ' Если вернулся просто массив или строка без обертки "data"
        '                LogInfo($"✅ Получен ответ от loadCriptFilter: {respText.Substring(0, Math.Min(50, respText.Length))}...")
        '                Return respText.Trim(""""c) ' Убираем кавычки, если это просто строка
        '            End If

        '        Catch ex As Exception
        '            LogError("💥 Ошибка при получении criptFilterInfo", ex)
        '            Return ""
        '        End Try
        '    End Function






        '    ' ===================================================================
        '    ' 🔥 ЗАГРУЗКА ОДНОЙ СТРАНИЦЫ (ТОЧНО КАК В БРАУЗЕРЕ)
        '    ' ===================================================================
        '    Private Async Function LoadPageAsync(page As Integer) As Task(Of JournalApiResponse)

        '        Try
        '            Dim currentCript As String = CriptFilterInfo

        '            ' 🔑 Если токен не задан вручную жестко, получаем его свежим запросом
        '            If String.IsNullOrWhiteSpace(currentCript) Then
        '                LogInfo("⚙️ CriptFilterInfo не задан вручную. Генерирую его динамически...")
        '                currentCript = Await GetFreshCriptFilterInfoAsync()

        '                If String.IsNullOrWhiteSpace(currentCript) Then
        '                    LogError("❌ Не удалось получить criptFilterInfo от сервера. Запрос к View будет отклонен.")
        '                    Return Nothing
        '                End If
        '            Else
        '                LogInfo("ℹ️ Использую заданный вручную CriptFilterInfo")
        '            End If

        '            Dim paramsObj = New With {
        '    .reportName = ReportName,
        '    .criptFilterInfo = currentCript,
        '    .activePage = page,
        '    .numberOfElements = PageSize,
        '    .presentation = Presentation,
        '    .reportLevel = ReportLevel
        '}

        '            Dim body As String = JsonSerializer.Serialize(paramsObj)
        '            Dim content As New StringContent(body, Encoding.UTF8, "application/json")
        '            content.Headers.ContentType.CharSet = "utf-8"

        '            Dim url As String = $"{_baseUrl}/version/rest/api/View"

        '            LogInfo($"📤 POST {url} | Page={page}")

        '            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Origin", _baseUrl)
        '            _httpClient.DefaultRequestHeaders.Referrer = New Uri($"{_baseUrl}/index2560R.html")

        '            Dim resp As HttpResponseMessage = Await _httpClient.PostAsync(url, content)
        '            Dim respText As String = Await resp.Content.ReadAsStringAsync()

        '            LogInfo($"📥 Response View: Status={resp.StatusCode}, Len={respText.Length}")

        '            If Not resp.IsSuccessStatusCode Then
        '                LogError($"❌ HTTP {resp.StatusCode}: {respText.Substring(0, Math.Min(200, respText.Length))}")
        '                Return Nothing
        '            End If

        '            Dim options As New JsonSerializerOptions() With {.PropertyNameCaseInsensitive = True}
        '            Dim result As JournalApiResponse = JsonSerializer.Deserialize(Of JournalApiResponse)(respText, options)

        '            If result Is Nothing Then
        '                LogError("❌ Десериализация вернула Nothing")
        '            Else
        '                LogInfo($"✅ Десериализация: Count={If(result.ListValue?.Count, 0)}, Total={result.TotalElements}")
        '            End If

        '            Return result
        '        Catch ex As Exception
        '            LogError($"💥 LoadPage({page}) FAILED", ex)
        '            Return Nothing
        '        End Try

        '    End Function

        '    ' ===================================================================
        '    ' 🔥 ЗАГРУЗКА ВСЕХ СТРАНИЦ
        '    ' ===================================================================
        '    Public Async Function FetchAllAsync(Optional progress As IProgress(Of Integer) = Nothing) As Task(Of List(Of JournalRecordRest))
        '        If Not _sessionInitialized Then
        '            Await InitSessionAsync()
        '        End If

        '        Dim allRecords As New List(Of JournalRecordRest)
        '        Dim seenIds As New HashSet(Of String)
        '        Dim page As Integer = 1

        '        LogInfo($"🚀 START: Dor={DorKod}, {DateFrom:dd.MM.yy}-{DateTo:dd.MM.yy}, pageSize={PageSize}")

        '        Try
        '            Dim first As JournalApiResponse = Await LoadPageAsync(1)
        '            If first Is Nothing Then
        '                LogError("❌ Не удалось загрузить первую страницу")
        '                Return allRecords
        '            End If

        '            Dim total As Integer = first.TotalElements
        '            Dim pages As Integer = CInt(Math.Ceiling(total / PageSize))
        '            If pages = 0 AndAlso total > 0 Then
        '                pages = 1
        '            End If
        '            LogInfo($"📊 totalElements={total}, totalPages={pages}")

        '            For Each rec As JournalRecordRest In first.ListValue
        '                If seenIds.Add(rec.ViolId) Then
        '                    allRecords.Add(rec)
        '                End If
        '            Next
        '            If progress IsNot Nothing Then
        '                progress.Report(allRecords.Count)
        '            End If
        '            LogInfo($"➕ Стр.1: +{first.ListValue.Count}, Всего: {allRecords.Count}")

        '            page = 2
        '            While page <= pages
        '                LogInfo($"📥 Страница {page}/{pages}...")
        '                Dim resp As JournalApiResponse = Await LoadPageAsync(page)
        '                If resp Is Nothing OrElse resp.ListValue Is Nothing Then
        '                    LogWarn($"⚠ Пустой ответ на стр.{page}")
        '                    page += 1
        '                    Await Task.Delay(100)
        '                    Continue While
        '                End If

        '                Dim added As Integer = 0
        '                For Each rec As JournalRecordRest In resp.ListValue
        '                    If seenIds.Add(rec.ViolId) Then
        '                        allRecords.Add(rec)
        '                        added += 1
        '                    End If
        '                Next
        '                LogInfo($"✅ Стр.{page}: +{added} | Всего: {allRecords.Count}")
        '                If progress IsNot Nothing Then
        '                    progress.Report(allRecords.Count)
        '                End If
        '                Await Task.Delay(100)
        '                page += 1
        '            End While
        '        Catch ex As Exception
        '            LogError($"💥 КРИТИЧЕСКАЯ ОШИБКА в FetchAllAsync", ex)
        '        End Try

        '        LogInfo($"🎉 FINISH: Собрано {allRecords.Count} записей")
        '        Return allRecords
        '    End Function

        '    ' ===================================================================
        '    ' 🔧 КОНВЕРТЕР (если нужен старый JournalRecord)
        '    ' ===================================================================
        '    Public Function ConvertToJournalRecord(restRecord As JournalRecordRest) As JournalRecord
        '        If restRecord Is Nothing Then
        '            Return Nothing
        '        End If

        '        Dim target As New JournalRecord()
        '        target.Category = CleanHtml(restRecord.Category)
        '        target.ViolId = restRecord.ViolId
        '        target.StartTime = CleanHtml(restRecord.DateFrom)
        '        target.EndTime = CleanHtml(restRecord.DateTo)
        '        target.Location = CleanHtml(restRecord.PlaceInfo)
        '        target.FromDept = CleanHtml(restRecord.Investigation)
        '        target.ToDept = CleanHtml(restRecord.Guilty)
        '        target.Equipment = CleanHtml(restRecord.ReasonObjectTop)
        '        Return target
        '    End Function

        '    Private Function CleanHtml(text As String) As String
        '        If String.IsNullOrWhiteSpace(text) Then
        '            Return ""
        '        End If
        '        text = Regex.Replace(text, "<[^>]+>", " ")
        '        text = text.Replace("&nbsp;", " ").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&")
        '        text = Regex.Replace(text, "\s+", " ")
        '        Return text.Trim()
        '    End Function

        'End Class

        '=======================================================================
        ' ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ
        '=======================================================================
        'Private Function GetDebugFolder() As String
        '    Dim path As String = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "KasAntDebug")
        '    Try
        '        If Not System.IO.Directory.Exists(path) Then
        '            System.IO.Directory.CreateDirectory(path)
        '        End If
        '    Catch ex As Exception
        '        ' Игнорируем
        '    End Try
        '    Return path
        'End Function

    End Module

End Namespace
