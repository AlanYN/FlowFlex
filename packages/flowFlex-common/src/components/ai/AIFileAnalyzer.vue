<template>
	<div class="ai-file-analyzer">
		<el-upload
			ref="uploadRef"
			:auto-upload="false"
			:show-file-list="false"
			:accept="acceptedFileTypes"
			:on-change="handleFileSelect"
			:before-upload="beforeUpload"
			class="file-upload-area"
		>
			<div class="upload-content">
				<div class="upload-icon">
					<el-icon><Document /></el-icon>
				</div>
				<div class="upload-text">
					<p class="upload-title">AI Analyze Files</p>
					<p class="supported-formats">
						Supported: TXT, PDF, DOCX, XLSX, CSV, MD, JSON
					</p>
				</div>
			</div>
		</el-upload>

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
						:class="['step-item', { 
							active: currentStep === index, 
							completed: currentStep > index,
							error: step.error 
						}]"
					>
						<div class="step-icon">
							<el-icon v-if="step.error"><Close /></el-icon>
							<el-icon v-else-if="currentStep > index"><Check /></el-icon>
							<el-icon v-else-if="currentStep === index" class="loading"><Loading /></el-icon>
							<span v-else class="step-number">{{ index + 1 }}</span>
						</div>
						<div class="step-content">
							<p class="step-title">{{ step.title }}</p>
							<p v-if="step.description" class="step-description">{{ step.description }}</p>
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
						Total characters: {{ extractedContent.length }} | 
						Words: {{ extractedContent.split(/\s+/).length }}
					</p>
				</div>
			</div>

			<template #footer>
				<div class="dialog-footer">
					<el-button @click="cancelProcessing" :disabled="isProcessing">
						Cancel
					</el-button>
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
import { streamAIChatMessageNative, type AIChatMessage } from '../../app/apis/ai/workflow';
import { getDefaultAIModel, type AIModelConfig } from '../../app/apis/ai/config';

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
	{ title: 'Preparing content', description: 'Formatting for AI analysis...', error: '' }
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
	processingSteps.value.forEach(step => step.error = '');

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
		processingSteps.value[currentStep.value].error = error instanceof Error ? error.message : 'Processing failed';
		ElMessage.error('Failed to process file: ' + (error instanceof Error ? error.message : 'Unknown error'));
	} finally {
		isProcessing.value = false;
	}
};

const readTextFile = (file: File): Promise<string> => {
	return new Promise((resolve, reject) => {
		const reader = new FileReader();
		reader.onload = (e) => resolve(e.target?.result as string || '');
		reader.onerror = () => reject(new Error('Failed to read text file'));
		reader.readAsText(file);
	});
};

const readCSVFile = async (file: File): Promise<string> => {
	const text = await readTextFile(file);
	const lines = text.split('\n');
	return lines.map(line => line.replace(/,/g, ' | ')).join('\n');
};

