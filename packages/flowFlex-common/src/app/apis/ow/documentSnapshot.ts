import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';

const globSetting = useGlobSetting();

const Api = (id?: string | number) => ({
	snapshots: `${globSetting.apiProName}/ow/document-snapshots/${globSetting.apiVersion}`,
	snapshotByUnitId: `${globSetting.apiProName}/ow/document-snapshots/${globSetting.apiVersion}/${id}`,
	snapshotSave: `${globSetting.apiProName}/ow/document-snapshots/${globSetting.apiVersion}/${id}/save`,
	snapshotQuery: `${globSetting.apiProName}/ow/document-snapshots/${globSetting.apiVersion}/query`,
	snapshotByEntity: (entityType: string, entityId: string | number) =>
		`${globSetting.apiProName}/ow/document-snapshots/${globSetting.apiVersion}/by-entity/${entityType}/${entityId}`,
});

// ===================== Document Snapshot API =====================

/**
 * Get document snapshot by unitId [DS-01]
 */
export function getSnapshotByUnitId(unitId: string) {
	return defHttp.get({ url: Api(unitId).snapshotByUnitId });
}

/**
 * Create a new empty document snapshot; returns unitId [DS-02]
 */
export function createSnapshot(params: {
	title: string;
	docType: 'Sheet' | 'Doc' | 'Slide';
	dataJson?: string;
	entityType?: string;
	entityId?: number;
}) {
	return defHttp.post({ url: Api().snapshots, params });
}

/**
 * Save / update document snapshot (Upsert) [DS-03]
 *
 * Every field is optional: the backend merges with the stored record
 * (`input.DataJson ?? existing.DataJson`), so renaming a document by sending
 * only `{ title }` keeps its content intact.
 */
export function saveSnapshot(
	unitId: string,
	params: { title?: string; docType?: string; dataJson?: string }
) {
	return defHttp.post({ url: Api(unitId).snapshotSave, params });
}

/**
 * Soft-delete document by numeric ID [DS-04]
 */
export function deleteSnapshot(id: string | number) {
	return defHttp.delete({ url: Api(id).snapshotByUnitId });
}

/**
 * Paged query with keyword / docType / sort filters [DS-05]
 */
export function querySnapshots(params: {
	pageIndex?: number;
	pageSize?: number;
	keyword?: string;
	docType?: string;
	sortBy?: string;
	sortOrder?: string;
}) {
	return defHttp.post({ url: Api().snapshotQuery, params });
}

/**
 * Get documents associated with a business entity [DS-06]
 */
export function getSnapshotsByEntity(entityType: string, entityId: string | number) {
	return defHttp.get({ url: Api().snapshotByEntity(entityType, entityId) });
}
