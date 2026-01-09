using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FlowFlex.Application.Contracts;
using FlowFlex.Application.Contracts.Helpers;
using FlowFlex.Application.Contracts.Options;
using Application.Contracts.Options;
using FlowFlex.Domain.Shared;
using System.Security.Cryptography;
using System.Text;
using Item.BlobProvider;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Cloud file storage service implementation using OSS/AWS
    /// </summary>
    public class CloudFileStorageService : IFileStorageService, IScopedService
    {
        private readonly FileStorageOptions _options;
        private readonly IBlobContainer _blobContainer;
        private readonly ILogger<CloudFileStorageService> _logger;
        private readonly IOptions<BlobStoreOptions> _blobStoreOptions;

        public CloudFileStorageService(
            IOptions<FileStorageOptions> options,
            IBlobContainer blobContainer,
            ILogger<CloudFileStorageService> logger,
            IOptions<BlobStoreOptions> blobStoreOptions)
        {
            _options = options.Value;
            _blobContainer = blobContainer;
            _logger = logger;
            _blobStoreOptions = blobStoreOptions;
        }

        public async Task<FileStorageResult> SaveFileAsync(IFormFile file, string category = "default", string tenantId = "default")
        {
            try
            {
                // Validate file
                var validation = await ValidateFileAsync(file);
                if (!validation.IsValid)
                {
                    return new FileStorageResult
                    {
                        Success = false,
                        ErrorMessage = validation.ErrorMessage
                    };
                }

                // Generate file path
                var fileName = GenerateFileName(file.FileName);
                var relativePath = GenerateFilePath(fileName, category, tenantId);

                // Calculate file hash and upload to cloud storage
                string fileHash;
                using (var source = file.OpenReadStream())
                using (var sha256 = SHA256.Create())
                {
                    // Read file into memory stream for hash calculation and upload
                    using (var memoryStream = new MemoryStream())
                    {
                        var buffer = new byte[81920];
                        int read;
                        while ((read = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await memoryStream.WriteAsync(buffer, 0, read);
                            sha256.TransformBlock(buffer, 0, read, null, 0);
                        }
                        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                        fileHash = Convert.ToHexString(sha256.Hash!).ToLowerInvariant();

                        // Reset stream position for upload
                        memoryStream.Position = 0;

                        // Upload to cloud storage
                        await _blobContainer.SaveAsync(
                            relativePath,
                            memoryStream,
                            overrideExisting: true,
                            CancellationToken.None);
                    }
                }

                // Get access URL
                var accessUrl = await _blobContainer.GetAccessUrl(relativePath);

                var result = new FileStorageResult
                {
                    Success = true,
                    FilePath = relativePath,
                    FileName = fileName,
                    OriginalFileName = file.FileName,
                    FileSize = file.Length,
                    ContentType = file.ContentType,
                    AccessUrl = accessUrl,
                    FileHash = fileHash
                };

                _logger.LogInformation($"File saved successfully to cloud storage: {relativePath}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving file to cloud storage: {file.FileName}");
                
                // Provide more specific error messages for common OSS errors
                string errorMessage = ex.Message;
                if (ex.Message.Contains("bucket acl", StringComparison.OrdinalIgnoreCase) || 
                    ex.Message.Contains("no right to access", StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = "OSS Bucket ACL权限不足。解决方案：1) 在阿里云OSS控制台检查Bucket的读写权限设置；2) 确保AccessKey对应的RAM用户具有oss:PutObject、oss:GetObject权限；3) 检查Bucket的ACL是否为私有读写（private）或公共读（public-read），确保RAM用户有足够权限；4) 如果使用RAM子账号，请检查RAM策略是否包含完整的OSS操作权限。";
                }
                else if (ex.Message.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
                         ex.Message.Contains("InvalidAccessKeyId", StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = "访问被拒绝。请检查：1) AccessKey和SecretKey是否正确；2) AccessKey是否已启用；3) RAM用户是否有足够的权限策略。";
                }
                else if (ex.Message.Contains("bucket not found", StringComparison.OrdinalIgnoreCase) ||
                         ex.Message.Contains("NoSuchBucket", StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = "Bucket不存在或配置错误。请检查配置中的Bucket名称是否正确，以及Bucket是否在正确的Region中。";
                }
                else if (ex.Message.Contains("SignatureDoesNotMatch", StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = "签名不匹配。请检查SecretAccessKey是否正确。";
                }
                
                return new FileStorageResult
                {
                    Success = false,
                    ErrorMessage = $"文件上传失败: {errorMessage}"
                };
            }
        }

        public async Task<(Stream stream, string fileName, string contentType)> GetFileAsync(string filePath)
        {
            try
            {
                // Extract file path from URL if it's a full OSS URL
                string actualFilePath = ExtractFilePathFromUrl(filePath);
                
                _logger.LogDebug($"Attempting to get file from cloud storage. Original: {filePath}, Extracted path: {actualFilePath}");
                
                // Try to get the file
                var stream = await _blobContainer.GetAsync(actualFilePath);
                var fileName = Path.GetFileName(actualFilePath);
                var contentType = MimeTypeHelper.GetMimeTypeFromFileName(fileName);

                _logger.LogInformation($"Successfully retrieved file from cloud storage: {actualFilePath}");
                return (stream, fileName, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting file from cloud storage. Original path: {filePath}, Extracted path: {ExtractFilePathFromUrl(filePath)}");
                
                // Provide more helpful error message
                if (ex.Message.Contains("Could not find", StringComparison.OrdinalIgnoreCase))
                {
                    var extractedPath = ExtractFilePathFromUrl(filePath);
                    throw new FileNotFoundException(
                        $"文件在云存储中不存在。路径: {extractedPath}。可能的原因：1) 文件未成功上传；2) 文件路径不匹配；3) 文件已被删除。请检查 OSS Bucket 中是否存在该文件。", 
                        ex);
                }
                
                throw;
            }
        }

        /// <summary>
        /// Extract file path from OSS URL or return original path
        /// </summary>
        private string ExtractFilePathFromUrl(string filePathOrUrl)
        {
            // If it's already a relative path, return as is
            if (!filePathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !filePathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return filePathOrUrl;
            }

            try
            {
                // Parse URL
                var uri = new Uri(filePathOrUrl);
                
                // Extract path from URL (remove leading slash and query parameters)
                var path = uri.AbsolutePath.TrimStart('/');
                
                // Remove query string if present
                if (path.Contains('?'))
                {
                    path = path.Substring(0, path.IndexOf('?'));
                }
                
                // Remove ProfileName prefix if present (e.g., "crm/1000/..." -> "1000/...")
                // This happens when GetAccessUrl adds ProfileName to the path
                var profileName = _blobStoreOptions?.Value?.ProfileName;
                if (!string.IsNullOrEmpty(profileName) && path.StartsWith(profileName + "/", StringComparison.OrdinalIgnoreCase))
                {
                    path = path.Substring(profileName.Length + 1);
                    _logger.LogDebug($"Removed ProfileName prefix '{profileName}' from path");
                }
                
                _logger.LogDebug($"Extracted file path '{path}' from URL '{filePathOrUrl}'");
                return path;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to parse URL '{filePathOrUrl}', using as-is");
                // If URL parsing fails, try to extract path manually
                // Remove protocol and domain, keep only path
                var path = filePathOrUrl;
                if (path.Contains("://"))
                {
                    var parts = path.Split(new[] { "://" }, StringSplitOptions.None);
                    if (parts.Length > 1)
                    {
                        var afterProtocol = parts[1];
                        var slashIndex = afterProtocol.IndexOf('/');
                        if (slashIndex >= 0)
                        {
                            path = afterProtocol.Substring(slashIndex + 1);
                        }
                    }
                }
                
                // Remove query parameters
                if (path.Contains('?'))
                {
                    path = path.Substring(0, path.IndexOf('?'));
                }
                
                // Remove ProfileName prefix if present
                var profileName = _blobStoreOptions?.Value?.ProfileName;
                if (!string.IsNullOrEmpty(profileName) && path.StartsWith(profileName + "/", StringComparison.OrdinalIgnoreCase))
                {
                    path = path.Substring(profileName.Length + 1);
                }
                
                return path;
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                // Note: IBlobContainer may not have DeleteAsync method
                // If needed, you may need to use the underlying SDK directly
                // For now, we'll check if file exists and log deletion attempt
                var exists = await FileExistsAsync(filePath);
                if (exists)
                {
                    _logger.LogWarning($"DeleteFileAsync called for {filePath}, but IBlobContainer may not support deletion directly");
                    // TODO: Implement deletion using underlying SDK if needed
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file from cloud storage: {filePath}");
                return false;
            }
        }

        public async Task<bool> FileExistsAsync(string filePath)
        {
            try
            {
                // Extract file path from URL if needed
                string actualFilePath = ExtractFilePathFromUrl(filePath);
                
                // Try to get the file, if it throws FileNotFoundException, it doesn't exist
                try
                {
                    using var stream = await _blobContainer.GetAsync(actualFilePath);
                    return stream != null;
                }
                catch (FileNotFoundException)
                {
                    return false;
                }
                catch
                {
                    // Other exceptions might indicate the file doesn't exist
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public string GetFileUrl(string filePath)
        {
            try
            {
                // Note: IBlobContainer.GetAccessUrl is async, but this interface method is synchronous
                // In a real implementation, you might want to cache URLs or use a different approach
                // For now, we return the file path and let the caller handle async URL retrieval if needed
                // The actual URL will be returned in SaveFileAsync result's AccessUrl property
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting file URL: {filePath}");
                return filePath;
            }
        }

        /// <summary>
        /// Get file access URL asynchronously (helper method for cloud storage)
        /// Generates a real-time signed URL for the file
        /// </summary>
        public async Task<string> GetFileUrlAsync(string filePath)
        {
            // Handle null or empty file path
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogWarning("GetFileUrlAsync called with null or empty file path");
                return null;
            }

            try
            {
                // Extract actual file path from URL if it's a full OSS URL
                string actualFilePath = ExtractFilePathFromUrl(filePath);
                
                _logger.LogDebug("Generating signed URL for file path: {OriginalPath} -> {ActualPath}", 
                    filePath, actualFilePath);
                
                var signedUrl = await _blobContainer.GetAccessUrl(actualFilePath);
                
                if (string.IsNullOrEmpty(signedUrl))
                {
                    _logger.LogWarning("BlobContainer.GetAccessUrl returned null or empty for path: {FilePath}", actualFilePath);
                    return null;
                }
                
                // Ensure HTTPS protocol for security
                if (signedUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    signedUrl = "https://" + signedUrl.Substring(7);
                    _logger.LogDebug("Converted HTTP to HTTPS for signed URL");
                }
                
                _logger.LogDebug("Successfully generated signed URL for path: {FilePath}", actualFilePath);
                return signedUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file URL: {FilePath}", filePath);
                return null;
            }
        }

        public async Task<FileValidationResult> ValidateFileAsync(IFormFile file)
        {
            // Check file size
            if (file != null && file.Length > _options.MaxFileSize)
            {
                return new FileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"File size cannot exceed {_options.MaxFileSize / 1024 / 1024} MB"
                };
            }

            // Check file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = _options.AllowedExtensions.Split(',').Select(x => x.Trim().ToLowerInvariant()).ToArray();

            if (!allowedExtensions.Contains(extension))
            {
                return new FileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Unsupported file type: {extension}. Supported types: {_options.AllowedExtensions}"
                };
            }

            // Check file content (simple MIME type validation)
            if (!MimeTypeHelper.IsValidMimeType(file.ContentType, extension))
            {
                return new FileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "File content does not match extension"
                };
            }

            return new FileValidationResult { IsValid = true };
        }

        public Task<int> CleanupTempFilesAsync()
        {
            // Cloud storage cleanup is typically handled by lifecycle policies
            // This method is kept for interface compatibility
            _logger.LogInformation("Cloud storage cleanup is handled by lifecycle policies");
            return Task.FromResult(0);
        }

        #region Private Methods

        private string GenerateFileName(string originalFileName)
        {
            if (!_options.EnableFileNameEncryption)
            {
                return $"{DateTimeOffset.UtcNow.Ticks}_{originalFileName}";
            }

            var extension = Path.GetExtension(originalFileName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            var timestamp = DateTimeOffset.UtcNow.Ticks;
            var hash = ComputeStringHash(nameWithoutExtension + timestamp);

            return $"{timestamp}_{hash}{extension}";
        }

        private string GenerateFilePath(string fileName, string category, string tenantId)
        {
            var pathParts = new List<string> { tenantId, category };

            if (_options.GroupByDate)
            {
                var now = DateTime.UtcNow;
                pathParts.Add(now.Year.ToString());
                pathParts.Add(now.Month.ToString("D2"));
                pathParts.Add(now.Day.ToString("D2"));
            }

            pathParts.Add(fileName);
            // Use forward slash for cloud storage paths
            return string.Join("/", pathParts);
        }

        private string ComputeStringHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes)[..8].ToLowerInvariant();
        }

        #endregion
    }
}

