using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    public interface IPluginPriceListRepository : IOwBaseRepository<PluginPriceList>
    {
        Task<PluginPriceList?> GetByCaseCodeAsync(string caseCode);
    }
}
