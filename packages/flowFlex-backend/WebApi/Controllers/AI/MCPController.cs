using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.IServices;
using Item.Internal.StandardApi.Response;
using System.Net;

namespace FlowFlex.WebApi.Controllers.AI
{
    /// <summary>
    /// MCP (Memory, Context, Processing) Service API
    /// 为AI提供上下文记忆和知识图谱功能
    /// </summary>
    [ApiController]
    [Route("mcp/v{version:apiVersion}")]
    [Display(Name = "MCP Service")]
    [Tags("MCP", "Memory", "Context", "Knowledge Graph")]
    [Authorize]
    public class MCPController : Controllers.ControllerBase
    {
        private readonly IMCPService _mcpService;
        private readonly IAIService _aiService;

        public MCPController(IMCPService mcpService, IAIService aiService)
        {
            _mcpService = mcpService;
            _aiService = aiService;
        }

        #region Context Management

        /// <summary>
        /// Store context information
        /// </summary>
        /// <param name="request">Context storage request</param>
        /// <returns>Success status</returns>
        [HttpPost("contexts")]
        [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> StoreContext([FromBody] StoreContextRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.ContextId) || string.IsNullOrEmpty(request.Content))
            {
                return BadRequest("ContextId and Content are required");
            }

            await _mcpService.StoreContextAsync(request.ContextId, request.Content, request.Metadata);
            return Success(true);
        }

