<script setup lang="ts">
interface Props {
	/** `edit`: signing in-progress mode; `signed`: read-only post-signing mode */
	mode: 'edit' | 'signed';
	/** Whether at least one element (signature or date) has been placed on any page */
	hasElements: boolean;
	/** True while the PDF synthesis + upload request is in-flight */
	isSaving: boolean;
	/** Name of the document being signed */
	fileName: string;
	/** Whether the Add Date button should be disabled (date limit reached) */
	addDateDisabled?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	mode: 'edit',
	hasElements: false,
	isSaving: false,
	fileName: '',
	addDateDisabled: false,
});

const emit = defineEmits<{
	/** Fired when user clicks "Add Signature" (edit mode only) */
	addSignature: [];
	/** Fired when user clicks "Add Date" (edit mode only) */
	addDate: [];
	/** Fired when user clicks "Confirm Signature" (edit mode, enabled only when hasElements) */
	confirmSignature: [];
	/** Fired when user clicks "×" close button (both modes) */
	close: [];
	/** Fired when user clicks "Download" (signed mode only) */
	download: [];
	/** Fired when user clicks "Print" (signed mode only) */
	print: [];
}>();
</script>

<template>
	<div class="signing-toolbar">
		<!-- Left: file name + optional Signed badge -->
		<div class="signing-toolbar__file-name">
			<span class="file-name-text truncate" :title="props.fileName">
				{{ props.fileName }}
			</span>
			<span v-if="props.mode === 'signed'" class="signed-badge">Signed</span>
		</div>

		<!-- Right: action buttons -->
		<div class="signing-toolbar__actions">
			<!-- ── Edit mode buttons ── -->
			<template v-if="props.mode === 'edit'">
				<el-button size="small" :disabled="props.isSaving" @click="emit('addSignature')">
					Add Signature
				</el-button>

				<el-button
					size="small"
					:disabled="props.isSaving || props.addDateDisabled"
					@click="emit('addDate')"
				>
					Add Date
				</el-button>

				<el-button
					type="primary"
					size="small"
					:disabled="!props.hasElements || props.isSaving"
					:loading="props.isSaving"
					@click="emit('confirmSignature')"
				>
					Confirm Signature
				</el-button>
			</template>

			<!-- ── Signed (read-only) mode buttons ── -->
			<template v-else-if="props.mode === 'signed'">
				<el-button size="small" @click="emit('download')">Download</el-button>

				<el-button size="small" @click="emit('print')">Print</el-button>
			</template>

			<!-- ── Close button — always present ── -->
			<el-button
				size="small"
				class="close-btn"
				:disabled="props.isSaving"
				@click="emit('close')"
			>
				×
			</el-button>
		</div>
	</div>
</template>

<style scoped lang="scss">
.signing-toolbar {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 0 16px;
	height: 48px;
	background: #1e1e1e;
	border-bottom: 1px solid #333333;
	flex-shrink: 0;
	gap: 12px;
}

.signing-toolbar__file-name {
	display: flex;
	align-items: center;
	gap: 8px;
	min-width: 0;
	flex: 1;
}

.file-name-text {
	font-size: 13px;
	color: #e5e7eb;
	max-width: 320px;
	white-space: nowrap;
	overflow: hidden;
	text-overflow: ellipsis;
}

.signed-badge {
	display: inline-flex;
	align-items: center;
	padding: 2px 8px;
	border-radius: 10px;
	font-size: 11px;
	font-weight: 600;
	letter-spacing: 0.02em;
	background-color: #dcfce7;
	color: #15803d;
	white-space: nowrap;
	flex-shrink: 0;
}

.signing-toolbar__actions {
	display: flex;
	align-items: center;
	gap: 8px;
	flex-shrink: 0;
}

.close-btn {
	// Slightly larger click area for the close button; the × glyph is small
	font-size: 16px;
	line-height: 1;
	padding: 0 10px;
}
</style>
