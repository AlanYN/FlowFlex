/**
 * Content Merger Module
 *
 * Provides functionality to merge content from different sources (text layer, OCR, Vision API),
 * deduplicate overlapping content, and maintain logical reading order.
 */

// Content source types
export type ContentSource = 'text-layer' | 'ocr' | 'vision-api';

// Content block with source tracking
export interface ContentBlock {
	/** The text content */
	content: string;
	/** Source of the content */
	source: ContentSource;
	/** Page number (1-indexed, optional) */
	pageNumber?: number;
	/** Recognition confidence (0-100, optional) */
	confidence?: number;
}

// Merged content result
export interface MergedContent {
	/** Combined full text */
	fullText: string;
	/** Individual content blocks with metadata */
	blocks: ContentBlock[];
	/** Content statistics */
	statistics: ContentStatistics;
}

// Content statistics
export interface ContentStatistics {
	/** Total character count */
	totalCharacters: number;
	/** Total word count */
	totalWords: number;
	/** Characters from OCR */
	ocrCharacters: number;
	/** Characters from text layer */
	textLayerCharacters: number;
	/** Characters from Vision API */
	visionApiCharacters: number;
	/** Average confidence across all blocks with confidence */
	averageConfidence: number;
	/** Number of blocks */
	blockCount: number;
}

// Content merger configuration
export interface ContentMergerConfig {
	/** Similarity threshold for deduplication (0-1) */
	deduplicationThreshold: number;
	/** Whether to preserve block boundaries in full text */
	preserveBlockBoundaries: boolean;
	/** Separator between blocks */
	blockSeparator: string;
}

// Content merger interface
export interface IContentMerger {
	merge(blocks: ContentBlock[]): MergedContent;
	deduplicate(text1: string, text2: string, threshold?: number): string;
	orderByPage(blocks: ContentBlock[]): ContentBlock[];
	calculateStatistics(blocks: ContentBlock[]): ContentStatistics;
}

// Default configuration
export const DEFAULT_MERGER_CONFIG: ContentMergerConfig = {
	deduplicationThreshold: 0.8,
	preserveBlockBoundaries: true,
	blockSeparator: '\n\n',
};

/**
 * Calculate word count from text
 */
export const countWords = (text: string): number => {
	if (!text || !text.trim()) return 0;
	// Split by whitespace and filter empty strings
	return text.trim().split(/\s+/).filter(Boolean).length;
};

/**
 * Calculate similarity between two strings using Jaccard similarity
 * @param str1 First string
 * @param str2 Second string
 * @returns Similarity score between 0 and 1
 */
export const calculateSimilarity = (str1: string, str2: string): number => {
	if (!str1 && !str2) return 1;
	if (!str1 || !str2) return 0;

	const words1 = new Set(str1.toLowerCase().split(/\s+/).filter(Boolean));
	const words2 = new Set(str2.toLowerCase().split(/\s+/).filter(Boolean));

	if (words1.size === 0 && words2.size === 0) return 1;
	if (words1.size === 0 || words2.size === 0) return 0;

	const intersection = new Set([...words1].filter((x) => words2.has(x)));
	const union = new Set([...words1, ...words2]);

	return intersection.size / union.size;
};

/**
 * Find longest common substring between two strings
 */
const findLongestCommonSubstring = (str1: string, str2: string): string => {
	if (!str1 || !str2) return '';

	const m = str1.length;
	const n = str2.length;
	let maxLength = 0;
	let endIndex = 0;

	// Create a 2D array to store lengths of longest common suffixes
	const dp: number[][] = Array(m + 1)
		.fill(null)
		.map(() => Array(n + 1).fill(0));

	for (let i = 1; i <= m; i++) {
		for (let j = 1; j <= n; j++) {
			if (str1[i - 1] === str2[j - 1]) {
				dp[i][j] = dp[i - 1][j - 1] + 1;
				if (dp[i][j] > maxLength) {
					maxLength = dp[i][j];
					endIndex = i;
				}
			}
		}
	}

	return str1.substring(endIndex - maxLength, endIndex);
};

/**
 * Content Merger implementation
 */
export class ContentMerger implements IContentMerger {
	private config: ContentMergerConfig;

	constructor(config?: Partial<ContentMergerConfig>) {
		this.config = { ...DEFAULT_MERGER_CONFIG, ...config };
	}

