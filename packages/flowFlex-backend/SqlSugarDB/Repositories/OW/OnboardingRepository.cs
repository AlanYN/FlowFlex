using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// Onboarding repository implementation
    /// </summary>
    public class OnboardingRepository : BaseRepository<Onboarding>, IOnboardingRepository, IScopedService
    {
        public OnboardingRepository(ISqlSugarClient context) : base(context)
        {
        }

        /// <summary>
        /// Ensure the onboarding table exists, create if necessary
        /// </summary>
        public async Task EnsureTableExistsAsync()
        {
            try
            {
                // Check if table exists
                var tableExists = db.DbMaintenance.IsAnyTable("ff_onboarding", false);

                if (!tableExists)
                {
                    // Debug logging handled by structured logging
                    // Create table using SqlSugar code first
                    db.CodeFirst.SetStringDefaultLength(200).InitTables<Onboarding>();
                    // Debug logging handled by structured logging
                    // Create performance optimization indexes
                    await CreatePerformanceIndexesAsync();
                }
                else
                {
                    // Debug logging handled by structured logging
                    // Ensure indexes exist
                    await EnsurePerformanceIndexesAsync();
                }
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                // Log but don't throw, let the insert try anyway
            }
        }

        /// <summary>
        /// Create performance optimization indexes
        /// </summary>
        private async Task CreatePerformanceIndexesAsync()
        {
            try
            {
                var indexQueries = new[]
                {
                    // Tenant ID + validity index - most important query condition
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_tenant_valid ON ff_onboarding(tenant_id, is_valid)",
                    
                    // Tenant ID + validity + active status index
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_tenant_valid_active ON ff_onboarding(tenant_id, is_valid, is_active)",
                    
                    // Lead ID index - for Lead related queries
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_lead_id ON ff_onboarding(lead_id)",
                    
                    // Workflow ID index - for workflow queries
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_workflow_id ON ff_onboarding(workflow_id)",
                    
                    // Current Stage ID index - for stage queries
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_stage_id ON ff_onboarding(current_stage_id)",
                    
                    // Status index - for status filtering
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_status ON ff_onboarding(status)",
                    
                    // Priority index - for priority filtering
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_priority ON ff_onboarding(priority)",
                    
                    // Current assignee index - for assignee queries
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_assignee_id ON ff_onboarding(current_assignee_id)",
                    
                    // Create time index - for sorting and time range queries
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_create_date ON ff_onboarding(create_date)",
                    
                    // Start time index - for time range queries
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_start_date ON ff_onboarding(start_date)",
                    
                    // Completion rate index - for progress queries
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_completion_rate ON ff_onboarding(completion_rate)",
                    
                    // Composite index: tenant + Lead name (for fuzzy queries)
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_tenant_lead_name ON ff_onboarding(tenant_id, lead_name)",
                    
                    // Composite index: tenant + status + create time (common query combination)
                    "CREATE INDEX IF NOT EXISTS idx_onboarding_tenant_status_create ON ff_onboarding(tenant_id, status, create_date)",
                };

                foreach (var query in indexQueries)
                {
                    try
                    {
                        await db.Ado.ExecuteCommandAsync(query);
                        // Debug logging handled by structured logging[5]}"); // Extract index name
                    }
                    catch (Exception ex)
                    {
                        // Debug logging handled by structured logging
                    }
                }
                // Debug logging handled by structured logging
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
            }
        }

        /// <summary>
        /// Ensure performance indexes exist
        /// </summary>
        private async Task EnsurePerformanceIndexesAsync()
        {
            try
            {
                // Check if key indexes exist, create if not
                var checkIndexQuery = @"
                    SELECT COUNT(*) 
                    FROM pg_indexes 
                    WHERE tablename = 'ff_onboarding' 
                    AND indexname = 'idx_onboarding_tenant_valid'";

                var indexExists = await db.Ado.GetIntAsync(checkIndexQuery);

                if (indexExists == 0)
                {
                    // Debug logging handled by structured logging
                    await CreatePerformanceIndexesAsync();
                }
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
            }
        }

        /// <summary>
        /// Get SqlSugar client for direct testing purposes
        /// </summary>
        public ISqlSugarClient GetSqlSugarClient()
        {
            return db;
        }



        /// <summary>
        /// Get onboarding list by lead IDs (batch operation)
        /// </summary>
        public async Task<List<Onboarding>> GetByLeadIdsAsync(List<string> leadIds)
        {
            if (leadIds?.Any() != true)
            {
                return new List<Onboarding>();
            }

            return await GetListAsync(x => leadIds.Contains(x.LeadId) && x.IsValid);
        }

        /// <summary>
        /// Get onboarding list by workflow ID
        /// </summary>
        public async Task<List<Onboarding>> GetListByWorkflowIdAsync(long workflowId)
        {
            return await GetListAsync(x => x.WorkflowId == workflowId && x.IsValid);
        }

        /// <summary>
        /// Get onboarding list by stage ID
        /// </summary>
        public async Task<List<Onboarding>> GetListByStageIdAsync(long stageId)
        {
            return await GetListAsync(x => x.CurrentStageId == stageId && x.IsValid);
        }

        /// <summary>
        /// Get onboarding list by status
        /// </summary>
        public async Task<List<Onboarding>> GetListByStatusAsync(string status)
        {
            return await GetListAsync(x => x.Status == status && x.IsValid);
        }

        /// <summary>
        /// Get onboarding list by assignee ID
        /// </summary>
        public async Task<List<Onboarding>> GetListByAssigneeIdAsync(long assigneeId)
        {
            return await GetListAsync(x => x.CurrentAssigneeId == assigneeId && x.IsValid);
        }

        /// <summary>
        /// Update stage for onboarding
        /// </summary>
        public async Task<bool> UpdateStageAsync(long id, long stageId, int stageOrder)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            entity.CurrentStageId = stageId;
            entity.CurrentStageOrder = stageOrder;
            entity.ModifyDate = DateTimeOffset.UtcNow;

            return await UpdateAsync(entity);
        }

        /// <summary>
        /// Update completion rate
        /// </summary>
        public async Task<bool> UpdateCompletionRateAsync(long id, decimal completionRate)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }
            // Debug logging handled by structured logging
            // Try raw SQL update as the most direct approach
            try
            {
                var modifyDate = DateTimeOffset.UtcNow;
                // Debug logging handled by structured logging
                var rawSql = @"
                    UPDATE ff_onboarding 
                    SET completion_rate = @CompletionRate, 
                        modify_date = @ModifyDate 
                    WHERE id = @Id";

                var parameters = new
                {
                    CompletionRate = completionRate,
                    ModifyDate = modifyDate,
                    Id = id
                };
                // Debug logging handled by structured logging
                var rowsAffected = await db.Ado.ExecuteCommandAsync(rawSql, parameters);
                // Debug logging handled by structured logging
                if (rowsAffected > 0)
                {
                    // Debug logging handled by structured logging
                    // Verify the update by reading back the data
                    var verifyEntity = await GetByIdAsync(id);
                    // Debug logging handled by structured logging
                    return true;
                }
                else
                {
                    // Debug logging handled by structured logging
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
            }

            // Try direct SQL update with SqlSugar
            try
            {
                var modifyDate = DateTimeOffset.UtcNow;
                // Debug logging handled by structured logging
                var sqlResult = await db.Updateable<Onboarding>()
                    .SetColumns(x => new Onboarding
                    {
                        CompletionRate = completionRate,
                        ModifyDate = modifyDate
                    })
                    .Where(x => x.Id == id)
                    .ExecuteCommandAsync();
                // Debug logging handled by structured logging
                if (sqlResult > 0)
                {
                    // Debug logging handled by structured logging
                    // Verify the update by reading back the data
                    var verifyEntity = await GetByIdAsync(id);
                    // Debug logging handled by structured logging
                    return true;
                }
                else
                {
                    // Debug logging handled by structured logging
                }
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
            }

            // Fallback to entity update
            entity.CompletionRate = completionRate;
            entity.ModifyDate = DateTimeOffset.UtcNow;
            // Debug logging handled by structured logging
            var result = await UpdateAsync(entity);
            // Debug logging handled by structured logging
            return result;
        }

        /// <summary>
        /// Update status
        /// </summary>
        public async Task<bool> UpdateStatusAsync(long id, string status)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null || !entity.IsValid)
            {
                throw new CRMException(ErrorCodeEnum.DataNotFound, "Onboarding not found");
            }

            entity.Status = status;
            entity.ModifyDate = DateTimeOffset.UtcNow;

            // If completed, set actual completion date
            if (status == "Completed" && !entity.ActualCompletionDate.HasValue)
            {
                entity.ActualCompletionDate = DateTimeOffset.UtcNow;
                entity.CompletionRate = 100;
            }

            return await UpdateAsync(entity);
        }

        /// <summary>
        /// Get onboarding statistics
        /// </summary>
        public async Task<Dictionary<string, object>> GetStatisticsAsync()
        {
            var totalCount = await CountAsync(x => x.IsValid);
            var inProgressCount = await CountAsync(x => x.Status == "InProgress" && x.IsValid);
            var completedCount = await CountAsync(x => x.Status == "Completed" && x.IsValid);
            var pausedCount = await CountAsync(x => x.Status == "Paused" && x.IsValid);
            var cancelledCount = await CountAsync(x => x.Status == "Cancelled" && x.IsValid);

            var overdueCount = await CountAsync(x =>
                x.EstimatedCompletionDate.HasValue &&
                x.EstimatedCompletionDate.Value < DateTimeOffset.UtcNow &&
                x.Status != "Completed" &&
                x.IsValid);

            var allOnboardings = await GetListAsync(x => x.IsValid);
            var averageCompletionRate = allOnboardings.Any() ? allOnboardings.Average(x => x.CompletionRate) : 0;

            return new Dictionary<string, object>
            {
                { "TotalCount", totalCount },
                { "InProgressCount", inProgressCount },
                { "CompletedCount", completedCount },
                { "PausedCount", pausedCount },
                { "CancelledCount", cancelledCount },
                { "OverdueCount", overdueCount },
                { "AverageCompletionRate", averageCompletionRate }
            };
        }

        /// <summary>
        /// Batch update status
        /// </summary>
        public async Task<bool> BatchUpdateStatusAsync(List<long> ids, string status)
        {
            var entities = await GetListAsync(x => ids.Contains(x.Id) && x.IsValid);
            if (!entities.Any())
            {
                return false;
            }

            foreach (var entity in entities)
            {
                entity.Status = status;
                entity.ModifyDate = DateTimeOffset.UtcNow;

                if (status == "Completed" && !entity.ActualCompletionDate.HasValue)
                {
                    entity.ActualCompletionDate = DateTimeOffset.UtcNow;
                    entity.CompletionRate = 100;
                }
            }

            return await UpdateRangeAsync(entities);
        }

        /// <summary>
        /// Get overdue onboarding list
        /// </summary>
        public async Task<List<Onboarding>> GetOverdueListAsync()
        {
            return await GetListAsync(x =>
                x.EstimatedCompletionDate.HasValue &&
                x.EstimatedCompletionDate.Value < DateTimeOffset.UtcNow &&
                x.Status != "Completed" &&
                x.IsValid);
        }

        /// <summary>
        /// Get onboarding count by status
        /// </summary>
        public async Task<Dictionary<string, int>> GetCountByStatusAsync()
        {
            var allOnboardings = await GetListAsync(x => x.IsValid);

            return allOnboardings
                .GroupBy(x => x.Status)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}
