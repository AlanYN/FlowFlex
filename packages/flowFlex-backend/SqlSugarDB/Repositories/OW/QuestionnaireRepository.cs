using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Text.Json;
using System.Linq.Expressions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums.OW;
using Microsoft.AspNetCore.Http;
using AppContext = FlowFlex.Domain.Shared.Models.AppContext;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// Questionnaire repository implementation
    /// </summary>
    public class QuestionnaireRepository : BaseRepository<Questionnaire>, IQuestionnaireRepository, IScopedService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<QuestionnaireRepository> _logger;

        public QuestionnaireRepository(
            ISqlSugarClient sqlSugarClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<QuestionnaireRepository> logger) : base(sqlSugarClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// 获取当前租户ID
        /// </summary>
        private string GetCurrentTenantId()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
                return "DEFAULT";

            // 从请求头获取
            var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                return tenantId;
            }

            // 从 AppContext 获取
            if (httpContext.Items.TryGetValue("AppContext", out var appContextObj) &&
                appContextObj is AppContext appContext)
            {
                return appContext.TenantId;
            }

            return "DEFAULT";
        }

        /// <summary>
        /// 获取当前应用代码
        /// </summary>
        private string GetCurrentAppCode()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
                return "DEFAULT";

            // 从请求头获取
            var appCode = httpContext.Request.Headers["X-App-Code"].FirstOrDefault();
            if (!string.IsNullOrEmpty(appCode))
            {
                return appCode;
            }

            // 从 AppContext 获取
            if (httpContext.Items.TryGetValue("AppContext", out var appContextObj) &&
                appContextObj is AppContext appContext)
            {
                return appContext.AppCode;
            }

            return "DEFAULT";
        }

        /// <summary>
        /// Get questionnaire list by category
        /// </summary>
        public async Task<List<Questionnaire>> GetByCategoryAsync(string category)
        {
            var query = db.Queryable<Questionnaire>()
                .Where(x => x.IsValid == true && x.IsActive == true);

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(x => x.Category == category);
            }

            return await query
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get questionnaires by multiple IDs (batch query)
        /// </summary>
        public async Task<List<Questionnaire>> GetByIdsAsync(List<long> ids)
        {
            if (ids == null || !ids.Any())
            {
                return new List<Questionnaire>();
            }

            return await db.Queryable<Questionnaire>()
                .Where(x => x.IsValid == true && ids.Contains(x.Id))
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get template questionnaires
        /// </summary>
        public async Task<List<Questionnaire>> GetTemplatesAsync()
        {
            return await db.Queryable<Questionnaire>()
                .Where(x => x.IsTemplate == true && x.IsValid == true && x.IsActive == true)
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get questionnaire instances by template
        /// </summary>
        public async Task<List<Questionnaire>> GetByTemplateIdAsync(long templateId)
        {
            return await db.Queryable<Questionnaire>()
                .Where(x => x.TemplateId == templateId && x.IsValid == true)
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Check if name exists
        /// </summary>
        public async Task<bool> IsNameExistsAsync(string name, string category = null, long? excludeId = null)
        {
            // 获取当前租户ID和应用代码
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();
            
            _logger.LogInformation($"[QuestionnaireRepository] IsNameExistsAsync with name={name}, category={category}, TenantId={currentTenantId}, AppCode={currentAppCode}");
            
            var query = db.Queryable<Questionnaire>()
                .Where(x => x.Name == name && x.IsValid == true)
                .Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode); // 添加租户和应用代码过滤

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(x => x.Category == category);
            }

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// Get questionnaires with pagination and filters
        /// </summary>
        public async Task<(List<Questionnaire> items, int totalCount)> GetPagedAsync(
            int pageIndex,
            int pageSize,
            string name = null,
            long? workflowId = null,
            long? stageId = null,
            bool? isTemplate = null,
            bool? isActive = null,
            string sortField = "CreateDate",
            string sortDirection = "desc")
        {
            // 记录当前请求头
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var headerTenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                var headerAppCode = httpContext.Request.Headers["X-App-Code"].FirstOrDefault();
                _logger.LogInformation($"[QuestionnaireRepository] GetPagedAsync with headers: X-Tenant-Id={headerTenantId}, X-App-Code={headerAppCode}");
            }

            // Build basic query condition list (without workflowId and stageId since they're in JSON now)
            var whereExpressions = new List<Expression<Func<Questionnaire, bool>>>();

            // Basic filter conditions
            whereExpressions.Add(x => x.IsValid == true);

            // 获取当前租户ID和应用代码
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();
            
            _logger.LogInformation($"[QuestionnaireRepository] GetPagedAsync applying explicit filters: TenantId={currentTenantId}, AppCode={currentAppCode}");
            
            // 添加租户和应用过滤条件
            whereExpressions.Add(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode);

            if (!string.IsNullOrWhiteSpace(name))
            {
                // Support comma-separated questionnaire names
                var names = name.Split(',', StringSplitOptions.RemoveEmptyEntries)
                               .Select(n => n.Trim())
                               .Where(n => !string.IsNullOrEmpty(n))
                               .ToList();

                if (names.Any())
                {
                    // Use OR condition to match any of the questionnaire names (case-insensitive)
                    whereExpressions.Add(x => names.Any(n => x.Name.ToLower().Contains(n.ToLower())));
                }
            }

            if (isTemplate.HasValue)
            {
                whereExpressions.Add(x => x.IsTemplate == isTemplate.Value);
            }

            if (isActive.HasValue)
            {
                whereExpressions.Add(x => x.IsActive == isActive.Value);
            }

            // Determine sort field and direction
            Expression<Func<Questionnaire, object>> orderByExpression = sortField?.ToLower() switch
            {
                "name" => x => x.Name,
                "createdate" => x => x.CreateDate,
                "modifydate" => x => x.ModifyDate,
                _ => x => x.CreateDate
            };

            bool isAsc = sortDirection?.ToLower() == "asc";

            // Get all matching questionnaires first
            var query = db.Queryable<Questionnaire>();

            // Apply where conditions
            foreach (var whereExpression in whereExpressions)
            {
                query = query.Where(whereExpression);
            }

            var allItems = await query
                .OrderBy(orderByExpression, isAsc ? SqlSugar.OrderByType.Asc : SqlSugar.OrderByType.Desc)
                .ToListAsync();

            // Filter by workflowId and stageId in memory (since they're in JSON now)
            if (workflowId.HasValue || stageId.HasValue)
            {
                allItems = allItems.Where(q =>
                {
                    // Check if assignments contain the specified workflowId or stageId
                    if (q.Assignments?.Any() == true)
                    {
                        return q.Assignments.Any(a =>
                            (!workflowId.HasValue || a.WorkflowId == workflowId.Value) &&
                            (!stageId.HasValue || a.StageId == stageId.Value)
                        );
                    }
                    return false;
                }).ToList();
            }

            // Calculate total count
            var totalCount = allItems.Count;
            
            _logger.LogInformation($"[QuestionnaireRepository] GetPagedAsync found {totalCount} total questionnaires with TenantId={currentTenantId}, AppCode={currentAppCode}");

            // Apply pagination
            var items = allItems
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            _logger.LogInformation($"[QuestionnaireRepository] GetPagedAsync returned {items.Count} questionnaires for page {pageIndex}");

            return (items, totalCount);
        }

        /// <summary>
        /// Get questionnaire statistics by category
        /// </summary>
        public async Task<Dictionary<string, object>> GetStatisticsByCategoryAsync(string category)
        {
            var query = db.Queryable<Questionnaire>()
                .Where(x => x.IsValid == true);

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(x => x.Category == category);
            }

            var totalCount = await query.CountAsync();
            var activeCount = await query.Where(x => x.IsActive == true).CountAsync();
            var templateCount = await query.Where(x => x.IsTemplate == true).CountAsync();
            var publishedCount = await query.Where(x => x.Status == "Published").CountAsync();
            var draftCount = await query.Where(x => x.Status == "Draft").CountAsync();

            return new Dictionary<string, object>
            {
                { "TotalCount", totalCount },
                { "ActiveCount", activeCount },
                { "TemplateCount", templateCount },
                { "PublishedCount", publishedCount },
                { "DraftCount", draftCount }
            };
        }

        /// <summary>
        /// Update questionnaire statistics
        /// </summary>
        public async Task<bool> UpdateStatisticsAsync(long id, int totalQuestions, int requiredQuestions)
        {
            var result = await db.Updateable<Questionnaire>()
                .SetColumns(x => new Questionnaire
                {
                    TotalQuestions = totalQuestions,
                    RequiredQuestions = requiredQuestions,
                    ModifyDate = DateTimeOffset.Now
                })
                .Where(x => x.Id == id && x.IsValid == true)
                .ExecuteCommandAsync();

            return result > 0;
        }

        /// <summary>
        /// Get published questionnaires
        /// </summary>
        public async Task<List<Questionnaire>> GetPublishedAsync()
        {
            return await db.Queryable<Questionnaire>()
                .Where(x => x.Status == "Published" && x.IsValid == true && x.IsActive == true)
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Validate structure JSON
        /// </summary>
        public async Task<bool> ValidateStructureAsync(string structureJson)
        {
            if (string.IsNullOrWhiteSpace(structureJson))
            {
                return true; // Empty structure is valid
            }

            try
            {
                // Basic JSON validation
                JsonDocument.Parse(structureJson);

                // Additional structure validation can be implemented here
                // Future enhancement: validate required fields, question types, etc.

                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>
        /// Get questionnaires by multiple stage IDs (supports both old single StageId and new Assignments JSON)
        /// </summary>
        public async Task<List<Questionnaire>> GetByStageIdsAsync(List<long> stageIds)
        {
            if (stageIds == null || !stageIds.Any())
            {
                return new List<Questionnaire>();
            }

            // Get all questionnaires and filter by assignments in memory
            // This is necessary because SqlSugar doesn't support JSON queries directly
            var allQuestionnaires = await db.Queryable<Questionnaire>()
                .Where(x => x.IsValid == true && x.IsActive == true)
                .ToListAsync();

            return allQuestionnaires.Where(q =>
                // Check new Assignments JSON field
                (q.Assignments?.Any(a => stageIds.Contains(a.StageId) && a.StageId > 0) == true) // 只匹配有效的StageId
            ).OrderByDescending(x => x.CreateDate).ToList();
        }

        /// <summary>
        /// Check if workflow and stage association already exists (supports new Assignments JSON)
        /// Note: This method is retained for backward compatibility but is no longer used for uniqueness validation
        /// Multiple questionnaires can now be associated with the same workflow-stage combination
        /// </summary>
        public async Task<bool> IsWorkflowStageAssociationExistsAsync(long? workflowId, long? stageId, long? excludeId = null)
        {
            // If both workflowId and stageId are empty, no need to check
            if (!workflowId.HasValue && !stageId.HasValue)
            {
                return false;
            }

            // Get all questionnaires and check assignments in memory
            var allQuestionnaires = await db.Queryable<Questionnaire>()
                .Where(x => x.IsValid == true)
                .WhereIF(excludeId.HasValue, x => x.Id != excludeId.Value)
                .ToListAsync();

            return allQuestionnaires.Any(q =>
            {
                if (q.Assignments?.Any() == true)
                {
                    return q.Assignments.Any(a =>
                        (!workflowId.HasValue || a.WorkflowId == workflowId.Value) &&
                        (!stageId.HasValue || a.StageId == stageId.Value)
                    );
                }
                return false;
            });
        }

        /// <summary>
        /// Get existing questionnaire with same workflow and stage association (supports new Assignments JSON)
        /// Note: This method returns the first match found, but multiple questionnaires can now exist with the same association
        /// </summary>
        public async Task<Questionnaire> GetByWorkflowStageAssociationAsync(long? workflowId, long? stageId, long? excludeId = null)
        {
            // If both workflowId and stageId are empty, return null
            if (!workflowId.HasValue && !stageId.HasValue)
            {
                return null;
            }

            // Get all questionnaires and check assignments in memory
            var allQuestionnaires = await db.Queryable<Questionnaire>()
                .Where(x => x.IsValid == true)
                .WhereIF(excludeId.HasValue, x => x.Id != excludeId.Value)
                .ToListAsync();

            return allQuestionnaires.FirstOrDefault(q =>
            {
                if (q.Assignments?.Any() == true)
                {
                    return q.Assignments.Any(a =>
                        (!workflowId.HasValue || a.WorkflowId == workflowId.Value) &&
                        (!stageId.HasValue || a.StageId == stageId.Value)
                    );
                }
                return false;
            });
        }

        /// <summary>
        /// Get questionnaires by workflow ID (supports both old single WorkflowId and new Assignments JSON)
        /// </summary>
        public async Task<List<Questionnaire>> GetByWorkflowIdAsync(long workflowId)
        {
            // Get all questionnaires and filter by assignments in memory
            var allQuestionnaires = await db.Queryable<Questionnaire>()
                .Where(x => x.IsValid == true)
                .ToListAsync();

            return allQuestionnaires.Where(q =>
                // Check new Assignments JSON field
                (q.Assignments?.Any(a => a.WorkflowId == workflowId) == true)
            ).OrderBy(x => x.CreateDate).ToList();
        }

        /// <summary>
        /// Get questionnaires by stage ID (supports both old single StageId and new Assignments JSON)
        /// </summary>
        public async Task<List<Questionnaire>> GetByStageIdAsync(long stageId)
        {
            // Get all questionnaires and filter by assignments in memory
            var allQuestionnaires = await db.Queryable<Questionnaire>()
                .Where(x => x.IsValid == true)
                .ToListAsync();

            return allQuestionnaires.Where(q =>
                // Check new Assignments JSON field
                (q.Assignments?.Any(a => a.StageId == stageId && a.StageId > 0) == true) // 只匹配有效的StageId
            ).OrderBy(x => x.CreateDate).ToList();
        }

        /// <summary>
        /// Get questionnaires by status
        /// </summary>
        public async Task<List<Questionnaire>> GetByStatusAsync(string status)
        {
            return await db.Queryable<Questionnaire>()
                .Where(x => x.Status == status && x.IsValid == true)
                .OrderBy(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Update questionnaire statistics
        /// </summary>
        public async Task<bool> UpdateQuestionnaireStatsAsync(long questionnaireId, int totalQuestions, int requiredQuestions)
        {
            var result = await db.Updateable<Questionnaire>()
                .SetColumns(x => new Questionnaire
                {
                    TotalQuestions = totalQuestions,
                    RequiredQuestions = requiredQuestions,
                    ModifyDate = DateTimeOffset.Now
                })
                .Where(x => x.Id == questionnaireId && x.IsValid == true)
                .ExecuteCommandAsync();

            return result > 0;
        }

        /// <summary>
        /// Get questionnaires by names
        /// </summary>
        public async Task<List<Questionnaire>> GetByNamesAsync(List<string> names)
        {
            if (names == null || !names.Any())
            {
                return new List<Questionnaire>();
            }

            return await db.Queryable<Questionnaire>()
                .Where(x => x.IsValid == true && names.Contains(x.Name))
                .OrderBy(x => x.Name)
                .OrderBy(x => x.CreateDate, SqlSugar.OrderByType.Desc)
                .ToListAsync();
        }

        /// <summary>
        /// 获取所有问卷列表，确保应用租户和应用过滤器
        /// </summary>
        public override async Task<List<Questionnaire>> GetListAsync(CancellationToken cancellationToken = default, bool copyNew = false)
        {
            // 记录当前请求头
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var headerTenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                var headerAppCode = httpContext.Request.Headers["X-App-Code"].FirstOrDefault();
                _logger.LogInformation($"[QuestionnaireRepository] GetListAsync with headers: X-Tenant-Id={headerTenantId}, X-App-Code={headerAppCode}");
            }

            // 显式添加租户和应用过滤条件
            var query = db.Queryable<Questionnaire>().Where(x => x.IsValid == true);
            
            // 获取当前租户ID和应用代码
            var currentTenantId = GetCurrentTenantId();
            var currentAppCode = GetCurrentAppCode();
            
            _logger.LogInformation($"[QuestionnaireRepository] GetListAsync applying explicit filters: TenantId={currentTenantId}, AppCode={currentAppCode}");
            
            // 显式添加过滤条件
            query = query.Where(x => x.TenantId == currentTenantId && x.AppCode == currentAppCode);
            
            // 执行查询
            var result = await query.OrderByDescending(x => x.CreateDate).ToListAsync(cancellationToken);
            
            _logger.LogInformation($"[QuestionnaireRepository] GetListAsync returned {result.Count} questionnaires with TenantId={currentTenantId}, AppCode={currentAppCode}");
            
            return result;
        }

        /// <summary>
        /// 直接查询，使用显式过滤条件
        /// </summary>
        public async Task<List<Questionnaire>> GetListWithExplicitFiltersAsync(string tenantId, string appCode)
        {
            _logger.LogInformation($"[QuestionnaireRepository] GetListWithExplicitFiltersAsync with explicit TenantId={tenantId}, AppCode={appCode}");
            
            // 临时禁用全局过滤器
            db.QueryFilter.ClearAndBackup();
            
            try
            {
                // 使用显式过滤条件
                var query = db.Queryable<Questionnaire>()
                    .Where(x => x.IsValid == true)
                    .Where(x => x.TenantId == tenantId && x.AppCode == appCode);
                
                // 执行查询
                var result = await query.OrderByDescending(x => x.CreateDate).ToListAsync();
                
                _logger.LogInformation($"[QuestionnaireRepository] GetListWithExplicitFiltersAsync returned {result.Count} questionnaires with explicit filters");
                
                return result;
            }
            finally
            {
                // 恢复全局过滤器
                db.QueryFilter.Restore();
            }
        }
    }
}
