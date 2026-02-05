using Item.Internal.StandardApi.Response;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Security.Claims;

namespace FlowFlex.WebApi.Controllers;

/// <summary>
/// Base controller class for all controllers in the application
/// </summary>
public abstract class ControllerBase : Microsoft.AspNetCore.Mvc.ControllerBase
{
    /// <summary>
    /// The name of the application
    /// </summary>
    protected readonly string ApplicationName = "unisco-wfe";

    protected ControllerBase()
    {
    }

    protected IActionResult Success<T>(T data)
    {
        return Ok(SuccessResponse.Create(data));
    }

    /// <summary>
    /// Creates a success response without data based on the current router type
    /// </summary>
    /// <returns>An IActionResult containing the success response</returns>
    protected IActionResult Success()
    {
        return Ok(SuccessResponse.Create());
    }

    /// <summary>
    /// Get current user ID
    /// </summary>
    /// <returns>Current user ID</returns>
    protected long GetCurrentUserId()
    {
        // Get user ID from request header
        var userIdHeader = HttpContext.Request.Headers["X-User-Id"].FirstOrDefault();

        // If not in header, get from JWT token
        if (string.IsNullOrEmpty(userIdHeader))
        {
            var userIdClaim = HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                            ?? HttpContext.User?.FindFirst("sub");
            userIdHeader = userIdClaim?.Value;
        }

        // If still no user ID, use default value 1
        if (string.IsNullOrEmpty(userIdHeader) || !long.TryParse(userIdHeader, out long userId))
        {
            userId = 1; // Default admin user ID
        }

        return userId;
    }
}
