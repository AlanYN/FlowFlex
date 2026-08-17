<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted, nextTick } from 'vue';
import type { ComponentPublicInstance } from 'vue';

/**
 * Minimal shape of a PDF.js page proxy needed by this component.
 * PDF.js is loaded via CDN (window.pdfjsLib), so pdfjs-dist types are not imported.
 * Only the methods actually used here are declared.
 */
export interface PDFPageProxy {
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

/** Exported so DocumentSigningDialog can import PDF proxy types from one place */
export interface PDFDocumentProxy {
	numPages: number;
	getPage(pageNumber: number): Promise<PDFPageProxy>;
	destroy(): void;
}

interface Props {
	pdfDoc: PDFDocumentProxy | null;
	/** 1-based page number, same convention as PdfViewer's pageNumber prop */
	currentPage: number;
}

const props = withDefaults(defineProps<Props>(), {
	pdfDoc: null,
	currentPage: 1,
});

const emit = defineEmits<{
	/** Emits a 1-based page number to match PdfViewer's pageNumber convention */
	pageChanged: [pageIndex: number];
}>();

/** Total number of pages, derived from pdfDoc */
const totalPages = computed(() => props.pdfDoc?.numPages ?? 0);

/** Holds refs to each thumbnail canvas element */
const canvasRefs = ref<HTMLCanvasElement[]>([]);

/** Holds refs to the thumbnail item wrapper divs (used for scrollIntoView) */
const itemRefs = ref<HTMLElement[]>([]);

/** Tracks which page numbers have already been rendered to avoid duplicate renders */
const renderedPages = new Set<number>();

/** The active IntersectionObserver instance */
let observer: IntersectionObserver | null = null;

// ─────────────────────────────────────────────────────────────────────────────
// v-for ref callbacks
// ─────────────────────────────────────────────────────────────────────────────

function setCanvasRef(el: Element | ComponentPublicInstance | null, index: number): void {
	if (el instanceof HTMLCanvasElement) {
		canvasRefs.value[index] = el;
	}
}

function setItemRef(el: Element | ComponentPublicInstance | null, index: number): void {
	if (el instanceof HTMLElement) {
		itemRefs.value[index] = el;
	}
}

// ─────────────────────────────────────────────────────────────────────────────
// Thumbnail rendering
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Renders a single page thumbnail onto the given canvas at scale 0.15.
 * After rendering, the page resources are cleaned up and the canvas is
 * marked as rendered so it won't be re-rendered on subsequent scroll events.
 *
 * @param pageNum - 1-based page number to render
 * @param canvas  - Target HTMLCanvasElement to draw onto
 */
async function renderThumbnail(pageNum: number, canvas: HTMLCanvasElement): Promise<void> {
	if (!props.pdfDoc) return;
	if (renderedPages.has(pageNum)) return;

	// Mark early to prevent concurrent renders of the same page
	renderedPages.add(pageNum);

	try {
		const page = await props.pdfDoc.getPage(pageNum);
		const viewport = page.getViewport({ scale: 0.15 });

		const ctx = canvas.getContext('2d');
		if (!ctx) return;

		canvas.width = viewport.width;
		canvas.height = viewport.height;

		await page.render({ canvasContext: ctx, viewport }).promise;
		page.cleanup();
	} catch {
		// Remove from rendered set so it can be retried if the observer fires again
		renderedPages.delete(pageNum);
	}
}

// ─────────────────────────────────────────────────────────────────────────────
// IntersectionObserver lifecycle
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Creates a new IntersectionObserver that watches each thumbnail canvas.
 * When a canvas enters the viewport the thumbnail is rendered once, then
 * the observer stops watching that element (render-once, no re-render on scroll).
 */
function setupObserver(): void {
	if (observer) {
		observer.disconnect();
		observer = null;
	}

	if (!props.pdfDoc) return;

	observer = new IntersectionObserver((entries) => {
		entries.forEach((entry) => {
			if (!entry.isIntersecting) return;

			const canvas = entry.target as HTMLCanvasElement;
			const pageNum = parseInt(canvas.dataset['page'] ?? '0', 10);
			if (!pageNum) return;

			// Unobserve immediately — render once, not on every scroll
			observer?.unobserve(canvas);
			renderThumbnail(pageNum, canvas);
		});
	});

	// Observe all canvases currently in the DOM
	canvasRefs.value.forEach((canvas) => {
		if (canvas) observer?.observe(canvas);
	});
}

/**
 * Clears the rendered-pages tracking set and reconnects the observer.
 * Called when pdfDoc changes so all thumbnails are re-rendered for the new document.
 */
function resetAndObserve(): void {
	renderedPages.clear();
	nextTick(() => {
		setupObserver();
	});
}

onMounted(() => {
	if (props.pdfDoc) {
		nextTick(() => setupObserver());
	}
});

onUnmounted(() => {
	if (observer) {
		observer.disconnect();
		observer = null;
	}
});

// ─────────────────────────────────────────────────────────────────────────────
// Watchers
// ─────────────────────────────────────────────────────────────────────────────

/** Re-initialise when the PDF document is replaced (new file loaded) */
watch(
	() => props.pdfDoc,
	(newDoc) => {
		if (newDoc) {
			resetAndObserve();
		} else {
			observer?.disconnect();
			observer = null;
			renderedPages.clear();
		}
	}
);

/**
 * Scroll the active thumbnail into view whenever currentPage changes.
 * Uses smooth scrolling so the transition is not jarring.
 */
watch(
	() => props.currentPage,
	(page) => {
		nextTick(() => {
			const index = page - 1;
			const item = itemRefs.value[index];
			item?.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
		});
	}
);

// ─────────────────────────────────────────────────────────────────────────────
// User interactions
// ─────────────────────────────────────────────────────────────────────────────

/** Emit a 1-based page number when the user clicks a thumbnail */
function handlePageClick(pageNum: number): void {
	emit('pageChanged', pageNum);
}
</script>

<template>
	<div class="page-thumbnails">
		<template v-if="totalPages > 0">
			<div
				v-for="index in totalPages"
				:key="index"
				:ref="(el) => setItemRef(el, index - 1)"
				class="thumbnail-item"
				:class="{ 'thumbnail-item--active': index === currentPage }"
				@click="handlePageClick(index)"
			>
				<canvas
					:ref="(el) => setCanvasRef(el, index - 1)"
					:data-page="index"
					class="thumbnail-canvas"
				></canvas>
				<span class="thumbnail-label">Page {{ index }}</span>
			</div>
		</template>

		<div v-else class="thumbnail-empty">
			<span>No pages</span>
		</div>
	</div>
</template>

<style scoped lang="scss">
.page-thumbnails {
	width: 180px;
	height: 100%;
	overflow-y: auto;
	background: #2d2d2d;
	flex-shrink: 0;
	box-sizing: border-box;
}

.thumbnail-item {
	padding: 8px;
	cursor: pointer;
	border-radius: 4px;
	border: 2px solid transparent;
	box-sizing: border-box;
	transition: background-color 0.15s ease;

	&:hover {
		background-color: rgba(255, 255, 255, 0.06);
	}

	&--active {
		border-color: #7c3aed;
	}
}

.thumbnail-canvas {
	max-width: 100%;
	display: block;
	margin: 0 auto;
}

.thumbnail-label {
	display: block;
	text-align: center;
	font-size: 11px;
	color: #aaa;
	margin-top: 4px;
	user-select: none;
}

.thumbnail-empty {
	display: flex;
	align-items: center;
	justify-content: center;
	height: 100%;
	color: #666;
	font-size: 12px;
}
</style>
