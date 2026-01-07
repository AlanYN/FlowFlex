using System.Net;
using FlowFlex.Application.Contracts.Dtos.Shared;
using FlowFlex.Domain.Repository.Shared;
using Item.Internal.StandardApi.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowFlex.WebApi.Controllers.Shared;

/// <summary>
/// Dictionary data controller for shared/common data
/// </summary>
[ApiController]
[Route("shared/v{version:apiVersion}/dictionary")]
[Asp.Versioning.ApiVersion("1.0")]
[Authorize]
public class DictionaryController : Controllers.ControllerBase
{
    private readonly IPhoneNumberPrefixRepository _phoneNumberPrefixRepository;

    public DictionaryController(IPhoneNumberPrefixRepository phoneNumberPrefixRepository)
    {
        _phoneNumberPrefixRepository = phoneNumberPrefixRepository;
    }

    /// <summary>
    /// Get phone number prefixes for dropdown list
    /// </summary>
    /// <returns>List of phone number prefixes with key-value format</returns>
    [HttpGet("phone-number-prefixes")]
    [ProducesResponseType<SuccessResponse<IEnumerable<object>>>((int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetPhoneNumberPrefixes(CancellationToken cancellationToken)
    {
        var data = await _phoneNumberPrefixRepository.GetAllAsync(cancellationToken);

        var result = data?.Select(x => new
        {
            key = x.Id.ToString(),
            value = new PhoneNumberPrefixDto
            {
                DialingCode = $"+{x.DialingCode}",
                Description = x.Description,
                CountryCode = x.CountryCode
            }
        }) ?? [];

        return Success(result);
    }
}
