/**
 * Property-based tests for Image Processor Module
 *
 * Uses fast-check for property-based testing to verify image file type validation
 * and other image processing properties.
 */

import fc from 'fast-check';
import { isValidImageFileType, SUPPORTED_IMAGE_EXTENSIONS } from '../utils/imageProcessor';

describe('Image Processor - Property Tests', () => {
	/**
	 * **Feature: pdf-image-ocr, Property 1: Image file type validation**
	 * **Validates: Requirements 1.1**
	 *
	 * For any file with an extension, the file type validator SHALL return true
	 * if and only if the extension is one of: jpg, jpeg, png, gif, bmp, webp (case-insensitive)
	 */
	describe('Property 1: Image file type validation', () => {
		it('should accept all supported image extensions (case-insensitive)', () => {
			// Generate arbitrary filenames with supported extensions
			const supportedExtensionArb = fc.constantFrom(...SUPPORTED_IMAGE_EXTENSIONS);
			const filenameArb = fc
				.tuple(
					fc.string({ minLength: 1, maxLength: 50 }).filter((s) => !s.includes('.')),
					supportedExtensionArb
				)
				.map(([name, ext]) => `${name}.${ext}`);

			fc.assert(
				fc.property(filenameArb, (filename) => {
					return isValidImageFileType(filename) === true;
				}),
				{ numRuns: 100 }
			);
		});

		it('should accept uppercase extensions', () => {
			const supportedExtensionArb = fc.constantFrom(
				...SUPPORTED_IMAGE_EXTENSIONS.map((e) => e.toUpperCase())
			);
			const filenameArb = fc
				.tuple(
					fc.string({ minLength: 1, maxLength: 50 }).filter((s) => !s.includes('.')),
					supportedExtensionArb
				)
				.map(([name, ext]) => `${name}.${ext}`);

			fc.assert(
				fc.property(filenameArb, (filename) => {
					return isValidImageFileType(filename) === true;
				}),
				{ numRuns: 100 }
			);
		});

		it('should accept mixed case extensions', () => {
			const mixedCaseExtensions = ['JpG', 'JpEg', 'PnG', 'GiF', 'BmP', 'WeBp'];
			const supportedExtensionArb = fc.constantFrom(...mixedCaseExtensions);
			const filenameArb = fc
				.tuple(
					fc.string({ minLength: 1, maxLength: 50 }).filter((s) => !s.includes('.')),
					supportedExtensionArb
				)
				.map(([name, ext]) => `${name}.${ext}`);

			fc.assert(
				fc.property(filenameArb, (filename) => {
					return isValidImageFileType(filename) === true;
				}),
				{ numRuns: 100 }
			);
		});

		it('should reject unsupported extensions', () => {
			const unsupportedExtensions = [
				'txt',
				'pdf',
				'doc',
				'docx',
				'xls',
				'xlsx',
				'csv',
				'md',
				'json',
				'xml',
				'html',
				'css',
				'js',
				'ts',
				'exe',
				'zip',
				'rar',
				'mp3',
				'mp4',
				'avi',
				'mov',
			];
			const unsupportedExtensionArb = fc.constantFrom(...unsupportedExtensions);
			const filenameArb = fc
				.tuple(
					fc.string({ minLength: 1, maxLength: 50 }).filter((s) => !s.includes('.')),
					unsupportedExtensionArb
				)
				.map(([name, ext]) => `${name}.${ext}`);

			fc.assert(
				fc.property(filenameArb, (filename) => {
					return isValidImageFileType(filename) === false;
				}),
				{ numRuns: 100 }
			);
		});

		it('should reject files without extensions', () => {
			const filenameWithoutExtArb = fc
				.string({ minLength: 1, maxLength: 50 })
				.filter((s) => !s.includes('.'));

			fc.assert(
				fc.property(filenameWithoutExtArb, (filename) => {
					return isValidImageFileType(filename) === false;
				}),
				{ numRuns: 100 }
			);
		});

		it('should handle empty and invalid inputs', () => {
			expect(isValidImageFileType('')).toBe(false);
			expect(isValidImageFileType('.')).toBe(false);
			expect(isValidImageFileType('.jpg')).toBe(true); // Hidden file with extension
		});
	});
});
