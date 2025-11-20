<template>
	<el-card class="integration-card overflow-hidden transition-all cursor-pointer">
		<!-- 卡片头部 -->
		<template #header>
			<div class="card-header -m-5 p-4">
				<div class="flex items-center justify-between w-full">
					<div class="flex items-center space-x-3 flex-1 min-w-0" @click="handleClick">
						<div
							class="card-icon rounded-full flex-shrink-0 flex items-center justify-center"
						>
							<span class="text-2xl">{{ getSystemIcon() }}</span>
						</div>
						<h3
							class="card-title text-xl font-semibold leading-tight tracking-tight truncate"
							:title="integration.name"
						>
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
				<p class="text-white text-sm mt-1.5 truncate h-6">
					{{ getSystemLabel() }}
				</p>
			</div>
		</template>

		<!-- 卡片内容 -->
		<div class="space-y-3" @click="handleClick">
			<!-- 状态标签 -->
			<div class="flex items-center gap-2">
				<el-tag
					:type="integration.status === 'connected' ? 'success' : 'info'"
					size="small"
				>
					{{ integration.status === 'connected' ? 'Connected' : 'Disconnected' }}
				</el-tag>
			</div>

			<!-- 信息列表 -->
			<div class="space-y-2 text-sm">
				<div class="flex items-center gap-2">
					<el-icon class="text-primary-500"><Connection /></el-icon>
					<span class="font-medium">
						{{ getEntityCount() }} entity type(s) configured
					</span>
				</div>

				<div v-if="integration.connection?.endpointUrl" class="flex items-center gap-2">
					<el-icon class="text-primary-500"><Link /></el-icon>
					<span class="font-medium truncate">
						{{ integration.connection.endpointUrl }}
					</span>
				</div>

				<div v-if="integration.connection?.authMethod" class="flex items-center gap-2">
					<el-icon class="text-primary-500"><Key /></el-icon>
					<span class="font-medium">{{ integration.connection.authMethod }}</span>
				</div>

				<div v-if="integration.updatedAt" class="flex items-center gap-2">
					<el-icon class="text-primary-500"><Clock /></el-icon>
					<span class="font-medium">Updated {{ formatDate(integration.updatedAt) }}</span>
				</div>
			</div>
		</div>
	</el-card>
</template>

<script setup lang="ts">
import { ElMessage, ElMessageBox } from 'element-plus';
import { Connection, Link, Clock, Delete, Edit, MoreFilled, Key } from '@element-plus/icons-vue';
import { useRouter } from 'vue-router';
import { deleteIntegration } from '@/apis/integration';
import { timeZoneConvert } from '@/hooks/time';
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
 * 获取实体数量
 */
function getEntityCount(): number {
	const inboundCount = props.integration.inboundSettings?.entityMappings?.length || 0;
	return inboundCount;
}

/**
 * 格式化日期
 */
function formatDate(date: string): string {
	if (!date) return '';

	try {
		// 使用 timeZoneConvert 将零时区时间转换为本地时区
		const localDate = timeZoneConvert(date, false, 'YYYY-MM-DD HH:mm:ss');
		const d = new Date(localDate);
		const now = new Date();
		const diff = now.getTime() - d.getTime();
		const days = Math.floor(diff / (1000 * 60 * 60 * 24));

		if (days === 0) return 'today';
		if (days === 1) return 'yesterday';
		if (days < 7) return `${days} days ago`;
		if (days < 30) return `${Math.floor(days / 7)} weeks ago`;
		return timeZoneConvert(date, false, 'MM/DD/YYYY');
	} catch (error) {
		console.error('Failed to format date:', error);
		return date;
	}
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
async function handleDelete() {
	try {
		await ElMessageBox.confirm(
			`Are you sure you want to delete the integration "${props.integration.name}"? This action cannot be undone.`,
			'Confirm Deletion',
			{
				confirmButtonText: 'Delete',
				cancelButtonText: 'Cancel',
				type: 'warning',
			}
		);

		await deleteIntegration(props.integration.id);
		ElMessage.success('Integration deleted successfully');
		emit('refresh');
	} catch (error) {
		if (error !== 'cancel') {
			console.error('Failed to delete integration:', error);
			ElMessage.error('Failed to delete integration');
		}
	}
}
</script>

<style scoped lang="scss"></style>
