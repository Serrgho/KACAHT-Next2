Namespace Kas

	Partial Public Class HistoryOTSControl

		Sub New()

			' Этот вызов является обязательным для конструктора.
			InitializeComponent()

			' Добавить код инициализации после вызова InitializeComponent().

		End Sub

		Private Sub ListBoxItem_MouseRightButtonDown(sender As Object, e As MouseButtonEventArgs)
			Dim item = TryCast(sender, ListBoxItem)
			If item Is Nothing Then Return

			'Dim _selectedEntry = DirectCast(item.DataContext, HistoryEntry)
			Dim _selectedEntry = TryCast(item.DataContext, HistoryEntry)
			If _selectedEntry Is Nothing Then Return ' Или выход, если данные не те

			_selectedEntry.IsHighlighted = True
			HistoryUC.SelectedEntry = _selectedEntry
			HistoryPop.IsOpen = True
		End Sub

		' Показываем панель ввода при клике на плюс
		Private Sub BtnAddHistory_Click(sender As Object, e As RoutedEventArgs)
			HistEditorCtrl.ShowInput()
			BtnAddHistory.Visibility = Visibility.Collapsed
		End Sub

		' Получаем данные от редактора и добавляем их в модель
		Private Sub HistEditorCtrl_EntryAdded(sender As Object, entry As HistoryEntry)
			' Предполагаем, что DataContext этого контрола - это Otkaz
			Dim currentOtkaz = TryCast(DataContext, Otkaz)

			If currentOtkaz IsNot Nothing Then
				currentOtkaz.History.Add(entry)

				' Прокручиваем список к новой записи
				HistList.ScrollIntoView(entry)
				' Возвращаем кнопку на место
				BtnAddHistory.Visibility = Visibility.Visible
			End If
		End Sub


		Private Sub HistEditorCtrl_Cancelled(sender As Object, e As EventArgs)
			BtnAddHistory.Visibility = Visibility.Visible
		End Sub

		Private Sub HistoryPop_Closed(sender As Object, e As EventArgs)
			' Сбрасываем подсветку при закрытии попапа
			HistoryUC.ClearHighlight()
		End Sub


	End Class
End Namespace

