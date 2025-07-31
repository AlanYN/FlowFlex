<template>
	<el-card class="mb-6 rounded-md filter_card">
		<template #default>
			<div class="pt-6">
				<el-form
					ref="searchFormRef"
					:model="searchParams"
					@submit.prevent="handleSearch"
					class="onboardSearch-form"
				>
					<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mb-4">
						<div class="space-y-2">
							<label class="text-sm font-medium text-primary-500">Lead ID</label>
							<InputTag
								v-model="leadIdTags"
								placeholder="Enter Lead ID and press enter"
								style-type="normal"
								:limit="10"
								@change="handleLeadIdTagsChange"
								class="w-full rounded-md"
							/>
						</div>

						<div class="space-y-2">
							<label class="text-sm font-medium text-primary-500">
								Company/Contact Name
							</label>
							<InputTag
								v-model="leadNameTags"
								placeholder="Enter Company/Contact Name and press enter"
								style-type="normal"
								:limit="10"
								@change="handleLeadNameTagsChange"
								class="w-full rounded-md"
							/>
						</div>

						<div class="space-y-2">
							<label class="text-sm font-medium text-primary-500">
								Life Cycle Stage
							</label>
							<el-select
								v-model="searchParams.lifeCycleStageName"
								placeholder="Select Stage"
								clearable
								class="w-full rounded-md"
							
							:teleported="false">
								<el-option label="All Stages" value="" />
								<el-option
									v-for="stage in lifeCycleStage"
									:key="stage.name"
									:label="stage.name"
									:value="stage.name"
								/>
							</el-select>
						</div>

						<div class="space-y-2" v-if="filterType === 'table'">
							<label class="text-sm font-medium text-primary-500">
								Onboard Workflow
							</label>
							<el-select
								v-model="searchParams.workFlowId"
								placeholder="Select Work Flow"
								clearable
								class="w-full rounded-md"
								@change="handleWorkflowChange"
							
							:teleported="false">
								<el-option label="All Work Flows" value="" />
								<el-option
									v-for="workflow in allWorkflows"
									:key="workflow.id"
									:label="workflow.name"
									:value="workflow.id"
								/>
							</el-select>
						</div>

						<div class="space-y-2" v-if="filterType === 'table'">
							<label class="text-sm font-medium text-primary-500">
								Onboard Stage
							</label>
							<el-select
								v-model="searchParams.currentStageId"
								placeholder="Select Stage"
								clearable
								class="w-full rounded-md"
								:disabled="!searchParams.workFlowId || stagesLoading"
								:loading="stagesLoading"
							
							:teleported="false">
								<el-option label="All Stages" value="" />
								<el-option
									v-for="stage in dynamicOnboardingStages"
									:key="stage.id"
									:label="stage.name"
									:value="stage.id"
								/>
							</el-select>
						</div>

						<div class="space-y-2">
							<label class="text-sm font-medium text-primary-500">Updated By</label>
							<InputTag
								v-model="updatedByTags"
								placeholder="Enter User Name and press enter"
								style-type="normal"
								:limit="10"
								@change="handleUpdatedByTagsChange"
								class="w-full rounded-md"
							/>
						</div>

						<div class="space-y-2">
							<label class="text-sm font-medium text-primary-500">Priority</label>
							<el-select
								v-model="searchParams.priority"
								placeholder="Select Priority"
								clearable
								class="w-full rounded-md"
							
							:teleported="false">
								<el-option label="All Priorities" value="" />
								<el-option label="High" value="High" />
								<el-option label="Medium" value="Medium" />
								<el-option label="Low" value="Low" />
							</el-select>
						</div>
					</div>

					<div class="flex justify-end space-x-2">
						<el-button @click="handleReset">
							<el-icon><Close /></el-icon>
							Reset
						</el-button>
						<el-button type="primary" @click="handleSearch">
							<el-icon><Search /></el-icon>
							Search
						</el-button>
						<el-button @click="handleExport" :loading="loading" :disabled="loading">
							<el-icon><Download /></el-icon>
							Export
							{{ selectedItems.length > 0 ? `(${selectedItems.length})` : 'All' }}
						</el-button>
					</div>
				</el-form>
			</div>
		</template>
	</el-card>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { Search, Close, Download } from '@element-plus/icons-vue';
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
	reset: [];
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

// 标签变化处理函数
const handleLeadIdTagsChange = (tags: string[]) => {
	searchParams.leadId = tags.join(',');
};

const handleLeadNameTagsChange = (tags: string[]) => {
	searchParams.leadName = tags.join(',');
};

