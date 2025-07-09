using SqlSugar;

namespace FlowFlex.SqlSugarDB.Context
{
    /// <summary>
    /// SqlSugar database context implementation
    /// </summary>
    public class SqlSugarContext : ISqlSugarContext
    {
        private readonly ISqlSugarClient _sqlSugarClient;

        public SqlSugarContext(ISqlSugarClient sqlSugarClient)
        {
            _sqlSugarClient = sqlSugarClient;
        }

        /// <summary>
        /// Get database client
        /// </summary>
        /// <returns>Database client</returns>
        public ISqlSugarClient GetDbClient()
        {
            return _sqlSugarClient;
        }
    }
}
