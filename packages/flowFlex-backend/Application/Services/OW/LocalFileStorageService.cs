using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FlowFlex.Application.Contracts;
using FlowFlex.Application.Contracts.Options;
using FlowFlex.Domain.Shared;
using System.Security.Cryptography;
using System.Text;

namespace FlowFlex.Application.Services.OW
{
    /// <summary>
    /// Local file storage service implementation
    /// </summary>
    public class LocalFileStorageService : IFileStorageService, IScopedService
    {
        private readonly FileStorageOptions _options;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<LocalFileStorageService> _logger;

        public LocalFileStorageService(
            IOptions<FileStorageOptions> options,
            IWebHostEnvironment webHostEnvironment,
            ILogger<LocalFileStorageService> logger)
        {
            _options = options.Value;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public async Task<FileStorageResult> SaveFileAsync(IFormFile file, string category = "DEFAULT", string tenantId = "DEFAULT")
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
                var fullPath = Path.Combine(_webHostEnvironment.ContentRootPath, _options.LocalStoragePath, relativePath);

                // Ensure directory exists
                var directory = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Save file and calculate hash
                string fileHash;
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                    await stream.FlushAsync();
                }

                // Wait briefly to ensure file is fully written
                await Task.Delay(10);

                // Calculate file hash
                fileHash = await ComputeFileHashAsync(fullPath);

                var result = new FileStorageResult
                {
                    Success = true,
                    FilePath = relativePath,
                    FileName = fileName,
                    OriginalFileName = file.FileName,
                    FileSize = file.Length,
                    ContentType = file.ContentType,
                    AccessUrl = GetFileUrl(relativePath),
                    FileHash = fileHash
                };

                _logger.LogInformation($"File saved successfully: {relativePath}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving file: {file.FileName}");
                return new FileStorageResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to save file: {ex.Message}"
                };
            }
        }

        public async Task<(Stream stream, string fileName, string contentType)> GetFileAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_webHostEnvironment.ContentRootPath, _options.LocalStoragePath, filePath);

                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                var fileName = Path.GetFileName(fullPath);
                var contentType = GetContentType(fileName);

                return (stream, fileName, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting file: {filePath}");
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_webHostEnvironment.ContentRootPath, _options.LocalStoragePath, filePath);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation($"File deleted: {filePath}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file: {filePath}");
                return false;
            }
        }

        public async Task<bool> FileExistsAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_webHostEnvironment.ContentRootPath, _options.LocalStoragePath, filePath);
                return File.Exists(fullPath);
            }
            catch
            {
                return false;
            }
        }

        public string GetFileUrl(string filePath)
        {
            return $"{_options.FileUrlPrefix}/{filePath.Replace("\\", "/")}";
        }

        public async Task<FileValidationResult> ValidateFileAsync(IFormFile file)
        {
            // Check if file is empty
            if (file == null || file.Length == 0)
            {
                return new FileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "File cannot be empty"
                };
            }

            // Check file size
            if (file.Length > _options.MaxFileSize)
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
            if (!IsValidMimeType(file.ContentType, extension))
            {
                return new FileValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "File content does not match extension"
                };
            }

            return new FileValidationResult { IsValid = true };
        }

        public async Task<int> CleanupTempFilesAsync()
        {
            try
            {
                var tempPath = Path.Combine(_webHostEnvironment.ContentRootPath, _options.LocalStoragePath, "temp");
                if (!Directory.Exists(tempPath))
                {
                    return 0;
                }

                var cutoffTime = DateTime.Now.AddHours(-_options.TempFileRetentionHours);
                var files = Directory.GetFiles(tempPath, "*", SearchOption.AllDirectories);
                var deletedCount = 0;

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffTime)
                    {
                        try
                        {
                            File.Delete(file);
                            deletedCount++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Failed to delete temp file: {file}");
                        }
                    }
                }

                _logger.LogInformation($"Cleaned up {deletedCount} temporary files");
                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up temporary files");
                return 0;
            }
        }

        #region Private Methods

        private string GenerateFileName(string originalFileName)
        {
            if (!_options.EnableFileNameEncryption)
            {
                return $"{DateTimeOffset.Now.Ticks}_{originalFileName}";
            }

            var extension = Path.GetExtension(originalFileName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            var timestamp = DateTimeOffset.Now.Ticks;
            var hash = ComputeStringHash(nameWithoutExtension + timestamp);

            return $"{timestamp}_{hash}{extension}";
        }

        private string GenerateFilePath(string fileName, string category, string tenantId)
        {
            var pathParts = new List<string> { tenantId, category };

            if (_options.GroupByDate)
            {
                var now = DateTime.Now;
                pathParts.Add(now.Year.ToString());
                pathParts.Add(now.Month.ToString("D2"));
                pathParts.Add(now.Day.ToString("D2"));
            }

            pathParts.Add(fileName);
            return Path.Combine(pathParts.ToArray());
        }

        private string ComputeStringHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes)[..8].ToLowerInvariant();
        }

        private async Task<string> ComputeFileHashAsync(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var bytes = await sha256.ComputeHashAsync(stream);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

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
                _ => "application/octet-stream"
            };
        }

        private bool IsValidMimeType(string contentType, string extension)
        {
            var expectedContentType = GetContentType($"file{extension}");

            // Allow some common MIME type variants
            return contentType switch
            {
                var ct when ct == expectedContentType => true,
                "application/octet-stream" => true, // Generic binary type
                var ct when ct.StartsWith("image/") && extension.StartsWith(".") &&
                           new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(extension) => true,
                var ct when ct.StartsWith("video/") && extension.StartsWith(".") &&
                           new[] { ".mp4", ".avi", ".mov" }.Contains(extension) => true,
                _ => false
            };
        }

        #endregion
    }
}