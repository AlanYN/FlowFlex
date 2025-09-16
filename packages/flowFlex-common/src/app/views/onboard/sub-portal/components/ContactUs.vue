<template>
	<div class="contact-us">
		<!-- 联系我们头部 -->
		<div class="contact-header">
			<div class="header-title">
				<h2>联系我们</h2>
				<p>我们的专业团队随时为您提供帮助</p>
			</div>
		</div>

		<!-- 团队成员展示 -->
		<div class="team-section">
			<h3>您的专属服务团队</h3>
			<el-row :gutter="20">
				<el-col
					v-for="member in teamMembers"
					:key="member.id"
					:span="8"
					class="team-member-col"
				>
					<el-card class="team-member-card">
						<div class="member-info">
							<el-avatar :size="60" :src="member.avatar">
								<i class="el-icon-user-solid"></i>
							</el-avatar>
							<div class="member-details">
								<h4>{{ member.name }}</h4>
								<p class="member-role">{{ member.role }}</p>
								<p class="member-department">{{ member.department }}</p>
								<div class="member-status">
									<el-tag
										:type="member.available ? 'success' : 'info'"
										size="mini"
									>
										{{ member.available ? '在线' : '离线' }}
									</el-tag>
								</div>
							</div>
						</div>
						<div class="member-contact">
							<el-button size="small" type="primary" @click="contactMember(member)">
								<i class="el-icon-message"></i>
								发送消息
							</el-button>
							<el-button v-if="member.phone" size="small" @click="callMember(member)">
								<i class="el-icon-phone"></i>
								电话联系
							</el-button>
						</div>
						<div class="member-specialties">
							<div class="specialties-title">专业领域:</div>
							<el-tag
								v-for="specialty in member.specialties"
								:key="specialty"
								size="mini"
								class="specialty-tag"
							>
								{{ specialty }}
							</el-tag>
						</div>
					</el-card>
				</el-col>
			</el-row>
		</div>

		<!-- 快速联系表单 -->
		<div class="quick-contact-section">
			<h3>快速联系</h3>
			<el-row :gutter="20">
				<el-col :span="12">
					<el-card>
						<template #header>
							<div>
								<span>发送消息给团队</span>
							</div>
						</template>
						<el-form
							ref="contactForm"
							:model="contactForm"
							:rules="contactRules"
							label-width="100px"
						>
							<el-form-item label="联系对象" prop="recipient">
								<el-select
									v-model="contactForm.recipient"
									placeholder="选择联系对象"
									style="width: 100%"
								>
									<el-option-group label="团队成员">
										<el-option
											v-for="member in availableMembers"
											:key="member.id"
											:label="member.name"
											:value="member.id"
										>
											<span style="float: left">{{ member.name }}</span>
											<span
												style="
													float: right;
													color: #8492a6;
													font-size: 13px;
												"
											>
												{{ member.role }}
											</span>
										</el-option>
									</el-option-group>
									<el-option-group label="部门团队">
										<el-option label="销售团队" value="sales_team" />
										<el-option label="实施团队" value="implementation_team" />
										<el-option label="技术支持" value="tech_support" />
										<el-option label="客户服务" value="customer_service" />
									</el-option-group>
								</el-select>
							</el-form-item>
							<el-form-item label="问题类型" prop="issueType">
								<el-select
									v-model="contactForm.issueType"
									placeholder="选择问题类型"
									style="width: 100%"
								>
									<el-option label="一般咨询" value="general" />
									<el-option label="技术问题" value="technical" />
									<el-option label="账单问题" value="billing" />
									<el-option label="服务投诉" value="complaint" />
									<el-option label="功能建议" value="suggestion" />
									<el-option label="紧急问题" value="urgent" />
								</el-select>
							</el-form-item>
							<el-form-item label="优先级" prop="priority">
								<el-radio-group v-model="contactForm.priority">
									<el-radio label="low">低</el-radio>
									<el-radio label="normal">普通</el-radio>
									<el-radio label="high">高</el-radio>
									<el-radio label="urgent">紧急</el-radio>
								</el-radio-group>
							</el-form-item>
							<el-form-item label="主题" prop="subject">
								<el-input
									v-model="contactForm.subject"
									placeholder="请输入问题主题"
								/>
							</el-form-item>
							<el-form-item label="详细描述" prop="message">
								<el-input
									v-model="contactForm.message"
									type="textarea"
									:rows="6"
									placeholder="请详细描述您的问题或需求"
								/>
							</el-form-item>
							<el-form-item>
								<el-button type="primary" @click="sendContact" :loading="sending">
									<i class="el-icon-send"></i>
									发送消息
								</el-button>
								<el-button @click="resetForm">重置</el-button>
							</el-form-item>
						</el-form>
					</el-card>
				</el-col>
				<el-col :span="12">
					<el-card>
						<template #header>
							<div>
								<span>联系方式</span>
							</div>
						</template>
						<div class="contact-methods">
							<div class="contact-item">
								<div class="contact-icon">
									<i class="el-icon-phone"></i>
								</div>
								<div class="contact-details">
									<h4>客服热线</h4>
									<p>400-888-9999</p>
									<span class="contact-time">工作时间: 9:00-18:00</span>
								</div>
							</div>
							<div class="contact-item">
								<div class="contact-icon">
									<i class="el-icon-message"></i>
								</div>
								<div class="contact-details">
									<h4>在线客服</h4>
									<p>即时消息支持</p>
									<span class="contact-time">7×24小时在线</span>
								</div>
							</div>
							<div class="contact-item">
								<div class="contact-icon">
									<i class="el-icon-postcard"></i>
								</div>
								<div class="contact-details">
									<h4>邮箱支持</h4>
									<p>support@company.com</p>
									<span class="contact-time">24小时内回复</span>
								</div>
							</div>
							<div class="contact-item">
								<div class="contact-icon">
									<i class="el-icon-location"></i>
								</div>
								<div class="contact-details">
									<h4>公司地址</h4>
									<p>北京市朝阳区xxx大厦</p>
									<span class="contact-time">欢迎预约拜访</span>
								</div>
							</div>
						</div>

						<!-- 常见问题快速链接 -->
						<div class="faq-section">
							<h4>常见问题</h4>
							<div class="faq-links">
								<el-link
									v-for="faq in faqs"
									:key="faq.id"
									:href="faq.link"
									target="_blank"
									class="faq-link"
								>
									{{ faq.title }}
								</el-link>
							</div>
						</div>
					</el-card>
				</el-col>
			</el-row>
		</div>

		<!-- 服务承诺 -->
		<div class="service-commitment">
			<h3>我们的服务承诺</h3>
			<el-row :gutter="20">
				<el-col :span="6">
					<div class="commitment-item">
						<div class="commitment-icon">
							<i class="el-icon-time"></i>
						</div>
						<h4>快速响应</h4>
						<p>工作时间内1小时内响应，紧急问题15分钟内响应</p>
					</div>
				</el-col>
				<el-col :span="6">
					<div class="commitment-item">
						<div class="commitment-icon">
							<i class="el-icon-user"></i>
						</div>
						<h4>专业团队</h4>
						<p>经验丰富的专业团队，提供高质量的技术支持</p>
					</div>
				</el-col>
				<el-col :span="6">
					<div class="commitment-item">
						<div class="commitment-icon">
							<i class="el-icon-shield"></i>
						</div>
						<h4>安全保障</h4>
						<p>严格保护客户信息安全，确保数据隐私</p>
					</div>
				</el-col>
				<el-col :span="6">
					<div class="commitment-item">
						<div class="commitment-icon">
							<i class="el-icon-star-on"></i>
						</div>
						<h4>持续改进</h4>
						<p>持续优化服务质量，不断提升客户满意度</p>
					</div>
				</el-col>
			</el-row>
		</div>
	</div>
