
Imports System.IO
Imports OfficeOpenXml
Imports OfficeOpenXml.Style
Imports System.Diagnostics
Imports ExcelInterop = Microsoft.Office.Interop.Excel

Namespace Kas
    Module ReportsExcelModule

        ' =====================================================================
        ' БЛОК 1. ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ
        ' =====================================================================

        Private Sub AddLog(message As String)
            If Application.Current?.Dispatcher IsNot Nothing Then
                Application.Current.Dispatcher.Invoke(Sub() MW.InfoBLOK.AddItem(message))
            End If
        End Sub

        ' =====================================================================
        ' БЛОК 2. ЗАПОЛНЕНИЕ ЛИСТА
        ' =====================================================================

        Private Function FillWorksheet(worksheet As ExcelWorksheet,
                                       otsList As List(Of Otkaz),
                                       reportTitle As String,
                                       Optional extraColumn As Tuple(Of String, Func(Of Otkaz, Double)) = Nothing) As Integer

            Dim hasExtra As Boolean = extraColumn IsNot Nothing
            Dim lastCol As Integer = If(hasExtra, 13, 12)

            WriteHeaders(worksheet, hasExtra, extraColumn)
            Dim rowCount = WriteDataRows(worksheet, otsList, hasExtra, extraColumn)
            WriteFooter(worksheet, rowCount, reportTitle, hasExtra, otsList, extraColumn)
            ApplyFormatting(worksheet, rowCount, lastCol)
            SetColumnWidths(worksheet, hasExtra)
            SetBorders(worksheet, rowCount, lastCol)
            SetPrintSettings(worksheet)

            Return rowCount
        End Function

        Private Sub WriteHeaders(worksheet As ExcelWorksheet, hasExtra As Boolean, extraColumn As Tuple(Of String, Func(Of Otkaz, Double)))
            worksheet.Cells(2, 2).Value = "№ ОТС"
            worksheet.Cells(2, 3).Value = "кат"
            worksheet.Cells(2, 4).Value = "Начало"
            worksheet.Cells(2, 5).Value = "Поездо-часов"
            worksheet.Cells(2, 6).Value = "Привязано поездов"
            worksheet.Cells(2, 7).Value = "Расследует"
            worksheet.Cells(2, 8).Value = "Место отказа"
            worksheet.Cells(2, 9).Value = "Описание"
            worksheet.Cells(2, 10).Value = "Отнесен на"
            worksheet.Cells(2, 11).Value = "Поступил"
            worksheet.Cells(2, 12).Value = "От кого"

            If hasExtra Then worksheet.Cells(2, 13).Value = extraColumn.Item1
            worksheet.Cells(2, 2, 2, If(hasExtra, 13, 12)).AutoFilter = True
        End Sub

        Private Function WriteDataRows(worksheet As ExcelWorksheet, otsList As List(Of Otkaz), hasExtra As Boolean, extraColumn As Tuple(Of String, Func(Of Otkaz, Double))) As Integer
            Dim startRow As Integer = 3
            Dim rowCount As Integer = 0

            For Each o In otsList
                If String.IsNullOrEmpty(o.Id) Then Continue For
                Dim row As Integer = startRow + rowCount

                worksheet.Cells(row, 2).Value = o.Id
                worksheet.Cells(row, 3).Value = o.Kat
                If o.Nach > Date.MinValue Then worksheet.Cells(row, 4).Value = o.Nach.ToString("dd.MM.yy HH:mm")
                worksheet.Cells(row, 5).Value = Math.Round(o.PCh, 2)

                Dim parts As New List(Of String)
                If o.GruzKol > 0 Then parts.Add($"Груз: {o.GruzKol}{If(o.GruzPCH > 0.1, $" на {o.GruzPCH:F2}ч", "")}")
                If o.PasKol > 0 Then parts.Add($"Пасс: {o.PasKol}{If(o.PasPCH > 0.1, $" на {o.PasPCH:F2}ч", "")}")
                If o.PrigKol > 0 Then parts.Add($"Приг: {o.PrigKol}{If(o.PrigPCH > 0.1, $" на {o.PrigPCH:F2}ч", "")}")
                worksheet.Cells(row, 6).Value = String.Join($"{vbLf}{vbLf}", parts)

                worksheet.Cells(row, 7).Value = o.KtoZakryl
                worksheet.Cells(row, 8).Value = o.MestoOTS_TXT

                Dim Look As String = IIf(o.SerLokPripLokInRowTXT <> "", $"{o.SerLokPripLokInRowTXT},", "")
                Dim Maash As String = IIf(o.MashPripInRowTXT <> "", $"машинист {o.MashPripInRowTXT},", "")
                worksheet.Cells(row, 9).Value = $"{Look} {Maash} {o.Opis} {FormatLokData(o.DaNaLok)}{vbLf}{FormatHistoryToExcelText(o.History)}"
                worksheet.Cells(row, 10).Value = IIf(o.ZaKem <> "!", o.ZaKem, "")
                If o.Postup > Date.MinValue Then worksheet.Cells(row, 11).Value = o.Postup.ToString("dd.MM.yyyy")
                If Not o.Istochnik.ToUpper().Contains("ГИД") Then worksheet.Cells(row, 12).Value = o.Istochnik

                If hasExtra Then
                    worksheet.Cells(row, 13).Value = Math.Round(extraColumn.Item2(o), 2)
                    worksheet.Cells(row, 13).Style.Numberformat.Format = "+0.00;-0.00;0"
                End If

                rowCount += 1
            Next
            Return rowCount
        End Function

        Private Sub WriteFooter(worksheet As ExcelWorksheet, rowCount As Integer, reportTitle As String, hasExtra As Boolean, otsList As List(Of Otkaz), extraColumn As Tuple(Of String, Func(Of Otkaz, Double)))
            Dim lastRow As Integer = 3 + rowCount - 1
            Dim wi As Integer = If(hasExtra, 13, 12)

            worksheet.Cells(1, 2, 1, wi).Merge = True
            worksheet.Cells(1, 2).Value = reportTitle
            worksheet.Cells(1, 2).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center
            worksheet.Cells(1, 2).Style.VerticalAlignment = ExcelVerticalAlignment.Center
            worksheet.Cells(1, 2).Style.Font.Bold = True
            worksheet.Cells(1, 2).Style.Font.Size = 14
            worksheet.Cells(1, 2).Style.WrapText = True

            Dim lineCount As Integer = reportTitle.Split(vbLf).Length
            worksheet.Row(1).Height = lineCount * 18 + 4

            worksheet.Cells(lastRow + 1, 4).Value = $"ВСЕГО {rowCount}:"
            worksheet.Cells(lastRow + 1, 5).Value = Math.Round(GetTotalPCh(worksheet), 2)
            worksheet.Cells(lastRow + 1, 4, lastRow + 1, 5).Style.Fill.PatternType = ExcelFillStyle.Solid
            worksheet.Cells(lastRow + 1, 4, lastRow + 1, 5).Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow)

            If hasExtra Then
                Dim totalExtra As Double = 0
                For Each o In otsList
                    If String.IsNullOrEmpty(o.Id) Then Continue For
                    totalExtra += (extraColumn.Item2(o))
                Next
                worksheet.Cells(lastRow + 1, 13).Value = Math.Round(totalExtra, 2)
                worksheet.Cells(lastRow + 1, 13).Style.Numberformat.Format = "+0.00;-0.00;0"
                worksheet.Cells(lastRow + 1, 13).Style.Fill.PatternType = ExcelFillStyle.Solid
                worksheet.Cells(lastRow + 1, 13).Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow)
                worksheet.Cells(lastRow + 1, 13).Style.Font.Bold = True
            End If
        End Sub

        Private Function GetTotalPCh(worksheet As ExcelWorksheet) As Double
            Dim total As Double = 0, row As Integer = 3
            While worksheet.Cells(row, 2).Value IsNot Nothing
                Dim val = worksheet.Cells(row, 5).Value
                If val IsNot Nothing AndAlso IsNumeric(val) Then total += CDbl(val)
                row += 1
            End While
            Return total
        End Function

        Private Sub ApplyFormatting(worksheet As ExcelWorksheet, rowCount As Integer, lastCol As Integer)
            Dim lastRow As Integer = 3 + rowCount - 1

            ' ✅ УСТАНАВЛИВАЕМ ПЕРЕНОС ТЕКСТА ДЛЯ ВСЕХ ЯЧЕЕК С ДАННЫМИ
            Dim allDataRange = worksheet.Cells(2, 2, lastRow + 1, lastCol)
            allDataRange.Style.WrapText = True

            Dim dataRange = worksheet.Cells(2, 2, lastRow + 1, lastCol)
            worksheet.Cells(1, 1, lastRow + 1, lastCol).Style.Font.Name = "Times New Roman"

            With dataRange.Style
                .HorizontalAlignment = ExcelHorizontalAlignment.Center
                .VerticalAlignment = ExcelVerticalAlignment.Top
            End With

            worksheet.Cells(2, 2, 2, lastCol).Style.Font.Bold = True
            worksheet.Cells(1, 2).Style.Font.Bold = True

            If lastRow >= 3 Then
                worksheet.Cells(3, 9, lastRow, 9).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left
                worksheet.Cells(3, 6, lastRow, 6).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left
                worksheet.Cells(3, 5, lastRow + 1, 5).Style.Numberformat.Format = "0.00"
            End If
        End Sub

        Private Sub SetColumnWidths(worksheet As ExcelWorksheet, hasExtra As Boolean)
            '✅ ШИРИНЫ КОЛОНОК ПОДГНАНЫ ПОД ОРИГИНАЛ ИЗ Лист1.xlsx
            worksheet.Column(1).Width = 2
            worksheet.Column(2).Width = 8.13
            worksheet.Column(3).Width = 5.22
            worksheet.Column(4).Width = 7.63
            worksheet.Column(5).Width = 7.78
            worksheet.Column(6).Width = 7.78
            worksheet.Column(7).Width = 10.86
            worksheet.Column(8).Width = 24.11
            worksheet.Column(9).Width = 70
            worksheet.Column(10).Width = 15
            worksheet.Column(11).Width = 11
            worksheet.Column(12).Width = 9


            If hasExtra Then
                worksheet.Column(13).Width = 14
            End If
        End Sub

        Private Sub SetBorders(worksheet As ExcelWorksheet, rowCount As Integer, lastCol As Integer)
            Dim lastRow As Integer = 3 + rowCount - 1
            Using borderRange = worksheet.Cells(2, 2, lastRow + 1, lastCol)
                borderRange.Style.Border.Top.Style = ExcelBorderStyle.Thin
                borderRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin
                borderRange.Style.Border.Left.Style = ExcelBorderStyle.Thin
                borderRange.Style.Border.Right.Style = ExcelBorderStyle.Thin
            End Using
        End Sub

        Private Sub SetPrintSettings(worksheet As ExcelWorksheet)

            With worksheet.PrinterSettings
                ' 1. ЯВНО ЗАДАЁМ РАЗМЕР БУМАГИ И ОРИЕНТАЦИЮ (это критически важно!)
                .PaperSize = ePaperSize.A4
                .Orientation = eOrientation.Landscape

                ' 2. НАСТРОЙКИ МАСШТАБИРОВАНИЯ
                .FitToPage = True
                .FitToWidth = 1       ' Уместить по ширине в 1 страницу А4
                .FitToHeight = 0      ' 0 = высота не ограничена (пусть будет сколько угодно страниц вниз)

                '' 3. ПОВТОР ЗАГОЛОВКОВ НА КАЖДОЙ СТРАНИЦЕ
                '.RepeatRows = New ExcelAddress("$1:$2") ' Повторять строки 1 (заголовок отчёта) и 2 (шапку таблицы)

                ' 4. ПОЛЯ СТРАНИЦЫ (в дюймах, делаем минимальными, чтобы влезло больше данных)
                .LeftMargin = 0.2
                .RightMargin = 0.2
                .TopMargin = 0.2
                .BottomMargin = 0.2
                .HeaderMargin = 0.1
                .FooterMargin = 0.1
            End With
        End Sub

        ' =====================================================================
        ' БЛОК 3. ОДИНОЧНЫЙ ЭКСПОРТ (для совместимости)
        ' =====================================================================
        Public Sub ExportOTSListToExcel(otsList As List(Of Otkaz), Optional reportTitle As String = "Выборка ОТС", Optional fullFilePath As String = "", Optional extraColumn As Tuple(Of String, Func(Of Otkaz, Double)) = Nothing)
            Try
                ExcelPackage.License.SetNonCommercialPersonal("СергейВалерьевич")
                Dim isBatchMode As Boolean = Not String.IsNullOrEmpty(fullFilePath)
                If Not isBatchMode Then
                    Dim safeTitle = reportTitle.Replace(" ", "_").Replace("/", "_").Replace("\", "_").Replace(":", "_")
                    fullFilePath = Path.Combine(My.Settings.ReportFolderPath, $"OTS_{safeTitle}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx")
                End If

                Using package As New ExcelPackage()
                    Dim sheetName = If(reportTitle.Length > 31, reportTitle.Substring(0, 31), reportTitle)
                    FillWorksheet(package.Workbook.Worksheets.Add(sheetName), otsList, reportTitle, extraColumn)
                    Dim fileInfo As New FileInfo(fullFilePath)
                    If Not fileInfo.Directory.Exists Then fileInfo.Directory.Create()
                    package.SaveAs(fileInfo)

                    If Not isBatchMode AndAlso fileInfo.Exists Then
                        Process.Start(New ProcessStartInfo(fullFilePath) With {.UseShellExecute = True})
                        AddLog($"Экспорт выполнен: открыто {otsList.Count} отказов")
                    Else
                        AddLog($"Создан файл: {reportTitle} ({otsList.Count} записей)")
                    End If
                End Using
            Catch ex As Exception
                AddLog($"Ошибка экспорта [{reportTitle}]: {ex.Message}")
            End Try
        End Sub

        ' =====================================================================
        ' БЛОК 4. ГЛАВНАЯ ПРОЦЕДУРА: 1 EXCEL -> 1 PDF (через MiniPdf)
        ' =====================================================================
        Public Sub GenerateAllReports(otsList As List(Of Otkaz))
            Try
                ExcelPackage.License.SetNonCommercialPersonal("СергейВалерьевич")
                Dim periodText As String = $"с {Fetcher.NachDat:dd.MM.yyyy} по {Fetcher.KonDat:dd.MM.yyyy}"
                Dim timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm")
                Dim targetFolder = Path.Combine(My.Settings.ReportFolderPath, $"Выборки_ОТС_{timeStamp}")
                Directory.CreateDirectory(targetFolder)

                AddLog($"Начинаю генерацию единого отчёта за период {periodText}...")
                Dim excelPath = Path.Combine(targetFolder, $"Отчёт_ОТС_{timeStamp}.xlsx")
                Dim pdfPath = Path.Combine(targetFolder, $"Отчёт_ОТС_{timeStamp}.pdf")

                ' 1. Формируем список всех 13 отчетов в строгом порядке
                Dim noExtra = CType(Nothing, Tuple(Of String, Func(Of Otkaz, Double)))
                Dim korrExtra = New Tuple(Of String, Func(Of Otkaz, Double))("Откорректировано часов", Function(o) o.KorPCHonDate)

                Dim reports = New List(Of Tuple(Of String, String, Func(Of Otkaz, Boolean), Tuple(Of String, Func(Of Otkaz, Double)))) From {
        Tuple.Create("ПФБ", "Выборка ОТС ПФБ", IsPFB, noExtra),
        Tuple.Create("Расследование >10 суток", "Выборка ОТС в расследовании свыше 10 суток", UpTo10DaysRassled, noExtra),
        Tuple.Create("Поступили за период", $"ОТС поступившие за период {periodText}", PostupilZaPeriod, noExtra),
        Tuple.Create("Расследованные за период", $"Отчёт о проделанной работе за период {periodText}{vbLf}Выборка ОТС, расследованных за период {periodText}", ZakrytZaPeriod, noExtra),
        Tuple.Create("Технологические нарушения", $"Отчёт о проделанной работе за период {periodText}{vbLf}Выборка ОТС, переведенных в технологические нарушения за период {periodText}", ToTechnoZaPeriod, noExtra),
        Tuple.Create("Переданы на др. дороги", $"Отчёт о проделанной работе за период {periodText}{vbLf}Выборка ОТС, переданных на другие дороги за период {periodText}", PeredanZaPeriod, noExtra),
        Tuple.Create("Откорректированные", $"Отчёт о проделанной работе за период {periodText}{vbLf}Выборка ОТС, откорректированных за период {periodText}", KorrZaPeriod, korrExtra)
    }

                For Each tche In {"ТЧЭ-1", "ТЧЭ-2", "ТЧЭ-3", "ТЧЭ-5", "ТЧЭ-7"}
                    reports.Add(Tuple.Create($"Расследование {tche}", $"ОТС в расследовании на {tche}", GetVRassZaPeriod(tche), noExtra))
                Next
                reports.Add(Tuple.Create("ПАСС.ПРИГ.", "Выборка ОТС ПАСС.ПРИГ.", WithPassZaPeriod, noExtra))

                ' 2. Создаем ОДИН Excel со всеми листами
                Using package As New ExcelPackage()
                    For i = 0 To reports.Count - 1
                        Dim def = reports(i)
                        Dim filtered = otsList.Where(def.Item3).ToList()
                        Dim sheetName = If(def.Item1.Length > 31, def.Item1.Substring(0, 31), def.Item1)
                        FillWorksheet(package.Workbook.Worksheets.Add(sheetName), filtered, def.Item2, def.Item4)
                        AddLog($"  ✓ Лист '{def.Item1}': {filtered.Count} записей")
                    Next

                    package.SaveAs(New FileInfo(excelPath))
                    AddLog("✅ Единый Excel-файл сохранен.")
                End Using

                ' 3. КОНВЕРТАЦИЯ В PDF ЧЕРЕЗ COM EXCEL (идеальный рендеринг, как в Excel)
                AddLog("Конвертирую Excel в PDF через COM Excel...")
                ExportToPdfViaExcel(excelPath, pdfPath)

                ' 4. Открываем результат
                If File.Exists(pdfPath) Then Process.Start(New ProcessStartInfo() With {.FileName = pdfPath, .UseShellExecute = True})
                Process.Start(New ProcessStartInfo() With {.FileName = "explorer.exe", .Arguments = targetFolder, .UseShellExecute = True})
                AddLog($"✅ Готово! Файлы в папке: Выборки_ОТС_{timeStamp}")

            Catch ex As Exception
                AddLog($"❌ Критическая ошибка: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Конвертирует Excel-файл в PDF через COM Excel (идеальный рендеринг)
        ''' </summary>
        Private Sub ExportToPdfViaExcel(excelPath As String, pdfPath As String)
            Dim excelApp As ExcelInterop.Application = Nothing
            Dim wb As ExcelInterop.Workbook = Nothing

            Try
                AddLog("Конвертирую Excel → PDF через COM Excel...")

                ' Создаём скрытый экземпляр Excel
                excelApp = New ExcelInterop.Application() With {
                    .Visible = False,
                    .DisplayAlerts = False
                }

                ' Открываем наш Excel-файл
                wb = excelApp.Workbooks.Open(excelPath)

                ' Экспортируем ВСЕ листы в один PDF
                wb.ExportAsFixedFormat(
                    Type:=ExcelInterop.XlFixedFormatType.xlTypePDF,
                    Filename:=pdfPath,
                    Quality:=ExcelInterop.XlFixedFormatQuality.xlQualityStandard,
                    IncludeDocProperties:=True,
                    IgnorePrintAreas:=False,
                    OpenAfterPublish:=False)

                AddLog("✅ PDF-файл успешно создан через COM Excel!")

            Catch ex As Exception
                AddLog($"⚠ Ошибка создания PDF: {ex.Message}")
            Finally
                ' === ОБЯЗАТЕЛЬНАЯ ОЧИСТКА, чтобы EXCEL.EXE не завис в процессах ===
                If wb IsNot Nothing Then
                    Try : wb.Close(SaveChanges:=False) : Catch : End Try
                End If
                If excelApp IsNot Nothing Then
                    Try : excelApp.Quit() : Catch : End Try
                End If

                ' Освобождаем COM-объекты
                If wb IsNot Nothing Then Runtime.InteropServices.Marshal.ReleaseComObject(wb)
                If excelApp IsNot Nothing Then Runtime.InteropServices.Marshal.ReleaseComObject(excelApp)

                ' Принудительная сборка мусора
                GC.Collect()
                GC.WaitForPendingFinalizers()
            End Try
        End Sub




    End Module
End Namespace









'Imports System.IO
'Imports OfficeOpenXml
'Imports OfficeOpenXml.Style
'Imports System.Diagnostics

'Namespace Kas
'	Module ReportsExcelModule

'        ' =====================================================================
'        ' БЛОК 1. ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ
'        ' =====================================================================

'        ' Безопасное логирование из любого потока
'        Private Sub AddLog(message As String)
'            If Application.Current?.Dispatcher IsNot Nothing Then
'                Application.Current.Dispatcher.Invoke(Sub() MW.InfoBLOK.AddItem(message))
'            End If
'        End Sub

'        ' =====================================================================
'        ' БЛОК 2. ЗАПОЛНЕНИЕ ЛИСТА (с поддержкой доп. колонки)
'        ' =====================================================================

'        ' extraColumn: Tuple(заголовок_колонки, функция_получения_значения_из_Otkaz)
'        ' Если Nothing — работает как раньше (12 колонок)
'        ' Если задан — добавляется 13-я колонка (M), значения берутся через функцию,
'        '              в итоговой строке выводится сумма по модулю
'        Private Function FillWorksheet(worksheet As ExcelWorksheet,
'                               otsList As List(Of Otkaz),
'                               reportTitle As String,
'                               Optional extraColumn As Tuple(Of String, Func(Of Otkaz, Double)) = Nothing) As Integer

'            Dim hasExtra As Boolean = extraColumn IsNot Nothing
'            Dim lastCol As Integer = If(hasExtra, 13, 12)

'            WriteHeaders(worksheet, hasExtra, extraColumn)
'            Dim rowCount = WriteDataRows(worksheet, otsList, hasExtra, extraColumn)
'            WriteFooter(worksheet, rowCount, reportTitle, hasExtra, otsList, extraColumn)
'            ApplyFormatting(worksheet, rowCount, lastCol)
'            SetColumnWidths(worksheet, hasExtra)
'            SetBorders(worksheet, rowCount, lastCol)
'            SetPrintSettings(worksheet)

'            Return rowCount
'        End Function

'        Private Sub WriteHeaders(worksheet As ExcelWorksheet,
'                         hasExtra As Boolean,
'                         extraColumn As Tuple(Of String, Func(Of Otkaz, Double)))
'            worksheet.Cells(2, 2).Value = "№ ОТС"
'            worksheet.Cells(2, 3).Value = "кат"
'            worksheet.Cells(2, 4).Value = "Начало"
'            worksheet.Cells(2, 5).Value = "Поездо-часов"
'            worksheet.Cells(2, 6).Value = "Привязано поездов"
'            worksheet.Cells(2, 7).Value = "Расследует"
'            worksheet.Cells(2, 8).Value = "Место отказа"
'            worksheet.Cells(2, 9).Value = "Описание"
'            worksheet.Cells(2, 10).Value = "Отнесен на"
'            worksheet.Cells(2, 11).Value = "Поступил"
'            worksheet.Cells(2, 12).Value = "От кого"

'            If hasExtra Then
'                worksheet.Cells(2, 13).Value = extraColumn.Item1
'            End If

'            worksheet.Cells(2, 2, 2, If(hasExtra, 13, 12)).AutoFilter = True
'        End Sub

'        Private Function WriteDataRows(worksheet As ExcelWorksheet,
'                               otsList As List(Of Otkaz),
'                               hasExtra As Boolean,
'                               extraColumn As Tuple(Of String, Func(Of Otkaz, Double))) As Integer

'            Dim startRow As Integer = 3
'            Dim rowCount As Integer = 0

'            For Each o In otsList
'                If String.IsNullOrEmpty(o.Id) Then Continue For

'                Dim row As Integer = startRow + rowCount

'                worksheet.Cells(row, 2).Value = o.Id
'                worksheet.Cells(row, 3).Value = o.Kat
'                If o.Nach > Date.MinValue Then worksheet.Cells(row, 4).Value = o.Nach.ToString("dd.MM.yy HH:mm")
'                worksheet.Cells(row, 5).Value = Math.Round(o.PCh, 2)

'                Dim parts As New List(Of String)
'                If o.GruzKol > 0 Then parts.Add($"Груз: {o.GruzKol}{If(o.GruzPCH > 0.1, $" на {o.GruzPCH:F2}ч", "")}")
'                If o.PasKol > 0 Then parts.Add($"Пасс: {o.PasKol}{If(o.PasPCH > 0.1, $" на {o.PasPCH:F2}ч", "")}")
'                If o.PrigKol > 0 Then parts.Add($"Приг: {o.PrigKol}{If(o.PrigPCH > 0.1, $" на {o.PrigPCH:F2}ч", "")}")
'                worksheet.Cells(row, 6).Value = String.Join($"{vbCrLf}{vbCrLf}", parts)

'                worksheet.Cells(row, 7).Value = o.KtoZakryl
'                worksheet.Cells(row, 8).Value = o.MestoOTS_TXT

'                Dim Look As String = IIf(o.SerLokPripLokInRowTXT <> "", $"{o.SerLokPripLokInRowTXT},", "")
'                Dim Maash As String = IIf(o.MashPripInRowTXT <> "", $"машинист {o.MashPripInRowTXT},", "")
'                worksheet.Cells(row, 9).Value = $"{Look} {Maash} {o.Opis} {FormatLokData(o.DaNaLok)}{vbCrLf}{FormatHistoryToExcelText(o.History)}"

'                worksheet.Cells(row, 10).Value = IIf(o.ZaKem <> "!", o.ZaKem, "")

'                If o.Postup > Date.MinValue Then worksheet.Cells(row, 11).Value = o.Postup.ToString("dd.MM.yyyy")

'                If Not o.Istochnik.ToUpper().Contains("ГИД") Then
'                    worksheet.Cells(row, 12).Value = o.Istochnik
'                End If

'                ' === ДОПОЛНИТЕЛЬНАЯ КОЛОНКА ===
'                If hasExtra Then
'                    Dim val As Double = extraColumn.Item2(o)
'                    worksheet.Cells(row, 13).Value = Math.Round(val, 2)
'                    worksheet.Cells(row, 13).Style.Numberformat.Format = "0.00"
'                End If

'                rowCount += 1
'            Next

'            Return rowCount
'        End Function

'        Private Sub WriteFooter(worksheet As ExcelWorksheet,
'                        rowCount As Integer,
'                        reportTitle As String,
'                        hasExtra As Boolean,
'                        otsList As List(Of Otkaz),
'                        extraColumn As Tuple(Of String, Func(Of Otkaz, Double)))

'            Dim lastRow As Integer = 3 + rowCount - 1
'            Dim wi As Integer = If(hasExtra, 13, 12)

'            ' Заголовок отчета
'            worksheet.Cells(1, 2, 1, wi).Merge = True
'            worksheet.Cells(1, 2).Value = reportTitle
'            worksheet.Cells(1, 2).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center
'            worksheet.Cells(1, 2).Style.VerticalAlignment = ExcelVerticalAlignment.Center
'            worksheet.Cells(1, 2).Style.Font.Bold = True
'            worksheet.Cells(1, 2).Style.Font.Size = 14
'            worksheet.Cells(1, 2).Style.WrapText = True

'            ' ✅ АВТОРАСЧЁТ ВЫСОТЫ СТРОКИ ПО КОЛИЧЕСТВУ ПЕРЕНОСОВ
'            ' Считаем количество строк в заголовке (количество vbLf + 1)
'            Dim lineCount As Integer = reportTitle.Split(vbLf).Length
'            ' Высота одной строки при шрифте 14 ≈ 18 пунктов
'            ' Добавляем небольшой запас
'            Dim rowHeight As Double = lineCount * 18 + 4
'            worksheet.Row(1).Height = rowHeight

'            ' Итого по ПЧ (как было)
'            worksheet.Cells(lastRow + 1, 4).Value = $"ВСЕГО {rowCount}:"
'            worksheet.Cells(lastRow + 1, 5).Value = Math.Round(GetTotalPCh(worksheet), 2)
'            worksheet.Cells(lastRow + 1, 4, lastRow + 1, 5).Style.Fill.PatternType = ExcelFillStyle.Solid
'            worksheet.Cells(lastRow + 1, 4, lastRow + 1, 5).Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow)

'            ' === СУМА ПО ДОП. КОЛОНКЕ (ПО МОДУЛЮ) ===
'            If hasExtra Then
'                Dim totalExtra As Double = 0
'                For Each o In otsList
'                    If String.IsNullOrEmpty(o.Id) Then Continue For
'                    totalExtra += Math.Abs(extraColumn.Item2(o))
'                Next

'                worksheet.Cells(lastRow + 1, 13).Value = Math.Round(totalExtra, 2)
'                worksheet.Cells(lastRow + 1, 13).Style.Numberformat.Format = "0.00"
'                worksheet.Cells(lastRow + 1, 13).Style.Fill.PatternType = ExcelFillStyle.Solid
'                worksheet.Cells(lastRow + 1, 13).Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow)
'                worksheet.Cells(lastRow + 1, 13).Style.Font.Bold = True
'            End If
'        End Sub

'        Private Function GetTotalPCh(worksheet As ExcelWorksheet) As Double
'            Dim total As Double = 0
'            Dim row As Integer = 3
'            While worksheet.Cells(row, 2).Value IsNot Nothing
'                Dim val = worksheet.Cells(row, 5).Value
'                If val IsNot Nothing AndAlso IsNumeric(val) Then
'                    total += CDbl(val)
'                End If
'                row += 1
'            End While
'            Return total
'        End Function

'        Private Sub ApplyFormatting(worksheet As ExcelWorksheet, rowCount As Integer, lastCol As Integer)
'            Dim lastRow As Integer = 3 + rowCount - 1

'            Dim dataRange = worksheet.Cells(2, 2, lastRow + 1, lastCol)
'            worksheet.Cells(1, 1, lastRow + 1, lastCol).Style.Font.Name = "Times New Roman"

'            With dataRange.Style
'                .HorizontalAlignment = ExcelHorizontalAlignment.Center
'                .VerticalAlignment = ExcelVerticalAlignment.Center
'                .WrapText = True
'            End With

'            worksheet.Cells(2, 2, 2, lastCol).Style.Font.Bold = True
'            worksheet.Cells(1, 2).Style.Font.Bold = True

'            If lastRow >= 3 Then
'                worksheet.Cells(3, 9, lastRow, 9).Style.HorizontalAlignment = ExcelHorizontalAlignment.Justify
'                worksheet.Cells(3, 6, lastRow, 6).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left
'                worksheet.Cells(3, 5, lastRow + 1, 5).Style.Numberformat.Format = "0.00"
'            End If
'        End Sub

'        Private Sub SetColumnWidths(worksheet As ExcelWorksheet, hasExtra As Boolean)
'            worksheet.Column(1).Width = 2
'            worksheet.Column(2).Width = 8.13
'            worksheet.Column(3).Width = 5.22
'            worksheet.Column(4).Width = 7.63
'            worksheet.Column(5).Width = 7.78
'            worksheet.Column(6).Width = 7.78
'            worksheet.Column(7).Width = 10.86
'            worksheet.Column(8).Width = 24.11
'            worksheet.Column(9).Width = 70
'            worksheet.Column(10).Width = 15
'            worksheet.Column(11).Width = 11
'            worksheet.Column(12).Width = 9

'            If hasExtra Then
'                worksheet.Column(13).Width = 14  ' "Откорректировано часов"
'            End If
'        End Sub

'        Private Sub SetBorders(worksheet As ExcelWorksheet, rowCount As Integer, lastCol As Integer)
'            Dim lastRow As Integer = 3 + rowCount - 1
'            Using borderRange = worksheet.Cells(2, 2, lastRow + 1, lastCol)
'                borderRange.Style.Border.Top.Style = ExcelBorderStyle.Thin
'                borderRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin
'                borderRange.Style.Border.Left.Style = ExcelBorderStyle.Thin
'                borderRange.Style.Border.Right.Style = ExcelBorderStyle.Thin
'            End Using
'        End Sub

'        Private Sub SetPrintSettings(worksheet As ExcelWorksheet)
'            With worksheet.PrinterSettings
'                .Orientation = eOrientation.Landscape
'                .FitToPage = True
'                .FitToWidth = 1
'                .FitToHeight = 350
'                .RepeatRows = New ExcelAddress("$2:$2")
'                .LeftMargin = 0.1
'                .RightMargin = 0.1
'                .TopMargin = 0.1
'                .BottomMargin = 0.1
'                .HeaderMargin = 0.2
'                .FooterMargin = 0.2
'            End With
'        End Sub


'        ' =====================================================================
'        ' БЛОК 3. ОДИНОЧНЫЙ ЭКСПОРТ (с поддержкой доп. колонки)
'        ' =====================================================================

'        Public Sub ExportOTSListToExcel(
'    otsList As List(Of Otkaz),
'    Optional reportTitle As String = "Выборка ОТС",
'    Optional fullFilePath As String = "",
'    Optional extraColumn As Tuple(Of String, Func(Of Otkaz, Double)) = Nothing)

'            Try
'                ExcelPackage.License.SetNonCommercialPersonal("СергейВалерьевич")

'                Dim isBatchMode As Boolean = Not String.IsNullOrEmpty(fullFilePath)

'                If Not isBatchMode Then
'                    Dim timeStamp As String = DateTime.Now.ToString("yyyyMMdd_HHmmss")
'                    Dim safeTitle As String = reportTitle.Replace(" ", "_").
'                                                      Replace("/", "_").
'                                                      Replace("\", "_").
'                                                      Replace(":", "_")
'                    fullFilePath = Path.Combine(My.Settings.ReportFolderPath,
'                                        $"OTS_{safeTitle}_{timeStamp}.xlsx")
'                End If

'                Using package As New ExcelPackage()
'                    Dim sheetName As String = If(reportTitle.Length > 31,
'                                         reportTitle.Substring(0, 31),
'                                         reportTitle)
'                    Dim worksheet = package.Workbook.Worksheets.Add(sheetName)

'                    Dim rowCount = FillWorksheet(worksheet, otsList, reportTitle, extraColumn)

'                    Dim fileInfo As New FileInfo(fullFilePath)
'                    If Not fileInfo.Directory.Exists Then fileInfo.Directory.Create()
'                    package.SaveAs(fileInfo)

'                    If Not isBatchMode Then
'                        If fileInfo.Exists Then
'                            Dim psi As New ProcessStartInfo(fullFilePath) With {.UseShellExecute = True}
'                            Try
'                                Process.Start(psi)
'                                AddLog($"Экспорт выполнен: открыто {rowCount} отказов")
'                            Catch ex As Exception
'                                AddLog($"Файл сохранен, но не открылся: {ex.Message}")
'                            End Try
'                        End If
'                    Else
'                        AddLog($"Создан файл: {reportTitle} ({rowCount} записей)")
'                    End If
'                End Using

'            Catch ex As Exception
'                AddLog($"Критическая ошибка экспорта [{reportTitle}]: {ex.Message}")
'            End Try
'        End Sub


'        ' =====================================================================
'        ' БЛОК 4. МНОГОЛИСТОВОЙ ЭКСПОРТ
'        ' =====================================================================

'        Public Sub ExportMultiSheetExcel(sheets As List(Of Tuple(Of String, String, List(Of Otkaz))),
'                                 fullFilePath As String,
'                                 Optional fileTitle As String = "Отчёт")

'            Try
'                ExcelPackage.License.SetNonCommercialPersonal("СергейВалерьевич")

'                Using package As New ExcelPackage()
'                    Dim totalRows As Integer = 0

'                    For Each sh In sheets
'                        Dim sheetName As String = If(sh.Item1.Length > 31,
'                                             sh.Item1.Substring(0, 31),
'                                             sh.Item1)
'                        Dim worksheet = package.Workbook.Worksheets.Add(sheetName)

'                        Dim rowCount = FillWorksheet(worksheet, sh.Item3, sh.Item2)
'                        totalRows += rowCount
'                    Next

'                    Dim fileInfo As New FileInfo(fullFilePath)
'                    If Not fileInfo.Directory.Exists Then fileInfo.Directory.Create()
'                    package.SaveAs(fileInfo)

'                    AddLog($"Создан многолистовой файл: {fileTitle} ({sheets.Count} листов, всего {totalRows} записей)")
'                End Using

'            Catch ex As Exception
'                AddLog($"Ошибка создания многолистового файла [{fileTitle}]: {ex.Message}")
'            End Try
'        End Sub


'        ' =====================================================================
'        ' БЛОК 5. ПАКЕТНАЯ ГЕНЕРАЦИЯ ВСЕХ ОТЧЁТОВ
'        ' =====================================================================
















'        Public Sub GenerateAllReports(otsList As List(Of Otkaz))
'            Try
'                ExcelPackage.License.SetNonCommercialPersonal("СергейВалерьевич")

'                Dim dFrom As String = Fetcher.NachDat.ToString("dd.MM.yyyy")
'                Dim dTo As String = Fetcher.KonDat.ToString("dd.MM.yyyy")
'                Dim periodText As String = $"с {dFrom} по {dTo}"

'                Dim timeStamp As String = DateTime.Now.ToString("yyyy-MM-dd_HH-mm")
'                Dim batchFolderName As String = $"Выборки_ОТС_{timeStamp}"
'                Dim baseFolder As String = My.Settings.ReportFolderPath
'                Dim targetFolder As String = Path.Combine(baseFolder, batchFolderName)
'                Directory.CreateDirectory(targetFolder)

'                AddLog($"Начинаю генерацию отчётов за период {periodText}...")

'                GenerateSingleReports(otsList, periodText, targetFolder)
'                GenerateTcheReports(otsList, targetFolder)

'                Try
'                    Dim psi As New ProcessStartInfo() With {
'                .FileName = "explorer.exe",
'                .Arguments = targetFolder,
'                .UseShellExecute = True
'            }
'                    Process.Start(psi)
'                    AddLog($"✅ Готово! Все файлы в папке: {batchFolderName}")
'                Catch ex As Exception
'                    AddLog($"Файлы созданы, но не удалось открыть папку: {ex.Message}")
'                End Try

'            Catch ex As Exception
'                AddLog($"Критическая ошибка генерации отчётов: {ex.Message}")
'            End Try
'        End Sub

'        Private Sub GenerateSingleReports(otsList As List(Of Otkaz),
'                                  periodText As String,
'                                  targetFolder As String)

'            ' Доп. колонка для отчёта "Откорректированные"
'            Dim korrExtraColumn As New Tuple(Of String, Func(Of Otkaz, Double))(
'        "Откорректировано часов",
'        Function(o) o.KorPCHonDate)

'            ' Список отчётов: (имя_файла, заголовок, фильтр, доп.колонка)
'            ' ⚠️ Используем ОРИГИНАЛЬНЫЕ имена фильтров из твоего модуля
'            Dim singleReports As New List(Of Tuple(Of String, String, Func(Of Otkaz, Boolean), Tuple(Of String, Func(Of Otkaz, Double))))

'            singleReports.Add(Tuple.Create("ПФБ",
'                                   "Выборка ОТС ПФБ",
'                                   IsPFB,
'                                   CType(Nothing, Tuple(Of String, Func(Of Otkaz, Double)))))

'            singleReports.Add(Tuple.Create("Расследование_свыше_10_суток",
'                                   "Выборка ОТС в расследовании свыше 10 суток",
'                                   UpTo10DaysRassled,
'                                   CType(Nothing, Tuple(Of String, Func(Of Otkaz, Double)))))

'            singleReports.Add(Tuple.Create("Поступили_за_период",
'                                   $"ОТС поступившие за период {periodText}",
'                                   PostupilZaPeriod,
'                                   CType(Nothing, Tuple(Of String, Func(Of Otkaz, Double)))))

'            singleReports.Add(Tuple.Create("Расследованные_за_период",
'                                   $"Отчёт о проделанной работе за период {periodText}{vbLf}Выборка ОТС, расследованных за период {periodText}",
'                                   ZakrytZaPeriod,
'                                   CType(Nothing, Tuple(Of String, Func(Of Otkaz, Double)))))

'            singleReports.Add(Tuple.Create("Технологические_нарушения",
'                                   $"Отчёт о проделанной работе за период {periodText}{vbLf}Выборка ОТС, переведенных в технологические нарушения за период {periodText}",
'                                   ToTechnoZaPeriod,
'                                   CType(Nothing, Tuple(Of String, Func(Of Otkaz, Double)))))

'            singleReports.Add(Tuple.Create("Переданы_на_другие_дороги",
'                                   $"Отчёт о проделанной работе за период {periodText}{vbLf}Выборка ОТС, переданных на другие дороги за период {periodText}",
'                                   PeredanZaPeriod,
'                                   CType(Nothing, Tuple(Of String, Func(Of Otkaz, Double)))))

'            ' ⚠️ ЭТОТ ОТЧЁТ С ДОП. КОЛОНКОЙ
'            singleReports.Add(Tuple.Create("Откорректированные",
'                                   $"Отчёт о проделанной работе за период {periodText}{vbLf}Выборка ОТС, откорректированных за период {periodText}",
'                                   KorrZaPeriod,
'                                   korrExtraColumn))

'            singleReports.Add(Tuple.Create("С_пассажирами",
'                                   "Выборка ОТС ПАСС.ПРИГ.",
'                                   WithPass,
'                                   CType(Nothing, Tuple(Of String, Func(Of Otkaz, Double)))))

'            ' Генерация
'            For Each rpt In singleReports
'                Dim filteredList = otsList.Where(rpt.Item3).ToList()
'                Dim safeFileName As String = rpt.Item1.Replace(" ", "_") & ".xlsx"
'                Dim fullPath As String = Path.Combine(targetFolder, safeFileName)

'                ' Передаём доп. колонку (для большинства отчётов это Nothing)
'                ExportOTSListToExcel(filteredList, rpt.Item2, fullPath, rpt.Item4)
'            Next
'        End Sub

'        Private Sub GenerateTcheReports(otsList As List(Of Otkaz), targetFolder As String)
'            Dim tcheList = {"ТЧЭ-1", "ТЧЭ-2", "ТЧЭ-3", "ТЧЭ-5", "ТЧЭ-7"}
'            Dim tcheSheets As New List(Of Tuple(Of String, String, List(Of Otkaz)))

'            For Each tche In tcheList
'                ' ⚠️ Используем ОРИГИНАЛЬНЫЙ GetVRassZaPeriod из твоего модуля
'                Dim filter = GetVRassZaPeriod(tche)
'                Dim filteredList = otsList.Where(filter).ToList()
'                tcheSheets.Add(Tuple.Create($"ОТС в расследовании на {tche}",
'                                    $"ОТС в расследовании на {tche}",
'                                    filteredList))
'            Next

'            Dim tcheFilePath As String = Path.Combine(targetFolder, "ОТС_в_расследовании_по_депо.xlsx")
'            ExportMultiSheetExcel(tcheSheets, tcheFilePath, "ОТС в расследовании по депо")
'        End Sub



'    End Module

'End Namespace
