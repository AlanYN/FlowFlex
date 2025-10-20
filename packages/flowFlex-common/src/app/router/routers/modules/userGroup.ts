import type { AppRouteModule } from '@/router/types';
import { LAYOUT } from '@/router/constant';
import { t } from '@/hooks/useI18n';

import userGroupIcon from '@assets/svg/menu/userGroup.svg';

const userGroup: AppRouteModule = {
	path: '/userGroup',
	name: 'UserGroup',
	component: LAYOUT,
	redirect: '/userGroup/index',
	meta: {
		hideChildrenInMenu: true,
		icon: userGroupIcon,
		title: t('sys.router.roleManagement'),
		code: 'USERGROUP',
		ordinal: 9,
		hidden: false,
		status: true,
	},
	children: [
		{
			path: 'index',
			name: 'UserGroupManagement',
			component: () => import('@/views/authorityManagement/userGroup.vue'),
			meta: {
				title: t('sys.router.roleManagement'),
				code: 'USERGROUP',
				ordinal: 1,
				hidden: false,
				status: true,
			},
		},
	],
};

export default userGroup;
