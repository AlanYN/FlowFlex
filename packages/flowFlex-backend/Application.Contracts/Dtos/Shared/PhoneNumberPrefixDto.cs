namespace FlowFlex.Application.Contracts.Dtos.Shared;

/// <summary>
/// Phone number prefix DTO
/// </summary>
public class PhoneNumberPrefixDto
{
    /// <summary>
    /// Country code (e.g., CN, US)
    /// </summary>
    public string CountryCode { get; set; }

    /// <summary>
    /// Dialing code with + prefix (e.g., +86, +1)
    /// </summary>
    public string DialingCode { get; set; }

    /// <summary>
    /// Description (country/region name)
    /// </summary>
    public string Description { get; set; }
}
