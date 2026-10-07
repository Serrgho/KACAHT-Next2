Imports System.Windows.Media.Effects

Namespace Kas
    Partial Public Class InfoRowCard

        Private _fullText As String = ""
        Private _isBold As Boolean = False

        Public Sub New()
            InitializeComponent()
        End Sub

        ''' <summary>
        ''' Устанавливает данные строки и цвет значения
        ''' </summary>
        Public Sub SetData(key As String, value As String, valueColor As Brush)
            tbKey.Text = key
            tbValue.Text = value
            tbValue.Foreground = If(valueColor, Brushes.Black)
            _fullText = $"{key} {value}"
        End Sub

        ''' <summary>
        ''' При наведении — тень под текстом + жирный шрифт
        ''' </summary>
        Private Sub RootBorder_MouseEnter(sender As Object, e As MouseEventArgs)
            ' Тень под ключом
            tbKey.Effect = New DropShadowEffect With {
                .BlurRadius = 2,
                .ShadowDepth = 2,
                .Direction = 270,
                .Color = Color.FromRgb(125, 125, 125),
                .Opacity = 0.5
            }
            ' Тень под значением
            tbValue.Effect = New DropShadowEffect With {
                .BlurRadius = 2,
                .ShadowDepth = 2,
                .Direction = 270,
                .Color = Color.FromRgb(125, 125, 125),
                .Opacity = 0.5
            }

            '' Жирный шрифт
            'tbKey.FontWeight = FontWeights.Bold
            'tbValue.FontWeight = FontWeights.Bold
            _isBold = True
        End Sub

        ''' <summary>
        ''' Убираем тень и жирность при уходе мыши
        ''' </summary>
        Private Sub RootBorder_MouseLeave(sender As Object, e As MouseEventArgs)
            tbKey.Effect = Nothing
            tbValue.Effect = Nothing

            tbKey.FontWeight = FontWeights.SemiBold
            tbValue.FontWeight = FontWeights.Normal
            _isBold = False
        End Sub

        ''' <summary>
        ''' Копирование в буфер обмена по ЛКМ
        ''' </summary>
        Private Sub RootBorder_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs)
            Try
                Clipboard.SetText(_fullText)

                ' Пытаемся найти родительское окно KasAntWin и установить статус
                Dim win = TryCast(Window.GetWindow(Me), KasAntWin)
                If win IsNot Nothing Then
                    win.SetStatus($"✓ Скопировано: {_fullText}", Brushes.DarkGreen)
                End If
            Catch ex As Exception
                Dim win = TryCast(Window.GetWindow(Me), KasAntWin)
                If win IsNot Nothing Then
                    win.SetStatus($"❌ Ошибка копирования: {ex.Message}", Brushes.Red)
                End If
            End Try
            e.Handled = True
        End Sub

    End Class
End Namespace
