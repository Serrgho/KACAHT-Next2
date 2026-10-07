Imports System.ComponentModel
Imports System.Globalization
Imports OfficeOpenXml.Drawing

Namespace Kas
    Partial Public Class Yarlyk

        Implements INotifyPropertyChanged

        Private _headerText As String = "---"
        Private _kolOts As Integer = 0
        Private _kolPCh As Double = 0
        Private _pointed As Boolean = False
        Private _zapros As Func(Of Otkaz, Boolean)

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Protected Sub MyPropertyChanged(ByVal propertyName As String)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
        End Sub

        Public Property Pointed As Boolean
            Get
                Return _pointed
            End Get
            Set(value As Boolean)
                If _pointed <> value Then
                    _pointed = value
                    MyPropertyChanged(NameOf(Pointed))
                End If
            End Set
        End Property

        Public Property HeaderText As String
            Get
                Return _headerText
            End Get
            Set(value As String)
                If value <> _headerText Then
                    _headerText = value
                    MyPropertyChanged(NameOf(HeaderText))
                End If
            End Set
        End Property

        Public Property KolOts As Integer
            Get
                Return _kolOts
            End Get
            Set(value As Integer)
                If value <> _kolOts Then
                    _kolOts = value
                    MyPropertyChanged(NameOf(KolOts))
                End If
            End Set
        End Property

        Public Property KolPCh As Double
            Get
                Return _kolPCh
            End Get
            Set(value As Double)
                If Math.Abs(value - _kolPCh) > 0.001 Then
                    _kolPCh = value
                    MyPropertyChanged(NameOf(KolPCh))
                End If
            End Set
        End Property

        Public Property Zapros As Func(Of Otkaz, Boolean)
            Get
                Return _zapros
            End Get
            Set(value As Func(Of Otkaz, Boolean))
                _zapros = value
                ' Мы НЕ вызываем расчет здесь, чтобы не тормозить при назначении
            End Set
        End Property

        ''' <summary>
        ''' Выполняет пересчет значений на основе текущего фильтра Zapros.
        ''' Вызывайте этот метод после загрузки данных в OTSList.
        ''' </summary>
        Public Sub Recalculate()
            If _zapros Is Nothing OrElse OTSList Is Nothing Then
                KolOts = 0
                KolPCh = 0
                Return
            End If

            Dim count As Integer = 0
            Dim sumHours As Double = 0

            ' Проходим по списку один раз
            For Each item In OTSList
                If _zapros(item) Then
                    count += 1
                    If Name = "KorrP" Then
                        sumHours += item.KorPCH
                    Else
                        sumHours += item.PCh
                    End If
                End If
            Next

            KolOts = count
            KolPCh = sumHours
        End Sub

        Sub New()
            InitializeComponent()
            DataContext = Me
        End Sub



    End Class
End Namespace


