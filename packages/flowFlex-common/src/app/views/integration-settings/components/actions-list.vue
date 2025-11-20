<template>
	<div class="actions-list">
		<div class="section-header">
			<h4 class="section-title">Actions</h4>
			<el-tag type="info" size="small">Read Only</el-tag>
		</div>

		<el-alert type="info" :closable="false" show-icon class="info-alert">
			This list shows all actions associated with this integration. Click on an action name to
			view details.
		</el-alert>

		<el-table
			v-loading="isLoading"
			:data="actions"
			border
			stripe
			class="actions-table"
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
					<el-tag
						v-for="workflowId in row.workflows"
						:key="workflowId"
						size="small"
						class="workflow-tag"
					>
						{{ getWorkflowName(workflowId) }}
					</el-tag>
				</template>
			</el-table-column>
		</el-table>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { getActions } from '@/apis/integration';
import type { IAction } from '#/integration';

interface Props {
	integrationId: string;
}

const props = defineProps<Props>();
const router = useRouter();

// 状态管理
const isLoading = ref(false);
const actions = ref<IAction[]>([]);

// 模拟工作流名称映射（实际应该从 API 获取）
const workflowNameMap: Record<string, string> = {
	'wf-1': 'Onboarding Workflow',
	'wf-2': 'Customer Service Workflow',
	'wf-3': 'Sales Workflow',
};

/**
 * 获取工作流名称
 */
function getWorkflowName(workflowId: string): string {
	return workflowNameMap[workflowId] || workflowId;
}

/**
 * 加载动作列表
 */
async function loadActions() {
	isLoading.value = true;
	try {
		actions.value = await getActions(props.integrationId);
	} catch (error) {
		console.error('Failed to load actions:', error);
	} finally {
		isLoading.value = false;
	}
}

/**
 * 点击动作名称
 */
function handleActionClick(actionId: string) {
	// 跳转到动作详情页
	router.push(`/actions/${actionId}`);
}

// 初始化
onMounted(() => {
	loadActions();
});
</script>

<style scoped lang="scss">
.actions-list {
	.section-header {
		display: flex;
		justify-content: space-between;
		align-items: center;
		margin-bottom: 16px;

		.section-title {
			font-size: 16px;
			font-weight: 600;
			color: var(--text-primary);
			margin: 0;
		}
	}

	.info-alert {
		margin-bottom: 20px;
	}

	.actions-table {
		:deep(.el-table) {
			background: var(--el-bg-color);
		}

		:deep(.el-table__header) {
			th {
				background: var(--el-fill-color);
				color: var(--el-text-color-primary);
				font-weight: 600;
				font-size: 13px;
			}
		}

		:deep(.el-table__body) {
			tr {
				background: var(--el-bg-color);
			}
		}

		.action-id {
			font-family: 'Courier New', monospace;
			font-size: 12px;
			color: var(--el-text-color-secondary);
		}

		.workflow-tag {
			margin-right: 8px;
			margin-bottom: 4px;
		}
	}
}
</style>
