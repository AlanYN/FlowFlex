/**
 * @description: 数据类型检测工具
 * 用于智能识别ActionResultDialog中executionOutput的数据类型
 */

import { useGlobSetting } from '@/settings';

// 注意：如果后续需要将Base64转换为Blob，可以导入 dataURLtoBlob from './file'

// 输出类型定义
export interface OutputType {
	type: 'file' | 'json' | 'text';
	data: any;
	subType?: string;
	metadata?: FileMetadata;
}

// 文件下载信息接口
export interface FileDownloadInfo {
	fileHash?: string;
	fileName?: string;
	filePath?: string;
	fileSize?: number;
	accessUrl?: string;
	downloaded?: boolean;
	contentType?: string;
	originalFileName?: string;
}

// 文件元数据接口
export interface FileMetadata {
	fileName?: string;
	mimeType?: string;
	size?: number;
	extension?: string;
	// Action执行结果相关信息
	actionResult?: {
		success?: boolean;
		timestamp?: string;
		executionTime?: string;
		statusCode?: number;
	};
	// 文件下载信息
	fileDownload?: FileDownloadInfo;
}

// 文件数据结构接口
export interface FileStructure {
	fileName?: string;
	originalFileName?: string;
	contentType?: string;
	mimeType?: string;
	data?: string;
	content?: string;
	base64?: string;
	url?: string;
	size?: number;
	fileSize?: number;
	extension?: string;
	// 文件访问相关URL
	accessUrl?: string; // 原始的相对访问路径
	fullUrl?: string; // 拼接域名后的完整URL
	// 完整的文件下载信息（用于文件下载结果）
	fileDownloadInfo?: FileDownloadInfo;
}

/**
 * 根据文件扩展名获取标准MIME类型
 * 复用现有的getMimeType逻辑
 */
export const getMimeType = (fileExtension: string): string => {
	const mimeTypes = {
		// PDF文档
		pdf: 'application/pdf',
		// Microsoft Word文档
		doc: 'application/msword',
		docx: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
		// Microsoft Excel文档
		xls: 'application/vnd.ms-excel',
		xlsx: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
		// PowerPoint文档
		ppt: 'application/vnd.ms-powerpoint',
		pptx: 'application/vnd.openxmlformats-officedocument.presentationml.presentation',
		// 图片文件
		jpg: 'image/jpeg',
		jpeg: 'image/jpeg',
		png: 'image/png',
		gif: 'image/gif',
		bmp: 'image/bmp',
		webp: 'image/webp',
		svg: 'image/svg+xml',
		// 文本文件
		txt: 'text/plain',
		csv: 'text/csv',
		json: 'application/json',
		xml: 'application/xml',
		html: 'text/html',
		css: 'text/css',
		js: 'application/javascript',
		// 压缩文件
		zip: 'application/zip',
		rar: 'application/x-rar-compressed',
		'7z': 'application/x-7z-compressed',
		// 邮件文件
		msg: 'application/vnd.ms-outlook',
		eml: 'message/rfc822',
	} as const;

	return (
		mimeTypes[fileExtension.toLowerCase() as keyof typeof mimeTypes] ||
		'application/octet-stream'
	);
};

/**
 * 检查字符串是否为有效的JSON格式
 */
export const isValidJSON = (data: string): boolean => {
	if (!data || typeof data !== 'string') {
		return false;
	}

	// 去除首尾空白字符
	const trimmed = data.trim();

	// 基本格式检查
	if (!trimmed.startsWith('{') && !trimmed.startsWith('[')) {
		return false;
	}

	try {
		const parsed = JSON.parse(trimmed);
		return typeof parsed === 'object' && parsed !== null;
	} catch (error) {
		return false;
	}
};

/**
 * 检查解析后的对象是否包含文件结构特征
 */
