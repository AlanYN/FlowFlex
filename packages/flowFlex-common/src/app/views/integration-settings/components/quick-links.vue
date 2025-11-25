<template>
	<div class="space-y-6">
		<!-- Header -->
		<div class="flex justify-between items-start">
			<div>
				<h3 class="text-lg font-semibold text-text-primary m-0">Quick Links</h3>
				<p class="text-sm text-text-secondary mt-1">
					Configure external system pages for quick redirection from workflows.
				</p>
			</div>
			<el-button type="primary" @click="handleAddQuickLink">
				<el-icon><Plus /></el-icon>
				Add Quick Link
			</el-button>
		</div>

		<!-- Info Alert -->
		<div class="border-2 p-4 flex gap-x-2">
			<Icon icon="lucide:link" class="w-5 h-5 text-gray-500" />
			<div>
				<div class="font-semibold text-sm">How Quick Links Work in Workflows</div>
				<div class="text-sm mt-1 text-gray-500">
					Configured quick links become available as "External Link" components in
					workflow stages. Users can click these components in workflow cases to quickly
					navigate to relevant pages in IAM System.
				</div>
			</div>
		</div>

		<!-- Quick Links Table -->
		<el-table
			v-loading="isLoading"
			:data="quickLinks"
			class="w-full"
			empty-text="No quick links configured"
			:border="true"
		>
			<el-table-column label="Link Name" prop="name" min-width="180">
				<template #default="{ row }">
					<span class="font-medium text-sm">{{ row.name }}</span>
				</template>
			</el-table-column>

			<el-table-column label="Description" prop="description" min-width="250">
				<template #default="{ row }">
					<span class="text-sm text-text-secondary">
						{{ getDescription(row) }}
					</span>
				</template>
			</el-table-column>

			<el-table-column label="Target URL" prop="targetUrl" min-width="300">
				<template #default="{ row }">
					<div class="flex items-center gap-2">
						<span class="text-sm font-mono">{{ row.targetUrl }}</span>
						<el-icon class="text-text-secondary">
							<Link />
						</el-icon>
					</div>
				</template>
			</el-table-column>

			<el-table-column label="Icon" prop="icon" width="100" align="center">
				<template #default="{ row }">
					<el-icon v-if="row.icon" class="text-lg">
						<component :is="getIconComponent(row.icon)" />
					</el-icon>
					<span v-else class="text-text-secondary text-sm">-</span>
				</template>
			</el-table-column>

			<el-table-column label="Confirmation" prop="redirectType" width="120" align="center">
				<template #default="{ row }">
					<span class="text-sm">
						{{ row.redirectType === RedirectType.Direct ? 'Direct' : 'Popup' }}
					</span>
				</template>
			</el-table-column>

			<el-table-column label="Status" prop="status" width="100" align="center">
				<template #default="{ row }">
					<el-tag size="small" :type="getStatus(row) === 'Active' ? 'success' : 'info'">
						{{ getStatus(row) }}
					</el-tag>
				</template>
			</el-table-column>

			<el-table-column label="Actions" width="120" align="center">
				<template #default="{ row }">
					<div class="flex items-center justify-center gap-2">
						<el-button text type="primary" size="small" @click="handleEdit(row)">
							Edit
						</el-button>
						<el-button text type="danger" size="small" @click="handleDelete(row)">
							<el-icon><Delete /></el-icon>
						</el-button>
					</div>
				</template>
			</el-table-column>
		</el-table>

		<!-- Edit/Add Quick Link Dialog -->
		<el-dialog
			v-model="dialogVisible"
			:title="isEditMode ? 'Edit Quick Link' : 'Add Quick Link'"
			width="600px"
			:close-on-click-modal="false"
			append-to-body
		>
			<template #header>
				<div class="flex flex-col gap-1">
					<div class="text-lg font-semibold">
						{{ isEditMode ? 'Edit Quick Link' : 'Add Quick Link' }}
					</div>
					<div class="text-sm text-text-secondary">
						Configure external page redirection for use in workflows
					</div>
				</div>
			</template>

			<el-form
				ref="formRef"
				:model="formData"
				label-position="top"
				:rules="formRules"
				label-width="160px"
			>
				<!-- Link Name -->
				<el-form-item label="Link Name" prop="name" required>
					<el-input
						v-model="formData.name"
						placeholder="e.g., View Customer in CRM"
						maxlength="100"
						show-word-limit
					/>
				</el-form-item>

				<!-- Description -->
				<el-form-item label="Description" prop="description">
					<el-input
						v-model="formData.description"
						type="textarea"
						:rows="3"
						placeholder="Optional description of what this link does..."
						maxlength="500"
						show-word-limit
					/>
				</el-form-item>

				<!-- Target URL -->
				<el-form-item label="Target URL" prop="targetUrl" required>
					<el-input
						v-model="formData.targetUrl"
						placeholder="https://external.com/page"
						maxlength="500"
						show-word-limit
					/>
					<div class="text-xs text-text-secondary mt-1">
						Must start with http:// or https://
					</div>
				</el-form-item>

				<!-- Parameter Configuration -->
				<el-form-item label="Parameter Configuration">
					<div class="w-full">
						<div class="flex justify-end mb-2">
							<el-button text type="primary" size="small" @click="handleAddParameter">
								<el-icon><Plus /></el-icon>
								Add Parameter
							</el-button>
						</div>
						<div
							v-if="
								!formData.parameters ||
								Object.keys(formData.parameters).length === 0
							"
							class="parameter-empty"
						>
							No parameters configured. Click "Add Parameter" to add dynamic
							parameters.
						</div>
						<div v-else class="space-y-2">
							<div
								v-for="(_, key) in formData.parameters"
								:key="key"
								class="parameter-item"
							>
								<div class="flex items-center gap-2">
									<el-input
										v-model="parameterKeys[key]"
										placeholder="Parameter key"
										class="flex-1"
										@blur="handleParameterKeyChange(key, parameterKeys[key])"
									/>
									<span class="text-text-secondary">:</span>
									<el-input
										v-model="formData.parameters![key]"
										placeholder="Parameter value"
										class="flex-1"
									/>
									<el-button
										text
										type="danger"
										size="small"
										@click="handleRemoveParameter(key)"
									>
										<el-icon><Delete /></el-icon>
									</el-button>
								</div>
							</div>
						</div>
					</div>
				</el-form-item>

				<!-- Display Icon -->
				<el-form-item label="Display Icon" prop="icon">
					<el-select v-model="formData.icon" placeholder="Select icon..." class="w-full">
						<el-option
							v-for="icon in iconOptions"
							:key="icon.value"
							:label="icon.label"
							:value="icon.value"
						>
							<div class="flex items-center gap-2">
								<el-icon><component :is="icon.component" /></el-icon>
								<span>{{ icon.label }}</span>
							</div>
						</el-option>
					</el-select>
				</el-form-item>

				<!-- Redirect Confirmation -->
				<el-form-item label="Redirect Confirmation" prop="redirectType" required>
					<el-radio-group v-model="formData.redirectType">
						<el-radio :label="RedirectType.Direct">Direct Redirect</el-radio>
						<el-radio :label="RedirectType.PopupConfirmation">
							Popup Confirmation
						</el-radio>
					</el-radio-group>
				</el-form-item>

				<!-- Status -->
				<el-form-item label="Status">
					<div class="flex items-center gap-3">
						<div class="flex-1">
							<div class="text-sm text-text-secondary">
								Set whether this quick link is active or inactive
							</div>
						</div>
						<el-switch v-model="formData.isActive" />
					</div>
				</el-form-item>
			</el-form>

			<template #footer>
				<div class="flex justify-end gap-3">
					<el-button @click="handleDialogCancel">Cancel</el-button>
					<el-button type="primary" :loading="isSaving" @click="handleSave">
						{{ isEditMode ? 'Update Quick Link' : 'Create Quick Link' }}
					</el-button>
				</div>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, reactive } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import { Plus, Delete, Link, ArrowRight } from '@element-plus/icons-vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import {
	getQuickLinksByIntegration,
	deleteQuickLink,
	createQuickLink,
	updateQuickLink,
} from '@/apis/integration';
import { RedirectType } from '@/enums/integration';
import type { IQuickLink } from '#/integration';

