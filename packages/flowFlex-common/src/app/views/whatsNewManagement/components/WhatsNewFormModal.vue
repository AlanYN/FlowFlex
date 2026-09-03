<template>
	<el-dialog
		:model-value="true"
		:title="mode === 'create' ? 'New Update' : 'Edit Update'"
		width="600px"
		:close-on-click-modal="false"
		@close="handleCancel"
	>
		<el-form
			ref="formRef"
			:model="form"
			:rules="rules"
			label-position="top"
			class="whats-new-form"
		>
			<!-- Title -->
			<el-form-item label="Title" prop="title">
				<el-input
					v-model="form.title"
					placeholder="Enter title"
					:maxlength="100"
					show-word-limit
					clearable
				/>
			</el-form-item>

			<!-- Category -->
			<el-form-item label="Category" prop="category">
				<el-select v-model="form.category" placeholder="Select category" class="w-full">
					<el-option
						v-for="opt in CATEGORY_OPTIONS"
						:key="opt.value"
						:label="opt.label"
						:value="opt.value"
					/>
				</el-select>
			</el-form-item>

			<!-- Summary -->
			<el-form-item label="Summary" prop="summary">
				<el-input
					v-model="form.summary"
					type="textarea"
					placeholder="Enter a short summary"
					:maxlength="200"
					show-word-limit
					:autosize="{ minRows: 2, maxRows: 4 }"
				/>
			</el-form-item>

			<!-- Content (RichTextEditor) -->
			<el-form-item label="Content" prop="content">
				<div class="w-full">
					<div v-if="detailLoading" class="content-loading">
						<el-icon class="is-loading mr-1"><Loading /></el-icon>
						Loading content...
					</div>
					<RichTextEditor
						v-else
						ref="richTextEditorRef"
						v-model="form.content"
						placeholder="Write the update content..."
						min-height="220px"
						max-height="360px"
						@change="handleContentChange"
					/>
					<div v-if="contentError" class="content-error-msg">Content is required</div>
				</div>
			</el-form-item>

			<!-- Publishing Options -->
			<el-form-item label="Publishing">
				<div class="publishing-options">
					<!-- Publish Now -->
					<div
						class="publish-option"
						:class="{ 'is-selected': form.publishingMode === 'publish' }"
						@click="form.publishingMode = 'publish'"
					>
						<span class="option-radio">
							<span
								v-if="form.publishingMode === 'publish'"
								class="option-radio-dot"
							></span>
						</span>
						<span class="option-label">Publish Now</span>
					</div>

					<!-- Schedule (disabled) -->
					<el-tooltip content="Coming soon" placement="top" :show-after="200">
						<div class="publish-option is-disabled">
							<span class="option-radio"></span>
							<span class="option-label">Schedule</span>
						</div>
					</el-tooltip>

					<!-- Save as Draft -->
					<div
						class="publish-option"
						:class="{ 'is-selected': form.publishingMode === 'draft' }"
						@click="form.publishingMode = 'draft'"
					>
						<span class="option-radio">
							<span
								v-if="form.publishingMode === 'draft'"
								class="option-radio-dot"
							></span>
						</span>
						<span class="option-label">Save as draft</span>
					</div>
				</div>
			</el-form-item>
		</el-form>

		<template #footer>
			<div class="flex justify-end gap-2">
				<el-button :disabled="submitting" @click="handleCancel">Cancel</el-button>
				<el-button type="primary" :loading="submitting" @click="handleSubmit">
					{{ submitButtonLabel }}
				</el-button>
			</div>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, nextTick } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import { ElMessage } from 'element-plus';
import { Loading } from '@element-plus/icons-vue';
import RichTextEditor from '@/components/RichTextEditor/index.vue';
import { createWhatsNew, updateWhatsNew, getDetail } from '@/apis/whatsNew';
import type { WhatsNewAdminItem } from '#/whatsNew';

// ========================= Props & Emits =========================

interface Props {
	mode: 'create' | 'edit';
	item?: WhatsNewAdminItem | null;
}

const props = withDefaults(defineProps<Props>(), {
	item: null,
});

const emit = defineEmits<{
	success: [];
	close: [];
}>();

// ========================= Constants =========================

const CATEGORY_OPTIONS = [
	{ value: 'NewFeature', label: 'New Feature' },
	{ value: 'Improvement', label: 'Improvement' },
	{ value: 'BugFix', label: 'Bug Fix' },
	{ value: 'Announcement', label: 'Announcement' },
] as const;

// ========================= Form State =========================

const formRef = ref<FormInstance | null>(null);
const richTextEditorRef = ref<InstanceType<typeof RichTextEditor> | null>(null);
const submitting = ref<boolean>(false);
const contentError = ref<boolean>(false);
const detailLoading = ref<boolean>(false);

const form = reactive({
	title: '',
	summary: '',
	content: '',
	category: '',
	publishingMode: 'draft' as 'publish' | 'schedule' | 'draft',
});

// ========================= Validation Rules =========================

const rules: FormRules = {
	title: [
		{ required: true, message: 'Title is required', trigger: 'blur' },
		{ max: 100, message: 'Title must be 100 characters or fewer', trigger: 'blur' },
	],
	category: [{ required: true, message: 'Category is required', trigger: 'change' }],
	summary: [
		{ required: true, message: 'Summary is required', trigger: 'blur' },
		{ max: 200, message: 'Summary must be 200 characters or fewer', trigger: 'blur' },
	],
};

