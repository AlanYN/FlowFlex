using System.Text.Json;
using FlowFlex.Application.Helpers.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Events;
using FlowFlex.Domain.Shared.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Notification
{
    /// <summary>
    /// Handles <see cref="OnboardingStartedEvent"/> by computing and writing
    /// <c>plannedStartDate</c> / <c>plannedEndDate</c> for every Stage in the Case.
    ///
    /// This handler is idempotent: if any StageProgress already has a
    /// <c>PlannedStartDate</c> value the handler exits without making changes.
    ///
    /// All exceptions are caught and logged as errors — they never propagate,
    /// so a failure here cannot block the StartOnboarding response.
    /// </summary>
    public class GanttPlannedTimeInitHandler : INotificationHandler<OnboardingStartedEvent>
    {
        private readonly ILogger<GanttPlannedTimeInitHandler> _logger;
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IStageRepository _stageRepository;
        private readonly UserContext _userContext;

        public GanttPlannedTimeInitHandler(
            ILogger<GanttPlannedTimeInitHandler> logger,
            IOnboardingRepository onboardingRepository,
            IStageRepository stageRepository,
            UserContext userContext)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _onboardingRepository = onboardingRepository ?? throw new ArgumentNullException(nameof(onboardingRepository));
            _stageRepository = stageRepository ?? throw new ArgumentNullException(nameof(stageRepository));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        public async Task Handle(OnboardingStartedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "GanttPlannedTimeInitHandler: processing OnboardingStartedEvent for onboardingId={OnboardingId}",
                    notification.OnboardingId);

                // Propagate tenant context so repository filters work correctly
                if (!string.IsNullOrEmpty(notification.TenantId))
                    _userContext.TenantId = notification.TenantId;

                if (notification.UserId > 0)
                    _userContext.UserId = notification.UserId.ToString();

                if (!string.IsNullOrEmpty(notification.UserName))
                    _userContext.UserName = notification.UserName;

                // ── Step 1: Load the Onboarding entity ────────────────────────────
                var onboarding = await _onboardingRepository.GetByIdAsync(notification.OnboardingId, cancellationToken: cancellationToken);
                if (onboarding == null || !onboarding.IsValid)
                {
                    _logger.LogWarning(
                        "GanttPlannedTimeInitHandler: onboarding not found or soft-deleted, id={OnboardingId}",
                        notification.OnboardingId);
                    return;
                }

                // ── Step 2: Load stages ordered by order_index ASC ────────────────
                var stages = await _stageRepository.GetByWorkflowIdAsync(onboarding.WorkflowId);
                if (stages == null || stages.Count == 0)
                {
                    _logger.LogWarning(
                        "GanttPlannedTimeInitHandler: no stages found for workflowId={WorkflowId}, skipping",
                        onboarding.WorkflowId);
                    return;
                }

                var orderedStages = stages.OrderBy(s => s.Order).ToList();

                // ── Step 3: Deserialize stages_progress_json ──────────────────────
                var stagesProgress = JsonParsingHelper.ParseStagesProgress(onboarding.StagesProgressJson, _logger);
                if (stagesProgress.Count == 0)
                {
                    _logger.LogWarning(
                        "GanttPlannedTimeInitHandler: empty StagesProgressJson for onboardingId={OnboardingId}, skipping",
                        notification.OnboardingId);
                    return;
                }

                // ── Step 4: Idempotency check ─────────────────────────────────────
                if (stagesProgress.Any(sp => sp.PlannedStartDate.HasValue))
                {
                    _logger.LogInformation(
                        "GanttPlannedTimeInitHandler: PlannedStartDate already set for onboardingId={OnboardingId}, skipping (idempotent)",
                        notification.OnboardingId);
                    return;
                }

                // ── Step 5: Compute planned times ─────────────────────────────────
                var plannedTimes = ComputePlannedTimes(
                    orderedStages,
                    notification.StartDate,
                    notification.EstimatedCompletionDate);

                // ── Step 6: Apply computed times to each StageProgress ────────────
                foreach (var sp in stagesProgress)
                {
                    if (plannedTimes.TryGetValue(sp.StageId, out var times))
                    {
                        sp.PlannedStartDate = times.PlannedStart;
                        sp.PlannedEndDate = times.PlannedEnd;
                    }
                }

                // ── Step 7: Serialize and persist ─────────────────────────────────
                var updatedJson = JsonSerializer.Serialize(stagesProgress, JsonParsingHelper.DefaultOptions);
                var db = _onboardingRepository.GetSqlSugarClient();
                const string sql = "UPDATE ff_onboarding SET stages_progress_json = @StagesProgressJson::jsonb WHERE id = @Id";
                await db.Ado.ExecuteCommandAsync(sql, new
                {
                    StagesProgressJson = updatedJson,
                    Id = notification.OnboardingId
                });

                _logger.LogInformation(
                    "GanttPlannedTimeInitHandler: successfully wrote planned times for {Count} stages, onboardingId={OnboardingId}",
                    stagesProgress.Count,
                    notification.OnboardingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "GanttPlannedTimeInitHandler: error processing OnboardingStartedEvent for onboardingId={OnboardingId}",
                    notification.OnboardingId);
                // Never rethrow — this handler must not block the StartOnboarding response
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Core algorithm
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes <c>plannedStartDate</c> and <c>plannedEndDate</c> for every stage.
        /// </summary>
        /// <param name="orderedStages">Stages sorted by <c>order_index</c> ASC.</param>
        /// <param name="caseStartDate">The date the Case was started.</param>
        /// <param name="caseEtaDate">Optional ETA for the Case.</param>
        /// <returns>Dictionary keyed by Stage ID.</returns>
        internal static Dictionary<long, (DateTimeOffset PlannedStart, DateTimeOffset PlannedEnd)> ComputePlannedTimes(
            IReadOnlyList<Domain.Entities.OW.Stage> orderedStages,
            DateTimeOffset caseStartDate,
            DateTimeOffset? caseEtaDate)
        {
            var result = new Dictionary<long, (DateTimeOffset, DateTimeOffset)>();

            if (orderedStages.Count == 0)
                return result;

            // Fallback days per stage when EstimatedDuration is absent
            int fallbackDays = caseEtaDate.HasValue
                ? Math.Max(1, (int)Math.Round((caseEtaDate.Value - caseStartDate).TotalDays / orderedStages.Count))
                : 7;

            var current = NormalizeToStartOfDay(caseStartDate);

            foreach (var stage in orderedStages)
            {
                int duration = (stage.EstimatedDuration.HasValue && stage.EstimatedDuration.Value > 0)
                    ? (int)Math.Round(stage.EstimatedDuration.Value)
                    : fallbackDays;

                // Ensure at least 1 day duration to avoid zero-length windows
                if (duration < 1)
                    duration = 1;

                var plannedStart = current;
                var plannedEnd   = current.AddDays(duration - 1); // inclusive end

                result[stage.Id] = (plannedStart, plannedEnd);

                current = plannedEnd.AddDays(1); // next stage starts the day after
            }

            return result;
        }

        /// <summary>
        /// Strips the time component from a <see cref="DateTimeOffset"/>,
        /// returning midnight (00:00:00) in the same UTC offset.
        /// </summary>
        private static DateTimeOffset NormalizeToStartOfDay(DateTimeOffset dt)
            => new DateTimeOffset(dt.Year, dt.Month, dt.Day, 0, 0, 0, dt.Offset);
    }
}
