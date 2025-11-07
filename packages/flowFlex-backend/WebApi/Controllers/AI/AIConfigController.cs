using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.OW;
using Item.Internal.StandardApi.Response;
using System.Net;
using FlowFlex.Domain.Entities.OW;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowFlex.WebApi.Controllers.AI
{
    /// <summary>
    /// AI Model Configuration API
    /// 提供AI模型配置管理功能
    /// </summary>
    [ApiController]
    [Route("ai/config/v{version:apiVersion}")]
    [Display(Name = "AI Model Configuration Service")]
    [Tags("AI", "Configuration", "Model Management")]
    [Authorize]
    public class AIConfigController : Controllers.ControllerBase
    {
        private readonly IAIModelConfigService _configService;
        private readonly ILogger<AIConfigController> _logger;
        private readonly IOperatorContextService _operatorContextService;

        public AIConfigController(
            IAIModelConfigService configService,
            ILogger<AIConfigController> logger,
            IOperatorContextService operatorContextService)
        {
            _configService = configService;
            _logger = logger;
            _operatorContextService = operatorContextService;
        }

        /// <summary>
        /// 获取当前租户的所有AI模型配置
        /// </summary>
        /// <returns>AI模型配置列表</returns>
        [HttpGet("models")]
        [ProducesResponseType<SuccessResponse<List<AIModelConfig>>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> GetUserAIModels()
        {
            try
            {
                // 使用租户隔离查询，只返回当前租户的配置
                // 服务层通过UserContext自动获取租户信息，不再依赖用户ID
                var userId = GetCurrentUserId(); // 保留用于兼容性，但服务层实际使用租户隔离
                var configs = await _configService.GetUserAIModelConfigsAsync(userId);
                return Success(configs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get tenant AI model configurations");
                return BadRequest("Failed to get AI model configurations");
            }
        }

        /// <summary>
        /// 获取当前租户的默认AI模型配置
        /// </summary>
        /// <returns>默认AI模型配置</returns>
        [HttpGet("models/default")]
        [ProducesResponseType<SuccessResponse<AIModelConfig>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> GetDefaultAIModel()
        {
            try
            {
                // 使用租户隔离查询，只返回当前租户的默认配置
                // 服务层通过UserContext自动获取租户信息，不再依赖用户ID
                var userId = GetCurrentUserId(); // 保留用于兼容性，但服务层实际使用租户隔离
                var config = await _configService.GetUserDefaultConfigAsync(userId);
                if (config == null)
                {
                    return BadRequest("Default AI model configuration not found");
                }
                return Success(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get tenant default AI model configuration");
                return BadRequest("Failed to get default AI model configuration");
            }
        }

        /// <summary>
        /// 创建AI模型配置
        /// </summary>
        /// <param name="config">AI模型配置</param>
        /// <returns>创建结果</returns>
        [HttpPost("models")]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> CreateAIModel([FromBody] AIModelConfig config)
        {
            try
            {
                // 使用租户隔离，UserContext会自动处理租户信息
                // 服务层会从UserContext自动获取TenantId和AppCode
                var operatorId = _operatorContextService.GetOperatorId();
                var operatorName = _operatorContextService.GetOperatorDisplayName();
                var currentTime = DateTime.UtcNow;

                config.UserId = operatorId; // 保留用于审计追踪
                config.CreatedBy = operatorId;
                config.CreatedByName = operatorName;
                config.CreatedTime = currentTime;
                config.UpdatedBy = operatorId;
                config.UpdatedByName = operatorName;
                config.UpdatedTime = currentTime;

                var id = await _configService.CreateConfigAsync(config);
                return Success(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create AI model configuration");
                return BadRequest($"Failed to create AI model configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新AI模型配置
        /// </summary>
        /// <param name="id">配置ID</param>
        /// <param name="config">AI模型配置</param>
        /// <returns>更新结果</returns>
        [HttpPut("models/{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> UpdateAIModel(long id, [FromBody] AIModelConfig config)
        {
            try
            {
                config.Id = id;
                // 使用租户隔离，UserContext会自动处理租户信息
                var operatorId = _operatorContextService.GetOperatorId();
                var operatorName = _operatorContextService.GetOperatorDisplayName();
                var currentTime = DateTime.UtcNow;

                config.UserId = operatorId;
                config.UpdatedBy = operatorId;
                config.UpdatedByName = operatorName;
                config.UpdatedTime = currentTime;

                var result = await _configService.UpdateConfigAsync(config);
                return Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update AI model configuration, Config ID: {ConfigId}", id);
                return BadRequest($"Failed to update AI model configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除AI模型配置
        /// </summary>
        /// <param name="id">配置ID</param>
        /// <returns>删除结果</returns>
        [HttpDelete("models/{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> DeleteAIModel(long id)
        {
            try
            {
                // 使用租户隔离，不需要手动获取用户ID
                var result = await _configService.DeleteConfigAsync(id);
                return Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete AI model configuration, Config ID: {ConfigId}", id);
                return BadRequest($"Failed to delete AI model configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置为默认配置
        /// </summary>
        /// <param name="id">配置ID</param>
        /// <returns>设置结果</returns>
        [HttpPut("models/{id}/default")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> SetAsDefault(long id)
        {
            try
            {
                var result = await _configService.SetAsDefaultAsync(id);
                return Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set default AI model configuration, Config ID: {ConfigId}", id);
                return BadRequest($"Failed to set default AI model configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试AI模型配置连接
        /// </summary>
        /// <param name="config">AI模型配置</param>
        /// <returns>测试结果</returns>
        [HttpPost("models/test")]
        [ProducesResponseType<SuccessResponse<AIModelTestResult>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> TestAIModelConnection([FromBody] AIModelConfig config)
        {
            try
            {
                var result = await _configService.TestConnectionAsync(config);

                // 如果有配置ID，获取更新后的配置状态返回给前端
                if (config.Id > 0)
                {
                    var updatedConfig = await _configService.GetConfigByIdAsync(config.Id);
                    if (updatedConfig != null)
                    {
                        _logger.LogInformation("After test, config ID: {ConfigId} isAvailable: {IsAvailable}",
                            config.Id, updatedConfig.IsAvailable);
                    }
                }

                return Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test AI model configuration connection");
                return BadRequest($"Failed to test AI model configuration connection: {ex.Message}");
            }
        }

        /// <summary>
        /// Get supported AI providers list
        /// </summary>
        /// <returns>AI providers list</returns>
        [HttpGet("providers")]
        [ProducesResponseType<SuccessResponse<List<AIProviderInfo>>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public IActionResult GetProviders()
        {
            try
            {
                var providers = new List<AIProviderInfo>
                {
                    new AIProviderInfo
                    {
                        Name = "openai",
                        DisplayName = "OpenAI",
                        Icon = "🚀",
                        Description = "Industry-leading large language models from OpenAI. GPT-4o is the flagship model with 128K context. BaseURL example: https://api.openai.com/v1",
                        Website = "https://openai.com",
                        SupportedModels = new[] { "gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-4", "gpt-3.5-turbo", "o1-preview", "o1-mini" }
                    },

                    new AIProviderInfo
                    {
                        Name = "deepseek",
                        DisplayName = "DeepSeek",
                        Icon = "🔍",
                        Description = "Cost-effective models specialized in code generation and reasoning. DeepSeek-V3 offers GPT-4 level performance at lower cost. BaseURL example: https://api.deepseek.com",
                        Website = "https://deepseek.com",
                        SupportedModels = new[] { "deepseek-chat", "deepseek-reasoner", "deepseek-coder" }
                    },
                    new AIProviderInfo
                    {
                        Name = "item",
                        DisplayName = "Item (OpenAI/Gemini)",
                        Icon = "🏢",
                        Description = "Company internal LLM Gateway service supporting multiple AI providers. Unified access to OpenAI and Gemini models. BaseURL example: https://aiop-gateway.item.com (default). API Key format: gw-wfe-{env}-{id}",
                        Website = "https://aiop-gateway.item.com",
                        SupportedModels = new[] {
                            "openai/gpt-4o",
                            "openai/gpt-4o-mini",
                            "openai/gpt-4-turbo",
                            "openai/gpt-4",
                            "openai/gpt-3.5-turbo",
                            "gemini/gemini-2.5-flash",
                            "gemini/gemini-2.5-pro",
                            "gemini/gemini-2.0-flash-exp",
                            "gemini/gemini-1.5-pro",
                            "gemini/gemini-1.5-flash"
                        }
                    }
                };

                return Success(providers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get AI providers list");
                return BadRequest("Failed to get AI providers list");
            }
        }
    }

    /// <summary>
    /// AI提供商信息
    /// </summary>
    public class AIProviderInfo
    {
        /// <summary>
        /// 提供商名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 图标
        /// </summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 官网地址
        /// </summary>
        public string Website { get; set; } = string.Empty;

        /// <summary>
        /// 支持的模型列表
        /// </summary>
        public string[] SupportedModels { get; set; } = Array.Empty<string>();
    }
}