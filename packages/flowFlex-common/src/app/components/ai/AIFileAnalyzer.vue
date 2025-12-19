<template>
	<div class="ai-file-analyzer">
		<el-tooltip
			content="Supported: TXT, PDF, DOCX, XLSX, CSV, MD, JSON, JPG, PNG, GIF, BMP, WebP"
			placement="top"
			effect="dark"
		>
			<el-upload
				ref="uploadRef"
				:auto-upload="false"
				:show-file-list="false"
				:accept="acceptedFileTypes"
				:on-change="handleFileSelect"
				:before-upload="beforeUpload"
				class="file-upload-icon"
			>
				<el-button link size="large" class="attachment-btn">
					<svg
						xmlns="http://www.w3.org/2000/svg"
						xmlns:xlink="http://www.w3.org/1999/xlink"
						aria-hidden="true"
						role="img"
						name="Clip"
						class="attachment-icon"
						width="1em"
						height="1em"
						viewBox="0 0 1024 1024"
					>
						<path
							d="M239.08352 319.0784a188.17024 188.17024 0 0 1 376.29952 0v0.16384l4.62848 347.62752v0.08192a112.64 112.64 0 0 1-156.0576 105.63584 112.55808 112.55808 0 0 1-68.97664-105.39008V315.14624a36.864 36.864 0 1 1 73.728 0v352.99328a38.83008 38.83008 0 1 0 77.57824 0v-0.16384l-4.58752-347.58656V320.3072a114.4832 114.4832 0 0 0-228.88448-0.4096l4.5056 347.58656a190.13632 190.13632 0 1 0 380.3136 0l0.4096-334.39744a36.864 36.864 0 1 1 73.728 0.08192l-0.4096 334.31552a263.90528 263.90528 0 0 1-450.43712 186.61376 263.86432 263.86432 0 0 1-77.29152-186.368l-4.54656-347.50464v-1.10592z"
							fill="currentColor"
						/>
					</svg>
				</el-button>
			</el-upload>
		</el-tooltip>

		<!-- File Processing Dialog -->
		<el-dialog
			v-model="showProcessingDialog"
			title="File Analysis"
			width="500px"
			:close-on-click-modal="false"
			:close-on-press-escape="false"
		>
			<div class="processing-content">
				<div class="file-info">
					<el-icon class="file-icon"><Document /></el-icon>
					<div class="file-details">
						<p class="file-name">{{ selectedFile?.name }}</p>
						<p class="file-size">{{ formatFileSize(selectedFile?.size || 0) }}</p>
					</div>
				</div>

				<!-- Image Preview with Rotation Controls -->
				<div v-if="isImageFile && imagePreviewUrl" class="image-preview-section">
					<div class="image-preview-container">
						<img :src="imagePreviewUrl" alt="Preview" class="image-preview" />
					</div>
					<div class="rotation-controls">
						<el-button
							:icon="RefreshLeft"
							circle
							size="small"
							@click="rotateImageCounterClockwise"
							:disabled="isRotating"
							title="Rotate Left 90°"
						/>
						<span class="rotation-label">{{ currentRotation }}°</span>
						<el-button
							:icon="RefreshRight"
							circle
							size="small"
							@click="rotateImageClockwise"
							:disabled="isRotating"
							title="Rotate Right 90°"
						/>
						<el-button
							v-if="needsReanalyze"
							type="primary"
							size="small"
							@click="reanalyzeImage"
							:loading="isReanalyzing"
							:disabled="isReanalyzing"
							class="reanalyze-btn"
						>
							Re-analyze
						</el-button>
					</div>
					<p class="rotation-hint">
						Rotate image if text appears sideways or upside down
					</p>
				</div>

				<div class="processing-steps">
					<div
						v-for="(step, index) in processingSteps"
						:key="index"
						:class="[
							'step-item',
							{
								active: currentStep === index,
								completed: currentStep > index,
								error: step.error,
							},
						]"
					>
						<div class="step-icon">
							<el-icon v-if="step.error"><Close /></el-icon>
							<el-icon v-else-if="currentStep > index"><Check /></el-icon>
							<el-icon v-else-if="currentStep === index" class="loading">
								<Loading />
							</el-icon>
							<span v-else class="step-number">{{ index + 1 }}</span>
						</div>
						<div class="step-content">
							<p class="step-title">{{ step.title }}</p>
							<p v-if="step.description" class="step-description">
								{{ step.description }}
							</p>
							<p v-if="step.error" class="step-error">{{ step.error }}</p>
						</div>
					</div>
				</div>

				<div v-if="extractedContent" class="content-preview">
					<h4>Content Preview:</h4>
					<div class="preview-text">
						{{ extractedContent.substring(0, 300) }}
						<span v-if="extractedContent.length > 300">...</span>
					</div>
					<p class="content-stats">
						Total characters: {{ extractedContent.length }} | Words:
						{{ extractedContent.split(/\s+/).length }}
					</p>
				</div>
			</div>

			<template #footer>
				<div class="dialog-footer">
					<el-button @click="cancelProcessing" :disabled="isProcessing">Cancel</el-button>
					<el-button
						type="primary"
						@click="sendToAI"
						:disabled="!extractedContent || isProcessing"
					>
						Send to AI Chat
					</el-button>
				</div>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import {
	Document,
	Check,
	Close,
	Loading,
	RefreshLeft,
	RefreshRight,
} from '@element-plus/icons-vue';
import * as XLSX from 'xlsx-js-style';
import {
	getOCRService,
	getPDFDetector,
	getContentMerger,
	getImageProcessor,
	isLargePDF,
	LARGE_PDF_PAGE_THRESHOLD,
	type ContentBlock,
	type RotationAngle,
} from './utils';

