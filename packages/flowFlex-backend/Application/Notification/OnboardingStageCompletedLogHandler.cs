using FlowFlex.Domain.Shared.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared;
using System.Text.Json;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Notification
{
    /// <summary>
    /// Onboarding stage completion event handler - stores events to database and evaluates stage conditions
    /// </summary>
    public class OnboardingStageCompletedLogHandler : INotificationHandler<OnboardingStageCompletedEvent>
    {
        private readonly ILogger<OnboardingStageCompletedLogHandler> _logger;
        private readonly IEventRepository _eventRepository;
        private readonly UserContext _userContext;
        private readonly IRulesEngineService _rulesEngineService;

        public OnboardingStageCompletedLogHandler(
            ILogger<OnboardingStageCompletedLogHandler> logger,
            IEventRepository eventRepository,
            UserContext userContext,
            IRulesEngineService rulesEngineService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _rulesEngineService = rulesEngineService ?? throw new ArgumentNullException(nameof(rulesEngineService));
        }

        public async Task Handle(OnboardingStageCompletedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing OnboardingStageCompletedEvent: {EventId}", notification.EventId);

                // Set UserContext from event data for background task compatibility
                // This ensures downstream services use the correct TenantId
                if (!string.IsNullOrEmpty(notification.TenantId))
                {
                    _userContext.TenantId = notification.TenantId;
                }
                if (notification.UserId > 0)
                {
                    _userContext.UserId = notification.UserId.ToString();
                }
                if (!string.IsNullOrEmpty(notification.UserName))
                {
                    _userContext.UserName = notification.UserName;
                }

                // 1. Save to ff_events table
                await SaveToEventTableAsync(notification);

                // 2. Evaluate stage condition (no longer execute actions directly)
                await EvaluateStageConditionAsync(notification);

                _logger.LogInformation("Successfully processed OnboardingStageCompletedEvent: {EventId}", notification.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing OnboardingStageCompletedEvent: {EventId}, Error: {Error}",
                    notification.EventId, ex.Message);

                // Don't rethrow exception to avoid affecting other event handlers
                await HandleEventProcessingErrorAsync(notification, ex);
            }
        }

        /// <summary>
        /// Evaluate stage condition for the completed stage
        /// </summary>
        private async Task EvaluateStageConditionAsync(OnboardingStageCompletedEvent eventData)
        {
            try
            {
                _logger.LogDebug("Evaluating stage condition for OnboardingId={OnboardingId}, StageId={StageId}",
                    eventData.OnboardingId, eventData.CompletedStageId);

                // Evaluate condition using RulesEngineService
                var result = await _rulesEngineService.EvaluateConditionAsync(
                    eventData.OnboardingId, 
                    eventData.CompletedStageId);

                if (result.IsConditionMet)
                {
                    _logger.LogInformation("Stage condition met for OnboardingId={OnboardingId}, StageId={StageId}",
                        eventData.OnboardingId, eventData.CompletedStageId);
                }
                else
                {
                    _logger.LogInformation("Stage condition not met for OnboardingId={OnboardingId}, StageId={StageId}, NextStageId={NextStageId}, Error={Error}",
                        eventData.OnboardingId, eventData.CompletedStageId, result.NextStageId, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating stage condition for OnboardingId={OnboardingId}, StageId={StageId}",
                    eventData.OnboardingId, eventData.CompletedStageId);
                // Don't rethrow - condition evaluation failure should not block event processing
            }
        }

        /// <summary>
        /// Save event to ff_events table
        /// </summary>
        private async Task SaveToEventTableAsync(OnboardingStageCompletedEvent eventData)
        {
            try
            {
                // Use event data for user info (available in background tasks)
                var processedBy = !string.IsNullOrEmpty(eventData.UserName) 
                    ? eventData.UserName 
                    : (_userContext?.UserName ?? "System");

                // Build event metadata
                var eventMetadata = new
                {
                    RoutingTags = eventData.RoutingTags,
                    Priority = eventData.Priority,
                    Source = eventData.Source,
                    ProcessingInfo = new
                    {
                        ProcessedAt = DateTimeOffset.UtcNow,
                        ProcessedBy = processedBy,
                        ProcessorVersion = "1.0"
                    }
                };

                // Create event entity
                var eventEntity = new Event
                {
                    EventId = eventData.EventId,
                    EventType = "OnboardingStageCompleted",
                    EventVersion = eventData.Version,
                    EventTimestamp = eventData.Timestamp,
                    AggregateId = eventData.OnboardingId,
                    AggregateType = "Onboarding",
                    EventSource = eventData.Source ?? "Unknown",
                    EventData = JsonSerializer.Serialize(eventData, new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }),
                    EventMetadata = JsonSerializer.Serialize(eventMetadata, new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }),
                    EventDescription = eventData.Description,
                    EventStatus = "Published",
                    EventTags = JsonSerializer.Serialize(eventData.Tags ?? new List<string>()),
                    RelatedEntityId = eventData.CompletedStageId,
                    RelatedEntityType = "Stage",
                    ProcessCount = 1,
                    LastProcessedAt = DateTimeOffset.UtcNow,
                    RequiresRetry = false,
                    NextRetryAt = null,
                    MaxRetryCount = 3,
                    TenantId = eventData.TenantId
                };

                // Set creation info using event data for background task compatibility
                eventEntity.InitCreateInfoFromEvent(eventData.UserId, eventData.UserName);

                // Save to database
                await _eventRepository.InsertAsync(eventEntity);

                _logger.LogDebug("Event saved to ff_events table: {EventId}", eventData.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving event to ff_events table: {EventId}", eventData.EventId);
                throw;
            }
        }

        /// <summary>
        /// Handle event processing error
        /// </summary>
        private async Task HandleEventProcessingErrorAsync(OnboardingStageCompletedEvent eventData, Exception error)
        {
            try
            {
                // Save failed event to events table for retry
                var failedEventEntity = new Event
                {
                    EventId = eventData.EventId + "_failed",
                    EventType = "OnboardingStageCompleted",
                    EventVersion = eventData.Version,
                    EventTimestamp = eventData.Timestamp,
                    AggregateId = eventData.OnboardingId,
                    AggregateType = "Onboarding",
                    EventSource = eventData.Source ?? "Unknown",
                    EventData = JsonSerializer.Serialize(eventData, new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }),
                    EventDescription = $"Failed to process: {eventData.Description}",
                    EventStatus = "Failed",
                    EventTags = JsonSerializer.Serialize(eventData.Tags ?? new List<string>()),
                    RelatedEntityId = eventData.CompletedStageId,
                    RelatedEntityType = "Stage",
                    ProcessCount = 1,
                    LastProcessedAt = DateTimeOffset.UtcNow,
                    RequiresRetry = true,
                    MaxRetryCount = 3,
                    ErrorMessage = error.Message,
                    NextRetryAt = DateTimeOffset.UtcNow.AddMinutes(5),
                    TenantId = eventData.TenantId
                };

                // Set creation info using event data for background task compatibility
                failedEventEntity.InitCreateInfoFromEvent(eventData.UserId, eventData.UserName);
                await _eventRepository.InsertAsync(failedEventEntity);

                _logger.LogWarning("Failed event saved to ff_events table for retry: {EventId}", failedEventEntity.EventId);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Error saving failed event: {EventId}", eventData.EventId);
            }
        }
    }
}
