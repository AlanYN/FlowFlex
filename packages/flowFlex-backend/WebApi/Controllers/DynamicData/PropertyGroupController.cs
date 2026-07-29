using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.IServices.DynamicData;
using FlowFlex.Domain.Shared.Models.DynamicData;
using Item.Internal.StandardApi.Response;
using System.Net;

namespace FlowFlex.WebApi.Controllers.DynamicData;

/// <summary>
/// Property group management API - Manages groups/categories for organizing dynamic field definitions.
/// Groups provide a logical structure for displaying and managing properties in the UI.
/// </summary>
[ApiController]
[Route("ow/dynamic-data/v{version:apiVersion}/groups")]
[Display(Name = "dynamic-data-groups")]
[Asp.Versioning.ApiVersion("1.0")]
[Authorize]
public class PropertyGroupController : Controllers.ControllerBase
{
    private readonly IPropertyGroupService _propertyGroupService;

    public PropertyGroupController(IPropertyGroupService propertyGroupService)
    {
        _propertyGroupService = propertyGroupService;
    }

    /// <summary>
    /// Get all property groups (without field details)
    /// </summary>
    /// <returns>List of all property groups</returns>
    [HttpGet]
    [ProducesResponseType<SuccessResponse<List<PropertyGroupDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetAll()
    {
        var data = await _propertyGroupService.GetGroupListAsync();
        return Success(data);
    }

    /// <summary>
    /// Get group by ID
    /// </summary>
    /// <param name="id">Group ID</param>
    [HttpGet("{id}")]
    [ProducesResponseType<SuccessResponse<PropertyGroupDto>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetById(long id)
    {
        var data = await _propertyGroupService.GetGroupByIdAsync(id);
        if (data == null)
        {
            return NotFound();
        }
        return Success(data);
    }

    /// <summary>
    /// Get all property groups with their associated field definitions
    /// </summary>
    /// <returns>List of groups each containing their field definitions</returns>
    [HttpGet("with-fields")]
    [ProducesResponseType<SuccessResponse<List<PropertyGroupDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetWithFields()
    {
        var data = await _propertyGroupService.GetGroupsWithFieldsAsync();
        return Success(data);
    }

    /// <summary>
    /// Create a new property group
    /// </summary>
    /// <param name="dto">Group definition including name, description, and sort order</param>
    /// <returns>Created group ID</returns>
    [HttpPost]
    [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Create([FromBody] PropertyGroupDto dto)
    {
        var id = await _propertyGroupService.AddGroupAsync(dto);
        return Success(id);
    }

    /// <summary>
    /// Update an existing property group
    /// </summary>
    /// <param name="id">Group ID</param>
    /// <param name="dto">Updated group definition</param>
    /// <returns>Whether update was successful</returns>
    [HttpPut("{id}")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Update(long id, [FromBody] PropertyGroupDto dto)
    {
        dto.Id = id;
        await _propertyGroupService.UpdateGroupAsync(dto);
        return Success(true);
    }

    /// <summary>
    /// Delete a property group (fields in this group will become ungrouped)
    /// </summary>
    /// <param name="id">Group ID to delete</param>
    /// <returns>Whether deletion was successful</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Delete(long id)
    {
        await _propertyGroupService.DeleteGroupAsync(id);
        return Success(true);
    }

    /// <summary>
    /// Batch update group display sort order
    /// </summary>
    /// <param name="sorts">Dictionary mapping group ID to new sort order value</param>
    /// <returns>Whether sort update was successful</returns>
    [HttpPut("sort")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> UpdateSort([FromBody] Dictionary<long, int> sorts)
    {
        await _propertyGroupService.UpdateGroupSortAsync(sorts);
        return Success(true);
    }
}
