<template>
	<el-card class="mb-6 rounded-md">
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
							<label class="text-sm font-medium text-gray-700">Lead ID</label>
							<el-input
								v-model="searchParams.leadId"
								placeholder="Search or enter Lead ID"
								clearable
								class="w-full rounded-md"
							/>
						</div>

						<div class="space-y-2">
							<label class="text-sm font-medium text-gray-700">
								Company/Contact Name
							</label>
							<el-input
								v-model="searchParams.leadName"
								placeholder="Enter Company or Contact Name"
								clearable
								class="w-full rounded-md"
							/>
						</div>

						<div class="space-y-2">
							<label class="text-sm font-medium text-gray-700">
								Life Cycle Stage
							</label>
							<el-select
								v-model="searchParams.lifeCycleStageName"
								placeholder="Select Stage"
								clearable
								class="w-full rounded-md"
							>
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
							<label class="text-sm font-medium text-gray-700">
								Onboard Work Flow
							</label>
							<el-select
								v-model="searchParams.workFlowId"
								placeholder="Select Work Flow"
								clearable
								class="w-full rounded-md"
							>
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
							<label class="text-sm font-medium text-gray-700">Onboard Stage</label>
							<el-select
								v-model="searchParams.currentStageId"
								placeholder="Select Stage"
								clearable
								class="w-full rounded-md"
							>
								<el-option label="All Stages" value="" />
								<el-option
									v-for="stage in onboardingStages"
									:key="stage.id"
									:label="stage.name"
									:value="stage.id"
								/>
							</el-select>
						</div>

						<div class="space-y-2">
							<label class="text-sm font-medium text-gray-700">Updated By</label>
							<el-input
								v-model="searchParams.updatedBy"
								placeholder="Enter User Name"
								clearable
								class="w-full rounded-md"
							/>
						</div>

						<div class="space-y-2">
							<label class="text-sm font-medium text-gray-700">Priority</label>
							<el-select
								v-model="searchParams.priority"
								placeholder="Select Priority"
								clearable
								class="w-full rounded-md"
							>
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

// Props
interface Props {
	lifeCycleStage: Array<{ id: string; name: string }>;
	allWorkflows: Array<{ id: string; name: string }>;
	onboardingStages: Array<{ id: string; name: string }>;
	loading: boolean;
	selectedItems: Array<any>;
	filterType: string;
}

withDefaults(defineProps<Props>(), {
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

// 事件处理函数
const handleSearch = () => {
	emit('search', { ...searchParams });
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

	emit('reset');
};

const handleExport = () => {
	emit('export');
};
</script>

<style scoped lang="scss">
/* 搜索表单样式 */
.onboardSearch-form :deep(.el-form-item) {
	margin-bottom: 0;
}

.onboardSearch-form :deep(.el-input__wrapper) {
	border: 1px solid #d1d5db;
	transition: all 0.2s;
}

.onboardSearch-form :deep(.el-input__wrapper:hover) {
	border-color: #9ca3af;
}

.onboardSearch-form :deep(.el-input__wrapper.is-focus) {
	border-color: #3b82f6;
	box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
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
		background-color: var(--black-200) !important;
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

	/* 文本颜色调整 */
	.text-gray-700 {
		@apply text-white-100 !important;
	}
}
</style>
