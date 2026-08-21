Namespace Kas
    Module YarlykControlModule
        Public Event PointedYarlykChanged As EventHandler



        Private _pointedYarlyk As Yarlyk = Nothing
        'это свойство для хранения выбранного мышью отказа
        Public Property PointedYarlyk As Yarlyk
            Get
                Return _pointedYarlyk
            End Get
            Set(value As Yarlyk)
                ' Сбрасываем старый объект
                If _pointedYarlyk IsNot Nothing Then
                    _pointedYarlyk.Pointed = False
                End If

                ' Устанавливаем новый объект
                _pointedYarlyk = value
                If _pointedYarlyk IsNot Nothing Then
                    _pointedYarlyk.Pointed = True

                End If

                ' Уведомляем об изменении
                RaiseEvent PointedYarlykChanged(Nothing, EventArgs.Empty)
            End Set
        End Property

        ' Метод для обновления DataContext UserControl
        Public Sub UpdateYarlykDataContext(control As Yarlyk)
            If control IsNot Nothing Then
                control.DataContext = PointedYarlyk
            End If
        End Sub
    End Module

End Namespace


