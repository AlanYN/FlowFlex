import { defineStore } from 'pinia';
import { store } from '@/stores';
import { getAuthCache, setAuthCache } from '@/utils/auth';
import { MENU_MENUTYPE } from '@/enums/cacheEnum';
import { isFunction } from 'lodash-es';
import { getFlowflexUser } from '@/apis/global';
import type { FlowflexUser } from '#/golbal';

interface MenuRoles {
	menuId: string;
	functionIds: string[];
	menuWatchForm: boolean;
	menuType: string;
	stopWatch: Function;
	saveChange: Function | null;
	// 用户数据缓存
	flowflexUserData: FlowflexUser[];
	userDataLoading: boolean;
}

export const MenuFunctionStore = defineStore({
	id: 'item-wfe-menu-Function',
	state: (): MenuRoles => ({
		menuId: '',
		functionIds: [],
		menuWatchForm: false,
		menuType: getAuthCache(MENU_MENUTYPE) || 'add',
		stopWatch: () => {},
		saveChange: () => {},
		// 用户数据缓存
		flowflexUserData: [],
		userDataLoading: false,
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
		getFlowflexUserData: (state) => {
			return state.flowflexUserData;
		},
		getUserDataLoading: (state) => {
			return state.userDataLoading;
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
		// 获取用户数据（带缓存）
		async getFlowflexUserDataWithCache(searchText = ''): Promise<FlowflexUser[]> {
			// 如果正在加载，等待加载完成
			if (this.userDataLoading) {
				return new Promise((resolve) => {
					const checkLoading = () => {
						if (!this.userDataLoading) {
							resolve(this.flowflexUserData);
						} else {
							setTimeout(checkLoading, 100);
						}
					};
					checkLoading();
				});
			}

			// 如果已有数据且没有搜索条件，直接返回缓存
			if (this.flowflexUserData.length > 0 && !searchText) {
				return this.flowflexUserData;
			}

			// 如果有搜索条件，直接调用接口不使用缓存
			if (searchText) {
				try {
					const response = await getFlowflexUser({ searchText });
					if (response.code === '200' && response.data) {
						return response.data;
					}
					return [];
				} catch (error) {
					console.error('Failed to fetch user data:', error);
					return [];
				}
			}

			// 首次加载数据
			try {
				this.userDataLoading = true;
				const response = await getFlowflexUser({ searchText });
				if (response.code === '200' && response.data) {
					this.flowflexUserData = response.data;
					return response.data;
				}
				return [];
			} catch (error) {
				console.error('Failed to fetch user data:', error);
				return [];
			} finally {
				this.userDataLoading = false;
			}
		},
		// 清空用户数据缓存
		clearFlowflexUserData() {
			this.flowflexUserData = [];
		},
	},
});

// Need to be used outside the setup
export function menuRoles() {
	return MenuFunctionStore(store);
}