interface Props {
	integrationId: string | number;
}

const props = defineProps<Props>();

// 状态管理
const isLoading = ref(false);
const quickLinks = ref<IQuickLink[]>([]);

// 对话框状态
const dialogVisible = ref(false);
const isEditMode = ref(false);
const isSaving = ref(false);
const formRef = ref<FormInstance>();
const currentEditId = ref<string | number | null>(null);

// 参数管理
const parameterKeys = ref<Record<string, string>>({});

// 表单数据
const formData = reactive<{
	name: string;
	description?: string;
	targetUrl: string;
	icon?: string;
	redirectType: RedirectType;
	parameters?: Record<string, any>;
	isActive: boolean;
}>({
	name: '',
	description: '',
	targetUrl: '',
	icon: '',
	redirectType: RedirectType.Direct,
	parameters: {},
	isActive: true,
});

// 表单验证规则
const formRules: FormRules = {
	name: [
		{ required: true, message: 'Please enter link name', trigger: 'blur' },
		{ max: 100, message: 'Link name cannot exceed 100 characters', trigger: 'blur' },
	],
	targetUrl: [
		{ required: true, message: 'Please enter target URL', trigger: 'blur' },
		{
			validator: (rule, value, callback) => {
				if (!value) {
					callback(new Error('Please enter target URL'));
					return;
				}
				if (!value.startsWith('http://') && !value.startsWith('https://')) {
					callback(new Error('URL must start with http:// or https://'));
					return;
				}
				callback();
			},
			trigger: 'blur',
		},
		{ max: 500, message: 'Target URL cannot exceed 500 characters', trigger: 'blur' },
	],
	redirectType: [
		{ required: true, message: 'Please select redirect confirmation type', trigger: 'change' },
	],
};

