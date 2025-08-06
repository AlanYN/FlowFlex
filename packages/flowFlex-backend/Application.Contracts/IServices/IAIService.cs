using FlowFlex.Domain.Shared;
using FlowFlex.Application.Contracts.Dtos.OW.Workflow;
using FlowFlex.Application.Contracts.Dtos.OW.Questionnaire;
using FlowFlex.Application.Contracts.Dtos.OW.Checklist;

namespace FlowFlex.Application.Contracts.IServices
{
    /// <summary>
    /// AI service interface for natural language processing and generation
    /// </summary>
    public interface IAIService : IScopedService
    {
        /// <summary>
        /// Generate workflow from natural language description
        /// </summary>
        /// <param name="input">Natural language input</param>
        /// <returns>Generated workflow structure</returns>
        Task<AIWorkflowGenerationResult> GenerateWorkflowAsync(AIWorkflowGenerationInput input);

        /// <summary>
        /// Generate questionnaire from natural language description
        /// </summary>
        /// <param name="input">Natural language input</param>
        /// <returns>Generated questionnaire structure</returns>
        Task<AIQuestionnaireGenerationResult> GenerateQuestionnaireAsync(AIQuestionnaireGenerationInput input);

        /// <summary>
        /// Generate checklist from natural language description
        /// </summary>
        /// <param name="input">Natural language input</param>
        /// <returns>Generated checklist structure</returns>
        Task<AIChecklistGenerationResult> GenerateChecklistAsync(AIChecklistGenerationInput input);

        /// <summary>
        /// Stream generate workflow with real-time updates
        /// </summary>
        /// <param name="input">Natural language input</param>
        /// <returns>Streaming workflow generation</returns>
        IAsyncEnumerable<AIWorkflowStreamResult> StreamGenerateWorkflowAsync(AIWorkflowGenerationInput input);

        /// <summary>
        /// Stream generate questionnaire with real-time updates
        /// </summary>
        /// <param name="input">Natural language input</param>
        /// <returns>Streaming questionnaire generation</returns>
        IAsyncEnumerable<AIQuestionnaireStreamResult> StreamGenerateQuestionnaireAsync(AIQuestionnaireGenerationInput input);

        /// <summary>
        /// Stream generate checklist with real-time updates
        /// </summary>
        /// <param name="input">Natural language input</param>
        /// <returns>Streaming checklist generation</returns>
        IAsyncEnumerable<AIChecklistStreamResult> StreamGenerateChecklistAsync(AIChecklistGenerationInput input);

        /// <summary>
        /// Enhance existing workflow with AI suggestions
        /// </summary>
        /// <param name="workflowId">Existing workflow ID</param>
        /// <param name="enhancement">Enhancement request</param>
        /// <returns>Enhanced workflow suggestions</returns>
        Task<AIWorkflowEnhancementResult> EnhanceWorkflowAsync(long workflowId, string enhancement);

        /// <summary>
        /// Validate and suggest improvements for workflow
        /// </summary>
        /// <param name="workflow">Workflow to validate</param>
        /// <returns>Validation results and suggestions</returns>
        Task<AIValidationResult> ValidateWorkflowAsync(WorkflowInputDto workflow);

        /// <summary>
        /// Parse natural language into structured requirements
        /// </summary>
        /// <param name="naturalLanguage">Natural language input</param>
        /// <returns>Structured requirements</returns>
        Task<AIRequirementsParsingResult> ParseRequirementsAsync(string naturalLanguage);

        /// <summary>
        /// Enhance existing workflow using modification input
        /// </summary>
        /// <param name="input">Modification input</param>
        /// <returns>Enhanced workflow result</returns>
        Task<AIWorkflowGenerationResult> EnhanceWorkflowAsync(AIWorkflowModificationInput input);

        /// <summary>
        /// Send message to AI chat and get response
        /// </summary>
        /// <param name="input">Chat input with messages and context</param>
        /// <returns>AI chat response</returns>
        Task<AIChatResponse> SendChatMessageAsync(AIChatInput input);

