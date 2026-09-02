<template>
	<div
		ref="canvasRef"
		class="tc-canvas"
		:class="{ 'tc-canvas--panning': isPanning }"
		@mousemove="onMouseMove"
		@mouseup="onMouseUp"
		@mouseleave="onMouseLeave"
		@wheel.prevent="onWheel"
		@mousedown="onCanvasMouseDown"
		@keydown.space.prevent="onSpaceDown"
		@keyup.space="onSpaceUp"
		tabindex="0"
	>
		<div
			class="relative w-full h-full"
			:style="{
				transform: `translate(${panX}px, ${panY}px) scale(${props.zoomLevel ?? 1})`,
				transformOrigin: '0 0',
			}"
			@click.self="emit('deselect')"
		>
			<!-- SVG 层 -->
			<svg
				class="tc-svg absolute inset-0 w-full h-full overflow-visible pointer-events-none"
				xmlns="http://www.w3.org/2000/svg"
				aria-hidden="true"
			>
				<defs>
					<!-- 默认箭头 -->
					<marker
						id="tc-arrow"
						markerWidth="10"
						markerHeight="7"
						refX="9"
						refY="3.5"
						orient="auto"
					>
						<polygon points="0 0, 10 3.5, 0 7" :fill="colors.default" />
					</marker>
					<!-- 选中箭头 -->
					<marker
						id="tc-arrow-sel"
						markerWidth="10"
						markerHeight="7"
						refX="9"
						refY="3.5"
						orient="auto"
					>
						<polygon points="0 0, 10 3.5, 0 7" :fill="colors.selected" />
					</marker>
					<!-- hover 箭头 -->
					<marker
						id="tc-arrow-hover"
						markerWidth="10"
						markerHeight="7"
						refX="9"
						refY="3.5"
						orient="auto"
					>
						<polygon points="0 0, 10 3.5, 0 7" :fill="colors.hovered" />
					</marker>
					<!-- 预览箭头 -->
					<marker
						id="tc-arrow-preview"
						markerWidth="10"
						markerHeight="7"
						refX="9"
						refY="3.5"
						orient="auto"
					>
						<polygon points="0 0, 10 3.5, 0 7" :fill="colors.preview" />
					</marker>
				</defs>

				<!-- 已有连线：只渲染两端卡片都在 canvas 上的连线 -->
				<g
					v-for="conn in visibleConnections"
					:key="conn.id"
					class="tc-conn-group"
					@click.stop="emit('select-connection', conn.id)"
					@mouseenter="hoveredConnId = conn.id"
					@mouseleave="hoveredConnId = null"
				>
					<!-- 加宽透明路径，扩大点击区域 -->
					<path
						:d="getPath(conn.sourceWorkflowId, conn.targetWorkflowId)"
						class="tc-conn-hit"
					/>
					<!-- 实际显示路径 -->
					<path
						:d="getPath(conn.sourceWorkflowId, conn.targetWorkflowId)"
						class="tc-conn-line"
						:class="{
							'tc-conn-line--selected': conn.id === selectedConnectionId,
							'tc-conn-line--hovered':
								hoveredConnId === conn.id && conn.id !== selectedConnectionId,
						}"
						:marker-end="getArrowMarker(conn.id)"
					/>
					<!-- 条件标签（可点击，打开配置面板） -->
					<g
						v-if="conn.conditionSummary"
						class="tc-conn-label-group"
						@click.stop="emit('select-connection', conn.id)"
					>
						<rect
							class="tc-conn-label-bg"
							:class="{
								'tc-conn-label-bg--selected': conn.id === selectedConnectionId,
							}"
							:x="
								getMidpoint(conn.sourceWorkflowId, conn.targetWorkflowId).x -
								getLabelWidth(conn.conditionSummary) / 2
							"
							:y="getMidpoint(conn.sourceWorkflowId, conn.targetWorkflowId).y - 24"
							:width="getLabelWidth(conn.conditionSummary)"
							height="20"
							rx="4"
						/>
						<text
							class="tc-conn-label"
							:class="{ 'tc-conn-label--selected': conn.id === selectedConnectionId }"
							:x="getMidpoint(conn.sourceWorkflowId, conn.targetWorkflowId).x"
							:y="getMidpoint(conn.sourceWorkflowId, conn.targetWorkflowId).y - 8"
							text-anchor="middle"
							dominant-baseline="auto"
						>
							{{ getLabelText(conn.conditionSummary) }}
						</text>
					</g>
				</g>

				<!-- 连线预览线（拖拽中） -->
				<line
					v-if="previewLine"
					:x1="previewLine.x1"
					:y1="previewLine.y1"
					:x2="previewLine.x2"
					:y2="previewLine.y2"
					class="tc-preview-line"
					marker-end="url(#tc-arrow-preview)"
				/>
			</svg>

			<!-- 卡片层 -->
			<WorkflowCard
				v-for="card in cards"
				:key="card.workflowId"
				v-bind="card"
				:is-current="card.workflowId === currentWorkflowId"
				:incoming-count="getInCount(card.workflowId)"
				:outgoing-count="getOutCount(card.workflowId)"
				:is-connecting-source="connectingFrom === card.workflowId"
				:is-connecting-target="
					connectingFrom !== null &&
					connectingFrom !== card.workflowId &&
					// 已有 input 连线的 workflow 不能作为连线目标
					!connections.some((c) => c.targetWorkflowId === card.workflowId)
				"
				@drag-start="onDragStart"
				@connect-start="onConnectStart"
				@connect-end="onConnectEnd"
				@handle-drag-start="onHandleDragStart"
				@remove="emit('remove-card', $event)"
			/>

			<!-- 空状态 -->
			<transition name="tc-fade">
				<div
					v-if="cards.length === 0"
					class="tc-empty absolute inset-0 flex flex-col items-center justify-center gap-2 pointer-events-none"
				>
					<div class="tc-empty__icon opacity-50 mb-1">
						<svg width="48" height="48" viewBox="0 0 48 48" fill="none">
							<rect
								x="4"
								y="14"
								width="18"
								height="12"
								rx="3"
								stroke="#dcdfe6"
								stroke-width="2"
							/>
							<rect
								x="26"
								y="22"
								width="18"
								height="12"
								rx="3"
								stroke="#dcdfe6"
								stroke-width="2"
							/>
							<path
								d="M22 20L26 28"
								stroke="#dcdfe6"
								stroke-width="2"
								stroke-dasharray="3 2"
							/>
						</svg>
					</div>
					<p class="tc-empty__title text-[15px] font-semibold m-0">
						No workflows on canvas
					</p>
					<p class="tc-empty__desc text-[13px] m-0">
						Add workflows from the left panel to get started
					</p>
				</div>
			</transition>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import WorkflowCard from './WorkflowCard.vue';
