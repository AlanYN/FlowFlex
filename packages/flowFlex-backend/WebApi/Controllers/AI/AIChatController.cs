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
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task StreamChatMessage([FromBody] AIChatInput input)
        {
            // 设置流式响应头
            Response.ContentType = "text/event-stream";
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            Response.Headers.Append("Access-Control-Allow-Origin", "*");

            _logger.LogInformation("Stream chat request received. Input is null: {InputNull}", input == null);

            if (input != null)
            {
                _logger.LogInformation("Input details - Messages count: {MessageCount}, SessionId: {SessionId}, Mode: {Mode}",
                    input.Messages?.Count ?? 0, input.SessionId, input.Mode);
            }

            if (input == null || input.Messages == null || !input.Messages.Any())
            {
                _logger.LogWarning("Invalid stream chat input - Input null: {InputNull}, Messages null: {MessagesNull}, Messages empty: {MessagesEmpty}",
                    input == null, input?.Messages == null, input?.Messages?.Any() == false);

                var errorData = new AIChatStreamResult
                {
                    Type = "error",
                    Content = "Chat messages are required",
                    IsComplete = true,
                    SessionId = input?.SessionId ?? ""
                };

                await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(errorData)}\n\n");
                await Response.Body.FlushAsync();
                return;
            }

            _logger.LogInformation("Starting stream chat for session: {SessionId}", input.SessionId);

            try
            {
                await foreach (var result in _aiService.StreamChatAsync(input))
                {
                    var jsonData = System.Text.Json.JsonSerializer.Serialize(result);
                    await Response.WriteAsync($"data: {jsonData}\n\n");
                    await Response.Body.FlushAsync();
                }

                // Send completion signal
                await Response.WriteAsync("data: [DONE]\n\n");
                await Response.Body.FlushAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in stream chat for session: {SessionId}", input.SessionId);

                var errorData = new AIChatStreamResult
                {
                    Type = "error",
                    Content = $"Stream error: {ex.Message}",
                    IsComplete = true,
                    SessionId = input.SessionId
                };

                var jsonData = System.Text.Json.JsonSerializer.Serialize(errorData);
                await Response.WriteAsync($"data: {jsonData}\n\n");
                await Response.Body.FlushAsync();
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