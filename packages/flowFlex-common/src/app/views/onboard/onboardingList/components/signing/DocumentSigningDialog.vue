<script setup lang="ts">
import { ref, shallowRef, markRaw, computed, watch } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import SigningToolbar from './SigningToolbar.vue';
import PageThumbnails from './PageThumbnails.vue';
import PdfViewer from './PdfViewer.vue';
import SigningOverlay from './SigningOverlay.vue';
import AddSignatureDialog from './AddSignatureDialog.vue';
import { loadPdfJs } from '@/components/ai/utils/pdfDetector';
import type { PDFDocumentProxy } from './PageThumbnails.vue';
import type { PlacedElement, SignedFileResponse } from '@/views/profile/types';
import { synthesizePdf } from './utils/pdfSynthesis';
import { signDocument } from '@/apis/ow/documentSigning';
import { useUserStoreWithOut } from '@/stores/modules/user';
import { previewOnboardingFile } from '@/apis/ow/onboarding';

/**
 * Generates a unique ID. Uses crypto.randomUUID() when available (HTTPS/localhost),
 * falls back to a Math.random-based UUID for non-secure contexts (e.g. HTTP dev access via IP).
 */
function generateId(): string {
	if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
		return crypto.randomUUID();
	}
	// RFC 4122 v4 UUID fallback
	return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
		const r = (Math.random() * 16) | 0;
		const v = c === 'x' ? r : (r & 0x3) | 0x8;
		return v.toString(16);
	});
}

// ========================= Props / Emits =========================

interface Props {
	visible: boolean;
	fileId: string | number;
	onboardingId: string | number;
	fileUrl: string;
	fileName: string;
}

const props = withDefaults(defineProps<Props>(), {
	visible: false,
	fileId: '',
	onboardingId: '',
	fileUrl: '',
	fileName: '',
});

const emit = defineEmits<{
	'update:visible': [value: boolean];
	/** Emitted after a successful sign operation so parent can refresh the file list */
	refreshDocuments: [];
}>();

// ========================= Internal State =========================

/** Current mode of the dialog */
type DialogMode = 'preview' | 'edit' | 'signed';

const mode = ref<DialogMode>('preview');
// shallowRef prevents Vue from wrapping PDFDocumentProxy in a Proxy.
// PDF.js uses ES2022 private fields (#field) internally; Vue's deep reactive
// Proxy breaks private field access with "Cannot read private member".
const pdfDoc = shallowRef<PDFDocumentProxy | null>(null);

/** 1-based current page number */
const currentPage = ref(1);
const totalPages = ref(0);

/** Zoom percentage: 50–200 */
const scale = ref(100);

/**
 * All placed elements keyed by 0-based pageIndex.
 * Preserves elements for all pages when navigating (Requirement 18.1).
 */
const elements = ref<Map<number, PlacedElement[]>>(new Map());

const isSaving = ref(false);
const signedFileData = ref<SignedFileResponse | null>(null);

/** True when PDF failed to load (e.g. encrypted PDF) */
const loadFailed = ref(false);

/** Loading state while fetching / parsing the PDF */
const isLoading = ref(false);

/** Whether the add-signature sub-dialog is open */
const addSignatureVisible = ref(false);

/** Canvas dimensions from the most recent PdfViewer render event */
const canvasWidth = ref(0);
const canvasHeight = ref(0);

// ========================= Computed =========================

/**
 * Flat list of elements for the current page only,
 * passed to SigningOverlay as its `elements` prop.
 */
const currentPageElements = computed<PlacedElement[]>(() => {
	// pageIndex is 0-based; currentPage is 1-based
	return elements.value.get(currentPage.value - 1) ?? [];
});

/**
 * True if at least one element exists across all pages.
 * Controls "Confirm Signature" button enabled state.
 */
const hasElements = computed<boolean>(() => {
	for (const pageElements of elements.value.values()) {
		if (pageElements.length > 0) return true;
	}
	return false;
});

/**
 * Total number of signature-type elements across all pages.
 * Used to enforce the 10-signature limit (Requirement 12.4).
 */
