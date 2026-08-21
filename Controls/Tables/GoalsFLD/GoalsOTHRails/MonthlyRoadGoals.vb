Imports Newtonsoft.Json

Public Class MonthlyRoadGoals
    Public Property Year As Integer
    Public Property Month As Integer

    ' ВСЖД
    Public Property VsjdOtsGoal As Integer = 0
    Public Property VsjdOtsFact As Integer = 0
    Public Property VsjdHoursGoal As Single = 0
    Public Property VsjdHoursFact As Single = 0

    ' ЗАБЖД
    Public Property ZabdOtsGoal As Integer = 0
    Public Property ZabdOtsFact As Integer = 0
    Public Property ZabdHoursGoal As Single = 0
    Public Property ZabdHoursFact As Single = 0

    ' ДВЖД
    Public Property DvdOtsGoal As Integer = 0
    Public Property DvdOtsFact As Integer = 0
    Public Property DvdHoursGoal As Single = 0
    Public Property DvdHoursFact As Single = 0
End Class

Public Class RoadGoalsStore
    Private Shared ReadOnly FileName As String = "RoadGoals.json"

    Public Shared Function Load() As List(Of MonthlyRoadGoals)
        Dim path = System.IO.Path.Combine(My.Settings.SetsFolder, FileName)
        If Not System.IO.File.Exists(path) Then Return New List(Of MonthlyRoadGoals)()
        Try
            Dim json = System.IO.File.ReadAllText(path)
            Return JsonConvert.DeserializeObject(Of List(Of MonthlyRoadGoals))(json)
        Catch ex As Exception
            MessageBox.Show($"Ошибка загрузки {FileName}: {ex.Message}")
            Return New List(Of MonthlyRoadGoals)()
        End Try
    End Function

    Public Shared Sub Save(goals As List(Of MonthlyRoadGoals))
        Dim path = System.IO.Path.Combine(My.Settings.SetsFolder, FileName)
        Try
            Dim json = JsonConvert.SerializeObject(goals, Formatting.Indented)
            System.IO.File.WriteAllText(path, json)
        Catch ex As Exception
            MessageBox.Show($"Ошибка сохранения {FileName}: {ex.Message}")
        End Try
    End Sub
End Class
