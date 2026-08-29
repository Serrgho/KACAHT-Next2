
Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Globalization
Imports System.IO
Imports System.Net
Imports System.Net.Http
Imports System.Runtime.CompilerServices
Imports System.Text
Imports System.Text.RegularExpressions
Imports HtmlAgilityPack

Namespace Kas

	Public Class KasantFetcher
		Implements INotifyPropertyChanged

		' 🔹 Скомпилированные Regex для парсинга скрытых полей (кэшируются)
		Private Shared ReadOnly _statusPatternBase As String = "name\s*=\s*[""']?viol{0}_status[""']?[^>]*?value\s*=\s*[""']([^""']*)[""']"
		Private Shared ReadOnly _trainPatternBase As String = "name\s*=\s*[""']?viol{0}_train_cnt[""']?[^>]*?value\s*=\s*[""']([^""']*)[""']"
		Private Shared ReadOnly _attachRegex As New Regex("attach\.gif.*?\((\d+)\)", RegexOptions.Compiled Or RegexOptions.IgnoreCase Or RegexOptions.Singleline)



		Private ReadOnly _httpClient As HttpClient
		Private ReadOnly _cookieContainer As New CookieContainer()
		Public ReadOnly _baseUrl As String = "http://kasant.gvc.oao.rzd:8888/kasant"

		' Событие: статус авторизации изменился
		Public Event ConnectionChanged()
		Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged




		' Поля дат (теперь private)
		Private _nachDat As Date = Date.MinValue
		Private _konDat As Date = Date.MinValue
		Private _nachTim As Integer = 0
		Private _konTim As Integer = 0
		Private _konMinut As Integer = 0
		Private _DorOfOTS As Integer = 88

		' Свойства с уведомлением об изменении
		Public Property NachDat As Date
			Get
				If _nachDat = Date.MinValue Then
					'Return Today.Date.AddDays(-1)
					If Today.Day < 13 Then
						Return New Date(Today.Year, Today.Month, 1).AddMonths(-1)
					Else
						Return New Date(Today.Year, Today.Month, 1)
					End If
				Else
					Return _nachDat
				End If

			End Get
			Set(value As Date)
				If _nachDat <> value Then
					_nachDat = value
					' Уведомляем, что NachKon_TXT изменился
					RaisePropertyChanged(NameOf(NachDat))
					RaisePropertyChanged(NameOf(NachKon_TXT))
				End If
			End Set
		End Property

		Public Property KonDat As Date
			Get
				If _konDat = Date.MinValue Then
					Return Today.Date
				Else
					Return _konDat
				End If

			End Get
			Set(value As Date)
				If _konDat <> value Then
					_konDat = value
					RaisePropertyChanged(NameOf(KonDat))
					RaisePropertyChanged(NameOf(NachKon_TXT))
				End If
			End Set
		End Property

		' Текст для кнопки (остаётся ReadOnly)
		Public ReadOnly Property NachKon_TXT As String
			Get
				' Свойства сами подставят "вчера" и "сегодня", если поля пусты
				Return $"{NachDat:dd.MM.yy} - {KonDat:dd.MM.yy}"
			End Get
		End Property


		Public Property NachTim As Integer
			Get
				Return _nachTim
			End Get
			Set(value As Integer)
				If _nachTim <> value Then
					_nachTim = value

					RaisePropertyChanged(NameOf(NachTim))
				End If
			End Set
		End Property

		Public Property KonTim As Integer
			Get
				Return _konTim
			End Get
			Set(value As Integer)
				If _konTim <> value Then
					_konTim = value
					' Уведомляем, что NachKon_TXT изменился
					RaisePropertyChanged(NameOf(KonTim))
				End If
			End Set
		End Property

		Public Property KonMinut As Integer
			Get
				Return _konMinut
			End Get
			Set(value As Integer)
				If _konMinut <> value Then
					_konMinut = value
					' Уведомляем, что NachKon_TXT изменился
					RaisePropertyChanged(NameOf(KonMinut))
				End If
			End Set
		End Property

		Public Property DorOfOTS As Integer
			Get
				Return _DorOfOTS
			End Get
			Set(value As Integer)
				If _DorOfOTS <> value Then
					_DorOfOTS = value
					' Уведомляем, что NachKon_TXT изменился
					RaisePropertyChanged(NameOf(DorOfOTS))
				End If
			End Set
		End Property


		Public Async Function EnsureConnectedAsync() As Task(Of Boolean)
			' 1. Если сессия уже живая — сразу выходим
			If Await Fetcher.IsLoggedInAsync() Then Return True

			' 2. Берём учётные данные (подставь свой источник, если не My.Settings)
			Dim user = MW.AuthCtrl.txtLogin.Text.Trim() 'My.Settings.KasantLogin
			Dim pass = MW.AuthCtrl.txtPassword.Password 'My.Settings.KasantPassword ' Если зашифрован, раскомментируй: DecryptPassword(...)




			If String.IsNullOrWhiteSpace(user) OrElse String.IsNullOrWhiteSpace(pass) Then
				'MessageBox.Show("Не заданы логин/пароль для подключения.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning)
				Return False
			End If

			' 3. Пробуем войти
			Dim success = Await Fetcher.LoginAsync(user, pass)
			If success Then
				My.Settings.Save()
				Return True
			Else
				'MessageBox.Show("Ошибка авторизации. Проверьте учётные данные.", "Вход", MessageBoxButton.OK, MessageBoxImage.Error)
				Return False
			End If
		End Function





		' Вспомогательный метод для raise события
		Protected Sub RaisePropertyChanged(<CallerMemberName> Optional propertyName As String = Nothing)
			RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
		End Sub

		' ← ← ← ПУБЛИЧНЫЙ ДОСТУП К HTTPCLIENT
		Public ReadOnly Property HttpClient As HttpClient
			Get
				Return _httpClient
			End Get
		End Property

		Public Sub New()
			Dim handler As New HttpClientHandler()
			handler.CookieContainer = _cookieContainer
			handler.AllowAutoRedirect = True
			handler.UseCookies = True


			_httpClient = New HttpClient(handler)
			_httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36")
			_httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")
			_httpClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9,en;q=0.8")
			_httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive")
			_httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1")

			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
		End Sub



		Public Async Function IsLoggedInAsync() As Task(Of Boolean)
			Try
				' Делаем легкий запрос к главной странице
				Dim response = Await _httpClient.GetAsync($"{_baseUrl}/index.jsp")
				Dim bytes = Await response.Content.ReadAsByteArrayAsync()
				Dim html = Encoding.GetEncoding("windows-1251").GetString(bytes)

				' Если нет панели входа и есть ключевые слова — мы всё еще в системе
				Return response.IsSuccessStatusCode AndAlso
		   Not html.Contains("anauth_panel") AndAlso
		   html.Contains("Журналы")
			Catch
				Return False
			End Try
		End Function


		Public Async Function LoginAsync(username As String, password As String) As Task(Of Boolean)
			Try
				Dim debugFolder = GetDebugFolder()

				' 1. Загружаем главную страницу для кук
				Dim mainPageResponse = Await _httpClient.GetAsync($"{_baseUrl}/index.jsp")
				Dim mainPageBytes = Await mainPageResponse.Content.ReadAsByteArrayAsync()
				Dim mainPageHtml = Encoding.GetEncoding("windows-1251").GetString(mainPageBytes)
				System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, "debug_main_page.html"), mainPageHtml, Encoding.GetEncoding("windows-1251"))

				' 2. Отправляем авторизацию
				Dim content = New FormUrlEncodedContent(New Dictionary(Of String, String) From
				{
					{"id_prog", "47"},
					{"action", "full_card.jsp"},
					{"dor_user", "88"},
					{"login", username},
					{"pass", password},
					{"save_password", "on"}
				})

				content.Headers.ContentType = New System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded")
				content.Headers.ContentType.CharSet = "windows-1251"

				'==============================================================================================
				System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, "debug_request_info.txt"),
					$"Отправляю POST на {_baseUrl}/login с логином: {username} в {DateTime.Now}")

				' Заголовки для имитации браузера
				_httpClient.DefaultRequestHeaders.Referrer = New Uri($"{_baseUrl}/index.jsp")
				_httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Origin", _baseUrl)
				_httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")
				_httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Charset", "windows-1251,utf-8;q=0.7")

				'==============================================================================================

				Dim response = Await _httpClient.PostAsync($"{_baseUrl}/login", content)

				' 3. Читаем ответ
				Dim loginBytes = Await response.Content.ReadAsByteArrayAsync()
				Dim resultHtml = Encoding.GetEncoding("windows-1251").GetString(loginBytes)

				System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, "debug_login_result.html"), resultHtml, Encoding.GetEncoding("windows-1251"))
				System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, "debug_response_status.txt"),
					$"Статус: {response.StatusCode}, Длина: {loginBytes.Length} байт в {DateTime.Now}")

				' === ВАРИАНТ 2: Если тело пустое — делаем повторный GET ===
				If String.IsNullOrWhiteSpace(resultHtml) OrElse loginBytes.Length = 0 Then
					System.IO.File.AppendAllText(System.IO.Path.Combine(debugFolder, "debug_login_result.html"),
						$"{vbCrLf}=== ПУСТОЕ ТЕЛО, ДЕЛАЕМ ПОВТОРНЫЙ GET ==={vbCrLf}")

					Try
						Dim followUpResponse = Await _httpClient.GetAsync($"{_baseUrl}/index.jsp")
						Dim followUpBytes = Await followUpResponse.Content.ReadAsByteArrayAsync()
						resultHtml = Encoding.GetEncoding("windows-1251").GetString(followUpBytes)

						System.IO.File.AppendAllText(System.IO.Path.Combine(debugFolder, "debug_login_result.html"),
							$"Повторный GET: {followUpBytes.Length} байт{vbCrLf}{vbCrLf}{resultHtml}",
							Encoding.GetEncoding("windows-1251"))
					Catch ex As Exception
						System.IO.File.AppendAllText(System.IO.Path.Combine(debugFolder, "debug_login_error.txt"),
							$"Ошибка повторного GET: {ex.Message}{vbCrLf}")
					End Try
				End If
				' =========================================================
				' 4. Проверка входа
				Dim isLogged = Not resultHtml.Contains("anauth_panel") AndAlso
							   (resultHtml.Contains("Журналы") OrElse
								resultHtml.Contains("Отчёты") OrElse
								resultHtml.Contains("session_invalidate") OrElse
								resultHtml.Contains("Смена пользователя")) ' resultHtml.Contains("Еремин")← Имя пользователя как запасной вариант
				If isLogged Then
					System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, "login_status.txt"), "ВХОД УСПЕШЕН")
					RaiseEvent ConnectionChanged() ' <--- Генерируем событие
					Return True
				Else
					System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, "login_status.txt"),
						$"ВХОД НЕ УДАЛСЯ{vbCrLf}Поиск маркеров:{vbCrLf}" &
						$"  anauth_panel: {resultHtml.Contains("anauth_panel")}{vbCrLf}" &
						$"  Журналы: {resultHtml.Contains("Журналы")}{vbCrLf}" &
						$"  session_invalidate: {resultHtml.Contains("session_invalidate")}") '& $"  Еремин{resultHtml.Contains("Еремин")}"
					Return False
					'Return False
				End If

			Catch ex As Exception
				Dim debugFolder = GetDebugFolder()
				System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, "debug_login_error.txt"),
					$"Ошибка: {ex.Message}{vbCrLf}{vbCrLf}{ex.StackTrace}")
				Return False
			End Try
		End Function


		Public Async Function FetchOtsDataAsync(violId As String, dorKod As Integer) As Task(Of OtsData)
			Try
				' 1. Формируем URL
				Dim url As String = $"{_baseUrl}/full_card.jsp?viol={violId}&dor_kod={dorKod:00}&tab=140142325"
				Dim debugFolder = GetDebugFolder()

				' Сохраняем URL для отладки
				System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, $"debug_url_{violId}.txt"),
		$"URL запроса: {url}{vbCrLf}Время: {DateTime.Now}")

				' 2. Делаем запрос (используем уже авторизованный _httpClient)
				Dim response = Await _httpClient.GetAsync(url)
				response.EnsureSuccessStatusCode()

				Dim bytes = Await response.Content.ReadAsByteArrayAsync()
				Dim html = Encoding.GetEncoding("windows-1251").GetString(bytes)

				' Сохраняем HTML для анализа
				Dim htmlPath = System.IO.Path.Combine(debugFolder, $"kasant_page_{violId}.html")
				System.IO.File.WriteAllText(htmlPath, html, Encoding.GetEncoding("windows-1251"))

				' 3. Проверка: не вылетела ли сессия?
				If html.Contains("anauth_panel") OrElse html.Contains("id_prog") Then
					' Если сессия протухла, можно либо выбросить ошибку, либо попробовать перелогиниться
					Throw New Exception("Сессия КАСАНТ завершена. Требуется повторный вход.")
				End If

				' 4. Парсим данные
				Return ParseHtml(html, violId)

			Catch ex As Exception
				' Логируем ошибку
				Dim debugFolder = GetDebugFolder()
				System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, $"fetch_error_{violId}.txt"),
		$"Ошибка: {ex.Message}{vbCrLf}{ex.StackTrace}")
				Throw
			End Try
		End Function


		Public log As New System.Text.StringBuilder()



		' ================= ФАКТИЧЕСКОЕ ВРЕМЯ НАЧАЛА =================
		''' <summary>
		''' Парсит блок "Фактическое время (ОТС выявлено в результате проверки достоверности учета)"
		''' </summary>
		Public Sub ParseFactTime(doc As HtmlDocument, result As OtsData)
			' ← ← ← КЛЮЧЕВОЕ: . вместо text()
			Dim headerNode = doc.DocumentNode.SelectSingleNode("//b[contains(., 'Фактическое время') and contains(., 'проверки достоверности')]")

			If headerNode Is Nothing Then
				Return
			End If

			' Ищем таблицу внутри того же div
			Dim container = headerNode.ParentNode
			Dim timeTable = container?.SelectSingleNode(".//table[.//b[contains(., 'Начало')]]")

			If timeTable Is Nothing Then
				log.AppendLine("⚠ Таблица времени НЕ найдена")
				Return
			End If

			' Ищем строку с данными (есть дата в формате dd.dd.dd)
			Dim dataRow = timeTable.SelectSingleNode(".//tr[td[nobr[contains(., '.')]]]")
			If dataRow Is Nothing Then Return

			Dim cells = dataRow.SelectNodes(".//td")
			If cells IsNot Nothing AndAlso cells.Count >= 4 Then
				Dim startTime = CleanText(cells(1)?.InnerText)
				Dim endTime = CleanText(cells(2)?.InnerText)
				Dim duration = CleanText(cells(3)?.InnerText)

				If Not String.IsNullOrWhiteSpace(startTime) Then result.FactStartTime = startTime
				If Not String.IsNullOrWhiteSpace(endTime) Then result.FactEndTime = endTime
				If Not String.IsNullOrWhiteSpace(duration) Then result.FactDuration = duration

				log.AppendLine($"✓ Факт. время: {startTime} - {endTime} ({duration})")
			End If
		End Sub






		' ================= ОПАСНЫЙ ОТКАЗ =================
		Public Sub ParseDangerOTS(doc As HtmlDocument, result As OtsData)
			' 1. Ищем конкретную ячейку <td>, которая содержит текст "опасного" 
			' И при этом находится ПЕРЕД таблицей с историей (ограничиваем область поиска верхом страницы)
			' Используем индекс или поиск по конкретным стилям из вашего примера
			Dim statusNode = doc.DocumentNode.SelectSingleNode("//td[@style[contains(.,'color: red')] and contains(., 'Присвоен признак «опасного»')]")

			' 2. Если нашли такую ячейку
			If statusNode IsNot Nothing Then
				' Берем текст ТОЛЬКО этой ячейки
				Dim txt = CleanText(statusNode.InnerText)

				' На всякий случай проверяем, не слишком ли длинный текст (защита от захвата всей таблицы)
				If txt.Length > 100 Then
					' Если текст огромный, значит XPath промахнулся, пробуем найти именно текст внутри
					result.DangerStatus = "Присвоен признак «опасного» отказа"
				Else
					result.DangerStatus = txt
				End If

				log.AppendLine($"✓ Статус: {result.DangerStatus}")
			Else
				result.DangerStatus = ""
				log.AppendLine("i Признак «опасного» не найден.")
			End If

		End Sub


		' ================= 5.15 ОТКАЗ =================
		Public Sub Parse515Reason(doc As HtmlDocument, result As OtsData)
			' Ищем ячейку td, которая содержит специфический текст пункта Положения
			Dim xpath As String = "//td[contains(text(), 'п.5.15 Положения')]"
			Dim reasonNode = doc.DocumentNode.SelectSingleNode(xpath)

			If reasonNode IsNot Nothing Then
				' Извлекаем текст и очищаем его от лишних пробелов/переносов строк
				Dim rawText As String = reasonNode.InnerText.Trim()

				' Записываем в свойство (CleanText — ваша вспомогательная функция для очистки)
				result.Is_5_15_OTS = CleanText(rawText)

				log.AppendLine($"✓ Причина 5.15 найдена: {result.Is_5_15_OTS}")
			Else
				log.AppendLine("⚠ Причина 5.15 (сбой ГЛОНАСС) в таблице НЕ найдена!")


			End If
		End Sub


		' ================= 8. ДОКУМЕНТЫ (Дополнительные материалы) =================
		Public Async Sub ParseDocs(doc As HtmlDocument, result As OtsData)

			' Выполняем тяжелый парсинг в фоновом потоке
			Await System.Threading.Tasks.Task.Run(Sub()
													  ' Ищем контейнер с материалами
													  Dim materialsDiv = doc.DocumentNode.SelectSingleNode("//b[contains(., 'Дополнительные материалы')]/ancestor::div[@style]")
													  If materialsDiv Is Nothing Then Return

													  ' Ищем все ссылки на скачивание файлов
													  Dim fileLinks = materialsDiv.SelectNodes(".//a[contains(@href, 'DownloadFile')]")
													  If fileLinks Is Nothing Then Return

													  For Each link In fileLinks
														  Dim href = link.GetAttributeValue("href", "")
														  Dim fileName = CleanText(link.InnerText)

														  ' Пропускаем пустые ссылки и иконки-заглушки
														  If String.IsNullOrWhiteSpace(fileName) OrElse fileName.ToLower().EndsWith(".gif") Then Continue For

														  ' Извлекаем размер файла (обычно в скобках рядом с названием)
														  Dim fileSize As String = ""
														  Dim rawFileText = CleanText(link.ParentNode?.InnerText)
														  If rawFileText.Contains("(") Then
															  fileSize = rawFileText.Split("("c).Last().Replace(")", "").Trim()
														  End If

														  Dim uploadDate = "", uploadedBy = "", commentText = ""

														  ' --- ЛОГИКА ПОИСКА КОММЕНТАРИЯ (ВВЕРХ ПО ТАБЛИЦЕ) ---

														  ' 1. Находим строку во ВНЕШНЕЙ таблице. 
														  ' Ищем предка tr, который является прямым ребенком (или через tbody) таблицы responsible_table
														  Dim outerFileRow = link.Ancestors("tr").FirstOrDefault(Function(r)
																													 Dim parentTable = r.Ancestors("table").FirstOrDefault()
																													 Return parentTable IsNot Nothing AndAlso parentTable.Id = "responsible_table"
																												 End Function)

														  ' ВАЖНО: ссылка на файл на самом деле находится во ВТОРОЙ строке блока (под датой).
														  ' Чтобы найти дату и комментарий, нам нужно подняться на одну значимую строку выше.
														  If outerFileRow IsNot Nothing Then
															  Dim allRows = outerFileRow.ParentNode.SelectNodes("./tr")
															  Dim currentIndex = allRows.IndexOf(outerFileRow)

															  ' Ищем строку с данными (она обычно сразу над строкой с файлом)
															  Dim infoRow As HtmlNode = Nothing
															  For i As Integer = currentIndex - 1 To 0 Step -1
																  Dim tempRow = allRows(i)
																  Dim testCells = tempRow.SelectNodes("./td")

																  ' Строка с инфо содержит 4 ячейки (Дата, Депо, ФИО, Комментарий)
																  If testCells IsNot Nothing AndAlso testCells.Count = 4 Then
																	  ' Проверка на дату в первой ячейке
																	  If testCells(0).InnerText.Contains(".") Then
																		  infoRow = tempRow
																		  Exit For
																	  End If
																  End If
															  Next

															  ' 4. Если нашли строку с данными — вытягиваем их
															  If infoRow IsNot Nothing Then
																  Dim tds = infoRow.SelectNodes("./td")
																  ' Дата и время
																  uploadDate = CleanText(tds(0).InnerText).Replace(vbCrLf, " ")
																  ' Подразделение + ФИО
																  Dim dept = CleanText(tds(1).InnerText)
																  Dim person = CleanText(tds(2).InnerText)
																  uploadedBy = $"{dept} {person}".Trim()
																  ' Тот самый комментарий "Для расследования..."
																  commentText = CleanText(tds(3).InnerText)
															  End If
														  End If

														  ' Формируем полный URL
														  Dim fullUrl = If(href.StartsWith("http"), href, _baseUrl.TrimEnd("/"c) & "/" & href.TrimStart("/"c))

														  ' Безопасно добавляем в результат (рекомендуется SyncLock, если result общий)
														  SyncLock result.AttachedFiles
															  result.AttachedFiles.Add(New AttachedFile With {
																					   .FileName = fileName,
																					   .DownloadUrl = fullUrl,
																					   .FileSize = fileSize,
																					   .UploadDate = uploadDate,
																					   .UploadedBy = uploadedBy,
																					   .Comment = commentText
																					   })
														  End SyncLock

														  log.AppendLine($"📎 {fileName} | Коммент: {If(String.IsNullOrEmpty(commentText), "[пусто]", commentText)}")
													  Next
												  End Sub)

		End Sub


		' ================= 0. СТАТУС ПЕРЕДАЧИ =================
		Public Sub ParseStatus(doc As HtmlDocument, result As OtsData)
			' ИСПРАВЛЕНО: проверяем оба возможных статуса
			Dim statusNode = doc.DocumentNode.SelectSingleNode("//td[contains(@style, 'color: darkgreen')]//b")
			If statusNode IsNot Nothing Then
				Dim statusText = statusNode.InnerText

				result.InvestigationStatus = CleanText(statusText)
				log.AppendLine($"✓ Статус: {result.InvestigationStatus}")

				If Not String.IsNullOrWhiteSpace(result.InvestigationStatus) Then
					If result.InvestigationStatus.ToLower().Contains("статус на другой дороге") Then
						Dim mat = Regex.Match(result.InvestigationStatus,
						"передан\s+(.+)\s+\d{2}\.\d{2}\.\d{4}") '"был\s+передан\s+(.+?)\s+\d{2}\.\d{2}\.\d{4}"

						If mat.Success Then
							result.PeredanOnOtherDor = $"передан {mat.Groups(1).Value.Trim()}"
							log.AppendLine($"✓ Передан на другую дорогу: {result.PeredanOnOtherDor}")
						End If
					End If
				End If

			End If
		End Sub


		' ================= 1. МЕСТО ОТКАЗА =================
		Public Sub ParseMestoOTS(doc As HtmlDocument, result As OtsData)
			' ИСПРАВЛЕНО: более точный поиск места
			Dim locationDiv = doc.DocumentNode.SelectSingleNode("//div[@id='locations_div']")
			If locationDiv IsNot Nothing Then
				Dim nobr = locationDiv.SelectSingleNode(".//nobr")
				If nobr IsNot Nothing Then
					result.Location = CleanText(nobr.InnerText)
					log.AppendLine($"✓ Место: {result.Location}")
				End If
			End If
		End Sub


		' ================= 2. ВРЕМЯ (НАЧАЛО / УСТРАНЕНИЕ / ПРОДОЛЖИТЕЛЬНОСТЬ) =================
		Public Sub ParseNachOTS(doc As HtmlDocument, result As OtsData)
			Dim timeDiv = doc.DocumentNode.SelectSingleNode("//b[contains(text(), 'Время')]/ancestor::div[@style][1]")
			If timeDiv IsNot Nothing Then
				' Ищем таблицу по заголовкам, а не по border
				Dim timeTable = timeDiv.SelectSingleNode(".//table[.//b[contains(text(), 'Начало')]]")
				If timeTable IsNot Nothing Then
					Dim dataRow = timeTable.SelectSingleNode(".//tr[td[nobr][3]]") ' 3 ячейки с данными
					If dataRow IsNot Nothing Then
						Dim cells = dataRow.SelectNodes(".//td[nobr]")
						If cells?.Count >= 3 Then
							result.StartTime = CleanText(cells(0).InnerText)
							result.EndTime = CleanText(cells(1).InnerText)
							result.Duration = CleanText(cells(2).InnerText)
						End If
					End If
				End If
			End If
		End Sub


		' ================= 5.1. ВРЕМЯ К УЧЕТУ (PCHasy) =================
		Public Sub ParsePCHUchet(doc As HtmlDocument, result As OtsData)
			' Ищем текст "к учету" и извлекаем то, что после него в скобках
			Dim fullText = doc.DocumentNode.InnerText
			Dim match = Regex.Match(fullText, "к учету\s+([^)]+)")
			If match.Success Then
				result.PCHasy = match.Groups(1).Value.Trim()
				log.AppendLine($"✓ Потери поездо-часов: {result.PCHasy}")
			End If
		End Sub


		' ================= ПАРСИНГ ОБОРУДОВАНИЯ (Oborud) =================
		Public Sub ParseOborud(doc As HtmlDocument, result As OtsData)
			Dim causeDiv = doc.DocumentNode.SelectSingleNode("//b[text()='Причина']/ancestor::div[@style][1]")
			If causeDiv IsNot Nothing Then
				' Ищем таблицу с заголовками "Отказавшее тех. средство", "Причины отказа"
				Dim reasonTable = causeDiv.SelectSingleNode(".//table[.//b[contains(text(), 'Отказавшее тех. средство')]]")
				If reasonTable IsNot Nothing Then
					' === Ищем ТОЛЬКО вложенную таблицу с иерархией (в ячейке под "Отказавшее тех. средство") ===
					' Структура: заголовки → строка данных → первая ячейка данных содержит <table> с иерархией
					Dim hierarchyCell = reasonTable.SelectSingleNode(".//tr[2]//td[2]//table")

					If hierarchyCell IsNot Nothing Then
						Dim boldNodes = hierarchyCell.SelectNodes(".//b")
						Dim equipParts As New List(Of String)

						If boldNodes IsNot Nothing AndAlso boldNodes.Count > 0 Then
							' === ЕСТЬ ЖИРНОЕ: собираем ВСЕ уровни иерархии из <td> ===
							result.IsKasantHierarchy = True  ' <--- ВАЖНО: помечаем успешный парсинг
							Dim allTd = hierarchyCell.SelectNodes(".//td")
							If allTd IsNot Nothing Then
								For Each td In allTd
									Dim txt = CleanText(td.InnerText)
									' Фильтруем мусор: пустоты, разделители, заголовки таблицы
									If Not String.IsNullOrWhiteSpace(txt) AndAlso txt <> "|" AndAlso
									   txt <> "Отказавшее тех. средство" AndAlso txt <> "Причины отказа" AndAlso txt <> "Комментарий" Then
										' Избегаем дубликатов (на всякий случай)
										If Not equipParts.Contains(txt) Then
											equipParts.Add(txt)
										End If
									End If
								Next
							End If
						Else
							' === НЕТ ЖИРНОГО: старый фоллбэк — последняя непустая ячейка ===
							Dim allTd = hierarchyCell.SelectNodes(".//td")
							If allTd IsNot Nothing Then
								For i As Integer = allTd.Count - 1 To 0 Step -1
									Dim txt = CleanText(allTd(i).InnerText)
									If Not String.IsNullOrWhiteSpace(txt) AndAlso txt <> "|" Then
										equipParts.Add(txt)
										Exit For
									End If
								Next
							End If
						End If

						' === Формируем итоговую строку ===
						If equipParts.Count > 0 Then
							result.Oborud = String.Join(" → ", equipParts)
							log.AppendLine($"✓ Оборудование: {result.Oborud}")
						End If
					End If


				End If
			End If
		End Sub


		' ================= 3. ОБЩЕЕ ОПИСАНИЕ ОТКАЗА =================
		Public Sub ParseCommonOpisOTS(doc As HtmlDocument, result As OtsData)
			Try
				'log.AppendLine("[DEBUG] === Начинаю парсинг Характера ===")

				' Ищем раздел "Характер"
				Dim characterDiv = doc.DocumentNode.SelectSingleNode("//b[text()='Характер']/ancestor::div[@style][1]")
				'log.AppendLine($"[DEBUG] characterDiv = {If(characterDiv IsNot Nothing, "НАЙДЕН", "Nothing")}")

				If characterDiv IsNot Nothing Then
					' === 1. Основной текст характера (Тип пометки, Причина ГИД, Отказавший локомотив) ===
					Dim infoCell = characterDiv.SelectSingleNode(".//td[contains(., 'Тип пометки') or contains(., 'Причина ГИД')]")
					'log.AppendLine($"[DEBUG] infoCell = {If(infoCell IsNot Nothing, "НАЙДЕН", "Nothing")}")

					If infoCell IsNot Nothing Then
						result.CharacterText = CleanTextWithBreaks(infoCell.InnerHtml)
						log.AppendLine($"✓ Характер: {result.CharacterText}")
					End If

					' === 2. Таблица: Отказавшее тех. средство | Проявление | Комментарий ===
					Dim charTable = characterDiv.SelectSingleNode(".//table[.//b[contains(text(), 'Отказавшее тех. средство')]]")
					'log.AppendLine($"[DEBUG] charTable = {If(charTable IsNot Nothing, "НАЙДЕН", "Nothing")}")

					If charTable IsNot Nothing Then
						Dim dataRows = charTable.SelectNodes(".//tr")

						If dataRows IsNot Nothing Then
							' === Ищем строку с данными (где есть 4 ячейки и во 2-й ячейке есть текст) ===
							Dim dataRow As HtmlNode = Nothing
							For Each row In dataRows
								Dim cells = row.SelectNodes(".//td")
								' Пропускаем, если ячеек меньше 4
								If cells IsNot Nothing AndAlso cells.Count >= 4 Then
									' Проверяем 2-ю ячейку (индекс 1) - там должно быть оборудование
									Dim cell2Text = CleanText(cells(1).InnerText)
									' Пропускаем заголовок и пустые строки
									If Not String.IsNullOrWhiteSpace(cell2Text) AndAlso Not cell2Text.Contains("Отказавшее тех. средство") Then
										dataRow = row
										Exit For
									End If
								End If
							Next

							If dataRow IsNot Nothing Then
								Dim cells = dataRow.SelectNodes(".//td")
								If cells IsNot Nothing AndAlso cells.Count >= 4 Then
									' Ячейка 1: Отказавшее тех. средство
									result.FailedEquipment = CleanText(cells(1).InnerText)
									log.AppendLine($"  ✓ Отказавшее средство: {result.FailedEquipment}")
									' Ячейка 2: Проявление отказа
									result.FailureManifestation = CleanText(cells(2).InnerText)
									log.AppendLine($"  ✓ Проявление: {result.FailureManifestation}")
									' Ячейка 3: Комментарий ← InnerHtml для <NLok=...>
									result.CharacterComment = CleanComment(cells(3).InnerHtml)
									log.AppendLine($"  ✓ Комментарий: {result.CharacterComment}")
								End If
							Else
								log.AppendLine("⚠ Не найдена строка с данными в таблице Характера")
							End If
						End If
					End If
				End If

			Catch ex As Exception
				log.AppendLine($"!!! ОШИБКА в парсинге Характера: {ex.GetType().Name}: {ex.Message}")
				log.AppendLine(ex.StackTrace)
			End Try
		End Sub


		' ================= 4. КАТЕГОРИИ =================
		Public Sub ParseKategorys(doc As HtmlDocument, result As OtsData)
			Dim cat1052 = doc.DocumentNode.SelectSingleNode("//td[contains(text(), '1052р')]/b")
			If cat1052 IsNot Nothing Then
				result.Category1052 = cat1052.InnerText.Trim()
				log.AppendLine($"✓ Категория 1052р: {result.Category775}")
			End If

			Dim cat775 = doc.DocumentNode.SelectSingleNode("//td[contains(text(), '775р')]/b")
			If cat775 IsNot Nothing Then
				result.Category775 = cat775.InnerText.Trim()
				log.AppendLine($"✓ Категория 775р: {result.Category775}")
			End If


			Dim cat1915 = doc.DocumentNode.SelectSingleNode("//td[contains(text(), '1915р')]/i")
			If cat1915 IsNot Nothing Then
				result.Category1915 = cat1915.InnerText.Trim()
				log.AppendLine($"✓ Категория 1915р: {result.Category1915}")
			End If
		End Sub


		' ================= 9. ВИНОВНАЯ ОРГАНИЗАЦИЯ =================
		Public Sub ParseVinovnikOrganization(doc As HtmlDocument, result As OtsData)
			' 1. Сначала пробуем стандартный поиск (сервисная/сторонняя организация)
			Dim alienTable = doc.DocumentNode.SelectSingleNode("//table[@id='alien_guilty_table']")
			If alienTable IsNot Nothing Then
				Dim orgRow = alienTable.SelectSingleNode(".//tr[td[contains(., 'Наименование предприятия') or contains(., 'Наименования сервисной')]]")
				If orgRow IsNot Nothing Then
					Dim cells = orgRow.SelectNodes(".//td")
					If cells IsNot Nothing AndAlso cells.Count >= 2 Then
						Dim rawText = CleanText(cells(1).InnerText)
						result.ThirdPartyOrg = CleanThirdPartyOrg(rawText)
						log.AppendLine($"✓ Отнесен на (сервис): {result.ThirdPartyOrg}")
						Exit Sub ' Нашли сервисную, выходим
					End If
				End If
			End If






			' 2. Если сервисную не нашли, проверяем на "эксплуатационный" характер
			' Ищем текст в документе
			' 2. Если сервисную не нашли, проверяем на "эксплуатационный" характер
			Dim isExploitation = doc.DocumentNode.InnerHtml.Contains("Характер причины отказа</b>: эксплуатационный")

			If isExploitation Then
				Dim respTable = doc.DocumentNode.SelectSingleNode("//table[@id='responsible_table']")
				If respTable IsNot Nothing Then
					' Ищем строку, в которой НЕТ атрибута background (заголовки обычно с ним) 
					' и которая содержит данные (больше 4 ячеек)
					Dim dataRow = respTable.SelectSingleNode(".//tr[not(td[@style[contains(., 'background')]]) and count(td) >= 4]")

					' Если XPath выше не сработал, берем просто последнюю строку таблицы
					If dataRow Is Nothing Then
						dataRow = respTable.SelectNodes(".//tr").LastOrDefault()
					End If

					If dataRow IsNot Nothing Then
						Dim cells = dataRow.SelectNodes("td")
						If cells IsNot Nothing AndAlso cells.Count >= 4 Then
							' Индекс 3 — это 4-я колонка "Подразделение"
							result.ThirdPartyOrg = CleanText(cells(3).InnerText)
							log.AppendLine($"✓ Отнесен на (эксплуатация): {result.ThirdPartyOrg}")
						End If
					End If
				End If
			End If


		End Sub


		' ================= 7. ПОСЛЕДСТВИЯ (СОБЫТИЯ, КОРПОРАТИВ И Т.Д.) =================
		Public Sub ParseSobytieKorpETC(doc As HtmlDocument, result As OtsData)
			Dim consequencesDiv = doc.DocumentNode.SelectSingleNode("//b[contains(text(), 'Последствия')]/ancestor::div[@style][1]")
			If consequencesDiv IsNot Nothing Then
				' Ищем все <li> элементы внутри блока
				Dim listItems = consequencesDiv.SelectNodes(".//li")

				If listItems IsNot Nothing Then
					Dim consequencesList As New List(Of String)

					For Each li In listItems
						Dim text = CleanText(li.ParentNode.InnerText)
						If Not String.IsNullOrWhiteSpace(text) Then
							consequencesList.Add(text)
							' Извлекаем текст о транспортном происшествии
							If text.ToLower().Contains("транспортное происшествие") Then
								result.Consequences = text
							End If

							If text.ToLower().Contains("орпоративное нарушение") Then
								result.Korporativ = text
							End If
						End If
					Next

					If consequencesList.Count > 0 Then
						log.AppendLine($"✓ Последствия: {consequencesList.Count} записей")
						If Not String.IsNullOrWhiteSpace(result.Consequences) Then
							log.AppendLine($"  📋 {result.Consequences.Substring(0, Math.Min(100, result.Consequences.Length))}...")
						End If
					End If
				End If
			End If
		End Sub


		' ================= 5. ЗАДЕРЖАННЫЕ ПОЕЗДА =================
		Public Sub ParseTrains(doc As HtmlDocument, result As OtsData)
			' ИСПРАВЛЕНО: более надежный парсинг поездов
			Dim conseqTable = doc.DocumentNode.SelectSingleNode("//table[@id='conseq_table']")
			If conseqTable IsNot Nothing Then
				' Ищем все строки, содержащие информацию о поездах
				Dim allRows = conseqTable.SelectNodes(".//tr")
				Dim i As Integer = 0

				While i < allRows.Count
					Dim row = allRows(i)
					Dim rowText = row.InnerText

					' Проверяем, содержит ли строка информацию о поезде
					If (rowText.Contains("Грузо") OrElse rowText.Contains("Пассажир") OrElse rowText.Contains("Пригородн") OrElse rowText.Contains("Прочие")) AndAlso rowText.Contains("№") Then

						Dim train = New TrainDelay()

						' Определяем тип поезда
						If rowText.Contains("Грузов") Then
							train.TrainType = "Грузовой"
						ElseIf rowText.Contains("Пассажир") Then
							train.TrainType = "Пассажирский"
						ElseIf rowText.Contains("Пригородн") Then
							train.TrainType = "Пригородный"
						ElseIf rowText.Contains("Прочие") Then
							train.TrainType = "Прочие"
						End If

						' ← ← ← ДОБАВИТЬ: Извлекаем маршрут и время из <nobr>
						Dim nobrNode = row.SelectSingleNode(".//nobr")
						If nobrNode IsNot Nothing Then
							Dim nobrText = CleanText(nobrNode.InnerText)
							' Убираем лишние пробелы и переносы
							nobrText = Regex.Replace(nobrText, "\s+", " ").Trim()

							' Если есть текст в скобках — это маршрут
							Dim routeMatch = Regex.Match(nobrText, "\(([^)]+)\)")
							If routeMatch.Success Then
								train.RouteInfo = routeMatch.Groups(1).Value.Trim()
								log.AppendLine($"Место: {train.RouteInfo}")
							End If
						End If

						' Ищем номер поезда
						Dim numMatch = Regex.Match(rowText, "№\s*(\d+)")
						If numMatch.Success Then
							train.TrainNumber = numMatch.Groups(1).Value
						End If

						' Проверяем, первый ли поезд (со звездочкой)
						Dim firstCell = row.SelectSingleNode(".//td[1]")
						If firstCell IsNot Nothing AndAlso firstCell.InnerHtml.Contains("*") AndAlso (firstCell.InnerHtml.Contains("color: red") OrElse firstCell.InnerHtml.Contains("color:red")) Then
							train.IsFirst = True
						End If

						' Ищем время задержки (может быть в этой же или следующей строке)
						Dim delay As String = ""
						Dim delayCell = row.SelectSingleNode(".//td[contains(., 'мин') or contains(., 'ч')]")
						If delayCell Is Nothing AndAlso i + 1 < allRows.Count Then
							delayCell = allRows(i + 1).SelectSingleNode(".//td[contains(., 'мин') or contains(., 'ч')]")
						End If

						If delayCell IsNot Nothing Then
							delay = CleanText(delayCell.InnerText)
							train.DelayMinutes = ParseDelay(delay)
						End If

						' Парсим локомотивы, если это первый поезд или поезд со звездочкой
						If train.IsFirst OrElse result.DelayedTrains.Count = 0 Then
							' Ищем панель с локомотивами
							For j As Integer = i To Math.Min(i + 2, allRows.Count - 1)
								Dim searchRow = allRows(j)
								Dim panelMatch = Regex.Match(searchRow.InnerHtml, "train(\d+)_locs_panel")

								If panelMatch.Success Then
									Dim panelId = $"train{panelMatch.Groups(1).Value}_locs_panel"
									Dim locoPanel = doc.DocumentNode.SelectSingleNode($"//table[@id='{panelId}']")

									If locoPanel IsNot Nothing Then
										Dim locoRows = locoPanel.SelectNodes(".//tr")
										If locoRows IsNot Nothing Then
											For Each locoRow In locoRows
												Dim locoText = locoRow.InnerText
												If locoText.Contains("Серия локомотива:") Then
													Dim loco = New LocoInfo()

													loco.Series = ExtractField(locoText, "Серия локомотива:", "Номер локомотива:")
													loco.Number = ExtractField(locoText, "Номер локомотива:", "Дорога приписки локомотива")
													loco.RealNumber = Regex.Match(result.CharacterComment, "<nlok=(\d+)").Groups(1).Value
													loco.Road = ExtractField(locoText, "Дорога приписки локомотива, МВПС (ССПС):", "Депо приписки локомотива, МВПС (ССПС):")
													loco.Depot = ExtractField(locoText, "Депо приписки локомотива, МВПС (ССПС):", "Дорога приписки бригады:")
													loco.CrewRoad = ExtractField(locoText, "Дорога приписки бригады:", "Депо приписки бригады:")
													loco.CrewDepot = ExtractField(locoText, "Депо приписки бригады:", "Табельный номер машиниста:")
													loco.Driver = ExtractField(locoText, "Фамилия машиниста (водителя):", "Время явки")

													If Not String.IsNullOrWhiteSpace(loco.Series) Then
														train.Locomotives.Add(loco)

													End If
												End If
											Next
										End If
									End If
									Exit For
								End If
							Next
						End If

						If Not String.IsNullOrWhiteSpace(train.TrainNumber) Then
							result.DelayedTrains.Add(train)
							log.AppendLine($"✓ Поезд №{train.TrainNumber} ({train.TrainType}), Задержка: {train.DelayMinutes} мин, Локомотивов: {train.Locomotives.Count}")
						End If
					End If

					i += 1
				End While
			End If
		End Sub


		' ================= 6. ИСТОРИЯ ОТКАЗА =================
		Public Sub ParseHistory(doc As HtmlDocument, result As OtsData)
			' ИСПРАВЛЕНО: улучшен парсинг истории
			Dim historyDiv = doc.DocumentNode.SelectSingleNode("//b[contains(text(), 'История')]/ancestor::div[@style][1]")
			If historyDiv IsNot Nothing Then
				Dim historyTable = historyDiv.SelectSingleNode(".//table[.//b[contains(text(), 'Когда')]]")
				If historyTable IsNot Nothing Then
					Dim historyRows = historyTable.SelectNodes(".//tr")

					' ← ← ← Список слов-заголовков, которые нужно пропускать
					Dim headerWords = {"Когда", "Кем", "АСУ", "ЧТО", "КОМУ", "Откуда", "Куда", "Действие", "Примечание", "Комментарий"}

					If historyRows IsNot Nothing Then
						For Each row In historyRows
							Dim cells = row.SelectNodes(".//td")

							If cells IsNot Nothing AndAlso cells.Count >= 2 Then
								Dim dateText = CleanText(If(cells(1)?.InnerText, ""))

								' ← ← ← ФИЛЬТР 1: Пропускаем, если текст содержит стоп-слово заголовка
								If headerWords.Any(Function(w) dateText.ToLower().Contains(w.ToLower())) Then
									Continue For
								End If

								' ← ← ← ФИЛЬТР 2: Требуем хотя бы 2 цифры подряд (признак даты)
								If Not Regex.IsMatch(dateText, "\d{2}") Then
									Continue For
								End If

								Dim record = New HistoryRecord()
								record.DateTime = dateText
								record.ActionType = If(cells.Count > 2, CleanText(cells(2)?.InnerText), "")
								record.From = If(cells.Count > 3, CleanText(cells(3)?.InnerText), "")
								record.ToDest = If(cells.Count > 4, CleanText(cells(4)?.InnerText), "")
								record.ByWhom = If(cells.Count > 5, CleanText(cells(5)?.InnerText), "")
								record.Comment = If(cells.Count > 6, CleanText(cells(6)?.InnerText), "")

								result.History.Add(record)
								log.AppendLine($"  [{record.DateTime}] {record.ActionType}")
							End If
						Next
					End If
				End If
			End If
		End Sub





		Private Function ParseHtml(html As String, violId As String) As OtsData
			Dim doc = New HtmlDocument()

			doc.LoadHtml(html)
			Dim result = New OtsData With {.ViolId = violId}
			Dim debugFolder = GetDebugFolder()
			'Dim log As New System.Text.StringBuilder()

			log.AppendLine($"=== Парсинг отказа {violId} ===")
			log.AppendLine($"Время: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")

			Try

				' ================= ФАКТИЧЕСКОЕ ВРЕМЯ НАЧАЛА =================
				ParseFactTime(doc, result)

				' ================= ОПАСНЫЙ ОТКАЗ =================
				ParseDangerOTS(doc, result)

				' ================= 5.15 ОТКАЗ =================
				Parse515Reason(doc, result)

				' ================= 8. ДОКУМЕНТЫ (Дополнительные материалы) =================
				ParseDocs(doc, result)

				' ================= 0. СТАТУС ПЕРЕДАЧИ =================
				ParseStatus(doc, result)

				' ================= 1. МЕСТО ОТКАЗА =================
				ParseMestoOTS(doc, result)

				' ================= 2. ВРЕМЯ (НАЧАЛО / УСТРАНЕНИЕ / ПРОДОЛЖИТЕЛЬНОСТЬ) =================
				ParseNachOTS(doc, result)

				' ================= 5.1. ВРЕМЯ К УЧЕТУ (PCHasy) =================
				ParsePCHUchet(doc, result)

				' ================= ПАРСИНГ ОБОРУДОВАНИЯ (Oborud) =================
				ParseOborud(doc, result)

				' ================= 3. ОБЩЕЕ ОПИСАНИЕ ОТКАЗА =================
				ParseCommonOpisOTS(doc, result)

				' ================= 4. КАТЕГОРИИ =================
				ParseKategorys(doc, result)

				' ================= 9. ВИНОВНАЯ ОРГАНИЗАЦИЯ =================
				ParseVinovnikOrganization(doc, result)

				' ================= 7. ПОСЛЕДСТВИЯ (СОБЫТИЯ, КОРПОРАТИВ И Т.Д.) =================
				ParseSobytieKorpETC(doc, result)

				' ================= 5. ЗАДЕРЖАННЫЕ ПОЕЗДА =================
				ParseTrains(doc, result)

				' ================= 6. ИСТОРИЯ ОТКАЗА =================
				ParseHistory(doc, result)


			Catch ex As Exception
				log.AppendLine($"!!! ОШИБКА: {ex.Message}")
				log.AppendLine(ex.StackTrace)
			End Try

			' Сохранение лога
			Try
				Dim logPath = System.IO.Path.Combine(debugFolder, $"parse_log_{violId}_{DateTime.Now:yyyyMMdd_HHmmss}.txt")
				System.IO.File.WriteAllText(logPath, log.ToString())
				log.Clear()
			Catch

			End Try

			Return result
		End Function

		' ================= ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ =================

		' === НОВАЯ ФУНКЦИЯ ДЛЯ ТЕСТОВ ===
		Public Function ParseLocalHtmlFile(filePath As String, violId As String) As OtsData
			If Not System.IO.File.Exists(filePath) Then
				Throw New FileNotFoundException($"Файл не найден: {filePath}")
			End If

			Dim html As String = System.IO.File.ReadAllText(filePath, Encoding.GetEncoding("windows-1251"))
			Return ParseHtml(html, violId)
		End Function
		' ================================






		' Для комментария — чистим, но оставляем теги типа <NLok=...>
		Private Function CleanComment(text As String) As String
			If String.IsNullOrWhiteSpace(text) Then Return ""

			' <br> → пробел
			text = Regex.Replace(text, "<br\s*/?>", " ")

			' Удаляем обычные теги, НО оставляем <NLok=...>
			'text = Regex.Replace(text, "<(?!NLok)[^>]+>", "")

			' HTML-сущности и пробелы
			text = text.Replace("&nbsp;", " ")
			text = Regex.Replace(text, "\s+", " ")

			Return text.Trim()
		End Function


		Private Function CleanText(text As String) As String
			If String.IsNullOrWhiteSpace(text) Then Return ""

			' Заменяем <br> на пробел
			text = Regex.Replace(text, "<br\s*/?>", " ")

			' Удаляем HTML теги
			text = Regex.Replace(text, "<[^>]+>", " ")

			' Заменяем HTML сущности
			'text = text.Replace("&nbsp;", " ").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&").Replace(vbCrLf, " ").Replace(vbTab, " ")

			text = text.Replace("&nbsp;", " ").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&")

			text = text.Replace("#", " # ")

			' Удаляем множественные пробелы
			text = Regex.Replace(text, "\s+", " ")

			Return text.Trim()
		End Function


		Private Function CleanTextWithBreaks(text As String) As String
			If String.IsNullOrWhiteSpace(text) Then Return ""

			' === <br> → перенос строки ===
			text = Regex.Replace(text, "<br\s*/?>", vbCrLf)

			' Удаляем остальные теги
			text = Regex.Replace(text, "<[^>]+>", "")

			' HTML-сущности
			text = text.Replace("&nbsp;", " ")

			' Нормализуем пробелы, но НЕ трогаем \r\n
			text = Regex.Replace(text, "[^\S\r\n]+", " ")

			Return text.Trim()
		End Function



		Private Function ExtractField(text As String, startMarker As String, endMarker As String) As String
			If String.IsNullOrWhiteSpace(text) Then Return ""

			' Экранируем специальные символы в маркерах
			Dim escapedStart = Regex.Escape(startMarker)
			Dim escapedEnd = Regex.Escape(endMarker)

			Dim pattern = $"{escapedStart}\s*(.*?)\s*{escapedEnd}"
			Dim match = Regex.Match(text, pattern, RegexOptions.Singleline)

			If match.Success Then
				Return CleanText(match.Groups(1).Value)
			End If

			Return ""
		End Function

		Private Function ParseDelay(text As String) As Integer
			Dim minutes As Integer = 0

			' Парсим часы
			Dim hoursMatch = Regex.Match(text, "(\d+)\s*ч\.?\s*")
			If hoursMatch.Success Then
				Integer.TryParse(hoursMatch.Groups(1).Value, minutes)
				minutes *= 60
			End If

			' Парсим минуты
			Dim minsMatch = Regex.Match(text, "(\d+)\s*мин\.?\s*")
			If minsMatch.Success Then
				Dim m As Integer = 0
				Integer.TryParse(minsMatch.Groups(1).Value, m)
				minutes += m
			End If

			Return minutes
		End Function

		''' <summary>
		''' Очищает название сторонней организации от дублей и служебной информации
		''' </summary>
		Private Function CleanThirdPartyOrg(text As String) As String

			If String.IsNullOrWhiteSpace(text) Then Return text

			' Очистка от HTML-сущностей и спецсимволов пробела
			text = text.Replace("&nbsp;", " ").Replace(ChrW(160), " ").Trim()

			' ШАГ 0: Отсекаем скобки со служебной информацией (станции приписки и т.д.)
			Dim mainText = text
			Dim parenPos = text.IndexOf("("c)
			If parenPos > 0 Then
				mainText = text.Substring(0, parenPos).Trim()
			End If

			' ШАГ 1: Разбиваем по разделителю " - "
			Dim parts = mainText.Split(New String() {" - "}, StringSplitOptions.None).Select(Function(p) p.Trim()).ToArray()

			' Если разделителя нет — возвращаем то, что осталось после очистки от скобок
			If parts.Length < 2 Then
				Return mainText
			End If

			Dim firstPart = parts(0)
			Dim secondPart = parts(1)

			' ШАГ 2: Логика выбора результата

			' 1. Проверка на дублирование (как в АО «ТМХ-ЛОКОМОТИВЫ» - АО «ТМХ-ЛОКОМОТИВЫ»)
			' Сравниваем, предварительно удалив кавычки для точности
			If firstPart.Replace("«", "").Replace("»", "").Equals(
   secondPart.Replace("«", "").Replace("»", ""), StringComparison.OrdinalIgnoreCase) Then
				Return firstPart
			End If

			' 2. Если первая часть — холдинг (ЖЕЛДОРРЕММАШ), берем конкретный завод из второй части
			' Сюда же можно добавить РЕМПУТЬМАШ через запятую
			Dim factoryHoldings = {"ЖЕЛДОРРЕММАШ", "РЕМПУТЬМАШ"}
			If factoryHoldings.Any(Function(h) firstPart.Contains(h)) Then
				Return secondPart
			End If

			' 3. Если это сервисное депо (СЛД) — они обычно во второй части
			If secondPart.StartsWith("СЛД", StringComparison.OrdinalIgnoreCase) Then
				Return secondPart
			End If

			' 4. Если в первой части ТМХ (но не дубль), то обычно это и есть головная компания
			If firstPart.Contains("ТМХ") Then
				Return firstPart
			End If

			' 5. Запасной вариант: если ничего не подошло, возвращаем всю строку без скобок
			Return mainText
		End Function








		''' <summary>
		''' Парсит HTML-таблицу списка отказов и возвращает список записей
		''' </summary>
		Public Function ParseJournalList(html As String) As List(Of JournalRecord)


			Dim doc As New HtmlAgilityPack.HtmlDocument()
			doc.OptionFixNestedTags = True
			doc.LoadHtml(html)

			Dim result As New List(Of JournalRecord)()
			Dim seenIds As New HashSet(Of String)()

			' 🔑 КЛЮЧЕВОЕ: ищем таблицу по id="data_table"
			Dim table = doc.GetElementbyId("data_table")
			If table Is Nothing Then Return result

			Dim rows = table.SelectNodes(".//tr")
			If rows Is Nothing Then Return result

			' Функция для чистого извлечения текста из ячейки
			Dim GetText = Function(cell As HtmlNode) As String
							  Dim raw = cell.InnerHtml
							  raw = raw.Replace("<br>", " ").Replace("<br/>", " ").Replace("<br />", " ")
							  raw = System.Text.RegularExpressions.Regex.Replace(raw, "<[^>]+>", "")
							  raw = raw.Replace("&nbsp;", " ").Replace(ChrW(160), " ")
							  Return System.Text.RegularExpressions.Regex.Replace(raw, "\s+", " ").Trim()
						  End Function

			For Each row In rows
				' 🔑 Берём ViolId из id строки: <tr id="row15831622">
				Dim rowId = row.GetAttributeValue("id", "")
				If Not rowId.StartsWith("row") OrElse rowId = "table_header" Then Continue For

				Dim violId = rowId.Substring(3).Trim()
				If Not System.Text.RegularExpressions.Regex.IsMatch(violId, "^\d{7,9}$") Then Continue For
				If seenIds.Contains(violId) Then Continue For
				seenIds.Add(violId)

				Dim cells = row.SelectNodes(".//td")

				' 🔍 отладочный лог структуры ячеек
				If result.Count = 0 Then
					Try
						Dim debugLines As New List(Of String)
						debugLines.Add($"=== ROW ID: {rowId} ===")
						If cells IsNot Nothing Then
							debugLines.Add($"Cells count: {cells.Count}")
							For i As Integer = 0 To Math.Min(cells.Count - 1, 20) ' первые 20 ячеек
								Dim raw = cells(i).InnerHtml
								Dim clean = raw.Replace("<br>", " ").Replace("<br/>", " ").Replace("<br />", " ")
								clean = System.Text.RegularExpressions.Regex.Replace(clean, "<[^>]+>", "")
								clean = clean.Replace("&nbsp;", " ").Replace(ChrW(160), " ")
								clean = System.Text.RegularExpressions.Regex.Replace(clean, "\s+", " ").Trim()
								debugLines.Add($"{i}: '{clean}'")
							Next
						Else
							debugLines.Add("Cells = Nothing!")
						End If
						System.IO.File.WriteAllText(
				System.IO.Path.Combine(GetDebugFolder(), $"debug_row_structure_{violId}.txt"),
				String.Join(vbCrLf, debugLines),
				Encoding.UTF8)
					Catch ex As Exception
						' Игнорируем ошибки отладки, чтобы не ломать основной парсинг
					End Try
				End If
				' 🔍 КОНЕЦ БЛОКА ОТЛАДКИ

				If cells Is Nothing OrElse cells.Count < 14 Then Continue For

				' 🔑 Фиксированные индексы колонок (проверено на твоём HTML)
				Dim category = cells(6).InnerText.Trim()
				If category <> "1" AndAlso category <> "2" AndAlso category <> "3" Then Continue For

				Dim rec As New JournalRecord() With {
					.ViolId = violId,
					.Category = category,
					.StartTime = GetText(cells(8)),
					.EndTime = GetText(cells(9)),
					.FromDept = GetText(cells(10)),
					.ToDept = GetText(cells(11)),
					.Location = (GetText(cells(12))), 'NormalizeForCompare(GetText(cells(12)), uniqueTrains:=True), 
					.MestoOTS_TXT = .Location,
					.Equipment = GetText(cells(13))
				}

				' Иконки и ASU
				Dim rowHtml = row.InnerHtml
				Dim asuImg = cells(0).SelectSingleNode(".//img")
				Dim asuAlt = If(asuImg IsNot Nothing, asuImg.GetAttributeValue("alt", "").Trim(), "")
				rec.ASU = If(String.IsNullOrEmpty(asuAlt), "РУЧНОЙ ВВОД", asuAlt)

				rec.IsLocked = rowHtml.Contains("lock.gif", StringComparison.OrdinalIgnoreCase)
				rec.HasAttachments = rowHtml.Contains("attach.gif", StringComparison.OrdinalIgnoreCase)
				rec.NeedsAdditional = rowHtml.Contains("additional_investigation.gif", StringComparison.OrdinalIgnoreCase)
				rec.IsEasapr = rowHtml.Contains("easapr_rzd.gif", StringComparison.OrdinalIgnoreCase)

				' Количество вложений из alt картинки
				Dim attachImg = cells(3).SelectSingleNode(".//img")
				If attachImg IsNot Nothing Then
					Dim altText = attachImg.GetAttributeValue("alt", "")
					Dim match = System.Text.RegularExpressions.Regex.Match(altText, "\((\d+)\)")
					If match.Success Then Integer.TryParse(match.Groups(1).Value, rec.AttachCount)
				End If

				' Скрытые поля (статус, кол-во поездов)
				Dim statusInput = row.SelectSingleNode($".//input[@name='viol{violId}_status']")
				If statusInput IsNot Nothing Then rec.Status = statusInput.GetAttributeValue("value", "")

				Dim trainInput = row.SelectSingleNode($".//input[@name='viol{violId}_train_cnt']")
				If trainInput IsNot Nothing Then Integer.TryParse(trainInput.GetAttributeValue("value", "0"), rec.TrainCount)

				result.Add(rec)
			Next

			Return result


		End Function


		'''' <summary>
		'''' Оптимизированная версия — принимает скомпилированный шаблон
		'''' </summary>
		'Private Function ExtractHiddenValueOptimized(html As String, patternBase As String, fieldValue As String) As String
		'    Dim pattern = String.Format(patternBase, Regex.Escape(fieldValue))
		'    Dim m = Regex.Match(html, pattern, RegexOptions.IgnoreCase Or RegexOptions.Singleline)
		'    Return If(m.Success, m.Groups(1).Value.Trim(), "")
		'End Function

		Private Function HasNextPage(html As String, currentPage As Integer) As Boolean
			Dim nextNum As String = (currentPage + 1).ToString()

			' 🔹 Ищем точные совпадения из HTML (в VB.NET кавычки внутри строк удваиваются: "")
			Dim patterns = {
				$"saveStatus('activePage', '{nextNum}'",
				$"saveStatus(""activePage"", ""{nextNum}""",
				$"activePage={nextNum}",
				$"]{nextNum}[",
				$">{nextNum}<"
			}

			For Each p In patterns
				If html.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0 Then Return True
			Next

			' 🔹 Запасной вариант: ищем любые номера страниц в вызовах saveStatus
			Dim matches = Regex.Matches(html, "saveStatus\(['""]activePage['""],\s*['""]?(\d+)")
			For Each m As Match In matches
				Dim pageIdx As Integer
				If Integer.TryParse(m.Groups(1).Value, pageIdx) AndAlso pageIdx > currentPage Then
					Return True
				End If
			Next

			Return False
		End Function



		''' <summary>
		''' Автоматически обходит все страницы журнала, собирает уникальные отказы
		''' </summary>
		Public Async Function FetchAllJournalPagesAsync(baseJournalUrl As String, Optional progress As IProgress(Of Integer) = Nothing, Optional saveExcel As Boolean = False) As Task(Of List(Of JournalRecord))


			'================ это ускоренный вариант С ТАЙМИНГОМ В ЛОГЕ ===================

			Dim allRecords As New List(Of JournalRecord)
			Dim seenIds As New HashSet(Of String)
			Dim page As Integer = 1
			Dim logPath = System.IO.Path.Combine(GetDebugFolder(), "fetch_log.txt")
			Dim logLock As New Object()
			Dim sw As New Stopwatch() ' ⏱ Таймер для замеров

			Dim LogWrite = Sub(msg As String)
							   SyncLock logLock
								   System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}{vbCrLf}", Encoding.UTF8)
							   End SyncLock
						   End Sub

			LogWrite($"🚀 Старт загрузки: {baseJournalUrl}")
			Dim reportTab As String = Nothing
			Dim baseUrl As String = ""
			Try
				' 1️⃣ Загружаем базовую страницу
				LogWrite("📄 Загрузка базовой страницы...")
				Dim initialResponse = Await _httpClient.GetAsync(baseJournalUrl)
				initialResponse.EnsureSuccessStatusCode()
				Dim initialHtml = Await initialResponse.Content.ReadAsStringAsync()
				LogWrite($"✅ Базовая получена: {initialHtml.Length} байт")

				' 🔥 Ищем ПРАВИЛЬНЫЙ tab
				reportTab = Nothing
				Dim docInit As New HtmlDocument()
				docInit.LoadHtml(initialHtml)
				Dim tabNode = docInit.DocumentNode.SelectSingleNode("//input[@name='tab']")
				If tabNode IsNot Nothing Then reportTab = tabNode.GetAttributeValue("value", "")

				If String.IsNullOrEmpty(reportTab) Then
					Dim m = Regex.Match(initialResponse.RequestMessage.RequestUri.ToString(), "[?&]tab=(\d+)")
					If m.Success Then reportTab = m.Groups(1).Value
				End If

				If String.IsNullOrEmpty(reportTab) Then
					LogWrite("❌ Не найден параметр 'tab'.")
					Return allRecords
				End If
				LogWrite($"🔑 reportTab = {reportTab}")

				baseUrl = initialResponse.RequestMessage.RequestUri.GetLeftPart(UriPartial.Authority)
				Dim saveStatusUrl = $"{baseUrl}/kasant/tabSaveStatus"
				Dim tableUrl = $"{baseUrl}/kasant/journal_table.jsp"

				' 2️⃣ Цикл пагинации
				While True
					LogWrite($"📥 Страница {page}: подготовка...")

					' 🔹 ЭТАП 1: Сообщаем серверу, какую страницу хотим (только для стр. 2+)
					If page > 1 Then
						LogWrite($"📤 Вызов tabSaveStatus для страницы {page}...")
						Dim statusContent = New FormUrlEncodedContent(New Dictionary(Of String, String) From {
							{"tab", reportTab},
							{"activePage", page.ToString()},
							{"operation", "update_journal_table_commit()"},
							{"_", ""}
						})

						Using statusReq As New HttpRequestMessage(HttpMethod.Post, saveStatusUrl)
							statusReq.Content = statusContent
							statusReq.Headers.Referrer = New Uri($"{baseUrl}/kasant/journal.jsp?tab={reportTab}")
							statusReq.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest")
							statusReq.Headers.TryAddWithoutValidation("Accept", "text/javascript, text/html, application/xml, text/xml, */*")
							statusReq.Headers.TryAddWithoutValidation("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7")
							statusReq.Headers.TryAddWithoutValidation("X-Prototype-Version", "1.5.0")

							Dim statusResp = Await _httpClient.SendAsync(statusReq)
							Dim statusResult = Await statusResp.Content.ReadAsStringAsync()
							LogWrite($"📡 Ответ tabSaveStatus: {statusResult.Trim()}")
						End Using
						Await Task.Delay(150)
					End If

					' 🔹 ЭТАП 2: Получаем таблицу для сохранённой страницы + ⏱ ЗАМЕР ВРЕМЕНИ
					LogWrite($"📥 Запрос таблицы для страницы {page}...")
					Dim rndVal = DateTimeOffset.Now.ToUnixTimeMilliseconds()

					Using tableReq As New HttpRequestMessage(HttpMethod.Post, $"{tableUrl}?tab={reportTab}")
						tableReq.Content = New StringContent($"=undefined&rndval={rndVal}", Encoding.UTF8, "application/x-www-form-urlencoded")
						tableReq.Headers.Referrer = New Uri($"{baseUrl}/kasant/journal.jsp?tab={reportTab}")
						tableReq.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest")
						tableReq.Headers.TryAddWithoutValidation("Accept", "*/*")
						tableReq.Headers.TryAddWithoutValidation("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7")

						' ⏱ ЗАМЕР СЕТИ
						sw.Restart()
						Dim tableResp = Await _httpClient.SendAsync(tableReq)
						'Dim html = Await tableResp.Content.ReadAsStringAsync()
						Dim bytes = Await tableResp.Content.ReadAsByteArrayAsync()
						Dim html = Encoding.GetEncoding("windows-1251").GetString(bytes)
						sw.Stop()
						Dim networkMs = sw.ElapsedMilliseconds

						If Not tableResp.IsSuccessStatusCode OrElse String.IsNullOrWhiteSpace(html) Then Exit While

						' ⏱ ЗАМЕР ПАРСИНГА
						sw.Restart()
						Dim pageRecords = ParseJournalList(html)
						sw.Stop()
						Dim parseMs = sw.ElapsedMilliseconds

						' 📊 Вывод замеров в лог
						LogWrite($"⏱ Стр. {page}: сеть {networkMs}мс | парсинг {parseMs}мс | найдено {pageRecords.Count}")

						' === >>> ВСТАВКА НАЧАЛО <<< ===
						If pageRecords.Count = 0 Then
							Dim emptyPath = System.IO.Path.Combine(GetDebugFolder(),
					  $"debug_empty_page{page}_{DateTime.Now:yyyyMMdd_HHmmss}.html")
							System.IO.File.WriteAllText(emptyPath, html, Encoding.GetEncoding("windows-1251"))
							LogWrite($"⚠ Пустая страница {page}, HTML сохранён: {emptyPath}")
							Exit While
						End If
						' === >>> ВСТАВКА КОНЕЦ <<< ===


						'If pageRecords.Count = 0 Then Exit While

						' 🔹 Добавление уникальных
						Dim addedCount As Integer = 0
						For Each rec In pageRecords
							If seenIds.Contains(rec.ViolId) Then
								' LogWrite($"⚠ Дубль: {rec.ViolId} (стр. {page})") ' Закомментировано, чтобы не спамить лог
							Else
								seenIds.Add(rec.ViolId)
								allRecords.Add(rec)
								addedCount += 1
							End If
						Next
						LogWrite($"➕ Добавлено: {addedCount}. Всего: {allRecords.Count}")
						progress?.Report(allRecords.Count)

						If Not HasNextPage(html, page) Then
							LogWrite($"🏁 Последняя страница: {page}")
							Exit While
						End If
					End Using

					page += 1
					Await Task.Delay(200)
				End While

			Catch ex As Exception
				LogWrite($"💥 Ошибка: {ex.Message}")
				System.Diagnostics.Debug.WriteLine(ex.ToString())
			End Try

			' ========================================================================
			' 🔹 3️⃣ СКАЧИВАНИЕ EXCEL (если запрошено и мы знаем tab)
			' ========================================================================
			If saveExcel AndAlso Not String.IsNullOrEmpty(reportTab) AndAlso Not String.IsNullOrEmpty(baseUrl) Then
				Try
					LogWrite("📥 Начало скачивания Excel-отчета...")

					' Формируем URL как в HAR-файле
					Dim excelUrl = $"{baseUrl}/kasant/Journal_list?tab={reportTab}&presentation=excel"
					Dim refererUrl = $"{baseUrl}/kasant/journal.jsp?tab={reportTab}"

					Using excelReq As New HttpRequestMessage(HttpMethod.Get, excelUrl)
						' Обязательно указываем Referer, иначе сервер может отказать
						excelReq.Headers.Referrer = New Uri(refererUrl)
						excelReq.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/vnd.ms-excel,*/*")
						excelReq.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36")

						Dim excelResp = Await _httpClient.SendAsync(excelReq)

						If excelResp.IsSuccessStatusCode Then
							Dim excelBytes = Await excelResp.Content.ReadAsByteArrayAsync()

							' Получаем путь к рабочему столу текущего пользователя
							Dim desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
							Dim fileName = "1. 4Отч - новый.xls"
							Dim fullPath = System.IO.Path.Combine(desktopPath, fileName)

							' Сохраняем файл
							System.IO.File.WriteAllBytes(fullPath, excelBytes)
							LogWrite($"✅ Excel успешно сохранён на рабочий стол: {fullPath} ({excelBytes.Length} байт)")
						Else
							LogWrite($"⚠️ Ошибка скачивания Excel. Статус: {excelResp.StatusCode}")
						End If
					End Using

				Catch ex As Exception
					LogWrite($"💥 Ошибка при сохранении Excel: {ex.Message}")
				End Try
			End If



			LogWrite($"🎉 Завершено! Всего: {allRecords.Count}")
			Return allRecords
		End Function


		''' <summary>
		''' Загружает и парсит отчёт о состоянии расследования отказов
		''' </summary>
		Public Async Function FetchInvestigationReportAsync(reportUrl As String) As Task(Of List(Of InvestigationReportItem))

			Try
				Dim debugFolder = GetDebugFolder()

				' Просто загружаем HTML
				Dim response = Await _httpClient.GetAsync(reportUrl)
				response.EnsureSuccessStatusCode()

				Dim bytes = Await response.Content.ReadAsByteArrayAsync()
				Dim html = Encoding.GetEncoding("windows-1251").GetString(bytes)

				' Сохраняем для отладки
				Dim fileName = If(reportUrl.Contains("dt_nd"), "investigation_report_current.html", "investigation_report_previous.html")
				System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, fileName), html, Encoding.GetEncoding("windows-1251"))

				' Парсим и возвращаем
				Return ParseInvestigationReport(html)

			Catch ex As Exception
				Dim debugFolder = GetDebugFolder()
				System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, "investigation_report_error.txt"),
		$"Ошибка: {ex.Message}{vbCrLf}{vbCrLf}{ex.StackTrace}")
				Return New List(Of InvestigationReportItem)()
			End Try

		End Function





		Public Async Function FetchDepotReportAsync(reportUrl As String,
										Optional isPrevious As Boolean = False,
										Optional filterSLD As Boolean = False) As Task(Of List(Of InvestigationReportItem))
			Try
				Dim debugFolder = GetDebugFolder()

				' 1. Первый запрос
				Dim response = Await Fetcher.HttpClient.GetAsync(reportUrl)
				response.EnsureSuccessStatusCode()
				Dim bytes = Await response.Content.ReadAsByteArrayAsync()
				Dim html = Encoding.GetEncoding("windows-1251").GetString(bytes)

				' 2. 🔑 ПРОВЕРКА НА РАЗЛОГИН (если пришла форма входа)
				If html.Contains("anauth_panel") OrElse html.Contains("id_prog") Then
					MW.InfoBLOK.AddItem("⚠ Сессия КАСАНТ неактивна. Выполняю автоматический вход...")
					Dim loginOk = Await Fetcher.EnsureConnectedAsync()

					If Not loginOk Then
						MW.InfoBLOK.AddItem("❌ Не удалось войти в КАСАНТ. Проверьте логин/пароль в настройках.")
						Return New List(Of InvestigationReportItem)()
					End If

					' 3. Повторяем запрос после успешного входа
					MW.InfoBLOK.AddItem("✅ Вход выполнен. Повторяю загрузку отчёта...")
					response = Await Fetcher.HttpClient.GetAsync(reportUrl)
					response.EnsureSuccessStatusCode()
					bytes = Await response.Content.ReadAsByteArrayAsync()
					html = Encoding.GetEncoding("windows-1251").GetString(bytes)
				End If

				' 4. Сохраняем для отладки
				Dim fileName = If(isPrevious, "depot_report_previous.html", "depot_report_current.html")
				File.WriteAllText(System.IO.Path.Combine(debugFolder, fileName), html, Encoding.GetEncoding("windows-1251"))

				' 5. Парсим с учётом флага фильтрации СЛД
				Return ParseDepotReport(html, filterSLD)

			Catch ex As Exception
				Dim debugFolder = GetDebugFolder()
				System.IO.File.WriteAllText(System.IO.Path.Combine(debugFolder, "depot_report_error.txt"),
		$"Ошибка: {ex.Message}{vbCrLf}{vbCrLf}{ex.StackTrace}")
				Return New List(Of InvestigationReportItem)()
			End Try

		End Function

		Public Function GetDebugFolder() As String
			Dim desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
			Dim debugFolder = Path.Combine(desktopPath, "KasAntDebug")

			Try
				If Not Directory.Exists(debugFolder) Then
					Directory.CreateDirectory(debugFolder)
				End If
			Catch
				debugFolder = desktopPath
			End Try

			Return debugFolder
		End Function
	End Class
End Namespace


