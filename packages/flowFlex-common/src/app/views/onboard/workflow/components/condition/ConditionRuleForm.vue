<template>
	<div class="condition-rule-form">
		<!-- 逻辑运算符选择 -->
		<div class="logic-selector" v-if="modelValue.length > 1">
			<span class="logic-label">Match</span>
			<el-radio-group :model-value="logic" @change="onLogicChange" size="small">
				<el-radio-button value="AND">All (AND)</el-radio-button>
				<el-radio-button value="OR">Any (OR)</el-radio-button>
			</el-radio-group>
			<span class="logic-hint">of the following rules</span>
		</div>

		<!-- 规则列表 -->
		<div class="rules-list">
			<div v-for="(rule, index) in modelValue" :key="rule._id" class="rule-item">
				<div class="rule-header">
					<span class="rule-number">Rule {{ index + 1 }}</span>
					<el-button
						type="danger"
						link
						size="small"
						:disabled="modelValue.length <= 1"
						@click="handleRemoveRule(index)"
					>
						<el-icon><Delete /></el-icon>
					</el-button>
				</div>

				<div class="rule-fields">
					<!-- Source Stage -->
					<el-form-item label="Source Stage">
						<el-select
							:model-value="rule.sourceStageId"
							placeholder="Select stage"
							@update:model-value="(val) => updateRule(index, 'sourceStageId', val)"
						>
							<el-option
								v-for="stage in stages"
								:key="stage.id"
								:label="stage.name"
								:value="stage.id"
							/>
						</el-select>
					</el-form-item>

					<!-- Component Type -->
					<el-form-item label="Component Type">
						<el-select
							:model-value="rule.componentType"
							placeholder="Select type"
							@update:model-value="(val) => handleComponentTypeChange(index, val)"
						>
							<el-option
								v-for="type in componentTypes"
								:key="type.value"
								:label="type.label"
								:value="type.value"
							/>
						</el-select>
					</el-form-item>

					<!-- Component -->
					<el-form-item label="Component">
						<el-select
							:model-value="rule.componentId"
							placeholder="Select component"
							:loading="loadingComponents[rule._id]"
							@update:model-value="(val) => handleComponentChange(index, val)"
							@focus="loadComponents(rule)"
						>
							<el-option
								v-for="comp in rule._componentOptions || []"
								:key="comp.id"
								:label="comp.name"
								:value="comp.id"
							/>
						</el-select>
					</el-form-item>

					<!-- Field -->
					<el-form-item label="Field">
						<el-select
							:model-value="rule.fieldPath"
							placeholder="Select field"
							:loading="loadingFields[rule._id]"
							@update:model-value="(val) => updateRule(index, 'fieldPath', val)"
							@focus="loadFields(rule)"
						>
							<el-option
								v-for="field in rule._fieldOptions || []"
								:key="field.fieldPath"
								:label="field.label"
								:value="field.fieldPath"
							/>
						</el-select>
					</el-form-item>

					<!-- Operator -->
					<el-form-item label="Operator">
						<el-select
							:model-value="rule.operator"
							placeholder="Select operator"
							@update:model-value="(val) => updateRule(index, 'operator', val)"
						>
							<el-option
								v-for="op in operators"
								:key="op.value"
								:label="op.label"
								:value="op.value"
							/>
						</el-select>
					</el-form-item>

					<!-- Value -->
					<el-form-item v-if="!isNoValueOperator(rule.operator)" label="Value">
						<el-input
							:model-value="String(rule.value)"
							placeholder="Enter value"
							@update:model-value="(val) => updateRule(index, 'value', val)"
						/>
					</el-form-item>
				</div>
			</div>
		</div>

		<!-- 添加规则按钮 -->
		<el-button type="primary" link @click="handleAddRule" class="add-rule-btn">
			<el-icon class="mr-1"><Plus /></el-icon>
			Add Rule
		</el-button>
	</div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { Plus, Delete } from '@element-plus/icons-vue';
import type { RuleFormItem, ComponentType, ConditionOperator } from '#/condition';
import type { Stage } from '#/onboard';
import { getStageComponents, getComponentFields } from '@/apis/ow';

// Props
const props = defineProps<{
	modelValue: RuleFormItem[];
	stages: Stage[];
	currentStageIndex: number;
	logic: 'AND' | 'OR';
}>();

// Emits
const emit = defineEmits<{
	(e: 'update:modelValue', value: RuleFormItem[]): void;
	(e: 'update:logic', value: 'AND' | 'OR'): void;
}>();

// 加载状态
const loadingComponents = reactive<Record<string, boolean>>({});
const loadingFields = reactive<Record<string, boolean>>({});

// 组件类型选项
const componentTypes = [
	{ value: 'checklist', label: 'Checklist' },
	{ value: 'questionnaires', label: 'Questionnaire' },
	{ value: 'fields', label: 'Fields' },
	{ value: 'files', label: 'Files' },
];

