/**
 * Stage Condition 类型定义
 * 与后端 StageCondition 实体对齐
 */

// ============ 操作符枚举 ============

/**
 * 条件操作符（用于 Microsoft RulesEngine 表达式）
 */
export type ConditionOperator =
	| '==' // 等于
	| '!=' // 不等于
	| '>' // 大于
	| '<' // 小于
	| '>=' // 大于等于
	| '<=' // 小于等于
	| 'Contains' // 包含
	| 'StartsWith' // 开头是
	| 'EndsWith' // 结尾是
	| 'CompleteTask' // Checklist: 完成任务时触发
	| 'CompleteStage'; // Checklist: 完成阶段时触发

/**
 * 操作符配置（用于 UI 展示）
 */
export interface OperatorConfig {
	value: ConditionOperator;
	label: string;
	description: string;
	requiresValue: boolean;
}

// ============ 动作类型枚举 ============

/**
 * 条件动作类型（7种）
 */
export type ConditionActionType =
	| 'GoToStage' // 跳转到特定阶段
	| 'SkipStage' // 跳过下一阶段
	| 'EndWorkflow' // 立即完成工作流
	| 'SendNotification' // 发送邮件/短信通知
	| 'UpdateField' // 自动更新字段值
	| 'TriggerAction' // 执行预定义动作
	| 'AssignUser'; // 重新分配用户/团队

/**
 * 动作类型配置（用于 UI 展示）
 */
export interface ActionTypeConfig {
	value: ConditionActionType;
	label: string;
	description: string;
	requiresParams: boolean;
}

// ============ 组件类型枚举 ============

/**
 * Stage 组件类型
 */
export type ComponentType = 'checklist' | 'questionnaires' | 'fields' | 'files';

// ============ 条件规则 ============

/**
 * 条件规则
 */
export interface ConditionRule {
	sourceStageId: string;
	componentType: ComponentType;
	componentId?: string; // checklist/questionnaire 需要，fields 不需要
	fieldPath: string;
	operator: ConditionOperator;
	value: string;
}

/**
 * 规则 JSON 结构（与后端 RulesJson 对齐）
 */
export interface RulesJson {
	logic: 'AND' | 'OR';
	rules: ConditionRule[];
}

// ============ 条件动作 ============

/**
 * 条件动作
 */
export interface ConditionAction {
	type: ConditionActionType;
	targetStageId?: string; // GoToStage 时使用
	actionDefinitionId?: string; // TriggerAction 时使用
	parameters?: Record<string, any>; // 动作参数
	order: number;
}

/**
 * GoToStage 动作参数
 */
export interface GoToStageParams {
	targetStageId: string;
}

/**
 * SendNotification 动作参数
 */
export interface SendNotificationParams {
	recipientType: 'user' | 'team' | 'email';
	recipientId?: string;
	recipientEmail?: string;
	templateId: string;
}

/**
 * UpdateField 动作参数
 */
export interface UpdateFieldParams {
	fieldPath: string;
	newValue: string | number | boolean;
}

/**
 * TriggerAction 动作参数
 */
export interface TriggerActionParams {
	actionDefinitionId: string;
	parameters?: Record<string, any>;
}

/**
 * AssignUser 动作参数
 */
export interface AssignUserParams {
	assigneeType: 'user' | 'team';
	assigneeId: string;
}

// ============ Fallback 配置 ============

/**
 * Fallback 配置
 */
export interface FallbackConfig {
	type: 'default' | 'specified';
	fallbackStageId?: string;
}

// ============ Stage Condition 实体 ============

/**
 * Stage Condition 实体（与后端对齐）
 */
export interface StageCondition {
	id: string;
	stageId: string;
	name: string;
	description?: string;
	order: number;
	rulesJson: RulesJson;
	actionsJson: ConditionAction[];
	fallbackStageId?: string;
	isActive: boolean;
	createDate?: string;
	createBy?: string;
	modifyDate?: string;
	modifyBy?: string;
}

/**
 * 创建/更新 Condition 的输入
 */
export interface StageConditionInput {
	name: string;
	description?: string;
	order?: number;
	rulesJson: RulesJson;
	actionsJson: ConditionAction[];
	fallbackStageId?: string;
	isActive?: boolean;
}

// ============ API 响应类型 ============

/**
 * Stage 组件信息（用于规则配置）
 */
export interface StageComponentInfo {
	id: string;
	name: string;
	type: ComponentType;
}

/**
 * 组件字段信息（用于规则配置）
 */
export interface ComponentFieldInfo {
	fieldPath: string;
	label: string;
	type: 'string' | 'number' | 'boolean' | 'date' | 'array';
}

// ============ UI 辅助类型 ============

/**
 * 规则表单项（UI 使用）
 */
export interface RuleFormItem extends ConditionRule {}

/**
 * 动作表单项（UI 使用）
 */
export interface ActionFormItem extends ConditionAction {}

/**
 * Condition 编辑器表单数据
 */
export interface ConditionFormData {
	name: string;
	description: string;
	logic: 'AND' | 'OR';
	rules: RuleFormItem[];
	actions: ActionFormItem[];
	fallbackType: 'default' | 'specified';
	fallbackStageId?: string;
}

// ============ Stage 进度状态（Case 详情页使用） ============

/**
 * Stage 状态枚举
 */
export type StageStatus =
	| 'Pending' // 待处理
	| 'Current' // 当前
	| 'Completed' // 已完成
	| 'Skipped'; // 已跳过（Condition 触发）

/**
 * Condition 执行结果
 */
export interface ConditionExecutionResult {
	conditionId: string;
	conditionName: string;
	isMatched: boolean;
	executedActions: {
		type: ConditionActionType;
		success: boolean;
		message?: string;
	}[];
	skippedStageIds?: string[];
	targetStageId?: string;
}
