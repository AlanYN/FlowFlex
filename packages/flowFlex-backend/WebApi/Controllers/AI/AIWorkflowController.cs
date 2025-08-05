using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.Dtos.OW.Workflow;
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

            // Log the enhanced input information for debugging
            Console.WriteLine($"[AI Workflow] Generating workflow with enhanced context:");
            Console.WriteLine($"[AI Workflow] Description length: {input.Description?.Length ?? 0} characters");
            Console.WriteLine($"[AI Workflow] Context: {input.Context}");
            Console.WriteLine($"[AI Workflow] Selected AI Model: {input.ModelProvider} {input.ModelName} (ID: {input.ModelId})");
            Console.WriteLine($"[AI Workflow] Session ID: {input.SessionId}");
            Console.WriteLine($"[AI Workflow] Conversation History: {input.ConversationHistory?.Count ?? 0} messages");
            
            if (input.ConversationMetadata != null)
            {
                Console.WriteLine($"[AI Workflow] Conversation Metadata:");
                Console.WriteLine($"  - Total Messages: {input.ConversationMetadata.TotalMessages}");
                Console.WriteLine($"  - Mode: {input.ConversationMetadata.ConversationMode}");
                Console.WriteLine($"  - Start Time: {input.ConversationMetadata.ConversationStartTime}");
                Console.WriteLine($"  - End Time: {input.ConversationMetadata.ConversationEndTime}");
            }

            var result = await _aiService.GenerateWorkflowAsync(input);
            return Success(result);
        }

        /// <summary>
        /// Stream generate workflow with real-time updates
        /// </summary>
        /// <param name="input">Natural language workflow description</param>
        /// <returns>Streaming workflow generation updates</returns>
        [HttpPost("generate/stream")]
        [ProducesResponseType(typeof(IAsyncEnumerable<AIWorkflowStreamResult>), 200)]
        public async IAsyncEnumerable<AIWorkflowStreamResult> StreamGenerateWorkflow([FromBody] AIWorkflowGenerationInput input)
        {
            if (input == null || string.IsNullOrEmpty(input.Description))
            {
                yield return new AIWorkflowStreamResult
                {
                    Type = "error",
                    Message = "Workflow description is required",
                    IsComplete = true
                };
                yield break;
            }

            // Log the enhanced input information for streaming generation
            Console.WriteLine($"[AI Workflow Stream] Starting stream generation with enhanced context:");
            Console.WriteLine($"[AI Workflow Stream] Selected AI Model: {input.ModelProvider} {input.ModelName} (ID: {input.ModelId})");
            Console.WriteLine($"[AI Workflow Stream] Session ID: {input.SessionId}");
            Console.WriteLine($"[AI Workflow Stream] Conversation History: {input.ConversationHistory?.Count ?? 0} messages");

            await foreach (var result in _aiService.StreamGenerateWorkflowAsync(input))
            {
                yield return result;
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
            Console.WriteLine($"[DEBUG] ModifyWorkflow called with workflowId: {workflowId}");
            Console.WriteLine($"[DEBUG] Input: {System.Text.Json.JsonSerializer.Serialize(input)}");
            
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
            Console.WriteLine($"[DEBUG] Final input WorkflowId: {input.WorkflowId}");
            
            var result = await _aiService.EnhanceWorkflowAsync(input);
            
            Console.WriteLine($"[DEBUG] AI Service result: Success={result.Success}, WorkflowName={result.GeneratedWorkflow?.Name}");
            
            return Success(result);
        }
    }

    #region Request/Response Models

    public class EnhanceWorkflowRequest
    {
        /// <summary>
        /// Enhancement description
        /// </summary>
        [Required]
        public string Enhancement { get; set; } = string.Empty;

        /// <summary>
        /// Additional context
        /// </summary>
        public string Context { get; set; } = string.Empty;
    }

    public class ParseRequirementsRequest
    {
        /// <summary>
        /// Natural language requirements description
        /// </summary>
        [Required]
        public string NaturalLanguage { get; set; } = string.Empty;

        /// <summary>
        /// Context information
        /// </summary>
        public string Context { get; set; } = string.Empty;
    }

    public class AIServiceStatus
    {
        public bool IsAvailable { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public List<string> Features { get; set; } = new();
        public string Version { get; set; } = string.Empty;
        public DateTime LastHealthCheck { get; set; }
    }

    #endregion
} 