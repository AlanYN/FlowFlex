using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FlowFlex.Domain.Entities;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.SqlSugarDB.Context;
using SqlSugar;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// OW base repository implementation
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public abstract class OwBaseRepository<T> : IOwBaseRepository<T> where T : OwEntityBase, new()
    {
        protected readonly ISqlSugarClient _db;

        public OwBaseRepository(ISqlSugarContext context)
        {
            _db = context.GetDbClient();
        }

        /// <summary>
        /// Get entity by ID
        /// </summary>
        /// <param name="id">Entity ID</param>
        /// <returns>Entity</returns>
        public virtual async Task<T> GetByIdAsync(long id)
        {
            return await _db.Queryable<T>().Where(e => e.Id == id && e.IsValid).FirstAsync();
        }

        /// <summary>
        /// Get all entities
        /// </summary>
        /// <returns>Entity list</returns>
        public virtual async Task<List<T>> GetAllAsync()
        {
            return await _db.Queryable<T>().Where(e => e.IsValid).ToListAsync();
        }

        /// <summary>
        /// Get entities by condition
        /// </summary>
        /// <param name="predicate">Condition expression</param>
        /// <returns>Entity list</returns>
        public virtual async Task<List<T>> GetAsync(Expression<Func<T, bool>> predicate)
        {
            return await _db.Queryable<T>().Where(predicate).Where(e => e.IsValid).ToListAsync();
        }

        /// <summary>
        /// Add entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <returns>Added entity</returns>
        public virtual async Task<T> InsertAsync(T entity)
        {
            entity.CreateDate = DateTimeOffset.Now;
            entity.ModifyDate = DateTimeOffset.Now;
            entity.IsValid = true;
            
            // Set default value if TenantId is empty
            if (string.IsNullOrEmpty(entity.TenantId))
            {
                entity.TenantId = "default";
            }

            await _db.Insertable(entity).ExecuteCommandAsync();
            return entity;
        }

        /// <summary>
        /// Add entities in batch
        /// </summary>
        /// <param name="entities">Entity list</param>
        /// <returns>Added entity list</returns>
        public virtual async Task<List<T>> InsertRangeAsync(List<T> entities)
        {
            foreach (var entity in entities)
            {
                entity.CreateDate = DateTimeOffset.Now;
                entity.ModifyDate = DateTimeOffset.Now;
                entity.IsValid = true;
                
                // Set default value if TenantId is empty
                if (string.IsNullOrEmpty(entity.TenantId))
                {
                    entity.TenantId = "default";
                }
            }

            await _db.Insertable(entities).ExecuteCommandAsync();
            return entities;
        }

        /// <summary>
        /// Update entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <returns>Updated entity</returns>
        public virtual async Task<T> UpdateAsync(T entity)
        {
            entity.ModifyDate = DateTimeOffset.Now;
            await _db.Updateable(entity).ExecuteCommandAsync();
            return entity;
        }

        /// <summary>
        /// Update entities in batch
        /// </summary>
        /// <param name="entities">Entity list</param>
        /// <returns>Updated entity list</returns>
        public virtual async Task<List<T>> UpdateRangeAsync(List<T> entities)
        {
            foreach (var entity in entities)
            {
                entity.ModifyDate = DateTimeOffset.Now;
            }

            await _db.Updateable(entities).ExecuteCommandAsync();
            return entities;
        }

        /// <summary>
        /// Delete entity
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <returns>Whether deletion successful</returns>
        public virtual async Task<bool> DeleteAsync(T entity)
        {
            entity.IsValid = false;
            entity.ModifyDate = DateTimeOffset.Now;
            return await _db.Updateable(entity).ExecuteCommandAsync() > 0;
        }

        /// <summary>
        /// Delete entity by ID
        /// </summary>
        /// <param name="id">Entity ID</param>
        /// <returns>Whether deletion successful</returns>
        public virtual async Task<bool> DeleteByIdAsync(long id)
        {
            var entity = await GetByIdAsync(id);
            if (entity == null)
            {
                return false;
            }

            return await DeleteAsync(entity);
        }

        /// <summary>
        /// Delete entities in batch
        /// </summary>
        /// <param name="entities">Entity list</param>
        /// <returns>Whether deletion successful</returns>
        public virtual async Task<bool> DeleteRangeAsync(List<T> entities)
        {
            foreach (var entity in entities)
            {
                entity.IsValid = false;
                entity.ModifyDate = DateTimeOffset.Now;
            }

            return await _db.Updateable(entities).ExecuteCommandAsync() > 0;
        }

        /// <summary>
        /// Delete entities by condition
        /// </summary>
        /// <param name="predicate">Condition expression</param>
        /// <returns>Whether deletion successful</returns>
        public virtual async Task<bool> DeleteAsync(Expression<Func<T, bool>> predicate)
        {
            var entities = await GetAsync(predicate);
            if (entities.Count == 0)
            {
                return false;
            }

            return await DeleteRangeAsync(entities);
        }

        /// <summary>
        /// Check if entity exists
        /// </summary>
        /// <param name="predicate">Condition expression</param>
        /// <returns>Whether exists</returns>
        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _db.Queryable<T>().Where(predicate).Where(e => e.IsValid).AnyAsync();
        }

        /// <summary>
        /// Get entity count
        /// </summary>
        /// <param name="predicate">Condition expression</param>
        /// <returns>Entity count</returns>
        public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null)
        {
            var query = _db.Queryable<T>().Where(e => e.IsValid);
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return await query.CountAsync();
        }

        /// <summary>
        /// Set creator information
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="userEmail">User email</param>
        /// <param name="userId">User ID</param>
        public void SetCreateInfo(T entity, string userEmail, long userId = 0)
        {
            entity.CreateBy = userEmail;
            entity.ModifyBy = userEmail;
            
            if (userId > 0)
            {
                entity.CreateUserId = userId;
                entity.ModifyUserId = userId;
            }
        }

        /// <summary>
        /// Set modifier information
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="userEmail">User email</param>
        /// <param name="userId">User ID</param>
        public void SetModifyInfo(T entity, string userEmail, long userId = 0)
        {
            entity.ModifyBy = userEmail;
            
            if (userId > 0)
            {
                entity.ModifyUserId = userId;
            }
        }
    }
} 
