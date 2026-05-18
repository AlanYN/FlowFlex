import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';
import type { LookupPreviewRequest, LookupPreviewResponse } from '#/action-field-lookup';

const globSetting = useGlobSetting();

const Api = () => ({
	preview: `${globSetting.apiProName}/action/${globSetting.apiVersion}/lookup/preview`,
});

/**
 * Preview lookup options for a field configuration
 * Used by the Test button to validate lookup settings
 */
export function previewLookupOptions(data: LookupPreviewRequest) {
	return defHttp.post<LookupPreviewResponse>({
		url: Api().preview,
		data,
	});
}