</template>

<script>
export default {
	name: 'ContactUs',
	props: {
		teamMembers: {
			type: Array,
			default: () => [],
		},
		customerData: {
			type: Object,
			required: true,
		},
	},
	data() {
		return {
			sending: false,
			contactForm: {
				recipient: '',
				issueType: '',
				priority: 'normal',
				subject: '',
				message: '',
			},
			contactRules: {
				recipient: [{ required: true, message: '请选择联系对象', trigger: 'change' }],
				issueType: [{ required: true, message: '请选择问题类型', trigger: 'change' }],
				subject: [{ required: true, message: '请输入问题主题', trigger: 'blur' }],
				message: [{ required: true, message: '请输入详细描述', trigger: 'blur' }],
			},
			faqs: [
				{ id: 1, title: '如何重置密码？', link: '/faq/reset-password' },
				{ id: 2, title: '系统使用指南', link: '/faq/user-guide' },
				{ id: 3, title: '常见技术问题', link: '/faq/technical-issues' },
				{ id: 4, title: '账单相关问题', link: '/faq/billing' },
				{ id: 5, title: '服务条款说明', link: '/faq/terms' },
			],
		};
	},
	computed: {
		availableMembers() {
			return this.teamMembers.filter((member) => member.available);
		},
	},
	methods: {
		contactMember(member) {
			this.contactForm.recipient = member.id;
			this.contactForm.subject = `咨询 - ${member.name}`;
			// 滚动到联系表单
			this.$nextTick(() => {
				const formElement = this.$refs.contactForm.$el;
				formElement.scrollIntoView({ behavior: 'smooth' });
			});
		},
		callMember(member) {
			if (member.phone) {
				// 在实际应用中，这里可能会打开电话应用或显示电话号码
				this.$alert(`请拨打电话: ${member.phone}`, `联系 ${member.name}`, {
					confirmButtonText: '确定',
					type: 'info',
				});
			}
		},
		sendContact() {
			this.$refs.contactForm.validate((valid) => {
				if (valid) {
					this.sending = true;
					// 模拟发送延迟
					setTimeout(() => {
						this.sending = false;
						this.$message.success('消息发送成功，我们会尽快回复您');
						this.$emit('send-contact', {
							...this.contactForm,
							customerData: this.customerData,
							timestamp: new Date().toISOString(),
						});
						this.resetForm();
					}, 1000);
				} else {
					this.$message.error('请完善表单信息');
					return false;
				}
			});
		},
		resetForm() {
			this.$refs.contactForm.resetFields();
			this.contactForm = {
				recipient: '',
				issueType: '',
				priority: 'normal',
				subject: '',
				message: '',
			};
		},
	},
};
</script>

