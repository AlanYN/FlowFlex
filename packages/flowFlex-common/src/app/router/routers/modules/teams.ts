import type { AppRouteModule } from '@/router/types';
import { LAYOUT } from '@/router/constant';
import { t } from '@/hooks/useI18n';

import teamsIcon from '@assets/svg/menu/teams.svg';

const teams: AppRouteModule = {
	path: '/teams',
	name: 'Teams',
	component: LAYOUT,
	redirect: '/teams/index',
	meta: {
		hideChildrenInMenu: true,
		icon: teamsIcon,
		title: t('sys.router.teamsManagement'),
		ordinal: 12,
		code: 'TEAMS',
		hidden: false,
		status: true,
	},
	children: [
		{
			path: 'index',
			name: 'TeamsManagement',
			component: () => import('@/views/authorityManagement/teams.vue'),
			meta: {
				title: t('sys.router.teamsManagement'),
				ordinal: 1,
				code: 'TEAMS',
				hidden: false,
				status: true,
			},
		},
	],
};

export default teams;