        /// <summary>
        /// Retrieve context by ID
        /// </summary>
        /// <param name="contextId">Context identifier</param>
        /// <returns>Context information</returns>
        [HttpGet("contexts/{contextId}")]
        [ProducesResponseType<SuccessResponse<MCPContextResult>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 404)]
        public async Task<IActionResult> GetContext(string contextId)
        {
            var context = await _mcpService.GetContextAsync(contextId);
            if (context == null)
            {
                return NotFound($"Context not found: {contextId}");
            }

            return Success(context);
        }

        /// <summary>
        /// Search contexts by query
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="limit">Maximum results (default: 10)</param>
        /// <returns>Matching contexts</returns>
        [HttpGet("contexts/search")]
        [ProducesResponseType<SuccessResponse<List<MCPContextResult>>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SearchContexts([FromQuery] string query, [FromQuery] int limit = 10)
        {
            if (string.IsNullOrEmpty(query))
            {
                return BadRequest("Search query is required");
            }

            var results = await _mcpService.SearchContextsAsync(query, limit);
            return Success(results);
        }

        #endregion

        #region Knowledge Graph Management

        /// <summary>
        /// Create entity in knowledge graph
        /// </summary>
        /// <param name="entity">Entity data</param>
        /// <returns>Success status</returns>
        [HttpPost("entities")]
        [ProducesResponseType<SuccessResponse<string>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> CreateEntity([FromBody] MCPEntity entity)
        {
            if (entity == null || string.IsNullOrEmpty(entity.Name))
            {
                return BadRequest("Entity name is required");
            }

            await _mcpService.CreateEntityAsync(entity);
            return Success(entity.Id);
        }

        /// <summary>
        /// Create relationship between entities
        /// </summary>
        /// <param name="relationship">Relationship data</param>
        /// <returns>Success status</returns>
        [HttpPost("relationships")]
        [ProducesResponseType<SuccessResponse<string>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> CreateRelationship([FromBody] MCPRelationship relationship)
        {
            if (relationship == null || string.IsNullOrEmpty(relationship.FromEntityId) || string.IsNullOrEmpty(relationship.ToEntityId))
            {
                return BadRequest("FromEntityId and ToEntityId are required");
            }

            await _mcpService.CreateRelationshipAsync(relationship);
            return Success(relationship.Id);
        }

        /// <summary>
        /// Query knowledge graph
        /// </summary>
        /// <param name="query">Graph query</param>
        /// <returns>Query results</returns>
        [HttpPost("graph/query")]
        [ProducesResponseType<SuccessResponse<MCPGraphQueryResult>>((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), 400)]
        public async Task<IActionResult> QueryGraph([FromBody] GraphQueryRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Query))
            {
                return BadRequest("Query is required");
            }

            var result = await _mcpService.QueryGraphAsync(request.Query);
            return Success(result);
        }

        #endregion

        #region MCP Tools for AI

        /// <summary>
        /// AI工具：智能工作流生成（带上下文记忆）
        /// </summary>
        /// <param name="request">工作流生成请求</param>
        /// <returns>生成结果</returns>
        [HttpPost("tools/generate-workflow")]
        [ProducesResponseType<SuccessResponse<AIWorkflowGenerationResult>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> MCPGenerateWorkflow([FromBody] MCPWorkflowGenerationRequest request)
        {
            // Store the generation request in context for future reference
            var contextId = $"workflow_gen_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
            await _mcpService.StoreContextAsync(contextId, request.Description, new Dictionary<string, object>
            {
                { "type", "workflow_generation" },
                { "user_id", request.UserId ?? "unknown" },
                { "session_id", request.SessionId ?? "unknown" },
                { "timestamp", DateTime.UtcNow }
            });

            // Search for similar previous requests to provide context
            var similarContexts = await _mcpService.SearchContextsAsync(request.Description, 5);
            var contextInfo = string.Join("\n", similarContexts.Select(c => $"Previous: {c.Content}"));

            var aiInput = new AIWorkflowGenerationInput
            {
                Description = request.Description,
                Context = $"{request.Context}\n\nSimilar previous requests:\n{contextInfo}",
                Requirements = request.Requirements,
                Industry = request.Industry,
                ProcessType = request.ProcessType,
                IncludeApprovals = request.IncludeApprovals,
                IncludeNotifications = request.IncludeNotifications
            };

            var result = await _aiService.GenerateWorkflowAsync(aiInput);

            // Store the result for future reference
            if (result.Success)
            {
                await _mcpService.StoreContextAsync($"{contextId}_result", 
                    System.Text.Json.JsonSerializer.Serialize(result), 
                    new Dictionary<string, object>
                    {
                        { "type", "workflow_generation_result" },
                        { "success", true },
                        { "confidence_score", result.ConfidenceScore }
                    });
            }

            return Success(result);
        }

        /// <summary>
        /// AI工具：智能问卷生成（带上下文记忆）
        /// </summary>
        /// <param name="request">问卷生成请求</param>
        /// <returns>生成结果</returns>
        [HttpPost("tools/generate-questionnaire")]
        [ProducesResponseType<SuccessResponse<AIQuestionnaireGenerationResult>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> MCPGenerateQuestionnaire([FromBody] MCPQuestionnaireGenerationRequest request)
        {
            var contextId = $"questionnaire_gen_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
            await _mcpService.StoreContextAsync(contextId, request.Purpose, new Dictionary<string, object>
            {
                { "type", "questionnaire_generation" },
                { "target_audience", request.TargetAudience },
                { "complexity", request.Complexity }
            });

            var aiInput = new AIQuestionnaireGenerationInput
            {
                Purpose = request.Purpose,
                TargetAudience = request.TargetAudience,
                Topics = request.Topics,
                Context = request.Context,
                EstimatedQuestions = request.EstimatedQuestions,
                IncludeValidation = request.IncludeValidation,
                Complexity = request.Complexity
            };

            var result = await _aiService.GenerateQuestionnaireAsync(aiInput);
            return Success(result);
        }

        /// <summary>
        /// AI工具：智能检查清单生成（带上下文记忆）
        /// </summary>
        /// <param name="request">检查清单生成请求</param>
        /// <returns>生成结果</returns>
        [HttpPost("tools/generate-checklist")]
        [ProducesResponseType<SuccessResponse<AIChecklistGenerationResult>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> MCPGenerateChecklist([FromBody] MCPChecklistGenerationRequest request)
        {
            var contextId = $"checklist_gen_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}";
            await _mcpService.StoreContextAsync(contextId, request.ProcessName, new Dictionary<string, object>
            {
                { "type", "checklist_generation" },
                { "team", request.Team },
                { "description", request.Description }
            });

            var aiInput = new AIChecklistGenerationInput
            {
                ProcessName = request.ProcessName,
                Description = request.Description,
                Team = request.Team,
                RequiredSteps = request.RequiredSteps,
                Context = request.Context,
                IncludeDependencies = request.IncludeDependencies,
                IncludeEstimates = request.IncludeEstimates
            };

            var result = await _aiService.GenerateChecklistAsync(aiInput);
            return Success(result);
        }

        /// <summary>
        /// 获取MCP服务状态
        /// </summary>
        /// <returns>服务状态</returns>
        [HttpGet("status")]
        [ProducesResponseType<SuccessResponse<MCPServiceStatus>>((int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetMCPStatus()
        {
            var status = new MCPServiceStatus
            {
                IsAvailable = true,
                Version = "1.0.0",
                Features = new List<string>
                {
                    "Context Storage",
                    "Context Search",
                    "Knowledge Graph",
                    "AI Tool Integration",
                    "Memory Persistence"
                },
                MemoryProvider = "InMemory",
                EnablePersistence = true,
                LastHealthCheck = DateTime.UtcNow
            };

            return Success(status);
        }

        #endregion
    }

    #region Request/Response Models

    public class StoreContextRequest
    {
        [Required]
        public string ContextId { get; set; } = string.Empty;
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class GraphQueryRequest
    {
        [Required]
        public string Query { get; set; } = string.Empty;
    }

    public class MCPWorkflowGenerationRequest
    {
        [Required]
        public string Description { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public List<string> Requirements { get; set; } = new();
        public string Industry { get; set; } = string.Empty;
        public string ProcessType { get; set; } = string.Empty;
        public bool IncludeApprovals { get; set; } = true;
        public bool IncludeNotifications { get; set; } = true;
        public string UserId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
    }

    public class MCPQuestionnaireGenerationRequest
    {
        [Required]
        public string Purpose { get; set; } = string.Empty;
        public string TargetAudience { get; set; } = string.Empty;
        public List<string> Topics { get; set; } = new();
        public string Context { get; set; } = string.Empty;
        public int EstimatedQuestions { get; set; } = 10;
        public bool IncludeValidation { get; set; } = true;
        public string Complexity { get; set; } = "Medium";
    }

    public class MCPChecklistGenerationRequest
    {
        [Required]
        public string ProcessName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Team { get; set; } = string.Empty;
        public List<string> RequiredSteps { get; set; } = new();
        public string Context { get; set; } = string.Empty;
        public bool IncludeDependencies { get; set; } = true;
        public bool IncludeEstimates { get; set; } = true;
    }

    public class MCPServiceStatus
    {
        public bool IsAvailable { get; set; }
        public string Version { get; set; } = string.Empty;
        public List<string> Features { get; set; } = new();
        public string MemoryProvider { get; set; } = string.Empty;
        public bool EnablePersistence { get; set; }
        public DateTime LastHealthCheck { get; set; }
    }

    #endregion
} 