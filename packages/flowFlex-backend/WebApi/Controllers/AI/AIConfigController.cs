using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.IServices;
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

        public AIConfigController(IAIModelConfigService configService, ILogger<AIConfigController> logger)
        {
            _configService = configService;
            _logger = logger;
        }

        /// <summary>
        /// 获取当前用户的所有AI模型配置
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
                var userId = GetCurrentUserId();
                var configs = await _configService.GetUserAIModelConfigsAsync(userId);
                return Success(configs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user AI model configurations");
                return BadRequest("Failed to get AI model configurations");
            }
        }

        /// <summary>
        /// 获取当前用户的默认AI模型配置
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
                var userId = GetCurrentUserId();
                var config = await _configService.GetUserDefaultConfigAsync(userId);
                if (config == null)
                {
                    return BadRequest("Default AI model configuration not found");
                }
                return Success(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user default AI model configuration");
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
                // 使用租户隔离，UserContext会自动处理用户ID和租户信息
                config.UserId = GetCurrentUserId();
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
                config.UserId = GetCurrentUserId();
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
                return Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test AI model configuration connection");
                return BadRequest($"Failed to test AI model configuration connection: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取支持的AI提供商列表
        /// </summary>
        /// <returns>AI提供商列表</returns>
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
                        Name = "zhipuai",
                        DisplayName = "智谱AI",
                        Icon = "🤖",
                        Description = "智谱AI GLM系列大模型，支持文本生成、对话问答等功能。BaseURL示例：https://open.bigmodel.cn/api/paas/v4",
                        Website = "https://zhipuai.cn",
                        SupportedModels = new[] { "glm-4", "glm-4v", "glm-3-turbo" }
                    },
                    new AIProviderInfo
                    {
                        Name = "openai",
                        DisplayName = "OpenAI",
                        Icon = "🚀",
                        Description = "OpenAI GPT系列模型，业界领先的大语言模型。BaseURL示例：https://api.openai.com",
                        Website = "https://openai.com",
                        SupportedModels = new[] { "gpt-4", "gpt-4-turbo", "gpt-3.5-turbo", "gpt-4o", "gpt-4o-mini" }
                    },
                    new AIProviderInfo
                    {
                        Name = "claude",
                        DisplayName = "Claude",
                        Icon = "🎭",
                        Description = "Anthropic Claude系列模型，注重安全和有用性。BaseURL示例：https://api.anthropic.com",
                        Website = "https://claude.ai",
                        SupportedModels = new[] { "claude-3-opus-20240229", "claude-3-sonnet-20240229", "claude-3-haiku-20240307" }
                    },
                    new AIProviderInfo
                    {
                        Name = "deepseek",
                        DisplayName = "DeepSeek",
                        Icon = "🔍",
                        Description = "DeepSeek系列模型，专注于代码生成和数学推理。BaseURL示例：https://api.deepseek.com",
                        Website = "https://deepseek.com",
                        SupportedModels = new[] { "deepseek-chat", "deepseek-coder", "deepseek-math" }
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