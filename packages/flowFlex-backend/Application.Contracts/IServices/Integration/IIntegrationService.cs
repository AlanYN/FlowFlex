using FlowFlex.Application.Contracts.Dtos.Integration;

namespace FlowFlex.Application.Contracts.IServices.Integration
{
    /// <summary>
    /// Integration service interface
    /// </summary>
    public interface IIntegrationService
    {
        /// <summary>
        /// Create a new integration
        /// </summary>
        Task<long> CreateAsync(IntegrationInputDto input);

        /// <summary>
        /// Update an existing integration
        /// </summary>
        Task<bool> UpdateAsync(long id, IntegrationInputDto input);

        /// <summary>
        /// Delete an integration
        /// </summary>
        Task<bool> DeleteAsync(long id);

        /// <summary>
        /// Get integration by ID
        /// </summary>
        Task<IntegrationOutputDto> GetByIdAsync(long id);

        /// <summary>
        /// Get integration with all details (mappings, configurations, etc.)
        /// </summary>
        Task<IntegrationOutputDto> GetWithDetailsAsync(long id);

        /// <summary>
        /// Get paginated list of integrations
        /// </summary>
        Task<(List<IntegrationOutputDto> items, int total)> GetPagedListAsync(
            int pageIndex,
            int pageSize,
            string? name = null,
            string? type = null,
            string? status = null,
            string sortField = "CreateDate",
            string sortDirection = "desc");

        /// <summary>
        /// Test integration connection
        /// </summary>
        Task<bool> TestConnectionAsync(long id);

        /// <summary>
        /// Update integration status
        /// </summary>
        Task<bool> UpdateStatusAsync(long id, global::Domain.Shared.Enums.IntegrationStatus status);

        /// <summary>
        /// Get integrations by type
        /// </summary>
        Task<List<IntegrationOutputDto>> GetByTypeAsync(string type);

        /// <summary>
        /// Get all active (connected) integrations
        /// </summary>
        Task<List<IntegrationOutputDto>> GetActiveIntegrationsAsync();

        /// <summary>
        /// Get inbound configuration overview for an integration
        /// </summary>
        Task<List<InboundConfigurationOverviewDto>> GetInboundOverviewAsync(long integrationId);

        /// <summary>
        /// Get outbound configuration overview for an integration
        /// </summary>
        Task<List<OutboundConfigurationOverviewDto>> GetOutboundOverviewAsync(long integrationId);

        /// <summary>
        /// Get inbound field mappings by action ID (read-only view)
        /// </summary>
        Task<List<InboundFieldMappingDto>> GetInboundFieldMappingsByActionAsync(
            long integrationId,
            long actionId,
            string? externalFieldName = null,
            string? wfeFieldName = null);

        /// <summary>
        /// Get outbound shared fields by action ID (read-only view)
        /// </summary>
        Task<List<OutboundSharedFieldDto>> GetOutboundSharedFieldsByActionAsync(
            long integrationId,
            long actionId,
            string? fieldName = null);
    }
}
