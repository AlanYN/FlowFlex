/**
 * 邮件暗黑模式适配工具
 * 通过直接修改 HTML 内容实现暗黑模式适配
 */

import darkreaderSource from 'darkreader/darkreader.js?raw';

interface RGB {
	r: number;
	g: number;
	b: number;
	a?: number;
}

/**
 * 解析颜色字符串为 RGB
 */
function parseColor(color: string): RGB | null {
	if (!color) return null;

	color = color.trim().toLowerCase();

	// 处理 hex 格式
	if (color.startsWith('#')) {
		const hex = color.replace('#', '');
		if (hex.length === 3) {
			return {
				r: parseInt(hex[0] + hex[0], 16),
				g: parseInt(hex[1] + hex[1], 16),
				b: parseInt(hex[2] + hex[2], 16),
			};
		}
		if (hex.length === 6) {
			return {
				r: parseInt(hex.substring(0, 2), 16),
				g: parseInt(hex.substring(2, 4), 16),
				b: parseInt(hex.substring(4, 6), 16),
			};
		}
	}

	// 处理 rgb/rgba 格式
	const rgbMatch = color.match(
		/rgba?\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*(?:,\s*([\d.]+))?\s*\)/
	);
	if (rgbMatch) {
		const rgb: RGB = {
			r: parseInt(rgbMatch[1]),
			g: parseInt(rgbMatch[2]),
			b: parseInt(rgbMatch[3]),
		};
		if (rgbMatch[4] !== undefined) {
			rgb.a = parseFloat(rgbMatch[4]);
		}
		return rgb;
	}

	// 处理命名颜色
	const namedColors: Record<string, RGB> = {
		white: { r: 255, g: 255, b: 255 },
		black: { r: 0, g: 0, b: 0 },
		red: { r: 255, g: 0, b: 0 },
		green: { r: 0, g: 128, b: 0 },
		blue: { r: 0, g: 0, b: 255 },
		gray: { r: 128, g: 128, b: 128 },
		grey: { r: 128, g: 128, b: 128 },
		silver: { r: 192, g: 192, b: 192 },
		transparent: { r: 0, g: 0, b: 0, a: 0 },
	};

	return namedColors[color] || null;
}

/**
 * 计算亮度
 */
function getLuminance(rgb: RGB): number {
	return (0.299 * rgb.r + 0.587 * rgb.g + 0.114 * rgb.b) / 255;
}

/**
 * 判断是否为浅色
 */
function isLightColor(rgb: RGB): boolean {
	return getLuminance(rgb) > 0.5;
}

/**
 * 判断是否为深色
 */
function isDarkColor(rgb: RGB): boolean {
	return getLuminance(rgb) < 0.3;
}

/**
 * 判断是否为高饱和度彩色（品牌色）
 */
function isColorful(rgb: RGB): boolean {
	const max = Math.max(rgb.r, rgb.g, rgb.b);
	const min = Math.min(rgb.r, rgb.g, rgb.b);
	const diff = max - min;
	// 色差大于 60 认为是彩色
	return diff > 60;
}

/**
 * 转换颜色到暗色模式
 */
function convertColorToDark(color: string, isBackground: boolean): string | null {
	const rgb = parseColor(color);
	if (!rgb) return null;

	// 透明色不处理
	if (rgb.a === 0) return null;

	// 高饱和度彩色保持不变（品牌色、按钮色等）
	if (isColorful(rgb)) {
		return null;
	}

	// 背景色处理
	if (isBackground) {
		if (isLightColor(rgb)) {
			// 浅色背景 -> 深色背景
			return '#1e1e1e';
		}
		// 深色背景保持不变
		return null;
	}

	// 文本色处理
	if (isDarkColor(rgb)) {
		// 深色文本 -> 浅色文本
		return '#e5e5e5';
	}

	// 浅色文本保持不变
	return null;
}

/**
 * 处理元素的内联样式
 */
