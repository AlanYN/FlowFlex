import type { Router, RouteLocationNormalized, RouteRecordRaw } from 'vue-router';
import { useUserStoreWithOut } from '@/stores/modules/user';
import { menuRoles } from '@/stores/modules/menuFunction';
import { ElNotification, ElMessageBox } from 'element-plus';
import { createStateGuard } from './stateGuard';
import { usePermissionStoreWithOut } from '@/stores/modules/permission';
import { Routes } from '@/router/routers';
import { getMenuListPath } from '@/utils';
import { PageEnum } from '@/enums/pageEnum';
import { AxiosCanceler } from '@/apis/axios/axiosCancel';
import { useGlobSetting } from '@/settings';
import { ParametersToken } from '#/config';
import { isIframe, parseUrlSearch, objectToQueryString } from '@/utils/utils';
import { passSsoToken, setEnvironment, wujieCrmToken } from '@/utils/threePartyLogin';

import { getTokenobj } from '@/utils/auth';

import nProgress from 'nprogress';

const LOGIN_PATH = PageEnum.BASE_LOGIN;
const HOME_PATH = PageEnum.BASE_HOME;
const allPagePaths = getMenuListPath(Routes);
const globSetting = useGlobSetting();

export async function setupRouterGuard(router: Router) {
	await handleTripartiteToken();
	await createDynamicRoutes(router);

	router.beforeEach(async (to, from, next) => {
		nProgress.start();
		handleHttpGuard();
		handleMessageGuard();

		if (handleTokenCheck(to, next)) return;
		await handleNavigateWatchForm(next);
		await handlePermissionGuard(to, from, next);
	});

	router.afterEach((to) => {
		const menuRolesStore = menuRoles();
		menuRolesStore.setWatchForm(false);
		handleScrollGuard(to);
		createStateGuard(to);
		nProgress.done();
	});
}

function handleHttpGuard() {
	const axiosCanceler = new AxiosCanceler();
	axiosCanceler.removeAllPending();
}

function handleMessageGuard() {
	try {
		ElNotification.closeAll();
	} catch (error) {
		console.warn('Message guard error:', error);
	}
}

function handleTokenCheck(to, next) {
	const accessToken = getTokenobj()?.accessToken.token;

	// 对于customer-portal页面，优先检查标准认证，fallback到portal认证
	if (to.path.startsWith('/customer-portal')) {
		const portalAccessToken = localStorage.getItem('portal_access_token');
		const urlToken = to.query.token;

		if (accessToken || portalAccessToken || urlToken) {
			// 有标准用户认证、portal访问token或URL中有token参数，允许访问
			return false;
		}
	}

	// 对于portal-access页面，允许直接访问（不需要标准认证）
	if (to.path.startsWith('/portal-access')) {
		return false;
	}

	if (!accessToken) {
		redirectToLogin(to, next);
		return true;
	} else if (to.path === '/login') {
		next({ path: HOME_PATH });
		return true;
	}

	return false;
}

function redirectToLogin(to, next) {
	if (to.path !== '/login' && !window?.__POWERED_BY_WUJIE__) {
		const redirectData = {
			path: LOGIN_PATH,
			replace: true,
			query: {
				...to.query,
				redirect: to.fullPath,
			},
		};
		next(redirectData);
	} else {
		next();
	}
}

async function handleNavigateWatchForm(next) {
	const menuRolesStore = menuRoles();

	if (!menuRolesStore.getMenuWatchForm) {
		menuRolesStore.cancelWatchForm();
		return;
	}

	try {
		const answer = await ElMessageBox.confirm(
			'Do you need to save your changes before you leave?',
			'Warning',
			{
				confirmButtonText: 'Yes',
				cancelButtonText: 'No',
				type: 'warning',
				showClose: false,
				draggable: true,
				closeOnClickModal: false,
			}
		);
		await menuRolesStore.globelSaveChaneg(answer === 'confirm');
		menuRolesStore.cancelWatchForm();
	} catch (err) {
		if (err !== 'cancel') {
			next(false);
			return;
		}
		menuRolesStore.cancelWatchForm();
	}
}

async function createDynamicRoutes(router: Router) {
	try {
		const accessToken = getTokenobj()?.accessToken.token;
		if (!accessToken) return;
		const permissionStore = usePermissionStoreWithOut();
		if (permissionStore.getFrontMenuList.length <= 0) {
			const routes = await permissionStore.buildRoutesAction();
			routes.forEach((route) => {
				router.addRoute(route as unknown as RouteRecordRaw);
			});
		}
	} catch (err) {
		console.log('Error in createDynamicRoutes:', err);
	}
}

async function handleTripartiteToken() {
	const parameterObj = parseUrlSearch(window.location.href)?.query as ParametersToken;

	const userStore = useUserStoreWithOut();
	// 直接检查无界环境，避免调用可能未初始化的 useWujie
	if (window.__POWERED_BY_WUJIE__) {
		userStore.setLayout({
			hideMenu: true,
			hideEditMenu: true,
		});
	}
	if (window.__POWERED_BY_WUJIE__ && window.$wujie?.props) {
		console.log('无界环境处理 token');
		console.log('window.$wujie.props:', window.$wujie.props);
		if (userStore.getUserInfo.appCode && getTokenobj()?.accessToken?.token) return;
		const { appCode, tenantId, authorizationToken, currentRoute } = window.$wujie.props;
		if (appCode && tenantId && authorizationToken) {
			try {
				await wujieCrmToken(
					{
						appCode,
						tenantId,
						authorizationToken,
					},
					currentRoute
				);
			} catch (error) {
				console.error('无界环境 token 处理失败:', error);
			}
		}
	} else if (!window.__POWERED_BY_WUJIE__ && parameterObj) {
		const { loginType, code = '', state = '', hideEditMenu, hideMenu } = parameterObj;

		userStore.setLayout({
			hideMenu: hideMenu || isIframe(),
			hideEditMenu,
		});

		if (loginType) {
			setEnvironment(loginType);
			await userStore.logout(true);
			return;
		}

		switch (globSetting.environment) {
			case 'ITEM':
				if (code) {
					setEnvironment('item');
					await passSsoToken(code, state);
				}
				break;
			default:
				break;
		}
	}
}

async function handlePermissionGuard(to, from, next) {
	const permissionStore = usePermissionStoreWithOut();
	const rolePath = getMenuListPath(permissionStore.getFrontMenuList);

	// 跳过 portal-access 页面的权限检查（公开页面）
	if (
		!to.path.startsWith('/portal-access') &&
		allPagePaths.includes(to.path) &&
		!rolePath.includes(to.path)
	) {
		to.query.status = '403';
	}

	if (to.path.includes('Edit')) {
		mergeQueryParams(from, to);
	}

	next();
}

function mergeQueryParams(from, to) {
	if (!from.fullPath.includes('?')) return;

	const fromQuery = from.fullPath.split('?')[1];
	console.log('fromQuery:', fromQuery);
	if (fromQuery) {
		const queryParams = parseUrlSearch(from.fullPath);
		from.query = {
			...queryParams.query,
			...from.query,
		};
		if (Object.keys(from.query).includes('customerId')) {
			to.query = {
				...from.query,
				...to.query,
			};
			to.fullPath = `${to.path}?${objectToQueryString(to.query)}`;
		}
	}
}

function handleScrollGuard(to) {
	const isHash = (href: string) => /^#/.test(href);

	if (isHash((to as RouteLocationNormalized & { href: string })?.href)) {
		document.querySelector('#app-root')?.scrollTo(0, 0);
	}
}
