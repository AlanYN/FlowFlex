using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts;
using FlowFlex.Application.Contracts.Dtos.OW.Message;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Application.Services.OW.Extensions;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Helpers;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Application.Services.MessageCenter;

/// <summary>
/// Message Attachment Service Implementation
/// </summary>
public class MessageAttachmentService : IMessageAttachmentService, IScopedService
{
    private readonly IMessageAttachmentRepository _attachmentRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IEmailBindingRepository _emailBindingRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IOutlookService _outlookService;
    private readonly UserContext _userContext;
    private readonly ILogger<MessageAttachmentService> _logger;

    private const string AttachmentCategory = "MessageAttachments";

    public MessageAttachmentService(
        IMessageAttachmentRepository attachmentRepository,
        IMessageRepository messageRepository,
        IEmailBindingRepository emailBindingRepository,
        IFileStorageService fileStorageService,
        IOutlookService outlookService,
        UserContext userContext,
        ILogger<MessageAttachmentService> logger)
    {
        _attachmentRepository = attachmentRepository;
        _messageRepository = messageRepository;
        _emailBindingRepository = emailBindingRepository;
        _fileStorageService = fileStorageService;
        _outlookService = outlookService;
        _userContext = userContext;
        _logger = logger;
    }

    /// <summary>
    /// Get attachments for a message
    /// </summary>
    public async Task<List<MessageAttachmentDto>> GetByMessageIdAsync(long messageId)
    {
        // Verify user has access to the message
        await VerifyMessageAccessAsync(messageId);

        var attachments = await _attachmentRepository.GetByMessageIdAsync(messageId);
        return attachments.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Get attachment by ID
    /// </summary>
    public async Task<MessageAttachmentDto?> GetByIdAsync(long id)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(id);
        if (attachment == null || !attachment.IsValid)
        {
            return null;
        }

        // Verify user has access to the parent message (if associated)
        if (attachment.MessageId > 0)
        {
            await VerifyMessageAccessAsync(attachment.MessageId);
        }

        return MapToDto(attachment);
    }


    /// <summary>
    /// Upload attachment with message association
    /// </summary>
    public async Task<long> UploadAsync(long messageId, string fileName, string contentType, Stream content)
    {
        // Verify user has access to the message
        await VerifyMessageAccessAsync(messageId);

        // Save file to storage
        var formFile = CreateFormFile(content, fileName, contentType);
        var storageResult = await _fileStorageService.SaveFileAsync(formFile, AttachmentCategory, TenantContextHelper.GetTenantIdOrDefault(_userContext));

        if (!storageResult.Success)
        {
            throw new CRMException(ErrorCodeEnum.BusinessError, storageResult.ErrorMessage ?? "Failed to upload attachment");
        }

        // Create attachment record
        var attachment = new MessageAttachment
        {
            MessageId = messageId,
            FileName = fileName,
            FileSize = storageResult.FileSize,
            ContentType = contentType,
            StoragePath = storageResult.FilePath
        };

        attachment.InitCreateInfo(_userContext);
        await _attachmentRepository.InsertAsync(attachment);

        // Update message HasAttachments flag
        var message = await _messageRepository.GetByIdAsync(messageId);
        if (message != null && !message.HasAttachments)
        {
            message.HasAttachments = true;
            await _messageRepository.UpdateAsync(message);
        }

        return attachment.Id;
    }

    /// <summary>
    /// Upload attachment without message association (for draft attachments)
    /// </summary>
    public async Task<long> UploadTempAsync(string fileName, string contentType, Stream content)
    {
        // Save file to storage
        var formFile = CreateFormFile(content, fileName, contentType);
        var storageResult = await _fileStorageService.SaveFileAsync(formFile, AttachmentCategory, TenantContextHelper.GetTenantIdOrDefault(_userContext));

        if (!storageResult.Success)
        {
            throw new CRMException(ErrorCodeEnum.BusinessError, storageResult.ErrorMessage ?? "Failed to upload attachment");
        }

        // Create attachment record without message association
        var attachment = new MessageAttachment
        {
            MessageId = 0, // Temporary attachment
            FileName = fileName,
            FileSize = storageResult.FileSize,
            ContentType = contentType,
            StoragePath = storageResult.FilePath
        };

        attachment.InitCreateInfo(_userContext);
        await _attachmentRepository.InsertAsync(attachment);

        return attachment.Id;
    }

    /// <summary>
    /// Download attachment content
    /// </summary>
    public async Task<(Stream Content, string ContentType, string FileName)?> DownloadAsync(long id)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(id);
        if (attachment == null || !attachment.IsValid)
        {
            return null;
        }

        // Verify user has access to the parent message (if associated)
        Message? message = null;
        if (attachment.MessageId > 0)
        {
            message = await VerifyMessageAccessAndGetAsync(attachment.MessageId);
        }

        // If StoragePath is empty but has ExternalAttachmentId, download from Outlook
        if (string.IsNullOrEmpty(attachment.StoragePath) && !string.IsNullOrEmpty(attachment.ExternalAttachmentId))
        {
            return await DownloadFromOutlookAsync(attachment, message);
        }

