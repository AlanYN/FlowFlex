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
        /// Stage saved
        /// </summary>
        [Description("Stage Save")]
        StageSave = 47,

        /// <summary>
        /// Case status changed
        /// Note: Enum name changed from OnboardingStatusChange to CaseStatusChange. Database migration may be needed for existing records.
        /// </summary>
        [Description("Case Status Change")]
        CaseStatusChange = 11,

        /// <summary>
        /// Stage action execution
        /// </summary>
        [Description("Stage Action Execution")]
        StageActionExecution = 12,

        /// <summary>
        /// Task action execution
        /// </summary>
        [Description("Task Action Execution")]
        TaskActionExecution = 13,

        /// <summary>
        /// Question action execution
        /// </summary>
        [Description("Question Action Execution")]
        QuestionActionExecution = 14,

        // Workflow Operations
        /// <summary>
        /// Workflow created
        /// </summary>
        [Description("Workflow Create")]
        WorkflowCreate = 15,

        /// <summary>
        /// Workflow updated
        /// </summary>
        [Description("Workflow Update")]
        WorkflowUpdate = 16,

        /// <summary>
        /// Workflow deleted
        /// </summary>
        [Description("Workflow Delete")]
        WorkflowDelete = 17,

        /// <summary>
        /// Workflow published
        /// </summary>
        [Description("Workflow Publish")]
        WorkflowPublish = 18,

        /// <summary>
        /// Workflow unpublished
        /// </summary>
        [Description("Workflow Unpublish")]
        WorkflowUnpublish = 19,

        /// <summary>
        /// Workflow activated
        /// </summary>
        [Description("Workflow Activate")]
        WorkflowActivate = 20,

        /// <summary>
        /// Workflow deactivated
        /// </summary>
        [Description("Workflow Deactivate")]
        WorkflowDeactivate = 21,

        // Stage Operations (independent of Onboarding)
        /// <summary>
        /// Stage created
        /// </summary>
        [Description("Stage Create")]
        StageCreate = 22,

        /// <summary>
        /// Stage updated
        /// </summary>
        [Description("Stage Update")]
        StageUpdate = 23,

        /// <summary>
        /// Stage deleted
        /// </summary>
        [Description("Stage Delete")]
        StageDelete = 24,

        /// <summary>
        /// Stage order changed
        /// </summary>
        [Description("Stage Order Change")]
        StageOrderChange = 25,

        // Checklist Operations (independent of Onboarding)
        /// <summary>
        /// Checklist created
        /// </summary>
        [Description("Checklist Create")]
        ChecklistCreate = 26,

        /// <summary>
        /// Checklist updated
        /// </summary>
        [Description("Checklist Update")]
        ChecklistUpdate = 27,

        /// <summary>
        /// Checklist deleted
        /// </summary>
        [Description("Checklist Delete")]
        ChecklistDelete = 28,

        /// <summary>
        /// Checklist task created
        /// </summary>
        [Description("Checklist Task Create")]
        ChecklistTaskCreate = 29,

        /// <summary>
        /// Checklist task updated
        /// </summary>
        [Description("Checklist Task Update")]
        ChecklistTaskUpdate = 30,

        /// <summary>
        /// Checklist task deleted
        /// </summary>
        [Description("Checklist Task Delete")]
        ChecklistTaskDelete = 31,

        // Questionnaire Operations (independent of Onboarding)
        /// <summary>
        /// Questionnaire created
        /// </summary>
        [Description("Questionnaire Create")]
        QuestionnaireCreate = 32,

        /// <summary>
        /// Questionnaire updated
        /// </summary>
        [Description("Questionnaire Update")]
        QuestionnaireUpdate = 33,

        /// <summary>
        /// Questionnaire deleted
        /// </summary>
        [Description("Questionnaire Delete")]
        QuestionnaireDelete = 34,

        /// <summary>
        /// Questionnaire published
        /// </summary>
        [Description("Questionnaire Publish")]
        QuestionnairePublish = 35,

        /// <summary>
        /// Questionnaire unpublished
        /// </summary>
        [Description("Questionnaire Unpublish")]
        QuestionnaireUnpublish = 36,

        // Duplicate Operations
        /// <summary>
        /// Workflow duplicated
        /// </summary>
        [Description("Workflow Duplicate")]
        WorkflowDuplicate = 37,

        /// <summary>
        /// Stage duplicated
        /// </summary>
        [Description("Stage Duplicate")]
        StageDuplicate = 38,

        /// <summary>
        /// Checklist duplicated
        /// </summary>
        [Description("Checklist Duplicate")]
        ChecklistDuplicate = 39,

        /// <summary>
        /// Questionnaire duplicated
        /// </summary>
        [Description("Questionnaire Duplicate")]
        QuestionnaireDuplicate = 40,

        /// <summary>
        /// Action definition created
        /// </summary>
        [Description("Action Definition Create")]
        ActionDefinitionCreate = 41,

        /// <summary>
        /// Action definition updated
        /// </summary>
        [Description("Action Definition Update")]
        ActionDefinitionUpdate = 42,

        /// <summary>
        /// Action definition deleted
        /// </summary>
        [Description("Action Definition Delete")]
        ActionDefinitionDelete = 43,

        /// <summary>
        /// Action mapping created (association)
        /// </summary>
        [Description("Action Mapping Create")]
        ActionMappingCreate = 44,

        /// <summary>
        /// Action mapping deleted (disassociation)
        /// </summary>
        [Description("Action Mapping Delete")]
        ActionMappingDelete = 45,

        /// <summary>
        /// Action mapping updated
        /// </summary>
        [Description("Action Mapping Update")]
        ActionMappingUpdate = 46,

        // Onboarding Operations (CRUD)
        /// <summary>
        /// Case created
        /// Note: Enum name changed from OnboardingCreate to CaseCreate. Database migration may be needed for existing records.
        /// </summary>
        [Description("Case Create")]
        CaseCreate = 48,

        /// <summary>
        /// Case updated
        /// Note: Enum name changed from OnboardingUpdate to CaseUpdate. Database migration may be needed for existing records.
        /// </summary>
        [Description("Case Update")]
        CaseUpdate = 49,

        /// <summary>
        /// Case deleted
        /// Note: Enum name changed from OnboardingDelete to CaseDelete. Database migration may be needed for existing records.
        /// </summary>
        [Description("Case Delete")]
        CaseDelete = 50,

        /// <summary>
        /// Case started
        /// Note: Enum name changed from OnboardingStart to CaseStart. Database migration may be needed for existing records.
        /// </summary>
        [Description("Case Start")]
        CaseStart = 51,

        /// <summary>
        /// Case paused
        /// Note: Enum name changed from OnboardingPause to CasePause. Database migration may be needed for existing records.
        /// </summary>
        [Description("Case Pause")]
        CasePause = 52,

        /// <summary>
        /// Case resumed
        /// Note: Enum name changed from OnboardingResume to CaseResume. Database migration may be needed for existing records.
        /// </summary>
        [Description("Case Resume")]
        CaseResume = 53,

        /// <summary>
        /// Case aborted
        /// Note: Enum name changed from OnboardingAbort to CaseAbort. Database migration may be needed for existing records.
        /// </summary>
        [Description("Case Abort")]
        CaseAbort = 54,

        /// <summary>
        /// Case reactivated
        /// Note: Enum name changed from OnboardingReactivate to CaseReactivate. Database migration may be needed for existing records.
        /// </summary>
        [Description("Case Reactivate")]
        CaseReactivate = 55,

        /// <summary>
        /// Case force completed
        /// Note: Enum name changed from OnboardingForceComplete to CaseForceComplete. Database migration may be needed for existing records.
        /// </summary>
        [Description("Case Force Complete")]
        CaseForceComplete = 56
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
        File = 8,

        /// <summary>
        /// Task
        /// </summary>
        [Description("Task")]
        Task = 9,

        /// <summary>
        /// Question
        /// </summary>
        [Description("Question")]
        Question = 10,

        /// <summary>
        /// Workflow
        /// </summary>
        [Description("Workflow")]
        Workflow = 11,

        /// <summary>
        /// Action
        /// </summary>
        [Description("Action")]
        Action = 12,

        /// <summary>
        /// Action Mapping
        /// </summary>
        [Description("ActionMapping")]
        ActionMapping = 13
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
        Partial = 3,

        /// <summary>
        /// Pending
        /// </summary>
        [Description("Pending")]
        Pending = 4,

        /// <summary>
        /// In Progress
        /// </summary>
        [Description("InProgress")]
        InProgress = 5,

        /// <summary>
        /// Cancelled
        /// </summary>
        [Description("Cancelled")]
        Cancelled = 6,

        /// <summary>
        /// Unknown
        /// </summary>
        [Description("Unknown")]
        Unknown = 7
    }
}
