/**
 * Shared type definitions for the User Profile & Document Signing feature.
 * These types are used across both the Profile page (OW-703) and the
 * Document Signing workflow (OW-704).
 */

export interface SignatureItem {
    id: string;
    imageBase64: string;
    createdDate: string; // ISO 8601
}

export interface PlacedElement {
    id: string; // local UUID
    type: 'signature' | 'date';
    pageIndex: number; // 0-based
    /** Stored in PDF.js pt (viewport.scale already cancelled out), NOT canvas px */
    x: number;
    y: number;
    width: number;
    height: number;
    imageBase64?: string; // when type==='signature'
    dateText?: string; // when type==='date', format MM/DD/YYYY
}

export interface SignedFileResponse {
    signedFileId: string;
    downloadUrl: string;
    fileName: string;
    fileHash: string; // SHA-256 hex
}
