Namespace Kas
	Partial Public Class NumOTSEditBox
        Inherits UserControl

        Public Event Cleared As EventHandler
        Public Event Copied As EventHandler

        Public Shared ReadOnly TextProperty As DependencyProperty =
            DependencyProperty.Register("Text", GetType(String), GetType(NumOTSEditBox),
                New FrameworkPropertyMetadata(String.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, AddressOf OnTextPropertyChanged))

        Public Property Text As String
            Get
                Return CType(Me.GetValue(TextProperty), String)
            End Get
            Set(value As String)
                Me.SetValue(TextProperty, value)
            End Set
        End Property

        Private Shared Sub OnTextPropertyChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim ctrl As NumOTSEditBox = CType(d, NumOTSEditBox)
            If Not ctrl.isInternalUpdate Then
                Dim caretIndex = ctrl.PART_TextBox.CaretIndex
                ctrl.PART_TextBox.Text = CType(e.NewValue, String)
                ctrl.PART_TextBox.CaretIndex = Math.Min(caretIndex, ctrl.PART_TextBox.Text.Length)
            End If
        End Sub

        Public Shared ReadOnly CountProperty As DependencyProperty =
            DependencyProperty.Register("Count", GetType(Integer), GetType(NumOTSEditBox),
                New FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, AddressOf OnCountPropertyChanged))

        Public Property Count As Integer
            Get
                Return CType(Me.GetValue(CountProperty), Integer)
            End Get
            Set(value As Integer)
                Me.SetValue(CountProperty, value)
            End Set
        End Property

        Private Shared Sub OnCountPropertyChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim ctrl As NumOTSEditBox = CType(d, NumOTSEditBox)
            Dim newCount = CType(e.NewValue, Integer)
            If newCount > 0 Then
                ctrl.Stat.Foreground = Brushes.Green
                ctrl.Stat.Visibility = Visibility.Visible ' ✅ Показываем
            Else
                ctrl.Stat.Foreground = CType(Application.Current.FindResource("SplitterBrush"), Brush)
                ctrl.Stat.Visibility = Visibility.Collapsed ' ✅ Скрываем
            End If
        End Sub

        Private isInternalUpdate As Boolean = False

        Sub New()

			' Этот вызов является обязательным для конструктора.
			InitializeComponent()

			' Добавить код инициализации после вызова InitializeComponent().

		End Sub

        Public Sub FocusTextBox()
            PART_TextBox.Focus()
        End Sub

        Private Sub UpdateStat()
            Dim sourceText = PART_TextBox.Text
            Dim cnt = If(sourceText, "").Split(","c).
                                      Select(Function(s) s.Trim()).
                                      Where(Function(s) Not String.IsNullOrWhiteSpace(s)).
                                      Count()
            Me.Count = cnt
        End Sub

        Private Function InsertWithDuplicatesCheck(ByVal currentText As String, ByVal pastedText As String, ByVal caretIndex As Integer) As String
            If String.IsNullOrWhiteSpace(pastedText) Then Return currentText

            Dim separators = {","c, ";"c, ControlChars.Lf, ControlChars.Cr, " "c, vbTab, "."c, "-"c}
            Dim newItems = pastedText.Split(separators, StringSplitOptions.RemoveEmptyEntries).
                    Select(Function(s) New String(s.Where(Function(c) Char.IsDigit(c)).ToArray())).
                    Where(Function(s) Not String.IsNullOrWhiteSpace(s)).
                    Distinct().
                    ToList()

            If newItems.Count = 0 Then Return currentText

            Dim currentItems As New List(Of String)
            If Not String.IsNullOrWhiteSpace(currentText) Then
                currentItems = currentText.Split(","c).
                        Select(Function(s) s.Trim()).
                        Where(Function(s) Not String.IsNullOrWhiteSpace(s)).
                        ToList()
            End If

            Dim insertPosition As Integer = 0
            If currentItems.Count > 0 AndAlso caretIndex > 0 Then
                Dim textBeforeCursor = currentText.Substring(0, Math.Min(caretIndex, currentText.Length))
                insertPosition = textBeforeCursor.Split(","c).Count(Function(s) Not String.IsNullOrWhiteSpace(s.Trim()))
                insertPosition = Math.Min(insertPosition, currentItems.Count)
            Else
                insertPosition = currentItems.Count
            End If

            Dim uniqueNewItems = newItems.Where(Function(item) Not currentItems.Contains(item)).ToList()
            If uniqueNewItems.Count = 0 Then Return currentText

            Dim result As New List(Of String)
            For i As Integer = 0 To insertPosition - 1
                If i < currentItems.Count Then result.Add(currentItems(i))
            Next
            result.AddRange(uniqueNewItems)
            For i As Integer = insertPosition To currentItems.Count - 1
                result.Add(currentItems(i))
            Next

            Return String.Join(",", result)
        End Function

        Private Sub PART_TextBox_PreviewTextInput(sender As Object, e As TextCompositionEventArgs)
            Dim textBox = CType(sender, TextBox)
            If e.Text.Any(Function(c) Char.IsDigit(c)) Then Return
            If e.Text = "," Then Return
            If e.Text = "." Then
                textBox.SelectedText = ","
                textBox.CaretIndex = textBox.CaretIndex + 1
                e.Handled = True
                Return
            End If
            e.Handled = True
        End Sub

        Private Sub PART_TextBox_PreviewKeyDown(sender As Object, e As KeyEventArgs)
            Dim textBox = CType(sender, TextBox)
            If Keyboard.Modifiers.HasFlag(ModifierKeys.Control) Then
                If e.Key = Key.C OrElse e.Key = Key.A OrElse e.Key = Key.X OrElse e.Key = Key.V Then Return
            End If
            If e.Key = Key.Return Then
                textBox.SelectedText = ","
                textBox.CaretIndex = textBox.CaretIndex + 1
                e.Handled = True
                Return
            End If
            If e.Key = Key.Back OrElse e.Key = Key.Delete OrElse e.Key = Key.Left OrElse e.Key = Key.Right OrElse e.Key = Key.Home OrElse e.Key = Key.End OrElse e.Key = Key.Tab Then Return
            If e.Key = Key.OemComma OrElse e.Key = Key.OemPeriod OrElse e.Key = Key.Decimal Then Return
            If (e.Key >= Key.D0 AndAlso e.Key <= Key.D9) OrElse (e.Key >= Key.NumPad0 AndAlso e.Key <= Key.NumPad9) Then Return
            e.Handled = True
        End Sub

        Private Sub PART_TextBox_Pasting(sender As Object, e As DataObjectPastingEventArgs)
            If e.DataObject.GetDataPresent(DataFormats.Text) Then
                e.CancelCommand()
                Dim newText = InsertWithDuplicatesCheck(PART_TextBox.Text, e.DataObject.GetData(DataFormats.Text), PART_TextBox.CaretIndex)
                If newText <> PART_TextBox.Text Then
                    isInternalUpdate = True
                    PART_TextBox.Text = newText
                    PART_TextBox.CaretIndex = newText.Length
                    UpdateTextProperty()
                    isInternalUpdate = False
                End If
            End If
        End Sub

        Private Sub UpdateTextProperty()
            If Text <> PART_TextBox.Text Then
                isInternalUpdate = True
                Text = PART_TextBox.Text
                isInternalUpdate = False
                UpdateStat()
            End If
        End Sub

        Public Function GetItems() As List(Of String)
            Dim sourceText = PART_TextBox.Text
            Return If(sourceText, "").Split(","c).
                             Select(Function(s) s.Trim()).
                             Where(Function(s) Not String.IsNullOrWhiteSpace(s)).
                             ToList()
        End Function

        Public Sub SetItems(items As IEnumerable(Of String))
            Dim caretIndex = PART_TextBox.CaretIndex
            Text = String.Join(",", items.Where(Function(s) Not String.IsNullOrWhiteSpace(s)))
            PART_TextBox.CaretIndex = Math.Min(caretIndex, PART_TextBox.Text.Length)
            UpdateStat()
        End Sub

        Private Sub PART_TextBox_TextChanged(sender As Object, e As TextChangedEventArgs)
            If Not isInternalUpdate Then
                UpdateStat()
                UpdateTextProperty()
            End If
        End Sub

        Private Sub BtnClear_MouseUp(sender As Object, e As MouseButtonEventArgs)
            isInternalUpdate = True
            PART_TextBox.Text = String.Empty
            Text = String.Empty
            isInternalUpdate = False
            UpdateStat()
            RaiseEvent Cleared(Me, EventArgs.Empty)
            PART_TextBox.Focus()
        End Sub

        Private Sub BtnCopy_MouseUp(sender As Object, e As MouseButtonEventArgs)
            If Not String.IsNullOrWhiteSpace(PART_TextBox.Text) Then
                Clipboard.SetText(PART_TextBox.Text)
                RaiseEvent Copied(Me, EventArgs.Empty)
            End If
        End Sub

    End Class
End Namespace