export const hasFileStructure = (parsed: any): boolean => {
	if (!parsed || typeof parsed !== 'object') {
		return false;
	}

	// 检查是否包含fileDownload结构
	if (parsed.fileDownload && typeof parsed.fileDownload === 'object') {
		const fileDownload = parsed.fileDownload;
		// 验证fileDownload对象包含必要的文件信息
		return !!(fileDownload.fileName || fileDownload.originalFileName);
	}

	// 检查常见的文件数据字段组合
	const fileIndicators = [
		// 基本文件信息字段
		['fileName', 'data'],
		['fileName', 'content'],
		['fileName', 'base64'],
		['fileName', 'url'],
		['name', 'data'],
		['name', 'content'],
		['name', 'base64'],
		['name', 'url'],
		// 带类型信息的文件字段
		['fileName', 'contentType', 'data'],
		['fileName', 'mimeType', 'data'],
		['name', 'type', 'data'],
		// 文件上传响应格式
		['originalFileName', 'data'],
		['originalName', 'content'],
		// 文件下载格式
		['filename', 'blob'],
		['filename', 'buffer'],
	];

	// 检查是否匹配任何文件结构模式
	return fileIndicators.some((indicators) => indicators.every((field) => field in parsed));
};

/**
 * 检查是否为文件下载结果结构
 */
export const isFileDownloadResult = (parsed: any): boolean => {
	if (!parsed || typeof parsed !== 'object') {
		return false;
	}

	// 检查是否包含fileDownload字段且具有文件下载的基本结构
	if (parsed.fileDownload && typeof parsed.fileDownload === 'object') {
		const fileDownload = parsed.fileDownload;
		// 检查文件下载的关键字段
		const requiredFields = ['fileName', 'fileSize', 'contentType'];
		const hasRequiredFields = requiredFields.some((field) => field in fileDownload);

		// 同时需要有success和statusCode等HTTP响应特征
		const hasHttpStructure = 'success' in parsed && 'statusCode' in parsed;

		return hasRequiredFields && hasHttpStructure;
	}

	return false;
};

/**
 * 检查是否为Action执行结果结构（Python脚本、HTTP API等）
 */
export const isActionExecutionResult = (parsed: any): boolean => {
	if (!parsed || typeof parsed !== 'object') {
		return false;
	}

	// 优先检查文件下载结果
	if (isFileDownloadResult(parsed)) {
		return true;
	}

	// Python脚本执行结果特征
	const pythonResultIndicators = ['stdout', 'stderr', 'success', 'executionTime'];
	const hasPythonStructure = pythonResultIndicators.some((field) => field in parsed);

	// HTTP API响应结果特征
	const httpResultIndicators = ['response', 'statusCode', 'headers'];
	const hasHttpStructure = httpResultIndicators.some((field) => field in parsed);

	return hasPythonStructure || hasHttpStructure;
};

/**
 * 构建完整的文件访问URL
 */
export const buildFullFileUrl = (accessUrl: string): string => {
	if (!accessUrl) {
		return '';
	}

	// 如果已经是完整的URL（包含协议），直接返回
	if (accessUrl.startsWith('http://') || accessUrl.startsWith('https://')) {
		return accessUrl;
	}

	try {
		const globSetting = useGlobSetting();
		const domainUrl = globSetting.domainUrl;

		if (!domainUrl) {
			return accessUrl;
		}

		// 确保domainUrl不以斜杠结尾，accessUrl以斜杠开头
		const cleanDomain = domainUrl.replace(/\/$/, '');
		const cleanPath = accessUrl.startsWith('/') ? accessUrl : `/${accessUrl}`;

		return `${cleanDomain}${cleanPath}`;
	} catch (error) {
		console.warn('Failed to build full file URL:', error);
		return accessUrl;
	}
};

/**
 * 从文件下载结果中提取文件信息
 */
