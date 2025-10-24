import type { Router, RouteLocationNormalized, RouteRecordRaw } from 'vue-router';
import { useUserStoreWithOut } from '@/stores/modules/user';
import { menuRoles } from '@/stores/modules/menuFunction';
import { ElNotification, ElMessageBox } from 'element-plus';
import { createStateGuard } from './stateGuard';
import { usePermissionStoreWithOut } from '@/stores/modules/permission';
import { Routes } from '@/router/routers';
import { getMenuListPath } from '@/utils';
import { AxiosCanceler } from '@/apis/axios/axiosCancel';
import { ParametersToken } from '#/config';
import { isIframe, parseUrlSearch, objectToQueryString } from '@/utils/utils';
import {
	formIDMLogin,
	toIDMLogin,
	setEnvironment,
	wujieCrmToken,
	setAppCode,
} from '@/utils/threePartyLogin';
import { PageEnum } from '@/enums/pageEnum';
// import { getEnv } from '@/utils/env';

import { getTokenobj } from '@/utils/auth';

import nProgress from 'nprogress';

// 配置 nProgress - 禁用右侧的旋转圆圈
nProgress.configure({ showSpinner: false });

const allPagePaths = getMenuListPath(Routes);

export async function setupRouterGuard(router: Router) {
	await handleTripartiteToken();
	await createDynamicRoutes(router);

	router.beforeEach(async (to, from, next) => {
		nProgress.start();
		handleHttpGuard();
		handleMessageGuard();

		if (handleTokenCheck(to, next)) return;
		await handleNavigateWatchForm(next);
		await handleRouterRoles(to);
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

	// Portal相关路径处理 - Portal用户使用Portal Token访问
	// Portal Token有限制的scope，只能访问Portal相关接口
	if (to.path.startsWith('/portal-access')) {
		// portal-access页面允许直接访问（不需要任何认证）
		return false;
	}

	if (to.path.startsWith('/customer-portal') || to.path.startsWith('/onboard/sub-portal')) {
		const portalAccessToken = localStorage.getItem('portal_access_token');
		const urlToken = to.query.token;

		// Portal页面：有Portal Token或标准Token都允许访问
		if (portalAccessToken || accessToken || urlToken) {
			console.log('[Router Guard] Portal access allowed:', {
				hasPortalToken: !!portalAccessToken,
				hasAccessToken: !!accessToken,
				hasUrlToken: !!urlToken,
			});
			return false;
		}

		// 没有任何Token，跳转到Portal Access页面（而不是登录页）
		console.log('[Router Guard] No token found, redirecting to portal-access');
		next({ path: '/portal-access' });
		return true;
	}

	// 普通页面的Token检查
	if (!accessToken || (accessToken && to.path === '/login')) {
		toIDMLogin('login');
		return true;
	}
	if (accessToken && to.path === '/login') {
		next({ path: PageEnum.BASE_HOME });
		return true;
	}

	return false;
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

async function handleRouterRoles(to) {
	console.log('to.meta.menuId:', to.meta.menuId);
	if (!to.meta.menuId) return;

	try {
		// TODO: 根据路由切换 权限codes获取
		const menuRolesStore = menuRoles();
		await menuRolesStore.setFunctionIds(to.meta.menuId as string);
	} catch (error) {
		console.log('Error in handleRouterRoles:', error);
	}
}

async function createDynamicRoutes(router: Router) {
	try {
		// Skip dynamic route creation for Portal pages
		// Portal pages don't need user permissions or dynamic routes
		const isPortalPath =
			window.location.pathname.startsWith('/portal-access') ||
			window.location.pathname.startsWith('/customer-portal') ||
			window.location.pathname.startsWith('/onboard/sub-portal');

		if (isPortalPath) {
			console.log('[Router Guard] Skipping dynamic routes for Portal page');
			return;
		}

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

	// 统一处理：无论是否在微前端环境，都通过参数请求接口获取token
	if (parameterObj) {
		const {
			loginType,
			appCode,
			ticket = '',
			code = '',
			oauth,
			hideEditMenu,
			hideMenu,
			state,
		} = parameterObj;

		userStore.setLayout({
			hideMenu: hideMenu || isIframe(),
			hideEditMenu,
		});

		if (loginType) {
			setEnvironment(loginType);
			await userStore.logout(true);
			return;
		}
		setAppCode(appCode);

		const authParam = ticket || code;

		if (authParam) {
			setEnvironment('unissso');
			await formIDMLogin(authParam, oauth, state);
		}
	}

	// 旧的微前端token处理逻辑（注释掉，保留备用）
	if (window.__POWERED_BY_WUJIE__ && window.$wujie?.props) {
		console.log('无界环境处理 token');
		console.log('window.$wujie.props:', window.$wujie.props);
		if (getTokenobj()?.accessToken?.token) return;
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
	}
}

async function handlePermissionGuard(to, from, next) {
	const permissionStore = usePermissionStoreWithOut();
	const rolePath = getMenuListPath(permissionStore.getFrontMenuList);

	// 跳过Portal相关页面的权限检查（Portal用户使用Portal Token，不需要菜单权限）
	const isPortalPath =
		to.path.startsWith('/portal-access') ||
		to.path.startsWith('/customer-portal') ||
		to.path.startsWith('/onboard/sub-portal');

	if (!isPortalPath && allPagePaths.includes(to.path) && !rolePath.includes(to.path)) {
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
