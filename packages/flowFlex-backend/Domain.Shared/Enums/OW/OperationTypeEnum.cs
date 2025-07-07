using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums.OW
{
    /// <summary>
    /// Operation type enumeration
    /// </summary>
    public enum OperationTypeEnum
    {
        /// <summary>
        /// Checklist task completed
        /// </summary>
        [Description("Checklist Task Complete")]
        ChecklistTaskComplete = 1,

        /// <summary>
        /// Checklist task uncompleted
        /// </summary>
        [Description("Checklist Task Uncomplete")]
        ChecklistTaskUncomplete = 2,

        /// <summary>
        /// Questionnaire answer submitted
        /// </summary>
        [Description("Questionnaire Answer Submit")]
        QuestionnaireAnswerSubmit = 3,

        /// <summary>
        /// Questionnaire answer updated
        /// </summary>
        [Description("Questionnaire Answer Update")]
        QuestionnaireAnswerUpdate = 4,

        /// <summary>
        /// Static field value changed
        /// </summary>
        [Description("Static Field Value Change")]
        StaticFieldValueChange = 5,

        /// <summary>
        /// File uploaded
        /// </summary>
        [Description("File Upload")]
        FileUpload = 6,

        /// <summary>
        /// File deleted
        /// </summary>
        [Description("File Delete")]
        FileDelete = 7,

        /// <summary>
        /// File updated
        /// </summary>
        [Description("File Update")]
        FileUpdate = 8,

        /// <summary>
        /// Stage completed
        /// </summary>
        [Description("Stage Complete")]
        StageComplete = 9,

        /// <summary>
        /// Stage reopened
        /// </summary>
        [Description("Stage Reopen")]
        StageReopen = 10,

        /// <summary>
        /// Onboarding status changed
        /// </summary>
        [Description("Onboarding Status Change")]
        OnboardingStatusChange = 11
    }

    /// <summary>
    /// Business module enumeration
    /// </summary>
    public enum BusinessModuleEnum
    {
        /// <summary>
        /// Onboarding
        /// </summary>
        [Description("Onboarding")]
        Onboarding = 1,

        /// <summary>
        /// Stage
        /// </summary>
        [Description("Stage")]
        Stage = 2,

        /// <summary>
        /// Checklist
        /// </summary>
        [Description("Checklist")]
        Checklist = 3,

        /// <summary>
        /// ChecklistTask
        /// </summary>
        [Description("ChecklistTask")]
        ChecklistTask = 4,

        /// <summary>
        /// Questionnaire
        /// </summary>
        [Description("Questionnaire")]
        Questionnaire = 5,

        /// <summary>
        /// QuestionnaireAnswer
        /// </summary>
        [Description("QuestionnaireAnswer")]
        QuestionnaireAnswer = 6,

        /// <summary>
        /// StaticField
        /// </summary>
        [Description("StaticField")]
        StaticField = 7,

        /// <summary>
        /// File
        /// </summary>
        [Description("File")]
        File = 8
    }

    /// <summary>
    /// Operation status enumeration
    /// </summary>
    public enum OperationStatusEnum
    {
        /// <summary>
        /// Success
        /// </summary>
        [Description("Success")]
        Success = 1,

        /// <summary>
        /// Failed
        /// </summary>
        [Description("Failed")]
        Failed = 2,

        /// <summary>
        /// Partial success
        /// </summary>
        [Description("Partial")]
        Partial = 3
    }
}
