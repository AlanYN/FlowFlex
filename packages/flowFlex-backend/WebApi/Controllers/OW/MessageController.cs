using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Application.Contracts.Dtos.OW.Message;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Shared.Models;
using Item.Internal.StandardApi.Response;

namespace FlowFlex.WebApi.Controllers.OW;

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
    /// Get paginated message list with filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType<SuccessResponse<PageModelDto<MessageListItemDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetPagedAsync([FromQuery] MessageQueryDto query)
    {
        var result = await _messageService.GetPagedAsync(query);
        return Success(result);
    }

    /// <summary>
    /// Get message detail by ID (auto-marks as read)
    /// </summary>
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
    /// Create and send a message
    /// </summary>
    [HttpPost]
    [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> CreateAsync([FromBody] MessageCreateDto input)
    {
        var id = await _messageService.CreateAsync(input);
        return Success(id);
    }

    /// <summary>
    /// Update a draft message
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> UpdateAsync(long id, [FromBody] MessageUpdateDto input)
    {
        var result = await _messageService.UpdateAsync(id, input);
        return Success(result);
    }

    /// <summary>
    /// Delete message (move to Trash)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> DeleteAsync(long id)
    {
        var result = await _messageService.DeleteAsync(id);
        return Success(result);
    }

    /// <summary>
    /// Permanently delete message
    /// </summary>
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
    [HttpPost("{id}/restore")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> RestoreAsync(long id)
    {
        var result = await _messageService.RestoreAsync(id);
        return Success(result);
    }

    #endregion


    #region Message Operations

    /// <summary>
    /// Star a message
    /// </summary>
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
    /// Reply to a message
    /// </summary>
    [HttpPost("{id}/reply")]
    [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> ReplyAsync(long id, [FromBody] MessageReplyDto input)
    {
        var newId = await _messageService.ReplyAsync(id, input);
        return Success(newId);
    }

    /// <summary>
    /// Forward a message
    /// </summary>
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
    /// Save message as draft
    /// </summary>
    [HttpPost("archive")]
    [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> SaveDraftAsync([FromBody] MessageCreateDto input)
    {
        var id = await _messageService.SaveDraftAsync(input);
        return Success(id);
    }

    /// <summary>
    /// Send a draft message
    /// </summary>
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
    /// Get folder statistics
    /// </summary>
    [HttpGet("stats/folders")]
    [ProducesResponseType<SuccessResponse<List<FolderStatsDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetFolderStatsAsync()
    {
        var result = await _messageService.GetFolderStatsAsync();
        return Success(result);
    }

    /// <summary>
    /// Get unread message count
    /// </summary>
    [HttpGet("stats/unread")]
    [ProducesResponseType<SuccessResponse<int>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetUnreadCountAsync()
    {
        var result = await _messageService.GetUnreadCountAsync();
        return Success(result);
    }

    /// <summary>
    /// Manually trigger Outlook email sync
    /// </summary>
    [HttpPost("sync/outlook")]
    [ProducesResponseType<SuccessResponse<int>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> SyncOutlookEmailsAsync()
    {
        var count = await _messageService.SyncOutlookEmailsAsync();
        return Success(count);
    }

    #endregion
}
