// import * as curlconverter from 'curlconverter';
import parser from 'yargs-parser/browser';

// 长选项替换映射（完全参考Hoppscotch）
const replaceables: Record<string, string> = {
	'--request': '-X',
	'--header': '-H',
	'--url': '',
	'--form': '-F',
	'--data-raw': '--data',
	'--data': '-d',
	'--data-ascii': '-d',
	'--data-binary': '-d',
	'--user': '-u',
	'--get': '-G',
};

/**
 * 基础清理函数（参考Hoppscotch的paperCuts，增强Windows转义符处理）
 */
function paperCuts(curlCommand: string): string {
	return (
		curlCommand
			// 移除反斜杠和换行符
			.replace(/ ?\\ ?$/gm, ' ')
			.replace(/\n/g, ' ')
			// 处理Windows风格的^转义符（按顺序处理，避免冲突）
			.replace(/\^\s*$/gm, '') // 处理行末的^符号
			.replace(/\^\n/g, ' ') // 处理^换行符
			// 处理复杂的Windows嵌套转义（按复杂度顺序处理）
			// 首先处理最复杂的 ^\^" 模式 -> "
			.replace(/\^\\?\^"/g, '"') // 处理^\^"或^\\^"嵌套转义
			.replace(/\^\\?\^'/g, "'") // 处理^\^'或^\\^'嵌套转义
			// 然后处理双重^转义
			.replace(/\^\^"/g, '"') // 处理^^"双重^转义
			.replace(/\^\^'/g, "'") // 处理^^'双重^转义
			.replace(/\^\^/g, '^') // 处理^^双重^转义
			// 处理简单的^转义
			.replace(/\^&/g, '&') // 处理^&转义
			.replace(/\^</g, '<') // 处理^<转义
			.replace(/\^>/g, '>') // 处理^>转义
			.replace(/\^\|/g, '|') // 处理^|转义
			.replace(/\^"/g, '"') // 处理^"转义
			.replace(/\^'/g, "'") // 处理^'转义
			// 移除$符号
			.replace(/\$'/g, "'")
			.replace(/\$"/g, '"')
			.trim()
	);
}

/**
 * 替换长选项为短选项（参考Hoppscotch）
 */
function replaceLongOptions(curlCmd: string): string {
	return Object.keys(replaceables).reduce((cmd, longOption) => {
		const shortOption = replaceables[longOption];
		if (
			longOption.includes('data') ||
			longOption.includes('form') ||
			longOption.includes('header')
		) {
			return cmd.replace(new RegExp(`[ \\t]${longOption}(["' ])`, 'g'), ` ${shortOption}$1`);
		} else {
			return cmd.replace(new RegExp(`[ \\t]${longOption}(["' ])`), ` ${shortOption}$1`);
		}
	}, curlCmd);
}

/**
 * 预处理-X参数格式（参考Hoppscotch）
 */
function prescreenXArgs(curlCommand: string): string {
	return curlCommand
		.replace(/ -X(GET|POST|PUT|PATCH|DELETE|HEAD|CONNECT|OPTIONS|TRACE)/, ' -X $1')
		.trim();
}

/**
 * 预处理curl命令，清理和标准化格式（完全参考Hoppscotch的实现）
 * @param curlCommand 原始curl命令字符串
 * @returns 清理后的curl命令字符串
 */
function preProcessCurlCommand(curlCommand: string): string {
	if (!curlCommand || curlCommand.length === 0) {
		return '';
	}

	// 完全按照Hoppscotch的流程：paperCuts -> replaceLongOptions -> prescreenXArgs
	return prescreenXArgs(replaceLongOptions(paperCuts(curlCommand)));
}

/**
 * 递归移除字符串首尾的引号（包括不完整的引号）
 */
function removeQuotes(str: string): string {
	let cleaned = str.trim();

	// 递归移除完整的引号对
	while (
		(cleaned.startsWith('"') && cleaned.endsWith('"')) ||
		(cleaned.startsWith("'") && cleaned.endsWith("'"))
	) {
		cleaned = cleaned.slice(1, -1).trim();
	}

	// 移除不完整的引号（只有开头或只有结尾的引号）
	if (cleaned.startsWith('"') || cleaned.startsWith("'")) {
		cleaned = cleaned.slice(1).trim();
	}
	if (cleaned.endsWith('"') || cleaned.endsWith("'")) {
		cleaned = cleaned.slice(0, -1).trim();
	}

	return cleaned;
}

/**
 * 解析单个header字符串为键值对（参考Hoppscotch，增强引号处理）
 */
function getHeaderPair(headerString: string): [string, string] | null {
	// 先移除整个header字符串的首尾引号
	const cleanHeader = removeQuotes(headerString);

	// 查找第一个冒号的位置来分割key和value
	const colonIndex = cleanHeader.indexOf(':');
	if (colonIndex === -1) {
		return null;
	}

	// 分割key和value
	let key = cleanHeader.substring(0, colonIndex).trim();
	let value = cleanHeader.substring(colonIndex + 1).trim();

	// 递归移除key和value中的引号
	key = removeQuotes(key);
	value = removeQuotes(value);

	// 确保key不为空
	if (!key) {
		return null;
	}

	return [key, value];
}

/**
 * 从yargs-parser结果中提取HTTP请求头（完全参考Hoppscotch的实现）
 * @param args yargs-parser解析结果
 * @returns HTTP请求头对象和原始Content-Type
 */
function extractHeaders(args: any): { headers: Record<string, string>; rawContentType: string } {
	const headers: Record<string, string> = {};

	// 处理-H选项（参考Hoppscotch的逻辑）
	let headerStrings: string[] = [];

	if (typeof args.H === 'string') {
		headerStrings = [args.H];
	} else if (Array.isArray(args.H)) {
		headerStrings = args.H;
	}

	// 解析所有header字符串
	headerStrings.forEach((headerString) => {
		const pair = getHeaderPair(headerString);
		if (pair) {
			headers[pair[0]] = pair[1];
		}
	});

	// 处理User-Agent（参考Hoppscotch）
	if (args.A || args['user-agent']) {
		headers['User-Agent'] = args.A ?? args['user-agent'];
	}

	// 获取原始Content-Type
	const rawContentType = headers['Content-Type'] ?? headers['content-type'] ?? '';

	return {
		headers,
		rawContentType,
	};
}

/**
 * 从-X参数获取HTTP方法（参考Hoppscotch）
 */
function getMethodFromXArg(args: any): string | null {
	if (typeof args.X === 'string') {
		const xarg = args.X.trim();
		const methodMatch = xarg.match(/GET|POST|PUT|PATCH|DELETE|HEAD|CONNECT|OPTIONS|TRACE/i);
		if (methodMatch) {
			return methodMatch[0];
		}
		// 如果没有匹配到标准方法，尝试匹配任何字母
		const anyMethodMatch = xarg.match(/[a-zA-Z]+/);
		if (anyMethodMatch) {
			return anyMethodMatch[0];
		}
	}
	return null;
}

/**
 * 通过其他参数推断HTTP方法（参考Hoppscotch）
 */
function getMethodByDeduction(args: any): string | null {
	// 如果有上传文件参数，推断为PUT
	if (args.T || args['upload-file']) {
		return 'PUT';
	}
	// 如果有head参数，推断为HEAD
	if (args.I || args.head) {
		return 'HEAD';
	}
	// 如果有-G参数，推断为GET
	if (args.G) {
		return 'GET';
	}
	// 如果有数据或表单参数，推断为POST
	if (args.d || args.F) {
		return 'POST';
	}
	return null;
}

/**
 * 提取HTTP方法（完全参考Hoppscotch的实现）
 */
function extractMethod(args: any): string {
	// 首先尝试从-X参数获取
	const methodFromX = getMethodFromXArg(args);
	if (methodFromX) {
		return methodFromX;
	}

	// 然后尝试通过其他参数推断
	const methodByDeduction = getMethodByDeduction(args);
	if (methodByDeduction) {
		return methodByDeduction;
	}

	// 默认返回GET
	return 'GET';
}

/**
 * 检查URL是否有效
 */
function isURLValid(urlString: string): boolean {
	try {
		new URL(urlString);
		return true;
	} catch {
		return false;
	}
}

/**
 * 为本地URL添加协议
 */
function getProtocolFromURL(url: string): string {
	const match = /^([^\s:@]+:[^\s:@]+@)?([^:/\s]+)([:]*)/.exec(url);
	if (match && match.length > 1) {
		const baseUrl = match[2];
		// 为本地URL设置http协议
		if (
			baseUrl === 'localhost' ||
			baseUrl === '2130706433' ||
			/127(\.0){0,2}\.1/.test(baseUrl) ||
			/192\.168(\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){2}/.test(baseUrl) ||
			/10(\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){3}/.test(baseUrl)
		) {
			return 'http://' + url;
		} else {
			return 'https://' + url;
		}
	}
	return url;
}

/**
 * 解析URL字符串
 */
function parseURL(urlText: string | number): URL | null {
	if (!urlText) return null;

	// 预处理URL字符串
	let urlString = urlText
		.toString()
		.replace(/^'|'$/g, '') // 移除首尾引号
		.replace(/[^a-zA-Z0-9_\-./?&=:@%+#,;()'<>\s]/g, ''); // 移除特殊字符

	if (urlString.length === 0) return null;

	// 检查是否有协议
	if (!/^[^:\s]+(?=:\/\/)/.test(urlString)) {
		urlString = getProtocolFromURL(urlString);
	}

	if (isURLValid(urlString)) {
		return new URL(urlString);
	}

	return null;
}

/**
 * 提取URL对象（参考Hoppscotch的实现）
 */
function extractURL(args: any): URL {
	// 从非选项参数中查找URL（跳过第一个参数，通常是curl命令本身）
	const urlCandidates = [...args._.slice(1), args.location].filter(Boolean);

	for (const candidate of urlCandidates) {
		const url = parseURL(candidate);
		if (url) {
			return url;
		}
	}

	// 如果没有找到有效URL，返回默认URL
	return new URL('');
}

export interface ParsedCurlConfig {
	url: string;
	method: string;
	headers: Record<string, string>;
	params: Record<string, string>;
	bodyType: 'none' | 'form-data' | 'x-www-form-urlencoded' | 'raw';
	formData?: Record<string, string>;
	urlEncoded?: Record<string, string>;
	rawBody?: string;
	rawFormat?: string;
}

export function parseCurl(curlCommand: string): ParsedCurlConfig {
	try {
		// 清理curl命令，移除多余的空白字符
		const cleanCommand = preProcessCurlCommand(curlCommand);

		if (!cleanCommand) {
			throw new Error('cURL command cannot be empty');
		}

		// 使用yargs-parser解析curl命令，替代JSON.parse
		const args = parser(cleanCommand);

		// 使用Hoppscotch风格的提取函数
		const urlObj = extractURL(args);
		const params: Record<string, string> = {};
		urlObj.searchParams.forEach((value, key) => {
			params[key] = value;
		});

		// 提取HTTP请求头
		const headerResult = extractHeaders(args);
		const headers = headerResult.headers;
		const rawContentType = headerResult.rawContentType;

		// 提取HTTP方法
		const method = extractMethod(args);

		// 简化的body处理（使用rawContentType来推断格式）
		let bodyType: ParsedCurlConfig['bodyType'] = 'none';
		let formData: Record<string, string> | undefined;
		let urlEncoded: Record<string, string> | undefined;
		let rawBody: string | undefined;
		let rawFormat = 'json';

		// 检查是否有form数据
		const formArgs = args.F || args.form;
		if (formArgs) {
			bodyType = 'form-data';
			formData = {};
			const formList = Array.isArray(formArgs) ? formArgs : [formArgs];
			formList.forEach((formItem: string) => {
				if (formItem && typeof formItem === 'string') {
					const [key, ...valueParts] = formItem.split('=');
					if (key && formData) {
						formData[key] = valueParts.join('=') || '';
					}
				}
			});
		}

		// 检查是否有data数据
		const dataArgs = args.d || args.data;
		if (dataArgs && !formArgs) {
			const rawData = Array.isArray(dataArgs) ? dataArgs.join('') : dataArgs;

			// 根据Content-Type判断body类型
			if (rawContentType && rawContentType.includes('application/x-www-form-urlencoded')) {
				bodyType = 'x-www-form-urlencoded';
				// 解析URL编码数据
				urlEncoded = {};
				const pairs = rawData.split('&');
				pairs.forEach((pair) => {
					const [key, value] = pair.split('=');
					if (key && urlEncoded) {
						try {
							urlEncoded[decodeURIComponent(key)] = decodeURIComponent(value || '');
						} catch {
							urlEncoded[key] = value || '';
						}
					}
				});
			} else {
				bodyType = 'raw';
				rawBody = rawData;
				// 根据Content-Type推断格式
				if (rawContentType) {
					if (rawContentType.includes('application/json')) {
						rawFormat = 'json';
					} else if (
						rawContentType.includes('application/xml') ||
						rawContentType.includes('text/xml')
					) {
						rawFormat = 'xml';
					} else if (rawContentType.includes('text/html')) {
						rawFormat = 'html';
					} else if (rawContentType.includes('text/')) {
						rawFormat = 'text';
					}
				}
			}
		}

		return {
			url: urlObj.origin + urlObj.pathname,
			method: method,
			headers,
			params,
			bodyType,
			formData,
			urlEncoded,
			rawBody,
			rawFormat,
		};
	} catch (error) {
		if (error instanceof Error) {
			// 提供更友好的错误信息
			if (error.message.includes('Invalid URL')) {
				throw new Error('Invalid URL in cURL command. Please check the URL format.');
			} else if (error.message.includes('Unexpected token')) {
				throw new Error('Invalid cURL command format. Please check the syntax.');
			} else {
				throw new Error(`Failed to parse cURL command: ${error.message}`);
			}
		} else {
			throw new Error('Failed to parse cURL command: Unknown error');
		}
	}
}

/**
 * 验证解析结果的完整性
 */
export function validateParsedConfig(config: ParsedCurlConfig): boolean {
	// 基本验证
	if (!config.url || !config.method) {
		return false;
	}

	// URL格式验证
	try {
		new URL(config.url);
	} catch {
		return false;
	}

	// HTTP方法验证
	const validMethods = ['GET', 'POST', 'PUT', 'DELETE', 'PATCH', 'HEAD', 'OPTIONS'];
	if (!validMethods.includes(config.method.toUpperCase())) {
		return false;
	}

	return true;
}
