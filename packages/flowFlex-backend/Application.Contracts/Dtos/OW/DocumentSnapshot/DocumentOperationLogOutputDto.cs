using System;
using Newtonsoft.Json;
using Item.Common.Lib.JsonConverts;

namespace FlowFlex.Application.Contracts.Dtos.OW.DocumentSnapshot;

/// <summary>
/// Document operation log output DTO
/// </summary>
public class DocumentOperationLogOutputDto
{
    /// <summary>
    /// Log ID
    /// </summary>
    [JsonConverter(typeof(ValueToStringConverter))]
    public long Id { get; set; }

    /// <summary>
    /// Associated document unit_id
    /// </summary>
    public string UnitId { get; set; }

    /// <summary>
    /// Operator user ID
    /// </summary>
    public string OperatorUserId { get; set; }

    /// <summary>
    /// Operator user display name
    /// </summary>
    public string OperatorUserName { get; set; }

    /// <summary>
    /// Operation timestamp (milliseconds)
    /// </summary>
    public long OpTimestamp { get; set; }

    /// <summary>
    /// Univer MutationId
    /// </summary>
    public string MutationId { get; set; }

    /// <summary>
    /// Mutation params JSON
    /// </summary>
    public string ParamsJson { get; set; }

    /// <summary>
    /// Revision number at time of operation
    /// </summary>
    public int Revision { get; set; }

    /// <summary>
    /// Created at
    /// </summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>
    /// Cell values before this mutation was applied (backend-captured).
    /// Same structure as cellValue in ParamsJson: { rowIndex: { colIndex: oldValue|null } }.
    /// Only present for set-range-values mutations.
    /// </summary>
    public string OldValuesJson { get; set; }
}
