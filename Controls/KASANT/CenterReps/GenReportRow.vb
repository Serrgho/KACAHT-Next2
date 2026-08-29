
Namespace Kas

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
End Namespace


