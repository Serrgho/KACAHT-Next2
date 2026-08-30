
Namespace Kas

    Public Class TableRowData

        ' ===== БАЗОВЫЕ СПИСКИ (заполняются вручную) =====
        ' Т
        Public Property T1List As List(Of Otkaz) = Nothing
        Public Property T2List As List(Of Otkaz) = Nothing
        Public Property T3List As List(Of Otkaz) = Nothing

        ' ТР
        Public Property TR1List As List(Of Otkaz) = Nothing
        Public Property TR2List As List(Of Otkaz) = Nothing
        Public Property TR3List As List(Of Otkaz) = Nothing

        ' СЛД
        Public Property SLD1List As List(Of Otkaz) = Nothing
        Public Property SLD2List As List(Of Otkaz) = Nothing
        Public Property SLD3List As List(Of Otkaz) = Nothing

        ' Заводы
        Public Property Factory1List As List(Of Otkaz) = Nothing
        Public Property Factory2List As List(Of Otkaz) = Nothing
        Public Property Factory3List As List(Of Otkaz) = Nothing

        ' ===== ВЫЧИСЛЯЕМЫЕ СПИСКИ (ReadOnly) =====

        ' ----- Т -----
        Public ReadOnly Property T12List As List(Of Otkaz)
            Get
                Return T1List?.Concat(T2List).ToList()
            End Get
        End Property

        Public ReadOnly Property T13List As List(Of Otkaz)
            Get
                Return T1List?.Concat(T2List)?.Concat(T3List).ToList()
            End Get
        End Property

        ' ----- ТР -----
        Public ReadOnly Property TR12List As List(Of Otkaz)
            Get
                Return TR1List?.Concat(TR2List).ToList()
            End Get
        End Property

        Public ReadOnly Property TR13List As List(Of Otkaz)
            Get
                Return TR1List?.Concat(TR2List)?.Concat(TR3List).ToList()
            End Get
        End Property

        ' ----- СЛД -----
        Public ReadOnly Property SLD12List As List(Of Otkaz)
            Get
                Return SLD1List?.Concat(SLD2List).ToList()
            End Get
        End Property

        Public ReadOnly Property SLD13List As List(Of Otkaz)
            Get
                Return SLD1List?.Concat(SLD2List)?.Concat(SLD3List).ToList()
            End Get
        End Property

        ' ----- Заводы -----
        Public ReadOnly Property Factory12List As List(Of Otkaz)
            Get
                Return Factory1List?.Concat(Factory2List).ToList()
            End Get
        End Property

        Public ReadOnly Property Factory13List As List(Of Otkaz)
            Get
                Return Factory1List?.Concat(Factory2List).Concat(Factory3List).ToList()
            End Get
        End Property

        ' ----- КОМПЛЕКС (агрегат) -----
        Public ReadOnly Property Complex1List As List(Of Otkaz)
            Get
                Return T1List?.Concat(TR1List)?.Concat(SLD1List)?.Concat(Factory1List)?.ToList()
            End Get
        End Property

        Public ReadOnly Property Complex2List As List(Of Otkaz)
            Get
                Return T2List?.Concat(TR2List)?.Concat(SLD2List)?.Concat(Factory2List)?.ToList()
            End Get
        End Property

        Public ReadOnly Property Complex3List As List(Of Otkaz)
            Get
                Return T3List?.Concat(TR3List)?.Concat(SLD3List)?.Concat(Factory3List)?.ToList()
            End Get
        End Property

        Public ReadOnly Property Complex12List As List(Of Otkaz)
            Get
                Return Complex1List?.Concat(Complex2List).ToList()
            End Get
        End Property

        Public ReadOnly Property Complex13List As List(Of Otkaz)
            Get
                Return Complex1List?.Concat(Complex2List)?.Concat(Complex3List)?.ToList()
            End Get
        End Property
    End Class

End Namespace

