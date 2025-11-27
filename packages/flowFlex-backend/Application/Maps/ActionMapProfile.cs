using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Domain.Entities.Action;
using FlowFlex.Domain.Shared.Enums.Action;
using FlowFlex.Domain.Shared.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// Action AutoMapper Profile
    /// </summary>
    public class ActionMapProfile : Profile
    {
        public ActionMapProfile()
        {
            // ActionDefinition mappings
            CreateMap<ActionDefinition, ActionDefinitionDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.ActionName))
                .ForMember(dest => dest.ActionType, opt => opt.MapFrom(src => Enum.Parse<ActionTypeEnum>(src.ActionType)))
                .ForMember(dest => dest.ActionConfig, opt => opt.MapFrom(src => JsonConvert.SerializeObject(src.ActionConfig, Formatting.None)))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreateDate.DateTime))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.ModifyDate.DateTime))
                .ForMember(dest => dest.IntegrationId, opt => opt.MapFrom(src => src.IntegrationId))
                .ForMember(dest => dest.DataDirection, opt => opt.MapFrom(src => new DataDirectionDto 
                { 
                    Inbound = src.DataDirectionInbound, 
                    Outbound = src.DataDirectionOutbound 
                }))
                .ForMember(dest => dest.IntegrationName, opt => opt.Ignore())
                .ForMember(dest => dest.FieldMappings, opt => opt.Ignore());

            CreateMap<CreateActionDefinitionDto, ActionDefinition>()
                .ForMember(dest => dest.ActionName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.ActionType, opt => opt.MapFrom(src => src.ActionType.ToString()))
                .ForMember(dest => dest.ActionConfig, opt => opt.MapFrom(src => JToken.Parse(src.ActionConfig)))
                .ForMember(dest => dest.IsValid, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreateDate, opt => opt.MapFrom(src => DateTimeOffset.UtcNow))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => DateTimeOffset.UtcNow))
                .ForMember(dest => dest.IntegrationId, opt => opt.MapFrom(src => src.IntegrationId))
                .ForMember(dest => dest.DataDirectionInbound, opt => opt.MapFrom(src => src.DataDirectionInbound))
                .ForMember(dest => dest.DataDirectionOutbound, opt => opt.MapFrom(src => src.DataDirectionOutbound));

            CreateMap<UpdateActionDefinitionDto, ActionDefinition>()
                .ForMember(dest => dest.ActionName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.ActionType, opt => opt.MapFrom(src => src.ActionType.ToString()))
                .ForMember(dest => dest.ActionConfig, opt => opt.MapFrom(src => JToken.Parse(src.ActionConfig)))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => DateTimeOffset.UtcNow))
                .ForMember(dest => dest.IntegrationId, opt => opt.MapFrom(src => src.IntegrationId))
                .ForMember(dest => dest.DataDirectionInbound, opt => opt.MapFrom(src => src.DataDirectionInbound))
                .ForMember(dest => dest.DataDirectionOutbound, opt => opt.MapFrom(src => src.DataDirectionOutbound));

            // ActionTriggerMapping mappings
            CreateMap<ActionTriggerMapping, ActionTriggerMappingDto>()
                .ForMember(dest => dest.TriggerConditions, opt => opt.MapFrom(src => JsonConvert.SerializeObject(src.TriggerConditions, Formatting.None)))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreateDate.DateTime))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.ModifyDate.DateTime));

            CreateMap<CreateActionTriggerMappingDto, ActionTriggerMapping>()
                .ForMember(dest => dest.TriggerConditions, opt => opt.MapFrom(src => string.IsNullOrEmpty(src.TriggerConditions) ? new JObject() : JToken.Parse(src.TriggerConditions)))
                .ForMember(dest => dest.MappingConfig, opt => opt.MapFrom(src => new JObject()))
                .ForMember(dest => dest.IsValid, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreateDate, opt => opt.MapFrom(src => DateTimeOffset.UtcNow))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => DateTimeOffset.UtcNow));

            // ActionTriggerMappingWithDetails to ActionTriggerMappingInfo mapping
            CreateMap<ActionTriggerMappingWithDetails, ActionTriggerMappingInfo>();

            // ActionTriggerMappingWithActionDetails to ActionTriggerMappingWithActionInfo mapping
            CreateMap<ActionTriggerMappingWithActionDetails, ActionTriggerMappingWithActionInfo>();

            // Action execution with action info mapping
            CreateMap<ActionExecutionWithActionInfo, ActionExecutionWithActionInfoDto>();
        }
    }
}