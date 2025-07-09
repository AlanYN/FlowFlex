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
        private readonly UserContext _userContext;

        public QuestionnaireService(
            IQuestionnaireRepository questionnaireRepository,
            IQuestionnaireSectionRepository sectionRepository,
            IMapper mapper,
            UserContext userContext)
        {
            _questionnaireRepository = questionnaireRepository;
            _sectionRepository = sectionRepository;
            _mapper = mapper;
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
                assignments = input.Assignments.Select(a => ((long?)a.WorkflowId, (long?)a.StageId)).ToList();
            }
            else
            {
                // Use single WorkflowId/StageId (backward compatibility)
                assignments.Add((input.WorkflowId, input.StageId));
            }

            // Validate each assignment's uniqueness
            foreach (var (workflowId, stageId) in assignments)
            {
                if (workflowId.HasValue || stageId.HasValue)
                {
                    if (await _questionnaireRepository.IsWorkflowStageAssociationExistsAsync(workflowId, stageId))
                    {
                        var existingQuestionnaire = await _questionnaireRepository.GetByWorkflowStageAssociationAsync(workflowId, stageId);

                        if (workflowId.HasValue && stageId.HasValue)
                        {
                            throw new CRMException(ErrorCodeEnum.BusinessError,
                                $"A questionnaire '{existingQuestionnaire?.Name}' is already associated with Workflow ID {workflowId} and Stage ID {stageId}. Each workflow-stage combination can only have one questionnaire.");
                        }
                        else if (workflowId.HasValue)
                        {
                            throw new CRMException(ErrorCodeEnum.BusinessError,
                                $"A questionnaire '{existingQuestionnaire?.Name}' is already associated with Workflow ID {workflowId}. Each workflow can only have one questionnaire.");
                        }
                        else if (stageId.HasValue)
                        {
                            throw new CRMException(ErrorCodeEnum.BusinessError,
                                $"A questionnaire '{existingQuestionnaire?.Name}' is already associated with Stage ID {stageId}. Each stage can only have one questionnaire.");
                        }
                    }
                }
            }

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
                StageId = a.stageId ?? 0
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

            // Determine assignments to use
            var assignments = new List<(long? workflowId, long? stageId)>();
            
            if (input.Assignments != null && input.Assignments.Any())
            {
                // Use assignments array (new approach)
                assignments = input.Assignments.Select(a => ((long?)a.WorkflowId, (long?)a.StageId)).ToList();
            }
            else
            {
                // Use single WorkflowId/StageId (backward compatibility)
                assignments.Add((input.WorkflowId, input.StageId));
            }

            // Validate each assignment's uniqueness (exclude current record)
            foreach (var (workflowId, stageId) in assignments)
            {
                if (workflowId.HasValue || stageId.HasValue)
                {
                    if (await _questionnaireRepository.IsWorkflowStageAssociationExistsAsync(workflowId, stageId, id))
                    {
                        var existingQuestionnaire = await _questionnaireRepository.GetByWorkflowStageAssociationAsync(workflowId, stageId, id);

                        if (workflowId.HasValue && stageId.HasValue)
                        {
                            throw new CRMException(ErrorCodeEnum.BusinessError,
                                $"A questionnaire '{existingQuestionnaire?.Name}' is already associated with Workflow ID {workflowId} and Stage ID {stageId}. Each workflow-stage combination can only have one questionnaire.");
                        }
                        else if (workflowId.HasValue)
                        {
                            throw new CRMException(ErrorCodeEnum.BusinessError,
                                $"A questionnaire '{existingQuestionnaire?.Name}' is already associated with Workflow ID {workflowId}. Each workflow can only have one questionnaire.");
                        }
                        else if (stageId.HasValue)
                        {
                            throw new CRMException(ErrorCodeEnum.BusinessError,
                                $"A questionnaire '{existingQuestionnaire?.Name}' is already associated with Stage ID {stageId}. Each stage can only have one questionnaire.");
                        }
                    }
                }
            }

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
                StageId = a.stageId ?? 0
            }).ToList();

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

            // Fill assignments for the questionnaire
            await FillAssignmentsAsync(new List<QuestionnaireOutputDto> { result });

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

            var newQuestionnaire = new Questionnaire
            {
                Name = input.Name,
                Description = input.Description ?? sourceQuestionnaire.Description,
                Category = input.Category ?? sourceQuestionnaire.Category,
                Type = input.SetAsTemplate ? "Template" : "Instance",
                Status = "Draft",
                IsTemplate = input.SetAsTemplate,
                TemplateId = input.SetAsTemplate ? null : sourceQuestionnaire.Id,
                StructureJson = input.CopyStructure ? sourceQuestionnaire.StructureJson : null,
                TagsJson = sourceQuestionnaire.TagsJson,
                EstimatedMinutes = sourceQuestionnaire.EstimatedMinutes,
                AllowDraft = sourceQuestionnaire.AllowDraft,
                AllowMultipleSubmissions = sourceQuestionnaire.AllowMultipleSubmissions,
                IsActive = true,
                Version = 1,
                // Copy assignments from source questionnaire
                Assignments = sourceQuestionnaire.Assignments
            };

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
                    await _sectionRepository.InsertAsync(newSection);
                }
            }

            // Calculate question statistics
            await CalculateQuestionStatistics(newQuestionnaire);

            return newQuestionnaire.Id;
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
                
                // Check assignments for stage IDs
                if (questionnaire.Assignments?.Any() == true)
                {
                    foreach (var assignment in questionnaire.Assignments)
                    {
                        if (!groupedQuestionnaires.ContainsKey(assignment.StageId))
                        {
                            groupedQuestionnaires[assignment.StageId] = new List<QuestionnaireOutputDto>();
                        }
                        groupedQuestionnaires[assignment.StageId].Add(mappedQuestionnaire);
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

            // Group questionnaires by name to find all assignments for each questionnaire
            var questionnaireNames = questionnaires.Select(q => q.Name).Distinct().ToList();
            
            // Get all questionnaires with the same names to build assignments
            var allQuestionnaires = await _questionnaireRepository.GetByNamesAsync(questionnaireNames);
            
            // Group by name to build assignments from JSON field
            var assignmentsByName = allQuestionnaires
                .Where(q => q.Assignments?.Any() == true)
                .GroupBy(q => q.Name)
                .ToDictionary(
                    g => g.Key,
                    g => g.SelectMany(q => q.Assignments ?? new List<QuestionnaireAssignmentDto>())
                         .Select(a => new FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto
                         {
                             WorkflowId = a.WorkflowId,
                             StageId = a.StageId
                         })
                         .GroupBy(a => new { a.WorkflowId, a.StageId })
                         .Select(ag => ag.First())
                         .ToList()
                );

            // Fill assignments for each questionnaire
            foreach (var questionnaire in questionnaires)
            {
                if (assignmentsByName.TryGetValue(questionnaire.Name, out var assignments))
                {
                    questionnaire.Assignments = assignments;
                }
                else
                {
                    // If no assignments found, set empty list
                    questionnaire.Assignments = new List<FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto>();
                }
            }
        }
    }
}