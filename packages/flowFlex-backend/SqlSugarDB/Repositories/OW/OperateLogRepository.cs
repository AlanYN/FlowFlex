using FlowFlex.Domain.Entities.Item;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.SqlSugarDB.Implements;
using SqlSugar;
using FlowFlex.Domain.Shared;

namespace FlowFlex.SqlSugarDB.Repositories.OW
{
    public class OperateLogRepository : BaseRepository<OperateLog>, IOperateLogRepository, IScopedService
    {
        public OperateLogRepository(ISqlSugarClient context) : base(context)
        {
        }
    }
} 