// ========================= Computed =========================

const submitButtonLabel = computed(() =>
	form.publishingMode === 'publish' ? 'Publish update' : 'Save as draft'
);

// ========================= Lifecycle =========================

onMounted(async () => {
	if (props.mode === 'edit' && props.item) {
		// 先用列表数据填充非内容字段，让用户感知到弹窗已打开
		form.title = props.item.title ?? '';
		form.summary = props.item.summary ?? '';
		form.category = props.item.category ?? '';
		form.publishingMode = props.item.status === 1 ? 'publish' : 'draft';

		// 再调详情接口拿 content
		detailLoading.value = true;
		try {
			const detail = await getDetail(props.item.id);
			form.content = detail.content ?? '';
			// 等编辑器挂载完再注入内容
			nextTick(() => {
				setTimeout(() => {
					richTextEditorRef.value?.setContent(form.content);
				}, 100);
			});
		} catch {
			ElMessage.error('Failed to load content. Please try again.');
		} finally {
			detailLoading.value = false;
		}
	}
});

// ========================= Handlers =========================

const handleContentChange = (value: string) => {
	form.content = value;
	// Clear content error once user starts typing
	if (contentError.value && !isContentEmpty(value)) {
		contentError.value = false;
	}
};

const isContentEmpty = (html: string): boolean => {
	if (!html) return true;
	const trimmed = html.trim();
	// Quill empty states
	return trimmed === '' || trimmed === '<p><br></p>' || trimmed === '<p></p>';
};

const handleSubmit = async () => {
	if (submitting.value) return;

	// Validate ElForm fields
	let formValid = false;
	try {
		formValid =
			(await formRef.value
				?.validate()
				.then(() => true)
				.catch(() => false)) ?? false;
	} catch {
		formValid = false;
	}

	// Get current rich text content
	const currentContent = richTextEditorRef.value?.getContent() ?? form.content;
	const hasContent = !isContentEmpty(currentContent);
	contentError.value = !hasContent;

	if (!formValid || !hasContent) return;

	submitting.value = true;
	try {
		const status = form.publishingMode === 'publish' ? 1 : 0;
		const payload = {
			title: form.title.trim(),
			summary: form.summary.trim(),
			content: currentContent,
			category: form.category,
			status: status as 0 | 1,
		};

		if (props.mode === 'create') {
			await createWhatsNew(payload);
		} else {
			await updateWhatsNew(props.item!.id, payload);
		}

		emit('success');
	} catch (err: any) {
		const msg =
			err?.response?.data?.message ||
			err?.message ||
			'Failed to save update. Please try again.';
		ElMessage.error(msg);
	} finally {
		submitting.value = false;
	}
};

const handleCancel = () => {
	if (submitting.value) return;
	emit('close');
};
</script>

<style scoped lang="scss">
/* Form layout */
.whats-new-form {
	:deep(.el-form-item) {
		margin-bottom: 18px;

		.el-form-item__label {
			font-weight: 500;
			font-size: 13px;
			color: var(--el-text-color-primary);
			padding-bottom: 4px;
		}
	}

	:deep(.el-input__wrapper),
	:deep(.el-textarea__inner) {
		background: var(--el-bg-color);
	}

	:deep(.el-select) {
		width: 100%;
	}
}

/* Content error message */
.content-error-msg {
	font-size: 12px;
	color: var(--el-color-danger);
	margin-top: 4px;
	line-height: 1.4;
}

/* Content loading placeholder */
.content-loading {
	display: flex;
	align-items: center;
	justify-content: center;
	height: 220px;
	font-size: 13px;
	color: var(--el-text-color-secondary);
	border: 1px solid var(--el-border-color);
	border-radius: var(--el-border-radius-base);
	background: var(--el-fill-color-lighter);
}

/* Publishing options */
.publishing-options {
	display: flex;
	gap: 12px;
	flex-wrap: wrap;
}

.publish-option {
	display: flex;
	align-items: center;
	gap: 8px;
	padding: 8px 14px;
	border-radius: 8px;
	border: 1.5px solid var(--el-border-color);
	cursor: pointer;
	user-select: none;
	transition:
		border-color 0.15s ease,
		background-color 0.15s ease;
	background: var(--el-bg-color);

	&:hover:not(.is-disabled) {
		border-color: var(--el-color-primary-light-5);
		background: var(--el-color-primary-light-9);
	}

	&.is-selected {
		border-color: var(--el-color-primary);
		background: var(--el-color-primary-light-9);

		.option-label {
			color: var(--el-color-primary);
			font-weight: 500;
		}
	}

	&.is-disabled {
		cursor: not-allowed;
		opacity: 0.45;
		border-color: var(--el-border-color-lighter);
		background: var(--el-fill-color-lighter);
	}
}

.option-radio {
	display: inline-flex;
	align-items: center;
	justify-content: center;
	width: 16px;
	height: 16px;
	border-radius: 50%;
	border: 1.5px solid var(--el-border-color);
	flex-shrink: 0;
	background: var(--el-bg-color);
	transition: border-color 0.15s ease;

	.publish-option.is-selected & {
		border-color: var(--el-color-primary);
	}
}

.option-radio-dot {
	width: 8px;
	height: 8px;
	border-radius: 50%;
	background: var(--el-color-primary);
}

.option-label {
	font-size: 13px;
	color: var(--el-text-color-regular);
	white-space: nowrap;
}
</style>
