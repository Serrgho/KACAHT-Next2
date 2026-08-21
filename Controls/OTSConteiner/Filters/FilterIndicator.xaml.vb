Namespace Kas
    Partial Public Class FilterIndicator


        ' DependencyProperty: имя свойства фильтра (например, "Kat")
        Public Shared ReadOnly PropertyNameProperty As DependencyProperty = DependencyProperty.Register(
            "PropertyName",
            GetType(String),
            GetType(FilterIndicator),
            New PropertyMetadata(Nothing, AddressOf OnPropertyNameChanged)
        )

        Public Property PropertyName As String
            Get
                Return CStr(GetValue(PropertyNameProperty))
            End Get
            Set(value As String)
                SetValue(PropertyNameProperty, value)
            End Set
        End Property

        ' DependencyProperty: активен ли фильтр
        Public Shared ReadOnly ThIsFilteredProperty As DependencyProperty = DependencyProperty.Register(
            "ThIsFiltered",
            GetType(Boolean),
            GetType(FilterIndicator),
            New PropertyMetadata(False)
        )


        Public Property ThIsFiltered As Boolean
            Get
                Return CBool(GetValue(ThIsFilteredProperty))
            End Get
            Set(value As Boolean)
                SetValue(ThIsFilteredProperty, value)
            End Set
        End Property


        ' Callback при изменении PropertyName
        Private Shared Sub OnPropertyNameChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim instance = DirectCast(d, FilterIndicator)
            instance.UpdateIsFiltered()
        End Sub


        ' --- НОВОЕ: DependencyProperty для ссылки на TableHeader ---
        Public Shared ReadOnly HeaderRefProperty As DependencyProperty = DependencyProperty.Register(
            "HeaderRef",
            GetType(Object), ' Используем Object, чтобы не создавать жёсткую зависимость, если TableHeader в другом сборке
            GetType(FilterIndicator),
            New PropertyMetadata(Nothing, AddressOf OnHeaderRefChanged)
        )

        Public Property HeaderRef As Object
            Get
                Return GetValue(HeaderRefProperty)
            End Get
            Set(value As Object)
                SetValue(HeaderRefProperty, value)
            End Set
        End Property
        ' --- КОНЕЦ НОВОГО ---

        ' --- НОВОЕ: Callback при изменении HeaderRef ---
        Private Shared Sub OnHeaderRefChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim instance = DirectCast(d, FilterIndicator)
            instance.UpdateIsFiltered()
        End Sub
        ' --- КОНЕЦ НОВОГО ---



        ' Конструктор
        Public Sub New()
            InitializeComponent()

            AddHandler Me.DataContextChanged, AddressOf FilterIndicator_DataContextChanged
        End Sub

        Private Sub FilterIndicator_DataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
            UpdateIsFiltered()
        End Sub

        ' 🔥 Основная логика: кроме значения "[Все]"
        Public Sub UpdateIsFiltered()
            ' Пытаемся преобразовать HeaderRef к TableHeader
            Dim header = TryCast(HeaderRef, TableHeader)

            If header IsNot Nothing AndAlso Not String.IsNullOrEmpty(PropertyName) Then
                Dim values = header._filterState.GetFilter(PropertyName)
                ThIsFiltered = values.Any(AddressOf IsFilterActive)
            Else
                ThIsFiltered = False
            End If
            IndicatorEllipse.Visibility = If(ThIsFiltered, Visibility.Visible, Visibility.Collapsed)
        End Sub
        Private Function IsFilterActive(v As Object) As Boolean
            If v Is Nothing Then Return False
            If TypeOf v Is String Then Return v.ToString() <> "[Все]"
            If TypeOf v Is FilterValue Then Return CType(v, FilterValue).Value IsNot Nothing
            Return False
        End Function

        ' Полезно, если TableHeader меняет состояние фильтра и хочет уведомить индикатор.
        Public Sub NotifyFilterChanged()
            UpdateIsFiltered()
        End Sub



    End Class

End Namespace
