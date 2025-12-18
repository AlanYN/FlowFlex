/**
 * Vision API Client Module
 *
 * Provides integration with Vision-capable AI models for image analysis.
 * Includes fallback logic to OCR when Vision API is unavailable.
 */

import { getDefaultAIModel, type AIModelConfig } from '@/apis/ai/config';
import { streamAIChatMessageNative, type AIChatMessage } from '@/apis/ai/workflow';
import { getOCRService, type OCRResult } from './ocrUtils';
import { getImageProcessor } from './imageProcessor';

// Vision API request interface
export interface VisionAnalysisRequest {
	/** Image blob or base64 string */
	image: Blob | string;
	/** Custom analysis prompt */
	prompt?: string;
	/** Specific model ID to use */
	modelId?: string;
	/** Model provider */
	modelProvider?: string;
	/** Model name */
	modelName?: string;
}

// Vision API response interface
export interface VisionAnalysisResponse {
	/** AI analysis result */
	analysis: string;
	/** Model used for analysis */
	modelUsed: string;
	/** Processing time in milliseconds */
	processingTime: number;
	/** Whether fallback to OCR was used */
	usedFallback: boolean;
}

// Vision API client configuration
export interface VisionApiClientConfig {
	/** Default prompt for image analysis */
	defaultPrompt: string;
	/** Timeout for API calls in milliseconds */
	timeout: number;
	/** Whether to automatically fallback to OCR on failure */
	autoFallback: boolean;
	/** OCR language for fallback */
	fallbackOcrLanguage: string;
}

// Vision API client interface
export interface IVisionApiClient {
	isVisionSupported(modelId?: string): Promise<boolean>;
	analyzeImage(request: VisionAnalysisRequest): Promise<VisionAnalysisResponse>;
	analyzeImages(requests: VisionAnalysisRequest[]): Promise<VisionAnalysisResponse[]>;
}

// Default configuration
export const DEFAULT_VISION_CONFIG: VisionApiClientConfig = {
	defaultPrompt:
		'Please analyze this image and extract all text content. If there are diagrams or charts, describe them in detail.',
	timeout: 60000,
	autoFallback: true,
	fallbackOcrLanguage: 'eng',
};

// Vision-capable model providers and their vision model patterns
const VISION_CAPABLE_PATTERNS: Record<string, RegExp[]> = {
	openai: [/gpt-4o/i, /gpt-4-vision/i, /gpt-4-turbo/i],
	anthropic: [/claude-3/i, /claude-3.5/i],
	google: [/gemini/i],
	azure: [/gpt-4o/i, /gpt-4-vision/i, /gpt-4-turbo/i],
};

/**
 * Check if a model supports vision capabilities
 */
export const isVisionCapableModel = (provider: string, modelName: string): boolean => {
	const patterns = VISION_CAPABLE_PATTERNS[provider.toLowerCase()];
	if (!patterns) return false;
	return patterns.some((pattern) => pattern.test(modelName));
};

/**
 * Vision API Client implementation
 */
export class VisionApiClient implements IVisionApiClient {
	private config: VisionApiClientConfig;
	private currentModel: AIModelConfig | null = null;

	constructor(config?: Partial<VisionApiClientConfig>) {
		this.config = { ...DEFAULT_VISION_CONFIG, ...config };
	}

	/**
	 * Check if the current or specified model supports vision
	 */
	async isVisionSupported(modelId?: string): Promise<boolean> {
		try {
			const model = await this.getModel(modelId);
			if (!model) return false;
			return isVisionCapableModel(model.provider, model.modelName);
		} catch {
			return false;
		}
	}

