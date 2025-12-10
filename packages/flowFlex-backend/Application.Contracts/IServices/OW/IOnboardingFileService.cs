using Microsoft.AspNetCore.Http;
using FlowFlex.Application.Contracts.Dtos.OW.OnboardingFile;
using FlowFlex.Domain.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace FlowFlex.Application.Contracts.IServices.OW
{
    /// <summary>
    /// Onboarding file service interface
    /// </summary>
    public interface IOnboardingFileService : IScopedService
    {
        /// <summary>
        /// Upload file
        /// </summary>
        /// <param name="input">File upload input information</param>
        /// <returns>Successfully uploaded file information</returns>
        Task<OnboardingFileOutputDto> UploadFileAsync(OnboardingFileInputDto input);

        /// <summary>
        /// Upload multiple files
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID (optional)</param>
        /// <param name="formFiles">File list</param>
        /// <param name="category">File category</param>
        /// <param name="description">File description</param>
        /// <returns>Successfully uploaded file list</returns>
        Task<List<OnboardingFileOutputDto>> UploadMultipleFilesAsync(long onboardingId, long? stageId, List<IFormFile> formFiles, string category, string description);

        /// <summary>
        /// Get file list
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID (optional)</param>
        /// <param name="category">File category filter (optional)</param>
        /// <returns>File list</returns>
        Task<List<OnboardingFileOutputDto>> GetFilesAsync(long onboardingId, long? stageId = null, string category = null);

        /// <summary>
        /// Download file
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <returns>File stream, file name and content type</returns>
        Task<(Stream stream, string fileName, string contentType)> DownloadFileAsync(long fileId);

        /// <summary>
        /// Get file access URL
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <returns>File access URL</returns>
        Task<string> GetFileUrlAsync(long fileId);

        /// <summary>
        /// Delete file
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <returns>Delete result</returns>
        Task<bool> DeleteFileAsync(long fileId);

        /// <summary>
        /// Update file information
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="input">Update information</param>
        /// <returns>Updated file information</returns>
        Task<OnboardingFileOutputDto> UpdateFileAsync(long fileId, UpdateOnboardingFileInputDto input);

        /// <summary>
        /// Get file statistics
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>File statistics</returns>
        Task<OnboardingFileStatisticsDto> GetFileStatisticsAsync(long onboardingId);

        /// <summary>
        /// Get file information by ID
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <returns>File information</returns>
        Task<OnboardingFileOutputDto> GetFileByIdAsync(long fileId);

        /// <summary>
        /// Get file list by stage
        /// </summary>
        /// <param name="stageId">Stage ID</param>
        /// <returns>File list</returns>
        Task<List<OnboardingFileOutputDto>> GetFilesByStageAsync(long stageId);

        /// <summary>
        /// Get file list by category
        /// </summary>
        /// <param name="category">File category</param>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>File list</returns>
        Task<List<OnboardingFileOutputDto>> GetFilesByCategoryAsync(string category, long onboardingId);

        /// <summary>
        /// Get required file list
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <returns>Required file list</returns>
        Task<List<OnboardingFileOutputDto>> GetRequiredFilesAsync(long onboardingId);

        /// <summary>
        /// Get file details
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <returns>File details</returns>
        Task<OnboardingFileOutputDto> GetFileDetailsAsync(long fileId);

        /// <summary>
        /// Import files from external URLs (e.g., OSS links)
        /// Downloads files from provided URLs and saves them as onboarding files
        /// </summary>
        /// <param name="input">Import input containing file URLs</param>
        /// <returns>Import result with progress details</returns>
        Task<ImportFilesResultDto> ImportFilesFromUrlAsync(ImportFilesFromUrlInputDto input);

        /// <summary>
        /// Start an async import task from external URLs
        /// Returns immediately with task ID, import runs in background
        /// </summary>
        /// <param name="input">Import input containing file URLs</param>
        /// <param name="createdBy">User who created the task</param>
        /// <returns>Start import response with task ID</returns>
        Task<StartImportTaskResponseDto> StartImportTaskAsync(ImportFilesFromUrlInputDto input, string createdBy);

        /// <summary>
        /// Get import task progress by task ID
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <returns>Task progress details</returns>
        Task<FileImportTaskDto> GetImportTaskProgressAsync(string taskId);

        /// <summary>
        /// Get import tasks by onboarding ID and stage ID
        /// </summary>
        /// <param name="onboardingId">Onboarding ID</param>
        /// <param name="stageId">Stage ID</param>
        /// <returns>List of import tasks</returns>
        Task<List<FileImportTaskDto>> GetImportTasksByStageAsync(long onboardingId, long stageId);

        /// <summary>
        /// Cancel a specific file import item
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <param name="itemId">Item ID to cancel</param>
        /// <returns>Cancel response</returns>
        Task<CancelImportFileResponseDto> CancelImportItemAsync(string taskId, string itemId);

        /// <summary>
        /// Cancel all pending items in a task
        /// </summary>
        /// <param name="taskId">Task ID</param>
        /// <returns>Cancel response</returns>
        Task<CancelImportFileResponseDto> CancelAllPendingItemsAsync(string taskId);
    }
}
