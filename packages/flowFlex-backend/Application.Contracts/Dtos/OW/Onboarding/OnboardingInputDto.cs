using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FlowFlex.Domain.Shared.Enums.OW;
using Newtonsoft.Json;

namespace FlowFlex.Application.Contracts.Dtos.OW.Onboarding
{
    /// <summary>
    /// Onboarding输入DTO
    /// </summary>
    public class OnboardingInputDto
    {
        /// <summary>
        /// 所属Workflow主键ID（可选，不填时后端自动选择默认工作流）
        /// </summary>
        public long? WorkflowId { get; set; }

        /// <summary>
        /// 当前所在Stage主键ID
        /// </summary>
        public long? CurrentStageId { get; set; }

        /// <summary>
        /// 客户/线索ID
        /// </summary>
        [StringLength(100)]
        public string LeadId { get; set; }

        /// <summary>
        /// 客户/线索名称
        /// </summary>
        [StringLength(200)]
        public string LeadName { get; set; }

        /// <summary>
        /// 客户/线索邮箱
        /// </summary>
        [StringLength(200)]
        public string LeadEmail { get; set; }

        /// <summary>
        /// 客户/线索电话
        /// </summary>
        [StringLength(50)]
        public string LeadPhone { get; set; }

        /// <summary>
        /// 联系人姓名
        /// </summary>
        [StringLength(200)]
        public string ContactPerson { get; set; }

        /// <summary>
        /// 联系人邮箱（可选，格式验证会在非空时进行）
        /// </summary>
        [StringLength(200)]
        public string ContactEmail { get; set; }

        /// <summary>
        /// CRM Lead的Life Cycle Stage ID
        /// </summary>
        [JsonConverter(typeof(NullableLongConverter))]
        public long? LifeCycleStageId { get; set; }

        /// <summary>
        /// CRM Lead的Life Cycle Stage名称
        /// </summary>
        [StringLength(100)]
        public string LifeCycleStageName { get; set; }

        /// <summary>
        /// Onboarding状态（Started/InProgress/Completed/Paused/Cancelled）
        /// </summary>
        [StringLength(20)]
        public string Status { get; set; } = "Started";

        /// <summary>
        /// 开始日期
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DateTimeOffset? StartDate { get; set; }

        /// <summary>
        /// 预计完成日期
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DateTimeOffset? EstimatedCompletionDate { get; set; }

        /// <summary>
        /// 当前负责人ID
        /// </summary>
        public long? CurrentAssigneeId { get; set; }

        /// <summary>
        /// 当前负责人姓名
        /// </summary>
        [StringLength(100)]
        public string CurrentAssigneeName { get; set; }

        /// <summary>
        /// 当前负责团队
        /// </summary>
        [StringLength(100)]
        public string CurrentTeam { get; set; }

        /// <summary>
        /// Stage更新人ID
        /// </summary>
        public long? StageUpdatedById { get; set; }

        /// <summary>
        /// Stage更新人姓名
        /// </summary>
        [StringLength(100)]
        public string StageUpdatedBy { get; set; }

        /// <summary>
        /// Stage更新人邮箱
        /// </summary>
        [StringLength(200)]
        public string StageUpdatedByEmail { get; set; }

        /// <summary>
        /// Stage更新时间
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DateTimeOffset? StageUpdatedTime { get; set; }

        /// <summary>
        /// 当前Stage开始时间
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DateTimeOffset? CurrentStageStartTime { get; set; }

        /// <summary>
        /// 优先级（Low/Medium/High/Critical）
        /// </summary>
        [StringLength(20)]
        public string Priority { get; set; } = "Medium";

        /// <summary>
        /// 是否已设置优先级
        /// </summary>
        public bool IsPrioritySet { get; set; } = false;

        /// <summary>
        /// Ownership - User ID who owns this onboarding
        /// </summary>
        public long? Ownership { get; set; }

        /// <summary>
        /// Ownership Name - User name who owns this onboarding
        /// </summary>
        [StringLength(100)]
        public string OwnershipName { get; set; }

        /// <summary>
        /// Ownership Email - User email who owns this onboarding
        /// </summary>
        [StringLength(200)]
        public string OwnershipEmail { get; set; }

        /// <summary>
        /// 动态扩展字段
        /// </summary>
        public string CustomFieldsJson { get; set; }

        /// <summary>
        /// 备注信息
        /// </summary>
        [StringLength(1000)]
        public string Notes { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// View Permission Subject Type - Team or User based view permissions
        /// </summary>
        public PermissionSubjectTypeEnum ViewPermissionSubjectType { get; set; } = PermissionSubjectTypeEnum.Team;

        /// <summary>
        /// Operate Permission Subject Type - Team or User based operate permissions
        /// </summary>
        public PermissionSubjectTypeEnum OperatePermissionSubjectType { get; set; } = PermissionSubjectTypeEnum.Team;

        /// <summary>
        /// View Permission Mode - Public/VisibleToTeams/InvisibleToTeams/Private
        /// </summary>
        public ViewPermissionModeEnum ViewPermissionMode { get; set; } = ViewPermissionModeEnum.Public;

        /// <summary>
        /// View Teams - List of team names for view permission control (used when ViewPermissionSubjectType=Team)
        /// </summary>
        public List<string> ViewTeams { get; set; }

        /// <summary>
        /// View Users - List of user IDs for view permission control (used when ViewPermissionSubjectType=User)
        /// </summary>
        public List<string> ViewUsers { get; set; }

        /// <summary>
        /// Operate Teams - List of team names that can perform operations (used when OperatePermissionSubjectType=Team)
        /// </summary>
        public List<string> OperateTeams { get; set; }

        /// <summary>
        /// Operate Users - List of user IDs that can perform operations (used when OperatePermissionSubjectType=User)
        /// </summary>
        public List<string> OperateUsers { get; set; }

        /// <summary>
        /// 验证邮箱格式（仅在邮箱不为空时验证）
        /// </summary>
        public bool IsValidContactEmail()
        {
            if (string.IsNullOrWhiteSpace(ContactEmail))
                return true; // 空值被认为是有效的

            try
            {
                var addr = new System.Net.Mail.MailAddress(ContactEmail);
                return addr.Address == ContactEmail;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Newtonsoft.Json 自定义转换器，用于处理空字符串到 nullable long 的转换
    /// </summary>
    public class NullableLongConverter : JsonConverter<long?>
    {
        public override void WriteJson(JsonWriter writer, long? value, JsonSerializer serializer)
        {
            if (value.HasValue)
            {
                writer.WriteValue(value.Value);
            }
            else
            {
                writer.WriteNull();
            }
        }

        public override long? ReadJson(JsonReader reader, Type objectType, long? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonToken.String)
            {
                var stringValue = reader.Value?.ToString();
                if (string.IsNullOrEmpty(stringValue))
                {
                    return null;
                }
                if (long.TryParse(stringValue, out var result))
                {
                    return result;
                }
                return null; // 无法解析时返回 null 而不是抛出异常
            }

            if (reader.TokenType == JsonToken.Integer)
            {
                return Convert.ToInt64(reader.Value);
            }

            return null;
        }
    }
}