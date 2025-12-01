namespace Application.Contracts.Options
{
    /// <summary>
    /// Blob storage configuration options for OSS/AWS
    /// </summary>
    public class BlobStoreOptions
    {
        /// <summary>
        /// Access Key ID for cloud storage
        /// </summary>
        public string AccessKeyId { get; set; } = string.Empty;

        /// <summary>
        /// Secret Access Key for cloud storage
        /// </summary>
        public string SecretAccessKey { get; set; } = string.Empty;

        /// <summary>
        /// Region for AWS S3 (required for AWS, optional for OSS)
        /// </summary>
        public string Region { get; set; } = string.Empty;

        /// <summary>
        /// Profile name for AWS (required for AWS)
        /// </summary>
        public string ProfileName { get; set; } = "default";

        /// <summary>
        /// Bucket name for cloud storage
        /// </summary>
        public string Bucket { get; set; } = string.Empty;

        /// <summary>
        /// Endpoint for OSS (required for OSS, optional for AWS)
        /// </summary>
        public string EndPoint { get; set; } = string.Empty;
    }
}

