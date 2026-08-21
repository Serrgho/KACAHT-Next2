Imports System.Windows.Threading
Imports KACAHT_Next2.Kas.CalendarControl

Namespace Kas


    Partial Public Class SingleCalendarControl

        Private _IsOpen As Boolean = True
        Private _PlaceTarget As UIElement
        Private _editMode As String = "" ' Добавляем поле для режима редактирования

        Public Property PlaceTarget As UIElement
            Get
                Return _PlaceTarget
            End Get
            Set
                _PlaceTarget = Value
                CalendarPopup.PlacementTarget = PlaceTarget
            End Set
        End Property

        Public Property IsOpen As Boolean
            Get
                Return _IsOpen
            End Get
            Set
                _IsOpen = Value
                CalendarPopup.IsOpen = Value
            End Set
        End Property

        ' Свойство для режима редактирования
        Public Property EditMode As String
            Get
                Return _editMode
            End Get
            Set(value As String)
                _editMode = value
            End Set
        End Property

        ' Событие с обновленными аргументами
        Public Event DateSelected As EventHandler(Of DateSelectedEventArgs)

        Public Shared ReadOnly TargetDateProperty As DependencyProperty =
            DependencyProperty.Register("TargetDate", GetType(DateTime?), GetType(SingleCalendarControl), New PropertyMetadata(Nothing))

        Public Property TargetDate As DateTime?
            Get
                Return CType(GetValue(TargetDateProperty), DateTime?)
            End Get
            Set(value As DateTime?)
                SetValue(TargetDateProperty, value)
            End Set
        End Property

        Sub New()
            InitializeComponent()
            AddHandler MyCalendar0.SelectedDateChanged, AddressOf OnSelectedNachDateChanged
        End Sub

        Private Sub UserControl_Loaded(sender As Object, e As RoutedEventArgs)

        End Sub


        Private Sub OnSelectedNachDateChanged(sender As Object, e As SelectedDateChangedEventArgs)
            'вот тут устанавливается дата в целевое свойство
            TargetDate = e.NewDate.Date

            ' Создаем аргументы с датой и режимом
            Dim args As New DateSelectedEventArgs(e.NewDate.Date) With {
                .EditMode = Me.EditMode
            }

            RaiseEvent DateSelected(Me, args)

            ' Закрыть попап
            Me.IsOpen = False
        End Sub

        Private Sub OKBU_Click(sender As Object, e As RoutedEventArgs)
            'MyCalendar0.ParPop.IsOpen = True
        End Sub

        Sub ClearHandlers()
            RemoveHandler MyCalendar0.SelectedDateChanged, AddressOf OnSelectedNachDateChanged
        End Sub

        Private Sub CalendarPopup_Closed(sender As Object, e As EventArgs)

            CalendarPopup.IsOpen = False
        End Sub


    End Class

    ' Обновленный класс аргументов события
    Public Class DateSelectedEventArgs
        Inherits EventArgs

        Public Property SelectedDate As DateTime
        Public Property EditMode As String
        Public Property Source As Object
        Public Property Handled As Boolean
        Public Property EventId As String  ' Уникальный идентификатор события!

        Sub New(d As DateTime)
            SelectedDate = d
            EditMode = String.Empty
            Source = Nothing
            Handled = False
            EventId = Guid.NewGuid().ToString()
        End Sub

        Sub New(d As DateTime, mode As String)
            SelectedDate = d
            EditMode = mode
            Source = Nothing
            Handled = False
            EventId = Guid.NewGuid().ToString()
        End Sub


    End Class
End Namespace
