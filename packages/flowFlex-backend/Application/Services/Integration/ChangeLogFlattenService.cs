using System.Text.Json;
using FlowFlex.Application.Contracts.Dtos.Integration;
using FlowFlex.Application.Contracts.IServices.Integration;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.Integration;

/// <summary>
/// Converts ChangeLog records into a flattened field-level diff format.
/// Parses internal before_data/after_data JSON snapshots into {field, oldValue, newValue} arrays.
/// </summary>
public class ChangeLogFlattenService : IChangeLogFlattenService
{
    private readonly IOnboardingRepository _onboardingRepository;
    private readonly IOperationChangeLogRepository _changeLogRepository;
    private readonly ILogger<ChangeLogFlattenService> _logger;

    public ChangeLogFlattenService(
        IOnboardingRepository onboardingRepository,
        IOperationChangeLogRepository changeLogRepository,
        ILogger<ChangeLogFlattenService> logger)
    {
        _onboardingRepository = onboardingRepository ?? throw new ArgumentNullException(nameof(onboardingRepository));
        _changeLogRepository = changeLogRepository ?? throw new ArgumentNullException(nameof(changeLogRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<FlattenedChangeLogPagedResponse> GetFlattenedChangeLogsAsync(
        string? entityId = null,
        long? onboardingId = null,
        DateTimeOffset? since = null,
        bool changesOnly = false,
        int pageIndex = 1,
        int pageSize = 20)
    {
        // Validate: at least one identifier must be provided
        if (string.IsNullOrWhiteSpace(entityId) && !onboardingId.HasValue)
        {
            return new FlattenedChangeLogPagedResponse();
        }

        // Clamp page size
        pageSize = Math.Clamp(pageSize, 1, 100);
        pageIndex = Math.Max(1, pageIndex);

        // Resolve onboardingId from entityId if needed
        long resolvedOnboardingId;

        if (onboardingId.HasValue)
        {
            resolvedOnboardingId = onboardingId.Value;
        }
        else
        {
            // Find onboarding by entity_id
            var onboarding = await _onboardingRepository.GetFirstAsync(
                x => x.EntityId == entityId && x.IsValid);

            if (onboarding == null)
            {
                _logger.LogWarning("No onboarding found for entityId: {EntityId}", entityId);
                return new FlattenedChangeLogPagedResponse();
            }

            resolvedOnboardingId = onboarding.Id;
        }

        // Step 2: Query change logs for this onboarding
        var allLogs = await _changeLogRepository.GetByOnboardingIdAsync(resolvedOnboardingId);

        // Apply since filter
        if (since.HasValue)
        {
            allLogs = allLogs.Where(x => x.OperationTime > since.Value).ToList();
        }

        // Order by time descending
        allLogs = allLogs.OrderByDescending(x => x.OperationTime).ToList();

        // Step 3: Convert to flattened format
        var allItems = allLogs.Select(MapToExternalDto).ToList();

        // Apply changesOnly filter: only keep records with non-empty Changes array
        if (changesOnly)
        {
            allItems = allItems.Where(x => x.Changes.Count > 0).ToList();
        }

        // Pagination (applied after filtering)
        var totalCount = allItems.Count;
        var pagedItems = allItems
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new FlattenedChangeLogPagedResponse
        {
            Items = pagedItems,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    #region Private Mapping Methods

    /// <summary>
    /// Map internal OperationChangeLog to external flattened DTO
    /// </summary>
    private FlattenedChangeLogItemDto MapToExternalDto(OperationChangeLog log)
    {
        return new FlattenedChangeLogItemDto
        {
            Id = log.Id.ToString(),
            Timestamp = log.OperationTime,
            Operator = new ChangeLogOperatorDto
            {
                Id = log.OperatorId.ToString(),
                Name = log.OperatorName ?? string.Empty
            },
            OperationType = log.OperationType ?? string.Empty,
            Title = log.OperationTitle ?? string.Empty,
            Description = log.OperationDescription ?? string.Empty,
            Changes = ParseChanges(log)
        };
    }

    /// <summary>
    /// Parse before_data/after_data into flattened field changes
    /// </summary>
    private List<FieldChangeDto> ParseChanges(OperationChangeLog log)
    {
        try
        {
            var operationType = log.OperationType ?? string.Empty;

            // Handle StaticFieldValueChange - single field per record
            if (operationType == "StaticFieldValueChange")
            {
                return ParseStaticFieldChange(log);
            }

            // Handle object-level snapshot changes (CaseUpdate, StageComplete, etc.)
            if (!string.IsNullOrEmpty(log.BeforeData) || !string.IsNullOrEmpty(log.AfterData))
            {
                return ParseSnapshotChanges(log);
            }

            // For operation types without before/after data, return empty changes
            return new List<FieldChangeDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse changes for log {LogId}", log.Id);
            return new List<FieldChangeDto>();
        }
    }

    /// <summary>
    /// Parse static field value change (single field per record)
    /// Format: after_data = { "fieldName": "Company Name", "value": "\"ABC Inc\"" }
    /// </summary>
    private List<FieldChangeDto> ParseStaticFieldChange(OperationChangeLog log)
    {
        var changes = new List<FieldChangeDto>();

        var afterData = DeserializeJson(log.AfterData);
        var beforeData = DeserializeJson(log.BeforeData);

        if (afterData == null)
            return changes;

        var fieldName = GetJsonString(afterData, "fieldName") ?? "Unknown Field";
        var newValue = UnwrapQuotedValue(GetJsonString(afterData, "value"));
        string? oldValue = null;

        if (beforeData != null)
        {
            oldValue = UnwrapQuotedValue(GetJsonString(beforeData, "value"));
        }

        changes.Add(new FieldChangeDto
        {
            Field = fieldName,
            OldValue = oldValue,
            NewValue = newValue
        });

        return changes;
    }

    /// <summary>
    /// Parse object snapshot changes (before/after are full objects, changed_fields lists what changed)
    /// </summary>
    private List<FieldChangeDto> ParseSnapshotChanges(OperationChangeLog log)
    {
        var changes = new List<FieldChangeDto>();

        var beforeData = DeserializeJson(log.BeforeData);
        var afterData = DeserializeJson(log.AfterData);

        // Get changed fields list
        var changedFields = ParseChangedFields(log.ChangedFields);

        if (changedFields.Count == 0 && afterData != null && beforeData != null)
        {
            // If no explicit changed fields, diff all keys
            changedFields = afterData.Keys.ToList();
        }

        // For cases where only afterData exists (creation events)
        if (beforeData == null && afterData != null)
        {
            foreach (var key in afterData.Keys)
            {
                var friendlyName = GetFriendlyFieldName(key);
                var newValue = GetDisplayValue(afterData, key);
                if (!string.IsNullOrEmpty(newValue))
                {
                    changes.Add(new FieldChangeDto
                    {
                        Field = friendlyName,
                        OldValue = null,
                        NewValue = newValue
                    });
                }
            }
            return changes;
        }

        // Diff before vs after for changed fields
        if (beforeData != null && afterData != null)
        {
            foreach (var field in changedFields)
            {
                var oldValue = GetDisplayValue(beforeData, field);
                var newValue = GetDisplayValue(afterData, field);

                // Only include if values actually differ
                if (oldValue != newValue)
                {
                    changes.Add(new FieldChangeDto
                    {
                        Field = GetFriendlyFieldName(field),
                        OldValue = oldValue,
                        NewValue = newValue
                    });
                }
            }
        }

        return changes;
    }

    #endregion

    #region JSON Helpers

    private Dictionary<string, object?>? DeserializeJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            // Handle if it's already a parsed object (SqlSugar JSONB auto-deserialize)
            var doc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (doc == null) return null;

            return doc.ToDictionary(
                kvp => kvp.Key,
                kvp => (object?)kvp.Value.Clone());
        }
        catch
        {
            return null;
        }
    }

    private string? GetJsonString(Dictionary<string, object?> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value) || value == null)
            return null;

        if (value is JsonElement element)
        {
            return element.ValueKind == JsonValueKind.Null ? null : element.ToString();
        }

        return value.ToString();
    }

    private string? GetDisplayValue(Dictionary<string, object?> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value) || value == null)
            return null;

        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => element.GetRawText()
            };
        }

        return value.ToString();
    }

    /// <summary>
    /// Parse changed_fields which can be a JSON array string or a plain string
    /// </summary>
    private List<string> ParseChangedFields(string? changedFieldsJson)
    {
        if (string.IsNullOrWhiteSpace(changedFieldsJson))
            return new List<string>();

        try
        {
            // Try parsing as JSON array
            var trimmed = changedFieldsJson.Trim();
            if (trimmed.StartsWith("["))
            {
                var list = JsonSerializer.Deserialize<List<string>>(trimmed);
                return list ?? new List<string>();
            }

            // Plain string (single field name)
            return new List<string> { trimmed };
        }
        catch
        {
            return new List<string> { changedFieldsJson };
        }
    }

    /// <summary>
    /// Unwrap double-quoted JSON encoded values like "\"ABC Inc\""
    /// </summary>
    private string? UnwrapQuotedValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Handle double JSON encoding: "\"actual value\""
        if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length > 2)
        {
            try
            {
                var unwrapped = JsonSerializer.Deserialize<string>(value);
                return unwrapped ?? value;
            }
            catch
            {
                // Not valid JSON string, return as-is
            }
        }

        return value;
    }

    #endregion

    #region Field Name Mapping

    /// <summary>
    /// Map internal field names to human-readable display names
    /// </summary>
    private static string GetFriendlyFieldName(string fieldName)
    {
        return fieldName switch
        {
            "CaseName" or "LeadName" => "Customer Name",
            "CaseCode" => "Case Code",
            "Status" => "Status",
            "Priority" => "Priority",
            "WorkflowId" => "Workflow",
            "WorkflowName" => "Workflow",
            "CurrentStageId" => "Current Stage",
            "LifeCycleStageName" => "Life Cycle Stage",
            "ContactPerson" => "Contact Name",
            "ContactEmail" => "Contact Email",
            "Ownership" => "Owner",
            "OwnershipName" => "Owner",
            "ViewPermissionMode" => "View Permission Mode",
            "ViewTeams" => "View Teams",
            "ViewUsers" => "View Users",
            "OperateTeams" => "Operate Teams",
            "OperateUsers" => "Operate Users",
            "IsCompleted" => "Completed",
            "CompletionRate" => "Completion Rate",
            "CompletedBy" => "Completed By",
            "CompletedTime" => "Completed Time",
            "StageName" => "Stage",
            "StageId" => "Stage",
            _ => fieldName
        };
    }

    #endregion
}
