import { AppRouteModule } from '@/router/types';
import type { MenuModule, Menu, AppRouteRecordRaw } from '@/router/types';
import { findPath, treeMap } from '@/utils/helper/treeHelper';
import { cloneDeep } from 'lodash-es';
import { isHttpUrl } from '@/utils/is';
import { RouteParams } from 'vue-router';
import { toRaw } from 'vue';

export function getAllParentPath<T = Recordable>(treeData: T[], path: string) {
	const menuList = findPath(treeData, (n) => n.path === path) as Menu[];
	return (menuList || []).map((item) => item.path);
}

function joinParentPath(menus: Menu[], parentPath = '') {
	for (let index = 0; index < menus.length; index++) {
		const menu = menus[index];
		// https://next.router.vuejs.org/guide/essentials/nested-routes.html
		// Note that nested paths that start with / will be treated as a root path.
		// This allows you to leverage the component nesting without having to use a nested URL.
		if (!(menu.path.startsWith('/') || isHttpUrl(menu.path))) {
			// path doesn't start with /, nor is it a url, join parent path
			menu.path = `${parentPath}/${menu.path}`;
		}
		if (menu?.children?.length) {
			joinParentPath(menu.children, menu.meta?.hidePathForChildren ? parentPath : menu.path);
		}
	}
}

// Parsing the menu module
export function transformMenuModule(menuModule: MenuModule): Menu {
	const { menu } = menuModule;

	const menuList = [menu];

	joinParentPath(menuList);
	return menuList[0];
}

export function transformRouteToMenu(routeModList: AppRouteModule[], routerMapping = false) {
	const cloneRouteModList = cloneDeep(routeModList);
	const routeList: AppRouteRecordRaw[] = [];
	cloneRouteModList.forEach((item) => {
		if (routerMapping && item.meta.hideChildrenInMenu && typeof item.redirect === 'string') {
			item.path = item.redirect;
		}
		if (item.meta?.single) {
			const realItem = item?.children?.[0];
			realItem && routeList.push(realItem);
		} else {
			routeList.push(item);
		}
	});
	const list = treeMap(routeList, {
		conversion: (node: AppRouteRecordRaw) => {
			const { meta: { title, hideMenu = false } = {} } = node;

			return {
				...(node.meta || {}),
				meta: node.meta,
				name: title,
				hideMenu,
				path: node.path,
				...(node.redirect ? { redirect: node.redirect } : {}),
			};
		},
	});
	joinParentPath(list);
	return cloneDeep(list);
}

/**
 * config menu with given params
 */
const menuParamRegex = /(?::)([\s\S]+?)((?=\/)|$)/g;
export function configureDynamicParamsMenu(menu: Menu, params: RouteParams) {
	const { path, paramPath } = toRaw(menu);
	let realPath = paramPath ? paramPath : path;
	const matchArr = realPath.match(menuParamRegex);

	matchArr?.forEach((it) => {
		const realIt = it.substr(1);
		if (params[realIt]) {
			realPath = realPath.replace(`:${realIt}`, params[realIt] as string);
		}
	});
	// save original param path.
	if (!paramPath && matchArr && matchArr.length > 0) {
		menu.paramPath = path;
	}
	menu.path = realPath;
	// children
	menu.children?.forEach((item) => configureDynamicParamsMenu(item, params));
}
