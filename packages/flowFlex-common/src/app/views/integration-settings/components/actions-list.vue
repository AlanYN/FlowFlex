<template>
	<div class="">
		<div class="flex items-center justify-between">
			<div class="flex flex-col gap-2 my-2">
				<div class="font-bold">Integration Actions</div>
				<div class="text-gray-500">
					All actions related to {{ integrationName }} System integration
				</div>
			</div>
			<el-button
				@click="handleAddAction"
				:icon="Plus"
				v-permission="ProjectPermissionEnum.integration.create"
			>
				Add Action
			</el-button>
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
			ref="actionConfigDialogRef"
			:triggerSourceId="integrationId"
			:triggerType="TriggerTypeEnum.Integration"
			mappingRequired
			@save-success="onActionSave"
		/>
	</div>
</template>

<script setup lang="ts">
import { useTemplateRef } from 'vue';
import { Plus } from '@element-plus/icons-vue';
import ActionConfigDialog from '@/components/actionTools/ActionConfigDialog.vue';
import { TriggerTypeEnum } from '@/enums/appEnum';
import { ACTION_TYPE_MAPPING, ActionType } from '@/apis/action';
import { ProjectPermissionEnum } from '@/enums/permissionEnum';

interface Props {
	integrationId: string;
	integrationName: string;
	allWorkflows: any[];
	actions: any[];
	isLoading: boolean;
}

defineProps<Props>();
const emit = defineEmits(['refresh', 'openAction']);

// Action 弹窗相关状态
const actionConfigDialogRef = useTemplateRef('actionConfigDialogRef');
const handleActionClick = (row) => {
	actionConfigDialogRef.value?.open({
		actionId: row.id,
	});
};

const handleAddAction = () => {
	actionConfigDialogRef.value?.open();
};

const onActionSave = async (actionResult) => {
	emit('refresh');
};

// Methods
const getActionTypeName = (actionType: number) => {
	return ACTION_TYPE_MAPPING[actionType as ActionType] || 'Unknown';
};
</script>
