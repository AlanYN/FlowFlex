using FlowFlex.Domain.Shared.Enums.Permission;

namespace FlowFlex.Application.Contracts.Dtos.OW.Permission
{
    /// <summary>
    /// Check permission request
    /// </summary>
    public class CheckPermissionRequest
    {
        /// <summary>
        /// Resource ID (Workflow/Stage/Case ID)
        /// </summary>
        public long ResourceId { get; set; }

        /// <summary>
        /// Resource type (Workflow/Stage/Case)
        /// </summary>
        public PermissionEntityTypeEnum ResourceType { get; set; }
    }
}

