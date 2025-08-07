using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowFlex.Application.Contracts.Dtos.OW.Questionnaire;

/// <summary>
/// Questionnaire query request DTO
/// </summary>
public class QuestionnaireQueryRequest
{
    /// <summary>
    /// Page number
    /// </summary>
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Filter by questionnaire name (optional, supports comma-separated values)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Filter by workflow ID
    /// </summary>
    public long? WorkflowId { get; set; }

    /// <summary>
    /// Filter by stage ID
    /// </summary>
    public long? StageId { get; set; }



    /// <summary>
    /// Filter by active status
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Sort field
    /// </summary>
    public string? SortField { get; set; } = "CreateDate";

    /// <summary>
    /// Sort direction (asc/desc)
    /// </summary>
    public string? SortDirection { get; set; } = "desc";

    /// <summary>
    /// Get questionnaire names as list (splits comma-separated values)
    /// </summary>
    public List<string> GetNamesList()
    {
        if (string.IsNullOrEmpty(Name))
            return new List<string>();

        return Name.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(name => name.Trim())
                  .Where(name => !string.IsNullOrEmpty(name))
                  .ToList();
    }
}