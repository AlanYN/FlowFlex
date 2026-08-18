declare module 'vue-signature-pad' {
    import type { DefineComponent } from 'vue';

    export interface SignaturePadOptions {
        /** Dot size (Float or a function) */
        dotSize?: number | (() => number);
        /** Minimum width of a line. Default: 0.5 */
        minWidth?: number;
        /** Maximum width of a line. Default: 2.5 */
        maxWidth?: number;
        /** Speed that the stroke's width changes. Default: 0 */
        throttle?: number;
        /** How much the previous and next point is followed. Default: 0.7 */
        minDistance?: number;
        /** Color used to clear the background. Default: rgba(0,0,0,0) */
        backgroundColor?: string;
        /** Color of the stroke. Default: black */
        penColor?: string;
        /** Weight used to modify new velocity. Default: 0.7 */
        velocityFilterWeight?: number;
        /** Custom resize handler */
        resizeHandler?: (this: VueSignaturePadInstance) => void;
        /** Callback when stroke begins */
        onBegin?: (event: MouseEvent | TouchEvent) => void;
        /** Callback when stroke ends */
        onEnd?: (event: MouseEvent | TouchEvent) => void;
    }

    export interface SaveSignatureResult {
        isEmpty: boolean;
        data: string | undefined;
    }

    export interface VueSignaturePadInstance {
        saveSignature(type?: string, encoderOptions?: number): SaveSignatureResult;
        undoSignature(): void;
        clearSignature(): void;
        lockSignaturePad(): void;
        openSignaturePad(): void;
        isEmpty(): boolean;
        fromDataURL(data: string, options?: object, callback?: () => void): void;
        fromData(data: object[]): void;
        toData(): object[];
        mergeImageAndSignature(customSignature: string): Promise<string>;
        addImages(images?: string[]): Promise<string>;
        getPropImagesAndCacheImages(): string[];
        clearCacheImages(): string[];
    }

    export interface VueSignaturePadProps {
        width?: string;
        height?: string;
        customStyle?: Record<string, string>;
        options?: SignaturePadOptions;
        images?: string[];
        scaleToDevicePixelRatio?: boolean;
    }

    const VueSignaturePad: DefineComponent<VueSignaturePadProps>;

    export { VueSignaturePad };
    export default VueSignaturePad;
}
