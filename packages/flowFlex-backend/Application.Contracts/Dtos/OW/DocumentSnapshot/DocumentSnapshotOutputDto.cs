using System;
using Newtonsoft.Json;
using Item.Common.Lib.JsonConverts;

namespace FlowFlex.Application.Contracts.Dtos.OW.DocumentSnapshot;

/// <summary>
/// Document snapshot output DTO
/// </summary>
public class DocumentSnapshotOutputDto
{
    /// <summary>
    /// Primary key ID (serialized as string for JS precision)
    /// </summary>
    [JsonConverter(typeof(ValueToStringConverter))]
    public long Id { get; set; }

    /// <summary>
    /// Univer document unique ID (UUID)
    /// </summary>
    public string UnitId { get; set; }

    /// <summary>
    /// Document title
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Document type: Sheet / Doc / Slide
    /// </summary>
    public string DocType { get; set; }

    /// <summary>
    /// Document data — inlined as a JSON object so the frontend receives it
    /// as a plain object (no JSON.parse needed).
    /// Newtonsoft.Json serialises JToken/object inline without extra string escaping.
    /// </summary>
    [JsonProperty("dataJson")]
    public object DataJson { get; set; }

    /// <summary>
    /// Revision number for collaborative editing
    /// </summary>
    public int Revision { get; set; }

    /// <summary>
    /// Associated business entity type
    /// </summary>
    public string EntityType { get; set; }

    /// <summary>
    /// Associated business entity ID
    /// </summary>
    [JsonConverter(typeof(ValueToStringConverter))]
    public long EntityId { get; set; }

    /// <summary>
    /// Created at
    /// </summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>
    /// Created by
    /// </summary>
    public string CreateBy { get; set; }

    /// <summary>
    /// Modified at
    /// </summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>
    /// Modified by
    /// </summary>
    public string ModifyBy { get; set; }
}
