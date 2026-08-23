Imports System.Collections.ObjectModel
Imports System.Windows.Controls.Primitives

Imports System.Windows.Media
Namespace Kas

    Partial Public Class HistoryControl
        Inherits UserControl
        ' ✅ 1. Объявляем DependencyProperty
        Public Shared ReadOnly PopupOwnerProperty As DependencyProperty =
    DependencyProperty.Register(
        NameOf(PopupOwner),
        GetType(Popup),
        GetType(HistoryControl),
        New FrameworkPropertyMetadata(Nothing)
    )

        ' ✅ 2. Обёртка CLR-свойства
        Public Property PopupOwner As Popup
            Get
                Return CType(GetValue(PopupOwnerProperty), Popup)
            End Get
            Set(value As Popup)
                SetValue(PopupOwnerProperty, value)
            End Set
        End Property

        ' ✅ 1. Объявляем DependencyProperty
        Public Shared ReadOnly SelectedEntryProperty As DependencyProperty =
        DependencyProperty.Register(
            NameOf(SelectedEntry),
            GetType(HistoryEntry),
            GetType(HistoryControl),
            New FrameworkPropertyMetadata(Nothing, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        )
        ' Ссылка на выбранный элемент истории
        Public Property SelectedEntry As HistoryEntry
            Get
                Return CType(GetValue(SelectedEntryProperty), HistoryEntry)
            End Get
            Set(value As HistoryEntry)
                SetValue(SelectedEntryProperty, value)
            End Set
        End Property

        Public ParLBox As ListBox


        Sub New()
            InitializeComponent()
            ' ✅ 3. Важно: DataContext контрола должен указывать на сам контрол
            ' чтобы Binding RelativeSource={RelativeSource AncestorType=UserControl} видел свойства
            Me.DataContext = Me
        End Sub





        Private Sub BtnUP_PreviewMouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
            e.Handled = True ' ✅ Останавливаем всплытие к родителю
            If SelectedEntry Is Nothing OrElse PointedOtkaz Is Nothing Then Return

            Dim idx = PointedOtkaz.History.IndexOf(SelectedEntry)
            If idx > 0 Then
                Swap(Of HistoryEntry)(PointedOtkaz.History(idx), PointedOtkaz.History(idx - 1))
                SelectedEntry = PointedOtkaz.History(idx - 1)

                ' ✅ Просто обновляем выделение на новом элементе
                SelectedEntry.IsHighlighted = True

                'Dim Selecteditm = DirectCast(PointedOtkaz.History(idx - 1), HistoryEntry)
                PointedOtkaz.NotifyHistoryChanged()

            End If

        End Sub

        Private Sub BtnDOWN_PreviewMouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
            If SelectedEntry Is Nothing OrElse PointedOtkaz Is Nothing Then Return

            Dim idx = PointedOtkaz.History.IndexOf(SelectedEntry)
            If idx >= 0 AndAlso idx < PointedOtkaz.History.Count - 1 Then
                Swap(Of HistoryEntry)(PointedOtkaz.History(idx), PointedOtkaz.History(idx + 1))
                SelectedEntry = PointedOtkaz.History(idx + 1)

                ' ✅ Просто обновляем выделение на новом элементе
                SelectedEntry.IsHighlighted = True

                PointedOtkaz.NotifyHistoryChanged()

            End If
            e.Handled = True ' ✅ Останавливаем всплытие к родителю
        End Sub

        Public Sub ClearHighlight()
            If SelectedEntry IsNot Nothing Then
                SelectedEntry.IsHighlighted = False
                SelectedEntry = Nothing
            End If
        End Sub

        Private Sub BtnDel_PreviewMouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
            'фокус теряется
            Dim idx = PointedOtkaz.History.IndexOf(SelectedEntry)
            If Not ShowMSG(MW, "Удалить запись из истории?", "Удаление записи", buttons:=MsgButtons.OKCancel) Then Exit Sub
            PointedOtkaz.History.RemoveAt(idx)
            ClearHighlight()
            ' ✅ Закрываем Popup
            If PopupOwner IsNot Nothing Then
                PopupOwner.IsOpen = False
            End If
        End Sub

        Private Sub BtnCopy_PreviewMouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
            With SelectedEntry
                Clipboard.SetText($"{ .DisplayText}")
            End With
            ' ✅ Закрываем Popup
            If PopupOwner IsNot Nothing Then
                PopupOwner.IsOpen = False
            End If
        End Sub
    End Class

End Namespace