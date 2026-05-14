using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.Integration;
using FlowFlex.Domain.Repository.Integration;
using FlowFlex.Domain.Shared;
using Domain.Shared.Enums;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FlowFlex.Application.Services.Integration
{
    /// <summary>
    /// HTTP client for making authenticated requests through integrations
    /// </summary>
    public class IntegrationHttpClient : IIntegrationHttpClient, IScopedService
    {
        private readonly IIntegrationRepository _integrationRepository;
        private readonly IEncryptionService _encryptionService;
        private readonly IIntegrationApiLogService _logService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IntegrationHttpClient> _logger;

        public IntegrationHttpClient(
            IIntegrationRepository integrationRepository,
            IEncryptionService encryptionService,
            IIntegrationApiLogService logService,
            IHttpClientFactory httpClientFactory,
            ILogger<IntegrationHttpClient> logger)
        {
            _integrationRepository = integrationRepository;
            _encryptionService = encryptionService;
            _logService = logService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Send a GET request using the specified integration's authentication
        /// </summary>
        public async Task<IntegrationHttpResponse> GetAsync(
            long integrationId,
            string relativePath,
            Dictionary<string, string>? additionalHeaders = null,
            int timeoutSeconds = 10,
            CancellationToken cancellationToken = default)
        {
            return await SendAsync(integrationId, HttpMethod.Get, relativePath, null, additionalHeaders, timeoutSeconds, cancellationToken);
        }

        /// <summary>
        /// Send a POST request using the specified integration's authentication
        /// </summary>
        public async Task<IntegrationHttpResponse> PostAsync(
            long integrationId,
            string relativePath,
            object? body = null,
            Dictionary<string, string>? additionalHeaders = null,
            int timeoutSeconds = 10,
            CancellationToken cancellationToken = default)
        {
            return await SendAsync(integrationId, HttpMethod.Post, relativePath, body, additionalHeaders, timeoutSeconds, cancellationToken);
        }

        /// <summary>
        /// Core method to send HTTP request with integration authentication
        /// </summary>
        private async Task<IntegrationHttpResponse> SendAsync(
            long integrationId,
            HttpMethod method,
            string relativePath,
            object? body,
            Dictionary<string, string>? additionalHeaders,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var startedAt = DateTimeOffset.UtcNow;

            try
            {
                // 1. Get integration entity
                var integration = await _integrationRepository.GetByIdAsync(integrationId);
                if (integration == null)
                {
                    return new IntegrationHttpResponse
                    {
                        IsSuccess = false,
                        StatusCode = 0,
                        Error = $"Integration not found: {integrationId}",
                        DurationMs = stopwatch.ElapsedMilliseconds
                    };
                }

                // 2. Decrypt credentials
                var credentials = DecryptCredentials(integration.EncryptedCredentials);

                // 3. Build request URL
                var requestUrl = BuildRequestUrl(integration.EndpointUrl, relativePath, integration.AuthMethod);

                // 4. Create HTTP request
                using var httpClient = _httpClientFactory.CreateClient();

                // 5. Set timeout
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                var request = new HttpRequestMessage(method, requestUrl);

                // 6. Apply authentication headers
                ApplyAuthentication(request, integration.AuthMethod, credentials);

                // 7. Add common headers
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.TryAddWithoutValidation("User-Agent", "FlowFlex-Integration/1.0");

                // 8. Merge additional headers (overrides auth headers on conflict)
                if (additionalHeaders != null)
                {
                    foreach (var header in additionalHeaders)
                    {
                        request.Headers.Remove(header.Key);
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                // 9. Set body for POST requests
                if (body != null && method == HttpMethod.Post)
                {
                    var jsonBody = JsonConvert.SerializeObject(body);
                    request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                }

                _logger.LogDebug("IntegrationHttpClient: Sending {Method} request to {Url} for integration {IntegrationId}",
                    method, requestUrl, integrationId);

                // 10. Send request
                var response = await httpClient.SendAsync(request, timeoutCts.Token);
                var responseBody = await response.Content.ReadAsStringAsync(timeoutCts.Token);

                stopwatch.Stop();
                var completedAt = DateTimeOffset.UtcNow;

                // 11. Log API call
                await LogApiCallSafeAsync(integrationId, integration.SystemName, relativePath, method.Method,
                    startedAt, completedAt, stopwatch.ElapsedMilliseconds,
                    (int)response.StatusCode, response.IsSuccessStatusCode,
                    response.IsSuccessStatusCode ? null : responseBody);

                return new IntegrationHttpResponse
                {
                    IsSuccess = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    Body = responseBody,
                    Error = response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
                    DurationMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                await LogApiCallSafeAsync(integrationId, string.Empty, relativePath, method.Method,
                    startedAt, DateTimeOffset.UtcNow, stopwatch.ElapsedMilliseconds,
                    0, false, $"Request timeout ({timeoutSeconds}s)");

                return new IntegrationHttpResponse
                {
                    IsSuccess = false,
                    StatusCode = 0,
                    Error = $"Request timeout ({timeoutSeconds}s)",
                    DurationMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                return new IntegrationHttpResponse
                {
                    IsSuccess = false,
                    StatusCode = 0,
                    Error = "Request was cancelled",
                    DurationMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();

                await LogApiCallSafeAsync(integrationId, string.Empty, relativePath, method.Method,
                    startedAt, DateTimeOffset.UtcNow, stopwatch.ElapsedMilliseconds,
                    0, false, $"HTTP request failed: {ex.Message}");

                return new IntegrationHttpResponse
                {
                    IsSuccess = false,
                    StatusCode = 0,
                    Error = $"HTTP request failed: {ex.Message}",
                    DurationMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "IntegrationHttpClient: Unexpected error for integration {IntegrationId}, path: {Path}",
                    integrationId, relativePath);

                await LogApiCallSafeAsync(integrationId, string.Empty, relativePath, method.Method,
                    startedAt, DateTimeOffset.UtcNow, stopwatch.ElapsedMilliseconds,
                    0, false, $"Unexpected error: {ex.Message}");

                return new IntegrationHttpResponse
                {
                    IsSuccess = false,
                    StatusCode = 0,
                    Error = $"Unexpected error: {ex.Message}",
                    DurationMs = stopwatch.ElapsedMilliseconds
                };
            }
        }

        /// <summary>
        /// Build the full request URL based on relativePath and auth method
        /// </summary>
        private string BuildRequestUrl(string endpointUrl, string relativePath, AuthenticationMethod authMethod)
        {
            // If relativePath is already a full URL, use it directly
            if (relativePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                relativePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return relativePath;
            }

            // For OAuth2, EndpointUrl is the token endpoint, so relativePath should be a full URL
            // But if it's not, concatenate with EndpointUrl as fallback
            return endpointUrl.TrimEnd('/') + "/" + relativePath.TrimStart('/');
        }

        /// <summary>
        /// Apply authentication headers based on the integration's auth method
        /// </summary>
        private void ApplyAuthentication(HttpRequestMessage request, AuthenticationMethod authMethod, Dictionary<string, string> credentials)
        {
            switch (authMethod)
            {
                case AuthenticationMethod.ApiKey:
                    if (credentials.TryGetValue("apiKey", out var apiKey))
                    {
                        request.Headers.Add("X-API-Key", apiKey);
                        request.Headers.Authorization = new AuthenticationHeaderValue("ApiKey", apiKey);
                    }
                    break;

                case AuthenticationMethod.BasicAuth:
                    if (credentials.TryGetValue("username", out var username) &&
                        credentials.TryGetValue("password", out var password))
                    {
                        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);
                    }
                    break;

                case AuthenticationMethod.OAuth2:
                    // For OAuth2, we need to get an access token first
                    // The token should be obtained before calling this method
                    // For now, if a token is available in credentials, use it
                    if (credentials.TryGetValue("accessToken", out var accessToken))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    }
                    break;

                case AuthenticationMethod.BearerToken:
                    if (credentials.TryGetValue("token", out var token))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    }
                    break;
            }
        }

        /// <summary>
        /// Decrypt encrypted credentials JSON string to dictionary
        /// </summary>
        private Dictionary<string, string> DecryptCredentials(string? encryptedCredentials)
        {
            if (string.IsNullOrEmpty(encryptedCredentials) || encryptedCredentials == "{}")
            {
                return new Dictionary<string, string>();
            }

            try
            {
                var json = _encryptionService.Decrypt(encryptedCredentials);
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt credentials");
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Log API call without throwing exceptions
        /// </summary>
        private async Task LogApiCallSafeAsync(
            long integrationId,
            string systemId,
            string endpoint,
            string httpMethod,
            DateTimeOffset startedAt,
            DateTimeOffset completedAt,
            long durationMs,
            int statusCode,
            bool isSuccess,
            string? errorMessage)
        {
            try
            {
                await _logService.LogApiCallAsync(
                    integrationId,
                    systemId,
                    endpoint,
                    httpMethod,
                    startedAt,
                    completedAt,
                    durationMs,
                    statusCode,
                    isSuccess,
                    errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log API call for integration {IntegrationId}", integrationId);
            }
        }
    }
}
