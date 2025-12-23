using FlowFlex.Domain.Entities.Integration;
using FlowFlex.Domain.Repository;

namespace FlowFlex.Domain.Repository.Integration
{
    /// <summary>
    /// Quick link repository interface
    /// </summary>
    public interface IQuickLinkRepository : IBaseRepository<QuickLink>
    {
        /// <summary>
        /// Get quick links by integration ID
        /// </summary>
        Task<List<QuickLink>> GetByIntegrationIdAsync(long integrationId);

        /// <summary>
        /// Get quick link by label
        /// </summary>
        Task<QuickLink> GetByLabelAsync(long integrationId, string label);

        /// <summary>
        /// Check if quick link label exists
        /// </summary>
        Task<bool> ExistsLabelAsync(long integrationId, string label, long? excludeId = null);

        /// <summary>
        /// Delete quick links by integration ID
        /// </summary>
        Task<bool> DeleteByIntegrationIdAsync(long integrationId);

        /// <summary>
        /// Reorder quick links
        /// </summary>
        Task<bool> ReorderAsync(List<(long id, int displayOrder)> orders);

        /// <summary>
        /// Get all quick links
        /// </summary>
        Task<List<QuickLink>> GetAllAsync();
    }
}

