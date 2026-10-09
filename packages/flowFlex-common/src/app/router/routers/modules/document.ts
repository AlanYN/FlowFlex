import type { AppRouteModule } from '@/router/types';
import { LAYOUT } from '@/router/constant';
import { t } from '@/hooks/useI18n';
import managementIcon from '@assets/svg/menu/management.svg';

const documentModule: AppRouteModule = {
	path: '/documents',
	name: 'Documents',
	component: LAYOUT,
	redirect: '/documents/list',
	meta: {
		hideChildrenInMenu: true,
		icon: managementIcon,
		title: t('sys.router.document'),
		// code: 'DOCUMENTS',
		ordinal: 7,
		hidden: false,
		status: true,
	},
	children: [
		{
			path: 'list',
			name: 'DocumentList',
			component: () => import('@/views/document/list/index.vue'),
			meta: {
				title: t('sys.router.document'),
				// code: 'DOCUMENTS',
				ordinal: 1,
				hidden: false,
				status: true,
				keepAlive: true,
			},
		},
		{
			path: ':unitId/edit',
			name: 'DocumentEditor',
			component: () => import('@/views/document/editor/index.vue'),
			meta: {
				title: t('sys.router.document'),
				// code: 'DOCUMENTS',
				hidden: true,
				status: true,
				activeMenu: '/documents/list',
			},
		},
	],
};

export default documentModule;
