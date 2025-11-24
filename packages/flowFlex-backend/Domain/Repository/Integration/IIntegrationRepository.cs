using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository;

namespace FlowFlex.Domain.Repository.Integration
{
    /// <summary>
    /// Integration repository interface
    /// </summary>
    public interface IIntegrationRepository : IBaseRepository<Entities.Integration.Integration>
    {
        /// <summary>
        /// Query integrations with pagination
        /// </summary>
        Task<(List<Entities.Integration.Integration> items, int total)> QueryPagedAsync(
            int pageIndex,
            int pageSize,
            string name = null,
            string type = null,
            string status = null,
            string sortField = "CreateDate",
            string sortDirection = "desc");

        /// <summary>
        /// Get integration by name
        /// </summary>
        Task<Entities.Integration.Integration> GetByNameAsync(string name);

        /// <summary>
        /// Check if integration name exists
        /// </summary>
        Task<bool> ExistsNameAsync(string name, long? excludeId = null);

        /// <summary>
        /// Get integration with all related data (mappings, configurations, actions)
        /// </summary>
        Task<Entities.Integration.Integration> GetWithDetailsAsync(long id);

        /// <summary>
        /// Get integrations by type
        /// </summary>
        Task<List<Entities.Integration.Integration>> GetByTypeAsync(string type);

        /// <summary>
        /// Get integrations by status
        /// </summary>
        Task<List<Entities.Integration.Integration>> GetByStatusAsync(string status);

        /// <summary>
        /// Get all active integrations
        /// </summary>
        Task<List<Entities.Integration.Integration>> GetActiveIntegrationsAsync();
    }
}

