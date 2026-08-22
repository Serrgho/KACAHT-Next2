Imports System.ComponentModel
Imports System.Runtime.CompilerServices

Namespace Kas

    Public Class PlanEntry
        Implements INotifyPropertyChanged

        ' ==========================================
        ' 1. ЭТАЛОННЫЙ СПИСОК (НУЖЕН ТОЛЬКО ДЛЯ СРАВНЕНИЯ/ВАЛИДАЦИИ)
        ' К раскраске в красный он НЕ имеет отношения!
        ' ==========================================
        Public Shared ReadOnly ReferenceReasonsList As New List(Of String) From {
        "На др дорогу", 'обрабатывается при передаче на др дорогу
        "В технологию",
        "На удаление",
        "письмо НЗ-1", 'обрабатывается при передаче на др дорогу
        "За СЛД",
        "За заводом",
        "Корректировка",
        "в 3 категорию",
        "за ТЧЭ"
        }

        Public Shared ReadOnly GreenReasons As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {
        "На др дорогу",
        "В технологию",
        "На удаление",
        "письмо НЗ-1"
        }

        Public Shared ReadOnly ReferenceReasons As New HashSet(Of String)(ReferenceReasonsList, StringComparer.OrdinalIgnoreCase)

        Private _description As String = String.Empty
        Private _isHighlighted As Boolean = False
        Private _isGreen As Boolean = False

        ' ==========================================
        ' 2. СВОЙСТВО ОПИСАНИЯ
        ' ==========================================
        Public Property Description As String
            Get
                Return _description
            End Get
            Set(value As String)
                ' Чистим от пробелов на случай кривого импорта
                Dim cleanValue As String = If(value, String.Empty).Trim()

                If _description <> cleanValue Then
                    _description = cleanValue
                    OnPropertyChanged(NameOf(Description))
                    OnPropertyChanged(NameOf(DisplayText))

                    ' Запускаем расчет цвета
                    UpdateIsGreen()
                End If
            End Set
        End Property

        ' ==========================================
        ' 3. ЛОГИКА КРАСНОГО ЦВЕТА (ТОЛЬКО "На др дорогу"!)
        ' ==========================================
        Private Sub UpdateIsGreen()


            Dim shouldBeGreen As Boolean = GreenReasons.Contains(_description)

            ' Обновляем свойство. Если было черным, а стало красным - UI обновится.
            IsGreen = shouldBeGreen

        End Sub

        ' ==========================================
        ' 4. ОСТАЛЬНЫЕ СВОЙСТВА
        ' ==========================================
        Public Property IsHighlighted As Boolean
            Get
                Return _isHighlighted
            End Get
            Set(value As Boolean)
                If _isHighlighted <> value Then
                    _isHighlighted = value
                    OnPropertyChanged(NameOf(IsHighlighted))
                End If
            End Set
        End Property

        Public Property IsGreen As Boolean
            Get
                Return _isGreen
            End Get
            Set(value As Boolean)
                If _isGreen <> value Then
                    _isGreen = value
                    OnPropertyChanged(NameOf(IsGreen)) ' <-- Триггер для WPF триггера/DataTrigger
                End If
            End Set
        End Property

        Public ReadOnly Property DisplayText As String
            Get
                Return $"План: {_description}"
            End Get
        End Property

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Protected Sub OnPropertyChanged(<CallerMemberName> Optional propertyName As String = Nothing)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
        End Sub
    End Class

End Namespace


