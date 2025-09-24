import { defHttp } from '@/apis/axios';
import type { UserInfoPost, GetUserInfoModel } from '@/apis/model/userModel';
import { useGlobSetting } from '@/settings';

const globSetting = useGlobSetting();

export const UnisApi = () => {
	return {
		UserInfo: `${globSetting.idmUrl}/api/v1/users/current/info`,

		// 获取新token
		getNewToken: `${globSetting.idmUrl}/api/v1/oauth/token`,

		verifyTicket: `${globSetting.idmUrl}/api/v1/oauth/ticket`,
	};
};

const Api = () => {
	return {
		Login: `${globSetting.apiProName}/ow/users/login`,

		emailCodelogin: `${globSetting.apiProName}/ow/users/login-with-code`,
		register: `${globSetting.apiProName}/ow/users/register`,
	};
};

export function loginApi(params) {
	return defHttp.post<UserInfoPost>({ url: Api().Login, params });
}

export function emailCodelogin(params) {
	return defHttp.post({ url: Api().emailCodelogin, params });
}

export function userInfoApi() {
	return defHttp.get<GetUserInfoModel>({ url: UnisApi().UserInfo });
}

export function verifyTicket(params) {
	return defHttp.post({ url: UnisApi().verifyTicket, params });
}

export function registerApi(params: {
	email: string;
	password: string;
	confirmPassword: string;
	verificationCode: string;
}) {
	return defHttp.post({ url: Api().register, params });
}
