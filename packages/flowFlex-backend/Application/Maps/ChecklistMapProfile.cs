using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Checklist;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// Checklist AutoMapper Profile
    /// </summary>
    public class ChecklistMapProfile : Profile
    {
        public ChecklistMapProfile()
        {
            // Checklist 映射
            CreateMap<Checklist, ChecklistOutputDto>()
                .ForMember(dest => dest.Tasks, opt => opt.MapFrom(src => src.Tasks))
                .ForMember(dest => dest.WorkflowName, opt => opt.Ignore())
                .ForMember(dest => dest.StageName, opt => opt.Ignore());

            // InputDto -> Entity
            CreateMap<ChecklistInputDto, Checklist>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore())
                .ForMember(dest => dest.Tasks, opt => opt.Ignore());
        }
    }
} 
