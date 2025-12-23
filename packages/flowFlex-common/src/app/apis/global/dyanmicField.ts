import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';
import { DynamicApiResponse, DynamciFile } from '#/dynamic';

const globSetting = useGlobSetting();

const Api = () => {
	return {
		dynamicField: `${globSetting.apiProName}/integration/dynamic-fields/${globSetting.apiVersion}`,
	};
};

export function getDynamicField(): Promise<DynamicApiResponse<DynamciFile[]>> {
	return defHttp.get({ url: Api().dynamicField });
}
