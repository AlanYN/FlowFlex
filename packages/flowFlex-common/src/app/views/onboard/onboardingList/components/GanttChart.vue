<template>
	<div class="gantt-chart" ref="containerRef">
		<!-- Case 汇总 Header（顶部，带 rounded-t） -->
		<div
			v-if="summary"
			data-tour="gantt-case-summary"
			class="gantt-summary px-5 pt-4 pb-3 bg-white dark:bg-black-300 rounded-t-xl border-t border-x border-[--el-border-color-lighter]"
		>
			<!-- 第一行：Case 名称 + Code + Workflow -->
			<div class="flex items-center justify-between mb-4">
				<div class="flex items-baseline gap-2 min-w-0">
					<span class="font-bold text-base truncate">
						{{ summary.caseName }}
					</span>
					<span class="text-sm twhitespace-nowrap">({{ summary.caseCode }})</span>
				</div>
				<span class="text-xs whitespace-nowrap ml-4">
					Workflow: {{ summary.workflowName }}
				</span>
			</div>

			<!-- 第二行：四列统计 -->
			<div class="gantt-summary-grid">
				<!-- PROGRESS -->
				<div class="gantt-summary-col">
					<span class="gantt-summary-label">PROGRESS</span>
					<div class="gantt-summary-value">
						<span class="font-bold">
							{{ summary.completedStages }} of {{ summary.totalStages }} Stages
						</span>
						<span class="gantt-summary-sub">
							({{ summary.overallCompletionPercentage }}%)
						</span>
					</div>
					<div class="gantt-summary-bar-track">
						<div
							class="gantt-summary-bar-fill"
							:style="{ width: summary.overallCompletionPercentage + '%' }"
						></div>
					</div>
				</div>
				<!-- START -->
				<div class="gantt-summary-col">
					<span class="gantt-summary-label">START</span>
					<div class="gantt-summary-value font-bold">
						{{ formatShortDate(summary.plannedStartDate) }}
					</div>
				</div>
				<!-- ETA -->
				<div class="gantt-summary-col">
					<span class="gantt-summary-label">ETA</span>
					<div class="gantt-summary-value font-bold">
						{{ formatShortDate(summary.projectedEndDate || summary.plannedEndDate) }}
					</div>
				</div>
				<!-- VARIANCE -->
				<div class="gantt-summary-col">
					<span class="gantt-summary-label">
						VARIANCE
						<el-tooltip
							content="Difference between Projected and Planned"
							placement="top"
						>
							<el-icon class="gsp-info-icon" style="font-size: 11px; cursor: help">
								<InfoFilled />
							</el-icon>
						</el-tooltip>
					</span>
					<div
						class="gantt-summary-value font-bold"
						:class="
							caseVarianceDays > 0
								? 'gantt-variance-late'
								: caseVarianceDays < 0
								? 'gantt-variance-early'
								: 'gantt-variance-neutral'
						"
					>
						{{
							caseVarianceDays === 0
								? '—'
								: (caseVarianceDays > 0 ? '+' : '') + caseVarianceDays + 'd'
						}}
					</div>
				</div>
			</div>
		</div>

		<!-- 工具栏（紧贴在 Header 下方） -->
		<div
			data-tour="gantt-toolbar"
			class="gantt-toolbar flex items-center justify-between px-4 py-2 bg-white dark:bg-black-300 border border-[--el-border-color-lighter]"
			:class="summary ? 'border-t-0' : 'rounded-t-xl'"
		>
			<div class="flex items-center gap-2">
				<!-- 视图切换 -->
				<el-radio-group v-model="viewMode" @change="renderGantt">
					<el-radio-button value="day">Day</el-radio-button>
					<el-radio-button value="week">Week</el-radio-button>
					<el-radio-button value="month">Month</el-radio-button>
				</el-radio-group>

				<!-- Status 筛选 -->
				<el-popover
					v-model:visible="statusFilterVisible"
					trigger="click"
					placement="bottom-start"
					:width="180"
					popper-class="gantt-filter-popper"
				>
					<template #reference>
						<el-button class="gantt-filter-btn">
							{{
								selectedStatuses.length
									? `${selectedStatuses.length} status(es)`
									: 'All statuses'
							}}
							<el-icon class="ml-1"><ArrowDown /></el-icon>
						</el-button>
					</template>
					<div class="py-1">
						<div
							v-for="opt in allStatusOptions"
							:key="opt.value"
							class="gantt-filter-option"
							:class="{ 'is-checked': selectedStatuses.includes(opt.value) }"
							@click="toggleStatus(opt.value)"
						>
							<span class="gantt-filter-check">
								<el-icon v-if="selectedStatuses.includes(opt.value)">
									<Check />
								</el-icon>
							</span>
							<span
								class="gantt-filter-dot"
								:style="{ backgroundColor: getStatusBarColor(opt.value) }"
							></span>
							{{ opt.label }}
						</div>
						<div
							v-if="selectedStatuses.length"
							class="gantt-filter-clear"
							@click="selectedStatuses = []"
						>
							Clear
						</div>
					</div>
				</el-popover>

				<!-- Assignee 筛选 -->
				<el-popover
					v-model:visible="assigneeFilterVisible"
					trigger="click"
					placement="bottom-start"
					:width="220"
					popper-class="gantt-filter-popper"
				>
					<template #reference>
						<el-button class="gantt-filter-btn">
							{{
								selectedAssignees.length
									? `${selectedAssignees.length} assignee(s)`
									: 'All assignees'
							}}
							<el-icon class="ml-1"><ArrowDown /></el-icon>
						</el-button>
					</template>
					<div class="py-1">
						<div class="px-2 pb-1">
							<el-input
								v-model="assigneeSearchText"
								placeholder="Search assignees..."
								clearable
								:prefix-icon="Search"
							/>
						</div>
						<div class="px-3 py-1 text-xs text-gray-400">Match any (OR)</div>
						<div
							v-for="name in filteredAssigneeList"
							:key="name"
							class="gantt-filter-option"
							:class="{ 'is-checked': selectedAssignees.includes(name) }"
							@click="toggleAssignee(name)"
						>
							<span class="gantt-filter-check">
								<el-icon v-if="selectedAssignees.includes(name)"><Check /></el-icon>
							</span>
							{{ name }}
						</div>
						<div
							v-if="!filteredAssigneeList.length"
							class="px-3 py-2 text-xs text-gray-400"
						>
							No results
						</div>
						<div
							v-if="selectedAssignees.length"
							class="gantt-filter-clear"
							@click="selectedAssignees = []"
						>
							Clear
						</div>
					</div>
				</el-popover>
			</div>

			<div class="flex items-center gap-3">
				<span class="text-xs text-gray-400">Range: {{ rangeText }}</span>
				<el-button-group>
					<el-button @click="shiftTimeline(-1)" :icon="ArrowLeft" />
					<el-button @click="goToToday" data-tour="gantt-today-btn">Today</el-button>
					<el-button @click="shiftTimeline(1)" :icon="ArrowRight" />
				</el-button-group>
				<el-button @click="fitToContent" :icon="FullScreen">Fit</el-button>
			</div>
		</div>

		<!-- 甘特图主体 -->
		<div
			data-tour="gantt-body"
			class="gantt-body bg-white dark:bg-black-300 rounded-b-xl border-x border-b border-[--el-border-color-lighter]"
		>
			<div class="gantt-inner flex" v-loading="loading">
				<!-- 左侧：Stage 信息列（固定宽度，不随时间轴滚动） -->
				<div
					class="gantt-left-panel flex-shrink-0"
					:style="{ width: leftPanelWidth + 'px' }"
				>
					<!-- 列标题 -->
					<div
						class="gantt-header-row flex items-center border-b border-[--el-border-color-lighter] bg-gray-50 dark:bg-black-400 px-3 gap-2"
						:style="{ height: headerHeight + 'px' }"
					>
						<span
							class="text-xs font-semibold text-gray-500 uppercase tracking-wide w-5 flex-shrink-0"
						>
							#
						</span>
						<span
							class="text-xs font-semibold text-gray-500 uppercase tracking-wide flex-1"
						>
							Stage
						</span>
						<span
							class="text-xs font-semibold text-gray-500 uppercase tracking-wide w-20 flex-shrink-0"
						>
							Status
						</span>
						<span
							class="text-xs font-semibold text-gray-500 uppercase tracking-wide w-20 flex-shrink-0"
						>
							Assignee
						</span>
					</div>
					<!-- Stage 行：严格固定高度，内容 overflow hidden -->
					<div
						v-for="stage in filteredStages"
						:key="stage.stageId"
						class="gantt-row flex items-center gap-2 px-3 border-b border-[--el-border-color-lighter] cursor-pointer hover:bg-gray-50 dark:hover:bg-black-400 transition-colors overflow-hidden"
						:style="{ height: rowHeight + 'px' }"
						@mouseenter="onRowMouseenter(stage, $event)"
						@mouseleave="onBarMouseleave"
					>
						<!-- 序号 -->
						<span class="text-xs text-gray-400 w-5 flex-shrink-0">
							{{ stage.stageOrder }}
						</span>
						<!-- 名称 + 日期范围（竖排，overflow hidden） -->
						<div class="flex-1 min-w-0 overflow-hidden">
							<div class="flex items-center gap-1 min-w-0">
								<span
									class="w-2 h-2 rounded-full flex-shrink-0"
									:style="{ backgroundColor: stage.color || '#5b8cff' }"
								></span>
								<span
									class="text-sm text-gray-700 dark:text-gray-200 truncate"
									:title="stage.stageName"
								>
									{{ stage.stageName }}
								</span>
							</div>
							<div class="text-xs text-gray-400 truncate pl-3">
								{{ formatShortDate(stage.plannedStartDate) }} –
								{{ formatShortDate(stage.plannedEndDate) }}
							</div>
						</div>
						<!-- 状态 tag（固定宽度，左对齐） -->
						<div class="w-20 flex-shrink-0">
							<el-tag
								:type="
									getStatusTagType(
										stage.isBlocked ? 'Blocked' : stage.ganttStatus
									)
								"
								effect="plain"
								class="text-xs gantt-status-tag"
							>
								{{
									getStatusLabel(stage.isBlocked ? 'Blocked' : stage.ganttStatus)
								}}
							</el-tag>
						</div>
						<!-- Assignee（固定宽度，左对齐） -->
						<div class="w-20 flex-shrink-0">
							<span
								class="text-xs text-gray-500 dark:text-gray-400 truncate block"
								:title="stage.assignee?.[0]?.name"
							>
								{{ stage.assignee?.[0]?.name || '—' }}
							</span>
						</div>
					</div>
				</div>

				<!-- 右侧：ganttastic 时间轴，允许横向滚动 -->
				<div
					ref="ganttWrapperRef"
					class="flex-1 min-w-0 overflow-x-auto gantt-chart-wrapper"
				>
					<!-- key 绑定 viewMode + chartDateRange，切换视图时强制重新渲染 -->
					<g-gantt-chart
						:key="`${viewMode}-${chartDateRange.start}-${chartDateRange.end}`"
						:chart-start="chartDateRange.start"
						:chart-end="chartDateRange.end"
						:precision="ganttPrecision"
						bar-start="start"
						bar-end="end"
						:row-height="rowHeight"
						:current-time="true"
						color-scheme="default"
						class="gantt-lib"
						@mouseenter-bar="onBarMouseover($event.bar, $event.e)"
						@mouseleave-bar="onBarMouseleave"
					>
						<g-gantt-row
							v-for="row in ganttRows"
							:key="row.stageRef.stageId"
							:label="row.label"
							:bars="row.bars"
						/>
					</g-gantt-chart>
				</div>
			</div>
		</div>

		<!-- 图例 -->
		<div
			data-tour="gantt-legend"
			class="gantt-legend mt-4 px-4 py-3 bg-white dark:bg-black-300 rounded-xl border border-[--el-border-color-lighter]"
		>
			<div class="flex items-center gap-1 mb-2">
				<span class="text-xs font-semibold text-gray-500 uppercase tracking-wide">
					Legend
				</span>
			</div>
			<div class="flex flex-wrap items-center gap-4">
				<div
					v-for="item in legendItems"
					:key="item.label"
					class="flex items-center gap-1.5"
				>
					<span class="w-4 h-3 rounded" :style="{ backgroundColor: item.color }"></span>
					<span class="text-xs text-gray-600 dark:text-gray-400">{{ item.label }}</span>
				</div>
				<el-divider direction="vertical" />
				<div class="flex items-center gap-1.5">
					<span
						class="w-4 h-0.5 bg-gray-300 dark:bg-gray-600 border-t border-dashed border-gray-400"
					></span>
					<span class="text-xs text-gray-600 dark:text-gray-400">Planned</span>
				</div>
				<div class="flex items-center gap-1.5">
					<span
						class="w-4 h-3 rounded opacity-80"
						style="
							background: linear-gradient(
								90deg,
								#5b8cff 60%,
								rgba(91, 140, 255, 0.3) 100%
							);
						"
					></span>
					<span class="text-xs text-gray-600 dark:text-gray-400">Projected</span>
				</div>
				<div class="flex items-center gap-1.5">
					<span class="w-0.5 h-4 bg-red-500 rounded"></span>
					<span class="text-xs text-gray-600 dark:text-gray-400">Today</span>
				</div>
			</div>
		</div>

		<!-- Stage 详情 Popover (hover 触发) -->
		<el-popover
			v-model:visible="popoverVisible"
			:virtual-ref="popoverTriggerRef"
			virtual-triggering
			placement="right-start"
			:width="360"
			trigger="manual"
			popper-class="gantt-stage-popover"
		>
			<template v-if="selectedStage">
				<div
					class="gsp-wrap"
					@mouseenter="handlePopoverMouseEnter"
					@mouseleave="handlePopoverMouseLeave"
				>
					<!-- 顶部：Stage 序号 + 名称 + 状态 + 偏差 -->
					<div class="gsp-header">
						<div class="gsp-header__left">
							<span class="gsp-stage-num">STAGE {{ selectedStage.stageOrder }}</span>
							<h3 class="gsp-stage-name">{{ selectedStage.stageName }}</h3>
						</div>
						<div class="gsp-header__right">
							<span
								v-if="stageVarianceDays !== 0"
								class="gsp-variance"
								:class="
									stageVarianceDays > 0
										? 'gsp-variance--late'
										: 'gsp-variance--early'
								"
							>
								{{ stageVarianceDays > 0 ? 'Overdue' : 'Early' }}
								{{ stageVarianceDays > 0 ? '+' : '' }}{{ stageVarianceDays }}d
							</span>
							<el-tag
								:type="getStatusTagType(selectedStage.ganttStatus)"
								effect="light"
								class="gsp-status-tag"
							>
								{{ getStatusLabel(selectedStage.ganttStatus) }}
							</el-tag>
						</div>
					</div>

					<!-- 警告提示（超期 / 阻塞时显示） -->
					<div v-if="selectedStage.ganttStatus === 'Overdue'" class="gsp-alert">
						<el-icon class="gsp-alert__icon"><InfoFilled /></el-icon>
						<span v-if="selectedStage.ganttStatus === 'Overdue'">
							This stage is taking longer than planned.
						</span>
					</div>

					<!-- Blocked badge -->
					<div v-if="selectedStage.isBlocked" class="mb-3">
						<div class="blocked-badge">
							<span class="blocked-badge__icon">🚫</span>
							5
							<div class="blocked-badge__body">
								<div class="blocked-badge__title">
									Blocked
									<span v-if="selectedStage.blockedByName">
										by
										<strong>{{ selectedStage.blockedByName }}</strong>
									</span>
									<span
										v-if="selectedStage.blockedAt"
										class="blocked-badge__date"
									>
										· {{ formatDate(selectedStage.blockedAt) }}
									</span>
								</div>
								<div v-if="selectedStage.blockReason" class="blocked-badge__reason">
									{{ selectedStage.blockReason }}
								</div>
								<div
									v-if="selectedStage.expectedResolutionDate"
									class="blocked-badge__eta"
								>
									Expected resolution:
									<strong>
										{{ formatDate(selectedStage.expectedResolutionDate) }}
									</strong>
								</div>
							</div>
						</div>
					</div>

					<!-- Assignee -->
					<div v-if="selectedStage.assignee?.length" class="gsp-assignee">
						<div class="gsp-assignee__avatar">
							{{ (selectedStage.assignee[0]?.name || '?').charAt(0) }}
						</div>
						<div class="gsp-assignee__info">
							<span class="gsp-assignee__name">
								{{ selectedStage.assignee[0]?.name }}
							</span>
							<span
								v-if="selectedStage.assignee[0]?.email"
								class="gsp-assignee__email"
							>
								{{ selectedStage.assignee[0]?.email }}
							</span>
						</div>

						<!-- Co-assignee avatars stacked -->
						<div v-if="selectedStage.coAssignees?.length" class="gsp-co-avatars">
							<el-tooltip
								v-for="co in selectedStage.coAssignees.slice(0, 3)"
								:key="co.name"
								:content="`${co.name}${co.email ? ' · ' + co.email : ''}`"
								placement="top"
								:show-after="200"
							>
								<div class="gsp-co-avatars__item">
									{{ (co.name || '?').charAt(0) }}
								</div>
							</el-tooltip>
							<el-tooltip
								v-if="selectedStage.coAssignees.length > 3"
								:content="
									selectedStage.coAssignees
										.slice(3)
										.map((a) => a.name)
										.join(', ')
								"
								placement="top"
								:show-after="200"
							>
								<div class="gsp-co-avatars__item gsp-co-avatars__item--more">
									+{{ selectedStage.coAssignees.length - 3 }}
								</div>
							</el-tooltip>
						</div>
					</div>

					<!-- TIMELINE 区块 -->
					<div class="gsp-section">
						<div class="gsp-section__title">
							<el-icon><Calendar /></el-icon>
							TIMELINE
						</div>

						<!-- PLANNED -->
						<div class="gsp-time-block">
							<div class="gsp-time-block__label">
								PLANNED
								<el-tooltip
									content="Original plan, set when case started, does not change"
									placement="top"
								>
									<el-icon class="gsp-info-icon"><InfoFilled /></el-icon>
								</el-tooltip>
							</div>
							<div class="gsp-time-rows">
								<div class="gsp-time-row">
									<span>Start</span>
									<span>
										{{ formatShortDate(selectedStage.plannedStartDate) }}
									</span>
								</div>
								<div class="gsp-time-row">
									<span>ETA</span>
									<span>{{ formatShortDate(selectedStage.plannedEndDate) }}</span>
								</div>
								<div class="gsp-time-row">
									<span>Duration</span>
									<span>{{ selectedStage.estimatedDurationDays }} days</span>
								</div>
							</div>
						</div>

						<div class="gsp-divider"></div>

						<!-- PROJECTED -->
						<div class="gsp-time-block">
							<div class="gsp-time-block__label">
								PROJECTED
								<el-tooltip
									content="Current forecast, updates as stages complete"
									placement="top"
								>
									<el-icon class="gsp-info-icon"><InfoFilled /></el-icon>
								</el-tooltip>
							</div>
							<div class="gsp-time-rows">
								<div class="gsp-time-row">
									<span>Start</span>
									<span>
										{{
											selectedStage.projectedStartDate
												? formatShortDate(selectedStage.projectedStartDate)
												: '—'
										}}
									</span>
								</div>
								<div class="gsp-time-row">
									<span>End</span>
									<span
										:class="{
											'gsp-time-row__value--muted':
												!selectedStage.projectedEndDate,
										}"
									>
										{{
											selectedStage.projectedEndDate
												? formatShortDate(selectedStage.projectedEndDate)
												: 'TBD'
										}}
									</span>
								</div>
							</div>
						</div>

						<!-- ACTUAL（有实际开始时间才显示） -->
						<template v-if="selectedStage.actualStartDate">
							<div class="gsp-divider"></div>
							<div class="gsp-time-block">
								<div class="gsp-time-block__label">ACTUAL</div>
								<div class="gsp-time-rows">
									<div class="gsp-time-row">
										<span>Start</span>
										<span>
											{{ formatShortDate(selectedStage.actualStartDate) }}
										</span>
									</div>
									<div class="gsp-time-row">
										<span>End</span>
										<span
											:class="{
												'gsp-time-row__value--muted':
													!selectedStage.actualEndDate,
											}"
										>
											{{
												selectedStage.actualEndDate
													? formatShortDate(selectedStage.actualEndDate)
													: '—'
											}}
										</span>
									</div>
									<div
										v-if="selectedStage.daysElapsed !== undefined"
										class="gsp-time-row"
									>
										<span>Days Elapsed</span>
										<span>{{ selectedStage.daysElapsed }} days</span>
									</div>
								</div>
							</div>
						</template>
					</div>

					<!-- Last saved -->
					<div v-if="selectedStage.lastSavedBy" class="gsp-meta">
						Last saved by
						<strong>{{ selectedStage.lastSavedBy }}</strong>
						<span v-if="selectedStage.lastSavedAt">
							on {{ formatDateTime(selectedStage.lastSavedAt) }}
						</span>
					</div>

					<!-- COMPONENTS 区块 -->
					<div v-if="selectedStage.components" class="gsp-section">
						<div class="gsp-section__title gsp-section__title--with-pct">
							<span>COMPONENTS</span>
							<span class="gsp-section__pct">
								{{ selectedStage.completionPercentage }}%
							</span>
						</div>
						<div class="gsp-comp-rows">
							<div
								v-if="selectedStage.components.checklistsTotal > 0"
								class="gsp-comp-row"
							>
								Checklists:
								<strong>
									{{ selectedStage.components.checklistsCompleted }} /
									{{ selectedStage.components.checklistsTotal }}
								</strong>
								completed
							</div>
							<div
								v-if="selectedStage.components.questionnairesTotal > 0"
								class="gsp-comp-row"
							>
								Questionnaires:
								<strong>
									{{ selectedStage.components.questionnairesSubmitted }} /
									{{ selectedStage.components.questionnairesTotal }}
								</strong>
								submitted
							</div>
							<div
								v-if="selectedStage.components.fieldsTotal > 0"
								class="gsp-comp-row"
							>
								Fields:
								<strong>
									{{ selectedStage.components.fieldsFilled }} /
									{{ selectedStage.components.fieldsTotal }}
								</strong>
								filled
							</div>
							<div
								v-if="selectedStage.components.filesUploaded > 0"
								class="gsp-comp-row"
							>
								Files:
								<strong>{{ selectedStage.components.filesUploaded }}</strong>
								uploaded
							</div>
						</div>
					</div>

					<!-- Go to Stage 按钮 -->
					<div class="gsp-footer">
						<el-button class="gsp-action" @click.stop="handleGoToStage(selectedStage)">
							Go to Stage
							<el-icon><ArrowRight /></el-icon>
						</el-button>
					</div>
				</div>
			</template>
		</el-popover>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue';
