<template>
	<Teleport to="body">
		<Transition name="fade">
			<div v-if="isActive" class="tenant-switching-overlay">
				<div class="container">
					<div class="icon">🏢</div>
					<h1 class="title">{{ t('sys.tenant.switching') }}</h1>
					<p class="subtitle">{{ t('sys.tenant.pleaseWait') }}</p>

					<div class="progress-wrapper">
						<!-- 租户标签 -->
						<div class="tenant-labels">
							<div class="tenant-from">
								<span class="tenant-label">{{ t('sys.tenant.fromLabel') }}:</span>
								<span class="tenant-name">{{ fromTenantName }}</span>
							</div>
							<div class="tenant-to">
								<span class="tenant-label">{{ t('sys.tenant.toLabel') }}:</span>
								<span class="tenant-name">{{ toTenantName }}</span>
							</div>
						</div>

						<!-- 进度条 -->
						<div class="progress-bar">
							<div class="progress-start">←</div>
							<div
								class="progress-fill"
								:class="{ 'progress-cancelling': isCancelling }"
								:style="{ width: `${progress}%` }"
							></div>
						</div>

						<div class="progress-text">{{ stepText }}</div>
					</div>

					<!-- Spinner和取消按钮（无错误时显示） -->
					<div v-if="!hasError">
						<div class="spinner"></div>
						<div class="action-buttons">
							<button v-if="!isCancelling" class="cancel-btn" @click="handleCancel">
								{{ t('sys.tenant.cancel') }}
							</button>
						</div>
					</div>

					<!-- 错误UI -->
					<div v-if="hasError" class="error-message">
						<div class="error-icon">⚠️</div>
						<div class="error-content">
							<strong>{{ t('sys.tenant.error.title') }}</strong>
							<div class="error-details">
								{{ switching.error || t('sys.tenant.error.general') }}
							</div>
							<div class="error-actions">
								<button class="retry-btn" @click="handleRetry">
									{{ t('sys.tenant.retry') }}
								</button>
								<button class="help-btn" @click="handleHelp">
									{{ switchBackText }}
								</button>
							</div>
						</div>
					</div>
				</div>
			</div>
		</Transition>
	</Teleport>
</template>

<script setup lang="ts">
import { computed, nextTick } from 'vue';
import { useUserStore } from '@/stores/modules/user';
import { useI18n } from '@/hooks/useI18n';
import { switchingCompany } from '@/apis/login/user';
import { ElMessage } from 'element-plus';
import { useWujie } from '@/hooks/wujie/micro-app.config';

const userStore = useUserStore();
const { t } = useI18n();

const switching = computed(() => userStore.getTenantSwitching);
const isActive = computed(() => switching.value.isActive);
const fromTenantName = computed(() => {
	const id = switching.value.fromTenantId;
	return (id && userStore.getUserInfo?.tenants?.[id]) || 'Unknown';
});
const toTenantName = computed(() => {
	const id = switching.value.toTenantId;
	return (id && userStore.getUserInfo?.tenants?.[id]) || 'Unknown';
});
const progress = computed(() => switching.value.progress);
const stepKey = computed(() => switching.value.currentStep);
const stepText = computed(() => {
	if (!stepKey.value) return '';
	return t(`sys.tenant.steps.${stepKey.value}`);
});
const hasError = computed(() => !!switching.value.error);
const isCancelling = computed(() => stepKey.value === 'cancelling');
const switchBackText = computed(() => {
	// 动态生成"切换回原租户"的文本，包含租户名称
	return `${t('sys.tenant.switchBack')} ${fromTenantName.value}`;
});

