Imports System.Net.Http
Imports System.Text

Namespace Kas
	Partial Public Class HelperLocomotiveWindow
        Inherits Window

        Public Property ViolId As String
        Public Property DorKod As String

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub HelperLocomotiveWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
            txtViolId.Text = ViolId

            ' Попытка предзаполнить данные, если они уже есть (опционально)
            ' Можно добавить метод Fetcher.GetHelperLocoDataAsync(ViolId, DorKod)
        End Sub

        Private Async Sub BtnSave_Click(sender As Object, e As RoutedEventArgs)
            If String.IsNullOrWhiteSpace(txtTrainNumber.Text) OrElse String.IsNullOrWhiteSpace(txtOrderInfo.Text) Then
                MessageBox.Show("Заполните номер поезда и приказ!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            Try
                ' ✅ Вызываем через глобальный Fetcher (не Shared!)
                Await Fetcher.SaveHelperLocomotiveAsync(
            ViolId,
            88,'DorKod,
            txtTrainNumber.Text,
            txtOrderInfo.Text,
            chkFollowWithHelper.IsChecked.GetValueOrDefault(False)
        )

                DialogResult = True
            Catch ex As Exception
                ShowMSG(Me, $"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error)
            Finally
                Fetcher.ForceCleanup()
            End Try
        End Sub

        Private Sub BtnCancel_Click(sender As Object, e As RoutedEventArgs)
            DialogResult = False
        End Sub
    End Class
End Namespace



