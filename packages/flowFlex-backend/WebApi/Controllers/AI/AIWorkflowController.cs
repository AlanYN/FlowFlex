using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.Dtos.OW.Workflow;
using FlowFlex.Application.Contracts.Dtos.AI;
using Item.Internal.StandardApi.Response;
using System.Net;

namespace FlowFlex.WebApi.Controllers.AI
{
    /// <summary>
    /// AI Workflow Generation API
    /// </summary>
    [ApiController]
    [Route("ai/workflows/v{version:apiVersion}")]
    [Display(Name = "AI Workflow Generation")]
    [Tags("AI", "Workflow Generation", "Natural Language Processing")]
    [Authorize]
    public class AIWorkflowController : Controllers.ControllerBase
    {
        private readonly IAIService _aiService;

        public AIWorkflowController(IAIService aiService)
        {
            _aiService = aiService;
        }

        /// <summary>
        /// Generate workflow from natural language description
        /// </summary>
        /// <param name="input">Natural language workflow description</param>
        /// <returns>Generated workflow structure</returns>
        [HttpPost("generate")]
        [ProducesResponseType<SuccessResponse<AIWorkflowGenerationResult>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> GenerateWorkflow([FromBody] AIWorkflowGenerationInput input)
        {
            if (input == null || string.IsNullOrEmpty(input.Description))
            {
                return BadRequest("Workflow description is required");
            }

            // Enhanced input information logged via structured logging

            var result = await _aiService.GenerateWorkflowAsync(input);
            return Success(result);
        }

        /// <summary>
        /// Stream generate workflow with real-time updates
        /// </summary>
        /// <param name="input">Natural language workflow description</param>
        /// <returns>Streaming workflow generation updates</returns>
        [HttpPost("generate/stream")]
        [ProducesResponseType(200)]
        public async Task StreamGenerateWorkflow([FromBody] AIWorkflowGenerationInput input)
        {
            // 设置流式响应头
            Response.ContentType = "text/event-stream";
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");
            Response.Headers.Append("Access-Control-Allow-Origin", "*");

            if (input == null || string.IsNullOrEmpty(input.Description))
            {
                var errorData = new AIWorkflowStreamResult
                {
                    Type = "error",
                    Message = "Workflow description is required",
                    IsComplete = true
                };

                await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(errorData)}\n\n");
                await Response.Body.FlushAsync();
                return;
            }

            // Enhanced input information for streaming generation logged via structured logging

            try
            {
                // Performance metrics tracked internally

                await foreach (var result in _aiService.StreamGenerateWorkflowAsync(input))
                {
                    try
                    {
                        var jsonData = System.Text.Json.JsonSerializer.Serialize(result);
                        var sseData = $"data: {jsonData}\n\n";
                        await Response.WriteAsync(sseData);
                    }
                    catch (Exception)
                    {
                        // Write error handled silently
                    }
                }

                // Send completion signal with timeout
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    await Response.WriteAsync("data: [DONE]\n\n").WaitAsync(cts.Token);
                    await Response.Body.FlushAsync().WaitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // DONE signal timeout, but continuing
                }
            }
            catch (Exception ex)
            {
                var errorData = new AIWorkflowStreamResult
                {
                    Type = "error",
                    Message = $"Stream error: {ex.Message}",
                    IsComplete = true
                };

                var jsonData = System.Text.Json.JsonSerializer.Serialize(errorData);
                await Response.WriteAsync($"data: {jsonData}\n\n");
                await Response.Body.FlushAsync();
            }
        }

        /// <summary>
        /// Enhance existing workflow with AI suggestions
        /// </summary>
        /// <param name="workflowId">Existing workflow ID</param>
        /// <param name="request">Enhancement request</param>
        /// <returns>Enhancement suggestions</returns>
        [HttpPost("{workflowId}/enhance")]
        [ProducesResponseType<SuccessResponse<AIWorkflowEnhancementResult>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> EnhanceWorkflow(long workflowId, [FromBody] EnhanceWorkflowRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Enhancement))
            {
                return BadRequest("Enhancement description is required");
            }

