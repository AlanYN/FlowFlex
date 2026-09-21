using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Events;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlowFlex.Application.Notification
{
    /// <summary>
    /// Listens to OnboardingStageCompletedEvent and fires the Workflow Trigger execution engine
    /// asynchronously when the Case reaches Completed state (all stages done or force-completed).
    ///
    /// Uses IServiceScopeFactory to avoid Transient → Scoped circular dependency.
    /// </summary>
    public class OnboardingTriggerHandler : INotificationHandler<OnboardingStageCompletedEvent>
    {
        private readonly IServiceScopeFactory    _scopeFactory;
        private readonly IBackgroundTaskQueue    _backgroundQueue;
        private readonly ILogger<OnboardingTriggerHandler> _logger;

        public OnboardingTriggerHandler(
            IServiceScopeFactory    scopeFactory,
            IBackgroundTaskQueue    backgroundQueue,
            ILogger<OnboardingTriggerHandler> logger)
        {
            _scopeFactory    = scopeFactory    ?? throw new ArgumentNullException(nameof(scopeFactory));
            _backgroundQueue = backgroundQueue ?? throw new ArgumentNullException(nameof(backgroundQueue));
            _logger          = logger          ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task Handle(OnboardingStageCompletedEvent notification, CancellationToken cancellationToken)
        {
            // Only fire when the Case is truly complete (last stage or all stages done)
            bool isCaseCompleted = notification.IsFinalStage || notification.CompletionRate >= 100;
            if (!isCaseCompleted)
                return Task.CompletedTask;

            _logger.LogInformation(
                "[TriggerHandler] Case completed — queuing trigger engine | OnboardingId={Id} WorkflowId={Wf}",
                notification.OnboardingId, notification.WorkflowId);

            // Snapshot event values for the background lambda (avoid closure capture of notification)
            var tenantId     = notification.TenantId;
            var appCode      = notification.AppCode ?? "default";
            var userId       = notification.UserId.ToString();
            var userName     = notification.UserName;
            var onboardingId = notification.OnboardingId;
            var workflowId   = notification.WorkflowId;

            _backgroundQueue.QueueBackgroundWorkItem(async token =>
            {
                try
                {
                    // Create a new DI scope — avoids Transient→Scoped circular dependency
                    using var scope = _scopeFactory.CreateScope();

                    // Restore user context so downstream services get correct tenant/user
                    var userCtx = scope.ServiceProvider.GetRequiredService<UserContext>();
                    if (!string.IsNullOrEmpty(tenantId)) userCtx.TenantId = tenantId;
                    if (!string.IsNullOrEmpty(userId))   userCtx.UserId   = userId;
                    if (!string.IsNullOrEmpty(userName))  userCtx.UserName = userName;

                    var triggerService = scope.ServiceProvider.GetRequiredService<ITriggerExecutionService>();
                    // Pass tenantId/appCode explicitly — background tasks have no HttpContext
                    await triggerService.ExecuteTriggersAsync(onboardingId, workflowId, "Completed", tenantId, appCode, userId, userName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[TriggerHandler] Background trigger execution failed | OnboardingId={Id}", onboardingId);
                }
            });

            return Task.CompletedTask;
        }
    }
}
