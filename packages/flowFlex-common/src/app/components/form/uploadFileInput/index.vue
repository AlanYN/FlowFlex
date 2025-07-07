<template>
	<div class="w-full flex flex-col justify-end pr-2">
		<!-- Upload Button -->
		<el-upload
			class="upload-file"
			:auto-upload="true"
			:show-file-list="true"
			:http-request="handleUpload"
			:on-preview="handlePreview"
			:beforeRemove="deleteFile"
			:accept="property.accept"
			:disabled="disabled"
			:loading="uploadLoading"
			:file-list="fileList"
			:on-exceed="handleExceed"
			:limit="property?.limit ? +property?.limit : 1"
		>
			<div class="w-full flex items-center justify-between h-[31px]">
				<slot></slot>
				<el-button
					:disabled="disabled"
					link
					class="whitespace-normal break-all leading-6 group relative"
				>
					<el-icon class="leading-6"><FirstAidKit /></el-icon>
				</el-button>
			</div>
		</el-upload>
	</div>
</template>

<script lang="ts" setup>
import { ref, computed, toRaw } from 'vue';
import { ElMessage } from 'element-plus';
import { FirstAidKit } from '@element-plus/icons-vue';
import { globalUploadFile, globalDeleteFile } from '@/apis/global';
import { fileAttachment } from '@/apis/global';
import { useI18n } from '@/hooks/useI18n';

const { t } = useI18n();

interface UploadResponse {
	code: string;
	data: {
		id: string;
		realName: string;
	};
	message?: string;
}

interface Props {
	modelValue: any;
	disabled?: boolean;
	property?: {
		limit?: string;
		accept?: string;
	};
}

const props = withDefaults(defineProps<Props>(), {
	property: () => ({
		limit: '1',
		accept: '.pdf, .docx, .doc, .jpg, .jpeg, .png, .xlsx, .msg, .eml',
	}),
});

const emit = defineEmits(['update:modelValue', 'change']);

const uploadLoading = ref(false);
const fileList = computed({
	get() {
		return !props.modelValue || typeof props.modelValue === 'string' ? [] : props.modelValue;
	},
	set(value) {
		emit('update:modelValue', value);
	},
});

// Handle file upload
const handleUpload = async (uploadFile: any) => {
	return new Promise((resolve, reject) => {
		try {
			uploadLoading.value = true;
			const { file } = uploadFile;
			globalUploadFile(
				{
					name: 'formFile',
					file: file,
					filename: file.name,
				},
				(progressEvent) => {
					const percent = Math.round((progressEvent.loaded * 100) / progressEvent.total);
					uploadFile.onProgress({ percent });
				}
			)
				.then((res: { data: UploadResponse }) => {
					if (res?.data?.code === '200') {
						const fileData = res.data.data;
						const newFile = {
							uid: file.uid,
							size: file.size,
							type: file.type,
							id: fileData.id,
							name: fileData.realName,
							status: 'success',
						};
						resolve(newFile);
						const updatedFiles = [...toRaw(fileList.value), newFile].map((item) => {
							return {
								uid: item.uid,
								size: item.size,
								type: item.type,
								id: item.id,
								name: item.name,
								status: item.status,
							};
						});
						fileList.value = updatedFiles;
						emit('change', updatedFiles);
						ElMessage.success('Upload successful');
					} else {
						ElMessage.error(res?.data?.message || 'Upload failed');
						reject(false);
					}
				})
				.catch((err) => {
					reject(false);
				});
		} catch (error) {
			console.error('Upload failed:', error);
			ElMessage.error('Upload failed');
			reject(false);
		} finally {
			uploadLoading.value = false;
		}
	});
};

const handlePreview = async (file) => {
	try {
		uploadLoading.value = true;
		const response = await fileAttachment(file.id);
		downloadFile(response, file);
	} catch (error) {
		ElMessage.error(t('sys.api.operationFailed'));
		console.error('Error downloading file:', error);
	} finally {
		uploadLoading.value = false;
	}
};

const downloadFile = (res, file) => {
	const blob = new Blob([res], { type: 'application/octet-stream' });
	const link = document.createElement('a');
	link.download = file.name;
	link.href = URL.createObjectURL(blob);
	document.body.appendChild(link);
	link.click();
	window.URL.revokeObjectURL(link.href);
	document.body.removeChild(link);
};

const deleteFile = async (file) => {
	if (file.status != 'success' && !file.id) return true;
	try {
		uploadLoading.value = true;
		const res = await globalDeleteFile(file.id);
		if (res.code == '200') {
			fileList.value = fileList.value.filter((item) => item.id !== file.id);
			ElMessage.success(t('sys.api.operationSuccess'));
		} else {
			return false;
		}
	} catch (error) {
		ElMessage.error(t('sys.api.operationFailed'));
		console.error('Error deleting file:', error);
		return false;
	} finally {
		uploadLoading.value = false;
	}
};

const handleExceed = () => {
	ElMessage.warning(`Only ${props.property?.limit} file can be uploaded`);
};
</script>

<style lang="scss" scoped>
:deep(.el-upload) {
	@apply block;
}

:deep(.el-upload-list) {
	@apply m-0;
}
</style>
