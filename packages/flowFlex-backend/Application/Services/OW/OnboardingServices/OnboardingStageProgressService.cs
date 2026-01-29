using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices.OW.Onboarding;
using FlowFlex.Application.Helpers.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Text.Json;

namespace FlowFlex.Application.Services.OW.OnboardingServices
{
    /// <summary>
    /// Service for managing onboarding stage progress operations
    /// Handles stage progress initialization, updates, validation, and serialization
    /// </summary>
    public class OnboardingStageProgressService : IOnboardingStageProgressService
    {
        private readonly IStageRepository _stageRepository;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IOperatorContextService _operatorContextService;
        private readonly IOperationChangeLogService _operationChangeLogService;
        private readonly IPermissionService _permissionService;
        private readonly IUserService _userService;
        private readonly UserContext _userContext;
        private readonly ILogger<OnboardingStageProgressService> _logger;

        // Initialization tracking to prevent infinite recursion
        private static readonly HashSet<long> _initializingEntities = new HashSet<long>();
        private static readonly object _initializationLock = new object();

        // Use shared JSON serializer options for consistency
        private static readonly JsonSerializerOptions JsonOptions = OnboardingSharedUtilities.JsonOptions;

        public OnboardingStageProgressService(
            IStageRepository stageRepository,
            IOnboardingRepository onboardingRepository,
            IOperatorContextService operatorContextService,
            IOperationChangeLogService operationChangeLogService,
            IPermissionService permissionService,
            IUserService userService,
            UserContext userContext,
            ILogger<OnboardingStageProgressService> logger)
        {
            _stageRepository = stageRepository ?? throw new ArgumentNullException(nameof(stageRepository));
            _onboardingRepository = onboardingRepository ?? throw new ArgumentNullException(nameof(onboardingRepository));
            _operatorContextService = operatorContextService ?? throw new ArgumentNullException(nameof(operatorContextService));
            _operationChangeLogService = operationChangeLogService ?? throw new ArgumentNullException(nameof(operationChangeLogService));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        /// <inheritdoc />
        public async Task InitializeStagesProgressAsync(Domain.Entities.OW.Onboarding entity, List<Stage> stages)
        {
            try
            {
                entity.StagesProgress = new List<OnboardingStageProgress>();

                if (stages == null || !stages.Any())
                {
                    entity.StagesProgressJson = "[]";
                    return;
                }

                var orderedStages = stages.OrderBy(s => s.Order).ToList();

                // Use sequential stage order (1, 2, 3, 4, 5...) instead of the original stage.Order
                for (int i = 0; i < orderedStages.Count; i++)
                {
                    var stage = orderedStages[i];
                    var sequentialOrder = i + 1;
                    var isFirstStage = sequentialOrder == 1;

                    var stageProgress = new OnboardingStageProgress
                    {
                        StageId = stage.Id,
                        Status = isFirstStage ? "InProgress" : "Pending",
                        IsCompleted = false,
                        StartTime = isFirstStage ? OnboardingSharedUtilities.GetNormalizedUtcNowOffset() : null,
                        CompletionTime = null,
                        CompletedById = null,
                        CompletedBy = null,
                        Notes = null,
                        IsCurrent = isFirstStage,
                        Assignee = ParseDefaultAssignee(stage.DefaultAssignee),
                        CoAssignees = GetFilteredCoAssignees(stage.CoAssignees, stage.DefaultAssignee),
                        CustomStageAssignee = null,
                        CustomStageCoAssignees = null,
                        StageName = stage.Name,
                        StageDescription = stage.Description,
                        StageOrder = sequentialOrder,
                        EstimatedDays = OnboardingSharedUtilities.NormalizeEstimatedDays(stage.EstimatedDuration),
                        VisibleInPortal = stage.VisibleInPortal,
                        PortalPermission = stage.PortalPermission,
                        AttachmentManagementNeeded = stage.AttachmentManagementNeeded,
                        Required = stage.Required,
                        ComponentsJson = stage.ComponentsJson,
                        Components = stage.Components
                    };

                    entity.StagesProgress.Add(stageProgress);
                }

                entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing stages progress for Onboarding {OnboardingId}", entity.Id);
                entity.StagesProgress = new List<OnboardingStageProgress>();
                entity.StagesProgressJson = "[]";
            }
        }

        /// <inheritdoc />
        public async Task UpdateStagesProgressAsync(Domain.Entities.OW.Onboarding entity, long completedStageId, string? completedBy = null, long? completedById = null, string? notes = null)
        {
            try
            {
                LoadStagesProgressFromJson(entity);

                _logger.LogDebug("UpdateStagesProgressAsync - Onboarding {OnboardingId} has {StageCount} stages",
                    entity.Id, entity.StagesProgress?.Count ?? 0);

                var currentTime = DateTimeOffset.UtcNow;
                var completedStage = entity.StagesProgress?.FirstOrDefault(s => s.StageId == completedStageId);

                if (completedStage != null)
                {
                    var wasAlreadyCompleted = completedStage.IsCompleted;

                    completedStage.Status = "Completed";
                    completedStage.IsCompleted = true;
                    completedStage.CompletionTime = currentTime;
                    completedStage.CompletedBy = completedBy ?? _operatorContextService.GetOperatorDisplayName();
                    completedStage.CompletedById = completedById ?? _operatorContextService.GetOperatorId();
                    completedStage.IsCurrent = false;
                    completedStage.LastUpdatedTime = currentTime;
                    completedStage.LastUpdatedBy = completedBy ?? _operatorContextService.GetOperatorDisplayName();

                    if (!completedStage.StartTime.HasValue)
                    {
                        completedStage.StartTime = OnboardingSharedUtilities.GetNormalizedUtcNowOffset();
                    }

                    if (!string.IsNullOrEmpty(notes))
                    {
                        if (wasAlreadyCompleted && !string.IsNullOrEmpty(completedStage.Notes))
                        {
                            completedStage.Notes += $"\n[Re-completed {currentTime:yyyy-MM-dd HH:mm:ss}] {notes}";
                        }
                        else
                        {
                            completedStage.Notes = notes;
                        }
                    }

                    // Find next stage to activate
                    var nextStage = entity.StagesProgress?
                        .Where(s => s.StageOrder > completedStage.StageOrder && !s.IsCompleted)
                        .OrderBy(s => s.StageOrder)
                        .FirstOrDefault();

                    // Clear all current stage flags first
                    if (entity.StagesProgress != null)
                    {
                        foreach (var stage in entity.StagesProgress)
                        {
                            stage.IsCurrent = false;
                        }
                    }

                    if (nextStage != null)
                    {
                        nextStage.Status = "InProgress";
                        nextStage.IsCurrent = true;
                        nextStage.LastUpdatedTime = currentTime;
                        nextStage.LastUpdatedBy = completedBy ?? _operatorContextService.GetOperatorDisplayName();

                        _logger.LogDebug("UpdateStagesProgressAsync - Activated next stage {StageId} for Onboarding {OnboardingId}",
                            nextStage.StageId, entity.Id);
                    }

                    // Update completion rate
                    entity.CompletionRate = CalculateCompletionRateByCompletedStages(entity.StagesProgress ?? new List<OnboardingStageProgress>());

                    _logger.LogDebug("UpdateStagesProgressAsync - Completed stage {StageId}, CompletionRate: {Rate}%",
                        completedStageId, entity.CompletionRate);
                }

                await FilterValidStagesProgressAsync(entity);
                entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress ?? new List<OnboardingStageProgress>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stages progress for Onboarding {OnboardingId}, Stage {StageId}",
                    entity.Id, completedStageId);
            }
        }


