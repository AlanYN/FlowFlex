using FlowFlex.Domain.Shared.Enums;
using FlowFlex.Domain.Shared.Models;
using System.Threading.Tasks;

namespace FlowFlex.Application.Contracts;

public interface IRequestDataRecordService
{
    Task<long> InsertRequestRecordAsync(RequestDataRecordModel model);

    Task UpdateResponseAsync(long id, string response, long? referenceId);

    Task<string> QueryRequestDataAsync(long referenceId, RecordSource? source = null, string type = null);
}
