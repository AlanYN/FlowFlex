<template>
	<div class="ai-file-analyzer">
		<el-tooltip
			content="Supported: TXT, PDF, DOCX, XLSX, CSV, MD, JSON"
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
						:loading="isSendingToAI"
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
import { ElMessage } from 'element-plus';
import { Document, Check, Close, Loading } from '@element-plus/icons-vue';
import * as XLSX from 'xlsx-js-style';
import { streamAIChatMessageNative, type AIChatMessage } from '@/apis/ai/workflow';
import { getDefaultAIModel, type AIModelConfig } from '@/apis/ai/config';

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

// Props & Emits
const emit = defineEmits<{
	fileAnalyzed: [content: string, fileName: string];
	analysisComplete: [result: any];
	streamChunk: [chunk: string];
	analysisStarted: [fileName: string];
}>();

// Reactive Data
const uploadRef = ref();
const selectedFile = ref<File | null>(null);
const showProcessingDialog = ref(false);
const isProcessing = ref(false);
const isSendingToAI = ref(false);
const currentStep = ref(0);
const extractedContent = ref('');
const currentAIModel = ref<AIModelConfig | null>(null);

// File Types Configuration
const acceptedFileTypes = '.txt,.pdf,.docx,.xlsx,.csv,.md,.json';
const maxFileSize = 10 * 1024 * 1024; // 10MB

// Processing Steps
const processingSteps = ref([
	{ title: 'Reading file', description: 'Loading file content...', error: '' },
	{ title: 'Extracting text', description: 'Converting to readable text...', error: '' },
	{ title: 'Preparing content', description: 'Formatting for AI analysis...', error: '' },
]);

// Computed
const supportedFormats = computed(() => {
	return ['txt', 'pdf', 'docx', 'xlsx', 'csv', 'md', 'json'];
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

	selectedFile.value = rawFile;
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
	currentStep.value = 0;
	extractedContent.value = '';

	// Reset error states
	processingSteps.value.forEach((step) => (step.error = ''));

	try {
		// Step 1: Reading file
		await delay(500);
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
				content = await readPDFFile(file);
				break;
			case 'docx':
				content = await readDocxFile(file);
				break;
			default:
				throw new Error(`Unsupported file type: ${extension}`);
		}

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

		// Emit file analyzed event
		emit('fileAnalyzed', extractedContent.value, file.name);
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

const sendToAI = async () => {
	if (!extractedContent.value) {
		ElMessage.error('No content to send');
		return;
	}

	isSendingToAI.value = true;

	// 立即关闭弹窗
	showProcessingDialog.value = false;

	// 通知父组件分析开始
	emit('analysisStarted', selectedFile.value?.name || 'Unknown file');

	try {
		// Get current AI model configuration
		if (!currentAIModel.value) {
			const modelResponse = await getDefaultAIModel();
			if (modelResponse.success && modelResponse.data) {
				currentAIModel.value = modelResponse.data;
			}
		}

		// Prepare the message for AI analysis
		const analysisPrompt = `Please analyze the following file content and provide insights, summary, or answer any questions about it:

File: ${selectedFile.value?.name}
Content:
${extractedContent.value}

Please provide a comprehensive analysis of this content.`;

		const messages: AIChatMessage[] = [
			{
				role: 'system',
				content:
					'You are an AI assistant specialized in analyzing and understanding various types of documents and files. Provide detailed, helpful analysis of the content provided.',
				timestamp: new Date().toISOString(),
			},
			{
				role: 'user',
				content: analysisPrompt,
				timestamp: new Date().toISOString(),
			},
		];

		// Prepare chat request
		const chatRequest = {
			messages,
			context: 'file_analysis',
			mode: 'general' as const,
			...(currentAIModel.value && {
				modelId: currentAIModel.value.id.toString(),
				modelProvider: currentAIModel.value.provider,
				modelName: currentAIModel.value.modelName,
			}),
		};

		let analysisResult = '';

		// Call streaming API
		await streamAIChatMessageNative(
			chatRequest,
			(chunk: string) => {
				analysisResult += chunk;
				// 实时发送流式响应给父组件
				emit('streamChunk', chunk);
			},
			(data: any) => {
				console.log('File analysis completed:', data);

				// Emit analysis complete event
				emit('analysisComplete', {
					fileName: selectedFile.value?.name,
					originalContent: extractedContent.value,
					analysis: analysisResult,
					metadata: {
						fileSize: selectedFile.value?.size,
						fileType: selectedFile.value?.type,
						analysisTimestamp: new Date().toISOString(),
					},
				});

				ElMessage.success('File analysis completed successfully!');
			},
			(error: any) => {
				console.error('File analysis failed:', error);
				ElMessage.error('Failed to analyze file: ' + (error.message || 'Unknown error'));
			}
		);
	} catch (error) {
		console.error('Send to AI error:', error);
		ElMessage.error(
			'Failed to send content to AI: ' +
				(error instanceof Error ? error.message : 'Unknown error')
		);
	} finally {
		isSendingToAI.value = false;
	}
};

const cancelProcessing = () => {
	showProcessingDialog.value = false;
	selectedFile.value = null;
	extractedContent.value = '';
	currentStep.value = 0;
	isProcessing.value = false;
	isSendingToAI.value = false;
};

const delay = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));
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
</style>
