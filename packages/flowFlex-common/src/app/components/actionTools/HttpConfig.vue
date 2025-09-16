<template>
	<div class="space-y-6 import-dialog">
		<!-- Import Section -->
		<div class="import-section flex justify-between items-center mb-4">
			<h4 class="font-medium text-gray-700 dark:text-gray-300">HTTP Configuration</h4>
			<el-button type="primary" @click="showImportDialog" :disabled="disabled">
				Import
			</el-button>
		</div>

		<!-- Import Dialog -->
		<el-dialog
			v-model="importDialogVisible"
			title="Import HTTP Configuration"
			width="600px"
			:before-close="handleImportDialogClose"
			append-to-body
		>
			<PrototypeTabs
				v-model="activeImportTab"
				:tabs="importTabsConfig"
				type="default"
				size="default"
			>
				<!-- cURL Tab -->
				<TabPane value="curl">
					<div class="curl-import-content space-y-4">
						<div
							class="bg-blue-50 dark:bg-gray-800 p-4 rounded-lg border border-blue-200 dark:border-gray-700"
						>
							<div class="flex items-start space-x-3">
								<div class="flex-shrink-0">
									<Icon
										icon="heroicons:information-circle"
										class="w-5 h-5 text-blue-600 dark:text-blue-400 mt-0.5"
									/>
								</div>
								<div>
									<h4
										class="text-sm font-medium text-blue-900 dark:text-blue-100 mb-1"
									>
										How to use cURL Import
									</h4>
									<p class="text-sm text-blue-700 dark:text-blue-300">
										Paste your cURL command below. The system will automatically
										parse and populate all form fields including URL, method,
										headers, and body data.
									</p>
								</div>
							</div>
						</div>
						<variable-auto-complete
							v-model="curlInput"
							type="textarea"
							:rows="10"
							placeholder="Paste your cURL command here...&#10;&#10;Example:&#10;curl -X POST 'https://api.example.com/users' \&#10;  -H 'Content-Type: application/json' \&#10;  -H 'Authorization: Bearer your-token' \&#10;  -d '{&#10;    &quot;name&quot;: &quot;John Doe&quot;,&#10;    &quot;email&quot;: &quot;john@example.com&quot;&#10;  }'"
							class="font-mono text-sm"
						/>

						<!-- 错误信息显示区域 -->
						<div v-if="importError" class="import-error-message">
							<div class="flex items-start space-x-3">
								<div class="flex-shrink-0">
									<Icon
										icon="heroicons:exclamation-triangle"
										class="w-5 h-5 text-red-600 dark:text-red-400 mt-0.5"
									/>
								</div>
								<div class="flex-1">
									<h4
										class="text-sm font-medium text-red-900 dark:text-red-100 mb-1"
									>
										Import Failed
									</h4>
									<p class="text-sm text-red-700 dark:text-red-300">
										{{ importError }}
									</p>
								</div>
							</div>
						</div>
					</div>
				</TabPane>

				<!-- AI Tab -->
				<TabPane value="ai">
					<div class="ai-import-content">
						<!-- AI Chat Interface -->
						<div class="ai-chat-container">
							<!-- Chat Messages -->
							<div class="ai-chat-messages" ref="chatMessagesRef">
								<div
									v-for="(message, index) in aiChatMessages"
									:key="index"
									class="ai-message"
									:class="{
										'user-message': message.role === 'user',
										'assistant-message': message.role === 'assistant',
									}"
								>
									<div class="message-content">
										<div class="message-text">{{ message.content }}</div>
										<div
											v-if="
												message.role === 'assistant' && message.httpConfig
											"
											class="generated-config"
										>
											<div class="config-preview">
												<h5 class="config-title">
													Generated HTTP Configuration:
												</h5>
												<div class="config-details">
													<!-- Action Name Input -->
													<div class="config-item">
														<span class="config-label">
															Action Name:
														</span>
														<div class="config-value-input">
															<el-input
																v-model="
																	message.httpConfig.actionName
																"
																placeholder="Enter custom action name"
																size="small"
																class="action-name-input"
																@input="
																	updateActionName(
																		message,
																		$event
																	)
																"
															/>
														</div>
													</div>
													<div class="config-item">
														<span class="config-label">Method:</span>
														<span class="config-value">
															{{ message.httpConfig.method }}
														</span>
													</div>
													<div class="config-item">
														<span class="config-label">URL:</span>
														<span class="config-value">
															{{ message.httpConfig.url }}
														</span>
													</div>
													<div
														v-if="
															Object.keys(
																message.httpConfig.headers || {}
															).length > 0
														"
														class="config-item"
													>
														<span class="config-label">Headers:</span>
														<div class="config-headers">
															<div
																v-for="(value, key) in message
																	.httpConfig.headers"
																:key="key"
																class="header-item"
															>
																{{ key }}: {{ value }}
															</div>
														</div>
													</div>
												</div>
												<el-button
													type="primary"
													size="small"
													@click="
														applyGeneratedConfig(message.httpConfig)
													"
													class="apply-config-btn"
												>
													Apply Configuration
												</el-button>
											</div>
										</div>
									</div>
								</div>

								<!-- Loading indicator - only show when no messages are being streamed -->
								<div
									v-if="aiGenerating && aiChatMessages.length === 0"
									class="ai-message assistant-message"
								>
									<div class="message-content">
										<div class="message-text">
											<div class="typing-indicator">
												<span></span>
												<span></span>
												<span></span>
											</div>
											Generating HTTP configuration...
										</div>
									</div>
								</div>
							</div>

							<!-- Input Area -->
							<div class="ai-input-area">
								<div class="ai-input-with-button">
									<div class="ai-input-container">
										<el-input
											v-model="aiCurrentInput"
											type="textarea"
											:rows="3"
											placeholder="Describe your API requirements"
											@keydown="handleAIKeydown"
											class="ai-chat-input"
										/>
										<div class="input-bottom-actions">
											<div class="ai-model-selector-bottom">
												<el-select
													v-model="currentAIModel"
													placeholder="Select AI Model"
													size="small"
													class="model-select"
													style="width: 180px"
													value-key="id"
													@change="handleModelChange"
												>
													<el-option
														v-for="model in availableModels"
														:key="model.id"
														:label="`${model.provider.toLowerCase()} ${
															model.modelName
														}`"
														:value="model"
														:disabled="!model.isAvailable"
													>
														<div class="model-option">
															<div class="model-info">
																<span class="model-display">
																	{{
																		model.provider.toLowerCase()
																	}}
																	{{ model.modelName }}
																</span>
															</div>
															<div class="model-status">
																<span
																	class="status-dot"
																	:class="{
																		online: model.isAvailable,
																		offline: !model.isAvailable,
																	}"
																></span>
															</div>
														</div>
													</el-option>
												</el-select>
											</div>
										</div>
										<div class="input-right-actions">
											<!-- File Upload for Analysis -->
											<el-tooltip
												content="Supported: TXT, PDF, DOCX, XLSX, CSV, MD, JSON"
												placement="top"
												effect="dark"
											>
												<el-upload
													ref="fileUploadRef"
													:show-file-list="false"
													:before-upload="handleFileUpload"
													accept=".txt,.pdf,.docx,.xlsx,.csv,.md,.json"
													class="file-upload-btn"
												>
													<el-button
														type="text"
														size="small"
														class="upload-button"
														:disabled="aiGenerating"
													>
														<el-icon :size="18"><Paperclip /></el-icon>
													</el-button>
												</el-upload>
											</el-tooltip>
											<el-button
												type="primary"
												@click="sendAIMessage"
												:disabled="!aiCurrentInput.trim() && !uploadedFile"
												:loading="aiGenerating"
												size="small"
												class="ai-send-button"
												circle
											>
												<svg
													xmlns="http://www.w3.org/2000/svg"
													viewBox="0 0 1024 1024"
													class="send-icon"
												>
													<path
														fill="currentColor"
														d="m249.6 417.088 319.744 43.072 39.168 310.272L845.12 178.88 249.6 417.088zm-129.024 47.168a32 32 0 0 1-7.68-61.44l777.792-311.04a32 32 0 0 1 41.6 41.6l-310.336 775.68a32 32 0 0 1-61.44-7.808L512 516.992l-391.424-52.736z"
													/>
												</svg>
											</el-button>
										</div>
									</div>
								</div>

								<!-- Uploaded File Display -->
								<div v-if="uploadedFile" class="uploaded-file-display">
									<div class="file-info">
										<el-icon><Document /></el-icon>
										<span class="file-name">{{ uploadedFile.name }}</span>
										<el-button
											type="text"
											size="small"
											@click="removeUploadedFile"
											class="remove-file-btn"
										>
											<el-icon><Close /></el-icon>
										</el-button>
									</div>
								</div>
							</div>
						</div>
					</div>
				</TabPane>
			</PrototypeTabs>

			<template #footer>
				<div class="text-right">
					<el-button @click="handleImportDialogClose">Cancel</el-button>
					<el-button
						v-if="activeImportTab === 'curl'"
						type="primary"
						@click="handleCurlImport"
						:disabled="!curlInput.trim() || importLoading"
						:loading="importLoading"
					>
						Import
					</el-button>
				</div>
			</template>
		</el-dialog>

		<!-- HTTP Configuration Form -->
		<div class="http-form">
			<el-form :model="formConfig" label-width="120px" class="space-y-6" label-position="top">
				<el-form-item label="Request URL" required class="request-url-input">
					<el-input
						:model-value="formConfig.url"
						@update:model-value="setUrl"
						placeholder="Enter URL, type '/' to insert variables"
						class="w-full"
						:disabled="disabled"
					>
						<template #prepend>
							<el-select
								:model-value="formConfig.method"
								@update:model-value="setMethod"
								style="width: 115px"
								:disabled="disabled"
							>
								<el-option label="GET" value="GET" />
								<el-option label="POST" value="POST" />
								<el-option label="PUT" value="PUT" />
								<el-option label="DELETE" value="DELETE" />
								<el-option label="PATCH" value="PATCH" />
							</el-select>
						</template>
					</el-input>
					<div class="text-xs text-gray-500 mt-1">
						Use variables like &#123;&#123;onboarding.id&#125;&#125; or
						&#123;&#123;stage.name&#125;&#125; in the URL
					</div>
				</el-form-item>

				<!-- Headers Section -->
				<el-form-item label="HEADERS">
					<div class="params-section-enhanced">
						<div class="params-header-enhanced">
							<div class="param-col-key">Key</div>
							<div class="param-col-value">Value</div>
							<div class="param-actions-enhanced"></div>
						</div>
						<div class="params-body-enhanced">
							<div
								v-for="(header, index) in formConfig.headersList"
								:key="index"
								class="param-row-enhanced"
							>
								<div class="param-key-container">
									<variable-auto-complete
										v-model="header.key"
										placeholder="Header key"
										class="param-input-enhanced"
										@update:model-value="updateHeaderKey(index, $event)"
										:disabled="disabled"
									/>
								</div>
								<div class="param-value-container">
									<variable-auto-complete
										v-if="!header.focused"
										v-model="header.value"
										placeholder="Header value"
										class="param-input-enhanced"
										@update:model-value="updateHeaderValue(index, $event)"
										@focus="setHeaderFocused(index, true)"
										:disabled="disabled"
									/>
									<el-input
										v-else
										v-model="header.value"
										type="textarea"
										placeholder="Header value"
										class="param-textarea-auto-height"
										@update:model-value="updateHeaderValue(index, $event)"
										@blur="setHeaderFocused(index, false)"
										:disabled="disabled"
										:autosize="{ minRows: 1, maxRows: 10 }"
										:ref="
											(el) => {
												if (el) headerTextareaRefs[index] = el;
											}
										"
									/>
								</div>
								<div class="param-delete-container">
									<el-button
										type="danger"
										text
										@click="removeHeader(index)"
										class="param-delete-enhanced"
										:disabled="disabled"
									>
										<el-icon><Delete /></el-icon>
									</el-button>
								</div>
							</div>
						</div>
					</div>
				</el-form-item>

				<!-- Params Section -->
				<el-form-item label="PARAMS">
					<div class="params-section-enhanced">
						<div class="params-header-enhanced">
							<div class="param-col-key">Key</div>
							<div class="param-col-value">Value</div>
							<div class="param-actions-enhanced"></div>
						</div>
						<div class="params-body-enhanced">
							<div
								v-for="(param, index) in formConfig.paramsList"
								:key="index"
								class="param-row-enhanced"
							>
								<div class="param-key-container">
									<variable-auto-complete
										v-model="param.key"
										placeholder="Parameter key"
										class="param-input-enhanced"
										@update:model-value="updateParamKey(index, $event)"
										:disabled="disabled"
									/>
								</div>
								<div class="param-value-container">
									<variable-auto-complete
										v-if="!param.focused"
										v-model="param.value"
										placeholder="Parameter value"
										class="param-input-enhanced"
										@update:model-value="updateParamValue(index, $event)"
										@focus="setParamFocused(index, true)"
										:disabled="disabled"
									/>
									<el-input
										v-else
										v-model="param.value"
										type="textarea"
										placeholder="Parameter value"
										class="param-textarea-auto-height"
										@update:model-value="updateParamValue(index, $event)"
										@blur="setParamFocused(index, false)"
										:disabled="disabled"
										:autosize="{ minRows: 1, maxRows: 10 }"
										:ref="
											(el) => {
												if (el) paramTextareaRefs[index] = el;
											}
										"
									/>
								</div>
								<div class="param-delete-container">
									<el-button
										type="danger"
										text
										@click="removeParam(index)"
										class="param-delete-enhanced"
										:disabled="disabled"
									>
										<el-icon><Delete /></el-icon>
									</el-button>
								</div>
							</div>
						</div>
					</div>
				</el-form-item>

				<!-- Body Section -->
				<el-form-item label="BODY">
					<div class="body-section">
						<!-- Body Type Selection -->
						<el-radio-group
							:model-value="formConfig.bodyType"
							@update:model-value="setBodyType"
							class="body-type-group"
							:disabled="disabled"
						>
							<el-radio value="none">none</el-radio>
							<el-radio value="form-data">form-data</el-radio>
							<el-radio value="x-www-form-urlencoded">x-www-form-urlencoded</el-radio>
							<el-radio value="raw">raw</el-radio>
						</el-radio-group>

						<!-- Body Content based on type -->
						<div class="body-content">
							<!-- None - No content -->
							<div v-if="formConfig.bodyType === 'none'" class="body-none">
								<p class="text-gray-500 text-sm">This request has no body</p>
							</div>

							<!-- Form Data -->
							<div
								v-else-if="formConfig.bodyType === 'form-data'"
								class="params-section-enhanced"
							>
								<div class="params-header-enhanced">
									<div class="param-col-key">Key</div>
									<div class="param-col-value">Value</div>
									<div class="param-actions-enhanced"></div>
								</div>
								<div class="params-body-enhanced">
									<div
										v-for="(item, index) in formConfig.formDataList"
										:key="index"
										class="param-row-enhanced"
									>
										<div class="param-key-container">
											<variable-auto-complete
												v-model="item.key"
												placeholder="Form data key"
												class="param-input-enhanced"
												@update:model-value="
													updateFormDataKey(index, $event)
												"
												:disabled="disabled"
											/>
										</div>
										<div class="param-value-container">
											<variable-auto-complete
												v-if="!item.focused"
												v-model="item.value"
												placeholder="Form data value"
												class="param-input-enhanced"
												@update:model-value="
													updateFormDataValue(index, $event)
												"
												@focus="setFormDataFocused(index, true)"
												:disabled="disabled"
											/>
											<el-input
												v-else
												v-model="item.value"
												type="textarea"
												placeholder="Form data value"
												class="param-textarea-auto-height"
												@update:model-value="
													updateFormDataValue(index, $event)
												"
												@blur="setFormDataFocused(index, false)"
												:disabled="disabled"
												:autosize="{ minRows: 1, maxRows: 10 }"
												:ref="
													(el) => {
														if (el) formDataTextareaRefs[index] = el;
													}
												"
											/>
										</div>
										<div class="param-delete-container">
											<el-button
												type="danger"
												text
												@click="removeFormData(index)"
												class="param-delete-enhanced"
												v-if="formConfig.formDataList.length > 1"
												:disabled="disabled"
											>
												<el-icon><Delete /></el-icon>
											</el-button>
										</div>
									</div>
								</div>
							</div>

							<!-- URL Encoded -->
							<div
								v-else-if="formConfig.bodyType === 'x-www-form-urlencoded'"
								class="params-section-enhanced"
							>
								<div class="params-header-enhanced">
									<div class="param-col-key">Key</div>
									<div class="param-col-value">Value</div>
									<div class="param-actions-enhanced"></div>
								</div>
								<div class="params-body-enhanced">
									<div
										v-for="(item, index) in formConfig.urlEncodedList"
										:key="index"
										class="param-row-enhanced"
									>
										<div class="param-key-container">
											<variable-auto-complete
												v-model="item.key"
												placeholder="URL encoded key"
												class="param-input-enhanced"
												@update:model-value="
													updateUrlEncodedKey(index, $event)
												"
												:disabled="disabled"
											/>
										</div>
										<div class="param-value-container">
											<variable-auto-complete
												v-if="!item.focused"
												v-model="item.value"
												placeholder="URL encoded value"
												class="param-input-enhanced"
												@update:model-value="
													updateUrlEncodedValue(index, $event)
												"
												@focus="setUrlEncodedFocused(index, true)"
												:disabled="disabled"
											/>
											<el-input
												v-else
												v-model="item.value"
												type="textarea"
												placeholder="URL encoded value"
												class="param-textarea-auto-height"
												@update:model-value="
													updateUrlEncodedValue(index, $event)
												"
												@blur="setUrlEncodedFocused(index, false)"
												:disabled="disabled"
												:autosize="{ minRows: 1, maxRows: 10 }"
												:ref="
													(el) => {
														if (el) urlEncodedTextareaRefs[index] = el;
													}
												"
											/>
										</div>
										<div class="param-delete-container">
											<el-button
												type="danger"
												text
												@click="removeUrlEncoded(index)"
												class="param-delete-enhanced"
												v-if="formConfig.urlEncodedList.length > 1"
												:disabled="disabled"
											>
												<el-icon><Delete /></el-icon>
											</el-button>
										</div>
									</div>
								</div>
							</div>

							<!-- Raw - with format selection -->
							<div v-else-if="formConfig.bodyType === 'raw'" class="raw-section">
								<div class="raw-header">
									<div class="raw-format-controls">
										<el-select
											:model-value="formConfig.rawFormat"
											@update:model-value="setRawFormat"
											class="raw-format-select"
											:disabled="disabled"
										>
											<el-option label="JSON" value="json" />
											<el-option label="Text" value="text" />
											<el-option label="JavaScript" value="javascript" />
											<el-option label="HTML" value="html" />
											<el-option label="XML" value="xml" />
										</el-select>
										<el-button
											type="primary"
											@click="formatRawContent"
											:disabled="disabled || !formConfig.body.trim()"
											class="format-btn"
										>
											<el-icon><DocumentCopy /></el-icon>
											Beautify
										</el-button>
									</div>
								</div>
								<div class="raw-textarea-container">
									<variable-auto-complete
										:model-value="formConfig.body"
										@update:model-value="setBody"
										type="textarea"
										:rows="8"
										placeholder="Enter your content here, type '/' to insert variables..."
										class="font-mono text-sm raw-textarea"
										:disabled="disabled"
									/>
								</div>
							</div>
						</div>
					</div>
				</el-form-item>
			</el-form>
		</div>

		<!-- Test Run Section -->
		<div class="test-section">
			<div class="flex items-center justify-between mb-3">
				<h5 class="font-medium text-gray-700 dark:text-gray-300">Test API Call</h5>
				<el-button
					type="success"
					@click="handleTest"
					:loading="testing"
					:disabled="!formConfig.url"
				>
					Test Request
				</el-button>
			</div>

			<div v-if="testResult" class="test-result">
				<div class="bg-gray-50 dark:bg-gray-800 rounded p-3">
					<h6 class="font-medium text-sm mb-2">Test Result:</h6>
					<pre class="text-xs text-gray-700 dark:text-gray-300 whitespace-pre-wrap">{{
						testResult.stdout || testResult
					}}</pre>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed, ref, nextTick, onMounted } from 'vue';
import { Delete, DocumentCopy, Paperclip, Document, Close } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import { useI18n } from '@/hooks/useI18n';
import { getTokenobj } from '@/utils/auth';
import { useUserStoreWithOut } from '@/stores/modules/user';
import { getTimeZoneInfo } from '@/hooks/time';
import { useGlobSetting } from '@/settings';
import VariableAutoComplete from './VariableAutoComplete.vue';
import PrototypeTabs from '@/components/PrototypeTabs/index.vue';
import TabPane from '@/components/PrototypeTabs/TabPane.vue';
import { parseCurl, type ParsedCurlConfig } from '@/utils/curlParser';
import * as XLSX from 'xlsx-js-style';

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

interface KeyValueItem {
	key: string;
	value: string;
	focused?: boolean;
}

interface Props {
	modelValue?: {
		url?: string;
		method?: string;
		headers?: Record<string, string>;
		params?: Record<string, string>;
		bodyType?: 'none' | 'form-data' | 'x-www-form-urlencoded' | 'raw';
		formData?: Record<string, string>;
		urlEncoded?: Record<string, string>;
		rawFormat?: string;
		body?: string; // 统一的请求体字段，当bodyType为'raw'时存储raw内容
		timeout?: number;
		followRedirects?: boolean;
		// Prefer lists to preserve order and duplicates in UI
		headersList?: KeyValueItem[];
		paramsList?: KeyValueItem[];
		formDataList?: KeyValueItem[];
		urlEncodedList?: KeyValueItem[];
	};
	testing?: boolean;
	testResult?: {
		executionTime: string;
		memoryUsage: number;
		message: string;
		status: string;
		stdout: string;
		success: boolean;
		timestamp: string;
		token: string;
	};
	idEditing?: boolean;
	disabled?: boolean;
}

// Refs for textarea elements
const headerTextareaRefs = ref<any[]>([]);
const paramTextareaRefs = ref<any[]>([]);
const formDataTextareaRefs = ref<any[]>([]);
const urlEncodedTextareaRefs = ref<any[]>([]);

const { t } = useI18n();

const props = withDefaults(defineProps<Props>(), {
	modelValue: () => ({
		url: '',
		method: 'GET',
		headers: {},
		params: {},
		bodyType: 'none',
		formData: {},
		urlEncoded: {},
		rawFormat: 'json',
		body: '', // 统一的请求体字段
		timeout: 30,
		followRedirects: true,
		headersList: [],
		paramsList: [],
		formDataList: [],
		urlEncodedList: [],
	}),
	testing: false,
});

const emit = defineEmits<{
	'update:modelValue': [value: any];
	test: [];
}>();

// 主要的配置对象，在computed中处理所有数据格式转换
const formConfig = computed({
	get() {
		const data = props.modelValue || ({} as NonNullable<Props['modelValue']>);

		// headers：优先使用外部传入的列表以保留重复与顺序
		let headersList: KeyValueItem[] = Array.isArray(data.headersList)
			? [...(data.headersList as KeyValueItem[])]
			: Object.entries(data.headers || {}).map(([key, value]) => ({ key, value }));
		if (
			headersList.length === 0 ||
			String(headersList[headersList.length - 1]?.key || '').trim() ||
			String(headersList[headersList.length - 1]?.value || '').trim()
		) {
			headersList.push({ key: '', value: '' });
		}

		// params：同样优先使用列表
		let paramsList: KeyValueItem[] = Array.isArray(data.paramsList)
			? [...(data.paramsList as KeyValueItem[])]
			: Object.entries(data.params || {}).map(([key, value]) => ({ key, value }));
		if (
			paramsList.length === 0 ||
			String(paramsList[paramsList.length - 1]?.key || '').trim() ||
			String(paramsList[paramsList.length - 1]?.value || '').trim()
		) {
			paramsList.push({ key: '', value: '' });
		}

		// formData：优先列表
		let formDataList: KeyValueItem[] = Array.isArray(data.formDataList)
			? [...(data.formDataList as KeyValueItem[])]
			: Object.entries(data.formData || {}).map(([key, value]) => ({ key, value }));
		if (
			formDataList.length === 0 ||
			String(formDataList[formDataList.length - 1]?.key || '').trim() ||
			String(formDataList[formDataList.length - 1]?.value || '').trim()
		) {
			formDataList.push({ key: '', value: '' });
		}

		// urlEncoded：优先列表
		let urlEncodedList: KeyValueItem[] = Array.isArray(data.urlEncodedList)
			? [...(data.urlEncodedList as KeyValueItem[])]
			: Object.entries(data.urlEncoded || {}).map(([key, value]) => ({ key, value }));
		if (
			urlEncodedList.length === 0 ||
			String(urlEncodedList[urlEncodedList.length - 1]?.key || '').trim() ||
			String(urlEncodedList[urlEncodedList.length - 1]?.value || '').trim()
		) {
			urlEncodedList.push({ key: '', value: '' });
		}

		return {
			...data,
			url: data.url || '',
			method: data.method || 'GET',
			bodyType: data.bodyType || 'none',
			rawFormat: data.rawFormat || 'json',
			body: data.body || '', // 统一的请求体字段
			timeout: data.timeout || 30,
			followRedirects: data.followRedirects !== undefined ? data.followRedirects : true,
			// 转换后的数组格式，用于模板显示（保留到 modelValue）
			headersList,
			paramsList,
			formDataList,
			urlEncodedList,
		};
	},
	set(value) {
		// 将数组格式转换回对象格式（对象只用于请求执行；UI 仍依赖列表以保留重复与顺序）
		const headers: Record<string, string> = {};
		if (value.headersList) {
			value.headersList.forEach(({ key, value: val }: { key: string; value: string }) => {
				const k = String(key || '').trim();
				if (k) {
					headers[k] = val; // last-wins
				}
			});
		}

		const params: Record<string, string> = {};
		if (value.paramsList) {
			value.paramsList.forEach(({ key, value: val }: { key: string; value: string }) => {
				const k = String(key || '').trim();
				if (k) {
					params[k] = val; // last-wins
				}
			});
		}

		const formData: Record<string, string> = {};
		if (value.formDataList) {
			value.formDataList.forEach(({ key, value: val }: { key: string; value: string }) => {
				const k = String(key || '').trim();
				if (k) {
					formData[k] = val; // last-wins
				}
			});
		}

		const urlEncoded: Record<string, string> = {};
		if (value.urlEncodedList) {
			value.urlEncodedList.forEach(({ key, value: val }: { key: string; value: string }) => {
				const k = String(key || '').trim();
				if (k) {
					urlEncoded[k] = val; // last-wins
				}
			});
		}
		// 保留列表字段到上层 modelValue，以维持 UI 的重复 key 与顺序
		emit('update:modelValue', {
			...value,
			headers,
			params,
			formData,
			urlEncoded,
		});
	},
});

// Controlled setters to trigger computed.set via whole-object assignment
// Update action name for generated HTTP config
const updateActionName = (message: any, newName: string) => {
	console.log('📝 Updating action name:', newName);
	if (message.httpConfig) {
		message.httpConfig.actionName = newName;
		console.log('✅ Action name updated to:', newName);
	}
};

const setUrl = (val: string) => {
	formConfig.value = { ...formConfig.value, url: val } as any;
};

const setMethod = (val: string) => {
	formConfig.value = { ...formConfig.value, method: val } as any;
};

const setBodyType = (val: 'none' | 'form-data' | 'x-www-form-urlencoded' | 'raw') => {
	formConfig.value = { ...formConfig.value, bodyType: val } as any;
};

const setRawFormat = (val: string) => {
	formConfig.value = { ...formConfig.value, rawFormat: val } as any;
};

const setBody = (val: string) => {
	formConfig.value = { ...formConfig.value, body: val } as any;
};

// 在每次更新后，确保末尾存在一行空白输入
const ensureTrailingEmptyRow = (
	listName: 'headersList' | 'paramsList' | 'formDataList' | 'urlEncodedList'
) => {
	const currentList = [...(formConfig.value as any)[listName]] as Array<{
		key: unknown;
		value: unknown;
	}>;
	const last = currentList[currentList.length - 1];
	const lastKeyFilled = String((last?.key as any) ?? '').trim();
	const lastValueFilled = String((last?.value as any) ?? '').trim();
	if (last && (lastKeyFilled || lastValueFilled)) {
		currentList.push({ key: '', value: '' });
		(formConfig.value as any) = { ...(formConfig.value as any), [listName]: currentList };
	}
};

// 更新函数 - 简化为直接触发computed的set
const updateHeaderKey = (index: number, newKey: string) => {
	const newHeadersList = [...formConfig.value.headersList];
	newHeadersList[index] = { ...newHeadersList[index], key: newKey };
	formConfig.value = { ...formConfig.value, headersList: newHeadersList };
	ensureTrailingEmptyRow('headersList');
};

const updateHeaderValue = (index: number, newValue: string) => {
	const newHeadersList = [...formConfig.value.headersList];
	newHeadersList[index] = { ...newHeadersList[index], value: newValue };
	formConfig.value = { ...formConfig.value, headersList: newHeadersList };
	ensureTrailingEmptyRow('headersList');
};

const updateParamKey = (index: number, newKey: string) => {
	const newParamsList = [...formConfig.value.paramsList];
	newParamsList[index] = { ...newParamsList[index], key: newKey };
	formConfig.value = { ...formConfig.value, paramsList: newParamsList };
	ensureTrailingEmptyRow('paramsList');
};

const updateParamValue = (index: number, newValue: string) => {
	const newParamsList = [...formConfig.value.paramsList];
	newParamsList[index] = { ...newParamsList[index], value: newValue };
	formConfig.value = { ...formConfig.value, paramsList: newParamsList };
	ensureTrailingEmptyRow('paramsList');
};

const updateFormDataKey = (index: number, newKey: string) => {
	const newFormDataList = [...formConfig.value.formDataList];
	newFormDataList[index] = { ...newFormDataList[index], key: newKey };
	formConfig.value = { ...formConfig.value, formDataList: newFormDataList };
	ensureTrailingEmptyRow('formDataList');
};

const updateFormDataValue = (index: number, newValue: string) => {
	const newFormDataList = [...formConfig.value.formDataList];
	newFormDataList[index] = { ...newFormDataList[index], value: newValue };
	formConfig.value = { ...formConfig.value, formDataList: newFormDataList };
	ensureTrailingEmptyRow('formDataList');
};

const updateUrlEncodedKey = (index: number, newKey: string) => {
	const newUrlEncodedList = [...formConfig.value.urlEncodedList];
	newUrlEncodedList[index] = { ...newUrlEncodedList[index], key: newKey };
	formConfig.value = { ...formConfig.value, urlEncodedList: newUrlEncodedList };
	ensureTrailingEmptyRow('urlEncodedList');
};

const updateUrlEncodedValue = (index: number, newValue: string) => {
	const newUrlEncodedList = [...formConfig.value.urlEncodedList];
	newUrlEncodedList[index] = { ...newUrlEncodedList[index], value: newValue };
	formConfig.value = { ...formConfig.value, urlEncodedList: newUrlEncodedList };
	ensureTrailingEmptyRow('urlEncodedList');
};

// 删除函数
const removeHeader = (index: number) => {
	const newHeadersList = [...formConfig.value.headersList];
	if (newHeadersList.length > 1) {
		newHeadersList.splice(index, 1);
		formConfig.value = { ...formConfig.value, headersList: newHeadersList };
	}
};

const removeParam = (index: number) => {
	const newParamsList = [...formConfig.value.paramsList];
	if (newParamsList.length > 1) {
		newParamsList.splice(index, 1);
		formConfig.value = { ...formConfig.value, paramsList: newParamsList };
	}
};

const removeFormData = (index: number) => {
	const newFormDataList = [...formConfig.value.formDataList];
	if (newFormDataList.length > 1) {
		newFormDataList.splice(index, 1);
		formConfig.value = { ...formConfig.value, formDataList: newFormDataList };
	}
};

const removeUrlEncoded = (index: number) => {
	const newUrlEncodedList = [...formConfig.value.urlEncodedList];
	if (newUrlEncodedList.length > 1) {
		newUrlEncodedList.splice(index, 1);
		formConfig.value = { ...formConfig.value, urlEncodedList: newUrlEncodedList };
	}
};

// 聚焦/失焦功能
const setHeaderFocused = (index: number, focused: boolean) => {
	const newHeadersList = [...formConfig.value.headersList];
	newHeadersList[index] = {
		...newHeadersList[index],
		focused: focused,
	};
	formConfig.value = { ...formConfig.value, headersList: newHeadersList };

	// 如果是聚焦，等待DOM更新后重新聚焦textarea
	if (focused) {
		nextTick(() => {
			const textareaRef = headerTextareaRefs.value[index];
			if (textareaRef && textareaRef.textarea) {
				textareaRef.textarea.focus();
			}
		});
	}
};

const setParamFocused = (index: number, focused: boolean) => {
	const newParamsList = [...formConfig.value.paramsList];
	newParamsList[index] = {
		...newParamsList[index],
		focused: focused,
	};
	formConfig.value = { ...formConfig.value, paramsList: newParamsList };

	// 如果是聚焦，等待DOM更新后重新聚焦textarea
	if (focused) {
		nextTick(() => {
			const textareaRef = paramTextareaRefs.value[index];
			if (textareaRef && textareaRef.textarea) {
				textareaRef.textarea.focus();
			}
		});
	}
};

const setFormDataFocused = (index: number, focused: boolean) => {
	const newFormDataList = [...formConfig.value.formDataList];
	newFormDataList[index] = {
		...newFormDataList[index],
		focused: focused,
	};
	formConfig.value = { ...formConfig.value, formDataList: newFormDataList };

	// 如果是聚焦，等待DOM更新后重新聚焦textarea
	if (focused) {
		nextTick(() => {
			const textareaRef = formDataTextareaRefs.value[index];
			if (textareaRef && textareaRef.textarea) {
				textareaRef.textarea.focus();
			}
		});
	}
};

const setUrlEncodedFocused = (index: number, focused: boolean) => {
	const newUrlEncodedList = [...formConfig.value.urlEncodedList];
	newUrlEncodedList[index] = {
		...newUrlEncodedList[index],
		focused: focused,
	};
	formConfig.value = { ...formConfig.value, urlEncodedList: newUrlEncodedList };

	// 如果是聚焦，等待DOM更新后重新聚焦textarea
	if (focused) {
		nextTick(() => {
			const textareaRef = urlEncodedTextareaRefs.value[index];
			if (textareaRef && textareaRef.textarea) {
				textareaRef.textarea.focus();
			}
		});
	}
};

// 格式化Raw内容
const formatRawContent = () => {
	try {
		const content = formConfig.value.body.trim();
		if (!content) {
			ElMessage.warning(t('sys.api.formatEmptyContent'));
			return;
		}

		let formattedContent = '';
		const format = formConfig.value.rawFormat;

		switch (format) {
			case 'json':
				try {
					const parsed = JSON.parse(content);
					formattedContent = JSON.stringify(parsed, null, 2);
				} catch (error) {
					ElMessage.error(t('sys.api.formatInvalidJson'));
					return;
				}
				break;

			case 'xml':
			case 'html':
				// 简单的XML/HTML格式化
				formattedContent = formatXmlHtml(content);
				break;

			case 'javascript':
				// 对于JavaScript，我们可以做一些基本的格式化
				formattedContent = formatJavaScript(content);
				break;

			case 'text':
			default:
				ElMessage.info(t('sys.api.formatNotNeeded'));
				return;
		}

		setBody(formattedContent);
		ElMessage.success(t('sys.api.formatSuccess'));
	} catch (error) {
		console.error('格式化失败:', error);
		ElMessage.error(t('sys.api.formatFailed'));
	}
};

// XML/HTML格式化辅助函数
const formatXmlHtml = (content: string): string => {
	// 简单的XML/HTML格式化逻辑
	let formatted = content
		.replace(/></g, '>\n<') // 在标签之间添加换行
		.replace(/^\s+|\s+$/g, '') // 去除首尾空白
		.split('\n');

	let indentLevel = 0;
	const indentSize = 2;

	return formatted
		.map((line) => {
			const trimmed = line.trim();
			if (!trimmed) return '';

			// 减少缩进（闭合标签）
			if (trimmed.startsWith('</')) {
				indentLevel = Math.max(0, indentLevel - 1);
			}

			const indentedLine = ' '.repeat(indentLevel * indentSize) + trimmed;

			// 增加缩进（开放标签，但不是自闭合标签）
			if (trimmed.startsWith('<') && !trimmed.startsWith('</') && !trimmed.endsWith('/>')) {
				indentLevel++;
			}

			return indentedLine;
		})
		.join('\n');
};

// JavaScript格式化辅助函数
const formatJavaScript = (content: string): string => {
	// 简单的JavaScript格式化
	return content
		.replace(/;/g, ';\n') // 分号后换行
		.replace(/{/g, '{\n') // 开括号后换行
		.replace(/}/g, '\n}') // 闭括号前换行
		.replace(/,/g, ',\n') // 逗号后换行
		.split('\n')
		.map((line) => line.trim())
		.filter((line) => line.length > 0)
		.join('\n');
};

// 导入弹窗相关状态
const importDialogVisible = ref(false);
const activeImportTab = ref('curl');
const curlInput = ref('');
const importError = ref(''); // 导入错误信息
const importLoading = ref(false); // 导入加载状态

// AI 相关状态
interface AIChatMessage {
	role: 'user' | 'assistant';
	content: string;
	timestamp: string;
	httpConfig?: any;
}

const aiChatMessages = ref<AIChatMessage[]>([]);
const aiCurrentInput = ref('');
const aiGenerating = ref(false);
const selectedAIModel = ref('zhipuai');
const currentAIModel = ref<AIModelConfig | null>(null);
const availableModels = ref<AIModelConfig[]>([]);
const uploadedFile = ref<File | null>(null);
const chatMessagesRef = ref<HTMLElement>();
const fileUploadRef = ref();

// File Types Configuration
const maxFileSize = 10 * 1024 * 1024; // 10MB

// Computed
const supportedFormats = computed(() => {
	return ['txt', 'pdf', 'docx', 'xlsx', 'csv', 'md', 'json'];
});

// AI Model Configuration interface
interface AIModelConfig {
	id: number;
	provider: string;
	modelName: string;
	isAvailable: boolean;
	isDefault?: boolean;
}

// 导入标签页配置
const importTabsConfig = [
	{
		value: 'curl',
		label: 'From cURL',
	},
	{
		value: 'ai',
		label: 'AI Generate',
	},
];

// 显示导入弹窗
const showImportDialog = () => {
	importDialogVisible.value = true;
	activeImportTab.value = 'curl';
	curlInput.value = '';
	importError.value = ''; // 清空错误信息
	importLoading.value = false; // 重置加载状态
};

// 关闭导入弹窗
const handleImportDialogClose = () => {
	importDialogVisible.value = false;
	curlInput.value = '';
	importError.value = ''; // 清空错误信息
	importLoading.value = false; // 重置加载状态
};

// 处理cURL导入
const handleCurlImport = async () => {
	importError.value = ''; // 清空之前的错误
	importLoading.value = true; // 开始加载
	try {
		if (!curlInput.value.trim()) {
			importError.value = 'Please enter a cURL command';
			return;
		}

		// 解析cURL命令
		const parsedConfig = parseCurl(curlInput.value);
		// 更新表单配置
		await updateFormFromParsedCurl(parsedConfig);

		// 成功后关闭弹窗
		handleImportDialogClose();
		ElMessage.success('cURL command imported successfully');
	} catch (error) {
		// 显示通用的格式错误信息
		importError.value = 'Incorrect format, please input the right format to import';
	} finally {
		importLoading.value = false; // 结束加载
	}
};

/**
 * 将键值对对象转换为KeyValueItem数组，并确保末尾有空行
 */
const convertToKeyValueList = (obj: Record<string, string>): KeyValueItem[] => {
	const list: KeyValueItem[] = [];

	// 转换有效的键值对
	Object.entries(obj || {}).forEach(([key, value]) => {
		if (key && value !== undefined && value !== null) {
			list.push({ key: key.trim(), value: String(value).trim() });
		}
	});

	// 确保至少有一个空行用于添加新项
	if (list.length === 0 || list[list.length - 1].key !== '') {
		list.push({ key: '', value: '' });
	}

	return list;
};

/**
 * 重置所有body相关字段为默认状态
 */
const resetBodyFields = () => {
	formConfig.value.formDataList = [{ key: '', value: '' }];
	formConfig.value.urlEncodedList = [{ key: '', value: '' }];
	formConfig.value.body = '';
	formConfig.value.rawFormat = 'json';
};

// 根据解析的cURL配置更新表单
const updateFormFromParsedCurl = async (parsedConfig: ParsedCurlConfig) => {
	try {
		// 1. 更新基本信息
		formConfig.value.url = parsedConfig.url || '';
		formConfig.value.method = parsedConfig.method || 'GET';

		// 2. 更新查询参数
		formConfig.value.paramsList = convertToKeyValueList(parsedConfig.params);

		// 3. 更新请求头
		formConfig.value.headersList = convertToKeyValueList(parsedConfig.headers);

		// 4. 更新请求体类型
		formConfig.value.bodyType = parsedConfig.bodyType || 'none';

		// 5. 根据body类型更新相应字段
		switch (parsedConfig.bodyType) {
			case 'form-data':
				// 重置其他body字段
				formConfig.value.urlEncodedList = [{ key: '', value: '' }];
				formConfig.value.body = '';
				// 设置form-data
				formConfig.value.formDataList = convertToKeyValueList(parsedConfig.formData || {});
				break;

			case 'x-www-form-urlencoded':
				// 重置其他body字段
				formConfig.value.formDataList = [{ key: '', value: '' }];
				formConfig.value.body = '';
				// 设置url-encoded
				formConfig.value.urlEncodedList = convertToKeyValueList(
					parsedConfig.urlEncoded || {}
				);
				break;

			case 'raw':
				// 重置其他body字段
				formConfig.value.formDataList = [{ key: '', value: '' }];
				formConfig.value.urlEncodedList = [{ key: '', value: '' }];
				// 设置raw body
				formConfig.value.body = parsedConfig.rawBody || '';
				formConfig.value.rawFormat = parsedConfig.rawFormat || 'json';
				break;

			case 'none':
			default:
				// 重置所有body字段
				resetBodyFields();
				break;
		}

		// 6. 触发响应式更新
		formConfig.value = { ...formConfig.value };
	} catch (error) {
		throw new Error('Failed to update form configuration');
	}
};

const handleTest = () => {
	emit('test');
};

// AI 相关方法
const handleAIKeydown = (event: KeyboardEvent) => {
	if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
		event.preventDefault();
		sendAIMessage();
	}
};

const sendAIMessage = async () => {
	console.log('🚀 sendAIMessage called');

	if (!aiCurrentInput.value.trim() && !uploadedFile.value) {
		console.log('❌ No input or file, returning');
		return;
	}

	console.log('📝 User input:', aiCurrentInput.value.trim());
	console.log('📎 Uploaded file:', uploadedFile.value?.name);

	const userMessage = {
		role: 'user' as const,
		content: aiCurrentInput.value.trim(),
		timestamp: new Date().toISOString(),
	};

	// 如果有上传的文件，解析文件内容并添加到消息中
	if (uploadedFile.value) {
		try {
			console.log('📄 Reading file content for display...', uploadedFile.value.name);
			const fileContent = await readFileContent(uploadedFile.value);
			const truncatedContent =
				fileContent.length > 1000
					? fileContent.substring(0, 1000) + '\n\n[Content truncated for display...]'
					: fileContent;
			userMessage.content += `\n\n📎 **File Content** (${uploadedFile.value.name}):\n\`\`\`\n${truncatedContent}\n\`\`\``;
			console.log('✅ File content added to message, length:', fileContent.length);
		} catch (error) {
			console.error('❌ Error reading file content:', error);
			userMessage.content += `\n\n📎 **File** (${
				uploadedFile.value.name
			}): ❌ Failed to read content - ${
				error instanceof Error ? error.message : 'Unknown error'
			}`;
		}
	}

	aiChatMessages.value.push(userMessage);
	console.log('💬 Added user message, total messages:', aiChatMessages.value.length);

	const currentInput = aiCurrentInput.value.trim();
	aiCurrentInput.value = '';
	aiGenerating.value = true;
	console.log('⏳ Set aiGenerating to true');

	// 滚动到底部
	nextTick(() => {
		if (chatMessagesRef.value) {
			chatMessagesRef.value.scrollTop = chatMessagesRef.value.scrollHeight;
		}
	});

	try {
		// 关闭加载状态，开始显示流式内容
		aiGenerating.value = false;
		console.log('✅ Set aiGenerating to false, starting streaming');

		// 使用流式响应处理AI请求
		await processAIRequestWithStreaming(currentInput, uploadedFile.value);
		console.log('🎉 processAIRequestWithStreaming completed');
	} catch (error) {
		console.error('AI generation error:', error);
		const errorMessage = {
			role: 'assistant' as const,
			content: `Sorry, I encountered an error while generating the HTTP configuration: ${
				error instanceof Error ? error.message : 'Unknown error'
			}`,
			timestamp: new Date().toISOString(),
		};
		aiChatMessages.value.push(errorMessage);
		ElMessage.error('Failed to generate HTTP configuration');
	} finally {
		aiGenerating.value = false;
		uploadedFile.value = null;

		// 滚动到底部
		nextTick(() => {
			if (chatMessagesRef.value) {
				chatMessagesRef.value.scrollTop = chatMessagesRef.value.scrollHeight;
			}
		});
	}
};

