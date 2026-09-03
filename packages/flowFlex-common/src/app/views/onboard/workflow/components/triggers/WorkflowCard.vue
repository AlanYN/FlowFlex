<template>
	<div
		class="wf-card"
		:class="{
			'wf-card--current': isCurrent,
			'wf-card--active': status === 'active',
			'wf-card--inactive': status === 'inactive',
			'wf-card--connecting-src': isConnectingSource,
			'wf-card--connecting-tgt': isConnectingTarget,
			'wf-card--dragging': isDragging,
		}"
		:style="{ left: `${x}px`, top: `${y}px` }"
		@mousedown.stop="onCardMouseDown"
	>
		<!-- 顶部色条 -->
		<div class="wf-card__stripe"></div>

		<!-- 主体内容 -->
		<div class="px-3 pt-[10px] pb-2">
			<!-- 名称行 -->
			<div class="flex items-center gap-1.5 mb-[5px]">
				<span
					class="wf-card__status-dot shrink-0 w-[7px] h-[7px] rounded-full"
					:class="`wf-card__status-dot--${status}`"
				></span>
				<span
					class="wf-card__name flex-1 text-[13px] font-semibold whitespace-nowrap overflow-hidden text-ellipsis leading-[1.4]"
					:title="name"
				>
					{{ name }}
				</span>
				<span
					v-if="isCurrent"
					class="wf-card__badge shrink-0 text-[10px] font-semibold px-1.5 py-px rounded-[4px] tracking-[0.02em]"
				>
					Current
				</span>
			</div>

			<!-- 连线计数 -->
			<div class="flex items-center gap-[5px]">
				<span class="wf-card__meta-item flex items-center gap-[3px] text-[11px]">
					<svg
						width="11"
						height="11"
						viewBox="0 0 11 11"
						fill="none"
						class="wf-card__meta-icon"
					>
						<path
							d="M5.5 1L1 5.5M5.5 1L10 5.5M5.5 1V10"
							stroke="currentColor"
							stroke-width="1.4"
							stroke-linecap="round"
							stroke-linejoin="round"
						/>
					</svg>
					{{ incomingCount }} in
				</span>
				<span class="wf-card__meta-sep text-[11px]">·</span>
				<span class="wf-card__meta-item flex items-center gap-[3px] text-[11px]">
					<svg
						width="11"
						height="11"
						viewBox="0 0 11 11"
						fill="none"
						class="wf-card__meta-icon"
					>
						<path
							d="M5.5 10L1 5.5M5.5 10L10 5.5M5.5 10V1"
							stroke="currentColor"
							stroke-width="1.4"
							stroke-linecap="round"
							stroke-linejoin="round"
						/>
					</svg>
					{{ outgoingCount }} out
				</span>
			</div>
		</div>

		<!-- 底部工具栏 -->
		<div class="wf-card__toolbar flex items-center gap-1 px-2 pt-1.5 pb-2 border-t">
			<el-button
				class="wf-card__btn--connect flex-1"
				:icon="Link"
				size="small"
				@mousedown.stop
				@click.stop="emit('connect-start', workflowId)"
			>
				Connect
			</el-button>

			<el-button
				v-if="!isCurrent"
				:icon="Delete"
				type="danger"
				text
				size="small"
				title="Remove from canvas"
				@mousedown.stop
				@click.stop="emit('remove', workflowId)"
			/>

			<!-- 拖拽连线 Handle -->
			<div
				class="wf-card__handle shrink-0 flex items-center justify-center w-6 h-6 rounded-[5px] cursor-crosshair transition-colors"
				title="Drag to connect to another workflow"
				@mousedown.stop="onHandleMouseDown"
			>
				<span
					class="wf-card__handle-dot w-2 h-2 rounded-full border-[1.5px] transition-all duration-150"
				></span>
			</div>
		</div>

		<!-- 连线模式：目标高亮蒙层 -->
		<div
			v-if="isConnectingTarget"
			class="wf-card__drop-overlay absolute inset-0 rounded-[10px] backdrop-blur-sm flex flex-col items-center justify-center gap-1 cursor-crosshair z-20"
			@mousedown.stop
			@mouseup.stop="emit('connect-end', workflowId)"
		>
			<el-icon class="wf-card__drop-icon text-[20px]"><Connection /></el-icon>
			<span class="wf-card__drop-label text-[11px] font-semibold">Connect here</span>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { Link, Delete, Connection } from '@element-plus/icons-vue';

interface Props {
	workflowId: string;
	name: string;
	status: 'active' | 'inactive';
	x: number;
	y: number;
	isCurrent?: boolean;
	incomingCount?: number;
	outgoingCount?: number;
	isConnectingSource?: boolean;
	isConnectingTarget?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	isCurrent: false,
	incomingCount: 0,
	outgoingCount: 0,
	isConnectingSource: false,
	isConnectingTarget: false,
});

const emit = defineEmits<{
	'drag-start': [
		workflowId: string,
		startX: number,
		startY: number,
		mouseX: number,
		mouseY: number,
	];
	'connect-start': [workflowId: string];
	'connect-end': [workflowId: string];
	'handle-drag-start': [workflowId: string, mouseX: number, mouseY: number];
	remove: [workflowId: string];
}>();

