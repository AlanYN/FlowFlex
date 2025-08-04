<template>
	<div
		:class="[
			isCollapse ? 'w-[80px]' : 'w-[300px]',
			' bg-siderbarGray dark:bg-black-400 py-8 unis-sidebar px-2',
		]"
	>
		<div :class="[{ 'flex-col': isCollapse }, 'flex items-center justify-between px-2']">
			<div class="block mr-[10px]" v-if="!isCollapse">
				<div class="logo-top flex gap-1 text-lg font-medium ml-2 flex items-center">
					<Logo />
					<div class="text-3xl font-bold ml-2">WFE</div>
				</div>
			</div>
			<div v-else>
				<Logo class="mt-[32px] mb-[32px]" />
			</div>
			<template v-if="!isCollapse">
				<DarkCollapseIcon
					class="cursor-pointer"
					v-if="theme === 'dark'"
					@click="collapseEvent(true)"
				/>
				<CollapseIcon v-else class="cursor-pointer" @click="collapseEvent(true)" />
			</template>
			<template v-else>
				<DarkExpandIcon
					class="cursor-pointer"
					v-if="theme === 'dark'"
					@click="collapseEvent(false)"
				/>
				<ExpandIcon class="cursor-pointer" v-else @click="collapseEvent(false)" />
			</template>
		</div>
		<el-scrollbar :class="['mt-10']">
			<Menu
				:collapse="isCollapse"
				:uniqueOpened="true"
				:collapseTransition="true"
				mode="vertical"
			/>
		</el-scrollbar>
	</div>
</template>
<script setup lang="ts">
import { ref, computed } from 'vue';
import { useTheme } from '@/utils/theme';
import Menu from './components/menu.vue';
// import { useGlobSetting } from '@/settings';

import CollapseIcon from '@assets/svg/layout/collapseButton.svg';
import DarkCollapseIcon from '@assets/svg/layout/dark_collapse.svg';
import ExpandIcon from '@assets/svg/layout/expand.svg';
import DarkExpandIcon from '@assets/svg/layout/dark_expand.svg';
import Logo from '@assets/svg/layout/logo.svg';

// const globSetting = useGlobSetting();

let isCollapse = ref(false);
const emit = defineEmits(['clickEvent']);

const theme = computed(() => {
	return useTheme().theme;
});

const collapseEvent = (info?: boolean) => {
	isCollapse.value = info || false;
	emit('clickEvent', isCollapse.value);
};

defineExpose({
	collapseEvent,
});
</script>

<style lang="scss" scoped>
.unis-sidebar {
	transition: width 0.5s ease;
}

.el-scrollbar {
	.el-menu {
		border-right: none !important;
		padding-left: 0 !important;
	}
}

.el-menu-item {
	padding-left: 0 !important;
}
</style>
