using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Dashboard;
using FlowFlex.Application.Contracts.Dtos.OW.Message;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Helpers.OW;
using FlowFlex.Application.Services.OW.Permission;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Enums.Permission;
using FlowFlex.Domain.Shared.Helpers;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Dashboard service implementation - provides aggregated data for dashboard UI
    /// Performance optimized with request-scoped caching for workflow data
    /// </summary>
    public class DashboardService : IDashboardService, IScopedService
    {
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IChecklistTaskRepository _checklistTaskRepository;
        private readonly IStageRepository _stageRepository;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly UserContext _userContext;
        private readonly ILogger<DashboardService> _logger;
        private readonly PermissionHelpers _permissionHelpers;
        
        // Request-scoped cache for workflow data to avoid repeated DB queries
        private Dictionary<long, Workflow> _workflowCache;
        private readonly object _workflowCacheLock = new();

        public DashboardService(
            IOnboardingRepository onboardingRepository,
            IChecklistTaskRepository checklistTaskRepository,
            IStageRepository stageRepository,
            IWorkflowRepository workflowRepository,
            IMessageService messageService,
            IMapper mapper,
            UserContext userContext,
            ILogger<DashboardService> logger,
            PermissionHelpers permissionHelpers)
        {
            _onboardingRepository = onboardingRepository;
            _checklistTaskRepository = checklistTaskRepository;
            _stageRepository = stageRepository;
            _workflowRepository = workflowRepository;
            _messageService = messageService;
            _mapper = mapper;
            _userContext = userContext;
            _logger = logger;
            _permissionHelpers = permissionHelpers;
        }
        
        /// <summary>
        /// Get or load workflows into request-scoped cache
        /// </summary>
        private async Task<Dictionary<long, Workflow>> GetOrLoadWorkflowsAsync(IEnumerable<long> workflowIds)
        {
            var idsToLoad = workflowIds.Distinct().ToList();
            
            lock (_workflowCacheLock)
            {
                if (_workflowCache == null)
                {
                    _workflowCache = new Dictionary<long, Workflow>();
                }
                else
                {
                    // Filter out already cached IDs
                    idsToLoad = idsToLoad.Where(id => !_workflowCache.ContainsKey(id)).ToList();
                }
            }
            
            if (idsToLoad.Any())
            {
                var workflows = await _workflowRepository.GetListAsync(w => idsToLoad.Contains(w.Id));
                
                lock (_workflowCacheLock)
                {
                    foreach (var workflow in workflows)
                    {
                        _workflowCache[workflow.Id] = workflow;
                    }
                }
                
                _logger.LogDebug("Loaded {Count} workflows into cache", workflows.Count);
            }
            
            return _workflowCache;
        }

        /// <summary>
        /// Get aggregated dashboard data with optional module selection
        /// </summary>
        public async Task<DashboardDto> GetDashboardAsync(DashboardQueryDto query)
        {
            var result = new DashboardDto();
            var modules = query.Modules ?? new List<string>();
            var includeAll = modules.Count == 0;

            // Execute modules in parallel with timeout for better performance
            var tasks = new List<Task>();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // 30 second timeout

            // Use async lambdas directly instead of Task.Run to avoid thread pool overhead
            if (includeAll || modules.Contains("statistics", StringComparer.OrdinalIgnoreCase))
            {
                tasks.Add(SafeExecuteAsync(async () =>
                {
                    result.Statistics = await GetStatisticsAsync(query.Team);
                }, "statistics", cts.Token));
            }

            if (includeAll || modules.Contains("casesOverview", StringComparer.OrdinalIgnoreCase) || modules.Contains("stageDistribution", StringComparer.OrdinalIgnoreCase))
            {
                tasks.Add(SafeExecuteAsync(async () =>
                {
                    result.CasesOverview = await GetStageDistributionAsync(query.WorkflowId);
                }, "casesOverview", cts.Token));
            }

            if (includeAll || modules.Contains("tasks", StringComparer.OrdinalIgnoreCase))
            {
                tasks.Add(SafeExecuteAsync(async () =>
                {
                    result.Tasks = await GetTasksAsync(new DashboardTaskQueryDto
                    {
                        Category = query.TaskCategory,
                        PageIndex = query.TaskPageIndex,
                        PageSize = query.TaskPageSize
                    });
                }, "tasks", cts.Token));
            }

            if (includeAll || modules.Contains("messages", StringComparer.OrdinalIgnoreCase))
            {
                tasks.Add(SafeExecuteAsync(async () =>
                {
                    result.Messages = await GetMessageSummaryAsync(query.MessageLimit);
                }, "messages", cts.Token));
            }

            if (includeAll || modules.Contains("achievements", StringComparer.OrdinalIgnoreCase))
            {
                tasks.Add(SafeExecuteAsync(async () =>
                {
                    result.Achievements = await GetAchievementsAsync(query.AchievementLimit, query.Team);
                }, "achievements", cts.Token));
            }

            if (includeAll || modules.Contains("deadlines", StringComparer.OrdinalIgnoreCase))
            {
                tasks.Add(SafeExecuteAsync(async () =>
                {
                    result.Deadlines = await GetDeadlinesAsync(query.DeadlineDays);
                }, "deadlines", cts.Token));
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Dashboard data fetch timed out after 30 seconds");
            }

            return result;
        }

        /// <summary>
        /// Safely execute an async action with error handling and cancellation support
        /// </summary>
        private async Task SafeExecuteAsync(Func<Task> action, string moduleName, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await action();
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Dashboard module {ModuleName} was cancelled", moduleName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get dashboard {ModuleName}", moduleName);
            }
        }

        /// <summary>
        /// Get statistics overview with month-over-month comparison
        /// Performance optimized: uses parallel queries and batch data loading
        /// </summary>
        public async Task<DashboardStatisticsDto> GetStatisticsAsync(string? team = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var now = DateTimeOffset.UtcNow;
            var thisMonthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var lastMonthEnd = thisMonthStart.AddTicks(-1);

            // Admin bypass for better performance
            var isAdmin = _permissionHelpers.HasAdminPrivileges();

            // Execute all queries in parallel for better performance
            var activeCasesTask = GetActiveCasesCountOptimizedAsync(team, isAdmin);
            var completedThisMonthTask = GetCompletedCasesCountOptimizedAsync(thisMonthStart, now, team, isAdmin);
            var overdueTasksTask = GetOverdueTasksCountAsync(team);
            var avgCompletionTimeTask = GetAverageCompletionTimeOptimizedAsync(thisMonthStart, now, team, isAdmin);
            var lastMonthActiveCasesTask = GetActiveCasesCountAtDateOptimizedAsync(lastMonthEnd, team, isAdmin);
            var lastMonthCompletedTask = GetCompletedCasesCountOptimizedAsync(lastMonthStart, lastMonthEnd, team, isAdmin);
            var lastMonthOverdueTasksTask = GetOverdueTasksCountAtDateAsync(lastMonthEnd, team);
            var lastMonthAvgCompletionTimeTask = GetAverageCompletionTimeOptimizedAsync(lastMonthStart, lastMonthEnd, team, isAdmin);

            // Wait for all tasks to complete
            await Task.WhenAll(
                activeCasesTask, completedThisMonthTask, overdueTasksTask, avgCompletionTimeTask,
                lastMonthActiveCasesTask, lastMonthCompletedTask, lastMonthOverdueTasksTask, lastMonthAvgCompletionTimeTask);

            _logger.LogInformation("GetStatisticsAsync: Completed in {Ms}ms", sw.ElapsedMilliseconds);

            return new DashboardStatisticsDto
            {
                ActiveCases = CreateStatisticItem(await activeCasesTask, await lastMonthActiveCasesTask, true),
                CompletedThisMonth = CreateStatisticItem(await completedThisMonthTask, await lastMonthCompletedTask, true),
                OverdueTasks = CreateStatisticItem(await overdueTasksTask, await lastMonthOverdueTasksTask, false),
                AvgCompletionTime = CreateStatisticItem(await avgCompletionTimeTask, await lastMonthAvgCompletionTimeTask, false, "days")
            };
        }

        /// <summary>
        /// Get case distribution by stage (optimized to avoid N+1 queries)
        /// Performance optimized: uses lightweight projection and parallel permission check
        /// </summary>
        public async Task<StageDistributionResultDto> GetStageDistributionAsync(long? workflowId = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
            
            // Get all stages ordered by sequence (lightweight query)
            // Note: Sorting is done at database level for better performance
            var stages = await _stageRepository.GetListAsync(s => 
                s.IsActive && s.IsValid && 
                s.TenantId == tenantId &&
                (workflowId == null || s.WorkflowId == workflowId));
            
            // Database already returns ordered results, but ensure order is correct
            if (!stages.Any())
            {
                return new StageDistributionResultDto
                {
                    Stages = new List<StageDistributionDto>(),
                    OverallProgress = 0
                };
            }
            
            // Sort in memory only if needed (small dataset)
            stages = stages.OrderBy(s => s.Order).ToList();

            _logger.LogDebug("GetStageDistributionAsync: Loaded {Count} stages in {Ms}ms", stages.Count, sw.ElapsedMilliseconds);

            // Admin bypass - skip permission filtering for better performance
            if (_permissionHelpers.HasAdminPrivileges())
            {
                return await GetStageDistributionForAdminAsync(stages, workflowId, tenantId);
            }

            // Get active cases with minimal fields for permission check
            var activeCases = await _onboardingRepository.GetListAsync(o => 
                o.IsActive && o.IsValid && 
                o.TenantId == tenantId &&
                (o.Status == "Started" || o.Status == "InProgress" || o.Status == "Active") &&
                (workflowId == null || o.WorkflowId == workflowId));

            _logger.LogDebug("GetStageDistributionAsync: Loaded {Count} active cases in {Ms}ms", activeCases.Count, sw.ElapsedMilliseconds);

            // Apply permission filter with pre-loaded workflows
            var filteredCases = await FilterCasesByPermissionOptimizedAsync(activeCases);

            _logger.LogDebug("GetStageDistributionAsync: Filtered to {Count} cases in {Ms}ms", filteredCases.Count, sw.ElapsedMilliseconds);

            // Group by stage ID and count (filter out null CurrentStageId)
            var stageCounts = filteredCases
                .Where(o => o.CurrentStageId.HasValue)
                .GroupBy(o => o.CurrentStageId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            var totalCases = filteredCases.Count;

            // Build stage distribution
            var stageDistribution = stages.Select(stage => new StageDistributionDto
            {
                StageId = stage.Id,
                StageName = stage.Name,
                CaseCount = stageCounts.TryGetValue(stage.Id, out var count) ? count : 0,
                Order = stage.Order,
                Color = stage.Color,
                Percentage = totalCases > 0 
                    ? Math.Round((decimal)(stageCounts.TryGetValue(stage.Id, out var c) ? c : 0) / totalCases * 100, 1) 
                    : 0
            }).ToList();

            // Calculate overall progress (average completion rate of filtered cases)
            var overallProgress = filteredCases.Any() 
                ? Math.Round(filteredCases.Average(c => c.CompletionRate), 0) 
                : 0;

            _logger.LogInformation("GetStageDistributionAsync: Completed in {Ms}ms", sw.ElapsedMilliseconds);

            return new StageDistributionResultDto
            {
                Stages = stageDistribution,
                OverallProgress = overallProgress
            };
        }

        /// <summary>
        /// Optimized stage distribution for admin users (no permission filtering)
        /// </summary>
        private async Task<StageDistributionResultDto> GetStageDistributionForAdminAsync(List<Stage> stages, long? workflowId, string tenantId)
        {
            var activeCases = await _onboardingRepository.GetListAsync(o => 
                o.IsActive && o.IsValid && 
                o.TenantId == tenantId &&
                (o.Status == "Started" || o.Status == "InProgress" || o.Status == "Active") &&
                (workflowId == null || o.WorkflowId == workflowId));

            var stageCounts = activeCases
                .Where(o => o.CurrentStageId.HasValue)
                .GroupBy(o => o.CurrentStageId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            var totalCases = activeCases.Count;

            var stageDistribution = stages.Select(stage => new StageDistributionDto
            {
                StageId = stage.Id,
                StageName = stage.Name,
                CaseCount = stageCounts.TryGetValue(stage.Id, out var count) ? count : 0,
                Order = stage.Order,
                Color = stage.Color,
                Percentage = totalCases > 0 
                    ? Math.Round((decimal)(stageCounts.TryGetValue(stage.Id, out var c) ? c : 0) / totalCases * 100, 1) 
                    : 0
            }).ToList();

            var overallProgress = activeCases.Any() 
                ? Math.Round(activeCases.Average(c => c.CompletionRate), 0) 
                : 0;

            return new StageDistributionResultDto
            {
                Stages = stageDistribution,
                OverallProgress = overallProgress
            };
        }

        /// <summary>
        /// Get pending tasks for current user
        /// </summary>
        public async Task<PagedResult<DashboardTaskDto>> GetTasksAsync(DashboardTaskQueryDto query)
        {
            var userId = long.TryParse(_userContext?.UserId, out var uid) ? uid : 0;
            var userTeamIds = _userContext.UserTeams?.GetAllTeamIds() ?? new List<long>();

            // Build query for tasks assigned to current user or their teams
            // Note: We need to get more tasks than requested for permission filtering
            var tasks = await _checklistTaskRepository.GetPendingTasksForUserAsync(
                userId, 
                userTeamIds, 
                query.Category,
                1,  // Start from page 1
                1000);  // Get more tasks for permission filtering

            if (!tasks.Any())
            {
                return new PagedResult<DashboardTaskDto>
                {
                    Items = new List<DashboardTaskDto>(),
                    TotalCount = 0,
                    PageIndex = query.PageIndex,
                    PageSize = query.PageSize
                };
            }

            // Get unique onboarding IDs and filter by operate permission
            var onboardingIds = tasks.Select(t => t.OnboardingId).Distinct().ToList();
            var onboardings = await _onboardingRepository.GetListAsync(o => onboardingIds.Contains(o.Id));
            var filteredOnboardings = await FilterCasesByOperatePermissionAsync(onboardings);
            var allowedOnboardingIds = filteredOnboardings.Select(o => o.Id).ToHashSet();

            // Filter tasks by allowed onboardings
            var filteredTasks = tasks.Where(t => allowedOnboardingIds.Contains(t.OnboardingId)).ToList();

            var totalCount = filteredTasks.Count;

            // Apply pagination after permission filtering
            var pagedTasks = filteredTasks
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            var now = DateTimeOffset.UtcNow;
            var taskDtos = pagedTasks.Select(t => new DashboardTaskDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Priority = t.Priority,
                DueDate = t.DueDate,
                IsOverdue = t.DueDate.HasValue && t.DueDate.Value < now && !t.IsCompleted,
                DaysUntilDue = t.DueDate.HasValue ? (int)(t.DueDate.Value - now).TotalDays : null,
                DueDateDisplay = FormatDueDate(t.DueDate, now),
                CaseCode = t.CaseCode ?? string.Empty,
                CaseName = t.CaseName ?? string.Empty,
                OnboardingId = t.OnboardingId,
                ChecklistId = t.ChecklistId,
                AssignedTeam = t.AssignedTeam,
                AssigneeName = t.AssigneeName,
                Category = DetermineTaskCategory(t.AssignedTeam),
                Status = t.Status,
                IsRequired = t.IsRequired,
                IsCompleted = t.IsCompleted
            }).ToList();

            return new PagedResult<DashboardTaskDto>
            {
                Items = taskDtos,
                TotalCount = totalCount,
                PageIndex = query.PageIndex,
                PageSize = query.PageSize
            };
        }

        /// <summary>
        /// Get recent messages summary - uses same format as /api/ow/messages/v1
        /// </summary>
        public async Task<MessageSummaryDto> GetMessageSummaryAsync(int limit = 5)
        {
            // Use MessageService.GetPagedAsync to get messages in the same format as /api/ow/messages/v1
            var query = new MessageQueryDto
            {
                PageIndex = 1,
                PageSize = limit,
                Folder = "Inbox"
            };

            var result = await _messageService.GetPagedAsync(query);
            var unreadCount = result.Data?.Count(m => !m.IsRead) ?? 0;

            return new MessageSummaryDto
            {
                Messages = result.Data?.ToList() ?? new List<MessageListItemDto>(),
                UnreadCount = unreadCount
            };
        }

        /// <summary>
        /// Get recent achievements/milestones (includes both case completions and stage completions)
        /// Performance optimized: limits data loading and uses parallel processing
        /// </summary>
        public async Task<List<AchievementDto>> GetAchievementsAsync(int limit = 5, string? team = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var achievements = new List<AchievementDto>();

            // Admin bypass for better performance
            var isAdmin = _permissionHelpers.HasAdminPrivileges();

            // Get recently completed cases (CaseCompleted type) - already limited by repository
            var completedCases = await _onboardingRepository.GetRecentlyCompletedAsync(limit * 2, team);
            
            _logger.LogDebug("GetAchievementsAsync: Loaded {Count} completed cases in {Ms}ms", completedCases.Count, sw.ElapsedMilliseconds);

            // Apply permission filter only for non-admin users
            var filteredCompletedCases = isAdmin 
                ? completedCases 
                : await FilterCasesByPermissionOptimizedAsync(completedCases);
            
            var caseAchievements = filteredCompletedCases.Take(limit).Select(c => new AchievementDto
            {
                Id = c.Id,
                Title = $"{c.CaseName} case completed",
                Description = c.ActualCompletionDate.HasValue && c.StartDate.HasValue
                    ? $"Successfully completed all case stages in {(int)(c.ActualCompletionDate.Value - c.StartDate.Value).TotalDays} days"
                    : "Successfully completed all case stages",
                CompletionDate = c.ActualCompletionDate ?? c.ModifyDate,
                CompletionDateDisplay = FormatCompletionDate(c.ActualCompletionDate ?? c.ModifyDate),
                Teams = ParseTeams(c.OperateTeams),
                Type = "CaseCompleted",
                CaseCode = c.CaseCode,
                CaseName = c.CaseName,
                DaysToComplete = c.ActualCompletionDate.HasValue && c.StartDate.HasValue
                    ? (int)(c.ActualCompletionDate.Value - c.StartDate.Value).TotalDays
                    : null
            }).ToList();

            achievements.AddRange(caseAchievements);

            _logger.LogDebug("GetAchievementsAsync: Processed case achievements in {Ms}ms", sw.ElapsedMilliseconds);

            // Get recently completed stages (StageCompleted type) - optimized version
            var stageAchievements = await GetRecentlyCompletedStagesOptimizedAsync(limit, team, isAdmin);
            achievements.AddRange(stageAchievements);

            _logger.LogInformation("GetAchievementsAsync: Completed in {Ms}ms with {Count} achievements", sw.ElapsedMilliseconds, achievements.Count);

            // Sort by completion date descending and take the limit
            return achievements
                .OrderByDescending(a => a.CompletionDate)
                .Take(limit)
                .ToList();
        }

        /// <summary>
        /// Optimized version of GetRecentlyCompletedStagesAsync
        /// Limits data loading and avoids unnecessary JSON parsing
        /// </summary>
        private async Task<List<AchievementDto>> GetRecentlyCompletedStagesOptimizedAsync(int limit, string? team, bool isAdmin)
        {
            var achievements = new List<AchievementDto>();

            try
            {
                // Get recent cases without filtering by StagesProgressJson in SQL
                // This avoids PostgreSQL JSON parsing errors for invalid data
                var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
                var cutoffDate = DateTimeOffset.UtcNow.AddDays(-30);
                var filterByTeam = !string.IsNullOrEmpty(team);
                
                var allCases = await _onboardingRepository.GetListAsync(o =>
                    o.IsValid &&
                    o.TenantId == tenantId &&
                    o.ModifyDate >= cutoffDate &&
                    (!filterByTeam || o.CurrentTeam == team));

                // Filter in memory to avoid SQL JSON parsing issues
                var recentCases = allCases
                    .Where(o => !string.IsNullOrEmpty(o.StagesProgressJson))
                    .OrderByDescending(o => o.ModifyDate)
                    .Take(100) // Process at most 100 recent cases
                    .ToList();

                // Apply permission filter only for non-admin users
                var filteredCases = isAdmin 
                    ? recentCases 
                    : await FilterCasesByPermissionOptimizedAsync(recentCases);

                _logger.LogDebug("GetRecentlyCompletedStagesOptimizedAsync: Processing {Count} cases", filteredCases.Count);

                // Get all stages for enrichment (batch load)
                var stageIds = new HashSet<long>();

                // First pass: parse JSON using helper and collect stage IDs
                var casesWithProgress = new List<(Onboarding Onboarding, List<OnboardingStageProgress> Progress)>();
                foreach (var onboarding in filteredCases)
                {
                    // Use JsonParsingHelper for consistent parsing with automatic unwrapping
                    if (JsonParsingHelper.TryParseStagesProgress(onboarding.StagesProgressJson, out var progress, _logger))
                    {
                        // Only keep cases with completed stages
                        var completedStages = progress.Where(p => p.IsCompleted && p.CompletionTime.HasValue).ToList();
                        if (completedStages.Any())
                        {
                            casesWithProgress.Add((onboarding, completedStages));
                            foreach (var p in completedStages)
                            {
                                stageIds.Add(p.StageId);
                            }
                        }
                    }
                }

                // Get stage names from Stage repository (batch load using optimized method)
                var stages = stageIds.Any() 
                    ? await _stageRepository.GetByIdsAsync(stageIds.ToList())
                    : new List<Stage>();
                var stageDict = stages.ToDictionary(s => s.Id, s => s);

                // Extract completed stages from each case
                foreach (var (onboarding, progress) in casesWithProgress)
                {
                    var completedStages = progress
                        .Select(s =>
                        {
                            var stage = stageDict.GetValueOrDefault(s.StageId);
                            var stageName = stage?.Name ?? s.StageName ?? $"Stage {s.StageId}";
                            var stageOrder = stage?.Order ?? s.StageOrder;

                            return new AchievementDto
                            {
                                Id = s.StageId,
                                Title = $"{stageName} stage completed",
                                Description = s.StartTime.HasValue && s.CompletionTime.HasValue
                                    ? $"Stage completed in {(int)(s.CompletionTime.Value - s.StartTime.Value).TotalDays} days"
                                    : "Stage completed successfully",
                                CompletionDate = s.CompletionTime!.Value,
                                CompletionDateDisplay = FormatCompletionDate(s.CompletionTime!.Value),
                                Teams = ParseTeams(onboarding.OperateTeams),
                                Type = "StageCompleted",
                                CaseCode = onboarding.CaseCode,
                                CaseName = onboarding.CaseName,
                                DaysToComplete = s.StartTime.HasValue && s.CompletionTime.HasValue
                                    ? (int)(s.CompletionTime.Value - s.StartTime.Value).TotalDays
                                    : null,
                                StageId = s.StageId,
                                StageName = stageName,
                                StageOrder = stageOrder,
                                OnboardingId = onboarding.Id,
                                CompletedBy = s.CompletedBy
                            };
                        });

                    achievements.AddRange(completedStages);
                }

                // Sort by completion date descending and take the limit
                return achievements
                    .OrderByDescending(a => a.CompletionDate)
                    .Take(limit)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recently completed stages");
                return new List<AchievementDto>();
            }
        }

        /// <summary>
        /// Get upcoming deadlines based on Stage EndTime from StagesProgress
        /// Uses same filtering logic as Tasks (based on Task AssigneeId)
        /// </summary>
        public async Task<List<DeadlineDto>> GetDeadlinesAsync(int days = 7)
        {
            var userId = long.TryParse(_userContext?.UserId, out var uid) ? uid : 0;
            var userTeamIds = _userContext.UserTeams?.GetAllTeamIds() ?? new List<long>();
            var now = DateTimeOffset.UtcNow;
            var endDate = now.AddDays(days);

            var deadlines = new List<DeadlineDto>();

            try
            {
                // Get onboarding IDs from tasks assigned to current user (same logic as Tasks API)
                // This ensures Deadlines and Tasks use the same filtering criteria
                // Use reasonable limit to avoid memory issues - deadlines typically don't need all tasks
                const int MaxTasksForDeadlines = 5000;
                var userTasks = await _checklistTaskRepository.GetPendingTasksForUserAsync(
                    userId, userTeamIds, null, 1, MaxTasksForDeadlines);
                
                var onboardingIds = userTasks.Select(t => t.OnboardingId).Distinct().ToList();
                
                if (!onboardingIds.Any())
                {
                    _logger.LogDebug("GetDeadlinesAsync: No tasks found for user {UserId}, returning empty deadlines", userId);
                    return deadlines;
                }

                // Get active cases that have tasks assigned to current user
                var activeCases = await _onboardingRepository.GetListAsync(o =>
                    o.IsValid && o.IsActive &&
                    (o.Status == "Started" || o.Status == "InProgress" || o.Status == "Active") &&
                    onboardingIds.Contains(o.Id));

                // Apply permission filter
                var filteredCases = await FilterCasesByPermissionAsync(activeCases);

                _logger.LogDebug("GetDeadlinesAsync: Found {Count} active cases for user {UserId}, {FilteredCount} after permission filter", 
                    activeCases.Count, userId, filteredCases.Count);

                // Get all stages for enrichment (batch load)
                var allStageIds = new HashSet<long>();

                // First pass: collect all stage IDs using JsonParsingHelper
                var casesWithProgress = new List<(Onboarding Onboarding, List<OnboardingStageProgress> Progress)>();
                foreach (var onboarding in filteredCases)
                {
                    if (string.IsNullOrEmpty(onboarding.StagesProgressJson))
                        continue;

                    // Use JsonParsingHelper for consistent parsing
                    if (JsonParsingHelper.TryParseStagesProgress(onboarding.StagesProgressJson, out var progress, _logger))
                    {
                        casesWithProgress.Add((onboarding, progress));
                        foreach (var p in progress)
                        {
                            allStageIds.Add(p.StageId);
                        }
                    }
                }

                // Get stage details using batch method
                var stages = allStageIds.Any()
                    ? await _stageRepository.GetByIdsAsync(allStageIds.ToList())
                    : new List<Stage>();
                var stageDict = stages.ToDictionary(s => s.Id, s => s);

                // Second pass: extract deadlines
                foreach (var (onboarding, progress) in casesWithProgress)
                {
                    foreach (var stageProgress in progress)
                    {
                        // Skip completed stages
                        if (stageProgress.IsCompleted)
                            continue;

                        // Get stage info for EstimatedDays
                        var stage = stageDict.GetValueOrDefault(stageProgress.StageId);
                        if (stage != null)
                        {
                            stageProgress.EstimatedDays = stage.EstimatedDuration;
                            stageProgress.StageName = stage.Name;
                            stageProgress.StageOrder = stage.Order;
                        }

                        // Calculate EndTime
                        var stageEndTime = stageProgress.EndTime;

                        // Only include if EndTime is within the deadline range
                        if (stageEndTime.HasValue && stageEndTime.Value <= endDate)
                        {
                            var stageName = stage?.Name ?? $"Stage {stageProgress.StageId}";
                            var stageOrder = stage?.Order ?? 0;

                            deadlines.Add(new DeadlineDto
                            {
                                Id = stageProgress.StageId,
                                Name = stageName,
                                DueDate = stageEndTime.Value,
                                DueDateDisplay = FormatDueDate(stageEndTime, now),
                                Urgency = DetermineUrgency(stageEndTime.Value, now),
                                DaysUntilDue = (int)(stageEndTime.Value - now).TotalDays,
                                CaseCode = onboarding.CaseCode ?? string.Empty,
                                CaseName = onboarding.CaseName ?? string.Empty,
                                OnboardingId = onboarding.Id,
                                ChecklistId = 0,
                                Type = "Stage",
                                Priority = stageEndTime.Value < now ? "Critical" : 
                                          (stageEndTime.Value - now).TotalDays <= 1 ? "High" :
                                          (stageEndTime.Value - now).TotalDays <= 3 ? "Medium" : "Low",
                                AssignedTeam = onboarding.CurrentTeam,
                                StageId = stageProgress.StageId,
                                StageName = stageName,
                                StageOrder = stageOrder
                            });
                        }
                    }
                }

                _logger.LogDebug("GetDeadlinesAsync: Found {Count} stage deadlines", deadlines.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stage deadlines");
            }

            // Sort all deadlines by due date
            return deadlines.OrderBy(d => d.DueDate).ToList();
        }

        #region Private Helper Methods

        /// <summary>
        /// Optimized: Get active cases count (admin bypass, no permission filter)
        /// </summary>
        private async Task<int> GetActiveCasesCountOptimizedAsync(string? team, bool isAdmin)
        {
            var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
            var filterByTeam = !string.IsNullOrEmpty(team);
            var cases = await _onboardingRepository.GetListAsync(o => 
                o.IsActive && o.IsValid && 
                o.TenantId == tenantId &&
                (o.Status == "Started" || o.Status == "InProgress" || o.Status == "Active") &&
                (!filterByTeam || o.CurrentTeam == team));
            
            if (isAdmin)
                return cases.Count;
            
            var filteredCases = await FilterCasesByPermissionOptimizedAsync(cases);
            return filteredCases.Count;
        }

        /// <summary>
        /// Optimized: Get active cases count at specific date (admin bypass)
        /// </summary>
        private async Task<int> GetActiveCasesCountAtDateOptimizedAsync(DateTimeOffset date, string? team, bool isAdmin)
        {
            var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
            var filterByTeam = !string.IsNullOrEmpty(team);
            var cases = await _onboardingRepository.GetListAsync(o => 
                o.IsValid && 
                o.TenantId == tenantId &&
                o.StartDate <= date &&
                (o.ActualCompletionDate == null || o.ActualCompletionDate > date) &&
                (!filterByTeam || o.CurrentTeam == team));
            
            if (isAdmin)
                return cases.Count;
            
            var filteredCases = await FilterCasesByPermissionOptimizedAsync(cases);
            return filteredCases.Count;
        }

        /// <summary>
        /// Optimized: Get completed cases count (admin bypass)
        /// </summary>
        private async Task<int> GetCompletedCasesCountOptimizedAsync(DateTimeOffset startDate, DateTimeOffset endDate, string? team, bool isAdmin)
        {
            var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
            var filterByTeam = !string.IsNullOrEmpty(team);
            var cases = await _onboardingRepository.GetListAsync(o => 
                o.IsValid && 
                o.TenantId == tenantId &&
                (o.Status == "Completed" || o.Status == "Force Completed") &&
                o.ActualCompletionDate >= startDate &&
                o.ActualCompletionDate <= endDate &&
                (!filterByTeam || o.CurrentTeam == team));
            
            if (isAdmin)
                return cases.Count;
            
            var filteredCases = await FilterCasesByPermissionOptimizedAsync(cases);
            return filteredCases.Count;
        }

        /// <summary>
        /// Optimized: Get average completion time (admin bypass)
        /// </summary>
        private async Task<decimal> GetAverageCompletionTimeOptimizedAsync(DateTimeOffset startDate, DateTimeOffset endDate, string? team, bool isAdmin)
        {
            var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
            var filterByTeam = !string.IsNullOrEmpty(team);
            var completedCases = await _onboardingRepository.GetListAsync(o => 
                o.IsValid && 
                o.TenantId == tenantId &&
                (o.Status == "Completed" || o.Status == "Force Completed") &&
                o.ActualCompletionDate >= startDate &&
                o.ActualCompletionDate <= endDate &&
                o.StartDate.HasValue &&
                (!filterByTeam || o.CurrentTeam == team));

            var filteredCases = isAdmin ? completedCases : await FilterCasesByPermissionOptimizedAsync(completedCases);

            if (!filteredCases.Any())
                return 0;

            var totalDays = filteredCases
                .Where(c => c.StartDate.HasValue && c.ActualCompletionDate.HasValue)
                .Sum(c => (c.ActualCompletionDate!.Value - c.StartDate!.Value).TotalDays);

            return Math.Round((decimal)(totalDays / filteredCases.Count), 0);
        }
        private async Task<int> GetOverdueTasksCountAsync(string? team)
        {
            var filterByTeam = !string.IsNullOrEmpty(team);
            var now = DateTimeOffset.UtcNow;
            var tasks = await _checklistTaskRepository.GetListAsync(t => 
                t.IsActive && t.IsValid && 
                !t.IsCompleted &&
                t.DueDate.HasValue && t.DueDate.Value < now &&
                (!filterByTeam || t.AssignedTeam == team));
            return tasks.Count;
        }

        private async Task<int> GetOverdueTasksCountAtDateAsync(DateTimeOffset date, string? team)
        {
            var filterByTeam = !string.IsNullOrEmpty(team);
            var tasks = await _checklistTaskRepository.GetListAsync(t => 
                t.IsValid && 
                !t.IsCompleted &&
                t.DueDate.HasValue && t.DueDate.Value < date &&
                t.CreateDate <= date &&
                (!filterByTeam || t.AssignedTeam == team));
            return tasks.Count;
        }

        private StatisticItemDto CreateStatisticItem(decimal currentValue, decimal previousValue, bool increaseIsPositive, string? suffix = null)
        {
            var difference = currentValue - previousValue;
            var trend = difference > 0 ? "up" : (difference < 0 ? "down" : "neutral");
            var isPositive = increaseIsPositive ? difference >= 0 : difference <= 0;

            return new StatisticItemDto
            {
                Value = currentValue,
                Difference = difference,
                Trend = trend,
                IsPositive = isPositive,
                Suffix = suffix
            };
        }

        private string FormatDueDate(DateTimeOffset? dueDate, DateTimeOffset now)
        {
            if (!dueDate.HasValue)
                return string.Empty;

            var days = (int)(dueDate.Value.Date - now.Date).TotalDays;

            return days switch
            {
                < 0 => $"Overdue by {Math.Abs(days)} day(s)",
                0 => "Due: Today",
                1 => "Due: Tomorrow",
                _ => $"Due in {days} days"
            };
        }

        private string FormatCompletionDate(DateTimeOffset date)
        {
            return date.ToString("MMM d, yyyy");
        }

        private List<string> ParseTeams(string? teamsJson)
        {
            if (string.IsNullOrWhiteSpace(teamsJson))
                return new List<string>();

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(teamsJson) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private string DetermineTaskCategory(string? assignedTeam)
        {
            if (string.IsNullOrWhiteSpace(assignedTeam))
                return "Other";

            var teamLower = assignedTeam.ToLower();
            if (teamLower.Contains("sales"))
                return "Sales";
            if (teamLower.Contains("account"))
                return "Account";
            return "Other";
        }

        private string DetermineUrgency(DateTimeOffset dueDate, DateTimeOffset now)
        {
            var days = (int)(dueDate.Date - now.Date).TotalDays;

            return days switch
            {
                < 0 => "overdue",
                0 => "today",
                1 => "tomorrow",
                <= 7 => "thisWeek",
                _ => "upcoming"
            };
        }

        /// <summary>
        /// Optimized filter cases by permission (with cached workflow data)
        /// Reuses workflow data if already loaded, avoiding duplicate DB queries
        /// </summary>
        private async Task<List<Onboarding>> FilterCasesByPermissionOptimizedAsync(
            List<Onboarding> cases, 
            Dictionary<long, Workflow>? preloadedWorkflows = null)
        {
            if (cases == null || !cases.Any())
            {
                return new List<Onboarding>();
            }

            // Admin bypass - return all cases
            if (_permissionHelpers.HasAdminPrivileges())
            {
                return cases;
            }

            var userId = long.TryParse(_userContext?.UserId, out var uid) ? uid : 0;
            var userTeamIds = _userContext.UserTeams?.GetAllTeamIds() ?? new List<long>();
            var userIdString = userId.ToString();

            // Use preloaded workflows, request-scoped cache, or load them
            Dictionary<long, Workflow> workflowDict;
            if (preloadedWorkflows != null)
            {
                workflowDict = preloadedWorkflows;
            }
            else
            {
                // Use request-scoped cache to avoid repeated DB queries
                var workflowIds = cases.Select(c => c.WorkflowId).Distinct().ToList();
                workflowDict = await GetOrLoadWorkflowsAsync(workflowIds);
            }

            var filteredCases = new List<Onboarding>();

            foreach (var entity in cases)
            {
                var workflow = workflowDict.GetValueOrDefault(entity.WorkflowId);
                if (CheckCaseViewPermissionInMemory(entity, workflow, userId, userTeamIds, userIdString))
                {
                    filteredCases.Add(entity);
                }
            }

            return filteredCases;
        }

        /// <summary>
        /// Filter cases by permission (in-memory check)
        /// Uses request-scoped workflow cache for better performance
        /// </summary>
        private async Task<List<Onboarding>> FilterCasesByPermissionAsync(List<Onboarding> cases)
        {
            if (cases == null || !cases.Any())
            {
                return new List<Onboarding>();
            }

            // Admin bypass - return all cases
            if (_permissionHelpers.HasAdminPrivileges())
            {
                _logger.LogDebug("User has admin privileges - returning all {Count} cases", cases.Count);
                return cases;
            }

            var userId = long.TryParse(_userContext?.UserId, out var uid) ? uid : 0;
            var userTeamIds = _userContext.UserTeams?.GetAllTeamIds() ?? new List<long>();
            var userIdString = userId.ToString();

            // Use request-scoped cache to avoid repeated DB queries
            var workflowIds = cases.Select(c => c.WorkflowId).Distinct().ToList();
            var workflowDict = await GetOrLoadWorkflowsAsync(workflowIds);

            var filteredCases = new List<Onboarding>();

            foreach (var entity in cases)
            {
                var workflow = workflowDict.GetValueOrDefault(entity.WorkflowId);
                if (CheckCaseViewPermissionInMemory(entity, workflow, userId, userTeamIds, userIdString))
                {
                    filteredCases.Add(entity);
                }
            }

            _logger.LogDebug("Permission filter: {Original} cases -> {Filtered} cases", cases.Count, filteredCases.Count);
            return filteredCases;
        }

        /// <summary>
        /// Filter cases by operate permission (in-memory check)
        /// Uses request-scoped workflow cache for better performance
        /// </summary>
        private async Task<List<Onboarding>> FilterCasesByOperatePermissionAsync(List<Onboarding> cases)
        {
            if (cases == null || !cases.Any())
            {
                return new List<Onboarding>();
            }

            // Admin bypass - return all cases
            if (_permissionHelpers.HasAdminPrivileges())
            {
                _logger.LogDebug("User has admin privileges - returning all {Count} cases for operate", cases.Count);
                return cases;
            }

            var userId = long.TryParse(_userContext?.UserId, out var uid) ? uid : 0;
            var userTeamIds = _userContext.UserTeams?.GetAllTeamIds() ?? new List<long>();
            var userIdString = userId.ToString();

            // Use request-scoped cache to avoid repeated DB queries
            var workflowIds = cases.Select(c => c.WorkflowId).Distinct().ToList();
            var workflowDict = await GetOrLoadWorkflowsAsync(workflowIds);

            var filteredCases = new List<Onboarding>();

            foreach (var entity in cases)
            {
                var workflow = workflowDict.GetValueOrDefault(entity.WorkflowId);
                if (CheckCaseOperatePermissionInMemory(entity, workflow, userId, userTeamIds, userIdString))
                {
                    filteredCases.Add(entity);
                }
            }

            _logger.LogDebug("Operate permission filter: {Original} cases -> {Filtered} cases", cases.Count, filteredCases.Count);
            return filteredCases;
        }

        /// <summary>
        /// Check Case operate permission in memory (no DB queries)
        /// </summary>
        private bool CheckCaseOperatePermissionInMemory(
            Onboarding entity,
            Workflow? workflow,
            long userId,
            List<long> userTeamIds,
            string userIdString)
        {
            // Step 1: Check Ownership (owner has full access)
            if (entity.Ownership.HasValue && entity.Ownership.Value == userId)
            {
                return true;
            }

            // Step 2: In Public mode or UseSameTeamForOperate, check operate teams
            if (entity.ViewPermissionMode == ViewPermissionModeEnum.Public)
            {
                if (workflow == null)
                {
                    return false;
                }
                return CheckWorkflowOperatePermissionInMemory(workflow, userId, userTeamIds);
            }

            // Step 3: Check Case-specific operate permissions
            return CheckCaseOperatePermissionBySubjectType(entity, userTeamIds, userIdString);
        }

        /// <summary>
        /// Check Workflow operate permission in memory
        /// </summary>
        private bool CheckWorkflowOperatePermissionInMemory(Workflow workflow, long userId, List<long> userTeamIds)
        {
            var userTeamStrings = userTeamIds.Select(t => t.ToString()).ToList();

            // Check operate teams
            if (!string.IsNullOrWhiteSpace(workflow.OperateTeams))
            {
                var operateTeams = ParseJsonArraySafe(workflow.OperateTeams);
                if (operateTeams.Any())
                {
                    return userTeamStrings.Any(ut => operateTeams.Contains(ut, StringComparer.OrdinalIgnoreCase));
                }
            }

            // If no operate teams defined, fall back to view permission
            return CheckWorkflowViewPermissionInMemory(workflow, userId, userTeamIds);
        }

        /// <summary>
        /// Check Case operate permission based on SubjectType (Team or User)
        /// </summary>
        private bool CheckCaseOperatePermissionBySubjectType(
            Onboarding entity,
            List<long> userTeamIds,
            string userIdString)
        {
            var userTeamStrings = userTeamIds.Select(t => t.ToString()).ToList();

            // If UseSameTeamForOperate is true, use view permission settings
            if (entity.UseSameTeamForOperate)
            {
                return CheckCaseViewPermissionBySubjectType(entity, userTeamIds, userIdString);
            }

            // Check operate permission based on subject type
            if (entity.OperatePermissionSubjectType == PermissionSubjectTypeEnum.Team)
            {
                if (string.IsNullOrWhiteSpace(entity.OperateTeams))
                {
                    return false;
                }
                var operateTeams = ParseJsonArraySafe(entity.OperateTeams);
                return userTeamStrings.Any(ut => operateTeams.Contains(ut, StringComparer.OrdinalIgnoreCase));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(entity.OperateUsers))
                {
                    return false;
                }
                var operateUsers = ParseJsonArraySafe(entity.OperateUsers);
                return operateUsers.Contains(userIdString, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Check Case view permission in memory (no DB queries)
        /// Mirrors the logic from CasePermissionService.CheckCasePermissionAsync
        /// </summary>
        private bool CheckCaseViewPermissionInMemory(
            Onboarding entity,
            Workflow? workflow,
            long userId,
            List<long> userTeamIds,
            string userIdString)
        {
            // Step 1: Check Ownership (owner has full access)
            if (entity.Ownership.HasValue && entity.Ownership.Value == userId)
            {
                return true;
            }

            // Step 2: In Public mode, inherit Workflow permissions
            if (entity.ViewPermissionMode == ViewPermissionModeEnum.Public)
            {
                if (workflow == null)
                {
                    return false;
                }
                return CheckWorkflowViewPermissionInMemory(workflow, userId, userTeamIds);
            }

            // Step 3: Check Case-specific view permissions (non-Public modes)
            return CheckCaseViewPermissionBySubjectType(entity, userTeamIds, userIdString);
        }

        /// <summary>
        /// Check Workflow view permission in memory
        /// </summary>
        private bool CheckWorkflowViewPermissionInMemory(Workflow workflow, long userId, List<long> userTeamIds)
        {
            if (workflow.ViewPermissionMode == ViewPermissionModeEnum.Public)
            {
                return true;
            }

            var userTeamStrings = userTeamIds.Select(t => t.ToString()).ToList();

            if (workflow.ViewPermissionMode == ViewPermissionModeEnum.VisibleToTeams)
            {
                if (string.IsNullOrWhiteSpace(workflow.ViewTeams))
                {
                    return false;
                }
                var viewTeams = ParseJsonArraySafe(workflow.ViewTeams);
                return userTeamStrings.Any(ut => viewTeams.Contains(ut, StringComparer.OrdinalIgnoreCase));
            }

            if (workflow.ViewPermissionMode == ViewPermissionModeEnum.InvisibleToTeams)
            {
                if (string.IsNullOrWhiteSpace(workflow.ViewTeams))
                {
                    return true; // Empty blacklist = everyone can view
                }
                var viewTeams = ParseJsonArraySafe(workflow.ViewTeams);
                return !userTeamStrings.Any(ut => viewTeams.Contains(ut, StringComparer.OrdinalIgnoreCase));
            }

            return false;
        }

        /// <summary>
        /// Check Case view permission based on SubjectType (Team or User)
        /// </summary>
        private bool CheckCaseViewPermissionBySubjectType(
            Onboarding entity,
            List<long> userTeamIds,
            string userIdString)
        {
            var userTeamStrings = userTeamIds.Select(t => t.ToString()).ToList();

            if (entity.ViewPermissionMode == ViewPermissionModeEnum.VisibleToTeams)
            {
                if (entity.ViewPermissionSubjectType == PermissionSubjectTypeEnum.Team)
                {
                    if (string.IsNullOrWhiteSpace(entity.ViewTeams))
                    {
                        return false;
                    }
                    var viewTeams = ParseJsonArraySafe(entity.ViewTeams);
                    return userTeamStrings.Any(ut => viewTeams.Contains(ut, StringComparer.OrdinalIgnoreCase));
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(entity.ViewUsers))
                    {
                        return false;
                    }
                    var viewUsers = ParseJsonArraySafe(entity.ViewUsers);
                    return viewUsers.Contains(userIdString, StringComparer.OrdinalIgnoreCase);
                }
            }

            if (entity.ViewPermissionMode == ViewPermissionModeEnum.InvisibleToTeams)
            {
                if (entity.ViewPermissionSubjectType == PermissionSubjectTypeEnum.Team)
                {
                    if (string.IsNullOrWhiteSpace(entity.ViewTeams))
                    {
                        return true; // Empty blacklist = everyone can view
                    }
                    var viewTeams = ParseJsonArraySafe(entity.ViewTeams);
                    return !userTeamStrings.Any(ut => viewTeams.Contains(ut, StringComparer.OrdinalIgnoreCase));
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(entity.ViewUsers))
                    {
                        return true;
                    }
                    var viewUsers = ParseJsonArraySafe(entity.ViewUsers);
                    return !viewUsers.Contains(userIdString, StringComparer.OrdinalIgnoreCase);
                }
            }

            if (entity.ViewPermissionMode == ViewPermissionModeEnum.Private)
            {
                return false; // Owner check is handled above
            }

            return false;
        }

        /// <summary>
        /// Parse JSON array safely
        /// </summary>
        private List<string> ParseJsonArraySafe(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<string>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        #endregion
    }
}
