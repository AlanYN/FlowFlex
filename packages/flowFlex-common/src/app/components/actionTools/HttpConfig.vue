<template>
	<div class="space-y-3">
		<!-- Import Section -->
		<div class="flex justify-between items-center mt-4">
			<h4 class="text-base font-bold">HTTP Configuration</h4>
			<el-button type="primary" @click="showImportDialog" :disabled="disabled">
				Import
			</el-button>
		</div>

		<!-- HTTP Configuration Form -->
		<el-form
			:model="formConfig"
			label-width="120px"
			class="http-config-form"
			label-position="top"
			@submit.prevent
		>
			<!-- Request URL with Test Button -->
			<el-form-item label="Request URL" required class="request-url-input">
				<div class="w-full flex flex-col gap-2">
					<div class="url-input-container">
						<el-input
							v-model="url"
							placeholder="Enter URL"
							class="flex-1"
							:disabled="disabled"
						>
							<template #prepend>
								<el-select
									v-model="method"
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
						<el-button
							type="primary"
							@click="handleTest"
							:loading="testing"
							:disabled="!formConfig.url || disabled"
							class="test-button"
						>
							Test Send
						</el-button>
					</div>
					<div class="text-xs url-hint mt-1">
						Use variables like &#123;&#123;onboarding.id&#125;&#125; or
						&#123;&#123;stage.name&#125;&#125; in the URL
					</div>
				</div>
			</el-form-item>

			<!-- Tabs Section: Params, Headers, Body -->
			<el-tabs v-model="activeRequestTab" class="http-request-tabs">
				<!-- Params Tab -->
				<el-tab-pane label="Params" name="params">
					<div class="params-section-enhanced">
						<div class="params-header-enhanced">
							<div class="param-col-key">Key</div>
							<div class="param-col-value">Value</div>
							<div class="param-actions-enhanced"></div>
						</div>
						<div class="params-body-enhanced tab-content-body">
							<div
								v-for="(param, index) in formConfig.paramsList"
								:key="index"
								class="param-row-enhanced"
							>
								<div class="param-key-container">
									<el-input
										v-model="param.key"
										placeholder="Parameter key"
										@update:model-value="updateParamKey(index, $event)"
										:disabled="disabled"
									/>
								</div>
								<div class="param-value-container">
									<variable-auto-complete
										v-model="param.value"
										placeholder="type '/' to insert variables"
										@update:model-value="updateParamValue(index, $event)"
										:disabled="disabled"
									/>
								</div>
								<div class="param-delete-container">
									<el-button
										type="danger"
										link
										@click="removeParam(index)"
										:disabled="disabled"
									>
										<el-icon><Delete /></el-icon>
									</el-button>
								</div>
							</div>
						</div>
					</div>
				</el-tab-pane>

				<!-- Headers Tab -->
				<el-tab-pane label="Headers" name="headers">
					<div class="params-section-enhanced">
						<div class="params-header-enhanced">
							<div class="param-col-key">Key</div>
							<div class="param-col-value">Value</div>
							<div class="param-actions-enhanced"></div>
						</div>
						<div class="params-body-enhanced tab-content-body">
							<div
								v-for="(header, index) in formConfig.headersList"
								:key="index"
								class="param-row-enhanced"
							>
								<div class="param-key-container">
									<el-input
										v-model="header.key"
										placeholder="Header key"
										@update:model-value="updateHeaderKey(index, $event)"
										:disabled="disabled"
									/>
								</div>
								<div class="param-value-container">
									<variable-auto-complete
										v-model="header.value"
										placeholder="type '/' to insert variables"
										class=""
										@update:model-value="updateHeaderValue(index, $event)"
										:disabled="disabled"
									/>
								</div>
								<div class="param-delete-container">
									<el-button
										type="danger"
										link
										@click="removeHeader(index)"
										:disabled="disabled"
									>
										<el-icon><Delete /></el-icon>
									</el-button>
								</div>
							</div>
						</div>
					</div>
				</el-tab-pane>

				<!-- Body Tab -->
				<el-tab-pane label="Body" name="body">
					<div class="body-section">
						<!-- Body Type Selection -->
						<el-radio-group
							v-model="bodyType"
							class="body-type-group"
							:disabled="disabled"
						>
							<el-radio value="none">none</el-radio>
							<el-radio value="form-data">form-data</el-radio>
							<el-radio value="x-www-form-urlencoded">x-www-form-urlencoded</el-radio>
							<el-radio value="raw">raw</el-radio>
						</el-radio-group>

						<!-- Body Content based on type -->
						<div class="body-content tab-content-body">
							<!-- None - No content -->
							<div v-if="formConfig.bodyType === 'none'" class="body-none">
								<p class="text-sm body-none-text">This request has no body</p>
							</div>

							<!-- Form Data -->
							<div
								v-else-if="formConfig.bodyType === 'form-data'"
								class="params-section-enhanced"
							>
								<div class="params-header-enhanced flex-shrink-0">
									<div class="param-col-key">Key</div>
									<div class="param-col-value">Value</div>
									<div class="param-actions-enhanced"></div>
								</div>
								<div class="params-body-enhanced flex-1 min-h-0 overflow-y-auto">
									<div
										v-for="(item, index) in formConfig.formDataList"
										:key="index"
										class="param-row-enhanced"
									>
										<div class="param-key-container">
											<el-input
												v-model="item.key"
												placeholder="Form data key"
												@update:model-value="
													updateFormDataKey(index, $event)
												"
												:disabled="disabled"
											/>
										</div>
										<div class="param-value-container">
											<variable-auto-complete
												v-model="item.value"
												placeholder="type '/' to insert variables"
												class=""
												@update:model-value="
													updateFormDataValue(index, $event)
												"
												:disabled="disabled"
											/>
										</div>
										<div class="param-delete-container">
											<el-button
												type="danger"
												link
												@click="removeFormData(index)"
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
								<div class="params-header-enhanced flex-shrink-0">
									<div class="param-col-key">Key</div>
									<div class="param-col-value">Value</div>
									<div class="param-actions-enhanced"></div>
								</div>
								<div class="params-body-enhanced flex-1 min-h-0 overflow-y-auto">
									<div
										v-for="(item, index) in formConfig.urlEncodedList"
										:key="index"
										class="param-row-enhanced"
									>
										<div class="param-key-container">
											<el-input
												v-model="item.key"
												placeholder="URL encoded key"
												:disabled="disabled"
												@update:model-value="
													updateUrlEncodedKey(index, $event)
												"
											/>
										</div>
										<div class="param-value-container">
											<variable-auto-complete
												v-model="item.value"
												placeholder="type '/' to insert variables"
												class=""
												@update:model-value="
													updateUrlEncodedValue(index, $event)
												"
												:disabled="disabled"
											/>
										</div>
										<div class="param-delete-container">
											<el-button
												type="danger"
												link
												@click="removeUrlEncoded(index)"
												v-if="formConfig.urlEncodedList.length > 1"
												:disabled="disabled"
												:icon="Delete"
											/>
										</div>
									</div>
								</div>
							</div>

							<!-- Raw - with format selection -->
							<div
								v-else-if="formConfig.bodyType === 'raw'"
								class="raw-section h-full flex flex-col"
							>
								<div class="raw-header flex-shrink-0">
									<div class="raw-format-controls">
										<el-select
											v-model="rawFormat"
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
											:disabled="disabled || !body.trim()"
											class="format-btn"
											:icon="DocumentCopy"
										>
											Beautify
										</el-button>
									</div>
								</div>
								<div class="raw-textarea-container flex-1 min-h-0 overflow-y-auto">
									<variable-auto-complete
										v-model="body"
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
				</el-tab-pane>
			</el-tabs>

			<!-- Response Section -->
			<el-tabs v-model="activeResponseTab">
				<el-tab-pane label="Response" name="response">
					<div v-if="testResult" class="w-full">
						<JsonResultRenderer
							:output-type="responseOutputType"
							:max-height="'500px'"
							:enable-syntax-highlight="true"
						/>
					</div>
					<div v-else class="response-empty">
						<div class="response-empty-content">
							<p class="text-sm text-text-secondary">
								Click Test Send to get a response
							</p>
						</div>
					</div>
				</el-tab-pane>
			</el-tabs>
		</el-form>
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
					<div class="space-y-4">
						<div class="curl-info-banner">
							<div class="flex items-start space-x-3">
								<div class="flex-shrink-0">
									<Icon
										icon="heroicons:information-circle"
										class="w-5 h-5 info-icon mt-0.5"
									/>
								</div>
								<div>
									<h4 class="text-sm font-medium info-title mb-1">
										How to use cURL Import
									</h4>
									<p class="text-sm info-text">
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
										class="w-5 h-5 error-icon mt-0.5"
									/>
								</div>
								<div class="flex-1">
									<h4 class="text-sm font-medium error-title mb-1">
										Import Failed
									</h4>
									<p class="text-sm error-text">
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
										/>
										<div class="input-bottom-actions">
											<div class="ai-model-selector-bottom">
												<el-select
													:model-value="currentAIModel || undefined"
													@update:model-value="handleModelChange"
													placeholder="Select AI Model"
													size="small"
													class="model-select"
													style="width: 180px"
													value-key="id"
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
											link
											size="small"
											@click="removeUploadedFile"
											:icon="Close"
										/>
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
	</div>