// External library loaders (CDN-based to avoid local dependency issues)
const PDF_JS_VERSION = '3.11.174';
const MAMMOTH_VERSION = '1.6.0';

let pdfJsLoadingPromise: Promise<any> | null = null;
let mammothLoadingPromise: Promise<any> | null = null;

const loadScriptOnce = (src: string): Promise<void> => {
	return new Promise((resolve, reject) => {
		const existing = Array.from(document.getElementsByTagName('script')).find(
			(s) => s.src === src
		);
		if (existing) {
			if ((existing as any)._loaded) {
				resolve();
			} else {
				existing.addEventListener('load', () => resolve());
				existing.addEventListener('error', () =>
					reject(new Error(`Failed to load ${src}`))
				);
			}
			return;
		}
		const script = document.createElement('script');
		script.src = src;
		script.async = true;
		script.addEventListener('load', () => {
			(script as any)._loaded = true;
			resolve();
		});
		script.addEventListener('error', () => reject(new Error(`Failed to load ${src}`)));
		document.head.appendChild(script);
	});
};

const loadPdfJs = async () => {
	if ((window as any).pdfjsLib) return (window as any).pdfjsLib;
	if (!pdfJsLoadingPromise) {
		pdfJsLoadingPromise = (async () => {
			const url = `https://cdnjs.cloudflare.com/ajax/libs/pdf.js/${PDF_JS_VERSION}/pdf.min.js`;
			await loadScriptOnce(url);
			const pdfjsLib = (window as any).pdfjsLib;
			if (!pdfjsLib) throw new Error('pdfjsLib not available after loading');
			pdfjsLib.GlobalWorkerOptions.workerSrc = `https://cdnjs.cloudflare.com/ajax/libs/pdf.js/${PDF_JS_VERSION}/pdf.worker.min.js`;
			return pdfjsLib;
		})();
	}
	return pdfJsLoadingPromise;
};

const loadMammoth = async () => {
	if ((window as any).mammoth) return (window as any).mammoth;
	if (!mammothLoadingPromise) {
		mammothLoadingPromise = (async () => {
			const url = `https://cdnjs.cloudflare.com/ajax/libs/mammoth/${MAMMOTH_VERSION}/mammoth.browser.min.js`;
			await loadScriptOnce(url);
			const mammoth = (window as any).mammoth;
			if (!mammoth) throw new Error('mammoth not available after loading');
			return mammoth;
		})();
	}
	return mammothLoadingPromise;
};

// Emits
const emit = defineEmits<{
	fileAnalyzed: [content: string, fileName: string];
}>();

// Reactive Data
const uploadRef = ref();
const selectedFile = ref<File | null>(null);
const showProcessingDialog = ref(false);
const isProcessing = ref(false);
const currentStep = ref(0);
const extractedContent = ref('');

