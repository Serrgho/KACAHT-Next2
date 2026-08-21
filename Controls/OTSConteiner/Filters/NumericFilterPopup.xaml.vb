Imports System.Globalization
Imports System.Windows.Controls
Imports System.Windows.Controls.Primitives

Namespace Kas
    Partial Public Class NumericFilterPopup
        Inherits UserControl

        Private _selectedOperator As String = "="
        Public ReadOnly Property SelectedOperator As String
            Get
                Return _selectedOperator
            End Get
        End Property

        Sub New()
            InitializeComponent()
            AddHandler Loaded, Sub(s, e) ValueBox.Focus()
        End Sub

        Public ReadOnly Property EnteredValue As Single?
            Get
                Dim s = ValueBox.Text.Trim()
                If String.IsNullOrEmpty(s) Then Return Nothing
                s = s.Replace(".", ",")
                Dim v As Single
                If Single.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, v) Then Return v
                If Single.TryParse(s, v) Then Return v
                Return Nothing
            End Get
        End Property

        Private Sub ValueBox_PreviewTextInput(sender As Object, e As TextCompositionEventArgs)
            Dim tb = CType(sender, TextBox)
            Dim c = e.Text(0)
            If Char.IsDigit(c) OrElse
               (c = "," AndAlso Not tb.Text.Contains(",")) OrElse
               (c = "-" AndAlso tb.SelectionStart = 0 AndAlso Not tb.Text.Contains("-")) Then
                Return
            End If
            e.Handled = True
        End Sub

        Private Sub OperatorButton_Click(sender As Object, e As RoutedEventArgs)
            Dim btn = CType(sender, Button)
            _selectedOperator = btn.Tag.ToString()
        End Sub

        Private Sub OperatorListBox_PreviewMouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
            Dim element = TryCast(e.OriginalSource, DependencyObject)
            Dim item As ListBoxItem = Nothing
            While element IsNot Nothing
                If TypeOf element Is ListBoxItem Then
                    item = DirectCast(element, ListBoxItem)
                    Exit While
                End If
                element = VisualTreeHelper.GetParent(element)
            End While

            If item?.Tag IsNot Nothing Then
                _selectedOperator = item.Tag.ToString()
                OperatorListBox.SelectedItem = item
                ApplyFilter()  ' ← ЕДИНСТВЕННОЕ действие
                e.Handled = True
            End If
        End Sub

        Public Event FilterApplied As EventHandler(Of FilterAppliedEventArgs)

        Public Sub ApplyFilter()
            Dim value = EnteredValue
            Dim op = SelectedOperator
            RaiseEvent FilterApplied(Me, New FilterAppliedEventArgs(value, op))
        End Sub
    End Class

    ' Класс события
    Public Class FilterAppliedEventArgs
        Inherits EventArgs
        Public Property Value As Single?
        Public Property Operatr As String

        Sub New(v As Single?, op As String)
            Value = v
            Operatr = op
        End Sub
    End Class

End Namespace

