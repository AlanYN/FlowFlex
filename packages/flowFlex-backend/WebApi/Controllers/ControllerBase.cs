using Item.Internal.StandardApi.Response;
using Microsoft.AspNetCore.Mvc;
using FlowFlex.Application.Filter;
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
    /// Sets the reference ID for the current request data
    /// </summary>
    /// <param name="id">The reference ID to be set</param>
    protected void SetRequestDataReferenceId(object id)
    {
        if (HttpContext.Items.ContainsKey(SaveRequestDataRecordAttribute.ReferenceId))
            HttpContext.Items.Remove(SaveRequestDataRecordAttribute.ReferenceId);

        HttpContext.Items.TryAdd(SaveRequestDataRecordAttribute.ReferenceId, id);
    }

    /// <summary>
    /// Retrieves the request data record ID from the HttpContext
    /// </summary>
    /// <returns>The request data record ID as a nullable long, or null if not found</returns>
    protected long? GetRequestDataRecordId()
    {
        if (HttpContext.Items.ContainsKey(SaveRequestDataRecordAttribute.Key))
        {
            var id = HttpContext.Items[SaveRequestDataRecordAttribute.Key];
            return long.Parse(id.ToString());
        }

        return null;
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    /// <returns>当前用户ID</returns>
    protected long GetCurrentUserId()
    {
        // 从请求头中获取用户ID
        var userIdHeader = HttpContext.Request.Headers["X-User-Id"].FirstOrDefault();

        // 如果请求头中没有用户ID，则从JWT令牌中获取
        if (string.IsNullOrEmpty(userIdHeader))
        {
            var userIdClaim = HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                            ?? HttpContext.User?.FindFirst("sub");
            userIdHeader = userIdClaim?.Value;
        }

        // 如果仍然没有用户ID，则使用默认值1
        if (string.IsNullOrEmpty(userIdHeader) || !long.TryParse(userIdHeader, out long userId))
        {
            userId = 1; // 默认管理员用户ID
        }

        return userId;
    }
}
