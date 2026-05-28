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
    /// AI Model Configuration API - Provides AI model configuration management
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
        /// Get all AI model configurations for the current tenant
        /// </summary>
        /// <returns>List of AI model configurations</returns>
        [HttpGet("models")]
        [ProducesResponseType<SuccessResponse<List<AIModelConfig>>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> GetUserAIModels()
        {
            try
            {
                // Tenant-isolated query, only returns configurations for the current tenant
                // Service layer automatically gets tenant info from UserContext
                var userId = GetCurrentUserId(); // Kept for compatibility, service layer uses tenant isolation
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
        /// Get the default AI model configuration for the current tenant
        /// </summary>
        /// <returns>Default AI model configuration</returns>
        [HttpGet("models/default")]
        [ProducesResponseType<SuccessResponse<AIModelConfig>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> GetDefaultAIModel()
        {
            try
            {
                // Tenant-isolated query, only returns the default config for the current tenant
                // Service layer automatically gets tenant info from UserContext
                var userId = GetCurrentUserId(); // Kept for compatibility, service layer uses tenant isolation
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
        /// Create AI model configuration
        /// </summary>
        /// <param name="config">AI model configuration</param>
        /// <returns>Created configuration ID</returns>
        [HttpPost("models")]
        [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> CreateAIModel([FromBody] AIModelConfig config)
        {
            try
            {
                // Tenant-isolated, UserContext handles tenant info automatically
                // Service layer gets TenantId and AppCode from UserContext automatically
                var operatorId = _operatorContextService.GetOperatorId();
                var operatorName = _operatorContextService.GetOperatorDisplayName();
                var currentTime = DateTime.UtcNow;

                config.UserId = operatorId; // Kept for audit trail
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
        /// Update AI model configuration
        /// </summary>
        /// <param name="id">Configuration ID</param>
        /// <param name="config">AI model configuration</param>
        /// <returns>Whether update was successful</returns>
        [HttpPut("models/{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> UpdateAIModel(long id, [FromBody] AIModelConfig config)
        {
            try
            {
                config.Id = id;
                // Tenant-isolated, UserContext handles tenant info automatically
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
        /// Delete AI model configuration
        /// </summary>
        /// <param name="id">Configuration ID</param>
        /// <returns>Whether deletion was successful</returns>
        [HttpDelete("models/{id}")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> DeleteAIModel(long id)
        {
            try
            {
                // Tenant-isolated, no need to manually get user ID
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
        /// Set configuration as default
        /// </summary>
        /// <param name="id">Configuration ID</param>
        /// <returns>Whether setting was successful</returns>
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
        /// Test AI model configuration connection
        /// </summary>
        /// <param name="config">AI model configuration to test</param>
        /// <returns>Connection test result</returns>
        [HttpPost("models/test")]
        [ProducesResponseType<SuccessResponse<AIModelTestResult>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> TestAIModelConnection([FromBody] AIModelConfig config)
        {
            try
            {
                var result = await _configService.TestConnectionAsync(config);

                // If config has an ID, get the updated config status to return to frontend
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
                        Description = "Industry-leading large language models from OpenAI. GPT-4o is the flagship model with 128K context. Default uses Item Gateway (prefix required: openai/model-name). For official API, use model name directly.",
                        Website = "https://openai.com",
                        DefaultBaseUrl = "https://aiop-gateway.item.com",
                        SupportedModels = new[] {
                            "gpt-4o",
                            "gpt-4o-mini",
                            "gpt-4-turbo",
                            "gpt-4",
                            "gpt-3.5-turbo",
                            "o1-preview",
                            "o1-mini"
                        }
                    },

                    new AIProviderInfo
                    {
                        Name = "gemini",
                        DisplayName = "Gemini",
                        Icon = "💎",
                        Description = "Google's advanced multimodal AI models with strong reasoning capabilities. Default uses Item Gateway (prefix required: gemini/model-name).",
                        Website = "https://ai.google.dev",
                        DefaultBaseUrl = "https://aiop-gateway.item.com",
                        SupportedModels = new[] {
                            "gemini-2.5-flash",
                            "gemini-2.5-pro",
                            "gemini-2.0-flash-exp",
                            "gemini-1.5-pro",
                            "gemini-1.5-flash"
                        }
                    },

                    new AIProviderInfo
                    {
                        Name = "deepseek",
                        DisplayName = "DeepSeek",
                        Icon = "🔍",
                        Description = "Cost-effective models specialized in code generation and reasoning. DeepSeek-V3 offers GPT-4 level performance at lower cost.",
                        Website = "https://deepseek.com",
                        DefaultBaseUrl = "https://api.deepseek.com",
                        SupportedModels = new[] { "deepseek-chat", "deepseek-reasoner", "deepseek-coder" }
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
    /// AI Provider information
    /// </summary>
    public class AIProviderInfo
    {
        /// <summary>
        /// Provider name (identifier)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Display name
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Icon emoji
        /// </summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// Provider description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Official website URL
        /// </summary>
        public string Website { get; set; } = string.Empty;

        /// <summary>
        /// Default base URL for API calls
        /// </summary>
        public string DefaultBaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// List of supported model names
        /// </summary>
        public string[] SupportedModels { get; set; } = Array.Empty<string>();
    }
}