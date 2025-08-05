using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.Options;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Application.Services.OW.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public AIModelConfigService(
            IAIModelConfigRepository repository,
            ILogger<AIModelConfigService> logger,
            IHttpClientFactory httpClientFactory,
            UserContext userContext)
        {
            _repository = repository;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _userContext = userContext;
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
                // 使用租户隔离查询，不再依赖用户ID
                return await _repository.GetByTenantAndAppAsync(_userContext.TenantId, _userContext.AppCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get AI model configuration list, Tenant ID: {TenantId}, App Code: {AppCode}", 
                    _userContext.TenantId, _userContext.AppCode);
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
                // 使用租户隔离查询，不再依赖用户ID
                return await _repository.GetDefaultByTenantAndAppAsync(_userContext.TenantId, _userContext.AppCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get default AI model configuration, Tenant ID: {TenantId}, App Code: {AppCode}", 
                    _userContext.TenantId, _userContext.AppCode);
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
                return await _repository.GetByIdAsync(configId);
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
                // 检查是否已存在相同提供商的配置（使用租户隔离）
                var existingConfig = await _repository.GetProviderConfigAsync(config.TenantId, config.AppCode, config.Provider);
                if (existingConfig != null)
                {
                    _logger.LogWarning("Tenant already has configuration for the same provider, Tenant ID: {TenantId}, App Code: {AppCode}, Provider: {Provider}", config.TenantId, config.AppCode, config.Provider);
                    throw new Exception($"Configuration for {config.Provider} provider already exists");
                }

                // 如果是第一个配置，设为默认（使用租户隔离查询）
                var configs = await _repository.GetByTenantAndAppAsync(config.TenantId, config.AppCode);
                if (configs == null || configs.Count == 0)
                {
                    config.IsDefault = true;
                }

                // 初始化雪花ID
                config.InitNewId();

                // 设置默认值
                if (config.LastCheckTime == null)
                {
                    config.LastCheckTime = DateTime.UtcNow;
                }

                // 使用UserContext初始化创建信息（包含租户信息）
                config.InitCreateInfo(_userContext);

                // 创建配置
                await _repository.InsertAsync(config);
                return config.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create AI model configuration, Tenant ID: {TenantId}, App Code: {AppCode}, Provider: {Provider}", config.TenantId, config.AppCode, config.Provider);
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
                // 使用租户隔离获取配置
                var config = await _repository.GetByIdAsync(configId);
                if (config == null)
                {
                    _logger.LogWarning("Configuration to set as default does not exist, Config ID: {ConfigId}", configId);
                    return false;
                }

                // 由于租户隔离，仓储方法需要调整为不需要userId
                // 这里需要修改仓储方法的实现
                return await _repository.SetAsDefaultAsync(configId, config.TenantId, config.AppCode);
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

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);

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
                    default:
                        result.Message = $"Unsupported AI provider: {config.Provider}";
                        break;
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
                await _repository.UpdateAvailabilityAsync(config.Id, result.Success);
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

        #endregion
    }
} 