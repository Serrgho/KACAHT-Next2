Imports System
Imports System.Runtime.InteropServices
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Windows.Media
Imports System.Windows.Media.Imaging

Namespace Kas

    Public Class SmokeCanvas
        Inherits Canvas

        ' --- СРЕДА (воздух) ---
        Public Property AirFriction As Single = 0.975F          ' трение среды: 0.95 кисель … 0.99 вакуум
        Public Property SmokeBuoyancy As Single = 1.5F          ' плавучесть: 0 выкл, 1.5 свеча, 5 костёр
        Public Property BuoyancyCeilingRows As Integer = 8      ' рядов у потолка, где плавучесть гаснет

        ' --- ИСТОЧНИК ---
        Public Property SmokeSourceRadius As Integer = 8        ' разброс точек выброса
        Public Property SmokeSourcePoints As Integer = 30       ' точек за импульс
        Public Property SmokeTurbulence As Single = 0.0F        ' боковой дрожь: 0 ровно … 4 живо

        ' --- ОТРИСОВКА ---
        Public Property SmokeContrast As Single = 200.0F        ' чёткость струек основного дыма
        Public Property MouseContrast As Single = 300.0F        ' контраст мышиного дыма
        Public Property SmokeAgeReveal As Single = 8.0F         ' кадров «детства» (проявление)
        Public Property CeilingAbsorb As Single = 0.08F         ' поглощение на потолке: 0 = отскок, 1 = вытяжка

        ' --- ФИЗИКА ---
        Public Property Diff As Single = 0.0F                   ' диффузия = мыло! держи 0
        Public Property SimDt As Single = 0.04F                 ' скорость симуляции
        Public Property PressureIterations As Integer = 10      ' итерации Project: меньше = спокойнее

        ' --- ПОРЫВЫ ---
        Public Property GustPeakBase As Single = 5.0F           ' базовая сила порыва
        Public Property GustChance As Single = 0.015F           ' шанс порыва за кадр

        ' --- КОЛЕЧКИ ---
        Public Property RingStrength As Single = 120.0F         ' сила вихрей кольца
        Public Property RingChance As Single = 0.02F            ' шанс кольца за кадр

        'э=======================


        ' --- НАСТРОЙКИ СКОРОСТИ И ПОВЕДЕНИЯ ДЫМА ---
        ''' <summary>Кудрявость: 0 — выкл, 2-5 — живые струйки, 10 — шторм.</summary>
        Public Property SmokeVorticity As Single = 3.0F

        ''' <summary>
        ''' True - дым серый (ползунок Hue игнорируется). False - цветной по SmokeHue.
        ''' </summary>
        Public Property SmokeUseGray As Boolean = True

        ''' <summary>
        ''' Яркость серого дыма: 0 = чёрный, 1 = белый.
        ''' 0.2–0.35 - тёмно-серый, как настоящий дым.
        ''' </summary>
        Public Property SmokeGrayLevel As Single = 0.28F



        ''' <summary>
        ''' Шаг микропропусков в подаче дыма (для текстурирования потока). 
        ''' 1 - сплошной поток без пропусков. 2 - подача через кадр. 3 - два кадра пропуска через один и т.д.
        ''' </summary>
        Public Property SmokePulseInterval As Integer = 1

        ''' <summary>
        ''' Общая густота (объем) дыма для источника и мыши.
        ''' </summary>
        Public Property SmokeDensity As Single = 100.0F


        ''' <summary>
        ''' Сила начального импульса выброса дыма из-под мыши.
        ''' </summary>
        Public Property SmokeMouseForce As Single = 6.0F

        ''' <summary>
        ''' Направление выстрела дыма из-под мыши в градусах (0 - вправо, 90 - вниз, 180 - влево, 270 - вверх).
        ''' </summary>
        Public Property SmokeMouseAngle As Double = 0.0


        ''' <summary>
        ''' Вязкость газа. 0.0F - тонкие фрактальные струйки, 
        ''' 0.05F и выше - густой, тяжёлый, монолитный пар с крупной инерцией.
        ''' </summary>
        Public Property Visc As Single = 0.0F
        ''' <summary>
        ''' Скорость подъема дыма вверх. Чем меньше значение, тем медленнее и ленивее всплывает дым.
        ''' </summary>
        Public Property SmokeUpSpeed As Single = 2.0F

        ''' <summary>
        ''' Время жизни дыма в воздухе (коэффициент угасания). 
        ''' 0.96F - растворяется быстро, 0.985F - живет очень долго.
        ''' </summary>
        Public Property SmokeFadeRatio As Single = 0.995F

        ''' <summary>
        ''' Сила покачивания струи у основания (извивание змейкой).
        ''' </summary>
        Public Property SmokeWobbleForce As Single = 2.0F

        ' Единственный ГСЧ на всё время жизни канваса
        Private ReadOnly _rng As New Random(Guid.NewGuid().GetHashCode())

        ' Внутренняя переменная для инерционного ветра (оставь её)
        Private _smoothWindX As Single = 0.05F

        ' --- СКВОЗНЯК: ПРЯМАЯ ТРАЕКТОРИЯ С ФИКСИРОВАННЫМ УГЛОМ ±45° ---
        Private _gustAge As Integer = 0
        Private _gustTotal As Integer = 1
        Private _gustCooldown As Integer = 0
        Private _gustPeak As Single = 0.0F
        Private _gustBaseX As Single, _gustBaseY As Single
        Private _gustTravel As Single
        Private _gustFx As Single, _gustFy As Single   ' ЗАФИКСИРОВАННЫЙ единичный вектор

        Private _gustCurX As Single = 0.0F    ' текущий вектор порыва (для свечи)
        Private _gustCurY As Single = 0.0F
        Private _flameLean As Single = 0.0F   ' текущий наклон пламени


        ' --- КОЛЕЧКИ ---
        Private _ringCooldown As Integer = 300


        ' Внутренний счетчик кадров для отсчета интервалов
        Private _frameCounter As Long = 0

        ' Новый массив плотности специально для дыма от мыши
        Private _densityMouse As Single() = New Single(SizeSq - 1) {}
        Private _densityMouseOld As Single() = New Single(SizeSq - 1) {}

        ' Ползунок будет передавать оттенок (от 0 до 360 градусов)
        Public Property SmokeHue As Double = 0.0 ' 0 - красный, 120 - зеленый, 240 - синий и т.д.


        ' --- ФЛАГИ И КООРДИНАТЫ ДЛЯ НЕПРЕРЫВНОГО МЫШИНОГО ШЛЕЙФА ---
        Private _isMouseDown As Boolean = False
        ' Координаты мыши для трекинга завихрений
        Private _lastMouseX As Double = -1
        Private _lastMouseY As Double = -1


        Private _bitmap As WriteableBitmap
        Private _image As Image
        Private _isDrawing As Boolean = False



        ' Разрешение сетки симуляции (120 - оптимально для производительности)
        Private Const Size As Integer = 120
        Private Const SizeSq As Integer = (Size + 2) * (Size + 2)

        ' Массивы для физики газа (Плотность и Велосити)
        Private _u As Single() = New Single(SizeSq - 1) {} ' Скорость X
        Private _v As Single() = New Single(SizeSq - 1) {} ' Скорость Y
        Private _uOld As Single() = New Single(SizeSq - 1) {}
        Private _vOld As Single() = New Single(SizeSq - 1) {}
        Private _density As Single() = New Single(SizeSq - 1) {} ' Плотность дыма
        Private _densityOld As Single() = New Single(SizeSq - 1) {}
        Private _age As Single() = New Single(SizeSq - 1) {}     ' ← возраст дыма в кадрах
        Private _ageOld As Single() = New Single(SizeSq - 1) {}  ' ← буфер для его адвекции
        Private _curl As Single() = New Single(SizeSq - 1) {}

        ' Константы физического движка
        ' Private Const Visc As Single = 0.0F ' Вязкость газа
        'Private Const Diff As Single = 0.0F ' Диффузия (рассеивание)




        Public Sub New()
            Me.Background = Brushes.Transparent ' Темный фон для контраста

            _bitmap = New WriteableBitmap(Size, Size, 96, 96, PixelFormats.Bgra32, Nothing)
            _image = New Image() With {
            .Source = _bitmap,
            .Stretch = Stretch.Fill,
            .IsHitTestVisible = True
        }
            Me.Children.Add(_image)

            ' Растягиваем изображение под размеры канваса
            AddHandler Me.SizeChanged, Sub(s, e)
                                           _image.Width = Me.ActualWidth
                                           _image.Height = Me.ActualHeight
                                       End Sub

            'AddHandler Me.MouseDown, AddressOf SmokeCanvas_MouseMove
            AddHandler Me.Unloaded, Sub() StopSmoke()
        End Sub
        Public Sub StartSmoke()
            If _isDrawing Then Return
            _isDrawing = True
            AddHandler CompositionTarget.Rendering, AddressOf OnRenderFrame
        End Sub

        Public Sub StopSmoke()
            If Not _isDrawing Then Return
            _isDrawing = False
            RemoveHandler CompositionTarget.Rendering, AddressOf OnRenderFrame

            ' Очищаем плотность нижнего источника и мышиного следа
            Array.Clear(_density, 0, _density.Length)
            Array.Clear(_densityMouse, 0, _densityMouse.Length) ' <-- ОБЯЗАТЕЛЬНО ДОБАВИТЬ!

            ' Очищаем поля скоростей (ветер)
            Array.Clear(_u, 0, _u.Length)
            Array.Clear(_v, 0, _v.Length)
        End Sub




        Private Sub SmokeCanvas_MouseDown(sender As Object, e As MouseButtonEventArgs)
            If e.LeftButton = MouseButtonState.Pressed Then
                _isMouseDown = True
            End If

        End Sub

        Private Sub SmokeCanvas_MouseUp(sender As Object, e As MouseButtonEventArgs)
            _isMouseDown = False
        End Sub



#Region "Свеча"

        ''' <summary>Физика пламени: ленивый наклон за ветром.</summary>
        Private Sub UpdateCandle()
            Dim windX As Single = _smoothWindX * 0.2F + _gustCurX * 0.6F
            _flameLean += (windX - _flameLean) * 0.08F   ' пламя наклоняется с инерцией
        End Sub

        ''' <summary>Рисует каплю пламени.</summary>
        Private Sub DrawCandle()
            _bitmap.Lock()
            Dim backBuffer As IntPtr = _bitmap.BackBuffer
            Dim stride As Integer = _bitmap.BackBufferStride
            Dim baseX As Single = Size \ 2
            Dim baseY As Integer = Size - 3
            Dim tm As Single = _frameCounter

            ' ветер делает пламя ниже, шире и нервнее
            Dim agit As Single = Math.Min(1.0F, Math.Abs(_flameLean) * 0.8F)
            Dim flick As Single = (0.85F - agit * 0.2F) +
                          (0.1F + agit * 0.15F) * CSng(Math.Sin(tm * (0.11 + agit * 0.2))) +
                          0.05F * CSng(Math.Sin(tm * 0.23 + 1.7))
            Dim H As Single = 16 * (1 + 0.12F * CSng(Math.Sin(tm * 0.07)) - agit * 0.25F)
            Dim W As Single = 4.5F
            Dim sway As Single = (1.8F + agit * 3.0F) * CSng(Math.Sin(tm * 0.05)) + 0.8F * CSng(Math.Sin(tm * 0.013 + 2))

            Dim y0 As Integer = Math.Max(1, CInt(baseY - H) - 1)
            For y As Integer = y0 To baseY
                Dim t As Single = (baseY - y) / H
                If t < 0 OrElse t > 1 Then Continue For
                Dim halfW As Single = W * CSng(Math.Sin(Math.PI * Math.Pow(t, 0.65))) + 0.4F
                ' наклон от ветра растёт к кончику
                Dim cx As Single = baseX + sway * t * t + _flameLean * t * 3.0F
                Dim rowPtr As IntPtr = backBuffer + (y * stride)

                For x As Integer = CInt(cx - halfW - 1) To CInt(cx + halfW + 1)
                    If x < 1 OrElse x >= Size Then Continue For
                    Dim d As Single = Math.Abs(x - cx) / halfW
                    If d > 1 Then Continue For
                    Dim radial As Single = 1 - d * d : radial *= radial
                    Dim I As Single = radial * (1 - t * 0.55) * flick
                    If I <= 0.02 Then Continue For

                    Dim r As Integer = CInt(Math.Min(255, 140 + 115 * Math.Min(1, I * 4)))
                    Dim g As Integer = CInt(Math.Min(255, 255 * Math.Pow(I, 2.2)))
                    Dim b As Integer = CInt(Math.Min(255, 200 * Math.Pow(I, 5)))
                    If t < 0.22 Then b = Math.Min(255, b + CInt(220 * (0.22 - t) / 0.22 * radial))

                    'Dim a As Integer = CInt(255 * Math.Min(1, I * 1.5))
                    'Runtime.InteropServices.Marshal.WriteInt32(rowPtr + (x * 4), (a << 24) Or (r << 16) Or (g << 8) Or b)
                    Dim pixelPtr As IntPtr = rowPtr + (x * 4)
                    Dim cur As Integer = Runtime.InteropServices.Marshal.ReadInt32(pixelPtr)
                    Dim curA As Single = ((cur >> 24) And &HFF) / 255.0F
                    Dim curR As Integer = (cur >> 16) And &HFF
                    Dim curG As Integer = (cur >> 8) And &HFF
                    Dim curB As Integer = cur And &HFF

                    Dim srcA As Single = Math.Min(1, I * 1.5)
                    Dim outA As Single = srcA + curA * (1 - srcA)
                    If outA < 0.01 Then Continue For

                    Dim outR As Integer = CInt((r * srcA + curR * curA * (1 - srcA)) / outA)
                    Dim outG As Integer = CInt((g * srcA + curG * curA * (1 - srcA)) / outA)
                    Dim outB As Integer = CInt((b * srcA + curB * curA * (1 - srcA)) / outA)

                    Runtime.InteropServices.Marshal.WriteInt32(pixelPtr,
                        (CInt(255 * outA) << 24) Or (outR << 16) Or (outG << 8) Or outB)


                Next
            Next
            _bitmap.AddDirtyRect(New Int32Rect(0, 0, Size, Size))
            _bitmap.Unlock()
        End Sub

        ''' <summary>Мягкое свечение вокруг (центр следует за наклоном).</summary>
        Private Sub DrawCandleGlow()
            _bitmap.Lock()
            Dim backBuffer As IntPtr = _bitmap.BackBuffer
            Dim stride As Integer = _bitmap.BackBufferStride
            Dim baseX As Single = (Size \ 2) + _flameLean * 0.5F
            Dim baseY As Integer = Size - 3
            Dim glowRadius As Integer = 8
            Dim pulse As Single = CSng(0.6 + 0.4 * Math.Sin(_frameCounter * 0.1))

            For y As Integer = baseY - 5 To baseY + 2
                If y < 1 OrElse y >= Size Then Continue For
                Dim rowPtr As IntPtr = backBuffer + (y * stride)
                For x As Integer = CInt(baseX - glowRadius) To CInt(baseX + glowRadius)
                    If x < 1 OrElse x >= Size Then Continue For
                    Dim dx As Single = x - baseX, dy As Single = y - baseY
                    Dim dist As Single = CSng(Math.Sqrt(dx * dx + dy * dy))
                    If dist > glowRadius Then Continue For
                    Dim glow As Single = (1.0F - dist / glowRadius) * pulse * 0.5F
                    Dim pixelPtr As IntPtr = rowPtr + (x * 4)
                    Dim cur As Integer = Runtime.InteropServices.Marshal.ReadInt32(pixelPtr)
                    Dim curB As Integer = cur And &HFF
                    Dim curG As Integer = (cur >> 8) And &HFF
                    Dim curR As Integer = (cur >> 16) And &HFF
                    Dim r As Integer = CInt(curR + (255 - curR) * glow)
                    Dim g As Integer = CInt(curG + (170 - curG) * glow)
                    Dim b As Integer = CInt(curB + (40 - curB) * glow)
                    Dim a2 As Integer = Math.Max((cur >> 24) And &HFF, CInt(glow * 255))
                    Runtime.InteropServices.Marshal.WriteInt32(pixelPtr, (a2 << 24) Or (r << 16) Or (g << 8) Or b)
                Next
            Next
            _bitmap.AddDirtyRect(New Int32Rect(0, 0, Size, Size))
            _bitmap.Unlock()
        End Sub

