using System.Text.Json;
using FlowFlex.Application.Helpers.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Events;
using FlowFlex.Domain.Shared.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Notification
{
    /// <summary>
    /// Handles both OnboardingStageCompletedEvent and OnboardingStageMovedEvent to
    /// recalculate Projected (forecast) start/end dates for all stages in a Case.
    ///
    /// Projected times reflect "what we now expect to happen" given the latest actual
    /// execution data.  They are persisted back to stages_progress_json so the Gantt
    /// chart can display an up-to-date forecast without recomputing on every read.
    ///
    /// Error handling contract: ALL exceptions are caught and logged at Error level.
    /// This handler must NEVER propagate exceptions — it runs as a side-effect of
    /// the main business flow and must not roll back or block it.
    /// </summary>
    public class GanttProjectedTimeRecalcHandler :
        INotificationHandler<OnboardingStageCompletedEvent>,
        INotificationHandler<OnboardingStageMovedEvent>
    {
        private readonly ILogger<GanttProjectedTimeRecalcHandler> _logger;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IStageRepository _stageRepository;
        private readonly UserContext _userContext;

        // Fallback duration (days) when a Stage has no EstimatedDuration configured
        private const int FallbackDurationDays = 7;

        // Shared serializer options — camelCase output to match existing JSON in the DB
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
        };

        public GanttProjectedTimeRecalcHandler(
            ILogger<GanttProjectedTimeRecalcHandler> logger,
            IOnboardingRepository onboardingRepository,
            IStageRepository stageRepository,
            UserContext userContext)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _onboardingRepository = onboardingRepository ?? throw new ArgumentNullException(nameof(onboardingRepository));
            _stageRepository = stageRepository ?? throw new ArgumentNullException(nameof(stageRepository));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        // ──────────────────────────────────────────────────────────────
        // INotificationHandler<OnboardingStageCompletedEvent>
        // ──────────────────────────────────────────────────────────────

        public async Task Handle(OnboardingStageCompletedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "[GanttProjectedTimeRecalcHandler] Handling OnboardingStageCompletedEvent for OnboardingId={OnboardingId}, CompletedStageId={StageId}",
                    notification.OnboardingId, notification.CompletedStageId);

                SetUserContextFromEvent(notification.TenantId, notification.UserId, notification.UserName);
                await RecalcProjectedTimesAsync(notification.OnboardingId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[GanttProjectedTimeRecalcHandler] Error handling OnboardingStageCompletedEvent for OnboardingId={OnboardingId}",
                    notification.OnboardingId);
                // Never rethrow — this handler must not affect the main business flow
            }
        }

        // ──────────────────────────────────────────────────────────────
        // INotificationHandler<OnboardingStageMovedEvent>
        // ──────────────────────────────────────────────────────────────

        public async Task Handle(OnboardingStageMovedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "[GanttProjectedTimeRecalcHandler] Handling OnboardingStageMovedEvent for OnboardingId={OnboardingId}, From={FromStageId}, To={ToStageId}",
                    notification.OnboardingId, notification.FromStageId, notification.ToStageId);

                SetUserContextFromEvent(notification.TenantId, notification.UserId, notification.UserName);
                await RecalcProjectedTimesAsync(notification.OnboardingId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[GanttProjectedTimeRecalcHandler] Error handling OnboardingStageMovedEvent for OnboardingId={OnboardingId}",
                    notification.OnboardingId);
                // Never rethrow
            }
        }

        // ──────────────────────────────────────────────────────────────
        // Core algorithm
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads the onboarding + its stages, computes new Projected dates for every
        /// stage, and persists the updated stages_progress_json back to the database.
        /// </summary>
        private async Task RecalcProjectedTimesAsync(long onboardingId, CancellationToken cancellationToken)
        {
            // ── 1. Load onboarding ──────────────────────────────────────
            var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
            if (onboarding == null)
            {
                _logger.LogWarning(
                    "[GanttProjectedTimeRecalcHandler] Onboarding {OnboardingId} not found — skipping Projected recalc",
                    onboardingId);
                return;
            }

            // ── 2. Parse stagesProgress from JSONB ─────────────────────
            var stagesProgress = JsonParsingHelper.ParseStagesProgress(onboarding.StagesProgressJson, _logger);
            if (stagesProgress.Count == 0)
            {
                _logger.LogDebug(
                    "[GanttProjectedTimeRecalcHandler] No stage progress data for OnboardingId={OnboardingId} — nothing to recalc",
                    onboardingId);
                return;
            }

            // ── 3. Load Stage metadata for EstimatedDuration values ─────
            var stages = await _stageRepository.GetByWorkflowIdAsync(onboarding.WorkflowId);
            var stageDurationMap = stages
                .Where(s => s.EstimatedDuration.HasValue && s.EstimatedDuration.Value > 0)
                .ToDictionary(s => s.Id, s => s.EstimatedDuration!.Value);

            // ── 4. Sort stagesProgress by StageOrder ascending ──────────
            var ordered = stagesProgress
                .OrderBy(sp => sp.StageOrder)
                .ToList();

            // ── 5. Determine today (normalised to start of day, UTC) ────
            var today = NormalizeToStartOfDay(DateTimeOffset.UtcNow);

            // ── 6. Run ComputeProjectedTimes algorithm ──────────────────
            ComputeProjectedTimes(ordered, stageDurationMap, onboarding.StartDate, today);

            // ── 7. Persist back to JSONB ────────────────────────────────
            var updatedJson = JsonSerializer.Serialize(stagesProgress, SerializerOptions);
            var db = _onboardingRepository.GetSqlSugarClient();
            var sql = "UPDATE ff_onboarding SET stages_progress_json = @StagesProgressJson::jsonb WHERE id = @Id";
            var rowsAffected = await db.Ado.ExecuteCommandAsync(sql, new
            {
                StagesProgressJson = updatedJson,
                Id = onboardingId
            });

            _logger.LogInformation(
                "[GanttProjectedTimeRecalcHandler] Projected times recalculated and persisted for OnboardingId={OnboardingId}, RowsAffected={Rows}",
                onboardingId, rowsAffected);
        }

        // ──────────────────────────────────────────────────────────────
        // ComputeProjectedTimes — pure algorithm (mutates list in-place)
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Mutates each <see cref="OnboardingStageProgress"/> in <paramref name="ordered"/>
        /// (which must already be sorted by <see cref="OnboardingStageProgress.StageOrder"/>
        /// ascending) by computing new <c>ProjectedStartDate</c> and <c>ProjectedEndDate</c>.
        ///
        /// Rules (evaluated in order for each stage N):
        /// <list type="number">
        ///   <item>If stage N is Blocked → set both projected dates to null and continue.</item>
        ///   <item>If upstream stage has null ProjectedEndDate → propagate null and continue.</item>
        ///   <item>If stage N is completed → Projected = Actual (fixed).</item>
        ///   <item>Otherwise compute projectedStart from the previous stage state, then
        ///         projectedEnd = projectedStart + (estimatedDuration − 1) days.</item>
        /// </list>
        /// </summary>
        private static void ComputeProjectedTimes(
            List<OnboardingStageProgress> ordered,
            Dictionary<long, decimal> stageDurationMap,
            DateTimeOffset? caseStartDate,
            DateTimeOffset today)
        {
            // Track whether an upstream blocked/unknown stage has broken the chain
            bool upstreamNull = false;

            for (int i = 0; i < ordered.Count; i++)
            {
                var sp = ordered[i];
                var prev = i > 0 ? ordered[i - 1] : null;

                // ── Rule 1: Blocked ─────────────────────────────────────
                if (sp.IsBlocked)
                {
                    sp.ProjectedStartDate = null;
                    sp.ProjectedEndDate = null;
                    upstreamNull = true;
                    continue;
                }

                // ── Rule 2: Upstream chain is broken ────────────────────
                if (upstreamNull)
                {
                    sp.ProjectedStartDate = null;
                    sp.ProjectedEndDate = null;
                    continue;
                }

                // Also propagate null when the previous stage's projected end is null
                // (but that stage is not marked blocked — e.g. it was already nulled by rule 1)
                if (prev != null && prev.ProjectedEndDate == null)
                {
                    sp.ProjectedStartDate = null;
                    sp.ProjectedEndDate = null;
                    upstreamNull = true;
                    continue;
                }

                // ── Rule 3: Stage is completed → Projected = Actual ─────
                if (sp.IsCompleted && sp.CompletionTime.HasValue)
                {
                    sp.ProjectedStartDate = sp.StartTime;
                    sp.ProjectedEndDate = sp.CompletionTime;
                    continue;
                }

                // ── Rule 4: Not yet completed — compute projected start ──
                DateTimeOffset projectedStart;

                if (prev == null)
                {
                    // First stage: use Case start date; fall back to today if missing
                    projectedStart = NormalizeToStartOfDay(caseStartDate ?? today);
                }
                else if (prev.IsCompleted && prev.CompletionTime.HasValue)
                {
                    // Previous stage finished: start the day after its actual completion
                    projectedStart = NormalizeToStartOfDay(prev.CompletionTime.Value).AddDays(1);
                }
                else if (prev.StartTime.HasValue && !prev.IsCompleted)
                {
                    // Previous stage is InProgress: estimate remaining duration
                    // completionPct defaults to 0 (conservative) if not directly available
                    int prevDuration = GetDurationDays(prev.StageId, stageDurationMap);
                    // completionPct is not persisted on StageProgress; use 0 as conservative default
                    const decimal completionPct = 0m;
                    int remainingDays = (int)Math.Round(prevDuration * (1m - completionPct / 100m));
                    remainingDays = Math.Max(remainingDays, 1);
                    projectedStart = today.AddDays(remainingDays);
                }
                else
                {
                    // Previous stage has not started: chain from its projected end
                    // prev.ProjectedEndDate is guaranteed non-null here (checked above)
                    projectedStart = NormalizeToStartOfDay(prev.ProjectedEndDate!.Value).AddDays(1);
                }

                int duration = GetDurationDays(sp.StageId, stageDurationMap);

                sp.ProjectedStartDate = projectedStart;
                sp.ProjectedEndDate = projectedStart.AddDays(duration - 1);
            }
        }

        // ──────────────────────────────────────────────────────────────
        // Helpers
        // ──────────────────────────────────────────────────────────────

        /// <summary>Returns the estimated duration (days) for the given stage, falling back to 7.</summary>
        private static int GetDurationDays(long stageId, Dictionary<long, decimal> durationMap)
        {
            if (durationMap.TryGetValue(stageId, out var d) && d > 0)
                return (int)Math.Round(d);
            return FallbackDurationDays;
        }

        /// <summary>Normalises a <see cref="DateTimeOffset"/> to 00:00:00 of its day, preserving offset.</summary>
        private static DateTimeOffset NormalizeToStartOfDay(DateTimeOffset dt) =>
            new(dt.Year, dt.Month, dt.Day, 0, 0, 0, dt.Offset);

        /// <summary>
        /// Copies tenant/user info from the event into the ambient UserContext so that
        /// repository global filters (multi-tenancy) resolve correctly for background handlers.
        /// </summary>
        private void SetUserContextFromEvent(string tenantId, long userId, string userName)
        {
            if (!string.IsNullOrEmpty(tenantId))
                _userContext.TenantId = tenantId;

            if (userId > 0)
                _userContext.UserId = userId.ToString();

            if (!string.IsNullOrEmpty(userName))
                _userContext.UserName = userName;
        }
    }
}
