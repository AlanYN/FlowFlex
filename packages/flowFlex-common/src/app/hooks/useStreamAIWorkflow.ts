import { ref } from 'vue';
import {
	generateAIWorkflow,
	streamGenerateAIWorkflowNative,
	parseAIRequirements,
} from '@/apis/ai/workflow';
import * as XLSX from 'xlsx-js-style';

export interface StreamMessage {
	id: string;
	type: 'user' | 'ai' | 'system' | 'generation-complete';
	content: string;
	timestamp: Date;
	data?: any;
	streaming?: boolean;
}

export function useStreamAIWorkflow() {
	const messageBuffer = ref('');
	const isStreaming = ref(false);
	const currentController = ref<AbortController | null>(null);

	/**
	 * 模拟流式响应生成器
	 * @param prompt 用户输入的提示
	 */
	async function* streamAIResponse(prompt: string) {
		const streamingMessages = [
			'Analyzing your requirements...',
			'Identifying key workflow stages...',
			'Optimizing stage sequences...',
			'Generating required fields...',
			'Creating checklists and questionnaires...',
			'Finalizing workflow structure...',
		];

		// 模拟流式响应
		for (const message of streamingMessages) {
			yield message;
			// 模拟网络延迟
			await new Promise((resolve) => setTimeout(resolve, 800));
		}
	}

	/**
	 * 开始流式响应
	 * @param prompt 用户输入
	 * @param onChunk 每个chunk的回调
	 * @param onComplete 完成时的回调
	 */
	async function startStreaming(
		prompt: string,
		onChunk: (chunk: string) => void,
		onComplete: (result: any) => void,
		modelConfig?: { id?: string; provider?: string; modelName?: string }
	) {
		if (isStreaming.value) return;

		isStreaming.value = true;
		messageBuffer.value = '';
		currentController.value = new AbortController();

		try {
			// 尝试使用真正的流式API
			let streamSuccess = false;

			try {
				console.log(
					'🚀 Attempting to use native stream API:',
					`/ai/workflows/v1/generate/stream`
				);
				await streamGenerateAIWorkflowNative(
					{
						description: prompt,
						modelId: modelConfig?.id,
						modelProvider: modelConfig?.provider,
						modelName: modelConfig?.modelName,
					},
					(chunk: string) => {
						if (currentController.value?.signal.aborted) {
							return;
						}
						console.log('📡 Stream chunk received:', chunk);
						onChunk(chunk);
					},
					(data: any) => {
						console.log('✅ Stream completed with data:', data);

						// 检查多种可能的数据结构
						const stages =
							data?.Stages ||
							data?.stages ||
							data?.Data?.Stages ||
							data?.data?.stages;
						const success = data?.Success !== false && data?.success !== false;

						if (success && Array.isArray(stages) && stages.length > 0) {
							// 构建标准化的响应格式
							const normalizedData = {
								success: true,
								message:
									data?.Message ||
									data?.message ||
									'Workflow generated successfully',
								generatedWorkflow:
									data?.GeneratedWorkflow ||
									data?.generatedWorkflow ||
									data?.Data?.GeneratedWorkflow ||
									data?.data?.generatedWorkflow,
								stages: stages,
								suggestions:
									data?.Suggestions ||
									data?.suggestions ||
									data?.Data?.Suggestions ||
									data?.data?.suggestions ||
									[],
								confidenceScore:
									data?.ConfidenceScore ||
									data?.confidenceScore ||
									data?.Data?.ConfidenceScore ||
									data?.data?.confidenceScore ||
									0.8,
							};

							onComplete(normalizedData);
							streamSuccess = true;
						} else {
							console.warn('Invalid stream response structure:', data);
							throw new Error(
								data?.Message || data?.message || 'Invalid stream response'
							);
						}
					},
					(error: any) => {
						console.warn('❌ Native stream API failed:', error);
						throw error;
					},
					currentController.value
				);

				if (streamSuccess) {
					return;
				}
			} catch (streamError) {
				console.warn('Stream API failed, falling back to regular API:', streamError);
			}

			// 回退到普通API + 模拟流式显示
			for await (const chunk of streamAIResponse(prompt)) {
				if (currentController.value?.signal.aborted) {
					break;
				}
				onChunk(chunk);
				await new Promise((resolve) => setTimeout(resolve, 100));
			}

			// 调用普通的AI API
			const result = await generateAIWorkflow({ description: prompt });

			if (result && (result.success || result.code === '200')) {
				const data = result.data || result;
				if (
					data?.success !== false &&
					Array.isArray(data?.stages) &&
					data.stages.length > 0
				) {
					onComplete(data);
				} else {
					throw new Error(data?.message || 'AI service unavailable');
				}
			} else {
				throw new Error('AI generation failed');
			}
		} catch (error) {
			console.error('Streaming error:', error);
			throw error;
		} finally {
			isStreaming.value = false;
			currentController.value = null;
		}
	}

	/**
	 * 处理文件上传的流式响应
	 * @param file 上传的文件
	 * @param onChunk 每个chunk的回调
	 * @param onComplete 完成时的回调
	 */
	async function streamFileAnalysis(
		file: File,
		onChunk: (chunk: string) => void,
		onComplete: (result: any) => void,
		modelConfig?: { id?: string; provider?: string; modelName?: string }
	) {
		if (isStreaming.value) return;

		isStreaming.value = true;
		currentController.value = new AbortController();

		try {
			let description = '';

			// 根据文件类型选择不同的处理方式
			if (isImageFile(file)) {
				// 图片文件处理
				const base64Data = await readImageAsBase64(file);

				// 流式显示图片分析过程
				const analysisMessages = [
					'Reading image content...',
					'Analyzing image elements...',
					'Extracting workflow information from image...',
					'Identifying process steps...',
					'Generating workflow structure...',
				];

				for (const message of analysisMessages) {
					if (currentController.value?.signal.aborted) {
						break;
					}
					onChunk(message);
					await new Promise((resolve) => setTimeout(resolve, 600));
				}

				// 对于图片文件，创建一个包含完整base64数据的描述
				description = `Please analyze this uploaded image file "${file.name}" and extract workflow information from it. 

Image content (base64): ${base64Data}

Please identify:
1. Any process steps or workflow stages shown in the image
2. Key stakeholders or roles involved
3. Required inputs, outputs, or deliverables
4. Timeline or sequence information
5. Any business rules or conditions

Based on this analysis, create a structured workflow with appropriate stages, assignments, and requirements.`;

				// 如果有专门的图片分析API，可以在这里调用
				try {
					const parseRes = await parseAIRequirements(description);
					if (parseRes?.data?.success && parseRes?.data?.structuredText) {
						description = parseRes.data.structuredText;
					}
				} catch (parseError) {
					console.warn('Image parsing failed, using direct description:', parseError);
				}
			} else {
				// 文本文件处理
				const fileText = await readFileAsText(file);

				// 流式显示分析过程
				const analysisMessages = [
					'Reading file content...',
					'Parsing document structure...',
					'Extracting workflow requirements...',
					'Analyzing process steps...',
					'Generating workflow structure...',
				];

				for (const message of analysisMessages) {
					if (currentController.value?.signal.aborted) {
						break;
					}
					onChunk(message);
					await new Promise((resolve) => setTimeout(resolve, 600));
				}

				// 对于文本文件，fileText已经包含了处理后的内容
				// 如果是Excel文件，fileText已经是格式化的描述
				// 如果是普通文本文件，fileText是文件的实际内容
				if (file.name.endsWith('.xlsx') || file.name.endsWith('.xls')) {
					// Excel文件已经在readFileAsText中处理为描述
					description = fileText;
				} else {
					// 普通文本文件的处理
					const truncatedText = fileText.slice(0, 5000);
					description = `Please analyze this uploaded file "${file.name}" and create a workflow based on its content.

File content:
${truncatedText}

Please extract workflow information including:
1. Process steps and stages
2. Stakeholders and responsibilities
3. Required inputs and outputs
4. Timeline and dependencies
5. Business rules and conditions

Create a structured workflow with appropriate stages, team assignments, and required fields.`;

					try {
						const parseRes = await parseAIRequirements(fileText);
						if (parseRes?.data?.success && parseRes?.data?.structuredText) {
							description = `Please create a workflow based on this structured analysis:

${parseRes.data.structuredText}

Create a structured workflow with appropriate stages, team assignments, and required fields.`;
						}
					} catch (parseError) {
						console.warn('File parsing failed, using direct content:', parseError);
					}
				}
			}

			// 生成工作流 - 尝试使用真正的流式API
			let streamSuccess = false;

			try {
				await streamGenerateAIWorkflowNative(
					{
						description,
						modelId: modelConfig?.id,
						modelProvider: modelConfig?.provider,
						modelName: modelConfig?.modelName,
					},
					(chunk: string) => {
						if (currentController.value?.signal.aborted) {
							return;
						}
						onChunk(chunk);
					},
					(data: any) => {
						if (
							data?.success !== false &&
							Array.isArray(data?.stages) &&
							data.stages.length > 0
						) {
							onComplete(data);
							streamSuccess = true;
						} else {
							throw new Error(data?.message || 'Invalid stream response');
						}
					},
					(error: any) => {
						console.warn('Native stream API failed for file analysis:', error);
						throw error;
					},
					currentController.value
				);

				if (streamSuccess) {
					return;
				}
			} catch (streamError) {
				console.warn(
					'Stream API failed for file analysis, falling back to regular API:',
					streamError
				);
			}

			// 回退到普通API
			const result = await generateAIWorkflow({ description });

			if (result && (result.success || result.code === '200')) {
				const data = result.data || result;
				if (
					data?.success !== false &&
					Array.isArray(data?.stages) &&
					data.stages.length > 0
				) {
					onComplete(data);
				} else {
					throw new Error(data?.message || 'AI service unavailable');
				}
			} else {
				throw new Error('AI generation failed');
			}
		} catch (error) {
			console.error('File analysis error:', error);
			throw error;
		} finally {
			isStreaming.value = false;
			currentController.value = null;
		}
	}

	/**
	 * 停止流式响应
	 */
	function stopStreaming() {
		if (currentController.value) {
			currentController.value.abort();
		}
		isStreaming.value = false;
	}

	/**
	 * 判断是否为图片文件
	 * @param file 文件对象
	 */
	function isImageFile(file: File): boolean {
		const imageTypes = [
			'image/jpeg',
			'image/jpg',
			'image/png',
			'image/gif',
			'image/bmp',
			'image/webp',
			'image/svg+xml',
		];
		return imageTypes.includes(file.type);
	}

	/**
	 * 读取图片文件为base64
	 * @param file 图片文件对象
	 */
	function readImageAsBase64(file: File): Promise<string> {
		return new Promise((resolve, reject) => {
			const reader = new FileReader();
			reader.onload = () => {
				const result = reader.result as string;
				// 移除data:image/...;base64,前缀，只保留base64数据
				const base64Data = result.split(',')[1] || result;
				resolve(base64Data);
			};
			reader.onerror = (err) => reject(err);
			reader.readAsDataURL(file);
		});
	}

	/**
	 * 读取文件为文本
	 * @param file 文件对象
	 */
	function readFileAsText(file: File): Promise<string> {
		return new Promise((resolve, reject) => {
			const reader = new FileReader();
			reader.onload = () => {
				const result = reader.result as string;
				resolve(result);
			};
			reader.onerror = (err) => reject(err);

			// 对于Excel文件，我们需要特殊处理
			if (file.name.endsWith('.xlsx') || file.name.endsWith('.xls')) {
				// 对于Excel文件，使用XLSX库解析
				reader.onload = () => {
					try {
						const arrayBuffer = reader.result as ArrayBuffer;
						const workbook = XLSX.read(arrayBuffer, { type: 'array' });

						// 获取第一个工作表
						const firstSheetName = workbook.SheetNames[0];
						const worksheet = workbook.Sheets[firstSheetName];

						// 将工作表转换为JSON格式
						const jsonData = XLSX.utils.sheet_to_json(worksheet, {
							header: 1,
							raw: false,
						});

						// 构建描述
						let description = `I have analyzed the Excel file "${file.name}" and extracted the following content:\n\n`;

						// 添加工作表信息
						description += `Worksheet: ${firstSheetName}\n`;
						description += `Total rows: ${jsonData.length}\n\n`;

						// 添加数据内容（前20行）
						description += `Data content:\n`;
						jsonData.slice(0, 20).forEach((row: any, index: number) => {
							if (Array.isArray(row) && row.length > 0) {
								description += `Row ${index + 1}: ${row.join(' | ')}\n`;
							}
						});

						if (jsonData.length > 20) {
							description += `... and ${jsonData.length - 20} more rows\n`;
						}

						description += `\nBased on this Excel file content about "${file.name.replace(
							/\.(xlsx|xls)$/,
							''
						)}", please create a comprehensive workflow that includes:
1. Data collection and validation stages
2. Environment setup and configuration  
3. Data transformation and preparation steps
4. Quality assurance and testing phases
5. Deployment and verification processes
6. Stakeholder roles and responsibilities
7. Required approvals and checkpoints

Please design a structured workflow with appropriate stages, team assignments, estimated durations, and required fields for each stage.`;

						resolve(description);
					} catch (error) {
						console.error('Excel parsing error:', error);
						// 如果解析失败，使用备用描述
						const fallbackDescription = `I have uploaded an Excel file named "${file.name}" for workflow analysis. Based on the filename, this appears to be related to staging environment data preparation.

Please create a comprehensive workflow that includes:
1. Data collection and validation stages
2. Environment setup and configuration
3. Data transformation and preparation steps
4. Quality assurance and testing phases
5. Deployment and verification processes
6. Stakeholder roles and responsibilities
7. Required approvals and checkpoints

Please design a structured workflow with appropriate stages, team assignments, estimated durations, and required fields for each stage.`;
						resolve(fallbackDescription);
					}
				};
				reader.readAsArrayBuffer(file);
			} else {
				// 对于其他文本文件，直接读取为文本
				reader.readAsText(file, 'utf-8');
			}
		});
	}

	return {
		messageBuffer,
		isStreaming,
		startStreaming,
		streamFileAnalysis,
		stopStreaming,
	};
}
