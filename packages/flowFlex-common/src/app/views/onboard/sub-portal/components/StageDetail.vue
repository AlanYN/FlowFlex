<template>
	<div class="stage-detail">
		<!-- 阶段基本信息 -->
		<div class="stage-info">
			<div class="stage-header">
				<div class="stage-title">
					<div class="stage-icon" :style="{ backgroundColor: stage.color }">
						<i :class="getStageIcon(stage.status)"></i>
					</div>
					<div class="stage-name">
						<h3>{{ stage.name }}</h3>
						<p>{{ stage.description }}</p>
					</div>
				</div>
				<div class="stage-status">
					<el-tag :type="getStatusType(stage.status)" size="medium">
						{{ getStatusText(stage.status) }}
					</el-tag>
				</div>
			</div>
		</div>

		<!-- 阶段进度 -->
		<div v-if="stage.status === 'in_progress'" class="stage-progress-section">
			<h4>当前进度</h4>
			<el-progress
				:percentage="stageProgress"
				:stroke-width="12"
				:text-inside="true"
				:color="stage.color"
			/>
			<div class="progress-info">
				<span>已完成 {{ completedTasks }}/{{ totalTasks }} 项任务</span>
			</div>
		</div>

		<!-- 任务列表 -->
		<div class="tasks-section">
			<h4>阶段任务</h4>
			<div class="task-list">
				<div
					v-for="task in stageTasks"
					:key="task.id"
					class="task-item"
					:class="{ completed: task.completed, editable: task.editable }"
				>
					<div class="task-content">
						<div class="task-checkbox">
							<el-checkbox
								v-model="task.completed"
								:disabled="!task.editable"
								@change="handleTaskToggle(task)"
							/>
						</div>
						<div class="task-info">
							<h5 :class="{ completed: task.completed }">{{ task.title }}</h5>
							<p>{{ task.description }}</p>
							<div class="task-meta">
								<span v-if="task.dueDate" class="due-date">
									<i class="el-icon-time"></i>
									截止时间: {{ formatDate(task.dueDate) }}
								</span>
								<span v-if="task.assignee" class="assignee">
									<i class="el-icon-user"></i>
									负责人: {{ task.assignee }}
								</span>
							</div>
						</div>
					</div>
					<div v-if="task.editable && !task.completed" class="task-actions">
						<el-button size="mini" type="primary" @click="startTask(task)">
							开始任务
						</el-button>
					</div>
				</div>
			</div>
		</div>

		<!-- 文档要求 -->
		<div v-if="requiredDocuments.length > 0" class="documents-section">
			<h4>所需文档</h4>
			<div class="document-list">
				<div
					v-for="doc in requiredDocuments"
					:key="doc.id"
					class="document-item"
					:class="{ uploaded: doc.uploaded }"
				>
					<div class="document-info">
						<div class="document-icon">
							<i :class="getDocumentIcon(doc.type)"></i>
						</div>
						<div class="document-details">
							<h5>{{ doc.name }}</h5>
							<p>{{ doc.description }}</p>
							<div v-if="doc.uploaded" class="upload-info">
								<el-tag type="success" size="mini">已上传</el-tag>
								<span class="upload-date">{{ formatDate(doc.uploadDate) }}</span>
							</div>
						</div>
					</div>
					<div class="document-actions">
						<el-upload
							v-if="!doc.uploaded"
							:action="uploadUrl"
							:on-success="(response) => handleDocumentUpload(response, doc)"
							:before-upload="beforeUpload"
							:show-file-list="false"
						>
							<el-button size="mini" type="primary">
								<i class="el-icon-upload2"></i>
								上传
							</el-button>
						</el-upload>
						<el-button v-else size="mini" @click="downloadDocument(doc)">
							<i class="el-icon-download"></i>
							下载
						</el-button>
					</div>
				</div>
			</div>
		</div>

		<!-- 阶段表单 -->
		<div v-if="stageForm && stage.portalEditable" class="stage-form-section">
			<h4>{{ stage.name }}表单</h4>
			<el-form ref="stageForm" :model="formData" :rules="formRules" label-width="120px">
				<el-form-item
					v-for="field in stageForm.fields"
					:key="field.name"
					:label="field.label"
					:prop="field.name"
				>
					<!-- 文本输入 -->
					<el-input
						v-if="field.type === 'text'"
						v-model="formData[field.name]"
						:placeholder="field.placeholder"
					/>
					<!-- 多行文本 -->
					<el-input
						v-else-if="field.type === 'textarea'"
						v-model="formData[field.name]"
						type="textarea"
						:rows="4"
						:placeholder="field.placeholder"
					/>
					<!-- 选择框 -->
					<el-select
						v-else-if="field.type === 'select'"
						v-model="formData[field.name]"
						:placeholder="field.placeholder"
						style="width: 100%"
					>
						<el-option
							v-for="option in field.options"
							:key="option.value"
							:label="option.label"
							:value="option.value"
						/>
					</el-select>
					<!-- 多选框 -->
					<el-checkbox-group
						v-else-if="field.type === 'checkbox'"
						v-model="formData[field.name]"
					>
						<el-checkbox
							v-for="option in field.options"
							:key="option.value"
							:label="option.value"
						>
							{{ option.label }}
						</el-checkbox>
					</el-checkbox-group>
					<!-- 单选框 -->
					<el-radio-group
						v-else-if="field.type === 'radio'"
						v-model="formData[field.name]"
					>
						<el-radio
							v-for="option in field.options"
							:key="option.value"
							:label="option.value"
						>
							{{ option.label }}
						</el-radio>
					</el-radio-group>
					<!-- 日期选择 -->
					<el-date-picker
						v-else-if="field.type === 'date'"
						v-model="formData[field.name]"
						type="date"
						:placeholder="field.placeholder"
						format="yyyy-MM-dd"
						value-format="yyyy-MM-dd"
					/>
					<!-- 数字输入 -->
					<el-input-number
						v-else-if="field.type === 'number'"
						v-model="formData[field.name]"
						:min="field.min"
						:max="field.max"
						:placeholder="field.placeholder"
					/>
				</el-form-item>
			</el-form>
		</div>

		<!-- 备注和历史 -->
		<div class="notes-section">
			<h4>阶段备注</h4>
			<div class="notes-list">
				<div v-for="note in stageNotes" :key="note.id" class="note-item">
					<div class="note-header">
						<span class="note-author">{{ note.author }}</span>
						<span class="note-date">{{ formatDateTime(note.createdAt) }}</span>
					</div>
					<div class="note-content">{{ note.content }}</div>
				</div>
			</div>
			<div v-if="stage.portalEditable" class="add-note">
				<el-input v-model="newNote" type="textarea" :rows="3" placeholder="添加备注..." />
				<el-button
					type="primary"
					size="small"
					@click="addNote"
					:disabled="!newNote.trim()"
					style="margin-top: 8px"
				>
					添加备注
				</el-button>
			</div>
		</div>

		<!-- 操作按钮 -->
		<div class="stage-actions">
			<el-button @click="$emit('close')">关闭</el-button>
			<el-button
				v-if="stage.portalEditable && hasChanges"
				type="primary"
				@click="saveStage"
				:loading="saving"
			>
				保存更改
			</el-button>
			<el-button
				v-if="stage.portalEditable && canComplete"
				type="success"
				@click="completeStage"
				:loading="completing"
			>
				完成阶段
			</el-button>
		</div>
	</div>
