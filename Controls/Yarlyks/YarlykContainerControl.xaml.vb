Namespace Kas

    Partial Public Class YarlykContainerControl


        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().
            YarCon = Me

        End Sub

        Private Sub UserControl_Loaded(sender As Object, e As RoutedEventArgs)
            UpdateYarlykInfo()
        End Sub

        Public Sub UpdateYarlykInfo()
            SetYarlykData(AllOTSYAR, AllOTS)
            SetYarlykData(OTSInKomplYAR, OTSInKomplex)

            SetYarlykData(Over10Days, UpTo10DaysRassled)
            SetYarlykData(Over10Zakr, UpTo10DaysZakryt)
            'SetYarlykData(Over10CreateZakr, UpTo10DaysonCreateZakryt)

            SetYarlykData(DangerOTSYar, DangerOTS)
            SetYarlykData(InOthRail, ToOtherRails)
            SetYarlykData(ToOthPredKras, ToOtherPredprKras)
            SetYarlykData(OnKRASRail, OTSOnKras)
            SetYarlykData(OnOtherRails, OTSOnOther)

            SetYarlykData(RassForTCH, VRassForTCH)
            SetYarlykData(SohrYAR, Sohranen)
            SetYarlykData(OTS_ON_TCH, SignedOnTCH)
            SetYarlykData(OTS_ON_TRPU, SignedOnTRPU)
            SetYarlykData(OTS_ON_SLD, SignedOnSLD)
            SetYarlykData(OTS_ON_Zavod_and_Proch, SignedOnZav)


            SetYarlykData(OTSOutKomplNotPeredanYAR, OTSOutNotPeredanKomplex)
            SetYarlykData(ToTechnology, ToTechnoOTS)


            SetYarlykData(OkaPomOTSYar, OkaPomOTS)
            SetYarlykData(HasPasYar, WithPass)

            SetYarlykData(HasAlienSLD, WithAlienSLD)
            SetYarlykData(KorporativYar, Korporativ)
            SetYarlykData(SobytiYar, Sobyti)
            SetYarlykData(KorrP, KorrectPCH)
            SetYarlykData(NarushSrokYar, NarushSroka)
            SetYarlykData(HasPFB, IsPFB)
        End Sub

        Sub SetYarlykData(Contrl As Yarlyk, Zapros As Func(Of Otkaz, Boolean))
            Contrl.Zapros = Zapros
            'остальное внутри контрола задается
        End Sub

        Sub ProcessingMDown(sender As Yarlyk)

            'MW.TRowsContainer.ClearAll0LevelFilters()
            'MW.TRowsContainer.THed.ClearAllFilters()
            MW.TRowsContainer.ResetAllFilters()

            PointedYarlyk = sender
            currentSelectionCriteria = PointedYarlyk.Zapros 'AllOTS
            MW.TRowsContainer.ItemsSource = OTSList.Where(currentSelectionCriteria).OrderBy(Function(u) u.Nach).ToList



        End Sub





        'Public Sub UpdateTableContainer(data As IEnumerable(Of Otkaz))

        '    'Stopwatch.Reset()
        '    'Stopwatch.Start()

        '    ' Присваиваем новые данные
        '    MW.TRowsContainer.ItemsSource = data.OrderBy(Function(u) u.Nach).ToList()

        '    'Stopwatch.Stop()
        '    'Dim elapsedTime As TimeSpan = Stopwatch.Elapsed

        '    If Not IsNothing(PointedOtkaz) Then
        '        MW.TRowsContainer.ScrollToPointedOTS()
        '    End If

        '    'MW.InfoBLOK.AddItem($"Загружено и отсортировано в контейнер{vbCrLf}за {elapsedTime.TotalMilliseconds:F0} мс", True)
        '    'MW.InfoBLOK.ScrollToEnd()
        'End Sub

        Sub YarlykPressHandler(sender As Object, e As MouseButtonEventArgs)
            ProcessingMDown(CType(sender, Yarlyk))
        End Sub

        Private Sub RassForTCH_Loaded(sender As Object, e As RoutedEventArgs) Handles RassForTCH.Loaded

        End Sub
    End Class
End Namespace

