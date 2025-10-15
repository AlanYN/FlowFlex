<template>
	<div class="customer-questionnaire">
		<!-- 问卷头部 -->
		<div class="questionnaire-header">
			<div class="header-title">
				<h2>客户问卷调查</h2>
				<p>请完成以下问卷，帮助我们更好地为您服务</p>
			</div>
			<div class="progress-info">
				<div class="progress-text">
					完成进度: {{ completedQuestions }}/{{ totalQuestions }}
				</div>
				<el-progress
					:percentage="progressPercentage"
					:stroke-width="8"
					:text-inside="true"
				/>
			</div>
		</div>

		<!-- 问卷表单 -->
		<div class="questionnaire-form">
			<el-form
				ref="questionnaireForm"
				:model="formData"
				:rules="formRules"
				label-width="200px"
				label-position="left"
			>
				<!-- 基本信息部分 -->
				<div class="form-section">
					<div class="section-header">
						<h3>基本信息</h3>
						<p>请提供您的基本业务信息</p>
					</div>

					<el-form-item label="公司规模" prop="companySize">
						<el-radio-group v-model="formData.companySize">
							<el-radio label="small">小型企业 (1-50人)</el-radio>
							<el-radio label="medium">中型企业 (51-200人)</el-radio>
							<el-radio label="large">大型企业 (200人以上)</el-radio>
						</el-radio-group>
					</el-form-item>

					<el-form-item label="行业类型" prop="industry">
						<el-select v-model="formData.industry" placeholder="请选择行业类型">
							<el-option label="制造业" value="manufacturing" />
							<el-option label="零售业" value="retail" />
							<el-option label="物流运输" value="logistics" />
							<el-option label="电子商务" value="ecommerce" />
							<el-option label="医疗健康" value="healthcare" />
							<el-option label="教育培训" value="education" />
							<el-option label="金融服务" value="finance" />
							<el-option label="其他" value="other" />
						</el-select>
					</el-form-item>

					<el-form-item label="年营业额" prop="annualRevenue">
						<el-select
							v-model="formData.annualRevenue"
							placeholder="请选择年营业额范围"
						>
							<el-option label="100万以下" value="under_1m" />
							<el-option label="100万-500万" value="1m_5m" />
							<el-option label="500万-1000万" value="5m_10m" />
							<el-option label="1000万-5000万" value="10m_50m" />
							<el-option label="5000万以上" value="over_50m" />
						</el-select>
					</el-form-item>

					<el-form-item label="主要业务地区" prop="businessRegion">
						<el-checkbox-group v-model="formData.businessRegion">
							<el-checkbox label="华北地区">华北地区</el-checkbox>
							<el-checkbox label="华东地区">华东地区</el-checkbox>
							<el-checkbox label="华南地区">华南地区</el-checkbox>
							<el-checkbox label="华中地区">华中地区</el-checkbox>
							<el-checkbox label="西南地区">西南地区</el-checkbox>
							<el-checkbox label="西北地区">西北地区</el-checkbox>
							<el-checkbox label="东北地区">东北地区</el-checkbox>
							<el-checkbox label="海外市场">海外市场</el-checkbox>
						</el-checkbox-group>
					</el-form-item>
				</div>

				<!-- 业务需求部分 -->
				<div class="form-section">
					<div class="section-header">
						<h3>业务需求</h3>
						<p>请详细描述您的业务需求和期望</p>
					</div>

					<el-form-item label="主要产品类型" prop="productTypes">
						<el-checkbox-group v-model="formData.productTypes">
							<el-checkbox label="实体商品">实体商品</el-checkbox>
							<el-checkbox label="数字产品">数字产品</el-checkbox>
							<el-checkbox label="服务类产品">服务类产品</el-checkbox>
							<el-checkbox label="定制化产品">定制化产品</el-checkbox>
						</el-checkbox-group>
					</el-form-item>

					<el-form-item label="月订单量预估" prop="monthlyOrders">
						<el-select
							v-model="formData.monthlyOrders"
							placeholder="请选择月订单量范围"
						>
							<el-option label="100单以下" value="under_100" />
							<el-option label="100-500单" value="100_500" />
							<el-option label="500-1000单" value="500_1000" />
							<el-option label="1000-5000单" value="1000_5000" />
							<el-option label="5000单以上" value="over_5000" />
						</el-select>
					</el-form-item>

					<el-form-item label="仓储情况" prop="warehousing">
						<el-radio-group v-model="formData.warehousing">
							<el-radio label="self_managed">自有仓库</el-radio>
							<el-radio label="third_party">第三方仓储</el-radio>
							<el-radio label="mixed">混合模式</el-radio>
							<el-radio label="none">暂无仓储</el-radio>
						</el-radio-group>
					</el-form-item>

					<el-form-item label="物流配送要求" prop="logistics">
						<el-checkbox-group v-model="formData.logistics">
							<el-checkbox label="当日达">当日达</el-checkbox>
							<el-checkbox label="次日达">次日达</el-checkbox>
							<el-checkbox label="标准配送">标准配送 (2-3天)</el-checkbox>
							<el-checkbox label="经济配送">经济配送 (3-5天)</el-checkbox>
							<el-checkbox label="冷链配送">冷链配送</el-checkbox>
							<el-checkbox label="大件配送">大件配送</el-checkbox>
						</el-checkbox-group>
					</el-form-item>

					<el-form-item label="特殊需求说明" prop="specialRequirements">
						<el-input
							v-model="formData.specialRequirements"
							type="textarea"
							:rows="4"
							placeholder="请详细描述您的特殊需求或要求"
						/>
					</el-form-item>
				</div>

				<!-- 技术集成部分 -->
				<div class="form-section">
					<div class="section-header">
						<h3>技术集成</h3>
						<p>请提供您的技术环境和集成需求</p>
					</div>

					<el-form-item label="现有系统" prop="existingSystems">
						<el-checkbox-group v-model="formData.existingSystems">
							<el-checkbox label="ERP系统">ERP系统</el-checkbox>
							<el-checkbox label="CRM系统">CRM系统</el-checkbox>
							<el-checkbox label="WMS系统">WMS系统</el-checkbox>
							<el-checkbox label="电商平台">电商平台</el-checkbox>
							<el-checkbox label="财务系统">财务系统</el-checkbox>
							<el-checkbox label="OA系统">OA系统</el-checkbox>
						</el-checkbox-group>
					</el-form-item>

					<el-form-item label="集成方式偏好" prop="integrationPreference">
						<el-radio-group v-model="formData.integrationPreference">
							<el-radio label="api">API接口</el-radio>
							<el-radio label="file">文件传输</el-radio>
							<el-radio label="manual">手工操作</el-radio>
							<el-radio label="custom">定制开发</el-radio>
						</el-radio-group>
					</el-form-item>

					<el-form-item label="技术团队规模" prop="techTeamSize">
						<el-select v-model="formData.techTeamSize" placeholder="请选择技术团队规模">
							<el-option label="无技术团队" value="none" />
							<el-option label="1-3人" value="small" />
							<el-option label="4-10人" value="medium" />
							<el-option label="10人以上" value="large" />
						</el-select>
					</el-form-item>

					<el-form-item label="预期上线时间" prop="expectedGoLive">
						<el-date-picker
							v-model="formData.expectedGoLive"
							type="date"
							placeholder="选择预期上线日期"
							format="yyyy-MM-dd"
							value-format="yyyy-MM-dd"
						/>
					</el-form-item>
				</div>

				<!-- 服务期望部分 -->
				<div class="form-section">
					<div class="section-header">
						<h3>服务期望</h3>
						<p>请告诉我们您对服务的期望和要求</p>
					</div>

					<el-form-item label="服务优先级" prop="servicePriority">
						<el-rate
							v-model="formData.servicePriority.stability"
							:texts="['很差', '较差', '一般', '良好', '优秀']"
							show-text
						/>
						<span class="priority-label">系统稳定性</span>
					</el-form-item>

					<el-form-item label="" prop="servicePriority">
						<el-rate
							v-model="formData.servicePriority.speed"
							:texts="['很差', '较差', '一般', '良好', '优秀']"
							show-text
						/>
						<span class="priority-label">响应速度</span>
					</el-form-item>

					<el-form-item label="" prop="servicePriority">
						<el-rate
							v-model="formData.servicePriority.support"
							:texts="['很差', '较差', '一般', '良好', '优秀']"
							show-text
						/>
						<span class="priority-label">技术支持</span>
					</el-form-item>

					<el-form-item label="培训需求" prop="trainingNeeds">
						<el-checkbox-group v-model="formData.trainingNeeds">
							<el-checkbox label="系统操作培训">系统操作培训</el-checkbox>
							<el-checkbox label="管理员培训">管理员培训</el-checkbox>
							<el-checkbox label="业务流程培训">业务流程培训</el-checkbox>
							<el-checkbox label="故障排除培训">故障排除培训</el-checkbox>
						</el-checkbox-group>
					</el-form-item>

					<el-form-item label="其他建议或要求" prop="additionalComments">
						<el-input
							v-model="formData.additionalComments"
							type="textarea"
							:rows="4"
							placeholder="请提供任何其他建议、要求或关注点"
						/>
					</el-form-item>
				</div>

				<!-- 表单操作按钮 -->
				<div class="form-actions">
					<el-button @click="saveDraft" :loading="saving">
						<i class="el-icon-document"></i>
						保存草稿
					</el-button>
					<el-button type="primary" @click="submitQuestionnaire" :loading="submitting">
						<i class="el-icon-check"></i>
						提交问卷
					</el-button>
				</div>
			</el-form>
		</div>
	</div>
