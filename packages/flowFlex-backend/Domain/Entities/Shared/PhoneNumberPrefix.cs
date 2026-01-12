using SqlSugar;

namespace FlowFlex.Domain.Entities.Shared;

/// <summary>
/// Phone number prefix entity (master data, no tenant isolation)
/// </summary>
[SugarTable("phone_number_prefixes", TableDescription = "Phone number prefixes")]
public class PhoneNumberPrefix
{
    /// <summary>
    /// Primary key ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true)]
    public long Id { get; set; }

    /// <summary>
    /// Country/Region name
    /// </summary>
    [SugarColumn(ColumnName = "country_name", Length = 100)]
    public string CountryName { get; set; }

    /// <summary>
    /// Country/Region code (e.g., CN, US)
    /// </summary>
    [SugarColumn(ColumnName = "country_code", Length = 100)]
    public string CountryCode { get; set; }

    /// <summary>
    /// Dialing code (e.g., 86, 1)
    /// </summary>
    [SugarColumn(ColumnName = "dialing_code", Length = 100)]
    public string DialingCode { get; set; }

    /// <summary>
    /// Description/Remarks
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 100)]
    public string Description { get; set; }
}
