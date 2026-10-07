Namespace Kas

    Partial Public Class YarlykContainerControl
        Sub New()
            InitializeComponent()
            YarCon = Me
        End Sub

        Private Sub UserControl_Loaded(sender As Object, e As RoutedEventArgs)
            UpdateYarlykInfo()
        End Sub

        Public Sub UpdateYarlykInfo()
            SetYarlykFilter(AllOTSYAR, AllOTS)
            SetYarlykFilter(OTSInKomplYAR, OTSInKomplex)
            SetYarlykFilter(Over10Days, UpTo10DaysRassled)
            SetYarlykFilter(Over10Zakr, UpTo10DaysZakryt)
            SetYarlykFilter(DangerOTSYar, DangerOTS)
            SetYarlykFilter(InOthRail, ToOtherRails)
            SetYarlykFilter(ToOthPredKras, ToOtherPredprKras)
            SetYarlykFilter(OnKRASRail, OTSOnKras)
            SetYarlykFilter(OnOtherRails, OTSOnOther)
            SetYarlykFilter(RassForTCH, VRassForTCH)
            SetYarlykFilter(SohrYAR, Sohranen)
            SetYarlykFilter(OTS_ON_TCH, SignedOnTCH)
            SetYarlykFilter(OTS_ON_TRPU, SignedOnTRPU)
            SetYarlykFilter(OTS_ON_SLD, SignedOnSLD)
            SetYarlykFilter(OTS_ON_Zavod_and_Proch, SignedOnZav)
            SetYarlykFilter(OTSOutKomplNotPeredanYAR, OTSOutNotPeredanKomplex)
            SetYarlykFilter(ToTechnology, ToTechnoOTS)
            SetYarlykFilter(OkaPomOTSYar, OkaPomOTS)
            SetYarlykFilter(HasPasYar, WithPass)
            SetYarlykFilter(HasAlienSLD, WithAlienSLD)
            SetYarlykFilter(KorporativYar, Korporativ)
            SetYarlykFilter(SobytiYar, Sobyti)
            SetYarlykFilter(KorrP, KorrectPCH)
            SetYarlykFilter(NarushSrokYar, NarushSroka)
            SetYarlykFilter(HasPFB, IsPFB)

            RecalculateAllYarlyks()
        End Sub

        Private Sub SetYarlykFilter(ctrl As Yarlyk, filter As Func(Of Otkaz, Boolean))
            If ctrl IsNot Nothing Then ctrl.Zapros = filter
        End Sub

        Public Sub RecalculateAllYarlyks()
            Dim allYarlyks As New List(Of Yarlyk) From {
                AllOTSYAR, OnKRASRail, OnOtherRails, SohrYAR, OTSOutKomplNotPeredanYAR,
                OTSInKomplYAR, RassForTCH, OTS_ON_TCH, OTS_ON_TRPU, OTS_ON_Zavod_and_Proch, OTS_ON_SLD,
                Over10Days, Over10Zakr, InOthRail, ToOthPredKras, ToTechnology, KorrP,
                SobytiYar, KorporativYar, DangerOTSYar, NarushSrokYar, OkaPomOTSYar, HasAlienSLD, HasPasYar, HasPFB
            }
            For Each y In allYarlyks
                y?.Recalculate()
            Next
        End Sub

        Sub ProcessingMDown(sender As Yarlyk)
            ' 1. Сброс фильтров
            MW.TRowsContainer.ResetAllFilters()
            PointedYarlyk = sender
            currentSelectionCriteria = PointedYarlyk.Zapros

            If currentSelectionCriteria IsNot Nothing AndAlso OTSList IsNot Nothing Then
                ' 2. Полная очистка источника перед новой загрузкой
                MW.TRowsContainer.ItemsSource = Nothing

                ' 3. Подготовка данных
                Dim filteredData = OTSList.Where(currentSelectionCriteria).OrderBy(Function(u) u.Nach).ToList()

                ' 4. Назначение новых данных
                MW.TRowsContainer.ItemsSource = filteredData
                filteredData = Nothing
            End If

            ' 5. Асинхронная очистка памяти, чтобы не морозить интерфейс
            Task.Run(Sub()
                         System.Threading.Thread.Sleep(100) ' Небольшая пауза для завершения рендеринга
                         ForceMemoryCleanup()
                     End Sub)

        End Sub

        Sub YarlykPressHandler(sender As Object, e As MouseButtonEventArgs)
            ProcessingMDown(CType(sender, Yarlyk))
        End Sub

        Public Shared Sub ForceMemoryCleanup()
            GC.Collect()
            GC.WaitForPendingFinalizers()
            GC.Collect()
        End Sub


        'Sub New()

        '    ' Этот вызов является обязательным для конструктора.
        '    InitializeComponent()

        '    ' Добавить код инициализации после вызова InitializeComponent().
        '    YarCon = Me

        'End Sub

        'Private Sub UserControl_Loaded(sender As Object, e As RoutedEventArgs)
        '    UpdateYarlykInfo()
        'End Sub

        'Public Sub UpdateYarlykInfo()
        '    SetYarlykData(AllOTSYAR, AllOTS)
        '    SetYarlykData(OTSInKomplYAR, OTSInKomplex)

        '    SetYarlykData(Over10Days, UpTo10DaysRassled)
        '    SetYarlykData(Over10Zakr, UpTo10DaysZakryt)


        '    SetYarlykData(DangerOTSYar, DangerOTS)
        '    SetYarlykData(InOthRail, ToOtherRails)
        '    SetYarlykData(ToOthPredKras, ToOtherPredprKras)
        '    SetYarlykData(OnKRASRail, OTSOnKras)
        '    SetYarlykData(OnOtherRails, OTSOnOther)

        '    SetYarlykData(RassForTCH, VRassForTCH)
        '    SetYarlykData(SohrYAR, Sohranen)
        '    SetYarlykData(OTS_ON_TCH, SignedOnTCH)
        '    SetYarlykData(OTS_ON_TRPU, SignedOnTRPU)
        '    SetYarlykData(OTS_ON_SLD, SignedOnSLD)
        '    SetYarlykData(OTS_ON_Zavod_and_Proch, SignedOnZav)


        '    SetYarlykData(OTSOutKomplNotPeredanYAR, OTSOutNotPeredanKomplex)
        '    SetYarlykData(ToTechnology, ToTechnoOTS)


        '    SetYarlykData(OkaPomOTSYar, OkaPomOTS)
        '    SetYarlykData(HasPasYar, WithPass)

        '    SetYarlykData(HasAlienSLD, WithAlienSLD)
        '    SetYarlykData(KorporativYar, Korporativ)
        '    SetYarlykData(SobytiYar, Sobyti)
        '    SetYarlykData(KorrP, KorrectPCH)
        '    SetYarlykData(NarushSrokYar, NarushSroka)
        '    SetYarlykData(HasPFB, IsPFB)
        'End Sub

        'Sub SetYarlykData(Contrl As Yarlyk, Zapros As Func(Of Otkaz, Boolean))
        '    Contrl.Zapros = Zapros
        '    'остальное внутри контрола задается
        'End Sub

        'Sub ProcessingMDown(sender As Yarlyk)

        '    'MW.TRowsContainer.ClearAll0LevelFilters()
        '    'MW.TRowsContainer.THed.ClearAllFilters()
        '    MW.TRowsContainer.ResetAllFilters()

        '    PointedYarlyk = sender
        '    currentSelectionCriteria = PointedYarlyk.Zapros 'AllOTS
        '    MW.TRowsContainer.ItemsSource = OTSList.Where(currentSelectionCriteria).OrderBy(Function(u) u.Nach).ToList



        'End Sub



        'Sub YarlykPressHandler(sender As Object, e As MouseButtonEventArgs)
        '    ProcessingMDown(CType(sender, Yarlyk))
        'End Sub

        'Private Sub RassForTCH_Loaded(sender As Object, e As RoutedEventArgs) Handles RassForTCH.Loaded

        'End Sub
    End Class
End Namespace

