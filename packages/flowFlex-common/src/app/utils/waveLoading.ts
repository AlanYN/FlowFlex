import { createApp, App } from 'vue';

// 波浪加载组件
const WaveLoadingComponent = {
	name: 'WaveLoading',
	props: {
		text: {
			type: String,
			default: 'Loading...',
		},
		background: {
			type: String,
			default: 'rgba(15, 15, 35, 0.9)',
		},
	},
	template: `
		<div class="wave-loading-overlay" :style="{ background }">
			<div class="wave-loading-content">
				<div class="wave-loading">
					<div class="bar"></div>
					<div class="bar"></div>
					<div class="bar"></div>
					<div class="bar"></div>
					<div class="bar"></div>
				</div>
				<div class="wave-loading-text" v-if="text">{{ text }}</div>
			</div>
		</div>
	`,
	style: `
		<style>
		.wave-loading-overlay {
			position: fixed;
			top: 0;
			left: 0;
			width: 100vw;
			height: 100vh;
			display: flex;
			justify-content: center;
			align-items: center;
			z-index: 9999;
			backdrop-filter: blur(2px);
		}

		.wave-loading-content {
			display: flex;
			flex-direction: column;
			align-items: center;
			gap: 16px;
		}

		.wave-loading {
			display: flex;
			justify-content: center;
			align-items: center;
			gap: 3px;
		}

		.wave-loading .bar {
			width: 4px;
			height: 20px;
			background: #8b5cf6;
			border-radius: 2px;
			animation: wave 1.2s ease-in-out infinite;
			transition: background-color 0.3s ease;
		}

		.wave-loading .bar:nth-child(1) {
			animation-delay: -1.1s;
		}
		.wave-loading .bar:nth-child(2) {
			animation-delay: -1.0s;
		}
		.wave-loading .bar:nth-child(3) {
			animation-delay: -0.9s;
		}
		.wave-loading .bar:nth-child(4) {
			animation-delay: -0.8s;
		}
		.wave-loading .bar:nth-child(5) {
			animation-delay: -0.7s;
		}

		.wave-loading-text {
			color: #ffffff;
			font-size: 14px;
			font-weight: 500;
			text-align: center;
			margin-top: 8px;
		}

		@keyframes wave {
			0%, 40%, 100% {
				transform: scaleY(0.4);
			}
			20% {
				transform: scaleY(1.0);
			}
		}
		</style>
	`,
};

interface WaveLoadingOptions {
	text?: string;
	background?: string;
	lock?: boolean;
}

class WaveLoadingService {
	private app: App | null = null;
	private container: HTMLElement | null = null;

	service(options: WaveLoadingOptions = {}) {
		// 如果已经有加载实例，先关闭它
		this.close();

		// 创建容器
		this.container = document.createElement('div');
		document.body.appendChild(this.container);

		// 创建 Vue 应用实例
		this.app = createApp(WaveLoadingComponent, {
			text: options?.text || '',
			background: options?.background || '#0f0f23',
		});

		// 挂载到容器
		this.app.mount(this.container);

		// 添加样式到 head
		this.addStyles();

		// 如果需要锁定页面
		if (options.lock !== false) {
			document.body.style.overflow = 'hidden';
		}

		return this;
	}

	close() {
		if (this.app && this.container) {
			// 卸载 Vue 应用
			this.app.unmount();

			// 移除容器
			if (this.container.parentNode) {
				this.container.parentNode.removeChild(this.container);
			}

			// 重置引用
			this.app = null;
			this.container = null;

			// 恢复页面滚动
			document.body.style.overflow = '';
		}
	}

	private addStyles() {
		// 检查是否已经添加了样式
		if (document.getElementById('wave-loading-styles')) {
			return;
		}

		const style = document.createElement('style');
		style.id = 'wave-loading-styles';
		style.textContent = `
			.wave-loading-overlay {
				position: fixed;
				top: 0;
				left: 0;
				width: 100vw;
				height: 100vh;
				display: flex;
				justify-content: center;
				align-items: center;
				z-index: 9999;
				backdrop-filter: blur(2px);
			}

			.wave-loading-content {
				display: flex;
				flex-direction: column;
				align-items: center;
				gap: 16px;
			}

			.wave-loading {
				display: flex;
				justify-content: center;
				align-items: center;
				gap: 3px;
			}

			.wave-loading .bar {
				width: 4px;
				height: 20px;
				background: #8b5cf6;
				border-radius: 2px;
				animation: wave 1.2s ease-in-out infinite;
				transition: background-color 0.3s ease;
			}

			.wave-loading .bar:nth-child(1) {
				animation-delay: -1.1s;
			}
			.wave-loading .bar:nth-child(2) {
				animation-delay: -1.0s;
			}
			.wave-loading .bar:nth-child(3) {
				animation-delay: -0.9s;
			}
			.wave-loading .bar:nth-child(4) {
				animation-delay: -0.8s;
			}
			.wave-loading .bar:nth-child(5) {
				animation-delay: -0.7s;
			}

			.wave-loading-text {
				color: #ffffff;
				font-size: 14px;
				font-weight: 500;
				text-align: center;
				margin-top: 8px;
			}

			@keyframes wave {
				0%, 40%, 100% {
					transform: scaleY(0.4);
				}
				20% {
					transform: scaleY(1.0);
				}
			}
		`;
		document.head.appendChild(style);
	}
}

// 创建全局实例
const WaveLoading = new WaveLoadingService();

export default WaveLoading;