import type { CanvasCard, TriggerConnection } from '@/hooks/useTriggerEditor';

interface Props {
	cards: readonly CanvasCard[];
	connections: readonly TriggerConnection[];
	currentWorkflowId: string;
	selectedConnectionId: string | null;
	connectingFrom: string | null;
	zoomLevel?: number;
}

const props = defineProps<Props>();

const emit = defineEmits<{
	'update-position': [workflowId: string, x: number, y: number];
	'add-connection': [sourceId: string, targetId: string];
	'remove-card': [workflowId: string];
	'select-connection': [connectionId: string];
	deselect: [];
	'connecting-start': [workflowId: string];
	'connecting-end': [workflowId: string];
	'connecting-cancel': [];
	zoom: [delta: number];
}>();

// ── 卡片尺寸常量（需与 CSS 一致）──────────────────────
const CARD_W = 188;
const CARD_H = 96; // body(~56) + toolbar(~40)

// ── refs ──────────────────────────────────────────────
const canvasRef = ref<HTMLDivElement | null>(null);
const hoveredConnId = ref<string | null>(null);

// ── 主题色（从 CSS 变量动态读取，支持主题切换） ──────────
const getCssVar = (name: string) =>
	getComputedStyle(document.documentElement).getPropertyValue(name).trim();

const colors = computed(() => ({
	default: getCssVar('--el-border-color') || '#dcdfe6',
	hovered: getCssVar('--el-color-primary-light-3') || '#79bbff',
	selected: getCssVar('--el-color-primary') || '#409eff',
	preview: getCssVar('--el-color-success') || '#67c23a',
}));

// Only render connections where both endpoints have a card on the canvas.
// Connections referencing a workflow that isn't on the canvas produce a (0,0) path
// that flies off the visible area.
const visibleConnections = computed(() =>
	props.connections.filter(
		(c) =>
			props.cards.some((card) => card.workflowId === c.sourceWorkflowId) &&
			props.cards.some((card) => card.workflowId === c.targetWorkflowId)
	)
);