function processElementStyle(element: HTMLElement) {
	const style = element.style;

	// 处理背景色
	if (style.backgroundColor) {
		const converted = convertColorToDark(style.backgroundColor, true);
		if (converted) {
			style.backgroundColor = converted;
		}
	}

	// 处理 background 简写属性中的颜色
	if (
		style.background &&
		!style.background.includes('url(') &&
		!style.background.includes('gradient')
	) {
		// 尝试提取颜色
		const colorMatch = style.background.match(/(#[0-9a-fA-F]{3,6}|rgba?\([^)]+\)|[a-z]+)/);
		if (colorMatch) {
			const converted = convertColorToDark(colorMatch[0], true);
			if (converted) {
				style.background = style.background.replace(colorMatch[0], converted);
			}
		}
	}

	// 处理文本色
	if (style.color) {
		const converted = convertColorToDark(style.color, false);
		if (converted) {
			style.color = converted;
		}
	}

	// 处理边框色
	if (style.borderColor) {
		const converted = convertColorToDark(style.borderColor, false);
		if (converted) {
			style.borderColor = converted;
		}
	}
}

/**
 * 处理 HTML 属性
 */
function processElementAttributes(element: HTMLElement) {
	// 处理 bgcolor 属性
	const bgcolor = element.getAttribute('bgcolor');
	if (bgcolor) {
		const converted = convertColorToDark(bgcolor, true);
		if (converted) {
			element.style.backgroundColor = converted;
			element.removeAttribute('bgcolor');
		}
	}

	// 处理 <font> 标签的 color 属性
	if (element.tagName === 'FONT') {
		const color = element.getAttribute('color');
		if (color) {
			const converted = convertColorToDark(color, false);
			if (converted) {
				element.style.color = converted;
				element.removeAttribute('color');
			}
		}
	}

	// 处理超链接
	if (element.tagName === 'A') {
		element.setAttribute('target', '_blank');
		element.setAttribute('rel', 'noopener noreferrer');
		// 设置链接颜色
		if (!element.style.color) {
			element.style.color = '#60a5fa';
		}
	}
}

/**
 * 递归处理 DOM 树
 */
function processDomTree(node: Node) {
	if (node.nodeType === Node.ELEMENT_NODE) {
		const element = node as HTMLElement;

		// 处理样式和属性
		processElementStyle(element);
		processElementAttributes(element);

		// 递归处理子节点
		Array.from(element.childNodes).forEach((child) => {
			processDomTree(child);
		});
	}
}

/**
 * 处理邮件 HTML 内容
 * @param htmlContent 原始 HTML 内容
 * @param isDark 是否为暗色模式
 * @returns 处理后的 HTML 内容（包含作用域样式和唯一类名）
 */
export function processEmailHtml(htmlContent: string, isDark: boolean): string {
	if (!htmlContent) return '';

	// 1. 将 style 标签中的样式内联化
	// const inlinedHtml = inlineStyles(htmlContent);

	// 2. 使用 DOMPurify 清理 HTML，防止 XSS 攻击
	// const cleanHtml = DOMPurify.sanitize(htmlContent, DOMPURIFY_CONFIG);

	// 3. 解析清理后的 HTML
	const parserHtml = htmlContent;

	// 4. 解析清理后的 HTML
	const parser = new DOMParser();
	const doc = parser.parseFromString(parserHtml, 'text/html');

	if (isDark) {
		// 处理 html 标签
		const htmlElement = doc.documentElement as HTMLElement;
		if (htmlElement && htmlElement.style.backgroundColor) {
			const converted = convertColorToDark(htmlElement.style.backgroundColor, true);
			if (converted) {
				htmlElement.style.backgroundColor = converted;
			} else {
				htmlElement.style.backgroundColor = 'transparent';
			}
		}

		// 处理 body 标签
		if (doc.body.style.backgroundColor) {
			const converted = convertColorToDark(doc.body.style.backgroundColor, true);
			if (converted) {
				doc.body.style.backgroundColor = converted;
			} else {
				doc.body.style.backgroundColor = 'transparent';
			}
		}

		// 处理所有子元素
		processDomTree(doc.body);
	}

	// 5. 处理超链接（无论是否暗色模式）
	const links = doc.querySelectorAll('a');
	links.forEach((link) => {
		link.setAttribute('target', '_blank');
		link.setAttribute('rel', 'noopener noreferrer');
		if (isDark && !link.style.color) {
			link.style.color = '#60a5fa';
		}
	});

	// 6. 返回处理后的 HTML
	// 如果原始 HTML 包含完整的 html 结构，返回完整的 HTML（包括 head 和 style）
	if (htmlContent.includes('<html') && htmlContent.includes('<head')) {
		// 创建完整的 HTML 字符串
		const headContent = doc.head.innerHTML;
		const bodyContent = doc.body.innerHTML;
		return `<html${
			doc.documentElement.getAttribute('lang')
				? ` lang="${doc.documentElement.getAttribute('lang')}"`
				: ''
		}><head>${headContent}</head><body>${bodyContent}</body></html>`;
	}

	// 否则只返回 body 内容
	return doc.body.innerHTML;
}

/**
 * 转换 CSS 规则中的颜色值（用于处理 <style> 标签）
 * @param cssText CSS 文本
 * @returns 转换后的 CSS 文本
 */
function convertCssColorsToDark(cssText: string): string {
	// 匹配颜色值的正则表达式
	const colorPatterns = [
		// background-color 和 background 属性
		{
			pattern:
				/(background(?:-color)?)\s*:\s*(#[0-9a-fA-F]{3,6}|rgba?\([^)]+\)|white|black|gray|grey|silver)/gi,
			isBackground: true,
		},
		// color 属性（排除 background-color）
		{
			pattern:
				/(?<!background-)color\s*:\s*(#[0-9a-fA-F]{3,6}|rgba?\([^)]+\)|white|black|gray|grey|silver)/gi,
			isBackground: false,
		},
		// border-color 属性
		{
			pattern:
				/border(?:-[a-z]+)?-color\s*:\s*(#[0-9a-fA-F]{3,6}|rgba?\([^)]+\)|white|black|gray|grey|silver)/gi,
			isBackground: false,
		},
	];

	let result = cssText;

	colorPatterns.forEach(({ pattern, isBackground }) => {
		result = result.replace(pattern, (match, propOrColor, colorValue) => {
			// 根据匹配情况确定颜色值
			const color = colorValue || propOrColor;
			const converted = convertColorToDark(color, isBackground);
			if (converted) {
				return match.replace(color, converted);
			}
			return match;
		});
	});

	return result;
}

/**
 * 处理 <style> 标签中的 CSS 规则
 * @param iframeDoc iframe 的 document 对象
 */
function processStyleTags(iframeDoc: Document) {
	const styleTags = iframeDoc.querySelectorAll('style');
	styleTags.forEach((styleTag) => {
		if (styleTag.textContent) {
			styleTag.textContent = convertCssColorsToDark(styleTag.textContent);
		}
	});
}

/**
 * 直接处理 iframe 文档
 * @param iframeDoc iframe 的 document 对象
 * @param isDark 是否为暗黑模式
 */
export function applyEmailDarkModeToIframe(iframeDoc: Document, isDark: boolean) {
	if (!iframeDoc || !iframeDoc.body) return;

	if (isDark) {
		// 1. 首先处理 <style> 标签中的 CSS 规则（这是关键！）
		processStyleTags(iframeDoc);

		// 2. 处理 html 标签的背景色
		const htmlElement = iframeDoc.documentElement as HTMLElement;
		if (htmlElement) {
			// 获取计算后的背景色（包括从 CSS 规则继承的）
			const computedStyle = iframeDoc.defaultView?.getComputedStyle(htmlElement);
			const bgColor = computedStyle?.backgroundColor || htmlElement.style.backgroundColor;
			if (bgColor && bgColor !== 'transparent' && bgColor !== 'rgba(0, 0, 0, 0)') {
				const converted = convertColorToDark(bgColor, true);
				if (converted) {
					htmlElement.style.backgroundColor = converted;
				} else {
					htmlElement.style.backgroundColor = 'transparent';
				}
			} else {
				htmlElement.style.backgroundColor = 'transparent';
			}
		}

		// 3. 处理 body 的背景色（使用计算后的样式）
		const bodyComputedStyle = iframeDoc.defaultView?.getComputedStyle(iframeDoc.body);
		const bodyBgColor =
			bodyComputedStyle?.backgroundColor || iframeDoc.body.style.backgroundColor;
		if (bodyBgColor && bodyBgColor !== 'transparent' && bodyBgColor !== 'rgba(0, 0, 0, 0)') {
			const converted = convertColorToDark(bodyBgColor, true);
			if (converted) {
				iframeDoc.body.style.backgroundColor = converted;
			} else {
				iframeDoc.body.style.backgroundColor = 'transparent';
			}
		} else {
			iframeDoc.body.style.backgroundColor = 'transparent';
		}

		// 4. 设置默认文本颜色
		iframeDoc.body.style.color = '#e5e5e5';

		// 5. 处理所有子元素的内联样式和 HTML 属性
		processDomTree(iframeDoc.body);

		// 6. 处理通过 CSS 规则设置背景色的元素（使用计算样式）
		processComputedStyles(iframeDoc);
	}
}

/**
 * 处理通过 CSS 规则设置样式的元素（使用计算样式）
 * @param iframeDoc iframe 的 document 对象
 */
function processComputedStyles(iframeDoc: Document) {
	// 获取所有元素
	const allElements = iframeDoc.body.querySelectorAll('*');

	allElements.forEach((el) => {
		const element = el as HTMLElement;
		const computedStyle = iframeDoc.defaultView?.getComputedStyle(element);
		if (!computedStyle) return;

		// 处理背景色（只有当元素没有内联样式时才使用计算样式）
		if (!element.style.backgroundColor) {
			const bgColor = computedStyle.backgroundColor;
			if (bgColor && bgColor !== 'transparent' && bgColor !== 'rgba(0, 0, 0, 0)') {
				const converted = convertColorToDark(bgColor, true);
				if (converted) {
					element.style.backgroundColor = converted;
				}
			}
		}

		// 处理文本颜色
		if (!element.style.color) {
			const color = computedStyle.color;
			if (color) {
				const converted = convertColorToDark(color, false);
				if (converted) {
					element.style.color = converted;
				}
			}
		}
	});
}

/**
 * 调整 iframe 高度以适应内容
 */
export function adjustIframeHeight(iframe: HTMLIFrameElement): void {
	const iframeDoc = iframe.contentDocument || iframe.contentWindow?.document;
	if (!iframeDoc || !iframeDoc.body) return;

	// 使用 requestAnimationFrame 确保 DOM 已渲染
	requestAnimationFrame(() => {
		// 使用多种方式获取高度，取最大值
		const bodyHeight = iframeDoc.body.scrollHeight;
		const bodyOffsetHeight = iframeDoc.body.offsetHeight;
		const docHeight = iframeDoc.documentElement.scrollHeight;
		const contentHeight = Math.max(bodyHeight, bodyOffsetHeight, docHeight);

		// 设置最小高度为 200px，防止内容过少时显示异常
		// 加上一些额外高度以确保内容完全显示
		iframe.style.height = `${Math.max(contentHeight + 10, 200)}px`;
	});
}

/**
 * 生成基础样式 CSS
 */

/**
 * 处理邮件 HTML 内容，返回处理好的完整 HTML
 * @param htmlContent 邮件 HTML 内容
 * @param isDark 是否为暗黑模式
 * @param cssVars CSS 变量配置（暗黑模式时使用）
 * @returns 处理好的完整 HTML 字符串
 */
export function renderEmailToIframe(htmlContent: string): string {
	if (!htmlContent) return htmlContent;

	// 先清洗，避免不受信内容的 XSS
	// const cleanHtml = DOMPurify.sanitize(htmlContent, DOMPURIFY_CONFIG);

	// 检查是否包含完整的 HTML 结构
	const hasFullHtml = htmlContent.includes('<html') || htmlContent.includes('<!DOCTYPE');

	let fullHtml: string;
	if (!hasFullHtml) {
		// 否则包装成完整的 HTML 文档
		fullHtml = `
			<!DOCTYPE html>
			<html>
			<head>
				<meta charset="UTF-8">
				<meta name="viewport" content="width=device-width, initial-scale=1.0">
				<style>
					body {
						margin: 0;
						padding: 0;
						font-size: 14px;
						line-height: 1.6;
					}
					a { color: #3b82f6; }
					img { max-width: 100%; height: auto; }
				</style>
			</head>
			<body>${htmlContent}</body>
			</html>
		`;
		return fullHtml;
	}

	return htmlContent;
}

async function ensureDarkreaderLoaded(iframe: HTMLIFrameElement): Promise<any | null> {
	const iframeWindow = iframe.contentWindow;
	const iframeDoc = iframe.contentDocument;
	if (!iframeWindow || !iframeDoc) return null;

	// 已经存在
	const existing = (iframeWindow as any).DarkReader;
	if (existing) return existing;

	// 已在加载中
	const pendingScript = iframeDoc.querySelector('script[data-darkreader]');
	if (pendingScript) {
		return new Promise((resolve) => {
			const timer = setInterval(() => {
				const dr = (iframeWindow as any).DarkReader;
				if (dr) {
					clearInterval(timer);
					resolve(dr);
				}
			}, 30);
		});
	}

	return new Promise((resolve, reject) => {
		const script = iframeDoc.createElement('script');
		const blob = new Blob([darkreaderSource], { type: 'application/javascript' });
		const blobUrl = URL.createObjectURL(blob);
		script.src = blobUrl;
		script.async = true;
		script.setAttribute('data-darkreader', 'true');
		script.onload = () => {
			URL.revokeObjectURL(blobUrl);
			resolve((iframeWindow as any).DarkReader || null);
		};
		script.onerror = () => {
			URL.revokeObjectURL(blobUrl);
			reject(new Error('Failed to load Darkreader'));
		};
		(iframeDoc.head || iframeDoc.body).appendChild(script);
	});
}

/**
 * 使用 Darkreader 对 iframe 内容进行暗黑处理（不再手动修改颜色）
 */
export async function applyDarkModeToElementsWithDarkreader(
	iframe: HTMLIFrameElement,
	isDark: boolean
): Promise<void> {
	const iframeWindow = iframe.contentWindow;
	const iframeDoc = iframe.contentDocument;
	if (!iframeWindow || !iframeDoc) return;

	const darkReader = await ensureDarkreaderLoaded(iframe);
	if (!darkReader) return;

	if (typeof darkReader.setFetchMethod === 'function') {
		darkReader.setFetchMethod((...args: Parameters<typeof fetch>) =>
			iframeWindow.fetch(...args)
		);
	}

	if (isDark) {
		darkReader.enable({
			brightness: 100,
			contrast: 100,
			sepia: 0,
		});
	} else if (typeof darkReader.disable === 'function') {
		darkReader.disable();
	}
}
