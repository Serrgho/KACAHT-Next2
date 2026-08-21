Namespace Kas

    Module MessageHelper

        Public Enum MsgButtons
            OK
            OKCancel
        End Enum

        Public Function ShowMSG(owner As Window, Optional message As String = "", Optional title As String = "ИНФОРМАЦИЯ", Optional buttons As MsgButtons = MsgButtons.OK, Optional icon As MessageBoxImage = MessageBoxImage.None) As Boolean?
            Dim showCancel = (buttons = MsgButtons.OKCancel)
            Dim dialog = New ModernMessageBox(owner, title, message, showCancel)

            ' Настройка иконки (если реализовано в ModernMessageBox)
            'If icon <> MessageBoxImage.None Then
            '    dialog.Icon = GetIcon(icon) ' Ваш метод для получения иконки
            'End If

            Return dialog.ShowDialog()
        End Function

    End Module

End Namespace

