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


		'' <summary>
		''' Формирует ссылку на отчет по Дирекции Т (Таблица 10) для дорожного пользователя
		''' Время всегда: 00:00 - 23:59
		''' </summary>
		Public Function BuildTchReportUrl(dateFrom As DateTime, dateTo As DateTime) As String
			' Генерируем tmp_unik на основе даты начала периода + текущего времени
			' Это гарантирует уникальность запроса для каждого месяца при помесячной разбивке
			Dim baseTicks = New DateTime(1970, 1, 1).Ticks
			Dim periodTicks = dateFrom.Ticks - baseTicks
			Dim uniqueSeed = Math.Floor((periodTicks + DateTime.UtcNow.Ticks) / 10000)

			Return $"{Fetcher._baseUrl}/reports/new/Report3_4_1" &
		   $"?page=reports/new/Report3_4_1" &
		   $"&tmp_unik={uniqueSeed}" &
		   $"&dt_nd={dateFrom:dd.MM.yyyy}&dt_nd_h=00&dt_nd_min=00" &
		   $"&dt_kd={dateTo:dd.MM.yyyy}&dt_kd_h=23&dt_kd_min=59" &
		   "&rep_asu=0,1,2,5,7,11,12,14,15,20,21,26,26" &
		   "&rep_asu_dop=1:1" &
		   "&kind_rep_type=1" &
		   "&flg_alien=0" &          ' Только РЖД
		   "&flg_id_cause_other=0" &
		   "&sls=172450" &           ' Код Дирекции Т
		   "&flg_alien_service=0" &
		   "&dor_kod_guilty=88" &    ' Красноярская ж.д.
		   "&rep_status=0,1,2,6,7,10"
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