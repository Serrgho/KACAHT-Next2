Namespace Kas
    'Этот класс отличается от TableRowData тем, что хранит готовые строковые значения (например, "+3", "-15.38%"), а не списки отказов. Он используется только для строк 8 и 9, где drill-down не нужен
    Public Class TableRowNumericData
        ' ===== Комплекс =====
        Public Property Complex12 As String = ""
        Public Property Complex3 As String = ""
        Public Property Complex13 As String = ""

        ' ===== Т =====
        Public Property T12 As String = ""
        Public Property T3 As String = ""
        Public Property T13 As String = ""

        ' ===== ТР =====
        Public Property TR12 As String = ""
        Public Property TR3 As String = ""
        Public Property TR13 As String = ""

        ' ===== СЛД =====
        Public Property SLD12 As String = ""
        Public Property SLD3 As String = ""
        Public Property SLD13 As String = ""

        ' ===== Заводы =====
        Public Property Factory12 As String = ""
        Public Property Factory3 As String = ""
        Public Property Factory13 As String = ""
    End Class

End Namespace

