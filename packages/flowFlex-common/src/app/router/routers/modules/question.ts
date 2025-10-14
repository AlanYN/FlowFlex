import type { AppRouteModule } from '@/router/types';

import { LAYOUT } from '@/router/constant';
import { t } from '@/hooks/useI18n';

import questionnaireIcon from '@assets/svg/menu/question.svg';

const leads: AppRouteModule = {
	path: '/onboard',
	name: 'OnboardQuestionnaire',
	component: LAYOUT,
	redirect: '/onboard/questionnaire',
	meta: {
		hideChildrenInMenu: true,
		icon: questionnaireIcon,
		title: t('sys.router.questionnaireSetup'),
		code: 'QUESTIONNAIRES',
		ordinal: 5,
		hidden: false,
		status: true,
	},
	children: [
		{
			path: 'questionnaire',
			name: 'Questionnaire',
			component: () => import('@/views/onboard/questionnaire/index.vue'),
			meta: {
				title: t('sys.router.questionnaireSetup'),
				code: 'QUESTIONNAIRES',
				ordinal: 1,
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
				code: 'QUESTIONNAIRES',
				ordinal: 2,
				hidden: false,
				status: true,
				activeMenu: '/onboard/questionnaire',
			},
		},
	],
};

export default leads;