// Image rotation state
const isImageFile = ref(false);
const imagePreviewUrl = ref('');
const currentRotation = ref<RotationAngle>(0);
const rotatedImageBlob = ref<Blob | null>(null);
const lastAnalyzedRotation = ref<RotationAngle>(0);
const isRotating = ref(false);
const isReanalyzing = ref(false);
const shouldCancelProcessing = ref(false);

// Computed: Check if re-analysis is needed after rotation
const needsReanalyze = computed(
	() => isImageFile.value && currentRotation.value !== lastAnalyzedRotation.value
);

// File Types Configuration
const acceptedFileTypes = '.txt,.pdf,.docx,.xlsx,.csv,.md,.json,.jpg,.jpeg,.png,.gif,.bmp,.webp';
const maxFileSize = 10 * 1024 * 1024; // 10MB

// Image file extensions
const imageExtensions = ['jpg', 'jpeg', 'png', 'gif', 'bmp', 'webp'];

// Processing Steps
const processingSteps = ref([
	{ title: 'Reading file', description: 'Loading file content...', error: '' },
	{ title: 'Extracting text', description: 'Converting to readable text...', error: '' },
	{ title: 'Preparing content', description: 'Formatting for AI analysis...', error: '' },
]);

// Computed
const supportedFormats = computed(() => {
	return ['txt', 'pdf', 'docx', 'xlsx', 'csv', 'md', 'json', ...imageExtensions];
});

// Methods
const handleFileSelect = async (file: any) => {
	const rawFile = file.raw || file;

	if (!isValidFileType(rawFile)) {
		ElMessage.error('Unsupported file type. Please select a supported format.');
		return;
	}

	if (rawFile.size > maxFileSize) {
		ElMessage.error('File size exceeds 10MB limit.');
		return;
	}

	// Reset rotation state
	resetRotationState();

	selectedFile.value = rawFile;
	isImageFile.value = checkIsImageFile(rawFile);

	// Create preview for image files
	if (isImageFile.value) {
		createImagePreview(rawFile);
	}

	showProcessingDialog.value = true;
	await processFile(rawFile);
};

const beforeUpload = () => {
	return false; // Prevent auto upload
};

const isValidFileType = (file: File): boolean => {
	const extension = file.name.toLowerCase().split('.').pop();
	return supportedFormats.value.includes(extension || '');
};

const formatFileSize = (bytes: number): string => {
	if (bytes === 0) return '0 Bytes';
	const k = 1024;
	const sizes = ['Bytes', 'KB', 'MB', 'GB'];
	const i = Math.floor(Math.log(bytes) / Math.log(k));
	return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
};

const processFile = async (file: File) => {
	isProcessing.value = true;
	shouldCancelProcessing.value = false;
	currentStep.value = 0;
	extractedContent.value = '';

	// Reset error states
	processingSteps.value.forEach((step) => (step.error = ''));

	try {
		// Step 1: Reading file
		await delay(500);
		if (shouldCancelProcessing.value) return;
		currentStep.value = 1;

		// Step 2: Extract text based on file type
		const extension = file.name.toLowerCase().split('.').pop();
		let content = '';

		switch (extension) {
			case 'txt':
			case 'md':
			case 'json':
				content = await readTextFile(file);
				break;
			case 'csv':
				content = await readCSVFile(file);
				break;
			case 'xlsx':
				content = await readExcelFile(file);
				break;
			case 'pdf':
				content = await readPDFFileWithOCR(file);
				break;
			case 'docx':
				content = await readDocxFile(file);
				break;
			case 'jpg':
			case 'jpeg':
			case 'png':
			case 'gif':
			case 'bmp':
			case 'webp':
				content = await readImageFile(file);
				break;
			default:
				throw new Error(`Unsupported file type: ${extension}`);
		}

		// Check if cancelled during processing
		if (shouldCancelProcessing.value) return;

		await delay(500);
		currentStep.value = 2;

		// Step 3: Prepare content
		if (!content.trim()) {
			throw new Error('No readable content found in the file');
		}

		// Clean and format content
		extractedContent.value = content.trim();
		await delay(500);
		currentStep.value = 3;

		// Content extracted successfully, wait for user to click "Send to AI Chat"
		// Do not emit fileAnalyzed here - only emit when user clicks send button
	} catch (error) {
		console.error('File processing error:', error);
		processingSteps.value[currentStep.value].error =
			error instanceof Error ? error.message : 'Processing failed';
		ElMessage.error(
			'Failed to process file: ' + (error instanceof Error ? error.message : 'Unknown error')
		);
	} finally {
		isProcessing.value = false;
	}
};

