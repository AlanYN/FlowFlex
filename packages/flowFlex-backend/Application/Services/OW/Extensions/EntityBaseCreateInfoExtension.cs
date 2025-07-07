using FlowFlex.Domain.Entities.Base;
using FlowFlex.Domain.Entities;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Shared;
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
            DateTimeOffset now = DateTimeOffset.Now;
            
            // Generate snowflake ID using entity's InitNewId method
            if (createInfo is IdEntityBase entityBase)
            {
                entityBase.InitNewId();
            }
            else
            {
                // Fallback: generate a simple timestamp-based ID
                createInfo.Id = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
            
            // Set timestamps
            createInfo.CreateDate = now;
            createInfo.ModifyDate = now;
            
            // Set user information
            createInfo.CreateBy = userContext?.UserName ?? "SYSTEM";
            createInfo.ModifyBy = userContext?.UserName ?? "SYSTEM";
            createInfo.CreateUserId = ParseToLong(userContext?.UserId);
            createInfo.ModifyUserId = ParseToLong(userContext?.UserId);
            
            // Set default values
            createInfo.IsValid = true;
            createInfo.TenantId = userContext?.TenantId ?? "DEFAULT";
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
                createInfo.Id = DateTimeOffset.Now.ToUnixTimeMilliseconds();
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
        /// Initialize create information for new entity with simple parameters
        /// </summary>
        /// <param name="createInfo">Entity to initialize</param>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="userId">User ID</param>
        /// <param name="userName">User name</param>
        public static void InitCreateInfo(this EntityBaseCreateInfo createInfo, string tenantId, long userId, string userName)
        {
            // Generate snowflake ID using entity's InitNewId method if available
            if (createInfo is IdEntityBase entityBase)
            {
                entityBase.InitNewId();
            }
            else
            {
                // Fallback: generate a simple timestamp-based ID
                createInfo.Id = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
            
            createInfo.TenantId = tenantId;
            createInfo.CreateBy = userName;
            createInfo.CreateDate = DateTimeOffset.Now;
            createInfo.ModifyBy = userName;
            createInfo.ModifyDate = DateTimeOffset.Now;
            createInfo.CreateUserId = userId;
            createInfo.ModifyUserId = userId;
            createInfo.IsValid = true;
        }

        /// <summary>
        /// Initialize update information for existing entity
        /// </summary>
        /// <param name="createInfo">Entity to update</param>
        /// <param name="userContext">User context</param>
        public static void InitUpdateInfo(this EntityBaseCreateInfo createInfo, UserContext userContext)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            
            // Update timestamps
            createInfo.ModifyDate = now;
            
            // Update user information
            createInfo.ModifyBy = userContext?.UserName ?? "SYSTEM";
            createInfo.ModifyUserId = ParseToLong(userContext?.UserId);
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
            createInfo.ModifyDate = DateTimeOffset.Now;
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
            DateTimeOffset now = DateTimeOffset.Now;
            
            // Generate timestamp-based ID for OwEntityBase
            entity.Id = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            
            // Set timestamps
            entity.CreateDate = now;
            entity.ModifyDate = now;
            
            // Set user information
            entity.CreateBy = userContext?.UserName ?? "SYSTEM";
            entity.ModifyBy = userContext?.UserName ?? "SYSTEM";
            entity.CreateUserId = ParseToLong(userContext?.UserId);
            entity.ModifyUserId = ParseToLong(userContext?.UserId);
            
            // Set default values
            entity.IsValid = true;
            entity.TenantId = userContext?.TenantId ?? "DEFAULT";
        }

        /// <summary>
        /// Initialize update information for OwEntityBase
        /// </summary>
        /// <param name="entity">Entity to update</param>
        /// <param name="userContext">User context</param>
        public static void InitUpdateInfo(this OwEntityBase entity, UserContext userContext)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            
            // Update timestamps
            entity.ModifyDate = now;
            
            // Update user information
            entity.ModifyBy = userContext?.UserName ?? "SYSTEM";
            entity.ModifyUserId = ParseToLong(userContext?.UserId);
        }

        /// <summary>
        /// Initialize create information for User entity
        /// </summary>
        /// <param name="user">User entity to initialize</param>
        /// <param name="userContext">User context</param>
        public static void InitCreateInfo(this User user, UserContext userContext)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            
            // Generate timestamp-based ID
            user.Id = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            
            // Set timestamps
            user.CreateDate = now;
            user.ModifyDate = now;
            
            // Set user information
            user.CreateBy = userContext?.UserName ?? "SYSTEM";
            user.ModifyBy = userContext?.UserName ?? "SYSTEM";
            user.CreateUserId = ParseToLong(userContext?.UserId);
            user.ModifyUserId = ParseToLong(userContext?.UserId);
            
            // Set default values
            user.IsValid = true;
            user.TenantId = userContext?.TenantId ?? "DEFAULT";
        }

        /// <summary>
        /// Initialize update information for User entity
        /// </summary>
        /// <param name="user">User entity to update</param>
        /// <param name="userContext">User context</param>
        public static void InitUpdateInfo(this User user, UserContext userContext)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            
            // Update timestamps
            user.ModifyDate = now;
            
            // Update user information
            user.ModifyBy = userContext?.UserName ?? "SYSTEM";
            user.ModifyUserId = ParseToLong(userContext?.UserId);
        }

        /// <summary>
        /// Initialize create information for InternalNote entity
        /// </summary>
        /// <param name="note">InternalNote entity to initialize</param>
        /// <param name="userContext">User context</param>
        public static void InitCreateInfo(this InternalNote note, UserContext userContext)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            
            // Generate timestamp-based ID
            note.Id = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            
            // Set timestamps
            note.CreateDate = now;
            note.ModifyDate = now;
            
            // Set user information
            note.CreateBy = userContext?.UserName ?? "SYSTEM";
            note.ModifyBy = userContext?.UserName ?? "SYSTEM";
            note.CreateUserId = ParseToLong(userContext?.UserId);
            note.ModifyUserId = ParseToLong(userContext?.UserId);
            
            // Set default values
            note.IsValid = true;
            note.TenantId = userContext?.TenantId ?? "DEFAULT";
        }

        /// <summary>
        /// Initialize update information for InternalNote entity
        /// </summary>
        /// <param name="note">InternalNote entity to update</param>
        /// <param name="userContext">User context</param>
        public static void InitUpdateInfo(this InternalNote note, UserContext userContext)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            
            // Update timestamps
            note.ModifyDate = now;
            
            // Update user information
            note.ModifyBy = userContext?.UserName ?? "SYSTEM";
            note.ModifyUserId = ParseToLong(userContext?.UserId);
        }

        /// <summary>
        /// Initialize create information for OperationChangeLog entity
        /// </summary>
        /// <param name="log">OperationChangeLog entity to initialize</param>
        /// <param name="userContext">User context</param>
        public static void InitCreateInfo(this OperationChangeLog log, UserContext userContext)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            
            // Generate timestamp-based ID
            log.Id = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            
            // Set timestamps
            log.CreateDate = now;
            log.ModifyDate = now;
            
            // Set user information
            log.CreateBy = userContext?.UserName ?? "SYSTEM";
            log.ModifyBy = userContext?.UserName ?? "SYSTEM";
            log.CreateUserId = ParseToLong(userContext?.UserId);
            log.ModifyUserId = ParseToLong(userContext?.UserId);
            
            // Set default values
            log.IsValid = true;
            log.TenantId = userContext?.TenantId ?? "DEFAULT";
        }

        /// <summary>
        /// Initialize update information for OperationChangeLog entity
        /// </summary>
        /// <param name="log">OperationChangeLog entity to update</param>
        /// <param name="userContext">User context</param>
        public static void InitUpdateInfo(this OperationChangeLog log, UserContext userContext)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            
            // Update timestamps
            log.ModifyDate = now;
            
            // Update user information
            log.ModifyBy = userContext?.UserName ?? "SYSTEM";
            log.ModifyUserId = ParseToLong(userContext?.UserId);
        }
    }
} 