</template>

<script setup lang="ts">
import { computed, ref, nextTick, onMounted } from 'vue';
import { Delete, DocumentCopy, Paperclip, Document, Close } from '@element-plus/icons-vue';
import { ElMessage, ElMessageBox } from 'element-plus';
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
import { getAppCode } from '@/utils/threePartyLogin';
import JsonResultRenderer from './JsonResultRenderer.vue';
import type { OutputType } from '@/utils/output-type-detector';

// External library loaders (CDN-based to avoid local dependency issues)
const PDF_JS_VERSION = '3.11.174';
const MAMMOTH_VERSION = '1.6.0';

let pdfJsLoadingPromise: Promise<any> | null = null;
let mammothLoadingPromise: Promise<any> | null = null;

// Helper to remove failed scripts
const removeFailedScripts = (src: string) => {
	const scripts = Array.from(document.getElementsByTagName('script')).filter(
		(s) => s.src === src
	);
	scripts.forEach((script) => {
		try {
			document.head.removeChild(script);
		} catch (e) {
			// Script removal failed, continue silently
		}
	});
};

const loadScriptOnce = (src: string, timeout: number = 10000): Promise<void> => {
	return new Promise((resolve, reject) => {
		// Check if script already exists and is loaded
		const existing = Array.from(document.getElementsByTagName('script')).find(
			(s) => s.src === src
		);

		if (existing) {
			if ((existing as any)._loaded) {
				resolve();
				return;
			} else {
				// Wait for existing script to load
				existing.addEventListener('load', () => {
					resolve();
				});
				existing.addEventListener('error', () => {
					reject(new Error(`Failed to load existing script: ${src}`));
				});
				return;
			}
		}

		const script = document.createElement('script');
		script.src = src;
		script.async = true;
		script.crossOrigin = 'anonymous'; // Allow CORS for CDN scripts

		// Set up timeout
		const timeoutId = setTimeout(() => {
			document.head.removeChild(script);
			reject(new Error(`Script loading timeout: ${src}`));
		}, timeout);

		script.addEventListener('load', () => {
			clearTimeout(timeoutId);
			(script as any)._loaded = true;
			resolve();
		});

		script.addEventListener('error', (event) => {
			clearTimeout(timeoutId);
			document.head.removeChild(script);
			reject(new Error(`Failed to load script: ${src}`));
		});

		try {
			document.head.appendChild(script);
		} catch (error) {
			clearTimeout(timeoutId);
			reject(new Error(`Failed to append script to document: ${src}`));
		}
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
	if ((window as any).mammoth) {
		return (window as any).mammoth;
	}

	// Reset the loading promise if there were previous failures
	if (mammothLoadingPromise) {
		try {
			await mammothLoadingPromise;
			if ((window as any).mammoth) {
				return (window as any).mammoth;
			}
		} catch (error) {
			mammothLoadingPromise = null;

			// Clean up any failed script tags
			const mammothUrls = [
				`https://cdnjs.cloudflare.com/ajax/libs/mammoth/${MAMMOTH_VERSION}/mammoth.browser.min.js`,
				`https://unpkg.com/mammoth@${MAMMOTH_VERSION}/mammoth.browser.min.js`,
				`https://cdn.jsdelivr.net/npm/mammoth@${MAMMOTH_VERSION}/mammoth.browser.min.js`,
			];
			mammothUrls.forEach(removeFailedScripts);
		}
	}

	if (!mammothLoadingPromise) {
		mammothLoadingPromise = (async () => {
			try {
				// Temporarily disable AMD/RequireJS to avoid conflicts
				const originalDefine = (window as any).define;
				const originalRequire = (window as any).require;
				if (originalDefine) {
					(window as any).define = undefined;
				}
				if (originalRequire) {
					(window as any).require = undefined;
				}

				try {
					// Try multiple CDN sources for better reliability
					const cdnUrls = [
						`https://cdnjs.cloudflare.com/ajax/libs/mammoth/${MAMMOTH_VERSION}/mammoth.browser.min.js`,
						`https://unpkg.com/mammoth@${MAMMOTH_VERSION}/mammoth.browser.min.js`,
						`https://cdn.jsdelivr.net/npm/mammoth@${MAMMOTH_VERSION}/mammoth.browser.min.js`,
					];

					let lastError: Error | null = null;
					let loadedSuccessfully = false;

					for (const cdnUrl of cdnUrls) {
						try {
							// Check if this script is already loaded
							const existingScript = Array.from(
								document.getElementsByTagName('script')
							).find((s) => s.src === cdnUrl);

							if (!existingScript) {
								await loadScriptOnce(cdnUrl);
							}

							// Wait a bit for the library to initialize
							await new Promise((resolve) => setTimeout(resolve, 200));

							// Check if mammoth is now available
							const mammothLib = (window as any).mammoth;

							if (mammothLib && typeof mammothLib.extractRawText === 'function') {
								loadedSuccessfully = true;
								break;
							} else if (mammothLib) {
								lastError = new Error(
									`Mammoth library incomplete - missing extractRawText method`
								);
							} else {
								lastError = new Error(
									`Mammoth object not found after loading from ${cdnUrl}`
								);
							}
						} catch (error) {
							lastError =
								error instanceof Error
									? error
									: new Error(`Failed to load from ${cdnUrl}: ${String(error)}`);
							continue;
						}
					}

					if (!loadedSuccessfully && !(window as any).mammoth) {
						throw new Error(
							`Failed to load mammoth library from all CDN sources. Last error: ${
								lastError?.message || 'Unknown error'
							}`
						);
					}

					return (window as any).mammoth;
				} finally {
					// Restore AMD/RequireJS
					if (originalDefine) {
						(window as any).define = originalDefine;
					}
					if (originalRequire) {
						(window as any).require = originalRequire;
					}
				}
			} catch (error) {
				throw new Error(
					'Unable to load document processing library. Please check your internet connection and try again.'
				);
			}
		})();
	}
	return mammothLoadingPromise;
};

interface KeyValueItem {
	key: string;
	value: string;
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
	'update:actionName': [name: string];
	'ai-config-applied': [config: any];
	test: [];
}>();

// Tabs state
const activeRequestTab = ref('params');
const activeResponseTab = ref('response');

// Response output type for JsonResultRenderer
const responseOutputType = computed<OutputType>(() => {
	const result = props.testResult?.stdout || props.testResult;
	return {
		type: 'json',
		data: result,
	};
});

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

// 为 bodyType 创建单独的 computed，支持 v-model 双向绑定
const bodyType = computed({
	get() {
		return formConfig.value.bodyType;
	},
	set(val: 'none' | 'form-data' | 'x-www-form-urlencoded' | 'raw') {
		formConfig.value = { ...formConfig.value, bodyType: val };
	},
});

// 为 url 创建单独的 computed，支持 v-model 双向绑定
const url = computed({
	get() {
		return formConfig.value.url;
	},
	set(val: string) {
		formConfig.value = { ...formConfig.value, url: val };
	},
});

// 为 method 创建单独的 computed，支持 v-model 双向绑定
const method = computed({
	get() {
		return formConfig.value.method;
	},
	set(val: string) {
		formConfig.value = { ...formConfig.value, method: val };
	},
});

// 为 rawFormat 创建单独的 computed，支持 v-model 双向绑定
const rawFormat = computed({
	get() {
		return formConfig.value.rawFormat;
	},
	set(val: string) {
		formConfig.value = { ...formConfig.value, rawFormat: val };
	},
});

const body = computed({
	get() {
		return formConfig.value.body;
	},
	set(val: string) {
		formConfig.value = { ...formConfig.value, body: val };
	},
});

// Controlled setters to trigger computed.set via whole-object assignment
// Filter stream content to show only essential progress information
const filterStreamContent = (content: string): string => {
	// 过滤掉所有JSON内容和代码块
	if (
		content.includes('```json') ||
		content.includes('```') ||
		content.includes('{') ||
		content.includes('}') ||
		content.includes('[') ||
		content.includes(']') ||
		content.includes('"') ||
		content.match(/\s*"[\w]+"\s*:/) // JSON键值对格式
	) {
		return '';
	}

	// 过滤掉包含特定关键词的详细内容
	const skipKeywords = [
		'actionItems',
		'keyInsights',
		'nextSteps',
		'stakeholders',
		'description',
		'category',
		'priority',
		'assignedTo',
		'dependencies',
		'tags',
		'actionPlan',
		'implementation',
		'riskFactors',
		'successMetrics',
		'confidenceScore',
		'Creating action plan',
		'Processing',
		'json',
	];

	// 如果内容包含要跳过的关键词，直接过滤
	if (skipKeywords.some((keyword) => content.toLowerCase().includes(keyword.toLowerCase()))) {
		return '';
	}

	// 只保留简短的关键进度信息
	const progressKeywords = [
		'Initializing',
		'Analysis completed',
		'Now creating',
		'Generated successfully',
		'Starting',
		'Completed',
		'✅',
		'🔄',
	];

	// 过滤掉重复的"Generating..."消息
	if (content.trim() === 'Generating...' || content.trim() === 'Generating') {
		return '';
	}

	// 进一步简化内容
	if (content.includes('Analysis completed')) {
		return '✅ Analysis completed\n';
	}
	if (content.includes('Now creating')) {
		return '🔄 Creating HTTP configuration...\n';
	}
	if (content.includes('Generated successfully')) {
		return '✅ HTTP configuration generated successfully!\n';
	}
	if (content.includes('Initializing')) {
		return '🚀 Initializing analysis...\n';
	}
	if (content.includes('Generating HTTP configuration')) {
		return '🔄 Processing...\n';
	}

	// 只保留包含关键进度信息且简短的内容
	if (progressKeywords.some((keyword) => content.includes(keyword))) {
		if (content.length <= 50) {
			return content + '\n';
		}
		return '';
	}

	// 过滤掉过长的内容
	if (content.length > 50) {
		return '';
	}

	// 过滤掉只包含空白字符的内容
	if (content.trim() === '') {
		return '';
	}

	return content.includes('.') || content.includes('!') ? content + '\n' : '';
};

// Update action name for generated HTTP config
const updateActionName = (message: any, newName: string) => {
	if (message.httpConfig) {
		message.httpConfig.actionName = newName;
	}
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
					ElMessage.warning(t('sys.api.formatInvalidJson'));
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

		formConfig.value = { ...formConfig.value, body: formattedContent } as any;
		ElMessage.success(t('sys.api.formatSuccess'));
	} catch (error) {
		ElMessage.warning(t('sys.api.formatFailed'));
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
const maxFileSize = 5 * 1024 * 1024; // 5MB

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
const handleAIKeydown = (event: KeyboardEvent | Event): any => {
	const keyboardEvent = event as KeyboardEvent;
	if (keyboardEvent.key === 'Enter') {
		if (keyboardEvent.shiftKey) {
			// Shift+Enter: Allow default behavior (new line)
			return;
		} else {
			// Enter: Send message
			keyboardEvent.preventDefault();
			if ((aiCurrentInput.value.trim() || uploadedFile.value) && !aiGenerating.value) {
				sendAIMessage();
			}
		}
	}
};

const sendAIMessage = async () => {
	if (!aiCurrentInput.value.trim() && !uploadedFile.value) {
		return;
	}

	const userMessage = {
		role: 'user' as const,
		content: aiCurrentInput.value.trim(),
		timestamp: new Date().toISOString(),
	};

	// 如果有上传的文件，解析文件内容并添加到消息中
	if (uploadedFile.value) {
		try {
			// Show a loading message for DOCX files since they take longer
			const isDocx = uploadedFile.value.name.toLowerCase().endsWith('.docx');
			if (isDocx) {
				ElMessage.info({
					message: 'Processing DOCX file, this may take a moment...',
					duration: 3000,
				});
			}

			const fileContent = await readFileContent(uploadedFile.value);
			// 对于 cURL 文件，不截断内容以确保完整的 JSON 数据能够被解析
			const shouldTruncate = uploadedFile.value.name.toLowerCase().includes('curl')
				? false
				: fileContent.length > 1000;
			const truncatedContent = shouldTruncate
				? fileContent.substring(0, 1000) + '\n\n[Content truncated for display...]'
				: fileContent;
			userMessage.content += `\n\n📎 **File Content** (${uploadedFile.value.name}):\n\`\`\`\n${truncatedContent}\n\`\`\``;

			ElMessage.success({
				message: `File "${uploadedFile.value.name}" processed successfully!`,
				duration: 2000,
			});
		} catch (error) {
			// Provide specific error handling for different file types
			const errorMessage = error instanceof Error ? error.message : 'Unknown error';
			const fileName = uploadedFile.value.name;
			const fileExt = fileName.toLowerCase().split('.').pop();

			let userFriendlyMessage = errorMessage;

			// Special handling for DOCX files
			if (fileExt === 'docx' || fileExt === 'doc') {
				if (errorMessage.includes('library') || errorMessage.includes('mammoth')) {
					userFriendlyMessage =
						'Unable to process Word document due to library loading issues. Please try: 1) Converting to PDF or plain text format, 2) Refreshing the page and trying again, or 3) Copying the text directly into the input field.';
				} else if (errorMessage.includes('corrupted') || errorMessage.includes('format')) {
					userFriendlyMessage =
						'Document file appears to be corrupted or in an unsupported format. Please try re-saving the document or converting it to PDF format.';
				} else if (errorMessage.includes('AMD') || errorMessage.includes('define')) {
					userFriendlyMessage =
						'Document processing conflict detected. Please try refreshing the page or converting the document to PDF format.';
				}
			}

			userMessage.content += `\n\n📎 **File** (${fileName}): ❌ ${userFriendlyMessage}`;

			ElMessage.error({
				message: `Failed to process ${fileName}: ${userFriendlyMessage}`,
				duration: 5000,
				showClose: true,
			});
		}
	}

	aiChatMessages.value.push(userMessage);

	const currentInput = aiCurrentInput.value.trim();
	aiCurrentInput.value = '';
	aiGenerating.value = true;

	// 滚动到底部
	nextTick(() => {
		if (chatMessagesRef.value) {
			chatMessagesRef.value.scrollTop = chatMessagesRef.value.scrollHeight;
		}
	});

	try {
		// 关闭加载状态，开始显示流式内容
		aiGenerating.value = false;

		// 使用流式响应处理AI请求
		await processAIRequestWithStreaming(currentInput, uploadedFile.value);
	} catch (error) {
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

// 流式处理AI请求 - 使用优化的单步流程
const processAIRequestWithStreaming = async (input: string, file: File | null) => {
	// 创建一个助手消息用于显示流式内容
	const assistantMessage = {
		role: 'assistant' as const,
		content: '🚀 Generating HTTP configuration...',
		timestamp: new Date().toISOString(),
		httpConfig: null as any,
	};
	aiChatMessages.value.push(assistantMessage);

	let streamingContent = '';

	try {
		// 使用新的优化端点直接生成HTTP配置
		await streamGenerateHttpConfigDirect(input, file, (chunk, data) => {
			// 统一处理大小写问题
			const chunkType = chunk.type || chunk.Type;
			const chunkContent = chunk.content || chunk.Content;
			const chunkActionData = chunk.actionData || chunk.ActionData;

			if (chunkType === 'progress' || chunkType === 'generation') {
				// 过滤并显示简洁的进度信息
				const filteredContent = filterStreamContent(chunkContent);
				if (filteredContent && !streamingContent.includes(filteredContent.trim())) {
					streamingContent += filteredContent;

					// 更新UI显示
					const lastMessageIndex = aiChatMessages.value.length - 1;
					if (lastMessageIndex >= 0) {
						const baseContent = '🚀 Generating HTTP configuration...\n\n';
						const fullContent = baseContent + streamingContent;
						aiChatMessages.value[lastMessageIndex].content = fullContent;
					}

					// 实时滚动到底部
					nextTick(() => {
						if (chatMessagesRef.value) {
							chatMessagesRef.value.scrollTop = chatMessagesRef.value.scrollHeight;
						}
					});
				}
			} else if (chunkType === 'complete' && chunkActionData) {
				// 直接从响应中提取HTTP配置
				const httpConfig = extractHttpConfigFromActionPlan(chunkActionData);

				// 更新最终消息
				const lastMessageIndex = aiChatMessages.value.length - 1;
				if (lastMessageIndex >= 0) {
					aiChatMessages.value[lastMessageIndex].httpConfig = httpConfig;
					aiChatMessages.value[lastMessageIndex].content =
						'🚀 Generating HTTP configuration...\n\n✅ HTTP configuration generated successfully!';
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
		// 直接修改数组中的最后一个消息（助手消息）
		const lastMessageIndex = aiChatMessages.value.length - 1;
		if (lastMessageIndex >= 0) {
			aiChatMessages.value[lastMessageIndex].content = `❌ Error: ${
				error instanceof Error ? error.message : 'Unknown error'
			}`;
		}
		throw error;
	}
};

// 优化的单步HTTP配置生成
const streamGenerateHttpConfigDirect = async (
	input: string,
	file: File | null,
	onChunk: (chunk: any, data?: any) => void
) => {
	// 构建优化的请求负载
	let context = 'Generate HTTP API configuration directly from user input';
	let fileContent = '';
	let fileName = '';

	// 如果有文件，读取文件内容
	if (file) {
		try {
			fileContent = await readFileContent(file);
			fileName = file.name;
		} catch (error) {
			// Error reading file, continue without file content
		}
	}

	const payload = {
		userInput: input,
		context,
		fileContent,
		fileName,
		sessionId: `http_config_${Date.now()}`,
		outputFormat: 'direct_config',
		skipDetailedAnalysis: true,
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
	headers['X-App-Code'] = getAppCode();
	if (userInfo?.tenantId) {
		headers['X-Tenant-Id'] = String(userInfo.tenantId);
	}

	// 使用新的专用端点
	return new Promise<void>((resolve, reject) => {
		fetch('/api/ai/v1/http-config/generate/stream', {
			method: 'POST',
			headers: headers,
			body: JSON.stringify(payload),
		})
			.then((response) => {
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
						for (;;) {
							const { done, value } = await reader.read();
							if (done) {
								break;
							}

							const chunk = decoder.decode(value, { stream: true });
							const lines = chunk.split('\n');

							for (const line of lines) {
								if (line.startsWith('data: ')) {
									const data = line.substring(6);

									if (data === '[DONE]') {
										resolve();
										return;
									}

									try {
										const parsed = JSON.parse(data);

										// 检查并应用HTTP配置 (仅在HTTP配置生成流程中)
										if (parsed.type === 'complete' && parsed.actionData) {
											// 在流式处理完成时自动应用配置
											const httpConfig = extractHttpConfigFromActionPlan(
												parsed.actionData
											);
											if (httpConfig) {
												try {
													await applyGeneratedConfig(httpConfig);
												} catch (error) {
													// 不要阻止流继续处理，只记录错误
												}
											} else {
												// 如果无法提取有效的HTTP配置，给出说明和建议
												showConfigurationSuggestions(parsed.actionData);
											}
										} else if (
											parsed.type === 'complete' &&
											!parsed.actionData
										) {
											// 如果完成但没有actionData，也给出建议
											showNoConfigurationDataSuggestions();
										}

										onChunk(parsed);

										if (parsed.type === 'complete') {
											resolve();
											return;
										} else if (parsed.type === 'error') {
											reject(
												new Error(
													parsed.content ||
														parsed.message ||
														'Stream processing error'
												)
											);
											return;
										}
									} catch (e) {
										// Skip invalid JSON but continue processing
										continue;
									}
								}
							}
						}
						resolve();
					} catch (error) {
						reject(error);
					}
				};

				readStream();
			})
			.catch((error) => {
				reject(error);
			});
	});
};

// 解析curl命令的函数
const parseCurlCommand = (input: string) => {
	const config: any = {
		method: 'GET',
		url: '',
		headers: {},
		bodyType: 'none',
		body: '',
	};

	// 查找curl命令 - 支持多种引号格式
	const curlMatch = input.match(
		/curl\s+[^'"^]*['"^]([^'"^]+)['"^]|curl\s+--location\s+['"^]([^'"^]+)['"^]|curl\s+([^\s\\]+)/
	);
	if (curlMatch) {
		config.url = curlMatch[1] || curlMatch[2] || curlMatch[3];
	}

	// 解析HTTP方法 - 支持引号格式
	const methodMatch = input.match(/(?:--request|-X)\s+['"^]*(\w+)['"^]*/i);
	if (methodMatch) {
		config.method = methodMatch[1].toUpperCase();
	} else {
		// 默认GET，除非有数据
		config.method = input.includes('--data') ? 'POST' : 'GET';
	}

	// 解析headers - 支持 -H 和 --header 两种格式
	const headerMatches = input.matchAll(/(?:--header|-H)\s+['"^]*([^'"\n\r^]+)['"^]*/g);
	for (const match of headerMatches) {
		const headerValue = match[1];
		if (headerValue) {
			const colonIndex = headerValue.indexOf(':');
			if (colonIndex > 0) {
				const key = headerValue.substring(0, colonIndex).trim();
				const value = headerValue.substring(colonIndex + 1).trim();
				if (key && value) {
					config.headers[key] = value;
				}
			}
		}
	}

	// 解析请求体 - 支持Windows cURL的特殊引号格式
	let bodyContent = '';

	// 查找 --data-raw 或 --data 的位置
	const dataRawMatch = input.match(/--data-raw\s+['"^]*([^]*)/);
	const dataMatch = input.match(/--data\s+['"^]*([^]*)/);

	let dataContent = '';
	if (dataRawMatch) {
		dataContent = dataRawMatch[1];
	} else if (dataMatch) {
		dataContent = dataMatch[1];
	}

	if (dataContent) {
		// 处理Windows cURL的特殊引号格式（^"...^"）和普通引号
		let cleanContent = dataContent;

		// 移除开头的转义字符和引号
		cleanContent = cleanContent.replace(/^[\s^]*["']/, '');

		// 移除结尾的转义字符和引号
		cleanContent = cleanContent.replace(/["'][\s^]*$/, '');

		// 移除结尾可能的额外空白和特殊字符
		cleanContent = cleanContent.trim();

		// 替换 Windows cURL 的转义字符
		cleanContent = cleanContent.replace(/\^\^/g, '^');
		cleanContent = cleanContent.replace(/\^"/g, '"');
		cleanContent = cleanContent.replace(/\^'/g, "'");

		bodyContent = cleanContent;
	}

	if (bodyContent.trim()) {
		config.body = bodyContent.trim();
		config.bodyType = 'raw';

		// 尝试检测JSON格式 - 先清理换行符
		try {
			// 尝试解析原始内容
			JSON.parse(config.body);
			config.rawFormat = 'json';
		} catch (error) {
			// 如果失败，尝试清理 Windows 换行符后再解析
			try {
				const cleanedBody = config.body.replace(/\r\n/g, '\n').replace(/\r/g, '\n');
				JSON.parse(cleanedBody);
				config.rawFormat = 'json';
			} catch (secondError) {
				// 检查是否看起来像 JSON（以 { 或 [ 开头）
				const trimmedBody = config.body.trim();
				if (trimmedBody.startsWith('{') || trimmedBody.startsWith('[')) {
					config.rawFormat = 'json';
				} else {
					config.rawFormat = 'text';
				}
			}
		}
	}

	return config;
};

const extractHttpConfigFromActionPlan = (actionPlan: any) => {
	// 从AI生成的行动计划中提取HTTP配置信息
	if (!actionPlan || typeof actionPlan !== 'object') {
		return null;
	}

	// 检查新的标准格式：{ actionPlan: { actions: [{ httpConfig: ... }] } }
	if (actionPlan.actionPlan?.actions) {
		const actions = actionPlan.actionPlan.actions;
		if (Array.isArray(actions) && actions.length > 0) {
			for (const action of actions) {
				if (action.httpConfig) {
					const config = action.httpConfig;
					// 确保actionName存在
					if (!config.actionName && config.url) {
						config.actionName = generateActionName(config.url, config.method || 'GET');
					}
					return config;
				}
			}
		}
	}

	// 检查简化格式：{ actions: [{ httpConfig: ... }] }
	if (actionPlan.actions) {
		const actions = actionPlan.actions;
		if (Array.isArray(actions) && actions.length > 0) {
			for (const action of actions) {
				if (action.httpConfig) {
					const config = action.httpConfig;
					// 确保actionName存在
					if (!config.actionName && config.url) {
						config.actionName = generateActionName(config.url, config.method || 'GET');
					}
					return config;
				}
			}
		}
	}

	// 检查直接httpConfig格式：{ httpConfig: ... }
	if (actionPlan.httpConfig) {
		const config = actionPlan.httpConfig;
		// 确保actionName存在
		if (!config.actionName && config.url) {
			config.actionName = generateActionName(config.url, config.method || 'GET');
		}
		return config;
	}

	// 首先尝试从用户的原始输入中解析curl命令（作为后备方案）
	const userInput = aiChatMessages.value.find((msg) => msg.role === 'user')?.content || '';

	const curlConfig = parseCurlCommand(userInput);
	if (curlConfig.url && curlConfig.url !== '') {
		// 添加默认的action名称
		curlConfig.actionName = generateActionName(curlConfig.url, curlConfig.method);
		return curlConfig;
	}

	// 如果以上都失败，回退到从旧格式AI响应中解析
	const actions = actionPlan.ActionItems || actionPlan.actions || [];

	// 查找包含HTTP配置信息的行动项目
	const httpAction = actions.find(
		(action: any) =>
			action.title?.toLowerCase().includes('http') ||
			action.title?.toLowerCase().includes('api') ||
			action.title?.toLowerCase().includes('request') ||
			action.description?.toLowerCase().includes('request') ||
			action.description?.toLowerCase().includes('endpoint') ||
			action.description?.toLowerCase().includes('api') ||
			action.category?.toLowerCase().includes('api')
	);

	if (httpAction) {
		// 尝试从描述中解析HTTP配置
		const title = httpAction.title || '';
		const description = httpAction.description || '';
		const fullText = `${title} ${description}`.toLowerCase();

		// 初始化配置对象
		const config: any = {
			method: 'GET',
			url: '',
			headers: {},
			bodyType: 'none',
			body: '',
		};

		// 解析HTTP方法 - 更强健的正则表达式
		const methodMatch = fullText.match(/\b(get|post|put|delete|patch)\b/i);
		if (methodMatch) {
			config.method = methodMatch[1].toUpperCase();
		}

		// 解析URL - 改进的URL匹配模式
		const urlPatterns = [
			/(?:url|endpoint|api):\s*(https?:\/\/[^\s\n]+)/i, // url: http://...
			/(?:to|at)\s+(https?:\/\/[^\s\n]+)/i, // to/at http://...
			/(https?:\/\/[^\s\n]+)/i, // 任何 http://...
			/(?:\/[a-zA-Z0-9\-_/]+)/, // 相对路径 /api/...
		];

		for (const pattern of urlPatterns) {
			const urlMatch = fullText.match(pattern);
			if (urlMatch) {
				config.url = urlMatch[1] || urlMatch[0];
				break;
			}
		}

		// 如果没有找到完整URL，检查是否有路径可以构建
		if (!config.url || config.url === '') {
			// 查找API路径
			const pathMatch =
				fullText.match(/\/api\/[^\s\n]*/i) || fullText.match(/\/[a-zA-Z0-9\-_/]+/);
			if (pathMatch) {
				// 只有找到明确的API路径才构建URL
				config.url = `https://api.example.com${pathMatch[0]}`;
			} else {
				// 根据用户要求，不提供默认URL
				return null;
			}
		}

		// 解析Content-Type
		if (fullText.includes('json')) {
			config.headers['Content-Type'] = 'application/json';
			if (config.method !== 'GET') {
				config.bodyType = 'raw';
				config.rawFormat = 'json';
			}
		} else if (fullText.includes('form')) {
			config.headers['Content-Type'] = 'application/x-www-form-urlencoded';
			if (config.method !== 'GET') {
				config.bodyType = 'x-www-form-urlencoded';
			}
		}

		// 解析认证头
		if (
			fullText.includes('authorization') ||
			fullText.includes('token') ||
			fullText.includes('auth')
		) {
			config.headers['Authorization'] = 'Bearer {{token}}';
		}

		// 添加一些常用的默认头部
		if (!config.headers.Accept) {
			config.headers.Accept = 'application/json';
		}

		// 添加默认的action名称
		config.actionName = generateActionName(config.url, config.method);
		return config;
	}

	// 根据用户要求，不返回默认配置，而是返回null
	return null;
};

// Enhanced file content reading functions
const isValidFileType = (file: File): boolean => {
	// Check file extension
	const extension = file.name.toLowerCase().split('.').pop();
	const isExtensionSupported = supportedFormats.value.includes(extension || '');

	// Additional MIME type validation for better reliability
	const supportedMimeTypes = [
		'text/plain',
		'text/markdown',
		'application/json',
		'text/csv',
		'application/pdf',
		'application/vnd.openxmlformats-officedocument.wordprocessingml.document', // .docx
		'application/msword', // .doc
		'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', // .xlsx
		'application/vnd.ms-excel', // .xls
	];

	const isMimeTypeSupported = supportedMimeTypes.includes(file.type) || file.type === '';

	return isExtensionSupported && (isMimeTypeSupported || file.type === '');
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
		throw new Error('Failed to parse PDF file. Please ensure the file is not corrupted.');
	}
};

const readDocxFile = async (file: File): Promise<string> => {
	try {
		const mammoth = await loadMammoth();

		// Validate file before processing
		if (!file || file.size === 0) {
			throw new Error('File is empty or corrupted');
		}

		// Check file extension
		const fileName = file.name.toLowerCase();
		if (!fileName.endsWith('.docx') && !fileName.endsWith('.doc')) {
			throw new Error('Invalid file format. Expected .docx or .doc file');
		}

		const arrayBuffer = await new Promise<ArrayBuffer>((resolve, reject) => {
			const reader = new FileReader();
			reader.onload = (e) => {
				const result = e.target?.result;
				if (!result) {
					reject(new Error('Failed to read file content'));
					return;
				}
				resolve(result as ArrayBuffer);
			};
			reader.onerror = () => reject(new Error('Failed to read DOCX file from disk'));
			reader.onabort = () => reject(new Error('File reading was aborted'));
			reader.readAsArrayBuffer(file);
		});

		// Validate array buffer
		if (!arrayBuffer || arrayBuffer.byteLength === 0) {
			throw new Error('File content is empty or corrupted');
		}

		// Extract text using mammoth
		const result = await mammoth.extractRawText({ arrayBuffer });

		// Check for critical errors in messages
		if (result.messages && result.messages.length > 0) {
			const errors = result.messages.filter((msg) => msg.type === 'error');
			if (errors.length > 0) {
				// Critical errors found, but continue processing
			}
		}

		const extractedText = result.value || '';

		if (!extractedText.trim()) {
			throw new Error('No readable text content found in the DOCX file');
		}

		return extractedText;
	} catch (error) {
		// Provide more specific error messages
		if (error instanceof Error) {
			if (error.message.includes('mammoth')) {
				throw new Error(
					'Failed to load document processing library. Please try again or use a different file format.'
				);
			} else if (error.message.includes('arrayBuffer') || error.message.includes('buffer')) {
				throw new Error(
					'File appears to be corrupted or in an unsupported format. Please check the file and try again.'
				);
			} else if (error.message.includes('empty') || error.message.includes('content')) {
				throw new Error('The document appears to be empty or contains no readable text.');
			} else {
				throw new Error(`DOCX processing failed: ${error.message}`);
			}
		} else {
			throw new Error(
				'Failed to parse DOCX file. Please ensure the file is not corrupted and try again.'
			);
		}
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
		throw new Error(`File size exceeds 5MB limit. Current size: ${formatFileSize(file.size)}`);
	}

	// Check if file is empty
	if (file.size === 0) {
		throw new Error(`File "${file.name}" is empty`);
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
			case 'doc':
				content = await readDocxFile(file);
				break;
			default:
				throw new Error(`Unsupported file type: ${extension}`);
		}

		if (!content || !content.trim()) {
			throw new Error(
				`No readable content found in the ${extension?.toUpperCase()} file. The file may be empty, corrupted, or in an unsupported format.`
			);
		}

		return content.trim();
	} catch (error) {
		// Re-throw with more specific context if needed
		if (error instanceof Error) {
			// If it's already a user-friendly error, pass it through
			if (
				error.message.includes('Failed to') ||
				error.message.includes('appears to be') ||
				error.message.includes('processing failed') ||
				error.message.includes('No readable content')
			) {
				throw error;
			} else {
				// Wrap technical errors with user-friendly messages
				throw new Error(
					`Failed to process ${extension?.toUpperCase()} file: ${error.message}`
				);
			}
		} else {
			throw new Error(
				`Failed to process ${extension?.toUpperCase()} file: Unknown error occurred`
			);
		}
	}
};

const handleFileUpload = async (file: File) => {
	// Validate file type with detailed feedback
	if (!isValidFileType(file)) {
		const extension = file.name.toLowerCase().split('.').pop();
		ElMessage.error({
			message: `Unsupported file type: "${extension}". Supported formats: ${supportedFormats.value
				.join(', ')
				.toUpperCase()}`,
			duration: 5000,
			showClose: true,
		});
		return false;
	}

	// Validate file size
	if (file.size > maxFileSize) {
		ElMessage.error({
			message: `File size exceeds 5MB limit. Current size: ${formatFileSize(
				file.size
			)}. Please compress or select a smaller file.`,
			duration: 5000,
			showClose: true,
		});
		return false;
	}

	// Check if file is empty
	if (file.size === 0) {
		ElMessage.error({
			message: `File "${file.name}" appears to be empty. Please select a valid file.`,
			duration: 5000,
			showClose: true,
		});
		return false;
	}

	// Store the file
	uploadedFile.value = file;

	// Provide success feedback with file details
	const extension = file.name.split('.').pop()?.toUpperCase();
	ElMessage.success({
		message: `File "${file.name}" (${extension}, ${formatFileSize(
			file.size
		)}) selected successfully!`,
		duration: 3000,
		showClose: true,
	});

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

const validateHttpConfig = (httpConfig: any): { isValid: boolean; errors: string[] } => {
	const errors: string[] = [];

	if (!httpConfig || typeof httpConfig !== 'object') {
		errors.push('Configuration must be a valid object');
		return { isValid: false, errors };
	}

	// Validate URL
	if (!httpConfig.url || typeof httpConfig.url !== 'string' || httpConfig.url.trim() === '') {
		errors.push('URL is required and must be a non-empty string');
	} else {
		const url = httpConfig.url.trim();
		if (!url.match(/^https?:\/\//) && !url.startsWith('/')) {
			errors.push(
				'URL must start with http://, https://, or be a relative path starting with /'
			);
		}
	}

	// Validate method
	if (!httpConfig.method || typeof httpConfig.method !== 'string') {
		errors.push('HTTP method is required');
	} else {
		const validMethods = ['GET', 'POST', 'PUT', 'DELETE', 'PATCH', 'HEAD', 'OPTIONS'];
		if (!validMethods.includes(httpConfig.method.toUpperCase())) {
			errors.push(`HTTP method must be one of: ${validMethods.join(', ')}`);
		}
	}

	// Validate headers
	if (httpConfig.headers && typeof httpConfig.headers !== 'object') {
		errors.push('Headers must be an object');
	}

	// Validate timeout
	if (httpConfig.timeout !== undefined) {
		if (typeof httpConfig.timeout !== 'number' || httpConfig.timeout <= 0) {
			errors.push('Timeout must be a positive number');
		}
	}

	// Validate actionName
	if (httpConfig.actionName && typeof httpConfig.actionName !== 'string') {
		errors.push('Action name must be a string');
	} else if (httpConfig.actionName && !httpConfig.actionName.match(/^[a-zA-Z][a-zA-Z0-9_]*$/)) {
		errors.push('Action name must be a valid identifier (letters, numbers, underscore)');
	}

	return { isValid: errors.length === 0, errors };
};

// 当AI无法生成有效配置时，提供用户建议
const showConfigurationSuggestions = (actionData: any) => {
	ElMessageBox.alert(
		`AI could not extract valid HTTP configuration from your input. Please try the following suggestions:

• Provide more detailed API description with complete URL and HTTP method
• Use cURL command format, for example: curl -X GET https://api.example.com/users
• Clearly specify the request method (GET, POST, PUT, DELETE, etc.)
• Include complete API endpoint URL (with protocol http:// or https://)
• For POST/PUT requests, specify request body format (JSON, form, etc.)

You can also switch to the "From cURL" tab to directly paste and import cURL commands.`,
		'Unable to Generate HTTP Configuration',
		{
			type: 'warning',
			confirmButtonText: 'Got it',
		}
	);
};

// 当AI完全没有返回配置数据时的建议
const showNoConfigurationDataSuggestions = () => {
	ElMessageBox.alert(
		`AI processing completed but no configuration data was returned. Please check your input and try:

• Ensure input contains API-related information
• Provide specific HTTP request description
• Use standard cURL command format
• Check network connection is working properly
• If the problem persists, try refreshing the page and retry

We recommend using the "From cURL" feature to import existing cURL commands directly.`,
		'No Configuration Data Received',
		{
			type: 'info',
			confirmButtonText: 'Got it',
		}
	);
};

const applyGeneratedConfig = async (httpConfig: any) => {
	// 验证配置有效性
	const validation = validateHttpConfig(httpConfig);
	if (!validation.isValid) {
		ElMessage.error(`Configuration validation failed: ${validation.errors.join(', ')}`);

		// 显示详细错误信息
		ElMessageBox.alert(
			`The generated HTTP configuration has the following issues:\n\n${validation.errors
				.map((error) => `• ${error}`)
				.join('\n')}\n\nPlease check your input and try again.`,
			'Configuration Validation Failed',
			{ type: 'error' }
		);
		return;
	}

	// 构建新的配置对象
	const newConfig = { ...formConfig.value };

	// 应用URL和自动提取查询参数
	if (httpConfig.url && typeof httpConfig.url === 'string' && httpConfig.url.trim() !== '') {
		// 确保URL是有效的
		let validUrl = httpConfig.url.trim();
		// 如果URL不包含协议，尝试添加https://
		if (!validUrl.match(/^https?:\/\//)) {
			if (validUrl.startsWith('//')) {
				validUrl = 'https:' + validUrl;
			} else if (validUrl.startsWith('/')) {
				// 相对路径需要基础域名，但不使用示例域名
				validUrl = 'https://localhost' + validUrl;
			} else if (!validUrl.includes('://')) {
				validUrl = 'https://' + validUrl;
			}
		}

		// 自动从URL中提取查询参数（如果AI没有提供params字段）
		if (!httpConfig.params || Object.keys(httpConfig.params).length === 0) {
			try {
				const urlObj = new URL(validUrl);
				if (urlObj.search) {
					const extractedParams: Record<string, string> = {};
					urlObj.searchParams.forEach((value, key) => {
						extractedParams[key] = value;
					});

					if (Object.keys(extractedParams).length > 0) {
						httpConfig.params = extractedParams;
						// 从URL中移除查询参数
						validUrl = urlObj.origin + urlObj.pathname;
					}
				}
			} catch (err) {
				// Failed to parse URL for param extraction, continue without params
			}
		}

		newConfig.url = validUrl;
	} else {
		// 这种情况不应该发生，因为配置在到达这里之前已经验证过了
		throw new Error('Invalid configuration: URL is required');
	}

	// 应用HTTP方法
	if (httpConfig.method && typeof httpConfig.method === 'string') {
		const validMethod = httpConfig.method.toUpperCase();
		if (['GET', 'POST', 'PUT', 'DELETE', 'PATCH'].includes(validMethod)) {
			newConfig.method = validMethod;
		} else {
			newConfig.method = 'GET';
		}
	}

	// 应用Headers
	if (httpConfig.headers && typeof httpConfig.headers === 'object') {
		const headersList = Object.entries(httpConfig.headers)
			.filter(([key, value]) => key && value) // 过滤空值
			.map(([key, value]) => ({
				key: String(key).trim(),
				value: String(value).trim(),
			}));
		headersList.push({ key: '', value: '' }); // 添加空行
		newConfig.headersList = headersList;
	}

	// 应用Params（查询参数）
	if (httpConfig.params && typeof httpConfig.params === 'object') {
		const paramsList = Object.entries(httpConfig.params)
			.filter(([key, value]) => key) // 过滤空key
			.map(([key, value]) => ({
				key: String(key).trim(),
				value: String(value ?? '').trim(),
			}));
		paramsList.push({ key: '', value: '' }); // 添加空行
		newConfig.paramsList = paramsList;
	}

	// 应用Body Type
	if (httpConfig.bodyType && typeof httpConfig.bodyType === 'string') {
		const validBodyTypes = ['none', 'form-data', 'x-www-form-urlencoded', 'raw'];
		if (validBodyTypes.includes(httpConfig.bodyType)) {
			newConfig.bodyType = httpConfig.bodyType;
		} else {
			newConfig.bodyType = 'none';
		}
	}

	// 应用timeout
	if (httpConfig.timeout && typeof httpConfig.timeout === 'number' && httpConfig.timeout > 0) {
		newConfig.timeout = httpConfig.timeout;
	} else if (httpConfig.timeout !== undefined) {
		newConfig.timeout = 30;
	}

	// 应用followRedirects
	if (
		httpConfig.followRedirects !== undefined &&
		typeof httpConfig.followRedirects === 'boolean'
	) {
		newConfig.followRedirects = httpConfig.followRedirects;
	}

	// 应用Body内容
	if (httpConfig.body && typeof httpConfig.body === 'string') {
		newConfig.body = httpConfig.body;
	}

	// 应用Raw格式
	if (httpConfig.rawFormat && typeof httpConfig.rawFormat === 'string') {
		const validFormats = ['json', 'text', 'xml', 'html', 'javascript'];
		if (validFormats.includes(httpConfig.rawFormat)) {
			newConfig.rawFormat = httpConfig.rawFormat;
		} else {
			newConfig.rawFormat = 'json';
		}
	}

	// 确保基本的列表字段存在
	if (!newConfig.paramsList || newConfig.paramsList.length === 0) {
		newConfig.paramsList = [{ key: '', value: '' }];
	}
	if (!newConfig.formDataList || newConfig.formDataList.length === 0) {
		newConfig.formDataList = [{ key: '', value: '' }];
	}
	if (!newConfig.urlEncodedList || newConfig.urlEncodedList.length === 0) {
		newConfig.urlEncodedList = [{ key: '', value: '' }];
	}

	// 一次性应用所有配置
	formConfig.value = newConfig;

	// 等待DOM更新
	await nextTick();

	// 应用Action名称到表单（通过emit传递给父组件）
	if (httpConfig.actionName && typeof httpConfig.actionName === 'string') {
		emit('update:actionName', httpConfig.actionName);
	}

	// 通知父组件这是AI生成的配置
	emit('ai-config-applied', httpConfig);

	// 显示配置应用成功消息
	ElMessage.success(
		'HTTP configuration applied to form! Please review and create the action manually.'
	);

	// 关闭导入对话框
	importDialogVisible.value = false;

	// 滚动到表单顶部以便用户看到应用的配置
	await nextTick();
	const formElement = document.querySelector('.http-form');
	if (formElement) {
		formElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
	}
};

// Model management
const handleModelChange = (model: AIModelConfig) => {
	currentAIModel.value = model;
	selectedAIModel.value = model.provider.toLowerCase();
	ElMessage.success(`Switched to ${model.provider.toLowerCase()} ${model.modelName}`);
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
		headers['X-App-Code'] = getAppCode();
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
				loadFallbackModels();
			}
		} else {
			loadFallbackModels();
		}
	} catch (error) {
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
// URL Input Container
.url-input-container {
	@apply flex items-center gap-2;
}

.test-button {
	@apply flex-shrink-0;
	min-width: 80px;
}

.response-box {
	background: var(--el-fill-color-lighter);
	min-height: 200px;
	max-height: 500px;
	overflow-y: auto;
}

html.dark .response-box {
	background: var(--el-fill-color-darker);
}

.response-text {
	color: var(--el-text-color-regular);
}

html.dark .response-text {
	color: var(--el-text-color-placeholder);
}

.response-empty {
	@apply w-full py-12 flex items-center justify-center;
	min-height: 200px;
}

.response-empty-content {
	@apply text-center;
}

// Enhanced Params Section Styles (New)
.params-section-enhanced {
	@apply w-full border overflow-hidden h-full flex flex-col mt-2;
	border-color: var(--el-border-color-light);
}

html.dark .params-section-enhanced {
	border-color: var(--el-border-color-dark);
}

// HTTP Request Tabs - 固定高度，避免双重滚动条
.http-request-tabs {
	height: 400px;

	:deep(.el-tabs__header) {
		@apply flex-shrink-0;
		margin-bottom: 0;
	}

	:deep(.el-tabs__content) {
		@apply flex-1 min-h-0 overflow-hidden;
	}

	:deep(.el-tab-pane) {
		@apply h-full overflow-hidden;
	}
}

// Tab 内容主体 - 可滚动
.tab-content-body {
	@apply flex-1 min-h-0 overflow-y-auto;
}

.params-header-enhanced {
	@apply px-4 py-2 border-b grid grid-cols-12 gap-4 items-center flex-shrink-0;
	background: var(--el-fill-color-lighter);
	border-bottom-color: var(--el-border-color-light);

	.param-col-key {
		@apply col-span-4 text-sm font-medium;
		color: var(--el-text-color-regular);
	}

	.param-col-value {
		@apply col-span-7 text-sm font-medium;
		color: var(--el-text-color-regular);
	}

	.param-actions-enhanced {
		@apply col-span-1 text-sm font-medium text-center;
		color: var(--el-text-color-regular);
	}
}

html.dark .params-header-enhanced {
	background: var(--el-fill-color-darker);
	border-bottom-color: var(--el-border-color-dark);

	.param-col-key,
	.param-col-value,
	.param-actions-enhanced {
		color: var(--el-text-color-secondary);
	}
}

.params-body-enhanced {
	@apply p-3 space-y-2 flex-1 min-h-0 overflow-y-auto;
}

.param-row-enhanced {
	@apply grid grid-cols-12 gap-4 items-center;
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

.params-header {
	@apply px-4 py-3 border-b flex items-center gap-4;
	background: var(--el-fill-color-lighter);
	border-bottom-color: var(--el-border-color-light);

	.param-col {
		@apply flex-1 text-sm font-medium;
		color: var(--el-text-color-regular);
	}

	.param-actions {
		@apply w-12 text-sm font-medium;
		color: var(--el-text-color-regular);
	}
}

html.dark .params-header {
	background: var(--el-fill-color-darker);
	border-bottom-color: var(--el-border-color-dark);

	.param-col,
	.param-actions {
		color: var(--el-text-color-secondary);
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
	@apply w-full space-y-4 h-full flex flex-col mt-2;
}

.body-type-group {
	@apply flex flex-wrap gap-4 flex-shrink-0;

	:deep(.el-radio) {
		@apply mr-0;
	}
}

.body-content {
	@apply mt-4 flex-1 min-h-0;
	overflow: hidden; // 让内部元素处理滚动
}

.body-none {
	@apply py-8 text-center h-full flex items-center justify-center;
	overflow-y: auto; // none 类型需要滚动
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
	@apply border rounded-xl p-4;
	border-color: var(--el-border-color-light);
}

html.dark .options-section {
	border-color: var(--el-border-color-dark);
}

.test-section {
	@apply border-t pt-6;
	border-top-color: var(--el-border-color-light);
}

html.dark .test-section {
	border-top-color: var(--el-border-color-dark);
}

.test-result {
	@apply mt-3;

	pre {
		@apply max-h-40 overflow-y-auto;
	}
}

// 导入错误信息样式
.import-error-message {
	@apply mt-4 p-4 rounded-xl border;
	background: var(--el-color-danger-light-9);
	border-color: var(--el-color-danger-light-5);
}

// 信息提示样式
.curl-info-banner {
	@apply p-4 rounded-xl border;
	background: var(--el-color-info-light-9);
	border-color: var(--el-color-info-light-5);
}

.info-icon {
	color: var(--el-color-info);
}

.info-title {
	color: var(--el-text-color-primary);
}

.info-text {
	color: var(--el-text-color-regular);
}

.error-icon {
	color: var(--el-color-danger);
}

.error-title {
	color: var(--el-text-color-primary);
}

.error-text {
	color: var(--el-text-color-regular);
}

// AI Chat 样式
.ai-import-content {
	@apply h-full;
}

.ai-chat-container {
	@apply flex flex-col h-96;
}

.ai-chat-messages {
	max-height: 300px;
	background: var(--el-fill-color-lighter);
	@apply flex-1 overflow-y-auto p-4 space-y-4 rounded-lg mb-4 dark:bg-black-400;
}

.ai-message {
	@apply flex;

	&.user-message {
		@apply justify-end;

		.message-content {
			@apply rounded-lg px-4 py-2 max-w-xs;
			background: var(--el-color-primary);
			color: var(--el-color-white);
		}
	}

	&.assistant-message {
		@apply justify-start;

		.message-content {
			@apply rounded-lg px-4 py-2 max-w-md border;
			background: var(--el-bg-color);
			color: var(--el-text-color-primary);
			border-color: var(--el-border-color-light);
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
	@apply rounded-lg p-3 border;
	background: var(--el-fill-color-lighter);
	border-color: var(--el-border-color-light);
}

html.dark .config-preview {
	background: var(--el-fill-color-darker);
	border-color: var(--el-border-color);
}

.config-title {
	@apply text-sm font-semibold mb-2;
	color: var(--el-text-color-regular);
}

html.dark .config-title {
	color: var(--el-text-color-placeholder);
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
	@apply text-xs font-medium min-w-16;
	color: var(--el-text-color-secondary);
}

.config-value {
	@apply text-xs font-mono px-2 py-1 rounded;
	color: var(--el-text-color-regular);
	background: var(--el-fill-color-light);
	word-break: break-all;
	max-width: 100%;
	overflow-wrap: break-word;
}

html.dark .config-value {
	color: var(--el-text-color-placeholder);
	background: var(--el-fill-color-dark);
}

.config-headers {
	@apply space-y-1;
}

.header-item {
	@apply text-xs font-mono px-2 py-1 rounded;
	color: var(--el-text-color-regular);
	background: var(--el-fill-color-light);
	word-break: break-all;
	max-width: 100%;
	overflow-wrap: break-word;
}

html.dark .header-item {
	color: var(--el-text-color-secondary);
	background: var(--el-fill-color-dark);
}

.apply-config-btn {
	@apply text-xs;
}

.typing-indicator {
	@apply inline-flex items-center space-x-1 mr-2;

	span {
		@apply w-2 h-2 rounded-full animate-pulse;
		background: var(--el-text-color-secondary);

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
	@apply border-t pt-4;
	border-top-color: var(--el-border-color-light);
}

html.dark .ai-input-area {
	border-top-color: var(--el-border-color-dark);
}

.ai-input-with-button {
	@apply flex items-stretch gap-2;
}

.ai-input-container {
	@apply flex-1 relative;
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
	@apply text-xs border-none bg-transparent p-1 shadow-none;
	color: var(--el-text-color-secondary);
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
	@apply text-sm font-normal;
	color: var(--el-text-color-primary);
}

.model-status {
	@apply flex items-center;
}

.status-dot {
	@apply w-2 h-2 rounded-full;
	background: var(--el-color-danger);
}

.status-dot.online {
	background: var(--el-color-success);
}

.status-dot.offline {
	background: var(--el-color-danger);
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
	@apply w-8 h-8 flex items-center justify-center;
	color: var(--el-text-color-secondary);

	&:hover {
		color: var(--el-text-color-regular);
	}
}

.input-right-actions .ai-send-button {
	width: 32px;
	height: 32px;
	min-width: 32px;
	padding: 0;
	border-radius: 50%;
	background: var(--el-color-primary);
	border: none;
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
	transition: all 0.2s ease;
}

.input-right-actions .ai-send-button:hover {
	transform: translateY(-1px);
	background: var(--el-color-primary-light-3);
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.input-right-actions .ai-send-button:disabled {
	background: var(--el-fill-color);
	box-shadow: none;
	transform: none;
}

.input-right-actions .ai-send-button .el-icon {
	font-size: 14px;
	color: var(--el-color-white);
}

.input-right-actions .ai-send-button .send-icon {
	width: 14px;
	height: 14px;
	color: var(--el-color-white);
	transform: rotate(-45deg);
}

.uploaded-file-display {
	@apply mt-2 p-2 rounded-lg;
	background: var(--el-fill-color-light);
}

html.dark .uploaded-file-display {
	background: var(--el-fill-color-dark);
}

.file-info {
	@apply flex items-center gap-2 text-sm;
	color: var(--el-text-color-regular);
}

html.dark .file-info {
	color: var(--el-text-color-secondary);
}

.file-name {
	@apply flex-1 truncate;
}

.url-hint,
.body-none-text {
	color: var(--el-text-color-secondary);
}

.test-result-box {
	background: var(--el-fill-color-lighter);
}

html.dark .test-result-box {
	background: var(--el-fill-color-darker);
}

.test-result-text {
	color: var(--el-text-color-regular);
}

html.dark .test-result-text {
	color: var(--el-text-color-placeholder);
}
</style>
