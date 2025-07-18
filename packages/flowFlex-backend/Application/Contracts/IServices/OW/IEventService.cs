using FlowFlex.Application.Contracts.Dtos.OW.Event;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Event service interface
    /// </summary>
    public interface IEventService
    {
        /// <summary>
        /// Get events by type
        /// </summary>
        /// <param name="eventType">Event type</param>
        /// <param name="days">Recent days</param>
        /// <returns>Event list</returns>
        Task<List<EventOutputDto>> GetEventsByTypeAsync(string eventType, int days = 30);

        /// <summary>
        /// Get events by aggregate ID
        /// </summary>
        /// <param name="aggregateId">Aggregate ID</param>
        /// <param name="aggregateType">Aggregate type</param>
        /// <returns>Event list</returns>
        Task<List<EventOutputDto>> GetEventsByAggregateIdAsync(long aggregateId, string? aggregateType = null);

        /// <summary>
        /// Get event by event ID
        /// </summary>
        /// <param name="eventId">Event ID</param>
        /// <returns>Event or null</returns>
        Task<EventOutputDto?> GetEventByEventIdAsync(string eventId);

        /// <summary>
        /// Query events with pagination
        /// </summary>
        /// <param name="request">Query request</param>
        /// <returns>Paginated events</returns>
        Task<PageModelDto<EventOutputDto>> QueryEventsAsync(EventQueryRequest request);

        /// <summary>
        /// Get failed events for retry
        /// </summary>
        /// <param name="maxRetryCount">Maximum retry count</param>
        /// <returns>Failed events</returns>
        Task<List<EventOutputDto>> GetFailedEventsForRetryAsync(int maxRetryCount = 3);

        /// <summary>
        /// Retry failed event
        /// </summary>
        /// <param name="eventId">Event ID</param>
        /// <returns>Success or failure</returns>
        Task<bool> RetryFailedEventAsync(string eventId);

        /// <summary>
        /// Mark event as processed
        /// </summary>
        /// <param name="eventId">Event ID</param>
        /// <returns>Success or failure</returns>
        Task<bool> MarkEventAsProcessedAsync(string eventId);

        /// <summary>
        /// Get event statistics
        /// </summary>
        /// <param name="days">Recent days</param>
        /// <returns>Event statistics</returns>
        Task<EventStatisticsDto> GetEventStatisticsAsync(int days = 7);

        /// <summary>
        /// Clean up old events
        /// </summary>
        /// <param name="days">Days to keep</param>
        /// <returns>Number of cleaned events</returns>
        Task<int> CleanupOldEventsAsync(int days = 90);
    }
}