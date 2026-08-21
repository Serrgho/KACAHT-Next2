Namespace Kas

    Partial Public Class ModernMessageBox
        Inherits Window

        Public Property MsgTitle As String

        ' Объявляем свойство как DependencyProperty (это обязательно для WPF)
        Public Shared ReadOnly MessageProperty As DependencyProperty =
        DependencyProperty.Register("Message", GetType(String), GetType(ModernMessageBox))

        ' Обычное свойство для доступа
        Public Property Message As String
            Get
                Return CStr(GetValue(MessageProperty))
            End Get
            Set(value As String)
                SetValue(MessageProperty, value)
            End Set
        End Property
        Public Property ShowCancel As Boolean

        ' Основной конструктор
        Public Sub New(owner As Window, title As String, message As String,
                      Optional showCancel As Boolean = True,
                      Optional startupLocation As WindowStartupLocation = WindowStartupLocation.CenterOwner)

            InitializeComponent()

            Me.Owner = owner
            Me.Title = title
            Me.Message = message
            Me.ShowCancel = showCancel
            Me.WindowStartupLocation = startupLocation
            Me.DataContext = Me
        End Sub

        Private Sub OkButton_Click(sender As Object, e As RoutedEventArgs)
            Me.DialogResult = True
        End Sub

        Private Sub CancelButton_Click(sender As Object, e As RoutedEventArgs)
            Me.DialogResult = False
        End Sub

        ' Статические методы для удобного вызова
        Public Shared Function MsgShow(owner As Window, title As String, message As String,
                                  Optional showCancel As Boolean = True) As Boolean?

            Dim dialog As New ModernMessageBox(owner, title, message, showCancel)
            Return dialog.ShowDialog()
        End Function

        Public Shared Sub ShowNonModal(owner As Window, title As String, message As String)
            Dim dialog As New ModernMessageBox(owner, title, message, False)
            dialog.Show()
        End Sub

    End Class

End Namespace


