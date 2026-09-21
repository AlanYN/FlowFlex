using System.Threading.Tasks;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Evaluates trigger connections and creates downstream Cases when a source Case completes.
    /// Called asynchronously via background queue so the original HTTP request is not blocked.
    /// </summary>
    public interface ITriggerExecutionService
    {
        /// <summary>
        /// Evaluate all outbound connections for the given source workflow and, for every connection
        /// whose conditions are satisfied, create a downstream Case and apply Data Mapping.
        /// </summary>
        /// <param name="sourceOnboardingId">The Case that just completed.</param>
        /// <param name="sourceWorkflowId">Workflow of the completed Case.</param>
        /// <param name="completionType">"Completed" or "ForceCompleted".</param>
        /// <param name="tenantId">Optional explicit TenantId — required in background tasks where UserContext may not be populated.</param>
        /// <param name="appCode">Optional explicit AppCode — required in background tasks where UserContext may not be populated.</param>
        /// <param name="operatorId">Optional operator user ID — for audit fields when called from background tasks.</param>
        /// <param name="operatorName">Optional operator display name — for audit fields when called from background tasks.</param>
        Task ExecuteTriggersAsync(
            long sourceOnboardingId,
            long sourceWorkflowId,
            string completionType,
            string? tenantId = null,
            string? appCode = null,
            string? operatorId = null,
            string? operatorName = null);
    }
}
