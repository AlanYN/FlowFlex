/**
 * 标签页同步信号类型
 */
type TabSyncSignalType = 'state_change';

/**
 * 标签页同步信号数据
 */
interface TabSyncSignal {
	type: TabSyncSignalType;
	timestamp: number;
	tabId: string;
}

/**
 * 标签页同步管理工具类
 * 实现轻量级的多标签页状态同步机制
 */
class TabSyncManager {
	private static instance: TabSyncManager;
	private currentTabId: string;
	private isInitialized = false;
	private storageListener: ((e: StorageEvent) => void) | null = null;
	private visibilityListener: (() => void) | null = null;

	private readonly SYNC_SIGNAL_KEY = '__tab_sync_signal__';
	private readonly NEED_REFRESH_KEY = '__need_refresh__';

	private constructor() {
		this.currentTabId = this.generateTabId();
	}

	public static getInstance(): TabSyncManager {
		if (!TabSyncManager.instance) {
			TabSyncManager.instance = new TabSyncManager();
		}
		return TabSyncManager.instance;
	}

	/**
	 * 初始化标签页同步功能
	 */
	public init(): void {
		if (this.isInitialized) {
			return;
		}

		// 清除可能存在的刷新标记，避免页面加载时立即触发刷新
		sessionStorage.removeItem(this.NEED_REFRESH_KEY);

		this.setupStorageListener();
		this.setupVisibilityListener();
		this.isInitialized = true;

		console.log('[TabSync] Tab synchronization initialized, tabId:', this.currentTabId);
	}

	/**
	 * 销毁监听器
	 */
	public destroy(): void {
		if (this.storageListener) {
			window.removeEventListener('storage', this.storageListener);
			this.storageListener = null;
		}

		if (this.visibilityListener) {
			document.removeEventListener('visibilitychange', this.visibilityListener);
			this.visibilityListener = null;
		}

		this.isInitialized = false;
		console.log('[TabSync] Tab synchronization destroyed');
	}

	/**
	 * 发送同步信号给其他标签页
	 */
	public notifyOtherTabs(): void {
		const signal: TabSyncSignal = {
			type: 'state_change',
			timestamp: Date.now(),
			tabId: this.currentTabId,
		};

		try {
			localStorage.setItem(this.SYNC_SIGNAL_KEY, JSON.stringify(signal));
			console.log('[TabSync] Notified other tabs: state changed');

			// 立即清除信号，避免影响localStorage
			setTimeout(() => {
				localStorage.removeItem(this.SYNC_SIGNAL_KEY);
			}, 100);
		} catch (error) {
			console.warn('[TabSync] Failed to notify other tabs:', error);
		}
	}

	/**
	 * 设置localStorage监听器
	 */
	private setupStorageListener(): void {
		this.storageListener = (e: StorageEvent) => {
			if (e.key === this.SYNC_SIGNAL_KEY && e.newValue) {
				try {
					const signal: TabSyncSignal = JSON.parse(e.newValue);

					// 忽略自己发送的信号
					if (signal.tabId === this.currentTabId) {
						return;
					}

					console.log(
						`[TabSync] Received signal from tab ${signal.tabId}: ${signal.type}`
					);

					// 标记需要刷新，不立即执行
					sessionStorage.setItem(
						this.NEED_REFRESH_KEY,
						JSON.stringify({
							type: signal.type,
							timestamp: signal.timestamp,
							fromTabId: signal.tabId,
						})
					);
				} catch (error) {
					console.warn('[TabSync] Failed to parse sync signal:', error);
				}
			}
		};

		window.addEventListener('storage', this.storageListener);
	}

	/**
	 * 设置页面可见性监听器
	 */
	private setupVisibilityListener(): void {
		this.visibilityListener = () => {
			// 页面变为可见时检查是否需要刷新
			if (!document.hidden) {
				this.checkAndRefresh();
			}
		};

		document.addEventListener('visibilitychange', this.visibilityListener);
	}

	/**
	 * 检查并刷新状态
	 */
	private checkAndRefresh(): void {
		const refreshInfo = sessionStorage.getItem(this.NEED_REFRESH_KEY);
		if (!refreshInfo) {
			return;
		}

		try {
			const { type, fromTabId, timestamp } = JSON.parse(refreshInfo);

			// 检查时间戳，避免页面刷新后重复处理同一个信号
			const now = Date.now();
			if (now - timestamp > 5000) {
				// 5秒内的信号才处理
				console.log('[TabSync] Signal too old, ignoring');
				sessionStorage.removeItem(this.NEED_REFRESH_KEY);
				return;
			}

			console.log(`[TabSync] Refreshing due to ${type} from tab ${fromTabId}`);

			// 先清除刷新标记，避免刷新后重复处理
			sessionStorage.removeItem(this.NEED_REFRESH_KEY);

			// 执行完整刷新
			this.performFullRefresh(type);
		} catch (error) {
			console.warn('[TabSync] Failed to process refresh info:', error);
			// 清除损坏的刷新标记
			sessionStorage.removeItem(this.NEED_REFRESH_KEY);
		}
	}

	/**
	 * 执行完整的状态刷新
	 */
	private async performFullRefresh(signalType: TabSyncSignalType): Promise<void> {
		console.log(
			`[TabSync] Detected ${signalType} from another tab, reloading page to sync state`
		);

		// 简单粗暴但有效的方案：直接刷新页面
		// 这样可以确保获取最新的localStorage状态，避免复杂的状态同步逻辑
		window.location.reload();
	}

	/**
	 * 生成唯一的标签页ID
	 */
	private generateTabId(): string {
		return `tab_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
	}

	/**
	 * 获取当前标签页ID
	 */
	public getTabId(): string {
		return this.currentTabId;
	}
}

// 导出单例实例
export const tabSyncManager = TabSyncManager.getInstance();

// 便捷的初始化和销毁函数
export function initTabSync(): void {
	tabSyncManager.init();
}

export function destroyTabSync(): void {
	tabSyncManager.destroy();
}

// 便捷的通知函数
export function notifyTabStateChange(): void {
	tabSyncManager.notifyOtherTabs();
}
