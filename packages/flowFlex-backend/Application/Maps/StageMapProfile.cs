using AutoMapper;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
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
        
        // Static logger for mapping profile - initialized via SetLogger method
        private static ILogger _logger;
        
        /// <summary>
        /// Set the logger instance for this profile (call during application startup)
        /// </summary>
        public static void SetLogger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory?.CreateLogger<StageMapProfile>();
        }
        
        private static void LogWarning(Exception ex, string message)
        {
            if (_logger != null)
            {
                _logger.LogWarning(ex, message);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[StageMapProfile] {message}: {ex.Message}");
            }
        }
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
                .ForMember(dest => dest.CoAssignees, opt => opt.MapFrom(src => ParseDefaultAssignee(src.CoAssignees)))
                .ForMember(dest => dest.EstimatedDuration, opt => opt.MapFrom(src => src.EstimatedDuration))
                .ForMember(dest => dest.Order, opt => opt.MapFrom(src => src.Order))
                .ForMember(dest => dest.ChecklistId, opt => opt.MapFrom(src => src.ChecklistId))
                .ForMember(dest => dest.QuestionnaireId, opt => opt.MapFrom(src => src.QuestionnaireId))
                .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.Color))
                .ForMember(dest => dest.ComponentsJson, opt => opt.MapFrom(src => NormalizeComponentsJson(src.ComponentsJson)))
                .ForMember(dest => dest.Components, opt => opt.MapFrom(src => ParseComponents(src.ComponentsJson)))
                .ForMember(dest => dest.VisibleInPortal, opt => opt.MapFrom(src => src.VisibleInPortal))
                .ForMember(dest => dest.PortalPermission, opt => opt.MapFrom(src => src.PortalPermission))
                .ForMember(dest => dest.ViewPermissionMode, opt => opt.MapFrom(src => src.ViewPermissionMode))
                .ForMember(dest => dest.ViewTeams, opt => opt.MapFrom(src => DeserializeTeamList(src.ViewTeams)))
                .ForMember(dest => dest.OperateTeams, opt => opt.MapFrom(src => DeserializeTeamList(src.OperateTeams)))
                .ForMember(dest => dest.AttachmentManagementNeeded, opt => opt.MapFrom(src => src.AttachmentManagementNeeded))
                .ForMember(dest => dest.Required, opt => opt.MapFrom(src => src.Required))
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
                .ForMember(dest => dest.CoAssignees, opt => opt.MapFrom(src => SerializeDefaultAssignee(src.CoAssignees)))
                .ForMember(dest => dest.EstimatedDuration, opt => opt.MapFrom(src => src.EstimatedDuration))
                .ForMember(dest => dest.Order, opt => opt.MapFrom(src => src.Order))
                .ForMember(dest => dest.ChecklistId, opt => opt.MapFrom(src => src.ChecklistId))
                .ForMember(dest => dest.QuestionnaireId, opt => opt.MapFrom(src => src.QuestionnaireId))
                .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.Color))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.VisibleInPortal, opt => opt.MapFrom(src => src.VisibleInPortal))
                .ForMember(dest => dest.PortalPermission, opt => opt.MapFrom(src => src.PortalPermission))
                .ForMember(dest => dest.ViewPermissionMode, opt => opt.MapFrom(src => src.ViewPermissionMode))
                .ForMember(dest => dest.ViewTeams, opt => opt.MapFrom(src => SerializeTeamList(src.ViewTeams)))
                .ForMember(dest => dest.OperateTeams, opt => opt.MapFrom(src => SerializeTeamList(src.OperateTeams)))
                .ForMember(dest => dest.AttachmentManagementNeeded, opt => opt.MapFrom(src => src.AttachmentManagementNeeded))
                .ForMember(dest => dest.Required, opt => opt.MapFrom(src => src.Required))
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
                // First try to parse with custom converter to handle legacy StaticFields format
                var options = new JsonSerializerOptions
                {
                    NumberHandling = JsonNumberHandling.AllowReadingFromString,
                    PropertyNameCaseInsensitive = true
                };
                
                // Try parsing as JsonDocument first to check StaticFields format
                using var doc = JsonDocument.Parse(normalized);
                var components = new List<StageComponent>();
                
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    var component = new StageComponent
                    {
                        Key = element.TryGetProperty("Key", out var keyProp) ? keyProp.GetString() : "",
                        Order = element.TryGetProperty("Order", out var orderProp) ? orderProp.GetInt32() : 0,
                        IsEnabled = element.TryGetProperty("IsEnabled", out var enabledProp) ? enabledProp.GetBoolean() : true,
                        Configuration = element.TryGetProperty("Configuration", out var configProp) && configProp.ValueKind != JsonValueKind.Null ? configProp.GetString() : null,
                        ChecklistIds = new List<long>(),
                        QuestionnaireIds = new List<long>(),
                        ChecklistNames = new List<string>(),
                        QuestionnaireNames = new List<string>(),
                        QuickLinkIds = new List<long>(),
                        QuickLinkNames = new List<string>(),
                        StaticFields = new List<StaticFieldConfig>()
                    };
                    
                    // Parse ChecklistIds
                    if (element.TryGetProperty("ChecklistIds", out var checklistIdsProp) && checklistIdsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var id in checklistIdsProp.EnumerateArray())
                        {
                            if (id.TryGetInt64(out var longId))
                                component.ChecklistIds.Add(longId);
                        }
                    }
                    
                    // Parse QuestionnaireIds
                    if (element.TryGetProperty("QuestionnaireIds", out var questionnaireIdsProp) && questionnaireIdsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var id in questionnaireIdsProp.EnumerateArray())
                        {
                            if (id.TryGetInt64(out var longId))
                                component.QuestionnaireIds.Add(longId);
                        }
                    }
                    
                    // Parse QuickLinkIds
                    if (element.TryGetProperty("QuickLinkIds", out var quickLinkIdsProp) && quickLinkIdsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var id in quickLinkIdsProp.EnumerateArray())
                        {
                            if (id.TryGetInt64(out var longId))
                                component.QuickLinkIds.Add(longId);
                        }
                    }
                    
                    // Parse ChecklistNames
                    if (element.TryGetProperty("ChecklistNames", out var checklistNamesProp) && checklistNamesProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var name in checklistNamesProp.EnumerateArray())
                        {
                            if (name.ValueKind == JsonValueKind.String)
                                component.ChecklistNames.Add(name.GetString());
                        }
                    }
                    
                    // Parse QuestionnaireNames
                    if (element.TryGetProperty("QuestionnaireNames", out var questionnaireNamesProp) && questionnaireNamesProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var name in questionnaireNamesProp.EnumerateArray())
                        {
                            if (name.ValueKind == JsonValueKind.String)
                                component.QuestionnaireNames.Add(name.GetString());
                        }
                    }
                    
                    // Parse QuickLinkNames
                    if (element.TryGetProperty("QuickLinkNames", out var quickLinkNamesProp) && quickLinkNamesProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var name in quickLinkNamesProp.EnumerateArray())
                        {
                            if (name.ValueKind == JsonValueKind.String)
                                component.QuickLinkNames.Add(name.GetString());
                        }
                    }
                    
                    // Parse CustomerPortalAccess
                    if (element.TryGetProperty("CustomerPortalAccess", out var portalAccessProp))
                    {
                        if (portalAccessProp.TryGetInt32(out var portalAccessInt))
                        {
                            component.CustomerPortalAccess = (Domain.Shared.Enums.PortalPermissionEnum)portalAccessInt;
                        }
                    }
                    
                    // Parse StaticFields - handle both legacy string array and new object array format
                    if (element.TryGetProperty("StaticFields", out var staticFieldsProp) && staticFieldsProp.ValueKind == JsonValueKind.Array)
                    {
                        int order = 0;
                        foreach (var field in staticFieldsProp.EnumerateArray())
                        {
                            if (field.ValueKind == JsonValueKind.String)
                            {
                                // Legacy format: string array ["COMPANYNAME", "CONTACTNAME", ...]
                                component.StaticFields.Add(new StaticFieldConfig
                                {
                                    Id = field.GetString(),
                                    IsRequired = false,
                                    Order = order++
                                });
                            }
                            else if (field.ValueKind == JsonValueKind.Object)
                            {
                                // New format: object array [{"Id": "COMPANYNAME", "IsRequired": true, "Order": 0}, ...]
                                var config = new StaticFieldConfig
                                {
                                    Id = field.TryGetProperty("Id", out var idProp) ? idProp.GetString() : "",
                                    IsRequired = field.TryGetProperty("IsRequired", out var requiredProp) && requiredProp.GetBoolean(),
                                    Order = field.TryGetProperty("Order", out var fieldOrderProp) ? fieldOrderProp.GetInt32() : order++
                                };
                                component.StaticFields.Add(config);
                            }
                        }
                    }
                    
                    components.Add(component);
                }
                
                return components;
            }
            catch (Exception ex)
            {
                // JSON parsing failed, log warning and return empty list
                LogWarning(ex, "Failed to parse ComponentsJson, returning empty list");
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
                    component.StaticFields ??= new List<StaticFieldConfig>();
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
        /// Ensures the result is always a valid JSON array or null to prevent scalar value issues
        /// </summary>
        private static string SerializeDefaultAssignee(List<string> assigneeList)
        {
            if (assigneeList == null || !assigneeList.Any())
                return null;

            try
            {
                // Filter out only empty/whitespace entries, keep all valid IDs regardless of length
                // This allows both long IDs (snowflake) and short IDs (legacy/test systems)
                var validAssignees = assigneeList
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(id => id.Trim())
                    .Distinct()
                    .ToList();

                if (!validAssignees.Any())
                    return null;

                var jsonResult = JsonSerializer.Serialize(validAssignees, _jsonOptions);

                // Verify the result is valid JSON array format
                if (!jsonResult.TrimStart().StartsWith("[") || !jsonResult.TrimEnd().EndsWith("]"))
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Serialized assignee list is not a valid JSON array: {jsonResult}");
                    return null;
                }

                return jsonResult;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error serializing assignee list: {ex.Message}");
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
                catch (Exception ex)
                {
                    // Log and continue to fallback
                    LogWarning(ex, "Failed to parse nested JSON assignee data, trying fallback");
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
            catch (Exception ex)
            {
                // Fallback to comma-separated format if JSON serialization fails
                LogWarning(ex, "Failed to serialize assignee list to JSON, using comma-separated fallback");
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
                catch (JsonException ex)
                {
                    // If JSON parsing fails, log and fall back to comma-separated parsing
                    LogWarning(ex, "Failed to parse assignee string as JSON, using comma-separated fallback");
                }
            }

            // Fallback to comma-separated format for backward compatibility
            return assigneeString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .Where(s => !string.IsNullOrWhiteSpace(s))
                                .ToList();
        }

        /// <summary>
        /// Helper method to deserialize JSON string to List of strings
        /// Handles both direct JSON arrays and double-encoded JSON strings
        /// </summary>
        private static List<string> DeserializeTeamList(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return new List<string>();
            }

            try
            {
                var trimmed = jsonString.Trim();

                // Check if it's double-encoded (starts with a quote)
                if (trimmed.StartsWith("\"") && trimmed.EndsWith("\""))
                {
                    // Remove outer quotes and unescape
                    var unescaped = JsonSerializer.Deserialize<string>(trimmed);
                    if (!string.IsNullOrEmpty(unescaped))
                    {
                        trimmed = unescaped;
                    }
                }

                // Now deserialize the actual array
                if (trimmed.StartsWith("["))
                {
                    return JsonSerializer.Deserialize<List<string>>(trimmed) ?? new List<string>();
                }

                return new List<string>();
            }
            catch (Exception ex)
            {
                LogWarning(ex, "Failed to deserialize team list JSON");
                return new List<string>();
            }
        }

        /// <summary>
        /// Helper method to serialize List of strings to JSON string
        /// </summary>
        private static string SerializeTeamList(List<string> teamList)
        {
            if (teamList == null || teamList.Count == 0)
            {
                return null;
            }

            return JsonSerializer.Serialize(teamList);
        }
    }
}

