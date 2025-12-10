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
			<el-button
				type="primary"
				@click="handleAddQuickLink"
				v-permission="ProjectPermissionEnum.integration.create"
			>
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
			<el-table-column label="Link Name" prop="linkName" min-width="180">
				<template #default="{ row }">
					<span class="font-medium text-sm">{{ row.linkName }}</span>
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
						<el-link
							:underline="false"
							class="cursor-pointer"
							@click="handleLinkClick(row)"
						>
							{{ row.targetUrl }}
						</el-link>
					</div>
				</template>
			</el-table-column>

			<el-table-column label="Icon" prop="displayIcon" width="100" align="center">
				<template #default="{ row }">
					<el-icon v-if="row.displayIcon" class="text-lg">
						<Icon :icon="getIconComponent(row.displayIcon)" />
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
					<el-tag :type="row.isActive ? 'success' : 'info'">
						{{ row.isActive ? 'Active' : 'Inactive' }}
					</el-tag>
				</template>
			</el-table-column>

			<el-table-column label="Actions" min-width="180" align="center">
				<template #default="{ row }">
					<div class="flex items-center justify-center gap-2">
						<el-button
							link
							type="primary"
							:icon="Edit"
							:loading="isLoadingEdit && currentEditId === row.id"
							@click="handleEdit(row)"
						/>
						<el-button link type="danger" :icon="Delete" @click="handleDelete(row)" />
					</div>
				</template>
			</el-table-column>
		</el-table>

		<!-- Edit/Add Quick Link Dialog -->
		<el-dialog
			v-model="dialogVisible"
			:title="isEditMode ? 'Edit Quick Link' : 'Add Quick Link'"
			:width="dialogWidth"
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

			<div v-loading="isLoadingEdit" class="dialog-content-wrapper">
				<el-form
					ref="formRef"
					:model="formData"
					label-position="top"
					:rules="formRules"
					label-width="160px"
				>
					<!-- Link Name -->
					<el-form-item label="Link Name" prop="linkName" required>
						<el-input
							v-model="formData.linkName"
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
					<el-form-item prop="urlParameters" class="w-full" label-width="100">
						<div class="w-full flex justify-between items-center mb-4">
							<span class="text-sm font-medium text-text-primary">
								Parameter Configuration
							</span>
							<el-button link type="primary" :icon="Plus" @click="handleAddParameter">
								Add Parameter
							</el-button>
						</div>
						<el-scrollbar max-height="400px" class="w-full parameter-scrollbar">
							<div class="w-full">
								<div
									v-if="
										!formData.urlParameters ||
										formData.urlParameters.length === 0
									"
									class="parameter-empty"
								>
									No parameters configured. Click "Add Parameter" to add dynamic
									parameters.
								</div>
								<div v-else class="space-y-3">
									<div
										v-for="(param, index) in formData.urlParameters"
										:key="index"
										class="parameter-card"
									>
										<div class="parameter-card-header">
											<span class="parameter-card-title">
												Parameter {{ index + 1 }}
											</span>
											<el-button
												link
												type="danger"
												:icon="Delete"
												@click="handleRemoveParameter(index)"
											/>
										</div>
										<div class="parameter-card-content">
											<!-- Parameter Name -->
											<el-form-item
												:prop="`urlParameters.${index}.name`"
												:rules="[
													{
														required: true,
														message: 'Please enter parameter name',
														trigger: 'blur',
													},
												]"
												class="parameter-form-item"
											>
												<template #label>
													<span class="text-sm font-medium">
														Parameter Name
													</span>
												</template>
												<el-input
													v-model="param.name"
													placeholder="e.g., orderNo, userId"
													maxlength="100"
												/>
											</el-form-item>

											<!-- Value Source and Value Detail -->
											<div class="flex gap-3">
												<el-form-item
													:prop="`urlParameters.${index}.valueSource`"
													:rules="[
														{
															required: true,
															message: 'Please select value source',
															trigger: 'change',
														},
													]"
													class="parameter-form-item flex-1"
												>
													<template #label>
														<span class="text-sm font-medium">
															Value Source
														</span>
													</template>
													<el-select
														v-model="param.valueSource"
														placeholder="Select value source"
														class="w-full"
														@change="handleValueSourceChange(index)"
													>
														<el-option
															v-for="option in valueSourceOptions"
															:key="option.value"
															:label="option.label"
															:value="option.value"
														/>
													</el-select>
												</el-form-item>

												<el-form-item
													:prop="`urlParameters.${index}.valueDetail`"
													:rules="[
														{
															required: true,
															message: isValueDetailSelect(
																param.valueSource
															)
																? 'Please select value detail'
																: 'Please enter value detail',
															trigger: isValueDetailSelect(
																param.valueSource
															)
																? 'change'
																: 'blur',
															validator: (rule, value, callback) => {
																// 如果 valueSource 未选择，不进行验证
																if (
																	param.valueSource === null ||
																	param.valueSource === undefined
																) {
																	callback();
																	return;
																}
																if (!value || value.trim() === '') {
																	callback(
																		new Error(
																			isValueDetailSelect(
																				param.valueSource
																			)
																				? 'Please select value detail'
																				: 'Please enter value detail'
																		)
																	);
																	return;
																}
																callback();
															},
														},
													]"
													class="parameter-form-item flex-1"
												>
													<template #label>
														<span class="text-sm font-medium">
															Value Detail
														</span>
													</template>
													<!-- 下拉选择：Page Parameter, Login User Info, System Variable -->
													<el-select
														v-if="
															isValueDetailSelect(param.valueSource)
														"
														v-model="param.valueDetail"
														:placeholder="
															getValueDetailPlaceholder(
																param.valueSource
															)
														"
														class="w-full"
														:disabled="
															param.valueSource === null ||
															param.valueSource === undefined
														"
													>
														<el-option
															v-for="option in getValueDetailOptions(
																param.valueSource
															)"
															:key="option.value"
															:label="option.label"
															:value="option.value"
														/>
													</el-select>
													<!-- 文本输入框：Fixed Value -->
													<el-input
														v-else
														v-model="param.valueDetail"
														:placeholder="
															getValueDetailPlaceholder(
																param.valueSource
															)
														"
														maxlength="500"
													/>
												</el-form-item>
											</div>
										</div>
									</div>
								</div>
							</div>
						</el-scrollbar>
					</el-form-item>

					<!-- Display Icon -->
					<el-form-item label="Display Icon" prop="icon">
						<el-select
							v-model="formData.displayIcon"
							placeholder="Select icon..."
							class="w-full"
						>
							<template #label="{ label, value }">
								<div class="flex items-center gap-2 text-sm font-medium">
									<Icon
										:icon="
											iconOptions.find((icon) => icon.value === value)
												?.component
										"
									/>
									<span>{{ label }}</span>
								</div>
							</template>
							<el-option
								v-for="icon in iconOptions"
								:key="icon.value"
								:label="icon.label"
								:value="icon.value"
							>
								<div class="flex items-center gap-2">
									<Icon :icon="icon.component" />
									<span>{{ icon.label }}</span>
								</div>
							</el-option>
						</el-select>
					</el-form-item>

					<!-- Redirect Confirmation -->
					<el-form-item label="Redirect Confirmation" prop="redirectType" required>
						<el-radio-group v-model="formData.redirectType">
							<el-radio :value="RedirectType.Direct">Direct Redirect</el-radio>
							<el-radio :value="RedirectType.PopupConfirmation">
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
			</div>

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
import { ref, onMounted, watch, nextTick } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import { Plus, Delete, Edit } from '@element-plus/icons-vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import {
	getQuickLinksByIntegration,
	deleteQuickLink,
	createQuickLink,
	updateQuickLink,
	getQuickLink,
} from '@/apis/integration';
import {
	RedirectType,
	ValueSource,
	PageParameterDetail,
	LoginUserInfoDetail,
	SystemVariableDetail,
} from '@/enums/integration';
import type { IQuickLink } from '#/integration';
import { dialogWidth } from '@/settings/projectSetting';
import { ProjectPermissionEnum } from '@/enums/permissionEnum';

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
const isLoadingEdit = ref(false);
const formRef = ref<FormInstance>();
const currentEditId = ref<string | number | null>(null);

