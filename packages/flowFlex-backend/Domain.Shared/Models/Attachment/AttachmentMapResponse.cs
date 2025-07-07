using FlowFlex.Domain.Shared.Enums;

namespace FlowFlex.Domain.Shared.Models.Attachment;

public class AttachmentMapResponse
{
    public long Id { get; set; }

    public long BusinessId { get; set; }

    public long AttachmentId { get; set; }

    public AttachmentTypeEnum BusinessType { get; set; }

    public string FileUploadType { get; set; }

    public string RealName { get; set; }

    public string FileName { get; set; }
}
