import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';
import {
	DynamicApiResponse,
	DynamicList,
	DynamicSearch,
	CreateDynamicFieldParams,
	DynamicApiListResponse,
} from '#/dynamic';

const globSetting = useGlobSetting();

const Api = () => {
	return {
		fieldsList: `${globSetting.apiProName}/ow/dynamic-data/${globSetting.apiVersion}/properties`,
	};
};

export function getDynamicField(params?: {
	workflowId: string;
}): Promise<DynamicApiResponse<DynamicList[]>> {
	return defHttp.get({ url: Api().fieldsList, params });
}

export function deleteDynamicField(id: string): Promise<DynamicApiResponse<string>> {
	return defHttp.delete({ url: `${Api().fieldsList}/${id}` });
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

export function exportDynamicFields(params?: any): Promise<any> {
	return defHttp.get({ url: `${Api().fieldsList}/export-excel`, params, responseType: 'blob' });
}

export function batchIdsDynamicFields(params: {
	ids: string[];
}): Promise<DynamicApiResponse<DynamicList[]>> {
	return defHttp.post({ url: `${Api().fieldsList}/batch `, params });
}
