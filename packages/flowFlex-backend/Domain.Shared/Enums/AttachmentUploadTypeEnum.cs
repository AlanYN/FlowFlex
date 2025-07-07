using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums
{
    public enum AttachmentUploadTypeEnum
    {
        [Description("CARRIER PACKET")]
        CARRIER_PACKET,
        CONF_RATE,
        CONTRACT,
        [Description("CREDIT APPROVAL")]
        CREDIT_APPROVAL,
        [Description("CREDIT SCORE")]
        CREDIT_SCORE,
        CREDIT_APP,
        CREDIT_CARD,
        [Description("CUSTOMER INFO")]
        CUSTOMER_INFO,
        [Description("CUSTOMER PROFILE")]
        CUSTOMER_PROFILE,
        FMCSA,
        INSURANCE,
        LOGO,
        OTHER,
        PICTURE,
        REFERENCES,
        [Description("W-9")]
        W_9,
        PDF,
        SIGNATURE_INITIAL,
        SIGNATURE_AO,
        SIGNATURE_CARD,
        GUARANTEE
    }
}
