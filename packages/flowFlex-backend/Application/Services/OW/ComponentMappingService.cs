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

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
        };

        public ComponentMappingService(ISqlSugarClient db, IStageRepository stageRepository, UserContext userContext)
        {
            _db = db;
            _stageRepository = stageRepository;
            _userContext = userContext;
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
            catch { }
            return current;
        }
    }
}