// 流式处理AI请求
const processAIRequestWithStreaming = async (input: string, file: File | null) => {
	// 创建一个助手消息用于显示流式内容
	const assistantMessage = {
		role: 'assistant' as const,
		content: 'Initializing AI analysis...',
		timestamp: new Date().toISOString(),
		httpConfig: null as any,
	};
	aiChatMessages.value.push(assistantMessage);

	let analysisResult: any = null;
	let streamingContent = '';
	let creationContent = '';

	try {
		console.log('🚀 Starting AI analysis stream...');

		// 第一步：流式分析用户请求
		await streamAnalyzeRequest(input, file, (chunk, data) => {
			console.log('📥 Received chunk:', chunk);

			// 统一处理大小写问题
			const chunkType = chunk.type || chunk.Type;
			const chunkContent = chunk.content || chunk.Content;
			const chunkActionData = chunk.actionData || chunk.ActionData;

			console.log('🔍 Normalized chunk type:', chunkType);
			if (chunkType === 'complete') {
				console.log('🔍 Complete chunk actionData exists:', !!chunkActionData);
			}

			if (chunkType === 'analysis' || chunkType === 'progress') {
				console.log('📝 Processing analysis/progress chunk:', chunkContent);
				streamingContent += chunkContent;

				// 直接修改数组中的最后一个消息（助手消息）
				// 保留初始的"Initializing AI analysis..."内容，然后追加流式内容
				const lastMessageIndex = aiChatMessages.value.length - 1;
				if (lastMessageIndex >= 0) {
					const initialContent = 'Initializing AI analysis...\n\n';
					const fullContent = initialContent + streamingContent;
					aiChatMessages.value[lastMessageIndex].content = fullContent;
					console.log(
						'🔄 Updated array message at index',
						lastMessageIndex,
						'new content length:',
						fullContent.length
					);
					console.log(
						'🔍 Current message content preview:',
						fullContent.substring(0, 100) + '...'
					);
				}
				console.log('📄 Updated message content length:', streamingContent.length);

				// 实时滚动到底部
				nextTick(() => {
					if (chatMessagesRef.value) {
						chatMessagesRef.value.scrollTop = chatMessagesRef.value.scrollHeight;
					}
				});
			} else if (chunkType === 'complete' && chunkActionData) {
				console.log('✅ Analysis completed, actionData:', chunkActionData);

				// 检查 ActionData 的成功状态
				console.log('🔍 Checking ActionData.Success:', chunkActionData.Success);
				console.log('🔍 ActionItems count:', chunkActionData.ActionItems?.length || 0);

				if (chunkActionData.Success === false) {
					console.warn(
						'⚠️ Analysis completed but marked as unsuccessful:',
						chunkActionData.Message
					);
					// 即使标记为不成功，如果有 ActionItems，我们仍然可以继续
					if (chunkActionData.ActionItems && chunkActionData.ActionItems.length > 0) {
						console.log(
							'📋 Found',
							chunkActionData.ActionItems.length,
							'action items, proceeding with creation...'
						);
						analysisResult = chunkActionData;
					} else {
						console.error('❌ No action items found in analysis result');
						analysisResult = null;
					}
				} else {
					console.log('✅ Analysis marked as successful');
					analysisResult = chunkActionData;
				}

				streamingContent +=
					'\n\n✅ Analysis completed. Now creating HTTP configuration...\n\n';

				// 直接修改数组中的最后一个消息（助手消息）
				// 保留初始内容和分析内容
				const lastMessageIndex = aiChatMessages.value.length - 1;
				if (lastMessageIndex >= 0) {
					const initialContent = 'Initializing AI analysis...\n\n';
					aiChatMessages.value[lastMessageIndex].content =
						initialContent + streamingContent;
				}

				// 滚动到底部
				nextTick(() => {
					if (chatMessagesRef.value) {
						chatMessagesRef.value.scrollTop = chatMessagesRef.value.scrollHeight;
					}
				});
			}
		});

		// 更详细的错误检查
		if (!analysisResult) {
			console.error('❌ Analysis result is null or undefined');
			throw new Error('Failed to analyze request: No analysis result received');
		}

		if (!analysisResult.ActionItems || analysisResult.ActionItems.length === 0) {
			console.error('❌ No action items found in analysis result:', analysisResult);
			throw new Error('Failed to analyze request: No actionable items identified');
		}

		console.log(
			'✅ Analysis result validated, proceeding with',
			analysisResult.ActionItems.length,
			'action items'
		);

		console.log('🔄 Starting HTTP configuration creation...');

		// 第二步：流式创建HTTP配置
		await streamCreateAction(analysisResult, (chunk, data) => {
			console.log('📦 Received creation chunk:', chunk);
			console.log('📦 Chunk raw data:', JSON.stringify(chunk).substring(0, 200) + '...');

			// 统一处理大小写问题
			const chunkType = chunk.type || chunk.Type;
			const chunkContent = chunk.content || chunk.Content;
			const chunkActionData = chunk.actionData || chunk.ActionData;

			console.log('🔍 Creation chunk type:', chunkType);
			if (chunkType === 'complete') {
				console.log('🔍 Creation complete chunk actionData exists:', !!chunkActionData);
			}

			if (chunkType === 'creation' || chunkType === 'progress') {
				console.log('🛠️ Processing creation/progress chunk:', chunkContent);
				// 累积创建阶段的内容
				creationContent += chunkContent;

				// 组合完整内容：初始内容 + 分析内容 + 创建内容
				const initialContent = 'Initializing AI analysis...\n\n';
				const analysisCompleteText =
					streamingContent +
					'\n\n✅ Analysis completed. Now creating HTTP configuration...\n\n';
				const fullContent = initialContent + analysisCompleteText + creationContent;

				// 直接修改数组中的最后一个消息（助手消息）
				const lastMessageIndex = aiChatMessages.value.length - 1;
				if (lastMessageIndex >= 0) {
					aiChatMessages.value[lastMessageIndex].content = fullContent;
				}
				console.log('📄 Updated creation content length:', fullContent.length);
				console.log(
					'🔍 Creation content preview:',
					creationContent.substring(0, 50) + '...'
				);

				// 实时滚动到底部
				nextTick(() => {
					if (chatMessagesRef.value) {
						chatMessagesRef.value.scrollTop = chatMessagesRef.value.scrollHeight;
					}
				});
			} else if (chunkType === 'complete' && chunkActionData) {
				console.log('🎉 Creation completed, actionData:', chunkActionData);
				// 从生成的行动计划中提取HTTP配置
				const httpConfig = extractHttpConfigFromActionPlan(chunkActionData);
				console.log('🔧 Extracted HTTP config:', httpConfig);

				// 直接修改数组中的最后一个消息（助手消息）
				// 保留所有分析和创建内容，只添加HTTP配置
				const lastMessageIndex = aiChatMessages.value.length - 1;
				if (lastMessageIndex >= 0) {
					aiChatMessages.value[lastMessageIndex].httpConfig = httpConfig;
					// 保留完整的分析和创建内容，添加完成提示
					const initialContent = 'Initializing AI analysis...\n\n';
					const analysisCompleteText =
						streamingContent +
						'\n\n✅ Analysis completed. Now creating HTTP configuration...\n\n';
					const finalContent =
						initialContent +
						analysisCompleteText +
						creationContent +
						'\n\n✅ HTTP configuration generated successfully!';
					aiChatMessages.value[lastMessageIndex].content = finalContent;
				}

				// 最终滚动到底部
				nextTick(() => {
					if (chatMessagesRef.value) {
						chatMessagesRef.value.scrollTop = chatMessagesRef.value.scrollHeight;
					}
				});
			}
		});
	} catch (error) {
		console.error('Streaming error:', error);

		// 直接修改数组中的最后一个消息（助手消息）
		const lastMessageIndex = aiChatMessages.value.length - 1;
		if (lastMessageIndex >= 0) {
			aiChatMessages.value[lastMessageIndex].content = `Error: ${
				error instanceof Error ? error.message : 'Unknown error'
			}`;
		}
		throw error;
	}
};

