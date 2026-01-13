using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SqlSugar;

namespace FlowFlex.Application.Service.OW
{
    /// <summary>
    /// Component data retrieval service implementation
    /// </summary>
    public class ComponentDataService : IComponentDataService, IScopedService
    {
        private readonly ISqlSugarClient _db;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IStageRepository _stageRepository;
        private readonly UserContext _userContext;
        private readonly ILogger<ComponentDataService> _logger;

        public ComponentDataService(
            ISqlSugarClient db,
            IOnboardingRepository onboardingRepository,
            IStageRepository stageRepository,
            UserContext userContext,
            ILogger<ComponentDataService> logger)
        {
            _db = db;
            _onboardingRepository = onboardingRepository;
            _stageRepository = stageRepository;
            _userContext = userContext;
            _logger = logger;
        }

        /// <summary>
        /// Get checklist data for a stage
        /// </summary>
        public async Task<ChecklistData> GetChecklistDataAsync(long onboardingId, long stageId, string? tenantId = null)
        {
            try
            {
                var result = new ChecklistData();

                // Use provided tenantId or fall back to UserContext
                var effectiveTenantId = tenantId ?? _userContext.TenantId;

                // Get checklist task completions for this onboarding
                var completions = await _db.Queryable<ChecklistTaskCompletion>()
                    .Where(c => c.OnboardingId == onboardingId && c.IsValid)
                    .Where(c => c.TenantId == effectiveTenantId)
                    .ToListAsync();

                // Get stage to find associated checklists from components_json
                var stage = await _stageRepository.GetByIdAsync(stageId);
                if (stage == null)
                {
                    _logger.LogDebug("Stage {StageId} not found", stageId);
                    return result;
                }

                // Get all checklist IDs from components_json
                var checklistIds = new List<long>();
                
                // First try to get from components_json
                if (!string.IsNullOrEmpty(stage.ComponentsJson))
                {
                    try
                    {
                        var components = System.Text.Json.JsonSerializer.Deserialize<List<FlowFlex.Domain.Shared.Models.StageComponent>>(
                            stage.ComponentsJson, 
                            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        if (components != null)
                        {
                            foreach (var component in components)
                            {
                                if (component.Key == "checklist" && component.ChecklistIds != null)
                                {
                                    checklistIds.AddRange(component.ChecklistIds);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse components_json for stage {StageId}", stageId);
                    }
                }

                // Fallback to stage.ChecklistId if no checklists found in components_json
                if (!checklistIds.Any() && stage.ChecklistId.HasValue)
                {
                    checklistIds.Add(stage.ChecklistId.Value);
                }

                if (!checklistIds.Any())
                {
                    _logger.LogDebug("No checklists found for stage {StageId}", stageId);
                    return result;
                }

                _logger.LogDebug("Found {ChecklistCount} checklists for stage {StageId}: [{ChecklistIds}]", 
                    checklistIds.Count, stageId, string.Join(", ", checklistIds));

                // Get checklist tasks for all checklists
                var tasks = await _db.Queryable<ChecklistTask>()
                    .Where(t => checklistIds.Contains(t.ChecklistId) && t.IsValid)
                    .ToListAsync();

                result.TotalCount = tasks.Count;
                result.CompletedCount = completions.Count(c => c.IsCompleted && tasks.Any(t => t.Id == c.TaskId));
                result.Status = result.CompletedCount >= result.TotalCount && result.TotalCount > 0 ? "Completed" : "Pending";

                result.Tasks = tasks.Select(t => new TaskStatusData
                {
                    TaskId = t.Id,
                    ChecklistId = t.ChecklistId,
                    Name = t.Name,
                    IsCompleted = completions.Any(c => c.TaskId == t.Id && c.IsCompleted),
                    CompletionNotes = completions.FirstOrDefault(c => c.TaskId == t.Id)?.CompletionNotes
                }).ToList();

                _logger.LogDebug("Retrieved {TaskCount} tasks from {ChecklistCount} checklists for stage {StageId}", 
                    result.Tasks.Count, checklistIds.Count, stageId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting checklist data for onboarding {OnboardingId}, stage {StageId}", onboardingId, stageId);
                return new ChecklistData();
            }
        }

        /// <summary>
        /// Get questionnaire data for a stage
        /// </summary>
        public async Task<QuestionnaireData> GetQuestionnaireDataAsync(long onboardingId, long stageId, string? tenantId = null)
        {
            try
            {
                var result = new QuestionnaireData();

                // Use provided tenantId or fall back to UserContext
                var effectiveTenantId = tenantId ?? _userContext.TenantId;

                // Get stage to find associated questionnaires from components_json
                var stage = await _stageRepository.GetByIdAsync(stageId);
                if (stage == null)
                {
                    _logger.LogDebug("Stage {StageId} not found", stageId);
                    return result;
                }

                // Get all questionnaire IDs from components_json
                var questionnaireIds = new List<long>();
                
                // First try to get from components_json
                if (!string.IsNullOrEmpty(stage.ComponentsJson))
                {
                    try
                    {
                        var components = System.Text.Json.JsonSerializer.Deserialize<List<FlowFlex.Domain.Shared.Models.StageComponent>>(
                            stage.ComponentsJson, 
                            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        if (components != null)
                        {
                            foreach (var component in components)
                            {
                                if (component.Key == "questionnaires" && component.QuestionnaireIds != null)
                                {
                                    questionnaireIds.AddRange(component.QuestionnaireIds);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse components_json for stage {StageId}", stageId);
                    }
                }

                // Fallback to stage.QuestionnaireId if no questionnaires found in components_json
                if (!questionnaireIds.Any() && stage.QuestionnaireId.HasValue)
                {
                    questionnaireIds.Add(stage.QuestionnaireId.Value);
                }

                if (!questionnaireIds.Any())
                {
                    _logger.LogDebug("No questionnaires found for stage {StageId}", stageId);
                    return result;
                }

                _logger.LogDebug("Found {QuestionnaireCount} questionnaires for stage {StageId}: [{QuestionnaireIds}]", 
                    questionnaireIds.Count, stageId, string.Join(", ", questionnaireIds));

                // Initialize empty dictionaries for all questionnaires in components_json
                // This ensures rules can safely access answers[questionnaireId] without KeyNotFoundException
                foreach (var questionnaireId in questionnaireIds)
                {
                    var questionnaireIdStr = questionnaireId.ToString();
                    if (!result.Answers.ContainsKey(questionnaireIdStr))
                    {
                        result.Answers[questionnaireIdStr] = new Dictionary<string, object>();
                        _logger.LogDebug("Initialized empty answers dictionary for questionnaire {QuestionnaireId}", questionnaireIdStr);
                    }
                }

                // Get questionnaire answers for all questionnaires
                // Build nested structure: answers[questionnaireId][questionId] = value
                var answers = await _db.Queryable<QuestionnaireAnswer>()
                    .Where(a => a.OnboardingId == onboardingId && a.QuestionnaireId.HasValue && questionnaireIds.Contains(a.QuestionnaireId.Value) && a.IsValid)
                    .Where(a => a.TenantId == effectiveTenantId)
                    .ToListAsync();

                foreach (var answer in answers)
                {
                    var questionnaireIdStr = answer.QuestionnaireId.ToString();
                    
                    _logger.LogDebug("Processing questionnaire answer: QuestionnaireId={QuestionnaireId}, AnswerType={AnswerType}", 
                        questionnaireIdStr, 
                        answer.Answer?.GetType().Name ?? "null");

                    if (answer.Answer != null)
                    {
                        try
                        {
                            // The answer JSON structure is: { "responses": [{ "questionId": "xxx", "answer": "value", ... }, ...] }
                            // We need to convert it to: { "questionId": "value", ... }
                            var answerDict = new Dictionary<string, object>();
                            
                            // answer.Answer is a JToken, check if it's a JObject
                            Newtonsoft.Json.Linq.JObject jObj = null;
                            if (answer.Answer is Newtonsoft.Json.Linq.JObject directJObj)
                            {
                                jObj = directJObj;
                            }
                            else if (answer.Answer is Newtonsoft.Json.Linq.JToken jToken)
                            {
                                // Try to convert JToken to JObject
                                jObj = jToken as Newtonsoft.Json.Linq.JObject ?? jToken.ToObject<Newtonsoft.Json.Linq.JObject>();
                            }

                            if (jObj != null)
                            {
                                // Check if it has "responses" array
                                if (jObj.TryGetValue("responses", out var responsesToken) && responsesToken is Newtonsoft.Json.Linq.JArray responsesArray)
                                {
                                    foreach (var response in responsesArray)
                                    {
                                        if (response is Newtonsoft.Json.Linq.JObject responseObj)
                                        {
                                            var questionId = responseObj["questionId"]?.ToString();
                                            var answerValue = responseObj["answer"]?.ToString();
                                            if (!string.IsNullOrEmpty(questionId))
                                            {
                                                answerDict[questionId] = answerValue ?? string.Empty;
                                            }
                                        }
                                    }
                                    _logger.LogDebug("Parsed questionnaire {QuestionnaireId} responses array with {KeyCount} questions: [{Keys}]", 
                                        questionnaireIdStr, answerDict.Count, string.Join(", ", answerDict.Keys));
                                }
                                else
                                {
                                    // Fallback: try to parse as flat dictionary
                                    foreach (var prop in jObj.Properties())
                                    {
                                        answerDict[prop.Name] = prop.Value?.ToString() ?? string.Empty;
                                    }
                                    _logger.LogDebug("Parsed questionnaire {QuestionnaireId} as flat dictionary with {KeyCount} keys: [{Keys}]", 
                                        questionnaireIdStr, answerDict.Count, string.Join(", ", answerDict.Keys));
                                }
                            }
                            else
                            {
                                // Try to deserialize as dictionary
                                var parsed = answer.Answer.ToObject<Dictionary<string, object>>();
                                if (parsed != null)
                                {
                                    answerDict = parsed;
                                    _logger.LogDebug("Parsed questionnaire {QuestionnaireId} via ToObject with {KeyCount} keys", 
                                        questionnaireIdStr, answerDict.Count);
                                }
                            }

                            if (answerDict.Count > 0)
                            {
                                result.Answers[questionnaireIdStr] = answerDict;
                            }
                        }
                        catch (Exception parseEx)
                        {
                            // If parsing fails, store the raw answer
                            result.Answers[questionnaireIdStr] = answer.Answer.ToString();
                            _logger.LogWarning(parseEx, "Failed to parse questionnaire {QuestionnaireId} answer, stored as string", questionnaireIdStr);
                        }
                    }

                    // Update status if any answer is completed
                    if (!string.IsNullOrEmpty(answer.Status) && answer.Status != "Pending")
                    {
                        result.Status = answer.Status;
                    }
                }

                _logger.LogDebug("Retrieved answers for {AnswerCount} questionnaires for stage {StageId}", 
                    result.Answers.Count, stageId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting questionnaire data for onboarding {OnboardingId}, stage {StageId}", onboardingId, stageId);
                return new QuestionnaireData();
            }
        }

        /// <summary>
        /// Get attachment data for a stage
        /// </summary>
        public async Task<AttachmentData> GetAttachmentDataAsync(long onboardingId, long stageId, string? tenantId = null)
        {
            try
            {
                var result = new AttachmentData();

                // Use provided tenantId or fall back to UserContext
                var effectiveTenantId = tenantId ?? _userContext.TenantId;

                // Get files for this onboarding and stage
                var files = await _db.Queryable<OnboardingFile>()
                    .Where(f => f.OnboardingId == onboardingId && f.StageId == stageId && f.IsValid)
                    .Where(f => f.TenantId == effectiveTenantId)
                    .ToListAsync();

                result.FileCount = files.Count;
                result.TotalSize = files.Sum(f => f.FileSize);
                result.FileNames = files.Select(f => f.OriginalFileName).ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attachment data for onboarding {OnboardingId}, stage {StageId}", onboardingId, stageId);
                return new AttachmentData();
            }
        }

        /// <summary>
        /// Get fields data from onboarding CustomFieldsJson
        /// </summary>
        public async Task<Dictionary<string, object>> GetFieldsDataAsync(long onboardingId, string? tenantId = null)
        {
            try
            {
                // Use GetByIdWithoutTenantFilterAsync to avoid tenant filter issues in background tasks
                var onboarding = await _onboardingRepository.GetByIdWithoutTenantFilterAsync(onboardingId);
                if (onboarding?.CustomFieldsJson == null)
                {
                    return new Dictionary<string, object>();
                }

                // Parse CustomFieldsJson
                var customFields = JsonConvert.DeserializeObject<Dictionary<string, object>>(onboarding.CustomFieldsJson);
                return customFields ?? new Dictionary<string, object>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fields data for onboarding {OnboardingId}", onboardingId);
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Get available components for a stage (for condition configuration UI)
        /// </summary>
        public async Task<List<AvailableComponent>> GetAvailableComponentsAsync(long stageId)
        {
            try
            {
                var result = new List<AvailableComponent>();

                // Get the current stage
                var currentStage = await _stageRepository.GetByIdAsync(stageId);
                if (currentStage == null)
                {
                    return result;
                }

                // Get all stages in the same workflow up to and including current stage
                var stages = await _db.Queryable<Stage>()
                    .Where(s => s.WorkflowId == currentStage.WorkflowId && s.IsValid && s.IsActive)
                    .Where(s => s.Order <= currentStage.Order)
                    .OrderBy(s => s.Order)
                    .ToListAsync();

                foreach (var stage in stages)
                {
                    // Add Fields component (always available)
                    result.Add(new AvailableComponent
                    {
                        ComponentId = stage.Id,
                        ComponentType = "Fields",
                        Name = "Dynamic Fields",
                        StageId = stage.Id,
                        StageName = stage.Name,
                        StageOrder = stage.Order
                    });

                    // Add Checklist if configured
                    if (stage.ChecklistId.HasValue)
                    {
                        result.Add(new AvailableComponent
                        {
                            ComponentId = stage.ChecklistId.Value,
                            ComponentType = "Checklist",
                            Name = "Checklist",
                            StageId = stage.Id,
                            StageName = stage.Name,
                            StageOrder = stage.Order
                        });
                    }

                    // Add Questionnaire if configured
                    if (stage.QuestionnaireId.HasValue)
                    {
                        result.Add(new AvailableComponent
                        {
                            ComponentId = stage.QuestionnaireId.Value,
                            ComponentType = "Questionnaire",
                            Name = "Questionnaire",
                            StageId = stage.Id,
                            StageName = stage.Name,
                            StageOrder = stage.Order
                        });
                    }

                    // Add FileAttachments if configured
                    if (stage.AttachmentManagementNeeded)
                    {
                        result.Add(new AvailableComponent
                        {
                            ComponentId = stage.Id,
                            ComponentType = "FileAttachments",
                            Name = "File Attachments",
                            StageId = stage.Id,
                            StageName = stage.Name,
                            StageOrder = stage.Order
                        });
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available components for stage {StageId}", stageId);
                return new List<AvailableComponent>();
            }
        }

        /// <summary>
        /// Get available fields for a component
        /// </summary>
        public async Task<List<AvailableField>> GetAvailableFieldsAsync(long componentId, string componentType)
        {
            try
            {
                var result = new List<AvailableField>();

                switch (componentType.ToLower())
                {
                    case "fields":
                        // Return common dynamic data fields
                        result.AddRange(new[]
                        {
                            new AvailableField { FieldKey = "status", FieldName = "Status", FieldType = "string", ExpressionPath = "input.fields.status" },
                            new AvailableField { FieldKey = "priority", FieldName = "Priority", FieldType = "string", ExpressionPath = "input.fields.priority" },
                            new AvailableField { FieldKey = "score", FieldName = "Score", FieldType = "number", ExpressionPath = "input.fields.score" }
                        });
                        break;

                    case "checklist":
                        result.AddRange(new[]
                        {
                            new AvailableField { FieldKey = "status", FieldName = "Completion Status", FieldType = "string", ExpressionPath = "input.checklist.status" },
                            new AvailableField { FieldKey = "completedCount", FieldName = "Completed Count", FieldType = "number", ExpressionPath = "input.checklist.completedCount" },
                            new AvailableField { FieldKey = "totalCount", FieldName = "Total Count", FieldType = "number", ExpressionPath = "input.checklist.totalCount" },
                            new AvailableField { FieldKey = "completionPercentage", FieldName = "Completion Percentage", FieldType = "number", ExpressionPath = "input.checklist.completionPercentage" }
                        });
                        break;

                    case "questionnaire":
                        result.AddRange(new[]
                        {
                            new AvailableField { FieldKey = "status", FieldName = "Completion Status", FieldType = "string", ExpressionPath = "input.questionnaire.status" },
                            new AvailableField { FieldKey = "totalScore", FieldName = "Total Score", FieldType = "number", ExpressionPath = "input.questionnaire.totalScore" }
                        });
                        // TODO: Add dynamic question fields based on questionnaire configuration
                        break;

                    case "fileattachments":
                        result.AddRange(new[]
                        {
                            new AvailableField { FieldKey = "fileCount", FieldName = "File Count", FieldType = "number", ExpressionPath = "input.attachments.fileCount" },
                            new AvailableField { FieldKey = "hasAttachment", FieldName = "Has Attachment", FieldType = "boolean", ExpressionPath = "input.attachments.hasAttachment" },
                            new AvailableField { FieldKey = "totalSize", FieldName = "Total Size (bytes)", FieldType = "number", ExpressionPath = "input.attachments.totalSize" }
                        });
                        break;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available fields for component {ComponentId}, type {ComponentType}", componentId, componentType);
                return new List<AvailableField>();
            }
        }
    }
}
