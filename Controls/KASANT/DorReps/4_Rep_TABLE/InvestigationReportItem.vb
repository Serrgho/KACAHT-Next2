
Imports System.ComponentModel

Namespace Kas

	''' <summary>
	''' Модель данных для строки отчёта о состоянии расследования отказов
	''' </summary>
	Public Class InvestigationReportItem
		Implements INotifyPropertyChanged
		Private _isTotalRow As Boolean = False

		' Основные данные
		Public Property Name As String
		Public Property Section As String  ' ⭐ НОВОЕ: блок, к которому относится строка
		Public Property Order As Integer

		Public Property IsTotalRow As Boolean
			Get
				Return _isTotalRow
			End Get
			Set(value As Boolean)
				If _isTotalRow <> value Then
					_isTotalRow = value
					RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(NameOf(IsTotalRow)))
				End If
			End Set
		End Property

		Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

		' Текущий период
		Public Property Total As Integer
		Public Property Accepted As Integer
		Public Property Overdue As Integer
		Public Property Investigated As Integer
		Public Property NotAccepted As Integer

		' Прошлый год (аналогичный период)
		Public Property Total_PY As Integer
		Public Property Accepted_PY As Integer
		Public Property Overdue_PY As Integer
		Public Property Investigated_PY As Integer
		Public Property NotAccepted_PY As Integer

		' Дельты (текущий - прошлый год)
		Public ReadOnly Property Delta_Total As Integer
			Get
				Return Total - Total_PY
			End Get
		End Property
		Public ReadOnly Property Delta_Accepted As Integer
			Get
				Return Accepted - Accepted_PY
			End Get
		End Property
		Public ReadOnly Property Delta_Overdue As Integer
			Get
				Return Overdue - Overdue_PY
			End Get
		End Property
		Public ReadOnly Property Delta_Investigated As Integer
			Get
				Return Investigated - Investigated_PY
			End Get
		End Property
		Public ReadOnly Property Delta_NotAccepted As Integer
			Get
				Return NotAccepted - NotAccepted_PY
			End Get
		End Property

	End Class

End Namespace



