
Imports System.IO

Namespace Kas
    Partial Public Class UstanowkiCTL

        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().

        End Sub

        Private Sub BtnOldYeFileSet_Click(sender As Object, e As RoutedEventArgs)
            Dim Pth As String = GetJSONFilePath()

            If Pth <> "" Then
                My.Settings.OldYJSON = Pth
                My.Settings.Save()
            End If
        End Sub

        Private Sub BtnThisYeFileSet_Click(sender As Object, e As RoutedEventArgs)
            Dim Pth As String = GetJSONFilePath()

            If Pth <> "" Then
                My.Settings.THISYJSON = Pth
                My.Settings.Save()
            End If
        End Sub



        Private Function GetJSONFilePath() As String

            ' 1. Создаём диалог выбора файла
            Dim openFileDialog As New Microsoft.Win32.OpenFileDialog With {
                .Filter = "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*",
                .Title = "Выберите JSON с данными прошлого года",
                .Multiselect = False
            }

            ' 2. Устанавливаем начальную папку — из настроек (если есть)
            Dim defaultPath As String = StorageModule.GetDataFolderPath()
            If Directory.Exists(defaultPath) Then
                openFileDialog.InitialDirectory = defaultPath
            Else
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            End If

            ' 3. Открываем диалог
            Dim result As Boolean? = openFileDialog.ShowDialog()
            If result <> True Then Return "" ' пользователь нажал "Отмена"

            Return openFileDialog.FileName
        End Function

        Private Sub BtnResetSettings_Click(sender As Object, e As RoutedEventArgs)
            If ShowMSG(MW, "Сбросить пути к папкам??", "ВНИМАНИЕ!", MsgButtons.OKCancel) Then
                With My.Settings
                    .DocFolder = ""
                    .ReportFolderPath = ""
                    .DataFolderPath = ""
                    .SetsFolder = ""
                    .Save()
                End With
            End If
        End Sub

        Private Sub BtnGoalsEditor_Click(sender As Object, e As RoutedEventArgs)
            Dim editor As New GoalsEditor()
            editor.Owner = MW
            editor.Show()
        End Sub

		Private Sub BtnGoalsHoursEditor_Click(sender As Object, e As RoutedEventArgs)
            Dim w As New Window()
            w.Title = "Целевые по часам"
            w.Owner = MW
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner
            w.SizeToContent = SizeToContent.WidthAndHeight
            w.Background = CType(FindResource("AppBackBrush"), Brush)
            w.Content = New ScrollViewer With {
        .Content = New GoalHoursEditor(),
        .HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        .VerticalScrollBarVisibility = ScrollBarVisibility.Auto
    }
            w.Show()
        End Sub

        Private Sub BtnGoalsOtherRailsEditor_Click(sender As Object, e As RoutedEventArgs)
            Dim w As New Window()
            w.Title = "Целевые по дорогам"
            w.Owner = MW
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner
            w.SizeToContent = SizeToContent.WidthAndHeight
            w.Background = CType(FindResource("AppBackBrush"), Brush)
            w.Content = New RoadGoalsEditor()
            w.Show()
        End Sub
    End Class
End Namespace

