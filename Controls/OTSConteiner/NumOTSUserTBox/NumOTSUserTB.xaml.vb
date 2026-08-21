Namespace Kas
    Partial Public Class NumOTSUserTB
        Inherits UserControl

		Public Event FilterRequested As EventHandler
		Public Event LokRequested As EventHandler
		Public Event Cleared As EventHandler
		Public Event Copied As EventHandler

		Public Shared ReadOnly TextProperty As DependencyProperty =
			DependencyProperty.Register("Text", GetType(String), GetType(NumOTSUserTB),
				New FrameworkPropertyMetadata(String.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, Nothing))

		Public Property Text As String
			Get
				Return CType(Me.GetValue(TextProperty), String)
			End Get
			Set(value As String)
				Me.SetValue(TextProperty, value)
			End Set
		End Property

		Public Shared ReadOnly CountProperty As DependencyProperty =
			DependencyProperty.Register("Count", GetType(Integer), GetType(NumOTSUserTB),
				New FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, Nothing))

		Public Property Count As Integer
			Get
				Return CType(Me.GetValue(CountProperty), Integer)
			End Get
			Set(value As Integer)
				Me.SetValue(CountProperty, value)
			End Set
		End Property

		Sub New()
			InitializeComponent()
		End Sub

		Public Function GetItems() As List(Of String)
			Return EditBox.GetItems()
		End Function

		Public Sub SetItems(items As IEnumerable(Of String))
			EditBox.SetItems(items)
		End Sub

		Private Sub BtnFilter_MouseUp(sender As Object, e As MouseButtonEventArgs)
			RaiseEvent FilterRequested(Me, EventArgs.Empty)
		End Sub

		Private Sub BtnFndLok_MouseUp(sender As Object, e As MouseButtonEventArgs)
			RaiseEvent LokRequested(Me, EventArgs.Empty)
		End Sub

		Private Sub EditBox_Cleared(sender As Object, e As EventArgs)
			RaiseEvent Cleared(Me, EventArgs.Empty)
		End Sub

		Private Sub EditBox_Copied(sender As Object, e As EventArgs)
			RaiseEvent Copied(Me, EventArgs.Empty)
		End Sub

	End Class
End Namespace