#End Region








        ' Кадровая обработка физики и рендер
        Private Sub OnRenderFrame(sender As Object, e As EventArgs)


            ClearBuffers()    ' обнуление буферов сил
            ApplyFade()       ' затухание дыма + трение воздуха
            FeedMouse()       ' честная подача от мыши (ЛКМ)
            ClearTop()        ' очистка верха сетки
            _frameCounter += 1
            FeedSource()      ' нижний источник с микропульсацией
            DoGusts()         ' порывы ветра
            DoRings()         ' колечки
            AddVorticity()    ' возвращает мелкие завитки
            StepPhysics()     ' шаг физики
            DrawFluid()

            'UpdateCandle()    ' наклон пламени за ветром
            'DrawCandle()      ' пламя
            'DrawCandleGlow()  ' свечение

        End Sub

        ''' <summary>Подкручивает мелкие вихри, которые съедает сетка.</summary>
        Private Sub AddVorticity()

            Dim n As Integer = Size
            Dim w1 As Integer = Size + 2

            ' 1. завихренность (ротор скорости)
            For j As Integer = 1 To n
                For i As Integer = 1 To n
                    Dim idx As Integer = i + j * w1
                    _curl(idx) = 0.5F * ((_v(idx + 1) - _v(idx - 1)) - (_u(idx + w1) - _u(idx - w1)))
                Next
            Next

            ' 2. сила, подкручивающая вихри
            For j As Integer = 2 To n - 1
                For i As Integer = 2 To n - 1
                    Dim idx As Integer = i + j * w1
                    Dim dx As Single = 0.5F * (Math.Abs(_curl(idx + 1)) - Math.Abs(_curl(idx - 1)))
                    Dim dy As Single = 0.5F * (Math.Abs(_curl(idx + w1)) - Math.Abs(_curl(idx - w1)))
                    Dim len As Single = CSng(Math.Sqrt(dx * dx + dy * dy)) + 0.00001F
                    dx /= len : dy /= len
                    Dim w As Single = _curl(idx)
                    _uOld(idx) += SmokeVorticity * w * dy    ' было: * w1 *
                    _vOld(idx) -= SmokeVorticity * w * dx
                Next
            Next
        End Sub

#Region "Кадр: очистка и подача"

        ''' <summary>Обнуление буферов сил перед кадром.</summary>
        Private Sub ClearBuffers()
            Array.Clear(_densityOld, 0, _densityOld.Length)
            Array.Clear(_densityMouseOld, 0, _densityMouseOld.Length)
            Array.Clear(_uOld, 0, _uOld.Length)
            Array.Clear(_vOld, 0, _vOld.Length)
            Array.Clear(_ageOld, 0, _ageOld.Length)
        End Sub

        ''' <summary>Дым тает по ползунку, скорости гасим трением воздуха.</summary>
        Private Sub ApplyFade()

            For i As Integer = 0 To _age.Length - 1
                If _density(i) > 0.02F OrElse _densityMouse(i) > 0.02F Then
                    _age(i) += 1.0F          ' в дыме — стареем
                Else
                    _age(i) = 0.0F           ' в пустоте — возраст ноль
                End If
            Next



            For i As Integer = 0 To _density.Length - 1
                _density(i) *= SmokeFadeRatio        ' ← твоё старое
                _densityMouse(i) *= SmokeFadeRatio   ' ← твоё старое
                _u(i) *= AirFriction                ' ← твоё старое
                _v(i) *= AirFriction                ' ← твоё старое

                ' --- ПЛАВУЧЕСТЬ (вот её не хватало — бегунок теперь живой) ---
                Dim y As Integer = i \ (Size + 2)
                Dim buoyScale As Single = CSng(Math.Min(1.0, (y - 1) / BuoyancyCeilingRows))
                If buoyScale < 0 Then buoyScale = 0
                _vOld(i) -= SmokeBuoyancy * buoyScale * (_density(i) + _densityMouse(i))

            Next

        End Sub

        ''' <summary>Очистка верхней строки сетки — дым не копится у потолка.</summary>
        Private Sub ClearTop()
            For x As Integer = 1 To Size
                _density(x + (Size + 2)) *= CeilingAbsorb       ' было = 0.0F
                _densityMouse(x + (Size + 2)) *= CeilingAbsorb  ' было = 0.0F
            Next
        End Sub

        ''' <summary>Честная подача мыши по зажатию ЛКМ.</summary>
        Public Sub FeedMouse()
            If Mouse.LeftButton = MouseButtonState.Pressed Then
                If _frameCounter Mod SmokePulseInterval = 0 Then
                    Dim pos = Mouse.GetPosition(_image)
                    Dim mX As Integer = CInt((pos.X / _image.ActualWidth) * Size)
                    Dim mY As Integer = CInt((pos.Y / _image.ActualHeight) * Size)
                    Dim angleRad As Double = SmokeMouseAngle * Math.PI / 180.0
                    Dim effectiveForce As Single = SmokeMouseForce * 0.4F
                    Dim forceX As Single = CSng(Math.Cos(angleRad)) * effectiveForce
                    Dim forceY As Single = CSng(Math.Sin(angleRad)) * effectiveForce
                    Dim mouseRadius As Integer = 1

                    For i As Integer = 1 To 6
                        Dim randX As Integer = mX + _rng.Next(-mouseRadius, mouseRadius + 1)
                        Dim randY As Integer = mY + _rng.Next(-mouseRadius, mouseRadius + 1)
                        If randX > 1 AndAlso randX < Size - 1 AndAlso randY > 1 AndAlso randY < Size - 1 Then
                            Dim mIdx As Integer = randX + (randY * (Size + 2))
                            _densityMouse(mIdx) += SmokeDensity
                            _uOld(mIdx) += forceX
                            _vOld(mIdx) += forceY
                            _vOld(mIdx) -= (SmokeUpSpeed / 4.0F)
                        End If
                    Next
                End If
                _lastMouseX = 1
            Else
            _lastMouseX = -1
            _lastMouseY = -1
            End If
        End Sub

        ''' <summary>Нижний источник: покачивание + подача дыма.</summary>
        Private Sub FeedSource()
            If _frameCounter Mod SmokePulseInterval <> 0 Then Return

            _smoothWindX += CSng(_rng.NextDouble() * 4.0 - 2.0)
            If _smoothWindX > SmokeWobbleForce Then _smoothWindX = SmokeWobbleForce
            If _smoothWindX < -SmokeWobbleForce Then _smoothWindX = -SmokeWobbleForce
            _smoothWindX *= 0.93F

            Dim sourceX As Integer = Size \ 2
            Dim sourceY As Integer = Size - 2
            Dim sourceRadius As Integer = SmokeSourceRadius


            For i As Integer = 1 To SmokeSourcePoints
                Dim randX As Integer = sourceX + _rng.Next(-sourceRadius, sourceRadius + 1)

                Dim randY As Integer = sourceY + _rng.Next(-2, 1)

                If randX > 1 AndAlso randX < Size - 1 AndAlso randY > 1 AndAlso randY < Size - 1 Then
                    Dim idx As Integer = randX + (randY * (Size + 2))

                    '==============================================
                    _density(idx) += SmokeDensity
                    _age(idx) = 0.0F
                    '==============================================

                    _vOld(idx) = -SmokeUpSpeed
                    _uOld(idx) = _smoothWindX + CSng((_rng.NextDouble() - 0.5) * SmokeTurbulence)

                End If
            Next
        End Sub

