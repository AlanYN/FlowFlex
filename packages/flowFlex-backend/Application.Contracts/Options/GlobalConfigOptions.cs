using FlowFlex.Application.Contracts.Dtos;

namespace Application.Contracts.Options
{
    /// <summary>
    /// Global configuration options
    /// </summary>
    public class GlobalConfigOptions
    {
        /// <summary>
        /// Enable Item IAM authentication
        /// </summary>
        public bool EnableItemIam { get; set; } = false;

        /// <summary>
        /// Blob storage type: Local, OSS, AWS, BNP_FTP
        /// </summary>
        public AttachmentStoreType BlobStoreType { get; set; } = AttachmentStoreType.Local;

        /// <summary>
        /// Portal base URL for generating invitation links
        /// </summary>
        public string PortalBaseUrl { get; set; } = "https://workflow.item.com";
    }
}
