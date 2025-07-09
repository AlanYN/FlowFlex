using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Linq;
using System.Linq.Expressions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// Workflow repository implementation
    /// </summary>
    public class WorkflowRepository : BaseRepository<Workflow>, IWorkflowRepository, IScopedService
    {
        public WorkflowRepository(ISqlSugarClient sqlSugarClient) : base(sqlSugarClient)
        {
        }

        /// <summary>
        /// Paginated query of workflows
        /// </summary>
        public async Task<(List<Workflow> items, int total)> QueryPagedAsync(int pageIndex, int pageSize, string name = null, bool? isActive = null)
        {
            // Build query condition list
            var whereExpressions = new List<Expression<Func<Workflow, bool>>>();

            // Basic filter conditions
            whereExpressions.Add(x => x.IsValid == true);

            if (!string.IsNullOrWhiteSpace(name))
            {
                whereExpressions.Add(x => x.Name.ToLower().Contains(name.ToLower()));
            }

            if (isActive.HasValue)
            {
                whereExpressions.Add(x => x.IsActive == isActive.Value);
            }

            // Use BaseRepository's safe pagination method
            var (items, total) = await GetPageListAsync(
                whereExpressions,
                pageIndex,
                pageSize,
                orderByExpression: x => x.CreateDate,
                isAsc: false // Descending order
            );

            return (items, total);
        }

        /// <summary>
        /// Query workflow by name
        /// </summary>
        public async Task<Workflow> GetByNameAsync(string name)
        {
            return await db.Queryable<Workflow>()
                .Where(x => x.Name == name && x.IsValid == true)
                .FirstAsync();
        }

        /// <summary>
        /// Get default workflow
        /// </summary>
        public async Task<Workflow> GetDefaultWorkflowAsync()
        {
            return await db.Queryable<Workflow>()
                .Where(x => x.IsDefault == true && x.IsValid == true)
                .FirstAsync();
        }

        /// <summary>
        /// Set default workflow (cancel other default states)
        /// </summary>
        public async Task<bool> SetDefaultWorkflowAsync(long workflowId)
        {
            try
            {
                await db.Ado.BeginTranAsync();

                // Cancel default state of all other workflows
                await db.Updateable<Workflow>()
                    .SetColumns(x => x.IsDefault == false)
                    .Where(x => x.IsValid == true && x.Id != workflowId)
                    .ExecuteCommandAsync();

                // Set specified workflow as default
                await db.Updateable<Workflow>()
                    .SetColumns(x => x.IsDefault == true)
                    .Where(x => x.Id == workflowId && x.IsValid == true)
                    .ExecuteCommandAsync();

                await db.Ado.CommitTranAsync();
                return true;
            }
            catch
            {
                await db.Ado.RollbackTranAsync();
                throw;
            }
        }

        /// <summary>
        /// Remove default workflow
        /// </summary>
        public async Task<bool> RemoveDefaultWorkflowAsync(long workflowId)
        {
            var result = await db.Updateable<Workflow>()
                .SetColumns(x => x.IsDefault == false)
                .Where(x => x.Id == workflowId && x.IsValid == true)
                .ExecuteCommandAsync();

            return result > 0;
        }

        /// <summary>
        /// Get workflow with stages
        /// </summary>
        public async Task<Workflow> GetWithStagesAsync(long id)
        {
            var workflow = await db.Queryable<Workflow>()
                .Where(x => x.Id == id && x.IsValid == true)
                .FirstAsync();

            if (workflow != null)
            {
                workflow.Stages = await db.Queryable<Stage>()
                    .Where(x => x.WorkflowId == id && x.IsValid == true)
                    .OrderBy(x => x.Order)
                    .ToListAsync();
            }

            return workflow;
        }

        /// <summary>
        /// Check if workflow name exists
        /// </summary>
        public async Task<bool> ExistsNameAsync(string name, long? excludeId = null)
        {
            var query = db.Queryable<Workflow>()
                .Where(x => x.Name == name && x.IsValid == true);

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// Batch update workflow status
        /// </summary>
        public async Task<bool> BatchUpdateStatusAsync(List<long> ids, string status)
        {
            var result = await db.Updateable<Workflow>()
                .SetColumns(x => x.Status == status)
                .Where(x => ids.Contains(x.Id) && x.IsValid == true)
                .ExecuteCommandAsync();

            return result > 0;
        }

        /// <summary>
        /// Get active workflow list
        /// </summary>
        public async Task<List<Workflow>> GetActiveWorkflowsAsync()
        {
            return await db.Queryable<Workflow>()
                .Where(x => x.IsActive == true && x.IsValid == true)
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Get all workflow list (including stage information)
        /// </summary>
        public async Task<List<Workflow>> GetAllWorkflowsAsync()
        {
            return await db.Queryable<Workflow>()
                .Where(x => x.IsValid == true)
                .Includes(x => x.Stages.Where(s => s.IsValid == true).ToList())
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get all valid workflows (optimized version)
        /// </summary>
        public async Task<List<Workflow>> GetAllOptimizedAsync()
        {
            return await db.Queryable<Workflow>()
                .Where(x => x.IsValid == true)
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get expired workflows that are still active
        /// </summary>
        public async Task<List<Workflow>> GetExpiredActiveWorkflowsAsync()
        {
            var currentDate = DateTimeOffset.UtcNow;

            return await db.Queryable<Workflow>()
                .Where(x => x.IsValid == true
                    && x.IsActive == true
                    && x.EndDate.HasValue
                    && x.EndDate.Value < currentDate)
                .OrderBy(x => x.EndDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get workflows expiring soon (advance reminder by specified days)
        /// </summary>
        public async Task<List<Workflow>> GetExpiringWorkflowsAsync(int daysAhead = 7)
        {
            var currentDate = DateTimeOffset.UtcNow;
            var futureDate = currentDate.AddDays(daysAhead);

            return await db.Queryable<Workflow>()
                .Where(x => x.IsValid == true
                    && x.IsActive == true
                    && x.EndDate.HasValue
                    && x.EndDate.Value >= currentDate
                    && x.EndDate.Value <= futureDate)
                .OrderBy(x => x.EndDate)
                .ToListAsync();
        }

        /// <summary>
        /// Batch get workflow information (optimized version)
        /// </summary>
        public async Task<List<Workflow>> GetBatchByIdsAsync(List<long> workflowIds)
        {
            if (!workflowIds?.Any() == true)
                return new List<Workflow>();

            return await db.Queryable<Workflow>()
                .Where(x => workflowIds.Contains(x.Id) && x.IsValid == true)
                .OrderByDescending(x => x.CreateDate)
                .ToListAsync();
        }
    }
}
