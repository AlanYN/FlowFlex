using AutoMapper;
using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.Dtos.OW.Permission;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Contracts.IServices.OW.Onboarding;
using FlowFlex.Application.Helpers.OW;
using FlowFlex.Application.Services.OW.Permission;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.OW;
using FlowFlex.Domain.Shared.Helpers;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using SqlSugar;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text.Json;

namespace FlowFlex.Application.Services.OW.OnboardingServices
{
    /// <summary>
    /// Service for onboarding query and export operations
    /// Extracted from OnboardingService to reduce complexity
    /// </summary>
    public class OnboardingQueryService : IOnboardingQueryService
    {
        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IWorkflowRepository _workflowRepository;
        private readonly IStageRepository _stageRepository;
        private readonly IMapper _mapper;
        private readonly UserContext _userContext;
        private readonly IOnboardingPermissionService _permissionService;
        private readonly CasePermissionService _casePermissionService;
        private readonly IActionManagementService _actionManagementService;
        private readonly IOnboardingStageProgressService _stageProgressService;
        private readonly IOnboardingHelperService _helperService;
        private readonly ILogger<OnboardingQueryService> _logger;

        // Use shared JSON serializer options for consistency
        private static readonly JsonSerializerOptions JsonOptions = OnboardingSharedUtilities.JsonOptions;

