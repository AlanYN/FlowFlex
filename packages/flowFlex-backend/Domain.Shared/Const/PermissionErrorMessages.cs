namespace FlowFlex.Domain.Shared.Const
{
    /// <summary>
    /// Permission error messages constants
    /// Centralized error messages for consistent user feedback
    /// </summary>
    public static class PermissionErrorMessages
    {
        // General messages
        public const string InvalidInput = "Invalid user ID or {0} ID";
        public const string ResourceNotFound = "{0} not found";
        public const string InternalError = "Internal error during permission check";
        public const string UnsupportedOperation = "Unsupported operation type";
        
        // View permission messages
        public const string NoViewPermission = "User does not have view permission for this {0}";
        public const string NoWorkflowViewPermission = "User does not have Workflow view permission";
        public const string NoStageViewPermission = "User does not have Stage view permission";
        
        // Operate permission messages
        public const string NoOperatePermission = "User has view permission but not operate permission for this {0}";
        public const string NoWorkflowOperatePermission = "User does not have Workflow operate permission";
        public const string NoStageOperatePermission = "User does not have Stage operate permission";
        
        // Module permission messages
        public const string NoModulePermission = "User does not have required module permission: {0}";
        public const string NoIamToken = "No IAM token available for permission check";
        
        // Entity-specific messages
        public const string NotInAllowedTeams = "User is not in allowed teams to view this {0}";
        public const string NotInAllowedUsers = "User is not in allowed users to view this {0}";
        
        // Helper method to format messages
        public static string FormatMessage(string template, params object[] args)
        {
            return string.Format(template, args);
        }
    }
}

