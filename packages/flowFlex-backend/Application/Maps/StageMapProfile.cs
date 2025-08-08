using AutoMapper;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;
using FlowFlex.Domain.Shared.Models;
using System.Text.Json;
using System.Linq;

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
                .ForMember(dest => dest.ComponentsJson, opt => opt.MapFrom(src => NormalizeComponentsJson(src.ComponentsJson)))
                .ForMember(dest => dest.Components, opt => opt.MapFrom(src => ParseComponents(src.ComponentsJson)))
                .ForMember(dest => dest.VisibleInPortal, opt => opt.MapFrom(src => src.VisibleInPortal))
                .ForMember(dest => dest.AttachmentManagementNeeded, opt => opt.MapFrom(src => src.AttachmentManagementNeeded))
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
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.VisibleInPortal, opt => opt.MapFrom(src => src.VisibleInPortal))
                .ForMember(dest => dest.AttachmentManagementNeeded, opt => opt.MapFrom(src => src.AttachmentManagementNeeded))
                // Ignore fields that will be set by extension methods or business logic
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore())

                .ForMember(dest => dest.ComponentsJson, opt => opt.MapFrom(src => SerializeComponents(src.Components)))
                .ForMember(dest => dest.Components, opt => opt.MapFrom(src => src.Components));
        }



        private static List<StageComponent> ParseComponents(string componentsJson)
        {
            if (string.IsNullOrWhiteSpace(componentsJson))
            {
                return new List<StageComponent>();
            }

            // Handle possible double-encoded JSON (e.g. "[ { ... } ]")
            var normalized = TryUnwrapDoubleEncodedJson(componentsJson);

            try
            {
                var parsedComponents = JsonSerializer.Deserialize<List<StageComponent>>(normalized);
                return parsedComponents ?? new List<StageComponent>();
            }
            catch
            {
                return new List<StageComponent>();
            }
        }

        private static string TryUnwrapDoubleEncodedJson(string json)
        {
            var trimmed = json.Trim();

            // If it looks like a JSON array/object already, return as-is
            if (trimmed.StartsWith("[") || trimmed.StartsWith("{"))
            {
                return trimmed;
            }

            // If it looks like a quoted JSON string, try to unwrap once
            if ((trimmed.StartsWith("\"") && trimmed.EndsWith("\"")) ||
                (trimmed.StartsWith("\'") && trimmed.EndsWith("\'")))
            {
                try
                {
                    var inner = JsonSerializer.Deserialize<string>(trimmed);
                    if (!string.IsNullOrWhiteSpace(inner))
                    {
                        return inner.Trim();
                    }
                }
                catch
                {
                    // fallthrough
                }
            }

            return trimmed;
        }

        private static string NormalizeComponentsJson(string componentsJson)
        {
            if (string.IsNullOrWhiteSpace(componentsJson))
            {
                return null;
            }
            var normalized = TryUnwrapDoubleEncodedJson(componentsJson);
            return normalized;
        }



        private static string SerializeComponents(List<StageComponent> components)
        {
            if (components == null || !components.Any())
                return null;

            try
            {
                // Ensure all components have proper default values
                foreach (var component in components)
                {
                    component.StaticFields ??= new List<string>();
                    component.ChecklistIds ??= new List<long>();
                    component.QuestionnaireIds ??= new List<long>();
                }

                return JsonSerializer.Serialize(components);
            }
            catch
            {
                return null;
            }
        }
    }
}

