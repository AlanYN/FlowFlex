using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.IServices.DynamicData;
using FlowFlex.Application.Contracts.Dtos.DynamicData;
using FlowFlex.Application.Filter;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Domain.Shared.Models.DynamicData;
using Item.Internal.StandardApi.Response;
using System.Net;

namespace FlowFlex.WebApi.Controllers.DynamicData;

/// <summary>
/// Property (field definition) management API
/// </summary>
[ApiController]
[Route("ow/dynamic-data/v{version:apiVersion}/properties")]
[Display(Name = "dynamic-data-properties")]
[Asp.Versioning.ApiVersion("1.0")]
[Authorize]
public class PropertyController : Controllers.ControllerBase
{
    private readonly IPropertyService _propertyService;

    public PropertyController(IPropertyService propertyService)
    {
        _propertyService = propertyService;
    }

    /// <summary>
    /// Get all properties
    /// </summary>
    [HttpGet]
    [ProducesResponseType<SuccessResponse<List<DefineFieldDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetAll()
    {
        var data = await _propertyService.GetPropertyListAsync();
        return Success(data);
    }

    /// <summary>
    /// Get properties with pagination and filters
    /// </summary>
    [HttpPost("query")]
    [ProducesResponseType<SuccessResponse<PagedResult<DefineFieldDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Query([FromBody] PropertyQueryRequest request)
    {
        var data = await _propertyService.GetPropertyPagedListAsync(request);
        return Success(data);
    }

    /// <summary>
    /// Export properties to Excel
    /// </summary>
    [HttpPost("export-excel")]
    [ProducesResponseType(typeof(FileResult), 200)]
    public async Task<IActionResult> ExportToExcelAsync([FromBody] PropertyQueryRequest request)
    {
        var stream = await _propertyService.ExportToExcelAsync(request);
        var fileName = $"DynamicFields_{DateTimeOffset.Now:MMddyyyy_HHmmss}.xlsx";
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Export properties to Excel (GET method)
    /// </summary>
    [HttpGet("export-excel")]
    [ProducesResponseType(typeof(FileResult), 200)]
    public async Task<IActionResult> ExportToExcelAsync(
        [FromQuery] string? ids = null,
        [FromQuery] string? fieldName = null,
        [FromQuery] string? displayName = null,
        [FromQuery] int? dataType = null,
        [FromQuery] string? createBy = null,
        [FromQuery] string? modifyBy = null)
    {
        var request = new PropertyQueryRequest
        {
            Ids = ParseIds(ids),
            FieldName = fieldName,
            DisplayName = displayName,
            DataType = dataType.HasValue ? (Domain.Shared.Enums.DynamicData.DataType)dataType.Value : null,
            CreateBy = createBy,
            ModifyBy = modifyBy
        };

        var stream = await _propertyService.ExportToExcelAsync(request);
        var fileName = $"DynamicFields_{DateTimeOffset.Now:MMddyyyy_HHmmss}.xlsx";
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Parse comma-separated IDs string to list of long
    /// </summary>
    private static List<long>? ParseIds(string? ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
            return null;

        var result = new List<long>();
        foreach (var idStr in ids.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (long.TryParse(idStr.Trim(), out var id))
            {
                result.Add(id);
            }
        }
        return result.Count > 0 ? result : null;
    }

    /// <summary>
    /// Get property by ID
    /// </summary>
    /// <param name="id">Property ID</param>
    [HttpGet("{id}")]
    [ProducesResponseType<SuccessResponse<DefineFieldDto>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetById(long id)
    {
        var data = await _propertyService.GetPropertyByIdAsync(id);
        if (data == null)
        {
            return NotFound();
        }
        return Success(data);
    }

    /// <summary>
    /// Get properties by IDs (batch query)
    /// </summary>
    /// <param name="request">Batch query request containing property IDs</param>
    [HttpPost("batch")]
    [PortalAccess]
    [ProducesResponseType<SuccessResponse<List<DefineFieldDto>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetByIds([FromBody] BatchGetPropertiesRequest request)
    {
        if (request?.Ids == null || !request.Ids.Any())
        {
            return Success(new List<DefineFieldDto>());
        }
        var data = await _propertyService.GetPropertiesByIdsAsync(request.Ids);
        return Success(data);
    }

    /// <summary>
    /// Get property by name
    /// </summary>
    /// <param name="name">Property name</param>
    [HttpGet("by-name/{name}")]
    [ProducesResponseType<SuccessResponse<DefineFieldDto>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetByName(string name)
    {
        var data = await _propertyService.GetPropertyByNameAsync(name);
        if (data == null)
        {
            return NotFound();
        }
        return Success(data);
    }

    /// <summary>
    /// Create property
    /// </summary>
    /// <param name="dto">Property definition</param>
    [HttpPost]
    [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Create([FromBody] DefineFieldDto dto)
    {
        var id = await _propertyService.AddPropertyAsync(dto);
        return Success(id);
    }

    /// <summary>
    /// Update property
    /// </summary>
    /// <param name="id">Property ID</param>
    /// <param name="dto">Property definition</param>
    [HttpPut("{id}")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Update(long id, [FromBody] DefineFieldDto dto)
    {
        dto.Id = id;
        await _propertyService.UpdatePropertyAsync(dto);
        return Success(true);
    }

    /// <summary>
    /// Delete property
    /// </summary>
    /// <param name="id">Property ID</param>
    [HttpDelete("{id}")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Delete(long id)
    {
        await _propertyService.DeletePropertyAsync(id);
        return Success(true);
    }

    /// <summary>
    /// Move properties to group
    /// </summary>
    /// <param name="request">Move request</param>
    [HttpPost("move-to-group")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> MoveToGroup([FromBody] MoveToGroupRequest request)
    {
        await _propertyService.MovePropertyToGroupAsync(request.PropertyIds, request.GroupId);
        return Success(true);
    }

    /// <summary>
    /// Update property sort order
    /// </summary>
    /// <param name="sorts">Property ID to sort order mapping</param>
    [HttpPut("sort")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> UpdateSort([FromBody] Dictionary<long, int> sorts)
    {
        await _propertyService.UpdatePropertySortAsync(sorts);
        return Success(true);
    }

    /// <summary>
    /// Initialize default properties from static-field.json
    /// </summary>
    [HttpPost("initialize")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> InitializeDefaultProperties()
    {
        var result = await _propertyService.InitializeDefaultPropertiesAsync();
        return Success(result);
    }
}
