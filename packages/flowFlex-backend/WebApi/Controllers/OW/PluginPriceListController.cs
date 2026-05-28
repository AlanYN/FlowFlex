using System.ComponentModel.DataAnnotations;
using System.Net;
using FlowFlex.Application.Contracts.Dtos.OW.PluginPriceList;
using FlowFlex.Application.Contracts.IServices.OW;
using Item.Internal.StandardApi.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowFlex.WebApi.Controllers.OW;

[ApiController]
[Route("ow/plugin-price-lists/v{version:apiVersion}")]
[Authorize]
public class PluginPriceListController : Controllers.ControllerBase
{
    private readonly IPluginPriceListService _service;

    public PluginPriceListController(IPluginPriceListService service)
    {
        _service = service;
    }

    /// <summary>
    /// Get plugin price list by case code
    /// </summary>
    /// <param name="caseCode">Case code identifier</param>
    /// <returns>Plugin price list data</returns>
    [HttpGet]
    [ProducesResponseType<SuccessResponse<PluginPriceListOutputDto>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetAsync([FromQuery, Required] string caseCode)
    {
        var result = await _service.GetAsync(caseCode);
        return Success(result);
    }

    /// <summary>
    /// Save plugin price list (create or update)
    /// </summary>
    /// <param name="input">Plugin price list data to save</param>
    /// <returns>Save result</returns>
    [HttpPost]
    [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> SaveAsync([FromBody] PluginPriceListInputDto input)
    {
        var result = await _service.SaveAsync(input);
        return Success(result);
    }

    /// <summary>
    /// Submit plugin price list for approval
    /// </summary>
    /// <param name="input">Plugin price list submission data</param>
    /// <returns>Submission result</returns>
    [HttpPost("submit")]
    [ProducesResponseType<SuccessResponse<object>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> SubmitAsync([FromBody] PluginPriceListSubmitDto input)
    {
        var result = await _service.SubmitAsync(input);
        return Success(result);
    }
}
