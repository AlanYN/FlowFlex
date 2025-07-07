using System.ComponentModel;

namespace FlowFlex.Domain.Shared.Enums
{
    /// <summary>
    /// Tenant display type enumeration
    /// </summary>
    public enum TenantDisplayType
    {
        /// <summary>
        /// UF display type (LT)
        /// </summary>
        [Description("UF Display Type")]
        LT,

        /// <summary>
        /// UT display type (SBFH)
        /// </summary>
        [Description("UT Display Type")]
        SBFH,

        /// <summary>
        /// CW display type
        /// </summary>
        [Description("CW Display Type")]
        CW,

        /// <summary>
        /// Other display type
        /// </summary>
        [Description("Other Display Type")]
        OTHER
    }

    /// <summary>
    /// Tenant display type extension methods
    /// </summary>
    public static class TenantDisplayTypeExtensions
    {
        /// <summary>
        /// Gets the code for the tenant display type
        /// </summary>
        /// <param name="type">Tenant display type</param>
        /// <returns>Tenant display type code</returns>
        public static string GetCode(this TenantDisplayType type)
        {
            return type switch
            {
                TenantDisplayType.LT => "LT",
                TenantDisplayType.SBFH => "SBFH",
                TenantDisplayType.CW => "CW",
                TenantDisplayType.OTHER => "OTHER",
                _ => "SBFH", // Default to UT (SBFH)
            };
        }

        /// <summary>
        /// Gets the tenant display type from a code
        /// </summary>
        /// <param name="code">Tenant display type code</param>
        /// <returns>Tenant display type</returns>
        public static TenantDisplayType FromCode(string code)
        {
            return code?.ToUpper() switch
            {
                "LT" => TenantDisplayType.LT,
                "SBFH" => TenantDisplayType.SBFH,
                "CW" => TenantDisplayType.CW,
                "OTHER" => TenantDisplayType.OTHER,
                _ => TenantDisplayType.SBFH, // Default to UT
            };
        }
    }
}
