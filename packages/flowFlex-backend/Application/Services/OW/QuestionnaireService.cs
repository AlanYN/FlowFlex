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
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Repository.OW;
using SqlSugar;

namespace FlowFlex.Application.Service.OW
{
    /// <summary>
    /// Questionnaire service implementation
    /// </summary>
    public class QuestionnaireService : IQuestionnaireService, IScopedService
    {
        private static readonly JsonSerializerOptions StageJsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
        };

        private readonly IQuestionnaireRepository _questionnaireRepository;
        private readonly IMapper _mapper;
        private readonly IStageRepository _stageRepository;
        private readonly IOperatorContextService _operatorContextService;
        private readonly UserContext _userContext;
        private readonly IComponentMappingService _mappingService;

        public QuestionnaireService(
            IQuestionnaireRepository questionnaireRepository,
            IMapper mapper,
            IStageRepository stageRepository,
            UserContext userContext,
            IOperatorContextService operatorContextService,
            IComponentMappingService mappingService)
        {
            _questionnaireRepository = questionnaireRepository;
            _mapper = mapper;
            _stageRepository = stageRepository;
            _mappingService = mappingService;
            _userContext = userContext;
            _operatorContextService = operatorContextService;
        }
        private static string TryUnwrapComponentsJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return json;
            var current = json.Trim();
            // If already looks like array/object, return
            if (current.StartsWith("[") || current.StartsWith("{")) return current;
            // Try unwrap one level string-encoded json
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

        private static string NormalizeJson(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var current = raw.Trim();
            // Unwrap up to 3 layers
            for (int i = 0; i < 3; i++)
            {
                if (current.StartsWith("[") || current.StartsWith("{"))
                {
                    return current;
                }
                var startsWithQuote = (current.StartsWith("\"") && current.EndsWith("\"")) ||
                                      (current.StartsWith("\'") && current.EndsWith("\'"));
                if (!startsWithQuote) break;
                try
                {
                    var inner = JsonSerializer.Deserialize<string>(current);
                    if (string.IsNullOrWhiteSpace(inner)) break;
                    current = inner.Trim();
                }
                catch { break; }
            }
            return current;
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

                    // Assignments are no longer stored in Questionnaire entity
        // They will be managed through Stage Components only

            // Normalize and validate structure JSON
            if (!string.IsNullOrWhiteSpace(input.StructureJson))
            {
                // Normalize IDs in structure JSON to use snowflake IDs
                input.StructureJson = NormalizeStructureJsonIds(input.StructureJson);
                
                if (!await _questionnaireRepository.ValidateStructureAsync(input.StructureJson))
                {
                    throw new CRMException(ErrorCodeEnum.BusinessError, "Invalid questionnaire structure JSON");
                }
            }

        // Create single questionnaire entity without assignments
            var entity = _mapper.Map<Questionnaire>(input);

        // Note: WorkflowId and StageId fields have been removed - assignments are now managed through Stage Components

            // Initialize create information with proper ID and timestamps
            entity.InitCreateInfo(_userContext);
            // Override audit with operator context to ensure non-empty ModifyBy/CreateBy
            var opNameCreate = _operatorContextService.GetOperatorDisplayName();
            var opIdCreate = _operatorContextService.GetOperatorId();
            entity.CreateBy = opNameCreate;
            entity.ModifyBy = opNameCreate;
            entity.CreateUserId = opIdCreate;
            entity.ModifyUserId = opIdCreate;

            // Calculate question statistics
            await CalculateQuestionStatistics(entity, input.Sections);

            await _questionnaireRepository.InsertAsync(entity);

            // Sections module removed; ignore input.Sections to keep compatibility

                    // Sync service is no longer needed as assignments are managed through Stage Components

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

            // Assignments are no longer stored in Questionnaire entity
            // They will be managed through Stage Components only

            // Normalize and validate structure JSON
            if (!string.IsNullOrWhiteSpace(input.StructureJson))
            {
                // Normalize IDs in structure JSON to use snowflake IDs
                input.StructureJson = NormalizeStructureJsonIds(input.StructureJson);
                
                if (!await _questionnaireRepository.ValidateStructureAsync(input.StructureJson))
                {
                    throw new CRMException(ErrorCodeEnum.BusinessError, "Invalid questionnaire structure JSON");
                }
            }

            // If questionnaire is published, do not allow structure modification
            if (entity.Status == "Published" && input.StructureJson != (entity.Structure != null ? entity.Structure.ToString(Newtonsoft.Json.Formatting.None) : null))
            {
                throw new CRMException(ErrorCodeEnum.BusinessError, "Cannot modify structure of published questionnaire");
            }

            // Update the entity (without assignments)
            _mapper.Map(input, entity);

            // Note: WorkflowId and StageId fields have been removed - assignments are now managed through Stage Components

            // Initialize update information with proper timestamps
            entity.InitUpdateInfo(_userContext);
            // Override audit with operator context (ensures ModifyBy populated)
            var opNameUpdate = _operatorContextService.GetOperatorDisplayName();
            var opIdUpdate = _operatorContextService.GetOperatorId();
            entity.ModifyBy = opNameUpdate;
            entity.ModifyUserId = opIdUpdate;
            entity.ModifyDate = DateTimeOffset.Now;

            // Recalculate question statistics
            await CalculateQuestionStatistics(entity, input.Sections);

            var result = await _questionnaireRepository.UpdateAsync(entity);

            // Sections module removed; ignore input.Sections to keep compatibility

            // Sync service is no longer needed as assignments are managed through Stage Components

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

            // Template validation removed - direct deletion allowed

            // Sections module removed; nothing to delete

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

            // Sections removed with ff_questionnaire_section table; keep empty for compatibility
            result.Sections = new List<QuestionnaireSectionDto>();

            // Get assignments from mapping table (ultra-fast)
            var assignments = await _mappingService.GetQuestionnaireAssignmentsAsync(new List<long> { id });
            result.Assignments = assignments.TryGetValue(id, out var questionnaireAssignments)
                ? questionnaireAssignments.Select(a => new FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto
                {
                    WorkflowId = a.WorkflowId,
                    StageId = a.StageId
                }).ToList()
                : new List<FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto>();

            return result;
        }

