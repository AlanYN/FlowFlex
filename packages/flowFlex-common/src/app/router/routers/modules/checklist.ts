import type { AppRouteModule } from '@/router/types';

import { LAYOUT } from '@/router/constant';
import { t } from '@/hooks/useI18n';

import checklistIcon from '@assets/svg/menu/checklist.svg';

const leads: AppRouteModule = {
	path: '/onboard',
	name: 'OnboardChecklist',
	component: LAYOUT,
	redirect: '/onboard/checklist',
	meta: {
		hideChildrenInMenu: true,
		icon: checklistIcon,
		title: t('sys.router.checklist'),
		code: 'CHECKLISTS',
		ordinal: 4,
		hidden: false,
		status: true,
	},
	children: [
		{
			path: 'checklist',
			name: 'Checklist',
			component: () => import('@/views/onboard/checkList/index.vue'),
			meta: {
				title: t('sys.router.checklist'),
				code: 'CHECKLISTS',
				ordinal: 3,
				hidden: false,
				status: true,
			},
		},
	],
};

export default leads;
