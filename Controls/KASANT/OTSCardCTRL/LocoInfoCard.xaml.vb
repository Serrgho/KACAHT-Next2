Namespace Kas
	Partial Public Class LocoInfoCard

        Inherits UserControl

        ' Поле для строки копирования — доступно обработчику
        Private _copyText As String = ""

        Public Sub New(loco As Object) ' ← замените Object на реальный тип
            InitializeComponent()

            ' === Заполнение Run-ов (полный аналог логики из CreateLabel) ===

            ' 1. Серия и номер
            If Not String.IsNullOrWhiteSpace(loco.Series) OrElse Not String.IsNullOrWhiteSpace(loco.Number) Then
                runSeries.Text = $"{loco.Series} №{loco.Number}  ({loco.RealNumber})"
                rtbSeries.Visibility = Visibility.Visible
            End If

            ' 2. Приписка локомотива
            If Not String.IsNullOrWhiteSpace(loco.Road) OrElse Not String.IsNullOrWhiteSpace(loco.Depot) Then
                runLocoAttachHeader.Text = "Приписка локомотива: "
                Dim attach As String = ""
                If Not String.IsNullOrWhiteSpace(loco.Road) Then attach = loco.Road.Trim()
                If Not String.IsNullOrWhiteSpace(loco.Depot) Then
                    If Not String.IsNullOrWhiteSpace(loco.Road) Then attach += ", "
                    attach += loco.Depot.Trim()
                End If
                runLocoAttachText.Text = attach
                rtbLocoAttach.Visibility = Visibility.Visible
            End If

            ' 3. Машинист
            If Not String.IsNullOrWhiteSpace(loco.Driver) Then
                runDriverHeader.Text = "Машинист: "
                runDriverText.Text = NormFam(FixCyrillicLatinity(loco.Driver))
                rtbDriver.Visibility = Visibility.Visible
            End If

            ' 4. Приписка бригады
            If Not String.IsNullOrWhiteSpace(loco.CrewRoad) OrElse Not String.IsNullOrWhiteSpace(loco.CrewDepot) Then
                runCrewAttachHeader.Text = "Приписка бригады: "
                Dim crewAttach As String = ""
                If Not String.IsNullOrWhiteSpace(loco.CrewRoad) Then crewAttach = loco.CrewRoad.Trim()
                If Not String.IsNullOrWhiteSpace(loco.CrewDepot) Then
                    If Not String.IsNullOrWhiteSpace(loco.CrewRoad) Then crewAttach += ", "
                    crewAttach += loco.CrewDepot.Trim()
                End If
                runCrewAttachText.Text = crewAttach
                rtbCrewAttach.Visibility = Visibility.Visible
            End If

            ' Формируем строку для буфера
            _copyText = BuildCopyString(loco)
        End Sub

        ' === Обработчик клика (привязан в XAML) ===
        Private Sub RootBorder_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs)
            If Not String.IsNullOrWhiteSpace(_copyText) Then
                Clipboard.SetText(_copyText)
                e.Handled = True
            End If
        End Sub

        ' === Твоя логика "выжимки" ===
        Private Function BuildCopyString(loco As Object) As String
            Dim sLoco = $"{loco.Series} №{loco.Number}".Trim()
            If Not String.IsNullOrWhiteSpace(loco.RealNumber) Then
                If Not String.IsNullOrWhiteSpace(loco.Number) AndAlso loco.Number.Contains(loco.RealNumber) Then
                    sLoco = $"{loco.Series} №{loco.RealNumber}".Trim()
                Else
                    sLoco = $"_ №{loco.RealNumber}".Trim()
                End If
            End If

            Dim sLocoAttach = ""
            If Not String.IsNullOrWhiteSpace(loco.Depot) AndAlso Not String.IsNullOrWhiteSpace(loco.Road) Then
                sLocoAttach = $"{loco.Road.Trim()}, {loco.Depot.Trim()}"
            End If

            Dim sDriver = If(String.IsNullOrWhiteSpace(loco.Driver), "", NormFam(FixCyrillicLatinity(loco.Driver)))

            Dim sCrewAttach = ""
            If Not String.IsNullOrWhiteSpace(loco.CrewDepot) AndAlso Not String.IsNullOrWhiteSpace(loco.CrewRoad) Then
                sCrewAttach = $"{loco.CrewRoad.Trim()}, {loco.CrewDepot.Trim()}"
            End If

            Return $"{sLoco} ({sLocoAttach}), машинист {sDriver} ({sCrewAttach})".Replace("  ", " ").Trim()
        End Function



    End Class
End Namespace