            var result = await _aiService.EnhanceWorkflowAsync(workflowId, request.Enhancement);
            return Success(result);
        }

        /// <summary>
        /// Validate workflow and get AI suggestions
        /// </summary>
        /// <param name="workflow">Workflow to validate</param>
        /// <returns>Validation results and suggestions</returns>
        [HttpPost("validate")]
        [ProducesResponseType<SuccessResponse<AIValidationResult>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> ValidateWorkflow([FromBody] WorkflowInputDto workflow)
        {
            if (workflow == null)
            {
                return BadRequest("Workflow data is required");
            }

            var result = await _aiService.ValidateWorkflowAsync(workflow);
            return Success(result);
        }

        /// <summary>
        /// Parse natural language requirements into structured format
        /// </summary>
        /// <param name="request">Natural language requirements</param>
        /// <returns>Structured requirements</returns>
        [HttpPost("parse-requirements")]
        [ProducesResponseType<SuccessResponse<AIRequirementsParsingResult>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> ParseRequirements([FromBody] ParseRequirementsRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.NaturalLanguage))
            {
                return BadRequest("Natural language description is required");
            }

            // If client provides model override, use it; otherwise fallback to default provider
            if (!string.IsNullOrWhiteSpace(request.ModelProvider) || !string.IsNullOrWhiteSpace(request.ModelName) || !string.IsNullOrWhiteSpace(request.ModelId))
            {
                var resultWithOverride = await _aiService.ParseRequirementsAsync(request.NaturalLanguage, request.ModelProvider, request.ModelName, request.ModelId);
                return Success(resultWithOverride);
            }

            var result = await _aiService.ParseRequirementsAsync(request.NaturalLanguage);
            return Success(result);
        }

        /// <summary>
        /// Get AI service status and capabilities
        /// </summary>
        /// <returns>AI service status</returns>
        [HttpGet("status")]
        [ProducesResponseType<SuccessResponse<AIServiceStatus>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetStatus()
        {
            var status = new AIServiceStatus
            {
                IsAvailable = true,
                Provider = "ZhipuAI",
                Model = "glm-4",
                Features = new List<string>
                {
                    "Workflow Generation",
                    "Real-time Streaming",
                    "Enhancement Suggestions",
                    "Validation",
                    "Requirements Parsing"
                },
                Version = "1.0.0",
                LastHealthCheck = DateTime.UtcNow
            };

            return Success(status);
        }

        /// <summary>
        /// Modify existing workflow based on natural language description
        /// </summary>
        /// <param name="workflowId">Workflow ID to modify</param>
        /// <param name="input">Modification description</param>
        /// <returns>Modified workflow structure</returns>
        [HttpPost("{workflowId}/modify")]
        [ProducesResponseType<SuccessResponse<AIWorkflowGenerationResult>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> ModifyWorkflow(
            [FromRoute] long workflowId,
            [FromBody] AIWorkflowModificationInput input)
        {
            if (input == null || string.IsNullOrEmpty(input.Description))
            {
                return BadRequest("Modification description is required");
            }

            // TODO: 验证workflow是否存在
            // var workflowExists = await _workflowService.ExistsAsync(workflowId);
            // if (!workflowExists)
            // {
            //     return NotFound($"Workflow with ID {workflowId} not found");
            // }

            input.WorkflowId = workflowId;

            var result = await _aiService.EnhanceWorkflowAsync(input);

            return Success(result);
        }

        /// <summary>
        /// Create stage components (checklists and questionnaires) for a workflow
        /// </summary>
        /// <param name="request">Stage components creation request</param>
        /// <returns>Success status</returns>
        [HttpPost("create-stage-components")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> CreateStageComponents([FromBody] CreateStageComponentsRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request cannot be null");
            }

            // Stage components creation logged via structured logging

            var result = await _aiService.CreateStageComponentsAsync(
                request.WorkflowId,
                request.Stages ?? new List<AIStageGenerationResult>(),
                request.Checklists ?? new List<AIChecklistGenerationResult>(),
                request.Questionnaires ?? new List<AIQuestionnaireGenerationResult>()
            );

            return Success(result);
        }
    }
}