using System.Text.Json;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Application.Contracts.Dtos.OW.Message;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums;
using FlowFlex.Domain.Shared.Models;
using MessageType = FlowFlex.Domain.Shared.Enums.MessageType;

namespace FlowFlex.Application.Services.MessageCenter;

/// <summary>
/// Message Center Service Implementation
/// Handles Internal Messages, Customer Emails (via Outlook), and Portal Messages
/// </summary>
public class MessageService : IMessageService, IScopedService
{
    private readonly IMessageRepository _messageRepository;
    private readonly IMessageAttachmentRepository _attachmentRepository;
    private readonly IEmailBindingRepository _emailBindingRepository;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserContext _userContext;
    private readonly IOperatorContextService _operatorContextService;
    private readonly IOutlookService _outlookService;
    private readonly IEmailBindingService _emailBindingService;
    private readonly ILogger<MessageService> _logger;
    private readonly IEncryptionService _encryptionService;

    public MessageService(
        IMessageRepository messageRepository,
        IMessageAttachmentRepository attachmentRepository,
        IEmailBindingRepository emailBindingRepository,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor,
        UserContext userContext,
        IOperatorContextService operatorContextService,
        IOutlookService outlookService,
        IEmailBindingService emailBindingService,
        ILogger<MessageService> logger,
        IEncryptionService encryptionService)
    {
        _messageRepository = messageRepository;
        _attachmentRepository = attachmentRepository;
        _emailBindingRepository = emailBindingRepository;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
        _userContext = userContext;
        _operatorContextService = operatorContextService;
        _outlookService = outlookService;
        _emailBindingService = emailBindingService;
        _logger = logger;
        _encryptionService = encryptionService;
    }

    #region Message CRUD

