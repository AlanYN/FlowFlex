<template>
	<el-card class="mb-6">
		<el-form
			ref="searchFormRef"
			:model="searchParams"
			@submit.prevent="handleSearch"
			class="onboardSearch-form"
		>
			<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
				<div class="space-y-2">
					<label class="text-sm font-medium">Lead ID</label>
					<InputTag
						v-model="leadIdTags"
						placeholder="Enter Lead ID and press enter"
						style-type="normal"
						:limit="10"
						clearable
						@change="handleLeadIdTagsChange"
						class="w-full rounded-xl"
					/>
				</div>

				<div class="space-y-2">
					<label class="text-sm font-medium">Company/Contact Name</label>
					<InputTag
						v-model="leadNameTags"
						placeholder="Enter Company/Contact Name and press enter"
						style-type="normal"
						:limit="10"
						clearable
						@change="handleLeadNameTagsChange"
						class="w-full rounded-xl"
					/>
				</div>

				<div class="space-y-2">
					<label class="text-sm font-medium">Life Cycle Stage</label>
					<el-select
						v-model="searchParams.lifeCycleStageName"
						placeholder="Select Stage"
						clearable
						class="w-full filter-select"
						@change="handleAutoSearch"
					>
						<el-option
							v-for="stage in lifeCycleStage"
							:key="stage.name"
							:label="stage.name"
							:value="stage.name"
						/>
					</el-select>
				</div>

				<div class="space-y-2" v-if="filterType === 'table'">
					<label class="text-sm font-medium">Workflow</label>
					<el-select
						v-model="searchParams.workFlowId"
						placeholder="Select Workflow"
						clearable
						class="w-full filter-select"
						@change="handleWorkflowChangeWithSearch"
					>
						<el-option
							v-for="workflow in allWorkflows"
							:key="workflow.id"
							:label="workflow.name"
							:value="workflow.id"
						/>
					</el-select>
				</div>

				<div class="space-y-2" v-if="filterType === 'table'">
					<label class="text-sm font-medium">Stage</label>
					<el-select
						v-model="searchParams.currentStageId"
						placeholder="Select Stage"
						clearable
						class="w-full filter-select"
						:disabled="!searchParams.workFlowId || stagesLoading"
						:loading="stagesLoading"
						@change="handleAutoSearch"
					>
						<el-option
							v-for="stage in dynamicOnboardingStages"
							:key="stage.id"
							:label="stage.name"
							:value="stage.id"
						/>
					</el-select>
				</div>

				<div class="space-y-2">
					<label class="text-sm font-medium">Updated By</label>
					<InputTag
						v-model="updatedByTags"
						placeholder="Enter User Name and press enter"
						style-type="normal"
						:limit="10"
						clearable
						@change="handleUpdatedByTagsChange"
						class="w-full rounded-xl"
					/>
				</div>

				<div class="space-y-2">
					<label class="text-sm font-medium">Priority</label>
					<el-select
						v-model="searchParams.priority"
						placeholder="Select Priority"
						clearable
						class="w-full filter-select"
						@change="handleAutoSearch"
					>
						<el-option label="High" value="High" />
						<el-option label="Medium" value="Medium" />
						<el-option label="Low" value="Low" />
					</el-select>
				</div>
			</div>
		</el-form>
	</el-card>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
// Icons removed as buttons are moved to parent component
import { SearchParams } from '#/onboard';
import InputTag from '@/components/global/u-input-tags/index.vue';
import { getStagesByWorkflow } from '@/apis/ow';

// Props
interface Props {
	lifeCycleStage: Array<{ id: string; name: string }>;
	allWorkflows: Array<{ id: string; name: string }>;
	onboardingStages: Array<{ id: string; name: string }>;
	loading: boolean;
	selectedItems: Array<any>;
	filterType: string;
}

const props = withDefaults(defineProps<Props>(), {
	lifeCycleStage: () => [],
	allWorkflows: () => [],
	onboardingStages: () => [],
	loading: false,
	selectedItems: () => [],
});

// Emits
const emit = defineEmits<{
	search: [params: SearchParams];
	export: [];
}>();

// 表单引用
const searchFormRef = ref();

// 搜索参数
const searchParams = reactive<SearchParams>({
	workFlowId: '',
	leadId: '',
	leadName: '',
	lifeCycleStageName: '',
	currentStageId: '',
	updatedBy: '',
	priority: '',
	page: 1,
	size: 15,
});

