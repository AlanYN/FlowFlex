/**
 * 简化版全局缩放适配器
 * 基于 viewport 宽度动态设置 rem 基准值
 */

export interface ScaleOptions {
	designWidth?: number; // 设计稿宽度，默认1920
	baseFontSize?: number; // 基础字体大小，默认16px
	minFontSize?: number; // 最小字体大小（防止过小）
	maxFontSize?: number; // 最大字体大小（防止过大）
}

class ScaleAdapter {
	private designWidth: number;
	private baseFontSize: number;
	private minFontSize: number;
	private maxFontSize: number;

	constructor(options: ScaleOptions = {}) {
		this.designWidth = options.designWidth || 1920;
		this.baseFontSize = options.baseFontSize || 16;
		this.minFontSize = options.minFontSize || 12;
		this.maxFontSize = options.maxFontSize || 24;

		if (typeof window !== 'undefined') {
			this.init();
		}
	}

	private init(): void {
		this.setFontSize();
		window.addEventListener(
			'resize',
			this.debounce(() => this.setFontSize(), 100)
		);
	}

	/**
	 * 根据当前窗口宽度动态调整 html 根字体
	 */
	private setFontSize(): void {
		const clientWidth = document.documentElement.clientWidth;
		const scale = clientWidth / this.designWidth;

		let newFontSize = this.baseFontSize * scale;
		newFontSize = Math.max(this.minFontSize, Math.min(this.maxFontSize, newFontSize));

		document.documentElement.style.fontSize = `${newFontSize}px`;
		document.documentElement.style.setProperty('--scale-factor', `${scale}`);
	}

	/**
	 * px 转 rem
	 */
	public pxToRem(px: number): string {
		const currentFontSize = parseFloat(getComputedStyle(document.documentElement).fontSize);
		return `${px / currentFontSize}rem`;
	}

	/**
	 * rem 转 px
	 */
	public remToPx(rem: number): number {
		const currentFontSize = parseFloat(getComputedStyle(document.documentElement).fontSize);
		return rem * currentFontSize;
	}

	/**
	 * 防抖函数
	 */
	private debounce<T extends (...args: any[]) => any>(func: T, wait: number) {
		let timeout: NodeJS.Timeout;
		return (...args: Parameters<T>) => {
			clearTimeout(timeout);
			timeout = setTimeout(() => func.apply(this, args), wait);
		};
	}
}

let globalScaleAdapter: ScaleAdapter | null = null;

export function initScaleAdapter(options?: ScaleOptions): ScaleAdapter {
	if (globalScaleAdapter) return globalScaleAdapter;
	globalScaleAdapter = new ScaleAdapter(options);
	return globalScaleAdapter;
}

export function getScaleAdapter(): ScaleAdapter | null {
	return globalScaleAdapter;
}
