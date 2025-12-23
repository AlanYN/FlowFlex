using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Dashboard;
using FlowFlex.Application.Contracts.Dtos.OW.Message;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Dashboard service implementation - provides aggregated data for dashboard UI
    /// </summary>
    public class DashboardService : IDashboardService, IScopedService
    {
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IChecklistTaskRepository _checklistTaskRepository;
        private readonly IStageRepository _stageRepository;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly UserContext _userContext;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            IOnboardingRepository onboardingRepository,
            IChecklistTaskRepository checklistTaskRepository,
            IStageRepository stageRepository,
            IMessageService messageService,
            IMapper mapper,
            UserContext userContext,
            ILogger<DashboardService> logger)
        {
            _onboardingRepository = onboardingRepository;
            _checklistTaskRepository = checklistTaskRepository;
            _stageRepository = stageRepository;
            _messageService = messageService;
            _mapper = mapper;
            _userContext = userContext;
            _logger = logger;
        }

        /// <summary>
        /// Get aggregated dashboard data with optional module selection
        /// </summary>
        public async Task<DashboardDto> GetDashboardAsync(DashboardQueryDto query)
        {
            var result = new DashboardDto();
            var modules = query.Modules ?? new List<string>();
            var includeAll = modules.Count == 0;

            // Execute modules in parallel for better performance
            var tasks = new List<Task>();

            if (includeAll || modules.Contains("statistics", StringComparer.OrdinalIgnoreCase))
            {
                tasks.Add(Task.Run(async () =>
                {
                    result.Statistics = await GetStatisticsAsync(query.Team);
                }));
            }

            if (includeAll || modules.Contains("casesOverview", StringComparer.OrdinalIgnoreCase) || modules.Contains("stageDistribution", StringComparer.OrdinalIgnoreCase))
            {
                tasks.Add(Task.Run(async () =>
                {
                    result.CasesOverview = await GetStageDistributionAsync(query.WorkflowId);
                }));
            }

            if (includeAll || modules.Contains("tasks", StringComparer.OrdinalIgnoreCase))
            {
                tasks.Add(Task.Run(async () =>
                {
                    result.Tasks = await GetTasksAsync(new DashboardTaskQueryDto
                    {
                        Category = query.TaskCategory,
                        PageIndex = query.TaskPageIndex,
                        PageSize = query.TaskPageSize
                    });
                }));
            }

            if (includeAll || modules.Contains("messages", StringComparer.OrdinalIgnoreCase))
            {
                tasks.Add(Task.Run(async () =>
                {
                    result.Messages = await GetMessageSummaryAsync(query.MessageLimit);
                }));
            }

            if (includeAll || modules.Contains("achievements", StringComparer.OrdinalIgnoreCase))
            {
                tasks.Add(Task.Run(async () =>
                {
                    result.Achievements = await GetAchievementsAsync(query.AchievementLimit, query.Team);
                }));
            }

            if (includeAll || modules.Contains("deadlines", StringComparer.OrdinalIgnoreCase))
            {
                tasks.Add(Task.Run(async () =>
                {
                    result.Deadlines = await GetDeadlinesAsync(query.DeadlineDays);
                }));
            }

            await Task.WhenAll(tasks);

            return result;
        }

        /// <summary>
        /// Get statistics overview with month-over-month comparison
        /// </summary>
        public async Task<DashboardStatisticsDto> GetStatisticsAsync(string? team = null)
        {
            var now = DateTimeOffset.UtcNow;
            var thisMonthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var lastMonthEnd = thisMonthStart.AddTicks(-1);

            // Get current month statistics
            var activeCasesCount = await GetActiveCasesCountAsync(team);
            var completedThisMonth = await GetCompletedCasesCountAsync(thisMonthStart, now, team);
            var overdueTasksCount = await GetOverdueTasksCountAsync(team);
            var avgCompletionTime = await GetAverageCompletionTimeAsync(thisMonthStart, now, team);

            // Get last month statistics for comparison
            var lastMonthActiveCases = await GetActiveCasesCountAtDateAsync(lastMonthEnd, team);
            var lastMonthCompleted = await GetCompletedCasesCountAsync(lastMonthStart, lastMonthEnd, team);
            var lastMonthOverdueTasks = await GetOverdueTasksCountAtDateAsync(lastMonthEnd, team);
            var lastMonthAvgCompletionTime = await GetAverageCompletionTimeAsync(lastMonthStart, lastMonthEnd, team);

            return new DashboardStatisticsDto
            {
                ActiveCases = CreateStatisticItem(activeCasesCount, lastMonthActiveCases, true),
                CompletedThisMonth = CreateStatisticItem(completedThisMonth, lastMonthCompleted, true),
                OverdueTasks = CreateStatisticItem(overdueTasksCount, lastMonthOverdueTasks, false), // Decrease is positive
                AvgCompletionTime = CreateStatisticItem(avgCompletionTime, lastMonthAvgCompletionTime, false, "days") // Decrease is positive
            };
        }

        /// <summary>
        /// Get case distribution by stage (optimized to avoid N+1 queries)
        /// </summary>
        public async Task<StageDistributionResultDto> GetStageDistributionAsync(long? workflowId = null)
        {
            // Get all stages ordered by sequence
            var stages = await _stageRepository.GetListAsync(s => 
                s.IsActive && s.IsValid && 
                (workflowId == null || s.WorkflowId == workflowId));
            
            stages = stages.OrderBy(s => s.Order).ToList();

            if (!stages.Any())
            {
                return new StageDistributionResultDto
                {
                    Stages = new List<StageDistributionDto>(),
                    OverallProgress = 0
                };
            }

            // Get all active cases with their stage IDs in a single query
            // Note: "Started" and "InProgress" status represent active cases
            var activeCases = await _onboardingRepository.GetListAsync(o => 
                o.IsActive && o.IsValid && 
                (o.Status == "Started" || o.Status == "InProgress") &&
                (workflowId == null || o.WorkflowId == workflowId));

            // Group by stage ID and count
            var stageCounts = activeCases
                .GroupBy(o => o.CurrentStageId)
                .ToDictionary(g => g.Key, g => g.Count());

            var totalCases = activeCases.Count;

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

            // Calculate overall progress (average completion rate of active cases)
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
            var userId = long.TryParse(_userContext.UserId, out var uid) ? uid : 0;
            var userTeamIds = _userContext.UserTeams?.GetAllTeamIds() ?? new List<long>();

            // Build query for tasks assigned to current user or their teams
            var tasks = await _checklistTaskRepository.GetPendingTasksForUserAsync(
                userId, 
                userTeamIds, 
                query.Category,
                query.PageIndex,
                query.PageSize);

            var totalCount = await _checklistTaskRepository.GetPendingTasksCountForUserAsync(
                userId, 
                userTeamIds, 
                query.Category);

            var now = DateTimeOffset.UtcNow;
            var taskDtos = tasks.Select(t => new DashboardTaskDto
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
                IsRequired = t.IsRequired
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
        /// </summary>
        public async Task<List<AchievementDto>> GetAchievementsAsync(int limit = 5, string? team = null)
        {
            var achievements = new List<AchievementDto>();

            // Get recently completed cases (CaseCompleted type)
            var completedCases = await _onboardingRepository.GetRecentlyCompletedAsync(limit, team);
            var caseAchievements = completedCases.Select(c => new AchievementDto
            {
                Id = c.Id,
                Title = $"{c.LeadName} case completed",
                Description = c.ActualCompletionDate.HasValue && c.StartDate.HasValue
                    ? $"Successfully completed all case stages in {(int)(c.ActualCompletionDate.Value - c.StartDate.Value).TotalDays} days"
                    : "Successfully completed all case stages",
                CompletionDate = c.ActualCompletionDate ?? c.ModifyDate,
                CompletionDateDisplay = FormatCompletionDate(c.ActualCompletionDate ?? c.ModifyDate),
                Teams = ParseTeams(c.OperateTeams),
                Type = "CaseCompleted",
                CaseCode = c.CaseCode,
                CaseName = c.LeadName,
                DaysToComplete = c.ActualCompletionDate.HasValue && c.StartDate.HasValue
                    ? (int)(c.ActualCompletionDate.Value - c.StartDate.Value).TotalDays
                    : null
            }).ToList();

            achievements.AddRange(caseAchievements);

            // Get recently completed stages (StageCompleted type)
            var stageAchievements = await GetRecentlyCompletedStagesAsync(limit, team);
            achievements.AddRange(stageAchievements);

            // Sort by completion date descending and take the limit
            return achievements
                .OrderByDescending(a => a.CompletionDate)
                .Take(limit)
                .ToList();
        }

        /// <summary>
        /// Get recently completed stages from active cases
        /// </summary>
        private async Task<List<AchievementDto>> GetRecentlyCompletedStagesAsync(int limit, string? team)
        {
            var achievements = new List<AchievementDto>();

            try
            {
                // Get all cases (not just Started) to find completed stages
                // A case can have completed stages even if the case itself is still in progress
                var filterByTeam = !string.IsNullOrEmpty(team);
                var allCases = await _onboardingRepository.GetListAsync(o =>
                    o.IsValid &&
                    (!filterByTeam || o.CurrentTeam == team));

                _logger.LogInformation("GetRecentlyCompletedStagesAsync: Found {Count} total cases", allCases.Count);

                // Get all stages for enrichment
                var stageIds = new HashSet<long>();
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                };

                // First pass: parse JSON and collect stage IDs
                var casesWithProgress = new List<(Onboarding Onboarding, List<OnboardingStageProgress> Progress)>();
                foreach (var onboarding in allCases)
                {
                    if (string.IsNullOrEmpty(onboarding.StagesProgressJson))
                    {
                        _logger.LogDebug("Onboarding {Id} has empty StagesProgressJson", onboarding.Id);
                        continue;
                    }

                    try
                    {
                        var jsonString = onboarding.StagesProgressJson.Trim();

                        // Handle double-serialized JSON (e.g., "\"[{...}]\"" instead of "[{...}]")
                        if (jsonString.StartsWith("\"") && jsonString.EndsWith("\""))
                        {
                            jsonString = JsonSerializer.Deserialize<string>(jsonString, jsonOptions) ?? "[]";
                        }

                        var progress = JsonSerializer.Deserialize<List<OnboardingStageProgress>>(
                            jsonString, jsonOptions) ?? new List<OnboardingStageProgress>();
                        
                        _logger.LogDebug("Onboarding {Id}: Parsed {Count} stages", onboarding.Id, progress.Count);
                        
                        // Log each stage's completion status
                        foreach (var p in progress)
                        {
                            _logger.LogDebug("  Stage {StageId}: IsCompleted={IsCompleted}, CompletionTime={CompletionTime}, Status={Status}", 
                                p.StageId, p.IsCompleted, p.CompletionTime, p.Status);
                        }
                        
                        if (progress.Any())
                        {
                            casesWithProgress.Add((onboarding, progress));
                            var completedCount = progress.Count(p => p.IsCompleted);
                            _logger.LogDebug("Onboarding {Id}: {CompletedCount} completed stages", onboarding.Id, completedCount);
                            
                            foreach (var p in progress.Where(p => p.IsCompleted))
                            {
                                stageIds.Add(p.StageId);
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse StagesProgressJson for onboarding {OnboardingId}", onboarding.Id);
                    }
                }

                _logger.LogInformation("GetRecentlyCompletedStagesAsync: Found {Count} completed stage IDs", stageIds.Count);

                // Get stage names from Stage repository
                var stages = stageIds.Any() 
                    ? await _stageRepository.GetListAsync(s => stageIds.Contains(s.Id))
                    : new List<Stage>();
                var stageDict = stages.ToDictionary(s => s.Id, s => s);

                // Extract completed stages from each case
                foreach (var (onboarding, progress) in casesWithProgress)
                {
                    var completedStages = progress
                        .Where(s => s.IsCompleted && s.CompletionTime.HasValue)
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
                                CaseName = onboarding.LeadName,
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

                _logger.LogInformation("GetRecentlyCompletedStagesAsync: Returning {Count} achievements", achievements.Count);

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
        /// Get upcoming deadlines
        /// </summary>
        public async Task<List<DeadlineDto>> GetDeadlinesAsync(int days = 7)
        {
            var userId = long.TryParse(_userContext.UserId, out var uid) ? uid : 0;
            var userTeamIds = _userContext.UserTeams?.GetAllTeamIds() ?? new List<long>();
            var now = DateTimeOffset.UtcNow;
            var endDate = now.AddDays(days);

            // Get tasks with upcoming due dates
            var tasks = await _checklistTaskRepository.GetUpcomingDeadlinesAsync(userId, userTeamIds, endDate);

            var deadlines = tasks.Select(t => new DeadlineDto
            {
                Id = t.Id,
                Name = t.Name,
                DueDate = t.DueDate!.Value,
                DueDateDisplay = FormatDueDate(t.DueDate, now),
                Urgency = DetermineUrgency(t.DueDate!.Value, now),
                DaysUntilDue = (int)(t.DueDate!.Value - now).TotalDays,
                CaseCode = t.CaseCode ?? string.Empty,
                CaseName = t.CaseName ?? string.Empty,
                OnboardingId = t.OnboardingId,
                ChecklistId = t.ChecklistId,
                Type = "Task",
                Priority = t.Priority,
                AssignedTeam = t.AssignedTeam
            })
            .OrderBy(d => d.DueDate)
            .ToList();

            return deadlines;
        }

        #region Private Helper Methods

        private async Task<int> GetActiveCasesCountAsync(string? team)
        {
            // Use GetListAsync which has tenant filtering, then count in memory
            // Note: "Started" and "InProgress" status represent active cases
            var filterByTeam = !string.IsNullOrEmpty(team);
            var cases = await _onboardingRepository.GetListAsync(o => 
                o.IsActive && o.IsValid && 
                (o.Status == "Started" || o.Status == "InProgress") &&
                (!filterByTeam || o.CurrentTeam == team));
            return cases.Count;
        }

        private async Task<int> GetActiveCasesCountAtDateAsync(DateTimeOffset date, string? team)
        {
            // Approximate: count cases that were active at that date
            var filterByTeam = !string.IsNullOrEmpty(team);
            var cases = await _onboardingRepository.GetListAsync(o => 
                o.IsValid && 
                o.StartDate <= date &&
                (o.ActualCompletionDate == null || o.ActualCompletionDate > date) &&
                (!filterByTeam || o.CurrentTeam == team));
            return cases.Count;
        }

        private async Task<int> GetCompletedCasesCountAsync(DateTimeOffset startDate, DateTimeOffset endDate, string? team)
        {
            var filterByTeam = !string.IsNullOrEmpty(team);
            var cases = await _onboardingRepository.GetListAsync(o => 
                o.IsValid && 
                (o.Status == "Completed" || o.Status == "Force Completed") &&
                o.ActualCompletionDate >= startDate &&
                o.ActualCompletionDate <= endDate &&
                (!filterByTeam || o.CurrentTeam == team));
            return cases.Count;
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

        private async Task<decimal> GetAverageCompletionTimeAsync(DateTimeOffset startDate, DateTimeOffset endDate, string? team)
        {
            var filterByTeam = !string.IsNullOrEmpty(team);
            var completedCases = await _onboardingRepository.GetListAsync(o => 
                o.IsValid && 
                (o.Status == "Completed" || o.Status == "Force Completed") &&
                o.ActualCompletionDate >= startDate &&
                o.ActualCompletionDate <= endDate &&
                o.StartDate.HasValue &&
                (!filterByTeam || o.CurrentTeam == team));

            if (!completedCases.Any())
                return 0;

            var totalDays = completedCases
                .Where(c => c.StartDate.HasValue && c.ActualCompletionDate.HasValue)
                .Sum(c => (c.ActualCompletionDate!.Value - c.StartDate!.Value).TotalDays);

            return Math.Round((decimal)(totalDays / completedCases.Count), 0);
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

        private string FormatReceivedDate(DateTimeOffset receivedDate, DateTimeOffset now)
        {
            var days = (int)(now.Date - receivedDate.Date).TotalDays;

            return days switch
            {
                0 => receivedDate.ToString("h:mm tt"),
                1 => "Yesterday",
                _ => receivedDate.ToString("MMM d, yyyy")
            };
        }

        private string FormatCompletionDate(DateTimeOffset date)
        {
            return date.ToString("MMM d, yyyy");
        }

        private string GetInitials(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "??";

            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            if (parts.Length == 1 && parts[0].Length >= 2)
                return parts[0].Substring(0, 2).ToUpper();
            return name.Substring(0, Math.Min(2, name.Length)).ToUpper();
        }

        private List<string> ParseLabels(string? labelsJson)
        {
            if (string.IsNullOrWhiteSpace(labelsJson))
                return new List<string>();

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(labelsJson) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
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

        #endregion
    }
}
