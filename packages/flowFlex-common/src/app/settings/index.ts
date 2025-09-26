import type { GlobConfig } from '../../types/config';

import { getAppEnvConfig } from '@/utils/env';

export const useGlobSetting = (): Readonly<GlobConfig> => {
	const {
		VITE_GLOB_APP_TITLE,
		VITE_GLOB_API_URL,
		VITE_GLOB_API_URL_PREFIX,
		VITE_GLOB_UPLOAD_URL,
		VITE_GLOB_CODE,
		VITE_GLOB_SSOURL,
		VITE_GLOB_ENVIRONMENT,
		VITE_GLOB_DOMAIN_URL,
		VITE_GLOB_IDM_URL,
	} = getAppEnvConfig();

	// Take global configuration
	const glob: Readonly<GlobConfig> = {
		title: VITE_GLOB_APP_TITLE, //项目title
		apiUrl: VITE_GLOB_API_URL,
		shortName: VITE_GLOB_APP_TITLE.replace(/\s/g, '_').replace(/-/g, '_'),
		urlPrefix: VITE_GLOB_API_URL_PREFIX,
		uploadUrl: VITE_GLOB_UPLOAD_URL,
		ssoCode: VITE_GLOB_CODE,
		ssoURL: VITE_GLOB_SSOURL,
		environment: VITE_GLOB_ENVIRONMENT,
		idmUrl: VITE_GLOB_IDM_URL,
		apiVersion: 'v1',
		apiProName: '/api',
		domainUrl: VITE_GLOB_DOMAIN_URL,
	};
	return glob as Readonly<GlobConfig>;
};
