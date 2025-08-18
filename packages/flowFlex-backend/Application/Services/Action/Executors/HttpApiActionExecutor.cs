using System.Text.Json;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Domain.Shared.Enums.Action;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace FlowFlex.Application.Services.Action.Executors
{
    /// <summary>
    /// HTTP API action executor - makes HTTP API calls
    /// </summary>
    public class HttpApiActionExecutor : IActionExecutor
    {
        private readonly ILogger<HttpApiActionExecutor> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions;

        public HttpApiActionExecutor(
            ILogger<HttpApiActionExecutor> logger,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public ActionTypeEnum ActionType => ActionTypeEnum.HttpApi;

        public async Task<object> ExecuteAsync(string config, object triggerContext)
        {
            _logger.LogInformation("Executing HTTP API action with config: {Config}", config);

            try
            {
                var configData = ParseConfig(config);
                if (configData == null)
                {
                    return CreateErrorResult("Invalid configuration format");
                }

                if (string.IsNullOrWhiteSpace(configData.Url))
                {
                    return CreateErrorResult("URL is required");
                }

                var result = await ExecuteHttpRequestAsync(configData, triggerContext);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing HTTP API action");
                return CreateErrorResult($"Execution failed: {ex.Message}");
            }
        }

        private HttpApiConfigDto? ParseConfig(string config)
        {
            try
            {
                return JsonSerializer.Deserialize<HttpApiConfigDto>(config, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse HTTP API configuration");
                return null;
            }
        }

        private async Task<object> ExecuteHttpRequestAsync(HttpApiConfigDto config, object triggerContext)
        {
            using var client = _httpClientFactory.CreateClient("HttpApiExecutor");

            // Set timeout based on configuration
            if (config.Timeout > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(config.Timeout);
            }

            // Replace placeholders in URL
            var processedUrl = ReplacePlaceholders(config.Url, triggerContext);
            var request = new HttpRequestMessage(GetHttpMethod(config.Method), processedUrl);

            // Add headers with placeholder replacement
            foreach (var header in config.Headers)
            {
                if (!request.Headers.Contains(header.Key))
                {
                    var processedValue = ReplacePlaceholders(header.Value, triggerContext);
                    request.Headers.Add(header.Key, processedValue);
                }
            }

            // Add body with placeholder replacement
            if (!string.IsNullOrEmpty(config.Body))
            {
                var processedBody = ReplacePlaceholders(config.Body, triggerContext);
                request.Content = new StringContent(processedBody, System.Text.Encoding.UTF8, "application/json");
            }

            try
            {
                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation(
                    "HTTP API request completed: {Method} {Url} - Status: {StatusCode}",
                    config.Method, processedUrl, (int)response.StatusCode);

                return CreateSuccessResult(response, content);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex,
                    "HTTP API request failed: {Method} {Url}",
                    config.Method, processedUrl);

                return CreateErrorResult($"HTTP request failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Replace placeholders like {{ stageId }} with values from triggerContext
        /// </summary>
        /// <param name="input">Input string containing placeholders</param>
        /// <param name="triggerContext">Context object containing values</param>
        /// <returns>String with placeholders replaced</returns>
        private string ReplacePlaceholders(string input, object triggerContext)
        {
            if (string.IsNullOrEmpty(input) || triggerContext == null)
                return input;

            try
            {
                // Use regex to find all placeholders like {{stageId}}
                var placeholderPattern = @"\{\{(\w+)\}\}";
                var result = Regex.Replace(input, placeholderPattern, match =>
                {
                    var placeholderName = match.Groups[1].Value.Trim();
                    var value = ExtractValueFromContext(triggerContext, placeholderName);
                    return value?.ToString() ?? match.Value; // Return original placeholder if value not found
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to replace placeholders in string: {Input}", input);
                return input;
            }
        }

        /// <summary>
        /// Extract value from triggerContext by property name
        /// </summary>
        /// <param name="triggerContext">Context object</param>
        /// <param name="propertyName">Property name to extract</param>
        /// <returns>Property value or null if not found</returns>
        private object? ExtractValueFromContext(object triggerContext, string propertyName)
        {
            if (triggerContext == null)
                return null;

            try
            {
                // Handle JToken/JObject
                if (triggerContext is JToken jToken)
                {
                    var token = jToken[propertyName];
                    if (token != null && token.Type != JTokenType.Null)
                    {
                        return token.ToObject<object>();
                    }
                    return null;
                }

                // Handle JObject
                if (triggerContext is JObject jObject)
                {
                    var token = jObject[propertyName];
                    if (token != null && token.Type != JTokenType.Null)
                    {
                        return token.ToObject<object>();
                    }
                    return null;
                }

                // Handle IDictionary
                if (triggerContext is System.Collections.IDictionary dict)
                {
                    if (dict.Contains(propertyName))
                    {
                        return dict[propertyName];
                    }
                    return null;
                }

                // Handle generic Dictionary
                if (triggerContext is IDictionary<string, object> genericDict)
                {
                    if (genericDict.TryGetValue(propertyName, out var value))
                    {
                        return value;
                    }
                    return null;
                }

                // Handle object properties via reflection
                var property = triggerContext.GetType().GetProperty(propertyName);
                if (property != null)
                {
                    return property.GetValue(triggerContext);
                }

                // Try case-insensitive property search
                property = triggerContext.GetType().GetProperties()
                    .FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
                if (property != null)
                {
                    return property.GetValue(triggerContext);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to extract property '{PropertyName}' from triggerContext", propertyName);
                return null;
            }
        }

        private static HttpMethod GetHttpMethod(string method)
        {
            return method.ToUpper() switch
            {
                "GET" => HttpMethod.Get,
                "POST" => HttpMethod.Post,
                "PUT" => HttpMethod.Put,
                "DELETE" => HttpMethod.Delete,
                "PATCH" => HttpMethod.Patch,
                _ => HttpMethod.Get
            };
        }

        private object CreateSuccessResult(HttpResponseMessage response, string content)
        {
            return new
            {
                success = response.IsSuccessStatusCode,
                statusCode = (int)response.StatusCode,
                response = content,
                headers = response.Headers.ToDictionary(h => h.Key, h => h.Value),
                timestamp = DateTimeOffset.UtcNow
            };
        }

        private object CreateErrorResult(string message)
        {
            return new
            {
                success = false,
                error = message,
                timestamp = DateTimeOffset.UtcNow
            };
        }
    }
}