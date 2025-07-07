using AutoMapper;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;
using System.Text.Json;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// Stage mapping configuration
    /// </summary>
    public class StageMapProfile : Profile
    {
        public StageMapProfile()
        {
            // Entity to OutputDto mapping
            CreateMap<Stage, StageOutputDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.TenantId, opt => opt.MapFrom(src => src.TenantId))
                .ForMember(dest => dest.WorkflowId, opt => opt.MapFrom(src => src.WorkflowId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.PortalName, opt => opt.MapFrom(src => src.PortalName))
                .ForMember(dest => dest.InternalName, opt => opt.MapFrom(src => src.InternalName))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.DefaultAssignedGroup, opt => opt.MapFrom(src => src.DefaultAssignedGroup))
                .ForMember(dest => dest.DefaultAssignee, opt => opt.MapFrom(src => src.DefaultAssignee))
                .ForMember(dest => dest.EstimatedDuration, opt => opt.MapFrom(src => src.EstimatedDuration))
                .ForMember(dest => dest.Order, opt => opt.MapFrom(src => src.Order))
                .ForMember(dest => dest.ChecklistId, opt => opt.MapFrom(src => src.ChecklistId))
                .ForMember(dest => dest.QuestionnaireId, opt => opt.MapFrom(src => src.QuestionnaireId))
                .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.Color))
                .ForMember(dest => dest.RequiredFieldsJson, opt => opt.MapFrom(src => src.RequiredFieldsJson))
                .ForMember(dest => dest.StaticFields, opt => opt.MapFrom(src => src.StaticFields))
                .ForMember(dest => dest.WorkflowVersion, opt => opt.MapFrom(src => src.WorkflowVersion))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.IsValid, opt => opt.MapFrom(src => src.IsValid))
                .ForMember(dest => dest.CreateDate, opt => opt.MapFrom(src => src.CreateDate))
                .ForMember(dest => dest.ModifyDate, opt => opt.MapFrom(src => src.ModifyDate))
                .ForMember(dest => dest.CreateBy, opt => opt.MapFrom(src => src.CreateBy))
                .ForMember(dest => dest.ModifyBy, opt => opt.MapFrom(src => src.ModifyBy))
                .ForMember(dest => dest.CreateUserId, opt => opt.MapFrom(src => src.CreateUserId))
                .ForMember(dest => dest.ModifyUserId, opt => opt.MapFrom(src => src.ModifyUserId));

            // InputDto to Entity mapping
            CreateMap<StageInputDto, Stage>()
                .ForMember(dest => dest.WorkflowId, opt => opt.MapFrom(src => src.WorkflowId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.PortalName, opt => opt.MapFrom(src => src.PortalName))
                .ForMember(dest => dest.InternalName, opt => opt.MapFrom(src => src.InternalName))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.DefaultAssignedGroup, opt => opt.MapFrom(src => src.DefaultAssignedGroup))
                .ForMember(dest => dest.DefaultAssignee, opt => opt.MapFrom(src => src.DefaultAssignee))
                .ForMember(dest => dest.EstimatedDuration, opt => opt.MapFrom(src => src.EstimatedDuration))
                .ForMember(dest => dest.Order, opt => opt.MapFrom(src => src.Order))
                .ForMember(dest => dest.ChecklistId, opt => opt.MapFrom(src => src.ChecklistId))
                .ForMember(dest => dest.QuestionnaireId, opt => opt.MapFrom(src => src.QuestionnaireId))
                .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.Color))
                .ForMember(dest => dest.RequiredFieldsJson, opt => opt.MapFrom(src => src.RequiredFieldsJson))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                // Ignore fields that will be set by extension methods or business logic
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.WorkflowVersion, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore())
                .ForMember(dest => dest.StaticFieldsJson, opt => opt.Ignore())
                .ForMember(dest => dest.StaticFields, opt => opt.Ignore());
        }

        private static List<string> ParseStaticFields(string staticFieldsJson)
        {
            if (string.IsNullOrEmpty(staticFieldsJson))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(staticFieldsJson) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
 
