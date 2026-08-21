Namespace Kas

    Partial Public Class RoadSourceWindow
        ' Список железных дорог (без "ГИД УРАЛ" и без "РУЧНОЙ ВВОД")
        Private ReadOnly RoadsList As New List(Of String) From {
            "ВСЖД", "ЗАБЖД", "ДВЖД", "ЗСЖД",
            "ПРИВЖД", "СКАВЖД", "ЮВЖД", "ЮУРЖД", "КБШЖД",
            "ГОРЖД", "СВРДЖД", "ОКТЖД", "КЛНГЖД", "СЕВЖД", "САХЖД"
        }

        ' Цели передачи
        Private ReadOnly PurposesList As New List(Of String) From {
            "---",
            "на закрытие",
            "предоставление документов",
            "для расследования",
            "корректировка",
            "объединение дубликатов",
            "корректировка документов",
            "не согласован с СЛД",
            "не указана причина отказа",
            "особое мнение",
            "с письмом НЗ-1"
        }

        Public Property SelectedRoad As String
        Public Property SelectedPurpose As String

        Sub New()

            InitializeComponent()
            PopulateLists()

            ' Подписка на события для живого превью
            AddHandler CustomPurposeTB.TextChanged, AddressOf UpdatePreview
            For Each rb In RoadPanel.Children.OfType(Of RadioButton)()
                AddHandler rb.Checked, AddressOf UpdatePreview
            Next
            For Each rb In PurposePanel.Children.OfType(Of RadioButton)()
                AddHandler rb.Checked, AddressOf UpdatePreview
            Next

            ' Первоначальное обновление
            UpdatePreview(Nothing, Nothing)



            'InitializeComponent()
            'PopulateLists()
        End Sub


        Private Sub UpdatePreview(sender As Object, e As RoutedEventArgs)
            ' Получаем выбранную дорогу
            Dim roadRb = RoadPanel.Children.OfType(Of RadioButton)().FirstOrDefault(Function(r) r.IsChecked)
            Dim road As String = If(roadRb IsNot Nothing, roadRb.Content.ToString(), "???")

            ' Определяем причину: TextBox имеет приоритет
            Dim purpose As String = ""
            If Not String.IsNullOrWhiteSpace(CustomPurposeTB.Text) Then
                purpose = CustomPurposeTB.Text.Trim()
            Else
                Dim purposeRb = PurposePanel.Children.OfType(Of RadioButton)().FirstOrDefault(Function(r) r.IsChecked)
                purpose = If(purposeRb IsNot Nothing, purposeRb.Content.ToString(), "???")
            End If

            ' Формируем текст превью
            PreviewTextBlock.Text = $"Будет записано: поступил с/передан на другой(-ую) дороги(-у) ({road}, {purpose})"
        End Sub




        Private Sub PopulateLists()
            ' Левая колонка: Дороги
            RoadPanel.Children.Clear()
            For Each road In RoadsList
                Dim rb As New RadioButton() With {
                    .Content = road,
                    .Margin = New Thickness(5, 2, 5, 2),
                    .GroupName = "Roads"
                }
                RoadPanel.Children.Add(rb)
            Next
            If RoadPanel.Children.Count > 0 Then
                DirectCast(RoadPanel.Children(0), RadioButton).IsChecked = True
            End If

            ' Правая колонка: Цели
            PurposePanel.Children.Clear()
            For Each purpose In PurposesList
                Dim rb As New RadioButton() With {
                    .Content = purpose,
                    .Margin = New Thickness(5, 2, 5, 2),
                    .GroupName = "Purposes"
                }
                PurposePanel.Children.Add(rb)
            Next
            If PurposePanel.Children.Count > 0 Then
                DirectCast(PurposePanel.Children(0), RadioButton).IsChecked = True
            End If
        End Sub

        Private Sub OkButton_Click(sender As Object, e As RoutedEventArgs)


            ' Дорога
            Dim selectedRoadRb = RoadPanel.Children.OfType(Of RadioButton)().FirstOrDefault(Function(rb) rb.IsChecked)
            If selectedRoadRb Is Nothing Then
                MessageBox.Show("Выберите дорогу", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning)
                Return
            End If

            ' Цель: приоритет у TextBox
            Dim finalPurpose As String = ""
            If Not String.IsNullOrWhiteSpace(CustomPurposeTB.Text) Then
                finalPurpose = CustomPurposeTB.Text.Trim()
            Else
                Dim selectedPurposeRb = PurposePanel.Children.OfType(Of RadioButton)().FirstOrDefault(Function(rb) rb.IsChecked)
                If selectedPurposeRb Is Nothing Then
                    MessageBox.Show("Выберите цель передачи", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning)
                    Return
                End If
                finalPurpose = selectedPurposeRb.Content.ToString()
            End If

            SelectedRoad = selectedRoadRb.Content.ToString()
            SelectedPurpose = finalPurpose

            DialogResult = True
            Close()




            'Dim selectedRoadRb = RoadPanel.Children.OfType(Of RadioButton)().FirstOrDefault(Function(rb) rb.IsChecked)
            'If selectedRoadRb Is Nothing Then
            '    MessageBox.Show("Выберите дорогу", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning)
            '    Return
            'End If

            'Dim selectedPurposeRb = PurposePanel.Children.OfType(Of RadioButton)().FirstOrDefault(Function(rb) rb.IsChecked)
            'If selectedPurposeRb Is Nothing Then
            '    MessageBox.Show("Выберите цель передачи", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning)
            '    Return
            'End If

            'SelectedRoad = selectedRoadRb.Content.ToString()
            'SelectedPurpose = selectedPurposeRb.Content.ToString()

            'DialogResult = True
            'Close()
        End Sub

        Private Sub CancelButton_Click(sender As Object, e As RoutedEventArgs)
            DialogResult = False
            Close()
        End Sub

    End Class

End Namespace