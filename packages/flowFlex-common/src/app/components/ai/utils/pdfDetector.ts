/**
 * PDF Detection Module
 *
 * Provides functionality to detect PDF content type (text-based, image-based, or mixed)
 * and extract page images for OCR processing.
 */

// PDF.js CDN configuration (reuse from AIFileAnalyzer)
const PDF_JS_VERSION = '3.11.174';
const PDF_JS_CDN_URL = `https://cdnjs.cloudflare.com/ajax/libs/pdf.js/${PDF_JS_VERSION}/pdf.min.js`;
const PDF_JS_WORKER_URL = `https://cdnjs.cloudflare.com/ajax/libs/pdf.js/${PDF_JS_VERSION}/pdf.worker.min.js`;

// PDF type detection result interface
export interface PDFDetectionResult {
	/** PDF content type classification */
	type: 'text-based' | 'image-based' | 'mixed';
	/** Total number of pages */
	pageCount: number;
	/** Average characters per page */
	textDensity: number;
	/** Whether PDF contains embedded images */
	hasImages: boolean;
	/** Page numbers that contain images (1-indexed) */
	imagePages: number[];
	/** Page numbers with extractable text (1-indexed) */
	textPages: number[];
	/** Total character count across all pages */
	totalCharacters: number;
}

// Page extraction result
export interface PageExtractionResult {
	pageNum: number;
	image: Blob;
	width: number;
	height: number;
}

// PDF detector configuration
export interface PDFDetectorConfig {
	/** Minimum characters per page to consider as text-based */
	textDensityThreshold: number;
	/** Scale factor for rendering pages to images */
	renderScale: number;
	/** Image format for extracted pages */
	imageFormat: 'jpeg' | 'png';
	/** Image quality (0-1) for JPEG */
	imageQuality: number;
}

// PDF detector interface
export interface IPDFDetector {
	detectPDFType(pdfData: ArrayBuffer): Promise<PDFDetectionResult>;
	extractPageAsImage(pdfData: ArrayBuffer, pageNum: number): Promise<Blob>;
	extractAllPagesAsImages(
		pdfData: ArrayBuffer,
		onProgress?: (current: number, total: number) => void
	): AsyncGenerator<PageExtractionResult>;
	getPageCount(pdfData: ArrayBuffer): Promise<number>;
}

// Default configuration
export const DEFAULT_PDF_DETECTOR_CONFIG: PDFDetectorConfig = {
	textDensityThreshold: 50, // Minimum 50 characters per page to be considered text-based
	renderScale: 2.0, // 2x scale for better OCR quality
	imageFormat: 'jpeg',
	imageQuality: 0.85,
};

// Large PDF warning threshold
export const LARGE_PDF_PAGE_THRESHOLD = 50;

// PDF.js type declarations
declare global {
	interface Window {
		pdfjsLib: any;
	}
}

// Lazy loading promise
let pdfJsLoadingPromise: Promise<any> | null = null;

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
 * Lazy load PDF.js from CDN
 */
export const loadPdfJs = async (): Promise<any> => {
	if (window.pdfjsLib) return window.pdfjsLib;

	if (!pdfJsLoadingPromise) {
		pdfJsLoadingPromise = (async () => {
			await loadScriptOnce(PDF_JS_CDN_URL);
			const pdfjsLib = window.pdfjsLib;
			if (!pdfjsLib) {
				throw new Error('PDF.js not available after loading');
			}
			pdfjsLib.GlobalWorkerOptions.workerSrc = PDF_JS_WORKER_URL;
			return pdfjsLib;
		})();
	}

	return pdfJsLoadingPromise;
};

/**
 * Check if a PDF exceeds the large file threshold
 */
export const isLargePDF = (pageCount: number): boolean => {
	return pageCount > LARGE_PDF_PAGE_THRESHOLD;
};

/**
 * PDF Detector implementation
 */
export class PDFDetector implements IPDFDetector {
	private config: PDFDetectorConfig;

	constructor(config?: Partial<PDFDetectorConfig>) {
		this.config = { ...DEFAULT_PDF_DETECTOR_CONFIG, ...config };
	}

	/**
	 * Detect the type of PDF content
	 */
	async detectPDFType(pdfData: ArrayBuffer): Promise<PDFDetectionResult> {
		const pdfjsLib = await loadPdfJs();
		// Copy the ArrayBuffer to avoid "detached ArrayBuffer" error
		const pdfDataCopy = pdfData.slice(0);
		const pdf = await pdfjsLib.getDocument({ data: pdfDataCopy }).promise;
		const pageCount = pdf.numPages;

		const textPages: number[] = [];
		const imagePages: number[] = [];
		let totalCharacters = 0;

		// Analyze each page
		for (let pageNum = 1; pageNum <= pageCount; pageNum++) {
			const page = await pdf.getPage(pageNum);

			// Extract text content
			const textContent = await page.getTextContent();
			const pageText = textContent.items
				.map((item: any) => item.str)
				.join('')
				.trim();
			const charCount = pageText.length;
			totalCharacters += charCount;

			if (charCount >= this.config.textDensityThreshold) {
				textPages.push(pageNum);
			}

			// Check for images in the page
			const operatorList = await page.getOperatorList();
			const hasImages = this.checkForImages(operatorList);
			if (hasImages) {
				imagePages.push(pageNum);
			}

			// Clean up page resources
			page.cleanup();
		}

		const textDensity = pageCount > 0 ? totalCharacters / pageCount : 0;
		const hasImages = imagePages.length > 0;

		// Determine PDF type
		let type: 'text-based' | 'image-based' | 'mixed';
		if (textPages.length === pageCount && !hasImages) {
			type = 'text-based';
		} else if (textPages.length === 0 && hasImages) {
			type = 'image-based';
		} else {
			type = 'mixed';
		}

		return {
			type,
			pageCount,
			textDensity,
			hasImages,
			imagePages,
			textPages,
			totalCharacters,
		};
	}

