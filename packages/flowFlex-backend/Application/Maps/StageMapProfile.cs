using AutoMapper;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;
using FlowFlex.Domain.Shared.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using System;

namespace FlowFlex.Application.Maps
{
    /// <summary>
    /// Stage mapping configuration
    /// </summary>
    public class StageMapProfile : Profile
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            PropertyNameCaseInsensitive = true
        };
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
                .ForMember(dest => dest.DefaultAssignee, opt => opt.MapFrom(src => ParseDefaultAssignee(src.DefaultAssignee)))
                .ForMember(dest => dest.EstimatedDuration, opt => opt.MapFrom(src => src.EstimatedDuration))
                .ForMember(dest => dest.Order, opt => opt.MapFrom(src => src.Order))
                .ForMember(dest => dest.ChecklistId, opt => opt.MapFrom(src => src.ChecklistId))
                .ForMember(dest => dest.QuestionnaireId, opt => opt.MapFrom(src => src.QuestionnaireId))
                .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.Color))
                .ForMember(dest => dest.ComponentsJson, opt => opt.MapFrom(src => NormalizeComponentsJson(src.ComponentsJson)))
                .ForMember(dest => dest.Components, opt => opt.MapFrom(src => ParseComponents(src.ComponentsJson)))
                .ForMember(dest => dest.VisibleInPortal, opt => opt.MapFrom(src => src.VisibleInPortal))
                .ForMember(dest => dest.PortalPermission, opt => opt.MapFrom(src => src.PortalPermission))
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
                .ForMember(dest => dest.DefaultAssignee, opt => opt.MapFrom(src => SerializeDefaultAssignee(src.DefaultAssignee)))
                .ForMember(dest => dest.EstimatedDuration, opt => opt.MapFrom(src => src.EstimatedDuration))
                .ForMember(dest => dest.Order, opt => opt.MapFrom(src => src.Order))
                .ForMember(dest => dest.ChecklistId, opt => opt.MapFrom(src => src.ChecklistId))
                .ForMember(dest => dest.QuestionnaireId, opt => opt.MapFrom(src => src.QuestionnaireId))
                .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.Color))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.VisibleInPortal, opt => opt.MapFrom(src => src.VisibleInPortal))
                .ForMember(dest => dest.PortalPermission, opt => opt.MapFrom(src => src.PortalPermission))
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
                var parsedComponents = JsonSerializer.Deserialize<List<StageComponent>>(normalized, _jsonOptions);
                return parsedComponents ?? new List<StageComponent>();
            }
            catch
            {
                return new List<StageComponent>();
            }
        }

        private static string TryUnwrapDoubleEncodedJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return json;
            }

            string current = json.Trim();

            // Attempt up to 3 unwrapping passes to handle legacy double/triple encoded values
            for (int i = 0; i < 3; i++)
            {
                if (string.IsNullOrWhiteSpace(current))
                {
                    return current;
                }

                // If it's already a JSON array/object, stop
                if (current.StartsWith("[") || current.StartsWith("{"))
                {
                    break;
                }

                // If it is a quoted JSON string, unwrap by deserializing as string
                if ((current.StartsWith("\"") && current.EndsWith("\"")) ||
                    (current.StartsWith("\'") && current.EndsWith("\'")))
                {
                    try
                    {
                        var inner = JsonSerializer.Deserialize<string>(current);
                        if (!string.IsNullOrWhiteSpace(inner))
                        {
                            current = inner.Trim();
                            continue;
                        }
                    }
                    catch
                    {
                        // fallthrough and try unescape approach below
                    }
                }

                // Heuristic: if we still see a lot of escaped quotes (\") then unescape once
                if (current.Contains("\\\""))
                {
                    try
                    {
                        current = current.Replace("\\\"", "\"");
                        current = current.Trim();
                        continue;
                    }
                    catch
                    {
                        // ignore and break
                    }
                }

                // Nothing changed, stop
                break;
            }

            return current;
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

        /// <summary>
        /// Serialize assignee list to JSONB format for database storage
        /// </summary>
        private static string SerializeDefaultAssignee(List<string> assigneeList)
        {
            if (assigneeList == null || !assigneeList.Any())
                return null;

            try
            {
                return JsonSerializer.Serialize(assigneeList, _jsonOptions);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Parse default assignee from JSONB field
        /// SqlSugar may provide JSONB data in different formats, so we need to handle various cases
        /// </summary>
        private static List<string> ParseDefaultAssignee(string assigneeData)
        {
            if (string.IsNullOrWhiteSpace(assigneeData))
                return new List<string>();

            // Debug: log the raw data to understand what SqlSugar is providing
            System.Diagnostics.Debug.WriteLine($"ParseDefaultAssignee received: {assigneeData}");

            try
            {
                // First, try direct JSON deserialization
                var result = JsonSerializer.Deserialize<List<string>>(assigneeData, _jsonOptions);
                return result ?? new List<string>();
            }
            catch (JsonException)
            {
                try
                {
                    // Maybe it's a nested JSON string? Try unescaping first
                    if (assigneeData.StartsWith("\"") && assigneeData.EndsWith("\""))
                    {
                        var unescaped = JsonSerializer.Deserialize<string>(assigneeData, _jsonOptions);
                        if (!string.IsNullOrWhiteSpace(unescaped))
                        {
                            var nestedResult = JsonSerializer.Deserialize<List<string>>(unescaped, _jsonOptions);
                            return nestedResult ?? new List<string>();
                        }
                    }
                }
                catch
                {
                    // Continue to fallback
                }

                // Fallback for backward compatibility with comma-separated format
                if (!assigneeData.TrimStart().StartsWith("["))
                {
                    return assigneeData.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(s => s.Trim())
                                      .Where(s => !string.IsNullOrWhiteSpace(s))
                                      .ToList();
                }
                
                return new List<string>();
            }
        }

        /// <summary>
        /// Convert List of assignee IDs to JSON string for database storage (legacy method)
        /// </summary>
        private static string ConvertAssigneeListToString(List<string> assigneeList)
        {
            if (assigneeList == null || !assigneeList.Any())
                return null;

            try
            {
                // Use JSON serialization to preserve all data without truncation
                return JsonSerializer.Serialize(assigneeList, _jsonOptions);
            }
            catch (Exception)
            {
                // Fallback to comma-separated format if JSON serialization fails
                return string.Join(",", assigneeList);
            }
        }

        /// <summary>
        /// Convert JSON string or comma-separated string to List of assignee IDs (legacy method)
        /// </summary>
        private static List<string> ConvertAssigneeStringToList(string assigneeString)
        {
            if (string.IsNullOrWhiteSpace(assigneeString))
                return new List<string>();

            // Try to deserialize as JSON first
            if (assigneeString.TrimStart().StartsWith("["))
            {
                try
                {
                    var jsonResult = JsonSerializer.Deserialize<List<string>>(assigneeString, _jsonOptions);
                    return jsonResult ?? new List<string>();
                }
                catch (JsonException)
                {
                    // If JSON parsing fails, fall back to comma-separated parsing
                }
            }

            // Fallback to comma-separated format for backward compatibility
            return assigneeString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                .ToList();
        }
    }
}

