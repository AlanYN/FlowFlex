using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.DocumentSnapshot;

/// <summary>
/// Document snapshot create/update input DTO
/// </summary>
public class DocumentSnapshotInputDto
{
    /// <summary>
    /// Document title
    /// </summary>
    [StringLength(200)]
    public string Title { get; set; }

    /// <summary>
    /// Document type: Sheet / Doc / Slide
    /// </summary>
    [StringLength(20)]
    public string DocType { get; set; } = "Sheet";

    /// <summary>
    /// Document data JSON (IWorkbookData / IDocumentData).
    /// Passed through as-is; backend does not parse.
    /// </summary>
    public string DataJson { get; set; }

    /// <summary>
    /// Associated business entity type (optional, e.g. "Onboarding")
    /// </summary>
    [StringLength(50)]
    public string EntityType { get; set; }

    /// <summary>
    /// Associated business entity ID (optional)
    /// </summary>
    public long EntityId { get; set; }
}
