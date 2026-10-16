
Imports Microsoft.Office.Interop
Imports Microsoft.Office.Interop.Word

Namespace Kas
    Public Class WordPara
        Public Property Lead As String = ""
        Public Property Text As String = ""
        Public Property BoldOnly As Boolean = False
        Public Property ItalicOnly As Boolean = False
        Public Property FontSize As Integer = 16
        Public Property Alignment As Word.WdParagraphAlignment = Word.WdParagraphAlignment.wdAlignParagraphLeft
        Public Property PageBreakBefore As Boolean = False
        Public Property FirstLineIndent As Single = 0
        Public Property LeftIndent As Single = 0

        ' Пустой — для инициализатора
        Public Sub New()
        End Sub

        ' Один фрагмент
        Public Sub New(text As String)
            Me.Text = If(text, "")
        End Sub

        ' Один фрагмент с жирностью
        Public Sub New(text As String, boldOnly As Boolean)
            Me.Text = If(text, "")
            Me.BoldOnly = boldOnly
        End Sub

        ' Лид + текст
        Public Sub New(lead As String, text As String)
            Me.Lead = If(lead, "")
            Me.Text = If(text, "")
        End Sub

        ' Полный конструктор с выравниванием
        Public Sub New(text As String, boldOnly As Boolean, align As Word.WdParagraphAlignment)
            Me.Text = If(text, "")
            Me.BoldOnly = boldOnly
            Me.Alignment = align
        End Sub
    End Class
End Namespace

