import type { AppRouteModule } from '@/router/types';

import { LAYOUT } from '@/router/constant';

const profile: AppRouteModule = {
    path: '/profile',
    name: 'Profile',
    component: LAYOUT,
    redirect: '/profile/index',
    meta: {
        hideChildrenInMenu: true,
        hidden: true, // 不在侧边栏显示
        title: 'My Profile',
        code: '', // 无权限码，所有登录用户可访问
        status: true,
    },
    children: [
        {
            path: 'index',
            name: 'UserProfile',
            component: () => import('@/views/profile/index.vue'),
            meta: {
                title: 'My Profile',
                code: '',
                hidden: true,
                status: true,
            },
        },
    ],
};

export default profile;
