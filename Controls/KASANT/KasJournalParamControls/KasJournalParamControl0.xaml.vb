
Imports System.ComponentModel

Namespace Kas
    Partial Public Class KasJournalParamControl0
        Public Shared ReadOnly Property RailwayCodes As New List(Of RailwayCodeItem) From {
                New RailwayCodeItem("Октябрьская", 1),
                New RailwayCodeItem("Калининградская", 10),
                New RailwayCodeItem("Московская", 17),
                New RailwayCodeItem("Горьковская", 24),
                New RailwayCodeItem("Северная", 28),
                New RailwayCodeItem("Северо-Кавказская", 51),
                New RailwayCodeItem("Юго-Восточная", 58),
                New RailwayCodeItem("Приволжская", 61),
                New RailwayCodeItem("Куйбышевская", 63),
                New RailwayCodeItem("Свердловская", 76),
                New RailwayCodeItem("Южно-Уральская", 80),
                New RailwayCodeItem("Западно-Сибирская", 83),
                New RailwayCodeItem("Красноярская", 88),
                New RailwayCodeItem("Восточно-Сибирская", 92),
                New RailwayCodeItem("Забайкальская", 94),
                New RailwayCodeItem("Дальневосточная", 96)
            }
        Sub New()

            ' Этот вызов является обязательным для конструктора.
            InitializeComponent()

            ' Добавить код инициализации после вызова InitializeComponent().

        End Sub

        Private Sub btnSetPeriodJournal_Click(sender As Object, e As RoutedEventArgs)
            ' Открываем окно
            MW.DContrPop.PlaceTarget = sender
            MW.DContrPop.IsOpen = True
        End Sub

        Private Sub btnReSetPeriodJournal_Click(sender As Object, e As RoutedEventArgs)



            ResetSet()


        End Sub

        Private Sub KasJournalParamControl0_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

            ' Если мы в дизайнере Visual Studio/Blend - выходим, чтобы не ломать отрисовку
            If DesignerProperties.GetIsInDesignMode(Me) Then
                Return
            End If
            ResetSet()
        End Sub

        Sub ResetSet()
            ' Дополнительная защита внутри метода тоже не помешает
            If DesignerProperties.GetIsInDesignMode(Me) Then
                Return
            End If

            If Fetcher Is Nothing Then
                Return
            End If

            With Fetcher
                If Today.Day < 13 Then
                    .NachDat = New Date(Today.Year, Today.Month, 1).AddMonths(-1)
                Else
                    .NachDat = New Date(Today.Year, Today.Month, 1)
                End If

                .KonDat = Today.Date

                ' Проверяем существование MW и вложенных элементов перед обращением
                If MW.JourParam IsNot Nothing AndAlso MW.JourParam.ParamTimeCTL IsNot Nothing Then
                    MW.JourParam.ParamTimeCTL.But23.IsChecked = True
                End If
            End With

            If DorTBlock IsNot Nothing Then
                DorTBlock.SelectedIndex = 12
            End If


            'With Fetcher
            '    If Today.Day < 13 Then
            '        .NachDat = New Date(Today.Year, Today.Month, 1).AddMonths(-1)
            '    Else
            '        .NachDat = New Date(Today.Year, Today.Month, 1)
            '    End If

            '    .KonDat = Today.Date
            '    MW.JourParam.ParamTimeCTL.But23.IsChecked = True
            '    DorTBlock.SelectedIndex = 12

            'End With
        End Sub


    End Class
End Namespace

