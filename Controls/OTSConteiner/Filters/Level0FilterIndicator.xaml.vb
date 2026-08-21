Imports System.Windows
Imports System.Windows.Controls
Namespace Kas

    Partial Public Class Level0FilterIndicator
        Public Shared ReadOnly PropertyNameProperty As DependencyProperty = DependencyProperty.Register(
            "PropertyName",
            GetType(String),
            GetType(Level0FilterIndicator),
            New PropertyMetadata(Nothing)
        )

        Public Property PropertyName As String
            Get
                Return CStr(GetValue(PropertyNameProperty))
            End Get
            Set(value As String)
                SetValue(PropertyNameProperty, value)
            End Set
        End Property

        Public Shared ReadOnly ThIsFilteredProperty As DependencyProperty = DependencyProperty.Register(
            "ThIsFiltered",
            GetType(Boolean),
            GetType(Level0FilterIndicator),
            New PropertyMetadata(False, AddressOf OnThIsFilteredChanged) ' Добавим callback
        )

        Public Property ThIsFiltered As Boolean
            Get
                Return CBool(GetValue(ThIsFilteredProperty))
            End Get
            Set(value As Boolean)
                SetValue(ThIsFilteredProperty, value)
            End Set
        End Property

        Private Shared Sub OnThIsFilteredChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim instance = DirectCast(d, Level0FilterIndicator)
            instance.IndicatorEllipse.Visibility = If(instance.ThIsFiltered, Visibility.Visible, Visibility.Collapsed)
        End Sub

        Public Sub New()
            InitializeComponent()
        End Sub

        ' Метод для внешнего вызова обновления состояния
        ' Принимает список всех фильтров, чтобы проверить, активен ли этот конкретный PropertyName
        Public Sub UpdateIsFiltered(level0FilterState As FilterState)
            If level0FilterState IsNot Nothing AndAlso Not String.IsNullOrEmpty(PropertyName) Then
                Dim values = level0FilterState.GetFilter(PropertyName)
                '    ' Проверяем, как и раньше: активен, если есть значения <> "[Все]" (для строк) или FilterValue (для чисел/дат, если применимо)
                '    ' Учитываем, что 0-уровень, скорее всего, только строки

                '    Dim hasActiveString = values.Any(Function(v) TypeOf v Is String AndAlso v.ToString() <> "[Все]")
                '    ' Если 0-уровень может быть числом/датой, добавь проверку FilterValue
                '    ThIsFiltered = hasActiveString
                'Else
                '    ThIsFiltered = False
                'End If
                '' Visibility обновится автоматически через Binding или через OnThIsFilteredChanged
                ' Проверяем активные строковые значения (<> "[Все]")


                ' Проверяем активные строковые значения (<> "[Все]")
                Dim hasActiveString = values.Any(Function(v) TypeOf v Is String AndAlso v.ToString() <> "[Все]")

                    ' Проверяем, есть ли вообще какие-либо FilterValue (это означает, что фильтр по числу/дате активен)
                    ' Если FilterValue существует, он всегда означает активный фильтр, в отличие от "[Все]"
                    Dim hasActiveFilterValue = values.Any(Function(v) TypeOf v Is FilterValue)

                    ' Фильтр считается активным, если есть либо активная строка, либо любой FilterValue
                    ThIsFiltered = hasActiveString OrElse hasActiveFilterValue
                Else
                    ThIsFiltered = False
                End If
    ' Visibility обновится автоматически через Binding или через OnThIsFilteredChanged
        End Sub


    End Class
End Namespace


