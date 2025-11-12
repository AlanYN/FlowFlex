<template>
	<div class="pb-6 customer-overview-bg">
		<!-- 加载状态 -->
		<PageHeader
			:title="`Customer Overview: ${customerData?.leadName || 'Loading...'}`"
			:show-back-button="true"
			@go-back="handleBack"
		>
			<template #actions>
				<el-button
					@click="handleExportExcel"
					:disabled="!customerData"
					class="page-header-btn page-header-btn-secondary"
					:icon="Download"
				>
					Export Excel
				</el-button>
				<el-button
					@click="handleExportPDF"
					:disabled="!customerData"
					class="page-header-btn page-header-btn-secondary"
					:icon="Document"
				>
					Export PDF
				</el-button>
			</template>
		</PageHeader>
		<customer-overview-loading v-if="loading" />

		<!-- 主要内容 -->
		<div v-else>
			<!-- Customer Info Card -->
			<el-card class="mb-6" v-if="customerData">
				<template #header>
					<div class="text-lg font-medium">Customer Information</div>
				</template>
				<div class="grid grid-cols-1 md:grid-cols-4 gap-4">
					<div>
						<p class="text-sm font-medium text-el-text-color-secondary">
							Customer Name
						</p>
						<p class="font-medium">{{ customerData.leadName }}</p>
					</div>
					<div>
						<p class="text-sm font-medium text-el-text-color-secondary">Contact Name</p>
						<p class="font-medium">{{ customerData.contactPerson || '' }}</p>
					</div>
					<div>
						<p class="text-sm font-medium text-el-text-color-secondary">
							Contact Email
						</p>
						<p class="font-medium">
							{{ customerData.contactEmail || customerData.leadEmail || '' }}
						</p>
					</div>
				</div>
			</el-card>

			<!-- Search and Filters -->
			<el-card class="mb-6">
				<div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
					<div class="space-y-2">
						<label class="filter-label text-sm font-medium">
							Search Questions & Answers
						</label>
						<el-input
							v-model="searchTerm"
							placeholder="Search questions, answers, or sections..."
							clearable
							class="w-full rounded-xl"
						>
							<template #prefix>
								<el-icon><Search /></el-icon>
							</template>
						</el-input>
					</div>

					<div class="space-y-2">
						<label class="filter-label text-sm font-medium">
							Filter by Questionnaires
						</label>
						<el-select
							v-model="selectedQuestionnaires"
							multiple
							filterable
							collapse-tags
							collapse-tags-tooltip
							placeholder="Search questionnaires..."
							class="w-full filter-select"
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
						<label class="filter-label text-sm font-medium">Filter by Sections</label>
						<el-select
							v-model="selectedSections"
							multiple
							filterable
							collapse-tags
							collapse-tags-tooltip
							placeholder="Search sections..."
							class="w-full filter-select"
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
				</div>

				<div class="flex justify-end space-x-2 mt-4">
					<el-button @click="clearFilters">
						<el-icon><Close /></el-icon>
						Reset
					</el-button>
					<el-button type="primary" @click="applyFilters">
						<el-icon><Filter /></el-icon>
						Search
					</el-button>
				</div>

				<div
					class="flex items-center justify-between text-sm text-el-text-color-secondary mt-4 pt-4 border-t border-el-border-color-light dark:border-el-border-color"
				>
					<span>
						Showing {{ visibleResponses.length }} of {{ totalResponsesCount }} responses
						from {{ filteredData.length }} questionnaires
					</span>
					<div
						class="flex items-center justify-between text-sm text-el-text-color-secondary"
					>
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
					<el-card
						v-for="questionnaire in paginatedData"
						:key="questionnaire.id"
						class="questionnaire-response-card"
					>
						<template #header>
							<div class="flex justify-between items-center">
								<div>
									<div class="text-lg font-medium">
										{{ questionnaire.name }}
									</div>
								</div>
								<div class="flex flex-col items-end space-y-1">
									<el-tag type="info">
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
							border
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
										class="font-medium text-el-text-color-primary"
										style="white-space: normal"
									>
										<span class="question-number">
											{{ row.questionNumber }}.
										</span>
										{{ row.question }}
									</p>
								</template>
							</el-table-column>
							<el-table-column label="Answer" show-overflow-tooltip min-width="200">
								<template #default="{ row }">
									<div class="answer-cell p-2 rounded-xl text-sm">
										<!-- 短答题 -->
										<div
											v-if="isShortAnswerType(row.questionType)"
											class="text-el-text-color-regular"
										>
											{{ row.answer }}
										</div>

										<!-- 长答题/段落 -->
										<div
											v-else-if="isParagraphType(row.questionType)"
											class="text-el-text-color-regular"
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
														row.questionConfig,
														row.responseText,
														row.id
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
											class="text-el-text-color-regular"
										>
											<template v-if="row.answer">
												<el-icon class="mr-1">
													<component
														:is="
															row.questionType === 'time'
																? 'Clock'
																: 'Calendar'
														"
													/>
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
														class="w-4 h-4 text-el-text-color-placeholder mr-1"
													/>
												</div>
												<span
													class="ml-2 text-sm text-el-text-color-regular"
												>
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
												<span class="text-sm font-medium">
													{{ row.answer }}/{{
														getLinearScaleMax(row.questionConfig)
													}}
												</span>
												<div
													class="flex-1 bg-el-fill-color-light rounded-full h-2 ml-2 bg-gray-200"
												>
													<div
														class="h-2 rounded-full bg-primary"
														:style="`width: ${
															(parseFloat(String(row.answer)) /
																getLinearScaleMax(
																	row.questionConfig
																)) *
															100
														}%`"
													></div>
												</div>
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
													:key="file.name + (file.url || '')"
													class="flex items-center text-sm overview-file-item p-1 rounded"
												>
													<el-icon class="mr-1 text-primary">
														<Document />
													</el-icon>
													<span class="truncate">{{ file.name }}</span>
													<span
														class="ml-1 text-xs text-el-text-color-secondary"
														v-if="file.size"
													>
														({{ formatFileSize(file.size) }})
													</span>
													<!-- 操作区：预览与下载 -->
													<div
														class="ml-auto flex items-center space-x-2"
													>
														<a
															v-if="file.url"
															:href="file.url"
															target="_blank"
															rel="noopener"
															class="text-primary hover:underline"
															@click.stop
														>
															Preview
														</a>
														<a
															v-if="file.url"
															:href="file.url"
															:download="file.name || 'download'"
															class="text-primary hover:underline"
															@click.stop
														>
															Download
														</a>
													</div>
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
													{{
														getGridAnswerLabels(
															row.answer,
															row.questionConfig,
															row.responseText,
															row.id
														)[0] || row.answer
													}}
												</el-tag>
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
											</template>
										</div>

										<!-- 短答网格 (Short answer grid) -->
										<div
											v-else-if="isShortAnswerGridType(row.questionType)"
											class="short-answer-grid"
										>
											<template
												v-if="
													getShortAnswerGridData(
														row.responseText,
														row.id,
														row.questionConfig
													).length > 0
												"
											>
												<div class="space-y-2">
													<div
														v-for="(
															gridData, index
														) in getShortAnswerGridData(
															row.responseText,
															row.id,
															row.questionConfig
														)"
														:key="`grid-${index}`"
														class="grid-row-data"
													>
														<div class="flex items-start text-sm">
															<span
																class="font-medium text-el-text-color-regular mr-2 min-w-0 flex-shrink-0"
															>
																{{ gridData.row }}:
															</span>
															<span
																class="px-2 py-1 rounded-xl flex-1"
															>
																{{ gridData.value }}
															</span>
														</div>
													</div>
												</div>
											</template>
										</div>

										<!-- 默认显示 -->
										<div
											v-else
											class="text-el-text-color-regular"
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
												class="flex items-center text-el-text-color-regular"
												v-if="row.answeredBy && row.answeredBy !== 'System'"
											>
												<el-icon class="mr-1"><User /></el-icon>
												<span class="font-medium">
													{{ row.answeredBy }}
												</span>
											</div>

											<!-- 首次回答时间 -->
											<div
												class="flex items-center text-el-text-color-regular"
												v-if="
													row.firstAnsweredDate &&
													row.answeredBy !== 'System'
												"
											>
												<el-icon class="mr-1"><Clock /></el-icon>
												<span>{{ formatDate(row.firstAnsweredDate) }}</span>
											</div>

											<!-- 最新修改时间 - 只有当修改时间与首次回答时间不同时才显示 -->
											<div
												v-if="
													row.lastUpdated &&
													row.updatedBy &&
													row.firstAnsweredDate &&
													!isSameTime(row.lastUpdated, row.firstAnsweredDate)
												"
												class="text-primary"
											>
												<div>
													Updated: {{ formatDate(row.lastUpdated) }}
												</div>
												<div>by {{ row.updatedBy }}</div>
											</div>

											<!-- 如果没有修改过，只显示创建时间 -->
											<div
												v-else-if="
													row.answeredDate &&
													!row.firstAnsweredDate &&
													row.answeredBy !== 'System'
												"
												class="flex items-center text-el-text-color-regular"
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
						<el-icon class="text-6xl overview-empty-icon mb-4"><Document /></el-icon>
						<h3 class="text-lg font-medium mb-2">No responses found</h3>
						<p class="text-el-text-color-secondary mb-4">
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
						<div class="text-2xl font-bold text-primary">
							{{ filteredData.length }}
						</div>
						<div class="text-sm text-el-text-color-secondary">Questionnaires</div>
					</div>
					<div class="text-center">
						<div class="text-2xl font-bold text-primary">
							{{ totalResponsesCount }}
						</div>
						<div class="text-sm text-el-text-color-secondary">Total Responses</div>
					</div>
					<div class="text-center">
						<div class="text-2xl font-bold text-primary">{{ sections.length }}</div>
						<div class="text-sm text-el-text-color-secondary">Sections</div>
					</div>
					<div class="text-center">
						<div class="text-2xl font-bold text-primary">
							{{ uniqueContributors }}
						</div>
						<div class="text-sm text-el-text-color-secondary">Contributors</div>
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
	Download,
	Document,
	Search,
	Filter,
	Close,
	User,
	Clock,
	// eslint-disable-next-line @typescript-eslint/no-unused-vars
	Calendar, // Used in dynamic component
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
import PageHeader from '@/components/global/PageHeader/index.vue';

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
		const shortGridAnswersMap = new Map<string, any[]>(); // 用于聚合短答网格的多条行答案

		// Process current questionnaire's answers
		parsedAnswers.responses?.forEach((resp) => {
			// 短答网格：同一题会有多条记录，按题目ID聚合
			if (resp.type === 'short_answer_grid') {
				const qid = resp.questionId;
				if (!shortGridAnswersMap.has(qid)) {
					shortGridAnswersMap.set(qid, []);
				}
				shortGridAnswersMap.get(qid)!.push(resp);
				return;
			}

			// 其他网格类型（questionId 中带行信息）
			if (resp.questionId.includes('_row-') || resp.questionId.includes('_')) {
				const baseQuestionId = resp.questionId.split('_')[0];
				if (!gridAnswersMap.has(baseQuestionId)) {
					gridAnswersMap.set(baseQuestionId, []);
				}
				gridAnswersMap.get(baseQuestionId)!.push(resp);
				return;
			}

			// 普通题：一题一条
			answersMap.set(resp.questionId, resp);
		});

		// Process each section and question
		structure.sections?.forEach((section: any) => {
			section.questions?.forEach((question: any, questionIndex: number) => {
				// 检查是否是网格类型的问题
				const isGridType =
					question.type === 'checkbox_grid' ||
					question.type === 'multiple_choice_grid' ||
					question.type === 'short_answer_grid';

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
					if (question.type === 'short_answer_grid') {
						const shortList = shortGridAnswersMap.get(possibleId);
						if (shortList && shortList.length > 0) {
							gridAnswers = shortList;
							break;
						}
					}
					const foundGridAnswers = gridAnswersMap.get(possibleId);
					if (foundGridAnswers && foundGridAnswers.length > 0) {
						gridAnswers = foundGridAnswers;
						break;
					}
				}

				// 如果是网格类型且有网格答案，处理网格答案
				if (isGridType && gridAnswers && gridAnswers.length > 0) {
					// 短答网格：将同一题的所有单元格答案合并为一条记录，答案放在 responseText
					if (question.type === 'short_answer_grid') {
						const merged: Record<string, any> = {};
						const rowsMap: Record<string, string> = {};
						let lastUpdated = '';
						let updatedBy = '';
						let firstAnsweredDate = '';
						let firstAnsweredBy = '';

						gridAnswers.forEach((ga) => {
							if (ga?.responseText) {
								try {
									const parsed = JSON.parse(ga.responseText);
									Object.assign(merged, parsed);
									// 从 key 里解析 row，并映射到题目配置的行 label
									const rowLabelMap = new Map<string, string>();
									const columnLabelMap = new Map<string, string>();
									(question.rows || []).forEach((r: any) => {
										rowLabelMap.set(r.id, r.label);
										rowLabelMap.set(String(r.id), r.label);
										// 支持多种ID格式
										if (r.id && !r.id.startsWith('row-')) {
											rowLabelMap.set(`row-${r.id}`, r.label);
										}
									});
									(question.columns || []).forEach((c: any) => {
										columnLabelMap.set(c.id, c.label);
										columnLabelMap.set(String(c.id), c.label);
										// 支持多种ID格式
										if (c.id && !c.id.startsWith('column-')) {
											columnLabelMap.set(`column-${c.id}`, c.label);
										}
									});

									Object.entries(parsed).forEach(([k, v]) => {
										const parts = k.split('_');
										// 格式: questionId_columnId_rowId 或 questionId_rowId_columnId
										if (parts.length >= 3) {
											// 尝试两种可能的格式
											let rowPart = '';
											let columnPart = '';
											
											// 格式1: questionId_columnId_rowId
											if (parts[1]?.startsWith('column') || parts[1]?.startsWith('row')) {
												columnPart = parts[1];
												rowPart = parts[2];
											}
											// 格式2: questionId_rowId_columnId
											else if (parts[2]?.startsWith('column') || parts[1]?.startsWith('row')) {
												rowPart = parts[1];
												columnPart = parts[2];
											}
											// 默认格式: questionId_columnId_rowId
											else {
												columnPart = parts[1];
												rowPart = parts[2];
											}

											// 尝试多种方式匹配 row label
											let label =
												rowLabelMap.get(rowPart) ||
												rowLabelMap.get(rowPart.replace('row-', '')) ||
												rowLabelMap.get(`row-${rowPart}`) ||
												rowPart.replace('row-', '');

											// 尝试匹配 column label
											let columnLabel =
												columnLabelMap.get(columnPart) ||
												columnLabelMap.get(columnPart.replace('column-', '')) ||
												columnLabelMap.get(`column-${columnPart}`) ||
												columnPart.replace('column-', '');

											if (String(v || '').trim() !== '') {
												// 对于短答网格，同一行可能有多个列的值
												// 使用行的label作为key，如果有多个列的值，用逗号分隔
												if (rowsMap[label]) {
													// 如果该行已有值，追加到现有值（用逗号分隔）
													rowsMap[label] = `${rowsMap[label]}, ${String(v)}`;
												} else {
													rowsMap[label] = String(v);
												}
											}
										}
									});
								} catch (e) {
									console.error('Error parsing grid answer:', e);
								}
							}

							if (ga?.changeHistory && Array.isArray(ga.changeHistory)) {
								const firstCreated = ga.changeHistory.find(
									(c: any) => c.action === 'created'
								);
								if (
									firstCreated &&
									(!firstAnsweredDate ||
										firstCreated.timestampUtc < firstAnsweredDate)
								) {
									firstAnsweredDate =
										firstCreated.timestampUtc || firstCreated.timestamp || '';
									firstAnsweredBy = firstCreated.user || '';
								}
							}

							if (
								ga?.lastModifiedAt &&
								(!lastUpdated || ga.lastModifiedAt > lastUpdated)
							) {
								lastUpdated = ga.lastModifiedAt;
								updatedBy = ga.lastModifiedBy || '';
							}
						});

						// Convert rowsMap to display text
						const answerText = Object.entries(rowsMap)
							.map(([row, value]) => `${row}: ${value}`)
							.join('; ');

						responses.push({
							id: question.id,
							question: question.title,
							description: question.description,
							answer: answerText, // Use formatted answer text to show Response Info
							answeredBy: firstAnsweredBy || answer?.createBy || '',
							answeredDate: answer?.createDate || '',
							firstAnsweredDate: firstAnsweredDate || answer?.createDate || '',
							lastUpdated: lastUpdated || answer?.modifyDate || '',
							updatedBy: updatedBy || answer?.modifyBy || '',
							questionType: question.type,
							section: section.name,
							required: question.required || false,
							questionConfig: question.config || question,
							questionNumber: questionIndex + 1,
							responseText: JSON.stringify({ rows: rowsMap, cells: merged }),
						});
					} else {
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

							// 避免空选择时将 "{}" 作为有效答案展示
							const sanitizedGridAnswer =
								typeof gridAnswer.answer === 'string'
									? gridAnswer.answer
									: String(gridAnswer.answer ?? '');

							responses.push({
								id: gridAnswer.questionId, // 使用网格行的完整ID
								question: gridAnswer.question || question.title, // 使用网格行的问题标题
								description: question.description,
								// 对于网格题，仅保留实际选择值；不再用 responseText 兜底，避免显示 "{}"
								answer: sanitizedGridAnswer,
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
					}
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
	// 从所有问卷的structureJson中提取所有sections，使用id去重
	const cacheKey = `sections_all_${questionnairesData.value.length}`;
	if (computedCache.has(cacheKey)) {
		return computedCache.get(cacheKey);
	}

	const sectionIdSet = new Set<string>(); // 使用id去重
	const sectionIdToNameMap = new Map<string, string>(); // id -> name 映射，用于显示
	
	// 从所有问卷的structureJson中提取sections
	questionnairesData.value.forEach((questionnaire) => {
		try {
			const structure = JSON.parse(questionnaire.structureJson);
			if (structure.sections && Array.isArray(structure.sections)) {
				structure.sections.forEach((section: any) => {
					// 优先使用id去重，支持多种id字段
					const sectionId = section.id || section.temporaryId || String(section.order) || '';
					const sectionName = section.name || section.title || '';
					
					if (sectionId) {
						// 使用id去重，即使name为空也统计
						if (!sectionIdSet.has(sectionId)) {
							sectionIdSet.add(sectionId);
							// 保存id到name的映射，如果name为空则使用id作为显示名称
							sectionIdToNameMap.set(sectionId, sectionName || sectionId);
						}
					} else if (sectionName) {
						// 如果没有id，使用name去重（兜底逻辑）
						sectionIdSet.add(sectionName);
						sectionIdToNameMap.set(sectionName, sectionName);
					}
				});
			}
		} catch (e) {
			console.error('Error parsing questionnaire structure:', e);
		}
	});
	
	// 转换为名称数组（如果name为空则使用id）
	const result = Array.from(sectionIdSet)
		.map((id) => sectionIdToNameMap.get(id) || id)
		.filter(Boolean)
		.sort();
	
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

// All questions for export (includes questions without answers) - kept for potential future use
// eslint-disable-next-line @typescript-eslint/no-unused-vars
const allQuestionsForExport = computed(() => {
	const responses: any[] = [];
	processedData.value.forEach((questionnaire) => {
		questionnaire.responses.forEach((response) => {
			// 处理答案显示，确保所有选择类型都显示正确的 label
			let displayAnswer = response.answer || '';

			// 如果是表格类型（单选/多选），转换为label显示
			if (
				(response.questionType === 'multiple_choice_grid' ||
					response.questionType === 'checkbox_grid') &&
				response.questionConfig
			) {
				const labels = getGridAnswerLabels(
					response.answer,
					response.questionConfig,
					response.responseText,
					response.id
				);
				displayAnswer = labels.join(', ');
			}
			// 如果是短答网格类型，转换为键值对显示
			else if (response.questionType === 'short_answer_grid') {
				const gridData = getShortAnswerGridData(
					response.responseText,
					response.id,
					response.questionConfig
				);
				displayAnswer = gridData.map((item) => `${item.row}: ${item.value}`).join('; ');
			}
			// 如果是多选类型，转换为label显示
			else if (response.questionType === 'checkboxes' && response.answer) {
				const labels = getCheckboxLabels(
					response.answer,
					response.questionConfig,
					response.responseText,
					response.id
				);
				displayAnswer = labels.join(', ');
			}
			// 如果是单选类型，转换为label显示
			else if (response.questionType === 'multiple_choice' && response.answer) {
				displayAnswer = getMultipleChoiceLabel(
					response.answer,
					response.questionConfig,
					response.responseText,
					response.id
				);
			}
			// 如果是下拉选择类型，转换为label显示
			else if (response.questionType === 'dropdown' && response.answer) {
				displayAnswer = getDropdownLabel(response.answer, response.questionConfig);
			}
			// 如果是文件上传类型，显示文件名
			else if (
				(response.questionType === 'file' || response.questionType === 'file_upload') &&
				response.answer
			) {
				const files = getFileAnswers(response.answer);
				displayAnswer = files.map((file) => file.name).join(', ');
			}
			// 如果是日期/时间类型，格式化为美国格式
			else if (
				(response.questionType === 'date' || response.questionType === 'time') &&
				response.answer
			) {
				displayAnswer = formatAnswerDate(response.answer, response.questionType);
			}
			// 其他类型保持原样
			else {
				displayAnswer = response.answer || '';
			}

			// Include ALL questions, regardless of whether they have answers
			// Only show response info fields when there's a valid answer
			const hasAnswer = hasValidAnswer(displayAnswer);
			responses.push({
				questionnaire: questionnaire.name,
				section: response.section,
				questionNumber: response.questionNumber,
				question: response.question,
				answer: displayAnswer,
				answeredBy: hasAnswer ? response.answeredBy || '' : '',
				answeredDate:
					hasAnswer && response.answeredDate ? formatDateUS(response.answeredDate) : '',
				lastUpdated:
					hasAnswer && response.lastUpdated ? formatDateUS(response.lastUpdated) : '',
				updatedBy: hasAnswer ? response.updatedBy || '' : '',
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

			// 如果是表格类型（单选/多选），转换为label显示
			if (
				(response.questionType === 'multiple_choice_grid' ||
					response.questionType === 'checkbox_grid') &&
				response.questionConfig
			) {
				const labels = getGridAnswerLabels(
					response.answer,
					response.questionConfig,
					response.responseText,
					response.id
				);
				displayAnswer = labels.join(', ');
			}
			// 如果是短答网格类型，转换为键值对显示
			else if (response.questionType === 'short_answer_grid') {
				const gridData = getShortAnswerGridData(
					response.responseText,
					response.id,
					response.questionConfig
				);
				displayAnswer = gridData.map((item) => `${item.row}: ${item.value}`).join('; ');
			}
			// 如果是多选类型，转换为label显示
			else if (response.questionType === 'checkboxes' && response.answer) {
				const labels = getCheckboxLabels(
					response.answer,
					response.questionConfig,
					response.responseText,
					response.id
				);
				displayAnswer = labels.join(', ');
			}
			// 如果是单选类型，转换为label显示
			else if (response.questionType === 'multiple_choice' && response.answer) {
				displayAnswer = getMultipleChoiceLabel(
					response.answer,
					response.questionConfig,
					response.responseText,
					response.id
				);
			}
			// 如果是下拉选择类型，转换为label显示
			else if (response.questionType === 'dropdown' && response.answer) {
				displayAnswer = getDropdownLabel(response.answer, response.questionConfig);
			}
			// 如果是文件上传类型，显示文件名
			else if (
				(response.questionType === 'file' || response.questionType === 'file_upload') &&
				response.answer
			) {
				const files = getFileAnswers(response.answer);
				displayAnswer = files.map((file) => file.name).join(', ');
			}
			// 如果是日期/时间类型，格式化为美国格式
			else if (
				(response.questionType === 'date' || response.questionType === 'time') &&
				response.answer
			) {
				displayAnswer = formatAnswerDate(response.answer, response.questionType);
			}
			// 其他类型保持原样
			else {
				displayAnswer = `${response.answer}` || '';
			}

			// Include ALL filtered questions, regardless of whether they have answers
			// Only show response info fields when there's a valid answer
			const hasAnswer = hasValidAnswer(displayAnswer);
			responses.push({
				questionnaire: questionnaire.name,
				section: response.section,
				questionNumber: response.questionNumber,
				question: response.question,
				answer: displayAnswer,
				answeredBy: hasAnswer ? response.answeredBy || '' : '',
				answeredDate:
					hasAnswer && response.answeredDate ? formatDateUS(response.answeredDate) : '',
				lastUpdated:
					hasAnswer && response.lastUpdated ? formatDateUS(response.lastUpdated) : '',
				updatedBy: hasAnswer ? response.updatedBy || '' : '',
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
const extractOtherValues = (
	responseText: string,
	questionId: string
): { [key: string]: string } => {
	const parsed = parseResponseText(responseText);
	const otherValues: { [key: string]: string } = {};

	// 提取基础questionId（去掉行信息，如 question-xxx_row-xxx -> question-xxx）
	const baseQuestionId = questionId.split('_row-')[0].split('_')[0];

	// 查找包含questionId或baseQuestionId的键
	Object.keys(parsed).forEach((key) => {
		// 匹配questionId或baseQuestionId
		if (key.includes(questionId) || key.includes(baseQuestionId)) {
			const customValue = parsed[key];
			const parts = key.split('_');
			
			// 存储完整的key，用于精确匹配
			otherValues[key] = customValue;
			
			// 格式1: questionId_optionId (单选/多选)
			// 例如: "1988483416273850373_1988483416273850372"
			if (parts.length === 2 && parts[0] === questionId) {
				const optionId = parts[1];
				otherValues[optionId] = customValue;
				otherValues['other'] = customValue; // 通用兜底
			}
			
			// 格式2: questionId_rowId_columnId (网格类型)
			// 例如: "1988483416273850381_1988483416273850376_1988483416273850380"
			if (parts.length === 3) {
				const columnId = parts[2]; // 最后一部分是column ID
				otherValues[columnId] = customValue;
				otherValues[`column-${columnId}`] = customValue;
				otherValues[`column-other-${columnId}`] = customValue;
				// 也存储完整的key格式，用于匹配
				const fullKey = `${questionId}_${parts[1]}_${columnId}`;
				otherValues[fullKey] = customValue;
			}
			
			// 格式3: 包含column-other-的格式
			const columnPart = parts.find((part) => part.startsWith('column-other-'));
			if (columnPart) {
				const columnId = columnPart.replace('column-other-', '');
				otherValues[columnPart] = customValue;
				otherValues[columnId] = customValue;
				otherValues[`column-${columnId}`] = customValue;
			}
			
			// 格式4: 包含option-的格式
			const optionPart = parts.find((part) => part.startsWith('option-'));
			if (optionPart) {
				const optionId = optionPart.replace('option-', '');
				otherValues[optionPart] = customValue;
				otherValues[optionId] = customValue;
				const alternativeKey = optionPart.replace('option-', 'option_');
				otherValues[alternativeKey] = customValue;
			}
			
			// 存储最后一部分（通常是ID），用于匹配
			const lastPart = parts[parts.length - 1];
			if (lastPart) {
				otherValues[lastPart] = customValue;
			}
			
			// 存储通用的"other"键，作为最后的兜底
			if (!otherValues['other']) {
				otherValues['other'] = customValue;
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

// 比较两个时间字符串是否相同（忽略毫秒差异）
const isSameTime = (time1: string, time2: string): boolean => {
	if (!time1 || !time2) return false;
	if (time1 === time2) return true;
	
	try {
		const date1 = new Date(time1);
		const date2 = new Date(time2);
		
		// 如果日期无效，使用字符串比较
		if (isNaN(date1.getTime()) || isNaN(date2.getTime())) {
			return time1 === time2;
		}
		
		// 比较到秒级别（忽略毫秒）
		return (
			date1.getFullYear() === date2.getFullYear() &&
			date1.getMonth() === date2.getMonth() &&
			date1.getDate() === date2.getDate() &&
			date1.getHours() === date2.getHours() &&
			date1.getMinutes() === date2.getMinutes() &&
			date1.getSeconds() === date2.getSeconds()
		);
	} catch {
		return time1 === time2;
	}
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
			'Updated By',
		];

		// Create worksheet with headers first
		const worksheet = XLSX.utils.aoa_to_sheet([headers]);

		// Add data starting from row 2
		XLSX.utils.sheet_add_json(worksheet, exportData, {
			origin: 'A2',
			skipHeader: true,
		});

		// Apply bold formatting to header row only
		const headerCells = ['A1', 'B1', 'C1', 'D1', 'E1', 'F1', 'G1', 'H1', 'I1'];
		headerCells.forEach((cellAddress) => {
			if (worksheet[cellAddress]) {
				worksheet[cellAddress].s = {
					font: {
						bold: true,
					},
				};
			}
		});

		// Set column widths for better readability
		worksheet['!cols'] = [
			{ wch: 20 }, // Questionnaire
			{ wch: 15 }, // Section
			{ wch: 5 }, // No.
			{ wch: 50 }, // Question
			{ wch: 30 }, // Answer
			{ wch: 15 }, // Answered By
			{ wch: 18 }, // Answered Date
			{ wch: 18 }, // Last Updated
			{ wch: 15 }, // Updated By
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
			sheetStubs: false,
		});

		const filterInfo = hasActiveFilters.value ? ' (filtered data)' : '';
		ElMessage.success(
			`Excel file exported successfully with ${exportData.length} questions${filterInfo}`
		);
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
		const sourceElement = document.querySelector('.pb-6.customer-overview-bg') as HTMLElement;
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
			titleElement.style.color = getComputedStyle(document.documentElement)
				.getPropertyValue('--el-text-color-primary')
				.trim();
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
			summarySection.style.border =
				'1px solid ' +
				getComputedStyle(document.documentElement)
					.getPropertyValue('--el-border-color-light')
					.trim();
			summarySection.style.borderRadius = '4px';
			summarySection.style.backgroundColor = getComputedStyle(document.documentElement)
				.getPropertyValue('--el-bg-color')
				.trim();
			summarySection.style.boxShadow = '0 2px 12px 0 rgba(0, 0, 0, 0.1)';
			summarySection.style.overflow = 'hidden';
			summarySection.style.display = 'block';
			summarySection.style.visibility = 'visible';

			// 创建标题部分
			const headerDiv = document.createElement('div');
			headerDiv.style.padding = '18px 20px';
			headerDiv.style.borderBottom =
				'1px solid ' +
				getComputedStyle(document.documentElement)
					.getPropertyValue('--el-border-color-light')
					.trim();
			headerDiv.style.backgroundColor = getComputedStyle(document.documentElement)
				.getPropertyValue('--el-fill-color-light')
				.trim();
			headerDiv.style.fontSize = '1.125rem';
			headerDiv.style.fontWeight = '500';
			headerDiv.style.color = getComputedStyle(document.documentElement)
				.getPropertyValue('--el-text-color-primary')
				.trim();
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
			const primaryColor = getComputedStyle(document.documentElement)
				.getPropertyValue('--el-color-primary')
				.trim();
			const successColor = getComputedStyle(document.documentElement)
				.getPropertyValue('--el-color-success')
				.trim();
			const infoColor = getComputedStyle(document.documentElement)
				.getPropertyValue('--el-color-info')
				.trim();
			const warningColor = getComputedStyle(document.documentElement)
				.getPropertyValue('--el-color-warning')
				.trim();
			const statsData = [
				{ value: filteredData.value.length, label: 'Questionnaires', color: primaryColor },
				{ value: totalResponsesCount.value, label: 'Total Responses', color: successColor },
				{ value: sections.value.length, label: 'Sections', color: infoColor },
				{ value: uniqueContributors.value, label: 'Contributors', color: warningColor },
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
				const secondaryColor = getComputedStyle(document.documentElement)
					.getPropertyValue('--el-text-color-secondary')
					.trim();
				labelDiv.style.fontSize = '0.875rem';
				labelDiv.style.lineHeight = '1.25rem';
				labelDiv.style.color = secondaryColor;
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
		clonedElement.style.backgroundColor = getComputedStyle(document.documentElement)
			.getPropertyValue('--el-bg-color')
			.trim();
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
			summaryInClone.style.backgroundColor = getComputedStyle(document.documentElement)
				.getPropertyValue('--el-bg-color')
				.trim();
			summaryInClone.style.border =
				'1px solid ' +
				getComputedStyle(document.documentElement)
					.getPropertyValue('--el-border-color-light')
					.trim();
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
			const bgColor = getComputedStyle(document.documentElement)
				.getPropertyValue('--el-bg-color')
				.trim();
			const borderColor = getComputedStyle(document.documentElement)
				.getPropertyValue('--el-border-color-light')
				.trim();
			// 强制设置所有可能影响渲染的样式
			finalSummaryForCanvas.style.cssText = `
			display: block !important;
			visibility: visible !important;
			opacity: 1 !important;
			position: relative !important;
			height: auto !important;
			width: 100% !important;
			margin-top: 24px !important;
			background-color: ${bgColor} !important;
			border: 1px solid ${borderColor} !important;
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
			backgroundColor: getComputedStyle(document.documentElement)
				.getPropertyValue('--el-bg-color')
				.trim(),
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

