using FlowFlex.Domain.Entities.OW;

namespace FlowFlex.Domain.Repository.OW
{
    /// <summary>
    /// Event repository interface
    /// </summary>
    public interface IEventRepository : IBaseRepository<Event>
    {
        /// <summary>
        /// Get events by event type
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="days">Recent days (default: 30)</param>
        /// <returns>Event list</returns>
        Task<List<Event>> GetByEventTypeAsync(string eventType, int days = 30);

        /// <summary>
        /// Get events by aggregate ID
        /// </summary>
        /// <param name="aggregateId">Aggregate ID</param>
        /// <param name="aggregateType">Aggregate type (optional)</param>
        /// <returns>Event list</returns>
        Task<List<Event>> GetByAggregateIdAsync(long aggregateId, string? aggregateType = null);

        /// <summary>
        /// Get events by event ID
        /// </summary>
        /// <param name="eventId">Event ID</param>
        /// <returns>Event or null</returns>
        Task<Event?> GetByEventIdAsync(string eventId);

        /// <summary>
        /// Get failed events that need retry
        /// </summary>
        /// <param name="maxRetryCount">Maximum retry count</param>
        /// <returns>Failed events list</returns>
        Task<List<Event>> GetFailedEventsForRetryAsync(int maxRetryCount = 3);

        /// <summary>
        /// Get events by status
        /// </summary>
        /// <param name="status">Event status</param>
        /// <param name="days">Recent days (default: 7)</param>
        /// <returns>Event list</returns>
        Task<List<Event>> GetByStatusAsync(string status, int days = 7);

        /// <summary>
        /// Get events by source
        /// </summary>
        /// <param name="source">Event source</param>
        /// <param name="days">Recent days (default: 7)</param>
        /// <returns>Event list</returns>
        Task<List<Event>> GetBySourceAsync(string source, int days = 7);

        /// <summary>
        /// Get events by tags
        /// </summary>
        /// <param name="tags">Event tags to search for</param>
        /// <param name="days">Recent days (default: 7)</param>
        /// <returns>Event list</returns>
        Task<List<Event>> GetByTagsAsync(List<string> tags, int days = 7);

        /// <summary>
        /// Update event status
        /// </summary>
        /// <param name="id">Event ID</param>
        /// <param name="status">New status</param>
        /// <param name="errorMessage">Error message (optional)</param>
        /// <returns>Success or failure</returns>
        Task<bool> UpdateEventStatusAsync(long id, string status, string? errorMessage = null);

        /// <summary>
        /// Mark event as processed
        /// </summary>
        /// <param name="id">Event ID</param>
        /// <returns>Success or failure</returns>
        Task<bool> MarkAsProcessedAsync(long id);

        /// <summary>
        /// Increment process count
        /// </summary>
        /// <param name="id">Event ID</param>
        /// <returns>Success or failure</returns>
        Task<bool> IncrementProcessCountAsync(long id);

        /// <summary>
        /// Clean up old events
        /// </summary>
        /// <param name="days">Days to keep (default: 90)</param>
        /// <returns>Number of deleted events</returns>
        Task<int> CleanupOldEventsAsync(int days = 90);
    }
}