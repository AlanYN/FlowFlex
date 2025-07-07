using Item.Internal.StandardApi.Response;
using Microsoft.AspNetCore.Mvc;
using FlowFlex.Application.Filter;

namespace FlowFlex.WebApi.Controllers;

/// <summary>
/// Base controller class for all controllers in the application
/// </summary>
public abstract class ControllerBase : Microsoft.AspNetCore.Mvc.ControllerBase
{
    /// <summary>
    /// The name of the application
    /// </summary>
    protected readonly string ApplicationName = "unisco-crm";

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
}
