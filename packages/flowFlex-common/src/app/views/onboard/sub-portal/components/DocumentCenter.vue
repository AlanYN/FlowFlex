<template>
	<div class="document-center">
		<!-- 文档中心头部 -->
		<div class="document-header">
			<div class="header-title">
				<h2>文档中心</h2>
				<p>管理您的入职相关文档</p>
			</div>
			<div class="document-actions">
				<el-upload
					ref="upload"
					:action="uploadUrl"
					:on-success="handleUploadSuccess"
					:on-error="handleUploadError"
					:before-upload="beforeUpload"
					:show-file-list="false"
					multiple
				>
					<el-button type="primary">
						<i class="el-icon-upload2"></i>
						上传文档
					</el-button>
				</el-upload>
			</div>
		</div>

		<!-- 文档统计 -->
		<div class="document-stats">
			<el-row :gutter="16">
				<el-col :span="6">
					<el-card>
						<div class="stat-content">
							<div class="stat-icon">
								<i class="el-icon-document" style="color: #409eff"></i>
							</div>
							<div class="stat-info">
								<div class="stat-value">{{ documentStats.total }}</div>
								<div class="stat-label">总文档数</div>
							</div>
						</div>
					</el-card>
				</el-col>
				<el-col :span="6">
					<el-card>
						<div class="stat-content">
							<div class="stat-icon">
								<i class="el-icon-check" style="color: #67c23a"></i>
							</div>
							<div class="stat-info">
								<div class="stat-value">{{ documentStats.approved }}</div>
								<div class="stat-label">已审批</div>
							</div>
						</div>
					</el-card>
				</el-col>
				<el-col :span="6">
					<el-card>
						<div class="stat-content">
							<div class="stat-icon">
								<i class="el-icon-time" style="color: #e6a23c"></i>
							</div>
							<div class="stat-info">
								<div class="stat-value">{{ documentStats.pending }}</div>
								<div class="stat-label">待审核</div>
							</div>
						</div>
					</el-card>
				</el-col>
				<el-col :span="6">
					<el-card>
						<div class="stat-content">
							<div class="stat-icon">
								<i class="el-icon-warning" style="color: #f56c6c"></i>
							</div>
							<div class="stat-info">
								<div class="stat-value">{{ documentStats.rejected }}</div>
								<div class="stat-label">被拒绝</div>
							</div>
						</div>
					</el-card>
				</el-col>
			</el-row>
		</div>

		<!-- 文档搜索和筛选 -->
		<div class="document-filters">
			<el-row :gutter="16">
				<el-col :span="8">
					<el-input
						v-model="searchQuery"
						placeholder="搜索文档名称..."
						prefix-icon="el-icon-search"
						clearable
					/>
				</el-col>
				<el-col :span="5">
					<el-select v-model="stageFilter" placeholder="阶段筛选" clearable>
						<el-option label="全部阶段" value="" />
						<el-option
							v-for="stage in stages"
							:key="stage.id"
							:label="stage.name"
							:value="stage.id"
						/>
					</el-select>
				</el-col>
				<el-col :span="5">
					<el-select v-model="statusFilter" placeholder="状态筛选" clearable>
						<el-option label="全部状态" value="" />
						<el-option label="已审批" value="approved" />
						<el-option label="审核中" value="under_review" />
						<el-option label="待审核" value="pending" />
						<el-option label="被拒绝" value="rejected" />
					</el-select>
				</el-col>
				<el-col :span="6">
					<el-select v-model="typeFilter" placeholder="文件类型" clearable>
						<el-option label="全部类型" value="" />
						<el-option label="PDF" value="pdf" />
						<el-option label="图片" value="image" />
						<el-option label="Excel" value="excel" />
						<el-option label="Word" value="word" />
					</el-select>
				</el-col>
			</el-row>
		</div>

		<!-- 文档列表 -->
		<div class="document-list">
			<el-row :gutter="16">
				<el-col
					v-for="document in filteredDocuments"
					:key="document.id"
					:span="12"
					class="document-col"
				>
					<el-card
						class="document-item"
						:class="{ rejected: document.status === 'rejected' }"
					>
						<div class="document-content">
							<div class="document-icon">
								<component :is="getFileIcon(document.type)" />
							</div>
							<div class="document-info">
								<div class="document-header-info">
									<h4 class="document-name">{{ document.name }}</h4>
									<div class="document-meta">
										<span class="document-size">{{ document.size }}</span>
										<span class="document-date">
											{{ formatDate(document.uploadedDate) }}
										</span>
									</div>
								</div>
								<div class="document-details">
									<div class="document-stage">
										<el-tag size="mini" type="info">
											{{ getStageNameById(document.stageId) }}
										</el-tag>
									</div>
									<div class="document-status">
										<el-tag :type="getStatusType(document.status)" size="mini">
											{{ getStatusText(document.status) }}
										</el-tag>
									</div>
								</div>
								<p class="document-description">{{ document.description }}</p>
							</div>
						</div>
						<div class="document-actions">
							<el-button
								size="mini"
								type="primary"
								@click="downloadDocument(document)"
							>
								<i class="el-icon-download"></i>
								下载
							</el-button>
							<el-button size="mini" @click="showPreview(document)">
								<i class="el-icon-view"></i>
								预览
							</el-button>
							<el-button
								v-if="document.status === 'rejected'"
								size="mini"
								type="warning"
								@click="reuploadDocument(document)"
							>
								<i class="el-icon-refresh"></i>
								重新上传
							</el-button>
							<el-popconfirm
								title="确定删除这个文档吗？"
								@confirm="deleteDocument(document.id)"
							>
								<template #reference>
									<el-button size="mini" type="danger">
										<i class="el-icon-delete"></i>
										删除
									</el-button>
								</template>
							</el-popconfirm>
						</div>
					</el-card>
				</el-col>
			</el-row>

			<!-- 空状态 -->
			<div v-if="filteredDocuments.length === 0" class="empty-state">
				<i class="el-icon-document"></i>
				<p>暂无文档</p>
				<el-button type="primary" @click="$refs.upload.$el.click()">
					上传第一个文档
				</el-button>
			</div>
		</div>

		<!-- 文档预览对话框 -->
		<el-dialog title="文档预览" v-model:visible="previewDialogVisible" width="80%" top="5vh">
			<div v-if="previewDocument" class="preview-content">
				<div v-if="previewDocument.type === 'image'" class="image-preview">
					<img
						:src="previewDocument.url"
						alt="文档预览"
						style="max-width: 100%; height: auto"
					/>
				</div>
				<div v-else-if="previewDocument.type === 'pdf'" class="pdf-preview">
					<iframe :src="previewDocument.url" width="100%" height="600px"></iframe>
				</div>
				<div v-else class="unsupported-preview">
					<i class="el-icon-document"></i>
					<p>此文件类型不支持预览，请下载后查看</p>
					<el-button type="primary" @click="downloadDocument(previewDocument)">
						<i class="el-icon-download"></i>
						下载文档
					</el-button>
				</div>
			</div>
		</el-dialog>

		<!-- 文档上传进度 -->
		<el-dialog title="文档上传" v-model:visible="uploadDialogVisible" width="500px">
			<div class="upload-progress">
				<div v-for="file in uploadingFiles" :key="file.name" class="upload-item">
					<div class="upload-info">
						<span class="file-name">{{ file.name }}</span>
						<span class="file-size">{{ file.size }}</span>
					</div>
					<el-progress :percentage="file.progress" :status="file.status" />
				</div>
			</div>
		</el-dialog>
	</div>
