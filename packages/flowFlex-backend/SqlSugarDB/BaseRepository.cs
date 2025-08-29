using Item.Common.Lib.Common;
using FlowFlex.Domain.Entities.Base;
using FlowFlex.Domain.Repository;
using SqlSugar;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.Linq;

namespace FlowFlex.SqlSugarDB
{
    /// <summary>
    /// Common data repository interface implementation class, IBaseRepository<T>: inherits from the common data repository interface
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BaseRepository<T> : IBaseRepository<T> where T : class, new()
    {
        public ISqlSugarClient db;

        protected static string[] ModifyColumn =>
        [
            "modify_date",
            "modify_by",
            "modify_user_id"
        ];

        public BaseRepository(ISqlSugarClient context)
        {
            db = context;
        }

        #region Insert

        public async Task<bool> InsertableByObjectAsync(object entity)
        {
            return await db.InsertableByObject(entity).ExecuteCommandAsync() > 0;
        }

        /// <summary>
        /// Insert data
        /// </summary>
        /// <returns>Whether successful</returns>
        public bool Insert(T insertObj)
        {
            return db.Insertable(insertObj).ExecuteCommand() > 0;
        }

        /// <summary>
        /// Insert or update data
        /// </summary>
        /// <returns>Whether successful</returns>
        public bool InsertOrUpdate(T data)
        {
            return db.Storageable(data).ExecuteCommand() > 0;
        }

        /// <summary>
        /// Insert or update data list
        /// </summary>
        /// <returns>Whether successful</returns>
        public bool InsertOrUpdate(List<T> datas)
        {
            return db.Storageable(datas).ExecuteCommand() > 0;
        }

        /// <summary>
        /// Insert data and return auto-increment id
        /// </summary>
        /// <returns>Return value</returns>
        public int InsertReturnIdentity(T insertObj)
        {
            return db.Insertable(insertObj).ExecuteReturnIdentity();
        }

        /// <summary>
        /// Insert data and return bigInt auto-increment id
        /// </summary>
        /// <returns>Return value</returns>
        public long InsertReturnBigIdentity(T insertObj)
        {
            return db.Insertable(insertObj).ExecuteReturnBigIdentity();
        }

        /// <summary>
        /// Insert data and return snowflake id
        /// </summary>
        /// <returns>Return value</returns>
        public long InsertReturnSnowflakeId(T insertObj)
        {
            return db.Insertable(insertObj).ExecuteReturnSnowflakeId();
        }

        /// <summary>
        /// Insert data list and return snowflake id list
        /// </summary>
        /// <returns>Return value</returns>
        public List<long> InsertReturnSnowflakeId(List<T> insertObjs)
        {
            return db.Insertable(insertObjs).ExecuteReturnSnowflakeIdList();
        }

        /// <summary>
        /// Insert data and return entity
        /// </summary>
        /// <returns>Return value</returns>
        public T InsertReturnEntity(T insertObj)
        {
            return db.Insertable(insertObj).ExecuteReturnEntity();
        }

        /// <summary>
        /// Insert data array
        /// </summary>
        /// <returns>Whether successful</returns>
        public bool InsertRange(T[] insertObjs)
        {
            return db.Insertable(insertObjs).ExecuteCommand() > 0;
        }

        /// <summary>
        /// Insert data list
        /// </summary>
        /// <returns>Whether successful</returns>
        public bool InsertRange(List<T> insertObjs)
        {
            return db.Insertable(insertObjs).ExecuteCommand() > 0;
        }

        /// <summary>
        /// Insert data and return snowflake id (async)
        /// </summary>
        /// <returns>Return value</returns>
        public async Task<long> InsertReturnSnowflakeIdAsync(T insertObj, CancellationToken cancellationToken = default)
        {
            var backupFilters = BackupFilters();

            db.Ado.CancellationToken = cancellationToken;
            var result = await db.Insertable(insertObj).ExecuteReturnSnowflakeIdAsync();

            RestoreFiltersIfChanged(backupFilters);
            return result;
        }

        /// <summary>
        /// Insert data and return SnowflakeId (async)
        /// </summary>
        /// <returns>SnowflakeId list</returns>
        public async Task<List<long>> InsertReturnSnowflakeIdAsync(List<T> insertObjs, CancellationToken cancellationToken = default)
        {
            var backupFilters = BackupFilters();

            db.Ado.CancellationToken = cancellationToken;
            var result = await db.Insertable(insertObjs).ExecuteReturnSnowflakeIdListAsync();

            RestoreFiltersIfChanged(backupFilters);
            return result;
        }

        /// <summary>
        /// Insert or update data (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        public async Task<bool> InsertOrUpdateAsync(T data, CancellationToken cancellationToken = default)
        {
            var backupFilters = BackupFilters();

            db.Ado.CancellationToken = cancellationToken;
            var result = await db.Storageable(data).ExecuteCommandAsync() > 0;

            RestoreFiltersIfChanged(backupFilters);
            return result;
        }

        /// <summary>
        /// Batch insert or update data (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        public async Task<bool> InsertOrUpdateAsync(List<T> datas, CancellationToken cancellationToken = default)
        {
            var backupFilters = BackupFilters();

            db.Ado.CancellationToken = cancellationToken;
            var result = await db.Storageable(datas).ExecuteCommandAsync() > 0;

            RestoreFiltersIfChanged(backupFilters);
            return result;
        }

        /// <summary>
        /// Insert data (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        public async Task<bool> InsertAsync(T insertObj, CancellationToken cancellationToken = default, bool copyNew = false)
        {
            var dbNew = copyNew ? db.CopyNew() : db;

            // Auto-generate snowflake ID if entity has Id property and it's 0
            if (insertObj is IdEntityBase idEntity && idEntity.Id == 0)
            {
                idEntity.InitNewId();
            }

            dbNew.Ado.CancellationToken = cancellationToken;
            var result = await dbNew.Insertable(insertObj).ExecuteCommandAsync() > 0;

            return result;
        }

        /// <summary>
        /// Insert data and return Identity (async)
        /// </summary>
        /// <returns>Identity</returns>
        public async Task<int> InsertReturnIdentityAsync(T insertObj, CancellationToken cancellationToken = default)
        {
            var backupFilters = BackupFilters();

            db.Ado.CancellationToken = cancellationToken;
            var result = await db.Insertable(insertObj).ExecuteReturnIdentityAsync();

            RestoreFiltersIfChanged(backupFilters);
            return result;
        }

        /// <summary>
        /// Insert data and return BigIdentity (async)
        /// </summary>
        /// <returns>BigIdentity</returns>
        public async Task<long> InsertReturnBigIdentityAsync(T insertObj, CancellationToken cancellationToken = default)
        {
            var backupFilters = BackupFilters();

            db.Ado.CancellationToken = cancellationToken;
            var result = await db.Insertable(insertObj).ExecuteReturnBigIdentityAsync();

            RestoreFiltersIfChanged(backupFilters);
            return result;
        }

        /// <summary>
        /// Insert data and return entity (async)
        /// </summary>
        /// <returns>Return value</returns>
        public async Task<T> InsertReturnEntityAsync(T insertObj, CancellationToken cancellationToken = default)
        {
            var backupFilters = BackupFilters();

            db.Ado.CancellationToken = cancellationToken;
            var result = await db.Insertable(insertObj).ExecuteReturnEntityAsync();

            RestoreFiltersIfChanged(backupFilters);
            return result;
        }

        /// <summary>
        /// Batch insert data (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        public async Task<bool> InsertRangeAsync(T[] insertObjs, CancellationToken cancellationToken = default)
        {
            var backupFilters = BackupFilters();

            // Auto-generate snowflake IDs for entities with Id property = 0
            foreach (var insertObj in insertObjs)
            {
                if (insertObj is IdEntityBase idEntity && idEntity.Id == 0)
                {
                    idEntity.InitNewId();
                }
            }

            db.Ado.CancellationToken = cancellationToken;
            var result = await db.Insertable(insertObjs).ExecuteCommandAsync() > 0;

            RestoreFiltersIfChanged(backupFilters);
            return result;
        }

        /// <summary>
        /// Batch insert data (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        public async Task<bool> InsertRangeAsync(List<T> insertObjs, CancellationToken cancellationToken = default)
        {
            var backupFilters = BackupFilters();

            // Auto-generate snowflake IDs for entities with Id property = 0
            foreach (var insertObj in insertObjs)
            {
                if (insertObj is IdEntityBase idEntity && idEntity.Id == 0)
                {
                    idEntity.InitNewId();
                }
            }

            db.Ado.CancellationToken = cancellationToken;
            var result = await db.Insertable(insertObjs).ExecuteCommandAsync() > 0;

            RestoreFiltersIfChanged(backupFilters);
            return result;
        }

        #endregion Insert

        #region Update

        /// <summary>
        /// Update data
        /// </summary>
        /// <returns>Whether successful</returns>
        public bool Update(T updateObj)
        {
            return db.Updateable(updateObj).ExecuteCommand() > 0;
        }

        /// <summary>
        /// Update data array
        /// </summary>
        /// <returns>Whether successful</returns>
        public bool UpdateRange(T[] updateObjs)
        {
            return db.Updateable(updateObjs).ExecuteCommand() > 0;
        }

        /// <summary>
        /// Update data list
        /// </summary>
        /// <returns>Whether successful</returns>
        public bool UpdateRange(List<T> updateObjs)
        {
            return db.Updateable(updateObjs).ExecuteCommand() > 0;
        }

        /// <summary>
        /// Update data by condition
        /// </summary>
        /// <returns>Whether successful</returns>
        public bool Update(Expression<Func<T, T>> columns, Expression<Func<T, bool>> whereExpression)
        {
            return db.Updateable<T>().SetColumns(columns).Where(whereExpression).ExecuteCommand() > 0;
        }

        /// <summary>
        /// Update data by condition and set columns to true
        /// </summary>
        /// <returns>Whether successful</returns>
        public bool UpdateSetColumnsTrue(Expression<Func<T, T>> columns, Expression<Func<T, bool>> whereExpression)
        {
            return db.Updateable<T>().SetColumns(columns, appendColumnsByDataFilter: true).Where(whereExpression).ExecuteCommand() > 0;
        }

        /// <summary>
        /// Update data (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        public async Task<bool> UpdateAsync(T updateObj, Expression<Func<T, object>>? updateColumns = null,
            CancellationToken cancellationToken = default, bool copyNew = false)
        {
            var backupFilters = BackupFilters();

            var dbNew = copyNew ? db.CopyNew() : db;
            dbNew.Ado.CancellationToken = cancellationToken;

            bool result;
            if (updateColumns != null)
            {
                if (updateObj is EntityBaseCreateInfo)
                {
                    result = await dbNew.Updateable(updateObj)
                        .UpdateColumns(updateColumns, true).UpdateColumns(ModifyColumn)
                        .ExecuteCommandAsync(cancellationToken) > 0;
                }
                else
                {
                    result = await dbNew.Updateable(updateObj).UpdateColumns(updateColumns, true)
                        .ExecuteCommandAsync(cancellationToken) > 0;
                }
            }
            else
            {
                result = await dbNew.Updateable(updateObj)
                    .ExecuteCommandAsync(cancellationToken) > 0;
            }

            RestoreFiltersIfChanged(backupFilters);
            return result;
        }

