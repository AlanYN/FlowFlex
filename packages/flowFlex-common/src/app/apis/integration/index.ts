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
	IUpdateIntegrationRequest,
	IUpdateIntegrationStatusRequest,
	ITestConnectionResponse,
	IFieldMapping,
	IEntityMapping,
	IGetIntegrationsParams,
	IPaginatedResponse,
	IApiResponse,
	IQuickLink,
	IGenerateQuickLinkUrlRequest,
	ISyncLog,
	IGetSyncLogsParams,
	IInboundConfiguration,
	IOutboundConfiguration,
	IReceiveExternalDataConfig,
	IGetAvailableWorkflowsParams,
	IGetEntityMappingsParams,
	IWorkflowOption,
} from '#/integration';

const globSetting = useGlobSetting();

const Api = {
	integration: `${globSetting.apiProName}/integration/${globSetting.apiVersion}`,
	entityMapping: `${globSetting.apiProName}/integration/entity-mappings/${globSetting.apiVersion}`,
	fieldMapping: `${globSetting.apiProName}/integration/field-mappings/${globSetting.apiVersion}`,
	quickLink: `${globSetting.apiProName}/integration/quick-links/${globSetting.apiVersion}`,
	sync: `${globSetting.apiProName}/integration/sync/${globSetting.apiVersion}`,
	receiveExternalData: `${globSetting.apiProName}/integration/receive-external-data/${globSetting.apiVersion}`,
};

// ==================== Integration Management API ====================

/**
 * 获取集成列表（分页）
 * GET /integration/v1
 */
export async function getIntegrations(
	params?: IGetIntegrationsParams
): Promise<IApiResponse<IPaginatedResponse<IIntegrationConfig>>> {
	return defHttp.get({ url: Api.integration, params });
}

/**
 * 创建集成
 * POST /integration/v1
 */
export async function createIntegration(
	data: ICreateIntegrationRequest
): Promise<IApiResponse<string | number>> {
	return defHttp.post({ url: Api.integration, data });
}

/**
 * 获取单个集成详情
 * GET /integration/v1/{id}
 */
export async function getIntegration(
	id: string | number
): Promise<IApiResponse<IIntegrationConfig>> {
	return defHttp.get({ url: `${Api.integration}/${id}` });
}

/**
 * 获取集成完整详情（包括所有关联数据）
 * GET /integration/v1/{id}/details
 */
export async function getIntegrationDetails(
	id: string | number
): Promise<IApiResponse<IIntegrationConfig>> {
	return defHttp.get({ url: `${Api.integration}/${id}/details` });
}

/**
 * 更新集成配置
 * PUT /integration/v1/{id}
 */
export async function updateIntegration(
	id: string | number,
	data: ICreateIntegrationRequest
): Promise<IApiResponse<boolean>> {
	return defHttp.put({ url: `${Api.integration}/${id}`, data });
}

/**
 * 删除集成
 * DELETE /integration/v1/{id}
 */
export async function deleteIntegration(id: string | number): Promise<IApiResponse<boolean>> {
	return defHttp.delete({ url: `${Api.integration}/${id}` });
}

/**
 * 测试连接
 * POST /integration/v1/{id}/test-connection
 */
export async function testConnection(
	id: string | number
): Promise<IApiResponse<ITestConnectionResponse>> {
	return defHttp.post({ url: `${Api.integration}/${id}/test-connection` });
}

/**
 * 更新集成状态
 * PUT /integration/v1/{id}/status
 */
export async function updateIntegrationStatus(
	id: string | number,
	data: IUpdateIntegrationStatusRequest
): Promise<IApiResponse<boolean>> {
	return defHttp.put({ url: `${Api.integration}/${id}/status`, data: data.status });
}

// ==================== Entity Mapping API ====================

/**
 * 创建实体映射
 * POST /integration/entity-mappings/v1
 */
