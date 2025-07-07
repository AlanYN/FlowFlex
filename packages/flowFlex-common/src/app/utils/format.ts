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
