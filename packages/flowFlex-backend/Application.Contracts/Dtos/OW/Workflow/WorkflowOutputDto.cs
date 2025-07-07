using System;
using System.Collections.Generic;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;

namespace FlowFlex.Application.Contracts.Dtos.OW.Workflow
{
    /// <summary>
    /// Workflow output DTO
    /// </summary>
    public class WorkflowOutputDto
    {
        /// <summary>
        /// Primary key ID (Snowflake generated)
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Tenant ID
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Workflow name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Workflow description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether it's the default workflow
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Workflow status
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Start date
        /// </summary>
        public DateTimeOffset StartDate { get; set; }

        /// <summary>
        /// End date
        /// </summary>
        public DateTimeOffset? EndDate { get; set; }

        /// <summary>
        /// Version number
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Whether it's active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Whether it's valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Create date
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// Modify date
        /// </summary>
        public DateTimeOffset ModifyDate { get; set; }

        /// <summary>
        /// Creator name
        /// </summary>
        public string CreateBy { get; set; }

        /// <summary>
        /// Modifier name
        /// </summary>
        public string ModifyBy { get; set; }

        /// <summary>
        /// Creator user ID
        /// </summary>
        public long CreateUserId { get; set; }

        /// <summary>
        /// Modifier user ID
        /// </summary>
        public long ModifyUserId { get; set; }

        /// <summary>
        /// Stage list
        /// </summary>
        public List<StageOutputDto> Stages { get; set; }
    }
}