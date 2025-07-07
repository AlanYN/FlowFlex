/**
 * 生成唯一ID
 * @returns string 返回一个唯一的字符串ID
 */
export function generateUniqueId(): string {
	return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
		const r = (Math.random() * 16) | 0;
		const v = c === 'x' ? r : (r & 0x3) | 0x8;
		return v.toString(16);
	});
}

/**
 * 生成简短的唯一ID
 * @returns string 返回一个简短的唯一字符串ID
 */
export function generateShortId(): string {
	return Math.random().toString(36).substring(2) + Date.now().toString(36);
}
