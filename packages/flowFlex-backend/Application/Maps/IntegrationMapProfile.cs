using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Domain.Entities.Integration;
using System.Text.Json;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// Integration mapping profile
    /// </summary>
    public class IntegrationMapProfile : Profile
    {
        public IntegrationMapProfile()
        {
            // Integration mappings
            CreateMap<Domain.Entities.Integration.Integration, IntegrationOutputDto>()
                .ForMember(dest => dest.ConfiguredEntityTypes, opt => opt.Ignore())
                .ForMember(dest => dest.ConfiguredEntityTypeNames, opt => opt.Ignore());

            CreateMap<IntegrationInputDto, Domain.Entities.Integration.Integration>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.EncryptedCredentials, opt => opt.Ignore())
                .ForMember(dest => dest.ConfiguredEntityTypes, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore())
                .ForMember(dest => dest.EntityMappings, opt => opt.Ignore())
                .ForMember(dest => dest.FieldMappings, opt => opt.Ignore())
                .ForMember(dest => dest.IntegrationActions, opt => opt.Ignore())
                .ForMember(dest => dest.QuickLinks, opt => opt.Ignore())
                .ForMember(dest => dest.SyncLogs, opt => opt.Ignore())
                .ForMember(dest => dest.InboundConfig, opt => opt.Ignore())
                .ForMember(dest => dest.OutboundConfig, opt => opt.Ignore());

            // EntityMapping mappings
            CreateMap<EntityMapping, EntityMappingOutputDto>()
                .ForMember(dest => dest.WorkflowIds, opt => opt.Ignore());

            CreateMap<EntityMappingInputDto, EntityMapping>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.WorkflowIds, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore())
                .ForMember(dest => dest.Integration, opt => opt.Ignore())
                .ForMember(dest => dest.KeyMappings, opt => opt.Ignore())
                .ForMember(dest => dest.FieldMappings, opt => opt.Ignore());

            // FieldMapping mappings (WorkflowIds removed in V2)
            CreateMap<FieldMapping, FieldMappingOutputDto>()
                .ForMember(dest => dest.TransformRules, opt => opt.Ignore())
                .ForMember(dest => dest.WfeFieldName, opt => opt.Ignore())
                .ForMember(dest => dest.IsStaticField, opt => opt.Ignore());

            CreateMap<FieldMappingInputDto, FieldMapping>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TransformRules, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore())
                .ForMember(dest => dest.Integration, opt => opt.Ignore())
                .ForMember(dest => dest.EntityMapping, opt => opt.Ignore());

            // QuickLink mappings
            CreateMap<QuickLink, QuickLinkOutputDto>()
                .ForMember(dest => dest.UrlParameters, opt => opt.Ignore());

            CreateMap<QuickLinkInputDto, QuickLink>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UrlParameters, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore())
                .ForMember(dest => dest.Integration, opt => opt.Ignore());

            // IntegrationSyncLog mappings
            CreateMap<IntegrationSyncLog, IntegrationSyncLogOutputDto>();
            CreateMap<IntegrationSyncLogInputDto, IntegrationSyncLog>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore());

            // ReceiveExternalDataConfig mappings (V2)
            CreateMap<ReceiveExternalDataConfig, ReceiveExternalDataConfigOutputDto>()
                .ForMember(dest => dest.TriggerWorkflowName, opt => opt.Ignore())
                .ForMember(dest => dest.FieldMappings, opt => opt.Ignore());

            CreateMap<ReceiveExternalDataConfigInputDto, ReceiveExternalDataConfig>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.IntegrationId, opt => opt.Ignore())
                .ForMember(dest => dest.FieldMappingConfig, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore())
                .ForMember(dest => dest.Integration, opt => opt.Ignore());
        }

    }
}

