Imports System.Text
Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.IO
Imports System.Linq

Imports System.Windows.Forms

Namespace Kas
    Module PripLokModule


        Friend ReadOnly _pripisLokFileName As String = "user_pripis_lok.txt"

        Public Ist As New List(Of String)({"КРАС", "ВСЖД", "ЗАБЖД", "ДВЖД", "ЗСЖД", "ПРИВЖД", "СКАВЖД", "ЮУРЖД", "ОКТЖД", "КЛНГЖД", "ГОРЖД", "СЕВЖД", "ЮВЖД", "КБШЖД", "СВРДЖД", "САХЖД"})


        Friend _pripisLokList As List(Of String)

        ' Базовые приписки 
        Public ReadOnly _PPZHTPripisLokList As New List(Of String) From {
        "АО СУЭК",
        "Крас ДИ",
        "МВД Красноярск",
        "ППЖТ",
        "СП Партизанская ГРЭС"
        }

        Public ReadOnly _KrasPripisLokList As New List(Of String) From {
        "ТЧЭ Абакан",
        "ТЧЭ Ачинск",
        "ТЧЭ Боготол",
        "ТЧЭ Иланская",
        "ТЧЭ Красноярск"
        }

        Public ReadOnly _VSIBPripisLokList As New List(Of String) From {
        "ТЧЭ Вихоревка",
        "ТЧЭ Иркутск",
        "ТЧЭ Нижнеудинск",
        "ТЧЭ Северобайкальск",
        "ТЧЭ Улан-Удэ",
        "ТЧЭ Тайшет"
        }

        Public ReadOnly _ZABPripisLokList As New List(Of String) From {
        "ТЧЭ Белогорск",
        "ТЧЭ Петров Вал",
        "ТЧЭ Чита"
        }

        Public ReadOnly _DVPripisLokList As New List(Of String) From {
        "ТЧЭ Н-Ургал",
        "ТЧЭ Смоляниново",
        "ТЧЭ Партизанск",
        "ТЧЭ Хабаровск"
        }

        Public ReadOnly _ZSIBPripisLokList As New List(Of String) From {
        "ТЧЭ Барабинск",
        "ТЧЭ Карасук",
        "ТЧЭ Омск",
        "ТЧЭ Тайга"
        }

        Public ReadOnly _PRIVPripisLokList As New List(Of String) From {
        "ТЧЭ Батайск",
        "ТЧЭ Белово",
        "ТЧЭ Тимашевская"
        }

        Public ReadOnly _KUBSHPripisLokList As New List(Of String) From {
        "ТЧЭ Красноуфимск",
        "ТЧЭ Ярославль"
        }
        Public ReadOnly _GORHPripisLokList As New List(Of String) From {
        "ТЧЭ Горький"
        }
        Public ReadOnly _SEVPripisLokList As New List(Of String) From {
        "ТЧЭ Лянгасово"
        }
        ' ====== СЛОВАРЬ: дорога -> приписки ======
        Private _roadToPripis As Dictionary(Of String, List(Of String)) = Nothing

        Public Property RoadToPripis As Dictionary(Of String, List(Of String))
            Get
                If _roadToPripis Is Nothing Then
                    _roadToPripis = New Dictionary(Of String, List(Of String)) From {
                {"ППЖТ", _PPZHTPripisLokList},
                {"КРАС", _KrasPripisLokList},
                {"ВСЖД", _VSIBPripisLokList},
                {"ЗАБЖД", _ZABPripisLokList},
                {"ДВЖД", _DVPripisLokList},
                {"ЗСЖД", _ZSIBPripisLokList},
                {"ПРИВЖД", _PRIVPripisLokList},
                {"КБШЖД", _KUBSHPripisLokList},
                {"СЕВЖД", _SEVPripisLokList},
                {"ГОРЖД", _GORHPripisLokList}
            }
                End If
                Return _roadToPripis
            End Get
            Set(value As Dictionary(Of String, List(Of String)))
                _roadToPripis = value
            End Set
        End Property

        ' ====== СПИСОК ДОРОГ ДЛЯ TABCONTROL ======
        Public ReadOnly Property AllRoads As List(Of String)
            Get
                Return New List(Of String) From {
                     "КРАС", "ВСЖД", "ЗАБЖД", "ДВЖД",
                    "ЗСЖД", "ППЖТ", "ПРИВЖД", "КБШЖД", "ГОРЖД", "СЕВЖД"
                }
            End Get
        End Property

        ' ====== ВСЕ ПРИПИСКИ (для поиска/фильтра) ======
        Public ReadOnly Property AllPripis As List(Of String)
            Get
                Dim all As New List(Of String)

                ' Базовые — из всех дорог
                For Each road In AllRoads
                    If RoadToPripis.ContainsKey(road) Then
                        all.AddRange(RoadToPripis(road))
                    End If
                Next

                ' Пользовательские — из файла (см. ниже)
                all.AddRange(LoadUserPripis())

                Return all.Distinct().OrderBy(Function(s) s).ToList()
            End Get
        End Property


        ' ====== РАБОТА С ФАЙЛОМ ======

        Private Function GetPripisFilePath() As String
            Dim dataFolder = GetOrCreateDataFolderPath(ForParams:=True)
            If String.IsNullOrEmpty(dataFolder) Then Return Nothing
            Return IO.Path.Combine(dataFolder, _pripisLokFileName)
        End Function

        Public Function LoadUserPripis() As List(Of String)
            Dim list As New List(Of String)
            Dim path = GetPripisFilePath()
            If IO.File.Exists(path) Then
                Try
                    list.AddRange(IO.File.ReadAllLines(path, Text.Encoding.UTF8).
                          Select(Function(s) s.Trim()).
                          Where(Function(s) Not String.IsNullOrWhiteSpace(s)))
                Catch ex As Exception
                    Debug.WriteLine($"Ошибка загрузки: {ex.Message}")
                End Try
            End If
            Return list
        End Function

        'Public Sub SavePripisToFile()
        '    Dim path = GetPripisFilePath()
        '    Try
        '        ' Просто пишем одну строку — без условий
        '        IO.File.WriteAllText(path, "ТЕСТ_123" & vbCrLf & "ЕЩЁ_ТЕСТ", Text.Encoding.UTF8)
        '        Debug.WriteLine($"✅ ФАЙЛ ЗАПИСАН: {path}")
        '    Catch ex As Exception
        '        Debug.WriteLine($"❌ ОШИБКА: {ex.Message}")
        '    End Try
        'End Sub

        Private Function IsBasePripis(pripis As String) As Boolean
            For Each road In AllRoads
                If RoadToPripis.ContainsKey(road) AndAlso RoadToPripis(road).Contains(pripis) Then
                    Return True
                End If
            Next
            Return False
        End Function

        ' ====== ДОБАВЛЕНИЕ / УДАЛЕНИЕ ======

        Public Sub AddPripis(pripis As String)
            Dim s = pripis.Trim()
            If String.IsNullOrWhiteSpace(s) OrElse AllPripis.Contains(s) Then Return

            Dim path = GetPripisFilePath()
            If path Is Nothing Then Return

            Try
                ' Просто дописываем — без чтения
                Using sw As New StreamWriter(path, True, Encoding.UTF8)
                    sw.WriteLine(s)
                End Using
                Debug.WriteLine($"✅ Записано: '{s}'")
            Catch ex As Exception
                Debug.WriteLine($"❌ {ex.Message}")
            End Try
        End Sub

        Public Function RemovePripis(pripis As String) As Boolean
            Dim s = pripis.Trim()
            If String.IsNullOrWhiteSpace(s) Then Return False

            Dim removedFromDict As Boolean = False
            Dim roadFound As String = Nothing

            ' 1. Удаляем из RoadToPripis (во всех дорогах)
            For Each kvp In RoadToPripis.ToList()
                Dim road = kvp.Key
                Dim list = kvp.Value
                If list.Contains(s) Then
                    list.Remove(s)
                    removedFromDict = True
                    roadFound = road
                    Exit For
                End If
            Next

            ' 2. Удаляем из файла
            Dim path = GetPripisFilePath()
            If path IsNot Nothing AndAlso File.Exists(path) Then
                Try
                    Dim lines = File.ReadAllLines(path, Encoding.UTF8).ToList()
                    Dim lineToRemove = lines.FirstOrDefault(Function(line) line.Trim() = s)
                    If lineToRemove IsNot Nothing AndAlso lines.Remove(lineToRemove) Then
                        File.WriteAllLines(path, lines, Encoding.UTF8)
                    End If
                Catch ex As Exception
                    Debug.WriteLine($"Ошибка удаления из файла: {ex.Message}")
                End Try
            End If

            Return removedFromDict
        End Function


    End Module
End Namespace



