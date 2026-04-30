using FlowFlex.Domain.Entities.Base;
using FlowFlex.Domain.Entities;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared;
using FlowFlex.Domain.Shared.Helpers;
using FlowFlex.Domain.Shared.Models;
using SqlSugar.DistributedSystem.Snowflake;
using SqlSugar;


namespace FlowFlex.Application.Services.OW.Extensions
{
    /// <summary>
    /// EntityBaseCreateInfo extension methods for initialization
    /// </summary>
    public static class EntityBaseCreateInfoExtension
    {
        /// <summary>
        /// Initialize create information for new entity
        /// </summary>
        /// <param name="createInfo">Entity to initialize</param>
        /// <param name="userContext">User context</param>
        public static void InitCreateInfo(this EntityBaseCreateInfo createInfo, UserContext userContext)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Generate snowflake ID using entity's InitNewId method
            if (createInfo is IdEntityBase entityBase)
            {
                entityBase.InitNewId();
            }
            else
            {
                // Fallback: generate a simple timestamp-based ID
                createInfo.Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            // Set timestamps (UTC)
            createInfo.CreateDate = now;
            createInfo.ModifyDate = now;

            // Set user information (Priority: FirstName + LastName > UserName)
            var displayName = GetDisplayName(userContext);
            createInfo.CreateBy = displayName;
            createInfo.ModifyBy = displayName;
            createInfo.CreateUserId = ParseToLong(userContext?.UserId);
            createInfo.ModifyUserId = ParseToLong(userContext?.UserId);

            // Set default values
            createInfo.IsValid = true;
            createInfo.TenantId = TenantContextHelper.GetTenantIdOrDefault(userContext);

            // Set app code if entity supports it
            if (createInfo is AbstractEntityBase abstractEntity)
            {
                abstractEntity.AppCode = TenantContextHelper.GetAppCodeOrDefault(userContext);
            }
        }

        /// <summary>
        /// Get display name from UserContext (Priority: FirstName + LastName > UserName)
        /// </summary>
        private static string GetDisplayName(UserContext userContext)
        {
            if (userContext == null) return "SYSTEM";

            // Priority 1: FirstName + LastName
            var firstName = userContext.FirstName?.Trim();
            var lastName = userContext.LastName?.Trim();
            var fullName = $"{firstName} {lastName}".Trim();
            if (!string.IsNullOrEmpty(fullName))
            {
                return fullName;
            }

            // Priority 2: UserName
            if (!string.IsNullOrEmpty(userContext.UserName))
            {
                return userContext.UserName;
            }

            // Priority 3: Email
            if (!string.IsNullOrEmpty(userContext.Email))
            {
                return userContext.Email;
            }

            return "SYSTEM";
        }

        /// <summary>
        /// Initialize create information from event data (for background tasks without HttpContext)
        /// </summary>
        /// <param name="createInfo">Entity to initialize</param>
        /// <param name="userId">User ID from event</param>
        /// <param name="userName">User name from event</param>
        public static void InitCreateInfoFromEvent(this EntityBaseCreateInfo createInfo, long userId, string userName)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Generate snowflake ID using entity's InitNewId method
            if (createInfo is IdEntityBase entityBase)
            {
                entityBase.InitNewId();
            }
            else
            {
                // Fallback: generate a simple timestamp-based ID
                createInfo.Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            // Set timestamps (UTC)
            createInfo.CreateDate = now;
            createInfo.ModifyDate = now;

            // Set user information from event data
            var displayName = !string.IsNullOrEmpty(userName) ? userName : "System";
            createInfo.CreateBy = displayName;
            createInfo.ModifyBy = displayName;
            createInfo.CreateUserId = userId;
            createInfo.ModifyUserId = userId;

            // Set default values
            createInfo.IsValid = true;
            // Note: TenantId should be set separately from event data
        }

        /// <summary>
        /// Initialize create information for new entity with specific parameters
        /// </summary>
        /// <param name="createInfo">Entity to initialize</param>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="createUserId">Create user ID</param>
        /// <param name="createUserName">Create user name</param>
        /// <param name="modifyUserId">Modify user ID</param>
        /// <param name="modifyUserName">Modify user name</param>
        /// <param name="createDate">Create date</param>
        /// <param name="modifyDate">Modify date</param>
        public static void InitCreateInfo(this EntityBaseCreateInfo createInfo, string tenantId, long createUserId, string createUserName, long modifyUserId, string modifyUserName, DateTimeOffset createDate, DateTimeOffset modifyDate)
        {
            // Generate snowflake ID using entity's InitNewId method if available
            if (createInfo is IdEntityBase entityBase)
            {
                entityBase.InitNewId();
            }
            else
            {
                // Fallback: generate a simple timestamp-based ID
                createInfo.Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            createInfo.TenantId = tenantId;
            createInfo.CreateBy = createUserName;
            createInfo.CreateDate = createDate;
            createInfo.ModifyBy = modifyUserName;
            createInfo.ModifyDate = modifyDate;
            createInfo.CreateUserId = createUserId;
            createInfo.ModifyUserId = modifyUserId;
            createInfo.IsValid = true;
        }

        /// <summary>
        /// Initialize update information for existing entity
        /// </summary>
        /// <param name="createInfo">Entity to update</param>
        /// <param name="userContext">User context</param>
        public static void InitUpdateInfo(this EntityBaseCreateInfo createInfo, UserContext userContext)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Update timestamps
            createInfo.ModifyDate = now;

            // Update user information (Priority: FirstName + LastName > UserName)
            createInfo.ModifyBy = GetDisplayName(userContext);
            createInfo.ModifyUserId = ParseToLong(userContext?.UserId);
        }

