using Item.Common.Lib.JsonConverts;
using System;
using System.Collections.Generic;
using FlowFlex.Domain.Shared.Enums.Item;

namespace FlowFlex.Domain.Shared.Models.Item
{
    public class TaskModel
    {
        /// <summary>
        /// Task Identifier ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Task Title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Notes
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        public string TaskType { get; set; }

        /// <summary>
        /// Assigned User Id
        /// </summary>
        public long? AssignToId { get; set; }

        /// <summary>
        /// Assigned User Name
        /// </summary>
        public string AssignToName { get; set; }

        /// <summary>
        /// Is Completed
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Expiration Date
        /// </summary>
        public DateTimeOffset? DateOfExpiration { get; set; }

        /// <summary>
        /// Create By
        /// </summary>
        public string CreateBy { get; set; }

        /// <summary>
        /// Create Date
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        public string ModifyBy { get; set; }

        /// <summary>
        /// Modify Date
        /// </summary>
        public DateTimeOffset ModifyDate { get; set; }

        /// <summary>
        /// TeamId
        /// </summary>
        public long TeamId { get; set; }

        public DateTimeOffset? CloseDate { get; set; }

        public string CompletionNote { get; set; }
    }
}
