Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Runtime.CompilerServices
Imports KACAHT_Next2.Kas.Otkaz

Namespace Kas

    Partial Public Class HistoryEditor


        Implements INotifyPropertyChanged

        ' --- Событие для передачи данных родителю ---
        Public Event EntryAdded As EventHandler(Of HistoryEntry)
        Public Event Cancelled As EventHandler ' Новое событие для отмены

        ' --- Реализация интерфейса INotifyPropertyChanged ---
        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Protected Sub NotifyPropertyChanged(<CallerMemberName> Optional propName As String = Nothing)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
        End Sub
        ' ----------------------------------------------------

        ' --- Свойства для привязки (Binding) внутри XAML этого контрола ---

        Public Shared ReadOnly ShowDateProperty As DependencyProperty =
            DependencyProperty.Register("ShowDate", GetType(Boolean), GetType(HistoryEditor), New PropertyMetadata(True))

        Public Property ShowDate As Boolean
            Get
                Return CType(GetValue(ShowDateProperty), Boolean)
            End Get
            Set(value As Boolean)
                SetValue(ShowDateProperty, value)
            End Set
        End Property

        Public Shared ReadOnly EventDateProperty As DependencyProperty =
            DependencyProperty.Register("EventDate", GetType(DateTime?), GetType(HistoryEditor), New PropertyMetadata(DateTime.Now))

        Public Property EventDate As DateTime?
            Get
                Return CType(GetValue(EventDateProperty), DateTime?)
            End Get
            Set(value As DateTime?)
                SetValue(EventDateProperty, value)
            End Set
        End Property

        Public Shared ReadOnly DescriptionProperty As DependencyProperty =
            DependencyProperty.Register("Description", GetType(String), GetType(HistoryEditor), New PropertyMetadata(String.Empty))

        Public Property Description As String
            Get
                Return CType(GetValue(DescriptionProperty), String)
            End Get
            Set(value As String)
                SetValue(DescriptionProperty, value)
            End Set
        End Property

        Sub New()
            InitializeComponent()
            ' Изначально скрываем панель, чтобы она не занимала место в Grid родителя
            Me.Visibility = Visibility.Collapsed
        End Sub

        ''' <summary>
        ''' Вызывается из родителя при нажатии на кнопку "+"
        ''' </summary>
        Public Sub ShowInput()
            Me.Visibility = Visibility.Visible
            ' Ставим фокус на поле ввода для удобства
            TxtDescription.Focus()
        End Sub

        ''' <summary>
        ''' Обработчик кнопки "Добавить" внутри контрола
        ''' </summary>
        Private Sub BtnAdd_Click(sender As Object, e As RoutedEventArgs)
            ' Простая валидация
            If String.IsNullOrWhiteSpace(Description) Then
                Return
            End If

            ' Формируем объект записи
            Dim newEntry As New HistoryEntry With {
                .EventDate = If(EventDate.HasValue, EventDate.Value, DateTime.Now),
                .Description = Description.Trim(),
                .ShowDate = ShowDate
            }

            ' Отправляем запись родителю через событие
            RaiseEvent EntryAdded(Me, newEntry)

            ' Сбрасываем состояние
            ClearAndCollapse()
        End Sub

        ''' <summary>
        ''' Обработчик кнопки "Отмена"
        ''' </summary>
        Private Sub BtnCancel_Click(sender As Object, e As RoutedEventArgs)
            ' Очищаем поля и скрываем контрол
            ClearAndCollapse()

            ' Уведомляем родителя, что ввод отменен
            RaiseEvent Cancelled(Me, EventArgs.Empty)
        End Sub

        ''' <summary>
        ''' Очищает поля и скрывает контрол
        ''' </summary>
        Private Sub ClearAndCollapse()
            Description = String.Empty
            EventDate = DateTime.Now
            ShowDate = True
            Me.Visibility = Visibility.Collapsed
        End Sub

        ''' <summary>
        ''' Обработка нажатия Enter в поле ввода
        ''' </summary>
        Private Sub TxtDescription_PreviewKeyDown(sender As Object, e As KeyEventArgs)
            ' Проверяем, что нажат именно Enter (а не NumpadEnter, если нужно - добавь OrElse e.Key = Key.NumPadEnter)
            If e.Key = Key.Enter Then
                ' Если зажат Shift или Ctrl, то обычно пользователь хочет перенос строки, а не отправку
                ' Если тебе нужен перенос по Shift+Enter, оставь эту проверку. 
                ' Если Enter всегда добавляет запись - убери проверку модификаторов.
                If Keyboard.Modifiers = ModifierKeys.None Then

                    ' Эмулируем клик по кнопке добавления
                    BtnAdd_Click(Nothing, Nothing)

                    ' Помечаем событие как обработанное, чтобы оно не ушло дальше (например, не вызвало поиск в окне)
                    e.Handled = True
                End If
            End If
        End Sub


    End Class
End Namespace

