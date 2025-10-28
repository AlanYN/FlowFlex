/**
 * URL 工具函数
 */

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
