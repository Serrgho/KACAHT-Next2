Namespace Kas
	Public Class TrainDelay
		Public Property TrainNumber As String
		Public Property TrainType As String
		Public Property DelayMinutes As Integer
		Public Property IsFirst As Boolean
		Public Property Locomotives As List(Of LocoInfo)

		' ← ← ← НОВОЕ: Маршрут и время из <nobr>
		Public Property RouteInfo As String

		Public Sub New()
			Locomotives = New List(Of LocoInfo)
		End Sub
	End Class
End Namespace


