Imports KACAHT_Next2.Kas.PeriodBlockControl

Namespace Kas

    Module Zaprosy
        Public YarCon As YarlykContainerControl
        Public currentSelectionCriteria As Func(Of Otkaz, Boolean) = Nothing

#Region "Основные запросы"
        ' Базовая проверка по периоду
        Public BaseFilter As Func(Of Otkaz, Boolean) = Function(o) IsWithinPeriod(o.Nach)


        Public AllOTS As Func(Of Otkaz, Boolean) = Function(o) BaseFilter(o)



        Public OTSInKomplex As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso o.Uchet)
        Public OTSOutNotPeredanKomplex As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso (Not o.Uchet AndAlso (Not o.ZaKemCode?.ToLower Like "*дорог*") AndAlso (Not o.ZaKemCode?.ToLower Like "*ехнолог*")))

        Public UpTo10DaysRassled As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso (o.DaysOnRassled > 10 And o.Uchet And o.ZaKem?.ToLower <> "тр"))
        Public UpTo10DaysZakryt As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso ((o.Zakryt - o.Nach).Days > 10 And o.Uchet And o.ZaKem?.ToLower <> "тр"))
        Public UpTo10DaysonCreateZakryt As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso ((o.Zakryt - o.Sozdan).Days > 10 And o.Uchet And o.ZaKem?.ToLower <> "тр"))



        Public VRassForTCH As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso (o.VRassled))
        Public Sohranen As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso (o.IsSaved))

        'здесь на дату расследования можно не проверять
        Public SignedOnTCH As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso o.ZaKem?.ToLower = ("тчэ"))
        Public SignedOnTRPU As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso o.KtoZakryl?.ToLower Like "трпу*" AndAlso o.Uchet)
        Public SignedOnSLD As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso o.ZaKemCode?.ToLower Like "слд*")
        Public SignedOnZav As Func(Of Otkaz, Boolean) = Function(o) BaseFilter(o) AndAlso (Not String.IsNullOrEmpty(o.ZaKemCode) AndAlso ({"окорем", "окострой"}.Any(Function(k) o.ZaKemCode?.ToLower().Contains(k)) OrElse o.ZaKemCode?.ToLower() = "прочие"))

        Public IsPFB As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso o.IsPFB AndAlso o.AlienSLD = "")

        'именно *дорога - чтобы др дорога/П и т.п. не попадали сюда
        Public ToOtherRails As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso o.ZaKemCode?.ToLower Like "*дорога")
        Public ToTechnoOTS As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso o.ZaKemCode?.ToLower Like "*ехноло*")
        Public ToOtherPredprKras As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso o.ZaKemCode?.ToLower Like "*дорога/*")

        Public OTSOnKras As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso o.MestoOTS_Dor?.ToLower Like "*расноя*")
        Public OTSOnOther As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso (Not (o.MestoOTS_Dor?.ToLower Like "*расноя*")))

        'Public RassOver As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso ((o.Uslovie2 Or o.ZaKemCode?.ToLower = "тч") And o.Zakryt > Date.MinValue))
        Public DangerOTS As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso o.ISDanger)
        Public OkaPomOTS As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso o.OkaPom)
        Public WithPass As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso o.HasPass AndAlso (Not o.KtoZakryl?.ToLower Like "трпу*"))
        Public WithAlienSLD As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso o.AlienSLD <> "")
        Public Sobyti As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso o.ISSobyt)
        Public Korporativ As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso o.ISKorp)
        Public KorrectPCH As Func(Of Otkaz, Boolean) = (Function(o) BaseFilter(o) AndAlso o.IsKorrect AndAlso o.KtoZakryl.ToLower.Contains("тч") AndAlso (o.Uslovie4))
        Public NarushSroka As Func(Of Otkaz, Boolean) = Function(u) BaseFilter(u) AndAlso u.NarushSroka
#End Region