    /// <summary>
    /// Get paginated message list with filtering and sorting
    /// Automatically syncs Outlook emails using EmailBindingService
    /// </summary>
    public async Task<PageModelDto<MessageListItemDto>> GetPagedAsync(MessageQueryDto query)
    {
        var ownerId = GetCurrentUserId();

        // Normalize folder name to match database storage (e.g., "sent" -> "Sent")
        var normalizedFolder = NormalizeFolderName(query.Folder);

        // Try to sync Outlook emails silently (non-blocking, errors are logged but not thrown)
        await TrySyncEmailsAsync();

        // Query local messages
        var (items, totalCount) = await _messageRepository.GetPagedByOwnerAsync(
            ownerId,
            query.PageIndex,
            query.PageSize,
            normalizedFolder,
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
    /// Normalize folder name to match database storage format
    /// </summary>
    private static string? NormalizeFolderName(string? folder)
    {
        if (string.IsNullOrEmpty(folder)) return null;

        return folder.ToLower() switch
        {
            "inbox" => "Inbox",
            "sent" => "Sent",
            "drafts" => "Drafts",
            "trash" => "Trash",
            "archive" => "Archive",
            "starred" => "Starred",
            _ => folder
        };
    }

    /// <summary>
    /// Try to sync Outlook emails silently using EmailBindingService
    /// Errors are logged but not thrown to avoid blocking message list queries
    /// </summary>
    private async Task TrySyncEmailsAsync()
    {
        try
        {
            await _emailBindingService.SyncEmailsAsync();
        }
        catch (Exception ex)
        {
            // Silently ignore sync errors - user can still see local messages
            _logger.LogDebug(ex, "Email sync skipped or failed (this is normal if user has no binding)");
        }
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

        // For Email type with empty body, fetch full content from Outlook
        if (message.MessageType == "Email" && string.IsNullOrEmpty(message.Body) && !string.IsNullOrEmpty(message.ExternalMessageId))
        {
            await TryFetchEmailBodyFromOutlookAsync(message);
        }

        // Process cid: references in email body (for previously synced emails with inline images)
        if (message.MessageType == "Email" && !string.IsNullOrEmpty(message.Body) &&
            message.Body.Contains("cid:") && !string.IsNullOrEmpty(message.ExternalMessageId))
        {
            await TryProcessCidReferencesAsync(message);
        }

        // Auto-mark as read if unread
        if (!message.IsRead)
        {
            // Sync read status to Outlook if this is an external email
            if (message.MessageType == "Email" && !string.IsNullOrEmpty(message.ExternalMessageId))
            {
                await TrySyncReadStatusToOutlookAsync(message, true);
            }

            await _messageRepository.MarkAsReadAsync(id);
            message.IsRead = true;
        }

        // Get attachments
        var attachments = await _attachmentRepository.GetByMessageIdAsync(id);

        // Sync attachments if email has attachments but none are in local database
        if (message.MessageType == "Email" && message.HasAttachments &&
            attachments.Count == 0 && !string.IsNullOrEmpty(message.ExternalMessageId))
        {
            await TrySyncAttachmentsAsync(message);
            // Reload attachments after sync
            attachments = await _attachmentRepository.GetByMessageIdAsync(id);
        }

        return MapToDetailDto(message, attachments);
    }

    /// <summary>
    /// Try to fetch email body from Outlook and update local cache
    /// </summary>
    private async Task TryFetchEmailBodyFromOutlookAsync(Message message)
    {
        try
        {
            var binding = await _emailBindingRepository.GetByUserIdAndProviderAsync(message.OwnerId, "Outlook");
            if (binding == null || string.IsNullOrEmpty(binding.AccessToken)) return;

            // Decrypt access token
            var accessToken = DecryptToken(binding.AccessToken);

            // Refresh token if needed
            if (binding.TokenExpireTime <= DateTimeOffset.UtcNow.AddMinutes(5))
            {
                if (string.IsNullOrEmpty(binding.RefreshToken)) return;
                var refreshToken = DecryptToken(binding.RefreshToken);
                var newToken = await _outlookService.RefreshTokenAsync(refreshToken);
                if (newToken == null) return;
                // Encrypt new tokens before storing
                await _emailBindingRepository.UpdateTokenAsync(binding.Id, 
                    EncryptToken(newToken.AccessToken), 
                    EncryptToken(newToken.RefreshToken), 
                    newToken.ExpiresAt);
                accessToken = newToken.AccessToken;
            }

            // Fetch full email from Outlook
            var outlookEmail = await _outlookService.GetEmailByIdAsync(accessToken, message.ExternalMessageId);
            if (outlookEmail != null && !string.IsNullOrEmpty(outlookEmail.Body))
            {
                // Update local cache with full body
                message.Body = outlookEmail.Body;
                await _messageRepository.UpdateAsync(message);
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail - user can still see preview
            _logger.LogDebug(ex, "Failed to fetch full email body for message {MessageId}", message.Id);
        }
    }

    /// <summary>
    /// Try to process cid: references in email body by replacing them with base64 data URIs
    /// Used for emails that were previously synced without inline image processing
    /// </summary>
    private async Task TryProcessCidReferencesAsync(Message message)
    {
        try
        {
            var binding = await _emailBindingRepository.GetByUserIdAndProviderAsync(message.OwnerId, "Outlook");
            if (binding == null || string.IsNullOrEmpty(binding.AccessToken)) return;

            // Decrypt access token
            var accessToken = DecryptToken(binding.AccessToken);

            // Refresh token if needed
            if (binding.TokenExpireTime <= DateTimeOffset.UtcNow.AddMinutes(5))
            {
                if (string.IsNullOrEmpty(binding.RefreshToken)) return;
                var refreshToken = DecryptToken(binding.RefreshToken);
                var newToken = await _outlookService.RefreshTokenAsync(refreshToken);
                if (newToken == null) return;
                // Encrypt new tokens before storing
                await _emailBindingRepository.UpdateTokenAsync(binding.Id, 
                    EncryptToken(newToken.AccessToken), 
                    EncryptToken(newToken.RefreshToken), 
                    newToken.ExpiresAt);
                accessToken = newToken.AccessToken;
            }

            // Process cid: references
            var processedBody = await _outlookService.ProcessCidReferencesAsync(
                accessToken,
                message.ExternalMessageId!,
                message.Body);

            if (processedBody != message.Body)
            {
                // Update local cache with processed body
                message.Body = processedBody;
                await _messageRepository.UpdateAsync(message);
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail - user can still see the email with broken images
            _logger.LogDebug(ex, "Failed to process cid references for message {MessageId}", message.Id);
        }
    }

    /// <summary>
    /// Try to sync attachments for a message from Outlook
    /// Used for emails that were synced before attachment sync was implemented
    /// </summary>
    private async Task TrySyncAttachmentsAsync(Message message)
    {
        try
        {
            var binding = await _emailBindingRepository.GetByUserIdAndProviderAsync(message.OwnerId, "Outlook");
            if (binding == null || string.IsNullOrEmpty(binding.AccessToken)) return;

            // Decrypt access token
            var accessToken = DecryptToken(binding.AccessToken);

            // Refresh token if needed
            if (binding.TokenExpireTime <= DateTimeOffset.UtcNow.AddMinutes(5))
            {
                if (string.IsNullOrEmpty(binding.RefreshToken)) return;
                var refreshToken = DecryptToken(binding.RefreshToken);
                var newToken = await _outlookService.RefreshTokenAsync(refreshToken);
                if (newToken == null) return;
                // Encrypt new tokens before storing
                await _emailBindingRepository.UpdateTokenAsync(binding.Id, 
                    EncryptToken(newToken.AccessToken), 
                    EncryptToken(newToken.RefreshToken), 
                    newToken.ExpiresAt);
                accessToken = newToken.AccessToken;
            }

            // Sync attachments
            await _outlookService.SyncAttachmentsAsync(
                accessToken,
                message.ExternalMessageId!,
                message.Id);
        }
        catch (Exception ex)
        {
            // Log but don't fail - user can still see the email without attachments
            _logger.LogDebug(ex, "Failed to sync attachments for message {MessageId}", message.Id);
        }
    }


    /// <summary>
    /// Create and send a message
    /// </summary>
    public async Task<long> CreateAsync(MessageCreateDto input)
    {
        ValidateCreateInput(input);

        var ownerId = GetCurrentUserId();
        var senderName = _operatorContextService.GetOperatorDisplayName();
        // Fallback to UserName if Email is empty
        var senderEmail = !string.IsNullOrEmpty(_userContext.Email) 
            ? _userContext.Email 
            : (_userContext.UserName ?? string.Empty);

        // Log warning if Email is empty (for debugging)
        if (string.IsNullOrEmpty(_userContext.Email))
        {
            _logger.LogWarning("UserContext.Email is empty for user {UserId}, using UserName as fallback: {UserName}", 
                _userContext.UserId, _userContext.UserName);
        }

        // Handle based on message type
        return input.MessageType switch
        {
            MessageType.Internal => await CreateInternalMessageAsync(input, ownerId, senderName, senderEmail),
            MessageType.Email => await CreateEmailMessageAsync(input, ownerId, senderName, senderEmail),
            MessageType.Portal => await CreatePortalMessageAsync(input, ownerId, senderName, senderEmail),
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

        // Sync delete to Outlook if this is an external email
        if (message.MessageType == "Email" && !string.IsNullOrEmpty(message.ExternalMessageId))
        {
            await TryDeleteFromOutlookAsync(message);
        }

        // Move to Trash, save original folder for restore
        return await _messageRepository.MoveToFolderAsync(id, "Trash", message.Folder);
    }

    /// <summary>
    /// Soft delete message (set is_valid = false)
    /// Email type permanently deletes from Outlook
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

        // Permanently delete from Outlook if this is an external email
        if (message.MessageType == "Email" && !string.IsNullOrEmpty(message.ExternalMessageId))
        {
            await TryPermanentDeleteFromOutlookAsync(message);
        }

        // Soft delete attachments first
        await _attachmentRepository.DeleteByMessageIdAsync(id);

        // Soft delete message (set is_valid = false)
        return await _messageRepository.PermanentDeleteAsync(id);
    }

    /// <summary>
    /// Try to delete email from Outlook (move to deleted items)
    /// </summary>
    private async Task TryDeleteFromOutlookAsync(Message message)
    {
        try
        {
            var binding = await _emailBindingRepository.GetByUserIdAndProviderAsync(message.OwnerId, "Outlook");
            if (binding == null || string.IsNullOrEmpty(binding.AccessToken)) return;

            // Decrypt access token
            var accessToken = DecryptToken(binding.AccessToken);

            // Refresh token if needed
            if (binding.TokenExpireTime <= DateTimeOffset.UtcNow.AddMinutes(5))
            {
                if (string.IsNullOrEmpty(binding.RefreshToken)) return;
                var refreshToken = DecryptToken(binding.RefreshToken);
                var newToken = await _outlookService.RefreshTokenAsync(refreshToken);
                if (newToken == null) return;
                await _emailBindingRepository.UpdateTokenAsync(binding.Id, 
                    EncryptToken(newToken.AccessToken), 
                    EncryptToken(newToken.RefreshToken), 
                    newToken.ExpiresAt);
                accessToken = newToken.AccessToken;
            }

            // Delete from Outlook (moves to deleted items)
            var success = await _outlookService.DeleteEmailAsync(accessToken, message.ExternalMessageId!);
            if (success)
            {
                _logger.LogInformation("Deleted email {ExternalMessageId} from Outlook", message.ExternalMessageId);
            }
            else
            {
                _logger.LogWarning("Failed to delete email {ExternalMessageId} from Outlook", message.ExternalMessageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting email {ExternalMessageId} from Outlook", message.ExternalMessageId);
            // Silently ignore - local delete will still proceed
        }
    }

    /// <summary>
    /// Try to permanently delete email from Outlook
    /// </summary>
    private async Task TryPermanentDeleteFromOutlookAsync(Message message)
    {
        try
        {
            var binding = await _emailBindingRepository.GetByUserIdAndProviderAsync(message.OwnerId, "Outlook");
            if (binding == null || string.IsNullOrEmpty(binding.AccessToken)) return;

            // Decrypt access token
            var accessToken = DecryptToken(binding.AccessToken);

            // Refresh token if needed
            if (binding.TokenExpireTime <= DateTimeOffset.UtcNow.AddMinutes(5))
            {
                if (string.IsNullOrEmpty(binding.RefreshToken)) return;
                var refreshToken = DecryptToken(binding.RefreshToken);
                var newToken = await _outlookService.RefreshTokenAsync(refreshToken);
                if (newToken == null) return;
                await _emailBindingRepository.UpdateTokenAsync(binding.Id, 
                    EncryptToken(newToken.AccessToken), 
                    EncryptToken(newToken.RefreshToken), 
                    newToken.ExpiresAt);
                accessToken = newToken.AccessToken;
            }

            // Permanently delete from Outlook
            var success = await _outlookService.PermanentDeleteEmailAsync(accessToken, message.ExternalMessageId!);
            if (success)
            {
                _logger.LogInformation("Permanently deleted email {ExternalMessageId} from Outlook", message.ExternalMessageId);
            }
            else
            {
                _logger.LogWarning("Failed to permanently delete email {ExternalMessageId} from Outlook", message.ExternalMessageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error permanently deleting email {ExternalMessageId} from Outlook", message.ExternalMessageId);
            // Silently ignore - local delete will still proceed
        }
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

    /// <summary>
    /// Move message to Inbox folder
    /// </summary>
    public async Task<bool> MoveToInboxAsync(long id)
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
            throw new CRMException(ErrorCodeEnum.OperationNotAllowed, "You don't have permission to move this message");
        }

        // Sync to Outlook if this is an external email
        if (message.MessageType == "Email" && !string.IsNullOrEmpty(message.ExternalMessageId))
        {
            await TryMoveToOutlookFolderAsync(message, "inbox");
        }

        return await _messageRepository.MoveToFolderAsync(id, "Inbox", message.Folder);
    }

    /// <summary>
    /// Move message to Sent folder
    /// </summary>
    public async Task<bool> MoveToSentAsync(long id)
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
            throw new CRMException(ErrorCodeEnum.OperationNotAllowed, "You don't have permission to move this message");
        }

        // Sync to Outlook if this is an external email
        if (message.MessageType == "Email" && !string.IsNullOrEmpty(message.ExternalMessageId))
        {
            await TryMoveToOutlookFolderAsync(message, "sentitems");
        }

        return await _messageRepository.MoveToFolderAsync(id, "Sent", message.Folder);
    }

    /// <summary>
    /// Try to move email to Outlook folder
    /// </summary>
    private async Task TryMoveToOutlookFolderAsync(Message message, string outlookFolderId)
    {
        try
        {
            var binding = await _emailBindingRepository.GetByUserIdAndProviderAsync(message.OwnerId, "Outlook");
            if (binding == null || string.IsNullOrEmpty(binding.AccessToken)) return;

            // Decrypt access token
            var accessToken = DecryptToken(binding.AccessToken);

            // Refresh token if needed
            if (binding.TokenExpireTime <= DateTimeOffset.UtcNow.AddMinutes(5))
            {
                if (string.IsNullOrEmpty(binding.RefreshToken)) return;
                var refreshToken = DecryptToken(binding.RefreshToken);
                var newToken = await _outlookService.RefreshTokenAsync(refreshToken);
                if (newToken == null) return;
                await _emailBindingRepository.UpdateTokenAsync(binding.Id, 
                    EncryptToken(newToken.AccessToken), 
                    EncryptToken(newToken.RefreshToken), 
                    newToken.ExpiresAt);
                accessToken = newToken.AccessToken;
            }

            // Move to Outlook folder
            var success = await _outlookService.MoveEmailAsync(accessToken, message.ExternalMessageId!, outlookFolderId);
            if (success)
            {
                _logger.LogInformation("Moved email {ExternalMessageId} to Outlook folder {FolderId}",
                    message.ExternalMessageId, outlookFolderId);
            }
            else
            {
                _logger.LogWarning("Failed to move email {ExternalMessageId} to Outlook folder {FolderId}",
                    message.ExternalMessageId, outlookFolderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving email {ExternalMessageId} to Outlook folder {FolderId}",
                message.ExternalMessageId, outlookFolderId);
            // Silently ignore - local move will still proceed
        }
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
        await VerifyMessageOwnership(id);
        return await _messageRepository.ArchiveAsync(id);
    }

    /// <summary>
    /// Unarchive a message
    /// </summary>
    public async Task<bool> UnarchiveAsync(long id)
    {
        await VerifyMessageOwnership(id);
        return await _messageRepository.UnarchiveAsync(id);
    }

    /// <summary>
    /// Mark message as read
    /// </summary>
    public async Task<bool> MarkAsReadAsync(long id)
    {
        var message = await VerifyMessageOwnership(id);

        // Sync read status to Outlook if this is an external email
        if (message.MessageType == "Email" && !string.IsNullOrEmpty(message.ExternalMessageId))
        {
            await TrySyncReadStatusToOutlookAsync(message, true);
        }

        return await _messageRepository.MarkAsReadAsync(id);
    }

    /// <summary>
    /// Mark message as unread
    /// </summary>
    public async Task<bool> MarkAsUnreadAsync(long id)
    {
        var message = await VerifyMessageOwnership(id);

        // Sync read status to Outlook if this is an external email
        if (message.MessageType == "Email" && !string.IsNullOrEmpty(message.ExternalMessageId))
        {
            await TrySyncReadStatusToOutlookAsync(message, false);
        }

        return await _messageRepository.MarkAsUnreadAsync(id);
    }

    /// <summary>
    /// Try to sync read status to Outlook
    /// </summary>
    private async Task TrySyncReadStatusToOutlookAsync(Message message, bool isRead)
    {
        try
        {
            var binding = await _emailBindingRepository.GetByUserIdAndProviderAsync(message.OwnerId, "Outlook");
            if (binding == null || string.IsNullOrEmpty(binding.AccessToken)) return;

            // Decrypt access token
            var accessToken = DecryptToken(binding.AccessToken);

            // Refresh token if needed
            if (binding.TokenExpireTime <= DateTimeOffset.UtcNow.AddMinutes(5))
            {
                if (string.IsNullOrEmpty(binding.RefreshToken)) return;
                var refreshToken = DecryptToken(binding.RefreshToken);
                var newToken = await _outlookService.RefreshTokenAsync(refreshToken);
                if (newToken == null) return;
                await _emailBindingRepository.UpdateTokenAsync(binding.Id, 
                    EncryptToken(newToken.AccessToken), 
                    EncryptToken(newToken.RefreshToken), 
                    newToken.ExpiresAt);
                accessToken = newToken.AccessToken;
            }

            // Sync read status to Outlook
            bool success;
            if (isRead)
            {
                success = await _outlookService.MarkAsReadAsync(accessToken, message.ExternalMessageId!);
            }
            else
            {
                success = await _outlookService.MarkAsUnreadAsync(accessToken, message.ExternalMessageId!);
            }

            if (success)
            {
                _logger.LogInformation("Synced read status ({IsRead}) for email {ExternalMessageId} to Outlook",
                    isRead, message.ExternalMessageId);
            }
            else
            {
                _logger.LogWarning("Failed to sync read status for email {ExternalMessageId} to Outlook",
                    message.ExternalMessageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing read status for email {ExternalMessageId} to Outlook",
                message.ExternalMessageId);
            // Silently ignore - local update will still proceed
        }
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
        // Fallback to UserName if Email is empty
        var senderEmail = !string.IsNullOrEmpty(_userContext.Email) 
            ? _userContext.Email 
            : (_userContext.UserName ?? string.Empty);

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
            IsRead = true, // Sent messages are automatically marked as read
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
        // Fallback to UserName if Email is empty
        var senderEmail = !string.IsNullOrEmpty(_userContext.Email) 
            ? _userContext.Email 
            : (_userContext.UserName ?? string.Empty);

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
            IsRead = true, // Sent messages are automatically marked as read
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
        // Fallback to UserName if Email is empty
        var senderEmail = !string.IsNullOrEmpty(_userContext.Email) 
            ? _userContext.Email 
            : (_userContext.UserName ?? string.Empty);

        var draft = new Message
        {
            Subject = input.Subject ?? string.Empty,
            Body = input.Body ?? string.Empty,
            BodyPreview = GetBodyPreview(input.Body ?? string.Empty),
            MessageType = input.MessageType.ToString(),
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
        draft.IsRead = true; // Sent messages are automatically marked as read
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
            PortalCount = kvp.Value.portalCount
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
        // Check if user has Outlook binding
        var binding = await _emailBindingRepository.GetByUserIdAndProviderAsync(ownerId, "Outlook");
        if (binding == null || string.IsNullOrEmpty(binding.AccessToken))
        {
            throw new CRMException(ErrorCodeEnum.OperationNotAllowed, "Please bind your Outlook account first before sending emails");
        }

        // Decrypt access token
        var accessToken = DecryptToken(binding.AccessToken);

        // Refresh token if needed
        if (binding.TokenExpireTime <= DateTimeOffset.UtcNow.AddMinutes(5))
        {
            if (string.IsNullOrEmpty(binding.RefreshToken))
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed, "Outlook token expired, please re-bind your account");
            }
            var refreshToken = DecryptToken(binding.RefreshToken);
            var newToken = await _outlookService.RefreshTokenAsync(refreshToken);
            if (newToken == null)
            {
                throw new CRMException(ErrorCodeEnum.OperationNotAllowed, "Failed to refresh Outlook token, please re-bind your account");
            }
            await _emailBindingRepository.UpdateTokenAsync(binding.Id, 
                EncryptToken(newToken.AccessToken), 
                EncryptToken(newToken.RefreshToken), 
                newToken.ExpiresAt);
            accessToken = newToken.AccessToken;
        }

        // Build send email DTO
        var sendEmailDto = new OutlookSendEmailDto
        {
            Subject = input.Subject,
            Body = input.Body,
            IsHtml = input.IsHtml,
            ToRecipients = input.Recipients.Select(r => new RecipientDto { Name = r.Name, Email = r.Email }).ToList(),
            CcRecipients = input.CcRecipients?.Select(r => new RecipientDto { Name = r.Name, Email = r.Email }).ToList(),
            BccRecipients = input.BccRecipients?.Select(r => new RecipientDto { Name = r.Name, Email = r.Email }).ToList()
        };

        // Load attachments if present
        if (input.AttachmentIds?.Any() == true)
        {
            sendEmailDto.Attachments = await LoadAttachmentsForOutlookAsync(input.AttachmentIds);
        }

        // Send email via Outlook
        var sendResult = await _outlookService.SendEmailAsync(accessToken, sendEmailDto);
        if (!sendResult)
        {
            throw new CRMException(ErrorCodeEnum.BusinessError, "Failed to send email via Outlook");
        }

        // Create local record
        var message = CreateBaseMessage(input, ownerId, senderName, binding.Email ?? senderEmail);
        message.Labels = "[\"External\"]";

        message.InitCreateInfo(_userContext);
        await _messageRepository.InsertAsync(message);

        // Associate attachments with local message record
        if (input.AttachmentIds?.Any() == true)
        {
            _logger.LogInformation("Associating {AttachmentCount} attachments {AttachmentIds} with message {MessageId}", 
                input.AttachmentIds.Count, 
                string.Join(", ", input.AttachmentIds),
                message.Id);
            
            var associateResult = await _attachmentRepository.AssociateWithMessageAsync(input.AttachmentIds, message.Id);
            _logger.LogInformation("Attachment association result: {Result}", associateResult);
            
            message.HasAttachments = true;
            await _messageRepository.UpdateAsync(message);
        }

        _logger.LogInformation("Email sent via Outlook to {Recipients} with {AttachmentCount} attachments by user {UserId}, MessageId: {MessageId}", 
            string.Join(", ", input.Recipients.Select(r => r.Email)), 
            input.AttachmentIds?.Count ?? 0,
            ownerId,
            message.Id);

        return message.Id;
    }

    /// <summary>
    /// Load attachments from storage for sending via Outlook
    /// </summary>
    private async Task<List<OutlookAttachmentDto>> LoadAttachmentsForOutlookAsync(List<long> attachmentIds)
    {
        var outlookAttachments = new List<OutlookAttachmentDto>();

        foreach (var attachmentId in attachmentIds)
        {
            try
            {
                var attachment = await _attachmentRepository.GetByIdAsync(attachmentId);
                if (attachment == null || !attachment.IsValid)
                {
                    _logger.LogWarning("Attachment {AttachmentId} not found or invalid, skipping", attachmentId);
                    continue;
                }

                // Get file content from storage
                var (stream, _, _) = await GetFileStorageService().GetFileAsync(attachment.StoragePath);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var contentBytes = memoryStream.ToArray();

                outlookAttachments.Add(new OutlookAttachmentDto
                {
                    FileName = attachment.FileName,
                    ContentType = attachment.ContentType,
                    ContentBytes = contentBytes,
                    IsInline = attachment.IsInline,
                    ContentId = attachment.ContentId
                });

                _logger.LogDebug("Loaded attachment {FileName} ({Size} bytes) for Outlook", 
                    attachment.FileName, contentBytes.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load attachment {AttachmentId} for Outlook", attachmentId);
                // Continue with other attachments
            }
        }

        return outlookAttachments;
    }

    /// <summary>
    /// Get file storage service from DI container
    /// </summary>
    private IFileStorageService GetFileStorageService()
    {
        return _httpContextAccessor.HttpContext?.RequestServices.GetService(typeof(IFileStorageService)) as IFileStorageService
            ?? throw new CRMException(ErrorCodeEnum.BusinessError, "File storage service not available");
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
            MessageType = input.MessageType.ToString(),
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
            IsRead = !input.SaveAsDraft, // Sent messages are automatically marked as read
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

        // Copy attachments for recipient's message
        if (senderMessage.HasAttachments)
        {
            await CopyAttachmentsForRecipientAsync(senderMessage.Id, recipientCopy.Id);
        }
    }

    /// <summary>
    /// Copy attachment records from sender's message to recipient's message
    /// </summary>
    private async Task CopyAttachmentsForRecipientAsync(long senderMessageId, long recipientMessageId)
    {
        var senderAttachments = await _attachmentRepository.GetByMessageIdAsync(senderMessageId);
        foreach (var attachment in senderAttachments)
        {
            var recipientAttachment = new MessageAttachment
            {
                MessageId = recipientMessageId,
                FileName = attachment.FileName,
                FileSize = attachment.FileSize,
                ContentType = attachment.ContentType,
                StoragePath = attachment.StoragePath, // Share the same storage path
                ExternalAttachmentId = attachment.ExternalAttachmentId,
                ContentId = attachment.ContentId,
                IsInline = attachment.IsInline
            };
            recipientAttachment.InitCreateInfo(_userContext);
            await _attachmentRepository.InsertAsync(recipientAttachment);
        }
    }

    private static string GetBodyPreview(string body)
    {
        if (string.IsNullOrEmpty(body)) return string.Empty;

        // Strip HTML tags for preview
        var text = System.Text.RegularExpressions.Regex.Replace(body, "<[^>]*>", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

        return text.Length > 200 ? text.Substring(0, 200) + "..." : text;
    }

    private static string GetLabelsForMessageType(MessageType messageType)
    {
        return messageType switch
        {
            MessageType.Internal => "[\"Internal\"]",
            MessageType.Email => "[\"External\"]",
            MessageType.Portal => "[\"Portal\"]",
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
            SenderId = message.SenderId,
            SenderName = message.SenderName,
            SenderEmail = message.SenderEmail,
            Recipients = ParseRecipients(message.Recipients),
            MessageType = ParseMessageType(message.MessageType),
            Labels = ParseLabels(message.Labels),
            RelatedEntityCode = message.RelatedEntityCode,
            IsRead = message.IsRead,
            IsStarred = message.IsStarred,
            IsArchived = message.IsArchived,
            HasAttachments = message.HasAttachments,
            ReceivedDate = message.ReceivedDate?.ToUniversalTime(),
            SentDate = message.SentDate?.ToUniversalTime()
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
            SenderId = message.SenderId,
            SenderName = message.SenderName,
            SenderEmail = message.SenderEmail,
            MessageType = ParseMessageType(message.MessageType),
            Labels = ParseLabels(message.Labels),
            RelatedEntityCode = message.RelatedEntityCode,
            RelatedEntityType = message.RelatedEntityType,
            RelatedEntityId = message.RelatedEntityId,
            IsRead = message.IsRead,
            IsStarred = message.IsStarred,
            IsArchived = message.IsArchived,
            HasAttachments = message.HasAttachments,
            ReceivedDate = message.ReceivedDate?.ToUniversalTime(),
            SentDate = message.SentDate?.ToUniversalTime(),
            Folder = ParseMessageFolder(message.Folder),
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

    private static MessageType ParseMessageType(string? messageType)
    {
        return messageType switch
        {
            "Internal" => MessageType.Internal,
            "Email" => MessageType.Email,
            "Portal" => MessageType.Portal,
            _ => MessageType.Internal
        };
    }

    private static MessageFolder ParseMessageFolder(string? folder)
    {
        return folder switch
        {
            "Inbox" => MessageFolder.Inbox,
            "Sent" => MessageFolder.Sent,
            "Archive" => MessageFolder.Archive,
            "Trash" => MessageFolder.Trash,
            "Drafts" => MessageFolder.Drafts,
            _ => MessageFolder.Inbox
        };
    }

    private static List<MessageLabel> ParseLabels(string? labelsJson)
    {
        if (string.IsNullOrEmpty(labelsJson)) return new List<MessageLabel>();
        try
        {
            // Handle double-encoded JSON (e.g., "\"[\\\"Internal\\\"]\"")
            var jsonStr = labelsJson.Trim();
            if (jsonStr.StartsWith("\"") && jsonStr.EndsWith("\""))
            {
                jsonStr = JsonSerializer.Deserialize<string>(jsonStr) ?? "[]";
            }

            var stringLabels = JsonSerializer.Deserialize<List<string>>(jsonStr) ?? new List<string>();
            return stringLabels.Select(label => label switch
            {
                "Internal" => MessageLabel.Internal,
                "External" => MessageLabel.External,
                "Important" => MessageLabel.Important,
                "Portal" => MessageLabel.Portal,
                _ => MessageLabel.Internal
            }).ToList();
        }
        catch
        {
            return new List<MessageLabel>();
        }
    }

    private static List<RecipientDto> ParseRecipients(string? recipientsJson)
    {
        if (string.IsNullOrEmpty(recipientsJson)) return new List<RecipientDto>();
        try
        {
            // Handle double-encoded JSON (e.g., "\"[{...}]\"")
            var jsonStr = recipientsJson.Trim();
            if (jsonStr.StartsWith("\"") && jsonStr.EndsWith("\""))
            {
                jsonStr = JsonSerializer.Deserialize<string>(jsonStr) ?? "[]";
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<RecipientDto>>(jsonStr, options) ?? new List<RecipientDto>();
        }
        catch
        {
            return new List<RecipientDto>();
        }
    }

    /// <summary>
    /// Encrypt token for secure storage
    /// </summary>
    private string EncryptToken(string token)
    {
        if (string.IsNullOrEmpty(token)) return token;
        try
        {
            return _encryptionService.Encrypt(token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to encrypt token, storing as plain text");
            return token;
        }
    }

    /// <summary>
    /// Decrypt token for use
    /// </summary>
    private string DecryptToken(string encryptedToken)
    {
        if (string.IsNullOrEmpty(encryptedToken)) return encryptedToken;
        try
        {
            return _encryptionService.Decrypt(encryptedToken);
        }
        catch (Exception ex)
        {
            // Token might not be encrypted (legacy data), return as-is
            _logger.LogDebug(ex, "Failed to decrypt token, assuming plain text");
            return encryptedToken;
        }
    }

    #endregion
}