<style scoped lang="scss">
.contact-us {
	padding: 20px;
}

.contact-header {
	margin-bottom: 32px;
	text-align: center;
}

.contact-header h2 {
	margin: 0 0 8px 0;
	color: #1f2937;
	font-size: 28px;
	font-weight: 600;
}

.contact-header p {
	margin: 0;
	color: #6b7280;
	font-size: 16px;
}

.team-section {
	margin-bottom: 40px;
}

.team-section h3 {
	margin: 0 0 24px 0;
	color: #1f2937;
	font-size: 20px;
	font-weight: 600;
}

.team-member-col {
	margin-bottom: 20px;
}

.team-member-card {
	height: 280px;
	transition: all 0.3s ease;
}

.team-member-card:hover {
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
	transform: translateY(-2px);
}

.member-info {
	display: flex;
	flex-direction: column;
	align-items: center;
	text-align: center;
	margin-bottom: 16px;
}

.member-details {
	margin-top: 12px;
}

.member-details h4 {
	margin: 0 0 4px 0;
	color: #1f2937;
	font-size: 16px;
	font-weight: 600;
}

.member-role {
	margin: 0 0 4px 0;
	color: #3b82f6;
	font-size: 14px;
	font-weight: 500;
}

.member-department {
	margin: 0 0 8px 0;
	color: #6b7280;
	font-size: 12px;
}

.member-status {
	margin-bottom: 16px;
}

.member-contact {
	display: flex;
	gap: 8px;
	justify-content: center;
	margin-bottom: 16px;
}

.member-specialties {
	text-align: center;
}

.specialties-title {
	font-size: 12px;
	color: #6b7280;
	margin-bottom: 8px;
}

.specialty-tag {
	margin: 2px;
}

.quick-contact-section {
	margin-bottom: 40px;
}

.quick-contact-section h3 {
	margin: 0 0 24px 0;
	color: #1f2937;
	font-size: 20px;
	font-weight: 600;
}

.contact-methods {
	margin-bottom: 24px;
}

.contact-item {
	display: flex;
	align-items: flex-start;
	margin-bottom: 20px;
	padding: 16px;
	background: #f9fafb;
	@apply rounded-xl;
}

.contact-icon {
	width: 40px;
	height: 40px;
	background: #3b82f6;
	border-radius: 50%;
	display: flex;
	align-items: center;
	justify-content: center;
	color: white;
	font-size: 18px;
	margin-right: 16px;
	flex-shrink: 0;
}

.contact-details h4 {
	margin: 0 0 4px 0;
	color: #1f2937;
	font-size: 16px;
	font-weight: 600;
}

.contact-details p {
	margin: 0 0 4px 0;
	color: #374151;
	font-size: 14px;
}

.contact-time {
	color: #6b7280;
	font-size: 12px;
}

.faq-section {
	border-top: 1px solid #e5e7eb;
	padding-top: 20px;
}

.faq-section h4 {
	margin: 0 0 16px 0;
	color: #1f2937;
	font-size: 16px;
	font-weight: 600;
}

.faq-links {
	display: flex;
	flex-direction: column;
	gap: 8px;
}

.faq-link {
	font-size: 14px;
}

.service-commitment {
	background: #f9fafb;
	padding: 32px;
	@apply rounded-xl;
}

.service-commitment h3 {
	margin: 0 0 32px 0;
	color: #1f2937;
	font-size: 20px;
	font-weight: 600;
	text-align: center;
}

.commitment-item {
	text-align: center;
	padding: 20px;
}

.commitment-icon {
	width: 60px;
	height: 60px;
	background: #3b82f6;
	border-radius: 50%;
	display: flex;
	align-items: center;
	justify-content: center;
	color: white;
	font-size: 24px;
	margin: 0 auto 16px auto;
}

.commitment-item h4 {
	margin: 0 0 12px 0;
	color: #1f2937;
	font-size: 16px;
	font-weight: 600;
}

.commitment-item p {
	margin: 0;
	color: #6b7280;
	font-size: 14px;
	line-height: 1.5;
}

@media (max-width: 768px) {
	.team-member-col {
		span: 24;
	}

	.quick-contact-section .el-col {
		span: 24;
		margin-bottom: 20px;
	}

	.service-commitment .el-col {
		span: 12;
		margin-bottom: 20px;
	}
}

@media (max-width: 480px) {
	.service-commitment .el-col {
		span: 24;
	}
}
</style>
