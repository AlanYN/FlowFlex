/**
 * Condition Rule 工具函数
 * 用于 ConditionRuleForm 组件的纯函数逻辑抽离
 */

import type {
	RuleFormItem,
	DynamicFieldConstraints,
	QuestionMetadata,
	ValueOption,
	ValueInputType,
	OperatorOption,
} from '#/condition';
import type { DynamicList, DynamicDropdownItem } from '#/dynamic';
import { propertyTypeEnum } from '@/enums/appEnum';
import {
	dynamicFieldInputTypeMap,
	questionTypeInputMap,
	dynamicFieldOperatorMap,
	questionTypeOperatorMap,
	checklistOperators,
	allOperators,
} from '@/enums/conditionEnum';

// ============ 组件 Key 解析 ============

/**
 * 解析组件 key 获取类型和 ID
 * @example parseComponentKey('field_123') => { type: 'fields', id: '123' }
 * @example parseComponentKey('questionnaire_456') => { type: 'questionnaires', id: '456' }
 */
export const parseComponentKey = (key: string): { type: string; id?: string } => {
	if (key === 'fields') {
		return { type: 'fields' };
	}
	if (key.startsWith('field_')) {
		return { type: 'fields', id: key.replace('field_', '') };
	}
	if (key.startsWith('questionnaire_')) {
		return { type: 'questionnaires', id: key.replace('questionnaire_', '') };
	}
	if (key.startsWith('checklist_')) {
		return { type: 'checklist', id: key.replace('checklist_', '') };
	}
	return { type: 'fields' };
};

/**
 * 根据 rule 生成组件 key
 */
export const generateComponentKey = (rule: RuleFormItem): string => {
	if (rule.componentType === 'fields' && rule.fieldPath) {
		const fieldId = getFieldIdFromPath(rule.fieldPath);
		if (fieldId) return `field_${fieldId}`;
	}
	if (rule.componentType === 'questionnaires' && rule.componentId) {
		return `questionnaire_${rule.componentId}`;
	}
	if (rule.componentType === 'checklist' && rule.componentId) {
		return `checklist_${rule.componentId}`;
	}
	return '';
};

/**
 * 从 fieldPath 中提取字段 ID
 */
export const getFieldIdFromPath = (fieldPath: string): string => {
	if (!fieldPath) return '';
	const match = fieldPath.match(/input\.fields\.(.+)/);
	return match ? match[1] : '';
};

// ============ Label 映射 ============

const fieldLabelMap: Record<string, string> = {
	questionnaires: 'Question',
	checklist: 'Task',
	fields: 'Field',
};

/**
 * 根据组件类型获取 Field label
 */
export const getFieldLabel = (componentType: string): string => {
	return fieldLabelMap[componentType] || 'Field';
};

// ============ 类型判断 ============

const gridTypes = ['multiple_choice_grid', 'checkbox_grid', 'short_answer_grid'];

/**
 * 判断问题是否为 Grid 类型
 */
export const isGridType = (metadata: QuestionMetadata | undefined): boolean => {
	if (!metadata) return false;
	return gridTypes.includes(metadata.type);
};

/**
 * 判断 Grid 类型是否需要文本输入
 * - short_answer_grid 类型：始终使用文本输入
 * - multiple_choice_grid / checkbox_grid 类型：如果选择的列是 Other，使用文本输入
 */
export const isGridTextInput = (
	metadata: QuestionMetadata | undefined,
	columnKey: string | undefined
): boolean => {
	if (!metadata) return true;

	// short_answer_grid 始终使用文本输入
	if (metadata.type === 'short_answer_grid') {
		return true;
	}

	// multiple_choice_grid / checkbox_grid：检查选择的列是否是 Other
	if (columnKey && metadata.columns?.length) {
		const selectedCol = metadata.columns.find((col) => col.id === columnKey);
		if (selectedCol?.isOther) {
			return true;
		}
	}

	return false;
};

/**
 * 判断列是否是 Other 类型
 */
export const isColumnOther = (metadata: QuestionMetadata | undefined, colId: string): boolean => {
	if (!metadata?.columns?.length) return false;
	const col = metadata.columns.find((c) => c.id === colId);
	return col?.isOther ?? false;
};

// ============ 值输入类型 ============

/**
 * 根据规则获取值输入类型
 */
export const getValueInputType = (
	rule: RuleFormItem,
	fieldInfo: DynamicList | undefined,
	metadata: QuestionMetadata | undefined
): ValueInputType => {
	// 动态字段类型
	if (rule.componentType === 'fields') {
		if (!fieldInfo) return 'text';
		return dynamicFieldInputTypeMap[fieldInfo.dataType] || 'text';
	}

	// 问卷问题类型
	if (rule.componentType === 'questionnaires') {
		if (!metadata) return 'text';
		return questionTypeInputMap[metadata.type] || 'text';
	}

	return 'text';
};

