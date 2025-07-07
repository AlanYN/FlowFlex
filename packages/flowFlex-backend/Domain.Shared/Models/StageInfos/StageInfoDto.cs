using System;

namespace FlowFlex.Domain.Shared.Models.StageInfos;

public class StageInfoDto
{
    public long Id { get; set; }

    /// <summary>
    /// pipeline Id
    /// </summary>
    public long PipelineId { get; set; }

    /// <summary>
    /// Stage ID
    /// </summary>
    public long PipelineStageId { get; set; }

    /// <summary>
    /// Stage start time
    /// </summary>
    public DateTimeOffset? StageStartDate { get; set; }

    /// <summary>
    /// Expected completion time
    /// </summary>
    public DateTimeOffset? ExpectedCompletionDate { get; set; }

    /// <summary>
    /// Actual completion time
    /// </summary>
    public DateTimeOffset? ActualCompletionDate { get; set; }

    /// <summary>
    /// Associated business data ID
    /// </summary>
    public long BusinessId { get; set; }

    /// <summary>
    /// Whether completed
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Lifecycle stage ID
    /// </summary>
    public long? LifeCycleStageId { get; set; }

    public long? StageOwnerId { get; set; }

    public DateTimeOffset ModifyDate { get; set; }
}
