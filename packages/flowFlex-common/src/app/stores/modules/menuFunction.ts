import { defineStore } from 'pinia';
import { store } from '@/stores';
import { getAuthCache, setAuthCache } from '@/utils/auth';
import { MENU_MENUTYPE } from '@/enums/cacheEnum';
import { isFunction } from 'lodash-es';

interface MenuRoles {
	menuId: string;
	functionIds: string[];
	menuWatchForm: boolean;
	menuType: string;
	stopWatch: Function;
	saveChange: Function | null;
}

export const MenuFunctionStore = defineStore({
	id: 'flowFlex-menu-Function',
	state: (): MenuRoles => ({
		menuId: '',
		functionIds: [],
		menuWatchForm: false,
		menuType: getAuthCache(MENU_MENUTYPE) || 'add',
		stopWatch: () => {},
		saveChange: () => {},
	}),
	getters: {
		getMenuId: (state) => {
			return state.menuId;
		},
		getFunctionIds: (state) => {
			return state.functionIds;
		},
		getMenuWatchForm: (state) => {
			return state.menuWatchForm;
		},
	},
	actions: {
		async globelSaveChaneg(saveOrCancel: boolean) {
			if (saveOrCancel && isFunction(this.saveChange)) {
				return await this.saveChange();
			} else {
				this.cancelWatchForm();
				return null;
			}
		},
		cancelWatchForm() {
			this.stopWatch();
			this.saveChange = null;
			this.setWatchForm(false);
		},
		setWatchForm(value: boolean) {
			this.menuWatchForm = value;
		},
		setMenuId(menuId: string) {
			this.menuId = menuId;
		},
		async setFunctionIds(menuId: string) {
			this.menuId = menuId;
			this.functionIds = [];
		},
		setMenuType(menuType: string) {
			this.menuType = menuType;
			setAuthCache(MENU_MENUTYPE, menuType);
		},
	},
});

// Need to be used outside the setup
export function menuRoles() {
	return MenuFunctionStore(store);
}
