import type { AppRouteModule } from '@/router/types';

import { LAYOUT } from '@/router/constant';

const whatsNewManagement: AppRouteModule = {
    path: '/whats-new-management',
    name: 'WhatsNewManagement',
    component: LAYOUT,
    redirect: '/whats-new-management/index',
    meta: {
        title: "What's New Management",
        hidden: true,
        status: true,
    },
    children: [
        {
            path: 'index',
            name: 'WhatsNewManagementIndex',
            component: () => import('@/views/whatsNewManagement/index.vue'),
            meta: {
                title: "What's New Management",
                hidden: true,
                status: true,
            },
        },
    ],
};

export default whatsNewManagement;