const totalSignatureCount = computed<number>(() => {
	let count = 0;
	for (const pageElements of elements.value.values()) {
		count += pageElements.filter((el) => el.type === 'signature').length;
	}
	return count;
});

/**
 * Total number of date-type elements across all pages.
 * Used to enforce the 10-date limit (Requirement 12.3).
 */
const totalDateCount = computed<number>(() => {
	let count = 0;
	for (const pageElements of elements.value.values()) {
		count += pageElements.filter((el) => el.type === 'date').length;
	}
	return count;
});

// ========================= PDF Loading =========================

/**
 * Loads the PDF from the `fileUrl` prop using PDF.js.
 * On success: sets pdfDoc, totalPages, enters edit mode.
 * On failure: shows error message, stays in preview mode.
 * Validates: Requirements 9.1, 9.6, 9.7
 */
async function loadPdf(): Promise<void> {
	if (!props.fileId || !props.onboardingId) return;

	isLoading.value = true;
	loadFailed.value = false;
	pdfDoc.value = null;
	currentPage.value = 1;
	totalPages.value = 0;
	elements.value = new Map();
	canvasWidth.value = 0;
	canvasHeight.value = 0;

	try {
		// Fetch the PDF via Axios so all auth headers (Authorization, X-App-Code,
		// X-Tenant-Id, etc.) are included automatically. PDF.js Worker fetches
		// URLs without any headers, which results in 401 for authenticated endpoints.
		const res = await previewOnboardingFile(props.onboardingId, props.fileId);

		// Convert Blob response to ArrayBuffer for PDF.js
		const blob = res instanceof Blob ? res : new Blob([res]);
		const arrayBuffer = await blob.arrayBuffer();

		const pdfjsLib = await loadPdfJs();

		// Use { data } instead of { url } so PDF.js loads from memory,
		// bypassing the Worker's inability to attach auth headers (Requirement 9.1).
		const loadingTask = pdfjsLib.getDocument({ data: new Uint8Array(arrayBuffer) });

		const doc: PDFDocumentProxy = await loadingTask.promise;

		// markRaw prevents Vue from ever making this object reactive,
		// preserving private field access inside PDF.js internals.
		pdfDoc.value = markRaw(doc);
		totalPages.value = doc.numPages;
		mode.value = 'edit';
	} catch (err: unknown) {
		// PDF.js rejects with a PasswordException for encrypted/password-protected PDFs.
		// Any load failure triggers the same error UX (Requirement 9.6).
		loadFailed.value = true;
		mode.value = 'preview';
		console.error('[DocumentSigningDialog] PDF load failed:', err);
	} finally {
		isLoading.value = false;
	}
}

// ========================= Dialog Open/Close =========================

/**
 * Watch `visible` — when it becomes true, reset state and load the PDF.
 * When it becomes false, clean up the pdfDoc to free memory.
 */
watch(
	() => props.visible,
	async (newVisible) => {
		if (newVisible) {
			// Reset all state before loading
			mode.value = 'preview';
			signedFileData.value = null;
			isSaving.value = false;
			addSignatureVisible.value = false;
			await loadPdf();
		} else {
			// Clean up PDF resources when dialog closes
			if (pdfDoc.value) {
				pdfDoc.value.destroy();
				pdfDoc.value = null;
			}
		}
	}
);

/**
 * Closes the dialog by emitting the v-model update.
 * If there are unsaved elements, shows a confirmation first (Requirement 10.4).
 */
async function handleClose(): Promise<void> {
	if (hasElements.value && mode.value === 'edit') {
		try {
			await ElMessageBox.confirm(
				'Placed signature and date elements will be lost. Close anyway?',
				'Confirm Close',
				{
					confirmButtonText: 'Close',
					cancelButtonText: 'Cancel',
					type: 'warning',
				}
			);
			// User confirmed — clear elements and close (Requirement 10.5)
			elements.value = new Map();
			emit('update:visible', false);
		} catch {
			// User cancelled — stay in edit mode
		}
	} else {
		emit('update:visible', false);
	}
}

// ========================= Page Navigation =========================

/**
 * Navigate to the previous page.
 * Validates: Requirement 9.4
 */
function handlePrevPage(): void {
	if (currentPage.value > 1) {
		currentPage.value -= 1;
	}
}

