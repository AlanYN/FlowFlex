using SqlSugar;

using System.Linq.Expressions;

namespace FlowFlex.Domain.Repository
{
    /// <summary>
    /// Data repository common interface
    /// </summary>
    public interface IBaseRepository<T> where T : class, new()
    {
        #region Insert

        Task<bool> InsertableByObjectAsync(object entity);

        /// <summary>
        /// Insert data
        /// </summary>
        /// <returns>Whether successful</returns>
        bool Insert(T insertObj);

        /// <summary>
        /// Insert or update data
        /// </summary>
        /// <returns>Whether successful</returns>
        bool InsertOrUpdate(T data);

        /// <summary>
        /// Insert or update data list
        /// </summary>
        /// <returns>Whether successful</returns>
        bool InsertOrUpdate(List<T> datas);

        /// <summary>
        /// Insert data and return auto-increment ID
        /// </summary>
        /// <returns>Return value</returns>
        int InsertReturnIdentity(T insertObj);

        /// <summary>
        /// Insert data and return big integer auto-increment ID
        /// </summary>
        /// <returns>Return value</returns>
        long InsertReturnBigIdentity(T insertObj);

        /// <summary>
        /// Insert data and return snowflake ID
        /// </summary>
        /// <returns>Return value</returns>
        long InsertReturnSnowflakeId(T insertObj);

        /// <summary>
        /// Insert data list and return snowflake ID list
        /// </summary>
        /// <returns>Return value</returns>
        List<long> InsertReturnSnowflakeId(List<T> insertObjs);

        /// <summary>
        /// Insert data and return entity
        /// </summary>
        /// <returns>Return value</returns>
        T InsertReturnEntity(T insertObj);

        /// <summary>
        /// Insert data array
        /// </summary>
        /// <returns>Whether successful</returns>
        bool InsertRange(T[] insertObjs);

        /// <summary>
        /// Insert data list
        /// </summary>
        /// <returns>Whether successful</returns>
        bool InsertRange(List<T> insertObjs);

        /// <summary>
        /// Insert data and return snowflake ID (async)
        /// </summary>
        /// <returns>Return value</returns>
        Task<long> InsertReturnSnowflakeIdAsync(T insertObj, CancellationToken cancellationToken = default);

