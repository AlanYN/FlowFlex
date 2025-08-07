<template>
	<div class="bg-gray-50">
		<!-- Header and Action Area -->
		<div class="actions-header">
			<h1 class="title">Actions</h1>
			<div class="actions">
				<el-button
					class="new-action-btn"
					type="primary"
					@click="handleCreateAction"
				>
					<el-icon><Plus /></el-icon>
					<span>New Action</span>
				</el-button>
				<el-button @click="handleExport">
					<el-icon><Download /></el-icon>
					<span>Export ({{ selectedActions.length }})</span>
				</el-button>
			</div>
		</div>

		<!-- Search and Filter Area -->
		<el-card class="mb-6 rounded-md">
			<template #default>
				<div class="pt-6">
					<el-form
						ref="searchFormRef"
						:model="searchForm"
						@submit.prevent="handleSearch"
						class="onboardSearch-form"
					>
						<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mb-4">
							<div class="space-y-2">
								<label class="text-sm font-medium text-gray-700">Action ID or Action Name</label>
								<el-input
									v-model="searchForm.keyword"
									placeholder="Enter Action ID or Action Name"
									clearable
									class="w-full rounded-md"
								>
									<template #prefix>
										<el-icon><Search /></el-icon>
									</template>
								</el-input>
							</div>

							<div class="space-y-2">
								<label class="text-sm font-medium text-gray-700">Type</label>
								<el-select
									v-model="searchForm.type"
									placeholder="Select Type"
									clearable
									class="w-full rounded-md"
								>
									<el-option 
										v-for="option in getActionTypeOptions()" 
										:key="option.value" 
										:label="option.label" 
										:value="option.value" 
									/>
								</el-select>
							</div>

							<div class="space-y-2">
								<label class="text-sm font-medium text-gray-700">Assignment - Workflow</label>
								<el-select
									v-model="searchForm.assignmentWorkflow"
									placeholder="Select Assignment"
									clearable
									class="w-full rounded-md"
								>
									<el-option label="All" value="all" />
									<el-option label="Yes" value="yes" />
									<el-option label="No" value="no" />
								</el-select>
							</div>
						</div>

						<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mb-4">
							<div class="space-y-2">
								<label class="text-sm font-medium text-gray-700">Assignment - Stage</label>
								<el-select
									v-model="searchForm.assignmentStage"
									placeholder="Select Assignment"
									clearable
									class="w-full rounded-md"
								>
									<el-option label="All" value="all" />
									<el-option label="Yes" value="yes" />
									<el-option label="No" value="no" />
								</el-select>
							</div>

							<div class="space-y-2">
								<label class="text-sm font-medium text-gray-700">Assignment - Checklist</label>
								<el-select
									v-model="searchForm.assignmentChecklist"
									placeholder="Select Assignment"
									clearable
									class="w-full rounded-md"
								>
									<el-option label="All" value="all" />
									<el-option label="Yes" value="yes" />
									<el-option label="No" value="no" />
								</el-select>
							</div>

							<div class="space-y-2">
								<label class="text-sm font-medium text-gray-700">Assignment - Questionnaire</label>
								<el-select
									v-model="searchForm.assignmentQuestionnaire"
									placeholder="Select Assignment"
									clearable
									class="w-full rounded-md"
								>
									<el-option label="All" value="all" />
									<el-option label="Yes" value="yes" />
									<el-option label="No" value="no" />
								</el-select>
							</div>
						</div>

						<div class="flex justify-end space-x-2">
							<el-button type="primary" @click="handleSearch">
								<el-icon><Search /></el-icon>
								<span>Search</span>
							</el-button>
					</div>
					</el-form>
				</div>
			</template>
		</el-card>

		<!-- Table Area -->
		<div class="customer-block !p-0 !ml-0">
			<el-table
				:data="actionsList"
				style="width: 100%"
				@selection-change="handleSelectionChange"
				v-loading="loading"
			>
				<el-table-column type="selection" width="55" />
				<el-table-column prop="actionCode" label="Action ID" width="120" />
				<el-table-column prop="name" label="Action Name" min-width="200" />
				<el-table-column prop="actionType" label="Type" width="150">
					<template #default="{ row }">
						<el-tag class="type-tag">
							{{ getActionTypeName(row.actionType) }}
						</el-tag>
					</template>
				</el-table-column>
				<el-table-column prop="triggerMappings" label="Assignments" min-width="300">
					<template #default="{ row }">
						<div class="assignments-list">
							<div v-for="mapping in row.triggerMappings" :key="mapping.id" class="assignment-item">
								<span class="assignment-name">
									{{ getAssignmentDisplayName(mapping) }}
								</span>
								<span class="assignment-date">Last applied: {{ mapping.lastApplied }}</span>
							</div>
						</div>
					</template>
				</el-table-column>
				<el-table-column label="Actions" width="120" fixed="right">
					<template #default="{ row }">
						<div class="action-buttons">
							<el-tooltip content="Edit" placement="top">
								<el-button type="primary" link @click="handleEdit(row)">
									<el-icon><Edit /></el-icon>
								</el-button>
							</el-tooltip>
							<el-tooltip content="Delete" placement="top">
								<el-button type="danger" link @click="handleDelete(row)">
									<el-icon><Delete /></el-icon>
								</el-button>
							</el-tooltip>
						</div>
					</template>
				</el-table-column>
			</el-table>

			<!-- Pagination -->
			<div class="border-t bg-white rounded-b-md">
				<CustomerPagination
					:total="pagination.total"
					:limit="pagination.pageSize"
					:page="pagination.currentPage"
					:background="true"
					@pagination="handleLimitUpdate"
					@update:page="handleCurrentChange"
					@update:limit="handlePageUpdate"
				/>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import {
	Plus,
	Download,
	Search,
	Edit,
	Delete,
} from '@element-plus/icons-vue';
import CustomerPagination from '@/components/global/u-pagination/index.vue';
import { getActionDefinitions, deleteAction, type ActionDefinition, type ActionQueryRequest, type TriggerMapping, ActionType, ACTION_TYPE_MAPPING, FRONTEND_TO_BACKEND_TYPE_MAPPING } from '../../apis/action';

