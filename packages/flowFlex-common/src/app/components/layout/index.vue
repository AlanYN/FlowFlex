<template>
	<div class="flex flex-row w-full overflow-hidden">
		<keep-alive>
			<sidebar
				ref="sidebarRef"
				v-if="!hideMenu"
				:is-collapse="isCollapse"
				@navigation-start="handleNavigationStart"
				@navigation-end="handleNavigationEnd"
				@navigation-error="handleNavigationError"
			/>
		</keep-alive>
		<div
			class="h-screen pb-10 w-full main-content"
			:class="hideMenu || isCollapse ? 'right-content-full' : 'right-content'"
		>
			<navbar v-if="!hideMenu" @toggle-sidebar="handleSidebarToggle" />
			<!-- 页面切换容器 -->
			<div class="w-full h-full relative">
				<!-- 导航 Loading 覆盖层 -->
				<div v-if="isNavigating" class="navigation-loading-container">
					<div class="wave-loading">
						<div class="bar"></div>
						<div class="bar"></div>
						<div class="bar"></div>
						<div class="bar"></div>
						<div class="bar"></div>
					</div>
				</div>

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
	</div>
</template>

<script lang="ts" setup>
import { ref, computed, provide, onMounted } from 'vue';
import sidebar from '@/components/sidebar/index.vue';
import navbar from './components/navbar.vue';
import { useUserStore } from '@/stores/modules/user';
import { usePermissionStore } from '@/stores/modules/permission';
import HistoryTable from '@/components/changeHistory/historyTable.vue';
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
// 导航状态管理
const isNavigating = ref(false);

// 处理sidebar折叠切换
const handleSidebarToggle = (collapsed: boolean) => {
	isCollapse.value = collapsed;
};

// 处理导航事件
const handleNavigationStart = (path: string) => {
	console.log('导航开始:', path);
	isNavigating.value = true;
};

const handleNavigationEnd = (path: string) => {
	console.log('导航结束:', path);
	isNavigating.value = false;
};

const handleNavigationError = (error: any, path: string) => {
	console.error('导航失败:', error, path);
	isNavigating.value = false;
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

/* 导航 Loading 样式 */
.navigation-loading-container {
	position: absolute;
	top: 0;
	left: 0;
	width: 100%;
	height: 100%;
	background: hsl(var(--background) / 0.8);
	display: flex;
	justify-content: center;
	align-items: center;
	z-index: 9999;
	backdrop-filter: blur(2px);
	transition: background-color 0.3s ease;
}

/* 深色主题下的背景覆盖层 */
.dark .navigation-loading-container {
	background: hsl(var(--background) / 0.9);
}

/* 浅色主题下的背景覆盖层 */
html:not(.dark) .navigation-loading-container {
	background: var(--white-80);
}

/* Wave Loading */
.wave-loading {
	display: flex;
	justify-content: center;
	align-items: center;
	gap: 3px;
}

.wave-loading .bar {
	width: 4px;
	height: 20px;
	background: var(--el-color-primary);
	border-radius: 2px;
	animation: wave 1.2s ease-in-out infinite;
	transition: background-color 0.3s ease;
}

.wave-loading .bar:nth-child(1) {
	animation-delay: -1.1s;
}
.wave-loading .bar:nth-child(2) {
	animation-delay: -1s;
}
.wave-loading .bar:nth-child(3) {
	animation-delay: -0.9s;
}
.wave-loading .bar:nth-child(4) {
	animation-delay: -0.8s;
}
.wave-loading .bar:nth-child(5) {
	animation-delay: -0.7s;
}

@keyframes wave {
	0%,
	40%,
	100% {
		transform: scaleY(0.4);
	}
	20% {
		transform: scaleY(1);
	}
}
</style>
