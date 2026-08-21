Imports System.ComponentModel
Imports System.IO
Imports System.Net
Imports System.Net.Http
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices.JavaScript.JSType
Imports System.Text
Imports System.Text.Json
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports HtmlAgilityPack

Namespace Kas


    Public Module KasReport341Module

        ' Глобальный экземпляр загрузчика для центральной сессии
        Public CentralFetcher As New Report341Fetcher()

#Region "Внешние классы"

        '' =====================================================================
        '' Класс для хранения результата парсинга по одному депо
        '' =====================================================================
        'Public Class Report341Result
        '    Public Property DepotName As String
        '    Public Property TotalCount As Integer
        '    Public Property AcceptedCount As Integer
        '    Public Property OverdueCount As Integer
        '    Public Property InvestigatedCount As Integer
        'End Class

        ' =====================================================================
        ' Класс для описания периода запроса
        ' =====================================================================
        Public Class ReportPeriod
            Public Property DateFrom As Date
            Public Property DateTo As Date
            Public Property Title As String
            Public Property IsCurrentYear As Boolean
            Public Property PeriodNumber As Integer
        End Class

        ' =====================================================================
        ' Класс для строки итоговой таблицы (5 строк по депо)
        ' =====================================================================
        Public Class DepotReportRow
            Public Property DepotName As String
            Public Property Period1Current As Integer?   ' Текущий год, период 1
            Public Property Period1PrevYear As Integer?  ' Прошлый год, период 1
            Public Property Period2Current As Integer?   ' Текущий год, период 2 (если день < 15)
            Public Property Period2PrevYear As Integer?  ' Прошлый год, период 2 (если день < 15)

            ' Новое свойство для выделения итоговой строки
            Public Property IsTotalRow As Boolean = False
        End Class


