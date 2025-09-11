<template>
	<span
		:class="[
			'inline-flex items-center rounded-full font-medium transition-all duration-300 ease-out',
			'backdrop-blur-sm relative overflow-hidden select-none',
			'shadow-lg hover:shadow-xl transform-gpu',
			sizeClasses,
			typeClasses,
			cursorClasses,
		]"
		class="mt-1"
		@click="handleClick"
	>
		<span
			v-if="dot"
			:class="[
				'rounded-full mr-2 flex-shrink-0',
				dotSizeClasses,
				dotClasses,
				{ 'animate-pulse': pulse },
			]"
		></span>
		<slot>{{ text }}</slot>
		<el-icon
			v-if="closable"
			class="ml-2 cursor-pointer hover:bg-black hover:bg-opacity-10 dark:hover:bg-white dark:hover:bg-opacity-10 rounded-full p-0.5 transition-all duration-200 hover:scale-110 active:scale-95"
			@click.stop="handleClose"
		>
			<Close />
		</el-icon>
	</span>
</template>

<script setup lang="ts">
import { computed, useAttrs } from 'vue';
import { Close } from '@element-plus/icons-vue';

type TagType = 'primary' | 'success' | 'warning' | 'danger' | 'info' | 'default';
type TagSize = 'large' | 'default' | 'small';

interface Props {
	type?: TagType;
	size?: TagSize;
	text?: string;
	closable?: boolean;
	dot?: boolean;
	pulse?: boolean;
	round?: boolean;
	disabled?: boolean;
	loading?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	type: 'default',
	size: 'default',
	closable: false,
	dot: false,
	pulse: false,
	round: true,
	disabled: false,
	loading: false,
});

const emit = defineEmits<{
	close: [event: MouseEvent];
	click: [event: MouseEvent];
}>();

const attrs = useAttrs();

// 尺寸样式
const sizeClasses = computed(() => {
	switch (props.size) {
		case 'large':
			return 'px-5 py-2.5 text-base font-semibold';
		case 'small':
			return 'px-3 py-1 text-xs font-medium';
		default:
			return 'px-4 py-1.5 text-sm font-medium';
	}
});

// 点的尺寸
const dotSizeClasses = computed(() => {
	switch (props.size) {
		case 'large':
			return 'w-3 h-3';
		case 'small':
			return 'w-1.5 h-1.5';
		default:
			return 'w-2 h-2';
	}
});

// 类型配置映射
const typeConfig = {
	primary: { colors: ['blue', 'indigo', 'purple'], main: 'blue' },
	success: { colors: ['emerald', 'teal', 'green'], main: 'emerald' },
	warning: { colors: ['amber', 'yellow', 'orange'], main: 'amber' },
	danger: { colors: ['red', 'rose', 'pink'], main: 'red' },
	info: { colors: ['slate', 'gray', 'zinc'], main: 'slate' },
	default: { colors: ['gray', 'neutral', 'stone'], main: 'gray' },
} as const;

// 类型样式 - 完整版本
const typeClasses = computed(() => {
	const config = typeConfig[props.type] || typeConfig.default;
	const [c1, c2, c3] = config.colors;
	const main = config.main;

	return [
		`gradient-tag-${props.type}`,
		`bg-gradient-to-br from-${c1}-100 via-${c2}-200 to-${c3}-300`,
		`text-${main}-800 border border-${main}-300/60`,
		`dark:from-${c1}-800/90 dark:via-${c2}-700/90 dark:to-${c3}-600/90`,
		`dark:text-${main}-100 dark:border-${main}-400/40`,
	].join(' ');
});

// 点的颜色 - 简化版本
const dotClasses = computed(() => {
	const config = typeConfig[props.type] || typeConfig.default;
	const [c1, c2, c3] = config.colors;

	return `bg-gradient-to-br from-${c1}-400 via-${c2}-500 to-${c3}-600 dot-shadow dark:from-${c1}-300 dark:via-${c2}-400 dark:to-${c3}-500`;
});

// 鼠标样式和交互效果
const cursorClasses = computed(() => {
	if (props.disabled) {
		return 'cursor-not-allowed opacity-60';
	}

	if (props.loading) {
		return 'cursor-wait loading';
	}

	const isInteractive = props.closable || !!attrs.onClick;

	if (isInteractive) {
		return 'cursor-pointer hover:scale-105 active:scale-95 hover:cursor-pointer';
	}

	return 'cursor-default';
});

const handleClick = (event: MouseEvent) => {
	if (props.disabled || props.loading) {
		event.preventDefault();
		event.stopPropagation();
		return;
	}
	emit('click', event);
};

const handleClose = (event: MouseEvent) => {
	if (props.disabled || props.loading) {
		event.preventDefault();
		event.stopPropagation();
		return;
	}
	emit('close', event);
};
</script>

<style scoped>
/* 基础样式 */
span {
	user-select: none;
}

