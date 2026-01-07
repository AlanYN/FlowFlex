import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';

import {
	ListApiResponse,
	MessageList,
	ListApiParams,
	ApiResponse,
	MessageInfo,
	MessageCenterForm,
} from '#/message';

const globSetting = useGlobSetting();

const Api = () => {
	return {
		messageCenter: `${globSetting.apiProName}/ow/messages/${globSetting.apiVersion}`,

		unreadMessageCenter: `${globSetting.apiProName}/ow/messages/${globSetting.apiVersion}/stats/unread`,

		authEmailUrl: `${globSetting.apiProName}/ow/email-binding/${globSetting.apiVersion}/authorize`,
		isBinding: `${globSetting.apiProName}/ow/email-binding/${globSetting.apiVersion}/current`,

		messageFile: `${globSetting.apiProName}/ow/message-attachments/${globSetting.apiVersion}`,

		syncMessage: `${globSetting.apiProName}/ow/email-binding/${globSetting.apiVersion}/sync`,

		unbindMessageCenter: `${globSetting.apiProName}/ow/email-binding/${globSetting.apiVersion}/unbind`,
	};
};

export function messageCenterList(params?: ListApiParams): Promise<ListApiResponse<MessageList[]>> {
	return defHttp.get({ url: Api().messageCenter, params });
}

export function messageCenterInfo(id: string): Promise<ApiResponse<MessageInfo>> {
	return defHttp.get({ url: `${Api().messageCenter}/${id}` });
}

export function sendMessageCenter(params: MessageCenterForm): Promise<ApiResponse<string>> {
	return defHttp.post({ url: `${Api().messageCenter}`, params });
}

export function getMessageUnreadCount(): Promise<ApiResponse<number>> {
	return defHttp.get({ url: `${Api().unreadMessageCenter}` });
}

export function deleteMessage(id: string): Promise<ApiResponse<boolean>> {
	return defHttp.delete({ url: `${Api().messageCenter}/${id}` });
}

export function permanentDeleteMessage(id: string): Promise<ApiResponse<boolean>> {
	return defHttp.delete({ url: `${Api().messageCenter}/${id}/permanent` });
}

export function unstarMessage(id: string): Promise<ApiResponse<boolean>> {
	return defHttp.post({ url: `${Api().messageCenter}/${id}/unstar` });
}
export function starMessage(id: string): Promise<ApiResponse<boolean>> {
	return defHttp.post({ url: `${Api().messageCenter}/${id}/star` });
}

export function archiveMessage(id: string): Promise<ApiResponse<boolean>> {
	return defHttp.post({ url: `${Api().messageCenter}/${id}/archive` });
}

export function unarchiveMessage(id: string): Promise<ApiResponse<boolean>> {
	return defHttp.post({ url: `${Api().messageCenter}/${id}/unarchive` });
}

export function unreadMessage(id: string): Promise<ApiResponse<MessageList[]>> {
	return defHttp.post({ url: `${Api().messageCenter}/${id}/unread` });
}

export function restoreMessage(id: string): Promise<ApiResponse<MessageList[]>> {
	return defHttp.post({ url: `${Api().messageCenter}/${id}/restore` });
}

export function getEmailAuth(): Promise<
	ApiResponse<{
		authorizationUrl: string;
		state: string;
	}>
> {
	return defHttp.get({ url: Api().authEmailUrl });
}

export function getIsBindIng(): Promise<
	ApiResponse<{
		id: string;
		email: string;
		provider: string;
		syncStatus: string;
		lastSyncTime: string | null;
		lastSyncError: string;
		autoSyncEnabled: boolean;
		syncIntervalMinutes: number;
		isTokenValid: boolean;
		tokenExpireTime: string;
	}>
> {
	return defHttp.get({ url: Api().isBinding });
}

export function uploadMessageFile(params: any, onUploadProgress?: (progressEvent: any) => void) {
	return defHttp.uploadFile(
		{
			url: `${Api().messageFile}/upload`,
			onUploadProgress,
		},
		params
	);
}

export function downLoadFile(id: string) {
	return defHttp.get({
		url: `${Api().messageFile}/${id}/download`,
		responseType: 'blob',
		timeout: 10 * 60 * 1000,
	});
}

export function syncMessage(): Promise<ApiResponse<boolean>> {
	return defHttp.post({ url: Api().syncMessage });
}

export function syncMessageFull(): Promise<ApiResponse<boolean>> {
	return defHttp.post({ url: `${Api().syncMessage}/full`, timeout: 10 * 60 * 1000 });
}

export function unbindMessageCenter(): Promise<ApiResponse<boolean>> {
	return defHttp.post({ url: Api().unbindMessageCenter });
}