/**
 * Navigate to the next page.
 * Validates: Requirement 9.4
 */
function handleNextPage(): void {
	if (currentPage.value < totalPages.value) {
		currentPage.value += 1;
	}
}

/**
 * Navigate to a specific page via thumbnail click.
 * PageThumbnails emits 1-based page numbers.
 * Validates: Requirement 9.3
 */
function handlePageChanged(pageNum: number): void {
	if (pageNum >= 1 && pageNum <= totalPages.value) {
		currentPage.value = pageNum;
	}
}

// ========================= Zoom Controls =========================

const SCALE_MIN = 50;
const SCALE_MAX = 200;
const SCALE_STEP = 10;

/**
 * Decrease zoom by one step.
 * Validates: Requirement 9.5
 */
function handleZoomOut(): void {
	scale.value = Math.max(SCALE_MIN, scale.value - SCALE_STEP);
}

/**
 * Increase zoom by one step.
 * Validates: Requirement 9.5
 */
function handleZoomIn(): void {
	scale.value = Math.min(SCALE_MAX, scale.value + SCALE_STEP);
}

/**
 * Clamp zoom input to the allowed range on blur/change.
 */
function handleScaleInput(value: number): void {
	scale.value = Math.min(SCALE_MAX, Math.max(SCALE_MIN, value || SCALE_MIN));
}

// ========================= PdfViewer Events =========================

/**
 * PdfViewer emits canvas dimensions after each render.
 * We store them so SigningOverlay can match the canvas exactly.
 */
function handleRendered({ width, height }: { width: number; height: number }): void {
	canvasWidth.value = width;
	canvasHeight.value = height;
}

/**
 * PdfViewer emits this when PDF.js fails to load the document.
 * Validates: Requirement 9.6
 */
function handleLoadFailed(_error: unknown): void {
	loadFailed.value = true;
	mode.value = 'preview';
	ElMessage.error('This PDF is encrypted and cannot be signed online.');
}

// ========================= SigningOverlay Events =========================

/**
 * Update element position in the elements Map.
 * Uses immutable update to preserve Vue reactivity.
 * Validates: Requirement 13.2
 */
function handleElementMoved(id: string, pos: { x: number; y: number }): void {
	const pageIndex = currentPage.value - 1;
	const pageElements = elements.value.get(pageIndex) ?? [];
	const updated = pageElements.map((el) => (el.id === id ? { ...el, ...pos } : el));
	const newMap = new Map(elements.value);
	newMap.set(pageIndex, updated);
	elements.value = newMap;
}

/**
 * Update element size in the elements Map.
 * Validates: Requirement 13.3
 */
function handleElementResized(id: string, size: { w: number; h: number }): void {
	const pageIndex = currentPage.value - 1;
	const pageElements = elements.value.get(pageIndex) ?? [];
	const updated = pageElements.map((el) =>
		el.id === id ? { ...el, width: size.w, height: size.h } : el
	);
	const newMap = new Map(elements.value);
	newMap.set(pageIndex, updated);
	elements.value = newMap;
}

/**
 * Remove an element from the current page.
 * Validates: Requirement 13.4
 */
function handleElementDeleted(id: string): void {
	const pageIndex = currentPage.value - 1;
	const pageElements = elements.value.get(pageIndex) ?? [];
	const updated = pageElements.filter((el) => el.id !== id);
	const newMap = new Map(elements.value);
	newMap.set(pageIndex, updated);
	elements.value = newMap;
}

// ========================= Add Signature Flow =========================

/**
 * Opens the Add Signature sub-dialog.
 * Enforces the 10-signature limit across all pages (Requirement 12.4).
 */
function handleAddSignature(): void {
	if (totalSignatureCount.value >= 10) {
		ElMessage.warning('Signature limit reached (10). Please remove some before adding more.');
		return;
	}
	addSignatureVisible.value = true;
}

/**
 * Places a signature element on the current page, centered in the canvas.
 * Validates: Requirements 11.3, 12.4
 */
