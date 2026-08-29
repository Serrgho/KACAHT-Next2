Imports System.ComponentModel
Imports System.Runtime.CompilerServices
Imports Newtonsoft.Json

Public Class MonthlyRoadGoals
    Implements INotifyPropertyChanged

    Public Event PropertyChanged As PropertyChangedEventHandler _
                Implements INotifyPropertyChanged.PropertyChanged

    Public Property Year As Integer
    Public Property Month As Integer

    ' ===== Поля =====
    Private _vsjdOtsGoal As Integer, _vsjdOtsFact As Integer
    Private _vsjdHoursGoal As Single, _vsjdHoursFact As Single

    Private _zabdOtsGoal As Integer, _zabdOtsFact As Integer
    Private _zabdHoursGoal As Single, _zabdHoursFact As Single

    Private _dvdOtsGoal As Integer, _dvdOtsFact As Integer
    Private _dvdHoursGoal As Single, _dvdHoursFact As Single

    ' ===== ВСЖД =====
    Public Property VsjdOtsGoal As Integer
        Get
            Return _vsjdOtsGoal
        End Get
        Set(v As Integer)
            If _vsjdOtsGoal <> v Then
                _vsjdOtsGoal = v
                OnPropertyChanged()
            End If
        End Set
    End Property

    Public Property VsjdOtsFact As Integer
        Get
            Return _vsjdOtsFact
        End Get
        Set(v As Integer)
            If _vsjdOtsFact <> v Then
                _vsjdOtsFact = v
                OnPropertyChanged()
            End If
        End Set
    End Property

    Public Property VsjdHoursGoal As Single
        Get
            Return _vsjdHoursGoal
        End Get
        Set(v As Single)
            If _vsjdHoursGoal <> v Then
                _vsjdHoursGoal = v
                OnPropertyChanged()
            End If
        End Set
    End Property

    Public Property VsjdHoursFact As Single
        Get
            Return _vsjdHoursFact
        End Get
        Set(v As Single)
            If _vsjdHoursFact <> v Then
                _vsjdHoursFact = v
                OnPropertyChanged()
            End If
        End Set
    End Property

    ' ===== ЗАБЖД =====
    Public Property ZabdOtsGoal As Integer
        Get
            Return _zabdOtsGoal
        End Get
        Set(v As Integer)
            If _zabdOtsGoal <> v Then
                _zabdOtsGoal = v
                OnPropertyChanged()
            End If
        End Set
    End Property

    Public Property ZabdOtsFact As Integer
        Get
            Return _zabdOtsFact
        End Get
        Set(v As Integer)
            If _zabdOtsFact <> v Then
                _zabdOtsFact = v
                OnPropertyChanged()
            End If
        End Set
    End Property

    Public Property ZabdHoursGoal As Single
        Get
            Return _zabdHoursGoal
        End Get
        Set(v As Single)
            If _zabdHoursGoal <> v Then
                _zabdHoursGoal = v
                OnPropertyChanged()
            End If
        End Set
    End Property

    Public Property ZabdHoursFact As Single
        Get
            Return _zabdHoursFact
        End Get
        Set(v As Single)
            If _zabdHoursFact <> v Then
                _zabdHoursFact = v
                OnPropertyChanged()
            End If
        End Set
    End Property

    ' ===== ДВЖД =====
    Public Property DvdOtsGoal As Integer
        Get
            Return _dvdOtsGoal
        End Get
        Set(v As Integer)
            If _dvdOtsGoal <> v Then
                _dvdOtsGoal = v
                OnPropertyChanged()
            End If
        End Set
    End Property

    Public Property DvdOtsFact As Integer
        Get
            Return _dvdOtsFact
        End Get
        Set(v As Integer)
            If _dvdOtsFact <> v Then
                _dvdOtsFact = v
                OnPropertyChanged()
            End If
        End Set
    End Property

    Public Property DvdHoursGoal As Single
        Get
            Return _dvdHoursGoal
        End Get
        Set(v As Single)
            If _dvdHoursGoal <> v Then
                _dvdHoursGoal = v
                OnPropertyChanged()
            End If
        End Set
    End Property

    Public Property DvdHoursFact As Single
        Get
            Return _dvdHoursFact
        End Get
        Set(v As Single)
            If _dvdHoursFact <> v Then
                _dvdHoursFact = v
                OnPropertyChanged()
            End If
        End Set
    End Property

    ' ===== Механизм уведомлений =====
    Private Sub OnPropertyChanged(<CallerMemberName> Optional propertyName As String = Nothing)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub
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
