<script setup lang="ts">
import { ref, nextTick, onMounted } from 'vue';
import { VueSignaturePad, type VueSignaturePadInstance } from 'vue-signature-pad';
import { ElMessage } from 'element-plus';

interface Props {
	mode: 'profile' | 'signing';
}

const props = withDefaults(defineProps<Props>(), {
	mode: 'signing',
});

const emit = defineEmits<{
	save: [imageBase64: string];
	use: [imageBase64: string];
}>();

const signaturePadRef = ref<VueSignaturePadInstance | null>(null);

/**
 * Resizes the signature pad to match the actual rendered canvas dimensions.
 * Must be called after the containing dialog becomes visible, because
 * VueSignaturePad initialises canvas size at mount time — if the dialog
 * was hidden at mount, the canvas size is 0×0 and nothing gets drawn.
 */
async function resizePad(): Promise<void> {
	await nextTick();
	signaturePadRef.value?.resizeCanvas?.();
}

onMounted(() => {
	// Attempt resize on mount in case dialog is already visible
	resizePad();
});

/**
 * Clears the canvas programmatically (called by parent when dialog re-opens).
 */
async function clearPad(): Promise<void> {
	await nextTick();
	signaturePadRef.value?.clearSignature();
}

defineExpose({ resizePad, clearPad });

const padOptions = {
	backgroundColor: 'rgba(0, 0, 0, 0)', // transparent — prevents white box over PDF content
	penColor: 'rgb(0, 0, 0)',
};

/**
 * Clears the signature canvas without closing any dialog.
 * Validates: Requirements 3.3, 11.7
 */
function handleClear() {
	signaturePadRef.value?.clearSignature();
}

/**
 * Reads the canvas data and validates it is not empty.
 * Returns a base64 PNG with a transparent background.
 */
async function getSignatureData(): Promise<string | null> {
	const result = signaturePadRef.value?.saveSignature('image/png');
	if (!result || result.isEmpty || !result.data) {
		ElMessage.warning('Please draw your signature first.');
		return null;
	}

	// Redraw on a fresh transparent canvas to strip any white background
	// that the signature pad may have applied during initialisation.
	return new Promise((resolve) => {
		const img = new Image();
		img.onload = () => {
			const canvas = document.createElement('canvas');
			canvas.width = img.width;
			canvas.height = img.height;
			const ctx = canvas.getContext('2d');
			if (ctx) {
				// Do NOT call fillRect — transparent by default
				ctx.drawImage(img, 0, 0);
				resolve(canvas.toDataURL('image/png'));
			} else {
				resolve(result.data); // fallback
			}
		};
		img.onerror = () => resolve(result.data); // fallback
		img.src = result.data;
	});
}

/**
 * Profile mode: saves the drawn signature as base64 PNG.
 * Validates: Requirements 3.2
 */
async function handleSave() {
	const data = await getSignatureData();
	if (data) {
		emit('save', data);
	}
}

/**
 * Signing mode: emits the drawn signature for placement on the PDF.
 * The signature is NOT saved to the user's profile (Requirement 11.8).
 * Validates: Requirements 11.6, 11.8
 */
async function handleUse() {
	const data = await getSignatureData();
	if (data) {
		emit('use', data);
	}
}
</script>

<template>
	<div class="draw-tab flex flex-col items-center gap-4 py-4">
		<p class="text-sm text-gray-500">Draw your signature below.</p>

		<!-- Signature canvas: 300×150px, white background -->
		<div class="signature-canvas-wrapper border border-gray-300 rounded">
			<VueSignaturePad
				ref="signaturePadRef"
				width="300px"
				height="150px"
				:options="padOptions"
			/>
		</div>

		<!-- Action buttons -->
		<div class="flex items-center gap-3">
			<!-- Clear button: always visible, clears canvas without closing dialog -->
			<el-button @click="handleClear">Clear</el-button>

			<!-- Profile mode: Save button saves to user's signature list -->
			<el-button v-if="props.mode === 'profile'" type="primary" @click="handleSave">
				Save
			</el-button>

			<!-- Signing mode: Use Signature button places on PDF, does NOT save to profile -->
			<el-button v-if="props.mode === 'signing'" type="primary" @click="handleUse">
				Use Signature
			</el-button>
		</div>
	</div>
</template>

<style scoped lang="scss">
.signature-canvas-wrapper {
	background-color: #ffffff;
	overflow: hidden;
}
</style>
