using Microsoft.AspNetCore.Http;
using FlowFlex.Domain.Shared;

namespace FlowFlex.Application.Contracts
{
    /// <summary>
    /// File storage service interface
    /// </summary>
    public interface IFileStorageService : IScopedService
    {
        /// <summary>
        /// Save file
        /// </summary>
        /// <param name="file">Uploaded file</param>
        /// <param name="category">File category</param>
        /// <param name="tenantId">Tenant ID</param>
        /// <returns>File information</returns>
        Task<FileStorageResult> SaveFileAsync(IFormFile file, string category = "DEFAULT", string tenantId = "DEFAULT");

        /// <summary>
        /// Get file
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>File stream and information</returns>
        Task<(Stream stream, string fileName, string contentType)> GetFileAsync(string filePath);

        /// <summary>
        /// Delete file
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>Whether deletion was successful</returns>
        Task<bool> DeleteFileAsync(string filePath);

        /// <summary>
        /// Check if file exists
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>Whether exists</returns>
        Task<bool> FileExistsAsync(string filePath);

        /// <summary>
        /// Get file access URL (synchronous)
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>Access URL</returns>
        string GetFileUrl(string filePath);

        /// <summary>
        /// Get file access URL asynchronously
        /// For cloud storage, this generates a real-time signed URL that won't expire immediately
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>Access URL (signed URL for cloud storage)</returns>
        Task<string> GetFileUrlAsync(string filePath);

        /// <summary>
        /// Validate file
        /// </summary>
        /// <param name="file">Uploaded file</param>
        /// <returns>Validation result</returns>
        Task<FileValidationResult> ValidateFileAsync(IFormFile file);

        /// <summary>
        /// Clean up temporary files
        /// </summary>
        /// <returns>Number of cleaned files</returns>
        Task<int> CleanupTempFilesAsync();
    }

    /// <summary>
    /// File storage result
    /// </summary>
    public class FileStorageResult
    {
        /// <summary>
        /// Whether successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// File path
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// File name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Original file name
        /// </summary>
        public string OriginalFileName { get; set; }

        /// <summary>
        /// File size
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Content type
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Access URL
        /// </summary>
        public string AccessUrl { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// File hash value
        /// </summary>
        public string FileHash { get; set; }
    }

    /// <summary>
    /// File validation result
    /// </summary>
    public class FileValidationResult
    {
        /// <summary>
        /// Whether valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
