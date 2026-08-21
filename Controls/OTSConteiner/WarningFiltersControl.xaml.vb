
Imports System.Collections.Generic
Imports System.Linq
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input

Namespace Kas
	Partial Public Class WarningFiltersControl
        Inherits UserControl

        Public Shared ReadOnly ItemsSourceProperty As DependencyProperty =
            DependencyProperty.Register("ItemsSource", GetType(IEnumerable(Of Otkaz)), GetType(WarningFiltersControl), New PropertyMetadata(Nothing))

        Public Property ItemsSource As IEnumerable(Of Otkaz)
            Get
                Return GetValue(ItemsSourceProperty)
            End Get
            Set(value As IEnumerable(Of Otkaz))
                SetValue(ItemsSourceProperty, value)
            End Set
        End Property

        Public Event FilterApplied(filteredItems As List(Of Otkaz))

        ' Обработчик клика по бордеру
        Private Sub Border_MouseUp(sender As Object, e As MouseButtonEventArgs)
            Dim bordr As Border = TryCast(sender, Border)
            If bordr Is Nothing Then Return

            Dim source = Me.ItemsSource
            If source Is Nothing Then Return

            Dim filtered As List(Of Otkaz) = Nothing

            Select Case bordr.Name
                Case "EmptyFieldsBorder"
                    filtered = source.Where(Function(o) String.IsNullOrWhiteSpace(o.SerLokExact) OrElse o.MyKlasLev3 = "").ToList()
                Case "VidTyagiBorder"
                    filtered = source.Where(Function(o) o.VidT = "").ToList()
                Case "IsVioletsBorder"
                    filtered = source.Where(Function(o) o.IsViolet).ToList()
                Case "IsRedsMashBrdr"
                    filtered = source.Where(Function(o) o.MashPripTXT_IsRed AndAlso Not o.KtoZakryl.ToLower().Contains("трп")).ToList()
                Case "IsEmptyPCHBrdr"
                    filtered = source.Where(Function(o) o.PCh = 0 AndAlso o.Kat < 3).ToList()
            End Select

            If filtered IsNot Nothing Then
                bordr.Visibility = Visibility.Collapsed
                RaiseEvent FilterApplied(filtered)
            End If

            e.Handled = True
        End Sub




        ' ← фиксированный список для проверки незаполненных свойств отказа
        Public RequiredProps = {"PripLok", "SerLokExact", "MyKlasLev3", "IsViolet", "MashPripTXT_IsRed", "PCh", "VidT"}

        ' Метод проверки и обновления видимости бордеров
        Public Sub CheckForEmptyFields()
            ' 1. Безопасно получаем данные
            Dim source = Me.ItemsSource
            Dim items As IEnumerable(Of Otkaz)
            If source Is Nothing Then
                items = Enumerable.Empty(Of Otkaz)()
            Else
                Try
                    items = CType(source, IEnumerable(Of Otkaz))
                Catch
                    items = source.Cast(Of Otkaz)()
                End Try
            End If

            Dim otkazType = GetType(Otkaz)
            Dim parts As New List(Of String)

            ' 2. Проходим по глобальному списку RequiredProps
            For Each propName As String In RequiredProps
                Dim prop = otkazType.GetProperty(propName)
                If prop Is Nothing Then Continue For

                Select Case propName
                    Case "VidT"
                        Dim vidTCount = items.Count(Function(u) u.VidT = "")
                        If vidTCount > 0 Then
                            VidTyagiLabel.Text = $"Не указан вид тяги: {vidTCount}"
                            VidTyagiBorder.Visibility = Visibility.Visible
                        Else
                            VidTyagiBorder.Visibility = Visibility.Collapsed
                        End If

                    Case "IsViolet"
                        Dim violetCount = items.Count(Function(u) CBool(prop.GetValue(u)))
                        If violetCount > 0 Then
                            VioletsLabel.Text = $"Не заполнено описаний: {violetCount}"
                            IsVioletsBorder.Visibility = Visibility.Visible
                        Else
                            IsVioletsBorder.Visibility = Visibility.Collapsed
                        End If

                    Case "MashPripTXT_IsRed"
                        Dim redCount = items.Count(Function(u)
                                                       Dim isRed As Boolean = CBool(prop.GetValue(u))
                                                       Dim kto As String = If(u.KtoZakryl, "").ToLower()
                                                       Return isRed And (Not kto.Contains("трп"))
                                                   End Function)
                        If redCount > 0 Then
                            IsRedsMashTBlk.Text = $"Неверно указан машинист: {redCount}"
                            IsRedsMashBrdr.Visibility = Visibility.Visible
                        Else
                            IsRedsMashBrdr.Visibility = Visibility.Collapsed
                        End If

                    Case "PCh"
                        Dim emptyPch = items.Count(Function(u) u.PCh = 0 AndAlso u.Kat < 3)
                        If emptyPch > 0 Then
                            IsEmptyPCHTBlk.Text = $"Не указаны потери П/Ч: {emptyPch}"
                            IsEmptyPCHBrdr.Visibility = Visibility.Visible
                        Else
                            IsEmptyPCHBrdr.Visibility = Visibility.Collapsed
                        End If

                    Case Else
                        ' Стандартная обработка для остальных свойств
                        Dim emptyCount = items.Count(Function(u)
                                                         Dim value = prop.GetValue(u)
                                                         Return value Is Nothing OrElse value = "---" OrElse
                                                                (TypeOf value Is String AndAlso String.IsNullOrWhiteSpace(CStr(value)))
                                                     End Function)
                        If emptyCount > 0 Then
                            ' GetDisplayName тоже должна быть доступна (в модуле или здесь)
                            parts.Add($"{GetDisplayName(propName)} ({emptyCount})")
                        End If
                End Select
            Next

            ' 3. Управление оранжевым бордером
            Dim isVisible = parts.Count > 0
            If isVisible Then
                EmptyFieldsLabel.Text = $"Не заполнено: {String.Join("; ", parts)}"
                EmptyFieldsBorder.Visibility = Visibility.Visible
            Else
                EmptyFieldsBorder.Visibility = Visibility.Collapsed
                EmptyFieldsLabel.Text = ""
            End If
        End Sub

        Public Sub ShowAllWarnings()
            EmptyFieldsBorder.Visibility = Visibility.Visible
            VidTyagiBorder.Visibility = Visibility.Visible
            IsVioletsBorder.Visibility = Visibility.Visible
            IsRedsMashBrdr.Visibility = Visibility.Visible
            IsEmptyPCHBrdr.Visibility = Visibility.Visible
        End Sub

    End Class
End Namespace