// 流式分析请求
const streamAnalyzeRequest = async (
	input: string,
	file: File | null,
	onChunk: (chunk: any, data?: any) => void
) => {
	const conversationHistory = [
		{
			role: 'user',
			content: input,
			timestamp: new Date().toISOString(),
		},
	];

	// 如果有文件，读取文件内容并添加到上下文中
	let context = 'HTTP API configuration generation request';
	if (file) {
		try {
			const fileContent = await readFileContent(file);
			context += `\n\nFile content (${file.name}):\n${fileContent}`;
		} catch (error) {
			console.error('Error reading file:', error);
		}
	}

	const payload = {
		conversationHistory,
		sessionId: `http_config_${Date.now()}`,
		context,
		focusAreas: ['HTTP API', 'Configuration', 'Request/Response'],
		modelId: currentAIModel.value?.id?.toString(),
		modelProvider: currentAIModel.value?.provider || selectedAIModel.value,
		modelName:
			currentAIModel.value?.modelName ||
			(selectedAIModel.value === 'zhipuai'
				? 'glm-4'
				: selectedAIModel.value === 'openai'
				? 'gpt-4'
				: 'claude-3'),
	};

	// 获取认证信息
	const tokenObj = getTokenobj();
	const userStore = useUserStoreWithOut();
	const userInfo = userStore.getUserInfo;
	const globSetting = useGlobSetting();

	// 构建请求头
	const headers: Record<string, string> = {
		'Content-Type': 'application/json',
		'Time-Zone': getTimeZoneInfo().timeZone,
		'Application-code': globSetting?.ssoCode || '',
		Accept: 'text/event-stream',
		'Cache-Control': 'no-cache',
	};

	// 添加认证头
	if (tokenObj?.accessToken?.token) {
		const token = tokenObj.accessToken.token;
		const tokenType = tokenObj.accessToken.tokenType || 'Bearer';
		headers.Authorization = `${tokenType} ${token}`;
	}

	// 添加用户相关头信息
	if (userInfo?.appCode) {
		headers['X-App-Code'] = String(userInfo.appCode);
	}
	if (userInfo?.tenantId) {
		headers['X-Tenant-Id'] = String(userInfo.tenantId);
	}

	console.log('🌐 Starting analyze stream request to:', '/api/ai/v1/actions/analyze/stream');
	console.log('📤 Request payload:', payload);

	// 使用EventSource进行流式请求
	return new Promise<void>((resolve, reject) => {
		fetch('/api/ai/v1/actions/analyze/stream', {
			method: 'POST',
			headers: headers,
			body: JSON.stringify(payload),
		})
			.then((response) => {
				console.log('📡 Response received:', response.status, response.statusText);

				if (!response.ok) {
					throw new Error(`HTTP ${response.status}: ${response.statusText}`);
				}

				const reader = response.body?.getReader();
				if (!reader) {
					throw new Error('No response body reader available');
				}

				const decoder = new TextDecoder();

				const readStream = async () => {
					try {
						console.log('📖 Starting to read stream...');
						while (true) {
							const { done, value } = await reader.read();
							if (done) {
								console.log('✅ Stream reading completed');
								break;
							}

							const chunk = decoder.decode(value, { stream: true });
							console.log('📝 Raw chunk received:', chunk);
							const lines = chunk.split('\n');

							for (const line of lines) {
								if (line.startsWith('data: ')) {
									const data = line.substring(6);
									console.log('📊 Processing data line:', data);

									if (data === '[DONE]') {
										console.log('🏁 Received [DONE] signal');
										resolve();
										return;
									}

									try {
										const parsed = JSON.parse(data);
										console.log('✨ Parsed JSON data:', parsed);
										onChunk(parsed);

										if (parsed.type === 'complete') {
											console.log('🎯 Stream completed');
											resolve();
											return;
										} else if (parsed.type === 'error') {
											console.error('❌ Stream error:', parsed.content);
											reject(new Error(parsed.content));
											return;
										}
									} catch (e) {
										console.warn('⚠️ Failed to parse JSON:', data, e);
										// Skip invalid JSON
										continue;
									}
								}
							}
						}
						resolve();
					} catch (error) {
						console.error('💥 Stream reading error:', error);
						reject(error);
					}
				};

				readStream();
			})
			.catch((error) => {
				console.error('🚫 Fetch error:', error);
				reject(error);
			});
	});
};

