import type { RouteLocationNormalized, RouteRecordNormalized } from 'vue-router';
import type { App, Component } from 'vue';

import { intersectionWith, isEqual, mergeWith, unionWith } from 'lodash-es';
import { unref } from 'vue';
import { isArray, isObject } from '@/utils/is';

import { useUserStore } from '@/stores/modules/user';

export { showPermissionBtn } from './permission';

export const noop = () => {};

/**
 * @description:  Set ui mount node
 */
export function getPopupContainer(node?: HTMLElement): HTMLElement {
	return (node?.parentNode as HTMLElement) ?? document.body;
}

/**
 * Add the object as a parameter to the URL
 * @param baseUrl url
 * @param obj
 * @returns {string}
 * eg:
 *  let obj = {a: '3', b: '4'}
 *  setObjToUrlParams('www.baidu.com', obj)
 *  ==>www.baidu.com?a=3&b=4
 */
export function setObjToUrlParams(baseUrl: string, obj: any): string {
	let parameters = '';
	for (const key in obj) {
		parameters += key + '=' + encodeURIComponent(obj[key]) + '&';
	}
	parameters = parameters.replace(/&$/, '');
	return /\?$/.test(baseUrl) ? baseUrl + parameters : baseUrl.replace(/\/?$/, '?') + parameters;
}

/**
 * Recursively merge two objects.
 * 递归合并两个对象。
 *
 * @param source The source object to merge from. 要合并的源对象。
 * @param target The target object to merge into. 目标对象，合并后结果存放于此。
 * @param mergeArrays How to merge arrays. Default is "replace".
 *        如何合并数组。默认为replace。
 *        - "union": Union the arrays. 对数组执行并集操作。
 *        - "intersection": Intersect the arrays. 对数组执行交集操作。
 *        - "concat": Concatenate the arrays. 连接数组。
 *        - "replace": Replace the source array with the target array. 用目标数组替换源数组。
 * @returns The merged object. 合并后的对象。
 */
export function deepMerge<T extends object | null | undefined, U extends object | null | undefined>(
	source: T,
	target: U,
	mergeArrays: 'union' | 'intersection' | 'concat' | 'replace' = 'replace'
): T & U {
	if (!target) {
		return source as T & U;
	}
	if (!source) {
		return target as T & U;
	}
	return mergeWith({}, source, target, (sourceValue, targetValue) => {
		if (isArray(targetValue) && isArray(sourceValue)) {
			switch (mergeArrays) {
				case 'union':
					return unionWith(sourceValue, targetValue, isEqual);
				case 'intersection':
					return intersectionWith(sourceValue, targetValue, isEqual);
				case 'concat':
					return sourceValue.concat(targetValue);
				case 'replace':
					return targetValue;
				default:
					throw new Error(`Unknown merge array strategy: ${mergeArrays as string}`);
			}
		}
		if (isObject(targetValue) && isObject(sourceValue)) {
			return deepMerge(sourceValue, targetValue, mergeArrays);
		}
		return undefined;
	});
}

export function openWindow(
	url: string,
	opt?: { target?: any | string; noopener?: boolean; noreferrer?: boolean }
) {
	const { target = '__blank', noopener = true, noreferrer = true } = opt || {};
	const feature: string[] = [];

	noopener && feature.push('noopener=yes');
	noreferrer && feature.push('noreferrer=yes');

	window.open(url, target, feature.join(','));
}

// dynamic use hook props
export function getDynamicProps<T extends Record<string, unknown>, U>(props: T): Partial<U> {
	const ret: Recordable = {};

	Object.keys(props).map((key) => {
		ret[key] = unref((props as Recordable)[key]);
	});

	return ret as Partial<U>;
}

export function getRawRoute(route: RouteLocationNormalized): RouteLocationNormalized {
	if (!route) return route;
	const { matched, ...opt } = route;
	return {
		...opt,
		matched: (matched
			? matched.map((item) => ({
					meta: item.meta,
					name: item.name,
					path: item.path,
			  }))
			: undefined) as RouteRecordNormalized[],
	};
}

// https://github.com/vant-ui/vant/issues/8302
type EventShim = {
	new (...args: any[]): {
		$props: {
			onClick?: (...args: any[]) => void;
		};
	};
};

export type WithInstall<T> = T & {
	install(app: App): void;
} & EventShim;

export type CustomComponent = Component & { displayName?: string };

