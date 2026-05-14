using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Domain.Shared;
using Newtonsoft.Json.Linq;

namespace FlowFlex.Application.Contracts.IServices.Action
{
    /// <summary>
    /// Service for fetching and previewing field lookup options
    /// </summary>
    public interface IFieldLookupService : IScopedService
    {
        /// <summary>
        /// Fetch lookup options for all fields with lookup configuration in the mapping config
        /// </summary>
        /// <param name="defaultIntegrationId">Default integration ID used when field-level override is not specified</param>
        /// <param name="mappingConfig">Mapping configuration JSON containing field mappings with lookup settings</param>
        /// <param name="contextData">Optional context data for placeholder replacement in endpoints</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of lookup results for each field with lookup configuration</returns>
        Task<List<FieldLookupResult>> FetchLookupOptionsAsync(
            long defaultIntegrationId,
            JToken mappingConfig,
            object? contextData = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Fetch lookup options for a list of field mapping items (from ActionConfig.lookupMappings)
        /// </summary>
        /// <param name="integrationId">Integration ID for authentication</param>
        /// <param name="lookupMappings">List of field mapping items with lookup configuration</param>
        /// <param name="contextData">Optional context data for placeholder replacement in endpoints</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of lookup results for each field with lookup configuration</returns>
        Task<List<FieldLookupResult>> FetchLookupOptionsAsync(
            long integrationId,
            List<FieldMappingItem> lookupMappings,
            object? contextData = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Fetch lookup options using contextData for authentication (headers with {{placeholder}} replacement)
        /// No IntegrationId required - authentication is handled via placeholder replacement in headers
        /// </summary>
        /// <param name="lookupMappings">List of field mapping items with lookup configuration</param>
        /// <param name="contextData">Context data for placeholder replacement in endpoints and headers</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of lookup results for each field with lookup configuration</returns>
        Task<List<FieldLookupResult>> FetchLookupOptionsAsync(
            List<FieldMappingItem> lookupMappings,
            object? contextData = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Preview lookup options for a single field configuration (used by frontend Test button)
        /// </summary>
        /// <param name="integrationId">Integration ID for authentication</param>
        /// <param name="request">Lookup preview request with endpoint and path configurations</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Lookup result with preview options (limited to 10 items)</returns>
        Task<FieldLookupResult> PreviewLookupAsync(
            long integrationId,
            LookupPreviewRequest request,
            CancellationToken cancellationToken = default);
    }
}