// 表单初始值
const initialFormData: Partial<IQuickLink> & {
	description?: string;
	displayIcon?: string;
	isActive: boolean;
	urlParameters?: Array<{
		name: string;
		valueSource: ValueSource;
		valueDetail: string;
	}>;
} = {
	linkName: '',
	description: undefined,
	targetUrl: '',
	displayIcon: 'link',
	redirectType: RedirectType.Direct,
	urlParameters: undefined,
	isActive: true,
};

// 表单数据
const formData = ref({ ...initialFormData } as any);

// 表单验证规则
const formRules: FormRules = {
	linkName: [
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
	{ value: 'link', label: 'External Link', component: 'lucide-external-link' },
	{ value: 'chain', label: 'Chain Link', component: 'lucide-link' },
	{ value: 'arrow', label: 'Arrow', component: 'lucide-arrow-right' },
];

// Value Source 选项
const valueSourceOptions = [
	{ value: ValueSource.PageParameter, label: 'Page Parameter' },
	{ value: ValueSource.LoginUserInfo, label: 'Login User Info' },
	{ value: ValueSource.FixedValue, label: 'Fixed Value' },
	{ value: ValueSource.SystemVariable, label: 'System Variable' },
];

// Page Parameter Detail 选项
const pageParameterDetailOptions = [
	{ value: PageParameterDetail.CaseId, label: 'Case ID' },
	{ value: PageParameterDetail.CustomerId, label: 'Customer ID' },
	{ value: PageParameterDetail.OrderNumber, label: 'Order Number' },
];

// Login User Info Detail 选项
const loginUserInfoDetailOptions = [
	{ value: LoginUserInfoDetail.UserId, label: 'User ID' },
	{ value: LoginUserInfoDetail.Username, label: 'Username' },
	{ value: LoginUserInfoDetail.Email, label: 'Email' },
];

// System Variable Detail 选项
const systemVariableDetailOptions = [
	{ value: SystemVariableDetail.CurrentTimestamp, label: 'Current Timestamp' },
	{ value: SystemVariableDetail.CurrentDate, label: 'Current Date' },
];

/**
 * 获取描述信息（从 urlParameters 或其他字段获取）
 */
function getDescription(link: IQuickLink): string {
	// 如果接口中有 description 字段，直接返回
	// 否则可以从 urlParameters 或其他地方获取
	return (link as any).description || 'No description available';
}

/**
 * 获取图标组件
 */
function getIconComponent(iconName: string) {
	// 根据图标名称返回对应的图标组件
	const iconMap: Record<string, any> = {
		link: 'lucide-external-link',
		chain: 'lucide-link',
		arrow: 'lucide-arrow-right',
	};
	return iconMap[iconName.toLowerCase()] || 'lucide-link';
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
const resetForm = () => {
	formData.value = { ...initialFormData };
	currentEditId.value = null;
	formRef.value?.clearValidate();
};

/**
 * 打开添加对话框
 */
function handleAddQuickLink() {
	isEditMode.value = false;
	resetForm();
	dialogVisible.value = true;
}

/**
 * 处理 urlParameters 数据转换
 */
function normalizeUrlParameters(
	urlParameters: any
): Array<{ name: string; valueSource: ValueSource; valueDetail: string }> {
	if (!urlParameters) return [];

	if (Array.isArray(urlParameters)) {
		return urlParameters.map((param: any) => {
			// 处理 valueSource：可能是数字、字符串或枚举值
			let valueSource = ValueSource.FixedValue;
			if (param.valueSource !== undefined && param.valueSource !== null) {
				if (typeof param.valueSource === 'number') {
					// 如果是数字，直接使用
					valueSource = param.valueSource as ValueSource;
				} else if (typeof param.valueSource === 'string') {
					// 如果是字符串，尝试转换为枚举值
					const sourceMap: Record<string, ValueSource> = {
						PageParameter: ValueSource.PageParameter,
						LoginUserInfo: ValueSource.LoginUserInfo,
						FixedValue: ValueSource.FixedValue,
						SystemVariable: ValueSource.SystemVariable,
					};
					valueSource = sourceMap[param.valueSource] || ValueSource.FixedValue;
				} else {
					valueSource = param.valueSource as ValueSource;
				}
			}

			return {
				name: param.name || param.key || '',
				valueSource,
				valueDetail: String(param.valueDetail || param.value || ''),
			};
		});
	}

	if (typeof urlParameters === 'object') {
		return Object.keys(urlParameters).map((key) => ({
			name: key,
			valueSource: ValueSource.FixedValue,
			valueDetail: String(urlParameters[key] || ''),
		}));
	}

	return [];
}

/**
 * 打开编辑对话框
 */
async function handleEdit(link: IQuickLink) {
	if (!link.id) return;
	resetForm();

	isEditMode.value = true;
	currentEditId.value = link.id;
	isLoadingEdit.value = true;
	dialogVisible.value = true;

	try {
		// 尝试获取完整详情
		const response = await getQuickLink(link.id);
		const linkData = response.success && response.data ? response.data : link;

		// 直接复制对象数据
		formData.value = {
			...linkData,
			linkName: linkData.linkName || '',
			description: (linkData as any).description,
			targetUrl: linkData.targetUrl || '',
			displayIcon: (linkData as any).displayIcon || (linkData as any).icon,
			redirectType: linkData.redirectType ?? RedirectType.Direct,
			urlParameters: normalizeUrlParameters(linkData.urlParameters) as any,
			isActive: !!linkData?.isActive,
		};
	} catch (error) {
		console.error('Failed to load quick link details:', error);
		// 使用列表数据作为后备
		const fallbackData = link;
		formData.value = {
			...fallbackData,
			linkName: fallbackData.linkName || '',
			description: (fallbackData as any).description,
			targetUrl: fallbackData.targetUrl || '',
			displayIcon: (fallbackData as any).displayIcon || (fallbackData as any).icon,
			redirectType: fallbackData.redirectType ?? RedirectType.Direct,
			urlParameters: normalizeUrlParameters(fallbackData.urlParameters) as any,
			isActive: !!fallbackData?.isActive,
		};
	} finally {
		isLoadingEdit.value = false;
	}
}

/**
 * 获取 Value Detail 的选项列表
 */
function getValueDetailOptions(valueSource: ValueSource) {
	switch (valueSource) {
		case ValueSource.PageParameter:
			return pageParameterDetailOptions;
		case ValueSource.LoginUserInfo:
			return loginUserInfoDetailOptions;
		case ValueSource.SystemVariable:
			return systemVariableDetailOptions;
		case ValueSource.FixedValue:
		default:
			return null; // FixedValue 使用输入框，不需要选项
	}
}

/**
 * 判断 Value Detail 是否为下拉选择
 */
function isValueDetailSelect(valueSource: ValueSource): boolean {
	return (
		valueSource === ValueSource.PageParameter ||
		valueSource === ValueSource.LoginUserInfo ||
		valueSource === ValueSource.SystemVariable
	);
}

/**
 * 获取 Value Detail 的 placeholder
 */
function getValueDetailPlaceholder(valueSource: ValueSource): string {
	const placeholders: Record<ValueSource, string> = {
		[ValueSource.PageParameter]: 'Select page parameter',
		[ValueSource.LoginUserInfo]: 'Select user info field',
		[ValueSource.FixedValue]: 'Enter fixed value',
		[ValueSource.SystemVariable]: 'Select system variable',
	};
	return placeholders[valueSource] || 'Enter value detail';
}

/**
 * 处理 Value Source 变化
 */
function handleValueSourceChange(index: number) {
	if (formData.value.urlParameters && index >= 0 && index < formData.value.urlParameters.length) {
		// 先清除该字段的验证错误
		const propPath = `urlParameters.${index}.valueDetail`;
		formRef.value?.clearValidate([propPath]);

		// 当 valueSource 改变时，清空 valueDetail
		nextTick(() => {
			formData.value.urlParameters[index].valueDetail = '';
			// 再次清除验证，确保验证错误被清除
			setTimeout(() => {
				formRef.value?.clearValidate([propPath]);
			}, 0);
		});
	}
}

/**
 * 添加参数
 */
function handleAddParameter() {
	if (!formData.value.urlParameters) {
		formData.value.urlParameters = [];
	}
	formData.value.urlParameters.push({
		name: '',
		valueSource: ValueSource.FixedValue,
		valueDetail: '',
	});
}

/**
 * 移除参数
 */
function handleRemoveParameter(index: number) {
	if (formData.value.urlParameters && index >= 0 && index < formData.value.urlParameters.length) {
		formData.value.urlParameters.splice(index, 1);
	}
}

/**
 * 取消对话框
 */
function handleDialogCancel() {
	dialogVisible.value = false;
	resetForm();
	isLoadingEdit.value = false;
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
			// 准备保存数据，将枚举值转换为数字
			const saveData: any = {
				...formData.value,
				urlParameters: formData.value.urlParameters?.map((param) => ({
					name: param.name,
					valueSource:
						typeof param.valueSource === 'number'
							? param.valueSource
							: (param.valueSource as unknown as number),
					valueDetail: param.valueDetail,
				})),
			};

			let response;
			if (isEditMode.value && currentEditId.value) {
				response = await updateQuickLink(currentEditId.value, saveData);
			} else {
				response = await createQuickLink({
					...saveData,
					integrationId: props.integrationId,
				});
			}

			if (response.code == '200') {
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
		} finally {
			isSaving.value = false;
		}
	});
}

