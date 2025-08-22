/**
 * Portal Access Utilities
 * 处理portal访问时的租户信息提取和设置
 */

import { useUserStoreWithOut } from '@/stores/modules/user';

/**
 * 从URL参数中提取租户信息并设置到用户store
 */
export function extractAndSetTenantInfoFromUrl(): void {
	const urlParams = new URLSearchParams(window.location.search);
	const tenantId = urlParams.get('tenantId') || urlParams.get('tenant_id');
	const appCode = urlParams.get('appCode') || urlParams.get('app_code');

	if (tenantId || appCode) {
		const userStore = useUserStoreWithOut();
		const currentUserInfo = userStore.getUserInfo || {};

		// 更新用户信息中的租户信息
		const updatedUserInfo = {
			...currentUserInfo,
			...(tenantId && { tenantId }),
			...(appCode && { appCode }),
		};

		userStore.setUserInfo(updatedUserInfo);

		console.log('[Portal Access] Updated tenant info from URL:', {
			tenantId,
			appCode,
			updatedUserInfo,
		});
	}
}

/**
 * 检查当前页面是否为portal访问页面
 */
export function isPortalAccessPage(): boolean {
	return window.location.pathname.includes('/portal-access/');
}

/**
 * 在portal页面初始化时自动提取租户信息
 */
export function initPortalAccess(): void {
	if (isPortalAccessPage()) {
		extractAndSetTenantInfoFromUrl();
	}
}

/**
 * 为API请求添加租户header的拦截器辅助函数
 */
export function getTenantHeaders(): Record<string, string> {
	const userStore = useUserStoreWithOut();
	const userInfo = userStore.getUserInfo;

	const headers: Record<string, string> = {};

	if (userInfo?.tenantId) {
		headers['X-Tenant-Id'] = userInfo.tenantId;
	}

	if (userInfo?.appCode) {
		headers['X-App-Code'] = String(userInfo.appCode);
	}

	return headers;
}
