using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Application.Contracts.IServices.DynamicData;
using FlowFlex.Domain.Shared.Models.DynamicData;
using Item.Internal.StandardApi.Response;
using System.Net;

namespace FlowFlex.WebApi.Controllers.DynamicData;

/// <summary>
/// Dynamic data management API - Business data CRUD operations
/// </summary>
[ApiController]
[Route("ow/dynamic-data/v{version:apiVersion}")]
[Display(Name = "dynamic-data")]
[Asp.Versioning.ApiVersion("1.0")]
[Authorize]
public class DynamicDataController : Controllers.ControllerBase
{
    private readonly IBusinessDataService _businessDataService;
    private const int DefaultModuleId = 0;

    public DynamicDataController(IBusinessDataService businessDataService)
    {
        _businessDataService = businessDataService;
    }

    /// <summary>
    /// Get business data by ID
    /// </summary>
    /// <param name="id">Business data ID</param>
    [HttpGet("{id}")]
    [ProducesResponseType<SuccessResponse<DynamicDataObject>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetById(long id)
    {
        var data = await _businessDataService.GetBusinessDataObjectAsync(id);
        if (data == null)
        {
            return NotFound();
        }
        return Success(data);
    }

    /// <summary>
    /// Get business data list by IDs
    /// </summary>
    /// <param name="ids">Business data IDs</param>
    [HttpPost("batch")]
    [ProducesResponseType<SuccessResponse<List<DynamicDataObject>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetByIds([FromBody] List<long> ids)
    {
        var data = await _businessDataService.GetBusinessDataObjectListAsync(ids);
        return Success(data);
    }

    /// <summary>
    /// Create business data
    /// </summary>
    /// <param name="request">Business data request</param>
    [HttpPost]
    [ProducesResponseType<SuccessResponse<long>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Create([FromBody] CreateBusinessDataRequest request)
    {
        var data = new DynamicDataObject(DefaultModuleId);
        if (request.Fields != null)
        {
            foreach (var field in request.Fields)
            {
                data.Add(new FieldDataItem
                {
                    FieldName = field.FieldName,
                    Value = field.Value,
                    DataType = field.DataType
                });
            }
        }
        data.InternalData = request.InternalData;

        var id = await _businessDataService.CreateBusinessDataAsync(data);
        return Success(id);
    }

    /// <summary>
    /// Update business data
    /// </summary>
    /// <param name="id">Business data ID</param>
    /// <param name="request">Business data request</param>
    [HttpPut("{id}")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateBusinessDataRequest request)
    {
        var data = new DynamicDataObject(DefaultModuleId) { BusinessId = id };
        if (request.Fields != null)
        {
            foreach (var field in request.Fields)
            {
                data.Add(new FieldDataItem
                {
                    FieldName = field.FieldName,
                    Value = field.Value,
                    DataType = field.DataType
                });
            }
        }
        data.InternalData = request.InternalData;

        await _businessDataService.UpdateBusinessDataAsync(data);
        return Success(true);
    }

    /// <summary>
    /// Update specific fields of business data
    /// </summary>
    /// <param name="id">Business data ID</param>
    /// <param name="fields">Fields to update</param>
    [HttpPatch("{id}")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> UpdateFields(long id, [FromBody] Dictionary<string, object?> fields)
    {
        await _businessDataService.UpdateBusinessDataAsync(id, fields);
        return Success(true);
    }

    /// <summary>
    /// Delete business data
    /// </summary>
    /// <param name="id">Business data ID</param>
    [HttpDelete("{id}")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> Delete(long id)
    {
        await _businessDataService.DeleteBusinessDataAsync(id);
        return Success(true);
    }

    /// <summary>
    /// Batch delete business data
    /// </summary>
    /// <param name="ids">Business data IDs</param>
    [HttpDelete("batch")]
    [ProducesResponseType<SuccessResponse<bool>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> BatchDelete([FromBody] List<long> ids)
    {
        await _businessDataService.BatchDeleteBusinessDataAsync(ids);
        return Success(true);
    }
}

/// <summary>
/// Create business data request
/// </summary>
public class CreateBusinessDataRequest
{
    /// <summary>
    /// Field values
    /// </summary>
    public List<FieldValueRequest>? Fields { get; set; }

    /// <summary>
    /// Internal extension data
    /// </summary>
    public Newtonsoft.Json.Linq.JObject? InternalData { get; set; }
}

/// <summary>
/// Update business data request
/// </summary>
public class UpdateBusinessDataRequest
{
    /// <summary>
    /// Field values
    /// </summary>
    public List<FieldValueRequest>? Fields { get; set; }

    /// <summary>
    /// Internal extension data
    /// </summary>
    public Newtonsoft.Json.Linq.JObject? InternalData { get; set; }
}

/// <summary>
/// Field value request
/// </summary>
public class FieldValueRequest
{
    /// <summary>
    /// Field name
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Field value
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Data type
    /// </summary>
    public FlowFlex.Domain.Shared.Enums.DynamicData.DataType DataType { get; set; }
}
