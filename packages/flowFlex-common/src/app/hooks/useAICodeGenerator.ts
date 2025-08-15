import { ref } from 'vue';
import { ElMessage } from 'element-plus';
import { getUserAIModels, getDefaultAIModel } from '@/apis/ai/config';
import { sendAIChatMessage } from '@/apis/ai/workflow';

export function useAICodeGenerator() {
	// AI Code Generator state
	const showAICodeGeneratorDialog = ref(false);
	const aiInstructions = ref('');
	const generatedCode = ref('');
	const generating = ref(false);

	// AI Model state
	const selectedModelId = ref('');
	const modelOptions = ref<any[]>([]);
	const loadingModels = ref(false);

	// Show AI Code Generator dialog
	const showAICodeGenerator = () => {
		showAICodeGeneratorDialog.value = true;
		aiInstructions.value = '';
		generatedCode.value = '';
	};

	// Generate code using AI
	const generateCode = async () => {
		if (!aiInstructions.value.trim()) {
			ElMessage.warning('Please enter code generation instructions');
			return;
		}

		if (!selectedModelId.value) {
			ElMessage.warning('Please select an AI model to generate code.');
			return;
		}

		generating.value = true;
		try {
			// Get selected model info
			const selectedModel = modelOptions.value.find(
				(model) => String(model.id) === selectedModelId.value
			);
			if (!selectedModel) {
				throw new Error('Selected model not found');
			}

			// Prepare request payload using existing API format
			const requestPayload = {
				messages: [
					{
						role: 'user' as const,
						content: aiInstructions.value,
						timestamp: new Date().toISOString(),
					},
				],
				mode: 'generate_code' as const,
				modelId: selectedModelId.value,
				modelProvider: selectedModel.provider,
				modelName: selectedModel.name,
			};

			console.log('Sending AI code generation request:', requestPayload);

			// Use existing API function
			const response = await sendAIChatMessage(requestPayload);
			console.log('AI code generation response:', response);

			// Handle response - use type assertion since actual response might differ from type definition
			const responseAny = response as any;
			let content = '';

			if (responseAny.data?.success && responseAny.data?.response?.content) {
				// If response has nested data structure
				content = responseAny.data.response.content;
			} else if (responseAny.success && responseAny.response?.content) {
				// If response is already unwrapped
				content = responseAny.response.content;
			} else {
				throw new Error(
					responseAny.data?.message || responseAny.message || 'Code generation failed'
				);
			}

			if (content) {
				generatedCode.value = content;
				ElMessage.success('Code generated successfully!');
			} else {
				throw new Error('No content received from AI');
			}
		} catch (error) {
			console.error('AI code generation error:', error);
			ElMessage.error(`Code generation failed: ${error.message}`);
		} finally {
			generating.value = false;
		}
	};

	// Apply generated code to editor (需要外部传入回调函数)
	const createApplyGeneratedCode = (onApply: (code: string) => void) => {
		return () => {
			if (generatedCode.value) {
				onApply(generatedCode.value);
				showAICodeGeneratorDialog.value = false;
				ElMessage.success('Code has been applied to the editor');
			}
		};
	};

	// Clear generated code
	const clearGeneratedCode = () => {
		generatedCode.value = '';
		aiInstructions.value = '';
	};

	// Model selection change handler
	const onModelChange = (value: string) => {
		selectedModelId.value = value;
	};

	// Get model display name
	const getModelDisplayName = (model: any) => {
		return `${model.name} (${model.provider})`;
	};

	// Refresh models
	const refreshModels = async () => {
		loadingModels.value = true;
		try {
			// Get all available models
			const modelsResponse = await getUserAIModels();

			if (modelsResponse.success && modelsResponse.data) {
				modelOptions.value = modelsResponse.data.map((model: any) => ({
					id: model.id,
					name: model.modelName,
					provider: model.provider,
					isDefault: model.isDefault,
					isAvailable: model.isAvailable,
				}));
			} else {
				modelOptions.value = [];
			}

			// Get default model configuration
			try {
				const defaultResponse = await getDefaultAIModel();
				if (defaultResponse.success && defaultResponse.data) {
					// Auto-select default model if available
					const defaultModel = modelOptions.value.find(
						(model: any) => model.id === defaultResponse.data.id
					);
					if (defaultModel) {
						selectedModelId.value = String(defaultModel.id);
					} else if (modelOptions.value.length > 0) {
						selectedModelId.value = String(modelOptions.value[0].id);
					}
				} else if (modelOptions.value.length > 0) {
					// Fallback to first available model if no default
					selectedModelId.value = String(modelOptions.value[0].id);
				}
			} catch (defaultError) {
				console.warn('Failed to get default model, using first available:', defaultError);
				if (modelOptions.value.length > 0) {
					selectedModelId.value = String(modelOptions.value[0].id);
				}
			}
		} catch (error) {
			ElMessage.error('Failed to refresh models');
			console.error('Refresh models error:', error);
			modelOptions.value = [];
			selectedModelId.value = '';
		} finally {
			loadingModels.value = false;
		}
	};

	return {
		// State
		showAICodeGeneratorDialog,
		aiInstructions,
		generatedCode,
		generating,
		selectedModelId,
		modelOptions,
		loadingModels,

		// Methods
		showAICodeGenerator,
		generateCode,
		createApplyGeneratedCode,
		clearGeneratedCode,
		onModelChange,
		getModelDisplayName,
		refreshModels,
	};
}
