export interface IBaseDataPermission {
	userPermissions?: {
		currentUserCanEdit?: boolean;
		currentUserCanView?: boolean;
		currentUserCanDelete?: boolean;
	};
}
