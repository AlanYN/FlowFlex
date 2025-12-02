using Microsoft.Extensions.Logging;
using SqlSugar;
using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
using Microsoft.AspNetCore.Http;

namespace FlowFlex.SqlSugarDB.Implements.Integration
{
    /// <summary>
    /// Dynamic field repository implementation
    /// </summary>
    public class DynamicFieldRepository : BaseRepository<DynamicField>, IDynamicFieldRepository, IScopedService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<DynamicFieldRepository> _logger;

        public DynamicFieldRepository(
            ISqlSugarClient sqlSugarClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<DynamicFieldRepository> logger) : base(sqlSugarClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Get dynamic field by field ID
        /// </summary>
        public async Task<DynamicField?> GetByFieldIdAsync(string fieldId)
        {
            return await db.Queryable<DynamicField>()
                .Where(x => x.FieldId == fieldId && x.IsValid)
                .FirstAsync();
        }

        /// <summary>
        /// Get all dynamic fields by category
        /// </summary>
        public async Task<List<DynamicField>> GetByCategoryAsync(string category)
        {
            return await db.Queryable<DynamicField>()
                .Where(x => x.Category == category && x.IsValid)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();
        }

        /// <summary>
        /// Get all dynamic fields ordered by sort order
        /// </summary>
        public async Task<List<DynamicField>> GetAllOrderedAsync()
        {
            return await db.Queryable<DynamicField>()
                .Where(x => x.IsValid)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();
        }

        /// <summary>
        /// Check if field ID exists
        /// </summary>
        public async Task<bool> ExistsFieldIdAsync(string fieldId, long? excludeId = null)
        {
            var query = db.Queryable<DynamicField>()
                .Where(x => x.FieldId == fieldId && x.IsValid);

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// Get dynamic field by form property name
        /// </summary>
        public async Task<DynamicField?> GetByFormPropAsync(string formProp)
        {
            return await db.Queryable<DynamicField>()
                .Where(x => x.FormProp == formProp && x.IsValid)
                .FirstAsync();
        }
    }
}

