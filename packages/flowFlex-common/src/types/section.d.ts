// Section和条件跳转相关的类型定义

export interface Section {
	id: string;
	name: string;
	description?: string;
	questions?: string[]; // 问题ID数组
	order?: number;
	items: QuestionnaireSection[];
}

export interface JumpRule {
	id: string;
	questionId: string;
	optionId: string; // 选项ID
	optionLabel: string; // 选项文本
	targetSectionId: string; // 目标小节ID
	targetSectionName: string; // 目标小节名称
}

export interface QuestionWithJumpRules {
	id: string;
	type: string;
	question: string;
	required: boolean;
	description?: string;
	options: Array<{
		id: string;
		value: string;
		label: string;
		isOther: boolean;
	}>;
	jumpRules?: JumpRule[]; // 跳转规则
}

export interface JumpRuleConfig {
	questionId: string;
	rules: JumpRule[];
}

// 小节跳转验证结果
export interface SectionJumpValidation {
	isValid: boolean;
	errors: string[];
	warnings: string[];
}

export interface QuestionnaireSection {
	id: string;
	columns?: { id: string; isOther: boolean; label: string }[];
	description?: string;
	iconType?: string;
	jumpRules?: JumpRule[];
	max?: number;
	maxLabel?: string;
	min?: number;
	minLabel?: string;
	options?: {
		id: string;
		isOther: boolean;
		label: string;
		value: string;
	}[];
	question: string;
	requireOneResponsePerRow?: boolean;
	required?: boolean;
	rows?: { id: string; label: string }[];
	fileUrl?: string;
	type:
		| 'short_answer'
		| 'paragraph'
		| 'multiple_choice'
		| 'checkboxes'
		| 'dropdown'
		| 'number'
		| 'date'
		| 'time'
		| 'rating'
		| 'file'
		| 'linear_scale'
		| 'multiple_choice_grid'
		| 'checkbox_grid'
		| 'divider'
		| 'description'
		| 'page_break'
		| 'video'
		| 'image'
		| null;
}
