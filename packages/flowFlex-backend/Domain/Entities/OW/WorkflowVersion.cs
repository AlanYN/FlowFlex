using SqlSugar;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Entities.Base;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Workflow Version History Entity
    /// </summary>
    [SugarTable("ff_workflow_version")]
    public class WorkflowVersion : OwEntityBase
    {
        /// <summary>
        /// Original Workflow ID
        /// </summary>
        [SugarColumn(ColumnName = "original_workflow_id")]
        public long OriginalWorkflowId { get; set; }

        /// <summary>
        /// Workflow Name
        /// </summary>
        [SugarColumn(ColumnName = "name", Length = 100)]
        public string Name { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        [SugarColumn(ColumnName = "description", Length = 500)]
        public string Description { get; set; }

        /// <summary>
        /// Is Default
        /// </summary>
        [SugarColumn(ColumnName = "is_default")]
        public bool IsDefault { get; set; }

        /// <summary>
        /// Status
        /// </summary>
        [SugarColumn(ColumnName = "status", Length = 50)]
        public string Status { get; set; }

        /// <summary>
        /// Start Date
        /// </summary>
        [SugarColumn(ColumnName = "start_date")]
        public DateTimeOffset StartDate { get; set; }

        /// <summary>
        /// End Date
        /// </summary>
        [SugarColumn(ColumnName = "end_date")]
        public DateTimeOffset? EndDate { get; set; }

        /// <summary>
        /// Version Number
        /// </summary>
        [SugarColumn(ColumnName = "version")]
        public int Version { get; set; }

        /// <summary>
        /// Is Active
        /// </summary>
        [SugarColumn(ColumnName = "is_active")]
        public bool IsActive { get; set; }

        /// <summary>
        /// Configuration JSON
        /// </summary>
        [SugarColumn(ColumnName = "config_json", Length = 2000)]
        public string ConfigJson { get; set; }

        /// <summary>
        /// Change Reason
        /// </summary>
        [SugarColumn(ColumnName = "change_reason", Length = 500)]
        public string ChangeReason { get; set; }

        /// <summary>
        /// Change Type (Created, Updated, Deleted)
        /// </summary>
        [SugarColumn(ColumnName = "change_type", Length = 50)]
        public string ChangeType { get; set; }
    }
}