#End Region


#Region "Ветер и колечки"

        ''' <summary>Порывы: состояние + применение. Вызывать перед шагом физики.</summary>
        Private Sub DoGusts()
            _gustCurX = 0.0F : _gustCurY = 0.0F

            ' --- СОСТОЯНИЕ: кулдаун и запуск нового порыва ---
            If _gustCooldown > 0 Then _gustCooldown -= 1

            If _gustAge >= _gustTotal AndAlso _gustCooldown = 0 AndAlso _rng.NextDouble() < GustChance Then
                _gustTotal = _rng.Next(90, 180)
                _gustAge = 0
                _gustCooldown = _rng.Next(60, 150)
                _gustPeak = CSng(GustPeakBase + _rng.NextDouble()) '_gustPeak = CSng(5 + _rng.NextDouble() * 1)
                _gustBaseX = CSng(_rng.Next(25, Size - 25))
                _gustBaseY = CSng(_rng.Next(20, Size - 20))
                _gustTravel = CSng(_rng.Next(15, 45))
                ' угол выбирается ОДИН раз на порыв
                Dim baseDir As Single = If(_rng.NextDouble() < 0.5, 0.0F, CSng(Math.PI))
                Dim tilt As Single = CSng((_rng.NextDouble() * 2 - 1) * (Math.PI / 4))
                _gustFx = CSng(Math.Cos(baseDir + tilt))
                _gustFy = CSng(Math.Sin(baseDir + tilt))
            End If

            If _gustAge < _gustTotal Then _gustAge += 1

            ' --- ПРИМЕНЕНИЕ: дуем пятном на летящий дым ---
            If _gustAge < _gustTotal Then
                Dim t As Single = _gustAge / CSng(_gustTotal)
                Dim px As Single = _gustBaseX '+ _gustFx * _gustTravel * t
                Dim py As Single = _gustBaseY + _gustFy * _gustTravel * t
                Dim F As Single = _gustPeak * CSng(Math.Sin(Math.PI * t))
                _gustCurX = _gustFx * F
                _gustCurY = _gustFy * F

                Dim rad As Integer = 7
                Dim ix As Integer = CInt(px), iy As Integer = CInt(py)
                For y As Integer = Math.Max(1, iy - rad) To Math.Min(Size, iy + rad)
                    For x As Integer = Math.Max(1, ix - rad) To Math.Min(Size, ix + rad)
                        Dim dx As Single = x - px, dy As Single = y - py
                        Dim d2 As Single = dx * dx + dy * dy
                        If d2 > rad * rad Then Continue For
                        Dim fall As Single = 1 - d2 / (rad * rad)
                        Dim idx As Integer = x + y * (Size + 2)
                        _uOld(idx) += _gustFx * F * fall
                        _vOld(idx) += _gustFy * F * fall
                    Next
                Next
            End If
        End Sub

        ''' <summary>Колечки: случайный выстрел с кулдауном.</summary>
        Private Sub DoRings()
            If _ringCooldown > 0 Then
                _ringCooldown -= 1
            ElseIf _rng.NextDouble() < RingChance Then
                SpawnSmokeRing(Size \ 2, Size - 12)
                _ringCooldown = _rng.Next(250, 600)
            End If
        End Sub


        ''' <summary>Выстреливает дымовое колечко: два встречных вихря + плотность по окружности.</summary>
        Private Sub SpawnSmokeRing(cx As Integer, cy As Integer)
            Dim rr As Single = CSng(2.5 + _rng.NextDouble() * 2)        ' радиус кольца 2.5–4.5 клеток
            Dim s As Single = CSng(RingStrength + _rng.NextDouble() * 80)       ' сила вихрей
            Dim tilt As Single = CSng((_rng.NextDouble() - 0.5) * 0.8) ' лёгкий наклон оси

            ' Центры двух вихрей (левый и правый)
            Dim lx As Single = cx - rr, ly As Single = cy + tilt * rr
            Dim rx As Single = cx + rr, ry As Single = cy - tilt * rr

            Dim rad As Integer = CInt(rr * 2.5) + 2
            For y As Integer = cy - rad To cy + rad
                For x As Integer = cx - rad * 2 To cx + rad * 2
                    If x < 1 Or x > Size - 1 Or y < 1 Or y > Size - 1 Then Continue For
                    Dim idx As Integer = x + y * (Size + 2)

                    ' Левый вихрь (−s) и правый (+s): вместе толкают дым ВВЕРХ между собой
                    Dim dxL As Single = x - lx, dyL As Single = y - ly
                    Dim dL As Single = dxL * dxL + dyL * dyL + 1.5
                    Dim dxR As Single = x - rx, dyR As Single = y - ry
                    Dim dR As Single = dxR * dxR + dyR * dyR + 1.5

                    _uOld(idx) += (s * dyL) / dL - (s * dyR) / dR
                    _vOld(idx) += (-s * dxL) / dL + (s * dxR) / dR
                Next
            Next

            ' Плотность по окружности (сам «бублик») + стартовый пинок вверх
            Dim steps As Integer = 24
            For a As Integer = 0 To steps - 1
                Dim ang As Double = a / steps * Math.PI * 2
                Dim px As Integer = CInt(cx + Math.Cos(ang) * rr)
                Dim py As Integer = CInt(cy + Math.Sin(ang) * rr)
                If px > 1 AndAlso px < Size - 1 AndAlso py > 1 AndAlso py < Size - 1 Then
                    Dim idx As Integer = px + py * (Size + 2)
                    _density(idx) += SmokeDensity * 2.0F
                    _vOld(idx) -= 30.0F
                End If
            Next
        End Sub

#End Region


        ''' <summary>Шаг физики: скорость, плотность, мышиный след.</summary>
        Private Sub StepPhysics()
            Dim slowDt As Single = 0.04F
            VelStep(Size, _u, _v, _uOld, _vOld, Visc, SimDt)
            DensStep(Size, _density, _densityOld, _u, _v, Diff, SimDt)

            Array.Copy(_age, _ageOld, _age.Length)
            Advect(Size, 0, _age, _ageOld, _u, _v, SimDt)

            Array.Copy(_densityMouse, _densityMouseOld, _densityMouse.Length)
            Advect(Size, 0, _densityMouse, _densityMouseOld, _u, _v, SimDt)
        End Sub





        ' Скоростной вывод массива плотности напрямую в массив пикселей WriteableBitmap
        Private Sub DrawFluid()

            _bitmap.Lock()
            Dim backBuffer As IntPtr = _bitmap.BackBuffer
            Dim stride As Integer = _bitmap.BackBufferStride

            ' --- РАСЧЁТ БАЗОВОГО ЦВЕТА ИСТОЧНИКА ---
            Dim rBase As Byte, gBase As Byte, bBase As Byte

            If SmokeUseGray Then
                ' СЕРЫЙ РЕЖИМ: R = G = B
                Dim gray As Byte = CByte(Math.Max(0, Math.Min(255, 255 * SmokeGrayLevel)))
                rBase = gray : gBase = gray : bBase = gray
            Else
                ' ЦВЕТНОЙ РЕЖИМ ПО HUE
                Dim h As Double = SmokeHue / 60.0
                Dim xColor As Byte = CByte(255 * (1 - Math.Abs((h Mod 2) - 1)))
                If h < 1 Then : rBase = 255 : gBase = xColor : bBase = 0
                ElseIf h < 2 Then : rBase = xColor : gBase = 255 : bBase = 0
                ElseIf h < 3 Then : rBase = 0 : gBase = 255 : bBase = xColor
                ElseIf h < 4 Then : rBase = 0 : gBase = xColor : bBase = 255
                ElseIf h < 5 Then : rBase = xColor : gBase = 0 : bBase = 255
                Else : rBase = 255 : gBase = 0 : bBase = xColor
                End If
            End If

            Threading.Tasks.Parallel.For(0, Size, Sub(y)
                                                      Dim rowPtr As IntPtr = backBuffer + (y * stride)
                                                      For x As Integer = 0 To Size - 1
                                                          Dim idx As Integer = (x + 1) + ((y + 1) * (Size + 2))


                                                          'Множитель
                                                          '450      'даже лёгкая волна читается как жирная ёлка
                                                          '300      'текущий баланс
                                                          '200–220  'волна становится «худой», полупрозрачной — вихляние визуально слабее, но и дым бледнее (компенсируй SmokeDensity ↑)


                                                          Dim dMain As Single = _density(idx) * SmokeContrast
                                                          ' проявляемся за 8 кадров
                                                          Dim ageFade As Single = Math.Min(1.0F, _age(idx) / SmokeAgeReveal)
                                                          dMain *= ageFade
                                                          Dim dMouse As Single = _densityMouse(idx) * MouseContrast

                                                          If dMain > 255.0F Then dMain = 255.0F
                                                          If dMouse > 255.0F Then dMouse = 255.0F

                                                          ' --- РАСЧЁТ ЦВЕТА ПИКСЕЛЯ ---
                                                          Dim r As Integer, g As Integer, b As Integer

                                                          If SmokeUseGray Then
                                                              ' СЕРЫЙ: оба источника используют один серый оттенок
                                                              Dim grayVal As Integer = CInt((dMain + dMouse) / 255.0F * rBase)
                                                              r = grayVal : g = grayVal : b = grayVal
                                                          Else
                                                              ' ЦВЕТНОЙ: основной дым по hue, мышиный - белый
                                                              Dim rSrc As Double = (dMain / 255.0F) * rBase
                                                              Dim gSrc As Double = (dMain / 255.0F) * gBase
                                                              Dim bSrc As Double = (dMain / 255.0F) * bBase

                                                              Dim rMsh As Double = (dMouse / 255.0F) * 255.0
                                                              Dim gMsh As Double = (dMouse / 255.0F) * 255.0
                                                              Dim bMsh As Double = (dMouse / 255.0F) * 255.0

                                                              r = CInt(rSrc + rMsh - (rSrc * rMsh / 255.0))
                                                              g = CInt(gSrc + gMsh - (gSrc * gMsh / 255.0))
                                                              b = CInt(bSrc + bMsh - (bSrc * bMsh / 255.0))
                                                          End If

                                                          ' Ограничение рамок байта
                                                          If r > 255 Then r = 255
                                                          If g > 255 Then g = 255
                                                          If b > 255 Then b = 255
                                                          If r < 0 Then r = 0
                                                          If g < 0 Then g = 0
                                                          If b < 0 Then b = 0

                                                          ' Альфа-канал для прозрачности
                                                          Dim alpha As Integer = CInt(Math.Min(255, dMain + dMouse))
                                                          Dim pixelColor As Integer = (alpha << 24) Or (b << 16) Or (g << 8) Or r
                                                          Dim pixelPtr As IntPtr = rowPtr + (x * 4)
                                                          Runtime.InteropServices.Marshal.WriteInt32(pixelPtr, pixelColor)
                                                      Next
                                                  End Sub)

            _bitmap.AddDirtyRect(New Int32Rect(0, 0, Size, Size))
            _bitmap.Unlock()

        End Sub


