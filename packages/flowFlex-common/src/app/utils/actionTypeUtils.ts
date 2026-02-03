import type { ConditionActionType, ActionFormItem } from '#/condition';

// 动作类型选项（完整列表）
export const allActionTypes: { value: ConditionActionType; label: string; description: string }[] =
	[
		{
			value: 'GoToStage',
			label: 'Go to Stage',
			description: 'Jump to a specific workflow stage',
		},
		{
			value: 'SkipStage',
			label: 'Skip Stage',
			description: 'Skip the next stage and continue',
		},
		{
			value: 'EndWorkflow',
			label: 'End Workflow',
			description: 'Complete the workflow immediately',
		},
		{
			value: 'SendNotification',
			label: 'Send Notification',
			description: 'Send email to user or team',
		},
		{
			value: 'UpdateField',
			label: 'Update Field',
			description: 'Automatically update a field value',
		},
		{
			value: 'TriggerAction',
			label: 'Trigger Action',
			description: 'Execute a predefined action',
		},
		{
			value: 'AssignUser',
			label: 'Assign User',
			description: 'Reassign stage to specific user/team',
		},
	];

// Stage 状态变更类型（这三种类型只能选择其中一个）
export const stageChangeTypes: ConditionActionType[] = ['GoToStage', 'SkipStage', 'EndWorkflow'];

/**
 * 获取可用的 action types
 */
export const getAvailableActionTypes = (
	currentStageIndex: number,
	totalStages: number,
	existingActions: ActionFormItem[],
	currentActionIndex?: number
): typeof allActionTypes => {
	const isLastStage = currentStageIndex === totalStages - 1;
	const isSecondLastStage = currentStageIndex === totalStages - 2;

	// 检查其他 action 是否已经使用了任意一个 stage 状态变更类型
	const hasStageChangeTypeUsedByOthers = existingActions
		.filter((_, idx) => currentActionIndex === undefined || idx !== currentActionIndex)
		.some((action) => stageChangeTypes.includes(action.type));

	return allActionTypes.filter((type) => {
		// 最后一个 stage 不显示 GoToStage 和 SkipStage
		if (isLastStage && (type.value === 'GoToStage' || type.value === 'SkipStage')) {
			return false;
		}

		// 倒数第二个 stage 不显示 SkipStage
		if (isSecondLastStage && type.value === 'SkipStage') {
			return false;
		}

		// 如果其他 action 已经使用了任意一个 stage 状态变更类型，则隐藏所有 stage 状态变更类型
		if (stageChangeTypes.includes(type.value)) {
			if (hasStageChangeTypeUsedByOthers) {
				return false;
			}
		}

		return true;
	});
};

/**
 * 获取默认的 action type（根据当前 stage 位置和已有 actions）
 */
export const getDefaultActionType = (
	currentStageIndex: number,
	totalStages: number,
	existingActions: ActionFormItem[] = []
): ConditionActionType => {
	const availableTypes = getAvailableActionTypes(currentStageIndex, totalStages, existingActions);
	return availableTypes.length > 0 ? availableTypes[0].value : 'SendNotification';
};
