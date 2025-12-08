<template>
	<Teleport to="body">
		<Transition name="fade">
			<div v-if="props.visible" class="tenant-error-overlay" @click.self="handleCancel">
				<div class="dialog-container">
					<!-- 关闭按钮 -->
					<el-button
						class="close-btn"
						text
						circle
						@click="handleCancel"
						aria-label="Close"
					>
						<el-icon><Close /></el-icon>
					</el-button>

					<!-- 图标 -->
					<div class="icon">⚠️</div>

					<!-- 标题 -->
					<h2 class="title">Switch Failed</h2>

					<!-- 错误消息 -->
					<div class="message">{{ displayMessage }}</div>

					<!-- 操作按钮 -->
					<div class="actions">
						<el-button @click="handleCancel">Back to Home</el-button>
						<el-button type="primary" @click="handleConfirm">Relogin</el-button>
					</div>
				</div>
			</div>
		</Transition>
	</Teleport>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { Close } from '@element-plus/icons-vue';
import { ElIcon, ElButton } from 'element-plus';

interface Props {
	visible: boolean;
	errorCode?: string | number;
	errorMessage?: string;
}

interface Emits {
	(e: 'confirm'): void;
	(e: 'cancel'): void;
}

// 错误码到错误类型的映射
const getErrorType = (code: string | number | undefined): 'noAccess' | 'noRole' | 'general' => {
	if (!code) return 'general';
	const codeStr = String(code);
	if (codeStr === '4032') return 'noAccess';
	if (codeStr === '4033') return 'noRole';
	return 'general';
};

// 错误类型对应的默认消息
const ERROR_MESSAGES: Record<string, string> = {
	noAccess: 'No permission to access this tenant. Please contact your administrator.',
	noRole: "You can access this tenant, but you don't have any roles assigned. Please contact your administrator to assign appropriate roles.",
	general: 'Unable to switch to the selected company.',
};

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

// 根据错误码确定错误类型
const errorType = computed(() => getErrorType(props.errorCode));

// 计算最终显示的错误消息
const displayMessage = computed(() => {
	return props.errorMessage || ERROR_MESSAGES[errorType.value] || ERROR_MESSAGES.general;
});

const handleConfirm = () => {
	emit('confirm');
};

const handleCancel = () => {
	emit('cancel');
};
</script>

<style scoped lang="scss">
.tenant-error-overlay {
	position: fixed;
	top: 0;
	left: 0;
	right: 0;
	bottom: 0;
	z-index: 9999;
	display: flex;
	align-items: center;
	justify-content: center;
	padding: 20px;
	background: rgba(0, 0, 0, 0.5);
	backdrop-filter: blur(4px);
}

.dialog-container {
	position: relative;
	max-width: 480px;
	width: 100%;
	background: var(--el-bg-color-page);
	border-radius: var(--el-border-radius-base);
	padding: 40px;
	box-shadow: var(--el-box-shadow-light);
	border: 1px solid var(--el-border-color);
	text-align: center;
}

.close-btn {
	position: absolute;
	top: 16px;
	right: 16px;
}

.icon {
	font-size: 48px;
	margin-bottom: 16px;
	opacity: 0.9;
}

.title {
	font-size: var(--subtitle-1-size);
	font-weight: var(--heading-4-weight);
	color: var(--el-text-color-primary);
	margin-bottom: 12px;
}

.message {
	font-size: var(--text-base-size);
	color: var(--el-text-color-regular);
	line-height: 1.6;
	margin-bottom: 32px;
	white-space: pre-line;
}

.actions {
	display: flex;
	gap: 12px;
	justify-content: center;
	flex-wrap: wrap;
}

.fade-enter-active,
.fade-leave-active {
	transition: opacity 0.3s ease;
}

.fade-enter-active .dialog-container,
.fade-leave-active .dialog-container {
	transition:
		transform 0.3s ease,
		opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
	opacity: 0;

	.dialog-container {
		transform: scale(0.95) translateY(-10px);
		opacity: 0;
	}
}

.fade-enter-to,
.fade-leave-from {
	opacity: 1;

	.dialog-container {
		transform: scale(1) translateY(0);
		opacity: 1;
	}
}

/* 响应式设计 */
@media (max-width: 480px) {
	.dialog-container {
		padding: 32px 24px;
		margin: 10px;
	}

	.actions {
		flex-direction: column;
	}
}
</style>
