using AutoMapper;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Application.Contracts.Dtos.OW.StageVersion;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// StageVersion 映射配置
    /// </summary>
    public class StageVersionMapProfile : Profile
    {
        public StageVersionMapProfile()
        {
            // Entity -> OutputDto
            CreateMap<StageVersion, StageVersionOutputDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.WorkflowVersionId, opt => opt.MapFrom(src => src.WorkflowVersionId))
                .ForMember(dest => dest.OriginalStageId, opt => opt.MapFrom(src => src.OriginalStageId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.DefaultAssignedGroup, opt => opt.MapFrom(src => src.DefaultAssignedGroup))
                .ForMember(dest => dest.DefaultAssignee, opt => opt.MapFrom(src => src.DefaultAssignee))
                .ForMember(dest => dest.EstimatedDuration, opt => opt.MapFrom(src => src.EstimatedDuration))
                .ForMember(dest => dest.OrderIndex, opt => opt.MapFrom(src => src.OrderIndex))
                .ForMember(dest => dest.ChecklistId, opt => opt.MapFrom(src => src.ChecklistId))
                .ForMember(dest => dest.QuestionnaireId, opt => opt.MapFrom(src => src.QuestionnaireId))
                .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.Color))
                .ForMember(dest => dest.WorkflowVersion, opt => opt.MapFrom(src => src.WorkflowVersion))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.CreateDate, opt => opt.MapFrom(src => src.CreateDate))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => src.ModifyDate))
                .ForMember(dest => dest.CreateBy, opt => opt.MapFrom(src => src.CreateBy))
                .ForMember(dest => dest.ModifyBy, opt => opt.MapFrom(src => src.ModifyBy));

            // InputDto -> Entity
            CreateMap<StageVersionInputDto, StageVersion>()
                .ForMember(dest => dest.WorkflowVersionId, opt => opt.MapFrom(src => src.WorkflowVersionId))
                .ForMember(dest => dest.OriginalStageId, opt => opt.MapFrom(src => src.OriginalStageId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.DefaultAssignedGroup, opt => opt.MapFrom(src => src.DefaultAssignedGroup))
                .ForMember(dest => dest.DefaultAssignee, opt => opt.MapFrom(src => src.DefaultAssignee))
                .ForMember(dest => dest.EstimatedDuration, opt => opt.MapFrom(src => src.EstimatedDuration))
                .ForMember(dest => dest.OrderIndex, opt => opt.MapFrom(src => src.OrderIndex))
                .ForMember(dest => dest.ChecklistId, opt => opt.MapFrom(src => src.ChecklistId))
                .ForMember(dest => dest.QuestionnaireId, opt => opt.MapFrom(src => src.QuestionnaireId))
                .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.Color))
                .ForMember(dest => dest.WorkflowVersion, opt => opt.MapFrom(src => src.WorkflowVersion))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore());
        }
    }
}

