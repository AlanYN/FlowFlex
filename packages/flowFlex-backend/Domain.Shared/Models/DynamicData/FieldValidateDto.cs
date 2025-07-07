using System;
using FlowFlex.Domain.Shared.Enums.DynamicData;

namespace FlowFlex.Domain.Shared.Models.DynamicData;

public class FieldValidateDto
{
    /// <summary>
    /// ID of the validation settings
    /// </summary>
    public long ValidateId { get; set; }

    /// <summary>
    /// Data type of the field
    /// </summary>
    public DataType DataType { get; set; }

    #region Number

    /// <summary>
    /// Minimum value for number fields
    /// </summary>
    public long? NumberMinValue { get; set; }

    /// <summary>
    /// Maximum value for number fields
    /// </summary>
    public long? NumberMaxValue { get; set; }

    /// <summary>
    /// Number of decimal places for number fields
    /// </summary>
    public int? NumberDecimalPlaces { get; set; }

    #endregion

    #region Text

    /// <summary>
    /// Minimum length for text fields
    /// </summary>
    public int? TextMinLength { get; set; }

    /// <summary>
    /// Maximum length for text fields
    /// </summary>
    public int? TextMaxLength { get; set; }

    /// <summary>
    /// Indicates if only numbers are allowed in text fields
    /// </summary>
    public bool? TextOnlyNumber { get; set; }

    /// <summary>
    /// Indicates if special characters are not allowed in text fields
    /// </summary>
    public bool? TextNoSpecialCharAllowed { get; set; }

    #endregion

    #region DateTime

    /// <summary>
    /// Allows any date to be entered
    /// </summary>
    public bool? DateAnyDate { get; set; }

    /// <summary>
    /// Allows only future dates to be entered
    /// </summary>
    public bool? DateOnlyFutureDate { get; set; }

    /// <summary>
    /// Allows only past dates to be entered
    /// </summary>
    public bool? DateOnlyPastDate { get; set; }

    /// <summary>
    /// Indicates if a specific date range is set
    /// </summary>
    public bool? DateSpecificDateRange { get; set; }

    /// <summary>
    /// Start date of the specific date range
    /// </summary>
    public DateTimeOffset? DateStartDate { get; set; }

    /// <summary>
    /// End date of the specific date range
    /// </summary>
    public DateTimeOffset? DateEndDate { get; set; }

    /// <summary>
    /// Allows only Monday to Friday dates
    /// </summary>
    public bool? DateOnlyMToF { get; set; }

    #endregion

    #region FileList

    /// <summary>
    /// Maximum file count
    /// </summary>
    public int? MaxCount { get; set; }

    /// <summary>
    /// Maximum file length in MB
    /// </summary>
    public int? MaxFileLength { get; set; }

    #endregion
}
