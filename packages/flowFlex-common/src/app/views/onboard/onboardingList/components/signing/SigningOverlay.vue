<script setup lang="ts">
import { ref } from 'vue';
import { clampPosition, clampSize } from './utils/clampUtils';
import type { PlacedElement } from '@/views/profile/types';

export type { PlacedElement };

interface Props {
	elements: PlacedElement[];
	canvasWidth: number;
	canvasHeight: number;
	/** Zoom percentage, e.g. 100 means 100% */
	scale: number;
}

const props = withDefaults(defineProps<Props>(), {
	elements: () => [],
	canvasWidth: 0,
	canvasHeight: 0,
	scale: 100,
});

const emit = defineEmits<{
	elementMoved: [id: string, pos: { x: number; y: number }];
	elementResized: [id: string, size: { w: number; h: number }];
	elementDeleted: [id: string];
}>();

// ID of the currently selected element (null = none selected)
const selectedId = ref<string | null>(null);

/**
 * Converts a zoom-percentage value to a CSS scale factor.
 * e.g. scale=100 → 1.0, scale=150 → 1.5
 */
function scaleFactor(): number {
	return props.scale / 100;
}

/**
 * Page dimensions in PDF.js pt units.
 * Used by clamp helpers that work in pt space.
 */
function pagePtWidth(): number {
	return props.canvasWidth / scaleFactor();
}

function pagePtHeight(): number {
	return props.canvasHeight / scaleFactor();
}

// ─────────────────────────────────────────────────────────────────────────────
// Selection
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Select element when clicking on its body.
 * Stops propagation so the overlay's own click-outside handler is not triggered.
 */
function handleElementClick(event: MouseEvent, id: string) {
	event.stopPropagation();
	selectedId.value = id;
}

/**
 * Initiates a drag from anywhere on the element body.
 * This allows the user to drag the element by clicking anywhere on it,
 * not just the small move-handle dot in the top-left corner.
 */
function handleElementPointerDown(event: PointerEvent, el: PlacedElement) {
	// If the pointer is on a handle (move, delete, resize), let that handle's
	// own event listeners take over — do not start a body drag.
	const target = event.target as HTMLElement;
	if (target.classList.contains('handle')) return;

	// Only respond to primary button (left mouse / single touch)
	if (event.button !== 0 && event.pointerType === 'mouse') return;

	event.preventDefault();
	event.stopPropagation();

	selectedId.value = el.id;

	(event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);

	activeDrag.value = {
		pointerId: event.pointerId,
		startPointerX: event.clientX,
		startPointerY: event.clientY,
		startElementPtX: el.x,
		startElementPtY: el.y,
		elementPtW: el.width,
		elementPtH: el.height,
		elementType: el.type,
	};
}

function handleElementPointerMove(event: PointerEvent, el: PlacedElement) {
	// Reuse the same move logic as the handle
	handleMovePointerMove(event, el);
}

function handleElementPointerUp(event: PointerEvent) {
	// Reuse the same up logic as the handle
	handleMovePointerUp(event);
}

/** Clicking the overlay background deselects any active element. */
function handleOverlayClick() {
	selectedId.value = null;
}

// ─────────────────────────────────────────────────────────────────────────────
// Move (drag the move-handle — top-left purple circle)
// ─────────────────────────────────────────────────────────────────────────────

interface DragState {
	pointerId: number;
	startPointerX: number;
	startPointerY: number;
	startElementPtX: number;
	startElementPtY: number;
	elementPtW: number;
	elementPtH: number;
	elementType: 'signature' | 'date';
}

const activeDrag = ref<DragState | null>(null);

function handleMovePointerDown(event: PointerEvent, el: PlacedElement) {
	event.preventDefault();
	event.stopPropagation();

	// Ensure the element is selected while dragging
	selectedId.value = el.id;

	(event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);

	activeDrag.value = {
		pointerId: event.pointerId,
		startPointerX: event.clientX,
		startPointerY: event.clientY,
		startElementPtX: el.x,
		startElementPtY: el.y,
		elementPtW: el.width,
		elementPtH: el.height,
		elementType: el.type,
	};
}

function handleMovePointerMove(event: PointerEvent, el: PlacedElement) {
	const drag = activeDrag.value;
	if (!drag || drag.pointerId !== event.pointerId) return;

	// Delta in canvas pixels → convert to pt
	const sf = scaleFactor();
	const deltaPtX = (event.clientX - drag.startPointerX) / sf;
	const deltaPtY = (event.clientY - drag.startPointerY) / sf;

	const rawX = drag.startElementPtX + deltaPtX;
	const rawY = drag.startElementPtY + deltaPtY;

	const clamped = clampPosition(
		rawX,
		rawY,
		drag.elementPtW,
		drag.elementPtH,
		pagePtWidth(),
		pagePtHeight()
	);

	emit('elementMoved', el.id, clamped);
}

function handleMovePointerUp(event: PointerEvent) {
	const drag = activeDrag.value;
	if (!drag || drag.pointerId !== event.pointerId) return;

	(event.currentTarget as HTMLElement).releasePointerCapture(event.pointerId);
	activeDrag.value = null;
}

// ─────────────────────────────────────────────────────────────────────────────
// Resize (drag the resize-handle — bottom-right black circle)
// ─────────────────────────────────────────────────────────────────────────────

interface ResizeState {
	pointerId: number;
	startPointerX: number;
	startPointerY: number;
	startPtW: number;
	startPtH: number;
	elementType: 'signature' | 'date';
}

const activeResize = ref<ResizeState | null>(null);

