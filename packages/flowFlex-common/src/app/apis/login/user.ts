import { defHttp } from '@/apis/axios';
import type { UserInfoPost, GetUserInfoModel } from '@/apis/model/userModel';
import { useGlobSetting } from '@/settings';

const globSetting = useGlobSetting();

export const UnisApi = () => {
	return {
		UserInfo: `${globSetting.apiProName}/permission-center/${globSetting.apiVersion}/users/user-info`,

		// 获取新token
		getNewToken: `${globSetting.apiProName}/ow/users/refresh-access-token`,

		getSSOToken: `${globSetting.iamUrl}/${globSetting.apiVersion}/oauth/item-iam/token`,

		userGeneral: `${globSetting.iamUrl}/${globSetting.apiVersion}/users/user-general`,
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

export function getSSOToken(params) {
	return defHttp.post({ url: UnisApi().getSSOToken, params });
}

export function setUserGeneral(params) {
	return defHttp.put({ url: UnisApi().userGeneral, params });
}

export function getUserGeneral(params) {
	return defHttp.get({ url: UnisApi().userGeneral, params });
}

export function registerApi(params: {
	email: string;
	password: string;
	confirmPassword: string;
	verificationCode: string;
}) {
	return defHttp.post({ url: Api().register, params });
}
