/**
 * Image Processor Module
 *
 * Provides image loading, resizing, and preview functionality
 * for the AIFileAnalyzer component.
 */

// Supported image file extensions
export const SUPPORTED_IMAGE_EXTENSIONS = ['jpg', 'jpeg', 'png', 'gif', 'bmp', 'webp'] as const;
export type SupportedImageExtension = (typeof SUPPORTED_IMAGE_EXTENSIONS)[number];

// Image processing options interface
export interface ImageProcessingOptions {
	/** Maximum width for resizing */
	maxWidth?: number;
	/** Maximum height for resizing */
	maxHeight?: number;
	/** JPEG quality (0-1) */
	quality?: number;
	/** Output format */
	format?: 'jpeg' | 'png';
}

// Rotation angle type (clockwise degrees)
export type RotationAngle = 0 | 90 | 180 | 270;

// Image metadata interface
export interface ImageMetadata {
	width: number;
	height: number;
	aspectRatio: number;
	fileSize: number;
	mimeType: string;
}

// Image processor interface
export interface IImageProcessor {
	loadImage(file: File | Blob): Promise<HTMLImageElement>;
	resizeImage(image: HTMLImageElement, options: ImageProcessingOptions): Promise<Blob>;
	rotateImage(
		image: HTMLImageElement,
		angle: RotationAngle,
		options?: ImageProcessingOptions
	): Promise<Blob>;
	getImageData(image: HTMLImageElement): ImageData;
	getImageMetadata(file: File | Blob): Promise<ImageMetadata>;
	createPreviewUrl(file: File | Blob): string;
	revokePreviewUrl(url: string): void;
}

// Default processing options
export const DEFAULT_IMAGE_OPTIONS: ImageProcessingOptions = {
	maxWidth: 2048,
	maxHeight: 2048,
	quality: 0.85,
	format: 'jpeg',
};

/**
 * Validates if a file extension is a supported image type
 * @param filename - The filename to validate
 * @returns true if the extension is supported, false otherwise
 */
export const isValidImageFileType = (filename: string): boolean => {
	if (!filename || typeof filename !== 'string') {
		return false;
	}
	const extension = filename.toLowerCase().split('.').pop();
	if (!extension) {
		return false;
	}
	return SUPPORTED_IMAGE_EXTENSIONS.includes(extension as SupportedImageExtension);
};

/**
 * Gets the MIME type for a supported image extension
 * @param extension - The file extension
 * @returns The MIME type string
 */
export const getImageMimeType = (extension: string): string => {
	const mimeTypes: Record<string, string> = {
		jpg: 'image/jpeg',
		jpeg: 'image/jpeg',
		png: 'image/png',
		gif: 'image/gif',
		bmp: 'image/bmp',
		webp: 'image/webp',
	};
	return mimeTypes[extension.toLowerCase()] || 'image/jpeg';
};

/**
 * Image Processor implementation
 */
export class ImageProcessor implements IImageProcessor {
	private previewUrls: Set<string> = new Set();

	/**
	 * Load an image from a File or Blob
	 * @param file - The image file to load
	 * @returns Promise resolving to an HTMLImageElement
	 */
	async loadImage(file: File | Blob): Promise<HTMLImageElement> {
		return new Promise((resolve, reject) => {
			const url = URL.createObjectURL(file);
			const img = new Image();

			img.onload = () => {
				URL.revokeObjectURL(url);
				resolve(img);
			};

			img.onerror = () => {
				URL.revokeObjectURL(url);
				reject(new Error('Failed to load image'));
			};

			img.src = url;
		});
	}

	/**
	 * Resize an image according to the specified options
	 * @param image - The image element to resize
	 * @param options - Processing options
	 * @returns Promise resolving to a Blob of the resized image
	 */
	async resizeImage(
		image: HTMLImageElement,
		options: ImageProcessingOptions = {}
	): Promise<Blob> {
		const opts = { ...DEFAULT_IMAGE_OPTIONS, ...options };
		const { maxWidth, maxHeight, quality, format } = opts;

		// Calculate new dimensions maintaining aspect ratio
		let { width, height } = image;

		if (maxWidth && width > maxWidth) {
			height = Math.round((height * maxWidth) / width);
			width = maxWidth;
		}

		if (maxHeight && height > maxHeight) {
			width = Math.round((width * maxHeight) / height);
			height = maxHeight;
		}

		// Create canvas and draw resized image
		const canvas = document.createElement('canvas');
		canvas.width = width;
		canvas.height = height;

		const ctx = canvas.getContext('2d');
		if (!ctx) {
			throw new Error('Failed to get canvas context');
		}

		// Use high-quality image smoothing
		ctx.imageSmoothingEnabled = true;
		ctx.imageSmoothingQuality = 'high';
		ctx.drawImage(image, 0, 0, width, height);

		// Convert to blob
		return new Promise((resolve, reject) => {
			canvas.toBlob(
				(blob) => {
					if (blob) {
						resolve(blob);
					} else {
						reject(new Error('Failed to create image blob'));
					}
				},
				`image/${format}`,
				quality
			);
		});
	}

