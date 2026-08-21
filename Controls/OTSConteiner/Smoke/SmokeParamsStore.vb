Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Windows.Interop
Imports Newtonsoft.Json

Namespace Kas

    Public Module SmokeParamsStore

        Private ReadOnly FileName As String = "smoke_params.json"

        ''' <summary>Только СОБСТВЕННЫЕ свойства канваса (без унаследованных от WPF Width/Opacity и т.п.).</summary>
        Private Function TunableProps() As PropertyInfo()
            Return GetType(SmokeCanvas).GetProperties(
                BindingFlags.Public Or BindingFlags.Instance Or BindingFlags.DeclaredOnly).
                Where(Function(p) p.CanRead AndAlso p.CanWrite AndAlso (
                    p.PropertyType Is GetType(Single) OrElse
                    p.PropertyType Is GetType(Double) OrElse
                    p.PropertyType Is GetType(Integer) OrElse
                    p.PropertyType Is GetType(Boolean))).ToArray()
        End Function

        ''' <summary>Применяет параметры из JSON. Прощает хвостовые пробелы в ключах.</summary>
        Private Sub ApplyFromJson(cv As SmokeCanvas, json As String)
            Dim dict = JsonConvert.DeserializeObject(Of Dictionary(Of String, Object))(json)
            If dict Is Nothing Then Return
            For Each p In TunableProps()
                Dim key As String = dict.Keys.FirstOrDefault(Function(k) k.Trim() = p.Name)
                If key Is Nothing Then Continue For
                Try
                    Dim raw = dict(key)
                    If p.PropertyType Is GetType(Boolean) Then
                        p.SetValue(cv, Convert.ToBoolean(raw))
                    ElseIf p.PropertyType Is GetType(Integer) Then
                        p.SetValue(cv, Convert.ToInt32(raw))
                    ElseIf p.PropertyType Is GetType(Double) Then
                        p.SetValue(cv, Convert.ToDouble(raw))
                    Else
                        p.SetValue(cv, Convert.ToSingle(raw))
                    End If
                Catch
                    ' битое значение — остаётся дефолт
                End Try
            Next
        End Sub

        ''' <summary>Дефолты «повелителя дыма» — если файла в папке данных ещё нет.</summary>
        Private Sub ApplyDefaults(cv As SmokeCanvas)

            With cv
                .AirFriction = 0.9833522
                .SmokeBuoyancy = 0.4173913
                .BuoyancyCeilingRows = 4
                .SmokeSourceRadius = 3
                .SmokeSourcePoints = 10
                .SmokeTurbulence = 0.0
                .SmokeContrast = 450.0
                .MouseContrast = 290.0
                .SmokeAgeReveal = 1.0
                .CeilingAbsorb = 0.08
                .Diff = 0.0
                .SimDt = 0.029565217
                .PressureIterations = 7
                .GustPeakBase = 11.216216
                .GustChance = 0.015
                .RingStrength = 10.0
                .RingChance = 0.02
                .SmokeVorticity = 4.5315313
                .SmokeUseGray = True
                .SmokeGrayLevel = 1.0
                .SmokePulseInterval = 1
                .SmokeDensity = 1.9
                .SmokeMouseForce = 6.0
                .SmokeMouseAngle = 270.0
                .Visc = 0.0
                .SmokeUpSpeed = 80.0
                .SmokeFadeRatio = 0.995
                .SmokeWobbleForce = 0.0
                .SmokeHue = 0.0
            End With




        End Sub




        <DllImport("user32.dll")>
        Private Function GetWindowLong(hWnd As IntPtr, nIndex As Integer) As Integer
        End Function
        <DllImport("user32.dll")>
        Private Function SetWindowLong(hWnd As IntPtr, nIndex As Integer, dwNewLong As Integer) As Integer
        End Function

        Private Const GWL_EXSTYLE As Integer = -20
        Private Const WS_EX_TRANSPARENT As Integer = &H20
        Private Const WS_EX_LAYERED As Integer = &H80000
        Private Const WS_EX_NOACTIVATE As Integer = &H8000000

        ''' <summary>Попап становится призраком: дым виден, но мышь проходит насквозь.</summary>
        Public Sub MakePopupClickThrough(pop As Primitives.Popup)
            Dim src = TryCast(PresentationSource.FromVisual(pop.Child), HwndSource)
            If src Is Nothing Then Return
            Dim ex = GetWindowLong(src.Handle, GWL_EXSTYLE)
            SetWindowLong(src.Handle, GWL_EXSTYLE, ex Or WS_EX_TRANSPARENT Or WS_EX_LAYERED Or WS_EX_NOACTIVATE)
        End Sub



        ''' <summary>Есть файл в папке данных — читаем его; нет — ставим дефолты.</summary>
        Public Sub LoadParams(cv As SmokeCanvas)
            Dim pat As String = Path.Combine(GetOrCreateDataFolderPath(ForParams:=True), FileName)
            If File.Exists(pat) Then
                Try
                    ApplyFromJson(cv, File.ReadAllText(pat))
                Catch
                    ApplyDefaults(cv)   ' файл битый — не падаем, ставим дефолты
                End Try
            Else
                ApplyDefaults(cv)
            End If
        End Sub

        ''' <summary>Сохраняет только собственные свойства канваса, ключи без пробелов.</summary>
        Public Sub SaveParams(cv As SmokeCanvas)
            Try
                'Directory.CreateDirectory(GetOrCreateDataFolderPath(ForParams:=True))
                Dim dict As New Dictionary(Of String, Object)
                For Each p In TunableProps()
                    dict(p.Name) = p.GetValue(cv)
                Next
                File.WriteAllText(Path.Combine(GetOrCreateDataFolderPath(ForParams:=True), FileName),
                                  JsonConvert.SerializeObject(dict, Formatting.Indented))
            Catch
            End Try
        End Sub

    End Module

End Namespace