import { addLoginActivity } from '@/apis/pass/notify';
import { useUserStoreWithOut } from '@/stores/modules/user';
import { useGlobSetting } from '@/settings';
import { getItem, isIframe, setItem } from './utils';
import { verifyTicket } from '@/apis/login/user';
import { ProjectEnum } from '@/enums/appEnum';
import WaveLoading from './waveLoading';
import { router } from '@/router';
import dayjs from 'dayjs';
import { getEnv } from './env';
import { removeIdmParams } from '@/utils/urlUtils';

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
	try {
		const userStore = useUserStoreWithOut();
		if (oauth) {
			userStore.setIsLogin(true);
		}

		const currentEnv = getEnv();
		let res;
		let refreshToken, expiresIn, token, tokenType;
		if (currentEnv === 'development') {
			// Development 环境：维持原逻辑
			res = await verifyTicket({
				ticket,
				appId: ProjectEnum.WFE,
			});
			// 旧逻辑的参数结构
			({ refreshToken, expiresIn, token, tokenType } = res);
		} else {
			res = await verifyTicket({
				ticket,
				appId: ProjectEnum.WFE,
			});
			({ refreshToken, expiresIn, token, tokenType } = res);
		}
		const currentDate = dayjs(new Date()).unix();
		userStore.setTokenobj({
			accessToken: {
				token: token,
				expire: +currentDate + +expiresIn,
				tokenType: tokenType,
			},
			refreshToken: refreshToken,
		});
		detailUrlQuery();
	} catch {
		toIDMLogin('logout');
	}
}

export function toIDMLogin(type = 'Switch') {
	WaveLoading.service({
		lock: true,
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
	}
	removeIdmParams();
}

export function Logout(type?: string) {
	WaveLoading.service({
		lock: true,
		background: '#0f0f23',
	});

	if (type != 'logout') {
		localStorage.setItem('href', window.location.href);
	}

	const currentEnv = getEnv();
	let urlParameter = '';
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