import { useRouter } from 'vue-router';
import {
	ArrowLeft,
	ArrowRight,
	FullScreen,
	InfoFilled,
	Calendar,
	Check,
	ArrowDown,
	Search,
} from '@element-plus/icons-vue';
import dayjs from 'dayjs';
import isoWeek from 'dayjs/plugin/isoWeek';
import weekOfYear from 'dayjs/plugin/weekOfYear';
import { GGanttChart, GGanttRow } from '@infectoone/vue-ganttastic';
import { timeZoneConvert } from '@/hooks/time';
import {
	projectDate,
	projectTenMinutesSsecondsDate,
	ganttDateFormat,
} from '@/settings/projectSetting';
import { GanttCaseSummary, GanttStageItem, GanttStageStatus } from '#/gantt';

// ganttastic 内部依赖这两个 dayjs 插件
dayjs.extend(isoWeek);
dayjs.extend(weekOfYear);

// ========================= Props & Emits =========================

interface Props {
	stages: GanttStageItem[];
	summary: GanttCaseSummary | null;
	loading?: boolean;
	onboardingId?: string | number;
}

const props = withDefaults(defineProps<Props>(), { loading: false });

const emit = defineEmits<{ close: [] }>();

const router = useRouter();

// ========================= 状态 =========================

type ViewMode = 'day' | 'week' | 'month';

