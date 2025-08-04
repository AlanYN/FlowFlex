<template>
	<div class="onboarding-progress">
		<!-- 进度概览 -->
		<div class="progress-header">
			<div class="progress-title">
				<h2>入职进度概览</h2>
				<p>您的入职流程当前进度</p>
			</div>
			<div class="progress-stats">
				<div class="stat-item">
					<div class="stat-value">{{ customerData.overallProgress }}%</div>
					<div class="stat-label">总体进度</div>
				</div>
				<div class="stat-item">
					<div class="stat-value">{{ completedStages }}</div>
					<div class="stat-label">已完成阶段</div>
				</div>
				<div class="stat-item">
					<div class="stat-value">{{ remainingDays }}</div>
					<div class="stat-label">预计剩余天数</div>
				</div>
			</div>
		</div>

		<!-- 整体进度条 -->
		<div class="overall-progress">
			<el-progress
				:percentage="customerData.overallProgress"
				:stroke-width="12"
				:text-inside="true"
				status="success"
			/>
		</div>

		<!-- 阶段列表 -->
		<div class="stages-container">
			<div class="stages-timeline">
				<div
					v-for="(stage, index) in visibleStages"
					:key="stage.id"
					class="stage-item"
					:class="{
						completed: stage.status === 'completed',
						'in-progress': stage.status === 'in_progress',
						pending: stage.status === 'pending',
						editable: stage.portalEditable,
					}"
					@click="handleStageClick(stage)"
				>
					<!-- 阶段连接线 -->
					<div v-if="index < visibleStages.length - 1" class="stage-connector"></div>

					<!-- 阶段图标 -->
					<div class="stage-icon" :style="{ backgroundColor: getStageColor(stage) }">
						<i :class="getStageIcon(stage)"></i>
					</div>

					<!-- 阶段内容 -->
					<div class="stage-content">
						<div class="stage-header">
							<h4>{{ stage.name }}</h4>
							<div class="stage-status">
								<el-tag :type="getStatusType(stage.status)" size="small">
									{{ getStatusText(stage.status) }}
								</el-tag>
								<span v-if="stage.completedDate" class="completion-date">
									{{ formatDate(stage.completedDate) }}
								</span>
							</div>
						</div>

						<p class="stage-description">{{ stage.description }}</p>

						<!-- 可编辑阶段的操作按钮 -->
						<div
							v-if="
								stage.portalEditable &&
								(stage.status === 'in_progress' || stage.status === 'pending')
							"
							class="stage-actions"
						>
							<el-button
								type="primary"
								size="small"
								@click.stop="handleStageAction(stage)"
							>
								<i class="el-icon-edit"></i>
								{{ stage.status === 'in_progress' ? '继续完成' : '开始' }}
							</el-button>
						</div>

						<!-- 阶段进度详情 -->
						<div v-if="stage.status === 'in_progress'" class="stage-progress">
							<div class="progress-info">
								<span>阶段进度</span>
								<span>{{ getStageProgress(stage) }}%</span>
							</div>
							<el-progress
								:percentage="getStageProgress(stage)"
								:stroke-width="6"
								:show-text="false"
								:color="stage.color"
							/>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 预计完成时间 -->
		<div class="completion-info">
			<el-card>
				<div class="completion-content">
					<div class="completion-item">
						<i class="el-icon-date"></i>
						<div>
							<label>开始日期:</label>
							<span>{{ formatDate(customerData.startDate) }}</span>
						</div>
					</div>
					<div class="completion-item">
						<i class="el-icon-time"></i>
						<div>
							<label>预计完成:</label>
							<span>{{ formatDate(customerData.estimatedCompletion) }}</span>
						</div>
					</div>
					<div class="completion-item">
						<i class="el-icon-user"></i>
						<div>
							<label>客户经理:</label>
							<span>{{ customerData.accountManager }}</span>
						</div>
					</div>
				</div>
			</el-card>
		</div>
	</div>
</template>

