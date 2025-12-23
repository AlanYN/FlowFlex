import type { AppRouteModule } from '@/router/types';

import { LAYOUT } from '@/router/constant';
import { t } from '@/hooks/useI18n';

import settingsIcon from '@assets/svg/menu/setting.svg';

const intergration: AppRouteModule = {
	path: '/intergration',
	name: 'Intergration',
	component: LAYOUT,
	redirect: '/intergration/integration',
	meta: {
		hideChildrenInMenu: true,
		icon: settingsIcon,
		title: t('sys.router.integration'),
		code: 'INTEGRATION',
		ordinal: 11,
		hidden: false,
		status: true,
	},
	children: [
		{
			path: 'integration',
			name: 'Integration',
			component: () => import('@/views/integration-settings/index.vue'),
			meta: {
				title: t('sys.router.integration'),
				code: 'INTEGRATION',
				ordinal: 1,
				hidden: false,
				status: true,
			},
		},
		{
			path: 'integration/:id',
			name: 'IntegrationDetail',
			component: () => import('@/views/integration-settings/detail.vue'),
			meta: {
				title: t('sys.router.integration'),
				code: 'INTEGRATION',
				ordinal: 1,
				hidden: false,
				status: true,
				activeMenu: '/intergration/integration',
			},
		},
	],
};

export default intergration;
