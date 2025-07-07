<template>
	<div class="min-h-screen bg-gray-50 dark:bg-black-400">
		<div class="container mx-auto py-6 px-4" v-loading="loading">
			<!-- Header -->
			<div class="flex justify-between items-center mb-6">
				<div class="flex items-center">
					<el-button @click="handleBack" type="text" class="mr-2">
						<el-icon><ArrowLeft /></el-icon>
						Back
					</el-button>
					<h1 class="text-2xl font-bold">
						Customer Overview:
						{{ customerData.companyName || mockCustomerData.companyName }}
					</h1>
				</div>
				<div class="flex items-center space-x-2">
					<el-button @click="handleExportExcel">
						<el-icon><Download /></el-icon>
						Export Excel
					</el-button>
					<el-button @click="handleExportPDF">
						<el-icon><Document /></el-icon>
						Export PDF
					</el-button>
				</div>
			</div>

			<!-- Customer Info Card -->
			<el-card class="mb-6">
				<template #header>
					<div class="text-lg font-medium">Customer Information</div>
				</template>
				<div class="grid grid-cols-1 md:grid-cols-4 gap-4">
					<div>
						<p class="text-sm font-medium text-gray-500">Lead/Customer ID</p>
						<p class="font-medium">{{ leadId }}</p>
					</div>
					<div>
						<p class="text-sm font-medium text-gray-500">Company Name</p>
						<p class="font-medium">
							{{ customerData.companyName || mockCustomerData.companyName }}
						</p>
					</div>
					<div>
						<p class="text-sm font-medium text-gray-500">Contact Person</p>
						<p class="font-medium">
							{{
								customerData.contactPerson ||
								customerData.contactName ||
								mockCustomerData.contactName ||
								'N/A'
							}}
						</p>
					</div>
					<div>
						<p class="text-sm font-medium text-gray-500">Email</p>
						<p class="font-medium">
							{{
								customerData.contactEmail ||
								customerData.email ||
								mockCustomerData.email ||
								'N/A'
							}}
						</p>
					</div>
				</div>
			</el-card>

			<!-- Search and Filters -->
			<el-card class="mb-6">
				<div class="pt-6">
					<div class="grid grid-cols-1 md:grid-cols-4 gap-4 mb-4">
						<div class="space-y-2">
							<label class="text-sm font-medium">Search Questions & Answers</label>
							<el-input
								v-model="searchTerm"
								placeholder="Search questions, answers, or sections..."
								clearable
							>
								<template #prefix>
									<el-icon><Search /></el-icon>
								</template>
							</el-input>
						</div>

						<div class="space-y-2">
							<label class="text-sm font-medium">Filter by Questionnaires</label>
							<el-select
								v-model="selectedQuestionnaires"
								multiple
								collapse-tags
								collapse-tags-tooltip
								placeholder="Select questionnaires..."
								class="w-full"
							>
								<el-option
									v-for="questionnaire in questionnaires"
									:key="questionnaire.id"
									:label="questionnaire.title"
									:value="questionnaire.id"
								/>
							</el-select>
						</div>

						<div class="space-y-2">
							<label class="text-sm font-medium">Filter by Sections</label>
							<el-select
								v-model="selectedSections"
								multiple
								collapse-tags
								collapse-tags-tooltip
								placeholder="Select sections..."
								class="w-full"
							>
								<el-option
									v-for="section in sections"
									:key="section"
									:label="section"
									:value="section"
								/>
							</el-select>
						</div>

						<div class="space-y-2">
							<label class="text-sm font-medium">Actions</label>
							<div class="flex space-x-2">
								<el-button @click="applyFilters" type="primary" class="flex-1">
									<el-icon><Filter /></el-icon>
									Filter
								</el-button>
								<el-button @click="clearFilters">
									<el-icon><Close /></el-icon>
								</el-button>
							</div>
						</div>
					</div>

					<div class="flex items-center justify-between text-sm text-gray-500">
						<span>
							Showing {{ allResponses.length }} responses from
							{{ filteredData.length }} questionnaires
						</span>
						<div v-if="hasActiveFilters" class="flex items-center space-x-2">
							<el-tag type="info">Filters Applied</el-tag>
							<el-tag v-if="appliedQuestionnaires.length > 0" type="primary">
								{{ appliedQuestionnaires.length }} questionnaires
							</el-tag>
							<el-tag v-if="appliedSections.length > 0" type="success">
								{{ appliedSections.length }} sections
							</el-tag>
						</div>
					</div>
				</div>
			</el-card>

			<!-- Questionnaire Responses -->
			<div class="space-y-6">
				<template v-if="filteredData.length > 0">
					<el-card v-for="questionnaire in filteredData" :key="questionnaire.id">
						<template #header>
							<div class="flex justify-between items-center bg-blue-50 -m-4 p-4">
								<div>
									<div class="text-lg font-medium">
										{{ questionnaire.questionnaireTitle }}
									</div>
									<p class="text-sm text-gray-500 mt-1">
										ID: {{ questionnaire.questionnaireId }}
									</p>
								</div>
								<el-tag type="info">
									{{ questionnaire.responses.length }} responses
								</el-tag>
							</div>
						</template>
						<el-table :data="questionnaire.responses" class="w-full">
							<el-table-column label="Section" width="120">
								<template #default="{ row }">
									<el-tag size="small" type="info">{{ row.section }}</el-tag>
								</template>
							</el-table-column>
							<el-table-column label="Question" width="350">
								<template #default="{ row }">
									<p class="font-medium text-gray-900">{{ row.question }}</p>
								</template>
							</el-table-column>
							<el-table-column label="Answer" width="300">
								<template #default="{ row }">
									<div class="bg-blue-50 p-2 rounded text-sm">
										<p class="text-gray-700">{{ row.answer }}</p>
									</div>
								</template>
							</el-table-column>
							<el-table-column label="Response Info" width="180">
								<template #default="{ row }">
									<div class="space-y-1 text-xs text-gray-500">
										<div class="flex items-center">
											<el-icon class="mr-1"><User /></el-icon>
											<span>{{ row.answeredBy }}</span>
										</div>
										<div class="flex items-center">
											<el-icon class="mr-1"><Clock /></el-icon>
											<span>{{ row.answeredDate }}</span>
										</div>
										<div
											v-if="row.lastUpdated !== row.answeredDate"
											class="text-blue-600 font-medium"
										>
											<span>Updated: {{ row.lastUpdated }}</span>
											<br />
											<span>by {{ row.updatedBy }}</span>
										</div>
									</div>
								</template>
							</el-table-column>
						</el-table>
					</el-card>
				</template>
				<el-card v-else>
					<div class="py-12 text-center">
						<el-icon class="text-6xl text-gray-400 mb-4"><Document /></el-icon>
						<h3 class="text-lg font-medium mb-2">No responses found</h3>
						<p class="text-gray-500 mb-4">
							{{
								hasActiveFilters
									? 'Try adjusting your search criteria or filters.'
									: "This customer hasn't completed any questionnaires yet."
							}}
						</p>
						<el-button v-if="hasActiveFilters" @click="clearFilters">
							Clear Filters
						</el-button>
					</div>
				</el-card>
			</div>

			<!-- Summary Statistics -->
			<el-card v-if="filteredData.length > 0" class="mt-6">
				<template #header>
					<div class="text-lg font-medium">Response Summary</div>
				</template>
				<div class="grid grid-cols-1 md:grid-cols-4 gap-4">
					<div class="text-center">
						<div class="text-2xl font-bold text-blue-600">
							{{ filteredData.length }}
						</div>
						<div class="text-sm text-gray-500">Questionnaires</div>
					</div>
					<div class="text-center">
						<div class="text-2xl font-bold text-green-600">
							{{ allResponses.length }}
						</div>
						<div class="text-sm text-gray-500">Total Responses</div>
					</div>
					<div class="text-center">
						<div class="text-2xl font-bold text-purple-600">{{ sections.length }}</div>
						<div class="text-sm text-gray-500">Sections</div>
					</div>
					<div class="text-center">
						<div class="text-2xl font-bold text-orange-600">
							{{ uniqueContributors }}
						</div>
						<div class="text-sm text-gray-500">Contributors</div>
					</div>
				</div>
			</el-card>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import {
	ArrowLeft,
	Download,
	Document,
	Search,
	Filter,
	Close,
	User,
	Clock,
} from '@element-plus/icons-vue';
import * as XLSX from 'xlsx';
import jsPDF from 'jspdf';
import html2canvas from 'html2canvas';
import { getOnboardingDetail } from '@/apis/ow/onboarding';

