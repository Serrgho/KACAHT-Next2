Imports System.ComponentModel
Imports System.Reflection

Namespace Kas
    Module OTSControlModule
        Public Event PointedOtkazChanged As EventHandler

        Private _pointedOtkaz As Otkaz = Nothing
        'это свойство для хранения выбранного мышью отказа
        Public Property PointedOtkaz As Otkaz
            Get
                Return _pointedOtkaz
            End Get
            Set(value As Otkaz)
                Debug.WriteLine($"PointedOtkaz SET: {If(value Is Nothing, "Nothing", value.Id)}")
                ' Сбрасываем старый объект
                If _pointedOtkaz IsNot Nothing Then
                    _pointedOtkaz.Pointed = False
                End If

                ' Устанавливаем новый объект
                _pointedOtkaz = value
                If _pointedOtkaz IsNot Nothing Then
                    _pointedOtkaz.Pointed = True
                End If

                RaiseEvent PointedOtkazChanged(Nothing, EventArgs.Empty)

                ' ← обновляем видимость вкладки СРАЗУ
                'If Application.Current.MainWindow IsNot Nothing Then

                '    MW.OTSCard.Visibility = If(_pointedOtkaz IsNot Nothing, Visibility.Visible, Visibility.Collapsed)
                '    MW.OTSCard.Header = If(_pointedOtkaz?.Id, "-----")
                'End If
            End Set
        End Property

        'Потому что XAML не любит generic-типы в статических свойствах модуля. Object — безопасно.
        Public ReadOnly Property BindingPoint As Object
            Get
                Return PointedOtkaz  ' ← просто возвращает текущее значение
            End Get
        End Property








    End Module
End Namespace


