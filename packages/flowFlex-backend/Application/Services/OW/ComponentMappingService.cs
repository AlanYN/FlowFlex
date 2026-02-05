using System.Text.Json;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Helpers;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Application.Services.OW.Extensions;
using SqlSugar;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Component mapping synchronization service
    /// </summary>
    public class ComponentMappingService : IComponentMappingService
    {
        private readonly ISqlSugarClient _db;
        private readonly IStageRepository _stageRepository;
        private readonly UserContext _userContext;
        private readonly IStageComponentNameSyncService _nameSync;
        private readonly ILogger<ComponentMappingService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
        };

        public ComponentMappingService(
            ISqlSugarClient db, 
            IStageRepository stageRepository, 
            UserContext userContext, 
            ILogger<ComponentMappingService> logger,
            IStageComponentNameSyncService nameSync = null)
        {
            _db = db;
            _stageRepository = stageRepository;
            _userContext = userContext;
            _logger = logger;
            _nameSync = nameSync; // Optional to avoid circular dependency
        }

        /// <summary>
        /// Sync mappings for all stages in a workflow
        /// </summary>
        public async Task SyncWorkflowMappingsAsync(long workflowId)
        {
            try
            {
                _logger.LogInformation("Syncing mappings for workflow {WorkflowId}", workflowId);

                // Get all stages in the workflow
                var stages = await _stageRepository.GetListAsync(s => s.WorkflowId == workflowId && s.IsValid);

                foreach (var stage in stages)
                {
                    await SyncStageMappingsAsync(stage.Id);
                }

                _logger.LogInformation("Completed syncing mappings for workflow {WorkflowId} - {StageCount} stages processed", workflowId, stages.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing workflow {WorkflowId} mappings", workflowId);
                throw;
            }
        }

        /// <summary>
        /// Sync mappings for a specific stage
        /// </summary>
        public async Task SyncStageMappingsAsync(long stageId)
        {
            var stage = await _stageRepository.GetByIdAsync(stageId);
            if (stage == null) return;

            _logger.LogDebug("Syncing mappings for stage {StageId}", stageId);

            // Delete existing mappings for this stage
            await _db.Deleteable<QuestionnaireStageMapping>()
                .Where(m => m.StageId == stageId)
                .ExecuteCommandAsync();

            await _db.Deleteable<ChecklistStageMapping>()
                .Where(m => m.StageId == stageId)
                .ExecuteCommandAsync();

            // Parse and create new mappings
            if (!string.IsNullOrEmpty(stage.ComponentsJson))
            {
                try
                {
                    var normalized = TryUnwrapComponentsJson(stage.ComponentsJson);
                    var components = JsonSerializer.Deserialize<List<StageComponent>>(normalized, JsonOptions);

                    foreach (var component in components ?? new List<StageComponent>())
                    {
                        // Sync questionnaire mappings
                        if (component.Key == "questionnaires" && component.QuestionnaireIds?.Any() == true)
                        {
                            var questionnaireMappings = component.QuestionnaireIds.Select(qId =>
                            {
                                var mapping = new QuestionnaireStageMapping
                                {
                                    QuestionnaireId = qId,
                                    StageId = stage.Id,
                                    WorkflowId = stage.WorkflowId,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                };

                                // Initialize snowflake ID and entity info like Stage does
                                mapping.InitCreateInfo(_userContext);

                                return mapping;
                            }).ToList();

                            await _db.Insertable(questionnaireMappings).ExecuteCommandAsync();
                            _logger.LogDebug("Synced {Count} questionnaire mappings for stage {StageId}", questionnaireMappings.Count, stageId);
                        }

                        // Sync checklist mappings
                        if (component.Key == "checklist" && component.ChecklistIds?.Any() == true)
                        {
                            var checklistMappings = component.ChecklistIds.Select(cId =>
                            {
                                var mapping = new ChecklistStageMapping
                                {
                                    ChecklistId = cId,
                                    StageId = stage.Id,
                                    WorkflowId = stage.WorkflowId,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                };

                                // Initialize snowflake ID and entity info like Stage does
                                mapping.InitCreateInfo(_userContext);

                                return mapping;
                            }).ToList();

                            await _db.Insertable(checklistMappings).ExecuteCommandAsync();
                            _logger.LogDebug("Synced {Count} checklist mappings for stage {StageId}", checklistMappings.Count, stageId);
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Error parsing ComponentsJson for stage {StageId}", stageId);
                }
            }
        }

        /// <summary>
        /// Sync all stage mappings (for initial data migration)
        /// </summary>
        public async Task SyncAllStageMappingsAsync()
        {
            _logger.LogInformation("Starting full sync of all stage mappings");

            var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
            var allStages = await _stageRepository.GetListAsync(s => s.TenantId == tenantId && s.IsValid);
            var totalStages = allStages.Count;
            var processedStages = 0;

            foreach (var stage in allStages)
            {
                await SyncStageMappingsAsync(stage.Id);
                processedStages++;

                if (processedStages % 10 == 0)
                {
                    _logger.LogInformation("Progress: {ProcessedStages}/{TotalStages} stages processed", processedStages, totalStages);
                }
            }

            _logger.LogInformation("Full sync completed: {ProcessedStages} stages processed", processedStages);
        }

        /// <summary>
        /// Get questionnaire assignments from mapping table (ultra-fast)
        /// </summary>
        public async Task<Dictionary<long, List<(long WorkflowId, long StageId)>>> GetQuestionnaireAssignmentsAsync(List<long> questionnaireIds)
        {
            var result = new Dictionary<long, List<(long, long)>>();

            if (!questionnaireIds.Any())
                return result;

            try
            {
                _logger.LogDebug("Getting assignments from mapping table for {Count} questionnaires", questionnaireIds.Count);

                var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
                var appCode = TenantContextHelper.GetAppCodeOrDefault(_userContext);

                var mappings = await _db.Queryable<QuestionnaireStageMapping>()
                    .Where(m => questionnaireIds.Contains(m.QuestionnaireId))
                    .Where(m => m.TenantId == tenantId && m.AppCode == appCode)
                    .Where(m => m.IsValid == true)
                    .ToListAsync();

                _logger.LogDebug("Found {Count} mappings from mapping table", mappings.Count);

                foreach (var questionnaireId in questionnaireIds)
                {
                    result[questionnaireId] = mappings
                        .Where(m => m.QuestionnaireId == questionnaireId)
                        .Select(m => (m.WorkflowId, m.StageId))
                        .ToList();
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting questionnaire assignments");
                return result;
            }
        }

        /// <summary>
        /// Get checklist assignments from mapping table (ultra-fast)
        /// </summary>
        public async Task<Dictionary<long, List<(long WorkflowId, long StageId)>>> GetChecklistAssignmentsAsync(List<long> checklistIds)
        {
            var result = new Dictionary<long, List<(long, long)>>();

            if (!checklistIds.Any())
                return result;

            try
            {
                _logger.LogDebug("Getting checklist assignments from mapping table for {Count} checklists", checklistIds.Count);

                var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
                var appCode = TenantContextHelper.GetAppCodeOrDefault(_userContext);

                var mappings = await _db.Queryable<ChecklistStageMapping>()
                    .Where(m => checklistIds.Contains(m.ChecklistId))
                    .Where(m => m.TenantId == tenantId && m.AppCode == appCode)
                    .Where(m => m.IsValid == true)
                    .ToListAsync();

                _logger.LogDebug("Found {Count} checklist mappings from mapping table", mappings.Count);

                foreach (var checklistId in checklistIds)
                {
                    result[checklistId] = mappings
                        .Where(m => m.ChecklistId == checklistId)
                        .Select(m => (m.WorkflowId, m.StageId))
                        .ToList();
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting checklist assignments");
                return result;
            }
        }

        /// <summary>
        /// Get questionnaire IDs by workflow and/or stage from mapping table (ultra-fast)
        /// </summary>
        public async Task<List<long>> GetQuestionnaireIdsByWorkflowStageAsync(long? workflowId = null, long? stageId = null)
        {
            try
            {
                _logger.LogDebug("Getting questionnaire IDs from mapping table - WorkflowId: {WorkflowId}, StageId: {StageId}", workflowId, stageId);

                var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
                var appCode = TenantContextHelper.GetAppCodeOrDefault(_userContext);

                var query = _db.Queryable<QuestionnaireStageMapping>()
                    .Where(m => m.TenantId == tenantId && m.AppCode == appCode)
                    .Where(m => m.IsValid == true);

                if (workflowId.HasValue)
                {
                    query = query.Where(m => m.WorkflowId == workflowId.Value);
                }

                if (stageId.HasValue)
                {
                    query = query.Where(m => m.StageId == stageId.Value);
                }

                var mappings = await query.ToListAsync();
                var questionnaireIds = mappings.Select(m => m.QuestionnaireId).Distinct().ToList();

                _logger.LogDebug("Found {Count} distinct questionnaire IDs from mapping table", questionnaireIds.Count);
                return questionnaireIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting questionnaire IDs by workflow/stage");
                return new List<long>();
            }
        }

        /// <summary>
        /// Get checklist IDs by workflow and/or stage from mapping table (ultra-fast)
        /// </summary>
        public async Task<List<long>> GetChecklistIdsByWorkflowStageAsync(long? workflowId = null, long? stageId = null)
        {
            try
            {
                _logger.LogDebug("Getting checklist IDs from mapping table - WorkflowId: {WorkflowId}, StageId: {StageId}", workflowId, stageId);

                var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
                var appCode = TenantContextHelper.GetAppCodeOrDefault(_userContext);

                var query = _db.Queryable<ChecklistStageMapping>()
                    .Where(m => m.TenantId == tenantId && m.AppCode == appCode)
                    .Where(m => m.IsValid == true);

                if (workflowId.HasValue)
                {
                    query = query.Where(m => m.WorkflowId == workflowId.Value);
                }

                if (stageId.HasValue)
                {
                    query = query.Where(m => m.StageId == stageId.Value);
                }

                var mappings = await query.ToListAsync();
                var checklistIds = mappings.Select(m => m.ChecklistId).Distinct().ToList();

                _logger.LogDebug("Found {Count} distinct checklist IDs from mapping table", checklistIds.Count);
                return checklistIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting checklist IDs by workflow/stage");
                return new List<long>();
            }
        }

        /// <summary>
        /// Sync stage mappings within transaction (for data consistency)
        /// </summary>
        public async Task SyncStageMappingsInTransactionAsync(long stageId, ISqlSugarClient transaction)
        {
            var stage = await transaction.Queryable<Stage>().Where(s => s.Id == stageId).FirstAsync();
            if (stage == null) return;

            _logger.LogDebug("Syncing mappings for stage {StageId} within transaction", stageId);

            // Delete existing mappings for this stage within transaction
            await transaction.Deleteable<QuestionnaireStageMapping>()
                .Where(m => m.StageId == stageId)
                .ExecuteCommandAsync();

            await transaction.Deleteable<ChecklistStageMapping>()
                .Where(m => m.StageId == stageId)
                .ExecuteCommandAsync();

            // Parse and create new mappings
            if (!string.IsNullOrEmpty(stage.ComponentsJson))
            {
                try
                {
                    var normalized = TryUnwrapComponentsJson(stage.ComponentsJson);
                    var components = JsonSerializer.Deserialize<List<StageComponent>>(normalized, JsonOptions);

                    foreach (var component in components ?? new List<StageComponent>())
                    {
                        // Sync questionnaire mappings
                        if (component.Key == "questionnaires" && component.QuestionnaireIds?.Any() == true)
                        {
                            var questionnaireMappings = component.QuestionnaireIds.Select(qId =>
                            {
                                var mapping = new QuestionnaireStageMapping
                                {
                                    QuestionnaireId = qId,
                                    StageId = stage.Id,
                                    WorkflowId = stage.WorkflowId,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                };

                                // Initialize snowflake ID and entity info
                                mapping.InitCreateInfo(_userContext);

                                return mapping;
                            }).ToList();

                            await transaction.Insertable(questionnaireMappings).ExecuteCommandAsync();
                            _logger.LogDebug("Synced {Count} questionnaire mappings for stage {StageId} in transaction", questionnaireMappings.Count, stageId);
                        }

                        // Sync checklist mappings
                        if (component.Key == "checklist" && component.ChecklistIds?.Any() == true)
                        {
                            var checklistMappings = component.ChecklistIds.Select(cId =>
                            {
                                var mapping = new ChecklistStageMapping
                                {
                                    ChecklistId = cId,
                                    StageId = stage.Id,
                                    WorkflowId = stage.WorkflowId,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                };

                                // Initialize snowflake ID and entity info
                                mapping.InitCreateInfo(_userContext);

                                return mapping;
                            }).ToList();

                            await transaction.Insertable(checklistMappings).ExecuteCommandAsync();
                            _logger.LogDebug("Synced {Count} checklist mappings for stage {StageId} in transaction", checklistMappings.Count, stageId);
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error parsing ComponentsJson for stage {StageId} in transaction", stageId);
                    throw; // Re-throw to rollback transaction
                }
            }
        }

        /// <summary>
        /// Validate data consistency between stage components and mappings
        /// </summary>
        public async Task<bool> ValidateStageComponentConsistencyAsync(long stageId)
        {
            try
            {
                var stage = await _stageRepository.GetByIdAsync(stageId);
                if (stage == null) return false;

                // Get IDs from stage components
                var componentChecklistIds = new List<long>();
                var componentQuestionnaireIds = new List<long>();

                if (!string.IsNullOrEmpty(stage.ComponentsJson))
                {
                    try
                    {
                        var normalized = TryUnwrapComponentsJson(stage.ComponentsJson);
                        var components = JsonSerializer.Deserialize<List<StageComponent>>(normalized, JsonOptions);

                        componentChecklistIds = components?
                            .Where(c => c.Key == "checklist")
                            .SelectMany(c => c.ChecklistIds ?? new List<long>())
                            .Distinct()
                            .OrderBy(x => x)
                            .ToList() ?? new List<long>();

                        componentQuestionnaireIds = components?
                            .Where(c => c.Key == "questionnaires")
                            .SelectMany(c => c.QuestionnaireIds ?? new List<long>())
                            .Distinct()
                            .OrderBy(x => x)
                            .ToList() ?? new List<long>();
                    }
                    catch (JsonException)
                    {
                        _logger.LogWarning("Invalid ComponentsJson for stage {StageId}", stageId);
                        return false;
                    }
                }

                // Get IDs from mapping tables
                var mappingChecklistIds = await _db.Queryable<ChecklistStageMapping>()
                    .Where(m => m.StageId == stageId && m.IsValid == true)
                    .Select(m => m.ChecklistId)
                    .ToListAsync();
                mappingChecklistIds = mappingChecklistIds.Distinct().OrderBy(x => x).ToList();

                var mappingQuestionnaireIds = await _db.Queryable<QuestionnaireStageMapping>()
                    .Where(m => m.StageId == stageId && m.IsValid == true)
                    .Select(m => m.QuestionnaireId)
                    .ToListAsync();
                mappingQuestionnaireIds = mappingQuestionnaireIds.Distinct().OrderBy(x => x).ToList();

                // Compare collections
                var checklistsMatch = componentChecklistIds.SequenceEqual(mappingChecklistIds);
                var questionnairesMatch = componentQuestionnaireIds.SequenceEqual(mappingQuestionnaireIds);

                if (!checklistsMatch || !questionnairesMatch)
                {
                    _logger.LogWarning("Data inconsistency detected for stage {StageId}: ComponentChecklists=[{ComponentChecklists}], MappingChecklists=[{MappingChecklists}], ComponentQuestionnaires=[{ComponentQuestionnaires}], MappingQuestionnaires=[{MappingQuestionnaires}]",
                        stageId,
                        string.Join(", ", componentChecklistIds),
                        string.Join(", ", mappingChecklistIds),
                        string.Join(", ", componentQuestionnaireIds),
                        string.Join(", ", mappingQuestionnaireIds));
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating consistency for stage {StageId}", stageId);
                return false;
            }
        }

        /// <summary>
        /// Check if stage mappings need synchronization
        /// </summary>
        public async Task<bool> NeedsSyncAsync(long stageId, List<long> newChecklistIds, List<long> newQuestionnaireIds)
        {
            try
            {
                // Get current mapping IDs
                var currentChecklistIds = await _db.Queryable<ChecklistStageMapping>()
                    .Where(m => m.StageId == stageId && m.IsValid == true)
                    .Select(m => m.ChecklistId)
                    .ToListAsync();
                currentChecklistIds = currentChecklistIds.Distinct().OrderBy(x => x).ToList();

                var currentQuestionnaireIds = await _db.Queryable<QuestionnaireStageMapping>()
                    .Where(m => m.StageId == stageId && m.IsValid == true)
                    .Select(m => m.QuestionnaireId)
                    .ToListAsync();
                currentQuestionnaireIds = currentQuestionnaireIds.Distinct().OrderBy(x => x).ToList();

                // Sort new IDs for comparison
                newChecklistIds = newChecklistIds?.Distinct().OrderBy(x => x).ToList() ?? new List<long>();
                newQuestionnaireIds = newQuestionnaireIds?.Distinct().OrderBy(x => x).ToList() ?? new List<long>();

                // Compare collections
                var checklistsChanged = !currentChecklistIds.SequenceEqual(newChecklistIds);
                var questionnairesChanged = !currentQuestionnaireIds.SequenceEqual(newQuestionnaireIds);

                return checklistsChanged || questionnairesChanged;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking sync needs for stage {StageId}", stageId);
                return true; // Assume sync is needed on error
            }
        }

        private static string TryUnwrapComponentsJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return json;
            var current = json.Trim();
            if (current.StartsWith("[") || current.StartsWith("{")) return current;

            try
            {
                var inner = JsonSerializer.Deserialize<string>(current);
                if (!string.IsNullOrWhiteSpace(inner))
                {
                    inner = inner.Trim();
                    if (inner.StartsWith("[") || inner.StartsWith("{")) return inner;
                }
            }
            catch (JsonException)
            {
                // Not valid JSON string, return current
            }
            return current;
        }

        /// <summary>
        /// Notify component name changes to sync service
        /// High-performance method to trigger stage component name updates
        /// </summary>
        public async Task NotifyChecklistNameChangeAsync(long checklistId, string newName)
        {
            if (_nameSync != null)
            {
                try
                {
                    await _nameSync.SyncChecklistNameChangeAsync(checklistId, newName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error in checklist name sync notification for checklist {ChecklistId}", checklistId);
                    // Don't throw to avoid breaking the main operation
                }
            }
        }

        /// <summary>
        /// Notify component name changes to sync service
        /// High-performance method to trigger stage component name updates
        /// </summary>
        public async Task NotifyQuestionnaireNameChangeAsync(long questionnaireId, string newName)
        {
            if (_nameSync != null)
            {
                try
                {
                    await _nameSync.SyncQuestionnaireNameChangeAsync(questionnaireId, newName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error in questionnaire name sync notification for questionnaire {QuestionnaireId}", questionnaireId);
                    // Don't throw to avoid breaking the main operation
                }
            }
        }

        /// <summary>
        /// Batch notify multiple component name changes for better performance
        /// </summary>
        public async Task BatchNotifyChecklistNameChangesAsync(Dictionary<long, string> nameChanges)
        {
            if (_nameSync != null && nameChanges?.Any() == true)
            {
                try
                {
                    await _nameSync.BatchSyncChecklistNameChangesAsync(nameChanges);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error in batch checklist name sync notification");
                    // Don't throw to avoid breaking the main operation
                }
            }
        }

        /// <summary>
        /// Batch notify multiple component name changes for better performance
        /// </summary>
        public async Task BatchNotifyQuestionnaireNameChangesAsync(Dictionary<long, string> nameChanges)
        {
            if (_nameSync != null && nameChanges?.Any() == true)
            {
                try
                {
                    await _nameSync.BatchSyncQuestionnaireNameChangesAsync(nameChanges);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error in batch questionnaire name sync notification");
                    // Don't throw to avoid breaking the main operation
                }
            }
        }

        /// <summary>
        /// Get all stages that use a specific checklist (delegated to name sync service)
        /// </summary>
        public async Task<List<long>> GetStagesUsingChecklistFastAsync(long checklistId)
        {
            if (_nameSync != null)
            {
                return await _nameSync.GetStagesUsingChecklistAsync(checklistId);
            }

            // Fallback to direct mapping query
            return await GetChecklistIdsByWorkflowStageAsync(null, null);
        }

        /// <summary>
        /// Get all stages that use a specific questionnaire (delegated to name sync service)  
        /// </summary>
        public async Task<List<long>> GetStagesUsingQuestionnaireFastAsync(long questionnaireId)
        {
            if (_nameSync != null)
            {
                return await _nameSync.GetStagesUsingQuestionnaireAsync(questionnaireId);
            }

            // Fallback to direct mapping query
            return await GetQuestionnaireIdsByWorkflowStageAsync(null, null);
        }
    }
}