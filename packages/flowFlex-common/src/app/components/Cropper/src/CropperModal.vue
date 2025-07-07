<template>
	<el-dialog
		v-model="drawer"
		:close-on-click-modal="false"
		:before-close="cancel"
		:title="t('sys.cropper.modalTitle')"
		:width="bigDialogWidth"
		draggable
	>
		<div :class="prefixCls">
			<div :class="`${prefixCls}-left`">
				<div :class="`${prefixCls}-cropper`">
					<CropperImage
						v-if="src"
						:src="src"
						height="300px"
						:circled="circled"
						@cropend="handleCropend"
						@ready="handleReady"
					/>
				</div>

				<div :class="`${prefixCls}-toolbar`">
					<el-upload
						:fileList="[]"
						accept=".jpg, .png, .jpeg"
						:beforeUpload="handleBeforeUpload"
						class="flex"
					>
						<el-tooltip :content="t('sys.cropper.selectImage')" placement="bottom">
							<el-button size="small" :icon="Upload" type="primary" />
						</el-tooltip>
					</el-upload>
					<div class="flex">
						<el-tooltip :content="t('sys.cropper.btn_reset')" placement="bottom">
							<el-button
								type="primary"
								:icon="ReloadOutlinedIcon"
								size="small"
								:disabled="!src"
								@click="handlerToolbar('reset')"
							/>
						</el-tooltip>
						<el-tooltip :content="t('sys.cropper.btn_rotate_left')" placement="bottom">
							<el-button
								type="primary"
								:icon="RotateLeftOutlinedIcon"
								size="small"
								:disabled="!src"
								@click="handlerToolbar('rotate', -45)"
							/>
						</el-tooltip>
						<el-tooltip :content="t('sys.cropper.btn_rotate_right')" placement="bottom">
							<el-button
								type="primary"
								:icon="RotateRightOutlinedIcon"
								size="small"
								:disabled="!src"
								@click="handlerToolbar('rotate', 45)"
							/>
						</el-tooltip>
						<el-tooltip :content="t('sys.cropper.btn_scale_x')" placement="bottom">
							<el-button
								type="primary"
								:icon="ArrowsLongVIcon"
								size="small"
								:disabled="!src"
								@click="handlerToolbar('scaleX')"
							/>
						</el-tooltip>
						<el-tooltip :content="t('sys.cropper.btn_scale_y')" placement="bottom">
							<el-button
								type="primary"
								:icon="ArrowsLongHIcon"
								size="small"
								:disabled="!src"
								@click="handlerToolbar('scaleY')"
							/>
						</el-tooltip>
						<el-tooltip :content="t('sys.cropper.btn_zoom_in')" placement="bottom">
							<el-button
								type="primary"
								:icon="ZoomInIcon"
								size="small"
								:disabled="!src"
								@click="handlerToolbar('zoom', 0.1)"
							/>
						</el-tooltip>
						<el-tooltip :content="t('sys.cropper.btn_zoom_out')" placement="bottom">
							<el-button
								type="primary"
								:icon="ZoomOutIcon"
								size="small"
								:disabled="!src"
								@click="handlerToolbar('zoom', -0.1)"
							/>
						</el-tooltip>
					</div>
				</div>
			</div>
			<div :class="`${prefixCls}-right`">
				<div :class="`${prefixCls}-preview`">
					<img
						:src="previewSource"
						v-if="previewSource"
						:alt="t('sys.cropper.preview')"
					/>
				</div>
				<template v-if="previewSource">
					<div :class="`${prefixCls}-group`">
						<el-avatar :src="previewSource" size="large" />
						<el-avatar :src="previewSource" :size="48" />
						<el-avatar :src="previewSource" :size="64" />
						<el-avatar :src="previewSource" :size="80" />
					</div>
				</template>
			</div>
		</div>
		<template #footer>
			<div class="dialog-footer">
				<el-button @click="cancel">Cancel</el-button>
				<el-button
					type="primary"
					:disabled="!previewSource"
					:loading="isUploading"
					@click="handleOk"
				>
					{{ t('sys.button.dialogConfig') }}
				</el-button>
			</div>
		</template>
	</el-dialog>
</template>
<script lang="ts" setup>
import type { CropendResult, Cropper } from './typing';

