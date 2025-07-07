<template>
	<div class="flex flex-row w-full overflow-hidden">
		<keep-alive>
			<sidebar ref="sidebarRef" v-if="!hideMenu" @click-event="getIsCollapse" />
		</keep-alive>
		<div
			class="h-screen bg-gray-50 dark:bg-black-300 pb-10 w-full"
			:class="hideMenu ? '' : isCollapse ? 'right-content-collapse' : 'right-content'"
		>
			<navbar v-if="!hideMenu" />
			<!-- 页面切换容器 -->
			<div class="w-full h-full relative overflow-auto p-4">
				<router-view v-slot="{ Component }">
					<keep-alive :max="10" :include="cachedViews">
						<component :is="Component" />
					</keep-alive>
				</router-view>
			</div>
		</div>
	</div>
</template>

<script lang="ts" setup>
import { ref, computed, provide, onMounted } from 'vue';
import sidebar from '@/components/sidebar/index.vue';
import navbar from './components/navbar.vue';
import { useUserStore } from '@/stores/modules/user';
import { usePermissionStore } from '@/stores/modules/permission';

const userStore = useUserStore();

const sidebarRef = ref<InstanceType<typeof sidebar>>();

provide('openOrClose', (info: boolean) => {
	sidebarRef.value?.collapseEvent(info);
});

let isCollapse = ref(false);

const getIsCollapse = (newIsCollapse: boolean) => {
	isCollapse.value = newIsCollapse;
};

const hideMenu = computed(() => {
	return userStore?.getLayout?.hideMenu || false;
});

// 添加需要缓存的组件名列表
const cachedViews = ref<string[]>([]);
const permissionStore = usePermissionStore();

// 初始化需要缓存的组件
const initCachedViews = () => {
	const routes = permissionStore.frontMenuList;
	const cacheNames: string[] = [];

	// 递归遍历路由
	const findKeepAliveComponents = (routes: any[]) => {
		routes.forEach((route) => {
			if (route.meta?.keepAlive && route.name) {
				cacheNames.push(route.name);
			}
			if (route.children && route.children.length > 0) {
				findKeepAliveComponents(route.children);
			}
		});
	};

	findKeepAliveComponents(routes);
	cachedViews.value = cacheNames;
	console.log('需要缓存的组件：', cacheNames);
};

onMounted(() => {
	initCachedViews();
});
</script>

<style lang="scss" scoped>
.right-content {
	width: calc(100% - 300px);
}

.right-content-collapse {
	width: calc(100% - 80px);
}
</style>
