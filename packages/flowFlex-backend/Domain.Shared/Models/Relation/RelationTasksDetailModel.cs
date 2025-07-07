using Newtonsoft.Json;
using System;
using Item.Common.Lib.JsonConverts;

namespace FlowFlex.Domain.Shared.Models.Relation
{
    public class RelationTasksDetailModel
    {
        /// <summary>
        /// Binding relationship primary key ID for unbinding operation
        /// </summary>
        [JsonConverter(typeof(ValueToStringConverter))]
        public long Id { get; set; }

        /// <summary>
        /// Task primary key ID
        /// </summary>
        [JsonConverter(typeof(ValueToStringConverter))]
        public long TaskId { get; set; }

        /// <summary>
        /// Task title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Task notes
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Task type
        /// </summary>
        public int? ToDo { get; set; }

        /// <summary>
        /// Task assigned user ID
        /// </summary>
        public long? AssignToId { get; set; }

        /// <summary>
        /// Task due date
        /// </summary>
        public DateTimeOffset? DueDate { get; set; }

        /// <summary>
        /// Priority
        /// </summary>
        public int? Priority { get; set; }

        /// <summary>
        /// Whether completed
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Create time
        /// </summary>
        public DateTimeOffset? CreateDate { get; set; }

        /// <summary>
        /// upcoming
        /// </summary>
        public bool UpComing { get; set; } = false;
        public long TeamId { get; set; }
        public UserPermissionsModel UserPermissions { get; set; } = new UserPermissionsModel();
    }
}
