Imports System.ComponentModel

Namespace Kas
    Public Class YarlykSelectionService
        Implements INotifyPropertyChanged

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Public ReadOnly Property PointedYarlyk As Yarlyk
            Get
                Return YarlykControlModule.PointedYarlyk
            End Get
        End Property

        Public ReadOnly Property PointedHeaderText As String
            Get
                Dim y = YarlykControlModule.PointedYarlyk
                Return If(y?.HeaderText, "")
            End Get
        End Property

        Public Sub New()
            AddHandler YarlykControlModule.PointedYarlykChanged, Sub()
                                                                     RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(NameOf(PointedYarlyk)))
                                                                     RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(NameOf(PointedHeaderText)))
                                                                 End Sub
        End Sub
    End Class
End Namespace

