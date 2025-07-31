using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Questionnaire;
using FlowFlex.Application.Contracts.Dtos.OW.Common;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;

using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Exceptions;
using System.Text.Json;
using System.Linq;
using System;
// using Item.Redis; // Redis dependency removed
using System.Diagnostics;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Application.Services.OW.Extensions;

namespace FlowFlex.Application.Service.OW
{
    /// <summary>
    /// Questionnaire service implementation
    /// </summary>
    public class QuestionnaireService : IQuestionnaireService, IScopedService
    {
        private readonly IQuestionnaireRepository _questionnaireRepository;
        private readonly IQuestionnaireSectionRepository _sectionRepository;
        private readonly IMapper _mapper;
        private readonly IStageAssignmentSyncService _syncService;
        private readonly UserContext _userContext;

        public QuestionnaireService(
            IQuestionnaireRepository questionnaireRepository,
            IQuestionnaireSectionRepository sectionRepository,
            IMapper mapper,
            IStageAssignmentSyncService syncService,
            UserContext userContext)
        {
            _questionnaireRepository = questionnaireRepository;
            _sectionRepository = sectionRepository;
            _mapper = mapper;
            _syncService = syncService;
            _userContext = userContext;
        }

        public async Task<long> CreateAsync(QuestionnaireInputDto input)
        {
            if (input == null)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input cannot be null");
            }

            // Validate name uniqueness
            if (await _questionnaireRepository.IsNameExistsAsync(input.Name))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Questionnaire name '{input.Name}' already exists");
            }

            // Determine assignments to use
            var assignments = new List<(long? workflowId, long? stageId)>();

            if (input.Assignments != null && input.Assignments.Any())
            {
                // Use assignments array (new approach)
                assignments = input.Assignments.Select(a => (
                    (long?)a.WorkflowId,
                    a.StageId.HasValue && a.StageId.Value > 0 ? a.StageId : null // 处理空StageId
                )).ToList();
            }
            else
            {
                // Use single WorkflowId/StageId (backward compatibility)
                assignments.Add((input.WorkflowId, input.StageId));
            }

            // Note: Removed workflow-stage assignment uniqueness validation
            // Multiple questionnaires can now be associated with the same workflow-stage combination

            // Validate structure JSON
            if (!string.IsNullOrWhiteSpace(input.StructureJson))
            {
                if (!await _questionnaireRepository.ValidateStructureAsync(input.StructureJson))
                {
                    throw new CRMException(ErrorCodeEnum.BusinessError, "Invalid questionnaire structure JSON");
                }
            }

            // Create single questionnaire entity with all assignments
            var entity = _mapper.Map<Questionnaire>(input);

            // Set assignments in JSON format
            entity.Assignments = assignments.Select(a => new QuestionnaireAssignmentDto
            {
                WorkflowId = a.workflowId ?? 0,
                StageId = a.stageId ?? 0 // 允许为0表示空StageId
            }).ToList();

            // Note: WorkflowId and StageId fields have been removed - assignments are now stored in JSON

            // Initialize create information with proper ID and timestamps
            entity.InitCreateInfo(_userContext);

            // Calculate question statistics
            await CalculateQuestionStatistics(entity, input.Sections);

            await _questionnaireRepository.InsertAsync(entity);

            // Create Sections
            if (input.Sections != null && input.Sections.Any())
            {
                await CreateSectionsAsync(entity.Id, input.Sections);
            }

            // Cache removed, no need to clean up