function handleSignatureSelected(imageBase64: string): void {
	const pageIndex = currentPage.value - 1;

	const INIT_W = 150;
	const INIT_H = 60;
	const scaleRatio = scale.value / 100;

	// Convert canvas px dimensions back to PDF.js pt units
	const pageWidthPt = canvasWidth.value / scaleRatio;
	const pageHeightPt = canvasHeight.value / scaleRatio;

	// Center the element; clamp to 0 in case canvas hasn't rendered yet
	const x = Math.max(0, (pageWidthPt - INIT_W) / 2);
	const y = Math.max(0, (pageHeightPt - INIT_H) / 2);

	const newElement: PlacedElement = {
		id: generateId(),
		type: 'signature',
		pageIndex,
		x,
		y,
		width: INIT_W,
		height: INIT_H,
		imageBase64,
	};

	const pageElements = elements.value.get(pageIndex) ?? [];
	const newMap = new Map(elements.value);
	newMap.set(pageIndex, [...pageElements, newElement]);
	elements.value = newMap;
}

/**
 * Places a date element centered on the current page.
 * Enforces the 10-date limit across all pages (Requirement 12.3).
 * Validates: Requirements 12.1, 12.2, 12.3
 */
function handleAddDate(): void {
	if (totalDateCount.value >= 10) {
		ElMessage.warning(
			'Date element limit reached (10). Please remove some before adding more.'
		);
		return;
	}

	const pageIndex = currentPage.value - 1;

	const INIT_W = 100;
	const INIT_H = 20;
	const scaleRatio = scale.value / 100;

	// Convert canvas px dimensions back to PDF.js pt units
	const pageWidthPt = canvasWidth.value / scaleRatio;
	const pageHeightPt = canvasHeight.value / scaleRatio;

	// Center the element; clamp to 0 in case canvas hasn't rendered yet
	const x = Math.max(0, (pageWidthPt - INIT_W) / 2);
	const y = Math.max(0, (pageHeightPt - INIT_H) / 2);

	// Format today's date as MM/DD/YYYY (Requirement 12.2)
	const today = new Date();
	const mm = String(today.getMonth() + 1).padStart(2, '0');
	const dd = String(today.getDate()).padStart(2, '0');
	const yyyy = today.getFullYear();
	const dateText = `${mm}/${dd}/${yyyy}`;

	const newElement: PlacedElement = {
		id: generateId(),
		type: 'date',
		pageIndex,
		x,
		y,
		width: INIT_W,
		height: INIT_H,
		dateText,
	};

	const pageElements = elements.value.get(pageIndex) ?? [];
	const newMap = new Map(elements.value);
	newMap.set(pageIndex, [...pageElements, newElement]);
	elements.value = newMap;
}

/**
 * Initiates the confirm & sign flow (pdf-lib synthesis + upload).
 * Validates: Requirements 14.1, 14.2, 14.3, 14.4, 14.5, 14.6
 */
