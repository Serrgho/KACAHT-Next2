Namespace Kas

	Public Class OtsData
		Public Property IsKasantHierarchy As Boolean = False
		Public Property Oborud As String
		Public Property PCHasy As String
		Public Property ViolId As String
		Public Property Location As String
		Public Property Category1052 As String
		Public Property Category775 As String
		Public Property Category1915 As String
		Public Property StartTime As String
		Public Property EndTime As String
		Public Property Duration As String
		Public Property CharacterText As String
		Public Property FailedEquipment As String
		Public Property FailureManifestation As String
		Public Property InvestigationStatus As String  ' ← НОВОЕ: статус расследования
		Public Property DangerStatus As String  ' ← НОВОЕ: статус ОПАСНЫЙ ОТКАЗ

		Public Property Is_5_15_OTS As String

		Public Property CharacterComment As String  ' ← НОВОЕ: комментарий из таблицы "Характер"
		Public Property ThirdPartyOrg As String  ' ← НОВОЕ: Наименование сторонней организации
		Public Property Consequences As String  ' Текст о транспортном происшествии
		Public Property Korporativ As String  ' Текст о корпоративном происшествии
		Public Property FactStartTime As String
		Public Property FactEndTime As String
		Public Property FactDuration As String
		Public Property HasHelperLoco As Boolean = False       ' Признак наличия
		Public Property HelperLocoInfo As String = ""          ' Текст (номер поезда, приказ)

		Public Property DelayedTrains As List(Of TrainDelay)
		Public Property History As List(Of HistoryRecord)
		Public Property PeredanOnOtherDor As String
		Public Property AttachedFiles As New List(Of AttachedFile)
		Public Property ServiceOrgDetails As String = "" ' Детали сервиса + договор
		Public Property EasaprStatus As String = ""      ' Статус из ЕАСАПР

		' Список пар "Параметр - Значение" из блока alien_guilty_table
		Public Property AlienGuiltyParams As New List(Of KeyValuePair(Of String, String))
		Public Sub New()
			DelayedTrains = New List(Of TrainDelay)
			History = New List(Of HistoryRecord)
		End Sub
	End Class

End Namespace


