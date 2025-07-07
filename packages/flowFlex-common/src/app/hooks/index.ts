import { router } from '@/router';
import { menuRoles } from '@/stores/modules/menuFunction';
import { getTimeZoneInfo } from './time';

// 使用 router 实例来访问当前路由和执行导航
const currentRoute = router.currentRoute.value;

interface PrevRoute {
	name?: string;
	tab?: string;
}

// 返回到上一级
export const useGoBack = () => {
	// 如果页面有指定上一个路由的参数则根据参数跳转
	if (currentRoute.meta?.prevRoute) {
		const prevRoute = currentRoute.meta.prevRoute as PrevRoute;
		router.push({ name: prevRoute.name, query: { tab: prevRoute?.tab } });
		return;
	}
	router.back();
};

// 自定义指令 控制按钮是否展示
export const vPerMission = (app) => {
	/**
	 * 自定义指令 判断用户是否有需要控制的权限，传权限code ，可以传多个权限code 多个权限code用逗号隔开
	 */
	app.directive('permission', {
		mounted(el, binding) {
			const { value } = binding;
			if (!value) return;
			const menuRolesPer = menuRoles();
			const values = value?.split(',');
			if (!arrayContainsArray(menuRolesPer.getFunctionIds, values)) {
				el.remove();
			} else {
				try {
					const placeholder = document.createComment('permission placeholder');
					const parent = el?.parentNode;
					if (parent && el && !parent.contains(el)) {
						// console.log('parent:', parent);
						parent?.insertBefore(el, placeholder);
					}
				} catch (error) {
					console.log(error);
				}
			}
		},
	});
};

// 检测数组是否包含另一个数组的所有选项
function arrayContainsArray(superset, subset) {
	return subset.every((element) => superset.includes(element));
}

export function functionPermission(permissionRoles) {
	if (!permissionRoles) return true;
	const values = permissionRoles?.split(',');
	const menuRolesPer = menuRoles();
	if (!arrayContainsArray(menuRolesPer.getFunctionIds, values)) {
		return false;
	}
	return true;
}

export function reverseCreatedBy(name: string) {
	const reverseNameCountry = ['Asia/Hong_Kong', 'Asia/Macau', 'Asia/Shanghai', 'Asia/Taiwan'];
	const { timeZone } = getTimeZoneInfo();
	const splitName = typeof name === 'string' ? name.split(' ') : [];
	if (splitName.length > 1 && reverseNameCountry.includes(timeZone)) {
		return `${splitName[1]} ${splitName[0]}`;
	}
	return `${name}`;
}