export async function createEntityMapping(
	data: Omit<IEntityMapping, 'id'>
): Promise<IApiResponse<string | number>> {
	return defHttp.post({ url: Api.entityMapping, data });
}

/**
 * 更新实体映射
 * PUT /integration/entity-mappings/v1/{id}
 */
export async function updateEntityMapping(
	id: string | number,
	data: Partial<IEntityMapping>
): Promise<IApiResponse<boolean>> {
	return defHttp.put({ url: `${Api.entityMapping}/${id}`, data });
}

/**
 * 删除实体映射
 * DELETE /integration/entity-mappings/v1/{id}
 */
export async function deleteEntityMapping(id: string | number): Promise<IApiResponse<boolean>> {
	return defHttp.delete({ url: `${Api.entityMapping}/${id}` });
}

/**
 * 获取实体映射详情
 * GET /integration/entity-mappings/v1/{id}
 */
export async function getEntityMapping(id: string | number): Promise<IApiResponse<IEntityMapping>> {
	return defHttp.get({ url: `${Api.entityMapping}/${id}` });
}

/**
 * 按集成 ID 获取实体映射列表
 * GET /integration/entity-mappings/v1/by-integration/{integrationId}
 */
export async function getEntityMappingsByIntegration(
	integrationId: string | number
): Promise<IApiResponse<IEntityMapping[]>> {
	return defHttp.get({ url: `${Api.entityMapping}/by-integration/${integrationId}` });
}

/**
 * 获取实体映射列表（分页）
 * GET /integration/entity-mappings/v1
 */
export async function getEntityMappings(
	params: IGetEntityMappingsParams
): Promise<IApiResponse<IPaginatedResponse<IEntityMapping>>> {
	return defHttp.get({ url: Api.entityMapping, params });
}

// ==================== Field Mapping API ====================

/**
 * 创建字段映射
 * POST /integration/field-mappings/v1
 */
export async function createFieldMapping(
	data: Omit<IFieldMapping, 'id'>
): Promise<IApiResponse<string | number>> {
	return defHttp.post({ url: Api.fieldMapping, data });
}

/**
 * 更新字段映射
 * PUT /integration/field-mappings/v1/{id}
 */
export async function updateFieldMapping(
	id: string | number,
	data: Partial<IFieldMapping>
): Promise<IApiResponse<boolean>> {
	return defHttp.put({ url: `${Api.fieldMapping}/${id}`, data });
}

/**
 * 删除字段映射
 * DELETE /integration/field-mappings/v1/{id}
 */
export async function deleteFieldMapping(id: string | number): Promise<IApiResponse<boolean>> {
	return defHttp.delete({ url: `${Api.fieldMapping}/${id}` });
}

/**
 * 获取字段映射详情
 * GET /integration/field-mappings/v1/{id}
 */
export async function getFieldMapping(id: string | number): Promise<IApiResponse<IFieldMapping>> {
	return defHttp.get({ url: `${Api.fieldMapping}/${id}` });
}

/**
 * 按实体映射 ID 获取字段映射列表
 * GET /integration/field-mappings/v1/by-entity-mapping/{entityMappingId}
 */
export async function getFieldMappingsByEntityMapping(
	entityMappingId: string | number
): Promise<IApiResponse<IFieldMapping[]>> {
	return defHttp.get({
		url: `${Api.fieldMapping}/by-entity-mapping/${entityMappingId}`,
	});
}

/**
 * 按集成 ID 获取字段映射列表
 * GET /integration/field-mappings/v1/by-integration/{integrationId}
 */
export async function getFieldMappingsByIntegration(
	integrationId: string | number
): Promise<IApiResponse<IFieldMapping[]>> {
	return defHttp.get({ url: `${Api.fieldMapping}/by-integration/${integrationId}` });
}

/**
 * 批量更新字段映射
 * PUT /integration/field-mappings/v1/batch
 */
