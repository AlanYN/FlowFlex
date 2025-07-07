<template>
	<div class="flex flex-wrap gap-2">
		<template v-if="peopleList.length > 0">
			<template v-for="item in peopleList" :key="item">
				<el-tooltip placement="top">
					<template #content>
						<div>
							{{ item.value }}
							<br />
							{{ item?.email }}
						</div>
					</template>
					<div class="flex items-center justify-center">
						<el-image
							v-if="item.headUrl"
							:src="item.headUrl"
							alt="avatar"
							class="people-tag"
						/>
						<div v-else class="people-tag">
							<text class="text-base leading-[25px]">
								{{
									item.value?.split(' ').length > 1
										? item.value.split(' ')[1].substring(0, 1)
										: item.value.substring(0, 1)
								}}
							</text>
						</div>
					</div>
				</el-tooltip>
			</template>
		</template>
		<div v-else>{{ defaultStr }}</div>
	</div>
</template>

<script setup lang="ts">
import { watchEffect, ref } from 'vue';
import { Options } from '#/setting';
import { assginToOprions } from '@/hooks/searchAssginTo';
import { defaultStr } from '@/settings/projectSetting';

const { assignOptions, initSearchAssgin } = assginToOprions();

interface UrlExtension {
	headUrl?: string;
	value: string;
	email?: string;
}

type PeopleOptions = Options & UrlExtension;
interface Props {
	value?: string;
	options: PeopleOptions[];
}

const props = defineProps<Props>();

const peopleList = ref<UrlExtension[]>([]);
watchEffect(async () => {
	if (!props.value) {
		peopleList.value = [];
		return;
	}
	if (props.options.find((item) => item.key === props.value)) {
		peopleList.value = props.options
			.filter((item) => item.key === props.value)
			.map((item) => {
				return {
					value: item.value,
					headUrl: item.headUrl,
					email: item?.email || '',
				};
			});
	} else {
		await initSearchAssgin(props.value);
		peopleList.value = assignOptions.value
			.filter((item) => item.key === props.value)
			.map((item) => {
				return {
					value: item.value,
					headUrl: item.headUrl,
					email: item?.email || '',
				};
			});
	}
});
</script>

<style scoped lang="scss">
.people-tag {
	@apply h-[25px] w-[25px] bg-primary-500 rounded-full text-xl text-center font-bold leading-[25px] text-white;
}
</style>
