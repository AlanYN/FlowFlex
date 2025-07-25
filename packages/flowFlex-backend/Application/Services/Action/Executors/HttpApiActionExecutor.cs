using System.Text.Json;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts.Dtos.Action;
using FlowFlex.Application.Contracts.IServices.Action;
using FlowFlex.Domain.Shared.Enums.Action;

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

                var result = await ExecuteHttpRequestAsync(configData);
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

        private async Task<object> ExecuteHttpRequestAsync(HttpApiConfigDto config)
        {
            using var client = _httpClientFactory.CreateClient("HttpApiExecutor");

            // Set timeout based on configuration
            if (config.Timeout > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(config.Timeout);
            }

            var request = new HttpRequestMessage(GetHttpMethod(config.Method), config.Url);

            // Add headers
            foreach (var header in config.Headers)
            {
                if (!request.Headers.Contains(header.Key))
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            // Add body
            if (!string.IsNullOrEmpty(config.Body))
            {
                request.Content = new StringContent(config.Body, System.Text.Encoding.UTF8, "application/json");
            }

            try
            {
                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                _logger.LogInformation(
                    "HTTP API request completed: {Method} {Url} - Status: {StatusCode}",
                    config.Method, config.Url, (int)response.StatusCode);

                return CreateSuccessResult(response, content);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex,
                    "HTTP API request failed: {Method} {Url}",
                    config.Method, config.Url);

                return CreateErrorResult($"HTTP request failed: {ex.Message}");
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