async function handleConfirmSignature(): Promise<void> {
	// Req 14.1 — warn the user that the document cannot be modified after signing
	try {
		await ElMessageBox.confirm(
			'Once confirmed, the document will be permanently signed and cannot be modified. Do you want to proceed?',
			'Confirm Signature',
			{
				confirmButtonText: 'Confirm',
				cancelButtonText: 'Cancel',
				type: 'warning',
			}
		);
	} catch {
		// User cancelled — stay in edit mode, do nothing
		return;
	}

	isSaving.value = true;

	try {
		// [DEBUG] Diagnostic logging for signing
		console.log('[Sign] fileUrl prop:', props.fileUrl);
		console.log('[Sign] fileId prop:', props.fileId);

		// Expand elements map to log per-page element coordinates
		const elementsDebug: Record<
			number,
			{ id: string; type: string; x: number; y: number; w: number; h: number }[]
		> = {};
		for (const [pageIdx, els] of elements.value) {
			elementsDebug[pageIdx] = els.map((el) => ({
				id: el.id,
				type: el.type,
				x: Math.round(el.x),
				y: Math.round(el.y),
				w: Math.round(el.width),
				h: Math.round(el.height),
			}));
		}
		console.log('[Sign] elements snapshot:', JSON.stringify(elementsDebug));

		// Req 14.2 — synthesize the PDF with all placed elements using pdf-lib
		const arrayBuffer = await synthesizePdf(props.fileUrl, elements.value);

		// Req 14.4 — build FormData with the signed PDF file and signing metadata
		const userStore = useUserStoreWithOut();
		const userInfo = userStore.getUserInfo;
		const signerName = userInfo.realName || userInfo.userName || '';

		// Derive the output file name from the original file name
		const baseName = props.fileName.endsWith('.pdf')
			? props.fileName.slice(0, -4)
			: props.fileName;
		const signedFileName = baseName ? `${baseName}_signed.pdf` : 'signed.pdf';

		const formData = new FormData();
		formData.append(
			'file',
			new File([arrayBuffer], signedFileName, { type: 'application/pdf' })
		);
		formData.append('signerName', signerName);
		formData.append('signedAt', new Date().toISOString());

		// Req 14.4 — POST to /ow/files/{fileId}/sign
		const response = await signDocument(String(props.fileId), formData);

		// Req 14.5 — transition to signed read-only mode on success.
		// signDocument returns the standard { code, data, msg } envelope;
		// the actual SignedFileResponse payload is in .data.
		const signedData = ((response as any)?.data ?? response) as SignedFileResponse;
		signedFileData.value = signedData;
		isSaving.value = false; // Reset saving state so the Close button is re-enabled in signed mode
		mode.value = 'signed';
		emit('refreshDocuments');
	} catch (err: unknown) {
		// Req 14.6 — retain edit mode with all placed elements intact on any failure
		isSaving.value = false;
		const message =
			err instanceof Error ? err.message : 'Failed to sign the document. Please try again.';
		ElMessage.error(message);
	}
}

/**
 * Download the signed file.
 *
 * Uses the backend preview proxy to fetch PDF bytes rather than linking
 * directly to the OSS/S3 URL. Direct cross-origin URLs ignore the HTML
 * `download` attribute, so the browser falls back to its own
 * Content-Disposition handling — which on S3 (staging) means inline preview
 * instead of a download prompt.
 *
 * Fetching through the backend returns raw bytes as a Blob, from which we
 * create a same-origin blob: URL. The `download` attribute is then honoured
 * by every browser, giving consistent download behaviour across environments.
 *
 * Validates: Requirement 16.2
 */
async function handleDownload(): Promise<void> {
	const signedFileId = signedFileData.value?.signedFileId;
	if (!signedFileId || !props.onboardingId) return;

	try {
		const res = await previewOnboardingFile(props.onboardingId, signedFileId);
		const blob = res instanceof Blob ? res : new Blob([res], { type: 'application/pdf' });
		const blobUrl = URL.createObjectURL(blob);

		const fileName = signedFileData.value?.fileName || 'signed.pdf';
		const a = document.createElement('a');
		a.href = blobUrl;
		a.download = fileName;
		document.body.appendChild(a);
		a.click();
		document.body.removeChild(a);

		// Revoke the blob URL shortly after to free memory
		setTimeout(() => URL.revokeObjectURL(blobUrl), 10_000);
	} catch {
		ElMessage.error('Failed to download the signed file. Please try again.');
	}
}

/**
 * Print the signed file.
 * Fetches the PDF bytes and creates a blob: URL to bypass the OSS
 * Content-Disposition: attachment header that would otherwise cause
 * window.open() to download the file instead of displaying it.
 * Validates: Requirement 16.3
 */
