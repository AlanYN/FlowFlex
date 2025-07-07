import type {
	AxiosRequestConfig,
	AxiosInstance,
	AxiosResponse,
	AxiosError,
	InternalAxiosRequestConfig,
} from 'axios';
import type { RequestOptions, Result, UploadFileParams } from '@/../types/axios';
import type { CreateAxiosOptions, TokenObj } from './axiosTransform'; //
import axios from 'axios';
import { AxiosCanceler } from './axiosCancel';
import { isFunction } from '@/utils/is';
import { cloneDeep } from 'lodash-es';
import { ContentTypeEnum, RequestEnum } from '@/enums/httpEnum';
import { getTokenobj, setAuthCache } from '@/utils/auth';
import { TOKENOBJ_KEY } from '@/enums/cacheEnum';
import { parseJWT } from '@/utils';
import { useUserStoreWithOut } from '@/stores/modules/user';
import { UnisApi } from '@/apis/login/user';
import { isoldEnvironment } from '@/utils/threePartyLogin';
// import { ElMessage } from 'element-plus';
import dayjs from 'dayjs';
import qs from 'qs';
import { useGlobSetting } from '@/settings';

export * from './axiosTransform';

const globSetting = useGlobSetting();

/**
 * @description:  axios module
 */
export class VAxios {
	private axiosInstance: AxiosInstance;
	private readonly options: CreateAxiosOptions;
	private refreshPromise: Promise<any> | null = null;

	constructor(options: CreateAxiosOptions) {
		this.options = options;
		this.axiosInstance = axios.create(options);
		this.setupInterceptors();
	}

	/**
	 * @description:  Create axios instance
	 */
	private createAxios(config: CreateAxiosOptions): void {
		this.axiosInstance = axios.create(config);
	}

	private getTransform() {
		const { transform } = this.options;
		return transform;
	}

	getAxios(): AxiosInstance {
		return this.axiosInstance;
	}

	/**
	 * @description: Reconfigure axios
	 */
	configAxios(config: CreateAxiosOptions) {
		if (!this.axiosInstance) {
			return;
		}
		this.createAxios(config);
	}

	/**
	 * @description: Set general header
	 */
	setHeader(headers: any): void {
		if (!this.axiosInstance) {
			return;
		}
		Object.assign(this.axiosInstance.defaults.headers, headers);
	}

	/**
	 * @description: Interceptor configuration 拦截器配置
	 */
	private setupInterceptors() {
		// const transform = this.getTransform();
		const {
			axiosInstance,
			options: { transform },
		} = this;
		if (!transform) {
			return;
		}
		const {
			requestInterceptors,
			requestInterceptorsCatch,
			responseInterceptors,
			responseInterceptorsCatch,
		} = transform;

		const axiosCanceler = new AxiosCanceler();

		// Request interceptor configuration processing
		this.axiosInstance.interceptors.request.use((config: InternalAxiosRequestConfig) => {
			// If cancel repeat request is turned on, then cancel repeat request is prohibited
			const requestOptions =
				(config as unknown as any).requestOptions ?? this?.options?.requestOptions;
			const ignoreCancelToken = requestOptions?.ignoreCancelToken ?? true;
			!ignoreCancelToken && axiosCanceler.addPending(config);
			if (requestInterceptors && isFunction(requestInterceptors)) {
				config = requestInterceptors(config, this.options);
			}
			return config;
		}, undefined);

		// 请求拦截器错误捕获
		requestInterceptorsCatch &&
			isFunction(requestInterceptorsCatch) &&
			this.axiosInstance.interceptors.request.use(undefined, requestInterceptorsCatch);

		// 响应结果拦截器处理
		this.axiosInstance.interceptors.response.use((res: AxiosResponse<any>) => {
			// if (res && res.data && res.data.status == 'error' && res.data.message) {
			// 	ElMessage.error(res.data.message);
			// }
			res && axiosCanceler.removePending(res.config);
			if (responseInterceptors && isFunction(responseInterceptors)) {
				res = responseInterceptors(res);
			}
			return res;
		}, undefined);

		// 响应结果拦截器错误捕获
		responseInterceptorsCatch &&
			isFunction(responseInterceptorsCatch) &&
			this.axiosInstance.interceptors.response.use(undefined, (error) => {
				return responseInterceptorsCatch(axiosInstance, error);
			});
	}

