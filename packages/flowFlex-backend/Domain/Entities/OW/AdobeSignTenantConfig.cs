using FlowFlex.Domain.Entities.Base;
using SqlSugar;

namespace FlowFlex.Domain.Entities.OW
{
    /// <summary>
    /// Adobe Sign tenant account config — maps an AppCode to a named Adobe Sign account key.
    /// This is a system-level config table (no multi-tenancy filtering).
    /// When no record exists for a given AppCode, the service falls back to
    /// the DefaultAccount value in appsettings.json.
    /// </summary>
    [SugarTable("ff_adobe_sign_tenant_config")]
    public class AdobeSignTenantConfig : EntityBaseCreateInfo
    {
        /// <summary>
        /// Not used — ff_adobe_sign_tenant_config has no tenant_id column.
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public override string TenantId { get; set; }

        /// <summary>
        /// Not used — ff_adobe_sign_tenant_config has no app_code column.
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public override string AppCode { get; set; }

        /// <summary>
        /// The AppCode being configured (e.g. "item-wfe", "unisco-wfe").
        /// Unique constraint: one record per AppCode.
        /// </summary>
        [SugarColumn(ColumnName = "configured_app_code", Length = 100)]
        public string ConfiguredAppCode { get; set; }

        /// <summary>
        /// The Adobe Sign account key to use for this AppCode (e.g. "Item", "Unisco").
        /// Must match a key in appsettings.json AdobeSign:Accounts.
        /// </summary>
        [SugarColumn(ColumnName = "account_key", Length = 50)]
        public string AccountKey { get; set; }
    }
}
