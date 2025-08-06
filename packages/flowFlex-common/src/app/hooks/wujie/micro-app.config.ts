import { setPrimary, setTheme } from '@/utils/theme';
import { router } from '@/router';
import { useUserStoreWithOut } from '@/stores/modules/user';
import { setupRouterGuard } from '@/router/guard';

export const useWujie = () => {
	if (!window.$wujie) {
		return {
			sendMessageToMainApp: () => {},
			initWujieSubApp: () => {},
			isMicroAppEnvironment: () => false,
		};
	}

	let currentProps = window.$wujie?.props || {};

	// 设置无界事件监听
	const setupEventListeners = () => {
		if (window.$wujie?.bus) {
			window.$wujie.bus.$on('props-update', (newProps: any) => {
				currentProps = newProps;
				againgWujieSubApp(newProps);
			});

			window.$wujie.bus.$on('primary-change', (newPrimary: string) => {
				setPrimary(newPrimary);
			});

			window.$wujie.bus.$on('theme-change', (newTheme: string) => {
				setTheme(newTheme);
			});

			window.$wujie.bus.$on('logout', async (isTokenExpired: boolean) => {
				const userStore = useUserStoreWithOut();
				await userStore.logout(isTokenExpired, 'mainLoagout');
			});
		}
	};

	/**
	 * 无界环境token过期
	 * @param isTokenExpired 是否是token过期,如果是true表示是token过期,如果是false表示主动退出登录
	 */
	const tokenExpiredLogOut = (isTokenExpired: boolean) => {
		window.$wujie.bus.$emit('token-expired-logout', isTokenExpired);
	};

	const sendMessageToMainApp = (type: string, data: any) => {
		if (window.$wujie?.bus) {
			window.$wujie.bus.$emit(type, data);
		}
	};

	/**
	 * 判断是否是无界环境
	 * @returns boolean
	 */
	const isMicroAppEnvironment = () => {
		return !!window?.__POWERED_BY_WUJIE__;
	};

	const againgWujieSubApp = async (newProps: any) => {
		currentProps = newProps;
		const { theme, primary, currentRoute, appCode, tenantId, authorizationToken } =
			currentProps;

		if (theme) {
			setTheme(theme);
		}
		if (primary) {
			setPrimary(primary);
		}

		if (appCode && tenantId && authorizationToken) {
			try {
				window.$wujie.props = newProps;
				await setupRouterGuard(router);
			} catch (error) {
				console.error('无界环境 token 处理失败:', error);
			}
		}

		if (currentRoute) {
			router.push(currentRoute);
		}

		const userStore = useUserStoreWithOut();
		userStore.setLayout({
			hideMenu: true,
			hideEditMenu: true,
		});
	};

	const initWujieSubApp = async () => {
		const currentProps = window.$wujie?.props || {};
		if (!isMicroAppEnvironment() || !currentProps) {
			return;
		}

		const { theme, primary, currentRoute, appCode, tenantId, authorizationToken } =
			currentProps;

		if (theme) {
			setTheme(theme);
		}
		if (primary) {
			setPrimary(primary);
		}

		if (appCode && tenantId && authorizationToken) {
			try {
				await setupRouterGuard(router);
			} catch (error) {
				console.error('无界环境 token 处理失败:', error);
			}
		}

		if (currentRoute) {
			router.push(currentRoute);
		}

		const userStore = useUserStoreWithOut();
		userStore.setLayout({
			hideMenu: true,
			hideEditMenu: true,
		});
	};

	// 立即设置事件监听器
	setupEventListeners();

	// 立即初始化一次
	initWujieSubApp();

	return {
		sendMessageToMainApp,
		initWujieSubApp,
		isMicroAppEnvironment,
		tokenExpiredLogOut,
	};
};
