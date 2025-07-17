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
                // Debug logging handled by structured logging
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
            }
        }

        /// <summary>
        /// Save stage questionnaire answer
        /// </summary>
        public async Task<bool> SaveAnswerAsync(QuestionnaireAnswerInputDto input)
        {
            try
            {
                // Debug logging handled by structured logging
                // Check if answer already exists - use questionnaireId if provided
                QuestionnaireAnswer existingAnswer = null;

                if (input.QuestionnaireId.HasValue)
                {
                    // Query by (onboardingId, stageId, questionnaireId) combination
                    existingAnswer = await _repository.GetByOnboardingStageAndQuestionnaireAsync(
                        input.OnboardingId,
                        input.StageId,
                        input.QuestionnaireId.Value);
                }
                else
                {
                    // Fallback to original logic for backward compatibility
                    existingAnswer = await _repository.GetByOnboardingAndStageAsync(input.OnboardingId, input.StageId);
                }

                bool isUpdate = existingAnswer != null;
                string oldAnswerJson = existingAnswer?.AnswerJson;

                // Format and validate answer JSON
                var formattedJson = string.IsNullOrWhiteSpace(input.AnswerJson) ? "{}" : input.AnswerJson.Trim();
                // Debug logging handled by structured logging
                if (isUpdate)
                {
                    // Debug logging handled by structured logging
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
                    // Debug logging handled by structured logging
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
                    // Debug logging handled by structured logging
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
                    // Debug logging handled by structured logging
                    // Debug logging handled by structured logging ?? "NULL"}");

                    // Use SqlSugar ORM insert
                    var result = await _sqlSugarClient.Insertable(entity).ExecuteCommandAsync();
                    // Debug logging handled by structured logging
                    // Log the creation
                    if (result > 0)
                    {
                        // Get the inserted entity ID - for new records with questionnaireId, search by all three fields
                        QuestionnaireAnswer insertedEntity;
                        if (input.QuestionnaireId.HasValue)
                        {
                            insertedEntity = await _repository.GetByOnboardingStageAndQuestionnaireAsync(
                                input.OnboardingId,
                                input.StageId,
                                input.QuestionnaireId.Value);
                        }
                        else
                        {
                            insertedEntity = await _repository.GetByOnboardingAndStageAsync(input.OnboardingId, input.StageId);
                        }

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
                // Debug logging handled by structured logging
                throw;
            }
        }

        /// <summary>
        /// Get stage questionnaire answer (latest version only)
        /// </summary>
        public async Task<QuestionnaireAnswerOutputDto?> GetAnswerAsync(long onboardingId, long stageId)
        {
            try
            {
                // Debug logging handled by structured logging
                // Get current tenant ID - from HTTP headers
                string httpTenantId = GetTenantId();
                // Debug logging handled by structured logging
                // Get tenant ID from UserContext
                var userContext = _httpContextAccessor.HttpContext?.RequestServices.GetService(typeof(UserContext)) as UserContext;
                string userContextTenantId = userContext?.TenantId;
                // Debug logging handled by structured logging
                // Check SqlSugar filter status
                // Debug logging handled by structured logging
                // First try querying without filters to see if data exists
                using var scope = _sqlSugarClient.CreateFilterScope();
                var allEntities = await _sqlSugarClient.Queryable<QuestionnaireAnswer>()
                    .Where(x => x.OnboardingId == onboardingId && x.StageId == stageId && x.IsValid)
                    .ToListAsync();
                // Debug logging handled by structured logging
                foreach (var e in allEntities)
                {
                    // Debug logging handled by structured logging
                }

                // Use normal repository method query (with tenant filter)
                var entity = await _repository.GetByOnboardingAndStageAsync(onboardingId, stageId);
                // Debug logging handled by structured logging}");

                // If not found, try manual tenant ID matching
                if (entity == null && allEntities.Count > 0)
                {
                    // Debug logging handled by structured logging
                    var matchingEntity = allEntities.FirstOrDefault(e =>
                        e.TenantId == httpTenantId || e.TenantId == userContextTenantId);
                    if (matchingEntity != null)
                    {
                        // Debug logging handled by structured logging
                        return _mapper.Map<QuestionnaireAnswerOutputDto>(matchingEntity);
                    }
                }

                return entity == null ? null : _mapper.Map<QuestionnaireAnswerOutputDto>(entity);
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                throw;
            }
        }

        /// <summary>
        /// Get all stage questionnaire answers (including multiple versions)
        /// </summary>
        public async Task<List<QuestionnaireAnswerOutputDto>> GetAllAnswersAsync(long onboardingId, long stageId)
        {
            try
            {
                // Debug logging handled by structured logging
                var entities = await _repository.GetAllByOnboardingAndStageAsync(onboardingId, stageId);
                // Debug logging handled by structured logging
                return _mapper.Map<List<QuestionnaireAnswerOutputDto>>(entities);
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
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
                // Debug logging handled by structured logging
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

            // 方案1: 按Stage分组，但支持每个Stage有多个答案的情况
            var stageAnswersDict = new Dictionary<long, object>();

            // Group by Stage ID
            var groupedByStage = allAnswers.GroupBy(a => a.StageId);

            foreach (var stageGroup in groupedByStage)
            {
                var stageId = stageGroup.Key;
                var stageAnswers = stageGroup.ToList();

                if (stageAnswers.Count == 1)
                {
                    // 如果Stage只有一个答案，直接返回该答案
                    stageAnswersDict[stageId] = _mapper.Map<QuestionnaireAnswerOutputDto>(stageAnswers.First());
                }
                else if (stageAnswers.Count > 1)
                {
                    // 如果Stage有多个答案，按问卷ID分组返回
                    var questionnaireAnswers = stageAnswers.ToDictionary(
                        a => a.QuestionnaireId,
                        a => _mapper.Map<QuestionnaireAnswerOutputDto>(a)
                    );
                    stageAnswersDict[stageId] = questionnaireAnswers;
                }
            }

            // 确保所有请求的Stage ID都有对应的结果（即使是null）
            foreach (var stageId in request.StageIds)
            {
                if (stageAnswersDict.ContainsKey(stageId))
                {
                    response.StageAnswers[stageId] = stageAnswersDict[stageId];
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

                // 处理新答案中的每个响�?
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
                                    // 答案发生变更，添加变更历�?
                                    // 先保留原有的变更历史，然后添加新的变更记�?
                                    if (oldResponse.TryGetProperty("changeHistory", out var existingChangeHistory))
                                    {
                                        try
                                        {
                                            responseObj["changeHistory"] = JsonSerializer.Deserialize<List<object>>(existingChangeHistory.GetRawText());
                                        }
                                        catch (Exception ex)
                                        {
                                            // Debug logging handled by structured logging
                                            responseObj["changeHistory"] = new List<object>();
                                        }
                                    }

                                    AddChangeHistory(responseObj, currentUser, currentTime, "modified");
                                    // Debug logging handled by structured logging
                                }
                                else
                                {
                                    // 答案未变更，保留原有的变更历史（如果存在�?
                                    // Debug logging handled by structured logging
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
                                                // Debug logging handled by structured logging
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            // Debug logging handled by structured logging
                                        }
                                    }

                                    // 如果没有现有的变更历史，创建一个初始记�?
                                    if (!hasExistingHistory)
                                    {
                                        // Debug logging handled by structured logging
                                        AddChangeHistory(responseObj, "System", currentTime, "created");
                                    }
                                    else
                                    {
                                        // 保留最后修改信�?
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
                                // 新增的问题答�?
                                AddChangeHistory(responseObj, currentUser, currentTime, "created");
                                // Debug logging handled by structured logging
                            }

                            updatedResponses.Add(responseObj);
                        }
                    }
                }

                // 重新构建完整的答案对�?
                var updatedAnswerData = JsonSerializer.Deserialize<Dictionary<string, object>>(newAnswerJson);
                updatedAnswerData["responses"] = updatedResponses;

                return JsonSerializer.Serialize(updatedAnswerData, new JsonSerializerOptions
                {
                    WriteIndented = false
                });
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                // 如果处理过程中出错，返回原始新答�?
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
                            // Debug logging handled by structured logging
                        }
                        else
                        {
                            string questionId = response.TryGetProperty("questionId", out var qId) ? qId.GetString() : "unknown";
                            // Debug logging handled by structured logging
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
                // Debug logging handled by structured logging
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

                // 比较主要的答案字�?
                var fieldsToCompare = new[] { "answer", "responseText" };

                foreach (var field in fieldsToCompare)
                {
                    bool oldHasField = oldResponse.TryGetProperty(field, out var oldValue);
                    bool newHasField = newResponse.TryGetProperty(field, out var newValue);
                    // Debug logging handled by structured logging
                    if (oldHasField != newHasField)
                    {
                        // Debug logging handled by structured logging
                        return true; // 字段存在性发生变�?
                    }

                    if (oldHasField && newHasField)
                    {
                        // 比较�?
                        string oldStr = JsonSerializer.Serialize(oldValue);
                        string newStr = JsonSerializer.Serialize(newValue);
                        // Debug logging handled by structured logging
                        if (oldStr != newStr)
                        {
                            // Debug logging handled by structured logging
                            return true; // 值发生变�?
                        }
                    }
                }
                // Debug logging handled by structured logging
                return false;
            }
            catch (Exception ex)
            {
                string questionId = newResponse.TryGetProperty("questionId", out var qId) ? qId.GetString() : "unknown";
                // Debug logging handled by structured logging
                // 如果比较过程中出错，保守地假设没有发生变�?
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

                            // 如果字符串不为空且不只是空白字符，则认为有�?
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
                // Debug logging handled by structured logging
                // 如果检查过程中出错，保守地假设有�?
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

                // 获取现有的变更历�?
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

                // 只保留最近的10条变更记�?
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
                // Debug logging handled by structured logging
            }
        }
    }
}