// Router
const route = useRoute();
const router = useRouter();

// Get leadId from route params
const leadId = computed(() => route.params.leadId as string);
const companyName = computed(() => (route.query.companyName as string) || '');

// State
const loading = ref(false);
const searchTerm = ref('');
const selectedQuestionnaires = ref<string[]>([]);
const selectedSections = ref<string[]>([]);

// Applied filters (what's actually being used for filtering)
const appliedSearchTerm = ref('');
const appliedQuestionnaires = ref<string[]>([]);
const appliedSections = ref<string[]>([]);

// Mock data for customer questionnaire responses
const mockQuestionnaireData = ref([
	{
		id: 'Q1',
		questionnaireTitle: 'Initial Business Assessment',
		questionnaireId: 'QUEST-001',
		responses: [
			{
				id: 'R1',
				question: 'What is your primary business type?',
				answer: 'E-commerce retail with focus on consumer electronics',
				answeredBy: 'John Doe',
				answeredDate: '2023-05-01 10:30',
				lastUpdated: '2023-05-01 10:30',
				updatedBy: 'John Doe',
				questionType: 'text',
				section: 'Business Information',
			},
			{
				id: 'R2',
				question: 'What is your expected monthly order volume?',
				answer: '500-1000 orders per month',
				answeredBy: 'John Doe',
				answeredDate: '2023-05-01 10:32',
				lastUpdated: '2023-05-02 14:20',
				updatedBy: 'Sarah Johnson',
				questionType: 'multiple_choice',
				section: 'Volume Expectations',
			},
			{
				id: 'R3',
				question: 'Do you require special handling for any products?',
				answer: 'Yes - fragile electronics requiring extra padding',
				answeredBy: 'John Doe',
				answeredDate: '2023-05-01 10:35',
				lastUpdated: '2023-05-01 10:35',
				updatedBy: 'John Doe',
				questionType: 'yes_no_text',
				section: 'Special Requirements',
			},
			{
				id: 'R4',
				question: 'What are your preferred shipping carriers?',
				answer: 'FedEx, UPS, USPS',
				answeredBy: 'John Doe',
				answeredDate: '2023-05-01 10:40',
				lastUpdated: '2023-05-01 10:40',
				updatedBy: 'John Doe',
				questionType: 'multiple_select',
				section: 'Shipping Preferences',
			},
		],
	},
	{
		id: 'Q2',
		questionnaireTitle: 'Warehouse Requirements',
		questionnaireId: 'QUEST-002',
		responses: [
			{
				id: 'R5',
				question: 'What types of storage conditions do you require?',
				answer: 'Climate-controlled storage',
				answeredBy: 'John Doe',
				answeredDate: '2023-05-08 09:15',
				lastUpdated: '2023-05-08 09:15',
				updatedBy: 'John Doe',
				questionType: 'select',
				section: 'Storage Requirements',
			},
			{
				id: 'R6',
				question: 'Do you need inventory management integration?',
				answer: 'Yes',
				answeredBy: 'John Doe',
				answeredDate: '2023-05-08 09:18',
				lastUpdated: '2023-05-08 09:18',
				updatedBy: 'John Doe',
				questionType: 'yes_no',
				section: 'Integration Requirements',
			},
			{
				id: 'R7',
				question: 'Which e-commerce platform do you use?',
				answer: 'Shopify Plus',
				answeredBy: 'John Doe',
				answeredDate: '2023-05-08 09:20',
				lastUpdated: '2023-05-10 11:45',
				updatedBy: 'Mike Johnson',
				questionType: 'select',
				section: 'Integration Requirements',
			},
			{
				id: 'R8',
				question: 'What are your peak season months?',
				answer: 'November, December, August',
				answeredBy: 'John Doe',
				answeredDate: '2023-05-08 09:25',
				lastUpdated: '2023-05-08 09:25',
				updatedBy: 'John Doe',
				questionType: 'multiple_select',
				section: 'Seasonal Information',
			},
		],
	},
	{
		id: 'Q3',
		questionnaireTitle: 'Shipping & Logistics',
		questionnaireId: 'QUEST-003',
		responses: [
			{
				id: 'R9',
				question: 'Do you offer international shipping?',
				answer: 'Yes',
				answeredBy: 'John Doe',
				answeredDate: '2023-05-12 14:30',
				lastUpdated: '2023-05-12 14:30',
				updatedBy: 'John Doe',
				questionType: 'yes_no',
				section: 'International Shipping',
			},
			{
				id: 'R10',
				question: 'Which countries do you ship to?',
				answer: 'Canada, UK, Australia',
				answeredBy: 'John Doe',
				answeredDate: '2023-05-12 14:32',
				lastUpdated: '2023-05-12 14:32',
				updatedBy: 'John Doe',
				questionType: 'multiple_select',
				section: 'International Shipping',
			},
			{
				id: 'R11',
				question: 'What are your standard delivery timeframes?',
				answer: '2-3 business days standard, next-day premium',
				answeredBy: 'John Doe',
				answeredDate: '2023-05-12 14:35',
				lastUpdated: '2023-05-13 10:20',
				updatedBy: 'Emily Davis',
				questionType: 'text',
				section: 'Delivery Requirements',
			},
			{
				id: 'R12',
				question: 'Do you require signature confirmation?',
				answer: 'For orders over $500',
				answeredBy: 'John Doe',
				answeredDate: '2023-05-12 14:40',
				lastUpdated: '2023-05-12 14:40',
				updatedBy: 'John Doe',
				questionType: 'conditional',
				section: 'Delivery Requirements',
			},
		],
	},
	{
		id: 'Q4',
		questionnaireTitle: 'Technical Integration',
		questionnaireId: 'QUEST-004',
		responses: [
			{
				id: 'R13',
				question: 'Do you have existing API integrations?',
				answer: 'Yes',
				answeredBy: 'John Doe',
				answeredDate: '2023-05-18 11:00',
				lastUpdated: '2023-05-18 11:00',
				updatedBy: 'John Doe',
				questionType: 'yes_no',
				section: 'API Integrations',
			},
			{
				id: 'R14',
				question: 'Which systems need API integration?',
				answer: 'QuickBooks, Zendesk, Shopify',
				answeredBy: 'John Doe',
				answeredDate: '2023-05-18 11:05',
				lastUpdated: '2023-05-18 11:05',
				updatedBy: 'John Doe',
				questionType: 'multiple_select',
				section: 'API Integrations',
			},
			{
				id: 'R15',
				question: 'What is your preferred data sync frequency?',
				answer: 'Real-time',
				answeredBy: 'John Doe',
				answeredDate: '2023-05-18 11:10',
				lastUpdated: '2023-05-18 11:10',
				updatedBy: 'John Doe',
				questionType: 'select',
				section: 'Data Synchronization',
			},
		],
	},
]);

