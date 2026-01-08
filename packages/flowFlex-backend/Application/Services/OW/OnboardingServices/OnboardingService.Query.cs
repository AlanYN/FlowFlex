using AutoMapper;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Attr;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Events;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Utils;
using FlowFlex.Infrastructure.Extensions;
using FlowFlex.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using SqlSugar;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
// using Item.Redis; // Temporarily disable Redis
using System.Text.Json;
using PermissionOperationType = FlowFlex.Domain.Shared.Enums.Permission.OperationTypeEnum;
using FlowFlex.Application.Contracts.Dtos.OW.User;
using Microsoft.Extensions.Logging;


namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Onboarding service - Query and export operations
    /// </summary>
    public partial class OnboardingService
    {
        public async Task<PageModelDto<OnboardingOutputDto>> QueryAsync(OnboardingQueryRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var tenantId = _userContext?.TenantId ?? "default";
            var appCode = _userContext?.AppCode ?? "default";
            try
            {
                _logger.LogDebug("QueryAsync started - TenantId: {TenantId}, AppCode: {AppCode}", tenantId, appCode);
                
                // Build query conditions list - using safe BaseRepository approach
                var whereExpressions = new List<Expression<Func<Onboarding, bool>>>();

                // Basic filter conditions
                whereExpressions.Add(x => x.IsValid == true);
                // Apply tenant isolation
                if (!string.IsNullOrEmpty(tenantId))
                {
                    whereExpressions.Add(x => x.TenantId.ToLower() == tenantId.ToLower());
                }
                if (!string.IsNullOrEmpty(appCode))
                {
                    whereExpressions.Add(x => x.AppCode.ToLower() == appCode.ToLower());
                }
                // Apply filter conditions
                if (request.WorkflowId.HasValue && request.WorkflowId.Value > 0)
                {
                    whereExpressions.Add(x => x.WorkflowId == request.WorkflowId.Value);
                }

                if (request.CurrentStageId.HasValue && request.CurrentStageId.Value > 0)
                {
                    whereExpressions.Add(x => x.CurrentStageId == request.CurrentStageId.Value);
                }

                // Support comma-separated Lead IDs with fuzzy matching
                if (!string.IsNullOrEmpty(request.LeadId) && request.LeadId != "string")
                {
                    var leadIds = request.GetLeadIdsList();
                    if (leadIds.Any())
                    {
                        // Use OR condition to match any of the lead IDs with fuzzy matching (case-insensitive)
                        whereExpressions.Add(x => leadIds.Any(id => x.LeadId.ToLower().Contains(id.ToLower())));
                    }
                }

                // Support batch LeadIds query for performance optimization
                if (request.LeadIds?.Any() == true)
                {
                    whereExpressions.Add(x => request.LeadIds.Contains(x.LeadId));
                }

                // Support batch OnboardingIds query for exporting selected records
                if (request.OnboardingIds?.Any() == true)
                {
                    whereExpressions.Add(x => request.OnboardingIds.Contains(x.Id));
                }

                // Support comma-separated Case Names
                if (!string.IsNullOrEmpty(request.CaseName) && request.CaseName != "string")
                {
                    var caseNames = request.GetCaseNamesList();
                    if (caseNames.Any())
                    {
                        // Use OR condition to match any of the case names (case-insensitive)
                        whereExpressions.Add(x => caseNames.Any(name => x.CaseName.ToLower().Contains(name.ToLower())));
                    }
                }

                // Support comma-separated Case Codes with fuzzy matching
                if (!string.IsNullOrEmpty(request.CaseCode) && request.CaseCode != "string")
                {
                    var caseCodes = request.GetCaseCodesList();
                    if (caseCodes.Any())
                    {
                        // Use OR condition to match any of the case codes (case-insensitive)
                        whereExpressions.Add(x => x.CaseCode != null && caseCodes.Any(code => x.CaseCode.ToLower().Contains(code.ToLower())));
                    }
                }

                if (request.LifeCycleStageId.HasValue && request.LifeCycleStageId.Value > 0)
                {
                    whereExpressions.Add(x => x.LifeCycleStageId == request.LifeCycleStageId.Value);
                }

                if (!string.IsNullOrEmpty(request.LifeCycleStageName) && request.LifeCycleStageName != "string")
                {
                    whereExpressions.Add(x => x.LifeCycleStageName.ToLower().Contains(request.LifeCycleStageName.ToLower()));
                }

                if (!string.IsNullOrEmpty(request.Priority) && request.Priority != "string")
                {
                    whereExpressions.Add(x => x.Priority == request.Priority);
                }

                if (!string.IsNullOrEmpty(request.Status) && request.Status != "string")
                {
                    whereExpressions.Add(x => x.Status == request.Status);
                }

                if (request.IsActive.HasValue)
                {
                    whereExpressions.Add(x => x.IsActive == request.IsActive.Value);
                }

                // Support comma-separated Updated By users
                if (!string.IsNullOrEmpty(request.UpdatedBy) && request.UpdatedBy != "string")
                {
                    var updatedByUsers = request.GetUpdatedByList();
                    if (updatedByUsers.Any())
                    {
                        // Match by StageUpdatedBy first; fallback to ModifyBy (case-insensitive)
                        whereExpressions.Add(x => updatedByUsers.Any(user =>
                            (!string.IsNullOrEmpty(x.StageUpdatedBy) && x.StageUpdatedBy.ToLower().Contains(user.ToLower()))
                            || x.ModifyBy.ToLower().Contains(user.ToLower())));
                    }
                }

                if (request.UpdatedByUserId.HasValue && request.UpdatedByUserId.Value > 0)
                {
                    whereExpressions.Add(x => x.ModifyUserId == request.UpdatedByUserId.Value);
                }

                if (!string.IsNullOrEmpty(request.CreatedBy) && request.CreatedBy != "string")
                {
                    whereExpressions.Add(x => x.CreateBy.ToLower().Contains(request.CreatedBy.ToLower()));
                }

                if (request.CreatedByUserId.HasValue && request.CreatedByUserId.Value > 0)
                {
                    whereExpressions.Add(x => x.CreateUserId == request.CreatedByUserId.Value);
                }

                // Filter by Ownership
                if (request.Ownership.HasValue && request.Ownership.Value > 0)
                {
                    whereExpressions.Add(x => x.Ownership == request.Ownership.Value);
                }

                if (!string.IsNullOrEmpty(request.OwnershipName) && request.OwnershipName != "string")
                {
                    whereExpressions.Add(x => x.OwnershipName.ToLower().Contains(request.OwnershipName.ToLower()));
                }

                // Determine sort field and direction
                Expression<Func<Onboarding, object>> orderByExpression = GetOrderByExpression(request);
                bool isAsc = GetSortDirection(request);

                // Pagination parameters
                int pageIndex = Math.Max(1, request.PageIndex > 0 ? request.PageIndex : 1);
                int pageSize = Math.Max(1, Math.Min(100, request.PageSize > 0 ? request.PageSize : 10));

                // Build base queryable
                var queryable = _onboardingRepository.GetSqlSugarClient().Queryable<Onboarding>();

                // Apply all where conditions
                foreach (var whereExpression in whereExpressions)
                {
                    queryable = queryable.Where(whereExpression);
                }

                // PERFORMANCE OPTIMIZATION: Apply permission filter at SQL level for regular users
                // This reduces memory usage by filtering at database level instead of loading all data
                var permissionContext = BuildPermissionFilterContext();
                if (permissionContext.NeedsPermissionFilter)
                {
                    queryable = ApplyPermissionFilterToQuery(queryable, permissionContext);
                }

                // Apply sorting
                queryable = isAsc 
                    ? queryable.OrderBy(orderByExpression) 
                    : queryable.OrderByDescending(orderByExpression);

                // Execute query with pagination at SQL level
                List<Onboarding> pagedEntities;
                int totalCount;

                if (request.AllData)
                {
                    pagedEntities = await queryable.ToListAsync();
                    totalCount = pagedEntities.Count;
                }
                else
                {
                    // Use SQL-level pagination for better performance
                    var pageResult = await queryable.ToPageListAsync(pageIndex, pageSize);
                    pagedEntities = pageResult;
                    totalCount = await queryable.CountAsync();
                }

                // Batch get Workflow and Stage information to avoid N+1 queries (only for paged data)
                var (workflows, stages) = await GetRelatedDataBatchOptimizedAsync(pagedEntities);

                // Create lookup dictionaries to improve search performance
                var workflowDict = workflows.ToDictionary(w => w.Id, w => w.Name);
                var stageDict = stages.ToDictionary(s => s.Id, s => s.Name);

                // Batch process JSON deserialization (only for paged data)
                ProcessStagesProgressParallel(pagedEntities);

                // Map to output DTOs
                var results = _mapper.Map<List<OnboardingOutputDto>>(pagedEntities);

                _logger.LogDebug("Query mapped {ResultCount} results from {EntityCount} entities", 
                    results.Count, pagedEntities.Count);

                // Populate workflow/stage names and calculate current stage end time
                await PopulateOnboardingOutputDtoAsync(results, pagedEntities);

                // Create page model with appropriate pagination info
                var pageModel = request.AllData
                    ? new PageModelDto<OnboardingOutputDto>(1, totalCount, results, totalCount)
                    : new PageModelDto<OnboardingOutputDto>(pageIndex, pageSize, results, totalCount);

                // Record performance statistics
                stopwatch.Stop();
                _logger.LogDebug("QueryAsync completed in {ElapsedMs}ms - TotalCount: {TotalCount}, PageSize: {PageSize}", 
                    stopwatch.ElapsedMilliseconds, totalCount, pageSize);
                return pageModel;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "QueryAsync failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw new CRMException(ErrorCodeEnum.SystemError,
                    $"Error querying onboardings: {ex.Message}");
            }
        }

        #region Permission Filter Context

        /// <summary>
        /// Context for building permission filter SQL conditions
        /// </summary>
        private class PermissionFilterContext
        {
            public bool NeedsPermissionFilter { get; set; }
            public bool SkipAllFilters { get; set; }
            public long UserId { get; set; }
            public string UserIdString { get; set; } = string.Empty;
            public List<long> UserTeamIds { get; set; } = new();
            public List<long> AccessibleWorkflowIds { get; set; } = new();
        }

        /// <summary>
        /// Build permission filter context based on current user
        /// </summary>
        private PermissionFilterContext BuildPermissionFilterContext()
        {
            var context = new PermissionFilterContext();

            // Fast path: Client Credentials token - skip all permission checks
            if (_userContext?.Schema == Domain.Shared.Const.AuthSchemes.ItemIamClientIdentification)
            {
                _logger.LogDebug("Client Credentials token detected, skipping permission filters");
                context.SkipAllFilters = true;
                return context;
            }

            var userId = _userContext?.UserId;
            if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var userIdLong))
            {
                // No valid user context - will return empty results
                context.NeedsPermissionFilter = true;
                return context;
            }

            context.UserId = userIdLong;
            context.UserIdString = userIdLong.ToString();

            // Fast path: System Admin - skip permission checks
            if (_userContext?.IsSystemAdmin == true)
            {
                _logger.LogDebug("User {UserId} is System Admin, skipping permission filters", userIdLong);
                context.SkipAllFilters = true;
                return context;
            }

            // Fast path: Tenant Admin - skip permission checks
            if (_userContext != null && _userContext.HasAdminPrivileges(_userContext.TenantId))
            {
                _logger.LogDebug("User {UserId} is Tenant Admin, skipping permission filters", userIdLong);
                context.SkipAllFilters = true;
                return context;
            }

            // Regular user - needs permission filtering
            context.NeedsPermissionFilter = true;

            // Get user teams once
            var userTeams = _permissionService.GetUserTeamIds();
            context.UserTeamIds = userTeams?
                .Select(t => long.TryParse(t, out var tid) ? tid : 0)
                .Where(t => t > 0)
                .ToList() ?? new List<long>();

            _logger.LogDebug("User {UserId} has {TeamCount} teams for permission filtering", 
                userIdLong, context.UserTeamIds.Count);

            return context;
        }

        /// <summary>
        /// Apply permission filter to queryable at SQL level
        /// This pushes permission filtering to the database for better performance
        /// </summary>
        private ISugarQueryable<Onboarding> ApplyPermissionFilterToQuery(
            ISugarQueryable<Onboarding> queryable, 
            PermissionFilterContext context)
        {
            if (context.SkipAllFilters)
            {
                return queryable;
            }

            if (!context.NeedsPermissionFilter)
            {
                return queryable;
            }

            // No valid user - return empty result
            if (context.UserId == 0)
            {
                return queryable.Where(x => false);
            }

            var userId = context.UserId;
            var userTeamIds = context.UserTeamIds;
            var userIdString = context.UserIdString;

            // Build permission filter using raw SQL for PostgreSQL jsonb compatibility
            // A user can view a case if:
            // 1. User is the owner (Ownership == userId)
            // 2. Case is in Public mode (ViewPermissionMode == Public)
            // 3. Case is in Team mode and user's team is in ViewTeams (using PostgreSQL jsonb::text LIKE)
            // 4. Case is in User mode and user is in ViewUsers (using PostgreSQL jsonb::text LIKE)

            // Build SQL condition for team check using jsonb::text LIKE
            var teamConditions = userTeamIds.Select(t => $"view_teams::text LIKE '%\"{t}\"%'").ToList();
            var teamSqlCondition = teamConditions.Count > 0 
                ? $"({string.Join(" OR ", teamConditions)})" 
                : "false";

            // Build SQL condition for user check
            var userSqlCondition = $"view_users::text LIKE '%\"{userIdString}\"%'";

            // Use raw SQL for the entire permission filter to avoid jsonb ILIKE issue
            var permissionSql = $@"(
                ownership = {userId} OR 
                view_permission_mode = {(int)ViewPermissionModeEnum.Public} OR
                (view_permission_mode = {(int)ViewPermissionModeEnum.VisibleToTeams} AND 
                 view_permission_subject_type = {(int)PermissionSubjectTypeEnum.Team} AND 
                 view_teams IS NOT NULL AND 
                 {teamSqlCondition}) OR
                (view_permission_mode = {(int)ViewPermissionModeEnum.VisibleToTeams} AND 
                 view_permission_subject_type = {(int)PermissionSubjectTypeEnum.User} AND 
                 view_users IS NOT NULL AND 
                 {userSqlCondition})
            )";

            queryable = queryable.Where(permissionSql);

            _logger.LogDebug("Applied SQL-level permission filter for user {UserId} with {TeamCount} teams", 
                userId, userTeamIds.Count);

            return queryable;
        }

        #endregion

        /// <summary>
        /// Get sort expression
        /// </summary>
        private Expression<Func<Onboarding, object>> GetOrderByExpression(OnboardingQueryRequest request)
        {
            var sortBy = request.SortField?.ToLower() ?? "createdate";

            return sortBy switch
            {
                "id" => x => x.Id,
                "leadid" => x => x.LeadId,
                "leadname" => x => x.CaseName,
                "casename" => x => x.CaseName,
                "casecode" => x => x.CaseCode ?? "",
                "contactperson" => x => x.ContactPerson ?? "",
                "contactemail" => x => x.ContactEmail ?? "",
                "leademail" => x => x.LeadEmail ?? "",
                "leadphone" => x => x.LeadPhone ?? "",
                "workflowid" => x => x.WorkflowId,
                "currentstageid" => x => x.CurrentStageId,
                "lifecyclestageid" => x => x.LifeCycleStageId,
                "lifecyclestagename" => x => x.LifeCycleStageName ?? "",
                "priority" => x => x.Priority ?? "",
                "status" => x => x.Status ?? "",
                "isactive" => x => x.IsActive,
                "completionrate" => x => x.CompletionRate,
                "ownership" => x => x.Ownership,
                "ownershipname" => x => x.OwnershipName ?? "",
                "createdate" => x => x.CreateDate,
                "modifydate" => x => x.ModifyDate,
                "createby" => x => x.CreateBy ?? "",
                "modifyby" => x => x.ModifyBy ?? "",
                _ => x => x.CreateDate
            };
        }

        /// <summary>
        /// Get sort direction
        /// </summary>
        private bool GetSortDirection(OnboardingQueryRequest request)
        {
            var sortDirection = request.SortDirection?.ToLower() ?? "desc";
            return sortDirection == "asc";
        }

        /// <summary>
        /// Batch get related Workflow and Stage data (avoid N+1 queries) - Sequential execution to avoid concurrency issues
        /// </summary>
        private async Task<(List<Workflow> workflows, List<Stage> stages)> GetRelatedDataBatchOptimizedAsync(List<Onboarding> entities)
        {
            var workflowIds = entities.Select(x => x.WorkflowId).Distinct().ToList();
            var stageIds = entities.Where(x => x.CurrentStageId.HasValue)
                    .Select(x => x.CurrentStageId.Value).Distinct().ToList();

            // Sequential execution to avoid SQL concurrency conflicts
            var workflows = await GetWorkflowsBatchAsync(workflowIds);
            var stages = await GetStagesBatchAsync(stageIds);

            return (workflows, stages);
        }

        /// <summary>
        /// Parallel processing of stage progress
        /// </summary>
        private void ProcessStagesProgressParallel(List<Onboarding> entities)
        {
            try
            {
                if (entities.Count <= 10)
                {
                    // Direct processing for small datasets
                    foreach (var entity in entities)
                    {
                        LoadStagesProgressFromJson(entity);
                    }
                }
                else
                {
                    // Parallel processing for large datasets, but using safer approach
                    // Create copy of entities list to avoid collection modification exceptions
                    var entitiesCopy = entities.ToList();
                    Parallel.ForEach(entitiesCopy, new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 4) // Limit parallelism degree
                    }, entity =>
                    {
                        try
                        {
                            LoadStagesProgressFromJson(entity);
                        }
                        catch (Exception ex)
                        {
                            // Debug logging handled by structured logging
                            // Ensure that even if single entity processing fails, it doesn't affect other entities
                            entity.StagesProgress = new List<OnboardingStageProgress>();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                // If parallel processing fails, fallback to sequential processing
                foreach (var entity in entities)
                {
                    try
                    {
                        LoadStagesProgressFromJson(entity);
                    }
                    catch (Exception entityEx)
                    {
                        // Debug logging handled by structured logging
                        entity.StagesProgress = new List<OnboardingStageProgress>();
                    }
                }
            }
        }

        /// <summary>
        /// Batch retrieve Workflows information, avoid N+1 queries, support caching
        /// </summary>
        private async Task<List<Workflow>> GetWorkflowsBatchAsync(List<long> workflowIds)
        {
            if (!workflowIds.Any())
                return new List<Workflow>();

            try
            {
                var workflows = new List<Workflow>();
                var uncachedIds = new List<long>();

                // Safely get tenant ID
                var tenantId = !string.IsNullOrEmpty(_userContext?.TenantId)
                    ? _userContext.TenantId.ToLowerInvariant()
                    : "default";

                // First get from cache
                foreach (var id in workflowIds)
                {
                    try
                    {
                        var cacheKey = $"{WORKFLOW_CACHE_PREFIX}:{tenantId}:{id}";
                        // Redis cache temporarily disabled
                        string cachedWorkflow = null;

                        if (!string.IsNullOrEmpty(cachedWorkflow))
                        {
                            var workflow = JsonSerializer.Deserialize<Workflow>(cachedWorkflow);
                            if (workflow != null)
                            {
                                workflows.Add(workflow);
                            }
                        }
                        else
                        {
                            uncachedIds.Add(id);
                        }
                    }
                    catch (Exception cacheEx)
                    {
                        // Debug logging handled by structured logging
                        uncachedIds.Add(id);
                    }
                }

                // Get uncached data from database - use direct query to avoid repository conflicts
                if (uncachedIds.Any())
                {
                    List<Workflow> dbWorkflows;
                    try
                    {
                        // Use direct repository method to avoid connection conflicts
                        dbWorkflows = await _workflowRepository.GetListAsync(w => uncachedIds.Contains(w.Id) && w.IsValid);
                        workflows.AddRange(dbWorkflows);
                    }
                    catch (Exception dbEx)
                    {
                        // Debug logging handled by structured logging
                        // Fallback to repository method if direct query fails
                        dbWorkflows = await _workflowRepository.GetListAsync(w => uncachedIds.Contains(w.Id) && w.IsValid);
                        workflows.AddRange(dbWorkflows);
                    }
                    // Cache newly retrieved data
                    var cacheExpiry = TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES);
                    var cacheTasks = dbWorkflows.Select(async workflow =>
                    {
                        try
                        {
                            var cacheKey = $"{WORKFLOW_CACHE_PREFIX}:{tenantId}:{workflow.Id}";
                            var serializedWorkflow = JsonSerializer.Serialize(workflow);
                            // Redis cache temporarily disabled
                            await Task.CompletedTask;
                        }
                        catch (Exception cacheEx)
                        {
                            // Debug logging handled by structured logging
                        }
                    });

                    await Task.WhenAll(cacheTasks);
                }

                return workflows;
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                // If cache fails, get directly from database
                return await _workflowRepository.GetListAsync(w => workflowIds.Contains(w.Id) && w.IsValid);
            }
        }

        /// <summary>
        /// Batch get Stages information, avoid N+1 queries, support caching
        /// </summary>
        private async Task<List<Stage>> GetStagesBatchAsync(List<long> stageIds)
        {
            if (!stageIds.Any())
                return new List<Stage>();

            try
            {
                var stages = new List<Stage>();
                var uncachedIds = new List<long>();

                // Safely get tenant ID
                var tenantId = !string.IsNullOrEmpty(_userContext?.TenantId)
                    ? _userContext.TenantId.ToLowerInvariant()
                    : "default";

                // First get from cache
                foreach (var id in stageIds)
                {
                    try
                    {
                        var cacheKey = $"{STAGE_CACHE_PREFIX}:{tenantId}:{id}";
                        // Redis cache temporarily disabled
                        string cachedStage = null;

                        if (!string.IsNullOrEmpty(cachedStage))
                        {
                            var stage = JsonSerializer.Deserialize<Stage>(cachedStage);
                            if (stage != null)
                            {
                                stages.Add(stage);
                            }
                        }
                        else
                        {
                            uncachedIds.Add(id);
                        }
                    }
                    catch (Exception cacheEx)
                    {
                        // Debug logging handled by structured logging
                        uncachedIds.Add(id);
                    }
                }

                // Get uncached data from database - use direct query to avoid repository conflicts
                if (uncachedIds.Any())
                {
                    List<Stage> dbStages;
                    try
                    {
                        // Use direct repository method to avoid connection conflicts
                        dbStages = await _stageRepository.GetListAsync(s => uncachedIds.Contains(s.Id) && s.IsValid);
                        stages.AddRange(dbStages);
                    }
                    catch (Exception dbEx)
                    {
                        // Debug logging handled by structured logging
                        // Fallback to repository method if direct query fails
                        dbStages = await _stageRepository.GetListAsync(s => uncachedIds.Contains(s.Id) && s.IsValid);
                        stages.AddRange(dbStages);
                    }
                    // Cache newly retrieved data
                    var cacheExpiry = TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES);
                    var cacheTasks = dbStages.Select(async stage =>
                    {
                        try
                        {
                            var cacheKey = $"{STAGE_CACHE_PREFIX}:{tenantId}:{stage.Id}";
                            var serializedStage = JsonSerializer.Serialize(stage);
                            // Redis cache temporarily disabled
                            await Task.CompletedTask;
                        }
                        catch (Exception cacheEx)
                        {
                            // Debug logging handled by structured logging
                        }
                    });

                    await Task.WhenAll(cacheTasks);
                }

                return stages;
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                // If cache fails, get directly from database
                return await _stageRepository.GetListAsync(s => stageIds.Contains(s.Id) && s.IsValid);
            }
        }

        /// <summary>
        /// Move to next stage
        /// </summary>

        #region Permission Check Helper Methods

        /// <summary>
        /// Check Case view permission in memory (no DB queries)
        /// Mirrors the logic from CasePermissionService.CheckCasePermissionAsync but without async DB calls
        /// </summary>
        private bool CheckCaseViewPermissionInMemory(
            Onboarding entity,
            Domain.Entities.OW.Workflow workflow,
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
                // In Public mode, if user can view Workflow, they can view Case
                // Note: CheckWorkflowViewPermissionInMemory is defined in OnboardingService.StatusOperations.cs
                return CheckWorkflowViewPermissionInMemory(workflow, userId, userTeamIds);
            }

            // Step 3: Check Case-specific view permissions (non-Public modes)
            return CheckCaseViewPermissionBySubjectType(entity, userTeamIds, userIdString);
        }

        /// <summary>
        /// Check Case view permission based on SubjectType (Team or User)
        /// </summary>
        private bool CheckCaseViewPermissionBySubjectType(
            Onboarding entity,
            List<long> userTeamIds,
            string userIdString)
        {
            // If SubjectType is Team, check ViewTeams
            if (entity.ViewPermissionSubjectType == PermissionSubjectTypeEnum.Team)
            {
                if (string.IsNullOrWhiteSpace(entity.ViewTeams))
                {
                    return false; // No teams specified = no access
                }

                // Note: ParseJsonArraySafe is defined in OnboardingService.StatusOperations.cs
                var viewTeams = ParseJsonArraySafe(entity.ViewTeams);
                if (viewTeams.Count == 0)
                {
                    return false; // Empty whitelist = no access
                }

                var viewTeamLongs = viewTeams
                    .Select(t => long.TryParse(t, out var tid) ? tid : 0)
                    .Where(t => t > 0)
                    .ToHashSet();

                return userTeamIds.Any(ut => viewTeamLongs.Contains(ut));
            }

            // If SubjectType is User, check ViewUsers
            if (entity.ViewPermissionSubjectType == PermissionSubjectTypeEnum.User)
            {
                if (string.IsNullOrWhiteSpace(entity.ViewUsers))
                {
                    return false; // No users specified = no access
                }

                var viewUsers = ParseJsonArraySafe(entity.ViewUsers);
                if (viewUsers.Count == 0)
                {
                    return false; // Empty whitelist = no access
                }

                return viewUsers.Contains(userIdString, StringComparer.OrdinalIgnoreCase);
            }

            // Unknown SubjectType = deny access
            return false;
        }

        #endregion

        #region GetActiveBySystemId

        /// <summary>
        /// Get all active onboardings by System ID
        /// Returns all onboarding records where SystemId matches and IsActive is true
        /// </summary>
        /// <param name="systemId">External system identifier</param>
        /// <param name="entityId">External entity ID for filtering (optional)</param>
        /// <param name="sortField">Sort field: createDate, modifyDate, leadName, caseCode, status (default: createDate)</param>
        /// <param name="sortOrder">Sort order: asc, desc (default: desc)</param>
        /// <param name="limit">Maximum number of records to return (default: 100, max: 1000)</param>
        /// <returns>List of active onboarding records</returns>
        public async Task<List<OnboardingOutputDto>> GetActiveBySystemIdAsync(string systemId, string? entityId = null, string sortField = "createDate", string sortOrder = "desc", int limit = 100)
        {
            if (string.IsNullOrWhiteSpace(systemId))
            {
                return new List<OnboardingOutputDto>();
            }

            var tenantId = _userContext?.TenantId ?? "default";
            var appCode = _userContext?.AppCode ?? "default";

            try
            {
                // Build query with filters
                var queryable = _onboardingRepository.GetSqlSugarClient().Queryable<Onboarding>()
                    .Where(x => x.IsValid == true)
                    .Where(x => x.IsActive == true)
                    .Where(x => x.SystemId == systemId);

                // Apply entityId filter if provided
                if (!string.IsNullOrWhiteSpace(entityId))
                {
                    queryable = queryable.Where(x => x.EntityId == entityId);
                }

                // Apply tenant isolation
                if (!string.IsNullOrEmpty(tenantId))
                {
                    queryable = queryable.Where(x => x.TenantId.ToLower() == tenantId.ToLower());
                }
                if (!string.IsNullOrEmpty(appCode))
                {
                    queryable = queryable.Where(x => x.AppCode.ToLower() == appCode.ToLower());
                }

                // Apply sorting based on sortField and sortOrder
                var isAscending = sortOrder?.ToLower() == "asc";
                queryable = (sortField?.ToLower()) switch
                {
                    "modifydate" => isAscending ? queryable.OrderBy(x => x.ModifyDate) : queryable.OrderByDescending(x => x.ModifyDate),
                    "leadname" => isAscending ? queryable.OrderBy(x => x.CaseName) : queryable.OrderByDescending(x => x.CaseName),
                    "casename" => isAscending ? queryable.OrderBy(x => x.CaseName) : queryable.OrderByDescending(x => x.CaseName),
                    "casecode" => isAscending ? queryable.OrderBy(x => x.CaseCode) : queryable.OrderByDescending(x => x.CaseCode),
                    "status" => isAscending ? queryable.OrderBy(x => x.Status) : queryable.OrderByDescending(x => x.Status),
                    _ => isAscending ? queryable.OrderBy(x => x.CreateDate) : queryable.OrderByDescending(x => x.CreateDate) // default: createDate
                };

                // Apply limit
                if (limit > 0)
                {
                    queryable = queryable.Take(limit);
                }

                // Execute query
                var entities = await queryable.ToListAsync();

                if (!entities.Any())
                {
                    return new List<OnboardingOutputDto>();
                }

                // Batch load workflows and stages to avoid N+1 queries
                var workflowIds = entities.Select(e => e.WorkflowId).Distinct().ToList();
                var stageIds = entities.Where(e => e.CurrentStageId.HasValue).Select(e => e.CurrentStageId!.Value).Distinct().ToList();

                // Use ClearFilter to bypass tenant/appCode filtering for workflow queries
                var workflows = await _workflowRepository.ClearFilter()
                    .Where(w => workflowIds.Contains(w.Id) && w.IsValid)
                    .ToListAsync();
                var workflowDict = workflows.ToDictionary(w => w.Id, w => w.Name);

                var stageDict = new Dictionary<long, string>();
                if (stageIds.Any())
                {
                    // Use ClearFilter to bypass tenant/appCode filtering for stage queries
                    var stages = await _stageRepository.ClearFilter()
                        .Where(s => stageIds.Contains(s.Id) && s.IsValid)
                        .ToListAsync();
                    stageDict = stages.ToDictionary(s => s.Id, s => s.Name);
                }

                // Map to DTOs
                var results = new List<OnboardingOutputDto>();
                foreach (var entity in entities)
                {
                    var dto = _mapper.Map<OnboardingOutputDto>(entity);

                    // Set workflow name from cache
                    if (workflowDict.TryGetValue(entity.WorkflowId, out var workflowName))
                    {
                        dto.WorkflowName = workflowName;
                    }

                    // Set current stage name from cache
                    if (entity.CurrentStageId.HasValue && stageDict.TryGetValue(entity.CurrentStageId.Value, out var stageName))
                    {
                        dto.CurrentStageName = stageName;
                    }

                    results.Add(dto);
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active onboardings by SystemId: {SystemId}, SortField: {SortField}, SortOrder: {SortOrder}, Limit: {Limit}", 
                    systemId, sortField, sortOrder, limit);
                throw;
            }
        }

        #endregion
    }
}

