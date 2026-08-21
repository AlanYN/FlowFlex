import { PDFDocument, StandardFonts, rgb } from 'pdf-lib';
import type { PlacedElement } from '@/views/profile/types';
import { toPdfLibY } from './coordinateUtils';

/**
 * Convert a base64 data URI or raw base64 string to an ArrayBuffer.
 */
function base64ToArrayBuffer(base64: string): ArrayBuffer {
	// Strip optional data URI prefix (e.g. "data:image/png;base64,")
	const raw = base64.includes(',') ? base64.split(',')[1] : base64;
	const binary = atob(raw);
	const buffer = new ArrayBuffer(binary.length);
	const view = new Uint8Array(buffer);
	for (let i = 0; i < binary.length; i++) {
		view[i] = binary.charCodeAt(i);
	}
	return buffer;
}

/**
 * Synthesizes a signed PDF by embedding all placed signature and date elements
 * into their corresponding pages.
 *
 * Coordinate note: PlacedElement coordinates are stored in PDF.js pt
 * (viewport scale already cancelled out). X maps directly to pdf-lib X.
 * Y must be flipped using: pdfLibY = pageHeight - pdfJsY - elementHeight
 *
 * Validates: Requirements 14.2, 14.3, 18.2
 *
 * @param pdfUrl - URL of the original PDF to sign
 * @param elements - Map of pageIndex (0-based) → array of placed elements
 * @returns ArrayBuffer of the synthesized PDF
 */
export async function synthesizePdf(
	pdfUrl: string,
	elements: Map<number, PlacedElement[]>
): Promise<ArrayBuffer> {
	// Step 1: Fetch the original PDF and load it with pdf-lib
	console.log('[Synthesis] pdfUrl received:', pdfUrl);
	console.log(
		'[Synthesis] pdfUrl type:',
		pdfUrl.startsWith('blob:')
			? 'blob'
			: pdfUrl.startsWith('http')
			? 'http/https'
			: pdfUrl === ''
			? 'EMPTY STRING'
			: 'other'
	);
	const response = await fetch(pdfUrl);
	if (!response.ok) {
		throw new Error(`Failed to fetch PDF: ${response.status} ${response.statusText}`);
	}
	const arrayBuffer = await response.arrayBuffer();
	console.log('[Synthesis] fetched PDF bytes:', arrayBuffer.byteLength);
	const pdfDoc = await PDFDocument.load(arrayBuffer);

	// Embed Helvetica font once for all date elements
	const helveticaFont = await pdfDoc.embedFont(StandardFonts.Helvetica);

	// Step 2: Iterate over all pages that have placed elements
	for (const [pageIndex, pageElements] of elements) {
		if (!pageElements || pageElements.length === 0) continue;

		const page = pdfDoc.getPage(pageIndex);
		const { height: pageHeight } = page.getSize();

		for (const element of pageElements) {
			// Convert from PDF.js pt (top-left origin) to pdf-lib (bottom-left origin).
			// X is the same; Y needs the origin flip.
			const pdfLibX = element.x;
			const pdfLibY = toPdfLibY(element.y, pageHeight, element.height);

			if (element.type === 'signature') {
				if (!element.imageBase64) continue;

				const imgBuffer = base64ToArrayBuffer(element.imageBase64);
				const embeddedImage = await pdfDoc.embedPng(imgBuffer);

				page.drawImage(embeddedImage, {
					x: pdfLibX,
					y: pdfLibY,
					width: element.width,
					height: element.height,
				});
			} else if (element.type === 'date') {
				const dateText = element.dateText ?? '';
				if (!dateText) continue;

				// Font size: 60% of element height, clamped to [8, 36] pt.
				// The 0.6 ratio matches the SigningOverlay inline style formula so the PDF
				// result visually matches what the user saw while editing.
				const fontSize = Math.max(8, Math.min(element.height * 0.6, 36));

				// drawText Y anchor is the text baseline (bottom of cap-height).
				// Correct formula: pageHeight - element.y - fontSize
				// (NOT element.height, which is the image-box formula used by drawImage)
				const textBaselineY = pageHeight - element.y - fontSize;

				page.drawText(dateText, {
					x: pdfLibX,
					y: textBaselineY,
					size: fontSize,
					font: helveticaFont,
					color: rgb(0, 0, 0),
				});
			}
		}
	}

	// Step 3: Serialize the modified PDF and return as ArrayBuffer
	// 统计所有 elements
	let totalElements = 0;
	for (const [, els] of elements) totalElements += els.length;
	console.log('[Synthesis] total elements to embed:', totalElements);
	const savedBytes = await pdfDoc.save();
	return savedBytes.buffer as ArrayBuffer;
}
