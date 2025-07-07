using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Shared.Enums.Item;
using Item.Common.Lib.DataAnnotations;
using Item.Common.Lib.JsonConverts;
using FlowFlex.Domain.Shared.Enums.Item.Task;
using FlowFlex.Domain.Shared.Models;

namespace FlowFlex.Domain.Shared.Models
{
    /// <summary>
    /// Add Task
    /// </summary>
    public class TaskDto
    {
        /// <summary>
        /// Task ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Task Title
        /// </summary>
        [Required(ErrorMessage = "The Title cannot be empty.")]
        [StringLength(500, ErrorMessage = "The Title cannot be longer than 500 characters.")]
        public string Title { get; set; }

        /// <summary>
        /// Task Notes
        /// </summary>
        [StringLength(1000, ErrorMessage = "The Notes cannot be longer than 1000 characters.")]
        public string Notes { get; set; }

        /// <summary>
        /// Task Type
        /// </summary>
        [EnumValueValidate(typeof(ToDoEnum))]
        public ToDoEnum ToDo { get; set; }

        /// <summary>
        /// Task Assignment
        /// </summary>
        [Required(ErrorMessage = "The Assign To cannot be empty.")]
        public long AssignToId { get; set; }

        /// <summary>
        /// Task Priority
        /// </summary>
        [EnumValueValidate(typeof(PriorityEnum), false, ErrorMessage = "Please enter the correct Priority type.")]
        public PriorityEnum? Priority { get; set; }

        /// <summary>
        /// Task Expiration Date
        /// </summary>
        public DateTimeOffset? DateOfExpiration { get; set; }

        /// <summary>
        /// Related Information
        /// </summary>
        public List<TaskConnectDto> ConnectDetails { get; set; }

        /// <summary>
        /// Send Reminder Rule
        /// </summary>
        public SendReminderRuleEnum? SendReminderRule { get; set; }

        public string CreateBy { get; set; }

        public DateTimeOffset CreateDate { get; set; }

        public string ModifyBy { get; set; }

        public virtual DateTimeOffset ModifyDate { get; set; }

        /// <summary>
        /// Is Have Related Data
        /// </summary>
        public bool? IsRelatedData { get; set; }
    }

    /// <summary>
    /// Contains data related to the task
    /// </summary>
    public class TaskConnectDto
    {
        /// <summary>
        /// IDs
        /// </summary>
        public List<string> Ids { get; set; }

        /// <summary>
        /// Connection Type (TaskConnectTypeEnum)
        /// </summary>
        [EnumValueValidate(typeof(RelationalTypeEnum), ErrorMessage = "Please enter the correct connect type.")]
        public RelationalTypeEnum ConnectType { get; set; }
    }

    /// <summary>
    /// Task Search
    /// </summary>
    public class TaskSearchDto : SearchPageDto
    {
        /// <summary>
        /// Task Type
        /// </summary>
        public TaskTypeEnum? TaskType { get; set; }

        /// <summary>
        /// To-Do Type
        /// </summary>
        [EnumValueValidate(typeof(ToDoEnum), false, ErrorMessage = "Please enter the correct ToDo type.")]
        public ToDoEnum? ToDo { get; set; }

        /// <summary>
        /// Filter
        /// </summary>
        [EnumValueValidate(typeof(TaskFilterEnum), false, ErrorMessage = "Please enter the correct Filter type.")]
        public TaskFilterEnum? Filter { get; set; }

        /// <summary>
        /// Sort
        /// </summary>
        [EnumValueValidate(typeof(TaskSortEnum), false, ErrorMessage = "Please enter the correct Sort type.")]
        public TaskSortEnum? Sort { get; set; }

        public long? BusinessId { get; set; }

        public long? StageId { get; set; }
    }

    /// <summary>
    /// Task Response Entity
    /// </summary>
    public class TaskResponseDto
    {
        /// <summary>
        /// Task Identifier ID
        /// </summary>
        [JsonConverter(typeof(ValueToStringConverter))]
        public long Id { get; set; }

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
        public ToDoEnum? ToDo { get; set; }

        /// <summary>
        /// Assigned User ID
        /// </summary>
        [JsonConverter(typeof(ValueToStringConverter))]
        public long AssignToId { get; set; }

        /// <summary>
        /// Assigned User Name
        /// </summary>
        [JsonConverter(typeof(ValueToStringConverter))]
        public string AssignToName { get; set; }

        /// <summary>
        /// Priority
        /// </summary>
        public PriorityEnum? Priority { get; set; }

        /// <summary>
        /// Is Completed
        /// </summary>
        public bool IsCompleted { get; set; }

        public string CompletionNote { get; set; }

        public DateTimeOffset? CloseDate { get; set; }

        /// <summary>
        /// Expiration Date
        /// </summary>
        public DateTimeOffset? DateOfExpiration { get; set; }

        /// <summary>
        /// Task-related data (companies, contacts, etc.)
        /// </summary>
        public List<TaskConnectDetails> ConnectDetails { get; set; }

        /// <summary>
        /// Send Reminder Rule
        /// </summary>
        public SendReminderRuleEnum? SendReminderRule { get; set; }

        public UserPermissionsModel UserPermissions { get; set; } = new UserPermissionsModel();
        [JsonConverter(typeof(ValueToStringConverter))]
        public long TeamId { get; set; }
    }


    /// <summary>
    /// Task-related Data
    /// </summary>
    public class TaskConnectDetails
    {
        /// <summary>
        /// Primary Key ID (can be used to delete the association)
        /// </summary>
        [JsonConverter(typeof(ValueToStringConverter))]
        public long ConnectId { get; set; }

        /// <summary>
        /// Data ID
        /// </summary>
        public string DataId { get; set; }

        /// <summary>
        /// Name or Title
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Connection Type (TaskConnectTypeEnum description)
        /// </summary>
        public RelationalTypeEnum ConnectType { get; set; }
    }

    public class TaskOutPutDto
    {
        /// <summary>
        /// Task Identifier ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Task Title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Notes
        /// </summary>
        public string Notes { get; set; }

        public int? ToDo { get; set; }

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

        /// <summary>
        /// Task-related data  companies
        /// </summary>
        public List<TaskConnectDatas> ConnectCompanies { get; set; } = new List<TaskConnectDatas>();

        /// <summary>
        /// Task-related data contacts
        /// </summary>
        public List<TaskConnectDatas> ConnectContacts { get; set; } = new List<TaskConnectDatas>();

        /// <summary>
        /// Task-related data deals
        /// </summary>
        public List<TaskConnectDatas> ConnectDeals { get; set; } = new List<TaskConnectDatas>();

        /// <summary>
        /// User Permissions
        /// </summary>
        public UserPermissionsModel UserPermissions { get; set; } = new UserPermissionsModel();
    }

    public class TaskConnectDatas
    {
        /// <summary>
        /// Id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }
    }
}
