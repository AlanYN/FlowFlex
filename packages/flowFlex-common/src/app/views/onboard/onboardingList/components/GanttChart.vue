<template>
	<div class="gantt-chart" ref="containerRef">
		<!-- Case 汇总 Header（顶部，带 rounded-t） -->
		<div
			v-if="summary"
			class="gantt-summary px-5 pt-4 pb-3 bg-white dark:bg-black-300 rounded-t-xl border-t border-x border-[--el-border-color-lighter]"
		>
			<!-- 第一行：Case 名称 + Code + Workflow -->
			<div class="flex items-center justify-between mb-4">
				<div class="flex items-baseline gap-2 min-w-0">
					<span class="font-bold text-base text-gray-800 dark:text-gray-100 truncate">
						{{ summary.caseName }}
					</span>
					<span class="text-sm text-gray-400 whitespace-nowrap">
						({{ summary.caseCode }})
					</span>
				</div>
				<span class="text-xs text-gray-400 whitespace-nowrap ml-4">
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
						{{ formatDate(summary.plannedStartDate) }}
					</div>
				</div>
				<!-- ETA -->
				<div class="gantt-summary-col">
					<span class="gantt-summary-label">ETA</span>
					<div class="gantt-summary-value font-bold">
						{{ formatDate(summary.projectedEndDate || summary.plannedEndDate) }}
					</div>
				</div>
				<!-- VARIANCE -->
				<div class="gantt-summary-col">
					<span class="gantt-summary-label">VARIANCE</span>
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
			class="gantt-toolbar flex items-center justify-between px-4 py-2 bg-white dark:bg-black-300 border border-[--el-border-color-lighter]"
			:class="summary ? 'border-t-0' : 'rounded-t-xl'"
		>
			<div class="flex items-center gap-2">
				<!-- 视图切换 -->
				<el-radio-group v-model="viewMode" size="small" @change="renderGantt">
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
						<el-button size="small" class="gantt-filter-btn">
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
						<el-button size="small" class="gantt-filter-btn">
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
								size="small"
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
				<el-button-group size="small">
					<el-button @click="shiftTimeline(-1)" :icon="ArrowLeft" />
					<el-button @click="goToToday" size="small">Today</el-button>
					<el-button @click="shiftTimeline(1)" :icon="ArrowRight" />
				</el-button-group>
				<el-button size="small" @click="fitToContent" :icon="FullScreen">Fit</el-button>
			</div>
		</div>

		<!-- 甘特图主体 -->
		<div
			class="gantt-body bg-white dark:bg-black-300 rounded-b-xl border-x border-b border-[--el-border-color-lighter] overflow-hidden"
		>
			<div class="gantt-inner flex" v-loading="loading">
				<!-- 左侧：Stage 信息列 -->
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
							class="text-xs font-semibold text-gray-500 uppercase tracking-wide w-24 flex-shrink-0 text-right"
						>
							Assignee
						</span>
					</div>
					<!-- Stage 行 -->
					<div
						v-for="stage in filteredStages"
						:key="stage.stageId"
						class="gantt-row flex items-start gap-2 px-3 py-2 border-b border-[--el-border-color-lighter] cursor-pointer hover:bg-gray-50 dark:hover:bg-black-400 transition-colors"
						:style="{ height: rowHeight + 'px' }"
					>
						<!-- 序号 -->
						<span class="text-xs text-gray-400 w-5 flex-shrink-0 pt-0.5">
							{{ stage.stageOrder }}
						</span>
						<!-- 名称 + 日期范围 -->
						<div class="flex-1 min-w-0">
							<div class="flex items-center gap-1.5">
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
								<el-tag
									:type="getStatusTagType(stage.status)"
									size="small"
									effect="plain"
									class="flex-shrink-0 text-xs"
								>
									{{ getStatusLabel(stage.status) }}
								</el-tag>
							</div>
							<div class="text-xs text-gray-400 mt-0.5 pl-3.5">
								{{ formatDate(stage.plannedStartDate) }} –
								{{ formatDate(stage.plannedEndDate) }}
							</div>
						</div>
						<!-- Assignee -->
						<div class="w-24 flex-shrink-0 text-right">
							<span
								class="text-xs text-gray-500 dark:text-gray-400 truncate block"
								:title="stage.assignee?.[0]"
							>
								{{ stage.assignee?.[0] || '—' }}
							</span>
						</div>
					</div>
				</div>

				<!-- 分割线 -->
				<div class="w-px bg-gray-200 dark:bg-gray-700 flex-shrink-0"></div>

				<!-- 右侧：时间轴区域 -->
				<div
					class="gantt-right-panel flex-1 overflow-x-auto"
					ref="rightPanelRef"
					@scroll="handleScroll"
				>
					<div :style="{ width: totalTimelineWidth + 'px', minWidth: '100%' }">
						<!-- 时间刻度 header -->
						<div
							class="gantt-timeline-header flex border-b border-[--el-border-color-lighter] bg-gray-50 dark:bg-black-400"
							:style="{ height: headerHeight + 'px' }"
						>
							<div
								v-for="col in timelineColumns"
								:key="col.key"
								class="flex-shrink-0 flex items-center justify-center border-r border-[--el-border-color-lighter] last:border-r-0"
								:style="{ width: col.width + 'px' }"
								:class="{ 'font-semibold text-primary-500': col.isToday }"
							>
								<span
									class="text-xs text-gray-500 dark:text-gray-400 select-none"
									:class="{
										'text-primary-500 dark:text-primary-400': col.isToday,
									}"
								>
									{{ col.label }}
								</span>
							</div>
						</div>

						<!-- 甘特图行 -->
						<div class="gantt-rows relative">
							<!-- Today 竖线 -->
							<div
								v-if="todayX !== null"
								class="absolute top-0 bottom-0 z-10 pointer-events-none"
								:style="{
									left: todayX + 'px',
									width: '2px',
									background: 'rgba(255, 77, 79, 0.7)',
								}"
							>
								<div
									class="absolute -top-1 left-1/2 -translate-x-1/2 text-xs text-red-500 font-semibold whitespace-nowrap"
								>
									Today
								</div>
							</div>

							<!-- 每个 Stage 的行 -->
							<div
								v-for="(stage, idx) in filteredStages"
								:key="stage.stageId"
								class="gantt-bar-row relative flex items-center border-b border-[--el-border-color-lighter]"
								:style="{
									height: rowHeight + 'px',
									width: totalTimelineWidth + 'px',
								}"
								:class="{ 'bg-gray-50/50 dark:bg-black-400/30': idx % 2 === 1 }"
							>
								<!-- 列背景格线 -->
								<div
									v-for="col in timelineColumns"
									:key="col.key"
									class="absolute top-0 bottom-0 border-r border-[--el-border-color-lighter]"
									:class="{ 'bg-red-50/30 dark:bg-red-900/10': col.isToday }"
									:style="{ left: col.x + 'px', width: col.width + 'px' }"
								></div>

								<!-- Planned 条（灰色半透明底条） -->
								<div
									v-if="getBarStyle(stage, 'planned')"
									class="absolute rounded gantt-bar-planned"
									:style="getBarStyle(stage, 'planned')"
									:title="`Planned: ${formatDate(
										stage.plannedStartDate
									)} → ${formatDate(stage.plannedEndDate)}`"
								></div>

								<!-- Projected/Actual 条（主色彩条） -->
								<div
									v-if="getBarStyle(stage, 'projected')"
									class="absolute rounded gantt-bar-projected cursor-pointer transition-all hover:brightness-110"
									:style="getBarStyle(stage, 'projected')"
									@mouseenter="showStagePopover(stage, $event)"
									@mouseleave="hideStagePopover"
									@click.stop
								>
									<!-- 进度填充（InProgress / Overdue 时显示完成度） -->
									<div
										v-if="
											stage.status === 'InProgress' ||
											stage.status === 'Overdue'
										"
										class="absolute left-0 top-0 bottom-0 rounded gantt-bar-progress"
										:style="{
											width: stage.completionPercentage + '%',
											backgroundColor: getProgressColor(stage.status),
										}"
									></div>
									<!-- 阻塞标记 -->
									<div
										v-if="stage.isBlocked"
										class="absolute right-1 top-1/2 -translate-y-1/2 text-white text-xs font-bold"
									>
										🚫
									</div>
									<!-- 完成度文字 -->
									<span
										v-if="
											(stage.status === 'InProgress' ||
												stage.status === 'Overdue') &&
											stage.completionPercentage > 20
										"
										class="absolute inset-0 flex items-center justify-center text-xs font-semibold text-white z-10 select-none"
									>
										{{ stage.completionPercentage }}%
									</span>
								</div>
							</div>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 图例 -->
		<div
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
								:type="getStatusTagType(selectedStage.status)"
								size="small"
								effect="light"
								class="gsp-status-tag"
							>
								{{ getStatusLabel(selectedStage.status) }}
							</el-tag>
						</div>
					</div>

					<!-- 警告提示（超期 / 阻塞时显示） -->
					<div
						v-if="
							selectedStage.status === 'Overdue' || selectedStage.status === 'Blocked'
						"
						class="gsp-alert"
					>
						<el-icon class="gsp-alert__icon"><InfoFilled /></el-icon>
						<span v-if="selectedStage.status === 'Overdue'">
							This stage is taking longer than planned.
						</span>
						<span v-else>
							{{ selectedStage.blockReason || 'This stage is currently blocked.' }}
						</span>
					</div>

					<!-- Assignee -->
					<div v-if="selectedStage.assignee?.length" class="gsp-assignee">
						<div class="gsp-assignee__avatar">
							{{ (selectedStage.assignee[0] || '?').charAt(0) }}
						</div>
						<div class="gsp-assignee__info">
							<span class="gsp-assignee__name">{{ selectedStage.assignee[0] }}</span>
							<span v-if="selectedStage.assigneeEmail" class="gsp-assignee__email">
								{{ selectedStage.assigneeEmail }}
							</span>
						</div>
						<span v-if="selectedStage.coAssignees?.length" class="gsp-assignee__co">
							· {{ selectedStage.coAssignees.join(', ') }}
						</span>
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
									content="Original plan set when stage started"
									placement="top"
								>
									<el-icon class="gsp-info-icon"><InfoFilled /></el-icon>
								</el-tooltip>
							</div>
							<div class="gsp-time-rows">
								<div class="gsp-time-row">
									<span>Start</span>
									<span>{{ formatDate(selectedStage.plannedStartDate) }}</span>
								</div>
								<div class="gsp-time-row">
									<span>ETA</span>
									<span>{{ formatDate(selectedStage.plannedEndDate) }}</span>
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
									content="Current forecast based on actual progress"
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
												? formatDate(selectedStage.projectedStartDate)
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
												? formatDate(selectedStage.projectedEndDate)
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
										<span>{{ formatDate(selectedStage.actualStartDate) }}</span>
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
													? formatDate(selectedStage.actualEndDate)
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
						<button class="gsp-action" @click.stop="handleGoToStage(selectedStage)">
							Go to Stage
							<el-icon><ArrowRight /></el-icon>
						</button>
					</div>
				</div>
			</template>
		</el-popover>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch, nextTick } from 'vue';
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
import isSameOrBefore from 'dayjs/plugin/isSameOrBefore';
import isSameOrAfter from 'dayjs/plugin/isSameOrAfter';
import { timeZoneConvert } from '@/hooks/time';
import { projectDate } from '@/settings/projectSetting';
import type { GanttStageItem, GanttCaseSummary, GanttStageStatus } from '@/apis/ow/gantt';

dayjs.extend(isSameOrBefore);
dayjs.extend(isSameOrAfter);

// ========================= Props =========================

interface Props {
	stages: GanttStageItem[];
	summary: GanttCaseSummary | null;
	loading?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
	loading: false,
});

// ========================= 状态 =========================

type ViewMode = 'day' | 'week' | 'month';

const containerRef = ref<HTMLElement | null>(null);
const rightPanelRef = ref<HTMLElement | null>(null);
const viewMode = ref<ViewMode>('week');

/** Popover 状态 */
const popoverVisible = ref(false);
const selectedStage = ref<GanttStageItem | null>(null);
const popoverTriggerRef = ref<HTMLElement | null>(null);

/** 时间轴起始/结束（viewportStart/viewportEnd 控制显示范围） */
const viewportStart = ref<dayjs.Dayjs>(dayjs());
const viewportEnd = ref<dayjs.Dayjs>(dayjs());

/** 筛选状态 */
const selectedStatuses = ref<GanttStageStatus[]>([]);
const selectedAssignees = ref<string[]>([]);
const assigneeSearchText = ref('');
const statusFilterVisible = ref(false);
const assigneeFilterVisible = ref(false);

// ========================= 常量 =========================

const rowHeight = 48;
const headerHeight = 40;
const leftPanelWidth = 320;

/** 每种视图模式下，每列的宽度（px）和对应时间单位 */
const colConfig: Record<ViewMode, { colWidth: number; unit: 'day' | 'week' | 'month' }> = {
	day: { colWidth: 40, unit: 'day' },
	week: { colWidth: 80, unit: 'week' },
	month: { colWidth: 100, unit: 'month' },
};

// ========================= 计算属性 =========================

/** 从 stages 中提取时间范围 */
const dataTimeRange = computed(() => {
	if (!props.stages.length) {
		return { start: dayjs(), end: dayjs().add(30, 'day') };
	}
	const allDates = props.stages
		.flatMap((s) => [
			s.plannedStartDate,
			s.plannedEndDate,
			s.projectedStartDate,
			s.projectedEndDate,
			s.actualStartDate,
			s.actualEndDate,
		])
		.filter(Boolean) as string[];

	const timestamps = allDates.map((d) => dayjs(d).valueOf());
	return {
		start: dayjs(Math.min(...timestamps)).subtract(2, 'day'),
		end: dayjs(Math.max(...timestamps)).add(5, 'day'),
	};
});

/** 时间轴列数据 */
const timelineColumns = computed(() => {
	const { colWidth, unit } = colConfig[viewMode.value];
	const cols: Array<{
		key: string;
		label: string;
		x: number;
		width: number;
		isToday: boolean;
		date: dayjs.Dayjs;
	}> = [];

	let current = viewportStart.value.startOf(unit);
	let x = 0;

	while (current.isSameOrBefore(viewportEnd.value, unit)) {
		const isToday = current.isSame(dayjs(), unit);
		let label = '';
		if (unit === 'day') {
			label = current.format('MM/DD');
		} else if (unit === 'week') {
			label = current.format('MM/DD');
		} else {
			label = current.format('MMM YY');
		}

		cols.push({
			key: current.format('YYYY-MM-DD'),
			label,
			x,
			width: colWidth,
			isToday,
			date: current,
		});
		x += colWidth;
		current = current.add(1, unit);
	}
	return cols;
});

/** 时间轴总宽度 */
const totalTimelineWidth = computed(() => {
	if (!timelineColumns.value.length) return 800;
	const last = timelineColumns.value[timelineColumns.value.length - 1];
	return last.x + last.width;
});

/** Today 的 x 坐标 */
const todayX = computed(() => {
	return getXForDate(dayjs());
});

/** 图例数据 */
const legendItems = [
	{ label: 'Not Started', color: '#d9d9d9' },
	{ label: 'In Progress', color: '#5b8cff' },
	{ label: 'Completed', color: '#52c41a' },
	{ label: 'Overdue', color: '#ff4d4f' },
	{ label: 'Delayed', color: '#fa8c16' },
	{ label: 'Blocked', color: '#8c8c8c' },
];

// ========================= 筛选计算属性 =========================

/** 所有 Assignee 去重列表 */
const allAssignees = computed(() => {
	const set = new Set<string>();
	props.stages.forEach((s) => {
		s.assignee?.forEach((a) => set.add(a));
		s.coAssignees?.forEach((a) => set.add(a));
	});
	return Array.from(set).sort();
});

/** 按搜索文字过滤后的 Assignee 列表 */
const filteredAssigneeList = computed(() => {
	const q = assigneeSearchText.value.toLowerCase().trim();
	if (!q) return allAssignees.value;
	return allAssignees.value.filter((a) => a.toLowerCase().includes(q));
});

/** 过滤后的 Stage 列表（同时应用 status + assignee 筛选） */
const filteredStages = computed(() => {
	return props.stages.filter((s) => {
		// Status 筛选
		if (selectedStatuses.value.length > 0 && !selectedStatuses.value.includes(s.status)) {
			return false;
		}
		// Assignee 筛选
		if (selectedAssignees.value.length > 0) {
			const stageAssignees = [...(s.assignee ?? []), ...(s.coAssignees ?? [])];
			if (!selectedAssignees.value.some((a) => stageAssignees.includes(a))) return false;
		}
		return true;
	});
});

/** 当前视图时间范围文字 */
const rangeText = computed(() => {
	return `${formatDate(viewportStart.value.toISOString())} – ${formatDate(
		viewportEnd.value.toISOString()
	)}`;
});

/** 计划偏差天数（projected end vs planned end） */
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

// ========================= 方法 =========================

/** 初始化 viewport 范围 */
function initViewport() {
	const range = dataTimeRange.value;
	viewportStart.value = range.start;
	viewportEnd.value = range.end;
}

/** 将日期转为时间轴 x 坐标 */
function getXForDate(date: dayjs.Dayjs | string | null): number | null {
	if (!date) return null;
	const d = dayjs(date);
	const { colWidth, unit } = colConfig[viewMode.value];

	const startOf = viewportStart.value.startOf(unit);
	const diff = d.diff(startOf, unit, true); // 浮点数
	return diff * colWidth;
}

/** 生成甘特条样式 */
function getBarStyle(stage: GanttStageItem, type: 'planned' | 'projected') {
	const barHeight = 28;
	const topOffset = (rowHeight - barHeight) / 2;

	let startDate: string | null = null;
	let endDate: string | null = null;

	if (type === 'planned') {
		startDate = stage.plannedStartDate;
		endDate = stage.plannedEndDate;
	} else {
		// projected：优先显示实际，否则显示预测
		startDate = stage.actualStartDate || stage.projectedStartDate || stage.plannedStartDate;
		endDate = stage.actualEndDate || stage.projectedEndDate || stage.plannedEndDate;
	}

	if (!startDate || !endDate) return null;

	const x1 = getXForDate(startDate);
	const x2 = getXForDate(endDate);
	if (x1 === null || x2 === null) return null;

	const width = Math.max(x2 - x1, 8);

	if (type === 'planned') {
		return {
			left: x1 + 'px',
			top: topOffset + 'px',
			width: width + 'px',
			height: barHeight + 'px',
			background: 'var(--el-fill-color)',
			border: '1px dashed var(--el-border-color)',
			zIndex: 1,
		};
	}

	const bgColor = getStatusBarColor(stage.status);
	return {
		left: x1 + 'px',
		top: topOffset + 'px',
		width: width + 'px',
		height: barHeight + 'px',
		background: bgColor,
		opacity: stage.isBlocked ? '0.6' : '1',
		zIndex: 2,
		overflow: 'hidden',
	};
}

/** 根据状态获取条形图背景色 */
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

/** 进度条前景色 */
function getProgressColor(status: GanttStageStatus): string {
	const colors: Record<GanttStageStatus, string> = {
		NotStarted: '#bfbfbf',
		InProgress: '#2d6ef7',
		Completed: '#389e0d',
		Overdue: '#cf1322',
		Delayed: '#d46b08',
		Blocked: '#595959',
	};
	return colors[status] ?? '#2d6ef7';
}

/** 状态 Tag 类型 */
function getStatusTagType(
	status: GanttStageStatus
): '' | 'success' | 'warning' | 'danger' | 'info' {
	const map: Record<GanttStageStatus, '' | 'success' | 'warning' | 'danger' | 'info'> = {
		NotStarted: 'info',
		InProgress: '',
		Completed: 'success',
		Overdue: 'danger',
		Delayed: 'warning',
		Blocked: 'info',
	};
	return map[status] ?? 'info';
}

/** 状态展示文字 */
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

/** 格式化日期（使用 timeZoneConvert） */
function formatDate(dateString: string | null | undefined): string {
	if (!dateString) return '—';
	return timeZoneConvert(dateString, false, projectDate);
}

/** 时间轴平移 */
function shiftTimeline(direction: number) {
	const { unit } = colConfig[viewMode.value];
	const step = unit === 'day' ? 7 : unit === 'week' ? 4 : 3;
	viewportStart.value = viewportStart.value.add(direction * step, unit);
	viewportEnd.value = viewportEnd.value.add(direction * step, unit);
}

/** 跳到今天 */
function goToToday() {
	initViewport();
	nextTick(() => {
		scrollToToday();
	});
}

/** 滚动让 Today 居中 */
function scrollToToday() {
	if (!rightPanelRef.value || todayX.value === null) return;
	const panelWidth = rightPanelRef.value.clientWidth;
	rightPanelRef.value.scrollLeft = todayX.value - panelWidth / 2;
}

/** 适配所有内容 */
function fitToContent() {
	initViewport();
}

function handleScroll() {
	// 可扩展：虚拟滚动逻辑
}

function renderGantt() {
	// viewMode 变更后重新初始化 viewport
	initViewport();
}

/** 切换 Status 筛选 */
function toggleStatus(status: GanttStageStatus) {
	const idx = selectedStatuses.value.indexOf(status);
	if (idx > -1) {
		selectedStatuses.value.splice(idx, 1);
	} else {
		selectedStatuses.value.push(status);
	}
}

/** 切换 Assignee 筛选 */
function toggleAssignee(name: string) {
	const idx = selectedAssignees.value.indexOf(name);
	if (idx > -1) {
		selectedAssignees.value.splice(idx, 1);
	} else {
		selectedAssignees.value.push(name);
	}
}

/** 显示 Stage Popover (hover) — 支持快速切换，直接更新不关闭 */
let hideTimer: ReturnType<typeof setTimeout> | null = null;

function showStagePopover(stage: GanttStageItem, event: MouseEvent) {
	// 取消任何待执行的隐藏计时器
	if (hideTimer) {
		clearTimeout(hideTimer);
		hideTimer = null;
	}
	// 无论 popover 是否已显示，直接更新 stage 和触发元素
	// 这样快速切换时不会有闪烁或需要重新触发的问题
	selectedStage.value = stage;
	popoverTriggerRef.value = event.currentTarget as HTMLElement;
	popoverVisible.value = true;
}

function hideStagePopover() {
	// 给一个短暂延迟，让鼠标有时间移入 Popover
	hideTimer = setTimeout(() => {
		popoverVisible.value = false;
		hideTimer = null;
	}, 200);
}

function handlePopoverMouseEnter() {
	// 鼠标进入 Popover，取消隐藏
	if (hideTimer) {
		clearTimeout(hideTimer);
		hideTimer = null;
	}
}

function handlePopoverMouseLeave() {
	// 鼠标离开 Popover，延迟隐藏
	hideTimer = setTimeout(() => {
		popoverVisible.value = false;
		hideTimer = null;
	}, 100);
}

/** Go to Stage 按钮 */
function handleGoToStage(stage: GanttStageItem) {
	// TODO: 后端就绪后根据 onboardingId + stageId 跳转
	popoverVisible.value = false;
}

/** 格式化日期时间（带时间） */
function formatDateTime(dateString: string | null | undefined): string {
	if (!dateString) return '—';
	return timeZoneConvert(dateString, false, 'MM/DD/YYYY HH:mm:ss');
}

/** Stage 偏差天数（projected end vs planned end） */
const stageVarianceDays = computed(() => {
	const s = selectedStage.value;
	if (!s?.projectedEndDate || !s?.plannedEndDate) return 0;
	return dayjs(s.projectedEndDate).diff(dayjs(s.plannedEndDate), 'day');
});

// ========================= 生命周期 =========================

onMounted(() => {
	initViewport();
	nextTick(() => {
		scrollToToday();
	});
});

watch(
	() => props.stages,
	() => {
		initViewport();
		nextTick(() => {
			scrollToToday();
		});
	},
	{ deep: true }
);
</script>

<style scoped lang="scss">
.gantt-chart {
	user-select: none;
}

.gantt-left-panel {
	border-right: 1px solid var(--el-border-color-lighter);
}

.gantt-bar-row {
	position: relative;
}

.gantt-bar-planned {
	pointer-events: none;
}

.gantt-bar-projected {
	position: absolute;
}

.gantt-bar-progress {
	pointer-events: none;
}

/* 右侧滚动条美化 */
.gantt-right-panel {
	&::-webkit-scrollbar {
		height: 6px;
	}
	&::-webkit-scrollbar-track {
		background: transparent;
	}
	&::-webkit-scrollbar-thumb {
		background: rgba(0, 0, 0, 0.15);
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
		color: var(--el-color-primary);
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

	&__co {
		font-size: 12px;
		font-weight: 400;
		color: var(--el-text-color-secondary);
		margin-left: 4px;
	}
}

/* Section 通用 — 无背景，用上边框分隔 */
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

/* 两列网格：START / ETA 并排 */
.gsp-time-rows {
	display: grid;
	grid-template-columns: 1fr 1fr;
	gap: 10px 8px;
}

/* 单个时间字段：label 上，value 下 */
.gsp-time-row {
	display: flex;
	flex-direction: column;
	gap: 2px;

	/* label */
	span:first-child {
		font-size: 10px;
		font-weight: 600;
		color: var(--el-text-color-secondary);
		letter-spacing: 0.06em;
		text-transform: uppercase;
	}

	/* value */
	span:last-child {
		font-size: 15px;
		font-weight: 700;
		color: var(--el-text-color-primary);
		line-height: 1.2;
	}

	/* 跨两列（Duration、Days Elapsed 等单值行） */
	&--full {
		grid-column: 1 / -1;
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
