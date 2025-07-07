import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';

const globSetting = useGlobSetting();

const Api = () => {
	return {
		// 获取时区列表
		timeZone: `${globSetting.apiProName}/User/${globSetting.apiVersion}/TimeZones	`,
	};
};

export function getTimeZoneList(params) {
	return defHttp.get({ url: Api().timeZone, params });
}

export function updateTimeZone(params: any) {
	return defHttp.put({ url: `${Api().timeZone}`, params });
}
