Imports System.Collections.ObjectModel
Imports System.Collections.Specialized
Imports System.ComponentModel
Imports System.Globalization
Imports System.Runtime.CompilerServices
Imports System.Text.RegularExpressions
Imports System.Windows.Controls.Primitives
Imports System.Xml.Serialization
Imports Microsoft.Office.Interop.Excel
Imports Microsoft.Office.Interop.Word
Imports Newtonsoft.Json
Imports Windows.Win32.System

Namespace Kas

    Public Class Otkaz
        Implements INotifyPropertyChanged



#Region "Объявление Загадочного свойства (все такие будут)"

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
        Protected Sub OnPropertyChanged(ByVal propertyName As String)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
        End Sub


#End Region

#Region "Локальное объявление свойств"

        Private _marked As Boolean = False
        Private _Pointed As Boolean = False
        Private _NarushSroka As Boolean = False
        Private _IsStation As Boolean
        Private _ISDanger As Boolean = False
        Private _ISKorp As Boolean = False
        Private _ISSobyt As Boolean = False
        Private _OkaPom As Boolean = False

        Private _Nach As Date
        Private _Postup As Date
        Private _Peredan As Date
        Private _Zakryt As Date
        Private _Sozdan As Date

        Private _DateFontWeightStyle As FontWeight

        Private _Id As String
        Private _NewId As String

        Private _Istochnik As String

        Private _Kat As Integer = 0
        Private _PCh As Single? = Nothing  ' Nothing = ещё не установлено

        Private _KorPCH As Single = 0

        Private _GruzKol As Integer = 0
        Private _PrigKol As Integer = 0
        Private _PasKol As Integer = 0

        Private _PasPCH As Single = 0
        Private _PrigPCH As Single = 0
        Private _GruzPCH As Single = 0

        Private _Dlit As Single = 0

        Private _MestoOTS As String
        Private _MestoOTS_TXT As String
        Private _MestoOTS_Dor As String


        Private _SerLokExact As String
        Private _NumLok As String
        Private _PripLok As String
        Private _VidT As String

        Private _Mash As String
        Private _PripMash As String

        Private _KtoZakryl As String
        Private _ZaKem As String
        Private _AlienSLD As String = ""

        Public _Opis As String

        Private _HarPrichin As String
        Private _MyKlasLev1 As String
        Private _MyKlasLev2 As String
        Private _MyKlasLev3 As String
        Private _OTSLev1 As String
        Private _OTSLev2 As String
        Private _OTSLev3 As String

#End Region





