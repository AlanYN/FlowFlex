import type { AppRouteModule } from '@/router/types';

import { LAYOUT } from '@/router/constant';
import { t } from '@/hooks/useI18n';

import checklistIcon from '@assets/svg/menu/checklist.svg';

const Dashboard: AppRouteModule = {
	path: '/dashboard',
	name: 'Dashboard',
	component: LAYOUT,
	redirect: '/dashboard/index',
	meta: {
		hideChildrenInMenu: true,
		icon: checklistIcon,
		title: t('sys.router.dashboard'),
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
				ordinal: 1,
				hidden: false,
				status: true,
			},
		},
	],
};

export default Dashboard;
