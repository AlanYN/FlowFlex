<template>
	<aside class="ts-sidebar flex flex-col w-[280px] min-w-[280px] h-full overflow-hidden border-r">
		<!-- 标题 -->
		<div class="pt-[14px] px-4 pb-[10px]">
			<span class="ts-sidebar__title text-[13px] font-semibold tracking-[0.01em]">
				Workflows
			</span>
		</div>

		<!-- 搜索框 -->
		<div class="px-3 pb-[10px]">
			<el-input v-model="keyword" placeholder="Search workflows" clearable>
				<template #prefix>
					<el-icon class="ts-search-icon text-[13px]"><Search /></el-icon>
				</template>
			</el-input>
		</div>

		<!-- 筛选 tabs -->
		<div class="flex gap-0.5 px-3 pb-[10px] flex-nowrap overflow-hidden">
			<el-button
				v-for="f in filters"
				:key="f.value"
				:type="activeFilter === f.value ? 'primary' : ''"
				:plain="activeFilter === f.value"
				link
				class="ts-tab flex-1 min-w-0"
				:class="{ 'ts-tab--active': activeFilter === f.value }"
				@click="activeFilter = f.value"
			>
				{{ f.label }}
			</el-button>
		</div>

		<!-- Workflow 列表 -->
		<div class="flex-1 overflow-y-auto py-0.5 px-2 scroll-smooth">
			<transition-group name="ts-item">
				<div
					v-for="wf in filteredWorkflows"
					:key="wf.id"
					class="ts-item flex items-center gap-2 px-2 py-1.5 rounded-[6px] cursor-default transition-colors duration-[120ms]"
					:class="{
						'ts-item--on-canvas': canvasWorkflowIds.has(wf.id),
						'ts-item--current': wf.id === currentWorkflowId,
						'ts-item--inactive': wf.status === 'inactive',
					}"
					@dblclick.stop="canvasWorkflowIds.has(wf.id) && emit('focus', wf.id)"
				>
					<!-- 左侧状态点 -->
					<span
						class="ts-item__dot shrink-0 w-1.5 h-1.5 rounded-full"
						:class="`ts-item__dot--${wf.status}`"
					></span>

					<!-- 名称 + 标签 -->
					<div class="flex-1 min-w-0 flex items-center gap-1.5">
						<span
							class="ts-item__name flex-1 text-[13px] whitespace-nowrap overflow-hidden text-ellipsis"
							:title="wf.name"
						>
							{{ wf.name }}
						</span>
					</div>

					<el-tag v-if="wf.id === currentWorkflowId" type="primary">Current</el-tag>
					<!-- 操作按钮 -->
					<div v-if="!canvasWorkflowIds.has(wf.id)">
						<el-button
							:icon="Plus"
							:title="`Add ${wf.name} to canvas`"
							type="primary"
							link
							@click.stop="emit('add', wf.id)"
						/>
					</div>
					<!-- current workflow 不可删除，只显示 Current tag，无 × -->
					<div v-else-if="wf.id !== currentWorkflowId">
						<el-button
							:icon="Close"
							:title="`Remove ${wf.name}`"
							type="danger"
							link
							@click.stop="emit('remove', wf.id)"
						/>
					</div>
				</div>
			</transition-group>

			<p
				v-if="filteredWorkflows.length === 0"
				class="ts-sidebar__empty py-8 text-center text-[13px]"
			>
				No workflows found
			</p>
		</div>

		<!-- 底部统计 -->
		<div class="ts-sidebar__footer flex items-center gap-1.5 px-4 py-[10px] border-t text-xs">
			<span class="flex items-center gap-[3px]">
				<span class="ts-stat__num font-semibold">{{ onCanvasCount }}</span>
				on canvas
			</span>
			<span class="ts-stat__sep">·</span>
			<span class="flex items-center gap-[3px]">
				<span class="ts-stat__num font-semibold">{{ connectionCount }}</span>
				connections
			</span>
		</div>
	</aside>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { Search, Plus, Close } from '@element-plus/icons-vue';
import type { WorkflowItem } from '@/hooks/useTriggerEditor';

interface Props {
	workflows: WorkflowItem[];
	canvasWorkflowIds: Set<string>;
	onCanvasCount: number;
	connectionCount: number;
	currentWorkflowId: string;
	modelValue?: { keyword: string; filter: string };
}

const props = withDefaults(defineProps<Props>(), {
	modelValue: () => ({ keyword: '', filter: 'all' }),
});

const emit = defineEmits<{
	add: [workflowId: string];
	remove: [workflowId: string];
	focus: [workflowId: string];
	'update:modelValue': [value: { keyword: string; filter: string }];
}>();

const keyword = computed({
	get: () => props.modelValue?.keyword ?? '',
	set: (v) => emit('update:modelValue', { keyword: v, filter: activeFilter.value }),
});

const activeFilter = computed({
	get: () => (props.modelValue?.filter ?? 'all') as 'all' | 'on-canvas' | 'active' | 'inactive',
	set: (v) => emit('update:modelValue', { keyword: keyword.value, filter: v }),
});

const filters = [
	{ value: 'all', label: 'All' },
	{ value: 'on-canvas', label: 'On canvas' },
	{ value: 'active', label: 'Active' },
	{ value: 'inactive', label: 'Inactive' },
] as const;

const filteredWorkflows = computed(() => {
	let list = props.workflows;
	const kw = keyword.value.trim().toLowerCase();
	if (kw) list = list.filter((w) => w.name.toLowerCase().includes(kw));
	switch (activeFilter.value) {
		case 'on-canvas':
			list = list.filter(
				(w) => props.canvasWorkflowIds.has(w.id) || w.id === props.currentWorkflowId
			);
			break;
		case 'active':
			list = list.filter((w) => w.status === 'active');
			break;
		case 'inactive':
			list = list.filter((w) => w.status === 'inactive');
			break;
	}
	return list;
});
</script>

<style scoped lang="scss">
/* 颜色依赖 CSS 变量，无法用 tailwind 替代 */
.ts-sidebar {
	background: var(--el-bg-color);
	border-color: var(--el-border-color-light);
}

.ts-sidebar__title {
	color: var(--el-text-color-primary);
}

.ts-search-icon {
	color: var(--el-text-color-placeholder);
}

/* Tab el-button 宽度均分 */
.ts-tab {
	:deep(.el-button) {
		width: 100%;
	}
}

/* Item 状态颜色 */
.ts-item {
	&:hover {
		background: var(--el-fill-color-light);
	}
	&--on-canvas .ts-item__name {
		color: var(--el-text-color-primary);
		font-weight: 500;
	}
	&--current {
		background: var(--el-color-primary-light-9);
		&:hover {
			background: var(--el-color-primary-light-8);
		}
	}
	&--inactive .ts-item__name {
		color: var(--el-text-color-secondary);
	}
}

.ts-item__dot {
	&--active {
		background: var(--el-color-success);
		box-shadow: 0 0 0 2px var(--el-color-success-light-7);
	}
	&--inactive {
		background: var(--el-text-color-placeholder);
	}
}

.ts-item__name {
	color: var(--el-text-color-primary);
}

.ts-sidebar__empty {
	color: var(--el-text-color-placeholder);
}

.ts-sidebar__footer {
	border-color: var(--el-border-color-lighter);
	color: var(--el-text-color-secondary);
}

.ts-stat__num {
	color: var(--el-text-color-primary);
}
.ts-stat__sep {
	color: var(--el-border-color);
}
</style>
