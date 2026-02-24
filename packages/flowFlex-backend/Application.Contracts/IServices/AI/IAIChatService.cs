using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.AI
{
    /// <summary>
    /// AI chat service interface.
    /// Responsible for chat message sending and streaming.
    /// </summary>
    public interface IAIChatService : IScopedService
    {
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
}
