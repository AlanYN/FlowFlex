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
        /// Parse natural language with explicit AI model override
        /// </summary>
        /// <param name="naturalLanguage">Natural language input</param>
        /// <param name="modelProvider">AI provider name, e.g. zhipuai/openai/anthropic</param>
        /// <param name="modelName">Model name, e.g. glm-4/gpt-4o/claude-3</param>
        /// <param name="modelId">Optional user model configuration id</param>
        /// <returns>Structured requirements</returns>
        Task<AIRequirementsParsingResult> ParseRequirementsAsync(string naturalLanguage, string? modelProvider, string? modelName, string? modelId);

        /// <summary>
        /// Enhance existing workflow using modification input
        /// </summary>
        /// <param name="input">Modification input</param>
        /// <returns>Enhanced workflow result</returns>
        Task<AIWorkflowGenerationResult> EnhanceWorkflowAsync(AIWorkflowModificationInput input);

        /// <summary>
        /// Create actual checklist and questionnaire records and associate them with stages
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="stages">Generated stages</param>
        /// <param name="checklists">Generated checklists</param>
        /// <param name="questionnaires">Generated questionnaires</param>
        /// <returns>Success status</returns>
        Task<bool> CreateStageComponentsAsync(long workflowId, List<AIStageGenerationResult> stages, List<AIChecklistGenerationResult> checklists, List<AIQuestionnaireGenerationResult> questionnaires);

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

        /// <summary>
        /// Generate AI summary for stage based on checklist tasks and questionnaire questions
        /// </summary>
        /// <param name="input">Stage summary generation input</param>
        /// <returns>Generated stage summary</returns>
        Task<AIStageSummaryResult> GenerateStageSummaryAsync(AIStageSummaryInput input);
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
        
        // AI Model Information
        public string? ModelId { get; set; }
        public string? ModelProvider { get; set; }
        public string? ModelName { get; set; }
        
        // Conversation History Information
        public List<AIChatMessage>? ConversationHistory { get; set; }
        public string? SessionId { get; set; }
        
        // Additional Context Information
        public ConversationMetadata? ConversationMetadata { get; set; }
    }

    /// <summary>
    /// Conversation metadata for workflow generation
    /// </summary>
    public class ConversationMetadata
    {
        public int TotalMessages { get; set; }
        public string? ConversationStartTime { get; set; }
        public string? ConversationEndTime { get; set; }
        public string? ConversationMode { get; set; }
    }

    public class AIWorkflowGenerationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public WorkflowInputDto GeneratedWorkflow { get; set; }
        public List<AIStageGenerationResult> Stages { get; set; } = new();
        public List<AIChecklistGenerationResult> Checklists { get; set; } = new();
        public List<AIQuestionnaireGenerationResult> Questionnaires { get; set; } = new();
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
        public List<AIQuestionGenerationResult> Questions { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
        public double ConfidenceScore { get; set; }
    }

    public class AIQuestionGenerationResult
    {
        public string Id { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public string Type { get; set; } = "text"; // text, select, multiselect, number, date, boolean
        public List<string> Options { get; set; } = new();
        public bool IsRequired { get; set; }
        public string Category { get; set; } = string.Empty;
        public string HelpText { get; set; } = string.Empty;
        public string ValidationRule { get; set; } = string.Empty;
        public object DefaultValue { get; set; }
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
        public List<AITaskGenerationResult> Tasks { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
        public double ConfidenceScore { get; set; }
    }

    public class AITaskGenerationResult
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public bool Completed { get; set; } = false;
        public int EstimatedMinutes { get; set; } = 0;
        public string Category { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new();
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
        [Newtonsoft.Json.JsonProperty("role")]
        public string Role { get; set; } = string.Empty; // 'user', 'assistant', 'system'
        
        [Newtonsoft.Json.JsonProperty("content")]
        public string Content { get; set; } = string.Empty;
        
        [Newtonsoft.Json.JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// AI chat input
    /// </summary>
    public class AIChatInput
    {
        [Newtonsoft.Json.JsonProperty("messages")]
        public List<AIChatMessage> Messages { get; set; } = new();
        
        [Newtonsoft.Json.JsonProperty("context")]
        public string Context { get; set; } = string.Empty;
        
        [Newtonsoft.Json.JsonProperty("sessionId")]
        public string SessionId { get; set; } = string.Empty;
        
        [Newtonsoft.Json.JsonProperty("mode")]
        public string Mode { get; set; } = "general"; // 'workflow_planning', 'general'
        
        // 添加模型相关字段
        [Newtonsoft.Json.JsonProperty("modelId")]
        public string? ModelId { get; set; }
        
        [Newtonsoft.Json.JsonProperty("modelProvider")]
        public string? ModelProvider { get; set; }
        
        [Newtonsoft.Json.JsonProperty("modelName")]
        public string? ModelName { get; set; }
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
        [Newtonsoft.Json.JsonProperty("type")]
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; // 'delta', 'complete', 'error'
        
        [Newtonsoft.Json.JsonProperty("content")]
        [System.Text.Json.Serialization.JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
        
        [Newtonsoft.Json.JsonProperty("isComplete")]
        [System.Text.Json.Serialization.JsonPropertyName("isComplete")]
        public bool IsComplete { get; set; }
        
        [Newtonsoft.Json.JsonProperty("sessionId")]
        [System.Text.Json.Serialization.JsonPropertyName("sessionId")]
        public string SessionId { get; set; } = string.Empty;
    }

    #endregion

    #region AI Stage Summary DTOs

    /// <summary>
    /// AI stage summary generation input
    /// </summary>
    public class AIStageSummaryInput
    {
        /// <summary>
        /// Stage ID
        /// </summary>
        public long StageId { get; set; }

        /// <summary>
        /// Stage name
        /// </summary>
        public string StageName { get; set; } = string.Empty;

        /// <summary>
        /// Stage description
        /// </summary>
        public string StageDescription { get; set; } = string.Empty;

        /// <summary>
        /// Checklist tasks information
        /// </summary>
        public List<AISummaryTaskInfo> Tasks { get; set; } = new();

        /// <summary>
        /// Checklist tasks information (alias for compatibility)
        /// </summary>
        public List<AISummaryTaskInfo> ChecklistTasks 
        { 
            get => Tasks; 
            set => Tasks = value; 
        }

        /// <summary>
        /// Questionnaire questions information
        /// </summary>
        public List<AISummaryQuestionInfo> Questions { get; set; } = new();

        /// <summary>
        /// Questionnaire questions information (alias for compatibility)
        /// </summary>
        public List<AISummaryQuestionInfo> QuestionnaireQuestions 
        { 
            get => Questions; 
            set => Questions = value; 
        }

        /// <summary>
        /// AI model ID to use (optional)
        /// </summary>
        public string? ModelId { get; set; }

        /// <summary>
        /// AI model provider (optional)
        /// </summary>
        public string? ModelProvider { get; set; }

        /// <summary>
        /// AI model name (optional)
        /// </summary>
        public string? ModelName { get; set; }

        /// <summary>
        /// Summary generation language
        /// </summary>
        public string Language { get; set; } = "zh-CN";

        /// <summary>
        /// Summary length preference
        /// </summary>
        public string SummaryLength { get; set; } = "medium";

        /// <summary>
        /// Additional context for summary generation
        /// </summary>
        public string AdditionalContext { get; set; } = string.Empty;

        /// <summary>
        /// Include task analysis in summary
        /// </summary>
        public bool IncludeTaskAnalysis { get; set; } = true;

        /// <summary>
        /// Include questionnaire insights in summary
        /// </summary>
        public bool IncludeQuestionnaireInsights { get; set; } = true;

        /// <summary>
        /// Include risk assessment in summary
        /// </summary>
        public bool IncludeRiskAssessment { get; set; } = true;

        /// <summary>
        /// Include recommendations in summary
        /// </summary>
        public bool IncludeRecommendations { get; set; } = true;

        /// <summary>
        /// Static fields information
        /// </summary>
        public List<AISummaryFieldInfo> StaticFields { get; set; } = new();
    }

    /// <summary>
    /// Task information for AI summary
    /// </summary>
    public class AISummaryTaskInfo
    {
        /// <summary>
        /// Task ID
        /// </summary>
        public long TaskId { get; set; }

        /// <summary>
        /// Task title
        /// </summary>
        public string TaskTitle { get; set; } = string.Empty;

        /// <summary>
        /// Task name (alias for TaskTitle)
        /// </summary>
        public string TaskName 
        { 
            get => TaskTitle; 
            set => TaskTitle = value; 
        }

        /// <summary>
        /// Task description
        /// </summary>
        public string TaskDescription { get; set; } = string.Empty;

        /// <summary>
        /// Description (alias for TaskDescription)
        /// </summary>
        public string Description 
        { 
            get => TaskDescription; 
            set => TaskDescription = value; 
        }

        /// <summary>
        /// Task completion notes
        /// </summary>
        public string CompletionNotes { get; set; } = string.Empty;

        /// <summary>
        /// Is task required
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Is task completed
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Task category
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Estimated completion time in minutes
        /// </summary>
        public int EstimatedMinutes { get; set; }
    }

    /// <summary>
    /// Question information for AI summary
    /// </summary>
    public class AISummaryQuestionInfo
    {
        /// <summary>
        /// Question ID
        /// </summary>
        public long QuestionId { get; set; }

        /// <summary>
        /// Question text
        /// </summary>
        public string QuestionText { get; set; } = string.Empty;

        /// <summary>
        /// Question type
        /// </summary>
        public string QuestionType { get; set; } = string.Empty;

        /// <summary>
        /// Is question required
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Is question answered
        /// </summary>
        public bool IsAnswered { get; set; }

        /// <summary>
        /// Question answer (if available)
        /// </summary>
        public string? Answer { get; set; }

        /// <summary>
        /// Question category
        /// </summary>
        public string Category { get; set; } = string.Empty;
    }

    /// <summary>
    /// Static field information for AI summary
    /// </summary>
    public class AISummaryFieldInfo
    {
        /// <summary>
        /// Field name
        /// </summary>
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// Field display name
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Field type
        /// </summary>
        public string FieldType { get; set; } = string.Empty;

        /// <summary>
        /// Is field required
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Field description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Field category
        /// </summary>
        public string Category { get; set; } = "Static Field";
    }

    /// <summary>
    /// AI stage summary generation result
    /// </summary>
    public class AIStageSummaryResult
    {
        /// <summary>
        /// Generation success status
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Result message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Generated summary text
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Structured summary breakdown
        /// </summary>
        public AIStageSummaryBreakdown? Breakdown { get; set; }

        /// <summary>
        /// Key insights from the analysis
        /// </summary>
        public List<string> KeyInsights { get; set; } = new();

        /// <summary>
        /// Recommendations for improvement
        /// </summary>
        public List<string> Recommendations { get; set; } = new();

        /// <summary>
        /// Completion status analysis
        /// </summary>
        public AISummaryCompletionStatus? CompletionStatus { get; set; }

        /// <summary>
        /// Generation timestamp
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// AI model used for generation
        /// </summary>
        public string? ModelUsed { get; set; }

        /// <summary>
        /// Confidence score of the generated summary (0-1)
        /// </summary>
        public double ConfidenceScore { get; set; } = 0.8;
    }

    /// <summary>
    /// Structured breakdown of stage summary
    /// </summary>
    public class AIStageSummaryBreakdown
    {
        /// <summary>
        /// Overall stage overview
        /// </summary>
        public string Overview { get; set; } = string.Empty;

        /// <summary>
        /// Checklist tasks summary
        /// </summary>
        public string ChecklistSummary { get; set; } = string.Empty;

        /// <summary>
        /// Questionnaire responses summary
        /// </summary>
        public string QuestionnaireSummary { get; set; } = string.Empty;

        /// <summary>
        /// Progress analysis
        /// </summary>
        public string ProgressAnalysis { get; set; } = string.Empty;

        /// <summary>
        /// Risk assessment
        /// </summary>
        public string RiskAssessment { get; set; } = string.Empty;
    }

    /// <summary>
    /// Stage completion status analysis
    /// </summary>
    public class AISummaryCompletionStatus
    {
        /// <summary>
        /// Overall completion rate (0-100)
        /// </summary>
        public double OverallCompletionRate { get; set; }

        /// <summary>
        /// Checklist completion rate (0-100)
        /// </summary>
        public double ChecklistCompletionRate { get; set; }

        /// <summary>
        /// Questionnaire completion rate (0-100)
        /// </summary>
        public double QuestionnaireCompletionRate { get; set; }

        /// <summary>
        /// Are critical tasks completed
        /// </summary>
        public bool CriticalTasksCompleted { get; set; }

        /// <summary>
        /// Are required questions answered
        /// </summary>
        public bool RequiredQuestionsAnswered { get; set; }

        /// <summary>
        /// Estimated time to completion
        /// </summary>
        public string EstimatedTimeToCompletion { get; set; } = string.Empty;
    }

    #endregion
} 