using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.AI
{
    /// <summary>
    /// Unified AI provider adapter interface for calling various AI providers
    /// Encapsulates provider routing, request construction, and response deserialization
    /// </summary>
    public interface IAIProviderAdapter : IScopedService
    {
        /// <summary>
        /// Call AI provider with a single prompt (for generation/parsing scenarios)
        /// </summary>
        /// <param name="request">Provider request containing prompt and optional configuration</param>
        /// <returns>Unified provider response</returns>
        Task<AIProviderResponse> CallAsync(AIProviderRequest request);

        /// <summary>
        /// Call AI provider with multi-message chat input
        /// </summary>
        /// <param name="request">Chat provider request containing messages and optional configuration</param>
        /// <returns>Unified provider response</returns>
        Task<AIProviderResponse> CallChatAsync(AIChatProviderRequest request);

        /// <summary>
        /// Stream AI provider response with multi-message chat input
        /// </summary>
        /// <param name="request">Chat provider request containing messages and optional configuration</param>
        /// <returns>Async enumerable of string chunks from the AI provider</returns>
        IAsyncEnumerable<string> StreamChatAsync(AIChatProviderRequest request);

        /// <summary>
        /// Call AI provider with automatic fallback on failure
        /// When the primary provider fails, attempts a fallback provider based on configuration
        /// </summary>
        /// <param name="request">Provider request containing prompt and optional configuration</param>
        /// <returns>Unified provider response from primary or fallback provider</returns>
        Task<AIProviderResponse> CallWithFallbackAsync(AIProviderRequest request);
    }

    /// <summary>
    /// Request DTO for single-prompt AI provider calls
    /// Used for generation, parsing, and other non-chat scenarios
    /// </summary>
    public class AIProviderRequest
    {
        /// <summary>
        /// The prompt text to send to the AI provider
        /// </summary>
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// Optional model ID to use for the request
        /// </summary>
        public string? ModelId { get; set; }

        /// <summary>
        /// Optional provider name to route the request (e.g., "zhipuai", "openai", "gemini", "claude", "deepseek")
        /// When not specified, the default provider from tenant configuration is used
        /// </summary>
        public string? Provider { get; set; }

        /// <summary>
        /// Optional model name override
        /// </summary>
        public string? ModelName { get; set; }

        /// <summary>
        /// Optional max tokens override for the request
        /// When not specified, the default max tokens from model configuration is used
        /// </summary>
        public int? MaxTokensOverride { get; set; }
    }

    /// <summary>
    /// Request DTO for multi-message chat AI provider calls
    /// Used for chat and streaming scenarios
    /// </summary>
    public class AIChatProviderRequest
    {
        /// <summary>
        /// List of chat messages to send to the AI provider
        /// </summary>
        public List<object> Messages { get; set; } = new();

        /// <summary>
        /// Optional AI model configuration containing provider details, API key, base URL, etc.
        /// </summary>
        public AIModelConfig? Config { get; set; }

        /// <summary>
        /// Optional provider name to route the request
        /// When not specified, the provider from Config is used
        /// </summary>
        public string? Provider { get; set; }
    }

    /// <summary>
    /// Unified response DTO from AI provider calls
    /// Contains the response content, token usage, and provider metadata
    /// </summary>
    public class AIProviderResponse
    {
        /// <summary>
        /// Whether the AI provider call was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// The response content from the AI provider
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Error message when the call fails
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Token usage information as JSON string
        /// </summary>
        public string TokenUsage { get; set; } = string.Empty;

        /// <summary>
        /// The provider that handled the request
        /// </summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// The model name used for the request
        /// </summary>
        public string ModelName { get; set; } = string.Empty;

        /// <summary>
        /// The model ID used for the request
        /// </summary>
        public string ModelId { get; set; } = string.Empty;
    }
}
