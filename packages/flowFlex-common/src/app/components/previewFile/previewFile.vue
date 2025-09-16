<template>
	<div class="office_block" v-if="isShow">
		<!-- 图片预览 -->
		<ElImageViewer
			v-if="type == 'jpg' || type == 'png' || type == 'jpeg'"
			:url-list="[previewSrc]"
			:hideOnClickModal="true"
			@close="closeOffice"
		/>

		<!-- 文档预览 -->
		<div class="office_block-item" v-else>
			<!-- 顶部工具栏 -->
			<div class="preview-toolbar">
				<div class="toolbar-left">
					<el-button-group>
						<el-button :disabled="offloading" :icon="ZoomIn" @click="zoomIn" />
						<el-button :disabled="offloading" :icon="ZoomOut" @click="zoomOut" />
						<el-button :disabled="offloading" :icon="RefreshRight" @click="resetZoom" />
					</el-button-group>
				</div>
				<div class="toolbar-right">
					<el-button :icon="CloseBold" @click="closeOffice" />
				</div>
			</div>

			<!-- 预览内容区 -->
			<div
				class="preview-content"
				v-loading="offloading"
				:style="{ transform: `scale(${scale})` }"
			>
				<vue-office-docx
					v-if="type == 'docx'"
					class="office_block-office"
					:src="previewSrc"
					@rendered="rendered"
				/>

				<vue-office-excel
					v-else-if="type == 'xlsx'"
					class="office_block-office"
					:src="previewSrc"
					@rendered="rendered"
				/>

				<iframe
					v-else
					:src="previewSrc"
					frameborder="0"
					class="office_block-office"
				></iframe>
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from 'vue';
import { CloseBold, ZoomIn, ZoomOut, RefreshRight } from '@element-plus/icons-vue';
import VueOfficeDocx from '@vue-office/docx';
import VueOfficeExcel from '@vue-office/excel';

const emit = defineEmits(['closeOffice', 'renderedOffice']);

const props = defineProps({
	fileUrl: {
		type: String,
		default: '',
	},
	fileBlob: {
		type: [Blob, String],
		default: null,
	},
	type: {
		type: String,
		default: '',
	},
	isShow: {
		type: Boolean,
		default: false,
	},
	offloading: {
		type: Boolean,
		default: false,
	},
});

// 缩放控制
const scale = ref(1);
const zoomIn = () => (scale.value = Math.min(scale.value + 0.1, 2));
const zoomOut = () => (scale.value = Math.max(scale.value - 0.1, 0.5));
const resetZoom = () => (scale.value = 1);

// 计算最终的预览源
const previewSrc = computed(() => {
	if (props.fileBlob) {
		if (props.fileBlob instanceof Blob) {
			return URL.createObjectURL(props.fileBlob);
		}
		return props.fileBlob;
	}
	return props.fileUrl;
});

// 键盘快捷键
const handleKeydown = (e: KeyboardEvent) => {
	if (e.key === 'Escape') closeOffice();
	if (e.ctrlKey && e.key === '+') {
		e.preventDefault();
		zoomIn();
	}
	if (e.ctrlKey && e.key === '-') {
		e.preventDefault();
		zoomOut();
	}
	if (e.ctrlKey && e.key === '0') {
		e.preventDefault();
		resetZoom();
	}
};

// 滚轮缩放
const handleWheel = (e: WheelEvent) => {
	if (e.ctrlKey) {
		e.preventDefault();
		const delta = e.deltaY > 0 ? -0.1 : 0.1;
		scale.value = Math.max(0.5, Math.min(2, scale.value + delta));
	}
};

onMounted(() => {
	window.addEventListener('keydown', handleKeydown);
	window.addEventListener('wheel', handleWheel, { passive: false });
});

onBeforeUnmount(() => {
	if (previewSrc.value && previewSrc.value.startsWith('blob:')) {
		URL.revokeObjectURL(previewSrc.value);
	}
	window.removeEventListener('keydown', handleKeydown);
	window.removeEventListener('wheel', handleWheel);
});

const rendered = () => {
	console.log('渲染完成');
	emit('renderedOffice');
};

const closeOffice = () => {
	emit('closeOffice');
};
</script>

<style lang="scss" scoped>
.office_block {
	@apply fixed inset-0 z-50 flex items-center justify-center;
	background-color: rgba(0, 0, 0, 0.3);
}

.office_block-item {
	width: 90vw;
	height: 90vh;
	background-color: #f5f5f5;
	display: flex;
	flex-direction: column;
	overflow: hidden;
	@apply rounded-xl;
}

.preview-toolbar {
	@apply flex justify-between items-center p-2;
	background: rgba(255, 255, 255, 0.9);
	border-bottom: 1px solid #eee;
	z-index: 1;
}