        /// <summary>
        /// Initialize modify information for existing entity (alias for InitUpdateInfo)
        /// </summary>
        /// <param name="createInfo">Entity to update</param>
        /// <param name="userContext">User context</param>
        public static void InitModifyInfo(this EntityBaseCreateInfo createInfo, UserContext userContext)
        {
            InitUpdateInfo(createInfo, userContext);
        }

        /// <summary>
        /// Update information for existing entity with specific parameters
        /// </summary>
        /// <param name="createInfo">Entity to update</param>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="userId">User ID</param>
        /// <param name="userName">User name</param>
        public static void UpdateInfo(this EntityBaseCreateInfo createInfo, string tenantId, long userId, string userName)
        {
            createInfo.ModifyBy = userName;
            createInfo.ModifyDate = DateTimeOffset.UtcNow;
            createInfo.ModifyUserId = userId;
        }

        /// <summary>
        /// Parse string to long, return 0 if null or invalid
        /// </summary>
        /// <param name="value">String value to parse</param>
        /// <returns>Long value or 0</returns>
        private static long ParseToLong(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            if (long.TryParse(value, out long result))
                return result;

            return 0;
        }

        /// <summary>
        /// Initialize create information for OwEntityBase
        /// </summary>
        /// <param name="entity">Entity to initialize</param>
        /// <param name="userContext">User context</param>
        public static void InitCreateInfo(this OwEntityBase entity, UserContext userContext)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Generate snowflake ID for OwEntityBase
            entity.InitNewId();

            // Set timestamps
            entity.CreateDate = now;
            entity.ModifyDate = now;

            // Set user information (Priority: FirstName + LastName > UserName)
            var displayName = GetDisplayName(userContext);
            entity.CreateBy = displayName;
            entity.ModifyBy = displayName;
            entity.CreateUserId = ParseToLong(userContext?.UserId);
            entity.ModifyUserId = ParseToLong(userContext?.UserId);

            // Set default values
            entity.IsValid = true;
            entity.TenantId = TenantContextHelper.GetTenantIdOrDefault(userContext);
            entity.AppCode = TenantContextHelper.GetAppCodeOrDefault(userContext);
        }

        /// <summary>
        /// Initialize update information for OwEntityBase
        /// </summary>
        /// <param name="entity">Entity to update</param>
        /// <param name="userContext">User context</param>
        public static void InitUpdateInfo(this OwEntityBase entity, UserContext userContext)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Update timestamps
            entity.ModifyDate = now;

