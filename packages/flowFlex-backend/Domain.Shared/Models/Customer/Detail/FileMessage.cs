using System;

namespace FlowFlex.Domain.Shared.Models.Customer.Detail
{
    public class FileMessage : DataMessageBase
    {
        /// <summary>
        /// Attachment ID
        /// </summary>
        public long AttachmentId { get; set; }

        /// <summary>
        /// Business type (AttachmentTypeEnum)
        /// </summary>
        public int BusinessType { get; set; }

        /// <summary>
        /// Business type name in English
        /// </summary>
        public string BusinessTypeName { get; set; }

        /// <summary>
        /// File upload type
        /// </summary>
        public string FileUploadType { get; set; }

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
        /// Absolute file storage path, e.g., /uploads/20220202
        /// </summary>
        public string FileAbsolutePath { get; set; }

        /// <summary>
        /// Repository location, e.g., /uploads
        /// </summary>
        public string FolderPath { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// File extension
        /// </summary>
        public string FileExt { get; set; }

        /// <summary>
        /// Storage type
        /// </summary>
        public int? StoreType { get; set; }

        /// <summary>
        /// Access URL for the file
        /// </summary>
        public string AccessUrl { get; set; }

        /// <summary>
        /// Full URL for direct file access
        /// </summary>
        public string FullUrl { get; set; }
    }
}
