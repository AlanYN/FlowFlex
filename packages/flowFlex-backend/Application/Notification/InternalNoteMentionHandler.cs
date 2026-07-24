using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW;
using FlowFlex.Domain.Shared.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Notification
{
    /// <summary>
    /// 处理 Internal Note @mention 事件，解析 mentions 并发送邮件通知
    /// </summary>
    public class InternalNoteMentionHandler : INotificationHandler<InternalNoteMentionEvent>
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<InternalNoteMentionHandler> _logger;

        public InternalNoteMentionHandler(
            IEmailService emailService,
            ILogger<InternalNoteMentionHandler> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task Handle(InternalNoteMentionEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Processing mention notification for note {NoteId}, OnboardingId={OnboardingId}",
                    notification.NoteId, notification.OnboardingId);

                // 1. 解析当前 content 中的 mentions
                var currentMentions = MentionParser.ParseMentions(notification.Content);

                // 2. 如果是编辑场景，计算增量 diff
                if (!string.IsNullOrEmpty(notification.PreviousContent))
                {
                    var previousMentions = MentionParser.ParseMentions(notification.PreviousContent);
                    currentMentions = MentionParser.GetNewMentions(currentMentions, previousMentions);
                }

                if (currentMentions == null || currentMentions.Count == 0) return;

                // 3. 去重（按 Key GroupBy 取 First）
                var uniqueMentions = currentMentions
                    .GroupBy(m => m.Key)
                    .Select(g => g.First())
                    .ToList();

                // 4. 逐个发送通知（email 直接从 mention 中取，无需查库）
                var onboardingUrl = $"/onboard/onboardDetail?onboardingId={notification.OnboardingId}";
                // 将 mention 标记渲染为可读文本用于邮件内容
                var displayContent = MentionParser.RenderForDisplay(notification.Content);

                foreach (var mention in uniqueMentions)
                {
                    try
                    {
                        var email = mention.Email;

                        if (string.IsNullOrEmpty(email))
                        {
                            _logger.LogWarning(
                                "Could not resolve email for mention: {Value}", mention.Value);
                            continue;
                        }

                        await _emailService.SendMentionNotificationAsync(
                            email,
                            notification.SenderName,
                            notification.CaseName,
                            notification.CaseCode,
                            notification.StageName,
                            displayContent,
                            onboardingUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to send mention notification to {Email}", mention.Email);
                        // 单个 mention 失败不影响其他 mention
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing mention event for note {NoteId}",
                    notification.NoteId);
                // 不 throw —— 通知失败不应影响其他 handler 或调用方
            }
        }
    }
}
