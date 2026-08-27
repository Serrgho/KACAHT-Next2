
Namespace Kas
	Partial Public Class PoyasnilkaCTL

        ''' <summary>
        ''' Возвращает внутренний Expander для точного позиционирования попапов
        ''' </summary>
        Public ReadOnly Property InnerExpander As Expander
            Get
                Return expPoyasnilka  ' это имя из XAML: x:Name="expPoyasnilka"
            End Get
        End Property


        Sub New()

			' Этот вызов является обязательным для конструктора.
			InitializeComponent()

			' Добавить код инициализации после вызова InitializeComponent().

		End Sub

		Private Sub SelectPeriod_Click(sender As Object, e As RoutedEventArgs)
            ' Открываем окно/ Определяет цель по имени кнопки
            MW.DContrPop.PlaceTarget = sender
            MW.DContrPop.IsOpen = True
        End Sub

        Private Sub ResetPeriod_Click(sender As Object, e As RoutedEventArgs)
            ResetPeriodSUB()
            ' 2. Обновляем источник данных — показываем ВСЕ записи
            ' Предполагаем, что у вас есть полный список в OTSList
            If OTSList IsNot Nothing Then
                MW.TRowsContainer.ItemsSource = OTSList
            Else
                ' Если OTSList ещё не загружен — можно загрузить из JSON или Excel
                ShowMSG(MW, "Данные не загружены. Сначала выполните импорт.", "Внимание")
                Return
            End If

            '' 3. Обновляем UI
            'InitFirst()                  ' ← ваша глобальная инициализация
            'YarCon.UpdateYarlykInfo()   ' ← обновление ярлыков
        End Sub

        Private Sub BtnExport_Click(sender As Object, e As RoutedEventArgs)

            If My.Settings.OldYJSON = "" Then Exit Sub

            If Fetcher.NachDat > Fetcher.KonDat Then
                MessageBox.Show("Некорректный период")
                Return
            End If

            Dim oldY As List(Of Otkaz) = StorageModule.LoadFromJson(My.Settings.OldYJSON)

            ' объединённый текущий год (THISYJSON + OTSList), обрезанный по выбранному периоду
            Dim fullCurY = GetFullCurrentYearOtkazy(OTSList, oldY, Fetcher.NachDat, Fetcher.KonDat)
            'Dim fullCurY = GetFullCurrentYearOtkazy(OTSList, Fetcher.NachDat, Fetcher.KonDat)

            ' период прошлого года с тем же сдвигом, что и в таблицах
            Dim oldP = GetPrevPeriod(Fetcher.NachDat, Fetcher.KonDat, oldY)

            ' готовые списки с учетом Uchet
            Dim NY = GetUchetOtkazy(fullCurY, Fetcher.NachDat, Fetcher.KonDat)
            Dim NY1 = GetUchetOtkazy(oldY, oldP.Start, oldP.End)

            ExportFullReport(NY, NY1,
                             Fetcher.NachDat, Fetcher.KonDat,
                             oldP.Start, oldP.End)

        End Sub



        Private Sub btnS24Show_Click(sender As Object, e As RoutedEventArgs)
            If My.Settings.OldYJSON = "" Then Exit Sub
            Dim oldY As List(Of Otkaz) = StorageModule.LoadFromJson(My.Settings.OldYJSON)

            Dim nachDateTime = AddTimeToDate(Fetcher.NachDat, Fetcher.NachTim)
            Dim konDateTime = AddTimeToDate(Fetcher.KonDat, Fetcher.KonTim, Fetcher.KonMinut)

            Dim fullCurY = GetFullCurrentYearOtkazy(OTSList, oldY, nachDateTime, konDateTime)
            'Dim fullCurY = GetFullCurrentYearOtkazy(OTSList, oldY, Fetcher.NachDat, Fetcher.KonDat)

            ' спросили ОДИН раз
            Dim useOneTable As Boolean = AskOneTable()

            Dim tables1 = BuildTableList(Of S24Table1)(
                Function(st, en) New S24Table1(fullCurY, oldY, st, en),
                useOneTable)

            Dim tables3 = BuildTableList(Of S24Table3)(
                Function(st, en) New S24Table3(fullCurY, oldY, st, en),
                useOneTable)

            Dim tables4 = BuildTableList(Of S24Table4)(
                Function(st, en) New S24Table4(fullCurY, st, en),
                useOneTable)

            ' ▼▼▼ ВОТ ТУТ, в кнопке: табл.1 своей строкой, табл.3+табл.4 парой в один ряд ▼▼▼
            Dim all As New List(Of FrameworkElement)
            Dim maxN = Math.Max(tables1.Count, Math.Max(tables3.Count, tables4.Count))

            For i = 0 To maxN - 1
                If i < tables1.Count Then all.Add(tables1(i))

                Dim pair As New StackPanel With {
                    .Orientation = Orientation.Horizontal,
                    .VerticalAlignment = VerticalAlignment.Top,
                    .Margin = New Thickness(0, 10, 0, 0)
                }
                If i < tables3.Count Then pair.Children.Add(tables3(i))
                If i < tables4.Count Then
                    tables4(i).Margin = New Thickness(20, 0, 0, 0)
                    pair.Children.Add(tables4(i))
                End If
                If pair.Children.Count > 0 Then all.Add(pair)
            Next

            ShowInWindow(all, $"Справка {Fetcher.NachDat.ToString("yy")}")
        End Sub

        Private Sub btnS24T2Show_Click(sender As Object, e As RoutedEventArgs)
            If My.Settings.OldYJSON = "" Then Exit Sub
            Dim oldY As List(Of Otkaz) = StorageModule.LoadFromJson(My.Settings.OldYJSON)
            Dim nachDateTime = AddTimeToDate(Fetcher.NachDat, Fetcher.NachTim)
            Dim konDateTime = AddTimeToDate(Fetcher.KonDat, Fetcher.KonTim, Fetcher.KonMinut)
            ' Для основных таблиц: фильтруем по периоду
            Dim fullCurY = GetFullCurrentYearOtkazy(OTSList, oldY, nachDateTime, konDateTime)
            ' Для "с начала года": берём всё без фильтрации
            Dim ytdOtkazy = GetYearToDateOtkazy(OTSList, oldY)

            ' ▼▼▼ спросили один раз ▼▼▼
            Dim useOneTable As Boolean = AskOneTable()

            Dim tables = BuildTableList(Of S24Table2)(
                Function(st, en) New S24Table2(fullCurY, oldY, st, en, ytdOtkazy),
                useOneTable)

            ' ▼▼▼ вот тут: перекладываем в List(Of FrameworkElement) ▼▼▼
            Dim all As New List(Of FrameworkElement)
            all.AddRange(tables)

            ShowInWindow(all, $"Отказы и часы {Fetcher.NachDat.ToString("yy")}")
        End Sub



        Private Function AskOneTable() As Boolean
            Dim differentMonths As Boolean =
        Fetcher.NachDat.Year <> Fetcher.KonDat.Year OrElse
        Fetcher.NachDat.Month <> Fetcher.KonDat.Month

            ' месяцы одинаковые — и спрашивать нечего
            If Not differentMonths Then Return True

            Return ShowMSG(
        owner:=MW,
        $"Начало и конец периода находятся в разных месяцах{vbCrLf}Показать одну таблицу за весь период [OK]{vbCrLf}Разбить период на месяцы [ОТМЕНА]",
        "Справка", MsgButtons.OKCancel)
        End Function


        Private Function BuildTableList(Of T As FrameworkElement)(create As Func(Of Date, Date, T), useOneTable As Boolean) As List(Of T)


            Dim result As New List(Of T)

            If useOneTable Then
                result.Add(create(Fetcher.NachDat, Fetcher.KonDat))
            Else
                For Each p In GetMonthPeriods()
                    result.Add(create(p.Start, p.End))
                Next
            End If

            Return result
        End Function


        Private Function GetMonthPeriods() As List(Of (Start As Date, [End] As Date))
            Dim result As New List(Of (Start As Date, [End] As Date))

            Dim curStart As Date = Fetcher.NachDat

            While curStart <= Fetcher.KonDat
                Dim curEnd As Date = New Date(
            curStart.Year,
            curStart.Month,
            DateTime.DaysInMonth(curStart.Year, curStart.Month)
        )

                If curEnd > Fetcher.KonDat Then curEnd = Fetcher.KonDat

                result.Add((curStart, curEnd))
                curStart = curEnd.AddDays(1)
            End While

            Return result
        End Function


        Private Sub ShowInWindow(controls As List(Of FrameworkElement), title As String)
            Dim panel As New StackPanel() With {
        .HorizontalAlignment = HorizontalAlignment.Stretch
    }

            For Each c In controls
                c.Margin = New Thickness(2, 15, 2, 15)
                panel.Children.Add(c)
            Next

            Dim scroller As New ScrollViewer() With {
            .Margin = New Thickness(15),
            .Content = panel,
        .VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        .HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
    }

            ' ▼▼▼ заставляем внешний скроллер реагировать на колесо ЛЮБОЙ точкой мыши ▼▼▼
            AddHandler scroller.PreviewMouseWheel, Sub(s As Object, e As MouseWheelEventArgs)
                                                       Dim sv = DirectCast(s, ScrollViewer)
                                                       sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta / 3)
                                                       e.Handled = True
                                                   End Sub
            ' ▲▲▲ ▲▲▲


            Dim win As New Window() With {
            .Owner = MW,
        .Title = title,
        .WindowStartupLocation = WindowStartupLocation.CenterScreen,
        .SizeToContent = SizeToContent.WidthAndHeight,
        .MaxWidth = SystemParameters.PrimaryScreenWidth,
        .MaxHeight = SystemParameters.PrimaryScreenHeight / 2
    }

            win.Content = scroller
            win.Show()
        End Sub

        Private Async Sub btnDailyShow_Click(sender As Object, e As RoutedEventArgs)
            btnDailyShow.IsEnabled = False
            'Dim MyOtsList As List(Of Otkaz) = OTSList
            Try
                Await Task.Run(Sub() GenerateAllReports(OTSList.OrderBy(Function(u) u.Nach).ToList))
            Finally
                btnDailyShow.IsEnabled = True
            End Try
        End Sub

        Private Sub btnDailyPoyasnShow_Click(sender As Object, e As RoutedEventArgs)
            'Dim ExclusivePeriod As Func(Of Otkaz, Boolean) = (Function(o) o.Nach.IsInRangeWithTime AndAlso Not (o.KtoZakryl.ToLower.Contains("трп")))
            'RezervList = OTSList
            'OTSList = OTSList.Where(ExclusivePeriod).OrderBy(Function(o) o.Nach).ToList
            'ResetPeriodSUB()
            'InitFirst()
            'YarCon.UpdateYarlykInfo()
        End Sub
    End Class
End Namespace


