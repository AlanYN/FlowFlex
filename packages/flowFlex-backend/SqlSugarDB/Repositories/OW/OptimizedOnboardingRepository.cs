using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using SqlSugar;
using System.Linq.Expressions;

namespace FlowFlex.SqlSugarDB.Repositories.OW
{
    /// <summary>
    /// Optimized Onboarding repository implementation
    /// </summary>
    public class OptimizedOnboardingRepository : IOnboardingRepository, IBaseRepository<Onboarding>, IScopedService
    {
        protected readonly ISqlSugarClient _db;
        protected readonly UserContext _userContext;

        public OptimizedOnboardingRepository(
            ISqlSugarClient db,
            UserContext userContext)
        {
            _db = db;
            _userContext = userContext;
        }

        #region IOnboardingRepository Implementation

        /// <summary>
        /// Batch get Onboarding by Lead IDs (optimized batch query)
        /// </summary>
        public async Task<List<Onboarding>> GetByLeadIdsAsync(List<string> leadIds)
        {
            if (leadIds?.Any() != true)
            {
                return new List<Onboarding>();
            }

            return await _db.Queryable<Onboarding>()
                .Where(x => leadIds.Contains(x.LeadId) && x.IsValid)
                .ToListAsync();
        }

        /// <summary>
        /// Check if Lead already has existing Onboarding
        /// </summary>
        public async Task<bool> ExistsByLeadIdAsync(string leadId)
        {
            return await _db.Queryable<Onboarding>()
                .Where(x => x.LeadId == leadId && x.IsValid)
                .AnyAsync();
        }

        /// <summary>
        /// Get next order number
        /// </summary>
        public async Task<int> GetNextOrderAsync(long workflowId)
        {
            var maxOrder = await _db.Queryable<Onboarding>()
                .Where(x => x.WorkflowId == workflowId && x.IsValid)
                .MaxAsync(x => (int?)x.CurrentStageOrder);
            return (maxOrder ?? 0) + 1;
        }

        /// <summary>
        /// Batch update status (optimized batch operation)
        /// </summary>
        public async Task<bool> BatchUpdateStatusAsync(List<long> ids, string status)
        {
            if (ids?.Any() != true)
                return false;

            var result = await _db.Updateable<Onboarding>()
                .SetColumns(x => new Onboarding { Status = status, ModifyDate = DateTimeOffset.Now })
                .Where(x => ids.Contains(x.Id) && x.IsValid)
                .ExecuteCommandAsync();

            return result > 0;
        }

        /// <summary>
        /// Get paged Onboarding list (with sorting and filtering)
        /// </summary>
        public async Task<PagedResult<Onboarding>> GetPagedOnboardingsAsync(
            int pageIndex,
            int pageSize,
            string? status = null,
            string? priority = null,
            string? searchKeyword = null)
        {
            var query = _db.Queryable<Onboarding>().Where(x => x.IsValid);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(x => x.Status == status);
            }

            if (!string.IsNullOrEmpty(priority))
            {
                query = query.Where(x => x.Priority == priority);
            }

            if (!string.IsNullOrEmpty(searchKeyword))
            {
                query = query.Where(x => x.LeadName.Contains(searchKeyword) || x.LeadId.Contains(searchKeyword));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.CreateDate)
                .ToPageListAsync(pageIndex, pageSize);

