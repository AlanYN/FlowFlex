using System;

namespace FlowFlex.Domain.Shared.Attr
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ChangeTrackerAttribute : Attribute
    {
        /// <summary>
        /// Menu codes that use this entity
        /// </summary>
        public string[] MenuCodes { get; } = [];
        public ChangeTrackerAttribute(params string[] menuCodes)
        {
            MenuCodes = menuCodes;
        }
        public ChangeTrackerAttribute()
        {
        }
    }
}
