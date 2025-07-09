using SqlSugar;

namespace FlowFlex.SqlSugarDB.Context
{
    /// <summary>
    /// SqlSugar database context interface
    /// </summary>
    public interface ISqlSugarContext
    {
        /// <summary>
        /// Get database client
        /// </summary>
        /// <returns>Database client</returns>
        ISqlSugarClient GetDbClient();
    }
}
