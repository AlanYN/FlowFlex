/**
 * Integration Settings - API 接口定义
 * 所有与集成设置相关的 API 请求
 * 根据 Integration-API-Documentation.md 定义
 */

import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';
import type {
	IIntegrationConfig,
	ICreateIntegrationRequest,
	ITestConnectionResponse,
	IEntityMapping,
	IGetIntegrationsParams,
	IPaginatedResponse,
	IApiResponse,
	IQuickLink,
	InboundAttachmentIteml,
} from '#/integration';

const globSetting = useGlobSetting();

const Api = {
	integration: `${globSetting.apiProName}/integration/${globSetting.apiVersion}`,
	entityMapping: `${globSetting.apiProName}/integration/entity-mappings/${globSetting.apiVersion}`,
	fieldMapping: `${globSetting.apiProName}/integration/field-mappings/${globSetting.apiVersion}`,
	quickLink: `${globSetting.apiProName}/integration/quick-links/${globSetting.apiVersion}`,
	sync: `${globSetting.apiProName}/integration/sync/${globSetting.apiVersion}`,
	receiveExternalData: `${globSetting.apiProName}/integration/receive-external-data/${globSetting.apiVersion}`,

	inboundSettingsAttachment: `${globSetting.apiProName}/integration/attachment-sharing/${globSetting.apiVersion}`,

	attachmentApiMd: `${globSetting.apiProName}/integration/external/${globSetting.apiVersion}/attachments/protocol`,
};

// ==================== Integration Management API ====================

/**
 * 获取集成列表（分页）
 * GET /integration/v1
 */
export function getIntegrations(
	params?: IGetIntegrationsParams
): Promise<IApiResponse<IPaginatedResponse<IIntegrationConfig>>> {
	return defHttp.get({ url: Api.integration, params });
}

/**
 * 创建集成
 * POST /integration/v1
 */
export function createIntegration(
	data: ICreateIntegrationRequest
): Promise<IApiResponse<string | number>> {
	return defHttp.post({ url: Api.integration, data });
}

/**
 * 获取集成完整详情（包括所有关联数据）
 * GET /integration/v1/{id}/details
 */
export function getIntegrationDetails(
	id: string | number
): Promise<IApiResponse<IIntegrationConfig>> {
	return defHttp.get({ url: `${Api.integration}/${id}/details` });
}

/**
 * 更新集成配置
 * PUT /integration/v1/{id}
 */
export function updateIntegration(
	id: string | number,
	data: ICreateIntegrationRequest
): Promise<IApiResponse<boolean>> {
	return defHttp.put({ url: `${Api.integration}/${id}`, data });
}

/**
 * 删除集成
 * DELETE /integration/v1/{id}
 */
export function deleteIntegration(id: string | number): Promise<IApiResponse<boolean>> {
	return defHttp.delete({ url: `${Api.integration}/${id}` });
}

/**
 * 测试连接
 * POST /integration/v1/{id}/test-connection
 */
export function testConnection(
	id: string | number
): Promise<IApiResponse<ITestConnectionResponse>> {
	return defHttp.post({ url: `${Api.integration}/${id}/test-connection` });
}

// ==================== Entity Mapping API ====================

/**
 * 创建实体映射
 * POST /integration/entity-mappings/v1
 */
export function createEntityMapping(
	data: Omit<IEntityMapping, 'id'>
): Promise<IApiResponse<string | number>> {
	return defHttp.post({ url: Api.entityMapping, data });
}

/**
 * 更新实体映射
 * PUT /integration/entity-mappings/v1/{id}
 */
export function updateEntityMapping(
	id: string | number,
	data: Partial<IEntityMapping>
): Promise<IApiResponse<boolean>> {
	return defHttp.put({ url: `${Api.entityMapping}/${id}`, data });
}

/**
 * 删除实体映射
 * DELETE /integration/entity-mappings/v1/{id}
 */
export function deleteEntityMapping(id: string | number): Promise<IApiResponse<boolean>> {
	return defHttp.delete({ url: `${Api.entityMapping}/${id}` });
}

// ==================== Quick Link API ====================

/**
 * 创建快速链接
 * POST /integration/quick-links/v1
 */
export function createQuickLink(data: Partial<IQuickLink>): Promise<IApiResponse<string | number>> {
	return defHttp.post({ url: Api.quickLink, data });
}

/**
 * 更新快速链接
 * PUT /integration/quick-links/v1/{id}
 */
export function updateQuickLink(
	id: string | number,
	data: Partial<IQuickLink>
): Promise<IApiResponse<boolean>> {
	return defHttp.put({ url: `${Api.quickLink}/${id}`, data });
}

/**
 * 删除快速链接
 * DELETE /integration/quick-links/v1/{id}
 */
export function deleteQuickLink(id: string | number): Promise<IApiResponse<boolean>> {
	return defHttp.delete({ url: `${Api.quickLink}/${id}` });
}

/**
 * 获取快速链接详情
 * GET /integration/quick-links/v1/{id}
 */
export function getQuickLink(id: string | number): Promise<IApiResponse<IQuickLink>> {
	return defHttp.get({ url: `${Api.quickLink}/${id}` });
}

/**
 *  获取所有的quickLinks
 * @returns Promise<IApiResponse<IQuickLink[]>>
 */
export function getQuickLinks(): Promise<IApiResponse<IQuickLink[]>> {
	return defHttp.get({ url: Api.quickLink });
}

/**
 * 按集成 ID 获取快速链接列表
 * GET /integration/quick-links/v1/by-integration/{integrationId}
 */
export function getQuickLinksByIntegration(
	integrationId: string | number
): Promise<IApiResponse<IQuickLink[]>> {
	return defHttp.get({ url: `${Api.quickLink}/by-integration/${integrationId}` });
}

export function createInboundSettingsAttachment(
	data: InboundAttachmentIteml
): Promise<IApiResponse<string | number>> {
	return defHttp.post({ url: Api.inboundSettingsAttachment, data });
}

export function getInboundSettingsAttachment(
	integrationId: string | number
): Promise<IApiResponse<any>> {
	return defHttp.get({ url: `${Api.inboundSettingsAttachment}/by-integration/${integrationId}` });
}

export function deleteInboundSettingsAttachment(
	id: string | number,
	integrationId: string | number
): Promise<IApiResponse<boolean>> {
	return defHttp.delete(
		{
			url: `${Api.inboundSettingsAttachment}/${id}`,
			params: { integrationId },
		},
		{
			joinParamsToUrl: true, // Force params to be added to URL as query string
		}
	);
}

export function createOutboundSettingsAttachment(
	integrationId: string,
	items: {
		id: string;
		workflowId: string;
		stageIds: string[];
	}[]
): Promise<IApiResponse<boolean>> {
	return defHttp.put({
		url: `${Api.integration}/${integrationId}/outbound/attachment-workflows`,
		data: {
			items,
		},
	});
}

export function getOutboundSettingsAttachment(integrationId: string | number): Promise<
	IApiResponse<{
		integrationId: string;
		items: {
			id: string;
			workflowId: string;
			stageIds: string;
		}[];
	}>
> {
	return defHttp.get({
		url: `${Api.integration}/${integrationId}/outbound/attachment-workflows`,
	});
}

export function getAttachmentApiMd(): Promise<IApiResponse<string>> {
	return defHttp.get({ url: Api.attachmentApiMd });
}
