
Imports System.ComponentModel
Imports System.Globalization
Imports System.IO
Imports System.Runtime.CompilerServices
Imports Newtonsoft.Json

Namespace Kas
    Module GoalsStoreModule

        Public Class GoalsStore
            ' Файл будет лежать в папке с программой
            Private Shared ReadOnly FilePath As String = Path.Combine(My.Settings.SetsFolder, "Goals.json")

            ''' <summary>
            ''' Загружает все цели из файла. Если файла нет - возвращает пустой список.
            ''' </summary>
            Public Shared Function Load() As List(Of MonthlyGoal)
                If Not File.Exists(FilePath) Then Return New List(Of MonthlyGoal)

                Dim json = File.ReadAllText(FilePath)
                Return JsonConvert.DeserializeObject(Of List(Of MonthlyGoal))(json)
            End Function

            ''' <summary>
            ''' Сохраняет список целей в файл (понадобится для будущего редактора)
            ''' </summary>
            Public Shared Sub Save(goals As List(Of MonthlyGoal))
                Dim json = JsonConvert.SerializeObject(goals, Formatting.Indented)
                File.WriteAllText(FilePath, json)
            End Sub
        End Class

        Public Class MonthlyGoal
            Implements INotifyPropertyChanged

            Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

            ' ===== ПОЛЯ ДЛЯ ХРАНЕНИЯ ЗНАЧЕНИЙ =====
            Private _t12 As Integer
            Private _t3 As Integer
            Private _tr12 As Integer
            Private _tr3 As Integer
            Private _sld12 As Integer
            Private _sld3 As Integer
            Private _factory12 As Integer
            Private _factory3 As Integer

            Public Property Year As Integer
            Public Property Month As Integer

            ' ===== СВОЙСТВА С УВЕДОМЛЕНИЯМИ =====

            Public Property T12 As Integer
                Get
                    Return _t12
                End Get
                Set(value As Integer)
                    If _t12 <> value Then
                        _t12 = value
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(T13))
                        OnPropertyChanged(NameOf(Complex12))
                        OnPropertyChanged(NameOf(Complex13))
                    End If
                End Set
            End Property

            Public Property T3 As Integer
                Get
                    Return _t3
                End Get
                Set(value As Integer)
                    If _t3 <> value Then
                        _t3 = value
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(T13))
                        OnPropertyChanged(NameOf(Complex3))
                        OnPropertyChanged(NameOf(Complex13))
                    End If
                End Set
            End Property

            Public Property TR12 As Integer
                Get
                    Return _tr12
                End Get
                Set(value As Integer)
                    If _tr12 <> value Then
                        _tr12 = value
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(TR13))
                        OnPropertyChanged(NameOf(Complex12))
                        OnPropertyChanged(NameOf(Complex13))
                    End If
                End Set
            End Property

            Public Property TR3 As Integer
                Get
                    Return _tr3
                End Get
                Set(value As Integer)
                    If _tr3 <> value Then
                        _tr3 = value
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(TR13))
                        OnPropertyChanged(NameOf(Complex3))
                        OnPropertyChanged(NameOf(Complex13))
                    End If
                End Set
            End Property

            Public Property SLD12 As Integer
                Get
                    Return _sld12
                End Get
                Set(value As Integer)
                    If _sld12 <> value Then
                        _sld12 = value
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(SLD13))
                        OnPropertyChanged(NameOf(Complex12))
                        OnPropertyChanged(NameOf(Complex13))
                    End If
                End Set
            End Property

            Public Property SLD3 As Integer
                Get
                    Return _sld3
                End Get
                Set(value As Integer)
                    If _sld3 <> value Then
                        _sld3 = value
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(SLD13))
                        OnPropertyChanged(NameOf(Complex3))
                        OnPropertyChanged(NameOf(Complex13))
                    End If
                End Set
            End Property

            Public Property Factory12 As Integer
                Get
                    Return _factory12
                End Get
                Set(value As Integer)
                    If _factory12 <> value Then
                        _factory12 = value
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(Factory13))
                        OnPropertyChanged(NameOf(Complex12))
                        OnPropertyChanged(NameOf(Complex13))
                    End If
                End Set
            End Property

            Public Property Factory3 As Integer
                Get
                    Return _factory3
                End Get
                Set(value As Integer)
                    If _factory3 <> value Then
                        _factory3 = value
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(Factory13))
                        OnPropertyChanged(NameOf(Complex3))
                        OnPropertyChanged(NameOf(Complex13))
                    End If
                End Set
            End Property

            ' ===== ВЫЧИСЛЯЕМЫЕ СВОЙСТВА (ReadOnly) =====

            Public ReadOnly Property Complex12 As Integer
                Get
                    Return T12 + TR12 + SLD12 + Factory12
                End Get
            End Property

            Public ReadOnly Property Complex3 As Integer
                Get
                    Return T3 + TR3 + SLD3 + Factory3
                End Get
            End Property

            Public ReadOnly Property Complex13 As Integer
                Get
                    Return Complex12 + Complex3
                End Get
            End Property

            Public ReadOnly Property T13 As Integer
                Get
                    Return T12 + T3
                End Get
            End Property

            Public ReadOnly Property TR13 As Integer
                Get
                    Return TR12 + TR3
                End Get
            End Property

            Public ReadOnly Property SLD13 As Integer
                Get
                    Return SLD12 + SLD3
                End Get
            End Property

            Public ReadOnly Property Factory13 As Integer
                Get
                    Return Factory12 + Factory3
                End Get
            End Property

            ' ===== МЕХАНИЗМ УВЕДОМЛЕНИЯ =====
            Private Sub OnPropertyChanged(<CallerMemberName> Optional propertyName As String = Nothing)
                RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
            End Sub
        End Class





    End Module

    Public Class ZeroToEmptyConverter
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            If value Is Nothing Then Return ""
            If Integer.TryParse(value.ToString(), Nothing) Then
                Dim num = CInt(value)
                If num = 0 Then Return ""
            End If
            Return value.ToString()
        End Function

        Public Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            If String.IsNullOrWhiteSpace(value?.ToString()) Then Return 0
            Dim result As Integer
            If Integer.TryParse(value.ToString(), result) Then
                Return result
            End If
            Return 0
        End Function
    End Class


End Namespace

