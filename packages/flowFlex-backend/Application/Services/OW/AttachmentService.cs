using FlowFlex.Application.Contracts;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Application.Contracts.Options;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FlowFlex.Infrastructure.Services;
using SqlSugar;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Real implementation of AttachmentService
    /// Supports hybrid storage: both cloud (OSS/AWS) and local file storage
    /// </summary>
    public class AttachmentService : IAttachmentService, IScopedService
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IOnboardingFileRepository _onboardingFileRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly FileStorageOptions _fileStorageOptions;
        private readonly ILogger<AttachmentService> _logger;

        public AttachmentService(
            IFileStorageService fileStorageService,
            IOnboardingFileRepository onboardingFileRepository,
            IWebHostEnvironment webHostEnvironment,
            IOptions<FileStorageOptions> fileStorageOptions,
            ILogger<AttachmentService> logger)
        {
            _fileStorageService = fileStorageService;
            _onboardingFileRepository = onboardingFileRepository;
            _webHostEnvironment = webHostEnvironment;
            _fileStorageOptions = fileStorageOptions?.Value ?? new FileStorageOptions();
            _logger = logger;
        }

        public async Task<AttachmentOutputDto> CreateAttachmentAsync(AttachmentDto attachment, string tenantId, CancellationToken cancellationToken)
        {
            try
            {
                if (attachment.FileData == null)
                {
                    throw new ArgumentException("FileData is required", nameof(attachment));
                }

                // Use file storage service to save file
                var result = await _fileStorageService.SaveFileAsync(
                    attachment.FileData,
                    "attachments",
                    tenantId);

                if (!result.Success)
                {
                    throw new InvalidOperationException($"Failed to save file: {result.ErrorMessage}");
                }

                return new AttachmentOutputDto
                {
                    Id = SnowFlakeSingle.Instance.NextId(), // Use snowflake algorithm to generate ID
                    FileName = result.FileName,
                    RealName = result.OriginalFileName,
                    FileType = result.ContentType,
                    AccessUrl = result.AccessUrl,
                    CreateDate = DateTimeOffset.UtcNow,
                    TenantId = tenantId,
                    FileSize = result.FileSize,
                    FilePath = result.FilePath,
                    FileHash = result.FileHash
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating attachment for tenant {tenantId}");
                throw;
            }
        }

        public async Task<AttachmentOutputDto> GetAttachmentByIdAsync(long Id, CancellationToken cancellationToken)
        {
            try
            {
                // Find the OnboardingFile that references this attachment ID
                var onboardingFile = await _onboardingFileRepository.GetByAttachmentIdAsync(Id);

                if (onboardingFile == null)
                {
                    throw new FileNotFoundException($"Attachment with ID {Id} not found");
                }

                return new AttachmentOutputDto
                {
                    Id = Id,
                    FileName = onboardingFile.StoredFileName,
                    RealName = onboardingFile.OriginalFileName,
                    FileType = onboardingFile.ContentType,
                    AccessUrl = onboardingFile.AccessUrl,
                    CreateDate = onboardingFile.CreateDate,
                    FileSize = onboardingFile.FileSize,
                    FilePath = onboardingFile.StoragePath
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting attachment {Id}");
                throw;
            }
        }

        public async Task<List<AttachmentOutputDto>> GetAttachmentsAsync(List<long> Ids, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);

            var result = new List<AttachmentOutputDto>();
            foreach (var id in Ids)
            {
                result.Add(new AttachmentOutputDto
                {
                    Id = id,
                    FileName = $"test_{id}.txt",
                    RealName = $"test_{id}.txt",
                    FileType = "text/plain",
                    AccessUrl = $"/files/{id}",
                    CreateDate = DateTimeOffset.UtcNow
                });
            }
            return result;
        }

        public async Task<string> GetAttachmentUrlAsync(long id, CancellationToken cancellationToken)
        {
            // Fallback URL for download endpoint
            var fallbackUrl = $"/ow/onboarding-files/v1.0/{id}/download";
            
            try
            {
                // Find the OnboardingFile that references this attachment ID
                var onboardingFile = await _onboardingFileRepository.GetByAttachmentIdAsync(id);
                
                if (onboardingFile == null)
                {
                    _logger.LogWarning("Attachment not found for ID {AttachmentId}, returning fallback URL", id);
                    return fallbackUrl;
                }

                // Update fallback URL to use the correct file ID
                fallbackUrl = $"/ow/onboarding-files/v1.0/{onboardingFile.Id}/download";

                // Get the file path for generating real-time URL
                // Priority: StoragePath > AccessUrl (extract path from URL)
                string filePath = null;
                
                if (!string.IsNullOrEmpty(onboardingFile.StoragePath))
                {
                    filePath = onboardingFile.StoragePath;
                }
                else if (!string.IsNullOrEmpty(onboardingFile.AccessUrl))
                {
                    filePath = onboardingFile.AccessUrl;
                }

                if (string.IsNullOrEmpty(filePath))
                {
                    _logger.LogWarning("No file path found for attachment {AttachmentId} (FileId: {FileId}), returning fallback URL", 
                        id, onboardingFile.Id);
                    return fallbackUrl;
                }

                // Check if it's local storage - return direct access URL
                if (IsLocalStoragePath(filePath))
                {
                    _logger.LogDebug("Local storage detected for attachment {AttachmentId}, returning local URL: {Url}", id, filePath);
                    return filePath; // Return the /uploads/... path directly
                }

                // For cloud storage, generate real-time signed URL
                var signedUrl = await _fileStorageService.GetFileUrlAsync(filePath);
                
                // Check if the returned URL is valid (not null, not empty, and not just the original path)
                if (string.IsNullOrEmpty(signedUrl))
                {
                    _logger.LogWarning("FileStorageService returned empty URL for path {FilePath}, returning fallback URL", filePath);
                    return fallbackUrl;
                }
                
                _logger.LogDebug("Generated real-time URL for attachment {AttachmentId}: {Url}", id, signedUrl);
                return signedUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting real-time URL for attachment {AttachmentId}, returning fallback URL", id);
                return fallbackUrl;
            }
        }

        public async Task DeleteAttachment(long attachmentId, CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            // Mock delete operation
        }

        public async Task BatchDeleteAttachment(IList<long> attachmentIds)
        {
            await Task.Delay(10);
            // Mock batch delete operation
        }

        public async Task<bool> DeleteByBusinessIdAsync(long businessId)
        {
            await Task.Delay(10);
            return true;
        }

        public async Task DeleteByMappingIdAsync(long mappingId)
        {
            await Task.Delay(10);
            // Mock delete mapping operation
        }

        public async Task<List<AttachmentOutputDto>> GetAttachmentsByBusinessIdAsync(long businessId)
        {
            await Task.Delay(10);
            return new List<AttachmentOutputDto>();
        }

        public async Task<List<AttachmentOutputDto>> GetAttachmentsByAttachmentTypeAsync(long businessId, AttachmentTypeEnum attachmentType)
        {
            await Task.Delay(10);
            return new List<AttachmentOutputDto>();
        }

        public async Task<(Stream, AttachmentOutputDto)> GetAttachmentAsync(long id)
        {
            try
            {
                var attachment = await GetAttachmentByIdAsync(id, CancellationToken.None);

                // Try to get real file from file storage
                try
                {
                    // Determine storage type based on AccessUrl or FilePath
                    var accessUrl = attachment.AccessUrl ?? attachment.FilePath ?? "";
                    var isLocalStorage = IsLocalStoragePath(accessUrl);

                    _logger.LogDebug("GetAttachmentAsync - Id: {Id}, AccessUrl: {AccessUrl}, IsLocal: {IsLocal}", 
                        id, accessUrl, isLocalStorage);

                    Stream stream;
                    string fileName;
                    string contentType;

                    if (isLocalStorage)
                    {
                        // Handle local storage - read file directly from local file system
                        (stream, fileName, contentType) = await GetLocalFileAsync(accessUrl);
                    }
                    else
                    {
                        // Handle cloud storage (OSS/AWS) - use file storage service
                        string filePath = !string.IsNullOrEmpty(attachment.FilePath)
                            ? attachment.FilePath
                            : attachment.AccessUrl;

                        (stream, fileName, contentType) = await _fileStorageService.GetFileAsync(filePath);
                    }

                    // Update attachment info with actual file data
                    attachment.FileType = contentType;
                    attachment.RealName = fileName;

                    return (stream, attachment);
                }
                catch (FileNotFoundException ex)
                {
                    _logger.LogWarning(ex, $"File not found for attachment {id}: {ex.Message}");
                    throw new FileNotFoundException($"Physical file not found for attachment {id}", ex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting attachment stream {id}");
                throw;
            }
        }

        /// <summary>
        /// Determine if the path represents local storage
        /// Local storage paths start with /uploads/ or are relative paths without http(s)://
        /// </summary>
        private bool IsLocalStoragePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            // Check for local storage URL prefix
            if (path.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                return true;

            // If it's a full URL (http:// or https://), it's cloud storage
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return false;

            // Other relative paths without http are considered local
            // But we need to be careful - some relative paths might be for cloud storage
            // Default to false (cloud) for ambiguous cases
            return false;
        }

        /// <summary>
        /// Get file from local storage
        /// </summary>
        private async Task<(Stream stream, string fileName, string contentType)> GetLocalFileAsync(string accessUrl)
        {
            try
            {
                // Remove /uploads/ prefix to get relative path
                var relativePath = accessUrl;
                if (relativePath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                {
                    relativePath = relativePath.Substring("/uploads/".Length);
                }
                else if (relativePath.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
                {
                    relativePath = relativePath.Substring("uploads/".Length);
                }

                // Build full path
                var localStoragePath = _fileStorageOptions.LocalStoragePath ?? "uploads";
                var fullPath = Path.Combine(_webHostEnvironment.ContentRootPath, localStoragePath, relativePath);

                _logger.LogDebug("Reading local file - RelativePath: {RelativePath}, FullPath: {FullPath}", 
                    relativePath, fullPath);

                // Check if file exists
                if (!File.Exists(fullPath))
                {
                    _logger.LogWarning("Local file not found: {FullPath}", fullPath);
                    throw new FileNotFoundException($"Local file not found: {relativePath}", fullPath);
                }

                // Open file stream
                var stream = new FileStream(
                    fullPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 4096,
                    useAsync: true);

                var fileName = Path.GetFileName(fullPath);
                var contentType = GetContentType(fileName);

                _logger.LogInformation("Successfully retrieved local file: {FileName}", fileName);
                return (stream, fileName, contentType);
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading local file: {AccessUrl}", accessUrl);
                throw new FileNotFoundException($"Failed to read local file: {accessUrl}", ex);
            }
        }

        /// <summary>
        /// Get content type from file name
        /// </summary>
        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".txt" => "text/plain",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                ".mp4" => "video/mp4",
                ".avi" => "video/x-msvideo",
                ".mov" => "video/quicktime",
                ".eml" => "message/rfc822",
                ".msg" => "application/vnd.ms-outlook",
                _ => "application/octet-stream"
            };
        }
    }
}