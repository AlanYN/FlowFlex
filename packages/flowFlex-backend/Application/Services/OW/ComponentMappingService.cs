using System.Text.Json;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Application.Services.OW.Extensions;
using SqlSugar;

namespace FlowFlex.Application.Service.OW
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

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
        };

        public ComponentMappingService(ISqlSugarClient db, IStageRepository stageRepository, UserContext userContext, IStageComponentNameSyncService nameSync = null)
        {
            _db = db;
            _stageRepository = stageRepository;
            _userContext = userContext;
            _nameSync = nameSync; // Optional to avoid circular dependency
        }

        /// <summary>
        /// Sync mappings for all stages in a workflow
        /// </summary>
        public async Task SyncWorkflowMappingsAsync(long workflowId)
        {
            try
            {
                Console.WriteLine($"[ComponentMappingService] Syncing mappings for workflow {workflowId}");

                // Get all stages in the workflow
                var stages = await _stageRepository.GetListAsync(s => s.WorkflowId == workflowId && s.IsValid);
                
                foreach (var stage in stages)
                {
                    await SyncStageMappingsAsync(stage.Id);
                }

                Console.WriteLine($"[ComponentMappingService] Completed syncing mappings for workflow {workflowId} - {stages.Count} stages processed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ComponentMappingService] Error syncing workflow {workflowId} mappings: {ex.Message}");
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

            Console.WriteLine($"[ComponentMappingService] Syncing mappings for stage {stageId}");

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
                            Console.WriteLine($"[ComponentMappingService] Synced {questionnaireMappings.Count} questionnaire mappings for stage {stageId}");
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
                            Console.WriteLine($"[ComponentMappingService] Synced {checklistMappings.Count} checklist mappings for stage {stageId}");
                        }
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"[ComponentMappingService] Error parsing ComponentsJson for stage {stageId}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Sync all stage mappings (for initial data migration)
        /// </summary>
        public async Task SyncAllStageMappingsAsync()
        {
            Console.WriteLine("[ComponentMappingService] Starting full sync of all stage mappings");

            var allStages = await _stageRepository.GetListAsync();
            var totalStages = allStages.Count;
            var processedStages = 0;

            foreach (var stage in allStages)
            {
                await SyncStageMappingsAsync(stage.Id);
                processedStages++;
                
                if (processedStages % 10 == 0)
                {
                    Console.WriteLine($"[ComponentMappingService] Progress: {processedStages}/{totalStages} stages processed");
                }
            }

            Console.WriteLine($"[ComponentMappingService] Full sync completed: {processedStages} stages processed");
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
                Console.WriteLine($"[ComponentMappingService] Getting assignments from mapping table for {questionnaireIds.Count} questionnaires");
                
                var mappings = await _db.Queryable<QuestionnaireStageMapping>()
                    .Where(m => questionnaireIds.Contains(m.QuestionnaireId))
                    .Where(m => m.TenantId == _userContext.TenantId && m.AppCode == _userContext.AppCode)
                    .Where(m => m.IsValid == true)
                    .ToListAsync();

                Console.WriteLine($"[ComponentMappingService] Found {mappings.Count} mappings from mapping table");

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
                Console.WriteLine($"[ComponentMappingService] Error getting questionnaire assignments: {ex.Message}");
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
                Console.WriteLine($"[ComponentMappingService] Getting checklist assignments from mapping table for {checklistIds.Count} checklists");
                
                var mappings = await _db.Queryable<ChecklistStageMapping>()
                    .Where(m => checklistIds.Contains(m.ChecklistId))
                    .Where(m => m.TenantId == _userContext.TenantId && m.AppCode == _userContext.AppCode)
                    .Where(m => m.IsValid == true)
                    .ToListAsync();

                Console.WriteLine($"[ComponentMappingService] Found {mappings.Count} checklist mappings from mapping table");

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
                Console.WriteLine($"[ComponentMappingService] Error getting checklist assignments: {ex.Message}");
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
                Console.WriteLine($"[ComponentMappingService] Getting questionnaire IDs from mapping table - WorkflowId: {workflowId}, StageId: {stageId}");

                var query = _db.Queryable<QuestionnaireStageMapping>()
                    .Where(m => m.TenantId == _userContext.TenantId && m.AppCode == _userContext.AppCode)
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

                Console.WriteLine($"[ComponentMappingService] Found {questionnaireIds.Count} distinct questionnaire IDs from mapping table");
                return questionnaireIds;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ComponentMappingService] Error getting questionnaire IDs by workflow/stage: {ex.Message}");
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
                Console.WriteLine($"[ComponentMappingService] Getting checklist IDs from mapping table - WorkflowId: {workflowId}, StageId: {stageId}");

                var query = _db.Queryable<ChecklistStageMapping>()
                    .Where(m => m.TenantId == _userContext.TenantId && m.AppCode == _userContext.AppCode)
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

                Console.WriteLine($"[ComponentMappingService] Found {checklistIds.Count} distinct checklist IDs from mapping table");
                return checklistIds;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ComponentMappingService] Error getting checklist IDs by workflow/stage: {ex.Message}");
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

            Console.WriteLine($"[ComponentMappingService] Syncing mappings for stage {stageId} within transaction");

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
                            Console.WriteLine($"[ComponentMappingService] Synced {questionnaireMappings.Count} questionnaire mappings for stage {stageId} in transaction");
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
                            Console.WriteLine($"[ComponentMappingService] Synced {checklistMappings.Count} checklist mappings for stage {stageId} in transaction");
                        }
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"[ComponentMappingService] Error parsing ComponentsJson for stage {stageId} in transaction: {ex.Message}");
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
                        Console.WriteLine($"[ComponentMappingService] Invalid ComponentsJson for stage {stageId}");
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
                    Console.WriteLine($"[ComponentMappingService] Data inconsistency detected for stage {stageId}:");
                    Console.WriteLine($"  Component Checklists: [{string.Join(", ", componentChecklistIds)}]");
                    Console.WriteLine($"  Mapping Checklists: [{string.Join(", ", mappingChecklistIds)}]");
                    Console.WriteLine($"  Component Questionnaires: [{string.Join(", ", componentQuestionnaireIds)}]");
                    Console.WriteLine($"  Mapping Questionnaires: [{string.Join(", ", mappingQuestionnaireIds)}]");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ComponentMappingService] Error validating consistency for stage {stageId}: {ex.Message}");
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
                Console.WriteLine($"[ComponentMappingService] Error checking sync needs for stage {stageId}: {ex.Message}");
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
            catch {             }
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
                    Console.WriteLine($"[ComponentMappingService] Error in checklist name sync notification: {ex.Message}");
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
                    Console.WriteLine($"[ComponentMappingService] Error in questionnaire name sync notification: {ex.Message}");
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
                    Console.WriteLine($"[ComponentMappingService] Error in batch checklist name sync notification: {ex.Message}");
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
                    Console.WriteLine($"[ComponentMappingService] Error in batch questionnaire name sync notification: {ex.Message}");
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