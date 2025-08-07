using SqlSugar;

namespace FlowFlex.SqlSugarDB.Context
{
    /// <summary>
    /// 数据库迁移操作接口
    /// </summary>
    public interface IMigrationOperation
    {
        /// <summary>
        /// 执行顺序，数字越小越先执行
        /// </summary>
        int Order { get; }

        /// <summary>
        /// 迁移名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 执行迁移操作
        /// </summary>
        /// <param name="unitOfWork">工作单元</param>
        void Execute(ISugarUnitOfWork unitOfWork);
    }
} 