// ── 拖拽状态 ──────────────────────────────────────────
interface DragState {
	workflowId: string;
	startCardX: number;
	startCardY: number;
	startMouseX: number;
	startMouseY: number;
}
const dragState = ref<DragState | null>(null);

// ── 画布平移（中键 / 空格+左键） ──────────────────────────
interface PanState {
	startPanX: number;
	startPanY: number;
	startMouseX: number;
	startMouseY: number;
}
const panState = ref<PanState | null>(null);
const isPanning = ref(false);
const spaceDown = ref(false);
const panX = ref(0);
const panY = ref(0);

const onSpaceDown = () => {
	spaceDown.value = true;
};
const onSpaceUp = () => {
	spaceDown.value = false;
};

const onCanvasMouseDown = (e: MouseEvent) => {
	// Middle-click OR space+left-click → start pan
	const isMiddle = e.button === 1;
	const isSpaceLeft = e.button === 0 && spaceDown.value;
	if (!isMiddle && !isSpaceLeft) return;
	e.preventDefault();
	isPanning.value = true;
	panState.value = {
		startPanX: panX.value,
		startPanY: panY.value,
		startMouseX: e.clientX,
		startMouseY: e.clientY,
	};
};

// ── 画布平移边界限制 ──────────────────────────────────────────
// CSS: transform: translate(panX, panY) scale(zoom)
// 实际变换顺序：先 scale，再 translate
// ∴ screen = canvas * zoom + pan
// 保证至少 PAN_MARGIN 屏幕 px 的卡片区域仍在视口内。
const PAN_MARGIN = 80; // 屏幕 px

const clampPan = (px: number, py: number): { x: number; y: number } => {
	const rect = canvasRef.value?.getBoundingClientRect();
	if (!rect || props.cards.length === 0) return { x: px, y: py };
	const zoom = props.zoomLevel ?? 1;

	// 所有卡片的包围盒（画布 px）
	let minX = Infinity,
		minY = Infinity,
		maxX = -Infinity,
		maxY = -Infinity;
	for (const c of props.cards) {
		minX = Math.min(minX, c.x);
		minY = Math.min(minY, c.y);
		maxX = Math.max(maxX, c.x + CARD_W);
		maxY = Math.max(maxY, c.y + CARD_H);
	}

	// screen = canvas * zoom + pan
	// 卡片右边界屏幕坐标 >= PAN_MARGIN:  maxX * zoom + pan >= PAN_MARGIN
	//   → pan >= PAN_MARGIN - maxX * zoom
	const minPanX = PAN_MARGIN - maxX * zoom;
	const minPanY = PAN_MARGIN - maxY * zoom;

	// 卡片左边界屏幕坐标 <= vw - PAN_MARGIN:  minX * zoom + pan <= vw - PAN_MARGIN
	//   → pan <= vw - PAN_MARGIN - minX * zoom
	const maxPanX = rect.width - PAN_MARGIN - minX * zoom;
	const maxPanY = rect.height - PAN_MARGIN - minY * zoom;

	return {
		x: Math.min(maxPanX, Math.max(minPanX, px)),
		y: Math.min(maxPanY, Math.max(minPanY, py)),
	};
};
const onWheel = (e: WheelEvent) => {
	if (e.ctrlKey || e.metaKey) {
		// Ctrl/Cmd + wheel → zoom
		const delta = e.deltaY > 0 ? -10 : 10;
		emit('zoom', delta);
	} else {
		// Plain scroll → pan the canvas (with boundary)
		const clamped = clampPan(panX.value - e.deltaX, panY.value - e.deltaY);
		panX.value = clamped.x;
		panY.value = clamped.y;
	}
};

// ── 连线预览 ──────────────────────────────────────────
interface PreviewLine {
	x1: number;
	y1: number;
	x2: number;
	y2: number;
}
const previewLine = ref<PreviewLine | null>(null);
const connectingSourceId = ref<string | null>(null);

// ── 连线统计 ──────────────────────────────────────────
const getInCount = (id: string) =>
	props.connections.filter((c) => c.targetWorkflowId === id).length;
const getOutCount = (id: string) =>
	props.connections.filter((c) => c.sourceWorkflowId === id).length;

// ── 路径计算 ──────────────────────────────────────────
const cardRight = (id: string) => {
	const c = props.cards.find((c) => c.workflowId === id);
	return c ? { x: c.x + CARD_W, y: c.y + CARD_H / 2 } : null;
};
const cardLeft = (id: string) => {
	const c = props.cards.find((c) => c.workflowId === id);
	return c ? { x: c.x, y: c.y + CARD_H / 2 } : null;
};

