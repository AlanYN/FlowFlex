<template>
	<div class="">
		<div class="flex items-center justify-between">
			<div class="flex flex-col gap-2 my-2">
				<div class="font-bold">Integration Actions</div>
				<div class="text-gray-500">
					All actions related to {{ integrationName }} System integration
				</div>
			</div>
			<el-button type="primary" @click="handleAddAction" :icon="Plus">Add Action</el-button>
		</div>

		<el-table
			v-loading="isLoading"
			:data="actions"
			border
			stripe
			empty-text="No actions configured for this integration"
		>
			<el-table-column label="Action ID" prop="id" width="180">
				<template #default="{ row }">
					<span class="action-id">{{ row.id }}</span>
				</template>
			</el-table-column>

			<el-table-column label="Action Name" prop="name" min-width="200">
				<template #default="{ row }">
					<el-link type="primary" :underline="false" @click="handleActionClick(row.id)">
						{{ row.name }}
					</el-link>
				</template>
			</el-table-column>

			<el-table-column label="Type" prop="type" width="150">
				<template #default="{ row }">
					<el-tag size="small" type="info">{{ row.type }}</el-tag>
				</template>
			</el-table-column>

			<el-table-column label="Status" prop="status" width="120" align="center">
				<template #default="{ row }">
					<el-tag
						:type="row.status === 'active' ? 'success' : 'info'"
						size="small"
						:style="{
							background:
								row.status === 'active' ? 'var(--success-color)' : '#909399',
							borderColor:
								row.status === 'active' ? 'var(--success-color)' : '#909399',
							color: 'white',
						}"
					>
						{{ row.status === 'active' ? 'Active' : 'Inactive' }}
					</el-tag>
				</template>
			</el-table-column>

			<el-table-column label="Workflows" prop="workflows" min-width="250">
				<template #default="{ row }">
					<el-tag v-for="workflowId in row.workflows" :key="workflowId">
						{{ getWorkflowName(workflowId) }}
					</el-tag>
				</template>
			</el-table-column>
		</el-table>

		<!-- Action Config Dialog -->
		<ActionConfigDialog
			v-model="actionEditorVisible"
			:action="actionInfo"
			:is-editing="!!actionInfo"
			:triggerSourceId="integrationId"
			:triggerType="TriggerTypeEnum.Integration"
			:loading="editActionLoading"
			@save-success="onActionSave"
			@cancel="onActionCancel"
		/>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { Plus } from '@element-plus/icons-vue';
import ActionConfigDialog from '@/components/actionTools/ActionConfigDialog.vue';
import { TriggerTypeEnum } from '@/enums/appEnum';

interface Props {
	integrationId: string;
	integrationName: string;
	allWorkflows: any[];
}

const props = defineProps<Props>();

// 状态管理
const isLoading = ref(false);
const actions = ref<[]>([]);

/**
 * 获取工作流名称
 */
const getWorkflowName = (workflowId: string): string => {
	return props.allWorkflows.find((workflow) => workflow.id === workflowId)?.name || workflowId;
};

/**
 * 加载动作列表
 */
const loadActions = async () => {
	isLoading.value = true;
	try {
		actions.value = [];
	} catch (error) {
		console.error('Failed to load actions:', error);
	} finally {
		isLoading.value = false;
	}
};

// Action 弹窗相关状态
const actionEditorVisible = ref(false);
const actionInfo = ref<any>(null);
const editActionLoading = ref(false);
const handleActionClick = (actionId: string) => {
	// 跳转到动作详情页
};

const handleAddAction = () => {
	actionEditorVisible.value = true;
	actionInfo.value = null;
};

const onActionSave = async (actionResult) => {};

const onActionCancel = () => {
	actionEditorVisible.value = false;
	actionInfo.value = null;
};

// 初始化
onMounted(() => {
	loadActions();
});
</script>
