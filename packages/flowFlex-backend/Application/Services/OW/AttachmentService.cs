using FlowFlex.Application.Contracts;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using Microsoft.Extensions.Logging;
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
    /// </summary>
    public class AttachmentService : IAttachmentService, IScopedService
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IOnboardingFileRepository _onboardingFileRepository;
        private readonly ILogger<AttachmentService> _logger;

        public AttachmentService(
            IFileStorageService fileStorageService,
            IOnboardingFileRepository onboardingFileRepository,
            ILogger<AttachmentService> logger)
        {
            _fileStorageService = fileStorageService;
            _onboardingFileRepository = onboardingFileRepository;
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
            await Task.Delay(10, cancellationToken);
            return $"/files/{id}";
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
                    // Extract file path from AccessUrl or FilePath
                    string filePath;

                    if (!string.IsNullOrEmpty(attachment.FilePath))
                    {
                        // Remove /uploads/ prefix if present (for local storage compatibility)
                        filePath = attachment.FilePath.Replace("/uploads/", "");
                    }
                    else if (!string.IsNullOrEmpty(attachment.AccessUrl))
                    {
                        // For cloud storage, AccessUrl might be a full URL or a path
                        // CloudFileStorageService.GetFileAsync will handle URL extraction
                        filePath = attachment.AccessUrl;
                        
                        // If it's a local storage URL, remove /uploads/ prefix
                        if (filePath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                        {
                            filePath = filePath.Replace("/uploads/", "");
                        }
                    }
                    else
                    {
                        throw new FileNotFoundException($"No file path found for attachment {id}");
                    }

                    var (stream, fileName, contentType) = await _fileStorageService.GetFileAsync(filePath);

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
    }
}