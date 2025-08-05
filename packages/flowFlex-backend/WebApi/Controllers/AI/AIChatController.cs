using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.IServices;
using Item.Internal.StandardApi.Response;
using System.Net;
using static FlowFlex.Application.Contracts.IServices.IAIService;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlowFlex.WebApi.Controllers.AI
{
    /// <summary>
    /// AI Chat Conversation API
    /// 提供AI对话和实时聊天功能
    /// </summary>
    [ApiController]
    [Route("ai/chat/v{version:apiVersion}")]
    [Display(Name = "AI Chat Service")]
    [Tags("AI", "Chat", "Conversation", "Natural Language")]
    [Authorize]
    public class AIChatController : Controllers.ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly IAIModelConfigService _configService;
        private readonly ILogger<AIChatController> _logger;

        public AIChatController(IAIService aiService, IAIModelConfigService configService, ILogger<AIChatController> logger)
        {
            _aiService = aiService;
            _configService = configService;
            _logger = logger;
        }

        /// <summary>
        /// Send message to AI and get response
        /// </summary>
        /// <param name="input">Chat input with messages and context</param>
        /// <returns>AI chat response</returns>
        [HttpPost("conversation")]
        [ProducesResponseType<SuccessResponse<AIChatResponse>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> SendChatMessage([FromBody] AIChatInput input)
        {
            try
            {
                if (input == null || input.Messages == null || !input.Messages.Any())
                {
                    return BadRequest("Chat messages are required");
                }

                _logger.LogInformation("Processing chat message for session: {SessionId}, ModelId: {ModelId}, Provider: {Provider}, Model: {Model}", 
                    input.SessionId, input.ModelId, input.ModelProvider, input.ModelName);

                var result = await _aiService.SendChatMessageAsync(input);
                
                _logger.LogInformation("Chat response generated for session: {SessionId}", input.SessionId);
                
                return Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message for session: {SessionId}", input.SessionId);
                return BadRequest($"Chat processing failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Stream chat conversation with AI
        /// </summary>
        /// <param name="input">Chat input with messages and context</param>
        /// <returns>Streaming chat response</returns>
        [HttpPost("conversation/stream")]
        [ProducesResponseType(typeof(IAsyncEnumerable<AIChatStreamResult>), 200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async IAsyncEnumerable<AIChatStreamResult> StreamChatMessage([FromBody] AIChatInput input)
        {
            if (input == null || input.Messages == null || !input.Messages.Any())
            {
                yield return new AIChatStreamResult
                {
                    Type = "error",
                    Content = "Chat messages are required",
                    IsComplete = true,
                    SessionId = input?.SessionId ?? ""
                };
                yield break;
            }

            _logger.LogInformation("Starting stream chat for session: {SessionId}", input.SessionId);

            await foreach (var result in _aiService.StreamChatAsync(input))
            {
                yield return result;
            }

            _logger.LogInformation("Stream chat completed for session: {SessionId}", input.SessionId);
        }

        /// <summary>
        /// Get AI chat service status
        /// </summary>
        /// <returns>Chat service status</returns>
        [HttpGet("status")]
        [ProducesResponseType<SuccessResponse<AIChatServiceStatus>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetChatStatus()
        {
            try
            {
                // 获取当前用户的默认AI配置
                var userId = GetCurrentUserId();
                var userConfig = await _configService.GetUserDefaultConfigAsync(userId);

            var status = new AIChatServiceStatus
                {
                    IsAvailable = userConfig?.IsAvailable ?? true,
                    Provider = userConfig?.Provider ?? "ZhipuAI",
                    Model = userConfig?.ModelName ?? "glm-4",
                    Features = new List<string>
                    {
                        "Real-time Conversation",
                        "Workflow Planning Mode",
                        "Context Awareness",
                        "Session Management",
                        "Streaming Support",
                        "Custom AI Model Configuration"
                    },
                    Version = "1.1.0",
                    LastHealthCheck = userConfig?.LastCheckTime ?? DateTime.UtcNow,
                    SupportedModes = new List<string> { "workflow_planning", "general" }
                };

                return Success(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat service status");
                
                // 返回默认状态
                var defaultStatus = new AIChatServiceStatus
            {
                IsAvailable = true,
                Provider = "ZhipuAI",
                Model = "glm-4",
                Features = new List<string>
                {
                    "Real-time Conversation",
                    "Workflow Planning Mode",
                    "Context Awareness",
                    "Session Management",
                    "Streaming Support"
                },
                Version = "1.0.0",
                LastHealthCheck = DateTime.UtcNow,
                SupportedModes = new List<string> { "workflow_planning", "general" }
            };

                return Success(defaultStatus);
            }
        }
    }

    #region Request/Response Models

    /// <summary>
    /// AI chat service status
    /// </summary>
    public class AIChatServiceStatus
    {
        public bool IsAvailable { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public List<string> Features { get; set; } = new();
        public string Version { get; set; } = string.Empty;
        public DateTime LastHealthCheck { get; set; }
        public List<string> SupportedModes { get; set; } = new();
    }

    #endregion
} 