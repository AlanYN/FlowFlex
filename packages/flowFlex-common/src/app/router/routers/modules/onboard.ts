import type { AppRouteModule } from '@/router/types';

import { LAYOUT } from '@/router/constant';
import { t } from '@/hooks/useI18n';

import onboardIcon from '@assets/svg/menu/onboard.svg';

const cases: AppRouteModule = {
	path: '/onboard',
	name: 'Onboard',
	component: LAYOUT,
	redirect: '/onboard/onboardList',
	meta: {
		hideChildrenInMenu: true,
		icon: onboardIcon,
		title: t('sys.router.onboardList'),
		ordinal: 1,
		code: 'CASES',
		hidden: false,
		status: true,
	},
	children: [
		{
			path: 'onboardList',
			name: 'OnboardList',
			component: () => import('@/views/onboard/onboardingList/index.vue'),
			meta: {
				title: t('sys.router.onboardList'),
				ordinal: 1,
				code: 'CASES',
				hidden: false,
				status: true,
			},
		},
		{
			path: 'onboardDetail',
			name: 'OnboardDetail',
			component: () => import('@/views/onboard/onboardingList/detail.vue'),
			hidden: true,
			meta: {
				title: t('sys.router.onboardList'),
				code: 'CASES',
				ordinal: 2,
				activeMenu: '/onboard/onboardList',
			},
		},
		{
			path: 'customer-overview/:leadId',
			name: 'CustomerOverview',
			component: () => import('@/views/onboard/overview/customer-overview.vue'),
			hidden: true,
			meta: {
				title: 'Customer Overview',
				code: 'CASES',
				ordinal: 3,
				activeMenu: '/onboard/onboardList',
			},
		},
	],
};

export default cases;
