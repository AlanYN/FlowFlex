<script setup lang="ts">
import { ref } from 'vue';
import { ElMessage } from 'element-plus';
import { Loading } from '@element-plus/icons-vue';
import type { UploadRawFile } from 'element-plus';
import DrawTab from '@/views/onboard/onboardingList/components/signing/DrawTab.vue';
import { createSignature } from '@/apis/ow/profile';

// ========================= Props & Emits =========================

interface Props {
	visible: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	visible: false,
});

const emit = defineEmits<{
	'update:visible': [value: boolean];
	saved: [];
}>();

// ========================= State =========================

const activeTab = ref<'draw' | 'upload'>('draw');
const isSaving = ref(false);

// Upload tab state
const uploadedFile = ref<UploadRawFile | null>(null);
const previewUrl = ref<string>('');

// ========================= Constants =========================

const MAX_FILE_SIZE = 500 * 1024; // 500KB in bytes
const ALLOWED_TYPES = ['image/png', 'image/jpeg'];

// ========================= Dialog =========================

function handleClose() {
	emit('update:visible', false);
	resetUploadState();
}

function resetUploadState() {
	uploadedFile.value = null;
	previewUrl.value = '';
}

// ========================= Draw Tab =========================

/**
 * Called when DrawTab emits 'save' with base64 image data.
 * Calls createSignature API, emits 'saved', and closes dialog.
 * Validates: Requirements 3.1, 3.2
 */
async function handleDrawSave(imageBase64: string) {
	if (isSaving.value) return;
	isSaving.value = true;
	try {
		await createSignature(imageBase64);
		emit('saved');
		handleClose();
	} catch (error: unknown) {
		const msg =
			error instanceof Error ? error.message : 'Failed to save signature. Please try again.';
		ElMessage.error(msg);
	} finally {
		isSaving.value = false;
	}
}

// ========================= Upload Tab =========================

/**
 * Validates file type and size before accepting the upload.
 * Returns false (with error message) to reject, true to accept.
 * Validates: Requirements 3.5, 3.6
 */
function beforeUpload(file: UploadRawFile): boolean {
	if (!ALLOWED_TYPES.includes(file.type)) {
		ElMessage.error('Only PNG or JPG images are supported.');
		return false;
	}
	if (file.size > MAX_FILE_SIZE) {
		ElMessage.error('Image size must not exceed 500KB.');
		return false;
	}
	return true;
}

/**
 * Handles file selection change from el-upload (manual mode).
 * Validates and stores the file, shows a preview.
 * Validates: Requirements 3.4
 */
function handleFileChange(uploadFile: { raw: UploadRawFile }) {
	const file = uploadFile.raw;
	if (!beforeUpload(file)) {
		return;
	}

	// Revoke previous object URL to avoid memory leaks
	if (previewUrl.value) {
		URL.revokeObjectURL(previewUrl.value);
	}

	uploadedFile.value = file;
	previewUrl.value = URL.createObjectURL(file);
}

/**
 * Reads the selected file as base64 and calls createSignature.
 * Validates: Requirements 3.4, 3.7
 */
async function handleUploadSave() {
	if (!uploadedFile.value) {
		ElMessage.warning('Please select an image first.');
		return;
	}
	if (isSaving.value) return;

	isSaving.value = true;
	try {
		const imageBase64 = await readFileAsBase64(uploadedFile.value);
		await createSignature(imageBase64);
		emit('saved');
		handleClose();
	} catch (error: unknown) {
		const msg =
			error instanceof Error ? error.message : 'Failed to save signature. Please try again.';
		ElMessage.error(msg);
	} finally {
		isSaving.value = false;
	}
}

/**
 * Reads a File object and returns its base64-encoded data URL string.
 */
function readFileAsBase64(file: File): Promise<string> {
	return new Promise((resolve, reject) => {
		const reader = new FileReader();
		reader.onload = () => resolve(reader.result as string);
		reader.onerror = () => reject(new Error('Failed to read file'));
		reader.readAsDataURL(file);
	});
}
</script>

<template>
	<el-dialog
		:model-value="props.visible"
		title="Add Signature"
		width="480px"
		:close-on-click-modal="false"
		:close-on-press-escape="!isSaving"
		append-to-body
		:before-close="handleClose"
	>
		<el-tabs v-model="activeTab">
			<!-- ===== Draw Tab ===== -->
			<el-tab-pane label="Draw" name="draw">
				<!-- DrawTab handles its own canvas + Clear + Save buttons.
                     mode='profile' means clicking Save emits 'save', not 'use'. -->
				<DrawTab mode="profile" :disabled="isSaving" @save="handleDrawSave" />

				<!-- Loading overlay during API call -->
				<div
					v-if="isSaving"
					class="flex items-center justify-center py-2 text-sm text-gray-500"
				>
					<el-icon class="is-loading mr-1"><Loading /></el-icon>
					Saving…
				</div>
			</el-tab-pane>

			<!-- ===== Upload Tab ===== -->
			<el-tab-pane label="Upload" name="upload">
				<div class="flex flex-col gap-4 py-2">
					<!-- Guidance text. Validates: Requirement 3.7 -->
					<el-alert type="info" :closable="false" show-icon>
						We recommend uploading a PNG with a transparent background. White-background
						images will appear as a white block on the PDF.
					</el-alert>

					<!-- File picker (manual upload, no auto-upload) -->
					<el-upload
						action="#"
						:auto-upload="false"
						accept="image/png,image/jpeg"
						:show-file-list="false"
						:on-change="handleFileChange"
						:disabled="isSaving"
					>
						<el-button :disabled="isSaving">Choose Image</el-button>
						<template #tip>
							<div class="text-xs text-gray-400 mt-1">
								PNG / JPG supported, max 500KB
							</div>
						</template>
					</el-upload>

					<!-- Preview on simulated PDF background. Validates: Requirement 3.4 -->
					<div
						v-if="previewUrl"
						class="pdf-preview-container flex items-center justify-center rounded border border-gray-300 bg-gray-100 p-4"
					>
						<img
							:src="previewUrl"
							alt="Signature preview"
							class="max-h-32 max-w-full object-contain"
						/>
					</div>

					<!-- Empty preview placeholder -->
					<div
						v-else
						class="pdf-preview-container flex items-center justify-center rounded border border-dashed border-gray-300 bg-gray-100 p-4 text-sm text-gray-400"
					>
						Image preview (simulated PDF background)
					</div>

					<!-- Save button -->
					<div class="flex justify-end">
						<el-button
							type="primary"
							:loading="isSaving"
							:disabled="!uploadedFile || isSaving"
							@click="handleUploadSave"
						>
							Save
						</el-button>
					</div>
				</div>
			</el-tab-pane>
		</el-tabs>
	</el-dialog>
</template>

<style scoped lang="scss">
.pdf-preview-container {
	min-height: 120px;
	background-color: #f5f5f5;
	/* Simulates a light paper/PDF page background */
	box-shadow: inset 0 1px 3px rgba(0, 0, 0, 0.08);
}
</style>
