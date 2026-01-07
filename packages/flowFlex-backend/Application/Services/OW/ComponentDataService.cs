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
        public async Task<ChecklistData> GetChecklistDataAsync(long onboardingId, long stageId)
        {
            try
            {
                var result = new ChecklistData();

                // Get checklist task completions for this onboarding and stage
                var completions = await _db.Queryable<ChecklistTaskCompletion>()
                    .Where(c => c.OnboardingId == onboardingId && c.IsValid)
                    .Where(c => c.TenantId == _userContext.TenantId)
                    .ToListAsync();

                // Get stage to find associated checklist
                var stage = await _stageRepository.GetByIdAsync(stageId);
                if (stage?.ChecklistId == null)
                {
                    return result;
                }

                // Get checklist tasks
                var tasks = await _db.Queryable<ChecklistTask>()
                    .Where(t => t.ChecklistId == stage.ChecklistId && t.IsValid)
                    .ToListAsync();

                result.TotalCount = tasks.Count;
                result.CompletedCount = completions.Count(c => c.IsCompleted);
                result.Status = result.CompletedCount >= result.TotalCount ? "Completed" : "Pending";

                result.Tasks = tasks.Select(t => new TaskStatusData
                {
                    TaskId = t.Id,
                    Name = t.Name,
                    IsCompleted = completions.Any(c => c.TaskId == t.Id && c.IsCompleted),
                    CompletionNotes = completions.FirstOrDefault(c => c.TaskId == t.Id)?.CompletionNotes
                }).ToList();

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
        public async Task<QuestionnaireData> GetQuestionnaireDataAsync(long onboardingId, long stageId)
        {
            try
            {
                var result = new QuestionnaireData();

                // Get stage to find associated questionnaire
                var stage = await _stageRepository.GetByIdAsync(stageId);
                if (stage?.QuestionnaireId == null)
                {
                    return result;
                }

                // Get questionnaire answer (one answer record per onboarding/questionnaire)
                var answer = await _db.Queryable<QuestionnaireAnswer>()
                    .Where(a => a.OnboardingId == onboardingId && a.QuestionnaireId == stage.QuestionnaireId && a.IsValid)
                    .Where(a => a.TenantId == _userContext.TenantId)
                    .FirstAsync();

                if (answer != null)
                {
                    result.Status = answer.Status;
                    // Parse Answer JToken to dictionary
                    if (answer.Answer != null)
                    {
                        try
                        {
                            var answerDict = answer.Answer.ToObject<Dictionary<string, object>>();
                            if (answerDict != null)
                            {
                                foreach (var kvp in answerDict)
                                {
                                    result.Answers[kvp.Key] = kvp.Value;
                                }
                            }
                        }
                        catch
                        {
                            // If parsing fails, store the raw answer
                            result.Answers["raw_answer"] = answer.Answer.ToString();
                        }
                    }
                }

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
        public async Task<AttachmentData> GetAttachmentDataAsync(long onboardingId, long stageId)
        {
            try
            {
                var result = new AttachmentData();

                // Get files for this onboarding and stage
                var files = await _db.Queryable<OnboardingFile>()
                    .Where(f => f.OnboardingId == onboardingId && f.StageId == stageId && f.IsValid)
                    .Where(f => f.TenantId == _userContext.TenantId)
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
        public async Task<Dictionary<string, object>> GetFieldsDataAsync(long onboardingId)
        {
            try
            {
                var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
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