const readTextFile = (file: File): Promise<string> => {
	return new Promise((resolve, reject) => {
		const reader = new FileReader();
		reader.onload = (e) => resolve((e.target?.result as string) || '');
		reader.onerror = () => reject(new Error('Failed to read text file'));
		reader.readAsText(file);
	});
};

const readCSVFile = async (file: File): Promise<string> => {
	const text = await readTextFile(file);
	const lines = text.split('\n');
	return lines.map((line) => line.replace(/,/g, ' | ')).join('\n');
};

const readExcelFile = (file: File): Promise<string> => {
	return new Promise((resolve, reject) => {
		const reader = new FileReader();
		reader.onload = (e) => {
			try {
				const data = new Uint8Array(e.target?.result as ArrayBuffer);
				const workbook = XLSX.read(data, { type: 'array' });

				let content = '';
				workbook.SheetNames.forEach((sheetName) => {
					const worksheet = workbook.Sheets[sheetName];
					const jsonData = XLSX.utils.sheet_to_json(worksheet, { header: 1 });

					content += `Sheet: ${sheetName}\n`;
					jsonData.forEach((row: any) => {
						if (Array.isArray(row) && row.length > 0) {
							content += row.join(' | ') + '\n';
						}
					});
					content += '\n';
				});

				resolve(content);
			} catch (error) {
				reject(new Error('Failed to parse Excel file'));
			}
		};
		reader.onerror = () => reject(new Error('Failed to read Excel file'));
		reader.readAsArrayBuffer(file);
	});
};

const readPDFFile = async (file: File): Promise<string> => {
	try {
		const pdfjsLib = await loadPdfJs();
		const arrayBuffer = await new Promise<ArrayBuffer>((resolve, reject) => {
			const reader = new FileReader();
			reader.onload = () => resolve(reader.result as ArrayBuffer);
			reader.onerror = () => reject(new Error('Failed to read PDF file'));
			reader.readAsArrayBuffer(file);
		});
		const pdf = await pdfjsLib.getDocument({ data: arrayBuffer }).promise;
		let fullText = '';
		for (let pageNum = 1; pageNum <= pdf.numPages; pageNum++) {
			const page = await pdf.getPage(pageNum);
			const textContent = await page.getTextContent();
			const pageText = textContent.items.map((item: any) => item.str).join(' ');
			fullText += `Page ${pageNum}:\n${pageText}\n\n`;
		}
		return fullText.trim();
	} catch (error) {
		console.error('PDF parsing error:', error);
		throw new Error('Failed to parse PDF file. Please ensure the file is not corrupted.');
	}
};

// Enhanced PDF reading with OCR support
const readPDFFileWithOCR = async (file: File): Promise<string> => {
	try {
		const originalBuffer = await new Promise<ArrayBuffer>((resolve, reject) => {
			const reader = new FileReader();
			reader.onload = () => resolve(reader.result as ArrayBuffer);
			reader.onerror = () => reject(new Error('Failed to read PDF file'));
			reader.readAsArrayBuffer(file);
		});

		// Create a copy of the ArrayBuffer to avoid detached buffer issues
		// PDF.js transfers ownership of ArrayBuffer, so we need separate copies for each operation
		const pdfDetector = getPDFDetector();
		const detectionResult = await pdfDetector.detectPDFType(originalBuffer.slice(0));

		// Check for large PDF warning
		if (isLargePDF(detectionResult.pageCount)) {
			try {
				await ElMessageBox.confirm(
					`This PDF has ${detectionResult.pageCount} pages (exceeds ${LARGE_PDF_PAGE_THRESHOLD}). Processing may take a while. Continue?`,
					'Large PDF Warning',
					{
						confirmButtonText: 'Continue',
						cancelButtonText: 'Cancel',
						type: 'warning',
					}
				);
			} catch {
				throw new Error('Processing cancelled by user');
			}
		}

		// Update processing step description
		processingSteps.value[1].description = `Detected: ${detectionResult.type} PDF (${detectionResult.pageCount} pages)`;

		const contentBlocks: ContentBlock[] = [];

		// Extract text layer content for text pages
		if (detectionResult.textPages.length > 0) {
			const textContent = await readPDFFile(file);
			contentBlocks.push({
				content: textContent,
				source: 'text-layer',
				pageNumber: 1,
			});
		}

		// Perform OCR for image-based pages
		if (pdfDetector.shouldTriggerOCR(detectionResult)) {
			const pagesToOCR = pdfDetector.getPagesNeedingOCR(detectionResult);

			if (pagesToOCR.length > 0) {
				processingSteps.value[1].description = `Running OCR on ${pagesToOCR.length} pages...`;

				const ocrService = getOCRService();
				await ocrService.initialize();

				for (const pageNum of pagesToOCR) {
					processingSteps.value[1].description = `OCR: Page ${pageNum}/${detectionResult.pageCount}`;

					// Use a fresh copy of the buffer for each page extraction
					const pageImage = await pdfDetector.extractPageAsImage(
						originalBuffer.slice(0),
						pageNum
					);
					const ocrResult = await ocrService.recognizeImage(pageImage);

					if (ocrResult.text.trim()) {
						contentBlocks.push({
							content: `Page ${pageNum} (OCR):\n${ocrResult.text}`,
							source: 'ocr',
							pageNumber: pageNum,
							confidence: ocrResult.confidence,
						});
					}
				}
			}
		}

		// Merge content
		const contentMerger = getContentMerger();
		const mergedContent = contentMerger.merge(contentBlocks);

		processingSteps.value[1].description = `Extracted ${mergedContent.statistics.totalCharacters} characters`;

		return mergedContent.fullText || 'No readable content found in the PDF';
	} catch (error) {
		console.error('PDF processing error:', error);
		if (error instanceof Error && error.message === 'Processing cancelled by user') {
			throw error;
		}
		// Fallback to basic PDF reading
		console.log('Falling back to basic PDF reading');
		return readPDFFile(file);
	}
};

