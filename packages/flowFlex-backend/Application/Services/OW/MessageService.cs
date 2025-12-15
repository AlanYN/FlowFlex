using System.Text.Json;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Application.Contracts.Dtos.OW.Message;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Exceptions;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Services.OW;

/// <summary>
/// Message Center Service Implementation
/// Handles Internal Messages, Customer Emails (via Outlook), and Portal Messages
/// </summary>
public class MessageService : IMessageService, IScopedService
{
    private readonly IMessageRepository _messageRepository;
    private readonly IMessageAttachmentRepository _attachmentRepository;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserContext _userContext;
    private readonly IOperatorContextService _operatorContextService;

    public MessageService(
        IMessageRepository messageRepository,
        IMessageAttachmentRepository attachmentRepository,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        UserContext userContext,
        IOperatorContextService operatorContextService)
    {
        _messageRepository = messageRepository;
        _attachmentRepository = attachmentRepository;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        _userContext = userContext;
        _operatorContextService = operatorContextService;
    }

    #region Message CRUD

    /// <summary>
    /// Get paginated message list with filtering and sorting
    /// </summary>
    public async Task<PageModelDto<MessageListItemDto>> GetPagedAsync(MessageQueryDto query)
    {
        var ownerId = GetCurrentUserId();

        // Query local messages
        var (items, totalCount) = await _messageRepository.GetPagedByOwnerAsync(
            ownerId,
            query.PageIndex,
            query.PageSize,
            query.Folder,
            query.Label,
            query.MessageType,
            query.SearchTerm,
            query.RelatedEntityId,
            query.SortField,
            query.SortDirection);

        // Map to DTOs
        var dtos = items.Select(MapToListItemDto).ToList();

        return new PageModelDto<MessageListItemDto>(query.PageIndex, query.PageSize, dtos, totalCount);
    }

    /// <summary>
    /// Get message detail by ID, automatically marks as read
    /// </summary>
    public async Task<MessageDetailDto?> GetByIdAsync(long id)
    {
        var message = await _messageRepository.GetByIdAsync(id);
        if (message == null || !message.IsValid)
        {
            return null;
        }

        // Verify ownership
        var ownerId = GetCurrentUserId();
        if (message.OwnerId != ownerId)
        {
            throw new CRMException(ErrorCodeEnum.OperationNotAllowed, "You don't have permission to view this message");
        }

        // Auto-mark as read if unread
        if (!message.IsRead)
        {
            await _messageRepository.MarkAsReadAsync(id);
            message.IsRead = true;
        }

        // Get attachments
        var attachments = await _attachmentRepository.GetByMessageIdAsync(id);

        return MapToDetailDto(message, attachments);
    }


    /// <summary>
    /// Create and send a message
    /// </summary>
    public async Task<long> CreateAsync(MessageCreateDto input)
    {
        ValidateCreateInput(input);

        var ownerId = GetCurrentUserId();
        var senderName = _operatorContextService.GetOperatorDisplayName();
        var senderEmail = _userContext.Email ?? string.Empty;

        // Handle based on message type
        return input.MessageType switch
        {
            "Internal" => await CreateInternalMessageAsync(input, ownerId, senderName, senderEmail),
            "Email" => await CreateEmailMessageAsync(input, ownerId, senderName, senderEmail),
            "Portal" => await CreatePortalMessageAsync(input, ownerId, senderName, senderEmail),
            _ => throw new CRMException(ErrorCodeEnum.ParamInvalid, $"Invalid message type: {input.MessageType}")
        };
    }

