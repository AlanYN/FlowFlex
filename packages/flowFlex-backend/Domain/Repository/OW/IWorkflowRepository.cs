using FlowFlex.Domain.Repository;
using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Workflow repository interface
    /// </summary>
    public interface IWorkflowRepository : IBaseRepository<Workflow>
    {
        /// <summary>
        /// Query workflows with pagination
        /// </summary>
        Task<(List<Workflow> items, int total)> QueryPagedAsync(int pageIndex, int pageSize, string name = null, bool? isActive = null);

        /// <summary>
        /// Get workflow by name
        /// </summary>
        Task<Workflow> GetByNameAsync(string name);

        /// <summary>
        /// Get default workflow
        /// </summary>
        Task<Workflow> GetDefaultWorkflowAsync();

        /// <summary>
        /// Set default workflow (cancel other default status)
        /// </summary>
        Task<bool> SetDefaultWorkflowAsync(long workflowId);

        /// <summary>
        /// Remove default workflow
        /// </summary>
        Task<bool> RemoveDefaultWorkflowAsync(long workflowId);



        /// <summary>
        /// Get workflow including stages
        /// </summary>
        Task<Workflow> GetWithStagesAsync(long id);

        /// <summary>
        /// Check if workflow name exists
        /// </summary>
        Task<bool> ExistsNameAsync(string name, long? excludeId = null);

        /// <summary>
        /// Batch update workflow status
        /// </summary>
        Task<bool> BatchUpdateStatusAsync(List<long> ids, string status);

        /// <summary>
        /// Get active workflow list
        /// </summary>
        Task<List<Workflow>> GetActiveWorkflowsAsync();

        /// <summary>
        /// Get all workflows list (including stage information)
        /// </summary>
        Task<List<Workflow>> GetAllWorkflowsAsync();

        /// <summary>
        /// Get all workflows (optimized version, returns only necessary fields)
        /// </summary>
        Task<List<Workflow>> GetAllOptimizedAsync();

        /// <summary>
        /// Get expired active workflows
        /// </summary>
        Task<List<Workflow>> GetExpiredActiveWorkflowsAsync();

        /// <summary>
        /// Get workflows that are about to expire (remind ahead by specified days)
        /// </summary>
        Task<List<Workflow>> GetExpiringWorkflowsAsync(int daysAhead = 7);
    }
}
