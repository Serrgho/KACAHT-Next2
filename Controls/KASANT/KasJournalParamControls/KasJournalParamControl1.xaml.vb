Namespace Kas
	Partial Public Class KasJournalParamControl1

		Sub New()

			' Этот вызов является обязательным для конструктора.
			InitializeComponent()

			' Добавить код инициализации после вызова InitializeComponent().

		End Sub

        Private Sub TextBox_SelectAll_OnPreviewMouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
            Dim tb As Controls.TextBox = TryCast(sender, Controls.TextBox)

            ' Стандартный UX: выделяем текст только при первом клике (получении фокуса).
            ' Если кликнуть второй раз, когда фокус уже есть — курсор встанет в нужное место для редактирования.
            If tb IsNot Nothing AndAlso Not tb.IsKeyboardFocused Then
                tb.SelectAll()
                tb.Focus()
                e.Handled = True ' Отменяем стандартное поведение, чтобы курсор не встал в середину
            End If
        End Sub



        Private Sub But18_Checked(sender As Object, e As RoutedEventArgs)
            With Fetcher
                .NachTim = 18
                .KonTim = 18
                .KonMinut = 0

            End With
        End Sub

        Private Sub But23_Checked(sender As Object, e As RoutedEventArgs)
            With Fetcher
                .NachTim = 0
                .KonTim = 23
                .KonMinut = 59
            End With
        End Sub

		Private Sub FilterWithTimeOTSList_Click(sender As Object, e As RoutedEventArgs)
            Dim ExclusivePeriod As Func(Of Otkaz, Boolean) = (Function(o) o.Nach.IsInRangeWithTime AndAlso Not (o.KtoZakryl.ToLower.Contains("трп")))
            Dim OTSLs = OTSList.Where(ExclusivePeriod).OrderBy(Function(o) o.Nach).ToList
            AddOTSToContainer(OTSLs)
            MW.TRowsContainer.UpdateTotal()
        End Sub
	End Class
End Namespace

