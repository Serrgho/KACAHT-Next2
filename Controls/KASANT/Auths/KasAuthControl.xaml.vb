Imports System.Text

Namespace Kas

    Partial Public Class KasAuthControl

        Sub New()
            InitializeComponent()
            expAuth.IsExpanded = False
            LoadSettings()
        End Sub

        Private Sub btnClearSettings_Click(sender As Object, e As RoutedEventArgs)
            ClrSettings()
        End Sub

        Private Sub btnSaveSettings_Click(sender As Object, e As RoutedEventArgs)
            SaveSettings()
        End Sub

        ' ===================================================================
        ' СОХРАНЕНИЕ НАСТРОЕК
        ' ===================================================================
        Private Sub SaveSettings()
            With My.Settings
                ' 🔹 Дорожный логин
                If chkRemember.IsChecked = True Then
                    .KasantLogin = txtLogin.Text
                    .KasantPassword = EncryptPassword(txtPassword.Password)
                Else
                    .KasantLogin = ""
                    .KasantPassword = ""
                End If

                ' 🔹 Центральный логин (для REST API)
                If chkRememberCentral.IsChecked = True Then
                    .CentralLogin = txtCentralLogin.Text
                    .CentralPassword = EncryptPassword(txtCentralPassword.Password)
                Else
                    .CentralLogin = ""
                    .CentralPassword = ""
                End If

                .Save()
            End With

            ShowMSG(MW, "Настройки сохранены", "Готово", MessageBoxButton.OK, MessageBoxImage.Information)
        End Sub

        ' ===================================================================
        ' ЗАГРУЗКА НАСТРОЕК
        ' ===================================================================
        Private Sub LoadSettings()
            Try
                ' 🔹 Дорожный логин
                If Not String.IsNullOrWhiteSpace(My.Settings.KasantLogin) Then
                    txtLogin.Text = My.Settings.KasantLogin
                    txtPassword.Password = DecryptPassword(My.Settings.KasantPassword)
                    chkRemember.IsChecked = True
                End If

                ' 🔹 Центральный логин
                If Not String.IsNullOrWhiteSpace(My.Settings.CentralLogin) Then
                    txtCentralLogin.Text = My.Settings.CentralLogin
                    txtCentralPassword.Password = DecryptPassword(My.Settings.CentralPassword)
                    chkRememberCentral.IsChecked = True
                End If
            Catch ex As Exception
                ' Тихо игнорируем ошибки декодирования
            End Try
        End Sub

        ' ===================================================================
        ' ОЧИСТКА НАСТРОЕК
        ' ===================================================================
        Private Sub ClrSettings()
            With My.Settings
                .KasantLogin = ""
                .KasantPassword = ""
                .CentralLogin = ""
                .CentralPassword = ""
                .Save()
            End With

            txtLogin.Text = ""
            txtPassword.Password = ""
            txtCentralLogin.Text = ""
            txtCentralPassword.Password = ""
            chkRemember.IsChecked = False
            chkRememberCentral.IsChecked = False

            ShowMSG(MW, "Все данные очищены", "Готово", MessageBoxButton.OK, MessageBoxImage.Information)
        End Sub

        ' ===================================================================
        ' ШИФРОВАНИЕ / ДЕШИФРОВАНИЕ (ваша простая схема)
        ' ===================================================================
        'Private Function EncryptPassword(password As String) As String
        '    If String.IsNullOrWhiteSpace(password) Then Return ""
        '    Dim bytes = Encoding.UTF8.GetBytes(password)
        '    Dim reversed = bytes.Reverse().ToArray()
        '    Return Convert.ToBase64String(reversed)
        'End Function

        'Private Function DecryptPassword(encrypted As String) As String
        '    If String.IsNullOrWhiteSpace(encrypted) Then Return ""
        '    Try
        '        Dim bytes = Convert.FromBase64String(encrypted)
        '        Dim reversed = bytes.Reverse().ToArray()
        '        Return Encoding.UTF8.GetString(reversed)
        '    Catch
        '        Return ""
        '    End Try
        'End Function

        ' ===================================================================
        ' ПУБЛИЧНЫЕ СВОЙСТВА ДЛЯ ДОСТУПА ИЗВНЕ
        ' ===================================================================
        Public ReadOnly Property RoadLogin As String
            Get
                Return txtLogin.Text : End Get
        End Property

        Public ReadOnly Property RoadPassword As String
            Get
                Return txtPassword.Password : End Get
        End Property

        Public ReadOnly Property CentralLogin As String
            Get
                Return txtCentralLogin.Text : End Get
        End Property

        Public ReadOnly Property CentralPassword As String
            Get
                Return txtCentralPassword.Password : End Get
        End Property

        Public Property IsExpanded As Boolean
            Get
                Return expAuth.IsExpanded : End Get
            Set(value As Boolean)
                expAuth.IsExpanded = value : End Set
        End Property

    End Class
End Namespace

