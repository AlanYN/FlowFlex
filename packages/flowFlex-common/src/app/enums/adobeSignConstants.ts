import type { AdobeAgreementStatus } from '../../types/adobeSign';

/** 状态颜色配置（与设计规范对齐） */
export const ADOBE_SIGN_STATUS_COLORS: Record<AdobeAgreementStatus, string> = {
	Awaiting: '#F59E0B',
	Completed: '#10B981',
	Declined: '#EF4444',
	Expired: '#6B7280',
	Cancelled: '#374151',
};

/** 状态对应的 el-tag type */
export const ADOBE_SIGN_TAG_TYPES: Record<
	AdobeAgreementStatus,
	'success' | 'warning' | 'danger' | 'info' | 'primary'
> = {
	Awaiting: 'warning',
	Completed: 'primary',
	Declined: 'danger',
	Expired: 'info',
	Cancelled: 'info',
};

/** 过期天数选项 */
export const ADOBE_SIGN_EXPIRATION_OPTIONS = [7, 14, 30, 60, 90] as const;

/** 签署角色选项 */
export const ADOBE_SIGNER_ROLES = ['Signer', 'Approver', 'CC'] as const;
