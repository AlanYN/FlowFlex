<template>
	<el-card class="integration-card overflow-hidden transition-all cursor-pointer">
		<!-- 卡片头部 -->
		<template #header>
			<div class="card-header">
				<div class="flex items-center justify-between w-full">
					<div class="flex items-center space-x-3 flex-1 min-w-0" @click="handleClick">
						<div
							class="card-icon rounded-full flex-shrink-0 flex items-center justify-center"
						>
							<span class="text-2xl">{{ getSystemIcon() }}</span>
						</div>
						<h3 class="card-title tracking-tight truncate" :title="integration.name">
							{{ integration.name }}
						</h3>
					</div>
					<el-dropdown trigger="click" @command="handleCommand" class="flex-shrink-0">
						<el-button text class="card-more-btn" link>
							<el-icon class="h-4 w-4"><MoreFilled /></el-icon>
						</el-button>
						<template #dropdown>
							<el-dropdown-menu>
								<el-dropdown-item command="edit">
									<el-icon class="mr-2"><Edit /></el-icon>
									Edit
								</el-dropdown-item>
								<el-divider class="my-0" />
								<el-dropdown-item
									command="delete"
									class="text-red-500 hover:!bg-red-500 hover:!text-white"
								>
									<el-icon class="mr-2"><Delete /></el-icon>
									Delete
								</el-dropdown-item>
							</el-dropdown-menu>
						</template>
					</el-dropdown>
				</div>
				<p class="text-sm mt-1.5 truncate">
					{{ getSystemLabel() }}
				</p>
			</div>
		</template>

		<!-- 卡片内容 -->
		<div class="space-y-3">
			<div class="flex items-center gap-2">Entity Types:</div>
			<div class="flex items-center gap-2">
				<template v-for="value in integration.configuredEntityTypeNames" :key="value">
					<el-tag type="info">
						{{ value }}
					</el-tag>
				</template>
			</div>
		</div>
	</el-card>
</template>

<script setup lang="ts">
import { ElMessage, ElMessageBox } from 'element-plus';
import { Delete, Edit, MoreFilled } from '@element-plus/icons-vue';
import { useRouter } from 'vue-router';
import { nextTick } from 'vue';
import { deleteIntegration } from '@/apis/integration';
import type { IIntegrationConfig } from '#/integration';

interface Props {
	integration: IIntegrationConfig;
}

interface Emits {
	(e: 'refresh'): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();
const router = useRouter();

/**
 * 获取系统图标
 */
function getSystemIcon(): string {
	const icons: Record<string, string> = {
		salesforce: '☁️',
		hubspot: '🔶',
		zoho: '🔷',
		dynamics: '🔵',
		custom: '⚙️',
	};
	return icons[props.integration.type] || '🔗';
}

/**
 * 获取系统标签
 */
function getSystemLabel(): string {
	const labels: Record<string, string> = {
		salesforce: 'Salesforce CRM',
		hubspot: 'HubSpot CRM',
		zoho: 'Zoho CRM',
		dynamics: 'Microsoft Dynamics 365',
		custom: 'Custom Integration',
	};
	return labels[props.integration.type] || 'External System';
}

/**
 * 点击卡片
 */
function handleClick() {
	router.push({
		name: 'IntegrationDetail',
		params: { id: props.integration.id },
	});
}

/**
 * 处理下拉菜单命令
 */
function handleCommand(command: string) {
	if (command === 'edit') {
		handleClick();
	} else if (command === 'delete') {
		handleDelete();
	}
}

/**
 * 删除集成
 */
function handleDelete() {
	ElMessageBox({
		title: 'Confirm Deletion',
		message: `Are you sure you want to delete the integration "${props.integration.name}"? This action cannot be undone.`,
		showCancelButton: true,
		confirmButtonText: 'Delete',
		cancelButtonText: 'Cancel',
		type: 'warning',
		confirmButtonClass: 'el-button--danger',
		distinguishCancelAndClose: true,
		beforeClose: async (action, instance, done) => {
			if (action === 'confirm') {
				instance.confirmButtonLoading = true;
				instance.confirmButtonText = 'Deleting...';
				try {
					const res = await deleteIntegration(props.integration.id as string | number);
					instance.confirmButtonText = 'Delete';
					instance.confirmButtonLoading = false;
					if (res.success) {
						ElMessage.success('Integration deleted successfully');
						nextTick(() => {
							emit('refresh');
							done();
						});
					} else {
						ElMessage.error(res.msg || 'Failed to delete integration');
						done();
					}
				} finally {
					instance.confirmButtonText = 'Delete';
					instance.confirmButtonLoading = false;
					done();
				}
			} else {
				done();
			}
		},
	});
}
</script>