function handleResizePointerDown(event: PointerEvent, el: PlacedElement) {
	event.preventDefault();
	event.stopPropagation();

	selectedId.value = el.id;

	(event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);

	activeResize.value = {
		pointerId: event.pointerId,
		startPointerX: event.clientX,
		startPointerY: event.clientY,
		startPtW: el.width,
		startPtH: el.height,
		elementType: el.type,
	};
}

function handleResizePointerMove(event: PointerEvent, el: PlacedElement) {
	const resize = activeResize.value;
	if (!resize || resize.pointerId !== event.pointerId) return;

	const sf = scaleFactor();
	const deltaPtW = (event.clientX - resize.startPointerX) / sf;
	const deltaPtH = (event.clientY - resize.startPointerY) / sf;

	const rawW = resize.startPtW + deltaPtW;
	const rawH = resize.startPtH + deltaPtH;

	const clamped = clampSize(rawW, rawH, resize.elementType);

	emit('elementResized', el.id, clamped);
}

function handleResizePointerUp(event: PointerEvent) {
	const resize = activeResize.value;
	if (!resize || resize.pointerId !== event.pointerId) return;

	(event.currentTarget as HTMLElement).releasePointerCapture(event.pointerId);
	activeResize.value = null;
}

// ─────────────────────────────────────────────────────────────────────────────
// Delete
// ─────────────────────────────────────────────────────────────────────────────

function handleDelete(event: MouseEvent, id: string) {
	event.stopPropagation();
	if (selectedId.value === id) {
		selectedId.value = null;
	}
	emit('elementDeleted', id);
}
</script>

<template>
	<!-- Transparent overlay — pointer-events disabled at container level so clicks
         on the PDF canvas pass through. Each placed element re-enables pointer-events. -->
	<div
		class="signing-overlay"
		:style="{
			width: `${canvasWidth}px`,
			height: `${canvasHeight}px`,
		}"
		@click="handleOverlayClick"
	>
		<div
			v-for="el in elements"
			:key="el.id"
			class="signing-element"
			:class="{ 'signing-element--selected': selectedId === el.id }"
			:style="{
				left: `${el.x * scaleFactor()}px`,
				top: `${el.y * scaleFactor()}px`,
				width: `${el.width * scaleFactor()}px`,
				height: `${el.height * scaleFactor()}px`,
			}"
			@click="handleElementClick($event, el.id)"
			@pointerdown="handleElementPointerDown($event, el)"
			@pointermove="handleElementPointerMove($event, el)"
			@pointerup="handleElementPointerUp($event)"
		>
			<!-- Signature: render image -->
			<img
				v-if="el.type === 'signature' && el.imageBase64"
				:src="el.imageBase64"
				class="signing-element__content signing-element__img"
				draggable="false"
				alt="Signature"
			/>

			<!-- Date: render text -->
			<span
				v-else-if="el.type === 'date'"
				class="signing-element__content signing-element__date"
				:style="{
					fontSize: `${Math.max(
						8,
						Math.min(el.height * scaleFactor() * 0.6, 36 * scaleFactor())
					)}px`,
				}"
			>
				{{ el.dateText }}
			</span>

			<!-- ── Handles (only visible when selected) ── -->
			<template v-if="selectedId === el.id">
				<!-- Top-left: move handle (purple circle) -->
				<div
					class="handle handle--move"
					@pointerdown="handleMovePointerDown($event, el)"
					@pointermove="handleMovePointerMove($event, el)"
					@pointerup="handleMovePointerUp($event)"
				></div>

				<!-- Top-right: delete button (red ×) -->
				<div class="handle handle--delete" @click.stop="handleDelete($event, el.id)">×</div>

				<!-- Bottom-right: resize handle (black circle) -->
				<div
					class="handle handle--resize"
					@pointerdown="handleResizePointerDown($event, el)"
					@pointermove="handleResizePointerMove($event, el)"
					@pointerup="handleResizePointerUp($event)"
				></div>
			</template>
		</div>
	</div>
</template>

<style scoped lang="scss">
.signing-overlay {
	position: absolute;
	top: 0;
	left: 0;
	pointer-events: none;
	// Overflow hidden prevents handles from extending outside canvas bounds
	overflow: hidden;
}

.signing-element {
	position: absolute;
	pointer-events: auto;
	cursor: grab;
	box-sizing: border-box;

	&:active {
		cursor: grabbing;
	}

	&--selected {
		outline: 1px dashed #7c3aed; // purple dashed border on selection
	}
}

.signing-element__content {
	display: block;
	width: 100%;
	height: 100%;
}

.signing-element__img {
	object-fit: contain;
	user-select: none;
	-webkit-user-drag: none;
}

.signing-element__date {
	display: flex;
	align-items: center;
	justify-content: center;
	/* font-size is set inline to match the PDF rendering formula */
	font-family: Helvetica, Arial, sans-serif;
	color: #1a1a1a;
	user-select: none;
	white-space: nowrap;
}

// ── Handle base styles ──────────────────────────────────────────────────────

.handle {
	position: absolute;
	width: 14px;
	height: 14px;
	border-radius: 50%;
	pointer-events: auto;
	// Shift handles so they sit at element corners, half inside / half outside
	transform: translate(-50%, -50%);
}

// Top-left: move handle — purple circle
.handle--move {
	top: 0;
	left: 0;
	background-color: #7c3aed;
	cursor: grab;

	&:active {
		cursor: grabbing;
	}
}

// Top-right: delete button — red circle with ×
.handle--delete {
	top: 0;
	left: 100%;
	background-color: #dc2626;
	color: #ffffff;
	font-size: 11px;
	font-weight: bold;
	line-height: 14px;
	text-align: center;
	cursor: pointer;
	border-radius: 50%;
}

// Bottom-right: resize handle — black circle
.handle--resize {
	top: 100%;
	left: 100%;
	background-color: #111827;
	cursor: nwse-resize;
}
</style>