#Region "Запросы для суточ отчета"
        'в отчеты еще добавлять существующие - ПФБ, ПАСС/ПРИГ
        Public PostupilZaPeriod As Func(Of Otkaz, Boolean) = (Function(o) (o.Postup.IsInPeriod OrElse o.VernulsaOTS.IsInPeriod) AndAlso o.Uchet AndAlso (Not o.KtoZakryl?.ToLower Like "трпу*"))
        Public ZakrytZaPeriod As Func(Of Otkaz, Boolean) = (Function(o) o.Zakryt.IsInPeriod AndAlso (Not o.KtoZakryl?.ToLower Like "трпу*"))
        Public ToTechnoZaPeriod As Func(Of Otkaz, Boolean) = (Function(o) o.ZaKemCode?.ToLower Like "*ехноло*" AndAlso ZakrytZaPeriod(o))
        Public PeredanZaPeriod As Func(Of Otkaz, Boolean) = (Function(o) o.Peredan.IsInPeriod)
        Public KorrZaPeriod As Func(Of Otkaz, Boolean) = (Function(o) o.KorDate.IsInPeriod)
        Public WithPassZaPeriod As Func(Of Otkaz, Boolean) = (Function(o) o.HasPass AndAlso ((o.VRassled) OrElse (o.IsSaved)) AndAlso (Not (o.MestoOTS_Dor?.ToLower Like "*расноя*") AndAlso (Not o.KtoZakryl?.ToLower Like "трпу*")))

        Public UpTo10DaysZaPeriod As Func(Of Otkaz, Boolean) = (Function(o) o.DaysOnRassled > 9 AndAlso o.ZaKem?.ToLower <> "тр")


        'Функция, которая создаёт предикат под конкретный KtoRass
        Public Function GetVRassZaPeriod(ktoRass As String) As Func(Of Otkaz, Boolean)
            ' Возвращаем стандартный Func(Of Otkaz, Boolean), но внутри используем ktoRass
            Return Function(o) (o.VRassled OrElse o.IsSaved) AndAlso o.KtoZakryl = ktoRass
        End Function


#End Region





















        'Если период не установлен в параметрах, IsWithinPeriod всегда возвращает True, и фильтры работают как обычно. Если период установлен, фильтрация выполняется по периоду из параметров (My.Settings).
        Private Function IsWithinPeriod(dateToCheck As Date?) As Boolean
            If dateToCheck.HasValue Then
                ' Дата существует — проверяем её на соответствие периоду
                With My.Settings
                    If .NachPeriod = Date.MinValue OrElse .KonPeriod = Date.MinValue Then
                        Return True ' Период не установлен — пропускаем проверку
                    Else
                        Return dateToCheck.Value >= .NachPeriod AndAlso dateToCheck.Value <= .KonPeriod
                    End If
                End With
            Else
                ' Дата отсутствует (Nothing) — объект не проходит фильтрацию
                Return False
            End If
        End Function


        'добавление выбранного временного промежутка (18-18 или 00-23:59) к выбранным датам
        Public Function ApplyTimeMode(startDate As Date, endDate As Date) As (Date, Date)
            If startDate = Nothing OrElse endDate = Nothing Then
                ' ← Это невозможно? DateTime — структура, НО: если вы передаёте Nullable(Of DateTime), то может быть Nothing!
                Throw New ArgumentException("startDate и endDate не могут быть Nothing")
            End If

            ' Также: если вы где-то используете глобальные объекты — проверяйте их:
            If MW Is Nothing Then
                ' В дизайнере MW = Nothing
                Return (startDate, endDate) ' или выбросить исключение, но лучше — просто не применять коррекцию
            End If


            Select Case MW.DContrPop.PeriodBC.CurrentTimeMode
                Case TimeMode.From18To18
                    ' С 18:00 до 18:00
                    Dim startDateTime = startDate.Date.AddHours(18)
                    Dim endDateTime = endDate.Date.AddHours(18)
                    Return (startDateTime, endDateTime)

                Case TimeMode.From00To2359
                    ' С 00:00 до 23:59
                    Dim startDateTime = startDate.Date
                    Dim endDateTime = endDate.Date.AddHours(23).AddMinutes(59)
                    Return (startDateTime, endDateTime)

                Case Else
                    ' По умолчанию — с 00:00 до 23:59
                    Dim startDateTime = startDate.Date
                    Dim endDateTime = endDate.Date.AddHours(23).AddMinutes(59)
                    Return (startDateTime, endDateTime)
            End Select
        End Function




    End Module

End Namespace

