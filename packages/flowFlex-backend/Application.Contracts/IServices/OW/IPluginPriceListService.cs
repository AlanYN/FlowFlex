using FlowFlex.Application.Contracts.Dtos.OW.PluginPriceList;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    public interface IPluginPriceListService : IScopedService
    {
        Task<PluginPriceListOutputDto> GetAsync(string caseCode);
        Task<object> SaveAsync(PluginPriceListInputDto input);
        Task<object> SubmitAsync(PluginPriceListSubmitDto input);
    }
}