export const extractFileFromDownloadResult = (parsed: any): FileStructure | null => {
	if (!isFileDownloadResult(parsed)) {
		return null;
	}

	const fileDownload = parsed.fileDownload;
	const fileName = fileDownload.fileName || fileDownload.originalFileName || 'downloaded-file';

	// 优先使用接口返回的 contentType
	const contentType = fileDownload.contentType || 'application/octet-stream';

	// 从文件名提取扩展名，如果没有则根据 contentType 推断
	const extension = fileName.includes('.')
		? fileName.split('.').pop()?.toLowerCase() || ''
		: getExtensionFromMimeType(contentType);

	// 构建完整的文件访问URL
	const fullUrl = buildFullFileUrl(fileDownload.accessUrl || '');

	return {
		fileName,
		originalFileName: fileDownload.originalFileName,
		extension,
		mimeType: contentType,
		contentType: contentType,
		size: fileDownload.fileSize,
		url: fullUrl,
		// 保存原始的访问路径和完整URL
		accessUrl: fileDownload.accessUrl,
		fullUrl,
		// 保存完整的下载信息
		fileDownloadInfo: fileDownload,
	};
};

/**
 * 从Action执行结果中提取实际的数据内容
 */
export const extractDataFromActionResult = (parsed: any): string => {
	if (!parsed || typeof parsed !== 'object') {
		return '';
	}

	// 优先检查文件下载结果
	if (isFileDownloadResult(parsed)) {
		// 对于文件下载结果，返回response中的描述信息
		if (parsed.response && typeof parsed.response === 'string') {
			return parsed.response.trim();
		}
		return `File downloaded: ${parsed.fileDownload?.fileName || 'unknown'}`;
	}

	// 从Python脚本结果中提取stdout内容
	if (parsed.stdout && typeof parsed.stdout === 'string') {
		return parsed.stdout.trim();
	}

	// 从HTTP API响应中提取response内容
	if (parsed.response && typeof parsed.response === 'string') {
		return parsed.response.trim();
	}

	// 如果有其他可能的数据字段
	if (parsed.data && typeof parsed.data === 'string') {
		return parsed.data.trim();
	}

	if (parsed.result && typeof parsed.result === 'string') {
		return parsed.result.trim();
	}

	return '';
};

/**
 * 检查字符串是否为Base64 Data URL格式
 */
export const isBase64DataURL = (data: string): boolean => {
	if (!data || typeof data !== 'string') {
		return false;
	}

	// Data URL格式: data:[<mediatype>][;base64],<data>
	const dataURLPattern = /^data:([a-zA-Z0-9][a-zA-Z0-9/+-]*);base64,([A-Za-z0-9+/=]+)$/;
	return dataURLPattern.test(data.trim());
};

/**
 * 检查字符串是否为文件URL格式
 */
export const isFileURL = (data: string): boolean => {
	if (!data || typeof data !== 'string') {
		return false;
	}

	try {
		const url = new URL(data.trim());

		// 检查协议
		if (!['http:', 'https:', 'blob:', 'file:'].includes(url.protocol)) {
			return false;
		}

		// 检查路径是否包含文件扩展名
		const pathname = url.pathname.toLowerCase();
		const fileExtensions = [
			'.pdf',
			'.doc',
			'.docx',
			'.xls',
			'.xlsx',
			'.ppt',
			'.pptx',
			'.jpg',
			'.jpeg',
			'.png',
			'.gif',
			'.bmp',
			'.webp',
			'.svg',
			'.txt',
			'.csv',
			'.json',
			'.xml',
			'.html',
			'.css',
			'.js',
			'.zip',
			'.rar',
			'.7z',
			'.msg',
			'.eml',
		];

		return fileExtensions.some((ext) => pathname.includes(ext));
	} catch (error) {
		return false;
	}
};

/**
 * 解析Base64 Data URL，提取文件信息
 */
