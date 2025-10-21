import { addLoginActivity } from '@/apis/pass/notify';
import { useUserStoreWithOut } from '@/stores/modules/user';
import { useGlobSetting } from '@/settings';
import { getItem, isIframe, setItem } from './utils';
import { verifyTicket, getSSOToken } from '@/apis/login/user';
import { ProjectEnum } from '@/enums/appEnum';
import { ElLoading } from 'element-plus';
import { router } from '@/router';
import dayjs from 'dayjs';
import { getEnv } from './env';

const globSetting = useGlobSetting();

export function addActivity(type = 'Switch') {
	const userStore = useUserStoreWithOut();
	if (userStore.getIsLogin || isIframe()) return;

	addLoginActivity({
		// 用户id
		idmUserId: userStore.getUserInfo.userId,
		// 设备标识
		device: ``,
		// 平台
		platform: 'Web',
		// 自己的系统标识
		applicationCode: globSetting.ssoCode,
		// type： login | Switch
		activityType: type,
	});
	userStore.setIsLogin(true);
}

export async function formIDMLogin(ticket, oauth, state) {
	const userStore = useUserStoreWithOut();
	if (oauth) {
		userStore.setIsLogin(true);
	}

	const currentEnv = getEnv();
	let res;
	let refreshToken, expiresIn, token, tokenType, userId;

	if (currentEnv === 'development') {
		// Development 环境：维持原逻辑
		res = await verifyTicket({
			ticket,
			appId: ProjectEnum.WFE,
		});
		// 旧逻辑的参数结构
		({ refreshToken, expiresIn, token, tokenType, userId } = res);
	} else {
		// 其他环境：使用 getSSOToken 接口
		res = await getSSOToken({
			code: ticket,
			redirectUrl: window.location.origin,
			clientid: globSetting.ssoCode,
		});
		// 新接口的参数结构适配
		refreshToken = res.refresh_token;
		expiresIn = res.expires_in;
		token = res.access_token;
		tokenType = res.token_type;
		// 从 access_token 中解析用户信息（JWT token）
		try {
			const tokenPayload = JSON.parse(atob(res.access_token.split('.')[1]));
			userId = tokenPayload.data?.user_id;
		} catch (error) {
			console.error('解析 access_token 失败:', error);
			userId = null;
		}
	}
	userStore.setUserInfo({
		...userStore.getUserInfo,
		userId,
	});
	const currentDate = dayjs(new Date()).unix();
	userStore.setTokenobj({
		accessToken: {
			token: token,
			expire: +currentDate + +expiresIn,
			tokenType: tokenType,
		},
		refreshToken: refreshToken,
	});
	await userStore.afterLoginAction(false);
	detailUrlQuery();

	// 获取当前URL，移除SSO参数，保留原始路径
	// const currentUrl = new URL(window.location.href);
	// const originalPath = currentUrl.pathname;
	// const originalSearch = new URLSearchParams(currentUrl.search);

	// 移除SSO相关参数
	// originalSearch.delete('ticket');
	// originalSearch.delete('oauth');
	// originalSearch.delete('userId');
	// originalSearch.delete('state');
	// originalSearch.delete('code');

	// if (currentEnv === 'development') {
	// 	// 构建干净的URL
	// 	const cleanUrl =
	// 		originalPath + (originalSearch.toString() ? '?' + originalSearch.toString() : '');

	// 	// 跳转到原始页面
	// 	window.location.href = cleanUrl;
	// } else {
	// 	const redirectUrl = decodeURIComponent(state);
	// 	window.location.href = redirectUrl;
	// }
}

export function toIDMLogin(type = 'Switch') {
	ElLoading.service({
		lock: true,
		text: 'Loading',
		background: 'rgba(0, 0, 0, 0.7)',
	});

	if (type != 'logout') {
		localStorage.setItem('href', window.location.href);
	}

	const currentEnv = getEnv();
	let urlParameter = '';
	console.log('currentEnv:', currentEnv);
	// 如果是 stage 环境，使用新的 SSO 验证逻辑
	// if (currentEnv === 'stage' || currentEnv === 'production') {
	// 	urlParameter = `response_type=code&client_id=${
	// 		globSetting.ssoCode
	// 	}&scope=${'profile email phone openid'}&redirect_uri=${encodeURIComponent(
	// 		window.location.origin
	// 	)}&state=${encodeURIComponent(window.location.href)}`;
	// 	window.open(`${globSetting.ssoURL}oauth2/authorize?${urlParameter}`, '_self');
	// } else {
	// 其他环境保持原有逻辑
	urlParameter = `redirect_uri=${encodeURIComponent(window.location.href)}&appId=${
		ProjectEnum.WFE
	}&action_type=${type}&theme=${localStorage.getItem('theme')}&primary=${localStorage.getItem(
		'primary'
	)}`;
	window.open(`${globSetting.idmUrl}/oauth?${urlParameter}`, '_self');
	// }
}

export function setEnvironment(type: string) {
	setItem('loginType', type);
}

export function getEnvironment() {
	return getItem('loginType');
}

export function isoldEnvironment() {
	return getItem('loginType') == 'self';
}

export function setAppCode(appCode: string = 'default') {
	setItem('appCode', appCode);
}

export function getAppCode() {
	return getItem('appCode') || 'default';
}

export function detailUrlQuery() {
	if (localStorage.getItem('href')) {
		window.location.href = localStorage.getItem('href') as string;
		localStorage.removeItem('href');
	} else {
		window.location.href = window.location.origin;
	}
}

export function Logout(type?: string) {
	ElLoading.service({
		lock: true,
		text: 'Loading',
		background: 'rgba(0, 0, 0, 0.7)',
	});

	if (type != 'logout') {
		localStorage.setItem('href', window.location.href);
	}

	const currentEnv = getEnv();
	console.log('currentEnv:', currentEnv);
	// 如果是 stage 环境，使用新的 SSO 验证逻辑
	// if (currentEnv === 'stage' || currentEnv === 'production') {
	// 	urlParameter = `post_logout_redirect_uri=${encodeURIComponent(
	// 		window.location.origin
	// 	)}&appId=${ProjectEnum.WFE}&action_type=${type}&theme=${localStorage.getItem(
	// 		'theme'
	// 	)}&primary=${localStorage.getItem('primary')}`;
	// 	window.open(`${globSetting.ssoURL}oauth2/logout?${urlParameter}`, '_self');
	// } else {
	let urlParameter = '';
	urlParameter = `redirect_uri=${encodeURIComponent(window.location.origin)}&appId=${
		ProjectEnum.WFE
	}&action_type=${type}&theme=${localStorage.getItem('theme')}&primary=${localStorage.getItem(
		'primary'
	)}`;
	window.open(`${globSetting.idmUrl}/oauth?${urlParameter}`, '_self');
	// }
}

export async function wujieCrmToken(
	params: {
		appCode: string;
		tenantId: string;
		authorizationToken: string;
	},
	currentRoute: string
) {
	const userStore = useUserStoreWithOut();
	await userStore.setTokenobj({
		accessToken: {
			token: params.authorizationToken,
			tokenType: 'Bearer',
		},
		refreshToken: params.authorizationToken,
	});
	setAppCode(params.appCode);
	userStore.afterLoginAction(true);
	if (currentRoute) {
		router.push(currentRoute);
	}
}
