namespace FlowFlex.Domain.Shared.Const
{
    /// <summary>
    /// Permission error codes constants
    /// Standardized error codes for API responses and logging
    /// </summary>
    public static class PermissionErrorCodes
    {
        // General error codes
        public const string InvalidInput = "INVALID_INPUT";
        public const string InternalError = "PERMISSION_CHECK_ERROR";
        public const string UnsupportedOperation = "UNSUPPORTED_OPERATION";
        public const string UnsupportedResourceType = "UNSUPPORTED_RESOURCE_TYPE";
        public const string UnsupportedPermission = "UNSUPPORTED_PERMISSION";

        // Resource not found codes
        public const string WorkflowNotFound = "WORKFLOW_NOT_FOUND";
        public const string StageNotFound = "STAGE_NOT_FOUND";
        public const string CaseNotFound = "CASE_NOT_FOUND";

        // Permission denied codes
        public const string PermissionDenied = "PERMISSION_DENIED";
        public const string ViewPermissionDenied = "VIEW_PERMISSION_DENIED";
        public const string OperatePermissionDenied = "OPERATE_PERMISSION_DENIED";

        // Module permission codes
        public const string ModulePermissionDenied = "MODULE_PERMISSION_DENIED";
        public const string ModulePermissionCheckError = "MODULE_PERMISSION_CHECK_ERROR";
        public const string NoIamToken = "NO_IAM_TOKEN";

        // User/Group related codes
        public const string UserNoGroup = "USER_NO_GROUP";
    }
}

