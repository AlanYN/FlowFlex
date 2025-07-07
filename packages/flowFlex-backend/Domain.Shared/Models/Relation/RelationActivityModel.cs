using Item.Common.Lib.JsonConverts;
using Newtonsoft.Json;
using System;
using FlowFlex.Domain.Shared.Enums.Item;
using FlowFlex.Domain.Shared.Enums.Item.Task;

namespace FlowFlex.Domain.Shared.Models.Relation
{
    public class RelationActivityModel
    {
        /// <summary>
        /// Binding relationship primary key ID for unbinding operation
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Task or Note primary key ID
        /// </summary>
        public long TagerId { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        public RelationalTypeEnum? TagerType { get; set; }

        /// <summary>
        /// Task title - task
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Task notes - task, note
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Task type - task
        /// </summary>
        public int? ToDo { get; set; }

        /// <summary>
        /// Task assigned user ID - task
        /// </summary>
        public long? AssignToId { get; set; }

        /// <summary>
        /// Task due date - task
        /// </summary>
        public DateTimeOffset? DueDate { get; set; }

        /// <summary>
        /// Priority - task
        /// </summary>
        public int? Priority { get; set; }

        /// <summary>
        /// Whether completed - task
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Creation time - task, note
        /// </summary>
        public DateTimeOffset? CreateDate { get; set; }

        /// <summary>
        /// Creator - task, note
        /// </summary>
        public string CreateBy { get; set; }

        /// <summary>
        /// Priority name - task
        /// </summary>
        public string PriorityName { get; set; }

        /// <summary>
        /// Task type - task
        /// </summary>
        public string ToDoName { get; set; }

        /// <summary>
        /// Task, note sort date
        /// </summary>
        public DateTimeOffset? OrderByDate { get; set; }
        /// <summary>
        /// Send reminder rule
        /// </summary>
        public SendReminderRuleEnum? SendReminderRule { get; set; }

        public string AssignName { get; set; }

        /// <summary>
        /// SourceId
        /// </summary>
        public string SourceId { get; set; }

        /// <summary>
        /// SourceName
        /// </summary>
        public string SourceName { get; set; }

        /// <summary>
        /// SourceType
        /// </summary>
        public RelationalTypeEnum SourceType { get; set; }

        /// <summary>
        /// Comment Count
        /// </summary>
        public int CommentCount { get; set; }

        /// <summary>
        /// CreateUserId
        /// </summary>
        public long CreateUserId { get; set; }
        /// <summary>
        /// ModifyBy
        /// </summary>
        public string ModifyBy { get; set; }
        [JsonConverter(typeof(ValueToStringConverter))]
        public long TeamId { get; set; }

        public UserPermissionsModel UserPermissions { get; set; } = new UserPermissionsModel();

        /// <summary>
        /// ModifyDate
        /// </summary>
        public DateTimeOffset? ModifyDate { get; set; }

        public DateTimeOffset? CloseDate { get; set; }

        public string CompletionNote { get; set; }

        /// <summary>
        /// ModifyUserId
        /// </summary>
        public long ModifyUserId { get; set; }

        public long? PipelineStageId { get; set; }

        public long? PipelineId { get; set; }
    }
}
