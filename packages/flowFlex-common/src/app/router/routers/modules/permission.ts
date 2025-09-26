import type { AppRouteModule } from '@/router/types';
import { LAYOUT } from '@/router/constant';
import { t } from '@/hooks/useI18n';

import dealsIcon from '@assets/svg/menu/management.svg';

const permission: AppRouteModule = {
	path: '/permission',
	name: 'Permission',
	component: LAYOUT,
	redirect: '/permission/index',
	meta: {
		hideChildrenInMenu: true,
		icon: dealsIcon,
		title: t('sys.router.permissionManagement'),
		code: 'PERMISSION',
		ordinal: 7,
		hidden: false,
		status: true,
	},
	children: [
		{
			path: 'index',
			name: 'PermissionManagement',
			component: () => import('@/views/authorityManagement/permission.vue'),
			meta: {
				title: t('sys.router.permissionManagement'),
				code: 'PERMISSION',
				ordinal: 1,
				hidden: false,
				status: true,
			},
		},
	],
};

export default permission;
