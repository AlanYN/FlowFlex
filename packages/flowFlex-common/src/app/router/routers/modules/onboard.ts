import type { AppRouteModule } from '@/router/types';

import { LAYOUT } from '@/router/constant';
import { t } from '@/hooks/useI18n';

import onboardIcon from '@assets/svg/menu/onboard.svg';

const leads: AppRouteModule = {
	path: '/onboard',
	name: 'Onboard',
	component: LAYOUT,
	meta: {
		hideChildrenInMenu: false,
		icon: onboardIcon,
		title: t('sys.router.onboardWizard'),
		ordinal: 1,
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
				ordinal: 3,
				activeMenu: '/onboard/onboardList',
			},
		},

		{
			path: 'onboardWorkflow',
			name: 'OnboardWorkflow',
			component: () => import('@/views/onboard/workflow/index.vue'),
			meta: {
				title: t('sys.router.workflow'),
				ordinal: 2,
				hidden: false,
				status: true,
			},
		},		
		{
			path: 'checklist',
			name: 'Checklist',
			component: () => import('@/views/onboard/checkList/index.vue'),
			meta: {
				title: t('sys.router.checklist'),
				ordinal: 3,
				hidden: false,
				status: true,
			},
		},
		{
			path: 'questionnaire',
			name: 'Questionnaire',
			component: () => import('@/views/onboard/questionnaire/index.vue'),
			meta: {
				title: t('sys.router.questionnaireSetup'),
				ordinal: 4,
				hidden: false,
				status: true,
			},
		},
		{
			path: 'createQuestion',
			name: 'CreateQuestion',
			component: () => import('@/views/onboard/questionnaire/createQuestion.vue'),
			hidden: true,
			meta: {
				title: t('sys.router.questionnaireSetup'),
				ordinal: 5,
				hidden: false,
				status: true,
				activeMenu: '/onboard/questionnaire',
			},
		},
	],
};

export default leads;