export async function batchUpdateFieldMappings(
	data: Array<Omit<IFieldMapping, 'id'>>
): Promise<IApiResponse<boolean>> {
	return defHttp.put({ url: `${Api.fieldMapping}/batch`, data });
}

// ==================== Quick Link API ====================

/**
 * 创建快速链接
 * POST /integration/quick-links/v1
 */
export async function createQuickLink(
	data: Omit<IQuickLink, 'id'>
): Promise<IApiResponse<string | number>> {
	return defHttp.post({ url: Api.quickLink, data });
}

/**
 * 更新快速链接
 * PUT /integration/quick-links/v1/{id}
 */
export async function updateQuickLink(
	id: string | number,
	data: Partial<IQuickLink>
): Promise<IApiResponse<boolean>> {
	return defHttp.put({ url: `${Api.quickLink}/${id}`, data });
}

/**
 * 删除快速链接
 * DELETE /integration/quick-links/v1/{id}
 */
export async function deleteQuickLink(id: string | number): Promise<IApiResponse<boolean>> {
	return defHttp.delete({ url: `${Api.quickLink}/${id}` });
}

/**
 * 获取快速链接详情
 * GET /integration/quick-links/v1/{id}
 */
export async function getQuickLink(id: string | number): Promise<IApiResponse<IQuickLink>> {
	return defHttp.get({ url: `${Api.quickLink}/${id}` });
}

/**
 * 按集成 ID 获取快速链接列表
 * GET /integration/quick-links/v1/by-integration/{integrationId}
 */
export async function getQuickLinksByIntegration(
	integrationId: string | number
): Promise<IApiResponse<IQuickLink[]>> {
	return defHttp.get({ url: `${Api.quickLink}/by-integration/${integrationId}` });
}

/**
 * 生成快速链接 URL
 * POST /integration/quick-links/v1/{id}/generate-url
 */
export async function generateQuickLinkUrl(
	id: string | number,
	data: IGenerateQuickLinkUrlRequest
): Promise<IApiResponse<string>> {
	return defHttp.post({ url: `${Api.quickLink}/${id}/generate-url`, data });
}

// ==================== Integration Sync API ====================

/**
 * 入站同步（Inbound）
 * POST /integration/sync/v1/inbound
 */
export async function syncInbound(params: {
	integrationId: string | number;
	entityType: string;
	externalEntityId: string;
}): Promise<IApiResponse<boolean>> {
	return defHttp.post({ url: `${Api.sync}/inbound`, params });
}

/**
 * 出站同步（Outbound）
 * POST /integration/sync/v1/outbound
 */
export async function syncOutbound(params: {
	integrationId: string | number;
	entityType: string;
	wfeEntityId: string | number;
}): Promise<IApiResponse<boolean>> {
	return defHttp.post({ url: `${Api.sync}/outbound`, params });
}

/**
 * 获取同步日志（分页）
 * GET /integration/sync/v1/logs
 */
export async function getSyncLogs(
	params: IGetSyncLogsParams
): Promise<IApiResponse<IPaginatedResponse<ISyncLog>>> {
	return defHttp.get({ url: `${Api.sync}/logs`, params });
}

/**
 * 重试失败的同步
 * POST /integration/sync/v1/retry/{syncLogId}
 */
export async function retrySync(syncLogId: string | number): Promise<IApiResponse<boolean>> {
	return defHttp.post({ url: `${Api.sync}/retry/${syncLogId}` });
}

// ==================== Inbound/Outbound Configuration API ====================

/**
 * 获取 Inbound 配置概览
 * GET /integration/v1/{integrationId}/inbound-overview
 */
export async function getInboundOverview(
	integrationId: string | number
): Promise<IApiResponse<IInboundConfiguration[]>> {
	return defHttp.get({ url: `${Api.integration}/${integrationId}/inbound-overview` });
}

/**
 * 获取 Outbound 配置概览
 * GET /integration/v1/{integrationId}/outbound-overview
 */
