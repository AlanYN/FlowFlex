using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices.OW.Onboarding;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Infrastructure.Services;
using Microsoft.Extensions.Logging;
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
        private readonly IOperatorContextService _operatorContextService;
        private readonly UserContext _userContext;
        private readonly ILogger<OnboardingStageProgressService> _logger;

        // Initialization tracking to prevent infinite recursion
        private static readonly HashSet<long> _initializingEntities = new HashSet<long>();
        private static readonly object _initializationLock = new object();

        // Shared JSON serializer options for consistent serialization
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        public OnboardingStageProgressService(
            IStageRepository stageRepository,
            IOperatorContextService operatorContextService,
            UserContext userContext,
            ILogger<OnboardingStageProgressService> logger)
        {
            _stageRepository = stageRepository ?? throw new ArgumentNullException(nameof(stageRepository));
            _operatorContextService = operatorContextService ?? throw new ArgumentNullException(nameof(operatorContextService));
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
                        StartTime = isFirstStage ? GetNormalizedUtcNow() : null,
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
                        EstimatedDays = NormalizeEstimatedDays(stage.EstimatedDuration),
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
                        completedStage.StartTime = GetNormalizedUtcNow();
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
                            stageProgress.EstimatedDays = NormalizeEstimatedDays(stage.EstimatedDuration);
                        }

                        stageProgress.VisibleInPortal = stage.VisibleInPortal;
                        stageProgress.PortalPermission = stage.PortalPermission;
                        stageProgress.AttachmentManagementNeeded = stage.AttachmentManagementNeeded;
                        stageProgress.Required = stage.Required;
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

        #region Helper Methods

        /// <summary>
        /// Normalize DateTimeOffset to start of day (00:00:00)
        /// </summary>
        private static DateTimeOffset NormalizeToStartOfDay(DateTimeOffset dateTime)
        {
            return new DateTimeOffset(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, dateTime.Offset);
        }

        /// <summary>
        /// Normalize estimated days to integer (round to nearest whole number)
        /// </summary>
        private static decimal? NormalizeEstimatedDays(decimal? days)
        {
            if (!days.HasValue) return null;
            return Math.Round(days.Value, 0);
        }

        /// <summary>
        /// Get current UTC time normalized to start of day (00:00:00)
        /// </summary>
        private static DateTimeOffset GetNormalizedUtcNow()
        {
            return NormalizeToStartOfDay(DateTimeOffset.UtcNow);
        }

        #endregion
    }
}
