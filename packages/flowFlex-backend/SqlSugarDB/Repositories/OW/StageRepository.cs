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
    /// Stage repository implementation
    /// </summary>
    public class StageRepository : BaseRepository<Stage>, IStageRepository, IScopedService
    {
        public StageRepository(ISqlSugarClient sqlSugarClient) : base(sqlSugarClient)
        {
        }

        /// <summary>
        /// Get stage list by workflow ID
        /// </summary>
        public async Task<List<Stage>> GetByWorkflowIdAsync(long workflowId)
        {
            return await db.Queryable<Stage>()
                .Where(x => x.WorkflowId == workflowId && x.IsValid == true)
                .OrderBy(x => x.Order)
                .ToListAsync();
        }

        /// <summary>
        /// Paginated query of stages
        /// </summary>
        public async Task<(List<Stage> items, int total)> QueryPagedAsync(int pageIndex, int pageSize, long? workflowId = null, string name = null)
        {
            // Build query condition list
            var whereExpressions = new List<Expression<Func<Stage, bool>>>();

            // Basic filter conditions
            whereExpressions.Add(x => x.IsValid == true);

            if (workflowId.HasValue)
            {
                whereExpressions.Add(x => x.WorkflowId == workflowId.Value);
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                whereExpressions.Add(x => x.Name.ToLower().Contains(name.ToLower()));
            }

            // Use BaseRepository's safe pagination method
            var (items, total) = await GetPageListAsync(
                whereExpressions,
                pageIndex,
                pageSize,
                orderByExpression: x => x.Order, // Sort by Order
                isAsc: true // Ascending order
            );

            return (items, total);
        }

        /// <summary>
        /// Get maximum order number in workflow
        /// </summary>
        public async Task<int> GetMaxOrderByWorkflowIdAsync(long workflowId)
        {
            var maxOrder = await db.Queryable<Stage>()
                .Where(x => x.WorkflowId == workflowId && x.IsValid == true)
                .MaxAsync(x => (int?)x.Order);

            return maxOrder ?? 0;
        }

        /// <summary>
        /// Batch update stage order
        /// </summary>
        public async Task<bool> BatchUpdateOrderAsync(List<(long stageId, int order)> stageOrders)
        {
            try
            {
                await db.Ado.BeginTranAsync();

                foreach (var (stageId, order) in stageOrders)
                {
                    await db.Updateable<Stage>()
                        .SetColumns(x => x.Order == order)
                        .Where(x => x.Id == stageId && x.IsValid == true)
                        .ExecuteCommandAsync();
                }

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
        /// Get stages by workflow ID and order range
        /// </summary>
        public async Task<List<Stage>> GetByWorkflowIdAndOrderRangeAsync(long workflowId, int minOrder, int maxOrder)
        {
            return await db.Queryable<Stage>()
                .Where(x => x.WorkflowId == workflowId && x.IsValid == true && x.Order >= minOrder && x.Order <= maxOrder)
                .OrderBy(x => x.Order)
                .ToListAsync();
        }

        /// <summary>
        /// Delete multiple stages
        /// </summary>
        public async Task<bool> BatchDeleteAsync(List<long> stageIds)
        {
            var result = await db.Updateable<Stage>()
                .SetColumns(x => x.IsValid == false)
                .Where(x => stageIds.Contains(x.Id))
                .ExecuteCommandAsync();

            return result > 0;
        }

        /// <summary>
        /// Get next order number for stage
        /// </summary>
        public async Task<int> GetNextOrderAsync(long workflowId)
        {
            var maxOrder = await GetMaxOrderByWorkflowIdAsync(workflowId);
            return maxOrder + 1;
        }

        /// <summary>
        /// Get stage count by color
        /// </summary>
        public async Task<int> GetCountByColorAsync(string color)
        {
            return await db.Queryable<Stage>()
                .Where(x => x.Color == color && x.IsValid == true)
                .CountAsync();
        }

        /// <summary>
        /// Get active stages in workflow
        /// </summary>
        public async Task<List<Stage>> GetActiveStagesByWorkflowIdAsync(long workflowId)
        {
            return await db.Queryable<Stage>()
                .Where(x => x.WorkflowId == workflowId && x.IsActive == true && x.IsValid == true)
                .OrderBy(x => x.Order)
                .ToListAsync();
        }

        /// <summary>
        /// Batch update stage status
        /// </summary>
        public async Task<bool> BatchUpdateActiveStatusAsync(List<long> stageIds, bool isActive)
        {
            var result = await db.Updateable<Stage>()
                .SetColumns(x => x.IsActive == isActive)
                .Where(x => stageIds.Contains(x.Id) && x.IsValid == true)
                .ExecuteCommandAsync();

            return result > 0;
        }

        /// <summary>
        /// Get all valid stages (optimized version)
        /// </summary>
        public async Task<List<Stage>> GetAllOptimizedAsync()
        {
            return await db.Queryable<Stage>()
                .Where(x => x.IsValid == true)
                .OrderBy(x => x.WorkflowId, OrderByType.Asc)
                .OrderBy(x => x.Order, OrderByType.Asc)
                .ToListAsync();
        }

        /// <summary>
        /// Check if stage name exists in workflow
        /// </summary>
        public async Task<bool> ExistsNameInWorkflowAsync(long workflowId, string name, long? excludeId = null)
        {
            var query = db.Queryable<Stage>()
                .Where(x => x.WorkflowId == workflowId && x.Name == name && x.IsValid == true);

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// Check if stage name exists in workflow (renamed method)
        /// </summary>
        public async Task<bool> IsNameExistsInWorkflowAsync(long workflowId, string name, long? excludeId = null)
        {
            return await ExistsNameInWorkflowAsync(workflowId, name, excludeId);
        }

        /// <summary>
        /// Batch get stage information (optimized version)
        /// </summary>
        public async Task<List<Stage>> GetBatchByIdsAsync(List<long> stageIds)
        {
            if (!stageIds?.Any() == true)
                return new List<Stage>();

            return await db.Queryable<Stage>()
                .Where(x => stageIds.Contains(x.Id) && x.IsValid == true)
                .OrderBy(x => x.WorkflowId, OrderByType.Asc)
                .OrderBy(x => x.Order, OrderByType.Asc)
                .ToListAsync();
        }
    }
}
