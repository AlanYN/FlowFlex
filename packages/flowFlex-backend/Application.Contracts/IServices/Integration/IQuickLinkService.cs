using FlowFlex.Application.Contracts.Dtos.Integration;

namespace FlowFlex.Application.Contracts.IServices.Integration
{
    /// <summary>
    /// Quick link service interface
    /// </summary>
    public interface IQuickLinkService
    {
        /// <summary>
        /// Create a new quick link
        /// </summary>
        Task<long> CreateAsync(QuickLinkInputDto input);

        /// <summary>
        /// Update an existing quick link
        /// </summary>
        Task<bool> UpdateAsync(long id, QuickLinkInputDto input);

        /// <summary>
        /// Delete a quick link
        /// </summary>
        Task<bool> DeleteAsync(long id);

        /// <summary>
        /// Get quick link by ID
        /// </summary>
        Task<QuickLinkOutputDto> GetByIdAsync(long id);

        /// <summary>
        /// Get all quick links for an integration
        /// </summary>
        Task<List<QuickLinkOutputDto>> GetByIntegrationIdAsync(long integrationId);

        /// <summary>
        /// Get all quick links
        /// </summary>
        Task<List<QuickLinkOutputDto>> GetAllAsync();

        /// <summary>
        /// Generate URL with parameters
        /// </summary>
        Task<string> GenerateUrlAsync(long quickLinkId, Dictionary<string, string> dataContext);
    }
}
