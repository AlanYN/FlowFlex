<template>
	<div class="min-h-screen">
		<PageHeader
			:title="integration?.name || 'Integration Details'"
			description="Configure connection and data exchange settings"
			:show-back-button="true"
			@go-back="handleBack"
		>
			<template #actions>
				<el-tag
					v-if="integration"
					:type="integration.status === 'connected' ? 'success' : 'info'"
				>
					{{ integration.status === 'connected' ? 'Connected' : 'Disconnected' }}
				</el-tag>
			</template>
		</PageHeader>

		<!-- 加载状态 -->
		<div v-if="isLoading" v-loading="true" class="min-h-[400px]"></div>

		<!-- 详情内容 -->
		<div v-else-if="integration" class="space-y-6 pb-8">
			<div class="bg-bg-overlay">
				<connection-auth
					:integration="integration"
					@update="handleUpdate"
					@test="handleTestConnection"
				/>
			</div>

			<!-- Tabs -->
			<div class="bg-bg-overlay">
				<PrototypeTabs v-model="activeTab" :tabs="tabsConfig" class="">
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
			</div>

			<!-- 底部操作按钮 -->
			<div class="flex justify-between items-center">
				<el-button
					v-if="integration.id !== 'new'"
					type="danger"
					:icon="Delete"
					@click="handleDelete"
				>
					Delete Integration
				</el-button>
				<div v-else></div>
				<div class="flex gap-3">
					<el-button type="primary" :loading="isSaving" @click="handleSave">
						{{ integration.id === 'new' ? 'Create Integration' : 'Save Configuration' }}
					</el-button>
				</div>
			</div>
		</div>

		<!-- 错误状态 -->
		<el-empty v-else description="Integration not found">
			<el-button type="primary" @click="handleBack">Go Back</el-button>
		</el-empty>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Delete } from '@element-plus/icons-vue';
import { PrototypeTabs, TabPane } from '@/components/PrototypeTabs';
import ConnectionAuth from './components/connection-auth.vue';
import InboundSettings from './components/inbound-settings.vue';
import OutboundSettings from './components/outbound-settings.vue';
import ActionsList from './components/actions-list.vue';
import {
	getIntegration,
	createIntegration,
	updateIntegration,
	deleteIntegration,
	testConnection,
} from '@/apis/integration';
import type { IIntegrationConfig } from '#/integration';
import PageHeader from '@/components/global/PageHeader/index.vue';

const route = useRoute();
const router = useRouter();

// 状态管理
const integration = ref<IIntegrationConfig | null>(null);
const isLoading = ref(false);
const isSaving = ref(false);
const activeTab = ref('inbound');

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
 * 加载集成详情
 */
async function loadIntegration() {
	const id = route.params.id as string;
	if (!id) {
		ElMessage.error('Invalid integration ID');
		handleBack();
		return;
	}

	// 如果是创建新集成，创建一个空的集成对象
	if (id === 'new') {
		integration.value = {
			id: 'new',
			type: 'custom',
			name: 'New Integration',
			status: 'disconnected',
			connection: {
				systemName: '',
				endpointUrl: '',
				authMethod: 'api_key',
				credentials: {},
			},
			inboundSettings: {
				entityMappings: [],
				fieldMappings: [],
				attachmentSharing: [],
			},
			outboundSettings: {
				masterData: [],
				fields: [],
				attachmentWorkflows: [],
			},
			actions: [],
			createdAt: new Date().toISOString(),
			updatedAt: new Date().toISOString(),
		};
		return;
	}

	isLoading.value = true;
	try {
		integration.value = await getIntegration(id);
	} catch (error) {
		console.error('Failed to load integration:', error);
		ElMessage.error('Failed to load integration details');
	} finally {
		isLoading.value = false;
	}
}

/**
 * 更新集成配置
 */
function handleUpdate(data: Partial<IIntegrationConfig>) {
	if (integration.value) {
		integration.value = { ...integration.value, ...data };
	}
}

/**
 * 测试连接
 */
async function handleTestConnection() {
	if (!integration.value) return;

	try {
		const result = await testConnection(integration.value.id);
		if (result.success) {
			ElMessage.success('Connection test successful');
			integration.value.status = 'connected';
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
	if (!integration.value) return;

	isSaving.value = true;
	try {
		// 如果是新建集成，调用创建接口
		if (integration.value.id === 'new') {
			const newIntegration = await createIntegration({
				type: integration.value.type,
				name: integration.value.name,
			});

			ElMessage.success('Integration created successfully');

			// 然后更新集成配置
			const updatedIntegration = await updateIntegration(newIntegration.id, {
				connection: integration.value.connection,
				inboundSettings: integration.value.inboundSettings,
				outboundSettings: integration.value.outboundSettings,
			});

			integration.value = updatedIntegration;

			// 更新路由为实际的集成 ID
			router.replace({
				name: 'IntegrationDetail',
				params: { id: updatedIntegration.id },
			});
		} else {
			// 否则调用更新接口
			await updateIntegration(integration.value.id, integration.value);
			ElMessage.success('Configuration saved successfully');
		}
	} catch (error) {
		console.error('Failed to save configuration:', error);
		ElMessage.error('Failed to save configuration');
	} finally {
		isSaving.value = false;
	}
}

/**
 * 删除集成
 */
async function handleDelete() {
	if (!integration.value) return;

	try {
		await ElMessageBox.confirm(
			`Are you sure you want to delete the integration "${integration.value.name}"? This action cannot be undone.`,
			'Confirm Deletion',
			{
				confirmButtonText: 'Delete',
				cancelButtonText: 'Cancel',
				type: 'warning',
			}
		);

		await deleteIntegration(integration.value.id);
		ElMessage.success('Integration deleted successfully');
		handleBack();
	} catch (error) {
		if (error !== 'cancel') {
			console.error('Failed to delete integration:', error);
			ElMessage.error('Failed to delete integration');
		}
	}
}

/**
 * 返回列表
 */
function handleBack() {
	router.push({ name: 'Integration' });
}

// 初始化
onMounted(() => {
	loadIntegration();
});
</script>

<style scoped lang="scss"></style>
