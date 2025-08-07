using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Options
{
    /// <summary>
    /// AI service configuration options
    /// </summary>
    public class AIOptions
    {
        public static readonly string SectionName = "AI";

        /// <summary>
        /// AI provider (OpenAI, Azure, Claude, LocalLLM)
        /// </summary>
        [Required]
        public string Provider { get; set; } = "OpenAI";

        /// <summary>
        /// OpenAI configuration
        /// </summary>
        public OpenAIConfig OpenAI { get; set; } = new();

        /// <summary>
        /// ZhipuAI configuration
        /// </summary>
        public ZhipuAIConfig ZhipuAI { get; set; } = new();

        /// <summary>
        /// Azure OpenAI configuration
        /// </summary>
        public AzureConfig Azure { get; set; } = new();

        /// <summary>
        /// Claude configuration
        /// </summary>
        public ClaudeConfig Claude { get; set; } = new();

        /// <summary>
        /// Local LLM configuration
        /// </summary>
        public LocalLLMConfig LocalLLM { get; set; } = new();

        /// <summary>
        /// Feature flags
        /// </summary>
        public AIFeatures Features { get; set; } = new();

        /// <summary>
        /// System prompts
        /// </summary>
        public AIPrompts Prompts { get; set; } = new();

        /// <summary>
        /// Connection test settings
        /// </summary>
        public AIConnectionTest ConnectionTest { get; set; } = new();
    }

    public class OpenAIConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";
        public string Model { get; set; } = "gpt-4";
        public int MaxTokens { get; set; } = 4096;
        public double Temperature { get; set; } = 0.7;
        public bool EnableStreaming { get; set; } = true;
    }

    public class ZhipuAIConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://open.bigmodel.cn/api/paas/v4";
        public string Model { get; set; } = "glm-4";
        public int MaxTokens { get; set; } = 8192;
        public double Temperature { get; set; } = 0.7;
        public bool EnableStreaming { get; set; } = true;
    }

    public class AzureConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public string DeploymentName { get; set; } = string.Empty;
        public string ApiVersion { get; set; } = "2024-02-01";
    }

    public class ClaudeConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.anthropic.com";
        public string Model { get; set; } = "claude-3-sonnet-20240229";
        public int MaxTokens { get; set; } = 4096;
    }

    public class LocalLLMConfig
    {
        public string BaseUrl { get; set; } = "http://localhost:11434";
        public string Model { get; set; } = "llama2";
        public int MaxTokens { get; set; } = 4096;
    }

    public class AIFeatures
    {
        public bool WorkflowGeneration { get; set; } = true;
        public bool QuestionnaireGeneration { get; set; } = true;
        public bool ChecklistGeneration { get; set; } = true;
        public bool RealTimePreview { get; set; } = true;
        public bool ContextMemory { get; set; } = true;
    }

    public class AIPrompts
    {
        public string WorkflowSystem { get; set; } = "You are an expert workflow designer. Generate structured workflow definitions based on user requirements.";
        public string QuestionnaireSystem { get; set; } = "You are a questionnaire design expert. Create comprehensive questionnaires based on user needs.";
        public string ChecklistSystem { get; set; } = "You are a task management expert. Generate detailed checklists for various processes.";
    }

    public class AIConnectionTest
    {
        /// <summary>
        /// Connection test timeout in seconds (default: 30)
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum retry attempts for connection test (default: 1)
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 1;

        /// <summary>
        /// Delay between retry attempts in milliseconds (default: 1000)
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// Enable detailed logging for connection tests (default: true)
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = true;
    }

    /// <summary>
    /// MCP service configuration options
    /// </summary>
    public class MCPOptions
    {
        public static readonly string SectionName = "MCP";

        public bool EnableMCP { get; set; } = true;
        public MCPServices Services { get; set; } = new();
    }

    public class MCPServices
    {
        public MCPMemoryConfig Memory { get; set; } = new();
        public MCPContextConfig Context { get; set; } = new();
    }

    public class MCPMemoryConfig
    {
        public string Provider { get; set; } = "InMemory";
        public string ConnectionString { get; set; } = string.Empty;
        public int MaxEntities { get; set; } = 10000;
        public bool EnablePersistence { get; set; } = true;
    }

    public class MCPContextConfig
    {
        public int MaxContextLength { get; set; } = 8192;
        public bool EnableSemanticSearch { get; set; } = true;
        public string EmbeddingModel { get; set; } = "text-embedding-ada-002";
    }
} 