// Read image file using OCR
const readImageFile = async (file: File): Promise<string> => {
	try {
		processingSteps.value[1].description = 'Performing OCR on image...';

		const ocrService = getOCRService();
		await ocrService.initialize();

		// Use rotated image if available, otherwise use original file
		const imageToProcess = rotatedImageBlob.value || file;

		const result = await ocrService.recognizeImage(imageToProcess, (progress) => {
			processingSteps.value[1].description = `OCR: ${progress.status} (${Math.round(
				progress.progress * 100
			)}%)`;
		});

		if (!result.text.trim()) {
			throw new Error('No text found in the image');
		}

		const rotationInfo =
			currentRotation.value > 0 ? ` (rotated ${currentRotation.value}°)` : '';
		processingSteps.value[1].description = `Extracted ${
			result.text.length
		} characters (confidence: ${Math.round(result.confidence)}%)${rotationInfo}`;

		// Update last analyzed rotation
		lastAnalyzedRotation.value = currentRotation.value;

		return result.text;
	} catch (error) {
		console.error('Image OCR error:', error);
		throw new Error(
			'Failed to extract text from image. Please ensure the image contains readable text.'
		);
	}
};

const readDocxFile = async (file: File): Promise<string> => {
	try {
		const mammoth = await loadMammoth();
		const arrayBuffer = await new Promise<ArrayBuffer>((resolve, reject) => {
			const reader = new FileReader();
			reader.onload = () => resolve(reader.result as ArrayBuffer);
			reader.onerror = () => reject(new Error('Failed to read DOCX file'));
			reader.readAsArrayBuffer(file);
		});
		const result = await mammoth.extractRawText({ arrayBuffer });
		if (result.messages && result.messages.length > 0) {
			console.warn('DOCX parsing warnings:', result.messages);
		}
		return result.value || '';
	} catch (error) {
		console.error('DOCX parsing error:', error);
		throw new Error('Failed to parse DOCX file. Please ensure the file is not corrupted.');
	}
};

const sendToAI = () => {
	if (!extractedContent.value) {
		ElMessage.error('No content to send');
		return;
	}

	// Close dialog
	showProcessingDialog.value = false;

	// Emit fileAnalyzed event - parent component will handle the AI chat
	emit('fileAnalyzed', extractedContent.value, selectedFile.value?.name || 'Unknown file');
};

const cancelProcessing = () => {
	showProcessingDialog.value = false;
	selectedFile.value = null;
	extractedContent.value = '';
	currentStep.value = 0;
	isProcessing.value = false;
	resetRotationState();
};

const delay = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

