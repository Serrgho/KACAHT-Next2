Imports System.Globalization
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media

Namespace Kas

    Partial Public Class S24Table8

        Private _cur As List(Of Otkaz)
        Private _periodEnd As Date?
        Private Shared ReadOnly Ru As CultureInfo = CultureInfo.GetCultureInfo("ru-RU")

        Public Sub New(currentYearOtkazy As List(Of Otkaz),
                       Optional periodStart As Date? = Nothing,
                       Optional periodEnd As Date? = Nothing)
            InitializeComponent()
            _periodEnd = periodEnd

            ' Фильтрация: учтенные отказы 1 и 2 категории за период
            If periodStart.HasValue AndAlso periodEnd.HasValue Then
                Dim nextDayAfterEnd = periodEnd.Value.Date.AddDays(1)
                _cur = currentYearOtkazy.Where(Function(o)
                                                   Return (o.Kat = 1 OrElse o.Kat = 2) AndAlso
                                                          o.Uchet AndAlso
                                                          o.Nach.Date >= periodStart.Value.Date AndAlso
                                                          o.Nach.Date < nextDayAfterEnd
                                               End Function).ToList()
            Else
                _cur = currentYearOtkazy.Where(Function(o) (o.Kat = 1 OrElse o.Kat = 2) AndAlso o.Uchet).ToList()
            End If
        End Sub

        Private Sub S24Table8_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
            Try
                If _periodEnd.HasValue Then
                    Dim monthName = Ru.DateTimeFormat.GetMonthName(_periodEnd.Value.Month)
                    LblCaption.Text = $"Корректировки ОТС 1,2 категории за {monthName} {_periodEnd.Value.Year}"
                End If

                BuildAndFillRows()
            Catch ex As Exception
                MessageBox.Show(ex.ToString(), "Ошибка в S24Table8")
            End Try
        End Sub

        Private Sub BuildAndFillRows()
            ' ВАЖНО: Добавляем RowDefinition для ШАПКИ (строка 0)
            MainGrid.RowDefinitions.Add(New RowDefinition With {.Height = GridLength.Auto})

            ' Считаем общие суммы по всему списку (без разбивки по депо)
            Dim cat3 = CountPlans("в 3 категорию")
            Dim tn = CountPlans("В технологию")
            Dim otherRoad = CountPlans("На др дорогу")
            Dim sum = cat3 + tn + otherRoad

            ' Строка данных (индекс 1)
            MainGrid.RowDefinitions.Add(New RowDefinition With {.Height = GridLength.Auto})
            Dim dataRowIdx As Integer = MainGrid.RowDefinitions.Count - 1

            'AddCell(dataRowIdx, 0, "", "CellBorder", "CellStyle") ' Пустая ячейка под "Депо"
            AddCell(dataRowIdx, 0, If(cat3 = 0, "", cat3.ToString()), "CellBorder", "CellStyle")
            AddCell(dataRowIdx, 1, If(tn = 0, "", tn.ToString()), "CellBorder", "CellStyle")
            AddCell(dataRowIdx, 2, If(otherRoad = 0, "", otherRoad.ToString()), "CellBorder", "CellStyle")
            AddCell(dataRowIdx, 3, If(sum = 0, "", sum.ToString()), "CellBorder", "CellStyle")

        End Sub

        ''' <summary>
        ''' Считает количество отказов с конкретным типом плана во ВСЕМ списке
        ''' </summary>
        Private Function CountPlans(planDescription As String) As Integer
            Dim targetDesc As String = planDescription.Trim().ToLower()

            Return _cur.Where(Function(o) CheckPlanEntry(o, targetDesc)).Count()
        End Function

        ''' <summary>
        ''' Проверяет наличие конкретного описания плана (вынесено из лямбды)
        ''' </summary>
        Private Function CheckPlanEntry(o As Otkaz, targetDesc As String) As Boolean
            If o.Plan Is Nothing OrElse o.Plan.Count = 0 Then Return False

            For Each entry In o.Plan
                If entry.Description IsNot Nothing AndAlso
           entry.Description.Trim().ToLower() = targetDesc Then
                    Return True
                End If
            Next

            Return False
        End Function

        Private Sub AddCell(row As Integer, col As Integer, text As String,
                            borderStyleKey As String, textStyleKey As String,
                            Optional isBold As Boolean = False)
            Dim b As New Border()
            b.Style = CType(FindResource(borderStyleKey), Style)

            Dim t As New TextBlock()
            t.Style = CType(FindResource(textStyleKey), Style)
            t.Text = text
            If isBold Then t.FontWeight = FontWeights.Bold

            b.Child = t
            Grid.SetRow(b, row)
            Grid.SetColumn(b, col)
            MainGrid.Children.Add(b)
        End Sub

        Private Sub S24Table8_Unloaded(sender As Object, e As RoutedEventArgs) Handles Me.Unloaded
            _cur = Nothing


            ' ВЫЗЫВАЕМ ТВОЙ УНИВЕРСАЛЬНЫЙ ОТПИСЧИК
            UnsubscribeAllEvents(Me)



            ' ОТПИСЫВАЕМСЯ ОТ Unloaded (ЧТОБЫ НЕ БЫЛО ЦИКЛИЧЕСКИХ ССЫЛОК)
            RemoveHandler Me.Unloaded, AddressOf S24Table8_Unloaded

            GC.Collect()
        End Sub
    End Class

End Namespace