const containerRef = ref<HTMLElement | null>(null);
const ganttWrapperRef = ref<HTMLElement | null>(null);
const viewMode = ref<ViewMode>('week');
const timelineOffset = ref(0); // 时间轴偏移量（控制前/后移）

// Popover 状态
const popoverVisible = ref(false);
const selectedStage = ref<GanttStageItem | null>(null);
const popoverTriggerRef = ref<HTMLElement | null>(null);

// 筛选状态
const selectedStatuses = ref<GanttStageStatus[]>([]);
const selectedAssignees = ref<string[]>([]);
const assigneeSearchText = ref('');
const statusFilterVisible = ref(false);
const assigneeFilterVisible = ref(false);

// ========================= 常量 =========================

const rowHeight = 48;
const leftPanelWidth = 380;
// headerHeight 与 ganttastic 的 .g-timeaxis 实际高度一致（库内部固定 80px）
const headerHeight = 80;

// ========================= 计算属性 =========================

/** 图例数据 */
const legendItems = [
	{ label: 'Not Started', color: '#d9d9d9' },
	{ label: 'In Progress', color: '#5b8cff' },
	{ label: 'Completed', color: '#52c41a' },
	{ label: 'Overdue', color: '#ff4d4f' },
	{ label: 'Delayed', color: '#fa8c16' },
	{ label: 'Blocked', color: '#8c8c8c' },
];

