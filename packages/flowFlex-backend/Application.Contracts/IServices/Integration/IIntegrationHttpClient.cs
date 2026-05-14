using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.Integration
{
    /// <summary>
    /// HTTP client for making authenticated requests through integrations
    /// </summary>
    public interface IIntegrationHttpClient : IScopedService
    {
        /// <summary>
        /// Send a GET request using the specified integration's authentication
        /// </summary>
        /// <param name="integrationId">Integration ID for authentication lookup</param>
        /// <param name="relativePath">Relative path or full URL for the request</param>
        /// <param name="additionalHeaders">Optional additional headers (overrides auth headers on conflict)</param>
        /// <param name="timeoutSeconds">Request timeout in seconds (default: 10)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Unified HTTP response</returns>
        Task<IntegrationHttpResponse> GetAsync(
            long integrationId,
            string relativePath,
            Dictionary<string, string>? additionalHeaders = null,
            int timeoutSeconds = 10,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Send a POST request using the specified integration's authentication
        /// </summary>
        /// <param name="integrationId">Integration ID for authentication lookup</param>
        /// <param name="relativePath">Relative path or full URL for the request</param>
        /// <param name="body">Optional request body (will be serialized to JSON)</param>
        /// <param name="additionalHeaders">Optional additional headers (overrides auth headers on conflict)</param>
        /// <param name="timeoutSeconds">Request timeout in seconds (default: 10)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Unified HTTP response</returns>
        Task<IntegrationHttpResponse> PostAsync(
            long integrationId,
            string relativePath,
            object? body = null,
            Dictionary<string, string>? additionalHeaders = null,
            int timeoutSeconds = 10,
            CancellationToken cancellationToken = default);
    }
}