export const parseBase64File = (dataURL: string): FileStructure => {
	try {
		const matches = dataURL.match(
			/^data:([a-zA-Z0-9][a-zA-Z0-9/+-]*);base64,([A-Za-z0-9+/=]+)$/
		);

		if (!matches) {
			throw new Error('Invalid Base64 Data URL format');
		}

		const mimeType = matches[1];
		const base64Data = matches[2];

		// 根据MIME类型推断文件扩展名
		const extension = getExtensionFromMimeType(mimeType);

		// 计算文件大小（Base64编码后的大小约为原文件的4/3）
		const estimatedSize = Math.floor((base64Data.length * 3) / 4);

		return {
			data: dataURL,
			base64: base64Data,
			mimeType,
			contentType: mimeType,
			extension,
			fileName: `file.${extension}`,
			size: estimatedSize,
		};
	} catch (error) {
		console.error('Failed to parse Base64 file:', error);
		return {
			data: dataURL,
			mimeType: 'application/octet-stream',
			extension: 'bin',
			fileName: 'file.bin',
		};
	}
};

/**
 * 根据MIME类型获取文件扩展名
 */
export const getExtensionFromMimeType = (mimeType: string): string => {
	const mimeToExtension: Record<string, string> = {
		'application/pdf': 'pdf',
		'application/msword': 'doc',
		'application/vnd.openxmlformats-officedocument.wordprocessingml.document': 'docx',
		'application/vnd.ms-excel': 'xls',
		'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet': 'xlsx',
		'application/vnd.ms-powerpoint': 'ppt',
		'application/vnd.openxmlformats-officedocument.presentationml.presentation': 'pptx',
		'image/jpeg': 'jpg',
		'image/png': 'png',
		'image/gif': 'gif',
		'image/bmp': 'bmp',
		'image/webp': 'webp',
		'image/svg+xml': 'svg',
		'text/plain': 'txt',
		'text/csv': 'csv',
		'application/json': 'json',
		'application/xml': 'xml',
		'text/html': 'html',
		'text/css': 'css',
		'application/javascript': 'js',
		'application/zip': 'zip',
		'application/x-rar-compressed': 'rar',
		'application/x-7z-compressed': '7z',
		'application/vnd.ms-outlook': 'msg',
		'message/rfc822': 'eml',
	};

	return mimeToExtension[mimeType] || 'bin';
};

/**
 * 解析文件URL，提取文件信息
 */
export const parseFileURL = (url: string): FileStructure => {
	try {
		const urlObj = new URL(url);
		const pathname = urlObj.pathname;

		// 提取文件名和扩展名
		const fileName = pathname.split('/').pop() || 'file';
		const extension = fileName.split('.').pop()?.toLowerCase() || '';
		const mimeType = getMimeType(extension);

		return {
			url,
			fileName,
			extension,
			mimeType,
			contentType: mimeType,
		};
	} catch (error) {
		console.error('Failed to parse file URL:', error);
		return {
			url,
			fileName: 'file',
			extension: '',
			mimeType: 'application/octet-stream',
		};
	}
};

/**
 * 主要的类型检测函数
 * 智能识别executionOutput中的数据类型
 */
