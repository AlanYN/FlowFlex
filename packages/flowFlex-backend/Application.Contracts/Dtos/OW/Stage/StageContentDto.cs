using System;
using System.Collections.Generic;

namespace FlowFlex.Application.Contracts.Dtos.OW.Stage
{
    /// <summary>
    /// Stage内容DTO - 包含Stage的所有内容部分
    /// </summary>
    public class StageContentDto
    {
        /// <summary>
        /// Stage ID
        /// </summary>
        public long StageId { get; set; }

        /// <summary>
        /// Stage名称
        /// </summary>
        public string StageName { get; set; }

        /// <summary>
        /// Portal显示名称
        /// </summary>
        public string PortalName { get; set; }

        /// <summary>
        /// 内部名称
        /// </summary>
        public string InternalName { get; set; }



        /// <summary>
        /// 2. Checklist部分 - Checklist Tasks
        /// </summary>
        public StageChecklistDto Checklist { get; set; }

        /// <summary>
        /// 3. 问卷调查部分 - Questionnaire
        /// </summary>
        public StageQuestionnaireDto Questionnaire { get; set; }

        /// <summary>
        /// 4. 文件管理部分 - Files/Documents
        /// </summary>
        public StageFilesDto Files { get; set; }

        /// <summary>
        /// 其他：内部备注 - Internal Notes
        /// </summary>
        public StageNotesDto Notes { get; set; }

        /// <summary>
        /// 其他：操作日志 - Operation Logs
        /// </summary>
        public StageLogsDto Logs { get; set; }
    }

    /// <summary>
    /// Stage静态字段部分
    /// </summary>
    public class StageStaticFieldsDto
    {
        /// <summary>
        /// 必填字段列表
        /// </summary>
        public List<StageFieldDto> RequiredFields { get; set; } = new List<StageFieldDto>();

        /// <summary>
        /// 可选字段列表
        /// </summary>
        public List<StageFieldDto> OptionalFields { get; set; } = new List<StageFieldDto>();

        /// <summary>
        /// 自定义字段
        /// </summary>
        public Dictionary<string, object> CustomFields { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 字段验证结果
        /// </summary>
        public StageFieldValidationDto Validation { get; set; }
    }

    /// <summary>
    /// Stage字段定义
    /// </summary>
    public class StageFieldDto
    {
        /// <summary>
        /// 字段名称
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// 字段显示名称
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 字段类型 (text, date, number, email, phone, select, etc.)
        /// </summary>
        public string FieldType { get; set; }

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 字段值
        /// </summary>
        public object FieldValue { get; set; }

        /// <summary>
        /// 验证规则
        /// </summary>
        public string ValidationRules { get; set; }

        /// <summary>
        /// 字段选项（用于select类型）
        /// </summary>
        public List<string> Options { get; set; } = new List<string>();

        /// <summary>
        /// 字段描述/帮助文本
        /// </summary>
        public string HelpText { get; set; }
    }

    /// <summary>
    /// Stage字段验证结果
    /// </summary>
    public class StageFieldValidationDto
    {
        /// <summary>
        /// 是否所有必填字段都已填写
        /// </summary>
        public bool AllRequiredFieldsCompleted { get; set; }

        /// <summary>
        /// 完成的必填字段数
        /// </summary>
        public int CompletedRequiredFields { get; set; }

        /// <summary>
        /// 总必填字段数
        /// </summary>
        public int TotalRequiredFields { get; set; }

        /// <summary>
        /// 验证错误列表
        /// </summary>
        public List<string> ValidationErrors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Stage Checklist部分
    /// </summary>
    public class StageChecklistDto
    {
        /// <summary>
        /// Checklist ID
        /// </summary>
        public long? ChecklistId { get; set; }

        /// <summary>
        /// Checklist名称
        /// </summary>
        public string ChecklistName { get; set; }

        /// <summary>
        /// 任务列表
        /// </summary>
        public List<ChecklistTaskDto> Tasks { get; set; } = new List<ChecklistTaskDto>();

        /// <summary>
        /// 完成率 (0-100)
        /// </summary>
        public decimal CompletionRate { get; set; }

        /// <summary>
        /// 总任务数
        /// </summary>
        public int TotalTasks { get; set; }

        /// <summary>
        /// 已完成任务数
        /// </summary>
        public int CompletedTasks { get; set; }

