<script setup lang="ts">
import { ref, watch, nextTick, onUnmounted } from 'vue';

/**
 * Minimal shape of PDF.js PDFDocumentProxy needed by this component.
 * PDF.js is loaded via CDN (window.pdfjsLib), so pdfjs-dist types are not available.
 * Only the methods actually used here are declared.
 */
interface PDFPageProxy {
	getViewport(params: { scale: number }): { width: number; height: number };
	render(params: {
		canvasContext: CanvasRenderingContext2D;
		viewport: ReturnType<PDFPageProxy['getViewport']>;
	}): {
		promise: Promise<void>;
		cancel(): void;
	};
	cleanup(): void;
}

export interface PDFDocumentProxy {
	numPages: number;
	getPage(pageNumber: number): Promise<PDFPageProxy>;
	destroy(): void;
}

interface Props {
	pdfDoc: PDFDocumentProxy | null;
	pageNumber: number;
	scale: number; // 50–200, percentage (e.g. 100 = 100%)
}

const props = withDefaults(defineProps<Props>(), {
	pdfDoc: null,
	pageNumber: 1,
	scale: 100,
});

const emit = defineEmits<{
	rendered: [{ width: number; height: number }];
	loadFailed: [error: unknown];
}>();

const canvasRef = ref<HTMLCanvasElement | null>(null);

/**
 * Tracks the render task currently in progress so it can be cancelled
 * before starting a new one, preventing race conditions when props change
 * rapidly (e.g. fast zoom changes or quick page navigation).
 */
let pendingRenderTask: { promise: Promise<void>; cancel(): void } | null = null;

/**
 * Renders the current page onto the canvas using the provided scale.
 * Cancels any in-progress render before starting a new one.
 * Emits `rendered` with the canvas dimensions on success, or
 * `loadFailed` on any error.
 *
 * Validates: Requirements 9.1, 9.5, 9.6
 */
async function renderPage(): Promise<void> {
	// Cancel the previous render task if it has not completed yet.
	if (pendingRenderTask) {
		pendingRenderTask.cancel();
		pendingRenderTask = null;
	}

	if (!props.pdfDoc || !canvasRef.value) return;

	try {
		const page = await props.pdfDoc.getPage(props.pageNumber);
		const viewport = page.getViewport({ scale: props.scale / 100 });

		const canvas = canvasRef.value;
		const ctx = canvas.getContext('2d');
		if (!ctx) {
			emit('loadFailed', new Error('Failed to obtain 2D canvas context'));
			return;
		}

		// Sync canvas dimensions to the viewport — no CSS transform involved.
		canvas.width = viewport.width;
		canvas.height = viewport.height;

		const renderTask = page.render({ canvasContext: ctx, viewport });
		pendingRenderTask = renderTask;

		await renderTask.promise;
		pendingRenderTask = null;

		// Clean up page resources to free memory.
		page.cleanup();

		emit('rendered', { width: viewport.width, height: viewport.height });
	} catch (err: unknown) {
		// A cancelled render rejects with an object whose `name` is 'RenderingCancelledException'.
		// We silently ignore cancellations since they are intentional.
		if (
			err &&
			typeof err === 'object' &&
			(err as { name?: string }).name === 'RenderingCancelledException'
		) {
			return;
		}
		pendingRenderTask = null;
		emit('loadFailed', err);
	}
}

// Re-render whenever any of the three controlling props change.
// Use nextTick to ensure canvasRef is mounted before rendering,
// because PdfViewer is conditionally rendered (v-else-if="pdfDoc")
// and canvasRef.value is null during the immediate watch callback
// triggered at mount time.
watch(
	() => [props.pdfDoc, props.pageNumber, props.scale] as const,
	() => {
		nextTick(() => {
			renderPage();
		});
	},
	{ immediate: true }
);

onUnmounted(() => {
	if (pendingRenderTask) {
		pendingRenderTask.cancel();
		pendingRenderTask = null;
	}
});
</script>

<template>
	<div class="pdf-viewer">
		<canvas ref="canvasRef" class="pdf-canvas" />
	</div>
</template>

<style scoped lang="scss">
.pdf-viewer {
	display: inline-block;
	line-height: 0;
}

.pdf-canvas {
	display: block;
}
</style>
