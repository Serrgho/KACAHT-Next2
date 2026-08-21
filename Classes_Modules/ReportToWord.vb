Imports System.Reflection.Metadata
Imports KACAHT_Next2.Kas
Imports Microsoft.Office.Interop
'Imports Microsoft.Office.Interop.Excel
Imports Microsoft.Office.Interop.Word
Imports OfficeOpenXml.Drawing
Imports Windows.UI.Text
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.IO
Module ReportToWord

    Private WApp As New Word.Application()
    Private WDoc As Microsoft.Office.Interop.Word.Document

    ' ===== КОНТЕКСТ ТЕКУЩЕГО ЭКСПОРТА =====
    Public ExpNach As Date
    Public ExpKon As Date
    Public ExpNachOld As Date
    Public ExpKonOld As Date

    Public Sub SetExportPeriods(nach As Date, kon As Date, nachOld As Date, konOld As Date)
        ExpNach = nach
        ExpKon = kon
        ExpNachOld = nachOld
        ExpKonOld = konOld
    End Sub









    ''' <summary>Готовый заголовок с периодами</summary>
    Public Function PeriodTitle(text As String) As String
        Return $"{text}{Chr(11)}за период с {ExpNach:d MMMM yyyy} г. по {ExpKon:d MMMM yyyy} г.{Chr(11)}в сравнении с периодом от {ExpNachOld:d MMMM yyyy} г. по {ExpKonOld:d MMMM yyyy} г."
    End Function

    ''' <summary>Короткое «с 1 июля по 15 августа»</summary>
    Public Function PeriodShort() As String
        Return $"с {ExpNach:d MMMM} по {ExpKon:d MMMM}"
    End Function


    ''' <summary>
    ''' Основной вход: экспорт полного отчёта в Word (пояснительная, по комплексам, по локомотивам, оборудованию и т.д.)
    ''' Вызывается по кнопке. Использует OTSContainer.ItemsSource как источник.
    ''' </summary>
    Public Sub ExportFullReport(NY As List(Of Otkaz), NY1 As List(Of Otkaz),
                            nachPeriod As Date, konPeriod As Date,
                            nachOldPeriod As Date, konOldPeriod As Date)
        Try
            SetExportPeriods(nachPeriod, konPeriod, nachOldPeriod, konOldPeriod)
            SetWord()

            ' === По комплексам (по категориям, по сериям, по оборудованию) ===
            PoYasnilka_2(NY, NY1)

            AddTextBlock($"{Chr(12)}")

            ' === Экспорт по оборудованию по комплексам ===
            AddTextBlock($"Распределение отказов технических средств по оборудованию{Chr(11)}за период с {nachPeriod:d MMMM yyyy} г. по {konPeriod:d MMMM yyyy} г.{Chr(11)}в сравнении с периодом от {nachOldPeriod:d MMMM yyyy} г. по {konOldPeriod:d MMMM yyyy} г.", isBold:=True, isCenter:=True)

            OborudPoKomplexText(NY, NY1)

            ' === Экспорт по локомотивам по комплексам ===
            AddTextBlock($"Распределение отказов технических средств по сериям локомотивов{Chr(11)}за период с {nachPeriod:d MMMM yyyy} г. по {konPeriod:d MMMM yyyy} г.{Chr(11)}в сравнении с периодом от {nachOldPeriod:d MMMM yyyy} г. по {konOldPeriod:d MMMM yyyy} г.",
                     isBold:=True, isCenter:=True)
            LokPoKomplexText(NY, NY1)

            AddTextBlock($"{Chr(12)}")

            OborudPoSerLokText(NY, NY1)

            WApp.Visible = True
            WApp.Activate()

        Catch ex As Exception
            MessageBox.Show($"Ошибка при экспорте: {ex.Message}" & vbCrLf & ex.StackTrace,
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error)
            If WApp IsNot Nothing Then WApp.Quit(False)
        End Try
    End Sub



    ' ===================== ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ =====================

    Private Sub SetWord()
        ' Если уже есть Word - закрываем его
        If WApp IsNot Nothing Then
            Try
                WApp.Quit(False) ' без сохранения
            Catch
                ' Игнорируем - возможно, Word уже умер
            End Try
            WApp = Nothing
        End If

        ' Создаём новый экземпляр
        WApp = New Microsoft.Office.Interop.Word.Application()

        ' Устанавливаем документ
        WDoc = WApp.Documents.Add()

        ' Настройки страницы
        With WDoc.PageSetup
            .LeftMargin = WApp.CentimetersToPoints(0.5)
            .RightMargin = WApp.CentimetersToPoints(0.5)
            .TopMargin = WApp.CentimetersToPoints(0.5)
            .BottomMargin = WApp.CentimetersToPoints(0.5)
        End With

        ' Стиль по умолчанию
        With WDoc.Styles(WdBuiltinStyle.wdStyleNormal).ParagraphFormat
            .FirstLineIndent = 0
        End With
    End Sub

    Private Sub AddTextBlock(
        ByVal text As String,
        Optional ByVal fontName As String = "Times New Roman",
        Optional ByVal fontSize As Single = 12,
        Optional ByVal isBold As Boolean = False,
        Optional ByVal isItalic As Boolean = False,
        Optional ByVal addNewLine As Boolean = True,
        Optional ByVal isCenter As Boolean = False)

        If WDoc Is Nothing Then Return

        Dim range = WDoc.Content
        range.Collapse(WdCollapseDirection.wdCollapseEnd)

        range.Text = If(addNewLine, text & vbCrLf, text)

        With range.Font
            .Name = fontName
            .Size = fontSize
            .Bold = If(isBold, 1, 0)
            .Italic = If(isItalic, 1, 0)
        End With

        With range.Paragraphs(1).Format
            .SpaceAfter = 10
            .Alignment = If(isCenter,
                            WdParagraphAlignment.wdAlignParagraphCenter,
                            WdParagraphAlignment.wdAlignParagraphJustify)
        End With
    End Sub

    Private Sub AddEmptyLineAfterRange()
        Dim range As Word.Range = WDoc.Content.Duplicate
        range.Collapse(WdCollapseDirection.wdCollapseEnd)
        range.Text = vbCrLf
        range.Paragraphs(1).Borders.Enable = False
    End Sub

    Private Sub RemoveExtraEmptyLinesAtEnd()
        While WDoc.Paragraphs.Count > 1 AndAlso String.IsNullOrWhiteSpace(WDoc.Paragraphs.Last.Range.Text)
            WDoc.Paragraphs.Last.Range.Delete()
        End While
    End Sub

    Private Sub ApplyBoxBorderToRange(ByVal startRange As Word.Range, ByVal endRange As Word.Range)
        endRange.MoveEnd(WdUnits.wdParagraph, 1)
        Dim fullRange = startRange.Parent.Range(startRange.Start, endRange.End)
        With fullRange.Borders
            .OutsideLineStyle = WdLineStyle.wdLineStyleSingle
            .OutsideLineWidth = WdLineWidth.wdLineWidth025pt
            .DistanceFromTop = 6
            .DistanceFromBottom = 6
            .DistanceFromLeft = 6
            .DistanceFromRight = 6
        End With
    End Sub

    ' ===================== ФОРМАТЫ =====================

    Private Function EndOfSluch_TXT(count As Integer) As String
        Static endings = {"случай", "случая", "случаев"}
        Dim n = count Mod 100
        Dim k = If(n >= 11 AndAlso n <= 14, 2,
                   If(n Mod 10 = 1, 0,
                      If(n Mod 10 >= 2 AndAlso n Mod 10 <= 4, 1, 2)))
        Return endings(k)
    End Function

    Private Function Rost(delta As Integer) As String
        Return If(delta > 0, $"рост количества ОТС на {delta} {EndOfSluch_TXT(delta)}",
                  If(delta < 0, $"снижение количества ОТС на {Math.Abs(delta)} {EndOfSluch_TXT(Math.Abs(delta))}", "количество ОТС на уровне прошлого года."))
    End Function

    Private Function Rost(delta As Single) As String
        Dim h = Math.Abs(Math.Round(delta, 2))
        Return If(delta > 0, $"рост потерь на {h}ч.",
                  If(delta < 0, $"снижение потерь на {h}ч.",
                     "потери поездо-часов на уровне прошлого года."))
    End Function

    ' ===================== АГРЕГАЦИЯ =====================

    Private Function Kat123TXT(otcs As IEnumerable(Of Otkaz)) As String
        Return $"{otcs.Count()} ОТС на {Math.Round(otcs.Sum(Function(o) o.PCh), 2)}ч."
    End Function

    Private Function Kat12TXT(otcs As IEnumerable(Of Otkaz)) As String
        Return Kat123TXT(otcs.Where(Function(o) o.Kat < 3))
    End Function

    Private Function Kat1TXT(otcs As IEnumerable(Of Otkaz)) As String
        Return Kat123TXT(otcs.Where(Function(o) o.Kat = 1))
    End Function

    Private Function Kat2TXT(otcs As IEnumerable(Of Otkaz)) As String
        Return Kat123TXT(otcs.Where(Function(o) o.Kat = 2))
    End Function

    Private Function Kat3TXT(otcs As IEnumerable(Of Otkaz)) As String
        Return Kat123TXT(otcs.Where(Function(o) o.Kat = 3))
    End Function

    Private Function Kat12_OTS_Kompl(t1 As IEnumerable(Of Otkaz), tr1 As IEnumerable(Of Otkaz),
                                     s1 As IEnumerable(Of Otkaz), z1 As IEnumerable(Of Otkaz)) As Integer
        Return t1.Count(Function(o) o.Kat < 3) + tr1.Count(Function(o) o.Kat < 3) +
               s1.Count(Function(o) o.Kat < 3) + z1.Count(Function(o) o.Kat < 3)
    End Function

    Private Function Kat1_OTS_Kompl(t1 As IEnumerable(Of Otkaz), tr1 As IEnumerable(Of Otkaz),
                                    s1 As IEnumerable(Of Otkaz), z1 As IEnumerable(Of Otkaz)) As Integer
        Return t1.Count(Function(o) o.Kat = 1) + tr1.Count(Function(o) o.Kat = 1) +
               s1.Count(Function(o) o.Kat = 1) + z1.Count(Function(o) o.Kat = 1)
    End Function

    Private Function Kat2_OTS_Kompl(t1 As IEnumerable(Of Otkaz), tr1 As IEnumerable(Of Otkaz),
                                    s1 As IEnumerable(Of Otkaz), z1 As IEnumerable(Of Otkaz)) As Integer
        Return t1.Count(Function(o) o.Kat = 2) + tr1.Count(Function(o) o.Kat = 2) +
               s1.Count(Function(o) o.Kat = 2) + z1.Count(Function(o) o.Kat = 2)
    End Function

    Private Function Kat3_OTS_Kompl(t1 As IEnumerable(Of Otkaz), tr1 As IEnumerable(Of Otkaz),
                                    s1 As IEnumerable(Of Otkaz), z1 As IEnumerable(Of Otkaz)) As Integer
        Return t1.Count(Function(o) o.Kat = 3) + tr1.Count(Function(o) o.Kat = 3) +
               s1.Count(Function(o) o.Kat = 3) + z1.Count(Function(o) o.Kat = 3)
    End Function

    Private Function Kat12_PCH_Kompl(t1 As IEnumerable(Of Otkaz), s1 As IEnumerable(Of Otkaz),
                                     tr1 As IEnumerable(Of Otkaz), z1 As IEnumerable(Of Otkaz)) As Single
        Return Math.Round(t1.Where(Function(o) o.Kat < 3).Sum(Function(o) o.PCh) +
                         s1.Where(Function(o) o.Kat < 3).Sum(Function(o) o.PCh) +
                         tr1.Where(Function(o) o.Kat < 3).Sum(Function(o) o.PCh) +
                         z1.Where(Function(o) o.Kat < 3).Sum(Function(o) o.PCh), 2)
    End Function

    Private Function Kat1_PCH_Kompl(t1 As IEnumerable(Of Otkaz), s1 As IEnumerable(Of Otkaz),
                                    tr1 As IEnumerable(Of Otkaz), z1 As IEnumerable(Of Otkaz)) As Single
        Return Math.Round(t1.Where(Function(o) o.Kat = 1).Sum(Function(o) o.PCh) +
                         s1.Where(Function(o) o.Kat = 1).Sum(Function(o) o.PCh) +
                         tr1.Where(Function(o) o.Kat = 1).Sum(Function(o) o.PCh) +
                         z1.Where(Function(o) o.Kat = 1).Sum(Function(o) o.PCh), 2)
    End Function

    Private Function Kat2_PCH_Kompl(t1 As IEnumerable(Of Otkaz), s1 As IEnumerable(Of Otkaz),
                                    tr1 As IEnumerable(Of Otkaz), z1 As IEnumerable(Of Otkaz)) As Single
        Return Math.Round(t1.Where(Function(o) o.Kat = 2).Sum(Function(o) o.PCh) +
                         s1.Where(Function(o) o.Kat = 2).Sum(Function(o) o.PCh) +
                         tr1.Where(Function(o) o.Kat = 2).Sum(Function(o) o.PCh) +
                         z1.Where(Function(o) o.Kat = 2).Sum(Function(o) o.PCh), 2)
    End Function

    Private Function Kat3_PCH_Kompl(t1 As IEnumerable(Of Otkaz), s1 As IEnumerable(Of Otkaz),
                                    tr1 As IEnumerable(Of Otkaz), z1 As IEnumerable(Of Otkaz)) As Single
        Return Math.Round(t1.Where(Function(o) o.Kat = 3).Sum(Function(o) o.PCh) +
                         s1.Where(Function(o) o.Kat = 3).Sum(Function(o) o.PCh) +
                         tr1.Where(Function(o) o.Kat = 3).Sum(Function(o) o.PCh) +
                         z1.Where(Function(o) o.Kat = 3).Sum(Function(o) o.PCh), 2)
    End Function

    ' ===================== ОСНОВНЫЕ ПОДПРОГРАММЫ =====================




    Private Sub OTSPoKat(y1 As String, NY As IEnumerable(Of Otkaz), NY1 As IEnumerable(Of Otkaz))
        AddTextBlock("")
        AddTextBlock("По категориям:")
        AddTextBlock($"1 категория - {NY.Count(Function(o) o.Kat = 1)} ОТС против " &
                     $"{NY1.Count(Function(o) o.Kat = 1)} {y1}")
        AddTextBlock($"2 категория - {NY.Count(Function(o) o.Kat = 2)} ОТС против " &
                     $"{NY1.Count(Function(o) o.Kat = 2)} {y1}")
        AddTextBlock($"3 категория - {NY.Count(Function(o) o.Kat = 3)} ОТС против " &
                     $"{NY1.Count(Function(o) o.Kat = 3)} {y1}")
    End Sub


    Private Sub FromDrDor(OldYE As String, NY As IEnumerable(Of Otkaz), NY1 As IEnumerable(Of Otkaz))
        Dim ths = NY.Where(Function(o) Not String.IsNullOrEmpty(o.Istochnik) AndAlso
                                     o.Istochnik <> "ГИД УРАЛ" AndAlso
                                     o.Istochnik <> "РУЧНОЙ ВВОД")
        Dim thsErmak = ths.Where(Function(o) o.SerLok.Contains("ЭС5К"))

        Dim old = NY1.Where(Function(o) Not String.IsNullOrEmpty(o.Istochnik) AndAlso
                                      o.Istochnik <> "ГИД УРАЛ" AndAlso
                                      o.Istochnik <> "РУЧНОЙ ВВОД")
        Dim oldErmak = old.Where(Function(o) o.SerLok.Contains("ЭС5К"))

        Dim a = ths.Count : Dim b = CSng(Math.Round(ths.Sum(Function(o) o.PCh), 2))
        Dim a1 = old.Count : Dim b1 = CSng(Math.Round(old.Sum(Function(o) o.PCh), 2))
        Dim ermak = $"{thsErmak.Count()} ОТС на {CSng(Math.Round(thsErmak.Sum(Function(o) o.PCh), 2))}ч."
        Dim ermak1 = $"{oldErmak.Count()} ОТС на {CSng(Math.Round(oldErmak.Sum(Function(o) o.PCh), 2))}ч."
        AddTextBlock($"{vbTab}С других дорог ", isBold:=True, addNewLine:=False)
        Dim str = $"для завершения расследования по ответственности сервисных и сторонних организаций, расположенных на территории Красноярской ж.д. поступило {a} ОТС на {b} ч. против {a1} ОТС на {b1} ч. {OldYE} {Rost(a - a1)}, {Rost(CSng(b - b1))}."
        AddTextBlock(str)
        str = $"Из них с локомотивами серии 2(3)ЭС5К "
        AddTextBlock(str, isBold:=True, addNewLine:=False)
        str = $"допущено {ermak} против {ermak1} {OldYE}, {Rost(thsErmak.Count() - oldErmak.Count())}, {Rost(CSng(thsErmak.Sum(Function(o) o.PCh) - oldErmak.Sum(Function(o) o.PCh)))}"
        AddTextBlock(str)
    End Sub

    Private Sub WriKORR()
        Dim kat3Cnt = OTSList.Where(Function(o) o.Nach >= My.Settings.NachPeriod AndAlso
                                         o.Nach <= My.Settings.KonPeriod).Where(Function(o) o.Kat = 3 AndAlso o.MestoOTS_Dor.Contains("раснояр") AndAlso o.KtoZakryl.ToLower.Contains("тч") AndAlso (o.Uslovie4)).Count

        AddTextBlock($"{vbTab}Проведенная работа: ", isBold:=True, addNewLine:=False)
        AddTextBlock($"произведена корректировка в 3 категорию по {kat3Cnt} отказам. Корректировка потерь поездо-часов от отказов {GetSumKor_TXT()}")
    End Sub


    Function GetSumKor_TXT(Optional ForWord As Boolean = True) As String
        Dim Rez As String = ""
        Dim KorPart_CNT, RedirectPart_CNT As IEnumerable(Of Otkaz)

        Dim OTS_SOURCE As IEnumerable(Of Otkaz) = DirectCast(MW.TRowsContainer.ItemsSource, IEnumerable(Of Otkaz))
        Dim KorrectedOTS_inSource As Func(Of Otkaz, Boolean) = (Function(o) o.IsKorrect AndAlso o.KtoZakryl.ToLower.Contains("тч") AndAlso (o.Uslovie4))

        Dim RedirOTS As Func(Of Otkaz, Boolean) = Function(u) u.ZaKem IsNot Nothing AndAlso u.ZaKem = ("Передан на другую дорогу") AndAlso u.KtoZakryl.ToLower.Contains("тч") AndAlso (u.Uslovie4)

        If ForWord Then
            'здесь применяем готовые функции KorrectPCH, AllOTS - с учетом установленного периода
            KorPart_CNT = OTSList.Where(KorrectPCH)
            RedirectPart_CNT = OTSList.Where(AllOTS).Where(RedirOTS)
        Else
            'здесь просто считаем содержимое списка отказов
            KorPart_CNT = OTS_SOURCE.Where(KorrectedOTS_inSource)
            RedirectPart_CNT = OTS_SOURCE.Where(RedirOTS)
        End If




        Dim KorPart_PCH As Single = KorPart_CNT.Sum(Function(u) Math.Abs(u.KorPCH))
        Dim RedirectPart_PCH As Single = (RedirectPart_CNT.Sum(Function(u) u.PCh))

        'по {korrCnt + drDorCnt} случаям с уменьшением потерь на {Math.Round(korrPch + drDorPch, 2)}ч.

        If ForWord Then
            Rez = $"по {KorPart_CNT.Count + RedirectPart_CNT.Count} случаям с уменьшением потерь на {KorPart_PCH + RedirectPart_PCH:F2}ч."
        Else
            Rez = $"Корректировки:{vbCrLf}{KorPart_CNT.Count } ОТС на -{KorPart_PCH:F2}ч."
        End If

        Return Rez
    End Function


    Private Sub PoYasnilka_2(NY As List(Of Otkaz), NY1 As List(Of Otkaz))


        'AddTextBlock(PeriodTitle("Отказы технических средств отнесенные на Красноярский локомотивный комплекс"), isBold:=True, isCenter:=True)

        '        AddTextBlock($"{vbTab}За период {PeriodShort()} на Красноярский локомотивный комплекс отнесено {a} ОТС всех категорий на {b}ч. против {a1} ОТС на {b1}ч. {y1}, {Rost(a - a1)}, {Rost(CSng(b - b1))} Из них:")


        Dim y1 = "за предыдущий период" 'My.Settings.nachOLDPeriod.Year
        Dim a = NY.Count : Dim b = CSng(Math.Round(NY.Sum(Function(o) o.PCh), 2))
        Dim a1 = NY1.Count : Dim b1 = CSng(Math.Round(NY1.Sum(Function(o) o.PCh), 2))

        AddTextBlock(PeriodTitle("Отказы технических средств отнесенные на Красноярский локомотивный комплекс"), isBold:=True, isCenter:=True)

        'AddTextBlock($"Отказы технических средств отнесенные на Красноярский локомотивный комплекс{Chr(11)}за период с {nachPeriod:d MMMM yyyy} г. по {konPeriod:d MMMM yyyy} г.{Chr(11)}в сравнении с периодом от {nachOldPeriod:d MMMM yyyy} г. по {konOldPeriod:d MMMM yyyy} г.", isBold:=True, isCenter:=True)


        AddTextBlock("")

        AddTextBlock($"{vbTab}За период {PeriodShort()} на Красноярский локомотивный комплекс отнесено {a} ОТС всех категорий на {b}ч. против {a1} ОТС на {b1}ч. {y1}, {Rost(a - a1)}, {Rost(CSng(b - b1))} Из них:")
        'AddTextBlock($"{vbTab}За период с {nachPeriod:d MMMM} по {konPeriod:d MMMM} на Красноярский локомотивный комплекс отнесено {a} ОТС всех категорий на {b}ч. против {a1} ОТС на {b1}ч. {y1}, {Rost(a - a1)}, {Rost(CSng(b - b1))} Из них:")



        Dim T = NY.Where(Function(o) o.ZaKem = "" OrElse o.ZaKem.ToLower = "тчэ" OrElse o.ZaKem = "!")
        Dim Tras = T.Where(Function(o) o.ZaKem = "" OrElse o.ZaKem = "!")
        Dim TR = NY.Where(Function(o) o.ZaKem = "тр")
        Dim SLD = NY.Where(Function(o) o.ZaKemCode Like "слд*")
        Dim ZAV = NY.Where(Function(o) o.ZaKemCode Like "*око*" OrElse o.ZaKemCode Like "*рочи*")

        Dim T1 = NY1.Where(Function(o) o.ZaKem = "" OrElse o.ZaKem.ToLower = "тчэ" OrElse o.ZaKem = "!")
        Dim Tras1 = T1.Where(Function(o) o.ZaKem = "" OrElse o.ZaKem = "!")
        Dim TR1 = NY1.Where(Function(o) o.ZaKem = "тр")
        Dim SLD1 = NY1.Where(Function(o) o.ZaKemCode Like "слд*")
        Dim ZAV1 = NY1.Where(Function(o) o.ZaKemCode Like "*око*" OrElse o.ZaKemCode Like "*рочи*")

        Dim str = $"{vbTab}На дирекцию тяги отнесено {Kat123TXT(T)}"
        If Tras.Any Then str += $" из них в расследовании {Kat123TXT(Tras)}"
        str += $" ({y1} - {Kat123TXT(T1)}"
        If Tras1.Any Then str += $" из них в расследовании {Kat123TXT(Tras1)}"
        str += $"), на ТР отнесено {Kat123TXT(TR)} ({y1} - {Kat123TXT(TR1)}), " &
               $"на СЛД отнесено {Kat123TXT(SLD)} ({y1} - {Kat123TXT(SLD1)}), " &
               $"на заводы и прочие предприятия - {Kat123TXT(ZAV)} ({y1} - {Kat123TXT(ZAV1)})."
        AddTextBlock(str)
        AddTextBlock("")
        AddTextBlock($"{vbTab}За рассматриваемый период отнесенные на локомотивный комплекс отказы технических средств распределились ", addNewLine:=False)
        AddTextBlock($"по категориям ", isBold:=True, addNewLine:=False)
        AddTextBlock($"следующим образом:")
        AddTextBlock($"Отказы 1 категории - {NY.Where(Function(o) o.Kat = 1).Count} {EndOfSluch_TXT(NY.Where(Function(o) o.Kat = 1).Count)} ({y1} - {NY1.Where(Function(o) o.Kat = 1).Count} {EndOfSluch_TXT(NY1.Where(Function(o) o.Kat = 1).Count)}) {Rost(NY.Where(Function(o) o.Kat = 1).Count - NY1.Where(Function(o) o.Kat = 1).Count)}.
Отказы 2 категории - {NY.Where(Function(o) o.Kat = 2).Count} {EndOfSluch_TXT(NY.Where(Function(o) o.Kat = 2).Count)} ({y1} - {NY1.Where(Function(o) o.Kat = 2).Count} {EndOfSluch_TXT(NY1.Where(Function(o) o.Kat = 2).Count)}) {Rost(NY.Where(Function(o) o.Kat = 2).Count - NY1.Where(Function(o) o.Kat = 2).Count)}.
Отказы 3 категории - {NY.Where(Function(o) o.Kat = 3).Count} {EndOfSluch_TXT(NY.Where(Function(o) o.Kat = 3).Count)} ({y1} - {NY1.Where(Function(o) o.Kat = 3).Count} {EndOfSluch_TXT(NY1.Where(Function(o) o.Kat = 3).Count)}) {Rost(NY.Where(Function(o) o.Kat = 3).Count - NY1.Where(Function(o) o.Kat = 3).Count)}.")

        Dim kmp = {
            GetPoKompl(NY, NY1, "ТЧЭ-1", "ТРПУ-11", "слд1", $"{y1}"),
            GetPoKompl(NY, NY1, "ТЧЭ-2", "ТРПУ-4", "слд2", $"{y1}"),
            GetPoKompl(NY, NY1, "ТЧЭ-3", "ТРПУ-9", "слд3", $"{y1}"),
            GetPoKompl(NY, NY1, "ТЧЭ-5", "ТРПУ-12", "слд5", $"{y1}"),
            GetPoKompl(NY, NY1, "ТЧЭ-7", "ТРПУ-10", "слд7", $"{y1}")
        }.OrderByDescending(Function(x) CInt(x(1))).ToList()
        AddTextBlock("")
        AddTextBlock($"{vbTab}По комплексам эксплуатационных локомотивных депо ", isBold:=True, addNewLine:=False)
        AddTextBlock($"наибольшее число отказов допущено в {Otkaz.NormalizeName(kmp(0)(0))} - {kmp(0)(1)} ОТС на {kmp(0)(2)}ч. ({kmp(0)(3)} - {kmp(0)(4)} ОТС на {kmp(0)(5)}ч.), {kmp(0)(6)}, {kmp(0)(7)}")

        AddTextBlock("СПРАВОЧНО:", isItalic:=True, isCenter:=True)
        For i = 1 To 4
            AddTextBlock($"{Otkaz.NormalizeName(kmp(i)(0))} - {kmp(i)(1)} ОТС на {kmp(i)(2)}ч. " &
                         $"({kmp(i)(3)} - {kmp(i)(4)} ОТС на {kmp(i)(5)}ч.), {kmp(i)(6)}, {kmp(i)(7)}",
                         isItalic:=True)
        Next


        Dim serLok = GetSerLokk($"{y1}", NY, NY1).OrderByDescending(Function(x) CInt(x(1))).ToList()

        If serLok.Count > 0 Then
            AddTextBlock("")
            AddTextBlock($"{vbTab}По сериям локомотивов ", isBold:=True, addNewLine:=False)
            AddTextBlock($"наибольшее число отказов допущено локомотивами {serLok(0)(0)} - {serLok(0)(1)} ОТС ({serLok(0)(3)} - {serLok(0)(2)} ОТС), {serLok(0)(4)}")

            AddTextBlock("СПРАВОЧНО:", isItalic:=True, isCenter:=True)
            For i = 0 To serLok.Count - 1
                AddTextBlock($"{serLok(i)(0)} - {serLok(i)(1)} ОТС ({serLok(i)(3)} - {serLok(i)(2)} ОТС), {serLok(i)(4)}", isItalic:=True)
            Next
        End If

        'Dim serLok = GetSerLokk($"{y1}", NY, NY1).OrderByDescending(Function(x) CInt(x(1))).ToList
        'AddTextBlock("")
        'AddTextBlock($"{vbTab}По сериям локомотивов ", isBold:=True, addNewLine:=False)
        'AddTextBlock($"наибольшее число отказов допущено локомотивами {serLok(0)(0)} - {serLok(0)(1)} ОТС ({serLok(0)(3)} - {serLok(0)(2)} ОТС), {serLok(0)(4)}")

        'AddTextBlock("СПРАВОЧНО:", isItalic:=True, isCenter:=True)
        'For i = 0 To serLok.Count - 1
        '    AddTextBlock($"{serLok(i)(0)} - {serLok(i)(1)} ОТС ({serLok(i)(3)} - {serLok(i)(2)} ОТС), {serLok(i)(4)}",
        '                 isItalic:=True)
        'Next

        WriOBORUD(NY, NY1, $"{y1}", Sokr:=True)
        AddTextBlock("")
        FromDrDor($"{y1}", NY, NY1)
        WriKORR() 'NY)
        AddTextBlock($"{Chr(12)}")
        'AddTextBlock($"{vbCrLf}По видам отказавшего оборудования ", isBold:=True, addNewLine:=False)
        WriOBORUD(NY, NY1, $"{y1}", Sokr:=True)
        AddTextBlock("")
    End Sub

    Private Function GetPoKompl(NY As IEnumerable(Of Otkaz), NY1 As IEnumerable(Of Otkaz),
                                DepT As String, DepTR As String, DepSLD As String, OldYe As String) As List(Of String)
        Dim T1 = NY.Where(Function(o) o.KtoZakryl = DepT AndAlso
                                       (o.ZaKemCode = "" OrElse o.ZaKemCode = "тч" OrElse o.ZaKemCode = "!"))
        Dim TR1 = NY.Where(Function(o) o.ZaKem = "тр" AndAlso o.KtoZakryl = DepTR)
        Dim S1 = NY.Where(Function(o) o.ZaKemCode = DepSLD)
        Dim Z1 = NY.Where(Function(o) o.KtoZakryl = DepT AndAlso
                                       (o.ZaKemCode Like "*око*" OrElse o.ZaKemCode Like "*рочи*"))

        Dim T1_1 = NY1.Where(Function(o) o.KtoZakryl = DepT AndAlso
                                          (o.ZaKemCode = "" OrElse o.ZaKemCode = "тч"))
        Dim TR1_1 = NY1.Where(Function(o) o.ZaKem = "тр" AndAlso o.KtoZakryl = DepTR)
        Dim S1_1 = NY1.Where(Function(o) o.ZaKemCode = DepSLD)
        Dim Z1_1 = NY1.Where(Function(o) o.KtoZakryl = DepT AndAlso
                                          (o.ZaKemCode Like "*око*" OrElse o.ZaKemCode Like "*рочи*"))

        Dim Tch = Math.Round(T1.Sum(Function(o) o.PCh), 2)
        Dim TRch = Math.Round(TR1.Sum(Function(o) o.PCh), 2)
        Dim Sch = Math.Round(S1.Sum(Function(o) o.PCh), 2)
        Dim Zch = Math.Round(Z1.Sum(Function(o) o.PCh), 2)

        Dim Tch_1 = Math.Round(T1_1.Sum(Function(o) o.PCh), 2)
        Dim TRch_1 = Math.Round(TR1_1.Sum(Function(o) o.PCh), 2)
        Dim Sch_1 = Math.Round(S1_1.Sum(Function(o) o.PCh), 2)
        Dim Zch_1 = Math.Round(Z1_1.Sum(Function(o) o.PCh), 2)

        Dim A = T1.Count + TR1.Count + S1.Count + Z1.Count
        Dim B = CSng(Math.Round(Tch + TRch + Sch + Zch, 2))
        Dim C = Kat12_OTS_Kompl(T1, TR1, S1, Z1)
        Dim D = Kat3_OTS_Kompl(T1, TR1, S1, Z1)
        Dim E = Kat2_OTS_Kompl(T1, TR1, S1, Z1)
        Dim F = Kat1_OTS_Kompl(T1, TR1, S1, Z1)

        Dim A_1 = T1_1.Count + TR1_1.Count + S1_1.Count + Z1_1.Count
        Dim B_1 = CSng(Math.Round(Tch_1 + TRch_1 + Sch_1 + Zch_1, 2))
        Dim C_1 = Kat12_OTS_Kompl(T1_1, TR1_1, S1_1, Z1_1)
        Dim D_1 = Kat3_OTS_Kompl(T1_1, TR1_1, S1_1, Z1_1)
        Dim E_1 = Kat2_OTS_Kompl(T1_1, TR1_1, S1_1, Z1_1)
        Dim F_1 = Kat1_OTS_Kompl(T1_1, TR1_1, S1_1, Z1_1)

        Return New List(Of String) From {
            GetDepoName(DepT), A, B, OldYe, A_1, B_1, Rost(A - A_1), Rost(CSng(B - B_1)),
            C, C_1, D, D_1, E, E_1, F, F_1
        }
    End Function

    Private Function GetDepoName(dept As String)
        Select Case dept
            Case "ТЧЭ-1"
                Return "ТЧЭ Боготол"
            Case "ТЧЭ-2"
                Return "ТЧЭ Красноярск"
            Case "ТЧЭ-3"
                Return "ТЧЭ Иланская"
            Case "ТЧЭ-5"
                Return "ТЧЭ Ачинск"
            Case "ТЧЭ-7"
                Return "ТЧЭ Абакан"
            Case Else
                Return dept
        End Select
    End Function

    Private Function GetSerLokk(OldYE As String, NY As IEnumerable(Of Otkaz), NY1 As IEnumerable(Of Otkaz),
                                Optional ExactSer As Boolean = False) As List(Of List(Of String))
        Dim tSet = If(ExactSer,
                      NY.Where(Function(o) Not String.IsNullOrEmpty(o.SerLokExact)).Select(Function(o) o.SerLokExact),
                      NY.Where(Function(o) Not String.IsNullOrEmpty(o.SerLok)).Select(Function(o) o.SerLok))
        Dim oSet = If(ExactSer,
                      NY1.Where(Function(o) Not String.IsNullOrEmpty(o.SerLokExact)).Select(Function(o) o.SerLokExact),
                      NY1.Where(Function(o) Not String.IsNullOrEmpty(o.SerLok)).Select(Function(o) o.SerLok))
        Dim allSer = tSet.Concat(oSet).Distinct().ToList()

        Dim rez As New List(Of List(Of String))
        For Each ser In allSer
            Dim gg = GetCntOfSerlok(ser, NY, NY1, ExactSer)
            rez.Add(New List(Of String) From {
                ser, gg(0), gg(1), OldYE, Rost(gg(0) - gg(1))
            })
        Next
        Return rez
    End Function

    Private Function GetCntOfSerlok(Ser As String, NY As IEnumerable(Of Otkaz), NY1 As IEnumerable(Of Otkaz),
                                    Optional SerLokExact As Boolean = False) As Integer()
        Dim A = NY.Count(Function(o) If(SerLokExact, o.SerLokExact = Ser, o.SerLok = Ser))
        Dim B = NY1.Count(Function(o) If(SerLokExact, o.SerLokExact = Ser, o.SerLok = Ser))
        Return {A, B}
    End Function

    Private Sub WriOBORUD(NY As List(Of Otkaz), NY1 As List(Of Otkaz), OldYE As String,
                      Optional Sokr As Boolean = False, Optional Ramka As Boolean = True, Optional BezZag As Boolean = False)

        Dim tgGroups = NY.GroupBy(Function(o) o.MyKlasLev1).OrderByDescending(Function(g) g.Count()).ToList()
        Dim pgDict = NY1.GroupBy(Function(o) o.MyKlasLev1).ToDictionary(Function(g) g.Key, Function(g) g.Count())

        If Sokr Then
            AddTextBlock($"{vbCrLf}{vbTab}По видам отказавшего оборудования ", isBold:=True, addNewLine:=False)

            Dim topGroup = tgGroups.FirstOrDefault()          ' ← было: tgGroups(0)

            If topGroup Is Nothing Then
                ' текущий период пустой — аккуратно пишем и выходим из ветки
                AddTextBlock("— данных за период нет")
            Else
                Dim prevCount = pgDict.GetValueOrDefault(topGroup.Key, 0)
                Dim delta = topGroup.Count() - prevCount
                Dim STR2 = $"наибольшее число неисправностей отнесено на группу {Chr(34)}{topGroup.Key}{Chr(34)} - {topGroup.Count()} {EndOfSluch_TXT(topGroup.Count())} ({OldYE} - {prevCount} {EndOfSluch_TXT(prevCount)}), {Rost(delta)}"
                AddTextBlock(STR2)
                AddTextBlock("СПРАВОЧНО:", isItalic:=True, isCenter:=True)
            End If
            'AddTextBlock($"{vbCrLf}{vbTab}По видам отказавшего оборудования ", isBold:=True, addNewLine:=False)
            'Dim topGroup = tgGroups(0)
            'Dim prevCount = pgDict.GetValueOrDefault(topGroup.Key, 0)
            'Dim delta = topGroup.Count() - prevCount
            'Dim STR2 = $"наибольшее число неисправностей отнесено на группу {Chr(34)}{topGroup.Key}{Chr(34)} - {topGroup.Count()} {EndOfSluch_TXT(topGroup.Count())} ({OldYE} - {prevCount} {EndOfSluch_TXT(prevCount)}), {Rost(delta)}"
            'AddTextBlock(STR2)
            'AddTextBlock("СПРАВОЧНО:", isItalic:=True, isCenter:=True)
        Else
            If Not BezZag Then
                AddTextBlock(PeriodTitle("Распределение отказов технических средств по оборудованию"), isBold:=True, isCenter:=True)
                AddTextBlock("")
                'AddTextBlock($"Распределение отказов технических средств по оборудованию{Chr(11)}за период с {nachPeriod:d MMMM yyyy} г. по {konPeriod:d MMMM yyyy} г.{Chr(11)}в сравнении с периодом от {nachOldPeriod:d MMMM yyyy} г. по {konOldPeriod:d MMMM yyyy} г.", isBold:=True, isCenter:=True)
                'AddTextBlock("")

            End If

        End If

        For Each g In tgGroups
            Dim currCount = g.Count()
            Dim prevCount = pgDict.GetValueOrDefault(g.Key, 0)
            Dim delta = currCount - prevCount

            ' Строка 1-го уровня
            AddTextBlock($"{g.Key} - {currCount} {EndOfSluch_TXT(currCount)} " &
                         $"({OldYE} - {prevCount} {EndOfSluch_TXT(prevCount)}) {Rost(delta)}",
                         isBold:=Not Sokr, isItalic:=Sokr)

            If Not Sokr AndAlso Not String.IsNullOrEmpty(g.Key) Then
                ' Lev3 напрямую (когда Lev2 = Lev1)
                Dim lev3Direct = g.Where(Function(o) o.MyKlasLev2 = g.Key).
                                    GroupBy(Function(o) o.MyKlasLev3).
                                    Where(Function(gr) Not String.IsNullOrEmpty(gr.Key)).
                                    OrderByDescending(Function(gr) gr.Count()).ToList()

                If lev3Direct.Count > 0 Then
                    Dim parts = lev3Direct.Select(Function(lev3)
                                                      Dim cntPrev = NY1.Where(Function(o) o.MyKlasLev3 = lev3.Key).Count()
                                                      Return $"{lev3.Key} ({lev3.Count()}/{cntPrev})"
                                                  End Function).ToArray()
                    AddTextBlock($"{vbTab}{String.Join(", ", parts)}", "Times New Roman", 12)
                End If

                ' Lev2 (≠ Lev1) + их Lev3
                Dim g2 = g.GroupBy(Function(o) o.MyKlasLev2).
                            Where(Function(sub2) sub2.Key <> g.Key).
                            OrderByDescending(Function(sub2) sub2.Count()).ToList()

                For Each sub2 In g2
                    Dim pg2Count = NY1.Where(Function(o) o.MyKlasLev2 = sub2.Key).Count()
                    AddTextBlock($"-     {sub2.Key} {sub2.Count()} {EndOfSluch_TXT(sub2.Count())} " &
                                 $"({OldYE} - {pg2Count} {EndOfSluch_TXT(pg2Count)})",
                                 isItalic:=True)

                    Dim lev3InLev2 = sub2.GroupBy(Function(o) o.MyKlasLev3).
                                        Where(Function(gr) Not String.IsNullOrEmpty(gr.Key)).
                                        OrderByDescending(Function(gr) gr.Count()).ToList()
                    If lev3InLev2.Count > 0 Then
                        Dim parts = lev3InLev2.Select(Function(lev3)
                                                          Dim cntPrev = NY1.Where(Function(o) o.MyKlasLev3 = lev3.Key).Count
                                                          Return $"{lev3.Key} ({lev3.Count()}/{cntPrev})"
                                                      End Function).ToArray()
                        AddTextBlock($"{vbTab}{String.Join(", ", parts)}", "Times New Roman", 12)
                    End If
                Next

                ' Пустая строка ПОСЛЕ рамки (между блоками)
                AddEmptyLineAfterRange()
            End If
        Next

    End Sub

    Private Sub LokPoKomplexText(NY As List(Of Otkaz), NY1 As List(Of Otkaz))
        Dim y1 = "за предыдущий период" '
        WriteMSG_to_TB_Lok(NY, NY1, y1, "ТЧЭ Боготол", 1)
        WriteMSG_to_TB_Lok(NY, NY1, y1, "ТЧЭ Красноярск", 2)
        WriteMSG_to_TB_Lok(NY, NY1, y1, "ТЧЭ Иланская", 3)
        WriteMSG_to_TB_Lok(NY, NY1, y1, "ТЧЭ Ачинск", 5)
        WriteMSG_to_TB_Lok(NY, NY1, y1, "ТЧЭ Абакан", 7)
    End Sub

    Private Sub WriteMSG_to_TB_Lok(NY As List(Of Otkaz), NY1 As List(Of Otkaz), y1 As String,
                                   kmpName As String, kmpInt As Integer)
        Dim tg = NY.Where(Function(o) o.GetKomplexOTS() = kmpInt).ToList()
        Dim pg = NY1.Where(Function(o) o.GetKomplexOTS() = kmpInt).ToList()

        'AddTextBlock($"Распределение отказов технических средств по сериям локомотивов{Chr(11)}за период с {My.Settings.NachPeriod:d MMMM yyyy} г. по {My.Settings.KonPeriod:d MMMM yyyy} г.{Chr(11)}в сравнении с периодом от {My.Settings.nachOLDPeriod:d MMMM yyyy} г. по {My.Settings.konOLDPeriod:d MMMM yyyy} г.{Chr(11)}в локомотивном комплексе {kmpName}", isBold:=True, isCenter:=True)
        AddTextBlock($"В локомотивном комплексе {kmpName}", isBold:=True, isCenter:=True)

        Dim serList = GetSerLokk(y1, tg, pg)
        For Each item In serList
            If CInt(item(1)) > 0 OrElse CInt(item(2)) > 0 Then
                AddTextBlock($"{item(0)} - {item(1)} ОТС ({item(3)} - {item(2)} ОТС), {item(4)}")
            End If
        Next
        AddTextBlock("")
    End Sub

    Private Sub OborudPoKomplexText(NY As List(Of Otkaz), NY1 As List(Of Otkaz))
        Dim y1 = "за предыдущий период"
        'AddTextBlock($"Распределение отказов технических средств по оборудованию{Chr(11)}за период с {My.Settings.NachPeriod:d MMMM yyyy} г. по {My.Settings.KonPeriod:d MMMM yyyy} г.{Chr(11)}в сравнении с периодом от {My.Settings.nachOLDPeriod:d MMMM yyyy} г.", isBold:=True, isCenter:=True)


        WriteMSG_to_TB(NY, NY1, y1, "ТЧЭ Боготол", 1)
        'AddTextBlock($"{vbCrLf}")
        WriteMSG_to_TB(NY, NY1, y1, "ТЧЭ Красноярск", 2)
        'AddTextBlock($"{vbCrLf}")
        WriteMSG_to_TB(NY, NY1, y1, "ТЧЭ Иланская", 3)
        'AddTextBlock($"{vbCrLf}")
        WriteMSG_to_TB(NY, NY1, y1, "ТЧЭ Ачинск", 5)
        'AddTextBlock($"{vbCrLf}")
        WriteMSG_to_TB(NY, NY1, y1, "ТЧЭ Абакан", 7)
        AddTextBlock($"{Chr(12)}")
    End Sub

    Private Sub WriteMSG_to_TB(NY As List(Of Otkaz), NY1 As List(Of Otkaz), y1 As String,
                               kmpName As String, kmpInt As Integer)
        Dim tg = NY.Where(Function(o) o.GetKomplexOTS() = kmpInt).ToList()
        Dim pg = NY1.Where(Function(o) o.GetKomplexOTS() = kmpInt).ToList()

        AddTextBlock($"В локомотивном комплексе {kmpName}", isBold:=True, isCenter:=True)
        AddTextBlock("")

        WriOBORUD(tg, pg, y1, Ramka:=False, BezZag:=True)
    End Sub




    ''' <summary>
    ''' Глобальное распределение отказавшего оборудования в разрезе ВСЕХ серий локомотивов
    ''' </summary>
    Private Sub OborudPoSerLokText(NY As List(Of Otkaz), NY1 As List(Of Otkaz))
        Dim y1 = "за предыдущий период"

        Dim Ff As String = PeriodTitle("Распределение отказавшего оборудования по сериям локомотивов")
        AddTextBlock(Ff, isCenter:=True, isBold:=True)
        AddTextBlock("")
        'Dim Ff As String = $"Распределение отказавшего оборудования по сериям локомотивов{Chr(11)}за период с {My.Settings.NachPeriod:d MMMM yyyy} г. по {My.Settings.KonPeriod:d MMMM yyyy} г.{Chr(11)}в сравнении с периодом от {My.Settings.nachOLDPeriod:d MMMM yyyy} г. по {My.Settings.konOLDPeriod:d MMMM yyyy} г."
        'AddTextBlock(Ff, isCenter:=True, isBold:=True)
        'AddTextBlock("")



        ' 1. Собираем уникальный список ВСЕХ серий (SerLokExact) из обоих периодов
        Dim allSeries = NY.Select(Function(o) o.SerLok) _
                          .Concat(NY1.Select(Function(o) o.SerLok)) _
                          .Where(Function(s) Not String.IsNullOrWhiteSpace(s)) _
                          .Distinct() _
                          .OrderBy(Function(s) s) _
                          .ToList()
        ' 2. Проходим по каждой серии
        For Each ser In allSeries
            ' Фильтруем глобальные списки под конкретную серию
            Dim tgSer = NY.Where(Function(o) o.SerLok = ser).ToList()
            Dim pgSer = NY1.Where(Function(o) o.SerLok = ser).ToList()

            '' Если по какой-то причине в обоих периодах пусто - пропускаем
            'If Not tgSer.Any() AndAlso Not pgSer.Any() Then Continue For

            ' Исключаем серии, у которых в текущем году нет отказов
            If Not tgSer.Any() Then Continue For

            ' Заголовок для конкретной серии
            AddTextBlock($"СЕРИЯ ЛОКОМОТИВОВ {ser} ({tgSer.Count} / {pgSer.Count})", isBold:=True, isCenter:=True)

            ' Вызываем вашу готовую функцию сборки оборудования (3 уровня)
            WriOBORUD(tgSer, pgSer, y1, Ramka:=False, BezZag:=True)

            AddTextBlock("") ' Пустая строка между сериями
        Next
    End Sub





    ''' <summary>
    ''' Универсальный конструктор диаграммы. Рисует и вставляет в Word.
    ''' </summary>
    ''' <param name="title">Заголовок диаграммы</param>
    ''' <param name="xLabels">Подписи по оси X (например, серии локомотивов)</param>
    ''' <param name="seriesData">Серии данных: каждая = (Имя, Значения, Цвет)</param>
    ''' <param name="xAxisTitle">Подпись оси X</param>
    ''' <param name="yAxisTitle">Подпись оси Y</param>
    ''' <param name="width">Ширина картинки</param>
    ''' <param name="height">Высота картинки</param>
    Private Sub DrawAndInsertChart(
    title As String,
    xLabels As List(Of String),
    seriesData As IEnumerable(Of (Name As String, Values As List(Of Double), Color As System.Drawing.Color)),
    Optional xAxisTitle As String = "",
    Optional yAxisTitle As String = "",
    Optional width As Integer = 850,
    Optional height As Integer = 420)

        If WDoc Is Nothing OrElse Not xLabels.Any() Then Return

        Using bmp As New Bitmap(width, height)
            Using g As Graphics = Graphics.FromImage(bmp)
                g.Clear(Color.White)
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

                ' --- Заголовок ---
                Using fTitle As New System.Drawing.Font("Times New Roman", 14, System.Drawing.FontStyle.Bold)
                    g.DrawString(title, fTitle, Brushes.Black,
                    New RectangleF(0, 10, width, 40),
                    New StringFormat() With {.Alignment = StringAlignment.Center})
                End Using

                ' --- Область графика ---
                Dim mL = 70, mR = 30, mT = 75, mB = 80  ' УВЕЛИЧЕН mT с 55 до 75
                Dim plotW = width - mL - mR
                Dim plotH = height - mT - mB

                ' Оси — только горизонтальная (ось X)
                g.DrawLine(Pens.Black, mL, mT + plotH, mL + plotW, mT + plotH)

                ' Максимум по Y
                Dim maxVal = seriesData.SelectMany(Function(s) s.Values).DefaultIfEmpty(1).Max()
                If maxVal = 0 Then maxVal = 1

                ' ИСПРАВЛЕНО: явно задаём тип Single
                Dim scaleY As Single = CSng(plotH / maxVal)

                ' Подпись оси Y (если задана)
                Using fAxis As New System.Drawing.Font("Times New Roman", 10)
                    If Not String.IsNullOrEmpty(yAxisTitle) Then
                        g.DrawString(yAxisTitle, fAxis, Brushes.Black, 10, CSng(mT + plotH \ 2))
                    End If

                    ' --- Столбцы ---
                    Dim seriesCount = seriesData.Count
                    Dim barW As Single = Math.Max(10, Math.Min(40, plotW \ (xLabels.Count * (seriesCount + 1))))
                    Dim gap As Single = barW / 3
                    Dim startX As Single = mL + 15

                    For i = 0 To xLabels.Count - 1
                        Dim xBase As Single = startX + i * (barW * seriesCount + gap * (seriesCount + 1))

                        For s = 0 To seriesCount - 1
                            Dim val As Single = CSng(seriesData(s).Values(i))
                            Dim h As Single = CSng(val * scaleY)

                            If h > 0 Then
                                Dim xRect As Single = xBase + s * (barW + gap)
                                Dim yRect As Single = CSng(mT + plotH - h)

                                Using brush As New SolidBrush(seriesData(s).Color)
                                    g.FillRectangle(brush, xRect, yRect, barW, h)
                                End Using
                                g.DrawRectangle(Pens.Black, xRect, yRect, barW, h)

                                ' === ЗНАЧЕНИЕ НАД СТОЛБЦОМ ===
                                Dim valText As String = val.ToString("F0")
                                Using fVal As New System.Drawing.Font("Times New Roman", 8, System.Drawing.FontStyle.Bold)
                                    Dim valSize As SizeF = g.MeasureString(valText, fVal)
                                    Dim valX As Single = xRect + (barW - valSize.Width) / 2
                                    Dim valY As Single = yRect - valSize.Height - 2
                                    g.DrawString(valText, fVal, Brushes.Black, valX, valY)
                                End Using
                            End If
                        Next

                        ' Подпись X
                        Dim lbl = If(xLabels(i).Length > 10, xLabels(i).Substring(0, 9) & "..", xLabels(i))
                        Using fX As New System.Drawing.Font("Times New Roman", 9)
                            g.DrawString(lbl, fX, Brushes.Black,
                        New RectangleF(xBase - gap, CSng(mT + plotH + 5), barW * seriesCount + gap * seriesCount, 30),
                        New StringFormat() With {.Alignment = StringAlignment.Center})
                        End Using
                    Next

                    ' Подпись оси X
                    If Not String.IsNullOrEmpty(xAxisTitle) Then
                        g.DrawString(xAxisTitle, fAxis, Brushes.Black,
                    New RectangleF(CSng(mL), CSng(mT + plotH + 40), CSng(plotW), 20),
                    New StringFormat() With {.Alignment = StringAlignment.Center})
                    End If

                    ' --- Легенда ---
                    Dim legY As Single = height - 30
                    Dim legX As Single = width - 200
                    For s = 0 To seriesCount - 1
                        g.FillRectangle(New SolidBrush(seriesData(s).Color), legX, legY + s * 18, 15, 12)
                        g.DrawString(seriesData(s).Name, fAxis, Brushes.Black, legX + 20, legY + s * 18 - 2)
                    Next
                End Using
            End Using

            ' --- Сохранение и вставка в Word ---
            Dim tempFile = Path.Combine(Path.GetTempPath(), $"chart_{Guid.NewGuid():N}.png")
            Try
                bmp.Save(tempFile, ImageFormat.Png)
                WApp.Selection.EndKey(WdUnits.wdStory)
                WApp.Selection.TypeParagraph()

                Dim pic = WApp.Selection.InlineShapes.AddPicture(tempFile, False, True)
                Dim shp = pic.ConvertToShape()
                shp.WrapFormat.Type = WdWrapType.wdWrapTopBottom
                shp.RelativeHorizontalPosition = WdRelativeHorizontalPosition.wdRelativeHorizontalPositionPage
                shp.Left = WdShapePosition.wdShapeCenter

                WApp.Selection.TypeParagraph()
            Finally
                If File.Exists(tempFile) Then
                    Try
                        File.Delete(tempFile)
                    Catch
                    End Try
                End If
            End Try
        End Using
    End Sub




End Module
