<template>
	<el-dialog
		v-model="visible"
		:title="`Gantt Chart — ${summary?.caseName || ''}`"
		:width="dialogWidth"
		class="gantt-modal"
		:top="'5vh'"
		destroy-on-close
		@open="handleOpen"
	>
		<template #header>
			<div class="flex items-center justify-between w-full pr-8">
				<div class="flex items-center gap-3">
					<span class="font-semibold text-gray-800 dark:text-gray-100 text-base">
						Gantt Chart
					</span>
					<span v-if="summary" class="text-sm text-gray-500">
						{{ summary.caseCode }} · {{ summary.caseName }}
					</span>
					<el-tag v-if="summary?.status" size="small" type="primary" effect="plain">
						{{ summary.status }}
					</el-tag>
				</div>
			</div>
		</template>

		<!-- 加载中占位 -->
		<div v-if="loading" class="flex items-center justify-center py-20">
			<el-icon class="is-loading text-4xl text-primary-500 mr-3"><Loading /></el-icon>
			<span class="text-gray-500">Loading gantt data...</span>
		</div>

		<!-- 错误提示 -->
		<el-result
			v-else-if="error"
			icon="error"
			title="Failed to load gantt data"
			:sub-title="error"
		>
			<template #extra>
				<el-button type="primary" @click="fetchData">Retry</el-button>
			</template>
		</el-result>

		<!-- 甘特图主体 -->
		<div v-else class="gantt-modal-content" :style="{ maxHeight: '75vh', overflowY: 'auto' }">
			<GanttChart :stages="stages" :summary="summary" :loading="loading" />
		</div>

		<template #footer>
			<div class="flex justify-end">
				<el-button @click="visible = false">Close</el-button>
			</div>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { Loading } from '@element-plus/icons-vue';
import GanttChart from './GanttChart.vue';
import {
	getOnboardingGanttData,
	type GanttStageItem,
	type GanttCaseSummary,
} from '@/apis/ow/gantt';

// ========================= Props & Emits =========================

interface Props {
	modelValue: boolean;
	onboardingId: string | number;
}

const props = defineProps<Props>();
const emit = defineEmits<{
	'update:modelValue': [val: boolean];
}>();

// ========================= 状态 =========================

const stages = ref<GanttStageItem[]>([]);
const summary = ref<GanttCaseSummary | null>(null);
const loading = ref(false);
const error = ref<string | null>(null);

const visible = computed({
	get: () => props.modelValue,
	set: (val) => emit('update:modelValue', val),
});

const dialogWidth = computed(() => {
	// 响应式宽度：最大 90vw
	if (typeof window !== 'undefined') {
		return Math.min(window.innerWidth * 0.9, 1400) + 'px';
	}
	return '90%';
});

// ========================= 方法 =========================

async function fetchData() {
	if (!props.onboardingId) return;
	loading.value = true;
	error.value = null;
	try {
		const result = await getOnboardingGanttData(props.onboardingId);
		stages.value = result.stages;
		summary.value = result.summary;
	} catch (e: any) {
		error.value = e?.message || 'Unknown error';
	} finally {
		loading.value = false;
	}
}

function handleOpen() {
	fetchData();
}

// 每次打开时重新加载
watch(
	() => props.modelValue,
	(val) => {
		if (val) {
			fetchData();
		}
	}
);
</script>

<style lang="scss">
.gantt-modal {
	.el-dialog__body {
		padding: 0 20px 0 20px;
	}
	.el-dialog__header {
		padding: 16px 20px 12px;
		border-bottom: 1px solid var(--el-border-color-lighter);
		margin-right: 0;
	}
	.el-dialog__footer {
		padding: 12px 20px;
		border-top: 1px solid var(--el-border-color-lighter);
	}
}
</style>