// Customer data
const customerData = ref({
	id: '',
	companyName: companyName.value || '',
	contactName: '',
	email: '',
	phone: '',
	contactPerson: '',
	contactEmail: '',
});

// Mock customer data (fallback)
const mockCustomerData = ref({
	id: 'LEAD-001',
	companyName: companyName.value || 'Acme Corporation',
	contactName: 'John Doe',
	email: 'john.doe@acmecorp.com',
	phone: '+1 (555) 123-4567',
});

// Computed properties
const questionnaires = computed(() => {
	return mockQuestionnaireData.value.map((q) => ({
		id: q.questionnaireId,
		title: q.questionnaireTitle,
	}));
});

const sections = computed(() => {
	const allSections = new Set<string>();
	mockQuestionnaireData.value.forEach((q) => {
		q.responses.forEach((r) => {
			allSections.add(r.section);
		});
	});
	return Array.from(allSections).sort();
});

// Filter responses based on applied filters
const filteredData = computed(() => {
	return mockQuestionnaireData.value
		.map((questionnaire) => ({
			...questionnaire,
			responses: questionnaire.responses.filter((response) => {
				const matchesSearch =
					appliedSearchTerm.value === '' ||
					response.question
						.toLowerCase()
						.includes(appliedSearchTerm.value.toLowerCase()) ||
					response.answer.toLowerCase().includes(appliedSearchTerm.value.toLowerCase()) ||
					response.section.toLowerCase().includes(appliedSearchTerm.value.toLowerCase());

				const matchesQuestionnaire =
					appliedQuestionnaires.value.length === 0 ||
					appliedQuestionnaires.value.includes(questionnaire.questionnaireId);

				const matchesSection =
					appliedSections.value.length === 0 ||
					appliedSections.value.includes(response.section);

				return matchesSearch && matchesQuestionnaire && matchesSection;
			}),
		}))
		.filter((questionnaire) => questionnaire.responses.length > 0);
});