// 流式创建Action
const streamCreateAction = async (
	analysisResult: any,
	onChunk: (chunk: any, data?: any) => void
) => {
	const payload = {
		analysisResult,
		context: 'Generate HTTP API configuration based on user requirements',
		stakeholders: ['Developer', 'API Consumer'],
		priority: 'High',
		modelId: currentAIModel.value?.id?.toString(),
		modelProvider: currentAIModel.value?.provider || selectedAIModel.value,
		modelName:
			currentAIModel.value?.modelName ||
			(selectedAIModel.value === 'zhipuai'
				? 'glm-4'
				: selectedAIModel.value === 'openai'
				? 'gpt-4'
				: 'claude-3'),
	};

	// 获取认证信息
	const tokenObj = getTokenobj();
	const userStore = useUserStoreWithOut();
	const userInfo = userStore.getUserInfo;
	const globSetting = useGlobSetting();

	// 构建请求头
	const headers: Record<string, string> = {
		'Content-Type': 'application/json',
		'Time-Zone': getTimeZoneInfo().timeZone,
		'Application-code': globSetting?.ssoCode || '',
		Accept: 'text/event-stream',
		'Cache-Control': 'no-cache',
	};

	// 添加认证头
	if (tokenObj?.accessToken?.token) {
		const token = tokenObj.accessToken.token;
		const tokenType = tokenObj.accessToken.tokenType || 'Bearer';
		headers.Authorization = `${tokenType} ${token}`;
	}

	// 添加用户相关头信息
	if (userInfo?.appCode) {
		headers['X-App-Code'] = String(userInfo.appCode);
	}
	if (userInfo?.tenantId) {
		headers['X-Tenant-Id'] = String(userInfo.tenantId);
	}

	console.log('🌐 Starting create stream request to:', '/api/ai/v1/actions/create/stream');
	console.log('📤 Create request payload:', payload);

	// 使用fetch进行流式请求
	return new Promise<void>((resolve, reject) => {
		fetch('/api/ai/v1/actions/create/stream', {
			method: 'POST',
			headers: headers,
			body: JSON.stringify(payload),
		})
			.then((response) => {
				console.log('📡 Create response received:', response.status, response.statusText);

				if (!response.ok) {
					throw new Error(`HTTP ${response.status}: ${response.statusText}`);
				}

				const reader = response.body?.getReader();
				if (!reader) {
					throw new Error('No response body reader available');
				}

				const decoder = new TextDecoder();

				const readStream = async () => {
					try {
						console.log('📖 Starting to read create stream...');
						while (true) {
							const { done, value } = await reader.read();
							if (done) {
								console.log('✅ Create stream reading completed');
								break;
							}

							const chunk = decoder.decode(value, { stream: true });
							console.log('📝 Create raw chunk received:', chunk);
							const lines = chunk.split('\n');

							for (const line of lines) {
								if (line.startsWith('data: ')) {
									const data = line.substring(6);
									console.log('📊 Processing create data line:', data);

									if (data === '[DONE]') {
										console.log('🏁 Create received [DONE] signal');
										resolve();
										return;
									}

									try {
										const parsed = JSON.parse(data);
										console.log('✨ Create parsed JSON data:', parsed);
										onChunk(parsed);

										if (parsed.type === 'complete') {
											console.log('🎯 Create stream completed');
											resolve();
											return;
										} else if (parsed.type === 'error') {
											console.error(
												'❌ Create stream error:',
												parsed.content
											);
											reject(new Error(parsed.content));
											return;
										}
									} catch (e) {
										console.warn('⚠️ Create failed to parse JSON:', data, e);
										// Skip invalid JSON
										continue;
									}
								}
							}
						}
						resolve();
					} catch (error) {
						console.error('💥 Create stream reading error:', error);
						reject(error);
					}
				};

				readStream();
			})
			.catch((error) => {
				console.error('🚫 Create fetch error:', error);
				reject(error);
			});
	});
};

