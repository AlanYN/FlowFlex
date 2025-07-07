import { defHttp } from '@/apis/axios';
import { getTokenobj } from '@/utils/auth';
import { useGlobSetting } from '@/settings';

const itemApi = () => {
	const globSetting = useGlobSetting();
	return {
		// sso Activities
		ssoLoginActivity: `${globSetting.iamUrl}/api/idm-app/user/sso/add-login-activity`,
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