// 操作符选项
const operators = [
	{ value: 'Equals', label: 'Equals' },
	{ value: 'NotEquals', label: 'Not Equals' },
	{ value: 'GreaterThan', label: 'Greater Than' },
	{ value: 'LessThan', label: 'Less Than' },
	{ value: 'GreaterThanOrEqual', label: 'Greater Than or Equal' },
	{ value: 'LessThanOrEqual', label: 'Less Than or Equal' },
	{ value: 'Contains', label: 'Contains' },
	{ value: 'DoesNotContain', label: 'Does Not Contain' },
	{ value: 'StartsWith', label: 'Starts With' },
	{ value: 'EndsWith', label: 'Ends With' },
	{ value: 'IsEmpty', label: 'Is Empty' },
	{ value: 'IsNotEmpty', label: 'Is Not Empty' },
	{ value: 'InList', label: 'In List' },
	{ value: 'NotInList', label: 'Not In List' },
];

// 不需要值的操作符
const noValueOperators = ['IsEmpty', 'IsNotEmpty'];

const isNoValueOperator = (operator: ConditionOperator) => {
	return noValueOperators.includes(operator);
};

// 生成唯一 ID
const generateId = () => `temp_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;

// 更新规则字段
const updateRule = (index: number, field: keyof RuleFormItem, value: any) => {
	const newRules = [...props.modelValue];
	newRules[index] = { ...newRules[index], [field]: value };
	emit('update:modelValue', newRules);
};

// 处理逻辑运算符变化
const onLogicChange = (val: string | number | boolean | undefined) => {
	if (val === 'AND' || val === 'OR') {
		emit('update:logic', val);
	}
};

// 处理组件类型变化
const handleComponentTypeChange = (index: number, val: ComponentType) => {
	const newRules = [...props.modelValue];
	newRules[index] = {
		...newRules[index],
		componentType: val,
		componentId: '',
		fieldPath: '',
		_componentOptions: [],
		_fieldOptions: [],
	};
	emit('update:modelValue', newRules);
};

// 处理组件变化
const handleComponentChange = (index: number, val: string) => {
	const newRules = [...props.modelValue];
	newRules[index] = {
		...newRules[index],
		componentId: val,
		fieldPath: '',
		_fieldOptions: [],
	};
	emit('update:modelValue', newRules);
};

// 加载组件列表
const loadComponents = async (rule: RuleFormItem) => {
	if (!rule.sourceStageId || rule._componentOptions?.length) return;

	loadingComponents[rule._id] = true;
	try {
		const components = await getStageComponents(rule.sourceStageId);
		const filteredComponents = components.filter((c) => c.type === rule.componentType);

		const index = props.modelValue.findIndex((r) => r._id === rule._id);
		if (index !== -1) {
			const newRules = [...props.modelValue];
			newRules[index] = { ...newRules[index], _componentOptions: filteredComponents };
			emit('update:modelValue', newRules);
		}
	} catch (error) {
		console.error('Failed to load components:', error);
	} finally {
		loadingComponents[rule._id] = false;
	}
};

// 加载字段列表
const loadFields = async (rule: RuleFormItem) => {
	if (!rule.componentType || !rule.componentId || rule._fieldOptions?.length) return;

	loadingFields[rule._id] = true;
	try {
		const fields = await getComponentFields(rule.componentType, rule.componentId);

		const index = props.modelValue.findIndex((r) => r._id === rule._id);
		if (index !== -1) {
			const newRules = [...props.modelValue];
			newRules[index] = { ...newRules[index], _fieldOptions: fields };
			emit('update:modelValue', newRules);
		}
	} catch (error) {
		console.error('Failed to load fields:', error);
	} finally {
		loadingFields[rule._id] = false;
	}
};

// 添加规则
const handleAddRule = () => {
	const newRule: RuleFormItem = {
		_id: generateId(),
		sourceStageId: props.stages[props.currentStageIndex]?.id || '',
		componentType: 'checklist',
		componentId: '',
		fieldPath: '',
		operator: 'Equals',
		value: '',
	};
	emit('update:modelValue', [...props.modelValue, newRule]);
};

// 删除规则
const handleRemoveRule = (index: number) => {
	if (props.modelValue.length <= 1) return;
	const newRules = props.modelValue.filter((_, i) => i !== index);
	emit('update:modelValue', newRules);
};
</script>

<style lang="scss" scoped>
.condition-rule-form {
	@apply flex flex-col gap-4;
}

.logic-selector {
	@apply flex items-center gap-2 p-3 rounded-lg;
	background-color: var(--el-fill-color-lighter);
}

.logic-label {
	@apply text-sm font-medium;
	color: var(--el-text-color-regular);
}

.logic-hint {
	@apply text-sm;
	color: var(--el-text-color-secondary);
}

.rules-list {
	@apply flex flex-col gap-4;
}

.rule-item {
	@apply p-4 rounded-lg border bg-black-400;
}

.rule-header {
	@apply flex items-center justify-between mb-3;
}

.rule-number {
	@apply text-sm font-medium;
	color: var(--el-text-color-primary);
}

.rule-fields {
	@apply grid gap-3;
	grid-template-columns: repeat(2, 1fr);
}

.add-rule-btn {
	@apply self-start;
}
</style>
