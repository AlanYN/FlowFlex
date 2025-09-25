import type { AppRouteModule } from '@/router/types';

import { LAYOUT } from '@/router/constant';

import aiWorkflowIcon from '@assets/svg/menu/ai-workflow.svg';

const leads: AppRouteModule = {
	path: '/onboard',
	name: 'OnboardAIWorkflow',
	component: LAYOUT,
	redirect: '/onboard/workflow/ai-workflow',
	meta: {
		hideChildrenInMenu: true,
		icon: aiWorkflowIcon,
		title: 'AI Workflow',
		code: 'AIWORKFLOW',
		ordinal: 3,
		hidden: false,
		status: true,
	},
	children: [
		{
			path: 'workflow/ai-workflow',
			name: 'AIWorkflow',
			component: () => import('@/views/onboard/workflow/ai-workflow.vue'),
			meta: {
				title: 'AI Workflow',
				code: 'AIWORKFLOW',
				ordinal: 2.1,
				hidden: false,
				status: true,
				beta: true,
			},
		},
	],
};

export default leads;