const handleUpdatedByTagsChange = (tags: string[]) => {
	searchParams.updatedBy = tags.join(',');
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

const handleReset = () => {
	// 重置搜索参数
	searchParams.leadId = '';
	searchParams.leadName = '';
	searchParams.lifeCycleStageName = '';
	searchParams.currentStageId = '';
	searchParams.updatedBy = '';
	searchParams.priority = '';
	searchParams.workFlowId = '';
	// 重置标签数组
	leadIdTags.value = [];
	leadNameTags.value = [];
	updatedByTags.value = [];
	// 重置动态 stages 为原始数据
	dynamicOnboardingStages.value = props.onboardingStages;

	emit('reset');
};

const handleExport = () => {
	emit('export');
};
</script>

<style scoped lang="scss">
.filter_card {
	background: linear-gradient(to right, var(--primary-50), var(--primary-100));
}

/* 搜索表单样式 */
.onboardSearch-form :deep(.el-form-item) {
	margin-bottom: 0;
}

.onboardSearch-form :deep(.el-input__wrapper) {
	transition: all 0.2s;
}

.onboardSearch-form :deep(.el-input__wrapper:hover) {
	border-color: #9ca3af;
}

.onboardSearch-form :deep(.el-input__wrapper.is-focus) {
	border-color: #3b82f6;
	box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

/* InputTag组件样式调整 - 优化显示效果 */
.onboardSearch-form :deep(.layout) {
	min-height: 32px;
	border: 1px solid var(--el-border-color, #dcdfe6);
	border-radius: 8px;
	padding: 4px 11px;
	background-color: var(--el-fill-color-blank, #ffffff);
	transition: all var(--el-transition-duration, 0.2s);
	box-shadow: 0 0 0 1px transparent inset;
	font-size: 14px;
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 4px;
}

.onboardSearch-form :deep(.layout:hover) {
	border-color: var(--el-border-color-hover, #c0c4cc);
}

.onboardSearch-form :deep(.layout:focus-within) {
	border-color: var(--primary-500, #409eff);
	box-shadow: 0 0 0 1px var(--primary-500, #409eff) inset !important;
}

.onboardSearch-form :deep(.input-tag) {
	min-width: 100px;
	height: 24px;
	line-height: 24px;
	font-size: 14px;
	color: var(--el-text-color-regular, #606266);
	border: none;
	outline: none;
	background: transparent;
	flex: 1;
	padding: 0;
}

.onboardSearch-form :deep(.input-tag::placeholder) {
	color: var(--el-text-color-placeholder, #a8abb2);
	font-size: 14px;
}

.onboardSearch-form :deep(.label-box) {
	height: 24px;
	margin: 0;
	border-radius: 12px;
	background-color: var(--el-fill-color-light, #f5f7fa);
	border: 1px solid var(--el-border-color-lighter, #e4e7ed);
	display: inline-flex;
	align-items: center;
	padding: 0 8px;
	transition: all 0.2s ease;
}

.onboardSearch-form :deep(.label-title) {
	font-size: 12px;
	padding: 0;
	line-height: 24px;
	color: var(--el-text-color-regular, #606266);
	font-weight: 500;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
	max-width: 120px;
}

.onboardSearch-form :deep(.label-close) {
	padding: 0;
	margin-left: 6px;
	color: var(--el-text-color-placeholder, #a8abb2);
	cursor: pointer;
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 16px;
	height: 16px;
	border-radius: 50%;
	background: var(--el-fill-color, #f0f2f5);
	transition: all 0.2s ease;
	transform: none;
}

.onboardSearch-form :deep(.label-close:hover) {
	background: var(--el-fill-color-dark, #e6e8eb);
	color: var(--el-text-color-regular, #606266);
}

.onboardSearch-form :deep(.label-close:after) {
	content: '×';
	font-size: 12px;
	line-height: 1;
	font-weight: bold;
}

/* 暗色主题样式 */
html.dark {
	/* 卡片和容器背景 */
	.rounded-md {
		background-color: var(--black-400) !important;
		border: 1px solid var(--black-200) !important;
	}

	/* 搜索表单暗色主题 */
	.onboardSearch-form :deep(.el-input__wrapper) {
		background-color: #2d3748 !important;
		border: 1px solid var(--black-200) !important;
	}

	.onboardSearch-form :deep(.el-input__wrapper:hover) {
		border-color: var(--black-100) !important;
	}

	.onboardSearch-form :deep(.el-input__wrapper.is-focus) {
		border-color: var(--primary-500);
		box-shadow: 0 0 0 3px rgba(126, 34, 206, 0.2);
	}

	.onboardSearch-form :deep(.el-input__inner) {
		@apply text-white-100;
	}

	/* InputTag暗色主题 - 优化暗色显示效果 */
	.onboardSearch-form :deep(.layout) {
		background-color: var(--black-200) !important;
		border: 1px solid var(--black-200) !important;
		color: var(--white-100) !important;
	}

	.onboardSearch-form :deep(.layout:hover) {
		border-color: var(--black-100) !important;
	}

	.onboardSearch-form :deep(.layout:focus-within) {
		border-color: var(--primary-500) !important;
		box-shadow: 0 0 0 1px var(--primary-500) inset !important;
	}

	.onboardSearch-form :deep(.label-box) {
		background-color: var(--black-300) !important;
		border: 1px solid var(--black-100) !important;
	}

	.onboardSearch-form :deep(.label-title) {
		color: var(--white-100) !important;
	}

	.onboardSearch-form :deep(.label-close) {
		background: var(--black-200) !important;
		color: var(--gray-300) !important;
	}

	.onboardSearch-form :deep(.label-close:hover) {
		background: var(--black-100) !important;
		color: var(--white-100) !important;
	}
}
</style>
