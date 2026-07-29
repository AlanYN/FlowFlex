using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Application.Contracts.Dtos.OW.Message;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Shared.Models;
using Item.Internal.StandardApi.Response;

namespace FlowFlex.WebApi.Controllers.MessageCenter;

/// <summary>
/// Message Center Controller
/// Handles Internal Messages, Customer Emails (via Outlook), and Portal Messages
/// </summary>
[ApiController]
[Route("ow/messages/v{version:apiVersion}")]
[Display(Name = "messages")]
[Asp.Versioning.ApiVersion("1.0")]
[Authorize]
public class MessageController : Controllers.ControllerBase
{
    private readonly IMessageService _messageService;

    public MessageController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    #region Message CRUD

    /// <summary>
    /// Get paginated message list with filtering by folder, type, read status, etc.
    /// </summary>
    /// <param name="query">Query parameters including folder, type, isRead, searchText, pageIndex, pageSize</param>
    /// <returns>Paginated message list with summary information</returns>
    [HttpGet]
    [ProducesResponseType<SuccessResponse<PageModelDto<MessageListItemDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetPagedAsync([FromQuery] MessageQueryDto query)
    {
        var result = await _messageService.GetPagedAsync(query);
        return Success(result);
    }

    /// <summary>
    /// Get message detail by ID (automatically marks the message as read)
    /// </summary>
    /// <param name="id">Message ID</param>
    /// <returns>Full message details including body, attachments, and thread information</returns>
    [HttpGet("{id}")]
    [ProducesResponseType<SuccessResponse<MessageDetailDto>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetByIdAsync(long id)
    {
        var result = await _messageService.GetByIdAsync(id);
        if (result == null)
        {
            return NotFound("Message not found");
        }
        return Success(result);
    }

    /// <summary>
    /// Create and send a new message (Internal, Email via Outlook, or Portal message)
    /// </summary>
    /// <param name="input">Message content including recipients, subject, body, type, and optional attachments</param>
    /// <returns>Created message ID</returns>
    [HttpPost]
    [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> CreateAsync([FromBody] MessageCreateDto input)
    {
        var id = await _messageService.CreateAsync(input);
        return Success(id);
    }

    /// <summary>
    /// Update a draft message (only draft messages can be updated)
    /// </summary>
    /// <param name="id">Draft message ID</param>
    /// <param name="input">Updated message content</param>
    /// <returns>Whether update was successful</returns>
    [HttpPut("{id}")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> UpdateAsync(long id, [FromBody] MessageUpdateDto input)
    {
        var result = await _messageService.UpdateAsync(id, input);
        return Success(result);
    }

    /// <summary>
    /// Soft delete message (move to Trash folder, can be restored later)
    /// </summary>
    /// <param name="id">Message ID</param>
    /// <returns>Whether deletion was successful</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> DeleteAsync(long id)
    {
        var result = await _messageService.DeleteAsync(id);
        return Success(result);
    }

    /// <summary>
    /// Permanently delete a message (cannot be recovered)
    /// </summary>
    /// <param name="id">Message ID</param>
    /// <returns>Whether permanent deletion was successful</returns>
    [HttpDelete("{id}/permanent")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> PermanentDeleteAsync(long id)
    {
        var result = await _messageService.PermanentDeleteAsync(id);
        return Success(result);
    }

    /// <summary>
    /// Restore message from Trash
    /// </summary>
    /// <param name="id">Message ID</param>
    /// <returns>Whether restore was successful</returns>
    [HttpPost("{id}/restore")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> RestoreAsync(long id)
    {
        var result = await _messageService.RestoreAsync(id);
        return Success(result);
    }

    /// <summary>
    /// Move message to Inbox folder
    /// </summary>
    /// <param name="id">Message ID</param>
    /// <returns>Whether move was successful</returns>
    [HttpPost("{id}/move-to-inbox")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> MoveToInboxAsync(long id)
    {
        var result = await _messageService.MoveToInboxAsync(id);
        return Success(result);
    }

    /// <summary>
    /// Move message to Sent folder
    /// </summary>
    /// <param name="id">Message ID</param>
    /// <returns>Whether move was successful</returns>
    [HttpPost("{id}/move-to-sent")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> MoveToSentAsync(long id)
    {
        var result = await _messageService.MoveToSentAsync(id);
        return Success(result);
    }

    #endregion


    #region Message Operations

    /// <summary>
    /// Star a message
    /// </summary>
    /// <param name="id">Message ID</param>
    /// <returns>Whether starring was successful</returns>
    [HttpPost("{id}/star")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> StarAsync(long id)
    {
        var result = await _messageService.StarAsync(id);
        return Success(result);
    }

    /// <summary>
    /// Unstar a message
    /// </summary>
    /// <param name="id">Message ID</param>
    /// <returns>Whether unstarring was successful</returns>
    [HttpPost("{id}/unstar")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> UnstarAsync(long id)
    {
        var result = await _messageService.UnstarAsync(id);
        return Success(result);
    }

    /// <summary>
    /// Archive a message
    /// </summary>
    /// <param name="id">Message ID</param>
    /// <returns>Whether archiving was successful</returns>
    [HttpPost("{id}/archive")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> ArchiveAsync(long id)
    {
        var result = await _messageService.ArchiveAsync(id);
        return Success(result);
    }

    /// <summary>
    /// Unarchive a message
    /// </summary>
    /// <param name="id">Message ID</param>
    /// <returns>Whether unarchiving was successful</returns>
    [HttpPost("{id}/unarchive")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> UnarchiveAsync(long id)
    {
        var result = await _messageService.UnarchiveAsync(id);
        return Success(result);
    }

    /// <summary>
    /// Mark message as read
    /// </summary>
    /// <param name="id">Message ID</param>
    /// <returns>Whether marking as read was successful</returns>
    [HttpPost("{id}/read")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> MarkAsReadAsync(long id)
    {
        var result = await _messageService.MarkAsReadAsync(id);
        return Success(result);
    }

    /// <summary>
    /// Mark message as unread
    /// </summary>
    /// <param name="id">Message ID</param>
    /// <returns>Whether marking as unread was successful</returns>
    [HttpPost("{id}/unread")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> MarkAsUnreadAsync(long id)
    {
        var result = await _messageService.MarkAsUnreadAsync(id);
        return Success(result);
    }

    #endregion

    #region Reply and Forward

    /// <summary>
    /// Reply to a message (creates a new message in the same thread)
    /// </summary>
    /// <param name="id">Original message ID to reply to</param>
    /// <param name="input">Reply content including body and optional attachments</param>
    /// <returns>New reply message ID</returns>
    [HttpPost("{id}/reply")]
    [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> ReplyAsync(long id, [FromBody] MessageReplyDto input)
    {
        var newId = await _messageService.ReplyAsync(id, input);
        return Success(newId);
    }

    /// <summary>
    /// Forward a message to new recipients
    /// </summary>
    /// <param name="id">Message ID to forward</param>
    /// <param name="input">Forward details including new recipients and optional additional content</param>
    /// <returns>New forwarded message ID</returns>
    [HttpPost("{id}/forward")]
    [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> ForwardAsync(long id, [FromBody] MessageForwardDto input)
    {
        var newId = await _messageService.ForwardAsync(id, input);
        return Success(newId);
    }

    #endregion

    #region Drafts

    /// <summary>
    /// Save a new message as draft (not sent, can be edited later)
    /// </summary>
    /// <param name="input">Draft message content</param>
    /// <returns>Draft message ID</returns>
    [HttpPost("archive")]
    [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> SaveDraftAsync([FromBody] MessageCreateDto input)
    {
        var id = await _messageService.SaveDraftAsync(input);
        return Success(id);
    }

    /// <summary>
    /// Send a previously saved draft message
    /// </summary>
    /// <param name="id">Draft message ID</param>
    /// <returns>Sent message ID</returns>
    [HttpPost("archive/{id}/send")]
    [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> SendDraftAsync(long id)
    {
        var result = await _messageService.SendDraftAsync(id);
        return Success(result);
    }

    #endregion

    #region Statistics and Sync

    /// <summary>
    /// Get folder statistics including message count per folder and unread counts
    /// </summary>
    /// <returns>List of folder stats with name, total count, and unread count</returns>
    [HttpGet("stats/folders")]
    [ProducesResponseType<SuccessResponse<List<FolderStatsDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetFolderStatsAsync()
    {
        var result = await _messageService.GetFolderStatsAsync();
        return Success(result);
    }

    /// <summary>
    /// Get total unread message count for current user across all folders
    /// </summary>
    /// <returns>Total unread message count</returns>
    [HttpGet("stats/unread")]
    [ProducesResponseType<SuccessResponse<int>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetUnreadCountAsync()
    {
        var result = await _messageService.GetUnreadCountAsync();
        return Success(result);
    }

    #endregion
}