#Region "Свойства, требующие чтения из файла"

        ''' <summary>
        ''' (2) номер ОТС
        ''' </summary>
        <Description("Номер ОТС")>
        Public Property Id As String
            Get
                Return _Id
            End Get
            Set
                If _Id <> Value Then
                    _Id = Value
                    OnPropertyChanged(NameOf(Id))
                End If
            End Set
        End Property


        ''' <summary>
        ''' (2) новый номер ОТС (если был изменен на др дорогах)
        ''' </summary>
        <Description("Новый Номер ОТС")>
        Public Property NewId As String
            Get
                Return _NewId
            End Get
            Set
                If _NewId <> Value Then
                    _NewId = Value
                    OnPropertyChanged(NameOf(NewId))
                End If
            End Set
        End Property


        ''' <summary>
        ''' (3) Категория ОТС
        ''' </summary>
        <Description("Категория ОТС")>
        Public Property Kat As Integer
            Get
                Return _Kat
            End Get
            Set
                If _Kat <> Value Then
                    History.Add(New HistoryEntry With {.ShowDate = False, .EventDate = Now, .Description = $"Изменена категория с {_Kat} на {Value}"})
                    _Kat = Value
                    OnPropertyChanged(NameOf(Kat))
                    If Kat = 3 Then
                        PCh = 0
                    End If
                End If
            End Set
        End Property

        ''' <summary>
        ''' (4) Дата/время начала ОТС
        ''' </summary>
        <Description("Дата/время начала ОТС")>
        Public Property Nach As Date
            Get
                Return _Nach
            End Get
            Set
                If _Nach <> Value Then
                    _Nach = Value
                    OnPropertyChanged(NameOf(Nach))
                End If
            End Set
        End Property

        <JsonIgnore>
        Public ReadOnly Property NachFormatted As String
            Get
                Dim d As DateTime = CType(Nach, DateTime)
                Return d.ToString("dd.MM.yyyy HH:mm")

            End Get
        End Property


        ''' <summary>
        ''' (6) От кого поступил ОТС
        ''' </summary>
        <Description("От кого поступил ОТС")>
        Public Property Istochnik As String
            Get
                Return _Istochnik
            End Get
            Set
                If _Istochnik <> Value Then
                    _Istochnik = Value
                    OnPropertyChanged(NameOf(Istochnik))
                    OnPropertyChanged(NameOf(NeedMashViz))
                    OnPropertyChanged(NameOf(MashPripTXT_IsRed))
                End If
            End Set
        End Property




        ''' <summary>
        ''' (8) Место отказа с поездами (как в касанте)
        ''' </summary>
        ''' <returns></returns>
        <Description("Место отказа с поездами (как в касанте)")>
        Public Property MestoOTS_TXT As String
            Get
                Return _MestoOTS_TXT
            End Get
            Set
                If _MestoOTS_TXT <> Value Then
                    _MestoOTS_TXT = Value
                    _cachedLocation = Nothing  ' сброс кеша
                    OnPropertyChanged(NameOf(MestoOTS_TXT))
                    OnPropertyChanged(NameOf(MestoOTS))        ' потому что зависит от TXT
                    OnPropertyChanged(NameOf(MestoOTS_Dor))    ' и от него тоже
                    OnPropertyChanged(NameOf(KrasREG))         ' добавили пересчет нового свойства
                    OnPropertyChanged(NameOf(HasPass))
                    OnPropertyChanged(NameOf(NeedMashViz))
                End If
            End Set
        End Property

        <JsonIgnore>
        Public Property Diff_Before As InlineCollection

        <JsonIgnore>
        Public Property Diff_After As InlineCollection
        <JsonIgnore>
        Public Property PreviousMestoOTS_TXT As String = Nothing

        ' Производные — только чтение
        <JsonIgnore>
        Private _cachedLocation As Tuple(Of String, String)

        ''' <summary>
        ''' Вычисляется из MestoOTS и MestoOTS_Dor — не сохраняется в JSON
        ''' Если дорога Красноярская и МестоОТС содержит РЕГ-1 - то "Абаканский регион"
        ''' Если дорога Красноярская и МестоОТС содержит РЕГ-2 - то "Красноярский регион"
        ''' В остальных случаях - "Прочие регионы"
        ''' </summary>
        <JsonIgnore>
        Public ReadOnly Property KrasREG As String
            Get

                Dim road As String = MestoOTS_Dor
                Dim place As String = MestoOTS_TXT

                If String.IsNullOrEmpty(road) OrElse String.IsNullOrEmpty(place) Then
                    Return "Прочие регионы"
                End If

                ' Проверяем, является ли дорога Красноярской (с учетом разных вариантов написания)
                If road.ToLower = "красноярская" Then
                    If place.Contains("РЕГ-1", StringComparison.OrdinalIgnoreCase) Then
                        Return "Абаканский регион"
                    ElseIf place.Contains("РЕГ-2", StringComparison.OrdinalIgnoreCase) Then
                        Return "Красноярский регион"
                    End If
                End If

                Return "Прочие регионы"
            End Get
        End Property

        ''' <summary>
        ''' Вычисляется из MestoOTS_TXT — не сохраняется в JSON
        ''' </summary>
        <JsonIgnore>
        Public ReadOnly Property MestoOTS As String
            Get
                If _cachedLocation Is Nothing Then
                    _cachedLocation = ParseLocationAndRoad(MestoOTS_TXT)
                    OnPropertyChanged(NameOf(IsStation))
                End If
                Return _cachedLocation.Item1
            End Get
        End Property







        ''' <summary>
        ''' Список планов через запятую (для отображения в карточке отказа)
        ''' </summary>
        <JsonIgnore>
        Public ReadOnly Property PlanListText As String
            Get
                If _Plan Is Nothing OrElse _Plan.Count = 0 Then
                    Return String.Empty
                End If

                ' Собираем все описания планов через запятую
                Dim descriptions As New List(Of String)
                For Each p In _Plan
                    If Not String.IsNullOrWhiteSpace(p.Description) Then
                        descriptions.Add(p.Description.Trim())
                    End If
                Next

                Return String.Join(", ", descriptions)
            End Get
        End Property




        '' <summary>
        ''' Вычисляется из MestoOTS_TXT — не сохраняется в JSON
        ''' </summary>
        <JsonIgnore>
        Public ReadOnly Property MestoOTS_Dor As String
            Get
                If _cachedLocation Is Nothing Then
                    _cachedLocation = ParseLocationAndRoad(MestoOTS_TXT)
                End If
                Return _cachedLocation.Item2
            End Get
        End Property


        <JsonIgnore>
        Private _cachedTrainInfo As Tuple(Of Boolean, List(Of Integer)) ' (HasPass, PassengerNumbers)



        ''' <summary>
        ''' True, если в отказе задержаны пассажирские или пригородные поезда 
        ''' (номер < 600 или 6000 ≤ № < 7000)
        ''' </summary>
        <JsonIgnore>
        Public ReadOnly Property HasPass As Boolean
            Get
                Return HasPassInText(Me.MestoOTS_TXT)
            End Get
        End Property




        ''' <summary>
        ''' (09) Серия локомотива точная
        ''' </summary>
        ''' <returns></returns>
        <Description("Серия локомотива точная")>
        Public Property SerLokExact As String
            Get
                Return _SerLokExact
            End Get
            Set
                If _SerLokExact <> Value Then
                    _SerLokExact = Value
                    OnPropertyChanged(NameOf(SerLokExact))
                    OnPropertyChanged(NameOf(SerLok))
                    OnPropertyChanged(NameOf(SerLokPripLokTXT))
                    OnPropertyChanged(NameOf(SerLokNumLokTXT))
                End If
            End Set
        End Property


        ''' <summary>
        ''' (000) серия локомотива (объед.)
        ''' </summary>
        ''' <returns>возвращает уже классифицированный тип серии локомотива, устанавливает во время присвоения</returns>
        <Description("Серия локомотива (объед.)")>
        <JsonIgnore>
        Public ReadOnly Property SerLok As String
            Get
                If SerLokExact <> "" Then
                    Return TypLoka(SerLokExact)
                Else
                    Return ""
                End If
            End Get
        End Property

        ''' <summary>
        ''' (10) Номер локомотива
        ''' </summary>
        <Description("Номер локомотива")>
        Public Property NumLok As String
            Get
                Return _NumLok
            End Get
            Set
                If _NumLok <> Value Then
                    _NumLok = Value
                    OnPropertyChanged(NameOf(NumLok))
                    OnPropertyChanged(NameOf(SerLokPripLokTXT))
                    OnPropertyChanged(NameOf(SerLokNumLokTXT))

                End If
            End Set
        End Property

        ''' <summary>
        ''' (11) Приписка локомотива
        ''' </summary>
        <Description("Приписка локомотива")>
        Public Property PripLok As String
            Get
                Return _PripLok
            End Get
            Set
                If _PripLok <> Value Then
                    _PripLok = Value
                    OnPropertyChanged(NameOf(PripLok))
                    OnPropertyChanged(NameOf(SerLokPripLokTXT))
                End If
            End Set
        End Property

        Private _DaNaLok As New ObservableCollection(Of Remont)

        ''' <summary>
        ''' Структурированные данные о ремонтах локомотива
        ''' </summary>
        Public Property DaNaLok As ObservableCollection(Of Remont)
            Get
                Return _DaNaLok
            End Get
            Set(value As ObservableCollection(Of Remont))
                If _DaNaLok IsNot value Then
                    _DaNaLok = value
                    OnPropertyChanged(NameOf(DaNaLok))
                End If
            End Set
        End Property


        <JsonIgnore>
        Public ReadOnly Property SerLokPripLokTXT As String
            Get
                If SerLokExact <> "" AndAlso NumLok <> "" AndAlso PripLok <> "" Then
                    Return $"{SerLokExact} №{NumLok}{vbCrLf}({PripLok})"
                Else
                    Return "Не указан"
                End If

            End Get
        End Property

        <JsonIgnore>
        Public ReadOnly Property SerLokPripLokInRowTXT As String
            Get
                If SerLokExact <> "" AndAlso NumLok <> "" AndAlso PripLok <> "" Then
                    Return $"{SerLokExact} №{NumLok} ({PripLok})"
                Else
                    Return ""
                End If

            End Get
        End Property



        <JsonIgnore>
        Public ReadOnly Property SerLokNumLokTXT As String
            Get
                If SerLokExact <> "" AndAlso NumLok <> "" Then
                    Return $"{SerLokExact} №{NumLok}"
                Else
                    Return "Не указан"
                End If

            End Get
        End Property

        ''' <summary>
        ''' (59) машинист
        ''' </summary>
        ''' <returns></returns>
        <Description("Фамилия машиниста")>
        Public Property Mash As String
            Get
                Return _Mash
            End Get
            Set
                Dim FF As String = NormFam(Value)
                If _Mash <> FF Then
                    _Mash = FF
                    OnPropertyChanged(NameOf(Mash))
                    OnPropertyChanged(NameOf(MashPripTXT))
                    OnPropertyChanged(NameOf(NeedMashViz))
                    OnPropertyChanged(NameOf(MashPripTXT_IsRed))
                End If
            End Set
        End Property

        ''' <summary>
        ''' (60) приписка машиниста
        ''' </summary>
        ''' <returns></returns>
        <Description("Приписка машиниста")>
        Public Property PripMash As String
            Get
                Return _PripMash
            End Get
            Set
                If _PripMash <> Value Then
                    _PripMash = Value
                    OnPropertyChanged(NameOf(PripMash))
                    OnPropertyChanged(NameOf(MashPripTXT))
                    OnPropertyChanged(NameOf(MashPripTXT_IsRed))
                End If
            End Set
        End Property


        <JsonIgnore>
        Public ReadOnly Property MashPripTXT As String
            Get
                If Mash <> "" AndAlso PripMash <> "" Then
                    Return $"{Mash}{vbCrLf}({PripMash})"
                Else
                    Return "Не указан"
                End If

            End Get
        End Property

        <JsonIgnore>
        Public ReadOnly Property MashPripInRowTXT As String
            Get
                If Mash <> "" AndAlso PripMash <> "" Then
                    Return $"{Mash} ({PripMash})"
                Else
                    Return ""
                End If

            End Get
        End Property


        Public ReadOnly Property MashPripTXT_IsRed As Boolean
            Get
                'если маш указан, дорога не крас и не ГИД 
                If (Not String.IsNullOrWhiteSpace(Mash)) AndAlso (Not MestoOTS_Dor.ToLower.Contains("крас") AndAlso (Not String.IsNullOrWhiteSpace(Istochnik) AndAlso Not Istochnik.ToLower.Contains("гид"))) Then
                    Return True
                    'если маш НЕ указан, дорога крас и ГИД или ручвв
                ElseIf (String.IsNullOrWhiteSpace(Mash)) AndAlso (MestoOTS_Dor.ToLower.Contains("крас") AndAlso (Not String.IsNullOrWhiteSpace(Istochnik) AndAlso (Istochnik.ToLower.Contains("гид") Or Istochnik.ToLower.Contains("руч")))) Then
                    Return True
                    'если маш НЕ указан, дорога крас и ВСЖД
                ElseIf (String.IsNullOrWhiteSpace(Mash)) AndAlso (MestoOTS_Dor.ToLower.Contains("крас") AndAlso (Not String.IsNullOrWhiteSpace(Istochnik) AndAlso (Not Istochnik.ToLower.Contains("гид") AndAlso (Not Istochnik.ToLower.Contains("руч"))))) Then
                    Return True
                    'если маш НЕ указан, дорога НЕ крас и ГИД 
                ElseIf (String.IsNullOrWhiteSpace(Mash)) AndAlso (Not MestoOTS_Dor.ToLower.Contains("крас") AndAlso (Not String.IsNullOrWhiteSpace(Istochnik) AndAlso Istochnik.ToLower.Contains("гид"))) Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property


        Public ReadOnly Property NeedMashViz As Visibility
            Get
                Dim Rez As Visibility = Visibility.Collapsed
                ' 1. Сначала проверяем, пустой ли машинист. Если уже заполнен — показывать плашку не надо.
                If Not String.IsNullOrWhiteSpace(Mash) Then Return Rez

                ' 2. Если пустой, проверяем условия дорог и источников
                Dim src As String = If(Istochnik, "").ToUpper().Trim()
                Dim road As String = If(MestoOTS_Dor, "").ToLower()
                Dim KtoZa As String = If(KtoZakryl, "").ToLower()

                ' Логика КрасЖД
                Dim isKrasOk = {"ГИД УРАЛ", "ВСЖД", "РУЧНОЙ ВВОД"}.Contains(src) AndAlso road.Contains("расноярс")
                ' Логика ВСЖД
                Dim isVszdOk = (src = "ГИД УРАЛ" AndAlso road.Contains("осточно"))

                If Not KtoZa.Contains("трп") Then
                    If isKrasOk OrElse isVszdOk Then
                        Rez = Visibility.Visible
                    End If
                End If
                Return Rez
            End Get
        End Property


        ''' <summary>
        ''' (13) Описание отказа
        ''' </summary>
        ''' <returns></returns>
        <Description("Описание отказа")>
        Public Property Opis As String
            Get
                Return _Opis
            End Get
            Set
                If _Opis <> Value Then
                    _Opis = Value
                    OnPropertyChanged(NameOf(Opis))
                    OnPropertyChanged(NameOf(IsViolet))
                End If
            End Set
        End Property


        <JsonIgnore> Public ReadOnly Property IsViolet As Boolean
            Get
                ' Условие: описание пустое (ничего нет или только пробелы) 
                ' ИЛИ в поле ZaKem стоит восклицательный знак
                Return String.IsNullOrWhiteSpace(Opis) OrElse
               (ZaKemCode = "!") OrElse Opis.Contains("_")
            End Get
        End Property


        Public Function ContainsPhrase(phrase As String) As Boolean

            If String.IsNullOrEmpty(phrase) OrElse String.IsNullOrEmpty(Me.Opis) Then
                Return False
            End If

            ' Очищаем текст от лишних пробелов и приводим к нижнему регистру
            Dim cleanOpis = Regex.Replace(Me.Opis, "\s+", " ").Trim().ToLower()
            Dim cleanPhrase = phrase.Trim().ToLower()

            Return cleanOpis.Contains(cleanPhrase)

        End Function



        Public Function IsPFB() As Boolean
            Dim KnownLocoSeries As String() = {"вл", "тэм2", "тэ10", "эп"}
            ' 1. Быстрый отказ по основным критериям
            If Not Uchet OrElse Kat > 1 Then Return False

            ' 2. Проверка ZaKem на "слд"
            If Not String.IsNullOrEmpty(ZaKem) AndAlso
               ZaKem.IndexOf("слд", StringComparison.OrdinalIgnoreCase) >= 0 Then Return True

            ' 3. Альтернативная ветка (пустой, Nothing или "!" ZaKem)
            If String.IsNullOrEmpty(ZaKem) OrElse ZaKem = "!" Then

                ' Безопасная проверка серии 3эс5
                Dim serLokHas3es5 As Boolean = Not String.IsNullOrEmpty(SerLokExact) AndAlso
                                               SerLokExact.IndexOf("3эс5", StringComparison.OrdinalIgnoreCase) >= 0

                ' Безопасная проверка номера локомотива (вместо CInt)
                If serLokHas3es5 Then
                    Dim numVal As Integer = 0
                    If Integer.TryParse(NumLok?.ToString(), numVal) AndAlso numVal < 859 Then Return True
                End If

                ' Проверка известных серий (вл, тэм18, тэ10, эп)
                If Not String.IsNullOrEmpty(SerLok) AndAlso
                   KnownLocoSeries.Any(Function(s) SerLok.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0) Then Return True
            End If

            Return False
        End Function


        Private _UpdateNotes As String = ""

        ''' <summary>
        ''' Системные пометки об обновлении: "дата!", "др дорога", "руч ввод" и т.п.
        ''' </summary>
        <Description("Пометки об обновлении из КАС АНТ")>
        Public Property UpdateNotes As String
            Get
                Return _UpdateNotes
            End Get
            Set(value As String)
                If _UpdateNotes <> value Then
                    _UpdateNotes = value
                    OnPropertyChanged(NameOf(UpdateNotes))
                    OnPropertyChanged(NameOf(UpdateNotesList))
                End If
            End Set
        End Property


        Public ReadOnly Property UpdateNotesList As List(Of String)
            Get
                If String.IsNullOrEmpty(UpdateNotes) Then
                    Return New List(Of String)
                End If
                Return UpdateNotes.Split(";"c).Select(Function(part) part.Trim()).Where(Function(s) s.Length > 0).ToList()
            End Get
        End Property


        Sub SetUpdateNotesList()
            If Zakryt > Date.MinValue Then
                AddUpdateNote("Восстановлен")

            ElseIf IsSaved Then
                AddUpdateNote("Восстановлен")
                AddUpdateNote("уже закрыт")
            ElseIf Not String.IsNullOrEmpty(ZaKem) AndAlso ZaKem?.ToLower.Contains("дорог") OrElse ZaKem?.ToLower.Contains("передан на") Then
                AddUpdateNote("Вернулся")
            Else
                'это на случай с ТР
                If ZaKem = ("") OrElse ZaKem = ("!") Then
                    AddUpdateNote("уже закрыт")
                    AddUpdateNote("???")
                    AddUpdateNote("Сохранен")
                ElseIf KtoZakryl.ToLower.Contains("трп") Then
                    AddUpdateNote("уже закрыт")
                    AddUpdateNote("???")
                End If
            End If

        End Sub

        Sub AddUpdateNote(Note As String)

            If String.IsNullOrEmpty(UpdateNotes) Then
                UpdateNotes = Note
            Else
                If Not UpdateNotes.Contains(Note) Then
                    UpdateNotes &= $"; {Note}"
                End If

            End If
        End Sub

        Public Sub RemoveItemUpdateNote(Itm As String)
            If String.IsNullOrEmpty(UpdateNotes) Then Return
            ' Разделяем строку на части
            Dim parts As String() = UpdateNotes.Split(New String() {"; "}, StringSplitOptions.None)
            ' Фильтруем: убираем совпадающие элементы и пустые/пробельные
            Dim filteredParts As New List(Of String)
            For Each part In parts
                If Not String.Equals(part.Trim(), Itm.Trim(), StringComparison.OrdinalIgnoreCase) AndAlso
                   Not String.IsNullOrWhiteSpace(part) Then
                    filteredParts.Add(part.Trim())
                End If
            Next
            ' Собираем обратно
            UpdateNotes = String.Join("; ", filteredParts)
            OnPropertyChanged(NameOf(UpdateNotes))
            OnPropertyChanged(NameOf(UpdateNotesList))
        End Sub

        Public Sub ClearAllItemUpdateNotes()
            UpdateNotes = ""
            OnPropertyChanged(NameOf(UpdateNotes))
            OnPropertyChanged(NameOf(UpdateNotesList))
        End Sub

        ''' <summary>
        ''' (12) типа Электрические машины Внутренний классификатор
        ''' </summary>
        ''' <returns></returns>
        <Description("Классификатор Внутренний (1 ур.)")>
        Public Property MyKlasLev1 As String
            Get
                Return _MyKlasLev1
            End Get
            Set
                If _MyKlasLev1 <> Value Then
                    _MyKlasLev1 = Value
                    OnPropertyChanged(NameOf(MyKlasLev1))
                End If
            End Set
        End Property


        ''' <summary>
        ''' (16) типа ТЭД Внутренний классификатор
        ''' </summary>
        ''' <returns></returns>
        <Description("Классификатор Внутренний (2 ур.)")>
        Public Property MyKlasLev2 As String
            Get
                Return _MyKlasLev2
            End Get
            Set
                If _MyKlasLev2 <> Value Then
                    _MyKlasLev2 = Value
                    OnPropertyChanged(NameOf(MyKlasLev2))
                End If
            End Set
        End Property

        ''' <summary>
        ''' (17) типа Коллектор Внутренний классификатор
        ''' </summary>
        ''' <returns></returns>
        <Description("Классификатор Внутренний (3 ур.)")>
        Public Property MyKlasLev3 As String
            Get
                Return _MyKlasLev3
            End Get
            Set
                If _MyKlasLev3 <> Value Then
                    _MyKlasLev3 = Value
                    OnPropertyChanged(NameOf(MyKlasLev3))
                    OnPropertyChanged(NameOf(MyKlasLev3_TXT))
                End If
            End Set
        End Property


        <JsonIgnore>
        Public ReadOnly Property MyKlasLev3_TXT As String
            Get
                Return IIf(_MyKlasLev3 <> "", $"{_MyKlasLev2}{vbCrLf}******{vbCrLf}{_MyKlasLev3}", "---")
            End Get
        End Property


        ''' <summary>
        ''' (23) станция (Да/Нет)
        ''' </summary>
        <Description("Признак: станция (Да/Нет)")>
        Public Property IsStation As Boolean
            Get
                Return _IsStation
            End Get
            Set
                If _IsStation <> Value Then
                    _IsStation = Value

                    OnPropertyChanged(NameOf(IsStation))
                    OnPropertyChanged(NameOf(IsStation_TXT))
                End If
            End Set
        End Property


        ''' <summary>
        ''' текст: станция/перегон
        ''' </summary>
        <Description("текст: станция/перегон")>
        Public ReadOnly Property IsStation_TXT As String
            Get
                If _IsStation = True Then
                    Return "Станция"
                Else
                    Return "Перегон"
                End If
            End Get



        End Property



        ''' <summary>
        ''' (24) количество задержанных пасс. п.
        ''' </summary>
        ''' <returns></returns>
        <Description("Количество задержанных пасс. п.")>
        Public Property PasKol As Integer
            Get
                Return _PasKol
            End Get
            Set
                If _PasKol <> Value Then
                    _PasKol = Value
                    OnPropertyChanged(NameOf(PasKol))
                    OnPropertyChanged(NameOf(TotalTrainsKol))
                End If
            End Set
        End Property

        ''' <summary>
        ''' (25) количество задержанных приг. п.
        ''' </summary>
        ''' <returns></returns>
        <Description("Количество задержанных приг. п.")>
        Public Property PrigKol As Integer
            Get
                Return _PrigKol
            End Get
            Set
                If _PrigKol <> Value Then
                    _PrigKol = Value
                    OnPropertyChanged(NameOf(PrigKol))
                    OnPropertyChanged(NameOf(TotalTrainsKol))
                End If
            End Set
        End Property

        ''' <summary>
        ''' (26) количество задержанных груз. п.
        ''' </summary>
        ''' <returns></returns>
        <Description("Количество задержанных груз. п.")>
        Public Property GruzKol As Integer
            Get
                Return _GruzKol
            End Get
            Set
                If _GruzKol <> Value Then
                    _GruzKol = Value
                    OnPropertyChanged(NameOf(GruzKol))
                    OnPropertyChanged(NameOf(TotalTrainsKol))
                End If
            End Set
        End Property

        <Description("Количество задержанных п.")>
        Public ReadOnly Property TotalTrainsKol As Integer
            Get
                Return GruzKol + PrigKol + PasKol
                OnPropertyChanged(NameOf(TotalTrainsKol))
            End Get
        End Property


        Public Property HarPrichin As String
            Get
                Return _HarPrichin
            End Get
            Set
                If _HarPrichin <> Value Then
                    _HarPrichin = Value
                    OnPropertyChanged(NameOf(HarPrichin))
                End If
            End Set
        End Property


        ''' <summary>
        ''' (45) типа Экипажная часть и механическое оборудование локомотива, МВПС  По классификатору из КАСАНТ
        ''' </summary>
        ''' <returns></returns>
        <Description("Классификатор КАСАНТ (1 ур.)")>
        Public Property OTSLev1 As String
            Get
                Return _OTSLev1
            End Get
            Set
                If _OTSLev1 <> Value Then
                    _OTSLev1 = Value
                    OnPropertyChanged(NameOf(OTSLev1))
                End If
            End Set
        End Property

        ''' <summary>
        ''' (46) типа Тяговый редуктор  По классификатору из КАСАНТ
        ''' </summary>
        ''' <returns></returns>
        <Description("Классификатор КАСАНТ (2 ур.)")>
        Public Property OTSLev2 As String
            Get
                Return _OTSLev2
            End Get
            Set
                If _OTSLev2 <> Value Then
                    _OTSLev2 = Value
                    OnPropertyChanged(NameOf(OTSLev2))
                End If
            End Set
        End Property


        ''' <summary>
        ''' (43) типа Тяговый редуктор  По классификатору из КАСАНТ
        ''' </summary>
        ''' <returns></returns>
        <Description("Классификатор КАСАНТ (3 ур.)")>
        Public Property OTSLev3 As String
            Get
                Return _OTSLev3
            End Get
            Set
                If _OTSLev3 <> Value Then
                    _OTSLev3 = Value
                    OnPropertyChanged(NameOf(OTSLev3))
                End If
            End Set
        End Property



        ''' <summary>
        ''' (47) признак Опасный отказ
        ''' </summary>
        ''' <returns></returns>
        <Description("Признак: Опасный ОТС")>
        Public Property ISDanger As Boolean
            Get
                Return _ISDanger
            End Get
            Set
                If _ISDanger <> Value Then
                    _ISDanger = Value
                    OnPropertyChanged(NameOf(ISDanger))
                    OnPropertyChanged(NameOf(HasAnyWarning))  ' ← важно!
                End If
            End Set
        End Property


        ''' <summary>
        ''' (51) Вид тяги
        ''' </summary>
        ''' <returns></returns>
        <Description("Вид тяги")>
        Public Property VidT As String
            Get
                Return _VidT
            End Get
            Set
                If _VidT <> Value Then
                    _VidT = Value
                    OnPropertyChanged(NameOf(VidT))
                End If
            End Set
        End Property

        ''' <summary>
        ''' (52) Дата создания ОТС
        ''' </summary>
        ''' <returns></returns>
        <Description("Дата создания ОТС")>
        Public Property Sozdan As Date
            Get
                Return _Sozdan
            End Get
            Set
                If _Sozdan <> Value Then
                    _Sozdan = Value
                    OnPropertyChanged(NameOf(Sozdan))
                End If
            End Set
        End Property


        ''' <summary>
        ''' (53) признак Корпоративного нарушения
        ''' </summary>
        ''' <returns></returns>
        <Description("Признак: Корпоративное нарушение")>
        Public Property ISKorp As Boolean
            Get
                Return _ISKorp
            End Get
            Set
                If _ISKorp <> Value Then
                    _ISKorp = Value
                    OnPropertyChanged(NameOf(ISKorp))
                    OnPropertyChanged(NameOf(HasAnyWarning))  ' ← важно!
                End If
            End Set
        End Property

        ''' <summary>
        ''' (54) признак Событие
        ''' </summary>
        ''' <returns></returns>
        <Description("Признак: Событие")>
        Public Property ISSobyt As Boolean
            Get
                Return _ISSobyt
            End Get
            Set
                If _ISSobyt <> Value Then
                    _ISSobyt = Value
                    OnPropertyChanged(NameOf(ISSobyt))
                    OnPropertyChanged(NameOf(HasAnyWarning))  ' ← важно!
                End If
            End Set
        End Property


        ''' <summary>
        ''' (55) продолжительность ОТС
        ''' </summary>
        ''' <returns></returns>
        <Description("(55) продолжительность ОТС")>
        Public Property Dlit As Single
            Get
                Return _Dlit
            End Get
            Set(value As Single)
                If _Dlit <> value Then
                    _Dlit = value
                    OnPropertyChanged(NameOf(Dlit))
                End If
            End Set
        End Property


        ''' <summary>
        ''' (56) время задержки пасс. п. (обрабатывать в SetPCH) 
        ''' </summary>
        ''' <returns></returns>
        <Description("Время задержки пасс. п.")>
        Public Property PasPCH As Single
            Get
                Return _PasPCH
            End Get
            Set(value As Single)

                If _PasPCH <> value Then
                    _PasPCH = value
                    OnPropertyChanged(NameOf(PasPCH))
                End If
            End Set
        End Property

        ''' <summary>
        ''' (57) время задержки приг. п. (обрабатывать в SetPCH) 
        ''' </summary>
        ''' <returns></returns>
        <Description("Время задержки приг. п.")>
        Public Property PrigPCH As Single
            Get
                Return _PrigPCH
            End Get
            Set(value As Single)

                If _PrigPCH <> value Then
                    _PrigPCH = value
                    OnPropertyChanged(NameOf(PrigPCH))
                End If
            End Set
        End Property

        ''' <summary>
        ''' (58) время задержки груз. п. (обрабатывать в SetPCH) 
        ''' </summary>
        ''' <returns></returns>
        <Description("Время задержки груз. п.")>
        Public Property GruzPCH As Single
            Get
                Return _GruzPCH
            End Get
            Set(value As Single)

                If _GruzPCH <> value Then
                    _GruzPCH = value
                    OnPropertyChanged(NameOf(GruzPCH))
                End If
            End Set
        End Property


        '==============================================================================================
        '==============================================================================================


        ''' <summary>
        ''' (66) Расследован за чужим СЛД
        ''' </summary>
        ''' <returns></returns>
        <Description("Расследован за чужим СЛД")>
        Public Property AlienSLD As String
            Get
                Return _AlienSLD
            End Get
            Set
                If _AlienSLD <> Value Then
                    _AlienSLD = Value
                    OnPropertyChanged(NameOf(AlienSLD))
                    OnPropertyChanged(NameOf(HasAnyWarning))  ' ← важно!
                End If
            End Set
        End Property

        ''' <summary>
        ''' (67) Расследован с нарушением срока
        ''' </summary>
        ''' <returns></returns>
        <Description("Расследован с нарушением срока")>
        Public Property NarushSroka As Boolean
            Get
                Return _NarushSroka
            End Get
            Set
                If _NarushSroka <> Value Then
                    _NarushSroka = Value
                    OnPropertyChanged(NameOf(NarushSroka))
                End If
            End Set
        End Property

        ''' <summary>
        ''' (65) оказание помощи
        ''' </summary>
        <Description("Оказание помощи")>
        Public Property OkaPom As Boolean
            Get
                Return _OkaPom
            End Get
            Set
                If _OkaPom <> Value Then
                    _OkaPom = Value
                    OnPropertyChanged(NameOf(OkaPom))
                    OnPropertyChanged(NameOf(HasAnyWarning))  ' ← важно!
                End If
            End Set
        End Property

        ' --- Вычисляемое свойство ---
        <JsonIgnore>
        Public ReadOnly Property HasAnyWarning As Boolean
            Get
                Return Not String.IsNullOrEmpty(_AlienSLD) OrElse _OkaPom OrElse ISDanger OrElse ISKorp OrElse ISSobyt
            End Get
        End Property


        '==============================================================================================
        '==============================================================================================

        ''' <summary>
        ''' (20) Дата поступления ОТС
        ''' </summary>
        <Description("Дата поступления ОТС")>
        Public Property Postup As Date
            Get
                Return _Postup
            End Get
            Set(value As Date)
                If value.Date < Nach.Date Then
                    Exit Property
                End If
                'при AND вычисляются оба значения, та к инадо, чтобы просто при пустой дате не обрубалась установка свойства
                If Zakryt.Date > Date.MinValue And Zakryt.Date < value.Date Then
                    Exit Property
                End If
                If _Postup <> value Then
                    _Postup = value
                    OnPropertyChanged(NameOf(Postup))

                End If
            End Set
        End Property


        Private _VernulsaOTS As Date




        ''' <summary>
        ''' (20) Дата поступления ОТС
        ''' </summary>
        <Description("Дата возвращения ОТС")>
        Public Property VernulsaOTS As Date
            Get
                Return _VernulsaOTS
            End Get
            Set(value As Date)
                If Not Uchet Then Exit Property
                If value.Date < Nach.Date Then Exit Property
                If Zakryt.Date > Date.MinValue Then Exit Property
                If Peredan.Date > value.Date Then Exit Property
                If _VernulsaOTS = value Then Exit Property

                _VernulsaOTS = value
                OnPropertyChanged(NameOf(VernulsaOTS))
            End Set
        End Property


        ''' <summary>
        ''' (5) Потери п/часов ОТС
        ''' </summary>
        <Description("Потери п/часов ОТС")>
        Public Property PCh As Single
            Get
                If Kat > 2 Then Return 0
                Return _PCh.GetValueOrDefault(0)

            End Get
            Set

                Dim currentValue = If(_PCh, 0)
                If currentValue <> Value Then
                    Dim newValue = If(Kat < 3, Value, 0)

                    ' Вызываем корректировку только если значение уже было установлено
                    If _PCh.HasValue Then
                        SetKorPCH(currentValue, newValue)
                    End If

                    _PCh = newValue
                    OnPropertyChanged(NameOf(PCh))
                End If
                FormattedPCh = PCh.ToString("F2", CultureInfo.GetCultureInfo("ru-RU"))



            End Set

        End Property



        Public Sub SetKorPCH(OldVal As Single, NewVal As Single)
            Dim Rez As String = Math.Round(NewVal - OldVal, 2).ToString("+0.00;-0.00;0.00", Ru)
            Dim Txt As String = $"корректировка п/часов с {OldVal} до {NewVal} на {Rez:+0.00;-0.00;0}"
            'If NewVal < OldVal Then
            MW.SharedCalendar.EditMode = "KorDate"  ' "Postup" или "Zakryt"
                KorPCHonDate = Rez
                BindingOperations.ClearBinding(MW.SharedCalendar, SingleCalendarControl.TargetDateProperty)

                'сразу привязываем к целевому свойству (при указании даты - она сразу установится через SingleCalendarControl в свойство PointedOtkaz-а
                MW.SharedCalendar.SetBinding(SingleCalendarControl.TargetDateProperty,
            New Binding("KorDate") With {
                .Source = PointedOtkaz,
                .Mode = BindingMode.TwoWay
            })

                MW.SharedCalendar.CalendarPopup.PlacementTarget = MW
                MW.SharedCalendar.CalendarPopup.Placement = PlacementMode.Center
                MW.SharedCalendar.IsOpen = True
                KorPCH += Rez
            'End If
            Dim Hent As New HistoryEntry
            With Hent
                .ShowDate = False
                .Description = Txt
            End With
            History.Add(Hent)
        End Sub


        Private _FormattedPCh As String
        ' Свойство для форматирования PCh
        Public Property FormattedPCh As String
            Get
                Return _FormattedPCh
            End Get
            Set
                If _FormattedPCh <> Value Then
                    _FormattedPCh = Value
                    OnPropertyChanged(NameOf(Uchet))
                    OnPropertyChanged(NameOf(FormattedPCh))
                End If

            End Set
        End Property



        ''' <summary>
        ''' (7) Кто расследовал ОТС
        ''' </summary>
        <Description("Кто расследовал ОТС")>
        Public Property KtoZakryl As String
            Get
                Return _KtoZakryl
            End Get
            Set
                If _KtoZakryl <> Value Then
                    _KtoZakryl = Value

                    OnPropertyChanged(NameOf(KtoZakryl))
                    OnPropertyChanged(NameOf(KomplexAsInt))
                    OnPropertyChanged(NameOf(Uchet))
                    OnPropertyChanged(NameOf(NeedMashViz))
                    OnPropertyChanged(NameOf(DateColorBrush))
                    OnPropertyChanged(NameOf(IDBackColorBrush))
                    OnPropertyChanged(NameOf(OTSBackColorBrush))
                    OnPropertyChanged(NameOf(ZaKem))
                End If

            End Set
        End Property


        ''' <summary>
        ''' (21) Дата передачи ОТС на др дорогу
        ''' </summary>
        <Description("Дата передачи ОТС на др дорогу")>
        Public Property Peredan As Date
            Get
                Return _Peredan
            End Get
            Set
                If _Peredan <> Value Then
                    _Peredan = Value

                    OnPropertyChanged(NameOf(Peredan))
                    OnPropertyChanged(NameOf(DateColorBrush))
                    OnPropertyChanged(NameOf(IDBackColorBrush))
                    OnPropertyChanged(NameOf(KomplexAsInt))
                    OnPropertyChanged(NameOf(DaysOnRassled))
                    OnPropertyChanged(NameOf(Uslovie3))
                    OnPropertyChanged(NameOf(DisplayKomplexInteger))
                    OnPropertyChanged(NameOf(DisplayDaysOnRassled))
                    OnPropertyChanged(NameOf(PCh))
                    OnPropertyChanged(NameOf(FormattedPCh))
                    OnPropertyChanged(NameOf(Uchet))
                    'работает (вместо CheckChangeProperties())
                End If
            End Set
        End Property

        ''' <summary>
        ''' (22) Дата расследования ОТС
        ''' </summary>
        <Description("Дата расследования ОТС")>
        Public Property Zakryt As Date
            Get
                Return _Zakryt
            End Get
            Set
                If _Zakryt <> Value Then
                    _Zakryt = Value
                    OnPropertyChanged(NameOf(Zakryt))
                    OnPropertyChanged(NameOf(DateColorBrush))
                    OnPropertyChanged(NameOf(KomplexAsInt))
                    OnPropertyChanged(NameOf(DaysOnRassled))
                    OnPropertyChanged(NameOf(IsSaved))
                    OnPropertyChanged(NameOf(Uslovie3))
                    OnPropertyChanged(NameOf(DisplayKomplexInteger))
                    OnPropertyChanged(NameOf(DisplayDaysOnRassled))
                    OnPropertyChanged(NameOf(PCh))
                    OnPropertyChanged(NameOf(FormattedPCh))
                    OnPropertyChanged(NameOf(Uchet))
                    'работает (вместо CheckChangeProperties())
                End If
            End Set
        End Property


