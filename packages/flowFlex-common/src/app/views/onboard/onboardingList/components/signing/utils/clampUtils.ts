/**
 * Boundary clamping utilities for PDF signing element placement.
 *
 * All values are in PDF.js points (pdfJsPt), top-left origin.
 */

/** Minimum dimensions for signature elements (pt) */
const SIGNATURE_MIN_WIDTH = 50;
const SIGNATURE_MIN_HEIGHT = 20;

/** Minimum dimensions for date elements (pt) */
const DATE_MIN_WIDTH = 60;
const DATE_MIN_HEIGHT = 16;

/**
 * Clamps an element's position so it stays fully within the page boundaries.
 *
 * Ensures:
 * - 0 ≤ x ≤ pageW - w
 * - 0 ≤ y ≤ pageH - h
 *
 * Validates: Requirements 13.2, 13.5
 *
 * @param x - Desired X position in PDF.js points
 * @param y - Desired Y position in PDF.js points
 * @param w - Element width in PDF.js points
 * @param h - Element height in PDF.js points
 * @param pageW - Page width in PDF.js points
 * @param pageH - Page height in PDF.js points
 * @returns Clamped { x, y } position ensuring the element remains within the page
 */
export function clampPosition(
    x: number,
    y: number,
    w: number,
    h: number,
    pageW: number,
    pageH: number,
): { x: number; y: number } {
    const clampedX = Math.min(Math.max(x, 0), pageW - w);
    const clampedY = Math.min(Math.max(y, 0), pageH - h);
    return { x: clampedX, y: clampedY };
}

/**
 * Clamps element dimensions to their type-specific minimum sizes.
 *
 * Minimum sizes:
 * - signature: width ≥ 50pt, height ≥ 20pt
 * - date:      width ≥ 60pt, height ≥ 16pt
 *
 * Validates: Requirements 13.3, 13.6
 *
 * @param w - Desired width in PDF.js points
 * @param h - Desired height in PDF.js points
 * @param type - Element type: 'signature' or 'date'
 * @returns Clamped { w, h } dimensions no smaller than the type minimum
 */
export function clampSize(
    w: number,
    h: number,
    type: 'signature' | 'date',
): { w: number; h: number } {
    if (type === 'signature') {
        return {
            w: Math.max(w, SIGNATURE_MIN_WIDTH),
            h: Math.max(h, SIGNATURE_MIN_HEIGHT),
        };
    } else {
        return {
            w: Math.max(w, DATE_MIN_WIDTH),
            h: Math.max(h, DATE_MIN_HEIGHT),
        };
    }
}
