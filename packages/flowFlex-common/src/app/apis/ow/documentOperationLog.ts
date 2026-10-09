import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';

const globSetting = useGlobSetting();

const Api = (unitId?: string) => ({
	logs: `${globSetting.apiProName}/ow/document-operation-logs/${globSetting.apiVersion}`,
	logsByUnitId: `${globSetting.apiProName}/ow/document-operation-logs/${globSetting.apiVersion}/${unitId}`,
	revision: `${globSetting.apiProName}/ow/document-operation-logs/${globSetting.apiVersion}/${unitId}/revision`,
});

// ===================== Document Operation Log API =====================

/**
 * Report a MUTATION operation log (fire-and-forget from frontend) [OL-01]
 */
export function addOperationLog(params: {
	unitId: string;
	userId?: string;
	userName?: string;
	timestamp: number;
	mutationId: string;
	paramsJson?: string;
	revision?: number;
}) {
	return defHttp.post({ url: Api().logs, params });
}

/**
 * Query operation logs by unitId [OL-02]
 */
export function getOperationLogs(
	unitId: string,
	query?: {
		from?: string;
		to?: string;
		userId?: string;
		pageIndex?: number;
		pageSize?: number;
		keyword?: string;
	}
) {
	return defHttp.get({ url: Api(unitId).logsByUnitId, params: query });
}

/**
 * Get current revision number for a document [OL-03]
 */
export function getCurrentRevision(unitId: string) {
	return defHttp.get({ url: Api(unitId).revision });
}
