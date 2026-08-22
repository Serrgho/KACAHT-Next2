
Imports System.ComponentModel

Namespace Kas

    Public Class HistoryEntry
        Implements INotifyPropertyChanged

        Private _eventDate As DateTime = DateTime.Now
        Private _description As String = String.Empty
        Private _showDate As Boolean = True ' ← новое свойство
        Private _isHighlighted As Boolean = False
        Private _IsMoused As Boolean = False
        Private _IsRed As Boolean = False



        Public Property IsHighlighted As Boolean
            Get
                Return _isHighlighted
            End Get
            Set(value As Boolean)
                If _isHighlighted <> value Then
                    _isHighlighted = value
                    OnPropertyChanged(NameOf(IsHighlighted))
                End If
            End Set
        End Property

        Public Property IsMoused As Boolean
            Get
                Return _IsMoused
            End Get
            Set(value As Boolean)
                If _IsMoused <> value Then
                    _IsMoused = value
                    OnPropertyChanged(NameOf(IsMoused))
                End If
            End Set
        End Property

        Public Property EventDate As DateTime
            Get
                Return _eventDate
            End Get
            Set(value As DateTime)
                If _eventDate <> value Then
                    _eventDate = value
                    OnPropertyChanged(NameOf(EventDate))
                    OnPropertyChanged(NameOf(DisplayText))
                End If
            End Set
        End Property

        Public Property Description As String
            Get
                Return _description
            End Get
            Set(value As String)
                If _description <> value Then
                    _description = value
                    OnPropertyChanged(NameOf(Description))
                    OnPropertyChanged(NameOf(DisplayText))
                    ' ✅ АВТО-РАСЧЁТ при любом изменении описания
                    Dim shouldBeRed As Boolean = Not String.IsNullOrEmpty(_description) AndAlso _description.Contains("казание пом")
                    If _IsRed <> shouldBeRed Then
                        _IsRed = shouldBeRed
                        OnPropertyChanged(NameOf(IsRed)) ' ← UI мгновенно обновится
                    End If
                End If
            End Set
        End Property

        Public ReadOnly Property DisplayText As String
            Get
                If ShowDate Then
                    Return $"{EventDate:dd.MM.yyyy}  {Description}"
                Else
                    Return $"{Description}"
                End If


            End Get
        End Property

        Public Property ShowDate As Boolean
            Get
                Return _showDate
            End Get
            Set(value As Boolean)
                If _showDate <> value Then
                    _showDate = value
                    OnPropertyChanged(NameOf(ShowDate))
                End If
            End Set
        End Property

        Public Property IsRed As Boolean
            Get
                Return _IsRed
            End Get
            Set(value As Boolean)

                If _IsRed <> value Then
                    _IsRed = value
                    OnPropertyChanged(NameOf(IsRed))
                End If
            End Set
        End Property

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
        Protected Sub OnPropertyChanged(<Runtime.CompilerServices.CallerMemberName> Optional propertyName As String = Nothing)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
        End Sub
    End Class
End Namespace


