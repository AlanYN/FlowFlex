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
using FlowFlex.Application.Contracts.IServices.OW;

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
        private readonly IStageService _stageService;
        private readonly IChecklistService _checklistService;
        private readonly IQuestionnaireService _questionnaireService;
        private readonly IChecklistTaskCompletionService _checklistTaskCompletionService;
        private readonly IQuestionnaireAnswerService _questionnaireAnswerService;

        public OnboardingStageCompletedLogHandler(
            ILogger<OnboardingStageCompletedLogHandler> logger,
            IMediator mediator,
            IEventRepository eventRepository,
            UserContext userContext,
            IStageService stageService,
            IChecklistService checklistService,
            IQuestionnaireService questionnaireService,
            IChecklistTaskCompletionService checklistTaskCompletionService,
            IQuestionnaireAnswerService questionnaireAnswerService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _stageService = stageService ?? throw new ArgumentNullException(nameof(stageService));
            _checklistService = checklistService ?? throw new ArgumentNullException(nameof(checklistService));
            _questionnaireService = questionnaireService ?? throw new ArgumentNullException(nameof(questionnaireService));
            _checklistTaskCompletionService = checklistTaskCompletionService ?? throw new ArgumentNullException(nameof(checklistTaskCompletionService));
            _questionnaireAnswerService = questionnaireAnswerService ?? throw new ArgumentNullException(nameof(questionnaireAnswerService));
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
                    triggerSourceType: "",
                    triggerSourceId: eventData.CompletedStageId,
                    triggerEventType: "Completed",
                    contextData: contextData,
                    userId: currentUserId > 0 ? currentUserId : null
                );

                await _mediator.Publish(actionTriggerEvent);

                // 通过stage的compose获取checklist的task，question 对应的action，发送ActionTriggerEvent
                await PublishComponentActionTriggerEventsAsync(eventData, contextData, currentUserId);


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
        /// 发布组件相关的 ActionTriggerEvent（为 checklist task 和 questionnaire question 发布事件）
        /// </summary>
        private async Task PublishComponentActionTriggerEventsAsync(OnboardingStageCompletedEvent eventData, object contextData, long currentUserId)
        {
            try
            {
                _logger.LogDebug("开始发布组件相关的 ActionTriggerEvent: StageId={StageId}", eventData.CompletedStageId);

                // 获取 stage components
                var stageComponents = await _stageService.GetComponentsAsync(eventData.CompletedStageId);
                
                if (stageComponents == null || !stageComponents.Any())
                {
                    _logger.LogDebug("Stage {StageId} 没有配置组件", eventData.CompletedStageId);
                    return;
                }

                _logger.LogDebug("Stage {StageId} 找到 {Count} 个组件", eventData.CompletedStageId, stageComponents.Count);

                // 处理 checklist 组件
                await ProcessChecklistComponentsAsync(eventData, contextData, currentUserId, stageComponents);

                // 处理 questionnaire 组件
                await ProcessQuestionnaireComponentsAsync(eventData, contextData, currentUserId, stageComponents);

                _logger.LogDebug("完成发布组件相关的 ActionTriggerEvent: StageId={StageId}", eventData.CompletedStageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布组件相关的 ActionTriggerEvent 时发生错误: StageId={StageId}", eventData.CompletedStageId);
                // 不重新抛出异常，避免影响主流程
            }
        }

        /// <summary>
        /// 处理 checklist 组件，为每个 task 发布 ActionTriggerEvent
        /// </summary>
        private async Task ProcessChecklistComponentsAsync(OnboardingStageCompletedEvent eventData, object contextData, long currentUserId, List<StageComponent> stageComponents)
        {
            try
            {
                // 获取所有 checklist 组件
                var checklistComponents = stageComponents.Where(c => c.Key == "checklist").ToList();
                
                if (!checklistComponents.Any())
                {
                    _logger.LogDebug("Stage {StageId} 没有 checklist 组件", eventData.CompletedStageId);
                    return;
                }

                // 收集所有 checklist IDs
                var checklistIds = checklistComponents
                    .SelectMany(c => c.ChecklistIds ?? new List<long>())
                    .Distinct()
                    .ToList();

                if (!checklistIds.Any())
                {
                    _logger.LogDebug("Stage {StageId} 的 checklist 组件没有配置 checklist IDs", eventData.CompletedStageId);
                    return;
                }

                _logger.LogDebug("Stage {StageId} 找到 {Count} 个 checklist IDs: [{ChecklistIds}]", 
                    eventData.CompletedStageId, checklistIds.Count, string.Join(", ", checklistIds));

                // 批量获取 checklists
                var checklists = await _checklistService.GetByIdsAsync(checklistIds);
                
                // 获取当前 onboarding 和 stage 的所有任务完成状态
                var taskCompletions = await _checklistTaskCompletionService.GetByOnboardingAndStageAsync(eventData.OnboardingId, eventData.CompletedStageId);
                var taskCompletionMap = taskCompletions.ToDictionary(tc => tc.TaskId, tc => tc.IsCompleted);
                
                foreach (var checklist in checklists)
                {
                    if (checklist.Tasks != null && checklist.Tasks.Any())
                    {
                        foreach (var task in checklist.Tasks)
                        {
                            // 只为有 ActionId 的 task 创建 ActionTriggerEvent
                            if (!task.ActionId.HasValue)
                            {
                                _logger.LogDebug("Task {TaskId} ({TaskName}) 没有配置 ActionId，跳过发布 ActionTriggerEvent", 
                                    task.Id, task.Name);
                                continue;
                            }

                            // 检查 task 是否已完成
                            var isCompleted = taskCompletionMap.ContainsKey(task.Id) && taskCompletionMap[task.Id];
                            if (!isCompleted)
                            {
                                _logger.LogDebug("Task {TaskId} ({TaskName}) 尚未完成，跳过发布 ActionTriggerEvent", 
                                    task.Id, task.Name);
                                continue;
                            }

                            // 为每个 task 创建 ActionTriggerEvent
                            var taskContextData = new
                            {
                                // 包含原始上下文数据
                                ((dynamic)contextData).OnboardingId,
                                ((dynamic)contextData).LeadId,
                                ((dynamic)contextData).WorkflowId,
                                ((dynamic)contextData).WorkflowName,
                                ((dynamic)contextData).CompletedStageId,
                                ((dynamic)contextData).CompletedStageName,
                                ((dynamic)contextData).NextStageId,
                                ((dynamic)contextData).NextStageName,
                                ((dynamic)contextData).CompletionRate,
                                ((dynamic)contextData).IsFinalStage,
                                ((dynamic)contextData).BusinessContext,
                                ((dynamic)contextData).Components,
                                ((dynamic)contextData).TenantId,
                                ((dynamic)contextData).Source,
                                ((dynamic)contextData).Priority,
                                ((dynamic)contextData).OriginalEventId,
                                
                                // 添加 task 相关的上下文数据
                                ChecklistId = checklist.Id,
                                ChecklistName = checklist.Name,
                                TaskId = task.Id,
                                TaskName = task.Name,
                                TaskType = task.TaskType,
                                TaskIsRequired = task.IsRequired,
                                TaskPriority = task.Priority,
                                TaskAssigneeId = task.AssigneeId,
                                TaskAssigneeName = task.AssigneeName,
                                TaskAssignedTeam = task.AssignedTeam,
                                TaskActionId = task.ActionId,
                                TaskActionName = task.ActionName
                            };

                            var taskActionTriggerEvent = new ActionTriggerEvent(
                                triggerSourceType: "",
                                triggerSourceId: task.Id,
                                triggerEventType: "Completed",
                                contextData: taskContextData,
                                userId: currentUserId > 0 ? currentUserId : null
                            );

                            await _mediator.Publish(taskActionTriggerEvent);

                            _logger.LogDebug("已发布 Task ActionTriggerEvent: TaskId={TaskId}, TaskName={TaskName}, ChecklistId={ChecklistId}, StageId={StageId}",
                                task.Id, task.Name, checklist.Id, eventData.CompletedStageId);
                        }
                    }
                }

                _logger.LogDebug("完成处理 checklist 组件: StageId={StageId}, ChecklistCount={ChecklistCount}", 
                    eventData.CompletedStageId, checklists.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理 checklist 组件时发生错误: StageId={StageId}", eventData.CompletedStageId);
            }
        }

        /// <summary>
        /// 处理 questionnaire 组件，为每个 question 发布 ActionTriggerEvent
        /// </summary>
        private async Task ProcessQuestionnaireComponentsAsync(OnboardingStageCompletedEvent eventData, object contextData, long currentUserId, List<StageComponent> stageComponents)
        {
            try
            {
                // 获取所有 questionnaire 组件
                var questionnaireComponents = stageComponents.Where(c => c.Key == "questionnaires").ToList();
                
                if (!questionnaireComponents.Any())
                {
                    _logger.LogDebug("Stage {StageId} 没有 questionnaire 组件", eventData.CompletedStageId);
                    return;
                }

                // 收集所有 questionnaire IDs
                var questionnaireIds = questionnaireComponents
                    .SelectMany(c => c.QuestionnaireIds ?? new List<long>())
                    .Distinct()
                    .ToList();

                if (!questionnaireIds.Any())
                {
                    _logger.LogDebug("Stage {StageId} 的 questionnaire 组件没有配置 questionnaire IDs", eventData.CompletedStageId);
                    return;
                }

                _logger.LogDebug("Stage {StageId} 找到 {Count} 个 questionnaire IDs: [{QuestionnaireIds}]", 
                    eventData.CompletedStageId, questionnaireIds.Count, string.Join(", ", questionnaireIds));

                // 批量获取 questionnaires
                var questionnaires = await _questionnaireService.GetByIdsAsync(questionnaireIds);
                
                foreach (var questionnaire in questionnaires)
                {
                    // 解析 StructureJson 获取实际的 sections 和 questions
                    if (string.IsNullOrWhiteSpace(questionnaire.StructureJson))
                    {
                        _logger.LogDebug("Questionnaire {QuestionnaireId} ({QuestionnaireName}) 没有 StructureJson 数据，跳过处理", 
                            questionnaire.Id, questionnaire.Name);
                        continue;
                    }

                    // 获取当前 onboarding 的 questionnaire answers
                    var questionnaireAnswer = await _questionnaireAnswerService.GetAnswerAsync(eventData.OnboardingId, eventData.CompletedStageId);
                    if (questionnaireAnswer?.AnswerJson == null)
                    {
                        _logger.LogDebug("Questionnaire {QuestionnaireId} 在 Onboarding {OnboardingId} Stage {StageId} 中没有答案数据，跳过处理", 
                            questionnaire.Id, eventData.OnboardingId, eventData.CompletedStageId);
                        continue;
                    }

                    try
                    {
                        var structureData = JsonSerializer.Deserialize<QuestionnaireStructure>(questionnaire.StructureJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (structureData?.Sections == null || !structureData.Sections.Any())
                        {
                            _logger.LogDebug("Questionnaire {QuestionnaireId} ({QuestionnaireName}) 的 StructureJson 中没有 sections，跳过处理", 
                                questionnaire.Id, questionnaire.Name);
                            continue;
                        }

                        foreach (var section in structureData.Sections)
                        {
                            // 合并 items 和 questions 列表（有些 questionnaire 使用 items，有些使用 questions）
                            var allQuestions = new List<QuestionnaireQuestion>();
                            if (section.Items != null && section.Items.Any())
                            {
                                allQuestions.AddRange(section.Items);
                            }
                            if (section.Questions != null && section.Questions.Any())
                            {
                                allQuestions.AddRange(section.Questions);
                            }

                            // 去重（以防同一个 question 同时在 items 和 questions 中）
                            allQuestions = allQuestions.GroupBy(q => q.Id).Select(g => g.First()).ToList();

                            if (allQuestions.Any())
                            {
                                foreach (var question in allQuestions)
                                {
                                    // 只为有 Action 的 question 创建 ActionTriggerEvent
                                    if (question.Action == null || string.IsNullOrWhiteSpace(question.Action.Id))
                                    {
                                        _logger.LogDebug("Question {QuestionId} ({QuestionTitle}) 没有配置 Action，跳过发布 ActionTriggerEvent", 
                                            question.Id, question.Title ?? question.Question);
                                        continue;
                                    }

                                    // 检查 question 是否已回答
                                    var isAnswered = CheckIfQuestionIsAnswered(questionnaireAnswer.AnswerJson, question.Id);
                                    if (!isAnswered)
                                    {
                                        _logger.LogDebug("Question {QuestionId} ({QuestionTitle}) 尚未回答，跳过发布 ActionTriggerEvent", 
                                            question.Id, question.Title ?? question.Question);
                                        continue;
                                    }
                                    // 为每个 question 创建 ActionTriggerEvent
                                    var questionContextData = new
                                    {
                                        // 包含原始上下文数据
                                        ((dynamic)contextData).OnboardingId,
                                        ((dynamic)contextData).LeadId,
                                        ((dynamic)contextData).WorkflowId,
                                        ((dynamic)contextData).WorkflowName,
                                        ((dynamic)contextData).CompletedStageId,
                                        ((dynamic)contextData).CompletedStageName,
                                        ((dynamic)contextData).NextStageId,
                                        ((dynamic)contextData).NextStageName,
                                        ((dynamic)contextData).CompletionRate,
                                        ((dynamic)contextData).IsFinalStage,
                                        ((dynamic)contextData).BusinessContext,
                                        ((dynamic)contextData).Components,
                                        ((dynamic)contextData).TenantId,
                                        ((dynamic)contextData).Source,
                                        ((dynamic)contextData).Priority,
                                        ((dynamic)contextData).OriginalEventId,
                                        
                                        // 添加 question 相关的上下文数据
                                        QuestionnaireId = questionnaire.Id,
                                        QuestionnaireName = questionnaire.Name,
                                        QuestionnaireCategory = questionnaire.Category,
                                        QuestionnaireVersion = questionnaire.Version,
                                        SectionId = section.Id,
                                        SectionName = section.Name,
                                        SectionTitle = section.Title,
                                        SectionDescription = section.Description,
                                        SectionOrder = section.Order,
                                        QuestionId = question.Id,
                                        QuestionTitle = question.Title,
                                        QuestionText = question.Question,
                                        QuestionType = question.Type,
                                        QuestionIsRequired = question.Required,
                                        QuestionOrder = question.Order,
                                        QuestionActionId = question.Action.Id,
                                        QuestionActionName = question.Action.Name
                                    };

                                    var questionActionTriggerEvent = new ActionTriggerEvent(
                                        triggerSourceType: "",
                                        triggerSourceId: long.TryParse(question.Id, out long questionIdLong) ? questionIdLong : 0,
                                        triggerEventType: "Completed",
                                        contextData: questionContextData,
                                        userId: currentUserId > 0 ? currentUserId : null
                                    );

                                    await _mediator.Publish(questionActionTriggerEvent);

                                    _logger.LogDebug("已发布 Question ActionTriggerEvent: QuestionId={QuestionId}, QuestionTitle={QuestionTitle}, QuestionActionId={QuestionActionId}, QuestionnaireId={QuestionnaireId}, StageId={StageId}",
                                        question.Id, question.Title ?? question.Question, question.Action.Id, questionnaire.Id, eventData.CompletedStageId);
                                }
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "解析 Questionnaire {QuestionnaireId} 的 StructureJson 时发生错误", questionnaire.Id);
                    }
                }

                _logger.LogDebug("完成处理 questionnaire 组件: StageId={StageId}, QuestionnaireCount={QuestionnaireCount}", 
                    eventData.CompletedStageId, questionnaires.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理 questionnaire 组件时发生错误: StageId={StageId}", eventData.CompletedStageId);
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

        /// <summary>
        /// 检查 question 是否已回答
        /// </summary>
        private bool CheckIfQuestionIsAnswered(string answerJson, string questionId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(answerJson) || string.IsNullOrWhiteSpace(questionId))
                {
                    return false;
                }

                using var answersDoc = JsonDocument.Parse(answerJson);
                var answersRoot = answersDoc.RootElement;

                // 检查是否有 responses 数组结构
                if (answersRoot.TryGetProperty("responses", out var responsesElement) && responsesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var responseElement in responsesElement.EnumerateArray())
                    {
                        if (responseElement.TryGetProperty("questionId", out var qIdElement))
                        {
                            var responseQuestionId = qIdElement.ValueKind == JsonValueKind.String ? qIdElement.GetString() : qIdElement.ToString();
                            if (responseQuestionId == questionId)
                            {
                                // 找到匹配的 question，检查是否有答案
                                if (responseElement.TryGetProperty("answer", out var answerElement))
                                {
                                    var answer = answerElement.GetString();
                                    return !string.IsNullOrEmpty(answer);
                                }
                                break;
                            }
                        }
                    }
                }
                else
                {
                    // 检查直接属性结构 (question ID 作为属性名)
                    if (answersRoot.TryGetProperty(questionId, out var answerProperty))
                    {
                        var answer = answerProperty.GetString();
                        return !string.IsNullOrEmpty(answer);
                    }
                }

                return false;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "解析 answerJson 时发生错误: QuestionId={QuestionId}", questionId);
                return false;
            }
        }
    }

    /// <summary>
    /// Questionnaire Structure JSON 数据模型
    /// </summary>
    public class QuestionnaireStructure
    {
        public List<QuestionnaireSection> Sections { get; set; } = new List<QuestionnaireSection>();
    }

    /// <summary>
    /// Questionnaire Section 数据模型
    /// </summary>
    public class QuestionnaireSection
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
        public string TemporaryId { get; set; }
        public List<QuestionnaireQuestion> Items { get; set; } = new List<QuestionnaireQuestion>();
        public List<QuestionnaireQuestion> Questions { get; set; } = new List<QuestionnaireQuestion>();
    }

    /// <summary>
    /// Questionnaire Question 数据模型
    /// </summary>
    public class QuestionnaireQuestion
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public int Order { get; set; }
        public string Title { get; set; }
        public string Question { get; set; }
        public string Description { get; set; }
        public string TemporaryId { get; set; }
        public bool Required { get; set; }
        public List<object> Options { get; set; } = new List<object>();
        public List<object> Rows { get; set; } = new List<object>();
        public List<object> Columns { get; set; } = new List<object>();
        public List<object> JumpRules { get; set; } = new List<object>();
        public QuestionAction Action { get; set; }
        public object QuestionProps { get; set; }
        public string IconType { get; set; }
        public string MaxLabel { get; set; }
        public string MinLabel { get; set; }
        public bool RequireOneResponsePerRow { get; set; }
    }

    /// <summary>
    /// Question Action 数据模型
    /// </summary>
    public class QuestionAction
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
