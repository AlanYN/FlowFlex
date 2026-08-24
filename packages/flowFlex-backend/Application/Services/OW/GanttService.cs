using FlowFlex.Application.Contracts.Dtos.OW.Gantt;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices.OW.Onboarding;
using FlowFlex.Application.Helpers.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Events;
using FlowFlex.Domain.Shared.Helpers;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Enums.OW;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Collections.Generic;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Gantt chart service — data query and blocker management.
    /// Implements GetGanttDataAsync, BlockStageAsync, UnblockStageAsync.
    /// </summary>
    public class GanttService : IGanttService, IScopedService
    {
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IStageRepository _stageRepository;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IOnboardingPermissionService _permissionService;
        private readonly IOnboardingStageProgressService _stageProgressService;
        private readonly IUserService _userService;
        private readonly IMediator _mediator;
        private readonly UserContext _userContext;
        private readonly ILogger<GanttService> _logger;
        private readonly IOperationChangeLogService _operationChangeLogService;
        private readonly IChecklistTaskCompletionRepository _checklistTaskCompletionRepository;
        private readonly IChecklistTaskRepository _checklistTaskRepository;
        private readonly IQuestionnaireAnswerRepository _questionnaireAnswerRepository;
        private readonly IStaticFieldValueRepository _staticFieldValueRepository;
        private readonly IOnboardingFileRepository _onboardingFileRepository;

        // Shared JSON options for consistency with rest of application
        private static readonly JsonSerializerOptions JsonOptions = OnboardingSharedUtilities.JsonOptions;

        public GanttService(
            IOnboardingRepository onboardingRepository,
            IStageRepository stageRepository,
            IWorkflowRepository workflowRepository,
            IOnboardingPermissionService permissionService,
            IOnboardingStageProgressService stageProgressService,
            IUserService userService,
            IMediator mediator,
            UserContext userContext,
            IOperationChangeLogService operationChangeLogService,
            IChecklistTaskCompletionRepository checklistTaskCompletionRepository,
            IChecklistTaskRepository checklistTaskRepository,
            IQuestionnaireAnswerRepository questionnaireAnswerRepository,
            IStaticFieldValueRepository staticFieldValueRepository,
            IOnboardingFileRepository onboardingFileRepository,
            ILogger<GanttService> logger)
        {
            _onboardingRepository = onboardingRepository ?? throw new ArgumentNullException(nameof(onboardingRepository));
            _stageRepository = stageRepository ?? throw new ArgumentNullException(nameof(stageRepository));
            _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _stageProgressService = stageProgressService ?? throw new ArgumentNullException(nameof(stageProgressService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _operationChangeLogService = operationChangeLogService ?? throw new ArgumentNullException(nameof(operationChangeLogService));
            _checklistTaskCompletionRepository = checklistTaskCompletionRepository ?? throw new ArgumentNullException(nameof(checklistTaskCompletionRepository));
            _checklistTaskRepository = checklistTaskRepository ?? throw new ArgumentNullException(nameof(checklistTaskRepository));
            _questionnaireAnswerRepository = questionnaireAnswerRepository ?? throw new ArgumentNullException(nameof(questionnaireAnswerRepository));
            _staticFieldValueRepository = staticFieldValueRepository ?? throw new ArgumentNullException(nameof(staticFieldValueRepository));
            _onboardingFileRepository = onboardingFileRepository ?? throw new ArgumentNullException(nameof(onboardingFileRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region GetGanttDataAsync

        /// <inheritdoc />
        public async Task<GanttDataResponseDto> GetGanttDataAsync(long onboardingId)
        {
            // Step 1: Permission check
            await _permissionService.EnsureCaseViewPermissionAsync(onboardingId);

            // Step 2: Load onboarding entity
            var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
            if (onboarding == null || !onboarding.IsValid)
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");

            // Step 3: Load workflow to get its name
            Workflow workflow = null;
            try
            {
                workflow = await _workflowRepository.GetByIdAsync(onboarding.WorkflowId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load workflow {WorkflowId} for onboarding {OnboardingId}", onboarding.WorkflowId, onboardingId);
            }

            // Step 4: Load stages ordered by order_index ascending
            var stages = (await _stageRepository.GetByWorkflowIdAsync(onboarding.WorkflowId))
                         .OrderBy(s => s.Order)
                         .ToList();

            // Step 5: Deserialize StagesProgressJson
            _stageProgressService.LoadStagesProgressFromJsonReadOnly(onboarding);
            var stagesProgress = onboarding.StagesProgress ?? new List<OnboardingStageProgress>();

            // Enrich stageProgress with stage metadata (StageName, StageOrder, EstimatedDays, Color, Required, Components)
            _stageProgressService.EnrichStagesProgressWithStageData(onboarding, stages);
            stagesProgress = onboarding.StagesProgress ?? stagesProgress;

            // Step 5 (continued): Build stageId → Stage lookup
            var stageDict = stages.ToDictionary(s => s.Id, s => s);

            // Step 5 (old-case fallback): if first stage has no plannedStartDate → compute dynamically (do NOT write to DB)
            var orderedProgress = stagesProgress.OrderBy(sp => sp.StageOrder).ToList();

            Dictionary<long, (DateTimeOffset plannedStart, DateTimeOffset plannedEnd)> plannedTimeMap = null;
            var firstProgress = orderedProgress.FirstOrDefault();
            bool needsFallback = firstProgress != null && !firstProgress.PlannedStartDate.HasValue;

            if (needsFallback && onboarding.StartDate.HasValue)
            {
                plannedTimeMap = ComputePlannedTimes(stages, onboarding.StartDate.Value, onboarding.EstimatedCompletionDate);
            }

            // Step 5b: Batch-fetch all completion data for real completionPercentage / GanttComponentsDto
            var allStageIds = orderedProgress.Select(sp => sp.StageId).ToList();
            var onboardingIdLong = onboarding.Id;

            // Checklist task completions — parallel fetch per stage, then union
            Dictionary<long, List<ChecklistTaskCompletion>> completionsByStage;
            try
            {
                var completionTasks = allStageIds
                    .Select(sid => _checklistTaskCompletionRepository.GetByOnboardingAndStageAsync(onboardingIdLong, sid))
                    .ToList();
                var completionResults = await Task.WhenAll(completionTasks);
                completionsByStage = allStageIds
                    .Zip(completionResults, (sid, records) => (sid, records))
                    .ToDictionary(x => x.sid, x => x.records ?? new List<ChecklistTaskCompletion>());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[GanttService] Failed to batch-fetch checklist completions for onboarding {OnboardingId}", onboardingIdLong);
                completionsByStage = new Dictionary<long, List<ChecklistTaskCompletion>>();
            }

            // Checklist task definitions
            List<ChecklistTask> allChecklistTasks;
            try
            {
                var allChecklistIds = orderedProgress
                    .Where(sp => stageDict.ContainsKey(sp.StageId))
                    .SelectMany(sp => ParseStageComponents(stageDict[sp.StageId].ComponentsJson)
                        .Where(c => c.Key?.ToLowerInvariant() == "checklist")
                        .SelectMany(c => c.ChecklistIds ?? new List<long>()))
                    .Distinct()
                    .ToList();
                allChecklistTasks = allChecklistIds.Any()
                    ? await _checklistTaskRepository.GetByChecklistIdsAsync(allChecklistIds)
                    : new List<ChecklistTask>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[GanttService] Failed to batch-fetch checklist tasks for onboarding {OnboardingId}", onboardingIdLong);
                allChecklistTasks = new List<ChecklistTask>();
            }

            // Questionnaire answers
            Dictionary<(long stageId, long questionnaireId), QuestionnaireAnswer> answerLookup;
            try
            {
                var questionnaireAnswers = await _questionnaireAnswerRepository
                    .GetByOnboardingAndStageIdsAsync(onboardingIdLong, allStageIds);
                // For each (stageId, questionnaireId) pair keep the most-recently submitted answer,
                // falling back to any answer if none is submitted.
                answerLookup = questionnaireAnswers
                    .GroupBy(a => (a.StageId, a.QuestionnaireId ?? 0L))
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(a => a.Status == "Submitted" || a.Status == "Approved")
                               .ThenByDescending(a => a.CreateDate)
                               .First());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[GanttService] Failed to batch-fetch questionnaire answers for onboarding {OnboardingId}", onboardingIdLong);
                answerLookup = new Dictionary<(long, long), QuestionnaireAnswer>();
            }

            // Static field values
            Dictionary<long, List<StaticFieldValue>> fieldValuesByStage;
            try
            {
                var allFieldValues = await _staticFieldValueRepository.GetByOnboardingIdAsync(onboardingIdLong);
                fieldValuesByStage = allFieldValues
                    .GroupBy(fv => fv.StageId)
                    .ToDictionary(g => g.Key, g => g.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[GanttService] Failed to batch-fetch static field values for onboarding {OnboardingId}", onboardingIdLong);
                fieldValuesByStage = new Dictionary<long, List<StaticFieldValue>>();
            }

            // Uploaded files per stage
            Dictionary<long, int> fileCountByStage;
            try
            {
                var allFiles = await _onboardingFileRepository.GetFilesByOnboardingAsync(onboardingIdLong, null);
                fileCountByStage = allFiles
                    .Where(f => f.StageId.HasValue)
                    .GroupBy(f => f.StageId!.Value)
                    .ToDictionary(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[GanttService] Failed to batch-fetch uploaded files for onboarding {OnboardingId}", onboardingIdLong);
                fileCountByStage = new Dictionary<long, int>();
            }

            // Build O(1) lookup: checklistId → tasks
            var tasksByChecklist = allChecklistTasks
                .GroupBy(t => t.ChecklistId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Step 6 & 7: Collect all assignee/co-assignee user IDs for batch resolution
            var allUserIdStrings = orderedProgress
                .SelectMany(sp => (sp.CustomStageAssignee?.Any() == true ? sp.CustomStageAssignee : sp.Assignee) ?? new List<string>())
                .Concat(orderedProgress.SelectMany(sp => (sp.CustomStageCoAssignees?.Any() == true ? sp.CustomStageCoAssignees : sp.CoAssignees) ?? new List<string>()))
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            // Step 8: Resolve user IDs to UserDto (batch)
            var userMap = await ResolveUserIdsAsync(allUserIdStrings);

            // Today normalized to start of day (UTC)
            var today = OnboardingSharedUtilities.GetNormalizedUtcNowOffset();

            // Step 6 & 7: Build stage items
            var stageItems = new List<GanttStageItemDto>();
            foreach (var sp in orderedProgress)
            {
                if (!stageDict.TryGetValue(sp.StageId, out var stage))
                    continue;

                // Resolve planned dates (persisted or fallback-computed)
                DateTimeOffset? plannedStart = sp.PlannedStartDate;
                DateTimeOffset? plannedEnd = sp.PlannedEndDate;
                if (!plannedStart.HasValue && plannedTimeMap != null && plannedTimeMap.TryGetValue(sp.StageId, out var pt))
                {
                    plannedStart = pt.plannedStart;
                    plannedEnd = pt.plannedEnd;
                }

                // Compute completionPercentage
                var completionPct = ComputeCompletionPercentage(stage, sp, completionsByStage, tasksByChecklist, answerLookup, fieldValuesByStage);

                // Derive ganttStatus
                var ganttStatus = DeriveGanttStageStatus(sp, plannedStart, plannedEnd, today);

                // Resolve assignees
                var effectiveAssignee = sp.CustomStageAssignee?.Any() == true ? sp.CustomStageAssignee : sp.Assignee;
                var effectiveCoAssignee = sp.CustomStageCoAssignees?.Any() == true ? sp.CustomStageCoAssignees : sp.CoAssignees;

                var assigneeDtos = BuildAssigneeDtos(effectiveAssignee, userMap);
                var coAssigneeDtos = BuildAssigneeDtos(effectiveCoAssignee, userMap);

                // Estimated duration days
                var estimatedDurationDays = stage.EstimatedDuration.HasValue && stage.EstimatedDuration > 0
                    ? (int)Math.Round((double)stage.EstimatedDuration.Value)
                    : 0;

                // DaysElapsed
                int? daysElapsed = null;
                if (sp.StartTime.HasValue)
                    daysElapsed = (int)Math.Round((today - OnboardingSharedUtilities.NormalizeToStartOfDay(sp.StartTime.Value)).TotalDays);

                // Variance fields (only for Completed stages)
                int? inheritedDelayDays = null;
                int? ownVarianceDays = null;
                int? totalVarianceDays = null;
                if (sp.IsCompleted)
                {
                    // Use persisted values if available; otherwise compute on the fly
                    if (sp.InheritedDelayDays.HasValue)
                    {
                        inheritedDelayDays = sp.InheritedDelayDays;
                        ownVarianceDays = sp.OwnVarianceDays;
                        totalVarianceDays = sp.TotalVarianceDays;
                    }
                    else if (plannedStart.HasValue && sp.StartTime.HasValue)
                    {
                        inheritedDelayDays = (int)Math.Round(
                            (OnboardingSharedUtilities.NormalizeToStartOfDay(sp.StartTime.Value) - plannedStart.Value).TotalDays);

                        if (sp.CompletionTime.HasValue && sp.StartTime.HasValue)
                        {
                            var actualDuration = (int)Math.Round(
                                (OnboardingSharedUtilities.NormalizeToStartOfDay(sp.CompletionTime.Value) -
                                 OnboardingSharedUtilities.NormalizeToStartOfDay(sp.StartTime.Value)).TotalDays) + 1;
                            ownVarianceDays = actualDuration - estimatedDurationDays;
                        }

                        if (plannedEnd.HasValue && sp.CompletionTime.HasValue)
                        {
                            totalVarianceDays = (int)Math.Round(
                                (OnboardingSharedUtilities.NormalizeToStartOfDay(sp.CompletionTime.Value) - plannedEnd.Value).TotalDays);
                        }
                    }
                }

                // Blocker fields
                var blockedDays = ComputeBlockedDays(sp, today);
                string blockReason = null;
                string blockedByName = null;
                DateTimeOffset? blockedAt = null;
                DateTimeOffset? expectedResolutionDate = null;
                if (sp.IsBlocked)
                {
                    var activeBlocker = sp.BlockerHistory?.LastOrDefault(b => !b.BlockerResolvedDate.HasValue);
                    blockReason = activeBlocker?.BlockerReason;
                    blockedByName = activeBlocker?.BlockedByName;
                    blockedAt = activeBlocker?.BlockerStartDate;
                    expectedResolutionDate = activeBlocker?.ExpectedResolutionDate;
                }

                // Components summary
                var componentsSummary = BuildComponentsSummary(stage, sp, completionsByStage, tasksByChecklist, answerLookup, fieldValuesByStage, fileCountByStage);

                // Last saved audit
                string lastSavedBy = null;
                DateTimeOffset? lastSavedAt = null;
                if (sp.IsSaved)
                {
                    lastSavedBy = sp.SavedBy;
                    lastSavedAt = sp.SaveTime;
                }
                else if (!string.IsNullOrEmpty(sp.LastUpdatedBy))
                {
                    lastSavedBy = sp.LastUpdatedBy;
                    lastSavedAt = sp.LastUpdatedTime;
                }

                stageItems.Add(new GanttStageItemDto
                {
                    StageId = stage.Id.ToString(),
                    StageName = sp.StageName ?? stage.Name,
                    StageOrder = sp.StageOrder,
                    Color = sp.Color ?? stage.Color,
                    IsRequired = sp.Required,
                    GanttStatus = ganttStatus,
                    IsBlocked = sp.IsBlocked,
                    Assignee = assigneeDtos,
                    CoAssignees = coAssigneeDtos,
                    PlannedStartDate = plannedStart,
                    PlannedEndDate = plannedEnd,
                    ProjectedStartDate = sp.ProjectedStartDate,
                    ProjectedEndDate = sp.ProjectedEndDate,
                    ActualStartDate = sp.StartTime,
                    ActualEndDate = sp.CompletionTime,
                    EstimatedDurationDays = estimatedDurationDays,
                    CompletionPercentage = completionPct,
                    DaysElapsed = daysElapsed,
                    InheritedDelayDays = inheritedDelayDays,
                    OwnVarianceDays = ownVarianceDays,
                    TotalVarianceDays = totalVarianceDays,
                    BlockedDays = blockedDays,
                    BlockReason = blockReason,
                    BlockedByName = blockedByName,
                    BlockedAt = blockedAt,
                    ExpectedResolutionDate = expectedResolutionDate,
                    Components = componentsSummary,
                    LastSavedBy = lastSavedBy,
                    LastSavedAt = lastSavedAt
                });
            }

            // Step 9: Aggregate GanttCaseSummaryDto
            var completedCount = stageItems.Count(s => s.GanttStatus == "Completed");
            var overdueCount = stageItems.Count(s => s.GanttStatus == "Overdue");
            var delayedCount = stageItems.Count(s => s.GanttStatus == "Delayed");
            var blockedCount = stageItems.Count(s => s.IsBlocked);

            decimal overallCompletion = stageItems.Any()
                ? Math.Round(stageItems.Average(s => s.CompletionPercentage), 2)
                : 0m;

            // Planned dates from first and last stage items
            var firstStageItem = stageItems.OrderBy(s => s.StageOrder).FirstOrDefault();
            var lastStageItem = stageItems.OrderByDescending(s => s.StageOrder).FirstOrDefault();

            DateTimeOffset? summaryPlannedStart = firstStageItem?.PlannedStartDate;
            DateTimeOffset? summaryPlannedEnd = lastStageItem?.PlannedEndDate;
            DateTimeOffset? summaryProjectedEnd = lastStageItem?.ProjectedEndDate;

            // Current stage = first InProgress or first NotStarted
            var currentStageItem = stageItems.FirstOrDefault(s => s.GanttStatus == "InProgress")
                                ?? stageItems.FirstOrDefault(s => s.GanttStatus == "NotStarted");

            var summary = new GanttCaseSummaryDto
            {
                OnboardingId = onboarding.Id.ToString(),
                CaseName = onboarding.CaseName,
                CaseCode = onboarding.CaseCode,
                WorkflowName = workflow?.Name ?? string.Empty,
                Status = onboarding.Status,
                Priority = onboarding.Priority,
                PlannedStartDate = summaryPlannedStart,
                PlannedEndDate = summaryPlannedEnd,
                ProjectedEndDate = summaryProjectedEnd,
                ActualStartDate = onboarding.StartDate,
                ActualEndDate = onboarding.ActualCompletionDate,
                OverallCompletionPercentage = overallCompletion,
                TotalStages = stageItems.Count,
                CompletedStages = completedCount,
                OverdueStages = overdueCount,
                DelayedStages = delayedCount,
                BlockedStages = blockedCount,
                CurrentStageName = currentStageItem?.StageName,
                CurrentStageOrder = currentStageItem?.StageOrder ?? 0
            };

            return new GanttDataResponseDto
            {
                Summary = summary,
                Stages = stageItems
            };
        }

        #endregion

        #region BlockStageAsync / UnblockStageAsync

        /// <inheritdoc />
        public async Task<bool> BlockStageAsync(long onboardingId, BlockStageInputDto input)
        {
            await _permissionService.EnsureCaseOperatePermissionAsync(onboardingId);

            var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
            if (onboarding == null || !onboarding.IsValid)
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");

            _stageProgressService.LoadStagesProgressFromJson(onboarding);
            var stagesProgress = onboarding.StagesProgress ?? new List<OnboardingStageProgress>();

            var target = stagesProgress.FirstOrDefault(sp => sp.StageId == input.StageId);
            if (target == null)
                throw new CRMException(ErrorCodeEnum.DataNotFound, $"Stage {input.StageId} not found in this onboarding");

            if (target.IsBlocked)
                throw new CRMException(ErrorCodeEnum.BusinessError, "Stage is already blocked");

            target.IsBlocked = true;
            target.BlockerHistory ??= new List<BlockerRecord>();
            target.BlockerHistory.Add(new BlockerRecord
            {
                BlockerReason = input.BlockerReason,
                BlockerStartDate = DateTimeOffset.UtcNow,
                BlockedByName = _userContext.UserName,
                ExpectedResolutionDate = input.ExpectedResolutionDate
            });

            onboarding.StagesProgressJson = _stageProgressService.SerializeStagesProgress(stagesProgress);

            var db = _onboardingRepository.GetSqlSugarClient();
            await db.Ado.ExecuteCommandAsync(
                "UPDATE ff_onboarding SET stages_progress_json = @StagesProgressJson::jsonb WHERE id = @Id",
                new { StagesProgressJson = onboarding.StagesProgressJson, Id = onboarding.Id });

            // Trigger Projected time recalculation — fire-and-forget; errors are caught and logged by the handler
            try
            {
                await _mediator.Publish(new OnboardingStageMovedEvent
                {
                    OnboardingId = onboardingId,
                    FromStageId = input.StageId,
                    ToStageId = input.StageId,
                    TenantId = _userContext.TenantId,
                    UserId = long.TryParse(_userContext.UserId, out var blockUid) ? blockUid : 0,
                    UserName = _userContext.UserName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GanttService] Failed to publish projected recalc event after BlockStage for OnboardingId={OnboardingId}", onboardingId);
            }

            // Write changelog
            try
            {
                var stageEntity = await _stageRepository.GetByIdAsync(input.StageId);
                var stageName = stageEntity?.Name ?? input.StageId.ToString();
                var afterData = new { IsBlocked = true, BlockerReason = input.BlockerReason, BlockedByName = _userContext.UserName, BlockedAt = DateTimeOffset.UtcNow };
                await _operationChangeLogService.LogOperationAsync(
                    operationType: OperationTypeEnum.StageBlock,
                    businessModule: BusinessModuleEnum.Stage,
                    businessId: input.StageId,
                    onboardingId: onboardingId,
                    stageId: input.StageId,
                    operationTitle: $"Stage '{stageName}' blocked by {_userContext.UserName}" +
                        (!string.IsNullOrWhiteSpace(input.BlockerReason) ? $": {input.BlockerReason}" : ""),
                    operationDescription: $"Stage '{stageName}' has been marked as blocked by {_userContext.UserName}. Reason: {input.BlockerReason}",
                    beforeData: JsonSerializer.Serialize(new { IsBlocked = false }),
                    afterData: JsonSerializer.Serialize(afterData),
                    changedFields: new List<string> { "IsBlocked", "BlockerReason" },
                    extendedData: JsonSerializer.Serialize(new { Source = "manual_block", ExpectedResolutionDate = input.ExpectedResolutionDate })
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GanttService] Failed to write changelog after BlockStage for OnboardingId={OnboardingId}", onboardingId);
            }

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> UnblockStageAsync(long onboardingId, UnblockStageInputDto input)
        {
            await _permissionService.EnsureCaseOperatePermissionAsync(onboardingId);

            var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
            if (onboarding == null || !onboarding.IsValid)
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");

            _stageProgressService.LoadStagesProgressFromJson(onboarding);
            var stagesProgress = onboarding.StagesProgress ?? new List<OnboardingStageProgress>();

            var target = stagesProgress.FirstOrDefault(sp => sp.StageId == input.StageId);
            if (target == null)
                throw new CRMException(ErrorCodeEnum.DataNotFound, $"Stage {input.StageId} not found in this onboarding");

            if (!target.IsBlocked)
                throw new CRMException(ErrorCodeEnum.BusinessError, "Stage is not blocked");

            target.IsBlocked = false;

            // Fill resolution details on the latest open blocker record
            var latestBlocker = target.BlockerHistory?.LastOrDefault(b => !b.BlockerResolvedDate.HasValue);
            if (latestBlocker != null)
            {
                var now = DateTimeOffset.UtcNow;
                latestBlocker.BlockerResolvedDate = now;
                latestBlocker.ResolutionNotes = input.ResolutionNotes;
                if (latestBlocker.BlockerStartDate.HasValue)
                    latestBlocker.BlockedDays = (int)Math.Round((now - latestBlocker.BlockerStartDate.Value).TotalDays);
            }

            onboarding.StagesProgressJson = _stageProgressService.SerializeStagesProgress(stagesProgress);

            var db = _onboardingRepository.GetSqlSugarClient();
            await db.Ado.ExecuteCommandAsync(
                "UPDATE ff_onboarding SET stages_progress_json = @StagesProgressJson::jsonb WHERE id = @Id",
                new { StagesProgressJson = onboarding.StagesProgressJson, Id = onboarding.Id });

            // Trigger Projected time recalculation — fire-and-forget; errors are caught and logged by the handler
            try
            {
                await _mediator.Publish(new OnboardingStageMovedEvent
                {
                    OnboardingId = onboardingId,
                    FromStageId = input.StageId,
                    ToStageId = input.StageId,
                    TenantId = _userContext.TenantId,
                    UserId = long.TryParse(_userContext.UserId, out var unblockUid) ? unblockUid : 0,
                    UserName = _userContext.UserName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GanttService] Failed to publish projected recalc event after UnblockStage for OnboardingId={OnboardingId}", onboardingId);
            }

            // Write changelog
            try
            {
                var stageEntity = await _stageRepository.GetByIdAsync(input.StageId);
                var stageName = stageEntity?.Name ?? input.StageId.ToString();
                await _operationChangeLogService.LogOperationAsync(
                    operationType: OperationTypeEnum.StageUnblock,
                    businessModule: BusinessModuleEnum.Stage,
                    businessId: input.StageId,
                    onboardingId: onboardingId,
                    stageId: input.StageId,
                    operationTitle: $"Stage '{stageName}' unblocked by {_userContext.UserName}" +
                        (!string.IsNullOrWhiteSpace(input.ResolutionNotes) ? $": {input.ResolutionNotes}" : ""),
                    operationDescription: $"Stage '{stageName}' blocker has been resolved by {_userContext.UserName}" +
                        (!string.IsNullOrWhiteSpace(input.ResolutionNotes) ? $". Notes: {input.ResolutionNotes}" : ""),
                    beforeData: JsonSerializer.Serialize(new { IsBlocked = true }),
                    afterData: JsonSerializer.Serialize(new { IsBlocked = false, ResolvedByName = _userContext.UserName, ResolvedAt = DateTimeOffset.UtcNow, ResolutionNotes = input.ResolutionNotes }),
                    changedFields: new List<string> { "IsBlocked" },
                    extendedData: JsonSerializer.Serialize(new { Source = "manual_unblock" })
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GanttService] Failed to write changelog after UnblockStage for OnboardingId={OnboardingId}", onboardingId);
            }

            return true;
        }

        #endregion

        #region Private Algorithms

        /// <summary>
        /// Compute planned start/end dates for all stages.
        /// Used at Case start (GanttPlannedTimeInitHandler) and as read-only fallback for old cases.
        /// </summary>
        /// <param name="stages">Stages ordered by order_index ascending.</param>
        /// <param name="caseStartDate">Case start date.</param>
        /// <param name="caseEtaDate">Optional estimated completion date.</param>
        private static Dictionary<long, (DateTimeOffset plannedStart, DateTimeOffset plannedEnd)> ComputePlannedTimes(
            List<Stage> stages,
            DateTimeOffset caseStartDate,
            DateTimeOffset? caseEtaDate)
        {
            var result = new Dictionary<long, (DateTimeOffset, DateTimeOffset)>();

            if (stages == null || !stages.Any())
                return result;

            // Fallback days when EstimatedDuration is null or 0
            int fallbackDays;
            if (caseEtaDate.HasValue)
                fallbackDays = Math.Max(1, (int)Math.Round((caseEtaDate.Value - caseStartDate).TotalDays / stages.Count));
            else
                fallbackDays = 7;

            var current = OnboardingSharedUtilities.NormalizeToStartOfDay(caseStartDate);

            foreach (var stage in stages.OrderBy(s => s.Order))
            {
                int duration = (stage.EstimatedDuration.HasValue && stage.EstimatedDuration > 0)
                    ? (int)Math.Round((double)stage.EstimatedDuration.Value)
                    : fallbackDays;

                // Ensure duration is at least 1 day
                if (duration < 1) duration = 1;

                var plannedStart = current;
                var plannedEnd = current.AddDays(duration - 1);
                result[stage.Id] = (plannedStart, plannedEnd);
                current = plannedEnd.AddDays(1);
            }

            return result;
        }

        /// <summary>
        /// Derive the Gantt-specific status for a single stage progress record.
        /// Priority order: Completed → Overdue → InProgress → Delayed → NotStarted
        /// </summary>
        private static string DeriveGanttStageStatus(
            OnboardingStageProgress progress,
            DateTimeOffset? plannedStart,
            DateTimeOffset? plannedEnd,
            DateTimeOffset today)
        {
            // Rule 1: Completed
            if (progress.IsCompleted)
                return "Completed";

            // Rule 2: Started AND past planned end → Overdue
            if (progress.StartTime.HasValue && plannedEnd.HasValue && today > plannedEnd.Value)
                return "Overdue";

            // Rule 3: Started AND within planned window → InProgress
            if (progress.StartTime.HasValue)
                return "InProgress";

            // Rule 4: Not started AND today is past planned start → Delayed
            if (!progress.StartTime.HasValue && plannedStart.HasValue && today > plannedStart.Value)
                return "Delayed";

            // Rule 5: Default
            return "NotStarted";
        }

        /// <summary>
        /// Compute the weighted completion percentage for a stage at query time.
        /// Not persisted to the database.
        /// </summary>
        private decimal ComputeCompletionPercentage(
            Stage stage,
            OnboardingStageProgress progress,
            Dictionary<long, List<ChecklistTaskCompletion>> completionsByStage,
            Dictionary<long, List<ChecklistTask>> tasksByChecklist,
            Dictionary<(long stageId, long questionnaireId), QuestionnaireAnswer> answerLookup,
            Dictionary<long, List<StaticFieldValue>> fieldValuesByStage)
        {
            // Short-circuit: completed stage is always 100%
            if (progress.IsCompleted)
                return 100m;

            // Parse components from Stage.ComponentsJson
            var components = ParseStageComponents(stage.ComponentsJson);
            if (!components.Any())
                return 0m;

            // Quick return: all components are quickLink (no real work items)
            // Return 100% only if started, 0% if not yet started
            if (components.Any() && components.All(c =>
                    c.Key?.ToLowerInvariant() == "quicklinks" ||
                    c.Key?.ToLowerInvariant() == "quicklink"))
                return progress.StartTime.HasValue ? 100m : 0m;

            // Parse component weights from Stage.ComponentWeights
            var weights = ParseComponentWeights(stage.ComponentWeights, components);

            decimal totalWeight = weights.Values.Sum();
            if (totalWeight == 0)
                return 0m;

            decimal weightedSum = 0m;

            foreach (var component in components)
            {
                var key = component.Key?.ToLowerInvariant();
                if (string.IsNullOrEmpty(key)) continue;

                // Quick links never contribute to completion
                if (key == "quicklinks" || key == "quicklink") continue;

                decimal componentCompletion = ComputeComponentCompletion(key, component, progress, completionsByStage, tasksByChecklist, answerLookup, fieldValuesByStage);
                decimal weight = weights.TryGetValue(component.Key, out var w) ? w : 0m;
                weightedSum += weight * componentCompletion;
            }

            // Normalise by totalWeight to handle cases where weights don't perfectly sum to 100
            var result = (totalWeight > 0) ? weightedSum / totalWeight : 0m;
            return Math.Round(Math.Clamp(result, 0m, 100m), 2);
        }

        /// <summary>
        /// Compute completion fraction (0.0 – 1.0) for an individual component type.
        /// </summary>
        private decimal ComputeComponentCompletion(
            string componentKey,
            StageComponent component,
            OnboardingStageProgress progress,
            Dictionary<long, List<ChecklistTaskCompletion>> completionsByStage,
            Dictionary<long, List<ChecklistTask>> tasksByChecklist,
            Dictionary<(long stageId, long questionnaireId), QuestionnaireAnswer> answerLookup,
            Dictionary<long, List<StaticFieldValue>> fieldValuesByStage)
        {
            switch (componentKey)
            {
                case "checklist":
                {
                    if (!component.ChecklistIds.Any()) return 0m;

                    var stageCompletions = completionsByStage.TryGetValue(progress.StageId, out var sc)
                        ? sc
                        : new List<ChecklistTaskCompletion>();
                    var completedTaskIds = stageCompletions
                        .Where(c => c.IsCompleted)
                        .Select(c => c.TaskId)
                        .ToHashSet();

                    int totalTasks = 0, completedTasks = 0;
                    foreach (var checklistId in component.ChecklistIds)
                    {
                        var tasks = tasksByChecklist.TryGetValue(checklistId, out var tl) ? tl : new List<ChecklistTask>();
                        totalTasks += tasks.Count;
                        completedTasks += tasks.Count(t => completedTaskIds.Contains(t.Id));
                    }
                    return totalTasks == 0 ? 0m : Math.Round((decimal)completedTasks / totalTasks * 100m, 2);
                }

                case "questionnaires":
                case "questionnaire":
                {
                    if (!component.QuestionnaireIds.Any()) return 0m;
                    int submitted = 0;
                    foreach (var qId in component.QuestionnaireIds)
                    {
                        if (answerLookup.TryGetValue((progress.StageId, qId), out var answer)
                            && (answer.Status == "Submitted" || answer.Status == "Approved"))
                            submitted++;
                    }
                    return Math.Round((decimal)submitted / component.QuestionnaireIds.Count * 100m, 2);
                }

                case "fields":
                {
                    // Required fields are identified by StaticFieldConfig.Id == StaticFieldValue.FieldName
                    var requiredFieldIds = component.StaticFields?
                        .Where(f => f.IsRequired && !string.IsNullOrWhiteSpace(f.Id))
                        .Select(f => f.Id!)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase)
                        ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    // No required fields — treat as complete only if stage has been started
                    if (requiredFieldIds.Count == 0)
                        return progress.StartTime.HasValue ? 100m : 0m;

                    var filledValues = fieldValuesByStage.TryGetValue(progress.StageId, out var fvs)
                        ? fvs
                        : new List<StaticFieldValue>();
                    var filledFieldIds = filledValues
                        .Where(fv => !string.IsNullOrWhiteSpace(fv.FieldValueJson)
                                     && fv.FieldValueJson != "{}"
                                     && fv.FieldValueJson != "null"
                                     && fv.FieldId.HasValue)
                        .Select(fv => fv.FieldId!.Value.ToString())
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    int filled = requiredFieldIds.Count(id => filledFieldIds.Contains(id));
                    return Math.Round((decimal)filled / requiredFieldIds.Count * 100m, 2);
                }

                case "files":
                    return 0m; // file completion tracking out of scope

                default:
                    return 0m;
            }
        }

        /// <summary>
        /// Parse StageComponent list from ComponentsJson.
        /// </summary>
        private static List<StageComponent> ParseStageComponents(string componentsJson)
        {
            if (string.IsNullOrWhiteSpace(componentsJson))
                return new List<StageComponent>();
            return JsonParsingHelper.ParseJsonArray<StageComponent>(componentsJson);
        }

        /// <summary>
        /// Parse ComponentWeight entries from the stage's ComponentWeights JSONB column.
        /// Falls back to equal distribution when null/empty.
        /// Returns a map from component Key → weight value (0–100 scale).
        /// Orphan weight records (whose Id does not correspond to any component in <paramref name="components"/>) are filtered out.
        /// </summary>
        private static Dictionary<string, decimal> ParseComponentWeights(string componentWeightsJson, List<StageComponent> components)
        {
            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(componentWeightsJson))
            {
                try
                {
                    var items = JsonSerializer.Deserialize<List<ComponentWeightItem>>(componentWeightsJson, JsonOptions);
                    if (items != null && items.Any())
                    {
                        // Build a set of valid component IDs from current stage components
                        var validIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var comp in components)
                        {
                            var key = comp.Key?.ToLowerInvariant() ?? "";
                            if (key == "fields")
                                validIds.Add("fields");
                            else if (key == "files")
                                validIds.Add("files");

                            if (comp.ChecklistIds != null)
                                foreach (var id in comp.ChecklistIds) validIds.Add(id.ToString());

                            if (comp.QuestionnaireIds != null)
                                foreach (var id in comp.QuestionnaireIds) validIds.Add(id.ToString());

                            if (key == "quicklinks" || key == "quicklink")
                                foreach (var id in comp.QuickLinkIds) validIds.Add(id.ToString());
                        }

                        // Filter orphan records — only keep items with a matching Id in current components
                        items = items.Where(item => !string.IsNullOrEmpty(item.Id) && validIds.Contains(item.Id)).ToList();

                        foreach (var item in items)
                        {
                            if (!string.IsNullOrEmpty(item.Type))
                            {
                                // Normalise singular "questionnaire" to plural "questionnaires" so it matches
                                // the component Key used in components_json (frontend always stores "questionnaires")
                                var normalizedType = item.Type.Equals("questionnaire", StringComparison.OrdinalIgnoreCase)
                                    ? "questionnaires"
                                    : item.Type;
                                result[normalizedType] = item.Weight;
                            }
                        }
                        if (result.Any())
                            return result;
                    }
                }
                catch
                {
                    // Fall through to equal distribution
                }
            }

            // Equal distribution fallback: ignore quickLinks
            var eligibleKeys = components
                .Where(c => !string.IsNullOrEmpty(c.Key) &&
                            !c.Key.Equals("quicklinks", StringComparison.OrdinalIgnoreCase) &&
                            !c.Key.Equals("quicklink", StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Key)
                .Distinct()
                .ToList();

            if (!eligibleKeys.Any())
                return result;

            decimal equalWeight = 100m / eligibleKeys.Count;
            foreach (var key in eligibleKeys)
                result[key] = equalWeight;

            return result;
        }

        /// <summary>
        /// Compute the total blocked days for a stage (resolved + current active blocker).
        /// </summary>
        private static int ComputeBlockedDays(OnboardingStageProgress progress, DateTimeOffset today)
        {
            if (progress.BlockerHistory == null || !progress.BlockerHistory.Any())
                return 0;

            int total = 0;
            foreach (var record in progress.BlockerHistory)
            {
                if (record.BlockedDays.HasValue)
                {
                    // Resolved record with stored days
                    total += record.BlockedDays.Value;
                }
                else if (record.BlockerStartDate.HasValue && !record.BlockerResolvedDate.HasValue)
                {
                    // Currently active blocker — compute days since start
                    total += (int)Math.Round((today - OnboardingSharedUtilities.NormalizeToStartOfDay(record.BlockerStartDate.Value)).TotalDays);
                }
            }
            return total;
        }

        /// <summary>
        /// Resolve user ID strings to GanttAssigneeDtos via batch user lookup.
        /// </summary>
        private async Task<Dictionary<string, GanttAssigneeDto>> ResolveUserIdsAsync(List<string> userIdStrings)
        {
            var result = new Dictionary<string, GanttAssigneeDto>(StringComparer.OrdinalIgnoreCase);
            if (!userIdStrings.Any())
                return result;

            var longIds = userIdStrings
                .Where(s => long.TryParse(s, out _))
                .Select(s => long.Parse(s))
                .Distinct()
                .ToList();

            if (!longIds.Any())
                return result;

            try
            {
                var users = await _userService.GetUsersByIdsAsync(longIds);
                foreach (var user in users)
                {
                    result[user.Id.ToString()] = new GanttAssigneeDto
                    {
                        Name = user.Username ?? user.Email ?? user.Id.ToString(),
                        Email = user.Email
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve user IDs for Gantt assignees");
            }

            return result;
        }

        /// <summary>
        /// Build GanttAssigneeDto list from a list of user ID strings, using the pre-resolved user map.
        /// </summary>
        private static List<GanttAssigneeDto> BuildAssigneeDtos(
            List<string> userIdStrings,
            Dictionary<string, GanttAssigneeDto> userMap)
        {
            if (userIdStrings == null || !userIdStrings.Any())
                return new List<GanttAssigneeDto>();

            return userIdStrings
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => userMap.TryGetValue(id, out var dto) ? dto : new GanttAssigneeDto { Name = id, Email = null })
                .ToList();
        }

        /// <summary>
        /// Build a GanttComponentsDto from the Stage entity's component configuration,
        /// filling both total and completion counts using the pre-fetched lookup data.
        /// </summary>
        private GanttComponentsDto BuildComponentsSummary(
            Stage stage,
            OnboardingStageProgress progress,
            Dictionary<long, List<ChecklistTaskCompletion>> completionsByStage,
            Dictionary<long, List<ChecklistTask>> tasksByChecklist,
            Dictionary<(long stageId, long questionnaireId), QuestionnaireAnswer> answerLookup,
            Dictionary<long, List<StaticFieldValue>> fieldValuesByStage,
            Dictionary<long, int> fileCountByStage)
        {
            var dto = new GanttComponentsDto();
            var components = ParseStageComponents(stage.ComponentsJson);

            var stageCompletions = completionsByStage.TryGetValue(progress.StageId, out var sc)
                ? sc
                : new List<ChecklistTaskCompletion>();
            var completedTaskIds = stageCompletions
                .Where(c => c.IsCompleted)
                .Select(c => c.TaskId)
                .ToHashSet();

            var filledValues = fieldValuesByStage.TryGetValue(progress.StageId, out var fvs)
                ? fvs
                : new List<StaticFieldValue>();
            var filledFieldIds = filledValues
                .Where(fv => !string.IsNullOrWhiteSpace(fv.FieldValueJson)
                             && fv.FieldValueJson != "{}"
                             && fv.FieldValueJson != "null"
                             && fv.FieldId.HasValue)
                .Select(fv => fv.FieldId!.Value.ToString())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var component in components)
            {
                var key = component.Key?.ToLowerInvariant();
                switch (key)
                {
                    case "checklist":
                        foreach (var checklistId in component.ChecklistIds ?? new List<long>())
                        {
                            var tasks = tasksByChecklist.TryGetValue(checklistId, out var tl)
                                ? tl
                                : new List<ChecklistTask>();
                            dto.ChecklistsTotal += tasks.Count;
                            dto.ChecklistsCompleted += tasks.Count(t => completedTaskIds.Contains(t.Id));
                        }
                        break;

                    case "questionnaires":
                    case "questionnaire":
                        dto.QuestionnairesTotal += component.QuestionnaireIds?.Count ?? 0;
                        foreach (var qId in component.QuestionnaireIds ?? new List<long>())
                        {
                            if (answerLookup.TryGetValue((progress.StageId, qId), out var ans)
                                && (ans.Status == "Submitted" || ans.Status == "Approved"))
                                dto.QuestionnairesSubmitted++;
                        }
                        break;

                    case "fields":
                        var requiredConfigs = component.StaticFields?
                            .Where(f => f.IsRequired && !string.IsNullOrWhiteSpace(f.Id))
                            .ToList()
                            ?? new List<StaticFieldConfig>();
                        dto.FieldsTotal += requiredConfigs.Count;
                        dto.FieldsFilled += requiredConfigs.Count(f => filledFieldIds.Contains(f.Id!));
                        break;

                    case "files":
                        dto.FilesUploaded = fileCountByStage.TryGetValue(progress.StageId, out var fc) ? fc : 0;
                        break;
                }
            }

            return dto;
        }

        #endregion

    }
}
