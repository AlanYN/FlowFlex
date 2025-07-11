<template>
	<div class="flex flex-wrap gap-2">
		<!-- 骨架屏 -->
		<template v-if="loading">
			<el-skeleton-item
				v-for="i in 5"
				:key="i"
				variant="button"
				style="width: 120px; height: 32px; margin: 2px"
			/>
		</template>

		<!-- 实际标签 -->
		<template v-else>
			<el-tag
				v-for="stage in availableStages"
				:key="stage"
				:type="selectedStages.includes(stage) ? 'primary' : 'info'"
				:effect="selectedStages.includes(stage) ? 'dark' : 'plain'"
				class="cursor-pointer"
				@click="handleStageClick(stage)"
			>
				{{ stage }} ({{ stageCountMap[stage] || 0 }})
			</el-tag>
		</template>
	</div>
</template>

<script setup lang="ts">
import { PropType } from 'vue';

defineProps({
	loading: {
		type: Boolean,
		default: false,
	},
	availableStages: {
		type: Array as PropType<string[]>,
		required: true,
	},
	selectedStages: {
		type: Array as PropType<string[]>,
		required: true,
	},
	stageCountMap: {
		type: Object as PropType<Record<string, number>>,
		required: true,
	},
});

const emit = defineEmits(['stage-click']);

const handleStageClick = (stage: string) => {
	emit('stage-click', stage);
};
</script>

<style scoped lang="scss">
/* 暗色主题支持 */
html.dark {
	/* 标签暗色主题 */
	:deep(.el-tag) {
		@apply bg-black-200 border-black-200 text-white-100;
	}

	:deep(.el-tag.el-tag--info) {
		@apply bg-black-200 border-black-200 text-gray-300;
	}

	:deep(.el-tag.el-tag--primary) {
		background-color: var(--primary-500);
		border-color: var(--primary-500);
		color: white;
	}
}
</style>
