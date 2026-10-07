
Namespace Kas

	Partial Public Class FoundOtkazWindow

		Sub New()

			' Этот вызов является обязательным для конструктора.
			InitializeComponent()

			' Добавить код инициализации после вызова InitializeComponent().

		End Sub


		Public Sub New(items As IEnumerable(Of Otkaz))
			InitializeComponent()
			dgFound.ItemsSource = items
		End Sub


	End Class
End Namespace

