/**
 * Portal Permission Enum
 * Defines the level of access a user has in the customer portal for a specific stage
 */
export enum PortalPermissionEnum {
	/** Viewable only - User can view the stage content but cannot complete or modify it */
	Viewable = 1,
	/** Completable - User can view and complete the stage */
	Completable = 2,
}

/**
 * Portal Permission Options for UI
 */
export const portalPermissionOptions = [
	{
		label: 'View only',
		value: PortalPermissionEnum.Viewable,
		key: 'viewable',
	},
	{
		label: 'Completable',
		value: PortalPermissionEnum.Completable,
		key: 'completable',
	},
];

export type PortalPermissionType = PortalPermissionEnum.Viewable | PortalPermissionEnum.Completable;
