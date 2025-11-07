using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.Options;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Application.Services.OW.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// AI模型配置服务实现
    /// </summary>
    public class AIModelConfigService : IAIModelConfigService
    {
        private readonly IAIModelConfigRepository _repository;
        private readonly ILogger<AIModelConfigService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly UserContext _userContext;
        private readonly IEncryptionService _encryptionService;
        private readonly AIOptions _aiOptions;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AIModelConfigService(
            IAIModelConfigRepository repository,
            ILogger<AIModelConfigService> logger,
            IHttpClientFactory httpClientFactory,
            UserContext userContext,
            IEncryptionService encryptionService,
            IOptions<AIOptions> aiOptions,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _userContext = userContext;
            _encryptionService = encryptionService;
            _aiOptions = aiOptions.Value;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 记录请求头信息（用于test）
        /// </summary>
        private void LogRequestHeaders()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null) return;

            _logger.LogDebug("=== Request Headers Debug ===");
            foreach (var header in httpContext.Request.Headers)
            {
                if (header.Key.ToLower().Contains("tenant") || header.Key.ToLower().Contains("app"))
                {
                    _logger.LogDebug("Header: {Key} = {Value}", header.Key, header.Value);
                }
            }
            _logger.LogDebug("=== End Request Headers ===");
        }

        /// <summary>
        /// 从HTTP请求头获取租户ID
        /// </summary>
        /// <returns>租户ID</returns>
        private string GetCurrentTenantId()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
            {
                _logger.LogWarning("HttpContext is null, using DEFAULT tenant");
                return "DEFAULT";
            }

            // 记录请求头（调试用）
            LogRequestHeaders();

            // 1. 优先从 X-Tenant-Id 请求头获取
            var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                _logger.LogDebug("Found TenantId from X-Tenant-Id header: {TenantId}", tenantId);
                return tenantId;
            }

            // 2. 从 x-tenant-id 请求头获取（小写）
            tenantId = httpContext.Request.Headers["x-tenant-id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
            {
                _logger.LogDebug("Found TenantId from x-tenant-id header: {TenantId}", tenantId);
                return tenantId;
            }

            // 3. Fallback到UserContext
            if (!string.IsNullOrEmpty(_userContext?.TenantId))
            {
                _logger.LogDebug("Using TenantId from UserContext: {TenantId}", _userContext.TenantId);
                return _userContext.TenantId;
            }

            // 4. 默认值
            _logger.LogWarning("No TenantId found in headers or UserContext, using DEFAULT");
            return "DEFAULT";
        }

        /// <summary>
        /// 从HTTP请求头获取应用代码
        /// </summary>
        /// <returns>应用代码</returns>
        private string GetCurrentAppCode()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
            {
                _logger.LogWarning("HttpContext is null, using DEFAULT app code");
                return "DEFAULT";
            }

            // 1. 优先从 X-App-Code 请求头获取
            var appCode = httpContext.Request.Headers["X-App-Code"].FirstOrDefault();
            if (!string.IsNullOrEmpty(appCode))
            {
                _logger.LogDebug("Found AppCode from X-App-Code header: {AppCode}", appCode);
                return appCode;
            }

            // 2. 从 x-app-code 请求头获取（小写）
            appCode = httpContext.Request.Headers["x-app-code"].FirstOrDefault();
            if (!string.IsNullOrEmpty(appCode))
            {
                _logger.LogDebug("Found AppCode from x-app-code header: {AppCode}", appCode);
                return appCode;
            }

            // 3. Fallback到UserContext
            if (!string.IsNullOrEmpty(_userContext?.AppCode))
            {
                _logger.LogDebug("Using AppCode from UserContext: {AppCode}", _userContext.AppCode);
                return _userContext.AppCode;
            }

            // 4. 默认值
            _logger.LogWarning("No AppCode found in headers or UserContext, using DEFAULT");
            return "DEFAULT";
        }

        /// <summary>
        /// 获取当前租户的所有AI模型配置
        /// </summary>
        /// <param name="userId">用户ID（保留兼容性，实际使用租户隔离）</param>
        /// <returns>AI模型配置列表</returns>
        public async Task<List<AIModelConfig>> GetUserAIModelConfigsAsync(long userId)
        {
            try
            {
                // 从HTTP请求头获取租户信息，实现真正的租户隔离
                var tenantId = GetCurrentTenantId();
                var appCode = GetCurrentAppCode();

                _logger.LogInformation("Getting AI model configurations for Tenant: {TenantId}, App: {AppCode}", tenantId, appCode);

                var configs = await _repository.GetByTenantAndAppAsync(tenantId, appCode);

                // 解密 API Key 用于返回给前端
                ProcessApiKeyEncryptionBatch(configs, false);

                return configs;
            }
            catch (Exception ex)
            {
                var tenantId = GetCurrentTenantId();
                var appCode = GetCurrentAppCode();
                _logger.LogError(ex, "Failed to get AI model configuration list, Tenant ID: {TenantId}, App Code: {AppCode}",
                    tenantId, appCode);
                return new List<AIModelConfig>();
            }
        }

        /// <summary>
        /// 获取当前租户的默认AI模型配置
        /// </summary>
        /// <param name="userId">用户ID（保留兼容性，实际使用租户隔离）</param>
        /// <returns>默认AI模型配置</returns>
        public async Task<AIModelConfig> GetUserDefaultConfigAsync(long userId)
        {
            try
            {
                // 从HTTP请求头获取租户信息，实现真正的租户隔离
                var tenantId = GetCurrentTenantId();
                var appCode = GetCurrentAppCode();

                _logger.LogInformation("Getting default AI model configuration for Tenant: {TenantId}, App: {AppCode}", tenantId, appCode);

                var config = await _repository.GetDefaultByTenantAndAppAsync(tenantId, appCode);

                // 解密 API Key 用于返回给前端
                if (config != null)
                {
                    ProcessApiKeyEncryption(config, false);
                }

                return config;
            }
            catch (Exception ex)
            {
                var tenantId = GetCurrentTenantId();
                var appCode = GetCurrentAppCode();
                _logger.LogError(ex, "Failed to get default AI model configuration, Tenant ID: {TenantId}, App Code: {AppCode}",
                    tenantId, appCode);
                return null;
            }
        }

        /// <summary>
        /// 通过ID获取AI模型配置
        /// </summary>
        /// <param name="configId">配置ID</param>
        /// <returns>AI模型配置</returns>
        public async Task<AIModelConfig> GetConfigByIdAsync(long configId)
        {
            try
            {
                // 使用租户隔离，仓储层会自动过滤
                var config = await _repository.GetByIdAsync(configId);

                // 解密 API Key 用于返回给调用方
                if (config != null)
                {
                    ProcessApiKeyEncryption(config, false);
                }

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get AI model configuration, Config ID: {ConfigId}", configId);
                return null;
            }
        }

        /// <summary>
        /// 测试AI模型连接
        /// </summary>
        /// <param name="configId">配置ID</param>
        /// <returns>测试结果</returns>
        public async Task<bool> TestConfigConnectionAsync(long configId)
        {
            try
            {
                // 获取配置
                var config = await GetConfigByIdAsync(configId);
                if (config == null)
                {
                    _logger.LogWarning("Configuration for connection test does not exist, Config ID: {ConfigId}", configId);
                    return false;
                }

                // 调用详细测试方法
                var result = await TestConnectionAsync(config);
                return result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test AI model connection, Config ID: {ConfigId}", configId);
                return false;
            }
        }

        /// <summary>
        /// 创建AI模型配置
        /// </summary>
        /// <param name="config">AI模型配置</param>
        /// <returns>创建的配置ID</returns>
        public async Task<long> CreateConfigAsync(AIModelConfig config)
        {
            try
            {
                // 从HTTP请求头获取租户信息
                var tenantId = GetCurrentTenantId();
                var appCode = GetCurrentAppCode();

                // 设置配置的租户信息
                config.TenantId = tenantId;
                config.AppCode = appCode;

                _logger.LogInformation("Creating AI model configuration for Tenant: {TenantId}, App: {AppCode}, Provider: {Provider}",
                    tenantId, appCode, config.Provider);

                // 注释掉提供商重复检查，允许同一租户下同一提供商的多个配置
                // var existingConfig = await _repository.GetProviderConfigAsync(tenantId, appCode, config.Provider);
                // if (existingConfig != null)
                // {
                //     _logger.LogWarning("Tenant already has configuration for the same provider, Tenant ID: {TenantId}, App Code: {AppCode}, Provider: {Provider}", 
                //         tenantId, appCode, config.Provider);
                //     throw new Exception($"Configuration for {config.Provider} provider already exists");
                // }

                _logger.LogInformation("Creating new AI model configuration, Tenant ID: {TenantId}, App Code: {AppCode}, Provider: {Provider}",
                    tenantId, appCode, config.Provider);

                // 新创建的配置默认不设为默认配置，用户可以通过UI手动设置
                // 保持 config.IsDefault 的原始值（通常为false）

                // 初始化雪花ID
                config.InitNewId();

                // 设置默认值
                if (config.LastCheckTime == null)
                {
                    config.LastCheckTime = DateTime.UtcNow;
                }

                // 使用UserContext初始化创建信息（包含租户信息）
                config.InitCreateInfo(_userContext);

                // 存储前加密 API Key
                ProcessApiKeyEncryption(config, true);

                // 创建配置
                await _repository.InsertAsync(config);
                return config.Id;
            }
            catch (Exception ex)
            {
                var tenantId = GetCurrentTenantId();
                var appCode = GetCurrentAppCode();
                _logger.LogError(ex, "Failed to create AI model configuration, Tenant ID: {TenantId}, App Code: {AppCode}, Provider: {Provider}",
                    tenantId, appCode, config.Provider);
                throw;
            }
        }

        /// <summary>
        /// 更新AI模型配置
        /// </summary>
        /// <param name="config">AI模型配置</param>
        /// <returns>操作结果</returns>
        public async Task<bool> UpdateConfigAsync(AIModelConfig config)
        {
            try
            {
                // 使用租户隔离获取原配置
                var existingConfig = await _repository.GetByIdAsync(config.Id);
                if (existingConfig == null)
                {
                    _logger.LogWarning("Configuration to update does not exist, Config ID: {ConfigId}", config.Id);
                    return false;
                }

                // 更新配置
                existingConfig.Provider = config.Provider;
                existingConfig.ApiKey = config.ApiKey;
                existingConfig.BaseUrl = config.BaseUrl;
                existingConfig.ApiVersion = config.ApiVersion;
                existingConfig.ModelName = config.ModelName;
                existingConfig.Temperature = config.Temperature;
                existingConfig.MaxTokens = config.MaxTokens;
                existingConfig.EnableStreaming = config.EnableStreaming;
                existingConfig.IsDefault = config.IsDefault;
                existingConfig.Remarks = config.Remarks;

                // 使用UserContext初始化更新信息
                existingConfig.InitUpdateInfo(_userContext);

                // 更新前加密 API Key
                ProcessApiKeyEncryption(existingConfig, true);

                return await _repository.UpdateAsync(existingConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update AI model configuration, Config ID: {ConfigId}", config.Id);
                return false;
            }
        }

        /// <summary>
        /// 删除AI模型配置
        /// </summary>
        /// <param name="configId">配置ID</param>
        /// <returns>操作结果</returns>
        public async Task<bool> DeleteConfigAsync(long configId)
        {
            try
            {
                // 使用租户隔离获取配置
                var config = await _repository.GetByIdAsync(configId);
                if (config == null)
                {
                    _logger.LogWarning("Configuration to delete does not exist, Config ID: {ConfigId}", configId);
                    return false;
                }

                // 如果是默认配置，不允许删除
                if (config.IsDefault)
                {
                    _logger.LogWarning("Cannot delete default configuration, Config ID: {ConfigId}", configId);
                    throw new Exception("Cannot delete default configuration, please set another configuration as default first");
                }

                // 软删除配置
                config.IsValid = false;

                // 使用UserContext初始化更新信息
                config.InitUpdateInfo(_userContext);

                return await _repository.UpdateAsync(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete AI model configuration, Config ID: {ConfigId}", configId);
                throw;
            }
        }

        /// <summary>
        /// 设置为默认配置
        /// </summary>
        /// <param name="configId">配置ID</param>
        /// <returns>操作结果</returns>
        public async Task<bool> SetAsDefaultAsync(long configId)
        {
            try
            {
                // 从HTTP请求头获取租户信息
                var tenantId = GetCurrentTenantId();
                var appCode = GetCurrentAppCode();

                _logger.LogInformation("Setting default AI model configuration for Tenant: {TenantId}, App: {AppCode}, Config ID: {ConfigId}",
                    tenantId, appCode, configId);

                // 使用租户隔离获取配置
                var config = await _repository.GetByIdAsync(configId);
                if (config == null)
                {
                    _logger.LogWarning("Configuration to set as default does not exist, Config ID: {ConfigId}", configId);
                    return false;
                }

                // 验证配置属于当前租户
                if (config.TenantId != tenantId || config.AppCode != appCode)
                {
                    _logger.LogWarning("Configuration does not belong to current tenant, Config ID: {ConfigId}, Config Tenant: {ConfigTenantId}, Current Tenant: {CurrentTenantId}",
                        configId, config.TenantId, tenantId);
                    return false;
                }

                // 使用当前租户信息设置默认配置
                return await _repository.SetAsDefaultAsync(configId, tenantId, appCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set default AI model configuration, Config ID: {ConfigId}", configId);
                return false;
            }
        }

        /// <summary>
        /// 测试AI模型配置连接
        /// </summary>
        /// <param name="config">AI模型配置</param>
        /// <returns>测试结果</returns>
        public async Task<AIModelTestResult> TestConnectionAsync(AIModelConfig config)
        {
            var result = new AIModelTestResult
            {
                Success = false,
                Message = "Unknown error",
                ModelInfo = string.Empty,
                ResponseTime = 0
            };

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            HttpClient httpClient = null;

            try
            {
                // 创建配置的副本用于测试，避免修改原始配置
                var testConfig = new AIModelConfig
                {
                    Id = config.Id, // 保留原始ID用于更新数据库
                    Provider = config.Provider,
                    ApiKey = config.ApiKey,
                    BaseUrl = config.BaseUrl,
                    ApiVersion = config.ApiVersion,
                    ModelName = config.ModelName,
                    Temperature = config.Temperature,
                    MaxTokens = config.MaxTokens
                };

                // 智能处理API Key：检测是否需要解密
                // 来源1：GetConfigByIdAsync - API Key已解密
                // 来源2：前端传入的新配置 - API Key是明文
                // 来源3：数据库直接查询 - API Key是加密的
                if (IsLikelyEncryptedApiKey(testConfig.ApiKey))
                {
                    try
                    {
                        testConfig.ApiKey = DecryptApiKey(testConfig.ApiKey);
                        _logger.LogDebug("Decrypted API key for connection test");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to decrypt API key, assuming it's already plain text");
                    }
                }

                config = testConfig;
                httpClient = _httpClientFactory.CreateClient();
                // Use configurable timeout for AI model connection tests
                // AI services may need more time to respond, especially for initial connections
                var timeoutSeconds = _aiOptions.ConnectionTest.TimeoutSeconds;
                httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

                if (_aiOptions.ConnectionTest.EnableDetailedLogging)
                {
                    _logger.LogInformation("Starting AI model connection test, Provider: {Provider}, Timeout: {Timeout}s",
                        config.Provider, timeoutSeconds);
                }

                // Add retry mechanism for connection tests
                var maxRetries = _aiOptions.ConnectionTest.MaxRetryAttempts;
                var retryDelay = _aiOptions.ConnectionTest.RetryDelayMs;

                for (int attempt = 1; attempt <= maxRetries + 1; attempt++)
                {
                    try
                    {
                        if (attempt > 1 && _aiOptions.ConnectionTest.EnableDetailedLogging)
                        {
                            _logger.LogInformation("Retrying connection test, Attempt: {Attempt}/{MaxAttempts}",
                                attempt, maxRetries + 1);
                        }

                        // 根据不同的提供商，使用不同的测试端点和方法
                        switch (config.Provider.ToLower())
                        {
                            case "zhipuai":
                                result = await TestZhipuAIAsync(httpClient, config);
                                break;
                            case "openai":
                                result = await TestOpenAIAsync(httpClient, config);
                                break;
                            case "claude":
                                result = await TestClaudeAsync(httpClient, config);
                                break;
                            case "deepseek":
                                result = await TestDeepSeekAsync(httpClient, config);
                                break;
                            case "item":
                            case "llmgateway": // Backward compatibility
                                result = await TestItemGatewayAsync(httpClient, config);
                                break;
                            default:
                                result.Message = $"Unsupported AI provider: {config.Provider}";
                                break;
                        }

                        // If successful or this is the last attempt, break the retry loop
                        if (result.Success || attempt >= maxRetries + 1)
                        {
                            break;
                        }

                        // Wait before next retry
                        if (attempt < maxRetries + 1)
                        {
                            await Task.Delay(retryDelay);
                        }
                    }
                    catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || ex.Message.Contains("timeout"))
                    {
                        result.Success = false;
                        result.Message = $"Connection timeout after {timeoutSeconds} seconds (Attempt {attempt}/{maxRetries + 1}). The AI service may be slow to respond or unavailable.";

                        if (attempt < maxRetries + 1)
                        {
                            if (_aiOptions.ConnectionTest.EnableDetailedLogging)
                            {
                                _logger.LogWarning("Connection timeout on attempt {Attempt}, retrying in {Delay}ms",
                                    attempt, retryDelay);
                            }
                            await Task.Delay(retryDelay);
                            continue;
                        }

                        _logger.LogWarning(ex, "AI model connection test timed out after {Attempts} attempts, Provider: {Provider}, Timeout: {Timeout}s",
                            maxRetries + 1, config.Provider, timeoutSeconds);
                        break;
                    }
                    catch (HttpRequestException ex)
                    {
                        result.Success = false;
                        result.Message = $"Network error (Attempt {attempt}/{maxRetries + 1}): {ex.Message}";

                        if (attempt < maxRetries + 1)
                        {
                            if (_aiOptions.ConnectionTest.EnableDetailedLogging)
                            {
                                _logger.LogWarning("Network error on attempt {Attempt}, retrying in {Delay}ms: {Error}",
                                    attempt, retryDelay, ex.Message);
                            }
                            await Task.Delay(retryDelay);
                            continue;
                        }

                        _logger.LogWarning(ex, "Network error during AI model connection test after {Attempts} attempts, Provider: {Provider}",
                            maxRetries + 1, config.Provider);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Connection test exception: {ex.Message}";
                _logger.LogError(ex, "Failed to test AI model configuration connection, Provider: {Provider}", config.Provider);
            }
            finally
            {
                stopwatch.Stop();
                result.ResponseTime = stopwatch.ElapsedMilliseconds;
            }

            // 更新配置的可用状态
            if (config.Id > 0)
            {
                try
                {
                    var updateResult = await _repository.UpdateAvailabilityAsync(config.Id, result.Success);
                    _logger.LogInformation("Updated availability status for config ID: {ConfigId}, IsAvailable: {IsAvailable}, UpdateResult: {UpdateResult}",
                        config.Id, result.Success, updateResult);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update availability status for config ID: {ConfigId}", config.Id);
                }
            }
            else
            {
                _logger.LogWarning("Cannot update availability status: Invalid config ID {ConfigId}", config.Id);
            }

            return result;
        }

        #region 私有方法

        /// <summary>
        /// 测试智谱AI连接
        /// </summary>
        private async Task<AIModelTestResult> TestZhipuAIAsync(HttpClient httpClient, AIModelConfig config)
        {
            var result = new AIModelTestResult();

            try
            {
                // ZhipuAI 使用聊天接口进行测试
                var url = config.BaseUrl.TrimEnd('/');
                if (!url.EndsWith("/chat/completions"))
                {
                    url = $"{config.BaseUrl.TrimEnd('/')}/chat/completions";
                }

                // 设置请求头
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");

                // 创建测试请求体
                var testPayload = new
                {
                    model = config.ModelName,
                    messages = new[]
                    {
                        new { role = "user", content = "Hello" }
                    },
                    max_tokens = 10,
                    temperature = 0.1
                };

                var jsonContent = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(testPayload),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                // 发送POST请求测试连接
                var response = await httpClient.PostAsync(url, jsonContent);

                // 检查响应
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    result.Success = true;
                    result.Message = "Connection successful";
                    result.ModelInfo = content;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    result.Success = false;
                    result.Message = $"Connection failed, Status code: {response.StatusCode}, Reason: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Connection exception: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 测试OpenAI连接
        /// </summary>
        private async Task<AIModelTestResult> TestOpenAIAsync(HttpClient httpClient, AIModelConfig config)
        {
            var result = new AIModelTestResult();

            try
            {
                // OpenAI的模型列表接口
                var url = $"{config.BaseUrl.TrimEnd('/')}/v1/models";

                // 设置请求头
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");

                // 发送请求
                var response = await httpClient.GetAsync(url);

                // 检查响应
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    result.Success = true;
                    result.Message = "Connection successful";
                    result.ModelInfo = content;
                }
                else
                {
                    result.Success = false;
                    result.Message = $"Connection failed, Status code: {response.StatusCode}, Reason: {await response.Content.ReadAsStringAsync()}";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Connection exception: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 测试Claude连接
        /// </summary>
        private async Task<AIModelTestResult> TestClaudeAsync(HttpClient httpClient, AIModelConfig config)
        {
            var result = new AIModelTestResult();

            try
            {
                // Claude的模型列表接口
                var url = $"{config.BaseUrl.TrimEnd('/')}/v1/models";

                // 设置请求头
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("x-api-key", config.ApiKey);
                httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                // 发送请求
                var response = await httpClient.GetAsync(url);

                // 检查响应
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    result.Success = true;
                    result.Message = "Connection successful";
                    result.ModelInfo = content;
                }
                else
                {
                    result.Success = false;
                    result.Message = $"Connection failed, Status code: {response.StatusCode}, Reason: {await response.Content.ReadAsStringAsync()}";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Connection exception: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 测试DeepSeek连接
        /// </summary>
        private async Task<AIModelTestResult> TestDeepSeekAsync(HttpClient httpClient, AIModelConfig config)
        {
            var result = new AIModelTestResult();

            try
            {
                // DeepSeek的模型列表接口
                var url = $"{config.BaseUrl.TrimEnd('/')}/v1/models";

                // 设置请求头
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");

                // 发送请求
                var response = await httpClient.GetAsync(url);

                // 检查响应
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    result.Success = true;
                    result.Message = "Connection successful";
                    result.ModelInfo = content;
                }
                else
                {
                    result.Success = false;
                    result.Message = $"Connection failed, Status code: {response.StatusCode}, Reason: {await response.Content.ReadAsStringAsync()}";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Connection exception: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 测试 Item Gateway 连接
        /// </summary>
        private async Task<AIModelTestResult> TestItemGatewayAsync(HttpClient httpClient, AIModelConfig config)
        {
            var result = new AIModelTestResult();

            try
            {
                // Step 1: Get JWT Token
                var jwtUrl = $"{config.BaseUrl.TrimEnd('/')}/admin/api/credentials/jwt";

                var jwtRequestBody = new
                {
                    apiKey = config.ApiKey,
                    tenantId = "",
                    agentCode = "w",
                    agentName = "w",
                    appCode = "wfe",
                    userId = "",
                    userName = ""
                };

                var jwtJson = JsonSerializer.Serialize(jwtRequestBody);
                var jwtContent = new StringContent(jwtJson, Encoding.UTF8, "application/json");

                httpClient.DefaultRequestHeaders.Clear();
                var jwtResponse = await httpClient.PostAsync(jwtUrl, jwtContent);
                var jwtResponseContent = await jwtResponse.Content.ReadAsStringAsync();

                if (!jwtResponse.IsSuccessStatusCode)
                {
                    result.Success = false;
                    result.Message = $"Failed to obtain JWT token, Status code: {jwtResponse.StatusCode}, Reason: {jwtResponseContent}";
                    return result;
                }

                // Parse JWT response
                var jwtData = JsonSerializer.Deserialize<JsonElement>(jwtResponseContent);
                if (!jwtData.TryGetProperty("code", out var code) || code.GetInt32() != 0)
                {
                    result.Success = false;
                    result.Message = $"JWT token request failed: {jwtResponseContent}";
                    return result;
                }

                var jwtToken = jwtData.GetProperty("data").GetString();
                if (string.IsNullOrEmpty(jwtToken))
                {
                    result.Success = false;
                    result.Message = "JWT token is empty";
                    return result;
                }

                // Step 2: Test chat completions API with JWT token
                var chatUrl = $"{config.BaseUrl.TrimEnd('/')}/openai/v1/chat/completions";

                var chatRequestBody = new
                {
                    model = config.ModelName ?? "openai/gpt-4o-mini", // Use a small model for testing
                    messages = new[]
                    {
                        new { role = "user", content = "Hello" }
                    },
                    max_tokens = 10
                };

                var chatJson = JsonSerializer.Serialize(chatRequestBody);
                var chatContent = new StringContent(chatJson, Encoding.UTF8, "application/json");

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {jwtToken}");

                var chatResponse = await httpClient.PostAsync(chatUrl, chatContent);
                var chatResponseContent = await chatResponse.Content.ReadAsStringAsync();

                if (chatResponse.IsSuccessStatusCode)
                {
                    result.Success = true;
                    result.Message = "Connection successful";
                    result.ModelInfo = $"JWT Token obtained and chat API tested successfully. Model: {config.ModelName}";
                }
                else
                {
                    result.Success = false;
                    result.Message = $"Chat API test failed, Status code: {chatResponse.StatusCode}, Reason: {chatResponseContent}";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Connection exception: {ex.Message}";
            }

            return result;
        }

        #endregion

        #region API Key Encryption Helpers

        /// <summary>
        /// 加密 API Key
        /// </summary>
        /// <param name="apiKey">明文 API Key</param>
        /// <returns>加密后的 API Key</returns>
        private string EncryptApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                return string.Empty;
            }

            try
            {
                return _encryptionService.Encrypt(apiKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt API key");
                throw new Exception("Failed to encrypt API key for security reasons");
            }
        }

        /// <summary>
        /// 解密 API Key
        /// </summary>
        /// <param name="encryptedApiKey">加密的 API Key</param>
        /// <returns>解密后的 API Key</returns>
        private string DecryptApiKey(string encryptedApiKey)
        {
            if (string.IsNullOrEmpty(encryptedApiKey))
            {
                return string.Empty;
            }

            try
            {
                return _encryptionService.Decrypt(encryptedApiKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt API key");
                throw new Exception("Failed to decrypt API key");
            }
        }

        /// <summary>
        /// 处理配置中的 API Key 加密
        /// </summary>
        /// <param name="config">AI 模型配置</param>
        /// <param name="isForStorage">是否用于存储（true=加密，false=解密）</param>
        private void ProcessApiKeyEncryption(AIModelConfig config, bool isForStorage)
        {
            if (config == null) return;

            if (isForStorage)
            {
                // 存储前加密 API Key
                config.ApiKey = EncryptApiKey(config.ApiKey);
            }
            else
            {
                // 读取后解密 API Key，如果解密失败则保持原始值
                try
                {
                    config.ApiKey = DecryptApiKey(config.ApiKey);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to decrypt API key for config ID: {ConfigId}, keeping original value", config.Id);
                    // 保持原始的加密值，或者如果是明文则保持明文
                    // 这样可以避免因为解密失败而丢失整个配置列表
                }
            }
        }

        /// <summary>
        /// 批量处理配置列表中的 API Key 加密
        /// </summary>
        /// <param name="configs">AI 模型配置列表</param>
        /// <param name="isForStorage">是否用于存储（true=加密，false=解密）</param>
        private void ProcessApiKeyEncryptionBatch(List<AIModelConfig> configs, bool isForStorage)
        {
            if (configs == null) return;

            foreach (var config in configs)
            {
                try
                {
                    ProcessApiKeyEncryption(config, isForStorage);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process API key encryption for config ID: {ConfigId}, skipping this config", config.Id);
                    // 继续处理其他配置，不让单个配置的错误影响整个列表
                }
            }
        }

        /// <summary>
        /// 检测API Key是否可能已加密
        /// </summary>
        /// <param name="apiKey">API Key字符串</param>
        /// <returns>是否可能已加密</returns>
        private bool IsLikelyEncryptedApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return false;

            try
            {
                // 检查是否是有效的Base64字符串（加密后的格式）
                var buffer = Convert.FromBase64String(apiKey);

                // 加密后的字符串通常会比较长（至少32字符）
                // 且不会包含典型的API Key格式
                return apiKey.Length >= 32 &&
                       buffer.Length >= 16 &&
                       !apiKey.Contains(" ") &&
                       !apiKey.StartsWith("sk-") && // OpenAI API key格式
                       !apiKey.StartsWith("glm-") && // ZhipuAI API key格式
                       !apiKey.StartsWith("claude-") && // Claude API key格式
                       !apiKey.Contains(".") && // 避免JWT token格式
                       !apiKey.StartsWith("Bearer "); // 避免Bearer token格式
            }
            catch
            {
                // 不是有效的Base64，可能是明文API Key
                return false;
            }
        }

        #endregion

    }
}