/** Case 偏差天数 */
const caseVarianceDays = computed(() => {
	if (!props.summary?.projectedEndDate || !props.summary?.plannedEndDate) return 0;
	return dayjs(props.summary.projectedEndDate).diff(dayjs(props.summary.plannedEndDate), 'day');
});

/** 所有状态选项 */
const allStatusOptions: { value: GanttStageStatus; label: string }[] = [
	{ value: 'NotStarted', label: 'Not Started' },
	{ value: 'InProgress', label: 'In Progress' },
	{ value: 'Completed', label: 'Completed' },
	{ value: 'Overdue', label: 'Overdue' },
	{ value: 'Delayed', label: 'Delayed' },
	{ value: 'Blocked', label: 'Blocked' },
];

/** 所有 Assignee 去重 */
const allAssignees = computed(() => {
	const set = new Set<string>();
	props.stages.forEach((s) => {
		s.assignee?.forEach((a) => set.add(a.name));
		s.coAssignees?.forEach((a) => set.add(a.name));
	});
	return Array.from(set).sort();
});

/** 按搜索过滤的 Assignee */
const filteredAssigneeList = computed(() => {
	const q = assigneeSearchText.value.toLowerCase().trim();
	if (!q) return allAssignees.value;
	return allAssignees.value.filter((a) => a.toLowerCase().includes(q));
});

/** 过滤后的 Stage 列表 */
const filteredStages = computed(() => {
	return props.stages.filter((s) => {
		if (selectedStatuses.value.length > 0 && !selectedStatuses.value.includes(s.ganttStatus))
			return false;
		if (selectedAssignees.value.length > 0) {
			const stageAssignees = [
				...(s.assignee ?? []).map((a) => a.name),
				...(s.coAssignees ?? []).map((a) => a.name),
			];
			if (!selectedAssignees.value.some((a) => stageAssignees.includes(a))) return false;
		}
		return true;
	});
});

