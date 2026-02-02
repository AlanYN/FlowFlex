using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Application.Services.OW.Extensions;
using SqlSugar;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Stage component name synchronization service implementation
    /// High-performance service for updating component names across all related stages
    /// </summary>
    public class StageComponentNameSyncService : IStageComponentNameSyncService, IScopedService
    {
        private readonly ISqlSugarClient _db;
        private readonly IStageRepository _stageRepository;
        private readonly UserContext _userContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StageComponentNameSyncService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
        };

        public StageComponentNameSyncService(
            ISqlSugarClient db,
            IStageRepository stageRepository,
            UserContext userContext,
            IServiceProvider serviceProvider,
            ILogger<StageComponentNameSyncService> logger)
        {
            _db = db;
            _stageRepository = stageRepository;
            _userContext = userContext;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Sync checklist name changes to all related stages
        /// </summary>
        public async Task<int> SyncChecklistNameChangeAsync(long checklistId, string newName)
        {
            try
            {
                // Get all stages that use this checklist through mapping table
                var stageIds = await GetStagesUsingChecklistAsync(checklistId);

                if (!stageIds.Any())
                {
                    return 0;
                }

                // Batch update stages
                return await BatchUpdateStageComponentNames(stageIds, checklistId, newName, isChecklist: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing checklist name change for {ChecklistId}", checklistId);
                throw;
            }
        }

        /// <summary>
        /// Sync questionnaire name changes to all related stages
        /// </summary>
        public async Task<int> SyncQuestionnaireNameChangeAsync(long questionnaireId, string newName)
        {
            try
            {
                // Get all stages that use this questionnaire through mapping table
                var stageIds = await GetStagesUsingQuestionnaireAsync(questionnaireId);

                if (!stageIds.Any())
                {
                    return 0;
                }

                // Batch update stages
                return await BatchUpdateStageComponentNames(stageIds, questionnaireId, newName, isChecklist: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing questionnaire name change for {QuestionnaireId}", questionnaireId);
                throw;
            }
        }

        /// <summary>
        /// Get all stages that use a specific checklist
        /// </summary>
        public async Task<List<long>> GetStagesUsingChecklistAsync(long checklistId)
        {
            try
            {
                var tenantId = _userContext?.TenantId ?? "default";
                var appCode = _userContext?.AppCode ?? "default";
                
                var stageIds = await _db.Queryable<ChecklistStageMapping>()
                    .Where(m => m.ChecklistId == checklistId)
                    .Where(m => m.TenantId == tenantId && m.AppCode == appCode)
                    .Where(m => m.IsValid == true)
                    .Select(m => m.StageId)
                    .ToListAsync();

                return stageIds.Distinct().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting stages using checklist {ChecklistId}", checklistId);
                return new List<long>();
            }
        }

        /// <summary>
        /// Get all stages that use a specific questionnaire
        /// </summary>
        public async Task<List<long>> GetStagesUsingQuestionnaireAsync(long questionnaireId)
        {
            try
            {
                var tenantId = _userContext?.TenantId ?? "default";
                var appCode = _userContext?.AppCode ?? "default";
                
                var stageIds = await _db.Queryable<QuestionnaireStageMapping>()
                    .Where(m => m.QuestionnaireId == questionnaireId)
                    .Where(m => m.TenantId == tenantId && m.AppCode == appCode)
                    .Where(m => m.IsValid == true)
                    .Select(m => m.StageId)
                    .ToListAsync();

                return stageIds.Distinct().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting stages using questionnaire {QuestionnaireId}", questionnaireId);
                return new List<long>();
            }
        }

        /// <summary>
        /// Batch sync multiple checklist name changes
        /// </summary>
        public async Task<int> BatchSyncChecklistNameChangesAsync(Dictionary<long, string> nameChanges)
        {
            if (nameChanges == null || !nameChanges.Any())
                return 0;

            var totalUpdated = 0;
            foreach (var change in nameChanges)
            {
                totalUpdated += await SyncChecklistNameChangeAsync(change.Key, change.Value);
            }

            return totalUpdated;
        }

        /// <summary>
        /// Batch sync multiple questionnaire name changes
        /// </summary>
        public async Task<int> BatchSyncQuestionnaireNameChangesAsync(Dictionary<long, string> nameChanges)
        {
            if (nameChanges == null || !nameChanges.Any())
                return 0;

            var totalUpdated = 0;
            foreach (var change in nameChanges)
            {
                totalUpdated += await SyncQuestionnaireNameChangeAsync(change.Key, change.Value);
            }

            return totalUpdated;
        }

        /// <summary>
        /// Force refresh all component names in a specific stage
        /// </summary>
        public async Task<bool> RefreshStageComponentNamesAsync(long stageId)
        {
            try
            {
                _logger.LogDebug("Refreshing component names for stage {StageId}", stageId);

                var stage = await _stageRepository.GetByIdAsync(stageId);
                if (stage == null)
                {
                    _logger.LogDebug("Stage {StageId} not found", stageId);
                    return false;
                }

                if (string.IsNullOrEmpty(stage.ComponentsJson))
                {
                    _logger.LogDebug("Stage {StageId} has no components", stageId);
                    return true; // No components to refresh
                }

                // Parse existing components
                var components = ParseStageComponents(stage.ComponentsJson);
                if (components == null || !components.Any())
                {
                    _logger.LogDebug("Stage {StageId} has no valid components", stageId);
                    return true;
                }

                var updated = false;

                // Refresh checklist names
                var checklistIds = components
                    .Where(c => c.Key == "checklist")
                    .SelectMany(c => c.ChecklistIds ?? new List<long>())
                    .Distinct()
                    .ToList();

                if (checklistIds.Any())
                {
                    var checklistNameMap = await GetChecklistNameMap(checklistIds);
                    foreach (var component in components.Where(c => c.Key == "checklist"))
                    {
                        if (component.ChecklistIds?.Any() == true)
                        {
                            var newNames = component.ChecklistIds
                                .Select(id => checklistNameMap.TryGetValue(id, out var name) ? name : $"Checklist {id}")
                                .ToList();

                            if (!AreListsEqual(component.ChecklistNames, newNames))
                            {
                                component.ChecklistNames = newNames;
                                updated = true;
                            }
                        }
                    }
                }

                // Refresh questionnaire names
                var questionnaireIds = components
                    .Where(c => c.Key == "questionnaires")
                    .SelectMany(c => c.QuestionnaireIds ?? new List<long>())
                    .Distinct()
                    .ToList();

                if (questionnaireIds.Any())
                {
                    var questionnaireNameMap = await GetQuestionnaireNameMap(questionnaireIds);
                    foreach (var component in components.Where(c => c.Key == "questionnaires"))
                    {
                        if (component.QuestionnaireIds?.Any() == true)
                        {
                            var newNames = component.QuestionnaireIds
                                .Select(id => questionnaireNameMap.TryGetValue(id, out var name) ? name : $"Questionnaire {id}")
                                .ToList();

                            if (!AreListsEqual(component.QuestionnaireNames, newNames))
                            {
                                component.QuestionnaireNames = newNames;
                                updated = true;
                            }
                        }
                    }
                }

                if (updated)
                {
                    // Update stage components
                    stage.Components = components;

                    // CRITICAL: Also update ComponentsJson field since Components is ignored in database mapping
                    stage.ComponentsJson = JsonSerializer.Serialize(components, JsonOptions);

                    stage.InitUpdateInfo(_userContext);
                    await _stageRepository.UpdateAsync(stage);

                    return true;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error refreshing component names for stage {StageId}", stageId);
                return false;
            }
        }

        /// <summary>
        /// Validate and fix any missing or incorrect component names across all stages
        /// </summary>
        public async Task<int> ValidateAndFixAllStageComponentNamesAsync()
        {
            _logger.LogInformation("Starting validation and fix of all stage component names");

            try
            {
                var tenantId = _userContext?.TenantId ?? "default";
                var appCode = _userContext?.AppCode ?? "default";
                
                // Get all stages that have components
                var stages = await _db.Queryable<Stage>()
                    .Where(s => s.TenantId == tenantId && s.AppCode == appCode)
                    .Where(s => s.IsValid == true)
                    .Where(s => !string.IsNullOrEmpty(s.ComponentsJson))
                    .ToListAsync();

                _logger.LogInformation("Found {StageCount} stages with components to validate", stages.Count);

                var fixedCount = 0;
                foreach (var stage in stages)
                {
                    try
                    {
                        var success = await RefreshStageComponentNamesAsync(stage.Id);
                        if (success)
                        {
                            fixedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error fixing stage {StageId}", stage.Id);
                    }
                }

                _logger.LogInformation("Validation and fix completed: {FixedCount} stages processed", fixedCount);
                return fixedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in validate and fix all");
                throw;
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Batch update stage component names for multiple stages
        /// </summary>
        private async Task<int> BatchUpdateStageComponentNames(List<long> stageIds, long componentId, string newName, bool isChecklist)
        {
            if (!stageIds.Any())
                return 0;

            var updatedCount = 0;

            // Process in batches to avoid memory issues
            const int batchSize = 20;
            for (int i = 0; i < stageIds.Count; i += batchSize)
            {
                var batch = stageIds.Skip(i).Take(batchSize).ToList();
                var batchUpdatedCount = await ProcessStageBatch(batch, componentId, newName, isChecklist);
                updatedCount += batchUpdatedCount;
            }

            return updatedCount;
        }

        /// <summary>
        /// Process a batch of stages for component name updates
        /// </summary>
        private async Task<int> ProcessStageBatch(List<long> stageIds, long componentId, string newName, bool isChecklist)
        {
            try
            {
                var tenantId = _userContext?.TenantId ?? "default";
                var appCode = _userContext?.AppCode ?? "default";
                
                // Get all stages in this batch
                var stages = await _db.Queryable<Stage>()
                    .Where(s => stageIds.Contains(s.Id))
                    .Where(s => s.TenantId == tenantId && s.AppCode == appCode)
                    .Where(s => s.IsValid == true)
                    .ToListAsync();

                var stagesToUpdate = new List<Stage>();

                foreach (var stage in stages)
                {
                    if (string.IsNullOrEmpty(stage.ComponentsJson))
                        continue;

                    // Parse components
                    var components = ParseStageComponents(stage.ComponentsJson);
                    if (components == null || !components.Any())
                        continue;

                    var updated = false;

                    // Update names in relevant components
                    if (isChecklist)
                    {
                        foreach (var component in components.Where(c => c.Key == "checklist"))
                        {
                            if (component.ChecklistIds?.Contains(componentId) == true)
                            {
                                var index = component.ChecklistIds.IndexOf(componentId);
                                if (index >= 0)
                                {
                                    // Ensure names list has correct size
                                    component.ChecklistNames ??= new List<string>();
                                    while (component.ChecklistNames.Count <= index)
                                    {
                                        component.ChecklistNames.Add("");
                                    }

                                    // Update the name
                                    if (component.ChecklistNames[index] != newName)
                                    {
                                        component.ChecklistNames[index] = newName;
                                        updated = true;
                                    }
                                }
                            }
                        }
                    }
                    else // questionnaire
                    {
                        foreach (var component in components.Where(c => c.Key == "questionnaires"))
                        {
                            if (component.QuestionnaireIds?.Contains(componentId) == true)
                            {
                                var index = component.QuestionnaireIds.IndexOf(componentId);
                                if (index >= 0)
                                {
                                    // Ensure names list has correct size
                                    component.QuestionnaireNames ??= new List<string>();
                                    while (component.QuestionnaireNames.Count <= index)
                                    {
                                        component.QuestionnaireNames.Add("");
                                    }

                                    // Update the name
                                    if (component.QuestionnaireNames[index] != newName)
                                    {
                                        component.QuestionnaireNames[index] = newName;
                                        updated = true;
                                    }
                                }
                            }
                        }
                    }

                    if (updated)
                    {
                        stage.Components = components;

                        // CRITICAL: Also update ComponentsJson field since Components is ignored in database mapping
                        stage.ComponentsJson = JsonSerializer.Serialize(components, JsonOptions);

                        stage.InitUpdateInfo(_userContext);
                        stagesToUpdate.Add(stage);
                    }
                }

                // Batch update all modified stages
                if (stagesToUpdate.Any())
                {
                    await _db.Updateable(stagesToUpdate).ExecuteCommandAsync();
                }

                return stagesToUpdate.Count;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing stage batch");
                return 0;
            }
        }

        /// <summary>
        /// Parse stage components from JSON string
        /// </summary>
        private List<StageComponent> ParseStageComponents(string componentsJson)
        {
            if (string.IsNullOrEmpty(componentsJson))
                return new List<StageComponent>();

            try
            {
                // Handle double-escaped JSON
                var jsonString = componentsJson;
                if (jsonString.StartsWith("\"") && jsonString.EndsWith("\""))
                {
                    jsonString = JsonSerializer.Deserialize<string>(jsonString) ?? jsonString.Trim('"');
                }

                return JsonSerializer.Deserialize<List<StageComponent>>(jsonString, JsonOptions) ?? new List<StageComponent>();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Error parsing components JSON");
                return new List<StageComponent>();
            }
        }

        /// <summary>
        /// Get checklist name mapping for multiple IDs
        /// </summary>
        private async Task<Dictionary<long, string>> GetChecklistNameMap(List<long> checklistIds)
        {
            try
            {
                // Use service locator to avoid circular dependency
                var checklistService = _serviceProvider.GetRequiredService<IChecklistService>();
                var checklists = await checklistService.GetByIdsAsync(checklistIds);
                return checklists?.ToDictionary(c => c.Id, c => c.Name) ?? new Dictionary<long, string>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting checklist names");
                return new Dictionary<long, string>();
            }
        }

        /// <summary>
        /// Get questionnaire name mapping for multiple IDs
        /// </summary>
        private async Task<Dictionary<long, string>> GetQuestionnaireNameMap(List<long> questionnaireIds)
        {
            try
            {
                // Use service locator to avoid circular dependency
                var questionnaireService = _serviceProvider.GetRequiredService<IQuestionnaireService>();
                var questionnaires = await questionnaireService.GetByIdsAsync(questionnaireIds);
                return questionnaires?.ToDictionary(q => q.Id, q => q.Name) ?? new Dictionary<long, string>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting questionnaire names");
                return new Dictionary<long, string>();
            }
        }

        /// <summary>
        /// Compare two string lists for equality
        /// </summary>
        private bool AreListsEqual(List<string> list1, List<string> list2)
        {
            if (list1 == null && list2 == null) return true;
            if (list1 == null || list2 == null) return false;
            if (list1.Count != list2.Count) return false;

            for (int i = 0; i < list1.Count; i++)
            {
                if (list1[i] != list2[i]) return false;
            }

            return true;
        }

        #endregion
    }
}