        public OnboardingQueryService(
            IOnboardingRepository onboardingRepository,
            IWorkflowRepository workflowRepository,
            IStageRepository stageRepository,
            IMapper mapper,
            UserContext userContext,
            IOnboardingPermissionService permissionService,
            CasePermissionService casePermissionService,
            IActionManagementService actionManagementService,
            IOnboardingStageProgressService stageProgressService,
            IOnboardingHelperService helperService,
            ILogger<OnboardingQueryService> logger)
        {
            _onboardingRepository = onboardingRepository ?? throw new ArgumentNullException(nameof(onboardingRepository));
            _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
            _stageRepository = stageRepository ?? throw new ArgumentNullException(nameof(stageRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _casePermissionService = casePermissionService ?? throw new ArgumentNullException(nameof(casePermissionService));
            _actionManagementService = actionManagementService ?? throw new ArgumentNullException(nameof(actionManagementService));
            _stageProgressService = stageProgressService ?? throw new ArgumentNullException(nameof(stageProgressService));
            _helperService = helperService ?? throw new ArgumentNullException(nameof(helperService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        #region QueryAsync

        public async Task<PageModelDto<OnboardingOutputDto>> QueryAsync(OnboardingQueryRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
            var appCode = TenantContextHelper.GetAppCodeOrDefault(_userContext);
            try
            {
                _logger.LogDebug("QueryAsync started - TenantId: {TenantId}, AppCode: {AppCode}", tenantId, appCode);
                
                // Build query conditions list
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
                ApplyFilterConditions(request, whereExpressions);

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

                // Apply permission filter at SQL level for regular users
                var permissionContext = BuildPermissionFilterContext();
                if (permissionContext.NeedsPermissionFilter)
                {
                    queryable = ApplyPermissionFilterToQuery(queryable, permissionContext);
                }

                // Apply sorting
                queryable = isAsc 
                    ? queryable.OrderBy(orderByExpression) 
                    : queryable.OrderByDescending(orderByExpression);

                // Execute query with pagination
                List<Onboarding> pagedEntities;
                int totalCount;

                if (request.AllData)
                {
                    pagedEntities = await queryable.ToListAsync();
                    totalCount = pagedEntities.Count;
                }
                else
                {
                    var pageResult = await queryable.ToPageListAsync(pageIndex, pageSize);
                    pagedEntities = pageResult;
                    totalCount = await queryable.CountAsync();
                }

                // Batch process JSON deserialization
                ProcessStagesProgressParallel(pagedEntities);

                // Map to output DTOs
                var results = _mapper.Map<List<OnboardingOutputDto>>(pagedEntities);

                _logger.LogDebug("Query mapped {ResultCount} results from {EntityCount} entities", 
                    results.Count, pagedEntities.Count);

                // Populate workflow/stage names
                await PopulateOnboardingOutputDtoAsync(results, pagedEntities);

                // Create page model
                var pageModel = request.AllData
                    ? new PageModelDto<OnboardingOutputDto>(1, totalCount, results, totalCount)
                    : new PageModelDto<OnboardingOutputDto>(pageIndex, pageSize, results, totalCount);

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

        private void ApplyFilterConditions(OnboardingQueryRequest request, List<Expression<Func<Onboarding, bool>>> whereExpressions)
        {
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
                    whereExpressions.Add(x => leadIds.Any(id => x.LeadId.ToLower().Contains(id.ToLower())));
                }
            }

            // Support batch LeadIds query
            if (request.LeadIds?.Any() == true)
            {
                whereExpressions.Add(x => request.LeadIds.Contains(x.LeadId));
            }

            // Support batch OnboardingIds query
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
                    whereExpressions.Add(x => caseNames.Any(name => x.CaseName.ToLower().Contains(name.ToLower())));
                }
            }

            // Support comma-separated Case Codes
            if (!string.IsNullOrEmpty(request.CaseCode) && request.CaseCode != "string")
            {
                var caseCodes = request.GetCaseCodesList();
                if (caseCodes.Any())
                {
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
        }

        #endregion


        #region Permission Filter

        private class PermissionFilterContext
        {
            public bool NeedsPermissionFilter { get; set; }
            public bool SkipAllFilters { get; set; }
            public long UserId { get; set; }
            public string UserIdString { get; set; } = string.Empty;
            public List<long> UserTeamIds { get; set; } = new();
        }

        private PermissionFilterContext BuildPermissionFilterContext()
        {
            var context = new PermissionFilterContext();

            // Fast path: Client Credentials token
            if (_userContext?.Schema == Domain.Shared.Const.AuthSchemes.ItemIamClientIdentification)
            {
                _logger.LogDebug("Client Credentials token detected, skipping permission filters");
                context.SkipAllFilters = true;
                return context;
            }

            var userId = _userContext?.UserId;
            if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var userIdLong))
            {
                context.NeedsPermissionFilter = true;
                return context;
            }

            context.UserId = userIdLong;
            context.UserIdString = userIdLong.ToString();

            // Fast path: System Admin
            if (_userContext?.IsSystemAdmin == true)
            {
                _logger.LogDebug("User {UserId} is System Admin, skipping permission filters", userIdLong);
                context.SkipAllFilters = true;
                return context;
            }

            // Fast path: Tenant Admin
            if (_userContext != null && _userContext.HasAdminPrivileges(_userContext.TenantId))
            {
                _logger.LogDebug("User {UserId} is Tenant Admin, skipping permission filters", userIdLong);
                context.SkipAllFilters = true;
                return context;
            }

            // Regular user - needs permission filtering
            context.NeedsPermissionFilter = true;

            var userTeams = _permissionService.GetUserTeamIds();
            context.UserTeamIds = userTeams?
                .Select(t => long.TryParse(t, out var tid) ? tid : 0)
                .Where(t => t > 0)
                .ToList() ?? new List<long>();

            _logger.LogDebug("User {UserId} has {TeamCount} teams for permission filtering", 
                userIdLong, context.UserTeamIds.Count);

            return context;
        }

        private ISugarQueryable<Onboarding> ApplyPermissionFilterToQuery(
            ISugarQueryable<Onboarding> queryable, 
            PermissionFilterContext context)
        {
            if (context.SkipAllFilters || !context.NeedsPermissionFilter)
            {
                return queryable;
            }

            if (context.UserId == 0)
            {
                return queryable.Where(x => false);
            }

            var userId = context.UserId;
            var userTeamIds = context.UserTeamIds;
            var userIdString = context.UserIdString;

            // Build SQL condition for team check
            var teamConditions = userTeamIds.Select(t => $"view_teams::text LIKE '%\"{t}\"%'").ToList();
            var teamSqlCondition = teamConditions.Count > 0 
                ? $"({string.Join(" OR ", teamConditions)})" 
                : "false";

            var userSqlCondition = $"view_users::text LIKE '%\"{userIdString}\"%'";

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

        #region Sort Helpers

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

        private bool GetSortDirection(OnboardingQueryRequest request)
        {
            var sortDirection = request.SortDirection?.ToLower() ?? "desc";
            return sortDirection == "asc";
        }

        #endregion


        #region Data Loading Helpers

        private void ProcessStagesProgressParallel(List<Onboarding> entities)
        {
            try
            {
                if (entities.Count <= 10)
                {
                    foreach (var entity in entities)
                    {
                        _stageProgressService.LoadStagesProgressFromJsonReadOnly(entity);
                    }
                }
                else
                {
                    var entitiesCopy = entities.ToList();
                    Parallel.ForEach(entitiesCopy, new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 4)
                    }, entity =>
                    {
                        try
                        {
                            _stageProgressService.LoadStagesProgressFromJsonReadOnly(entity);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "[OnboardingQueryService] Failed to load stages progress for entity {EntityId}", entity.Id);
                            entity.StagesProgress = new List<OnboardingStageProgress>();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[OnboardingQueryService] Parallel loading failed, falling back to sequential");
                foreach (var entity in entities)
                {
                    try
                    {
                        _stageProgressService.LoadStagesProgressFromJsonReadOnly(entity);
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogDebug(innerEx, "[OnboardingQueryService] Failed to load stages progress for entity {EntityId}", entity.Id);
                        entity.StagesProgress = new List<OnboardingStageProgress>();
                    }
                }
            }
        }

        /// <inheritdoc />
        public async Task PopulateOnboardingOutputDtoAsync(List<OnboardingOutputDto> results, List<Onboarding> entities)
        {
            if (!results.Any() || !entities.Any())
            {
                _logger.LogDebug("PopulateOnboardingOutputDtoAsync - No results or entities to populate");
                return;
            }

            _logger.LogDebug(
                "PopulateOnboardingOutputDtoAsync - Processing {ResultCount} results and {EntityCount} entities",
                results.Count,
                entities.Count);

            // Step 1: Auto-generate CaseCode for legacy data (batch processing)
            await _helperService.EnsureCaseCodesAsync(entities);

            // Step 2: Batch get related data to avoid N+1 queries
            var (workflowDict, stageDict, stageEstimatedDaysDict) = await GetRelatedDataDictionariesAsync(entities);

            // Step 3: Batch check permissions using in-memory data (no DB queries)
            var permissionContext = await BuildPermissionContextAsync(results, entities);

            // Step 4: Populate each result DTO
            foreach (var result in results)
            {
                PopulateOnboardingOutputDtoFields(result, workflowDict, stageDict, stageEstimatedDaysDict, permissionContext);
            }

            _logger.LogDebug("PopulateOnboardingOutputDtoAsync - Completed successfully");
        }

        /// <summary>
        /// Get related data dictionaries for fast lookup
        /// </summary>
        private async Task<(Dictionary<long, string> workflowDict, Dictionary<long, string> stageDict, Dictionary<long, decimal?> stageEstimatedDaysDict)>
            GetRelatedDataDictionariesAsync(List<Onboarding> entities)
        {
            var (workflows, stages) = await GetRelatedDataBatchOptimizedAsync(entities);

            var workflowDict = workflows.ToDictionary(w => w.Id, w => w.Name);
            var stageDict = stages.ToDictionary(s => s.Id, s => s.Name);
            var stageEstimatedDaysDict = stages.ToDictionary(s => s.Id, s => s.EstimatedDuration);

            _logger.LogDebug(
                "GetRelatedDataDictionariesAsync - Loaded {WorkflowCount} workflows and {StageCount} stages",
                workflowDict.Count,
                stageDict.Count);

            return (workflowDict, stageDict, stageEstimatedDaysDict);
        }

        /// <summary>
        /// Build permission context for batch permission checking
        /// </summary>
        private async Task<PermissionBatchContext> BuildPermissionContextAsync(
            List<OnboardingOutputDto> results,
            List<Onboarding> entities)
        {
            var context = new PermissionBatchContext
            {
                UserId = _userContext?.UserId,
                IsSystemAdmin = _userContext?.IsSystemAdmin == true,
                IsTenantAdmin = _userContext != null && _userContext.HasAdminPrivileges(_userContext.TenantId)
            };

            if (string.IsNullOrEmpty(context.UserId) || !long.TryParse(context.UserId, out var userIdLong))
            {
                _logger.LogDebug("BuildPermissionContextAsync - No valid user context, skipping permission checks");
                context.Permissions = new Dictionary<long, PermissionInfoDto>();
                return context;
            }

            context.UserIdLong = userIdLong;

            // Pre-check module permissions once (batch optimization)
            context.CanViewCases = await _permissionService.CanViewCasesAsync();
            context.CanOperateCases = await _permissionService.CanOperateCasesAsync();

            _logger.LogDebug(
                "BuildPermissionContextAsync - User {UserId} module permissions: CanView={CanView}, CanOperate={CanOperate}",
                userIdLong,
                context.CanViewCases,
                context.CanOperateCases);

            // Delegate to CasePermissionService for batch permission checking
            context.Permissions = await _casePermissionService.CheckBatchCasePermissionsAsync(
                entities,
                userIdLong,
                context.CanViewCases,
                context.CanOperateCases);

            return context;
        }

        /// <summary>
        /// Permission batch context for efficient permission checking
        /// </summary>
        private class PermissionBatchContext
        {
            public string UserId { get; set; }
            public long UserIdLong { get; set; }
            public bool IsSystemAdmin { get; set; }
            public bool IsTenantAdmin { get; set; }
            public bool CanViewCases { get; set; }
            public bool CanOperateCases { get; set; }
            public Dictionary<long, PermissionInfoDto> Permissions { get; set; }
        }

        /// <summary>
        /// Populate individual onboarding output DTO fields
        /// </summary>
        private void PopulateOnboardingOutputDtoFields(
            OnboardingOutputDto result,
            Dictionary<long, string> workflowDict,
            Dictionary<long, string> stageDict,
            Dictionary<long, decimal?> stageEstimatedDaysDict,
            PermissionBatchContext permissionContext)
        {
            // Populate workflow name
            result.WorkflowName = workflowDict.GetValueOrDefault(result.WorkflowId);

            // Populate permission info from context
            if (permissionContext.Permissions.TryGetValue(result.Id, out var permission))
            {
                result.Permission = permission;
                result.IsDisabled = !permission.CanOperate;
            }
            else
            {
                // Fallback: deny all permissions if not found in context
                result.Permission = new PermissionInfoDto { CanView = false, CanOperate = false };
                result.IsDisabled = true;
            }

            // IMPORTANT: If CurrentStageId is null but stagesProgress exists, try to get current stage from stagesProgress
            if (!result.CurrentStageId.HasValue && result.StagesProgress != null && result.StagesProgress.Any())
            {
                var currentStageProgress = result.StagesProgress.FirstOrDefault(sp => sp.IsCurrent);
                if (currentStageProgress != null)
                {
                    result.CurrentStageId = currentStageProgress.StageId;
                    _logger.LogDebug("PopulateOnboardingOutputDto - Recovered CurrentStageId {CurrentStageId} from StagesProgress for Onboarding {OnboardingId}",
                        result.CurrentStageId, result.Id);
                }
            }

            // currentStageStartTime only takes startTime (null if none)
            result.CurrentStageStartTime = null;
            result.CurrentStageEndTime = null;
            double? estimatedDays = null;
            if (result.CurrentStageId.HasValue && result.StagesProgress != null && result.StagesProgress.Any())
            {
                var currentStageProgress = result.StagesProgress.FirstOrDefault(sp => sp.StageId == result.CurrentStageId.Value);
                if (currentStageProgress != null)
                {
                    if (currentStageProgress.StartTime.HasValue)
                    {
                        result.CurrentStageStartTime = OnboardingSharedUtilities.NormalizeToStartOfDay(currentStageProgress.StartTime.Value);
                    }
                    // currentStageEndTime priority: customEndTime > endTime > (startTime+estimatedDays) > null
                    // All times normalized to start of day (00:00:00)
                    if (currentStageProgress.CustomEndTime.HasValue)
                    {
                        result.CurrentStageEndTime = OnboardingSharedUtilities.NormalizeToStartOfDay(currentStageProgress.CustomEndTime.Value);
                    }
                    else if (currentStageProgress.EndTime.HasValue)
                    {
                        result.CurrentStageEndTime = OnboardingSharedUtilities.NormalizeToStartOfDay(currentStageProgress.EndTime.Value);
                    }
                    else
                    {
                        // Three-level priority: json.customEstimatedDays > json.estimatedDays > stageDict
                        // Normalize to integer
                        estimatedDays = currentStageProgress.CustomEstimatedDays.HasValue 
                            ? (double?)Math.Round(currentStageProgress.CustomEstimatedDays.Value, 0) 
                            : null;
                        if (!estimatedDays.HasValue || estimatedDays.Value <= 0)
                        {
                            estimatedDays = currentStageProgress.EstimatedDays.HasValue 
                                ? (double?)Math.Round(currentStageProgress.EstimatedDays.Value, 0) 
                                : null;
                            if ((!estimatedDays.HasValue || estimatedDays.Value <= 0) && result.CurrentStageId.HasValue)
                            {
                                // stageEstimatedDaysDict key: stageId -> EstimatedDuration
                                if (stageEstimatedDaysDict.TryGetValue(result.CurrentStageId.Value, out var val) && val != null && val > 0)
                                {
                                    estimatedDays = (double?)Math.Round(val.Value, 0);
                                }
                            }
                        }
                    }
                }
            }
            // Calculate currentStageEndTime separately - only when both startTime and estimatedDays exist
            // Normalize to start of day (00:00:00)
            if (result.CurrentStageEndTime == null && result.CurrentStageStartTime.HasValue && (estimatedDays.HasValue && estimatedDays.Value > 0))
            {
                var normalizedStartTime = OnboardingSharedUtilities.NormalizeToStartOfDay(result.CurrentStageStartTime.Value);
                result.CurrentStageEndTime = normalizedStartTime.AddDays((int)estimatedDays.Value);
            }

            if (result.CurrentStageId.HasValue)
            {
                result.CurrentStageName = stageDict.GetValueOrDefault(result.CurrentStageId.Value);

                // IMPORTANT: Priority for EstimatedDays: customEstimatedDays > stage.EstimatedDuration
                // Normalize to integer (round to nearest whole number)
                var currentStageProgress = result.StagesProgress?.FirstOrDefault(sp => sp.StageId == result.CurrentStageId.Value);
                if (currentStageProgress != null && currentStageProgress.CustomEstimatedDays.HasValue && currentStageProgress.CustomEstimatedDays.Value > 0)
                {
                    result.CurrentStageEstimatedDays = Math.Round(currentStageProgress.CustomEstimatedDays.Value, 0);
                }
                else
                {
                    var stageEstimatedDays = stageEstimatedDaysDict.GetValueOrDefault(result.CurrentStageId.Value);
                    result.CurrentStageEstimatedDays = stageEstimatedDays.HasValue 
                        ? Math.Round(stageEstimatedDays.Value, 0) 
                        : null;
                }

                // End time already derived strictly from stagesProgress above
            }
            else
            {
                // Log when CurrentStageId is still null after fallback attempt
                _logger.LogWarning("PopulateOnboardingOutputDto - CurrentStageId is null for Onboarding {OnboardingId}, StagesProgress count: {StagesProgressCount}",
                    result.Id, result.StagesProgress?.Count ?? 0);
            }
        }

        private async Task<(List<Workflow> workflows, List<Stage> stages)> GetRelatedDataBatchOptimizedAsync(List<Onboarding> entities)
        {
            var workflowIds = entities.Select(x => x.WorkflowId).Distinct().ToList();
            var stageIds = entities.Where(x => x.CurrentStageId.HasValue)
                    .Select(x => x.CurrentStageId.Value).Distinct().ToList();

            var workflows = await GetWorkflowsBatchAsync(workflowIds);
            var stages = await GetStagesBatchAsync(stageIds);

            return (workflows, stages);
        }

        private async Task<List<Workflow>> GetWorkflowsBatchAsync(List<long> workflowIds)
        {
            if (!workflowIds.Any())
                return new List<Workflow>();

            try
            {
                return await _workflowRepository.GetListAsync(w => workflowIds.Contains(w.Id) && w.IsValid);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error batch loading workflows");
                return new List<Workflow>();
            }
        }

        private async Task<List<Stage>> GetStagesBatchAsync(List<long> stageIds)
        {
            if (!stageIds.Any())
                return new List<Stage>();

            try
            {
                return await _stageRepository.GetListAsync(s => stageIds.Contains(s.Id) && s.IsValid);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error batch loading stages");
                return new List<Stage>();
            }
        }

        #endregion


        #region GetActiveBySystemIdAsync

        public async Task<PagedResult<OnboardingOutputDto>> GetActiveBySystemIdAsync(
            string systemId, 
            string? entityId = null, 
            string sortField = "createDate", 
            string sortOrder = "desc", 
            int pageIndex = 1, 
            int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(systemId))
            {
                return new PagedResult<OnboardingOutputDto>
                {
                    Items = new List<OnboardingOutputDto>(),
                    TotalCount = 0,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
            }

            // Validate pagination parameters
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var tenantId = TenantContextHelper.GetTenantIdOrDefault(_userContext);
            var appCode = TenantContextHelper.GetAppCodeOrDefault(_userContext);

            try
            {
                var queryable = _onboardingRepository.GetSqlSugarClient().Queryable<Onboarding>()
                    .Where(x => x.IsValid == true)
                    .Where(x => x.IsActive == true)
                    .Where(x => x.SystemId == systemId);

                if (!string.IsNullOrWhiteSpace(entityId))
                {
                    queryable = queryable.Where(x => x.EntityId == entityId);
                }

                if (!string.IsNullOrEmpty(tenantId))
                {
                    queryable = queryable.Where(x => x.TenantId.ToLower() == tenantId.ToLower());
                }
                if (!string.IsNullOrEmpty(appCode))
                {
                    queryable = queryable.Where(x => x.AppCode.ToLower() == appCode.ToLower());
                }

                // Apply sorting
                var isAscending = sortOrder?.ToLower() == "asc";
                queryable = (sortField?.ToLower()) switch
                {
                    "modifydate" => isAscending ? queryable.OrderBy(x => x.ModifyDate) : queryable.OrderByDescending(x => x.ModifyDate),
                    "leadname" => isAscending ? queryable.OrderBy(x => x.CaseName) : queryable.OrderByDescending(x => x.CaseName),
                    "casename" => isAscending ? queryable.OrderBy(x => x.CaseName) : queryable.OrderByDescending(x => x.CaseName),
                    "casecode" => isAscending ? queryable.OrderBy(x => x.CaseCode) : queryable.OrderByDescending(x => x.CaseCode),
                    "status" => isAscending ? queryable.OrderBy(x => x.Status) : queryable.OrderByDescending(x => x.Status),
                    _ => isAscending ? queryable.OrderBy(x => x.CreateDate) : queryable.OrderByDescending(x => x.CreateDate)
                };

                var totalCount = await queryable.CountAsync();

                var entities = await queryable
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                if (!entities.Any())
                {
                    return new PagedResult<OnboardingOutputDto>
                    {
                        Items = new List<OnboardingOutputDto>(),
                        TotalCount = totalCount,
                        PageIndex = pageIndex,
                        PageSize = pageSize
                    };
                }

                // Batch load workflows and stages
                var workflowIds = entities.Select(e => e.WorkflowId).Distinct().ToList();
                var stageIds = entities.Where(e => e.CurrentStageId.HasValue).Select(e => e.CurrentStageId!.Value).Distinct().ToList();

                var workflows = await _workflowRepository.ClearFilter()
                    .Where(w => workflowIds.Contains(w.Id) && w.IsValid)
                    .ToListAsync();
                var workflowDict = workflows.ToDictionary(w => w.Id, w => w.Name);

                var stageDict = new Dictionary<long, string>();
                if (stageIds.Any())
                {
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

                    if (workflowDict.TryGetValue(entity.WorkflowId, out var workflowName))
                    {
                        dto.WorkflowName = workflowName;
                    }

                    if (entity.CurrentStageId.HasValue && stageDict.TryGetValue(entity.CurrentStageId.Value, out var stageName))
                    {
                        dto.CurrentStageName = stageName;
                    }

                    results.Add(dto);
                }

                return new PagedResult<OnboardingOutputDto>
                {
                    Items = results,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active onboardings by SystemId: {SystemId}", systemId);
                throw;
            }
        }

        #endregion


        #region GetProgressAsync

        public async Task<OnboardingProgressDto> GetProgressAsync(long id)
        {
            var entity = await _onboardingRepository.GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            var stages = await _stageRepository.GetByWorkflowIdAsync(entity.WorkflowId);
            
            // Ensure stages progress is properly initialized
            await _stageProgressService.EnsureStagesProgressInitializedAsync(entity, stages);

            var totalStages = stages.Count;
            var completedStages = entity.CurrentStageOrder;

            var estimatedCompletion = entity.CreateDate.AddDays(totalStages * 7);

            var isOverdue = entity.Status != "Completed" &&
                           entity.EstimatedCompletionDate.HasValue &&
                           DateTimeOffset.UtcNow > entity.EstimatedCompletionDate.Value;

            var stagesProgressDto = _mapper.Map<List<OnboardingStageProgressDto>>(entity.StagesProgress);

            // Batch query all actions for all stages
            if (stagesProgressDto != null && stagesProgressDto.Any())
            {
                var stageIds = stagesProgressDto.Select(sp => sp.StageId).ToList();
                Dictionary<long, List<ActionTriggerMappingWithActionInfo>> actionsDict;
                try
                {
                    actionsDict = await _actionManagementService.GetActionTriggerMappingsByTriggerSourceIdsAsync(stageIds);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to batch query actions for stages in GetProgressAsync");
                    actionsDict = new Dictionary<long, List<ActionTriggerMappingWithActionInfo>>();
                }

                foreach (var stageProgress in stagesProgressDto)
                {
                    stageProgress.Actions = actionsDict.TryGetValue(stageProgress.StageId, out var actions)
                        ? actions
                        : new List<ActionTriggerMappingWithActionInfo>();
                }
            }

            return new OnboardingProgressDto
            {
                OnboardingId = entity.Id,
                CurrentStageId = entity.CurrentStageId,
                CurrentStageName = stages.FirstOrDefault(s => s.Id == entity.CurrentStageId)?.Name,
                TotalStages = totalStages,
                CompletedStages = completedStages,
                CompletionPercentage = entity.CompletionRate,
                StartTime = entity.CreateDate,
                EstimatedCompletionTime = entity.EstimatedCompletionDate ?? estimatedCompletion,
                ActualCompletionTime = entity.ActualCompletionDate,
                IsOverdue = isOverdue,
                Status = entity.Status,
                Priority = entity.Priority,
                StagesProgress = stagesProgressDto
            };
        }

        #endregion


        #region ExportToExcelAsync

        public async Task<Stream> ExportToExcelAsync(OnboardingQueryRequest query)
        {
            // Get data using existing query method
            var result = await QueryAsync(query);
            var data = result.Data;

            // Transform to export format
            var exportData = data.Select(item => new OnboardingExportDto
            {
                CustomerName = item.CaseName,
                Id = item.LeadId,
                CaseCode = item.CaseCode,
                ContactName = item.ContactPerson,
                LifeCycleStage = item.LifeCycleStageName,
                WorkFlow = item.WorkflowName,
                OnboardStage = item.CurrentStageName,
                Priority = item.Priority,
                Ownership = !string.IsNullOrWhiteSpace(item.OwnershipName)
                    ? $"{item.OwnershipName} ({item.OwnershipEmail})"
                    : string.Empty,
                Status = GetDisplayStatus(item.Status),
                StartDate = FormatDateForExport(item.CurrentStageStartTime),
                EndDate = FormatDateForExport(item.CurrentStageEndTime),
                UpdatedBy = string.IsNullOrWhiteSpace(item.StageUpdatedBy) ? item.ModifyBy : item.StageUpdatedBy,
                UpdateTime = (item.StageUpdatedTime.HasValue ? item.StageUpdatedTime.Value : item.ModifyDate)
                  .ToString("MM/dd/yyyy HH:mm:ss")
            }).ToList();

            return GenerateExcelWithEPPlus(exportData);
        }

        private string GetDisplayStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
                return status;

            return status switch
            {
                "Active" or "Started" => "InProgress",
                "Cancelled" => "Aborted",
                "Force Completed" => "Force Completed",
                _ => status
            };
        }

        private string FormatDateForExport(DateTimeOffset? dateTime)
        {
            if (!dateTime.HasValue)
                return "";

            return dateTime.Value.ToString("MM/dd/yyyy HH:mm");
        }

        /// <summary>
        /// Generate Excel file using EPPlus
        /// Note: The returned Stream must be disposed by the caller
        /// </summary>
        /// <returns>A MemoryStream containing the Excel content. Caller is responsible for disposing.</returns>
        private Stream GenerateExcelWithEPPlus(List<OnboardingExportDto> data)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Onboarding Export");

            var headers = new[]
            {
                "Customer Name", "Case Code", "Contact Name", "Life Cycle Stage", "Workflow", "Stage",
                "Priority", "Ownership", "Status", "Start Date", "End Date", "Updated By", "Update Time"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }

            for (int row = 0; row < data.Count; row++)
            {
                var item = data[row];
                worksheet.Cells[row + 2, 1].Value = item.CustomerName;
                worksheet.Cells[row + 2, 2].Value = item.CaseCode;
                worksheet.Cells[row + 2, 3].Value = item.ContactName;
                worksheet.Cells[row + 2, 4].Value = item.LifeCycleStage;
                worksheet.Cells[row + 2, 5].Value = item.WorkFlow;
                worksheet.Cells[row + 2, 6].Value = item.OnboardStage;
                worksheet.Cells[row + 2, 7].Value = item.Priority;
                worksheet.Cells[row + 2, 8].Value = item.Ownership;
                worksheet.Cells[row + 2, 9].Value = item.Status;
                worksheet.Cells[row + 2, 10].Value = item.StartDate;
                worksheet.Cells[row + 2, 11].Value = item.EndDate;
                worksheet.Cells[row + 2, 12].Value = item.UpdatedBy;
                worksheet.Cells[row + 2, 13].Value = item.UpdateTime;
            }

            worksheet.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            try
            {
                package.SaveAs(stream);
                stream.Position = 0;
                return stream;
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        #endregion
    }
}
