using SqlSugar;

namespace FlowFlex.SqlSugarDB.Context
{
    /// <summary>
    /// SqlSugar工作单元接口
    /// </summary>
    public interface ISugarUnitOfWork : SqlSugar.ISugarUnitOfWork
    {
        /// <summary>
        /// 获取数据库上下文
        /// </summary>
        /// <returns>数据库上下文</returns>
        ISqlSugarClient GetDbContext();
    }
}