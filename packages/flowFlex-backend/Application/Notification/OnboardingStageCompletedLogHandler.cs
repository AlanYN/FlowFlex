using FlowFlex.Domain.Shared.Events;
using FlowFlex.Domain.Shared.Events.Action;
using MediatR;
using Microsoft.Extensions.Logging;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared;
using System.Text.Json;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Application.Services.OW.Extensions;

namespace FlowFlex.Application.Notification
{
    /// <summary>
    /// Onboarding stage completion event handler - stores events to database
    /// </summary>
    public class OnboardingStageCompletedLogHandler : INotificationHandler<OnboardingStageCompletedEvent>
    {
        private readonly ILogger<OnboardingStageCompletedLogHandler> _logger;
        private readonly IMediator _mediator;
        private readonly IEventRepository _eventRepository;
        private readonly UserContext _userContext;

        public OnboardingStageCompletedLogHandler(
            ILogger<OnboardingStageCompletedLogHandler> logger,
            IMediator mediator,
            IEventRepository eventRepository,
            UserContext userContext)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        }

        public async Task Handle(OnboardingStageCompletedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("开始处理 OnboardingStageCompletedEvent: {EventId}", notification.EventId);

                // 1. 保存到新的事件表 ff_events
                await SaveToEventTableAsync(notification);

                // 2. 发布 ActionTriggerEvent 
                await PublishActionTriggerEventAsync(notification);

                // 3. 保存到原有的阶段完成日志表 ff_stage_completion_log (保持向后兼容)
                // Stage completion log functionality removed

                _logger.LogInformation("成功处理 OnboardingStageCompletedEvent: {EventId}", notification.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理 OnboardingStageCompletedEvent 时发生错误: {EventId}, 错误: {Error}",
                    notification.EventId, ex.Message);

                // 不重新抛出异常，避免影响其他事件处理器
                // 可以考虑将失败的事件标记为需要重试
                await HandleEventProcessingErrorAsync(notification, ex);
            }
        }

        /// <summary>
        /// 保存事件到新的事件表 ff_events
        /// </summary>
        private async Task SaveToEventTableAsync(OnboardingStageCompletedEvent eventData)
        {
            try
            {
                // 构建事件元数据
                var eventMetadata = new
                {
                    RoutingTags = eventData.RoutingTags,
                    Priority = eventData.Priority,
                    Source = eventData.Source,
                    ProcessingInfo = new
                    {
                        ProcessedAt = DateTimeOffset.UtcNow,
                        ProcessedBy = _userContext?.UserName ?? "System",
                        ProcessorVersion = "1.0"
                    }
                };

                // 创建事件实体
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
                    NextRetryAt = null, // 显式设置为 null，因为不需要重试
                    MaxRetryCount = 3,
                    TenantId = eventData.TenantId
                };

                // 设置创建信息
                eventEntity.InitCreateInfo(_userContext);

                // 保存到数据库
                await _eventRepository.InsertAsync(eventEntity);

                _logger.LogDebug("事件已保存到 ff_events 表: {EventId}", eventData.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存事件到 ff_events 表时发生错误: {EventId}", eventData.EventId);
                throw;
            }
        }

        /// <summary>
        /// 发布 ActionTriggerEvent 以触发相关动作
        /// </summary>
        private async Task PublishActionTriggerEventAsync(OnboardingStageCompletedEvent eventData)
        {
            try
            {
                // 构建上下文数据
                var contextData = new
                {
                    OnboardingId = eventData.OnboardingId,
                    LeadId = eventData.LeadId,
                    WorkflowId = eventData.WorkflowId,
                    WorkflowName = eventData.WorkflowName,
                    CompletedStageId = eventData.CompletedStageId,
                    CompletedStageName = eventData.CompletedStageName,
                    NextStageId = eventData.NextStageId,
                    NextStageName = eventData.NextStageName,
                    CompletionRate = eventData.CompletionRate,
                    IsFinalStage = eventData.IsFinalStage,
                    BusinessContext = eventData.BusinessContext,
                    Components = eventData.Components,
                    TenantId = eventData.TenantId,
                    Source = eventData.Source,
                    Priority = eventData.Priority,
                    OriginalEventId = eventData.EventId
                };

                // 获取当前用户ID
                var currentUserId = GetCurrentUserId();

                // 创建并发布 ActionTriggerEvent
                var actionTriggerEvent = new ActionTriggerEvent(
                    triggerSourceType: "Stage",
                    triggerSourceId: eventData.CompletedStageId,
                    triggerEventType: "Completed",
                    contextData: contextData,
                    userId: currentUserId > 0 ? currentUserId : null
                );

                await _mediator.Publish(actionTriggerEvent);


                // 这里通过stage的compose获取checklist的task，question 对应的action，发送ActionTriggerEvent


                _logger.LogDebug("已发布 ActionTriggerEvent: SourceType={SourceType}, SourceId={SourceId}, EventType={EventType}, OriginalEventId={OriginalEventId}",
                    actionTriggerEvent.TriggerSourceType,
                    actionTriggerEvent.TriggerSourceId,
                    actionTriggerEvent.TriggerEventType,
                    eventData.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布 ActionTriggerEvent 时发生错误: {EventId}", eventData.EventId);
                throw;
            }
        }

        // Stage completion log functionality removed

        /// <summary>
        /// 处理事件处理错误
        /// </summary>
        private async Task HandleEventProcessingErrorAsync(OnboardingStageCompletedEvent eventData, Exception error)
        {
            try
            {
                // 尝试将失败的事件保存到事件表，标记为失败状态
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
                    NextRetryAt = DateTimeOffset.UtcNow.AddMinutes(5), // 5分钟后重试
                    TenantId = eventData.TenantId
                };

                failedEventEntity.InitCreateInfo(_userContext);
                await _eventRepository.InsertAsync(failedEventEntity);

                _logger.LogWarning("失败的事件已保存到 ff_events 表以供重试: {EventId}", failedEventEntity.EventId);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "保存失败事件时发生错误: {EventId}", eventData.EventId);
            }
        }

        /// <summary>
        /// 获取当前用户ID
        /// </summary>
        private long GetCurrentUserId()
        {
            if (long.TryParse(_userContext?.UserId, out long userId))
            {
                return userId;
            }
            return 0;
        }
    }
}