const handleCancel = async () => {
	try {
		// 获取原租户ID
		const fromTenantId = switching.value.fromTenantId;

		// 立即显示取消状态和视觉反馈
		userStore.setTenantCancelling();

		// 进度条倒退动画（从当前进度倒退到0）
		const currentProgress = switching.value.progress;
		const regressStep = async () => {
			for (let p = currentProgress; p >= 0; p -= 5) {
				userStore.updateProgress(p, 'cancelling');
				await new Promise((resolve) => setTimeout(resolve, 30));
			}
		};
		regressStep();

		if (fromTenantId) {
			// 调用API切换回原租户（因为后端已经保存了切换状态）
			const res = await switchingCompany(fromTenantId);

			if (res.code == '200' || res.code == 1) {
				// 切换成功，重置状态并重新加载页面
				userStore.resetTenantSwitching();
				window.location.reload();
			} else {
				// 切换失败，但仍然重置状态并刷新（不显示错误消息）
				console.error('Failed to switch back to original tenant:', res.msg);
				userStore.resetTenantSwitching();
				window.location.reload();
			}
		} else {
			// 没有原租户ID，直接重置并刷新
			userStore.resetTenantSwitching();
			window.location.reload();
		}
	} catch (error) {
		// 发生错误，但仍然刷新页面（不显示错误消息）
		console.error('Cancel switch error:', error);
		userStore.resetTenantSwitching();
		window.location.reload();
	}
};

const handleRetry = async () => {
	// 重新尝试切换到目标租户
	try {
		const fromTenantId = String(switching.value.fromTenantId || '');
		const toTenantId = String(switching.value.toTenantId || '');

		if (!toTenantId) {
			// 如果没有目标租户ID，直接刷新页面
			userStore.resetTenantSwitching();
			window.location.reload();
			return;
		}

		// 重置错误状态，重新开始切换
		userStore.startTenantSwitching(fromTenantId, toTenantId);

		// 等待DOM更新和动画开始
		await nextTick();
		await new Promise((resolve) => setTimeout(resolve, 150));

		// 模拟进度更新（4个阶段）
		const steps = [
			{ key: 'validating', progress: 25, duration: 800 },
			{ key: 'loading', progress: 50, duration: 1000 },
			{ key: 'configuring', progress: 75, duration: 800 },
			{ key: 'finalizing', progress: 95, duration: 600 },
		];

		let currentStepIndex = 0;
		const updateStep = () => {
			// 检查用户是否取消了切换
			if (
				!userStore.getTenantSwitching.isActive ||
				userStore.getTenantSwitching.currentStep === 'cancelling'
			) {
				clearInterval(progressInterval);
				return;
			}
			if (currentStepIndex < steps.length) {
				const step = steps[currentStepIndex];
				userStore.updateProgress(step.progress, step.key);
				currentStepIndex++;
			}
		};

		// 启动进度模拟
		const progressInterval = setInterval(updateStep, 800);
		updateStep(); // 立即执行第一步

		// 执行API调用
		const res = await switchingCompany(toTenantId);

		// 清除进度模拟
		clearInterval(progressInterval);

		// 检查用户是否在API调用期间取消了切换
		if (!userStore.getTenantSwitching.isActive) {
			return;
		}

		if (res.code == '200' || res.code == 1) {
			// 成功：更新到100%
			userStore.updateProgress(100, 'finalizing');
			await new Promise((resolve) => setTimeout(resolve, 300));

			// 执行登出和重新登录
			userStore.setSessionTimeout(true);
			const { tokenExpiredLogOut } = useWujie();
			if (tokenExpiredLogOut) {
				await tokenExpiredLogOut(true);
			}
			await userStore.afterLoginAction(true);

			// 刷新页面
			window.location.reload();
		} else {
			// 失败：根据错误码显示不同的错误消息
			let errorMessage = '';

			if (res.code === 4032) {
				errorMessage = t('sys.tenant.error.noAccess');
			} else if (res.code === 4033) {
				errorMessage = t('sys.tenant.error.noRole');
			} else {
				errorMessage = res.msg || t('sys.tenant.error.general');
			}

			userStore.setTenantError(errorMessage);
			ElMessage.error(errorMessage);
			return;
		}
	} catch (error) {
		// 异常：显示网络错误
		const errorMessage = t('sys.tenant.error.network');
		userStore.setTenantError(errorMessage);
		ElMessage.error(errorMessage);
		console.error('Tenant switch retry error:', error);
		return;
	}
};