// Removed unused analyzeUserRequest function

// Removed unused createHttpAction function

// 解析curl命令的函数
const parseCurlCommand = (input: string) => {
	console.log('🔍 Parsing curl command from input:', input.substring(0, 200) + '...');

	const config: any = {
		method: 'GET',
		url: '',
		headers: {},
		bodyType: 'none',
		body: '',
	};

	// 查找curl命令
	const curlMatch = input.match(
		/curl\s+[^']*'([^']+)'|curl\s+--location\s+'([^']+)'|curl\s+([^\s\\]+)/
	);
	if (curlMatch) {
		config.url = curlMatch[1] || curlMatch[2] || curlMatch[3];
		console.log('📍 Found URL:', config.url);
	}

	// 解析HTTP方法
	const methodMatch = input.match(/--request\s+(\w+)|-X\s+(\w+)/i);
	if (methodMatch) {
		config.method = (methodMatch[1] || methodMatch[2]).toUpperCase();
	} else {
		// 默认GET，除非有数据
		config.method = input.includes('--data') ? 'POST' : 'GET';
	}
	console.log('🔧 HTTP Method:', config.method);

	// 解析headers
	const headerMatches = input.matchAll(/--header\s+'([^']+)'|--header\s+"([^"]+)"/g);
	for (const match of headerMatches) {
		const headerValue = match[1] || match[2];
		const [key, ...valueParts] = headerValue.split(':');
		if (key && valueParts.length > 0) {
			const value = valueParts.join(':').trim();
			config.headers[key.trim()] = value;
		}
	}
	console.log('📋 Headers:', config.headers);

	// 解析请求体
	const dataMatch = input.match(/--data-raw\s+'([^']+)'|--data\s+'([^']+)'/);
	if (dataMatch) {
		config.body = dataMatch[1] || dataMatch[2];
		config.bodyType = 'raw';

		// 尝试检测JSON格式
		try {
			JSON.parse(config.body);
			config.rawFormat = 'json';
		} catch {
			config.rawFormat = 'text';
		}
	}

	return config;
};

