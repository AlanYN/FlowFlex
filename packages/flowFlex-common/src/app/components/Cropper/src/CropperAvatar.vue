<template>
	<div class="activeSrc_block">
		<div
			:class="`${prefixCls}-image-wrapper`"
			:style="getImageWrapperStyle"
			@click="openModal()"
		>
			<div :class="`${prefixCls}-image-mask`" class="flex" :style="getImageWrapperStyle">
				<Upload class="w-[30px] h-[30px] m-auto" />
			</div>
			<img :src="sourceValue" v-if="sourceValue" alt="avatar" />
		</div>
		<div>
			<div style="display: flex">
				<el-button
					:class="`${prefixCls}-upload-btn`"
					@click="openModal"
					v-if="showBtn"
					v-bind="btnProps"
				>
					{{ sourceValue ? 'Replace image' : t('sys.cropper.selectImage') }}
				</el-button>
				<el-button
					v-if="sourceValue"
					:class="`${prefixCls}-upload-btn`"
					class="removeBUtton"
					@click="removeImage"
					v-bind="btnProps"
				>
					{{ btnText ? btnText : t('sys.upload.removeImage') }}
				</el-button>
			</div>
			<div class="activeSrc_block-tips">
				.jpg, .png, .jpeg files up to 8MB.Recommended size 256x256px
			</div>
			<!--v-if="!sourceValue" <div class="activeSrc_block-tips" v-else>{{ getValueName }}</div> -->
		</div>

		<CropperModal
			ref="cropperModalRef"
			@upload-success="handleUploadSuccess"
			:uploadApi="uploadApi"
			:size="size"
			@upload-error="fileUploadError"
		/>
	</div>
</template>
<script lang="ts" setup>
import { computed, CSSProperties, unref, ref, PropType, nextTick } from 'vue';
import CropperModal from './CropperModal.vue';
import { useI18n } from '@/hooks/useI18n';
import { ElMessage } from 'element-plus';
import { Upload } from '@element-plus/icons-vue';

defineOptions({ name: 'CropperAvatar' });

const props = defineProps({
	width: { type: [String, Number], default: '100px' },
	showBtn: { type: Boolean, default: true },
	btnProps: { type: Object as PropType<any> },
	btnText: { type: String, default: '' },
	uploadApi: {
		type: Function as PropType<({ file, name }: { file: Blob; name: string }) => Promise<void>>,
	},

	size: { type: Number, default: 5 },
});

const fileId = ref('');

const emit = defineEmits(['update:value', 'change', 'remove']);

const sourceValue = ref('');
const prefixCls = 'cropper-avatar';
const { t } = useI18n();

const getWidth = computed(() => `${props.width}`.replace(/px/, '') + 'px');

const getImageWrapperStyle = computed(
	(): CSSProperties => ({ width: unref(getWidth), height: unref(getWidth) })
);

function handleUploadSuccess({ source, data }) {
	if (data?.data?.code == '200') {
		nextTick(() => {
			fileId.value = source.fileId;
			emit('change', { source, data: data.data });
			ElMessage.success(t('sys.cropper.uploadSuccess'));
			cropperModalRef.value?.cancel();
		});
	} else {
		sourceValue.value = '';
		ElMessage.error(t('sys.upload.uploadError'));
	}
}

const fileUploadError = (data) => {
	ElMessage.error(data.msg);
};

const cropperModalRef = ref<{
	cancel: () => void;
	openDialog: () => void;
}>();
const openModal = () => {
	cropperModalRef.value?.openDialog();
};

const removeImage = () => {
	emit('remove');
};

const setAvatar = (id) => {};

defineExpose({
	setAvatar,
});
</script>

<style lang="scss" scoped>
.activeSrc_block {
	display: flex;
	align-items: center;
	justify-content: center;
	margin-bottom: 40px;
}

.activeSrc_block-tips {
	margin-left: 20px;
	color: #606266;
}

.cropper-avatar {
	display: inline-block;
	text-align: center;

	&-image-wrapper {
		@apply bg-black-300 dark:bg-black-300;
		position: relative;
		overflow: hidden;
		border: 1px solid;
		border-radius: 50%;
		cursor: pointer;

		img {
			width: 100%;
		}
	}

	&-image-mask {
		position: absolute;
		width: inherit;
		height: inherit;
		transition: opacity 0.4s;
		border: inherit;
		border-radius: inherit;
		opacity: 0;
		background: rgb(0 0 0 / 40%);
		cursor: pointer;
	}

	&-image-mask:hover {
		opacity: 40;
	}

	&-upload-btn {
		margin: 10px;
	}
}

.removeBUtton {
	color: #f00;
}
</style>
