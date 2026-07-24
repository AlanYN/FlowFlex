<template>
	<div class="mention-wrapper">
		<el-mention
			ref="selectRef"
			v-model="formattedValue"
			:options="assignOptions"
			:loading="optionsLoading"
			placeholder="Please input"
			@search="handleSearch"
			@select="handleSelect"
			@blur="handleBlur"
			type="textarea"
			whole
			show-arrow
			:maxlength="textraTwoHundredLength"
			show-word-limit
			:rows="inputTextraAutosize.minRows"
			:autoSize="inputTextraAutosize"
		>
			<template #label="{ item }">
				<div class="mention-option">
					<el-icon v-if="item.isExternal" class="mention-option__icon">
						<Message />
					</el-icon>
					<span class="mention-option__value">{{ item.label ?? item.value }}</span>
					<el-tag
						v-if="item.isExternal"
						size="small"
						type="info"
						class="mention-option__tag"
					>
						External
					</el-tag>
				</div>
			</template>
			<template #loading>
				<svg class="circular" viewBox="0 0 50 50">
					<circle class="path" cx="25" cy="25" r="20" fill="none" />
				</svg>
			</template>
			<template #footer>
				<div class="mention-footer" @click="handleOpenExternalEmail">
					<el-icon class="mention-footer__icon"><Message /></el-icon>
					<span>+ Add external email</span>
				</div>
			</template>
		</el-mention>
	</div>

	<!-- External email input dialog -->
	<el-dialog
		v-model="externalEmailDialogVisible"
		title="Add External Email"
		width="400px"
		:close-on-click-modal="false"
		append-to-body
		align-center
		:lock-scroll="true"
		@closed="handleDialogClosed"
	>
		<el-form @submit.prevent="handleConfirmExternalEmail">
			<el-form-item label="Email Address">
				<el-input
					ref="externalEmailInputRef"
					v-model="externalEmailInput"
					placeholder="Enter email address"
					@keyup.enter="handleConfirmExternalEmail"
				/>
			</el-form-item>
			<p v-if="externalEmailError" class="text-red-500 text-xs mt-1">
				{{ externalEmailError }}
			</p>
		</el-form>
		<template #footer>
			<el-button @click="externalEmailDialogVisible = false">Cancel</el-button>
			<el-button type="primary" @click="handleConfirmExternalEmail">Confirm</el-button>
		</template>
	</el-dialog>
</template>

<script setup lang="ts">
import { computed, ref, nextTick } from 'vue';
import { ElMention, ElIcon, ElTag } from 'element-plus';
import type { InputInstance } from 'element-plus';
import { Message } from '@element-plus/icons-vue';
import { useInternalNoteUsers } from '@/hooks/useInternalNoteUsers';
import { textraTwoHundredLength, inputTextraAutosize } from '@/settings/projectSetting';

const props = defineProps<{
	modelValue: string;
	id: string;
}>();

const { assignOptions, optionsLoading, remoteMethod, mentionUserMap } = useInternalNoteUsers(
	props.id
);

const emit = defineEmits(['update:modelValue', 'blur']);

const selectRef = ref<InstanceType<typeof ElMention>>();

// Track the current mention search state (text after @)
const currentSearchText = ref('');
// Store what needs to be cleaned from the display text when inserting external email
const pendingMentionPrefix = ref('');

// External email dialog
const externalEmailDialogVisible = ref(false);
const externalEmailInput = ref('');
const externalEmailError = ref('');
const externalEmailInputRef = ref<InputInstance>();

const handleSearch = (pattern: string) => {
	currentSearchText.value = pattern;
	remoteMethod(pattern);
};

