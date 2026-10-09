<template>
	<el-dialog
		v-model="visible"
		title="New Document"
		width="480px"
		:close-on-click-modal="false"
		@open="handleOpen"
		@closed="handleClosed"
	>
		<!-- Document name -->
		<div class="space-y-2 mb-5">
			<label class="text-sm font-medium">Document name</label>
			<el-input
				ref="nameInputRef"
				v-model="docName"
				:class="{ 'border-red-500': nameError }"
				placeholder="Enter document name"
				maxlength="200"
				data-testid="create-doc-dialog-input-name"
				@input="nameError = ''"
			/>
			<p v-if="nameError" class="text-xs text-[#f56c6c] mt-1">{{ nameError }}</p>
		</div>

		<!-- Document type -->
		<div class="space-y-2">
			<label class="text-sm font-medium">Document type</label>
			<div class="grid grid-cols-2 gap-3 mt-1">
				<div
					v-for="type in TYPE_OPTIONS"
					:key="type.value"
					class="flex flex-col items-center gap-2 p-3 rounded-xl border-2 cursor-pointer transition-all duration-150"
					:class="
						selectedType === type.value
							? 'border-primary bg-[#ecf5ff]'
							: 'border-[#e4e7ed] hover:border-[#c0c4cc]'
					"
					:data-testid="`create-doc-dialog-type-${type.value.toLowerCase()}`"
					@click="selectedType = type.value"
				>
					<span class="text-3xl leading-none">{{ type.icon }}</span>
					<span class="text-xs font-medium text-center leading-tight">
						{{ type.label }}
					</span>
				</div>
			</div>
		</div>

		<template #footer>
			<div class="flex justify-end gap-2">
				<el-button data-testid="create-doc-dialog-btn-cancel" @click="visible = false">
					Cancel
				</el-button>
				<el-button
					type="primary"
					:loading="creating"
					data-testid="create-doc-dialog-btn-confirm"
					@click="handleCreate"
				>
					Create
				</el-button>
			</div>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, nextTick } from 'vue';
import { ElDialog, ElInput, ElButton } from 'element-plus';
import { createSnapshot } from '@/apis/ow/documentSnapshot';

const props = defineProps<{ modelValue: boolean }>();
const emit = defineEmits<{
	'update:modelValue': [value: boolean];
	created: [unitId: string];
}>();

const visible = computed({
	get: () => props.modelValue,
	set: (v) => emit('update:modelValue', v),
});

const TYPE_OPTIONS = [
	{ value: 'Sheet' as const, icon: '📊', label: 'Spreadsheet' },
	{ value: 'Doc' as const, icon: '📄', label: 'Document' },
];

const nameInputRef = ref();
const docName = ref('');
const nameError = ref('');
const selectedType = ref<'Sheet' | 'Doc'>('Sheet');
const creating = ref(false);

function handleOpen() {
	docName.value = '';
	nameError.value = '';
	selectedType.value = 'Sheet';
	nextTick(() => nameInputRef.value?.focus());
}

function handleClosed() {
	docName.value = '';
	nameError.value = '';
	creating.value = false;
}

async function handleCreate() {
	const name = docName.value.trim();
	if (!name) {
		nameError.value = 'Please enter a document name';
		nameInputRef.value?.focus();
		return;
	}

	creating.value = true;
	try {
		// For Doc type, provide a minimal valid snapshot so the render engine
		// doesn't crash with an empty body on first open.
		const initialDataJson =
			selectedType.value === 'Doc'
				? JSON.stringify({
						body: {
							dataStream: '\r\n',
							paragraphs: [{ startIndex: 0 }],
							sectionBreaks: [{ startIndex: 1 }],
						},
				  })
				: '{}';
		const res = (await createSnapshot({
			title: name,
			docType: selectedType.value,
			dataJson: initialDataJson,
		})) as { code: string; success: boolean; data: string | null };
		const unitId = res?.data ?? '';
		if (!unitId) {
			nameError.value = 'Failed to create document. Please try again.';
			return;
		}
		visible.value = false;
		emit('created', unitId);
	} catch {
		nameError.value = 'Failed to create document. Please try again.';
	} finally {
		creating.value = false;
	}
}
</script>
