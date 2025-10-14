<template>
	<div class="flex relative group cursor-pointer">
		<div class="flex flex-row justify-between items-center">
			<IonLanguage width="24px" height="24px" />
			<ul
				class="absolute top-full rounded-xl right-[-30px] w-[100px] p-[10px] hidden group-hover:block bg-siderbarGray dark:bg-black-300 z-[10000]"
			>
				<li
					v-for="item in localeArr"
					:key="item.event"
					class="text-placeholder cursor-pointer text-center dark:text-white-100 flex flex-row items-center"
				>
					<el-button
						:bg="true"
						link
						class="w-full my-1"
						@click="changeLocale(item.event)"
					>
						{{ item.text }}
					</el-button>
				</li>
			</ul>
		</div>
	</div>
</template>

<script lang="ts" setup>
import { computed } from 'vue';
import { localeList } from '@/settings/localeSetting';
import { useLocale } from '@/../locales/useLocale';

import IonLanguage from '@assets/svg/global/IonLanguage.svg';

const localeArr = computed(() => {
	return localeList;
});

const { changeLocale } = useLocale();
</script>
