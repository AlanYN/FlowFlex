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
			<el-table-column label="Action ID" prop="actionCode" width="180">
				<template #default="{ row }">
					<span class="action-id">{{ row.actionCode }}</span>
				</template>
			</el-table-column>

			<el-table-column label="Action Name" prop="name" min-width="200">
				<template #default="{ row }">
					<el-link type="primary" :underline="false" @click="handleActionClick(row)">
						{{ row.name }}
					</el-link>
				</template>
			</el-table-column>

			<el-table-column label="Type" prop="actionType" min-width="150">
				<template #default="{ row }">
					<el-tag class="type-tag">
						{{ getActionTypeName(row.actionType) }}
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
import { getActionDefinitions, ACTION_TYPE_MAPPING, ActionType } from '@/apis/action';
import { ElMessage } from 'element-plus';

interface Props {
	integrationId: string;
	integrationName: string;
	allWorkflows: any[];
}

const props = defineProps<Props>();
const emit = defineEmits(['refresh', 'openAction']);

// 状态管理
const isLoading = ref(false);
const actions = ref<[]>([]);

/**
 * 加载动作列表
 */
const loadActions = async () => {
	isLoading.value = true;
	try {
		actions.value = [];
		const res = await getActionDefinitions({
			integrationId: props.integrationId,
		});
		if (res.code === '200' && res.success) {
			actions.value = res.data.data || [];
		} else {
			actions.value = [];
			ElMessage.error(res.msg || 'Failed to load actions');
		}
	} finally {
		isLoading.value = false;
	}
};

// Action 弹窗相关状态
const actionEditorVisible = ref(false);
const actionInfo = ref<any>(null);
const editActionLoading = ref(false);
const handleActionClick = (row) => {
	actionInfo.value = row;
	actionEditorVisible.value = true;
};

const handleAddAction = () => {
	actionEditorVisible.value = true;
	actionInfo.value = null;
};

const onActionSave = async (actionResult) => {
	emit('refresh');
};

const onActionCancel = () => {
	actionEditorVisible.value = false;
	actionInfo.value = null;
};

// Methods
const getActionTypeName = (actionType: number) => {
	return ACTION_TYPE_MAPPING[actionType as ActionType] || 'Unknown';
};

// 初始化
onMounted(() => {
	loadActions();
});
</script>