</template>

<script>
export default {
	name: 'StageDetail',
	props: {
		stage: {
			type: Object,
			required: true,
		},
		customerData: {
			type: Object,
			required: true,
		},
	},
	data() {
		return {
			saving: false,
			completing: false,
			newNote: '',
			formData: {},
			originalFormData: {},
			uploadUrl: '/api/documents/upload',
			stageTasks: [
				{
					id: 1,
					title: '填写基本信息',
					description: '完善公司基本信息和联系方式',
					completed: true,
					editable: false,
					dueDate: '2024-01-20',
					assignee: '客户',
				},
				{
					id: 2,
					title: '上传营业执照',
					description: '上传公司营业执照扫描件',
					completed: false,
					editable: true,
					dueDate: '2024-01-25',
					assignee: '客户',
				},
				{
					id: 3,
					title: '完成问卷调查',
					description: '填写详细的业务需求问卷',
					completed: false,
					editable: true,
					dueDate: '2024-01-30',
					assignee: '客户',
				},
			],
			requiredDocuments: [
				{
					id: 1,
					name: '营业执照',
					description: '公司营业执照扫描件',
					type: 'pdf',
					uploaded: false,
					required: true,
				},
				{
					id: 2,
					name: '法人身份证',
					description: '法定代表人身份证扫描件',
					type: 'image',
					uploaded: true,
					uploadDate: '2024-01-18',
					required: true,
				},
			],
			stageNotes: [
				{
					id: 1,
					author: 'Sarah Johnson',
					content: '客户已提交初步资料，正在审核中',
					createdAt: '2024-01-18T10:30:00Z',
				},
				{
					id: 2,
					author: '系统',
					content: '阶段状态更新为进行中',
					createdAt: '2024-01-18T09:15:00Z',
				},
			],
			stageForm: null,
			formRules: {},
		};
	},
	computed: {
		stageProgress() {
			if (this.totalTasks === 0) return 0;
			return Math.round((this.completedTasks / this.totalTasks) * 100);
		},
		completedTasks() {
			return this.stageTasks.filter((task) => task.completed).length;
		},
		totalTasks() {
			return this.stageTasks.length;
		},
		hasChanges() {
			return JSON.stringify(this.formData) !== JSON.stringify(this.originalFormData);
		},
		canComplete() {
			// 检查是否所有必需任务都完成
			const requiredTasks = this.stageTasks.filter((task) => task.required !== false);
			return requiredTasks.every((task) => task.completed);
		},
	},
	created() {
		this.initializeStageData();
	},
	methods: {
		initializeStageData() {
			// 根据阶段类型加载相应的表单配置
			this.loadStageForm();
			// 初始化表单数据
			this.initializeFormData();
		},
		loadStageForm() {
			// 根据阶段ID加载对应的表单配置
			const formConfigs = {
				questionnaire: {
					fields: [
						{
							name: 'companySize',
							label: '公司规模',
							type: 'select',
							placeholder: '请选择公司规模',
							options: [
								{ label: '小型企业', value: 'small' },
								{ label: '中型企业', value: 'medium' },
								{ label: '大型企业', value: 'large' },
							],
							required: true,
						},
						{
							name: 'businessDescription',
							label: '业务描述',
							type: 'textarea',
							placeholder: '请描述您的主要业务',
							required: true,
						},
					],
				},
			};

			this.stageForm = formConfigs[this.stage.id] || null;
		},
		initializeFormData() {
			if (this.stageForm) {
				const data = {};
				this.stageForm.fields.forEach((field) => {
					data[field.name] = field.type === 'checkbox' ? [] : '';
				});
				this.formData = data;
				this.originalFormData = { ...data };

				// 设置表单验证规则
				const rules = {};
				this.stageForm.fields.forEach((field) => {
					if (field.required) {
						rules[field.name] = [
							{ required: true, message: `请填写${field.label}`, trigger: 'blur' },
						];
					}
				});
				this.formRules = rules;
			}
		},
		handleTaskToggle(task) {
			this.$emit('task-toggle', {
				stageId: this.stage.id,
				taskId: task.id,
				completed: task.completed,
			});
		},
		startTask(task) {
			// 根据任务类型执行相应操作
			if (task.id === 3) {
				// 跳转到问卷页面
				this.$emit('navigate-to-questionnaire');
			}
		},
		handleDocumentUpload(response, doc) {
			doc.uploaded = true;
			doc.uploadDate = new Date().toISOString();
			this.$message.success('文档上传成功');
		},
		beforeUpload(file) {
			const isValidSize = file.size / 1024 / 1024 < 10;
			if (!isValidSize) {
				this.$message.error('文件大小不能超过10MB');
			}
			return isValidSize;
		},
		downloadDocument(doc) {
			// 实现文档下载
			this.$message.info('开始下载文档');
		},
		addNote() {
			if (this.newNote.trim()) {
				const note = {
					id: Date.now(),
					author: this.customerData.contactName,
					content: this.newNote.trim(),
					createdAt: new Date().toISOString(),
				};
				this.stageNotes.unshift(note);
				this.newNote = '';
				this.$message.success('备注添加成功');
			}
		},
		saveStage() {
			if (this.stageForm) {
				this.$refs.stageForm.validate((valid) => {
					if (valid) {
						this.saving = true;
						// 模拟保存
						setTimeout(() => {
							this.saving = false;
							this.originalFormData = { ...this.formData };
							this.$message.success('保存成功');
							this.$emit('update-stage', {
								stageId: this.stage.id,
								formData: this.formData,
							});
						}, 1000);
					}
				});
			} else {
				this.saving = true;
				setTimeout(() => {
					this.saving = false;
					this.$message.success('保存成功');
				}, 1000);
			}
		},
		completeStage() {
			this.$confirm('确定要完成此阶段吗？', '确认操作', {
				confirmButtonText: '确定',
				cancelButtonText: '取消',
				type: 'warning',
			}).then(() => {
				this.completing = true;
				setTimeout(() => {
					this.completing = false;
					this.$message.success('阶段完成成功');
					this.$emit('complete-stage', this.stage.id);
					this.$emit('close');
				}, 1500);
			});
		},
		getStageIcon(status) {
			switch (status) {
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
		getDocumentIcon(type) {
			switch (type) {
				case 'pdf':
					return 'el-icon-document';
				case 'image':
					return 'el-icon-picture';
				case 'excel':
					return 'el-icon-s-grid';
				default:
					return 'el-icon-document';
			}
		},
		formatDate(dateString) {
			if (!dateString) return '';
			const date = new Date(dateString);
			return date.toLocaleDateString('zh-CN');
		},
		formatDateTime(dateString) {
			if (!dateString) return '';
			const date = new Date(dateString);
			return date.toLocaleString('zh-CN');
		},
	},
};
</script>

<style scoped>
.stage-detail {
	padding: 20px;
	max-height: 70vh;
	overflow-y: auto;
}

.stage-info {
	margin-bottom: 24px;
}

.stage-header {
	display: flex;
	justify-content: space-between;
	align-items: flex-start;
}

.stage-title {
	display: flex;
	align-items: flex-start;
	gap: 16px;
}

.stage-icon {
	width: 48px;
	height: 48px;
	border-radius: 50%;
	display: flex;
	align-items: center;
	justify-content: center;
	color: white;
	font-size: 20px;
	flex-shrink: 0;
}

.stage-name h3 {
	margin: 0 0 8px 0;
	color: #1f2937;
	font-size: 20px;
	font-weight: 600;
}

.stage-name p {
	margin: 0;
	color: #6b7280;
	font-size: 14px;
}

.stage-progress-section {
	margin-bottom: 24px;
	padding: 16px;
	background: #f9fafb;
	border-radius: 8px;
}

.stage-progress-section h4 {
	margin: 0 0 16px 0;
	color: #1f2937;
	font-size: 16px;
	font-weight: 600;
}

.progress-info {
	margin-top: 8px;
	font-size: 14px;
	color: #6b7280;
}

.tasks-section,
.documents-section,
.stage-form-section,
.notes-section {
	margin-bottom: 24px;
}

.tasks-section h4,
.documents-section h4,
.stage-form-section h4,
.notes-section h4 {
	margin: 0 0 16px 0;
	color: #1f2937;
	font-size: 16px;
	font-weight: 600;
}

.task-item,
.document-item {
	display: flex;
	justify-content: space-between;
	align-items: flex-start;
	padding: 16px;
	margin-bottom: 12px;
	background: #f9fafb;
	border-radius: 8px;
	border-left: 4px solid transparent;
}

.task-item.completed {
	border-left-color: #10b981;
	background: #f0fdf4;
}

.task-item.editable {
	border-left-color: #3b82f6;
}

.task-content {
	display: flex;
	align-items: flex-start;
	gap: 12px;
	flex: 1;
}

.task-info h5 {
	margin: 0 0 4px 0;
	color: #1f2937;
	font-size: 14px;
	font-weight: 600;
}

.task-info h5.completed {
	text-decoration: line-through;
	color: #6b7280;
}

.task-info p {
	margin: 0 0 8px 0;
	color: #6b7280;
	font-size: 13px;
}

.task-meta {
	display: flex;
	gap: 16px;
	font-size: 12px;
	color: #9ca3af;
}

.document-item.uploaded {
	border-left-color: #10b981;
	background: #f0fdf4;
}

.document-info {
	display: flex;
	align-items: flex-start;
	gap: 12px;
	flex: 1;
}

.document-icon {
	font-size: 24px;
	color: #3b82f6;
}

.document-details h5 {
	margin: 0 0 4px 0;
	color: #1f2937;
	font-size: 14px;
	font-weight: 600;
}

.document-details p {
	margin: 0 0 8px 0;
	color: #6b7280;
	font-size: 13px;
}

.upload-info {
	display: flex;
	align-items: center;
	gap: 8px;
}

.upload-date {
	font-size: 12px;
	color: #9ca3af;
}

.notes-list {
	margin-bottom: 16px;
}

.note-item {
	padding: 12px;
	margin-bottom: 8px;
	background: #f9fafb;
	border-radius: 6px;
}

.note-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	margin-bottom: 8px;
}

.note-author {
	font-weight: 500;
	color: #1f2937;
	font-size: 14px;
}

.note-date {
	font-size: 12px;
	color: #9ca3af;
}

.note-content {
	color: #4b5563;
	font-size: 14px;
	line-height: 1.5;
}

.stage-actions {
	display: flex;
	justify-content: flex-end;
	gap: 12px;
	padding-top: 20px;
	border-top: 1px solid #e5e7eb;
}

@media (max-width: 768px) {
	.stage-header {
		flex-direction: column;
		gap: 16px;
	}

	.task-item,
	.document-item {
		flex-direction: column;
		gap: 12px;
	}

	.task-actions,
	.document-actions {
		align-self: flex-start;
	}
}
</style>