// ========================= Ganttastic 相关计算属性 =========================

/** 基础时间范围（来自 stages 数据） */
const baseDateRange = computed(() => {
	const stages = props.stages;
	const fallback = {
		minDate: dayjs().subtract(7, 'day'),
		maxDate: dayjs().add(30, 'day'),
	};
	if (!stages.length) return fallback;

	const allDates = stages
		.flatMap((s) => [
			s.plannedStartDate,
			s.plannedEndDate,
			s.projectedStartDate,
			s.projectedEndDate,
		])
		.filter(Boolean) as string[];

	// filter(Boolean) removes null; also exclude strings dayjs cannot parse
	const validTimestamps = allDates
		.map((d) => dayjs(d))
		.filter((d) => d.isValid())
		.map((d) => d.valueOf());

	if (!validTimestamps.length) return fallback;

	// 始终把今天纳入范围，确保 Today 线可见
	const allTimestamps = [...validTimestamps, dayjs().valueOf()];

	return {
		minDate: dayjs(Math.min(...allTimestamps)).subtract(3, 'day'),
		maxDate: dayjs(Math.max(...allTimestamps)).add(5, 'day'),
	};
});

/** 应用 offset 后的显示范围（ganttastic 需要 YYYY-MM-DD HH:mm 格式） */
const chartDateRange = computed(() => {
	const { minDate, maxDate } = baseDateRange.value;
	const unit = viewMode.value === 'day' ? 'day' : viewMode.value === 'week' ? 'week' : 'month';
	const step = viewMode.value === 'day' ? 7 : viewMode.value === 'week' ? 4 : 3;
	const offset = timelineOffset.value;
	return {
		start: minDate.add(offset * step, unit).format(ganttDateFormat),
		end: maxDate.add(offset * step, unit).format(ganttDateFormat),
	};
});

/** 范围文字（用于工具栏显示） */
const rangeText = computed(() => {
	const start = dayjs(chartDateRange.value.start).format(projectDate);
	const end = dayjs(chartDateRange.value.end).format(projectDate);
	return `${start} – ${end}`;
});

/** ganttastic precision 映射 */
const ganttPrecision = computed((): 'day' | 'week' | 'month' => {
	const map: Record<ViewMode, 'day' | 'week' | 'month'> = {
		day: 'day',
		week: 'week',
		month: 'month',
	};
	return map[viewMode.value] ?? 'week';
});

/**
 * 甘特图最小宽度：确保内容超出容器时出现横向滚动条
 * Day: 每天 60px；Week: 每周 160px；Month: 每月 200px
 */
const ganttMinWidth = computed(() => {
	const start = dayjs(chartDateRange.value.start);
	const end = dayjs(chartDateRange.value.end);
	let minPx = 800;
	if (viewMode.value === 'day') {
		minPx = Math.max(800, end.diff(start, 'day') * 60);
	} else if (viewMode.value === 'week') {
		minPx = Math.max(800, end.diff(start, 'week') * 160);
	} else {
		minPx = Math.max(800, end.diff(start, 'month') * 200);
	}
	return minPx + 'px';
});

/** 将 filteredStages 转换为 ganttastic row 格式 */
const ganttRows = computed(() => {
	return filteredStages.value.map((stage) => {
		const bars: any[] = [];

		// Planned 底层虚线条
		if (stage.plannedStartDate && stage.plannedEndDate) {
			bars.push({
				start: dayjs(stage.plannedStartDate).format(ganttDateFormat),
				end: dayjs(stage.plannedEndDate).format(ganttDateFormat),
				ganttBarConfig: {
					id: `${stage.stageId}-planned`,
					label: '',
					style: {
						background: 'var(--el-fill-color)',
						border: '1px dashed var(--el-border-color)',
						borderRadius: '4px',
						height: '24px',
						zIndex: 1,
					},
					stageRef: stage,
					isPlanned: true,
				},
			});
		}

		// Projected/Actual 主色彩条
		const projStart =
			stage.actualStartDate || stage.projectedStartDate || stage.plannedStartDate;
		const projEnd = stage.actualEndDate || stage.projectedEndDate || stage.plannedEndDate;

		if (projStart && projEnd) {
			const bgColor = getStatusBarColor(stage.isBlocked ? 'Blocked' : stage.ganttStatus);
			let label = '';
			if (
				(stage.ganttStatus === 'InProgress' || stage.ganttStatus === 'Overdue') &&
				stage.completionPercentage > 20
			) {
				label = `${stage.completionPercentage}%`;
			}
			if (stage.isBlocked) label = '🚫';

			bars.push({
				start: dayjs(projStart).format(ganttDateFormat),
				end: dayjs(projEnd).format(ganttDateFormat),
				ganttBarConfig: {
					id: `${stage.stageId}-projected`,
					label,
					hasHandles: false,
					style: {
						background: bgColor,
						borderRadius: '4px',
						height: '24px',
						opacity: stage.isBlocked ? '0.6' : '1',
						cursor: 'pointer',
						zIndex: 2,
					},
					stageRef: stage,
				},
			});
		}

		return {
			label: `${stage.stageOrder}`,
			bars,
			stageRef: stage,
		};
	});
});

// ========================= 方法 =========================

function getStatusBarColor(status: GanttStageStatus): string {
	const colors: Record<GanttStageStatus, string> = {
		NotStarted: '#d9d9d9',
		InProgress: '#5b8cff',
		Completed: '#52c41a',
		Overdue: '#ff4d4f',
		Delayed: '#fa8c16',
		Blocked: '#8c8c8c',
	};
	return colors[status] ?? '#d9d9d9';
}

function getStatusTagType(
	status: GanttStageStatus
): 'primary' | 'success' | 'warning' | 'danger' | 'info' | undefined {
	const map: Record<
		GanttStageStatus,
		'primary' | 'success' | 'warning' | 'danger' | 'info' | undefined
	> = {
		NotStarted: 'info',
		InProgress: 'primary',
		Completed: 'success',
		Overdue: 'danger',
		Delayed: 'warning',
		Blocked: 'info',
	};
	return map[status];
}

function getStatusLabel(status: GanttStageStatus): string {
	const labels: Record<GanttStageStatus, string> = {
		NotStarted: 'Not Started',
		InProgress: 'In Progress',
		Completed: 'Completed',
		Overdue: 'Overdue',
		Delayed: 'Delayed',
		Blocked: 'Blocked',
	};
	return labels[status] ?? status;
}

function formatDate(dateString: string | null | undefined): string {
	if (!dateString) return '—';
	return timeZoneConvert(dateString, false, projectTenMinutesSsecondsDate);
}