	/**
	 * @description:  File Upload
	 */
	uploadFile<T = any>(config: AxiosRequestConfig, params: UploadFileParams) {
		const formData = new window.FormData();
		const customFilename = params.name || 'file';

		if (params.filename) {
			formData.append(customFilename, params.file, params.filename);
		} else {
			formData.append(customFilename, params.file);
		}

		if (params.data) {
			Object.keys(params.data).forEach((key) => {
				const value = params.data![key];
				if (Array.isArray(value)) {
					value.forEach((item) => {
						formData.append(`${key}[]`, item);
					});
					return;
				}

				formData.append(key, params.data![key]);
			});
		}
		return this.axiosInstance.request<T>({
			...config,
			method: 'POST',
			data: formData,
			headers: {
				'Content-type': ContentTypeEnum.FORM_DATA,
				// @ts-ignore
				ignoreCancelToken: true,
			},
			timeout: 60 * 100000,
		});
	}

	// support form-data
	supportFormData(config: AxiosRequestConfig) {
		const headers = config.headers || this.options.headers;
		const contentType = headers?.['Content-Type'] || headers?.['content-type'];

		if (
			contentType !== ContentTypeEnum.FORM_URLENCODED ||
			!Reflect.has(config, 'data') ||
			config.method?.toUpperCase() === RequestEnum.GET
		) {
			return config;
		}

		return {
			...config,
			data: qs.stringify(config.data, { arrayFormat: 'brackets' }),
		};
	}

	get<T = any>(config: AxiosRequestConfig, options?: RequestOptions): Promise<T> {
		return this.request({ ...config, method: 'GET' }, options);
	}

	post<T = any>(config: AxiosRequestConfig, options?: RequestOptions): Promise<T> {
		return this.request({ ...config, method: 'POST' }, options);
	}

	put<T = any>(config: AxiosRequestConfig, options?: RequestOptions): Promise<T> {
		return this.request({ ...config, method: 'PUT' }, options);
	}

	delete<T = any>(config: AxiosRequestConfig, options?: RequestOptions): Promise<T> {
		return this.request({ ...config, method: 'DELETE' }, options);
	}

	patch<T = any>(config: AxiosRequestConfig, options?: RequestOptions): Promise<T> {
		return this.request({ ...config, method: 'PATCH' }, options);
	}

	fetch<T = any>(config: AxiosRequestConfig, options?: RequestOptions): Promise<T> {
		return this.request({ ...config, method: 'POST' }, options, true);
	}

