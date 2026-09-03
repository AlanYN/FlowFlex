import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';

import {
	WhatsNewPanelResponse,
	WhatsNewDetail,
	WhatsNewAdminListResponse,
	CreateWhatsNewRequest,
	UpdateWhatsNewRequest,
} from '#/whatsNew';

const globSetting = useGlobSetting();

const Api = (id?: string | number) => {
	return {
		unreadCount: `${globSetting.apiProName}/ow/whats-new/${globSetting.apiVersion}/unread-count`,
		panel: `${globSetting.apiProName}/ow/whats-new/${globSetting.apiVersion}/panel`,
		detail: `${globSetting.apiProName}/ow/whats-new/${globSetting.apiVersion}/${id}`,
		read: `${globSetting.apiProName}/ow/whats-new/${globSetting.apiVersion}/${id}/read`,
		readAll: `${globSetting.apiProName}/ow/whats-new/${globSetting.apiVersion}/read-all`,
		adminList: `${globSetting.apiProName}/ow/whats-new/${globSetting.apiVersion}/admin`,
		adminItem: `${globSetting.apiProName}/ow/whats-new/${globSetting.apiVersion}/admin/${id}`,
	};
};

// ========================= 用户端接口 =========================

/**
 * 获取当前用户未读更新数量
 * GET ow/whats-new/v1/unread-count
 */
export function getUnreadCount(): Promise<number> {
	return defHttp.get({ url: Api().unreadCount }).then((res: any) => res?.data ?? res);
}

/**
 * 获取 What's New 面板列表（最多 10 条 Published，含 isRead）
 * GET ow/whats-new/v1/panel
 */
export function getPanel(): Promise<WhatsNewPanelResponse> {
	return defHttp.get({ url: Api().panel }).then((res: any) => res?.data ?? res);
}

/**
 * 获取单条更新详情（含完整富文本 content）
 * GET ow/whats-new/v1/{id}
 */
export function getDetail(id: string): Promise<WhatsNewDetail> {
	return defHttp.get({ url: Api(id).detail }).then((res: any) => res?.data ?? res);
}

/**
 * 标记某条更新为已读（幂等）
 * POST ow/whats-new/v1/{id}/read
 */
export function markRead(id: string): Promise<boolean> {
	return defHttp.post({ url: Api(id).read }).then((res: any) => res?.data ?? res);
}

/**
 * 标记所有 Published 更新为已读
 * POST ow/whats-new/v1/read-all
 */
export function markAllRead(): Promise<boolean> {
	return defHttp.post({ url: Api().readAll }).then((res: any) => res?.data ?? res);
}

// ========================= 管理端接口 =========================

/**
 * 获取管理端列表（含统计计数，支持 status 过滤）
 * GET ow/whats-new/v1/admin
 * @param status 可选，0=Draft / 1=Published
 */
export function getAdminList(status?: number): Promise<WhatsNewAdminListResponse> {
	return defHttp
		.get({ url: Api().adminList, params: status !== undefined ? { status } : undefined })
		.then((res: any) => res?.data ?? res);
}

/**
 * 创建新更新
 * POST ow/whats-new/v1/admin
 */
export function createWhatsNew(data: CreateWhatsNewRequest): Promise<string> {
	return defHttp.post({ url: Api().adminList, data }).then((res: any) => res?.data ?? res);
}

/**
 * 编辑已有更新
 * PUT ow/whats-new/v1/admin/{id}
 */
export function updateWhatsNew(id: string, data: UpdateWhatsNewRequest): Promise<boolean> {
	return defHttp.put({ url: Api(id).adminItem, data }).then((res: any) => res?.data ?? res);
}

/**
 * 软删除更新
 * DELETE ow/whats-new/v1/admin/{id}
 */
export function deleteWhatsNew(id: string): Promise<boolean> {
	return defHttp.delete({ url: Api(id).adminItem }).then((res: any) => res?.data ?? res);
}
