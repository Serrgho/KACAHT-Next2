Imports System.Collections.ObjectModel
Imports System.IO
Imports System.Net.Http
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks
Imports System.Windows
Imports Microsoft.Office.Interop.Word
'Imports Microsoft.Office.Interop.Word
Imports OfficeOpenXml
Imports OfficeOpenXml.Drawing.EMF
Imports OfficeOpenXml.Style

Namespace Kas

    Public Module KasGenReportModule
        Public LastFilteredOut As Integer = 0
        ' =====================================================================
        ' КЛАСС ДАННЫХ СТРОКИ ОТЧЕТА
        ' Колонки соответствуют макросу PCHASCompNew
        ' =====================================================================
        Public Class GenReportRow
            Public Property Category As Integer?          ' Кол.1  - OTSKat
            Public Property ViolId As String               ' Кол.2  - OTSNu (ID отказа)
            Public Property StartTime As String           ' Кол.3  - OTSNach
            Public Property EndTime As String             ' Кол.4  - OTSOkon
            Public Property Duration As String            ' Кол.5  - OTSDlit
            Public Property Road As String                ' Кол.6  - OTSDor
            Public Property Region As String              ' Кол.7  - OTSReg
            Public Property Location As String            ' Кол.8  - OTSMesto
            Public Property MestoOTS_TXT As String         ' сборная строка
            Public Property Investigator As String        ' Кол.10 - OTSUKogo (ответственный)
            Public Property GuiltyRoad As String          ' Кол.11 - OTSZakemDor
            Public Property GuiltyDepot As String         ' Кол.13 - OTSZakemPredpr (виновный)

            Public Property TotalTrains As Integer?       ' Кол.14 - OTSTrainsAll
            Public Property PChTotal As Single?           ' Кол.15 - OTSPCH (поездо-часы)

            ' Грузовые
            Public Property GrCount As Integer?           ' Кол.16
            Public Property GrNumbers As String           ' Кол.17
            Public Property GrPCh As Single?              ' Кол.18

            ' Пассажирские
            Public Property PasCount As Integer?          ' Кол.19
            Public Property PasNumbers As String          ' Кол.20
            Public Property PasPCh As Single?             ' Кол.21

            ' Пригородные
            Public Property PrigCount As Integer?         ' Кол.22
            Public Property PrigNumbers As String         ' Кол.23
            Public Property PrigPCh As Single?            ' Кол.24

            ' Оборудование (КАСАНТ-уровни)
            Public Property OTSLev1 As String             ' Кол.25 - Тех. средство
            Public Property OTSLev2 As String             ' Кол.26 - Составная часть
            Public Property OTSLev3 As String             ' Кол.29 - Элемент / Группа причин

            ' Дополнительные поля для UI
            Public Property RowNumber As Integer          ' Номер строки в отчете
        End Class


        ' =====================================================================
        ' ПОКАЗ ОТЧЕТА ИЗ ЛОКАЛЬНОГО ФАЙЛА (для тестирования)
        ' =====================================================================
        Public Function ShowGenReportFromLocalFile(filePath As String) As Boolean
            Try
                If String.IsNullOrWhiteSpace(filePath) OrElse Not File.Exists(filePath) Then
                    Log($"❌ Файл не найден: {filePath}")
                    MessageBox.Show($"Файл не найден: {filePath}", "Ошибка",
                          MessageBoxButton.OK, MessageBoxImage.Warning)
                    Return False
                End If

                Log($"📂 Чтение локального файла: {filePath}")
                Dim rows = ParseExcelGenReport(filePath, SaveFilteredItog:=False)

                If rows.Count = 0 Then
                    MessageBox.Show("Файл пуст или не удалось распарсить данные.",
                          "Информация", MessageBoxButton.OK, MessageBoxImage.Information)
                    Return False
                End If

                Dim win As New GenReportWindow() With {
            .Owner = MW,
            .Title = $"[ЛОКАЛЬНО] Генеральный отчет ({rows.Count} записей) — {Path.GetFileName(filePath)}"
        }
                win.LoadData(rows)
                win.Show()

                Log($"✅ Окно открыто из файла: {rows.Count} записей")
                Return True

            Catch ex As Exception
                Log($"💥 Ошибка чтения файла: {ex.Message}")
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                       MessageBoxButton.OK, MessageBoxImage.Error)
                Return False
            End Try
        End Function

        ' =====================================================================
        ' ВЫБОР ФАЙЛА ЧЕРЕЗ ДИАЛОГ И ПОКАЗ ОТЧЕТА
        ' =====================================================================
        Public Function ShowGenReportFromFileDialog() As Boolean
            Dim dlg As New Microsoft.Win32.OpenFileDialog() With {
        .Filter = "Excel файлы|*.xls;*.xlsx|Все файлы|*.*",
        .Title = "Выберите файл Генерального отчета",
        .InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
    }

            If dlg.ShowDialog() <> True Then Return False

            Return ShowGenReportFromLocalFile(dlg.FileName)
        End Function


        ' =====================================================================
        ' ГЛАВНЫЙ МЕТОД: скачать + распарсить + показать окно
        ' =====================================================================
        Public Async Function ShowGenReportWindowAsync() As System.Threading.Tasks.Task

            Try
                Log("📊 Запрос и отображение ГенОтчета...")

                ' Получаем готовый список строк одной строкой
                Dim rows As List(Of GenReportRow)
                rows = Await DownloadAndParseGenReportAsync(Fetcher.NachDat, Fetcher.KonDat, SaveFilteredItog:=False)

                If rows Is Nothing Then Exit Function

                ' 3. Создаём и показываем окно
                Dim win As New GenReportWindow() With {
                    .Owner = MW,
                    .Title = $"ГенОтчет КАСАНТ ({rows.Count} записей)"
                }
                win.LoadData(rows)
                win.Show()

                Log($"✅ Окно ГенОтчета открыто: {rows.Count} записей")
                rows = Nothing
            Catch ex As Exception

                Log($"💥 Ошибка показа окна: {ex.Message}")
                ShowMSG(MW, $"Ошибка: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error)
            End Try

        End Function


        ' =====================================================================
        ' УНИВЕРСАЛЬНАЯ ФУНКЦИЯ: скачивание + парсинг ГенОтчета
        ' Возвращает список строк или Nothing в случае ошибки
        ' =====================================================================
        Public Async Function DownloadAndParseGenReportAsync(dateFrom As DateTime, dateTo As DateTime, SaveFilteredItog As Boolean) As Task(Of List(Of GenReportRow))
            Try
                Log("📊 Запрос и отображение ГенОтчета...")

                ' 1. Скачиваем Excel через существующую сессию
                Dim filePath = Await CentralFetcher.DownloadGenReportAsync(dateFrom, dateTo)

                If String.IsNullOrWhiteSpace(filePath) OrElse Not File.Exists(filePath) Then
                    Log("❌ Не удалось скачать ГенОтчет")
                    ShowMSG(MW, "Не удалось скачать ГенОтчет. Проверьте сессию.",
                          "Ошибка", MessageBoxButton.OK)
                    Return Nothing
                End If

                ' 2. Парсим
                Dim rows = ParseExcelGenReport(filePath, SaveFilteredItog:=SaveFilteredItog)

                If rows.Count = 0 Then
                    Log("⚠ Отчет пуст или не удалось распарсить данные")
                    ShowMSG(MW, "Отчет пуст или не удалось распарсить данные.",
                          "Информация", MessageBoxButton.OK, MessageBoxImage.Information)
                    Return Nothing
                End If

                Log($"✅ ГенОтчет успешно загружен: {rows.Count} записей")
                Return rows

            Catch ex As Exception
                Log($"💥 Ошибка при загрузке ГенОтчета: {ex.Message}")
                ShowMSG(MW, $"Ошибка при загрузке ГенОтчета: {ex.Message}",
                      "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error)
                Return Nothing
            End Try
        End Function


        ' =====================================================================
        ' ПАРСИНГ СКАЧАННОГО EXCEL-ФАЙЛА
        ' =====================================================================
        Public Function ParseExcelGenReport(filePath As String, SaveFilteredItog As Boolean) As List(Of GenReportRow)

            Dim result As New List(Of GenReportRow)
            Dim convertedPath As String = Nothing
            Dim rowNum As Integer = 0
            Dim skippedRows As Integer = 0
            Dim filteredOut As Integer = 0

            ' 🆕 Список индексов строк, которые нужно будет удалить
            Dim rowsToDelete As New List(Of Integer)

            Try
                Dim ext = Path.GetExtension(filePath).ToLower()
                Dim actualPath = filePath

                If ext = ".xls" Then
                    Log($"⚠ Обнаружен старый формат .xls, конвертирую...")
                    convertedPath = ConvertXlsToXlsx(filePath)

                    If String.IsNullOrWhiteSpace(convertedPath) OrElse Not File.Exists(convertedPath) Then
                        Log("❌ Не удалось конвертировать файл")
                        Return result
                    End If
                    actualPath = convertedPath
                End If

                ExcelPackage.License.SetNonCommercialPersonal("СергейВалерьевич")

                Using package As New ExcelPackage(New FileInfo(actualPath))
                    Dim ws = package.Workbook.Worksheets(0)
                    If ws Is Nothing OrElse ws.Dimension Is Nothing Then
                        Log("⚠ В Excel нет данных")
                        Return result
                    End If

                    Dim lastRow = ws.Dimension.End.Row
                    Log($"📊 ГенОтчет: {lastRow} строк")

                    ' ═══════════════════════════════════════════════════════════════
                    ' ШАГ 1: ЧИТАЕМ ДАННЫЕ И СОБИРАЕМ ИНДЕКСЫ ДЛЯ УДАЛЕНИЯ
                    ' ═══════════════════════════════════════════════════════════════
                    For row As Integer = 3 To lastRow
                        Dim keepRow As Boolean = True

                        Dim violIdCell = ws.Cells(row, 2).Value

                        If violIdCell Is Nothing Then
                            skippedRows += 1
                            keepRow = False
                        Else
                            Dim violIdRaw As String = violIdCell.ToString().Trim()
                            Dim temp As Long
                            If Not Long.TryParse(violIdRaw, temp) Then
                                skippedRows += 1
                                keepRow = False
                            End If
                        End If

                        Dim investigatorRaw As String = ""
                        If keepRow Then
                            investigatorRaw = SafeStr(ws.Cells(row, 10).Value)
                            If String.IsNullOrEmpty(investigatorRaw) OrElse Not investigatorRaw.Contains("КРАС") Then
                                filteredOut += 1
                                keepRow = False
                            End If
                        End If

                        If Not keepRow Then
                            If SaveFilteredItog Then rowsToDelete.Add(row) ' Сохраняем индекс строки (в EPPlus строки нумеруются с 1)
                            Continue For
                        End If

                        ' ═══════════════════════════════════════════════════════════════
                        ' ПАРСИМ ХОРОШУЮ СТРОКУ (твой существующий код)
                        ' ═══════════════════════════════════════════════════════════════
                        Dim violId As String = violIdCell.ToString().Trim()
                        Dim MestoOTS_TXT, Doroga, REG, Mesto, Dop, Poezda As String

                        Doroga = SafeStr(ws.Cells(row, 6).Value)
                        REG = SafeStr(ws.Cells(row, 7).Value)
                        Mesto = SafeStr(ws.Cells(row, 8).Value)
                        Dop = SafeStr(ws.Cells(row, 9).Value)
                        Poezda = SafeStr(ws.Cells(row, 20).Value) & " " &
                                 SafeStr(ws.Cells(row, 17).Value) & " " &
                                 SafeStr(ws.Cells(row, 23).Value)

                        MestoOTS_TXT = getMestoTXT(Doroga, REG, Mesto, Dop, Poezda)
                        investigatorRaw = Clean_TCH_TR(investigatorRaw)
                        rowNum += 1

                        Dim r As New GenReportRow With {
                            .RowNumber = rowNum,
                            .ViolId = violId,
                            .Category = ParseInt(ws.Cells(row, 1).Value),
                            .StartTime = SafeStr(ws.Cells(row, 3).Value),
                            .EndTime = SafeStr(ws.Cells(row, 4).Value),
                            .Duration = SafeStr(ws.Cells(row, 5).Value),
                            .Road = SafeStr(ws.Cells(row, 6).Value),
                            .Region = SafeStr(ws.Cells(row, 7).Value),
                            .Location = SafeStr(ws.Cells(row, 8).Value),
                            .MestoOTS_TXT = MestoOTS_TXT,
                            .Investigator = investigatorRaw,
                            .GuiltyRoad = SafeStr(ws.Cells(row, 11).Value),
                            .GuiltyDepot = SafeStr(ws.Cells(row, 13).Value),
                            .TotalTrains = ParseInt(ws.Cells(row, 14).Value),
                            .PChTotal = ParseSingle(ws.Cells(row, 15).Value),
                            .GrCount = ParseInt(ws.Cells(row, 16).Value),
                            .GrNumbers = SafeStr(ws.Cells(row, 17).Value),
                            .GrPCh = ParseSingle(ws.Cells(row, 18).Value),
                            .PasCount = ParseInt(ws.Cells(row, 19).Value),
                            .PasNumbers = SafeStr(ws.Cells(row, 20).Value),
                            .PasPCh = ParseSingle(ws.Cells(row, 21).Value),
                            .PrigCount = ParseInt(ws.Cells(row, 22).Value),
                            .PrigNumbers = SafeStr(ws.Cells(row, 23).Value),
                            .PrigPCh = ParseSingle(ws.Cells(row, 24).Value),
                            .OTSLev1 = SafeStr(ws.Cells(row, 25).Value),
                            .OTSLev2 = SafeStr(ws.Cells(row, 26).Value),
                            .OTSLev3 = SafeStr(ws.Cells(row, 29).Value)
                        }
                        result.Add(r)
                    Next

                    Log($"✓ Распарсено: {result.Count} записей (КРАС)")
                    Log($"   Пустых: {skippedRows}, отфильтровано не-КРАС: {filteredOut}")

                End Using   ' ← ExcelPackage закрыт, файл больше не заблокирован

                ' ═══════════════════════════════════════════════════════════════
                ' 🆕 ШАГ 2: ФИЗИЧЕСКОЕ УДАЛЕНИЕ СТРОК ЧЕРЕЗ INTEROP И СОХРАНЕНИЕ КАК .xls
                ' ═══════════════════════════════════════════════════════════════

                If SaveFilteredItog AndAlso rowsToDelete.Count > 0 Then
                    Log($"🗑 Удаляем {rowsToDelete.Count} лишних строк из исходного .xls файла...")

                    ' ✅ Сохраняем на Рабочий стол с именем "1. ГенОтч - новый.xls"
                    Dim finalSavePath As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "1. ГенОтч - новый.xls")

                    ' Вызываем процедуру удаления и сохранения
                    DeleteRowsAndSaveAsXls(rowsToDelete, convertedPath, finalSavePath)
                End If


            Catch ex As Exception
                Log($"💥 Ошибка парсинга: {ex.Message}{vbCrLf}{ex.StackTrace}")
            Finally
                ' ═══════════════════════════════════════════════════════════════
                ' 🗑 УДАЛЯЕМ ВРЕМЕННЫЙ ФАЙЛ ПОСЛЕ ЧТЕНИЯ
                ' ═══════════════════════════════════════════════════════════════
                If Not String.IsNullOrWhiteSpace(convertedPath) AndAlso File.Exists(convertedPath) Then
                    Try
                        File.Delete(convertedPath)
                        Log($"🗑 Временный файл удалён: {Path.GetFileName(convertedPath)}")
                    Catch delEx As Exception
                        Log($"⚠ Не удалось удалить временный файл: {delEx.Message}")
                    End Try
                End If
            End Try

            LastFilteredOut = filteredOut
            Return result




        End Function


        ' =====================================================================
        ' УДАЛЕНИЕ ЛИШНИХ СТРОК ИЗ XLSX И СОХРАНЕНИЕ КАК XLS
        ' =====================================================================
        Private Sub DeleteRowsAndSaveAsXls(rowsToDelete As List(Of Integer), sourceXlsxPath As String, finalXlsPath As String)
            Try
                ' 1. Открываем .xlsx через EPPlus и удаляем строки (быстро)
                ExcelPackage.License.SetNonCommercialPersonal("СергейВалерьевич")

                Using package As New ExcelPackage(New FileInfo(sourceXlsxPath))
                    Dim ws = package.Workbook.Worksheets(0)

                    If ws Is Nothing OrElse ws.Dimension Is Nothing Then
                        Log("⚠ В Excel нет данных для удаления")
                        Return
                    End If

                    Log($"🗑 Удаляем {rowsToDelete.Count} лишних строк из .xlsx...")

                    ' Сортируем и удаляем СНИЗУ ВВЕРХ
                    rowsToDelete.Sort()
                    For i As Integer = rowsToDelete.Count - 1 To 0 Step -1
                        ws.DeleteRow(rowsToDelete(i))
                    Next

                    ' Сохраняем очищенный .xlsx (перезаписываем временный файл)
                    package.Save()
                End Using

                Log($"✅ Строки удалены из .xlsx. Теперь конвертируем в .xls...")

                ' 2. Открываем очищенный .xlsx через Interop и сохраняем как .xls
                Dim excelApp As Microsoft.Office.Interop.Excel.Application = Nothing
                Dim wb As Microsoft.Office.Interop.Excel.Workbook = Nothing

                Try
                    excelApp = New Microsoft.Office.Interop.Excel.Application() With {
                .Visible = False,
                .DisplayAlerts = False
            }

                    ' Открываем очищенный .xlsx
                    wb = excelApp.Workbooks.Open(sourceXlsxPath)

                    ' Сохраняем как .xls
                    wb.SaveAs(finalXlsPath, Microsoft.Office.Interop.Excel.XlFileFormat.xlExcel8)
                    wb.Close(False)
                    excelApp.Quit()

                    Log($"💾 Очищенный .xls файл сохранён: {finalXlsPath}")

                Catch ex As Exception
                    Log($"💥 Ошибка при сохранении .xls через Interop: {ex.Message}")
                    If wb IsNot Nothing Then wb.Close(False)
                    If excelApp IsNot Nothing Then excelApp.Quit()
                End Try

            Catch ex As Exception
                Log($"💥 Ошибка при удалении строк: {ex.Message}")
            End Try
        End Sub




        Public Function getMestoTXT(Doroga As String, REG As String, Mesto As String, Dop As String, Poezda As String) As String

            Dim result As String = ""

            ' ==== ДО МЕСТА ОТС ====
            ' 1. Дорога (если не содержит "Красноярс")
            If Not String.IsNullOrWhiteSpace(Doroga) AndAlso Not Doroga.ToLower().Contains("красноярс") Then
                result &= Doroga & ", " & vbCrLf
            End If

            ' 2. РЕГ и МЕСТО (для всех случаев)
            result &= REG & ",  " & Mesto

            ' ==== ПОСЛЕ МЕСТА ОТС ====
            ' Сначала обрабатываем сырую строку поездов
            Dim formattedPoezda As String = ""
            If Not String.IsNullOrWhiteSpace(Poezda) Then
                formattedPoezda = FormPoezd(Poezda)
            End If

            Dim hasDop As Boolean = Not String.IsNullOrWhiteSpace(Dop)
            Dim hasPoezda As Boolean = Not String.IsNullOrWhiteSpace(formattedPoezda)

            ' Применяем правила формирования хвоста строки
            If hasDop AndAlso Not hasPoezda Then
                ' Условие: доп есть, а поездов нет
                result &= "  , " & Dop

            ElseIf Not hasDop AndAlso hasPoezda Then
                ' Условие: доп нет, а поезда есть 
                ' (2 пробела от пустого доп + 2 пробела перед "поезда" = 4 пробела)
                result &= "    поезда №" & formattedPoezda

            ElseIf hasDop AndAlso hasPoezda Then
                ' Условие: есть и доп, и поезда
                ' 2 пробела + запятая + пробел + {доп} + 2 пробела + "поезда №" + {номера}
                result &= "  , " & Dop & "  поезда №" & formattedPoezda
            End If
            ' Если оба пустые (Not hasDop AndAlso Not hasPoezda), ничего не добавляем.

            Return result


        End Function


        Public Function FormPoezd(Nums As String) As String
            ' 2. Убираем символ "№" и разбиваем строку на массив по запятым и пробелам
            Dim parts As String() = Nums.Replace("№", "").Split({","c, " "c}, StringSplitOptions.RemoveEmptyEntries)

            ' 3. Извлекаем только валидные числа (защита от случайного текста или пустых ячеек)
            Dim numbers As New List(Of Integer)()
            For Each part As String In parts
                Dim num As Integer
                If Integer.TryParse(part.Trim(), num) Then
                    numbers.Add(num)
                End If
            Next

            ' 4. Удаляем дубликаты (Distinct), сортируем по возрастанию (OrderBy) и собираем в строку
            Return String.Join(", ", numbers.OrderBy(Function(n) n))
        End Function


        ' =====================================================================
        ' ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ
        ' =====================================================================
        Private Function SafeStr(cellValue As Object) As String
            If cellValue Is Nothing Then Return ""
            Return cellValue.ToString().Trim()
        End Function

        Private Function ParseInt(cellValue As Object) As Integer?
            If cellValue Is Nothing Then Return Nothing
            Dim s = cellValue.ToString().Trim()
            If String.IsNullOrEmpty(s) Then Return Nothing
            Dim v As Integer
            If Integer.TryParse(s, v) Then Return v
            Return Nothing
        End Function

        Private Function ParseSingle(cellValue As Object) As Single?
            If cellValue Is Nothing Then Return Nothing
            Dim s = cellValue.ToString().Trim().Replace(".", ",")
            If String.IsNullOrEmpty(s) Then Return Nothing
            Dim v As Single
            If Single.TryParse(s, v) Then Return v
            Return Nothing
        End Function

        ' Логирование через CentralFetcher
        Public Sub Log(msg As String)
            Try
                LogWritePublic(msg)
            Catch
            End Try
        End Sub


        ' =====================================================================
        ' КОНВЕРТАЦИЯ .xls → .xlsx через Excel Interop
        ' =====================================================================
        Public Function ConvertXlsToXlsx(xlsPath As String) As String
            Dim excelApp As Microsoft.Office.Interop.Excel.Application = Nothing
            Dim wb As Microsoft.Office.Interop.Excel.Workbook = Nothing

            Try
                excelApp = New Microsoft.Office.Interop.Excel.Application() With {
                    .Visible = False,
                    .DisplayAlerts = False
                }

                wb = excelApp.Workbooks.Open(xlsPath)

                Dim xlsxPath = Path.Combine(
                    Path.GetDirectoryName(xlsPath),
                    Path.GetFileNameWithoutExtension(xlsPath) & "_converted.xlsx"
                )

                wb.SaveAs(xlsxPath, Microsoft.Office.Interop.Excel.XlFileFormat.xlOpenXMLWorkbook)
                wb.Close(False)
                excelApp.Quit()

                Log($"✓ Конвертировано: {xlsPath} → {xlsxPath}")
                Return xlsxPath

            Catch ex As Exception
                Log($"💥 Ошибка конвертации: {ex.Message}")
                If wb IsNot Nothing Then wb.Close(False)
                If excelApp IsNot Nothing Then excelApp.Quit()
                Return Nothing
            End Try
        End Function



        '===========================================================================
        '================ СИНХРОНИЗАЦИЯ С ОТЧЕТОМ ПО ОТКАЗАМ =======================
        '===========================================================================

        Public Sub SyncGenOTSReport(rows As List(Of GenReportRow))

            If rows Is Nothing OrElse rows.Count = 0 Then
                MW.InfoBLOK.AddItem("КАС АНТ: данных не получено")
                Return
            End If

            For Each rec In rows
                Dim o = OTSList.FirstOrDefault(Function(x) x.Id = rec.ViolId)
                If o Is Nothing Then

                    Dim GenOTS As Otkaz = GetNewOTSWithParams(rec)
                    GenOTS.Dlit = ToDecimalHours(rec.Duration)

                    If Not IsRightIstocnik(GenOTS.Istochnik) Then GenOTS.AddUpdateNote("От кого ОТС")
                    GenOTS.AddUpdateNote("Дата поступления")
                    'GenOTS.UpdateNotes += String.Join("; ", notes)
                    OTSList.Add(GenOTS)
                Else 'номер ОТС найден
                    o.Dlit = ToDecimalHours(rec.Duration)
                    If o.Postup = Date.MinValue Then
                        SetTrainsAndOborudLev(o, rec, SetPCH:=True)
                    Else

                        'молча добавить в историю если изменилось количество поездов
                        If rec.TotalTrains <> o.TotalTrainsKol Then
                            Dim fd As String
                            fd = $"изменено количество поездов с {o.TotalTrainsKol} до {rec.TotalTrains} на {rec.TotalTrains - o.TotalTrainsKol:+0;-0;0}"
                            o.AddHistoryEntry(DateTime.Now, fd)
                            o.SelectedHistoryEntry.ShowDate = False
                        End If

                        'проверка на изменение Часов (в ActionControl чтобы пользователь знал)
                        If o.PCh <> rec.PChTotal Then
                            Dim Delta As Single = rec.PChTotal - o.PCh
                            Delta = CSng(Math.Round(Delta, 2))

                            '!!!!!!!!!!!  тут в ActionControl !!!!!!!!!!
                            o.AddUpdateNote($"п/ч {rec.PChTotal}")
                            'не забываем про KorPCH - перенесено в ActionUserControl
                            ' вбить часы и количество по видам поездов
                            SetTrainsAndOborudLev(o, rec, SetPCH:=False)
                        End If
                    End If



                End If

            Next

            MW.InfoBLOK.AddItem($"✅ Обработано {rows.Count} отказов из веб-журнала. Excel больше не нужен.")

        End Sub





        ''' <summary>
        ''' ОТС с основными параметрами + часы + поезда
        ''' </summary>
        ''' <param name="rec"></param>
        ''' <returns></returns>
        Public Function GetNewOTSWithParams(rec As GenReportRow) As Otkaz
            Dim o = New Otkaz()

            o.Id = rec.ViolId
            o.Kat = rec.Category
            o.Nach = rec.StartTime

            o.MestoOTS_TXT = RemoveDoubleTrains(rec.MestoOTS_TXT) 'rec.MestoOTS_TXT 'NormalizeForCompare(rec.MestoOTS_TXT, uniqueTrains:=True)
            o.IsStation = Not (o.MestoOTS.Contains(" - "))

            o.ZaKem = "!"
            o.KtoZakryl = rec.Investigator

            SetTrainsAndOborudLev(o, rec)

            Return o
        End Function


        ''' <summary>
        ''' часы и количество по видам поездов
        ''' </summary>
        ''' <param name="o"></param>
        ''' <param name="rec"></param>
        Sub SetTrainsAndOborudLev(o As Otkaz, rec As GenReportRow, Optional SetPCH As Boolean = True)
            If SetPCH Then
                'Оставить старое значение, если новое — Nothing
                o.PCh = If(rec.PChTotal, o.PCh)
            End If

            o.GruzPCH = If(rec.GrPCh, o.GruzPCH)
            o.PrigPCH = If(rec.PrigPCh, o.PrigPCH)
            o.PasPCH = If(rec.PasPCh, o.PasPCH)

            o.GruzKol = If(rec.GrCount, o.GruzKol)
            o.PrigKol = If(rec.PrigCount, o.PrigKol)
            o.PasKol = If(rec.PasCount, o.PasKol)

            o.OTSLev1 = rec.OTSLev1
            o.OTSLev2 = rec.OTSLev2
            o.OTSLev3 = rec.OTSLev3
        End Sub

    End Module

End Namespace