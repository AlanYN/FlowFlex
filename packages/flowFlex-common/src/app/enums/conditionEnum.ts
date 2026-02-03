/**
 * 条件规则相关枚举和常量
 * 用于 ConditionRuleForm 组件的类型映射和操作符配置
 */

import { propertyTypeEnum } from './appEnum';
import type { ValueInputType, OperatorOption } from '#/condition';

// ============ 值输入类型映射 ============

/**
 * 动态字段类型到值输入类型的映射
 */
export const dynamicFieldInputTypeMap: Record<number, ValueInputType> = {
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

/**
 * 问卷问题类型到值输入类型的映射
 */
export const questionTypeInputMap: Record<string, ValueInputType> = {
	short_answer: 'text',
	paragraph: 'text',
	multiple_choice: 'select',
	checkboxes: 'select',
	dropdown: 'select',
	number: 'number',
	date: 'date',
	time: 'time',
	rating: 'number',
	linear_scale: 'number',
	file: 'text',
	file_upload: 'text',
	multiple_choice_grid: 'select',
	checkbox_grid: 'select',
	short_answer_grid: 'text',
};

/**
 * 不支持条件配置的问题类型
 */
export const unsupportedQuestionTypes: string[] = [
	'divider',
	'description',
	'page_break',
	'video',
	'image',
];

// ============ 操作符配置 ============

/**
 * 文本类型操作符
 */
export const textOperators: OperatorOption[] = [
	{ value: '==', label: '=' },
	{ value: '>', label: '>' },
	{ value: '<', label: '<' },
	{ value: '>=', label: '≥' },
	{ value: '<=', label: '≤' },
	{ value: '!=', label: '≠' },
	{ value: 'Contains', label: 'Contains' },
	{ value: 'DoesNotContains', label: 'Does Not Contains' },
	{ value: 'StartsWith', label: 'Starts With' },
	{ value: 'EndsWith', label: 'Ends With' },
	{ value: 'IsEmpty', label: 'Is Empty' },
	{ value: 'IsNotEmpty', label: 'Is Not Empty' },
];

/**
 * 数字类型操作符
 */
export const numericOperators: OperatorOption[] = [
	{ value: '==', label: '=' },
	{ value: '!=', label: '≠' },
	{ value: '>', label: '>' },
	{ value: '<', label: '<' },
	{ value: '>=', label: '≥' },
	{ value: '<=', label: '≤' },
	{ value: 'IsEmpty', label: 'Is Empty' },
	{ value: 'IsNotEmpty', label: 'Is Not Empty' },
];

/**
 * 选择类型操作符
 */
export const selectionOperators: OperatorOption[] = [
	{ value: '==', label: '=' },
	{ value: '!=', label: '≠' },
	{ value: 'InList', label: 'In List' },
	{ value: 'NotInList', label: 'Not In List' },
	{ value: 'IsEmpty', label: 'Is Empty' },
	{ value: 'IsNotEmpty', label: 'Is Not Empty' },
];

/**
 * 日期类型操作符
 */
export const dateOperators: OperatorOption[] = [
	{ value: '==', label: '=' },
	{ value: '!=', label: '≠' },
	{ value: '>', label: '>' },
	{ value: '<', label: '<' },
	{ value: '>=', label: '≥' },
	{ value: '<=', label: '≤' },
	{ value: 'IsEmpty', label: 'Is Empty' },
	{ value: 'IsNotEmpty', label: 'Is Not Empty' },
];

/**
 * Checklist 专用操作符
 */
export const checklistOperators: OperatorOption[] = [
	{ value: 'CompleteTask', label: 'Complete Task' },
	// { value: 'CompleteStage', label: 'Complete Stage' },
];

/**
 * 所有操作符（用于默认情况）
 */
export const allOperators: OperatorOption[] = [
	{ value: '==', label: '=' },
	{ value: '!=', label: '≠' },
	{ value: '>', label: '>' },
	{ value: '<', label: '<' },
	{ value: '>=', label: '≥' },
	{ value: '<=', label: '≤' },
	{ value: 'Contains', label: 'Contains' },
	{ value: 'DoesNotContains', label: 'Does Not Contains' },
	{ value: 'StartsWith', label: 'Starts With' },
	{ value: 'EndsWith', label: 'Ends With' },
	{ value: 'IsEmpty', label: 'Is Empty' },
	{ value: 'IsNotEmpty', label: 'Is Not Empty' },
	{ value: 'InList', label: 'In List' },
	{ value: 'NotInList', label: 'Not In List' },
];

/**
 * 动态字段类型到操作符的映射
 */
export const dynamicFieldOperatorMap: Record<number, OperatorOption[]> = {
	[propertyTypeEnum.SingleLineText]: textOperators,
	[propertyTypeEnum.MultilineText]: textOperators,
	[propertyTypeEnum.Phone]: textOperators,
	[propertyTypeEnum.Email]: textOperators,
	[propertyTypeEnum.Number]: numericOperators,
	[propertyTypeEnum.DropdownSelect]: selectionOperators,
	[propertyTypeEnum.Switch]: selectionOperators,
	[propertyTypeEnum.DatePicker]: dateOperators,
	[propertyTypeEnum.File]: selectionOperators,
	[propertyTypeEnum.Pepole]: selectionOperators,
};

/**
 * 问卷问题类型到操作符的映射
 */
export const questionTypeOperatorMap: Record<string, OperatorOption[]> = {
	short_answer: textOperators,
	paragraph: textOperators,
	multiple_choice: selectionOperators,
	checkboxes: selectionOperators,
	dropdown: selectionOperators,
	number: numericOperators,
	date: dateOperators,
	time: dateOperators,
	rating: numericOperators,
	linear_scale: numericOperators,
	file: selectionOperators,
	file_upload: selectionOperators,
	multiple_choice_grid: selectionOperators,
	checkbox_grid: selectionOperators,
	short_answer_grid: textOperators,
};
