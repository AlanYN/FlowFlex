using FlowFlex.Domain.Entities.OW;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Repository interface for WorkflowTriggerConnection
    /// </summary>
    public interface IWorkflowTriggerConnectionRepository : IBaseRepository<WorkflowTriggerConnection>
    {
        /// <summary>
        /// Get all connections belonging to a graph
        /// </summary>
        Task<List<WorkflowTriggerConnection>> GetByGraphIdAsync(long graphId);

        /// <summary>
        /// Delete all connections of a graph (used when resaving the full graph)
        /// </summary>
        Task<bool> DeleteByGraphIdAsync(long graphId);

        /// <summary>
        /// Get all connections where source or target is the given workflow
        /// </summary>
        Task<List<WorkflowTriggerConnection>> GetByWorkflowIdAsync(long workflowId);
    }
}