        /// <inheritdoc />
        public void LoadStagesProgressFromJson(Domain.Entities.OW.Onboarding entity)
        {
            LoadStagesProgressFromJsonInternal(entity, fixStageOrder: true);
        }

        /// <inheritdoc />
        public void LoadStagesProgressFromJsonReadOnly(Domain.Entities.OW.Onboarding entity)
        {
            LoadStagesProgressFromJsonInternal(entity, fixStageOrder: false);
        }

        /// <summary>
        /// Core implementation for loading stages progress from JSON
        /// </summary>
        private void LoadStagesProgressFromJsonInternal(Domain.Entities.OW.Onboarding entity, bool fixStageOrder)
        {
            try
            {
                if (!string.IsNullOrEmpty(entity.StagesProgressJson))
                {
                    var jsonString = entity.StagesProgressJson.Trim();

                    // Handle double-serialized JSON
                    if (jsonString.StartsWith("\"") && jsonString.EndsWith("\""))
                    {
                        jsonString = JsonSerializer.Deserialize<string>(jsonString, JsonOptions) ?? "[]";
                    }

                    entity.StagesProgress = JsonSerializer.Deserialize<List<OnboardingStageProgress>>(
                        jsonString, JsonOptions) ?? new List<OnboardingStageProgress>();

                    // Only fix stage order when needed and requested
                    if (fixStageOrder && NeedsStageOrderFix(entity.StagesProgress))
                    {
                        FixStageOrderSequence(entity.StagesProgress);
                        entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress);
                    }
                }
                else
                {
                    entity.StagesProgress = new List<OnboardingStageProgress>();
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogWarning(jsonEx, "JSON parsing error in LoadStagesProgressFromJson for Onboarding {OnboardingId}", entity.Id);
                entity.StagesProgress = new List<OnboardingStageProgress>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error loading stages progress for Onboarding {OnboardingId}", entity.Id);
                entity.StagesProgress = new List<OnboardingStageProgress>();
            }
        }

        /// <inheritdoc />
        public bool NeedsStageOrderFix(List<OnboardingStageProgress> stagesProgress)
        {
            if (stagesProgress == null || !stagesProgress.Any())
                return false;

            var orderedStages = stagesProgress.OrderBy(s => s.StageOrder).ToList();
            for (int i = 0; i < orderedStages.Count; i++)
            {
                if (orderedStages[i].StageOrder != i + 1)
                {
                    return true;
                }
            }
            return false;
        }