const handleHelp = () => {
	// 切换失败后，直接关闭错误UI即可（不需要刷新页面，因为租户状态未改变）
	// 同时关闭所有ElMessage提示框
	ElMessage.closeAll();
	userStore.resetTenantSwitching();
};
</script>

<style scoped lang="scss">
// ✅ 使用项目规范的 Element Plus 语义变量
.tenant-switching-overlay {
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
	background: var(--el-bg-color); // 使用 Element Plus 背景色
	transition: background-color 0.3s ease;
}

.container {
	max-width: 400px;
	width: 100%;
	text-align: center;
	background: var(--el-bg-color-page); // 使用页面背景色
	border-radius: var(--el-border-radius-base); // 使用统一圆角
	padding: 32px;
	box-shadow: var(--el-box-shadow-light); // 使用统一阴影
	border: 1px solid var(--el-border-color); // 使用边框色
	transition:
		background-color 0.3s ease,
		border-color 0.3s ease,
		box-shadow 0.3s ease;
}

.icon {
	font-size: 48px;
	margin-bottom: 20px;
	opacity: 0.8;
}

.title {
	font-size: var(--heading-4-size); // 使用 Typography 变量
	font-weight: var(--heading-4-weight);
	margin-bottom: 8px;
	color: var(--el-text-color-primary); // 使用主要文本色
	transition: color 0.3s ease;
}

.subtitle {
	font-size: var(--text-base-size); // 使用 Typography 变量
	color: var(--el-text-color-regular); // 使用常规文本色
	margin-bottom: 24px;
	transition: color 0.3s ease;
}

.progress-wrapper {
	margin-bottom: 24px;
	position: relative;
}

.tenant-labels {
	display: flex;
	justify-content: space-between;
	margin-bottom: 8px;
}

.tenant-from {
	font-size: var(--text-sm-size); // 使用 Typography 变量
	font-weight: 500;
	text-align: left;
	transition: color 0.3s ease;
	display: flex;
	flex-direction: column;
	align-items: flex-start;
	gap: 4px;

	.tenant-label {
		font-size: var(--text-xs-size); // 使用 Typography 变量
		color: var(--el-text-color-secondary); // 使用次要文本色
		opacity: 0.8;
	}

	.tenant-name {
		color: var(--el-color-danger); // 使用 Element Plus 危险色（红色，表示离开）
		font-weight: 600;
	}
}

.tenant-to {
	font-size: var(--text-sm-size); // 使用 Typography 变量
	font-weight: 500;
	text-align: right;
	transition: color 0.3s ease;
	display: flex;
	flex-direction: column;
	align-items: flex-end;
	gap: 4px;

	.tenant-label {
		font-size: var(--text-xs-size); // 使用 Typography 变量
		color: var(--el-text-color-secondary); // 使用次要文本色
		opacity: 0.8;
	}

	.tenant-name {
		color: var(--primary-500); // 使用主题色（表示前往）
		font-weight: 600;
	}
}

.progress-bar {
	width: 100%;
	height: 8px;
	background: var(--el-fill-color); // 使用填充色
	border-radius: 4px;
	overflow: hidden;
	position: relative;
	transition: background-color 0.3s ease;
	margin-bottom: 8px;
}

.progress-fill {
	height: 100%;
	background: linear-gradient(
		90deg,
		var(--el-color-danger),
		var(--primary-500)
	); // 从危险色到主题色的渐变
	border-radius: 4px;
	transition: width 0.3s ease;
	width: 0%;
	position: relative;

	&::after {
		content: '→';
		position: absolute;
		right: -8px;
		top: 50%;
		transform: translateY(-50%);
		color: var(--primary-500); // 使用主题色
		font-weight: bold;
		font-size: 12px;
		transition: color 0.3s ease;
	}

	&.progress-cancelling {
		background: linear-gradient(
			90deg,
			var(--el-color-warning-light-3),
			var(--el-color-warning)
		); // 使用警告色渐变
		animation: pulse 1s ease-in-out infinite;
	}
}

.progress-start {
	position: absolute;
	left: -8px;
	top: 50%;
	transform: translateY(-50%);
	color: var(--el-color-danger); // 使用危险色
	font-weight: bold;
	font-size: 12px;
	transition: color 0.3s ease;
	z-index: 1;
}