/* 类型特定的阴影和暗色主题 */
.gradient-tag-primary {
	box-shadow:
		0 2px 8px rgba(59, 130, 246, 0.15),
		inset 0 1px 0 rgba(255, 255, 255, 0.6);
}
.gradient-tag-success {
	box-shadow:
		0 2px 8px rgba(16, 185, 129, 0.15),
		inset 0 1px 0 rgba(255, 255, 255, 0.6);
}
.gradient-tag-warning {
	box-shadow:
		0 2px 8px rgba(245, 158, 11, 0.15),
		inset 0 1px 0 rgba(255, 255, 255, 0.6);
}
.gradient-tag-danger {
	box-shadow:
		0 2px 8px rgba(239, 68, 68, 0.15),
		inset 0 1px 0 rgba(255, 255, 255, 0.6);
}
.gradient-tag-info {
	box-shadow:
		0 2px 8px rgba(100, 116, 139, 0.15),
		inset 0 1px 0 rgba(255, 255, 255, 0.6);
}
.gradient-tag-default {
	box-shadow:
		0 2px 8px rgba(107, 114, 128, 0.15),
		inset 0 1px 0 rgba(255, 255, 255, 0.6);
}

/* 点的阴影 */
.dot-shadow {
	box-shadow:
		0 1px 3px currentColor,
		inset 0 1px 0 rgba(255, 255, 255, 0.3);
}

/* 交互效果 */
span:hover {
	transform: translateY(-3px) scale(1.02);
	filter: drop-shadow(0 12px 32px rgba(0, 0, 0, 0.12)) drop-shadow(0 6px 16px rgba(0, 0, 0, 0.06));
	box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.8) !important;
}

span.cursor-default:hover {
	transform: translateY(-1px);
	filter: drop-shadow(0 6px 16px rgba(0, 0, 0, 0.08)) drop-shadow(0 3px 8px rgba(0, 0, 0, 0.04));
	box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.6) !important;
}

span:active {
	transform: translateY(1px) scale(0.96);
	transition-duration: 0.1s;
	filter: drop-shadow(0 2px 8px rgba(0, 0, 0, 0.15));
	box-shadow: inset 0 2px 4px rgba(0, 0, 0, 0.1) !important;
}

span.cursor-not-allowed:hover {
	transform: none;
	filter: none;
}

/* 微光效果 */
span::before {
	content: '';
	position: absolute;
	inset: 0;
	left: -100%;
	background: linear-gradient(
		90deg,
		transparent 0%,
		rgba(255, 255, 255, 0.4) 25%,
		rgba(255, 255, 255, 0.6) 50%,
		rgba(255, 255, 255, 0.4) 75%,
		transparent 100%
	);
	transition: left 0.6s cubic-bezier(0.4, 0, 0.2, 1);
	border-radius: inherit;
}

span:hover::before {
	left: 100%;
}

/* 脉动动画 */
.animate-pulse {
	animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
}

@keyframes pulse {
	0%,
	100% {
		opacity: 1;
		transform: scale(1);
		filter: drop-shadow(0 2px 8px rgba(0, 0, 0, 0.1));
	}
	50% {
		opacity: 0.8;
		transform: scale(1.08);
		filter: drop-shadow(0 4px 16px rgba(0, 0, 0, 0.15)) drop-shadow(0 0 0 4px currentColor);
	}
}

/* 暗色主题阴影覆盖 */
html.dark .gradient-tag-primary {
	box-shadow:
		0 2px 8px rgba(59, 130, 246, 0.25),
		inset 0 1px 0 rgba(255, 255, 255, 0.1);
}
html.dark .gradient-tag-success {
	box-shadow:
		0 2px 8px rgba(16, 185, 129, 0.25),
		inset 0 1px 0 rgba(255, 255, 255, 0.1);
}
html.dark .gradient-tag-warning {
	box-shadow:
		0 2px 8px rgba(245, 158, 11, 0.25),
		inset 0 1px 0 rgba(255, 255, 255, 0.1);
}
html.dark .gradient-tag-danger {
	box-shadow:
		0 2px 8px rgba(239, 68, 68, 0.25),
		inset 0 1px 0 rgba(255, 255, 255, 0.1);
}
html.dark .gradient-tag-info {
	box-shadow:
		0 2px 8px rgba(100, 116, 139, 0.25),
		inset 0 1px 0 rgba(255, 255, 255, 0.1);
}
html.dark .gradient-tag-default {
	box-shadow:
		0 2px 8px rgba(107, 114, 128, 0.25),
		inset 0 1px 0 rgba(255, 255, 255, 0.1);
}

html.dark span:hover {
	filter: drop-shadow(0 12px 32px rgba(0, 0, 0, 0.6)) drop-shadow(0 6px 16px rgba(0, 0, 0, 0.3));
	box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.2) !important;
}

html.dark span.cursor-default:hover {
	filter: drop-shadow(0 6px 16px rgba(0, 0, 0, 0.4)) drop-shadow(0 3px 8px rgba(0, 0, 0, 0.2));
	box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.15) !important;
}

html.dark span:active {
	filter: drop-shadow(0 2px 8px rgba(0, 0, 0, 0.5));
	box-shadow: inset 0 2px 4px rgba(0, 0, 0, 0.3) !important;
}

html.dark span::before {
	background: linear-gradient(
		90deg,
		transparent 0%,
		rgba(255, 255, 255, 0.15) 25%,
		rgba(255, 255, 255, 0.25) 50%,
		rgba(255, 255, 255, 0.15) 75%,
		transparent 100%
	);
}
</style>
