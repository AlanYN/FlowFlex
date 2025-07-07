namespace FlowFlex.Domain.Shared.Enums.DynamicData;

public enum NumberTypeFormat
{
    /// <summary>
    /// No formatting
    /// </summary>
    None = 0,

    /// <summary>
    /// Formatted number
    /// Format property as number
    /// </summary>
    NumberStandard = 1,

    /// <summary>
    /// Unformatted number
    /// Remove formatting from property
    /// </summary>
    NumberNoFormat = 2,

    /// <summary>
    /// Currency
    /// Format property as currency
    /// </summary>
    NumberCurrency = 3,

    /// <summary>
    /// Percentage
    /// Format property as percentage
    /// </summary>
    NumberPercentage = 4,
}