// Router
const router = useRouter();

// Reactive data
const loading = ref(false);
const selectedActions = ref<any[]>([]);

// Search form
const searchForm = reactive({
	keyword: '',
	type: 'all',
	assignmentWorkflow: 'all',
	assignmentStage: 'all',
	assignmentChecklist: 'all',
	assignmentQuestionnaire: 'all',
});

// Pagination
const pagination = reactive({
	currentPage: 1,
	pageSize: 20,
	total: 0,
});

// Current page data
const actionsList = ref<ActionDefinition[]>([]);

// Methods
const getActionTypeName = (actionType: number) => {
	return ACTION_TYPE_MAPPING[actionType as ActionType] || 'Unknown';
};

// Get all available action type options
const getActionTypeOptions = () => {
	return [
		{ label: 'All Types', value: 'all' },
		...Object.entries(ACTION_TYPE_MAPPING).map(([key, value]) => ({
			label: value,
			value: key
		}))
	];
};


const getAssignmentDisplayName = (mapping: TriggerMapping) => {
	const parts: string[] = [];
	
	// Add WorkflowName
	if (mapping.workFlowName && mapping.workFlowName.trim()) {
		parts.push(mapping.workFlowName);
	}
	
	// Add StageName
	if (mapping.stageName && mapping.stageName.trim()) {
		parts.push(mapping.stageName);
	}
	
	// Add triggerSourceName
	if (mapping.triggerSourceName && mapping.triggerSourceName.trim()) {
		parts.push(mapping.triggerSourceName);
	}
	
	// If all fields are empty, return default value
	if (parts.length === 0) {
		return 'Unknown Assignment';
	}
	
	// Join all non-empty parts with arrows
	return parts.join(' → ');
};

const handleCreateAction = () => {
	router.push('/onboard/createAction');
};

const handleExport = () => {
	if (selectedActions.value.length === 0) {
		ElMessage.warning('Please select actions to export');
		return;
	}
	ElMessage.success(`Exporting ${selectedActions.value.length} actions`);
};

const handleSearch = async () => {
	// Reset to first page when searching
	pagination.currentPage = 1;
	// Reload data with search conditions
	await loadActionsList();
};



const handleSelectionChange = (selection: any[]) => {
	selectedActions.value = selection;
};

const handleEdit = (row: ActionDefinition) => {
	router.push(`/onboard/actionDetail/${row.id}`);
};