            // Update user information (Priority: FirstName + LastName > UserName)
            entity.ModifyBy = GetDisplayName(userContext);
            entity.ModifyUserId = ParseToLong(userContext?.UserId);
        }

        /// <summary>
        /// Initialize create information for User entity
        /// </summary>
        /// <param name="user">User entity to initialize</param>
        /// <param name="userContext">User context</param>
        public static void InitCreateInfo(this User user, UserContext userContext)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Generate timestamp-based ID
            user.Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Set timestamps
            user.CreateDate = now;
            user.ModifyDate = now;

            // Set user information (Priority: FirstName + LastName > UserName)
            var displayName = GetDisplayName(userContext);
            user.CreateBy = displayName;
            user.ModifyBy = displayName;
            user.CreateUserId = ParseToLong(userContext?.UserId);
            user.ModifyUserId = ParseToLong(userContext?.UserId);

            // Set default values
            user.IsValid = true;
            user.TenantId = TenantContextHelper.GetTenantIdOrDefault(userContext);
            user.AppCode = TenantContextHelper.GetAppCodeOrDefault(userContext);
        }

        /// <summary>
        /// Initialize update information for User entity
        /// </summary>
        /// <param name="user">User entity to update</param>
        /// <param name="userContext">User context</param>
        public static void InitUpdateInfo(this User user, UserContext userContext)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Update timestamps
            user.ModifyDate = now;

            // Update user information (Priority: FirstName + LastName > UserName)
            user.ModifyBy = GetDisplayName(userContext);
            user.ModifyUserId = ParseToLong(userContext?.UserId);
        }

        /// <summary>
        /// Initialize create information for InternalNote entity
        /// </summary>
        /// <param name="note">InternalNote entity to initialize</param>
        /// <param name="userContext">User context</param>
        public static void InitCreateInfo(this InternalNote note, UserContext userContext)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Generate timestamp-based ID
            note.Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Set timestamps
            note.CreateDate = now;
            note.ModifyDate = now;

            // Set user information (Priority: FirstName + LastName > UserName)
            var displayName = GetDisplayName(userContext);
            note.CreateBy = displayName;
            note.ModifyBy = displayName;
            note.CreateUserId = ParseToLong(userContext?.UserId);
            note.ModifyUserId = ParseToLong(userContext?.UserId);

            // Set default values
            note.IsValid = true;
            note.TenantId = TenantContextHelper.GetTenantIdOrDefault(userContext);
            note.AppCode = TenantContextHelper.GetAppCodeOrDefault(userContext);
        }

        /// <summary>
        /// Initialize create information for UserInvitation entity
        /// </summary>
        /// <param name="invitation">UserInvitation entity to initialize</param>
        /// <param name="userContext">User context</param>
        public static void InitCreateInfo(this UserInvitation invitation, UserContext userContext)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Generate timestamp-based ID
            invitation.Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Set timestamps
            invitation.CreateDate = now;
            invitation.ModifyDate = now;

            // Set user information (Priority: FirstName + LastName > UserName)
            var displayName = GetDisplayName(userContext);
            invitation.CreateBy = displayName;
            invitation.ModifyBy = displayName;
            invitation.CreateUserId = ParseToLong(userContext?.UserId);
            invitation.ModifyUserId = ParseToLong(userContext?.UserId);

            // Set default values
            invitation.IsValid = true;
            invitation.TenantId = TenantContextHelper.GetTenantIdOrDefault(userContext);
            invitation.AppCode = TenantContextHelper.GetAppCodeOrDefault(userContext);
        }

        /// <summary>
        /// Initialize update information for InternalNote entity
        /// </summary>
        /// <param name="note">InternalNote entity to update</param>
        /// <param name="userContext">User context</param>
        public static void InitUpdateInfo(this InternalNote note, UserContext userContext)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Update timestamps
            note.ModifyDate = now;

            // Update user information (Priority: FirstName + LastName > UserName)
            note.ModifyBy = GetDisplayName(userContext);
            note.ModifyUserId = ParseToLong(userContext?.UserId);
        }

        /// <summary>
        /// Initialize create information for OperationChangeLog entity
        /// </summary>
        /// <param name="log">OperationChangeLog entity to initialize</param>
        /// <param name="userContext">User context</param>
        public static void InitCreateInfo(this Domain.Entities.OW.OperationChangeLog log, UserContext userContext)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Generate timestamp-based ID
            log.Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Set timestamps
            log.CreateDate = now;
            log.ModifyDate = now;

            // Set user information (Priority: FirstName + LastName > UserName)
            var displayName = GetDisplayName(userContext);
            log.CreateBy = displayName;
            log.ModifyBy = displayName;
            log.CreateUserId = ParseToLong(userContext?.UserId);
            log.ModifyUserId = ParseToLong(userContext?.UserId);

            // Set default values
            log.IsValid = true;
            log.TenantId = TenantContextHelper.GetTenantIdOrDefault(userContext);
        }

        /// <summary>
        /// Initialize update information for OperationChangeLog entity
        /// </summary>
        /// <param name="log">OperationChangeLog entity to update</param>
        /// <param name="userContext">User context</param>
        public static void InitUpdateInfo(this Domain.Entities.OW.OperationChangeLog log, UserContext userContext)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Update timestamps
            log.ModifyDate = now;

            // Update user information (Priority: FirstName + LastName > UserName)
            log.ModifyBy = GetDisplayName(userContext);
            log.ModifyUserId = ParseToLong(userContext?.UserId);
        }

        /// <summary>
        /// Initialize create information for AIModelConfig
        /// </summary>
        /// <param name="config">Config to initialize</param>
        /// <param name="userContext">User context</param>
        public static void InitCreateInfo(this AIModelConfig config, UserContext userContext)
        {
            // Set tenant and app information from UserContext
            config.TenantId = TenantContextHelper.GetTenantIdOrDefault(userContext);
            config.AppCode = TenantContextHelper.GetAppCodeOrDefault(userContext);

            // Set IsValid to true for new records
            config.IsValid = true;
        }

        /// <summary>
        /// Initialize update information for AIModelConfig  
        /// </summary>
        /// <param name="config">Config to update</param>
        /// <param name="userContext">User context</param>
        public static void InitUpdateInfo(this AIModelConfig config, UserContext userContext)
        {
            // Update tenant and app information if needed
            // Usually these don't change, but we can update them if the context has changed
            if (!string.IsNullOrEmpty(userContext?.TenantId))
            {
                config.TenantId = userContext.TenantId;
            }
            if (!string.IsNullOrEmpty(userContext?.AppCode))
            {
                config.AppCode = userContext.AppCode;
            }
        }
    }
}