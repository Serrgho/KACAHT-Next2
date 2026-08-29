Namespace Kas
	Public Class AttachedFile
		Public Property FileName As String      ' Имя файла (например, "протокол №1563.pdf")
		Public Property DownloadUrl As String   ' Ссылка для скачивания
		Public Property FileSize As String      ' Размер (например, "84 Кб") - опционально

		' ← ← ← НОВЫЕ ПОЛЯ:
		Public Property UploadedBy As String        ' Кто загрузил (Дорога/Депо | ФИО)
		Public Property UploadDate As String        ' Дата/время загрузки
		Public Property Comment As String           ' Комментарий к документу
	End Class

End Namespace



