// axios配置  可自行根据项目进行更改，只需更改该文件即可，其他文件可以不动
// The axios configuration can be changed according to the project, just change the file, other files can be left unchanged

import type { AxiosInstance, AxiosResponse } from 'axios';
import { clone } from 'lodash-es';
import type { RequestOptions, Result } from '@/../types/axios';
import type { AxiosTransform, CreateAxiosOptions, TokenObj } from './axiosTransform';
import { VAxios } from './Axios';
import { checkStatus } from './checkStatus';
import { RequestEnum, ContentTypeEnum } from '@/enums/httpEnum';
import { isString } from '@/utils/is';
import { getTokenobj } from '@/utils/auth';
import { setObjToUrlParams, deepMerge } from '@/utils';
import { useI18n } from '@/hooks/useI18n';
import { joinTimestamp, formatRequestDate } from './helper';
import { getProcessErrorResponseMessage } from '@/utils/utils';
import { ElMessage } from 'element-plus';
import { AxiosCanceler } from './axiosCancel';
import { getTimeZoneInfo } from '@/hooks/time';
import { useUserStoreWithOut } from '@/stores/modules/user';
import { useWujie } from '@/hooks/wujie/micro-app.config';
import axios from 'axios';
import qs from 'qs';
import { ProjectEnum } from '@/enums/appEnum';

import { useGlobSetting } from '@/settings';
import { getAppCode } from '@/utils/threePartyLogin';

const globSetting = useGlobSetting();

const urlPrefix = globSetting.urlPrefix;

/**
 * @description: 数据处理，方便区分多种处理方式
 */