#End Region




        ' =====================================================================
        ' Класс-загрузчик отчёта 3.4.1 с собственной изолированной авторизацией
        ' =====================================================================
        Public Class Report341Fetcher
            Implements INotifyPropertyChanged

            Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
            Public Event CentralConnectionChanged()

            Private ReadOnly _httpClient As HttpClient
            Private ReadOnly _cookieContainer As New CookieContainer()
            Public ReadOnly _baseUrl As String = "http://kasant.gvc.oao.rzd:8888/kasant"

            Private _isConnected As Boolean = False
            Public Property IsConnected As Boolean
                Get
                    Return _isConnected
                End Get
                Private Set(value As Boolean)
                    If _isConnected <> value Then
                        _isConnected = value
                        RaisePropertyChanged(NameOf(IsConnected))
                    End If
                End Set
            End Property

            Public ReadOnly Property HttpClient As HttpClient
                Get
                    Return _httpClient
                End Get
            End Property

            Public Sub New()
                Dim handler As New HttpClientHandler()
                handler.CookieContainer = _cookieContainer
                handler.AllowAutoRedirect = True  ' ← КЛЮЧЕВОЕ: как в рабочем модуле!
                handler.UseCookies = True

                _httpClient = New HttpClient(handler)
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.216 YaBrowser/21.5.4.610 Yowser/2.5 Safari/537.36")
                _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")
                _httpClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9,en;q=0.8")
                _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive")
                _httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1")

                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
            End Sub

            Protected Sub RaisePropertyChanged(<CallerMemberName> Optional propertyName As String = Nothing)
                RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
            End Sub

            Private ReadOnly _logLock As New Object()

            Public Sub LogWrite(msg As String)
                Try
                    Dim debugFolder = GetDebugFolder()
                    Dim logPath = Path.Combine(debugFolder, "r341_log.txt")
                    SyncLock _logLock
                        File.AppendAllText(logPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {msg}{vbCrLf}",
                    Encoding.UTF8)
                    End SyncLock
                Catch
                End Try
            End Sub

            Private Function UrlEncodeWin1251(text As String) As String
                Dim encoding As Encoding = Encoding.GetEncoding("windows-1251")
                Dim bytes As Byte() = encoding.GetBytes(text)
                Dim sb As New StringBuilder()
                For Each b In bytes
                    If (b >= &H30 AndAlso b <= &H39) OrElse
                       (b >= &H41 AndAlso b <= &H5A) OrElse
                       (b >= &H61 AndAlso b <= &H7A) OrElse
                       b = &H2D OrElse b = &H5F OrElse b = &H2E OrElse b = &H7E Then
                        sb.Append(Chr(b))
                    Else
                        sb.Append("%" & b.ToString("X2"))
                    End If
                Next
                Return sb.ToString()
            End Function

            ' =================================================================
            ' АВТОРИЗАЦИЯ (ПОЛНОСТЬЮ КАК В РАБОЧЕМ МОДУЛЕ)
            ' =================================================================
            Public Async Function LoginAsync() As Task(Of Boolean)


                Dim user = My.Settings.CentralLogin

                ' 🔑 РАСШИФРОВЫВАЕМ ПАРОЛЬ перед отправкой на сервер
                Dim pass = DecryptPassword(My.Settings.CentralPassword)

                If String.IsNullOrWhiteSpace(user) OrElse String.IsNullOrWhiteSpace(pass) Then
                    LogWrite("⚠ Не заданы CentralLogin / CentralPassword в настройках")
                    IsConnected = False
                    RaiseEvent CentralConnectionChanged()
                    Return False
                End If

                Try
                    Dim debugFolder = GetDebugFolder()

                    ' 1. Загружаем главную страницу
                    LogWrite($"🔄 Начало авторизации пользователя: {user}")
                    Dim mainPageResponse = Await _httpClient.GetAsync($"{_baseUrl}/index.jsp")
                    Dim mainPageBytes = Await mainPageResponse.Content.ReadAsByteArrayAsync()
                    Dim mainPageHtml = Encoding.GetEncoding("windows-1251").GetString(mainPageBytes)
                    File.WriteAllText(Path.Combine(debugFolder, "r341_debug_main.html"), mainPageHtml, Encoding.GetEncoding("windows-1251"))

                    ' 2. Формируем запрос с РАСШИФРОВАННЫМ паролем
                    Dim content = New FormUrlEncodedContent(New Dictionary(Of String, String) From
        {
            {"id_prog", "47"},
            {"action", "full_card.jsp"},
            {"dor_user", "100"},          ' ← Центральный уровень
            {"login", user},
            {"pass", pass},               ' ← ТЕПЕРЬ ЗДЕСЬ НАСТОЯЩИЙ ПАРОЛЬ
            {"save_password", "on"}
        })

                    content.Headers.ContentType = New System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded")
                    content.Headers.ContentType.CharSet = "windows-1251"

                    _httpClient.DefaultRequestHeaders.Referrer = New Uri($"{_baseUrl}/index.jsp")
                    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Origin", _baseUrl)

                    ' Для отладки — смотрим, что реально уходит
                    Dim rawBody = Await content.ReadAsStringAsync()
                    LogWrite($"📤 RAW POST BODY: {rawBody}")

                    Dim response = Await _httpClient.PostAsync($"{_baseUrl}/login", content)

                    ' 3. Читаем ответ
                    Dim loginBytes = Await response.Content.ReadAsByteArrayAsync()
                    Dim resultHtml = Encoding.GetEncoding("windows-1251").GetString(loginBytes)
                    File.WriteAllText(Path.Combine(debugFolder, "r341_debug_login.html"), resultHtml, Encoding.GetEncoding("windows-1251"))
                    LogWrite($"📡 Ответ: статус {CInt(response.StatusCode)}, размер {loginBytes.Length} байт")

                    ' 4. Если тело пустое — делаем повторный GET
                    If String.IsNullOrWhiteSpace(resultHtml) OrElse loginBytes.Length < 100 Then
                        LogWrite("⚠ Пустое тело ответа, делаем повторный GET на index.jsp...")
                        Dim followUpResponse = Await _httpClient.GetAsync($"{_baseUrl}/index.jsp")
                        Dim followUpBytes = Await followUpResponse.Content.ReadAsByteArrayAsync()
                        resultHtml = Encoding.GetEncoding("windows-1251").GetString(followUpBytes)
                        File.WriteAllText(Path.Combine(debugFolder, "r341_debug_login_followup.html"), resultHtml, Encoding.GetEncoding("windows-1251"))
                        LogWrite($"📡 Повторный GET: {followUpBytes.Length} байт")
                    End If

                    ' 5. Проверка маркеров авторизации
                    Dim isLogged = Not resultHtml.Contains("anauth_panel") AndAlso
                                   (resultHtml.Contains("Журналы") OrElse
                                    resultHtml.Contains("Отчёты") OrElse
                                    resultHtml.Contains("session_invalidate") OrElse
                                    resultHtml.Contains("Смена пользователя") OrElse
                                    resultHtml.Contains("id_user"))

                    If isLogged Then
                        LogWrite("✅ ВХОД УСПЕШЕН (Central)")
                        IsConnected = True
                        RaiseEvent CentralConnectionChanged()
                    Else
                        LogWrite("❌ ВХОД НЕ УДАЛСЯ (Central)")
                        IsConnected = False
                        RaiseEvent CentralConnectionChanged()
                    End If


                    Return isLogged

                Catch ex As Exception
                    LogWrite($"💥 Ошибка входа (Central): {ex.Message}{vbCrLf}{ex.StackTrace}")
                    IsConnected = False
                    'RaiseEvent CentralConnectionChanged()
                    Return False
                End Try

            End Function

            ' =================================================================
            ' ПРОВЕРКА СЕССИИ (КАК В РАБОЧЕМ МОДУЛЕ)
            ' =================================================================
            Public Async Function IsLoggedInAsync() As Task(Of Boolean)
                Try
                    Dim response = Await _httpClient.GetAsync($"{_baseUrl}/index.jsp")
                    Dim bytes = Await response.Content.ReadAsByteArrayAsync()
                    Dim html = Encoding.GetEncoding("windows-1251").GetString(bytes)

                    'Dim result = response.IsSuccessStatusCode AndAlso
                    '     Not html.Contains("anauth_panel") AndAlso
                    '     html.Contains("Журналы")
                    ' Проверяем реальные маркеры, которые есть в твоем HTML файле
                    Dim result = response.IsSuccessStatusCode AndAlso
                     Not html.Contains("anauth_panel") AndAlso
                     (html.Contains("Смена пользователя") OrElse html.Contains("session_invalidate"))

                    IsConnected = result

                    Return result
                Catch ex As Exception
                    LogWrite($"⚠ Ошибка проверки сессии: {ex.Message}")
                    IsConnected = False
                    Return False
                End Try
            End Function

            Public Async Function EnsureConnectedAsync() As Task(Of Boolean)
                If Await IsLoggedInAsync() Then
                    LogWrite("✓ Сессия уже активна")
                    Return True
                End If
                LogWrite("🔄 Сессия неактивна, выполняем вход...")
                Return Await LoginAsync()
            End Function

            ' =================================================================
            ' Формирует URL для отчёта 3.4.1
            ' =================================================================
            Public Function BuildReportUrl(dateFrom As DateTime, dateTo As DateTime,
                                   startHour As Integer, endHour As Integer,
                                   Optional endMin As Integer = 59) As String
                Dim tmpUnik = Math.Floor((DateTime.UtcNow - New DateTime(1970, 1, 1)).TotalMilliseconds)

                Dim url = $"{_baseUrl}/reports/new/Report3_4_1?page=reports/new/Report3_4_1&tmp_unik={tmpUnik}" &
               $"&dt_nd={dateFrom:dd.MM.yyyy}&dt_nd_h={startHour:00}&dt_nd_min=00" &
               $"&dt_kd={dateTo:dd.MM.yyyy}&dt_kd_h={endHour:00}&dt_kd_min={endMin:00}" &
               "&rep_asu=0,1,2,5,7,11,12,14,15,20,21,26,26&rep_asu_dop=1:1" &
               "&addsls=172403,150301,150312,150313,702010,172470,172471,150321" &
               "&kind_rep_type=1&flg_alien=-1&flg_alien_service=3&rep_cause_alien=174782&usw=1929&ush=1123"

                LogWrite($"🔗 Сформирован URL: {url}")
                Return url
            End Function

            ' =================================================================
            ' ЛОГИКА ПЕРИОДОВ
            ' =================================================================
            Public Function GetReportPeriods() As List(Of ReportPeriod)
                Dim today = Date.Today
                Dim periods As New List(Of ReportPeriod)

                If today.Day < 15 Then
                    Dim firstDayPrevMonth = New Date(today.Year, today.Month, 1).AddMonths(-1)
                    Dim lastDayPrevMonth = firstDayPrevMonth.AddMonths(1).AddDays(-1)

                    periods.Add(New ReportPeriod With {
                        .DateFrom = firstDayPrevMonth, .DateTo = lastDayPrevMonth,
                        .Title = $"1–{lastDayPrevMonth.Day} {firstDayPrevMonth:MMMM}",
                        .IsCurrentYear = True, .PeriodNumber = 1
                    })
                    periods.Add(New ReportPeriod With {
                        .DateFrom = firstDayPrevMonth.AddYears(-1), .DateTo = lastDayPrevMonth.AddYears(-1),
                        .Title = $"1–{lastDayPrevMonth.Day} {firstDayPrevMonth:MMMM}",
                        .IsCurrentYear = False, .PeriodNumber = 1
                    })

                    Dim firstDayCurMonth = New Date(today.Year, today.Month, 1)
                    periods.Add(New ReportPeriod With {
                        .DateFrom = firstDayCurMonth, .DateTo = today,
                        .Title = $"1–{today.Day} {today:MMMM}",
                        .IsCurrentYear = True, .PeriodNumber = 2
                    })

                    Dim dayPrevYear = Math.Min(today.Day, DateTime.DaysInMonth(today.Year - 1, today.Month))
                    periods.Add(New ReportPeriod With {
                        .DateFrom = New Date(today.Year - 1, today.Month, 1),
                        .DateTo = New Date(today.Year - 1, today.Month, dayPrevYear),
                        .Title = $"1–{dayPrevYear} {today:MMMM}",
                        .IsCurrentYear = False, .PeriodNumber = 2
                    })
                Else
                    Dim firstDayCurMonth = New Date(today.Year, today.Month, 1)
                    periods.Add(New ReportPeriod With {
                        .DateFrom = firstDayCurMonth, .DateTo = today,
                        .Title = $"1–{today.Day} {today:MMMM}",
                        .IsCurrentYear = True, .PeriodNumber = 1
                    })

                    Dim dayPrevYear = Math.Min(today.Day, DateTime.DaysInMonth(today.Year - 1, today.Month))
                    periods.Add(New ReportPeriod With {
                        .DateFrom = New Date(today.Year - 1, today.Month, 1),
                        .DateTo = New Date(today.Year - 1, today.Month, dayPrevYear),
                        .Title = $"1–{dayPrevYear} {today:MMMM}",
                        .IsCurrentYear = False, .PeriodNumber = 1
                    })
                End If

                LogWrite($"📅 Сформировано периодов: {periods.Count}")
                Return periods
            End Function

            ' =================================================================
            ' БЫСТРЫЙ ПАРСИНГ — только название депо и колонка "Всего"
            ' =================================================================
            Public Async Function FetchDepotTotalsAsync(url As String) As Task(Of Dictionary(Of String, Integer))
                Dim result As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
                Dim debugFolder = GetDebugFolder()

                Try
                    LogWrite($"📥 Загрузка отчёта...")
                    Dim response = Await _httpClient.GetAsync(url)
                    response.EnsureSuccessStatusCode()

                    Dim bytes = Await response.Content.ReadAsByteArrayAsync()
                    Dim html = Encoding.GetEncoding("windows-1251").GetString(bytes)

                    File.WriteAllText(Path.Combine(debugFolder, "r341_report.html"), html, Encoding.GetEncoding("windows-1251"))
                    LogWrite($"📄 Получено {bytes.Length} байт")

                    If html.Contains("anauth_panel") OrElse html.Contains("id_prog") Then
                        LogWrite("⚠ Сессия протухла при загрузке отчёта")
                        Return result
                    End If

                    Dim doc As New HtmlAgilityPack.HtmlDocument()
                    doc.LoadHtml(html)

                    Dim rows = doc.DocumentNode.SelectNodes("//tr")
                    If rows Is Nothing Then
                        LogWrite("⚠ В HTML не найдены строки <tr>")
                        Return result
                    End If

                    Dim parsedCount As Integer = 0
                    For Each row In rows
                        Dim cells = row.SelectNodes("./td")
                        If cells IsNot Nothing AndAlso cells.Count >= 2 Then
                            Dim depotName = CleanText(cells(0).InnerText)

                            If String.IsNullOrWhiteSpace(depotName) OrElse
                               depotName.Contains("Наименование структурного") OrElse
                               depotName.Contains("Отчёт о состоянии") OrElse
                               depotName.ToUpper() = "ВСЕГО" Then
                                Continue For
                            End If

                            Dim totalCount As Integer = 0
                            Integer.TryParse(CleanText(cells(1).InnerText), totalCount)
                            result(depotName) = totalCount
                            parsedCount += 1
                        End If
                    Next

                    LogWrite($"✓ Распарсено записей в отчёте: {parsedCount}")

                Catch ex As Exception
                    LogWrite($"💥 Ошибка загрузки отчёта: {ex.Message}{vbCrLf}{ex.StackTrace}")
                End Try

                Return result
            End Function

            Private Function FindDepotValue(dict As Dictionary(Of String, Integer), keywords As String()) As Integer?
                For Each kvp In dict
                    For Each kw In keywords
                        If kvp.Key.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0 Then
                            Return kvp.Value
                        End If
                    Next
                Next
                Return Nothing
            End Function

            ' =================================================================
            ' ГЛАВНЫЙ МЕТОД
            ' =================================================================
            Public Async Function ShowDepotReportAsync() As Task
                Try
                    LogWrite("========== НАЧАЛО ФОРМИРОВАНИЯ ОТЧЁТА ==========")

                    If Not Await EnsureConnectedAsync() Then
                        LogWrite("❌ Не удалось подключиться к КАСАНТ — отчёт прерван")
                        Return
                    End If

                    Dim periods = GetReportPeriods()

                    Dim targetDepots As New List(Of (DisplayName As String, Keywords As String())) From {
                        ("СЛД Боготол", {"Богото"}),
                        ("СЛД Красноярск", {"Красноярс"}),
                        ("СЛД Конск-Иланский", {"Иланск"}),
                        ("СЛД Ачинск", {"Ачинс"}),
                        ("СЛД Абакан", {"Абака"})
                    }

                    Dim allResults As New Dictionary(Of String, Dictionary(Of String, Integer))

                    For Each period In periods
                        LogWrite($"📊 Запрос: {period.Title}...")
                        Dim url = BuildReportUrl(period.DateFrom, period.DateTo, 0, 23, 59)
                        Dim data = Await FetchDepotTotalsAsync(url)
                        allResults($"{period.PeriodNumber}_{If(period.IsCurrentYear, "cur", "prev")}") = data
                        Await Task.Delay(500)
                    Next

                    Dim rows As New List(Of DepotReportRow)
                    For Each depot In targetDepots
                        Dim row As New DepotReportRow With {.DepotName = depot.DisplayName}

                        If allResults.ContainsKey("1_cur") Then
                            row.Period1Current = FindDepotValue(allResults("1_cur"), depot.Keywords)
                        End If
                        If allResults.ContainsKey("1_prev") Then
                            row.Period1PrevYear = FindDepotValue(allResults("1_prev"), depot.Keywords)
                        End If
                        If allResults.ContainsKey("2_cur") Then
                            row.Period2Current = FindDepotValue(allResults("2_cur"), depot.Keywords)
                        End If
                        If allResults.ContainsKey("2_prev") Then
                            row.Period2PrevYear = FindDepotValue(allResults("2_prev"), depot.Keywords)
                        End If

                        LogWrite($"🏭 {depot.DisplayName}: P1cur={row.Period1Current}, P1prev={row.Period1PrevYear}, P2cur={row.Period2Current}, P2prev={row.Period2PrevYear}")
                        rows.Add(row)
                    Next


                    ' ========== ДОБАВЛЯЕМ ИТОГОВУЮ СТРОКУ ==========
                    Dim totalRow As New DepotReportRow With {
            .DepotName = "ИТОГО",
            .IsTotalRow = True,
            .Period1Current = rows.Sum(Function(r) If(r.Period1Current, 0)),
            .Period1PrevYear = rows.Sum(Function(r) If(r.Period1PrevYear, 0)),
            .Period2Current = rows.Sum(Function(r) If(r.Period2Current, 0)),
            .Period2PrevYear = rows.Sum(Function(r) If(r.Period2PrevYear, 0))
        }
                    rows.Add(totalRow)

                    LogWrite($"📊 ИТОГО: P1cur={totalRow.Period1Current}, P1prev={totalRow.Period1PrevYear}, P2cur={totalRow.Period2Current}, P2prev={totalRow.Period2PrevYear}")
                    ' =================================================


                    Dim hasPeriod2 = periods.Any(Function(p) p.PeriodNumber = 2)
                    Dim period1 = periods.FirstOrDefault(Function(p) p.PeriodNumber = 1 AndAlso p.IsCurrentYear)
                    Dim period2 = periods.FirstOrDefault(Function(p) p.PeriodNumber = 2 AndAlso p.IsCurrentYear)

                    Dim period1Title = If(period1 IsNot Nothing, period1.Title, "Период 1")
                    Dim period2Title As String = Nothing
                    If hasPeriod2 AndAlso period2 IsNot Nothing Then
                        period2Title = period2.Title
                    End If

                    ShowDepotReportPopup(rows, period1Title, period2Title)

                    LogWrite("✅ Отчет по депо сформирован и показан")
                    LogWrite("========== КОНЕЦ ФОРМИРОВАНИЯ ОТЧЁТА ==========")

                Catch ex As Exception
                    LogWrite($"💥 Критическая ошибка: {ex.Message}{vbCrLf}{ex.StackTrace}")
                End Try
            End Function

            Public Sub ShowDepotReportPopup(rows As List(Of DepotReportRow),
                                     period1Title As String,
                                     Optional period2Title As String = Nothing)
                Dim win As New Window() With {
                    .Title = "Отчет по СЛД — КАСАНТ",
                    .Owner = MW,
                    .WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    .SizeToContent = SizeToContent.WidthAndHeight
                    }

                Dim appBackBrush = TryCast(Application.Current.TryFindResource("AppBackBrush"), SolidColorBrush)
                If appBackBrush IsNot Nothing Then win.Background = appBackBrush

                Dim viewer As New DepotReportViewerControl()
                viewer.LoadData(rows, period1Title, period2Title)

                win.Content = viewer
                win.Show()
            End Sub

            Private Function CleanText(text As String) As String
                If String.IsNullOrWhiteSpace(text) Then Return ""
                text = Regex.Replace(text, "<br\s*/?>", " ")
                text = Regex.Replace(text, "<[^>]+>", " ")
                text = text.Replace("&nbsp;", " ").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&")
                text = Regex.Replace(text, "\s+", " ")
                Return text.Trim()
            End Function

            Public Function GetDebugFolder() As String
                Dim desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                Dim debugFolder = Path.Combine(desktopPath, "KasAntDebug")
                Try
                    If Not Directory.Exists(debugFolder) Then Directory.CreateDirectory(debugFolder)
                Catch
                    debugFolder = desktopPath
                End Try
                Return debugFolder
            End Function






            ' =================================================================
            ' ШАГ 1: Получение базового зашифрованного токена через loadCriptFilter
            ' =================================================================
            Private Async Function GetBaseCriptFilterAsync(dateFrom As DateTime, dateTo As DateTime) As Task(Of String)
                Try
                    If Not Await EnsureConnectedAsync() Then
                        LogWrite("❌ Нет сессии")
                        Return Nothing
                    End If

                    Dim filterParams = $"typeOfData=2&" &
                          $"dorKodPlace=0&" &
                          $"block=0&" &
                          $"kindParam=1%2C2%2C3&" &
                          $"statusParam=0%2C1%2C2%2C6%2C7%2C10&" &
                          $"dangerousParam=1&" &
                          $"asuParam=0%2C1%2C5%2C11%2C15%2C20%2C21%2C26&" &
                          $"firstTrainParam=1&" &
                          $"diagParam=1&" &
                          $"humanParam=0&" &
                          $"rulesParam=0&" &
                          $"dateStart={dateFrom:dd.MM.yyyy}&" &
                          $"dateEnd={dateTo:dd.MM.yyyy}&" &
                          $"fromHours=00&" &
                          $"toHours=23&" &
                          $"fromMinutes=00&" &
                          $"toMinutes=59&" &
                          $"typePeriod=1"

                    ' Безопасное создание JSON через анонимный тип, чтобы избежать проблем с кавычками
                    Dim payload As New With {
                        .reportName = "ReportCentral27553",
                        .dorVariableParam = "",
                        .criptFilterInfo = filterParams
                    }
                    Dim jsonBody = JsonSerializer.Serialize(payload)

                    Dim request As New System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, $"{_baseUrl}/version/rest/api/loadCriptFilter")
                    request.Content = New StringContent(jsonBody, Encoding.UTF8, "application/json")
                    request.Headers.Add("Accept", "application/json, text/plain, */*")
                    request.Headers.Add("Origin", _baseUrl)
                    request.Headers.Referrer = New Uri($"{_baseUrl}/index2560R.html")

                    LogWrite($"🔐 Запрос базового токена ({dateFrom:dd.MM.yyyy} - {dateTo:dd.MM.yyyy})")

                    Dim response = Await _httpClient.SendAsync(request)
                    response.EnsureSuccessStatusCode()

                    Dim responseBytes = Await response.Content.ReadAsByteArrayAsync()
                    Dim responseText = Encoding.GetEncoding("windows-1251").GetString(responseBytes)
                    Dim token = responseText.Trim(""""c)

                    If String.IsNullOrWhiteSpace(token) OrElse token.Length < 50 Then
                        LogWrite($"⚠ Подозрительный ответ базового токена: {responseText}")
                        Return Nothing
                    End If

                    LogWrite($"✓ Получен базовый токен: {token.Substring(0, Math.Min(50, token.Length))}...")
                    Return token

                Catch ex As Exception
                    LogWrite($"💥 Ошибка в GetBaseCriptFilterAsync: {ex.Message}")
                    Return Nothing
                End Try



            End Function






            ' =================================================================
            ' ШАГ 2: Получение токена строки через /api/View (как в браузере!)
            ' =================================================================
            Public Async Function GetLocomotiveComplexTokenAsync(dateFrom As DateTime, dateTo As DateTime) As Task(Of String)

                Try
                    LogWrite("🔍 Начало получения токена строки...")

                    ' 1. Получаем базовый токен
                    Dim baseToken = Await GetBaseCriptFilterAsync(dateFrom, dateTo)
                    If String.IsNullOrWhiteSpace(baseToken) Then
                        LogWrite("❌ Не удалось получить базовый токен")
                        Return Nothing
                    End If

                    ' 2. Отправляем базовый токен на /api/View с presentation:"html"
                    Dim payload As New With {
                    .reportName = "ReportCentral27553",
                    .criptFilterInfo = baseToken,
                    .presentation = "html",
                    .reportLevel = "center"
                    }

                    Dim jsonBody = JsonSerializer.Serialize(payload)

                    Dim request As New System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, $"{_baseUrl}/version/rest/api/View")
                    request.Content = New StringContent(jsonBody, Encoding.UTF8, "application/json")
                    request.Headers.Add("Accept", "application/json, text/plain, */*")
                    request.Headers.Add("Origin", _baseUrl)
                    request.Headers.Referrer = New Uri($"{_baseUrl}/index2560R.html")

                    LogWrite($"📄 POST /api/View (presentation: html)...")
                    Dim response = Await _httpClient.SendAsync(request)

                    If Not response.IsSuccessStatusCode Then
                        LogWrite($"⚠ Ошибка /api/View: {response.StatusCode}")
                        Return Nothing
                    End If

                    Dim htmlBytes = Await response.Content.ReadAsByteArrayAsync()
                    Dim html = Encoding.UTF8.GetString(htmlBytes)

                    LogWrite($"📄 Получено HTML: {htmlBytes.Length} байт")

                    ' Сохраняем HTML для отладки
                    Dim htmlPath = Path.Combine(GetDebugFolder(), "r341_page2_debug.html")
                    File.WriteAllText(htmlPath, html, Encoding.UTF8)
                    LogWrite($"💾 HTML сохранён в r341_page2_debug.html")

                    ' 3. 🔑 Ищем в СОХРАНЁННОМ файле как в тексте (всё в одну строку!)
                    Dim fileHtml = File.ReadAllText(htmlPath, Encoding.UTF8)

                    ' Ищем фразу
                    Dim searchText = "локомотивному комплексу"
                    Dim locoIndex = fileHtml.IndexOf(searchText, StringComparison.OrdinalIgnoreCase)

                    If locoIndex = -1 Then
                        LogWrite($"⚠ Фраза '{searchText}' не найдена")
                        Return Nothing
                    End If

                    LogWrite($"✓ Фраза '{searchText}' найдена на позиции {locoIndex}")

                    ' 4. Ищем ПЕРВЫЙ и ВТОРОЙ href ПОСЛЕ найденной фразы
                    Dim hrefPattern As New Regex("inner/DetailReportInfo2755/([^""'/]+)/CENTER", RegexOptions.IgnoreCase)
                    Dim matches = hrefPattern.Matches(fileHtml, locoIndex)

                    If matches.Count = 0 Then
                        LogWrite("⚠ href не найдены после фразы")
                        Return Nothing
                    End If

                    ' Первый токен (прошлый год) - сохраняем на будущее
                    Dim firstToken = matches(0).Groups(1).Value
                    LogWrite($"📌 Токен ПРОШЛОГО ГОДА (индекс 0): {firstToken.Substring(0, Math.Min(40, firstToken.Length))}...")

                    ' Второй токен (текущий год) - нужный
                    If matches.Count < 2 Then
                        LogWrite($"⚠ Нужно минимум 2 href, найдено {matches.Count}")
                        Return Nothing
                    End If

                    Dim rowToken = matches(1).Groups(1).Value
                    LogWrite($"🎯 Токен ТЕКУЩЕГО ГОДА (индекс 1): {rowToken.Substring(0, Math.Min(40, rowToken.Length))}...")

                    Return rowToken

                Catch ex As Exception
                    LogWrite($"💥 Ошибка в GetLocomotiveComplexTokenAsync: {ex.Message}{vbCrLf}{ex.StackTrace}")
                    Return Nothing
                End Try
            End Function


            ' =================================================================
            ' ШАГ 3: СКАЧИВАНИЕ EXCEL
            ' =================================================================
            Public Async Function DownloadGenReportAsync(dateFrom As DateTime, dateTo As DateTime) As Task(Of String)
                Try
                    ' Проверяем наличие активного подключения/сессии перед запросом
                    If Not Await EnsureConnectedAsync() Then
                        LogWrite("❌ Нет сессии")
                        Return Nothing
                    End If

                    ' Шаг 3.1: Вызываем метод парсинга HTML для получения токена строки "ЛОКОМОТИВНЫЙ КОМПЛЕКС"
                    Dim rowToken = Await GetLocomotiveComplexTokenAsync(dateFrom, dateTo)
                    If String.IsNullOrWhiteSpace(rowToken) Then
                        LogWrite("❌ Не удалось получить токен строки")
                        Return Nothing
                    End If

                    ' Шаг 3.2: Формируем JSON-payload для отправки на сервер
                    Dim payload As New With {
                        .reportName = "DetailReportInfo2755",
                        .criptFilterInfo = rowToken,
                        .presentation = "excel",
                        .reportLevel = "CENTER"
                    }
                    Dim jsonBody = JsonSerializer.Serialize(payload)

                    ' Шаг 3.3: Создаем HTTP POST запрос к эндпоинту экспорта в Excel
                    Dim request As New System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, $"{_baseUrl}/version/rest/api/Excel")

                    ' 🔑 КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Контент-тип отправляемых данных должен быть "application/json"
                    request.Content = New StringContent(jsonBody, Encoding.UTF8, "application/json")

                    ' Настраиваем заголовки: сообщаем серверу, что ожидаем в ответ файл Excel
                    request.Headers.Add("Accept", "application/vnd.ms-excel, application/octet-stream, */*")
                    request.Headers.Add("Origin", _baseUrl)
                    request.Headers.Referrer = New Uri($"{_baseUrl}/index2560R.html")

                    LogWrite($"📥 POST /api/Excel (период: {dateFrom:dd.MM.yyyy} - {dateTo:dd.MM.yyyy})")

                    ' Шаг 3.4: Отправляем запрос асинхронно
                    Dim response = Await _httpClient.SendAsync(request)

                    ' Проверяем успешность HTTP-статуса (200 OK)
                    If Not response.IsSuccessStatusCode Then
                        Dim errText = Await response.Content.ReadAsStringAsync()
                        LogWrite($"⚠ Ошибка HTTP /api/Excel: {response.StatusCode}. Ответ сервера: {errText}")
                        Return Nothing
                    End If

                    ' Шаг 3.5: Читаем бинарный ответ от сервера
                    Dim excelBytes = Await response.Content.ReadAsByteArrayAsync()
                    LogWrite($"📦 Получено байт с сервера: {excelBytes.Length}")

                    ' Проверка на слишком маленький размер (обычно означает, что вместо файла пришел JSON с ошибкой)
                    If excelBytes.Length < 1000 Then
                        LogWrite($"⚠ Файл слишком маленький: {excelBytes.Length} байт. Возможно, сервер вернул текстовую ошибку.")
                        Try
                            Dim errorSnapshot = Encoding.UTF8.GetString(excelBytes.Take(250).ToArray())
                            LogWrite($"Контекст ответа: {errorSnapshot}")
                        Catch
                        End Try
                        Return Nothing
                    End If



                    ' Шаг 3.6: Формируем имя файла и сохраняем его на диск
                    Dim savePath = Path.Combine(GetDebugFolder(), $"GenReport_Loco_{dateFrom:yyyyMMdd}_{dateTo:yyyyMMdd}.xls")
                    File.WriteAllBytes(savePath, excelBytes)
                    LogWrite($"💾 Файл успешно сохранён: {savePath} ({excelBytes.Length} байт)")


                    ' Возвращаем полный путь к сохраненному файлу
                    Return savePath

                Catch ex As Exception
                    LogWrite($"💥 Ошибка в DownloadGenReportAsync: {ex.Message}{vbCrLf}{ex.StackTrace}")
                    Return Nothing
                End Try
            End Function


        End Class





        Public Sub TestParseTokensFromFile()
            Try
                ' Открываем диалог выбора файла
                Dim openFileDialog As New OpenFileDialog() With {
            .Title = "Выберите HTML-файл для парсинга токенов",
            .Filter = "HTML files (*.html;*.htm)|*.html;*.htm|All files (*.*)|*.*",
            .DefaultExt = "html",
            .FileName = "r341_page2_debug.html"
        }

                If openFileDialog.ShowDialog() <> DialogResult.OK Then
                    Return
                End If

                Dim htmlPath = openFileDialog.FileName

                If Not File.Exists(htmlPath) Then
                    MessageBox.Show($"Файл не найден: {htmlPath}")
                    Return
                End If

                'Dim fileHtml = File.ReadAllText(htmlPath, Encoding.GetEncoding("windows-1251"))
                Dim fileHtml = File.ReadAllText(htmlPath, Encoding.UTF8)
                ' Ищем фразу
                Dim searchText = "локомотивному комплексу"
                Dim locoIndex = fileHtml.IndexOf(searchText, StringComparison.OrdinalIgnoreCase)

                If locoIndex = -1 Then
                    MessageBox.Show($"Фраза '{searchText}' не найдена")
                    Return
                End If

                ' Ищем все href после фразы
                Dim hrefPattern As New Regex("inner/DetailReportInfo2755/([^""'/]+)/CENTER", RegexOptions.IgnoreCase)
                Dim matches = hrefPattern.Matches(fileHtml, locoIndex)

                If matches.Count = 0 Then
                    MessageBox.Show("href не найдены после фразы")
                    Return
                End If

                ' Собираем результат
                Dim result As New System.Text.StringBuilder()
                result.AppendLine($"=== ТЕСТ ПАРСИНГА ТОКЕНОВ ===")
                result.AppendLine($"Файл: {htmlPath}")
                result.AppendLine($"Фраза найдена на позиции: {locoIndex}")
                result.AppendLine($"Найдено href: {matches.Count}")
                result.AppendLine()

                ' Токен 1 (прошлый год)
                If matches.Count >= 1 Then
                    Dim token1 = matches(0).Groups(1).Value
                    result.AppendLine($"📌 ТОКЕН 1 (ПРОШЛЫЙ ГОД):")
                    result.AppendLine($"   {token1}")

                    ' Ищем число после токена (цифры в тексте)
                    Dim afterToken1 = fileHtml.Substring(matches(0).Index + matches(0).Length)
                    Dim numMatch1 = Regex.Match(afterToken1, "\b(\d{1,10})\b")
                    If numMatch1.Success Then
                        result.AppendLine($"   Число после токена: {numMatch1.Groups(1).Value}")
                    Else
                        result.AppendLine($"   Число после токена: [не найдено]")
                    End If
                    result.AppendLine()
                End If

                ' Токен 2 (текущий год)
                If matches.Count >= 2 Then
                    Dim token2 = matches(1).Groups(1).Value
                    result.AppendLine($"🎯 ТОКЕН 2 (ТЕКУЩИЙ ГОД):")
                    result.AppendLine($"   {token2}")

                    ' Ищем число после токена (цифры в тексте)
                    Dim afterToken2 = fileHtml.Substring(matches(1).Index + matches(1).Length)
                    Dim numMatch2 = Regex.Match(afterToken2, "\b(\d{1,10})\b")
                    If numMatch2.Success Then
                        result.AppendLine($"   Число после токена: {numMatch2.Groups(1).Value}")
                    Else
                        result.AppendLine($"   Число после токена: [не найдено]")
                    End If
                    result.AppendLine()
                End If

                ' Выводим результат
                ShowMSG(MW, result.ToString(), "Тест парсинга токенов", MessageBoxButton.OK, MessageBoxImage.Information)

            Catch ex As Exception
                ShowMSG(MW, $"Ошибка: {ex.Message}{vbCrLf}{vbCrLf}{ex.StackTrace}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End Sub


        Function GenerateTestDepotData() As List(Of DepotReportRow)
            Dim rnd As New Random()
            Dim rows As New List(Of DepotReportRow)

            Dim depots = {
                "СЛД Боготол",
                "СЛД Красноярск",
                "СЛД Иланская (Канск)",
                "СЛД Ачинск",
                "СЛД Абакан"
            }

            Dim sumP1C As Integer = 0, sumP1P As Integer = 0
            Dim sumP2C As Integer = 0, sumP2P As Integer = 0

            For Each depotName In depots
                ' Генерируем случайные числа от 0 до 50
                Dim p1c = rnd.Next(5, 50)
                Dim p1p = rnd.Next(5, 50)
                Dim p2c = rnd.Next(0, 30) ' Может быть 0, чтобы проверить пустые ячейки
                Dim p2p = rnd.Next(0, 30)

                sumP1C += p1c
                sumP1P += p1p
                sumP2C += p2c
                sumP2P += p2p

                rows.Add(New DepotReportRow With {
                    .DepotName = depotName,
                    .Period1Current = p1c,
                    .Period1PrevYear = p1p,
                    .Period2Current = p2c,
                    .Period2PrevYear = p2p,
                    .IsTotalRow = False
                })
            Next

            ' Добавляем строку ИТОГО
            rows.Add(New DepotReportRow With {
                .DepotName = "ИТОГО",
                .Period1Current = sumP1C,
                .Period1PrevYear = sumP1P,
                .Period2Current = sumP2C,
                .Period2PrevYear = sumP2P,
                .IsTotalRow = True
            })

            Return rows
        End Function




        ' Публичный доступ к логированию для модуля
        Public Sub LogWritePublic(msg As String)
            CentralFetcher.LogWrite(msg)
        End Sub


    End Module
End Namespace