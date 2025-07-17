using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Entities.Base;
using SqlSugar;
using Newtonsoft.Json;

namespace FlowFlex.Domain.Entities.Action
{
    /// <summary>
    /// Action Definition Entity - Store basic information and configuration of Actions
    /// </summary>
    [SugarTable("ff_action_definitions")]
    public class ActionDefinition : EntityBaseCreateInfo
    {
        /// <summary>
        /// Action name
        /// </summary>
        [Required]
        [StringLength(100)]
        [SugarColumn(ColumnName = "action_name")]
        public string ActionName { get; set; } = string.Empty;

        /// <summary>
        /// Action type (Python, HttpApi, SendEmail)
        /// </summary>
        [Required]
        [StringLength(50)]
        [SugarColumn(ColumnName = "action_type")]
        public string ActionType { get; set; } = string.Empty;

        /// <summary>
        /// Action description
        /// </summary>
        [StringLength(500)]
        [SugarColumn(ColumnName = "description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Action configuration (JSON format, contains execution parameters)
        /// </summary>
        [SugarColumn(ColumnName = "action_config", ColumnDataType = "jsonb", IsJson = true)]
        public string ActionConfig { get; set; } = "{}";

        /// <summary>
        /// Whether this action is enabled
        /// </summary>
        [SugarColumn(ColumnName = "is_enabled")]
        public bool IsEnabled { get; set; } = true;
    }
} 