            return entity.Id;
        }

        public async Task<bool> UpdateAsync(long id, QuestionnaireInputDto input)
        {
            if (input == null)
            {
                throw new CRMException(ErrorCodeEnum.ParamInvalid, "Input cannot be null");
            }

            var entity = await _questionnaireRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Questionnaire with ID {id} not found");
            }

            // Validate name uniqueness (exclude current record)
            if (await _questionnaireRepository.IsNameExistsAsync(input.Name, null, id))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Questionnaire name '{input.Name}' already exists");
            }

            // Store old assignments for sync comparison
            var oldAssignments = entity.Assignments?.Where(a => a.StageId > 0)
                .Select(a => (a.WorkflowId, a.StageId))
                .ToList() ?? new List<(long, long)>();

            // Determine assignments to use
            var assignments = new List<(long? workflowId, long? stageId)>();

            if (input.Assignments != null && input.Assignments.Any())
            {
                // Use assignments array (new approach)
                assignments = input.Assignments.Select(a => (
                    (long?)a.WorkflowId,
                    a.StageId.HasValue && a.StageId.Value > 0 ? a.StageId : null // 处理空StageId
                )).ToList();
            }
            else
            {
                // Use single WorkflowId/StageId (backward compatibility)
                assignments.Add((input.WorkflowId, input.StageId));
            }

            // Note: Removed workflow-stage assignment uniqueness validation
            // Multiple questionnaires can now be associated with the same workflow-stage combination

            // Validate structure JSON
            if (!string.IsNullOrWhiteSpace(input.StructureJson))
            {
                if (!await _questionnaireRepository.ValidateStructureAsync(input.StructureJson))
                {
                    throw new CRMException(ErrorCodeEnum.BusinessError, "Invalid questionnaire structure JSON");
                }
            }

            // If questionnaire is published, do not allow structure modification
            if (entity.Status == "Published" && input.StructureJson != entity.StructureJson)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Cannot modify structure of published questionnaire");
            }

            // Update the entity with all assignments
            _mapper.Map(input, entity);

            // Set assignments in JSON format
            entity.Assignments = assignments.Select(a => new QuestionnaireAssignmentDto
            {
                WorkflowId = a.workflowId ?? 0,
                StageId = a.stageId ?? 0 // 允许为0表示空StageId
            }).ToList();

            // Get new assignments for sync comparison (only valid stage assignments)
            var newAssignments = entity.Assignments?.Where(a => a.StageId > 0)
                .Select(a => (a.WorkflowId, a.StageId))
                .ToList() ?? new List<(long, long)>();

            // Note: WorkflowId and StageId fields have been removed - assignments are now stored in JSON

            // Initialize update information with proper timestamps
            entity.InitUpdateInfo(_userContext);

            // Recalculate question statistics
            await CalculateQuestionStatistics(entity, input.Sections);

            var result = await _questionnaireRepository.UpdateAsync(entity);

            // Update sections for the current entity
            if (input.Sections != null)
            {
                await UpdateSectionsAsync(entity.Id, input.Sections);
            }

            // Sync with stage components if update was successful
            if (result)
            {
                try
                {
                    await _syncService.SyncStageComponentsFromQuestionnaireAssignmentsAsync(
                        id,
                        oldAssignments,
                        newAssignments);
                }
                catch (Exception ex)
                {
                    // Log sync error but don't fail the operation
                    Console.WriteLine($"Failed to sync stage components for questionnaire {id}: {ex.Message}");
                }
            }

            // TODO: Clean up any historical duplicate questionnaire records with same name but different assignments
            // This would be similar to what we did for Checklist

            return result;
        }

        public async Task<bool> DeleteAsync(long id, bool confirm = false)
        {
            if (!confirm)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Delete operation requires confirmation");
            }

            var entity = await _questionnaireRepository.GetByIdAsync(id);
            if (entity == null)
            {
                return false;
            }

            // Check if published
            if (entity.Status == "Published")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Cannot delete published questionnaire");
            }

            // Check if there are instances
            if (entity.IsTemplate)
            {
                var instances = await _questionnaireRepository.GetByTemplateIdAsync(id);
                if (instances.Any())
                {
                    throw new CRMException(ErrorCodeEnum.BusinessError, "Cannot delete template with existing instances");
                }
            }

            // Delete related Sections
            await _sectionRepository.DeleteByQuestionnaireIdAsync(id);

            var result = await _questionnaireRepository.DeleteAsync(entity);

            // Cache removed, no need to clean up

            return result;
        }

        /// <summary>
        /// Get questionnaire by ID (optimized version)
        /// </summary>
        public async Task<QuestionnaireOutputDto> GetByIdAsync(long id)
        {
            var entity = await _questionnaireRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Questionnaire with ID {id} not found");
            }

            var result = _mapper.Map<QuestionnaireOutputDto>(entity);

            // Get Sections
            var sections = await _sectionRepository.GetOrderedByQuestionnaireIdAsync(id);
            result.Sections = _mapper.Map<List<QuestionnaireSectionDto>>(sections);

            // 直接从实体获取assignments
            if (entity.Assignments?.Any() == true)
            {
                result.Assignments = entity.Assignments.Select(a => new FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto
                {
                    WorkflowId = a.WorkflowId,
                    StageId = a.StageId > 0 ? a.StageId : null // 处理空StageId，0表示空
                }).ToList();
            }
            else
            {
                result.Assignments = new List<FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto>();
            }

            return result;
        }

        public async Task<List<QuestionnaireOutputDto>> GetListAsync(string category = null)
        {
            var list = await _questionnaireRepository.GetByCategoryAsync(category);
            var result = _mapper.Map<List<QuestionnaireOutputDto>>(list);

            // Batch get all questionnaires' Sections to avoid N+1 query problem
            if (result.Any())
            {
                var questionnaireIds = result.Select(q => q.Id).ToList();
                var allSections = await _sectionRepository.GetByQuestionnaireIdsAsync(questionnaireIds);

                // Group by questionnaire ID
                var sectionsByQuestionnaireId = allSections.GroupBy(s => s.QuestionnaireId)
                    .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Order).ThenBy(s => s.Id).ToList());

                // Assign corresponding Sections to each questionnaire
                foreach (var questionnaire in result)
                {
                    if (sectionsByQuestionnaireId.TryGetValue(questionnaire.Id, out var sections))
                    {
                        questionnaire.Sections = _mapper.Map<List<QuestionnaireSectionDto>>(sections);
                    }
                    else
                    {
                        questionnaire.Sections = new List<QuestionnaireSectionDto>();
                    }
                }
            }

            // Fill assignments for the questionnaires
            await FillAssignmentsAsync(result);

            return result;
        }

        public async Task<List<QuestionnaireOutputDto>> GetByStageIdAsync(long stageId)
        {
            // Add debug log
            // Debug logging handled by structured logging
            // First query all questionnaire records for debugging
            var allQuestionnaires = await _questionnaireRepository.GetListAsync(x => x.IsValid == true);
            // Debug logging handled by structured logging

            // Get questionnaires associated with the specified stage
            var list = await _questionnaireRepository.GetByStageIdAsync(stageId);
            // Debug logging handled by structured logging
            var result = _mapper.Map<List<QuestionnaireOutputDto>>(list);

            // Get Sections for each questionnaire
            foreach (var questionnaire in result)
            {
                var sections = await _sectionRepository.GetOrderedByQuestionnaireIdAsync(questionnaire.Id);
                questionnaire.Sections = _mapper.Map<List<QuestionnaireSectionDto>>(sections);
            }

            // Fill assignments for the questionnaires
            await FillAssignmentsAsync(result);

            return result;
        }

        public async Task<List<QuestionnaireOutputDto>> GetByIdsAsync(List<long> ids)
        {
            if (ids == null || !ids.Any())
            {
                return new List<QuestionnaireOutputDto>();
            }

            var list = await _questionnaireRepository.GetByIdsAsync(ids);
            var result = _mapper.Map<List<QuestionnaireOutputDto>>(list);

            // Batch get all questionnaires' Sections to avoid N+1 query problem
            if (result.Any())
            {
                var questionnaireIds = result.Select(q => q.Id).ToList();
                var allSections = await _sectionRepository.GetByQuestionnaireIdsAsync(questionnaireIds);

                // Group by questionnaire ID
                var sectionsByQuestionnaireId = allSections.GroupBy(s => s.QuestionnaireId)
                    .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Order).ThenBy(s => s.Id).ToList());

                // Assign corresponding Sections to each questionnaire
                foreach (var questionnaire in result)
                {
                    if (sectionsByQuestionnaireId.TryGetValue(questionnaire.Id, out var sections))
                    {
                        questionnaire.Sections = _mapper.Map<List<QuestionnaireSectionDto>>(sections);
                    }
                    else
                    {
                        questionnaire.Sections = new List<QuestionnaireSectionDto>();
                    }
                }
            }

            // Fill assignments for the questionnaires
            await FillAssignmentsAsync(result);

            return result;
        }

        public async Task<PagedResult<QuestionnaireOutputDto>> QueryAsync(QuestionnaireQueryRequest query)
        {
            var (items, totalCount) = await _questionnaireRepository.GetPagedAsync(
                query.PageIndex,
                query.PageSize,
                query.Name,
                query.WorkflowId,
                query.StageId,
                query.IsTemplate,
                query.IsActive,
                query.SortField,
                query.SortDirection);

            var result = _mapper.Map<List<QuestionnaireOutputDto>>(items);

            // Batch get all questionnaires' Sections to avoid N+1 query problem
            if (result.Any())
            {
                var questionnaireIds = result.Select(q => q.Id).ToList();
                var allSections = await _sectionRepository.GetByQuestionnaireIdsAsync(questionnaireIds);

                // Group by questionnaire ID
                var sectionsByQuestionnaireId = allSections.GroupBy(s => s.QuestionnaireId)
                    .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Order).ThenBy(s => s.Id).ToList());

                // Assign corresponding Sections to each questionnaire
                foreach (var questionnaire in result)
                {
                    if (sectionsByQuestionnaireId.TryGetValue(questionnaire.Id, out var sections))
                    {
                        questionnaire.Sections = _mapper.Map<List<QuestionnaireSectionDto>>(sections);
                    }
                    else
                    {
                        questionnaire.Sections = new List<QuestionnaireSectionDto>();
                    }
                }
            }

            // Fill assignments for the questionnaires
            await FillAssignmentsAsync(result);

            return new PagedResult<QuestionnaireOutputDto>
            {
                Items = result,
                TotalCount = totalCount,
                PageIndex = query.PageIndex,
                PageSize = query.PageSize
            };
        }

        public async Task<long> DuplicateAsync(long id, DuplicateQuestionnaireInputDto input)
        {
            var sourceQuestionnaire = await _questionnaireRepository.GetByIdAsync(id);
            if (sourceQuestionnaire == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Source questionnaire with ID {id} not found");
            }

            // Validate new name uniqueness
            if (await _questionnaireRepository.IsNameExistsAsync(input.Name))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Questionnaire name '{input.Name}' already exists");
            }

            // Process StructureJson to generate new IDs
            string newStructureJson = null;
            if (input.CopyStructure && !string.IsNullOrWhiteSpace(sourceQuestionnaire.StructureJson))
            {
                newStructureJson = GenerateNewIdsInStructureJson(sourceQuestionnaire.StructureJson);
            }

            var newQuestionnaire = new Questionnaire
            {
                Name = input.Name,
                Description = input.Description ?? sourceQuestionnaire.Description,
                Category = input.Category ?? sourceQuestionnaire.Category,
                Type = input.SetAsTemplate ? "Template" : "Instance",
                Status = "Draft",
                IsTemplate = input.SetAsTemplate,
                TemplateId = input.SetAsTemplate ? null : sourceQuestionnaire.Id,
                StructureJson = newStructureJson,
                TagsJson = sourceQuestionnaire.TagsJson,
                EstimatedMinutes = sourceQuestionnaire.EstimatedMinutes,
                AllowDraft = sourceQuestionnaire.AllowDraft,
                AllowMultipleSubmissions = sourceQuestionnaire.AllowMultipleSubmissions,
                IsActive = true,
                Version = 1,
                // Copy assignments from source questionnaire
                Assignments = sourceQuestionnaire.Assignments,
                // Copy tenant and app information from source questionnaire
                TenantId = sourceQuestionnaire.TenantId,
                AppCode = sourceQuestionnaire.AppCode
            };

            // Initialize create information with proper ID and timestamps
            newQuestionnaire.InitCreateInfo(_userContext);

            await _questionnaireRepository.InsertAsync(newQuestionnaire);

            // Copy Sections
            if (input.CopyStructure)
            {
                var sourceSections = await _sectionRepository.GetOrderedByQuestionnaireIdAsync(id);
                foreach (var sourceSection in sourceSections)
                {
                    var newSection = new QuestionnaireSection
                    {
                        QuestionnaireId = newQuestionnaire.Id,
                        Title = sourceSection.Title,
                        Description = sourceSection.Description,
                        Order = sourceSection.Order,
                        IsActive = sourceSection.IsActive,
                        Icon = sourceSection.Icon,
                        Color = sourceSection.Color,
                        IsCollapsible = sourceSection.IsCollapsible,
                        IsExpanded = sourceSection.IsExpanded
                    };
                    newSection.InitCreateInfo(_userContext);
                    await _sectionRepository.InsertAsync(newSection);
                }
            }

            // Calculate question statistics
            await CalculateQuestionStatistics(newQuestionnaire);

            return newQuestionnaire.Id;
        }

        /// <summary>
        /// Generate new IDs for sections, questions, and options in StructureJson
        /// </summary>
        private string GenerateNewIdsInStructureJson(string originalStructureJson)
        {
            if (string.IsNullOrWhiteSpace(originalStructureJson))
            {
                return originalStructureJson;
            }

            try
            {
                // Parse the JSON structure
                var structure = JsonSerializer.Deserialize<JsonElement>(originalStructureJson);

                // Generate new structure with new IDs
                var newStructure = GenerateNewIdsInJsonElement(structure);

                // Serialize back to JSON
                return JsonSerializer.Serialize(newStructure, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }
            catch (Exception ex)
            {
                // If JSON parsing fails, return original structure
                // Log the error but don't fail the duplication
                return originalStructureJson;
            }
        }

        /// <summary>
        /// Recursively generate new IDs in JSON structure
        /// </summary>
        private object GenerateNewIdsInJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var obj = new Dictionary<string, object>();
                    foreach (var property in element.EnumerateObject())
                    {
                        var key = property.Name;
                        var value = property.Value;

                        // Generate new ID for specific fields
                        if (key == "id" && value.ValueKind == JsonValueKind.String)
                        {
                            var originalId = value.GetString();
                            if (!string.IsNullOrEmpty(originalId))
                            {
                                // Generate new ID based on the original pattern
                                if (originalId.StartsWith("section-"))
                                {
                                    obj[key] = $"section-{GenerateUniqueTimestamp()}";
                                }
                                else if (originalId.StartsWith("question-"))
                                {
                                    obj[key] = $"question-{GenerateUniqueTimestamp()}";
                                }
                                else if (originalId.StartsWith("option-"))
                                {
                                    obj[key] = $"option-{GenerateUniqueTimestamp()}";
                                }
                                else
                                {
                                    // For other ID patterns, generate a new timestamp-based ID
                                    var prefix = originalId.Contains('-') ? originalId.Split('-')[0] : "item";
                                    obj[key] = $"{prefix}-{GenerateUniqueTimestamp()}";
                                }
                            }
                            else
                            {
                                obj[key] = originalId;
                            }
                        }
                        else
                        {
                            obj[key] = GenerateNewIdsInJsonElement(value);
                        }
                    }
                    return obj;

                case JsonValueKind.Array:
                    var array = new List<object>();
                    foreach (var item in element.EnumerateArray())
                    {
                        array.Add(GenerateNewIdsInJsonElement(item));
                    }
                    return array;

                case JsonValueKind.String:
                    return element.GetString();

                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var intValue))
                        return intValue;
                    if (element.TryGetInt64(out var longValue))
                        return longValue;
                    if (element.TryGetDouble(out var doubleValue))
                        return doubleValue;
                    return element.GetRawText();

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                case JsonValueKind.Null:
                    return null;

                default:
                    return element.GetRawText();
            }
        }

        /// <summary>
        /// Generate unique timestamp for IDs
        /// </summary>
        private static long GenerateUniqueTimestamp()
        {
            // Add some randomness to ensure uniqueness when called multiple times quickly
            var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var random = new Random();
            return timestamp + random.Next(1, 1000);
        }

        /// <summary>
        /// Test method to verify ID generation in structure JSON (for debugging)
        /// </summary>
        public string TestGenerateNewIdsInStructureJson(string originalStructureJson)
        {
            return GenerateNewIdsInStructureJson(originalStructureJson);
        }

        public async Task<QuestionnaireOutputDto> PreviewAsync(long id)
        {
            var entity = await _questionnaireRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Questionnaire with ID {id} not found");
            }

            return _mapper.Map<QuestionnaireOutputDto>(entity);
        }

        public async Task<bool> PublishAsync(long id)
        {
            var entity = await _questionnaireRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Questionnaire with ID {id} not found");
            }

            if (entity.Status == "Published")
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Questionnaire is already published");
            }

            if (string.IsNullOrWhiteSpace(entity.StructureJson))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Cannot publish questionnaire without structure");
            }

            entity.Status = "Published";
            entity.IsActive = true;

            return await _questionnaireRepository.UpdateAsync(entity);
        }

        public async Task<bool> ArchiveAsync(long id)
        {
            var entity = await _questionnaireRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Questionnaire with ID {id} not found");
            }

            entity.Status = "Archived";
            entity.IsActive = false;

            return await _questionnaireRepository.UpdateAsync(entity);
        }

        public async Task<List<QuestionnaireOutputDto>> GetTemplatesAsync()
        {
            var templates = await _questionnaireRepository.GetTemplatesAsync();
            var result = _mapper.Map<List<QuestionnaireOutputDto>>(templates);

            // Fill assignments for the templates
            await FillAssignmentsAsync(result);

            return result;
        }

        public async Task<long> CreateFromTemplateAsync(long templateId, QuestionnaireInputDto input)
        {
            var template = await _questionnaireRepository.GetByIdAsync(templateId);
            if (template == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Template with ID {templateId} not found");
            }

            if (!template.IsTemplate)
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Source questionnaire is not a template");
            }

            // Validate name uniqueness
            if (await _questionnaireRepository.IsNameExistsAsync(input.Name))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, $"Questionnaire name '{input.Name}' already exists");
            }

            var entity = _mapper.Map<Questionnaire>(input);
            entity.Type = "Instance";
            entity.IsTemplate = false;
            entity.TemplateId = templateId;
            entity.StructureJson = template.StructureJson; // Inherit template structure
            entity.Version = 1;
            // Copy tenant and app information from template
            entity.TenantId = template.TenantId;
            entity.AppCode = template.AppCode;

            // Initialize create information with proper ID and timestamps
            entity.InitCreateInfo(_userContext);

            // Calculate question statistics
            await CalculateQuestionStatistics(entity);

            await _questionnaireRepository.InsertAsync(entity);

            return entity.Id;
        }

        public async Task<bool> ValidateStructureAsync(long id)
        {
            var entity = await _questionnaireRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Questionnaire with ID {id} not found");
            }

            return await _questionnaireRepository.ValidateStructureAsync(entity.StructureJson);
        }

        public async Task<bool> UpdateStatisticsAsync(long id)
        {
            var entity = await _questionnaireRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Questionnaire with ID {id} not found");
            }

            await CalculateQuestionStatistics(entity);

            return await _questionnaireRepository.UpdateStatisticsAsync(id, entity.TotalQuestions, entity.RequiredQuestions);
        }

        private async Task CalculateQuestionStatistics(Questionnaire questionnaire, List<QuestionnaireSectionInputDto> sections = null)
        {
            if (string.IsNullOrWhiteSpace(questionnaire.StructureJson))
            {
                questionnaire.TotalQuestions = 0;
                questionnaire.RequiredQuestions = 0;
                return;
            }

            try
            {
                var structure = JsonDocument.Parse(questionnaire.StructureJson);

                // Question statistics logic - future enhancement
                // This needs to be calculated based on the actual questionnaire structure JSON format
                // Example logic:
                var totalQuestions = 0;
                var requiredQuestions = 0;

                if (structure.RootElement.TryGetProperty("sections", out var sectionsElement))
                {
                    foreach (var section in sectionsElement.EnumerateArray())
                    {
                        if (section.TryGetProperty("questions", out var questions))
                        {
                            foreach (var question in questions.EnumerateArray())
                            {
                                totalQuestions++;
                                if (question.TryGetProperty("required", out var required) && required.GetBoolean())
                                {
                                    requiredQuestions++;
                                }
                            }
                        }
                    }
                }

                questionnaire.TotalQuestions = totalQuestions;
                questionnaire.RequiredQuestions = requiredQuestions;
            }
            catch (JsonException)
            {
                // If JSON parsing fails, set to 0
                questionnaire.TotalQuestions = 0;
                questionnaire.RequiredQuestions = 0;
            }

            await Task.CompletedTask;
        }

        private async Task CreateSectionsAsync(long questionnaireId, List<QuestionnaireSectionInputDto> sections)
        {
            foreach (var section in sections)
            {
                var newSection = _mapper.Map<QuestionnaireSection>(section);
                newSection.QuestionnaireId = questionnaireId;
                await _sectionRepository.InsertAsync(newSection);
            }
        }

        private async Task UpdateSectionsAsync(long questionnaireId, List<QuestionnaireSectionInputDto> sections)
        {
            await _sectionRepository.DeleteByQuestionnaireIdAsync(questionnaireId);
            await CreateSectionsAsync(questionnaireId, sections);
        }

        // Cache method removed - Redis dependency removed

        public async Task<BatchStageQuestionnaireResponse> GetByStageIdsBatchAsync(BatchStageQuestionnaireRequest request)
        {
            var response = new BatchStageQuestionnaireResponse();

            if (request.StageIds == null || !request.StageIds.Any())
            {
                return response;
            }

            // Batch query all questionnaires for all stages
            var allQuestionnaires = await _questionnaireRepository.GetByStageIdsAsync(request.StageIds);

            // Group by Stage ID from assignments
            var groupedQuestionnaires = new Dictionary<long, List<QuestionnaireOutputDto>>();

            foreach (var questionnaire in allQuestionnaires)
            {
                var mappedQuestionnaire = _mapper.Map<QuestionnaireOutputDto>(questionnaire);
                
                // 直接将实体的assignments映射到DTO，避免后续的FillAssignmentsAsync方法
                if (questionnaire.Assignments?.Any() == true)
                {
                    mappedQuestionnaire.Assignments = questionnaire.Assignments.Select(a => new FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto
                    {
                        WorkflowId = a.WorkflowId,
                        StageId = a.StageId > 0 ? a.StageId : null // 处理空StageId，0表示空
                    }).ToList();
                }
                else
                {
                    mappedQuestionnaire.Assignments = new List<FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto>();
                }

                // Check assignments for stage IDs
                if (questionnaire.Assignments?.Any() == true)
                {
                    foreach (var assignment in questionnaire.Assignments)
                    {
                        // 只处理有效的StageId（大于0）
                        if (assignment.StageId > 0)
                        {
                            if (!groupedQuestionnaires.ContainsKey(assignment.StageId))
                            {
                                groupedQuestionnaires[assignment.StageId] = new List<QuestionnaireOutputDto>();
                            }
                            groupedQuestionnaires[assignment.StageId].Add(mappedQuestionnaire);
                        }
                    }
                }
            }

            // Ensure all requested Stage IDs have corresponding results (even if empty lists)
            foreach (var stageId in request.StageIds)
            {
                response.StageQuestionnaires[stageId] = groupedQuestionnaires.ContainsKey(stageId)
                    ? groupedQuestionnaires[stageId]
                    : new List<QuestionnaireOutputDto>();
            }

            return response;
        }

        /// <summary>
        /// Fill assignments for questionnaire output DTOs
        /// </summary>
        private async Task FillAssignmentsAsync(List<QuestionnaireOutputDto> questionnaires)
        {
            if (questionnaires == null || !questionnaires.Any())
                return;

            // 获取所有问卷的ID
            var questionnaireIds = questionnaires.Select(q => q.Id).ToList();
            
            // 直接通过ID获取问卷，避免通过名称查询导致的assignments合并问题
            var allQuestionnaires = await _questionnaireRepository.GetByIdsAsync(questionnaireIds);
            
            // 按ID索引问卷，确保每个问卷只获取自己的assignments
            var questionnaireById = allQuestionnaires.ToDictionary(q => q.Id);
            
            // 为每个问卷填充其自身的assignments
            foreach (var questionnaire in questionnaires)
            {
                if (questionnaireById.TryGetValue(questionnaire.Id, out var entity) && entity.Assignments?.Any() == true)
                {
                    questionnaire.Assignments = entity.Assignments.Select(a => new FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto
                    {
                        WorkflowId = a.WorkflowId,
                        StageId = a.StageId > 0 ? a.StageId : null // 处理空StageId，0表示空
                    }).ToList();
                }
                else
                {
                    // 如果没有找到对应的问卷或assignments为空，则设置为空列表
                    questionnaire.Assignments = new List<FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto>();
                }
            }
        }
    }
}