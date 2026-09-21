using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowFlex.Domain.Repository.OW
{
    public interface IWorkflowTriggerLogRepository : IBaseRepository<WorkflowTriggerLog>
    {
        Task<List<WorkflowTriggerLog>> GetBySourceOnboardingIdAsync(long sourceOnboardingId);
        Task<List<WorkflowTriggerLog>> GetByTargetOnboardingIdAsync(long targetOnboardingId);
        Task<(List<TriggerLogOutputDto> items, int total)> GetPagedByWorkflowAsync(
            long sourceWorkflowId, int pageIndex, int pageSize, string? status = null);
        Task<bool> HasAlreadyTriggeredAsync(long connectionId, long sourceOnboardingId);
    }
}
