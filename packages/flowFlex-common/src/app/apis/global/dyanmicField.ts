import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';
import { DynamicApiResponse, DynamciFile, DynamicList, DynamicSearch } from '#/dynamic';

const globSetting = useGlobSetting();

const Api = () => {
	return {
		dynamicField: `${globSetting.apiProName}/integration/dynamic-fields/${globSetting.apiVersion}`,

		fieldsList: `${globSetting.apiProName}/ow/dynamic-data/${globSetting.apiVersion}/properties`,
	};
};

export function getDynamicField(): Promise<DynamicApiResponse<DynamciFile[]>> {
	return defHttp.get({ url: Api().dynamicField });
}

export function dynamicFieldList(
	params?: DynamicSearch
): Promise<DynamicApiResponse<DynamicList[]>> {
	return defHttp.get({ url: Api().fieldsList, params });
}