const extractHttpConfigFromActionPlan = (actionPlan: any) => {
	// 从AI生成的行动计划中提取HTTP配置信息，同时从原始用户输入中解析curl命令
	console.log('🔧 Extracting HTTP config from action plan:', actionPlan);

	// 首先尝试从用户的原始输入中解析curl命令
	const userInput = aiChatMessages.value.find((msg) => msg.role === 'user')?.content || '';
	console.log('📝 User input for parsing:', userInput);

	const curlConfig = parseCurlCommand(userInput);
	if (curlConfig.url) {
		console.log('✅ Successfully parsed curl command:', curlConfig);
		// 添加默认的action名称
		curlConfig.actionName = generateActionName(curlConfig.method, curlConfig.url);
		return curlConfig;
	}

	// 如果curl解析失败，回退到从AI响应中解析
	const actions = actionPlan.actions || [];

	// 查找包含HTTP配置信息的行动项目
	const httpAction = actions.find(
		(action: any) =>
			action.title?.toLowerCase().includes('http') ||
			action.description?.toLowerCase().includes('request') ||
			action.category?.toLowerCase().includes('api')
	);

	if (httpAction) {
		// 尝试从描述中解析HTTP配置
		const description = httpAction.description || '';

		// 简单的解析逻辑，实际应用中可能需要更复杂的解析
		const config: any = {
			method: 'GET',
			url: '',
			headers: {},
			bodyType: 'none',
			body: '',
		};

		// 解析HTTP方法
		const methodMatch = description.match(/\b(GET|POST|PUT|DELETE|PATCH)\b/i);
		if (methodMatch) {
			config.method = methodMatch[1].toUpperCase();
		}

		// 解析URL
		const urlMatch = description.match(/(?:url|endpoint|api):\s*([^\s\n]+)/i);
		if (urlMatch) {
			config.url = urlMatch[1];
		}

		// 解析Content-Type
		if (description.toLowerCase().includes('json')) {
			config.headers['Content-Type'] = 'application/json';
			config.bodyType = 'raw';
			config.rawFormat = 'json';
		} else if (description.toLowerCase().includes('form')) {
			config.headers['Content-Type'] = 'application/x-www-form-urlencoded';
			config.bodyType = 'x-www-form-urlencoded';
		}

		// 解析认证头
		if (
			description.toLowerCase().includes('authorization') ||
			description.toLowerCase().includes('token')
		) {
			config.headers['Authorization'] = 'Bearer {{token}}';
		}

		// 添加默认的action名称
		config.actionName = generateActionName(config.method, config.url);
		return config;
	}

	// 默认配置
	const defaultConfig = {
		method: 'GET',
		url: 'https://api.example.com/endpoint',
		headers: {
			'Content-Type': 'application/json',
		},
		bodyType: 'none',
		body: '',
		actionName: 'custom_api_action',
	};
	return defaultConfig;
};

