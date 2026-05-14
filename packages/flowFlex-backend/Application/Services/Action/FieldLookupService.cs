using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Application.Contracts.IServices.Integration;
using FlowFlex.Domain.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace FlowFlex.Application.Services.Action
{
    /// <summary>
    /// Service for fetching and previewing field lookup options
    /// </summary>
    public class FieldLookupService : IFieldLookupService, IScopedService
    {
        private readonly IIntegrationHttpClient _integrationHttpClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<FieldLookupService> _logger;

        public FieldLookupService(
            IIntegrationHttpClient integrationHttpClient,
            IHttpClientFactory httpClientFactory,
            ILogger<FieldLookupService> logger)
        {
            _integrationHttpClient = integrationHttpClient;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Fetch lookup options for all fields with lookup configuration in the mapping config
        /// </summary>
        public async Task<List<FieldLookupResult>> FetchLookupOptionsAsync(
            long defaultIntegrationId,
            JToken mappingConfig,
            object? contextData = null,
            CancellationToken cancellationToken = default)
        {
            // 1. Parse MappingConfig
            MappingConfigModel? config;
            try
            {
                config = mappingConfig.ToObject<MappingConfigModel>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse MappingConfig JSON");
                return new List<FieldLookupResult>();
            }

            if (config?.FieldMappings == null || !config.FieldMappings.Any())
            {
                return new List<FieldLookupResult>();
            }

            // 2. Filter fields with lookup configuration
            var lookupFields = config.FieldMappings.Where(f => f.Lookup != null).ToList();
            if (!lookupFields.Any())
            {
                return new List<FieldLookupResult>();
            }

            // 3. Get global configuration
            var timeout = config.LookupConfig?.TimeoutSeconds ?? 10;
            var maxOptions = config.LookupConfig?.MaxOptionsPerField ?? 200;

            _logger.LogDebug("FetchLookupOptionsAsync: Processing {Count} lookup fields with timeout={Timeout}s, maxOptions={MaxOptions}",
                lookupFields.Count, timeout, maxOptions);

            // 4. Fetch options in parallel
            var tasks = lookupFields.Select(field =>
                FetchSingleFieldOptionsAsync(field, defaultIntegrationId, timeout, maxOptions, contextData, cancellationToken));

            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }

        /// <summary>
        /// Fetch lookup options for a list of field mapping items (from ActionConfig.lookupMappings)
        /// </summary>
        public async Task<List<FieldLookupResult>> FetchLookupOptionsAsync(
            long integrationId,
            List<FieldMappingItem> lookupMappings,
            object? contextData = null,
            CancellationToken cancellationToken = default)
        {
            return await FetchLookupOptionsAsync(lookupMappings, contextData, cancellationToken);
        }

        /// <summary>
        /// Fetch lookup options using contextData for authentication (headers with {{placeholder}} replacement)
        /// </summary>
        public async Task<List<FieldLookupResult>> FetchLookupOptionsAsync(
            List<FieldMappingItem> lookupMappings,
            object? contextData = null,
            CancellationToken cancellationToken = default)
        {
            if (lookupMappings == null || !lookupMappings.Any())
            {
                return new List<FieldLookupResult>();
            }

            // Filter fields with lookup configuration
            var lookupFields = lookupMappings.Where(f => f.Lookup != null).ToList();
            if (!lookupFields.Any())
            {
                return new List<FieldLookupResult>();
            }

            const int timeout = 10;
            const int maxOptions = 200;

            _logger.LogDebug("FetchLookupOptionsAsync: Processing {Count} lookup fields", lookupFields.Count);

            // Fetch options in parallel
            var tasks = lookupFields.Select(field =>
                FetchSingleFieldOptionsAsync(field, 0, timeout, maxOptions, contextData, cancellationToken));

            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }

        /// <summary>
        /// Preview lookup options for a single field configuration (used by frontend Test button)
        /// </summary>
        public async Task<FieldLookupResult> PreviewLookupAsync(
            long integrationId,
            LookupPreviewRequest request,
            CancellationToken cancellationToken = default)
        {
            // Build a temporary FieldMappingItem from the preview request
            var field = new FieldMappingItem
            {
                ApiField = "preview",
                Lookup = new LookupConfig
                {
                    Endpoint = request.Endpoint,
                    DisplayPath = request.DisplayPath,
                    ValuePath = request.ValuePath,
                    ResponsePath = request.ResponsePath,
                    Headers = request.Headers,
                    IntegrationId = integrationId
                }
            };

            // Use a reasonable preview limit
            const int previewMaxOptions = 10;
            const int previewTimeout = 10;

            return await FetchSingleFieldOptionsAsync(field, integrationId, previewTimeout, previewMaxOptions, null, cancellationToken);
        }

        /// <summary>
        /// Fetch options for a single field's lookup configuration
        /// </summary>
        private async Task<FieldLookupResult> FetchSingleFieldOptionsAsync(
            FieldMappingItem field,
            long defaultIntegrationId,
            int timeout,
            int maxOptions,
            object? contextData,
            CancellationToken cancellationToken)
        {
            try
            {
                var lookup = field.Lookup!;
                var integrationId = lookup.IntegrationId ?? defaultIntegrationId;

                // Replace placeholders in endpoint (e.g. {{fieldName}}) with context values
                var processedEndpoint = ReplacePlaceholders(lookup.Endpoint, contextData);

                // Replace placeholders in headers values (e.g. {{integrationToken}})
                var processedHeaders = ProcessHeaders(lookup.Headers, contextData);

                // Determine how to make the HTTP call
                IntegrationHttpResponse response;
                if (integrationId > 0)
                {
                    // Use IntegrationHttpClient (with Integration authentication)
                    response = await _integrationHttpClient.GetAsync(
                        integrationId,
                        processedEndpoint,
                        processedHeaders,
                        timeout,
                        cancellationToken);
                }
                else
                {
                    // Direct HTTP call (authentication via processed headers, e.g. {{integrationToken}})
                    response = await SendDirectHttpGetAsync(processedEndpoint, processedHeaders, timeout, cancellationToken);
                }

                if (!response.IsSuccess)
                {
                    _logger.LogWarning("Lookup failed for field {ApiField}: {Error}", field.ApiField, response.Error);
                    return FieldLookupResult.Failed(field.ApiField, response.Error);
                }

                if (string.IsNullOrEmpty(response.Body))
                {
                    return FieldLookupResult.Failed(field.ApiField, "Empty response body");
                }

                // Parse response JSON
                JToken json;
                try
                {
                    json = JToken.Parse(response.Body);
                }
                catch (Exception ex)
                {
                    return FieldLookupResult.Failed(field.ApiField, $"Failed to parse response JSON: {ex.Message}");
                }

                // Extract array using ResponsePath
                JToken? array;
                if (!string.IsNullOrEmpty(lookup.ResponsePath))
                {
                    array = json.SelectToken(lookup.ResponsePath);
                    if (array == null)
                    {
                        return FieldLookupResult.Failed(field.ApiField,
                            $"Response path '{lookup.ResponsePath}' did not resolve to any value");
                    }
                }
                else
                {
                    array = json;
                }

                if (array.Type != JTokenType.Array)
                {
                    return FieldLookupResult.Failed(field.ApiField,
                        "Response path did not resolve to an array");
                }

                // Extract options from array
                var totalCount = array.Count();
                var options = array.Children()
                    .Take(maxOptions)
                    .Select(item => new OptionItem
                    {
                        Display = item.SelectToken(lookup.DisplayPath)?.ToString() ?? string.Empty,
                        Value = item.SelectToken(lookup.ValuePath)?.ToString() ?? string.Empty
                    })
                    .ToList();

                _logger.LogDebug("Lookup succeeded for field {ApiField}: {OptionsCount} options (total: {TotalCount})",
                    field.ApiField, options.Count, totalCount);

                return FieldLookupResult.Success(field.ApiField, options, totalCount);
            }
            catch (OperationCanceledException)
            {
                return FieldLookupResult.Failed(field.ApiField, "Request was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unexpected error during lookup for field {ApiField}", field.ApiField);
                return FieldLookupResult.Failed(field.ApiField, ex.Message);
            }
        }

        /// <summary>
        /// Send a direct HTTP GET request without Integration authentication
        /// Used when authentication is handled via placeholder replacement in headers
        /// </summary>
        private async Task<IntegrationHttpResponse> SendDirectHttpGetAsync(
            string url,
            Dictionary<string, string>? headers,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                using var httpClient = _httpClientFactory.CreateClient();
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                // Apply headers (which already have placeholders replaced)
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        if (!string.IsNullOrEmpty(header.Key) && !string.IsNullOrEmpty(header.Value))
                        {
                            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }
                }

                var response = await httpClient.SendAsync(request, timeoutCts.Token);
                var body = await response.Content.ReadAsStringAsync();

                return new IntegrationHttpResponse
                {
                    IsSuccess = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    Body = body,
                    Error = response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
                    DurationMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (OperationCanceledException)
            {
                return new IntegrationHttpResponse
                {
                    IsSuccess = false,
                    StatusCode = 0,
                    Error = $"Request timeout ({timeoutSeconds}s)",
                    DurationMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                return new IntegrationHttpResponse
                {
                    IsSuccess = false,
                    StatusCode = 0,
                    Error = $"HTTP request failed: {ex.Message}",
                    DurationMs = stopwatch.ElapsedMilliseconds
                };
            }
        }

        /// <summary>
        /// Process headers dictionary - replace {{placeholder}} patterns in header values
        /// </summary>
        private Dictionary<string, string>? ProcessHeaders(Dictionary<string, string>? headers, object? contextData)
        {
            if (headers == null || headers.Count == 0 || contextData == null)
                return headers;

            var processed = new Dictionary<string, string>();
            foreach (var kvp in headers)
            {
                processed[kvp.Key] = ReplacePlaceholders(kvp.Value, contextData);
            }
            return processed;
        }

        /// <summary>
        /// Replace {{placeholder}} patterns in a string with values from contextData
        /// </summary>
        private string ReplacePlaceholders(string input, object? contextData)
        {
            if (string.IsNullOrEmpty(input) || contextData == null)
                return input;

            try
            {
                var placeholderPattern = @"\{\{(\w+(?:\.\w+)*)\}\}";
                var result = Regex.Replace(input, placeholderPattern, match =>
                {
                    var placeholderName = match.Groups[1].Value.Trim();
                    var value = ExtractValueFromContext(contextData, placeholderName);
                    return value?.ToString() ?? string.Empty;
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to replace placeholders in: {Input}", input);
                return input;
            }
        }

        /// <summary>
        /// Extract a value from context data by property name (supports dot notation)
        /// </summary>
        private object? ExtractValueFromContext(object contextData, string propertyName)
        {
            try
            {
                // Parse context to JToken if needed
                JToken? token;
                if (contextData is JToken jToken)
                {
                    token = jToken;
                }
                else if (contextData is string jsonString)
                {
                    var trimmed = jsonString.Trim();
                    if ((trimmed.StartsWith("{") && trimmed.EndsWith("}")) ||
                        (trimmed.StartsWith("[") && trimmed.EndsWith("]")))
                    {
                        token = JToken.Parse(jsonString);
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    token = JToken.FromObject(contextData);
                }

                // Support dot notation (e.g. "onboarding.name")
                var result = token.SelectToken(propertyName);
                if (result != null && result.Type != JTokenType.Null)
                {
                    return result.ToObject<object>();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
