<template>
	<div
		class="w-full cursor-pointer h-[64px] border-gray-100 dark:border-primary-500 px-10 py-5 flex flex-row justify-between items-center border-solid border-b"
	>
		<div class="flex items-center gap-x-4">
			<el-icon :size="24" class="cursor-pointer" v-if="!isEditRoute" @click="goToHomePage">
				<HomeFilled />
			</el-icon>
			<div
				v-else
				class="dark-white-100 font-medium text-[22px] dark:text-white-100 cursor-pointer flex items-center"
				@click="goBack"
			>
				<el-icon :size="24" :color="theme == 'light' ? '#000' : '#fff'">
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

const Router = useRoute();

const isEditRoute = ref(false);

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
	setTheme();
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
</script>

<style lang="scss" scoped>
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
