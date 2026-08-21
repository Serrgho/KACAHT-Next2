Imports System.Collections.Generic
Imports System.IO
Namespace Kas

    Module EquipmentHierarchyModule

        Private Function CreateDefaultHierarchy() As Dictionary(Of String, List(Of String))
            Return New Dictionary(Of String, List(Of String)) From {
                        {"Цепи управления", New List(Of String) From {"автоматический выключатель", "Аккумулятор", "БВ", "Блокировочное устройство", "Блокировочный переключатель", "вентиль", "Вентилятор охлаждения ТЭД", "Выключатель", "ГВ", "датчик напряжения", "Датчик тока", "дроссель", "Изолятор", "клапан продувки", "Кнопка", "Кондиционер", "Контактор", "Контроллер машиниста", "ограничитель перенапряжений", "панель реле", "Переключатель", "Переключатель вентиляторов", "предохранитель", "преобразователь напряжения", "Провода", "Разъединитель высоковольтный", "сглаживающий реактор", "трансформатор", "Тумблер", "Тяговый трансформатор", "шина", "шунт", "ЭКГ", "Электропневматический клапан", "ТРПШ (зарядный агрегат)", "Токовый трансформатор", "датчик давления", "датчик скорости (ДПС)", "панель фильтров", "Штепсель", "Электрическая печь", "ВБО", "Нагреватель электрический", "Пневматический выключатель управления", "Главный контроллер", "Клапан продувки", "Электрический калорифер", "Статический преобразователь напряжения ЭДТ", "Пуско-тормозной резистор", "Переключатель тормозной", "Переходной реактор", "Электроблокировочный клапан", "Вентилятор охлаждения ВИП", "Измерительные приборы", "Вакуумный выключатель", "Реверсивный переключатель", "Выключатель управления", "Электромагнитный вентиль защиты", "Электромагнитный вентиль выключающий", "Переключатель вспомогательных приводов", "Электромагнитный вентиль броневого типа", "Индуктивный шунт", "Преобразователь собственных нужд", "Переключатель кулачковый двухпозиционный", "Переключатель режимов", "Переключатель вспомогательных машин", "Разрядник высоковольтный", "Тиристорная сборка", "Переключатель кулачковый групповой", "Переключатель мотор-вентиляторов", "Переключатель печей"}},
                        {"Тормозное оборудование", New List(Of String) From {"Воздухораспределитель", "Главный резервуар", "датчик обрыва ТМ", "Клапан песочницы", "Компрессор", "Концевой кран", "Кран вспомогательного тормоза", "кран двойной тяги", "Кран машиниста", "Масловлагоотделитель", "обратный клапан", "пневматическая блокировка", "предохранительный клапан", "Регулятор давления", "Резервуар цепи управления", "соединительные рукава", "Тормозная магистраль", "тормозной цилиндр", "Уравнительный резервуар", "Запасный резервуар", "Сигнализатор отпуска тормозов", "Переключательный клапан", "Клапан сигнала", "Фильтр", "Концевой кран", "Сигнализаторы обрыва тормозной магистрали", "Разобщительный кран", "Блок управления ЭПТ", "Трехходовой кран", "Срывной клапан", "Форсунка песочницы", "Песочная труба", "Воздухопровод тормозной магистрали", "Тифон", "Стояночный пружинный тормоз", "Авторежим", "Устройство блокировки тормозов", "Вспомогательный компрессор"}},
                        {"Механическое оборудование", New List(Of String) From {"автосцепка", "букса", "колесная пара", "Корпус кузова", "Люлечное подвешивание", "МОП", "Подвеска ТЭД", "противоразгрузочное устройство", "пружина", "путеочиститель", "Рама тележки", "рессора", "ТРП", "Тяговый редуктор", "Тяговое устройство", "Лабиринтные жалюзи", "Упругая муфта", "Кронштейн гасителя колебаний", "Опора кузова локомотива", "колодки"}},
                        {"Электронное оборудование", New List(Of String) From {"блок защиты", "Блок пуска дизеля", "блок стабилизации питания", "блок управления тягой", "БРН", "ВИП", "Датчик боксования", "МСУД", "УСТА", "Устройство импульсной подачи песка", "форсунка пескоподачи", "электронный регулятор", "Электронная система управления тягой", "Гребнесмазыватель", "Панель управления", "Система автоматизированного управления рекуперативным торможением", "Блок автоматического управления ВИП", "пожарная сигнализация", "ИСАВП-РТ"}},
                        {"Аппараты защиты", New List(Of String) From {"Реле боксования", "Реле времени", "Реле давления", "Реле дизеля", "Реле дифференциальной защиты", "Реле заземления", "Реле напряжения", "Реле перегрузки", "Реле промежуточное", "Реле тепловое", "Реле тока", "Реле максимального тока", "Реле управления", "Реле температуры", "Реле контроля земли", "Реле термозащитное", "реле максимального напряжения", "Реле контроля напряжения", "Реле электрического торможения", "Реле давления масла", "Блок-реле электропневматического тормоза", "Реле защиты от юза", "Реле давления воздуха", "реле переключения"}},
                        {"ТЭД", New List(Of String) From {"выводная коробка", "Главные полюса", "Дополнительные полюса", "Коллектор", "Компенсационная обмотка", "МЯП", "обмотка возбуждения", "остов", "Щеточный аппарат", "Якорь", "Подшипниковый щит", "Корпус тягового агрегата", "Уплотнение"}},
                        {"Токоприёмник", New List(Of String) From {"воздушный рукав", "каретка", "клапан", "пневматический привод", "полоз", "рама", "тяга", "угольная вставка", "Электромагнитный вентиль", "Резервуар", "Вставка токоприёмника", "Редуктор", "Пружина", "Изолятор", "Синхронизирующая тяга"}},
                        {"Вспомогательные машины", New List(Of String) From {"двухмашинный агрегат", "МВ", "МК", "МН", "сельсин", "тяговый генератор", "ФР", "ПД", "Электронасос", "Возбудитель синхронный"}},
                        {"Приборы безопасности", New List(Of String) From {"АЛСН", "КЛУБ", "КОН", "КПД", "Радиосвязь", "ЭПК", "САУТ", "Скоростемер", "ТСКБМ", "МЛСБ", "Антенна КЛУБ", "УКБМ", "БЛОК", "ГАЛС"}},
                        {"Прочее", New List(Of String) From {"неприём станцией", "взрез стрелки", "рельс", "провод КС", "Вагон", "погодные условия", "экипировка песком", "Рама грузового вагона"}},
                        {"Система подачи топлива", New List(Of String) From {"Вентиль", "Турбокомпрессор", "ТНВД", "фильтр", "топливоподогреватель", "Форсунка топливная", "РЧО", "Нагнетатель второй ступени", "Перепускной клапан", "Топливоподкачивающий насос", "Клапаны топливной системы", "Предельный регулятор", "Топливный коллектор", "Трубопровод системы подачи топлива"}},
                        {"Система охлаждения", New List(Of String) From {"Водяной насос охлаждения дизеля", "Валопровод", "вентилятор холодильника", "теплообменник", "главный вентилятор", "Гидромашина", "Воздухоочиститель", "Лабиринтные жалюзи", "Секции холодильника", "Дюритовые рукава", "Трубопровод системы циркуляции воды", "Редуктор вентилятора охлаждения тягового генератора", "карданная муфта", "Гидропривод вентилятора холодильника"}},
                        {"Цилиндро-поршневая группа", New List(Of String) From {"Блок цилиндра", "Поршневые кольца", "шатун", "Втулка цилиндра", "Вал", "Вертикальная передача", "Крышка цилиндра", "Поршень", "Распределительный редуктор", "Промежуточный вал", "Коленчатый вал", "Выпускной коллектор", "Впускной коллектор", "Трубопровод", "Шестерня ГРМ", "Гильза цилиндра дизеля", "Муфта привода генератора дизеля", "Привод клапанов дизеля"}},
                        {"Система подачи масла", New List(Of String) From {"насос", "клапан", "Трубопровод системы подачи масла", "Фильтры масляной системы", "Вентиль системы подачи масла", "Масляный насос редуктора гидронасоса"}}
            }
        End Function

        Public Sub ResetToDefaultAndSave()
            ' Берём чистую копию базового словаря
            _level2ToLevel3 = CreateDefaultHierarchy()
            _isLoaded = True ' помечаем как загруженный

            ' Перезаписываем файл
            SaveHierarchyToFile()
        End Sub



        ' ====== РАБОЧИЙ СЛОВАРЬ (может меняться) ======
        Private _level2ToLevel3 As Dictionary(Of String, List(Of String)) = Nothing
        Private _isLoaded As Boolean = False

        Public ReadOnly Property Level2ToLevel3 As Dictionary(Of String, List(Of String))
            Get
                If _level2ToLevel3 Is Nothing Then
                    _level2ToLevel3 = CreateDefaultHierarchy()
                End If
                Return _level2ToLevel3
            End Get
        End Property





        ' ====== Ур.2 → Ур.1 (из  макроса GetClass) ======
        Public Function GetLevel1ForLevel2(level2 As String) As String
            Select Case level2
                Case "Аппараты защиты", "Цепи управления", "Электронное оборудование", "Токоприёмник"
                    Return "Электрическое оборудование"
                Case "ТЭД", "Вспомогательные машины"
                    Return "Электрические машины"
                Case "Тормозное оборудование"
                    Return "Тормозное оборудование"
                Case "Механическое оборудование"
                    Return "Механическое оборудование"
                Case "Приборы безопасности"
                    Return "Приборы безопасности"
                Case "Прочее"
                    Return "Прочее"
                Case "Система подачи масла", "Система подачи топлива", "Система охлаждения", "Дизельное оборудование", "Цилиндро-поршневая группа", "Вертикальная передача дизеля"
                    Return "Дизельное оборудование"
                Case Else
                    Return ""
            End Select
        End Function

        ' ====== Списки для интерфейса ======
        Public ReadOnly Property AllLevel2 As List(Of String)
            Get
                Return Level2ToLevel3.Keys.OrderBy(Function(x) x).ToList()
            End Get
        End Property

        Public ReadOnly Property AllLevel1 As List(Of String)
            Get
                Return AllLevel2.Select(AddressOf GetLevel1ForLevel2).Where(Function(x) Not String.IsNullOrEmpty(x)).Distinct().OrderBy(Function(x) x).ToList()
            End Get
        End Property

        Public Function GetLevel2ForLevel1(level1 As String) As List(Of String)
            Return AllLevel2.Where(Function(l2) GetLevel1ForLevel2(l2) = level1).OrderBy(Function(x) x).ToList()
        End Function


        Private Function GetHierarchyFilePath(FNam As String) As String
            Dim dataFolder = GetOrCreateDataFolderPath(ForParams:=True)
            If String.IsNullOrEmpty(dataFolder) Then
                Return Nothing
            End If
            Return Path.Combine(dataFolder, FNam)
        End Function

        Public Sub SaveHierarchyToFile()
            Try
                Dim filePath = GetHierarchyFilePath("EquipmentHierarchy.json")
                If String.IsNullOrEmpty(filePath) Then Return

                Dim dir = IO.Path.GetDirectoryName(filePath)
                If Not IO.Directory.Exists(dir) Then IO.Directory.CreateDirectory(dir)

                Dim json = Newtonsoft.Json.JsonConvert.SerializeObject(Level2ToLevel3, Newtonsoft.Json.Formatting.Indented)
                IO.File.WriteAllText(filePath, json, System.Text.Encoding.UTF8)
            Catch ex As Exception
                Debug.WriteLine($"Ошибка сохранения EquipmentHierarchy.json: {ex.Message}")
            End Try
        End Sub

        Public Sub AddLevel3Item(level2 As String, level3 As String)
            'If String.IsNullOrEmpty(level2) OrElse String.IsNullOrEmpty(level3) Then Return
            'If Not Level2ToLevel3.ContainsKey(level2) Then Return
            'Dim list = Level2ToLevel3(level2)
            'If Not list.Contains(level3) Then
            '    list.Add(level3)
            '    list.Sort()
            '    SaveHierarchyToFile()
            'End If

            If String.IsNullOrEmpty(level2) OrElse String.IsNullOrEmpty(level3) Then Return
            If Not Level2ToLevel3.ContainsKey(level2) Then Return
            Dim normalized = level3.Trim()
            If String.IsNullOrEmpty(normalized) Then Return

            Dim list = Level2ToLevel3(level2)
            ' Сравнение без учёта регистра и пробелов
            If Not list.Any(Function(x) String.Equals(x.Trim(), normalized, StringComparison.OrdinalIgnoreCase)) Then
                list.Add(normalized)
                list.Sort(StringComparer.OrdinalIgnoreCase)
                SaveHierarchyToFile()
            End If

        End Sub

        ' ====== ЗАГРУЗКА ПРИ СТАРТЕ ======



        Public Sub EnsureHierarchyLoaded()

            If _isLoaded Then Return
            _isLoaded = True

            Dim filePath = GetHierarchyFilePath("EquipmentHierarchy.json")
            If String.IsNullOrEmpty(filePath) Then Return

            Try
                If Not IO.File.Exists(filePath) Then
                    SaveHierarchyToFile()
                    Return
                End If

                Dim json = IO.File.ReadAllText(filePath, System.Text.Encoding.UTF8)
                Dim loaded = Newtonsoft.Json.JsonConvert.DeserializeObject(Of Dictionary(Of String, List(Of String)))(json)

                ' Обновляем только существующие ключи
                For Each kvp In loaded
                    If Level2ToLevel3.ContainsKey(kvp.Key) Then
                        Level2ToLevel3(kvp.Key).Clear()
                        Level2ToLevel3(kvp.Key).AddRange(kvp.Value.Distinct().OrderBy(Function(x) x).ToList())
                    End If
                Next

            Catch ex As Exception
                Debug.WriteLine($"Ошибка загрузки EquipmentHierarchy.json: {ex.Message}")
                SaveHierarchyToFile() ' восстанавливаем дефолт
            End Try

            'If _isLoaded Then Return
            '_isLoaded = True

            'Dim filePath = GetHierarchyFilePath("EquipmentHierarchy.json")
            'If String.IsNullOrEmpty(filePath) Then Return

            'Try
            '    If Not IO.File.Exists(filePath) Then
            '        SaveHierarchyToFile()
            '        Return
            '    End If

            '    Dim json = IO.File.ReadAllText(filePath, System.Text.Encoding.UTF8)
            '    Dim loaded = Newtonsoft.Json.JsonConvert.DeserializeObject(Of Dictionary(Of String, List(Of String)))(json)

            '    ' Полная замена содержимого — только для ключей, которые есть в базовом словаре
            '    For Each kvp In loaded
            '        If Level2ToLevel3.ContainsKey(kvp.Key) Then
            '            Level2ToLevel3(kvp.Key).Clear()
            '            Level2ToLevel3(kvp.Key).AddRange(kvp.Value.Distinct().OrderBy(Function(x) x).ToList())
            '        End If
            '    Next

            'Catch ex As Exception
            '    Debug.WriteLine($"Ошибка загрузки EquipmentHierarchy.json: {ex.Message}")
            '    SaveHierarchyToFile() ' восстанавливаем дефолт
            'End Try





        End Sub

        'Public Sub LoadBase()
        '    Dim filePath = GetHierarchyFilePath("EquipmentHierarchy.json")
        '    Dim json = IO.File.ReadAllText(filePath, System.Text.Encoding.UTF8)
        '    Dim loaded = Newtonsoft.Json.JsonConvert.DeserializeObject(Of Dictionary(Of String, List(Of String)))(json)

        '    ' Полная замена содержимого — без слияния!
        '    For Each kvp In loaded
        '        If Level2ToLevel3.ContainsKey(kvp.Key) Then
        '            Level2ToLevel3(kvp.Key).Clear()
        '            Level2ToLevel3(kvp.Key).AddRange(kvp.Value)
        '        End If
        '    Next


        'End Sub

        Public Sub RemoveLevel3Item(level2 As String, level3 As String)
            If String.IsNullOrEmpty(level2) OrElse String.IsNullOrEmpty(level3) Then Return
            If Not Level2ToLevel3.ContainsKey(level2) Then Return

            Dim list = Level2ToLevel3(level2)
            If list.Contains(level3) Then
                list.Remove(level3)
                SaveHierarchyToFile()
            End If
        End Sub


        Public Sub ReloadFromDefault()
            ' Сброс состояния: пересоздаём словарь заново из кода
            _level2ToLevel3 = Nothing
            _isLoaded = False
            EnsureHierarchyLoaded() ' теперь загрузит дефолт и сохранит файл
        End Sub



    End Module

End Namespace