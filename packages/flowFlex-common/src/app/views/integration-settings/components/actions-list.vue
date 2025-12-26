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

			<el-table-column label="Actions" width="100" align="center">
				<template #default="{ row }">
					<el-button
						type="danger"
						link
						@click="handleDeleteAction(row)"
						v-permission="ProjectPermissionEnum.integration.delete"
					>
						<el-icon><Delete /></el-icon>
					</el-button>
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
import { Plus, Delete } from '@element-plus/icons-vue';
import { ElMessageBox, ElMessage } from 'element-plus';
import ActionConfigDialog from '@/components/actionTools/ActionConfigDialog.vue';
import { TriggerTypeEnum } from '@/enums/appEnum';
import { ACTION_TYPE_MAPPING, ActionType, deleteMappingAction } from '@/apis/action';
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

const onActionSave = async () => {
	emit('refresh');
};

// 删除 Action
const handleDeleteAction = async (row) => {
	try {
		await ElMessageBox.confirm(
			'Are you sure you want to delete this action? This action cannot be undone.',
			'⚠️ Confirm Deletion',
			{
				confirmButtonText: 'Delete Action',
				cancelButtonText: 'Cancel',
				confirmButtonClass: 'danger-confirm-btn',
				cancelButtonClass: 'cancel-confirm-btn',
				distinguishCancelAndClose: true,
				customClass: 'delete-confirmation-dialog',
				showCancelButton: true,
				showConfirmButton: true,
				beforeClose: async (action, instance, done) => {
					if (action === 'confirm') {
						instance.confirmButtonLoading = true;
						instance.confirmButtonText = 'Deleting...';
						try {
							const deleteMappingId = row?.triggerMappings?.find(
								(mapping) => mapping.triggerSourceId === row.integrationId
							)?.id;
							const res = await deleteMappingAction(deleteMappingId);
							if (res.code === '200') {
								ElMessage.success('Action deleted successfully');
								done();
								emit('refresh');
							} else {
								ElMessage.error(res.msg || 'Failed to delete action');
								instance.confirmButtonLoading = false;
								instance.confirmButtonText = 'Delete Action';
							}
						} catch {
							ElMessage.error('Failed to delete action');
							instance.confirmButtonLoading = false;
							instance.confirmButtonText = 'Delete Action';
						}
					} else {
						done();
					}
				},
			}
		);
	} catch {
		// User cancelled, do nothing
	}
};

// Methods
const getActionTypeName = (actionType: number) => {
	return ACTION_TYPE_MAPPING[actionType as ActionType] || 'Unknown';
};
</script>
