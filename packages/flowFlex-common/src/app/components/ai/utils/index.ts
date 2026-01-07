/**
 * AI File Analyzer Utilities
 *
 * This module exports utilities for OCR, PDF detection, image processing,
 * and content merging used by the AIFileAnalyzer component.
 */

// OCR utilities
export {
	OCRService,
	getOCRService,
	loadTesseract,
	DEFAULT_OCR_CONFIG,
	SUPPORTED_OCR_LANGUAGES,
	type OCRConfig,
	type OCRResult,
	type OCRBlock,
	type OCRProgressCallback,
	type IOCRService,
	type SupportedOCRLanguage,
} from './ocrUtils';

// PDF detection utilities
export {
	PDFDetector,
	getPDFDetector,
	loadPdfJs,
	isLargePDF,
	DEFAULT_PDF_DETECTOR_CONFIG,
	LARGE_PDF_PAGE_THRESHOLD,
	type PDFDetectionResult,
	type PageExtractionResult,
	type PDFDetectorConfig,
	type IPDFDetector,
} from './pdfDetector';

// Image processing utilities
export {
	ImageProcessor,
	getImageProcessor,
	isValidImageFileType,
	getImageMimeType,
	DEFAULT_IMAGE_OPTIONS,
	SUPPORTED_IMAGE_EXTENSIONS,
	type ImageProcessingOptions,
	type ImageMetadata,
	type IImageProcessor,
	type SupportedImageExtension,
	type RotationAngle,
} from './imageProcessor';

// Content merging utilities
export {
	ContentMerger,
	getContentMerger,
	countWords,
	calculateSimilarity,
	DEFAULT_MERGER_CONFIG,
	type ContentSource,
	type ContentBlock,
	type MergedContent,
	type ContentStatistics,
	type ContentMergerConfig,
	type IContentMerger,
} from './contentMerger';

// Vision API utilities
export {
	VisionApiClient,
	getVisionApiClient,
	isVisionCapableModel,
	DEFAULT_VISION_CONFIG,
	type VisionAnalysisRequest,
	type VisionAnalysisResponse,
	type VisionApiClientConfig,
	type IVisionApiClient,
} from './visionApiClient';
