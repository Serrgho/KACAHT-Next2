Module ReportBuilder

    Sub BuildSummaryReportSkeleton(doc As FlowDocument)
        doc.Blocks.Clear()
        doc.FontSize = 12
        doc.FontFamily = New FontFamily("Segoe UI")

        Dim table As New Table()
        doc.Blocks.Add(table)

        ' --- 21 колонка ---
        For i As Integer = 1 To 21
            table.Columns.Add(New TableColumn() With {.Width = New GridLength(70)})
        Next

        Dim group As New TableRowGroup()
        table.RowGroups.Add(group)

        ' 🔹 Строка 1: Верхние заголовки
        Dim row1 As New TableRow()
        AddMergedCell(row1, "Анализ КАСАНТ.", 2, Brushes.LightGray, True)
        AddMergedCell(row1, "Комплекс", 4, Brushes.LightGray, True)
        AddMergedCell(row1, "Т", 4, Brushes.LightGray, True)
        AddMergedCell(row1, "ТР", 4, Brushes.LightGray, True)
        AddMergedCell(row1, "СЛД", 4, Brushes.LightGray, True)
        AddMergedCell(row1, "Заводы и прочие предприятия", 3, Brushes.LightGray, True)
        group.Rows.Add(row1)

        ' 🔹 Строка 2: Подзаголовки
        Dim row2 As New TableRow()
        AddHeaderCell(row2, "Период")
        AddHeaderCell(row2, "Год")
        AddHeaderCell(row2, "1кат")
        AddHeaderCell(row2, "2кат")
        AddHeaderCell(row2, "3кат")
        AddHeaderCell(row2, "1-3кат")
        AddHeaderCell(row2, "1кат")
        AddHeaderCell(row2, "2кат")
        AddHeaderCell(row2, "3кат")
        AddHeaderCell(row2, "1-3кат")
        AddHeaderCell(row2, "1кат")
        AddHeaderCell(row2, "2кат")
        AddHeaderCell(row2, "3кат")
        AddHeaderCell(row2, "1-3кат")
        AddHeaderCell(row2, "1кат")
        AddHeaderCell(row2, "2кат")
        AddHeaderCell(row2, "3кат")
        AddHeaderCell(row2, "1-3кат")
        AddHeaderCell(row2, "1кат")
        AddHeaderCell(row2, "2кат")
        AddHeaderCell(row2, "3кат")
        group.Rows.Add(row2)

        ' 🔹 Строка 3: "12Mec" (RowSpan=4 в колонке 1) — не объединяем с Годом
        Dim row3 As New TableRow()
        AddDataCellWithRowSpan(row3, "12Mec", 4)  ' ← RowSpan=4, только в 1-й колонке
        AddDataCell(row3, "2024")  ' ← 2-я колонка — отдельная
        For i As Integer = 1 To 19 : AddDataCell(row3, "") : Next  ' ← данные с 3-й колонки
        group.Rows.Add(row3)

        ' 🔹 Строка 4
        Dim row4 As New TableRow()
        ' ← 1-я колонка пропущена (RowSpan из row3)
        AddDataCell(row4, "2023")  ' ← 2-я колонка
        For i As Integer = 1 To 19 : AddDataCell(row4, "") : Next
        group.Rows.Add(row4)

        ' 🔹 Строка 5
        Dim row5 As New TableRow()
        ' ← 1-я колонка пропущена
        AddDataCell(row5, "2024")  ' ← 2-я колонка
        For i As Integer = 1 To 19 : AddDataCell(row5, "") : Next
        group.Rows.Add(row5)

        ' 🔹 Строка 6
        Dim row6 As New TableRow()
        ' ← 1-я колонка пропущена
        AddDataCell(row6, "2023")  ' ← 2-я колонка
        For i As Integer = 1 To 19 : AddDataCell(row6, "") : Next
        group.Rows.Add(row6)

        ' 🔹 Строка 7: "Lenb 12 Mec..2024" — объединение 1+2 колонок (ColumnSpan=2)
        Dim row7 As New TableRow()
        AddDataCellWithColumnSpan(row7, "Lenb 12 Mec..2024", 2)  ' ← ColumnSpan=2
        For i As Integer = 1 To 19 : AddDataCell(row7, "") : Next  ' ← данные с 3-й колонки
        group.Rows.Add(row7)

        ' 🔹 Строка 8: "M3MeHeHMA CTY4." — объединение 1+2 колонок (ColumnSpan=2)
        Dim row8 As New TableRow()
        AddDataCellWithColumnSpan(row8, "M3MeHeHMA CTY4.", 2)  ' ← ColumnSpan=2
        For i As Integer = 1 To 19 : AddDataCell(row8, "") : Next
        group.Rows.Add(row8)

        ' 🔹 Строка 9: "%M3MEHEHMM" — объединение 1+2 колонок (ColumnSpan=2)
        Dim row9 As New TableRow()
        AddDataCellWithColumnSpan(row9, "%M3MEHEHMM", 2)  ' ← ColumnSpan=2
        For i As Integer = 1 To 19 : AddDataCell(row9, "") : Next
        group.Rows.Add(row9)

        ' 🔹 Строка 10: "12 Mec. 2023" — объединение 1+2 колонок (ColumnSpan=2)
        Dim row10 As New TableRow()
        AddDataCellWithColumnSpan(row10, "12 Mec. 2023", 2)  ' ← ColumnSpan=2
        For i As Integer = 1 To 19 : AddDataCell(row10, "") : Next
        group.Rows.Add(row10)

        doc.Blocks.Add(table)
    End Sub

    ' Вспомогательные методы:
    Private Sub AddHeaderCell(row As TableRow, text As String)
        Dim p As New Paragraph(New Run(text)) With {
        .Margin = New Thickness(2),
        .FontWeight = FontWeights.Bold,
        .TextAlignment = TextAlignment.Center
    }
        Dim cell As New TableCell(p) With {
        .BorderBrush = Brushes.Black,
        .BorderThickness = New Thickness(1),
        .Background = Brushes.Gainsboro
    }
        row.Cells.Add(cell)
    End Sub

    Private Sub AddMergedCell(row As TableRow, text As String, colSpan As Integer, bg As Brush, bold As Boolean)
        Dim run As New Run(text)
        If bold Then run.FontWeight = FontWeights.Bold

        Dim p As New Paragraph(run) With {
        .Margin = New Thickness(4, 2, 4, 2),
        .TextAlignment = TextAlignment.Center
    }

        Dim cell As New TableCell(p) With {
        .ColumnSpan = colSpan,
        .BorderBrush = Brushes.Black,
        .BorderThickness = New Thickness(1),
        .Background = bg
    }
        row.Cells.Add(cell)
    End Sub

    Private Sub AddDataCellWithRowSpan(row As TableRow, text As String, rowSpan As Integer)
        Dim p = New Paragraph(New Run(text)) With {.Margin = New Thickness(2)}
        Dim cell = New TableCell(p) With {
        .RowSpan = rowSpan,
        .BorderBrush = Brushes.Black,
        .BorderThickness = New Thickness(1)
    }
        row.Cells.Add(cell)
    End Sub

    Private Sub AddDataCellWithColumnSpan(row As TableRow, text As String, colSpan As Integer)
        Dim p = New Paragraph(New Run(text)) With {.Margin = New Thickness(2)}
        Dim cell = New TableCell(p) With {
        .ColumnSpan = colSpan,
        .BorderBrush = Brushes.Black,
        .BorderThickness = New Thickness(1)
    }
        row.Cells.Add(cell)
    End Sub

    Private Sub AddDataCell(row As TableRow, text As String)
        Dim p = New Paragraph(New Run(text)) With {.Margin = New Thickness(2)}
        Dim cell = New TableCell(p) With {
        .BorderBrush = Brushes.Black,
        .BorderThickness = New Thickness(1)
    }
        row.Cells.Add(cell)
    End Sub


End Module
