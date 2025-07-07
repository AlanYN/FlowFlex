<template>
	<div class="message-center">
		<!-- 消息中心头部 -->
		<div class="message-header">
			<div class="header-title">
				<h2>消息中心</h2>
				<p>与我们的团队保持沟通</p>
			</div>
			<div class="message-actions">
				<el-button type="primary" @click="openComposeDialog">
					<i class="el-icon-edit"></i>
					发送新消息
				</el-button>
			</div>
		</div>

		<!-- 消息统计 -->
		<div class="message-stats">
			<el-row :gutter="16">
				<el-col :span="8">
					<el-card>
						<div class="stat-content">
							<div class="stat-icon">
								<i class="el-icon-message" style="color: #409eff"></i>
							</div>
							<div class="stat-info">
								<div class="stat-value">{{ totalMessages }}</div>
								<div class="stat-label">总消息数</div>
							</div>
						</div>
					</el-card>
				</el-col>
				<el-col :span="8">
					<el-card>
						<div class="stat-content">
							<div class="stat-icon">
								<i class="el-icon-warning" style="color: #f56c6c"></i>
							</div>
							<div class="stat-info">
								<div class="stat-value">{{ unreadMessages }}</div>
								<div class="stat-label">未读消息</div>
							</div>
						</div>
					</el-card>
				</el-col>
				<el-col :span="8">
					<el-card>
						<div class="stat-content">
							<div class="stat-icon">
								<i class="el-icon-time" style="color: #67c23a"></i>
							</div>
							<div class="stat-info">
								<div class="stat-value">{{ todayMessages }}</div>
								<div class="stat-label">今日消息</div>
							</div>
						</div>
					</el-card>
				</el-col>
			</el-row>
		</div>

		<!-- 消息搜索和筛选 -->
		<div class="message-filters">
			<el-row :gutter="16">
				<el-col :span="12">
					<el-input
						v-model="searchQuery"
						placeholder="搜索消息内容..."
						prefix-icon="el-icon-search"
						clearable
						@input="handleSearch"
					/>
				</el-col>
				<el-col :span="6">
					<el-select v-model="priorityFilter" placeholder="优先级筛选" clearable>
						<el-option label="全部" value="" />
						<el-option label="高优先级" value="high" />
						<el-option label="普通" value="normal" />
						<el-option label="低优先级" value="low" />
					</el-select>
				</el-col>
				<el-col :span="6">
					<el-select v-model="categoryFilter" placeholder="分类筛选" clearable>
						<el-option label="全部" value="" />
						<el-option label="欢迎消息" value="welcome" />
						<el-option label="状态更新" value="update" />
						<el-option label="需要操作" value="action_required" />
						<el-option label="通知" value="notification" />
					</el-select>
				</el-col>
			</el-row>
		</div>

		<!-- 消息列表 -->
		<div class="message-list">
			<el-card v-for="message in filteredMessages" :key="message.id" class="message-item">
				<div class="message-content" @click="handleMessageClick(message)">
					<div class="message-meta">
						<div class="sender-info">
							<el-avatar :size="40" :src="message.senderAvatar">
								<i class="el-icon-user-solid"></i>
							</el-avatar>
							<div class="sender-details">
								<div class="sender-name">{{ message.sender }}</div>
								<div class="sender-role">{{ message.senderRole }}</div>
							</div>
						</div>
						<div class="message-info">
							<div class="message-time">{{ message.time }}</div>
							<div class="message-badges">
								<el-badge
									v-if="message.unread"
									value="未读"
									type="danger"
									class="unread-badge"
								/>
								<el-tag
									:type="getPriorityType(message.priority)"
									size="mini"
									class="priority-tag"
								>
									{{ getPriorityText(message.priority) }}
								</el-tag>
							</div>
						</div>
					</div>
					<div class="message-body">
						<h4 class="message-subject">{{ message.subject }}</h4>
						<p class="message-preview">{{ message.content }}</p>
					</div>
					<div class="message-actions">
						<el-button size="mini" @click.stop="handleReply(message)">
							<i class="el-icon-back"></i>
							回复
						</el-button>
						<el-button
							v-if="message.unread"
							size="mini"
							type="success"
							@click.stop="markAsRead(message.id)"
						>
							<i class="el-icon-check"></i>
							标记已读
						</el-button>
					</div>
				</div>
			</el-card>

			<!-- 空状态 -->
			<div v-if="filteredMessages.length === 0" class="empty-state">
				<i class="el-icon-message"></i>
				<p>暂无消息</p>
			</div>
		</div>

		<!-- 发送消息对话框 -->
		<el-dialog title="发送新消息" v-model:visible="composeDialogVisible" width="600px">
			<el-form
				:model="composeForm"
				:rules="composeRules"
				ref="composeForm"
				label-width="80px"
			>
				<el-form-item label="收件人" prop="recipient">
					<el-select
						v-model="composeForm.recipient"
						placeholder="选择收件人"
						style="width: 100%"
					>
						<el-option-group label="团队成员">
							<el-option
								v-for="member in teamMembers"
								:key="member.id"
								:label="member.name"
								:value="member.id"
							>
								<span style="float: left">{{ member.name }}</span>
								<span style="float: right; color: #8492a6; font-size: 13px">
									{{ member.role }}
								</span>
							</el-option>
						</el-option-group>
						<el-option-group label="团队组">
							<el-option label="销售团队" value="sales_team" />
							<el-option label="实施团队" value="implementation_team" />
							<el-option label="技术支持" value="tech_support" />
						</el-option-group>
					</el-select>
				</el-form-item>
				<el-form-item label="主题" prop="subject">
					<el-input v-model="composeForm.subject" placeholder="请输入消息主题" />
				</el-form-item>
				<el-form-item label="优先级" prop="priority">
					<el-radio-group v-model="composeForm.priority">
						<el-radio label="low">低</el-radio>
						<el-radio label="normal">普通</el-radio>
						<el-radio label="high">高</el-radio>
					</el-radio-group>
				</el-form-item>
				<el-form-item label="内容" prop="content">
					<el-input
						v-model="composeForm.content"
						type="textarea"
						:rows="6"
						placeholder="请输入消息内容"
					/>
				</el-form-item>
			</el-form>
			<template #footer>
				<div>
					<el-button @click="composeDialogVisible = false">取消</el-button>
					<el-button type="primary" @click="sendMessage" :loading="sending">
						发送
					</el-button>
				</div>
			</template>
		</el-dialog>

		<!-- 回复对话框 -->
		<el-dialog title="回复消息" v-model:visible="replyDialogVisible" width="600px">
			<div v-if="replyMessage" class="reply-context">
				<h4>原消息:</h4>
				<div class="original-message">
					<div class="original-header">
						<strong>{{ replyMessage.sender }}</strong>
						<span class="original-time">{{ replyMessage.time }}</span>
					</div>
					<div class="original-subject">主题: {{ replyMessage.subject }}</div>
					<div class="original-content">{{ replyMessage.content }}</div>
				</div>
			</div>
			<el-form :model="replyForm" :rules="replyRules" ref="replyForm" label-width="80px">
				<el-form-item label="回复内容" prop="content">
					<el-input
						v-model="replyForm.content"
						type="textarea"
						:rows="6"
						placeholder="请输入回复内容"
					/>
				</el-form-item>
			</el-form>
			<template #footer>
				<div>
					<el-button @click="replyDialogVisible = false">取消</el-button>
					<el-button type="primary" @click="sendReply" :loading="replying">
						发送回复
					</el-button>
				</div>
			</template>
		</el-dialog>
	</div>