#End Region

        '==============================================================================================

#Region "свойства, не требующие чтения из файла"


        <Description("Свойство для цвета фона номера ОТС")>
        <JsonIgnore>
        Public ReadOnly Property IDBackColorBrush As Brush
            Get
                If ZaKemCode?.ToLower Like "*доро*" Then
                    Return Brushes.Yellow
                Else
                    Return OTSBackColorBrush
                End If

            End Get
        End Property


        <Description("Свойство для цвета фона контрола ОТС")>
        <JsonIgnore>
        Public ReadOnly Property OTSBackColorBrush As Brush

            Get
                Dim key As String = "AppBackBrush" ' Дефолт
                Dim raw = ZaKemCode?.ToLower()

                ' Если кода нет, проверяем только KtoZakryl
                If String.IsNullOrEmpty(raw) Then
                    If KtoZakryl?.ToLower() Like "тр*" Then key = "TRBackBrush"
                Else
                    ' Используем True, чтобы внутри Case можно было писать выражения
                    Select Case True
                        Case raw Like "слд*" : key = "SLDBackBrush"
                        Case raw Like "*окорем*" : key = "LocoRemBackBrush"
                        Case raw Like "*окостро*" : key = "LocoStroyBackBrush"
                        Case raw Like "*ублика*" : key = "DublicatBackBrush"
                        Case raw Like "*ехнол*" : key = "TechnologyBackBrush"
                        Case raw = "тч" : key = "TCHBackBrush"
                        Case raw Like "проч*", raw = "тч9" : key = "ProchBackBrush"
                        Case KtoZakryl?.ToLower() Like "тр*" : key = "TRBackBrush"
                    End Select
                End If

                ' Достаем из ресурсов. Если ключа нет (в другом приложении), вернет стандартную AppBackBrush
                Dim res = Application.Current.TryFindResource(key)
                Return If(TryCast(res, Brush), DirectCast(Application.Current.FindResource("AppBackBrush"), Brush))
            End Get


        End Property




        <Description("Свойство для цвета шрифта даты")>
        <JsonIgnore>
        Public ReadOnly Property DateColorBrush As Brush
            Get
                Dim Co As New SolidColorBrush(Colors.Black)
                'поменяется в GetCol если надо будет
                DateFontWeightStyle = FontWeights.Normal
                Dim daysDifference As Integer = (DateTime.Today - _Nach.Date).Days

                If Not Uslovie3 OrElse (ZaKem = "" Or ZaKem = "!") Then
                    Co = GetCol(daysDifference)
                End If
                Return Co
            End Get
        End Property

        Function GetCol(daydif As Integer) As SolidColorBrush
            Dim Co As New SolidColorBrush(Colors.Black)
            If daydif >= 4 AndAlso daydif <= 10 Then
                Co = New SolidColorBrush(Colors.BlueViolet) ' Синий цвет
                DateFontWeightStyle = FontWeights.Bold
            ElseIf daydif > 10 Then
                Co = New SolidColorBrush(Colors.Red) ' Красный цвет
                DateFontWeightStyle = FontWeights.Bold
            End If

            OnPropertyChanged(NameOf(DateFontWeightStyle))
            Return Co
        End Function

        <Description("Свойство для жирности шрифта даты")>
        <JsonIgnore>
        Public Property DateFontWeightStyle As FontWeight
            Get
                Return _DateFontWeightStyle
            End Get
            Set
                If _DateFontWeightStyle <> Value Then
                    _DateFontWeightStyle = Value
                    OnPropertyChanged(NameOf(DateFontWeightStyle))

                End If
            End Set
        End Property


        ''' <summary>
        ''' (41) ОТС сохранен (Да/Нет) Zakryt = Date.MinValue And Peredan = Date.MinValue And Uslovie2()
        ''' </summary>
        <Description("ОТС сохранен (Да/Нет)")>
        <JsonIgnore>
        Public ReadOnly Property IsSaved As Boolean
            Get
                Dim Flag As Boolean = False
                If Zakryt = Date.MinValue And Peredan = Date.MinValue And Uslovie2() Then
                    Flag = True
                End If

                Return Flag
            End Get
        End Property

        ''' <summary>
        ''' "тч" или "*ублика*" или "*ехнолог*"
        ''' </summary>
        <JsonIgnore>
        Public ReadOnly Property Uslovie1() As Boolean
            Get
                Dim Flag As Boolean = False
                If ZaKemCode?.ToLower = "тч" OrElse ZaKemCode?.ToLower Like "*ублика*" OrElse ZaKemCode?.ToLower Like "*ехнолог*" Then
                    Flag = True
                End If
                OnPropertyChanged(NameOf(IsSaved))
                OnPropertyChanged(NameOf(Uchet))
                Return Flag

            End Get
        End Property


        ''' <summary>
        ''' "слд*" или "*окорем*" или "*окострой*" или "*проч*"
        ''' </summary>
        <JsonIgnore>
        Public ReadOnly Property Uslovie2() As Boolean
            Get
                Dim Flag As Boolean = False
                If ZaKemCode?.ToLower.Contains("слд") OrElse ZaKemCode?.ToLower.Contains("окорем") OrElse ZaKemCode?.ToLower.Contains("окострой") OrElse ZaKemCode?.ToLower.Contains("проч") Then
                    Flag = True
                End If
                Return Flag
            End Get
        End Property

        ''' <summary>
        ''' Peredan or Zakryt
        ''' </summary>
        <JsonIgnore>
        Public ReadOnly Property Uslovie3() As Boolean
            Get
                Return Peredan > Date.MinValue Or Zakryt > Date.MinValue
            End Get
        End Property



        '''' <summary>
        '''' Not "*ублика*" Not "*ехнолог*" Or <> "тч9"
        '''' </summary>
        <JsonIgnore> Public ReadOnly Property Uslovie4() As Boolean
            Get
                'это короткая запись
                Dim s As String = If(ZaKemCode, "").ToLower()
                Return Not {"ублика", "ехнолог", "тч9"}.Any(Function(w) s.Contains(w))
            End Get
        End Property

        ''' <summary>
        ''' дней в расследовании
        ''' </summary>
        <JsonIgnore>
        Public ReadOnly Property DaysOnRassled() As Integer
            Get

                Dim Rez As Integer = 0
                Dim isTrpu As Boolean = KtoZakryl IsNot Nothing AndAlso KtoZakryl.ToLower().Contains("трп")

                If (Not Uslovie3 OrElse ZaKem = "" OrElse ZaKem = "!") AndAlso Not isTrpu Then
                    Rez = (Date.Today - _Nach.Date).Days
                End If

                Return Rez

            End Get
        End Property

        <JsonIgnore>
        Public ReadOnly Property DisplayDaysOnRassled() As String
            Get
                If DaysOnRassled < 1 Then
                    Return "--"
                Else
                    Return DaysOnRassled.ToString()
                End If
            End Get
        End Property

        <JsonIgnore>
        Public ReadOnly Property DisplayKomplexInteger() As String
            Get
                If KomplexAsInt < 1 Then
                    Return "--"
                Else
                    Return KomplexAsInt.ToString()
                End If

            End Get
        End Property

        '==============================================================================================
        '==============================================================================================


        <Description("Отказ выбран/Pointed/ (да/нет)")>
        <JsonIgnore>
        Public Property Pointed As Boolean
            Get
                Return _Pointed
            End Get
            Set(value As Boolean)
                If _Pointed <> value Then
                    _Pointed = value
                    OnPropertyChanged(NameOf(Pointed))
                End If
            End Set
        End Property


        <Description("Отказ помечен (да/нет)")>
        <JsonIgnore>
        Public Property Marked As Boolean
            Get
                Return _marked
            End Get
            Set(value As Boolean)
                If _marked <> value Then
                    _marked = value
                    OnPropertyChanged(NameOf(Marked))
                End If
            End Set
        End Property

        ''' <summary>
        ''' попробовать местную функцию для суммирования при чтении из файла
        ''' </summary>
        ''' <returns></returns>
        <Description("Сумма корректировок времени")>
        Public Property KorPCH As Single
            Get
                Return _KorPCH
            End Get
            Set
                If _KorPCH <> Value Then
                    _KorPCH = Value
                    OnPropertyChanged(NameOf(KorPCH))
                    OnPropertyChanged(NameOf(IsKorrect))
                    OnPropertyChanged(NameOf(KorrectValTxt))
                End If
            End Set
        End Property


        Private _KorDate As Date = Date.MinValue

        ''' <summary>
        ''' попробовать местную функцию для суммирования при чтении из файла
        ''' </summary>
        ''' <returns></returns>
        <Description("Сумма корректировок времени")>
        Public Property KorDate As Date
            Get
                Return _KorDate
            End Get
            Set
                If _KorDate <> Value Then
                    _KorDate = Value
                    OnPropertyChanged(NameOf(KorDate))

                End If
            End Set
        End Property


        Private _KorPCHonDate As Single = 0
        Public Property KorPCHonDate As Single
            Get
                Return _KorPCHonDate
            End Get
            Set
                If _KorPCHonDate <> Value Then
                    _KorPCHonDate = Value
                    OnPropertyChanged(NameOf(KorPCHonDate))
                End If
            End Set
        End Property

        <JsonIgnore>
        Public ReadOnly Property IsKorrect As Boolean
            Get
                Return KorPCH < 0
            End Get
        End Property

        <JsonIgnore>
        Public ReadOnly Property KorrectValTxt As String
            Get
                Return $"Корр. п/ч: {KorPCH:F2}"
            End Get
        End Property