        /// <summary>
        /// 是否所有任务都已完成
        /// </summary>
        public bool IsAllTasksCompleted { get; set; }
    }

    /// <summary>
    /// Checklist任务
    /// </summary>
    public class ChecklistTaskDto
    {
        /// <summary>
        /// 任务ID
        /// </summary>
        public long TaskId { get; set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        public string TaskName { get; set; }

        /// <summary>
        /// 任务描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 是否必须完成
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 任务顺序
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 是否完成
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTimeOffset? CompletedTime { get; set; }

        /// <summary>
        /// 完成人
        /// </summary>
        public string CompletedBy { get; set; }

        /// <summary>
        /// 完成备注
        /// </summary>
        public string CompletionNotes { get; set; }
    }

    /// <summary>
    /// Stage问卷调查部分
    /// </summary>
    public class StageQuestionnaireDto
    {
        /// <summary>
        /// 问卷ID
        /// </summary>
        public long? QuestionnaireId { get; set; }

        /// <summary>
        /// 问卷名称
        /// </summary>
        public string QuestionnaireName { get; set; }

        /// <summary>
        /// 问卷描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 问题列表
        /// </summary>
        public List<QuestionDto> Questions { get; set; } = new List<QuestionDto>();

        /// <summary>
        /// 问卷完成率 (0-100)
        /// </summary>
        public decimal CompletionRate { get; set; }

        /// <summary>
        /// 总问题数
        /// </summary>
        public int TotalQuestions { get; set; }

        /// <summary>
        /// 已回答问题数
        /// </summary>
        public int AnsweredQuestions { get; set; }

        /// <summary>
        /// 是否所有必答问题都已回答
        /// </summary>
        public bool IsAllRequiredQuestionsAnswered { get; set; }

        /// <summary>
        /// 问卷开始时间
        /// </summary>
        public DateTimeOffset? StartTime { get; set; }

        /// <summary>
        /// 问卷提交时间
        /// </summary>
        public DateTimeOffset? SubmitTime { get; set; }
    }

    /// <summary>
    /// 问卷问题
    /// </summary>
    public class QuestionDto
    {
        /// <summary>
        /// 问题ID
        /// </summary>
        public long QuestionId { get; set; }

        /// <summary>
        /// 问题文本
        /// </summary>
        public string QuestionText { get; set; }

        /// <summary>
        /// 问题类型 (text, choice, multiple_choice, rating, etc.)
        /// </summary>
        public string QuestionType { get; set; }

        /// <summary>
        /// 是否必答
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 问题顺序
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 选项列表（用于选择题）
        /// </summary>
        public List<string> Options { get; set; } = new List<string>();

        /// <summary>
        /// 回答内容
        /// </summary>
        public object Answer { get; set; }

        /// <summary>
        /// 是否已回答
        /// </summary>
        public bool IsAnswered { get; set; }

        /// <summary>
        /// 回答时间
        /// </summary>
        public DateTimeOffset? AnswerTime { get; set; }
    }

    /// <summary>
    /// Stage文件管理部分
    /// </summary>
    public class StageFilesDto
    {
        /// <summary>
        /// 允许上传的文件类型
        /// </summary>
        public List<string> AllowedFileTypes { get; set; } = new List<string> { "pdf", "doc", "docx", "jpg", "png" };

        /// <summary>
        /// 最大文件大小（MB）
        /// </summary>
        public int MaxFileSizeMB { get; set; } = 10;

        /// <summary>
        /// 最大文件数量
        /// </summary>
        public int MaxFileCount { get; set; } = 10;

        /// <summary>
        /// 已上传的文件列表
        /// </summary>
        public List<StageFileDto> UploadedFiles { get; set; } = new List<StageFileDto>();

        /// <summary>
        /// 必需的文件类型
        /// </summary>
        public List<RequiredFileDto> RequiredFiles { get; set; } = new List<RequiredFileDto>();

        /// <summary>
        /// 文件上传完成率
        /// </summary>
        public decimal UploadCompletionRate { get; set; }

        /// <summary>
        /// 是否所有必需文件都已上传
        /// </summary>
        public bool IsAllRequiredFilesUploaded { get; set; }
    }

    /// <summary>
    /// Stage文件信息
    /// </summary>
    public class StageFileDto
    {
        /// <summary>
        /// 文件ID
        /// </summary>
        public long FileId { get; set; }

