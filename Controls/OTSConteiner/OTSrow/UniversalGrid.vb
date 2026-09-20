Namespace Kas
    Public Class UniversalGrid
        Inherits Grid

        Public Sub New()
            Dim widths = {
        40,  ' 0 — треугольник
        30,  ' 1 — чекбокс
        90,  ' 2 — ID + кат + PCh
        80,  ' 3 — Нач
        70,  ' 4 — дн.
        90,  ' 5 — (день недели) Поступил
        90,  ' 6 — (день недели) вернулся
        60,  ' 7 — Источник
        70,  ' 8 — Кто закрыл
        200, ' 9 — Место отказа
        550, ' 10 — Opis
        120, ' 11 — MyKlasLev3
        120, ' 12 - пробеги
        95,  ' 13 — ZaKem
        6,  ' 14 — ------------
        140  ' 15 — UpdateNotes (пометки)
    }
            For Each w In widths
                Me.ColumnDefinitions.Add(New ColumnDefinition With {.Width = New GridLength(w)})
            Next
        End Sub
    End Class
End Namespace