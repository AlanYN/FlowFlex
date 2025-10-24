import { defineStore } from 'pinia';
import { store } from '@/stores';
import { RoleEnum } from '@/enums/roleEnum';
import { PageEnum } from '@/enums/pageEnum';
import { ROLES_KEY, USER_INFO_KEY, TOKENOBJ_KEY, ISLOGIN_KEY } from '@/enums/cacheEnum';
import { getAuthCache, setAuthCache } from '@/utils/auth';
import { loginApi, userInfoApi, emailCodelogin, registerApi } from '@/apis/login/user';
import { useI18n } from '@/hooks/useI18n';
import { router } from '@/router';
import { ElMessage, ElMessageBox } from 'element-plus';
import type { TokenObj } from '@/apis/axios/axiosTransform';
import { usePermissionStore } from '@/stores/modules/permission';
// import { useGlobSetting } from '@/settings';
import { menuRoles } from '@/stores/modules/menuFunction';
import { Logout } from '@/utils/threePartyLogin';
import { useWujie } from '@/hooks/wujie/micro-app.config';

import { h } from 'vue';
import dayjs from 'dayjs';

import { UserInfo, ILayout, UserState } from '#/config';

// const globSetting = useGlobSetting();

const { t } = useI18n();

// 添加一个辅助函数来删除所有cookie
function deleteAllCookies() {
	const cookies = document.cookie.split(';');
	for (let i = 0; i < cookies.length; i++) {
		const cookie = cookies[i];
		const eqPos = cookie.indexOf('=');
		const name = eqPos > -1 ? cookie.substr(0, eqPos) : cookie;
		document.cookie = name + '=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/';
	}
}

