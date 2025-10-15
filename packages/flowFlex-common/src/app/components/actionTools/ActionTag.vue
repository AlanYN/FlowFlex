<template>
	<div class="flex justify-center items-center" @click.stop>
		<el-tooltip v-if="displayAction" :content="tooltipContent" placement="top">
			<div
				:class="['action-icon-tag', sizeClasses, cursorClasses, { 'opacity-50': disabled }]"
				@click="handleClick"
			>
				<!-- 始终显示图标 -->
				<Icon icon="material-symbols:smart-toy-outline-sharp" class="action-icon" />
				<!-- 多个 actions 时显示 +数量 -->
				<span v-if="isMultipleActions" class="action-count">+{{ actionCount }}</span>
			</div>
		</el-tooltip>

		<ActionResultDialog
			v-model="dialogVisible"
			:actions="shouldPassActionsArray ? actualActions : undefined"
			:action="shouldPassActionsArray ? undefined : action"
			:triggerSourceId="triggerSourceId"
			:actionName="dialogActionName"
			:onboardingId="onboardingId"
		/>
	</div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { Icon } from '@iconify/vue';
import ActionResultDialog from './ActionResultDialog.vue';

interface ActionInfo {
	id: string;
	name?: string;
	actionName?: string;
}

interface Props {
	action?: ActionInfo; // 单个 action（向后兼容）
	actions?: ActionInfo[]; // 多个 actions（新增）
	triggerSourceId?: string; // question id, option id, task id, or stage id（单个 action 时需要）
	triggerSourceType?: string; // 'question', 'option', 'task', 'stage'（单个 action 时需要）
	onboardingId: string; // current case/onboarding id
	size?: 'large' | 'default' | 'small';
	type?: 'success' | 'info' | 'warning' | 'danger';
	closable?: boolean;
	disabled?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	size: 'small',
	type: 'success',
	closable: false,
	disabled: false,
});

defineEmits<{
	close: [];
}>();

const dialogVisible = ref(false);

// 计算属性：获取实际的 actions 数组
const actualActions = computed(() => {
	if (props.actions && props.actions.length > 0) {
		return props.actions;
	} else if (props.action) {
		return [props.action];
	}
	return [];
});

// 计算属性：是否有可显示的 action
const displayAction = computed(() => {
	return actualActions.value.length > 0;
});

// 计算属性：是否是多个 actions
const isMultipleActions = computed(() => {
	return actualActions.value.length > 1;
});

// 计算属性：action 数量
const actionCount = computed(() => {
	return actualActions.value.length;
});

// 计算属性：tooltip 内容
const tooltipContent = computed(() => {
	if (isMultipleActions.value) {
		return `${actionCount.value} Actions`;
	} else if (actualActions.value.length === 1) {
		const action = actualActions.value[0];
		return action.name || action.actionName || 'Action';
	}
	return 'Action';
});

// 计算属性：对话框中显示的 action 名称
const dialogActionName = computed(() => {
	if (isMultipleActions.value) {
		return `${actionCount.value} Actions`;
	} else if (actualActions.value.length === 1) {
		const action = actualActions.value[0];
		return action.name || action.actionName || '';
	}
	return '';
});

// 计算属性：是否传递 actions 数组给 dialog
const shouldPassActionsArray = computed(() => {
	return isMultipleActions.value;
});

// Size classes
const sizeClasses = computed(() => {
	const sizeMap = {
		small: 'w-6 h-6 text-xs',
		default: 'w-8 h-8 text-sm',
		large: 'w-10 h-10 text-base',
	};
	return sizeMap[props.size];
});

// Cursor classes
const cursorClasses = computed(() => {
	return props.disabled ? 'cursor-not-allowed' : 'cursor-pointer';
});

const handleClick = () => {
	if (props.disabled || !displayAction.value) return;
	dialogVisible.value = true;
};
</script>

<style scoped lang="scss">
.action-icon-tag {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	border-radius: 50%;
	transition: all 0.2s ease;
	user-select: none;
	background-color: var(--el-color-primary);
	color: var(--el-color-white);
	position: relative;
}

.action-icon {
	width: 1.2em;
	height: 1.2em;
	flex-shrink: 0;
}

.action-count {
	@apply rounded-xl;
	position: absolute;
	top: -6px;
	right: -6px;
	background-color: var(--el-color-warning);
	color: var(--el-color-white);
	font-size: 0.6em;
	font-weight: 600;
	line-height: 1;
	padding: 2px 4px;
	min-width: 16px;
	height: 16px;
	display: flex;
	align-items: center;
	justify-content: center;
	border: 1px solid var(--el-color-white);
	box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.action-icon-tag:hover {
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
	background-color: var(--el-color-primary-light-9);
	border-color: var(--el-color-primary-light-5);
}

.action-icon-tag:active {
	transform: scale(0.98);
	transition-duration: 0.1s;
}

.action-icon-tag.opacity-50:hover {
	transform: none;
	box-shadow: none;
}

html.dark .action-count {
	background-color: var(--el-color-danger);
	border-color: var(--el-border-color);
}
</style>
