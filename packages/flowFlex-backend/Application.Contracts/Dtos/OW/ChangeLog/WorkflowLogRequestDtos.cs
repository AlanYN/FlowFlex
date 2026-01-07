using System.ComponentModel.DataAnnotations;

namespace FlowFlex.Application.Contracts.Dtos.OW.ChangeLog;

/// <summary>
/// Workflow creation log request
/// </summary>
public class WorkflowCreateLogRequest
{
    [Required]
    public long WorkflowId { get; set; }

    [Required]
    [StringLength(200)]
    public string WorkflowName { get; set; }

    [StringLength(1000)]
    public string WorkflowDescription { get; set; }

    public string ExtendedData { get; set; }
}

/// <summary>
/// Workflow update log request
/// </summary>
public class WorkflowUpdateLogRequest
{
    [Required]
    public long WorkflowId { get; set; }

    [Required]
    [StringLength(200)]
    public string WorkflowName { get; set; }

    public string BeforeData { get; set; }

    public string AfterData { get; set; }

    public List<string> ChangedFields { get; set; }

    public string ExtendedData { get; set; }
}

/// <summary>
/// Workflow deletion log request
/// </summary>
public class WorkflowDeleteLogRequest
{
    [Required]
    public long WorkflowId { get; set; }

    [Required]
    [StringLength(200)]
    public string WorkflowName { get; set; }

    [StringLength(500)]
    public string Reason { get; set; }

    public string ExtendedData { get; set; }
}

/// <summary>
/// Workflow publish log request
/// </summary>
public class WorkflowPublishLogRequest
{
    [Required]
    public long WorkflowId { get; set; }

    [Required]
    [StringLength(200)]
    public string WorkflowName { get; set; }

    [StringLength(50)]
    public string Version { get; set; }

    public string ExtendedData { get; set; }
}

/// <summary>
/// Workflow unpublish log request
/// </summary>
public class WorkflowUnpublishLogRequest
{
    [Required]
    public long WorkflowId { get; set; }

    [Required]
    [StringLength(200)]
    public string WorkflowName { get; set; }

    [StringLength(500)]
    public string Reason { get; set; }

    public string ExtendedData { get; set; }
}

/// <summary>
/// Workflow activation log request
/// </summary>
public class WorkflowActivateLogRequest
{
    [Required]
    public long WorkflowId { get; set; }

    [Required]
    [StringLength(200)]
    public string WorkflowName { get; set; }

    public string ExtendedData { get; set; }
}

/// <summary>
/// Workflow deactivation log request
/// </summary>
public class WorkflowDeactivateLogRequest
{
    [Required]
    public long WorkflowId { get; set; }

    [Required]
    [StringLength(200)]
    public string WorkflowName { get; set; }

    [StringLength(500)]
    public string Reason { get; set; }

    public string ExtendedData { get; set; }
}