        /// <inheritdoc />
        public void FixStageOrderSequence(List<OnboardingStageProgress> stagesProgress)
        {
            try
            {
                if (stagesProgress == null || !stagesProgress.Any())
                {
                    return;
                }

                var orderedStages = stagesProgress.OrderBy(s => s.StageOrder).ToList();

                bool needsFixing = false;
                for (int i = 0; i < orderedStages.Count; i++)
                {
                    if (orderedStages[i].StageOrder != i + 1)
                    {
                        needsFixing = true;
                        break;
                    }
                }

                if (!needsFixing)
                {
                    return;
                }

                // Reassign sequential stage orders
                for (int i = 0; i < orderedStages.Count; i++)
                {
                    orderedStages[i].StageOrder = i + 1;
                }

                // Update the original list with fixed orders safely
                for (int i = 0; i < stagesProgress.Count; i++)
                {
                    var matchingStage = orderedStages.FirstOrDefault(s => s.StageId == stagesProgress[i].StageId);
                    if (matchingStage != null)
                    {
                        stagesProgress[i] = matchingStage;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error fixing stage order sequence");
            }
        }

        /// <inheritdoc />
        public async Task<(bool CanComplete, string ErrorMessage)> ValidateStageCanBeCompletedAsync(Domain.Entities.OW.Onboarding entity, long stageId)
        {
            try
            {
                LoadStagesProgressFromJson(entity);

                if (entity.StagesProgress == null || !entity.StagesProgress.Any())
                {
                    return (false, "No stages progress found");
                }

                var stageToComplete = entity.StagesProgress.FirstOrDefault(s => s.StageId == stageId);
                if (stageToComplete == null)
                {
                    return (false, "Stage not found in progress");
                }

                // Check onboarding status
                if (entity.Status == "Completed")
                {
                    return (false, "Onboarding is already completed");
                }

                if (entity.Status == "Cancelled" || entity.Status == "Rejected")
                {
                    return (false, $"Cannot complete stages when onboarding status is {entity.Status}");
                }

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating stage completion for Onboarding {OnboardingId}, Stage {StageId}", entity.Id, stageId);
                return (false, $"Validation error: {ex.Message}");
            }
        }


        /// <inheritdoc />
        public decimal CalculateCompletionRateByCompletedStages(List<OnboardingStageProgress> stagesProgress)
        {
            try
            {
                if (stagesProgress == null || !stagesProgress.Any())
                {
                    return 0;
                }

                var totalStagesCount = stagesProgress.Count;
                var completedStagesCount = stagesProgress.Count(s => s.IsCompleted);

                if (totalStagesCount == 0)
                {
                    return 0;
                }

                var completionRate = Math.Round((decimal)completedStagesCount / totalStagesCount * 100, 2);
                return completionRate;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating completion rate");
                return 0;
            }
        }

        /// <inheritdoc />
        public void EnrichStagesProgressWithStageData(Domain.Entities.OW.Onboarding entity, List<Stage> stages)
        {
            try
            {
                if (entity?.StagesProgress == null || !entity.StagesProgress.Any())
                {
                    return;
                }

                if (stages == null || !stages.Any())
                {
                    return;
                }

                var stageDict = stages.ToDictionary(s => s.Id, s => s);

                foreach (var stageProgress in entity.StagesProgress)
                {
                    if (stageDict.TryGetValue(stageProgress.StageId, out var stage))
                    {
                        stageProgress.StageName = stage.Name;
                        stageProgress.StageDescription = stage.Description;

                        // Only update EstimatedDays if CustomEstimatedDays is not set
                        if (!stageProgress.CustomEstimatedDays.HasValue)
                        {
                            stageProgress.EstimatedDays = OnboardingSharedUtilities.NormalizeEstimatedDays(stage.EstimatedDuration);
                        }

                        stageProgress.VisibleInPortal = stage.VisibleInPortal;
                        stageProgress.PortalPermission = stage.PortalPermission;
                        stageProgress.AttachmentManagementNeeded = stage.AttachmentManagementNeeded;
                        stageProgress.Required = stage.Required;
                        stageProgress.Color = stage.Color;
                        stageProgress.ComponentsJson = stage.ComponentsJson;
                        stageProgress.Components = stage.Components;
                    }
                }

                // Set stage orders based on the order in workflow (sequential: 1, 2, 3, ...)
                var orderedStages = stages.OrderBy(s => s.Order).ToList();
                for (int i = 0; i < orderedStages.Count; i++)
                {
                    var stage = orderedStages[i];
                    var stageProgress = entity.StagesProgress.FirstOrDefault(sp => sp.StageId == stage.Id);
                    if (stageProgress != null)
                    {
                        stageProgress.StageOrder = i + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error enriching stages progress with stage data");
            }
        }

        /// <inheritdoc />
        public async Task EnrichStagesProgressWithStageDataAsync(Domain.Entities.OW.Onboarding entity)
        {
            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            EnrichStagesProgressWithStageData(entity, stages?.ToList() ?? new List<Stage>());
        }

        /// <inheritdoc />
        public async Task SyncStagesProgressWithWorkflowAsync(Domain.Entities.OW.Onboarding entity, List<Stage>? preloadedStages = null)
        {
            try
            {
                var stages = preloadedStages ?? (await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId))?.ToList();
                if (stages == null || !stages.Any())
                {
                    return;
                }

                LoadStagesProgressFromJson(entity);

                if (entity.StagesProgress == null)
                {
                    entity.StagesProgress = new List<OnboardingStageProgress>();
                }

                // Filter invalid stageIds
                var validStageIds = stages.Select(s => s.Id).ToHashSet();
                entity.StagesProgress?.RemoveAll(x => !validStageIds.Contains(x.StageId));

                var existingStageIds = entity.StagesProgress?.Select(sp => sp.StageId).ToHashSet() ?? new HashSet<long>();
                var newStages = stages.Where(s => !existingStageIds.Contains(s.Id)).ToList();

                if (newStages.Any())
                {
                    var orderedStages = stages.OrderBy(s => s.Order).ToList();

                    foreach (var newStage in newStages)
                    {
                        var stageIndex = orderedStages.FindIndex(s => s.Id == newStage.Id);
                        var sequentialOrder = stageIndex + 1;

                        var newStageProgress = new OnboardingStageProgress
                        {
                            StageId = newStage.Id,
                            Status = "Pending",
                            IsCompleted = false,
                            StartTime = null,
                            CompletionTime = null,
                            CompletedById = null,
                            CompletedBy = null,
                            Notes = null,
                            IsCurrent = false,
                            Assignee = ParseDefaultAssignee(newStage.DefaultAssignee),
                            CoAssignees = GetFilteredCoAssignees(newStage.CoAssignees, newStage.DefaultAssignee),
                            CustomStageAssignee = null,
                            CustomStageCoAssignees = null
                        };

                        if (stageIndex < (entity.StagesProgress?.Count ?? 0))
                        {
                            entity.StagesProgress?.Insert(stageIndex, newStageProgress);
                        }
                        else
                        {
                            entity.StagesProgress?.Add(newStageProgress);
                        }
                    }

                    entity.StagesProgressJson = SerializeStagesProgress(entity.StagesProgress ?? new List<OnboardingStageProgress>());
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error syncing stages progress with workflow for Onboarding {OnboardingId}", entity.Id);
            }
        }


        /// <inheritdoc />
        public string SerializeStagesProgress(List<OnboardingStageProgress> stagesProgress)
        {
            try
            {
                if (stagesProgress == null || !stagesProgress.Any())
                {
                    return "[]";
                }

                return JsonSerializer.Serialize(stagesProgress, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error serializing stages progress");
                return "[]";
            }
        }

        /// <inheritdoc />
        public async Task EnsureStagesProgressInitializedAsync(Domain.Entities.OW.Onboarding entity, IEnumerable<Stage>? preloadedStages = null)
        {
            // Prevent infinite recursion using thread-safe entity tracking
            lock (_initializationLock)
            {
                if (_initializingEntities.Contains(entity.Id))
                {
                    return;
                }
                _initializingEntities.Add(entity.Id);
            }

            try
            {
                LoadStagesProgressFromJson(entity);

                var stages = preloadedStages?.ToList() ?? await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
                if (stages == null || !stages.Any())
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "No stages found for workflow");
                }

                if (entity.StagesProgress == null || !entity.StagesProgress.Any())
                {
                    _logger.LogInformation("Initializing empty stagesProgress for Onboarding {OnboardingId} with {StageCount} stages from workflow {WorkflowId}",
                        entity.Id, stages.Count, entity.WorkflowId);
                    await InitializeStagesProgressAsync(entity, stages);

                    _logger.LogInformation("Successfully initialized stagesProgress for Onboarding {OnboardingId}",
                        entity.Id);
                }
                else
                {
                    await SyncStagesProgressWithWorkflowAsync(entity, stages);
                }

                EnrichStagesProgressWithStageData(entity, stages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing stages progress for Onboarding {OnboardingId}", entity.Id);
                throw;
            }
            finally
            {
                lock (_initializationLock)
                {
                    _initializingEntities.Remove(entity.Id);
                }
            }
        }

        /// <inheritdoc />
        public async Task FilterValidStagesProgressAsync(Domain.Entities.OW.Onboarding entity)
        {
            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            if (stages == null || !stages.Any()) return;
            var validStageIds = stages.Select(s => s.Id).ToHashSet();
            entity.StagesProgress?.RemoveAll(x => !validStageIds.Contains(x.StageId));
        }

        /// <inheritdoc />
        public List<string> ParseDefaultAssignee(string defaultAssigneeJson)
        {
            if (string.IsNullOrWhiteSpace(defaultAssigneeJson))
            {
                return new List<string>();
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var jsonString = defaultAssigneeJson.Trim();
                if (jsonString.StartsWith("\"") && jsonString.EndsWith("\""))
                {
                    jsonString = JsonSerializer.Deserialize<string>(jsonString, options) ?? "[]";
                }

                if (jsonString.StartsWith("["))
                {
                    var result = JsonSerializer.Deserialize<List<string>>(jsonString, options);
                    return result ?? new List<string>();
                }

                if (!string.IsNullOrWhiteSpace(jsonString))
                {
                    return new List<string> { jsonString };
                }

                return new List<string>();
            }
            catch (JsonException)
            {
                return new List<string>();
            }
        }

        /// <inheritdoc />
        public List<string> GetFilteredCoAssignees(string coAssigneesJson, string defaultAssigneeJson)
        {
            var coAssignees = ParseDefaultAssignee(coAssigneesJson);
            var defaultAssignees = ParseDefaultAssignee(defaultAssigneeJson);

            if (!defaultAssignees.Any())
            {
                return coAssignees;
            }

            return coAssignees
                .Where(id => !defaultAssignees.Contains(id))
                .ToList();
        }

        #region SafeUpdateOnboardingAsync and Helper Methods

        /// <summary>
        /// Audit info container
        /// </summary>
        private record AuditInfo(DateTimeOffset ModifyDate, string ModifyBy, long ModifyUserId);

        /// <inheritdoc />
        public async Task<bool> SafeUpdateOnboardingAsync(Domain.Entities.OW.Onboarding entity)
        {
            try
            {
                _logger.LogDebug("SafeUpdateOnboardingAsync - Updating Onboarding {OnboardingId}: CurrentStageId={CurrentStageId}, Status={Status}",
                    entity.Id, entity.CurrentStageId, entity.Status);

                var db = _onboardingRepository.GetSqlSugarClient();

                // Step 1: Update permission JSONB fields
                await UpdatePermissionFieldsAsync(db, entity);

                // Step 2: Filter valid stages progress
                await FilterValidStagesProgressAsync(entity);

                // Step 3: Prepare audit values
                var auditInfo = PrepareAuditInfo();
                entity.ModifyDate = auditInfo.ModifyDate;
                entity.ModifyBy = auditInfo.ModifyBy;
                entity.ModifyUserId = auditInfo.ModifyUserId;

                // Step 4: Update main entity fields
                var result = await UpdateMainEntityFieldsAsync(entity);

                // Step 5: Update audit fields via SQL (workaround for SqlSugar reset issue)
                await UpdateAuditFieldsAsync(db, entity.Id, auditInfo);

                // Step 6: Update stages_progress_json separately with JSONB casting
                await UpdateStagesProgressJsonAsync(db, entity);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to safely update onboarding {OnboardingId}", entity.Id);
                throw new CRMException(ErrorCodeEnum.SystemError,
                    $"Failed to safely update onboarding: {ex.Message}");
            }
        }

        /// <summary>
        /// Update permission JSONB fields with explicit casting
        /// </summary>
        private async Task UpdatePermissionFieldsAsync(ISqlSugarClient db, Domain.Entities.OW.Onboarding entity)
        {
            try
            {
                var permissionSql = @"
                    UPDATE ff_onboarding 
                    SET view_teams = @ViewTeams::jsonb,
                        view_users = @ViewUsers::jsonb,
                        operate_teams = @OperateTeams::jsonb,
                        operate_users = @OperateUsers::jsonb,
                        view_permission_mode = @ViewPermissionMode,
                        view_permission_subject_type = @ViewPermissionSubjectType,
                        operate_permission_subject_type = @OperatePermissionSubjectType,
                        use_same_team_for_operate = @UseSameTeamForOperate
                    WHERE id = @Id";

                await db.Ado.ExecuteCommandAsync(permissionSql, new
                {
                    ViewTeams = entity.ViewTeams,
                    ViewUsers = entity.ViewUsers,
                    OperateTeams = entity.OperateTeams,
                    OperateUsers = entity.OperateUsers,
                    ViewPermissionMode = (int)entity.ViewPermissionMode,
                    ViewPermissionSubjectType = (int)entity.ViewPermissionSubjectType,
                    OperatePermissionSubjectType = (int)entity.OperatePermissionSubjectType,
                    UseSameTeamForOperate = entity.UseSameTeamForOperate,
                    Id = entity.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update permission JSONB fields for onboarding {OnboardingId}", entity.Id);
                throw new CRMException($"Failed to update permission fields: {ex.Message}");
            }
        }

        /// <summary>
        /// Prepare audit information
        /// </summary>
        private AuditInfo PrepareAuditInfo()
        {
            return new AuditInfo(
                DateTimeOffset.UtcNow,
                _operatorContextService.GetOperatorDisplayName(),
                _operatorContextService.GetOperatorId()
            );
        }

        /// <summary>
        /// Update main entity fields using repository
        /// </summary>
        private async Task<bool> UpdateMainEntityFieldsAsync(Domain.Entities.OW.Onboarding entity)
        {
            return await _onboardingRepository.UpdateAsync(entity,
                it => new
                {
                    it.WorkflowId,
                    it.CurrentStageId,
                    it.CurrentStageOrder,
                    it.LeadId,
                    it.CaseName,
                    it.LeadEmail,
                    it.LeadPhone,
                    it.ContactPerson,
                    it.ContactEmail,
                    it.LifeCycleStageId,
                    it.LifeCycleStageName,
                    it.Status,
                    it.CompletionRate,
                    it.StartDate,
                    it.EstimatedCompletionDate,
                    it.ActualCompletionDate,
                    it.CurrentAssigneeId,
                    it.CurrentAssigneeName,
                    it.CurrentTeam,
                    it.StageUpdatedById,
                    it.StageUpdatedBy,
                    it.StageUpdatedByEmail,
                    it.StageUpdatedTime,
                    it.CurrentStageStartTime,
                    it.Priority,
                    it.IsPrioritySet,
                    it.Ownership,
                    it.OwnershipName,
                    it.OwnershipEmail,
                    it.Notes,
                    it.IsActive,
                    it.ModifyDate,
                    it.ModifyBy,
                    it.ModifyUserId,
                    it.IsValid
                });
        }

        /// <summary>
        /// Update audit fields via direct SQL
        /// </summary>
        private async Task UpdateAuditFieldsAsync(ISqlSugarClient db, long entityId, AuditInfo auditInfo)
        {
            try
            {
                var auditSql = @"
                    UPDATE ff_onboarding 
                    SET modify_date = @ModifyDate,
                        modify_by = @ModifyBy,
                        modify_user_id = @ModifyUserId
                    WHERE id = @Id";

                await db.Ado.ExecuteCommandAsync(auditSql, new
                {
                    ModifyDate = auditInfo.ModifyDate,
                    ModifyBy = auditInfo.ModifyBy,
                    ModifyUserId = auditInfo.ModifyUserId,
                    Id = entityId
                });

                _logger.LogDebug("Audit fields updated for onboarding {OnboardingId}: ModifyBy='{ModifyBy}'",
                    entityId, auditInfo.ModifyBy);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update audit fields for onboarding {OnboardingId}", entityId);
                // Don't fail the entire update if audit field update fails
            }
        }

        /// <summary>
        /// Update stages_progress_json with JSONB casting
        /// </summary>
        private async Task UpdateStagesProgressJsonAsync(ISqlSugarClient db, Domain.Entities.OW.Onboarding entity)
        {
            if (string.IsNullOrEmpty(entity.StagesProgressJson))
            {
                return;
            }

            try
            {
                var progressSql = "UPDATE ff_onboarding SET stages_progress_json = @StagesProgressJson::jsonb WHERE id = @Id";
                await db.Ado.ExecuteCommandAsync(progressSql, new
                {
                    StagesProgressJson = entity.StagesProgressJson,
                    Id = entity.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update stages_progress_json with parameterized query for onboarding {OnboardingId}", entity.Id);
                
                // Try alternative approach with direct SQL
                try
                {
                    var escapedJson = entity.StagesProgressJson.Replace("'", "''");
                    var directSql = $"UPDATE ff_onboarding SET stages_progress_json = '{escapedJson}'::jsonb WHERE id = {entity.Id}";
                    await db.Ado.ExecuteCommandAsync(directSql);
                }
                catch (Exception directEx)
                {
                    _logger.LogError(directEx, "Both parameterized and direct JSONB update failed for onboarding {OnboardingId}", entity.Id);
                }
            }
        }

        #endregion

        #region UpdateOnboardingStageAISummaryAsync

        /// <inheritdoc />
        public async Task<bool> UpdateOnboardingStageAISummaryAsync(long onboardingId, long stageId, string aiSummary, DateTime generatedAt, double? confidence, string modelUsed)
        {
            try
            {
                // Get current onboarding without tenant filter (for background tasks where HttpContext is not available)
                var onboarding = await _onboardingRepository.GetByIdWithoutTenantFilterAsync(onboardingId);
                if (onboarding == null)
                {
                    _logger.LogWarning("Onboarding {OnboardingId} not found for AI summary update", onboardingId);
                    return false;
                }

                // Sync stages progress with workflow to ensure all stages are included
                await SyncStagesProgressWithWorkflowAsync(onboarding);

                // Load stages progress from JSON (after sync)
                LoadStagesProgressFromJson(onboarding);

                // Find the stage progress entry
                var stageProgress = onboarding.StagesProgress?.FirstOrDefault(sp => sp.StageId == stageId);
                if (stageProgress == null)
                {
                    _logger.LogWarning("Stage progress not found for stage {StageId} in onboarding {OnboardingId}. Available stages: {AvailableStages}",
                        stageId, onboardingId, string.Join(", ", onboarding.StagesProgress?.Select(sp => sp.StageId.ToString()) ?? Array.Empty<string>()));
                    return false;
                }

                // Update AI summary fields - always overwrite for Onboarding-specific summaries
                stageProgress.AiSummary = aiSummary;
                stageProgress.AiSummaryGeneratedAt = generatedAt;
                stageProgress.AiSummaryConfidence = (decimal?)confidence;
                stageProgress.AiSummaryModel = modelUsed;
                stageProgress.AiSummaryData = JsonSerializer.Serialize(new
                {
                    trigger = "Stream API onboarding update",
                    generatedAt = generatedAt,
                    confidence = confidence,
                    model = modelUsed,
                    onboardingSpecific = true
                }, JsonOptions);

                // Save stages progress back to JSON
                onboarding.StagesProgressJson = SerializeStagesProgress(onboarding.StagesProgress);

                // Update only stages_progress_json in database WITHOUT updating modifyBy/modifyDate/modifyUserId
                // AI summary updates are system-generated and should not affect audit fields
                var result = await UpdateStagesProgressJsonOnlyAsync(onboarding.Id, onboarding.StagesProgressJson);

                if (result)
                {
                    _logger.LogInformation("Successfully updated AI summary for stage {StageId} in onboarding {OnboardingId}", stageId, onboardingId);
                }
                else
                {
                    _logger.LogWarning("Failed to save AI summary for stage {StageId} in onboarding {OnboardingId} - database update failed", stageId, onboardingId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update AI summary for stage {StageId} in onboarding {OnboardingId}", stageId, onboardingId);
                return false;
            }
        }

        /// <summary>
        /// Update only stages_progress_json field without modifying audit fields (modifyBy, modifyDate, modifyUserId)
        /// Used for system-generated updates like AI summary that should not affect audit trail
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stagesProgressJson">Serialized stages progress JSON</param>
        /// <returns>Success status</returns>
        private async Task<bool> UpdateStagesProgressJsonOnlyAsync(long onboardingId, string stagesProgressJson)
        {
            if (string.IsNullOrEmpty(stagesProgressJson))
            {
                return true;
            }

            try
            {
                var db = _onboardingRepository.GetSqlSugarClient();
                var sql = "UPDATE ff_onboarding SET stages_progress_json = @StagesProgressJson::jsonb WHERE id = @Id";
                var rowsAffected = await db.Ado.ExecuteCommandAsync(sql, new
                {
                    StagesProgressJson = stagesProgressJson,
                    Id = onboardingId
                });

                _logger.LogDebug("Updated stages_progress_json only for onboarding {OnboardingId}, rows affected: {RowsAffected}", 
                    onboardingId, rowsAffected);

                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update stages_progress_json for onboarding {OnboardingId}", onboardingId);
                return false;
            }
        }

        #endregion

        #region UpdateStageCustomFieldsAsync

        /// <inheritdoc />
        public async Task<bool> UpdateStageCustomFieldsAsync(long onboardingId, UpdateStageCustomFieldsInputDto input)
        {
            try
            {
                // Get current onboarding
                var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
                if (onboarding == null)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
                }

                // Ensure stages progress is properly initialized and synced
                // This handles cases where stagesProgress is empty or outdated
                await EnsureStagesProgressInitializedAsync(onboarding);

                // Find the stage progress entry
                var stageProgress = onboarding.StagesProgress?.FirstOrDefault(sp => sp.StageId == input.StageId);
                if (stageProgress == null)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, $"Stage {input.StageId} not found in onboarding {onboardingId}");
                }

                // Capture original values for comparison
                var originalEstimatedDays = stageProgress.CustomEstimatedDays;
                var originalEndTime = stageProgress.CustomEndTime;
                var originalCustomAssignee = stageProgress.CustomStageAssignee?.ToList() ?? new List<string>();
                var originalCustomCoAssignees = stageProgress.CustomStageCoAssignees?.ToList() ?? new List<string>();

                // Capture before data for change log
                var beforeData = JsonSerializer.Serialize(new
                {
                    CustomEstimatedDays = stageProgress.CustomEstimatedDays,
                    CustomEndTime = stageProgress.CustomEndTime,
                    CustomStageAssignee = stageProgress.CustomStageAssignee,
                    CustomStageCoAssignees = stageProgress.CustomStageCoAssignees,
                    LastUpdatedTime = stageProgress.LastUpdatedTime,
                    LastUpdatedBy = stageProgress.LastUpdatedBy
                }, JsonOptions);

                // Update custom fields - normalize estimated days to integer and end time to start of day
                stageProgress.CustomEstimatedDays = OnboardingSharedUtilities.NormalizeEstimatedDays(input.CustomEstimatedDays);
                stageProgress.CustomEndTime = OnboardingSharedUtilities.NormalizeToStartOfDay(input.CustomEndTime);

                // Update CustomStageAssignee if Assignee is provided (frontend uses Assignee field)
                if (input.Assignee != null)
                {
                    stageProgress.CustomStageAssignee = input.Assignee;
                }

                // Update CustomStageCoAssignees if CoAssignees is provided (frontend uses CoAssignees field)
                if (input.CoAssignees != null)
                {
                    stageProgress.CustomStageCoAssignees = input.CoAssignees;
                }

                // Add notes if provided
                if (!string.IsNullOrEmpty(input.Notes))
                {
                    var currentTime = DateTimeOffset.UtcNow;
                    var currentUser = GetCurrentUserName();
                    var updateNote = $"[Custom fields updated {currentTime:yyyy-MM-dd HH:mm:ss} by {currentUser}] {input.Notes}";

                    if (string.IsNullOrEmpty(stageProgress.Notes))
                    {
                        stageProgress.Notes = updateNote;
                    }
                    else
                    {
                        stageProgress.Notes += $"\n{updateNote}";
                    }
                }

                // Update last modified fields
                stageProgress.LastUpdatedTime = DateTimeOffset.UtcNow;
                stageProgress.LastUpdatedBy = GetCurrentUserName();

                // Capture after data for change log
                var afterData = JsonSerializer.Serialize(new
                {
                    CustomEstimatedDays = stageProgress.CustomEstimatedDays,
                    CustomEndTime = stageProgress.CustomEndTime,
                    CustomStageAssignee = stageProgress.CustomStageAssignee,
                    CustomStageCoAssignees = stageProgress.CustomStageCoAssignees,
                    LastUpdatedTime = stageProgress.LastUpdatedTime,
                    LastUpdatedBy = stageProgress.LastUpdatedBy
                }, JsonOptions);

                // Save stages progress back to JSON
                onboarding.StagesProgressJson = SerializeStagesProgress(onboarding.StagesProgress);

                // Update in database
                var result = await SafeUpdateOnboardingAsync(onboarding);

                // Log the operation if update was successful
                if (result)
                {
                    await LogStageCustomFieldsChangeAsync(
                        onboardingId, 
                        input, 
                        originalEstimatedDays, 
                        originalEndTime, 
                        originalCustomAssignee, 
                        originalCustomCoAssignees, 
                        beforeData, 
                        afterData);
                }

                return result;
            }
            catch (CRMException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update custom fields for stage {StageId} in onboarding {OnboardingId}", input.StageId, onboardingId);
                throw new CRMException(ErrorCodeEnum.SystemError, $"Failed to update custom fields for stage {input.StageId} in onboarding {onboardingId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Log stage custom fields change to operation change log
        /// </summary>
        private async Task LogStageCustomFieldsChangeAsync(
            long onboardingId,
            UpdateStageCustomFieldsInputDto input,
            decimal? originalEstimatedDays,
            DateTimeOffset? originalEndTime,
            List<string> originalCustomAssignee,
            List<string> originalCustomCoAssignees,
            string beforeData,
            string afterData)
        {
            var changedFields = new List<string>();
            var changeDetails = new List<string>();

            // Collect all user IDs that need name resolution
            var allUserIds = new HashSet<long>();
            
            // Add user IDs from assignee changes
            foreach (var id in originalCustomAssignee.Concat(input.Assignee ?? new List<string>())
                .Concat(originalCustomCoAssignees).Concat(input.CoAssignees ?? new List<string>()))
            {
                if (long.TryParse(id, out var userId))
                {
                    allUserIds.Add(userId);
                }
            }

            // Fetch user names for all IDs
            var userNameMap = new Dictionary<long, string>();
            if (allUserIds.Any())
            {
                try
                {
                    var tenantId = _userContext?.TenantId ?? "default";
                    var users = await _userService.GetUsersByIdsAsync(allUserIds.ToList(), tenantId);
                    userNameMap = users
                        .GroupBy(u => u.Id)
                        .ToDictionary(
                            g => g.Key,
                            g =>
                            {
                                var user = g.First();
                                return !string.IsNullOrEmpty(user.Username) ? user.Username :
                                       (!string.IsNullOrEmpty(user.Email) ? user.Email : $"User_{user.Id}");
                            }
                        );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch user names for stage custom fields change log");
                }
            }

            // Helper function to convert user IDs to names
            string GetUserDisplayNames(IEnumerable<string> userIds)
            {
                if (userIds == null || !userIds.Any()) return "empty";
                var names = userIds
                    .Select(id => long.TryParse(id, out var userId) && userNameMap.TryGetValue(userId, out var name) ? name : id)
                    .ToList();
                return string.Join(", ", names);
            }

            // Check for actual changes in CustomEstimatedDays
            if (input.CustomEstimatedDays.HasValue && originalEstimatedDays != input.CustomEstimatedDays)
            {
                changedFields.Add("CustomEstimatedDays");
                var beforeValue = originalEstimatedDays?.ToString() ?? "null";
                changeDetails.Add($"EstimatedDays: {beforeValue} → {input.CustomEstimatedDays}");
            }

            // Check for actual changes in CustomEndTime
            if (input.CustomEndTime.HasValue && originalEndTime != input.CustomEndTime)
            {
                changedFields.Add("CustomEndTime");
                var beforeValue = originalEndTime?.ToString("yyyy-MM-dd HH:mm") ?? "null";
                changeDetails.Add($"EndTime: {beforeValue} → {input.CustomEndTime?.ToString("yyyy-MM-dd HH:mm")}");
            }

            // Check for actual changes in CustomStageAssignee (input uses Assignee field)
            if (input.Assignee != null && !originalCustomAssignee.SequenceEqual(input.Assignee))
            {
                changedFields.Add("CustomStageAssignee");
                var beforeValue = GetUserDisplayNames(originalCustomAssignee);
                var afterValue = GetUserDisplayNames(input.Assignee);
                changeDetails.Add($"Assignee: {beforeValue} → {afterValue}");
            }

            // Check for actual changes in CustomStageCoAssignees (input uses CoAssignees field)
            if (input.CoAssignees != null && !originalCustomCoAssignees.SequenceEqual(input.CoAssignees))
            {
                changedFields.Add("CustomStageCoAssignees");
                var beforeValue = GetUserDisplayNames(originalCustomCoAssignees);
                var afterValue = GetUserDisplayNames(input.CoAssignees);
                changeDetails.Add($"CoAssignees: {beforeValue} → {afterValue}");
            }

            // Check if notes were added
            if (!string.IsNullOrEmpty(input.Notes))
            {
                changedFields.Add("Notes");
                changeDetails.Add("Notes: Added");
            }

            // Log as Stage operation with onboardingId to associate with Case
            // Use BusinessModule.Stage because legacy adapter doesn't support Onboarding module
            if (changeDetails.Any())
            {
                var operationTitle = $"Update Stage Custom Fields: {string.Join(", ", changeDetails)}";
                var operationDescription = $"Updated custom fields for stage {input.StageId} in case {onboardingId}";

                // Log the case stage custom fields update operation
                await _operationChangeLogService.LogOperationAsync(
                    operationType: OperationTypeEnum.StageUpdate,
                    businessModule: BusinessModuleEnum.Stage,
                    businessId: input.StageId,
                    onboardingId: onboardingId,
                    stageId: input.StageId,
                    operationTitle: operationTitle,
                    operationDescription: operationDescription,
                    beforeData: beforeData,
                    afterData: afterData,
                    changedFields: changedFields,
                    extendedData: JsonSerializer.Serialize(new
                    {
                        Notes = input.Notes,
                        OperationSource = "UpdateStageCustomFieldsAsync",
                        HasActualChanges = true
                    }, JsonOptions)
                );
            }
        }

        #endregion

        #region SaveStageAsync

        /// <inheritdoc />
        public async Task<bool> SaveStageAsync(long onboardingId, long stageId)
        {
            // Check permission
            await OnboardingSharedUtilities.EnsureCaseOperatePermissionAsync(_permissionService, _userContext, onboardingId);

            try
            {
                // Get current onboarding
                var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
                if (onboarding == null)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
                }

                // Ensure stages progress is properly initialized and synced
                // This handles cases where stagesProgress is empty or outdated
                await EnsureStagesProgressInitializedAsync(onboarding);

                // Find the stage progress entry
                var stageProgress = onboarding.StagesProgress?.FirstOrDefault(sp => sp.StageId == stageId);
                if (stageProgress == null)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, $"Stage {stageId} not found in onboarding {onboardingId}");
                }

                // Update save fields
                stageProgress.IsSaved = true;
                stageProgress.SaveTime = DateTimeOffset.UtcNow;
                stageProgress.SavedById = GetCurrentUserId()?.ToString();
                stageProgress.SavedBy = GetCurrentUserName();

                // Set StartTime if not already set (only during save operations)
                // This ensures StartTime is only set when user actually saves or completes work
                // Normalize to start of day (00:00:00)
                if (!stageProgress.StartTime.HasValue)
                {
                    stageProgress.StartTime = OnboardingSharedUtilities.GetNormalizedUtcNowOffset();
                }

                // IMPORTANT: If this is the current stage and CurrentStageStartTime is not set, set it now
                // Normalize to start of day (00:00:00)
                if (stageProgress.StageId == onboarding.CurrentStageId && !onboarding.CurrentStageStartTime.HasValue)
                {
                    onboarding.CurrentStageStartTime = OnboardingSharedUtilities.NormalizeToStartOfDay(stageProgress.StartTime);
                    _logger.LogDebug("SaveStageAsync - Set CurrentStageStartTime to {StartTime} for Stage {StageId}", 
                        onboarding.CurrentStageStartTime, stageId);
                }

                // Save stages progress back to JSON
                await FilterValidStagesProgressAsync(onboarding);
                onboarding.StagesProgressJson = SerializeStagesProgress(onboarding.StagesProgress);

                // Update in database
                var result = await SafeUpdateOnboardingAsync(onboarding);

                // Log stage save to operation_change_log
                if (result)
                {
                    await LogStageSaveAsync(onboarding, stageId, stageProgress);
                }

                return result;
            }
            catch (CRMException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CRMException(ErrorCodeEnum.SystemError, $"Failed to save stage {stageId} in onboarding {onboardingId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Log stage save operation to operation change log
        /// </summary>
        private async Task LogStageSaveAsync(Domain.Entities.OW.Onboarding onboarding, long stageId, OnboardingStageProgress stageProgress)
        {
            try
            {
                var stage = await _stageRepository.GetByIdAsync(stageId);
                var beforeData = new
                {
                    StageId = stageId,
                    StageName = stage?.Name,
                    IsSaved = false,
                    SaveTime = (DateTimeOffset?)null
                };

                var afterData = new
                {
                    StageId = stageId,
                    StageName = stage?.Name,
                    IsSaved = true,
                    SaveTime = stageProgress.SaveTime,
                    SavedBy = stageProgress.SavedBy,
                    StartTime = stageProgress.StartTime
                };

                var extendedData = new
                {
                    WorkflowId = onboarding.WorkflowId,
                    IsCurrentStage = stageProgress.StageId == onboarding.CurrentStageId,
                    CurrentStageStartTime = onboarding.CurrentStageStartTime,
                    Source = "manual_save"
                };

                await _operationChangeLogService.LogOperationAsync(
                    operationType: OperationTypeEnum.StageSave,
                    businessModule: BusinessModuleEnum.Stage,
                    businessId: stageId,
                    onboardingId: onboarding.Id,
                    stageId: stageId,
                    operationTitle: $"Stage Saved: {stage?.Name ?? "Unknown"}",
                    operationDescription: $"Stage '{stage?.Name}' has been saved by {stageProgress.SavedBy}",
                    beforeData: JsonSerializer.Serialize(beforeData, JsonOptions),
                    afterData: JsonSerializer.Serialize(afterData, JsonOptions),
                    changedFields: new List<string> { "IsSaved", "SaveTime", "SavedBy" },
                    extendedData: JsonSerializer.Serialize(extendedData, JsonOptions)
                );

                _logger.LogInformation("Stage save log recorded: OnboardingId={OnboardingId}, StageId={StageId}, StageName={StageName}",
                    onboarding.Id, stageId, stage?.Name);
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "Failed to record Stage save log: OnboardingId={OnboardingId}, StageId={StageId}",
                    onboarding.Id, stageId);
                // Don't re-throw to avoid breaking the main flow
            }
        }

        #endregion

        #region Helper Methods

        // Note: NormalizeToStartOfDay, NormalizeEstimatedDays, and GetNormalizedUtcNow
        // have been removed. Use OnboardingSharedUtilities methods directly:
        // - OnboardingSharedUtilities.NormalizeToStartOfDay(dateTime)
        // - OnboardingSharedUtilities.NormalizeEstimatedDays(days)
        // - OnboardingSharedUtilities.GetNormalizedUtcNowOffset()

        /// <summary>
        /// Get current user name from OperatorContextService
        /// </summary>
        private string GetCurrentUserName()
            => _operatorContextService.GetOperatorDisplayName();

        /// <summary>
        /// Get current user ID from OperatorContextService
        /// </summary>
        private long? GetCurrentUserId()
        {
            var id = _operatorContextService.GetOperatorId();
            return id == 0 ? null : id;
        }

        #endregion
    }
}
