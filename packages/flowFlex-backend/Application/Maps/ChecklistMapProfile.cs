using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Checklist;
using FlowFlex.Application.Contracts.Dtos.OW.Common;
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
            // AssignmentDto 映射 - Domain to Contracts
            CreateMap<FlowFlex.Domain.Entities.OW.AssignmentDto, FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto>();

            // Checklist 映射
            CreateMap<Checklist, ChecklistOutputDto>()
                .ForMember(dest => dest.Assignments, opt => opt.Ignore()); // Assignments handled separately in service

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
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore());
            // Assignments removed from entity - handled through Stage Components
        }
    }
}
