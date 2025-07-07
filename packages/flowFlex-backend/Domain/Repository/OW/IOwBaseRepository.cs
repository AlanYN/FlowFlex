using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FlowFlex.Domain.Entities;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// OW base repository interface
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IOwBaseRepository<T> where T : OwEntityBase
    {
        /// <summary>
        /// Get entity by ID
        /// </summary>
        /// <param name="id">Entity ID</param>
        /// <returns>Entity</returns>
        Task<T> GetByIdAsync(long id);

        /// <summary>
        /// Get all entities
        /// </summary>
        /// <returns>Entity list</returns>
        Task<List<T>> GetAllAsync();

        /// <summary>
        /// Get entities by condition
        /// </summary>
        /// <param name="predicate">Condition expression</param>
        /// <returns>Entity list</returns>
        Task<List<T>> GetAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Add entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <returns>Added entity</returns>
        Task<T> InsertAsync(T entity);

        /// <summary>
        /// Batch add entities
        /// </summary>
        /// <param name="entities">Entity list</param>
        /// <returns>Added entity list</returns>
        Task<List<T>> InsertRangeAsync(List<T> entities);

        /// <summary>
        /// Update entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <returns>Updated entity</returns>
        Task<T> UpdateAsync(T entity);

        /// <summary>
        /// Batch update entities
        /// </summary>
        /// <param name="entities">Entity list</param>
        /// <returns>Updated entity list</returns>
        Task<List<T>> UpdateRangeAsync(List<T> entities);

        /// <summary>
        /// Delete entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <returns>Whether deletion succeeded</returns>
        Task<bool> DeleteAsync(T entity);

        /// <summary>
        /// Delete entity by ID
        /// </summary>
        /// <param name="id">Entity ID</param>
        /// <returns>Whether deletion succeeded</returns>
        Task<bool> DeleteByIdAsync(long id);

        /// <summary>
        /// Batch delete entities
        /// </summary>
        /// <param name="entities">Entity list</param>
        /// <returns>Whether deletion succeeded</returns>
        Task<bool> DeleteRangeAsync(List<T> entities);

        /// <summary>
        /// Delete entities by condition
        /// </summary>
        /// <param name="predicate">Condition expression</param>
        /// <returns>Whether deletion succeeded</returns>
        Task<bool> DeleteAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Check if entity exists
        /// </summary>
        /// <param name="predicate">Condition expression</param>
        /// <returns>Whether exists</returns>
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Get entity count
        /// </summary>
        /// <param name="predicate">Condition expression</param>
        /// <returns>Entity count</returns>
        Task<int> CountAsync(Expression<Func<T, bool>> predicate = null);

        /// <summary>
        /// Set creator information
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="userEmail">User email</param>
        /// <param name="userId">User ID</param>
        void SetCreateInfo(T entity, string userEmail, long userId = 0);

        /// <summary>
        /// Set modifier information
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="userEmail">User email</param>
        /// <param name="userId">User ID</param>
        void SetModifyInfo(T entity, string userEmail, long userId = 0);
    }
} 
