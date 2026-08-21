Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Globalization
Imports System.Windows.Media
Imports System.Windows.Threading

Namespace Kas
    ' Класс для узла дерева дат
    Public Class DateTreeNode
        Implements INotifyPropertyChanged

        ' Теперь IsChecked - это Boolean, не Nullable.
        Private _isChecked As Boolean? = True
        ' Оставляем остальные поля
        Private _header As String
        Private _dateValue As DateTime?
        Private _children As ObservableCollection(Of DateTreeNode)

        Public Property Header As String
            Get
                Return _header
            End Get
            Set(value As String)
                _header = value
                OnPropertyChanged("Header")
            End Set
        End Property

        Public Property DateValue As DateTime?
            Get
                Return _dateValue
            End Get
            Set(value As DateTime?)
                _dateValue = value
                OnPropertyChanged("DateValue")
            End Set
        End Property

        Public Property Children As ObservableCollection(Of DateTreeNode)
            Get
                Return _children
            End Get
            Set(value As ObservableCollection(Of DateTreeNode))
                _children = value
                OnPropertyChanged("Children")
            End Set
        End Property

        ' Изменяем свойство IsChecked: теперь это просто Boolean.
        Public Property IsChecked As Boolean?
            Get
                Return _isChecked
            End Get
            Set(value As Boolean?)
                ' Сохраняем старое значение для сравнения
                Dim oldChecked = _isChecked
                ' --- ОСНОВНОЕ ИЗМЕНЕНИЕ ---
                ' Если пришло Nothing (Indeterminate) от UI (например, при клике, когда был Indeterminate),
                ' то внутренне устанавливаем его в False.
                Dim newValue As Boolean? = If(value Is Nothing, False, value)
                ' --- КОНЕЦ ОСНОВНОГО ИЗМЕНЕНИЯ ---
                _isChecked = newValue
                OnPropertyChanged("IsChecked")

                ' Если значение изменилось (т.е. было по клику или программно), обновляем дочерние и родительские элементы
                If oldChecked <> newValue Then
                    ' 1. Если у узла есть дочерние элементы, устанавливаем их IsChecked в то же *новое* значение (False или True)
                    If Children IsNot Nothing AndAlso Children.Count > 0 Then
                        For Each child In Children
                            ' Важно: передаем уже обработанное newValue (False или True), а не исходное value
                            child.SetCheckedState(newValue)
                        Next
                    End If

                    ' 2. Обновляем состояние родителя (если он есть), чтобы отразить изменения в детях
                    If Parent IsNot Nothing Then
                        Parent.UpdateCheckState()
                    End If
                End If

            End Set
        End Property

        ' Свойство для связи с родителем
        Public Property Parent As DateTreeNode

        Public Sub New(header As String, Optional dateValue As DateTime? = Nothing)
            Me.Header = header
            Me.DateValue = dateValue
            Me.Children = New ObservableCollection(Of DateTreeNode)()
            IsChecked = False 'как и раньше
        End Sub

        ' Вспомогательный метод для установки состояния дочернего узла без рекурсивного вызова UpdateCheckStateFromChildren
        ' Используется для синхронизации дочерних элементов с родительским при клике на родителя.
        Sub SetCheckedState(state As Boolean?)
            ' Сохраняем старое значение для сравнения
            Dim oldChecked = _isChecked
            ' --- ОСНОВНОЕ ИЗМЕНЕНИЕ ---
            ' Если передано Nothing, устанавливаем False
            Dim newState As Boolean? = If(state Is Nothing, False, state)
            ' --- КОНЕЦ ОСНОВНОГО ИЗМЕНЕНИЯ ---
            _isChecked = newState
            OnPropertyChanged("IsChecked")

            ' Если значение изменилось и у узла есть дочерние элементы, устанавливаем их IsChecked
            If oldChecked <> newState AndAlso Children IsNot Nothing AndAlso Children.Count > 0 Then
                For Each child In Children
                    ' Рекурсивно для детей детей, передаем newState
                    child.SetCheckedState(newState)
                Next
            End If
            ' Обратите внимание: мы НЕ вызываем UpdateCheckState у родителя здесь,
            ' чтобы избежать циклических обновлений при массовом изменении.
        End Sub

        ' Метод для обновления *этого* узла на основе состояния его *дочерних* элементов.
        ' Вызывается из Set (для родителя) или вручную.
        ' Метод для обновления состояния узла на основе его дочерних элементов
        Public Sub UpdateCheckState()
            If Children Is Nothing OrElse Children.Count = 0 Then Return

            Dim checkedCount As Integer = 0
            Dim uncheckedCount As Integer = 0
            Dim indeterminateCount As Integer = 0 ' Считаем и Indeterminate


            For Each child In Children
                If child.IsChecked.HasValue Then
                    If child.IsChecked.Value Then
                        checkedCount += 1
                    Else
                        uncheckedCount += 1
                    End If
                Else
                    indeterminateCount += 1
                End If
            Next

            ' Определяем новое состояние этого узла на основе детей
            ' Если все дети True -> родитель True
            ' Если все дети False -> родитель False
            ' Если есть хоть один Indeterminate или смешанные True/False -> родитель Nothing (Indeterminate)
            Dim newCheckedState As Boolean?
            If checkedCount = Children.Count Then
                newCheckedState = True
            ElseIf uncheckedCount = Children.Count Then
                newCheckedState = False
            Else
                ' Смешанное состояние -> устанавливаем в Nothing (Indeterminate)
                newCheckedState = Nothing
            End If

            ' Устанавливаем новое состояние, если оно изменилось
            If _isChecked <> newCheckedState Then
                _isChecked = newCheckedState
                OnPropertyChanged("IsChecked") ' <-- Уведомляем UI об изменении

                ' Обновляем родителя
                If Parent IsNot Nothing Then
                    Parent.UpdateCheckState()
                End If
            End If
        End Sub

        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Protected Sub OnPropertyChanged(propertyName As String)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
        End Sub
    End Class

    Partial Public Class DateFilterPopup
        Inherits UserControl

        ' Событие для передачи результата фильтрации
        Public Event FilterApplied As EventHandler(Of DateFilterAppliedEventArgs)

        Private _rootNode As DateTreeNode
        Private _allDates As List(Of DateTime)
        Public Property SourcePropertyName As String = ""
        Sub New()
            InitializeComponent()
            InitializeDateTree()
        End Sub

        Private Sub InitializeDateTree()
            '_rootNode = New DateTreeNode("Все даты")
            'DateTreeView.ItemsSource = New ObservableCollection(Of DateTreeNode) From {_rootNode}
        End Sub

        ' Метод для заполнения дерева датами из источника данных
        Public Sub PopulateDates(itemsSource As IEnumerable(Of Otkaz), propertyName As String, Optional previouslySelectedDates As HashSet(Of Date) = Nothing)
            If itemsSource Is Nothing Then Return

            ' Очищаем TreeView и список годов
            DateTreeView.Items.Clear()
            _yearNodes.Clear() ' <-- Очищаем список

            ' Получаем все уникальные даты из указанного свойства
            _allDates = New List(Of DateTime)

            ' Функция для получения даты из объекта Otkaz
            Dim selector As Func(Of Otkaz, DateTime?) = Nothing

            Select Case propertyName
                Case "Nach"
                    selector = Function(o) o.Nach ' If(o.Nach = Date.MinValue, CType(Nothing, DateTime?), CType(o.Nach, DateTime?))
                Case "Postup"
                    selector = Function(o) o.Postup ' If(o.Postup = Date.MinValue, CType(Nothing, DateTime?), CType(o.Postup, DateTime?)) ' Уже Nullable
                Case "VernulsaOTS"
                    selector = Function(o) o.VernulsaOTS
                Case "Zakryt"
                    selector = Function(o) o.Zakryt
                Case "Peredan"
                    selector = Function(o) o.Peredan
                Case "Sozdan"
                    selector = Function(o) o.Sozdan
                'Case "Sozdan"
                '    selector = Function(o) o.PlanListText
                Case "KorDate"
                    selector = Function(o) o.KorDate
                Case "UpdateNotes"
                    selector = Function(o) o.UpdateNotes
                Case Else
                    Return ' Неизвестное свойство
            End Select

            If selector Is Nothing Then Return


            'KorDate
            ' Извлекаем и фильтруем даты
            Dim dateValues = itemsSource.Select(selector).Where(Function(d) d.HasValue And (d.Value > Date.MinValue)).Select(Function(d) d.Value.Date).Distinct().OrderBy(Function(d) d).ToList()


            _allDates.AddRange(dateValues)

            ' Группируем по годам
            Dim groupedByYear = dateValues.GroupBy(Function(d) d.Year).OrderBy(Function(g) g.Key)

            For Each yearGroup In groupedByYear
                ' Заголовок года - просто число
                Dim yearNode As New DateTreeNode(yearGroup.Key.ToString())

                DateTreeView.Items.Add(yearNode)
                _yearNodes.Add(yearNode) ' <-- Сохраняем ссылку

                ' Группируем по месяцам
                Dim groupedByMonth = yearGroup.GroupBy(Function(d) d.Month).OrderBy(Function(g) g.Key)

                For Each monthGroup In groupedByMonth
                    ' Заголовок месяца - название месяца
                    Dim monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(monthGroup.Key)
                    Dim monthNode As New DateTreeNode(monthName)
                    monthNode.Parent = yearNode
                    yearNode.Children.Add(monthNode)

                    ' Добавляем дни
                    For Each currentDate In monthGroup.OrderBy(Function(d) d)
                        ' Заголовок дня - день месяца (цифра)
                        Dim dayNode As New DateTreeNode(currentDate.Day.ToString())
                        dayNode.DateValue = currentDate ' <-- Сохраняем полную дату для фильтрации
                        dayNode.Parent = monthNode
                        monthNode.Children.Add(dayNode)

                        ' --- НОВОЕ: Восстанавливаем состояние IsChecked ---
                        ' Проверяем, была ли дата выбрана ранее
                        If previouslySelectedDates IsNot Nothing AndAlso previouslySelectedDates.Contains(currentDate) Then
                            ' ВАЖНО: Не устанавливаем IsChecked напрямую!
                            ' Это может вызвать цепочку обновлений при инициализации.
                            ' Лучше установить внутреннее значение _isChecked и вызвать OnPropertyChanged.
                            ' Но если IsChecked - это public property, то можно и через неё, но убедимся, что это не вызывает лишних обновлений.
                            ' У тебя в DateTreeNode IsChecked вызывает логику при Set. Лучше использовать SetCheckedState.
                            ' SetCheckedState устанавливит _isChecked и OnPropertyChanged, но НЕ будет вызывать UpdateCheckState у родителя.
                            dayNode.SetCheckedState(True) ' <-- Используем SetCheckedState для инициализации
                        Else
                            ' Оставляем значение по умолчанию (False или какое там было изначально в конструкторе)
                            ' Если в конструкторе DateTreeNode _isChecked = True, то он останется True, если дата не была выбрана.
                            ' Это может быть не тем, что ты хочешь. Лучше в конструкторе DateTreeNode по умолчанию делать False или Nothing (Indeterminate).
                            ' Проверим конструктор: _isChecked As Boolean? = True <-- Это проблема!
                            ' Нужно: _isChecked As Boolean? = False или _isChecked As Boolean? = Nothing
                            ' Но если хочешь, чтобы по умолчанию было False, и только выбранные - True, то оставим как есть и сбросим тут, если не выбрана.
                            ' dayNode.SetCheckedState(False) ' <-- Это установит все в False. НЕ НАДО!
                            ' Правильный подход: если дата НЕ была выбрана, оставляем как есть (например, False, если конструктор ставит False, или True, если ставит True, но это не логично).
                            ' Лучше в конструкторе DateTreeNode сделать _isChecked = False
                            ' Тогда тут не нужно ничего делать для невыбранных дат, только для выбранных.
                            ' Но у тебя _isChecked = True по умолчанию. Это означает, что все даты будут Checked при открытии, если мы не сбросим их.
                            ' Правильное поведение: все даты по умолчанию False, затем выбранные делаются True.
                            ' Значит, нужно изменить конструктор DateTreeNode или явно установить False тут для НЕвыбранных.
                            ' Явно устанавливать False для НЕвыбранных - неэффективно, лучше в конструкторе DateTreeNode сделать False по умолчанию.
                        End If

                        ' --------------------------








                    Next
                Next
            Next

            ' --- НОВОЕ: После создания всей структуры дерева, обновляем состояния родителей ---
            ' Это нужно, чтобы правильно отобразились Indeterminate состояния у месяцев и годов
            ' на основе выбранных дней.
            For Each yearNode In _yearNodes
                yearNode.UpdateCheckState() ' <-- Вызываем для корневого узла года, он рекурсивно обновит своих детей
            Next

            ' --------------------------



            Me.Dispatcher.BeginInvoke(
       DispatcherPriority.Loaded,
       New Action(AddressOf ExpandYearNodes)
   )

        End Sub

        Private Sub ExpandYearNodes()
            ' Проходим по всем корневым элементам (годам), добавленным в DateTreeView.Items
            For Each item In DateTreeView.Items
                ' Получаем контейнер (TreeViewItem), связанный с этим элементом данных (DateTreeNode)
                Dim treeViewItem As TreeViewItem = TryCast(DateTreeView.ItemContainerGenerator.ContainerFromItem(item), TreeViewItem)
                If treeViewItem IsNot Nothing Then
                    ' Устанавливаем IsExpanded в True для узла года
                    treeViewItem.IsExpanded = True
                End If
            Next
        End Sub


        Private Function GetAllTreeViewNodes(nodes As ObservableCollection(Of DateTreeNode)) As List(Of DateTreeNode)
            Dim allNodes As New List(Of DateTreeNode)()

            For Each node As DateTreeNode In nodes ' Перебираем коллекцию узлов
                allNodes.Add(node)

                ' Рекурсивно добавляем дочерние элементы этого узла
                ' Рекурсивный вызов для дочерней ObservableCollection
                allNodes.AddRange(GetAllTreeViewNodes(node.Children))
            Next

            Return allNodes
        End Function

        ' В начале класса DateFilterPopup.xaml.vb
        Private _yearNodes As New List(Of DateTreeNode)()



        Private Sub ApplyButton_Click(sender As Object, e As RoutedEventArgs)
            Dim selectedDates As New List(Of DateTime)

            ' Проходим по всем корневым узлам (годам), которые мы сохранили
            ' и рекурсивно собираем все узлы под ними
            For Each rootNode In _yearNodes
                ' Получаем все узлы из поддерева rootNode
                ' Фильтруем: IsChecked = True и DateValue.HasValue = True (т.е. это листовой узел - день)
                ' Выбираем: DateValue.Value
                ' Добавляем результат в selectedDates
                selectedDates.AddRange(
            GetAllTreeViewNodes(rootNode.Children). ' <-- Передаем ObservableCollection(Of DateTreeNode)
            Where(Function(node) node.IsChecked AndAlso node.DateValue.HasValue).
            Select(Function(node) node.DateValue.Value)
        )
            Next

            ' Вызываем событие с выбранными датами
            Dim args As New DateFilterAppliedEventArgs With {
        .SelectedDates = selectedDates,
        .PropName = Me.SourcePropertyName
    }
            RaiseEvent FilterApplied(Me, args)
        End Sub

        'Private Sub CollectSelectedDates(node As DateTreeNode, dates As List(Of DateTime))
        '    dates.AddRange(DateTreeView.ItemsSource.Cast(Of DateTreeNode).Where(Function(u) u.IsChecked And u.DateValue.HasValue).Select(Function(u) u.DateValue.Value))
        '    ' Проверяем, выбран ли текущий узел (IsChecked = True)
        '    'If node.IsChecked Then
        '    ' Если у узла есть DateValue (т.е. это узел дня), добавляем её
        '    'If node.DateValue.HasValue Then
        '    '        dates.Add(node.DateValue.Value)
        '    '    Else
        '    '        ' Если узел не день (т.е. месяц или год), но он выбран,
        '    '        ' значит, все его дочерние элементы (дни) тоже выбраны.
        '    '        ' Обходим дочерние элементы для извлечения дат.
        '    '        For Each child In node.Children
        '    '            CollectSelectedDates(child, dates)
        '    '        Next
        '    '    End If
        '    ' Не проверяем IsChecked = False, потому что это означает, что узел не выбран.
        '    ' Также больше не проверяем IsChecked Is Nothing, т.к. IsChecked - Boolean.
        '    'End If
        'End Sub

        Private Sub ResetButton_Click(sender As Object, e As RoutedEventArgs)
            _rootNode.SetCheckedState(False) ' Снимаем все выделения, устанавливая IsChecked в False
        End Sub
    End Class

    ' Аргументы события для фильтрации дат
    Public Class DateFilterAppliedEventArgs
        Inherits EventArgs

        Public Property SelectedDates As List(Of DateTime)

        Public Property PropName As String = ""

    End Class
End Namespace