/**
 * 处理链接点击
 */
async function handleLinkClick(row: IQuickLink) {
	const url = row.targetUrl;
	if (!url) {
		ElMessage.warning('Target URL is empty');
		return;
	}

	// 如果是 PopupConfirmation 类型，需要弹窗确认
	if (row.redirectType === RedirectType.PopupConfirmation) {
		try {
			await ElMessageBox.confirm(
				`Are you sure you want to open this link?\n\n${url}`,
				'Confirm Redirect',
				{
					confirmButtonText: 'Open',
					cancelButtonText: 'Cancel',
					type: 'info',
				}
			);
			// 用户确认后，在新标签页打开
			window.open(url, '_blank');
		} catch {
			// 用户取消，不执行任何操作
		}
	} else {
		// Direct 类型，直接在新标签页打开
		window.open(url, '_blank');
	}
}

/**
 * 删除快速链接
 */
async function handleDelete(link: IQuickLink) {
	if (!link.id) return;

	try {
		await ElMessageBox.confirm(
			`Are you sure you want to delete the quick link "${link.linkName}"?`,
			'Confirm Deletion',
			{
				confirmButtonText: 'Delete',
				cancelButtonText: 'Cancel',
				type: 'warning',
			}
		);

		const response = await deleteQuickLink(link.id);
		if (response.code == '200') {
			ElMessage.success('Quick link deleted successfully');
			loadQuickLinks();
		} else {
			ElMessage.error(response.msg || 'Failed to delete quick link');
		}
	} catch (error) {
		if (error !== 'cancel') {
			console.error('Failed to delete quick link:', error);
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

.parameter-card {
	padding: 16px;
	background: var(--el-fill-color-lighter);
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 8px;
}

.parameter-card-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	margin-bottom: 16px;
}

.parameter-card-title {
	font-size: 14px;
	font-weight: 500;
	color: var(--el-text-color-primary);
}

.parameter-card-content {
	display: flex;
	flex-direction: column;
	gap: 16px;
}

.parameter-form-item {
	margin-bottom: 0;
}

.parameter-form-item :deep(.el-form-item__label) {
	margin-bottom: 8px;
	padding: 0;
	line-height: 1.5;
}

.parameter-scrollbar {
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 4px;
	padding: 12px;
	background: var(--el-fill-color-blank);
}

.parameter-scrollbar :deep(.el-scrollbar__wrap) {
	padding-right: 8px;
}

.dialog-content-wrapper {
	min-height: 200px;
	position: relative;
}
</style>