.progress-text {
	font-size: var(--text-sm-size); // 使用 Typography 变量
	color: var(--el-text-color-secondary); // 使用次要文本色
	margin-top: 8px;
	transition: color 0.3s ease;
}

.spinner {
	width: 20px;
	height: 20px;
	border: 2px solid var(--el-fill-color); // 使用填充色
	border-top: 2px solid var(--primary-500); // 使用主题色
	border-radius: 50%;
	animation: spin 1s linear infinite;
	margin: 0 auto 16px;
	transition: border-color 0.3s ease;
}

@keyframes spin {
	0% {
		transform: rotate(0deg);
	}
	100% {
		transform: rotate(360deg);
	}
}

@keyframes pulse {
	0%,
	100% {
		opacity: 1;
	}
	50% {
		opacity: 0.7;
	}
}

.action-buttons {
	display: flex;
	gap: 12px;
	justify-content: center;
	margin-top: 16px;
	flex-wrap: wrap;
}

.cancel-btn {
	background: transparent;
	color: var(--el-text-color-regular); // 使用常规文本色
	border: 1px solid var(--el-border-color); // 使用边框色
	padding: 8px 16px;
	border-radius: var(--el-border-radius-small); // 使用小号圆角
	font-size: var(--text-sm-size); // 使用 Typography 变量
	cursor: pointer;
	transition: all 0.3s ease;

	&:hover {
		background: var(--el-fill-color-light); // 使用填充色
		border-color: var(--primary-500); // 使用主题色
		color: var(--el-text-color-primary); // 使用主要文本色
	}
}

.error-message {
	display: block;
	background: var(--el-fill-color-light); // 使用填充色
	border: 1px solid var(--el-color-danger); // 使用危险色边框
	border-radius: var(--el-border-radius-small); // 使用小号圆角
	padding: 20px;
	margin-top: 16px;
	color: var(--el-color-danger); // 使用危险色
	font-size: var(--text-sm-size); // 使用 Typography 变量
	transition:
		background-color 0.3s ease,
		border-color 0.3s ease,
		color 0.3s ease;
	text-align: center;
}

.error-icon {
	font-size: 24px;
	margin-bottom: 12px;
	text-align: center;
}

.error-content {
	text-align: center;

	strong {
		display: block;
		font-size: var(--text-base-size); // 使用 Typography 变量
		font-weight: 600;
		margin-bottom: 8px;
		color: var(--el-text-color-primary); // 使用主要文本色
	}
}

.error-details {
	margin: 8px 0;
	color: var(--el-text-color-regular); // 使用常规文本色
	font-size: var(--text-xs-size); // 使用 Typography 变量
}

.error-actions {
	display: flex;
	gap: 8px;
	justify-content: center;
	margin-top: 16px;
	flex-wrap: wrap;
}

.retry-btn {
	background: var(--primary-500); // 使用主题色
	color: var(--el-color-white); // 使用白色
	border: none;
	padding: 8px 16px;
	border-radius: var(--el-border-radius-small); // 使用小号圆角
	font-size: var(--text-sm-size); // 使用 Typography 变量
	cursor: pointer;
	transition: all 0.3s ease;

	&:hover {
		background: var(--primary-600); // 使用深一级的主题色
	}
}

.help-btn {
	background: transparent;
	color: var(--el-color-danger); // 使用危险色
	border: 1px solid var(--el-color-danger); // 使用危险色边框
	padding: 8px 16px;
	border-radius: var(--el-border-radius-small); // 使用小号圆角
	font-size: var(--text-sm-size); // 使用 Typography 变量
	cursor: pointer;
	transition: all 0.3s ease;

	&:hover {
		background: var(--el-color-danger); // 使用危险色
		color: var(--el-color-white); // 使用白色
	}
}

.fade-enter-active,
.fade-leave-active {
	transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
	opacity: 0;
}

/* 响应式设计 */
@media (max-width: 480px) {
	.container {
		padding: 24px;
		margin: 10px;
	}
}
</style>