const handleDelete = async (row: ActionDefinition) => {
	try {
		await ElMessageBox.confirm(
			`Are you sure you want to delete action "${row.name}"?`,
			'Confirm Delete',
			{
				confirmButtonText: 'Delete',
				cancelButtonText: 'Cancel',
				type: 'warning',
			}
		);

		loading.value = true;
		const response = await deleteAction(row.id);
		
		if (response.code === '200' && response.success) {
			ElMessage.success('Action deleted successfully');
			// Reload data
			await loadActionsList();
		} else {
			ElMessage.error(response.msg || 'Failed to delete action');
		}
	} catch (error) {
		if (error !== 'cancel') {
			console.error('Failed to delete action:', error);
			ElMessage.error('Failed to delete action');
		}
	} finally {
		loading.value = false;
	}
};

const handlePageUpdate = async (size: number) => {
	pagination.pageSize = size;
	pagination.currentPage = 1;
	// Reload data
	await loadActionsList();
};

const handleCurrentChange = async (page: number) => {
	pagination.currentPage = page;
	// Reload data
	await loadActionsList();
};

const handleLimitUpdate = async () => {
	await loadActionsList();
};

// Load Actions list from API
const loadActionsList = async () => {
	try {
		loading.value = true;
		
		// Build query parameters
		const params: ActionQueryRequest = {
			pageIndex: pagination.currentPage,
			pageSize: pagination.pageSize,
		};

		// Add search conditions
		if (searchForm.keyword) {
			params.search = searchForm.keyword;
		}

		if (searchForm.type && searchForm.type !== 'all') {
			// Use enum value directly, convert to string
			params.actionType = searchForm.type;
		}

		if (searchForm.assignmentWorkflow && searchForm.assignmentWorkflow !== 'all') {
			params.isAssignmentWorkflow = searchForm.assignmentWorkflow === 'yes';
		}

		if (searchForm.assignmentStage && searchForm.assignmentStage !== 'all') {
			params.isAssignmentStage = searchForm.assignmentStage === 'yes';
		}

		if (searchForm.assignmentChecklist && searchForm.assignmentChecklist !== 'all') {
			params.isAssignmentChecklist = searchForm.assignmentChecklist === 'yes';
		}

		if (searchForm.assignmentQuestionnaire && searchForm.assignmentQuestionnaire !== 'all') {
			params.isAssignmentQuestionnaire = searchForm.assignmentQuestionnaire === 'yes';
		}

		// Call API
		const response = await getActionDefinitions(params);
		
		if (response.code === '200' && response.success) {
			actionsList.value = response.data.data || [];
			pagination.total = response.data.total || 0;
			pagination.currentPage = response.data.pageIndex || 1;
			pagination.pageSize = response.data.pageSize || 20;
		} else {
			actionsList.value = [];
			pagination.total = 0;
			ElMessage.error(response.msg || 'Failed to load actions');
		}
	} catch (error) {
		console.error('Failed to load actions:', error);
		actionsList.value = [];
		pagination.total = 0;
		ElMessage.error('Failed to load actions');
	} finally {
		loading.value = false;
	}
};

// Lifecycle
onMounted(() => {
	// Initialize data
	loadActionsList();
});
</script>

<style scoped lang="scss">
/* Header title bar styles */
.actions-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	padding: 0 24px 24px 24px;
}

.title {
	font-size: 24px;
	color: var(--primary-500, #2468f2);
	margin: 0;
	font-weight: 700;
}

.actions {
	display: flex;
	gap: 10px;
	align-items: center;
}

.assignments-list {
	.assignment-item {
		display: flex;
		flex-direction: column;
		margin-bottom: 8px;
		padding: 8px;

		&:last-child {
			margin-bottom: 0;
		}

		.assignment-name {
			font-weight: 500;
			color: #374151;
			margin-bottom: 4px;
		}

		.assignment-date {
			font-size: 12px;
			color: #6b7280;
		}
	}
}

.action-buttons {
	display: flex;
	gap: 8px;
	justify-content: center;
}

.type-tag {
	background-color: #e6f3ff !important;
	border-color: #b3d9ff !important;
	color: #2468f2 !important;
	border-radius: 16px !important;
	padding: 4px 12px !important;
	font-size: 12px !important;
	font-weight: 500 !important;
}




</style> 