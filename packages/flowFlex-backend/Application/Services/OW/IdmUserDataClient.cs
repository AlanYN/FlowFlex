using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FlowFlex.Application.Contracts.Dtos.OW.User;
using FlowFlex.Application.Contracts.Options;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// IDM User Data Client Service for IDM API integration
    /// </summary>
    public class IdmUserDataClient : IScopedService
    {
        private readonly HttpClient _client;
        private readonly IdentityHubOptions _options;
        private readonly IMemoryCache _cache;
        private readonly ILogger<IdmUserDataClient> _logger;

        public IdmUserDataClient(
            HttpClient client,
            IOptionsSnapshot<IdentityHubOptions> options,
            IMemoryCache cache,
            ILogger<IdmUserDataClient> logger)
        {
            _client = client;
            _options = options.Value;
            _cache = cache;
            _logger = logger;

            // Log configuration validation
            _logger.LogInformation("IdmUserDataClient initialized with configuration:");
            _logger.LogInformation("BaseUrl: {BaseUrl}", _options.BaseUrl);
            _logger.LogInformation("ClientId: {ClientId}", _options.ClientId);
            _logger.LogInformation("TokenEndpoint: {TokenEndpoint}", _options.TokenEndpoint);
            _logger.LogInformation("QueryUser: {QueryUser}", _options.QueryUser);
            _logger.LogInformation("HttpClient BaseAddress: {BaseAddress}", _client.BaseAddress);

            // Validate critical configuration
            if (string.IsNullOrEmpty(_options.BaseUrl))
            {
                _logger.LogError("BaseUrl is not configured in IdmApis settings");
            }
            if (string.IsNullOrEmpty(_options.ClientId))
            {
                _logger.LogError("ClientId is not configured in IdmApis settings");
            }
            if (string.IsNullOrEmpty(_options.ClientSecret))
            {
                _logger.LogError("ClientSecret is not configured in IdmApis settings");
            }
        }

        /// <summary>
        /// Get client token using Client Credentials flow
        /// </summary>
        public async Task<IdentityHubClientTokenResponse> ClientTokenAsync(string clientId, string clientSecret)
        {
            _logger.LogInformation("Requesting client token from IDM with ClientId: {ClientId}", clientId);
            _logger.LogDebug("Token endpoint: {TokenEndpoint}", _options.TokenEndpoint);

            var formContent = new List<KeyValuePair<string, string>>
            {
                new("grant_type", "client_credentials"),
                new("client_id", clientId),
                new("client_secret", clientSecret)
            };

            _logger.LogDebug("Token request form data:");
            foreach (var kvp in formContent)
            {
                if (kvp.Key == "client_secret")
                {
                    _logger.LogDebug("  {Key}: [REDACTED]", kvp.Key);
                }
                else
                {
                    _logger.LogDebug("  {Key}: {Value}", kvp.Key, kvp.Value);
                }
            }

            using var content = new FormUrlEncodedContent(formContent);

            try
            {
                var fullUrl = $"{_client.BaseAddress?.ToString().TrimEnd('/')}{_options.TokenEndpoint}";
                _logger.LogDebug("Sending POST request to: {Url}", fullUrl);

                // Clear any existing authorization headers before setting new one
                _client.DefaultRequestHeaders.Authorization = null;

                // Try HTTP Basic Authentication for client credentials (alternative method)
                var basicAuthBytes = System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}");
                var basicAuthString = Convert.ToBase64String(basicAuthBytes);
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuthString);
                _logger.LogDebug("Using HTTP Basic Authentication for client credentials");

                // For basic auth, we only need grant_type in the form data
                var basicAuthFormContent = new List<KeyValuePair<string, string>>
                {
                    new("grant_type", "client_credentials")
                };
                using var basicAuthContent = new FormUrlEncodedContent(basicAuthFormContent);

                // Log request headers
                _logger.LogDebug("Request headers:");
                foreach (var header in _client.DefaultRequestHeaders)
                {
                    if (header.Key.Contains("Authorization", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug("  {HeaderName}: Basic [REDACTED]", header.Key);
                    }
                    else
                    {
                        _logger.LogDebug("  {HeaderName}: {HeaderValue}", header.Key, string.Join(", ", header.Value));
                    }
                }
                _logger.LogDebug("Content-Type: {ContentType}", basicAuthContent.Headers.ContentType?.ToString());

                // First try with Basic Auth
                _logger.LogDebug("Attempting token request with HTTP Basic Authentication");
                using var basicResp = await _client.PostAsync(_options.TokenEndpoint, basicAuthContent);

                if (basicResp.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Token request succeeded with Basic Authentication: {StatusCode}", basicResp.StatusCode);

                    // Log the raw response content for debugging
                    var rawContent = await basicResp.Content.ReadAsStringAsync();
                    _logger.LogDebug("Raw token response content: {Content}", rawContent);

                    // Parse JSON from the string content
                    var basicResult = System.Text.Json.JsonSerializer.Deserialize<IdentityHubClientTokenResponse>(rawContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (basicResult != null)
                    {
                        _logger.LogInformation("Token obtained successfully. TokenType: {TokenType}, ExpiresIn: {ExpiresIn}s",
                            basicResult.TokenType ?? "(null)", basicResult.ExpiresIn);

                        // Provide fallback values if null
                        if (string.IsNullOrEmpty(basicResult.TokenType))
                        {
                            _logger.LogWarning("TokenType is null, using 'Bearer' as fallback");
                            basicResult.TokenType = "Bearer";
                        }

                        if (basicResult.ExpiresIn <= 0)
                        {
                            _logger.LogWarning("ExpiresIn is {ExpiresIn}, using 3600 as fallback", basicResult.ExpiresIn);
                            basicResult.ExpiresIn = 3600; // Default 1 hour
                        }
                    }
                    else
                    {
                        _logger.LogError("Failed to deserialize token response to IdentityHubClientTokenResponse");
                    }
                    return basicResult;
                }
                else
                {
                    var basicErrorContent = await basicResp.Content.ReadAsStringAsync();
                    _logger.LogWarning("Basic Auth failed: {StatusCode} - {Content}", basicResp.StatusCode, basicErrorContent);
                }

                // Clear Basic Auth and try with form data method
                _client.DefaultRequestHeaders.Authorization = null;
                _logger.LogDebug("Attempting token request with form data method");
                using var resp = await _client.PostAsync(_options.TokenEndpoint, content);

                _logger.LogInformation("Token request response: {StatusCode}", resp.StatusCode);

                if (!resp.IsSuccessStatusCode)
                {
                    var errorContent = await resp.Content.ReadAsStringAsync();
                    _logger.LogError("Token request failed: {StatusCode} - {Content}", resp.StatusCode, errorContent);
                    resp.EnsureSuccessStatusCode();
                }

                var responseContent = await resp.Content.ReadAsStringAsync();
                _logger.LogDebug("Form data raw token response content: {Content}", responseContent);

                var result = JsonSerializer.Deserialize<IdentityHubClientTokenResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result != null)
                {
                    _logger.LogInformation("Token obtained successfully. TokenType: {TokenType}, ExpiresIn: {ExpiresIn}s",
                        result.TokenType ?? "(null)", result.ExpiresIn);

                    // Provide fallback values if null
                    if (string.IsNullOrEmpty(result.TokenType))
                    {
                        _logger.LogWarning("TokenType is null, using 'Bearer' as fallback");
                        result.TokenType = "Bearer";
                    }

                    if (result.ExpiresIn <= 0)
                    {
                        _logger.LogWarning("ExpiresIn is {ExpiresIn}, using 3600 as fallback", result.ExpiresIn);
                        result.ExpiresIn = 3600; // Default 1 hour
                    }

                    if (!string.IsNullOrEmpty(result.Error))
                    {
                        _logger.LogError("Token response contains error: {Error}", result.Error);
                    }
                }
                else
                {
                    _logger.LogError("Token response is null");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while requesting token from IDM");
                throw;
            }
        }

        /// <summary>
        /// Get token with caching
        /// </summary>
        private async Task<IdentityHubClientTokenResponse> GetTokenAsync()
        {
            var cacheKey = $"idm:token:{_options.ClientId}";
            _logger.LogDebug("Checking token cache with key: {CacheKey}", cacheKey);

            if (!_cache.TryGetValue(cacheKey, out IdentityHubClientTokenResponse tokenInfo))
            {
                _logger.LogInformation("Token not found in cache, requesting new token from IDM for client: {ClientId}", _options.ClientId);

                tokenInfo = await ClientTokenAsync(_options.ClientId, _options.ClientSecret);
                if (tokenInfo == null || !string.IsNullOrWhiteSpace(tokenInfo.Error))
                {
                    _logger.LogError("Failed to get token from IDM. Error: {Error}", tokenInfo?.Error);
                    throw new Exception($"Failed to get token: {tokenInfo?.Error}");
                }

                // Cache for expires_in - 5 minutes to ensure validity
                var cacheExpiry = TimeSpan.FromSeconds(Math.Max(300, tokenInfo.ExpiresIn - 300));
                _cache.Set(cacheKey, tokenInfo, cacheExpiry);

                _logger.LogInformation("Successfully cached new token for {ExpirySeconds} seconds", cacheExpiry.TotalSeconds);
            }
            else
            {
                _logger.LogDebug("Using cached token for client: {ClientId}", _options.ClientId);
            }
            return tokenInfo;
        }

        /// <summary>
        /// Get all users from IDM (with large page size)
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="pageSize">Page size (default 10000)</param>
        /// <param name="pageIndex">Page index (default 1)</param>
        /// <returns>User list with pagination</returns>
        public async Task<PageModel<List<IdmUserOutputDto>>> GetAllUsersAsync(
            string tenantId = null,
            int pageSize = 10000,
            int pageIndex = 1)
        {
            try
            {
                _logger.LogInformation("=== Starting GetAllUsersAsync ===");
                _logger.LogInformation("Parameters - PageSize: {PageSize}, PageIndex: {PageIndex}, TenantId: {TenantId}",
                    pageSize, pageIndex, tenantId);
                _logger.LogInformation("QueryUser endpoint: {QueryUser}", _options.QueryUser);

                _client.DefaultRequestHeaders.Clear();
                _logger.LogDebug("Cleared default request headers");

                var tokenInfo = await GetTokenAsync();
                _logger.LogInformation("Retrieved token - Type: {TokenType}", tokenInfo?.TokenType);

                _client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(tokenInfo.TokenType, tokenInfo.AccessToken);

                // Log the token being used for debugging (first 20 chars only)
                var tokenPreview = tokenInfo.AccessToken?.Length > 20 ?
                    tokenInfo.AccessToken.Substring(0, 20) + "..." : tokenInfo.AccessToken;
                _logger.LogDebug("Using access token: {TokenPreview}", tokenPreview);
                _logger.LogDebug("Set Authorization header with {TokenType} token", tokenInfo.TokenType);

                // Build request URI
                var requestUri = $"{_options.QueryUser}?PageIndex={pageIndex}&PageSize={pageSize}";
                if (!string.IsNullOrEmpty(tenantId))
                {
                    requestUri += $"&TenantId={tenantId}";
                }

                var fullUrl = $"{_client.BaseAddress?.ToString().TrimEnd('/')}{requestUri}";
                _logger.LogInformation("Making GET request to full URL: {FullUrl}", fullUrl);

                var startTime = DateTimeOffset.UtcNow;
                using var resp = await _client.GetAsync(requestUri);
                var elapsed = DateTimeOffset.UtcNow - startTime;

                _logger.LogInformation("IDM API response - StatusCode: {StatusCode}, Elapsed: {ElapsedMs}ms",
                    resp.StatusCode, elapsed.TotalMilliseconds);

                // Log response headers
                _logger.LogDebug("Response headers:");
                foreach (var header in resp.Headers)
                {
                    _logger.LogDebug("  {HeaderName}: {HeaderValue}", header.Key, string.Join(", ", header.Value));
                }

                if (!resp.IsSuccessStatusCode)
                {
                    var errorContent = await resp.Content.ReadAsStringAsync();
                    _logger.LogError("IDM API request failed:");
                    _logger.LogError("  Status Code: {StatusCode}", resp.StatusCode);
                    _logger.LogError("  Error Content: {ErrorContent}", errorContent);
                    _logger.LogError("  Request URI: {RequestUri}", requestUri);
                    _logger.LogError("  Full URL: {FullUrl}", fullUrl);
                    throw new HttpRequestException($"IDM API request failed: {resp.StatusCode} - {errorContent}");
                }

                var responseContent = await resp.Content.ReadAsStringAsync();
                _logger.LogDebug("Raw response content (first 500 chars): {Content}",
                    responseContent.Length > 500 ? responseContent.Substring(0, 500) + "..." : responseContent);

                var result = await resp.Content.ReadFromJsonAsync<BasicResponse<PageModel<List<IdmUserOutputDto>>>>();

                if (result == null)
                {
                    _logger.LogError("Failed to deserialize response - result is null");
                    throw new Exception("Failed to deserialize IDM API response");
                }

                _logger.LogInformation("Deserialization successful - Status: {Status}, Code: {Code}, Message: {Message}",
                    result.Status, result.Code, result.Message);

                if (result.Data == null)
                {
                    _logger.LogWarning("IDM API returned null data in result");
                    return new PageModel<List<IdmUserOutputDto>>
                    {
                        PageIndex = pageIndex,
                        PageSize = pageSize,
                        PageCount = 0,
                        DataCount = 0,
                        Data = new List<IdmUserOutputDto>()
                    };
                }

                var userCount = result.Data.Data?.Count ?? 0;
                _logger.LogInformation("=== GetAllUsersAsync Success ===");
                _logger.LogInformation("Retrieved {UserCount} users from IDM", userCount);
                _logger.LogInformation("Pagination - Page {PageIndex} of {PageCount}, Total: {DataCount}",
                    result.Data.PageIndex, result.Data.PageCount, result.Data.DataCount);

                if (userCount > 0)
                {
                    var firstUser = result.Data.Data.First();
                    _logger.LogDebug("First user sample - Id: {Id}, Username: {Username}, Email: {Email}, UserGroups: {UserGroups}",
                        firstUser.Id, firstUser.Username, firstUser.Email,
                        firstUser.UserGroups != null ? string.Join(", ", firstUser.UserGroups) : "null");
                }

                return result.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== GetAllUsersAsync Failed ===");
                _logger.LogError("Exception details: {ExceptionType} - {ExceptionMessage}", ex.GetType().Name, ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerExceptionType} - {InnerExceptionMessage}",
                        ex.InnerException.GetType().Name, ex.InnerException.Message);
                }
                throw;
            }
        }

        /// <summary>
        /// Get users by IDs from IDM
        /// </summary>
        /// <param name="userIds">User ID list</param>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Paginated user data</returns>
        public async Task<PageModel<List<IdmUserOutputDto>>> GetUserByIdsAsync(
            List<string> userIds,
            string tenantId = null,
            int pageIndex = 1,
            int pageSize = 1000)
        {
            try
            {
                if (userIds == null || userIds.Count == 0)
                {
                    return new PageModel<List<IdmUserOutputDto>>
                    {
                        PageIndex = pageIndex,
                        PageSize = pageSize,
                        PageCount = 0,
                        DataCount = 0,
                        Data = new List<IdmUserOutputDto>()
                    };
                }

                _logger.LogInformation("Getting {UserIdCount} users by IDs from IDM", userIds.Count);

                _client.DefaultRequestHeaders.Clear();

                var tokenInfo = await GetTokenAsync();

                _client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(tokenInfo.TokenType, tokenInfo.AccessToken);

                var userIdsQuery = string.Join("&", userIds.Select(id => $"UserIds={id}"));
                var requestUri = $"{_options.QueryUser}?PageIndex={pageIndex}&PageSize={pageSize}&{userIdsQuery}";

                if (!string.IsNullOrEmpty(tenantId))
                {
                    requestUri += $"&TenantId={tenantId}";
                }

                using var resp = await _client.GetAsync(requestUri);
                resp.EnsureSuccessStatusCode();

                var result = await resp.Content.ReadFromJsonAsync<BasicResponse<PageModel<List<IdmUserOutputDto>>>>();
                return result?.Data ?? new PageModel<List<IdmUserOutputDto>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting users by IDs from IDM");
                throw;
            }
        }

        /// <summary>
        /// Get users by usernames from IDM
        /// </summary>
        public async Task<PageModel<List<IdmUserOutputDto>>> GetUsersByNamesAsync(
            List<string> userNames,
            string tenantId = null,
            int pageIndex = 1,
            int pageSize = 1000)
        {
            try
            {
                if (userNames == null || userNames.Count == 0)
                {
                    return new PageModel<List<IdmUserOutputDto>>();
                }

                _logger.LogInformation("Getting {UserNameCount} users by names from IDM", userNames.Count);

                _client.DefaultRequestHeaders.Clear();

                var tokenInfo = await GetTokenAsync();

                _client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(tokenInfo.TokenType, tokenInfo.AccessToken);

                var userNamesQuery = string.Join("&", userNames.Select(name => $"UserNames={name}"));
                var requestUri = $"{_options.QueryUser}?PageIndex={pageIndex}&PageSize={pageSize}&{userNamesQuery}";

                if (!string.IsNullOrEmpty(tenantId))
                {
                    requestUri += $"&TenantId={tenantId}";
                }

                using var resp = await _client.GetAsync(requestUri);
                resp.EnsureSuccessStatusCode();

                var result = await resp.Content.ReadFromJsonAsync<BasicResponse<PageModel<List<IdmUserOutputDto>>>>();
                return result?.Data ?? new PageModel<List<IdmUserOutputDto>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting users by names from IDM");
                throw;
            }
        }
        /// <summary>
        /// Get teams from IDM
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="pageSize">Page size (default 10000)</param>
        /// <param name="pageIndex">Page index (default 1)</param>
        /// <returns>Team list with pagination</returns>
        public async Task<PageModel<List<IdmTeamDto>>> GetAllTeamsAsync(
            string tenantId = null,
            int pageSize = 10000,
            int pageIndex = 1)
        {
            try
            {
                _logger.LogInformation("=== Starting GetAllTeamsAsync ===");
                _logger.LogInformation("Parameters - PageSize: {PageSize}, PageIndex: {PageIndex}, TenantId: {TenantId}",
                    pageSize, pageIndex, tenantId);
                _logger.LogInformation("QueryTeams endpoint: {QueryTeams}", _options.QueryTeams);

                _client.DefaultRequestHeaders.Clear();
                _logger.LogDebug("Cleared default request headers");

                var tokenInfo = await GetTokenAsync();
                _logger.LogInformation("Retrieved token - Type: {TokenType}", tokenInfo?.TokenType);

                _client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(tokenInfo.TokenType, tokenInfo.AccessToken);

                // Add X-App-Id header for public API endpoints
                if (!string.IsNullOrEmpty(_options.AppId))
                {
                    _client.DefaultRequestHeaders.Add("X-App-Id", _options.AppId);
                    _logger.LogDebug("Added X-App-Id header: {AppId}", _options.AppId);
                }

                // Build request URI with TenantId parameter
                var requestUri = $"{_options.QueryTeams}?PageIndex={pageIndex}&PageSize={pageSize}";
                if (!string.IsNullOrEmpty(tenantId))
                {
                    requestUri += $"&TenantId={tenantId}";
                }

                var fullUrl = $"{_client.BaseAddress?.ToString().TrimEnd('/')}{requestUri}";
                _logger.LogInformation("Making GET request to full URL: {FullUrl}", fullUrl);

                // Output cURL command for debugging
                try
                {
                    var tokenPreview = string.IsNullOrEmpty(tokenInfo.AccessToken) ? "[NULL]" :
                                      tokenInfo.AccessToken.Length > 20 ?
                                      $"{tokenInfo.AccessToken.Substring(0, 20)}..." :
                                      tokenInfo.AccessToken;
                    var appIdHeader = !string.IsNullOrEmpty(_options.AppId) ? $" -H \"X-App-Id: {_options.AppId}\"" : "";
                    var curlCommand = $"curl -X GET \"{fullUrl}\" -H \"Authorization: {tokenInfo.TokenType} {tokenPreview}\"{appIdHeader}";
                    _logger.LogInformation("🔍 Teams API cURL equivalent: {CurlCommand}", curlCommand);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to generate cURL command: {Exception}", ex.Message);
                }

                var startTime = DateTimeOffset.UtcNow;
                using var resp = await _client.GetAsync(requestUri);
                var elapsed = DateTimeOffset.UtcNow - startTime;

                _logger.LogInformation("IDM Teams API response - StatusCode: {StatusCode}, Elapsed: {ElapsedMs}ms",
                    resp.StatusCode, elapsed.TotalMilliseconds);

                if (!resp.IsSuccessStatusCode)
                {
                    var errorContent = await resp.Content.ReadAsStringAsync();
                    _logger.LogError("IDM Teams API request failed:");
                    _logger.LogError("  Status Code: {StatusCode}", resp.StatusCode);
                    _logger.LogError("  Error Content: {ErrorContent}", errorContent);
                    _logger.LogError("  Request URI: {RequestUri}", requestUri);
                    _logger.LogError("  Full URL: {FullUrl}", fullUrl);
                    throw new HttpRequestException($"IDM Teams API request failed: {resp.StatusCode} - {errorContent}");
                }

                var responseContent = await resp.Content.ReadAsStringAsync();
                _logger.LogDebug("Raw teams response content (first 500 chars): {Content}",
                    responseContent.Length > 500 ? responseContent.Substring(0, 500) + "..." : responseContent);

                var result = JsonSerializer.Deserialize<BasicResponse<PageModel<List<IdmTeamDto>>>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result == null)
                {
                    _logger.LogError("Failed to deserialize teams response - result is null");
                    throw new Exception("Failed to deserialize IDM Teams API response");
                }

                _logger.LogInformation("Teams deserialization successful - Status: {Status}, Code: {Code}, Message: {Message}",
                    result.Status, result.Code, result.Message);

                if (result.Data == null)
                {
                    _logger.LogWarning("IDM Teams API returned null data in result");
                    return new PageModel<List<IdmTeamDto>>
                    {
                        PageIndex = pageIndex,
                        PageSize = pageSize,
                        PageCount = 0,
                        DataCount = 0,
                        Data = new List<IdmTeamDto>()
                    };
                }

                var teamCount = result.Data.Data?.Count ?? 0;
                _logger.LogInformation("=== GetAllTeamsAsync Success ===");
                _logger.LogInformation("Retrieved {TeamCount} teams from IDM", teamCount);
                _logger.LogInformation("Pagination - Page {PageIndex} of {PageCount}, Total: {DataCount}",
                    result.Data.PageIndex, result.Data.PageCount, result.Data.DataCount);

                if (teamCount > 0)
                {
                    var firstTeam = result.Data.Data.First();
                    _logger.LogDebug("First team sample - Id: {Id}, TeamName: {TeamName}, TeamMembers: {TeamMembers}",
                        firstTeam.Id, firstTeam.TeamName, firstTeam.TeamMembers);
                }

                return result.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== GetAllTeamsAsync Failed ===");
                _logger.LogError("Exception details: {ExceptionType} - {ExceptionMessage}", ex.GetType().Name, ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerExceptionType} - {InnerExceptionMessage}",
                        ex.InnerException.GetType().Name, ex.InnerException.Message);
                }
                throw;
            }
        }

        /// <summary>
        /// Get team tree structure from IDM teamTree API
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <returns>Hierarchical team tree structure</returns>
        public async Task<List<IdmTeamTreeNodeDto>> GetTeamTreeAsync(string tenantId = null)
        {
            try
            {
                _logger.LogInformation("=== Starting GetTeamTreeAsync ===");
                _logger.LogInformation("Parameters - TenantId: {TenantId}", tenantId);
                _logger.LogInformation("QueryTeamTree endpoint: {QueryTeamTree}", _options.QueryTeamTree);

                _client.DefaultRequestHeaders.Clear();
                _logger.LogDebug("Cleared default request headers");

                var tokenInfo = await GetTokenAsync();
                _logger.LogInformation("Retrieved token - Type: {TokenType}", tokenInfo?.TokenType);

                _client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(tokenInfo.TokenType, tokenInfo.AccessToken);

                // Add X-App-Id header for public API endpoints
                if (!string.IsNullOrEmpty(_options.AppId))
                {
                    _client.DefaultRequestHeaders.Add("X-App-Id", _options.AppId);
                    _logger.LogDebug("Added X-App-Id header: {AppId}", _options.AppId);
                }

                // Build request URI with TenantId parameter
                var requestUri = $"{_options.QueryTeamTree}?TenantId={tenantId ?? "1000"}";

                var fullUrl = $"{_client.BaseAddress?.ToString().TrimEnd('/')}{requestUri}";
                _logger.LogInformation("Making GET request to full URL: {FullUrl}", fullUrl);

                // Output cURL command for debugging
                try
                {
                    var tokenPreview = string.IsNullOrEmpty(tokenInfo.AccessToken) ? "[NULL]" :
                                      tokenInfo.AccessToken.Length > 20 ?
                                      $"{tokenInfo.AccessToken.Substring(0, 20)}..." :
                                      tokenInfo.AccessToken;
                    var appIdHeader = !string.IsNullOrEmpty(_options.AppId) ? $" -H \"X-App-Id: {_options.AppId}\"" : "";
                    var curlCommand = $"curl -X GET \"{fullUrl}\" -H \"Authorization: {tokenInfo.TokenType} {tokenPreview}\"{appIdHeader}";
                    _logger.LogInformation("🔍 TeamTree API cURL equivalent: {CurlCommand}", curlCommand);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to generate cURL command: {Exception}", ex.Message);
                }

                var startTime = DateTimeOffset.UtcNow;
                using var resp = await _client.GetAsync(requestUri);
                var elapsed = DateTimeOffset.UtcNow - startTime;

                _logger.LogInformation("IDM TeamTree API response - StatusCode: {StatusCode}, Elapsed: {ElapsedMs}ms",
                    resp.StatusCode, elapsed.TotalMilliseconds);

                if (!resp.IsSuccessStatusCode)
                {
                    var errorContent = await resp.Content.ReadAsStringAsync();
                    _logger.LogError("IDM TeamTree API request failed:");
                    _logger.LogError("  Status Code: {StatusCode}", resp.StatusCode);
                    _logger.LogError("  Error Content: {ErrorContent}", errorContent);
                    _logger.LogError("  Request URI: {RequestUri}", requestUri);
                    _logger.LogError("  Full URL: {FullUrl}", fullUrl);
                    throw new HttpRequestException($"IDM TeamTree API request failed: {resp.StatusCode} - {errorContent}");
                }

                var responseContent = await resp.Content.ReadAsStringAsync();
                _logger.LogDebug("Raw team tree response content (first 500 chars): {Content}",
                    responseContent.Length > 500 ? responseContent.Substring(0, 500) + "..." : responseContent);

                var result = JsonSerializer.Deserialize<BasicResponse<List<IdmTeamTreeNodeDto>>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result == null)
                {
                    _logger.LogError("Failed to deserialize team tree response - result is null");
                    throw new Exception("Failed to deserialize IDM TeamTree API response");
                }

                _logger.LogInformation("TeamTree deserialization successful - Status: {Status}, Code: {Code}, Message: {Message}",
                    result.Status, result.Code, result.Message);

                if (result.Data == null)
                {
                    _logger.LogWarning("IDM TeamTree API returned null data in result");
                    return new List<IdmTeamTreeNodeDto>();
                }

                var teamCount = result.Data.Count;
                _logger.LogInformation("=== GetTeamTreeAsync Success ===");
                _logger.LogInformation("Retrieved {TeamCount} root team nodes from IDM", teamCount);

                if (teamCount > 0)
                {
                    var firstTeam = result.Data.First();
                    _logger.LogDebug("First team sample - Value: {Value}, Label: {Label}, ChildrenCount: {ChildrenCount}",
                        firstTeam.Value, firstTeam.Label, firstTeam.Children?.Count ?? 0);
                }

                return result.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== GetTeamTreeAsync Failed ===");
                _logger.LogError("Exception details: {ExceptionType} - {ExceptionMessage}", ex.GetType().Name, ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerExceptionType} - {InnerExceptionMessage}",
                        ex.InnerException.GetType().Name, ex.InnerException.Message);
                }
                throw;
            }
        }

        /// <summary>
        /// Get current user info from IDM (includes UserType)
        /// </summary>
        /// <param name="authorization">Authorization header value (e.g., "Bearer token")</param>
        /// <returns>User information including UserType</returns>
        /// <summary>
        /// Get user information by user ID from IDM
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="authorization">User's authorization token (e.g., "Bearer token")</param>
        /// <returns>User information with permissions</returns>
        public async Task<IdmUserOutputDto> GetUserByIdAsync(string userId, string authorization)
        {
            try
            {
                _logger.LogInformation("=== GetUserByIdAsync Started ===");
                _logger.LogInformation("Requesting user info for UserId: {UserId}", userId);

                // Endpoint: /api/v1/users/{userId}
                var endpoint = $"/api/v1/users/{userId}";
                var url = $"{_options.BaseUrl}{endpoint}";

                _logger.LogInformation("Requesting user info from: {Url}", url);

                // Create request with user's authorization token
                var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                request.Headers.Add("Authorization", authorization);

                var startTime = DateTimeOffset.UtcNow;
                var resp = await _client.SendAsync(request);
                var elapsed = DateTimeOffset.UtcNow - startTime;

                _logger.LogInformation("IDM user info API response - StatusCode: {StatusCode}, Elapsed: {ElapsedMs}ms",
                    resp.StatusCode, elapsed.TotalMilliseconds);

                if (!resp.IsSuccessStatusCode)
                {
                    var errorContent = await resp.Content.ReadAsStringAsync();

                    // If Forbidden (403), try fallback to /api/v1/users/current/info
                    // This happens when a normal user tries to view their own info
                    if (resp.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        _logger.LogInformation(
                            "User {UserId} got Forbidden (403) from /api/v1/users/{UserId}, falling back to /api/v1/users/current/info",
                            userId, userId);

                        try
                        {
                            // Fallback to current user info endpoint
                            var currentUserInfo = await GetCurrentUserInfoAsync(authorization);
                            if (currentUserInfo != null)
                            {
                                _logger.LogInformation("Successfully retrieved user info via /api/v1/users/current/info fallback");
                                return currentUserInfo;
                            }
                        }
                        catch (Exception fallbackEx)
                        {
                            _logger.LogWarning(fallbackEx, "Fallback to /api/v1/users/current/info also failed");
                        }
                    }

                    _logger.LogError("IDM user info API request failed: {StatusCode} - {ErrorContent}",
                        resp.StatusCode, errorContent);
                    throw new HttpRequestException($"IDM user info API request failed: {resp.StatusCode} - {errorContent}");
                }

                var responseContent = await resp.Content.ReadAsStringAsync();
                _logger.LogDebug("Raw user info response content (first 500 chars): {Content}",
                    responseContent.Length > 500 ? responseContent.Substring(0, 500) + "..." : responseContent);

                var result = JsonSerializer.Deserialize<BasicResponse<IdmUserOutputDto>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result == null)
                {
                    _logger.LogError("Failed to deserialize user info response - result is null");
                    throw new Exception("Failed to deserialize IDM user info API response");
                }

                _logger.LogInformation("User info deserialization successful - Status: {Status}, Code: {Code}, Message: {Message}",
                    result.Status, result.Code, result.Message);

                if (result.Data == null)
                {
                    _logger.LogWarning("IDM user info API returned null data in result");
                    return null;
                }

                _logger.LogInformation("=== GetUserByIdAsync Success ===");
                _logger.LogInformation("Retrieved user info - UserId: {UserId}, UserName: {UserName}, UserPermissions: {PermissionCount}",
                    result.Data.Id, result.Data.Username, result.Data.UserPermissions?.Count ?? 0);

                // Log user permissions for debugging
                if (result.Data.UserPermissions != null && result.Data.UserPermissions.Any())
                {
                    foreach (var permission in result.Data.UserPermissions)
                    {
                        _logger.LogDebug("User permission - TenantId: {TenantId}, UserType: {UserType}, RoleIds: {RoleIds}",
                            permission.TenantId, permission.UserType, string.Join(", ", permission.RoleIds ?? new List<string>()));
                    }
                }

                return result.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== GetUserByIdAsync Failed ===");
                _logger.LogError("Exception details: {ExceptionType} - {ExceptionMessage}", ex.GetType().Name, ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerExceptionType} - {InnerExceptionMessage}",
                        ex.InnerException.GetType().Name, ex.InnerException.Message);
                }
                throw;
            }
        }

        /// <summary>
        /// Get current user information from IDM using /api/v1/users/current/info
        /// This endpoint allows users to view their own information with their own token
        /// </summary>
        /// <param name="authorization">User's authorization token (e.g., "Bearer token")</param>
        /// <returns>User information</returns>
        public async Task<IdmUserOutputDto> GetCurrentUserInfoAsync(string authorization)
        {
            try
            {
                _logger.LogInformation("=== GetCurrentUserInfoAsync Started ===");

                // Endpoint: /api/v1/users/current/info
                var endpoint = "/api/v1/users/current/info";
                var url = $"{_options.BaseUrl}{endpoint}";

                _logger.LogInformation("Requesting current user info from: {Url}", url);

                // Create request with user's authorization token
                var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                request.Headers.Add("Authorization", authorization);

                var startTime = DateTimeOffset.UtcNow;
                var resp = await _client.SendAsync(request);
                var elapsed = DateTimeOffset.UtcNow - startTime;

                _logger.LogInformation("IDM current user info API response - StatusCode: {StatusCode}, Elapsed: {ElapsedMs}ms",
                    resp.StatusCode, elapsed.TotalMilliseconds);

                if (!resp.IsSuccessStatusCode)
                {
                    var errorContent = await resp.Content.ReadAsStringAsync();
                    _logger.LogError("IDM current user info API request failed: {StatusCode} - {ErrorContent}",
                        resp.StatusCode, errorContent);
                    throw new HttpRequestException($"IDM current user info API request failed: {resp.StatusCode} - {errorContent}");
                }

                var responseContent = await resp.Content.ReadAsStringAsync();
                _logger.LogDebug("Raw current user info response content (first 500 chars): {Content}",
                    responseContent.Length > 500 ? responseContent.Substring(0, 500) + "..." : responseContent);

                var result = JsonSerializer.Deserialize<BasicResponse<IdmUserOutputDto>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result == null)
                {
                    _logger.LogError("Failed to deserialize current user info response - result is null");
                    throw new Exception("Failed to deserialize IDM current user info API response");
                }

                _logger.LogInformation("Current user info deserialization successful - Status: {Status}, Code: {Code}, Message: {Message}",
                    result.Status, result.Code, result.Message);

                if (result.Data == null)
                {
                    _logger.LogWarning("IDM current user info API returned null data in result");
                    return null;
                }

                _logger.LogInformation("=== GetCurrentUserInfoAsync Success ===");
                _logger.LogInformation("Retrieved current user info - UserId: {UserId}, UserName: {UserName}",
                    result.Data.Id, result.Data.Username);

                return result.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== GetCurrentUserInfoAsync Failed ===");
                _logger.LogError("Exception details: {ExceptionType} - {ExceptionMessage}", ex.GetType().Name, ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerExceptionType} - {InnerExceptionMessage}",
                        ex.InnerException.GetType().Name, ex.InnerException.Message);
                }
                throw;
            }
        }

        /// <summary>
        /// Get team users from IDM
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="pageSize">Page size (default 10000)</param>
        /// <param name="pageIndex">Page index (default 1)</param>
        /// <returns>Team user relationship list (API returns direct array, not paginated)</returns>
        public async Task<List<IdmTeamUserDto>> GetAllTeamUsersAsync(
            string tenantId = null,
            int pageSize = 10000,
            int pageIndex = 1)
        {
            try
            {
                _logger.LogInformation("=== Starting GetAllTeamUsersAsync ===");
                _logger.LogInformation("Parameters - PageSize: {PageSize}, PageIndex: {PageIndex}, TenantId: {TenantId}",
                    pageSize, pageIndex, tenantId);
                _logger.LogInformation("QueryTeamUsers endpoint: {QueryTeamUsers}", _options.QueryTeamUsers);

                _client.DefaultRequestHeaders.Clear();
                _logger.LogDebug("Cleared default request headers");

                var tokenInfo = await GetTokenAsync();
                _logger.LogInformation("Retrieved token - Type: {TokenType}", tokenInfo?.TokenType);

                _client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(tokenInfo.TokenType, tokenInfo.AccessToken);

                // Add X-App-Id header for public API endpoints
                if (!string.IsNullOrEmpty(_options.AppId))
                {
                    _client.DefaultRequestHeaders.Add("X-App-Id", _options.AppId);
                    _logger.LogDebug("Added X-App-Id header: {AppId}", _options.AppId);
                }

                // Build request URI with pagination and TenantId parameters
                var requestUri = $"{_options.QueryTeamUsers}?PageIndex={pageIndex}&PageSize={pageSize}";
                if (!string.IsNullOrEmpty(tenantId))
                {
                    requestUri += $"&TenantId={tenantId}";
                }

                var fullUrl = $"{_client.BaseAddress?.ToString().TrimEnd('/')}{requestUri}";
                _logger.LogInformation("Making GET request to full URL: {FullUrl}", fullUrl);

                // Output cURL command for debugging
                try
                {
                    var tokenPreview = string.IsNullOrEmpty(tokenInfo.AccessToken) ? "[NULL]" :
                                      tokenInfo.AccessToken.Length > 20 ?
                                      $"{tokenInfo.AccessToken.Substring(0, 20)}..." :
                                      tokenInfo.AccessToken;
                    var appIdHeader = !string.IsNullOrEmpty(_options.AppId) ? $" -H \"X-App-Id: {_options.AppId}\"" : "";
                    var curlCommand = $"curl -X GET \"{fullUrl}\" -H \"Authorization: {tokenInfo.TokenType} {tokenPreview}\"{appIdHeader}";
                    _logger.LogInformation("🔍 TeamUsers API cURL equivalent: {CurlCommand}", curlCommand);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to generate cURL command: {Exception}", ex.Message);
                }

                var startTime = DateTimeOffset.UtcNow;
                using var resp = await _client.GetAsync(requestUri);
                var elapsed = DateTimeOffset.UtcNow - startTime;

                _logger.LogInformation("IDM TeamUsers API response - StatusCode: {StatusCode}, Elapsed: {ElapsedMs}ms",
                    resp.StatusCode, elapsed.TotalMilliseconds);

                if (!resp.IsSuccessStatusCode)
                {
                    var errorContent = await resp.Content.ReadAsStringAsync();
                    _logger.LogError("IDM TeamUsers API request failed:");
                    _logger.LogError("  Status Code: {StatusCode}", resp.StatusCode);
                    _logger.LogError("  Error Content: {ErrorContent}", errorContent);
                    _logger.LogError("  Request URI: {RequestUri}", requestUri);
                    _logger.LogError("  Full URL: {FullUrl}", fullUrl);
                    throw new HttpRequestException($"IDM TeamUsers API request failed: {resp.StatusCode} - {errorContent}");
                }

                var responseContent = await resp.Content.ReadAsStringAsync();
                _logger.LogDebug("Raw team users response content (first 500 chars): {Content}",
                    responseContent.Length > 500 ? responseContent.Substring(0, 500) + "..." : responseContent);

                var result = JsonSerializer.Deserialize<BasicResponse<List<IdmTeamUserDto>>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result == null)
                {
                    _logger.LogError("Failed to deserialize team users response - result is null");
                    throw new Exception("Failed to deserialize IDM TeamUsers API response");
                }

                _logger.LogInformation("TeamUsers deserialization successful - Status: {Status}, Code: {Code}, Message: {Message}",
                    result.Status, result.Code, result.Message);

                if (result.Data == null)
                {
                    _logger.LogWarning("IDM TeamUsers API returned null data in result");
                    return new List<IdmTeamUserDto>();
                }

                var teamUserCount = result.Data.Count;
                _logger.LogInformation("=== GetAllTeamUsersAsync Success ===");
                _logger.LogInformation("Retrieved {TeamUserCount} team user relationships from IDM", teamUserCount);

                if (teamUserCount > 0)
                {
                    var firstTeamUser = result.Data.First();
                    _logger.LogDebug("First team user sample - Id: {Id}, UserName: {UserName}, TeamId: {TeamId}",
                        firstTeamUser.Id, firstTeamUser.UserName, firstTeamUser.TeamId);
                }

                return result.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== GetAllTeamUsersAsync Failed ===");
                _logger.LogError("Exception details: {ExceptionType} - {ExceptionMessage}", ex.GetType().Name, ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerExceptionType} - {InnerExceptionMessage}",
                        ex.InnerException.GetType().Name, ex.InnerException.Message);
                }
                throw;
            }
        }
    }
}
