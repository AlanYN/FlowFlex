using FlowFlex.Application.Contracts.IServices;
using Item.Internal.StandardApi.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;

namespace FlowFlex.WebApi.Controllers.AI
{
    /// <summary>
    /// AI services controller
    /// </summary>
    [ApiController]
    [Route("ai/v{version:apiVersion}")]
    [Authorize]
    public class AIController : Controllers.ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly ILogger<AIController> _logger;

        public AIController(
            IAIService aiService,
            ILogger<AIController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        #region Action Analysis and Creation

        /// <summary>
        /// Analyze conversation to extract action insights with streaming response
        /// </summary>
        /// <param name="input">Action analysis input</param>
        /// <returns>Streaming action analysis result</returns>
        /// <remarks>
        /// Stream analyze conversation messages to extract:
        /// - Action items that need to be taken
        /// - Key insights from the conversation
        /// - Suggested next steps
        /// - Identified stakeholders
        /// - Priority assessment
        /// </remarks>
        [HttpPost("actions/analyze/stream")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task StreamAnalyzeActionAsync([FromBody] AIActionAnalysisInput input)
        {
            // 设置流式响应头
            Response.ContentType = "text/event-stream";
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            Response.Headers.Append("Access-Control-Allow-Origin", "*");

            _logger.LogInformation("Stream action analysis request received for SessionId: {SessionId}", input?.SessionId);

            if (input == null || input.ConversationHistory == null || !input.ConversationHistory.Any())
            {
                var errorData = new AIActionStreamResult
                {
                    Type = "error",
                    Content = "Conversation history is required",
                    IsComplete = true,
                    SessionId = input?.SessionId ?? ""
                };

                await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(errorData)}\n\n");
                await Response.Body.FlushAsync();
                return;
            }

            _logger.LogInformation("Starting stream action analysis for SessionId: {SessionId}", input.SessionId);

            try
            {
                await foreach (var result in _aiService.StreamAnalyzeActionAsync(input))
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
                _logger.LogError(ex, "Error in stream action analysis for SessionId: {SessionId}", input.SessionId);

                var errorData = new AIActionStreamResult
                {
                    Type = "error",
                    Content = $"Stream analysis error: {ex.Message}",
                    IsComplete = true,
                    SessionId = input.SessionId
                };

                var jsonData = System.Text.Json.JsonSerializer.Serialize(errorData);
                await Response.WriteAsync($"data: {jsonData}\n\n");
                await Response.Body.FlushAsync();
            }

            _logger.LogInformation("Stream action analysis completed for SessionId: {SessionId}", input.SessionId);
        }

        /// <summary>
        /// Create action plan with streaming response
        /// </summary>
        /// <param name="input">Action creation input</param>
        /// <returns>Streaming action creation result</returns>
        /// <remarks>
        /// Stream create comprehensive action plan with:
        /// - Detailed action steps
        /// - Timeline and priorities
        /// - Resource requirements
        /// - Success metrics
        /// - Risk assessment
        /// </remarks>
        [HttpPost("actions/create/stream")]
        [ProducesResponseType(200)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task StreamCreateActionAsync([FromBody] AIActionCreationInput input)
        {
            // 设置流式响应头
            Response.ContentType = "text/event-stream";
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            Response.Headers.Append("Access-Control-Allow-Origin", "*");

            _logger.LogInformation("Stream action creation request received");

            if (input == null || (string.IsNullOrEmpty(input.ActionDescription) && input.AnalysisResult == null))
            {
                var errorData = new AIActionStreamResult
                {
                    Type = "error",
                    Content = "Action description or analysis result is required",
                    IsComplete = true,
                    SessionId = ""
                };

                await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(errorData)}\n\n");
                await Response.Body.FlushAsync();
                return;
            }

            _logger.LogInformation("Starting stream action creation");

            try
            {
                await foreach (var result in _aiService.StreamCreateActionAsync(input))
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
                _logger.LogError(ex, "Error in stream action creation");

                var errorData = new AIActionStreamResult
                {
                    Type = "error",
                    Content = $"Stream creation error: {ex.Message}",
                    IsComplete = true,
                    SessionId = ""
                };

                var jsonData = System.Text.Json.JsonSerializer.Serialize(errorData);
                await Response.WriteAsync($"data: {jsonData}\n\n");
                await Response.Body.FlushAsync();
            }

            _logger.LogInformation("Stream action creation completed");
        }

        /// <summary>
        /// Analyze conversation to extract action insights
        /// </summary>
        /// <param name="input">Action analysis input</param>
        /// <returns>Action analysis result</returns>
        /// <remarks>
        /// Analyze conversation messages to extract:
        /// - Action items that need to be taken
        /// - Key insights from the conversation
        /// - Suggested next steps
        /// - Identified stakeholders
        /// - Priority assessment
        /// </remarks>
        [HttpPost("actions/analyze")]
        [ProducesResponseType<SuccessResponse<AIActionAnalysisResult>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> AnalyzeActionAsync([FromBody] AIActionAnalysisInput input)
        {
            try
            {
                _logger.LogInformation("Analyzing conversation for action insights, SessionId: {SessionId}", input.SessionId);

                var result = await _aiService.AnalyzeActionAsync(input);

                if (result.Success)
                {
                    _logger.LogInformation("Action analysis completed successfully for SessionId: {SessionId}", input.SessionId);
                    return Success(result);
                }
                else
                {
                    _logger.LogWarning("Action analysis failed for SessionId: {SessionId}, Message: {Message}", 
                        input.SessionId, result.Message);
                    return BadRequest(result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing conversation for actions, SessionId: {SessionId}", input.SessionId);
                return BadRequest($"Failed to analyze conversation: {ex.Message}");
            }
        }

        /// <summary>
        /// Create action plan based on analysis or description
        /// </summary>
        /// <param name="input">Action creation input</param>
        /// <returns>Action creation result</returns>
        /// <remarks>
        /// Create a comprehensive action plan that includes:
        /// - Detailed action plan with objectives
        /// - Individual action items with priorities
        /// - Implementation steps
        /// - Risk assessment
        /// - Success metrics
        /// </remarks>
        [HttpPost("actions/create")]
        [ProducesResponseType<SuccessResponse<AIActionCreationResult>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateActionAsync([FromBody] AIActionCreationInput input)
        {
            try
            {
                _logger.LogInformation("Creating action plan based on input");

                var result = await _aiService.CreateActionAsync(input);

                if (result.Success)
                {
                    _logger.LogInformation("Action plan created successfully");
                    return Success(result);
                }
                else
                {
                    _logger.LogWarning("Action creation failed, Message: {Message}", result.Message);
                    return BadRequest(result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating action plan");
                return BadRequest($"Failed to create action plan: {ex.Message}");
            }
        }

        /// <summary>
        /// Analyze conversation and create action plan in one step
        /// </summary>
        /// <param name="input">Action analysis input</param>
        /// <returns>Action creation result</returns>
        /// <remarks>
        /// This endpoint combines analysis and creation:
        /// 1. Analyzes the conversation to extract insights
        /// 2. Creates a comprehensive action plan based on the analysis
        /// 
        /// This is a convenience endpoint for workflows that need both operations.
        /// </remarks>
        [HttpPost("actions/analyze-and-create")]
        [ProducesResponseType<SuccessResponse<AIActionCreationResult>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> AnalyzeAndCreateActionAsync([FromBody] AIActionAnalysisInput input)
        {
            try
            {
                _logger.LogInformation("Analyzing conversation and creating action plan, SessionId: {SessionId}", input.SessionId);

                // Step 1: Analyze the conversation
                var analysisResult = await _aiService.AnalyzeActionAsync(input);

                if (!analysisResult.Success)
                {
                    _logger.LogWarning("Action analysis failed for SessionId: {SessionId}, Message: {Message}", 
                        input.SessionId, analysisResult.Message);
                    return BadRequest($"Analysis failed: {analysisResult.Message}");
                }

                // Step 2: Create action plan based on analysis
                var creationInput = new AIActionCreationInput
                {
                    AnalysisResult = analysisResult,
                    Context = input.Context,
                    Stakeholders = analysisResult.Stakeholders,
                    Priority = analysisResult.Priority,
                    ModelId = input.ModelId,
                    ModelProvider = input.ModelProvider,
                    ModelName = input.ModelName
                };

                var creationResult = await _aiService.CreateActionAsync(creationInput);

                if (creationResult.Success)
                {
                    _logger.LogInformation("Action analysis and creation completed successfully for SessionId: {SessionId}", input.SessionId);
                    return Success(creationResult);
                }
                else
                {
                    _logger.LogWarning("Action creation failed for SessionId: {SessionId}, Message: {Message}", 
                        input.SessionId, creationResult.Message);
                    return BadRequest($"Creation failed: {creationResult.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in analyze and create action workflow, SessionId: {SessionId}", input.SessionId);
                return BadRequest($"Failed to analyze and create action: {ex.Message}");
            }
        }

        #endregion
    }
}