export const detectOutputType = (executionOutput: string): OutputType => {
	// 输入验证
	if (!executionOutput || typeof executionOutput !== 'string') {
		return {
			type: 'text',
			data: executionOutput || '',
		};
	}

	const trimmed = executionOutput.trim();

	// 1. 检测Base64 Data URL格式
	if (isBase64DataURL(trimmed)) {
		const fileData = parseBase64File(trimmed);
		return {
			type: 'file',
			data: fileData,
			subType: 'base64',
			metadata: {
				fileName: fileData.fileName,
				mimeType: fileData.mimeType,
				size: fileData.size,
				extension: fileData.extension,
			},
		};
	}

	// 2. 检测文件URL格式
	if (isFileURL(trimmed)) {
		const fileData = parseFileURL(trimmed);
		return {
			type: 'file',
			data: fileData,
			subType: 'url',
			metadata: {
				fileName: fileData.fileName,
				mimeType: fileData.mimeType,
				extension: fileData.extension,
			},
		};
	}

	// 3. 检测JSON格式
	if (isValidJSON(trimmed)) {
		try {
			const parsed = JSON.parse(trimmed);

			// 检查是否为Action执行结果结构
			if (isActionExecutionResult(parsed)) {
				// 优先检查文件下载结果
				if (isFileDownloadResult(parsed)) {
					const fileData = extractFileFromDownloadResult(parsed);
					if (fileData) {
						return {
							type: 'file',
							data: fileData,
							subType: 'url',
							metadata: {
								fileName: fileData.fileName,
								mimeType: fileData.mimeType,
								size: fileData.size,
								extension: fileData.extension,
								// 保留Action执行信息
								actionResult: {
									success: parsed.success,
									timestamp: parsed.timestamp,
									executionTime: parsed.executionTime,
									statusCode: parsed.statusCode,
								},
								// 保留文件下载信息
								fileDownload: parsed.fileDownload,
							},
						};
					}
				}

				// 处理其他Action执行结果
				const extractedData = extractDataFromActionResult(parsed);

				if (extractedData) {
					// 递归检测提取出的数据类型
					const innerResult = detectOutputType(extractedData);

					// 如果提取的数据是文件或有意义的结构化数据，返回它
					if (
						innerResult.type === 'file' ||
						(innerResult.type === 'json' && extractedData !== trimmed)
					) {
						return {
							...innerResult,
							subType: `action-${innerResult.subType || innerResult.type}`,
							metadata: {
								...innerResult.metadata,
								// 保留原始Action执行信息
								actionResult: {
									success: parsed.success,
									timestamp: parsed.timestamp,
									executionTime: parsed.executionTime,
									statusCode: parsed.statusCode,
								},
							},
						};
					}
				}

				// 如果提取不到有意义的数据，返回原始的Action结果作为JSON
				return {
					type: 'json',
					data: parsed,
					subType: isFileDownloadResult(parsed)
						? 'file-download-result'
						: 'action-result',
				};
			}

			// 检查是否为包含文件信息的JSON结构
			if (hasFileStructure(parsed)) {
				// 处理fileDownload结构（备用处理，通常在Action执行结果中已处理）
				if (parsed.fileDownload && typeof parsed.fileDownload === 'object') {
					const fileData = extractFileFromDownloadResult(parsed);
					if (fileData) {
						return {
							type: 'file',
							data: fileData,
							subType: 'url',
							metadata: {
								fileName: fileData.fileName,
								mimeType: fileData.mimeType,
								size: fileData.size,
								extension: fileData.extension,
								// 保留Action执行信息（如果有的话）
								actionResult:
									parsed.success !== undefined
										? {
												success: parsed.success,
												timestamp: parsed.timestamp,
												statusCode: parsed.statusCode,
										  }
										: undefined,
								// 保留文件下载信息
								fileDownload: parsed.fileDownload,
							},
						};
					}
				}

				// 处理其他文件结构
				const fileName =
					parsed.fileName || parsed.name || parsed.originalFileName || 'file';
				const mimeType = parsed.contentType || parsed.mimeType || parsed.type;
				const size = parsed.size || parsed.fileSize;
				const extension = fileName.includes('.')
					? fileName.split('.').pop()?.toLowerCase()
					: getExtensionFromMimeType(mimeType || '');

				return {
					type: 'file',
					data: parsed,
					subType: 'structured',
					metadata: {
						fileName,
						mimeType: mimeType || getMimeType(extension || ''),
						size,
						extension,
					},
				};
			}

			// 普通JSON数据
			return {
				type: 'json',
				data: parsed,
			};
		} catch (error) {
			console.error('JSON parsing error:', error);
		}
	}

	// 4. 默认为文本类型
	return {
		type: 'text',
		data: trimmed,
	};
};

/**
 * 批量检测多个输出的类型
 */
export const detectMultipleOutputTypes = (outputs: string[]): OutputType[] => {
	return outputs.map((output) => detectOutputType(output));
};

/**
 * 检查输出类型是否为文件类型
 */
export const isFileType = (outputType: OutputType): boolean => {
	return outputType.type === 'file';
};

/**
 * 检查输出类型是否为JSON类型
 */
export const isJSONType = (outputType: OutputType): boolean => {
	return outputType.type === 'json';
};

/**
 * 检查输出类型是否为文本类型
 */