// 使用计算属性处理双向绑定
const formattedValue = computed({
	// 从存储格式 {{mention:user:email:username:displayName}} / {{mention:email:address}} 转为显示格式 @displayName / @address
	get() {
		if (!props.modelValue) return '';
		return props.modelValue.replace(
			/\{\{mention:(user|email):([^}]+?)\}\}/g,
			(_, type, payload) => {
				if (type === 'user') {
					// payload = "email:username:displayName"
					const parts = payload.split(':');
					const displayName =
						parts.length > 2
							? parts.slice(2).join(':')
							: parts.length > 1
							? parts[1]
							: payload;
					return `@${displayName}`;
				} else {
					// email type: payload is the email address
					return `@${payload}`;
				}
			}
		);
	},
	// 从显示格式 @displayName 转为存储格式
	set(val: string) {
		if (!val) {
			emit('update:modelValue', '');
			return;
		}

		let storageValue = val;
		const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

		// Sort map entries by length (longest first) to avoid partial matches
		const entries = [...mentionUserMap.value.entries()].sort(
			(a, b) => b[0].length - a[0].length
		);

		// Replace known mentions (displayName or username found in map)
		for (const [name, info] of entries) {
			const mentionPattern = `@${name}`;
			if (storageValue.includes(mentionPattern)) {
				const storageFormat = `{{mention:user:${info.email}:${info.username}:${info.displayName}}}`;
				storageValue = storageValue.split(mentionPattern).join(storageFormat);
			}
		}

		// Handle remaining @patterns that look like emails
		storageValue = storageValue.replace(/@([^\s@]+@[^\s@]+\.[^\s@]+)/g, (match, email) => {
			if (emailRegex.test(email)) {
				return `{{mention:email:${email}}}`;
			}
			return match;
		});

		emit('update:modelValue', storageValue);
	},
});

// 处理用户选择
const handleSelect = (option: any) => {
	console.log('Selected option:', option);
};

const handleOpenExternalEmail = () => {
	// Capture what el-mention has inserted: "@" + whatever the user typed as search
	pendingMentionPrefix.value = `@${currentSearchText.value}`;

	externalEmailInput.value = '';
	externalEmailError.value = '';
	// Small delay to let the mention dropdown fully close before opening dialog
	// This prevents layout shifts from the dropdown closing while dialog is positioning
	setTimeout(() => {
		externalEmailDialogVisible.value = true;
		nextTick(() => {
			externalEmailInputRef.value?.focus();
		});
	}, 150);
};

const handleConfirmExternalEmail = () => {
	const email = externalEmailInput.value.trim();
	const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

	if (!email) {
		externalEmailError.value = 'Please enter an email address';
		return;
	}
	if (!emailRegex.test(email)) {
		externalEmailError.value = 'Please enter a valid email address';
		return;
	}

	// Get the current storage value and replace the residual @searchText
	let currentStorage = props.modelValue || '';

	// The residual in modelValue is the literal text that was typed: "@searchText" or just "@"
	// It's at the position where the user was typing, likely near the end
	if (pendingMentionPrefix.value) {
		const lastIndex = currentStorage.lastIndexOf(pendingMentionPrefix.value);
		if (lastIndex !== -1) {
			currentStorage =
				currentStorage.substring(0, lastIndex) +
				currentStorage.substring(lastIndex + pendingMentionPrefix.value.length);
		}
	}

	const mentionTag = `{{mention:email:${email}}}`;
	// Trim trailing whitespace before inserting, then add the tag with a space after
	currentStorage = currentStorage.trimEnd();
	const newValue = currentStorage ? `${currentStorage} ${mentionTag} ` : `${mentionTag} `;
	emit('update:modelValue', newValue);

	externalEmailDialogVisible.value = false;
	externalEmailInput.value = '';
	externalEmailError.value = '';
	pendingMentionPrefix.value = '';
};

const handleDialogClosed = () => {
	externalEmailInput.value = '';
	externalEmailError.value = '';
	pendingMentionPrefix.value = '';
};

const handleBlur = () => {
	emit('blur');
};

defineExpose({
	focus: () => {
		selectRef.value?.input?.focus();
	},
});
</script>

<style scoped lang="scss">
.mention-wrapper {
	width: 100%;
}

.mention-footer {
	display: flex;
	align-items: center;
	gap: 6px;
	padding: 8px 12px;
	font-size: 13px;
	color: var(--el-color-primary);
	cursor: pointer;
	border-top: 1px solid var(--el-border-color-lighter);
	transition: background-color 0.2s;

	&:hover {
		background-color: var(--el-fill-color-light);
	}

	&__icon {
		font-size: 14px;
	}
}

.mention-option {
	display: flex;
	align-items: center;
	gap: 6px;
	width: 100%;

	&__icon {
		color: var(--el-color-info);
		flex-shrink: 0;
		font-size: 14px;
	}

	&__value {
		flex: 1;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	&__tag {
		flex-shrink: 0;
		margin-left: auto;
	}
}
</style>
