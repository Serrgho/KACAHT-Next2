Imports System.ComponentModel
Imports System.Globalization
Imports OfficeOpenXml.Drawing

Namespace Kas
    Partial Public Class Yarlyk
        Implements INotifyPropertyChanged

        Private _HeaderText As String = "---"
        Private _KolOTS As Integer = 0
        Private _KolPCh As Single = 0
        Private _Pointed As Boolean = False
        Private _Zapros As Func(Of Otkaz, Boolean)
        Private _ForeKolor As Brush
        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
        Protected Sub MyPropertyChanged(ByVal propertyName As String)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
        End Sub

        Public Property Pointed As Boolean
            Get
                Return _Pointed
            End Get
            Set
                _Pointed = Value
                MyPropertyChanged(NameOf(Pointed))
                'If Value Then
                '    ForeKolor = Brushes.Red
                'Else
                '    ForeKolor = Brushes.Black
                'End If
            End Set
        End Property

        'Public Property ForeKolor As Brush
        '    Get
        '        Return _ForeKolor
        '    End Get
        '    Set
        '        ' Проверяем, изменилось ли значение
        '        If _ForeKolor Is Nothing OrElse Not _ForeKolor.Equals(Value) Then
        '            _ForeKolor = Value
        '            ' Уведомляем об изменении свойства
        '            MyPropertyChanged(NameOf(ForeKolor))
        '        End If
        '    End Set
        'End Property

        Public Property HeaderText As String
            Get
                Return _HeaderText
            End Get
            Set
                If Value <> _HeaderText Then
                    _HeaderText = Value
                    MyPropertyChanged(NameOf(HeaderText))
                End If

            End Set
        End Property


        Public Property KolOTS As Integer
            Get
                Return _KolOTS
            End Get
            Set
                If Value <> _KolOTS Then
                    _KolOTS = Value
                    MyPropertyChanged(NameOf(KolOTS))
                End If

            End Set
        End Property

        Public Property KolPCh As Single
            Get
                Return _KolPCh
            End Get
            Set
                If Value <> _KolPCh Then
                    _KolPCh = Value
                    MyPropertyChanged(NameOf(KolPCh))
                End If

            End Set
        End Property

        Public Property Zapros As Func(Of Otkaz, Boolean)
            Get
                Return _Zapros
            End Get
            Set
                _Zapros = Value
                MyPropertyChanged(NameOf(Zapros))
                KolOTS = GetFilteredOTSList(Value).Count

                If Name = "KorrP" Then
                    KolPCh = GetFilteredOTSList(Value).Sum(Function(j) j.KorPCH)
                Else
                    KolPCh = GetFilteredOTSList(Value).Sum(Function(j) j.PCh)
                End If

            End Set
        End Property

        Function GetFilteredOTSList(filter As Func(Of Otkaz, Boolean)) As IEnumerable(Of Otkaz)
            Return OTSList.Where(filter)
        End Function
        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().
            DataContext = Me
            'ForeKolor = Brushes.Black
        End Sub

    End Class
End Namespace


