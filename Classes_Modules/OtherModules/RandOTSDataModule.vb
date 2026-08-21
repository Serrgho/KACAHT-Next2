Namespace Kas
    Module RandOTSDataModule

        Function GetRandDate() As Date
            Dim Ran As New Random
            Return DateTime.Now.AddDays(-(Ran.Next(30))).AddHours(-(Ran.Next(23))).AddMinutes(-(Ran.Next(58)))
        End Function

        Sub OtkazVosstanovlen(OTS As Otkaz)
            If IsNothing(OTS) Then Exit Sub
            If OTS.KtoZakryl?.ToLower Like "тр*" Then
                OTS.ZaKem = "тр"
            Else
                OTS.ZaKem = ""
            End If
            OTS.Zakryt = Date.MinValue

        End Sub

        Sub OtkazZakryt(OTS As Otkaz)
            If IsNothing(OTS) Then Exit Sub
            If Not (OTS.KtoZakryl.ToLower Like "тр*") And OTS.ZaKem?.ToLower <> "тч" And Not (OTS.ZaKem?.ToLower Like "*ехнолог*") Then 'OrElse OTS.ZaKem <> "тч" - с ним закрытые за тч пересохраняются
                OtkazSohranen(OTS)
            End If
            OTS.Zakryt = OTS.Nach

        End Sub

        ''' <summary>
        ''' обязательно проверять перед вызовом на Not OTS.KtoZakryl.ToLower Like "тр*" 
        ''' "слд{DepNum(Ran.Next(5))} "ЛокоРемЗавод" "локостройЗавод"
        ''' </summary>
        ''' <param name="OTS"></param>
        Sub OtkazSohranen(OTS As Otkaz)
            If IsNothing(OTS) Then Exit Sub
            OTS.ZaKem = GetZakemSohranen()

        End Sub

        ''' <summary>
        ''' OTS.Peredan = OTS.Nach
        ''' </summary>
        ''' <param name="OTS"></param>
        Sub OtkazPeredan(OTS As Otkaz)
            If IsNothing(OTS) Then Exit Sub
            If OTS.Zakryt > Date.MinValue Then 'Or (OTS.ZaKem <> "" And OTS.ZaKem <> "!" And OTS.ZaKem <> "тр") - вот это точно здесь не надо
                OtkazVosstanovlen(OTS)
            End If
            OTS.Peredan = OTS.Nach

        End Sub

        ''' <summary>
        ''' OTS.Peredan = Date.MinValue
        ''' </summary>
        ''' <param name="OTS"></param>
        Sub OtkazVernulsa(OTS As Otkaz)
            If IsNothing(OTS) Then Exit Sub
            OTS.Peredan = Date.MinValue

        End Sub

        ''' <summary>
        ''' кроме "передан на др дорогу"
        ''' </summary>
        Function GetZakemSohranen() As String
            Dim Ran As New Random
            Dim S As String = ""
            Select Case Ran.Next(5)
                Case 0
                    S = $"слд{DepNum(Ran.Next(5))}"
                Case 1
                    S = "ЛокоРемЗавод"
                Case 2
                    S = "ЛокоСтройЗавод"
                Case 3
                    S = "Прочие"
                Case 4
                    S = "тч9"
            End Select
            Return S
        End Function


        Function GetKtozakryl() As String
            Dim Ran As New Random
            Dim S As String = ""
            Select Case Ran.Next(2)
                Case 0
                    S = $"ТЧЭ-{DepNum(Ran.Next(5))}"
                Case 1
                    S = $"ТРПУ-{TRNum(Ran.Next(5))}"
            End Select
            Return S
        End Function

        Function GetZakem() As String
            Dim Ran As New Random
            Dim S As String = ""
            Select Case Ran.Next(11)
                Case 0
                    S = ""
                Case 1
                    S = "!"
                Case 2
                    S = $"слд{DepNum(Ran.Next(5))}"
                Case 3
                    S = "ЛокоРемЗавод"
                Case 4
                    S = "Др Дорога"
                Case 5
                    S = "Дубликат"
                Case 6
                    S = "Технология"
                Case 7
                    S = "тч"
                Case 8
                    S = "ЛокоСтройЗавод"
                Case 9
                    S = "тч9"
                Case 10
                    S = "Прочие"
            End Select
            Return S
        End Function

        Function GetZakemTR() As String
            Dim Ran As New Random
            Dim S As String = ""
            Select Case Ran.Next(3)
                Case 0
                    S = "Др Дорога"
                Case 1
                    S = "тр"
                Case 2
                    S = "Дубликат"
            End Select
            Return S
        End Function

        Function GenerateNum() As String
            Dim Ran As New Random
            Dim Rez As String = ""
            For i = 1 To 8
                Rez += Ran.Next(1, 8).ToString
            Next
            Return Rez
        End Function

        Function GetIstoch() As String
            Dim Ran As New Random
            Return Ist(Ran.Next(Ist.Count - 1).ToString)
        End Function

        Function GetKat() As Integer
            Dim Ran As New Random
            Return Ran.Next(1, 4)
        End Function

        Function GetPCh(Kat As Integer) As Single
            Dim Ran As New Random
            Select Case Ran.Next(10)
                Case Is = 5 And Kat = 1
                    Return Math.Round(Ran.Next(50) + Ran.NextSingle(), 2)
                Case Else
                    If Kat = 1 Then
                        If Ran.Next(20) = 3 Then
                            Return Math.Round(Ran.Next(6, 35) + Ran.NextSingle(), 2)
                        Else
                            Return Math.Round(Ran.Next(1, 5) + Ran.NextSingle(), 2)
                        End If

                    Else
                        Return Math.Round(Ran.Next(0, 3) + Ran.NextSingle() + 0.25, 2)
                    End If

            End Select

        End Function

        Sub SetOTSParam(OTS As Otkaz, Optional LogProcessing As Boolean = False)
            If IsNothing(OTS) Then Exit Sub
            Dim Ran As New Random
            Dim Stri As New List(Of String)

            ' Лямбда-функция для добавления логов
            Dim AddLog = Sub(message As String) If LogProcessing Then Stri.Add(message)

            With OTS
                '=============================
                If .Nach > Date.MinValue Then
                    If Ran.Next(10) = 5 Then
                        .Nach = GetRandDate() 'иначе ДатаНачала остается старой
                    End If
                Else
                    .Nach = GetRandDate()
                End If
                .Id = GenerateNum()
                .Kat = GetKat()
                .PCh = GetPCh(.Kat)
                .Istochnik = GetIstoch()
                '======================================
                OtkazVernulsa(OTS)
                AddLog($"Отказ вернулся")
                OtkazVosstanovlen(OTS)
                AddLog($"Отказ восстановлен")
                AddLog($"-----")

                '======================================
                .KtoZakryl = GetKtozakryl()
                '======================================
                If .KtoZakryl.ToLower Like "тр*" Then
                    .ZaKem = GetZakemTR()
                    AddLog($"потому что тр и { .ZaKem}")
                Else
                    .ZaKem = GetZakem()
                    If .Uslovie2 Then
                        OtkazSohranen(OTS)
                        AddLog($"OtkazSohranen потому что { .ZaKem}")
                    End If
                End If
                'может быть и тр и тч за др дорогой
                If .ZaKem?.ToLower Like "*орог*" Then
                    OtkazPeredan(OTS)
                    AddLog($"OtkazPeredan потому что выпало { .ZaKem}")
                End If
                '======================================
                If .Uslovie1() Then
                    OtkazZakryt(OTS)
                    AddLog($"OtkazZakryt потому что .Uslovie1 и { .ZaKem}")
                Else
                    If .ZaKem = "тр" Then
                        If Ran.Next(5) = 2 Then
                            OtkazZakryt(OTS)
                            AddLog($"OtkazZakryt просто случайно")
                        End If
                    ElseIf .ZaKem = "" Or .ZaKem = "!" Then
                        Select Case Ran.Next(5)
                            Case 1
                                OtkazZakryt(OTS)
                                AddLog($"OtkazZakryt случайно, был в расследовании")
                            Case 2
                                OtkazSohranen(PointedOtkaz)
                                AddLog($"OtkazSohranen случайно, был в расследовании")
                            Case Else
                        End Select
                    End If
                End If
            End With
            If LogProcessing Then
                SendAllToInfoLB(Stri)
            End If
        End Sub

        Sub SendAllToInfoLB(Stri As List(Of String))
            Dim OTSClone As New Otkaz
            OTSClone = CloneHelper.CloneOtkaz(PointedOtkaz)
            OTSClone.Pointed = False
            InformLB.AddItem(OTSClone)

            For Each str As String In Stri
                InformLB.AddItem(str)
            Next
            InformLB.AddItem(InformLB.EndOfMSG) '$"============ {DateTime.Now.ToString("HH:mm:ss.fff")}")
            InformLB.ScrollToEnd()
        End Sub

        Sub RollPointedOTS()
            If IsNothing(PointedOtkaz) Then
                Exit Sub
            End If
            Dim NC As Date = PointedOtkaz.Nach
            SetOTSParam(PointedOtkaz, True)
            MW.AddOTSToContainer()

            'проверяем, содержится ли в списке PointedOtkaz
            If NC <> PointedOtkaz.Nach Then
                MW.TRowsContainer.ScrollToPointedOTS()
            End If
        End Sub


    End Module
End Namespace