        /// <summary>
        /// 文件名称
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 文件大小显示文本（如: 2.4 MB）
        /// </summary>
        public string FileSizeDisplay { get; set; }

        /// <summary>
        /// 文件类型/扩展名
        /// </summary>
        public string FileType { get; set; }

        /// <summary>
        /// 文件分类（如：Application_Form, Company_Profile）
        /// </summary>
        public string FileCategory { get; set; }

        /// <summary>
        /// 上传时间
        /// </summary>
        public DateTimeOffset UploadTime { get; set; }

        /// <summary>
        /// 上传人
        /// </summary>
        public string UploadBy { get; set; }

        /// <summary>
        /// 文件下载URL
        /// </summary>
        public string FileUrl { get; set; }

        /// <summary>
        /// 文件预览URL（如果支持）
        /// </summary>
        public string PreviewUrl { get; set; }

        /// <summary>
        /// 是否可以预览
        /// </summary>
        public bool CanPreview { get; set; }
    }

    /// <summary>
    /// 必需文件定义
    /// </summary>
    public class RequiredFileDto
    {
        /// <summary>
        /// 文件类型名称
        /// </summary>
        public string FileTypeName { get; set; }

        /// <summary>
        /// 文件描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 是否必须
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 是否已上传
        /// </summary>
        public bool IsUploaded { get; set; }

        /// <summary>
        /// 已上传的文件ID
        /// </summary>
        public long? UploadedFileId { get; set; }
    }

    /// <summary>
    /// Stage内部备注部分
    /// </summary>
    public class StageNotesDto
    {
        /// <summary>
        /// 备注列表
        /// </summary>
        public List<StageNoteDto> Notes { get; set; } = new List<StageNoteDto>();

        /// <summary>
        /// 总备注数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 是否还有更多备注
        /// </summary>
        public bool HasMore { get; set; }

        /// <summary>
        /// 是否支持添加备注
        /// </summary>
        public bool CanAddNote { get; set; } = true;
    }

    /// <summary>
    /// Stage内部备注
    /// </summary>
    public class StageNoteDto
    {
        /// <summary>
        /// 备注ID
        /// </summary>
        public long NoteId { get; set; }

        /// <summary>
        /// 备注内容
        /// </summary>
        public string NoteContent { get; set; }

        /// <summary>
        /// 是否私有备注
        /// </summary>
        public bool IsPrivate { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// 创建人邮箱
        /// </summary>
        public string CreatedByEmail { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTimeOffset CreatedTime { get; set; }

        /// <summary>
        /// 创建时间显示文本
        /// </summary>
        public string CreatedTimeDisplay { get; set; }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTimeOffset? LastModifiedTime { get; set; }

        /// <summary>
        /// 是否可以编辑（只有创建人可以编辑）
        /// </summary>
        public bool CanEdit { get; set; }

        /// <summary>
        /// 是否可以删除（只有创建人可以删除）
        /// </summary>
        public bool CanDelete { get; set; }
    }

    /// <summary>
    /// Stage操作日志部分
    /// </summary>
    public class StageLogsDto
    {
        /// <summary>
        /// 日志列表
        /// </summary>
        public List<StageLogDto> Logs { get; set; } = new List<StageLogDto>();

        /// <summary>
        /// 总日志数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 是否还有更多日志
        /// </summary>
        public bool HasMore { get; set; }
    }

    /// <summary>
    /// Stage操作日志
    /// </summary>
    public class StageLogDto
    {
        /// <summary>
        /// 日志ID
        /// </summary>
        public long LogId { get; set; }

        /// <summary>
        /// 操作类型 (Update, Completion, Priority Change, etc.)
        /// </summary>
        public string OperationType { get; set; }

        /// <summary>
        /// 操作描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 详细信息（如：从Medium改为High）
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// 操作人
        /// </summary>
        public string OperatedBy { get; set; }

        /// <summary>
        /// 操作时间
        /// </summary>
        public DateTimeOffset OperatedTime { get; set; }

        /// <summary>
        /// 操作时间显示文本
        /// </summary>
        public string OperatedTimeDisplay { get; set; }

        /// <summary>
        /// 操作结果
        /// </summary>
        public string Result { get; set; }

        /// <summary>
        /// 日志类型标签（如：Update, Completion等）
        /// </summary>
        public string LogTypeTag { get; set; }
    }
}