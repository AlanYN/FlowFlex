import { addLoginActivity, wujieCrmTokenApi } from '@/apis/pass/notify';
import { useUserStoreWithOut } from '@/stores/modules/user';
import { useGlobSetting } from '@/settings';
import { getItem, isIframe, setItem } from './utils';
import { getSSOToken } from '@/apis/login/user';
import { ProjectEnum } from '@/enums/appEnum';
import { PageEnum } from '@/enums/pageEnum';
import { ElLoading } from 'element-plus';
import { router } from '@/router';
import dayjs from 'dayjs';

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

export async function passSsoToken(code, oauth) {
	const userStore = useUserStoreWithOut();
	if (oauth) {
		userStore.setIsLogin(true);
	}
	const res = await getSSOToken({
		code,
		redirectUrl: window.location.origin,
		clientid: globSetting.ssoCode,
	});
	const { access_token, id_token, expires_in, refresh_token, token_type } = res; //, scope
	if (access_token && access_token != '') {
		const currentDate = dayjs(new Date()).unix();
		userStore.setTokenobj({
			accessToken: {
				token: access_token,
				expire: +currentDate + +expires_in,
				tokenType: token_type,
			},
			refreshToken: refresh_token,
			tripartiteToken: id_token,
		});
		// addActivity();
		await userStore.afterLoginAction(false);
		detailUrlQuery();
	} else {
		throw 'Failed to obtain token'; //获取token失败
	}
}

export function toIAMLogin(type = 'Switch') {
	if (isoldEnvironment()) {
		router.push(PageEnum.BASE_LOGIN as string);
		return;
	}
	ElLoading.service({
		lock: true,
		text: 'Loading',
		background: 'rgba(0, 0, 0, 0.7)',
	});

	if (type != 'logout') {
		localStorage.setItem('href', window.location.href);
	}
	let urlParameter = '';
	if (globSetting.environment == 'UNIS') {
		urlParameter = `redirect_uri=${encodeURIComponent(window.location.origin)}&appId=${
			ProjectEnum.CRM
		}&action_type=${type}&theme=${localStorage.getItem('theme')}`;
		window.open(`${globSetting.ssoURL}oauth?${urlParameter}`, '_self');
	} else if (globSetting.environment == 'ITEM') {
		if (type == 'logout') {
			urlParameter = `post_logout_redirect_uri=${encodeURIComponent(
				window.location.origin
			)} `;
			window.open(`${globSetting.ssoURL}oauth2/logout?${urlParameter}`, '_self');
		} else {
			urlParameter = `response_type=code&client_id=${
				globSetting.ssoCode
			}&scope=${'profile email phone openid'}&redirect_uri=${encodeURIComponent(
				window.location.origin
			)}&state=${'abcxyz'}`;
			window.open(`${globSetting.ssoURL}oauth2/authorize?${urlParameter}`, '_self');
		}
	}
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

export function detailUrlQuery() {
	if (localStorage.getItem('href')) {
		window.location.href = localStorage.getItem('href') as string;
		localStorage.removeItem('href');
	} else {
		window.location.href = window.location.origin;
	}
}

export function passLogout(type?: string) {
	router.push(PageEnum.BASE_LOGIN as string);
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
	const res = (await wujieCrmTokenApi(params)) as any;
	if (res.code == '200') {
		const { accessToken, tokenType, expiresIn, user } = res.data;
		const currentDate = dayjs(new Date()).unix();
		await userStore.setTokenobj({
			accessToken: {
				token: accessToken,
				expire: +currentDate + +expiresIn,
				tokenType: tokenType,
			},
			refreshToken: accessToken,
		});

		await userStore.setUserInfo({
			appCode: params.appCode,
			tenantId: params.tenantId,
			...user,
		});
		if (currentRoute) {
			router.push(currentRoute);
		}
	} else {
		throw 'Failed to obtain token'; //获取token失败
	}
}
