import type { AppRouteModule } from '@/router/types';

import { LAYOUT } from '@/router/constant';
import { t } from '@/hooks/useI18n';

import dashboardIcon from '@assets/svg/menu/dashboard.svg';

const Dashboard: AppRouteModule = {
	path: '/dashboard',
	name: 'Dashboard',
	component: LAYOUT,
	redirect: '/dashboard/index',
	meta: {
		hideChildrenInMenu: true,
		icon: dashboardIcon,
		title: t('sys.router.dashboard'),
		code: 'DASHBOARD',
		ordinal: 1,
		hidden: false,
		status: true,
	},
	children: [
		{
			path: 'index',
			name: 'DashboardDetail',
			component: () => import('@/views/dashboard/index.vue'),
			meta: {
				title: t('sys.router.dashboard'),
				code: 'DASHBOARD',
				ordinal: 1,
				hidden: false,
				status: true,
			},
		},
	],
};

export default Dashboard;
