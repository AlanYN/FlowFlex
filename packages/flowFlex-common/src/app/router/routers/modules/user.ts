import type { AppRouteModule } from '@/router/types';
import { LAYOUT } from '@/router/constant';
import { t } from '@/hooks/useI18n';

import userIcon from '@assets/svg/menu/user.svg';

const user: AppRouteModule = {
	path: '/user',
	name: 'User',
	component: LAYOUT,
	redirect: '/user/index',
	meta: {
		hideChildrenInMenu: true,
		icon: userIcon,
		title: t('sys.router.userManagement'),
		code: 'USER',
		ordinal: 10,
		hidden: false,
		status: true,
	},
	children: [
		{
			path: 'index',
			name: 'UserManagement',
			component: () => import('@/views/authorityManagement/user.vue'),
			meta: {
				title: t('sys.router.userManagement'),
				code: 'USER',
				ordinal: 1,
				hidden: false,
				status: true,
			},
		},
	],
};

export default user;