/** 短日期格式（用于左侧列表区间、Summary Header 等） */
function formatShortDate(dateString: string | null | undefined): string {
	if (!dateString) return '—';
	return timeZoneConvert(dateString, false, projectDate);
}

function formatDateTime(dateString: string | null | undefined): string {
	if (!dateString) return '—';
	return timeZoneConvert(dateString, false, projectTenMinutesSsecondsDate);
}

function shiftTimeline(direction: number) {
	timelineOffset.value += direction;
}

function goToToday() {
	if (timelineOffset.value !== 0) {
		// 需要重置范围，key 变化会触发 ganttastic 重渲染，等渲染完成再滚动
		timelineOffset.value = 0;
		// ganttastic 重挂载需要多个 tick，用 setTimeout 保证 DOM 稳定
		setTimeout(() => scrollToToday(), 100);
	} else {
		// 范围未变，直接滚动即可
		scrollToToday();
	}
}

/** 滚动到 Today 线居中位置 */
function scrollToToday() {
	nextTick(() => {
		const wrapper = ganttWrapperRef.value;
		if (!wrapper) return;
		const todayLine = wrapper.querySelector<HTMLElement>(
			'.g-grid-current-time, .g-grid-current-time-marker'
		);
		if (todayLine) {
			const lineLeft = todayLine.offsetLeft;
			const wrapperWidth = wrapper.clientWidth;
			wrapper.scrollLeft = lineLeft - wrapperWidth / 2;
		}
	});
}

function fitToContent() {
	timelineOffset.value = 0;
}

function renderGantt() {
	timelineOffset.value = 0;
}

function toggleStatus(status: GanttStageStatus) {
	const idx = selectedStatuses.value.indexOf(status);
	if (idx > -1) selectedStatuses.value.splice(idx, 1);
	else selectedStatuses.value.push(status);
}

function toggleAssignee(name: string) {
	const idx = selectedAssignees.value.indexOf(name);
	if (idx > -1) selectedAssignees.value.splice(idx, 1);
	else selectedAssignees.value.push(name);
}

// ========================= Bar Hover（Popover） =========================

let hideTimer: ReturnType<typeof setTimeout> | null = null;

function onBarMouseover(bar: any, event: MouseEvent) {
	const stageRef = bar?.ganttBarConfig?.stageRef as GanttStageItem | undefined;
	if (!stageRef || bar?.ganttBarConfig?.isPlanned) return; // 忽略 planned 底层条
	if (hideTimer) {
		clearTimeout(hideTimer);
		hideTimer = null;
	}
	selectedStage.value = stageRef;
	popoverTriggerRef.value = event.currentTarget as HTMLElement;
	popoverVisible.value = true;
}

/** 左侧列表行 hover 触发 Popover（兜底：无甘特条的 stage 也能看到详情） */
function onRowMouseenter(stage: GanttStageItem, event: MouseEvent) {
	if (hideTimer) {
		clearTimeout(hideTimer);
		hideTimer = null;
	}
	selectedStage.value = stage;
	popoverTriggerRef.value = event.currentTarget as HTMLElement;
	popoverVisible.value = true;
}

function onBarMouseleave() {
	hideTimer = setTimeout(() => {
		popoverVisible.value = false;
		hideTimer = null;
	}, 200);
}

function handlePopoverMouseEnter() {
	if (hideTimer) {
		clearTimeout(hideTimer);
		hideTimer = null;
	}
}

function handlePopoverMouseLeave() {
	hideTimer = setTimeout(() => {
		popoverVisible.value = false;
		hideTimer = null;
	}, 100);
}

function handleGoToStage(stage: GanttStageItem) {
	const onboardingId = props.onboardingId ?? props.summary?.onboardingId;
	if (!onboardingId) {
		popoverVisible.value = false;
		return;
	}
	popoverVisible.value = false;
	emit('close');
	router.push({
		path: '/onboard/onboardDetail',
		query: {
			onboardingId: String(onboardingId),
			stageId: stage.stageId,
		},
	});
}

const stageVarianceDays = computed(() => {
	const s = selectedStage.value;
	if (!s?.projectedEndDate || !s?.plannedEndDate) return 0;
	return dayjs(s.projectedEndDate).diff(dayjs(s.plannedEndDate), 'day');
});

// ========================= 生命周期 =========================

watch(
	() => props.stages,
	() => {
		timelineOffset.value = 0;
		// stages 更新后等 ganttastic 渲染完成再滚到 Today
		setTimeout(() => scrollToToday(), 100);
	},
	{ deep: true }
);

defineExpose({ scrollToToday });
</script>

<style scoped lang="scss">
.gantt-chart {
	user-select: none;
}

.gantt-left-panel {
	border-right: 1px solid var(--el-border-color-lighter);
}

/* 状态 tag：固定宽度列内不换行，超出截断 */
.gantt-status-tag {
	max-width: 100%;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
	display: inline-flex;
}

/* ganttastic 包裹层样式 */
.gantt-chart-wrapper {
	/* 隐藏库自带的左侧 row label */
	:deep(.g-gantt-row-label) {
		display: none !important;
		width: 0 !important;
		min-width: 0 !important;
		padding: 0 !important;
	}

	/* 整个甘特图：去掉白色背景和外边框 */
	:deep(.g-gantt-chart) {
		border: none !important;
		background: transparent !important;
		min-width: v-bind(ganttMinWidth);
	}

	/* 时间轴整体容器 */
	:deep(.g-timeaxis) {
		background: var(--el-bg-color) !important;
		border-bottom: 1px solid var(--el-border-color-lighter) !important;
	}

	/* 上层月份/年份行 */
	:deep(.g-upper-timeunit) {
		background: var(--el-fill-color-light) !important;
		color: var(--el-text-color-secondary) !important;
		font-size: 11px !important;
		border-right: 1px solid var(--el-border-color-lighter) !important;
		border-bottom: 1px solid var(--el-border-color-lighter) !important;
	}

	/* 下层时间单元格容器 */
	:deep(.g-timeunits-container) {
		background: var(--el-bg-color) !important;
	}

	/* 下层日期/周单元格 */
	:deep(.g-timeunit) {
		background: var(--el-bg-color) !important;
		color: var(--el-text-color-secondary) !important;
		font-size: 11px !important;
		border-right: 1px solid var(--el-border-color-lighter) !important;
	}

	/* 行容器背景 */
	:deep(.g-gantt-rows-container) {
		background: var(--el-bg-color) !important;
	}

	/* 每一行：去掉自身边框，由 bars-container 的 border-top/bottom 来画分割线 */
	:deep(.g-gantt-row) {
		background: transparent !important;
		border-bottom: none !important;
	}

	/* 条形图区域容器：覆盖库硬编码的 #eaeaea 边框 */
	:deep(.g-gantt-row > .g-gantt-row-bars-container) {
		background: transparent !important;
		border-top: 1px solid var(--el-border-color-lighter) !important;
		border-bottom: 1px solid var(--el-border-color-lighter) !important;
	}

	/* 网格竖线 */
	:deep(.g-grid-line) {
		background: var(--el-border-color-lighter) !important;
	}

	/* 网格容器 */
	:deep(.g-grid-container) {
		background: transparent !important;
	}

	/* Today 线：深色主题下确保可见（覆盖 default scheme 的黑色 markerCurrentTime） */
	:deep(.g-grid-current-time-marker) {
		border-left: 2px solid var(--el-color-danger) !important;
		opacity: 1 !important;
	}
	:deep(.g-grid-current-time-text) {
		color: var(--el-color-danger) !important;
	}

	/* 横向滚动条美化 */
	&::-webkit-scrollbar {
		height: 6px;
	}
	&::-webkit-scrollbar-track {
		background: transparent;
	}
	&::-webkit-scrollbar-thumb {
		background: var(--el-border-color);
		border-radius: 3px;
	}
}

