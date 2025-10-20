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
	VisibleToTeams = 1,
	/** All teams except listed teams can view */
	InvisibleToTeams = 2,
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
		[ViewPermissionModeEnum.VisibleToTeams]: 'Visible to',
		[ViewPermissionModeEnum.InvisibleToTeams]: 'Invisible to',
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
	VisibleToTeams = 1,
	/** All teams except listed teams can access */
	InvisibleToTeams = 2,
	/** Only the creator/owner can access */
	Private = 3,
}

/**
 * Get case permission mode label
 */
export const getCasePermissionModeLabel = (value: CasePermissionModeEnum): string => {
	const labels = {
		[CasePermissionModeEnum.Public]: 'Public',
		[CasePermissionModeEnum.VisibleToTeams]: 'Visible to',
		[CasePermissionModeEnum.InvisibleToTeams]: 'Invisible to',
		[CasePermissionModeEnum.Private]: 'Private',
	};
	return labels[value] || 'Unknown';
};