const readExcelFile = (file: File): Promise<string> => {
	return new Promise((resolve, reject) => {
		const reader = new FileReader();
		reader.onload = (e) => {
			try {
				const data = new Uint8Array(e.target?.result as ArrayBuffer);
				const workbook = XLSX.read(data, { type: 'array' });
				
				let content = '';
				workbook.SheetNames.forEach(sheetName => {
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
	// For PDF files, we'll need a PDF parsing library
	// For now, return a placeholder message
	throw new Error('PDF parsing is not yet implemented. Please use other file formats.');
};

const readDocxFile = async (file: File): Promise<string> => {
	// For DOCX files, we'll need a DOCX parsing library
	// For now, return a placeholder message
	throw new Error('DOCX parsing is not yet implemented. Please use other file formats.');
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
				content: 'You are an AI assistant specialized in analyzing and understanding various types of documents and files. Provide detailed, helpful analysis of the content provided.',
				timestamp: new Date().toISOString()
			},
			{
				role: 'user',
				content: analysisPrompt,
				timestamp: new Date().toISOString()
			}
		];

		// Prepare chat request
		const chatRequest = {
			messages,
			context: 'file_analysis',
			mode: 'general' as const,
			...(currentAIModel.value && {
				modelId: currentAIModel.value.id.toString(),
				modelProvider: currentAIModel.value.provider,
				modelName: currentAIModel.value.modelName
			})
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
						analysisTimestamp: new Date().toISOString()
					}
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
		ElMessage.error('Failed to send content to AI: ' + (error instanceof Error ? error.message : 'Unknown error'));
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

const delay = (ms: number) => new Promise(resolve => setTimeout(resolve, ms));
</script>

<style scoped>
.ai-file-analyzer {
	width: 100%;
}

.file-upload-area {
	width: 100%;
}

.upload-content {
	display: flex;
	align-items: center;
	gap: 0.75rem;
	padding: 0.75rem;
	border: 2px dashed #d1d5db;
	border-radius: 8px;
	background: #f9fafb;
	transition: all 0.2s ease;
	cursor: pointer;
}

.upload-content:hover {
	border-color: #4f46e5;
	background: #f8fafc;
}

.upload-icon {
	width: 36px;
	height: 36px;
	border-radius: 50%;
	background: linear-gradient(135deg, #4f46e5 0%, #4338ca 100%);
	display: flex;
	align-items: center;
	justify-content: center;
	color: white;
	font-size: 18px;
	flex-shrink: 0;
}

.upload-text {
	flex: 1;
}

.upload-title {
	margin: 0 0 0.125rem 0;
	font-size: 14px;
	font-weight: 600;
	color: #374151;
}

.upload-subtitle {
	margin: 0 0 0.25rem 0;
	font-size: 12px;
	color: #6b7280;
}

.supported-formats {
	margin: 0;
	font-size: 11px;
	color: #9ca3af;
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
	background: #f8fafc;
	border-radius: 8px;
}

.file-icon {
	font-size: 32px;
	color: #4f46e5;
}

.file-details {
	flex: 1;
}

.file-name {
	margin: 0 0 0.25rem 0;
	font-size: 16px;
	font-weight: 600;
	color: #374151;
}

.file-size {
	margin: 0;
	font-size: 14px;
	color: #6b7280;
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
	border-radius: 8px;
	transition: all 0.2s ease;
}

.step-item.active {
	background: #eff6ff;
	border: 1px solid #dbeafe;
}

.step-item.completed {
	background: #f0fdf4;
	border: 1px solid #bbf7d0;
}

.step-item.error {
	background: #fef2f2;
	border: 1px solid #fecaca;
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
	background: #f3f4f6;
	color: #6b7280;
}

.step-item.active .step-icon {
	background: #3b82f6;
	color: white;
}

.step-item.completed .step-icon {
	background: #10b981;
	color: white;
}

.step-item.error .step-icon {
	background: #ef4444;
	color: white;
}

.step-number {
	font-size: 14px;
	font-weight: 600;
}

.loading {
	animation: spin 1s linear infinite;
}

@keyframes spin {
	from { transform: rotate(0deg); }
	to { transform: rotate(360deg); }
}

.step-content {
	flex: 1;
}

.step-title {
	margin: 0 0 0.25rem 0;
	font-size: 14px;
	font-weight: 600;
	color: #374151;
}

.step-description {
	margin: 0;
	font-size: 13px;
	color: #6b7280;
}

.step-error {
	margin: 0.25rem 0 0 0;
	font-size: 13px;
	color: #ef4444;
	font-weight: 500;
}

.content-preview {
	padding: 1rem;
	background: #f8fafc;
	border-radius: 8px;
	border: 1px solid #e5e7eb;
}

.content-preview h4 {
	margin: 0 0 0.75rem 0;
	font-size: 14px;
	font-weight: 600;
	color: #374151;
}

.preview-text {
	padding: 0.75rem;
	background: white;
	border-radius: 6px;
	border: 1px solid #d1d5db;
	font-family: 'Monaco', 'Menlo', 'Ubuntu Mono', monospace;
	font-size: 13px;
	line-height: 1.5;
	color: #374151;
	white-space: pre-wrap;
	word-break: break-word;
	max-height: 200px;
	overflow-y: auto;
}

.content-stats {
	margin: 0.75rem 0 0 0;
	font-size: 12px;
	color: #6b7280;
}

.dialog-footer {
	display: flex;
	justify-content: flex-end;
	gap: 0.5rem;
}
</style>