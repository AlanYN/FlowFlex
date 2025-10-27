import type { AxiosRequestConfig } from 'axios';
import { refreshToken as refreshTokenApi } from '@/apis/login/user';
import { getTokenobj } from '@/utils/auth';
import { useUserStoreWithOut } from '@/stores/modules/user';
import type { TokenObj } from './axiosTransform';
import { Logout } from '@/utils/threePartyLogin';

interface PendingRequest {
	config: AxiosRequestConfig;
	resolve: (value: any) => void;
	reject: (reason?: any) => void;
}

/**
 * Token 刷新管理器
 * 处理并发请求场景下的 token 刷新逻辑，确保只调用一次刷新接口
 */
class TokenRefreshManager {
	private isRefreshing = false;
	private pendingRequests: PendingRequest[] = [];

	/**
	 * 检查 token 是否过期
	 */
	private isTokenExpired(): boolean {
		const tokenObj = getTokenobj();
		if (!tokenObj?.accessToken?.expire) {
			return true;
		}

		const currentTime = Math.floor(Date.now() / 1000);
		// 提前 30 秒判断过期，避免边界情况
		return currentTime >= tokenObj.accessToken.expire - 30;
	}

	/**
	 * 刷新 token
	 */
	private async doRefreshToken(): Promise<TokenObj> {
		const tokenObj = getTokenobj();
		if (!tokenObj?.refreshToken) {
			throw new Error('No refresh token available');
		}

		try {
			const userStore = useUserStoreWithOut();
			const response = await refreshTokenApi({
				refreshToken: tokenObj.refreshToken,
			});
			// 更新 token 到 store
			const currentDate = Math.floor(Date.now() / 1000);
			const newTokenObj: TokenObj = {
				accessToken: {
					token: response.access_token || response.token,
					expire: currentDate + (response.expires_in || 3600),
					tokenType: response.token_type || 'Bearer',
				},
				refreshToken: response.refresh_token || tokenObj.refreshToken,
			};

			userStore.setTokenobj(newTokenObj);
			return newTokenObj;
		} catch (error) {
			console.error('Token refresh failed:', error);
			// 刷新失败，清除 token 并跳转登录
			const userStore = useUserStoreWithOut();
			userStore.setTokenobj(undefined);
			userStore.logout(true, 'logout');

			// 通知微前端或父窗口 token 过期
			const { useWujie } = await import('@/hooks/wujie/micro-app.config');
			const { tokenExpiredLogOut } = useWujie();
			if (tokenExpiredLogOut) {
				tokenExpiredLogOut(true);
			}
			window.parent.postMessage({ exceedToken: true }, '*');

			throw error;
		}
	}

	/**
	 * 处理待处理的请求
	 */
	private processPendingRequests(error?: any): void {
		this.pendingRequests.forEach(({ resolve, reject }) => {
			if (error) {
				reject(error);
			} else {
				resolve(null);
			}
		});
		this.pendingRequests = [];
	}

	/**
	 * 刷新 token 并重试请求
	 * @param axiosInstance axios 实例
	 * @param originalConfig 原始请求配置
	 */
	async refreshTokenAndRetry(
		axiosInstance: any,
		originalConfig: AxiosRequestConfig
	): Promise<any> {
		return new Promise((resolve, reject) => {
			// 将当前请求加入待处理队列
			this.pendingRequests.push({
				config: originalConfig,
				resolve,
				reject,
			});

			// 如果已经在刷新中，直接返回
			if (this.isRefreshing) {
				return;
			}

			this.isRefreshing = true;

			this.doRefreshToken()
				.then((newTokenObj) => {
					// 刷新成功，重试所有待处理的请求
					this.pendingRequests.forEach(
						({ config, resolve: pendingResolve, reject: pendingReject }) => {
							// 更新请求头中的 token
							if (config.headers) {
								const tokenType = newTokenObj.accessToken.tokenType || 'Bearer';
								config.headers.Authorization = `${tokenType} ${newTokenObj.accessToken.token}`;
							}

							// 重新发起请求
							axiosInstance.request(config).then(pendingResolve).catch(pendingReject);
						}
					);

					this.pendingRequests = [];
				})
				.catch((error) => {
					// 刷新失败，拒绝所有待处理的请求
					Logout('logout');
					this.processPendingRequests(error);
				})
				.finally(() => {
					this.isRefreshing = false;
				});
		});
	}

	/**
	 * 检查响应是否为 401 未授权错误
	 */
	is401UnauthorizedError(status: number): boolean {
		return status === 401;
	}

	/**
	 * 检查是否需要刷新 token（基于时间）
	 */
	shouldRefreshToken(): boolean {
		return this.isTokenExpired();
	}
}

// 导出单例实例
export const tokenRefreshManager = new TokenRefreshManager();
