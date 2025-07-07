using System.Collections.Generic;

namespace FlowFlex.Domain.Shared.Models.Settings;

public class PipelineConfigModel
{
    /// <summary>
    /// Whether enabled
    /// </summary>
    public bool Enable { get; set; }

    /// <summary>
    /// Rules list
    /// </summary>
    public PipelineStageRuleConfigModel Rules { get; set; }
}

public class PipelineStageRuleConfigModel
{
    /// <summary>
    /// Whether all tasks must be completed
    /// </summary>
    public bool AllTaskFinish { get; set; }

    /// <summary>
    /// Whether stage skipping is not allowed
    /// </summary>
    public bool NoAllowSkipStage { get; set; }

    /// <summary>
    /// Whether rollback is not allowed
    /// </summary>
    public bool NoAllowBack { get; set; }
}
