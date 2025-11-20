<template>
	<div class="wfe-global-block-bg p-4">
		<!-- 标题和描述 -->
		<div>
			<h3 class="text-xl font-semibold text-text-primary m-0">Connection & Authentication</h3>
			<p class="text-sm text-text-secondary my-2">
				Configure connection details and authentication method
			</p>
		</div>

		<el-form ref="formRef" :model="formData" :rules="rules" label-position="top">
			<!-- System Name & Endpoint URL (两列布局) -->
			<div class="grid grid-cols-2 gap-6">
				<el-form-item label="System Name" prop="systemName">
					<el-input
						v-model="formData.systemName"
						placeholder="BNP"
						@change="handleFormChange"
					/>
				</el-form-item>

				<el-form-item label="Endpoint URL" prop="endpointUrl">
					<el-input
						v-model="formData.endpointUrl"
						placeholder="https://api.example.com"
						@change="handleFormChange"
					/>
				</el-form-item>
			</div>

			<!-- Authentication Method -->
			<el-form-item label="Authentication Method" prop="authMethod">
				<el-radio-group
					class="w-full"
					v-model="formData.authMethod"
					@change="handleAuthMethodChange"
				>
					<div class="w-full flex items-center gap-2">
						<el-radio value="api_key" class="w-1/2">API Key</el-radio>
						<el-radio value="oauth2">OAuth 2.0</el-radio>
					</div>
					<div class="w-full flex items-center gap-2">
						<el-radio value="basic" class="w-1/2">Basic Auth</el-radio>
						<el-radio value="bearer">Bearer Token</el-radio>
					</div>
				</el-radio-group>
			</el-form-item>

			<!-- Credentials (两列布局) -->
			<div class="grid grid-cols-2 gap-6">
				<!-- API Key -->
				<template v-if="formData.authMethod === 'api_key'">
					<el-form-item label="API Key" prop="credentials.apiKey">
						<el-input
							v-model="formData.credentials.apiKey"
							type="password"
							placeholder="Enter API key"
							show-password
							@change="handleFormChange"
						/>
					</el-form-item>
				</template>

				<!-- Basic Auth -->
				<template v-else-if="formData.authMethod === 'basic'">
					<el-form-item label="Username" prop="credentials.username">
						<el-input
							v-model="formData.credentials.username"
							placeholder="Enter username"
							@change="handleFormChange"
						/>
					</el-form-item>
					<el-form-item label="Password" prop="credentials.password">
						<el-input
							v-model="formData.credentials.password"
							type="password"
							placeholder="Enter password"
							show-password
							@change="handleFormChange"
						/>
					</el-form-item>
				</template>

				<!-- Bearer Token -->
				<template v-else-if="formData.authMethod === 'bearer'">
					<el-form-item label="Bearer Token" prop="credentials.token">
						<el-input
							v-model="formData.credentials.token"
							type="password"
							placeholder="Enter bearer token"
							show-password
							@change="handleFormChange"
						/>
					</el-form-item>
				</template>

				<!-- OAuth 2.0 -->
				<template v-else-if="formData.authMethod === 'oauth2'">
					<el-form-item label="Client ID" prop="credentials.clientId">
						<el-input
							v-model="formData.credentials.clientId"
							placeholder="Enter client ID"
							@change="handleFormChange"
						/>
					</el-form-item>
					<el-form-item label="Client Secret" prop="credentials.clientSecret">
						<el-input
							v-model="formData.credentials.clientSecret"
							type="password"
							placeholder="Enter client secret"
							show-password
							@change="handleFormChange"
						/>
					</el-form-item>
				</template>
			</div>

			<!-- Test Connection Button (右对齐) -->
			<div class="flex justify-end">
				<el-button
					type="primary"
					size="large"
					:loading="isTesting"
					:disabled="!isFormValid"
					@click="handleTestConnection"
				>
					<el-icon class="mr-2"><Connection /></el-icon>
					Test Connection
				</el-button>
			</div>
		</el-form>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import { Connection } from '@element-plus/icons-vue';
