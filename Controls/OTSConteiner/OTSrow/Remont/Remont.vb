Imports System.ComponentModel
Imports System.Globalization
Imports System.Text.RegularExpressions

Namespace Kas

    ''' <summary>
    ''' Данные о ремонте локомотива
    ''' </summary>
    Public Class Remont


        Implements INotifyPropertyChanged

        Private _repairType As String = String.Empty
        ''' <summary>
        ''' Вид ремонта (ТР-1, ТО-2, КР и т.д.)
        ''' </summary>
        Public Property RepairType As String
            Get
                Return _repairType
            End Get
            Set(value As String)
                If _repairType <> value Then
                    _repairType = value
                    OnPropertyChanged(NameOf(RepairType))
                    OnPropertyChanged(NameOf(IsMajorRepair))
                    OnPropertyChanged(NameOf(IsTO2))
                End If
            End Set
        End Property

        ''' <summary>Место проведения ремонта</summary>
        Private _repairPlace As String = String.Empty
        Public Property RepairPlace As String
            Get
                Return _repairPlace
            End Get
            Set(value As String)
                If String.IsNullOrWhiteSpace(value) Then
                    _repairPlace = String.Empty
                    Return
                End If

                Dim cleaned = value.Trim()
                ' Только мусор по краям: точки, запятые, двоеточия, тире
                cleaned = Regex.Replace(cleaned, "^[.,;:\-\(\)\[\]\s]+|[.,;:\-\(\)\[\]\s]+$", "", RegexOptions.IgnoreCase)
                ' Нормализуем пробелы
                cleaned = Regex.Replace(cleaned, "\s+", " ").Trim()
                _repairPlace = cleaned
                OnPropertyChanged(NameOf(RepairPlace))
            End Set
        End Property

        ''' <summary>Пробег на момент ремонта</summary>
        Private _mileage As Integer?
        Public Property Mileage As Integer?
            Get
                Return _mileage
            End Get
            Set(value As Integer?)
                If Not Nullable.Equals(_mileage, value) Then
                    _mileage = value
                    OnPropertyChanged(NameOf(Mileage))
                End If
            End Set
        End Property

        ''' <summary>
        ''' Признак большого ремонта (ПОСТР, КР, СР, ТР-3, ТР-2, ТР-1)
        ''' </summary>
        Public ReadOnly Property IsMajorRepair As Boolean
            Get
                Dim typeUpper As String = Me.RepairType?.ToUpper()?.Trim()
                Select Case typeUpper
                    Case "ПОСТР", "КР", "СР", "ТР-3", "ТР-2", "ТР-1"
                        Return True
                    Case Else
                        Return False
                End Select
            End Get
        End Property

        ''' <summary>
        ''' Признак ТО-2
        ''' </summary>
        Public ReadOnly Property IsTO2 As Boolean
            Get
                Return Me.RepairType?.ToUpper()?.Trim() = "ТО-2"
            End Get
        End Property

        Public Overrides Function ToString() As String
            Dim mileageStr = If(Me.Mileage.HasValue, $" {Me.Mileage.Value} км", "")
            Return $"{Me.RepairType}{mileageStr} {Me.RepairPlace}".Trim()
        End Function

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Protected Sub OnPropertyChanged(propertyName As String)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
        End Sub






        'Public Property RepairType As String = String.Empty

        '''' <summary>
        '''' Вид ремонта (ТР-1, ТО-2, КР и т.д.)
        '''' </summary>
        'Private _repairPlace As String = String.Empty
        '''' <summary>Место проведения ремонта</summary>
        'Public Property RepairPlace As String
        '    Get
        '        Return _repairPlace
        '    End Get
        '    Set(value As String)
        '        If String.IsNullOrWhiteSpace(value) Then
        '            _repairPlace = String.Empty
        '            Return
        '        End If

        '        Dim cleaned = value.Trim()
        '        ' Только мусор по краям: точки, запятые, двоеточия, тире
        '        cleaned = Regex.Replace(cleaned, "^[.,;:\-\(\)\[\]\s]+|[.,;:\-\(\)\[\]\s]+$", "", RegexOptions.IgnoreCase)
        '        ' Нормализуем пробелы
        '        cleaned = Regex.Replace(cleaned, "\s+", " ").Trim()
        '        _repairPlace = cleaned
        '    End Set
        'End Property

        'Public Property Mileage As Integer?

        'Public Overrides Function ToString() As String
        '    Dim mileageStr = If(Mileage.HasValue, $" {Mileage.Value} км", "")
        '    Return $"{RepairType}{mileageStr} {RepairPlace}".Trim()
        'End Function
    End Class


End Namespace


