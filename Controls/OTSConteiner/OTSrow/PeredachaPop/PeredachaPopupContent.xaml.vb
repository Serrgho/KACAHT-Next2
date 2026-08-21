Imports System.ComponentModel
Imports System.Windows.Controls.Primitives

Namespace Kas

    Partial Public Class PeredachaPopupContent
        Inherits UserControl
        Implements INotifyPropertyChanged


        Public Event Confirmed(pred As String, addToHistory As Boolean, eventDate As DateTime?, comment As String, oldPred As String)
        Public Event Canceled()

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
        Protected Sub OnProperty(<Runtime.CompilerServices.CallerMemberName> Optional propName As String = Nothing)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propName))
        End Sub

        Private _currentButton As Button = Nothing
        Private _selectedPred As String = ""
        Private _eventDate As DateTime = DateTime.Now
        Private _comment As String = ""
        Private _oldPred As String = ""


        Public Property currentButton As Button
            Get
                Return _currentButton
            End Get
            Set
                _currentButton = Value
                OnProperty(NameOf(currentButton))
            End Set
        End Property

        Public Property SelectedPred As String
            Get
                Return _selectedPred
            End Get
            Private Set(value As String)
                _selectedPred = value
                OnProperty(NameOf(SelectedPred))
            End Set
        End Property

        Public Property EventDate As DateTime
            Get
                Return _eventDate
            End Get
            Set(value As DateTime)
                _eventDate = value
                OnProperty(NameOf(EventDate))
            End Set
        End Property

        Public Property Comment As String
            Get
                Return _comment
            End Get
            Set(value As String)
                _comment = value
                OnProperty(NameOf(Comment))
            End Set
        End Property

        Public Property OldPred As String
            Get
                Return _oldPred
            End Get
            Set(value As String)
                _oldPred = value
            End Set
        End Property

        Public ReadOnly Property IsHistoryEnabled As Boolean
            Get
                Return HistoryCheckBox.IsChecked = True
            End Get
        End Property

        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().

        End Sub

        Private Sub PredButton_Click(sender As Object, e As RoutedEventArgs)
            ResetOldSelectOfButton()
            Dim btn = CType(sender, Button)
            ' Выделение новой
            btn.FontWeight = FontWeights.Bold
            btn.Background = Brushes.LightBlue
            btn.Foreground = Brushes.DarkBlue
            currentButton = btn
            SelectedPred = CStr(btn.Tag)
            Comment = ""
            If SelectedPred.ToLower.Contains("тч") Then
                If OldPred.ToLower.Contains("тч") Then
                    Comment = "для завершения расследования"
                End If
            ElseIf SelectedPred.ToLower.Contains("трп") Then
                'If OldPred.ToLower.Contains
            End If
        End Sub

        Sub ResetOldSelectOfButton()
            ' Сброс старой — БЕЗ ЦИКЛОВ!
            If currentButton IsNot Nothing Then
                ' Сбрасываем ЛОКАЛЬНЫЕ значения → возвращаемся к стилю
                currentButton.ClearValue(Button.FontWeightProperty)
                currentButton.ClearValue(Button.BackgroundProperty)
                currentButton.ClearValue(Button.ForegroundProperty)

            End If

        End Sub

        Private Sub HistoryCheckBox_Toggled(sender As Object, e As RoutedEventArgs)
            HistSP.Visibility = If(HistoryCheckBox.IsChecked, Visibility.Visible, Visibility.Collapsed)

        End Sub

        Private Sub OkButton_Click(sender As Object, e As RoutedEventArgs)
            If String.IsNullOrEmpty(SelectedPred) Then
                MessageBox.Show("Выберите предприятие!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            RaiseEvent Confirmed(SelectedPred, IsHistoryEnabled,
                                If(IsHistoryEnabled, EventDate, Nothing),
                                If(IsHistoryEnabled, Comment.Trim(), ""),
                                OldPred)
        End Sub

        Private Sub CancelButton_Click(sender As Object, e As RoutedEventArgs)
            RaiseEvent Canceled() ' ← добавляем событие отмены
        End Sub





    End Class
End Namespace

