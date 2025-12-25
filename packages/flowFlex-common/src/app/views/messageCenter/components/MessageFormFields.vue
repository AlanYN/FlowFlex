<template>
	<div>
		<el-form-item label="Subject">
			<el-input v-model="subject" placeholder="Enter message subject" />
		</el-form-item>

		<el-form-item label="Related To">
			<el-select
				v-model="relatedTo"
				placeholder="Select related lead (optional)"
				class="w-full"
				filterable
				clearable
			/>
		</el-form-item>

		<el-form-item label="Message" class="w-full">
			<RichTextEditor
				ref="richTextEditorRef"
				placeholder="Type your message here..."
				min-height="200px"
				max-height="300px"
				@change="handleEditorChange"
			/>
		</el-form-item>

		<el-form-item label="Attachments">
			<el-upload
				ref="uploadRef"
				class="w-full"
				drag
				:auto-upload="false"
				:on-change="handleFileChange"
				:show-file-list="false"
				multiple
				accept=".pdf,.docx,.doc,.jpg,.jpeg,.png,.xlsx,.xls,.msg,.eml"
			>
				<div class="flex flex-col items-center justify-center py-6">
					<el-icon class="text-4xl text-gray-400 mb-2">
						<Upload />
					</el-icon>
					<div class="text-sm text-gray-600 dark:text-gray-300">
						Click to upload or drag and drop files
					</div>
				</div>
			</el-upload>

			<!-- 上传进度 -->
			<div v-if="uploadProgress.length > 0" class="space-y-2 w-full">
				<h4 class="text-sm font-medium text-gray-700 dark:text-gray-300">Uploading...</h4>
				<div
					v-for="progress in uploadProgress"
					:key="progress.uid"
					class="flex items-center space-x-3 p-2 bg-gray-50 dark:bg-black-200 rounded"
				>
					<el-icon class="text-primary-500">
						<Upload />
					</el-icon>
					<div class="flex-1">
						<div class="text-sm font-medium">{{ progress.name }}</div>
						<el-progress
							:percentage="progress.percentage"
							:status="progress.error ? 'exception' : undefined"
							:show-text="false"
						/>
					</div>
					<span class="text-xs text-gray-500">{{ progress.percentage }}%</span>
					<el-tooltip
						v-if="progress.error"
						:content="progress.error"
						placement="top"
						effect="dark"
					>
						<el-icon class="text-red-500 cursor-pointer text-lg">
							<WarningFilled />
						</el-icon>
					</el-tooltip>
				</div>
			</div>

			<!-- 已上传文件列表 -->
			<div v-if="uploadedAttachments.length > 0" class="space-y-2 mt-4 w-full">
				<h4 class="text-sm font-medium text-gray-700 dark:text-gray-300">Uploaded Files</h4>
				<div
					v-for="attachment in uploadedAttachments"
					:key="attachment.id"
					class="flex items-center justify-between p-2 bg-gray-50 dark:bg-black-200 rounded"
				>
					<div class="flex items-center space-x-2">
						<Icon icon="lucide-file" class="w-4 h-4 text-primary-500" />
						<span class="text-sm">{{ attachment.fileName }}</span>
					</div>
					<el-button
						type="danger"
						link
						size="small"
						@click="$emit('remove-file', attachment.id)"
					>
						<Icon icon="lucide-x" class="w-4 h-4" />
					</el-button>
				</div>
			</div>
		</el-form-item>
	</div>
</template>

<script lang="ts" setup>
import { ref, onMounted } from 'vue';
import { Upload } from '@element-plus/icons-vue';
import RichTextEditor from '@/components/RichTextEditor/index.vue';
import type { UploadFile, UploadInstance } from 'element-plus';

interface Props {
	uploadProgress: Array<{
		uid: string;
		name: string;
		percentage: number;
		error?: string;
	}>;
	uploadedAttachments: Array<{
		id: string;
		fileName: string;
		fileSize: number;
		contentType: string;
	}>;
}

defineProps<Props>();

const emit = defineEmits<{
	'file-change': [file: UploadFile];
	'remove-file': [fileId: string];
}>();

const subject = defineModel<string>('subject', { required: true });
const body = defineModel<string>('body', { required: true });
const relatedTo = defineModel<string>('relatedTo', { required: true });

// RichTextEditor ref
const richTextEditorRef = ref<InstanceType<typeof RichTextEditor> | null>(null);
const uploadRef = ref<UploadInstance>();

// 处理编辑器内容变化（用户输入）
const handleEditorChange = (html: string) => {
	body.value = html;
};

// 组件挂载后初始化内容
onMounted(() => {
	// 延迟执行，确保编辑器已准备好
	if (body.value) {
		setTimeout(() => {
			richTextEditorRef.value?.setContent(body.value);
		}, 100);
	}
});

const handleFileChange = (file: UploadFile) => {
	emit('file-change', file);
	// 每次选择文件后清空 el-upload 内部的文件列表
	// 这样上传失败的文件不会计入 limit 数量
	uploadRef.value?.clearFiles();
};
</script>