#End Region

#Region "свойства-функции, не требующие чтения из файла"

        Private _Uchet As Boolean
        Private _Okonch As Date

        ''' <summary>
        ''' ОТС закрыт в комплексе
        ''' </summary>
        <Description("ОТС закрыт в комплексе")>
        <JsonIgnore>
        Public ReadOnly Property Uchet As Boolean

            Get
                Return KomplexAsInt > 0
            End Get


        End Property


        <Description("комплекс")>
        <JsonIgnore>
        Public ReadOnly Property KomplexAsInt As Integer
            Get
                Return GetKomplexOTS()
            End Get
        End Property



        <Description("комплекс")>
        <JsonIgnore>
        Public ReadOnly Property KomplexAsString As String
            Get
                Select Case GetKomplexOTS()
                    Case 1
                        Return "ТЧЭ-1"
                    Case 2
                        Return "ТЧЭ-2"
                    Case 3
                        Return "ТЧЭ-3"
                    Case 5
                        Return "ТЧЭ-5"
                    Case 7
                        Return "ТЧЭ-7"
                    Case Else
                        Return "--"
                End Select
            End Get
        End Property

        <Description("ОТС в расследовании (да/нет)")>
        <JsonIgnore>
        Public ReadOnly Property VRassled As Boolean
            Get
                Select Case ZaKem
                    Case "", "!"
                        Return True
                    Case Else
                        Return False
                End Select
            End Get
        End Property


#End Region

        '==============================================================================================

#Region "Функции класса"

        <Description("Подлежит или нет подсчёту отказ")>
        Public Function Nado(Vina As String) As Boolean
            Dim Fla As Boolean
            Fla = True
            If Not IsNothing(Vina) Then
                If Vina?.ToLower Like "*дорог*" Then
                    Fla = False
                ElseIf Vina?.ToLower Like "*дублик*" Then
                    Fla = False
                ElseIf Vina?.ToLower Like "*ехнол*" Then
                    Fla = False
                ElseIf Vina?.ToLower = "тч9" Then
                    Fla = False
                ElseIf Vina?.ToLower = "тч10" Then
                    Fla = False
                End If
            Else
                Fla = False
            End If


            Return Fla
        End Function

        ''' <summary>
        ''' Метод для получения описания свойств
        ''' </summary>
        ''' <param name="propertyName"></param>
        ''' <returns></returns>
        <Description("Получить описание свойства")>
        Public Function GetPropertyDescription(propertyName As String) As String
            Dim propInfo = Me.GetType().GetProperty(propertyName)
            If propInfo IsNot Nothing Then
                Dim attributes = propInfo.GetCustomAttributes(GetType(DescriptionAttribute), False)
                If attributes.Length > 0 Then
                    Return CType(attributes(0), DescriptionAttribute).Description
                End If
            End If
            Return String.Empty
        End Function


        'подменяем свойство для джейсон, чтобы в файле хранилось сырое значение, которое потом будет грузиться сразу в Zakem и там работать
        <JsonProperty("ZaKem")>
        Friend Property ZaKemCode As String
            Get
                Return _ZaKem
            End Get
            Set(value As String)
                _ZaKem = value   ' ← важно: не через ZaKem, а напрямую в поле!
                OnPropertyChanged(NameOf(ZaKem))  ' ← обновить UI, потому что ZaKem зависит от _ZaKem
                'OnPropertyChanged(NameOf(FormattedPCh))
                OnPropertyChanged(NameOf(Uchet)) ' а то не срабатывает жирный PCh при ТЧ => ТР
                OnPropertyChanged(NameOf(OTSBackColorBrush))
                OnPropertyChanged(NameOf(IDBackColorBrush))
                OnPropertyChanged(NameOf(IsViolet))
                ' ... можно и другие OnPropertyChanged, IsViolet если нужно
            End Set
        End Property

        <JsonIgnore>
        Friend Shared ReadOnly SafeName As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From
            {
        {"тч", "ТЧЭ"},
        {"слд1", "СЛД Боготол"},
        {"слд2", "СЛД Красноярск"},
        {"слд3", "СЛД Иланская"},
        {"слд5", "СЛД Ачинск"},
        {"слд7", "СЛД Абакан"},
        {"тч9", "Прочие причины п.5.15 Положения"},
        {"прочие", "Прочие предприятия"},
        {"дубликат", "Дубликат"},
        {"технология", "Технологическое нарушение"},
        {"др дорога", "Передан на другую дорогу"},
        {"др дорога/П", "др дорога/П"},
        {"др дорога/В", "др дорога/В"},
        {"др дорога/Ш", "др дорога/Ш"},
        {"др дорога/Э", "др дорога/Э"},
        {"др дорога/Д", "др дорога/Д"},
        {"др дорога/ДМВ", "др дорога/ДМВ"},
        {"ЛокоРемЗавод", "Локомотиворемонтный завод"},
        {"локостройЗавод", "Локомотивостроительный завод"}
                               }


        Public Enum ZakemFilter
            Full
            RassledZa
            SavedOnly  ' для IsSavedWin
            ToOut
            ' ... можно добавить другие
        End Enum
        Friend Shared Function GetZakemByFilter(filter As ZakemFilter) As Dictionary(Of String, String)
            Dim keys As String() = Nothing
            Select Case filter
                Case ZakemFilter.Full
                    Return SafeName
                Case ZakemFilter.RassledZa
                    keys = {"слд1", "слд2", "слд3", "слд5", "слд7", "ЛокоРемЗавод", "прочие", "локостройЗавод", "тч9", "тч"}
                Case ZakemFilter.SavedOnly
                    keys = {"слд1", "слд2", "слд3", "слд5", "слд7", "прочие", "ЛокоРемЗавод", "локостройЗавод"}
                Case ZakemFilter.ToOut
                    keys = {"дубликат", "технология", "др дорога", "др дорога/П", "др дорога/В", "др дорога/Ш", "др дорога/Э", "др дорога/Д", "др дорога/ДМВ"}
                Case Else
                    Return New Dictionary(Of String, String)
            End Select

            ' 2. Теперь keys видна здесь, и код сработает
            ' Добавлена проверка Nothing на случай, если сработает Case Else (хотя там стоит Return)
            If keys Is Nothing Then Return New Dictionary(Of String, String)

            Return SafeName.Where(Function(kvp) keys.Contains(kvp.Key)).OrderBy(Function(kvp) kvp.Key).ToDictionary(Function(k) k.Key, Function(v) v.Value)

        End Function



        Friend Shared Function NormalizeName(value As String) As String
            If String.IsNullOrWhiteSpace(value) Then Return value

            Dim trimmed = value.Trim()

            ' 1. Точное совпадение из словаря
            Dim normalized = SafeName.GetValueOrDefault(trimmed, Nothing)
            If normalized IsNot Nothing Then
                ' 2. Шаблон "*дорога/*" — ищем подстроку "дорога/" (регистронезависимо)
                Dim idx = trimmed.IndexOf("дорога/", StringComparison.OrdinalIgnoreCase)
                If idx >= 0 Then
                    ' Берём всё после символа '/' (после "дорога/" — это позиция idx + 7)
                    Dim roadName = trimmed.Substring(idx + 7).Trim.ToUpper
                    Return $"Передан на {roadName}"
                Else
                    Return normalized
                End If
            Else
                ' 3. Ничего не подошло — возвращаем исходное значение
                Return value
            End If

        End Function


        ''' <summary>
        ''' Словарь соответствия номера ТЧЭ → полное название
        ''' </summary>
        Private Shared ReadOnly TCHE_NAMES As New Dictionary(Of Integer, String) From {
    {1, "ТЧЭ Боготол"},
    {2, "ТЧЭ Красноярск"},
    {3, "ТЧЭ Иланская"},
    {5, "ТЧЭ Ачинск"},
    {7, "ТЧЭ Абакан"}
}

        ''' <summary>
        ''' Возвращает полное название ТЧЭ по номеру
        ''' </summary>
        Public Shared Function GetTCHE_Name(number As Integer) As String
            Return If(TCHE_NAMES.ContainsKey(number), TCHE_NAMES(number), $"ТЧЭ-{number}")
        End Function

        ''' <summary>
        ''' Вспомогательная функция: возвращает номер ТЧЭ (1..7) или 0 если не распознано
        ''' </summary>
        Private Function GetTCHE_Number(input As String) As Integer
            If String.IsNullOrWhiteSpace(input) Then Return 0

            Dim result = GetTCH_OnPripMASH(input, False)
            If TypeOf result Is Integer Then
                Return CInt(result)
            End If

            Return 0
        End Function






        ''' <summary>
        ''' Возвращает номер ТЧЭ по названию (регистронезависимо)
        ''' </summary>
        Public Shared Function GetTCHE_NumberByName(name As String) As Integer
            If String.IsNullOrWhiteSpace(name) Then Return 0

            Dim cleanName = name.Trim().ToUpperInvariant()

            For Each kvp In TCHE_NAMES
                ' Извлекаем чистое название без "ТЧЭ " (например, "Боготол" из "ТЧЭ Боготол")
                Dim cleanValue = kvp.Value.Replace("ТЧЭ ", "").ToUpperInvariant()

                ' Проверяем совпадение по номеру или названию
                If cleanName Like $"*ТЧЭ*{kvp.Key}*" OrElse
           cleanName Like $"*{cleanValue}*" Then
                    Return kvp.Key
                End If
            Next

            Return 0
        End Function


        ''' <summary>
        ''' (14) За кем расследован ОТС
        ''' </summary>
        <Description("За кем расследован ОТС")>
        <JsonIgnore>
        Public Property ZaKem As String
            Get

                Dim Gg As String = NormalizeName(_ZaKem) ' на случай, если кто-то напрямую тронул _ZaKem

                If Not IsNothing(KtoZakryl) Then
                    Gg = IIf((Gg = "" Or Gg = "!") And (KtoZakryl?.ToLower.Contains("трп")), "тр", Gg)

                End If
                Return Gg
            End Get
            Set(value As String)
                If _ZaKem <> value Then
                    _ZaKem = value   ' ← сохраняем "как есть", но...
                    OnPropertyChanged(NameOf(ZaKem))  ' → Get вернёт нормализованное!
                    OnPropertyChanged(NameOf(Zakem_TXT))
                    OnPropertyChanged(NameOf(OTSBackColorBrush))
                    OnPropertyChanged(NameOf(IDBackColorBrush))
                    OnPropertyChanged(NameOf(KomplexAsInt))
                    OnPropertyChanged(NameOf(DaysOnRassled))
                    OnPropertyChanged(NameOf(DateColorBrush))
                    OnPropertyChanged(NameOf(DisplayKomplexInteger))
                    OnPropertyChanged(NameOf(DisplayDaysOnRassled))
                    OnPropertyChanged(NameOf(PCh))
                    OnPropertyChanged(NameOf(FormattedPCh))
                    OnPropertyChanged(NameOf(Uchet))
                    OnPropertyChanged(NameOf(IsViolet))
                    OnPropertyChanged(NameOf(IsSaved))
                End If
            End Set
        End Property




        <JsonIgnore>
        Public ReadOnly Property Zakem_TXT As String
            Get
                Return IIf(IsSaved, $"{ZaKem}{vbCr}(Сохранен)", ZaKem)
            End Get
        End Property


        ''' <summary>
        ''' Возвращает номер комплекса депо. 
        ''' </summary>
        ''' <returns> 1,2,3,5,7. Если 0 - не определено</returns>
        <Description("Возвращает номер комплекса депо")>
        Public Function GetKomplexOTS() As Integer
            Dim KMPL As Integer = 0

            If ZaKemCode = "слд1" Then
                KMPL = 1
            ElseIf ZaKemCode = "слд2" Then
                KMPL = 2
            ElseIf ZaKemCode = "слд3" Then
                KMPL = 3
            ElseIf ZaKemCode = "слд5" Then
                KMPL = 5
            ElseIf ZaKemCode = "слд7" Then
                KMPL = 7
            ElseIf ZaKem = "тр" Then
                If KtoZakryl = "ТРПУ-11" Then
                    KMPL = 1
                ElseIf KtoZakryl = "ТРПУ-4" Then
                    KMPL = 2
                ElseIf KtoZakryl = "ТРПУ-9" Then
                    KMPL = 3
                ElseIf KtoZakryl = "ТРПУ-12" Then
                    KMPL = 5
                ElseIf KtoZakryl = "ТРПУ-10" Then
                    KMPL = 7
                End If
            ElseIf ZaKemCode = "" Or ZaKemCode = "!" Or ZaKemCode = "тч" Or ZaKemCode Like "*авод*" Or ZaKemCode Like "*рочи*" Then
                If KtoZakryl = "ТЧЭ-1" Then
                    KMPL = 1
                ElseIf KtoZakryl = "ТЧЭ-2" Then
                    KMPL = 2
                ElseIf KtoZakryl = "ТЧЭ-3" Then
                    KMPL = 3
                ElseIf KtoZakryl = "ТЧЭ-5" Then
                    KMPL = 5
                ElseIf KtoZakryl = "ТЧЭ-7" Then
                    KMPL = 7
                End If
            End If


            Return KMPL
        End Function

        ''' <summary>
        ''' Возвращает ТЧЭ, закрывавшее, передававшее, расследовавшее ОТС
        ''' </summary>
        ''' <returns></returns>
        <Description("ТЧЭ, закрывавшее, передававшее, расследовавшее ОТС")>
        Public Function GetKtoRassledoval() As String
            ' цель - указать депо, расследовавшее отказ. Если наша приписка, то -ТЧЭ если не наша приписка - то *ЖД

            Dim Ff As String = "--"

            If Mash <> "" Then
                If Istochnik Like "*ЖД*" Then
                    If Not MestoOTS_Dor Like "*раснояр*" And (Not MestoOTS_Dor Like "*сточно*") Then
                        Ff = "!!" 'если Иванов, источник *ЖД и место на ЗАБЖД например
                    Else
                        Ff = Istochnik
                    End If
                Else
                    If KtoZakryl Like "*ТР*" Then
                        Ff = KtoZakryl
                    Else
                        Ff = GetTCH_OnPripMASH(PripMash)
                    End If
                End If
            Else
                If Istochnik Like "*ЖД*" Then
                    If MestoOTS_Dor Like "*раснояр*" Or (MestoOTS_Dor Like "*сточно*") Then ' непонятно ?????????
                        Ff = "!!" 'если маш пустой а дорога наша или ВСЖД
                    Else
                        Ff = Istochnik
                    End If
                Else
                    If KtoZakryl Like "*ТР*" Then
                        Ff = KtoZakryl
                    Else
                        Ff = "!!"
                    End If
                End If

            End If

            Return Ff
        End Function


        ''' <summary>
        ''' Возвращает ТЧЭ приписки машиниста в виде строки или числа
        ''' </summary>
        ''' <param name="St">Приписка машиниста</param>
        ''' <param name="asString">True - вернуть строку ("ТЧЭ-X"), False - вернуть число (X)</param>
        ''' <returns>ТЧЭ в виде строки или числа</returns>
        <Description("ТЧЭ приписки машиниста в виде строки или числа")>
        Public Function GetTCH_OnPripMASH(St As String, Optional asString As Boolean = True) As Object

            If String.IsNullOrWhiteSpace(St) Then Return If(asString, "--", 0)

            Dim cleanSt = St.Trim().ToUpperInvariant()

            ' === 1. Маппинг ТДЭ/ТД → ТЧЭ (иерархия депо) ===
            Dim tcheMap As New Dictionary(Of String, Integer) From {
                {"*МАРИИНСК*", 1},      ' ТДЭ Мариинск → ТЧЭ Боготол
                {"*САЯНСК*", 2},        ' ТДЭ Саянская → ТЧЭ Красноярск
                {"*РЕШОТ*", 3},         ' ТДЭ Решоты → ТЧЭ Иланская
                {"*УЖУР*", 5},          ' ТДЭ Ужур → ТЧЭ Ачинск
                {"*БИСКАМЖ*", 7},       ' ТДЭ Бискамжа → ТЧЭ Абакан
                {"*КОШУРНИК*", 7},      ' ТДЭ Кошурниково → ТЧЭ Абакан
                {"*МЕЖДУРЕЧЕНСК*", 7},  ' ТДЭ Междуреченск → ТЧЭ Абакан
                {"*АСКИЗ*", 7},         ' ТДЭ Аскиз → ТЧЭ Абакан
                {"*ОГОТО*", 1},         ' ТЧЭ Боготол
                {"*КРАСНОЯРС*", 2},     ' ТЧЭ Красноярск
                {"*ИЛАНСК*", 3},        ' ТЧЭ Иланская
                {"*АЧИНС*", 5},         ' ТЧЭ Ачинск
                {"*АБАКАН*", 7},        ' ТЧЭ Абакан
                {"*БОГОТОЛ*", 1}        ' ТЧЭ Боготол (полное название)
            }

            ' Ищем совпадение по паттернам
            For Each kvp In tcheMap
                If cleanSt Like kvp.Key Then
                    Return If(asString, $"ТЧЭ-{kvp.Value}", kvp.Value)
                End If
            Next

            ' === 2. Явный номер "ТЧЭ-X" в тексте ===
            Dim numMatch = Regex.Match(cleanSt, "ТЧЭ\s*[-]?\s*(\d+)", RegexOptions.IgnoreCase)
            If numMatch.Success AndAlso Integer.TryParse(numMatch.Groups(1).Value, Nothing) Then
                Dim num = CInt(numMatch.Groups(1).Value)
                Return If(asString, $"ТЧЭ-{num}", num)
            End If

            ' === 3. Не распознано ===
            Return If(asString, "--", 0)

        End Function


        '        ''' <summary>
        '        ''' Маппинг оборотных депо (ТДЭ) в эксплуатационные (ТЧЭ)
        '        ''' Ключ — шаблон поиска (Like-паттерн), Значение — номер ТЧЭ
        '        ''' </summary>
        '        Private Shared ReadOnly TDE_TO_TCHE_MAP As New Dictionary(Of String, Integer) From {
        '    {"*Мариинск*", 1},      ' ТДЭ Мариинск → ТЧЭ Боготол (ТЧЭ-1)
        '    {"*Саянск*", 2},        ' ТДЭ Саянская → ТЧЭ Красноярск (ТЧЭ-2)
        '    {"*Решот*", 3},         ' ТДЭ Решоты → ТЧЭ Иланская (ТЧЭ-3)
        '    {"*Ужур*", 5},          ' ТДЭ Ужур → ТЧЭ Ачинск (ТЧЭ-5)
        '    {"*Бискамж*", 7},       ' ТДЭ Бискамжа → ТЧЭ Абакан (ТЧЭ-7)
        '    {"*Кошурник*", 7},      ' ТДЭ Кошурниково → ТЧЭ Абакан
        '    {"*Междуреченск*", 7},  ' ТДЭ Междуреченск → ТЧЭ Абакан
        '    {"*Аскиз*", 7}          ' ТДЭ Аскиз → ТЧЭ Абакан (из вашего примера с ЛокоРемЗавод)
        '}





        ''' <summary>
        ''' Группировка по сериям локомотивов
        ''' </summary>
        ''' <param name="sourceString">исходная серия локомотива</param>
        ''' <returns>Сгруппированная серия - например 2(3)ТЭ10</returns>
        <Description("Группированные серии локомотивов")>
        Public Function TypLoka(ByVal sourceString As String) As String
            Select Case True
                Case sourceString.ToLower.Contains("вл80")
                    Return "ВЛ80в/и"
                Case InStr(sourceString, "вл60") > 0 ' включая ВЛ65
                    Return "ВЛ60"
                Case sourceString.ToLower.Contains("эс6")
                    Return "2[3]ЭС6"
                Case sourceString.ToLower.Contains("эс5к"), sourceString.ToLower.Contains("э5к")
                    Return "2[3]ЭС5К"
                Case sourceString.ToLower.Contains("вл85"), sourceString.ToLower.Contains("вл65")
                    Return "ВЛ85"
                Case sourceString.ToLower.Contains("эп")
                    Return "ЭП"
                Case sourceString.ToLower.Contains("тэ10"), sourceString.ToLower.Contains("тэ3"), sourceString.ToLower.Contains("тэ25")
                    Return "2[3]ТЭ10"
                Case sourceString.ToLower.Contains("тэм")
                    Return "ТЭМ"
                Case Else
                    Return ""
            End Select
        End Function





#End Region


        ''' <summary>
        ''' Извлекает серию, номер и приписку локомотива из текста (например: "3ЭС5К №978 (ТЧЭ Иланская)")
        ''' </summary>
        Private Sub ExtractLocomotiveIdentity(source As String)

            ' Паттерн: серия с префиксом, номер со слэшем, опциональная приписка
            Dim pattern = "([0-9,\.]*[А-ЯЁа-яё0-9\-]+)\s*№\s*([\d/]+)(?:\s*[\s,]*\(\s*([А-ЯЁа-яё0-9ёЁ\s\-]+?)\s*\))?"
            Dim m = Regex.Match(source, pattern, RegexOptions.IgnoreCase)

            If Not m.Success Then Return

            Dim series = m.Groups(1).Value.Trim()
            Dim numberRaw = m.Groups(2).Value.Trim()
            Dim pripis = If(m.Groups(3).Success, m.Groups(3).Value.Trim(), "")

            ' === Нормализация номера (убираем незначащие нули) ===
            Dim number As String = NormalizeLocoNumber(numberRaw)

            ' === ВСЕГДА перезаписываем — доверяем описанию ===
            If Not String.IsNullOrWhiteSpace(series) AndAlso series <> "Не указан" AndAlso Me.SerLokExact <> series Then
                Me.SerLokExact = series
                OnPropertyChanged(NameOf(SerLokExact))
            End If

            ' Теперь сравниваем уже очищенный номер
            If Not String.IsNullOrWhiteSpace(number) AndAlso number <> "Не указан" AndAlso Me.NumLok <> number Then
                Me.NumLok = number
                OnPropertyChanged(NameOf(NumLok))
            End If

            If Not String.IsNullOrWhiteSpace(pripis) AndAlso pripis.Length > 2 AndAlso pripis <> "Не указан" AndAlso Me.PripLok <> pripis Then
                Me.PripLok = pripis
                OnPropertyChanged(NameOf(PripLok))
            End If

        End Sub


        ''' <summary>
        ''' Убирает ведущие нули в номере локомотива.
        ''' Пример: "007" -> "7", "010/074" -> "10/74"
        ''' </summary>
        Private Function NormalizeLocoNumber(raw As String) As String
            If String.IsNullOrWhiteSpace(raw) Then Return raw

            ' Разбиваем по слэшу (для секций типа 101/102)
            Dim parts = raw.Split("/"c)

            For i As Integer = 0 To parts.Length - 1
                ' Удаляем ведущие нули, но оставляем один, если номер был "000"
                parts(i) = parts(i).TrimStart("0"c)
                If String.IsNullOrEmpty(parts(i)) Then parts(i) = "0"
            Next

            Return String.Join("/", parts)
        End Function


        ''' <summary>
        ''' Извлекает машиниста из описания и валидирует по депо расследования (KtoZakryl)
        ''' Если приписка из описания совпадает с KtoZakryl — перезаписывает MashPrip/PripMash
        ''' </summary>
        Private Sub ExtractAndValidateMashinist(source As String)

            ' === 1. Исправляем отсутствие пробела после "машинист" ===
            Dim corrected = Regex.Replace(source, "машинист(?=\S)", "машинист ", RegexOptions.IgnoreCase)

            ' === 2. Извлекаем машиниста ===
            Dim pattern = "машинист\s+([А-ЯЁ][а-яё\-]+)\s*(?:\(([^)]+)\))?"
            Dim m = Regex.Match(corrected, pattern, RegexOptions.IgnoreCase)
            If Not m.Success Then Return

            Dim surname = m.Groups(1).Value.Trim()
            Dim pripFromDesc = If(m.Groups(2).Success, m.Groups(2).Value.Trim(), "")

            ' === 3. Нормализуем приписку в родительское ТЧЭ (ТДЭ → ТЧЭ) ===
            Dim normalizedPrip As String = "Не указан"

            If Not String.IsNullOrWhiteSpace(pripFromDesc) Then
                Dim tcheNum = GetTCHE_Number(pripFromDesc)
                If tcheNum > 0 AndAlso TCHE_NAMES.ContainsKey(tcheNum) Then
                    normalizedPrip = TCHE_NAMES(tcheNum)
                Else
                    ' Не распознали — оставляем оригинальную приписку (лучше что-то, чем "Не указан")
                    normalizedPrip = pripFromDesc
                End If
            End If

            ' === 4. ВСЕГДА перезаписываем — доверяем описанию ===
            'If Not String.IsNullOrWhiteSpace(surname) AndAlso surname <> "Не указан" AndAlso Me.Mash <> surname Then
            '    Me.Mash = surname
            '    OnPropertyChanged(NameOf(Mash))
            'End If

            If Not String.IsNullOrWhiteSpace(surname) AndAlso surname <> "Не указан" Then
                ' Делаем первую заглавной, остальные строчными
                'Dim cleanSurname As String = Char.ToUpper(surname(0)) & surname.Substring(1).ToLower()
                Dim cleanSurname As String = NormFam(surname)

                If Me.Mash <> cleanSurname Then
                    Me.Mash = cleanSurname
                    OnPropertyChanged(NameOf(Mash))
                End If
            End If


            If Me.PripMash <> normalizedPrip Then
                Me.PripMash = normalizedPrip
                OnPropertyChanged(NameOf(PripMash))
            End If


        End Sub




        Public Sub ProcessOpisIntoHistory()

            If String.IsNullOrEmpty(Me.Opis) Then Return

            ' 1. Разделяем текст
            Dim splitResult = TextProcessor.SplitText(Me.Opis)
            Dim part1 = splitResult.Item1.Trim()
            Dim part2 = splitResult.Item2

            ' === НОВОЕ: Убираем ВСЕ невидимые и управляющие символы в part1 ===
            part1 = Regex.Replace(part1, "\p{C}+", " ")
            part1 = Regex.Replace(part1, "\u200B", "")
            part1 = Regex.Replace(part1, "\uFEFF", "")
            part1 = Regex.Replace(part1, "\u00AD", "")
            part1 = Regex.Replace(part1, "\u2028|\u2029", " ")
            part1 = Regex.Replace(part1, "\s+", " ").Trim()
            ' ==============================================================================

            ' 2. Извлекаем и валидируем машиниста
            ExtractAndValidateMashinist(part1)

            ' 3. Извлекаем локомотив
            ExtractLocomotiveIdentity(part1)

            ' 4. Очищаем описание
            Dim cleanedOpis = CleanDescriptionFromMachineData(part1)

            ' 5. ИЗВЛЕКАЕМ СТРУКТУРИРОВАННЫЕ ДАННЫЕ РЕМОНТОВ
            Dim repairsList = ExtractRemontData(cleanedOpis)

            ' 6. Удаляем ПОЛНУЮ ФРАЗУ "Данные на локомотив:..." если она есть
            If Not String.IsNullOrWhiteSpace(_remDataFullMatch) Then
                cleanedOpis = cleanedOpis.Replace(_remDataFullMatch, "").Trim()
            End If

            ' 7. Финальная очистка описания
            cleanedOpis = Regex.Replace(cleanedOpis, "\s+", " ").Trim()
            cleanedOpis = Regex.Replace(cleanedOpis, "^\s*[-,;:]+\s*|\s*[-,;:]+\s*$", "").Trim()

            ' 8. Обновляем свойства



            _DaNaLok.Clear()



            For Each repair In repairsList

                _DaNaLok.Add(repair)
            Next



            OnPropertyChanged(NameOf(DaNaLok))

            If _Opis <> cleanedOpis Then
                _Opis = cleanedOpis
                OnPropertyChanged(NameOf(Opis))
                OnPropertyChanged(NameOf(IsViolet))
            End If

            ' ==========================================
            ' 9. РАЗДЕЛЯЕМ ИСТОРИЮ И ПЛАНЫ
            ' ==========================================
            Dim events = TextProcessor.SplitEvents(part2)

            ' 9.1 Очищаем коллекции перед заполнением (чтобы при повторном парсинге не было дублей)
            Me.History.Clear()
            Me.Plan.Clear()

            ' 9.2 Проходим по событиям
            For Each ev In events
                Dim description As String = ev.Trim()
                If String.IsNullOrEmpty(description) Then Continue For

                Dim eventDate As DateTime = DateTime.MinValue
                Dim showDate As Boolean = False

                ' Парсим дату
                Dim dateMatch = Regex.Match(description, "^\s*-?\s*(\d{2}\.\d{2}\.\d{4})")

                If dateMatch.Success Then
                    Dim dateStr = dateMatch.Groups(1).Value
                    If DateTime.TryParseExact(dateStr, "dd.MM.yyyy", CultureInfo.InvariantCulture, Globalization.DateTimeStyles.None, eventDate) Then
                        Dim cutPos = dateMatch.Index + dateMatch.Length
                        If cutPos < description.Length Then
                            description = description.Substring(cutPos).TrimStart(" -", " ")
                        Else
                            description = String.Empty
                        End If
                        showDate = True
                    End If
                End If

                ' Если после вырезания даты ничего не осталось - пропускаем итерацию
                If String.IsNullOrWhiteSpace(description) Then Continue For

                ' ==========================================
                ' 9.5 ГЛАВНАЯ МАГИЯ: ФИЛЬТР ПЛАНОВ
                ' ==========================================

                ' Проверяем, является ли очищенный текст одним из эталонных планов
                ' (Обращаемся к Shared списку внутри класса PlanEntry)
                If PlanEntry.ReferenceReasons.Contains(description) Then

                    ' ЭТО ПЛАН! Добавляем в коллекцию Plan.
                    ' При присвоении .Description сработает Set в PlanEntry,
                    ' который сам проверит текст и, если это "На др дорогу", сделает IsGreen = True!
                    Dim PLe As New PlanEntry
                    With PLe
                        .Description = description
                    End With
                    Me.Plan.Add(New PlanEntry With {
                        .Description = description
                    })
                    Me.History.Insert(0, (New HistoryEntry With {.Description = PLe.DisplayText,
                        .ShowDate = False}))

                Else
                    ' ЭТО ИСТОРИЯ! Добавляем в коллекцию History
                    Me.History.Add(New HistoryEntry With {
                        .EventDate = eventDate,
                        .Description = description,
                        .ShowDate = showDate
                    })
                End If

            Next

        End Sub




        Private Function CleanDescriptionFromMachineData(source As String) As String


            If String.IsNullOrEmpty(source) Then Return source

            Dim result = source

            ' === 1. Исправляем пробел после "машинист" ===
            result = Regex.Replace(result, "машинист(?=\S)", "машинист ", RegexOptions.IgnoreCase)

            ' === 2. Удаляем локомотив (с префиксом секций и слэшами в номере) ===
            If Not String.IsNullOrWhiteSpace(Me.SerLokExact) AndAlso Me.SerLokExact <> "Не указан" AndAlso
               Not String.IsNullOrWhiteSpace(Me.NumLok) AndAlso Me.NumLok <> "Не указан" Then

                ' Паттерн: полная серия (с префиксом) + № + номер + (опционально скобки) + разделители
                Dim lokPattern = Regex.Escape(Me.SerLokExact) & "\s*№\s*" & Regex.Escape(Me.NumLok) &
                                 "(?:\s*[\s,]*\(\s*[^)]*?\s*\))?" &
                                 "[\s,;:]*"
                result = Regex.Replace(result, lokPattern, " ", RegexOptions.IgnoreCase).Trim()
            End If

            ' === 3. Удаляем машиниста ===
            If Me.Mash <> "Не указан" AndAlso Not String.IsNullOrWhiteSpace(Me.Mash) Then
                Dim surname = Me.Mash.Trim()
                Dim mashPattern = "машинист[\s,]*" & Regex.Escape(surname) & "\s*(?:\([^)]*\))?\s*[\s,;:]*"
                result = Regex.Replace(result, mashPattern, " ", RegexOptions.IgnoreCase).Trim()
            End If

            ' === 4. Чистим мусор ===
            result = result.Replace("  ", " ").Replace("  ", " ")
            result = result.Trim(","c, ";"c, ":"c, " "c, "-"c, "("c, ")"c)
            result = Regex.Replace(result, "\s+", " ").Trim()

            Return result.Trim()


        End Function




        Private _remDataFullMatch As String = String.Empty

        Private Function ExtractRemontData(source As String) As List(Of Remont)





            Dim result As New List(Of Remont)

            If String.IsNullOrEmpty(source) Then Return result

            Dim m = Regex.Match(source, "Данные на локомотив:\s*([^\r\n]*)", RegexOptions.IgnoreCase)
            If Not m.Success Then Return result

            Dim text = m.Groups(1).Value.Trim()
            _remDataFullMatch = m.Value.Trim()

            text = Regex.Replace(text, "[\r\n\t]", " ", RegexOptions.IgnoreCase)
            text = Regex.Replace(text, "\s+", " ").Trim()

            Dim pattern = "\b(ПОСТР|КР|СР|ТР-3|ТР-2|ТР-1|ТО-3|ТО-2)\b"
            Dim matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase)

            Dim i As Integer = 0
            While i < matches.Count
                Dim match = matches(i)
                Dim repairType = match.Groups(1).Value.ToUpper().Trim()

                ' Пропускаем "КР" в "Кр-Восточный"
                Dim endPos = match.Index + match.Length
                If endPos < text.Length AndAlso text(endPos) = "-"c Then
                    i += 1
                    Continue While
                End If

                ' === ЗАХВАТЫВАЕМ "с ТР-1" СРАЗУ В RepairPlace ===
                Dim blockStart = match.Index + match.Length
                Dim nextIdx = If(i < matches.Count - 1, matches(i + 1).Index, text.Length)
                Dim block = If(blockStart < nextIdx, text.Substring(blockStart, nextIdx - blockStart).Trim(), String.Empty)

                ' Если после ремонта идёт "с ТР-" или "с ТО-", захватываем ВЕСЬ фрагмент до конца следующего ремонта
                If i < matches.Count - 1 Then
                    Dim gapStart = match.Index + match.Length
                    Dim gapEnd = matches(i + 1).Index
                    Dim gap = text.Substring(gapStart, gapEnd - gapStart).Trim()

                    If gap = "с" OrElse gap = "c" Then
                        ' Захватываем "с ТР-1" целиком
                        Dim nextRepairEnd = matches(i + 1).Index + matches(i + 1).Length
                        block = text.Substring(blockStart, nextRepairEnd - blockStart).Trim()
                        i += 1 ' Пропускаем следующий ремонт (ТР-1)
                    End If
                End If

                Dim repair As New Remont With {.RepairType = repairType}
                '' === ДОБАВИТЬ ЭТУ СТРОКУ ===
                '' Передаем сырой блок текста, чтобы класс сам нашел пробег
                'repair.ExtractMileageFromSource(block)
                ' ============================
                If repairType = "ТО-2" Then
                    repair.Mileage = Nothing

                    repair.RepairPlace = CleanRepairPlace(block, isTO2:=True)
                Else
                    Dim mileageMatch = Regex.Match(block, "(\d+(?:\s+\d+)*)\s*км", RegexOptions.IgnoreCase)
                    If mileageMatch.Success Then
                        Dim cleanNumber = mileageMatch.Groups(1).Value.Replace(" ", "")
                        Dim mileageValue As Integer
                        If Integer.TryParse(cleanNumber, mileageValue) Then
                            repair.Mileage = mileageValue
                            block = block.Remove(mileageMatch.Index, mileageMatch.Length).Trim()
                        End If
                    End If

                    repair.RepairPlace = CleanRepairPlace(block, isTO2:=False)
                End If

                result.Add(repair)
                i += 1
            End While

            Return result

        End Function


        ' === ВСПОМОГАТЕЛЬНАЯ ФУНКЦИЯ ОЧИСТКИ ===
        Function CleanRepairPlace(block As String, isTO2 As Boolean) As String


            If String.IsNullOrWhiteSpace(block) Then Return String.Empty

            Dim cleaned = block.Trim()

            ' Удаляем временные метки
            cleaned = Regex.Replace(cleaned, "\d{1,2}[\.\/]\d{1,2}[-\/]\d{1,2}:\d{1,2}", " ", RegexOptions.IgnoreCase)
            cleaned = Regex.Replace(cleaned, "\d{1,2}[\.\/]\d{1,2}[-\/]\d{1,2}", " ", RegexOptions.IgnoreCase)

            ' Удаляем мусор
            cleaned = Regex.Replace(cleaned, "\d{4}\s*г\.?", " ", RegexOptions.IgnoreCase)
            cleaned = Regex.Replace(cleaned, "\b\d{1,2}[\.\/\-]\d{1,2}(?:[\.\/\-]\d{2,4})?\b", " ", RegexOptions.IgnoreCase)
            cleaned = Regex.Replace(cleaned, "\b\d{1,2}:\d{1,2}\b", " ", RegexOptions.IgnoreCase)
            cleaned = Regex.Replace(cleaned, "\b(проходил|пробег|год|вр|в|на|г\.?|от|до|по|у|к|из|за|над|под|для)\b", " ", RegexOptions.IgnoreCase)

            If Not isTO2 Then
                cleaned = Regex.Replace(cleaned, "\bс\b", " ", RegexOptions.IgnoreCase)
            End If

            ' Удаляем отдельные цифры, НО сохраняем дефисы в названиях
            cleaned = Regex.Replace(cleaned, "(?<![а-яА-ЯёЁa-zA-Z№#\-])\d+(?![а-яА-ЯёЁa-zA-Z0-9\-])", " ", RegexOptions.IgnoreCase)

            ' Удаляем мусорные символы, НО сохраняем буквы, цифры, дефисы, № и пробелы
            cleaned = Regex.Replace(cleaned, "[^а-яА-ЯёЁa-zA-Z0-9№\-\s]", " ", RegexOptions.IgnoreCase)

            ' === УДАЛЕНО: НЕ удаляем короткие слова (раньше ломало "Кр-Восточный") ===
            ' Было: cleaned = Regex.Replace(cleaned, "\b(?!(СЛД|СО|ТЧ|ПТОЛ|СУ)\b)[а-яА-ЯёЁa-zA-Z]{1,2}\b", " ", RegexOptions.IgnoreCase)

            cleaned = Regex.Replace(cleaned, "\s+", " ").Trim()
            cleaned = cleaned.Replace("c", "с")

            Return cleaned




        End Function

        '======================================================================================
        '======================= для истории отказа ===========================================
        Private _history As ObservableCollection(Of HistoryEntry)
        Private _historyHandler As NotifyCollectionChangedEventHandler
        Public Property History As ObservableCollection(Of HistoryEntry)
            Get
                If _history Is Nothing Then
                    _history = New ObservableCollection(Of HistoryEntry)()
                    _historyHandler = AddressOf OnHistoryChanged          ' ← подписка
                    AddHandler _history.CollectionChanged, _historyHandler
                End If
                Return _history
            End Get
            Set(value As ObservableCollection(Of HistoryEntry))
                If _history IsNot value Then
                    ' Отписка от старой коллекции
                    If _history IsNot Nothing AndAlso _historyHandler IsNot Nothing Then
                        RemoveHandler _history.CollectionChanged, _historyHandler
                    End If

                    _history = value

                    ' Подписка на новую коллекцию
                    If _history IsNot Nothing Then
                        _historyHandler = AddressOf OnHistoryChanged      ' ← подписка
                        AddHandler _history.CollectionChanged, _historyHandler
                    End If

                    OnPropertyChanged(NameOf(History))
                    RefreshVernulsaOTS()                                  ' ← ВЫЗОВ №1: коллекцию подменили целиком
                End If
            End Set
        End Property


        ' ═══════════════════════════════════════════════════════════
        ' ОБРАБОТЧИК ИЗМЕНЕНИЯ КОЛЛЕКЦИИ
        ' ═══════════════════════════════════════════════════════════
        Private Sub OnHistoryChanged(sender As Object, e As NotifyCollectionChangedEventArgs)
            OnPropertyChanged(NameOf(History))
            RefreshVernulsaOTS()                                          ' ← ВЫЗОВ №2: добавили/удалили запись
        End Sub


        ' ═══════════════════════════════════════════════════════════
        ' ОБНОВЛЕНИЕ ДАТЫ ИЗ ИСТОРИИ
        ' ═══════════════════════════════════════════════════════════
        Private Sub RefreshVernulsaOTS()
            Dim entry As HistoryEntry = GetLastRelevantHistoryEntry()
            If entry Is Nothing Then Return
            If Not Uchet Then Return

            Dim match As Match = DateRegex.Match(entry.DisplayText)
            If match.Success Then
                Dim entryDate As Date = Date.ParseExact(match.Value, "dd.MM.yyyy", Globalization.CultureInfo.InvariantCulture)

                ' Пишем НАПРЯМУЮ в поле, без валидации
                If _VernulsaOTS <> entryDate Then
                    _VernulsaOTS = entryDate
                    OnPropertyChanged(NameOf(VernulsaOTS))
                End If
            End If
        End Sub


        ' ═══════════════════════════════════════════════════════════
        ' ПОИСК ПОСЛЕДНЕЙ ПОДХОДЯЩЕЙ ЗАПИСИ (с конца)
        ' ═══════════════════════════════════════════════════════════
        Private Function GetLastRelevantHistoryEntry() As HistoryEntry
            If History Is Nothing Then Return Nothing

            For i As Integer = History.Count - 1 To 0 Step -1
                Dim entry As HistoryEntry = History(i)
                If entry IsNot Nothing AndAlso IsRelevantHistoryEntry(entry.DisplayText) Then
                    Return entry
                End If
            Next

            Return Nothing
        End Function




        ' 1. Регулярка для даты в начале строки (добавлен ^ для скорости и безопасности)
        Private Shared ReadOnly DateRegex As New Regex("^\d{2}\.\d{2}\.\d{4}", RegexOptions.Compiled)

        ' 2. Регулярка для извлечения кода депо ОТКУДА (после слова "передан" до первой запятой)
        ' СТАЛО (работает с дефисами):
        Private Shared ReadOnly DepotRegex As New Regex("передан\s+([А-Я][А-Я\-0-9]*)\s*(?:=>|,)\s*([А-Я][А-Я\-0-9]*)", RegexOptions.Compiled)






        ' ═══════════════════════════════════════════════════════════
        ' ФИЛЬТР ЗАПИСИ
        ' ═══════════════════════════════════════════════════════════
        Private Function IsRelevantHistoryEntry(text As String) As Boolean


            If String.IsNullOrEmpty(text) Then Return False

            If text.Contains("поступил") Then Return True

            If text.Contains("передан") Then
                Dim match As Match = DepotRegex.Match(text)
                If match.Success Then
                    Dim depotFrom As String = match.Groups(1).Value
                    Dim depotTo As String = match.Groups(2).Value

                    ' Вариант 1: ТР/ТРПУ передаёт на наше депо (ТЧЭ)
                    If depotFrom.StartsWith("ТР", StringComparison.OrdinalIgnoreCase) AndAlso
                       depotTo.StartsWith("ТЧЭ", StringComparison.OrdinalIgnoreCase) Then
                        Return True
                    End If

                    ' Вариант 2: Другая дорога (не КРАС и не ТЧЭ) передаёт нам
                    If depotFrom <> "КРАС" AndAlso
                       Not depotFrom.StartsWith("ТЧЭ", StringComparison.OrdinalIgnoreCase) AndAlso
                       depotFrom.Length > 1 AndAlso
                       Me.Uchet Then
                        Return True
                    End If
                End If
            End If

            Return False
        End Function



        Private _Plan As ObservableCollection(Of PlanEntry)

        Public Property Plan As ObservableCollection(Of PlanEntry)
            Get
                If _Plan Is Nothing Then
                    _Plan = New ObservableCollection(Of PlanEntry)()
                    ' Подписка для автоматического OnPropertyChanged при изменении коллекции
                    AddHandler _Plan.CollectionChanged, Sub()
                                                            OnPropertyChanged(NameOf(Plan))
                                                        End Sub
                End If
                Return _Plan
            End Get
            Set(value As ObservableCollection(Of PlanEntry))
                If _Plan IsNot value Then
                    If _Plan IsNot Nothing Then RemoveHandler _Plan.CollectionChanged, Nothing
                    _Plan = value
                    If _Plan IsNot Nothing Then
                        AddHandler _Plan.CollectionChanged, Sub()
                                                                OnPropertyChanged(NameOf(Plan))
                                                            End Sub
                    End If
                    OnPropertyChanged(NameOf(Plan))
                End If
            End Set
        End Property


        Public Sub NotifyHistoryChanged()
            OnPropertyChanged(NameOf(History))
        End Sub

        ' Текущая выбранная запись (для UI)
        Private _selectedHistoryEntry As HistoryEntry

        Public Sub LoadData()
            ' ... ваша логика загрузки данных из БД ...
            ' ... заполнение History ...

            RefreshVernulsaOTS()                                          ' ← ВЫЗОВ №3: после загрузки данных
        End Sub

        <JsonIgnore>
        Public Property SelectedHistoryEntry As HistoryEntry
            Get
                Return _selectedHistoryEntry
            End Get
            Set(value As HistoryEntry)
                If _selectedHistoryEntry IsNot value Then
                    _selectedHistoryEntry = value
                    OnPropertyChanged(NameOf(SelectedHistoryEntry))
                    OnPropertyChanged(NameOf(CanMoveHistoryUp))
                    OnPropertyChanged(NameOf(CanMoveHistoryDown))
                End If
            End Set
        End Property

        ' Команды для кнопок (можно привязать напрямую в XAML через RelayCommand или простые Sub)
        Public ReadOnly Property CanMoveHistoryUp As Boolean
            Get
                Dim idx = History.IndexOf(SelectedHistoryEntry)
                Return idx > 0
            End Get
        End Property

        Public ReadOnly Property CanMoveHistoryDown As Boolean
            Get
                Dim idx = History.IndexOf(SelectedHistoryEntry)
                Return idx >= 0 AndAlso idx < History.Count - 1
            End Get
        End Property

        Public Sub MoveHistoryUp()
            Dim idx = History.IndexOf(SelectedHistoryEntry)
            If idx > 0 Then
                Dim item = History(idx)
                History.RemoveAt(idx)
                History.Insert(idx - 1, item)
                SelectedHistoryEntry = item ' сохраняем выделение
            End If
        End Sub

        Public Sub MoveHistoryDown()
            Dim idx = History.IndexOf(SelectedHistoryEntry)
            If idx >= 0 AndAlso idx < History.Count - 1 Then
                Dim item = History(idx)
                History.RemoveAt(idx)
                History.Insert(idx + 1, item)
                SelectedHistoryEntry = item ' сохраняем выделение
            End If
        End Sub

        Public Sub AddHistoryEntry(eventDate As DateTime, description As String)
            Dim entry = New HistoryEntry With {
        .EventDate = eventDate,
        .Description = description
    }
            History.Add(entry)
            SelectedHistoryEntry = entry ' авто-выделение новой записи
        End Sub

        Public Sub RemoveHistoryEntry(entry As HistoryEntry)
            If entry IsNot Nothing AndAlso History.Contains(entry) Then
                History.Remove(entry)
                SelectedHistoryEntry = Nothing
            End If
        End Sub

        '=================================================
        Public Function GetHistoryEntryByDisplayText(displayText As String) As HistoryEntry
            If History Is Nothing Then Return Nothing

            For Each item As HistoryEntry In History
                If item IsNot Nothing AndAlso
           String.Equals(item.DisplayText, displayText, StringComparison.Ordinal) Then
                    Return item
                End If
            Next

            Return Nothing
        End Function

        Public Function GetPlanEntryByDescription(description As String) As PlanEntry
            If Plan Is Nothing Then Return Nothing

            For Each item As PlanEntry In Plan
                If item IsNot Nothing AndAlso
           String.Equals(item.Description, description, StringComparison.Ordinal) Then
                    Return item
                End If
            Next

            Return Nothing
        End Function

        Public Function GetHistoryEntryByDescription(description As String) As HistoryEntry
            If History Is Nothing Then Return Nothing

            For Each item As HistoryEntry In History
                If item IsNot Nothing AndAlso
           String.Equals(item.Description, description, StringComparison.Ordinal) Then
                    Return item
                End If
            Next

            Return Nothing
        End Function

        ''' <summary>
        ''' ищет в ToLower
        ''' </summary>
        ''' <param name="description"></param>
        ''' <returns></returns>
        Public Function GetContainsHistoryEntryByDescription(description As String) As HistoryEntry
            If History Is Nothing Then Return Nothing

            For i = History.Count - 1 To 0 Step -1
                If History(i) IsNot Nothing AndAlso History(i).DisplayText.ToLower.Contains(description) Then Return History(i)
            Next

            Return Nothing
        End Function

        Public Sub RemoveHistoryEntriesByDescription(description As String)
            If History Is Nothing Then Return

            Dim selectedRemoved As Boolean = False

            ' Идём с конца, чтобы безопасно удалять элементы из коллекции
            For i As Integer = History.Count - 1 To 0 Step -1
                Dim item As HistoryEntry = History(i)

                If item IsNot Nothing AndAlso
           String.Equals(item.Description, description, StringComparison.Ordinal) Then

                    If ReferenceEquals(item, SelectedHistoryEntry) Then
                        selectedRemoved = True
                    End If

                    History.RemoveAt(i)
                End If
            Next

            If selectedRemoved Then
                SelectedHistoryEntry = Nothing
            End If
        End Sub

        Public Sub RemoveHistoryEntriesByDescription(entry As HistoryEntry)
            If entry Is Nothing Then Return

            RemoveHistoryEntriesByDescription(entry.Description)
        End Sub

        Public Function HasHistoryEntry(description As String) As Boolean
            If History Is Nothing Then Return False

            For Each item As HistoryEntry In History
                If item IsNot Nothing AndAlso
           String.Equals(item.Description, description, StringComparison.Ordinal) Then
                    Return True
                End If
            Next

            Return False
        End Function
        '=================================================

        Public Function GetInspectorData() As List(Of InspectorItem)
            Dim items As New List(Of InspectorItem)

            ' Свойства
            Dim props = Me.GetType().GetProperties(Reflection.BindingFlags.Public Or Reflection.BindingFlags.Instance)
            For Each prop In props
                Dim value As String = ""
                Try
                    Dim val = prop.GetValue(Me)
                    If val Is Nothing Then
                        value = "<Nothing>"
                    ElseIf TypeOf val Is IEnumerable(Of Object) AndAlso Not TypeOf val Is String Then
                        value = $"[Коллекция: {CType(val, IEnumerable).Cast(Of Object).Count()} эл.]"
                    Else
                        value = val.ToString()
                    End If
                Catch ex As Exception
                    value = $"<Ошибка: {ex.Message}>"
                End Try

                items.Add(New InspectorItem With {
                    .Category = "Свойство",
                    .Name = prop.Name,
                    .Type = prop.PropertyType.Name,
                    .Value = value
                })
            Next
            Return items.OrderBy(Function(i) i.Category).ThenBy(Function(i) i.Name).ToList()
        End Function

        '======================================================================================
        '======================================================================================
    End Class



End Namespace

