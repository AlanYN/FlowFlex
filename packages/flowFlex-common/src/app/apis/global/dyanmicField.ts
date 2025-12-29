import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';
import {
	DynamicApiResponse,
	DynamciFile,
	DynamicList,
	DynamicSearch,
	CreateDynamicFieldParams,
	DynamicApiListResponse,
} from '#/dynamic';

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
): Promise<DynamicApiListResponse<DynamicList[]>> {
	return defHttp.post({ url: `${Api().fieldsList}/query`, params });
}

export function createDynamicField(
	params: CreateDynamicFieldParams
): Promise<DynamicApiResponse<string>> {
	return defHttp.post({ url: Api().fieldsList, params });
}

export function updateDynamicField(
	id: string,
	params: CreateDynamicFieldParams
): Promise<DynamicApiResponse<string>> {
	return defHttp.put({ url: `${Api().fieldsList}/${id}`, params });
}
