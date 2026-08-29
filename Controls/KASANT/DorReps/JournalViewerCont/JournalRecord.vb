Namespace Kas

	'=======================================================================
	' КЛАСС ДЛЯ ЗАПИСИ ЖУРНАЛА (НОВЫЙ)
	'=======================================================================
	Public Class JournalRecord
		Public Property Status As String
		Public Property Category As String
		Public Property ViolId As String
		Public Property StartTime As String
		Public Property EndTime As String
		Public Property FromDept As String
		Public Property ToDept As String
		Public Property Location As String
		Public Property MestoOTS_TXT As String         ' сборная строка
		Public Property Equipment As String
		'Public Property DuplicatesCount As Integer          ' ← НОВОЕ: кол-во дубликатов
		Public Property TrainCount As Integer               ' ← НОВОЕ: кол-во поездов
		Public Property IsLocked As Boolean          ' Расследование завершено 🔒
		Public Property HasAttachments As Boolean    ' Есть вложения 📎
		Public Property NeedsAdditional As Boolean   ' Требуется доп. расследование ⚠️
		Public Property IsEasapr As Boolean          ' Передан в ЕАСАПР 🔄
		'Public Property HasDuplicates As Boolean     ' Есть дубликаты 🔗
		Public Property AttachCount As Integer       ' Количество файлов
		Public Property ASU As String      ' Источник: "ГИД «Урал-ВНИИЖТ»", "КАСАНТ (ручной ввод)" и т.д.
	End Class
End Namespace