</template>

<script>
export default {
	name: 'MessageCenter',
	props: {
		messages: {
			type: Array,
			default: () => [],
		},
		teamMembers: {
			type: Array,
			default: () => [],
		},
	},
	data() {
		return {
			searchQuery: '',
			priorityFilter: '',
			categoryFilter: '',
			composeDialogVisible: false,
			replyDialogVisible: false,
			replyMessage: null,
			sending: false,
			replying: false,
			composeForm: {
				recipient: '',
				subject: '',
				priority: 'normal',
				content: '',
			},
			replyForm: {
				content: '',
			},
			composeRules: {
				recipient: [{ required: true, message: '请选择收件人', trigger: 'change' }],
				subject: [{ required: true, message: '请输入主题', trigger: 'blur' }],
				content: [{ required: true, message: '请输入消息内容', trigger: 'blur' }],
			},
			replyRules: {
				content: [{ required: true, message: '请输入回复内容', trigger: 'blur' }],
			},
		};
	},
	computed: {
		totalMessages() {
			return this.messages.length;
		},
		unreadMessages() {
			return this.messages.filter((m) => m.unread).length;
		},
		todayMessages() {
			const today = new Date().toDateString();
			return this.messages.filter((m) => {
				const messageDate = new Date(m.timestamp || Date.now()).toDateString();
				return messageDate === today;
			}).length;
		},
		filteredMessages() {
			return this.messages.filter((message) => {
				const matchesSearch =
					!this.searchQuery ||
					message.subject.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
					message.content.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
					message.sender.toLowerCase().includes(this.searchQuery.toLowerCase());

				const matchesPriority =
					!this.priorityFilter || message.priority === this.priorityFilter;
				const matchesCategory =
					!this.categoryFilter || message.category === this.categoryFilter;

				return matchesSearch && matchesPriority && matchesCategory;
			});
		},
	},
	methods: {
		handleSearch() {
			// 搜索逻辑已在computed中处理
		},
		handleMessageClick(message) {
			if (message.unread) {
				this.markAsRead(message.id);
			}
		},
		markAsRead(messageId) {
			this.$emit('mark-read', messageId);
		},
		openComposeDialog() {
			this.composeDialogVisible = true;
			this.resetComposeForm();
		},
		handleReply(message) {
			this.replyMessage = message;
			this.replyDialogVisible = true;
			this.resetReplyForm();
		},
		sendMessage() {
			this.$refs.composeForm.validate((valid) => {
				if (valid) {
					this.sending = true;
					// 模拟发送延迟
					setTimeout(() => {
						this.$emit('send-message', { ...this.composeForm });
						this.sending = false;
						this.composeDialogVisible = false;
						this.$message.success('消息发送成功');
					}, 1000);
				}
			});
		},
		sendReply() {
			this.$refs.replyForm.validate((valid) => {
				if (valid) {
					this.replying = true;
					setTimeout(() => {
						this.$emit('send-reply', {
							originalMessageId: this.replyMessage.id,
							content: this.replyForm.content,
						});
						this.replying = false;
						this.replyDialogVisible = false;
						this.$message.success('回复发送成功');
					}, 1000);
				}
			});
		},
		resetComposeForm() {
			this.composeForm = {
				recipient: '',
				subject: '',
				priority: 'normal',
				content: '',
			};
		},
		resetReplyForm() {
			this.replyForm = {
				content: '',
			};
		},
		getPriorityType(priority) {
			switch (priority) {
				case 'high':
					return 'danger';
				case 'low':
					return 'info';
				default:
					return 'warning';
			}
		},
		getPriorityText(priority) {
			switch (priority) {
				case 'high':
					return '高';
				case 'low':
					return '低';
				default:
					return '普通';
			}
		},
	},
};
</script>

