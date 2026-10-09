export interface IBaseDataPermission {
	userPermissions?: {
		currentUserCanEdit?: boolean;
		currentUserCanView?: boolean;
		currentUserCanDelete?: boolean;
	};
}

/** Edit mode for a Univer range-protection rule (matches Univer's EditStateEnum values). */
export type RangeProtectionEditState = 'onlyMe' | 'designedUserCanEdit';

/** A single range-protection rule as carried in a `sheet.mutation.add-range-protection` params. */
export interface RangeProtectionMutationRule {
	permissionId: string;
	editState?: RangeProtectionEditState;
	viewState?: string;
	allowedUsers?: string[];
	ranges?: unknown[];
	description?: string;
}

/** Params shape for `sheet.mutation.add-range-protection` / `set-range-protection`. */
export interface RangeProtectionMutationParams {
	unitId: string;
	subUnitId: string;
	rules?: RangeProtectionMutationRule[];
	ruleIds?: string[];
}
