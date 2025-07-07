interface WhitelistConfig {
	// 完全匹配的URL列表
	urls: string[];
	// 正则表达式匹配的URL模式
	patterns: RegExp[];
}

export const errorWhitelist: WhitelistConfig = {
	urls: [],
	patterns: [
		/^\/api\/system/, // 系统接口
		/^\/crm\/system/, // 问卷创建接口
	],
};

/**
 * 检查URL是否在白名单中
 * @param url 需要检查的URL
 * @returns boolean 是否在白名单中
 */
export function isUrlInWhitelist(url: string): boolean {
	// 检查完全匹配的URL
	if (errorWhitelist.urls.includes(url)) {
		return true;
	}

	// 检查正则表达式匹配
	return errorWhitelist.patterns.some((pattern) => pattern.test(url));
}
