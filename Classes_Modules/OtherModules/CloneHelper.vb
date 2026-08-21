Imports AutoMapper
Namespace Kas

    Public Module CloneHelper
        ' Создаем статический экземпляр маппера
        Private ReadOnly Property Mapper As IMapper
            Get
                Static mapperInstance As IMapper = Nothing
                If mapperInstance Is Nothing Then
                    ' Создаем выражение конфигурации отдельно
                    Dim expr As New MapperConfigurationExpression()

                    ' УКАЖИТЕ ВАШ КЛЮЧ ЛИЦЕНЗИИ (требуется для v15+)
                    ' expr.LicenseKey = "ВАШ_КЛЮЧ" 

                    expr.CreateMap(Of Otkaz, Otkaz)()

                    ' Передаем выражение и Nothing вместо ILoggerFactory
                    Dim config As New MapperConfiguration(expr, Nothing)

                    config.AssertConfigurationIsValid()
                    mapperInstance = config.CreateMapper()
                End If
                Return mapperInstance
            End Get
        End Property

        ' Метод для настройки маппера
        Private Sub ConfigureMapper(cfg As IMapperConfigurationExpression)
            cfg.CreateMap(Of Otkaz, Otkaz)()
        End Sub

        ' Метод для клонирования объекта
        Public Function CloneOtkaz(original As Otkaz) As Otkaz
            If original Is Nothing Then
                Throw New ArgumentNullException(NameOf(original), "Original object cannot be null.")
            End If
            Return Mapper.Map(Of Otkaz)(original)
        End Function
    End Module
End Namespace

