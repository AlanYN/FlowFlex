import type { AppRouteModule } from '@/router/types';

import { LAYOUT } from '@/router/constant';
import { t } from '@/hooks/useI18n';

import dyanmicFieldsIcon from '@assets/svg/menu/dynamicFields.svg';

const dynamic: AppRouteModule = {
	path: '/Dynamic',
	name: 'Dynamic',
	component: LAYOUT,
	redirect: '/Dynamic/dynamicFields',
	meta: {
		hideChildrenInMenu: true,
		icon: dyanmicFieldsIcon,
		title: t('sys.router.dynamicFields'),
		ordinal: 3,
		hidden: false,
		status: true,
	},
	children: [
		{
			path: 'dynamicFields',
			name: 'DynamicFields',
			component: () => import('@/views/dynamicFields/index.vue'),
			meta: {
				title: t('sys.router.dynamicFields'),
				ordinal: 1,
				hidden: false,
				status: true,
			},
		},
	],
};

export default dynamic;
