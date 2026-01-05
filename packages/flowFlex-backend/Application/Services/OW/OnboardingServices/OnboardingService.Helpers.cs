using AutoMapper;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Attr;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Events;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Utils;
using FlowFlex.Infrastructure.Extensions;
using FlowFlex.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using SqlSugar;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
// using Item.Redis; // Temporarily disable Redis
using System.Text.Json;
using PermissionOperationType = FlowFlex.Domain.Shared.Enums.Permission.OperationTypeEnum;
using FlowFlex.Application.Contracts.Dtos.OW.User;


namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Onboarding service - Helper and utility methods
    /// </summary>
    public partial class OnboardingService
    {
        public async Task<OnboardingProgressDto> GetProgressAsync(long id)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            // Ensure stages progress is properly initialized and synced
            await EnsureStagesProgressInitializedAsync(entity);

            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            var totalStages = stages.Count;
            var completedStages = entity.CurrentStageOrder;

            // Calculate estimated completion time based on average stage duration
            var avgStageDuration = TimeSpan.FromDays(7); // Default 7 days per stage
            var remainingStages = totalStages - completedStages;
            var estimatedCompletion = entity.CreateDate.AddDays(totalStages * 7);

            // Check if overdue
            var isOverdue = entity.Status != "Completed" &&
                           entity.EstimatedCompletionDate.HasValue &&
                           DateTimeOffset.UtcNow > entity.EstimatedCompletionDate.Value;

            // Map stages progress to DTO
            var stagesProgressDto = _mapper.Map<List<OnboardingStageProgressDto>>(entity.StagesProgress);

            // Get actions for each stage
            foreach (var stageProgress in stagesProgressDto)
            {
                try
                {
                    var actions = await _actionManagementService.GetActionTriggerMappingsByTriggerSourceIdAsync(stageProgress.StageId);
                    stageProgress.Actions = actions;
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the entire request
                    // Debug logging handled by structured logging
                    stageProgress.Actions = new List<ActionTriggerMappingWithActionInfo>();
                }
            }

            return new OnboardingProgressDto
            {
                OnboardingId = entity.Id,
                CurrentStageId = entity.CurrentStageId,
                CurrentStageName = stages.FirstOrDefault(s => s.Id == entity.CurrentStageId)?.Name,
                TotalStages = totalStages,
                CompletedStages = completedStages,
                CompletionPercentage = entity.CompletionRate,
                StartTime = entity.CreateDate,
                EstimatedCompletionTime = entity.EstimatedCompletionDate ?? estimatedCompletion,
                ActualCompletionTime = entity.ActualCompletionDate,
                IsOverdue = isOverdue,
                Status = entity.Status,
                Priority = entity.Priority,
                StagesProgress = stagesProgressDto
            };
        }

        /// <summary>
        /// Log general onboarding action to change log
        /// </summary>
        private async Task LogOnboardingActionAsync(Onboarding onboarding, string action, string logType, bool success, object additionalData = null)
        {
            try
            {
                var logData = new
                {
                    OnboardingId = onboarding.Id,
                    LeadId = onboarding.LeadId,
                    CaseName = onboarding.CaseName,
                    WorkflowId = onboarding.WorkflowId,
                    CurrentStageId = onboarding.CurrentStageId,
                    Status = onboarding.Status,
                    Priority = onboarding.Priority,
                    ActionTime = DateTimeOffset.UtcNow,
                    ActionBy = _operatorContextService.GetOperatorDisplayName(),
                    AdditionalData = additionalData
                };

                // Stage completion log functionality removed
                // Debug logging handled by structured logging
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
            }
        }

        /// <summary>
        /// Publish stage completion event for current stage completion (without CompleteStageInputDto)
        /// </summary>
        private async Task PublishStageCompletionEventForCurrentStageAsync(Onboarding onboarding, Stage completedStage, bool isFinalStage)
        {
            try
            {
                // Get next stage info if not final stage
                string nextStageName = null;
                long? nextStageId = null;
                if (!isFinalStage && onboarding.CurrentStageId.HasValue)
                {
                    var nextStage = await _stageRepository.GetByIdAsync(onboarding.CurrentStageId.Value);
                    nextStageName = nextStage?.Name;
                    nextStageId = nextStage?.Id;
                }

                // Build components payload
                var componentsPayload2 = await BuildStageCompletionComponentsAsync(onboarding.Id, completedStage.Id, completedStage.Components, completedStage.ComponentsJson);

                // Publish the OnboardingStageCompletedEvent for enhanced event handling
                var onboardingStageCompletedEvent = new OnboardingStageCompletedEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    Timestamp = DateTimeOffset.UtcNow,
                    Version = "1.0",
                    TenantId = onboarding.TenantId,
                    OnboardingId = onboarding.Id,
                    LeadId = onboarding.LeadId,
                    WorkflowId = onboarding.WorkflowId,
                    WorkflowName = (await _workflowRepository.GetByIdAsync(onboarding.WorkflowId))?.Name ?? "Unknown",
                    CompletedStageId = completedStage.Id,
                    CompletedStageName = completedStage.Name,
                    StageCategory = completedStage.Name ?? "Unknown",
                    NextStageId = nextStageId,
                    NextStageName = nextStageName,
                    CompletionRate = onboarding.CompletionRate,
                    IsFinalStage = isFinalStage,
                    AssigneeName = onboarding.CurrentAssigneeName ?? _operatorContextService.GetOperatorDisplayName(),
                    ResponsibleTeam = onboarding.CurrentTeam ?? "Default",
                    Priority = onboarding.Priority ?? "Medium",
                    Source = "CustomerPortal",
                    BusinessContext = new Dictionary<string, object>
                    {
                        ["CompletionMethod"] = "CompleteCurrentStage",
                        ["AutoMoveToNext"] = !isFinalStage,
                        ["CompletionNotes"] = "Stage completed via CompleteCurrentStageAsync"
                    },
                    RoutingTags = new List<string> { "onboarding", "stage-completion", "customer-portal", "auto-progression" },
                    Description = $"Stage '{completedStage.Name}' completed for Onboarding {onboarding.Id} via CompleteCurrentStageAsync",
                    Tags = new List<string> { "onboarding", "stage-completion", "auto-progression" },
                    Components = componentsPayload2
                };
                // Append lightweight debug metrics into business context for verification
                try
                {
                    onboardingStageCompletedEvent.BusinessContext["Components.ChecklistsCount"] = componentsPayload2?.Checklists?.Count ?? 0;
                    onboardingStageCompletedEvent.BusinessContext["Components.QuestionnairesCount"] = componentsPayload2?.Questionnaires?.Count ?? 0;
                    onboardingStageCompletedEvent.BusinessContext["Components.TaskCompletionsCount"] = componentsPayload2?.TaskCompletions?.Count ?? 0;
                    onboardingStageCompletedEvent.BusinessContext["Components.RequiredFieldsCount"] = componentsPayload2?.RequiredFields?.Count ?? 0;
                }
                catch { }

                // Use fire-and-forget pattern to handle events asynchronously without blocking main flow
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        // Create new scope to avoid ServiceProvider disposed error
                        using var scope = _serviceScopeFactory.CreateScope();
                        var scopedMediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                        await scopedMediator.Publish(onboardingStageCompletedEvent);
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't affect main flow
                        // TODO: Consider adding retry mechanism or using message queue
                        // Can add logging here, but ensure no exceptions are thrown
                    }
                });

                // Debug logging handled by structured logging
            }
            catch (Exception ex)
            {
                // Log error but don't fail the stage completion
                // Debug logging handled by structured logging
            }
        }

        /// <summary>
        /// Build components payload for stage completion event
        /// </summary>
        private async Task<StageCompletionComponents> BuildStageCompletionComponentsAsync(long onboardingId, long stageId, List<StageComponent> stageComponents, string componentsJson)
        {
            var payload = new StageCompletionComponents();

            LoggingExtensions.WriteLine($"[DEBUG] BuildStageCompletionComponentsAsync - OnboardingId: {onboardingId}, StageId: {stageId}");
            LoggingExtensions.WriteLine($"[DEBUG] Initial stageComponents count: {stageComponents?.Count ?? 0}");
            LoggingExtensions.WriteLine($"[DEBUG] Initial componentsJson: {(!string.IsNullOrWhiteSpace(componentsJson) ? "Present" : "Missing")}");

            // Ensure we have componentsJson from DB if missing
            if (string.IsNullOrWhiteSpace(componentsJson))
            {
                try
                {
                    var stageEntity = await _stageRepository.GetByIdAsync(stageId);
                    if (!string.IsNullOrWhiteSpace(stageEntity?.ComponentsJson))
                    {
                        componentsJson = stageEntity.ComponentsJson;
                        LoggingExtensions.WriteLine($"[DEBUG] Retrieved componentsJson from DB: {componentsJson.Substring(0, Math.Min(200, componentsJson.Length))}...");
                    }
                }
                catch (Exception ex)
                {
                    LoggingExtensions.WriteLine($"[DEBUG] Error retrieving componentsJson from DB: {ex.Message}");
                }
            }

            // Ensure we have components from JSON if not provided
            if ((stageComponents == null || stageComponents.Count == 0) && !string.IsNullOrWhiteSpace(componentsJson))
            {
                try
                {
                    stageComponents = JsonSerializer.Deserialize<List<StageComponent>>(componentsJson) ?? new List<StageComponent>();
                    LoggingExtensions.WriteLine($"[DEBUG] Standard JSON deserialization successful, components count: {stageComponents.Count}");
                }
                catch (Exception ex)
                {
                    LoggingExtensions.WriteLine($"[DEBUG] Standard JSON deserialization failed: {ex.Message}");
                    // Fallback: parse components from raw JSON with lenient parsing
                    var (parsedComponents, parsedStaticFields) = ParseComponentsFromJson(componentsJson);
                    stageComponents = parsedComponents;
                    LoggingExtensions.WriteLine($"[DEBUG] Lenient parsing successful, components count: {stageComponents.Count}");
                }
            }

            // Try fetch components from service if still empty
            if (stageComponents == null || stageComponents.Count == 0)
            {
                try
                {
                    var comps = await _stageService.GetComponentsAsync(stageId);
                    if (comps != null && comps.Count > 0)
                    {
                        stageComponents = comps;
                        LoggingExtensions.WriteLine($"[DEBUG] Retrieved components from service, count: {stageComponents.Count}");
                    }
                }
                catch (Exception ex)
                {
                    LoggingExtensions.WriteLine($"[DEBUG] Error retrieving components from service: {ex.Message}");
                }
            }

            LoggingExtensions.WriteLine($"[DEBUG] Final stageComponents count before processing: {stageComponents?.Count ?? 0}");

            // Process components to populate payload
            await ProcessStageComponentsAsync(onboardingId, stageId, stageComponents, componentsJson, payload);

            LoggingExtensions.WriteLine($"[DEBUG] Final payload counts - Checklists: {payload.Checklists.Count}, Questionnaires: {payload.Questionnaires.Count}, TaskCompletions: {payload.TaskCompletions.Count}, RequiredFields: {payload.RequiredFields.Count}");

            return payload;
        }
        /// <summary>
        /// Parse components from JSON with lenient parsing for both camelCase and PascalCase
        /// </summary>
        private (List<StageComponent> stageComponents, List<string> staticFieldNames) ParseComponentsFromJson(string componentsJson)
        {
            var stageComponents = new List<StageComponent>();
            var staticFieldNames = new List<string>();

            try
            {
                // Recursively unwrap JSON strings until we get to the actual array
                string currentJson = componentsJson;
                JsonDocument currentDoc = null;
                int unwrapCount = 0;

                while (true)
                {
                    currentDoc?.Dispose();
                    currentDoc = JsonDocument.Parse(currentJson);
                    LoggingExtensions.WriteLine($"[DEBUG] JSON unwrap #{unwrapCount}: root type = {currentDoc.RootElement.ValueKind}");

                    if (currentDoc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        LoggingExtensions.WriteLine($"[DEBUG] Found JSON array after {unwrapCount} unwraps");
                        break;
                    }
                    else if (currentDoc.RootElement.ValueKind == JsonValueKind.String)
                    {
                        currentJson = currentDoc.RootElement.GetString();
                        LoggingExtensions.WriteLine($"[DEBUG] Unwrapped JSON string, length: {currentJson?.Length}");
                        unwrapCount++;

                        if (unwrapCount > 5) // Prevent infinite loop
                        {
                            LoggingExtensions.WriteLine($"[ERROR] Too many JSON unwrap levels, stopping at {unwrapCount}");
                            break;
                        }
                    }
                    else
                    {
                        LoggingExtensions.WriteLine($"[ERROR] Unexpected JSON root type: {currentDoc.RootElement.ValueKind}");
                        break;
                    }
                }

                if (currentDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    LoggingExtensions.WriteLine($"[DEBUG] Processing {currentDoc.RootElement.GetArrayLength()} JSON array elements");
                    int elementIndex = 0;
                    foreach (var elem in currentDoc.RootElement.EnumerateArray())
                    {
                        LoggingExtensions.WriteLine($"[DEBUG] Element {elementIndex}: Type={elem.ValueKind}");
                        if (elem.ValueKind != JsonValueKind.Object)
                        {
                            elementIndex++;
                            continue;
                        }

                        // Get key with both camelCase and PascalCase support
                        string key = GetJsonProperty(elem, "key", "Key");
                        LoggingExtensions.WriteLine($"[DEBUG] Element {elementIndex} - Raw JSON: {elem.GetRawText()}");
                        LoggingExtensions.WriteLine($"[DEBUG] Element {elementIndex} - Extracted key: '{key}'");
                        if (string.IsNullOrWhiteSpace(key))
                        {
                            elementIndex++;
                            continue;
                        }

                        // Create StageComponent for each parsed element
                        var component = new StageComponent { Key = key };

                        if (string.Equals(key, "checklist", StringComparison.OrdinalIgnoreCase))
                        {
                            // Populate the StageComponent
                            component.ChecklistIds = new List<long>();
                            component.ChecklistNames = new List<string>();
                            var idArr = GetJsonArrayProperty(elem, "checklistIds", "ChecklistIds");
                            if (idArr.HasValue)
                            {
                                foreach (var idEl in idArr.Value.EnumerateArray())
                                {
                                    if (idEl.ValueKind == JsonValueKind.Number && idEl.TryGetInt64(out var lid)) component.ChecklistIds.Add(lid);
                                    else if (idEl.ValueKind == JsonValueKind.String && long.TryParse(idEl.GetString(), out var lsid)) component.ChecklistIds.Add(lsid);
                                }
                            }
                            var nameArr = GetJsonArrayProperty(elem, "checklistNames", "ChecklistNames");
                            if (nameArr.HasValue)
                            {
                                foreach (var nEl in nameArr.Value.EnumerateArray())
                                {
                                    if (nEl.ValueKind == JsonValueKind.String) component.ChecklistNames.Add(nEl.GetString());
                                }
                            }
                        }
                        else if (string.Equals(key, "questionnaires", StringComparison.OrdinalIgnoreCase))
                        {
                            // Populate the StageComponent
                            component.QuestionnaireIds = new List<long>();
                            component.QuestionnaireNames = new List<string>();
                            var idArr = GetJsonArrayProperty(elem, "questionnaireIds", "QuestionnaireIds");
                            if (idArr.HasValue)
                            {
                                foreach (var idEl in idArr.Value.EnumerateArray())
                                {
                                    if (idEl.ValueKind == JsonValueKind.Number && idEl.TryGetInt64(out var lid)) component.QuestionnaireIds.Add(lid);
                                    else if (idEl.ValueKind == JsonValueKind.String && long.TryParse(idEl.GetString(), out var lsid)) component.QuestionnaireIds.Add(lsid);
                                }
                            }
                            var nameArr = GetJsonArrayProperty(elem, "questionnaireNames", "QuestionnaireNames");
                            if (nameArr.HasValue)
                            {
                                foreach (var nEl in nameArr.Value.EnumerateArray())
                                {
                                    if (nEl.ValueKind == JsonValueKind.String) component.QuestionnaireNames.Add(nEl.GetString());
                                }
                            }
                        }
                        else if (string.Equals(key, "fields", StringComparison.OrdinalIgnoreCase))
                        {
                            // Extract static field configurations
                            var sfArr = GetJsonArrayProperty(elem, "staticFields", "StaticFields");
                            if (sfArr.HasValue)
                            {
                                var fieldConfigs = new List<StaticFieldConfig>();
                                foreach (var s in sfArr.Value.EnumerateArray())
                                {
                                    if (s.ValueKind == JsonValueKind.String)
                                    {
                                        // Legacy format: string array
                                        var name = s.GetString();
                                        if (!string.IsNullOrWhiteSpace(name))
                                        {
                                            staticFieldNames.Add(name);
                                            fieldConfigs.Add(new StaticFieldConfig { Id = name, IsRequired = false, Order = fieldConfigs.Count + 1 });
                                        }
                                    }
                                    else if (s.ValueKind == JsonValueKind.Object)
                                    {
                                        // New format: object array with id, isRequired, order
                                        var id = GetJsonProperty(s, "id", "Id");
                                        if (!string.IsNullOrWhiteSpace(id))
                                        {
                                            staticFieldNames.Add(id);
                                            var isRequired = s.TryGetProperty("isRequired", out var reqProp) ? reqProp.GetBoolean() :
                                                            (s.TryGetProperty("IsRequired", out reqProp) ? reqProp.GetBoolean() : false);
                                            var order = s.TryGetProperty("order", out var ordProp) ? ordProp.GetInt32() :
                                                       (s.TryGetProperty("Order", out ordProp) ? ordProp.GetInt32() : fieldConfigs.Count + 1);
                                            fieldConfigs.Add(new StaticFieldConfig { Id = id, IsRequired = isRequired, Order = order });
                                        }
                                    }
                                }
                                component.StaticFields = fieldConfigs;
                            }
                        }

                        // Add component to list after processing all types
                        stageComponents.Add(component);
                        LoggingExtensions.WriteLine($"[DEBUG] Added component '{key}' to list, total count: {stageComponents.Count}");
                        elementIndex++;
                    }
                }
                LoggingExtensions.WriteLine($"[DEBUG] ParseComponentsFromJson completed - Components: {stageComponents.Count}, StaticFields: {staticFieldNames.Count}");

                currentDoc?.Dispose();
            }
            catch (Exception ex)
            {
                LoggingExtensions.WriteLine($"[ERROR] ParseComponentsFromJson failed: {ex.Message}");
            }

            return (stageComponents, staticFieldNames);
        }

        /// <summary>
        /// Get JSON property value with support for both camelCase and PascalCase
        /// </summary>
        private string GetJsonProperty(JsonElement elem, string camelCase, string pascalCase)
        {
            if (elem.TryGetProperty(camelCase, out var prop)) return prop.GetString();
            if (elem.TryGetProperty(pascalCase, out var propPascal)) return propPascal.GetString();
            return null;
        }

        /// <summary>
        /// Get JSON array property with support for both camelCase and PascalCase
        /// </summary>
        private JsonElement? GetJsonArrayProperty(JsonElement elem, string camelCase, string pascalCase)
        {
            if (elem.TryGetProperty(camelCase, out var arr) && arr.ValueKind == JsonValueKind.Array) return arr;
            if (elem.TryGetProperty(pascalCase, out var arrPascal) && arrPascal.ValueKind == JsonValueKind.Array) return arrPascal;
            return null;
        }

        /// <summary>
        /// Process stage components to populate the payload
        /// </summary>
        private async Task ProcessStageComponentsAsync(long onboardingId, long stageId, List<StageComponent> stageComponents, string componentsJson, StageCompletionComponents payload)
        {
            var staticFieldNames = new List<string>();

            // If we need to parse from JSON for static fields, do it once here
            if ((stageComponents == null || !stageComponents.Any(c => c.Key == "fields")) && !string.IsNullOrWhiteSpace(componentsJson))
            {
                var (_, parsedStaticFields) = ParseComponentsFromJson(componentsJson);
                staticFieldNames.AddRange(parsedStaticFields);
            }

            // Process checklists
            await ProcessChecklistsAsync(onboardingId, stageId, stageComponents, componentsJson, payload);

            // Process questionnaires
            await ProcessQuestionnairesAsync(onboardingId, stageId, stageComponents, componentsJson, payload);

            // Process required fields
            await ProcessRequiredFieldsAsync(onboardingId, stageId, stageComponents, componentsJson, payload, staticFieldNames);
        }

        /// <summary>
        /// Process checklists and task completions
        /// </summary>
        private async Task ProcessChecklistsAsync(long onboardingId, long stageId, List<StageComponent> stageComponents, string componentsJson, StageCompletionComponents payload)
        {
            try
            {
                // Get checklist components - prioritize stageComponents, fallback to JSON parsing
                var checklistComponents = (stageComponents ?? new List<StageComponent>()).Where(c => c.Key == "checklist").ToList();

                if (checklistComponents.Count == 0 && !string.IsNullOrWhiteSpace(componentsJson))
                {
                    var (parsedComponents, _) = ParseComponentsFromJson(componentsJson);
                    checklistComponents = parsedComponents.Where(c => c.Key == "checklist").ToList();
                }

                // Fill checklist selections from components with detailed information
                foreach (var component in checklistComponents)
                {
                    if (component.ChecklistIds != null)
                    {
                        for (int i = 0; i < component.ChecklistIds.Count; i++)
                        {
                            var checklistId = component.ChecklistIds[i];
                            var checklistName = (component.ChecklistNames != null && component.ChecklistNames.Count > i)
                                ? component.ChecklistNames[i]
                                : $"Checklist {checklistId}";

                            // Get detailed checklist information
                            try
                            {
                                var detailedChecklist = await _checklistService.GetByIdAsync(checklistId);
                                if (detailedChecklist != null)
                                {
                                    // Map tasks
                                    var tasks = new List<ChecklistTaskInfo>();
                                    if (detailedChecklist.Tasks != null)
                                    {
                                        foreach (var task in detailedChecklist.Tasks)
                                        {
                                            tasks.Add(new ChecklistTaskInfo
                                            {
                                                Id = task.Id,
                                                ChecklistId = task.ChecklistId,
                                                Name = task.Name,
                                                Description = task.Description,
                                                OrderIndex = task.OrderIndex,
                                                TaskType = task.TaskType,
                                                IsRequired = task.IsRequired,
                                                EstimatedHours = task.EstimatedHours,
                                                Priority = task.Priority,
                                                IsCompleted = task.IsCompleted,
                                                Status = task.Status,
                                                IsActive = task.IsActive
                                            });
                                        }
                                    }

                                    payload.Checklists.Add(new ChecklistComponentInfo
                                    {
                                        ChecklistId = checklistId,
                                        ChecklistName = detailedChecklist.Name ?? checklistName,
                                        Description = detailedChecklist.Description,
                                        Team = detailedChecklist.Team,
                                        Type = detailedChecklist.Type,
                                        Status = detailedChecklist.Status,
                                        IsTemplate = detailedChecklist.IsTemplate,
                                        CompletionRate = detailedChecklist.CompletionRate,
                                        TotalTasks = detailedChecklist.TotalTasks,
                                        CompletedTasks = detailedChecklist.CompletedTasks,
                                        IsActive = detailedChecklist.IsActive,
                                        Tasks = tasks
                                    });
                                }
                                else
                                {
                                    // Fallback to basic info if detailed info not available
                                    payload.Checklists.Add(new ChecklistComponentInfo
                                    {
                                        ChecklistId = checklistId,
                                        ChecklistName = checklistName,
                                        IsActive = true
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                LoggingExtensions.WriteLine($"[ERROR] Failed to get checklist details for {checklistId}: {ex.Message}");
                                // Fallback to basic info
                                payload.Checklists.Add(new ChecklistComponentInfo
                                {
                                    ChecklistId = checklistId,
                                    ChecklistName = checklistName,
                                    IsActive = true
                                });
                            }
                        }
                    }
                }

                // Load task completions
                var completions = await _checklistTaskCompletionService.GetByOnboardingAndStageAsync(onboardingId, stageId);
                foreach (var c in completions)
                {
                    payload.TaskCompletions.Add(new ChecklistTaskCompletionInfo
                    {
                        ChecklistId = c.ChecklistId,
                        TaskId = c.TaskId,
                        IsCompleted = c.IsCompleted,
                        CompletionNotes = c.CompletionNotes,
                        CompletedBy = c.ModifyBy ?? c.CreateBy,
                        CompletedTime = c.CompletedTime
                    });
                }
            }
            catch { }
        }

        /// <summary>
        /// Process questionnaires and answers
        /// </summary>
        private async Task ProcessQuestionnairesAsync(long onboardingId, long stageId, List<StageComponent> stageComponents, string componentsJson, StageCompletionComponents payload)
        {
            try
            {
                // Get questionnaire components - prioritize stageComponents, fallback to JSON parsing
                var questionnaireComponents = (stageComponents ?? new List<StageComponent>()).Where(c => c.Key == "questionnaires").ToList();

                if (questionnaireComponents.Count == 0 && !string.IsNullOrWhiteSpace(componentsJson))
                {
                    var (parsedComponents, _) = ParseComponentsFromJson(componentsJson);
                    questionnaireComponents = parsedComponents.Where(c => c.Key == "questionnaires").ToList();
                }

                // Fill questionnaire selections from components with detailed information
                foreach (var component in questionnaireComponents)
                {
                    if (component.QuestionnaireIds != null)
                    {
                        for (int i = 0; i < component.QuestionnaireIds.Count; i++)
                        {
                            var questionnaireId = component.QuestionnaireIds[i];
                            var questionnaireName = (component.QuestionnaireNames != null && component.QuestionnaireNames.Count > i)
                                ? component.QuestionnaireNames[i]
                                : $"Questionnaire {questionnaireId}";

                            // Get detailed questionnaire information
                            try
                            {
                                var detailedQuestionnaire = await _questionnaireService.GetByIdAsync(questionnaireId);
                                if (detailedQuestionnaire != null)
                                {
                                    payload.Questionnaires.Add(new QuestionnaireComponentInfo
                                    {
                                        QuestionnaireId = questionnaireId,
                                        QuestionnaireName = detailedQuestionnaire.Name ?? questionnaireName,
                                        Description = detailedQuestionnaire.Description,
                                        Status = detailedQuestionnaire.Status,
                                        Version = detailedQuestionnaire.Version,
                                        Category = detailedQuestionnaire.Category,
                                        TotalQuestions = detailedQuestionnaire.TotalQuestions,
                                        RequiredQuestions = detailedQuestionnaire.RequiredQuestions,
                                        AllowDraft = detailedQuestionnaire.AllowDraft,
                                        AllowMultipleSubmissions = detailedQuestionnaire.AllowMultipleSubmissions,
                                        IsActive = detailedQuestionnaire.IsActive,
                                        StructureJson = detailedQuestionnaire.StructureJson
                                    });
                                }
                                else
                                {
                                    // Fallback to basic info if detailed info not available
                                    payload.Questionnaires.Add(new QuestionnaireComponentInfo
                                    {
                                        QuestionnaireId = questionnaireId,
                                        QuestionnaireName = questionnaireName,
                                        IsActive = true
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                LoggingExtensions.WriteLine($"[ERROR] Failed to get questionnaire details for {questionnaireId}: {ex.Message}");
                                // Fallback to basic info
                                payload.Questionnaires.Add(new QuestionnaireComponentInfo
                                {
                                    QuestionnaireId = questionnaireId,
                                    QuestionnaireName = questionnaireName,
                                    IsActive = true
                                });
                            }
                        }
                    }
                }

                // Load questionnaire answers
                var answers = await _questionnaireAnswerService.GetAllAnswersAsync(onboardingId, stageId);
                foreach (var a in answers)
                {
                    payload.QuestionnaireAnswers.Add(new QuestionnaireAnswerInfo
                    {
                        AnswerId = a.Id,
                        QuestionnaireId = a.QuestionnaireId ?? 0,
                        QuestionId = 0,
                        QuestionText = string.Empty,
                        QuestionType = string.Empty,
                        IsRequired = false,
                        Answer = a.AnswerJson,
                        AnswerTime = a.SubmitTime,
                        Status = a.Status
                    });
                }
            }
            catch { }
        }

        /// <summary>
        /// Process required fields
        /// </summary>
        private async Task ProcessRequiredFieldsAsync(long onboardingId, long stageId, List<StageComponent> stageComponents, string componentsJson, StageCompletionComponents payload, List<string> staticFieldNames)
        {
            try
            {
                // Collect static field names from components
                foreach (var comp in (stageComponents ?? new List<StageComponent>()).Where(c => c.Key == "fields"))
                {
                    if (comp.StaticFields != null)
                    {
                        foreach (var fieldConfig in comp.StaticFields)
                        {
                            if (!string.IsNullOrWhiteSpace(fieldConfig.Id) && !staticFieldNames.Contains(fieldConfig.Id))
                            {
                                staticFieldNames.Add(fieldConfig.Id);
                            }
                        }
                    }
                }

                // If no static fields found, parse from JSON
                if (staticFieldNames.Count == 0 && !string.IsNullOrWhiteSpace(componentsJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(componentsJson);
                        if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var elem in doc.RootElement.EnumerateArray())
                            {
                                if (elem.ValueKind != JsonValueKind.Object) continue;

                                string key = GetJsonProperty(elem, "key", "Key");
                                if (!string.Equals(key, "fields", StringComparison.OrdinalIgnoreCase)) continue;

                                var sfArr = GetJsonArrayProperty(elem, "staticFields", "StaticFields");
                                if (sfArr.HasValue)
                                {
                                    foreach (var s in sfArr.Value.EnumerateArray())
                                    {
                                        string fieldId = null;
                                        if (s.ValueKind == JsonValueKind.String)
                                        {
                                            // Legacy format: string array
                                            fieldId = s.GetString();
                                        }
                                        else if (s.ValueKind == JsonValueKind.Object)
                                        {
                                            // New format: object array
                                            fieldId = GetJsonProperty(s, "id", "Id");
                                        }
                                        
                                        if (!string.IsNullOrWhiteSpace(fieldId) && !staticFieldNames.Contains(fieldId))
                                        {
                                            staticFieldNames.Add(fieldId);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }

                // Load existing field values
                var fieldValues = await _staticFieldValueService.GetLatestByOnboardingAndStageAsync(onboardingId, stageId);
                var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var f in fieldValues)
                {
                    payload.RequiredFields.Add(new RequiredFieldInfo
                    {
                        FieldName = f.FieldName,
                        DisplayName = f.DisplayName,
                        FieldType = f.FieldType,
                        IsRequired = f.IsRequired,
                        FieldValue = f.FieldValueJson,
                        ValidationStatus = f.ValidationStatus,
                        ValidationErrors = string.IsNullOrWhiteSpace(f.ValidationErrors)
                            ? new List<string>()
                            : new List<string>(f.ValidationErrors.Split('\n'))
                    });
                    existing.Add(f.FieldName ?? string.Empty);
                }

                // Add placeholder entries for missing required fields
                foreach (var fieldName in staticFieldNames.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!existing.Contains(fieldName))
                    {
                        payload.RequiredFields.Add(new RequiredFieldInfo
                        {
                            FieldName = fieldName,
                            DisplayName = null,
                            FieldType = string.Empty,
                            IsRequired = true,
                            FieldValue = null,
                            ValidationStatus = "Pending",
                            ValidationErrors = new List<string>()
                        });
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Export onboarding data to Excel
        /// </summary>
        public async Task<Stream> ExportToExcelAsync(OnboardingQueryRequest query)
        {
            // Get data using existing query method
            var result = await QueryAsync(query);
            var data = result.Data;

            // Transform to export format
            var exportData = data.Select(item => new OnboardingExportDto
            {
                CustomerName = item.CaseName,
                Id = item.LeadId,
                CaseCode = item.CaseCode,
                ContactName = item.ContactPerson,
                LifeCycleStage = item.LifeCycleStageName,
                WorkFlow = item.WorkflowName,
                OnboardStage = item.CurrentStageName,
                Priority = item.Priority,
                Ownership = !string.IsNullOrWhiteSpace(item.OwnershipName)
                    ? $"{item.OwnershipName} ({item.OwnershipEmail})"
                    : string.Empty,
                Status = GetDisplayStatus(item.Status),
                StartDate = FormatDateForExport(item.CurrentStageStartTime),
                EndDate = FormatDateForExport(item.CurrentStageEndTime),
                UpdatedBy = string.IsNullOrWhiteSpace(item.StageUpdatedBy) ? item.ModifyBy : item.StageUpdatedBy,
                UpdateTime = (item.StageUpdatedTime.HasValue ? item.StageUpdatedTime.Value : item.ModifyDate)
                  .ToString("MM/dd/yyyy HH:mm:ss")
            }).ToList();
            // Use EPPlus to generate Excel file (avoid NPOI version conflict)
            return GenerateExcelWithEPPlus(exportData);
        }

        /// <summary>
        /// Convert status to display format to match frontend logic
        /// </summary>
        private string GetDisplayStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
                return status;

            return status switch
            {
                "Active" or "Started" => "InProgress",
                "Cancelled" => "Aborted",
                "Force Completed" => "Force Completed",
                _ => status
            };
        }

        /// <summary>
        /// Format date to match frontend display format (MM/dd/yyyy HH:mm)
        /// </summary>
        private string FormatDateForExport(DateTimeOffset? dateTime)
        {
            if (!dateTime.HasValue)
                return "";

            // Format as MM/dd/yyyy HH:mm to include time precision to minutes
            return dateTime.Value.ToString("MM/dd/yyyy HH:mm");
        }

        /// <summary>
        /// Generate Excel file using EPPlus
        /// </summary>
        private Stream GenerateExcelWithEPPlus(List<OnboardingExportDto> data)
        {
            using var package = new OfficeOpenXml.ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Onboarding Export");

            // Set headers
            var headers = new[]
            {
                "Customer Name", "Case Code", "Contact Name", "Life Cycle Stage", "Workflow", "Stage",
                "Priority", "Ownership", "Status", "Start Date", "End Date", "Updated By", "Update Time"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }

            // Set data
            for (int row = 0; row < data.Count; row++)
            {
                var item = data[row];
                worksheet.Cells[row + 2, 1].Value = item.CustomerName;
                worksheet.Cells[row + 2, 2].Value = item.CaseCode;
                worksheet.Cells[row + 2, 3].Value = item.ContactName;
                worksheet.Cells[row + 2, 4].Value = item.LifeCycleStage;
                worksheet.Cells[row + 2, 5].Value = item.WorkFlow;
                worksheet.Cells[row + 2, 6].Value = item.OnboardStage;
                worksheet.Cells[row + 2, 7].Value = item.Priority;
                worksheet.Cells[row + 2, 8].Value = item.Ownership;
                worksheet.Cells[row + 2, 9].Value = item.Status;
                worksheet.Cells[row + 2, 10].Value = item.StartDate;
                worksheet.Cells[row + 2, 11].Value = item.EndDate;
                worksheet.Cells[row + 2, 12].Value = item.UpdatedBy;
                worksheet.Cells[row + 2, 13].Value = item.UpdateTime;
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// Initialize stages progress array for a new onboarding
        /// </summary>
    }
}

