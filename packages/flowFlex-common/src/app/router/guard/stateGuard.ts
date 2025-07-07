import type { RouteLocationNormalized } from 'vue-router';
import { useMultipleTabStore } from '@/stores/modules/multipleTab';
import { useUserStore } from '@/stores/modules/user';
import { usePermissionStore } from '@/stores/modules/permission';
import { PageEnum } from '@/enums/pageEnum';
import { removeTabChangeListener } from '@/logics/mitt/routeChange';

export function createStateGuard(to: RouteLocationNormalized) {
	// 进入登录页面，清除认证信息即可
	if (to.path === PageEnum.BASE_LOGIN) {
		const tabStore = useMultipleTabStore();
		const userStore = useUserStore();
		const permissionStore = usePermissionStore();
		permissionStore.resetState();
		tabStore.resetState();
		userStore.resetState();
		removeTabChangeListener();
	}
}