	async request<T = any>(
		config: AxiosRequestConfig,
		options?: RequestOptions,
		isfetch = false
	): Promise<T> {
		let conf: CreateAxiosOptions = cloneDeep(config);
		// cancelToken 如果被深拷贝，会导致最外层无法使用cancel方法来取消请求
		if (config.cancelToken) {
			conf.cancelToken = config.cancelToken;
		}

		if (config.signal) {
			conf.signal = config.signal;
		}
		const transform = this.getTransform();

		const { requestOptions } = this.options;
		const opt: RequestOptions = Object.assign({}, requestOptions, options);

		const { beforeRequestHook, requestCatchHook, transformResponseHook } = transform || {};
		if (beforeRequestHook && isFunction(beforeRequestHook)) {
			conf = beforeRequestHook(conf, opt);
		}
		conf.requestOptions = opt;
		conf = this.supportFormData(conf);

		// 暂时没有刷新token 先注释掉下边内容

		const useUserStore = useUserStoreWithOut();
		const tokenObj = getTokenobj() as TokenObj;
		if (tokenObj) {
			let currentDate = dayjs(new Date()).unix();
			const givenDate = +tokenObj.accessToken.expire;
			if ((givenDate < currentDate || isNaN(givenDate)) && tokenObj.refreshToken) {
				// console.log('检测本地token过期了,过期时间：', givenDate, 'token是：', tokenObj);
				if (!this.refreshPromise) {
					this.refreshPromise = this.getNewToken({
						refreshToken: tokenObj.refreshToken,
						accessToken: tokenObj.accessToken.token,
					});
				}
				try {
					const res = await this.refreshPromise;
					this.refreshPromise = null;
					const isOldEnv = isoldEnvironment();
					if (isOldEnv && res.data.data.refreshToken) {
						const { accessToken, refreshToken } = res.data.data;
						currentDate = dayjs(new Date()).unix();
						setAuthCache(TOKENOBJ_KEY, {
							accessToken: {
								token: accessToken.token,
								expire: +currentDate + +parseJWT(accessToken.token).exp,
								tokenType: 'Bearer',
							},
							refreshToken: refreshToken.token,
						});
					} else if (!isOldEnv && res.data.refreshToken) {
						const { expiresIn, refreshToken, token, tokenType } = res.data;
						currentDate = dayjs(new Date()).unix();
						setAuthCache(TOKENOBJ_KEY, {
							accessToken: {
								token: token,
								expire: +currentDate + +expiresIn,
								tokenType: tokenType,
							},
							refreshToken: refreshToken,
						});
					} else if (!isOldEnv && res.data.refresh_token) {
						const { access_token, expires_in, refresh_token, token_type } = res.data;
						currentDate = dayjs(new Date()).unix();
						setAuthCache(TOKENOBJ_KEY, {
							accessToken: {
								token: access_token,
								expire: +currentDate + +expires_in,
								tokenType: token_type,
							},
							refreshToken: refresh_token,
						});
					} else {
						useUserStore.logout();
					}
				} catch (error) {
					this.refreshPromise = null;
					useUserStore.logout();
				}
			}
		}

		if (isfetch) {
			const tokenObj = getTokenobj() as TokenObj;
			const token = tokenObj?.accessToken?.token;
			return new Promise((resolve, reject) => {
				console.log('signal', conf.signal);
				fetch(conf.url as string, {
					method: 'POST', // 或 GET，根据接口要求
					headers: {
						'Content-Type': 'application/json',
						Authorization: 'Bearer ' + token,
					},
					body: JSON.stringify(conf.data), // 如果有请求体
					signal: conf?.signal as AbortSignal,
				})
					.then((res) => {
						if (transformResponseHook && isFunction(transformResponseHook)) {
							try {
								const ret = transformResponseHook(res as any, {
									...opt,
									isReturnNativeResponse: true,
								});
								if (res) {
									resolve(ret);
								} else {
									reject(res);
								}
							} catch (err) {
								reject(err || new Error('request error!'));
							}
							return;
						}
						resolve(res as unknown as Promise<T>);
					})
					.catch((e: Error | AxiosError) => {
						if (requestCatchHook && isFunction(requestCatchHook)) {
							reject(requestCatchHook(e, opt));
							return;
						}
						if (axios.isAxiosError(e)) {
							// rewrite error message from axios in here
						}
						reject(e);
					});
			});
		}
		return new Promise((resolve, reject) => {
			this.axiosInstance
				.request<any, AxiosResponse<Result>>(conf)
				.then((res: AxiosResponse<Result>) => {
					if (transformResponseHook && isFunction(transformResponseHook)) {
						try {
							const ret = transformResponseHook(res, opt);
							if (res) {
								resolve(ret);
							} else {
								reject(res);
							}
						} catch (err) {
							reject(err || new Error('request error!'));
						}
						return;
					}
					resolve(res as unknown as Promise<T>);
				})
				.catch((e: Error | AxiosError) => {
					if (requestCatchHook && isFunction(requestCatchHook)) {
						reject(requestCatchHook(e, opt));
						return;
					}
					if (axios.isAxiosError(e)) {
						// rewrite error message from axios in here
					}
					reject(e);
				});
		});
	}

	async getNewToken(parms): Promise<any> {
		if (globSetting.environment == 'UNIS') {
			return new Promise((resolve, reject) => {
				const RefreshTokenUrl = UnisApi().getNewToken;
				this.axiosInstance
					.put(RefreshTokenUrl, parms)
					.then((res) => {
						console.log('换取token成功');
						resolve(res);
					})
					.catch((err) => {
						reject(err);
					});
			});
		} else {
			return new Promise((resolve, reject) => {
				const RefreshTokenUrl = UnisApi().getIAMRefreshToken;
				this.axiosInstance
					.post(RefreshTokenUrl, { ...parms, clientId: globSetting.ssoCode })
					.then((res) => {
						console.log('换取token成功');
						resolve(res);
					})
					.catch((err) => {
						reject(err);
					});
			});
		}
	}
}
