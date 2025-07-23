import { setPrimary, setTheme } from '@/utils/theme';
import { router } from '@/router';
import { useUserStoreWithOut } from '@/stores/modules/user';

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
			window.$wujie.bus.$off('props-update');
			window.$wujie.bus.$off('primary-change');
			window.$wujie.bus.$off('theme-change');
			window.$wujie.bus.$off('logout');

			window.$wujie.bus.$on('props-update', (newProps: any) => {
				currentProps = newProps;
				againgWujieSubApp(newProps);
			});

			window.$wujie.bus.$on('primary-change', (newPrimary: string) => {
				setPrimary(newPrimary);
			});

			window.$wujie.bus.$on('theme-change', (newTheme: string) => {
				console.log('theme-change received via wujie bus:', newTheme);
				setTheme(newTheme);
			});

			window.$wujie.bus.$on('logout', (isTokenExpired: boolean) => {
				console.log('子应用收到主应用的退出事件', isTokenExpired);
				window.__WUJIE_UNMOUNT();
				const userStore = useUserStoreWithOut();
				userStore.logout(isTokenExpired, 'mainLoagout');
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
		const { theme, primary, currentRoute } = currentProps;

		if (theme) {
			console.log('设置主题:', theme);
			setTheme(theme);
		}
		if (primary) {
			console.log('设置主色调:', primary);
			setPrimary(primary);
		}

		if (currentRoute) {
			console.log('设置路由：', currentRoute, router);
			router.push(currentRoute);
		}

		const userStore = useUserStoreWithOut();
		userStore.setLayout({
			hideMenu: true,
			hideEditMenu: true,
		});
	};

	const initWujieSubApp = () => {
		const currentProps = window.$wujie?.props || {};
		if (!isMicroAppEnvironment() || !currentProps) {
			console.log('跳过初始化，环境检查失败或 props 为空');
			return;
		}

		const { theme, primary, currentRoute } = currentProps;

		if (theme) {
			console.log('设置主题:', theme);
			setTheme(theme);
		}
		if (primary) {
			console.log('设置主色调:', primary);
			setPrimary(primary);
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
