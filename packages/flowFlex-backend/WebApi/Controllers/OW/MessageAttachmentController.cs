using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.Dtos.OW.Message;
using FlowFlex.Application.Contracts.IServices.OW;
using Item.Internal.StandardApi.Response;

namespace FlowFlex.WebApi.Controllers.OW;

/// <summary>
/// Message Attachment Controller
/// </summary>
[ApiController]
[Route("ow/message-attachments/v{version:apiVersion}")]
[Display(Name = "message-attachments")]
[Asp.Versioning.ApiVersion("1.0")]
[Authorize]
public class MessageAttachmentController : Controllers.ControllerBase
{
    private readonly IMessageAttachmentService _attachmentService;

    public MessageAttachmentController(IMessageAttachmentService attachmentService)
    {
        _attachmentService = attachmentService;
    }

    /// <summary>
    /// Get attachments for a message
    /// </summary>
    [HttpGet("message/{messageId}")]
    [ProducesResponseType<SuccessResponse<List<MessageAttachmentDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetByMessageIdAsync(long messageId)
    {
        var result = await _attachmentService.GetByMessageIdAsync(messageId);
        return Success(result);
    }

    /// <summary>
    /// Get attachment by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType<SuccessResponse<MessageAttachmentDto>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetByIdAsync(long id)
    {
        var result = await _attachmentService.GetByIdAsync(id);
        if (result == null)
        {
            return NotFound("Attachment not found");
        }
        return Success(result);
    }

    /// <summary>
    /// Upload attachment for a message
    /// </summary>
    [HttpPost("message/{messageId}")]
    [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> UploadAsync(long messageId, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        using var stream = file.OpenReadStream();
        var id = await _attachmentService.UploadAsync(messageId, file.FileName, file.ContentType, stream);
        return Success(id);
    }

    /// <summary>
    /// Upload temporary attachment (for drafts)
    /// </summary>
    [HttpPost("temp")]
    [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> UploadTempAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        using var stream = file.OpenReadStream();
        var id = await _attachmentService.UploadTempAsync(file.FileName, file.ContentType, stream);
        return Success(id);
    }

    /// <summary>
    /// Upload attachment (alias for temp upload, returns full attachment info)
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType<SuccessResponse<MessageAttachmentDto>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> UploadAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        using var stream = file.OpenReadStream();
        var id = await _attachmentService.UploadTempAsync(file.FileName, file.ContentType, stream);
        var attachment = await _attachmentService.GetByIdAsync(id);
        return Success(attachment);
    }

    /// <summary>
    /// Download attachment
    /// </summary>
    [HttpGet("{id}/download")]
    [ProducesResponseType(typeof(FileStreamResult), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> DownloadAsync(long id)
    {
        var result = await _attachmentService.DownloadAsync(id);
        if (result == null)
        {
            return NotFound("Attachment not found");
        }

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    /// <summary>
    /// Delete attachment
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> DeleteAsync(long id)
    {
        var result = await _attachmentService.DeleteAsync(id);
        return Success(result);
    }

    /// <summary>
    /// Associate temporary attachments with a message
    /// </summary>
    [HttpPost("associate/{messageId}")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> AssociateWithMessageAsync(long messageId, [FromBody] List<long> attachmentIds)
    {
        var result = await _attachmentService.AssociateWithMessageAsync(attachmentIds, messageId);
        return Success(result);
    }
}
