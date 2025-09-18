/**
 * JSON语法高亮工具
 * 提供JSON字符串的语法高亮功能，支持不同数据类型的颜色区分
 */

/**
 * 转义HTML特殊字符
 * @param text 需要转义的文本
 * @returns 转义后的文本
 */
const escapeHtml = (text: string): string => {
	const div = document.createElement('div');
	div.textContent = text;
	return div.innerHTML;
};

/**
 * 应用JSON语法高亮
 * @param jsonString JSON字符串
 * @returns 带有HTML标签的高亮字符串
 */
export const applySyntaxHighlight = (jsonString: string): string => {
	// 转义HTML特殊字符
	let highlighted = escapeHtml(jsonString);

	// 使用更精确的方法来避免字符串内部内容被误识别
	// 我们需要先标记所有的字符串，然后再处理其他类型

	// 1. 首先标记所有的字符串（包括键名和值），使用特殊标记避免后续处理
	const stringMarker = '___STRING_PLACEHOLDER___';
	const strings: string[] = [];
	let stringIndex = 0;

	// 匹配所有引号字符串并替换为占位符
	highlighted = highlighted.replace(/"([^"\\]*(\\.[^"\\]*)*)"/g, (match) => {
		strings.push(match);
		return `${stringMarker}${stringIndex++}${stringMarker}`;
	});

	// 2. 处理各种数据类型的值
	const processValueType = (pattern: string, className: string) => {
		// 处理对象中的值
		highlighted = highlighted.replace(
			new RegExp(`:\\s*(${pattern})`, 'g'),
			`: <span class="${className}">$1</span>`
		);
		// 处理数组中的值
		highlighted = highlighted.replace(
			new RegExp(`(\\[|,)\\s*(${pattern})`, 'g'),
			`$1 <span class="${className}">$2</span>`
		);
	};

	// 处理数字值
	processValueType('-?\\d+(?:\\.\\d+)?(?:[eE][+-]?\\d+)?', 'json-number');
	// 处理布尔值
	processValueType('true|false', 'json-boolean');
	// 处理null值
	processValueType('null', 'json-null');

	// 8. 处理括号和分隔符
	highlighted = highlighted.replace(/([{}[\],])/g, '<span class="json-punctuation">$1</span>');

	// 9. 最后恢复字符串并应用正确的样式
	for (let i = 0; i < strings.length; i++) {
		const placeholder = `${stringMarker}${i}${stringMarker}`;
		const keyWithColon = `${placeholder}:`;
		const keyWithSpaceColon = `${placeholder} :`;

		// 先检查是否是键名（后面直接跟冒号）
		if (highlighted.includes(keyWithColon)) {
			highlighted = highlighted.replace(
				keyWithColon,
				`<span class="json-key">${strings[i]}</span>:`
			);
		} else if (highlighted.includes(keyWithSpaceColon)) {
			highlighted = highlighted.replace(
				keyWithSpaceColon,
				`<span class="json-key">${strings[i]}</span> :`
			);
		} else if (highlighted.includes(placeholder)) {
			// 如果不是键名，则作为字符串值处理
			highlighted = highlighted.replace(
				placeholder,
				`<span class="json-string">${strings[i]}</span>`
			);
		}
	}

	return highlighted;
};

/**
 * 格式化JSON字符串
 * @param jsonString JSON字符串
 * @param indent 缩进空格数，默认为2
 * @returns 格式化后的JSON字符串
 */
export const formatJsonString = (jsonString: string, indent: number = 2): string => {
	try {
		const parsed = JSON.parse(jsonString);
		return JSON.stringify(parsed, null, indent);
	} catch (error) {
		return jsonString;
	}
};

/**
 * 验证JSON字符串是否有效
 * @param jsonString JSON字符串
 * @returns 是否为有效的JSON
 */
export const isValidJson = (jsonString: string): boolean => {
	try {
		JSON.parse(jsonString);
		return true;
	} catch {
		return false;
	}
};
