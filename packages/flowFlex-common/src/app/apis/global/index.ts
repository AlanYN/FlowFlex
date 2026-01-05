import { defHttp } from '@/apis/axios';

import { useGlobSetting } from '@/settings';
import { FlowflexUser } from '#/golbal';

const globSetting = useGlobSetting();

const Api = (id?: string | number) => {
	return {
		tableView: `${globSetting.apiProName}/dynamic-tables/${globSetting.apiVersion}/views`,
		dynamicTable: `${globSetting.apiProName}/dynamic-tables/${globSetting.apiVersion}/modules/${id}/fields`,

		dynamicTableDate: `${globSetting.apiProName}/dynamic-tables/${globSetting.apiVersion}/data/${id}`,

		uploadFile: `${globSetting.apiProName}/shared/${globSetting.apiVersion}/files`,

		delete: `${globSetting.apiProName}/modules/${globSetting.apiVersion}/${id}/datas/batch`,

		sendEmailCode: `${globSetting.apiProName}/ow/users/send-verification-code`,

		flowflexUser: `${globSetting.apiProName}/ow/users/tree`,

		changeLog: `${globSetting.apiProName}/ow/change-logs/${globSetting.apiVersion}/business`,

		userPermissionsByMenu: `${globSetting.idmUrl}/api/v1/menus`,

		userPermissions: `${globSetting.idmUrl}/api/v1/users/current/permissions`,

		userList: `${globSetting.apiProName}/ow/onboardings/${globSetting.apiVersion}/${id}/authorized-users`,

		phoneNumber: `${globSetting.apiProName}/shared/${globSetting.apiVersion}/dictionary/phone-number-prefixes`,
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

export function getFlowflexUser(params: {
	pageIndex?: number;
	pageSize?: number;
	searchText?: string;
	team?: string;
}): Promise<{
	code: string;
	msg: string;
	data: FlowflexUser[];
}> {
	return defHttp.get({ url: `${Api().flowflexUser}`, params });
}

export function getChangeLogs(id, params) {
	return defHttp.get({ url: `${Api().changeLog}/${id}`, params });
}

export function menuFunctionPermission(menuId: string) {
	return defHttp.get({ url: `${Api().userPermissionsByMenu}/${menuId}/permissions` });
}

export function userPermissions() {
	return defHttp.get({ url: `${Api().userPermissions}` });
}

export function findUserList(id: string) {
	return defHttp.get({ url: `${Api(id).userList}` });
}

export function getPhoneAreaEnum() {
	return defHttp.get({ url: Api().phoneNumber });
}
