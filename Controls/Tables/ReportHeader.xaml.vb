Imports System.Globalization

Namespace Kas
    Partial Public Class ReportHeader
        Private Shared ReadOnly Ru As CultureInfo = CultureInfo.GetCultureInfo("ru-RU")

        ' Массив окончаний для родительного падежа (кого? чего?)
        Private Shared ReadOnly MonthGenitive As String() = {
            "Января", "Февраля", "Марта", "Апреля", "Мая", "Июня",
            "Июля", "Августа", "Сентября", "Октября", "Ноября", "Декабря"
        }

        Public Sub New()
            InitializeComponent()
        End Sub

        ''' <summary>
        ''' Устанавливает дату отчета в формате "29 Августа 2026г."
        ''' </summary>
        Public Sub SetReportDate(reportDate As Date)
            Dim day As Integer = reportDate.Day
            Dim monthName As String = MonthGenitive(reportDate.Month - 1)
            Dim year As Integer = reportDate.Year

            LblDate.Text = $"на {day} {monthName} {year}г."
        End Sub
    End Class
End Namespace