const isDragging = ref(false);

const onCardMouseDown = (e: MouseEvent) => {
	if (e.button !== 0) return;
	isDragging.value = true;
	emit('drag-start', props.workflowId, props.x, props.y, e.clientX, e.clientY);

	const onUp = () => {
		isDragging.value = false;
		window.removeEventListener('mouseup', onUp);
	};
	window.addEventListener('mouseup', onUp);
};

const onHandleMouseDown = (e: MouseEvent) => {
	if (e.button !== 0) return;
	e.preventDefault();
	emit('handle-drag-start', props.workflowId, e.clientX, e.clientY);
};
</script>

<style scoped lang="scss">
/* 卡片容器（box-shadow/animation/complex state，无法用 tailwind 替代） */
.wf-card {
	position: absolute;
	width: 188px;
	border-radius: 10px;
	background: var(--el-bg-color);
	border: 1.5px solid var(--el-border-color-light);
	box-shadow:
		0 2px 8px rgba(0, 0, 0, 0.08),
		0 0 0 0 transparent;
	cursor: grab;
	user-select: none;
	transition:
		border-color 0.18s ease,
		box-shadow 0.18s ease,
		transform 0.1s ease;

	&:hover:not(.wf-card--dragging) {
		border-color: var(--el-color-primary-light-5);
		box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12);
	}
	&--dragging {
		cursor: grabbing;
		box-shadow: 0 8px 24px rgba(0, 0, 0, 0.18);
		transform: scale(1.02);
		z-index: 100;
	}
	&--current {
		border-color: var(--el-color-primary);
		box-shadow:
			0 2px 8px rgba(0, 0, 0, 0.08),
			0 0 0 3px rgba(64, 158, 255, 0.15);
		.wf-card__stripe {
			background: var(--el-color-primary);
		}
		&:hover:not(.wf-card--dragging) {
			box-shadow:
				0 4px 16px rgba(0, 0, 0, 0.1),
				0 0 0 3px rgba(64, 158, 255, 0.2);
		}
	}
	&--connecting-src {
		border-color: var(--el-color-warning);
		box-shadow:
			0 2px 8px rgba(0, 0, 0, 0.08),
			0 0 0 3px rgba(230, 162, 60, 0.2);
		.wf-card__stripe {
			background: var(--el-color-warning);
		}
	}
	&--connecting-tgt {
		border-color: var(--el-color-success);
		box-shadow:
			0 4px 20px rgba(103, 194, 58, 0.25),
			0 0 0 3px rgba(103, 194, 58, 0.2);
		animation: pulse-border 1s ease infinite;
	}
}

@keyframes pulse-border {
	0%,
	100% {
		box-shadow:
			0 4px 20px rgba(103, 194, 58, 0.2),
			0 0 0 3px rgba(103, 194, 58, 0.15);
	}
	50% {
		box-shadow:
			0 4px 20px rgba(103, 194, 58, 0.35),
			0 0 0 4px rgba(103, 194, 58, 0.3);
	}
}

.wf-card__stripe {
	height: 3px;
	border-radius: 10px 10px 0 0;
	background: var(--el-border-color);
	transition: background 0.18s;
}
.wf-card--active .wf-card__stripe {
	background: var(--el-color-success);
}
.wf-card--inactive .wf-card__stripe {
	background: var(--el-text-color-placeholder);
}

.wf-card__toolbar {
	border-color: var(--el-border-color-lighter);
}

.wf-card__status-dot {
	&--active {
		background: var(--el-color-success);
	}
	&--inactive {
		background: var(--el-text-color-placeholder);
	}
}
.wf-card__name {
	color: var(--el-text-color-primary);
}
.wf-card__badge {
	background: var(--el-color-primary);
	color: var(--el-color-white);
	border: none;
}
.wf-card__meta-item {
	color: var(--el-text-color-secondary);
}
.wf-card__meta-icon {
	color: var(--el-text-color-placeholder);
}
.wf-card__meta-sep {
	color: var(--el-border-color);
}

/* Handle（含 :hover 嵌套子选择器） */
.wf-card__handle {
	border: 1px solid transparent;
	&:hover {
		background: var(--el-color-success-light-9);
		border-color: var(--el-color-success-light-5);
		.wf-card__handle-dot {
			background: var(--el-color-success);
			transform: scale(1.3);
			box-shadow: 0 0 0 3px rgba(103, 194, 58, 0.25);
		}
	}
	&:active {
		.wf-card__handle-dot {
			transform: scale(1.1);
		}
	}
}
.wf-card__handle-dot {
	background: var(--el-text-color-placeholder);
	border-color: var(--el-border-color);
}

/* Drop overlay */
.wf-card__drop-overlay {
	background: rgba(103, 194, 58, 0.1);
}
.wf-card__drop-icon {
	color: var(--el-color-success);
}
.wf-card__drop-label {
	color: var(--el-color-success);
}
</style>
