using System.Collections.Generic;
using System.Threading.Tasks;
using FlowFlex.Application.Contracts.Dtos.OW.Stage;
using FlowFlex.Application.Contracts.Models;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Stage service interface
    /// </summary>
    public interface IStageService : IScopedService
    {
        Task<long> CreateAsync(StageInputDto input);
        Task<bool> UpdateAsync(long id, StageInputDto input);
        Task<bool> DeleteAsync(long id, bool confirm = false);
        Task<StageOutputDto> GetByIdAsync(long id);
        Task<List<StageOutputDto>> GetListByWorkflowIdAsync(long workflowId);
        Task<List<StageOutputDto>> GetAllAsync();
        Task<PagedResult<StageOutputDto>> QueryAsync(StageQueryRequest query);
        Task<bool> SortStagesAsync(SortStagesInputDto input);
        Task<long> CombineStagesAsync(CombineStagesInputDto input);
        Task<bool> SetColorAsync(long id, string color);
        Task<bool> UpdateRequiredFieldsAsync(long id, UpdateRequiredFieldsInputDto input);
        Task<long> DuplicateAsync(long id, DuplicateStageInputDto input);

        /// <summary>
        /// Get Stage complete content
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Stage complete content</returns>
        Task<StageContentDto> GetStageContentAsync(long stageId, long onboardingId);

        /// <summary>
        /// Update Stage static fields
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="staticFields">Static field data</param>
        /// <returns>Update result</returns>
        Task<bool> UpdateStaticFieldsAsync(long stageId, long onboardingId, StageStaticFieldsDto staticFields);

        /// <summary>
        /// Update Checklist task status
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="taskId">Task ID</param>
        /// <param name="isCompleted">Is completed</param>
        /// <param name="completionNotes">Completion notes</param>
        /// <returns>Update result</returns>
        Task<bool> UpdateChecklistTaskAsync(long stageId, long onboardingId, long taskId, bool isCompleted, string completionNotes = null);

        /// <summary>
        /// Submit questionnaire answer
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="questionId">Question ID</param>
        /// <param name="answer">Answer</param>
        /// <returns>Submit result</returns>
        Task<bool> SubmitQuestionnaireAnswerAsync(long stageId, long onboardingId, long questionId, object answer);

        /// <summary>
        /// Upload Stage file
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="fileName">File name</param>
        /// <param name="fileContent">File content</param>
        /// <param name="fileCategory">File category</param>
        /// <returns>Uploaded file information</returns>
        Task<StageFileDto> UploadStageFileAsync(long stageId, long onboardingId, string fileName, byte[] fileContent, string fileCategory = null);

        /// <summary>
        /// Delete Stage file
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="fileId">File ID</param>
        /// <returns>Delete result</returns>
        Task<bool> DeleteStageFileAsync(long stageId, long onboardingId, long fileId);

        /// <summary>
        /// Validate Stage completion conditions
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Validation result</returns>
        Task<StageCompletionValidationDto> ValidateStageCompletionAsync(long stageId, long onboardingId);

        /// <summary>
        /// Complete Stage
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="completionNotes">Completion notes</param>
        /// <returns>Completion result</returns>
        Task<bool> CompleteStageAsync(long stageId, long onboardingId, string completionNotes = null);

        /// <summary>
        /// Get Stage operation logs
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Operation logs</returns>
        Task<StageLogsDto> GetStageLogsAsync(long stageId, long onboardingId, int pageIndex = 1, int pageSize = 20);

        /// <summary>
        /// Add Stage note
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="noteContent">Note content</param>
        /// <param name="isPrivate">Is private note</param>
        /// <returns>Add result</returns>
        Task<bool> AddStageNoteAsync(long stageId, long onboardingId, string noteContent, bool isPrivate = false);

        /// <summary>
        /// Get Stage notes list
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Notes list</returns>
        Task<StageNotesDto> GetStageNotesAsync(long stageId, long onboardingId, int pageIndex = 1, int pageSize = 20);

        /// <summary>
        /// Update Stage note
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="noteId">Note ID</param>
        /// <param name="noteContent">New note content</param>
        /// <returns>Update result</returns>
        Task<bool> UpdateStageNoteAsync(long stageId, long onboardingId, long noteId, string noteContent);

        /// <summary>
        /// Delete Stage note
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="noteId">Note ID</param>
        /// <returns>Delete result</returns>
        Task<bool> DeleteStageNoteAsync(long stageId, long onboardingId, long noteId);
    }

    /// <summary>
    /// Stage completion validation result
    /// </summary>
    public class StageCompletionValidationDto
    {
        /// <summary>
        /// Can complete
        /// </summary>
        public bool CanComplete { get; set; }

        /// <summary>
        /// Validation message list
        /// </summary>
        public List<string> ValidationMessages { get; set; } = new List<string>();

        /// <summary>
        /// Static fields validation result
        /// </summary>
        public StageFieldValidationDto StaticFieldsValidation { get; set; }

        /// <summary>
        /// Checklist completion status
        /// </summary>
        public bool ChecklistCompleted { get; set; }

        /// <summary>
        /// Questionnaire completion status
        /// </summary>
        public bool QuestionnaireCompleted { get; set; }

        /// <summary>
        /// File upload completion status
        /// </summary>
        public bool FilesCompleted { get; set; }

        /// <summary>
        /// Completion percentage
        /// </summary>
        public decimal CompletionPercentage { get; set; }
    }
}
