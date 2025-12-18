/**
 * OCR Utility Module
 *
 * Provides OCR functionality using Tesseract.js for text extraction from images.
 * Implements lazy loading and Web Worker support for non-blocking processing.
 */

// Tesseract.js CDN configuration
const TESSERACT_VERSION = '5.0.5';
const TESSERACT_CDN_BASE = 'https://cdn.jsdelivr.net/npm/tesseract.js@' + TESSERACT_VERSION;
const TESSERACT_CORE_PATH = `${TESSERACT_CDN_BASE}/dist/tesseract.min.js`;
const TESSERACT_WORKER_PATH = `${TESSERACT_CDN_BASE}/dist/worker.min.js`;
const TESSERACT_LANG_PATH = 'https://tessdata.projectnaptha.com/4.0.0';

// OCR configuration interface
export interface OCRConfig {
	/** OCR language code (e.g., 'eng', 'chi_sim', 'chi_tra') */
	language: string;
	/** Custom worker path */
	workerPath?: string;
	/** Custom core path */
	corePath?: string;
	/** Custom language data path */
	langPath?: string;
}

// OCR result interface
export interface OCRResult {
	/** Extracted text */
	text: string;
	/** Recognition confidence (0-100) */
	confidence: number;
	/** Text blocks with positions */
	blocks: OCRBlock[];
}

export interface OCRBlock {
	text: string;
	confidence: number;
	bbox: { x0: number; y0: number; x1: number; y1: number };
}

// Progress callback type
export type OCRProgressCallback = (progress: { status: string; progress: number }) => void;

// Main OCR service interface
export interface IOCRService {
	initialize(config?: OCRConfig): Promise<void>;
	recognizeImage(
		imageData: ImageData | Blob | string,
		onProgress?: OCRProgressCallback
	): Promise<OCRResult>;
	recognizeMultiple(
		images: Array<ImageData | Blob | string>,
		onProgress?: OCRProgressCallback
	): Promise<OCRResult[]>;
	terminate(): Promise<void>;
	isInitialized(): boolean;
	setLanguage(language: string): Promise<void>;
	getLanguage(): string;
}

// Tesseract.js type declarations
declare global {
	interface Window {
		Tesseract: any;
	}
}

// Lazy loading promise
let tesseractLoadingPromise: Promise<any> | null = null;

/**
 * Load script from URL once
 */
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

/**
 * Lazy load Tesseract.js from CDN
 */
export const loadTesseract = async (): Promise<any> => {
	if (window.Tesseract) return window.Tesseract;

	if (!tesseractLoadingPromise) {
		tesseractLoadingPromise = (async () => {
			await loadScriptOnce(TESSERACT_CORE_PATH);
			const Tesseract = window.Tesseract;
			if (!Tesseract) {
				throw new Error('Tesseract.js not available after loading');
			}
			return Tesseract;
		})();
	}

	return tesseractLoadingPromise;
};

/**
 * Default OCR configuration
 */
export const DEFAULT_OCR_CONFIG: OCRConfig = {
	language: 'eng',
	workerPath: TESSERACT_WORKER_PATH,
	langPath: TESSERACT_LANG_PATH,
};

/**
 * OCR Service implementation using Tesseract.js
 */
export class OCRService implements IOCRService {
	private worker: any = null;
	private config: OCRConfig;
	private initialized = false;
	private progressCallback: OCRProgressCallback | null = null;

	constructor(config?: Partial<OCRConfig>) {
		this.config = { ...DEFAULT_OCR_CONFIG, ...config };
	}

	async initialize(config?: OCRConfig): Promise<void> {
		if (config) {
			this.config = { ...this.config, ...config };
		}

		if (this.initialized && this.worker) {
			return;
		}

		const Tesseract = await loadTesseract();

		// Create worker with logger that calls the stored progress callback
		this.worker = await Tesseract.createWorker(this.config.language, 1, {
			workerPath: this.config.workerPath,
			langPath: this.config.langPath,
			logger: (m: any) => {
				// Call progress callback if set
				if (this.progressCallback && m.status && m.progress !== undefined) {
					this.progressCallback({ status: m.status, progress: m.progress });
				}
				console.debug('OCR Progress:', m);
			},
		});

		this.initialized = true;
	}

	async recognizeImage(
		imageData: ImageData | Blob | string,
		onProgress?: OCRProgressCallback
	): Promise<OCRResult> {
		if (!this.initialized || !this.worker) {
			await this.initialize();
		}

		// Store the progress callback to be used by the logger
		this.progressCallback = onProgress || null;

		try {
			// Tesseract.js v5 recognize() doesn't accept logger option
			// The logger is set during worker creation
			const result = await this.worker.recognize(imageData);
			return this.mapTesseractResult(result);
		} finally {
			// Clear the callback after recognition
			this.progressCallback = null;
		}
	}

	async recognizeMultiple(
		images: Array<ImageData | Blob | string>,
		onProgress?: OCRProgressCallback
	): Promise<OCRResult[]> {
		const results: OCRResult[] = [];
		const total = images.length;

		for (let i = 0; i < images.length; i++) {
			const result = await this.recognizeImage(images[i], (progress) => {
				if (onProgress) {
					onProgress({
						status: `Processing image ${i + 1}/${total}: ${progress.status}`,
						progress: (i + progress.progress) / total,
					});
				}
			});
			results.push(result);
		}

		return results;
	}

	async terminate(): Promise<void> {
		if (this.worker) {
			await this.worker.terminate();
			this.worker = null;
			this.initialized = false;
		}
	}

	isInitialized(): boolean {
		return this.initialized;
	}

	async setLanguage(language: string): Promise<void> {
		this.config.language = language;
		if (this.worker) {
			await this.worker.reinitialize(language);
		}
	}

	getLanguage(): string {
		return this.config.language;
	}

	private mapTesseractResult(result: any): OCRResult {
		const data = result.data;
		return {
			text: data.text || '',
			confidence: data.confidence || 0,
			blocks: (data.blocks || []).map((block: any) => ({
				text: block.text || '',
				confidence: block.confidence || 0,
				bbox: block.bbox || { x0: 0, y0: 0, x1: 0, y1: 0 },
			})),
		};
	}
}

// Singleton instance for convenience
let defaultOCRService: OCRService | null = null;

/**
 * Get the default OCR service instance
 */
export const getOCRService = (config?: Partial<OCRConfig>): OCRService => {
	if (!defaultOCRService) {
		defaultOCRService = new OCRService(config);
	}
	return defaultOCRService;
};

/**
 * Supported OCR languages
 */
export const SUPPORTED_OCR_LANGUAGES = [
	{ code: 'eng', name: 'English' },
	{ code: 'chi_sim', name: 'Chinese Simplified' },
	{ code: 'chi_tra', name: 'Chinese Traditional' },
	{ code: 'jpn', name: 'Japanese' },
	{ code: 'kor', name: 'Korean' },
	{ code: 'fra', name: 'French' },
	{ code: 'deu', name: 'German' },
	{ code: 'spa', name: 'Spanish' },
	{ code: 'rus', name: 'Russian' },
	{ code: 'ara', name: 'Arabic' },
] as const;

export type SupportedOCRLanguage = (typeof SUPPORTED_OCR_LANGUAGES)[number]['code'];