</template>

<script>
export default {
	name: 'CustomerQuestionnaire',
	props: {
		questionnaireData: {
			type: Object,
			default: () => null,
		},
	},
	data() {
		return {
			saving: false,
			submitting: false,
			formData: {
				companySize: '',
				industry: '',
				annualRevenue: '',
				businessRegion: [],
				productTypes: [],
				monthlyOrders: '',
				warehousing: '',
				logistics: [],
				specialRequirements: '',
				existingSystems: [],
				integrationPreference: '',
				techTeamSize: '',
				expectedGoLive: '',
				servicePriority: {
					stability: 0,
					speed: 0,
					support: 0,
				},
				trainingNeeds: [],
				additionalComments: '',
			},
			formRules: {
				companySize: [{ required: true, message: '请选择公司规模', trigger: 'change' }],
				industry: [{ required: true, message: '请选择行业类型', trigger: 'change' }],
				annualRevenue: [
					{ required: true, message: '请选择年营业额范围', trigger: 'change' },
				],
				businessRegion: [
					{
						required: true,
						type: 'array',
						min: 1,
						message: '请至少选择一个业务地区',
						trigger: 'change',
					},
				],
				productTypes: [
					{
						required: true,
						type: 'array',
						min: 1,
						message: '请至少选择一种产品类型',
						trigger: 'change',
					},
				],
				monthlyOrders: [
					{ required: true, message: '请选择月订单量范围', trigger: 'change' },
				],
				warehousing: [{ required: true, message: '请选择仓储情况', trigger: 'change' }],
				integrationPreference: [
					{ required: true, message: '请选择集成方式偏好', trigger: 'change' },
				],
				techTeamSize: [
					{ required: true, message: '请选择技术团队规模', trigger: 'change' },
				],
			},
		};
	},
	computed: {
		totalQuestions() {
			// 计算总问题数量
			return 15;
		},
		completedQuestions() {
			// 计算已完成的问题数量
			let completed = 0;
			if (this.formData.companySize) completed++;
			if (this.formData.industry) completed++;
			if (this.formData.annualRevenue) completed++;
			if (this.formData.businessRegion.length > 0) completed++;
			if (this.formData.productTypes.length > 0) completed++;
			if (this.formData.monthlyOrders) completed++;
			if (this.formData.warehousing) completed++;
			if (this.formData.logistics.length > 0) completed++;
			if (this.formData.existingSystems.length > 0) completed++;
			if (this.formData.integrationPreference) completed++;
			if (this.formData.techTeamSize) completed++;
			if (this.formData.expectedGoLive) completed++;
			if (this.formData.servicePriority.stability > 0) completed++;
			if (this.formData.servicePriority.speed > 0) completed++;
			if (this.formData.servicePriority.support > 0) completed++;
			return completed;
		},
		progressPercentage() {
			return Math.round((this.completedQuestions / this.totalQuestions) * 100);
		},
	},
	created() {
		this.loadQuestionnaireData();
	},
	methods: {
		loadQuestionnaireData() {
			if (this.questionnaireData) {
				this.formData = { ...this.formData, ...this.questionnaireData };
			}
		},
		saveDraft() {
			this.saving = true;
			// 模拟保存延迟
			setTimeout(() => {
				this.saving = false;
				this.$message.success('草稿保存成功');
				this.$emit('save-draft', this.formData);
			}, 1000);
		},
		submitQuestionnaire() {
			this.$refs.questionnaireForm.validate((valid) => {
				if (valid) {
					this.submitting = true;
					// 模拟提交延迟
					setTimeout(() => {
						this.submitting = false;
						this.$message.success('问卷提交成功');
						this.$emit('submit-questionnaire', this.formData);
					}, 1500);
				} else {
					this.$message.error('请完成必填项后再提交');
					return false;
				}
			});
		},
	},
};
</script>