#Region " Математика Fluid Dynamics (Jos Stam) "

        Private Sub AddSource(n As Integer, x As Single(), s As Single(), dt As Single)
            Dim sizeSq As Integer = (n + 2) * (n + 2)
            For i As Integer = 0 To sizeSq - 1
                x(i) += dt * s(i)
            Next
        End Sub

        Private Sub Diffuse(n As Integer, b As Integer, x As Single(), x0 As Single(), diff As Single, dt As Single)
            Dim a As Single = dt * diff * n * n
            Dim c As Single = 1 + 4 * a
            For k As Integer = 0 To 19
                For j As Integer = 1 To n
                    For i As Integer = 1 To n
                        Dim idx As Integer = i + (j * (n + 2))
                        x(idx) = (x0(idx) + a * (x(idx - 1) + x(idx + 1) + x(idx - (n + 2)) + x(idx + (n + 2)))) / c
                    Next
                Next
                SetBnd(n, b, x)
            Next
        End Sub

        Private Sub Advect(n As Integer, b As Integer, d As Single(), d0 As Single(), u As Single(), v As Single(), dt As Single)
            Dim dt0 As Single = dt * n
            For j As Integer = 1 To n
                For i As Integer = 1 To n
                    Dim idx As Integer = i + (j * (n + 2))
                    Dim x As Single = i - dt0 * u(idx)
                    Dim y As Single = j - dt0 * v(idx)

                    If x < 0.5F Then x = 0.5F
                    If x > n + 0.5F Then x = n + 0.5F
                    Dim i0 As Integer = CInt(Math.Floor(x))
                    Dim i1 As Integer = i0 + 1

                    If y < 0.5F Then y = 0.5F
                    If y > n + 0.5F Then y = n + 0.5F
                    Dim j0 As Integer = CInt(Math.Floor(y))
                    Dim j1 As Integer = j0 + 1

                    Dim s1 As Single = x - i0
                    Dim s0 As Single = 1 - s1
                    Dim t1 As Single = y - j0
                    Dim t0 As Single = 1 - t1

                    Dim row0 As Integer = j0 * (n + 2)
                    Dim row1 As Integer = j1 * (n + 2)

                    d(idx) = s0 * (t0 * d0(i0 + row0) + t1 * d0(i0 + row1)) +
                     s1 * (t0 * d0(i1 + row0) + t1 * d0(i1 + row1))
                Next
            Next
            SetBnd(n, b, d)
        End Sub

        Private Sub Project(n As Integer, u As Single(), v As Single(), p As Single(), div As Single())
            For j As Integer = 1 To n
                For i As Integer = 1 To n
                    Dim idx As Integer = i + (j * (n + 2))
                    div(idx) = -0.5F * (u(idx + 1) - u(idx - 1) + v(idx + (n + 2)) - v(idx - (n + 2))) / n
                    p(idx) = 0
                Next
            Next
            SetBnd(n, 0, div)
            SetBnd(n, 0, p)

            For k As Integer = 0 To PressureIterations - 1
                For j As Integer = 1 To n
                    For i As Integer = 1 To n
                        Dim idx As Integer = i + (j * (n + 2))
                        p(idx) = (div(idx) + p(idx - 1) + p(idx + 1) + p(idx - (n + 2)) + p(idx + (n + 2))) / 4
                    Next
                Next
                SetBnd(n, 0, p)
            Next

            For j As Integer = 1 To n
                For i As Integer = 1 To n
                    Dim idx As Integer = i + (j * (n + 2))
                    u(idx) -= 0.5F * n * (p(idx + 1) - p(idx - 1))
                    v(idx) -= 0.5F * n * (p(idx + (n + 2)) - p(idx - (n + 2)))
                Next
            Next
            SetBnd(n, 1, u)
            SetBnd(n, 2, v)
        End Sub


        Private Sub DensStepMouse(n As Integer, x As Single(), x0 As Single(), u As Single(), v As Single(), dt As Single)
            ' 1. Добавляем порцию дыма из источника
            AddSource(n, x, x0, dt)

            ' 2. Поскольку диффузия (Diff) у нас равна 0, мы полностью пропускаем шаг Diffuse,
            ' тем самым избавляясь от опасного Array.Copy, который затирал наш шлейф!

            ' 3. Сразу двигаем мышиный дым по полю скоростей (Адвекция)
            ' Для этого временно копируем x в x0, так как Advect требует историю
            Array.Copy(x, x0, x.Length)
            Advect(n, 0, x, x0, u, v, dt)
        End Sub


        Private Sub DensStep(n As Integer, x As Single(), x0 As Single(), u As Single(), v As Single(), diff As Single, dt As Single)
            AddSource(n, x, x0, dt)
            Array.Copy(x, x0, x.Length)
            Diffuse(n, 0, x, x0, diff, dt)
            Array.Copy(x, x0, x.Length)
            Advect(n, 0, x, x0, u, v, dt)
        End Sub

        Private Sub VelStep(n As Integer, u As Single(), v As Single(), u0 As Single(), v0 As Single(), visc As Single, dt As Single)
            AddSource(n, u, u0, dt)
            AddSource(n, v, v0, dt)
            Array.Copy(u, u0, u.Length)
            Diffuse(n, 1, u, u0, visc, dt)
            Array.Copy(v, v0, v.Length)
            Diffuse(n, 2, v, v0, visc, dt)
            Project(n, u, v, u0, v0)
            Array.Copy(u, u0, u.Length)
            Array.Copy(v, v0, v.Length)
            Advect(n, 1, u, u0, u0, v0, dt)
            Advect(n, 2, v, v0, u0, v0, dt)
            Project(n, u, v, u0, v0)
        End Sub

        Private Sub SetBnd(n As Integer, b As Integer, x As Single())
            For i As Integer = 1 To n
                x(i + (0 * (n + 2))) = If(b = 2, -x(i + (1 * (n + 2))), x(i + (1 * (n + 2))))
                x(i + ((n + 1) * (n + 2))) = If(b = 2, -x(i + (n * (n + 2))), x(i + (n * (n + 2))))
                x(0 + (i * (n + 2))) = If(b = 1, -x(1 + (i * (n + 2))), x(1 + (i * (n + 2))))
                x((n + 1) + (i * (n + 2))) = If(b = 1, -x(n + (i * (n + 2))), x(n + (i * (n + 2))))
            Next

            Dim rowN1 As Integer = (n + 1) * (n + 2)
            x(0 + 0) = 0.5F * (x(1 + 0) + x(0 + (1 * (n + 2))))
            x(0 + rowN1) = 0.5F * (x(1 + rowN1) + x(0 + (n * (n + 2))))
            x((n + 1) + 0) = 0.5F * (x(n + 0) + x((n + 1) + (1 * (n + 2))))
            x((n + 1) + rowN1) = 0.5F * (x(n + rowN1) + x((n + 1) + (n * (n + 2))))
        End Sub

#End Region

    End Class

End Namespace