	/**
	 * Merge content blocks into a single result
	 */
	merge(blocks: ContentBlock[]): MergedContent {
		if (!blocks || blocks.length === 0) {
			return {
				fullText: '',
				blocks: [],
				statistics: this.calculateStatistics([]),
			};
		}

		// Order blocks by page number
		const orderedBlocks = this.orderByPage(blocks);

		// Deduplicate overlapping content between adjacent blocks
		const deduplicatedBlocks = this.deduplicateBlocks(orderedBlocks);

		// Build full text
		const fullText = deduplicatedBlocks
			.map((block) => block.content.trim())
			.filter(Boolean)
			.join(this.config.blockSeparator);

		// Calculate statistics
		const statistics = this.calculateStatistics(deduplicatedBlocks);

		return {
			fullText,
			blocks: deduplicatedBlocks,
			statistics,
		};
	}

	/**
	 * Deduplicate overlapping content between two strings
	 */
	deduplicate(text1: string, text2: string, threshold?: number): string {
		const similarityThreshold = threshold ?? this.config.deduplicationThreshold;

		if (!text1) return text2 || '';
		if (!text2) return text1;

		const similarity = calculateSimilarity(text1, text2);

		// If texts are very similar, return the longer one
		if (similarity >= similarityThreshold) {
			return text1.length >= text2.length ? text1 : text2;
		}

		// Find and remove common substring if it's significant
		const commonSubstring = findLongestCommonSubstring(text1, text2);
		const minLength = Math.min(text1.length, text2.length);

		// If common substring is more than 50% of the shorter text, remove it from one
		if (commonSubstring.length > minLength * 0.5) {
			const text2WithoutCommon = text2.replace(commonSubstring, '').trim();
			return `${text1}\n${text2WithoutCommon}`.trim();
		}

		// Otherwise, concatenate both
		return `${text1}\n${text2}`.trim();
	}

	/**
	 * Order content blocks by page number
	 */
	orderByPage(blocks: ContentBlock[]): ContentBlock[] {
		return [...blocks].sort((a, b) => {
			const pageA = a.pageNumber ?? 0;
			const pageB = b.pageNumber ?? 0;
			return pageA - pageB;
		});
	}

	/**
	 * Calculate statistics for content blocks
	 */
	calculateStatistics(blocks: ContentBlock[]): ContentStatistics {
		let totalCharacters = 0;
		let ocrCharacters = 0;
		let textLayerCharacters = 0;
		let visionApiCharacters = 0;
		let totalConfidence = 0;
		let confidenceCount = 0;

		for (const block of blocks) {
			const charCount = block.content.length;
			totalCharacters += charCount;

			switch (block.source) {
				case 'ocr':
					ocrCharacters += charCount;
					break;
				case 'text-layer':
					textLayerCharacters += charCount;
					break;
				case 'vision-api':
					visionApiCharacters += charCount;
					break;
			}

			if (block.confidence !== undefined) {
				totalConfidence += block.confidence;
				confidenceCount++;
			}
		}

		const fullText = blocks.map((b) => b.content).join(' ');
		const totalWords = countWords(fullText);
		const averageConfidence = confidenceCount > 0 ? totalConfidence / confidenceCount : 0;

		return {
			totalCharacters,
			totalWords,
			ocrCharacters,
			textLayerCharacters,
			visionApiCharacters,
			averageConfidence,
			blockCount: blocks.length,
		};
	}

	/**
	 * Deduplicate content between adjacent blocks
	 */
	private deduplicateBlocks(blocks: ContentBlock[]): ContentBlock[] {
		if (blocks.length <= 1) return blocks;

		const result: ContentBlock[] = [blocks[0]];

		for (let i = 1; i < blocks.length; i++) {
			const prevBlock = result[result.length - 1];
			const currentBlock = blocks[i];

			// Check if blocks are from the same page
			if (prevBlock.pageNumber === currentBlock.pageNumber) {
				// Deduplicate content from same page
				const deduplicatedContent = this.deduplicate(
					prevBlock.content,
					currentBlock.content
				);

				// Update previous block with deduplicated content
				result[result.length - 1] = {
					...prevBlock,
					content: deduplicatedContent,
					// Keep higher confidence if available
					confidence:
						prevBlock.confidence !== undefined && currentBlock.confidence !== undefined
							? Math.max(prevBlock.confidence, currentBlock.confidence)
							: prevBlock.confidence ?? currentBlock.confidence,
				};
			} else {
				result.push(currentBlock);
			}
		}

		return result;
	}

	/**
	 * Create a content block from text
	 */
	static createBlock(
		content: string,
		source: ContentSource,
		pageNumber?: number,
		confidence?: number
	): ContentBlock {
		return {
			content,
			source,
			pageNumber,
			confidence,
		};
	}
}

// Singleton instance
let defaultContentMerger: ContentMerger | null = null;

/**
 * Get the default content merger instance
 */
export const getContentMerger = (config?: Partial<ContentMergerConfig>): ContentMerger => {
	if (!defaultContentMerger) {
		defaultContentMerger = new ContentMerger(config);
	}
	return defaultContentMerger;
};
