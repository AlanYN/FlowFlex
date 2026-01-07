using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.IServices.DynamicData;
using FlowFlex.Domain.Shared.Models.DynamicData;
using Item.Internal.StandardApi.Response;
using System.Net;

namespace FlowFlex.WebApi.Controllers.DynamicData;

/// <summary>
/// Property group management API
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
    /// Get all groups
    /// </summary>
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
    /// Get groups with fields
    /// </summary>
    [HttpGet("with-fields")]
    [ProducesResponseType<SuccessResponse<List<PropertyGroupDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetWithFields()
    {
        var data = await _propertyGroupService.GetGroupsWithFieldsAsync();
        return Success(data);
    }

    /// <summary>
    /// Create group
    /// </summary>
    /// <param name="dto">Group definition</param>
    [HttpPost]
    [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Create([FromBody] PropertyGroupDto dto)
    {
        var id = await _propertyGroupService.AddGroupAsync(dto);
        return Success(id);
    }

    /// <summary>
    /// Update group
    /// </summary>
    /// <param name="id">Group ID</param>
    /// <param name="dto">Group definition</param>
    [HttpPut("{id}")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Update(long id, [FromBody] PropertyGroupDto dto)
    {
        dto.Id = id;
        await _propertyGroupService.UpdateGroupAsync(dto);
        return Success(true);
    }

    /// <summary>
    /// Delete group
    /// </summary>
    /// <param name="id">Group ID</param>
    [HttpDelete("{id}")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Delete(long id)
    {
        await _propertyGroupService.DeleteGroupAsync(id);
        return Success(true);
    }

    /// <summary>
    /// Update group sort order
    /// </summary>
    /// <param name="sorts">Group ID to sort order mapping</param>
    [HttpPut("sort")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> UpdateSort([FromBody] Dictionary<long, int> sorts)
    {
        await _propertyGroupService.UpdateGroupSortAsync(sorts);
        return Success(true);
    }
}