<style scoped>
.message-center {
	padding: 20px;
}

.message-header {
	display: flex;
	justify-content: space-between;
	align-items: flex-start;
	margin-bottom: 24px;
}

.header-title h2 {
	margin: 0 0 8px 0;
	color: #1f2937;
	font-size: 24px;
	font-weight: 600;
}

.header-title p {
	margin: 0;
	color: #6b7280;
	font-size: 14px;
}

.message-stats {
	margin-bottom: 24px;
}

.stat-content {
	display: flex;
	align-items: center;
	gap: 16px;
}

.stat-icon {
	font-size: 24px;
	padding: 12px;
	border-radius: 8px;
	background-color: #f3f4f6;
}

.stat-value {
	font-size: 24px;
	font-weight: 700;
	color: #1f2937;
	margin-bottom: 4px;
}

.stat-label {
	font-size: 12px;
	color: #6b7280;
}

.message-filters {
	margin-bottom: 24px;
}

.message-list {
	max-height: 600px;
	overflow-y: auto;
}

.message-item {
	margin-bottom: 16px;
	cursor: pointer;
	transition: all 0.3s ease;
}

.message-item:hover {
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.message-content {
	padding: 16px;
}

.message-meta {
	display: flex;
	justify-content: space-between;
	align-items: flex-start;
	margin-bottom: 16px;
}

.sender-info {
	display: flex;
	align-items: center;
	gap: 12px;
}

.sender-name {
	font-weight: 600;
	color: #1f2937;
	margin-bottom: 4px;
}

.sender-role {
	font-size: 12px;
	color: #6b7280;
}

.message-info {
	text-align: right;
}

.message-time {
	font-size: 12px;
	color: #6b7280;
	margin-bottom: 8px;
}

.message-badges {
	display: flex;
	gap: 8px;
	justify-content: flex-end;
}

.message-body {
	margin-bottom: 16px;
}

.message-subject {
	margin: 0 0 8px 0;
	font-size: 16px;
	font-weight: 600;
	color: #1f2937;
}

.message-preview {
	margin: 0;
	color: #4b5563;
	line-height: 1.5;
	display: -webkit-box;
	-webkit-line-clamp: 2;
	-webkit-box-orient: vertical;
	overflow: hidden;
}

.message-actions {
	display: flex;
	gap: 8px;
	justify-content: flex-end;
}

.empty-state {
	text-align: center;
	padding: 60px 20px;
	color: #9ca3af;
}

.empty-state i {
	font-size: 48px;
	margin-bottom: 16px;
	display: block;
}

.reply-context {
	margin-bottom: 24px;
}

.original-message {
	background-color: #f9fafb;
	border-left: 4px solid #e5e7eb;
	padding: 16px;
	border-radius: 4px;
}

.original-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	margin-bottom: 8px;
}

.original-time {
	font-size: 12px;
	color: #6b7280;
}

.original-subject {
	font-weight: 500;
	margin-bottom: 8px;
	color: #374151;
}

.original-content {
	color: #4b5563;
	line-height: 1.5;
}

@media (max-width: 768px) {
	.message-header {
		flex-direction: column;
		gap: 16px;
	}

	.message-meta {
		flex-direction: column;
		gap: 12px;
	}

	.message-info {
		text-align: left;
	}

	.message-badges {
		justify-content: flex-start;
	}

	.message-actions {
		justify-content: flex-start;
	}
}
</style>