// Get all responses for export
const allResponses = computed(() => {
	const responses: any[] = [];
	filteredData.value.forEach((questionnaire) => {
		questionnaire.responses.forEach((response) => {
			responses.push({
				questionnaire: questionnaire.questionnaireTitle,
				questionnaireId: questionnaire.questionnaireId,
				section: response.section,
				question: response.question,
				answer: response.answer,
				answeredBy: response.answeredBy,
				answeredDate: response.answeredDate,
				lastUpdated: response.lastUpdated,
				updatedBy: response.updatedBy,
			});
		});
	});
	return responses;
});

const hasActiveFilters = computed(() => {
	return (
		appliedSearchTerm.value ||
		appliedQuestionnaires.value.length > 0 ||
		appliedSections.value.length > 0
	);
});

const uniqueContributors = computed(() => {
	return new Set(allResponses.value.map((r) => r.answeredBy)).size;
});

// Methods
const handleBack = () => {
	const from = route.query.from;
	if (from === 'onboardingDetail') {
		router.back();
	} else {
		router.push('/onboard/onboardingList');
	}
};

const handleExportExcel = () => {
	try {
		const exportData = allResponses.value.map((response) => ({
			questionnaire: response.questionnaire,
			questionnaireId: response.questionnaireId,
			section: response.section,
			question: response.question,
			answer: response.answer,
			answeredBy: response.answeredBy,
			answeredDate: response.answeredDate,
			lastUpdated: response.lastUpdated,
			updatedBy: response.updatedBy,
		}));

		const worksheet = XLSX.utils.json_to_sheet(exportData);
		const workbook = XLSX.utils.book_new();
		XLSX.utils.book_append_sheet(workbook, worksheet, 'Customer Overview');
		XLSX.writeFile(
			workbook,
			`Customer_Overview_${
				customerData.value.companyName || mockCustomerData.value.companyName
			}_${leadId.value}.xlsx`
		);
		ElMessage.success('Excel file exported successfully');
	} catch (error) {
		console.error('Export Excel failed:', error);
		ElMessage.error('Failed to export Excel file');
	}
};