// ============ 操作符 ============

/**
 * 根据规则获取可用的操作符列表
 */
export const getOperatorsForRule = (
	rule: RuleFormItem,
	fieldInfo: DynamicList | undefined,
	metadata: QuestionMetadata | undefined
): OperatorOption[] => {
	// Checklist 类型使用专用操作符
	if (rule.componentType === 'checklist') {
		return checklistOperators;
	}

	// 动态字段类型
	if (rule.componentType === 'fields') {
		if (!fieldInfo) return allOperators;
		return dynamicFieldOperatorMap[fieldInfo.dataType] || allOperators;
	}

	// 问卷问题类型
	if (rule.componentType === 'questionnaires') {
		if (!metadata) return allOperators;
		return questionTypeOperatorMap[metadata.type] || allOperators;
	}

	return allOperators;
};

// ============ 字段约束 ============

/**
 * 获取动态字段的约束配置
 */
export const getFieldConstraints = (
	fieldInfo: DynamicList | undefined
): DynamicFieldConstraints => {
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

	// DropdownSelect 类型约束
	if (fieldInfo.dataType === propertyTypeEnum.DropdownSelect) {
		constraints.allowMultiple = fieldInfo.additionalInfo?.allowMultiple ?? false;
		constraints.allowSearch = fieldInfo.additionalInfo?.allowSearch ?? true;
	}

	return constraints;
};

// ============ 选项生成 ============

/**
 * 获取 Grid 问题的行选项
 */
export const getGridRowOptions = (metadata: QuestionMetadata | undefined): ValueOption[] => {
	if (!metadata?.rows?.length) return [];
	return metadata.rows.map((row) => ({
		label: row.label,
		value: row.id,
	}));
};

/**
 * 获取 Grid 问题的列选项
 */
export const getGridColumnOptions = (metadata: QuestionMetadata | undefined): ValueOption[] => {
	if (!metadata?.columns?.length) return [];
	return metadata.columns.map((col) => ({
		label: col.label + (col.isOther ? ' (Other)' : ''),
		value: col.id,
	}));
};

/**
 * 生成动态字段的值选项
 */
export const getFieldValueOptions = (fieldInfo: DynamicList | undefined): ValueOption[] => {
	if (!fieldInfo) return [];

	const options: ValueOption[] = [];

	if (fieldInfo.dataType === propertyTypeEnum.DropdownSelect) {
		// 下拉框类型，使用 dropdownItems
		if (fieldInfo.dropdownItems?.length) {
			fieldInfo.dropdownItems.forEach((item: DynamicDropdownItem) => {
				options.push({
					label: item.value,
					value: item.value,
				});
			});
		}
	} else if (fieldInfo.dataType === propertyTypeEnum.Switch) {
		// 开关类型，固定 Yes/No 选项
		const trueLabel = fieldInfo.additionalInfo?.trueLabel || 'Yes';
		const falseLabel = fieldInfo.additionalInfo?.falseLabel || 'No';
		options.push({ label: trueLabel, value: 'true' }, { label: falseLabel, value: 'false' });
	}

	return options;
};

/**
 * 生成问卷问题的值选项
 */
export const getQuestionValueOptions = (metadata: QuestionMetadata | undefined): ValueOption[] => {
	if (!metadata) return [];

	const options: ValueOption[] = [];

	if (metadata.options?.length) {
		// 选择类型问题 (multiple_choice, checkboxes, dropdown)
		metadata.options.forEach((opt) => {
			options.push({
				label: opt.label,
				value: opt.value || opt.label,
			});
		});
	} else if (metadata.type === 'rating') {
		// 评分类型：生成 1 到 max 的选项
		const max = metadata.max || 5;
		for (let i = 1; i <= max; i++) {
			options.push({
				label: `${i} Star${i > 1 ? 's' : ''}`,
				value: String(i),
			});
		}
	} else if (metadata.type === 'linear_scale') {
		// 线性量表：生成 min 到 max 的选项
		const min = metadata.min || 1;
		const max = metadata.max || 10;
		for (let i = min; i <= max; i++) {
			let label = String(i);
			if (i === min && metadata.minLabel) {
				label = `${i} - ${metadata.minLabel}`;
			} else if (i === max && metadata.maxLabel) {
				label = `${i} - ${metadata.maxLabel}`;
			}
			options.push({ label, value: String(i) });
		}
	}

	return options;
};
