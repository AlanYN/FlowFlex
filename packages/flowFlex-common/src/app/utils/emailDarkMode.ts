/**
 * 邮件暗黑模式适配工具
 * 通过直接修改 HTML 内容实现暗黑模式适配
 */

import darkreaderSource from 'darkreader/darkreader.js?raw';

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