const handleExportPDF = async () => {
	try {
		ElMessage.info('Generating PDF, please wait...');

		// 获取页面内容元素
		const element = document.querySelector('.min-h-screen.bg-gray-50') as HTMLElement;
		if (!element) {
			throw new Error('Page content not found');
		}

		// 临时隐藏导出按钮
		const exportButtons = element.querySelectorAll('.flex.items-center.space-x-2');
		exportButtons.forEach((btn) => {
			(btn as HTMLElement).style.display = 'none';
		});

		// 生成canvas
		const canvas = await html2canvas(element, {
			scale: 1.2, // 进一步降低缩放比例以减小文件大小
			useCORS: true,
			allowTaint: true,
			backgroundColor: '#ffffff',
			width: element.scrollWidth,
			height: element.scrollHeight,
			logging: false, // 关闭日志以提高性能
			removeContainer: true, // 移除临时容器
		});

		// 恢复导出按钮显示
		exportButtons.forEach((btn) => {
			(btn as HTMLElement).style.display = '';
		});

		// 动态调整图片质量以优化文件大小
		let quality = 0.7;
		let imgData = canvas.toDataURL('image/jpeg', quality);

		// 如果图片数据过大，进一步降低质量
		while (imgData.length > 2000000 && quality > 0.4) {
			// 2MB 限制
			quality -= 0.1;
			imgData = canvas.toDataURL('image/jpeg', quality);
		}

		const pdf = new jsPDF({
			orientation: 'portrait',
			unit: 'mm',
			format: 'a4',
			compress: true, // 启用PDF压缩
		});

		const imgWidth = 210; // A4 width in mm
		const pageHeight = 295; // A4 height in mm
		const imgHeight = (canvas.height * imgWidth) / canvas.width;
		let heightLeft = imgHeight;
		let position = 0;

		// 添加第一页
		pdf.addImage(imgData, 'JPEG', 0, position, imgWidth, imgHeight);
		heightLeft -= pageHeight;

		// 如果内容超过一页，添加更多页面
		while (heightLeft >= 0) {
			position = heightLeft - imgHeight;
			pdf.addPage();
			pdf.addImage(imgData, 'JPEG', 0, position, imgWidth, imgHeight);
			heightLeft -= pageHeight;
		}

		pdf.save(
			`Customer_Overview_${
				customerData.value.companyName || mockCustomerData.value.companyName
			}_${leadId.value}.pdf`
		);
		ElMessage.success('PDF file exported successfully');
	} catch (error) {
		console.error('Export PDF failed:', error);
		ElMessage.error('Failed to export PDF file');
	}
};