        /// <summary>
        /// Insert data and return snowflake ID (async)
        /// </summary>
        /// <returns>Snowflake ID list</returns>
        Task<List<long>> InsertReturnSnowflakeIdAsync(List<T> insertObjs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Insert or update data (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        Task<bool> InsertOrUpdateAsync(T data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Batch insert or update data (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        Task<bool> InsertOrUpdateAsync(List<T> datas, CancellationToken cancellationToken = default);

        /// <summary>
        /// Insert data (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        Task<bool> InsertAsync(T insertObj, CancellationToken cancellationToken = default, bool copyNew = false);

        /// <summary>
        /// Insert data and return identity (async)
        /// </summary>
        /// <returns>Identity</returns>
        Task<int> InsertReturnIdentityAsync(T insertObj, CancellationToken cancellationToken = default);

        /// <summary>
        /// Insert data and return big identity (async)
        /// </summary>
        /// <returns>Big identity</returns>
        Task<long> InsertReturnBigIdentityAsync(T insertObj, CancellationToken cancellationToken = default);

        /// <summary>
        /// Insert data and return entity (async)
        /// </summary>
        /// <returns>Return value</returns>
        Task<T> InsertReturnEntityAsync(T insertObj, CancellationToken cancellationToken = default);

        /// <summary>
        /// Batch insert data (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        Task<bool> InsertRangeAsync(T[] insertObjs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Batch insert data (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        Task<bool> InsertRangeAsync(List<T> insertObjs, CancellationToken cancellationToken = default);

        #endregion Insert

        #region Update

        /// <summary>
        /// Update data
        /// </summary>
        /// <returns>Whether successful</returns>
        bool Update(T updateObj);

        /// <summary>
        /// Update data array
        /// </summary>
        /// <returns>Whether successful</returns>
        bool UpdateRange(T[] updateObjs);

        /// <summary>
        /// Update data list
        /// </summary>
        /// <returns>Whether successful</returns>
        bool UpdateRange(List<T> updateObjs);

        /// <summary>
        /// Update data by condition
        /// </summary>
        /// <returns>Whether successful</returns>
        bool Update(Expression<Func<T, T>> columns, Expression<Func<T, bool>> whereExpression);

        /// <summary>
        /// Update data by condition and set columns to true
        /// </summary>
        /// <returns>Whether successful</returns>
        bool UpdateSetColumnsTrue(Expression<Func<T, T>> columns, Expression<Func<T, bool>> whereExpression);

        /// <summary>
        /// Update data (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        Task<bool> UpdateAsync(T updateObj, Expression<Func<T, object>>? updateColumns = null,
            CancellationToken cancellationToken = default, bool copyNew = false);

        /// <summary>
        /// Batch update data (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        Task<bool> UpdateRangeAsync(T[] updateObjs, Expression<Func<T, object>>? updateColumns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Batch update data (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        Task<bool> UpdateRangeAsync(List<T> updateObjs, Expression<Func<T, object>>? updateColumns = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Update data by condition (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        Task<bool> UpdateAsync(Expression<Func<T, T>> columns, Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update data by condition and set columns to true (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        Task<bool> UpdateSetColumnsTrueAsync(Expression<Func<T, T>> columns, Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(Dictionary<string, object> dict);

        #endregion Update

        #region Query

        /// <summary>
        /// Get data by ID
        /// </summary>
        /// <returns>Return value</returns>
        T GetById(object id, bool copyNew = false);

        /// <summary>
        /// Get data by ID
        /// </summary>
        /// <returns>Return value</returns>
        Task<T> GetByIdAsync(object id, bool copyNew = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all data
        /// </summary>
        /// <returns>Return value</returns>
        List<T> GetList();

        /// <summary>
        /// Get data list by condition
        /// </summary>
        /// <returns>Return value</returns>
        List<T> GetList(Expression<Func<T, bool>> whereExpression, bool copyNew = false);

        /// <summary>
        /// Get single data by condition
        /// </summary>
        /// <returns>Return value</returns>
        T GetSingle(Expression<Func<T, bool>> whereExpression);

        /// <summary>
        /// Get first data by condition
        /// </summary>
        /// <returns>Return value</returns>
        T GetFirst(Expression<Func<T, bool>> whereExpression, bool copyNew = false);

        /// <summary>
        /// Get all data (async)
        /// </summary>
        /// <returns>Return value</returns>
        Task<List<T>> GetListAsync(CancellationToken cancellationToken = default, bool copyNew = false);

        /// <summary>
        /// Get data list by condition (async)
        /// </summary>
        /// <returns>Return value</returns>
        Task<List<T>> GetListAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default, bool copyNew = false);

        /// <summary>
        /// Get data list by condition (async)
        /// </summary>
        /// <returns>Return value</returns>
        Task<List<T>> GetListAsync(Expression<Func<T, bool>> whereExpression, Expression<Func<T, object>> orderExpression, OrderByType orderByType = OrderByType.Asc, int top = 0, CancellationToken cancellationToken = default, bool copyNew = false);
        /// <summary>
        /// Get data list by condition (specified columns) (async)
        /// </summary>
        /// <returns>Return value</returns>
        Task<List<T>> GetListAsync(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T>> selectedColumnExpression, Expression<Func<T, object>> orderExpression = null, OrderByType orderByType = OrderByType.Asc, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get single data by condition (async)
        /// </summary>
        /// <returns>Return value</returns>
        Task<T> GetSingleAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get first data by condition (async)
        /// </summary>
        /// <returns>Return value</returns>
        Task<T> GetFirstAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default, bool copyNew = false);

        #endregion Query

        #region Page Query

        /// <summary>
        /// Pagination with sorting
        /// </summary>
        /// <param name="whereExpression">Query condition</param>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Items per page</param>
        /// <param name="TotalCount">Total count</param>
        /// <param name="orderByExpression">Sort condition</param>
        /// <param name="isAsc">Sort type (true: ascending, false: descending)</param>
        /// <param name="selectedColumnExpression">Specify to query only certain columns</param>
        /// <returns>Result list</returns>
        List<T> GetPageList(Expression<Func<T, bool>> whereExpression, int pageIndex, int pageSize, out int TotalCount, Expression<Func<T, object>> orderByExpression = null, bool isAsc = true, Expression<Func<T, T>> selectedColumnExpression = null);

        /// <summary>
        /// Pagination (async) with sorting and cancellation
        /// </summary>
        /// <param name="whereExpression">Query condition</param>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Items per page</param>
        /// <param name="orderByExpression">Sort condition</param>
        /// <param name="isAsc">Sort type (true: ascending, false: descending)</param>
        /// <param name="selectedColumnExpression">Specify to query only certain columns</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>(Result list, total count)</returns>
        Task<(List<T> datas, int total)> GetPageListAsync(Expression<Func<T, bool>> whereExpression, int pageIndex, int pageSize, Expression<Func<T, object>> orderByExpression = null, bool isAsc = true, Expression<Func<T, T>> selectedColumnExpression = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pagination with sorting
        /// </summary>
        /// <param name="whereExpressionList">Multi-condition dynamic union query (suitable for query scenarios with multiple optional query items)</param>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Items per page</param>
        /// <param name="TotalCount">Total count</param>
        /// <param name="orderByExpression">Sort condition</param>
        /// <param name="isAsc">Sort type (true: ascending, false: descending)</param>
        /// <param name="selectedColumnExpression">Specify to query only certain columns</param>
        /// <returns>Result list</returns>
        List<T> GetPageList(List<Expression<Func<T, bool>>> whereExpressionList, int pageIndex, int pageSize, out int TotalCount, Expression<Func<T, object>> orderByExpression = null, bool isAsc = true, Expression<Func<T, T>> selectedColumnExpression = null);

        /// <summary>
        /// Pagination (async) with sorting and cancellation
        /// </summary>
        /// <param name="whereExpressionList">Multi-condition dynamic union query (suitable for query scenarios with multiple optional query items)</param>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Items per page</param>
        /// <param name="orderByExpression">Sort condition</param>
        /// <param name="isAsc">Sort type (true: ascending, false: descending)</param>
        /// <param name="selectedColumnExpression">Specify to query only certain columns</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>(Result list, total count)</returns>
        Task<(List<T> datas, int total)> GetPageListAsync(List<Expression<Func<T, bool>>> whereExpressionList, int pageIndex, int pageSize, Expression<Func<T, object>> orderByExpression = null, bool isAsc = true, Expression<Func<T, T>> selectedColumnExpression = null, CancellationToken cancellationToken = default);

        #endregion Page Query

        #region Delete

        /// <summary>
        /// Delete data
        /// </summary>
        /// <returns>Whether successful</returns>
        bool Delete(T deleteObj);

        /// <summary>
        /// Delete data list
        /// </summary>
        /// <returns>Whether successful</returns>
        bool Delete(List<T> deleteObjs);

        /// <summary>
        /// Delete data by condition
        /// </summary>
        /// <returns>Whether successful</returns>
        bool Delete(Expression<Func<T, bool>> whereExpression);

        /// <summary>
        /// Delete data by ID
        /// </summary>
        /// <returns>Whether successful</returns>
        bool DeleteById(object id);

        /// <summary>
        /// Delete data (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        Task<bool> DeleteAsync(T deleteObj, CancellationToken cancellationToken = default);

        /// <summary>
        /// Batch delete data (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        Task<bool> DeleteAsync(List<T> deleteObjs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete data by condition (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        Task<bool> DeleteAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete data by ID (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        Task<bool> DeleteByIdAsync(object id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete data by ID collection (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        Task<bool> DeleteByIdsAsync(dynamic[] ids, CancellationToken cancellationToken = default);

        #endregion Delete

        #region Is Any

        /// <summary>
        /// Check if data exists by condition
        /// </summary>
        /// <returns>Return value</returns>
        bool IsAny(Expression<Func<T, bool>> whereExpression);

        /// <summary>
        /// Check if data exists by condition (async)
        /// </summary>
        /// <returns>Whether exists</returns>
        Task<bool> IsAnyAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default, bool copyNew = false);

        #endregion Is Any

        #region Count

        /// <summary>
        /// Count data by condition
        /// </summary>
        /// <returns>Return value</returns>
        int Count(Expression<Func<T, bool>> whereExpression);

        /// <summary>
        /// Count data by condition (async)
        /// </summary>
        /// <returns>Count</returns>
        Task<int> CountAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default);

        #endregion Count

        #region Transaction

        /// <summary>
        /// Begin transaction
        /// </summary>
        void BeginTran();

        /// <summary>
        /// Commit transaction
        /// </summary>
        void CommitTran();

        /// <summary>
        /// Rollback transaction
        /// </summary>
        void RollbackTran();

        /// <summary>
        /// Execute transaction
        /// </summary>
        /// <param name="action">All operations to execute</param>
        /// <returns>Whether execution was successful</returns>
        bool UseTran(Action action);

        /// <summary>
        /// Execute transaction (async)
        /// </summary>
        /// <param name="action">All operations to execute</param>
        /// <returns>Whether execution was successful</returns>
        Task<bool> UseTranAsync(Func<Task> action);

        /// <summary>
        /// Execute transaction and get specified return value
        /// </summary>
        /// <param name="action">All operations to execute</param>
        /// <returns>Return value, whether execution was successful</returns>
        (ReturnValueT result, bool success) UseTranReturnValue<ReturnValueT>(Func<ReturnValueT> action);

        /// <summary>
        /// Execute transaction and get specified return value (async)
        /// </summary>
        /// <param name="action">All operations to execute</param>
        /// <returns>Return value, whether execution was successful</returns>
        Task<(ReturnValueT result, bool success)> UseTranReturnValueAsync<ReturnValueT>(Func<Task<ReturnValueT>> action);

        #endregion Transaction

        #region Not Supported Async Transaction

        ///// <summary>
        ///// Begin transaction (async)
        ///// </summary>
        //Task BeginTranAsync();
        ///// <summary>
        ///// Commit transaction (async)
        ///// </summary>
        //Task CommitTranAsync();
        ///// <summary>
        ///// Rollback transaction (async)
        ///// </summary>
        //Task RollbackTranAsync();

        #endregion Not Supported Async Transaction

        void QueryFilterClearAndBackup();

        void QueryFilterClear();

        void QueryFilterRestore();

        ISugarQueryable<T> ClearFilter();

        ISugarQueryable<T> ClearFilter<IFilter>();


        /// <summary>
        /// Use external transaction for internal transaction when external transaction exists
        /// Implement nested transactions through unit of work
        /// </summary>
        /// <returns></returns>
        SugarUnitOfWork CreateContext();
    }
}
