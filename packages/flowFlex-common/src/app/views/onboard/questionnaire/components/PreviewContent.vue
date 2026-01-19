<template>
	<div ref="printableRef" class="">
		<!-- 加载状态 -->
		<div v-if="loading" class="text-center py-12">
			<el-icon class="is-loading text-4xl text-primary-500 mb-4">
				<Loading />
			</el-icon>
			<p class="text-secondary">Loading preview...</p>
		</div>
		<div v-else-if="questionnaire" class="space-y-4">
			<!-- 问卷基本信息 -->
			<div class="questionnaire-header p-4 rounded-xl border">
				<div class="flex items-start justify-between mb-4">
					<div class="flex-1">
						<h2 class="text-2xl font-bold questionnaire-title mb-2">
							{{ questionnaire.name }}
						</h2>
						<p
							v-if="questionnaire.description"
							class="questionnaire-description text-base"
						>
							{{ questionnaire.description }}
						</p>

						<!-- Assignments区域 -->
						<div v-if="questionnaire.assignments" class="space-y-2 mt-3">
							<div class="flex items-center text-sm">
								<span class="preview_assignment-label">Assignments:</span>
							</div>
							<div class="flex items-start gap-2 flex-wrap assignments-container">
								<!-- 显示前5个组合的assignments -->
								<span
									class="preview_assignment-tag"
									v-for="assignment in getDisplayedAssignments(
										questionnaire.assignments
									)"
									:key="`${assignment.workflowId}-${assignment.stageId}`"
									:title="`${getWorkflowName(
										assignment.workflowId
									)} → ${getStageName(assignment.stageId)}`"
								>
									<text
										class="w-full overflow-hidden text-ellipsis whitespace-nowrap"
									>
										{{
											`${getWorkflowName(
												assignment.workflowId
											)} → ${getStageName(assignment.stageId)}`
										}}
									</text>
								</span>
								<!-- 显示剩余数量的按钮 -->
								<el-popover
									v-if="
										questionnaire.assignments &&
										getRemainingCount(questionnaire.assignments) > 0
									"
									placement="top"
									:width="400"
									trigger="click"
								>
									<template #reference>
										<span class="preview_assignment-tag-more">
											+{{ getRemainingCount(questionnaire.assignments) }}
										</span>
									</template>
									<div class="popover-content">
										<h4 class="preview_popover-title">More Assignments</h4>
										<div class="preview_popover-tags">
											<span
												class="preview_popover-tag"
												v-for="assignment in getRemainingAssignments(
													questionnaire.assignments
												)"
												:key="`${assignment.workflowId}-${assignment.stageId}`"
												:title="`${getWorkflowName(
													assignment.workflowId
												)} → ${getStageName(assignment.stageId)}`"
											>
												<span
													class="w-full overflow-hidden text-ellipsis whitespace-nowrap"
												>
													{{
														`${getWorkflowName(
															assignment.workflowId
														)} → ${getStageName(assignment.stageId)}`
													}}
												</span>
											</span>
										</div>
									</div>
								</el-popover>
							</div>
						</div>
					</div>

					<div class="ml-6 text-left text-sm space-y-1">
						<el-button
							class="print:hidden pdf-exclude w-full"
							type="primary"
							size="default"
							:loading="isExportingPdf"
							@click="printQuestionnaire"
						>
							Print
						</el-button>
						<div
							v-if="questionnaire.totalQuestions"
							class="flex items-center text-regular"
						>
							<el-icon class="mr-1">
								<Document />
							</el-icon>
							{{ questionnaire.totalQuestions }}
							{{ questionnaire.totalQuestions > 1 ? 'items' : 'item' }}
						</div>
						<div
							v-if="questionnaire.requiredQuestions"
							class="flex items-center text-red-500"
						>
							<el-icon class="mr-1">
								<Star />
							</el-icon>
							{{ questionnaire.requiredQuestions }} required
						</div>
					</div>
				</div>

				<!-- 问卷设置信息 -->
				<!-- <div class="flex flex-wrap gap-4 text-sm">
					<div
						v-if="questionnaire.allowMultipleSubmissions"
						class="flex items-center text-green-600"
					>
						<el-icon class="mr-1"><Refresh /></el-icon>
						Multiple submissions
					</div>
					<div v-if="questionnaire.category" class="flex items-center text-primary-600">
						<el-icon class="mr-1"><Collection /></el-icon>
						{{ questionnaire.category }}
					</div>
				</div> -->
			</div>

			<!-- 问卷章节 -->
			<div
				v-for="(section, sectionIndex) in questionnaire.sections"
				:key="section.id || sectionIndex"
				class="section-container border rounded-xl overflow-hidden"
			>
				<!-- 章节标题 -->
				<div class="section-header p-4 border-b" v-if="!section.isDefault">
					<div class="flex items-center justify-between gap-4">
						<div class="flex-1 min-w-0">
							<h3 class="text-lg font-medium section-title truncate">
								{{ section.name }}
							</h3>
							<p v-if="section.description" class="section-description mt-1 truncate">
								{{ section.description }}
							</p>
						</div>
						<div class="text-sm text-secondary flex-shrink-0">
							{{ section.questions?.length || 0 }}
							{{ section.questions?.length > 1 ? 'items' : 'item' }}
						</div>
					</div>
				</div>

				<!-- 章节问题 -->
				<div class="p-4 space-y-6">
					<div
						v-for="(item, itemIndex) in section.questions"
						:key="item.id || itemIndex"
						:class="[
							'question-item space-y-3 pb-6 border-b border-light last:border-b-0 last:pb-0',
							{ 'question-skipped': isQuestionSkipped(sectionIndex, itemIndex) },
						]"
					>
						<!-- 问题标题 -->
						<div
							class="flex items-start justify-between"
							v-if="item.type !== 'page_break'"
						>
							<h4
								class="text-base font-medium question-title flex-1 flex items-baseline gap-1 min-w-0"
							>
								<span class="text-placeholder mr-2">
									{{ getQuestionNumber(sectionIndex, itemIndex) }}.
								</span>
								<a :href="`#${item.id}`" class="truncate">
									{{ item.question || item.title }}
								</a>
								<span v-if="item.required" class="text-red-500 ml-1">*</span>
							</h4>
							<!-- <el-tag v-if="item.type" size="small" type="info" class="ml-2">
							{{ formatQuestionType(item.type) }}
						</el-tag> -->
						</div>

						<!-- 问题描述 -->
						<p
							v-if="item.description"
							class="text-sm question-description pl-6 truncate"
						>
							{{ item.description }}
						</p>

						<div
							v-if="item.questionProps && item.questionProps.fileUrl"
							class="flex flex-col max-h-[500px] justify-center items-center"
						>
							<el-image
								v-if="item.questionProps.type === 'image'"
								:src="item.questionProps.fileUrl"
								class="responsive-image"
								:preview-src-list="[`${item.questionProps.fileUrl}`]"
								fit="contain"
							/>
							<video
								v-else-if="item.questionProps.type === 'video'"
								:src="item.questionProps.fileUrl"
								:alt="item.questionProps.fileName || 'Uploaded video'"
								controls
								class="max-h-[500px] w-auto object-contain"
							></video>
						</div>

						<!-- 问题输入组件 -->
						<div class="pl-6">
							<!-- 错误提示 -->
							<div
								v-if="getFieldError(sectionIndex, itemIndex)"
								class="text-red-500 text-sm mb-2 flex items-center"
							>
								<el-icon class="mr-1">
									<Warning />
								</el-icon>
								{{ getFieldError(sectionIndex, itemIndex) }}
							</div>
							<!-- 短文本输入 -->
							<el-input
								v-if="item.type === 'short_answer'"
								v-model="previewData[getItemKey(sectionIndex, itemIndex)]"
								:placeholder="item.placeholder || 'Your answer'"
								:disabled="isQuestionSkipped(sectionIndex, itemIndex)"
								:class="[
									'preview-input',
									{ 'error-input': getFieldError(sectionIndex, itemIndex) },
								]"
							/>

							<!-- 长文本输入 -->
							<el-input
								v-else-if="item.type === 'paragraph'"
								v-model="previewData[getItemKey(sectionIndex, itemIndex)]"
								type="textarea"
								:rows="typeof item.rows === 'number' ? item.rows : 3"
								:placeholder="item.placeholder || 'Your answer'"
								:disabled="isQuestionSkipped(sectionIndex, itemIndex)"
								:class="[
									'preview-input',
									{ 'error-input': getFieldError(sectionIndex, itemIndex) },
								]"
							/>

							<!-- 单选题 -->
							<el-radio-group
								v-else-if="item.type === 'multiple_choice' && item.options"
								v-model="previewData[getItemKey(sectionIndex, itemIndex)]"
								@change="handleRadioChange(sectionIndex, itemIndex, item, $event)"
								:disabled="isQuestionSkipped(sectionIndex, itemIndex)"
								class="w-full"
							>
								<div class="space-y-2">
									<el-radio
										v-for="(option, optionIndex) in item.options"
										:key="option.id || optionIndex"
										:value="option.value || option.label"
										class="preview-radio w-full"
									>
										<div v-if="option.isOther">
											<el-input
												v-model="
													previewData[
														getItemKey(sectionIndex, itemIndex, true)
													]
												"
												:placeholder="item.placeholder || 'Your answer'"
											/>
										</div>
										<div v-else>
											{{ option.label || option.text || option.value }}
										</div>
									</el-radio>
								</div>
							</el-radio-group>

							<!-- 多选题 -->
							<el-checkbox-group
								v-else-if="item.type === 'checkboxes' && item.options"
								v-model="previewData[getItemKey(sectionIndex, itemIndex)]"
								@change="
									handleCheckboxChange(sectionIndex, itemIndex, item, $event)
								"
								:disabled="isQuestionSkipped(sectionIndex, itemIndex)"
								class="w-full flex"
							>
								<div class="space-y-2">
									<el-checkbox
										v-for="(option, optionIndex) in item.options"
										:key="option.id || optionIndex"
										:value="option.value || option.label"
										class="preview-checkbox w-full"
									>
										<div v-if="option.isOther">
											<el-input
												v-model="
													previewData[
														getItemKey(sectionIndex, itemIndex, true)
													]
												"
												:placeholder="item.placeholder || 'Your answer'"
											/>
										</div>
										<div v-else class="min-w-0 flex">
											<span class="truncate min-w-0">
												{{ option.label || option.text || option.value }}
											</span>
										</div>
									</el-checkbox>
								</div>
							</el-checkbox-group>

							<!-- 下拉选择 -->
							<el-select
								v-else-if="item.type === 'dropdown'"
								v-model="previewData[getItemKey(sectionIndex, itemIndex)]"
								:placeholder="item.placeholder || 'Please select'"
								:disabled="isQuestionSkipped(sectionIndex, itemIndex)"
								:class="[
									'w-full preview-select',
									{ 'error-select': getFieldError(sectionIndex, itemIndex) },
								]"
							>
								<el-option
									v-for="(option, optionIndex) in item.options"
									:key="option.id || optionIndex"
									:label="option.label || option.text || option.value"
									:value="option.value || option.label"
								/>
							</el-select>

							<!-- 数字输入 -->
							<el-input-number
								v-else-if="item.type === 'number'"
								v-model="previewData[getItemKey(sectionIndex, itemIndex)]"
								:placeholder="item.placeholder || 'Enter number'"
								:min="item.min"
								:max="item.max"
								:step="item.step || 1"
								class="preview-number"
							/>

							<!-- 日期选择 -->
							<el-date-picker
								v-else-if="item.type === 'date'"
								v-model="previewData[getItemKey(sectionIndex, itemIndex)]"
								type="date"
								:format="projectDate"
								:placeholder="item.placeholder || 'Select date'"
								class="preview-date w-full"
							/>

							<!-- 时间选择 -->
							<el-time-picker
								v-else-if="item.type === 'time'"
								v-model="previewData[getItemKey(sectionIndex, itemIndex)]"
								:placeholder="item.placeholder || 'Select time'"
								class="preview-time w-full"
							/>

							<!-- 评分 -->
							<div
								v-else-if="item.type === 'rating'"
								class="flex items-center space-x-2"
							>
								<el-rate
									v-model="previewData[getItemKey(sectionIndex, itemIndex)]"
									:max="item.max || 5"
									:icons="getSelectedFilledIcon(item.iconType)"
									:void-icon="getSelectedVoidIcon(item.iconType)"
									class="preview-rating"
								/>
								<span v-if="item.showText" class="text-sm text-secondary">
									({{ item.max || 5 }} stars)
								</span>
							</div>

							<!-- 文件上传 -->
							<div
								v-else-if="item.type === 'file' || item.type === 'file_upload'"
								:class="[
									'preview-file',
									{ 'error-upload': getFieldError(sectionIndex, itemIndex) },
								]"
							>
								<el-upload
									drag
									:auto-upload="false"
									:show-file-list="true"
									:on-change="
										(file, fileList) =>
											handleFileChange(
												sectionIndex,
												itemIndex,
												file,
												fileList
											)
									"
									:accept="item.accept"
									class="upload-demo w-full"
								>
									<el-icon class="el-icon--upload text-4xl">
										<Upload />
									</el-icon>
									<div>
										<text class="text-primary dark:text-white">
											Drop file here
										</text>
										<text>or</text>
										<em class="text-primary">click to select</em>
									</div>
									<div v-if="item.accept" class="el-upload__tip text-xs">
										Accepted formats: {{ item.accept }}
									</div>
								</el-upload>
							</div>

							<!-- 线性量表 -->
							<div v-else-if="item.type === 'linear_scale'" class="space-y-2">
								<el-slider
									v-model="previewData[getItemKey(sectionIndex, itemIndex)]"
									:min="item.min"
									:max="item.max"
									:marks="getSliderMarks(item)"
									:validate-event="false"
									show-stops
									:key="`slider-${getItemKey(sectionIndex, itemIndex)}-${
										previewData[getItemKey(sectionIndex, itemIndex)] || 0
									}`"
									class="preview-linear-scale"
								/>
								<div class="flex justify-between text-xs text-secondary min-w-0">
									<span class="truncate">{{ item.minLabel || item.min }}</span>
									<span class="truncate">{{ item.maxLabel || item.max }}</span>
								</div>
							</div>

							<!-- 多选网格 -->
							<div
								v-else-if="item.type === 'multiple_choice_grid'"
								class="preview-grid"
							>
								<!-- 如果有网格数据（rows + columns），渲染为多选网格 -->
								<el-table v-if="item.columns && item.rows" :data="item.rows" border>
									<el-table-column prop="label" label="" fixed="left" width="300">
										<template #default="{ row }">
											<span class="truncate" :title="row.label">
												{{ row.label }}
											</span>
										</template>
									</el-table-column>
									<el-table-column
										v-for="(column, colIndex) in item.columns"
										:key="colIndex"
										:label="column.label"
										min-width="120"
										align="center"
									>
										<template #header>
											<div class="flex items-center justify-center gap-1">
												{{ column.label }}
												<el-tag
													v-if="column.isOther"
													size="small"
													type="warning"
												>
													Other
												</el-tag>
											</div>
										</template>
										<template #default="{ $index: rowIndex }">
											<div class="flex items-center justify-center gap-2">
												<el-checkbox-group
													v-model="
														previewData[
															getGridKey(
																sectionIndex,
																itemIndex,
																rowIndex
															)
														]
													"
													@change="
														handleGridCheckboxChange(
															sectionIndex,
															itemIndex,
															rowIndex,
															item,
															$event
														)
													"
												>
													<el-checkbox
														:value="
															column.value ||
															column.label ||
															`col_${colIndex}`
														"
														class="grid-checkbox"
													/>
												</el-checkbox-group>
												<el-input
													v-if="column.isOther"
													v-model="
														previewData[
															getOtherTextKey(
																sectionIndex,
																itemIndex,
																rowIndex
															)
														]
													"
													:disabled="
														!previewData[
															getGridKey(
																sectionIndex,
																itemIndex,
																rowIndex
															)
														]?.includes(column.value || column.label)
													"
													placeholder="Enter other"
													size="small"
													class="other-input"
												/>
											</div>
										</template>
									</el-table-column>
								</el-table>

								<!-- 如果没有任何数据，显示占位符 -->
								<div
									v-else
									class="text-placeholder italic p-4 border border-dashed border-base rounded"
								>
									<el-icon class="mr-2">
										<Warning />
									</el-icon>
									Multiple choice grid: No options, columns or rows data available
								</div>
							</div>

							<!-- 单选网格 -->
							<div v-else-if="item.type === 'checkbox_grid'" class="preview-grid">
								<el-table
									v-if="
										item.rows &&
										item.rows.length > 0 &&
										item.columns &&
										item.columns.length > 0
									"
									:data="item.rows"
									border
								>
									<el-table-column prop="label" label="" fixed="left" width="300">
										<template #default="{ row }">
											<span class="truncate" :title="row.label">
												{{ row.label }}
											</span>
										</template>
									</el-table-column>
									<el-table-column
										v-for="(column, colIndex) in item.columns"
										:key="colIndex"
										:label="column.label"
										min-width="120"
										align="center"
									>
										<template #header>
											<div class="flex items-center justify-center gap-1">
												{{ column.label }}
												<el-tag
													v-if="column.isOther"
													size="small"
													type="warning"
												>
													Other
												</el-tag>
											</div>
										</template>
										<template #default="{ $index: rowIndex }">
											<div class="flex items-center justify-center gap-2">
												<el-radio
													v-model="
														previewData[
															getGridKey(
																sectionIndex,
																itemIndex,
																rowIndex
															)
														]
													"
													:name="`grid_${sectionIndex}_${itemIndex}_${rowIndex}`"
													:value="
														column.value ||
														column.label ||
														`${rowIndex}_${colIndex}`
													"
													@change="
														handleGridRadioChange(
															sectionIndex,
															itemIndex,
															rowIndex,
															item,
															$event
														)
													"
													class="grid-radio"
												/>
												<el-input
													v-if="column.isOther"
													v-model="
														previewData[
															getOtherTextKey(
																sectionIndex,
																itemIndex,
																rowIndex
															)
														]
													"
													:disabled="
														previewData[
															getGridKey(
																sectionIndex,
																itemIndex,
																rowIndex
															)
														] != (column.value || column.label)
													"
													placeholder="Enter other"
													size="small"
													class="other-input"
												/>
											</div>
										</template>
									</el-table-column>
								</el-table>

								<!-- 如果没有数据，显示占位符 -->
								<div
									v-else
									class="text-placeholder italic p-4 border border-dashed border-base rounded"
								>
									<el-icon class="mr-2">
										<Warning />
									</el-icon>
									Checkbox grid: No rows or columns data available
									<div class="text-xs mt-1">
										Rows: {{ item.rows?.length || 0 }}, Columns:
										{{ item.columns?.length || 0 }}
									</div>
								</div>
							</div>

							<div v-else-if="item.type === 'short_answer_grid'" class="preview-grid">
								<el-table v-if="item.columns && item.rows" :data="item.rows" border>
									<el-table-column prop="label" label="" fixed="left" width="300">
										<template #default="{ row }">
											<span class="truncate" :title="row.label">
												{{ row.label }}
											</span>
										</template>
									</el-table-column>
									<el-table-column
										v-for="(column, colIndex) in item.columns"
										:key="colIndex"
										:label="column.label"
										min-width="150"
										align="center"
									>
										<template #header>
											<div class="flex items-center justify-center gap-1">
												{{ column.label }}
												<el-tag
													v-if="column.isOther"
													size="small"
													type="warning"
												>
													Other
												</el-tag>
											</div>
										</template>
										<template #default="{ $index: rowIndex }">
											<el-input
												v-model="
													previewData[
														getGridKey(
															sectionIndex,
															column?.id ||
																column?.temporaryId ||
																column?.label,
															rowIndex
														)
													]
												"
											/>
										</template>
									</el-table-column>
								</el-table>
							</div>

							<!-- 说明文本 -->
							<div
								v-else-if="item.type === 'description'"
								class="text-regular italic"
							>
								{{ item.content || item.text }}
							</div>

							<div v-else-if="item.type === 'page_break'" class="text-regular italic">
								<div class="border-t-2 border-dashed border-primary-300 pt-4 mt-4">
									<div class="text-center text-primary-500 text-sm">
										— Page Break —
									</div>
								</div>
							</div>

							<div
								v-else-if="item.type === 'image'"
								class="flex justify-center items-center w-full"
							>
								<el-image
									:src="item.fileUrl"
									class="responsive-image"
									:preview-src-list="[`${item.fileUrl}`]"
									fit="contain"
								/>
							</div>

							<div
								v-else-if="item.type === 'video'"
								class="flex justify-center items-center"
							>
								<video
									:src="item.fileUrl"
									controls
									class="max-h-[500px] w-auto object-contain"
								></video>
							</div>

							<!-- 未知类型 -->
							<div
								v-else
								class="text-placeholder italic p-4 border border-dashed border-base rounded"
							>
								<el-icon class="mr-2">
									<Warning />
								</el-icon>
								Unsupported question type: {{ item.type }}
							</div>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 空状态 -->
		<div v-else class="text-center py-12">
			<el-icon class="text-6xl text-placeholder mb-4">
				<Document />
			</el-icon>
			<p class="text-secondary text-lg">No data available</p>
			<p class="text-placeholder text-sm mt-2">
				The questionnaire may not exist or failed to load
			</p>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, watch, nextTick } from 'vue';
import { Document, Upload, Loading, Star, Warning } from '@element-plus/icons-vue';
import { ElMessage } from 'element-plus';
import { projectDate } from '@/settings/projectSetting';
import { Workflow } from '#/onboard';

import IconStar from '~icons/mdi/star';
import IconStarOutline from '~icons/mdi/star-outline';
import IconHeart from '~icons/mdi/heart';
import IconHeartOutline from '~icons/mdi/heart-outline';
import IconThumbUp from '~icons/mdi/thumb-up';
import IconThumbUpOutline from '~icons/mdi/thumb-up-outline';

// 定义组件属性
interface Props {
	questionnaire?: any;
	loading?: boolean;
	workflows: Workflow[];
	allStages?: any[];
}

const props = withDefaults(defineProps<Props>(), {
	questionnaire: null,
	loading: false,
	allStages: () => [],
});

const printableRef = ref<HTMLDivElement | null>(null);
const isExportingPdf = ref(false);

const buildAttributeString = (element: Element, excludes: string[] = []) =>
	Array.from(element.attributes)
		.filter((attr) => !excludes.includes(attr.name))
		.map((attr) => `${attr.name}="${attr.value}"`)
		.join(' ');

const appendClassAndAttributes = (className: string, attributes: string) => {
	const parts: string[] = [];
	if (className) {
		parts.push(`class="${className}"`);
	}
	if (attributes) {
		parts.push(attributes);
	}
	return parts.length ? ` ${parts.join(' ')}` : '';
};

// 打印窗口必须剥离 dark 主题相关 class，避免继承暗色样式
const enforceLightThemeClasses = (className?: string) =>
	className
		?.split(/\s+/)
		.filter((item) => item && item !== 'dark')
		.join(' ') ?? '';

const collectPrintableHead = () =>
	Array.from(
		document.head.querySelectorAll<HTMLStyleElement | HTMLLinkElement>(
			'style, link[rel="stylesheet"]'
		)
	)
		.map((node) => node.outerHTML)
		.join('');

const PRINT_DELAY = 300;

const printQuestionnaire = () => {
	if (isExportingPdf.value) return;
	const container = printableRef.value;
	if (!container) {
		ElMessage.warning('No printable content available');
		return;
	}

	try {
		isExportingPdf.value = true;
		const headContent = collectPrintableHead();
		const htmlAttributes = appendClassAndAttributes(
			enforceLightThemeClasses(document.documentElement.className),
			buildAttributeString(document.documentElement, ['class'])
		);
		const bodyClassList = [document.body.className, 'print-body'].filter(Boolean).join(' ');
		const bodyAttributes = appendClassAndAttributes(
			enforceLightThemeClasses(bodyClassList),
			buildAttributeString(document.body, ['class'])
		);

		// 克隆内容并处理表格宽度
		const clonedContent = container.cloneNode(true) as HTMLElement;

		// 处理所有 el-table 相关元素，移除内联宽度样式
		const tables = clonedContent.querySelectorAll('.el-table');
		tables.forEach((table) => {
			(table as HTMLElement).style.width = '100%';
			// 移除固定列容器
			const fixedElements = table.querySelectorAll(
				'.el-table__fixed, .el-table__fixed-right'
			);
			fixedElements.forEach((el) => el.remove());
		});

		// 处理表格内部元素
		const tableInners = clonedContent.querySelectorAll(
			'.el-table__header-wrapper, .el-table__body-wrapper'
		);
		tableInners.forEach((el) => {
			(el as HTMLElement).style.overflow = 'visible';
			(el as HTMLElement).style.width = '100%';
		});

		// 处理 table 元素
		const tableElements = clonedContent.querySelectorAll('.el-table__header, .el-table__body');
		tableElements.forEach((el) => {
			(el as HTMLElement).style.width = '100%';
			(el as HTMLElement).style.tableLayout = 'fixed';
		});

		// 处理 colgroup 中的 col 元素，移除固定宽度
		const colElements = clonedContent.querySelectorAll('.el-table colgroup col');
		colElements.forEach((col) => {
			(col as HTMLElement).style.width = 'auto';
			col.removeAttribute('width');
		});

		// 处理 th 和 td 元素
		const cells = clonedContent.querySelectorAll('.el-table th, .el-table td');
		cells.forEach((cell) => {
			(cell as HTMLElement).style.minWidth = '0';
			(cell as HTMLElement).style.width = 'auto';
		});

		const printStyles = `
			@page {
				size: A4;
				margin: 12mm 14mm 15mm;
				@top-center {
    				content: ""; /* 设置页眉内容 */
    				font-size: 12pt;
  				}
  				@bottom-center {
    				content:  counter(page); /* 设置页脚内容，包含页码 */
    				font-size: 10pt;
  				}
			}
			@media print {
				:root {
					color-scheme: light;
				}
				html,
				body {
					height: auto !important;
					overflow: visible !important;
					background: #ffffff !important;
				}
				body {
					padding: 0;
				}
				.print-wrapper {
					width: 100%;
				}
				.pdf-exclude {
					display: none !important;
				}
				.questionnaire-header {
					break-after: avoid;
					page-break-after: avoid;
				}
				.section-container,
				.question-item {
					break-inside: avoid;
					page-break-inside: avoid;
				}
				.max-h-[500px],
				.overflow-hidden,
				.overflow-y-auto,
				.overflow-y-scroll,
				.overflow-auto {
					max-height: none !important;
					overflow: visible !important;
				}
				* {
					-webkit-print-color-adjust: exact !important;
					print-color-adjust: exact !important;
				}
			}
		`;

		const printWindow = window.open('', '_blank');
		if (!printWindow) {
			ElMessage.warning(
				'The browser blocked the print window, please allow pop-ups and try again'
			);
			return;
		}

		printWindow.document.open();
		printWindow.document.write(
			`<!DOCTYPE html><html${htmlAttributes}><head><meta charset="utf-8" /><title></title>${headContent}<style>${printStyles}</style></head><body${bodyAttributes}><div class="print-wrapper">${clonedContent.innerHTML}</div></body></html>`
		);
		printWindow.document.close();
		printWindow.document.title = '';

		const handlePrint = () => {
			printWindow.focus();
			printWindow.print();
			printWindow.close();
		};

		if ('onload' in printWindow) {
			printWindow.onload = () => setTimeout(handlePrint, PRINT_DELAY);
		} else {
			setTimeout(handlePrint, PRINT_DELAY);
		}
	} catch (error) {
		console.error('Failed to render print preview:', error);
		ElMessage.error('Failed to render print preview, please try again later');
	} finally {
		isExportingPdf.value = false;
	}
};
// 预览数据存储 - 独立于原始问卷数据
const previewData = ref<Record<string, any>>({});

// 校验错误状态存储
const validationErrors = ref<Record<string, string>>({});

// 被跳过的问题集合（存储问题的唯一标识：sectionIndex_itemIndex）
const skippedQuestions = ref<Set<string>>(new Set());

// 清除跳过状态
const clearSkippedQuestions = () => {
	skippedQuestions.value.clear();
};

// 生成问题项的唯一键
const getItemKey = (
	sectionIndex: string | number,
	itemIndex: string | number,
	isOther?: boolean
) => {
	return `section_${sectionIndex}_item_${itemIndex}${isOther ? '_other' : ''}`;
};

// 生成网格问题的唯一键
const getGridKey = (
	sectionIndex: string | number,
	itemIndex: string | number,
	rowIndex: string | number
) => {
	return `section_${sectionIndex}_item_${itemIndex}_row_${rowIndex}`;
};

// 生成Other文本输入框的唯一键
const getOtherTextKey = (
	sectionIndex: string | number,
	itemIndex: string | number,
	rowIndex: string | number
) => {
	return `section_${sectionIndex}_item_${itemIndex}_row_${rowIndex}_other_text`;
};

// 初始化预览数据
const initializePreviewData = () => {
	if (!props.questionnaire?.sections) return;

	const newPreviewData: Record<string, any> = {};

	props.questionnaire.sections.forEach((section: any, sectionIndex: number) => {
		section.questions?.forEach((item: any, itemIndex: number) => {
			const key = getItemKey(sectionIndex, itemIndex);

			// 根据问题类型设置默认值
			switch (item.type) {
				case 'short_answer':
				case 'paragraph':
					newPreviewData[key] = '';
					break;
				case 'multiple_choice':
				case 'dropdown':
					newPreviewData[key] = '';
					break;
				case 'checkboxes':
					newPreviewData[key] = [];
					break;
				case 'number':
					newPreviewData[key] = null;
					break;
				case 'date':
				case 'time':
					newPreviewData[key] = null;
					break;
				case 'rating':
					newPreviewData[key] = 0;
					break;
				case 'slider':
					newPreviewData[key] = item?.min;
					break;
				case 'linear_scale':
					newPreviewData[key] = item?.min;
					break;
				case 'multiple_choice_grid':
					// 如果有网格数据（rows + columns），为每一行初始化多选值（数组）
					if (item.rows && item.columns) {
						item.rows.forEach((_: any, rowIndex: number) => {
							const gridKey = getGridKey(sectionIndex, itemIndex, rowIndex);
							newPreviewData[gridKey] = []; // 多选应该是数组

							// 为Other选项初始化文本字段
							const otherTextKey = getOtherTextKey(sectionIndex, itemIndex, rowIndex);
							newPreviewData[otherTextKey] = '';
						});
					}
					// 如果只有options数据，初始化为普通多选
					else if (item.options) {
						newPreviewData[getItemKey(sectionIndex, itemIndex)] = []; // 多选应该是数组
					}
					break;
				case 'checkbox_grid':
					// 为每一行初始化单选值，只有当rows数组有内容时
					if (item.rows && item.rows.length > 0) {
						item.rows.forEach((_: any, rowIndex: number) => {
							const gridKey = getGridKey(sectionIndex, itemIndex, rowIndex);
							newPreviewData[gridKey] = null; // 单选应该是字符串/null

							// 为Other选项初始化文本字段
							const otherTextKey = getOtherTextKey(sectionIndex, itemIndex, rowIndex);
							newPreviewData[otherTextKey] = '';
						});
					}
					break;
				case 'file_upload':
					newPreviewData[getItemKey(sectionIndex, itemIndex)] = [];
					break;
				default:
					newPreviewData[getItemKey(sectionIndex, itemIndex)] = null;
			}
		});
	});

	previewData.value = newPreviewData;
};

// 监听问卷数据变化，重新初始化预览数据
watch(
	() => props.questionnaire,
	() => {
		initializePreviewData();
		// 重置跳过状态
		clearSkippedQuestions();
	},
	{ immediate: true, deep: true }
);

// 处理文件选择（仅本地预览，不上传）
const handleFileChange = (
	sectionIndex: string | number,
	itemIndex: string | number,
	file: any,
	fileList: any[]
) => {
	console.log('handleFileChange called:', { sectionIndex, itemIndex, file, fileList });
	const key = getItemKey(sectionIndex, itemIndex);
	// 只存储文件信息用于预览，不进行实际上传
	previewData.value[key] = fileList.map((f) => ({
		name: f.name,
		size: f.size,
		type: f.raw?.type || '',
		lastModified: f.raw?.lastModified || Date.now(),
	}));
	console.log('File data stored:', previewData.value[key]);
};

// 处理单选变化 - 清空Other输入框并处理跳转逻辑
const handleRadioChange = (
	sectionIndex: string | number,
	itemIndex: string | number,
	item: any,
	value?: string | number | boolean
) => {
	const otherTextKey = getItemKey(sectionIndex, itemIndex, true);

	// 查找Other选项
	const otherOption = item.options?.find((option: any) => option.isOther);
	if (otherOption) {
		const otherValue = otherOption.value || otherOption.label;

		// 如果选择的不是Other选项，清空Other文本输入框
		if (value !== otherValue) {
			previewData.value[otherTextKey] = '';
		}
	}

	// 处理跳转逻辑（仅单选题支持跳转到具体问题）
	if (item.type === 'multiple_choice' && item.jumpRules && item.jumpRules.length > 0 && value) {
		// 查找匹配的跳转规则
		const selectedOption = item.options?.find(
			(opt: any) => opt.value === value || opt.label === value
		);

		if (selectedOption) {
			const jumpRule = item.jumpRules.find(
				(rule: any) => rule.optionId === selectedOption.temporaryId
			);

			if (jumpRule) {
				// 计算被跳过的问题
				calculateSkippedQuestions(sectionIndex, itemIndex, jumpRule);

				// 执行跳转（滚动到目标问题）
				scrollToTargetQuestion(jumpRule);
			} else {
				// 如果没有跳转规则，清除之前的跳过状态
				clearSkippedQuestions();
			}
		}
	} else {
		// 如果没有选择值或没有跳转规则，清除跳过状态
		clearSkippedQuestions();
	}
};

// 计算被跳过的问题
const calculateSkippedQuestions = (
	currentSectionIndex: string | number,
	currentItemIndex: string | number,
	jumpRule: any
) => {
	const skipped = new Set<string>();

	// 获取目标问题位置
	const targetSectionIndex = props.questionnaire?.sections.findIndex(
		(s: any) => s.temporaryId === jumpRule.targetSectionId
	);

	if (targetSectionIndex === -1) return;

	const targetSection = props.questionnaire?.sections[targetSectionIndex];
	if (!targetSection) return;

	// 如果跳转规则有targetQuestionId，使用它；否则跳转到section的第一个问题（兼容旧数据）
	let targetQuestionIndex = -1;
	if (jumpRule.targetQuestionId) {
		targetQuestionIndex = targetSection.questions.findIndex(
			(q: any) => q.temporaryId === jumpRule.targetQuestionId
		);
	} else {
		// 兼容旧数据：跳转到section的第一个问题
		targetQuestionIndex = 0;
	}

	if (targetQuestionIndex === -1) return;

	// 判断是同section内跳转还是跨section跳转
	if (currentSectionIndex === targetSectionIndex) {
		// 同section内跳转：跳过当前问题之后到目标问题之前的所有问题
		for (let i = Number(currentItemIndex) + 1; i < targetQuestionIndex; i++) {
			skipped.add(`${currentSectionIndex}_${i}`);
		}
	} else {
		// 跨section跳转：
		// 1. 跳过当前section中当前问题之后的所有问题
		const currentSection = props.questionnaire?.sections[currentSectionIndex as number];
		if (currentSection) {
			for (let i = Number(currentItemIndex) + 1; i < currentSection.questions.length; i++) {
				skipped.add(`${currentSectionIndex}_${i}`);
			}
		}

		// 2. 跳过目标section中目标问题之前的所有问题
		for (let i = 0; i < targetQuestionIndex; i++) {
			skipped.add(`${targetSectionIndex}_${i}`);
		}
	}

	skippedQuestions.value = skipped;
};

// 滚动到目标问题
const scrollToTargetQuestion = (jumpRule: any) => {
	nextTick(() => {
		const targetSectionIndex = props.questionnaire?.sections.findIndex(
			(s: any) => s.temporaryId === jumpRule.targetSectionId
		);

		if (targetSectionIndex === -1) return;

		const targetSection = props.questionnaire?.sections[targetSectionIndex];
		if (!targetSection) return;

		let targetQuestionIndex = -1;
		if (jumpRule.targetQuestionId) {
			targetQuestionIndex = targetSection.questions.findIndex(
				(q: any) => q.temporaryId === jumpRule.targetQuestionId
			);
		} else {
			targetQuestionIndex = 0;
		}

		if (targetQuestionIndex === -1) return;

		// 查找目标问题的DOM元素
		const questionId =
			targetSection.questions[targetQuestionIndex]?.id ||
			targetSection.questions[targetQuestionIndex]?.temporaryId;

		if (questionId) {
			const element = document.querySelector(`a[href="#${questionId}"]`);
			if (element) {
				element.scrollIntoView({ behavior: 'smooth', block: 'center' });
			}
		}
	});
};

// 检查问题是否被跳过
const isQuestionSkipped = (sectionIndex: string | number, itemIndex: string | number) => {
	return skippedQuestions.value.has(`${sectionIndex}_${itemIndex}`);
};

// 处理多选变化 - 清空Other输入框
const handleCheckboxChange = (
	sectionIndex: string | number,
	itemIndex: string | number,
	item: any,
	value: string[]
) => {
	const otherTextKey = getItemKey(sectionIndex, itemIndex, true);

	// 查找Other选项
	const otherOption = item.options?.find((option: any) => option.isOther);
	if (otherOption) {
		const otherValue = otherOption.value || otherOption.label;

		// 如果Other选项被取消选择，清空Other文本输入框
		if (Array.isArray(value) && !value.includes(otherValue)) {
			previewData.value[otherTextKey] = '';
		}
	}
};

// 处理网格多选选项变化
const handleGridCheckboxChange = (
	sectionIndex: string | number,
	itemIndex: string | number,
	rowIndex: string | number,
	item: any,
	value: string[]
) => {
	const otherTextKey = getOtherTextKey(sectionIndex, itemIndex, rowIndex);

	// 查找Other列
	const otherColumn = item.columns?.find((col: any) => col.isOther);
	if (otherColumn) {
		const otherValue =
			otherColumn.value || otherColumn.label || `col_${item.columns.indexOf(otherColumn)}`;

		// 如果Other选项被取消选择，清空Other文本输入框
		if (Array.isArray(value) && !value.includes(otherValue)) {
			previewData.value[otherTextKey] = '';
		}
	}
};

// 处理单选网格选项变化
const handleGridRadioChange = (
	sectionIndex: string | number,
	itemIndex: string | number,
	rowIndex: string | number,
	item: any,
	value?: string | number | boolean
) => {
	const otherTextKey = getOtherTextKey(sectionIndex, itemIndex, rowIndex);

	// 查找Other列
	const otherColumn = item.columns?.find((col: any) => col.isOther);
	if (otherColumn) {
		const colIndex = item.columns.indexOf(otherColumn);
		const otherValue = otherColumn.value || otherColumn.label || `${rowIndex}_${colIndex}`;

		// 如果Other选项被取消选择，清空Other文本输入框
		if (value !== otherValue) {
			previewData.value[otherTextKey] = '';
		}
	}
};

// 校验表单数据
interface ValidationError {
	sectionIndex: number;
	itemIndex: number;
	sectionTitle: string;
	questionTitle: string;
	questionType: string;
	message: string;
}

interface ValidationResult {
	isValid: boolean;
	errors: ValidationError[];
	totalRequired: number;
	completedRequired: number;
}

const validateForm = (): ValidationResult => {
	const errors: ValidationError[] = [];
	let totalRequired = 0;
	let completedRequired = 0;

	// 清除之前的错误状态
	validationErrors.value = {};

	if (!props.questionnaire?.sections) {
		return {
			isValid: true,
			errors: [],
			totalRequired: 0,
			completedRequired: 0,
		};
	}

	props.questionnaire.sections.forEach((section: any, sectionIndex: number) => {
		section.questions?.forEach((item: any, itemIndex: number) => {
			// 跳过被跳过的问题（即使它们是必填的）
			if (isQuestionSkipped(sectionIndex, itemIndex)) return;

			// 只校验必填字段
			if (!item.required) return;

			totalRequired++;
			const key = getItemKey(sectionIndex, itemIndex);
			const value = previewData.value[key];
			let isFieldValid = false;
			let errorMessage = '';

			// 根据问题类型进行不同的校验
			switch (item.type) {
				case 'short_answer':
				case 'paragraph':
					isFieldValid = value && value.trim() !== '';
					errorMessage = 'This field is required';
					break;

				case 'multiple_choice':
					isFieldValid = value && value !== '';
					errorMessage = 'Please select an option';
					break;

				case 'checkboxes':
					isFieldValid = Array.isArray(value) && value.length > 0;
					errorMessage = 'Please select at least one option';
					break;

				case 'number':
					isFieldValid = value !== null && value !== undefined && value !== '';
					errorMessage = 'Please enter a number';
					break;

				case 'date':
				case 'time':
					isFieldValid = value !== null && value !== undefined;
					errorMessage = 'Please select a date/time';
					break;

				case 'rating':
					isFieldValid = value !== null && value !== undefined && value > 0;
					errorMessage = 'Please provide a rating';
					break;

				case 'slider':
				case 'linear_scale':
					isFieldValid = value !== null && value !== undefined;
					errorMessage = 'Please select a value';
					break;

				case 'file':
				case 'file_upload':
					isFieldValid = Array.isArray(value) && value.length > 0;
					errorMessage = 'Please upload a file';
					break;

				case 'multiple_choice_grid':
					// 网格多选：检查每一行是否都有选择
					if (item.rows && item.columns) {
						let allRowsCompleted = true;
						item.rows.forEach((_: any, rowIndex: number) => {
							const gridKey = getGridKey(sectionIndex, itemIndex, rowIndex);
							const gridValue = previewData.value[gridKey];
							if (!Array.isArray(gridValue) || gridValue.length === 0) {
								allRowsCompleted = false;
							}
						});
						isFieldValid = allRowsCompleted;
						errorMessage = 'Please complete all rows in the grid';
					} else if (item.options) {
						isFieldValid = Array.isArray(value) && value.length > 0;
						errorMessage = 'Please select at least one option';
					}
					break;

				case 'checkbox_grid':
					// 网格单选：检查每一行是否都有选择
					if (item.rows && item.rows.length > 0) {
						let allRowsCompleted = true;
						item.rows.forEach((_: any, rowIndex: number) => {
							const gridKey = getGridKey(sectionIndex, itemIndex, rowIndex);
							const gridValue = previewData.value[gridKey];
							if (!gridValue || gridValue === '') {
								allRowsCompleted = false;
							}
						});
						isFieldValid = allRowsCompleted;
						errorMessage = 'Please complete all rows in the grid';
					}
					break;

				case 'divider':
				case 'description':
					// 分隔符和说明文本不需要校验
					isFieldValid = true;
					totalRequired--; // 不计入必填字段总数
					break;

				default:
					isFieldValid = value !== null && value !== undefined && value !== '';
					errorMessage = 'This field is required';
			}

			if (isFieldValid) {
				completedRequired++;
			} else {
				// 存储错误信息到状态中
				validationErrors.value[key] = errorMessage;

				errors.push({
					sectionIndex,
					itemIndex,
					sectionTitle: section.title || `Section ${sectionIndex + 1}`,
					questionTitle: item.question || item.title || `Question ${itemIndex + 1}`,
					questionType: item.type,
					message: errorMessage,
				});
			}
		});
	});

	return {
		isValid: errors.length === 0,
		errors,
		totalRequired,
		completedRequired,
	};
};

// 清除校验错误
const clearValidationErrors = () => {
	validationErrors.value = {};
};

// 获取指定字段的错误信息
const getFieldError = (sectionIndex: string | number, itemIndex: string | number) => {
	const key = getItemKey(sectionIndex, itemIndex);
	return validationErrors.value[key] || '';
};

// Assignment 处理函数（参考 index.vue 的实现）
const getWorkflowName = (workflowId: string) => {
	if (!workflowId || workflowId === '0') return 'Unknown Workflow';
	const workflow = props.workflows.find((w) => w.id === workflowId);
	return workflow?.name || workflowId;
};

const getStageName = (stageId: string) => {
	if (!stageId || stageId === '0') return 'Unknown Stage';
	const stage = props.allStages?.find((s) => s.id === stageId);
	return stage ? stage.name : stageId;
};

// 获取显示的分配数量（去重）
const getDisplayedAssignments = (assignments: any[]) => {
	const displayedCount = 5; // 显示5个
	if (!assignments || assignments.length === 0) {
		return [];
	}

	// 根据workflowId+stageId组合进行去重
	const uniqueAssignments = assignments.filter((assignment, index, self) => {
		return (
			index ===
			self.findIndex(
				(a) => a.workflowId === assignment.workflowId && a.stageId === assignment.stageId
			)
		);
	});

	// 返回前N个去重后的数据
	return uniqueAssignments.slice(0, displayedCount);
};

// 获取去重后的所有数据
const getUniqueAssignments = (assignments: any[]) => {
	if (!assignments || assignments.length === 0) {
		return [];
	}

	return assignments.filter((assignment, index, self) => {
		return (
			index ===
			self.findIndex(
				(a) => a.workflowId === assignment.workflowId && a.stageId === assignment.stageId
			)
		);
	});
};

// 获取剩余数量（去重后）
const getRemainingCount = (assignments: any[]) => {
	const uniqueAssignments = getUniqueAssignments(assignments);
	return Math.max(0, uniqueAssignments.length - 5);
};

// 获取剩余的标签（去重后，跳过前5个）
const getRemainingAssignments = (assignments: any[]) => {
	const uniqueAssignments = getUniqueAssignments(assignments);
	return uniqueAssignments.slice(5);
};

// 暴露校验方法给父组件
defineExpose({
	validateForm,
	clearValidationErrors,
	getFieldError,
	previewData,
});

// 图标选项
const iconOptions = {
	star: {
		filledIcon: [IconStar, IconStar, IconStar],
		voidIcon: IconStarOutline,
	},
	heart: {
		filledIcon: [IconHeart, IconHeart, IconHeart],
		voidIcon: IconHeartOutline,
	},
	thumbs: {
		filledIcon: [IconThumbUp, IconThumbUp, IconThumbUp],
		voidIcon: IconThumbUpOutline,
	},
};

const getSelectedFilledIcon = (iconType: string) => {
	return iconOptions[iconType]?.filledIcon;
};

const getSelectedVoidIcon = (iconType: string) => {
	return iconOptions[iconType]?.voidIcon;
};

// 生成slider的刻度标记
const getSliderMarks = (item: any) => {
	const marks: Record<number, string> = {};
	const min = item?.min;
	const max = item?.max;

	for (let i = min; i <= max; i++) {
		marks[i] = '';
	}

	return marks;
};

// 计算问题的实际序号（跳过page_break类型）
const getQuestionNumber = (sectionIndex: string | number, itemIndex: string | number) => {
	const secIdx = Number(sectionIndex);
	const itemIdx = Number(itemIndex);
	if (!props.questionnaire?.sections) return itemIdx + 1;

	const section = props.questionnaire.sections[secIdx];
	if (!section?.questions) return itemIdx + 1;

	let actualQuestionNumber = 1;
	for (let i = 0; i <= itemIdx; i++) {
		const item = section.questions[i];
		if (item.type !== 'page_break') {
			if (i === itemIdx) {
				return actualQuestionNumber;
			}
			actualQuestionNumber++;
		}
	}
	return actualQuestionNumber;
};
</script>

<style scoped lang="scss">
/* 问卷头部样式 */
.questionnaire-header {
	background-color: var(--black-300);
}

.questionnaire-title {
	color: var(--primary-800);
	@apply dark:text-white;
}

.questionnaire-description {
	color: var(--primary-600);
	@apply dark:text-primary-200;
}

/* Assignments样式 */
.preview_assignment-label {
	color: var(--el-text-color-secondary);
	font-weight: 500;
	min-width: 70px;
}

html.dark .preview_assignment-label {
	color: var(--el-text-color-placeholder);
}

.preview_assignment-tag {
	@apply inline-flex items-center rounded-full  text-xs font-semibold transition-colors bg-primary text-white  px-2 py-1;
	white-space: nowrap;
	max-width: 200px;
	/* 固定宽度 */
	flex-shrink: 0;
	/* 防止收缩 */
	padding-right: 8px;
	/* 增加右边距 */
}

.preview_assignment-tag-more {
	@apply inline-flex items-center rounded-full  text-xs font-semibold transition-colors bg-primary text-white px-2 py-1;
	white-space: nowrap;
	width: 40px;
	/* 固定宽度 */
	overflow: hidden;
	text-overflow: ellipsis;
	justify-content: center;
	/* 文本居中 */
	flex-shrink: 0;
	/* 防止收缩 */
	margin-right: 8px;
	/* 增加右边距 */
}

.preview_popover-title {
	font-size: 14px;
	font-weight: 600;
	color: var(--primary-700);
	@apply dark:text-primary-300;
	margin-bottom: 10px;
}

.preview_popover-tags {
	display: flex;
	flex-wrap: wrap;
	gap: 8px;
}

.preview_popover-tag {
	@apply inline-flex items-center rounded-full border text-xs font-semibold transition-colors bg-primary-50 text-primary-500 border-primary-200 px-2 py-1;
	white-space: nowrap;
	width: 150px;
	/* 与主要标签保持一致的固定宽度 */
	overflow: hidden;
	text-overflow: ellipsis;
	justify-content: flex-start;
	/* 左对齐显示，优先显示workflow */
	flex-shrink: 0;
	/* 防止收缩 */
}

.preview_popover-tag:hover {
	@apply bg-primary-100 border-primary-300;
}

/* Assignments容器样式 */
.assignments-container {
	overflow: hidden;
}
.questionnaire-header .assignments-container {
	height: 63px !important;
}
.questionnaire-header .assignments-container span {
	overflow: hidden !important;
	text-overflow: ellipsis !important;
	white-space: nowrap;
}

/* 章节样式 */
.section-container {
	border-color: var(--primary-100);
	@apply dark:border-black-200 dark:bg-black-400;
}

.section-header {
	background-color: var(--black-300);
}

.section-title {
	color: var(--primary-800);
	@apply dark:text-white;
}

.section-description {
	color: var(--primary-600);
	@apply dark:text-primary-200;
}

/* 问题样式 */
.question-item {
	@apply dark:border-black-100;
}

.question-title {
	color: var(--primary-800);
	@apply dark:text-white;
}

.question-description {
	color: var(--primary-600);
	@apply dark:text-primary-300;
}

/* 预览输入组件样式 */
.preview-input {
	:deep(.el-input__wrapper) {
		border-color: var(--primary-200);
		@apply dark:border-black-200;
	}
}

/* 错误状态样式 */
.error-input {
	:deep(.el-input__wrapper) {
		border-color: var(--el-color-danger) !important;
		box-shadow: 0 0 0 1px var(--el-color-danger-light-7) !important;
	}
}

.error-select {
	:deep(.el-select__wrapper) {
		border-color: var(--el-color-danger) !important;
		box-shadow: 0 0 0 1px var(--el-color-danger-light-7) !important;
	}
}

.error-upload {
	:deep(.el-upload-dragger) {
		border-color: var(--el-color-danger) !important;
		background-color: var(--el-color-danger-light-9) !important;
	}
}

.preview-radio {
	:deep(.el-radio__label) {
		color: var(--primary-700);
		@apply dark:text-primary-300;
	}
}

.preview-checkbox {
	:deep(.el-checkbox__label) {
		color: var(--primary-700);
		@apply dark:text-primary-300;
	}
}

.preview-number {
	:deep(.el-input__wrapper) {
		border-color: var(--primary-200);
		@apply dark:border-black-200;
	}
}

.preview-date {
	:deep(.el-input__wrapper) {
		border-color: var(--primary-200);
		@apply dark:border-black-200;
	}
}

.preview-rating {
	:deep(.el-rate__item) {
		color: var(--primary-300);
		@apply dark:text-primary-500;
	}
}

.preview-file {
	:deep(.el-upload) {
		border-color: var(--primary-200);
		@apply dark:border-black-200;
	}

	:deep(.el-upload-dragger) {
		background-color: var(--primary-25);
		border-color: var(--primary-200);
		@apply dark:bg-black-300 dark:border-black-200;
	}
}

/* 网格样式 */
.preview-grid {
	@apply w-full;

	.grid-radio {
		margin: 0;

		:deep(.el-radio__input) {
			margin: 0;
		}

		:deep(.el-radio__label) {
			display: none;
		}
	}

	.grid-checkbox {
		margin: 0;

		:deep(.el-checkbox__input) {
			margin: 0;
		}

		:deep(.el-checkbox__label) {
			display: none;
		}
	}

	.other-input {
		width: 100%;
		max-width: 180px;
	}
}

.responsive-image {
	@apply block;
	max-height: 500px;
	max-width: 100%;
	width: auto;
	height: auto;
	object-fit: contain;

	:deep(.el-image__inner) {
		max-height: 500px;
		max-width: 100%;
		width: auto;
		height: auto;
		object-fit: contain;
	}
}

@media print {
	:deep(.max-h-\[500px\]) {
		max-height: none !important;
		overflow: visible !important;
	}

	:deep(.overflow-hidden),
	:deep(.overflow-y-auto),
	:deep(.overflow-y-scroll),
	:deep(.overflow-auto) {
		overflow: visible !important;
	}

	:deep(.section-container),
	:deep(.question-item) {
		break-inside: avoid;
		page-break-inside: avoid;
	}

	:deep(.print\:hidden) {
		display: none !important;
	}

	:deep(.pdf-exclude) {
		display: none !important;
	}

	.preview_assignment-tag {
		max-width: 150px !important;
	}

	.preview_assignment-tag text {
		overflow: hidden !important;
		text-overflow: ellipsis !important;
		white-space: nowrap !important;
	}

	:deep(.questionnaire-header) {
		background-color: var(--primary-50) !important;
		border-color: var(--primary-100) !important;
		break-after: avoid;
		page-break-after: avoid;
	}

	:global(html),
	:global(body) {
		background: #ffffff !important;
	}
}

// 自定义文本颜色类
.text-secondary {
	color: var(--el-text-color-secondary);
}

.text-regular {
	color: var(--el-text-color-regular);
}

.text-placeholder {
	color: var(--el-text-color-placeholder);
}

// 自定义边框颜色类
.border-light {
	border-color: var(--el-border-color-lighter);
}

.border-base {
	border-color: var(--el-border-color);
}

// 被跳过的问题样式
.question-skipped {
	opacity: 0.5;
	pointer-events: none;
	position: relative;

	&::before {
		content: '';
		position: absolute;
		top: 0;
		left: 0;
		right: 0;
		bottom: 0;
		background-color: rgba(0, 0, 0, 0.02);
		z-index: 1;
		pointer-events: none;
	}

	.question-title,
	.question-description {
		color: var(--el-text-color-disabled);
	}
}
</style>