// Check if file is an image
const checkIsImageFile = (file: File): boolean => {
	const extension = file.name.toLowerCase().split('.').pop();
	return imageExtensions.includes(extension || '');
};

// Create image preview URL
const createImagePreview = (file: File | Blob) => {
	if (imagePreviewUrl.value) {
		URL.revokeObjectURL(imagePreviewUrl.value);
	}
	imagePreviewUrl.value = URL.createObjectURL(file);
};

// Rotate image by 90 degrees clockwise
const rotateImageClockwise = async () => {
	if (!selectedFile.value || isRotating.value) return;

	isRotating.value = true;
	const newRotation = ((currentRotation.value + 90) % 360) as RotationAngle;
	currentRotation.value = newRotation;

	try {
		const imageProcessor = getImageProcessor();
		const image = await imageProcessor.loadImage(selectedFile.value);
		const rotatedBlob = await imageProcessor.rotateImage(image, newRotation);
		rotatedImageBlob.value = rotatedBlob;
		createImagePreview(rotatedBlob);
	} catch (error) {
		console.error('Failed to rotate image:', error);
		ElMessage.error('Failed to rotate image');
	} finally {
		isRotating.value = false;
	}
};

// Rotate image by 90 degrees counter-clockwise
const rotateImageCounterClockwise = async () => {
	if (!selectedFile.value || isRotating.value) return;

	isRotating.value = true;
	const newRotation = ((currentRotation.value - 90 + 360) % 360) as RotationAngle;
	currentRotation.value = newRotation;

	try {
		const imageProcessor = getImageProcessor();
		const image = await imageProcessor.loadImage(selectedFile.value);
		const rotatedBlob = await imageProcessor.rotateImage(image, newRotation);
		rotatedImageBlob.value = rotatedBlob;
		createImagePreview(rotatedBlob);
	} catch (error) {
		console.error('Failed to rotate image:', error);
		ElMessage.error('Failed to rotate image');
	} finally {
		isRotating.value = false;
	}
};

// Reset rotation state
const resetRotationState = () => {
	if (imagePreviewUrl.value) {
		URL.revokeObjectURL(imagePreviewUrl.value);
		imagePreviewUrl.value = '';
	}
	currentRotation.value = 0;
	rotatedImageBlob.value = null;
	isImageFile.value = false;
	lastAnalyzedRotation.value = 0;
};

// Re-analyze image after rotation
const reanalyzeImage = async () => {
	if (!selectedFile.value || isReanalyzing.value) return;

	// Cancel any ongoing processing
	if (isProcessing.value) {
		shouldCancelProcessing.value = true;
		// Terminate OCR worker to stop current processing
		const ocrService = getOCRService();
		await ocrService.terminate();
		isProcessing.value = false;
	}

	isReanalyzing.value = true;
	currentStep.value = 1;
	extractedContent.value = '';

	// Reset error states
	processingSteps.value.forEach((step) => (step.error = ''));

	try {
		const content = await readImageFile(selectedFile.value);

		await delay(300);
		currentStep.value = 2;

		if (!content.trim()) {
			throw new Error('No readable content found in the image');
		}

		extractedContent.value = content.trim();
		lastAnalyzedRotation.value = currentRotation.value;

		await delay(300);
		currentStep.value = 3;

		// Content extracted successfully, wait for user to click "Send to AI Chat"
	} catch (error) {
		console.error('Re-analyze error:', error);
		processingSteps.value[currentStep.value].error =
			error instanceof Error ? error.message : 'Re-analysis failed';
		ElMessage.error(
			'Failed to re-analyze: ' + (error instanceof Error ? error.message : 'Unknown error')
		);
	} finally {
		isReanalyzing.value = false;
		shouldCancelProcessing.value = false;
	}
};
</script>

<style scoped lang="scss">
.ai-file-analyzer {
	display: inline-block;
}

.file-upload-icon {
	display: inline-block;
}

.attachment-btn {
	font-size: 18px !important;
	color: var(--el-text-color-secondary) !important;
	padding: 4px 8px !important;
	margin: 0 !important;
	border: none !important;
	background: transparent !important;
	transition: all 0.2s ease !important;
	min-height: auto !important;
	height: auto !important;
}

.attachment-btn:hover {
	color: var(--el-color-primary) !important;
	background: var(--el-fill-color-light) !important;
	@apply rounded-xl;
}

.attachment-icon {
	font-size: 18px;
	color: currentColor;
}

