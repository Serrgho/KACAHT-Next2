

Imports System.Windows.Threading

Namespace Kas

    Partial Public Class SmokeControlWindow

        Private _isRunning As Boolean = True
        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' --- КНОПКИ ---
            AddHandler BtnToggle.Click, AddressOf BtnToggle_Click
            AddHandler BtnClear.Click, AddressOf BtnClear_Click
            AddHandler BtnSaveParams.Click, AddressOf BtnSaveParams_Click

            ' --- ЗАГРУЗКА ПАРАМЕТРОВ ИЗ JSON ---
            SmokeParamsStore.LoadParams(MySmokeCanvas)

            ' --- СИНХРОНИЗАЦИЯ ПОЛУЗНКОВ С ЗАГРУЖЕННЫМИ ЗНАЧЕНИЯМИ ---
            ' (обработчики сами воткнут значения обратно и обновят подписи)
            SldSpeed.Value = MySmokeCanvas.SmokeUpSpeed
            SldFade.Value = MySmokeCanvas.SmokeFadeRatio
            SldWobble.Value = MySmokeCanvas.SmokeWobbleForce
            SldVisc.Value = MySmokeCanvas.Visc
            SldDens.Value = MySmokeCanvas.SmokeDensity
            SldPulse.Value = MySmokeCanvas.SmokePulseInterval
            SldColor.Value = MySmokeCanvas.SmokeHue
            SldMouseForce.Value = MySmokeCanvas.SmokeMouseForce
            SldMouseAngle.Value = MySmokeCanvas.SmokeMouseAngle
            SldBuoyancy.Value = MySmokeCanvas.SmokeBuoyancy
            SldFriction.Value = MySmokeCanvas.AirFriction
            SldCeilRows.Value = MySmokeCanvas.BuoyancyCeilingRows
            SldCeil.Value = MySmokeCanvas.CeilingAbsorb
            SldContrast.Value = MySmokeCanvas.SmokeContrast
            SldVorticity.Value = MySmokeCanvas.SmokeVorticity
            SldDiff.Value = MySmokeCanvas.Diff
            SldSimDt.Value = MySmokeCanvas.SimDt
            SldPressure.Value = MySmokeCanvas.PressureIterations
            SldSrcRadius.Value = MySmokeCanvas.SmokeSourceRadius
            SldSrcPoints.Value = MySmokeCanvas.SmokeSourcePoints
            SldTurb.Value = MySmokeCanvas.SmokeTurbulence
            SldGust.Value = MySmokeCanvas.GustPeakBase
            SldGustChance.Value = MySmokeCanvas.GustChance
            SldRing.Value = MySmokeCanvas.RingStrength
            SldRingChance.Value = MySmokeCanvas.RingChance
            SldMouseContrast.Value = MySmokeCanvas.MouseContrast
            SldAgeReveal.Value = MySmokeCanvas.SmokeAgeReveal
            SldGrayLevel.Value = MySmokeCanvas.SmokeGrayLevel
            SldUseGray.Value = If(MySmokeCanvas.SmokeUseGray, 1, 0)
            LoadParams(MySmokeCanvas)
            ' Запускаем симуляцию при старте окна
            MySmokeCanvas.StartSmoke()
        End Sub


        ''' <summary>Универсальный ползунок: пишет значение в свойство канваса по имени из Tag.</summary>
        ''' <summary>Универсальный ползунок: пишет значение в свойство канваса по имени из Tag
        ''' и обновляет подпись Txt* (заголовок берёт из Tag текстблока).</summary>
        Private Sub SldGeneric_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double))
            Dim sld As Slider = DirectCast(sender, Slider)
            If MySmokeCanvas Is Nothing OrElse sld.Tag Is Nothing Then Return

            Dim p = MySmokeCanvas.GetType().GetProperty(CStr(sld.Tag))
            If p Is Nothing Then Return

            If p.PropertyType Is GetType(Integer) Then
                p.SetValue(MySmokeCanvas, CInt(sld.Value))
            ElseIf p.PropertyType Is GetType(Double) Then
                p.SetValue(MySmokeCanvas, sld.Value)
            ElseIf p.PropertyType Is GetType(Boolean) Then
                p.SetValue(MySmokeCanvas, sld.Value >= 0.5)
            Else
                p.SetValue(MySmokeCanvas, CSng(sld.Value))
            End If

            ' динамическая подпись: SldXxx -> TxtXxx
            Dim txt As TextBlock = TryCast(Me.FindName("Txt" & sld.Name.Substring(3)), TextBlock)
            If txt IsNot Nothing AndAlso txt.Tag IsNot Nothing Then
                Dim fmt As String = If(sld.TickFrequency >= 1, "F0",
                                  If(sld.TickFrequency >= 0.1, "F1",
                                  If(sld.TickFrequency >= 0.01, "F2", "F3")))
                txt.Text = CStr(txt.Tag) & " (" & sld.Value.ToString(fmt) & ")"
            End If
        End Sub

        Private Sub BtnToggle_Click(sender As Object, e As RoutedEventArgs)
            If _isRunning Then
                MySmokeCanvas.StopSmoke()
                BtnToggle.Content = "Запустить дым"
            Else
                MySmokeCanvas.StartSmoke()
                BtnToggle.Content = "Остановить дым"
            End If
            _isRunning = Not _isRunning
        End Sub

        Private Sub BtnClear_Click(sender As Object, e As RoutedEventArgs)
            MySmokeCanvas.StopSmoke()
            If _isRunning Then MySmokeCanvas.StartSmoke()
        End Sub

        Private Sub BtnSaveParams_Click(sender As Object, e As RoutedEventArgs)
            SmokeParamsStore.SaveParams(MySmokeCanvas)
            BtnSaveParams.Content = "✓ Сохранено"
            Dim t As New DispatcherTimer With {.Interval = TimeSpan.FromSeconds(2)}
            AddHandler t.Tick, Sub(s2, e2)
                                   BtnSaveParams.Content = "Сохранить параметры"
                                   t.Stop()
                               End Sub
            t.Start()
        End Sub

        Private Sub Window_Closing(sender As Object, e As ComponentModel.CancelEventArgs)
            ' 1. Останавливаем оверлей пыхов (если используется)
            If MW.SmokeOverlay IsNot Nothing Then
                MW.SmokeOverlay.StopSmoke()
            End If
            MySmokeCanvas = Nothing
            '' 2. Закрываем попап с дымом (если открыт)
            'If _smokePop IsNot Nothing Then
            '    If MySmokeCanvas IsNot Nothing Then MySmokeCanvas.StopSmoke()
            '    _smokePop.IsOpen = False
            '    _smokePop.Child = Nothing   ' разрываем связь с визуальным деревом
            '    _smokePop = Nothing
            '    MySmokeCanvas = Nothing
            'End If

            '' 3. Останавливаем редактор дыма (если открыт)
            '' Если у тебя есть переменная для окна настроек, например _smokeControlWin:
            '' If _smokeControlWin IsNot Nothing Then
            ''     _smokeControlWin.MySmokeCanvas.StopSmoke()
            ''     _smokeControlWin.Close()
            '' End If

            ' 4. Принудительно освобождаем ресурсы (на всякий случай)
            GC.Collect()
            GC.WaitForPendingFinalizers()
        End Sub


    End Class
End Namespace

