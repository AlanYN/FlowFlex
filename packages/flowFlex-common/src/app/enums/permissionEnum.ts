/**
 * Portal Availability Enum
 * Controls the visibility and interaction level in the customer portal
 */
export enum PortalAvailabilityEnum {
	/** Content is not available in the customer portal */
	NotAvailable = 0,
	/** User can view the content but cannot complete or modify it */
	Viewable = 1,
	/** User can view and complete the content */
	Completable = 2,
}

/**
 * View Permission Mode Enum
 * Controls who can view the content
 */
export enum ViewPermissionModeEnum {
	/** All users can view */
	Public = 0,
	/** Only listed teams can view */
	VisibleTo = 1,
	/** All teams except listed teams can view */
	InvisibleTo = 2,
	/** Only the creator/owner can view */
	Private = 3,
}

/**
 * Get portal availability label
 */
export const getPortalAvailabilityLabel = (value: PortalAvailabilityEnum): string => {
	const labels = {
		[PortalAvailabilityEnum.NotAvailable]: 'Not Available',
		[PortalAvailabilityEnum.Viewable]: 'Viewable',
		[PortalAvailabilityEnum.Completable]: 'Completable',
	};
	return labels[value] || 'Unknown';
};

/**
 * Get view permission mode label
 */
export const getViewPermissionModeLabel = (value: ViewPermissionModeEnum): string => {
	const labels = {
		[ViewPermissionModeEnum.Public]: 'Public',
		[ViewPermissionModeEnum.VisibleTo]: 'Visible to',
		[ViewPermissionModeEnum.InvisibleTo]: 'Invisible to',
		[ViewPermissionModeEnum.Private]: 'Private',
	};
	return labels[value] || 'Unknown';
};

/**
 * Case Permission Mode Enum
 * Controls who can view and operate on cases
 */
export enum CasePermissionModeEnum {
	/** All users can access */
	Public = 0,
	/** Only listed teams can access */
	VisibleTo = 1,
	/** All teams except listed teams can access */
	InvisibleTo = 2,
	/** Only the creator/owner can access */
	Private = 3,
}

/**
 * Get case permission mode label
 */
export const getCasePermissionModeLabel = (value: CasePermissionModeEnum): string => {
	const labels = {
		[CasePermissionModeEnum.Public]: 'Public',
		[CasePermissionModeEnum.VisibleTo]: 'Visible to',
		[CasePermissionModeEnum.InvisibleTo]: 'Invisible to',
		[CasePermissionModeEnum.Private]: 'Private',
	};
	return labels[value] || 'Unknown';
};

/**
 * Permission Subject Type Enum
 * Controls whether permissions are applied to teams or individual users
 */
export enum PermissionSubjectTypeEnum {
	/** Team-based permissions - Permission subjects are team names */
	Team = 1,
	/** User-based permissions - Permission subjects are individual user IDs */
	User = 2,
}

export const ProjectPermissionEnum = {
	case: {
		create: 'CASE:CREATE',
		read: 'CASE:READ',
		update: 'CASE:UPDATE',
		delete: 'CASE:DELETE',
	},
	workflow: {
		create: 'WORKFLOW:CREATE',
		read: 'WORKFLOW:READ',
		update: 'WORKFLOW:UPDATE',
		delete: 'WORKFLOW:DELETE',
	},
	checkList: {
		create: 'CHECKLIST:CREATE',
		read: 'CHECKLIST:READ',
		update: 'CHECKLIST:UPDATE',
		delete: 'CHECKLIST:DELETE',
	},
	question: {
		create: 'QUESTION:CREATE',
		read: 'QUESTION:READ',
		update: 'QUESTION:UPDATE',
		delete: 'QUESTION:DELETE',
	},
	tool: {
		create: 'TOOL:CREATE',
		read: 'TOOL:READ',
		update: 'TOOL:UPDATE',
		delete: 'TOOL:DELETE',
	},
	integration: {
		create: 'INTEGRATION:CREATE',
		read: 'INTEGRATION:READ',
		update: 'INTEGRATION:UPDATE',
		delete: 'INTEGRATION:DELETE',
	},
	dynamicField: {
		create: 'DYNAMICFIELD:CREATE',
		read: 'DYNAMICFIELD:READ',
		update: 'DYNAMICFIELD:UPDATE',
		delete: 'DYNAMICFIELD:DELETE',
	},
} as const;

// 类型定义
export type ProjectPermissionType =
	| (typeof ProjectPermissionEnum.case)[keyof typeof ProjectPermissionEnum.case]
	| (typeof ProjectPermissionEnum.workflow)[keyof typeof ProjectPermissionEnum.workflow]
	| (typeof ProjectPermissionEnum.checkList)[keyof typeof ProjectPermissionEnum.checkList]
	| (typeof ProjectPermissionEnum.question)[keyof typeof ProjectPermissionEnum.question]
	| (typeof ProjectPermissionEnum.tool)[keyof typeof ProjectPermissionEnum.tool]
	| (typeof ProjectPermissionEnum.integration)[keyof typeof ProjectPermissionEnum.integration]
	| (typeof ProjectPermissionEnum.dynamicField)[keyof typeof ProjectPermissionEnum.dynamicField];

// 辅助类型：获取所有权限值的联合类型
export type ProjectPermissionValue = ProjectPermissionType;
