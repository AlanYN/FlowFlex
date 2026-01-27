namespace FlowFlex.Domain.Shared.Const
{
    /// <summary>
    /// Constants for Stage Condition feature
    /// </summary>
    public static class StageConditionConstants
    {
        #region Status Values

        /// <summary>
        /// Valid condition status
        /// </summary>
        public const string StatusValid = "Valid";

        /// <summary>
        /// Invalid condition status
        /// </summary>
        public const string StatusInvalid = "Invalid";

        /// <summary>
        /// Completed status for workflow/stage
        /// </summary>
        public const string StatusCompleted = "Completed";

        /// <summary>
        /// Force completed status for workflow
        /// </summary>
        public const string StatusForceCompleted = "Force Completed";

        /// <summary>
        /// Skipped status for stage
        /// </summary>
        public const string StatusSkipped = "Skipped";

        /// <summary>
        /// In Progress status
        /// </summary>
        public const string StatusInProgress = "In Progress";

        #endregion

        #region System Values

        /// <summary>
        /// System user name for automated operations
        /// </summary>
        public const string SystemUser = "SYSTEM";

        /// <summary>
        /// Default workflow name for RulesEngine
        /// </summary>
        public const string DefaultWorkflowName = "StageCondition";

        /// <summary>
        /// Auto-advance action order (high value to indicate auto-generated)
        /// </summary>
        public const int AutoAdvanceActionOrder = 999;

        #endregion

        #region Action Types

        public const string ActionTypeGoToStage = "gotostage";
        public const string ActionTypeSkipStage = "skipstage";
        public const string ActionTypeEndWorkflow = "endworkflow";
        public const string ActionTypeSendNotification = "sendnotification";
        public const string ActionTypeUpdateField = "updatefield";
        public const string ActionTypeTriggerAction = "triggeraction";
        public const string ActionTypeAssignUser = "assignuser";

        /// <summary>
        /// All valid action types
        /// </summary>
        public static readonly string[] ValidActionTypes = new[]
        {
            ActionTypeGoToStage,
            ActionTypeSkipStage,
            ActionTypeEndWorkflow,
            ActionTypeSendNotification,
            ActionTypeUpdateField,
            ActionTypeTriggerAction,
            ActionTypeAssignUser
        };

        /// <summary>
        /// Stage control action types (only one can be executed)
        /// </summary>
        public static readonly string[] StageControlActionTypes = new[]
        {
            ActionTypeGoToStage,
            ActionTypeSkipStage,
            ActionTypeEndWorkflow
        };

        #endregion

        #region Logic Types

        public const string LogicAnd = "AND";
        public const string LogicOr = "OR";

        #endregion

        #region Assignee Types

        public const string AssigneeTypeUser = "user";
        public const string AssigneeTypeTeam = "team";
        public const string AssigneeTypeEmail = "email";

        #endregion

        #region Validation Error Codes

        public const string ErrorCodeNotFound = "NOT_FOUND";
        public const string ErrorCodeRulesRequired = "RULES_REQUIRED";
        public const string ErrorCodeRulesEmpty = "RULES_EMPTY";
        public const string ErrorCodeActionsRequired = "ACTIONS_REQUIRED";
        public const string ErrorCodeActionsEmpty = "ACTIONS_EMPTY";
        public const string ErrorCodeInvalidJson = "INVALID_JSON";
        public const string ErrorCodeInvalidFormat = "INVALID_FORMAT";
        public const string ErrorCodeInvalidExpression = "INVALID_EXPRESSION";
        public const string ErrorCodeRuleNameRequired = "RULE_NAME_REQUIRED";
        public const string ErrorCodeRuleExpressionRequired = "RULE_EXPRESSION_REQUIRED";
        public const string ErrorCodeActionTypeRequired = "ACTION_TYPE_REQUIRED";
        public const string ErrorCodeInvalidActionType = "INVALID_ACTION_TYPE";

        #endregion

        #region Validation Warning Codes

        public const string WarningCodeFormatConverted = "FORMAT_CONVERTED";
        public const string WarningCodeWorkflowNameEmpty = "WORKFLOW_NAME_EMPTY";
        public const string WarningCodeConflictingRules = "CONFLICTING_RULES";
        public const string WarningCodeConflictingStageActions = "CONFLICTING_STAGE_ACTIONS";
        public const string WarningCodeMultipleGoToStageTargets = "MULTIPLE_GOTOSTAGE_TARGETS";
        public const string WarningCodeCircularReference = "CIRCULAR_REFERENCE";

        #endregion

        #region Field Path Validation

        /// <summary>
        /// Allowed prefixes for field paths in rule expressions
        /// </summary>
        public static readonly string[] AllowedFieldPathPrefixes = new[]
        {
            "input.checklist",
            "input.questionnaire",
            "input.attachments",
            "input.fields"
        };

        /// <summary>
        /// Characters not allowed in field path (prevent injection)
        /// </summary>
        public static readonly char[] DisallowedFieldPathChars = new[]
        {
            ';', '&', '|', '`', '$', '!', '{', '}', '<', '>'
        };

        /// <summary>
        /// Maximum length for field path
        /// </summary>
        public const int MaxFieldPathLength = 500;

        /// <summary>
        /// Maximum length for field value in expression
        /// </summary>
        public const int MaxFieldValueLength = 1000;

        #endregion

        #region Retry Configuration

        /// <summary>
        /// Maximum retry attempts for external service calls
        /// </summary>
        public const int MaxRetryAttempts = 3;

        /// <summary>
        /// Base delay in milliseconds for retry backoff
        /// </summary>
        public const int RetryBaseDelayMs = 500;

        /// <summary>
        /// Maximum delay in milliseconds for retry backoff
        /// </summary>
        public const int RetryMaxDelayMs = 5000;

        #endregion

        #region Timeout Configuration

        /// <summary>
        /// Default timeout in seconds for action execution
        /// </summary>
        public const int ActionExecutionTimeoutSeconds = 30;

        /// <summary>
        /// Timeout in seconds for SendNotification action (may involve multiple emails)
        /// </summary>
        public const int SendNotificationTimeoutSeconds = 60;

        /// <summary>
        /// Timeout in seconds for TriggerAction action (external action execution)
        /// </summary>
        public const int TriggerActionTimeoutSeconds = 45;

        #endregion

        #region Source Identifiers

        /// <summary>
        /// Source identifier for stage condition operations
        /// </summary>
        public const string SourceStageCondition = "stage_condition";

        /// <summary>
        /// Source identifier for auto-advance operations
        /// </summary>
        public const string SourceAutoAdvance = "auto_advance";

        #endregion
    }
}
