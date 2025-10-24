<template>
	<div class="flex flex-row w-full overflow-hidden">
		<keep-alive>
			<sidebar ref="sidebarRef" v-if="!hideMenu" :is-collapse="isCollapse" />
		</keep-alive>
		<div
			class="h-screen pb-10 w-full main-content"
			:class="hideMenu || isCollapse ? 'right-content-full' : 'right-content'"
		>
			<navbar v-if="!hideMenu" @toggle-sidebar="handleSidebarToggle" />
			<!-- 页面切换容器 -->
			<div class="w-full h-full relative">
				<el-scrollbar class="h-full p-4">
					<router-view v-slot="{ Component }">
						<keep-alive :max="10" :include="cachedViews">
							<component :is="Component" />
						</keep-alive>
					</router-view>
				</el-scrollbar>
			</div>
		</div>
		<HistoryTable ref="historyTableRef" />
		<TenantSwitchingOverlay />
	</div>
</template>

<script lang="ts" setup>
import { ref, computed, provide, onMounted } from 'vue';
import sidebar from '@/components/sidebar/index.vue';
import navbar from './components/navbar.vue';
import { useUserStore } from '@/stores/modules/user';
import { usePermissionStore } from '@/stores/modules/permission';
import HistoryTable from '@/components/changeHistory/historyTable.vue';
import TenantSwitchingOverlay from '@/components/global/TenantSwitchingOverlay.vue';
import { WFEMoudels } from '@/enums/appEnum';

const userStore = useUserStore();

const sidebarRef = ref<InstanceType<typeof sidebar>>();

provide('openOrClose', (info: boolean) => {
	handleSidebarToggle(info);
});

const historyTableRef = ref<InstanceType<typeof HistoryTable>>();

provide('History', (id: string, type: WFEMoudels) => {
	historyTableRef.value?.showHistoryTable(id, type);
});

let isCollapse = ref(false);

// 处理sidebar折叠切换
const handleSidebarToggle = (collapsed: boolean) => {
	isCollapse.value = collapsed;
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
.main-content {
	background: var(--el-bg-color-page);
	transition: width 0.4s cubic-bezier(0.25, 0.8, 0.25, 1);
}

.right-content {
	width: calc(100% - 300px);
}

.right-content-full {
	width: 100%;
}

.right-content-collapse {
	width: calc(100% - 80px);
}
</style>