export async function getOutboundOverview(
	integrationId: string | number
): Promise<IApiResponse<IOutboundConfiguration[]>> {
	return defHttp.get({ url: `${Api.integration}/${integrationId}/outbound-overview` });
}

/**
 * 创建/更新 Inbound 配置
 * PUT /integration/v1/{integrationId}/actions/{actionId}/inbound
 */
export async function createOrUpdateInboundConfig(
	integrationId: string | number,
	actionId: string | number,
	data: Omit<IInboundConfiguration, 'id' | 'integrationId' | 'actionId'>
): Promise<IApiResponse<string | number>> {
	return defHttp.put({
		url: `${Api.integration}/${integrationId}/actions/${actionId}/inbound`,
		data,
	});
}

/**
 * 获取 Inbound 配置详情
 * GET /integration/v1/{integrationId}/actions/{actionId}/inbound
 */
export async function getInboundConfig(
	integrationId: string | number,
	actionId: string | number
): Promise<IApiResponse<IInboundConfiguration>> {
	return defHttp.get({
		url: `${Api.integration}/${integrationId}/actions/${actionId}/inbound`,
	});
}

/**
 * 创建/更新 Outbound 配置
 * PUT /integration/v1/{integrationId}/actions/{actionId}/outbound
 */
export async function createOrUpdateOutboundConfig(
	integrationId: string | number,
	actionId: string | number,
	data: Omit<IOutboundConfiguration, 'id' | 'integrationId' | 'actionId'>
): Promise<IApiResponse<string | number>> {
	return defHttp.put({
		url: `${Api.integration}/${integrationId}/actions/${actionId}/outbound`,
		data,
	});
}

/**
 * 获取 Outbound 配置详情
 * GET /integration/v1/{integrationId}/actions/{actionId}/outbound
 */
export async function getOutboundConfig(
	integrationId: string | number,
	actionId: string | number
): Promise<IApiResponse<IOutboundConfiguration>> {
	return defHttp.get({
		url: `${Api.integration}/${integrationId}/actions/${actionId}/outbound`,
	});
}

// ==================== Receive External Data Config API ====================

/**
 * 获取可用的 Workflows
 * GET /integration/receive-external-data/v1/available-workflows
 */
export async function getAvailableWorkflows(
	params: IGetAvailableWorkflowsParams
): Promise<IApiResponse<IPaginatedResponse<IWorkflowOption>>> {
	return defHttp.get({
		url: `${Api.receiveExternalData}/available-workflows`,
		params,
	});
}

/**
 * 创建接收外部数据配置
 * POST /integration/receive-external-data/v1
 */
export async function createReceiveExternalDataConfig(
	data: Omit<IReceiveExternalDataConfig, 'id'>
): Promise<IApiResponse<string | number>> {
	return defHttp.post({ url: Api.receiveExternalData, data });
}

/**
 * 删除接收外部数据配置
 * DELETE /integration/receive-external-data/v1/{id}
 */
export async function deleteReceiveExternalDataConfig(
	id: string | number
): Promise<IApiResponse<boolean>> {
	return defHttp.delete({ url: `${Api.receiveExternalData}/${id}` });
}

/**
 * 获取接收外部数据配置详情
 * GET /integration/receive-external-data/v1/{id}
 */
export async function getReceiveExternalDataConfig(
	id: string | number
): Promise<IApiResponse<IReceiveExternalDataConfig>> {
	return defHttp.get({ url: `${Api.receiveExternalData}/${id}` });
}

/**
 * 按集成 ID 获取配置列表
 * GET /integration/receive-external-data/v1/by-integration/{integrationId}
 */
export async function getReceiveExternalDataConfigsByIntegration(
	integrationId: string | number
): Promise<IApiResponse<IReceiveExternalDataConfig[]>> {
	return defHttp.get({
		url: `${Api.receiveExternalData}/by-integration/${integrationId}`,
	});
}
