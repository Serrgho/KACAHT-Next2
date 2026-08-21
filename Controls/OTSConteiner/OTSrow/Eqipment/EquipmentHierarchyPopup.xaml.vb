Imports System.Windows.Controls.Primitives
Imports Microsoft.Office.Interop.Word

Namespace Kas
    Partial Public Class EquipmentHierarchyPopup

        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().

        End Sub

        Public Sub ResetSelection()
            Level2ListBox.SelectedIndex = -1
            Level3ListBox.ItemsSource = Nothing
            Level1TextBlock.Text = ""
            OkButton.IsEnabled = False
        End Sub

        Private Sub UserControl_Loaded(sender As Object, e As RoutedEventArgs)
            EnsureHierarchyLoaded()
            Level2ListBox.ItemsSource = EquipmentHierarchyModule.AllLevel2.OrderBy(Function(item) item).ToList

        End Sub

        Private Sub Level2ListBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            If Level2ListBox.SelectedItem IsNot Nothing Then
                Dim l2 = Level2ListBox.SelectedItem.ToString()
                Dim l1 = EquipmentHierarchyModule.GetLevel1ForLevel2(l2)
                Level1TextBlock.Text = l1

                Dim l3List = If(EquipmentHierarchyModule.Level2ToLevel3.ContainsKey(l2),
                              EquipmentHierarchyModule.Level2ToLevel3(l2),
                              New List(Of String) From {"—"})
                Level3ListBox.ItemsSource = l3List.OrderBy(Function(item) item).ToList
                Level3ListBox.SelectedIndex = -1
            End If
        End Sub


        Private Sub Level3ListBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            OkButton.IsEnabled = (Level2ListBox.SelectedItem IsNot Nothing) AndAlso
                                 (Level3ListBox.SelectedItem IsNot Nothing)
        End Sub

        Private Sub OkButton_Click(sender As Object, e As RoutedEventArgs)

            If Level2ListBox.SelectedItem Is Nothing OrElse Level3ListBox.SelectedItem Is Nothing Then Return

            Dim l2 = Level2ListBox.SelectedItem.ToString()
            Dim l3 = Level3ListBox.SelectedItem.ToString()
            Dim l1 = EquipmentHierarchyModule.GetLevel1ForLevel2(l2)

            PointedOtkaz.MyKlasLev1 = l1
            PointedOtkaz.MyKlasLev2 = l2
            PointedOtkaz.MyKlasLev3 = l3

            MW.EquipmentPopup.IsOpen = False
        End Sub

        Private Sub CancelButton_Click(sender As Object, e As RoutedEventArgs)
            MW.EquipmentPopup.IsOpen = False
        End Sub

        Private Sub AddLevel3Button_Click(sender As Object, e As RoutedEventArgs)
            AddNewLevel3()
        End Sub

        Private Sub NewLevel3Input_KeyDown(sender As Object, e As KeyEventArgs)
            If e.Key = Key.Enter Then AddNewLevel3()
        End Sub

        Private Sub AddNewLevel3()

            Dim text = NewLevel3Input.Text?.Trim()
            If String.IsNullOrEmpty(text) Then Return

            Dim l2 = Level2ListBox.SelectedItem?.ToString()
            If String.IsNullOrEmpty(l2) Then Return

            ' Добавление + автоматическое сохранение внутри AddLevel3Item
            EquipmentHierarchyModule.AddLevel3Item(l2, text)

            ' Обновляем только UI — данные уже обновлены!
            Dim l3List = EquipmentHierarchyModule.Level2ToLevel3(l2).OrderBy(Function(x) x).ToList()
            Level3ListBox.ItemsSource = l3List
            Level3ListBox.SelectedIndex = -1
            NewLevel3Input.Clear()
            NewLevel3Input.Focus()



        End Sub

        Private Sub Level3ListBox_KeyDown(sender As Object, e As KeyEventArgs)
            If e.Key = Key.Delete Then
                Dim l2 = Level2ListBox.SelectedItem?.ToString()
                Dim l3 = Level3ListBox.SelectedItem?.ToString()

                If Not String.IsNullOrEmpty(l2) AndAlso Not String.IsNullOrEmpty(l3) Then
                    EquipmentHierarchyModule.RemoveLevel3Item(l2, l3)

                    ' Обновляем Level3 список
                    Dim l3List = EquipmentHierarchyModule.Level2ToLevel3(l2).OrderBy(Function(x) x).ToList()
                    Level3ListBox.ItemsSource = l3List
                    Level3ListBox.SelectedIndex = -1

                    ' Отключаем OK-кнопку, так как выделение сброшено
                    OkButton.IsEnabled = False
                End If
            End If
        End Sub

        Private Sub LoadBaseButton_Click(sender As Object, e As RoutedEventArgs)
            EquipmentHierarchyModule.ResetToDefaultAndSave()
            ' Обновляем UI
            Level2ListBox.ItemsSource = EquipmentHierarchyModule.AllLevel2
            ResetSelection()
        End Sub
    End Class
End Namespace

