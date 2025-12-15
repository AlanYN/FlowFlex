using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.Dtos.OW.EmailBinding;
using FlowFlex.Application.Contracts.IServices.OW;
using Item.Internal.StandardApi.Response;

namespace FlowFlex.WebApi.Controllers.OW;

/// <summary>
/// Email Binding Controller - Manages Outlook account binding
/// </summary>
[ApiController]
[Route("ow/email-binding/v{version:apiVersion}")]
[Display(Name = "email-binding")]
[Asp.Versioning.ApiVersion("1.0")]
[Authorize]
public class EmailBindingController : Controllers.ControllerBase
{
    private readonly IEmailBindingService _emailBindingService;

    public EmailBindingController(IEmailBindingService emailBindingService)
    {
        _emailBindingService = emailBindingService;
    }

    /// <summary>
    /// Get OAuth authorization URL for Outlook binding
    /// </summary>
    /// <remarks>
    /// Returns the Microsoft OAuth authorization URL. 
    /// Frontend should redirect user to this URL to start the OAuth flow.
    /// </remarks>
    [HttpGet("authorize")]
    [ProducesResponseType<SuccessResponse<AuthorizeUrlDto>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetAuthorizationUrlAsync()
    {
        var result = await _emailBindingService.GetAuthorizationUrlAsync();
        return Success(result);
    }

    /// <summary>
    /// Handle OAuth callback from Microsoft
    /// </summary>
    /// <remarks>
    /// This endpoint receives the OAuth callback from Microsoft after user authorization.
    /// It exchanges the authorization code for access token and creates/updates the email binding.
    /// </remarks>
    [HttpGet("callback")]
    [ProducesResponseType<SuccessResponse<EmailBindingDto>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> HandleCallbackAsync([FromQuery] string code, [FromQuery] string state, [FromQuery] string? error = null, [FromQuery] string? error_description = null)
    {
        var callback = new OAuthCallbackDto
        {
            Code = code ?? string.Empty,
            State = state ?? string.Empty,
            Error = error,
            ErrorDescription = error_description
        };

        var result = await _emailBindingService.HandleCallbackAsync(callback);
        return Success(result);
    }

    /// <summary>
    /// Get current user's email binding status
    /// </summary>
    [HttpGet("current")]
    [ProducesResponseType<SuccessResponse<EmailBindingDto>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetCurrentBindingAsync()
    {
        var result = await _emailBindingService.GetCurrentBindingAsync();
        return Success(result);
    }

    /// <summary>
    /// Unbind Outlook account
    /// </summary>
    [HttpPost("unbind")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> UnbindAsync()
    {
        var result = await _emailBindingService.UnbindAsync();
        return Success(result);
    }

    /// <summary>
    /// Update email binding settings
    /// </summary>
    [HttpPut("settings")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> UpdateSettingsAsync([FromBody] EmailBindingUpdateDto input)
    {
        var result = await _emailBindingService.UpdateSettingsAsync(input);
        return Success(result);
    }

    /// <summary>
    /// Manually trigger email sync
    /// </summary>
    [HttpPost("sync")]
    [ProducesResponseType<SuccessResponse<SyncResultDto>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> SyncEmailsAsync()
    {
        var result = await _emailBindingService.SyncEmailsAsync();
        return Success(result);
    }
}