<style scoped lang="scss">
.customer-questionnaire {
	padding: 20px;
}

.questionnaire-header {
	display: flex;
	justify-content: space-between;
	align-items: flex-start;
	margin-bottom: 32px;
	padding-bottom: 24px;
	border-bottom: 1px solid var(--el-border-color-light);
}

.header-title h2 {
	margin: 0 0 8px 0;
	color: var(--el-text-color-primary);
	font-size: 24px;
	font-weight: 600;
}

.header-title p {
	margin: 0;
	color: var(--el-text-color-secondary);
	font-size: 14px;
}

.progress-info {
	min-width: 300px;
	text-align: right;
}

.progress-text {
	font-size: 14px;
	color: var(--el-text-color-regular);
	margin-bottom: 8px;
}

.questionnaire-form {
	max-width: 800px;
}

.form-section {
	margin-bottom: 40px;
	padding: 24px;
	background: var(--el-fill-color-blank);
	border-left: 4px solid var(--el-color-primary);
	@apply rounded-xl;
}

.section-header {
	margin-bottom: 24px;
}

.section-header h3 {
	margin: 0 0 8px 0;
	color: var(--el-text-color-primary);
	font-size: 18px;
	font-weight: 600;
}

.section-header p {
	margin: 0;
	color: var(--el-text-color-secondary);
	font-size: 14px;
}

.priority-label {
	margin-left: 16px;
	font-size: 14px;
	color: var(--el-text-color-regular);
}

.form-actions {
	text-align: center;
	padding: 32px 0;
	border-top: 1px solid var(--el-border-color-lighter);
	margin-top: 32px;
}

.form-actions .el-button {
	margin: 0 8px;
	padding: 12px 24px;
}

/* 响应式设计 */
@media (max-width: 768px) {
	.questionnaire-header {
		flex-direction: column;
		gap: 16px;
	}

	.progress-info {
		min-width: auto;
		width: 100%;
		text-align: left;
	}

	.form-section {
		padding: 16px;
	}

	.questionnaire-form .el-form-item {
		margin-bottom: 20px;
	}
}

/* 自定义样式 */
.el-form-item__label {
	font-weight: 500;
	color: var(--el-text-color-regular);
}

.el-checkbox-group .el-checkbox {
	margin-right: 20px;
	margin-bottom: 8px;
}

.el-radio-group .el-radio {
	margin-right: 20px;
	margin-bottom: 8px;
}

.el-rate {
	margin-bottom: 16px;
}

.el-textarea .el-textarea__inner {
	min-height: 100px;
}
</style>
