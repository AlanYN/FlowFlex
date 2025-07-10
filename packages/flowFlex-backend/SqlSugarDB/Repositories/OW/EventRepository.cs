using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Linq.Expressions;
using FlowFlex.Domain.Abstracts;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.SqlSugarDB.Implements.OW
{
    /// <summary>
    /// Event repository implementation
    /// </summary>
    public class EventRepository : BaseRepository<Event>, IEventRepository, IScopedService
    {
        public EventRepository(ISqlSugarClient dbContext) : base(dbContext) { }

        /// <summary>
        /// Get events by event type
        /// </summary>
        public async Task<List<Event>> GetByEventTypeAsync(string eventType, int days = 30)
        {
            var startDate = DateTimeOffset.UtcNow.AddDays(-days);
            return await base.GetListAsync(
                x => x.EventType == eventType && x.EventTimestamp >= startDate && x.IsValid,
                x => x.EventTimestamp,
                SqlSugar.OrderByType.Desc
            );
        }

        /// <summary>
        /// Get events by aggregate ID
        /// </summary>
        public async Task<List<Event>> GetByAggregateIdAsync(long aggregateId, string? aggregateType = null)
        {
            var whereExpressions = new List<Expression<Func<Event, bool>>>
            {
                x => x.AggregateId == aggregateId && x.IsValid
            };

            if (!string.IsNullOrEmpty(aggregateType))
            {
                whereExpressions.Add(x => x.AggregateType == aggregateType);
            }

            var (events, _) = await base.GetPageListAsync(
                whereExpressions,
                1,
                1000, // Large page size to get all events
                x => x.EventTimestamp,
                false // Descending order
            );

            return events;
        }

        /// <summary>
        /// Get events by event ID
        /// </summary>
        public async Task<Event?> GetByEventIdAsync(string eventId)
        {
            return await base.GetFirstAsync(x => x.EventId == eventId && x.IsValid);
        }

        /// <summary>
        /// Get failed events that need retry
        /// </summary>
        public async Task<List<Event>> GetFailedEventsForRetryAsync(int maxRetryCount = 3)
        {
            var now = DateTimeOffset.UtcNow;
            return await base.GetListAsync(
                x => x.RequiresRetry && 
                     x.ProcessCount < maxRetryCount && 
                     (x.NextRetryAt == null || x.NextRetryAt <= now) &&
                     x.EventStatus == "Failed" &&
                     x.IsValid,
                x => x.NextRetryAt,
                SqlSugar.OrderByType.Asc
            );
        }

        /// <summary>
        /// Get events by status
        /// </summary>
        public async Task<List<Event>> GetByStatusAsync(string status, int days = 7)
        {
            var startDate = DateTimeOffset.UtcNow.AddDays(-days);
            return await base.GetListAsync(
                x => x.EventStatus == status && x.EventTimestamp >= startDate && x.IsValid,
                x => x.EventTimestamp,
                SqlSugar.OrderByType.Desc
            );
        }

        /// <summary>
        /// Get events by source
        /// </summary>
        public async Task<List<Event>> GetBySourceAsync(string source, int days = 7)
        {
            var startDate = DateTimeOffset.UtcNow.AddDays(-days);
            return await base.GetListAsync(
                x => x.EventSource == source && x.EventTimestamp >= startDate && x.IsValid,
                x => x.EventTimestamp,
                SqlSugar.OrderByType.Desc
            );
        }

        /// <summary>
        /// Get events by tags
        /// </summary>
        public async Task<List<Event>> GetByTagsAsync(List<string> tags, int days = 7)
        {
            var startDate = DateTimeOffset.UtcNow.AddDays(-days);
            
            // Use JSON contains query for PostgreSQL
            var query = db.Queryable<Event>()
                .Where(x => x.EventTimestamp >= startDate && x.IsValid);

            // Add tag filters
            foreach (var tag in tags)
            {
                query = query.Where(x => x.EventTags.Contains($"\"{tag}\""));
            }

            return await query.OrderByDescending(x => x.EventTimestamp).ToListAsync();
        }

        /// <summary>
        /// Update event status
        /// </summary>
        public async Task<bool> UpdateEventStatusAsync(long id, string status, string? errorMessage = null)
        {
            var entity = await base.GetByIdAsync(id);
            if (entity == null) return false;

            entity.EventStatus = status;
            entity.LastProcessedAt = DateTimeOffset.UtcNow;
            
            if (!string.IsNullOrEmpty(errorMessage))
            {
                entity.ErrorMessage = errorMessage;
            }

            return await base.UpdateAsync(entity);
        }

        /// <summary>
        /// Mark event as processed
        /// </summary>
        public async Task<bool> MarkAsProcessedAsync(long id)
        {
            var entity = await base.GetByIdAsync(id);
            if (entity == null) return false;

            entity.EventStatus = "Processed";
            entity.LastProcessedAt = DateTimeOffset.UtcNow;
            entity.RequiresRetry = false;

            return await base.UpdateAsync(entity);
        }

        /// <summary>
        /// Increment process count
        /// </summary>
        public async Task<bool> IncrementProcessCountAsync(long id)
        {
            var entity = await base.GetByIdAsync(id);
            if (entity == null) return false;

            entity.ProcessCount++;
            entity.LastProcessedAt = DateTimeOffset.UtcNow;

            // Set next retry time if failed
            if (entity.EventStatus == "Failed" && entity.ProcessCount < entity.MaxRetryCount)
            {
                entity.NextRetryAt = DateTimeOffset.UtcNow.AddMinutes(Math.Pow(2, entity.ProcessCount)); // Exponential backoff
            }

            return await base.UpdateAsync(entity);
        }

        /// <summary>
        /// Clean up old events
        /// </summary>
        public async Task<int> CleanupOldEventsAsync(int days = 90)
        {
            var expiredDate = DateTimeOffset.UtcNow.AddDays(-days);
            var expiredEvents = await base.GetListAsync(x => x.EventTimestamp < expiredDate && x.IsValid);

            if (expiredEvents.Any())
            {
                // Soft delete - update each event individually
                foreach (var evt in expiredEvents)
                {
                    evt.IsValid = false;
                    evt.ModifyDate = DateTimeOffset.UtcNow;
                    evt.ModifyBy = "System";
                    await base.UpdateAsync(evt);
                }
                return expiredEvents.Count;
            }

            return 0;
        }
    }
} 