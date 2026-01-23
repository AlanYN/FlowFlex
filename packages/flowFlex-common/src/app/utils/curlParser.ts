// 长选项替换映射（完全参考Hoppscotch，增加Postman/Apifox特有选项）
const replaceables: Record<string, string> = {
	'--request': '-X',
	'--header': '-H',
	'--url': '',
	'--form': '-F',
	'--data-raw': '--data',
	'--data': '-d',
	'--data-ascii': '-d',
	'--data-binary': '-d',
	'--data-urlencode': '-d',
	'--user': '-u',
	'--get': '-G',
	'--location': '-L', // Postman/Apifox 特有：跟随重定向
	'--compressed': '', // 移除，不影响解析
	'--insecure': '-k', // 忽略SSL证书验证
};

/**
 * 处理 Windows CMD 特殊转义的 JSON 格式
 */
function processCmdJsonFormat(text: string): string {
	let result = text;

	// 首先移除最外层的 ^"...^" 包装（如果存在）
	if (result.startsWith('^"') && result.endsWith('^"')) {
		result = result.slice(2, -2);
	}

	// 按顺序处理各种 CMD 转义模式
	result = result
		.replace(/\^\\\^"/g, '"') // ^\^" -> "
		.replace(/\^\{/g, '{')
		.replace(/\^\}/g, '}')
		.replace(/\^\[/g, '[')
		.replace(/\^\]/g, ']')
		.replace(/\^:/g, ':')
		.replace(/\^,/g, ',')
		.replace(/\^"/g, '"')
		.replace(/\^'/g, "'")
		.replace(/\\\n/g, '\n')
		.replace(/\^%/g, '%')
		.replace(/\^&/g, '&')
		.replace(/\^#/g, '#');

	return result;
}

/**
 * 基础清理函数（参考Hoppscotch的paperCuts，增强Windows转义符处理和Postman格式支持）
 */
function paperCuts(curlCommand: string): string {
	// 首先处理特殊的CMD JSON格式
	let cleaned = processCmdJsonFormat(curlCommand);

	// 处理 Postman/Apifox 格式: \--option 或 \'--option 变成空格 + --option
	// 这是关键：Postman 导出的格式是 'URL' \--header 这种没有空格的连接
	cleaned = cleaned.replace(/['"]?\s*\\(--[a-zA-Z-]+)/g, ' $1');
	cleaned = cleaned.replace(/['"]?\s*\\(-[a-zA-Z])/g, ' $1');

	return (
		cleaned
			// 移除反斜杠和换行符（标准格式）
			.replace(/ ?\\ ?$/gm, ' ')
			.replace(/\n/g, ' ')
			// 处理Windows风格的^转义符
			.replace(/\^\s*$/gm, '')
			.replace(/\^\n/g, ' ')
			// 处理复杂的Windows嵌套转义
			.replace(/\^\\?\^"/g, '"')
			.replace(/\^\\?\^'/g, "'")
			.replace(/\^\^"/g, '"')
			.replace(/\^\^'/g, "'")
			.replace(/\^\^/g, '^')
			// 处理简单的^转义
			.replace(/\^&/g, '&')
			.replace(/\^</g, '<')
			.replace(/\^>/g, '>')
			.replace(/\^\|/g, '|')
			.replace(/\^"/g, '"')
			.replace(/\^'/g, "'")
			// 处理 Windows CMD 的 ^-H 格式
			.replace(/\^(-[a-zA-Z])/g, '$1')
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
 * 预处理curl命令，清理和标准化格式
 */
function preProcessCurlCommand(curlCommand: string): string {
	if (!curlCommand || curlCommand.length === 0) {
		return '';
	}

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

	// 移除不完整的引号
	if (cleaned.startsWith('"') || cleaned.startsWith("'")) {
		cleaned = cleaned.slice(1).trim();
	}
	if (cleaned.endsWith('"') || cleaned.endsWith("'")) {
		cleaned = cleaned.slice(0, -1).trim();
	}

	return cleaned;
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

/**
 * 基于正则表达式的 curl 解析器
 */
function parseCurlWithRegex(curlCommand: string): {
	url: string;
	method: string;
	headers: Record<string, string>;
	data: string | null;
} {
	const result = {
		url: '',
		method: 'GET',
		headers: {} as Record<string, string>,
		data: null as string | null,
	};

	// 1. 提取 URL - 支持多种格式
	// Postman 格式: curl --location 'URL' 或 curl -L 'URL'
	// 浏览器格式: curl 'URL' 或 curl "URL"
	const urlPatterns = [
		// Postman: --location 'URL' 或 -L 'URL'
		/(?:--location|-L)\s+['"]([^'"]+)['"]/,
		// 标准: curl 'URL' 或 curl "URL"
		/curl\s+['"]([^'"]+)['"]/,
		// 无引号 URL
		/curl\s+(https?:\/\/[^\s]+)/,
		// 任意位置的 URL（作为后备）
		/['"]?(https?:\/\/[^\s'"]+)['"]?/,
	];

	for (const pattern of urlPatterns) {
		const match = curlCommand.match(pattern);
		if (match && match[1]) {
			const url = removeQuotes(match[1]);
			if (url.startsWith('http://') || url.startsWith('https://')) {
				result.url = url;
				break;
			}
		}
	}

	// 2. 提取 HTTP 方法
	const methodMatch = curlCommand.match(/-X\s+['"]?([A-Z]+)['"]?/i);
	if (methodMatch) {
		result.method = methodMatch[1].toUpperCase();
	}

	// 3. 提取所有头部信息
	// 支持 -H 'header' 和 --header 'header' 格式
	const headerRegex = /(?:-H|--header)\s+['"]([^'"]+)['"]/g;
	let headerMatch: RegExpExecArray | null;
	while ((headerMatch = headerRegex.exec(curlCommand)) !== null) {
		const headerStr = headerMatch[1];
		const colonIndex = headerStr.indexOf(':');
		if (colonIndex !== -1) {
			const key = headerStr.substring(0, colonIndex).trim();
			const value = headerStr.substring(colonIndex + 1).trim();
			if (key) {
				result.headers[key] = value;
			}
		}
	}

	// 4. 提取数据内容
	const dataPatterns = [
		/(?:--data-raw|-d|--data)\s+['"](.+?)['"](?=\s+-|\s+--|\s*$)/s,
		/(?:--data-raw|-d|--data)\s+([^\s-][^\s]*)(?=\s+-|\s+--|\s*$)/s,
	];

	for (const pattern of dataPatterns) {
		const dataMatch = curlCommand.match(pattern);
		if (dataMatch) {
			result.data = dataMatch[1].trim();
			break;
		}
	}

	return result;
}

export function parseCurl(curlCommand: string): ParsedCurlConfig {
	try {
		// 清理curl命令
		const preprocessedCommand = preProcessCurlCommand(curlCommand);

		if (!preprocessedCommand) {
			throw new Error('cURL command cannot be empty');
		}

		// 使用正则表达式解析器
		const parsed = parseCurlWithRegex(preprocessedCommand);

		// 解析 URL 和查询参数
		const urlObj = parsed.url ? new URL(parsed.url) : new URL('');
		const params: Record<string, string> = {};
		urlObj.searchParams.forEach((value, key) => {
			params[key] = value;
		});

		const headers = parsed.headers;
		const rawContentType = headers['Content-Type'] || headers['content-type'] || '';
		const method = parsed.method;

		// 处理 body 数据
		let bodyType: ParsedCurlConfig['bodyType'] = 'none';
		let formData: Record<string, string> | undefined;
		let urlEncoded: Record<string, string> | undefined;
		let rawBody: string | undefined;
		let rawFormat = 'json';

		if (parsed.data) {
			let rawData = removeQuotes(parsed.data);

			// 处理 CMD JSON 格式
			if (
				rawData.includes('^{') ||
				rawData.includes('^[') ||
				rawData.includes('^\\^"') ||
				rawData.includes('^"')
			) {
				rawData = processCmdJsonFormat(rawData);
			}

			// 清理隐藏字符
			rawData = rawData
				.replace(/^\uFEFF/, '')
				// eslint-disable-next-line no-control-regex
				.replace(/[\x00-\x08\x0E-\x1F\x7F]/g, '')
				.replace(/\u00A0/g, ' ')
				.trim();

			// 根据Content-Type判断body类型
			if (rawContentType.includes('application/x-www-form-urlencoded')) {
				bodyType = 'x-www-form-urlencoded';
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

		return {
			url: urlObj.origin + urlObj.pathname,
			method,
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
	if (!config.url || !config.method) {
		return false;
	}

	try {
		new URL(config.url);
	} catch {
		return false;
	}

	const validMethods = ['GET', 'POST', 'PUT', 'DELETE', 'PATCH', 'HEAD', 'OPTIONS'];
	if (!validMethods.includes(config.method.toUpperCase())) {
		return false;
	}

	return true;
}
