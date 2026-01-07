<template>
	<div
		class="bg-siderbarGray dark:bg-black py-8 unis-sidebar sidebar-container h-screen"
		:class="{ 'sidebar-collapsed': props.isCollapse }"
	>
		<div
			class="sidebar-content flex flex-col h-full"
			:class="{ 'content-hidden': props.isCollapse }"
		>
			<div class="flex px-3 gap-x-2 cursor-pointer flex-shrink-0" @click="goToHomePage">
				<Logo v-if="theme.theme == 'dark'" width="48" height="48" />
				<PurpleLogo v-else width="48" height="48" />
				<div class="flex flex-col dark:text-white">
					<div class="text-2xl font-bold">item</div>
					<div class="text-sm font-bold">{{ globSetting.title }}</div>
				</div>
			</div>
			<el-scrollbar class="mt-4 pr-1 flex-1">
				<Menu
					:collapse="false"
					:uniqueOpened="true"
					:collapseTransition="true"
					mode="vertical"
					@navigation-start="handleNavigationStart"
					@navigation-end="handleNavigationEnd"
					@navigation-error="handleNavigationError"
				/>
			</el-scrollbar>
		</div>
	</div>
</template>
<script setup lang="ts">
import Menu from './components/menu.vue';
import { PageEnum } from '@/enums/pageEnum';
import { router } from '@/router';

import Logo from '@assets/svg/layout/logo.svg';
import PurpleLogo from '@assets/svg/layout/purpleLogo.svg';
import { useTheme } from '@/utils/theme';
import { useGlobSetting } from '@/settings';

const globSetting = useGlobSetting();

const props = defineProps({
	isCollapse: {
		type: Boolean,
		default: false,
	},
});

// 定义 emit 事件
const emit = defineEmits<{
	navigationStart: [path: string];
	navigationEnd: [path: string];
	navigationError: [error: any, path: string];
}>();

// 处理菜单导航事件
const handleNavigationStart = (path: string) => {
	emit('navigationStart', path);
};

const handleNavigationEnd = (path: string) => {
	emit('navigationEnd', path);
};

const handleNavigationError = (error: any, path: string) => {
	emit('navigationError', error, path);
};

const goToHomePage = () => {
	router.push(PageEnum.BASE_HOME as string);
};

const theme = useTheme();
</script>

<style lang="scss" scoped>
.unis-sidebar {
	transition: all 0.3s ease-in-out;
	border-right: 2px solid var(--el-border-color-light);
}

.sidebar-container {
	width: 300px;
	transition: width 0.4s cubic-bezier(0.25, 0.8, 0.25, 1);
	overflow: hidden;
	position: relative;
}

.sidebar-collapsed {
	width: 0px;
}

.sidebar-content {
	width: 300px;
	opacity: 1;
	transition: opacity 0.3s ease-in-out;
}

.content-hidden {
	opacity: 0;
	transition: opacity 0.2s ease-in-out;
}
</style>
