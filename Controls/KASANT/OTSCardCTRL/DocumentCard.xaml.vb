Imports System.ComponentModel

Namespace Kas

    Partial Public Class DocumentCard
        Inherits UserControl

        ' === Зависимые свойства (для связи с окном) ===
        Public Shared ReadOnly FileDataProperty As DependencyProperty =
            DependencyProperty.Register("FileData", GetType(AttachedFile), GetType(DocumentCard), New PropertyMetadata(Nothing, AddressOf OnFileDataChanged))

        Public Shared ReadOnly CounterProperty As DependencyProperty =
            DependencyProperty.Register("Counter", GetType(Integer), GetType(DocumentCard), New PropertyMetadata(1, AddressOf OnCounterChanged))

        Public Shared ReadOnly ViolIdProperty As DependencyProperty =
            DependencyProperty.Register("ViolId", GetType(String), GetType(DocumentCard), New PropertyMetadata(String.Empty))

        Public Shared ReadOnly StatusCallbackProperty As DependencyProperty =
            DependencyProperty.Register("StatusCallback", GetType(Action(Of String, Brush)), GetType(DocumentCard), New PropertyMetadata(Nothing))

        ' === CLR-обёртки ===
        Public Property FileData As AttachedFile
            Get
                Return CType(GetValue(FileDataProperty), AttachedFile)
            End Get
            Set(value As AttachedFile)
                SetValue(FileDataProperty, value)
            End Set
        End Property

        Public Property Counter As Integer
            Get
                Return CInt(GetValue(CounterProperty))
            End Get
            Set(value As Integer)
                SetValue(CounterProperty, value)
            End Set
        End Property

        Public Property ViolId As String
            Get
                Return CStr(GetValue(ViolIdProperty))
            End Get
            Set(value As String)
                SetValue(ViolIdProperty, value)
            End Set
        End Property

        Public Property StatusCallback As Action(Of String, Brush)
            Get
                Return CType(GetValue(StatusCallbackProperty), Action(Of String, Brush))
            End Get
            Set(value As Action(Of String, Brush))
                SetValue(StatusCallbackProperty, value)
            End Set
        End Property

        ' === Конструктор ===
        Public Sub New()
            InitializeComponent()
            ' Вешаем клик на ссылку
            AddHandler FileLink.PreviewMouseLeftButtonUp, AddressOf OnFileLinkClicked
        End Sub

        ' === Реагируем на изменение данных ===
        Private Shared Sub OnFileDataChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim ctrl = DirectCast(d, DocumentCard)
            ctrl.UpdateContent()
        End Sub

        Private Shared Sub OnCounterChanged(d As DependencyObject, e As DependencyPropertyChangedEventArgs)
            Dim ctrl = DirectCast(d, DocumentCard)
            ctrl.UpdateContent()
        End Sub

        ' === Обновление UI (Самое важное) ===
        Private Sub UpdateContent()
            If FileData Is Nothing Then
                Me.Visibility = Visibility.Collapsed
                Return
            End If

            Me.Visibility = Visibility.Visible

            ' 1. Ссылка на файл
            FileLink.Inlines.Clear()
            FileLink.Inlines.Add(New Run($"{Counter}) ") With {.Foreground = Brushes.DarkRed, .FontWeight = FontWeights.Bold})
            ' Защита от Null имени
            Dim safeFileName = If(String.IsNullOrWhiteSpace(FileData.FileName), "[Без имени]", FileData.FileName)
            FileLink.Inlines.Add(New Run(safeFileName) With {.Foreground = Brushes.Blue, .FontWeight = FontWeights.DemiBold})

            ' 2. Кто загрузил
            If Not String.IsNullOrWhiteSpace(FileData.UploadedBy) Then
                UploaderText.Text = $"👤   {FileData.UploadedBy}"
                UploaderText.Visibility = Visibility.Visible
            Else
                UploaderText.Visibility = Visibility.Collapsed
            End If

            ' 3. Комментарий
            If Not String.IsNullOrWhiteSpace(FileData.Comment) Then
                CommentText.Text = $"{FileData.Comment}"
                CommentText.Visibility = Visibility.Visible
            Else
                CommentText.Visibility = Visibility.Collapsed
            End If
            ' 4. Загрузка иконки
            LoadFileIcon(FileData.FileName)
        End Sub

        Private Sub LoadFileIcon(fileName As String)
            Dim ext = System.IO.Path.GetExtension(fileName)?.ToLower()
            Dim iconFile = GetIconResourcePath(ext) ' твоя функция выбора имени файла
            ' === ОДНА СТРОКА, КАК В XAML ===
            FileIcon.Source = New BitmapImage(New Uri($"pack://application:,,,{iconFile}"))
        End Sub

        ' === Путь к ресурсу иконки ===
        Private Function GetIconResourcePath(ext As String) As String

            ' !!! Замени "Kas" на имя своей сборки, если отличается !!!
            Select Case ext
                Case ".pdf" : Return "/Icons/pdf1.png"
                Case ".doc", ".docx", ".rtf" : Return "/Icons/doc.png"
                Case ".xls", ".xlsx", ".csv" : Return "/Icons/xlsfile.png"
                Case ".jpg", ".jpeg", ".png", ".gif", ".bmp" : Return "/Icons/img.png"
                Case ".txt", ".log", ".ini" : Return "/Icons/txtfile.png"
                    'KACAHT Next2 — имя  сборки
                    'Component — ключевое слово WPF для доступа к ресурсам сборки
                    '/Icons/deflt.png — путь к файлу внутри сборки
                Case Else : Return "/KACAHT Next2;component/Icons/deflt.png" ' Дефолт
            End Select
        End Function




        ' === Обработчик клика: Скачивание ===
        Private Async Sub OnFileLinkClicked(sender As Object, e As MouseButtonEventArgs)
            If FileData Is Nothing OrElse String.IsNullOrWhiteSpace(FileData.DownloadUrl) Then Return

            Try
                StatusCallback?.Invoke($"📥 Скачивание {FileData.FileName}...", Brushes.DarkBlue)

                ' Скачиваем байты
                Dim bytes = Await Fetcher.HttpClient.GetByteArrayAsync(FileData.DownloadUrl)

                ' Формируем имя: "Имя (ViolId).ext"
                Dim nameOnly = System.IO.Path.GetFileNameWithoutExtension(FileData.FileName)
                Dim ext = System.IO.Path.GetExtension(FileData.FileName)
                Dim newFileName = $"{nameOnly} ({ViolId}){ext}"
                Dim tempPath = System.IO.Path.Combine(My.Settings.DocFolder, newFileName)

                ' Проверка дубликатов
                If System.IO.File.Exists(tempPath) Then
                    newFileName = $"{nameOnly} ({ViolId})_{Guid.NewGuid().ToString().Substring(0, 4)}{ext}"
                    tempPath = System.IO.Path.Combine(My.Settings.DocFolder, newFileName)
                End If

                ' Сохраняем
                System.IO.File.WriteAllBytes(tempPath, bytes)

                ' Открываем
                Process.Start(New ProcessStartInfo(tempPath) With {.UseShellExecute = True})

                StatusCallback?.Invoke($"✅ Открыто: {newFileName}", Brushes.DarkGreen)

            Catch ex As Exception
                StatusCallback?.Invoke($"❌ Ошибка: {ex.Message}", Brushes.Red)
            End Try

            e.Handled = True
        End Sub
    End Class
End Namespace