async function handlePrint(): Promise<void> {
	const signedFileId = signedFileData.value?.signedFileId;
	if (!signedFileId || !props.onboardingId) return;

	try {
		// Use the backend preview API instead of fetching the OSS URL directly.
		// Direct fetch of the OSS URL fails with CORS because the bucket does not
		// allow cross-origin requests from the app origin.
		// previewOnboardingFile goes through the backend proxy with auth headers,
		// returning the raw PDF bytes as a Blob — no CORS issue.
		const res = await previewOnboardingFile(props.onboardingId, signedFileId);
		const blob = res instanceof Blob ? res : new Blob([res], { type: 'application/pdf' });
		const blobUrl = URL.createObjectURL(blob);

		const win = window.open(blobUrl, '_blank');
		if (!win) {
			URL.revokeObjectURL(blobUrl);
			ElMessage.warning(
				'Pop-up was blocked. Please allow pop-ups for this site and try again.'
			);
			return;
		}

		// Wait for the PDF to render in the new tab, then trigger print
		win.addEventListener('load', () => {
			win.print();
			setTimeout(() => URL.revokeObjectURL(blobUrl), 10_000);
		});

		// Fallback: trigger after 2 s if the load event doesn't fire
		setTimeout(() => {
			try {
				win.print();
			} catch {
				// Window may have been closed; ignore
			}
			setTimeout(() => URL.revokeObjectURL(blobUrl), 10_000);
		}, 2000);
	} catch (err) {
		ElMessage.error(
			'Failed to prepare the PDF for printing. Please try downloading it instead.'
		);
	}
}
</script>

<template>
	<!-- Full-screen overlay dialog.
         Using a manual full-screen overlay instead of el-dialog fullscreen prop
         to ensure we own the layout entirely (toolbar + sidebar + main area). -->
	<Teleport to="body">
		<div v-if="props.visible" class="signing-dialog-overlay">
			<!-- ── Top toolbar ───────────────────────────────────────────────── -->
			<SigningToolbar
				:mode="mode === 'signed' ? 'signed' : 'edit'"
				:has-elements="hasElements"
				:is-saving="isSaving"
				:file-name="props.fileName"
				:add-date-disabled="totalDateCount >= 10"
				@add-signature="handleAddSignature"
				@add-date="handleAddDate"
				@confirm-signature="handleConfirmSignature"
				@close="handleClose"
				@download="handleDownload"
				@print="handlePrint"
			/>

			<!-- ── Body: sidebar + main content ──────────────────────────────── -->
			<div class="signing-dialog-body">
				<!-- Left sidebar: page thumbnails (180px fixed) -->
				<PageThumbnails
					:pdf-doc="pdfDoc"
					:current-page="currentPage"
					@page-changed="handlePageChanged"
				/>

				<!-- Main content: controls + PDF viewer area -->
				<div class="signing-dialog-main">
					<!-- Page navigation & zoom controls bar -->
					<div class="signing-dialog-controls">
						<!-- Page navigation -->
						<div class="controls-group">
							<el-button
								size="small"
								:disabled="currentPage <= 1 || isLoading"
								@click="handlePrevPage"
							>
								&lt;
							</el-button>

							<!-- "Page X / Y" display (Requirement 9.4) -->
							<span class="page-info">
								Page {{ currentPage }} / {{ totalPages || '—' }}
							</span>

							<el-button
								size="small"
								:disabled="currentPage >= totalPages || isLoading"
								@click="handleNextPage"
							>
								&gt;
							</el-button>
						</div>

						<!-- Zoom controls (Requirement 9.5) -->
						<div class="controls-group">
							<el-button
								size="small"
								:disabled="scale <= 50 || isLoading"
								@click="handleZoomOut"
							>
								−
							</el-button>

							<el-input-number
								:model-value="scale"
								size="small"
								:min="50"
								:max="200"
								:step="10"
								:controls="false"
								class="zoom-input"
								@change="handleScaleInput"
							/>
							<span class="zoom-label">%</span>

							<el-button
								size="small"
								:disabled="scale >= 200 || isLoading"
								@click="handleZoomIn"
							>
								+
							</el-button>
						</div>
					</div>

					<!-- PDF viewer viewport — scrollable -->
					<div class="signing-dialog-viewport">
						<!-- Loading spinner while PDF is loading -->
						<div v-if="isLoading" class="signing-dialog-state">
							<el-icon class="is-loading" :size="32">
								<svg viewBox="0 0 1024 1024" xmlns="http://www.w3.org/2000/svg">
									<path
										d="M512 64a448 448 0 1 1 0 896A448 448 0 0 1 512 64zm0 128a320 320 0 1 0 0 640A320 320 0 0 0 512 192z"
										fill="#e5e7eb"
									/>
									<path
										d="M512 64a448 448 0 0 1 448 448h-128a320 320 0 0 0-320-320V64z"
										fill="#7c3aed"
									/>
								</svg>
							</el-icon>
							<p class="state-text">Loading PDF…</p>
						</div>

						<!-- Error state: encrypted or unreadable PDF (Requirement 9.6) -->
						<div
							v-else-if="loadFailed"
							class="signing-dialog-state signing-dialog-state--error"
						>
							<p class="state-text state-text--error">
								This PDF is encrypted and cannot be signed online.
							</p>
						</div>

						<!-- Normal PDF render: PdfViewer + SigningOverlay stacked -->
						<div v-else-if="pdfDoc" class="pdf-canvas-wrapper">
							<!-- PdfViewer renders the canvas -->
							<PdfViewer
								:pdf-doc="pdfDoc"
								:page-number="currentPage"
								:scale="scale"
								@rendered="handleRendered"
								@load-failed="handleLoadFailed"
							/>

							<!-- SigningOverlay sits on top of the canvas, same dimensions -->
							<SigningOverlay
								class="signing-overlay-absolute"
								:elements="currentPageElements"
								:canvas-width="canvasWidth"
								:canvas-height="canvasHeight"
								:scale="scale"
								@element-moved="handleElementMoved"
								@element-resized="handleElementResized"
								@element-deleted="handleElementDeleted"
							/>
						</div>
					</div>
				</div>
			</div>

			<!-- ── Add Signature sub-dialog (signing flow version) ─────────── -->
			<AddSignatureDialog
				v-model:visible="addSignatureVisible"
				@signature-selected="handleSignatureSelected"
			/>
		</div>
	</Teleport>
