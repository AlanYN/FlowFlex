using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.SqlSugarDB.Context;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    public class PluginPriceListRepository : OwBaseRepository<PluginPriceList>, IPluginPriceListRepository, IScopedService
    {
        public PluginPriceListRepository(ISqlSugarContext context) : base(context)
        {
        }

        public async Task<PluginPriceList?> GetByCaseCodeAsync(string caseCode)
        {
            return await _db.Queryable<PluginPriceList>()
                .Where(x => x.CaseCode == caseCode && x.IsValid)
                .FirstAsync();
        }
    }
}
