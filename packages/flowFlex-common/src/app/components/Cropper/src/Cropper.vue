<template>
	<div :style="getWrapperStyle">
		<img
			v-show="isReady"
			ref="imgElRef"
			:src="src"
			:alt="alt"
			:crossorigin="crossorigin"
			:style="getImageStyle"
		/>
	</div>
</template>
<script lang="ts" setup>
import type { CSSProperties, PropType } from 'vue';
import { onMounted, ref, unref, computed, onUnmounted } from 'vue';
import Cropper from 'cropperjs';
import 'cropperjs/dist/cropper.css';
import { useDebounceFn } from '@vueuse/core';

type Options = Cropper.Options;

const defaultOptions: Options = {
	aspectRatio: 1,
	zoomable: true,
	zoomOnTouch: true,
	zoomOnWheel: true,
	cropBoxMovable: true,
	cropBoxResizable: true,
	toggleDragModeOnDblclick: true,
	autoCrop: true,
	background: true,
	highlight: true,
	center: true,
	responsive: true,
	restore: true,
	checkCrossOrigin: true,
	checkOrientation: true,
	scalable: true,
	modal: true,
	guides: true,
	movable: true,
	rotatable: true,
};

defineOptions({ name: 'CropperImage' });

const props = defineProps({
	src: { type: String, required: true },
	alt: { type: String },
	circled: { type: Boolean, default: false },
	realTimePreview: { type: Boolean, default: true },
	height: { type: [String, Number], default: '360px' },
	crossorigin: {
		type: String as PropType<'' | 'anonymous' | 'use-credentials' | undefined>,
		default: undefined,
	},
	imageStyle: { type: Object as PropType<CSSProperties>, default: () => ({}) },
	options: { type: Object as PropType<Options>, default: () => ({}) },
});

const emit = defineEmits(['cropend', 'ready', 'cropendError']);

const imgElRef = ref<HTMLImageElement>();
const cropper = ref<Cropper>();
const isReady = ref(false);

const debounceRealTimeCroppered = useDebounceFn(realTimeCroppered, 80);

const getImageStyle = computed((): CSSProperties => {
	return {
		height: props.height,
		maxWidth: '100%',
		...props.imageStyle,
	};
});

const getWrapperStyle = computed((): CSSProperties => {
	return { height: `${props.height}`.replace(/px/, '') + 'px' };
});

onMounted(init);

onUnmounted(() => {
	cropper.value?.destroy();
});

async function init() {
	const imgEl = unref(imgElRef);
	if (!imgEl) {
		return;
	}
	cropper.value = new Cropper(imgEl, {
		...defaultOptions,
		ready: () => {
			isReady.value = true;
			realTimeCroppered();
			emit('ready', cropper.value);
		},
		crop() {
			debounceRealTimeCroppered();
		},
		zoom() {
			debounceRealTimeCroppered();
		},
		cropmove() {
			debounceRealTimeCroppered();
		},
		...props.options,
	});
}

// Real-time display preview
function realTimeCroppered() {
	props.realTimePreview && croppered();
}

// event: return base64 and width and height information after cropping
function croppered() {
	if (!cropper.value) {
		return;
	}
	let imgInfo = cropper.value.getData();
	const canvas = props.circled ? getRoundedCanvas() : cropper.value.getCroppedCanvas();
	canvas.toBlob((blob) => {
		if (!blob) {
			return;
		}
		let fileReader: FileReader = new FileReader();
		fileReader.readAsDataURL(blob);
		fileReader.onloadend = (e) => {
			emit('cropend', {
				imgBase64: e.target?.result ?? '',
				imgInfo,
			});
		};
		fileReader.onerror = () => {
			emit('cropendError');
		};
	}, 'image/png');
}

// Get a circular picture canvas
function getRoundedCanvas() {
	const sourceCanvas = cropper.value!.getCroppedCanvas();
	const canvas = document.createElement('canvas');
	const context = canvas.getContext('2d')!;
	const width = sourceCanvas.width;
	const height = sourceCanvas.height;
	canvas.width = width;
	canvas.height = height;
	context.imageSmoothingEnabled = true;
	context.drawImage(sourceCanvas, 0, 0, width, height);
	context.globalCompositeOperation = 'destination-in';
	context.beginPath();
	context.arc(width / 2, height / 2, Math.min(width, height) / 2, 0, 2 * Math.PI, true);
	context.fill();
	return canvas;
}
</script>

<style lang="scss">
.cropper-image {
	&--circled {
		.cropper-view-box,
		.cropper-face {
			border-radius: 50%;
		}
	}
}
</style>