export const useUserStore = defineStore({
	id: 'item-wfe-app-user',
	state: (): UserState => ({
		// user info
		userInfo: null,
		// token
		token: undefined,
		// roleList
		roleList: [],
		// Whether the login expired
		sessionTimeout: false,
		// Last fetch time
		lastUpdateTime: 0,
		//terminal
		terminalCode: [],
		// tokenArr
		tokenObj: undefined,
		layout: {},
		isLogin: false,
		// tenant switching
		tenantSwitching: {
			isActive: false,
			fromTenantId: null,
			toTenantId: null,
			progress: 0,
			currentStep: '',
			error: null,
		},
	}),
	getters: {
		getToken(state): string {
			return (
				state.tokenObj?.accessToken?.token ||
				(getAuthCache<TokenObj>(TOKENOBJ_KEY)?.accessToken?.token as string)
			);
		},
		getUserInfo(state): UserInfo {
			return state.userInfo || getAuthCache<UserInfo>(USER_INFO_KEY) || {};
		},
		getTokenobj(state): TokenObj {
			return state.tokenObj || getAuthCache<TokenObj>(TOKENOBJ_KEY);
		},
		getRoleList(state): RoleEnum[] {
			return state.roleList.length > 0 ? state.roleList : getAuthCache<RoleEnum[]>(ROLES_KEY);
		},
		getSessionTimeout(state): boolean {
			return !!state.sessionTimeout;
		},
		getLastUpdateTime(state): number {
			return state.lastUpdateTime;
		},
		getLayout(state): ILayout {
			return state.layout;
		},
		getIsLogin(state): boolean {
			return state.isLogin || getAuthCache<boolean>(ISLOGIN_KEY);
		},
		getIsSwitchingTenant(state): boolean {
			// 向后兼容的getter
			return state.tenantSwitching.isActive;
		},
		getTenantSwitching(state) {
			return state.tenantSwitching;
		},
	},
	actions: {
		setRoleList(roleList: RoleEnum[]) {
			this.roleList = roleList;
			setAuthCache(ROLES_KEY, roleList);
		},
		setUserInfo(info: UserInfo | null) {
			this.userInfo = info;
			this.lastUpdateTime = new Date().getTime();
			setAuthCache(USER_INFO_KEY, info);
		},
		setSessionTimeout(flag: boolean) {
			this.sessionTimeout = flag;
		},
		setTokenobj(tokenObj: TokenObj | undefined) {
			this.tokenObj = tokenObj;
			setAuthCache(TOKENOBJ_KEY, tokenObj);
		},
		setLayout(layout: ILayout) {
			this.layout = layout;
		},
		async setTerminalCode(info: any[]) {
			this.terminalCode = info;
		},
		resetState() {
			this.userInfo = null;
			this.token = '';
			this.roleList = [];
			this.sessionTimeout = false;
		},
		setIsLogin(islogin) {
			this.isLogin = islogin;
			setAuthCache(ISLOGIN_KEY, islogin);
		},
		async siginUp(params) {
			const res = await registerApi(params);
			if (res.code === '200') {
				ElMessage.success('Register Success');
				await this.login({
					email: params.email,
					password: params.password,
				});
			} else {
				ElMessage.error(res.msg || t('sys.api.operationFailed'));
			}
		},
		/**
		 * @description: login
		 */
		async login(params, loginType = 'password') {
			try {
				const { ...loginParams } = params;
				const data =
					loginType === 'password'
						? await loginApi(loginParams)
						: await emailCodelogin(loginParams);
				const { accessToken, expiresIn, tokenType, user } = data.data;
				const currentDate = dayjs(new Date()).unix();
				if (accessToken && accessToken != '') {
					this.setTokenobj({
						accessToken: {
							token: accessToken,
							expire: +currentDate + +expiresIn,
							tokenType: tokenType,
						},
						refreshToken: accessToken,
					});
					return this.loginWithCode({
						...user,
						userName: user.email || user.username,
						userId: user.id,
					});
				} else {
					ElMessage.warning(data.msg || t('sys.api.operationFailed'));
				}
			} catch (error) {
				return Promise.reject(error);
			}
		},
		async afterLoginAction(goHome?: boolean) {
			const userDate = await userInfoApi();
			const userInfo: UserInfo = {
				...userDate.data,
				userId: userDate?.data?.userId,
				userName: userDate?.data?.userName,
				// userName: userDate?.data?.firstName + userDate?.data?.lastName,
				realName: `${userDate?.data?.firstName || ''}${
					userDate?.data?.lastName
						? ` ${userDate?.data?.lastName || ''}`
						: `${userDate?.data?.lastName || ''}`
				}`,
				email: userDate?.data?.email,
				desc: '',
				roles: userDate?.data?.roleIds,
			};
			this.setUserInfo(userInfo);
			const sessionTimeout = this.sessionTimeout;
			if (sessionTimeout) {
				this.setSessionTimeout(false);
			} else {
				const permissionStore = usePermissionStore();
				if (!permissionStore.isDynamicAddedRoute) {
					// console.log('buildRoutesAction');
					try {
						const routes = await permissionStore.buildRoutesAction();
						routes.forEach((route) => {
							router.addRoute(route as unknown as any);
						});
					} catch (error) {
						console.log(error);
					}
					// !goHome && router.addRoute(PAGE_NOT_FOUND_ROUTE as unknown as any);
					permissionStore.setDynamicAddedRoute(true);
				}
			}

			goHome && (await router.replace(PageEnum.BASE_HOME as string));
			return userInfo;
		},
		async loginWithCode(userInfo: UserInfo) {
			this.setUserInfo(userInfo);
			const permissionStore = usePermissionStore();
			if (!permissionStore.isDynamicAddedRoute) {
				// console.log('buildRoutesAction');
				try {
					const routes = await permissionStore.buildRoutesAction();
					routes.forEach((route) => {
						router.addRoute(route as unknown as any);
					});
				} catch (error) {
					console.log(error);
				}
				// !goHome && router.addRoute(PAGE_NOT_FOUND_ROUTE as unknown as any);
			}

			await router.replace(PageEnum.BASE_HOME as string);
			return userInfo;
		},
		/**
		 * @description: logout
		 */
		async logout(goLogin = false, type = 'logout') {
			const { tokenExpiredLogOut, isMicroAppEnvironment } = useWujie();
			type != 'mainLoagout' && tokenExpiredLogOut && tokenExpiredLogOut(true);
			const permissionStore = usePermissionStore();
			permissionStore.resetState();
			this.setTokenobj(undefined);
			this.setSessionTimeout(false);
			this.setUserInfo(null);
			// 添加删除所有cookie的操作
			deleteAllCookies();
			if (isMicroAppEnvironment()) return;
			goLogin && Logout(type);
		},

		/**
		 * @description: Confirm before logging out
		 */
		confirmLoginOut() {
			const { t } = useI18n();
			ElMessageBox({
				title: t('sys.app.logoutTip'),
				message: h('p', null, [h('span', null, t('sys.app.logoutMessage'))]),
				showCancelButton: true,
				confirmButtonText: t('sys.app.okText'),
				cancelButtonText: t('sys.app.cancelText'),
				closeOnClickModal: false,
				distinguishCancelAndClose: true,
				beforeClose: async (action, instance, done) => {
					if (action === 'confirm') {
						const menuRolesStore = menuRoles();
						menuRolesStore.cancelWatchForm();
						instance.confirmButtonLoading = true;
						instance.confirmButtonText = 'Loading...';
						await this.logout(true);
						instance.confirmButtonLoading = false;
						done();
					} else {
						done();
					}
				},
			});
		},

		/**
		 * @description: 请重新登录 没有取消按钮
		 */
		againLogin() {
			ElMessageBox.confirm(h('span', t('sys.app.logoutMessage')), t('sys.app.logoutTip'), {
				confirmButtonText: 'OK',
			})
				.then(() => {
					ElMessage({
						type: 'success',
						message: 'Delete completed',
					});
				})
				.catch(() => {
					ElMessage({
						type: 'info',
						message: 'Delete canceled',
					});
				});
		},

		/**
		 * @description: 开始租户切换
		 * @param {string} fromTenantId - 原租户ID
		 * @param {string} toTenantId - 目标租户ID
		 */
		startTenantSwitching(fromTenantId: string, toTenantId: string) {
			this.tenantSwitching = {
				isActive: true,
				fromTenantId,
				toTenantId,
				progress: 0,
				currentStep: 'validating',
				error: null,
			};
		},

		/**
		 * @description: 更新切换进度
		 * @param {number} progress - 进度百分比（0-100）
		 * @param {string} currentStep - 当前步骤key
		 */
		updateProgress(progress: number, currentStep: string) {
			if (this.tenantSwitching.isActive) {
				this.tenantSwitching.progress = progress;
				this.tenantSwitching.currentStep = currentStep;
			}
		},

		/**
		 * @description: 设置切换错误
		 * @param {string} error - 错误消息
		 */
		setTenantError(error: string) {
			if (this.tenantSwitching.isActive) {
				this.tenantSwitching.error = error;
				this.tenantSwitching.progress = 0;
			}
		},

		/**
		 * @description: 设置取消状态（显示回退视觉效果）
		 */
		setTenantCancelling() {
			if (this.tenantSwitching.isActive) {
				this.tenantSwitching.currentStep = 'cancelling';
			}
		},

		/**
		 * @description: 重置租户切换状态
		 */
		resetTenantSwitching() {
			this.tenantSwitching = {
				isActive: false,
				fromTenantId: null,
				toTenantId: null,
				progress: 0,
				currentStep: '',
				error: null,
			};
		},
	},
});

// Need to be used outside the setup
export function useUserStoreWithOut() {
	return useUserStore(store);
}
