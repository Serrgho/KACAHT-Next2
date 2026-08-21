Imports System.Windows.Controls
Namespace Kas
    Partial Public Class UniversalGrid
        Inherits Grid  ' ← ОБЯЗАТЕЛЬНО Grid, не UserControl!
        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()


        End Sub

        Private Sub UserControl_Loaded(sender As Object, e As RoutedEventArgs)
            ' Добавить код инициализации после вызова InitializeComponent().
            For Each w In {40, 40, 90, 30, 80, 50, 80, 80, 400, 480, 110, 30, 80, 50, 80, 80}
                Me.ColumnDefinitions.Add(New ColumnDefinition() With {.Width = New GridLength(w)})
            Next
        End Sub
    End Class
End Namespace

