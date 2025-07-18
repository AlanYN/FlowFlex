using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Service.OW
{
    /// <summary>
    /// Stage and Checklist/Questionnaire assignments bidirectional sync service implementation
    /// </summary>
    public class StageAssignmentSyncService : IStageAssignmentSyncService, IScopedService
    {
        private readonly IStageRepository _stageRepository;
        private readonly IChecklistRepository _checklistRepository;
        private readonly IQuestionnaireRepository _questionnaireRepository;
        private readonly ILogger<StageAssignmentSyncService> _logger;
        private readonly UserContext _userContext;

        public StageAssignmentSyncService(
            IStageRepository stageRepository,
            IChecklistRepository checklistRepository,
            IQuestionnaireRepository questionnaireRepository,
            ILogger<StageAssignmentSyncService> logger,
            UserContext userContext)
        {
            _stageRepository = stageRepository;
            _checklistRepository = checklistRepository;
            _questionnaireRepository = questionnaireRepository;
            _logger = logger;
            _userContext = userContext;
        }

        /// <summary>
        /// Sync assignments when stage components change
        /// </summary>
        public async Task<bool> SyncAssignmentsFromStageComponentsAsync(
            long stageId,
            long workflowId,
            List<long> oldChecklistIds,
            List<long> newChecklistIds,
            List<long> oldQuestionnaireIds,
            List<long> newQuestionnaireIds)
        {
            try
            {
                var success = true;

                // Sync checklist assignments
                var checklistResult = await SyncChecklistAssignmentsAsync(stageId, workflowId, oldChecklistIds, newChecklistIds);
                if (checklistResult)
                {
                    _logger.LogInformation("Successfully synced checklist assignments for stage {StageId}", stageId);
                }
                else
                {
                    success = false;
                    _logger.LogWarning("Failed to sync checklist assignments for stage {StageId}", stageId);
                }

                // Sync questionnaire assignments
                var questionnaireResult = await SyncQuestionnaireAssignmentsAsync(stageId, workflowId, oldQuestionnaireIds, newQuestionnaireIds);
                if (questionnaireResult)
                {
                    _logger.LogInformation("Successfully synced questionnaire assignments for stage {StageId}", stageId);
                }
                else
                {
                    success = false;
                    _logger.LogWarning("Failed to sync questionnaire assignments for stage {StageId}", stageId);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing assignments from stage components for stage {StageId}", stageId);
                return false;
            }
        }

        /// <summary>
        /// Sync stage components when checklist assignments change
        /// </summary>
        public async Task<bool> SyncStageComponentsFromChecklistAssignmentsAsync(
            long checklistId,
            List<(long workflowId, long stageId)> oldAssignments,
            List<(long workflowId, long stageId)> newAssignments)
        {
            try
            {
                // Get stages that need to be updated
                var stagesToRemove = oldAssignments.Except(newAssignments).Select(a => a.stageId).Distinct().ToList();
                var stagesToAdd = newAssignments.Except(oldAssignments).Select(a => a.stageId).Distinct().ToList();

                var success = true;

                // Remove checklist from stages that are no longer assigned
                foreach (var stageId in stagesToRemove)
                {
                    if (!await RemoveChecklistFromStageComponentsAsync(stageId, checklistId))
                    {
                        success = false;
                        _logger.LogWarning("Failed to remove checklist {ChecklistId} from stage {StageId}", checklistId, stageId);
                    }
                }

                // Add checklist to newly assigned stages
                foreach (var assignment in newAssignments.Where(a => stagesToAdd.Contains(a.stageId)))
                {
                    if (!await AddChecklistToStageComponentsAsync(assignment.stageId, checklistId))
                    {
                        success = false;
                        _logger.LogWarning("Failed to add checklist {ChecklistId} to stage {StageId}", checklistId, assignment.stageId);
                    }
                }

                if (success)
                {
                    _logger.LogInformation("Successfully synced stage components from checklist {ChecklistId} assignments", checklistId);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing stage components from checklist {ChecklistId} assignments", checklistId);
                return false;
            }
        }

        /// <summary>
        /// Sync stage components when questionnaire assignments change
        /// </summary>
        public async Task<bool> SyncStageComponentsFromQuestionnaireAssignmentsAsync(
            long questionnaireId,
            List<(long workflowId, long stageId)> oldAssignments,
            List<(long workflowId, long stageId)> newAssignments)
        {
            try
            {
                // Get stages that need to be updated
                var stagesToRemove = oldAssignments.Except(newAssignments).Select(a => a.stageId).Distinct().ToList();
                var stagesToAdd = newAssignments.Except(oldAssignments).Select(a => a.stageId).Distinct().ToList();

                var success = true;

                // Remove questionnaire from stages that are no longer assigned
                foreach (var stageId in stagesToRemove)
                {
                    if (!await RemoveQuestionnaireFromStageComponentsAsync(stageId, questionnaireId))
                    {
                        success = false;
                        _logger.LogWarning("Failed to remove questionnaire {QuestionnaireId} from stage {StageId}", questionnaireId, stageId);
                    }
                }

                // Add questionnaire to newly assigned stages
                foreach (var assignment in newAssignments.Where(a => stagesToAdd.Contains(a.stageId)))
                {
                    if (!await AddQuestionnaireToStageComponentsAsync(assignment.stageId, questionnaireId))
                    {
                        success = false;
                        _logger.LogWarning("Failed to add questionnaire {QuestionnaireId} to stage {StageId}", questionnaireId, assignment.stageId);
                    }
                }

                if (success)
                {
                    _logger.LogInformation("Successfully synced stage components from questionnaire {QuestionnaireId} assignments", questionnaireId);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing stage components from questionnaire {QuestionnaireId} assignments", questionnaireId);
                return false;
            }
        }

        /// <summary>
        /// Force sync all relationships (maintenance operation)
        /// </summary>
        public async Task<bool> ForceSyncAllRelationshipsAsync()
        {
            try
            {
                _logger.LogInformation("Starting force sync of all stage-checklist-questionnaire relationships");

                // Get all stages with components
                var allStages = await _stageRepository.GetAllOptimizedAsync();
                var success = true;

                foreach (var stage in allStages)
                {
                    if (!string.IsNullOrEmpty(stage.ComponentsJson))
                    {
                        try
                        {
                            var components = JsonSerializer.Deserialize<List<StageComponent>>(stage.ComponentsJson);
                            if (components?.Any() == true)
                            {
                                var checklistIds = components
                                    .Where(c => c.Key == "checklist")
                                    .SelectMany(c => c.ChecklistIds ?? new List<long>())
                                    .Distinct()
                                    .ToList();

                                var questionnaireIds = components
                                    .Where(c => c.Key == "questionnaires")
                                    .SelectMany(c => c.QuestionnaireIds ?? new List<long>())
                                    .Distinct()
                                    .ToList();

                                // Force sync for this stage
                                if (!await SyncAssignmentsFromStageComponentsAsync(
                                    stage.Id, stage.WorkflowId, new List<long>(), checklistIds, new List<long>(), questionnaireIds))
                                {
                                    success = false;
                                    _logger.LogWarning("Failed to force sync for stage {StageId}", stage.Id);
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Invalid JSON in stage {StageId} components", stage.Id);
                        }
                    }
                }

                _logger.LogInformation("Force sync of all relationships completed with success: {Success}", success);
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during force sync of all relationships");
                return false;
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Sync checklist assignments based on stage component changes
        /// </summary>
        private async Task<bool> SyncChecklistAssignmentsAsync(long stageId, long workflowId, List<long> oldChecklistIds, List<long> newChecklistIds)
        {
            try
            {
                oldChecklistIds ??= new List<long>();
                newChecklistIds ??= new List<long>();

                var checklistsToRemove = oldChecklistIds.Except(newChecklistIds).ToList();
                var checklistsToAdd = newChecklistIds.Except(oldChecklistIds).ToList();

                var success = true;

                // Remove assignments for checklists no longer in stage
                foreach (var checklistId in checklistsToRemove)
                {
                    if (!await RemoveAssignmentFromChecklistAsync(checklistId, workflowId, stageId))
                    {
                        success = false;
                    }
                }

                // Add assignments for new checklists in stage
                foreach (var checklistId in checklistsToAdd)
                {
                    if (!await AddAssignmentToChecklistAsync(checklistId, workflowId, stageId))
                    {
                        success = false;
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing checklist assignments for stage {StageId}", stageId);
                return false;
            }
        }

        /// <summary>
        /// Sync questionnaire assignments based on stage component changes
        /// </summary>
        private async Task<bool> SyncQuestionnaireAssignmentsAsync(long stageId, long workflowId, List<long> oldQuestionnaireIds, List<long> newQuestionnaireIds)
        {
            try
            {
                oldQuestionnaireIds ??= new List<long>();
                newQuestionnaireIds ??= new List<long>();

                var questionnairesToRemove = oldQuestionnaireIds.Except(newQuestionnaireIds).ToList();
                var questionnairesToAdd = newQuestionnaireIds.Except(oldQuestionnaireIds).ToList();

                var success = true;

                // Remove assignments for questionnaires no longer in stage
                foreach (var questionnaireId in questionnairesToRemove)
                {
                    if (!await RemoveAssignmentFromQuestionnaireAsync(questionnaireId, workflowId, stageId))
                    {
                        success = false;
                    }
                }

                // Add assignments for new questionnaires in stage
                foreach (var questionnaireId in questionnairesToAdd)
                {
                    if (!await AddAssignmentToQuestionnaireAsync(questionnaireId, workflowId, stageId))
                    {
                        success = false;
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing questionnaire assignments for stage {StageId}", stageId);
                return false;
            }
        }

        /// <summary>
        /// Add assignment to checklist
        /// </summary>
        private async Task<bool> AddAssignmentToChecklistAsync(long checklistId, long workflowId, long stageId)
        {
            try
            {
                var checklist = await _checklistRepository.GetByIdAsync(checklistId);
                if (checklist == null)
                {
                    _logger.LogWarning("Checklist {ChecklistId} not found when adding assignment", checklistId);
                    return false;
                }

                checklist.Assignments ??= new List<AssignmentDto>();

                // Check if exact assignment already exists
                var existingAssignments = checklist.Assignments.Where(a => a.WorkflowId == workflowId && a.StageId == stageId).ToList();
                if (existingAssignments.Any())
                {
                    return true; // Already exists, no need to update
                }

                // Create a clean assignments list and add the new assignment
                var cleanAssignments = checklist.Assignments
                    .Where(a => a.WorkflowId != 0 && a.StageId != 0) // Remove invalid assignments
                    .Where(a => !(a.StageId == stageId && a.WorkflowId != workflowId)) // Remove conflicting stage assignments
                    .ToList();

                // Add new assignment to the clean list
                cleanAssignments.Add(new AssignmentDto
                {
                    WorkflowId = workflowId,
                    StageId = stageId
                });

                // Set the assignments all at once to avoid multiple setter calls
                checklist.Assignments = cleanAssignments;

                return await _checklistRepository.UpdateAsync(checklist);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding assignment to checklist {ChecklistId}", checklistId);
                return false;
            }
        }

        /// <summary>
        /// Remove assignment from checklist
        /// </summary>
        private async Task<bool> RemoveAssignmentFromChecklistAsync(long checklistId, long workflowId, long stageId)
        {
            try
            {
                var checklist = await _checklistRepository.GetByIdAsync(checklistId);
                if (checklist == null)
                {
                    _logger.LogWarning("Checklist {ChecklistId} not found when removing assignment", checklistId);
                    return false;
                }

                if (checklist.Assignments?.Any() != true)
                {
                    return true; // No assignments to remove
                }

                // Remove the assignment
                checklist.Assignments = checklist.Assignments
                    .Where(a => !(a.WorkflowId == workflowId && a.StageId == stageId))
                    .ToList();

                // The Assignments property setter will automatically update AssignmentsJson
                return await _checklistRepository.UpdateAsync(checklist);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing assignment from checklist {ChecklistId}", checklistId);
                return false;
            }
        }

        /// <summary>
        /// Add assignment to questionnaire
        /// </summary>
        private async Task<bool> AddAssignmentToQuestionnaireAsync(long questionnaireId, long workflowId, long stageId)
        {
            try
            {
                var questionnaire = await _questionnaireRepository.GetByIdAsync(questionnaireId);
                if (questionnaire == null)
                {
                    _logger.LogWarning("Questionnaire {QuestionnaireId} not found when adding assignment", questionnaireId);
                    return false;
                }

                questionnaire.Assignments ??= new List<QuestionnaireAssignmentDto>();

                // Check if exact assignment already exists
                var existingAssignments = questionnaire.Assignments.Where(a => a.WorkflowId == workflowId && a.StageId == stageId).ToList();
                if (existingAssignments.Any())
                {
                    return true; // Already exists, no need to update
                }

                // Create a clean assignments list and add the new assignment
                var cleanAssignments = questionnaire.Assignments
                    .Where(a => a.WorkflowId != 0 && a.StageId != 0) // Remove invalid assignments
                    .Where(a => !(a.StageId == stageId && a.WorkflowId != workflowId)) // Remove conflicting stage assignments
                    .ToList();

                // Add new assignment to the clean list
                var newAssignment = new QuestionnaireAssignmentDto
                {
                    WorkflowId = workflowId,
                    StageId = stageId
                };

                cleanAssignments.Add(newAssignment);

                // Set the assignments all at once to avoid multiple setter calls
                questionnaire.Assignments = cleanAssignments;

                return await _questionnaireRepository.UpdateAsync(questionnaire);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding assignment to questionnaire {QuestionnaireId}", questionnaireId);
                return false;
            }
        }

        /// <summary>
        /// Remove assignment from questionnaire
        /// </summary>
        private async Task<bool> RemoveAssignmentFromQuestionnaireAsync(long questionnaireId, long workflowId, long stageId)
        {
            try
            {
                var questionnaire = await _questionnaireRepository.GetByIdAsync(questionnaireId);
                if (questionnaire == null)
                {
                    _logger.LogWarning("Questionnaire {QuestionnaireId} not found when removing assignment", questionnaireId);
                    return false;
                }

                if (questionnaire.Assignments?.Any() != true)
                {
                    return true; // No assignments to remove
                }

                // Remove the assignment
                questionnaire.Assignments = questionnaire.Assignments
                    .Where(a => !(a.WorkflowId == workflowId && a.StageId == stageId))
                    .ToList();

                // The Assignments property setter will automatically update AssignmentsJson
                return await _questionnaireRepository.UpdateAsync(questionnaire);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing assignment from questionnaire {QuestionnaireId}", questionnaireId);
                return false;
            }
        }

        /// <summary>
        /// Add checklist to stage components
        /// </summary>
        private async Task<bool> AddChecklistToStageComponentsAsync(long stageId, long checklistId)
        {
            try
            {
                var stage = await _stageRepository.GetByIdAsync(stageId);
                if (stage == null)
                {
                    _logger.LogWarning("Stage {StageId} not found when adding checklist component", stageId);
                    return false;
                }

                List<StageComponent> components;
                if (string.IsNullOrEmpty(stage.ComponentsJson))
                {
                    components = new List<StageComponent>();
                }
                else
                {
                    try
                    {
                        components = JsonSerializer.Deserialize<List<StageComponent>>(stage.ComponentsJson) ?? new List<StageComponent>();
                    }
                    catch (JsonException)
                    {
                        components = new List<StageComponent>();
                    }
                }

                // Find or create checklist component
                var checklistComponent = components.FirstOrDefault(c => c.Key == "checklist" && c.ChecklistIds.Contains(checklistId));
                if (checklistComponent == null)
                {
                    // Check if there's an existing checklist component with this ID
                    var existingComponent = components.FirstOrDefault(c => c.Key == "checklist" && c.ChecklistIds.Contains(checklistId));
                    if (existingComponent == null)
                    {
                        // Create new component
                        var newOrder = components.Any() ? components.Max(c => c.Order) + 1 : 1;
                        var newComponent = new StageComponent
                        {
                            Key = "checklist",
                            Order = newOrder,
                            IsEnabled = true,
                            StaticFields = new List<string>(),
                            ChecklistIds = new List<long> { checklistId },
                            QuestionnaireIds = new List<long>(),
                            ChecklistNames = new List<string>(),
                            QuestionnaireNames = new List<string>()
                        };

                        // Fill checklist names
                        try
                        {
                            var checklist = await _checklistRepository.GetByIdAsync(checklistId);
                            if (checklist != null)
                            {
                                newComponent.ChecklistNames.Add(checklist.Name);
                            }
                            else
                            {
                                newComponent.ChecklistNames.Add($"Checklist {checklistId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to fetch checklist name for ID {ChecklistId}", checklistId);
                            newComponent.ChecklistNames.Add($"Checklist {checklistId}");
                        }

                        components.Add(newComponent);
                    }
                }

                stage.ComponentsJson = JsonSerializer.Serialize(components);
                stage.Components = components;
                return await _stageRepository.UpdateAsync(stage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding checklist {ChecklistId} to stage {StageId} components", checklistId, stageId);
                return false;
            }
        }

        /// <summary>
        /// Remove checklist from stage components
        /// </summary>
        private async Task<bool> RemoveChecklistFromStageComponentsAsync(long stageId, long checklistId)
        {
            try
            {
                var stage = await _stageRepository.GetByIdAsync(stageId);
                if (stage == null)
                {
                    _logger.LogWarning("Stage {StageId} not found when removing checklist component", stageId);
                    return false;
                }

                if (string.IsNullOrEmpty(stage.ComponentsJson))
                {
                    return true; // No components to update
                }

                List<StageComponent> components;
                try
                {
                    components = JsonSerializer.Deserialize<List<StageComponent>>(stage.ComponentsJson) ?? new List<StageComponent>();
                }
                catch (JsonException)
                {
                    return true; // Invalid JSON, nothing to remove
                }

                // Remove checklist from components and remove empty components
                components = components
                    .Select(c =>
                    {
                        if (c.Key == "checklist" && c.ChecklistIds?.Contains(checklistId) == true)
                        {
                            c.ChecklistIds = c.ChecklistIds.Where(id => id != checklistId).ToList();
                        }
                        return c;
                    })
                    .Where(c => c.Key != "checklist" || c.ChecklistIds?.Any() == true)
                    .ToList();

                stage.ComponentsJson = components.Any() ? JsonSerializer.Serialize(components) : null;
                stage.Components = components;
                return await _stageRepository.UpdateAsync(stage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing checklist {ChecklistId} from stage {StageId} components", checklistId, stageId);
                return false;
            }
        }

        /// <summary>
        /// Add questionnaire to stage components
        /// </summary>
        private async Task<bool> AddQuestionnaireToStageComponentsAsync(long stageId, long questionnaireId)
        {
            try
            {
                var stage = await _stageRepository.GetByIdAsync(stageId);
                if (stage == null)
                {
                    _logger.LogWarning("Stage {StageId} not found when adding questionnaire component", stageId);
                    return false;
                }

                List<StageComponent> components;
                if (string.IsNullOrEmpty(stage.ComponentsJson))
                {
                    components = new List<StageComponent>();
                }
                else
                {
                    try
                    {
                        components = JsonSerializer.Deserialize<List<StageComponent>>(stage.ComponentsJson) ?? new List<StageComponent>();
                    }
                    catch (JsonException)
                    {
                        components = new List<StageComponent>();
                    }
                }

                // Find or create questionnaire component
                var questionnaireComponent = components.FirstOrDefault(c => c.Key == "questionnaires" && c.QuestionnaireIds.Contains(questionnaireId));
                if (questionnaireComponent == null)
                {
                    // Check if there's an existing questionnaire component with this ID
                    var existingComponent = components.FirstOrDefault(c => c.Key == "questionnaires" && c.QuestionnaireIds.Contains(questionnaireId));
                    if (existingComponent == null)
                    {
                        // Create new component
                        var newOrder = components.Any() ? components.Max(c => c.Order) + 1 : 1;
                        var newComponent = new StageComponent
                        {
                            Key = "questionnaires",
                            Order = newOrder,
                            IsEnabled = true,
                            StaticFields = new List<string>(),
                            ChecklistIds = new List<long>(),
                            QuestionnaireIds = new List<long> { questionnaireId },
                            ChecklistNames = new List<string>(),
                            QuestionnaireNames = new List<string>()
                        };

                        // Fill questionnaire names
                        try
                        {
                            var questionnaire = await _questionnaireRepository.GetByIdAsync(questionnaireId);
                            if (questionnaire != null)
                            {
                                newComponent.QuestionnaireNames.Add(questionnaire.Name);
                            }
                            else
                            {
                                newComponent.QuestionnaireNames.Add($"Questionnaire {questionnaireId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to fetch questionnaire name for ID {QuestionnaireId}", questionnaireId);
                            newComponent.QuestionnaireNames.Add($"Questionnaire {questionnaireId}");
                        }

                        components.Add(newComponent);
                    }
                }

                stage.ComponentsJson = JsonSerializer.Serialize(components);
                stage.Components = components;
                return await _stageRepository.UpdateAsync(stage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding questionnaire {QuestionnaireId} to stage {StageId} components", questionnaireId, stageId);
                return false;
            }
        }

        /// <summary>
        /// Remove questionnaire from stage components
        /// </summary>
        private async Task<bool> RemoveQuestionnaireFromStageComponentsAsync(long stageId, long questionnaireId)
        {
            try
            {
                var stage = await _stageRepository.GetByIdAsync(stageId);
                if (stage == null)
                {
                    _logger.LogWarning("Stage {StageId} not found when removing questionnaire component", stageId);
                    return false;
                }

                if (string.IsNullOrEmpty(stage.ComponentsJson))
                {
                    return true; // No components to update
                }

                List<StageComponent> components;
                try
                {
                    components = JsonSerializer.Deserialize<List<StageComponent>>(stage.ComponentsJson) ?? new List<StageComponent>();
                }
                catch (JsonException)
                {
                    return true; // Invalid JSON, nothing to remove
                }

                // Remove questionnaire from components and remove empty components
                components = components
                    .Select(c =>
                    {
                        if (c.Key == "questionnaires" && c.QuestionnaireIds?.Contains(questionnaireId) == true)
                        {
                            c.QuestionnaireIds = c.QuestionnaireIds.Where(id => id != questionnaireId).ToList();
                        }
                        return c;
                    })
                    .Where(c => c.Key != "questionnaires" || c.QuestionnaireIds?.Any() == true)
                    .ToList();

                stage.ComponentsJson = components.Any() ? JsonSerializer.Serialize(components) : null;
                stage.Components = components;
                return await _stageRepository.UpdateAsync(stage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing questionnaire {QuestionnaireId} from stage {StageId} components", questionnaireId, stageId);
                return false;
            }
        }

        #endregion
    }
}