using System.Diagnostics;
using System.Linq.Expressions;
using SqlSugar;
using FlowFlex.Domain.Entities.Base;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Infrastructure.Services.Logging;

namespace FlowFlex.Infrastructure.Data
{
    /// <summary>
    /// Optimized repository base class implementation
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class OptimizedRepository<T> : IOptimizedRepository<T> where T : class, new()
    {
        protected readonly ISqlSugarClient _db;
        protected readonly IApplicationLogger _logger;
        protected readonly UserContext _userContext;

        public OptimizedRepository(ISqlSugarClient db, IApplicationLogger logger, UserContext userContext)
        {
            _db = db;
            _logger = logger;
            _userContext = userContext;
        }

        public virtual async Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _db.Queryable<T>().InSingleAsync(id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting entity by ID: {Id}", id);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogPerformance($"GetByIdAsync<{typeof(T).Name}>", stopwatch.ElapsedMilliseconds, $"ID: {id}");
            }
        }

        public virtual async Task<List<T>> GetListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var query = _db.Queryable<T>();
                if (predicate != null)
                {
                    query = query.Where(predicate);
                }
                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting entity list");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogPerformance($"GetListAsync<{typeof(T).Name}>", stopwatch.ElapsedMilliseconds);
            }
        }

        public virtual async Task<PagedResult<T>> GetPagedListAsync(Expression<Func<T, bool>>? predicate = null, int pageIndex = 1, int pageSize = 10,
            Expression<Func<T, object>>? orderBy = null, bool ascending = true, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var query = _db.Queryable<T>();

                if (predicate != null)
                {
                    query = query.Where(predicate);
                }

                if (orderBy != null)
                {
                    query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
                }

                var totalCount = await query.CountAsync();
                var items = await query.ToPageListAsync(pageIndex, pageSize);

                return new PagedResult<T>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageIndex = pageIndex,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged entity list. PageIndex: {PageIndex}, PageSize: {PageSize}", pageIndex, pageSize);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogPerformance($"GetPagedListAsync<{typeof(T).Name}>", stopwatch.ElapsedMilliseconds,
                    $"PageIndex: {pageIndex}, PageSize: {pageSize}");
            }
        }

        public virtual async Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                return await _db.Queryable<T>().FirstAsync(predicate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting first entity");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogPerformance($"GetFirstOrDefaultAsync<{typeof(T).Name}>", stopwatch.ElapsedMilliseconds);
            }
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                return await _db.Queryable<T>().AnyAsync(predicate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking entity existence");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogPerformance($"ExistsAsync<{typeof(T).Name}>", stopwatch.ElapsedMilliseconds);
            }
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var query = _db.Queryable<T>();
                if (predicate != null)
                {
                    query = query.Where(predicate);
                }
                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting entities");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogPerformance($"CountAsync<{typeof(T).Name}>", stopwatch.ElapsedMilliseconds);
            }
        }

        public virtual async Task<T> InsertAsync(T entity, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Auto-generate ID for entities that support it
                if (entity is IdEntityBase idEntity && idEntity.Id == 0)
                {
                    idEntity.InitNewId();
                }

                var result = await _db.Insertable(entity).ExecuteReturnEntityAsync();

                _logger.LogBusinessOperation("INSERT", typeof(T).Name, GetEntityId(result), "Entity inserted successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting entity");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogPerformance($"InsertAsync<{typeof(T).Name}>", stopwatch.ElapsedMilliseconds);
            }
        }

        public virtual async Task<List<T>> InsertRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var entityList = entities.ToList();
            try
            {
                // Auto-generate IDs for entities that support it
                foreach (var entity in entityList)
                {
                    if (entity is IdEntityBase idEntity && idEntity.Id == 0)
                    {
                        idEntity.InitNewId();
                    }
                }

                await _db.Insertable(entityList).ExecuteCommandAsync();

                _logger.LogBusinessOperation("BULK_INSERT", typeof(T).Name, entityList.Count, $"{entityList.Count} entities inserted successfully");
                return entityList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting entity range. Count: {Count}", entityList.Count);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogPerformance($"InsertRangeAsync<{typeof(T).Name}>", stopwatch.ElapsedMilliseconds, $"Count: {entityList.Count}");
            }
        }

        public virtual async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await _db.Updateable(entity).ExecuteCommandAsync();

                _logger.LogBusinessOperation("UPDATE", typeof(T).Name, GetEntityId(entity), "Entity updated successfully");
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating entity");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogPerformance($"UpdateAsync<{typeof(T).Name}>", stopwatch.ElapsedMilliseconds);
            }
        }

        public virtual async Task<List<T>> UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var entityList = entities.ToList();
            try
            {
                await _db.Updateable(entityList).ExecuteCommandAsync();

                _logger.LogBusinessOperation("BULK_UPDATE", typeof(T).Name, entityList.Count, $"{entityList.Count} entities updated successfully");
                return entityList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating entity range. Count: {Count}", entityList.Count);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogPerformance($"UpdateRangeAsync<{typeof(T).Name}>", stopwatch.ElapsedMilliseconds, $"Count: {entityList.Count}");
            }
        }

        public virtual async Task<bool> DeleteAsync(object id, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _db.Deleteable<T>().In(id).ExecuteCommandAsync() > 0;

                if (result)
                {
                    _logger.LogBusinessOperation("DELETE", typeof(T).Name, id, "Entity deleted successfully");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting entity by ID: {Id}", id);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogPerformance($"DeleteAsync<{typeof(T).Name}>", stopwatch.ElapsedMilliseconds, $"ID: {id}");
            }
        }

        public virtual async Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _db.Deleteable(entity).ExecuteCommandAsync() > 0;

                if (result)
                {
                    _logger.LogBusinessOperation("DELETE", typeof(T).Name, GetEntityId(entity), "Entity deleted successfully");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting entity");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogPerformance($"DeleteAsync<{typeof(T).Name}>", stopwatch.ElapsedMilliseconds);
            }
        }

        public virtual async Task<int> DeleteRangeAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _db.Deleteable<T>().Where(predicate).ExecuteCommandAsync();

                _logger.LogBusinessOperation("BULK_DELETE", typeof(T).Name, result, $"{result} entities deleted successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting entity range");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogPerformance($"DeleteRangeAsync<{typeof(T).Name}>", stopwatch.ElapsedMilliseconds);
            }
        }

        public virtual async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _db.AsTenant().UseTranAsync(async () =>
                {
                    return await operation();
                });
                return result.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing transaction operation");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogPerformance($"ExecuteInTransactionAsync<{typeof(TResult).Name}>", stopwatch.ElapsedMilliseconds);
            }
        }

        public virtual async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await _db.AsTenant().UseTranAsync(async () =>
                {
                    await operation();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing transaction operation");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogPerformance("ExecuteInTransactionAsync", stopwatch.ElapsedMilliseconds);
            }
        }

        public virtual async Task<int> BulkInsertAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var entityList = entities.ToList();
            try
            {
                // Auto-generate IDs for entities that support it
                foreach (var entity in entityList)
                {
                    if (entity is IdEntityBase idEntity && idEntity.Id == 0)
                    {
                        idEntity.InitNewId();
                    }
                }

                var result = await _db.Fastest<T>().BulkCopyAsync(entityList);

                _logger.LogBusinessOperation("BULK_COPY", typeof(T).Name, entityList.Count, $"{entityList.Count} entities bulk inserted successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk inserting entities. Count: {Count}", entityList.Count);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogPerformance($"BulkInsertAsync<{typeof(T).Name}>", stopwatch.ElapsedMilliseconds, $"Count: {entityList.Count}");
            }
        }

        public virtual async Task<int> BulkUpdateAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var entityList = entities.ToList();
            try
            {
                var result = await _db.Fastest<T>().BulkUpdateAsync(entityList);

                _logger.LogBusinessOperation("BULK_UPDATE", typeof(T).Name, entityList.Count, $"{entityList.Count} entities bulk updated successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk updating entities. Count: {Count}", entityList.Count);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogPerformance($"BulkUpdateAsync<{typeof(T).Name}>", stopwatch.ElapsedMilliseconds, $"Count: {entityList.Count}");
            }
        }

        public virtual async Task<int> BulkDeleteAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _db.Deleteable<T>().Where(predicate).ExecuteCommandAsync();

                _logger.LogBusinessOperation("BULK_DELETE", typeof(T).Name, result, $"{result} entities bulk deleted successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk deleting entities");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogPerformance($"BulkDeleteAsync<{typeof(T).Name}>", stopwatch.ElapsedMilliseconds);
            }
        }

        // Optimized query methods

        /// <summary>
        /// Get list of specified fields to reduce data transfer
        /// </summary>
        public virtual async Task<List<TResult>> GetSelectedListAsync<TResult>(
            Expression<Func<T, TResult>> selector,
            Expression<Func<T, bool>>? predicate = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var query = _db.Queryable<T>();
                if (predicate != null)
                {
                    query = query.Where(predicate);
                }

                return await query.Select(selector).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting selected list");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogPerformance($"GetSelectedListAsync<{typeof(T).Name}, {typeof(TResult).Name}>", stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Batch query to avoid N+1 problem
        /// </summary>
        public virtual async Task<List<T>> GetByIdsAsync(IEnumerable<object> ids, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var idList = ids.ToList();
            try
            {
                return await _db.Queryable<T>().In(idList).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting entities by IDs. Count: {Count}", idList.Count);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogPerformance($"GetByIdsAsync<{typeof(T).Name}>", stopwatch.ElapsedMilliseconds, $"IDs Count: {idList.Count}");
            }
        }

        /// <summary>
        /// Check if entity exists (only query primary key for better performance)
        /// </summary>
        public virtual async Task<bool> ExistsByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                return await _db.Queryable<T>().Where($"Id = @id", new { id }).AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking entity existence by ID: {Id}", id);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogPerformance($"ExistsByIdAsync<{typeof(T).Name}>", stopwatch.ElapsedMilliseconds, $"ID: {id}");
            }
        }

        /// <summary>
        /// Get maximum value (for sorting fields, etc.)
        /// </summary>
        public virtual async Task<TResult> GetMaxAsync<TResult>(
            Expression<Func<T, TResult>> selector,
            Expression<Func<T, bool>>? predicate = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var query = _db.Queryable<T>();
                if (predicate != null)
                {
                    query = query.Where(predicate);
                }

                return await query.MaxAsync(selector);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting max value");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogPerformance($"GetMaxAsync<{typeof(T).Name}, {typeof(TResult).Name}>", stopwatch.ElapsedMilliseconds);
            }
        }

        protected virtual object GetEntityId(T entity)
        {
            if (entity is IdEntityBase idEntity)
            {
                return idEntity.Id;
            }

            // Try to get ID via reflection as fallback
            var idProperty = typeof(T).GetProperty("Id");
            return idProperty?.GetValue(entity) ?? "Unknown";
        }
    }
}