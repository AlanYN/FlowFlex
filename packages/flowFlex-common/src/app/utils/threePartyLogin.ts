import { addLoginActivity } from '@/apis/pass/notify';
import { useUserStoreWithOut } from '@/stores/modules/user';
import { useGlobSetting } from '@/settings';
import { getItem, isIframe, setItem } from './utils';
import { verifyTicket } from '@/apis/login/user';
import { ProjectEnum } from '@/enums/appEnum';
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

export async function formIDMLogin(ticket, oauth) {
	const userStore = useUserStoreWithOut();
	if (oauth) {
		userStore.setIsLogin(true);
	}
	const res = await verifyTicket({
		ticket,
		appId: ProjectEnum.WFE,
	});
	const { refreshToken, expiresIn, token, tokenType, userId } = res;
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
	let urlParameter = '';
	urlParameter = `redirect_uri=${encodeURIComponent(window.location.origin)}&appId=${
		ProjectEnum.WFE
	}&action_type=${type}&theme=${localStorage.getItem('theme')}`;
	window.open(`${globSetting.idmUrl}/oauth?${urlParameter}`, '_self');
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
	let urlParameter = '';
	urlParameter = `redirect_uri=${encodeURIComponent(window.location.origin)}&appId=${
		ProjectEnum.WFE
	}&action_type=${type}&theme=${localStorage.getItem('theme')}`;
	window.open(`${globSetting.idmUrl}/oauth?${urlParameter}`, '_self');
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
