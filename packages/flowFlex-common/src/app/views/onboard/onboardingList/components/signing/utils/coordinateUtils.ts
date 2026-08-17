/**
 * Coordinate conversion utilities for PDF signing.
 *
 * Coordinate systems:
 * - Canvas pixels (canvasPx): raw pixel positions on the rendered HTML canvas
 * - PDF.js points (pdfJsPt): canvas pixels divided by viewport scale; top-left origin
 * - pdf-lib coordinates: bottom-left origin; used when embedding elements into the PDF
 */

/**
 * Converts a canvas pixel value to PDF.js points by removing the viewport scale factor.
 *
 * @param canvasPx - Value in canvas pixels
 * @param scale - PDF.js viewport scale (e.g. 1.0 for 100%, 1.5 for 150%)
 * @returns Value in PDF.js points (top-left origin)
 */
export function toPdfJsPt(canvasPx: number, scale: number): number {
    return canvasPx / scale;
}

/**
 * Converts a PDF.js Y coordinate (top-left origin) to a pdf-lib Y coordinate (bottom-left origin).
 *
 * Formula: pdfLibY = pageHeight - pdfJsY - elementHeight
 * Validates: Requirements 14.3
 *
 * @param pdfJsY - Y coordinate in PDF.js points (top-left origin)
 * @param pageHeight - Total page height in PDF.js points
 * @param elementHeight - Height of the element in PDF.js points
 * @returns Y coordinate in pdf-lib points (bottom-left origin)
 */
export function toPdfLibY(pdfJsY: number, pageHeight: number, elementHeight: number): number {
    return pageHeight - pdfJsY - elementHeight;
}

/**
 * Comprehensive coordinate conversion from canvas pixels to pdf-lib coordinates.
 *
 * Combines toPdfJsPt and toPdfLibY into a single convenience function.
 * X coordinate only needs the scale conversion (origin is the same horizontal direction).
 * Y coordinate needs both scale conversion and origin flip.
 *
 * Validates: Requirements 14.3
 *
 * @param x - X position in canvas pixels (top-left origin)
 * @param y - Y position in canvas pixels (top-left origin)
 * @param w - Width in canvas pixels
 * @param h - Height in canvas pixels
 * @param scale - PDF.js viewport scale
 * @param pageHeight - Page height in PDF.js points
 * @returns Coordinates and dimensions in pdf-lib points (bottom-left origin)
 */
export function toPdfLibCoords(
    x: number,
    y: number,
    w: number,
    h: number,
    scale: number,
    pageHeight: number,
): { x: number; y: number; w: number; h: number } {
    const pdfJsX = toPdfJsPt(x, scale);
    const pdfJsY = toPdfJsPt(y, scale);
    const pdfJsW = toPdfJsPt(w, scale);
    const pdfJsH = toPdfJsPt(h, scale);

    return {
        x: pdfJsX,
        y: toPdfLibY(pdfJsY, pageHeight, pdfJsH),
        w: pdfJsW,
        h: pdfJsH,
    };
}