const getPath = (srcId: string, tgtId: string) => {
	const s = cardRight(srcId);
	const t = cardLeft(tgtId);
	// If either card is not on the canvas, skip rendering this connection
	if (!s || !t) return '';
	const dx = Math.max(Math.abs(t.x - s.x) * 0.5, 60);
	return `M ${s.x} ${s.y} C ${s.x + dx} ${s.y}, ${t.x - dx} ${t.y}, ${t.x} ${t.y}`;
};

const getMidpoint = (srcId: string, tgtId: string) => {
	const s = cardRight(srcId);
	const t = cardLeft(tgtId);
	if (!s || !t) return { x: 0, y: 0 };
	return { x: (s.x + t.x) / 2, y: (s.y + t.y) / 2 };
};

// 估算文字宽度（英文约 6.5px/字符 + 左右 padding 各 8px）
const getLabelWidth = (text: string) => {
	const truncated = text.length > 32 ? text.slice(0, 32) + '…' : text;
	return truncated.length * 6.5 + 16;
};

// 超长文字截断显示
const getLabelText = (text: string) => (text.length > 32 ? text.slice(0, 32) + '…' : text);

const getArrowMarker = (connId: string) => {
	if (connId === props.selectedConnectionId) return 'url(#tc-arrow-sel)';
	if (connId === hoveredConnId.value) return 'url(#tc-arrow-hover)';
	return 'url(#tc-arrow)';
};

// ── 卡片拖拽 ──────────────────────────────────────────
const onDragStart = (
	workflowId: string,
	startCardX: number,
	startCardY: number,
	startMouseX: number,
	startMouseY: number
) => {
	dragState.value = { workflowId, startCardX, startCardY, startMouseX, startMouseY };
};

// ── 连线操作 ──────────────────────────────────────────
const onConnectStart = (workflowId: string) => {
	emit('connecting-start', workflowId);
};

const onHandleDragStart = (workflowId: string, mouseX: number, mouseY: number) => {
	const rect = canvasRef.value?.getBoundingClientRect();
	if (!rect) return;
	const zoom = props.zoomLevel ?? 1;
	const src = cardRight(workflowId);
	connectingSourceId.value = workflowId;
	previewLine.value = {
		x1: src ? src.x : 0,
		y1: src ? src.y : 0,
		x2: (mouseX - rect.left - panX.value) / zoom,
		y2: (mouseY - rect.top - panY.value) / zoom,
	};
	emit('connecting-start', workflowId);
};

const onConnectEnd = (targetId: string) => {
	if (props.connectingFrom && props.connectingFrom !== targetId) {
		emit('add-connection', props.connectingFrom, targetId);
	}
	previewLine.value = null;
	connectingSourceId.value = null;
	dragState.value = null; // 确保连接后拖拽状态被清空
	emit('connecting-end', targetId);
};

// ── 全局鼠标事件 ──────────────────────────────────────
const onMouseMove = (e: MouseEvent) => {
	const rect = canvasRef.value?.getBoundingClientRect();
	if (!rect) return;

	// Canvas pan — use translate so we can pan in all directions
	if (panState.value) {
		const rawX = panState.value.startPanX + (e.clientX - panState.value.startMouseX);
		const rawY = panState.value.startPanY + (e.clientY - panState.value.startMouseY);
		const clamped = clampPan(rawX, rawY);
		panX.value = clamped.x;
		panY.value = clamped.y;
		return;
	}

	if (dragState.value) {
		const zoom = props.zoomLevel ?? 1;
		const dx = (e.clientX - dragState.value.startMouseX) / zoom;
		const dy = (e.clientY - dragState.value.startMouseY) / zoom;
		const nx = dragState.value.startCardX + dx;
		const ny = dragState.value.startCardY + dy;
		emit('update-position', dragState.value.workflowId, nx, ny);
	}

	if (previewLine.value) {
		const zoom = props.zoomLevel ?? 1;
		previewLine.value.x2 = (e.clientX - rect.left - panX.value) / zoom;
		previewLine.value.y2 = (e.clientY - rect.top - panY.value) / zoom;
	}
};

const onMouseUp = () => {
	if (panState.value) {
		panState.value = null;
		isPanning.value = false;
	}
	if (dragState.value) dragState.value = null;
	if (previewLine.value) {
		previewLine.value = null;
		connectingSourceId.value = null;
		emit('connecting-cancel');
	}
};