// 图标选项
const iconOptions = [
	{ value: 'link', label: 'External Link', component: Link },
	{ value: 'chain', label: 'Chain Link', component: Link },
	{ value: 'arrow', label: 'Arrow', component: ArrowRight },
];

/**
 * 获取描述信息（从 parameters 或其他字段获取）
 */
function getDescription(link: IQuickLink): string {
	// 如果接口中有 description 字段，直接返回
	// 否则可以从 parameters 或其他地方获取
	return (link as any).description || 'No description available';
}

/**
 * 获取状态（Active/Inactive）
 */
function getStatus(link: IQuickLink): 'Active' | 'Inactive' {
	// 如果接口中有 status 字段，使用它
	// 否则默认返回 Active
	return (link as any).status !== undefined
		? (link as any).status === 1 || (link as any).status === true
			? 'Active'
			: 'Inactive'
		: 'Active';
}

/**
 * 获取图标组件
 */
function getIconComponent(iconName: string) {
	// 根据图标名称返回对应的图标组件
	const iconMap: Record<string, any> = {
		link: Link,
		external: Link,
		arrow: 'ArrowRight',
		chain: Link,
	};
	return iconMap[iconName.toLowerCase()] || Link;
}

/**
 * 加载快速链接列表
 */
async function loadQuickLinks() {
	if (!props.integrationId || props.integrationId === 'new') {
		quickLinks.value = [];
		return;
	}

	isLoading.value = true;
	try {
		const response = await getQuickLinksByIntegration(props.integrationId);
		if (response.success && response.data) {
			quickLinks.value = response.data;
		} else {
			quickLinks.value = [];
		}
	} catch (error) {
		console.error('Failed to load quick links:', error);
		quickLinks.value = [];
	} finally {
		isLoading.value = false;
	}
}

/**
 * 重置表单
 */
function resetForm() {
	formData.name = '';
	formData.description = '';
	formData.targetUrl = '';
	formData.icon = '';
	formData.redirectType = RedirectType.Direct;
	formData.parameters = {};
	formData.isActive = true;
	parameterKeys.value = {};
	currentEditId.value = null;
	formRef.value?.clearValidate();
}

/**
 * 打开添加对话框
 */
function handleAddQuickLink() {
	isEditMode.value = false;
	resetForm();
	dialogVisible.value = true;
}

/**
 * 打开编辑对话框
 */
async function handleEdit(link: IQuickLink) {
	if (!link.id) return;

	isEditMode.value = true;
	currentEditId.value = link.id;
	resetForm();

	// 填充表单数据
	formData.name = link.name || '';
	formData.description = (link as any).description || '';
	formData.targetUrl = link.targetUrl || '';
	formData.icon = link.icon || '';
	formData.redirectType = link.redirectType ?? RedirectType.Direct;
	formData.parameters = link.parameters ? { ...link.parameters } : {};
	formData.isActive = getStatus(link) === 'Active';

	// 初始化参数键名
	if (formData.parameters) {
		parameterKeys.value = {};
		Object.keys(formData.parameters).forEach((key) => {
			parameterKeys.value[key] = key;
		});
	}

	dialogVisible.value = true;
}