            return new PagedResult<Onboarding>
            {
                Items = items,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        #region Compatibility Methods - Maintain compatibility with original interface

        public async Task EnsureTableExistsAsync()
        {
            var tableExists = _db.DbMaintenance.IsAnyTable("ff_onboarding", false);
            if (!tableExists)
            {
                _db.CodeFirst.SetStringDefaultLength(200).InitTables<Onboarding>();
            }
        }

        public ISqlSugarClient GetSqlSugarClient() => _db;

        public async Task<List<Onboarding>> GetListByWorkflowIdAsync(long workflowId)
        {
            return await _db.Queryable<Onboarding>()
                .Where(x => x.WorkflowId == workflowId && x.IsValid)
                .ToListAsync();
        }

        public async Task<List<Onboarding>> GetListByStageIdAsync(long stageId)
        {
            return await _db.Queryable<Onboarding>()
                .Where(x => x.CurrentStageId == stageId && x.IsValid)
                .ToListAsync();
        }

        public async Task<List<Onboarding>> GetListByStatusAsync(string status)
        {
            return await _db.Queryable<Onboarding>()
                .Where(x => x.Status == status && x.IsValid)
                .ToListAsync();
        }

        public async Task<List<Onboarding>> GetListByAssigneeIdAsync(long assigneeId)
        {
            return await _db.Queryable<Onboarding>()
                .Where(x => x.CurrentAssigneeId == assigneeId && x.IsValid)
                .ToListAsync();
        }

        public async Task<bool> UpdateStageAsync(long id, long stageId, int stageOrder)
        {
            var result = await _db.Updateable<Onboarding>()
                .SetColumns(x => new Onboarding
                {
                    CurrentStageId = stageId,
                    CurrentStageOrder = stageOrder,
                    ModifyDate = DateTimeOffset.Now
                })
                .Where(x => x.Id == id)
                .ExecuteCommandAsync();

            return result > 0;
        }

        public async Task<bool> UpdateCompletionRateAsync(long id, decimal completionRate)
        {
            var result = await _db.Updateable<Onboarding>()
                .SetColumns(x => new Onboarding
                {
                    CompletionRate = completionRate,
                    ModifyDate = DateTimeOffset.Now
                })
                .Where(x => x.Id == id)
                .ExecuteCommandAsync();

            return result > 0;
        }

        public async Task<bool> UpdateStatusAsync(long id, string status)
        {
            var result = await _db.Updateable<Onboarding>()
                .SetColumns(x => new Onboarding
                {
                    Status = status,
                    ModifyDate = DateTimeOffset.Now
                })
                .Where(x => x.Id == id)
                .ExecuteCommandAsync();

            return result > 0;
        }

        public async Task<List<Onboarding>> GetOverdueListAsync()
        {
            return await _db.Queryable<Onboarding>()
                .Where(x => x.IsValid && x.IsActive && x.EstimatedCompletionDate.HasValue && x.EstimatedCompletionDate.Value < DateTimeOffset.Now)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetCountByStatusAsync()
        {
            var result = await _db.Queryable<Onboarding>()
                .Where(x => x.IsValid)
                .GroupBy(x => x.Status)
                .Select(x => new { Status = x.Status, Count = SqlFunc.AggregateCount(x.Id) })
                .ToListAsync();

            return result.ToDictionary(x => x.Status, x => x.Count);
        }

        /// <summary>
        /// 直接查询，使用显式过滤条件
        /// </summary>
        public async Task<List<Onboarding>> GetListWithExplicitFiltersAsync(string tenantId, string appCode)
        {
            // 临时禁用全局过滤器
            _db.QueryFilter.ClearAndBackup();
            
            try
            {
                // 使用显式过滤条件
                var query = _db.Queryable<Onboarding>()
                    .Where(x => x.IsValid == true)
                    .Where(x => x.TenantId == tenantId && x.AppCode == appCode);
                
                // 执行查询
                var result = await query.OrderByDescending(x => x.CreateDate).ToListAsync();
                
                return result;
            }
            finally
            {
                // 恢复全局过滤器
                _db.QueryFilter.Restore();
            }
        }

        public async Task<Dictionary<string, object>> GetStatisticsAsync()
        {
            var totalCount = await _db.Queryable<Onboarding>()
                .Where(x => x.IsValid)
                .CountAsync();

            var activeCount = await _db.Queryable<Onboarding>()
                .Where(x => x.IsValid && x.Status == "Active")
                .CountAsync();

            var completedCount = await _db.Queryable<Onboarding>()
                .Where(x => x.IsValid && x.Status == "Completed")
                .CountAsync();

            var averageCompletionRate = await _db.Queryable<Onboarding>()
                .Where(x => x.IsValid)
                .AvgAsync(x => x.CompletionRate);

            return new Dictionary<string, object>
            {
                ["TotalCount"] = totalCount,
                ["ActiveCount"] = activeCount,
                ["CompletedCount"] = completedCount,
                ["AverageCompletionRate"] = averageCompletionRate
            };
        }

        #endregion

        #endregion

        #region IBaseRepository<Onboarding> Implementation

        public async Task<bool> InsertableByObjectAsync(object entity)
        {
            var result = await _db.Insertable(entity).ExecuteCommandAsync();
            return result > 0;
        }

        public bool Insert(Onboarding insertObj)
        {
            var result = _db.Insertable(insertObj).ExecuteCommand();
            return result > 0;
        }

        public bool InsertOrUpdate(Onboarding data)
        {
            var result = _db.Storageable(data).ExecuteCommand();
            return result > 0;
        }

        public bool InsertOrUpdate(List<Onboarding> datas)
        {
            var result = _db.Storageable(datas).ExecuteCommand();
            return result > 0;
        }

        public int InsertReturnIdentity(Onboarding insertObj)
        {
            return _db.Insertable(insertObj).ExecuteReturnIdentity();
        }

        public long InsertReturnBigIdentity(Onboarding insertObj)
        {
            return _db.Insertable(insertObj).ExecuteReturnBigIdentity();
        }

        public long InsertReturnSnowflakeId(Onboarding insertObj)
        {
            return _db.Insertable(insertObj).ExecuteReturnSnowflakeId();
        }

        public List<long> InsertReturnSnowflakeId(List<Onboarding> insertObjs)
        {
            return _db.Insertable(insertObjs).ExecuteReturnSnowflakeIdList();
        }

        public Onboarding InsertReturnEntity(Onboarding insertObj)
        {
            return _db.Insertable(insertObj).ExecuteReturnEntity();
        }

        public bool InsertRange(Onboarding[] insertObjs)
        {
            var result = _db.Insertable(insertObjs).ExecuteCommand();
            return result > 0;
        }

        public bool InsertRange(List<Onboarding> insertObjs)
        {
            var result = _db.Insertable(insertObjs).ExecuteCommand();
            return result > 0;
        }

        public async Task<long> InsertReturnSnowflakeIdAsync(Onboarding insertObj, CancellationToken cancellationToken = default)
        {
            return await _db.Insertable(insertObj).ExecuteReturnSnowflakeIdAsync();
        }

        public async Task<List<long>> InsertReturnSnowflakeIdAsync(List<Onboarding> insertObjs, CancellationToken cancellationToken = default)
        {
            return await _db.Insertable(insertObjs).ExecuteReturnSnowflakeIdListAsync();
        }

        public async Task<bool> InsertOrUpdateAsync(Onboarding data, CancellationToken cancellationToken = default)
        {
            var result = await _db.Storageable(data).ExecuteCommandAsync();
            return result > 0;
        }

        public async Task<bool> InsertOrUpdateAsync(List<Onboarding> datas, CancellationToken cancellationToken = default)
        {
            var result = await _db.Storageable(datas).ExecuteCommandAsync();
            return result > 0;
        }

        public async Task<bool> InsertAsync(Onboarding insertObj, CancellationToken cancellationToken = default, bool copyNew = false)
        {
            var result = await _db.Insertable(insertObj).ExecuteCommandAsync();
            return result > 0;
        }

        public async Task<int> InsertReturnIdentityAsync(Onboarding insertObj, CancellationToken cancellationToken = default)
        {
            return await _db.Insertable(insertObj).ExecuteReturnIdentityAsync();
        }

        public async Task<long> InsertReturnBigIdentityAsync(Onboarding insertObj, CancellationToken cancellationToken = default)
        {
            return await _db.Insertable(insertObj).ExecuteReturnBigIdentityAsync();
        }

        public async Task<Onboarding> InsertReturnEntityAsync(Onboarding insertObj, CancellationToken cancellationToken = default)
        {
            return await _db.Insertable(insertObj).ExecuteReturnEntityAsync();
        }

        public async Task<bool> InsertRangeAsync(Onboarding[] insertObjs, CancellationToken cancellationToken = default)
        {
            var result = await _db.Insertable(insertObjs).ExecuteCommandAsync();
            return result > 0;
        }

        public async Task<bool> InsertRangeAsync(List<Onboarding> insertObjs, CancellationToken cancellationToken = default)
        {
            var result = await _db.Insertable(insertObjs).ExecuteCommandAsync();
            return result > 0;
        }

        public bool Update(Onboarding updateObj)
        {
            var result = _db.Updateable(updateObj).ExecuteCommand();
            return result > 0;
        }

        public bool UpdateRange(Onboarding[] updateObjs)
        {
            var result = _db.Updateable(updateObjs).ExecuteCommand();
            return result > 0;
        }

        public bool UpdateRange(List<Onboarding> updateObjs)
        {
            var result = _db.Updateable(updateObjs).ExecuteCommand();
            return result > 0;
        }

        public bool Update(Expression<Func<Onboarding, Onboarding>> columns, Expression<Func<Onboarding, bool>> whereExpression)
        {
            var result = _db.Updateable<Onboarding>().SetColumns(columns).Where(whereExpression).ExecuteCommand();
            return result > 0;
        }

        public bool UpdateSetColumnsTrue(Expression<Func<Onboarding, Onboarding>> columns, Expression<Func<Onboarding, bool>> whereExpression)
        {
            var result = _db.Updateable<Onboarding>().SetColumns(columns).Where(whereExpression).ExecuteCommand();
            return result > 0;
        }

        public async Task<bool> UpdateAsync(Onboarding updateObj, Expression<Func<Onboarding, object>>? updateColumns = null,
            CancellationToken cancellationToken = default, bool copyNew = false)
        {
            var updateable = _db.Updateable(updateObj);
            if (updateColumns != null)
            {
                updateable = updateable.UpdateColumns(updateColumns);
            }
            var result = await updateable.ExecuteCommandAsync();
            return result > 0;
        }

        public async Task<bool> UpdateRangeAsync(Onboarding[] updateObjs, Expression<Func<Onboarding, object>>? updateColumns = null,
            CancellationToken cancellationToken = default)
        {
            var updateable = _db.Updateable(updateObjs);
            if (updateColumns != null)
            {
                updateable = updateable.UpdateColumns(updateColumns);
            }
            var result = await updateable.ExecuteCommandAsync();
            return result > 0;
        }

        public async Task<bool> UpdateRangeAsync(List<Onboarding> updateObjs, Expression<Func<Onboarding, object>>? updateColumns = null,
            CancellationToken cancellationToken = default)
        {
            var updateable = _db.Updateable(updateObjs);
            if (updateColumns != null)
            {
                updateable = updateable.UpdateColumns(updateColumns);
            }
            var result = await updateable.ExecuteCommandAsync();
            return result > 0;
        }

        public async Task<bool> UpdateAsync(Expression<Func<Onboarding, Onboarding>> columns, Expression<Func<Onboarding, bool>> whereExpression, CancellationToken cancellationToken = default)
        {
            var result = await _db.Updateable<Onboarding>().SetColumns(columns).Where(whereExpression).ExecuteCommandAsync();
            return result > 0;
        }

        public async Task<bool> UpdateSetColumnsTrueAsync(Expression<Func<Onboarding, Onboarding>> columns, Expression<Func<Onboarding, bool>> whereExpression, CancellationToken cancellationToken = default)
        {
            var result = await _db.Updateable<Onboarding>().SetColumns(columns).Where(whereExpression).ExecuteCommandAsync();
            return result > 0;
        }

        public async Task<bool> UpdateAsync(Dictionary<string, object> dict)
        {
            var result = await _db.Updateable<Onboarding>(dict).ExecuteCommandAsync();
            return result > 0;
        }

        public Onboarding GetById(object id, bool copyNew = false)
        {
            return _db.Queryable<Onboarding>().InSingle(id);
        }

        public async Task<Onboarding> GetByIdAsync(object id, bool copyNew = false, CancellationToken cancellationToken = default)
        {
            return await _db.Queryable<Onboarding>().InSingleAsync(id);
        }

        public List<Onboarding> GetList()
        {
            return _db.Queryable<Onboarding>().ToList();
        }

        public List<Onboarding> GetList(Expression<Func<Onboarding, bool>> whereExpression, bool copyNew = false)
        {
            return _db.Queryable<Onboarding>().Where(whereExpression).ToList();
        }

        public Onboarding GetSingle(Expression<Func<Onboarding, bool>> whereExpression)
        {
            return _db.Queryable<Onboarding>().Single(whereExpression);
        }

        public Onboarding GetFirst(Expression<Func<Onboarding, bool>> whereExpression, bool copyNew = false)
        {
            return _db.Queryable<Onboarding>().First(whereExpression);
        }

        public async Task<List<Onboarding>> GetListAsync(CancellationToken cancellationToken = default, bool copyNew = false)
        {
            return await _db.Queryable<Onboarding>().ToListAsync();
        }

        public async Task<List<Onboarding>> GetListAsync(Expression<Func<Onboarding, bool>> whereExpression, CancellationToken cancellationToken = default, bool copyNew = false)
        {
            return await _db.Queryable<Onboarding>().Where(whereExpression).ToListAsync();
        }

        public async Task<List<Onboarding>> GetListAsync(Expression<Func<Onboarding, bool>> whereExpression, Expression<Func<Onboarding, object>> orderExpression, OrderByType orderByType = OrderByType.Asc, int top = 0, CancellationToken cancellationToken = default, bool copyNew = false)
        {
            var query = _db.Queryable<Onboarding>().Where(whereExpression);

            if (orderByType == OrderByType.Asc)
                query = query.OrderBy(orderExpression);
            else
                query = query.OrderByDescending(orderExpression);

            if (top > 0)
                query = query.Take(top);

            return await query.ToListAsync();
        }

        public async Task<List<Onboarding>> GetListAsync(Expression<Func<Onboarding, bool>> whereExpression, Expression<Func<Onboarding, Onboarding>> selectedColumnExpression, Expression<Func<Onboarding, object>> orderExpression = null, OrderByType orderByType = OrderByType.Asc, CancellationToken cancellationToken = default)
        {
            var query = _db.Queryable<Onboarding>().Where(whereExpression).Select(selectedColumnExpression);

            if (orderExpression != null)
            {
                if (orderByType == OrderByType.Asc)
                    query = query.OrderBy(orderExpression);
                else
                    query = query.OrderByDescending(orderExpression);
            }

            return await query.ToListAsync();
        }

        public async Task<Onboarding> GetSingleAsync(Expression<Func<Onboarding, bool>> whereExpression, CancellationToken cancellationToken = default)
        {
            return await _db.Queryable<Onboarding>().SingleAsync(whereExpression);
        }

        public async Task<Onboarding> GetFirstAsync(Expression<Func<Onboarding, bool>> whereExpression, CancellationToken cancellationToken = default, bool copyNew = false)
        {
            return await _db.Queryable<Onboarding>().FirstAsync(whereExpression);
        }

        public List<Onboarding> GetPageList(Expression<Func<Onboarding, bool>> whereExpression, int pageIndex, int pageSize, out int TotalCount, Expression<Func<Onboarding, object>> orderByExpression = null, bool isAsc = true, Expression<Func<Onboarding, Onboarding>> selectedColumnExpression = null)
        {
            var query = _db.Queryable<Onboarding>().Where(whereExpression);

            if (selectedColumnExpression != null)
                query = query.Select(selectedColumnExpression);

            if (orderByExpression != null)
            {
                if (isAsc)
                    query = query.OrderBy(orderByExpression);
                else
                    query = query.OrderByDescending(orderByExpression);
            }

            TotalCount = 0;
            return query.ToPageList(pageIndex, pageSize, ref TotalCount);
        }

        public async Task<(List<Onboarding> datas, int total)> GetPageListAsync(Expression<Func<Onboarding, bool>> whereExpression, int pageIndex, int pageSize, Expression<Func<Onboarding, object>> orderByExpression = null, bool isAsc = true, Expression<Func<Onboarding, Onboarding>> selectedColumnExpression = null, CancellationToken cancellationToken = default)
        {
            var query = _db.Queryable<Onboarding>().Where(whereExpression);

            if (selectedColumnExpression != null)
                query = query.Select(selectedColumnExpression);

            if (orderByExpression != null)
            {
                if (isAsc)
                    query = query.OrderBy(orderByExpression);
                else
                    query = query.OrderByDescending(orderByExpression);
            }

            var total = await query.CountAsync();
            var datas = await query.ToPageListAsync(pageIndex, pageSize);

            return (datas, total);
        }

        public List<Onboarding> GetPageList(List<Expression<Func<Onboarding, bool>>> whereExpressionList, int pageIndex, int pageSize, out int TotalCount, Expression<Func<Onboarding, object>> orderByExpression = null, bool isAsc = true, Expression<Func<Onboarding, Onboarding>> selectedColumnExpression = null)
        {
            var query = _db.Queryable<Onboarding>();

            foreach (var whereExpression in whereExpressionList)
            {
                query = query.Where(whereExpression);
            }

            if (selectedColumnExpression != null)
                query = query.Select(selectedColumnExpression);

            if (orderByExpression != null)
            {
                if (isAsc)
                    query = query.OrderBy(orderByExpression);
                else
                    query = query.OrderByDescending(orderByExpression);
            }

            TotalCount = 0;
            return query.ToPageList(pageIndex, pageSize, ref TotalCount);
        }

        public async Task<(List<Onboarding> datas, int total)> GetPageListAsync(List<Expression<Func<Onboarding, bool>>> whereExpressionList, int pageIndex, int pageSize, Expression<Func<Onboarding, object>> orderByExpression = null, bool isAsc = true, Expression<Func<Onboarding, Onboarding>> selectedColumnExpression = null, CancellationToken cancellationToken = default)
        {
            var query = _db.Queryable<Onboarding>();

            foreach (var whereExpression in whereExpressionList)
            {
                query = query.Where(whereExpression);
            }

            if (selectedColumnExpression != null)
                query = query.Select(selectedColumnExpression);

            if (orderByExpression != null)
            {
                if (isAsc)
                    query = query.OrderBy(orderByExpression);
                else
                    query = query.OrderByDescending(orderByExpression);
            }

            var total = await query.CountAsync();
            var datas = await query.ToPageListAsync(pageIndex, pageSize);

            return (datas, total);
        }

        public bool Delete(Onboarding deleteObj)
        {
            var result = _db.Deleteable(deleteObj).ExecuteCommand();
            return result > 0;
        }

        public bool Delete(List<Onboarding> deleteObjs)
        {
            var result = _db.Deleteable(deleteObjs).ExecuteCommand();
            return result > 0;
        }

        public bool Delete(Expression<Func<Onboarding, bool>> whereExpression)
        {
            var result = _db.Deleteable<Onboarding>().Where(whereExpression).ExecuteCommand();
            return result > 0;
        }

        public bool DeleteById(object id)
        {
            var result = _db.Deleteable<Onboarding>().In(id).ExecuteCommand();
            return result > 0;
        }

        public async Task<bool> DeleteAsync(Onboarding deleteObj, CancellationToken cancellationToken = default)
        {
            var result = await _db.Deleteable(deleteObj).ExecuteCommandAsync();
            return result > 0;
        }

        public async Task<bool> DeleteAsync(List<Onboarding> deleteObjs, CancellationToken cancellationToken = default)
        {
            var result = await _db.Deleteable(deleteObjs).ExecuteCommandAsync();
            return result > 0;
        }

        public async Task<bool> DeleteAsync(Expression<Func<Onboarding, bool>> whereExpression, CancellationToken cancellationToken = default)
        {
            var result = await _db.Deleteable<Onboarding>().Where(whereExpression).ExecuteCommandAsync();
            return result > 0;
        }

        public async Task<bool> DeleteByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            var result = await _db.Deleteable<Onboarding>().In(id).ExecuteCommandAsync();
            return result > 0;
        }

        public async Task<bool> DeleteByIdsAsync(dynamic[] ids, CancellationToken cancellationToken = default)
        {
            var result = await _db.Deleteable<Onboarding>().In(ids).ExecuteCommandAsync();
            return result > 0;
        }

        public bool IsAny(Expression<Func<Onboarding, bool>> whereExpression)
        {
            return _db.Queryable<Onboarding>().Where(whereExpression).Any();
        }

        public async Task<bool> IsAnyAsync(Expression<Func<Onboarding, bool>> whereExpression, CancellationToken cancellationToken = default, bool copyNew = false)
        {
            return await _db.Queryable<Onboarding>().Where(whereExpression).AnyAsync();
        }

        public int Count(Expression<Func<Onboarding, bool>> whereExpression)
        {
            return _db.Queryable<Onboarding>().Where(whereExpression).Count();
        }

        public async Task<int> CountAsync(Expression<Func<Onboarding, bool>> whereExpression, CancellationToken cancellationToken = default)
        {
            return await _db.Queryable<Onboarding>().Where(whereExpression).CountAsync();
        }

        public void BeginTran()
        {
            _db.Ado.BeginTran();
        }

        public void CommitTran()
        {
            _db.Ado.CommitTran();
        }

        public void RollbackTran()
        {
            _db.Ado.RollbackTran();
        }

        public bool UseTran(Action action)
        {
            var result = _db.Ado.UseTran(action);
            return result.IsSuccess;
        }

        public async Task<bool> UseTranAsync(Func<Task> action)
        {
            var result = await _db.Ado.UseTranAsync(action);
            return result.IsSuccess;
        }

        public (ReturnValueT result, bool success) UseTranReturnValue<ReturnValueT>(Func<ReturnValueT> action)
        {
            var result = _db.Ado.UseTran(action);
            return (result.Data, result.IsSuccess);
        }

        public async Task<(ReturnValueT result, bool success)> UseTranReturnValueAsync<ReturnValueT>(Func<Task<ReturnValueT>> action)
        {
            var result = await _db.Ado.UseTranAsync(action);
            return (result.Data, result.IsSuccess);
        }

        public void QueryFilterClearAndBackup()
        {
            _db.QueryFilter.ClearAndBackup();
        }

        public void QueryFilterClear()
        {
            _db.QueryFilter.Clear();
        }

        public void QueryFilterRestore()
        {
            _db.QueryFilter.Restore();
        }

        public ISugarQueryable<Onboarding> ClearFilter()
        {
            return _db.Queryable<Onboarding>().ClearFilter();
        }

        public ISugarQueryable<Onboarding> ClearFilter<IFilter>()
        {
            return _db.Queryable<Onboarding>().ClearFilter<IFilter>();
        }

        public SugarUnitOfWork CreateContext()
        {
            return _db.CreateContext();
        }

        #endregion
    }
}