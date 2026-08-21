Namespace Kas
    Partial Public Class FieldRow
        Public Shared ReadOnly TxtProperty As DependencyProperty =
    DependencyProperty.Register(NameOf(Txt), GetType(String), GetType(FieldRow),
        New PropertyMetadata("", AddressOf OnTxtChanged))

        Public Shared ReadOnly ValuProperty As DependencyProperty =
    DependencyProperty.Register(NameOf(Valu), GetType(String), GetType(FieldRow),
        New PropertyMetadata("", AddressOf OnValuChanged))

        ' Свойства-обёртки
        Public Property Txt As String
            Get
                Return CType(GetValue(TxtProperty), String)
            End Get
            Set(value As String)
                SetValue(TxtProperty, value)
            End Set
        End Property

        Public Property Valu As String
            Get
                Return CType(GetValue(ValuProperty), String)
            End Get
            Set(value As String)
                SetValue(ValuProperty, value)
            End Set
        End Property

        ' Колбэки
        Private Shared Sub OnTxtChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim ctrl = CType(d, FieldRow)
            ctrl.TextField.Text = CStr(e.NewValue)
        End Sub

        Private Shared Sub OnValuChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim ctrl = CType(d, FieldRow)

            ' Игнорируем UnsetValue
            If e.NewValue Is DependencyProperty.UnsetValue Then
                ctrl.ValueField.Text = "—"
                Return
            End If

            Dim s As String = Nothing

            ' Безопасное приведение к строке
            If e.NewValue IsNot Nothing Then
                s = e.NewValue.ToString()
            End If

            ' Проверяем пусто, Nothing, "!"
            If String.IsNullOrEmpty(s) OrElse s = "!" Then
                ctrl.ValueField.Text = "—"   ' ← одинарное тире (U+2014),Alt-код (NumPad обязателен) Alt + 0151 → получится —
            Else
                ctrl.ValueField.Text = s
            End If

        End Sub

        Public Sub New()
            InitializeComponent()
        End Sub

    End Class
End Namespace