<script>
export default {
	name: 'OnboardingProgress',
	props: {
		customerData: {
			type: Object,
			required: true,
		},
		stages: {
			type: Array,
			required: true,
		},
	},
	computed: {
		visibleStages() {
			return this.stages
				.filter((stage) => stage.portalVisible)
				.sort((a, b) => a.order - b.order);
		},

		completedStages() {
			return this.visibleStages.filter((stage) => stage.status === 'completed').length;
		},

		remainingDays() {
			const today = new Date();
			const estimatedCompletion = new Date(this.customerData.estimatedCompletion);
			const diffTime = estimatedCompletion - today;
			const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
			return Math.max(0, diffDays);
		},
	},
	methods: {
		handleStageClick(stage) {
			if (stage.portalEditable) {
				this.$emit('stage-click', stage);
			}
		},

		handleStageAction(stage) {
			this.$emit('stage-action', stage);
		},

		getStageColor(stage) {
			return stage.color || '#409EFF';
		},

		getStageIcon(stage) {
			switch (stage.status) {
				case 'completed':
					return 'el-icon-check';
				case 'in_progress':
					return 'el-icon-loading';
				case 'pending':
					return 'el-icon-time';
				default:
					return 'el-icon-circle-check';
			}
		},

		getStatusType(status) {
			switch (status) {
				case 'completed':
					return 'success';
				case 'in_progress':
					return 'warning';
				case 'pending':
					return 'info';
				default:
					return 'info';
			}
		},

		getStatusText(status) {
			switch (status) {
				case 'completed':
					return '已完成';
				case 'in_progress':
					return '进行中';
				case 'pending':
					return '待开始';
				default:
					return '未知';
			}
		},

		getStageProgress(stage) {
			// 这里可以根据实际业务逻辑计算阶段进度
			if (stage.status === 'in_progress') {
				return 60; // 示例进度
			}
			return 0;
		},

		formatDate(dateString) {
			if (!dateString) return '';
			try {
				const date = new Date(dateString);
				if (isNaN(date.getTime())) {
					return String(dateString);
				}
				// Format as MM/dd/yyyy (US format)
				const month = String(date.getMonth() + 1).padStart(2, '0');
				const day = String(date.getDate()).padStart(2, '0');
				const year = date.getFullYear();
				return `${month}/${day}/${year}`;
			} catch {
				return String(dateString);
			}
		},
	},
};
</script>

<style scoped>
.onboarding-progress {
	padding: 20px;
}

.progress-header {
	display: flex;
	justify-content: space-between;
	align-items: flex-start;
	margin-bottom: 24px;
}

.progress-title h2 {
	margin: 0 0 8px 0;
	color: #1f2937;
	font-size: 24px;
	font-weight: 600;
}

.progress-title p {
	margin: 0;
	color: #6b7280;
	font-size: 14px;
}

.progress-stats {
	display: flex;
	gap: 32px;
}

.stat-item {
	text-align: center;
}

.stat-value {
	font-size: 28px;
	font-weight: 700;
	color: #3b82f6;
	margin-bottom: 4px;
}

.stat-label {
	font-size: 12px;
	color: #6b7280;
	text-transform: uppercase;
	letter-spacing: 0.5px;
}

.overall-progress {
	margin-bottom: 32px;
}

.stages-container {
	margin-bottom: 32px;
}

.stages-timeline {
	position: relative;
}

.stage-item {
	display: flex;
	align-items: flex-start;
	margin-bottom: 32px;
	position: relative;
	cursor: pointer;
	transition: all 0.3s ease;
}

.stage-item.editable:hover {
	transform: translateX(4px);
}

.stage-connector {
	position: absolute;
	left: 20px;
	top: 40px;
	width: 2px;
	height: 32px;
	background-color: #e5e7eb;
}

.stage-item.completed .stage-connector {
	background-color: #10b981;
}

.stage-icon {
	width: 40px;
	height: 40px;
	border-radius: 50%;
	display: flex;
	align-items: center;
	justify-content: center;
	color: white;
	font-size: 16px;
	flex-shrink: 0;
	margin-right: 16px;
	box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.stage-content {
	flex: 1;
	background: #f9fafb;
	border-radius: 8px;
	padding: 16px;
	border-left: 4px solid transparent;
	transition: all 0.3s ease;
}

.stage-item.completed .stage-content {
	border-left-color: #10b981;
	background: #f0fdf4;
}

.stage-item.in-progress .stage-content {
	border-left-color: #f59e0b;
	background: #fffbeb;
}

.stage-item.editable .stage-content:hover {
	background: #f3f4f6;
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.stage-header {
	display: flex;
	justify-content: space-between;
	align-items: flex-start;
	margin-bottom: 8px;
}

.stage-header h4 {
	margin: 0;
	color: #1f2937;
	font-size: 16px;
	font-weight: 600;
}

.stage-status {
	display: flex;
	align-items: center;
	gap: 8px;
}

.completion-date {
	font-size: 12px;
	color: #6b7280;
}

.stage-description {
	margin: 0 0 16px 0;
	color: #4b5563;
	font-size: 14px;
	line-height: 1.5;
}

.stage-actions {
	margin-bottom: 16px;
}

.stage-progress {
	margin-top: 12px;
}

.progress-info {
	display: flex;
	justify-content: space-between;
	align-items: center;
	margin-bottom: 8px;
	font-size: 12px;
	color: #6b7280;
}

.completion-info {
	margin-top: 24px;
}

.completion-content {
	display: flex;
	justify-content: space-around;
	align-items: center;
}

.completion-item {
	display: flex;
	align-items: center;
	gap: 8px;
}

.completion-item i {
	color: #3b82f6;
	font-size: 18px;
}

.completion-item label {
	color: #6b7280;
	font-size: 14px;
	margin-right: 4px;
}

.completion-item span {
	color: #1f2937;
	font-weight: 500;
}

@media (max-width: 768px) {
	.progress-header {
		flex-direction: column;
		gap: 16px;
	}

	.progress-stats {
		justify-content: space-around;
		width: 100%;
	}

	.completion-content {
		flex-direction: column;
		gap: 16px;
	}

	.stage-item {
		margin-left: 0;
	}

	.stage-connector {
		left: 20px;
	}
}
</style>