</template>

<script>
export default {
	name: 'DocumentCenter',
	props: {
		documents: {
			type: Array,
			default: () => [],
		},
		stages: {
			type: Array,
			default: () => [],
		},
	},
	data() {
		return {
			searchQuery: '',
			stageFilter: '',
			statusFilter: '',
			typeFilter: '',
			previewDialogVisible: false,
			uploadDialogVisible: false,
			previewDocument: null,
			uploadingFiles: [],
			uploadUrl: '/api/documents/upload', // 实际的上传接口
		};
	},
	computed: {
		documentStats() {
			return {
				total: this.documents.length,
				approved: this.documents.filter((doc) => doc.status === 'approved').length,
				pending: this.documents.filter(
					(doc) => doc.status === 'pending' || doc.status === 'under_review'
				).length,
				rejected: this.documents.filter((doc) => doc.status === 'rejected').length,
			};
		},
		filteredDocuments() {
			return this.documents.filter((document) => {
				const matchesSearch =
					!this.searchQuery ||
					document.name.toLowerCase().includes(this.searchQuery.toLowerCase()) ||
					document.description.toLowerCase().includes(this.searchQuery.toLowerCase());

				const matchesStage = !this.stageFilter || document.stageId === this.stageFilter;
				const matchesStatus = !this.statusFilter || document.status === this.statusFilter;
				const matchesType = !this.typeFilter || document.type === this.typeFilter;

				return matchesSearch && matchesStage && matchesStatus && matchesType;
			});
		},
	},
	methods: {
		getFileIcon(type) {
			const iconMap = {
				pdf: 'el-icon-document',
				image: 'el-icon-picture',
				excel: 'el-icon-s-grid',
				word: 'el-icon-document-copy',
			};
			return iconMap[type] || 'el-icon-document';
		},
		getStatusType(status) {
			const typeMap = {
				approved: 'success',
				under_review: 'warning',
				pending: 'info',
				rejected: 'danger',
			};
			return typeMap[status] || 'info';
		},
		getStatusText(status) {
			const textMap = {
				approved: '已审批',
				under_review: '审核中',
				pending: '待审核',
				rejected: '被拒绝',
			};
			return textMap[status] || '未知';
		},
		getStageNameById(stageId) {
			const stage = this.stages.find((s) => s.id === stageId);
			return stage ? stage.name : '未知阶段';
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
		downloadDocument(document) {
			this.$emit('download-document', document.id);
			this.$message.success('开始下载文档');
		},
		showPreview(document) {
			this.previewDocument = document;
			this.previewDialogVisible = true;
		},
		deleteDocument(documentId) {
			this.$emit('delete-document', documentId);
			this.$message.success('文档删除成功');
		},
		reuploadDocument(document) {
			// 触发重新上传
			this.$refs.upload.$el.click();
		},
		beforeUpload(file) {
			const isValidType = [
				'image/jpeg',
				'image/png',
				'application/pdf',
				'application/vnd.ms-excel',
			].includes(file.type);
			const isLt10M = file.size / 1024 / 1024 < 10;

			if (!isValidType) {
				this.$message.error('只能上传 JPG/PNG/PDF/Excel 格式的文件!');
				return false;
			}
			if (!isLt10M) {
				this.$message.error('上传文件大小不能超过 10MB!');
				return false;
			}

			// 添加到上传列表
			this.uploadingFiles.push({
				name: file.name,
				size: this.formatFileSize(file.size),
				progress: 0,
				status: 'active',
			});
			this.uploadDialogVisible = true;

			return true;
		},
		handleUploadSuccess(response, file) {
			const uploadingFile = this.uploadingFiles.find((f) => f.name === file.name);
			if (uploadingFile) {
				uploadingFile.progress = 100;
				uploadingFile.status = 'success';
			}

			this.$emit('upload-document', response);
			this.$message.success('文档上传成功');

			// 延迟关闭对话框
			setTimeout(() => {
				this.uploadDialogVisible = false;
				this.uploadingFiles = [];
			}, 1000);
		},
		handleUploadError(error, file) {
			const uploadingFile = this.uploadingFiles.find((f) => f.name === file.name);
			if (uploadingFile) {
				uploadingFile.status = 'exception';
			}
			this.$message.error('文档上传失败');
		},
		formatFileSize(bytes) {
			if (bytes === 0) return '0 Bytes';
			const k = 1024;
			const sizes = ['Bytes', 'KB', 'MB', 'GB'];
			const i = Math.floor(Math.log(bytes) / Math.log(k));
			return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
		},
	},
};
</script>

<style scoped>
.document-center {
	padding: 20px;
}

.document-header {
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

.document-stats {
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

.document-filters {
	margin-bottom: 24px;
}

.document-list {
	min-height: 400px;
}

.document-col {
	margin-bottom: 16px;
}

.document-item {
	height: 200px;
	transition: all 0.3s ease;
}

.document-item:hover {
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.document-item.rejected {
	border-left: 4px solid #f56c6c;
}

.document-content {
	display: flex;
	height: 120px;
	gap: 16px;
}

.document-icon {
	font-size: 48px;
	color: #409eff;
	display: flex;
	align-items: center;
	justify-content: center;
	width: 60px;
	flex-shrink: 0;
}

.document-info {
	flex: 1;
	display: flex;
	flex-direction: column;
	justify-content: space-between;
}

.document-header-info {
	margin-bottom: 8px;
}

.document-name {
	margin: 0 0 4px 0;
	font-size: 16px;
	font-weight: 600;
	color: #1f2937;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.document-meta {
	display: flex;
	gap: 12px;
	font-size: 12px;
	color: #6b7280;
}

.document-details {
	display: flex;
	gap: 8px;
	margin-bottom: 8px;
}

.document-description {
	margin: 0;
	font-size: 12px;
	color: #4b5563;
	line-height: 1.4;
	display: -webkit-box;
	-webkit-line-clamp: 2;
	-webkit-box-orient: vertical;
	overflow: hidden;
}

.document-actions {
	display: flex;
	gap: 8px;
	justify-content: flex-end;
	flex-wrap: wrap;
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

.preview-content {
	text-align: center;
}

.unsupported-preview {
	padding: 40px;
}

.unsupported-preview i {
	font-size: 48px;
	color: #9ca3af;
	margin-bottom: 16px;
	display: block;
}

.upload-progress {
	padding: 20px;
}

.upload-item {
	margin-bottom: 16px;
}

.upload-info {
	display: flex;
	justify-content: space-between;
	align-items: center;
	margin-bottom: 8px;
}

.file-name {
	font-weight: 500;
	color: #1f2937;
}

.file-size {
	font-size: 12px;
	color: #6b7280;
}

@media (max-width: 768px) {
	.document-header {
		flex-direction: column;
		gap: 16px;
	}

	.document-col {
		span: 24;
	}

	.document-actions {
		justify-content: flex-start;
	}
}
</style>
