using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Web;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.Dtos.OW.EmailBinding;
using FlowFlex.Application.Contracts.IServices;
using FlowFlex.Application.Contracts.IServices.OW;
using Item.Internal.StandardApi.Response;

namespace FlowFlex.WebApi.Controllers.MessageCenter;

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
    private readonly IEmailTemplateService _emailTemplateService;

    public EmailBindingController(
        IEmailBindingService emailBindingService,
        IEmailTemplateService emailTemplateService)
    {
        _emailBindingService = emailBindingService;
        _emailTemplateService = emailTemplateService;
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
    /// This endpoint does not require authentication as it's called by Microsoft OAuth redirect.
    /// Returns an HTML page to show the result to the user.
    /// </remarks>
    [HttpGet("callback")]
    [AllowAnonymous]
    [Produces("text/html")]
    public async Task<IActionResult> HandleCallbackAsync([FromQuery] string? code, [FromQuery] string state, [FromQuery] string? error = null, [FromQuery] string? error_description = null)
    {
        var callback = new OAuthCallbackDto
        {
            Code = code ?? string.Empty,
            State = state ?? string.Empty,
            Error = error,
            ErrorDescription = error_description
        };

        try
        {
            var result = await _emailBindingService.HandleCallbackAsync(callback);
            return Content(GenerateSuccessHtml(result.Email), "text/html");
        }
        catch (Exception ex)
        {
            return Content(GenerateErrorHtml(ex.Message), "text/html");
        }
    }

    /// <summary>
    /// Generate success HTML page from template
    /// </summary>
    private string GenerateSuccessHtml(string email)
    {
        var variables = new Dictionary<string, object>
        {
            { "Email", HttpUtility.HtmlEncode(email) },
            { "EmailJs", HttpUtility.JavaScriptStringEncode(email) }
        };
        return _emailTemplateService.Render("email_binding_success", variables);
    }

    /// <summary>
    /// Generate error HTML page from template
    /// </summary>
    private string GenerateErrorHtml(string errorMessage)
    {
        var variables = new Dictionary<string, object>
        {
            { "ErrorMessage", HttpUtility.HtmlEncode(errorMessage) },
            { "ErrorMessageJs", HttpUtility.JavaScriptStringEncode(errorMessage) }
        };
        return _emailTemplateService.Render("email_binding_error", variables);
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
    /// Enable auto sync
    /// </summary>
    /// <remarks>
    /// Enables automatic email synchronization in the background.
    /// Emails will be synced based on the configured interval (default: 15 minutes).
    /// </remarks>
    [HttpPost("auto-sync/enable")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> EnableAutoSyncAsync()
    {
        var result = await _emailBindingService.UpdateSettingsAsync(new EmailBindingUpdateDto { AutoSyncEnabled = true });
        return Success(result);
    }

    /// <summary>
    /// Disable auto sync
    /// </summary>
    /// <remarks>
    /// Disables automatic email synchronization.
    /// You can still manually trigger sync using the sync endpoints.
    /// </remarks>
    [HttpPost("auto-sync/disable")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> DisableAutoSyncAsync()
    {
        var result = await _emailBindingService.UpdateSettingsAsync(new EmailBindingUpdateDto { AutoSyncEnabled = false });
        return Success(result);
    }

    /// <summary>
    /// Manually trigger email sync
    /// </summary>
    /// <remarks>
    /// If LastSyncTime is null, performs full sync; otherwise performs incremental sync.
    /// </remarks>
    [HttpPost("sync")]
    [ProducesResponseType<SuccessResponse<SyncResultDto>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> SyncEmailsAsync()
    {
        var result = await _emailBindingService.SyncEmailsAsync();
        return Success(result);
    }

    /// <summary>
    /// Full sync - sync all emails from specified folders
    /// </summary>
    /// <remarks>
    /// Syncs all emails (up to maxCount) from specified folders with pagination.
    /// This is slower but ensures all historical emails are synced.
    /// Default folders: inbox, sentitems
    /// Default maxCount: 500, max: 2000
    /// </remarks>
    [HttpPost("sync/full")]
    [ProducesResponseType<SuccessResponse<FullSyncResultDto>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> FullSyncAsync()
    {
        var result = await _emailBindingService.FullSyncAsync();
        return Success(result);
    }
}