</template>

<style scoped lang="scss">
// Full-screen overlay that covers the entire viewport
.signing-dialog-overlay {
	position: fixed;
	inset: 0;
	z-index: 2000; // Above el-dialog default (2000) to sit on top of everything
	display: flex;
	flex-direction: column;
	background: #111827;
	width: 100%;
	height: 100%;
	overflow: hidden;
}

// Body below the toolbar
.signing-dialog-body {
	display: flex;
	flex: 1;
	overflow: hidden;
	min-height: 0;
}

// Main content area to the right of the thumbnails sidebar
.signing-dialog-main {
	flex: 1;
	display: flex;
	flex-direction: column;
	overflow: hidden;
	min-width: 0;
}

// ── Controls bar: page nav + zoom ────────────────────────────────────────────
.signing-dialog-controls {
	display: flex;
	align-items: center;
	gap: 16px;
	padding: 6px 16px;
	background: #1e1e1e;
	border-bottom: 1px solid #333;
	flex-shrink: 0;
}

.controls-group {
	display: flex;
	align-items: center;
	gap: 6px;
}

.page-info {
	font-size: 13px;
	color: #e5e7eb;
	white-space: nowrap;
	min-width: 80px;
	text-align: center;
}

.zoom-input {
	width: 58px;

	// Override el-input-number to make it compact
	:deep(.el-input__inner) {
		text-align: center;
		padding: 0 4px;
	}
}

.zoom-label {
	font-size: 13px;
	color: #e5e7eb;
}

// ── Viewport ─────────────────────────────────────────────────────────────────
.signing-dialog-viewport {
	flex: 1;
	overflow: auto;
	display: flex;
	justify-content: center;
	align-items: flex-start;
	padding: 24px;
	background: #374151;
	min-height: 0;
}

// State messages (loading / error)
.signing-dialog-state {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	gap: 12px;
	height: 100%;
	min-height: 200px;
}

.state-text {
	font-size: 14px;
	color: #9ca3af;
	margin: 0;
}

.state-text--error {
	color: #f87171;
	font-size: 15px;
}

// ── PDF canvas + overlay stack ────────────────────────────────────────────────

// Wraps PdfViewer and SigningOverlay so they stack exactly
.pdf-canvas-wrapper {
	position: relative;
	display: inline-block;
	line-height: 0;
	// Drop shadow to separate the PDF from the grey background
	box-shadow: 0 4px 24px rgba(0, 0, 0, 0.5);
}

// SigningOverlay must be positioned on top of the canvas absolutely
.signing-overlay-absolute {
	position: absolute;
	top: 0;
	left: 0;
}
</style>
