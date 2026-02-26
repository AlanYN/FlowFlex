namespace FlowFlex.Domain.Shared.Const
{
    /// <summary>
    /// Constants for AI streaming result type identifiers.
    /// Used across all AI services for consistent stream event typing.
    /// </summary>
    public static class AIStreamResultTypes
    {
        public const string Start = "start";
        public const string Delta = "delta";
        public const string Complete = "complete";
        public const string Error = "error";
        public const string Progress = "progress";
        public const string Stage = "stage";
        public const string Analysis = "analysis";
        public const string Questionnaire = "questionnaire";
        public const string Checklist = "checklist";
    }
}
