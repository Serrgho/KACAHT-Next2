Imports System.Windows.Controls.Primitives

Namespace Kas

    Module CalendarHelper


        '    ' Глобальная переменная для хранения заблокированного элемента
        '    Public BlockedElement As UIElement = Nothing
        '    Public IsConfirmed As Boolean = False ' Глобальный флаг для подтверждения выбора

        '    Public Event DialogClosed(ByVal isConfirmed As Boolean)

        '    Public Function ShowCalendarDialog(popup As Popup) As Date?
        '        ' Блокируем родительский контейнер
        '        If BlockedElement IsNot Nothing Then
        '            Throw New InvalidOperationException("Предыдущий элемент уже заблокирован.")
        '        End If

        '        ' Устанавливаем заблокированный элемент
        '        BlockedElement = popup.PlacementTarget

        '        ' Блокируем элемент
        '        BlockedElement.IsEnabled = False

        '        ' Переменные для хранения состояния
        '        Dim selectedDate As Date? = Nothing

        '        ' Получаем ссылки на календари из XAML
        '        Dim calendar1 As CalendarControl = MW.MyCalendar1
        '        Dim calendar2 As CalendarControl = MW.MyCalendar2

        '        ' Подписываемся на событие выбора даты из первого календаря
        '        AddHandler calendar1.DateSelected, Sub(sender, e)
        '                                               selectedDate = e.SelectedDate
        '                                           End Sub

        '        ' Подписываемся на событие выбора даты из второго календаря
        '        AddHandler calendar2.DateSelected, Sub(sender, e)
        '                                               selectedDate = e.SelectedDate
        '                                           End Sub

        '        ' Обработчик закрытия попапа
        '        AddHandler popup.Closed, Sub(sender, e)
        '                                     ' Восстанавливаем активность заблокированного элемента
        '                                     If BlockedElement IsNot Nothing Then
        '                                         BlockedElement.IsEnabled = True
        '                                         BlockedElement = Nothing
        '                                     End If
        '                                     ' Генерируем событие закрытия диалога
        '                                     RaiseEvent DialogClosed(IsConfirmed)
        '                                 End Sub

        '        ' Открываем попап
        '        popup.IsOpen = True

        '        ' Открываем попап
        '        popup.IsOpen = True

        '        ' Ждем, пока попап открыт
        '        While popup.IsOpen
        '            ' Обработка сообщений UI потока
        '            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
        'System.Windows.Threading.DispatcherPriority.Background,
        'New Action(Sub() Exit Sub)) ' Пустое действие
        '        End While

        '        ' Возвращаем результат
        '        Return selectedDate
        '    End Function
    End Module

End Namespace