/* ===== Case 汇总 Header — 四列网格 ===== */
.gantt-summary-grid {
	display: grid;
	grid-template-columns: 2fr 1fr 1fr 1fr;
	gap: 0;
}

.gantt-summary-col {
	padding: 0 16px 0 0;

	/* 第一列（PROGRESS）不要右 padding */
	&:first-child {
		padding-right: 20px;
	}

	/* 非第一列左边加竖线 */
	& + & {
		padding-left: 16px;
		border-left: 1px solid var(--el-border-color-lighter);
	}
}

.gantt-summary-label {
	display: block;
	font-size: 10px;
	font-weight: 600;
	color: var(--el-text-color-secondary);
	letter-spacing: 0.08em;
	text-transform: uppercase;
	margin-bottom: 4px;
}

.gantt-summary-value {
	font-size: 18px;
	color: var(--el-text-color-primary);
	line-height: 1.3;
	margin-bottom: 6px;
}

.gantt-summary-sub {
	font-size: 13px;
	font-weight: 400;
	color: var(--el-text-color-secondary);
	margin-left: 4px;
}

/* 细进度条 */
.gantt-summary-bar-track {
	height: 4px;
	background-color: var(--el-fill-color);
	border-radius: 2px;
	overflow: hidden;
	margin-top: 2px;
}

.gantt-summary-bar-fill {
	height: 100%;
	background-color: var(--el-color-primary);
	border-radius: 2px;
	transition: width 0.3s ease;
}

/* Variance 颜色 */
.gantt-variance-late {
	color: var(--el-color-danger);
}
.gantt-variance-early {
	color: var(--el-color-success);
}
.gantt-variance-neutral {
	color: var(--el-text-color-secondary);
}
</style>

<style lang="scss">
/* ===== Stage 详情 Popover ===== */
.gantt-stage-popover.el-popover {
	padding: 0 !important;
	border: 1px solid var(--el-border-color-light) !important;
	border-radius: var(--el-border-radius-large, 16px) !important;
	box-shadow: var(--el-box-shadow) !important;
	background-color: var(--el-bg-color-overlay) !important;
	overflow: hidden;
	max-height: 80vh;
	overflow-y: auto;

	/* 鼠标移入 Popover 时阻止消失 */
	pointer-events: auto;
}

.gsp-wrap {
	padding: 18px;
	background-color: var(--el-bg-color-overlay);
	color: var(--el-text-color-regular);

	/* blocked-badge reason 在 Popover 里不截断，加滚动 */
	.blocked-badge__reason {
		-webkit-line-clamp: unset;
		overflow-y: auto;
		max-height: 120px;
		text-overflow: unset;
		display: block;
	}
}

/* 顶部 */
.gsp-header {
	display: flex;
	align-items: flex-start;
	justify-content: space-between;
	gap: 8px;
	margin-bottom: 10px;

	&__left {
		min-width: 0;
		flex: 1;
	}

	&__right {
		display: flex;
		flex-direction: column;
		align-items: flex-end;
		gap: 4px;
		flex-shrink: 0;
	}
}

.gsp-stage-num {
	display: block;
	font-size: 11px;
	font-weight: 600;
	color: var(--el-text-color-secondary);
	letter-spacing: 0.06em;
	text-transform: uppercase;
	margin-bottom: 2px;
}

.gsp-stage-name {
	font-size: 16px;
	font-weight: 700;
	color: var(--el-text-color-primary);
	margin: 0;
	line-height: 1.3;
}

.gsp-variance {
	font-size: 11px;
	font-weight: 600;
	color: var(--el-text-color-secondary);

	&--late {
		color: var(--el-color-danger);
	}
	&--early {
		color: var(--el-color-success);
	}
}

/* 警告提示行 */
.gsp-alert {
	display: flex;
	align-items: flex-start;
	gap: 6px;
	padding: 8px 10px;
	background-color: var(--el-fill-color-light);
	border-radius: var(--el-border-radius-small, 8px);
	font-size: 12px;
	color: var(--el-text-color-secondary);
	margin-bottom: 10px;

	&__icon {
		flex-shrink: 0;
		margin-top: 1px;
		color: var(--el-text-color-secondary);
	}
}