// 标签数组
const leadIdTags = ref<string[]>([]);
const leadNameTags = ref<string[]>([]);
const updatedByTags = ref<string[]>([]);

// 动态 stages 管理
const dynamicOnboardingStages = ref<Array<{ id: string; name: string }>>([]);
const stagesLoading = ref(false);

// stages 数据缓存
const stagesCache = ref<Record<string, Array<{ id: string; name: string }>>>({});

// 加载指定 workflow 的 stages 数据
const loadStagesForWorkflow = async (workflowId: string) => {
	if (!workflowId || workflowId === '0' || stagesCache.value[workflowId]) {
		return stagesCache.value[workflowId] || [];
	}

	try {
		stagesLoading.value = true;
		const response = await getStagesByWorkflow(workflowId);
		if (response.code === '200') {
			const stages = response.data || [];
			stagesCache.value[workflowId] = stages;
			return stages;
		} else {
			stagesCache.value[workflowId] = [];
			return [];
		}
	} catch (error) {
		stagesCache.value[workflowId] = [];
		return [];
	} finally {
		stagesLoading.value = false;
	}
};

// 处理 workflow 变化
const handleWorkflowChange = async (workflowId: string) => {
	// 清空当前选择的 stage
	searchParams.currentStageId = '';

	if (workflowId && workflowId !== '0') {
		// 加载对应的 stages
		const stages = await loadStagesForWorkflow(workflowId);
		dynamicOnboardingStages.value = stages;
	} else {
		// 如果没有选择 workflow，使用原始的 onboardingStages
		dynamicOnboardingStages.value = props.onboardingStages;
	}
};

// 处理 workflow 变化并触发搜索
const handleWorkflowChangeWithSearch = async (workflowId: string) => {
	await handleWorkflowChange(workflowId);
	handleAutoSearch();
};

// 防抖搜索
let searchTimeout: any = null;

// 自动搜索函数
const handleAutoSearch = () => {
	// 清除之前的定时器
	if (searchTimeout) {
		clearTimeout(searchTimeout);
	}
	// 设置新的定时器，实现防抖
	searchTimeout = setTimeout(() => {
		handleSearch();
	}, 300);
};

// 标签变化处理函数
const handleLeadIdTagsChange = (tags: string[]) => {
	searchParams.leadId = tags.join(',');
	handleAutoSearch();
};

const handleLeadNameTagsChange = (tags: string[]) => {
	searchParams.leadName = tags.join(',');
	handleAutoSearch();
};

const handleUpdatedByTagsChange = (tags: string[]) => {
	searchParams.updatedBy = tags.join(',');
	handleAutoSearch();
};

// 事件处理函数
const handleSearch = () => {
	// 将标签数组转换为搜索参数
	const searchParamsWithTags = {
		...searchParams,
		leadIdTags: leadIdTags.value,
		leadNameTags: leadNameTags.value,
		updatedByTags: updatedByTags.value,
	};
	emit('search', searchParamsWithTags);
};
const handleExport = () => {
	emit('export');
};

// 暴露给父组件的方法
defineExpose({
	handleSearch,
	handleExport,
});
</script>

<style scoped lang="scss">
/* Element Plus 组件样式覆盖 */
:deep(.filter-select .el-input__wrapper) {
	border-color: var(--primary-200);
	@apply dark:border-black-200;
}

:deep(.filter-select .el-input__wrapper:hover) {
	border-color: var(--primary-400);
	@apply dark:border-primary-600;
}

:deep(.filter-select .el-input__wrapper.is-focus) {
	border-color: var(--primary-500);
	@apply dark:border-primary-500;
}

/* 搜索表单样式 */
.onboardSearch-form :deep(.el-form-item) {
	margin-bottom: 0;
}

/* 暗色主题样式 */
html.dark {
	/* Element Plus 组件暗色主题 */
	:deep(.filter-select .el-input__wrapper) {
		background-color: var(--black-200) !important;
		border-color: var(--black-200) !important;
	}

	:deep(.filter-select .el-input__wrapper:hover) {
		border-color: var(--black-100) !important;
	}

	:deep(.filter-select .el-input__wrapper.is-focus) {
		border-color: var(--primary-500);
		box-shadow: 0 0 0 3px rgba(126, 34, 206, 0.2);
	}

	:deep(.filter-select .el-input__inner) {
		@apply text-white-100;
	}
}
</style>
