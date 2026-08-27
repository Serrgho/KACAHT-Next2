Imports System.Collections.ObjectModel
Imports System.Globalization
Imports System.IO
Imports System.Text.RegularExpressions

Imports OfficeOpenXml
Imports OfficeOpenXml.Style

Namespace Kas

    Module ExcelModule



        Public Function LoadOTSFromExcel(filePath As String) As List(Of Otkaz)
            ' Установка лицензии для некоммерческого использования
            ExcelPackage.License.SetNonCommercialPersonal("СергейВалерьевич")

            Dim otkazy As New List(Of Otkaz)
            'Dim FileIsOK As Boolean = True



            Using package As New ExcelPackage(New FileInfo(filePath))

                If package.Workbook Is Nothing OrElse package.Workbook.Worksheets.Count = 0 Then
                    MW.InfoBLOK.AddItem("Файл не содержит листов")
                    Return otkazy
                End If

                Dim worksheet = package.Workbook.Worksheets(0)
                If worksheet Is Nothing Then
                    MW.InfoBLOK.AddItem("Не удалось загрузить первый лист")
                    Return otkazy
                End If

                Dim FileIsOK As Boolean = True
                Try
                    ' Только теперь обращаемся к Dimension — и проверяем его!
                    If worksheet.Dimension Is Nothing Then
                        FileIsOK = False
                    Else
                        ' Заголовки — только если есть данные
                        Dim c13 = SafeCellText(worksheet, 4, 13)
                        Dim c17 = SafeCellText(worksheet, 4, 17)
                        Dim c14 = SafeCellText(worksheet, 3, 14)
                        Dim c5 = SafeCellText(worksheet, 4, 5)

                        FileIsOK = c13.Contains("Обстоятельства") AndAlso
                       c17.Contains("Тех средство") AndAlso
                       c14.Contains("КНОПКА") AndAlso
                       c5.Contains("Поездо-часов")
                    End If

                Catch ex As Exception
                    MW.InfoBLOK.AddItem($"Ошибка при проверке файла: {ex.Message}")
                    FileIsOK = False
                End Try

                If Not FileIsOK Then
                    MW.InfoBLOK.AddItem("Файл не содержит нужные данные")
                    Return otkazy
                End If


                ' → Только теперь можно читать данные:
                Dim lastRow = worksheet.Dimension?.End.Row
                If lastRow <= 4 Then
                    MW.InfoBLOK.AddItem("Нет данных для импорта")
                    Return otkazy
                End If


                ' Проходим по строкам (начиная со второй строки)
                For row As Integer = 5 To lastRow
                    If worksheet.Cells(row, 2).Value?.ToString() = "" Then Continue For
                    If worksheet.Cells(row, 2).Value?.ToString().Length < 5 Then Continue For
                    Dim otkaz As New Otkaz()
                    With otkaz
                        ' Заполняем свойства по номерам столбцов
                        .Id = ParseString(worksheet.Cells(row, 2).Value) ' (2) номер ОТС

                        '============ целые значения ======================
                        .Kat = ParseInt(worksheet.Cells(row, 3).Value) ' (3) 
                        .PasKol = ParseInt(worksheet.Cells(row, 24).Value) ' (24) количество задержанных пасс. п.
                        .PrigKol = ParseInt(worksheet.Cells(row, 25).Value) ' (25) количество задержанных приг. п.
                        .GruzKol = ParseInt(worksheet.Cells(row, 26).Value) ' (26) количество задержанных груз. п.
                        '==================================

                        '=========== значения с точкой =======================
                        .PCh = ParseSingle(worksheet.Cells(row, 5).Value) ' (5)
                        .Dlit = ToDecimalHours(worksheet.Cells(row, 55).Value) 'UniversalToHours(worksheet.Cells(row, 55).Value) ' (55) продолжит-сть задержки
                        .PasPCH = UniversalToHours(worksheet.Cells(row, 56).Value) ' (56) время задержки пасс.
                        .PrigPCH = UniversalToHours(worksheet.Cells(row, 57).Value) ' (57) время задержки приг. 
                        .GruzPCH = UniversalToHours(worksheet.Cells(row, 58).Value) ' (58) время задержки груз.


                        '.Dlit = ParseHoursFromExcelMinutes(worksheet.Cells(row, 55).Value) ' (55) продолжит-сть задержки
                        '.PasPCH = ParseHoursFromExcelMinutes(worksheet.Cells(row, 56).Value) ' (56) время задержки пасс.
                        '.PrigPCH = ParseHoursFromExcelMinutes(worksheet.Cells(row, 57).Value) ' (57) время задержки приг. 
                        '.GruzPCH = ParseHoursFromExcelMinutes(worksheet.Cells(row, 58).Value) ' (58) время задержки груз. 

                        '==================================

                        '======= даты ===========================
                        .Nach = ToDateTime(worksheet.Cells(row, 4).Value) ' (4) Дата/время начала ОТС
                        .Peredan = ToDate(worksheet.Cells(row, 21).Value) ' (21) передан на др дорогу
                        .Zakryt = ToDate(worksheet.Cells(row, 22).Value) ' (22) закрыт
                        .Postup = ToDate(worksheet.Cells(row, 20).Value) ' (20) поступил
                        .Sozdan = ToDate(worksheet.Cells(row, 52).Value) ' (52) Дата создания ОТС

                        '==================================



                        '============ локомотив/бригада ======================
                        .SerLokExact = ParseString(worksheet.Cells(row, 9).Value) ' (9) Серия локомотива точная
                        .NumLok = ParseString(worksheet.Cells(row, 10).Value) ' (10) Номер локомотива
                        .PripLok = ParseString(worksheet.Cells(row, 11).Value) ' (11) Приписка локомотива
                        .VidT = ParseString(worksheet.Cells(row, 51).Value)  ' (51) Вид тяги
                        .Mash = ParseString(worksheet.Cells(row, 59).Value) ' (59) машинист
                        .PripMash = ParseString(worksheet.Cells(row, 60).Value) ' (60) приписка машиниста
                        '==================================



                        '==================================
                        .Istochnik = ParseString(worksheet.Cells(row, 6).Value) ' (6) От кого поступил ОТС
                        If Not IsRightIstocnik(.Istochnik) Then
                            .AddUpdateNote("От кого ОТС")
                        End If
                        .KtoZakryl = ParseString(worksheet.Cells(row, 7).Value)  ' (7) кто закрыл...
                        Dim Zak As String = ParseString(worksheet.Cells(row, 14).Value) ' (14) отнесен на...
                        .ZaKem = IIf(Zak = "тр", "", Zak)
                        .Opis = ParseString(worksheet.Cells(row, 13).Value)  ' (13) Описание отказа
                        .ProcessOpisIntoHistory()
                        .MestoOTS_TXT = ParseString(worksheet.Cells(row, 8).Value)  ' (8) Место отказа с поездами
                        '==================================


                        '========== признаки =====================================
                        '.IsStation = ParseFlag(worksheet.Cells(row, 23).Value)
                        .IsStation = Not (.MestoOTS.Contains(" - "))
                        .ISDanger = ParseFlag(worksheet.Cells(row, 47).Value)
                        .ISKorp = ParseFlag(worksheet.Cells(row, 53).Value)
                        .ISSobyt = ParseFlag(worksheet.Cells(row, 54).Value)
                        .OkaPom = ParseFlag(worksheet.Cells(row, 65).Value)
                        .NarushSroka = ParseFlag(worksheet.Cells(row, 67).Value)
                        .AlienSLD = ParseString(worksheet.Cells(row, 66).Value) ' (66) Расследован за чужим СЛД
                        '==================================

                        '========= виды оборудования =========================
                        .MyKlasLev1 = ParseString(worksheet.Cells(row, 12).Value) ' (12) Классификатор Внутренний (1 ур.)
                        .MyKlasLev2 = ParseString(worksheet.Cells(row, 16).Value) ' (16) Классификатор Внутренний (2 ур.)
                        .MyKlasLev3 = ParseString(worksheet.Cells(row, 17).Value) ' (17) Классификатор Внутренний (3 ур.)

                        .OTSLev1 = ParseString(worksheet.Cells(row, 45).Value) ' (45) Классификатор КАСАНТ (1 ур.)
                        .OTSLev2 = ParseString(worksheet.Cells(row, 46).Value)  ' (46) Классификатор КАСАНТ (2 ур.)
                        .OTSLev3 = ParseString(worksheet.Cells(row, 43).Value)  ' (43) Классификатор КАСАНТ (3 ур.)
                        '==================================

                    End With
                    ' Добавляем объект в список
                    otkazy.Add(otkaz)
                Next


                Dim worksht2 = package.Workbook.Worksheets("Хрон п_час")
                If worksht2 Is Nothing Then
                    MW.InfoBLOK.AddItem("Лист 'Хрон п_час' не найден")
                Else
                    Dim KS = worksht2.Dimension?.End.Row

                    If KS.HasValue AndAlso KS.Value >= 2 Then
                        Dim PCH As Single
                        Dim DTE As Date

                        For i = 2 To KS.Value
                            With worksht2
                                PCH = ParseSingle(.Cells(i, 3).Value)
                                DTE = Set4ToDate(.Cells(i, 1).Value)
                                Dim IDOTS As String = .Cells(i, 2).Value?.ToString()

                                ' Ищем отказ по ID
                                Dim foundOtkaz As Otkaz = otkazy.FirstOrDefault(Function(U) U.Id = IDOTS)

                                If foundOtkaz IsNot Nothing Then
                                    If PCH < 0 Then
                                        foundOtkaz.KorPCH += PCH
                                        foundOtkaz.KorDate = DTE
                                        foundOtkaz.KorPCHonDate = PCH
                                        MW.InfoBLOK.AddItem($"Проставляем сумму корректировок по отказу {IDOTS} на {PCH}")
                                    Else
                                        foundOtkaz.KorDate = DTE
                                        foundOtkaz.KorPCHonDate = PCH
                                        MW.InfoBLOK.AddItem($"Проставляем сумму корректировок по отказу {IDOTS} на {PCH}")
                                    End If
                                Else
                                    MW.InfoBLOK.AddItem($"---ВНИМАНИЕ!--- Отказ {IDOTS} из листа корректировок отсутствует в массиве импортированных отказов")
                                End If
                            End With
                        Next
                    End If
                End If









                'Dim worksht2 = package.Workbook.Worksheets("Хрон п_час")
                'Dim KS = worksht2.Dimension?.End.Row
                'Dim PCH As Single
                'Dim DTE As Date

                'For i = 2 To KS
                '    With worksht2
                '        PCH = ParseSingle(.Cells(i, 3).Value)
                '        DTE = Set4ToDate(.Cells(i, 1).Value)

                '        Dim IDOTS As String = .Cells(i, 2).Value
                '        If PCH < 0 Then
                '            Try
                '                otkazy.Where(Function(U) U.Id = IDOTS).First.KorPCH += PCH
                '                otkazy.Where(Function(U) U.Id = IDOTS).First.KorDate = DTE
                '                otkazy.Where(Function(U) U.Id = IDOTS).First.KorPCHonDate = PCH
                '                MW.InfoBLOK.AddItem($"Проставляем сумму корректировок по отказу {IDOTS} на {PCH}")
                '            Catch ex As Exception
                '                MW.InfoBLOK.AddItem($"---АШЫПКА!!---{vbCrLf}отказ {IDOTS} из листа корректировок отсутствует в массиве импортированных отказов {vbCrLf} {ex.Message}")
                '            End Try
                '        Else
                '            otkazy.Where(Function(U) U.Id = IDOTS).First.KorDate = DTE
                '            otkazy.Where(Function(U) U.Id = IDOTS).First.KorPCHonDate = PCH
                '            MW.InfoBLOK.AddItem($"Проставляем сумму корректировок по отказу {IDOTS} на {PCH}")
                '        End If

                '    End With
                'Next
            End Using

            Return otkazy
        End Function











        ' --- Вспомогательные парсеры (безопасные, с 0 по умолчанию) ---
        Friend Function ParseString(input As Object) As String
            Return If(input Is DBNull.Value Or input Is Nothing, "", input.ToString.Trim())
        End Function

        Friend Function ParseInt(input As Object, Optional [default] As Integer = 0) As Integer
            Dim s = CStr(input)?.Trim()
            Dim result As Integer
            Return If(Integer.TryParse(s, result), result, [default])
        End Function

        Friend Function ParseSingle(input As Object, Optional [default] As Single = 0) As Single
            Dim s = CStr(input)?.Trim()
            Dim result As Single
            Return If(Single.TryParse(s, result), result, [default])
        End Function

        Private Function ParseFlag(cellValue As Object) As Boolean?
            Dim s As String = cellValue?.ToString()?.Trim()
            If String.IsNullOrEmpty(s) Then
                Return False
            Else
                Return True
            End If

            '' Список "отказных" значений
            'Dim stopWords = {"0", "нет", "false", "no"}
            'Return Not stopWords.Contains(s)

        End Function

        Private Function SafeCellText(ws As ExcelWorksheet, row As Integer, col As Integer) As String
            Try
                Dim val = ws.Cells(row, col).Value
                If val Is Nothing OrElse IsDBNull(val) Then Return ""
                Return CStr(val).Trim()
            Catch
                Return ""
            End Try
        End Function

        Public Function ToDateTime(rawValue As Object) As Date
            Return SetToDate(rawValue, ShowTime:=True)
        End Function

        Public Function ToDate(rawValue As Object) As Date
            Return SetToDate(rawValue, ShowTime:=False)
        End Function

        Function SetToDate(rawValue As Object, Optional ShowTime As Boolean = True) As Date

            Dim result As Date = Date.MinValue

            Select Case True
                Case rawValue Is Nothing
                ' остаётся Date.MinValue
                Case TypeOf rawValue Is Date  ' ← ДОБАВИТЬ ЭТОТ CASE
                    result = DirectCast(rawValue, Date)
                Case TypeOf rawValue Is Double
                    result = Date.FromOADate(DirectCast(rawValue, Double))

                Case TypeOf rawValue Is String
                    Dim s As String = DirectCast(rawValue, String).Trim()
                    If s.Length > 0 Then
                        Dim formats = {"dd.MM.yyyy HH:mm:ss", "dd.MM.yyyy HH:mm", "dd.MM.yyyy"}
                        Date.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, result)
                        ' Если не удалось — result остаётся Date.MinValue
                    End If
            ' Добавим защиту от DBNull (на всякий случай)
                Case rawValue Is DBNull.Value
                    ' остаётся Date.MinValue
            End Select

            ' Обнуляем время, только если нужно и дата валидна
            If Not ShowTime AndAlso result > Date.MinValue Then
                result = result.Date
            End If
            Return result
        End Function

        Function Set4ToDate(rawValue As Object, Optional ShowTime As Boolean = True) As Date
            Dim result As Date = Date.MinValue

            Select Case True
                Case rawValue Is Nothing
            ' остаётся Date.MinValue

                Case TypeOf rawValue Is Date  ' ← ДОБАВИТЬ ЭТОТ CASE
                    result = DirectCast(rawValue, Date)

                Case TypeOf rawValue Is Double
                    result = Date.FromOADate(DirectCast(rawValue, Double))
                Case TypeOf rawValue Is String
                    'Dim s As String = DirectCast(rawValue, String).Trim()

                    ' 1. Убираем переносы строк (заменяем на пробел)
                    ' 2. Схлопываем двойные пробелы в один
                    Dim s As String = DirectCast(rawValue, String).Replace(vbCrLf, " ").Replace(vbLf, " ").Trim()

                    ' Чтобы "01.03.26    00:20" стало "01.03.26 00:20"
                    Do While s.Contains("  ")
                        s = s.Replace("  ", " ")
                    Loop

                    If s.Length > 0 Then
                        Dim formats = {
                            "dd.MM.yy HH:mm:ss",
                            "dd.MM.yy HH:mm",
                            "dd.MM.yyyy HH:mm:ss",
                            "dd.MM.yyyy HH:mm",
                            "dd.MM.yyyy"
                        }
                        ' ✅ Ключевое исправление: используем ru-RU
                        If Date.TryParseExact(s, formats, CultureInfo.GetCultureInfo("ru-RU"), DateTimeStyles.None, result) Then
                            ' Успешно
                        Else
                            MW.InfoBLOK.AddItem($"[ERROR] Не удалось распарсить дату: '{s}'")
                        End If
                    End If
                Case rawValue Is DBNull.Value
                    ' остаётся Date.MinValue
            End Select

            If Not ShowTime AndAlso result <> Date.MinValue Then
                result = result.Date
            End If
            Return result
        End Function

        ' Новый метод в ExcelModule
        Private Function ParseHoursFromExcelMinutes(input As Object, Optional [default] As Single = 0) As Single
            Dim s = CStr(input)?.Trim()
            Dim minutes As Single

            If Single.TryParse(s, minutes) Then
                ' Конвертируем минуты → часы и округляем до 4 знаков
                Return Math.Round(minutes / 60, 2)
            End If

            Return [default]
        End Function

        ''' <summary>
        ''' Определяет формат данных (целые минуты или дробные часы) и возвращает значение в часах.
        ''' </summary>
        Private Function UniversalToHours(input As Object, Optional defaultValue As Single = 0.0F) As Single
            Dim s = CStr(input)?.Trim()
            Dim value As Single

            If Single.TryParse(s, value) Then

                ' 1. Проверяем, есть ли дробная часть
                If value <> Math.Truncate(value) Then
                    ' Есть дробная часть (например, 0.83, 1.25) -> это ТОЧНО новый формат (часы)
                    ' Старый формат в минутах не бывает дробным, поэтому просто возвращаем как есть.
                    Return Math.Round(value, 2)
                End If

                ' 2. Если дробной части нет (например, 50, 120, 15) -> это ТОЧНО старый формат (минуты)
                ' Задержка в 50 часов невозможна, значит 50 — это минуты. Делим на 60.
                Return Math.Round(value / 60.0F, 2)

            End If

            ' Если парсинг не удался (пустая ячейка, текст и т.д.)
            Return defaultValue
        End Function




        '================ ЗАГРУЗКА 4 ОТЧЕТА =======================
        '================ ЗАГРУЗКА 4 ОТЧЕТА =======================
        '================ ЗАГРУЗКА 4 ОТЧЕТА =======================

        ' 🔹 Фиксированные номера столбцов — как в report.xls
        'Const COL_ID As Integer = 1        ' #
        'Const COL_ASU As Integer = 2       ' АСУ
        'Const COL_KAT As Integer = 3       ' Категория
        'Const COL_NACH As Integer = 4      ' Начало
        'Const COL_OKONCH As Integer = 5    ' Окончание (время возобновления движения!)
        'Const COL_STATUS As Integer = 6    ' Статус
        'Const COL_OT As Integer = 7        ' От
        'Const COL_KOMU As Integer = 8      ' Кому
        'Const COL_MESTO As Integer = 9     ' Место отказа
        'Const COL_TEH As Integer = 10      ' Тех.средство → OTSLev3




        Public Sub Load4ReportOld(filePath As String, otsList As List(Of Otkaz))
            ExcelPackage.License.SetNonCommercialPersonal("СергейВалерьевич")

            Using package As New ExcelPackage(New FileInfo(filePath))
                Dim ws = package.Workbook.Worksheets.FirstOrDefault()
                If ws Is Nothing Then
                    MW.InfoBLOK.AddItem("КАС АНТ: лист не найден")
                    Return
                End If

                Dim lastRow = ws.Dimension?.End.Row
                If lastRow < 2 Then
                    MW.InfoBLOK.AddItem("КАС АНТ: данных нет")
                    Return
                End If

                Dim DateOfFirstOTS As Date

                ' маркеры "ачато расслед", "ередан друго", "азначен", "ринят к уч"
                Dim States As New List(Of String) From {"ачато расслед", "ередан друго", "азначен", "ринят к уч"}

                ' маркеры "ередан друго", "азначен", "ринят к уч"
                Dim States2 As New List(Of String) From {"ередан друго", "азначен", "ринят к уч"}

                ' --- НОВОЕ: Список для хранения пар Старый ID -> Новый ID и ссылки на объект ---
                Dim changedIdsList As New ObservableCollection(Of IdMappingInfo)()
                ' ---------------------------------------------------------------------------


                For row As Integer = 2 To lastRow
                    Dim id = ParseString(ws.Cells(row, 1).Value)
                    If String.IsNullOrEmpty(id) OrElse id.Length < 5 Then Continue For

                    'делаем сразу, потом пригодится
                    Dim AsuEXCEL As String = ParseString(ws.Cells(row, 2).Value)
                    Dim KtoZakrEXCEL As String = Clean_TCH_TR(ws.Cells(row, 8).Value)
                    Dim MestoOTS_TXTEXCEL As String = ParseString(ws.Cells(row, 9).Value)
                    Dim KatEXCEL As Integer = ParseInt(ws.Cells(row, 3).Value)
                    Dim IstochnikEXCEL As String = ParseString(ws.Cells(row, 7).Value)
                    Dim NachEXCEL As Date = Set4ToDate(ws.Cells(row, 4).Value, ShowTime:=True)
                    Dim OTSLev3EXCEL As String = ParseString(ws.Cells(row, 10).Value)
                    Dim statusEXCEL As String = ParseString(ws.Cells(row, 6).Value)


                    ' ——— Найти или создать ———
                    Dim o = otsList.FirstOrDefault(Function(x) x.Id = id)
                    Dim isNew As Boolean = False
                    Dim notes As New List(Of String)
                    Dim NeedAddNoteFlg As Boolean = False



                    '==========================================================================
                    ' Если по новому ID не нашли, пробуем найти СТАРУЮ запись по дате и месту


                    '======================== новое
                    Dim isDubl As Boolean = False
                    Dim isoldId As String = ""
                    '========================


                    If o Is Nothing Then

                        '======================== новое
                        isDubl = True
                        '========================

                        Dim existingO = otsList.FirstOrDefault(Function(x) x.Nach = NachEXCEL AndAlso
                 NormalizeForCompare(x.MestoOTS_TXT, stripTrains:=True) = NormalizeForCompare(MestoOTS_TXTEXCEL, stripTrains:=True) AndAlso
                 Not String.IsNullOrWhiteSpace(NormalizeForCompare(x.MestoOTS_TXT, stripTrains:=True)))

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
                            existingO.AddUpdateNote("!!!Изменен № ОТС")
                            '========================

                        End If
                    End If
                    '==========================================================================



                    'если такого номера нет в отказах
                    If o Is Nothing Then
                        isNew = True ' обновляем значение, чтобы знать, что новый

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

                        o.Id = id
                        o.Kat = KatEXCEL

                        o.Nach = NachEXCEL
                        o.KtoZakryl = KtoZakrEXCEL
                        o.ZaKem = "!"   ' ← присаиваем сразу, потом изменим если надо будет

                        o.MestoOTS_TXT = MestoOTS_TXTEXCEL.Replace(" ,", ",")
                        o.IsStation = Not (o.MestoOTS.Contains(" - "))
                        'если ОТС новый, то добавляем
                        o.OTSLev3 = OTSLev3EXCEL

                        If AsuEXCEL.ToLower.Contains("ручной ввод") AndAlso o.MestoOTS_Dor.ToLower.Contains("расноярс") Then
                            o.Istochnik = "РУЧНОЙ ВВОД"
                        Else
                            o.Istochnik = IstochnikEXCEL
                            ''а теперь проверяем....
                            ''отдельно ВСЖД (на этот момент уже CleanTCH есть)
                            '' Просто вызываем функцию и присваиваем результат
                            'Dim newIstochnik As String = GetIstochnikOtkaza(o.MestoOTS, o.MestoOTS_TXT, o.KtoZakryl)

                            '' Если функция что-то определила, записываем (чтобы не затереть существующий Источник пустым значением)
                            'If Not String.IsNullOrEmpty(newIstochnik) Then
                            '    o.Istochnik = newIstochnik
                            'End If

                            'If Not IsRightIstocnik(o.Istochnik) Then
                            '    NeedAddNoteFlg = True
                            'End If

                        End If

                        otsList.Add(o)
                    End If

                    'но чтобы + не перебивало
                    If isNew Then
                        notes.Add("Дата поступления")
                        If NeedAddNoteFlg Then
                            notes.Add("От кого ОТС")
                        End If
                    Else
                        If NachEXCEL <> o.Nach Then
                            o.History.Add(New HistoryEntry With {.ShowDate = True, .IsRed = True, .EventDate = Now, .Description = $"Изменена дата начала с {o.Nach} на {NachEXCEL}"})
                            o.Nach = NachEXCEL
                        End If
                        If o.Uslovie4 Then
                            notes.Add("OK") ' сразу, чтобы видно было - какой отказ проверен, какой - нет
                        End If
                    End If

                    ' теперь в любом случае, что новый, что старый отказ
                    If AsuEXCEL.ToLower.Contains("ручной ввод") Then
                        If Not isNew AndAlso o.Istochnik <> "РУЧНОЙ ВВОД" AndAlso o.MestoOTS_Dor.ToLower.Contains("расноярс") Then
                            notes.Add("Ручной ввод")
                        End If
                    End If

                    If (statusEXCEL.ToLower.Contains("в еасапр") OrElse statusEXCEL.ToLower.Contains("личие реклам")) AndAlso statusEXCEL.ToLower.Contains("ачато расслед") Then
                        If o.ZaKem = "" OrElse o.ZaKem = "!" Then
                            notes.Add("Сохранен")
                        End If
                    End If

                    ' Одной строкой проверяем наличие любого маркера в текущем статусе
                    '"ачато расслед", "ередан друго", "азначен", "ринят к уч"
                    'просто сравнивай с True, чтобы превратить «возможно пусто» в конкретное «да/нет».
                    If States.Any(Function(s) statusEXCEL?.ToLower().Contains(s)) AndAlso (o.Zakryt > Date.MinValue) Then
                        notes.Add("Восстановлен")

                    ElseIf States2.Any(Function(s) statusEXCEL?.ToLower().Contains(s)) AndAlso (o.IsSaved) Then
                        'добавил для проверки на восстановленность сохраненных ОТС
                        notes.Add("Восстановлен")
                    End If



                    ' Был "др дорога", а теперь → вернулся??
                    If Not isNew Then
                        If (o.ZaKemCode?.Contains("орог")) Then
                            notes.Add("Вернулся")
                        End If
                    End If


                    ' проверяем не изменилось ли расследующее депо
                    If o.KtoZakryl <> KtoZakrEXCEL Then
                        notes.Add($"за {KtoZakrEXCEL}?")
                    End If


                    '' проверяем не изменилось ли место отказа
                    If o.MestoOTS_TXT <> MestoOTS_TXTEXCEL Then
                        ' Сохраняем старое значение
                        o.PreviousMestoOTS_TXT = o.MestoOTS_TXT
                        ' Обновляем новое
                        o.MestoOTS_TXT = MestoOTS_TXTEXCEL
                        ' Добавляем пометку (для триггера видимости)
                        notes.Add("Испр место ОТС")
                    End If

                    ' проверяем не изменилась ли категория
                    If o.Kat <> KatEXCEL Then
                        notes.Add($"кат {KatEXCEL}")
                    End If

                    ' ——— Обработка статуса ———
                    ' 🔸 "Расследован" → ZaKem = "-", только если ! или пусто (как в макросе)
                    If statusEXCEL.StartsWith("Расследован", StringComparison.Ordinal) Then
                        If o.ZaKem = "" OrElse o.ZaKem = "!" OrElse o.IsSaved OrElse ((o.KtoZakryl.ToLower.Contains("трп") AndAlso o.Zakryt = Date.MinValue)) Then
                            notes.Add("уже закрыт")
                        End If
                    End If

                    ' ——— Записываем пометки — OK там точно уже есть ———
                    o.UpdateNotes += String.Join("; ", notes)
                Next
                ' ——— ПОСЛЕ ИМПОРТА:
                ' берем все номера отказов из отчета———

                Dim idsInReport As New HashSet(Of String)
                For row As Integer = 4 To lastRow
                    Dim id = ParseString(ws.Cells(row, 1).Value)
                    If id.Length >= 5 Then idsInReport.Add(id)
                Next
                'теперь ОК и т.д. ставятся с первой даты в отчете и далее DateOfFirstOTS - в запросе теперь
                DateOfFirstOTS = Set4ToDate(ws.Cells(4, 4).Value).Date
                Dim outdatedOtkazy = otsList.Where(Function(o) o.KomplexAsInt > 0 AndAlso Not idsInReport.Contains(o.Id) AndAlso o.Nach.Date >= DateOfFirstOTS)

                For Each o In outdatedOtkazy
                    If Not o.UpdateNotes.Contains("???") Then
                        o.AddUpdateNote("???")

                    End If

                    If o.IsSaved OrElse o.Zakryt > Date.MinValue Then
                        o.AddUpdateNote("Восстановлен")

                    End If
                Next

            End Using
        End Sub



        Public Function GetIstochnikOtkaza(mestoOTS As String, mestoTXT As String, ktoZakryl As String) As String
            ' Статический список, чтобы не выделять память при каждом вызове
            Static VSIBstations As New List(Of String) From {"Юрты", "Бирюсинск", "Тайшет", "Разгон", "Байроновка", "Алзамай", "Замзор", "Камышет", "Ук", "Нижнеудинск"}
            Static KrasStations As New List(Of String) From {"Саянская", "Унерчик", "Хайрузовка", "Кравченко", "Мана", "Хабайдак", "Щетинкино", "Сисим", "Джебь", "Кошурниково", "Стофато", "Журавлево", "Ирба", "Краснокаменск", "Курагино", "Туба", "Кизир", "Жайма", "Лукашевич", "Крол", "Кой", "Агул", "Туманный", "Коростелево", "Тарбинский", "Саранчет", "Кварцит", "Запань", "Тагул"}

            Dim mLower As String = If(mestoOTS, "").ToLower().Trim()
            Dim tLower As String = If(mestoTXT, "").ToLower()
            Dim kLower As String = If(ktoZakryl, "").ToLower()

            ' Логика ВСЖД
            If tLower.Contains("осточно-сибир") Then
                ' Если закрыли  (ЧЭ-2) или  (ЧЭ-3)
                If kLower.Contains("чэ-2") OrElse kLower.Contains("чэ-3") Then
                    If Not IsStationInList(VSIBstations, mLower) Then Return "ВСЖД"
                    ' Если закрыли Боготол (ЧЭ-1) или Абакан (ЧЭ-7) - передано с ВСЖД
                ElseIf kLower.Contains("чэ-1") OrElse kLower.Contains("чэ-7") Then
                    Return "ВСЖД"
                End If


                'Логика Крас жд
            ElseIf tLower.StartsWith("рег") Then
                If Not IsStationInList(KrasStations, mLower) Then Return "ГИД УРАЛ"


                ' Логика других дорог
            ElseIf tLower.Contains("абайкальс") Then
                Return "ЗАБЖД"
            ElseIf tLower.Contains("альневосточ") Then
                Return "ДВЖД"
            End If
            ' Если ни одно условие не сработало, возвращаем пустую строку 
            Return ""
        End Function


        ''' <summary>
        ''' Проверяет, содержится ли станция из списка в строке mLower
        ''' </summary>
        Private Function IsStationInList(stations As List(Of String), mLower As String) As Boolean
            Return stations.Any(Function(s)
                                    Dim st As String = s.ToLower()
                                    ' Проверка на полное совпадение или наличие в строке с границами-пробелами
                                    Return mLower = st OrElse mLower.Contains(st & " ") OrElse mLower.Contains(" " & st)
                                End Function)
        End Function



        Function IsRightIstocnik(Vall As String) As Boolean
            Dim Flg As Boolean = False
            If IstRails.Contains(Vall) Then
                Flg = True
            End If
            Return Flg
        End Function

        Friend Function Clean_TCH_TR(input As Object) As String
            Dim s = ParseString(input)
            s = Regex.Replace(s, "^(ТР|Т|КРАС),\s*", "")
            Return s.Trim()
        End Function


        Public Function NormalizeForCompare(raw As String, Optional stripTrains As Boolean = False,
                                    Optional uniqueTrains As Boolean = False) As String

            Dim s As String = raw

            ' 0. НОВОЕ: Если нужно отрезать список поездов
            If stripTrains Then
                Dim idx = s.IndexOf("поезд", StringComparison.OrdinalIgnoreCase)
                If idx >= 0 Then
                    s = s.Substring(0, idx)
                End If
            End If


            '1. Убираем переносы и неразрывные пробелы (заменяем на обычный пробел)
            s = s.Replace(vbCrLf, " ").Replace(vbCr, " ").Replace(vbLf, " ").Replace(ChrW(&HA0), " ")

            ' 2. Пробелы вокруг запятой: убираем пробелы ДО, оставляем ровно 1 ПОСЛЕ
            s = Regex.Replace(s, "\s*,\s*", ", ")

            ' 3. Пробелы вокруг дефиса/тире (ловит -, –, —)
            s = Regex.Replace(s, "\s*[-–—]\s*", " - ")

            ' 4. Нормализуем "путь X" и "XкмYпк" (ровно 1 пробел вокруг)
            s = Regex.Replace(s, "\s*(путь\s*\d+)\s*", " $1 ")
            s = Regex.Replace(s, "\s*(\d+км(?:\d+пк)?)\s*", " $1 ")

            ' 5. Схлопываем все множественные пробелы в один
            s = Regex.Replace(s, "\s+", " ")

            ' 6. Убираем пробелы по краям и приводим к нижнему регистру (опционально, но надёжно)
            s = s.Trim() '.ToLowerInvariant()

            ' 7. НОВОЕ: Уникализируем номера поездов (игнорируем дубликаты)
            If uniqueTrains Then
                Dim trainPattern = "поезда?\s*№\s*(.+)"
                Dim match = Regex.Match(s, trainPattern, RegexOptions.IgnoreCase)
                If match.Success Then
                    Dim trainsPart = match.Groups(1).Value
                    ' Извлекаем все числа
                    Dim numbers = Regex.Matches(trainsPart, "\d+").Cast(Of Match)().
                                  Select(Function(m) m.Value).ToList()
                    ' Уникальные и отсортированные по числовому значению
                    Dim uniqueNumbers = numbers.Distinct().
                                        OrderBy(Function(n) Integer.Parse(n)).ToList()
                    ' Заменяем часть после "поезда №"
                    Dim prefix = s.Substring(0, match.Index)
                    s = prefix & "поезда №" & String.Join(", ", uniqueNumbers)
                End If
            End If
            Return s
        End Function




        Public Function FormatLokData(repairs As ObservableCollection(Of Remont)) As String
            If repairs Is Nothing OrElse repairs.Count = 0 Then Return String.Empty

            Dim parts As New List(Of String)
            For Each r In repairs
                ' Пропускаем полностью пустые записи
                If String.IsNullOrWhiteSpace(r.RepairType) AndAlso Not r.Mileage.HasValue AndAlso String.IsNullOrWhiteSpace(r.RepairPlace) Then Continue For

                'Dim datePart = If(String.IsNullOrWhiteSpace(repairDate), "", $" {repairDate} ")
                Dim mileagePart = If(r.Mileage.HasValue, $"пробег {r.Mileage.Value} км", "")

                ' Собираем часть: "ТР-1 проходил 10.02.2026 СЛД Дальневосточное, пробег 25661 км"
                Dim line = $"{r.RepairType} проходил {r.RepairPlace}"
                If Not String.IsNullOrEmpty(mileagePart) Then line &= $", пробег {mileagePart}"

                parts.Add(line)
            Next

            ' Склеиваем через точку с пробелом, добавляем префикс
            Return $"Данные на локомотив: {String.Join(". ", parts)}."
        End Function

        Public Function FormatHistoryToExcelText(history As ObservableCollection(Of HistoryEntry)) As String
            If history Is Nothing OrElse history.Count = 0 Then Return String.Empty

            Dim lines As New List(Of String)
            For Each entry In history
                ' Пропускаем пустые записи
                If String.IsNullOrWhiteSpace(entry.Description) Then Continue For

                ' Формируем дату только если в записи включён флаг ShowDate
                Dim datePart = If(entry.ShowDate, $"{entry.EventDate:dd.MM.yy} ", "")
                lines.Add($"{datePart}{entry.Description}")
            Next

            Return String.Join(vbCrLf, lines)
        End Function

    End Module

End Namespace

