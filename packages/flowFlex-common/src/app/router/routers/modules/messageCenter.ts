import type { AppRouteModule } from '@/router/types';

import { LAYOUT } from '@/router/constant';
import { t } from '@/hooks/useI18n';

import messageIcon from '@assets/svg/menu/message.svg';

const messageCenter: AppRouteModule = {
	path: '/message',
	name: 'Message',
	component: LAYOUT,
	redirect: '/message/messageCenter',
	meta: {
		hideChildrenInMenu: true,
		icon: messageIcon,
		title: t('sys.router.messageCenter'),
		code: 'MESSAGECENTER',
		ordinal: 8,
		hidden: false,
		status: true,
	},
	children: [
		{
			path: 'messageCenter',
			name: 'MessageCenter',
			component: () => import('@/views/messageCenter/index.vue'),
			meta: {
				title: t('sys.router.messageCenter'),
				code: 'MESSAGECENTER',
				ordinal: 1,
				hidden: false,
				status: true,
			},
		},
	],
};

export default messageCenter;
