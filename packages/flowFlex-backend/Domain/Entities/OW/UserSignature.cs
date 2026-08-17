using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// User Signature Entity - Stores electronic signature images for users.
    /// Inherits EntityBaseCreateInfo (not OwEntityBase) so it does NOT have app_code or tenant_id fields.
    /// Signatures are user-scoped and cross-tenant accessible.
    /// </summary>
    [SugarTable("ff_user_signature")]
    public class UserSignature : EntityBaseCreateInfo
    {
        /// <summary>
        /// Override inherited TenantId — ff_user_signature has no tenant_id column.
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public override string TenantId { get; set; }

        /// <summary>
        /// Override inherited AppCode — ff_user_signature has no app_code column.
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public override string AppCode { get; set; }

        /// <summary>
        /// The user who owns this signature
        /// </summary>
        [SugarColumn(ColumnName = "user_id")]
        public long UserId { get; set; }

        /// <summary>
        /// Base64-encoded PNG image data of the signature
        /// </summary>
        [SugarColumn(ColumnName = "image_data", ColumnDataType = "TEXT")]
        public string ImageData { get; set; }
    }
}