        /// <summary>
        /// Batch update data (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        public async Task<bool> UpdateRangeAsync(List<T> updateObjs, Expression<Func<T, object>>? updateColumns = null,
            CancellationToken cancellationToken = default)
        {
            var backupFilters = BackupFilters();

            db.Ado.CancellationToken = cancellationToken;
            bool result;

            if (updateColumns != null)
            {
                if (updateObjs.FirstOrDefault() is EntityBaseCreateInfo)
                {
                    result = await db.Updateable(updateObjs).UpdateColumns(ModifyColumn)
                        .UpdateColumns(updateColumns, true).UseParameter()
                        .ExecuteCommandAsync() > 0;
                }
                else
                {
                    result = await db.Updateable(updateObjs).UpdateColumns(updateColumns, true).UseParameter()
                        .ExecuteCommandAsync() > 0;
                }
            }
            else
            {
                result = await db.Updateable(updateObjs).UseParameter().ExecuteCommandAsync() > 0;
            }

            RestoreFiltersIfChanged(backupFilters);
            return result;
        }

        /// <summary>
        /// Batch update data (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        public async Task<bool> UpdateRangeAsync(T[] updateObjs, Expression<Func<T, object>>? updateColumns = null,
            CancellationToken cancellationToken = default)
        {
            // 将数组转换为列表，然后调用列表版本的方法
            return await UpdateRangeAsync(updateObjs.ToList(), updateColumns, cancellationToken);
        }

        /// <summary>
        /// Update data by condition (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        public async Task<bool> UpdateAsync(Expression<Func<T, T>> columns, Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default)
        {
            var backupFilters = BackupFilters();

            db.Ado.CancellationToken = cancellationToken;
            var result = await db.Updateable<T>().SetColumns(columns).Where(whereExpression).ExecuteCommandAsync() > 0;

            RestoreFiltersIfChanged(backupFilters);
            return result;
        }

        /// <summary>
        /// Update data by condition and set columns to True (async)
        /// </summary>
        /// <returns>Whether successful</returns>
        public async Task<bool> UpdateSetColumnsTrueAsync(Expression<Func<T, T>> columns, Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default)
        {
            var backupFilters = BackupFilters();

            db.Ado.CancellationToken = cancellationToken;
            var result = await db.Updateable<T>().SetColumns(columns, appendColumnsByDataFilter: true).Where(whereExpression).ExecuteCommandAsync() > 0;

            RestoreFiltersIfChanged(backupFilters);
            return result;
        }

        public async Task<bool> UpdateAsync(Dictionary<string, object> dict)
        {
            var backupFilters = BackupFilters();

            var result = await db.Updateable<T>(dict).WhereColumns(nameof(AbstractEntityBase.Id).ToSnakeCase()).ExecuteCommandAsync() > 0;

            RestoreFiltersIfChanged(backupFilters);
            return result;
        }

        #endregion Update

        #region Query

        /// <summary>
        /// Get data by id
        /// </summary>
        /// <returns>Return value</returns>
        public T GetById(object id, bool copyNew = false)
        {
            var dbNew = copyNew ? db.CopyNew() : db;
            return dbNew.Queryable<T>().InSingle(id);
        }

        public async Task<T> GetByIdAsync(object id, bool copyNew = false, CancellationToken cancellationToken = default)
        {
            db.Ado.CancellationToken = cancellationToken;
            var dbNew = copyNew ? db.CopyNew() : db;
            return await dbNew.Queryable<T>().InSingleAsync(id);
        }

        /// <summary>
        /// Get all data
        /// </summary>
        /// <returns>Return value</returns>
        public List<T> GetList()
        {
            return db.Queryable<T>().ToList();
        }

        /// <summary>
        /// Get data list by condition
        /// </summary>
        /// <returns>Return value</returns>
        public List<T> GetList(Expression<Func<T, bool>> whereExpression, bool copyNew = false)
        {
            var dbNew = copyNew ? db.CopyNew() : db;
            return dbNew.Queryable<T>().Where(whereExpression).ToList();
        }

        /// <summary>
        /// Get single data by condition
        /// </summary>
        /// <returns>Return value</returns>
        public T GetSingle(Expression<Func<T, bool>> whereExpression)
        {
            return db.Queryable<T>().Single(whereExpression);
        }

        /// <summary>
        /// Get first data by condition
        /// </summary>
        /// <returns>Return value</returns>
        public T GetFirst(Expression<Func<T, bool>> whereExpression, bool copyNew = false)
        {
            var dbNew = copyNew ? db.CopyNew() : db;
            return dbNew.Queryable<T>().First(whereExpression);
        }

        /// <summary>
        /// Get all data (async)
        /// </summary>
        /// <returns>Return value</returns>
        public virtual async Task<List<T>> GetListAsync(CancellationToken cancellationToken = default, bool copyNew = false)
        {
            var dbNew = copyNew ? db.CopyNew() : db;
            dbNew.Ado.CancellationToken = cancellationToken;

            // 检查并记录当前查询过滤器状态
            var filterCount = dbNew.QueryFilter.GeFilterList?.Count ?? 0;
            Console.WriteLine($"[BaseRepository] GetListAsync(all) executing with {filterCount} filters applied");

            // 如果是实体类型，检查租户过滤器是否存在
            if (typeof(AbstractEntityBase).IsAssignableFrom(typeof(T)))
            {
                Console.WriteLine($"[BaseRepository] Entity type {typeof(T).Name} should have tenant filters applied");

                // 检查当前 HttpContext 中的租户ID和应用代码
                var httpContext = new HttpContextAccessor().HttpContext;
                if (httpContext != null)
                {
                    var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                    var appCode = httpContext.Request.Headers["X-App-Code"].FirstOrDefault();
                    Console.WriteLine($"[BaseRepository] Current request headers: X-Tenant-Id={tenantId}, X-App-Code={appCode}");

                    // 备份过滤器状态
                    var backupFilters = BackupFilters();

                    // 获取实体类型
                    var entityType = typeof(T);
                    var propertyTenantId = entityType.GetProperty("TenantId");
                    var propertyAppCode = entityType.GetProperty("AppCode");

                    // 如果实体有 TenantId 和 AppCode 属性，添加显式过滤条件
                    if (propertyTenantId != null && propertyAppCode != null &&
                        !string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(appCode))
                    {
                        Console.WriteLine($"[BaseRepository] Adding explicit filter conditions: TenantId={tenantId}, AppCode={appCode}");

                        // 使用 SqlSugar 的 Where 方法添加显式过滤条件
                        var queryable = dbNew.Queryable<T>();
                        queryable = queryable.Where($"{propertyTenantId.Name} = @0 AND {propertyAppCode.Name} = @1", tenantId, appCode);
                        var result = await queryable.ToListAsync();

                        Console.WriteLine($"[BaseRepository] Query returned {result.Count} items");

                        // 恢复过滤器状态（如果发生变化）
                        RestoreFiltersIfChanged(backupFilters);
                        return result;
                    }
                }
            }

            return await dbNew.Queryable<T>().ToListAsync();
        }

        /// <summary>
        /// Get data list by condition (async)
        /// </summary>
        /// <returns>Return value</returns>
        public async Task<List<T>> GetListAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default, bool copyNew = false)
        {
            var dbNew = copyNew ? db.CopyNew() : db;
            dbNew.Ado.CancellationToken = cancellationToken;

            // 检查并记录当前查询过滤器状态
            var filterCount = dbNew.QueryFilter.GeFilterList?.Count ?? 0;
            Console.WriteLine($"[BaseRepository] GetListAsync executing with {filterCount} filters applied");

            // 如果是实体类型，检查租户过滤器是否存在
            if (typeof(AbstractEntityBase).IsAssignableFrom(typeof(T)))
            {
                Console.WriteLine($"[BaseRepository] Entity type {typeof(T).Name} should have tenant filters applied");

                // 检查当前 HttpContext 中的租户ID和应用代码
                var httpContext = new HttpContextAccessor().HttpContext;
                if (httpContext != null)
                {
                    var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                    var appCode = httpContext.Request.Headers["X-App-Code"].FirstOrDefault();
                    Console.WriteLine($"[BaseRepository] Current request headers: X-Tenant-Id={tenantId}, X-App-Code={appCode}");
                }

                // 备份过滤器状态
                var backupFilters = BackupFilters();
                var result = await dbNew.Queryable<T>().Where(whereExpression).ToListAsync();
                // 恢复过滤器状态（如果发生变化）
                RestoreFiltersIfChanged(backupFilters);
                return result;
            }

            return await dbNew.Queryable<T>().Where(whereExpression).ToListAsync();
        }

        /// <summary>
        /// Get data list by condition (async)
        /// </summary>
        /// <returns>Return value</returns>
        public async Task<List<T>> GetListAsync(Expression<Func<T, bool>> whereExpression, Expression<Func<T, object>> orderExpression, OrderByType orderByType = OrderByType.Asc, int top = 0, CancellationToken cancellationToken = default, bool copyNew = false)
        {
            var dbNew = copyNew ? db.CopyNew() : db;
            dbNew.Ado.CancellationToken = cancellationToken;
            var query = dbNew.Queryable<T>().Where(whereExpression);
            if (orderExpression != null)
            {
                query = query.OrderBy(orderExpression, orderByType);
            }
            if (top > 0)
            {
                query = query.Take(top);
            }
            return await query.ToListAsync();
        }
        /// <summary>
        /// Get data list by condition (specified columns) (async)
        /// </summary>
        /// <returns>Return value</returns>
        public async Task<List<T>> GetListAsync(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T>> selectedColumnExpression, Expression<Func<T, object>> orderExpression = null, OrderByType orderByType = OrderByType.Asc, CancellationToken cancellationToken = default)
        {
            db.Ado.CancellationToken = cancellationToken;
            if (orderExpression != null)
                return await db.Queryable<T>().Where(whereExpression).OrderBy(orderExpression, orderByType).ToListAsync();
            ISugarQueryable<T> query = db.Queryable<T>();
            if (selectedColumnExpression != null)
            {
                query.Select(selectedColumnExpression);
            }
            query = query.OrderByIF(orderExpression != null, orderExpression, orderByType);
            query = query.Where(whereExpression);
            return await query.ToListAsync();
        }
        /// <summary>
        /// Get single data by condition (async)
        /// </summary>
        /// <returns>Return value</returns>
        public async Task<T> GetSingleAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default)
        {
            db.Ado.CancellationToken = cancellationToken;
            return await db.Queryable<T>().SingleAsync(whereExpression);
        }

        /// <summary>
        /// Get first data by condition (async)
        /// </summary>
        /// <returns>Return value</returns>
        public async Task<T> GetFirstAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default, bool copyNew = false)
        {
            db.Ado.CancellationToken = cancellationToken;
            if (copyNew)
            {
                return await db.CopyNew().Queryable<T>().FirstAsync(whereExpression);
            }
            return await db.Queryable<T>().FirstAsync(whereExpression);
        }

        #endregion Query

        #region Pagination Query

        /// <summary>
        /// Pagination with sorting
        /// </summary>
        /// <param name="whereExpression">Query condition</param>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="TotalCount">Total count</param>
        /// <param name="orderByExpression">Sort condition</param>
        /// <param name="isAsc">Sort type (true: ascending, false: descending)</param>
        /// <param name="selectedColumnExpression">Specify columns to query</param>
        /// <returns>Result list</returns>
        public List<T> GetPageList(Expression<Func<T, bool>> whereExpression, int pageIndex, int pageSize, out int TotalCount, Expression<Func<T, object>> orderByExpression = null, bool isAsc = true, Expression<Func<T, T>> selectedColumnExpression = null)
        {
            return GetPageList(new List<Expression<Func<T, bool>>>() { whereExpression }, pageIndex, pageSize, out TotalCount, orderByExpression, isAsc, selectedColumnExpression);
        }

        /// <summary>
        /// Pagination (async) with sorting and cancellation support
        /// </summary>
        /// <param name="whereExpression">Query condition</param>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="orderByExpression">Sort condition</param>
        /// <param name="isAsc">Sort type (true: ascending, false: descending)</param>
        /// <param name="selectedColumnExpression">Specify columns to query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>(Result list, total count)</returns>
        public async Task<(List<T> datas, int total)> GetPageListAsync(Expression<Func<T, bool>> whereExpression, int pageIndex, int pageSize, Expression<Func<T, object>> orderByExpression = null, bool isAsc = true, Expression<Func<T, T>> selectedColumnExpression = null, CancellationToken cancellationToken = default)
        {
            return await GetPageListAsync(new List<Expression<Func<T, bool>>>() { whereExpression }, pageIndex, pageSize, orderByExpression, isAsc, selectedColumnExpression, cancellationToken);
        }

        /// <summary>
        /// Pagination with sorting
        /// </summary>
        /// <param name="whereExpressionList">Multi-condition dynamic joint query (suitable for query scenarios with multiple optional query items)</param>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="TotalCount">Total count</param>
        /// <param name="orderByExpression">Sort condition</param>
        /// <param name="isAsc">Sort type (true: ascending, false: descending)</param>
        /// <param name="selectedColumnExpression">Specify columns to query</param>
        /// <returns>Result list</returns>
        public List<T> GetPageList(List<Expression<Func<T, bool>>> whereExpressionList, int pageIndex, int pageSize, out int TotalCount, Expression<Func<T, object>> orderByExpression = null, bool isAsc = true, Expression<Func<T, T>> selectedColumnExpression = null)
        {
            PageModel page = new PageModel() { PageIndex = pageIndex, PageSize = pageSize };
            OrderByType orderByType = isAsc ? OrderByType.Asc : OrderByType.Desc;

            int count = 0;
            ISugarQueryable<T> query = db.Queryable<T>();
            if (selectedColumnExpression != null)
            {
                query.Select(selectedColumnExpression);
            }
            query = query.OrderByIF(orderByExpression != null, orderByExpression, orderByType);
            foreach (var item in whereExpressionList)
            {
                if (item != null)
                {
                    query = query.Where(item);
                }
            }
            List<T> results = query.ToPageList(page.PageIndex, page.PageSize, ref count);
            TotalCount = count;
            return results;
        }

        /// <summary>
        /// Pagination (async) with sorting and cancellation support
        /// </summary>
        /// <param name="whereExpressionList">Multi-condition dynamic joint query (suitable for query scenarios with multiple optional query items)</param>
        /// <param name="pageIndex">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="orderByExpression">Sort condition</param>
        /// <param name="isAsc">Sort type (true: ascending, false: descending)</param>
        /// <param name="selectedColumnExpression">Specify columns to query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>(Result list, total count)</returns>
        public async Task<(List<T> datas, int total)> GetPageListAsync(List<Expression<Func<T, bool>>> whereExpressionList, int pageIndex, int pageSize, Expression<Func<T, object>> orderByExpression = null, bool isAsc = true, Expression<Func<T, T>> selectedColumnExpression = null, CancellationToken cancellationToken = default)
        {
            db.Ado.CancellationToken = cancellationToken;

            PageModel page = new PageModel() { PageIndex = pageIndex, PageSize = pageSize };
            OrderByType orderByType = isAsc ? OrderByType.Asc : OrderByType.Desc;

            RefAsync<int> count = 0;
            ISugarQueryable<T> query = db.Queryable<T>();
            if (selectedColumnExpression != null)
            {
                query.Select(selectedColumnExpression);
            }
            query = query.OrderByIF(orderByExpression != null, orderByExpression, orderByType);
            foreach (var item in whereExpressionList)
            {
                if (item != null)
                {
                    query = query.Where(item);
                }
            }
            List<T> results = await query.ToPageListAsync(page.PageIndex, page.PageSize, count);
            page.TotalCount = count.Value;
            return (results, page.TotalCount);
        }

        public async Task<(List<T> datas, int total)> GetPageListAsync(List<Expression<Func<T, bool>>> whereExpressionList, int pageIndex, int pageSize, string orderPropertyName, bool isAsc = true, Expression<Func<T, T>> selectedColumnExpression = null, CancellationToken cancellationToken = default)
        {
            db.Ado.CancellationToken = cancellationToken;

            PageModel page = new PageModel() { PageIndex = pageIndex, PageSize = pageSize };
            OrderByType orderByType = isAsc ? OrderByType.Asc : OrderByType.Desc;

            RefAsync<int> count = 0;
            ISugarQueryable<T> query = db.Queryable<T>();
            if (selectedColumnExpression != null)
            {
                query.Select(selectedColumnExpression);
            }

            if (!string.IsNullOrEmpty(orderPropertyName))
            {
                query = query.OrderByPropertyName(orderPropertyName, orderByType);
            }

            foreach (var item in whereExpressionList)
            {
                if (item != null)
                {
                    query = query.Where(item);
                }
            }
            List<T> results = await query.ToPageListAsync(page.PageIndex, page.PageSize, count);
            page.TotalCount = count.Value;
            return (results, page.TotalCount);
        }

        #endregion Pagination Query

        #region Delete (Soft Delete Implementation)

        /// <summary>
        /// Delete data using soft delete (sets IsValid = false)
        /// </summary>
        /// <returns>Whether successful</returns>
        public bool Delete(T deleteObj)
        {
            return PerformSoftDelete(deleteObj);
        }

        /// <summary>
        /// Delete data list using soft delete (sets IsValid = false)
        /// </summary>
        /// <returns>Whether successful</returns>
        public bool Delete(List<T> deleteObjs)
        {
            return PerformSoftDelete(deleteObjs);
        }

        /// <summary>
        /// Delete data by condition using soft delete (sets IsValid = false)
        /// </summary>
        /// <returns>Whether successful</returns>
        public bool Delete(Expression<Func<T, bool>> whereExpression)
        {
            return PerformSoftDeleteByCondition(whereExpression);
        }

        /// <summary>
        /// Delete data by ID using soft delete (sets IsValid = false)
        /// </summary>
        /// <returns>Whether successful</returns>
        public bool DeleteById(object id)
        {
            var entity = GetById(id);
            if (entity == null) return false;
            return PerformSoftDelete(entity);
        }

        /// <summary>
        /// Delete data (async) using soft delete (sets IsValid = false)
        /// </summary>
        /// <returns>Whether successful</returns>
        public async Task<bool> DeleteAsync(T deleteObj, CancellationToken cancellationToken = default)
        {
            return await PerformSoftDeleteAsync(deleteObj, cancellationToken);
        }

        /// <summary>
        /// Batch delete data (async) using soft delete (sets IsValid = false)
        /// </summary>
        /// <returns>Whether successful</returns>
        public async Task<bool> DeleteAsync(List<T> deleteObjs, CancellationToken cancellationToken = default)
        {
            return await PerformSoftDeleteAsync(deleteObjs, cancellationToken);
        }

        /// <summary>
        /// Delete data by condition (async) using soft delete (sets IsValid = false)
        /// </summary>
        /// <returns>Whether successful</returns>
        public async Task<bool> DeleteAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default)
        {
            return await PerformSoftDeleteByConditionAsync(whereExpression, cancellationToken);
        }

        /// <summary>
        /// Delete data by ID (async) using soft delete (sets IsValid = false)
        /// </summary>
        /// <returns>Whether successful</returns>
        public async Task<bool> DeleteByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            var entity = await GetByIdAsync(id, cancellationToken: cancellationToken);
            if (entity == null) return false;
            return await PerformSoftDeleteAsync(entity, cancellationToken);
        }

        /// <summary>
        /// Delete data by IDs (async) using soft delete (sets IsValid = false)
        /// </summary>
        /// <returns>Whether successful</returns>
        public async Task<bool> DeleteByIdsAsync(dynamic[] ids, CancellationToken cancellationToken = default)
        {
            var entities = new List<T>();
            foreach (var id in ids)
            {
                var entity = await GetByIdAsync(id, cancellationToken: cancellationToken);
                if (entity != null)
                {
                    entities.Add(entity);
                }
            }
            return await PerformSoftDeleteAsync(entities, cancellationToken);
        }

        #region Soft Delete Helper Methods

        /// <summary>
        /// Perform soft delete on a single entity
        /// </summary>
        private bool PerformSoftDelete(T entity)
        {
            try
            {
                // Check if entity supports soft delete
                if (entity is FlowFlex.Domain.Abstracts.ISoftDeletable softDeletableEntity)
                {
                    softDeletableEntity.IsValid = false;
                    softDeletableEntity.ModifyDate = DateTimeOffset.UtcNow;
                    return Update(entity);
                }
                else
                {
                    // Try to use reflection to check for IsValid property
                    var isValidProperty = typeof(T).GetProperty("IsValid");
                    var modifyDateProperty = typeof(T).GetProperty("ModifyDate");

                    if (isValidProperty != null && isValidProperty.CanWrite)
                    {
                        isValidProperty.SetValue(entity, false);
                        if (modifyDateProperty != null && modifyDateProperty.CanWrite)
                        {
                            modifyDateProperty.SetValue(entity, DateTimeOffset.UtcNow);
                        }
                        return Update(entity);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Entity type {typeof(T).Name} does not support soft delete. Please implement ISoftDeletable interface or add IsValid property.");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to perform soft delete on entity {typeof(T).Name}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Perform soft delete on multiple entities
        /// </summary>
        private bool PerformSoftDelete(List<T> entities)
        {
            try
            {
                foreach (var entity in entities)
                {
                    if (entity is FlowFlex.Domain.Abstracts.ISoftDeletable softDeletableEntity)
                    {
                        softDeletableEntity.IsValid = false;
                        softDeletableEntity.ModifyDate = DateTimeOffset.UtcNow;
                    }
                    else
                    {
                        // Try to use reflection to check for IsValid property
                        var isValidProperty = typeof(T).GetProperty("IsValid");
                        var modifyDateProperty = typeof(T).GetProperty("ModifyDate");

                        if (isValidProperty != null && isValidProperty.CanWrite)
                        {
                            isValidProperty.SetValue(entity, false);
                            if (modifyDateProperty != null && modifyDateProperty.CanWrite)
                            {
                                modifyDateProperty.SetValue(entity, DateTimeOffset.UtcNow);
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException($"Entity type {typeof(T).Name} does not support soft delete. Please implement ISoftDeletable interface or add IsValid property.");
                        }
                    }
                }
                return UpdateRange(entities);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to perform soft delete on entities {typeof(T).Name}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Perform soft delete by condition
        /// </summary>
        private bool PerformSoftDeleteByCondition(Expression<Func<T, bool>> whereExpression)
        {
            try
            {
                // Check if entity supports soft delete
                var isValidProperty = typeof(T).GetProperty("IsValid");
                var modifyDateProperty = typeof(T).GetProperty("ModifyDate");

                if (isValidProperty != null && isValidProperty.CanWrite)
                {
                    var backupFilters = BackupFilters();
                    var result = db.Updateable<T>()
                        .SetColumns("is_valid", false)
                        .SetColumns("modify_date", DateTimeOffset.UtcNow)
                        .Where(whereExpression)
                        .ExecuteCommand() > 0;
                    RestoreFiltersIfChanged(backupFilters);
                    return result;
                }
                else
                {
                    throw new InvalidOperationException($"Entity type {typeof(T).Name} does not support soft delete. Please implement ISoftDeletable interface or add IsValid property.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to perform soft delete by condition on entity {typeof(T).Name}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Perform soft delete on a single entity (async)
        /// </summary>
        private async Task<bool> PerformSoftDeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if entity supports soft delete
                if (entity is FlowFlex.Domain.Abstracts.ISoftDeletable softDeletableEntity)
                {
                    softDeletableEntity.IsValid = false;
                    softDeletableEntity.ModifyDate = DateTimeOffset.UtcNow;
                    return await UpdateAsync(entity, cancellationToken: cancellationToken);
                }
                else
                {
                    // Try to use reflection to check for IsValid property
                    var isValidProperty = typeof(T).GetProperty("IsValid");
                    var modifyDateProperty = typeof(T).GetProperty("ModifyDate");

                    if (isValidProperty != null && isValidProperty.CanWrite)
                    {
                        isValidProperty.SetValue(entity, false);
                        if (modifyDateProperty != null && modifyDateProperty.CanWrite)
                        {
                            modifyDateProperty.SetValue(entity, DateTimeOffset.UtcNow);
                        }
                        return await UpdateAsync(entity, cancellationToken: cancellationToken);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Entity type {typeof(T).Name} does not support soft delete. Please implement ISoftDeletable interface or add IsValid property.");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to perform soft delete on entity {typeof(T).Name}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Perform soft delete on multiple entities (async)
        /// </summary>
        private async Task<bool> PerformSoftDeleteAsync(List<T> entities, CancellationToken cancellationToken = default)
        {
            try
            {
                foreach (var entity in entities)
                {
                    if (entity is FlowFlex.Domain.Abstracts.ISoftDeletable softDeletableEntity)
                    {
                        softDeletableEntity.IsValid = false;
                        softDeletableEntity.ModifyDate = DateTimeOffset.UtcNow;
                    }
                    else
                    {
                        // Try to use reflection to check for IsValid property
                        var isValidProperty = typeof(T).GetProperty("IsValid");
                        var modifyDateProperty = typeof(T).GetProperty("ModifyDate");

                        if (isValidProperty != null && isValidProperty.CanWrite)
                        {
                            isValidProperty.SetValue(entity, false);
                            if (modifyDateProperty != null && modifyDateProperty.CanWrite)
                            {
                                modifyDateProperty.SetValue(entity, DateTimeOffset.UtcNow);
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException($"Entity type {typeof(T).Name} does not support soft delete. Please implement ISoftDeletable interface or add IsValid property.");
                        }
                    }
                }
                return await UpdateRangeAsync(entities, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to perform soft delete on entities {typeof(T).Name}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Perform soft delete by condition (async)
        /// </summary>
        private async Task<bool> PerformSoftDeleteByConditionAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if entity supports soft delete
                var isValidProperty = typeof(T).GetProperty("IsValid");
                var modifyDateProperty = typeof(T).GetProperty("ModifyDate");

                if (isValidProperty != null && isValidProperty.CanWrite)
                {
                    var backupFilters = BackupFilters();
                    db.Ado.CancellationToken = cancellationToken;
                    var result = await db.Updateable<T>()
                        .SetColumns("is_valid", false)
                        .SetColumns("modify_date", DateTimeOffset.UtcNow)
                        .Where(whereExpression)
                        .ExecuteCommandAsync() > 0;
                    RestoreFiltersIfChanged(backupFilters);
                    return result;
                }
                else
                {
                    throw new InvalidOperationException($"Entity type {typeof(T).Name} does not support soft delete. Please implement ISoftDeletable interface or add IsValid property.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to perform soft delete by condition on entity {typeof(T).Name}: {ex.Message}", ex);
            }
        }

        #endregion Soft Delete Helper Methods

        #endregion Delete

        #region Existence Check

        /// <summary>
        /// Check if data exists based on condition
        /// </summary>
        /// <returns>Return value</returns>
        public bool IsAny(Expression<Func<T, bool>> whereExpression)
        {
            return db.Queryable<T>().Where(whereExpression).Any();
        }

        /// <summary>
        /// Check if data exists based on condition (async)
        /// </summary>
        /// <returns>Whether exists</returns>
        public async Task<bool> IsAnyAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default, bool copyNew = false)
        {
            var dbNew = copyNew ? db.CopyNew() : db;
            dbNew.Ado.CancellationToken = cancellationToken;
            return await dbNew.Queryable<T>().Where(whereExpression).AnyAsync();
        }

        #endregion Existence Check

        #region Count Data

        /// <summary>
        /// Count data based on condition
        /// </summary>
        /// <returns>Return value</returns>
        public int Count(Expression<Func<T, bool>> whereExpression)
        {
            return db.Queryable<T>().Where(whereExpression).Count();
        }

        /// <summary>
        /// Count data based on condition (async)
        /// </summary>
        /// <returns>Count</returns>
        public async Task<int> CountAsync(Expression<Func<T, bool>> whereExpression, CancellationToken cancellationToken = default)
        {
            db.Ado.CancellationToken = cancellationToken;
            return await db.Queryable<T>().Where(whereExpression).CountAsync();
        }

        #endregion Count Data

        #region Transaction

        /// <summary>
        /// Begin transaction
        /// </summary>
        public void BeginTran()
        {
            db.AsTenant().BeginTran();
        }

        /// <summary>
        /// Commit transaction
        /// </summary>
        public void CommitTran()
        {
            db.AsTenant().CommitTran();
        }

        /// <summary>
        /// Rollback transaction
        /// </summary>
        public void RollbackTran()
        {
            db.AsTenant().RollbackTran();
        }

        /// <summary>
        /// Execute transaction, async
        /// </summary>
        /// <param name="action">All operations to execute</param>
        /// <returns>Whether execution succeeded</returns>
        public async Task<bool> UseTranAsync(Func<Task> action)
        {
            try
            {
                // Begin transaction
                await db.AsTenant().BeginTranAsync();

                // Execute operations
                await action();

                // Commit transaction
                await db.AsTenant().CommitTranAsync();
                return true;
            }
            catch (Exception ex)
            {
                // If operation fails, rollback transaction
                await db.AsTenant().RollbackTranAsync();
                // After rollback, need to continue throwing the exception that occurred in the transaction
                throw;
            }
            /*
             *  Usage:
                 await UseTranAsync(async () =>
                {
                // Operations to execute in transaction
                // For example:
                // await db.InsertAsync(newEntity);
                // await db.UpdateAsync(existingEntity);
                });
            */
        }

        /// <summary>
        /// Execute transaction and get specified return value
        /// </summary>
        /// <param name="action">All operations to execute</param>
        /// <returns>Return value</returns>
        public (ReturnValueT result, bool success) UseTranReturnValue<ReturnValueT>(Func<ReturnValueT> action)
        {
            try
            {
                // Begin transaction
                db.AsTenant().BeginTran();

                // Execute operations
                var backValue = action();

                // Commit transaction
                db.AsTenant().CommitTran();

                // Return operation result and success flag
                return (backValue, true);
            }
            catch (Exception ex)
            {
                // If operation fails, rollback transaction
                db.AsTenant().RollbackTran();
                // Log exception information
                // Exception logging is handled by global exception middleware
                // Debug logging handled by structured logging
                // Throw exception directly
                throw;
            }
        }

        /// <summary>
        /// Execute transaction and get specified return value, async
        /// </summary>
        /// <param name="action">All operations to execute</param>
        /// <returns>Return value, whether execution succeeded</returns>
        public async Task<(ReturnValueT result, bool success)> UseTranReturnValueAsync<ReturnValueT>(Func<Task<ReturnValueT>> action)
        {
            try
            {
                // Begin transaction
                await db.AsTenant().BeginTranAsync();

                // Execute operations and get return value
                var backValue = await action();

                // Commit transaction
                await db.AsTenant().CommitTranAsync();

                // Return operation result and success flag
                return (backValue, true);
            }
            catch (Exception ex)
            {
                // If operation fails, rollback transaction
                await db.AsTenant().RollbackTranAsync();
                // Throw exception directly
                throw;
            }
        }

        /// <summary>
        /// Execute transaction
        /// </summary>
        /// <param name="action">All operations to execute</param>
        /// <returns>Whether execution was successful</returns>
        public bool UseTran(Action action)
        {
            try
            {
                BeginTran();
                action();
                CommitTran();
                return true;
            }
            catch
            {
                RollbackTran();
                return false;
            }
        }

        #endregion Transaction

        #region Cannot use async transactions this way, it will be ineffective

        // Begin transaction
        //public async Task BeginTranAsync()
        //{
        //    await db.AsTenant().BeginTranAsync();
        //}
        // Commit transaction
        //public async Task CommitTranAsync()
        //{
        //    await db.AsTenant().CommitTranAsync();
        //}
        // Rollback transaction
        //public async Task RollbackTranAsync()
        //{
        //    await db.AsTenant().RollbackTranAsync();
        //}

        #endregion Cannot use async transactions this way, it will be ineffective


        public void QueryFilterClearAndBackup()
        {
            db.QueryFilter.ClearAndBackup();
        }

        public void QueryFilterClear()
        {
            db.QueryFilter.Clear();
        }

        public void QueryFilterRestore()
        {
            db.QueryFilter.Restore();
        }

        public ISugarQueryable<T> ClearFilter()
        {
            return db.Queryable<T>().ClearFilter();
        }

        public ISugarQueryable<T> ClearFilter<IFilter>()
        {
            return db.Queryable<T>().ClearFilter<IFilter>();
        }

        public SugarUnitOfWork CreateContext()
        {
            return db.CreateContext(db.Ado.IsNoTran());
        }

        #region This method is specifically for SqlSugar official filter loss bug

        /// <summary>
        /// Backup current filter state (This method is specifically for SqlSugar official filter loss bug)
        /// </summary>
        /// <returns>Backup filter list, returns null if no filters</returns>
        protected List<SqlFilterItem> BackupFilters()
        {
            try
            {
                // Get GeFilterList reflection property
                var geFilterListProperty = db.QueryFilter.GetType().GetProperty("GeFilterList");

                if (geFilterListProperty == null)
                {
                    return null;
                }

                var geFilterList = geFilterListProperty.GetValue(db.QueryFilter) as List<SqlFilterItem>;

                if (geFilterList == null || !geFilterList.Any())
                {
                    return null;
                }

                // Return a copy of the filters
                return new List<SqlFilterItem>(geFilterList);
            }
            catch
            {
                // Return null instead of throwing exception to prevent crash
                return null;
            }
        }

        /// <summary>
        /// Check and restore filter state (This method is specifically for SqlSugar official filter loss bug)
        /// </summary>
        /// <param name="backupFilters">Backup filter list</param>
        /// <returns>Returns true if filters were restored</returns>
        protected bool RestoreFiltersIfChanged(List<SqlFilterItem> backupFilters)
        {
            try
            {
                var geFilterListProperty = db.QueryFilter.GetType().GetProperty("GeFilterList");

                if (geFilterListProperty == null)
                {
                    return false;
                }

                var geFilterList = geFilterListProperty.GetValue(db.QueryFilter) as List<SqlFilterItem>;

                if (backupFilters == null)
                {
                    return false;
                }

                // Check if filters have changed
                bool filtersChanged = false;
                // Check if filters completely disappeared
                if (geFilterList == null || !geFilterList.Any())
                {
                    filtersChanged = true;
                }
                // Check if filter count decreased
                else if (geFilterList.Count != backupFilters.Count)
                {
                    filtersChanged = true;
                }

                // If filters changed, restore backup
                if (filtersChanged)
                {
                    geFilterList?.Clear();
                    foreach (var filter in backupFilters)
                    {
                        geFilterList?.Add(filter);
                    }
                    return true;
                }

                return false;
            }
            catch
            {
                // Return false instead of throwing exception to prevent crash
                return false;
            }
        }

        #endregion This method is specifically for SqlSugar official filter loss bug
    }
}