const transform: AxiosTransform = {
	/**
	 * @description: 处理响应数据。如果数据不是预期格式，可直接抛出错误
	 */
	transformResponseHook: (res: AxiosResponse<Result>, options: RequestOptions) => {
		const { t } = useI18n();
		const { isTransformResponse, isReturnNativeResponse } = options;
		// 是否返回原生响应头 比如：需要获取响应头时使用该属性
		if (isReturnNativeResponse) {
			return res;
		}
		// 不进行任何处理，直接返回
		// 用于页面代码可能需要直接获取code，data，message这些信息时开启
		if (!isTransformResponse) {
			return res?.data || res;
		}
		// 错误的时候返回

		const { data } = res;
		if (!data) {
			// return '[HTTP] Request has no return value';
			throw new Error(t('sys.api.apiRequestFailed'));
		}
		//  这里 code，result，message为 后台统一的字段，需要在 types.ts内修改为项目自己的接口返回格式
		return data;
	},

	// 请求之前处理config
	beforeRequestHook: (config, options) => {
		const {
			apiUrl,
			joinPrefix,
			joinParamsToUrl,
			formatDate,
			joinTime = true,
			urlPrefix,
		} = options;

		if (joinPrefix) {
			config.url = `${urlPrefix}${config.url}`;
		}

		if (apiUrl && isString(apiUrl)) {
			config.url = `${apiUrl}${config.url}`;
		}
		const params = config.params != null ? config.params : {};
		const data = config.data || false;
		formatDate && data && !isString(data) && formatRequestDate(data);
		if (config.method?.toUpperCase() === RequestEnum.GET) {
			if (!isString(params)) {
				// 给 get 请求加上时间戳参数，避免从缓存中拿数据。
				config.params = Object.assign(params || {}, joinTimestamp(joinTime, false));
			} else {
				// 兼容restful风格
				config.url = config.url + params + `${joinTimestamp(joinTime, true)}`;
				config.params = undefined;
			}
		} else {
			if (!isString(params)) {
				formatDate && formatRequestDate(params);
				if (
					Reflect.has(config, 'data') &&
					config.data &&
					(Object.keys(config.data).length > 0 || config.data instanceof FormData)
				) {
					config.data = data;
					config.params = params;
				} else {
					// 非GET请求如果没有提供data，则将params视为data
					// 这块修改 如果有data则还用data 否则采用params
					config.data = config?.data || params;
					config.params = undefined;
				}
				if (joinParamsToUrl) {
					config.url = setObjToUrlParams(
						config.url as string,
						Object.assign({}, config.params, config.data)
					);
				}
			} else {
				// 兼容restful风格
				config.url = config.url + params;
				config.params = undefined;
			}
		}
		return config;
	},

	/**
	 * @description: 请求拦截器处理
	 */
	requestInterceptors: (config, options) => {
		if ((config as Recordable).requestOptions?.AiFields) {
			(config as Recordable).headers['Ai-Fields'] = `${(config as Recordable).requestOptions
				?.AiFields}`;
		}
		// 请求之前处理config
		const tokenObj = getTokenobj() as TokenObj;
		const token = tokenObj?.accessToken?.token;
		const authenticat = tokenObj?.accessToken?.tokenType;

		// Portal Token Management
		// Portal users have limited scope tokens that can only access Portal-specific endpoints
		const portalAccessToken = localStorage.getItem('portal_access_token');
		const isPortalPath =
			window.location.pathname.startsWith('/customer-portal') ||
			window.location.pathname.startsWith('/onboard/sub-portal/portal') ||
			window.location.pathname.startsWith('/portal-access');

		// Priority: Portal Token > Regular Token (for Portal paths)
		// This ensures Portal users use their limited-scope token
		if (
			isPortalPath &&
			portalAccessToken &&
			(config as Recordable)?.requestOptions?.withToken !== false
		) {
			// Use Portal token for Portal pages - has limited scope (scope: portal)
			(config as Recordable).headers.Authorization = `Bearer ${portalAccessToken}`;
			console.log('[Portal Token] Using Portal access token for Portal request');
		} else if (token && (config as Recordable)?.requestOptions?.withToken !== false) {
			// Use regular user token for non-Portal pages or when Portal token is not available
			(config as Recordable).headers.Authorization = authenticat
				? `${authenticat} ${token}`
				: `${options.authenticationScheme} ${token}`;
		} else {
			if (Object.keys((config as Recordable)?.requestOptions).includes('Authorization')) {
				(config as Recordable).headers.Authorization = (config as Recordable)
					?.requestOptions.Authorization;
			}
		}
		const userStore = useUserStoreWithOut();
		(config as Recordable).headers['Time-Zone'] = `${getTimeZoneInfo().timeZone}`;
		(config as Recordable).headers['Application-code'] = `${globSetting.ssoCode}`;
		(config as Recordable).headers['X-App-Code'] = getAppCode();
		(config as Recordable).headers['X-Tenant-Id'] = userStore.getUserInfo?.tenantId;
		(config as Recordable).headers['X-App-Id'] = ProjectEnum.WFE;
		// TODO: 在拦截器配置paramsSerializer
		// const METHOD = config.method?.toUpperCase();
		// if (METHOD === RequestEnum.GET || METHOD === RequestEnum.PUT) {
		// 	// 如果是get或者put请求，且params是数组类型如arr = [1,2]，则转换成arr=1&arr=2
		// 	config.paramsSerializer = function (params) {
		// 		return qs.stringify(params, { arrayFormat: 'repeat' });
		// 	};
		// 	console.log('url', config);
		// }
		return config;
	},

	/**
	 * @description: 响应拦截器处理
	 */
	responseInterceptors: (res: AxiosResponse<any>) => {
		// console.log('res:', res);
		// if (res.data.code != 1) {
		// 	res?.data?.msg && ElMessage.error(res?.data?.msg);
		// }
		return res;
	},

	/**
	 * @description: 响应错误处理
	 */
	responseInterceptorsCatch: (axiosInstance: AxiosInstance, error: any) => {
		const { t } = useI18n();
		const { response, code, message, config } = error || {};
		const errorMessageMode = config?.requestOptions?.errorMessageMode || 'none';
		const msg: string = getProcessErrorResponseMessage(response?.data?.msg) ?? '';
		const err: string = error?.toString?.() ?? '';
		let errMessage = '';
		const axiosCanceler = new AxiosCanceler();
		if (Object.keys(error?.response?.headers).includes('token-expired')) {
			console.log('token过期了'); // 只是token过期��� 不包含其他地方登录了
			axiosCanceler.removeAllPending();
			const { tokenExpiredLogOut } = useWujie();
			if (tokenExpiredLogOut) {
				tokenExpiredLogOut(true);
			}
			window.parent.postMessage({ exceedToken: true }, '*');
		} else if (error.response?.status === 401) {
			// Check if current page is Portal-related (public pages)
			// Portal pages should not trigger logout on 401 errors
			const isPortalPath =
				window.location.pathname.startsWith('/portal-access') ||
				window.location.pathname.startsWith('/customer-portal') ||
				window.location.pathname.startsWith('/onboard/sub-portal');

			if (!isPortalPath) {
				axiosCanceler.removeAllPending();
				const { tokenExpiredLogOut } = useWujie();
				if (tokenExpiredLogOut) {
					tokenExpiredLogOut(true);
				}
				window.parent.postMessage({ exceedToken: true }, '*');
			} else {
				console.log('[Portal] 401 error on Portal page - not triggering logout');
			}
		}
		if (axios.isCancel(error)) {
			return Promise.reject(error);
		}

		try {
			if (code === 'ECONNABORTED' && message.indexOf('timeout') !== -1) {
				errMessage = t('sys.api.apiTimeoutMessage');
			}
			if (err?.includes('Network Error')) {
				errMessage = t('sys.api.networkExceptionMsg');
			}
			if (errMessage) {
				if (errorMessageMode === 'modal') {
					ElMessage.error(t('sys.api.errorTip'));
				} else if (errorMessageMode === 'message') {
					ElMessage.error(errMessage);
				}
				return Promise.reject(error);
			}
		} catch (error) {
			throw new Error(error as unknown as string);
		}

		// 添加请求信息
		const requestInfo = {
			url: config?.url || '',
			method: config?.method || '',
			params: config?.data || config?.params,
		};

		checkStatus(error?.response?.status, msg, errorMessageMode, requestInfo);
	},
};

