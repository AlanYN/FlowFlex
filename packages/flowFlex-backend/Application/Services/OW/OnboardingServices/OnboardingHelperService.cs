using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.Dtos.OW.StaticField;
using FlowFlex.Application.Contracts.Dtos.OW.User;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices.OW.Onboarding;
using FlowFlex.Application.Helpers.OW;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Events;
using FlowFlex.Domain.Shared.Helpers;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Utils;
using FlowFlex.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FlowFlex.Application.Services.OW.OnboardingServices
{
    /// <summary>
    /// Service for onboarding helper and utility methods
    /// Handles: Event publishing, JSON parsing, component processing, utility methods
    /// </summary>
    public class OnboardingHelperService : IOnboardingHelperService
    {
        #region Fields

        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IStageRepository _stageRepository;
        private readonly IUserInvitationRepository _userInvitationRepository;
        private readonly IStageService _stageService;
        private readonly IChecklistService _checklistService;
        private readonly IQuestionnaireService _questionnaireService;
        private readonly IChecklistTaskCompletionService _checklistTaskCompletionService;
        private readonly IQuestionnaireAnswerService _questionnaireAnswerService;
        private readonly IStaticFieldValueService _staticFieldValueService;
        private readonly IOperatorContextService _operatorContextService;
        private readonly ICaseCodeGeneratorService _caseCodeGeneratorService;
        private readonly IUserService _userService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserContext _userContext;
        private readonly IMediator _mediator;
        private readonly ILogger<OnboardingHelperService> _logger;

        // Use shared JSON serializer options for consistency
        private static readonly JsonSerializerOptions JsonOptions = OnboardingSharedUtilities.JsonOptions;

        #endregion

        #region Constructor

        public OnboardingHelperService(
            IOnboardingRepository onboardingRepository,
            IWorkflowRepository workflowRepository,
            IStageRepository stageRepository,
            IUserInvitationRepository userInvitationRepository,
            IStageService stageService,
            IChecklistService checklistService,
            IQuestionnaireService questionnaireService,
            IChecklistTaskCompletionService checklistTaskCompletionService,
            IQuestionnaireAnswerService questionnaireAnswerService,
            IStaticFieldValueService staticFieldValueService,
            IOperatorContextService operatorContextService,
            ICaseCodeGeneratorService caseCodeGeneratorService,
            IUserService userService,
            IServiceScopeFactory serviceScopeFactory,
            IBackgroundTaskQueue backgroundTaskQueue,
            IHttpContextAccessor httpContextAccessor,
            UserContext userContext,
            IMediator mediator,
            ILogger<OnboardingHelperService> logger)
        {
            _onboardingRepository = onboardingRepository ?? throw new ArgumentNullException(nameof(onboardingRepository));
            _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
            _stageRepository = stageRepository ?? throw new ArgumentNullException(nameof(stageRepository));
            _userInvitationRepository = userInvitationRepository ?? throw new ArgumentNullException(nameof(userInvitationRepository));
            _stageService = stageService ?? throw new ArgumentNullException(nameof(stageService));
            _checklistService = checklistService ?? throw new ArgumentNullException(nameof(checklistService));
            _questionnaireService = questionnaireService ?? throw new ArgumentNullException(nameof(questionnaireService));
            _checklistTaskCompletionService = checklistTaskCompletionService ?? throw new ArgumentNullException(nameof(checklistTaskCompletionService));
            _questionnaireAnswerService = questionnaireAnswerService ?? throw new ArgumentNullException(nameof(questionnaireAnswerService));
            _staticFieldValueService = staticFieldValueService ?? throw new ArgumentNullException(nameof(staticFieldValueService));
            _operatorContextService = operatorContextService ?? throw new ArgumentNullException(nameof(operatorContextService));
            _caseCodeGeneratorService = caseCodeGeneratorService ?? throw new ArgumentNullException(nameof(caseCodeGeneratorService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _backgroundTaskQueue = backgroundTaskQueue ?? throw new ArgumentNullException(nameof(backgroundTaskQueue));
            _httpContextAccessor = httpContextAccessor;
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public async Task LogOnboardingActionAsync(
            Domain.Entities.OW.Onboarding onboarding,
            string action,
            string logType,
            bool success,
            object additionalData = null)
        {
            try
            {
                var logData = new
                {
                    OnboardingId = onboarding.Id,
                    LeadId = onboarding.LeadId,
                    CaseName = onboarding.CaseName,
                    WorkflowId = onboarding.WorkflowId,
                    CurrentStageId = onboarding.CurrentStageId,
                    Status = onboarding.Status,
                    Priority = onboarding.Priority,
                    ActionTime = DateTimeOffset.UtcNow,
                    ActionBy = _operatorContextService.GetOperatorDisplayName(),
                    AdditionalData = additionalData
                };

                _logger.LogDebug("Onboarding action logged: {Action}, Type: {LogType}, Success: {Success}, Data: {@LogData}",
                    action, logType, success, logData);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log onboarding action: {Action}", action);
            }
        }

        /// <inheritdoc />
        public async Task PublishStageCompletionEventForCurrentStageAsync(
            Domain.Entities.OW.Onboarding onboarding,
            Stage completedStage,
            bool isFinalStage)
        {
            try
            {
                // Get next stage info if not final stage
                string nextStageName = null;
                long? nextStageId = null;
                if (!isFinalStage && onboarding.CurrentStageId.HasValue)
                {
                    var nextStage = await _stageRepository.GetByIdAsync(onboarding.CurrentStageId.Value);
                    nextStageName = nextStage?.Name;
                    nextStageId = nextStage?.Id;
                }

                // Build components payload
                var componentsPayload = await BuildStageCompletionComponentsAsync(
                    onboarding.Id,
                    completedStage.Id,
                    completedStage.Components,
                    completedStage.ComponentsJson);

                // Publish the OnboardingStageCompletedEvent
                var onboardingStageCompletedEvent = new OnboardingStageCompletedEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    Timestamp = DateTimeOffset.UtcNow,
                    Version = "1.0",
                    TenantId = onboarding.TenantId,
                    UserId = long.TryParse(_userContext?.UserId, out var uid) ? uid : onboarding.CreateUserId,
                    UserName = _userContext?.UserName ?? _operatorContextService.GetOperatorDisplayName() ?? "System",
                    OnboardingId = onboarding.Id,
                    LeadId = onboarding.LeadId,
                    WorkflowId = onboarding.WorkflowId,
                    WorkflowName = (await _workflowRepository.GetByIdAsync(onboarding.WorkflowId))?.Name ?? "Unknown",
                    CompletedStageId = completedStage.Id,
                    CompletedStageName = completedStage.Name,
                    StageCategory = completedStage.Name ?? "Unknown",
                    NextStageId = nextStageId,
                    NextStageName = nextStageName,
                    CompletionRate = onboarding.CompletionRate,
                    IsFinalStage = isFinalStage,
                    AssigneeName = onboarding.CurrentAssigneeName ?? _operatorContextService.GetOperatorDisplayName(),
                    ResponsibleTeam = onboarding.CurrentTeam ?? "default",
                    Priority = onboarding.Priority ?? "Medium",
                    Source = "CustomerPortal",
                    BusinessContext = new Dictionary<string, object>
                    {
                        ["CompletionMethod"] = "CompleteCurrentStage",
                        ["AutoMoveToNext"] = !isFinalStage,
                        ["CompletionNotes"] = "Stage completed via CompleteCurrentStageAsync"
                    },
                    RoutingTags = new List<string> { "onboarding", "stage-completion", "customer-portal", "auto-progression" },
                    Description = $"Stage '{completedStage.Name}' completed for Onboarding {onboarding.Id} via CompleteCurrentStageAsync",
                    Tags = new List<string> { "onboarding", "stage-completion", "auto-progression" },
                    Components = componentsPayload
                };

                // Append debug metrics
                try
                {
                    onboardingStageCompletedEvent.BusinessContext["Components.ChecklistsCount"] = componentsPayload?.Checklists?.Count ?? 0;
                    onboardingStageCompletedEvent.BusinessContext["Components.QuestionnairesCount"] = componentsPayload?.Questionnaires?.Count ?? 0;
                    onboardingStageCompletedEvent.BusinessContext["Components.TaskCompletionsCount"] = componentsPayload?.TaskCompletions?.Count ?? 0;
                    onboardingStageCompletedEvent.BusinessContext["Components.RequiredFieldsCount"] = componentsPayload?.RequiredFields?.Count ?? 0;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error appending debug metrics to business context");
                }

                // Use fire-and-forget pattern
                _backgroundTaskQueue.QueueBackgroundWorkItem(async token =>
                {
                    try
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var scopedMediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                        await scopedMediator.Publish(onboardingStageCompletedEvent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to publish stage completion event");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error publishing stage completion event for Onboarding {OnboardingId}", onboarding.Id);
            }
        }

        /// <inheritdoc />
        public async Task<StageCompletionComponents> BuildStageCompletionComponentsAsync(
            long onboardingId,
            long stageId,
            List<StageComponent> stageComponents,
            string componentsJson)
        {
            var payload = new StageCompletionComponents();

            _logger.LogDebug("BuildStageCompletionComponentsAsync - OnboardingId: {OnboardingId}, StageId: {StageId}, ComponentsCount: {Count}, HasJson: {HasJson}",
                onboardingId, stageId, stageComponents?.Count ?? 0, !string.IsNullOrWhiteSpace(componentsJson));

            // Ensure we have componentsJson from DB if missing
            if (string.IsNullOrWhiteSpace(componentsJson))
            {
                try
                {
                    var stageEntity = await _stageRepository.GetByIdAsync(stageId);
                    if (!string.IsNullOrWhiteSpace(stageEntity?.ComponentsJson))
                    {
                        componentsJson = stageEntity.ComponentsJson;
                        _logger.LogDebug("Retrieved componentsJson from DB for Stage {StageId}", stageId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error retrieving componentsJson from DB for Stage {StageId}", stageId);
                }
            }

            // Ensure we have components from JSON if not provided
            if ((stageComponents == null || stageComponents.Count == 0) && !string.IsNullOrWhiteSpace(componentsJson))
            {
                try
                {
                    stageComponents = JsonSerializer.Deserialize<List<StageComponent>>(componentsJson) ?? new List<StageComponent>();
                    _logger.LogDebug("Standard JSON deserialization successful, components count: {Count}", stageComponents.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Standard JSON deserialization failed, trying lenient parsing");
                    var (parsedComponents, _) = ParseComponentsFromJson(componentsJson);
                    stageComponents = parsedComponents;
                    _logger.LogDebug("Lenient parsing successful, components count: {Count}", stageComponents.Count);
                }
            }

            // Try fetch components from service if still empty
            if (stageComponents == null || stageComponents.Count == 0)
            {
                try
                {
                    var comps = await _stageService.GetComponentsAsync(stageId);
                    if (comps != null && comps.Count > 0)
                    {
                        stageComponents = comps;
                        _logger.LogDebug("Retrieved components from service, count: {Count}", stageComponents.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error retrieving components from service for Stage {StageId}", stageId);
                }
            }

            _logger.LogDebug("Final stageComponents count before processing: {Count}", stageComponents?.Count ?? 0);

            // Process components to populate payload
            await ProcessStageComponentsAsync(onboardingId, stageId, stageComponents, componentsJson, payload);

            _logger.LogDebug("Final payload counts - Checklists: {Checklists}, Questionnaires: {Questionnaires}, TaskCompletions: {TaskCompletions}, RequiredFields: {RequiredFields}",
                payload.Checklists.Count, payload.Questionnaires.Count, payload.TaskCompletions.Count, payload.RequiredFields.Count);

            return payload;
        }

        /// <inheritdoc />
        public (List<StageComponent> stageComponents, List<string> staticFieldNames) ParseComponentsFromJson(string componentsJson)
        {
            var stageComponents = new List<StageComponent>();
            var staticFieldNames = new List<string>();

            try
            {
                // Recursively unwrap JSON strings until we get to the actual array
                string currentJson = componentsJson;
                JsonDocument currentDoc = null;
                int unwrapCount = 0;

                while (true)
                {
                    currentDoc?.Dispose();
                    currentDoc = JsonDocument.Parse(currentJson);

                    if (currentDoc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        _logger.LogDebug("Found JSON array after {UnwrapCount} unwraps", unwrapCount);
                        break;
                    }
                    else if (currentDoc.RootElement.ValueKind == JsonValueKind.String)
                    {
                        currentJson = currentDoc.RootElement.GetString();
                        unwrapCount++;

                        if (unwrapCount > 5)
                        {
                            _logger.LogWarning("Too many JSON unwrap levels, stopping at {UnwrapCount}", unwrapCount);
                            break;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Unexpected JSON root type: {ValueKind}", currentDoc.RootElement.ValueKind);
                        break;
                    }
                }

                if (currentDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    _logger.LogDebug("Processing {ElementCount} JSON array elements", currentDoc.RootElement.GetArrayLength());

                    foreach (var elem in currentDoc.RootElement.EnumerateArray())
                    {
                        if (elem.ValueKind != JsonValueKind.Object)
                        {
                            continue;
                        }

                        string key = GetJsonProperty(elem, "key", "Key");
                        if (string.IsNullOrWhiteSpace(key))
                        {
                            continue;
                        }

                        var component = new StageComponent { Key = key };

                        if (string.Equals(key, "checklist", StringComparison.OrdinalIgnoreCase))
                        {
                            component.ChecklistIds = new List<long>();
                            component.ChecklistNames = new List<string>();
                            var idArr = GetJsonArrayProperty(elem, "checklistIds", "ChecklistIds");
                            if (idArr.HasValue)
                            {
                                foreach (var idEl in idArr.Value.EnumerateArray())
                                {
                                    if (idEl.ValueKind == JsonValueKind.Number && idEl.TryGetInt64(out var lid)) component.ChecklistIds.Add(lid);
                                    else if (idEl.ValueKind == JsonValueKind.String && long.TryParse(idEl.GetString(), out var lsid)) component.ChecklistIds.Add(lsid);
                                }
                            }
                            var nameArr = GetJsonArrayProperty(elem, "checklistNames", "ChecklistNames");
                            if (nameArr.HasValue)
                            {
                                foreach (var nEl in nameArr.Value.EnumerateArray())
                                {
                                    if (nEl.ValueKind == JsonValueKind.String) component.ChecklistNames.Add(nEl.GetString());
                                }
                            }
                        }
                        else if (string.Equals(key, "questionnaires", StringComparison.OrdinalIgnoreCase))
                        {
                            component.QuestionnaireIds = new List<long>();
                            component.QuestionnaireNames = new List<string>();
                            var idArr = GetJsonArrayProperty(elem, "questionnaireIds", "QuestionnaireIds");
                            if (idArr.HasValue)
                            {
                                foreach (var idEl in idArr.Value.EnumerateArray())
                                {
                                    if (idEl.ValueKind == JsonValueKind.Number && idEl.TryGetInt64(out var lid)) component.QuestionnaireIds.Add(lid);
                                    else if (idEl.ValueKind == JsonValueKind.String && long.TryParse(idEl.GetString(), out var lsid)) component.QuestionnaireIds.Add(lsid);
                                }
                            }
                            var nameArr = GetJsonArrayProperty(elem, "questionnaireNames", "QuestionnaireNames");
                            if (nameArr.HasValue)
                            {
                                foreach (var nEl in nameArr.Value.EnumerateArray())
                                {
                                    if (nEl.ValueKind == JsonValueKind.String) component.QuestionnaireNames.Add(nEl.GetString());
                                }
                            }
                        }
                        else if (string.Equals(key, "fields", StringComparison.OrdinalIgnoreCase))
                        {
                            var sfArr = GetJsonArrayProperty(elem, "staticFields", "StaticFields");
                            if (sfArr.HasValue)
                            {
                                var fieldConfigs = new List<StaticFieldConfig>();
                                foreach (var s in sfArr.Value.EnumerateArray())
                                {
                                    if (s.ValueKind == JsonValueKind.String)
                                    {
                                        var name = s.GetString();
                                        if (!string.IsNullOrWhiteSpace(name))
                                        {
                                            staticFieldNames.Add(name);
                                            fieldConfigs.Add(new StaticFieldConfig { Id = name, IsRequired = false, Order = fieldConfigs.Count + 1 });
                                        }
                                    }
                                    else if (s.ValueKind == JsonValueKind.Object)
                                    {
                                        var id = GetJsonProperty(s, "id", "Id");
                                        if (!string.IsNullOrWhiteSpace(id))
                                        {
                                            staticFieldNames.Add(id);
                                            var isRequired = s.TryGetProperty("isRequired", out var reqProp) ? reqProp.GetBoolean() :
                                                            (s.TryGetProperty("IsRequired", out reqProp) ? reqProp.GetBoolean() : false);
                                            var order = s.TryGetProperty("order", out var ordProp) ? ordProp.GetInt32() :
                                                       (s.TryGetProperty("Order", out ordProp) ? ordProp.GetInt32() : fieldConfigs.Count + 1);
                                            fieldConfigs.Add(new StaticFieldConfig { Id = id, IsRequired = isRequired, Order = order });
                                        }
                                    }
                                }
                                component.StaticFields = fieldConfigs;
                            }
                        }

                        stageComponents.Add(component);
                    }
                }
                _logger.LogDebug("ParseComponentsFromJson completed - Components: {ComponentCount}, StaticFields: {StaticFieldCount}",
                    stageComponents.Count, staticFieldNames.Count);

                currentDoc?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ParseComponentsFromJson failed");
            }

            return (stageComponents, staticFieldNames);
        }

        /// <inheritdoc />
        public List<string> ParseJsonArraySafe(string jsonString)
            => OnboardingSharedUtilities.ParseJsonArraySafe(jsonString);

        /// <inheritdoc />
        public string ValidateAndFormatJsonArray(string jsonArray)
        {
            if (string.IsNullOrWhiteSpace(jsonArray))
            {
                return "[]";
            }

            try
            {
                var parsed = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonArray);
                if (parsed is Newtonsoft.Json.Linq.JArray)
                {
                    return jsonArray;
                }
                return "[]";
            }
            catch
            {
                return "[]";
            }
        }

        /// <inheritdoc />
        public async Task UpdateStageTrackingInfoAsync(Domain.Entities.OW.Onboarding entity)
        {
            entity.StageUpdatedTime = DateTimeOffset.UtcNow;
            entity.StageUpdatedBy = _operatorContextService.GetOperatorDisplayName();
            entity.StageUpdatedById = _operatorContextService.GetOperatorId();
            entity.StageUpdatedByEmail = GetCurrentUserEmail();

            if (entity.StagesProgress != null && entity.StagesProgress.Any())
            {
                foreach (var stage in entity.StagesProgress)
                {
                    stage.IsCurrent = stage.StageId == entity.CurrentStageId;
                    if (stage.IsCompleted)
                    {
                        stage.Status = "Completed";
                    }
                    else if (stage.IsCurrent)
                    {
                        stage.Status = "InProgress";
                    }
                    else
                    {
                        stage.Status = "Pending";
                    }
                }
                // Serialize entity list directly to JSON
                entity.StagesProgressJson = JsonSerializer.Serialize(entity.StagesProgress, JsonOptions);
            }
        }


        /// <inheritdoc />
        public async Task SyncStaticFieldValuesAsync(
            long onboardingId,
            long stageId,
            string originalLeadId,
            string originalCaseName,
            string originalContactPerson,
            string originalContactEmail,
            string originalLeadPhone,
            long? originalLifeCycleStageId,
            string originalPriority,
            OnboardingInputDto input)
        {
            try
            {
                _logger.LogDebug("Starting static field sync - OnboardingId: {OnboardingId}, StageId: {StageId}", onboardingId, stageId);

                var staticFieldUpdates = new List<StaticFieldValueInputDto>();

                if (!string.Equals(originalLeadId, input.LeadId, StringComparison.Ordinal))
                {
                    staticFieldUpdates.Add(CreateStaticFieldInput(onboardingId, stageId, "LEADID", input.LeadId, "text", "Lead ID", false));
                }

                if (!string.Equals(originalCaseName, input.CaseName, StringComparison.Ordinal))
                {
                    staticFieldUpdates.Add(CreateStaticFieldInput(onboardingId, stageId, "CUSTOMERNAME", input.CaseName, "text", "Customer Name", false));
                }

                if (!string.Equals(originalContactPerson, input.ContactPerson, StringComparison.Ordinal))
                {
                    staticFieldUpdates.Add(CreateStaticFieldInput(onboardingId, stageId, "CONTACTNAME", input.ContactPerson, "text", "Contact Name", false));
                }

                if (!string.Equals(originalContactEmail, input.ContactEmail, StringComparison.Ordinal))
                {
                    staticFieldUpdates.Add(CreateStaticFieldInput(onboardingId, stageId, "CONTACTEMAIL", input.ContactEmail, "email", "Contact Email", false));
                }

                if (!string.Equals(originalLeadPhone, input.LeadPhone, StringComparison.Ordinal))
                {
                    staticFieldUpdates.Add(CreateStaticFieldInput(onboardingId, stageId, "CONTACTPHONE", input.LeadPhone, "tel", "Contact Phone", false));
                }

                if (originalLifeCycleStageId != input.LifeCycleStageId)
                {
                    staticFieldUpdates.Add(CreateStaticFieldInput(onboardingId, stageId, "LIFECYCLESTAGE", input.LifeCycleStageId?.ToString() ?? "", "select", "Life Cycle Stage", false));
                }

                if (!string.Equals(originalPriority, input.Priority, StringComparison.Ordinal))
                {
                    staticFieldUpdates.Add(CreateStaticFieldInput(onboardingId, stageId, "PRIORITY", input.Priority, "select", "Priority", false));
                }

                if (staticFieldUpdates.Any())
                {
                    _logger.LogDebug("Syncing {FieldCount} static field(s) to database", staticFieldUpdates.Count);
                    var batchInput = new BatchStaticFieldValueInputDto
                    {
                        OnboardingId = onboardingId,
                        StageId = stageId,
                        FieldValues = staticFieldUpdates,
                        Source = "onboarding_update",
                        IpAddress = GetClientIpAddress(),
                        UserAgent = GetUserAgent()
                    };

                    await _staticFieldValueService.BatchSaveAsync(batchInput);
                    _logger.LogDebug("Static field sync completed successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync static field values for Onboarding {OnboardingId}", onboardingId);
            }
        }

        /// <inheritdoc />
        public string GetClientIpAddress()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null) return string.Empty;

            var ipAddress = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            }
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            }

            return ipAddress ?? string.Empty;
        }

        /// <inheritdoc />
        public string GetUserAgent()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            return httpContext?.Request.Headers["User-Agent"].ToString() ?? string.Empty;
        }

        /// <inheritdoc />
        public string SerializeStagesProgress(List<OnboardingStageProgressDto> stagesProgress)
        {
            if (stagesProgress == null || !stagesProgress.Any())
            {
                return "[]";
            }

            try
            {
                return JsonSerializer.Serialize(stagesProgress, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to serialize stages progress");
                return "[]";
            }
        }

        /// <inheritdoc />
        public DateTimeOffset NormalizeToStartOfDay(DateTimeOffset dateTime)
        {
            return new DateTimeOffset(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, dateTime.Offset);
        }

        /// <inheritdoc />
        public string GetCurrentUserEmail()
        {
            return _userContext?.Email ?? _httpContextAccessor?.HttpContext?.User?.FindFirst("email")?.Value ?? string.Empty;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Get JSON property value with support for both camelCase and PascalCase
        /// </summary>
        private string GetJsonProperty(JsonElement elem, string camelCase, string pascalCase)
        {
            if (elem.TryGetProperty(camelCase, out var prop)) return prop.GetString();
            if (elem.TryGetProperty(pascalCase, out var propPascal)) return propPascal.GetString();
            return null;
        }

        /// <summary>
        /// Get JSON array property with support for both camelCase and PascalCase
        /// </summary>
        private JsonElement? GetJsonArrayProperty(JsonElement elem, string camelCase, string pascalCase)
        {
            if (elem.TryGetProperty(camelCase, out var arr) && arr.ValueKind == JsonValueKind.Array) return arr;
            if (elem.TryGetProperty(pascalCase, out var arrPascal) && arrPascal.ValueKind == JsonValueKind.Array) return arrPascal;
            return null;
        }

        /// <summary>
        /// Process stage components to populate the payload
        /// </summary>
        private async Task ProcessStageComponentsAsync(
            long onboardingId,
            long stageId,
            List<StageComponent> stageComponents,
            string componentsJson,
            StageCompletionComponents payload)
        {
            var staticFieldNames = new List<string>();

            // If we need to parse from JSON for static fields, do it once here
            if ((stageComponents == null || !stageComponents.Any(c => c.Key == "fields")) && !string.IsNullOrWhiteSpace(componentsJson))
            {
                var (_, parsedStaticFields) = ParseComponentsFromJson(componentsJson);
                staticFieldNames.AddRange(parsedStaticFields);
            }

            // Process checklists
            await ProcessChecklistsAsync(onboardingId, stageId, stageComponents, componentsJson, payload);

            // Process questionnaires
            await ProcessQuestionnairesAsync(onboardingId, stageId, stageComponents, componentsJson, payload);

            // Process required fields
            await ProcessRequiredFieldsAsync(onboardingId, stageId, stageComponents, componentsJson, payload, staticFieldNames);
        }

        /// <summary>
        /// Process checklists and task completions
        /// </summary>
        private async Task ProcessChecklistsAsync(
            long onboardingId,
            long stageId,
            List<StageComponent> stageComponents,
            string componentsJson,
            StageCompletionComponents payload)
        {
            try
            {
                var checklistComponents = (stageComponents ?? new List<StageComponent>()).Where(c => c.Key == "checklist").ToList();

                if (checklistComponents.Count == 0 && !string.IsNullOrWhiteSpace(componentsJson))
                {
                    var (parsedComponents, _) = ParseComponentsFromJson(componentsJson);
                    checklistComponents = parsedComponents.Where(c => c.Key == "checklist").ToList();
                }

                foreach (var component in checklistComponents)
                {
                    if (component.ChecklistIds != null)
                    {
                        for (int i = 0; i < component.ChecklistIds.Count; i++)
                        {
                            var checklistId = component.ChecklistIds[i];
                            var checklistName = (component.ChecklistNames != null && component.ChecklistNames.Count > i)
                                ? component.ChecklistNames[i]
                                : $"Checklist {checklistId}";

                            try
                            {
                                var detailedChecklist = await _checklistService.GetByIdAsync(checklistId);
                                if (detailedChecklist != null)
                                {
                                    var tasks = new List<ChecklistTaskInfo>();
                                    if (detailedChecklist.Tasks != null)
                                    {
                                        foreach (var task in detailedChecklist.Tasks)
                                        {
                                            tasks.Add(new ChecklistTaskInfo
                                            {
                                                Id = task.Id,
                                                ChecklistId = task.ChecklistId,
                                                Name = task.Name,
                                                Description = task.Description,
                                                OrderIndex = task.OrderIndex,
                                                TaskType = task.TaskType,
                                                IsRequired = task.IsRequired,
                                                EstimatedHours = task.EstimatedHours,
                                                Priority = task.Priority,
                                                IsCompleted = task.IsCompleted,
                                                Status = task.Status,
                                                IsActive = task.IsActive
                                            });
                                        }
                                    }

                                    payload.Checklists.Add(new ChecklistComponentInfo
                                    {
                                        ChecklistId = checklistId,
                                        ChecklistName = detailedChecklist.Name ?? checklistName,
                                        Description = detailedChecklist.Description,
                                        Team = detailedChecklist.Team,
                                        Type = detailedChecklist.Type,
                                        Status = detailedChecklist.Status,
                                        IsTemplate = detailedChecklist.IsTemplate,
                                        CompletionRate = detailedChecklist.CompletionRate,
                                        TotalTasks = detailedChecklist.TotalTasks,
                                        CompletedTasks = detailedChecklist.CompletedTasks,
                                        IsActive = detailedChecklist.IsActive,
                                        Tasks = tasks
                                    });
                                }
                                else
                                {
                                    payload.Checklists.Add(new ChecklistComponentInfo
                                    {
                                        ChecklistId = checklistId,
                                        ChecklistName = checklistName,
                                        IsActive = true
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to get checklist details for {ChecklistId}", checklistId);
                                payload.Checklists.Add(new ChecklistComponentInfo
                                {
                                    ChecklistId = checklistId,
                                    ChecklistName = checklistName,
                                    IsActive = true
                                });
                            }
                        }
                    }
                }

                // Load task completions
                var completions = await _checklistTaskCompletionService.GetByOnboardingAndStageAsync(onboardingId, stageId);
                foreach (var c in completions)
                {
                    payload.TaskCompletions.Add(new ChecklistTaskCompletionInfo
                    {
                        ChecklistId = c.ChecklistId,
                        TaskId = c.TaskId,
                        IsCompleted = c.IsCompleted,
                        CompletionNotes = c.CompletionNotes,
                        CompletedBy = c.ModifyBy ?? c.CreateBy,
                        CompletedTime = c.CompletedTime
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing checklists for Onboarding {OnboardingId}, Stage {StageId}", onboardingId, stageId);
            }
        }

        /// <summary>
        /// Process questionnaires and answers
        /// </summary>
        private async Task ProcessQuestionnairesAsync(
            long onboardingId,
            long stageId,
            List<StageComponent> stageComponents,
            string componentsJson,
            StageCompletionComponents payload)
        {
            try
            {
                var questionnaireComponents = (stageComponents ?? new List<StageComponent>()).Where(c => c.Key == "questionnaires").ToList();

                if (questionnaireComponents.Count == 0 && !string.IsNullOrWhiteSpace(componentsJson))
                {
                    var (parsedComponents, _) = ParseComponentsFromJson(componentsJson);
                    questionnaireComponents = parsedComponents.Where(c => c.Key == "questionnaires").ToList();
                }

                foreach (var component in questionnaireComponents)
                {
                    if (component.QuestionnaireIds != null)
                    {
                        for (int i = 0; i < component.QuestionnaireIds.Count; i++)
                        {
                            var questionnaireId = component.QuestionnaireIds[i];
                            var questionnaireName = (component.QuestionnaireNames != null && component.QuestionnaireNames.Count > i)
                                ? component.QuestionnaireNames[i]
                                : $"Questionnaire {questionnaireId}";

                            try
                            {
                                var detailedQuestionnaire = await _questionnaireService.GetByIdAsync(questionnaireId);
                                if (detailedQuestionnaire != null)
                                {
                                    payload.Questionnaires.Add(new QuestionnaireComponentInfo
                                    {
                                        QuestionnaireId = questionnaireId,
                                        QuestionnaireName = detailedQuestionnaire.Name ?? questionnaireName,
                                        Description = detailedQuestionnaire.Description,
                                        Status = detailedQuestionnaire.Status,
                                        Version = detailedQuestionnaire.Version,
                                        Category = detailedQuestionnaire.Category,
                                        TotalQuestions = detailedQuestionnaire.TotalQuestions,
                                        RequiredQuestions = detailedQuestionnaire.RequiredQuestions,
                                        AllowDraft = detailedQuestionnaire.AllowDraft,
                                        AllowMultipleSubmissions = detailedQuestionnaire.AllowMultipleSubmissions,
                                        IsActive = detailedQuestionnaire.IsActive,
                                        StructureJson = detailedQuestionnaire.StructureJson
                                    });
                                }
                                else
                                {
                                    payload.Questionnaires.Add(new QuestionnaireComponentInfo
                                    {
                                        QuestionnaireId = questionnaireId,
                                        QuestionnaireName = questionnaireName,
                                        IsActive = true
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to get questionnaire details for {QuestionnaireId}", questionnaireId);
                                payload.Questionnaires.Add(new QuestionnaireComponentInfo
                                {
                                    QuestionnaireId = questionnaireId,
                                    QuestionnaireName = questionnaireName,
                                    IsActive = true
                                });
                            }
                        }
                    }
                }

                // Load questionnaire answers
                var answers = await _questionnaireAnswerService.GetAllAnswersAsync(onboardingId, stageId);
                foreach (var a in answers)
                {
                    payload.QuestionnaireAnswers.Add(new QuestionnaireAnswerInfo
                    {
                        AnswerId = a.Id,
                        QuestionnaireId = a.QuestionnaireId ?? 0,
                        QuestionId = 0,
                        QuestionText = string.Empty,
                        QuestionType = string.Empty,
                        IsRequired = false,
                        Answer = a.AnswerJson,
                        AnswerTime = a.SubmitTime,
                        Status = a.Status
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing questionnaires for Onboarding {OnboardingId}, Stage {StageId}", onboardingId, stageId);
            }
        }

        /// <summary>
        /// Process required fields
        /// </summary>
        private async Task ProcessRequiredFieldsAsync(
            long onboardingId,
            long stageId,
            List<StageComponent> stageComponents,
            string componentsJson,
            StageCompletionComponents payload,
            List<string> staticFieldNames)
        {
            try
            {
                // Collect static field names from components
                foreach (var comp in (stageComponents ?? new List<StageComponent>()).Where(c => c.Key == "fields"))
                {
                    if (comp.StaticFields != null)
                    {
                        foreach (var fieldConfig in comp.StaticFields)
                        {
                            if (!string.IsNullOrWhiteSpace(fieldConfig.Id) && !staticFieldNames.Contains(fieldConfig.Id))
                            {
                                staticFieldNames.Add(fieldConfig.Id);
                            }
                        }
                    }
                }

                // If no static fields found, parse from JSON
                if (staticFieldNames.Count == 0 && !string.IsNullOrWhiteSpace(componentsJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(componentsJson);
                        if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var elem in doc.RootElement.EnumerateArray())
                            {
                                if (elem.ValueKind != JsonValueKind.Object) continue;

                                string key = GetJsonProperty(elem, "key", "Key");
                                if (!string.Equals(key, "fields", StringComparison.OrdinalIgnoreCase)) continue;

                                var sfArr = GetJsonArrayProperty(elem, "staticFields", "StaticFields");
                                if (sfArr.HasValue)
                                {
                                    foreach (var s in sfArr.Value.EnumerateArray())
                                    {
                                        string fieldId = null;
                                        if (s.ValueKind == JsonValueKind.String)
                                        {
                                            fieldId = s.GetString();
                                        }
                                        else if (s.ValueKind == JsonValueKind.Object)
                                        {
                                            fieldId = GetJsonProperty(s, "id", "Id");
                                        }

                                        if (!string.IsNullOrWhiteSpace(fieldId) && !staticFieldNames.Contains(fieldId))
                                        {
                                            staticFieldNames.Add(fieldId);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error parsing static fields from JSON for Onboarding {OnboardingId}, Stage {StageId}", onboardingId, stageId);
                    }
                }

                // Load existing field values
                var fieldValues = await _staticFieldValueService.GetLatestByOnboardingAndStageAsync(onboardingId, stageId);
                var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var f in fieldValues)
                {
                    payload.RequiredFields.Add(new RequiredFieldInfo
                    {
                        FieldName = f.FieldName,
                        DisplayName = f.DisplayName,
                        FieldType = f.FieldType,
                        IsRequired = f.IsRequired,
                        FieldValue = f.FieldValueJson,
                        ValidationStatus = f.ValidationStatus,
                        ValidationErrors = string.IsNullOrWhiteSpace(f.ValidationErrors)
                            ? new List<string>()
                            : new List<string>(f.ValidationErrors.Split('\n'))
                    });
                    existing.Add(f.FieldName ?? string.Empty);
                }

                // Add placeholder entries for missing required fields
                foreach (var fieldName in staticFieldNames.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!existing.Contains(fieldName))
                    {
                        payload.RequiredFields.Add(new RequiredFieldInfo
                        {
                            FieldName = fieldName,
                            DisplayName = null,
                            FieldType = string.Empty,
                            IsRequired = true,
                            FieldValue = null,
                            ValidationStatus = "Pending",
                            ValidationErrors = new List<string>()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing required fields for Onboarding {OnboardingId}, Stage {StageId}", onboardingId, stageId);
            }
        }

        /// <summary>
        /// Create StaticFieldValueInputDto for static field sync
        /// </summary>
        private StaticFieldValueInputDto CreateStaticFieldInput(
            long onboardingId,
            long stageId,
            string fieldName,
            string fieldValue,
            string fieldType,
            string fieldLabel,
            bool isRequired)
        {
            return new StaticFieldValueInputDto
            {
                OnboardingId = onboardingId,
                StageId = stageId,
                FieldName = fieldName,
                FieldValueJson = JsonSerializer.Serialize(fieldValue),
                FieldType = fieldType,
                DisplayName = fieldLabel,
                FieldLabel = fieldLabel,
                IsRequired = isRequired,
                Status = "Draft",
                CompletionRate = string.IsNullOrWhiteSpace(fieldValue) ? 0 : 100,
                ValidationStatus = "Pending"
            };
        }

        #endregion

        #region Shared Utility Methods (moved from other services to eliminate duplication)

        /// <inheritdoc />
        public async Task ValidateTeamSelectionsFromJsonAsync(string viewTeamsJson, string operateTeamsJson)
        {
            var viewTeams = OnboardingSharedUtilities.ParseJsonArraySafe(viewTeamsJson) ?? new List<string>();
            var operateTeams = OnboardingSharedUtilities.ParseJsonArraySafe(operateTeamsJson) ?? new List<string>();

            var needsValidation = (viewTeams.Any() || operateTeams.Any());
            if (!needsValidation)
            {
                return;
            }

            HashSet<string> allTeamIds;
            try
            {
                allTeamIds = await GetAllTeamIdsFromUserTreeAsync();
            }
            catch (Exception ex)
            {
                // Do not block the operation if IDM is unavailable; just log
                _logger.LogWarning(ex, "Team validation skipped due to error fetching team tree");
                return;
            }

            // Add "Other" as a valid special team ID (for users without team assignment)
            allTeamIds.Add("Other");

            var invalid = new List<string>();
            invalid.AddRange(viewTeams.Where(id => !string.IsNullOrWhiteSpace(id) && !allTeamIds.Contains(id)));
            invalid.AddRange(operateTeams.Where(id => !string.IsNullOrWhiteSpace(id) && !allTeamIds.Contains(id)));
            invalid = invalid.Distinct(StringComparer.Ordinal).ToList();

            if (invalid.Any())
            {
                throw new CRMException(ErrorCodeEnum.BusinessError,
                    $"The following team IDs do not exist: {string.Join(", ", invalid)}");
            }
        }

        /// <inheritdoc />
        public bool IsJsonbTypeError(Exception ex)
        {
            if (ex == null) return false;
            
            var errorMessage = ex.ToString().ToLowerInvariant();
            return errorMessage.Contains("jsonb") ||
                   errorMessage.Contains("json") && errorMessage.Contains("type") ||
                   errorMessage.Contains("cannot cast") ||
                   errorMessage.Contains("invalid input syntax for type json");
        }

        /// <inheritdoc />
        public void SafeAppendToNotes(Domain.Entities.OW.Onboarding entity, string noteText)
        {
            if (entity == null || string.IsNullOrWhiteSpace(noteText))
            {
                return;
            }

            const int maxNotesLength = 1000;
            var timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            var formattedNote = $"[{timestamp}] {noteText}";

            if (string.IsNullOrWhiteSpace(entity.Notes))
            {
                entity.Notes = formattedNote.Length > maxNotesLength
                    ? formattedNote.Substring(0, maxNotesLength)
                    : formattedNote;
            }
            else
            {
                var newNotes = $"{entity.Notes}\n{formattedNote}";
                if (newNotes.Length > maxNotesLength)
                {
                    // Truncate from the beginning to keep the most recent notes
                    var overflow = newNotes.Length - maxNotesLength;
                    var firstNewlineAfterOverflow = newNotes.IndexOf('\n', overflow);
                    if (firstNewlineAfterOverflow > 0)
                    {
                        entity.Notes = newNotes.Substring(firstNewlineAfterOverflow + 1);
                    }
                    else
                    {
                        entity.Notes = newNotes.Substring(overflow);
                    }
                }
                else
                {
                    entity.Notes = newNotes;
                }
            }
        }

        /// <inheritdoc />
        public async Task CreateDefaultUserInvitationAsync(Domain.Entities.OW.Onboarding onboarding)
        {
            try
            {
                // Determine which email to use (prefer ContactEmail, fallback to LeadEmail)
                var emailToUse = !string.IsNullOrWhiteSpace(onboarding.ContactEmail)
                    ? onboarding.ContactEmail
                    : onboarding.LeadEmail;

                // Skip if no email is available
                if (string.IsNullOrWhiteSpace(emailToUse))
                {
                    _logger.LogDebug("CreateDefaultUserInvitationAsync - Skipping: No email available for Onboarding {OnboardingId}", onboarding.Id);
                    return;
                }

                // Check if invitation already exists for this onboarding and email
                var existingInvitation = await _userInvitationRepository.GetByEmailAndOnboardingIdAsync(emailToUse, onboarding.Id);
                if (existingInvitation != null)
                {
                    // Invitation already exists, skip creation
                    _logger.LogDebug("CreateDefaultUserInvitationAsync - Skipping: Invitation already exists for email {Email} and Onboarding {OnboardingId}", 
                        emailToUse, onboarding.Id);
                    return;
                }

                // Create new UserInvitation record
                var invitation = new UserInvitation
                {
                    OnboardingId = onboarding.Id,
                    Email = emailToUse,
                    InvitationToken = CryptoHelper.GenerateSecureToken(),
                    Status = "Pending",
                    SentDate = null, // Leave empty - will be set when invitation is actually sent
                    TokenExpiry = null, // No expiry
                    SendCount = 0, // Not sent via email
                    TenantId = onboarding.TenantId ?? TenantContextHelper.GetTenantIdOrDefault(_userContext),
                    Notes = "Auto-created default invitation (no email sent)"
                };

                // Generate short URL ID and invitation URL
                invitation.ShortUrlId = CryptoHelper.GenerateShortUrlId(
                    onboarding.Id,
                    emailToUse,
                    invitation.InvitationToken);

                // Generate invitation URL (using default base URL)
                invitation.InvitationUrl = GenerateShortInvitationUrl(
                    invitation.ShortUrlId,
                    onboarding.TenantId ?? TenantContextHelper.GetTenantIdOrDefault(_userContext),
                    onboarding.AppCode ?? TenantContextHelper.GetAppCodeOrDefault(_userContext));

                // Initialize create info
                invitation.InitCreateInfo(_userContext);

                // Insert the invitation record
                await _userInvitationRepository.InsertAsync(invitation);

                _logger.LogInformation("CreateDefaultUserInvitationAsync - Created invitation for email {Email}, Onboarding {OnboardingId}", 
                    emailToUse, onboarding.Id);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the onboarding creation
                // This is a non-critical operation
                _logger.LogWarning(ex, "CreateDefaultUserInvitationAsync - Failed to create invitation for Onboarding {OnboardingId}", onboarding.Id);
            }
        }

        /// <inheritdoc />
        public async Task EnsureCaseCodesAsync(List<Domain.Entities.OW.Onboarding> entities)
        {
            var entitiesWithoutCaseCode = entities.Where(e => string.IsNullOrWhiteSpace(e.CaseCode)).ToList();
            if (!entitiesWithoutCaseCode.Any())
            {
                return;
            }

            _logger.LogInformation(
                "EnsureCaseCodesAsync - Auto-generating CaseCode for {Count} legacy entities",
                entitiesWithoutCaseCode.Count);

            // Batch generate case codes to reduce database round trips
            var updateTasks = new List<(long Id, string CaseCode)>();
            foreach (var entity in entitiesWithoutCaseCode)
            {
                try
                {
                    entity.CaseCode = await _caseCodeGeneratorService.GenerateCaseCodeAsync(entity.CaseName);
                    updateTasks.Add((entity.Id, entity.CaseCode));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "EnsureCaseCodesAsync - Failed to generate CaseCode for Onboarding {OnboardingId}", entity.Id);
                }
            }

            // Batch update database if there are any codes to update
            if (updateTasks.Any())
            {
                try
                {
                    // Use batch update for better performance
                    var db = _onboardingRepository.GetSqlSugarClient();
                    foreach (var batch in updateTasks.Chunk(100)) // Process in batches of 100
                    {
                        var updates = batch.Select(t => new { Id = t.Id, CaseCode = t.CaseCode }).ToList();
                        foreach (var update in updates)
                        {
                            await db.Updateable<Domain.Entities.OW.Onboarding>()
                                .SetColumns(o => o.CaseCode == update.CaseCode)
                                .Where(o => o.Id == update.Id)
                                .ExecuteCommandAsync();
                        }
                    }
                    _logger.LogInformation("EnsureCaseCodesAsync - Batch updated {Count} CaseCodes", updateTasks.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "EnsureCaseCodesAsync - Failed to batch update CaseCodes");
                }
            }
        }

        /// <inheritdoc />
        public async Task<HashSet<string>> GetAllTeamIdsFromUserTreeAsync()
        {
            var tree = await _userService.GetUserTreeAsync();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (tree == null || !tree.Any())
            {
                return ids;
            }

            void Traverse(IEnumerable<UserTreeNodeDto> nodes)
            {
                foreach (var node in nodes)
                {
                    if (node == null) continue;
                    if (string.Equals(node.Type, "team", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(node.Id) && !string.Equals(node.Id, "Other", StringComparison.Ordinal))
                        {
                            ids.Add(node.Id);
                        }
                    }
                    if (node.Children != null && node.Children.Any())
                    {
                        Traverse(node.Children);
                    }
                }
            }

            Traverse(tree);
            return ids;
        }

        #endregion

        #region Private Helper Methods for New Methods

        /// <summary>
        /// Generate short invitation URL
        /// </summary>
        /// <param name="shortUrlId">Short URL ID</param>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="appCode">App code</param>
        /// <param name="baseUrl">Base URL (optional)</param>
        /// <returns>Generated invitation URL</returns>
        private string GenerateShortInvitationUrl(string shortUrlId, string tenantId, string appCode, string? baseUrl = null)
        {
            // Use provided base URL or fall back to a default one
            var effectiveBaseUrl = baseUrl ?? "https://portal.flowflex.com"; // Default base URL

            // Generate the short URL format: {baseUrl}/portal/{tenantId}/{appCode}/invite/{shortUrlId}
            return $"{effectiveBaseUrl.TrimEnd('/')}/portal/{tenantId}/{appCode}/invite/{shortUrlId}";
        }

        #endregion
    }
}