    /// <summary>
    /// Update a draft message
    /// </summary>
    public async Task<bool> UpdateAsync(long id, MessageUpdateDto input)
    {
        var message = await _messageRepository.GetByIdAsync(id);
        if (message == null || !message.IsValid)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "Message not found");
        }

        // Verify ownership
        var ownerId = GetCurrentUserId();
        if (message.OwnerId != ownerId)
        {
            throw new CRMException(ErrorCodeEnum.OperationNotAllowed, "You don't have permission to update this message");
        }

        // Only drafts can be updated
        if (!message.IsDraft)
        {
            throw new CRMException(ErrorCodeEnum.OperationNotAllowed, "Only draft messages can be updated");
        }

        // Update fields
        if (input.Subject != null) message.Subject = input.Subject;
        if (input.Body != null)
        {
            message.Body = input.Body;
            message.BodyPreview = GetBodyPreview(input.Body);
        }
        if (input.Recipients != null) message.Recipients = JsonSerializer.Serialize(input.Recipients);
        if (input.CcRecipients != null) message.CcRecipients = JsonSerializer.Serialize(input.CcRecipients);
        if (input.BccRecipients != null) message.BccRecipients = JsonSerializer.Serialize(input.BccRecipients);

        message.InitUpdateInfo(_userContext);

        return await _messageRepository.UpdateAsync(message);
    }

    /// <summary>
    /// Delete message (move to Trash)
    /// </summary>
    public async Task<bool> DeleteAsync(long id)
    {
        var message = await _messageRepository.GetByIdAsync(id);
        if (message == null || !message.IsValid)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "Message not found");
        }

        // Verify ownership
        var ownerId = GetCurrentUserId();
        if (message.OwnerId != ownerId)
        {
            throw new CRMException(ErrorCodeEnum.OperationNotAllowed, "You don't have permission to delete this message");
        }

        // Move to Trash, save original folder for restore
        return await _messageRepository.MoveToFolderAsync(id, "Trash", message.Folder);
    }

    /// <summary>
    /// Permanently delete message
    /// </summary>
    public async Task<bool> PermanentDeleteAsync(long id)
    {
        var message = await _messageRepository.GetByIdAsync(id);
        if (message == null || !message.IsValid)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "Message not found");
        }

        // Verify ownership
        var ownerId = GetCurrentUserId();
        if (message.OwnerId != ownerId)
        {
            throw new CRMException(ErrorCodeEnum.OperationNotAllowed, "You don't have permission to delete this message");
        }

        // Delete attachments first
        await _attachmentRepository.DeleteByMessageIdAsync(id);

        // Permanently delete message
        return await _messageRepository.PermanentDeleteAsync(id);
    }

    /// <summary>
    /// Restore message from Trash
    /// </summary>
    public async Task<bool> RestoreAsync(long id)
    {
        var message = await _messageRepository.GetByIdAsync(id);
        if (message == null || !message.IsValid)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "Message not found");
        }

        // Verify ownership
        var ownerId = GetCurrentUserId();
        if (message.OwnerId != ownerId)
        {
            throw new CRMException(ErrorCodeEnum.OperationNotAllowed, "You don't have permission to restore this message");
        }

        // Only messages in Trash can be restored
        if (message.Folder != "Trash")
        {
            throw new CRMException(ErrorCodeEnum.OperationNotAllowed, "Only messages in Trash can be restored");
        }

        // Restore to original folder or Inbox
        var targetFolder = !string.IsNullOrEmpty(message.OriginalFolder) ? message.OriginalFolder : "Inbox";
        return await _messageRepository.MoveToFolderAsync(id, targetFolder);
    }

    #endregion


    #region Message Operations

    /// <summary>
    /// Star a message
    /// </summary>
    public async Task<bool> StarAsync(long id)
    {
        await VerifyMessageOwnership(id);
        return await _messageRepository.StarAsync(id);
    }

    /// <summary>
    /// Unstar a message
    /// </summary>
    public async Task<bool> UnstarAsync(long id)
    {
        await VerifyMessageOwnership(id);
        return await _messageRepository.UnstarAsync(id);
    }

    /// <summary>
    /// Archive a message
    /// </summary>
    public async Task<bool> ArchiveAsync(long id)
    {
        var message = await VerifyMessageOwnership(id);
        return await _messageRepository.MoveToFolderAsync(id, "Archive", message.Folder);
    }

    /// <summary>
    /// Mark message as read
    /// </summary>
    public async Task<bool> MarkAsReadAsync(long id)
    {
        await VerifyMessageOwnership(id);
        return await _messageRepository.MarkAsReadAsync(id);
    }

    /// <summary>
    /// Mark message as unread
    /// </summary>
    public async Task<bool> MarkAsUnreadAsync(long id)
    {
        await VerifyMessageOwnership(id);
        return await _messageRepository.MarkAsUnreadAsync(id);
    }

    #endregion

    #region Reply and Forward

    /// <summary>
    /// Reply to a message
    /// </summary>
    public async Task<long> ReplyAsync(long id, MessageReplyDto input)
    {
        var originalMessage = await VerifyMessageOwnership(id);

        var ownerId = GetCurrentUserId();
        var senderName = _operatorContextService.GetOperatorDisplayName();
        var senderEmail = _userContext.Email ?? string.Empty;

        // Create reply message
        var replyMessage = new Message
        {
            Subject = GetReplySubject(originalMessage.Subject),
            Body = input.Body,
            BodyPreview = GetBodyPreview(input.Body),
            MessageType = originalMessage.MessageType,
            Folder = "Sent",
            Labels = originalMessage.Labels,
            SenderId = ownerId,
            SenderName = senderName,
            SenderEmail = senderEmail,
            Recipients = JsonSerializer.Serialize(new List<RecipientDto>
            {
                new RecipientDto
                {
                    UserId = originalMessage.SenderId,
                    Name = originalMessage.SenderName,
                    Email = originalMessage.SenderEmail
                }
            }),
            RelatedEntityType = originalMessage.RelatedEntityType,
            RelatedEntityId = originalMessage.RelatedEntityId,
            RelatedEntityCode = originalMessage.RelatedEntityCode,
            ParentMessageId = id,
            ConversationId = originalMessage.ConversationId ?? originalMessage.Id.ToString(),
            OwnerId = ownerId,
            SentDate = DateTimeOffset.UtcNow,
            ReceivedDate = DateTimeOffset.UtcNow
        };

        replyMessage.InitCreateInfo(_userContext);
        await _messageRepository.InsertAsync(replyMessage);

        // Create copy in recipient's inbox (for Internal messages)
        if (originalMessage.MessageType == "Internal" && originalMessage.SenderId.HasValue)
        {
            await CreateRecipientCopyAsync(replyMessage, originalMessage.SenderId.Value);
        }

        return replyMessage.Id;
    }

    /// <summary>
    /// Forward a message
    /// </summary>
    public async Task<long> ForwardAsync(long id, MessageForwardDto input)
    {
        var originalMessage = await VerifyMessageOwnership(id);

        if (input.Recipients == null || !input.Recipients.Any())
        {
            throw new CRMException(ErrorCodeEnum.ParamInvalid, "At least one recipient is required");
        }

        var ownerId = GetCurrentUserId();
        var senderName = _operatorContextService.GetOperatorDisplayName();
        var senderEmail = _userContext.Email ?? string.Empty;

        // Create forward message
        var forwardMessage = new Message
        {
            Subject = GetForwardSubject(originalMessage.Subject),
            Body = BuildForwardBody(input.Body, originalMessage),
            BodyPreview = GetBodyPreview(input.Body),
            MessageType = originalMessage.MessageType,
            Folder = "Sent",
            Labels = originalMessage.Labels,
            SenderId = ownerId,
            SenderName = senderName,
            SenderEmail = senderEmail,
            Recipients = JsonSerializer.Serialize(input.Recipients),
            RelatedEntityType = originalMessage.RelatedEntityType,
            RelatedEntityId = originalMessage.RelatedEntityId,
            RelatedEntityCode = originalMessage.RelatedEntityCode,
            ParentMessageId = id,
            ConversationId = originalMessage.ConversationId ?? originalMessage.Id.ToString(),
            OwnerId = ownerId,
            SentDate = DateTimeOffset.UtcNow,
            ReceivedDate = DateTimeOffset.UtcNow
        };

        forwardMessage.InitCreateInfo(_userContext);
        await _messageRepository.InsertAsync(forwardMessage);

        // Create copies in recipients' inboxes (for Internal messages)
        if (originalMessage.MessageType == "Internal")
        {
            foreach (var recipient in input.Recipients.Where(r => r.UserId.HasValue))
            {
                await CreateRecipientCopyAsync(forwardMessage, recipient.UserId!.Value);
            }
        }

        return forwardMessage.Id;
    }

    #endregion


    #region Drafts

    /// <summary>
    /// Save message as draft
    /// </summary>
    public async Task<long> SaveDraftAsync(MessageCreateDto input)
    {
        var ownerId = GetCurrentUserId();
        var senderName = _operatorContextService.GetOperatorDisplayName();
        var senderEmail = _userContext.Email ?? string.Empty;

        var draft = new Message
        {
            Subject = input.Subject ?? string.Empty,
            Body = input.Body ?? string.Empty,
            BodyPreview = GetBodyPreview(input.Body ?? string.Empty),
            MessageType = input.MessageType,
            Folder = "Drafts",
            Labels = GetLabelsForMessageType(input.MessageType),
            SenderId = ownerId,
            SenderName = senderName,
            SenderEmail = senderEmail,
            Recipients = JsonSerializer.Serialize(input.Recipients ?? new List<RecipientDto>()),
            CcRecipients = JsonSerializer.Serialize(input.CcRecipients ?? new List<RecipientDto>()),
            BccRecipients = JsonSerializer.Serialize(input.BccRecipients ?? new List<RecipientDto>()),
            RelatedEntityType = input.RelatedEntityType,
            RelatedEntityId = input.RelatedEntityId,
            PortalId = input.PortalId,
            IsDraft = true,
            OwnerId = ownerId
        };

        draft.InitCreateInfo(_userContext);
        await _messageRepository.InsertAsync(draft);

        // Associate attachments
        if (input.AttachmentIds?.Any() == true)
        {
            await _attachmentRepository.AssociateWithMessageAsync(input.AttachmentIds, draft.Id);
        }

        return draft.Id;
    }

    /// <summary>
    /// Send a draft message
    /// </summary>
    public async Task<long> SendDraftAsync(long id)
    {
        var draft = await VerifyMessageOwnership(id);

        if (!draft.IsDraft)
        {
            throw new CRMException(ErrorCodeEnum.OperationNotAllowed, "Message is not a draft");
        }

        // Validate draft has required fields
        var recipients = JsonSerializer.Deserialize<List<RecipientDto>>(draft.Recipients ?? "[]") ?? new List<RecipientDto>();
        if (!recipients.Any())
        {
            throw new CRMException(ErrorCodeEnum.ParamInvalid, "At least one recipient is required");
        }

        if (string.IsNullOrWhiteSpace(draft.Subject))
        {
            throw new CRMException(ErrorCodeEnum.ParamInvalid, "Subject is required");
        }

        // Update draft to sent message
        draft.IsDraft = false;
        draft.Folder = "Sent";
        draft.SentDate = DateTimeOffset.UtcNow;
        draft.ReceivedDate = DateTimeOffset.UtcNow;
        draft.InitUpdateInfo(_userContext);

        await _messageRepository.UpdateAsync(draft);

        // Create copies in recipients' inboxes (for Internal messages)
        if (draft.MessageType == "Internal")
        {
            foreach (var recipient in recipients.Where(r => r.UserId.HasValue))
            {
                await CreateRecipientCopyAsync(draft, recipient.UserId!.Value);
            }
        }

        return draft.Id;
    }

    #endregion

    #region Statistics and Sync

    /// <summary>
    /// Get folder statistics
    /// </summary>
    public async Task<List<FolderStatsDto>> GetFolderStatsAsync()
    {
        var ownerId = GetCurrentUserId();
        var stats = await _messageRepository.GetFolderStatsAsync(ownerId);

        return stats.Select(kvp => new FolderStatsDto
        {
            Folder = kvp.Key,
            TotalCount = kvp.Value.total,
            UnreadCount = kvp.Value.unread,
            InternalCount = kvp.Value.internalCount,
            EmailCount = kvp.Value.emailCount,
            PortalCount = kvp.Value.portalCount,
            DraftCount = kvp.Key == "Drafts" ? kvp.Value.total : 0
        }).ToList();
    }

    /// <summary>
    /// Get unread message count for inbox
    /// </summary>
    public async Task<int> GetUnreadCountAsync()
    {
        var ownerId = GetCurrentUserId();
        return await _messageRepository.GetUnreadCountAsync(ownerId, "Inbox");
    }

    /// <summary>
    /// Manually trigger Outlook email sync
    /// TODO: Implement Outlook integration when OutlookClient is available
    /// </summary>
    public async Task<int> SyncOutlookEmailsAsync()
    {
        // Placeholder for Outlook sync implementation
        // Will be implemented when OutlookClient is integrated
        await Task.CompletedTask;
        return 0;
    }

    #endregion


    #region Private Helper Methods

    private long GetCurrentUserId()
    {
        if (string.IsNullOrEmpty(_userContext.UserId) || !long.TryParse(_userContext.UserId, out var userId))
        {
            throw new CRMException(ErrorCodeEnum.AuthenticationFail, "User not authenticated");
        }
        return userId;
    }

    private async Task<Message> VerifyMessageOwnership(long id)
    {
        var message = await _messageRepository.GetByIdAsync(id);
        if (message == null || !message.IsValid)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "Message not found");
        }

        var ownerId = GetCurrentUserId();
        if (message.OwnerId != ownerId)
        {
            throw new CRMException(ErrorCodeEnum.OperationNotAllowed, "You don't have permission to access this message");
        }

        return message;
    }

    private void ValidateCreateInput(MessageCreateDto input)
    {
        if (input.SaveAsDraft) return; // Drafts don't need full validation

        if (input.Recipients == null || !input.Recipients.Any())
        {
            throw new CRMException(ErrorCodeEnum.ParamInvalid, "At least one recipient is required");
        }

        if (string.IsNullOrWhiteSpace(input.Subject))
        {
            throw new CRMException(ErrorCodeEnum.ParamInvalid, "Subject is required");
        }

        if (string.IsNullOrWhiteSpace(input.Body))
        {
            throw new CRMException(ErrorCodeEnum.ParamInvalid, "Body is required");
        }
    }

    private async Task<long> CreateInternalMessageAsync(MessageCreateDto input, long ownerId, string senderName, string senderEmail)
    {
        var message = CreateBaseMessage(input, ownerId, senderName, senderEmail);
        message.Labels = "[\"Internal\"]";

        message.InitCreateInfo(_userContext);
        await _messageRepository.InsertAsync(message);

        // Associate attachments
        if (input.AttachmentIds?.Any() == true)
        {
            await _attachmentRepository.AssociateWithMessageAsync(input.AttachmentIds, message.Id);
            message.HasAttachments = true;
            await _messageRepository.UpdateAsync(message);
        }

        // Create copies in recipients' inboxes
        foreach (var recipient in input.Recipients.Where(r => r.UserId.HasValue))
        {
            await CreateRecipientCopyAsync(message, recipient.UserId!.Value);
        }

        return message.Id;
    }

    private async Task<long> CreateEmailMessageAsync(MessageCreateDto input, long ownerId, string senderName, string senderEmail)
    {
        // TODO: Implement Outlook integration when OutlookClient is available
        // For now, create local record only
        var message = CreateBaseMessage(input, ownerId, senderName, senderEmail);
        message.Labels = "[\"External\"]";

        message.InitCreateInfo(_userContext);
        await _messageRepository.InsertAsync(message);

        // Associate attachments
        if (input.AttachmentIds?.Any() == true)
        {
            await _attachmentRepository.AssociateWithMessageAsync(input.AttachmentIds, message.Id);
            message.HasAttachments = true;
            await _messageRepository.UpdateAsync(message);
        }

        return message.Id;
    }

    private async Task<long> CreatePortalMessageAsync(MessageCreateDto input, long ownerId, string senderName, string senderEmail)
    {
        var message = CreateBaseMessage(input, ownerId, senderName, senderEmail);
        message.Labels = "[\"Portal\"]";
        message.PortalId = input.PortalId;

        message.InitCreateInfo(_userContext);
        await _messageRepository.InsertAsync(message);

        // Associate attachments
        if (input.AttachmentIds?.Any() == true)
        {
            await _attachmentRepository.AssociateWithMessageAsync(input.AttachmentIds, message.Id);
            message.HasAttachments = true;
            await _messageRepository.UpdateAsync(message);
        }

        return message.Id;
    }

    private Message CreateBaseMessage(MessageCreateDto input, long ownerId, string senderName, string senderEmail)
    {
        return new Message
        {
            Subject = input.Subject,
            Body = input.Body,
            BodyPreview = GetBodyPreview(input.Body),
            MessageType = input.MessageType,
            Folder = input.SaveAsDraft ? "Drafts" : "Sent",
            SenderId = ownerId,
            SenderName = senderName,
            SenderEmail = senderEmail,
            Recipients = JsonSerializer.Serialize(input.Recipients),
            CcRecipients = JsonSerializer.Serialize(input.CcRecipients ?? new List<RecipientDto>()),
            BccRecipients = JsonSerializer.Serialize(input.BccRecipients ?? new List<RecipientDto>()),
            RelatedEntityType = input.RelatedEntityType,
            RelatedEntityId = input.RelatedEntityId,
            IsDraft = input.SaveAsDraft,
            OwnerId = ownerId,
            SentDate = input.SaveAsDraft ? null : DateTimeOffset.UtcNow,
            ReceivedDate = input.SaveAsDraft ? null : DateTimeOffset.UtcNow
        };
    }

    private async Task CreateRecipientCopyAsync(Message senderMessage, long recipientUserId)
    {
        var recipientCopy = new Message
        {
            Subject = senderMessage.Subject,
            Body = senderMessage.Body,
            BodyPreview = senderMessage.BodyPreview,
            MessageType = senderMessage.MessageType,
            Folder = "Inbox",
            Labels = senderMessage.Labels,
            SenderId = senderMessage.SenderId,
            SenderName = senderMessage.SenderName,
            SenderEmail = senderMessage.SenderEmail,
            Recipients = senderMessage.Recipients,
            CcRecipients = senderMessage.CcRecipients,
            BccRecipients = senderMessage.BccRecipients,
            RelatedEntityType = senderMessage.RelatedEntityType,
            RelatedEntityId = senderMessage.RelatedEntityId,
            RelatedEntityCode = senderMessage.RelatedEntityCode,
            ParentMessageId = senderMessage.ParentMessageId,
            ConversationId = senderMessage.ConversationId ?? senderMessage.Id.ToString(),
            PortalId = senderMessage.PortalId,
            HasAttachments = senderMessage.HasAttachments,
            OwnerId = recipientUserId,
            IsRead = false,
            SentDate = senderMessage.SentDate,
            ReceivedDate = DateTimeOffset.UtcNow
        };

        recipientCopy.InitCreateInfo(_userContext);
        await _messageRepository.InsertAsync(recipientCopy);
    }

    private static string GetBodyPreview(string body)
    {
        if (string.IsNullOrEmpty(body)) return string.Empty;

        // Strip HTML tags for preview
        var text = System.Text.RegularExpressions.Regex.Replace(body, "<[^>]*>", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

        return text.Length > 200 ? text.Substring(0, 200) + "..." : text;
    }

    private static string GetLabelsForMessageType(string messageType)
    {
        return messageType switch
        {
            "Internal" => "[\"Internal\"]",
            "Email" => "[\"External\"]",
            "Portal" => "[\"Portal\"]",
            _ => "[]"
        };
    }

    private static string GetReplySubject(string originalSubject)
    {
        if (string.IsNullOrEmpty(originalSubject)) return "RE: ";
        if (originalSubject.StartsWith("RE:", StringComparison.OrdinalIgnoreCase)) return originalSubject;
        return $"RE: {originalSubject}";
    }

    private static string GetForwardSubject(string originalSubject)
    {
        if (string.IsNullOrEmpty(originalSubject)) return "FW: ";
        if (originalSubject.StartsWith("FW:", StringComparison.OrdinalIgnoreCase)) return originalSubject;
        return $"FW: {originalSubject}";
    }

    private static string BuildForwardBody(string? additionalBody, Message originalMessage)
    {
        var forwardHeader = $@"
<br/><br/>
---------- Forwarded message ----------<br/>
From: {originalMessage.SenderName} &lt;{originalMessage.SenderEmail}&gt;<br/>
Date: {originalMessage.SentDate:yyyy-MM-dd HH:mm}<br/>
Subject: {originalMessage.Subject}<br/>
<br/>
";
        return (additionalBody ?? string.Empty) + forwardHeader + originalMessage.Body;
    }

    private MessageListItemDto MapToListItemDto(Message message)
    {
        return new MessageListItemDto
        {
            Id = message.Id,
            Subject = message.Subject,
            BodyPreview = message.BodyPreview,
            SenderName = message.SenderName,
            SenderEmail = message.SenderEmail,
            MessageType = message.MessageType,
            Labels = ParseLabels(message.Labels),
            RelatedEntityCode = message.RelatedEntityCode,
            IsRead = message.IsRead,
            IsStarred = message.IsStarred,
            HasAttachments = message.HasAttachments,
            ReceivedDate = message.ReceivedDate,
            SentDate = message.SentDate
        };
    }

    private MessageDetailDto MapToDetailDto(Message message, List<MessageAttachment> attachments)
    {
        return new MessageDetailDto
        {
            Id = message.Id,
            Subject = message.Subject,
            Body = message.Body,
            BodyPreview = message.BodyPreview,
            SenderName = message.SenderName,
            SenderEmail = message.SenderEmail,
            MessageType = message.MessageType,
            Labels = ParseLabels(message.Labels),
            RelatedEntityCode = message.RelatedEntityCode,
            RelatedEntityType = message.RelatedEntityType,
            RelatedEntityId = message.RelatedEntityId,
            IsRead = message.IsRead,
            IsStarred = message.IsStarred,
            HasAttachments = message.HasAttachments,
            ReceivedDate = message.ReceivedDate,
            SentDate = message.SentDate,
            Folder = message.Folder,
            IsDraft = message.IsDraft,
            ParentMessageId = message.ParentMessageId,
            ConversationId = message.ConversationId,
            Recipients = ParseRecipients(message.Recipients),
            CcRecipients = ParseRecipients(message.CcRecipients),
            BccRecipients = ParseRecipients(message.BccRecipients),
            Attachments = attachments.Select(a => new MessageAttachmentDto
            {
                Id = a.Id,
                FileName = a.FileName,
                ContentType = a.ContentType,
                FileSize = a.FileSize
            }).ToList()
        };
    }

    private static List<string> ParseLabels(string? labelsJson)
    {
        if (string.IsNullOrEmpty(labelsJson)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(labelsJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static List<RecipientDto> ParseRecipients(string? recipientsJson)
    {
        if (string.IsNullOrEmpty(recipientsJson)) return new List<RecipientDto>();
        try
        {
            return JsonSerializer.Deserialize<List<RecipientDto>>(recipientsJson) ?? new List<RecipientDto>();
        }
        catch
        {
            return new List<RecipientDto>();
        }
    }

    #endregion
}
