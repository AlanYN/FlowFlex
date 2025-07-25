using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Domain.Entities.Action;
using FlowFlex.Domain.Shared.Enums.Action;
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
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.ModifyDate.DateTime));

            CreateMap<CreateActionDefinitionDto, ActionDefinition>()
                .ForMember(dest => dest.ActionName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.ActionType, opt => opt.MapFrom(src => src.ActionType.ToString()))
                .ForMember(dest => dest.ActionConfig, opt => opt.MapFrom(src => JToken.Parse(src.ActionConfig)))
                .ForMember(dest => dest.IsValid, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreateDate, opt => opt.MapFrom(src => DateTimeOffset.UtcNow))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => DateTimeOffset.UtcNow));

            CreateMap<UpdateActionDefinitionDto, ActionDefinition>()
                .ForMember(dest => dest.ActionName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.ActionType, opt => opt.MapFrom(src => src.ActionType.ToString()))
                .ForMember(dest => dest.ActionConfig, opt => opt.MapFrom(src => JToken.Parse(src.ActionConfig)))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => DateTimeOffset.UtcNow));

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
        }
    }
} 