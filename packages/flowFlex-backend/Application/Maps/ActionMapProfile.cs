using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Domain.Entities.Action;

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
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreateDate.DateTime))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.ModifyDate.DateTime));

            CreateMap<CreateActionDefinitionDto, ActionDefinition>()
                .ForMember(dest => dest.ActionName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.IsValid, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreateDate, opt => opt.MapFrom(src => DateTimeOffset.UtcNow))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => DateTimeOffset.UtcNow));

            CreateMap<UpdateActionDefinitionDto, ActionDefinition>()
                .ForMember(dest => dest.ActionName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => DateTimeOffset.UtcNow));

            // ActionTriggerMapping mappings
            CreateMap<ActionTriggerMapping, ActionTriggerMappingDto>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreateDate.DateTime))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.ModifyDate.DateTime));

            CreateMap<CreateActionTriggerMappingDto, ActionTriggerMapping>()
                .ForMember(dest => dest.IsValid, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreateDate, opt => opt.MapFrom(src => DateTimeOffset.UtcNow))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => DateTimeOffset.UtcNow));
        }
    }
} 