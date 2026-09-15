using FlowFlex.Domain.Entities.OW;
using System.Threading.Tasks;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Repository interface for WorkflowTriggerGraph
    /// </summary>
    public interface IWorkflowTriggerGraphRepository : IBaseRepository<WorkflowTriggerGraph>
    {
        /// <summary>
        /// Get the trigger graph owned by the given workflow (null if not created yet)
        /// </summary>
        Task<WorkflowTriggerGraph> GetByWorkflowIdAsync(long workflowId);
    }
}
