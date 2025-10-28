import type { RouteRecordRaw, RouteMeta } from 'vue-router';
import { RoleEnum } from '@/enums/roleEnum';
import { defineComponent } from 'vue';

export type Component<T = any> =
	| ReturnType<typeof defineComponent>
	| (() => Promise<typeof import('*.vue')>)
	| (() => Promise<T>);

// @ts-ignore
export interface AppRouteRecordRaw extends Omit<RouteRecordRaw, 'meta'> {
	name: string;
	meta: Meat;
	component?: Component | string;
	components?: Component;
	children?: AppRouteRecordRaw[];
	props?: any;
	fullPath?: string;
	hidden?: boolean;
}

export interface Meat extends RouteMeta {
	title?: string;
	code?: string;
	menuId?: string;
	beta?: boolean;
	ordinal?: number;
}

export interface MenuTag {
	type?: 'primary' | 'error' | 'warn' | 'success';
	content?: string;
	dot?: boolean;
}

export interface Menu {
	name: string;

	icon?: string;

	img?: string;

	path: string;

	// path contains param, auto assignment.
	paramPath?: string;

	disabled?: boolean;

	children?: Menu[];

	orderNo?: number;

	roles?: RoleEnum[];

	meta: Partial<Meat>;

	tag?: MenuTag;

	hideMenu?: boolean;

	hidden?: boolean;
}

export interface RoleMenu {
	code?: string;
	hidden?: boolean;
	icon?: string;
	isMenu?: boolean;
	menuId?: string;
	name?: string;
	ordinal?: number;
	parentId?: string;
	status?: boolean;
	url?: string;
}

export interface MenuModule {
	orderNo?: number;
	menu: Menu;
}

// export type AppRouteModule = RouteModule | AppRouteRecordRaw;
export type AppRouteModule = AppRouteRecordRaw;
