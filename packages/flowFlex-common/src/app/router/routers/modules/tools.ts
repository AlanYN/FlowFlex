import type { AppRouteModule } from '@/router/types';

import { LAYOUT } from '@/router/constant';
import { t } from '@/hooks/useI18n';

import toolsIcon from '@assets/svg/menu/tools.svg';

const leads: AppRouteModule = {
	path: '/onboard',
	name: 'OnboardTools',
	component: LAYOUT,
	redirect: '/onboard/actions',
	meta: {
		hideChildrenInMenu: true,
		icon: toolsIcon,
		title: t('sys.router.tools'),
		code: 'TOOLS',
		ordinal: 6,
		hidden: false,
		status: true,
	},
	children: [
		{
			path: 'actions',
			name: 'Tools',
			component: () => import('@/views/actions/index.vue'),
			meta: {
				title: t('sys.router.tools'),
				code: 'TOOLS',
				ordinal: 6,
				hidden: false,
				status: true,
			},
		},
	],
};

export default leads;
