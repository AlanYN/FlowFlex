using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using Item.Common.Lib.JsonConverts;

namespace FlowFlex.Application.Contracts.Dtos
{
    public class AttachmentDto
    {
        public string Id { get; set; }
        public AttachmentStoreType StoreType { get; set; }

        /// <summary>
        /// File content
        /// </summary>
        public IFormFile FileData { get; set; }

        /// <summary>
        /// Original file name
        /// </summary>
        public string RealName { get; set; }

        /// <summary>
        /// File type
        /// </summary>
        public string FileType { get; set; }

        /// <summary>
        /// Stored file name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// File storage path, e.g.: /uploads/20220202
        /// </summary>
        public string FileUrl { get; set; }

        /// <summary>
        /// Repository location, e.g.: /uploads
        /// </summary>
        public string StorePath { get; set; }

        /// <summary>
        /// File size
        /// </summary>
        public string FileSize { get; set; }

        /// <summary>
        /// File extension
        /// </summary>
        public string FileExt { get; set; }

        /// <summary>
        /// Access URL
        /// </summary>
        public string AccessUrl { get; set; }

        /// <summary>
        /// Creator
        /// </summary>
        public string CreateBy { get; set; }

        /// <summary>
        /// Upload time
        /// </summary>
        public DateTime? CreateDate { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public AttachmentDto()
        { }

        /// <summary>
        /// Parameterized constructor for creating an AttachmentDto
        /// </summary>
        /// <param name="originFileName">Original file name</param>
        /// <param name="fileName">Stored file name</param>
        /// <param name="ext">File extension</param>
        /// <param name="fileSize">File size</param>
        /// <param name="storePath">Storage path</param>
        /// <param name="create_by">Creator</param>
        public AttachmentDto(string originFileName, string fileName, string ext, string fileSize, string storePath, string create_by)
        {
            StorePath = storePath;
            RealName = originFileName;
            FileName = fileName;
            FileExt = ext;
            FileSize = fileSize;
            CreateBy = create_by;
            CreateDate = DateTime.Now;
        }
    }

    /// <summary>
    /// Output DTO for attachment information
    /// </summary>
    public class AttachmentOutputDto
    {
        [JsonConverter(typeof(ValueToStringConverter))]
        public long Id { get; set; }

        /// <summary>
        /// Stored file name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Original file name
        /// </summary>
        public string RealName { get; set; }

        /// <summary>
        /// Operation key for the attachment
        /// </summary>
        public string OperateKey { get; set; }

        /// <summary>
        /// Access URL for the attachment
        /// </summary>
        public string AccessUrl { get; set; }

        [JsonConverter(typeof(ValueToStringConverter))]
        public long AttachmentId { get; set; }

        /// <summary>
        /// Storage type of the attachment
        /// </summary>
        public AttachmentStoreType StoreType { get; set; }

        /// <summary>
        /// File type
        /// </summary>
        public string FileType { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// File storage path
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// File hash for integrity verification
        /// </summary>
        public string FileHash { get; set; }

        /// <summary>
        /// Creation date of the attachment
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }
        public string TenantId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Enum representing different types of attachment storage
    /// </summary>
    public enum AttachmentStoreType
    {
        Local = 0,
        OSS = 1,
        AWS = 2,
        BNP_FTP = 3
    }
}
