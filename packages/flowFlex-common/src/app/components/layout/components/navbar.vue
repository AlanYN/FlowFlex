<template>
	<div
		class="w-full cursor-pointer h-[64px] pr-10 py-5 flex flex-row justify-between items-center border-solid border-b navbar-border"
	>
		<div class="flex items-center gap-x-4">
			<!-- 折叠按钮 -->
			<div class="cursor-pointer flex items-center gap-2 px-4" @click="toggleSidebar">
				<el-button :icon="CollapseIcon" link />
				<div
					data-orientation="vertical"
					role="none"
					class="shrink-0 bg-border w-[1px] h-4"
				></div>
			</div>

			<el-icon :size="24" class="cursor-pointer" v-if="!isEditRoute" @click="goToHomePage">
				<HomeFilled />
			</el-icon>
			<div
				v-else
				class="dark-white-100 font-medium text-[22px] dark:text-white-100 cursor-pointer flex items-center"
				@click="goBack"
			>
				<el-icon :size="24">
					<el-icon :size="20">
						<Back />
					</el-icon>
				</el-icon>
				Back
			</div>
			<TimeZone v-if="changeTimeZoneButton" />
		</div>
		<div class="flex items-center gap-x-4">
			<el-switch
				v-if="changeThemeButton"
				v-model="themeSwitch"
				:active-action-icon="Moon"
				:inactive-action-icon="Sunny"
				@click="changeTheme"
			/>
			<el-button v-if="searchButton" :icon="Search" circle />
			<Lauguage v-if="changeI18nButton" />
			<Setting v-if="settingButton" />
			<Company v-if="changeCompanyButton" />
			<UserLayout />
		</div>
	</div>
</template>
<script lang="ts" setup>
import { computed, ref, watch } from 'vue';
import { router } from '@/router';
import { useRoute } from 'vue-router';
import { setTheme, useTheme } from '@/utils/theme';
import { Sunny, Moon, Search, HomeFilled } from '@element-plus/icons-vue';

import { PageEnum } from '@/enums/pageEnum';
import {
	searchButton,
	changeI18nButton,
	changeThemeButton,
	settingButton,
	changeCompanyButton,
	changeTimeZoneButton,
} from '@/settings/projectSetting';

import Lauguage from '@/components/navbarCompanents/language.vue';
import TimeZone from '@/components/navbarCompanents/timeZone.vue';
import UserLayout from '@/components/navbarCompanents/userLayout.vue';
import Setting from '@/components/navbarCompanents/setting.vue';
import Company from '@/components/navbarCompanents/company.vue';

// 导入折叠相关的图标
import CollapseIcon from '@assets/svg/layout/collapseButton.svg';

const Router = useRoute();

const isEditRoute = ref(false);
const sidebarCollapsed = ref(false);

// 定义emits
const emit = defineEmits(['toggleSidebar']);

const goBack = () => {
	router.back();
};

const theme = computed({
	get: () => {
		return useTheme().theme;
	},
	set: (value) => {
		theme.value = value;
	},
});

let themeSwitch = computed(() => {
	return theme.value === 'dark' ? true : false;
});

const changeTheme = () => {
	if ('startViewTransition' in document) {
		(document as any).startViewTransition(setTheme);
	} else {
		setTheme();
	}
};

watch(
	() => Router,
	(newVal: any) => {
		isEditRoute.value = newVal.meta.isEdit || false;
	},
	{
		deep: true,
		immediate: true,
	}
);

const goToHomePage = () => {
	router.push(PageEnum.BASE_HOME as string);
};

// 切换sidebar折叠状态
const toggleSidebar = () => {
	sidebarCollapsed.value = !sidebarCollapsed.value;
	emit('toggleSidebar', sidebarCollapsed.value);
};
</script>

<style lang="scss" scoped>
.navbar-border {
	border-color: var(--el-border-color-light);
}

:deep(.iconify) {
	display: block;
	width: 20px;
	height: 20px;
}

:deep(.el-switch__action) {
	.el-icon {
		width: 16px !important;
		height: 16px !important;

		svg {
			display: block;
			width: 16px !important;
			height: 16px !important;
			margin: auto;
		}
	}
}
</style>