.preview-content {
	flex: 1;
	overflow: auto;
	transition: transform 0.3s ease;
	transform-origin: center top;
	background: #f5f5f5;
	padding: 0 40px;
	display: flex;
	justify-content: center;

	/* 设置滚动条样式 */
	&::-webkit-scrollbar {
		width: 6px;
		height: 6px;
	}

	&::-webkit-scrollbar-track {
		background: #f1f1f1;
	}

	&::-webkit-scrollbar-thumb {
		background: #ccc;
		@apply rounded-xl;
		&:hover {
			background: #999;
		}
	}
}

.office_block-office {
	width: 100%;
	height: 100%;
}

:deep(.el-icon) {
	cursor: pointer;
}

/* 文档样式优化 */
:deep(.docx-wrapper) {
	width: 816px; /* A4纸张宽度 */
	min-height: 1056px; /* A4纸张高度 */
	margin: 0 auto;
	padding: 0;
	background: white;
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
	box-sizing: border-box;

	/* 文本渲染优化 */
	text-rendering: optimizeLegibility;
	-webkit-font-smoothing: antialiased;

	section {
		width: 100% !important;
		padding: 81pt 30pt 72pt 30pt !important;
		position: relative !important;
		overflow: hidden !important;
		box-sizing: border-box !important;

		/* 基础样式 */
		* {
			box-sizing: border-box !important;
			line-height: 1.6;
			margin: 0;
			padding: 0;
			max-width: 100% !important;
			overflow-wrap: break-word !important;
		}

		/* 容器元素溢出控制 */
		div,
		p,
		span {
			max-width: 100% !important;
			overflow: hidden !important;
			word-wrap: break-word !important;
			white-space: normal !important;
			margin-left: 0 !important;
			margin-right: 0 !important;
		}

		/* 表格溢出控制 */
		table {
			width: auto !important;
			max-width: 100% !important;
			table-layout: fixed !important;
			overflow-x: auto !important;
			display: table !important;
			margin: 0 auto !important;
			border-collapse: collapse !important;
			border: 1px solid #000 !important;

			td,
			th {
				max-width: none !important; /* 允许单元格内容换行 */
				word-break: break-word !important;
				overflow: hidden !important;
				white-space: normal !important;
				border: 1px solid #000 !important;
				padding: 8px !important;
			}
		}

		/* 图片溢出控制 */
		img,
		svg {
			max-width: 100% !important;
			height: auto !important;
			display: block !important;
		}

		/* 文本对齐控制 */
		[style*='text-align: center'] {
			text-align: center !important;
			display: block !important;
			width: 100% !important;
			overflow: hidden !important;

			> * {
				margin-left: 0 !important;
				margin-right: 0 !important;
				text-align: center !important;
				max-width: 100% !important;
			}

			> table {
				display: table !important;
				margin-left: 0 !important;
				margin-right: 0 !important;
				width: auto !important;
				max-width: 100% !important;
			}

			td,
			th {
				text-align: center !important;
			}
		}

		[style*='text-align: right'] {
			text-align: right !important;
			display: block !important;
			width: 100% !important;
			overflow: hidden !important;

			td,
			th {
				text-align: right !important;
			}
		}

		[style*='text-align: left'] {
			text-align: left !important;
			display: block !important;
			width: 100% !important;
			overflow: hidden !important;

			td,
			th {
				text-align: left !important;
			}
		}

		/* 列表溢出控制 */
		ul,
		ol {
			padding-left: 24px !important;
			max-width: calc(100% - 24px) !important;
			overflow: hidden !important;

			li {
				max-width: 100% !important;
				overflow: hidden !important;
				word-wrap: break-word !important;
			}
		}

		/* 标题溢出控制 */
		h1,
		h2,
		h3,
		h4,
		h5,
		h6 {
			max-width: 100% !important;
			overflow: hidden !important;
			word-wrap: break-word !important;
			white-space: normal !important;
		}

		/* 特殊内容处理 */
		pre,
		code {
			max-width: 100% !important;
			overflow-x: auto !important;
			white-space: pre-wrap !important;
		}
	}
}

/* 暗色模式适配 */
.dark {
	.preview-toolbar {
		background: rgba(0, 0, 0, 0.8);
		border-color: #333;
	}

	:deep(.docx-wrapper) {
		background: #1a1a1a;
		color: #e5e5e5;
		box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);

		table {
			td,
			th {
				border-color: #4a4a4a !important;
			}
		}
	}
}

/* 响应式布局 */
@media (max-width: 768px) {
	.office_block-item {
		width: 100vw;
		height: 100vh;
		@apply rounded-xl;
	}

	:deep(.docx-wrapper) {
		padding: 48px;
	}
}

/* 打印样式优化 */
@media print {
	.preview-toolbar {
		display: none;
	}

	.preview-content {
		transform: none !important;
	}

	:deep(.docx-wrapper) {
		box-shadow: none;
		padding: 0;
	}
}
</style>
