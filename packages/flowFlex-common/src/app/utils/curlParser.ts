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
 * 处理 Windows CMD 特殊转义的 JSON 格式
 */
function processCmdJsonFormat(text: string): string {
	// 处理 Windows CMD 格式的 JSON 转义
	// 例如：^"^{^\^"key^\^":^\^"value^\^"^}^" -> {"key":"value"}
	// 复杂格式：^{"workflowId":"123","array":^["item1","item2"^]^}

	let result = text;

	console.log('processCmdJsonFormat 输入:', result.substring(0, 200) + '...');

	// 首先移除最外层的 ^"...^" 包装（如果存在）
	if (result.startsWith('^"') && result.endsWith('^"')) {
		result = result.slice(2, -2);
		console.log('移除外层包装后:', result.substring(0, 100) + '...');
	}

	// 按顺序处理各种 CMD 转义模式
	// 1. 处理最复杂的 ^\^" 模式 -> "
	result = result.replace(/\^\\\^"/g, '"');

	// 2. 处理 ^{ 和 ^} 模式
	result = result.replace(/\^\{/g, '{');
	result = result.replace(/\^\}/g, '}');

	// 3. 处理 ^[ 和 ^] 模式
	result = result.replace(/\^\[/g, '[');
	result = result.replace(/\^\]/g, ']');

	// 4. 处理 ^: 模式 -> :
	result = result.replace(/\^:/g, ':');

	// 5. 处理 ^, 模式 -> ,
	result = result.replace(/\^,/g, ',');

	// 6. 处理简单的 ^" 模式 -> "
	result = result.replace(/\^"/g, '"');
	// 7. 处理其他转义字符
	result = result
		.replace(/\^'/g, "'") // 处理 ^' -> '
		.replace(/\\\n/g, '\n') // 处理转义的换行符
		.replace(/\^%/g, '%') // 处理 ^% -> %
		.replace(/\^&/g, '&') // 处理 ^& -> &
		.replace(/\^#/g, '#'); // 处理 ^# -> #

	console.log('processCmdJsonFormat 输出:', result.substring(0, 200) + '...');

	return result;
}

/**
 * 基础清理函数（参考Hoppscotch的paperCuts，增强Windows转义符处理）
 */
function paperCuts(curlCommand: string): string {
	// 首先处理特殊的CMD JSON格式
	const cleaned = processCmdJsonFormat(curlCommand);

	return (
		cleaned
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
 * 基于正则表达式的 curl 解析器（替代 yargs-parser）
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

	// 1. 提取 URL
	const urlPatterns = [
		// 匹配引号包围的 URL
		/curl\s+["']([^"']+)["']/,
		// 匹配非引号的 URL (第一个非选项参数)
		/curl\s+([^\s-][^\s]*?)(?=\s+-|$)/,
	];

	for (const pattern of urlPatterns) {
		const urlMatch = curlCommand.match(pattern);
		if (urlMatch) {
			result.url = removeQuotes(urlMatch[1]);
			break;
		}
	}

	// 2. 提取 HTTP 方法
	const methodMatch = curlCommand.match(/-X\s+["']?([A-Z]+)["']?/i);
	if (methodMatch) {
		result.method = methodMatch[1].toUpperCase();
	}

	// 3. 提取所有头部信息
	const headerPattern = /-H\s+["']([^"']+)["']/g;
	let headerMatch;
	while ((headerMatch = headerPattern.exec(curlCommand)) !== null) {
		const headerStr = headerMatch[1];
		const colonIndex = headerStr.indexOf(':');
		if (colonIndex !== -1) {
			const key = headerStr.substring(0, colonIndex).trim();
			const value = headerStr.substring(colonIndex + 1).trim();
			result.headers[key] = value;
		}
	}

	// 4. 提取数据内容
	const dataPatterns = [
		/--data-raw\s+(.+?)(?=\s+-[a-zA-Z]|$)/s,
		/-d\s+(.+?)(?=\s+-[a-zA-Z]|$)/s,
		/--data\s+(.+?)(?=\s+-[a-zA-Z]|$)/s,
	];

	for (const pattern of dataPatterns) {
		const dataMatch = curlCommand.match(pattern);
		if (dataMatch) {
			result.data = dataMatch[1].trim();
			break;
		}
	}

	console.log('正则表达式解析结果:');
	console.log('URL:', result.url);
	console.log('Method:', result.method);
	console.log('Headers count:', Object.keys(result.headers).length);
	console.log('Data length:', result.data ? result.data.length : 'null');
	if (result.data) {
		console.log('Data preview:', result.data.substring(0, 100) + '...');
	}

	return result;
}

export function parseCurl(curlCommand: string): ParsedCurlConfig {
	try {
		// 清理curl命令，移除多余的空白字符
		const preprocessedCommand = preProcessCurlCommand(curlCommand);

		if (!preprocessedCommand) {
			throw new Error('cURL command cannot be empty');
		}

		// 使用基于正则表达式的解析器
		const parsed = parseCurlWithRegex(preprocessedCommand);

		// 解析 URL 和查询参数
		const urlObj = parsed.url ? new URL(parsed.url) : new URL('');
		const params: Record<string, string> = {};
		urlObj.searchParams.forEach((value, key) => {
			params[key] = value;
		});

		// 使用解析出的 headers
		const headers = parsed.headers;
		const rawContentType = headers['Content-Type'] || headers['content-type'] || '';

		// 使用解析出的 method
		const method = parsed.method;

		// 处理 body 数据
		let bodyType: ParsedCurlConfig['bodyType'] = 'none';
		let formData: Record<string, string> | undefined;
		let urlEncoded: Record<string, string> | undefined;
		let rawBody: string | undefined;
		let rawFormat = 'json';

		// 检查是否有数据
		if (parsed.data) {
			let rawData = parsed.data;

			// 清理数据：移除引号和不必要的转义字符
			rawData = removeQuotes(rawData);

			// 特殊处理 CMD JSON 格式
			if (rawData && typeof rawData === 'string') {
				// 检查是否是 CMD 格式的 JSON (包含 ^{ 或 ^[ 等)
				if (
					rawData.includes('^{') ||
					rawData.includes('^[') ||
					rawData.includes('^\\^"') ||
					rawData.includes('^"')
				) {
					console.log('检测到 CMD 格式，开始处理转义...');
					// 不需要添加额外的引号，直接处理
					rawData = processCmdJsonFormat(rawData);
				}
			}

			// 进一步清理JSON数据中的格式问题
			if (rawData && typeof rawData === 'string') {
				// 移除可能的BOM和其他隐藏字符
				rawData = rawData
					.replace(/^\uFEFF/, '') // 移除BOM
					// eslint-disable-next-line no-control-regex
					.replace(/[\x00-\x08\x0E-\x1F\x7F]/g, '') // 移除控制字符但保留\t\n\r
					.replace(/\u00A0/g, ' ') // 替换不间断空格为普通空格
					.trim();
			}

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
