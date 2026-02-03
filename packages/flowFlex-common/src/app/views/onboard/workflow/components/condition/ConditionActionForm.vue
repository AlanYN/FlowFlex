<template>
	<div class="condition-action-form">
		<!-- Loading 骨架屏 -->
		<template v-if="isLoading">
			<div class="actions-list">
				<div v-for="(_, index) in modelValue" :key="index" class="action-item">
					<div class="action-header">
						<el-skeleton :rows="0" animated style="width: 80px">
							<template #template>
								<el-skeleton-item variant="text" style="width: 80px" />
							</template>
						</el-skeleton>
					</div>
					<el-skeleton :rows="2" animated />
				</div>
			</div>
		</template>

		<!-- 实际内容 -->
		<template v-else>
			<!-- 动作列表 -->
			<div class="actions-list">
				<div v-for="(action, index) in modelValue" :key="index" class="action-item">
					<el-form
						:ref="(el: any) => setFormRef(el, index)"
						:model="action"
						:rules="getActionValidationRules(action)"
						label-position="top"
						:validate-on-rule-change="false"
						@submit.prevent
					>
						<div class="action-header">
							<span class="action-number">Action {{ index + 1 }}</span>
							<el-button
								type="danger"
								link
								:icon="Delete"
								@click="handleRemoveAction(index)"
							/>
						</div>

						<!-- Action Type -->
						<el-form-item label="Action Type" class="action-field" prop="type">
							<el-select
								v-model="action.type"
								placeholder="Select action type"
								@change="() => handleActionTypeReset(action)"
							>
								<el-option
									v-for="type in getAvailableActionTypesForAction(action, index)"
									:key="type.value"
									:label="type.label"
									:value="type.value"
								>
									<div>
										<span>{{ `${type.label}   ` }}</span>
										<span class="action-type-desc">
											{{ `( ${type.description} )` }}
										</span>
									</div>
								</el-option>
							</el-select>
						</el-form-item>

						<!-- GoToStage: Target Stage -->
						<el-form-item
							v-if="action.type === 'GoToStage'"
							label="Target Stage"
							prop="targetStageId"
						>
							<el-select
								v-model="action.targetStageId"
								placeholder="Select target stage"
							>
								<el-option
									v-for="stage in availableTargetStages"
									:key="stage.id"
									:label="stage.name"
									:value="stage.id"
								/>
							</el-select>
							<!-- 循环警告 -->
							<div v-if="isLoopWarning(action.targetStageId)" class="loop-warning">
								<el-icon><Warning /></el-icon>
								<span>Warning: This may cause a loop in the workflow</span>
							</div>
						</el-form-item>

						<!-- TriggerAction: Action Definition -->
						<el-form-item
							v-if="action.type === 'TriggerAction'"
							label="Action"
							class="action-field"
							prop="actionDefinitionId"
						>
							<el-select
								v-model="action.actionDefinitionId"
								placeholder="Select action"
							>
								<el-option-group
									v-for="(actions, groupName) in groupedActions"
									:key="groupName"
									:label="groupName"
								>
									<el-option
										v-for="act in actions"
										:key="act.id"
										:label="act.name"
										:value="act.id"
									/>
								</el-option-group>
							</el-select>
						</el-form-item>

						<!-- SendNotification: Recipient -->
						<template v-if="action.type === 'SendNotification'">
							<!-- User/Team 选择器 -->
							<el-form-item
								label="Recipients"
								class="action-field"
								prop="parameters.recipients"
							>
								<div class="text-gray-500 mb-1">Select User</div>
								<FlowflexUserSelector
									v-model="getActionParams(action).users"
									selection-type="user"
									placeholder="Select user"
									clearable
									:args="{
										stageId: currentStageId,
									}"
								/>
								<div class="text-gray-500 mb-1">Select Team</div>
								<FlowflexUserSelector
									v-model="getActionParams(action).teams"
									selection-type="team"
									placeholder="Select team"
									clearable
									:args="{
										stageId: currentStageId,
									}"
								/>
							</el-form-item>

							<el-form-item
								label="Email Content"
								class="action-field"
								prop="parameters.emailContent"
							>
								<div class="text-gray-500 mb-1">subject</div>
								<el-input
									v-model="getActionParams(action).subject"
									placeholder="Enter email subject..."
								/>
								<div class="text-gray-500 mb-1">Email Body</div>
								<el-input
									v-model="getActionParams(action).emailBody"
									type="textarea"
									placeholder="Enter email content..."
									:maxlength="textraTwoHundredLength"
									:autosize="inputTextraAutosize"
									show-word-limit
								/>
							</el-form-item>
						</template>

						<!-- UpdateField: Field and Value -->
						<template v-if="action.type === 'UpdateField'">
							<el-form-item
								label="Target Field to Update"
								class="action-field"
								prop="parameters.fieldPath"
							>
								<el-select
									v-model="getActionParams(action).fieldPath"
									placeholder="Select field"
									@change="(val: string) => handleFieldSelect(action, val)"
								>
									<el-option-group
										v-for="group in getAvailableFieldOptions(action)"
										:key="group.stageId"
										:label="group.stageName"
									>
										<el-option
											v-for="field in group.fields"
											:key="field.key"
											:value="field.key"
											:label="field.name"
										/>
									</el-option-group>
								</el-select>
							</el-form-item>
							<el-form-item
								v-if="getActionParams(action).fieldPath"
								:label="`Value for &quot;${
									getActionParams(action).fieldName || ''
								}&quot;`"
								class="action-field"
								prop="parameters.fieldValue"
							>
								<DynamicValueInput
									v-model="getActionParams(action).fieldValue"
									:input-type="getFieldInputType(action)"
									:options="getFieldValueOptions(action)"
									:constraints="getFieldConstraints(action)"
								/>
							</el-form-item>
						</template>

						<!-- AssignUser: Assignee -->
						<template v-if="action.type === 'AssignUser'">
							<el-form-item
								label="Assignee Type"
								class="action-field"
								prop="parameters.assigneeType"
							>
								<el-select
									v-model="getActionParams(action).assigneeType"
									placeholder="Select assignee type"
									@change="() => handleAssigneeTypeChange(action)"
								>
									<el-option value="user" label="User" />
									<el-option value="team" label="Team" />
								</el-select>
							</el-form-item>
							<!-- User 选择器 (多选) -->
							<el-form-item
								v-if="getActionParams(action).assigneeType === 'user'"
								label="Select Users"
								class="action-field"
								prop="parameters.assigneeIds"
							>
								<FlowflexUserSelector
									v-model="getActionParams(action).assigneeIds"
									selection-type="user"
									placeholder="Select users"
									@change="(val) => handleAssigneeChange(action, val)"
									:args="{
										stageId: currentStageId,
									}"
								/>
							</el-form-item>
							<!-- Team 选择器 (多选) -->
							<el-form-item
								v-if="getActionParams(action).assigneeType === 'team'"
								label="Select Teams"
								class="action-field"
								prop="parameters.assigneeIds"
							>
								<FlowflexUserSelector
									v-model="getActionParams(action).assigneeIds"
									selection-type="team"
									placeholder="Select teams"
									@change="(val) => handleAssigneeChange(action, val)"
									:args="{
										stageId: currentStageId,
									}"
								/>
							</el-form-item>
						</template>
					</el-form>
				</div>
			</div>
		</template>

		<!-- 添加动作按钮 -->
		<el-button type="primary" link @click="handleAddAction">
			<el-icon class="mr-1"><Plus /></el-icon>
			Add Action
		</el-button>
	</div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, computed, watch } from 'vue';
import { Plus, Delete, Warning } from '@element-plus/icons-vue';
import type { FormInstance, FormRules } from 'element-plus';
import type { ActionFormItem, DynamicFieldConstraints, ConditionActionType } from '#/condition';
import type { Stage } from '#/onboard';
import type { DynamicList, DynamicDropdownItem } from '#/dynamic';
import FlowflexUserSelector from '@/components/form/flowflexUser/index.vue';
import DynamicValueInput from './DynamicValueInput.vue';
import { conditionAction } from '@/apis/ow';
import { batchIdsDynamicFields } from '@/apis/global/dyanmicField';
import { ToolsType, propertyTypeEnum } from '@/enums/appEnum';
import { textraTwoHundredLength, inputTextraAutosize } from '@/settings/projectSetting';
import {
	getAvailableActionTypes,
	getDefaultActionType,
} from '../../../../../utils/actionTypeUtils';

// 值输入类型
type ValueInputType = 'text' | 'number' | 'select' | 'date' | 'people' | 'phone';

// Props
const props = defineProps<{
	modelValue: ActionFormItem[];
	stages: Stage[];
	currentStageIndex: number;
	currentStageId: string;
}>();

// Emits
const emit = defineEmits<{
	(e: 'update:modelValue', value: ActionFormItem[]): void;
}>();

// 表单引用映射（按动作索引）
const formRefs = reactive<Record<number, FormInstance | null>>({});

// 设置表单引用
const setFormRef = (el: FormInstance | null, index: number) => {
	formRefs[index] = el;
};

// 获取动作的验证规则
const getActionValidationRules = (action: ActionFormItem): FormRules => {
	const rules: FormRules = {
		type: [{ required: true, message: 'Please select an action type', trigger: [] }],
	};

	// 根据动作类型添加特定验证规则
	switch (action.type) {
		case 'GoToStage':
			rules.targetStageId = [
				{ required: true, message: 'Please select a target stage', trigger: [] },
			];
			break;

		case 'TriggerAction':
			rules.actionDefinitionId = [
				{
					required: true,
					message: 'Please select an action to trigger',
					trigger: [],
				},
			];
			break;

		case 'SendNotification':
			rules['parameters.recipients'] = [
				{
					required: true,
					message: 'Please select at least one user or team',
					trigger: [],
					validator: (_rule: any, _value: any, callback: any) => {
						const params = action.parameters || {};
						const hasUsers = params.users && params.users.length > 0;
						const hasTeams = params.teams && params.teams.length > 0;
						if (!hasUsers && !hasTeams) {
							callback(new Error('Please select at least one user or team'));
						} else {
							callback();
						}
					},
				},
			];
			rules['parameters.emailContent'] = [
				{
					required: true,
					message: 'Please enter subject and email body',
					trigger: [],
					validator: (_rule: any, _value: any, callback: any) => {
						const params = action.parameters || {};
						const hasSubject = params.subject && params.subject.trim();
						const hasEmailBody = params.emailBody && params.emailBody.trim();
						if (!hasSubject && !hasEmailBody) {
							callback(new Error('Please enter subject and email body'));
						} else if (!hasSubject) {
							callback(new Error('Please enter subject'));
						} else if (!hasEmailBody) {
							callback(new Error('Please enter email body'));
						} else {
							callback();
						}
					},
				},
			];
			break;

		case 'UpdateField':
			rules['parameters.fieldPath'] = [
				{ required: true, message: 'Please select a field to update', trigger: [] },
			];
			if (action.parameters?.fieldPath) {
				rules['parameters.fieldValue'] = [
					{
						required: true,
						message: 'Please enter a value for the field',
						trigger: [],
						validator: (_rule: any, value: any, callback: any) => {
							if (value === '' || value === undefined || value === null) {
								callback(new Error('Please enter a value for the field'));
							} else {
								callback();
							}
						},
					},
				];
			}
			break;

		case 'AssignUser':
			rules['parameters.assigneeType'] = [
				{ required: true, message: 'Please select an assignee type', trigger: [] },
			];
			if (action.parameters?.assigneeType) {
				rules['parameters.assigneeIds'] = [
					{
						required: true,
						message: 'Please select at least one assignee',
						trigger: [],
						validator: (_rule: any, value: any, callback: any) => {
							if (!value || (Array.isArray(value) && value.length === 0)) {
								callback(new Error('Please select at least one assignee'));
							} else {
								callback();
							}
						},
					},
				];
			}
			break;
	}

	return rules;
};

// ToolsType 标签映射
const TOOLS_TYPE_LABELS: Record<number, string> = {
	[ToolsType.UseTool]: 'Public Tools',
	[ToolsType.MyTool]: 'My Tools',
	[ToolsType.SystemTools]: 'System Tools',
};

// 可用的 Action 定义
interface ActionOption {
	id: string;
	name: string;
	toolsType: ToolsType;
}
const availableActions = ref<ActionOption[]>([]);

// Loading 状态
const loadingActions = ref(false);
const loadingFields = ref(false);

// 整体 loading 状态
const isLoading = computed(() => loadingActions.value || loadingFields.value);

// 按 ToolsType 分组的 Actions
const groupedActions = computed(() => {
	const groups: Record<string, ActionOption[]> = {};

	availableActions.value.forEach((action) => {
		const typeName = TOOLS_TYPE_LABELS[action.toolsType] || 'Other';
		if (!groups[typeName]) {
			groups[typeName] = [];
		}
		groups[typeName].push(action);
	});

	return groups;
});

// 获取当前 action 可用的 action types（根据当前 action 的索引）
const getAvailableActionTypesForAction = (currentAction: ActionFormItem, currentIndex: number) => {
	return getAvailableActionTypes(
		props.currentStageIndex,
		props.stages.length,
		props.modelValue,
		currentIndex
	);
};

// 获取默认的 action type
const getDefaultType = (): ConditionActionType => {
	return getDefaultActionType(props.currentStageIndex, props.stages.length, props.modelValue);
};

// GoToStage 可选的目标 stages（只能选择当前 stage 之后的）
const availableTargetStages = computed(() => {
	return props.stages.slice(props.currentStageIndex + 1);
});

// 已使用的字段路径（用于 UpdateField 去重）
const usedFieldPaths = computed(() => {
	return props.modelValue
		.filter((action) => action.type === 'UpdateField' && action.parameters?.fieldPath)
		.map((action) => action.parameters!.fieldPath);
});

// 获取当前 action 可用的字段选项（排除已被其他 UpdateField 使用的字段）
const getAvailableFieldOptions = (currentAction: ActionFormItem): FieldOptionGroup[] => {
	const currentFieldPath = currentAction.parameters?.fieldPath;

	return groupedFieldOptions.value
		.map((group) => ({
			...group,
			fields: group.fields.filter((field) => {
				// 当前 action 已选的字段要保留
				if (field.key === currentFieldPath) return true;
				// 排除已被其他 action 使用的字段
				return !usedFieldPaths.value.includes(field.key);
			}),
		}))
		.filter((group) => group.fields.length > 0);
};

// 检查是否会产生循环
const isLoopWarning = (targetStageId?: string) => {
	if (!targetStageId) return false;
	const targetIndex = props.stages.findIndex((s) => s.id === targetStageId);
	return targetIndex !== -1 && targetIndex <= props.currentStageIndex;
};

// 获取 action 的 parameters，确保存在
const getActionParams = (action: ActionFormItem) => {
	if (!action.parameters) {
		action.parameters = {};
	}
	return action.parameters;
};

// 处理动作类型变化时重置相关字段
const handleActionTypeReset = (action: ActionFormItem) => {
	action.targetStageId = undefined;
	action.actionDefinitionId = undefined;
	action.parameters = {};
};

// 处理 AssignUser 的 assigneeType 变化
const handleAssigneeTypeChange = (action: ActionFormItem) => {
	const params = getActionParams(action);
	params.assigneeIds = [];
};

// 处理 Assignee 选择变化
const handleAssigneeChange = (action: ActionFormItem, value: string | string[] | undefined) => {
	const params = getActionParams(action);
	params.assigneeIds = Array.isArray(value) ? value : value ? [value] : [];
};

// 添加动作
const handleAddAction = () => {
	const newAction: ActionFormItem = {
		type: getDefaultType(),
		order: props.modelValue.length,
		parameters: {},
	};
	emit('update:modelValue', [...props.modelValue, newAction]);
};

// 删除动作
const handleRemoveAction = (index: number) => {
	const newActions = props.modelValue.filter((_, i) => i !== index);
	emit('update:modelValue', newActions);
};

// 加载 Actions
const loadAvailableActions = async () => {
	loadingActions.value = true;
	try {
		const res = await conditionAction();
		if (res.code === '200' && res.data) {
			availableActions.value = res.data.map((item: any) => {
				// 根据 isSystemTools 和 isTools 判断 ToolsType
				let toolsType: ToolsType;
				if (item.isSystemTools) {
					toolsType = ToolsType.SystemTools;
				} else if (item.isTools) {
					toolsType = ToolsType.UseTool;
				} else {
					toolsType = ToolsType.MyTool;
				}

				return {
					id: item.id,
					name: item.name || item.actionCode || 'Unnamed Action',
					toolsType,
				};
			});
		}
	} catch (error) {
		console.error('Failed to load available actions:', error);
		availableActions.value = [];
	} finally {
		loadingActions.value = false;
	}
};

// 静态字段映射缓存
const staticFieldsMap = ref<Map<string, DynamicList>>(new Map());

// 字段选项接口
interface FieldOption {
	key: string; // stageId_fieldId 组合键
	id: string;
	name: string;
	stageId: string;
	stageName: string;
	dataType: number;
	dropdownItems?: DynamicDropdownItem[];
	additionalInfo?: DynamicList['additionalInfo'];
	format?: DynamicList['format'];
	fieldValidate?: DynamicList['fieldValidate'];
}

// 字段分组接口
interface FieldOptionGroup {
	stageName: string;
	stageId: string;
	fields: FieldOption[];
}

// Value 选项接口
interface ValueOption {
	label: string;
	value: string;
}

// 按 Stage 分组的字段选项（只包含当前 stage 之后的 stage）
const groupedFieldOptions = computed<FieldOptionGroup[]>(() => {
	const groups: FieldOptionGroup[] = [];

	// 只遍历当前 stage 之后的 stages（不包含当前 stage）
	props.stages.slice(props.currentStageIndex + 1).forEach((stage) => {
		const fieldsComponent = stage.components?.find((c) => c.key === 'fields');
		if (!fieldsComponent?.staticFields?.length) return;

		const fields: FieldOption[] = [];
		fieldsComponent.staticFields.forEach((field) => {
			const fieldInfo = staticFieldsMap.value.get(field.id);
			if (fieldInfo) {
				fields.push({
					key: `${stage.id}_${field.id}`,
					id: field.id,
					name: fieldInfo.fieldName || field.id,
					stageId: stage.id,
					stageName: stage.name,
					dataType: fieldInfo.dataType,
					dropdownItems: fieldInfo.dropdownItems,
					additionalInfo: fieldInfo.additionalInfo,
					format: fieldInfo.format,
					fieldValidate: fieldInfo.fieldValidate,
				});
			}
		});

		if (fields.length > 0) {
			groups.push({
				stageName: stage.name,
				stageId: stage.id,
				fields,
			});
		}
	});

	return groups;
});

// 加载所有 Stage 的静态字段映射
const loadStaticFieldsMapping = async () => {
	// 收集所有 stage 的字段 ID
	const allFieldIds: string[] = [];
	props.stages.forEach((stage) => {
		const fieldsComponent = stage.components?.find((c) => c.key === 'fields');
		if (fieldsComponent?.staticFields?.length) {
			fieldsComponent.staticFields.forEach((f) => {
				if (!allFieldIds.includes(f.id)) {
					allFieldIds.push(f.id);
				}
			});
		}
	});

	if (allFieldIds.length === 0) return;

	// 过滤掉已经缓存的ID
	const uncachedIds = allFieldIds.filter((id) => !staticFieldsMap.value.has(id));

	if (uncachedIds.length === 0) return;

	loadingFields.value = true;
	try {
		const res: any = await batchIdsDynamicFields({ ids: uncachedIds });
		if (res.code === '200' && res.data) {
			res.data.forEach((field: DynamicList) => {
				staticFieldsMap.value.set(field.id, field);
			});
		}
	} catch (error) {
		console.error('Failed to load static fields mapping:', error);
	} finally {
		loadingFields.value = false;
	}
};

// 处理字段选择变化
const handleFieldSelect = (action: ActionFormItem, fieldKey: string) => {
	const params = getActionParams(action);

	// 从所有分组中查找选中的字段
	let selectedField: FieldOption | undefined;
	for (const group of groupedFieldOptions.value) {
		selectedField = group.fields.find((f) => f.key === fieldKey);
		if (selectedField) break;
	}

	if (selectedField) {
		params.stageId = selectedField.stageId;
		params.fieldId = selectedField.id;
		params.fieldName = selectedField.name;
		params.fieldPath = fieldKey;
		// 清空之前的值
		params.fieldValue = '';
	}
};

// 获取字段的值选项
const getFieldValueOptions = (action: ActionFormItem): ValueOption[] => {
	const params = getActionParams(action);
	const fieldKey = params.fieldPath;
	if (!fieldKey) return [];

	// 从所有分组中查找字段
	let fieldInfo: FieldOption | undefined;
	for (const group of groupedFieldOptions.value) {
		fieldInfo = group.fields.find((f) => f.key === fieldKey);
		if (fieldInfo) break;
	}

	if (!fieldInfo) return [];

	const options: ValueOption[] = [];

	if (fieldInfo.dataType === propertyTypeEnum.DropdownSelect) {
		if (fieldInfo.dropdownItems?.length) {
			fieldInfo.dropdownItems.forEach((item) => {
				options.push({
					label: item.value,
					value: item.value,
				});
			});
		}
	} else if (fieldInfo.dataType === propertyTypeEnum.Switch) {
		const trueLabel = fieldInfo.additionalInfo?.trueLabel || 'Yes';
		const falseLabel = fieldInfo.additionalInfo?.falseLabel || 'No';
		options.push({ label: trueLabel, value: 'true' }, { label: falseLabel, value: 'false' });
	}

	return options;
};

// 获取字段的输入类型
const getFieldInputType = (action: ActionFormItem): ValueInputType => {
	const params = getActionParams(action);
	const fieldKey = params.fieldPath;
	if (!fieldKey) return 'text';

	// 从所有分组中查找字段
	let fieldInfo: FieldOption | undefined;
	for (const group of groupedFieldOptions.value) {
		fieldInfo = group.fields.find((f) => f.key === fieldKey);
		if (fieldInfo) break;
	}

	if (!fieldInfo) return 'text';

	// 根据 dataType 返回对应的输入类型
	const inputTypeMap: Record<number, ValueInputType> = {
		[propertyTypeEnum.SingleLineText]: 'text',
		[propertyTypeEnum.MultilineText]: 'text',
		[propertyTypeEnum.Phone]: 'phone',
		[propertyTypeEnum.Email]: 'text',
		[propertyTypeEnum.Number]: 'number',
		[propertyTypeEnum.DropdownSelect]: 'select',
		[propertyTypeEnum.Switch]: 'select',
		[propertyTypeEnum.DatePicker]: 'date',
		[propertyTypeEnum.File]: 'text',
		[propertyTypeEnum.Pepole]: 'people',
	};

	return inputTypeMap[fieldInfo.dataType] || 'text';
};

// 获取字段的约束配置
const getFieldConstraints = (action: ActionFormItem): DynamicFieldConstraints => {
	const params = getActionParams(action);
	const fieldKey = params.fieldPath;
	if (!fieldKey) return {};

	// 从所有分组中查找字段
	let fieldInfo: FieldOption | undefined;
	for (const group of groupedFieldOptions.value) {
		fieldInfo = group.fields.find((f) => f.key === fieldKey);
		if (fieldInfo) break;
	}

	if (!fieldInfo) return {};

	const constraints: DynamicFieldConstraints = {};

	// Number 类型约束
	if (fieldInfo.dataType === propertyTypeEnum.Number) {
		constraints.isFloat = fieldInfo.additionalInfo?.isFloat ?? true;
		constraints.allowNegative = fieldInfo.additionalInfo?.allowNegative ?? false;
		constraints.isFinancial = fieldInfo.additionalInfo?.isFinancial ?? false;
		constraints.decimalPlaces = Number(fieldInfo.format?.decimalPlaces) || 2;
	}

	// DatePicker 类型约束
	if (fieldInfo.dataType === propertyTypeEnum.DatePicker) {
		constraints.dateFormat = fieldInfo.format?.dateFormat || 'YYYY-MM-DD';
		// 根据格式判断是否包含时间
		const format = fieldInfo.format?.dateFormat || '';
		constraints.dateType = format.includes('HH:mm') ? 'datetime' : 'date';
	}

	// Text 类型约束
	if (
		fieldInfo.dataType === propertyTypeEnum.SingleLineText ||
		fieldInfo.dataType === propertyTypeEnum.MultilineText
	) {
		constraints.maxLength = fieldInfo.fieldValidate?.maxLength;
	}

	if (fieldInfo.dataType === propertyTypeEnum.DropdownSelect) {
		constraints.allowMultiple = fieldInfo.additionalInfo?.allowMultiple || false;
	}

	return constraints;
};

// 加载可用的 Action 定义
onMounted(async () => {
	await loadAvailableActions();
	await loadStaticFieldsMapping();

	// 清除所有表单的验证状态
	setTimeout(() => {
		Object.values(formRefs).forEach((formRef) => {
			formRef?.clearValidate();
		});
	}, 0);
});

// 监听 stage 变化重新构建字段选项
watch(
	() => props.currentStageIndex,
	() => {
		loadStaticFieldsMapping();
	}
);

// 验证动作完整性
const validate = async (): Promise<{ valid: boolean; message: string }> => {
	if (props.modelValue.length === 0) {
		return { valid: false, message: 'Please add at least one action' };
	}

	// 验证所有表单
	const validationPromises: Promise<boolean>[] = [];
	for (let i = 0; i < props.modelValue.length; i++) {
		const formRef = formRefs[i];
		if (formRef) {
			validationPromises.push(
				formRef
					.validate()
					.then(() => true)
					.catch(() => false)
			);
		}
	}

	const results = await Promise.all(validationPromises);
	const allValid = results.every((result) => result);

	if (!allValid) {
		return { valid: false, message: '' }; // 错误信息已由表单显示
	}

	return { valid: true, message: '' };
};

// 暴露方法给父组件
defineExpose({
	validate,
	getDefaultActionType: getDefaultType,
});
</script>

<style lang="scss" scoped>
.condition-action-form {
	@apply flex flex-col gap-4;
}

.actions-list {
	@apply flex flex-col gap-4;
}

.action-item {
	@apply p-4 rounded-lg border;
	background-color: var(--el-fill-color-lighter);
}

.action-header {
	@apply flex items-center justify-between mb-3;
}

.action-number {
	@apply text-sm font-medium text-primary;
}

.action-type-desc {
	@apply text-xs;
	color: var(--el-text-color-secondary);
}

.loop-warning {
	@apply flex items-center gap-1 mt-2 text-xs;
	color: var(--el-color-warning);
}
</style>