// Enhanced file content reading functions
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

const readFileContent = async (file: File): Promise<string> => {
	// Validate file type
	if (!isValidFileType(file)) {
		throw new Error(
			`Unsupported file type: ${file.name}. Supported formats: ${supportedFormats.value.join(
				', '
			)}`
		);
	}

	// Validate file size
	if (file.size > maxFileSize) {
		throw new Error(`File size exceeds 10MB limit. Current size: ${formatFileSize(file.size)}`);
	}

	// Extract content based on file type
	const extension = file.name.toLowerCase().split('.').pop();
	let content = '';

	try {
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

		if (!content.trim()) {
			throw new Error('No readable content found in the file');
		}

		return content.trim();
	} catch (error) {
		console.error('File processing error:', error);
		throw error;
	}
};

const handleFileUpload = async (file: File) => {
	// Validate file type
	if (!isValidFileType(file)) {
		ElMessage.error(
			`Unsupported file type: ${file.name}. Supported formats: ${supportedFormats.value.join(
				', '
			)}`
		);
		return false;
	}

	// Validate file size
	if (file.size > maxFileSize) {
		ElMessage.error(`File size exceeds 10MB limit. Current size: ${formatFileSize(file.size)}`);
		return false;
	}

	uploadedFile.value = file;
	ElMessage.success(
		`File "${file.name}" selected successfully. Supported format: ${file.name
			.split('.')
			.pop()
			?.toUpperCase()}`
	);
	return false; // 阻止自动上传
};

const removeUploadedFile = () => {
	uploadedFile.value = null;
};

// 生成Action名称的辅助函数
const generateActionName = (url: string, method: string): string => {
	try {
		const urlObj = new URL(url);
		const pathSegments = urlObj.pathname.split('/').filter((segment) => segment.length > 0);
		const lastSegment = pathSegments[pathSegments.length - 1] || 'api';

		// 清理路径段，移除数字ID和特殊字符
		const cleanSegment = lastSegment
			.replace(/^\d+$/, 'item') // 纯数字替换为item
			.replace(/[^a-zA-Z0-9]/g, '_') // 特殊字符替换为下划线
			.replace(/_+/g, '_') // 多个下划线合并为一个
			.replace(/^_|_$/g, ''); // 移除开头和结尾的下划线

		return `${method.toLowerCase()}_${cleanSegment}`;
	} catch (error) {
		// 如果URL解析失败，使用默认名称
		return `${method.toLowerCase()}_api_action`;
	}
};

// 创建HTTP Action的API调用函数
const createHttpActionAPI = async (actionParams: any) => {
	// 获取认证信息
	const tokenObj = getTokenobj();
	const userStore = useUserStoreWithOut();
	const userInfo = userStore.getUserInfo;
	const globSetting = useGlobSetting();

	// 构建请求头
	const headers: Record<string, string> = {
		'Content-Type': 'application/json',
		'Time-Zone': getTimeZoneInfo().timeZone,
		'Application-code': globSetting?.ssoCode || '',
	};

	// 添加认证头
	if (tokenObj?.accessToken?.token) {
		const token = tokenObj.accessToken.token;
		const tokenType = tokenObj.accessToken.tokenType || 'Bearer';
		headers.Authorization = `${tokenType} ${token}`;
	}

	// 添加用户相关头信息
	if (userInfo?.appCode) {
		headers['X-App-Code'] = String(userInfo.appCode);
	}
	if (userInfo?.tenantId) {
		headers['X-Tenant-Id'] = String(userInfo.tenantId);
	}

	console.log('🌐 Calling addAction API with params:', actionParams);
	console.log('📋 Request headers:', headers);

	const response = await fetch('/api/action/v1/definitions', {
		method: 'POST',
		headers,
		body: JSON.stringify(actionParams),
	});

	console.log('📡 HTTP Response Status:', response.status, response.statusText);

	if (!response.ok) {
		console.error('❌ HTTP Error:', response.status, response.statusText);
		throw new Error(`HTTP ${response.status}: ${response.statusText}`);
	}

	const result = await response.json();
	console.log('📨 API Response:', result);

	return result;
};

const applyGeneratedConfig = async (httpConfig: any) => {
	console.log('🔧 Applying generated HTTP config:', httpConfig);

	// 应用生成的配置到表单
	if (httpConfig.url) {
		formConfig.value.url = httpConfig.url;
		console.log('✅ Applied URL:', httpConfig.url);
	}
	if (httpConfig.method) {
		formConfig.value.method = httpConfig.method;
		console.log('✅ Applied Method:', httpConfig.method);
	}
	if (httpConfig.headers) {
		const headersList = Object.entries(httpConfig.headers).map(([key, value]) => ({
			key,
			value: value as string,
		}));
		headersList.push({ key: '', value: '' }); // 添加空行
		formConfig.value.headersList = headersList;
		console.log('✅ Applied Headers:', headersList);
	}
	if (httpConfig.bodyType) {
		formConfig.value.bodyType = httpConfig.bodyType;
		console.log('✅ Applied Body Type:', httpConfig.bodyType);
	}
	if (httpConfig.body) {
		formConfig.value.body = httpConfig.body;
		console.log('✅ Applied Body:', httpConfig.body);
	}
	if (httpConfig.rawFormat) {
		formConfig.value.rawFormat = httpConfig.rawFormat;
		console.log('✅ Applied Raw Format:', httpConfig.rawFormat);
	}

	// 显示配置应用成功消息
	ElMessage.success('HTTP configuration applied successfully!');

	// 滚动到表单顶部以便用户看到应用的配置
	nextTick(() => {
		const formElement = document.querySelector('.space-y-6.import-dialog');
		if (formElement) {
			formElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
		}
	});

	// 自动创建HTTP Action
	try {
		console.log('🚀 Creating HTTP Action automatically...');

		// 使用自定义的Action名称，如果没有则生成默认名称
		const actionName =
			httpConfig.actionName || generateActionName(httpConfig.url, httpConfig.method);

		// 准备Action配置
		const actionConfig = {
			url: httpConfig.url || '',
			method: httpConfig.method || 'GET',
			headers: httpConfig.headers || {},
			params: {},
			body: httpConfig.body || '',
			timeout: 30,
			followRedirects: true,
		};

		// 准备创建Action的参数
		const actionParams = {
			name: actionName,
			description: `Auto-generated HTTP Action for ${httpConfig.method} ${httpConfig.url}`,
			actionType: 2, // ActionTypeEnum.HttpApi = 2
			actionConfig: JSON.stringify(actionConfig),
			workflowId: null,
			triggerSourceId: null,
			triggerType: null,
			isAIGenerated: true, // ✨ 标记为AI生成
		};

		console.log('📝 Action params:', actionParams);

		// 调用创建Action的API
		const result = await createHttpActionAPI(actionParams);

		if (result.code === '200') {
			ElMessage.success(`HTTP Action "${actionName}" created successfully!`);
			console.log('✅ HTTP Action created:', result.data);
		} else {
			ElMessage.error(`Failed to create HTTP Action: ${result.msg || 'Unknown error'}`);
			console.error('❌ Failed to create HTTP Action:', result);
		}
	} catch (error) {
		console.error('❌ Error creating HTTP Action:', error);
		ElMessage.error('Failed to create HTTP Action. Please try again.');
	}
};

// Model management
const handleModelChange = (model: AIModelConfig) => {
	currentAIModel.value = model;
	selectedAIModel.value = model.provider.toLowerCase();
	ElMessage.success(`Switched to ${model.provider.toLowerCase()} ${model.modelName}`);
	console.log('Model changed to:', model);
};

// Initialize AI models from API
const initializeAIModels = async () => {
	try {
		// 获取认证信息
		const tokenObj = getTokenobj();
		const userStore = useUserStoreWithOut();
		const userInfo = userStore.getUserInfo;
		const globSetting = useGlobSetting();

		// 构建请求头
		const headers: Record<string, string> = {
			'Content-Type': 'application/json',
			'Time-Zone': getTimeZoneInfo().timeZone,
			'Application-code': globSetting?.ssoCode || '',
		};

		// 添加认证头
		if (tokenObj?.accessToken?.token) {
			const token = tokenObj.accessToken.token;
			const tokenType = tokenObj.accessToken.tokenType || 'Bearer';
			headers.Authorization = `${tokenType} ${token}`;
		}

		// 添加用户相关头信息
		if (userInfo?.appCode) {
			headers['X-App-Code'] = String(userInfo.appCode);
		}
		if (userInfo?.tenantId) {
			headers['X-Tenant-Id'] = String(userInfo.tenantId);
		}

		const response = await fetch('/api/ai/config/v1/models', {
			method: 'GET',
			headers,
		});

		if (response.ok) {
			const result = await response.json();
			if (result.success && result.data) {
				// Map API response to our model interface
				availableModels.value = result.data.map((model: any) => ({
					id: model.id,
					provider: model.provider,
					modelName: model.modelName,
					isAvailable: model.isAvailable === true, // Use exact boolean value from API
					isDefault: model.isDefault === true,
				}));

				console.log('Loaded AI models:', availableModels.value);

				// Set default model
				if (availableModels.value.length > 0) {
					// Try to find the default model first, then available model, then first model
					const defaultModel =
						availableModels.value.find((m) => m.isDefault && m.isAvailable) ||
						availableModels.value.find((m) => m.isAvailable) ||
						availableModels.value[0];
					currentAIModel.value = defaultModel;
					selectedAIModel.value = defaultModel.provider.toLowerCase();
				}
			} else {
				console.warn('Failed to load AI models from API:', result.message);
				loadFallbackModels();
			}
		} else {
			console.warn('API request failed:', response.status, response.statusText);
			loadFallbackModels();
		}
	} catch (error) {
		console.error('Error loading AI models:', error);
		loadFallbackModels();
	}
};

// Fallback models in case API fails
const loadFallbackModels = () => {
	availableModels.value = [
		{
			id: 1,
			provider: 'ZhipuAI',
			modelName: 'glm-4',
			isAvailable: true,
		},
		{
			id: 2,
			provider: 'OpenAI',
			modelName: 'gpt-4',
			isAvailable: true,
		},
		{
			id: 3,
			provider: 'Claude',
			modelName: 'claude-3',
			isAvailable: true,
		},
	];

	// Set default model
	if (availableModels.value.length > 0) {
		currentAIModel.value = availableModels.value[0];
		selectedAIModel.value = currentAIModel.value.provider.toLowerCase();
	}
};

