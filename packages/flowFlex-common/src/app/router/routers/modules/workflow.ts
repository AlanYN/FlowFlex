import type { AppRouteModule } from '@/router/types';

import { LAYOUT } from '@/router/constant';
import { t } from '@/hooks/useI18n';

import workflowIcon from '@assets/svg/menu/workflow.svg';

const leads: AppRouteModule = {
	path: '/onboard',
	name: 'OnboardWorkflow',
	component: LAYOUT,
	redirect: '/onboard/onboardWorkflow',
	meta: {
		hideChildrenInMenu: true,
		icon: workflowIcon,
		title: t('sys.router.workflow'),
		code: 'WORKFLOWS',
		ordinal: 2,
		hidden: false,
		status: true,
		keepAlive: true,
	},
	children: [
		{
			path: 'onboardWorkflow',
			name: 'OnboardWorkflow',
			component: () => import('@/views/onboard/workflow/index.vue'),
			meta: {
				title: t('sys.router.workflow'),
				code: 'WORKFLOWS',
				ordinal: 2,
				hidden: false,
				status: true,
				keepAlive: true,
			},
		},
		{
			path: 'workflow/:workflowId/conditions',
			name: 'WorkflowConditionEditor',
			component: () => import('@/views/onboard/workflow/condition-editor.vue'),
			meta: {
				title: 'Condition Editor',
				code: 'WORKFLOWS',
				ordinal: 2,
				hidden: true,
				status: true,
				activeMenu: '/onboard/onboardWorkflow',
			},
		},
	],
};

export default leads;