	/**
	 * Check if operator list contains image operations
	 */
	private checkForImages(operatorList: any): boolean {
		const OPS = window.pdfjsLib.OPS;
		const imageOps = [
			OPS.paintImageXObject,
			OPS.paintInlineImageXObject,
			OPS.paintImageMaskXObject,
		];

		for (const op of operatorList.fnArray) {
			if (imageOps.includes(op)) {
				return true;
			}
		}
		return false;
	}

	/**
	 * Extract a single page as an image
	 */
	async extractPageAsImage(pdfData: ArrayBuffer, pageNum: number): Promise<Blob> {
		const pdfjsLib = await loadPdfJs();
		// Copy the ArrayBuffer to avoid "detached ArrayBuffer" error
		// PDF.js transfers ownership of the ArrayBuffer, so we need a fresh copy
		const pdfDataCopy = pdfData.slice(0);
		const pdf = await pdfjsLib.getDocument({ data: pdfDataCopy }).promise;

		if (pageNum < 1 || pageNum > pdf.numPages) {
			throw new Error(`Invalid page number: ${pageNum}. PDF has ${pdf.numPages} pages.`);
		}

		const page = await pdf.getPage(pageNum);
		const viewport = page.getViewport({ scale: this.config.renderScale });

		// Create canvas
		const canvas = document.createElement('canvas');
		canvas.width = viewport.width;
		canvas.height = viewport.height;

		const ctx = canvas.getContext('2d');
		if (!ctx) {
			throw new Error('Failed to get canvas context');
		}

		// Render page to canvas
		await page.render({
			canvasContext: ctx,
			viewport: viewport,
		}).promise;

		// Convert to blob
		return new Promise((resolve, reject) => {
			canvas.toBlob(
				(blob) => {
					page.cleanup();
					if (blob) {
						resolve(blob);
					} else {
						reject(new Error('Failed to create image blob'));
					}
				},
				`image/${this.config.imageFormat}`,
				this.config.imageQuality
			);
		});
	}

	/**
	 * Extract all pages as images using async generator for memory efficiency
	 */
	async *extractAllPagesAsImages(
		pdfData: ArrayBuffer,
		onProgress?: (current: number, total: number) => void
	): AsyncGenerator<PageExtractionResult> {
		const pdfjsLib = await loadPdfJs();
		// Copy the ArrayBuffer to avoid "detached ArrayBuffer" error
		const pdfDataCopy = pdfData.slice(0);
		const pdf = await pdfjsLib.getDocument({ data: pdfDataCopy }).promise;
		const pageCount = pdf.numPages;

		for (let pageNum = 1; pageNum <= pageCount; pageNum++) {
			const page = await pdf.getPage(pageNum);
			const viewport = page.getViewport({ scale: this.config.renderScale });

			// Create canvas
			const canvas = document.createElement('canvas');
			canvas.width = viewport.width;
			canvas.height = viewport.height;

			const ctx = canvas.getContext('2d');
			if (!ctx) {
				throw new Error('Failed to get canvas context');
			}

			// Render page to canvas
			await page.render({
				canvasContext: ctx,
				viewport: viewport,
			}).promise;

			// Convert to blob
			const blob = await new Promise<Blob>((resolve, reject) => {
				canvas.toBlob(
					(b) => {
						if (b) {
							resolve(b);
						} else {
							reject(new Error(`Failed to create image blob for page ${pageNum}`));
						}
					},
					`image/${this.config.imageFormat}`,
					this.config.imageQuality
				);
			});

			// Clean up page resources
			page.cleanup();

			// Report progress
			if (onProgress) {
				onProgress(pageNum, pageCount);
			}

			yield {
				pageNum,
				image: blob,
				width: viewport.width,
				height: viewport.height,
			};
		}
	}

	/**
	 * Get the page count of a PDF
	 */
	async getPageCount(pdfData: ArrayBuffer): Promise<number> {
		const pdfjsLib = await loadPdfJs();
		// Copy the ArrayBuffer to avoid "detached ArrayBuffer" error
		const pdfDataCopy = pdfData.slice(0);
		const pdf = await pdfjsLib.getDocument({ data: pdfDataCopy }).promise;
		return pdf.numPages;
	}

	/**
	 * Check if PDF needs OCR processing
	 */
	shouldTriggerOCR(detectionResult: PDFDetectionResult): boolean {
		return detectionResult.type === 'image-based' || detectionResult.type === 'mixed';
	}

	/**
	 * Get pages that need OCR processing
	 */
	getPagesNeedingOCR(detectionResult: PDFDetectionResult): number[] {
		if (detectionResult.type === 'image-based') {
			// All pages need OCR
			return Array.from({ length: detectionResult.pageCount }, (_, i) => i + 1);
		} else if (detectionResult.type === 'mixed') {
			// Only image pages need OCR
			return detectionResult.imagePages.filter(
				(page) => !detectionResult.textPages.includes(page)
			);
		}
		return [];
	}
}

// Singleton instance
let defaultPDFDetector: PDFDetector | null = null;

/**
 * Get the default PDF detector instance
 */
export const getPDFDetector = (config?: Partial<PDFDetectorConfig>): PDFDetector => {
	if (!defaultPDFDetector) {
		defaultPDFDetector = new PDFDetector(config);
	}
	return defaultPDFDetector;
};
