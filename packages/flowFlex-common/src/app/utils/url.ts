/**
 * 获取当前浏览器的基础 URL
 * @returns 当前域名和端口的基础 URL
 */
export function getCurrentBaseUrl(): string {
	if (typeof window !== 'undefined') {
		return window.location.origin;
	}
	return '';
}

/**
 * 获取当前完整的 URL
 * @returns 当前页面的完整 URL
 */
export function getCurrentFullUrl(): string {
	if (typeof window !== 'undefined') {
		return window.location.href;
	}
	return '';
}

/**
 * 获取当前页面的 pathname
 * @returns 当前页面的路径
 */
export function getCurrentPathname(): string {
	if (typeof window !== 'undefined') {
		return window.location.pathname;
	}
	return '';
}

/**
 * 构建门户访问 URL（指向onboardDetail页面）
 * @param token 邀请令牌
 * @param onboardingId onboarding ID
 * @param baseUrl 可选的基础 URL，默认使用当前浏览器的 URL
 * @returns 完整的门户访问 URL
 */
export function buildPortalAccessUrl(
	token: string,
	onboardingId: string,
	baseUrl?: string
): string {
	const base = baseUrl || getCurrentBaseUrl();
	return `${base}/onboard/onboardDetail?onboardingId=${encodeURIComponent(
		onboardingId
	)}&token=${encodeURIComponent(token)}`;
}

/**
 * 从URL中提取查询参数
 * @param url 完整的URL
 * @param paramName 要提取的参数名
 * @returns 参数值或null
 */
export function getUrlParam(url: string, paramName: string): string | null {
	try {
		const urlObj = new URL(url);
		return urlObj.searchParams.get(paramName);
	} catch {
		return null;
	}
}

/**
 * 向URL添加或更新查询参数
 * @param baseUrl 基础URL
 * @param params 要添加的参数对象
 * @returns 更新后的URL
 */
export function updateUrlParams(baseUrl: string, params: Record<string, string>): string {
	try {
		const urlObj = new URL(baseUrl);
		Object.entries(params).forEach(([key, value]) => {
			urlObj.searchParams.set(key, value);
		});
		return urlObj.toString();
	} catch {
		return baseUrl;
	}
}
