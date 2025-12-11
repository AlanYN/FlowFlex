using System.Collections.Concurrent;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FlowFlex.Application.Contracts.Dtos.OW.OnboardingFile;
using FlowFlex.Application.Contracts.IServices.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// File import task service implementation
    /// Uses in-memory storage for task tracking with background processing
    /// </summary>
    public class FileImportTaskService : IFileImportTaskService, ISingletonService
    {
        private readonly ConcurrentDictionary<string, FileImportTaskDto> _tasks = new();
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _taskCancellationTokens = new();
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOnboardingFileService _onboardingFileService;
        private readonly ILogger<FileImportTaskService> _logger;

        // Lazy initialization to avoid circular dependency
        private IOnboardingFileService _lazyFileService;
        private readonly IServiceProvider _serviceProvider;

        public FileImportTaskService(
            IHttpClientFactory httpClientFactory,
            IServiceProvider serviceProvider,
            ILogger<FileImportTaskService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        private IOnboardingFileService GetFileService()
        {
            if (_lazyFileService == null)
            {
                using var scope = _serviceProvider.CreateScope();
                _lazyFileService = scope.ServiceProvider.GetRequiredService<IOnboardingFileService>();
            }
            return _lazyFileService;
        }

        /// <summary>
        /// Start a new import task
        /// </summary>
        public async Task<StartImportTaskResponseDto> StartImportTaskAsync(ImportFilesFromUrlInputDto input, string createdBy)
        {
            var taskId = Guid.NewGuid().ToString("N");
            var cts = new CancellationTokenSource();

            var task = new FileImportTaskDto
            {
                TaskId = taskId,
                OnboardingId = input.OnboardingId,
                StageId = input.StageId,
                Status = "InProgress",
                TotalCount = input.Files?.Count ?? 0,
                SuccessCount = 0,
                FailedCount = 0,
                CancelledCount = 0,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = createdBy,
                Items = new List<FileImportItemDto>()
            };

            // Initialize items
            if (input.Files != null)
            {
                foreach (var file in input.Files)
                {
                    var itemId = Guid.NewGuid().ToString("N");
                    task.Items.Add(new FileImportItemDto
                    {
                        ItemId = itemId,
                        DownloadLink = file.DownloadLink,
                        FileName = file.FileName ?? ExtractFileNameFromUrl(file.DownloadLink),
                        Status = "Pending",
                        ProgressPercentage = 0
                    });
                }
            }

            _tasks[taskId] = task;
            _taskCancellationTokens[taskId] = cts;

            // Start background processing with error logging
            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessImportTaskAsync(taskId, input, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Import task {TaskId} was cancelled", taskId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Import task {TaskId} failed with error", taskId);
                    if (_tasks.TryGetValue(taskId, out var failedTask))
                    {
                        failedTask.Status = "Failed";
                        failedTask.CompletedAt = DateTimeOffset.UtcNow;
                    }
                }
            });

            _logger.LogInformation("Started import task {TaskId} with {FileCount} files for Onboarding {OnboardingId}, Stage {StageId}",
                taskId, task.TotalCount, input.OnboardingId, input.StageId);

            return new StartImportTaskResponseDto
            {
                TaskId = taskId,
                OnboardingId = input.OnboardingId,
                StageId = input.StageId,
                TotalCount = task.TotalCount,
                Message = $"Import task started. Use task ID '{taskId}' to track progress."
            };
        }

        /// <summary>
        /// Process import task in background
        /// </summary>
        private async Task ProcessImportTaskAsync(string taskId, ImportFilesFromUrlInputDto input, CancellationToken cancellationToken)
        {
            if (!_tasks.TryGetValue(taskId, out var task))
            {
                _logger.LogWarning("Task {TaskId} not found during processing", taskId);
                return;
            }

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromMinutes(10);

            for (int i = 0; i < task.Items.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var item = task.Items[i];
                var fileInput = input.Files[i];

                // Check if item was cancelled
                if (item.Status == "Cancelled")
                {
                    continue;
                }

                try
                {
                    item.Status = "Downloading";
                    item.ProgressPercentage = 10;

                    _logger.LogInformation("Downloading file {FileName} for task {TaskId}", item.FileName, taskId);

                    // Check cancellation before download
                    if (cancellationToken.IsCancellationRequested || item.Status == "Cancelled")
                    {
                        item.Status = "Cancelled";
                        task.CancelledCount++;
                        continue;
                    }

                    using var response = await httpClient.GetAsync(fileInput.DownloadLink, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    item.ProgressPercentage = 30;

                    var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

                    using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    using var memoryStream = new MemoryStream();
                    await contentStream.CopyToAsync(memoryStream, cancellationToken);
                    memoryStream.Position = 0;

                    // TODO: Remove this delay - only for testing progress UI
                    item.ProgressPercentage = 50;
                    _logger.LogWarning("DEBUG: Waiting 3 seconds at 50% progress for file: {FileName}...", item.FileName);
                    await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
                    _logger.LogWarning("DEBUG: Wait completed for file: {FileName}", item.FileName);

                    item.Status = "Processing";
                    item.ProgressPercentage = 60;

                    // Check cancellation before processing
                    if (cancellationToken.IsCancellationRequested || item.Status == "Cancelled")
                    {
                        item.Status = "Cancelled";
                        task.CancelledCount++;
                        continue;
                    }

                    // Create IFormFile and upload
                    var formFile = new FormFile(memoryStream, 0, memoryStream.Length, "file", item.FileName)
                    {
                        Headers = new HeaderDictionary(),
                        ContentType = contentType
                    };

                    var uploadInput = new OnboardingFileInputDto
                    {
                        OnboardingId = input.OnboardingId,
                        StageId = input.StageId,
                        FormFile = formFile,
                        Category = input.Category ?? "Document",
                        Description = fileInput.Description ?? input.Description,
                        OverrideUploaderId = input.OperatorId,
                        OverrideUploaderName = input.OperatorName
                    };

                    item.ProgressPercentage = 80;

                    // Use scoped service for upload
                    using var scope = _serviceProvider.CreateScope();
                    var fileService = scope.ServiceProvider.GetRequiredService<IOnboardingFileService>();
                    var uploadResult = await fileService.UploadFileAsync(uploadInput);

                    item.Status = "Success";
                    item.ProgressPercentage = 100;
                    item.FileId = uploadResult.Id;
                    item.FileOutput = uploadResult;
                    task.SuccessCount++;

                    _logger.LogInformation("Successfully imported file {FileName}, FileId: {FileId} for task {TaskId}",
                        item.FileName, uploadResult.Id, taskId);

                    // Remove completed item from memory
                    task.Items.Remove(item);
                    _logger.LogDebug("Removed completed item {ItemId} from task {TaskId}", item.ItemId, taskId);
                }
                catch (OperationCanceledException)
                {
                    item.Status = "Cancelled";
                    item.ErrorMessage = "Import was cancelled";
                    task.CancelledCount++;
                    _logger.LogInformation("File import cancelled: {FileName} for task {TaskId}", item.FileName, taskId);
                }
                catch (HttpRequestException ex)
                {
                    item.Status = "Failed";
                    item.ErrorMessage = $"Download failed: {ex.Message}";
                    item.ProgressPercentage = 0;
                    task.FailedCount++;
                    _logger.LogError(ex, "Failed to download file {FileName} for task {TaskId}", item.FileName, taskId);
                }
                catch (Exception ex)
                {
                    item.Status = "Failed";
                    item.ErrorMessage = $"Import failed: {ex.Message}";
                    item.ProgressPercentage = 0;
                    task.FailedCount++;
                    _logger.LogError(ex, "Failed to import file {FileName} for task {TaskId}", item.FileName, taskId);
                }
            }

            // Update task status
            task.CompletedAt = DateTimeOffset.UtcNow;
            if (task.CancelledCount == task.TotalCount)
            {
                task.Status = "Cancelled";
            }
            else if (task.FailedCount == task.TotalCount)
            {
                task.Status = "Failed";
            }
            else if (task.SuccessCount == task.TotalCount)
            {
                task.Status = "Completed";
            }
            else
            {
                task.Status = "PartiallyCompleted";
            }

            // Clean up cancellation token
            if (_taskCancellationTokens.TryRemove(taskId, out var cts))
            {
                cts.Dispose();
            }

            _logger.LogInformation("Import task {TaskId} completed: {Status}, Success: {SuccessCount}, Failed: {FailedCount}, Cancelled: {CancelledCount}",
                taskId, task.Status, task.SuccessCount, task.FailedCount, task.CancelledCount);

            // Remove task from memory if all items are completed successfully (no items left)
            if (task.Items.Count == 0)
            {
                _tasks.TryRemove(taskId, out _);
                _logger.LogInformation("Task {TaskId} removed from memory - all items completed successfully", taskId);
            }
        }

        /// <summary>
        /// Get import task progress by task ID
        /// </summary>
        public Task<FileImportTaskDto> GetTaskProgressAsync(string taskId)
        {
            if (_tasks.TryGetValue(taskId, out var task))
            {
                // Items are already removed from memory when completed (progressPercentage = 100)
                return Task.FromResult(task);
            }

            return Task.FromResult<FileImportTaskDto>(null);
        }

        /// <summary>
        /// Get import tasks by onboarding ID and stage ID
        /// </summary>
        public Task<List<FileImportTaskDto>> GetTasksByOnboardingAndStageAsync(long onboardingId, long stageId)
        {
            // Tasks and items are automatically removed from memory when completed
            var tasks = _tasks.Values
                .Where(t => t.OnboardingId == onboardingId && t.StageId == stageId)
                .OrderByDescending(t => t.CreatedAt)
                .ToList();

            return Task.FromResult(tasks);
        }

        /// <summary>
        /// Cancel a specific file import item
        /// </summary>
        public Task<CancelImportFileResponseDto> CancelImportItemAsync(string taskId, string itemId)
        {
            if (!_tasks.TryGetValue(taskId, out var task))
            {
                return Task.FromResult(new CancelImportFileResponseDto
                {
                    Success = false,
                    Message = $"Task '{taskId}' not found",
                    CancelledCount = 0
                });
            }

            var item = task.Items.FirstOrDefault(i => i.ItemId == itemId);
            if (item == null)
            {
                return Task.FromResult(new CancelImportFileResponseDto
                {
                    Success = false,
                    Message = $"Item '{itemId}' not found in task '{taskId}'",
                    CancelledCount = 0
                });
            }

            if (!item.CanCancel)
            {
                return Task.FromResult(new CancelImportFileResponseDto
                {
                    Success = false,
                    Message = $"Item '{itemId}' cannot be cancelled (status: {item.Status})",
                    CancelledCount = 0
                });
            }

            item.Status = "Cancelled";
            item.ErrorMessage = "Cancelled by user";
            task.CancelledCount++;

            _logger.LogInformation("Cancelled import item {ItemId} ({FileName}) in task {TaskId}", itemId, item.FileName, taskId);

            return Task.FromResult(new CancelImportFileResponseDto
            {
                Success = true,
                Message = $"Successfully cancelled import of '{item.FileName}'",
                CancelledCount = 1
            });
        }

        /// <summary>
        /// Cancel all pending items in a task
        /// </summary>
        public Task<CancelImportFileResponseDto> CancelAllPendingItemsAsync(string taskId)
        {
            if (!_tasks.TryGetValue(taskId, out var task))
            {
                return Task.FromResult(new CancelImportFileResponseDto
                {
                    Success = false,
                    Message = $"Task '{taskId}' not found",
                    CancelledCount = 0
                });
            }

            var cancelledCount = 0;
            foreach (var item in task.Items.Where(i => i.CanCancel))
            {
                item.Status = "Cancelled";
                item.ErrorMessage = "Cancelled by user";
                task.CancelledCount++;
                cancelledCount++;
            }

            // Also trigger cancellation token if task is still running
            if (_taskCancellationTokens.TryGetValue(taskId, out var cts))
            {
                cts.Cancel();
            }

            _logger.LogInformation("Cancelled {Count} pending items in task {TaskId}", cancelledCount, taskId);

            return Task.FromResult(new CancelImportFileResponseDto
            {
                Success = true,
                Message = $"Successfully cancelled {cancelledCount} pending items",
                CancelledCount = cancelledCount
            });
        }

        /// <summary>
        /// Clean up completed tasks older than specified time
        /// </summary>
        public void CleanupOldTasks(TimeSpan olderThan)
        {
            var cutoffTime = DateTimeOffset.UtcNow - olderThan;
            var tasksToRemove = _tasks
                .Where(kvp => kvp.Value.IsCompleted && kvp.Value.CompletedAt < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var taskId in tasksToRemove)
            {
                _tasks.TryRemove(taskId, out _);
                _taskCancellationTokens.TryRemove(taskId, out var cts);
                cts?.Dispose();
            }

            if (tasksToRemove.Count > 0)
            {
                _logger.LogInformation("Cleaned up {Count} old import tasks", tasksToRemove.Count);
            }
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
                var fileName = Path.GetFileName(path);
                fileName = Uri.UnescapeDataString(fileName);

                if (string.IsNullOrEmpty(fileName) || fileName.StartsWith("."))
                {
                    fileName = $"file_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{fileName}";
                }

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
    }
}