const applyFilters = () => {
	appliedSearchTerm.value = searchTerm.value;
	appliedQuestionnaires.value = [...selectedQuestionnaires.value];
	appliedSections.value = [...selectedSections.value];
};

const clearFilters = () => {
	searchTerm.value = '';
	selectedQuestionnaires.value = [];
	selectedSections.value = [];
	appliedSearchTerm.value = '';
	appliedQuestionnaires.value = [];
	appliedSections.value = [];
};

// Load customer data
const loadCustomerData = async () => {
	if (!leadId.value) return;

	try {
		loading.value = true;
		const response = await getOnboardingDetail(leadId.value);

		if (response.code === '200' && response.data) {
			const data = response.data;
			customerData.value = {
				id: data.leadId || '',
				companyName: data.leadName || companyName.value || '',
				contactName: data.contactPerson || '',
				email: data.contactEmail || data.leadEmail || '',
				phone: data.leadPhone || '',
				contactPerson: data.contactPerson || '',
				contactEmail: data.contactEmail || '',
			};
		}
	} catch (error) {
		console.error('Failed to load customer data:', error);
		ElMessage.error('Failed to load customer data');
	} finally {
		loading.value = false;
	}
};

// Load data on mount
onMounted(() => {
	console.log('Loading customer overview for Lead ID:', leadId.value);
	loadCustomerData();
});
</script>

<style scoped lang="scss">
.container {
	max-width: 1200px;
}

.space-y-2 > * + * {
	margin-top: 0.5rem;
}

.space-y-6 > * + * {
	margin-top: 1.5rem;
}

.space-x-2 > * + * {
	margin-left: 0.5rem;
}

.grid {
	display: grid;
}

.grid-cols-1 {
	grid-template-columns: repeat(1, minmax(0, 1fr));
}

@media (min-width: 768px) {
	.md\:grid-cols-4 {
		grid-template-columns: repeat(4, minmax(0, 1fr));
	}
}

.gap-4 {
	gap: 1rem;
}

.text-2xl {
	font-size: 1.5rem;
	line-height: 2rem;
}

.font-bold {
	font-weight: 700;
}

.font-medium {
	font-weight: 500;
}

.text-sm {
	font-size: 0.875rem;
	line-height: 1.25rem;
}

.text-xs {
	font-size: 0.75rem;
	line-height: 1rem;
}

.text-lg {
	font-size: 1.125rem;
	line-height: 1.75rem;
}

.text-gray-500 {
	color: #6b7280;
}

.text-gray-700 {
	color: #374151;
}

.text-gray-900 {
	color: #111827;
}

.text-blue-600 {
	color: #2563eb;
}

.text-green-600 {
	color: #16a34a;
}

.text-purple-600 {
	color: #9333ea;
}

.text-orange-600 {
	color: #ea580c;
}

.bg-blue-50 {
	background-color: #eff6ff;
}

.rounded {
	border-radius: 0.25rem;
}

.p-2 {
	padding: 0.5rem;
}

.p-4 {
	padding: 1rem;
}

.py-6 {
	padding-top: 1.5rem;
	padding-bottom: 1.5rem;
}

.py-12 {
	padding-top: 3rem;
	padding-bottom: 3rem;
}

.px-4 {
	padding-left: 1rem;
	padding-right: 1rem;
}

.mb-2 {
	margin-bottom: 0.5rem;
}

.mb-4 {
	margin-bottom: 1rem;
}

.mb-6 {
	margin-bottom: 1.5rem;
}

.mt-6 {
	margin-top: 1.5rem;
}

.mr-1 {
	margin-right: 0.25rem;
}

.mr-2 {
	margin-right: 0.5rem;
}

.text-center {
	text-align: center;
}

.flex {
	display: flex;
}

.flex-1 {
	flex: 1 1 0%;
}

.items-center {
	align-items: center;
}

.justify-between {
	justify-content: space-between;
}

.w-full {
	width: 100%;
}

.min-h-screen {
	min-height: 100vh;
}

/* 暗色主题样式 */
html.dark {
	.bg-gray-50 {
		@apply bg-black-400 !important;
	}

	.text-gray-900 {
		@apply text-white-100 !important;
	}

	.text-gray-600,
	.text-gray-500 {
		@apply text-gray-300 !important;
	}
}
</style>
