using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.StageCondition;
using FlowFlex.Domain.Shared;
using Newtonsoft.Json.Linq;

namespace FlowFlex.Application.Contracts.IServices.Action
{
    public interface IActionContextBuilder : IScopedService
    {
        Task<Dictionary<string, object>> BuildStageConditionTriggerContextAsync(
            ActionExecutionContext context,
            long actionDefinitionId,
            long? integrationId,
            JToken? previousActionResult = null,
            CancellationToken cancellationToken = default);
    }
}