        // Get file from local storage
        try
        {
            var (stream, _, _) = await _fileStorageService.GetFileAsync(attachment.StoragePath);
            return (stream, attachment.ContentType, attachment.FileName);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Download attachment from Outlook and optionally cache to local storage
    /// <summary>
    /// Download attachment from Outlook
    /// Note: The returned Stream must be disposed by the caller
    /// </summary>
    /// <returns>Tuple containing the stream, content type, and filename. Caller is responsible for disposing the stream.</returns>
    private async Task<(Stream Content, string ContentType, string FileName)?> DownloadFromOutlookAsync(
        MessageAttachment attachment, Message? message)
    {
        try
        {
            if (message == null || string.IsNullOrEmpty(message.ExternalMessageId))
            {
                _logger.LogWarning("Cannot download attachment {AttachmentId}: message or external message ID is missing",
                    attachment.Id);
                return null;
            }

            // Get Outlook binding for the message owner
            var binding = await _emailBindingRepository.GetByUserIdAndProviderAsync(message.OwnerId, "Outlook");
            if (binding == null || string.IsNullOrEmpty(binding.AccessToken))
            {
                _logger.LogWarning("Cannot download attachment {AttachmentId}: no Outlook binding found", attachment.Id);
                return null;
            }

            // Refresh token if needed
            if (binding.TokenExpireTime <= DateTimeOffset.UtcNow.AddMinutes(5))
            {
                if (string.IsNullOrEmpty(binding.RefreshToken))
                {
                    _logger.LogWarning("Cannot download attachment {AttachmentId}: token expired and no refresh token",
                        attachment.Id);
                    return null;
                }

                var newToken = await _outlookService.RefreshTokenAsync(binding.RefreshToken);
                if (newToken == null)
                {
                    _logger.LogWarning("Cannot download attachment {AttachmentId}: token refresh failed", attachment.Id);
                    return null;
                }

                await _emailBindingRepository.UpdateTokenAsync(binding.Id, newToken.AccessToken,
                    newToken.RefreshToken, newToken.ExpiresAt);
                binding.AccessToken = newToken.AccessToken;
            }

            // Download attachment from Outlook
            var contentBytes = await _outlookService.DownloadAttachmentAsync(
                binding.AccessToken,
                message.ExternalMessageId,
                attachment.ExternalAttachmentId!);

            if (contentBytes == null)
            {
                _logger.LogWarning("Failed to download attachment {AttachmentId} from Outlook", attachment.Id);
                return null;
            }

            _logger.LogInformation("Downloaded attachment {AttachmentId} ({FileName}) from Outlook, size: {Size} bytes",
                attachment.Id, attachment.FileName, contentBytes.Length);

            // MemoryStream from byte array is safe - no external resources to manage
            var stream = new MemoryStream(contentBytes);
            return (stream, attachment.ContentType, attachment.FileName);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error downloading attachment {AttachmentId} from Outlook", attachment.Id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading attachment {AttachmentId} from Outlook", attachment.Id);
            return null;
        }
    }

    /// <summary>
    /// Delete attachment
    /// </summary>
    public async Task<bool> DeleteAsync(long id)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(id);
        if (attachment == null || !attachment.IsValid)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "Attachment not found");
        }

        // Verify user has access to the parent message (if associated)
        if (attachment.MessageId > 0)
        {
            await VerifyMessageAccessAsync(attachment.MessageId);
        }

        // Delete file from storage
        await _fileStorageService.DeleteFileAsync(attachment.StoragePath);

        // Soft delete attachment record
        attachment.IsValid = false;
        attachment.InitUpdateInfo(_userContext);
        return await _attachmentRepository.UpdateAsync(attachment);
    }

    /// <summary>
    /// Associate temporary attachments with a message
    /// </summary>
    public async Task<bool> AssociateWithMessageAsync(List<long> attachmentIds, long messageId)
    {
        if (attachmentIds == null || !attachmentIds.Any())
        {
            return true;
        }

        // Verify user has access to the message
        await VerifyMessageAccessAsync(messageId);

        return await _attachmentRepository.AssociateWithMessageAsync(attachmentIds, messageId);
    }

    #region Private Helper Methods

    private long GetCurrentUserId()
    {
        if (string.IsNullOrEmpty(_userContext.UserId) || !long.TryParse(_userContext.UserId, out var userId))
        {
            throw new CRMException(ErrorCodeEnum.AuthenticationFail, "User not authenticated");
        }
        return userId;
    }

    private async Task VerifyMessageAccessAsync(long messageId)
    {
        await VerifyMessageAccessAndGetAsync(messageId);
    }

    private async Task<Message> VerifyMessageAccessAndGetAsync(long messageId)
    {
        var message = await _messageRepository.GetByIdAsync(messageId);
        if (message == null || !message.IsValid)
        {
            throw new CRMException(ErrorCodeEnum.DataNotFound, "Message not found");
        }

        var userId = GetCurrentUserId();
        if (message.OwnerId != userId)
        {
            throw new CRMException(ErrorCodeEnum.OperationNotAllowed, "You don't have permission to access this message");
        }

        return message;
    }

    private static MessageAttachmentDto MapToDto(MessageAttachment attachment)
    {
        return new MessageAttachmentDto
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            FileSize = attachment.FileSize
        };
    }

    private static IFormFile CreateFormFile(Stream content, string fileName, string contentType)
    {
        // Reset stream position if possible
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        return new StreamFormFile(content, fileName, contentType);
    }

    #endregion
}

/// <summary>
/// Helper class to create IFormFile from Stream
/// </summary>
internal class StreamFormFile : IFormFile
{
    private readonly Stream _stream;
    private readonly string _fileName;
    private readonly string _contentType;

    public StreamFormFile(Stream stream, string fileName, string contentType)
    {
        _stream = stream;
        _fileName = fileName;
        _contentType = contentType;
    }

    public string ContentType => _contentType;
    public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{_fileName}\"";
    public IHeaderDictionary Headers => new HeaderDictionary();
    public long Length => _stream.Length;
    public string Name => "file";
    public string FileName => _fileName;

    public void CopyTo(Stream target) => _stream.CopyTo(target);
    public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) => _stream.CopyToAsync(target, cancellationToken);
    public Stream OpenReadStream() => _stream;
}
