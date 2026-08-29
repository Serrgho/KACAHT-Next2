Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Globalization
Imports System.Runtime.CompilerServices
Imports System.Text
Imports System.Text.RegularExpressions
Imports HtmlAgilityPack


Namespace Kas

	Public Module KasAntLoader

		Public KasAntWND As KasAntWin = Nothing

		'Public Class KasantFetcher
		'     Implements INotifyPropertyChanged

		'     ' 🔹 Скомпилированные Regex для парсинга скрытых полей (кэшируются)
		'     Private Shared ReadOnly _statusPatternBase As String = "name\s*=\s*[""']?viol{0}_status[""']?[^>]*?value\s*=\s*[""']([^""']*)[""']"
		'     Private Shared ReadOnly _trainPatternBase As String = "name\s*=\s*[""']?viol{0}_train_cnt[""']?[^>]*?value\s*=\s*[""']([^""']*)[""']"
		'     Private Shared ReadOnly _attachRegex As New Regex("attach\.gif.*?\((\d+)\)", RegexOptions.Compiled Or RegexOptions.IgnoreCase Or RegexOptions.Singleline)



		'     Private ReadOnly _httpClient As HttpClient
		'     Private ReadOnly _cookieContainer As New CookieContainer()
		'     Public ReadOnly _baseUrl As String = "http://kasant.gvc.oao.rzd:8888/kasant"

		'     ' Событие: статус авторизации изменился
		'     Public Event ConnectionChanged()
		'     Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged




		'     ' Поля дат (теперь private)
		'     Private _nachDat As Date = Date.MinValue
		'     Private _konDat As Date = Date.MinValue
		'     Private _nachTim As Integer = 0
		'     Private _konTim As Integer = 0
		'     Private _konMinut As Integer = 0
		'     Private _DorOfOTS As Integer = 88

		'     ' Свойства с уведомлением об изменении
		'     Public Property NachDat As Date
		'         Get
		'             If _nachDat = Date.MinValue Then
		'                 'Return Today.Date.AddDays(-1)
		'                 If Today.Day < 13 Then
		'                     Return New Date(Today.Year, Today.Month, 1).AddMonths(-1)
		'                 Else
		'                     Return New Date(Today.Year, Today.Month, 1)
		'                 End If
		'             Else
		'                 Return _nachDat
		'             End If

		'         End Get
		'         Set(value As Date)
		'             If _nachDat <> value Then
		'                 _nachDat = value
		'                 ' Уведомляем, что NachKon_TXT изменился
		'                 RaisePropertyChanged(NameOf(NachDat))
		'                 RaisePropertyChanged(NameOf(NachKon_TXT))
		'             End If
		'         End Set
		'     End Property

		'     Public Property KonDat As Date
		'         Get
		'             If _konDat = Date.MinValue Then
		'                 Return Today.Date
		'             Else
		'                 Return _konDat
		'             End If

		'         End Get
		'         Set(value As Date)
		'             If _konDat <> value Then
		'                 _konDat = value
		'                 RaisePropertyChanged(NameOf(KonDat))
		'                 RaisePropertyChanged(NameOf(NachKon_TXT))
		'             End If
		'         End Set
		'     End Property

		'     ' Текст для кнопки (остаётся ReadOnly)
		'     Public ReadOnly Property NachKon_TXT As String
		'         Get
		'             ' Свойства сами подставят "вчера" и "сегодня", если поля пусты
		'             Return $"{NachDat:dd.MM.yy} - {KonDat:dd.MM.yy}"
		'         End Get
		'     End Property


		'     Public Property NachTim As Integer
		'         Get
		'             Return _nachTim
		'         End Get
		'         Set(value As Integer)
		'             If _nachTim <> value Then
		'                 _nachTim = value

		'                 RaisePropertyChanged(NameOf(NachTim))
		'             End If
		'         End Set
		'     End Property

		'     Public Property KonTim As Integer
		'         Get
		'             Return _konTim
		'         End Get
		'         Set(value As Integer)
		'             If _konTim <> value Then
		'                 _konTim = value
		'                 ' Уведомляем, что NachKon_TXT изменился
		'                 RaisePropertyChanged(NameOf(KonTim))
		'             End If
		'         End Set
		'     End Property

		'     Public Property KonMinut As Integer
		'         Get
		'             Return _konMinut
		'         End Get
		'         Set(value As Integer)
		'             If _konMinut <> value Then
		'                 _konMinut = value
		'                 ' Уведомляем, что NachKon_TXT изменился
		'                 RaisePropertyChanged(NameOf(KonMinut))
		'             End If
		'         End Set
		'     End Property

		'     Public Property DorOfOTS As Integer
		'         Get
		'             Return _DorOfOTS
		'         End Get
		'         Set(value As Integer)
		'             If _DorOfOTS <> value Then
		'                 _DorOfOTS = value
		'                 ' Уведомляем, что NachKon_TXT изменился
		'                 RaisePropertyChanged(NameOf(DorOfOTS))
		'             End If
		'         End Set
		'     End Property


		'     Public Async Function EnsureConnectedAsync() As Task(Of Boolean)
		'         ' 1. Если сессия уже живая — сразу выходим
		'         If Await Fetcher.IsLoggedInAsync() Then Return True

		'         ' 2. Берём учётные данные (подставь свой источник, если не My.Settings)
		'         Dim user = MW.AuthCtrl.txtLogin.Text.Trim() 'My.Settings.KasantLogin
		'         Dim pass = MW.AuthCtrl.txtPassword.Password 'My.Settings.KasantPassword ' Если зашифрован, раскомментируй: DecryptPassword(...)




		'         If String.IsNullOrWhiteSpace(user) OrElse String.IsNullOrWhiteSpace(pass) Then
		'             'MessageBox.Show("Не заданы логин/пароль для подключения.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning)
		'             Return False
		'         End If

		'         ' 3. Пробуем войти
		'         Dim success = Await Fetcher.LoginAsync(user, pass)
		'         If success Then
		'             My.Settings.Save()
		'             Return True
		'         Else
		'             'MessageBox.Show("Ошибка авторизации. Проверьте учётные данные.", "Вход", MessageBoxButton.OK, MessageBoxImage.Error)
		'             Return False
		'         End If
		'     End Function





		'     ' Вспомогательный метод для raise события
		'     Protected Sub RaisePropertyChanged(<CallerMemberName> Optional propertyName As String = Nothing)
		'         RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
		'     End Sub

		'     ' ← ← ← ПУБЛИЧНЫЙ ДОСТУП К HTTPCLIENT
		'     Public ReadOnly Property HttpClient As HttpClient
		'         Get
		'             Return _httpClient
		'         End Get
		'     End Property

		'     Public Sub New()
		'         Dim handler As New HttpClientHandler()
		'         handler.CookieContainer = _cookieContainer
		'         handler.AllowAutoRedirect = True
		'         handler.UseCookies = True


		'         _httpClient = New HttpClient(handler)
		'         _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36")
		'         _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")
		'         _httpClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9,en;q=0.8")
		'         _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive")
		'         _httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1")

		'         Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
		'     End Sub



		'     Public Async Function IsLoggedInAsync() As Task(Of Boolean)
		'         Try
		'             ' Делаем легкий запрос к главной странице
		'             Dim response = Await _httpClient.GetAsync($"{_baseUrl}/index.jsp")
		'             Dim bytes = Await response.Content.ReadAsByteArrayAsync()
		'             Dim html = Encoding.GetEncoding("windows-1251").GetString(bytes)

		'             ' Если нет панели входа и есть ключевые слова — мы всё еще в системе
		'             Return response.IsSuccessStatusCode AndAlso
		'        Not html.Contains("anauth_panel") AndAlso
		'        html.Contains("Журналы")
		'         Catch
		'             Return False
		'         End Try
		'     End Function


		'     Public Async Function LoginAsync(username As String, password As String) As Task(Of Boolean)
		'         Try
		'             Dim debugFolder = GetDebugFolder()

		'             ' 1. Загружаем главную страницу для кук
		'             Dim mainPageResponse = Await _httpClient.GetAsync($"{_baseUrl}/index.jsp")
		'             Dim mainPageBytes = Await mainPageResponse.Content.ReadAsByteArrayAsync()
		'             Dim mainPageHtml = Encoding.GetEncoding("windows-1251").GetString(mainPageBytes)
		'             System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, "debug_main_page.html"), mainPageHtml, Encoding.GetEncoding("windows-1251"))

		'             ' 2. Отправляем авторизацию
		'             Dim content = New FormUrlEncodedContent(New Dictionary(Of String, String) From
		'             {
		'                 {"id_prog", "47"},
		'                 {"action", "full_card.jsp"},
		'                 {"dor_user", "88"},
		'                 {"login", username},
		'                 {"pass", password},
		'                 {"save_password", "on"}
		'             })

		'             content.Headers.ContentType = New System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded")
		'             content.Headers.ContentType.CharSet = "windows-1251"

		'             '==============================================================================================
		'             System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, "debug_request_info.txt"),
		'                 $"Отправляю POST на {_baseUrl}/login с логином: {username} в {DateTime.Now}")

		'             ' Заголовки для имитации браузера
		'             _httpClient.DefaultRequestHeaders.Referrer = New Uri($"{_baseUrl}/index.jsp")
		'             _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Origin", _baseUrl)
		'             _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")
		'             _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Charset", "windows-1251,utf-8;q=0.7")

		'             '==============================================================================================

		'             Dim response = Await _httpClient.PostAsync($"{_baseUrl}/login", content)

		'             ' 3. Читаем ответ
		'             Dim loginBytes = Await response.Content.ReadAsByteArrayAsync()
		'             Dim resultHtml = Encoding.GetEncoding("windows-1251").GetString(loginBytes)

		'             System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, "debug_login_result.html"), resultHtml, Encoding.GetEncoding("windows-1251"))
		'             System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, "debug_response_status.txt"),
		'                 $"Статус: {response.StatusCode}, Длина: {loginBytes.Length} байт в {DateTime.Now}")

		'             ' === ВАРИАНТ 2: Если тело пустое — делаем повторный GET ===
		'             If String.IsNullOrWhiteSpace(resultHtml) OrElse loginBytes.Length = 0 Then
		'                 System.IO.File.AppendAllText(System.IO.Path.Combine(debugFolder, "debug_login_result.html"),
		'                     $"{vbCrLf}=== ПУСТОЕ ТЕЛО, ДЕЛАЕМ ПОВТОРНЫЙ GET ==={vbCrLf}")

		'                 Try
		'                     Dim followUpResponse = Await _httpClient.GetAsync($"{_baseUrl}/index.jsp")
		'                     Dim followUpBytes = Await followUpResponse.Content.ReadAsByteArrayAsync()
		'                     resultHtml = Encoding.GetEncoding("windows-1251").GetString(followUpBytes)

		'                     System.IO.File.AppendAllText(System.IO.Path.Combine(debugFolder, "debug_login_result.html"),
		'                         $"Повторный GET: {followUpBytes.Length} байт{vbCrLf}{vbCrLf}{resultHtml}",
		'                         Encoding.GetEncoding("windows-1251"))
		'                 Catch ex As Exception
		'                     System.IO.File.AppendAllText(System.IO.Path.Combine(debugFolder, "debug_login_error.txt"),
		'                         $"Ошибка повторного GET: {ex.Message}{vbCrLf}")
		'                 End Try
		'             End If
		'             ' =========================================================
		'             ' 4. Проверка входа
		'             Dim isLogged = Not resultHtml.Contains("anauth_panel") AndAlso
		'                            (resultHtml.Contains("Журналы") OrElse
		'                             resultHtml.Contains("Отчёты") OrElse
		'                             resultHtml.Contains("session_invalidate") OrElse
		'                             resultHtml.Contains("Смена пользователя")) ' resultHtml.Contains("Еремин")← Имя пользователя как запасной вариант
		'             If isLogged Then
		'                 System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, "login_status.txt"), "ВХОД УСПЕШЕН")
		'                 RaiseEvent ConnectionChanged() ' <--- Генерируем событие
		'                 Return True
		'             Else
		'                 System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, "login_status.txt"),
		'                     $"ВХОД НЕ УДАЛСЯ{vbCrLf}Поиск маркеров:{vbCrLf}" &
		'                     $"  anauth_panel: {resultHtml.Contains("anauth_panel")}{vbCrLf}" &
		'                     $"  Журналы: {resultHtml.Contains("Журналы")}{vbCrLf}" &
		'                     $"  session_invalidate: {resultHtml.Contains("session_invalidate")}") '& $"  Еремин{resultHtml.Contains("Еремин")}"
		'                 Return False
		'                 'Return False
		'             End If

		'         Catch ex As Exception
		'             Dim debugFolder = GetDebugFolder()
		'             System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, "debug_login_error.txt"),
		'                 $"Ошибка: {ex.Message}{vbCrLf}{vbCrLf}{ex.StackTrace}")
		'             Return False
		'         End Try
		'     End Function


		'     Public Async Function FetchOtsDataAsync(violId As String, dorKod As Integer) As Task(Of OtsData)
		'         Try
		'             ' 1. Формируем URL
		'             Dim url As String = $"{_baseUrl}/full_card.jsp?viol={violId}&dor_kod={dorKod:00}&tab=140142325"
		'             Dim debugFolder = GetDebugFolder()

		'             ' Сохраняем URL для отладки
		'             System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, $"debug_url_{violId}.txt"),
		'     $"URL запроса: {url}{vbCrLf}Время: {DateTime.Now}")

		'             ' 2. Делаем запрос (используем уже авторизованный _httpClient)
		'             Dim response = Await _httpClient.GetAsync(url)
		'             response.EnsureSuccessStatusCode()

		'             Dim bytes = Await response.Content.ReadAsByteArrayAsync()
		'             Dim html = Encoding.GetEncoding("windows-1251").GetString(bytes)

		'             ' Сохраняем HTML для анализа
		'             Dim htmlPath = System.IO.Path.Combine(debugFolder, $"kasant_page_{violId}.html")
		'             System.IO.File.WriteAllText(htmlPath, html, Encoding.GetEncoding("windows-1251"))

		'             ' 3. Проверка: не вылетела ли сессия?
		'             If html.Contains("anauth_panel") OrElse html.Contains("id_prog") Then
		'                 ' Если сессия протухла, можно либо выбросить ошибку, либо попробовать перелогиниться
		'                 Throw New Exception("Сессия КАСАНТ завершена. Требуется повторный вход.")
		'             End If

		'             ' 4. Парсим данные
		'             Return ParseHtml(html, violId)

		'         Catch ex As Exception
		'             ' Логируем ошибку
		'             Dim debugFolder = GetDebugFolder()
		'             System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, $"fetch_error_{violId}.txt"),
		'     $"Ошибка: {ex.Message}{vbCrLf}{ex.StackTrace}")
		'             Throw
		'         End Try
		'     End Function


		'     Public log As New System.Text.StringBuilder()



		'     ' ================= ФАКТИЧЕСКОЕ ВРЕМЯ НАЧАЛА =================
		'     ''' <summary>
		'     ''' Парсит блок "Фактическое время (ОТС выявлено в результате проверки достоверности учета)"
		'     ''' </summary>
		'     Public Sub ParseFactTime(doc As HtmlDocument, result As OtsData)
		'         ' ← ← ← КЛЮЧЕВОЕ: . вместо text()
		'         Dim headerNode = doc.DocumentNode.SelectSingleNode("//b[contains(., 'Фактическое время') and contains(., 'проверки достоверности')]")

		'         If headerNode Is Nothing Then
		'             Return
		'         End If

		'         ' Ищем таблицу внутри того же div
		'         Dim container = headerNode.ParentNode
		'         Dim timeTable = container?.SelectSingleNode(".//table[.//b[contains(., 'Начало')]]")

		'         If timeTable Is Nothing Then
		'             log.AppendLine("⚠ Таблица времени НЕ найдена")
		'             Return
		'         End If

		'         ' Ищем строку с данными (есть дата в формате dd.dd.dd)
		'         Dim dataRow = timeTable.SelectSingleNode(".//tr[td[nobr[contains(., '.')]]]")
		'         If dataRow Is Nothing Then Return

		'         Dim cells = dataRow.SelectNodes(".//td")
		'         If cells IsNot Nothing AndAlso cells.Count >= 4 Then
		'             Dim startTime = CleanText(cells(1)?.InnerText)
		'             Dim endTime = CleanText(cells(2)?.InnerText)
		'             Dim duration = CleanText(cells(3)?.InnerText)

		'             If Not String.IsNullOrWhiteSpace(startTime) Then result.FactStartTime = startTime
		'             If Not String.IsNullOrWhiteSpace(endTime) Then result.FactEndTime = endTime
		'             If Not String.IsNullOrWhiteSpace(duration) Then result.FactDuration = duration

		'             log.AppendLine($"✓ Факт. время: {startTime} - {endTime} ({duration})")
		'         End If
		'     End Sub






		'     ' ================= ОПАСНЫЙ ОТКАЗ =================
		'     Public Sub ParseDangerOTS(doc As HtmlDocument, result As OtsData)
		'         ' 1. Ищем конкретную ячейку <td>, которая содержит текст "опасного" 
		'         ' И при этом находится ПЕРЕД таблицей с историей (ограничиваем область поиска верхом страницы)
		'         ' Используем индекс или поиск по конкретным стилям из вашего примера
		'         Dim statusNode = doc.DocumentNode.SelectSingleNode("//td[@style[contains(.,'color: red')] and contains(., 'Присвоен признак «опасного»')]")

		'         ' 2. Если нашли такую ячейку
		'         If statusNode IsNot Nothing Then
		'             ' Берем текст ТОЛЬКО этой ячейки
		'             Dim txt = CleanText(statusNode.InnerText)

		'             ' На всякий случай проверяем, не слишком ли длинный текст (защита от захвата всей таблицы)
		'             If txt.Length > 100 Then
		'                 ' Если текст огромный, значит XPath промахнулся, пробуем найти именно текст внутри
		'                 result.DangerStatus = "Присвоен признак «опасного» отказа"
		'             Else
		'                 result.DangerStatus = txt
		'             End If

		'             log.AppendLine($"✓ Статус: {result.DangerStatus}")
		'         Else
		'             result.DangerStatus = ""
		'             log.AppendLine("i Признак «опасного» не найден.")
		'         End If

		'     End Sub


		'     ' ================= 5.15 ОТКАЗ =================
		'     Public Sub Parse515Reason(doc As HtmlDocument, result As OtsData)
		'         ' Ищем ячейку td, которая содержит специфический текст пункта Положения
		'         Dim xpath As String = "//td[contains(text(), 'п.5.15 Положения')]"
		'         Dim reasonNode = doc.DocumentNode.SelectSingleNode(xpath)

		'         If reasonNode IsNot Nothing Then
		'             ' Извлекаем текст и очищаем его от лишних пробелов/переносов строк
		'             Dim rawText As String = reasonNode.InnerText.Trim()

		'             ' Записываем в свойство (CleanText — ваша вспомогательная функция для очистки)
		'             result.Is_5_15_OTS = CleanText(rawText)

		'             log.AppendLine($"✓ Причина 5.15 найдена: {result.Is_5_15_OTS}")
		'         Else
		'             log.AppendLine("⚠ Причина 5.15 (сбой ГЛОНАСС) в таблице НЕ найдена!")


		'         End If
		'     End Sub


		'     ' ================= 8. ДОКУМЕНТЫ (Дополнительные материалы) =================
		'     Public Async Sub ParseDocs(doc As HtmlDocument, result As OtsData)

		'         ' Выполняем тяжелый парсинг в фоновом потоке
		'         Await System.Threading.Tasks.Task.Run(Sub()
		'                                                   ' Ищем контейнер с материалами
		'                                                   Dim materialsDiv = doc.DocumentNode.SelectSingleNode("//b[contains(., 'Дополнительные материалы')]/ancestor::div[@style]")
		'                                                   If materialsDiv Is Nothing Then Return

		'                                                   ' Ищем все ссылки на скачивание файлов
		'                                                   Dim fileLinks = materialsDiv.SelectNodes(".//a[contains(@href, 'DownloadFile')]")
		'                                                   If fileLinks Is Nothing Then Return

		'                                                   For Each link In fileLinks
		'                                                       Dim href = link.GetAttributeValue("href", "")
		'                                                       Dim fileName = CleanText(link.InnerText)

		'                                                       ' Пропускаем пустые ссылки и иконки-заглушки
		'                                                       If String.IsNullOrWhiteSpace(fileName) OrElse fileName.ToLower().EndsWith(".gif") Then Continue For

		'                                                       ' Извлекаем размер файла (обычно в скобках рядом с названием)
		'                                                       Dim fileSize As String = ""
		'                                                       Dim rawFileText = CleanText(link.ParentNode?.InnerText)
		'                                                       If rawFileText.Contains("(") Then
		'                                                           fileSize = rawFileText.Split("("c).Last().Replace(")", "").Trim()
		'                                                       End If

		'                                                       Dim uploadDate = "", uploadedBy = "", commentText = ""

		'                                                       ' --- ЛОГИКА ПОИСКА КОММЕНТАРИЯ (ВВЕРХ ПО ТАБЛИЦЕ) ---

		'                                                       ' 1. Находим строку во ВНЕШНЕЙ таблице. 
		'                                                       ' Ищем предка tr, который является прямым ребенком (или через tbody) таблицы responsible_table
		'                                                       Dim outerFileRow = link.Ancestors("tr").FirstOrDefault(Function(r)
		'                                                                                                                  Dim parentTable = r.Ancestors("table").FirstOrDefault()
		'                                                                                                                  Return parentTable IsNot Nothing AndAlso parentTable.Id = "responsible_table"
		'                                                                                                              End Function)

		'                                                       ' ВАЖНО: ссылка на файл на самом деле находится во ВТОРОЙ строке блока (под датой).
		'                                                       ' Чтобы найти дату и комментарий, нам нужно подняться на одну значимую строку выше.
		'                                                       If outerFileRow IsNot Nothing Then
		'                                                           Dim allRows = outerFileRow.ParentNode.SelectNodes("./tr")
		'                                                           Dim currentIndex = allRows.IndexOf(outerFileRow)

		'                                                           ' Ищем строку с данными (она обычно сразу над строкой с файлом)
		'                                                           Dim infoRow As HtmlNode = Nothing
		'                                                           For i As Integer = currentIndex - 1 To 0 Step -1
		'                                                               Dim tempRow = allRows(i)
		'                                                               Dim testCells = tempRow.SelectNodes("./td")

		'                                                               ' Строка с инфо содержит 4 ячейки (Дата, Депо, ФИО, Комментарий)
		'                                                               If testCells IsNot Nothing AndAlso testCells.Count = 4 Then
		'                                                                   ' Проверка на дату в первой ячейке
		'                                                                   If testCells(0).InnerText.Contains(".") Then
		'                                                                       infoRow = tempRow
		'                                                                       Exit For
		'                                                                   End If
		'                                                               End If
		'                                                           Next

		'                                                           ' 4. Если нашли строку с данными — вытягиваем их
		'                                                           If infoRow IsNot Nothing Then
		'                                                               Dim tds = infoRow.SelectNodes("./td")
		'                                                               ' Дата и время
		'                                                               uploadDate = CleanText(tds(0).InnerText).Replace(vbCrLf, " ")
		'                                                               ' Подразделение + ФИО
		'                                                               Dim dept = CleanText(tds(1).InnerText)
		'                                                               Dim person = CleanText(tds(2).InnerText)
		'                                                               uploadedBy = $"{dept} {person}".Trim()
		'                                                               ' Тот самый комментарий "Для расследования..."
		'                                                               commentText = CleanText(tds(3).InnerText)
		'                                                           End If
		'                                                       End If

		'                                                       ' Формируем полный URL
		'                                                       Dim fullUrl = If(href.StartsWith("http"), href, _baseUrl.TrimEnd("/"c) & "/" & href.TrimStart("/"c))

		'                                                       ' Безопасно добавляем в результат (рекомендуется SyncLock, если result общий)
		'                                                       SyncLock result.AttachedFiles
		'                                                           result.AttachedFiles.Add(New AttachedFile With {
		'                                                                                    .FileName = fileName,
		'                                                                                    .DownloadUrl = fullUrl,
		'                                                                                    .FileSize = fileSize,
		'                                                                                    .UploadDate = uploadDate,
		'                                                                                    .UploadedBy = uploadedBy,
		'                                                                                    .Comment = commentText
		'                                                                                    })
		'                                                       End SyncLock

		'                                                       log.AppendLine($"📎 {fileName} | Коммент: {If(String.IsNullOrEmpty(commentText), "[пусто]", commentText)}")
		'                                                   Next
		'                                               End Sub)

		'     End Sub


		'     ' ================= 0. СТАТУС ПЕРЕДАЧИ =================
		'     Public Sub ParseStatus(doc As HtmlDocument, result As OtsData)
		'         ' ИСПРАВЛЕНО: проверяем оба возможных статуса
		'         Dim statusNode = doc.DocumentNode.SelectSingleNode("//td[contains(@style, 'color: darkgreen')]//b")
		'         If statusNode IsNot Nothing Then
		'             Dim statusText = statusNode.InnerText

		'             result.InvestigationStatus = CleanText(statusText)
		'             log.AppendLine($"✓ Статус: {result.InvestigationStatus}")

		'             If Not String.IsNullOrWhiteSpace(result.InvestigationStatus) Then
		'                 If result.InvestigationStatus.ToLower().Contains("статус на другой дороге") Then
		'                     Dim mat = Regex.Match(result.InvestigationStatus,
		'                     "передан\s+(.+)\s+\d{2}\.\d{2}\.\d{4}") '"был\s+передан\s+(.+?)\s+\d{2}\.\d{2}\.\d{4}"

		'                     If mat.Success Then
		'                         result.PeredanOnOtherDor = $"передан {mat.Groups(1).Value.Trim()}"
		'                         log.AppendLine($"✓ Передан на другую дорогу: {result.PeredanOnOtherDor}")
		'                     End If
		'                 End If
		'             End If

		'         End If
		'     End Sub


		'     ' ================= 1. МЕСТО ОТКАЗА =================
		'     Public Sub ParseMestoOTS(doc As HtmlDocument, result As OtsData)
		'         ' ИСПРАВЛЕНО: более точный поиск места
		'         Dim locationDiv = doc.DocumentNode.SelectSingleNode("//div[@id='locations_div']")
		'         If locationDiv IsNot Nothing Then
		'             Dim nobr = locationDiv.SelectSingleNode(".//nobr")
		'             If nobr IsNot Nothing Then
		'                 result.Location = CleanText(nobr.InnerText)
		'                 log.AppendLine($"✓ Место: {result.Location}")
		'             End If
		'         End If
		'     End Sub


		'     ' ================= 2. ВРЕМЯ (НАЧАЛО / УСТРАНЕНИЕ / ПРОДОЛЖИТЕЛЬНОСТЬ) =================
		'     Public Sub ParseNachOTS(doc As HtmlDocument, result As OtsData)
		'         Dim timeDiv = doc.DocumentNode.SelectSingleNode("//b[contains(text(), 'Время')]/ancestor::div[@style][1]")
		'         If timeDiv IsNot Nothing Then
		'             ' Ищем таблицу по заголовкам, а не по border
		'             Dim timeTable = timeDiv.SelectSingleNode(".//table[.//b[contains(text(), 'Начало')]]")
		'             If timeTable IsNot Nothing Then
		'                 Dim dataRow = timeTable.SelectSingleNode(".//tr[td[nobr][3]]") ' 3 ячейки с данными
		'                 If dataRow IsNot Nothing Then
		'                     Dim cells = dataRow.SelectNodes(".//td[nobr]")
		'                     If cells?.Count >= 3 Then
		'                         result.StartTime = CleanText(cells(0).InnerText)
		'                         result.EndTime = CleanText(cells(1).InnerText)
		'                         result.Duration = CleanText(cells(2).InnerText)
		'                     End If
		'                 End If
		'             End If
		'         End If
		'     End Sub


		'     ' ================= 5.1. ВРЕМЯ К УЧЕТУ (PCHasy) =================
		'     Public Sub ParsePCHUchet(doc As HtmlDocument, result As OtsData)
		'         ' Ищем текст "к учету" и извлекаем то, что после него в скобках
		'         Dim fullText = doc.DocumentNode.InnerText
		'         Dim match = Regex.Match(fullText, "к учету\s+([^)]+)")
		'         If match.Success Then
		'             result.PCHasy = match.Groups(1).Value.Trim()
		'             log.AppendLine($"✓ Потери поездо-часов: {result.PCHasy}")
		'         End If
		'     End Sub


		'     ' ================= ПАРСИНГ ОБОРУДОВАНИЯ (Oborud) =================
		'     Public Sub ParseOborud(doc As HtmlDocument, result As OtsData)
		'         Dim causeDiv = doc.DocumentNode.SelectSingleNode("//b[text()='Причина']/ancestor::div[@style][1]")
		'         If causeDiv IsNot Nothing Then
		'             ' Ищем таблицу с заголовками "Отказавшее тех. средство", "Причины отказа"
		'             Dim reasonTable = causeDiv.SelectSingleNode(".//table[.//b[contains(text(), 'Отказавшее тех. средство')]]")
		'             If reasonTable IsNot Nothing Then
		'                 ' === Ищем ТОЛЬКО вложенную таблицу с иерархией (в ячейке под "Отказавшее тех. средство") ===
		'                 ' Структура: заголовки → строка данных → первая ячейка данных содержит <table> с иерархией
		'                 Dim hierarchyCell = reasonTable.SelectSingleNode(".//tr[2]//td[2]//table")

		'                 If hierarchyCell IsNot Nothing Then
		'                     Dim boldNodes = hierarchyCell.SelectNodes(".//b")
		'                     Dim equipParts As New List(Of String)

		'                     If boldNodes IsNot Nothing AndAlso boldNodes.Count > 0 Then
		'                         ' === ЕСТЬ ЖИРНОЕ: собираем ВСЕ уровни иерархии из <td> ===
		'                         result.IsKasantHierarchy = True  ' <--- ВАЖНО: помечаем успешный парсинг
		'                         Dim allTd = hierarchyCell.SelectNodes(".//td")
		'                         If allTd IsNot Nothing Then
		'                             For Each td In allTd
		'                                 Dim txt = CleanText(td.InnerText)
		'                                 ' Фильтруем мусор: пустоты, разделители, заголовки таблицы
		'                                 If Not String.IsNullOrWhiteSpace(txt) AndAlso txt <> "|" AndAlso
		'                                    txt <> "Отказавшее тех. средство" AndAlso txt <> "Причины отказа" AndAlso txt <> "Комментарий" Then
		'                                     ' Избегаем дубликатов (на всякий случай)
		'                                     If Not equipParts.Contains(txt) Then
		'                                         equipParts.Add(txt)
		'                                     End If
		'                                 End If
		'                             Next
		'                         End If
		'                     Else
		'                         ' === НЕТ ЖИРНОГО: старый фоллбэк — последняя непустая ячейка ===
		'                         Dim allTd = hierarchyCell.SelectNodes(".//td")
		'                         If allTd IsNot Nothing Then
		'                             For i As Integer = allTd.Count - 1 To 0 Step -1
		'                                 Dim txt = CleanText(allTd(i).InnerText)
		'                                 If Not String.IsNullOrWhiteSpace(txt) AndAlso txt <> "|" Then
		'                                     equipParts.Add(txt)
		'                                     Exit For
		'                                 End If
		'                             Next
		'                         End If
		'                     End If

		'                     ' === Формируем итоговую строку ===
		'                     If equipParts.Count > 0 Then
		'                         result.Oborud = String.Join(" → ", equipParts)
		'                         log.AppendLine($"✓ Оборудование: {result.Oborud}")
		'                     End If
		'                 End If


		'             End If
		'         End If
		'     End Sub


		'     ' ================= 3. ОБЩЕЕ ОПИСАНИЕ ОТКАЗА =================
		'     Public Sub ParseCommonOpisOTS(doc As HtmlDocument, result As OtsData)
		'         Try
		'             'log.AppendLine("[DEBUG] === Начинаю парсинг Характера ===")

		'             ' Ищем раздел "Характер"
		'             Dim characterDiv = doc.DocumentNode.SelectSingleNode("//b[text()='Характер']/ancestor::div[@style][1]")
		'             'log.AppendLine($"[DEBUG] characterDiv = {If(characterDiv IsNot Nothing, "НАЙДЕН", "Nothing")}")

		'             If characterDiv IsNot Nothing Then
		'                 ' === 1. Основной текст характера (Тип пометки, Причина ГИД, Отказавший локомотив) ===
		'                 Dim infoCell = characterDiv.SelectSingleNode(".//td[contains(., 'Тип пометки') or contains(., 'Причина ГИД')]")
		'                 'log.AppendLine($"[DEBUG] infoCell = {If(infoCell IsNot Nothing, "НАЙДЕН", "Nothing")}")

		'                 If infoCell IsNot Nothing Then
		'                     result.CharacterText = CleanTextWithBreaks(infoCell.InnerHtml)
		'                     log.AppendLine($"✓ Характер: {result.CharacterText}")
		'                 End If

		'                 ' === 2. Таблица: Отказавшее тех. средство | Проявление | Комментарий ===
		'                 Dim charTable = characterDiv.SelectSingleNode(".//table[.//b[contains(text(), 'Отказавшее тех. средство')]]")
		'                 'log.AppendLine($"[DEBUG] charTable = {If(charTable IsNot Nothing, "НАЙДЕН", "Nothing")}")

		'                 If charTable IsNot Nothing Then
		'                     Dim dataRows = charTable.SelectNodes(".//tr")

		'                     If dataRows IsNot Nothing Then
		'                         ' === Ищем строку с данными (где есть 4 ячейки и во 2-й ячейке есть текст) ===
		'                         Dim dataRow As HtmlNode = Nothing
		'                         For Each row In dataRows
		'                             Dim cells = row.SelectNodes(".//td")
		'                             ' Пропускаем, если ячеек меньше 4
		'                             If cells IsNot Nothing AndAlso cells.Count >= 4 Then
		'                                 ' Проверяем 2-ю ячейку (индекс 1) - там должно быть оборудование
		'                                 Dim cell2Text = CleanText(cells(1).InnerText)
		'                                 ' Пропускаем заголовок и пустые строки
		'                                 If Not String.IsNullOrWhiteSpace(cell2Text) AndAlso Not cell2Text.Contains("Отказавшее тех. средство") Then
		'                                     dataRow = row
		'                                     Exit For
		'                                 End If
		'                             End If
		'                         Next

		'                         If dataRow IsNot Nothing Then
		'                             Dim cells = dataRow.SelectNodes(".//td")
		'                             If cells IsNot Nothing AndAlso cells.Count >= 4 Then
		'                                 ' Ячейка 1: Отказавшее тех. средство
		'                                 result.FailedEquipment = CleanText(cells(1).InnerText)
		'                                 log.AppendLine($"  ✓ Отказавшее средство: {result.FailedEquipment}")
		'                                 ' Ячейка 2: Проявление отказа
		'                                 result.FailureManifestation = CleanText(cells(2).InnerText)
		'                                 log.AppendLine($"  ✓ Проявление: {result.FailureManifestation}")
		'                                 ' Ячейка 3: Комментарий ← InnerHtml для <NLok=...>
		'                                 result.CharacterComment = CleanComment(cells(3).InnerHtml)
		'                                 log.AppendLine($"  ✓ Комментарий: {result.CharacterComment}")
		'                             End If
		'                         Else
		'                             log.AppendLine("⚠ Не найдена строка с данными в таблице Характера")
		'                         End If
		'                     End If
		'                 End If
		'             End If

		'         Catch ex As Exception
		'             log.AppendLine($"!!! ОШИБКА в парсинге Характера: {ex.GetType().Name}: {ex.Message}")
		'             log.AppendLine(ex.StackTrace)
		'         End Try
		'     End Sub


		'     ' ================= 4. КАТЕГОРИИ =================
		'     Public Sub ParseKategorys(doc As HtmlDocument, result As OtsData)
		'         Dim cat1052 = doc.DocumentNode.SelectSingleNode("//td[contains(text(), '1052р')]/b")
		'         If cat1052 IsNot Nothing Then
		'             result.Category1052 = cat1052.InnerText.Trim()
		'             log.AppendLine($"✓ Категория 1052р: {result.Category775}")
		'         End If

		'         Dim cat775 = doc.DocumentNode.SelectSingleNode("//td[contains(text(), '775р')]/b")
		'         If cat775 IsNot Nothing Then
		'             result.Category775 = cat775.InnerText.Trim()
		'             log.AppendLine($"✓ Категория 775р: {result.Category775}")
		'         End If


		'         Dim cat1915 = doc.DocumentNode.SelectSingleNode("//td[contains(text(), '1915р')]/i")
		'         If cat1915 IsNot Nothing Then
		'             result.Category1915 = cat1915.InnerText.Trim()
		'             log.AppendLine($"✓ Категория 1915р: {result.Category1915}")
		'         End If
		'     End Sub


		'     ' ================= 9. ВИНОВНАЯ ОРГАНИЗАЦИЯ =================
		'     Public Sub ParseVinovnikOrganization(doc As HtmlDocument, result As OtsData)
		'         ' 1. Сначала пробуем стандартный поиск (сервисная/сторонняя организация)
		'         Dim alienTable = doc.DocumentNode.SelectSingleNode("//table[@id='alien_guilty_table']")
		'         If alienTable IsNot Nothing Then
		'             Dim orgRow = alienTable.SelectSingleNode(".//tr[td[contains(., 'Наименование предприятия') or contains(., 'Наименования сервисной')]]")
		'             If orgRow IsNot Nothing Then
		'                 Dim cells = orgRow.SelectNodes(".//td")
		'                 If cells IsNot Nothing AndAlso cells.Count >= 2 Then
		'                     Dim rawText = CleanText(cells(1).InnerText)
		'                     result.ThirdPartyOrg = CleanThirdPartyOrg(rawText)
		'                     log.AppendLine($"✓ Отнесен на (сервис): {result.ThirdPartyOrg}")
		'                     Exit Sub ' Нашли сервисную, выходим
		'                 End If
		'             End If
		'         End If






		'         ' 2. Если сервисную не нашли, проверяем на "эксплуатационный" характер
		'         ' Ищем текст в документе
		'         ' 2. Если сервисную не нашли, проверяем на "эксплуатационный" характер
		'         Dim isExploitation = doc.DocumentNode.InnerHtml.Contains("Характер причины отказа</b>: эксплуатационный")

		'         If isExploitation Then
		'             Dim respTable = doc.DocumentNode.SelectSingleNode("//table[@id='responsible_table']")
		'             If respTable IsNot Nothing Then
		'                 ' Ищем строку, в которой НЕТ атрибута background (заголовки обычно с ним) 
		'                 ' и которая содержит данные (больше 4 ячеек)
		'                 Dim dataRow = respTable.SelectSingleNode(".//tr[not(td[@style[contains(., 'background')]]) and count(td) >= 4]")

		'                 ' Если XPath выше не сработал, берем просто последнюю строку таблицы
		'                 If dataRow Is Nothing Then
		'                     dataRow = respTable.SelectNodes(".//tr").LastOrDefault()
		'                 End If

		'                 If dataRow IsNot Nothing Then
		'                     Dim cells = dataRow.SelectNodes("td")
		'                     If cells IsNot Nothing AndAlso cells.Count >= 4 Then
		'                         ' Индекс 3 — это 4-я колонка "Подразделение"
		'                         result.ThirdPartyOrg = CleanText(cells(3).InnerText)
		'                         log.AppendLine($"✓ Отнесен на (эксплуатация): {result.ThirdPartyOrg}")
		'                     End If
		'                 End If
		'             End If
		'         End If


		'     End Sub


		'     ' ================= 7. ПОСЛЕДСТВИЯ (СОБЫТИЯ, КОРПОРАТИВ И Т.Д.) =================
		'     Public Sub ParseSobytieKorpETC(doc As HtmlDocument, result As OtsData)
		'         Dim consequencesDiv = doc.DocumentNode.SelectSingleNode("//b[contains(text(), 'Последствия')]/ancestor::div[@style][1]")
		'         If consequencesDiv IsNot Nothing Then
		'             ' Ищем все <li> элементы внутри блока
		'             Dim listItems = consequencesDiv.SelectNodes(".//li")

		'             If listItems IsNot Nothing Then
		'                 Dim consequencesList As New List(Of String)

		'                 For Each li In listItems
		'                     Dim text = CleanText(li.ParentNode.InnerText)
		'                     If Not String.IsNullOrWhiteSpace(text) Then
		'                         consequencesList.Add(text)
		'                         ' Извлекаем текст о транспортном происшествии
		'                         If text.ToLower().Contains("транспортное происшествие") Then
		'                             result.Consequences = text
		'                         End If

		'                         If text.ToLower().Contains("орпоративное нарушение") Then
		'                             result.Korporativ = text
		'                         End If
		'                     End If
		'                 Next

		'                 If consequencesList.Count > 0 Then
		'                     log.AppendLine($"✓ Последствия: {consequencesList.Count} записей")
		'                     If Not String.IsNullOrWhiteSpace(result.Consequences) Then
		'                         log.AppendLine($"  📋 {result.Consequences.Substring(0, Math.Min(100, result.Consequences.Length))}...")
		'                     End If
		'                 End If
		'             End If
		'         End If
		'     End Sub


		'     ' ================= 5. ЗАДЕРЖАННЫЕ ПОЕЗДА =================
		'     Public Sub ParseTrains(doc As HtmlDocument, result As OtsData)
		'         ' ИСПРАВЛЕНО: более надежный парсинг поездов
		'         Dim conseqTable = doc.DocumentNode.SelectSingleNode("//table[@id='conseq_table']")
		'         If conseqTable IsNot Nothing Then
		'             ' Ищем все строки, содержащие информацию о поездах
		'             Dim allRows = conseqTable.SelectNodes(".//tr")
		'             Dim i As Integer = 0

		'             While i < allRows.Count
		'                 Dim row = allRows(i)
		'                 Dim rowText = row.InnerText

		'                 ' Проверяем, содержит ли строка информацию о поезде
		'                 If (rowText.Contains("Грузо") OrElse rowText.Contains("Пассажир") OrElse rowText.Contains("Пригородн") OrElse rowText.Contains("Прочие")) AndAlso rowText.Contains("№") Then

		'                     Dim train = New TrainDelay()

		'                     ' Определяем тип поезда
		'                     If rowText.Contains("Грузов") Then
		'                         train.TrainType = "Грузовой"
		'                     ElseIf rowText.Contains("Пассажир") Then
		'                         train.TrainType = "Пассажирский"
		'                     ElseIf rowText.Contains("Пригородн") Then
		'                         train.TrainType = "Пригородный"
		'                     ElseIf rowText.Contains("Прочие") Then
		'                         train.TrainType = "Прочие"
		'                     End If

		'                     ' ← ← ← ДОБАВИТЬ: Извлекаем маршрут и время из <nobr>
		'                     Dim nobrNode = row.SelectSingleNode(".//nobr")
		'                     If nobrNode IsNot Nothing Then
		'                         Dim nobrText = CleanText(nobrNode.InnerText)
		'                         ' Убираем лишние пробелы и переносы
		'                         nobrText = Regex.Replace(nobrText, "\s+", " ").Trim()

		'                         ' Если есть текст в скобках — это маршрут
		'                         Dim routeMatch = Regex.Match(nobrText, "\(([^)]+)\)")
		'                         If routeMatch.Success Then
		'                             train.RouteInfo = routeMatch.Groups(1).Value.Trim()
		'                             log.AppendLine($"Место: {train.RouteInfo}")
		'                         End If
		'                     End If

		'                     ' Ищем номер поезда
		'                     Dim numMatch = Regex.Match(rowText, "№\s*(\d+)")
		'                     If numMatch.Success Then
		'                         train.TrainNumber = numMatch.Groups(1).Value
		'                     End If

		'                     ' Проверяем, первый ли поезд (со звездочкой)
		'                     Dim firstCell = row.SelectSingleNode(".//td[1]")
		'                     If firstCell IsNot Nothing AndAlso firstCell.InnerHtml.Contains("*") AndAlso (firstCell.InnerHtml.Contains("color: red") OrElse firstCell.InnerHtml.Contains("color:red")) Then
		'                         train.IsFirst = True
		'                     End If

		'                     ' Ищем время задержки (может быть в этой же или следующей строке)
		'                     Dim delay As String = ""
		'                     Dim delayCell = row.SelectSingleNode(".//td[contains(., 'мин') or contains(., 'ч')]")
		'                     If delayCell Is Nothing AndAlso i + 1 < allRows.Count Then
		'                         delayCell = allRows(i + 1).SelectSingleNode(".//td[contains(., 'мин') or contains(., 'ч')]")
		'                     End If

		'                     If delayCell IsNot Nothing Then
		'                         delay = CleanText(delayCell.InnerText)
		'                         train.DelayMinutes = ParseDelay(delay)
		'                     End If

		'                     ' Парсим локомотивы, если это первый поезд или поезд со звездочкой
		'                     If train.IsFirst OrElse result.DelayedTrains.Count = 0 Then
		'                         ' Ищем панель с локомотивами
		'                         For j As Integer = i To Math.Min(i + 2, allRows.Count - 1)
		'                             Dim searchRow = allRows(j)
		'                             Dim panelMatch = Regex.Match(searchRow.InnerHtml, "train(\d+)_locs_panel")

		'                             If panelMatch.Success Then
		'                                 Dim panelId = $"train{panelMatch.Groups(1).Value}_locs_panel"
		'                                 Dim locoPanel = doc.DocumentNode.SelectSingleNode($"//table[@id='{panelId}']")

		'                                 If locoPanel IsNot Nothing Then
		'                                     Dim locoRows = locoPanel.SelectNodes(".//tr")
		'                                     If locoRows IsNot Nothing Then
		'                                         For Each locoRow In locoRows
		'                                             Dim locoText = locoRow.InnerText
		'                                             If locoText.Contains("Серия локомотива:") Then
		'                                                 Dim loco = New LocoInfo()

		'                                                 loco.Series = ExtractField(locoText, "Серия локомотива:", "Номер локомотива:")
		'                                                 loco.Number = ExtractField(locoText, "Номер локомотива:", "Дорога приписки локомотива")
		'                                                 loco.RealNumber = Regex.Match(result.CharacterComment, "<nlok=(\d+)").Groups(1).Value
		'                                                 loco.Road = ExtractField(locoText, "Дорога приписки локомотива, МВПС (ССПС):", "Депо приписки локомотива, МВПС (ССПС):")
		'                                                 loco.Depot = ExtractField(locoText, "Депо приписки локомотива, МВПС (ССПС):", "Дорога приписки бригады:")
		'                                                 loco.CrewRoad = ExtractField(locoText, "Дорога приписки бригады:", "Депо приписки бригады:")
		'                                                 loco.CrewDepot = ExtractField(locoText, "Депо приписки бригады:", "Табельный номер машиниста:")
		'                                                 loco.Driver = ExtractField(locoText, "Фамилия машиниста (водителя):", "Время явки")

		'                                                 If Not String.IsNullOrWhiteSpace(loco.Series) Then
		'                                                     train.Locomotives.Add(loco)

		'                                                 End If
		'                                             End If
		'                                         Next
		'                                     End If
		'                                 End If
		'                                 Exit For
		'                             End If
		'                         Next
		'                     End If

		'                     If Not String.IsNullOrWhiteSpace(train.TrainNumber) Then
		'                         result.DelayedTrains.Add(train)
		'                         log.AppendLine($"✓ Поезд №{train.TrainNumber} ({train.TrainType}), Задержка: {train.DelayMinutes} мин, Локомотивов: {train.Locomotives.Count}")
		'                     End If
		'                 End If

		'                 i += 1
		'             End While
		'         End If
		'     End Sub


		'     ' ================= 6. ИСТОРИЯ ОТКАЗА =================
		'     Public Sub ParseHistory(doc As HtmlDocument, result As OtsData)
		'         ' ИСПРАВЛЕНО: улучшен парсинг истории
		'         Dim historyDiv = doc.DocumentNode.SelectSingleNode("//b[contains(text(), 'История')]/ancestor::div[@style][1]")
		'         If historyDiv IsNot Nothing Then
		'             Dim historyTable = historyDiv.SelectSingleNode(".//table[.//b[contains(text(), 'Когда')]]")
		'             If historyTable IsNot Nothing Then
		'                 Dim historyRows = historyTable.SelectNodes(".//tr")

		'                 ' ← ← ← Список слов-заголовков, которые нужно пропускать
		'                 Dim headerWords = {"Когда", "Кем", "АСУ", "ЧТО", "КОМУ", "Откуда", "Куда", "Действие", "Примечание", "Комментарий"}

		'                 If historyRows IsNot Nothing Then
		'                     For Each row In historyRows
		'                         Dim cells = row.SelectNodes(".//td")

		'                         If cells IsNot Nothing AndAlso cells.Count >= 2 Then
		'                             Dim dateText = CleanText(If(cells(1)?.InnerText, ""))

		'                             ' ← ← ← ФИЛЬТР 1: Пропускаем, если текст содержит стоп-слово заголовка
		'                             If headerWords.Any(Function(w) dateText.ToLower().Contains(w.ToLower())) Then
		'                                 Continue For
		'                             End If

		'                             ' ← ← ← ФИЛЬТР 2: Требуем хотя бы 2 цифры подряд (признак даты)
		'                             If Not Regex.IsMatch(dateText, "\d{2}") Then
		'                                 Continue For
		'                             End If

		'                             Dim record = New HistoryRecord()
		'                             record.DateTime = dateText
		'                             record.ActionType = If(cells.Count > 2, CleanText(cells(2)?.InnerText), "")
		'                             record.From = If(cells.Count > 3, CleanText(cells(3)?.InnerText), "")
		'                             record.ToDest = If(cells.Count > 4, CleanText(cells(4)?.InnerText), "")
		'                             record.ByWhom = If(cells.Count > 5, CleanText(cells(5)?.InnerText), "")
		'                             record.Comment = If(cells.Count > 6, CleanText(cells(6)?.InnerText), "")

		'                             result.History.Add(record)
		'                             log.AppendLine($"  [{record.DateTime}] {record.ActionType}")
		'                         End If
		'                     Next
		'                 End If
		'             End If
		'         End If
		'     End Sub





		'     Private Function ParseHtml(html As String, violId As String) As OtsData
		'         Dim doc = New HtmlDocument()

		'         doc.LoadHtml(html)
		'         Dim result = New OtsData With {.ViolId = violId}
		'         Dim debugFolder = GetDebugFolder()
		'         'Dim log As New System.Text.StringBuilder()

		'         log.AppendLine($"=== Парсинг отказа {violId} ===")
		'         log.AppendLine($"Время: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")

		'         Try

		'             ' ================= ФАКТИЧЕСКОЕ ВРЕМЯ НАЧАЛА =================
		'             ParseFactTime(doc, result)

		'             ' ================= ОПАСНЫЙ ОТКАЗ =================
		'             ParseDangerOTS(doc, result)

		'             ' ================= 5.15 ОТКАЗ =================
		'             Parse515Reason(doc, result)

		'             ' ================= 8. ДОКУМЕНТЫ (Дополнительные материалы) =================
		'             ParseDocs(doc, result)

		'             ' ================= 0. СТАТУС ПЕРЕДАЧИ =================
		'             ParseStatus(doc, result)

		'             ' ================= 1. МЕСТО ОТКАЗА =================
		'             ParseMestoOTS(doc, result)

		'             ' ================= 2. ВРЕМЯ (НАЧАЛО / УСТРАНЕНИЕ / ПРОДОЛЖИТЕЛЬНОСТЬ) =================
		'             ParseNachOTS(doc, result)

		'             ' ================= 5.1. ВРЕМЯ К УЧЕТУ (PCHasy) =================
		'             ParsePCHUchet(doc, result)

		'             ' ================= ПАРСИНГ ОБОРУДОВАНИЯ (Oborud) =================
		'             ParseOborud(doc, result)

		'             ' ================= 3. ОБЩЕЕ ОПИСАНИЕ ОТКАЗА =================
		'             ParseCommonOpisOTS(doc, result)

		'             ' ================= 4. КАТЕГОРИИ =================
		'             ParseKategorys(doc, result)

		'             ' ================= 9. ВИНОВНАЯ ОРГАНИЗАЦИЯ =================
		'             ParseVinovnikOrganization(doc, result)

		'             ' ================= 7. ПОСЛЕДСТВИЯ (СОБЫТИЯ, КОРПОРАТИВ И Т.Д.) =================
		'             ParseSobytieKorpETC(doc, result)

		'             ' ================= 5. ЗАДЕРЖАННЫЕ ПОЕЗДА =================
		'             ParseTrains(doc, result)

		'             ' ================= 6. ИСТОРИЯ ОТКАЗА =================
		'             ParseHistory(doc, result)


		'         Catch ex As Exception
		'             log.AppendLine($"!!! ОШИБКА: {ex.Message}")
		'             log.AppendLine(ex.StackTrace)
		'         End Try

		'         ' Сохранение лога
		'         Try
		'             Dim logPath = System.IO.Path.Combine(debugFolder, $"parse_log_{violId}_{DateTime.Now:yyyyMMdd_HHmmss}.txt")
		'             System.IO.File.WriteAllText(logPath, log.ToString())
		'             log.Clear()
		'         Catch

		'         End Try

		'         Return result
		'     End Function

		'     ' ================= ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ =================

		'     ' === НОВАЯ ФУНКЦИЯ ДЛЯ ТЕСТОВ ===
		'     Public Function ParseLocalHtmlFile(filePath As String, violId As String) As OtsData
		'         If Not System.IO.File.Exists(filePath) Then
		'             Throw New FileNotFoundException($"Файл не найден: {filePath}")
		'         End If

		'         Dim html As String = System.IO.File.ReadAllText(filePath, Encoding.GetEncoding("windows-1251"))
		'         Return ParseHtml(html, violId)
		'     End Function
		'     ' ================================






		'     ' Для комментария — чистим, но оставляем теги типа <NLok=...>
		'     Private Function CleanComment(text As String) As String
		'         If String.IsNullOrWhiteSpace(text) Then Return ""

		'         ' <br> → пробел
		'         text = Regex.Replace(text, "<br\s*/?>", " ")

		'         ' Удаляем обычные теги, НО оставляем <NLok=...>
		'         'text = Regex.Replace(text, "<(?!NLok)[^>]+>", "")

		'         ' HTML-сущности и пробелы
		'         text = text.Replace("&nbsp;", " ")
		'         text = Regex.Replace(text, "\s+", " ")

		'         Return text.Trim()
		'     End Function


		'     Private Function CleanText(text As String) As String
		'         If String.IsNullOrWhiteSpace(text) Then Return ""

		'         ' Заменяем <br> на пробел
		'         text = Regex.Replace(text, "<br\s*/?>", " ")

		'         ' Удаляем HTML теги
		'         text = Regex.Replace(text, "<[^>]+>", " ")

		'         ' Заменяем HTML сущности
		'         'text = text.Replace("&nbsp;", " ").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&").Replace(vbCrLf, " ").Replace(vbTab, " ")

		'         text = text.Replace("&nbsp;", " ").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&")

		'         text = text.Replace("#", " # ")

		'         ' Удаляем множественные пробелы
		'         text = Regex.Replace(text, "\s+", " ")

		'         Return text.Trim()
		'     End Function


		'     Private Function CleanTextWithBreaks(text As String) As String
		'         If String.IsNullOrWhiteSpace(text) Then Return ""

		'         ' === <br> → перенос строки ===
		'         text = Regex.Replace(text, "<br\s*/?>", vbCrLf)

		'         ' Удаляем остальные теги
		'         text = Regex.Replace(text, "<[^>]+>", "")

		'         ' HTML-сущности
		'         text = text.Replace("&nbsp;", " ")

		'         ' Нормализуем пробелы, но НЕ трогаем \r\n
		'         text = Regex.Replace(text, "[^\S\r\n]+", " ")

		'         Return text.Trim()
		'     End Function



		'     Private Function ExtractField(text As String, startMarker As String, endMarker As String) As String
		'         If String.IsNullOrWhiteSpace(text) Then Return ""

		'         ' Экранируем специальные символы в маркерах
		'         Dim escapedStart = Regex.Escape(startMarker)
		'         Dim escapedEnd = Regex.Escape(endMarker)

		'         Dim pattern = $"{escapedStart}\s*(.*?)\s*{escapedEnd}"
		'         Dim match = Regex.Match(text, pattern, RegexOptions.Singleline)

		'         If match.Success Then
		'             Return CleanText(match.Groups(1).Value)
		'         End If

		'         Return ""
		'     End Function

		'     Private Function ParseDelay(text As String) As Integer
		'         Dim minutes As Integer = 0

		'         ' Парсим часы
		'         Dim hoursMatch = Regex.Match(text, "(\d+)\s*ч\.?\s*")
		'         If hoursMatch.Success Then
		'             Integer.TryParse(hoursMatch.Groups(1).Value, minutes)
		'             minutes *= 60
		'         End If

		'         ' Парсим минуты
		'         Dim minsMatch = Regex.Match(text, "(\d+)\s*мин\.?\s*")
		'         If minsMatch.Success Then
		'             Dim m As Integer = 0
		'             Integer.TryParse(minsMatch.Groups(1).Value, m)
		'             minutes += m
		'         End If

		'         Return minutes
		'     End Function

		'     ''' <summary>
		'     ''' Очищает название сторонней организации от дублей и служебной информации
		'     ''' </summary>
		'     Private Function CleanThirdPartyOrg(text As String) As String

		'         If String.IsNullOrWhiteSpace(text) Then Return text

		'         ' Очистка от HTML-сущностей и спецсимволов пробела
		'         text = text.Replace("&nbsp;", " ").Replace(ChrW(160), " ").Trim()

		'         ' ШАГ 0: Отсекаем скобки со служебной информацией (станции приписки и т.д.)
		'         Dim mainText = text
		'         Dim parenPos = text.IndexOf("("c)
		'         If parenPos > 0 Then
		'             mainText = text.Substring(0, parenPos).Trim()
		'         End If

		'         ' ШАГ 1: Разбиваем по разделителю " - "
		'         Dim parts = mainText.Split(New String() {" - "}, StringSplitOptions.None).Select(Function(p) p.Trim()).ToArray()

		'         ' Если разделителя нет — возвращаем то, что осталось после очистки от скобок
		'         If parts.Length < 2 Then
		'             Return mainText
		'         End If

		'         Dim firstPart = parts(0)
		'         Dim secondPart = parts(1)

		'         ' ШАГ 2: Логика выбора результата

		'         ' 1. Проверка на дублирование (как в АО «ТМХ-ЛОКОМОТИВЫ» - АО «ТМХ-ЛОКОМОТИВЫ»)
		'         ' Сравниваем, предварительно удалив кавычки для точности
		'         If firstPart.Replace("«", "").Replace("»", "").Equals(
		'secondPart.Replace("«", "").Replace("»", ""), StringComparison.OrdinalIgnoreCase) Then
		'             Return firstPart
		'         End If

		'         ' 2. Если первая часть — холдинг (ЖЕЛДОРРЕММАШ), берем конкретный завод из второй части
		'         ' Сюда же можно добавить РЕМПУТЬМАШ через запятую
		'         Dim factoryHoldings = {"ЖЕЛДОРРЕММАШ", "РЕМПУТЬМАШ"}
		'         If factoryHoldings.Any(Function(h) firstPart.Contains(h)) Then
		'             Return secondPart
		'         End If

		'         ' 3. Если это сервисное депо (СЛД) — они обычно во второй части
		'         If secondPart.StartsWith("СЛД", StringComparison.OrdinalIgnoreCase) Then
		'             Return secondPart
		'         End If

		'         ' 4. Если в первой части ТМХ (но не дубль), то обычно это и есть головная компания
		'         If firstPart.Contains("ТМХ") Then
		'             Return firstPart
		'         End If

		'         ' 5. Запасной вариант: если ничего не подошло, возвращаем всю строку без скобок
		'         Return mainText
		'     End Function








		'     ''' <summary>
		'     ''' Парсит HTML-таблицу списка отказов и возвращает список записей
		'     ''' </summary>
		'     Public Function ParseJournalList(html As String) As List(Of JournalRecord)


		'         Dim doc As New HtmlAgilityPack.HtmlDocument()
		'         doc.OptionFixNestedTags = True
		'         doc.LoadHtml(html)

		'         Dim result As New List(Of JournalRecord)()
		'         Dim seenIds As New HashSet(Of String)()

		'         ' 🔑 КЛЮЧЕВОЕ: ищем таблицу по id="data_table"
		'         Dim table = doc.GetElementbyId("data_table")
		'         If table Is Nothing Then Return result

		'         Dim rows = table.SelectNodes(".//tr")
		'         If rows Is Nothing Then Return result

		'         ' Функция для чистого извлечения текста из ячейки
		'         Dim GetText = Function(cell As HtmlNode) As String
		'                           Dim raw = cell.InnerHtml
		'                           raw = raw.Replace("<br>", " ").Replace("<br/>", " ").Replace("<br />", " ")
		'                           raw = System.Text.RegularExpressions.Regex.Replace(raw, "<[^>]+>", "")
		'                           raw = raw.Replace("&nbsp;", " ").Replace(ChrW(160), " ")
		'                           Return System.Text.RegularExpressions.Regex.Replace(raw, "\s+", " ").Trim()
		'                       End Function

		'         For Each row In rows
		'             ' 🔑 Берём ViolId из id строки: <tr id="row15831622">
		'             Dim rowId = row.GetAttributeValue("id", "")
		'             If Not rowId.StartsWith("row") OrElse rowId = "table_header" Then Continue For

		'             Dim violId = rowId.Substring(3).Trim()
		'             If Not System.Text.RegularExpressions.Regex.IsMatch(violId, "^\d{7,9}$") Then Continue For
		'             If seenIds.Contains(violId) Then Continue For
		'             seenIds.Add(violId)

		'             Dim cells = row.SelectNodes(".//td")

		'             ' 🔍 отладочный лог структуры ячеек
		'             If result.Count = 0 Then
		'                 Try
		'                     Dim debugLines As New List(Of String)
		'                     debugLines.Add($"=== ROW ID: {rowId} ===")
		'                     If cells IsNot Nothing Then
		'                         debugLines.Add($"Cells count: {cells.Count}")
		'                         For i As Integer = 0 To Math.Min(cells.Count - 1, 20) ' первые 20 ячеек
		'                             Dim raw = cells(i).InnerHtml
		'                             Dim clean = raw.Replace("<br>", " ").Replace("<br/>", " ").Replace("<br />", " ")
		'                             clean = System.Text.RegularExpressions.Regex.Replace(clean, "<[^>]+>", "")
		'                             clean = clean.Replace("&nbsp;", " ").Replace(ChrW(160), " ")
		'                             clean = System.Text.RegularExpressions.Regex.Replace(clean, "\s+", " ").Trim()
		'                             debugLines.Add($"{i}: '{clean}'")
		'                         Next
		'                     Else
		'                         debugLines.Add("Cells = Nothing!")
		'                     End If
		'                     System.IO.File.WriteAllText(
		'             System.IO.Path.Combine(GetDebugFolder(), $"debug_row_structure_{violId}.txt"),
		'             String.Join(vbCrLf, debugLines),
		'             Encoding.UTF8)
		'                 Catch ex As Exception
		'                     ' Игнорируем ошибки отладки, чтобы не ломать основной парсинг
		'                 End Try
		'             End If
		'             ' 🔍 КОНЕЦ БЛОКА ОТЛАДКИ

		'             If cells Is Nothing OrElse cells.Count < 14 Then Continue For

		'             ' 🔑 Фиксированные индексы колонок (проверено на твоём HTML)
		'             Dim category = cells(6).InnerText.Trim()
		'             If category <> "1" AndAlso category <> "2" AndAlso category <> "3" Then Continue For

		'             Dim rec As New JournalRecord() With {
		'                 .ViolId = violId,
		'                 .Category = category,
		'                 .StartTime = GetText(cells(8)),
		'                 .EndTime = GetText(cells(9)),
		'                 .FromDept = GetText(cells(10)),
		'                 .ToDept = GetText(cells(11)),
		'                 .Location = (GetText(cells(12))), 'NormalizeForCompare(GetText(cells(12)), uniqueTrains:=True), 
		'                 .MestoOTS_TXT = .Location,
		'                 .Equipment = GetText(cells(13))
		'             }

		'             ' Иконки и ASU
		'             Dim rowHtml = row.InnerHtml
		'             Dim asuImg = cells(0).SelectSingleNode(".//img")
		'             Dim asuAlt = If(asuImg IsNot Nothing, asuImg.GetAttributeValue("alt", "").Trim(), "")
		'             rec.ASU = If(String.IsNullOrEmpty(asuAlt), "РУЧНОЙ ВВОД", asuAlt)

		'             rec.IsLocked = rowHtml.Contains("lock.gif", StringComparison.OrdinalIgnoreCase)
		'             rec.HasAttachments = rowHtml.Contains("attach.gif", StringComparison.OrdinalIgnoreCase)
		'             rec.NeedsAdditional = rowHtml.Contains("additional_investigation.gif", StringComparison.OrdinalIgnoreCase)
		'             rec.IsEasapr = rowHtml.Contains("easapr_rzd.gif", StringComparison.OrdinalIgnoreCase)

		'             ' Количество вложений из alt картинки
		'             Dim attachImg = cells(3).SelectSingleNode(".//img")
		'             If attachImg IsNot Nothing Then
		'                 Dim altText = attachImg.GetAttributeValue("alt", "")
		'                 Dim match = System.Text.RegularExpressions.Regex.Match(altText, "\((\d+)\)")
		'                 If match.Success Then Integer.TryParse(match.Groups(1).Value, rec.AttachCount)
		'             End If

		'             ' Скрытые поля (статус, кол-во поездов)
		'             Dim statusInput = row.SelectSingleNode($".//input[@name='viol{violId}_status']")
		'             If statusInput IsNot Nothing Then rec.Status = statusInput.GetAttributeValue("value", "")

		'             Dim trainInput = row.SelectSingleNode($".//input[@name='viol{violId}_train_cnt']")
		'             If trainInput IsNot Nothing Then Integer.TryParse(trainInput.GetAttributeValue("value", "0"), rec.TrainCount)

		'             result.Add(rec)
		'         Next

		'         Return result


		'     End Function


		'     '''' <summary>
		'     '''' Оптимизированная версия — принимает скомпилированный шаблон
		'     '''' </summary>
		'     'Private Function ExtractHiddenValueOptimized(html As String, patternBase As String, fieldValue As String) As String
		'     '    Dim pattern = String.Format(patternBase, Regex.Escape(fieldValue))
		'     '    Dim m = Regex.Match(html, pattern, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
		'     '    Return If(m.Success, m.Groups(1).Value.Trim(), "")
		'     'End Function

		'     Private Function HasNextPage(html As String, currentPage As Integer) As Boolean
		'         Dim nextNum As String = (currentPage + 1).ToString()

		'         ' 🔹 Ищем точные совпадения из HTML (в VB.NET кавычки внутри строк удваиваются: "")
		'         Dim patterns = {
		'             $"saveStatus('activePage', '{nextNum}'",
		'             $"saveStatus(""activePage"", ""{nextNum}""",
		'             $"activePage={nextNum}",
		'             $"]{nextNum}[",
		'             $">{nextNum}<"
		'         }

		'         For Each p In patterns
		'             If html.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0 Then Return True
		'         Next

		'         ' 🔹 Запасной вариант: ищем любые номера страниц в вызовах saveStatus
		'         Dim matches = Regex.Matches(html, "saveStatus\(['""]activePage['""],\s*['""]?(\d+)")
		'         For Each m As Match In matches
		'             Dim pageIdx As Integer
		'             If Integer.TryParse(m.Groups(1).Value, pageIdx) AndAlso pageIdx > currentPage Then
		'                 Return True
		'             End If
		'         Next

		'         Return False
		'     End Function



		'     ''' <summary>
		'     ''' Автоматически обходит все страницы журнала, собирает уникальные отказы
		'     ''' </summary>
		'     Public Async Function FetchAllJournalPagesAsync(baseJournalUrl As String, Optional progress As IProgress(Of Integer) = Nothing, Optional saveExcel As Boolean = False) As Task(Of List(Of JournalRecord))


		'         '================ это ускоренный вариант С ТАЙМИНГОМ В ЛОГЕ ===================

		'         Dim allRecords As New List(Of JournalRecord)
		'         Dim seenIds As New HashSet(Of String)
		'         Dim page As Integer = 1
		'         Dim logPath = System.IO.Path.Combine(GetDebugFolder(), "fetch_log.txt")
		'         Dim logLock As New Object()
		'         Dim sw As New Stopwatch() ' ⏱ Таймер для замеров

		'         Dim LogWrite = Sub(msg As String)
		'                            SyncLock logLock
		'                                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}{vbCrLf}", Encoding.UTF8)
		'                            End SyncLock
		'                        End Sub

		'         LogWrite($"🚀 Старт загрузки: {baseJournalUrl}")
		'         Dim reportTab As String = Nothing
		'         Dim baseUrl As String = ""
		'         Try
		'             ' 1️⃣ Загружаем базовую страницу
		'             LogWrite("📄 Загрузка базовой страницы...")
		'             Dim initialResponse = Await _httpClient.GetAsync(baseJournalUrl)
		'             initialResponse.EnsureSuccessStatusCode()
		'             Dim initialHtml = Await initialResponse.Content.ReadAsStringAsync()
		'             LogWrite($"✅ Базовая получена: {initialHtml.Length} байт")

		'             ' 🔥 Ищем ПРАВИЛЬНЫЙ tab
		'             reportTab = Nothing
		'             Dim docInit As New HtmlDocument()
		'             docInit.LoadHtml(initialHtml)
		'             Dim tabNode = docInit.DocumentNode.SelectSingleNode("//input[@name='tab']")
		'             If tabNode IsNot Nothing Then reportTab = tabNode.GetAttributeValue("value", "")

		'             If String.IsNullOrEmpty(reportTab) Then
		'                 Dim m = Regex.Match(initialResponse.RequestMessage.RequestUri.ToString(), "[?&]tab=(\d+)")
		'                 If m.Success Then reportTab = m.Groups(1).Value
		'             End If

		'             If String.IsNullOrEmpty(reportTab) Then
		'                 LogWrite("❌ Не найден параметр 'tab'.")
		'                 Return allRecords
		'             End If
		'             LogWrite($"🔑 reportTab = {reportTab}")

		'             baseUrl = initialResponse.RequestMessage.RequestUri.GetLeftPart(UriPartial.Authority)
		'             Dim saveStatusUrl = $"{baseUrl}/kasant/tabSaveStatus"
		'             Dim tableUrl = $"{baseUrl}/kasant/journal_table.jsp"

		'             ' 2️⃣ Цикл пагинации
		'             While True
		'                 LogWrite($"📥 Страница {page}: подготовка...")

		'                 ' 🔹 ЭТАП 1: Сообщаем серверу, какую страницу хотим (только для стр. 2+)
		'                 If page > 1 Then
		'                     LogWrite($"📤 Вызов tabSaveStatus для страницы {page}...")
		'                     Dim statusContent = New FormUrlEncodedContent(New Dictionary(Of String, String) From {
		'                         {"tab", reportTab},
		'                         {"activePage", page.ToString()},
		'                         {"operation", "update_journal_table_commit()"},
		'                         {"_", ""}
		'                     })

		'                     Using statusReq As New HttpRequestMessage(HttpMethod.Post, saveStatusUrl)
		'                         statusReq.Content = statusContent
		'                         statusReq.Headers.Referrer = New Uri($"{baseUrl}/kasant/journal.jsp?tab={reportTab}")
		'                         statusReq.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest")
		'                         statusReq.Headers.TryAddWithoutValidation("Accept", "text/javascript, text/html, application/xml, text/xml, */*")
		'                         statusReq.Headers.TryAddWithoutValidation("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7")
		'                         statusReq.Headers.TryAddWithoutValidation("X-Prototype-Version", "1.5.0")

		'                         Dim statusResp = Await _httpClient.SendAsync(statusReq)
		'                         Dim statusResult = Await statusResp.Content.ReadAsStringAsync()
		'                         LogWrite($"📡 Ответ tabSaveStatus: {statusResult.Trim()}")
		'                     End Using
		'                     Await Task.Delay(150)
		'                 End If

		'                 ' 🔹 ЭТАП 2: Получаем таблицу для сохранённой страницы + ⏱ ЗАМЕР ВРЕМЕНИ
		'                 LogWrite($"📥 Запрос таблицы для страницы {page}...")
		'                 Dim rndVal = DateTimeOffset.Now.ToUnixTimeMilliseconds()

		'                 Using tableReq As New HttpRequestMessage(HttpMethod.Post, $"{tableUrl}?tab={reportTab}")
		'                     tableReq.Content = New StringContent($"=undefined&rndval={rndVal}", Encoding.UTF8, "application/x-www-form-urlencoded")
		'                     tableReq.Headers.Referrer = New Uri($"{baseUrl}/kasant/journal.jsp?tab={reportTab}")
		'                     tableReq.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest")
		'                     tableReq.Headers.TryAddWithoutValidation("Accept", "*/*")
		'                     tableReq.Headers.TryAddWithoutValidation("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7")

		'                     ' ⏱ ЗАМЕР СЕТИ
		'                     sw.Restart()
		'                     Dim tableResp = Await _httpClient.SendAsync(tableReq)
		'                     'Dim html = Await tableResp.Content.ReadAsStringAsync()
		'                     Dim bytes = Await tableResp.Content.ReadAsByteArrayAsync()
		'                     Dim html = Encoding.GetEncoding("windows-1251").GetString(bytes)
		'                     sw.Stop()
		'                     Dim networkMs = sw.ElapsedMilliseconds

		'                     If Not tableResp.IsSuccessStatusCode OrElse String.IsNullOrWhiteSpace(html) Then Exit While

		'                     ' ⏱ ЗАМЕР ПАРСИНГА
		'                     sw.Restart()
		'                     Dim pageRecords = ParseJournalList(html)
		'                     sw.Stop()
		'                     Dim parseMs = sw.ElapsedMilliseconds

		'                     ' 📊 Вывод замеров в лог
		'                     LogWrite($"⏱ Стр. {page}: сеть {networkMs}мс | парсинг {parseMs}мс | найдено {pageRecords.Count}")

		'                     ' === >>> ВСТАВКА НАЧАЛО <<< ===
		'                     If pageRecords.Count = 0 Then
		'                         Dim emptyPath = System.IO.Path.Combine(GetDebugFolder(),
		'                   $"debug_empty_page{page}_{DateTime.Now:yyyyMMdd_HHmmss}.html")
		'                         System.IO.File.WriteAllText(emptyPath, html, Encoding.GetEncoding("windows-1251"))
		'                         LogWrite($"⚠ Пустая страница {page}, HTML сохранён: {emptyPath}")
		'                         Exit While
		'                     End If
		'                     ' === >>> ВСТАВКА КОНЕЦ <<< ===


		'                     'If pageRecords.Count = 0 Then Exit While

		'                     ' 🔹 Добавление уникальных
		'                     Dim addedCount As Integer = 0
		'                     For Each rec In pageRecords
		'                         If seenIds.Contains(rec.ViolId) Then
		'                             ' LogWrite($"⚠ Дубль: {rec.ViolId} (стр. {page})") ' Закомментировано, чтобы не спамить лог
		'                         Else
		'                             seenIds.Add(rec.ViolId)
		'                             allRecords.Add(rec)
		'                             addedCount += 1
		'                         End If
		'                     Next
		'                     LogWrite($"➕ Добавлено: {addedCount}. Всего: {allRecords.Count}")
		'                     progress?.Report(allRecords.Count)

		'                     If Not HasNextPage(html, page) Then
		'                         LogWrite($"🏁 Последняя страница: {page}")
		'                         Exit While
		'                     End If
		'                 End Using

		'                 page += 1
		'                 Await Task.Delay(200)
		'             End While

		'         Catch ex As Exception
		'             LogWrite($"💥 Ошибка: {ex.Message}")
		'             System.Diagnostics.Debug.WriteLine(ex.ToString())
		'         End Try

		'         ' ========================================================================
		'         ' 🔹 3️⃣ СКАЧИВАНИЕ EXCEL (если запрошено и мы знаем tab)
		'         ' ========================================================================
		'         If saveExcel AndAlso Not String.IsNullOrEmpty(reportTab) AndAlso Not String.IsNullOrEmpty(baseUrl) Then
		'             Try
		'                 LogWrite("📥 Начало скачивания Excel-отчета...")

		'                 ' Формируем URL как в HAR-файле
		'                 Dim excelUrl = $"{baseUrl}/kasant/Journal_list?tab={reportTab}&presentation=excel"
		'                 Dim refererUrl = $"{baseUrl}/kasant/journal.jsp?tab={reportTab}"

		'                 Using excelReq As New HttpRequestMessage(HttpMethod.Get, excelUrl)
		'                     ' Обязательно указываем Referer, иначе сервер может отказать
		'                     excelReq.Headers.Referrer = New Uri(refererUrl)
		'                     excelReq.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/vnd.ms-excel,*/*")
		'                     excelReq.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36")

		'                     Dim excelResp = Await _httpClient.SendAsync(excelReq)

		'                     If excelResp.IsSuccessStatusCode Then
		'                         Dim excelBytes = Await excelResp.Content.ReadAsByteArrayAsync()

		'                         ' Получаем путь к рабочему столу текущего пользователя
		'                         Dim desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
		'                         Dim fileName = "1. 4Отч - новый.xls"
		'                         Dim fullPath = System.IO.Path.Combine(desktopPath, fileName)

		'                         ' Сохраняем файл
		'                         System.IO.File.WriteAllBytes(fullPath, excelBytes)
		'                         LogWrite($"✅ Excel успешно сохранён на рабочий стол: {fullPath} ({excelBytes.Length} байт)")
		'                     Else
		'                         LogWrite($"⚠️ Ошибка скачивания Excel. Статус: {excelResp.StatusCode}")
		'                     End If
		'                 End Using

		'             Catch ex As Exception
		'                 LogWrite($"💥 Ошибка при сохранении Excel: {ex.Message}")
		'             End Try
		'         End If



		'         LogWrite($"🎉 Завершено! Всего: {allRecords.Count}")
		'         Return allRecords
		'     End Function


		'     ''' <summary>
		'     ''' Загружает и парсит отчёт о состоянии расследования отказов
		'     ''' </summary>
		'     Public Async Function FetchInvestigationReportAsync(reportUrl As String) As Task(Of List(Of InvestigationReportItem))

		'         Try
		'             Dim debugFolder = GetDebugFolder()

		'             ' Просто загружаем HTML
		'             Dim response = Await _httpClient.GetAsync(reportUrl)
		'             response.EnsureSuccessStatusCode()

		'             Dim bytes = Await response.Content.ReadAsByteArrayAsync()
		'             Dim html = Encoding.GetEncoding("windows-1251").GetString(bytes)

		'             ' Сохраняем для отладки
		'             Dim fileName = If(reportUrl.Contains("dt_nd"), "investigation_report_current.html", "investigation_report_previous.html")
		'             System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, fileName), html, Encoding.GetEncoding("windows-1251"))

		'             ' Парсим и возвращаем
		'             Return ParseInvestigationReport(html)

		'         Catch ex As Exception
		'             Dim debugFolder = GetDebugFolder()
		'             System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, "investigation_report_error.txt"),
		'     $"Ошибка: {ex.Message}{vbCrLf}{vbCrLf}{ex.StackTrace}")
		'             Return New List(Of InvestigationReportItem)()
		'         End Try

		'     End Function





		'     Public Async Function FetchDepotReportAsync(reportUrl As String,
		'                                     Optional isPrevious As Boolean = False,
		'                                     Optional filterSLD As Boolean = False) As Task(Of List(Of InvestigationReportItem))
		'         Try
		'             Dim debugFolder = GetDebugFolder()

		'             ' 1. Первый запрос
		'             Dim response = Await Fetcher.HttpClient.GetAsync(reportUrl)
		'             response.EnsureSuccessStatusCode()
		'             Dim bytes = Await response.Content.ReadAsByteArrayAsync()
		'             Dim html = Encoding.GetEncoding("windows-1251").GetString(bytes)

		'             ' 2. 🔑 ПРОВЕРКА НА РАЗЛОГИН (если пришла форма входа)
		'             If html.Contains("anauth_panel") OrElse html.Contains("id_prog") Then
		'                 MW.InfoBLOK.AddItem("⚠ Сессия КАСАНТ неактивна. Выполняю автоматический вход...")
		'                 Dim loginOk = Await Fetcher.EnsureConnectedAsync()

		'                 If Not loginOk Then
		'                     MW.InfoBLOK.AddItem("❌ Не удалось войти в КАСАНТ. Проверьте логин/пароль в настройках.")
		'                     Return New List(Of InvestigationReportItem)()
		'                 End If

		'                 ' 3. Повторяем запрос после успешного входа
		'                 MW.InfoBLOK.AddItem("✅ Вход выполнен. Повторяю загрузку отчёта...")
		'                 response = Await Fetcher.HttpClient.GetAsync(reportUrl)
		'                 response.EnsureSuccessStatusCode()
		'                 bytes = Await response.Content.ReadAsByteArrayAsync()
		'                 html = Encoding.GetEncoding("windows-1251").GetString(bytes)
		'             End If

		'             ' 4. Сохраняем для отладки
		'             Dim fileName = If(isPrevious, "depot_report_previous.html", "depot_report_current.html")
		'             System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, fileName), html, Encoding.GetEncoding("windows-1251"))

		'             ' 5. Парсим с учётом флага фильтрации СЛД
		'             Return ParseDepotReport(html, filterSLD)

		'         Catch ex As Exception
		'             Dim debugFolder = GetDebugFolder()
		'             System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, "depot_report_error.txt"),
		'     $"Ошибка: {ex.Message}{vbCrLf}{vbCrLf}{ex.StackTrace}")
		'             Return New List(Of InvestigationReportItem)()
		'         End Try

		'     End Function



		'End Class

		'====================================================================================================
		'====================================================================================================
		'====================================================================================================
		'====================================================================================================
		'====================================================================================================
		'====================================================================================================
		'====================================================================================================
		'====================================================================================================
		'====================================================================================================
		'====================================================================================================
		'====================================================================================================
		'====================================================================================================
		'====================================================================================================
		'====================================================================================================




		''' <summary>
		''' Показывает всплывающее окно со списком отказов. Закрывается при потере фокуса.
		''' </summary>
		Public Sub ShowJournalPopup(records As List(Of JournalRecord), placementTarget As UIElement, DKodd As Integer, Optional OverDue As Boolean = False, Optional Dangerous As Boolean = False, Optional Events As Boolean = False, Optional Corp As Boolean = False)
			'это окно для контрола
			Dim Itm As RailwayCodeItem = MW.JourParam.ParamPeriodCTL.DorTBlock.SelectedItem

			' Определяем заголовок окна
			Dim title As String
			If Dangerous Then
				title = $"Журнал КАСАНТ - Опасные отказы (без п.5.15), дорога - {Itm.Name}"
			ElseIf OverDue Then
				title = $"Журнал КАСАНТ - Принятые с нарушением срока, дорога - {Itm.Name}"
			ElseIf Events Then
				title = $"Журнал КАСАНТ - События, дорога - {Itm.Name}"
			ElseIf Corp Then
				title = $"Журнал КАСАНТ - Корпоративные нарушения, дорога - {Itm.Name}"
			Else
				title = $"Журнал КАСАНТ - Список отказов, дорога - {Itm.Name}"
			End If

			Dim journalWin As New Window() With {
		.Title = title,
		.Owner = MW,
		.WindowStartupLocation = WindowStartupLocation.CenterOwner,
		.Width = 1350,
		.MaxHeight = 1000,
		.ResizeMode = ResizeMode.CanResize
	}

			' Подхватываем фон из ресурсов, если есть
			Dim appBackBrush = TryCast(Application.Current.TryFindResource("AppBackBrush"), SolidColorBrush)
			If appBackBrush IsNot Nothing Then journalWin.Background = appBackBrush

			' Создаём контрол и грузим данные
			' Создаём контрол и грузим данные
			Dim viewer As New JournalViewerControl(DKodd)
			viewer.LoadData(records, OverDue OrElse Dangerous OrElse Corp OrElse Events)  ' Передаём флаг для кнопки

			journalWin.Content = viewer
			journalWin.Show()
		End Sub






		''' <summary>
		''' Формирует ссылку на журнал КАСАНТ с указанными датами и фильтром статусов
		''' </summary>
		''' <param name="dateFrom">Дата и время начала периода</param>
		''' <param name="dateTo">Дата и время окончания периода</param>
		''' <param name="statusFilter">Статусы через запятую. По умолчанию: "ВСЕГО" (0,1,2,6,7,10)</param>
		Public Function BuildJournalUrl(dateFrom As DateTime, dateTo As DateTime, startHour As Integer, endHour As Integer, DKod As Integer, Optional endMin As Integer = 0, Optional statusFilter As String = "0,1,2,6,7,10") As String

			' Уникальный timestamp для обхода кэша сервера/прокси
			Dim tmpUnik = Math.Floor((DateTime.UtcNow - New DateTime(1970, 1, 1)).TotalMilliseconds)

			' ✅ Получаем код дороги из свойства (Integer) и форматируем в 2 цифры: 5 -> "05", 88 -> "88"
			' Если свойство равно 0 или не задано, подставим "88" как запасной вариант (Красноярская)
			Dim dorKod As String = If(DKod > 0, DKod.ToString("00"), "88")

			MW.InfoBLOK.AddItem($"дорога {DKod} c {startHour:00}ч {dateFrom:dd.MM.yyyy} по {endHour:00}ч {endMin:00}мин {dateTo:dd.MM.yyyy}{vbCrLf}{DKod}")

			Return $"{Fetcher._baseUrl}/OpenJournal?open_type=reports_list&dor_kod={DKod:00}&tmp_unik={tmpUnik}" &
		   $"&dt_nd={dateFrom:dd.MM.yyyy}&dt_nd_h={startHour:00}&dt_nd_min=00" &
		   $"&dt_kd={dateTo:dd.MM.yyyy}&dt_kd_h={endHour:00}&dt_kd_min={endMin:00}" &
		   $"&rep_asu=0,1,2,5,7,11,12,14,15,20,21,26,26&rep_asu_dop=1:1" &
		   $"&rep_service=150319,172450&kind_rep_type=1,2,3&dorGuiltyOrPlace={DKod:00}" &
		   $"&flg_alien_service=2&rep_status={statusFilter}"
		End Function


		''' <summary>
		''' Формирует ссылку на журнал КАСАНТ для отказов с нарушением срока
		''' </summary>
		Public Function BuildOverdueJournalUrl(dateFrom As DateTime, dateTo As DateTime,
	startHour As Integer, endHour As Integer, DKod As Integer,
	Optional endMin As Integer = 59) As String

			Dim tmpUnik = Math.Floor((DateTime.UtcNow - New DateTime(1970, 1, 1)).TotalMilliseconds)
			Dim dorKod = If(DKod > 0, DKod.ToString("00"), "88")

			' 🔑 Ключевое отличие: flg_overdue=1 и rep_status=1,2,7,10
			Return $"{Fetcher._baseUrl}/OpenJournal?open_type=reports_list&dor_kod={DKod:00}&tmp_unik={tmpUnik}" &
		   $"&dt_nd={dateFrom:dd.MM.yyyy}&dt_nd_h={startHour:00}&dt_nd_min=00" &
		   $"&dt_kd={dateTo:dd.MM.yyyy}&dt_kd_h={endHour:00}&dt_kd_min={endMin:00}" &
		   $"&rep_asu=0,1,2,5,7,11,12,14,15,20,21,26,26&rep_asu_dop=1:1" &
		   $"&rep_service=150319,172450&kind_rep_type=1,2,3&dorGuiltyOrPlace={DKod:00}" &
		   $"&flg_alien_service=2&flg_overdue=1&rep_status=1,2,7,10"
		End Function

		''' <summary>
		''' Формирует ссылку на журнал КАСАНТ для опасных отказов БЕЗ п.5.15
		''' </summary>
		Public Function BuildDangerousJournalUrl(dateFrom As DateTime, dateTo As DateTime,
	startHour As Integer, endHour As Integer, DKod As Integer,
	Optional endMin As Integer = 59) As String

			Dim tmpUnik = Math.Floor((DateTime.UtcNow - New DateTime(1970, 1, 1)).TotalMilliseconds)

			' 🔑 Ключевые параметры:
			' dangerous=1              — только опасные отказы
			' rep_status=2             — только расследованные
			' rep_status_among_road=2  — с учётом передачи между дорогами
			' flg_alien_service=4      — все виновные (дорожные + центральные + сторонние + п.5.15)
			' flg_id_cause_other=0     — ИСКЛЮЧАЕТ прочие причины (п.5.15 Положения)
			Return $"{Fetcher._baseUrl}/OpenJournal?open_type=reports_list&dor_kod={DKod:00}&tmp_unik={tmpUnik}" &
		   $"&dt_nd={dateFrom:dd.MM.yyyy}&dt_nd_h={startHour:00}&dt_nd_min=00" &
		   $"&dt_kd={dateTo:dd.MM.yyyy}&dt_kd_h={endHour:00}&dt_kd_min={endMin:00}" &
		   $"&rep_service=150319,172450&kind_rep_type=1,2,3" &
		   $"&dorGuiltyOrPlace={DKod:00}&flg_alien_service=4" &
		   $"&rep_status=2&rep_status_among_road=2&dangerous=1&flg_id_cause_other=0"
		End Function



		Public Function BuildEventsJournalUrl(dateFrom As DateTime, dateTo As DateTime,
	startHour As Integer, endHour As Integer, DKod As Integer,
	Optional endMin As Integer = 59) As String

			Dim tmpUnik = Math.Floor((DateTime.UtcNow - New DateTime(1970, 1, 1)).TotalMilliseconds)

			'  rep_conseq=154308 — Транспортное происшествие или событие
			Return $"{Fetcher._baseUrl}/OpenJournal?open_type=reports_list&dor_kod={DKod:00}&tmp_unik={tmpUnik}" &
		   $"&dt_nd={dateFrom:dd.MM.yyyy}&dt_nd_h={startHour:00}&dt_nd_min=00" &
		   $"&dt_kd={dateTo:dd.MM.yyyy}&dt_kd_h={endHour:00}&dt_kd_min={endMin:00}" &
		   $"&rep_asu=0,1,2,5,7,11,12,14,15,20,21,26,26&rep_asu_dop=1:1" &
		   $"&rep_service=150319,172450&kind_rep_type=1,2,3&dorGuiltyOrPlace={DKod:00}" &
		   $"&flg_alien_service=2&rep_conseq=154308&rep_status=0,1,2,6,7,10"
		End Function




		''' <summary>
		''' Формирует ссылку на журнал КАСАНТ для отказов с признаком "Корпоративное нарушение"
		''' Использует параметр rep_conseq=180373 (код корпоративного нарушения в системе КАСАНТ)
		''' </summary>
		''' <param name="dateFrom">Дата начала периода</param>
		''' <param name="dateTo">Дата окончания периода</param>
		''' <param name="startHour">Час начала</param>
		''' <param name="endHour">Час окончания</param>
		''' <param name="DKod">Код дороги</param>
		''' <param name="endMin">Минуты окончания (по умолчанию 59)</param>
		''' <returns>URL для загрузки журнала с фильтром по корпоративным нарушениям</returns>
		Public Function BuildCorpViolationJournalUrl(dateFrom As DateTime, dateTo As DateTime,
	startHour As Integer, endHour As Integer, DKod As Integer,
	Optional endMin As Integer = 59) As String

			Dim tmpUnik = Math.Floor((DateTime.UtcNow - New DateTime(1970, 1, 1)).TotalMilliseconds)
			Dim dorKod = If(DKod > 0, DKod.ToString("00"), "88")

			' 🔑 Ключевое отличие: rep_conseq=180373 (Корпоративное нарушение)
			' и rep_status=0,1,2,6,7,10 (все статусы)
			Return $"{Fetcher._baseUrl}/OpenJournal?open_type=reports_list&dor_kod={DKod:00}&tmp_unik={tmpUnik}" &
		   $"&dt_nd={dateFrom:dd.MM.yyyy}&dt_nd_h={startHour:00}&dt_nd_min=00" &
		   $"&dt_kd={dateTo:dd.MM.yyyy}&dt_kd_h={endHour:00}&dt_kd_min={endMin:00}" &
		   $"&rep_asu=0,1,2,5,7,11,12,14,15,20,21,26,26&rep_asu_dop=1:1" &
		   $"&rep_service=150319,172450&kind_rep_type=1,2,3&dorGuiltyOrPlace={DKod:00}" &
		   $"&flg_alien_service=2&rep_conseq=180373&rep_status=0,1,2,6,7,10"
		End Function








		''' <summary>
		''' Формирует ссылку на отчёт о состоянии расследования отказов
		''' </summary>
		Public Function BuildInvestigationReportUrl(dateFrom As DateTime, dateTo As DateTime,
	startHour As Integer, endHour As Integer,
	Optional endMin As Integer = 59) As String
			' DKod As Integer,
			'Dim tmpUnik = Math.Floor((DateTime.UtcNow - New DateTime(1970, 1, 1)).TotalMilliseconds)

			' Генерируем tmp_unik (timestamp)
			Dim tmpUnik = DateTimeOffset.Now.ToUnixTimeMilliseconds()

			' 🔑 ПРЯМОЙ ЗАПРОС К ОТЧЕТУ (не wait.jsp!)
			Return $"{Fetcher._baseUrl}/reports/new/Report2_4_1" &
		   $"?page=reports/new/Report2_4_1" &
		   $"&rep_nd={dateFrom:dd.MM.yyyy}&rep_nd_h={startHour:00}&rep_nd_min=00" &
		   $"&rep_kd={dateTo:dd.MM.yyyy}&rep_kd_h={endHour:00}&rep_kd_min={endMin:00}" &
		   $"&rep_conseq=null&rep_character=null" &
		   $"&kind_rep_type=1,2,3" &
		   $"&rep_service=150319,172450" &
		   $"&rep_subdivision=null&rep_nod0=null&rep_place0=null&rep_place0_id=null" &
		   $"&rep_stan0_1_id=null&rep_stan0_2_id=null&rep_object=null&nod_place=null" &
		   $"&rep_asu=0,1,2,5,7,11,12,14,15,20,21,26,26&rep_asu_dop=1:1" &
		   $"&four_report_save=null" &
		   $"&flg_alien_service=2" &
		   $"&usw={tmpUnik}&ush=1126"
		End Function


		' ========================================================================
		' 1. ФОРМИРОВАНИЕ URL ДЛЯ ОТЧЕТА ПО ДЕПО (Report 3_4_1)
		' ========================================================================
		Public Function BuildDepotReportUrl(dateFrom As DateTime, dateTo As DateTime,
									startHour As Integer, endHour As Integer,
									DKod As Integer, Optional endMin As Integer = 59) As String
			Dim tmpUnik = DateTimeOffset.Now.ToUnixTimeMilliseconds()
			Dim dorKodStr As String = If(DKod > 0, DKod.ToString("00"), "88")

			Return $"{Fetcher._baseUrl}/reports/new/Report3_4_1" &
		   $"?page=reports/new/Report3_4_1" &
		   $"&dor_kod={dorKodStr}" &
		   $"&tmp_unik={tmpUnik}" &
		   $"&dt_nd={dateFrom:dd.MM.yyyy}&dt_nd_h={startHour:00}&dt_nd_min=00" &
		   $"&dt_kd={dateTo:dd.MM.yyyy}&dt_kd_h={endHour:00}&dt_kd_min={endMin:00}" &
		   $"&rep_asu=0,1,2,5,7,11,12,14,15,20,21,26,26&rep_asu_dop=1:1" &
		   $"&rep_service=150319,172450" &
		   $"&kind_rep_type=1,2,3" &
		   $"&flg_alien=0" &
		   $"&flg_id_cause_other=0" &
		   $"&sls=172450" &
		   $"&flg_alien_service=0" &
		   $"&dor_kod_guilty={dorKodStr}" &
		   $"&usw={tmpUnik}&ush=990"
		End Function


		''' <summary>
		''' Формирует URL для ЦТ (174782)
		''' </summary>
		Public Function BuildSLDReportUrl_CT(dateFrom As DateTime, dateTo As DateTime,
									 startHour As Integer, endHour As Integer,
									 DKod As Integer, Optional endMin As Integer = 59) As String
			Dim tmpUnik = DateTimeOffset.Now.ToUnixTimeMilliseconds()
			Dim dorKodStr As String = If(DKod > 0, DKod.ToString("00"), "88")

			Return $"{Fetcher._baseUrl}/reports/new/Report3_4_1" &
		   $"?page=reports/new/Report3_4_1" &
		   $"&dor_kod={dorKodStr}" &
		   $"&tmp_unik={tmpUnik}" &
		   $"&dt_nd={dateFrom:dd.MM.yyyy}&dt_nd_h={startHour:00}&dt_nd_min=00" &
		   $"&dt_kd={dateTo:dd.MM.yyyy}&dt_kd_h={endHour:00}&dt_kd_min={endMin:00}" &
		   $"&rep_asu=0,1,2,5,7,11,12,14,15,20,21,26,26&rep_asu_dop=1:1" &
		   $"&rep_service=150301,150304,150305,150306,150307,150309,150310,150312,150313,150315,150316,150318,150319,150320,150321,172403,172450,172471,172473,173466,173473,179681,182951,300649,702022,702095,702096" &
		   $"&kind_rep_type=1,2,3" &
		   $"&flg_alien=-1" &
		   $"&flg_alien_service=3" &
		   $"&rep_cause_alien=174782" &  ' ТОЛЬКО ЦТ
		   $"&dor_kod_guilty={dorKodStr}" &
		   $"&usw={tmpUnik}&ush=990"
		End Function

		''' <summary>
		''' Формирует URL для ЦТР (174706)
		''' </summary>
		Public Function BuildSLDReportUrl_CTR(dateFrom As DateTime, dateTo As DateTime,
									  startHour As Integer, endHour As Integer,
									  DKod As Integer, Optional endMin As Integer = 59) As String
			Dim tmpUnik = DateTimeOffset.Now.ToUnixTimeMilliseconds()
			Dim dorKodStr As String = If(DKod > 0, DKod.ToString("00"), "88")

			Return $"{Fetcher._baseUrl}/reports/new/Report3_4_1" &
		   $"?page=reports/new/Report3_4_1" &
		   $"&dor_kod={dorKodStr}" &
		   $"&tmp_unik={tmpUnik}" &
		   $"&dt_nd={dateFrom:dd.MM.yyyy}&dt_nd_h={startHour:00}&dt_nd_min=00" &
		   $"&dt_kd={dateTo:dd.MM.yyyy}&dt_kd_h={endHour:00}&dt_kd_min={endMin:00}" &
		   $"&rep_asu=0,1,2,5,7,11,12,14,15,20,21,26,26&rep_asu_dop=1:1" &
		   $"&rep_service=150301,150304,150305,150306,150307,150309,150310,150312,150313,150315,150316,150318,150319,150320,150321,172403,172450,172471,172473,173466,173473,179681,182951,300649,702022,702095,702096" &
		   $"&kind_rep_type=1,2,3" &
		   $"&flg_alien=-1" &
		   $"&flg_alien_service=3" &
		   $"&rep_cause_alien=174706" &  ' ТОЛЬКО ЦТР
		   $"&dor_kod_guilty={dorKodStr}" &
		   $"&usw={tmpUnik}&ush=990"
		End Function


		''' <summary>
		''' Парсит таблицу. Если filterSLD = True, оставляет ТОЛЬКО ЦТ, ЦТР и ВСЕГО/ИТОГО.
		''' </summary>
		Public Function ParseDepotReport(html As String, Optional filterSLD As Boolean = False) As List(Of InvestigationReportItem)
			Dim result As New List(Of InvestigationReportItem)()
			Dim doc As New HtmlAgilityPack.HtmlDocument()
			doc.LoadHtml(html)

			Dim rows = doc.DocumentNode.SelectNodes("//tr")
			If rows Is Nothing Then Return result

			Dim orderCounter As Integer = 0
			For Each row In rows
				Dim cells = row.SelectNodes(".//td")
				If cells Is Nothing OrElse cells.Count < 6 Then Continue For

				Dim nameText = cells(0).InnerText.Trim().Replace("&nbsp;", "").Replace(ChrW(160), " ").Trim()
				nameText = Regex.Replace(nameText, "\s+", " ")

				' Пропускаем заголовки
				If nameText.Contains("Наименование структурного подразделения") OrElse
		   nameText.Contains("Отчёт о состоянии") OrElse
		   nameText.Contains("за период") Then
					Continue For
				End If

				If String.IsNullOrEmpty(nameText) Then Continue For

				orderCounter += 1

				'  ПРОСТАЯ ЛОГИКА: всегда берём cells(1), cells(2) и т.д.
				Dim total = ExtractNumberFromCell(cells(1))
				Dim accepted = ExtractNumberFromCell(cells(2))
				Dim overdue = ExtractNumberFromCell(cells(3))
				Dim investigated = ExtractNumberFromCell(cells(4))
				Dim notAccepted = ExtractNumberFromCell(cells(5))

				Dim isTotal As Boolean = nameText.ToUpper().Contains("ВСЕГО") OrElse nameText.ToUpper().Contains("ИТОГО")

				' ⭐ ФИЛЬТРАЦИЯ ЦШ (просто пропускаем строки с "ЦШ")
				If filterSLD AndAlso Not isTotal AndAlso (nameText.Contains("ЦШ") OrElse nameText.Contains("ЦСС")) Then
					Continue For
				End If

				' Пропускаем пустые депо, НО строку ВСЕГО оставляем всегда
				If Not isTotal AndAlso (Not total.HasValue OrElse total.Value = 0) Then
					Continue For
				End If

				result.Add(New InvestigationReportItem() With {
			.Name = nameText,
			.Section = "Структурные подразделения",
			.Order = orderCounter,
			.IsTotalRow = isTotal,
			.Total = If(total, 0),
			.Accepted = If(accepted, 0),
			.Overdue = If(overdue, 0),
			.Investigated = If(investigated, 0),
			.NotAccepted = If(notAccepted, 0)
		})
			Next
			Return result
		End Function


		''' <summary>
		''' Загружает отчёт по СЛД (ЦТ + ЦТР) из сети
		''' </summary>
		Public Async Function LoadSLDReportAsync(btn As Button) As Task
			Dim dateFrom = Fetcher.NachDat
			Dim dateTo = Fetcher.KonDat
			Dim startHour = Fetcher.NachTim
			Dim endHour = Fetcher.KonTim
			Dim endMin = Fetcher.KonMinut
			Dim dKod = Fetcher.DorOfOTS

			Dim curYear = dateTo.Year
			Dim prevYear = curYear - 1

			' ⭐ Формируем URL для ЦТ и ЦТР ОТДЕЛЬНО
			Dim urlCT_Current = BuildSLDReportUrl_CT(dateFrom, dateTo, startHour, endHour, dKod, endMin)
			Dim urlCTR_Current = BuildSLDReportUrl_CTR(dateFrom, dateTo, startHour, endHour, dKod, endMin)

			Dim urlCT_Previous = BuildSLDReportUrl_CT(dateFrom.AddYears(-1), dateTo.AddYears(-1), startHour, endHour, dKod, endMin)
			Dim urlCTR_Previous = BuildSLDReportUrl_CTR(dateFrom.AddYears(-1), dateTo.AddYears(-1), startHour, endHour, dKod, endMin)

			MW.InfoBLOK.AddItem("⏳ Загрузка отчёта по СЛД (ЦТ + ЦТР)...")

			Try
				' ⭐ Загружаем все 4 запроса параллельно
				Dim taskCT_Cur = Fetcher.FetchDepotReportAsync(urlCT_Current, False)
				Dim taskCTR_Cur = Fetcher.FetchDepotReportAsync(urlCTR_Current, False)
				Dim taskCT_Prev = Fetcher.FetchDepotReportAsync(urlCT_Previous, True)
				Dim taskCTR_Prev = Fetcher.FetchDepotReportAsync(urlCTR_Previous, True)

				Await Task.WhenAll(taskCT_Cur, taskCTR_Cur, taskCT_Prev, taskCTR_Prev)

				' ⭐ Объединяем результаты ЦТ и ЦТР
				Dim dataCur = taskCT_Cur.Result.Concat(taskCTR_Cur.Result).ToList()
				Dim dataPrev = taskCT_Prev.Result.Concat(taskCTR_Prev.Result).ToList()

				' Сортируем СЛД в нужном порядке
				SortSLDReport(dataCur)
				SortSLDReport(dataPrev)

				MergeAndShowReport(dataCur, dataPrev, curYear, prevYear, btn)
				MW.InfoBLOK.AddItem($"✅ Отчёт по СЛД загружен. Найдено подразделений: {dataCur.Count}")

			Catch ex As Exception
				MW.InfoBLOK.AddItem($"❌ Ошибка загрузки отчёта по СЛД: {ex.Message}")
			End Try

		End Function


		''' <summary>
		''' Сортирует список СЛД в заданном порядке. ВСЕГО всегда уходит в конец.
		''' Пересчитывает поле Order после сортировки!
		''' </summary>
		Public Sub SortSLDReport(items As List(Of InvestigationReportItem))
			MW.InfoBLOK.AddItem($"🔍 Сортировка СЛД. Всего строк: {items.Count}")

			' Словарь приоритетов
			Dim sortOrder As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase) _
			From {
			{"БОГОТОЛ", 1},
			{"КРАСНОЯРСК", 2},
			{"ИЛАНСК", 3},
			{"АЧИНСК", 4},
			{"АБАКАН", 5}
			}

			' Разделяем: обычные строки и строка ВСЕГО
			Dim regularItems = items.Where(Function(x) Not x.IsTotalRow).ToList()
			Dim totalItems = items.Where(Function(x) x.IsTotalRow).ToList()

			MW.InfoBLOK.AddItem($"  Обычных строк: {regularItems.Count}, ВСЕГО: {totalItems.Count}")

			' 🔍 ОТЛАДКА: выводим что нашли ДО сортировки
			For Each item In regularItems
				Dim foundKey = ""
				Dim priority As Integer = 999
				For Each kvp In sortOrder
					If item.Name.ToUpper().Contains(kvp.Key) Then
						foundKey = kvp.Key
						priority = kvp.Value
						Exit For
					End If
				Next
				MW.InfoBLOK.AddItem($"  - {item.Name} | Приоритет: {priority} ({If(foundKey, "НЕ НАЙДЕН")}) | Старый Order={item.Order}")
			Next

			' Сортируем обычные строки по приоритету
			Dim sorted = regularItems.
		OrderBy(Function(x)
					For Each kvp In sortOrder
						If x.Name.ToUpper().Contains(kvp.Key) Then
							Return kvp.Value
						End If
					Next
					Return 999
				End Function).
		ThenBy(Function(x) x.Name). ' Если приоритет одинаковый, сортируем по имени
		ToList()

			' ⭐ ПЕРЕСЧИТЫВАЕМ ORDER ПОСЛЕ СОРТИРОВКИ!
			For i As Integer = 0 To sorted.Count - 1
				sorted(i).Order = i + 1
			Next

			' Пересобираем список: сначала отсортированные депо, потом ВСЕГО
			items.Clear()
			items.AddRange(sorted)
			items.AddRange(totalItems)

			MW.InfoBLOK.AddItem($"✅ После сортировки (новый Order):")
			For Each item In items
				MW.InfoBLOK.AddItem($"  [{item.Order}] {item.Name} | IsTotal={item.IsTotalRow}")
			Next
		End Sub

		' ========================================================================
		' 5. ГЛАВНЫЙ МЕТОД ВЫЗОВА (ПОВЕСТЬ НА КНОПКУ)
		' ========================================================================
		''' <summary>
		''' Точка входа: загружает отчёт по депо за текущий и прошлый год, сравнивает и показывает таблицу
		''' </summary>
		Public Async Function LoadDepotReportAsync(btn As Button) As Task

			Dim dateFrom = Fetcher.NachDat
			Dim dateTo = Fetcher.KonDat
			Dim startHour = Fetcher.NachTim
			Dim endHour = Fetcher.KonTim
			Dim endMin = Fetcher.KonMinut
			Dim dKod = Fetcher.DorOfOTS

			Dim curYear = dateTo.Year
			Dim prevYear = curYear - 1

			' Формируем URL для текущего и прошлого года
			Dim urlCurrent = BuildDepotReportUrl(dateFrom, dateTo, startHour, endHour, dKod, endMin)
			Dim urlPrevious = BuildDepotReportUrl(dateFrom.AddYears(-1), dateTo.AddYears(-1), startHour, endHour, dKod, endMin)

			MW.InfoBLOK.AddItem("⏳ Загрузка отчёта по структурным подразделениям...")

			Try
				' Параллельная загрузка для ускорения
				Dim taskCur = Fetcher.FetchDepotReportAsync(urlCurrent, False)
				Dim taskPrev = Fetcher.FetchDepotReportAsync(urlPrevious, True)
				Await Task.WhenAll(taskCur, taskPrev)

				Dim dataCur = taskCur.Result
				Dim dataPrev = taskPrev.Result

				' ⭐ ИСПОЛЬЗУЕМ ВАШ СУЩЕСТВУЮЩИЙ МЕТОД СЛИЯНИЯ И ПОКАЗА!
				MergeAndShowReport(dataCur, dataPrev, curYear, prevYear, btn)

				MW.InfoBLOK.AddItem($"✅ Отчёт по депо загружен. Найдено подразделений: {dataCur.Count}")

			Catch ex As Exception
				MW.InfoBLOK.AddItem($"❌ Ошибка загрузки отчёта по депо: {ex.Message}")
			End Try
		End Function



		''' <summary>
		''' Загружает и парсит отчет по депо из локального HTML-файла, 
		''' а затем показывает его через ваш стандартный MergeAndShowReport
		''' </summary>
		Public Sub LoadDepotReportFromFile(btn As Button)

			Dim dlg As New Microsoft.Win32.OpenFileDialog()
			dlg.Filter = "HTML файлы (*.html;*.htm)|*.html;*.htm|Все файлы (*.*)|*.*"
			dlg.Title = "Выберите сохраненный HTML-файл отчета"

			If dlg.ShowDialog() <> True Then Return

			Try
				Dim html = System.IO.File.ReadAllText(dlg.FileName, Encoding.GetEncoding("windows-1251"))

				' 🔑 Определяем, это файл СЛД или ТЧЭ, по содержимому
				Dim isSLDFile As Boolean = html.Contains("Сервисные организации ЦТ") OrElse html.Contains("СЛД")

				' Передаём флаг фильтрации сразу в парсер!
				Dim currentData = ParseDepotReport(html, filterSLD:=isSLDFile)

				If currentData.Count = 0 Then
					MW.InfoBLOK.AddItem("⚠ В файле не найдено данных.")
					Return
				End If

				If isSLDFile Then
					SortSLDReport(currentData)
					MW.InfoBLOK.AddItem("✅ Загружен и отфильтрован отчёт по СЛД (ЦШ исключён)")
				Else
					Dim regularItems = currentData.Where(Function(x) Not x.IsTotalRow).OrderBy(Function(x) x.Order).ToList()
					Dim totalItems = currentData.Where(Function(x) x.IsTotalRow).ToList()
					currentData.Clear()
					currentData.AddRange(regularItems)
					currentData.AddRange(totalItems)
					MW.InfoBLOK.AddItem("✅ Загружен отчёт по ТЧЭ")
				End If

				Dim curYear = If(Fetcher.KonDat.Year > 2000, Fetcher.KonDat.Year, DateTime.Now.Year)
				Dim prevYear = curYear - 1

				MergeAndShowReport(currentData, New List(Of InvestigationReportItem)(), curYear, prevYear, btn)

			Catch ex As Exception
				MW.InfoBLOK.AddItem($"❌ Ошибка чтения файла: {ex.Message}")
			End Try


		End Sub






		''' <summary>
		''' Парсит HTML-таблицу отчёта о состоянии расследования отказов
		''' Извлекает данные из строк, где есть хотя бы одна ячейка с числом-ссылкой
		''' Исключает дублирующую строку "Итого по сервисным"
		''' </summary>
		Public Function ParseInvestigationReport(html As String) As List(Of InvestigationReportItem)


			Dim result As New List(Of InvestigationReportItem)
			Dim doc As New HtmlAgilityPack.HtmlDocument()
			doc.LoadHtml(html)

			Dim rows = doc.DocumentNode.SelectNodes("//tr")
			If rows Is Nothing Then Return result

			' ⭐ Текущая секция
			Dim currentSection As String = ""
			Dim inRegionsSection As Boolean = False
			Dim prevRowName As String = ""
			Dim orderCounter As Integer = 0

			For Each row In rows
				Dim cells = row.SelectNodes(".//td")
				If cells Is Nothing Then Continue For

				Dim firstCellText As String = cells(0).InnerText.Trim()
				If String.IsNullOrEmpty(firstCellText) Then Continue For

				firstCellText = Regex.Replace(firstCellText, "\s+", " ")
				firstCellText = firstCellText.Replace("&nbsp;", "").Trim()

				' ⭐ ОПРЕДЕЛЯЕМ СЕКЦИИ ПО ЗАГОЛОВКАМ
				If firstCellText.Contains("С разделением территориально по регионам") Then
					inRegionsSection = True
					currentSection = "Регионы"
					prevRowName = firstCellText
					Continue For
				End If

				If firstCellText.Contains("С разделением по виновным службам/дирекциям дорожного подчинения") Then
					inRegionsSection = False
					currentSection = "Виновные службы/дирекции"
					prevRowName = firstCellText
					Continue For
				End If

				If firstCellText.Contains("С разделением по виновным дирекциям центрального подчинения") Then
					currentSection = "Виновные дирекции центрального подчинения"
					prevRowName = firstCellText
					Continue For
				End If

				If firstCellText.Contains("С разделением по виновным ДЗО") Then
					currentSection = "Виновные ДЗО"
					prevRowName = firstCellText
					Continue For
				End If

				If firstCellText.Contains("С разделением по виновности других организаций и прочих причин") Then
					currentSection = "Прочие организации и причины"
					prevRowName = firstCellText
					Continue For
				End If

				If cells.Count < 6 Then
					prevRowName = firstCellText
					Continue For
				End If

				Dim name As String = firstCellText
				If String.IsNullOrEmpty(name) Then Continue For
				If name.Length < 2 Then Continue For

				' ⭐ ПРОПУСКАЕМ РЕГ-1 И РЕГ-2
				If name.Contains("РЕГ-1") OrElse name.Contains("РЕГ-2") Then
					prevRowName = name
					Continue For
				End If

				' ⭐ ПРОПУСКАЕМ ВСЕГО в разделе регионов
				If (name = "ВСЕГО" OrElse name.Contains("ВСЕГО")) Then
					If inRegionsSection Then
						prevRowName = name
						Continue For
					End If
					If prevRowName.Contains("РЕГ-1") OrElse prevRowName.Contains("РЕГ-2") Then
						prevRowName = name
						Continue For
					End If
				End If

				' ⭐ Пропускаем пустые строки-заглушки
				If name.Length <= 3 AndAlso Not firstCellText.Contains("href") Then
					Dim hasLink = cells(0).InnerHtml.Contains("href")
					If Not hasLink Then
						prevRowName = name
						Continue For
					End If
				End If

				Dim totalNum = ExtractNumberFromCell(cells(1))
				If Not totalNum.HasValue Then
					prevRowName = name
					Continue For
				End If
				If totalNum.Value = 0 Then
					prevRowName = name
					Continue For
				End If

				Dim acceptedNum = ExtractNumberFromCell(cells(2))
				Dim overdueNum = ExtractNumberFromCell(cells(3))
				Dim investigatedNum = ExtractNumberFromCell(cells(4))
				Dim notAcceptedNum = ExtractNumberFromCell(cells(5))

				orderCounter += 1

				Dim item As New InvestigationReportItem() With {
					.Name = name,
					.Section = currentSection,  ' ⭐ СОХРАНЯЕМ СЕКЦИЮ
					.Order = orderCounter,
					.Total = If(totalNum.HasValue, totalNum.Value, 0),
					.Accepted = If(acceptedNum.HasValue, acceptedNum.Value, 0),
					.Overdue = If(overdueNum.HasValue, overdueNum.Value, 0),
					.Investigated = If(investigatedNum.HasValue, investigatedNum.Value, 0),
					.NotAccepted = If(notAcceptedNum.HasValue, notAcceptedNum.Value, 0)
				}

				' Данные за прошлый год
				If cells.Count >= 11 Then
					Dim totalPY = ExtractNumberFromCell(cells(6))
					Dim acceptedPY = ExtractNumberFromCell(cells(7))
					Dim overduePY = ExtractNumberFromCell(cells(8))
					Dim investigatedPY = ExtractNumberFromCell(cells(9))
					Dim notAcceptedPY = ExtractNumberFromCell(cells(10))

					item.Total_PY = If(totalPY.HasValue, totalPY.Value, 0)
					item.Accepted_PY = If(acceptedPY.HasValue, acceptedPY.Value, 0)
					item.Overdue_PY = If(overduePY.HasValue, overduePY.Value, 0)
					item.Investigated_PY = If(investigatedPY.HasValue, investigatedPY.Value, 0)
					item.NotAccepted_PY = If(notAcceptedPY.HasValue, notAcceptedPY.Value, 0)
				End If

				result.Add(item)
				prevRowName = name
			Next

			Return result
		End Function

		Private Function ExtractNumberFromCell(cell As HtmlNode) As Integer?
			If cell Is Nothing Then Return Nothing

			Dim html = cell.InnerHtml

			' 1. Число в ссылке
			Dim links = cell.SelectNodes(".//a")
			If links IsNot Nothing Then
				For Each link In links
					Dim num As Integer
					If Integer.TryParse(link.InnerText.Trim(), num) Then
						Return num
					End If
				Next
			End If

			' 2. Число в квадратных скобках [123]
			Dim match = Regex.Match(html, "\[(\d+)\]")
			If match.Success Then
				Dim num As Integer
				If Integer.TryParse(match.Groups(1).Value, num) Then
					Return num
				End If
			End If

			' 3. Просто число
			match = Regex.Match(html, "\b(\d+)\b")
			If match.Success Then
				Dim num As Integer
				If Integer.TryParse(match.Groups(1).Value, num) Then
					Return num
				End If
			End If

			Return Nothing
		End Function




		Public Sub ShowInvestigationReportPopup(records As List(Of InvestigationReportItem), placementTarget As UIElement, Optional curYear As Integer = 0, Optional prevYear As Integer = 0)
			Dim Itm As RailwayCodeItem = MW.JourParam.ParamPeriodCTL.DorTBlock.SelectedItem

			Dim reportWin As New Window() With {
		.Title = $"Отчёт о состоянии расследования отказов - {Itm.Name}",
		.Owner = MW,
		.WindowStartupLocation = WindowStartupLocation.CenterOwner,
		.SizeToContent = SizeToContent.WidthAndHeight,
		.MaxWidth = SystemParameters.WorkArea.Width * 0.95,
		.MaxHeight = SystemParameters.WorkArea.Height * 0.9,
		.ResizeMode = ResizeMode.CanResizeWithGrip
	}

			Dim appBackBrush = TryCast(Application.Current.TryFindResource("AppBackBrush"), SolidColorBrush)
			If appBackBrush IsNot Nothing Then reportWin.Background = appBackBrush

			Dim viewer As New InvestigationReportViewerControl()

			' Если годы не переданы, берём из Fetcher.KonDat
			If curYear = 0 Then curYear = If(Fetcher.KonDat.Year > 2000, Fetcher.KonDat.Year, DateTime.Now.Year)
			If prevYear = 0 Then prevYear = curYear - 1

			viewer.LoadData(records, curYear, prevYear)

			reportWin.Content = viewer
			reportWin.Show()
		End Sub





		''' <summary>
		''' Объединяет данные текущего и прошлого года, показывает отчёт в popup
		''' </summary>
		Public Sub MergeAndShowReport(currentData As List(Of InvestigationReportItem),
							  previousYearData As List(Of InvestigationReportItem),
							  curYear As Integer,
							  prevYear As Integer,
							  btn As Button)

			' ⭐ КЛЮЧ: Section + Name (а не просто Name)
			Dim pyDict As New Dictionary(Of String, InvestigationReportItem)()
			For Each pyItem In previousYearData
				Dim key = $"{pyItem.Section}|{pyItem.Name}"
				pyDict(key) = pyItem
			Next

			' ⭐ Обновляем текущие элементы данными из прошлого года
			For Each currentItem In currentData
				Dim key = $"{currentItem.Section}|{currentItem.Name}"
				If pyDict.ContainsKey(key) Then
					Dim pyItem = pyDict(key)
					currentItem.Total_PY = pyItem.Total
					currentItem.Accepted_PY = pyItem.Accepted
					currentItem.Overdue_PY = pyItem.Overdue
					currentItem.Investigated_PY = pyItem.Investigated
					currentItem.NotAccepted_PY = pyItem.NotAccepted
					pyDict.Remove(key)
				End If
			Next

			' ⭐ Добавляем элементы, которые есть только в прошлом году
			' Важно: сохраняем их Section!
			For Each pyItem In pyDict.Values
				currentData.Add(New InvestigationReportItem With {
					.Name = pyItem.Name,
					.Section = pyItem.Section,  ' ⭐ СОХРАНЯЕМ СЕКЦИЮ
					.Order = pyItem.Order,
					.Total_PY = pyItem.Total,
					.Accepted_PY = pyItem.Accepted,
					.Overdue_PY = pyItem.Overdue,
					.Investigated_PY = pyItem.Investigated,
					.NotAccepted_PY = pyItem.NotAccepted
				})
			Next

			' ⭐ СОРТИРОВКА: сначала по секциям, потом по порядку
			Dim orderedData = currentData.
				OrderBy(Function(x) GetSectionOrder(x.Section)).
				ThenBy(Function(x) x.Order).
				ToList()

			' ⭐ Показываем в попупе с группировкой по секциям
			ShowInvestigationReportPopup(orderedData, btn, curYear, prevYear)


		End Sub

		''' <summary>
		''' Порядок секций для сортировки
		''' </summary>
		Private Function GetSectionOrder(section As String) As Integer
			Select Case section
				Case "Регионы"
					Return 1
				Case "Виновные службы/дирекции"
					Return 2
				Case "Виновные дирекции центрального подчинения"
					Return 3
				Case "Виновные ДЗО"
					Return 4
				Case "Прочие организации и причины"
					Return 5
				Case Else
					Return 99
			End Select
		End Function










		'=================================================================================================================
		'=================================================================================================================
		'=================================================================================================================
		'=================================================================================================================
		'=================================================================================================================
		'=================================================================================================================
		'=================================================================================================================


		''' <summary>
		''' Обрабатывает отказы, полученные напрямую из веб-парсера КАСАНТ.
		''' Полностью заменяет импорт из Excel (Load4ReportOld).
		''' </summary>
		Public Sub ProcessWebJournalData(records As List(Of JournalRecord))
			If records Is Nothing OrElse records.Count = 0 Then
				MW.InfoBLOK.AddItem("КАС АНТ: данных не получено")
				Return
			End If

			' --- НОВОЕ: Список для хранения пар Старый ID -> Новый ID и ссылки на объект ---
			Dim changedIdsList As New ObservableCollection(Of IdMappingInfo)()
			' ---------------------------------------------------------------------------



			' Определяем первую дату отчёта (аналог ws.Cells(4, 4))
			Dim DateOfFirstOTS As Date = records.Where(Function(r) Not String.IsNullOrEmpty(r.StartTime)) _
												.Select(Function(r) Set4ToDate(r.StartTime, ShowTime:=True).Date) _
												.DefaultIfEmpty(Date.MinValue).Min()

			Dim States As New List(Of String) From {"ачато расслед", "ередан друго", "азначен", "ринят к уч"}

			For Each rec In records
				Dim id = rec.ViolId
				If String.IsNullOrEmpty(id) OrElse id.Length < 5 Then Continue For

				' ——— Маппинг полей из JournalRecord (аналог чтения ячеек Excel) ———
				Dim AsuWeb As String = If(rec.ASU, "")
				Dim KtoZakrWeb As String = Clean_TCH_TR(rec.ToDept)
				Dim MestoOTS_TXTWeb As String = rec.Location



				'-----------------------



				Dim KatWeb As Integer = 0 : Integer.TryParse(rec.Category, KatWeb)
				Dim IstochnikWeb As String = If(rec.FromDept, "")
				Dim NachWeb As Date = Set4ToDate(rec.StartTime, ShowTime:=True)
				Dim OTSLev3Web As String = If(rec.Equipment, "")

				Dim StatusWeb As String = GetStatusText(rec.Status)
				' ——— Найти или создать ———
				Dim o = OTSList.FirstOrDefault(Function(x) x.Id = id)
				Dim isNew As Boolean = False
				Dim notes As New List(Of String)
				Dim NeedAddNoteFlg As Boolean = False

				'==========================================================================
				' Если по новому ID не нашли, пробуем найти СТАРУЮ запись по дате и месту
				'======================== новое
				Dim isDubl As Boolean = False
				Dim isoldId As String = ""

				If o Is Nothing Then
					'======================== новое
					isDubl = True
					'========================

					Dim existingO = OTSList.FirstOrDefault(Function(x) x.Nach = NachWeb AndAlso
				 NormalizeForCompare(x.MestoOTS_TXT, stripTrains:=True) = NormalizeForCompare(rec.Location, stripTrains:=True) AndAlso Not String.IsNullOrWhiteSpace(x.MestoOTS_TXT))

					If existingO IsNot Nothing Then

						' НАШЛИ СТАРУЮ ЗАПИСЬ!
						Dim oldId As String = existingO.Id

						' Добавляем информацию в наш список для показа в окне
						changedIdsList.Add(New IdMappingInfo With {
										   .OldId = oldId,
										   .NewId = id,
										   .TargetOtkaz = existingO
										   })

						'======================== новое
						isoldId = oldId 'передаем наружу для записи в новый отказ
						existingO.NewId = id
						existingO.AddUpdateNote("Изменен № ОТС")
						'========================
					End If
				End If
				'==========================================================================



				If o Is Nothing Then
					isNew = True
					o = New Otkaz()

					'======================= помечаем если это пришедший с изм № ОТС
					If isDubl Then
						If isoldId <> "" Then
							'o.ChangedOldId = isoldId
							'тут можно вставить ПЛАШКУ ActionUserControl с инфо (удаляемую) - вдруг ошибочно определилось..

						End If
						'сразу очищаем
						isoldId = ""
						isDubl = False
					End If
					'========================



					o.ZaKem = "!"
					o.Id = id
					o.Kat = KatWeb
					o.Nach = NachWeb
					o.KtoZakryl = KtoZakrWeb

					'If o.KtoZakryl.ToLower.Contains("трп") Then
					'    o.ZaKem = "тр"
					'End If
					o.MestoOTS_TXT = MestoOTS_TXTWeb 'NormalizeForCompare(rec.Location, uniqueTrains:=True)
					o.IsStation = Not (o.MestoOTS.Contains(" - "))

					o.OTSLev3 = OTSLev3Web

					If AsuWeb.ToLower.Contains("ручной ввод") AndAlso o.MestoOTS_Dor.ToLower.Contains("расноярс") Then
						o.Istochnik = "РУЧНОЙ ВВОД"
					Else
						o.Istochnik = IstochnikWeb

						''это автопростановка ВСЖД и т.д.
						'Dim newIstochnik As String = GetIstochnikOtkaza(o.MestoOTS, o.MestoOTS_TXT, o.KtoZakryl)
						'If Not String.IsNullOrEmpty(newIstochnik) Then o.Istochnik = newIstochnik
						'------------------------------------

						If Not IsRightIstocnik(o.Istochnik) Then NeedAddNoteFlg = True
					End If
					OTSList.Add(o)
				End If

				' ——— Логика пометок (оставлена 1:1 как в вашем коде) ———
				If isNew Then
					notes.Add("Дата поступления")
					If NeedAddNoteFlg Then notes.Add("От кого ОТС")
				Else
					If NachWeb <> o.Nach Then
						o.History.Add(New HistoryEntry With {.ShowDate = True, .IsRed = True, .EventDate = Now, .Description = $"Изменена дата начала с {o.Nach} на {NachWeb}"})
						o.Nach = NachWeb
					End If
					If o.Uslovie4 Then notes.Add("OK")
				End If

				If AsuWeb.ToLower.Contains("ручной ввод") Then
					If Not isNew AndAlso o.Istochnik <> "РУЧНОЙ ВВОД" AndAlso o.MestoOTS_Dor.ToLower.Contains("расноярс") Then
						notes.Add("Ручной ввод")
					End If
				End If

				'If (StatusWeb.ToLower.Contains("в еасапр") OrElse StatusWeb.ToLower.Contains("личие реклам")) AndAlso StatusWeb.ToLower.Contains("ачато расслед") Then
				If StatusWeb.ToLower.Contains("ачато расслед") Then
					If o.ZaKem = "" OrElse o.ZaKem = "!" OrElse o.ZaKemCode?.ToLower.Contains("тр") OrElse o.ZaKemCode?.Contains("орог") Then notes.Add("Сохранен")
				End If

				If States.Any(Function(s) StatusWeb?.ToLower().Contains(s) = True) AndAlso o.Zakryt > Date.MinValue Then
					notes.Add("Восстановлен")
				End If

				If Not isNew AndAlso o.ZaKemCode?.Contains("орог") Then notes.Add("Вернулся")
				If o.KtoZakryl <> KtoZakrWeb Then notes.Add($"за {KtoZakrWeb}?")

				'------------------------------------------------------------
				'а здесь уже по-человечески сравниваем через функцию без пробелов и т.п. (поезда не сбиваются в списке)
				Dim oldNorm As String = If(String.IsNullOrWhiteSpace(o.MestoOTS_TXT), "", NormalizeForCompare(o.MestoOTS_TXT, uniqueTrains:=True))
				Dim newNorm As String = NormalizeForCompare(rec.Location, uniqueTrains:=True)

				If oldNorm <> newNorm Then
					o.PreviousMestoOTS_TXT = oldNorm 'o.MestoOTS_TXT
					o.MestoOTS_TXT = newNorm 'MestoOTS_TXTWeb
					notes.Add("Испр место ОТС")
				End If
				'------------------------------------------------------------
				If o.Kat <> KatWeb Then notes.Add($"кат {KatWeb}")

				If StatusWeb.StartsWith("Расследован", StringComparison.Ordinal) Then
					If o.ZaKem = "" OrElse o.ZaKem = "!" OrElse o.IsSaved OrElse o.ZaKemCode?.Contains("орог") OrElse ((o.KtoZakryl.ToLower.Contains("трп") AndAlso o.Zakryt = Date.MinValue)) Then
						notes.Add("уже закрыт")
					End If
				End If

				o.UpdateNotes += String.Join("; ", notes)
			Next

			' ——— Проверка на устаревшие отказы (аналог цикла outdatedOtkazy) ———
			Dim idsInReport As New HashSet(Of String)(records.Where(Function(r) Not String.IsNullOrEmpty(r.ViolId) AndAlso r.ViolId.Length >= 5).Select(Function(r) r.ViolId))
			Dim outdatedOtkazy = OTSList.Where(Function(o) o.KomplexAsInt > 0 AndAlso Not idsInReport.Contains(o.Id) AndAlso o.Nach.Date >= DateOfFirstOTS)

			For Each o In outdatedOtkazy
				If o.IsSaved OrElse o.Zakryt > Date.MinValue Then o.AddUpdateNote("Восстановлен")
				If Not o.UpdateNotes.Contains("???") Then o.AddUpdateNote("???")

			Next

			MW.InfoBLOK.AddItem($"✅ Обработано {records.Count} отказов из веб-журнала. Excel больше не нужен.")
		End Sub




		Sub ShowChangeFRM(oldId, id, existingO)
			Dim changedIdsList As New ObservableCollection(Of IdMappingInfo)()
			' Добавляем информацию в наш список для показа в окне
			changedIdsList.Add(New IdMappingInfo With {
										   .OldId = oldId,
										   .NewId = id,
										   .TargetOtkaz = existingO
										   })
			Dim frm As New FrmChangedIds(changedIdsList,
												 onIdsSelectedAction:=Sub(oldnum, newId)
																		  ' 1. Фильтруем глобальный OTSList, оставляя только эти 2 записи
																		  Dim filteredList = OTSList.Where(Function(x) x.Id = oldnum OrElse x.Id = newId).ToList()

																		  ' 2. Вызываем ВАШУ процедуру, передавая ей готовый список из 2 элементов
																		  AddOTSToContainer(filteredList)
																		  MW.TRowsContainer.OTSContainer.ScrollIntoView(filteredList(0))
																	  End Sub,
												 onButtonClickAction:=Sub(oldnum, newId)
																		  Dim oldOtkaz = OTSList.FirstOrDefault(Function(x) x.Id = oldnum)
																		  Dim newOtkaz = OTSList.FirstOrDefault(Function(x) x.Id = newId)

																		  If oldOtkaz Is Nothing OrElse newOtkaz Is Nothing Then Return

																		  ' 1. Добавляем запись в историю
																		  Dim Zapis As String = $"Изменен № ОТС с {oldnum} на {newId}"
																		  oldOtkaz.AddHistoryEntry(DateTime.Now, Zapis)
																		  oldOtkaz.SelectedHistoryEntry.ShowDate = False

																		  ' 2. Удаляем НОВЫЙ отказ из глобального списка
																		  OTSList.Remove(newOtkaz)

																		  ' 3. Меняем ID у СТАРОГО отказа на новый
																		  oldOtkaz.Id = newId
																		  oldOtkaz.NewId = ""
																		  oldOtkaz.RemoveItemUpdateNote("!!!Изменен № ОТС")
																		  If oldOtkaz.ZaKem?.ToLower.Contains("дорог") Then
																			  oldOtkaz.AddUpdateNote("Вернулся")

																		  End If


																		  ' 4. Выполняем команду 1 (фильтрация). Останется только 1 запись
																		  Dim filteredList = OTSList.Where(Function(x) x.Id = oldnum OrElse x.Id = newId).ToList()
																		  PointedYarlyk = MW.YarlykContainer.AllOTSYAR
																		  AddOTSToContainer(filteredList)

																		  ' 5. Удаляем обработанную пару из ObservableCollection.
																		  ' Строка в таблице исчезнет АВТОМАТИЧЕСКИ вместе с кнопкой!
																		  Dim itemToRemove = changedIdsList.FirstOrDefault(Function(x) x.OldId = oldnum AndAlso x.NewId = newId)
																		  If itemToRemove IsNot Nothing Then
																			  changedIdsList.Remove(itemToRemove)
																		  End If

																		  MW.InfoBLOK.AddItem($"✅ Отказ {oldnum} объединен с {newId}")

																	  End Sub)
			frm.Owner = MW
			frm.Show()
		End Sub


		''' <summary>
		''' Загружает журнал отказов с признаком "Корпоративное нарушение"
		''' </summary>
		Public Async Function LoadCorpViolationJournalAsync(btn As Button) As Task
			Dim originalText = btn.Content.ToString()
			btn.Content = "⏳ Загрузка ..."
			btn.IsEnabled = False

			MW.InfoBLOK.AddItem("📋 Загрузка журнала: Корпоративные нарушения...")

			If Not Await Fetcher.EnsureConnectedAsync() Then
				btn.Content = originalText
				btn.IsEnabled = True
				Return
			End If

			Try
				' Формируем URL с фильтром по корпоративным нарушениям (rep_conseq=180373)
				Dim journalUrl = BuildCorpViolationJournalUrl(
			Fetcher.NachDat, Fetcher.KonDat,
			Fetcher.NachTim, Fetcher.KonTim,
			Fetcher.DorOfOTS, Fetcher.KonMinut)

				MW.InfoBLOK.AddItem($"  URL: {journalUrl.Substring(0, Math.Min(120, journalUrl.Length))}...")

				' Загружаем все страницы журнала
				Dim records = Await Fetcher.FetchAllJournalPagesAsync(journalUrl)

				If records.Count = 0 Then
					MW.InfoBLOK.AddItem("⚠ Отказы с признаком корпоративного нарушения не найдены")
					ShowMSG(MW, "Отказы с признаком корпоративного нарушения не найдены за выбранный период.", "КАСАНТ")
					Return
				End If

				' Показываем результат
				ShowJournalPopup(records, btn, Fetcher.DorOfOTS, Corp:=True)

				MW.InfoBLOK.AddItem($"✅ Загружено: {records.Count} отказов с признаком корпоративного нарушения")

			Catch ex As Exception
				MW.InfoBLOK.AddItem($"❌ Ошибка: {ex.Message}")
				ShowMSG(MW, $"Ошибка загрузки журнала: {ex.Message}", "КАСАНТ - Журнал", MessageBoxButton.OK, MessageBoxImage.Error)
			Finally
				btn.Content = originalText
				btn.IsEnabled = True
			End Try
		End Function





		Public Async Function LoadEventsJournalAsync(btn As Button) As Task
			Dim originalText = btn.Content.ToString()
			btn.Content = "⏳ Загрузка ..."
			btn.IsEnabled = False

			MW.InfoBLOK.AddItem($" [Журнал] События ")

			If Not Await Fetcher.EnsureConnectedAsync() Then
				btn.Content = originalText
				btn.IsEnabled = True
				Return
			End If

			Try
				Dim journalUrl = BuildEventsJournalUrl(Fetcher.NachDat, Fetcher.KonDat,
			Fetcher.NachTim, Fetcher.KonTim, Fetcher.DorOfOTS, Fetcher.KonMinut)

				Dim records = Await Fetcher.FetchAllJournalPagesAsync(journalUrl)
				If records.Count = 0 Then
					MW.InfoBLOK.AddItem("⚠ Отказы с признаком события не найдены")
					ShowMSG(MW, "Отказы с признаком события не найдены за выбранный период.", "КАСАНТ")
					Return
				End If
				ShowJournalPopup(records, btn, Fetcher.DorOfOTS, Events:=True)

				MW.InfoBLOK.AddItem($"  [Журнал] Загружено: {records.Count} событий")

			Catch ex As Exception
				ShowMSG(MW, $"Ошибка загрузки журнала: {ex.Message}", "КАСАНТ - Журнал", MessageBoxButton.OK, MessageBoxImage.Error)
			Finally
				btn.Content = originalText
				btn.IsEnabled = True
			End Try
		End Function


	End Module















	Public Class IdMappingInfo
		Implements INotifyPropertyChanged
		Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

		Protected Sub OnPropertyChanged(<CallerMemberName> Optional propertyName As String = Nothing)
			RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
		End Sub

		Public Property OldId As String
		Public Property NewId As String
		Public Property TargetOtkaz As New Otkaz

		Private _isReady As Boolean
		Public Property IsReady As Boolean
			Get
				Return _isReady
			End Get
			Set(value As Boolean)
				If _isReady <> value Then
					_isReady = value
					OnPropertyChanged() ' Уведомляет XAML об изменении
				End If
			End Set
		End Property
	End Class

















End Namespace