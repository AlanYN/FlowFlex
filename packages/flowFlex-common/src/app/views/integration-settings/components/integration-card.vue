<template>
	<div class="bg-bg-overlay border rounded-xl shadow-sm hover:shadow-md transition-shadow">
		<!-- 卡片头部 -->
		<div class="flex justify-between items-start gap-4 p-6">
			<div class="flex items-start gap-3 flex-1 cursor-pointer min-w-0" @click="toggleExpand">
				<el-icon
					:class="[
						'mt-0.5 text-text-secondary transition-transform flex-shrink-0',
						{ 'rotate-90': isExpanded },
					]"
					:size="20"
				>
					<ArrowRight />
				</el-icon>
				<div class="flex flex-col gap-1.5 flex-1 min-w-0">
					<div class="flex items-center gap-2 flex-wrap">
						<h3
							class="text-base font-semibold text-text-primary m-0 leading-tight transition-colors hover:text-primary"
						>
							{{ integration.name }}
						</h3>
						<el-tag
							:type="integration.status === 'connected' ? 'success' : 'info'"
							size="small"
							class="!px-2 !py-0.5 !font-medium !text-xs flex-shrink-0"
						>
							{{ integration.status === 'connected' ? 'Connected' : 'Disconnected' }}
						</el-tag>
					</div>
					<span class="text-sm text-text-secondary leading-tight">
						{{ getEntityCount() }} entity type(s) configured
					</span>
				</div>
			</div>

			<el-button
				type="danger"
				text
				:icon="Delete"
				class="flex-shrink-0 hover:!bg-danger-light-9"
				@click.stop="handleDelete"
			/>
		</div>

		<!-- 卡片内容（展开后显示） -->
		<div v-show="isExpanded" class="border-t bg-bg-overlay">
			<!-- Connection & Authentication 部分 -->
			<div class="p-6 border-b">
				<connection-auth
					:integration="integration"
					@update="handleUpdate"
					@test="handleTestConnection"
				/>
			</div>

			<!-- 三个 Tab -->
			<PrototypeTabs v-model="activeTab" :tabs="tabsConfig" class="px-6">
				<TabPane value="inbound">
					<inbound-settings :integration="integration" @update="handleUpdate" />
				</TabPane>

				<TabPane value="outbound">
					<outbound-settings :integration="integration" @update="handleUpdate" />
				</TabPane>

				<TabPane value="actions">
					<actions-list :integration-id="integration.id" />
				</TabPane>
			</PrototypeTabs>

			<!-- 底部操作按钮 -->
			<div class="flex justify-end gap-3 px-6 py-4 border-t">
				<el-button @click="handleCancel">Cancel</el-button>
				<el-button type="primary" :loading="isSaving" @click="handleSave">
					Save Configuration
				</el-button>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import { ArrowRight, Delete } from '@element-plus/icons-vue';
import { PrototypeTabs, TabPane } from '@/components/PrototypeTabs';
import ConnectionAuth from './connection-auth.vue';
import InboundSettings from './inbound-settings.vue';
import OutboundSettings from './outbound-settings.vue';
import ActionsList from './actions-list.vue';
import { updateIntegration, deleteIntegration, testConnection } from '@/apis/integration';
import type { IIntegrationConfig } from '#/integration';

interface Props {
	integration: IIntegrationConfig;
}

interface Emits {
	(e: 'refresh'): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

// 状态管理
const isExpanded = ref(false);
const activeTab = ref('inbound');
const isSaving = ref(false);
const localIntegration = ref<IIntegrationConfig>({ ...props.integration });

// Tab 配置
const tabsConfig = [
	{
		value: 'inbound',
		label: 'Inbound Settings',
	},
	{
		value: 'outbound',
		label: 'Outbound Settings',
	},
	{
		value: 'actions',
		label: 'Actions',
	},
];

/**
 * 切换展开/折叠
 */
function toggleExpand() {
	isExpanded.value = !isExpanded.value;
}

/**
 * 获取已配置实体数量
 */
function getEntityCount(): number {
	return localIntegration.value.inboundSettings?.entityMappings?.length || 0;
}

/**
 * 处理更新
 */
function handleUpdate(data: Partial<IIntegrationConfig>) {
	localIntegration.value = { ...localIntegration.value, ...data };
}

/**
 * 测试连接
 */
async function handleTestConnection() {
	try {
		const result = await testConnection(props.integration.id);
		if (result.success) {
			ElMessage.success(result.message || 'Connection test successful');
		} else {
			ElMessage.error(result.message || 'Connection test failed');
		}
	} catch (error) {
		console.error('Connection test failed:', error);
		ElMessage.error('Connection test failed');
	}
}

/**
 * 保存配置
 */
async function handleSave() {
	isSaving.value = true;
	try {
		await updateIntegration(props.integration.id, {
			name: localIntegration.value.name,
			connection: localIntegration.value.connection,
			inboundSettings: localIntegration.value.inboundSettings,
			outboundSettings: localIntegration.value.outboundSettings,
		});

		ElMessage.success('Configuration saved successfully');
		emit('refresh');
	} catch (error) {
		console.error('Failed to save configuration:', error);
		ElMessage.error('Failed to save configuration');
	} finally {
		isSaving.value = false;
	}
}

/**
 * 取消编辑
 */
function handleCancel() {
	localIntegration.value = { ...props.integration };
	isExpanded.value = false;
}

/**
 * 删除集成
 */
async function handleDelete() {
	try {
		await ElMessageBox.confirm(
			'Are you sure you want to delete this integration? This action cannot be undone.',
			'Delete Integration',
			{
				confirmButtonText: 'Delete',
				cancelButtonText: 'Cancel',
				type: 'warning',
				confirmButtonClass: 'el-button--danger',
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