const isShortAnswerGridType = (type: string): boolean => {
	return type === 'short_answer_grid';
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
const getMultipleChoiceLabel = (
	answer: string,
	questionConfig: any,
	responseText?: string,
	questionId?: string
): string => {
	if (!answer || !questionConfig?.options) return answer;

	// 检查是否是空的JSON对象字符串
	if (typeof answer === 'string' && (answer.trim() === '{}' || answer.trim() === '[]')) {
		return '';
	}

	// Find the option with matching value
	const option = questionConfig.options.find((opt: any) => opt.value === answer);
	
	// 如果答案是"other"或选项是Other类型，尝试从responseText中提取自定义值
	if (
		(answer === 'other' || answer.toLowerCase().includes('other')) ||
		(option && (
			option.isOther ||
			option.type === 'other' ||
			option.allowCustom ||
			option.hasInput ||
			(option.label && option.label.toLowerCase().includes('other'))
		))
	) {
		if (responseText && questionId) {
			const otherValues = extractOtherValues(responseText, questionId);
			// 尝试多种方式查找自定义值
			const customValue =
				otherValues[answer] ||
				otherValues['other'] ||
				otherValues[option?.value] ||
				// 查找包含questionId和option value的完整key
				(option?.value ? Object.entries(otherValues).find(([key]) => {
					return key.includes(questionId) && key.includes(option.value);
				})?.[1] : null) ||
				// 查找任何包含other的值
				Object.entries(otherValues).find(([key]) =>
					key.toLowerCase().includes('other')
				)?.[1] ||
				// 最后使用第一个值
				Object.values(otherValues)[0];
			
			if (customValue) {
				return `Other: ${customValue}`;
			}
		}
	}
	
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
const getCheckboxLabels = (
	answer: any,
	questionConfig: any,
	responseText?: string,
	questionId?: string
): string[] => {
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
		if (
			option.isOther ||
			option.type === 'other' ||
			option.allowCustom ||
			option.hasInput ||
			(option.label &&
				(option.label.toLowerCase().includes('other') ||
					option.label.toLowerCase().includes('enter other') ||
					option.label.toLowerCase().includes('custom') ||
					option.label.toLowerCase().includes('specify')))
		) {
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
				const customValue =
					otherValues[value] || otherValues[value.replace('option_', 'option-')];
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
			const isOtherValue = Object.keys(otherValues).some((otherKey) => {
				return (
					otherKey.includes(value) ||
					value.includes(otherKey.replace('option-', '').replace('option_', ''))
				);
			});

			if (isOtherValue) {
				// 如果这个值对应一个other选项，查找自定义值
				const customValue = Object.entries(otherValues).find(
					([key]) =>
						key.includes(value) ||
						value.includes(key.replace('option-', '').replace('option_', ''))
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
const getGridAnswerLabels = (
	answer: any,
	questionConfig: any,
	responseText?: string,
	questionId?: string
): string[] => {
	if (!answer || !questionConfig?.columns) return [];

	// 获取原始答案数组
	const answerIds = getCheckboxAnswers(answer);

	// 创建 column ID -> label 的映射
	const columnMap = new Map<string, string>();
	const otherColumnIds = new Set<string>();

	questionConfig.columns.forEach((column: any) => {
		columnMap.set(column.id, column.label);
		// 识别包含 Other/自定义输入 的列
		if (
			column.isOther ||
			column.type === 'other' ||
			column.allowCustom ||
			column.hasInput ||
			(column.label &&
				(column.label.toLowerCase().includes('other') ||
					column.label.toLowerCase().includes('enter other') ||
					column.label.toLowerCase().includes('custom') ||
					column.label.toLowerCase().includes('specify')))
		) {
			otherColumnIds.add(column.id);
		}
	});

	// 从 responseText 中提取 Other 的自定义值
	let otherValues: { [key: string]: string } = {};
	if (responseText && questionId) {
		otherValues = extractOtherValues(responseText, questionId);
	}

	// 将 ID 转换为对应的 label
	const labels: string[] = [];
	answerIds.forEach((id) => {
		const idLower = String(id).toLowerCase();
		const columnLabel = columnMap.get(id);

		if (columnLabel) {
			// 如果是 Other 类型的列，则优先显示用户输入的值
			if (
				otherColumnIds.has(id) ||
				idLower.includes('other') ||
				columnLabel.toLowerCase().includes('other')
			) {
				// 尝试多种匹配方式查找自定义值
				// 首先尝试直接匹配column ID的各种格式
				let customValue =
					// 直接匹配column ID
					otherValues[id] ||
					otherValues[`column-${id}`] ||
					otherValues[`column-other-${id}`] ||
					// 尝试匹配完整key格式: questionId_rowId_columnId
					(questionId ? Object.entries(otherValues).find(([key]) => {
						// 匹配格式: questionId_*_columnId 或 *_columnId
						return key.endsWith(`_${id}`) || key === `${questionId}_${id}`;
					})?.[1] : null) ||
					// 格式转换匹配
					otherValues[id.replace('column-', 'column-other-')] ||
					otherValues[id.replace('column-other-', '')] ||
					// 如果id是column-other-xxx格式，尝试提取xxx部分
					(idLower.startsWith('column-other-') ? otherValues[idLower.replace('column-other-', '')] : null) ||
					// 查找包含该column id的完整key
					Object.entries(otherValues).find(([key]) => {
						const keyLower = key.toLowerCase();
						return (
							keyLower.includes(idLower) ||
							keyLower.includes(`column-other-${idLower}`) ||
							keyLower.includes(`_${idLower}_`) ||
							keyLower.endsWith(`_${idLower}`) ||
							keyLower.endsWith(idLower)
						);
					})?.[1] ||
					// 如果还是找不到，尝试查找任何包含other的值（作为最后兜底）
					Object.entries(otherValues).find(([key]) =>
						key.toLowerCase().includes('other')
					)?.[1] ||
					// 最后使用第一个值
					Object.values(otherValues)[0];

				if (customValue) {
					labels.push(`Other: ${customValue}`);
				} else {
					labels.push(columnLabel);
				}
			} else {
				labels.push(columnLabel);
			}
		} else {
			// 未在 columnMap 中找到，可能是直接返回了 "Other"
			if (idLower === 'other' || idLower.includes('other')) {
				// 尝试多种方式查找自定义值
				// 首先尝试直接匹配
				let customValue =
					otherValues[id] ||
					otherValues[`column-other-${id}`] ||
					otherValues[`column-${id}`] ||
					// 查找包含other的完整key
					Object.entries(otherValues).find(([key]) => {
						const keyLower = key.toLowerCase();
						return (
							keyLower.includes('other') &&
							(keyLower.includes(idLower) || keyLower.endsWith('other') || keyLower.includes('column-other-'))
						);
					})?.[1] ||
					// 如果还是找不到，尝试查找任何包含other的值
					Object.entries(otherValues).find(([key]) =>
						key.toLowerCase().includes('other')
					)?.[1] ||
					// 最后兜底：使用第一个值
					Object.values(otherValues)[0];
				labels.push(customValue ? `Other: ${customValue}` : 'Other');
				return;
			}

			// 兜底：大小写不敏感地匹配 otherValues 的键
			const hasRelatedOther = Object.keys(otherValues).some((k) =>
				k.toLowerCase().includes(idLower) ||
				k.toLowerCase().includes(`column-other-${idLower}`) ||
				k.toLowerCase().includes(`_${idLower}_`) ||
				k.toLowerCase().endsWith(`_${idLower}`)
			);
			if (hasRelatedOther) {
				const customValue = Object.entries(otherValues).find(([key]) =>
					key.toLowerCase().includes(idLower) ||
					key.toLowerCase().includes(`column-other-${idLower}`) ||
					key.toLowerCase().includes(`_${idLower}_`) ||
					key.toLowerCase().endsWith(`_${idLower}`)
				)?.[1];
				labels.push(customValue ? `Other: ${customValue}` : String(id));
			} else {
				labels.push(String(id));
			}
		}
	});

	return labels.filter(Boolean);
};

// 解析短答网格数据
const getShortAnswerGridData = (
	responseText?: string,
	questionId?: string,
	questionConfig?: any
): Array<{ column: string; value: string; row: string }> => {
	if (!responseText || !questionId) return [];

	try {
		const parsed = parseResponseText(responseText);
		// 优先处理合并格式 { rows: { '问题1': '1-1', '问题2': '2-2' }, cells: {...} }
		if (parsed && (parsed as any).rows && typeof (parsed as any).rows === 'object') {
			const rowsObj = (parsed as any).rows as Record<string, any>;
			const result: Array<{ column: string; value: string; row: string }> = [];

			// 直接使用 rowsObj 中的键值对，键已经是 label
			Object.entries(rowsObj).forEach(([rowLabel, value]) => {
				if (value != null && String(value).trim() !== '') {
					result.push({
						row: String(rowLabel),
						column: '',
						value: String(value),
					});
				}
			});

			return result;
		}

		// 处理原始 cells 格式
		const gridData: Array<{ column: string; value: string; row: string }> = [];
		const rows = (questionConfig?.rows || []) as Array<{ id: string; label: string }>;
		const columns = (questionConfig?.columns || []) as Array<{ id: string; label: string }>;

		// 创建 ID 到 label 的映射
		const rowLabelMap = new Map<string, string>();
		const columnLabelMap = new Map<string, string>();
		rows.forEach((r) => {
			rowLabelMap.set(r.id, r.label);
			rowLabelMap.set(String(r.id), r.label);
		});
		columns.forEach((c) => {
			columnLabelMap.set(c.id, c.label);
			columnLabelMap.set(String(c.id), c.label);
		});

		// 遍历所有的键值对，查找属于当前问题的网格数据
		Object.entries(parsed).forEach(([key, value]) => {
			if (value && String(value).trim() !== '') {
				// 解析键名格式: questionId_columnId_rowId
				const parts = key.split('_');
				if (parts.length >= 3) {
					const baseQuestionId = parts[0];
					const columnId = parts[1];
					const rowId = parts[2];

					// 确保这是当前问题的数据 - 支持多种匹配方式
					const isCurrentQuestion =
						baseQuestionId === questionId ||
						key.startsWith(questionId + '_') ||
						key.startsWith('question-' + questionId.replace('question-', ''));

					if (isCurrentQuestion) {
						// 使用映射获取真实的 label
						let columnLabel =
							columnLabelMap.get(columnId) ||
							columnLabelMap.get(columnId.replace('column-', '')) ||
							columnId.replace('column-', 'Col ');

						let rowLabel =
							rowLabelMap.get(rowId) ||
							rowLabelMap.get(rowId.replace('row-', '')) ||
							rowId.replace('row-', '');

						gridData.push({
							column: columnLabel,
							row: rowLabel,
							value: String(value),
						});
					}
				}
			}
		});

		// 按行和列排序，确保显示顺序一致
		gridData.sort((a, b) => {
			if (a.row === b.row) {
				return a.column.localeCompare(b.column);
			}
			return a.row.localeCompare(b.row);
		});

		return gridData;
	} catch (error) {
		console.warn('Failed to parse short answer grid data:', responseText, error);
		return [];
	}
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

const getFileAnswers = (answer: any): Array<{ name: string; size?: number; url?: string }> => {
	if (!answer) return [];

	// If it's already an array, process each item
	if (Array.isArray(answer)) {
		return answer.map((file) => {
			if (typeof file === 'object' && file !== null) {
				const url =
					file.url ||
					file.fileUrl ||
					file.downloadUrl ||
					file.href ||
					file.path ||
					undefined;
				return {
					name: file.name || file.fileName || 'Unknown file',
					size: file.size || file.fileSize,
					url,
				};
			}
			const str = String(file);
			const isUrl = /^https?:\/\//i.test(str) || str.startsWith('data:');
			return {
				name: isUrl ? str.split('/').pop() || 'file' : str,
				url: isUrl ? str : undefined,
			};
		});
	}

	// If it's an object (single file)
	if (typeof answer === 'object' && answer !== null) {
		return [
			{
				name: answer.name || answer.fileName || 'Unknown file',
				size: answer.size || answer.fileSize,
				url:
					answer.url ||
					answer.fileUrl ||
					answer.downloadUrl ||
					answer.href ||
					answer.path,
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
					const url =
						(file as any).url ||
						(file as any).fileUrl ||
						(file as any).downloadUrl ||
						(file as any).href ||
						(file as any).path ||
						undefined;
					return {
						name: (file as any).name || (file as any).fileName || 'Unknown file',
						size: (file as any).size || (file as any).fileSize,
						url,
					};
				}
				const str = String(file);
				const isUrl = /^https?:\/\//i.test(str) || str.startsWith('data:');
				return {
					name: isUrl ? str.split('/').pop() || 'file' : str,
					url: isUrl ? str : undefined,
				};
			});
		}
		if (typeof parsed === 'object' && parsed !== null) {
			return [
				{
					name: (parsed as any).name || (parsed as any).fileName || 'Unknown file',
					size: (parsed as any).size || (parsed as any).fileSize,
					url:
						(parsed as any).url ||
						(parsed as any).fileUrl ||
						(parsed as any).downloadUrl ||
						(parsed as any).href ||
						(parsed as any).path,
				},
			];
		}
		// primitive string
		const isUrl = /^https?:\/\//i.test(answerStr) || answerStr.startsWith('data:');
		return [
			{
				name: isUrl ? answerStr.split('/').pop() || 'file' : answerStr,
				url: isUrl ? answerStr : undefined,
			},
		];
	} catch {
		const isUrl = /^https?:\/\//i.test(answerStr) || answerStr.startsWith('data:');
		return [
			{
				name: isUrl ? answerStr.split('/').pop() || 'file' : answerStr,
				url: isUrl ? answerStr : undefined,
			},
		];
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
		background-color: var(--el-fill-color-light) !important;
		transform: translateY(-1px);
		box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
	}
}

/* 选择类型答案样式优化 */
.checkbox-answers,
.single-choice-answer,
.dropdown-answer,
.grid-answer,
.short-answer-grid {
	.checkbox-tag,
	.choice-tag,
	.dropdown-tag,
	.grid-tag {
		display: inline-flex;
		align-items: center;
		margin: 2px;
		font-weight: 500;
		transition: all 0.2s ease;
		@apply rounded-xl;

		.el-icon {
			opacity: 0.8;
		}

		&:hover {
			transform: translateY(-1px);
			box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
		}
	}

	/* 短答网格特殊样式 */
	&.short-answer-grid {
		.grid-row-data {
			padding: 4px 0;
			border-bottom: 1px solid var(--el-border-color-lighter);

			&:last-child {
				border-bottom: none;
			}

			.font-medium {
				min-width: 120px;
				color: var(--el-text-color-secondary);
				font-size: 0.8rem;
			}

			span:last-child {
				border: 1px solid var(--el-border-color-light);
				padding: 2px 8px;
				font-size: 0.875rem;
				color: var(--el-text-color-regular);
				word-break: break-word;
				@apply rounded-xl bg-primary;
			}
		}
	}
}

/* 统一 Answer 单元格最小高度，保证无值与有值展示高度一致 */
.answer-cell {
	min-height: 40px;
	display: block;
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
	color: var(--el-color-primary);
	font-weight: 600;
	margin-right: 0.5rem;
	font-size: 0.9em;
}

/* 暗色主题下的题目序号样式 */
html.dark .question-number {
	color: var(--el-color-primary);
}

.filter-label {
	color: var(--primary-700);
}

html.dark .filter-label {
	color: var(--primary-300);
}

/* Element Plus 组件样式覆盖 */
:deep(.filter-select .el-input__wrapper) {
	border-color: var(--primary-200);
}

html.dark :deep(.filter-select .el-input__wrapper) {
	border-color: var(--black-200);
}

:deep(.filter-select .el-input__wrapper:hover) {
	border-color: var(--primary-400);
}

html.dark :deep(.filter-select .el-input__wrapper:hover) {
	border-color: var(--primary-600);
}

:deep(.filter-select .el-input__wrapper.is-focus) {
	border-color: var(--primary-500);
}

html.dark :deep(.filter-select .el-input__wrapper.is-focus) {
	border-color: var(--primary-500);
	box-shadow: 0 0 0 3px rgba(126, 34, 206, 0.2);
}

html.dark :deep(.filter-select .el-input__inner) {
	color: var(--white-100);
}

.customer-overview-bg {
	background: var(--el-bg-color-page);
}

/* Overview custom classes */
.overview-file-item {
	background-color: var(--el-fill-color-light);
}

.overview-grid-value {
	color: var(--el-text-color-primary);
	background-color: var(--primary);
}

.overview-empty-icon {
	color: var(--el-text-color-placeholder);
}

/* Questionnaire Response Card Header */
.questionnaire-response-card :deep(.el-card__header) {
	background-color: var(--primary-500);
	color: var(--el-color-white);
}
</style>
