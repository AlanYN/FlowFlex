// 格式化日期
export function formatDate(dateString: string): string {
	if (!dateString) return '';

	try {
		const date = new Date(dateString);
		if (isNaN(date.getTime())) return dateString;

		return new Intl.DateTimeFormat('zh-CN', {
			year: 'numeric',
			month: '2-digit',
			day: '2-digit',
			hour: '2-digit',
			minute: '2-digit',
		}).format(date);
	} catch (error) {
		console.error('Date format error:', error);
		return dateString;
	}
}

// 工具函数
export const formatFileSize = (bytes: number | string): string => {
	if (!bytes) return '0 Bytes';

	const k = 1024;
	const sizes = ['Bytes', 'KB', 'MB', 'GB'];
	const i = Math.floor(Math.log(+bytes) / Math.log(k));

	return parseFloat((+bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
};

// 根据文件扩展名获取标准MIME类型
export const getMimeType = (fileExtension: string) => {
	const mimeTypes = {
		// PDF文档
		pdf: 'application/pdf',
		// Microsoft Word文档
		doc: 'application/msword',
		docx: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
		// Microsoft Excel文档
		xls: 'application/vnd.ms-excel',
		xlsx: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
		// 图片文件
		jpg: 'image/jpeg',
		jpeg: 'image/jpeg',
		png: 'image/png',
		// 邮件文件
		msg: 'application/vnd.ms-outlook',
		eml: 'message/rfc822',
	} as const;

	return (
		mimeTypes[fileExtension.toLowerCase() as keyof typeof mimeTypes] ||
		'application/octet-stream'
	);
};
