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
									<div>
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
										<span class="question-number">{{ row.questionNumber }}.</span>
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
												v-if="hasValidAnswer(row.answer)"
												type="primary"
												size="small"
												effect="light"
												class="choice-tag"
											>
												<el-icon class="mr-1" size="12">
													<Check />
												</el-icon>
												{{
													getMultipleChoiceLabel(
														row.answer,
														row.questionConfig
													)
												}}
											</el-tag>
										</div>

										<!-- 多选题 -->
										<div
											v-else-if="isCheckboxType(row.questionType)"
											class="checkbox-answers"
										>
											<template
												v-if="
													getCheckboxLabels(
														row.answer,
														row.questionConfig,
														row.responseText,
														row.id
													).length > 0
												"
											>
												<div class="flex flex-wrap gap-1">
													<el-tag
														v-for="(item, index) in getCheckboxLabels(
															row.answer,
															row.questionConfig,
															row.responseText,
															row.id
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
													{{
														getCheckboxLabels(
															row.answer,
															row.questionConfig,
															row.responseText,
															row.id
														).length
													}}
													option{{
														getCheckboxLabels(
															row.answer,
															row.questionConfig,
															row.responseText,
															row.id
														).length > 1
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
												{{
													getDropdownLabel(row.answer, row.questionConfig)
												}}
											</el-tag>
										</div>

										<!-- 日期/时间 -->
										<div
											v-else-if="isDateTimeType(row.questionType)"
											class="text-gray-700"
										>
											<template v-if="row.answer">
												<el-icon class="mr-1">
													<component :is="row.questionType === 'time' ? 'Clock' : 'Calendar'" />
												</el-icon>
												{{ formatAnswerDate(row.answer, row.questionType) }}
											</template>
										</div>

										<!-- 评分 -->
										<div
											v-else-if="isRatingType(row.questionType)"
											class="flex items-center"
										>
											<template v-if="row.answer">
												<!-- 使用自定义图标显示 -->
												<div class="flex items-center">
													<component
														v-for="i in parseInt(String(row.answer))"
														:key="`filled-${i}`"
														:is="
															getIconForType(
																row.questionConfig?.iconType ||
																	'star',
																true
															)
														"
														class="w-4 h-4 text-yellow-500 mr-1"
													/>
													<component
														v-for="i in getRatingMax(
															row.questionConfig
														) - parseInt(String(row.answer))"
														:key="`empty-${i}`"
														:is="
															getIconForType(
																row.questionConfig?.iconType ||
																	'star',
																false
															)
														"
														class="w-4 h-4 text-gray-300 mr-1"
													/>
												</div>
												<span class="ml-2 text-sm text-gray-600">
													({{ row.answer }}/{{
														getRatingMax(row.questionConfig)
													}})
												</span>
											</template>
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
											<template v-if="row.answer">
												<el-tag
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
											</template>
										</div>

										<!-- 多选网格 (Multiple choice grid) -->
										<div
											v-else-if="isMultipleChoiceGridType(row.questionType)"
											class="grid-answer"
										>
											<template
												v-if="
													getGridAnswerLabels(
														row.answer,
														row.questionConfig,
														row.responseText,
														row.id
													).length > 0
												"
											>
												<div class="flex flex-wrap gap-1">
													<el-tag
														v-for="(item, index) in getGridAnswerLabels(
															row.answer,
															row.questionConfig,
															row.responseText,
															row.id
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
													{{
														getGridAnswerLabels(
															row.answer,
															row.questionConfig,
															row.responseText,
															row.id
														).length
													}}
													grid selection{{
														getGridAnswerLabels(
															row.answer,
															row.questionConfig,
															row.responseText,
															row.id
														).length > 1
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
import '../styles/errorDialog.css';
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
import IconStar from '~icons/mdi/star';
import IconStarOutline from '~icons/mdi/star-outline';
import IconHeart from '~icons/mdi/heart';
import IconHeartOutline from '~icons/mdi/heart-outline';
import IconThumbUp from '~icons/mdi/thumb-up';
import IconThumbUpOutline from '~icons/mdi/thumb-up-outline';
import * as XLSX from 'xlsx-js-style';
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
	questionNumber: number; // 题目序号
	responseText?: string; // 原始responseText，用于解析Other选项
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

// Removed pagination - showing all data

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

		// Process current questionnaire's answers
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
			section.questions?.forEach((question: any, questionIndex: number) => {
				// 检查是否是网格类型的问题
				const isGridType =
					question.type === 'checkbox_grid' || question.type === 'multiple_choice_grid';

				// Try to find answer using various possible question IDs
				let answerData: any = null;
				const possibleIds = [
					question.id,
					`question-${question.id}`,
					question.questionId,
					question.identifier,
				].filter(Boolean);

				for (const possibleId of possibleIds) {
					if (answersMap.has(possibleId)) {
						answerData = answersMap.get(possibleId);
						break;
					}
				}

				// Try to find grid answers using the same possible IDs
				let gridAnswers: any[] = [];
				for (const possibleId of possibleIds) {
					const foundGridAnswers = gridAnswersMap.get(possibleId);
					if (foundGridAnswers && foundGridAnswers.length > 0) {
						gridAnswers = foundGridAnswers;
						break;
					}
				}

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
							answer: gridAnswer.answer || gridAnswer.responseText || '',
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
							section: section.name,
							required: question.required || false,
							questionConfig: question.config || question, // 存储完整的问题配置
							questionNumber: questionIndex + 1, // 添加题目序号
							responseText: gridAnswer.responseText || answer?.answerJson || '', // 保存原始responseText
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

					// 处理答案，优先使用responseText，如果没有则使用answer
					let processedAnswer = '';
					if (question.type === 'file' || question.type === 'file_upload') {
						// 对于文件类型，使用原始answer字段
						processedAnswer = answerData?.answer || '';
					} else {
						// 对于其他类型，优先使用responseText，如果没有则使用answer
						processedAnswer = answerData?.answer || answerData?.responseText || '';
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
						section: section.name,
						required: question.required || false,
						questionConfig: question.config || question, // 存储完整的问题配置
						questionNumber: questionIndex + 1, // 添加题目序号
						responseText: answerData?.responseText || answer?.answerJson || '', // 保存原始responseText
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
			Object.entries(batchQuestionnaireResponse.stageQuestionnaires).forEach(
				([stageId, stageQuestionnaires]: [string, any]) => {
					if (Array.isArray(stageQuestionnaires)) {
						// 为每个问卷添加stageId信息
						const questionnairesWithStageId = stageQuestionnaires.map(
							(questionnaire: any) => ({
								...questionnaire,
								stageId: stageId,
							})
						);
						allQuestionnaires.push(...questionnairesWithStageId);
					}
				}
			);
		}
		questionnairesData.value = allQuestionnaires;

		// Process batch answer results
		const answersMap = new Map<string, QuestionnaireAnswer>();
		// Use stage+questionnaire key for strict matching
		const stageQuestionnaireAnswersMap = new Map<string, QuestionnaireAnswer>();
		if (batchAnswerResponse?.stageAnswers) {
			Object.entries(batchAnswerResponse.stageAnswers).forEach(
				([stageId, answerData]: [string, any]) => {
					if (answerData) {
						// Check if it's a single answer or multiple answers
						if (answerData.id && answerData.questionnaireId) {
							// Single answer object
							answersMap.set(stageId, answerData);
							// Store with stage+questionnaire key for strict matching
							const key = `${stageId}:${answerData.questionnaireId}`;
							stageQuestionnaireAnswersMap.set(key, answerData);
						} else if (typeof answerData === 'object' && !answerData.id) {
							// Multiple answers grouped by questionnaireId
							answersMap.set(stageId, answerData);
							Object.entries(answerData).forEach(
								([questionnaireId, answer]: [string, any]) => {
									if (answer && answer.id) {
										// Store with stage+questionnaire key for strict matching
										const key = `${stageId}:${questionnaireId}`;
										stageQuestionnaireAnswersMap.set(key, answer);
									}
								}
							);
						}
					} else {
						// No answer
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
				// Find answer by strict stage+questionnaire matching
				let answer: QuestionnaireAnswer | null = null;

				// Use strict stage+questionnaire key lookup
				const key = `${questionnaire.stageId}:${questionnaire.id}`;
				answer = stageQuestionnaireAnswersMap.get(key) || null;

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
				const answerText = String(response.answer || '').toLowerCase();
				const matchesSearch =
					!appliedSearchTerm.value ||
					response.question.toLowerCase().includes(searchLower) ||
					answerText.includes(searchLower) ||
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

// Removed pagination - showing all data
const paginatedData = computed(() => filteredData.value);

// Optimized response calculations
const totalResponsesCount = computed(() => {
	return filteredData.value.reduce((total, q) => {
		// 只统计有答案的问题
		const answeredResponses = q.responses.filter((response) => {
			const isValid = hasValidAnswer(response.answer);
			return isValid;
		});
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
					questionNumber: response.questionNumber,
					question: response.question,
					answer: response.answer,
					answeredBy: response.answeredBy,
					answeredDate: response.answeredDate ? formatDateUS(response.answeredDate) : '',
					lastUpdated: response.lastUpdated ? formatDateUS(response.lastUpdated) : '',
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
			// 处理答案显示，确保所有选择类型都显示正确的 label
			let displayAnswer = response.answer || '';

			// 如果是多选表格类型，转换为label显示
			if (response.questionType === 'multiple_choice_grid' && response.questionConfig) {
				const labels = getGridAnswerLabels(response.answer, response.questionConfig, response.responseText, response.id);
				displayAnswer = labels.join(', ');
			}
			// 如果是多选类型，转换为label显示
			else if (response.questionType === 'checkboxes' && response.answer) {
				const labels = getCheckboxLabels(response.answer, response.questionConfig, response.responseText, response.id);
				displayAnswer = labels.join(', ');
			}
			// 如果是单选类型，转换为label显示
			else if (response.questionType === 'multiple_choice' && response.answer) {
				displayAnswer = getMultipleChoiceLabel(response.answer, response.questionConfig);
			}
			// 如果是下拉选择类型，转换为label显示
			else if (response.questionType === 'dropdown' && response.answer) {
				displayAnswer = getDropdownLabel(response.answer, response.questionConfig);
			}
			// 其他类型保持原样
			else {
				displayAnswer = response.answer || '';
			}

			// Include ALL questions, regardless of whether they have answers
			responses.push({
				questionnaire: questionnaire.name,
				section: response.section,
				questionNumber: response.questionNumber,
				question: response.question,
				answer: displayAnswer,
				answeredBy: response.answeredBy || '',
				answeredDate: response.answeredDate ? formatDateUS(response.answeredDate) : '',
				lastUpdated: response.lastUpdated ? formatDateUS(response.lastUpdated) : '',
				updatedBy: response.updatedBy || '',
			});
		});
	});
	return responses;
});

// Filtered questions for export (based on current filters and search)
const filteredQuestionsForExport = computed(() => {
	const responses: any[] = [];
	filteredData.value.forEach((questionnaire) => {
		questionnaire.responses.forEach((response) => {
			// 处理答案显示，确保所有选择类型都显示正确的 label
			let displayAnswer = response.answer || '';
			
			// 如果是多选表格类型，转换为label显示
			if (response.questionType === 'multiple_choice_grid' && response.questionConfig) {
				const labels = getGridAnswerLabels(response.answer, response.questionConfig, response.responseText, response.id);
				displayAnswer = labels.join(', ');
			}
			// 如果是多选类型，转换为label显示
			else if (response.questionType === 'checkboxes' && response.answer) {
				const labels = getCheckboxLabels(response.answer, response.questionConfig, response.responseText, response.id);
				displayAnswer = labels.join(', ');
			}
			// 如果是单选类型，转换为label显示
			else if (response.questionType === 'multiple_choice' && response.answer) {
				displayAnswer = getMultipleChoiceLabel(response.answer, response.questionConfig);
			}
			// 如果是下拉选择类型，转换为label显示
			else if (response.questionType === 'dropdown' && response.answer) {
				displayAnswer = getDropdownLabel(response.answer, response.questionConfig);
			}
			// 其他类型保持原样
			else {
				displayAnswer = response.answer || '';
			}
			
			// Include ALL filtered questions, regardless of whether they have answers
			responses.push({
				questionnaire: questionnaire.name,
				section: response.section,
				questionNumber: response.questionNumber,
				question: response.question,
				answer: displayAnswer,
				answeredBy: response.answeredBy || '',
				answeredDate: response.answeredDate ? formatDateUS(response.answeredDate) : '',
				lastUpdated: response.lastUpdated ? formatDateUS(response.lastUpdated) : '',
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

// Removed pagination handlers

// 解析responseText中的自定义输入值
const parseResponseText = (responseText: string): { [key: string]: string } => {
	if (!responseText || responseText.trim() === '{}') {
		return {};
	}
	
	try {
		// 处理Unicode编码的字符串
		let decodedText = responseText;
		
		// 替换Unicode编码的字符
		decodedText = decodedText.replace(/u0022/g, '"');
		decodedText = decodedText.replace(/u0020/g, ' ');
		decodedText = decodedText.replace(/u003A/g, ':');
		decodedText = decodedText.replace(/u002C/g, ',');
		decodedText = decodedText.replace(/u007B/g, '{');
		decodedText = decodedText.replace(/u007D/g, '}');
		
		// 尝试解析JSON
		const parsed = JSON.parse(decodedText);
		return parsed || {};
	} catch (error) {
		console.warn('Failed to parse responseText:', responseText, error);
		return {};
	}
};

// 从responseText中提取Other选项的自定义值
const extractOtherValues = (responseText: string, questionId: string): { [key: string]: string } => {
	const parsed = parseResponseText(responseText);
	const otherValues: { [key: string]: string } = {};
	

	
	// 查找包含questionId的键
	Object.keys(parsed).forEach(key => {
		if (key.includes(questionId)) {
			// 对于网格类型：查找包含"other"的键
			if (key.includes('other')) {
				// 提取column ID，格式如：question-xxx_row-xxx_column-other-xxx
				const parts = key.split('_');
				const columnPart = parts.find(part => part.startsWith('column-other-'));
				if (columnPart) {
					otherValues[columnPart] = parsed[key];
				}
			}
			// 对于多选题：查找option类型的键
			else if (key.includes('option-') || key.includes('option_')) {
				// 提取option ID，格式如：question-xxx_option-xxx
				const parts = key.split('_');
				let optionPart = parts.find(part => part.startsWith('option-'));
				if (optionPart) {
					// 同时支持 option- 和 option_ 格式
					const alternativeKey = optionPart.replace('option-', 'option_');
					otherValues[optionPart] = parsed[key];
					otherValues[alternativeKey] = parsed[key];
				}
			}
		}
	});
	return otherValues;
};

// Utility functions
const formatDateUS = (dateString: string) => {
	if (!dateString) return '';
	try {
		const date = new Date(dateString);
		if (isNaN(date.getTime())) {
			return dateString;
		}
		
		// Format as MM/dd/yyyy HH:mm:ss (US format)
		const month = String(date.getMonth() + 1).padStart(2, '0');
		const day = String(date.getDate()).padStart(2, '0');
		const year = date.getFullYear();
		const hours = String(date.getHours()).padStart(2, '0');
		const minutes = String(date.getMinutes()).padStart(2, '0');
		const seconds = String(date.getSeconds()).padStart(2, '0');
		
		return `${month}/${day}/${year} ${hours}:${minutes}:${seconds}`;
	} catch {
		return dateString;
	}
};

// Legacy function kept for compatibility, now uses US format
const formatDate = (dateString: string) => {
	return formatDateUS(dateString);
};

// 判断答案是否有效（有实际内容）
const hasValidAnswer = (answer: string | any): boolean => {
	if (!answer) return false;
	if (typeof answer === 'string') {
		const trimmed = answer.trim();
		// 检查空的JSON对象字符串
		if (trimmed === '{}' || trimmed === '[]') {
			return false;
		}
		return (
			trimmed !== '' &&
			trimmed !== 'No answer provided' &&
			trimmed !== 'No selection made' &&
			trimmed !== 'null' &&
			trimmed !== 'undefined'
		);
	}
	if (Array.isArray(answer)) {
		return answer.length > 0;
	}
	if (typeof answer === 'object' && answer !== null) {
		// 检查是否是空对象
		return Object.keys(answer).length > 0;
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
		// Use filtered data to match what user sees on screen
		const exportData = filteredQuestionsForExport.value;

		if (exportData.length === 0) {
			ElMessage.warning('No questionnaire data available to export with current filters');
			return;
		}

		// Define headers explicitly
		const headers = [
			'Questionnaire',
			'Section',
			'No.',
			'Question',
			'Answer',
			'Answered By',
			'Answered Date',
			'Last Updated',
			'Updated By'
		];

		// Create worksheet with headers first
		const worksheet = XLSX.utils.aoa_to_sheet([headers]);
		
		// Add data starting from row 2
		XLSX.utils.sheet_add_json(worksheet, exportData, { 
			origin: 'A2', 
			skipHeader: true 
		});

		// Apply bold formatting to header row only
		const headerCells = ['A1', 'B1', 'C1', 'D1', 'E1', 'F1', 'G1', 'H1', 'I1'];
		headerCells.forEach(cellAddress => {
			if (worksheet[cellAddress]) {
				worksheet[cellAddress].s = {
					font: { 
						bold: true
					}
				};
			}
		});

		// Set column widths for better readability
		worksheet['!cols'] = [
			{ wch: 20 }, // Questionnaire
			{ wch: 15 }, // Section
			{ wch: 5 },  // No.
			{ wch: 50 }, // Question
			{ wch: 30 }, // Answer
			{ wch: 15 }, // Answered By
			{ wch: 18 }, // Answered Date
			{ wch: 18 }, // Last Updated
			{ wch: 15 }  // Updated By
		];
		
		const workbook = XLSX.utils.book_new();
		XLSX.utils.book_append_sheet(workbook, worksheet, 'Customer Overview');
		
		// Add filter info to filename if filters are active
		let filename = `Customer_Overview_${customerData.value?.leadName}_${customerData.value?.leadId}`;
		if (hasActiveFilters.value) {
			filename += '_Filtered';
		}
		filename += '.xlsx';
		
		// Write file with styling options
		XLSX.writeFile(workbook, filename, { 
			bookType: 'xlsx',
			cellStyles: true,
			sheetStubs: false
		});
		
		const filterInfo = hasActiveFilters.value ? ' (filtered data)' : '';
		ElMessage.success(`Excel file exported successfully with ${exportData.length} questions${filterInfo}`);
	} catch (error) {
		console.error('Export Excel failed:', error);
		ElMessage.error('Failed to export Excel file');
	}
};

const handleExportPDF = async () => {
	try {
		console.log('[PDF Export] Starting PDF export process...');
		ElMessage.info('Generating PDF, please wait...');

		// 获取页面内容元素
		const sourceElement = document.querySelector('.pb-6.bg-gray-50') as HTMLElement;
		console.log('[PDF Export] Found main element:', !!sourceElement);
		if (!sourceElement) {
			throw new Error('Page content not found');
		}

		// 创建克隆元素用于PDF导出，避免影响原页面
		const clonedElement = sourceElement.cloneNode(true) as HTMLElement;

		// 在克隆元素中应用PDF优化样式
		// 1. 移除导出按钮和搜索过滤区域
		const exportButtons = clonedElement.querySelectorAll('.flex.items-center.space-x-2');
		exportButtons.forEach((btn) => btn.remove());

		const filterSection = clonedElement.querySelector('el-card .pt-6');
		if (filterSection) {
			filterSection.remove();
		}

		// 2. 确保标题完全可见
		const titleElement = clonedElement.querySelector('h1') as HTMLElement;
		if (titleElement) {
			titleElement.style.whiteSpace = 'nowrap';
			titleElement.style.overflow = 'visible';
			titleElement.style.textOverflow = 'clip';
			titleElement.style.maxWidth = 'none';
			titleElement.style.fontSize = '1.5rem';
			titleElement.style.fontWeight = '700';
			titleElement.style.color = '#111827';
			titleElement.style.display = 'block';
			titleElement.style.visibility = 'visible';
		}

		// 3. 确保Summary部分完整可见并手动创建内容
		console.log('[PDF Export] Searching for Summary section...');

		// 尝试多种选择器查找Summary部分
		let summarySection = clonedElement.querySelector('el-card.mt-6') as HTMLElement;
		console.log('[PDF Export] Found with el-card.mt-6:', !!summarySection);

		if (!summarySection) {
			summarySection = clonedElement.querySelector('.mt-6') as HTMLElement;
			console.log('[PDF Export] Found with .mt-6:', !!summarySection);
		}

		if (!summarySection) {
			// 尝试查找包含"Response Summary"文字的元素
			const allElements = Array.from(clonedElement.querySelectorAll('*'));
			for (const el of allElements) {
				if (el.textContent?.includes('Response Summary')) {
					summarySection =
						(el.closest('el-card') as HTMLElement) ||
						(el.closest('.el-card') as HTMLElement);
					console.log('[PDF Export] Found by text content:', !!summarySection);
					break;
				}
			}
		}

		// 调试信息
		console.log('[PDF Export] Final Summary section found:', !!summarySection);
		console.log('[PDF Export] Filtered data length:', filteredData.value.length);
		console.log(
			'[PDF Export] Will create manually:',
			!summarySection && filteredData.value.length > 0
		);

		// 强制创建Summary部分以确保显示
		if (filteredData.value.length > 0) {
			// 如果找到了现有的Summary，先移除它
			if (summarySection) {
				console.log('[PDF Export] Removing existing Summary section');
				summarySection.remove();
			}
			console.log('[PDF Export] Creating Summary section manually...');
			summarySection = document.createElement('div');
			summarySection.className = 'el-card is-always-shadow mt-6';
			// 应用与Element Plus卡片相同的样式
			summarySection.style.marginTop = '24px';
			summarySection.style.border = '1px solid #ebeef5';
			summarySection.style.borderRadius = '4px';
			summarySection.style.backgroundColor = '#ffffff';
			summarySection.style.boxShadow = '0 2px 12px 0 rgba(0, 0, 0, 0.1)';
			summarySection.style.overflow = 'hidden';
			summarySection.style.display = 'block';
			summarySection.style.visibility = 'visible';

			// 创建标题部分
			const headerDiv = document.createElement('div');
			headerDiv.style.padding = '18px 20px';
			headerDiv.style.borderBottom = '1px solid #ebeef5';
			headerDiv.style.backgroundColor = '#fafafa';
			headerDiv.style.fontSize = '1.125rem';
			headerDiv.style.fontWeight = '500';
			headerDiv.style.color = '#303133';
			headerDiv.textContent = 'Response Summary';

			// 创建内容部分
			const contentDiv = document.createElement('div');
			contentDiv.style.padding = '20px';

			// 创建网格容器
			const gridDiv = document.createElement('div');
			gridDiv.style.display = 'grid';
			gridDiv.style.gridTemplateColumns = 'repeat(4, 1fr)';
			gridDiv.style.gap = '1rem';

			// 创建统计项目
			const statsData = [
				{ value: filteredData.value.length, label: 'Questionnaires', color: '#2563eb' },
				{ value: totalResponsesCount.value, label: 'Total Responses', color: '#16a34a' },
				{ value: sections.value.length, label: 'Sections', color: '#9333ea' },
				{ value: uniqueContributors.value, label: 'Contributors', color: '#ea580c' },
			];

			statsData.forEach((stat) => {
				const statDiv = document.createElement('div');
				statDiv.style.textAlign = 'center';

				const valueDiv = document.createElement('div');
				valueDiv.style.fontSize = '1.5rem';
				valueDiv.style.lineHeight = '2rem';
				valueDiv.style.fontWeight = '700';
				valueDiv.style.color = stat.color;
				valueDiv.textContent = String(stat.value);

				const labelDiv = document.createElement('div');
				labelDiv.style.fontSize = '0.875rem';
				labelDiv.style.lineHeight = '1.25rem';
				labelDiv.style.color = '#6b7280';
				labelDiv.textContent = stat.label;

				statDiv.appendChild(valueDiv);
				statDiv.appendChild(labelDiv);
				gridDiv.appendChild(statDiv);
			});

			contentDiv.appendChild(gridDiv);
			summarySection.appendChild(headerDiv);
			summarySection.appendChild(contentDiv);
			// 确保Summary section添加到正确的位置（内容的最后）
			const mainContent = clonedElement.querySelector('.space-y-6') || clonedElement;
			mainContent.appendChild(summarySection);
			console.log('[PDF Export] Summary section created and appended successfully');

			// 验证Summary section是否真的被添加了
			const verifySection = clonedElement.querySelector('.mt-6');
			console.log('[PDF Export] Summary section verification:', !!verifySection);
			if (verifySection) {
				console.log(
					'[PDF Export] Summary section content:',
					verifySection.textContent?.substring(0, 100)
				);
			}
		} else if (summarySection) {
			console.log('[PDF Export] Existing Summary section found, applying styles');
		}

		if (summarySection) {
			summarySection.style.pageBreakInside = 'avoid';
			summarySection.style.marginTop = '24px';
			summarySection.style.display = 'block';
			summarySection.style.visibility = 'visible';
			summarySection.style.height = 'auto';
			summarySection.style.minHeight = '120px'; // 确保有最小高度
			summarySection.style.width = '100%';
			summarySection.style.position = 'relative';
			summarySection.style.zIndex = '1';
			console.log('[PDF Export] Summary section styles applied');
		}

		// 4. 确保所有统计数字可见
		const statNumbers = clonedElement.querySelectorAll('.text-2xl.font-bold');
		statNumbers.forEach((stat) => {
			const statEl = stat as HTMLElement;
			statEl.style.display = 'block';
			statEl.style.visibility = 'visible';
			statEl.style.fontSize = '1.5rem';
			statEl.style.fontWeight = '700';
		});

		// 5. 替换SVG图标为文本
		const checkSvgIcons = clonedElement.querySelectorAll('svg');
		checkSvgIcons.forEach((svg) => {
			const pathElement = svg.querySelector('path');
			if (pathElement) {
				const pathData = pathElement.getAttribute('d');
				if (pathData && pathData.includes('M406.656 706.944')) {
					const textSpan = document.createElement('span');
					textSpan.textContent = '✓';
					textSpan.style.fontSize = '12px';
					textSpan.style.color = 'currentColor';
					textSpan.style.display = 'inline-block';
					textSpan.style.width = '12px';
					textSpan.style.height = '12px';
					textSpan.style.textAlign = 'center';
					textSpan.style.lineHeight = '12px';
					textSpan.style.fontWeight = 'bold';
					if (svg.parentNode) {
						svg.parentNode.replaceChild(textSpan, svg);
					}
				}
			}
		});

		// 6. 设置克隆元素的基本样式
		clonedElement.style.position = 'fixed';
		clonedElement.style.top = '0px';
		clonedElement.style.left = '0px';
		clonedElement.style.width = sourceElement.offsetWidth + 'px';
		clonedElement.style.height = 'auto';
		clonedElement.style.backgroundColor = '#ffffff';
		clonedElement.style.zIndex = '-1000';
		clonedElement.style.visibility = 'hidden';
		clonedElement.style.pointerEvents = 'none';

		// 将克隆元素添加到文档中
		document.body.appendChild(clonedElement);

		// 等待渲染完成
		await nextTick();

		// 给元素更多时间进行布局
		await new Promise((resolve) => setTimeout(resolve, 300));

		// 最终验证Summary section的存在和位置
		const finalSummaryCheck = clonedElement.querySelector('.mt-6');
		console.log('[PDF Export] Final Summary check before canvas:', !!finalSummaryCheck);
		console.log('[PDF Export] Cloned element height:', clonedElement.scrollHeight);
		console.log(
			'[PDF Export] Summary section position:',
			finalSummaryCheck?.getBoundingClientRect()
		);

		// 确保Summary section在正确位置并可见
		const summaryInClone = clonedElement.querySelector('.mt-6') as HTMLElement;
		if (summaryInClone) {
			summaryInClone.style.display = 'block';
			summaryInClone.style.visibility = 'visible';
			summaryInClone.style.opacity = '1';
			summaryInClone.style.position = 'static';
			summaryInClone.style.width = '100%';
			summaryInClone.style.minHeight = '150px';
			summaryInClone.style.backgroundColor = '#ffffff';
			summaryInClone.style.border = '1px solid #ddd';
			summaryInClone.style.borderRadius = '8px';
			summaryInClone.style.padding = '20px';
			summaryInClone.style.marginTop = '30px';
			console.log('[PDF Export] Summary section final styling applied');
		}

		// 最后强制确保Summary section可见
		const finalSummary = clonedElement.querySelector('.mt-6') as HTMLElement;
		if (finalSummary) {
			finalSummary.style.display = 'block !important';
			finalSummary.style.visibility = 'visible !important';
			finalSummary.style.opacity = '1 !important';
			finalSummary.style.position = 'static !important';
			finalSummary.style.height = 'auto !important';
			finalSummary.style.width = '100% !important';
			finalSummary.style.transform = 'none !important';
			finalSummary.style.left = 'auto !important';
			finalSummary.style.top = 'auto !important';
			console.log('[PDF Export] Final summary forced visible');
		}

		// 临时将克隆元素设置为可见用于html2canvas
		clonedElement.style.visibility = 'visible';
		clonedElement.style.position = 'absolute';
		clonedElement.style.top = '0px';
		clonedElement.style.left = '0px';

		// 强制重新计算布局
		clonedElement.offsetHeight; // 触发回流

		// 再次等待一下确保布局完成
		await new Promise((resolve) => setTimeout(resolve, 100));

		// 最终确认Summary section的完整性
		const finalSummaryForCanvas = clonedElement.querySelector('.mt-6') as HTMLElement;
		if (finalSummaryForCanvas) {
			// 强制设置所有可能影响渲染的样式
			finalSummaryForCanvas.style.cssText = `
				display: block !important;
				visibility: visible !important;
				opacity: 1 !important;
				position: relative !important;
				height: auto !important;
				width: 100% !important;
				margin-top: 24px !important;
				background-color: #ffffff !important;
				border: 1px solid #ebeef5 !important;
				border-radius: 4px !important;
				box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1) !important;
				overflow: visible !important;
				transform: none !important;
				left: auto !important;
				top: auto !important;
				right: auto !important;
				bottom: auto !important;
				z-index: 1 !important;
			`;

			// 确保内部元素也可见
			const summaryChildren = finalSummaryForCanvas.querySelectorAll('*');
			summaryChildren.forEach((child) => {
				const childEl = child as HTMLElement;
				childEl.style.visibility = 'visible';
				childEl.style.opacity = '1';
			});

			console.log('[PDF Export] Final Summary forced for canvas with cssText');
		}

		// 生成canvas
		console.log('[PDF Export] Starting html2canvas generation...');
		const canvas = await html2canvas(clonedElement, {
			scale: 1, // 降低缩放，避免渲染问题
			useCORS: true,
			allowTaint: true,
			backgroundColor: '#ffffff',
			width: clonedElement.offsetWidth,
			height: clonedElement.scrollHeight,
			logging: true, // 开启日志以调试
			removeContainer: false, // 保留容器
			foreignObjectRendering: true, // 启用外部对象渲染
			imageTimeout: 15000,
			scrollX: 0,
			scrollY: 0,
			onclone: (clonedDoc) => {
				// 在克隆文档中再次确保Summary可见
				const summaryInClonedDoc = clonedDoc.querySelector('.mt-6') as HTMLElement;
				if (summaryInClonedDoc) {
					summaryInClonedDoc.style.display = 'block';
					summaryInClonedDoc.style.visibility = 'visible';
					summaryInClonedDoc.style.opacity = '1';
					console.log('[PDF Export] Summary ensured in onclone callback');
				}
			},
		});

		// 移除克隆元素
		document.body.removeChild(clonedElement);

		console.log('[PDF Export] Canvas dimensions:', canvas.width, 'x', canvas.height);
		console.log('[PDF Export] Canvas created successfully');

		// 动态调整图片质量
		let quality = 0.8;
		let imgData = canvas.toDataURL('image/jpeg', quality);

		while (imgData.length > 2000000 && quality > 0.4) {
			quality -= 0.1;
			imgData = canvas.toDataURL('image/jpeg', quality);
		}

		const pdf = new jsPDF({
			orientation: 'portrait',
			unit: 'mm',
			format: 'a4',
			compress: true,
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
		console.log('[PDF Export] PDF file saved successfully');
		ElMessage.success('PDF file exported successfully');
	} catch (error) {
		console.error('[PDF Export] Export PDF failed:', error);
		ElMessage.error('Failed to export PDF file');
	}
};

const applyFilters = () => {
	appliedSearchTerm.value = searchTerm.value;
	appliedQuestionnaires.value = [...selectedQuestionnaires.value];
	appliedSections.value = [...selectedSections.value];
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
		if (newId && newId !== 'undefined') {
			// Clear cache when switching to a different onboarding
			computedCache.clear();
			loadData();
		}
	}
);

// Load data on mount
onMounted(() => {
	if (!onboardingId.value || onboardingId.value === 'undefined') {
		ElMessage.error('Missing onboarding ID in route parameters');
		console.error('Invalid onboarding ID:', onboardingId.value);
		return;
	}
	loadData();
});

// Question type checking methods
const isShortAnswerType = (type: string): boolean => {
	return ['short_answer'].includes(type);
};

const isParagraphType = (type: string): boolean => {
	return ['paragraph'].includes(type);
};

const isMultipleChoiceType = (type: string): boolean => {
	return ['multiple_choice'].includes(type);
};

const isCheckboxType = (type: string): boolean => {
	return ['checkboxes'].includes(type);
};

const isDropdownType = (type: string): boolean => {
	return ['dropdown'].includes(type);
};

const isDateTimeType = (type: string): boolean => {
	return ['time', 'date'].includes(type);
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

// 获取评分的最大值
const getRatingMax = (questionConfig: any): number => {
	// 尝试从不同可能的配置字段获取最大值
	if (questionConfig?.max !== undefined) return questionConfig.max;
	if (questionConfig?.maxValue !== undefined) return questionConfig.maxValue;
	if (questionConfig?.scale?.max !== undefined) return questionConfig.scale.max;
	if (questionConfig?.scaleMax !== undefined) return questionConfig.scaleMax;
	if (questionConfig?.range?.max !== undefined) return questionConfig.range.max;
	if (questionConfig?.settings?.max !== undefined) return questionConfig.settings.max;

	// 默认返回5（评分通常是5分制）
	return 5;
};

// 根据图标类型获取对应的图标组件
const getIconForType = (iconType: string, filled: boolean) => {
	const type = iconType || 'star';
	const iconConfig = iconOptions[type as keyof typeof iconOptions] || iconOptions.star;
	return filled ? iconConfig.filledIcon : iconConfig.voidIcon;
};

const isFileUploadType = (type: string): boolean => {
	return ['file_upload'].includes(type);
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

// Get label for multiple choice answer
const getMultipleChoiceLabel = (answer: string, questionConfig: any): string => {
	if (!answer || !questionConfig?.options) return answer;
	
	// 检查是否是空的JSON对象字符串
	if (typeof answer === 'string' && (answer.trim() === '{}' || answer.trim() === '[]')) {
		return '';
	}

	// Find the option with matching value
	const option = questionConfig.options.find((opt: any) => opt.value === answer);
	return option?.label || answer;
};

// Get labels for dropdown answer
const getDropdownLabel = (answer: string, questionConfig: any): string => {
	if (!answer || !questionConfig?.options) return answer;

	// Find the option with matching value
	const option = questionConfig.options.find((opt: any) => opt.value === answer);
	return option?.label || answer;
};

// Get labels for checkbox answers
const getCheckboxLabels = (answer: any, questionConfig: any, responseText?: string, questionId?: string): string[] => {
	if (!answer || !questionConfig?.options) {
		return getCheckboxAnswers(answer);
	}

	const answerValues = getCheckboxAnswers(answer);

	// Create a map of value to label
	const optionMap = new Map<string, string>();
	const otherOptionIds = new Set<string>();
	
	questionConfig.options.forEach((option: any) => {
		optionMap.set(option.value, option.label);
		// 识别other类型的选项
		if (option.isOther || 
			option.type === 'other' || 
			option.allowCustom || 
			option.hasInput ||
			(option.label && (
				option.label.toLowerCase().includes('other') || 
				option.label.toLowerCase().includes('enter other') ||
				option.label.toLowerCase().includes('custom') ||
				option.label.toLowerCase().includes('specify')
			))) {
			otherOptionIds.add(option.value);
		}
	});

	// 从responseText中提取Other选项的自定义值
	let otherValues: { [key: string]: string } = {};
	if (responseText && questionId) {
		otherValues = extractOtherValues(responseText, questionId);
	}

	// Convert values to labels
	const labels: string[] = [];
	answerValues.forEach((value) => {
		const optionLabel = optionMap.get(value);
		
		if (optionLabel) {
			// 如果这是一个other类型的选项，显示自定义值
			if (otherOptionIds.has(value)) {
				// 查找对应的自定义值
				const customValue = otherValues[value] || otherValues[value.replace('option_', 'option-')];
				if (customValue) {
					labels.push(`Other: ${customValue}`);
				} else {
					labels.push(optionLabel);
				}
			} else {
				labels.push(optionLabel);
			}
		} else {
			// 对于没有找到对应label的值，检查是否有other自定义值
			const isOtherValue = Object.keys(otherValues).some(otherKey => {
				return otherKey.includes(value) || value.includes(otherKey.replace('option-', '').replace('option_', ''));
			});
			
			if (isOtherValue) {
				// 如果这个值对应一个other选项，查找自定义值
				const customValue = Object.entries(otherValues).find(([key]) => 
					key.includes(value) || value.includes(key.replace('option-', '').replace('option_', ''))
				)?.[1];
				if (customValue) {
					labels.push(`Other: ${customValue}`);
				} else {
					// 如果没有自定义值，跳过不显示（避免显示原始值如"d"）
				}
			} else {
				labels.push(value);
			}
		}
	});

	return labels.filter(Boolean);
};

// 解析网格答案，将column ID转换为对应的label
const getGridAnswerLabels = (answer: any, questionConfig: any, responseText?: string, questionId?: string): string[] => {
	if (!answer || !questionConfig?.columns) return [];

	// 获取原始答案数组
	const answerIds = getCheckboxAnswers(answer);
	


	// 创建column ID到label的映射
	const columnMap = new Map<string, string>();
	const otherColumnIds = new Set<string>();
	
	questionConfig.columns.forEach((column: any) => {
		columnMap.set(column.id, column.label);
		// 识别other类型的列
		if (column.isOther || 
			column.type === 'other' || 
			column.allowCustom || 
			column.hasInput ||
			(column.label && (
				column.label.toLowerCase().includes('other') || 
				column.label.toLowerCase().includes('enter other') ||
				column.label.toLowerCase().includes('custom') ||
				column.label.toLowerCase().includes('specify')
			))) {
			otherColumnIds.add(column.id);
		}
	});

	// 从responseText中提取Other选项的自定义值
	let otherValues: { [key: string]: string } = {};
	if (responseText && questionId) {
		otherValues = extractOtherValues(responseText, questionId);

	}

	// 将ID转换为对应的label
	const labels: string[] = [];
	answerIds.forEach((id) => {
		const columnLabel = columnMap.get(id);
		
		if (columnLabel) {
			// 如果这是一个other类型的列，显示自定义值
			if (otherColumnIds.has(id) || id.includes('other') || columnLabel.toLowerCase().includes('other')) {
				// 查找对应的自定义值，尝试多种格式
				const customValue = otherValues[id] || 
								   otherValues[id.replace('column-', 'column-other-')] ||
								   Object.entries(otherValues).find(([key]) => key.includes(id))?.[1];

				if (customValue) {
					labels.push(`Other: ${customValue}`);
				} else {
					labels.push(columnLabel);
				}
			} else {
				labels.push(columnLabel);
			}
		} else {
			// 对于没有找到对应label的值，检查是否有other自定义值
			const isOtherValue = Object.keys(otherValues).some(otherKey => {
				return otherKey.includes(id) || id.includes('other');
			});
			
			if (isOtherValue) {
				// 如果这个值对应一个other选项，查找自定义值
				const customValue = Object.entries(otherValues).find(([key]) => 
					key.includes(id) || (id.includes('other') && key.includes('other'))
				)?.[1];
				if (customValue) {
					labels.push(`Other: ${customValue}`);
				} else if (id === 'Other' || id.toLowerCase() === 'other') {
					// 如果答案就是"Other"，查找任何other相关的自定义值
					const anyOtherValue = Object.values(otherValues)[0];
					if (anyOtherValue) {
						labels.push(`Other: ${anyOtherValue}`);
					} else {
						labels.push(id);
					}
				}
			} else {
				labels.push(id);
			}
		}
	});

	return labels.filter(Boolean);
};

// 图标选项配置
const iconOptions = {
	star: {
		filledIcon: IconStar,
		voidIcon: IconStarOutline,
	},
	heart: {
		filledIcon: IconHeart,
		voidIcon: IconHeartOutline,
	},
	thumbs: {
		filledIcon: IconThumbUp,
		voidIcon: IconThumbUpOutline,
	},
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

const formatAnswerDate = (dateStr: any, questionType?: string): string => {
	if (!dateStr) return '';

	// Convert to string if not already
	const dateString = String(dateStr);

	try {
		const date = new Date(dateString);
		// Check if the date is valid
		if (isNaN(date.getTime())) {
			return dateString;
		}
		
		// For time type questions, show only time
		if (questionType === 'time') {
			const hours = String(date.getHours()).padStart(2, '0');
			const minutes = String(date.getMinutes()).padStart(2, '0');
			const seconds = String(date.getSeconds()).padStart(2, '0');
			return `${hours}:${minutes}:${seconds}`;
		}
		
		// For date type questions or general date display, show date
		// Use US date format for consistency
		const month = String(date.getMonth() + 1).padStart(2, '0');
		const day = String(date.getDate()).padStart(2, '0');
		const year = date.getFullYear();
		
		return `${month}/${day}/${year}`;
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
	background-color: var(--primary-10);
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

/* PDF导出优化样式 */
.pdf-export-title {
	white-space: nowrap !important;
	overflow: hidden !important;
	text-overflow: ellipsis !important;
	max-width: 600px !important;
}

.pdf-export-summary {
	page-break-inside: avoid !important;
	margin-top: 24px !important;
}

/* 题目序号样式 */
.question-number {
	color: #2563eb;
	font-weight: 600;
	margin-right: 0.5rem;
	font-size: 0.9em;
}

/* 暗色主题下的题目序号样式 */
html.dark .question-number {
	color: #60a5fa;
}
</style>
