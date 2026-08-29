Imports System.Collections.ObjectModel
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input

Namespace Kas

    Partial Public Class RemontEditor
        Inherits UserControl

        Public Event Cancelled As EventHandler

        Private _daNaLok As ObservableCollection(Of Remont)
        Private _majorRemont As Remont
        Private _to2Remont As Remont

        ' Куда назначать выбранное значение
        Private _targetTextBox As TextBox

        Sub New()
            InitializeComponent()
        End Sub

        Private Sub RemontEditor_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
            InitializeRepairTypeCombo()
            InitializeRoadsCombo()
            InitializeFactoriesList()

            ' Заполняем поля ПОСЛЕ инициализации ComboBox
            If _pendingDaNaLok IsNot Nothing Then
                _daNaLok = _pendingDaNaLok
                FillFromExisting()
            End If
        End Sub

        Private _pendingDaNaLok As ObservableCollection(Of Remont)

        Public Sub BindTo(daNaLok As ObservableCollection(Of Remont))
            _pendingDaNaLok = daNaLok
        End Sub

        ' ==================== ИНИЦИАЛИЗАЦИЯ ====================

        Private Sub InitializeRepairTypeCombo()
            Dim repairTypes As String() = {"ПОСТР", "КР", "СР", "ТР-3", "ТР-2", "ТР-1", "ТО-3"}
            For Each rt As String In repairTypes
                CmbRepairType.Items.Add(New ComboBoxItem With {.Content = rt})
            Next
        End Sub

        Private Sub InitializeRoadsCombo()
            For Each road As String In RemontPlacesModule.AllRoads
                CmbRoad.Items.Add(New ComboBoxItem With {.Content = road})
            Next
        End Sub

        Private Sub InitializeFactoriesList()
            For Each factory As String In RemontPlacesModule.AllFactories
                CmbFactories.Items.Add(factory)
            Next
        End Sub

        ' ==================== ЗАПОЛНЕНИЕ ИЗ СУЩЕСТВУЮЩИХ ДАННЫХ ====================

        Private Sub FillFromExisting()
            If _daNaLok Is Nothing OrElse _daNaLok.Count = 0 Then Return

            For Each r As Remont In _daNaLok
                If r.IsTO2 AndAlso _to2Remont Is Nothing Then
                    _to2Remont = r
                ElseIf Not r.IsTO2 AndAlso _majorRemont Is Nothing Then
                    _majorRemont = r
                End If
                If _majorRemont IsNot Nothing AndAlso _to2Remont IsNot Nothing Then Exit For
            Next

            If _majorRemont IsNot Nothing Then
                For Each item As Object In CmbRepairType.Items
                    Dim cmbItem = TryCast(item, ComboBoxItem)
                    If cmbItem IsNot Nothing AndAlso CStr(cmbItem.Content) = _majorRemont.RepairType Then
                        CmbRepairType.SelectedItem = cmbItem
                        Exit For
                    End If
                Next
                If _majorRemont.Mileage.HasValue Then
                    TxtMileage.Text = _majorRemont.Mileage.Value.ToString()
                End If
                TxtMajorPlace.Text = _majorRemont.RepairPlace
            End If

            If _to2Remont IsNot Nothing Then
                TxtTO2Place.Text = _to2Remont.RepairPlace
            End If
        End Sub

        ' ==================== КНОПКИ ▼ (СТРЕЛКА ВНИЗ) ====================

        Private Sub BtnPickMajor_Click(sender As Object, e As RoutedEventArgs)
            _targetTextBox = TxtMajorPlace
            ShowPicker()
        End Sub

        Private Sub BtnPickTO2_Click(sender As Object, e As RoutedEventArgs)
            _targetTextBox = TxtTO2Place
            ShowPicker()
        End Sub

        Private Sub ShowPicker()
            PickerBorder.Visibility = Visibility.Visible
            RbSLD.IsChecked = True
            ' Принудительно обновляем видимость панелей
            PnlSLD.Visibility = Visibility.Visible
            PnlFactories.Visibility = Visibility.Collapsed
            BtnApplyPick.IsEnabled = False
        End Sub

        Private Sub HidePicker()
            PickerBorder.Visibility = Visibility.Collapsed
            _targetTextBox = Nothing
        End Sub

        ' ==================== ПЕРЕКЛЮЧЕНИЕ СЛД / ЗАВОДЫ ====================

        Private Sub RbCategory_Changed(sender As Object, e As RoutedEventArgs)
            If PnlSLD Is Nothing OrElse PnlFactories Is Nothing Then Return

            If RbSLD IsNot Nothing AndAlso RbSLD.IsChecked = True Then
                PnlSLD.Visibility = Visibility.Visible
                PnlFactories.Visibility = Visibility.Collapsed
            ElseIf RbFactories IsNot Nothing AndAlso RbFactories.IsChecked = True Then
                PnlSLD.Visibility = Visibility.Collapsed
                PnlFactories.Visibility = Visibility.Visible
            End If

            If BtnApplyPick IsNot Nothing Then
                BtnApplyPick.IsEnabled = False
            End If
        End Sub

        ' ==================== ВЫБОР ДОРОГИ → ЗАГРУЗКА СЛД ====================
        Private Sub CmbRoad_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            CmbSLD.Items.Clear()
            BtnApplyPick.IsEnabled = False

            If CmbRoad.SelectedItem Is Nothing Then Return

            Dim cmbItem = TryCast(CmbRoad.SelectedItem, ComboBoxItem)
            If cmbItem Is Nothing Then Return

            Dim roadName As String = CStr(cmbItem.Content)

            If RemontPlacesModule.RoadToSLD.ContainsKey(roadName) Then
                For Each sld As String In RemontPlacesModule.RoadToSLD(roadName)
                    CmbSLD.Items.Add(sld)
                Next
            End If
        End Sub

        ' ==================== ВЫБОР В СПИСКАХ ====================

        Private Sub CmbFactories_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            BtnApplyPick.IsEnabled = (CmbFactories.SelectedItem IsNot Nothing)
        End Sub


        ' ==================== ПРИМЕНИТЬ ВЫБОР ====================
        Private Sub BtnApplyPick_Click(sender As Object, e As RoutedEventArgs)
            Dim selectedValue As String = Nothing

            If PnlSLD.Visibility = Visibility.Visible Then
                If CmbSLD.SelectedItem IsNot Nothing Then
                    selectedValue = CmbSLD.SelectedItem.ToString()
                End If
            Else
                If CmbFactories.SelectedItem IsNot Nothing Then
                    selectedValue = CmbFactories.SelectedItem.ToString()
                End If
            End If

            If Not String.IsNullOrEmpty(selectedValue) Then
                ApplySelection(selectedValue)
            End If
        End Sub

        Private Sub ApplySelection(value As String)
            If _targetTextBox IsNot Nothing Then
                _targetTextBox.Text = value
            End If
            HidePicker()
        End Sub

        Private Sub BtnClosePicker_Click(sender As Object, e As RoutedEventArgs)
            HidePicker()
        End Sub


        Private Function GetSelectedRepairType() As String
            If CmbRepairType.SelectedItem Is Nothing Then Return String.Empty
            Dim cmbItem = TryCast(CmbRepairType.SelectedItem, ComboBoxItem)
            If cmbItem Is Nothing Then Return String.Empty
            Return CStr(cmbItem.Content)
        End Function

        Private Sub BtnOk_Click(sender As Object, e As RoutedEventArgs)
            If _daNaLok Is Nothing Then Return

            Dim repairType As String = GetSelectedRepairType()

            ' --- Большой ремонт ---
            If Not String.IsNullOrWhiteSpace(repairType) Then
                If _majorRemont Is Nothing Then
                    _majorRemont = New Remont()
                    _daNaLok.Add(_majorRemont)
                End If

                _majorRemont.RepairType = repairType

                Dim mileageText As String = TxtMileage.Text.Trim()
                If Not String.IsNullOrEmpty(mileageText) Then
                    Dim val As Integer
                    If Integer.TryParse(mileageText, val) AndAlso val > 0 Then
                        _majorRemont.Mileage = val
                    Else
                        _majorRemont.Mileage = Nothing
                    End If
                Else
                    _majorRemont.Mileage = Nothing
                End If

                _majorRemont.RepairPlace = TxtMajorPlace.Text.Trim()
            Else
                If _majorRemont IsNot Nothing Then
                    _daNaLok.Remove(_majorRemont)
                    _majorRemont = Nothing
                End If
            End If

            ' --- ТО-2 ---
            Dim to2Place As String = TxtTO2Place.Text.Trim()
            If Not String.IsNullOrWhiteSpace(to2Place) Then
                If _to2Remont Is Nothing Then
                    _to2Remont = New Remont()
                    _to2Remont.RepairType = "ТО-2"
                    _to2Remont.Mileage = Nothing
                    _daNaLok.Add(_to2Remont)
                End If
                _to2Remont.RepairPlace = to2Place
            Else
                If _to2Remont IsNot Nothing Then
                    _daNaLok.Remove(_to2Remont)
                    _to2Remont = Nothing
                End If
            End If

            ClosePopup()
        End Sub

        Private Sub BtnCancel_Click(sender As Object, e As RoutedEventArgs)
            RaiseEvent Cancelled(Me, EventArgs.Empty)
        End Sub

        Private Sub ClosePopup()
            Dim parentPopup As Primitives.Popup = TryCast(Me.Parent, Primitives.Popup)
            If parentPopup IsNot Nothing Then
                parentPopup.IsOpen = False
            End If
        End Sub

        ' ==================== ОБРАБОТЧИКИ TEXTBOX ====================

        Private Sub TextBox_SelectAll_OnPreviewMouseLeftButtonDown(sender As Object, e As MouseButtonEventArgs)
            Dim tb = TryCast(sender, TextBox)
            If tb IsNot Nothing AndAlso Not tb.IsKeyboardFocused Then
                tb.SelectAll()
                tb.Focus()
                e.Handled = True
            End If
        End Sub

        Private Sub TextBox_GotKeyboardFocus(sender As Object, e As KeyboardFocusChangedEventArgs)
            Dim tb = TryCast(sender, TextBox)
            If tb IsNot Nothing Then
                tb.SelectAll()
            End If
        End Sub

        Private Sub CmbSLD_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            BtnApplyPick.IsEnabled = (CmbSLD.SelectedItem IsNot Nothing)
        End Sub


    End Class

End Namespace