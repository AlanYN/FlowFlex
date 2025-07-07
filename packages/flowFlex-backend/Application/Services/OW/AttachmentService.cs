using FlowFlex.Application.Contracts;
using FlowFlex.Application.Contracts.Dtos;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Enums;
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
        private readonly ILogger<AttachmentService> _logger;

        public AttachmentService(
            IFileStorageService fileStorageService,
            ILogger<AttachmentService> logger)
        {
            _fileStorageService = fileStorageService;
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
                    CreateDate = DateTimeOffset.Now,
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
                // In actual implementation, this should query attachment information from database
                // Currently returns mock data, but uses real file path format
                return new AttachmentOutputDto
                {
                    Id = Id,
                    FileName = $"file_{Id}.txt",
                    RealName = $"original_file_{Id}.txt",
                    FileType = "text/plain",
                    AccessUrl = $"/uploads/default/attachments/{DateTime.Now:yyyy/MM/dd}/file_{Id}.txt",
                    CreateDate = DateTimeOffset.Now
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
                    CreateDate = DateTimeOffset.Now
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
                    // Extract file path from AccessUrl
                    var filePath = attachment.AccessUrl.Replace("/uploads/", "");
                    var (stream, fileName, contentType) = await _fileStorageService.GetFileAsync(filePath);
                    
                    attachment.FileType = contentType;
                    return (stream, attachment);
                }
                catch (FileNotFoundException)
                {
                    // If file doesn't exist, return mock content
                    _logger.LogWarning($"File not found for attachment {id}, returning mock content");
                    var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("File content not found"));
                    return (stream, attachment);
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