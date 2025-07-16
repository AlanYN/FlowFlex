import { defHttp } from '@/apis/axios';
import { getCurrentBaseUrl } from '@/utils/url';

export interface PortalUser {
	id: string;
	email: string;
	status: string;
	sentDate: string;
	invitationToken: string;
	tokenExpiry: string;
	lastLoginDate?: string;
}

export interface UserInvitationRequest {
	onboardingId: string;
	emailAddresses: string[];
	baseUrl?: string; // 添加可选的基础URL参数
}

export interface UserInvitationResponse {
	successfulInvitations: string[];
	failedInvitations: Record<string, string>;
	totalSent: number;
	totalFailed: number;
}

export interface ResendInvitationRequest {
	onboardingId: string;
	email: string;
	baseUrl?: string; // 添加可选的基础URL参数
}

export interface PortalAccessVerificationRequest {
	token: string;
	email: string;
}

export interface PortalAccessVerificationResponse {
	isValid: boolean;
	onboardingId: string;
	email: string;
	accessToken: string;
	errorMessage?: string;
}

export interface TokenValidationRequest {
	token: string;
	onboardingId: string;
}

export interface TokenValidationResponse {
	isValid: boolean;
	email?: string;
	errorMessage?: string;
}

/**
 * Send user invitations
 */
export const sendInvitations = (request: UserInvitationRequest) => {
	// 自动获取当前浏览器的基础URL
	const finalRequest = {
		...request,
		baseUrl: request.baseUrl || getCurrentBaseUrl(),
	};

	return defHttp.post({
		url: '/api/ow/user-invitations/v1/send',
		data: finalRequest,
	});
};

/**
 * Get portal users by onboarding ID
 */
export const getPortalUsers = (onboardingId: string) => {
	return defHttp.get<PortalUser[]>({
		url: `/api/ow/user-invitations/v1/portal-users/${onboardingId}`,
	});
};

/**
 * Resend invitation
 */
export const resendInvitation = (request: ResendInvitationRequest) => {
	// 自动获取当前浏览器的基础URL
	const finalRequest = {
		...request,
		baseUrl: request.baseUrl || getCurrentBaseUrl(),
	};

	return defHttp.post<{ success: boolean }>({
		url: '/api/ow/user-invitations/v1/resend',
		data: finalRequest,
	});
};

/**
 * Verify portal access
 */
export const verifyPortalAccess = (request: PortalAccessVerificationRequest) => {
	return defHttp.post<PortalAccessVerificationResponse>({
		url: '/api/ow/user-invitations/v1/verify-access',
		data: request,
	});
};

/**
 * Validate invitation token
 */
export const validateInvitationToken = (request: TokenValidationRequest) => {
	return defHttp.post<TokenValidationResponse>({
		url: '/api/ow/user-invitations/v1/validate-token',
		data: request,
	});
};

/**
 * Remove portal access
 */
export const removePortalAccess = (onboardingId: string, email: string) => {
	return defHttp.delete<{ success: boolean }>({
		url: `/api/ow/user-invitations/v1/remove-access/${onboardingId}?email=${encodeURIComponent(
			email
		)}`,
	});
};