/* Assignee */
.gsp-assignee {
	display: flex;
	align-items: center;
	gap: 10px;
	margin-bottom: 14px;

	&__avatar {
		width: 32px;
		height: 32px;
		border-radius: 50%;
		background-color: var(--el-color-primary-light-7);
		color: var(--white-100);
		display: flex;
		align-items: center;
		justify-content: center;
		font-size: 13px;
		font-weight: 700;
		flex-shrink: 0;
		text-transform: uppercase;
	}

	&__info {
		min-width: 0;
	}

	&__name {
		font-size: 15px;
		font-weight: 700;
		color: var(--el-text-color-primary);
		display: block;
		line-height: 1.3;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
		max-width: 160px;
	}

	&__email {
		font-size: 12px;
		font-weight: 400;
		color: var(--el-text-color-secondary);
		display: block;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	&__role {
		flex-shrink: 0;
		margin-left: auto;
		font-size: 10px;
		font-weight: 500;
		color: var(--el-text-color-secondary);
		background-color: var(--el-fill-color);
		border: 1px solid var(--el-border-color-lighter);
		border-radius: 4px;
		padding: 1px 6px;
		line-height: 1.6;
		white-space: nowrap;
	}

	&--co {
		margin-top: 6px;
		padding-top: 6px;
		border-top: 1px dashed var(--el-border-color-lighter);
	}

	&__avatar--co {
		width: 26px;
		height: 26px;
		font-size: 11px;
		background-color: var(--el-fill-color);
		color: var(--el-text-color-secondary);
	}
}

/* Co-assignee 堆叠头像组 */
.gsp-co-avatars {
	display: flex;
	flex-direction: row;
	margin-left: auto;
	flex-shrink: 0;

	&__item {
		width: 26px;
		height: 26px;
		border-radius: 50%;
		background-color: var(--el-fill-color);
		color: var(--el-text-color-secondary);
		border: 2px solid var(--el-bg-color-overlay);
		display: flex;
		align-items: center;
		justify-content: center;
		font-size: 11px;
		font-weight: 700;
		text-transform: uppercase;
		cursor: default;
		transition: transform 0.15s;

		& + & {
			margin-left: -8px;
		}

		&:hover {
			transform: translateY(-2px);
			z-index: 1;
		}
	}

	&__item--more {
		background-color: var(--el-border-color-light);
		color: var(--el-text-color-regular);
		font-size: 10px;
		font-weight: 600;
		letter-spacing: -0.5px;
	}
}

.gsp-section {
	padding: 12px 0 0;
	margin-bottom: 8px;

	&__title {
		display: flex;
		align-items: center;
		gap: 5px;
		font-size: 10px;
		font-weight: 700;
		color: var(--el-text-color-secondary);
		letter-spacing: 0.08em;
		text-transform: uppercase;
		margin-bottom: 12px;
	}

	&__title--with-pct {
		justify-content: space-between;
	}

	&__pct {
		font-size: 13px;
		font-weight: 700;
		color: var(--el-text-color-primary);
	}
}

/* TIMELINE 时间块 */
.gsp-time-block {
	margin-bottom: 2px;

	&__label {
		display: flex;
		align-items: center;
		gap: 4px;
		font-size: 10px;
		font-weight: 700;
		color: var(--el-text-color-secondary);
		letter-spacing: 0.08em;
		text-transform: uppercase;
		margin-bottom: 8px;
	}
}

.gsp-info-icon {
	font-size: 12px;
	color: var(--el-text-color-placeholder);
	cursor: help;
}

/* 单列纵排：每行 label 左对齐、value 右对齐 */
.gsp-time-rows {
	display: flex;
	flex-direction: column;
	gap: 6px;
}

/* 单个时间字段：label 左，value 右 */
.gsp-time-row {
	display: flex;
	flex-direction: row;
	align-items: baseline;
	justify-content: space-between;
	gap: 8px;

	/* label */
	span:first-child {
		font-size: 13px;
		font-weight: 400;
		color: var(--el-text-color-regular);
		letter-spacing: 0;
		text-transform: none;
		flex-shrink: 0;
	}

	/* value */
	span:last-child {
		font-size: 13px;
		font-weight: 700;
		color: var(--el-text-color-primary);
		line-height: 1.2;
		text-align: right;
	}

	/* Duration、Days Elapsed 等不需要特殊处理，已经在同一行 */
	&--full {
		/* 保留兼容，不再需要 grid-column */
	}

	&__value--muted span:last-child,
	span.gsp-time-row__value--muted {
		color: var(--el-text-color-placeholder) !important;
		font-weight: 400;
	}
}

/* 分割线 */
.gsp-divider {
	height: 1px;
	background-color: var(--el-border-color-lighter);
	margin: 12px 0;
}

/* Last saved meta */
.gsp-meta {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	margin-bottom: 12px;
	padding-top: 8px;
	border-top: 1px solid var(--el-border-color-lighter);

	strong {
		color: var(--el-text-color-primary);
		font-weight: 600;
	}
}

/* Components */
.gsp-comp-rows {
	display: flex;
	flex-direction: column;
	gap: 4px;
}

.gsp-comp-row {
	font-size: 13px;
	color: var(--el-text-color-secondary);

	strong {
		color: var(--el-text-color-primary);
	}
}

/* Footer */
.gsp-footer {
	display: flex;
	justify-content: flex-end;
	padding-top: 4px;
	border-top: 1px solid var(--el-border-color-lighter);
	margin-top: 4px;
}

/* Go to Stage 按钮 */
.gsp-action {
	display: inline-flex;
	align-items: center;
	gap: 5px;
	padding: 5px 14px;
	border: 1px solid var(--el-border-color);
	border-radius: var(--el-border-radius-round, 20px);
	background-color: var(--el-fill-color-blank);
	color: var(--el-text-color-regular);
	font-size: 13px;
	font-weight: 500;
	cursor: pointer;
	transition:
		background-color 0.15s,
		border-color 0.15s,
		color 0.15s;

	&:hover {
		background-color: var(--el-color-primary-light-9);
		border-color: var(--el-color-primary-light-7);
		color: var(--el-color-primary);
	}

	&:active {
		background-color: var(--el-color-primary-light-8);
	}
}

/* ===== Blocked badge（与 OnboardingProgress 保持一致）===== */
.blocked-badge {
	display: flex;
	align-items: flex-start;
	gap: 6px;
	padding: 6px 8px;
	background-color: #fff7ed;
	border: 1px solid #fed7aa;
	border-radius: 8px;
	font-size: 12px;

	&__icon {
		flex-shrink: 0;
		font-size: 12px;
		line-height: 1.5;
	}

	&__body {
		min-width: 0;
		flex: 1;
	}

	&__title {
		color: #c2410c;
		font-weight: 500;
		line-height: 1.4;

		strong {
			font-weight: 600;
		}
	}

	&__date {
		color: #ea580c;
		font-weight: 400;
	}

	&__reason {
		margin-top: 2px;
		color: #9a3412;
		line-height: 1.4;
		word-break: break-all;
		max-height: 120px;
		overflow-y: auto;
	}

	&__eta {
		margin-top: 4px;
		font-size: 11px;
		color: #9a3412;

		strong {
			color: #c2410c;
			font-weight: 600;
		}
	}
}
</style>

<style lang="scss">
/* ===== 筛选下拉按钮 ===== */
.gantt-filter-btn.el-button {
	border-color: var(--el-border-color) !important;
	background-color: var(--el-fill-color-blank) !important;
	color: var(--el-text-color-regular) !important;
	font-size: 12px;
	padding: 0 10px;

	&:hover {
		border-color: var(--el-color-primary-light-7) !important;
		color: var(--el-color-primary) !important;
	}
}

/* ===== 筛选 Popper ===== */
.gantt-filter-popper.el-popover {
	padding: 4px 0 !important;
	min-width: 160px;
}

.gantt-filter-option {
	display: flex;
	align-items: center;
	gap: 8px;
	padding: 6px 12px;
	font-size: 13px;
	color: var(--el-text-color-regular);
	cursor: pointer;
	transition: background-color 0.12s;

	&:hover {
		background-color: var(--el-fill-color-light);
	}

	&.is-checked {
		color: var(--el-color-primary);
	}
}

.gantt-filter-check {
	width: 14px;
	height: 14px;
	flex-shrink: 0;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 12px;
	color: var(--el-color-primary);
}

.gantt-filter-dot {
	width: 8px;
	height: 8px;
	border-radius: 50%;
	flex-shrink: 0;
}

.gantt-filter-clear {
	margin: 4px 8px 0;
	padding: 4px 8px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
	cursor: pointer;
	border-top: 1px solid var(--el-border-color-lighter);
	text-align: right;

	&:hover {
		color: var(--el-color-primary);
	}
}
</style>