	/**
	 * Analyze a single image
	 */
	async analyzeImage(request: VisionAnalysisRequest): Promise<VisionAnalysisResponse> {
		const startTime = Date.now();

		try {
			// Check if vision is supported
			const model = await this.getModel(request.modelId);
			if (!model || !isVisionCapableModel(model.provider, model.modelName)) {
				if (this.config.autoFallback) {
					return this.fallbackToOCR(request, startTime);
				}
				throw new Error('Vision API is not available for the current model');
			}

			// Convert image to base64 if needed
			const imageBase64 = await this.getImageBase64(request.image);

			// Build the message with image
			const prompt = request.prompt || this.config.defaultPrompt;
			const messages: AIChatMessage[] = [
				{
					role: 'system',
					content:
						'You are an AI assistant specialized in analyzing images and extracting information from them.',
					timestamp: new Date().toISOString(),
				},
				{
					role: 'user',
					content: [
						{ type: 'text', text: prompt },
						{ type: 'image_url', image_url: { url: imageBase64 } },
					] as any,
					timestamp: new Date().toISOString(),
				},
			];

			// Call the API
			let analysisResult = '';
			await new Promise<void>((resolve, reject) => {
				const timeoutId = setTimeout(() => {
					reject(new Error('Vision API request timed out'));
				}, this.config.timeout);

				streamAIChatMessageNative(
					{
						messages,
						context: 'vision_analysis',
						mode: 'general',
						modelId: model.id.toString(),
						modelProvider: model.provider,
						modelName: model.modelName,
					},
					(chunk: string) => {
						analysisResult += chunk;
					},
					() => {
						clearTimeout(timeoutId);
						resolve();
					},
					(error: any) => {
						clearTimeout(timeoutId);
						reject(error);
					}
				);
			});

			return {
				analysis: analysisResult,
				modelUsed: `${model.provider}/${model.modelName}`,
				processingTime: Date.now() - startTime,
				usedFallback: false,
			};
		} catch (error) {
			console.error('Vision API error:', error);

			// Fallback to OCR if enabled
			if (this.config.autoFallback) {
				return this.fallbackToOCR(request, startTime);
			}

			throw error;
		}
	}

	/**
	 * Analyze multiple images
	 */
	async analyzeImages(requests: VisionAnalysisRequest[]): Promise<VisionAnalysisResponse[]> {
		const results: VisionAnalysisResponse[] = [];

		for (const request of requests) {
			const result = await this.analyzeImage(request);
			results.push(result);
		}

		return results;
	}

	/**
	 * Fallback to OCR when Vision API is unavailable
	 */
	private async fallbackToOCR(
		request: VisionAnalysisRequest,
		startTime: number
	): Promise<VisionAnalysisResponse> {
		console.log('Falling back to OCR for image analysis');

		const ocrService = getOCRService({ language: this.config.fallbackOcrLanguage });
		await ocrService.initialize();

		let imageData: Blob | string;
		if (typeof request.image === 'string') {
			// Convert base64 to blob
			const imageProcessor = getImageProcessor();
			imageData = imageProcessor.base64ToBlob(request.image);
		} else {
			imageData = request.image;
		}

		const ocrResult: OCRResult = await ocrService.recognizeImage(imageData);

		return {
			analysis: ocrResult.text,
			modelUsed: 'Tesseract.js OCR (fallback)',
			processingTime: Date.now() - startTime,
			usedFallback: true,
		};
	}

	/**
	 * Get the AI model configuration
	 */
	private async getModel(modelId?: string): Promise<AIModelConfig | null> {
		if (this.currentModel && !modelId) {
			return this.currentModel;
		}

		try {
			const response = await getDefaultAIModel();
			if (response.success && response.data) {
				this.currentModel = response.data;
				return this.currentModel;
			}
		} catch (error) {
			console.error('Failed to get AI model:', error);
		}

		return null;
	}

	/**
	 * Convert image to base64 string
	 */
	private async getImageBase64(image: Blob | string): Promise<string> {
		if (typeof image === 'string') {
			// Already base64
			if (image.startsWith('data:')) {
				return image;
			}
			return `data:image/jpeg;base64,${image}`;
		}

		// Convert blob to base64
		return new Promise((resolve, reject) => {
			const reader = new FileReader();
			reader.onload = () => {
				const result = reader.result as string;
				resolve(result);
			};
			reader.onerror = () => reject(new Error('Failed to read image as base64'));
			reader.readAsDataURL(image);
		});
	}
}

// Singleton instance
let defaultVisionApiClient: VisionApiClient | null = null;

/**
 * Get the default Vision API client instance
 */
export const getVisionApiClient = (config?: Partial<VisionApiClientConfig>): VisionApiClient => {
	if (!defaultVisionApiClient) {
		defaultVisionApiClient = new VisionApiClient(config);
	}
	return defaultVisionApiClient;
};
