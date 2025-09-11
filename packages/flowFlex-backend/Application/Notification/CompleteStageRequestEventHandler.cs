using FlowFlex.Application.Contracts.Dtos.OW.Onboarding;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Shared.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowFlex.Application.Notification
{
    /// <summary>
    /// Handler for CompleteStageRequestEvent - handles stage completion requests
    /// </summary>
    public class CompleteStageRequestEventHandler : INotificationHandler<CompleteStageRequestEvent>
    {
        private readonly IOnboardingService _onboardingService;
        private readonly ILogger<CompleteStageRequestEventHandler> _logger;

        public CompleteStageRequestEventHandler(
            IOnboardingService onboardingService,
            ILogger<CompleteStageRequestEventHandler> logger)
        {
            _onboardingService = onboardingService;
            _logger = logger;
        }

        public async Task Handle(CompleteStageRequestEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("处理 CompleteStageRequest: OnboardingId={OnboardingId}, StageId={StageId}, Source={Source}, TriggerTaskId={TriggerTaskId}",
                    notification.OnboardingId, notification.StageId, notification.Source, notification.TriggerTaskId);

                // 构建完成 Stage 的输入参数 (使用带验证的完成方法)
                var completeCurrentStageInput = new CompleteCurrentStageInputDto
                {
                    StageId = notification.StageId, // 指定要完成的 Stage ID
                    CompletionNotes = notification.CompletionNotes,
                    ForceComplete = false // 不强制完成，会进行验证
                };

                // 调用 OnboardingService 完成指定 Stage (使用带验证的 complete-stage-with-validation API)
                var result = await _onboardingService.CompleteCurrentStageAsync(notification.OnboardingId, completeCurrentStageInput);

                if (result)
                {
                    _logger.LogInformation("成功完成 Stage: OnboardingId={OnboardingId}, StageId={StageId}, Source={Source}, TriggerTaskId={TriggerTaskId}, TriggerTaskName={TriggerTaskName}",
                        notification.OnboardingId, notification.StageId, notification.Source, notification.TriggerTaskId, notification.TriggerTaskName);
                }
                else
                {
                    _logger.LogWarning("完成 Stage 失败: OnboardingId={OnboardingId}, StageId={StageId}, Source={Source}, TriggerTaskId={TriggerTaskId}",
                        notification.OnboardingId, notification.StageId, notification.Source, notification.TriggerTaskId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理 CompleteStageRequest 时发生错误: OnboardingId={OnboardingId}, StageId={StageId}, Source={Source}, TriggerTaskId={TriggerTaskId}",
                    notification.OnboardingId, notification.StageId, notification.Source, notification.TriggerTaskId);
                // 不重新抛出异常，避免影响主业务流程
            }
        }
    }
}
