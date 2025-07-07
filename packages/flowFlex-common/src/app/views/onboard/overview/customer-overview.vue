<template>
	<div class="pb-6 bg-gray-50 dark:bg-black-400">
		<!-- 加载状态 -->
		<customer-overview-loading v-if="loading" />

		<!-- 主要内容 -->
		<div v-else>
			<!-- 顶部导航栏 -->
			<div class="flex justify-between items-center mb-6">
				<div class="flex items-center">
					<el-button
						link
						size="small"
						@click="handleBack"
						class="mr-2 !p-1 hover:bg-gray-100 dark:hover:bg-black-200 rounded"
					>
						<el-icon class="text-lg"><ArrowLeft /></el-icon>
						Back
					</el-button>
					<h1 class="text-2xl font-bold text-gray-900 dark:text-white-100">
						Customer Overview: {{ customerData?.leadName || 'Loading...' }}
					</h1>
				</div>
				<div class="flex items-center space-x-2">
					<el-button @click="handleExportExcel" :disabled="!customerData">
						<el-icon><Download /></el-icon>
						&nbsp;&nbsp;Export Excel
					</el-button>
					<el-button @click="handleExportPDF" :disabled="!customerData">
						<el-icon><Document /></el-icon>
						&nbsp;&nbsp;Export PDF
					</el-button>
				</div>
			</div>

			<!-- Customer Info Card -->
			<el-card class="mb-6" v-if="customerData">
				<template #header>
					<div class="text-lg font-medium">Customer Information</div>
				</template>
				<div class="grid grid-cols-1 md:grid-cols-4 gap-4">
					<div>
						<p class="text-sm font-medium text-gray-500">Lead/Customer ID</p>
						<p class="font-medium">{{ customerData.leadId }}</p>
					</div>
					<div>
						<p class="text-sm font-medium text-gray-500">Company Name</p>
						<p class="font-medium">{{ customerData.leadName }}</p>
					</div>
					<div>
						<p class="text-sm font-medium text-gray-500">Contact Name</p>
						<p class="font-medium">{{ customerData.contactPerson || 'N/A' }}</p>
					</div>
					<div>
						<p class="text-sm font-medium text-gray-500">Contact Email</p>
						<p class="font-medium">
							{{ customerData.contactEmail || customerData.leadEmail || 'N/A' }}
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
								filterable
								collapse-tags
								collapse-tags-tooltip
								placeholder="Search questionnaires..."
								class="w-full"
								no-data-text="No questionnaires found"
								filter-placeholder="Type to search questionnaires..."
							>
								<el-option
									v-for="questionnaire in questionnaires"
									:key="questionnaire.id"
									:label="questionnaire.name"
									:value="questionnaire.id"
								/>
							</el-select>
						</div>

						<div class="space-y-2">
							<label class="text-sm font-medium">Filter by Sections</label>
							<el-select
								v-model="selectedSections"
								multiple
								filterable
								collapse-tags
								collapse-tags-tooltip
								placeholder="Search sections..."
								class="w-full"
								no-data-text="No sections found"
								filter-placeholder="Type to search sections..."
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
									&nbsp;&nbsp;Search
								</el-button>
								<el-button @click="clearFilters">
									<el-icon><Close /></el-icon>
								</el-button>
							</div>
						</div>
					</div>

					<div class="flex items-center justify-between text-sm text-gray-500">
						<span>
							Showing {{ visibleResponses.length }} of
							{{ totalResponsesCount }} responses from
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
				<template v-if="paginatedData.length > 0">
					<el-card v-for="questionnaire in paginatedData" :key="questionnaire.id">
						<template #header>
							<div class="flex justify-between items-center bg-blue-50 -m-4 p-4">
								<div>
									<div class="text-lg font-medium">
										{{ questionnaire.name }}
									</div>
								</div>
								<div class="flex flex-col items-end space-y-1">
									<el-tag type="info" style="color: #000">
										{{
											questionnaire.responses.filter((r) =>
												hasValidAnswer(r.answer)
											).length
										}}
										responses
									</el-tag>
								</div>
							</div>
						</template>
						<el-table
							:data="questionnaire.responses"
							class="w-full"
							style="width: 100%"
							:lazy="true"
							v-loading="questionnaire.loading"
						>
							<el-table-column label="Section" width="150">
								<template #default="{ row }">
									<div
										style="
											color: rgb(0, 0, 0);
											background-color: rgb(255, 255, 255);
											box-sizing: border-box;
											border-width: 1px;
											border-style: solid;
											border-color: rgb(206, 206, 206);
											border-radius: 9999px;
											width: 130px;
											overflow: hidden;
											overflow-wrap: break-word;
											text-overflow: ellipsis;
											white-space: break-spaces;
											font-size: 12px;
											line-height: 20px;
											padding-left: 6px;
										"
									>
										{{ row.section }}
									</div>
								</template>
							</el-table-column>
							<el-table-column label="Question" show-overflow-tooltip min-width="200">
								<template #default="{ row }">
									<p
										class="font-medium text-gray-900"
										style="white-space: normal"
									>
										{{ row.question }}
									</p>
								</template>
							</el-table-column>
							<el-table-column label="Answer" show-overflow-tooltip min-width="200">
								<template #default="{ row }">
									<div class="bg-blue-50 p-2 rounded text-sm">
										<!-- 短答题 -->
										<div
											v-if="isShortAnswerType(row.questionType)"
											class="text-gray-700"
										>
											{{ row.answer }}
										</div>

										<!-- 长答题/段落 -->
										<div
											v-else-if="isParagraphType(row.questionType)"
											class="text-gray-700"
											style="white-space: pre-wrap"
										>
											{{ row.answer }}
										</div>

										<!-- 单选题 -->
										<div
											v-else-if="isMultipleChoiceType(row.questionType)"
											class="single-choice-answer"
										>
											<el-tag
												v-if="row.answer"
												type="primary"
												size="small"
												effect="light"
												class="choice-tag"
											>
												<el-icon class="mr-1" size="12">
													<Check />
												</el-icon>
												{{ row.answer }}
											</el-tag>
										</div>

										<!-- 多选题 -->
										<div
											v-else-if="isCheckboxType(row.questionType)"
											class="checkbox-answers"
										>
											<template
												v-if="getCheckboxAnswers(row.answer).length > 0"
											>
												<div class="flex flex-wrap gap-1">
													<el-tag
														v-for="(item, index) in getCheckboxAnswers(
															row.answer
														)"
														:key="`${item}-${index}`"
														type="success"
														size="small"
														effect="light"
														class="checkbox-tag"
													>
														<el-icon class="mr-1" size="12">
															<Check />
														</el-icon>
														{{ item }}
													</el-tag>
												</div>
												<div class="mt-1 text-xs text-gray-500">
													{{ getCheckboxAnswers(row.answer).length }}
													option{{
														getCheckboxAnswers(row.answer).length > 1
															? 's'
															: ''
													}}
													selected
												</div>
											</template>
										</div>

										<!-- 下拉选择 -->
										<div
											v-else-if="isDropdownType(row.questionType)"
											class="dropdown-answer"
										>
											<el-tag
												v-if="row.answer"
												type="info"
												size="small"
												effect="light"
												class="dropdown-tag"
											>
												<el-icon class="mr-1" size="12">
													<Check />
												</el-icon>
												{{ row.answer }}
											</el-tag>
										</div>

										<!-- 日期/时间 -->
										<div
											v-else-if="isDateTimeType(row.questionType)"
											class="text-gray-700"
										>
											<template v-if="row.answer">
												<el-icon class="mr-1"><Calendar /></el-icon>
												{{ formatAnswerDate(row.answer) }}
											</template>
										</div>

										<!-- 评分 -->
										<div
											v-else-if="isRatingType(row.questionType)"
											class="flex items-center"
										>
											<el-rate
												v-if="row.answer"
												:model-value="parseFloat(String(row.answer))"
												disabled
												:max="5"
												size="small"
											/>
											<span
												v-if="row.answer"
												class="ml-2 text-sm text-gray-600"
											>
												({{ row.answer }}/5)
											</span>
										</div>

										<!-- 线性量表 -->
										<div
											v-else-if="isLinearScaleType(row.questionType)"
											class="space-y-2"
										>
											<div v-if="row.answer" class="flex items-center">
												<div
													class="flex-1 bg-gray-200 rounded-full h-2 mr-2"
												>
													<div
														class="bg-blue-500 h-2 rounded-full"
														:style="`width: ${
															(parseFloat(String(row.answer)) /
																getLinearScaleMax(
																	row.questionConfig
																)) *
															100
														}%`"
													></div>
												</div>
												<span class="text-sm font-medium">
													{{ row.answer }}/{{
														getLinearScaleMax(row.questionConfig)
													}}
												</span>
											</div>
										</div>

										<!-- 文件上传 -->
										<div
											v-else-if="isFileUploadType(row.questionType)"
											class="space-y-1"
										>
											<template v-if="getFileAnswers(row.answer).length > 0">
												<div
													v-for="file in getFileAnswers(row.answer)"
													:key="file.name"
													class="flex items-center text-sm bg-gray-100 p-1 rounded"
												>
													<el-icon class="mr-1 text-blue-500">
														<Document />
													</el-icon>
													<span class="truncate">{{ file.name }}</span>
													<span class="ml-1 text-xs text-gray-500">
														({{ formatFileSize(file.size) }})
													</span>
												</div>
											</template>
										</div>

										<!-- 单选网格 (Checkbox grid) -->
										<div
											v-else-if="isCheckboxGridType(row.questionType)"
											class="grid-answer"
										>
											<el-tag
												v-if="row.answer"
												type="warning"
												size="small"
												effect="light"
												class="grid-tag"
											>
												<el-icon class="mr-1" size="12">
													<Check />
												</el-icon>
												{{ row.answer }}
											</el-tag>
											<div class="mt-1 text-xs text-gray-500">
												Grid selection
											</div>
										</div>

										<!-- 多选网格 (Multiple choice grid) -->
										<div
											v-else-if="isMultipleChoiceGridType(row.questionType)"
											class="grid-answer"
										>
											<template
												v-if="getCheckboxAnswers(row.answer).length > 0"
											>
												<div class="flex flex-wrap gap-1">
													<el-tag
														v-for="(item, index) in getCheckboxAnswers(
															row.answer
														)"
														:key="`${item}-${index}`"
														type="warning"
														size="small"
														effect="light"
														class="grid-tag"
													>
														<el-icon class="mr-1" size="12">
															<Check />
														</el-icon>
														{{ item }}
													</el-tag>
												</div>
												<div class="mt-1 text-xs text-gray-500">
													{{ getCheckboxAnswers(row.answer).length }}
													grid selection{{
														getCheckboxAnswers(row.answer).length > 1
															? 's'
															: ''
													}}
												</div>
											</template>
										</div>

										<!-- 默认显示 -->
										<div
											v-else
											class="text-gray-700"
											style="white-space: normal"
										>
											{{ row.answer }}
										</div>
									</div>
								</template>
							</el-table-column>
							<el-table-column label="Response Info" width="200">
								<template #default="{ row }">
									<div class="space-y-1 text-xs">
										<!-- 只有当有有效答案时才显示回答人信息和时间 -->
										<template v-if="hasValidAnswer(row.answer)">
											<!-- 回答人信息 -->
											<div
												class="flex items-center text-gray-700"
												v-if="row.answeredBy"
											>
												<el-icon class="mr-1"><User /></el-icon>
												<span class="font-medium">
													{{ row.answeredBy }}
												</span>
											</div>

											<!-- 首次回答时间 -->
											<div
												class="flex items-center text-gray-600"
												v-if="row.firstAnsweredDate"
											>
												<el-icon class="mr-1"><Clock /></el-icon>
												<span>{{ formatDate(row.firstAnsweredDate) }}</span>
											</div>

											<!-- 最新修改时间 - 只有当修改时间与首次回答时间不同时才显示 -->
											<div
												v-if="
													row.lastUpdated &&
													row.updatedBy &&
													row.lastUpdated !== row.firstAnsweredDate
												"
												class="text-blue-600"
											>
												<div>
													Updated: {{ formatDate(row.lastUpdated) }}
												</div>
												<div>by {{ row.updatedBy }}</div>
											</div>

											<!-- 如果没有修改过，只显示创建时间 -->
											<div
												v-else-if="
													row.answeredDate && !row.firstAnsweredDate
												"
												class="flex items-center text-gray-600"
											>
												<el-icon class="mr-1"><Clock /></el-icon>
												<span>{{ formatDate(row.answeredDate) }}</span>
											</div>
										</template>
									</div>
								</template>
							</el-table-column>
						</el-table>
					</el-card>
				</template>
				<el-card v-else-if="!loading">
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

			<!-- Pagination -->
			<div class="mt-6 flex justify-center" v-if="totalPages > 1">
				<el-pagination
					v-model:current-page="currentPage"
					:page-size="pageSize"
					:total="filteredData.length"
					layout="prev, pager, next, sizes, total"
					:page-sizes="[5, 10, 20, 50]"
					@size-change="handleSizeChange"
					@current-change="handleCurrentChange"
				/>
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
							{{ totalResponsesCount }}
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
import { ref, computed, onMounted, onUnmounted, nextTick, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage } from 'element-plus';
import '@/styles/errorDialog.css';
import {
	ArrowLeft,
	Download,
	Document,
	Search,
	Filter,
	Close,
	User,
	Clock,
	Calendar,
	Check,
} from '@element-plus/icons-vue';
import * as XLSX from 'xlsx';
import jsPDF from 'jspdf';
import html2canvas from 'html2canvas';
import { getOnboardingDetail } from '@/apis/ow/onboarding';
import { getStageQuestionnairesBatch, getQuestionnaireAnswersBatch } from '@/apis/ow/questionnaire';
import CustomerOverviewLoading from './customer-overview-loading.vue';

// Types
interface OnboardingData {
	id: string;
	workflowId: string;
	workflowName: string;
	currentStageId: string;
	currentStageName: string;
	leadId: string;
	leadName: string;
	leadEmail: string;
	leadPhone: string;
	contactPerson: string; // 联系人姓名
	contactEmail: string; // 联系人邮箱
	status: string;
	completionRate: number;
	startDate: string;
	estimatedCompletionDate: string;
	actualCompletionDate: string | null;
	currentAssigneeName: string;
	stagesProgress: StageProgress[];
}

interface StageProgress {
	stageId: string;
	stageName: string;
	stageOrder: number;
	status: string;
	isCompleted: boolean;
	startTime: string | null;
	completionTime: string | null;
	completedBy: string | null;
	isCurrent: boolean;
}

interface QuestionnaireData {
	id: string;
	name: string;
	description: string;
	type: string;
	status: string;
	structureJson: string;
	totalQuestions: number;
	requiredQuestions: number;
	stageId: string;
}

interface QuestionnaireAnswer {
	id: string;
	onboardingId: string;
	stageId: string;
	questionnaireId: string;
	answerJson: string;
	status: string;
	completionRate: number;
	submitTime: string | null;
	createBy: string;
	createDate: string;
	modifyBy: string;
	modifyDate: string;
}

interface ParsedResponse {
	questionId: string;
	answer: string;
	type: string;
	responseText: string;
}

interface ProcessedQuestion {
	id: string;
	question: string;
	description?: string;
	answer: string;
	answeredBy: string;
	answeredDate: string;
	firstAnsweredDate: string; // 首次回答时间
	lastUpdated: string;
	updatedBy: string;
	questionType: string;
	section: string;
	required: boolean;
	questionConfig?: any; // 存储问题配置信息，如线性量表的范围
}

interface ProcessedQuestionnaire {
	id: string;
	name: string;
	description: string;
	answerStatus: string;
	responses: ProcessedQuestion[];
	loading?: boolean;
}

// Reactive data
const route = useRoute();
const router = useRouter();
const loading = ref(true);
const onboardingId = computed(() => route.params.leadId as string);

// Data storage
const customerData = ref<OnboardingData | null>(null);
const questionnairesData = ref<QuestionnaireData[]>([]);
const answersData = ref<Map<string, QuestionnaireAnswer>>(new Map());
const processedData = ref<ProcessedQuestionnaire[]>([]);

// Search and filter states
const searchTerm = ref('');
const selectedQuestionnaires = ref<string[]>([]);
const selectedSections = ref<string[]>([]);
const appliedSearchTerm = ref('');
const appliedQuestionnaires = ref<string[]>([]);
const appliedSections = ref<string[]>([]);

// Pagination
const currentPage = ref(1);
const pageSize = ref(10);

// Cache for computed results
const computedCache = new Map<string, any>();

// API calls with caching and error handling
const fetchOnboardingData = async (id: string) => {
	if (!id || id === 'undefined') {
		throw new Error('Invalid onboarding ID provided');
	}

	const cacheKey = `onboarding_${id}`;
	if (computedCache.has(cacheKey)) {
		return computedCache.get(cacheKey);
	}

	try {
		const response = await getOnboardingDetail(id);

		if (response.success && response.data) {
			customerData.value = response.data;
			computedCache.set(cacheKey, response.data);
			return response.data;
		}
		throw new Error(response.msg || 'Failed to fetch onboarding data');
	} catch (error) {
		console.error('Error fetching onboarding data for ID:', id, error);
		throw error;
	}
};

// Batch API calls with caching
const fetchQuestionnairesBatch = async (stageIds: string[]) => {
	const cacheKey = `batch_questionnaires_${stageIds.sort().join('_')}`;
	if (computedCache.has(cacheKey)) {
		return computedCache.get(cacheKey);
	}

	try {
		const response = await getStageQuestionnairesBatch(stageIds);
		if (response.success && response.data) {
			computedCache.set(cacheKey, response.data);
			return response.data;
		}
		return { stageQuestionnaires: {} };
	} catch (error) {
		console.error('Error fetching questionnaires batch:', error);
		return { stageQuestionnaires: {} };
	}
};

const fetchAnswersBatch = async (onboardingId: string, stageIds: string[]) => {
	const cacheKey = `batch_answers_${onboardingId}_${stageIds.sort().join('_')}`;
	if (computedCache.has(cacheKey)) {
		return computedCache.get(cacheKey);
	}

	try {
		const response = await getQuestionnaireAnswersBatch(onboardingId, stageIds);
		if (response.success && response.data) {
			computedCache.set(cacheKey, response.data);
			return response.data;
		}
		return { stageAnswers: {} };
	} catch (error) {
		console.error('Error fetching answers batch:', error);
		return { stageAnswers: {} };
	}
};

// Optimized data processing with memoization
const processQuestionnaireData = (
	questionnaire: QuestionnaireData,
	answer: QuestionnaireAnswer | null
) => {
	const cacheKey = `processed_${questionnaire.id}_${answer?.modifyDate || 'no_answer'}`;
	if (computedCache.has(cacheKey)) {
		return computedCache.get(cacheKey);
	}

	try {
		const structure = JSON.parse(questionnaire.structureJson);
		const responses: ProcessedQuestion[] = [];

		let parsedAnswers: { responses: ParsedResponse[] } = { responses: [] };
		if (answer?.answerJson) {
			try {
				const parsedJson = JSON.parse(answer.answerJson);
				parsedAnswers =
					typeof parsedJson === 'string' ? JSON.parse(parsedJson) : parsedJson;
			} catch (e) {
				console.error('Error parsing answer JSON:', e);
			}
		}

		// Create a map of answers by question ID for O(1) lookup
		const answersMap = new Map<string, any>();
		const gridAnswersMap = new Map<string, any[]>(); // 用于存储网格类型的答案

		parsedAnswers.responses?.forEach((resp) => {
			// 检查是否是网格类型的答案（包含 _row- 或 _）
			if (resp.questionId.includes('_row-') || resp.questionId.includes('_')) {
				// 提取原始问题ID
				const baseQuestionId = resp.questionId.split('_')[0];
				if (!gridAnswersMap.has(baseQuestionId)) {
					gridAnswersMap.set(baseQuestionId, []);
				}
				gridAnswersMap.get(baseQuestionId)!.push(resp);
			} else {
				answersMap.set(resp.questionId, resp);
			}
		});

		// Process each section and question
		structure.sections?.forEach((section: any) => {
			section.questions?.forEach((question: any) => {
				// 检查是否是网格类型的问题
				const isGridType =
					question.type === 'checkbox_grid' || question.type === 'multiple_choice_grid';

				let answerData = answersMap.get(question.id);
				let gridAnswers = gridAnswersMap.get(question.id);

				// 如果是网格类型且有网格答案，处理网格答案
				if (isGridType && gridAnswers && gridAnswers.length > 0) {
					// 为每个网格行创建一个响应记录
					gridAnswers.forEach((gridAnswer) => {
						// 获取具体答案的更新信息
						let lastUpdated = '';
						let updatedBy = '';
						let firstAnsweredDate = '';
						let firstAnsweredBy = '';

						if (gridAnswer?.lastModifiedAt && gridAnswer?.lastModifiedBy) {
							lastUpdated = gridAnswer.lastModifiedAt;
							updatedBy = gridAnswer.lastModifiedBy;
						}

						// 从changeHistory中获取首次回答时间和回答人
						if (
							gridAnswer?.changeHistory &&
							Array.isArray(gridAnswer.changeHistory) &&
							gridAnswer.changeHistory.length > 0
						) {
							// 找到第一个"created"操作的时间和用户
							const firstCreated = gridAnswer.changeHistory.find(
								(change: any) => change.action === 'created'
							);
							if (firstCreated) {
								firstAnsweredDate =
									firstCreated.timestamp || firstCreated.timestampUtc || '';
								firstAnsweredBy = firstCreated.user || '';
							} else {
								// 如果没有找到created，使用第一个记录的时间和用户
								const firstRecord = gridAnswer.changeHistory[0];
								firstAnsweredDate =
									firstRecord.timestamp || firstRecord.timestampUtc || '';
								firstAnsweredBy = firstRecord.user || '';
							}
						}

						responses.push({
							id: gridAnswer.questionId, // 使用网格行的完整ID
							question: gridAnswer.question || question.title, // 使用网格行的问题标题
							description: question.description,
							answer: gridAnswer.responseText || gridAnswer.answer || '',
							answeredBy:
								firstAnsweredBy ||
								gridAnswer?.lastModifiedBy ||
								answer?.createBy ||
								'',
							answeredDate: answer?.createDate || '',
							firstAnsweredDate: firstAnsweredDate || answer?.createDate || '',
							lastUpdated: lastUpdated || answer?.modifyDate || '',
							updatedBy: updatedBy || answer?.modifyBy || '',
							questionType: question.type,
							section: section.title,
							required: question.required || false,
							questionConfig: question.config || question, // 存储完整的问题配置
						});
					});
				} else {
					// 非网格类型或没有网格答案的普通处理
					// 获取具体答案的更新信息
					let lastUpdated = '';
					let updatedBy = '';
					let firstAnsweredDate = '';
					let firstAnsweredBy = '';

					if (answerData?.lastModifiedAt && answerData?.lastModifiedBy) {
						lastUpdated = answerData.lastModifiedAt;
						updatedBy = answerData.lastModifiedBy;
					}

					// 从changeHistory中获取首次回答时间和回答人
					if (
						answerData?.changeHistory &&
						Array.isArray(answerData.changeHistory) &&
						answerData.changeHistory.length > 0
					) {
						// 找到第一个"created"操作的时间和用户
						const firstCreated = answerData.changeHistory.find(
							(change: any) => change.action === 'created'
						);
						if (firstCreated) {
							firstAnsweredDate =
								firstCreated.timestamp || firstCreated.timestampUtc || '';
							firstAnsweredBy = firstCreated.user || '';
						} else {
							// 如果没有找到created，使用第一个记录的时间和用户
							const firstRecord = answerData.changeHistory[0];
							firstAnsweredDate =
								firstRecord.timestamp || firstRecord.timestampUtc || '';
							firstAnsweredBy = firstRecord.user || '';
						}
					}

					// 对于文件类型，使用原始answer而不是responseText
					let processedAnswer = '';
					if (question.type === 'file' || question.type === 'file_upload') {
						// 对于文件类型，使用原始answer字段
						processedAnswer = answerData?.answer || '';
					} else {
						// 对于其他类型，使用responseText
						processedAnswer = answerData?.responseText || '';
					}

					responses.push({
						id: question.id,
						question: question.title,
						description: question.description,
						answer: processedAnswer,
						answeredBy:
							firstAnsweredBy || answerData?.lastModifiedBy || answer?.createBy || '',
						answeredDate: answer?.createDate || '',
						firstAnsweredDate: firstAnsweredDate || answer?.createDate || '',
						lastUpdated: lastUpdated || answer?.modifyDate || '',
						updatedBy: updatedBy || answer?.modifyBy || '',
						questionType: question.type,
						section: section.title,
						required: question.required || false,
						questionConfig: question.config || question, // 存储完整的问题配置
					});
				}
			});
		});

		const result = {
			id: questionnaire.id,
			name: questionnaire.name,
			description: questionnaire.description,
			answerStatus: answer?.status || 'Not Started',
			responses,
		};

		computedCache.set(cacheKey, result);
		return result;
	} catch (error) {
		console.error('Error processing questionnaire data:', error);
		return {
			id: questionnaire.id,
			name: questionnaire.name,
			description: questionnaire.description,
			answerStatus: 'Error',
			responses: [],
		};
	}
};

// Optimized data loading with batch API requests
const loadData = async () => {
	loading.value = true;
	try {
		// Load onboarding data first
		const onboarding = await fetchOnboardingData(onboardingId.value);

		// Collect unique stage IDs
		const stageIds = new Set<string>();
		onboarding.stagesProgress?.forEach((stage) => {
			stageIds.add(stage.stageId);
		});

		const stageIdArray = Array.from(stageIds);

		// Use batch APIs for significantly better performance
		const [batchQuestionnaireResponse, batchAnswerResponse] = await Promise.all([
			fetchQuestionnairesBatch(stageIdArray),
			fetchAnswersBatch(onboardingId.value, stageIdArray),
		]);

		// Process batch questionnaire results
		const allQuestionnaires: QuestionnaireData[] = [];
		if (batchQuestionnaireResponse?.stageQuestionnaires) {
			Object.values(batchQuestionnaireResponse.stageQuestionnaires).forEach(
				(stageQuestionnaires: any) => {
					if (Array.isArray(stageQuestionnaires)) {
						allQuestionnaires.push(...stageQuestionnaires);
					}
				}
			);
		}
		questionnairesData.value = allQuestionnaires;

		// Process batch answer results
		const answersMap = new Map<string, QuestionnaireAnswer>();
		if (batchAnswerResponse?.stageAnswers) {
			Object.entries(batchAnswerResponse.stageAnswers).forEach(
				([stageId, answer]: [string, any]) => {
					if (answer) {
						answersMap.set(stageId, answer);
					}
				}
			);
		}
		answersData.value = answersMap;

		// Process data in chunks to avoid blocking UI
		const processed: ProcessedQuestionnaire[] = [];
		const chunkSize = 5;

		for (let i = 0; i < allQuestionnaires.length; i += chunkSize) {
			const chunk = allQuestionnaires.slice(i, i + chunkSize);
			const chunkProcessed = chunk.map((questionnaire) => {
				const answer = answersMap.get(questionnaire.stageId) || null;
				return processQuestionnaireData(questionnaire, answer);
			});
			processed.push(...chunkProcessed);

			// Allow UI to update between chunks
			if (i + chunkSize < allQuestionnaires.length) {
				await nextTick();
			}
		}

		processedData.value = processed;
	} catch (error) {
		console.error('Error loading data:', error);
		ElMessage.error('Failed to load customer overview data');
	} finally {
		loading.value = false;
	}
};

// Optimized computed properties with caching
const questionnaires = computed(() => {
	return questionnairesData.value.map((q) => ({
		id: q.id,
		name: q.name,
	}));
});

const sections = computed(() => {
	const cacheKey = `sections_${processedData.value.length}`;
	if (computedCache.has(cacheKey)) {
		return computedCache.get(cacheKey);
	}

	const allSections = new Set<string>();
	processedData.value.forEach((q) => {
		q.responses.forEach((r) => {
			allSections.add(r.section);
		});
	});
	const result = Array.from(allSections).sort();
	computedCache.set(cacheKey, result);
	return result;
});

// Optimized filtering with debouncing
const filteredData = computed(() => {
	const cacheKey = `filtered_${appliedSearchTerm.value}_${appliedQuestionnaires.value.join(
		','
	)}_${appliedSections.value.join(',')}`;
	if (computedCache.has(cacheKey)) {
		return computedCache.get(cacheKey);
	}

	const result = processedData.value
		.map((questionnaire) => ({
			...questionnaire,
			responses: questionnaire.responses.filter((response) => {
				const searchLower = appliedSearchTerm.value.toLowerCase();
				const matchesSearch =
					!appliedSearchTerm.value ||
					response.question.toLowerCase().includes(searchLower) ||
					response.answer.toLowerCase().includes(searchLower) ||
					response.section.toLowerCase().includes(searchLower);

				const matchesQuestionnaire =
					appliedQuestionnaires.value.length === 0 ||
					appliedQuestionnaires.value.includes(questionnaire.id);

				const matchesSection =
					appliedSections.value.length === 0 ||
					appliedSections.value.includes(response.section);

				return matchesSearch && matchesQuestionnaire && matchesSection;
			}),
		}))
		.filter((questionnaire) => questionnaire.responses.length > 0);

	computedCache.set(cacheKey, result);
	return result;
});

// Pagination
const totalPages = computed(() => Math.ceil(filteredData.value.length / pageSize.value));

const paginatedData = computed(() => {
	const start = (currentPage.value - 1) * pageSize.value;
	const end = start + pageSize.value;
	return filteredData.value.slice(start, end);
});

// Optimized response calculations
const totalResponsesCount = computed(() => {
	return filteredData.value.reduce((total, q) => {
		// 只统计有答案的问题
		const answeredResponses = q.responses.filter((response) => hasValidAnswer(response.answer));
		return total + answeredResponses.length;
	}, 0);
});

const visibleResponses = computed(() => {
	return paginatedData.value.reduce((total, q) => {
		// 只统计有答案的问题
		const answeredResponses = q.responses.filter((response) => hasValidAnswer(response.answer));
		return total + answeredResponses.length;
	}, 0);
});

const allResponses = computed(() => {
	const responses: any[] = [];
	filteredData.value.forEach((questionnaire) => {
		questionnaire.responses.forEach((response) => {
			// 只包含有答案的问题
			if (hasValidAnswer(response.answer)) {
				responses.push({
					questionnaire: questionnaire.name,
					questionnaireId: questionnaire.id,
					section: response.section,
					question: response.question,
					answer: response.answer,
					answeredBy: response.answeredBy,
					answeredDate: response.answeredDate,
					lastUpdated: response.lastUpdated,
					updatedBy: response.updatedBy,
					required: response.required,
					type: response.questionType,
				});
			}
		});
	});
	return responses;
});

// All questions for export (includes questions without answers)
const allQuestionsForExport = computed(() => {
	const responses: any[] = [];
	processedData.value.forEach((questionnaire) => {
		questionnaire.responses.forEach((response) => {
			// Include ALL questions, regardless of whether they have answers
			responses.push({
				questionnaire: questionnaire.name,
				questionnaireId: questionnaire.id,
				section: response.section,
				question: response.question,
				answer: response.answer || '',
				answeredBy: response.answeredBy || '',
				answeredDate: response.answeredDate || '',
				lastUpdated: response.lastUpdated || '',
				updatedBy: response.updatedBy || '',
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
	return new Set(allResponses.value.map((r) => r.answeredBy).filter(Boolean)).size;
});

// Pagination handlers
const handleSizeChange = (newSize: number) => {
	pageSize.value = newSize;
	currentPage.value = 1;
};

const handleCurrentChange = (newPage: number) => {
	currentPage.value = newPage;
};

// Utility functions
const formatDate = (dateString: string) => {
	if (!dateString) return '';
	try {
		return new Date(dateString).toLocaleString();
	} catch {
		return dateString;
	}
};

// 判断答案是否有效（有实际内容）
const hasValidAnswer = (answer: string | any): boolean => {
	if (!answer) return false;
	if (typeof answer === 'string') {
		return answer !== '' && answer !== 'No answer provided' && answer !== 'No selection made';
	}
	if (Array.isArray(answer)) {
		return answer.length > 0;
	}
	return true;
};

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
		// Use the computed property that includes ALL questions, regardless of answers
		const exportData = allQuestionsForExport.value;

		if (exportData.length === 0) {
			ElMessage.warning('No questionnaire data available to export');
			return;
		}

		const worksheet = XLSX.utils.json_to_sheet(exportData);
		const workbook = XLSX.utils.book_new();
		XLSX.utils.book_append_sheet(workbook, worksheet, 'Customer Overview');
		XLSX.writeFile(
			workbook,
			`Customer_Overview_${customerData.value?.leadName}_${customerData.value?.leadId}.xlsx`
		);
		ElMessage.success(`Excel file exported successfully with ${exportData.length} questions`);
	} catch (error) {
		console.error('Export Excel failed:', error);
		ElMessage.error('Failed to export Excel file');
	}
};

const handleExportPDF = async () => {
	try {
		ElMessage.info('Generating PDF, please wait...');

		// 获取页面内容元素
		const element = document.querySelector('.pb-6.bg-gray-50') as HTMLElement;
		if (!element) {
			throw new Error('Page content not found');
		}

		// 临时隐藏导出按钮和搜索过滤区域
		const exportButtons = element.querySelectorAll('.flex.items-center.space-x-2');
		const filterSection = element.querySelector('.mb-6 .pt-6'); // 搜索和过滤区域

		exportButtons.forEach((btn) => {
			(btn as HTMLElement).style.display = 'none';
		});

		// 隐藏搜索过滤区域
		if (filterSection) {
			(filterSection as HTMLElement).style.display = 'none';
		}

		// 保存整个元素的HTML内容，以便稍后恢复
		const originalHtml = element.innerHTML;

		// 临时替换勾选SVG图标为Unicode字符以确保PDF中正确显示
		const checkSvgIcons = element.querySelectorAll('svg');

		checkSvgIcons.forEach((svg) => {
			// 只处理勾选图标
			const pathElement = svg.querySelector('path');
			if (pathElement) {
				const pathData = pathElement.getAttribute('d');

				// 检查是否是勾选图标 (Check icon)
				if (pathData && pathData.includes('M406.656 706.944')) {
					// 获取父元素的计算样式来继承颜色
					const computedStyle = window.getComputedStyle(svg);
					const inheritedColor = computedStyle.color || 'currentColor';

					// 创建替代的文本节点
					const textSpan = document.createElement('span');
					textSpan.textContent = '✓';
					textSpan.style.fontSize = computedStyle.fontSize || '12px';
					textSpan.style.color = inheritedColor;
					textSpan.style.display = 'inline-block';
					textSpan.style.width = '12px';
					textSpan.style.height = '12px';
					textSpan.style.textAlign = 'center';
					textSpan.style.lineHeight = '12px';
					textSpan.style.fontWeight = 'bold';
					textSpan.style.verticalAlign = 'middle';

					// 替换SVG
					if (svg.parentNode) {
						svg.parentNode.replaceChild(textSpan, svg);
					}
				}
			}
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
			foreignObjectRendering: false, // 禁用外部对象渲染以提高兼容性
			imageTimeout: 15000, // 增加图像超时时间
			onclone: (clonedDoc) => {
				// 在克隆的文档中确保所有文字都可见
				const clonedElement = clonedDoc.querySelector('.pb-6.bg-gray-50');
				if (clonedElement) {
					// 特别处理Element Plus标签内的文字
					const elTags = clonedElement.querySelectorAll('.el-tag');
					elTags.forEach((tag: any) => {
						// 确保标签内的所有文字都是黑色且可见
						const tagContent = tag.querySelector('.el-tag__content');
						if (tagContent) {
							tagContent.style.color = '#000000 !important';
							tagContent.style.opacity = '1';
							tagContent.style.zIndex = '999';
						}

						// 处理标签内的所有文本节点
						const allTextElements = tag.querySelectorAll('*');
						allTextElements.forEach((textEl: any) => {
							textEl.style.color = '#000000 !important';
							textEl.style.opacity = '1';
						});

						// 直接设置标签本身的文字颜色
						tag.style.color = '#000000 !important';
					});

					// 强制所有其他文本元素使用黑色
					const textElements = clonedElement.querySelectorAll('*');
					textElements.forEach((el: any) => {
						const computedStyle = window.getComputedStyle(el);
						if (computedStyle.color && computedStyle.color !== 'rgb(0, 0, 0)') {
							el.style.color = '#000000 !important';
						}
					});
				}
			},
		});

		// 恢复原始HTML内容
		element.innerHTML = originalHtml;

		// 恢复导出按钮和搜索过滤区域显示
		exportButtons.forEach((btn) => {
			(btn as HTMLElement).style.display = '';
		});

		if (filterSection) {
			(filterSection as HTMLElement).style.display = '';
		}

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
			`Customer_Overview_${customerData.value?.leadName}_${customerData.value?.leadId}.pdf`
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
	currentPage.value = 1; // Reset to first page
	// Clear cache for filtered results
	const cacheKeys = Array.from(computedCache.keys()).filter((key) => key.startsWith('filtered_'));
	cacheKeys.forEach((key) => computedCache.delete(key));
};

const clearFilters = () => {
	searchTerm.value = '';
	selectedQuestionnaires.value = [];
	selectedSections.value = [];
	appliedSearchTerm.value = '';
	appliedQuestionnaires.value = [];
	appliedSections.value = [];
	currentPage.value = 1;
	// Clear filtered cache
	const cacheKeys = Array.from(computedCache.keys()).filter((key) => key.startsWith('filtered_'));
	cacheKeys.forEach((key) => computedCache.delete(key));
};

// Cleanup on unmount
const cleanup = () => {
	computedCache.clear();
};

// Watch for route parameter changes
watch(
	() => route.params.leadId,
	(newId) => {
		console.log('Route parameter changed to:', newId);
		if (newId && newId !== 'undefined') {
			// Clear cache when switching to a different onboarding
			computedCache.clear();
			loadData();
		}
	}
);

// Load data on mount
onMounted(() => {
	console.log('Customer Overview mounted');
	console.log('Route params:', route.params);
	console.log('Route query:', route.query);
	console.log('onboardingId:', onboardingId.value);

	if (!onboardingId.value || onboardingId.value === 'undefined') {
		ElMessage.error('Missing onboarding ID in route parameters');
		console.error('Invalid onboarding ID:', onboardingId.value);
		return;
	}
	loadData();
});

// Question type checking methods
const isShortAnswerType = (type: string): boolean => {
	return ['short_answer', 'text'].includes(type);
};

const isParagraphType = (type: string): boolean => {
	return ['paragraph', 'long_answer', 'textarea'].includes(type);
};

const isMultipleChoiceType = (type: string): boolean => {
	return ['multiple_choice', 'radio'].includes(type);
};

const isCheckboxType = (type: string): boolean => {
	return ['checkboxes', 'checkbox'].includes(type);
};

const isDropdownType = (type: string): boolean => {
	return ['dropdown', 'select'].includes(type);
};

const isDateTimeType = (type: string): boolean => {
	return ['date', 'time', 'datetime'].includes(type);
};

const isRatingType = (type: string): boolean => {
	return type === 'rating';
};

const isLinearScaleType = (type: string): boolean => {
	return type === 'linear_scale';
};

// 获取线性量表的最大值
const getLinearScaleMax = (questionConfig: any): number => {
	// 尝试从不同可能的配置字段获取最大值
	if (questionConfig?.max !== undefined) return questionConfig.max;
	if (questionConfig?.maxValue !== undefined) return questionConfig.maxValue;
	if (questionConfig?.scale?.max !== undefined) return questionConfig.scale.max;
	if (questionConfig?.scaleMax !== undefined) return questionConfig.scaleMax;
	if (questionConfig?.range?.max !== undefined) return questionConfig.range.max;
	if (questionConfig?.settings?.max !== undefined) return questionConfig.settings.max;

	// 默认返回10（根据你的需求）
	return 10;
};

const isFileUploadType = (type: string): boolean => {
	return ['file', 'file_upload'].includes(type);
};

const isCheckboxGridType = (type: string): boolean => {
	return type === 'checkbox_grid';
};

const isMultipleChoiceGridType = (type: string): boolean => {
	return type === 'multiple_choice_grid';
};

// Answer formatting methods
const getCheckboxAnswers = (answer: any): string[] => {
	if (!answer) return [];

	// If it's already an array, return it
	if (Array.isArray(answer)) {
		return answer.map((item) => String(item)).filter(Boolean);
	}

	// If it's not a string, convert it to string first
	const answerStr = String(answer);

	try {
		// Try to parse as JSON array first
		const parsed = JSON.parse(answerStr);
		if (Array.isArray(parsed)) {
			return parsed.map((item) => String(item)).filter(Boolean);
		}
		// If it's a comma-separated string
		return answerStr
			.split(',')
			.map((item) => item.trim())
			.filter(Boolean);
	} catch {
		// Fallback to comma-separated string
		return answerStr
			.split(',')
			.map((item) => item.trim())
			.filter(Boolean);
	}
};

const getFileAnswers = (answer: any): Array<{ name: string; size?: number }> => {
	if (!answer) return [];

	// If it's already an array, process each item
	if (Array.isArray(answer)) {
		return answer.map((file) => {
			if (typeof file === 'object' && file !== null) {
				return {
					name: file.name || file.fileName || 'Unknown file',
					size: file.size || file.fileSize,
				};
			}
			return { name: String(file) };
		});
	}

	// If it's an object (single file)
	if (typeof answer === 'object' && answer !== null) {
		return [
			{
				name: answer.name || answer.fileName || 'Unknown file',
				size: answer.size || answer.fileSize,
			},
		];
	}

	// Convert to string if not already
	const answerStr = String(answer);

	// Check if it's "[object Object]" - this indicates improper serialization
	if (answerStr === '[object Object]') {
		return [{ name: 'File uploaded (name not available)' }];
	}

	try {
		const parsed = JSON.parse(answerStr);
		if (Array.isArray(parsed)) {
			return parsed.map((file) => {
				if (typeof file === 'object' && file !== null) {
					return {
						name: file.name || file.fileName || 'Unknown file',
						size: file.size || file.fileSize,
					};
				}
				return { name: String(file) };
			});
		}
		if (typeof parsed === 'object' && parsed !== null) {
			return [
				{
					name: parsed.name || parsed.fileName || 'Unknown file',
					size: parsed.size || parsed.fileSize,
				},
			];
		}
		return [{ name: answerStr }];
	} catch {
		return [{ name: answerStr }];
	}
};

const formatAnswerDate = (dateStr: any): string => {
	if (!dateStr) return '';

	// Convert to string if not already
	const dateString = String(dateStr);

	try {
		const date = new Date(dateString);
		// Check if the date is valid
		if (isNaN(date.getTime())) {
			return dateString;
		}
		return date.toLocaleDateString();
	} catch {
		return dateString;
	}
};

const formatFileSize = (bytes?: number): string => {
	if (!bytes) return '';
	const sizes = ['Bytes', 'KB', 'MB', 'GB'];
	if (bytes === 0) return '0 Bytes';
	const i = Math.floor(Math.log(bytes) / Math.log(1024));
	return Math.round((bytes / Math.pow(1024, i)) * 100) / 100 + ' ' + sizes[i];
};

// Cleanup on unmount
onUnmounted(() => {
	cleanup();
});
</script>

<style scoped lang="scss">
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

/* 表格全宽样式 */
:deep(.el-table) {
	width: 100% !important;
}

:deep(.el-table__body-wrapper) {
	width: 100% !important;
}

:deep(.el-table__header-wrapper) {
	width: 100% !important;
}

/* Section 标签样式 */
.section-tag {
	transition: all 0.2s ease;

	&:hover {
		background-color: #f5f5f5 !important;
		transform: translateY(-1px);
		box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
	}
}

/* 选择类型答案样式优化 */
.checkbox-answers,
.single-choice-answer,
.dropdown-answer,
.grid-answer {
	.checkbox-tag,
	.choice-tag,
	.dropdown-tag,
	.grid-tag {
		display: inline-flex;
		align-items: center;
		margin: 2px;
		border-radius: 12px;
		font-weight: 500;
		transition: all 0.2s ease;

		.el-icon {
			opacity: 0.8;
		}

		&:hover {
			transform: translateY(-1px);
			box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
		}
	}

	.flex {
		max-width: 100%;
	}
}

/* 确保卡片内容占满宽度 */
:deep(.el-card__body) {
	padding: 20px;
	width: 100%;
	color: #000;
}

/* 表格响应式调整 */
@media (max-width: 1024px) {
	:deep(.el-table .el-table__cell) {
		padding: 8px 4px;
	}
}

/* 确保标签内容在PDF导出时可见 */
:deep(.el-tag .el-tag__content) {
	position: relative !important;
	z-index: 10 !important;
	color: inherit !important;
	font-weight: bold !important;
}
</style>
