import { defHttp } from '@/apis/axios';

import { useGlobSetting } from '@/settings';

const globSetting = useGlobSetting();

const Api = (id?: string | number) => {
	return {
		tableView: `${globSetting.apiProName}/dynamic-tables/${globSetting.apiVersion}/views`,
		dynamicTable: `${globSetting.apiProName}/dynamic-tables/${globSetting.apiVersion}/modules/${id}/fields`,

		dynamicTableDate: `${globSetting.apiProName}/dynamic-tables/${globSetting.apiVersion}/data/${id}`,

		uploadFile: `${globSetting.apiProName}/shared/${globSetting.apiVersion}/files`,

		delete: `${globSetting.apiProName}/modules/${globSetting.apiVersion}/${id}/datas/batch`,

		getFileUrl: `${globSetting.apiProName}/shared/${globSetting.apiVersion}/files/${id}/path`,

		sendEmailCode: `${globSetting.apiProName}/ow/users/send-verification-code`,
	};
};

export function getTableViewInfo(id: string) {
	return defHttp.get({ url: `${Api().tableView}/${id}` });
}

export function getModuleTypeColumns(moduleType: number) {
	return defHttp.get({ url: `${Api(moduleType).dynamicTable}` });
}

export function getDynamicTableData(moduleType: number, params: any) {
	return defHttp.post({ url: `${Api(moduleType).dynamicTableDate}`, params });
}

export function globalUploadFile(params: any, onUploadProgress?: (progressEvent) => void) {
	return defHttp.uploadFile({ url: `${Api().uploadFile}`, onUploadProgress }, params);
}

export function globalDeleteFile(id: string) {
	return defHttp.delete({ url: `${Api().uploadFile}/${id}` });
}

export function getFileUrl(params) {
	return defHttp.get({ url: `${Api(params).getFileUrl}` });
}

export function fileAttachment(params, onDownloadProgress?: (progressEvent) => void) {
	return defHttp.get({
		url: `${Api().uploadFile}/${params}`,
		responseType: 'blob',
		timeout: 60 * 1000, // 修复：60秒超时
		onDownloadProgress,
	});
}

export function sendEmail(params) {
	return defHttp.post({ url: `${Api().sendEmailCode}`, params });
}
