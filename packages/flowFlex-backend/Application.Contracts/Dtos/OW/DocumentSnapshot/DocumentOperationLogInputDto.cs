using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.DocumentSnapshot;

/// <summary>
/// Document operation log input DTO — reported from frontend on every MUTATION event
/// </summary>
public class DocumentOperationLogInputDto
{
    /// <summary>
    /// Associated document unit_id
    /// </summary>
    [Required]
    [StringLength(100)]
    public string UnitId { get; set; }

    /// <summary>
    /// Operator user ID
    /// </summary>
    [StringLength(100)]
    public string UserId { get; set; }

    /// <summary>
    /// Operator user display name
    /// </summary>
    [StringLength(100)]
    public string UserName { get; set; }

    /// <summary>
    /// Operation timestamp in milliseconds (Date.now())
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// Univer MutationId (e.g. "sheet.mutation.set-range-values")
    /// </summary>
    [StringLength(200)]
    public string MutationId { get; set; }

    /// <summary>
    /// Mutation params JSON — passed through as-is
    /// </summary>
    public string ParamsJson { get; set; }

    /// <summary>
    /// Collaborative revision number at the time of this operation
    /// </summary>
    public int Revision { get; set; }
}
