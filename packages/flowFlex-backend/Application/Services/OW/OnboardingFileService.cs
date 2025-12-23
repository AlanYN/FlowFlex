using Microsoft.AspNetCore.Http;
using FlowFlex.Application.Contracts.Dtos.OW.OnboardingFile;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using AutoMapper;
using Microsoft.Extensions.Logging;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Models;
using FlowFlex.Application.Contracts;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Domain.Shared.Enums;
using System.IO;
using System.Net.Http;
using Item.Common.Lib.Common;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Shared.Exceptions;
using SqlSugar;
using FlowFlex.Application.Services.OW.Extensions;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Onboarding file service implementation
    /// </summary>
    public class OnboardingFileService : IOnboardingFileService, IScopedService
    {
        private readonly IOnboardingFileRepository _onboardingFileRepository;

        private readonly IOnboardingRepository _onboardingRepository;
        private readonly IStageRepository _stageRepository;
        private readonly IAttachmentService _attachmentService;
        private readonly IOperationChangeLogService _operationChangeLogService;
        private readonly IMapper _mapper;
        private readonly ILogger<OnboardingFileService> _logger;
        private readonly UserContext _userContext;
        private readonly IOperatorContextService _operatorContextService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IFileImportTaskService _fileImportTaskService;

        public OnboardingFileService(
            IOnboardingFileRepository onboardingFileRepository,

            IOnboardingRepository onboardingRepository,
            IStageRepository stageRepository,
            IAttachmentService attachmentService,
            IOperationChangeLogService operationChangeLogService,
            IMapper mapper,
            ILogger<OnboardingFileService> logger,
            UserContext userContext,
            IOperatorContextService operatorContextService,
            IHttpClientFactory httpClientFactory,
            IFileImportTaskService fileImportTaskService)
        {
            _onboardingFileRepository = onboardingFileRepository;

            _onboardingRepository = onboardingRepository;
            _stageRepository = stageRepository;
            _attachmentService = attachmentService;
            _operationChangeLogService = operationChangeLogService;
            _mapper = mapper;
            _logger = logger;
            _userContext = userContext;
            _operatorContextService = operatorContextService;
            _httpClientFactory = httpClientFactory;
            _fileImportTaskService = fileImportTaskService;
        }

        /// <summary>
        /// Log onboarding file change to Change Log
        /// </summary>
        private async Task LogOnboardingFileChangeAsync(long onboardingId, long? stageId, string fileName, string action, object fileInfo = null, string notes = null)
        {
            try
            {
                var onboarding = await _onboardingRepository.GetByIdAsync(onboardingId);
                Stage stage = null;
                if (stageId.HasValue)
                {
                    stage = await _stageRepository.GetByIdAsync(stageId.Value);
                }

                var logData = new
                {
                    OnboardingId = onboardingId,
                    LeadId = onboarding?.LeadId,
                    LeadName = onboarding?.LeadName,
                    StageId = stageId,
                    StageName = stage?.Name ?? "General",
                    FileName = fileName,
                    Action = action,
                    FileInfo = fileInfo,
                    Notes = notes,
                    ActionTime = DateTimeOffset.UtcNow,
                    ActionBy = _userContext?.UserName ?? "System",
                    Source = "onboarding_file"
                };

                // Stage completion log functionality removed
                // Debug logging handled by structured logging
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
            }
        }

        /// <summary>
        /// Upload file for onboarding
        /// </summary>
        public async Task<OnboardingFileOutputDto> UploadFileAsync(OnboardingFileInputDto input)
        {
            try
            {
                // Debug logging handled by structured logging
                // Validate file
                if (input.FormFile == null || input.FormFile.Length == 0)
                {
                    throw new CRMException(ErrorCodeEnum.BusinessError, "File is required");
                }
                // Debug logging handled by structured logging
                // Upload file to attachment system
                // Debug logging handled by structured logging
                var attachmentDto = new AttachmentDto
                {
                    FileData = input.FormFile,
                    CreateBy = _operatorContextService.GetOperatorDisplayName()
                };

                var attachment = await _attachmentService.CreateAttachmentAsync(
                    attachmentDto, _userContext.TenantId, CancellationToken.None);
                // Debug logging handled by structured logging
                // Create OnboardingFile record with minimal setup
                // Debug logging handled by structured logging
                var onboardingFile = new OnboardingFile
                {
                    OnboardingId = input.OnboardingId,
                    StageId = input.StageId,
                    AttachmentId = attachment.Id,
                    OriginalFileName = input.FormFile.FileName,
                    StoredFileName = attachment.FileName,
                    FileExtension = Path.GetExtension(input.FormFile.FileName),
                    FileSize = input.FormFile.Length,
                    ContentType = input.FormFile.ContentType,
                    Category = input.Category ?? "Document",
                    Description = input.Description,
                    IsRequired = input.IsRequired,
                    Tags = input.Tags,
                    AccessUrl = attachment.AccessUrl,
                    StoragePath = attachment.AccessUrl,
                    UploadedById = input.OverrideUploaderId ?? _userContext.UserId,
                    UploadedByName = input.OverrideUploaderName ?? _userContext.UserName,
                    UploadedDate = DateTimeOffset.UtcNow,
                    Status = "Active",
                    Version = 1,
                    SortOrder = 0
                };
                // Debug logging handled by structured logging
                // Initialize create information
                onboardingFile.InitCreateInfo(_userContext);
                // Debug logging handled by structured logging
                // Use simplified database insert
                // Debug logging handled by structured logging
                bool insertResult = await InsertOnboardingFileSimplified(onboardingFile);

                if (!insertResult)
                {
                    throw new CRMException(ErrorCodeEnum.BusinessError, "Failed to save file record to database");
                }

                try
                {
                    await _operationChangeLogService.LogFileUploadAsync(
                        onboardingFile.Id,
                        onboardingFile.OriginalFileName,
                        onboardingFile.OnboardingId,
                        onboardingFile.StageId,
                        onboardingFile.FileSize,
                        onboardingFile.ContentType,
                        onboardingFile.Category
                    );
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Operation log recording failed but continuing - Error: {ErrorMessage}", logEx.Message);
                    // Don't throw exception as main upload operation succeeded
                }

                // Debug logging handled by structured logging
                // Convert to output DTO
                var result = _mapper.Map<OnboardingFileOutputDto>(onboardingFile);
                result.FileSizeFormatted = FormatFileSize(onboardingFile.FileSize);
                result.DownloadUrl = $"/ow/onboarding-files/v1.0/{onboardingFile.Id}/download";
                // Debug logging handled by structured logging
                return result;
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                throw;
            }
        }

        /// <summary>
        /// Simplified database insert method that avoids complex filter operations
        /// </summary>
        private async Task<bool> InsertOnboardingFileSimplified(OnboardingFile onboardingFile)
        {
            try
            {
                // Debug logging handled by structured logging
                // Generate ID if not set
                if (onboardingFile.Id == 0)
                {
                    onboardingFile.InitNewId();
                    // Debug logging handled by structured logging
                }

                // Use SqlSugar direct insert without complex filter backup/restore
                var db = _onboardingFileRepository.GetType()
                    .GetField("db", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(_onboardingFileRepository) as ISqlSugarClient;

                if (db == null)
                {
                    // Debug logging handled by structured logging
                    return await _onboardingFileRepository.InsertAsync(onboardingFile);
                }
                // Debug logging handled by structured logging
                var result = await db.Insertable(onboardingFile).ExecuteCommandAsync();
                // Debug logging handled by structured logging
                return result > 0;
            }
            catch (Exception ex)
            {
                // Debug logging handled by structured logging
                // Last resort: try the original repository method
                try
                {
                    // Debug logging handled by structured logging
                    return await _onboardingFileRepository.InsertAsync(onboardingFile);
                }
                catch (Exception fallbackEx)
                {
                    // Debug logging handled by structured logging
                    throw;
                }
            }
        }

        public async Task<List<OnboardingFileOutputDto>> UploadMultipleFilesAsync(long onboardingId, long? stageId, List<IFormFile> formFiles, string category, string description)
        {
            var results = new List<OnboardingFileOutputDto>();

            foreach (var formFile in formFiles)
            {
                var input = new OnboardingFileInputDto
                {
                    OnboardingId = onboardingId,
                    StageId = stageId,
                    FormFile = formFile,
                    Category = category,
                    Description = description
                };

                var result = await UploadFileAsync(input);
                results.Add(result);
            }

            return results;
        }

        public async Task<List<OnboardingFileOutputDto>> GetFilesAsync(long onboardingId, long? stageId = null, string category = null)
        {
            try
            {
                var files = await _onboardingFileRepository.GetFilesByOnboardingAsync(onboardingId, stageId);

                if (!string.IsNullOrEmpty(category))
                {
                    files = files.Where(f => f.Category == category).ToList();
                }

                var results = _mapper.Map<List<OnboardingFileOutputDto>>(files);

                foreach (var result in results)
                {
                    result.FileSizeFormatted = FormatFileSize(result.FileSize);
                    result.DownloadUrl = $"/ow/onboarding-files/v1.0/{result.Id}/download";
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting files for Onboarding {onboardingId}");
                throw new CRMException(ErrorCodeEnum.SystemError, $"Failed to get files: {ex.Message}");
            }
        }

        public async Task<(Stream stream, string fileName, string contentType)> DownloadFileAsync(long fileId)
        {
            try
            {
                var onboardingFile = await _onboardingFileRepository.GetByIdAsync(fileId);
                if (onboardingFile == null || !onboardingFile.IsValid)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "File not found");
                }

                var (stream, attachmentDto) = await _attachmentService.GetAttachmentAsync(onboardingFile.AttachmentId);

                return (stream, onboardingFile.OriginalFileName, onboardingFile.ContentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading file {fileId}");
                throw new CRMException(ErrorCodeEnum.SystemError, $"File download failed: {ex.Message}");
            }
        }

        public async Task<string> GetFileUrlAsync(long fileId)
        {
            try
            {
                var onboardingFile = await _onboardingFileRepository.GetByIdAsync(fileId);
                if (onboardingFile == null || !onboardingFile.IsValid)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "File not found");
                }

                return await _attachmentService.GetAttachmentUrlAsync(onboardingFile.AttachmentId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting file URL {fileId}");
                throw new CRMException(ErrorCodeEnum.SystemError, $"Failed to get file URL: {ex.Message}");
            }
        }

        public async Task<bool> DeleteFileAsync(long fileId)
        {
            try
            {
                var onboardingFile = await _onboardingFileRepository.GetByIdAsync(fileId);
                if (onboardingFile == null || !onboardingFile.IsValid)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "File not found");
                }

                // Soft delete
                onboardingFile.IsValid = false;
                onboardingFile.Status = "Deleted";
                onboardingFile.ModifyBy = _operatorContextService.GetOperatorDisplayName();
                onboardingFile.ModifyUserId = _operatorContextService.GetOperatorId();
                onboardingFile.ModifyDate = DateTimeOffset.UtcNow;

                await _onboardingFileRepository.UpdateAsync(onboardingFile);

                // Log file deletion with detailed operation logging
                await _operationChangeLogService.LogFileDeleteAsync(
                    fileId,
                    onboardingFile.OriginalFileName,
                    onboardingFile.OnboardingId,
                    onboardingFile.StageId,
                    "File deleted by user request"
                );

                // Also log the legacy change log
                await LogOnboardingFileChangeAsync(
                    onboardingFile.OnboardingId,
                    onboardingFile.StageId,
                    onboardingFile.OriginalFileName,
                    "Delete File",
                    new { FileId = fileId },
                    $"File '{onboardingFile.OriginalFileName}' deleted"
                );

                _logger.LogInformation($"File deleted successfully: {fileId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file {fileId}");
                throw new CRMException(ErrorCodeEnum.SystemError, $"File deletion failed: {ex.Message}");
            }
        }

        public async Task<OnboardingFileOutputDto> UpdateFileAsync(long fileId, UpdateOnboardingFileInputDto input)
        {
            try
            {
                var onboardingFile = await _onboardingFileRepository.GetByIdAsync(fileId);
                if (onboardingFile == null || !onboardingFile.IsValid)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "File not found");
                }

                // Store old values for change tracking
                var oldData = new
                {
                    Category = onboardingFile.Category,
                    Description = onboardingFile.Description,
                    IsRequired = onboardingFile.IsRequired,
                    Tags = onboardingFile.Tags,
                    Status = onboardingFile.Status
                };

                var changedFields = new List<string>();

                // Update properties and track changes
                if (onboardingFile.Category != input.Category)
                {
                    onboardingFile.Category = input.Category;
                    changedFields.Add("Category");
                }

                if (onboardingFile.Description != input.Description)
                {
                    onboardingFile.Description = input.Description;
                    changedFields.Add("Description");
                }

                if (input.IsRequired.HasValue && onboardingFile.IsRequired != input.IsRequired.Value)
                {
                    onboardingFile.IsRequired = input.IsRequired.Value;
                    changedFields.Add("IsRequired");
                }

                if (onboardingFile.Tags != input.Tags)
                {
                    onboardingFile.Tags = input.Tags;
                    changedFields.Add("Tags");
                }

                if (!string.IsNullOrEmpty(input.Status) && onboardingFile.Status != input.Status)
                {
                    onboardingFile.Status = input.Status;
                    changedFields.Add("Status");
                }

                onboardingFile.ModifyBy = _operatorContextService.GetOperatorDisplayName();
                onboardingFile.ModifyUserId = _operatorContextService.GetOperatorId();
                onboardingFile.ModifyDate = DateTimeOffset.UtcNow;

                await _onboardingFileRepository.UpdateAsync(onboardingFile);

                // Log file update with detailed operation logging
                var newData = new
                {
                    Category = onboardingFile.Category,
                    Description = onboardingFile.Description,
                    IsRequired = onboardingFile.IsRequired,
                    Tags = onboardingFile.Tags,
                    Status = onboardingFile.Status
                };

                await _operationChangeLogService.LogFileUpdateAsync(
                    fileId,
                    onboardingFile.OriginalFileName,
                    onboardingFile.OnboardingId,
                    onboardingFile.StageId,
                    System.Text.Json.JsonSerializer.Serialize(oldData),
                    System.Text.Json.JsonSerializer.Serialize(newData),
                    changedFields
                );

                // Also log the legacy change log
                await LogOnboardingFileChangeAsync(
                    onboardingFile.OnboardingId,
                    onboardingFile.StageId,
                    onboardingFile.OriginalFileName,
                    "Update File",
                    new
                    {
                        FileId = fileId,
                        UpdatedFields = newData
                    },
                    $"File '{onboardingFile.OriginalFileName}' updated"
                );

                var result = _mapper.Map<OnboardingFileOutputDto>(onboardingFile);
                result.FileSizeFormatted = FormatFileSize(onboardingFile.FileSize);
                result.DownloadUrl = $"/ow/onboarding-files/v1.0/{onboardingFile.Id}/download";

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating file {fileId}");
                throw new CRMException(ErrorCodeEnum.SystemError, $"File update failed: {ex.Message}");
            }
        }

        public async Task<OnboardingFileStatisticsDto> GetFileStatisticsAsync(long onboardingId)
        {
            try
            {
                var files = await _onboardingFileRepository.GetFilesByOnboardingAsync(onboardingId);

                var statistics = new OnboardingFileStatisticsDto
                {
                    TotalFileCount = files.Count,
                    TotalFileSize = files.Sum(f => f.FileSize),
                    RequiredFileCount = files.Count(f => f.IsRequired),
                    OptionalFileCount = files.Count(f => !f.IsRequired),
                    ActiveFileCount = files.Count(f => f.Status == "Active"),
                    ArchivedFileCount = files.Count(f => f.Status == "Archived")
                };

                statistics.TotalFileSizeFormatted = FormatFileSize(statistics.TotalFileSize);

                // Statistics by category
                statistics.FileCountByCategory = files
                    .GroupBy(f => f.Category)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Statistics by file type
                statistics.FileCountByType = files
                    .GroupBy(f => f.FileExtension)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Recently uploaded files
                statistics.RecentFiles = _mapper.Map<List<OnboardingFileOutputDto>>(
                    files.OrderByDescending(f => f.UploadedDate).Take(5).ToList());

                foreach (var recentFile in statistics.RecentFiles)
                {
                    recentFile.FileSizeFormatted = FormatFileSize(recentFile.FileSize);
                    recentFile.DownloadUrl = $"/ow/onboarding-files/v1.0/{recentFile.Id}/download";
                }

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting file statistics for Onboarding {onboardingId}");
                throw new CRMException(ErrorCodeEnum.SystemError, $"Failed to get file statistics: {ex.Message}");
            }
        }

        public async Task<OnboardingFileOutputDto> GetFileByIdAsync(long fileId)
        {
            try
            {
                var onboardingFile = await _onboardingFileRepository.GetByIdAsync(fileId);
                if (onboardingFile == null || !onboardingFile.IsValid)
                {
                    throw new CRMException(ErrorCodeEnum.DataNotFound, "File not found");
                }

                var result = _mapper.Map<OnboardingFileOutputDto>(onboardingFile);
                result.FileSizeFormatted = FormatFileSize(onboardingFile.FileSize);
                result.DownloadUrl = $"/ow/onboarding-files/v1.0/{onboardingFile.Id}/download";

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting file by ID {fileId}");
                throw new CRMException(ErrorCodeEnum.SystemError, $"Failed to get file: {ex.Message}");
            }
        }

        public async Task<List<OnboardingFileOutputDto>> GetFilesByStageAsync(long stageId)
        {
            try
            {
                var files = await _onboardingFileRepository.GetFilesByStageAsync(stageId);
                var results = _mapper.Map<List<OnboardingFileOutputDto>>(files);

                foreach (var result in results)
                {
                    result.FileSizeFormatted = FormatFileSize(result.FileSize);
                    result.DownloadUrl = $"/ow/onboarding-files/v1.0/{result.Id}/download";
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting files by stage {stageId}");
                throw new CRMException(ErrorCodeEnum.SystemError, $"Failed to get files by stage: {ex.Message}");
            }
        }

        public async Task<List<OnboardingFileOutputDto>> GetFilesByCategoryAsync(string category, long onboardingId)
        {
            try
            {
                var files = await _onboardingFileRepository.GetFilesByCategoryAsync(onboardingId, category);
                var results = _mapper.Map<List<OnboardingFileOutputDto>>(files);

                foreach (var result in results)
                {
                    result.FileSizeFormatted = FormatFileSize(result.FileSize);
                    result.DownloadUrl = $"/ow/onboarding-files/v1.0/{result.Id}/download";
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting files by category {category} for Onboarding {onboardingId}");
                throw new CRMException(ErrorCodeEnum.SystemError, $"Failed to get files by category: {ex.Message}");
            }
        }

        public async Task<List<OnboardingFileOutputDto>> GetRequiredFilesAsync(long onboardingId)
        {
            try
            {
                var files = await _onboardingFileRepository.GetRequiredFilesAsync(onboardingId);
                var results = _mapper.Map<List<OnboardingFileOutputDto>>(files);

                foreach (var result in results)
                {
                    result.FileSizeFormatted = FormatFileSize(result.FileSize);
                    result.DownloadUrl = $"/ow/onboarding-files/v1.0/{result.Id}/download";
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting required files for Onboarding {onboardingId}");
                throw new CRMException(ErrorCodeEnum.SystemError, $"Failed to get required files: {ex.Message}");
            }
        }

        public async Task<OnboardingFileOutputDto> GetFileDetailsAsync(long fileId)
        {
            return await GetFileByIdAsync(fileId);
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 Bytes";

            string[] sizes = { "Bytes", "KB", "MB", "GB", "TB" };
            int i = (int)Math.Floor(Math.Log(bytes) / Math.Log(1024));

            return Math.Round(bytes / Math.Pow(1024, i), 2) + " " + sizes[i];
        }

        /// <summary>
        /// Import files from external URLs (e.g., OSS links)
        /// Downloads files from provided URLs and saves them as onboarding files
        /// </summary>
        public async Task<ImportFilesResultDto> ImportFilesFromUrlAsync(ImportFilesFromUrlInputDto input)
        {
            var result = new ImportFilesResultDto
            {
                TotalCount = input.Files?.Count ?? 0,
                SuccessCount = 0,
                FailedCount = 0,
                Items = new List<ImportFileProgressItemDto>()
            };

            if (input.Files == null || input.Files.Count == 0)
            {
                _logger.LogWarning("No files to import for OnboardingId {OnboardingId}", input.OnboardingId);
                return result;
            }

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromMinutes(10); // Set timeout for large files

            foreach (var fileItem in input.Files)
            {
                var progressItem = new ImportFileProgressItemDto
                {
                    DownloadLink = fileItem.DownloadLink,
                    FileName = fileItem.FileName,
                    Status = "Downloading",
                    ProgressPercentage = 0
                };
                result.Items.Add(progressItem);

                try
                {
                    // Extract filename from URL if not provided
                    if (string.IsNullOrEmpty(progressItem.FileName))
                    {
                        progressItem.FileName = ExtractFileNameFromUrl(fileItem.DownloadLink);
                    }

                    _logger.LogInformation("Starting download: {FileName} from {Url}", progressItem.FileName, fileItem.DownloadLink);

                    // Download file from URL
                    progressItem.Status = "Downloading";
                    progressItem.ProgressPercentage = 10;

                    using var response = await httpClient.GetAsync(fileItem.DownloadLink, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();

                    progressItem.ProgressPercentage = 30;

                    // Get content type from response
                    var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                    var contentLength = response.Content.Headers.ContentLength ?? 0;

                    // Read file content to memory stream
                    using var contentStream = await response.Content.ReadAsStreamAsync();
                    using var memoryStream = new MemoryStream();
                    await contentStream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    progressItem.Status = "Processing";
                    progressItem.ProgressPercentage = 60;

                    // Create IFormFile from downloaded content
                    var formFile = new FormFile(memoryStream, 0, memoryStream.Length, "file", progressItem.FileName)
                    {
                        Headers = new HeaderDictionary(),
                        ContentType = contentType
                    };

                    // Use existing upload method
                    var uploadInput = new OnboardingFileInputDto
                    {
                        OnboardingId = input.OnboardingId,
                        StageId = input.StageId,
                        FormFile = formFile,
                        Category = input.Category ?? "Document",
                        Description = fileItem.Description ?? input.Description
                    };

                    progressItem.ProgressPercentage = 80;

                    var uploadResult = await UploadFileAsync(uploadInput);

                    progressItem.Status = "Success";
                    progressItem.ProgressPercentage = 100;
                    progressItem.FileId = uploadResult.Id;
                    progressItem.FileOutput = uploadResult;
                    result.SuccessCount++;

                    _logger.LogInformation("Successfully imported file: {FileName}, FileId: {FileId}", progressItem.FileName, uploadResult.Id);
                }
                catch (HttpRequestException ex)
                {
                    progressItem.Status = "Failed";
                    progressItem.ErrorMessage = $"Download failed: {ex.Message}";
                    progressItem.ProgressPercentage = 0;
                    result.FailedCount++;
                    _logger.LogError(ex, "Failed to download file from {Url}: {ErrorMessage}", fileItem.DownloadLink, ex.Message);
                }
                catch (Exception ex)
                {
                    progressItem.Status = "Failed";
                    progressItem.ErrorMessage = $"Import failed: {ex.Message}";
                    progressItem.ProgressPercentage = 0;
                    result.FailedCount++;
                    _logger.LogError(ex, "Failed to import file {FileName}: {ErrorMessage}", progressItem.FileName, ex.Message);
                }
            }

            _logger.LogInformation("Import completed: {SuccessCount}/{TotalCount} files imported successfully for OnboardingId {OnboardingId}",
                result.SuccessCount, result.TotalCount, input.OnboardingId);

            return result;
        }

        /// <summary>
        /// Extract file name from URL
        /// </summary>
        private string ExtractFileNameFromUrl(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                    return $"file_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

                var uri = new Uri(url);
                var path = uri.AbsolutePath;

                // Get the last segment of the path
                var fileName = Path.GetFileName(path);

                // Remove any URL encoding
                fileName = Uri.UnescapeDataString(fileName);

                // If filename is empty or just an extension, generate one
                if (string.IsNullOrEmpty(fileName) || fileName.StartsWith("."))
                {
                    fileName = $"file_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{fileName}";
                }

                // Clean up filename - remove query parameters that might be included
                var queryIndex = fileName.IndexOf('?');
                if (queryIndex > 0)
                {
                    fileName = fileName.Substring(0, queryIndex);
                }

                return fileName;
            }
            catch
            {
                return $"file_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            }
        }

        /// <summary>
        /// Start an async import task from external URLs
        /// Returns immediately with task ID, import runs in background
        /// </summary>
        public async Task<StartImportTaskResponseDto> StartImportTaskAsync(ImportFilesFromUrlInputDto input, string createdBy)
        {
            return await _fileImportTaskService.StartImportTaskAsync(input, createdBy);
        }

        /// <summary>
        /// Get import task progress by task ID
        /// </summary>
        public async Task<FileImportTaskDto> GetImportTaskProgressAsync(string taskId)
        {
            return await _fileImportTaskService.GetTaskProgressAsync(taskId);
        }

        /// <summary>
        /// Get import tasks by onboarding ID and stage ID
        /// </summary>
        public async Task<List<FileImportTaskDto>> GetImportTasksByStageAsync(long onboardingId, long stageId)
        {
            return await _fileImportTaskService.GetTasksByOnboardingAndStageAsync(onboardingId, stageId);
        }

        /// <summary>
        /// Cancel a specific file import item
        /// </summary>
        public async Task<CancelImportFileResponseDto> CancelImportItemAsync(string taskId, string itemId)
        {
            return await _fileImportTaskService.CancelImportItemAsync(taskId, itemId);
        }

        /// <summary>
        /// Cancel all pending items in a task
        /// </summary>
        public async Task<CancelImportFileResponseDto> CancelAllPendingItemsAsync(string taskId)
        {
            return await _fileImportTaskService.CancelAllPendingItemsAsync(taskId);
        }
    }
}