

Imports System.IO
Imports System.Text
Imports System.Windows.Controls.Primitives
Imports System.Windows.Forms
Imports System.Windows.Forms.AxHost

Namespace Kas
    Partial Public Class ExperimentalButtonsCTL


        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().

        End Sub


        Private Sub BtnKAS_ANT_Click(sender As Object, e As RoutedEventArgs)

            ' Если окно не создано ИЛИ оно было закрыто ( IsLoaded = False )
            If KasAntWND Is Nothing OrElse Not KasAntWND.IsLoaded Then
                KasAntWND = New KasAntWin() With {.Owner = MW}
                KasAntWND.Show()
            Else
                ' Если окно уже открыто, просто выводим его на передний план
                KasAntWND.Activate()
                If KasAntWND.WindowState = WindowState.Minimized Then
                    KasAntWND.WindowState = WindowState.Normal
                End If
            End If

        End Sub



        '============= чтение 4 отчета сразу в список с Notes =========================
        Private Async Sub BtnAddJRNLFile_Click(sender As Object, e As RoutedEventArgs)
            ' 1. Открываем диалог выбора файла
            Dim dlg As New Microsoft.Win32.OpenFileDialog() With {
                .Filter = "HTML файлы КАСАНТ|*.html;*.htm|Все файлы|*.*",
                .Title = "Выберите сохранённый журнал 'Список отказов'",
                .InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            }

            If dlg.ShowDialog() <> True Then Exit Sub
            BtnAddJRNLFile.IsEnabled = False

            Try
                ' 2. Чтение и парсинг в фоновом потоке (UI не блокируется)
                Dim records As List(Of JournalRecord) =
                    Await System.Threading.Tasks.Task.Run(Function()
                                                              Dim html As String = System.IO.File.ReadAllText(dlg.FileName, Encoding.GetEncoding("windows-1251"))
                                                              Return Fetcher.ParseJournalList(html)
                                                          End Function)

                ProcessWebJournalData(records)
                AddOTSToContainer(OTSList)
                ' 4. Обновляем UI — твой OTSControl уже умеет отображать
                With MW.TRowsContainer
                    .ClearAll0LevelFilters()
                    .THed.ClearAllFilters()
                    .UpdateTotal()
                End With


            Catch ex As Exception
                ShowMSG(MW, $"Ошибка при обработке файла:{vbCrLf}{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error)
            Finally
                BtnAddJRNLFile.IsEnabled = True
            End Try
        End Sub


        Private Sub BtnAddFromKASANT_Click(sender As Object, e As RoutedEventArgs)
            Dim dlg = New Microsoft.Win32.OpenFileDialog With {
       .Title = "Выберите отчёт КАС АНТ",
       .Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|Excel 2007+ (*.xlsx)|*.xlsx|Excel 97-2003 (*.xls)|*.xls|All Files (*.*)|*.*"
   }

            If dlg.ShowDialog() <> True Then Return

            Try
                Dim filePathToProcess As String = dlg.FileName
                Dim ext = Path.GetExtension(dlg.FileName).ToLowerInvariant()
                If ext = ".xls" Then


                    Try
                        MW.InfoBLOK.AddItem("Конвертирую .xls → .xlsx...")
                        filePathToProcess = ConvXlsToXlsx(dlg.FileName)
                    Catch ex As Exception
                        MsgBox("Не удалось автоматически конвертировать файл." & vbCrLf &
                               "Пожалуйста, откройте его в Excel и сохраните как «Книга Excel (*.xlsx)»." & vbCrLf & vbCrLf &
                               "Ошибка: " & ex.Message,
                               vbExclamation, "Конвертация не удалась")
                        Return
                    End Try
                End If
                ClearAllNoTesOTS(OTSList)


                Load4ReportOld(filePathToProcess, OTSList)

                ' 4. Обновляем UI — твой OTSControl уже умеет отображать
                '========================================
                'TRowsContainer.ClearAll0LevelFilters()
                'TRowsContainer.THed.ClearAllFilters()
                'AddOTSToContainer(OTSList)

                With MW.TRowsContainer
                    .ClearAll0LevelFilters()
                    .THed.ClearAllFilters()
                    .UpdateTotal()
                End With
                '========================================
            Catch ex As Exception
                Dim err = $"❌ Ошибка импорта из КАС АНТ:{vbCrLf}{ex.Message}"
                MW.InfoBLOK.AddItem(err)
                MsgBox(err, vbCritical, "Ошибка")
            End Try
        End Sub


        Private Async Sub BtnGenOtch_Click(sender As Object, e As RoutedEventArgs)
            Try
                Log("📊 Начало генерации отчета...")
                Dim Rows As List(Of GenReportRow) = Await GetGenOtchRows(True)
                SyncGenOTSReport(Rows)
                Log("✅ ГенОтчет успешно сформирован.")

                AddOTSToContainer(OTSList)
                ' ШАГ 2: ОБНОВЛЕНИЕ UI
                With MW.TRowsContainer
                    .ClearAll0LevelFilters()
                    .THed.ClearAllFilters()
                    .UpdateTotal()
                End With
            Catch ex As Exception
                ' Если внутри GenOtchCalc или DownloadAndParseGenReportAsync будет ошибка,
                ' мы попадем сюда, а не уроним программу.
                Log($"❌ Ошибка при генерации отчета: {ex.Message}")
            End Try

        End Sub

        Private Sub BtnNewReport_Click(sender As Object, e As RoutedEventArgs)
            ' Вариант 2: Через диалог выбора файла
            ShowGenReportFromFileDialog()
        End Sub

        Private Async Sub BtnNewNetReport_Click(sender As Object, e As RoutedEventArgs)
            ' 1. Сохраняем исходный текст кнопки и меняем его
            Dim originalText As String = BtnNewNetReport.Content.ToString()
            BtnNewNetReport.Content = "⏳ Загрузка..."
            BtnNewNetReport.IsEnabled = False

            Try
                ' ШАГ 1: ЗАГРУЗКА И ОТОБРАЖЕНИЕ ОТЧЁТА
                Await ShowGenReportWindowAsync()

            Catch ex As Exception
                ShowMSG(MW, $"Ошибка загрузки отчёта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error)
            Finally
                ' 2. ГАРАНТИРОВАННО возвращаем исходное состояние
                BtnNewNetReport.Content = originalText
                BtnNewNetReport.IsEnabled = True
            End Try
        End Sub




        Private Sub BtnHtmlReport_Click(sender As Object, e As RoutedEventArgs)
            TestParseTokensFromFile()
        End Sub

        Private Function ConvXlsToXlsx(xlsPath As String) As String
            ' Создаём временный путь для .xlsx
            Dim tempXlsxPath = Path.ChangeExtension(Path.GetTempFileName(), ".xlsx")

            Dim excelApp As Microsoft.Office.Interop.Excel.Application = Nothing
            Dim workbook As Microsoft.Office.Interop.Excel.Workbook = Nothing

            Try
                ' Запускаем Excel в фоне
                excelApp = New Microsoft.Office.Interop.Excel.Application()
                excelApp.Visible = False
                excelApp.DisplayAlerts = False

                ' Открываем .xls
                workbook = excelApp.Workbooks.Open(xlsPath)

                ' Сохраняем как .xlsx
                workbook.SaveAs(tempXlsxPath, Microsoft.Office.Interop.Excel.XlFileFormat.xlOpenXMLWorkbook)

                ' Закрываем
                workbook.Close(SaveChanges:=False)
                Return tempXlsxPath

            Catch ex As Exception
                Throw New Exception($"Ошибка конвертации: {ex.Message}", ex)
            Finally
                ' Освобождаем COM-объекты
                If workbook IsNot Nothing Then
                    Runtime.InteropServices.Marshal.ReleaseComObject(workbook)
                End If
                If excelApp IsNot Nothing Then
                    excelApp.Quit()
                    Runtime.InteropServices.Marshal.ReleaseComObject(excelApp)
                End If
            End Try
        End Function

        Private gameIsOpen As Boolean = False



        Private Sub BtnZmeyka_Click(sender As Object, e As RoutedEventArgs)
            '        🔹 Дочерний проект (который вставляем)
            '- Свойства → Тип вывода (Output type) = «Библиотека классов» (для WPF — «Библиотека классов WPF»). Объект запуска при этом сам станет «(Нет)».
            '- Удалить Application.xaml(и его .vb) — это точка входа приложения с StartupUri, в библиотеке ей места нет.
            '- Удалить App.config и иконку (favicon.ico) — в DLL они не нужны (не критично, но мусор).
            '- Класс окна, которое вызываем снаружи — Public
            '- Никаких Application.Current.Shutdown() внутри библиотеки — только Me.Close(). Иначе модуль будет убивать всё приложение-хозяин.

            '       🔹 Родительский проект (куда вставляем)
            '- ПКМ по Зависимости (Ссылки) → Добавить ссылку на проект... → галка на дочернем проекте → ОК.
            '- Вызывать окно полным именем (без Imports, чтобы не было конфликтов имён)
            '               Dim game As New Zmeyka.MainWindow()
            '               game.ShowDialog()   ' или .Show()
            '- Сборка → Перестроить решение (Rebuild Solution).
            '- Целевые платформы совместимые: оба проекта.NET Framework 4.8 (или библиотека — .NET Standard). В 4.8 нельзя воткнуть библиотеку на .NET 8.




            ' Защита: не открываем второе окно, пока открыто первое
            If gameIsOpen Then Return
            gameIsOpen = True
            BtnZmeyka.IsEnabled = False

            Dim game As New Zmeyka.MainWindow()
            AddHandler game.Closed, AddressOf GameWindow_Closed
            game.Show()
        End Sub

        ' Срабатывает, когда окно игры закрылось (крестиком или кнопкой "Выход")
        Private Sub GameWindow_Closed(sender As Object, e As EventArgs)
            gameIsOpen = False
            BtnZmeyka.IsEnabled = True
        End Sub

        Private _smokePop As Popup = Nothing
        Private _smokeKanv As SmokeCanvas = Nothing

        Private Sub BtnSmoke_Click(sender As Object, e As RoutedEventArgs)




            ' повторный клик — снимаем задымление
            If _smokePop IsNot Nothing Then
                _smokeKanv.StopSmoke()
                _smokePop.IsOpen = False
                _smokePop = Nothing : _smokeKanv = Nothing
                Return
            End If

            Dim btn As FrameworkElement = CType(MW.expPoyasnilka, FrameworkElement)
            Dim winPos As System.Windows.Point = MW.PointToScreen(New System.Windows.Point(0, 0))
            Dim expPos As System.Windows.Point = btn.PointToScreen(New System.Windows.Point(0, 0))
            Dim splPos As System.Windows.Point = MW.MainWinSplitter.PointToScreen(New System.Windows.Point(0, 0))
            Dim connPos As System.Windows.Point = MW.ConnIndicator.PointToScreen(New System.Windows.Point(0, 0))

            Dim expLeft As Double = expPos.X - winPos.X
            Dim expTop As Double = expPos.Y - winPos.Y
            Dim splLeft As Double = splPos.X - winPos.X
            Dim connTop As Double = connPos.Y - winPos.Y

            Dim topY As Double = connTop - 20
            Dim bottomY As Double = expTop + 12
            Dim popWidth As Double = Math.Max(100, splLeft - expLeft)
            Dim popHeight As Double = Math.Max(100, bottomY - topY)

            Dim wa = SystemParameters.WorkArea
            If popWidth > wa.Width - 20 Then popWidth = wa.Width - 20
            If popHeight > wa.Height - 20 Then popHeight = wa.Height - 20

            'Dim Kanv As New SmokeCanvas With {.Width = 180, .Height = 200}
            Dim Kanv As New SmokeCanvas With {.Width = popWidth, .Height = popHeight}
            SmokeParamsStore.LoadParams(Kanv)
            Kanv.Background = Brushes.Transparent
            Kanv.IsHitTestVisible = False                 ' ← 2) WPF-уровень: канвас не перехватывает

            Dim Pop As New Popup With {
                .AllowsTransparency = True,
                .StaysOpen = True,                        ' ← 1) НЕТ захвата мыши!
                .PlacementTarget = MW,
                .Placement = Primitives.PlacementMode.Relative,
                .HorizontalOffset = expLeft,
                .VerticalOffset = topY,
                .Child = Kanv}

            _smokePop = Pop : _smokeKanv = Kanv
            Pop.IsOpen = True
            Kanv.StartSmoke()

            ' ← 3) патчим HWND ТОЛЬКО после реального открытия
            Dispatcher.BeginInvoke(Sub() MakePopupClickThrough(Pop),
                                   Threading.DispatcherPriority.Loaded)
        End Sub

        Private Sub BtnSmokeSettings_Click(sender As Object, e As RoutedEventArgs)

            '===========================================
            ' Указываем текущее окно как владельца (чтобы окно с дымом не пряталось за главное)
            Dim win As Window = New SmokeControlWindow
            win.Owner = MW

            ' Показываем окно
            win.Show()
        End Sub
    End Class
End Namespace