function createAxios(opt?: Partial<CreateAxiosOptions>) {
	return new VAxios(
		// 深度合并
		deepMerge(
			{
				// See https://developer.mozilla.org/en-US/docs/Web/HTTP/Authentication#authentication_schemes
				authenticationScheme: '', // 不能是固定的 必须是动态的
				timeout: 60 * 1000, // 为了处理bnp接口，设置超时时间为60秒
				// 基础接口地址
				// baseURL: globSetting.apiUrl,

				headers: { 'Content-Type': ContentTypeEnum.JSON },
				// 如果是form-data格式
				// headers: { 'Content-Type': ContentTypeEnum.FORM_URLENCODED },
				// 数据处理方式
				transform: clone(transform),
				paramsSerializer: (params) => {
					return qs.stringify(params, { arrayFormat: 'repeat' });
				},
				// 配置项，下面的选项都可以在独立的接口请求中覆盖
				requestOptions: {
					// 默认将prefix 添加到url
					joinPrefix: true,
					// 是否返回原生响应头 比如：需要获取响应头时使用该属性
					isReturnNativeResponse: false,
					// 需要对返回数据进行处理
					isTransformResponse: false,
					// post请求的时候添加参数到url
					joinParamsToUrl: false,
					// 格式化提交参数时间
					formatDate: true,
					// 消息提示类型
					errorMessageMode: 'message',
					// 接口地址
					// apiUrl: globSetting.apiUrl,
					// 接口拼接地址
					urlPrefix: urlPrefix,
					//  是否加入时间戳
					joinTime: false,
					// 忽略重复请���
					ignoreCancelToken: true,
					// 是否携带token
					withToken: true,
					retryRequest: {
						isOpenRetry: true,
						count: 5,
						waitTime: 100,
					},
				},
			},
			opt || {}
		)
	);
}
export const defHttp = createAxios();

// other api url
// export const otherHttp = createAxios({
//   requestOptions: {
//     apiUrl: 'xxx',
//     urlPrefix: 'xxx',
//   },
// });