import type { IIntegrationConfig, IConnectionConfig } from '#/integration';

interface Props {
	integration: IIntegrationConfig;
}

interface Emits {
	(e: 'update', data: Partial<IIntegrationConfig>): void;
	(e: 'test'): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

// 表单引用
const formRef = ref<FormInstance>();

// 表单数据
const formData = reactive<IConnectionConfig>({
	systemName: props.integration.connection?.systemName || '',
	endpointUrl: props.integration.connection?.endpointUrl || '',
	authMethod: props.integration.connection?.authMethod || 'api_key',
	credentials: props.integration.connection?.credentials || {},
});

// 表单验证规则
const rules = reactive<FormRules>({
	systemName: [{ required: true, message: 'Please enter system name', trigger: 'blur' }],
	endpointUrl: [
		{ required: true, message: 'Please enter endpoint URL', trigger: 'blur' },
		{ type: 'url', message: 'Please enter a valid URL', trigger: 'blur' },
	],
	authMethod: [{ required: true, message: 'Please select auth method', trigger: 'change' }],
	'credentials.apiKey': [
		{
			required: computed(() => formData.authMethod === 'api_key').value,
			message: 'Please enter API key',
			trigger: 'blur',
		},
	],
	'credentials.username': [
		{
			required: computed(() => formData.authMethod === 'basic').value,
			message: 'Please enter username',
			trigger: 'blur',
		},
	],
	'credentials.password': [
		{
			required: computed(() => formData.authMethod === 'basic').value,
			message: 'Please enter password',
			trigger: 'blur',
		},
	],
	'credentials.token': [
		{
			required: computed(() => formData.authMethod === 'bearer').value,
			message: 'Please enter bearer token',
			trigger: 'blur',
		},
	],
});

// 状态
const isTesting = ref(false);

// 表单是否有效
const isFormValid = computed(() => {
	return (
		formData.systemName && formData.endpointUrl && formData.authMethod && hasValidCredentials()
	);
});

/**
 * 检查凭证是否有效
 */
function hasValidCredentials(): boolean {
	switch (formData.authMethod) {
		case 'api_key':
			return !!formData.credentials.apiKey;
		case 'basic':
			return !!formData.credentials.username && !!formData.credentials.password;
		case 'bearer':
			return !!formData.credentials.token;
		case 'oauth2':
			return !!formData.credentials.clientId && !!formData.credentials.clientSecret;
		default:
			return false;
	}
}

/**
 * 处理认证方式变更
 */
function handleAuthMethodChange() {
	// 清空凭证
	formData.credentials = {};
	handleFormChange();
}

/**
 * 处理表单变更
 */
function handleFormChange() {
	emit('update', {
		connection: { ...formData },
	});
}

/**
 * 测试连接
 */
async function handleTestConnection() {
	if (!formRef.value) return;

	try {
		await formRef.value.validate();
		isTesting.value = true;
		emit('test');
	} catch (error) {
		console.error('Form validation failed:', error);
	} finally {
		setTimeout(() => {
			isTesting.value = false;
		}, 1000);
	}
}

// 监听 props 变化
watch(
	() => props.integration.connection,
	(newConnection) => {
		if (newConnection) {
			Object.assign(formData, newConnection);
		}
	},
	{ deep: true }
);
</script>

<style scoped lang="scss">
// Element Plus 表单样式覆盖
:deep(.el-form-item__label) {
	color: var(--el-text-color-primary);
	font-weight: 500;
	font-size: 14px;
}

:deep(.el-input__wrapper) {
	background: var(--el-bg-color);
	border-color: var(--el-border-color-lighter);
}

:deep(.el-radio) {
	margin-right: 24px;
	margin-bottom: 0;
}
</style>
