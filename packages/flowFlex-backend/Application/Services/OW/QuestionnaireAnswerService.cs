using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using FlowFlex.Application.Contracts.Dtos.OW.QuestionnaireAnswer;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using SqlSugar;
using System.Text.Json;
using System.Linq;
using FlowFlex.SqlSugarDB.Extensions;
using FlowFlex.Application.Services.OW.Extensions;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Questionnaire answer management service implementation
    /// </summary>
    public class QuestionnaireAnswerService : IQuestionnaireAnswerService, IScopedService
    {
        private readonly IQuestionnaireAnswerRepository _repository;
        private readonly IStageCompletionLogRepository _stageCompletionLogRepository;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IStageRepository _stageRepository;
        private readonly IOperationChangeLogService _operationChangeLogService;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISqlSugarClient _sqlSugarClient;
        private readonly UserContext _userContext;

        public QuestionnaireAnswerService(
            IQuestionnaireAnswerRepository repository,
            IStageCompletionLogRepository stageCompletionLogRepository,
            IOnboardingRepository onboardingRepository,
            IStageRepository stageRepository,
            IOperationChangeLogService operationChangeLogService,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            ISqlSugarClient sqlSugarClient,
            UserContext userContext)
        {
            _repository = repository;
            _stageCompletionLogRepository = stageCompletionLogRepository;
            _onboardingRepository = onboardingRepository;
            _stageRepository = stageRepository;
            _operationChangeLogService = operationChangeLogService;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _sqlSugarClient = sqlSugarClient;
            _userContext = userContext;
        }

        /// <summary>
        /// Get current user name from UserContext
        /// </summary>
        private string GetCurrentUserName()
        {
            return !string.IsNullOrEmpty(_userContext?.UserName) ? _userContext.UserName : "System";
        }

        /// <summary>
        /// Log questionnaire answer change to Change Log
        /// </summary>
        private async Task LogQuestionnaireAnswerChangeAsync(long onboardingId, long stageId, long questionnaireId, string action, object oldAnswer = null, object newAnswer = null, string notes = null)
        {
            try
            {
                var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
                var stage = await _stageRepository.GetByIdAsync(stageId);

                var logData = new
                {
                    OnboardingId = onboardingId,
                    LeadId = onboarding?.LeadId,
                    LeadName = onboarding?.LeadName,
                    StageId = stageId,
                    StageName = stage?.Name ?? "Unknown",
                    QuestionnaireId = questionnaireId,
                    Action = action,
                    OldAnswer = oldAnswer,
                    NewAnswer = newAnswer,
                    Notes = notes,
                    ActionTime = DateTimeOffset.UtcNow,
                    ActionBy = GetCurrentUserName(),
                    Source = "questionnaire_answer"
                };

                var stageCompletionLog = new StageCompletionLog
                {
                    TenantId = onboarding?.TenantId ?? _userContext?.TenantId ?? "default",
                    OnboardingId = onboardingId,
                    StageId = stageId,
                    StageName = stage?.Name ?? "Unknown",
                    LogType = "questionnaire_answer_change",
                    Action = action,
                    LogData = System.Text.Json.JsonSerializer.Serialize(logData),
                    Success = true,
                    NetworkStatus = "online",
                    CreateBy = GetCurrentUserName(),
                    ModifyBy = GetCurrentUserName()
                };

                await _stageCompletionLogRepository.InsertAsync(stageCompletionLog);
                Console.WriteLine($"✅ Questionnaire answer change logged: {action}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to log questionnaire answer change: {ex.Message}");
            }
        }

        /// <summary>
        /// Save stage questionnaire answer
        /// </summary>
        public async Task<bool> SaveAnswerAsync(QuestionnaireAnswerInputDto input)
        {
            try
            {
                Console.WriteLine($"[SaveAnswerAsync] Starting save for OnboardingId: {input.OnboardingId}, StageId: {input.StageId}");

                // Check if answer already exists
                var existingAnswer = await _repository.GetByOnboardingAndStageAsync(input.OnboardingId, input.StageId);
                bool isUpdate = existingAnswer != null;
                string oldAnswerJson = existingAnswer?.AnswerJson;

                // Format and validate answer JSON
                var formattedJson = string.IsNullOrWhiteSpace(input.AnswerJson) ? "{}" : input.AnswerJson.Trim();
                Console.WriteLine($"[SaveAnswerAsync] Formatted JSON length: {formattedJson.Length}");

                if (isUpdate)
                {
                    Console.WriteLine($"[SaveAnswerAsync] Updating existing answer with ID: {existingAnswer.Id}");

                    // Process answer changes history
                    var updatedAnswerJson = await ProcessAnswerChangesAsync(oldAnswerJson, formattedJson);

                    // Update existing answer
                    existingAnswer.AnswerJson = updatedAnswerJson;
                    existingAnswer.Status = input.Status ?? "Draft";
                    existingAnswer.CompletionRate = (int)Math.Round(input.CompletionRate ?? 0);
                    existingAnswer.InitUpdateInfo(_userContext);

                    if (input.Status == "Submitted")
                    {
                        existingAnswer.SubmitTime = DateTimeOffset.Now;
                    }

                    var updateResult = await _repository.UpdateAsync(existingAnswer);
                    Console.WriteLine($"[SaveAnswerAsync] Update result: {updateResult}");

                    // Log the update
                    if (updateResult)
                    {
                        await _operationChangeLogService.LogQuestionnaireAnswerSubmitAsync(
                            existingAnswer.Id,
                            input.OnboardingId,
                            input.StageId,
                            input.QuestionnaireId,
                            oldAnswerJson,
                            updatedAnswerJson,
                            true // isUpdate = true
                        );
                    }

                    return updateResult;
                }
                else
                {
                    Console.WriteLine("[SaveAnswerAsync] Creating new answer");

                    // Process new answer changes history
                    var processedAnswerJson = await ProcessAnswerChangesAsync(null, formattedJson);

                    // Create entity object
                    var entity = new QuestionnaireAnswer
                    {
                        OnboardingId = input.OnboardingId,
                        StageId = input.StageId,
                        QuestionnaireId = input.QuestionnaireId,
                        AnswerJson = processedAnswerJson,
                        Status = input.Status ?? "Draft",
                        CompletionRate = (int)Math.Round(input.CompletionRate ?? 0),
                        SubmitTime = input.Status == "Submitted" ? DateTimeOffset.Now : null,
                        Version = await GetNextVersionAsync(input.OnboardingId, input.StageId),
                        IsLatest = true,
                        Source = "customer_portal",
                        IpAddress = GetClientIpAddress(),
                        UserAgent = input.UserAgent ?? string.Empty
                    };

                    // Initialize create information with proper ID and timestamps
                    entity.InitCreateInfo(_userContext);

                    Console.WriteLine($"[SaveAnswerAsync] Using ORM insert, JSON length: {processedAnswerJson.Length}");
                    Console.WriteLine($"[SaveAnswerAsync] Status: '{entity.Status}', SubmitTime: {entity.SubmitTime?.ToString() ?? "NULL"}");

                    // Use SqlSugar ORM insert
                    var result = await _sqlSugarClient.Insertable(entity).ExecuteCommandAsync();
                    Console.WriteLine($"[SaveAnswerAsync] Insert result: {result}");

                    // Log the creation
                    if (result > 0)
                    {
                        // Get the inserted entity ID
                        var insertedEntity = await _repository.GetByOnboardingAndStageAsync(input.OnboardingId, input.StageId);
                        if (insertedEntity != null)
                        {
                            await _operationChangeLogService.LogQuestionnaireAnswerSubmitAsync(
                                insertedEntity.Id,
                                input.OnboardingId,
                                input.StageId,
                                input.QuestionnaireId,
                                null, // no before data for new creation
                                processedAnswerJson,
                                false // isUpdate = false
                            );
                        }
                    }

                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SaveAnswerAsync] Exception: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Get stage questionnaire answer
        /// </summary>
        public async Task<QuestionnaireAnswerOutputDto?> GetAnswerAsync(long onboardingId, long stageId)
        {
            try
            {
                Console.WriteLine($"[GetAnswerAsync] Starting query for onboardingId: {onboardingId}, stageId: {stageId}");

                // Get current tenant ID - from HTTP headers
                string httpTenantId = GetTenantId();
                Console.WriteLine($"[GetAnswerAsync] HTTP Header tenant ID: '{httpTenantId}'");

                // Get tenant ID from UserContext
                var userContext = _httpContextAccessor.HttpContext?.RequestServices.GetService(typeof(UserContext)) as UserContext;
                string userContextTenantId = userContext?.TenantId;
                Console.WriteLine($"[GetAnswerAsync] UserContext tenant ID: '{userContextTenantId ?? "NULL"}'");

                // Check SqlSugar filter status
                Console.WriteLine($"[GetAnswerAsync] Checking SqlSugar filters...");

                // First try querying without filters to see if data exists
                using var scope = _sqlSugarClient.CreateFilterScope();
                var allEntities = await _sqlSugarClient.Queryable<QuestionnaireAnswer>()
                    .Where(x => x.OnboardingId == onboardingId && x.StageId == stageId && x.IsValid)
                    .ToListAsync();

                Console.WriteLine($"[GetAnswerAsync] Found {allEntities.Count} records without tenant filter:");
                foreach (var e in allEntities)
                {
                    Console.WriteLine($"  - ID: {e.Id}, TenantId: '{e.TenantId}', IsLatest: {e.IsLatest}, IsValid: {e.IsValid}");
                }

                // Use normal repository method query (with tenant filter)
                var entity = await _repository.GetByOnboardingAndStageAsync(onboardingId, stageId);
                Console.WriteLine($"[GetAnswerAsync] Repository result: {(entity != null ? $"Found entity ID {entity.Id}" : "No entity found")}");

                // If not found, try manual tenant ID matching
                if (entity == null && allEntities.Count > 0)
                {
                    Console.WriteLine($"[GetAnswerAsync] Attempting manual tenant matching...");
                    var matchingEntity = allEntities.FirstOrDefault(e =>
                        e.TenantId == httpTenantId || e.TenantId == userContextTenantId);
                    if (matchingEntity != null)
                    {
                        Console.WriteLine($"[GetAnswerAsync] Found matching entity with manual search: ID {matchingEntity.Id}");
                        return _mapper.Map<QuestionnaireAnswerOutputDto>(matchingEntity);
                    }
                }

                return entity == null ? null : _mapper.Map<QuestionnaireAnswerOutputDto>(entity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetAnswerAsync] Exception: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Update questionnaire answer
        /// </summary>
        public async Task<bool> UpdateAnswerAsync(long answerId, QuestionnaireAnswerUpdateDto input)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(answerId);
                if (existing == null) return false;

                var oldAnswerJson = existing.AnswerJson;
                var newAnswerJson = input.AnswerJson;

                // If there are important updates, create new version
                if (ShouldCreateNewVersion(existing, input))
                {
                    // Create new version
                    var newEntity = _mapper.Map<QuestionnaireAnswer>(existing);
                    newEntity.Id = 0; // Reset ID to create new record
                    newEntity.AnswerJson = newAnswerJson;
                    newEntity.Status = input.Status ?? existing.Status;
                    newEntity.InitCreateInfo(_userContext);
                    newEntity.Version = existing.Version + 1;

                    await _repository.InsertAsync(newEntity);

                    // Mark old version as not latest
                    existing.IsLatest = false;
                    existing.InitUpdateInfo(_userContext);
                    await _repository.UpdateAsync(existing);
                }
                else
                {
                    // Update existing version
                    existing.AnswerJson = newAnswerJson;
                    existing.Status = input.Status ?? existing.Status;
                    existing.CompletionRate = input.CompletionRate.HasValue ? (int)Math.Round(input.CompletionRate.Value) : existing.CompletionRate;
                    existing.InitUpdateInfo(_userContext);

                    await _repository.UpdateAsync(existing);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateAnswerAsync] Exception: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Delete questionnaire answer
        /// </summary>
        public async Task<bool> DeleteAnswerAsync(long answerId)
        {
            return await _repository.DeleteByIdAsync(answerId);
        }

        /// <summary>
        /// Get all answers for onboarding
        /// </summary>
        public async Task<List<QuestionnaireAnswerOutputDto>> GetOnboardingAnswersAsync(long onboardingId)
        {
            var entities = await _repository.GetByOnboardingIdAsync(onboardingId);
            return _mapper.Map<List<QuestionnaireAnswerOutputDto>>(entities);
        }

        /// <summary>
        /// Get answer history versions
        /// </summary>
        public async Task<List<QuestionnaireAnswerOutputDto>> GetAnswerHistoryAsync(long onboardingId, long stageId)
        {
            var entities = await _repository.GetVersionHistoryAsync(onboardingId, stageId);
            return _mapper.Map<List<QuestionnaireAnswerOutputDto>>(entities);
        }

        /// <summary>
        /// Submit answer for review
        /// </summary>
        public async Task<bool> SubmitAnswerAsync(long onboardingId, long stageId)
        {
            try
            {
                var answer = await _repository.GetByOnboardingAndStageAsync(onboardingId, stageId);
                if (answer == null) return false;

                var oldStatus = answer.Status;
                var oldSubmitTime = answer.SubmitTime;

                answer.Status = "Submitted";
                answer.SubmitTime = DateTimeOffset.Now;
                answer.ModifyDate = DateTimeOffset.Now;

                var result = await _repository.UpdateAsync(answer);

                // Log the submission with detailed operation logging
                if (result)
                {
                    await _operationChangeLogService.LogQuestionnaireAnswerSubmitAsync(
                        answer.Id,
                        onboardingId,
                        stageId,
                        answer.QuestionnaireId,
                        System.Text.Json.JsonSerializer.Serialize(new { Status = oldStatus, SubmitTime = oldSubmitTime }),
                        System.Text.Json.JsonSerializer.Serialize(new { Status = answer.Status, SubmitTime = answer.SubmitTime }),
                        false // isUpdate = false (this is a submission)
                    );
                }

                return result;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Review answers
        /// </summary>
        public async Task<bool> ReviewAnswersAsync(QuestionnaireAnswerReviewDto input)
        {
            try
            {
                return await _repository.BatchUpdateStatusAsync(
                    input.AnswerIds,
                    input.Status,
                    input.ReviewerId,
                    input.ReviewNotes);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get answer statistics
        /// </summary>
        public async Task<Dictionary<string, object>> GetStatisticsAsync(long? stageId = null, int days = 30)
        {
            return await _repository.GetCompletionStatisticsAsync(stageId);
        }

        /// <summary>
        /// Get answer list by status
        /// </summary>
        public async Task<List<QuestionnaireAnswerOutputDto>> GetAnswersByStatusAsync(string status, int days = 30)
        {
            var entities = await _repository.GetByStatusAsync(status, days);
            return _mapper.Map<List<QuestionnaireAnswerOutputDto>>(entities);
        }

        /// <summary>
        /// Get next version number
        /// </summary>
        private async Task<int> GetNextVersionAsync(long onboardingId, long stageId)
        {
            var history = await _repository.GetVersionHistoryAsync(onboardingId, stageId);
            return history.Any() ? history.Max(x => x.Version) + 1 : 1;
        }

        /// <summary>
        /// Determine if new version should be created
        /// </summary>
        private static bool ShouldCreateNewVersion(QuestionnaireAnswer existing, QuestionnaireAnswerUpdateDto input)
        {
            // If answer content changes, create new version
            return existing.AnswerJson != input.AnswerJson;
        }

        private string GetClientIpAddress()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return "";

            var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            }
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "";
            }

            return ipAddress;
        }

        private string GetDeviceInfo()
        {
            var context = _httpContextAccessor.HttpContext;
            var userAgent = context?.Request.Headers["User-Agent"].FirstOrDefault() ?? "";

            // Simple device identification
            if (userAgent.Contains("Mobile"))
                return "Mobile";
            if (userAgent.Contains("Tablet"))
                return "Tablet";
            return "Desktop";
        }

        private string GetTenantId()
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? "DEFAULT";
        }

        private string GetCurrentUser()
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.User?.Identity?.Name ?? "SYSTEM";
        }

        /// <summary>
        /// Batch get questionnaire answers for multiple Stages
        /// </summary>
        public async Task<BatchStageAnswerResponse> GetAnswersBatchAsync(BatchStageAnswerRequest request)
        {
            var response = new BatchStageAnswerResponse();

            if (request.StageIds == null || !request.StageIds.Any())
            {
                return response;
            }

            // Batch query questionnaire answers for all Stages
            var allAnswers = await _repository.GetByOnboardingAndStageIdsAsync(request.OnboardingId, request.StageIds);

            // Group by Stage ID
            var groupedAnswers = allAnswers
                .GroupBy(a => a.StageId)
                .ToDictionary(
                    g => g.Key,
                    g => g.FirstOrDefault() // Only take one latest answer per Stage
                );

            // Ensure all requested Stage IDs have corresponding results (even if null)
            foreach (var stageId in request.StageIds)
            {
                if (groupedAnswers.ContainsKey(stageId))
                {
                    response.StageAnswers[stageId] = _mapper.Map<QuestionnaireAnswerOutputDto>(groupedAnswers[stageId]);
                }
                else
                {
                    response.StageAnswers[stageId] = null;
                }
            }

            return response;
        }

        /// <summary>
        /// Process answer change history, add change person and time for changed question answers
        /// </summary>
        /// <param name="oldAnswerJson">Old answer JSON</param>
        /// <param name="newAnswerJson">New answer JSON</param>
        /// <returns>Updated answer JSON</returns>
        private async Task<string> ProcessAnswerChangesAsync(string oldAnswerJson, string newAnswerJson)
        {
            try
            {
                // If no old answer, directly add creation info for all new answers
                if (string.IsNullOrWhiteSpace(oldAnswerJson))
                {
                    return await AddChangeHistoryToNewAnswers(newAnswerJson);
                }

                // Parse old and new answers
                var oldAnswerData = JsonSerializer.Deserialize<JsonElement>(oldAnswerJson);
                var newAnswerData = JsonSerializer.Deserialize<JsonElement>(newAnswerJson);

                // Get current user info and time
                string currentUser = GetCurrentUserName();
                DateTimeOffset currentTime = DateTimeOffset.UtcNow;

                // Check if responses field exists
                if (!oldAnswerData.TryGetProperty("responses", out var oldResponses) ||
                    !newAnswerData.TryGetProperty("responses", out var newResponses))
                {
                    return newAnswerJson; // If structure doesn't match, return original new answer
                }

                // 创建新的响应列表
                var updatedResponses = new List<object>();

                // 将旧答案转换为字典，方便查找
                var oldResponseDict = new Dictionary<string, JsonElement>();
                if (oldResponses.ValueKind == JsonValueKind.Array)
                {
                    foreach (var response in oldResponses.EnumerateArray())
                    {
                        if (response.TryGetProperty("questionId", out var questionId))
                        {
                            oldResponseDict[questionId.GetString()] = response;
                        }
                    }
                }

                // 处理新答案中的每个响应
                if (newResponses.ValueKind == JsonValueKind.Array)
                {
                    foreach (var newResponse in newResponses.EnumerateArray())
                    {
                        if (newResponse.TryGetProperty("questionId", out var questionId))
                        {
                            string questionIdStr = questionId.GetString();
                            var responseObj = JsonSerializer.Deserialize<Dictionary<string, object>>(newResponse.GetRawText());

                            // 检查是否存在旧答案
                            if (oldResponseDict.TryGetValue(questionIdStr, out var oldResponse))
                            {
                                // 比较答案是否发生变更
                                bool hasChanged = HasAnswerChanged(oldResponse, newResponse);

                                if (hasChanged)
                                {
                                    // 答案发生变更，添加变更历史
                                    // 先保留原有的变更历史，然后添加新的变更记录
                                    if (oldResponse.TryGetProperty("changeHistory", out var existingChangeHistory))
                                    {
                                        try
                                        {
                                            responseObj["changeHistory"] = JsonSerializer.Deserialize<List<object>>(existingChangeHistory.GetRawText());
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"[ProcessAnswerChanges] Question {questionIdStr} - Error deserializing existing changeHistory: {ex.Message}");
                                            responseObj["changeHistory"] = new List<object>();
                                        }
                                    }

                                    AddChangeHistory(responseObj, currentUser, currentTime, "modified");
                                    Console.WriteLine($"[ProcessAnswerChanges] Question {questionIdStr} answer changed by {currentUser}");
                                }
                                else
                                {
                                    // 答案未变更，保留原有的变更历史（如果存在）
                                    Console.WriteLine($"[ProcessAnswerChanges] Question {questionIdStr} - No change, preserving existing history");

                                    bool hasExistingHistory = false;
                                    if (oldResponse.TryGetProperty("changeHistory", out var changeHistory))
                                    {
                                        try
                                        {
                                            var historyList = JsonSerializer.Deserialize<List<object>>(changeHistory.GetRawText());
                                            if (historyList != null && historyList.Count > 0)
                                            {
                                                responseObj["changeHistory"] = historyList;
                                                hasExistingHistory = true;
                                                Console.WriteLine($"[ProcessAnswerChanges] Question {questionIdStr} - Preserved {historyList.Count} existing history records");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"[ProcessAnswerChanges] Question {questionIdStr} - Error deserializing changeHistory: {ex.Message}");
                                        }
                                    }

                                    // 如果没有现有的变更历史，创建一个初始记录
                                    if (!hasExistingHistory)
                                    {
                                        Console.WriteLine($"[ProcessAnswerChanges] Question {questionIdStr} - No existing history, creating initial record");
                                        AddChangeHistory(responseObj, "System", currentTime, "created");
                                    }
                                    else
                                    {
                                        // 保留最后修改信息
                                        if (oldResponse.TryGetProperty("lastModifiedBy", out var lastModifiedBy))
                                        {
                                            responseObj["lastModifiedBy"] = lastModifiedBy.GetString();
                                        }
                                        if (oldResponse.TryGetProperty("lastModifiedAt", out var lastModifiedAt))
                                        {
                                            responseObj["lastModifiedAt"] = lastModifiedAt.GetString();
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // 新增的问题答案
                                AddChangeHistory(responseObj, currentUser, currentTime, "created");
                                Console.WriteLine($"[ProcessAnswerChanges] Question {questionIdStr} answer created by {currentUser}");
                            }

                            updatedResponses.Add(responseObj);
                        }
                    }
                }

                // 重新构建完整的答案对象
                var updatedAnswerData = JsonSerializer.Deserialize<Dictionary<string, object>>(newAnswerJson);
                updatedAnswerData["responses"] = updatedResponses;

                return JsonSerializer.Serialize(updatedAnswerData, new JsonSerializerOptions
                {
                    WriteIndented = false
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProcessAnswerChanges] Error processing answer changes: {ex.Message}");
                // 如果处理过程中出错，返回原始新答案
                return newAnswerJson;
            }
        }

        /// <summary>
        /// Add creation history for new answers (only add for answers with values)
        /// </summary>
        private async Task<string> AddChangeHistoryToNewAnswers(string newAnswerJson)
        {
            try
            {
                var answerData = JsonSerializer.Deserialize<JsonElement>(newAnswerJson);

                if (!answerData.TryGetProperty("responses", out var responses))
                {
                    return newAnswerJson;
                }

                string currentUser = GetCurrentUserName();
                DateTimeOffset currentTime = DateTimeOffset.UtcNow;
                var updatedResponses = new List<object>();

                if (responses.ValueKind == JsonValueKind.Array)
                {
                    foreach (var response in responses.EnumerateArray())
                    {
                        var responseObj = JsonSerializer.Deserialize<Dictionary<string, object>>(response.GetRawText());

                        // Check if answer has value
                        bool hasValue = HasResponseValue(response);

                        if (hasValue)
                        {
                            // Only add change history for answers with values
                            AddChangeHistory(responseObj, currentUser, currentTime, "created");

                            string questionId = response.TryGetProperty("questionId", out var qId) ? qId.GetString() : "unknown";
                            Console.WriteLine($"[AddChangeHistoryToNewAnswers] Added change history for question with value: {questionId}");
                        }
                        else
                        {
                            string questionId = response.TryGetProperty("questionId", out var qId) ? qId.GetString() : "unknown";
                            Console.WriteLine($"[AddChangeHistoryToNewAnswers] Skipped change history for question without value: {questionId}");
                        }

                        updatedResponses.Add(responseObj);
                    }
                }

                var updatedAnswerData = JsonSerializer.Deserialize<Dictionary<string, object>>(newAnswerJson);
                updatedAnswerData["responses"] = updatedResponses;

                return JsonSerializer.Serialize(updatedAnswerData, new JsonSerializerOptions
                {
                    WriteIndented = false
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddChangeHistoryToNewAnswers] Error: {ex.Message}");
                return newAnswerJson;
            }
        }

        /// <summary>
        /// Check if answer has changed
        /// </summary>
        private bool HasAnswerChanged(JsonElement oldResponse, JsonElement newResponse)
        {
            try
            {
                string questionId = newResponse.TryGetProperty("questionId", out var qId) ? qId.GetString() : "unknown";

                // 比较主要的答案字段
                var fieldsToCompare = new[] { "answer", "responseText" };

                foreach (var field in fieldsToCompare)
                {
                    bool oldHasField = oldResponse.TryGetProperty(field, out var oldValue);
                    bool newHasField = newResponse.TryGetProperty(field, out var newValue);

                    Console.WriteLine($"[HasAnswerChanged] Question {questionId}, Field {field} - Old exists: {oldHasField}, New exists: {newHasField}");

                    if (oldHasField != newHasField)
                    {
                        Console.WriteLine($"[HasAnswerChanged] Question {questionId}, Field {field} - Field existence changed");
                        return true; // 字段存在性发生变化
                    }

                    if (oldHasField && newHasField)
                    {
                        // 比较值
                        string oldStr = JsonSerializer.Serialize(oldValue);
                        string newStr = JsonSerializer.Serialize(newValue);

                        Console.WriteLine($"[HasAnswerChanged] Question {questionId}, Field {field} - Old: {oldStr}, New: {newStr}");

                        if (oldStr != newStr)
                        {
                            Console.WriteLine($"[HasAnswerChanged] Question {questionId}, Field {field} - Value changed");
                            return true; // 值发生变化
                        }
                    }
                }

                Console.WriteLine($"[HasAnswerChanged] Question {questionId} - No changes detected");
                return false;
            }
            catch (Exception ex)
            {
                string questionId = newResponse.TryGetProperty("questionId", out var qId) ? qId.GetString() : "unknown";
                Console.WriteLine($"[HasAnswerChanged] Question {questionId} - Error during comparison: {ex.Message}");
                // 如果比较过程中出错，保守地假设没有发生变更
                return false;
            }
        }

        /// <summary>
        /// Check if response has value
        /// </summary>
        private bool HasResponseValue(JsonElement response)
        {
            try
            {
                // 检查主要的答案字段
                var fieldsToCheck = new[] { "answer", "responseText" };

                foreach (var field in fieldsToCheck)
                {
                    if (response.TryGetProperty(field, out var value))
                    {
                        // 检查值是否为空或null
                        if (value.ValueKind != JsonValueKind.Null &&
                            value.ValueKind != JsonValueKind.Undefined)
                        {
                            string stringValue = value.ValueKind == JsonValueKind.String
                                ? value.GetString()
                                : value.ToString();

                            // 如果字符串不为空且不只是空白字符，则认为有值
                            if (!string.IsNullOrWhiteSpace(stringValue))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                string questionId = response.TryGetProperty("questionId", out var qId) ? qId.GetString() : "unknown";
                Console.WriteLine($"[HasResponseValue] Question {questionId} - Error checking value: {ex.Message}");
                // 如果检查过程中出错，保守地假设有值
                return true;
            }
        }

        /// <summary>
        /// Add change history to response object
        /// </summary>
        private void AddChangeHistory(Dictionary<string, object> responseObj, string user, DateTimeOffset time, string action)
        {
            try
            {
                // 创建变更历史记录
                var changeRecord = new
                {
                    action = action, // "created", "modified"
                    user = user,
                    timestamp = time.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    timestampUtc = time.UtcDateTime
                };

                // 获取现有的变更历史
                List<object> changeHistory;
                if (responseObj.ContainsKey("changeHistory") && responseObj["changeHistory"] is List<object> existingHistory)
                {
                    changeHistory = existingHistory;
                }
                else
                {
                    changeHistory = new List<object>();
                }

                // 添加新的变更记录
                changeHistory.Add(changeRecord);

                // 只保留最近的10条变更记录
                if (changeHistory.Count > 10)
                {
                    changeHistory = changeHistory.Skip(changeHistory.Count - 10).ToList();
                }

                responseObj["changeHistory"] = changeHistory;

                // 添加最后修改信息（用于快速访问）
                responseObj["lastModifiedBy"] = user;
                responseObj["lastModifiedAt"] = time.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddChangeHistory] Error adding change history: {ex.Message}");
            }
        }
    }
}