const onMouseLeave = () => {
	if (panState.value) {
		panState.value = null;
		isPanning.value = false;
	}
	if (previewLine.value) {
		previewLine.value = null;
		connectingSourceId.value = null;
		emit('connecting-cancel');
	}
};

// ── 定位到指定卡片（双击 sidebar 时调用）─────────────────────
const focusCard = (workflowId: string) => {
	const card = props.cards.find((c) => c.workflowId === workflowId);
	const canvasEl = canvasRef.value;
	if (!card || !canvasEl) return;
	const zoom = props.zoomLevel ?? 1;
	const rect = canvasEl.getBoundingClientRect();
	// screen = canvas * zoom + pan
	// 目标：卡片中心在屏幕中央
	//   cardCenter * zoom + pan = viewport / 2
	//   pan = viewport / 2 - cardCenter * zoom
	const cardCenterX = card.x + CARD_W / 2;
	const cardCenterY = card.y + CARD_H / 2;
	const rawX = rect.width / 2 - cardCenterX * zoom;
	const rawY = rect.height / 2 - cardCenterY * zoom;
	const clamped = clampPan(rawX, rawY);
	panX.value = clamped.x;
	panY.value = clamped.y;
};

// 缩放变化时重新 clamp pan，避免缩小后内容跑到边界外
watch(
	() => props.zoomLevel,
	() => {
		const clamped = clampPan(panX.value, panY.value);
		panX.value = clamped.x;
		panY.value = clamped.y;
	}
);

defineExpose({ focusCard });
</script>

<style scoped lang="scss">
.tc-canvas {
	position: relative;
	width: 100%;
	height: 100%;
	overflow: hidden;
	cursor: default;
	outline: none;
	/* 精致的圆点网格背景 */
	background-color: var(--el-fill-color-lighter, #f5f7fa);
	background-image: radial-gradient(circle, var(--el-border-color, #dcdfe6) 1px, transparent 1px);
	background-size: 28px 28px;

	&--panning {
		cursor: grabbing !important;
		user-select: none;
	}
}

.tc-conn-group {
	pointer-events: all;
	cursor: pointer;
}

/* 加宽透明击中区 */
.tc-conn-hit {
	fill: none;
	stroke: transparent;
	stroke-width: 14;
}

/* 实际线条 */
.tc-conn-line {
	fill: none;
	stroke: var(--el-border-color);
	stroke-width: 2;
	transition:
		stroke 0.15s ease,
		stroke-width 0.15s ease;

	&--hovered {
		stroke: var(--el-color-primary-light-3);
		stroke-width: 2.5;
	}

	&--selected {
		stroke: var(--el-color-primary);
		stroke-width: 2.5;
	}
}

/* 标签背景 */
.tc-conn-label-group {
	cursor: pointer;

	&:hover .tc-conn-label-bg {
		stroke: var(--el-color-primary-light-3);
		fill: var(--el-color-primary-light-9);
	}

	&:hover .tc-conn-label {
		fill: var(--el-color-primary);
	}
}

.tc-conn-label-bg {
	fill: var(--el-bg-color);
	stroke: var(--el-border-color);
	stroke-width: 1;
	filter: drop-shadow(0 1px 3px rgba(0, 0, 0, 0.08));
	pointer-events: all;

	&--selected {
		fill: var(--el-color-primary-light-9);
		stroke: var(--el-color-primary-light-5);
	}
}

/* 标签文字 */
.tc-conn-label {
	font-size: 11px;
	fill: var(--el-text-color-regular);
	font-family: inherit;
	pointer-events: none;

	&--selected {
		fill: var(--el-color-primary);
		font-weight: 600;
	}
}

/* ====================================================
   连线预览
==================================================== */
.tc-preview-line {
	stroke: var(--el-color-success);
	stroke-width: 2;
	stroke-dasharray: 6 4;
}

/* ====================================================
   空状态（颜色依赖 CSS 变量）
==================================================== */
.tc-empty__title {
	color: var(--el-text-color-secondary);
}
.tc-empty__desc {
	color: var(--el-text-color-placeholder);
}

/* 淡入动画 */
.tc-fade-enter-active,
.tc-fade-leave-active {
	transition: opacity 0.25s ease;
}
.tc-fade-enter-from,
.tc-fade-leave-to {
	opacity: 0;
}
</style>