export const isTextType = (outputType: OutputType): boolean => {
	return outputType.type === 'text';
};

/**
 * 获取文件类型的显示名称
 */
export const getFileTypeDisplayName = (outputType: OutputType): string => {
	if (!isFileType(outputType)) {
		return '';
	}

	const extension = outputType.metadata?.extension?.toUpperCase();
	const mimeType = outputType.metadata?.mimeType;

	if (extension) {
		return `${extension} File`;
	}

	if (mimeType) {
		if (mimeType.startsWith('image/')) return 'Image';
		if (mimeType.startsWith('video/')) return 'Video';
		if (mimeType.startsWith('audio/')) return 'Audio';
		if (mimeType.includes('pdf')) return 'PDF Document';
		if (mimeType.includes('word')) return 'Word Document';
		if (mimeType.includes('excel') || mimeType.includes('spreadsheet'))
			return 'Excel Spreadsheet';
		if (mimeType.includes('powerpoint') || mimeType.includes('presentation'))
			return 'PowerPoint Presentation';
	}

	return 'File';
};

/**
 * 检查输出类型是否为文件下载类型
 */
export const isFileDownloadType = (outputType: OutputType): boolean => {
	return (
		(outputType.type === 'file' &&
			outputType.subType === 'url' &&
			!!outputType.metadata?.fileDownload) ||
		outputType.subType === 'file-download-result'
	);
};

/**
 * 检查输出类型是否为Action执行结果
 */
export const isActionResultType = (outputType: OutputType): boolean => {
	return (
		outputType.subType === 'action-result' ||
		outputType.subType === 'file-download-result' ||
		outputType.subType?.startsWith('action-') === true
	);
};

/**
 * 获取文件下载结果的摘要信息
 */
export const getFileDownloadSummary = (outputType: OutputType): string => {
	if (!isFileDownloadType(outputType) || !outputType.metadata?.fileDownload) {
		return '';
	}

	const fileDownload = outputType.metadata.fileDownload;
	const parts: string[] = [];

	if (fileDownload.fileName) {
		parts.push(`File: ${fileDownload.fileName}`);
	}

	if (fileDownload.fileSize) {
		const sizeInKB = Math.round(fileDownload.fileSize / 1024);
		parts.push(`Size: ${sizeInKB > 0 ? sizeInKB + ' KB' : fileDownload.fileSize + ' bytes'}`);
	}

	if (fileDownload.contentType) {
		parts.push(`Type: ${fileDownload.contentType}`);
	}

	if (fileDownload.downloaded !== undefined) {
		parts.push(`Downloaded: ${fileDownload.downloaded ? 'Yes' : 'No'}`);
	}

	return parts.join(' | ');
};

/**
 * 获取Action执行结果的摘要信息
 */
export const getActionResultSummary = (outputType: OutputType): string => {
	if (!isActionResultType(outputType) || !outputType.metadata?.actionResult) {
		return '';
	}

	const actionResult = outputType.metadata.actionResult;
	const parts: string[] = [];

	if (actionResult.success !== undefined) {
		parts.push(`Status: ${actionResult.success ? 'Success' : 'Failed'}`);
	}

	if (actionResult.executionTime) {
		parts.push(`Time: ${actionResult.executionTime}s`);
	}

	if (actionResult.statusCode) {
		parts.push(`Code: ${actionResult.statusCode}`);
	}

	return parts.join(' | ');
};

/**
 * 获取输出类型的显示标题
 */
export const getOutputTypeDisplayTitle = (outputType: OutputType): string => {
	switch (outputType.type) {
		case 'file':
			if (isFileDownloadType(outputType)) {
				return 'Downloaded File';
			}
			return getFileTypeDisplayName(outputType);
		case 'json':
			if (outputType.subType === 'action-result') {
				return 'Action Result';
			}
			if (outputType.subType === 'file-download-result') {
				return 'File Download Result';
			}
			return 'JSON Data';
		case 'text':
			return 'Text';
		default:
			return 'Unknown';
	}
};
