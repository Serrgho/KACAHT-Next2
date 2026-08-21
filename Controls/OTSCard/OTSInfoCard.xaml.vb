Namespace Kas
    Partial Public Class OTSInfoCard

        Public Shared ReadOnly CurrentOtkazProperty As DependencyProperty =
    DependencyProperty.Register(
        NameOf(CurrentOtkaz),
        GetType(Otkaz),
        GetType(OTSInfoCard),
        New PropertyMetadata(Nothing, AddressOf OnCurrentOtkazChanged)
    )

        Public Property CurrentOtkaz As Otkaz
            Get
                Return CType(GetValue(CurrentOtkazProperty), Otkaz)
            End Get
            Set(value As Otkaz)
                SetValue(CurrentOtkazProperty, value)
            End Set
        End Property

        ' Добавляем флаг, чтобы избежать двойных обновлений
        Private _isUpdating As Boolean = False

        Private Shared Sub OnCurrentOtkazChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)

            Dim ctrl = CType(d, OTSInfoCard)

            If ctrl._isUpdating Then Exit Sub
            ctrl._isUpdating = True

            Try
                Dim otkaz = CType(e.NewValue, Otkaz)
                ctrl.DataContext = otkaz

                ' ТОЛЬКО то, что НЕ работает через привязки:

                ctrl.UpdateStatusColors()    ' Цвета (зависит от Kat)

                ' ВСЁ остальное - через привязки в XAML
            Finally
                ctrl._isUpdating = False
            End Try


            ' В XAML: простые поля через привязку, сложные - через методы




        End Sub

        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().
            AddHandler DataContextChanged, AddressOf OnDataContextChanged
        End Sub

        Private Sub OnDataContextChanged(sender As Object, e As DependencyPropertyChangedEventArgs)
            Debug.WriteLine($"OnDataContextChanged: OldValue = {e.OldValue}, NewValue = {e.NewValue}")
            'SetLastText()

        End Sub





        Private Sub UpdateStatusColors()
            If CurrentOtkaz Is Nothing Then Exit Sub

            Dim statusBorder = TryCast(Me.FindName("StatusBorder"), Border)
            If statusBorder Is Nothing Then Exit Sub
            LastAction.Foreground = New SolidColorBrush(Colors.White) ' Контрастный текст
            LastAction.FontWeight = FontWeights.DemiBold
            CloseBUTBlock.Foreground = Brushes.White ' Контрастный цвет
            CloseButtonBorder.BorderBrush = Brushes.White

            Select Case CurrentOtkaz.Kat
                Case "1", "2"
                    statusBorder.Background = New SolidColorBrush(Colors.DarkRed)
                    'statusBorder.BorderBrush = New SolidColorBrush(Colors.DarkRed)
                    'LastAction.Foreground = New SolidColorBrush(Colors.White) ' Контрастный текст
                    'LastAction.FontWeight = FontWeights.Bold
                    'CloseBUTBlock.Foreground = Brushes.White ' Контрастный цвет
                    'CloseButtonBorder.BorderBrush = Brushes.White
                'Case "2"
                '    statusBorder.Background = New SolidColorBrush(Colors.LightYellow)
                '    statusBorder.BorderBrush = New SolidColorBrush(Colors.Orange)
                '    LastAction.Foreground = New SolidColorBrush(Colors.DarkOrange) ' Не чёрный, а тёмно-оранжевый
                '    LastAction.FontWeight = FontWeights.Bold
                '    CloseBUTBlock.Foreground = Brushes.DarkOrange ' Контрастный
                '    CloseButtonBorder.BorderBrush = Brushes.DarkOrange
                Case "3"
                    statusBorder.Background = New SolidColorBrush(Colors.DarkGreen)
                    'statusBorder.BorderBrush = New SolidColorBrush(Colors.DarkGreen)
                    'LastAction.Foreground = New SolidColorBrush(Colors.White) ' Контрастный текст
                    'LastAction.FontWeight = FontWeights.Bold
                    'CloseBUTBlock.Foreground = Brushes.DarkGreen ' Контрастный
                    'CloseButtonBorder.BorderBrush = Brushes.DarkGreen
                Case Else
                    statusBorder.Background = New SolidColorBrush(Colors.LightGray)
                    statusBorder.BorderBrush = New SolidColorBrush(Colors.Gray)
                    LastAction.Foreground = New SolidColorBrush(Colors.Black) ' Стандартный чёрный
                    LastAction.FontWeight = FontWeights.Normal
                    CloseBUTBlock.Foreground = Brushes.Black ' Стандартный
                    CloseButtonBorder.BorderBrush = Brushes.Gray
            End Select
            CloseButtonBorder.Background = statusBorder.Background
        End Sub

        Private Sub CloseButton_Click(sender As Object, e As RoutedEventArgs)
            ' Просто сбрасываем CurrentOtkaz
            PointedOtkaz = Nothing

            MW.MainTabControl.SelectedItem = MW.MainPage
        End Sub

        Private Sub CloseButtonBorder_MouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
            '' Основное действие закрытия
            'PointedOtkaz = Nothing
            'MW.MainTabControl.SelectedItem = MW.MainPage
            'e.Handled = True
        End Sub

        Private Sub CloseButtonBorder_PreviewMouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
            ' Можно добавить визуальную обратную связь при нажатии
            ' Основное действие закрытия
            PointedOtkaz = Nothing
            MW.MainTabControl.SelectedItem = MW.MainPage
            e.Handled = True
            'CloseButtonBorder.Background = New SolidColorBrush(Color.FromRgb(200, 50, 50))
        End Sub

        Private Sub MashField_PreviewMouseUp(sender As Object, e As MouseButtonEventArgs)
            If e.ChangedButton = MouseButton.Right Then
                MW.PripMashPopup.Istochnik = PointedOtkaz?.Istochnik 'чтобы знать, нужен машинист или нет
                MW.PripisMash.PlacementTarget = MW
                MW.PripisMash.IsOpen = True
            End If

        End Sub

        Private Sub LokField_PreviewMouseUp(sender As Object, e As MouseButtonEventArgs)
            If e.ChangedButton = MouseButton.Right Then
                MW.SerLokPopup.IsOpen = True
            End If
        End Sub

        Private Sub MyKlasLev3Field_PreviewMouseUp(sender As Object, e As MouseButtonEventArgs)
            If e.ChangedButton = MouseButton.Right Then
                MW.EquipmentContent.ResetSelection()
                MW.EquipmentPopup.IsOpen = True
            End If
        End Sub
    End Class
End Namespace

