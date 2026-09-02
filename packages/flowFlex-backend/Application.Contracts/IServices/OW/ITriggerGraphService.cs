using FlowFlex.Application.Contracts.Dtos.OW.TriggerGraph;
using FlowFlex.Application.Contracts.Dtos.OW.Workflow;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Service for Workflow Trigger Graph (OW-723 / OW-725)
    /// </summary>
    public interface ITriggerGraphService
    {
        /// <summary>
        /// Get (or create) the trigger graph for the given workflow.
        /// Returns an empty graph if none has been saved yet.
        /// </summary>
        Task<TriggerGraphDto> GetByWorkflowIdAsync(long workflowId);

        /// <summary>
        /// Create-or-update (upsert) the trigger graph for a workflow.
        /// All connections are replaced atomically.
        /// </summary>
        Task<TriggerGraphDto> SaveAsync(SaveTriggerGraphInput input);

        // ─── OW-725 query interfaces ──────────────────────────────────────

        /// <summary>
        /// Get all workflows (id + name + status) for the trigger graph left panel.
        /// </summary>
        Task<List<WorkflowOutputDto>> GetAllWorkflowsAsync();

        /// <summary>
        /// Get detailed node info for a workflow:
        /// stages → fields / questionnaire questions / checklist tasks.
        /// Used for condition configuration in the connection panel.
        /// </summary>
        Task<WorkflowNodeInfoDto> GetWorkflowNodeInfoAsync(long workflowId);
    }
}