.processing-content {
	display: flex;
	flex-direction: column;
	gap: 1.5rem;
}

.file-info {
	display: flex;
	align-items: center;
	gap: 1rem;
	padding: 1rem;
	background: var(--el-fill-color-blank);
	@apply rounded-xl;
}

.file-icon {
	font-size: 32px;
	color: var(--el-color-primary);
}

.file-details {
	flex: 1;
}

.file-name {
	margin: 0 0 0.25rem 0;
	font-size: 16px;
	font-weight: 600;
	color: var(--el-text-color-regular);
}

.file-size {
	margin: 0;
	font-size: 14px;
	color: var(--el-text-color-secondary);
}

.processing-steps {
	display: flex;
	flex-direction: column;
	gap: 1rem;
}

.step-item {
	display: flex;
	align-items: flex-start;
	gap: 1rem;
	padding: 0.75rem;
	transition: all 0.2s ease;
	@apply rounded-xl;
}

.step-item.active {
	background: var(--el-color-primary-light-9);
	border: 1px solid var(--el-color-primary-light-8);
}

.step-item.completed {
	background: var(--el-color-success-light-9);
	border: 1px solid var(--el-color-success-light-7);
}

.step-item.error {
	background: var(--el-color-danger-light-9);
	border: 1px solid var(--el-color-danger-light-7);
}

.step-icon {
	width: 32px;
	height: 32px;
	border-radius: 50%;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 16px;
	flex-shrink: 0;
}

.step-item .step-icon {
	background: var(--el-fill-color-light);
	color: var(--el-text-color-secondary);
}

.step-item.active .step-icon {
	background: var(--el-color-primary);
	color: var(--el-color-white);
}

.step-item.completed .step-icon {
	background: var(--el-color-success);
	color: var(--el-color-white);
}

.step-item.error .step-icon {
	background: var(--el-color-danger);
	color: var(--el-color-white);
}

.step-number {
	font-size: 14px;
	font-weight: 600;
}

.loading {
	animation: spin 1s linear infinite;
}

@keyframes spin {
	from {
		transform: rotate(0deg);
	}
	to {
		transform: rotate(360deg);
	}
}

.step-content {
	flex: 1;
}

.step-title {
	margin: 0 0 0.25rem 0;
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-regular);
}

.step-description {
	margin: 0;
	font-size: 13px;
	color: var(--el-text-color-secondary);
}

.step-error {
	margin: 0.25rem 0 0 0;
	font-size: 13px;
	color: var(--el-color-danger);
	font-weight: 500;
}

.content-preview {
	padding: 1rem;
	background: var(--el-fill-color-blank);
	border: 1px solid var(--el-border-color-light);
	@apply rounded-xl;
}

.content-preview h4 {
	margin: 0 0 0.75rem 0;
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-regular);
}

.preview-text {
	padding: 0.75rem;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color);
	font-size: 13px;
	line-height: 1.5;
	color: var(--el-text-color-regular);
	white-space: pre-wrap;
	word-break: break-word;
	max-height: 200px;
	overflow-y: auto;
	@apply rounded-xl;
}

.content-stats {
	margin: 0.75rem 0 0 0;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.dialog-footer {
	display: flex;
	justify-content: flex-end;
	gap: 0.5rem;
}

/* Image Preview and Rotation Styles */
.image-preview-section {
	display: flex;
	flex-direction: column;
	align-items: center;
	gap: 0.75rem;
	padding: 1rem;
	background: var(--el-fill-color-blank);
	border: 1px solid var(--el-border-color-light);
	@apply rounded-xl;
}

.image-preview-container {
	max-width: 100%;
	max-height: 200px;
	overflow: hidden;
	display: flex;
	align-items: center;
	justify-content: center;
	background: var(--el-fill-color-lighter);
	@apply rounded-lg;
}

.image-preview {
	max-width: 100%;
	max-height: 200px;
	object-fit: contain;
}

.rotation-controls {
	display: flex;
	align-items: center;
	gap: 1rem;
}

.rotation-label {
	font-size: 14px;
	font-weight: 500;
	color: var(--el-text-color-regular);
	min-width: 40px;
	text-align: center;
}

.rotation-hint {
	margin: 0;
	font-size: 12px;
	color: var(--el-text-color-secondary);
	text-align: center;
}

.reanalyze-btn {
	margin-left: 0.5rem;
}
</style>