        public async Task<List<QuestionnaireOutputDto>> GetListAsync(string category = null)
        {
            var list = await _questionnaireRepository.GetByCategoryAsync(category);
            var result = _mapper.Map<List<QuestionnaireOutputDto>>(list);

            // Sections removed; keep empty lists
            foreach (var questionnaire in result)
            {
                questionnaire.Sections = new List<QuestionnaireSectionDto>();
            }

            // Fill assignments for the questionnaires
            await FillAssignmentsAsync(result);

            return result;
        }

        public async Task<List<QuestionnaireOutputDto>> GetByStageIdAsync(long stageId)
        {
            var questionnaireIds = await GetQuestionnaireIdsByStageIdAsync(stageId);
            if (!questionnaireIds.Any())
                return new List<QuestionnaireOutputDto>();
                
            var questionnaires = await _questionnaireRepository.GetByIdsAsync(questionnaireIds);
            var result = _mapper.Map<List<QuestionnaireOutputDto>>(questionnaires);

            // Sections removed; keep empty lists
            foreach (var questionnaire in result)
            {
                questionnaire.Sections = new List<QuestionnaireSectionDto>();
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

            // Sections removed; keep empty lists
            foreach (var questionnaire in result)
            {
                questionnaire.Sections = new List<QuestionnaireSectionDto>();
            }

            // Fill assignments for the questionnaires
            await FillAssignmentsAsync(result);

            return result;
        }

        public async Task<PagedResult<QuestionnaireOutputDto>> QueryAsync(QuestionnaireQueryRequest query)
        {
            Console.WriteLine($"[QuestionnaireService] QueryAsync - WorkflowId: {query.WorkflowId}, StageId: {query.StageId}, PageIndex: {query.PageIndex}, PageSize: {query.PageSize}");
            
            List<Questionnaire> filteredItems;
            int totalCount;

                         // If workflowId or stageId filtering is needed, use ComponentMappingService
             if (query.WorkflowId.HasValue || query.StageId.HasValue)
             {
                 Console.WriteLine($"[QuestionnaireService] Using ComponentMappingService for WorkflowId: {query.WorkflowId}, StageId: {query.StageId}");
                 
                 // Get questionnaire IDs from mapping table (ultra-fast)
                 var questionnaireIds = await _mappingService.GetQuestionnaireIdsByWorkflowStageAsync(query.WorkflowId, query.StageId);
                 Console.WriteLine($"[QuestionnaireService] ComponentMappingService returned {questionnaireIds.Count} questionnaire IDs");

                 // Get paginated questionnaires by IDs
                 var (items, count) = await _questionnaireRepository.GetPagedByIdsAsync(
                     questionnaireIds,
                     query.PageIndex,
                     query.PageSize,
                     query.Name,
                     query.IsActive,
                     query.SortField,
                     query.SortDirection);
                 
                 filteredItems = items;
                 totalCount = count;
                 Console.WriteLine($"[QuestionnaireService] Mapping table query returned {items.Count} items, total count: {count}");
             }
            else
            {
                Console.WriteLine("[QuestionnaireService] Using repository method for basic filtering (no workflow/stage filtering)");
                
                // Use repository method for basic filtering (no workflow/stage filtering needed)
                var (items, count) = await _questionnaireRepository.GetPagedAsync(
                query.PageIndex,
                query.PageSize,
                query.Name,
                    null, // workflowId - not supported in repository anymore
                    null, // stageId - not supported in repository anymore
                null, // isTemplate parameter removed
                query.IsActive,
                query.SortField,
                query.SortDirection);

                filteredItems = items;
                totalCount = count;
                Console.WriteLine($"[QuestionnaireService] Repository returned {items.Count} items, total count: {count}");
            }

            var result = _mapper.Map<List<QuestionnaireOutputDto>>(filteredItems);

            // Sections removed; keep empty lists
            foreach (var questionnaire in result)
            {
                questionnaire.Sections = new List<QuestionnaireSectionDto>();
            }

            // Fill assignments for the questionnaires
            if (query.WorkflowId.HasValue || query.StageId.HasValue)
            {
                // For filtered queries, we know the context and can fill assignments efficiently
                Console.WriteLine("[QuestionnaireService] Filling assignments for filtered query");
                await FillAssignmentsFromFilterContext(result, query.WorkflowId, query.StageId);
            }
            else
            {
                // For basic queries, now filling assignments (may impact performance for large datasets)
                Console.WriteLine("[QuestionnaireService] Filling assignments for basic query");
            await FillAssignmentsAsync(result);
            }

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

            // Determine base name and ensure uniqueness
            var baseName = string.IsNullOrWhiteSpace(input.Name)
                ? $"{sourceQuestionnaire.Name} (Copy)"
                : input.Name;
            var uniqueName = await EnsureUniqueQuestionnaireNameAsync(baseName);

            // Process StructureJson to generate new snowflake IDs
            string newStructureJson = null;
            if (input.CopyStructure && sourceQuestionnaire.Structure != null)
            {
                newStructureJson = GenerateNewIdsInStructureJson(sourceQuestionnaire.Structure.ToString(Newtonsoft.Json.Formatting.None));
            }

            var newQuestionnaire = new Questionnaire
            {
                Name = uniqueName,
                Description = input.Description ?? sourceQuestionnaire.Description,
                Category = input.Category ?? sourceQuestionnaire.Category,
                            Status = "Draft",
                Structure = string.IsNullOrWhiteSpace(newStructureJson) ? null : Newtonsoft.Json.Linq.JToken.Parse(newStructureJson),
                Tags = sourceQuestionnaire.Tags != null ? (Newtonsoft.Json.Linq.JToken)sourceQuestionnaire.Tags.DeepClone() : null,
                EstimatedMinutes = sourceQuestionnaire.EstimatedMinutes,
                AllowDraft = sourceQuestionnaire.AllowDraft,
                AllowMultipleSubmissions = sourceQuestionnaire.AllowMultipleSubmissions,
                IsActive = true,
                Version = 1,
                // Copy question statistics from source questionnaire
                TotalQuestions = input.CopyStructure ? sourceQuestionnaire.TotalQuestions : 0,
                RequiredQuestions = input.CopyStructure ? sourceQuestionnaire.RequiredQuestions : 0,
                // Assignments are no longer stored in Questionnaire entity
                // Copy tenant and app information from source questionnaire
                TenantId = sourceQuestionnaire.TenantId,
                AppCode = sourceQuestionnaire.AppCode
            };

            // Initialize create information with proper ID and timestamps
            newQuestionnaire.InitCreateInfo(_userContext);

            await _questionnaireRepository.InsertAsync(newQuestionnaire);
                       
            // If we copied structure but statistics are still 0, try to calculate them
            if (input.CopyStructure && newQuestionnaire.TotalQuestions == 0 && !string.IsNullOrWhiteSpace(newStructureJson))
            {
                await CalculateQuestionStatistics(newQuestionnaire);
                // Update the statistics in database if they were calculated
                if (newQuestionnaire.TotalQuestions > 0)
                {
                    await _questionnaireRepository.UpdateStatisticsAsync(newQuestionnaire.Id, newQuestionnaire.TotalQuestions, newQuestionnaire.RequiredQuestions);
                }
            }

            return newQuestionnaire.Id;
        }

        private async Task<string> EnsureUniqueQuestionnaireNameAsync(string baseName)
        {
            var originalName = baseName;
            var counter = 1;
            var currentName = baseName;

            while (true)
            {
                var exists = await _questionnaireRepository.IsNameExistsAsync(currentName);
                if (!exists)
                {
                    return currentName;
                }

                counter++;
                currentName = $"{originalName} ({counter})";
            }
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
                                // Generate snowflake ID for all id fields (sections, questions, options, etc.)
                                obj[key] = GenerateSnowflakeId().ToString();
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
        /// Generate snowflake ID for structure elements
        /// </summary>
        private static long GenerateSnowflakeId()
        {
            return SnowFlakeSingle.Instance.NextId();
        }

        /// <summary>
        /// Normalize structure JSON to use snowflake IDs instead of prefixed IDs
        /// </summary>
        private string NormalizeStructureJsonIds(string structureJson)
        {
            if (string.IsNullOrWhiteSpace(structureJson))
            {
                return structureJson;
            }

            try
            {
                // Parse the JSON structure
                var structure = JsonSerializer.Deserialize<JsonElement>(structureJson);

                // Normalize IDs in the structure
                var normalizedStructure = NormalizeIdsInJsonElement(structure);

                // Serialize back to JSON
                return JsonSerializer.Serialize(normalizedStructure, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }
            catch (Exception ex)
            {
                // If JSON parsing fails, return original structure
                // Log the error but don't fail the operation
                Console.WriteLine($"[QuestionnaireService] Error normalizing structure JSON IDs: {ex.Message}");
                return structureJson;
            }
        }

        /// <summary>
        /// Recursively normalize IDs in JSON structure to use snowflake IDs
        /// </summary>
        private object NormalizeIdsInJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var obj = new Dictionary<string, object>();
                    foreach (var property in element.EnumerateObject())
                    {
                        var key = property.Name;
                        var value = property.Value;

                        // Normalize ID fields to use snowflake IDs
                        if (key == "id" && value.ValueKind == JsonValueKind.String)
                        {
                            var originalId = value.GetString();
                            if (!string.IsNullOrEmpty(originalId))
                            {
                                // Check if ID is already a snowflake ID (pure number)
                                if (long.TryParse(originalId, out _))
                                {
                                    // Already a snowflake ID, keep it
                                    obj[key] = originalId;
                                }
                                else
                                {
                                    // Convert prefixed ID to snowflake ID
                                    obj[key] = GenerateSnowflakeId().ToString();
                                }
                            }
                            else
                            {
                                obj[key] = originalId;
                            }
                        }
                        else
                        {
                            obj[key] = NormalizeIdsInJsonElement(value);
                        }
                    }
                    return obj;

                case JsonValueKind.Array:
                    var array = new List<object>();
                    foreach (var item in element.EnumerateArray())
                    {
                        array.Add(NormalizeIdsInJsonElement(item));
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
        /// Test method to verify ID normalization in structure JSON (for debugging)
        /// </summary>
        public string TestNormalizeStructureJsonIds(string originalStructureJson)
        {
            return NormalizeStructureJsonIds(originalStructureJson);
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

            if (entity.Structure == null || string.IsNullOrWhiteSpace(entity.Structure.ToString(Newtonsoft.Json.Formatting.None)))
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

        // GetTemplatesAsync method removed - no longer needed

        // CreateFromTemplateAsync method removed - template functionality discontinued

        public async Task<bool> ValidateStructureAsync(long id)
        {
            var entity = await _questionnaireRepository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new CRMException(ErrorCodeEnum.NotFound, $"Questionnaire with ID {id} not found");
            }

            var structureJson = entity.Structure != null ? entity.Structure.ToString(Newtonsoft.Json.Formatting.None) : null;
            return await _questionnaireRepository.ValidateStructureAsync(structureJson);
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
            if (questionnaire.Structure == null || string.IsNullOrWhiteSpace(questionnaire.Structure.ToString(Newtonsoft.Json.Formatting.None)))
            {
                questionnaire.TotalQuestions = 0;
                questionnaire.RequiredQuestions = 0;
                return;
            }

            try
            {
                var structure = JsonDocument.Parse(questionnaire.Structure.ToString(Newtonsoft.Json.Formatting.None));

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

        // Sections module removed; helper methods deleted

        // Cache method removed - Redis dependency removed

        public async Task<BatchStageQuestionnaireResponse> GetByStageIdsBatchAsync(BatchStageQuestionnaireRequest request)
        {
            var response = new BatchStageQuestionnaireResponse();

            if (request.StageIds == null || !request.StageIds.Any())
            {
                return response;
            }

            // Get questionnaires for each stage from Stage Components
            foreach (var stageId in request.StageIds)
            {
                var questionnaireIds = await GetQuestionnaireIdsByStageIdAsync(stageId);
                if (questionnaireIds.Any())
                {
                    var questionnaires = await _questionnaireRepository.GetByIdsAsync(questionnaireIds);
                    var questionnaireDtos = _mapper.Map<List<QuestionnaireOutputDto>>(questionnaires);
                    
                    // Sections removed; keep empty lists
                    foreach (var questionnaire in questionnaireDtos)
                    {
                        questionnaire.Sections = new List<QuestionnaireSectionDto>();
                    }
                    
                    await FillAssignmentsAsync(questionnaireDtos);
                    response.StageQuestionnaires[stageId] = questionnaireDtos;
                }
                else
                {
                    response.StageQuestionnaires[stageId] = new List<QuestionnaireOutputDto>();
                }
            }

            return response;
        }

            /// <summary>
    /// Fill assignments for questionnaire output DTOs using ultra-fast mapping table
    /// </summary>
    private async Task FillAssignmentsAsync(List<QuestionnaireOutputDto> questionnaires)
    {
        if (questionnaires == null || !questionnaires.Any())
            return;

        var questionnaireIds = questionnaires.Select(q => q.Id).ToList();
        
        Console.WriteLine($"[QuestionnaireService] Using mapping table to fill assignments for {questionnaireIds.Count} questionnaires");
        
        // Get assignments from mapping table (ultra-fast)
        var assignments = await _mappingService.GetQuestionnaireAssignmentsAsync(questionnaireIds);
        
        // Map assignments to questionnaires
        foreach (var questionnaire in questionnaires)
        {
            if (assignments.TryGetValue(questionnaire.Id, out var questionnaireAssignments))
            {
                questionnaire.Assignments = questionnaireAssignments.Select(a => new FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto
                    {
                        WorkflowId = a.WorkflowId,
                    StageId = a.StageId
                    }).ToList();
                }
                else
                {
                questionnaire.Assignments = new List<FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto>();
            }
        }
    }



        /// <summary>
            /// Fill assignments from filter context (optimized for when we already know the workflow/stage)
        /// </summary>
    private async Task FillAssignmentsFromFilterContext(List<QuestionnaireOutputDto> questionnaires, long? workflowId, long? stageId)
        {
        if (!questionnaires.Any())
                return;

        Console.WriteLine($"[QuestionnaireService] FillAssignmentsFromFilterContext - WorkflowId: {workflowId}, StageId: {stageId}, Questionnaires: {questionnaires.Count}");

        try
        {
            // Use the mapping service to get assignments (same as FillAssignmentsAsync but more efficient)
            var questionnaireIds = questionnaires.Select(q => q.Id).ToList();
            
            Console.WriteLine($"[QuestionnaireService] Using mapping table to fill assignments for {questionnaireIds.Count} questionnaires");
            
            // Get assignments from mapping table (ultra-fast)
            var assignments = await _mappingService.GetQuestionnaireAssignmentsAsync(questionnaireIds);
            
            // Map assignments to questionnaires, filtering by context if needed
            foreach (var questionnaire in questionnaires)
            {
                Console.WriteLine($"[QuestionnaireService] Processing questionnaire {questionnaire.Id} ({questionnaire.Name})");
                
                if (assignments.TryGetValue(questionnaire.Id, out var questionnaireAssignments))
                {
                    // Filter assignments based on the context (workflowId/stageId) if provided
                    var filteredAssignments = questionnaireAssignments.AsEnumerable();
                    
                    if (workflowId.HasValue)
                    {
                        filteredAssignments = filteredAssignments.Where(a => a.WorkflowId == workflowId.Value);
                    }
                    
                    if (stageId.HasValue)
                    {
                        filteredAssignments = filteredAssignments.Where(a => a.StageId == stageId.Value);
                    }
                    
                    questionnaire.Assignments = filteredAssignments.Select(a => new FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto
                    {
                        WorkflowId = a.WorkflowId,
                        StageId = a.StageId
                    }).ToList();
                    
                    Console.WriteLine($"[QuestionnaireService] Found {questionnaire.Assignments.Count} assignments for questionnaire {questionnaire.Id} after filtering");
                }
                else
                {
                    questionnaire.Assignments = new List<FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto>();
                    Console.WriteLine($"[QuestionnaireService] No assignments found for questionnaire {questionnaire.Id}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[QuestionnaireService] Error filling assignments from filter context: {ex.Message}");
            // Fallback to empty assignments
            foreach (var questionnaire in questionnaires)
            {
                questionnaire.Assignments = new List<FlowFlex.Application.Contracts.Dtos.OW.Common.AssignmentDto>();
            }
        }
        }

        /// <summary>
    /// Apply sorting to questionnaire list
        /// </summary>
    private List<Questionnaire> ApplySorting(List<Questionnaire> questionnaires, string sortField, string sortDirection)
        {
            if (questionnaires == null || !questionnaires.Any())
            return questionnaires ?? new List<Questionnaire>();

        bool isAsc = sortDirection?.ToLower() == "asc";

        return (sortField?.ToLower()) switch
        {
            "name" => isAsc ? questionnaires.OrderBy(q => q.Name).ToList() : questionnaires.OrderByDescending(q => q.Name).ToList(),
            "createdate" => isAsc ? questionnaires.OrderBy(q => q.CreateDate).ToList() : questionnaires.OrderByDescending(q => q.CreateDate).ToList(),
            "modifydate" => isAsc ? questionnaires.OrderBy(q => q.ModifyDate).ToList() : questionnaires.OrderByDescending(q => q.ModifyDate).ToList(),
            _ => isAsc ? questionnaires.OrderBy(q => q.CreateDate).ToList() : questionnaires.OrderByDescending(q => q.CreateDate).ToList()
        };
    }

    /// <summary>
    /// Get questionnaire IDs by stage ID from mapping table (ultra-fast)
    /// </summary>
    private async Task<List<long>> GetQuestionnaireIdsByStageIdAsync(long stageId)
    {
        try
        {
            Console.WriteLine($"[QuestionnaireService] Getting questionnaire IDs for stage {stageId} using ComponentMappingService");
            
            // Use ComponentMappingService for ultra-fast mapping table query
            var questionnaireIds = await _mappingService.GetQuestionnaireIdsByWorkflowStageAsync(null, stageId);
            
            Console.WriteLine($"[QuestionnaireService] ComponentMappingService found {questionnaireIds.Count} questionnaire IDs for stage {stageId}: [{string.Join(", ", questionnaireIds)}]");
            
            return questionnaireIds;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[QuestionnaireService] Error getting questionnaire IDs for stage {stageId}: {ex.Message}");
            return new List<long>();
        }
    }

            /// <summary>
        /// Get questionnaire IDs by workflow and/or stage ID using database-level JSONB query
        /// </summary>
        private async Task<List<long>> GetQuestionnaireIdsByWorkflowAndStageAsync(long? workflowId, long? stageId)
        {
            try
            {
                Console.WriteLine($"[QuestionnaireService] GetQuestionnaireIdsByWorkflowAndStageAsync using database JSONB query - WorkflowId: {workflowId}, StageId: {stageId}");
                
                // Use database-level JSONB query for optimal performance
                var questionnaireIds = await _questionnaireRepository.GetQuestionnaireIdsByStageComponentsAsync(workflowId, stageId);
                
                Console.WriteLine($"[QuestionnaireService] Database JSONB query found {questionnaireIds.Count} questionnaire IDs");
                
                return questionnaireIds;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting questionnaire IDs for workflow {workflowId} and stage {stageId}: {ex.Message}");
                return new List<long>();
            }
        }

    /// <summary>
    /// Debug method: Find which stages contain a specific questionnaire ID
    /// </summary>
    public async Task<List<Stage>> FindStagesContainingQuestionnaireAsync(long questionnaireId)
    {
        var matchingStages = new List<Stage>();
        
        try
        {
            // Get all stages for debugging
            var allStages = await _stageRepository.GetListAsync();
            Console.WriteLine($"[QuestionnaireService] Checking {allStages.Count} stages for questionnaire {questionnaireId}");
            
            foreach (var stage in allStages)
            {
                if (string.IsNullOrEmpty(stage.ComponentsJson))
                    continue;
                    
                try
                {
                    var normalized = TryUnwrapComponentsJson(stage.ComponentsJson);
                    var components = JsonSerializer.Deserialize<List<StageComponent>>(normalized, StageJsonOptions);
                    var hasQuestionnaire = components?.Any(c => c.Key == "questionnaires" && 
                        c.QuestionnaireIds?.Contains(questionnaireId) == true);
                        
                    if (hasQuestionnaire == true)
                    {
                        Console.WriteLine($"[QuestionnaireService] Found questionnaire {questionnaireId} in stage {stage.Id} (Workflow: {stage.WorkflowId})");
                        matchingStages.Add(stage);
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"[QuestionnaireService] Error parsing ComponentsJson for stage {stage.Id}: {ex.Message}");
                }
            }
            
            if (!matchingStages.Any())
            {
                Console.WriteLine($"[QuestionnaireService] Questionnaire {questionnaireId} not found in any stage components");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error finding stages for questionnaire {questionnaireId}: {ex.Message}");
        }
        
        return matchingStages;
        }
    }
}