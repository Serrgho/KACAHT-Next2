Imports System.ComponentModel
Imports System.IO
Imports System.Runtime.CompilerServices
Imports Newtonsoft.Json

Namespace Kas
    Module GoalHoursStoreModule

        ' =====================================================================
        ' Хранилище целей по часам. Файл лежит рядом с Goals.json.
        ' =====================================================================
        Public Class GoalHoursStore
            Private Shared ReadOnly FilePath As String =
                Path.Combine(My.Settings.SetsFolder, "GoalHours.json")

            ''' <summary>Загружает все записи. Если файла нет — пустой список.</summary>
            Public Shared Function Load() As List(Of MonthlyGoalHours)
                If Not File.Exists(FilePath) Then Return New List(Of MonthlyGoalHours)
                Dim json = File.ReadAllText(FilePath)
                Return JsonConvert.DeserializeObject(Of List(Of MonthlyGoalHours))(json)
            End Function

            ''' <summary>Сохраняет список записей в файл.</summary>
            Public Shared Sub Save(goals As List(Of MonthlyGoalHours))
                Dim json = JsonConvert.SerializeObject(goals, Formatting.Indented)
                File.WriteAllText(FilePath, json)
            End Sub
        End Class

        ' =====================================================================
        ' Модель одной записи: год/месяц + 5 депо × 4 группы = 20 полей (часы).
        ' =====================================================================
        Public Class MonthlyGoalHours
            Implements INotifyPropertyChanged

            Public Event PropertyChanged As PropertyChangedEventHandler _
                Implements INotifyPropertyChanged.PropertyChanged

            Public Property Year As Integer
            Public Property Month As Integer

            ' ===== Поля для хранения значений =====
            ' k=1 Ачинск
            Private _d1Tche As Single, _d1Sld As Single, _d1Zav As Single, _d1Tr As Single
            ' k=2 Боготол
            Private _d2Tche As Single, _d2Sld As Single, _d2Zav As Single, _d2Tr As Single
            ' k=3 Красноярск
            Private _d3Tche As Single, _d3Sld As Single, _d3Zav As Single, _d3Tr As Single
            ' k=5 Иланская
            Private _d5Tche As Single, _d5Sld As Single, _d5Zav As Single, _d5Tr As Single
            ' k=7 Нижнеудинск
            Private _d7Tche As Single, _d7Sld As Single, _d7Zav As Single, _d7Tr As Single

            ' ===== НОВОЕ ПОЛЕ: Суточное пороговое количество =====
            Private _dailyThreshold As Single




            ' ===== НОВОЕ СВОЙСТВО: Суточное пороговое количество =====
            Public Property DailyThreshold As Single
                Get
                    Return _dailyThreshold
                End Get
                Set(v As Single)
                    If _dailyThreshold <> v Then
                        _dailyThreshold = v
                        OnPropertyChanged()
                        ' Намеренно не вызываем OnPropertyChanged для Total, 
                        ' так как это лимит, а не часть суммы потерь.
                    End If
                End Set
            End Property



            ' =====================================================================
            ' Ачинск (k=1)
            ' =====================================================================
            Public Property D1Tche As Single
                Get
                    Return _d1Tche
                End Get
                Set(v As Single)
                    If _d1Tche <> v Then
                        _d1Tche = v
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(D1All))
                        OnPropertyChanged(NameOf(TotalTche))
                        OnPropertyChanged(NameOf(TotalAll))
                    End If
                End Set
            End Property

            Public Property D1Sld As Single
                Get
                    Return _d1Sld
                End Get
                Set(v As Single)
                    If _d1Sld <> v Then
                        _d1Sld = v
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(D1All))
                        OnPropertyChanged(NameOf(TotalSld))
                        OnPropertyChanged(NameOf(TotalAll))
                    End If
                End Set
            End Property

            Public Property D1Zav As Single
                Get
                    Return _d1Zav
                End Get
                Set(v As Single)
                    If _d1Zav <> v Then
                        _d1Zav = v
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(D1All))
                        OnPropertyChanged(NameOf(TotalZav))
                        OnPropertyChanged(NameOf(TotalAll))
                    End If
                End Set
            End Property

            Public Property D1Tr As Single
                Get
                    Return _d1Tr
                End Get
                Set(v As Single)
                    If _d1Tr <> v Then
                        _d1Tr = v
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(D1All))
                        OnPropertyChanged(NameOf(TotalTr))
                        OnPropertyChanged(NameOf(TotalAll))
                    End If
                End Set
            End Property

            ' =====================================================================
            ' Боготол (k=2)
            ' =====================================================================
            Public Property D2Tche As Single
                Get
                    Return _d2Tche
                End Get
                Set(v As Single)
                    If _d2Tche <> v Then
                        _d2Tche = v
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(D2All))
                        OnPropertyChanged(NameOf(TotalTche))
                        OnPropertyChanged(NameOf(TotalAll))
                    End If
                End Set
            End Property

            Public Property D2Sld As Single
                Get
                    Return _d2Sld
                End Get
                Set(v As Single)
                    If _d2Sld <> v Then
                        _d2Sld = v
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(D2All))
                        OnPropertyChanged(NameOf(TotalSld))
                        OnPropertyChanged(NameOf(TotalAll))
                    End If
                End Set
            End Property

            Public Property D2Zav As Single
                Get
                    Return _d2Zav
                End Get
                Set(v As Single)
                    If _d2Zav <> v Then
                        _d2Zav = v
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(D2All))
                        OnPropertyChanged(NameOf(TotalZav))
                        OnPropertyChanged(NameOf(TotalAll))
                    End If
                End Set
            End Property

            Public Property D2Tr As Single
                Get
                    Return _d2Tr
                End Get
                Set(v As Single)
                    If _d2Tr <> v Then
                        _d2Tr = v
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(D2All))
                        OnPropertyChanged(NameOf(TotalTr))
                        OnPropertyChanged(NameOf(TotalAll))
                    End If
                End Set
            End Property

            ' =====================================================================
            ' Красноярск (k=3)
            ' =====================================================================
            Public Property D3Tche As Single
                Get
                    Return _d3Tche
                End Get
                Set(v As Single)
                    If _d3Tche <> v Then
                        _d3Tche = v
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(D3All))
                        OnPropertyChanged(NameOf(TotalTche))
                        OnPropertyChanged(NameOf(TotalAll))
                    End If
                End Set
            End Property

            Public Property D3Sld As Single
                Get
                    Return _d3Sld
                End Get
                Set(v As Single)
                    If _d3Sld <> v Then
                        _d3Sld = v
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(D3All))
                        OnPropertyChanged(NameOf(TotalSld))
                        OnPropertyChanged(NameOf(TotalAll))
                    End If
                End Set
            End Property

            Public Property D3Zav As Single
                Get
                    Return _d3Zav
                End Get
                Set(v As Single)
                    If _d3Zav <> v Then
                        _d3Zav = v
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(D3All))
                        OnPropertyChanged(NameOf(TotalZav))
                        OnPropertyChanged(NameOf(TotalAll))
                    End If
                End Set
            End Property

            Public Property D3Tr As Single
                Get
                    Return _d3Tr
                End Get
                Set(v As Single)
                    If _d3Tr <> v Then
                        _d3Tr = v
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(D3All))
                        OnPropertyChanged(NameOf(TotalTr))
                        OnPropertyChanged(NameOf(TotalAll))
                    End If
                End Set
            End Property

            ' =====================================================================
            ' Иланская (k=5)
            ' =====================================================================
            Public Property D5Tche As Single
                Get
                    Return _d5Tche
                End Get
                Set(v As Single)
                    If _d5Tche <> v Then
                        _d5Tche = v
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(D5All))
                        OnPropertyChanged(NameOf(TotalTche))
                        OnPropertyChanged(NameOf(TotalAll))
                    End If
                End Set
            End Property

            Public Property D5Sld As Single
                Get
                    Return _d5Sld
                End Get
                Set(v As Single)
                    If _d5Sld <> v Then
                        _d5Sld = v
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(D5All))
                        OnPropertyChanged(NameOf(TotalSld))
                        OnPropertyChanged(NameOf(TotalAll))
                    End If
                End Set
            End Property

            Public Property D5Zav As Single
                Get
                    Return _d5Zav
                End Get
                Set(v As Single)
                    If _d5Zav <> v Then
                        _d5Zav = v
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(D5All))
                        OnPropertyChanged(NameOf(TotalZav))
                        OnPropertyChanged(NameOf(TotalAll))
                    End If
                End Set
            End Property

            Public Property D5Tr As Single
                Get
                    Return _d5Tr
                End Get
                Set(v As Single)
                    If _d5Tr <> v Then
                        _d5Tr = v
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(D5All))
                        OnPropertyChanged(NameOf(TotalTr))
                        OnPropertyChanged(NameOf(TotalAll))
                    End If
                End Set
            End Property

            ' =====================================================================
            ' Нижнеудинск (k=7)
            ' =====================================================================
            Public Property D7Tche As Single
                Get
                    Return _d7Tche
                End Get
                Set(v As Single)
                    If _d7Tche <> v Then
                        _d7Tche = v
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(D7All))
                        OnPropertyChanged(NameOf(TotalTche))
                        OnPropertyChanged(NameOf(TotalAll))
                    End If
                End Set
            End Property

            Public Property D7Sld As Single
                Get
                    Return _d7Sld
                End Get
                Set(v As Single)
                    If _d7Sld <> v Then
                        _d7Sld = v
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(D7All))
                        OnPropertyChanged(NameOf(TotalSld))
                        OnPropertyChanged(NameOf(TotalAll))
                    End If
                End Set
            End Property

            Public Property D7Zav As Single
                Get
                    Return _d7Zav
                End Get
                Set(v As Single)
                    If _d7Zav <> v Then
                        _d7Zav = v
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(D7All))
                        OnPropertyChanged(NameOf(TotalZav))
                        OnPropertyChanged(NameOf(TotalAll))
                    End If
                End Set
            End Property

            Public Property D7Tr As Single
                Get
                    Return _d7Tr
                End Get
                Set(v As Single)
                    If _d7Tr <> v Then
                        _d7Tr = v
                        OnPropertyChanged()
                        OnPropertyChanged(NameOf(D7All))
                        OnPropertyChanged(NameOf(TotalTr))
                        OnPropertyChanged(NameOf(TotalAll))
                    End If
                End Set
            End Property

            ' =====================================================================
            ' Вычисляемые: «Всего по депо» (сумма 4 групп)
            ' =====================================================================
            Public ReadOnly Property D1All As Single
                Get
                    Return _d1Tche + _d1Sld + _d1Zav + _d1Tr
                End Get
            End Property

            Public ReadOnly Property D2All As Single
                Get
                    Return _d2Tche + _d2Sld + _d2Zav + _d2Tr
                End Get
            End Property

            Public ReadOnly Property D3All As Single
                Get
                    Return _d3Tche + _d3Sld + _d3Zav + _d3Tr
                End Get
            End Property

            Public ReadOnly Property D5All As Single
                Get
                    Return _d5Tche + _d5Sld + _d5Zav + _d5Tr
                End Get
            End Property

            Public ReadOnly Property D7All As Single
                Get
                    Return _d7Tche + _d7Sld + _d7Zav + _d7Tr
                End Get
            End Property

            ' =====================================================================
            ' Вычисляемые: «Всего по группе» (сумма по всем 5 депо)
            ' =====================================================================
            Public ReadOnly Property TotalTche As Single
                Get
                    Return _d1Tche + _d2Tche + _d3Tche + _d5Tche + _d7Tche
                End Get
            End Property

            Public ReadOnly Property TotalSld As Single
                Get
                    Return _d1Sld + _d2Sld + _d3Sld + _d5Sld + _d7Sld
                End Get
            End Property

            Public ReadOnly Property TotalZav As Single
                Get
                    Return _d1Zav + _d2Zav + _d3Zav + _d5Zav + _d7Zav
                End Get
            End Property

            Public ReadOnly Property TotalTr As Single
                Get
                    Return _d1Tr + _d2Tr + _d3Tr + _d5Tr + _d7Tr
                End Get
            End Property

            Public ReadOnly Property TotalAll As Single
                Get
                    Return TotalTche + TotalSld + TotalZav + TotalTr
                End Get
            End Property

            ' =====================================================================
            ' Механизм уведомлений
            ' =====================================================================
            Private Sub OnPropertyChanged(<CallerMemberName> Optional propertyName As String = Nothing)
                RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
            End Sub
        End Class

    End Module
End Namespace