	/**
	 * Rotate an image by the specified angle (clockwise)
	 * @param image - The image element to rotate
	 * @param angle - Rotation angle in degrees (0, 90, 180, 270)
	 * @param options - Optional processing options for output
	 * @returns Promise resolving to a Blob of the rotated image
	 */
	async rotateImage(
		image: HTMLImageElement,
		angle: RotationAngle,
		options: ImageProcessingOptions = {}
	): Promise<Blob> {
		const opts = { ...DEFAULT_IMAGE_OPTIONS, ...options };
		const { quality, format } = opts;

		// For 90 or 270 degree rotation, swap width and height
		const isVerticalRotation = angle === 90 || angle === 270;
		const canvasWidth = isVerticalRotation ? image.height : image.width;
		const canvasHeight = isVerticalRotation ? image.width : image.height;

		const canvas = document.createElement('canvas');
		canvas.width = canvasWidth;
		canvas.height = canvasHeight;

		const ctx = canvas.getContext('2d');
		if (!ctx) {
			throw new Error('Failed to get canvas context');
		}

		// Use high-quality image smoothing
		ctx.imageSmoothingEnabled = true;
		ctx.imageSmoothingQuality = 'high';

		// Move to center, rotate, then draw
		ctx.translate(canvasWidth / 2, canvasHeight / 2);
		ctx.rotate((angle * Math.PI) / 180);
		ctx.drawImage(image, -image.width / 2, -image.height / 2);

		// Convert to blob
		return new Promise((resolve, reject) => {
			canvas.toBlob(
				(blob) => {
					if (blob) {
						resolve(blob);
					} else {
						reject(new Error('Failed to create rotated image blob'));
					}
				},
				`image/${format}`,
				quality
			);
		});
	}

	/**
	 * Get ImageData from an image element
	 * @param image - The image element
	 * @returns ImageData object
	 */
	getImageData(image: HTMLImageElement): ImageData {
		const canvas = document.createElement('canvas');
		canvas.width = image.width;
		canvas.height = image.height;

		const ctx = canvas.getContext('2d');
		if (!ctx) {
			throw new Error('Failed to get canvas context');
		}

		ctx.drawImage(image, 0, 0);
		return ctx.getImageData(0, 0, canvas.width, canvas.height);
	}

	/**
	 * Get metadata about an image file
	 * @param file - The image file
	 * @returns Promise resolving to ImageMetadata
	 */
	async getImageMetadata(file: File | Blob): Promise<ImageMetadata> {
		const image = await this.loadImage(file);
		return {
			width: image.width,
			height: image.height,
			aspectRatio: image.width / image.height,
			fileSize: file.size,
			mimeType: file.type || 'image/unknown',
		};
	}

	/**
	 * Create a preview URL for an image file
	 * @param file - The image file
	 * @returns Object URL string
	 */
	createPreviewUrl(file: File | Blob): string {
		const url = URL.createObjectURL(file);
		this.previewUrls.add(url);
		return url;
	}

	/**
	 * Revoke a preview URL to free memory
	 * @param url - The URL to revoke
	 */
	revokePreviewUrl(url: string): void {
		if (this.previewUrls.has(url)) {
			URL.revokeObjectURL(url);
			this.previewUrls.delete(url);
		}
	}

	/**
	 * Revoke all preview URLs
	 */
	revokeAllPreviewUrls(): void {
		this.previewUrls.forEach((url) => {
			URL.revokeObjectURL(url);
		});
		this.previewUrls.clear();
	}

	/**
	 * Convert image to base64 string
	 * @param file - The image file
	 * @returns Promise resolving to base64 string
	 */
	async toBase64(file: File | Blob): Promise<string> {
		return new Promise((resolve, reject) => {
			const reader = new FileReader();
			reader.onload = () => {
				const result = reader.result as string;
				resolve(result);
			};
			reader.onerror = () => reject(new Error('Failed to read file as base64'));
			reader.readAsDataURL(file);
		});
	}

	/**
	 * Convert base64 string to Blob
	 * @param base64 - The base64 string (with or without data URL prefix)
	 * @param mimeType - The MIME type of the image
	 * @returns Blob object
	 */
	base64ToBlob(base64: string, mimeType: string = 'image/jpeg'): Blob {
		// Remove data URL prefix if present
		const base64Data = base64.includes(',') ? base64.split(',')[1] : base64;
		const byteCharacters = atob(base64Data);
		const byteNumbers = new Array(byteCharacters.length);

		for (let i = 0; i < byteCharacters.length; i++) {
			byteNumbers[i] = byteCharacters.charCodeAt(i);
		}

		const byteArray = new Uint8Array(byteNumbers);
		return new Blob([byteArray], { type: mimeType });
	}
}

// Singleton instance
let defaultImageProcessor: ImageProcessor | null = null;

/**
 * Get the default image processor instance
 */
export const getImageProcessor = (): ImageProcessor => {
	if (!defaultImageProcessor) {
		defaultImageProcessor = new ImageProcessor();
	}
	return defaultImageProcessor;
};