import { ref, PropType } from 'vue';
import CropperImage from './Cropper.vue';
import { dataURLtoBlob } from '@/utils/file';
import { isFunction } from '@/utils/is';
import { useI18n } from '@/hooks/useI18n';
import { Upload } from '@element-plus/icons-vue';
import ReloadOutlinedIcon from '@assets/svg/global/reloadOutlined.svg';
import ZoomInIcon from '@assets/svg/global/zoomIn.svg';
import ZoomOutIcon from '@assets/svg/global/zoomOut.svg';
import ArrowsLongVIcon from '@assets/svg/global/arrowsLongV.svg';
import ArrowsLongHIcon from '@assets/svg/global/arrowsLongH.svg';
import RotateLeftOutlinedIcon from '@assets/svg/global/rotateLeftOutlined.svg';
import RotateRightOutlinedIcon from '@assets/svg/global/rotateRightOutlined.svg';

import { bigDialogWidth } from '@/settings/projectSetting';
import { ElMessage } from 'element-plus';

type apiFunParams = { file: Blob; name: string; filename: string };

defineOptions({ name: 'CropperModal' });

const props = defineProps({
	circled: { type: Boolean, default: true },
	uploadApi: {
		type: Function as PropType<
			(params: apiFunParams, onUploadProgress?: (progressEvent) => void) => Promise<any>
		>,
	},
	size: { type: Number },
});

const emit = defineEmits(['uploadSuccess', 'uploadError', 'register']);

const src = ref('');
const fileName = ref('');
const previewSource = ref('');
const cropper = ref<Cropper>();
let scaleX = 1;
let scaleY = 1;

const prefixCls = 'cropper-am';
const { t } = useI18n();

// Block upload
function handleBeforeUpload(file: File) {
	if (props.size && file.size > 1024 * 1024 * props.size) {
		emit('uploadError', { msg: t('sys.cropper.imageTooBig') });
		return;
	}
	const reader = new FileReader();
	reader.readAsDataURL(file);
	src.value = '';
	previewSource.value = '';
	reader.onload = function (e) {
		src.value = (e.target?.result as string) ?? '';
		fileName.value = file.name;
	};
	return false;
}

function handleCropend({ imgBase64 }: CropendResult) {
	previewSource.value = imgBase64;
}

function handleReady(cropperInstance: Cropper) {
	cropper.value = cropperInstance;
}

function handlerToolbar(event: string, arg?: number) {
	if (event === 'scaleX') {
		scaleX = arg = scaleX === -1 ? 1 : -1;
	}
	if (event === 'scaleY') {
		scaleY = arg = scaleY === -1 ? 1 : -1;
	}
	cropper?.value?.[event]?.(arg);
}

const isUploading = ref(false);
async function handleOk() {
	const uploadApi = props.uploadApi;
	if (uploadApi && isFunction(uploadApi)) {
		try {
			isUploading.value = true;
			const blob = dataURLtoBlob(previewSource.value);
			const res = await uploadApi({ file: blob, name: 'formFile', filename: fileName.value });
			emit('uploadSuccess', {
				source: {
					file: blob,
					fileId: res?.data?.data?.id,
				},
				data: res,
			});
		} catch {
			ElMessage.error(t('sys.upload.uploadError'));
		} finally {
			isUploading.value = false;
		}
	}
}

const drawer = ref(false);
const openDialog = () => {
	drawer.value = true;
};

const cancel = () => {
	src.value = '';
	previewSource.value = '';
	fileName.value = '';
	drawer.value = false;
};

defineExpose({
	openDialog,
	cancel,
});
</script>

<style lang="scss">
.cropper-am {
	display: flex;

	&-left,
	&-right {
		height: 340px;
	}

	&-left {
		width: 55%;
	}

	&-right {
		width: 45%;
	}

	&-cropper {
		height: 300px;
		background: #eee;
		background-image: linear-gradient(
				45deg,
				rgb(0 0 0 / 25%) 25%,
				transparent 0,
				transparent 75%,
				rgb(0 0 0 / 25%) 0
			),
			linear-gradient(
				45deg,
				rgb(0 0 0 / 25%) 25%,
				transparent 0,
				transparent 75%,
				rgb(0 0 0 / 25%) 0
			);
		background-position:
			0 0,
			12px 12px;
		background-size: 24px 24px;
	}

	&-toolbar {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-top: 10px;
	}

	&-preview {
		width: 220px;
		height: 220px;
		margin: 0 auto;
		overflow: hidden;
		border: 1px solid #000;
		border-radius: 50%;

		img {
			width: 100%;
			height: 100%;
		}
	}

	&-group {
		display: flex;
		align-items: center;
		justify-content: space-around;
		margin-top: 8px;
		padding-top: 8px;
		border-top: 1px solid #000;
	}
}
</style>
