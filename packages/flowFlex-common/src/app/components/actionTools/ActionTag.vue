<template>
	<div class="flex justify-center items-center" @click.stop>
		<el-tooltip
			v-if="action"
			:content="action.name || action.actionName || 'Action'"
			placement="top"
		>
			<div
				:class="['action-icon-tag', sizeClasses, cursorClasses, { 'opacity-50': disabled }]"
				@click="handleClick"
			>
				<Icon icon="mdi:script-text" class="action-icon" />
			</div>
		</el-tooltip>

		<ActionResultDialog
			v-model="dialogVisible"
			:triggerSourceId="triggerSourceId"
			:triggerSourceType="triggerSourceType"
			:actionName="action?.name || action?.actionName || ''"
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
	action?: ActionInfo;
	triggerSourceId: string; // question id, option id, task id, or stage id
	triggerSourceType: string; // 'question', 'option', 'task', 'stage'
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
	if (props.disabled || !props.action) return;
	dialogVisible.value = true;
};
</script>

<style scoped>
.action-icon-tag {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	border-radius: 50%;
	transition: all 0.2s ease;
	user-select: none;
	background-color: #f3f4f6;
	border: 1px solid #e5e7eb;
	color: #6366f1;
}

.action-icon {
	width: 1.2em;
	height: 1.2em;
	flex-shrink: 0;
}

.action-icon-tag:hover {
	box-shadow: 0 2px 8px rgba(99, 102, 241, 0.25);
	background-color: #eef2ff;
	border-color: #c7d2fe;
}

.action-icon-tag:active {
	transform: scale(0.98);
	transition-duration: 0.1s;
}

.action-icon-tag.opacity-50:hover {
	transform: none;
	box-shadow: none;
}

/* 暗色主题支持 */
html.dark .action-icon-tag {
	background-color: #374151;
	border-color: #4b5563;
	color: #818cf8;
}

html.dark .action-icon-tag:hover {
	background-color: #4338ca;
	border-color: #6366f1;
	color: #c7d2fe;
}
</style>
