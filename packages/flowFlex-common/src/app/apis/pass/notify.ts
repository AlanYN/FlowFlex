import { defHttp } from '@/apis/axios';
import { getTokenobj } from '@/utils/auth';
import { useGlobSetting } from '@/settings';

const itemApi = () => {
	const globSetting = useGlobSetting();
	return {
		// sso Activities
		ssoLoginActivity: `${globSetting.iamUrl}/api/idm-app/user/sso/add-login-activity`,

		wujieCrmToken: `${globSetting.apiProName}/ow/users/third-party-login`,
	};
};

export const addLoginActivity = (params: any) => {
	return defHttp.post(
		{ url: `${itemApi().ssoLoginActivity}`, params },
		{
			withToken: false,
			Authorization: `Bearer ${getTokenobj().tripartiteToken}`,
		}
	);
};

export const wujieCrmTokenApi = (params: any) => {
	return defHttp.post({ url: `${itemApi().wujieCrmToken}`, params });
};
