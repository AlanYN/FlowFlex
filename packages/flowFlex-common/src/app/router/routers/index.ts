import type { AppRouteRecordRaw, AppRouteModule } from '@/router/types';

import { PAGE_NOT_FOUND_ROUTE } from '@/router/routers/basic';

// import { mainOutRoutes } from './mainOut';
import { PageEnum } from '@/enums/pageEnum';
import { t } from '@/hooks/useI18n';

const modules = import.meta.glob('./modules/**/*.ts', { eager: true });
const routeModuleList: AppRouteModule[] = [];

// 加入到路由集合中
Object.keys(modules).forEach((key) => {
	const mod = (modules as Recordable)[key].default || {};
	routeModuleList.push(mod);
});

export const Routes = routeModuleList;

// 根路由
export const RootRoute: AppRouteRecordRaw = {
	path: '/',
	name: 'Root',
	redirect: PageEnum.BASE_HOME as string,
	meta: {
		title: 'Root',
	},
	// children: [...modules],
};

export const LoginRoute: AppRouteRecordRaw = {
	path: '/login',
	name: 'Login',
	component: () => import('@/views/login/index.vue'),
	meta: {
		title: t('sys.router.login'),
	},
};

// Basic routing without permission
// 未经许可的基本路由
// LoginRoute,
//   RootRoute,
export const whiteNameLiast = [RootRoute, LoginRoute, PAGE_NOT_FOUND_ROUTE]; //白名单
export const basicRoutes = [PAGE_NOT_FOUND_ROUTE, RootRoute, LoginRoute];
