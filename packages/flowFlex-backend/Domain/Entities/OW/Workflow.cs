using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Workflow Entity
    /// </summary>
    [SugarTable("ff_workflow")]
    public class Workflow : EntityBaseCreateInfo
    {
        /// <summary>
        /// Workflow Name
        /// </summary>
        
        [StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Workflow Description
        /// </summary>
        [StringLength(500)]
        public string Description { get; set; }

        /// <summary>
        /// Is Default Workflow
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Workflow Status (active/inactive)
        /// </summary>
        [StringLength(20)]
        public string Status { get; set; } = "active";

        /// <summary>
        /// Start Date
        /// </summary>
        [SugarColumn(ColumnName = "start_date")]
        public DateTimeOffset StartDate { get; set; }

        /// <summary>
        /// End Date (nullable, default workflow cannot be filled)
        /// </summary>
        [SugarColumn(ColumnName = "end_date")]
        public DateTimeOffset? EndDate { get; set; }

        /// <summary>
        /// Version Number
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// Is Active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// JSON for workflow configuration (stages, settings, etc.)
        /// </summary>
        [StringLength(2000)]
        [SugarColumn(ColumnName = "config_json")]
        public string ConfigJson { get; set; }

        /// <summary>
        /// Associated Stage Collection
        /// </summary>
        [Navigate(NavigateType.OneToMany, nameof(Stage.WorkflowId))]
        public List<Stage> Stages { get; set; }
    }
}
