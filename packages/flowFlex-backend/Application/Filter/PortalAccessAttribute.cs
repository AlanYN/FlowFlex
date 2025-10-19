using Microsoft.AspNetCore.Mvc;

namespace FlowFlex.Application.Filter
{
    /// <summary>
    /// Attribute to mark controllers/actions that allow Portal token access
    /// Controllers/actions marked with this attribute can be accessed by Portal users with limited scope
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class PortalAccessAttribute : Attribute
    {
        /// <summary>
        /// Whether this endpoint requires Portal token
        /// If true, only Portal tokens are allowed
        /// If false, both Portal and regular tokens are allowed
        /// </summary>
        public bool PortalOnly { get; set; } = false;

        public PortalAccessAttribute()
        {
        }

        public PortalAccessAttribute(bool portalOnly)
        {
            PortalOnly = portalOnly;
        }
    }
}

