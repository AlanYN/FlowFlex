/**
 * URL 工具函数
 */
import { router } from '@/router';

/**
 * 删除URL中的所有查询参数，但不刷新页面
 * @param replaceHistory 是否替换当前历史记录（默认true），false则添加新的历史记录
 * @param updateRouter 是否同时更新Vue Router（默认true）
 */
export function removeAllUrlParams(replaceHistory = true, updateRouter = true): void {
	const url = new URL(window.location.href);
	const cleanUrl = url.origin + url.pathname;

	// 更新浏览器历史记录
	if (replaceHistory) {
		window.history.replaceState({}, document.title, cleanUrl);
	} else {
		window.history.pushState({}, document.title, cleanUrl);
	}

	// 同时更新Vue Router
	if (updateRouter && router) {
		const currentRoute = router.currentRoute.value;

		// 只有当确实有查询参数时才更新路由
		if (Object.keys(currentRoute.query).length > 0) {
			if (replaceHistory) {
				router.replace({
					path: currentRoute.path,
					query: {},
				});
			} else {
				router.push({
					path: currentRoute.path,
					query: {},
				});
			}
		}
	}
}

/**
 * 删除URL中的特定查询参数，但不刷新页面
 * @param paramsToRemove 要删除的参数名数组
 * @param replaceHistory 是否替换当前历史记录（默认true）
 * @param updateRouter 是否同时更新Vue Router（默认true）
 */
export function removeUrlParams(
	paramsToRemove: string[],
	replaceHistory = true,
	updateRouter = true
): void {
	const url = new URL(window.location.href);

	paramsToRemove.forEach((param) => {
		url.searchParams.delete(param);
	});

	// 更新浏览器历史记录
	if (replaceHistory) {
		window.history.replaceState({}, document.title, url.toString());
	} else {
		window.history.pushState({}, document.title, url.toString());
	}
	window.location.href = window.location.origin;

	// 同时更新Vue Router
	if (updateRouter && router) {
		const currentRoute = router.currentRoute.value;
		const cleanQuery = Object.fromEntries(url.searchParams.entries());

		// 只有当查询参数确实发生变化时才更新路由
		const currentQueryString = new URLSearchParams(
			currentRoute.query as Record<string, string>
		).toString();
		const cleanQueryString = url.searchParams.toString();

		if (currentQueryString !== cleanQueryString) {
			if (replaceHistory) {
				router.replace({
					path: currentRoute.path,
					query: cleanQuery,
				});
			} else {
				router.push({
					path: currentRoute.path,
					query: cleanQuery,
				});
			}
		}
	}
}

/**
 * 添加或更新URL查询参数，但不刷新页面
 * @param params 要添加/更新的参数对象
 * @param replaceHistory 是否替换当前历史记录（默认true）
 * @param updateRouter 是否同时更新Vue Router（默认true）
 */
export function updateUrlParams(
	params: Record<string, string>,
	replaceHistory = true,
	updateRouter = true
): void {
	const url = new URL(window.location.href);

	Object.entries(params).forEach(([key, value]) => {
		url.searchParams.set(key, value);
	});

	// 更新浏览器历史记录
	if (replaceHistory) {
		window.history.replaceState({}, document.title, url.toString());
	} else {
		window.history.pushState({}, document.title, url.toString());
	}

	// 同时更新Vue Router
	if (updateRouter && router) {
		const currentRoute = router.currentRoute.value;
		const newQuery = Object.fromEntries(url.searchParams.entries());

		// 只有当查询参数确实发生变化时才更新路由
		const currentQueryString = new URLSearchParams(
			currentRoute.query as Record<string, string>
		).toString();
		const newQueryString = url.searchParams.toString();

		if (currentQueryString !== newQueryString) {
			if (replaceHistory) {
				router.replace({
					path: currentRoute.path,
					query: newQuery,
				});
			} else {
				router.push({
					path: currentRoute.path,
					query: newQuery,
				});
			}
		}
	}
}

/**
 * 获取当前URL的查询参数
 * @returns 参数对象
 */
export function getUrlParams(): Record<string, string> {
	const params: Record<string, string> = {};
	const urlParams = new URLSearchParams(window.location.search);

	urlParams.forEach((value, key) => {
		params[key] = value;
	});

	return params;
}

/**
 * 检查URL是否包含特定参数
 * @param paramName 参数名
 * @returns 是否包含该参数
 */
export function hasUrlParam(paramName: string): boolean {
	const urlParams = new URLSearchParams(window.location.search);
	return urlParams.has(paramName);
}

/**
 * 删除IDM登录相关的URL参数，但不刷新页面
 * @param replaceHistory 是否替换当前历史记录（默认true）
 * @param updateRouter 是否同时更新Vue Router（默认true）
 */
export function removeIdmParams(replaceHistory = true, updateRouter = true): void {
	const idmParams = ['ticket', 'oauth', 'userId', 'state', 'code'];
	removeUrlParams(idmParams, replaceHistory, updateRouter);
}
