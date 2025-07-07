using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums
{
    /// <summary>
    /// Attachment type
    /// </summary>
    public enum AttachmentTypeEnum
    {
        [Description("BasicLicenseImage")]
        BasicLicenseImage = 1,

        [Description("DocumentFile")]
        DocumentFile = 2,

        [Description("DocumentFileFromDAT")]
        DocumentFileFromDAT = 3,

        [Description("ProductImage")]
        ProductImage = 4,

        [Description("ContractFile")]
        ContractFile = 5
    }
}
