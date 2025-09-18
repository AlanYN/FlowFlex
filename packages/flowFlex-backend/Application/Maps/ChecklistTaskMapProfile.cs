using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.ChecklistTask;
using FlowFlex.Domain.Entities.OW;
using System.Text.Json;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// ChecklistTask AutoMapper Profile
    /// </summary>
    public class ChecklistTaskMapProfile : Profile
    {
        public ChecklistTaskMapProfile()
        {
            // ChecklistTask entity to ChecklistTaskOutputDto
            CreateMap<ChecklistTask, ChecklistTaskOutputDto>()
                .ForMember(dest => dest.OrderIndex, opt => opt.MapFrom(src => src.Order))
                .ForMember(dest => dest.CompletedAt, opt => opt.MapFrom(src => src.CompletedDate))
                .ForMember(dest => dest.Assignee, opt => opt.MapFrom(src => ParseAssigneeJson(src.AssigneeJson)))
                .ForMember(dest => dest.FilesCount, opt => opt.Ignore()) // Will be set in service layer
                .ForMember(dest => dest.NotesCount, opt => opt.Ignore()); // Will be set in service layer

            // ChecklistTaskInputDto to ChecklistTask entity
            CreateMap<ChecklistTaskInputDto, ChecklistTask>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Order, opt => opt.MapFrom(src => src.OrderIndex)) // Map OrderIndex to Order field
                .ForMember(dest => dest.CompletedDate, opt => opt.Ignore())
                .ForMember(dest => dest.CompletionNotes, opt => opt.Ignore())
                .ForMember(dest => dest.ActualHours, opt => opt.Ignore())
                .ForMember(dest => dest.IsCompleted, opt => opt.Ignore())
                // Status field will be mapped automatically now since it exists in InputDto
                .ForMember(dest => dest.AssigneeJson, opt => opt.MapFrom(src => SerializeAssignee(src.Assignee)))
                // Map Action-related fields to support clearing them
                .ForMember(dest => dest.ActionId, opt => opt.MapFrom(src => src.ActionId))
                .ForMember(dest => dest.ActionName, opt => opt.MapFrom(src => src.ActionName))
                .ForMember(dest => dest.ActionMappingId, opt => opt.MapFrom(src => src.ActionMappingId))
                .ForMember(dest => dest.TenantId, opt => opt.Ignore())
                .ForMember(dest => dest.IsValid, opt => opt.Ignore())
                .ForMember(dest => dest.CreateDate, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyDate, opt => opt.Ignore())
                .ForMember(dest => dest.CreateBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreateUserId, opt => opt.Ignore())
                .ForMember(dest => dest.ModifyUserId, opt => opt.Ignore());
        }

        /// <summary>
        /// Parse AssigneeJson to AssigneeDto
        /// </summary>
        private static AssigneeDto ParseAssigneeJson(string assigneeJson)
        {
            if (string.IsNullOrWhiteSpace(assigneeJson))
                return null;

            try
            {
                // Handle double-encoded JSON strings
                string cleanJson = assigneeJson;

                // If the string starts and ends with triple quotes, it's likely double-encoded
                if (cleanJson.StartsWith("\"\"\"") && cleanJson.EndsWith("\"\"\""))
                {
                    // Remove outer triple quotes and unescape
                    cleanJson = cleanJson.Substring(3, cleanJson.Length - 6);
                    cleanJson = cleanJson.Replace("\\\"", "\"");
                }
                // If the string starts and ends with quotes, remove them
                else if (cleanJson.StartsWith("\"") && cleanJson.EndsWith("\""))
                {
                    cleanJson = cleanJson.Substring(1, cleanJson.Length - 2);
                    cleanJson = cleanJson.Replace("\\\"", "\"");
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<AssigneeDto>(cleanJson, options);
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Failed to parse assignee JSON: {assigneeJson}, Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Serialize AssigneeDto to JSON
        /// </summary>
        private static string SerializeAssignee(AssigneeDto assignee)
        {
            if (assignee == null)
                return null;

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                return JsonSerializer.Serialize(assignee, options);
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Failed to serialize assignee: {ex.Message}");
                return null;
            }
        }
    }
}