export const withInstall = <T extends CustomComponent>(component: T, alias?: string) => {
	(component as Record<string, unknown>).install = (app: App) => {
		const compName = component.name || component.displayName;
		if (!compName) return;
		app.component(compName, component);
		if (alias) {
			app.config.globalProperties[alias] = component;
		}
	};
	return component as WithInstall<T>;
};

export function parseJWT(jwt) {
	const base64Url = jwt?.split('.')[1];
	const base64 = base64Url?.replace(/-/g, '+').replace(/_/g, '/');
	if (!base64) return jwt;
	const decodedData = atob(base64);
	const decodedObject = JSON?.parse(decodedData);
	return decodedObject;
}

/**
 *
 * @param stortName 检测模块的权限 当前用户的公司是否满足展示权限
 * @param args 剩余参数，可传递多个公司名字，多个公司的检测是或的关系 只要满足一个就可以
 * @returns 返回boolean值 当前用户公司满足权限返回true 否则返回false
 */
export function corporateAuthority(stortName, ...args) {
	const userStore = useUserStore();
	const tenantDisplayType = userStore.getUserInfo?.tenantDisplayType;
	if (args && args.length > 0) {
		return tenantDisplayType === stortName || args.includes(tenantDisplayType);
	}
	return tenantDisplayType === stortName;
}

//获取菜单列表的所有path
export function getMenuListPath(menuList: any[]): string[] {
	const arr = [] as string[];
	menuList.forEach((item) => {
		if (item.children && item.children.length > 0) {
			const pathList = getMenuListPath(item.children);
			pathList.forEach((path) => {
				arr.push(`${item.path}/${path}`);
			});
		} else {
			arr.push(item.path);
		}
	});
	return arr;
}

/**
 *  日期格式转换
 * @param dateTimeString
 * @returns
 */
export function formatDateTime(dateTimeString) {
	if (!dateTimeString) return dateTimeString;
	const date = new Date(dateTimeString);

	if (isNaN(date.getTime())) {
		return dateTimeString;
	}

	const month = String(date.getMonth() + 1).padStart(2, '0') as any;
	const day = String(date.getDate()).padStart(2, '0') as any;
	const year = date.getFullYear() as any;

	return `${month}/${day}/${year} `;
}

/* 深度解码url
 * @param str
 */
export const deepDecodeURIComponent = (str: string) => {
	let decoded = decodeURIComponent(str);
	while (decoded !== str) {
		str = decoded;
		decoded = decodeURIComponent(str);
	}
	return decoded;
};

/**
 * 将数字字符串格式化为货币格式
 * @param numStr 数字字符串
 * @param numberType 需要转换的语言环境，默认为 'en-US'
 * @returns
 */
export function formatToFinancial(numStr, numberType = 'en-US', fractionDigits = 2) {
	if (!numStr && numStr !== 0) return numStr;
	const num = parseFloat(numStr);

	if (isNaN(Number(num))) {
		throw new Error('Invalid number string');
	}

	return num.toLocaleString(numberType, {
		minimumFractionDigits: fractionDigits,
		maximumFractionDigits: fractionDigits,
	});
}

export function formatPhoneNumber(phoneNumber: string) {
	// 如果为空或undefined，返回null
	if (!phoneNumber) return null;
	// 检查是否只有区号，如果是则返回null
	else if (phoneNumber.startsWith('+') && !phoneNumber.includes(' ')) {
		return null;
	}
	// 检查是否包含空格（区号和手机号的分隔符）
	else if (phoneNumber.includes(' ')) {
		const parts = phoneNumber.split(' ');
		// 检查分隔符后是否有值，如果没有则认为只有区号
		if (parts.length > 1 && parts[1].trim()) {
			return phoneNumber; // 有区号和手机号，保持原值
		} else {
			return null; // 只有区号，返回null
		}
	} else {
		return phoneNumber;
	}
}

// 生成随机头像颜色
export const getAvatarColor = (name: string): string => {
	const colors = [
		'#C53030',
		'#2C7A7B',
		'#2B6CB0',
		'#38A169',
		'#D69E2E',
		'#9F7AEA',
		'#319795',
		'#D69E2E',
		'#805AD5',
		'#3182CE',
		'#DD6B20',
		'#38A169',
		'#E53E3E',
		'#3182CE',
		'#9F7AEA',
	];

	// 使用名字生成一个稳定的索引
	let hash = 0;
	for (let i = 0; i < name.length; i++) {
		hash = name.charCodeAt(i) + ((hash << 5) - hash);
	}
	return colors[Math.abs(hash) % colors.length];
};
