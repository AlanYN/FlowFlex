using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlowFlex.Domain.Shared.Const;
using FlowFlex.Domain.Shared.Enums;

namespace FlowFlex.Domain.Shared.Models
{
    public class UserContext : UserInfoModel
    {
        public string Schema { get; set; }
        public string ThirdPartyToken { get; set; }

        public string ValidationVersion { get; set; }

        public string IamToken { get; set; }

        public TimeSpan UtcOffset { get; set; } = TimeZoneInfo.Local.BaseUtcOffset;

        /// <summary>
        /// Application code for application isolation
        /// </summary>
        public string AppCode { get; set; }


        /// <summary>
        /// Gets the permission type for a user based on the specified permission code and operation type
        /// </summary>
        /// <param name="permissionCode">Permission code</param>
        /// <param name="operationType">Operation type</param>
        /// <returns>Permission type, returns None if no matching permission is found</returns>
        public DataPermissionTypeEnum GetDataPermissionType(string permissionCode, DataPermissionOperationTypeEnum operationType)
        {
            if (Schema == AuthSchemes.IdentityClient || Schema == AuthSchemes.ItemIamClientIdentification)
            {
                return DataPermissionTypeEnum.Everything;
            }

            if (UserDataPermissionConfig?.DataPermissions == null)
            {
                return DataPermissionTypeEnum.None;
            }

            var dataPermission = UserDataPermissionConfig.DataPermissions
                .FirstOrDefault(a => a.Code == permissionCode);

            if (dataPermission == null)
            {
                return DataPermissionTypeEnum.None;
            }

            return operationType switch
            {
                DataPermissionOperationTypeEnum.View => dataPermission.ViewType.HasValue ? (DataPermissionTypeEnum)dataPermission.ViewType : DataPermissionTypeEnum.None,
                DataPermissionOperationTypeEnum.Edit => dataPermission.EditType.HasValue ? (DataPermissionTypeEnum)dataPermission.EditType : DataPermissionTypeEnum.None,
                DataPermissionOperationTypeEnum.Delete => dataPermission.DeleteType.HasValue ? (DataPermissionTypeEnum)dataPermission.DeleteType : DataPermissionTypeEnum.None,
                _ => DataPermissionTypeEnum.None
            };
        }

        public UserPermissionsModel MakeUserPermissions(string dataPermissionCode, long? dataTeamId, long? assignedTo)
        {
            return new UserPermissionsModel()
            {
                CurrentUserCanView = EvaluateUserPermissions(dataPermissionCode, DataPermissionOperationTypeEnum.View, dataTeamId, assignedTo),
                CurrentUserCanEdit = EvaluateUserPermissions(dataPermissionCode, DataPermissionOperationTypeEnum.Edit, dataTeamId, assignedTo),
                CurrentUserCanDelete = EvaluateUserPermissions(dataPermissionCode, DataPermissionOperationTypeEnum.Delete, dataTeamId, assignedTo),
            };
        }
        public bool EvaluateUserPermissions(string dataPermissionCode,
            DataPermissionOperationTypeEnum operationType,
            long? dataTeamId,
            long? assignedTo)
        {
            var result = false;
            var viewType = GetDataPermissionType(dataPermissionCode, operationType);
            switch (viewType)
            {
                case DataPermissionTypeEnum.Everything:
                    result = true;
                    break;
                case DataPermissionTypeEnum.TeamOnly:
                    if (dataTeamId.HasValue)
                    {
                        result = UserTeams?.GetAllTeamIds().Contains(dataTeamId.Value) ?? false;
                    }
                    break;
                case DataPermissionTypeEnum.OwnedOnly:
                    result = Convert.ToString(assignedTo) == UserId;
                    break;
                default:
                    result = false;
                    break;
            }
            return result;
        }
    }
}