/**
 * 添加参数
 */
function handleAddParameter() {
	if (!formData.parameters) {
		formData.parameters = {};
	}
	const newKey = `param_${Date.now()}`;
	formData.parameters[newKey] = '';
	parameterKeys.value[newKey] = newKey;
}

/**
 * 移除参数
 */
function handleRemoveParameter(key: string) {
	if (formData.parameters) {
		delete formData.parameters[key];
		delete parameterKeys.value[key];
	}
}

/**
 * 参数键名变更
 */
function handleParameterKeyChange(oldKey: string, newKey: string) {
	if (!formData.parameters || oldKey === newKey) return;

	if (newKey && newKey.trim() !== '') {
		const value = formData.parameters[oldKey];
		delete formData.parameters[oldKey];
		delete parameterKeys.value[oldKey];
		formData.parameters[newKey] = value;
		parameterKeys.value[newKey] = newKey;
	}
}

/**
 * 取消对话框
 */
function handleDialogCancel() {
	dialogVisible.value = false;
	resetForm();
}

/**
 * 保存快速链接
 */
async function handleSave() {
	if (!formRef.value) return;

	await formRef.value.validate(async (valid) => {
		if (!valid) return;

		isSaving.value = true;
		try {
			const quickLinkData: Omit<IQuickLink, 'id'> = {
				integrationId: props.integrationId,
				name: formData.name,
				targetUrl: formData.targetUrl,
				icon: formData.icon || undefined,
				redirectType: formData.redirectType,
				parameters:
					formData.parameters && Object.keys(formData.parameters).length > 0
						? formData.parameters
						: undefined,
			};

			// 添加 description（如果接口支持）
			if (formData.description) {
				(quickLinkData as any).description = formData.description;
			}

			// 添加 status（如果接口支持）
			(quickLinkData as any).status = formData.isActive ? 1 : 0;

			let response;
			if (isEditMode.value && currentEditId.value) {
				response = await updateQuickLink(currentEditId.value, quickLinkData);
			} else {
				response = await createQuickLink(quickLinkData);
			}

			if (response.success) {
				ElMessage.success(
					isEditMode.value
						? 'Quick link updated successfully'
						: 'Quick link created successfully'
				);
				dialogVisible.value = false;
				resetForm();
				loadQuickLinks();
			} else {
				ElMessage.error(response.msg || 'Failed to save quick link');
			}
		} catch (error) {
			console.error('Failed to save quick link:', error);
			ElMessage.error('Failed to save quick link');
		} finally {
			isSaving.value = false;
		}
	});
}

/**
 * 删除快速链接
 */
async function handleDelete(link: IQuickLink) {
	if (!link.id) return;

	try {
		await ElMessageBox.confirm(
			`Are you sure you want to delete the quick link "${link.name}"?`,
			'Confirm Deletion',
			{
				confirmButtonText: 'Delete',
				cancelButtonText: 'Cancel',
				type: 'warning',
			}
		);

		const response = await deleteQuickLink(link.id);
		if (response.success) {
			ElMessage.success('Quick link deleted successfully');
			loadQuickLinks();
		} else {
			ElMessage.error(response.msg || 'Failed to delete quick link');
		}
	} catch (error) {
		if (error !== 'cancel') {
			console.error('Failed to delete quick link:', error);
			ElMessage.error('Failed to delete quick link');
		}
	}
}

// 监听 integrationId 变化
watch(
	() => props.integrationId,
	() => {
		loadQuickLinks();
	},
	{ immediate: true }
);

// 初始化
onMounted(() => {
	loadQuickLinks();
});
</script>

<style scoped lang="scss">
.parameter-empty {
	padding: 16px;
	text-align: center;
	color: var(--el-text-color-secondary);
	font-size: 14px;
	background: var(--el-fill-color-light);
	border: 1px dashed var(--el-border-color-lighter);
	border-radius: 4px;
}

.parameter-item {
	padding: 8px;
	background: var(--el-fill-color-lighter);
	border-radius: 4px;
}
</style>
