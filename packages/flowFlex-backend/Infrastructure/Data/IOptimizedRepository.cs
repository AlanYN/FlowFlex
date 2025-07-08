using System.Linq.Expressions;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Infrastructure.Data
{
    /// <summary>
    /// 优化的仓储接口
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public interface IOptimizedRepository<T> where T : class, new()
    {
        // 查询操作
        Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default);
        Task<List<T>> GetListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
        Task<PagedResult<T>> GetPagedListAsync(Expression<Func<T, bool>>? predicate = null, int pageIndex = 1, int pageSize = 10,
            Expression<Func<T, object>>? orderBy = null, bool ascending = true, CancellationToken cancellationToken = default);
        Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

        // 写入操作
        Task<T> InsertAsync(T entity, CancellationToken cancellationToken = default);
        Task<List<T>> InsertRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);
        Task<List<T>> UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(object id, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken = default);
        Task<int> DeleteRangeAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        // 事务操作
        Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken = default);
        Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);

        // 批量操作
        Task<int> BulkInsertAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        Task<int> BulkUpdateAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        Task<int> BulkDeleteAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    }
}