// Initialize on component mount
onMounted(() => {
	initializeAIModels();
});
</script>

<style scoped lang="scss">
.http-form {
	@apply dark:border-gray-700 rounded-lg;
}

// Params Section Styles (Legacy)
.params-section {
	@apply w-full border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden;
}

// Enhanced Params Section Styles (New)
.params-section-enhanced {
	@apply w-full border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden;
}

.params-header-enhanced {
	@apply bg-gray-50 dark:bg-gray-800 px-4 py-3 border-b border-gray-200 dark:border-gray-700 grid grid-cols-12 gap-4 items-center;

	.param-col-key {
		@apply col-span-4 text-sm font-medium text-gray-600 dark:text-gray-400;
	}

	.param-col-value {
		@apply col-span-7 text-sm font-medium text-gray-600 dark:text-gray-400;
	}

	.param-actions-enhanced {
		@apply col-span-1 text-sm font-medium text-gray-600 dark:text-gray-400 text-center;
	}
}

.params-body-enhanced {
	@apply p-3 space-y-2;
}

.param-row-enhanced {
	@apply grid grid-cols-12 gap-4 items-start;
}

.param-key-container {
	@apply col-span-4;
}

.param-value-container {
	@apply col-span-7 space-y-1;
}

.param-delete-container {
	@apply col-span-1 flex justify-center;
}

.param-input-enhanced {
	@apply w-full;
}

.param-value-wrapper {
	@apply relative w-full;
}

// 输入框内部的按钮组
.param-input-actions {
	@apply flex items-center space-x-1 mr-2;
}

.param-action-btn {
	@apply w-5 h-5 flex items-center justify-center text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors;

	&:hover {
		@apply bg-gray-100 dark:bg-gray-700 rounded;
	}
}

// 文本域容器
.param-textarea-container {
	@apply relative w-full;
}

.param-textarea-enhanced {
	@apply font-mono text-sm w-full;
}

.param-textarea-auto-height {
	@apply font-mono text-sm w-full;

	:deep(.el-textarea__inner) {
		resize: none !important;
		overflow-y: hidden !important;
	}
}

// 文本域右上角的按钮组
.param-textarea-actions {
	@apply absolute top-2 right-2 flex items-center space-x-1 bg-white dark:bg-gray-800 px-2 py-1 rounded shadow-sm border border-gray-200 dark:border-gray-600;
	z-index: 10;
}

.param-value-hint {
	@apply text-xs text-gray-500 dark:text-gray-400 italic;
}

.param-delete-enhanced {
	@apply w-8 h-8 flex items-center justify-center text-red-400 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 rounded transition-colors;
}

.params-header {
	@apply bg-gray-50 dark:bg-gray-800 px-4 py-3 border-b border-gray-200 dark:border-gray-700 flex items-center gap-4;

	.param-col {
		@apply flex-1 text-sm font-medium text-gray-600 dark:text-gray-400;
	}

	.param-actions {
		@apply w-12 text-sm font-medium text-gray-600 dark:text-gray-400;
	}
}

.params-body {
	@apply p-4 space-y-3;
}

.param-row {
	@apply flex items-center gap-4;
}

.param-input {
	@apply flex-1;
}

.param-delete {
	@apply w-8 h-8 flex items-center justify-center;
}

.add-param-btn {
	@apply mt-2;
}

// Raw Section Styles
.raw-section {
	@apply w-full;
}

.raw-header {
	@apply mb-4;
}

.raw-format-controls {
	@apply flex items-center gap-3;
}

.raw-format-select {
	@apply min-w-32;
}

.format-btn {
	@apply flex items-center gap-2;

	.el-icon {
		@apply text-sm;
	}
}

.raw-textarea-container {
	@apply relative;
}

.raw-textarea {
	@apply w-full min-h-48 resize-y;
}

// Body Section Styles
.body-section {
	@apply w-full space-y-4;
}

.body-type-group {
	@apply flex flex-wrap gap-4;

	:deep(.el-radio) {
		@apply mr-0;
	}
}

.body-content {
	@apply mt-4;
}

.body-none {
	@apply py-8 text-center;
}

.raw-section {
	@apply space-y-3;
}

.raw-header {
	@apply flex justify-end;
}

.raw-format-select {
	@apply w-32;
}

.raw-textarea {
	@apply font-mono;
}

// Options Section
.options-section {
	@apply border border-gray-200 dark:border-gray-700 rounded-lg p-4;
}

.test-section {
	@apply border-t border-gray-200 dark:border-gray-700 pt-6;
}

.test-result {
	@apply mt-3;

	pre {
		@apply max-h-40 overflow-y-auto;
	}
}

:deep(.el-input-number) {
	.el-input-group__append {
		@apply bg-gray-50 dark:bg-gray-700 text-gray-600 dark:text-gray-300;
	}
}

:deep(.request-url-input .el-input .el-input__wrapper) {
	border-top-left-radius: 0 !important;
	border-bottom-left-radius: 0 !important;
}

// 导入错误信息样式
.import-error-message {
	@apply mt-4 p-4 rounded-lg border border-red-200 dark:border-red-700 bg-red-50 dark:bg-red-900/20;
}

// AI Chat 样式
.ai-import-content {
	@apply h-full;
}

.ai-chat-container {
	@apply flex flex-col h-96;
}

.ai-chat-messages {
	@apply flex-1 overflow-y-auto p-4 space-y-4 bg-gray-50 dark:bg-gray-800 rounded-lg mb-4;
	max-height: 300px;
}

.ai-message {
	@apply flex;

	&.user-message {
		@apply justify-end;

		.message-content {
			@apply bg-blue-500 text-white rounded-lg px-4 py-2 max-w-xs;
		}
	}

	&.assistant-message {
		@apply justify-start;

		.message-content {
			@apply bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 rounded-lg px-4 py-2 max-w-md border border-gray-200 dark:border-gray-600;
		}
	}
}

.message-text {
	@apply text-sm leading-relaxed;
}

.generated-config {
	@apply mt-3;
}

.config-preview {
	@apply bg-gray-50 dark:bg-gray-800 rounded-lg p-3 border border-gray-200 dark:border-gray-600;
}

.config-title {
	@apply text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2;
}

.config-details {
	@apply space-y-2 mb-3;
}

.config-item {
	@apply flex flex-col space-y-1 mb-2;
}

.config-value-input {
	@apply w-full;
}

.action-name-input {
	@apply w-full;
}

.config-item-inline {
	@apply flex items-start gap-2;
}

.config-label {
	@apply text-xs font-medium text-gray-500 dark:text-gray-400 min-w-16;
}

.config-value {
	@apply text-xs text-gray-700 dark:text-gray-300 font-mono bg-gray-100 dark:bg-gray-700 px-2 py-1 rounded;
	word-break: break-all;
	max-width: 100%;
	overflow-wrap: break-word;
}

.config-headers {
	@apply space-y-1;
}

.header-item {
	@apply text-xs text-gray-600 dark:text-gray-400 font-mono bg-gray-100 dark:bg-gray-700 px-2 py-1 rounded;
	word-break: break-all;
	max-width: 100%;
	overflow-wrap: break-word;
}

.apply-config-btn {
	@apply text-xs;
}

.typing-indicator {
	@apply inline-flex items-center space-x-1 mr-2;

	span {
		@apply w-2 h-2 bg-gray-400 rounded-full animate-pulse;

		&:nth-child(1) {
			animation-delay: 0s;
		}

		&:nth-child(2) {
			animation-delay: 0.2s;
		}

		&:nth-child(3) {
			animation-delay: 0.4s;
		}
	}
}

.ai-input-area {
	@apply border-t border-gray-200 dark:border-gray-700 pt-4;
}

.ai-input-with-button {
	@apply flex items-stretch gap-2;
}

.ai-input-container {
	@apply flex-1 relative;
}

.ai-chat-input {
	@apply w-full;

	:deep(.el-textarea__inner) {
		resize: none;
		line-height: 1.5;
		min-height: 70px !important;
		height: 70px !important;
		border-radius: 12px;
		border: 1px solid #d1d5db;
		padding: 12px 80px 12px 16px;
		font-size: 14px;
		transition: all 0.2s ease;

		&:focus {
			border-color: #4f46e5;
			box-shadow: 0 0 0 3px rgba(79, 70, 229, 0.1);
		}
	}
}

.input-bottom-actions {
	@apply absolute bottom-2 left-3 z-10;
}

.ai-model-selector-bottom {
	@apply flex items-center gap-2 flex-shrink-0;
}

.input-bottom-actions .ai-model-selector-bottom {
	@apply flex items-center gap-2;
}

.input-bottom-actions .model-select {
	@apply w-36;
}

.input-bottom-actions .model-select :deep(.el-input__inner) {
	@apply text-xs border-none bg-transparent p-1 shadow-none text-gray-500;
}

.input-bottom-actions .model-select :deep(.el-input__inner:focus) {
	@apply border-none shadow-none;
}

.model-option {
	@apply flex justify-between items-center w-full;
}

.model-info {
	@apply flex items-center;
}

.model-display {
	@apply text-sm text-gray-800 font-normal;
}

.model-status {
	@apply flex items-center;
}

.status-dot {
	@apply w-2 h-2 rounded-full bg-red-400;
}

.status-dot.online {
	@apply bg-green-400;
}

.status-dot.offline {
	@apply bg-red-400;
}

.input-right-actions {
	position: absolute;
	bottom: 8px;
	right: 12px;
	z-index: 10;
	display: flex;
	align-items: center;
	gap: 0.5rem;
}

.file-upload-btn {
	:deep(.el-upload) {
		@apply flex;
	}
}

.upload-button {
	@apply w-8 h-8 flex items-center justify-center text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200;
}

.input-right-actions .ai-send-button {
	width: 32px;
	height: 32px;
	min-width: 32px;
	padding: 0;
	border-radius: 50%;
	background: linear-gradient(135deg, #4f46e5 0%, #4338ca 100%);
	border: none;
	box-shadow: 0 2px 8px rgba(79, 70, 229, 0.3);
	transition: all 0.2s ease;
}

.input-right-actions .ai-send-button:hover {
	transform: translateY(-1px);
	box-shadow: 0 4px 12px rgba(79, 70, 229, 0.4);
}

.input-right-actions .ai-send-button:disabled {
	background: #d1d5db;
	box-shadow: none;
	transform: none;
}

.input-right-actions .ai-send-button .el-icon {
	font-size: 14px;
	color: white;
}

.input-right-actions .ai-send-button .send-icon {
	width: 14px;
	height: 14px;
	color: white;
	transform: rotate(-45deg);
}

.uploaded-file-display {
	@apply mt-2 p-2 bg-gray-100 dark:bg-gray-700 rounded-lg;
}

.file-info {
	@apply flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400;
}

.file-name {
	@apply flex-1 truncate;
}

.remove-file-btn {
	@apply w-6 h-6 flex items-center justify-center text-gray-400 hover:text-red-500;
}
</style>