        /// <summary>
        /// Stream chat conversation with AI
        /// </summary>
        /// <param name="input">Chat input with messages and context</param>
        /// <returns>Streaming chat response</returns>
        IAsyncEnumerable<AIChatStreamResult> StreamChatAsync(AIChatInput input);
    }

    /// <summary>
    /// MCP (Memory, Context, Processing) service interface
    /// </summary>
    public interface IMCPService : IScopedService
    {
        /// <summary>
        /// Store context information for future reference
        /// </summary>
        /// <param name="contextId">Context identifier</param>
        /// <param name="content">Context content</param>
        /// <param name="metadata">Additional metadata</param>
        Task StoreContextAsync(string contextId, string content, Dictionary<string, object> metadata = null);

        /// <summary>
        /// Retrieve context information
        /// </summary>
        /// <param name="contextId">Context identifier</param>
        /// <returns>Context content</returns>
        Task<MCPContextResult> GetContextAsync(string contextId);

        /// <summary>
        /// Search contexts by semantic similarity
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="limit">Maximum results</param>
        /// <returns>Similar contexts</returns>
        Task<List<MCPContextResult>> SearchContextsAsync(string query, int limit = 10);

        /// <summary>
        /// Create knowledge graph entity
        /// </summary>
        /// <param name="entity">Entity data</param>
        Task CreateEntityAsync(MCPEntity entity);

        /// <summary>
        /// Create relationship between entities
        /// </summary>
        /// <param name="relationship">Relationship data</param>
        Task CreateRelationshipAsync(MCPRelationship relationship);

        /// <summary>
        /// Query knowledge graph
        /// </summary>
        /// <param name="query">Graph query</param>
        /// <returns>Query results</returns>
        Task<MCPGraphQueryResult> QueryGraphAsync(string query);
    }

    #region AI DTOs

    public class AIWorkflowGenerationInput
    {
        public string Description { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public List<string> Requirements { get; set; } = new();
        public string Industry { get; set; } = string.Empty;
        public string ProcessType { get; set; } = string.Empty;
        public bool IncludeApprovals { get; set; } = true;
        public bool IncludeNotifications { get; set; } = true;
        public int EstimatedDuration { get; set; } = 0;
    }

    public class AIWorkflowGenerationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public WorkflowInputDto GeneratedWorkflow { get; set; }
        public List<AIStageGenerationResult> Stages { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
        public double ConfidenceScore { get; set; }
    }

    public class AIStageGenerationResult
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Order { get; set; }
        public string AssignedGroup { get; set; } = string.Empty;
        public List<string> RequiredFields { get; set; } = new();
        public List<long> ChecklistIds { get; set; } = new();
        public List<long> QuestionnaireIds { get; set; } = new();
        public int EstimatedDuration { get; set; }
    }

    public class AIQuestionnaireGenerationInput
    {
        public string Purpose { get; set; } = string.Empty;
        public string TargetAudience { get; set; } = string.Empty;
        public List<string> Topics { get; set; } = new();
        public string Context { get; set; } = string.Empty;
        public int EstimatedQuestions { get; set; } = 10;
        public bool IncludeValidation { get; set; } = true;
        public string Complexity { get; set; } = "Medium"; // Simple, Medium, Complex
    }

    public class AIQuestionnaireGenerationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public QuestionnaireInputDto GeneratedQuestionnaire { get; set; }
        public List<string> Suggestions { get; set; } = new();
        public double ConfidenceScore { get; set; }
    }

    public class AIChecklistGenerationInput
    {
        public string ProcessName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Team { get; set; } = string.Empty;
        public List<string> RequiredSteps { get; set; } = new();
        public string Context { get; set; } = string.Empty;
        public bool IncludeDependencies { get; set; } = true;
        public bool IncludeEstimates { get; set; } = true;
    }

    public class AIChecklistGenerationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ChecklistInputDto GeneratedChecklist { get; set; }
        public List<string> Suggestions { get; set; } = new();
        public double ConfidenceScore { get; set; }
    }

    public class AIWorkflowStreamResult
    {
        public string Type { get; set; } = string.Empty; // "workflow", "stage", "complete", "error"
        public object Data { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsComplete { get; set; }
    }

    public class AIQuestionnaireStreamResult
    {
        public string Type { get; set; } = string.Empty; // "questionnaire", "section", "question", "complete", "error"
        public object Data { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsComplete { get; set; }
    }

    public class AIChecklistStreamResult
    {
        public string Type { get; set; } = string.Empty; // "checklist", "task", "complete", "error"
        public object Data { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsComplete { get; set; }
    }

    public class AIWorkflowEnhancementResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<AIEnhancementSuggestion> Suggestions { get; set; } = new();
        public WorkflowInputDto EnhancedWorkflow { get; set; }
    }

    public class AIEnhancementSuggestion
    {
        public string Type { get; set; } = string.Empty; // "add_stage", "modify_stage", "add_approval", etc.
        public string Description { get; set; } = string.Empty;
        public object Data { get; set; }
        public double Priority { get; set; }
    }

    public class AIValidationResult
    {
        public bool IsValid { get; set; }
        public List<AIValidationIssue> Issues { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
        public double QualityScore { get; set; }
    }

    public class AIValidationIssue
    {
        public string Severity { get; set; } = string.Empty; // "Error", "Warning", "Info"
        public string Message { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
        public string SuggestedFix { get; set; } = string.Empty;
    }

    public class AIRequirementsParsingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public AIRequirements Requirements { get; set; } = new();
    }

    public class AIRequirements
    {
        public string ProcessType { get; set; } = string.Empty;
        public List<string> Stakeholders { get; set; } = new();
        public List<string> Steps { get; set; } = new();
        public List<string> Approvals { get; set; } = new();
        public List<string> Notifications { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    #endregion

    #region MCP DTOs

    public class MCPContextResult
    {
        public string ContextId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public double RelevanceScore { get; set; }
    }

    public class MCPEntity
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new();
        public List<string> Tags { get; set; } = new();
    }

    public class MCPRelationship
    {
        public string Id { get; set; } = string.Empty;
        public string FromEntityId { get; set; } = string.Empty;
        public string ToEntityId { get; set; } = string.Empty;
        public string RelationType { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class MCPGraphQueryResult
    {
        public bool Success { get; set; }
        public List<MCPEntity> Entities { get; set; } = new();
        public List<MCPRelationship> Relationships { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// AI workflow modification input
    /// </summary>
    public class AIWorkflowModificationInput
    {
        public long WorkflowId { get; set; }
        public string Enhancement { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public List<string> Requirements { get; set; } = new();
        public bool PreserveExisting { get; set; } = true;
        public string ModificationMode { get; set; } = "modify"; // add, modify, remove, replace
    }

    // ========================= AI Chat DTOs =========================

    /// <summary>
    /// AI chat message
    /// </summary>
    public class AIChatMessage
    {
        public string Role { get; set; } = string.Empty; // 'user', 'assistant', 'system'
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// AI chat input
    /// </summary>
    public class AIChatInput
    {
        public List<AIChatMessage> Messages { get; set; } = new();
        public string Context { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string Mode { get; set; } = "general"; // 'workflow_planning', 'general'
    }

    /// <summary>
    /// AI chat response
    /// </summary>
    public class AIChatResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public AIChatResponseData Response { get; set; } = new();
        public string SessionId { get; set; } = string.Empty;
    }

    /// <summary>
    /// AI chat response data
    /// </summary>
    public class AIChatResponseData
    {
        public string Content { get; set; } = string.Empty;
        public List<string> Suggestions { get; set; } = new();
        public bool IsComplete { get; set; }
        public List<string> NextQuestions { get; set; } = new();
    }

    /// <summary>
    /// AI chat stream result
    /// </summary>
    public class AIChatStreamResult
    {
        public string Type { get; set; } = string.Empty; // 'delta', 'complete', 'error'
        public string Content { get; set; } = string.Empty;
        public bool IsComplete { get; set; }
        public string SessionId { get; set; } = string.Empty;
    }

    #endregion
} 