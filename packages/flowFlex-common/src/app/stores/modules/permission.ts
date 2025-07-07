import type { Menu, RoleMenu, AppRouteRecordRaw } from '@/router/types';
// import type { Router } from 'vue-router';

import { defineStore } from 'pinia';
import { store } from '@/stores';
import { useUserStore } from './user';
// import { flatMultiLevelRoutes } from '@/router/helper/routeHelper';

import { Routes } from '@/router/routers';

import { router, resetRouter } from '@/router';
import type { RouteRecordNormalized } from 'vue-router';

import { filter } from '@/utils/helper/treeHelper';

import { PageEnum } from '@/enums/pageEnum';

interface PermissionState {
	// Permission code list
	// 权限代码列表
	permCodeList: string[] | number[];
	// Whether the route has been dynamically added
	// 路由是否动态添加
	isDynamicAddedRoute: boolean;
	// To trigger a menu update
	// 触发菜单更新
	lastBuildMenuTime: number;
	// Backstage menu list
	// 后台菜单列表
	backMenuList: RoleMenu[];
	// 菜单列表
	frontMenuList: Menu[];
	// 是否开启pipeline
	pipeline: boolean;
}

export const usePermissionStore = defineStore({
	id: 'app-permission',
	state: (): PermissionState => ({
		// 权限代码列表
		permCodeList: [],
		// Whether the route has been dynamically added
		// 路由是否动态添加
		isDynamicAddedRoute: false,
		// To trigger a menu update
		// 触发菜单更新
		lastBuildMenuTime: 0,
		// Backstage menu list
		// 后台菜单列表
		backMenuList: Routes,
		// menu List
		// 菜单列表
		frontMenuList: [],
		// 是否开启pipeline
		pipeline: false,
	}),
	getters: {
		getPermCodeList(state): string[] | number[] {
			return state.permCodeList;
		},
		getBackMenuList(state): RoleMenu[] {
			return state.backMenuList;
		},
		getFrontMenuList(state): Menu[] {
			return state.frontMenuList;
		},
		getLastBuildMenuTime(state): number {
			return state.lastBuildMenuTime;
		},
		getIsDynamicAddedRoute(state): boolean {
			return state.isDynamicAddedRoute;
		},
		getPipeline(state): boolean {
			return state.pipeline;
		},
	},
	actions: {
		setPermCodeList(codeList: string[]) {
			this.permCodeList = codeList;
		},

		setBackMenuList(list: Menu[]) {
			this.backMenuList = list;
			list?.length > 0 && this.setLastBuildMenuTime();
		},

		setFrontMenuList(list: Menu[]) {
			this.frontMenuList = list;
		},

		setLastBuildMenuTime() {
			this.lastBuildMenuTime = new Date().getTime();
		},

		setDynamicAddedRoute(added: boolean) {
			this.isDynamicAddedRoute = added;
		},
		setPipeline(pipeline: boolean) {
			this.pipeline = pipeline;
		},
		resetState(): void {
			this.isDynamicAddedRoute = false;
			this.permCodeList = [];
			this.backMenuList = [];
			this.lastBuildMenuTime = 0;
			this.frontMenuList = [];
			resetRouter();
		},

		// 构建路由
		async buildRoutesAction(): Promise<AppRouteRecordRaw[]> {
			this.resetState();
			const userStore = useUserStore();
			let roledsMenu = [] as any[];
			try {
				roledsMenu = [];
			} catch {
				roledsMenu = [];
			}

			this.setPermCodeList(roledsMenu);

			const menuCodeLIst = roledsMenu?.map((item) => item.code) || [];

			let routes: AppRouteRecordRaw[] = [];

			// 路由过滤器 在 函数filter 作为回调传入遍历使用
			const routeFilter = (route: AppRouteRecordRaw) => {
				const { meta } = route;
				// 抽出角色
				const { code } = meta || {};
				if (!code) return true;
				// 进行角色权限判断
				return menuCodeLIst.includes(code);
			};

			/**
			 * @description 根据设置的首页path，修正routes中的affix标记（固定首页）
			 * */
			const patchHomeAffix = (routes: RouteRecordNormalized[]) => {
				if (!routes || routes.length === 0) return;
				const homePath = userStore.getUserInfo.homePath || (PageEnum.BASE_HOME as string);

				function patcher(routes: RouteRecordNormalized[], parentPath = '') {
					if (parentPath) parentPath = parentPath + '/';

					for (const route of routes) {
						const { path, children } = route;
						const currentPath = path.startsWith('/') ? path : parentPath + path;
						if (currentPath === '/') {
							route.redirect = homePath;
							break; // 直接结束循环
						}
						children &&
							children.length > 0 &&
							patcher(children as RouteRecordNormalized[], currentPath);
					}
				}

				try {
					patcher(routes);
				} catch (e) {
					// 已处理完毕跳出循环
				}
				return;
			};

			// 需要将后台返回的菜单转换成菜单列表

			// 使用ne Map优化之前嵌套循环 降低运行时间
			const menuSetMenuId = (menuList: Menu[]) => {
				// 创建一个 permCodeMap 来代替嵌套循环查找
				const permCodeMap = new Map();
				this.permCodeList.forEach((item2) => {
					permCodeMap.set(item2.code, item2);
				});

				const setMenuAttributes = (menuList: Menu[]) => {
					menuList.forEach((item) => {
						const perm = permCodeMap.get(item.meta.code);
						if (perm) {
							item.meta.menuId = perm.menuId || '';
							item.meta.ordinal = perm.ordinal;
							item.meta.hidden = perm.hidden;
							item.meta.status = perm.status;
						} else {
							item.meta.menuId = '';
						}
						if (item.children && item.children.length > 0) {
							setMenuAttributes(item.children);
						}
					});
				};

				setMenuAttributes(menuList);
				console.log('menuList:', menuList);
			};
			// 路由映射， 默认进入该case
			// 对非一级路由进行过滤

			routes = filter(Routes, routeFilter);

			// 对一级路由再次根据角色权限过滤
			routes = routes.filter(routeFilter);

			// 将路由转换成菜单
			// 移除掉 ignoreRoute: true 的路由 非一级路由
			// routes = filter(routes, routeRemoveIgnoreFilter);
			// 移除掉 ignoreRoute: true 的路由 一级路由；
			// routes = routes.filter(routeRemoveIgnoreFilter);
			menuSetMenuId(routes);
			console.log('设置菜单结束', routes);

			// 设置菜单列表
			this.setFrontMenuList(routes);

			// Convert multi-level routing to level 2 routing
			// 将多级路由转换为 2 级路由
			// routes = flatMultiLevelRoutes(routes);
			patchHomeAffix(router.getRoutes());
			return routes;
		},
	},
});

// Need to be used outside the setup
// 需要在设置之外使用
export function usePermissionStoreWithOut() {
	return usePermissionStore(store);
}
