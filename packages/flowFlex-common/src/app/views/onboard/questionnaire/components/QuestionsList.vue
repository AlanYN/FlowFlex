<template>
	<div class="questions-list">
		<draggable
			v-model="questionsData"
			item-key="temporaryId"
			handle=".drag-handle"
			:group="dragGroup"
			@change="handleQuestionDragEnd"
			@end="handleDragEnd"
			@start="handleDragStart"
			@remove="handleRemove"
			ghost-class="ghost-question"
			class="questions-draggable"
			:animation="300"
			:sort="true"
		>
			<template #item="{ element: item, index }">
				<div class="question-item flex max-w-full">
					<template
						v-if="
							editingQuestionId === item.id || editingQuestionId === item.temporaryId
						"
					>
						<div class="w-full">
							<QuestionEditor
								:question-types="questionTypes"
								:pressent-question-type="item.type"
								:editing-question="editingQuestion"
								:is-editing="true"
								@update-question="handleUpdateQuestion"
								@cancel-edit="cancelEditQuestion"
							/>
						</div>
					</template>
					<template v-else>
						<div class="drag-handle">
							<DragIcon class="drag-icon" />
						</div>
						<div class="flex-1 max-w-[calc(100%-2.25rem)]">
							<div class="question-header">
								<div class="question-left">
									<div class="question-info">
										<div class="flex items-center gap-2 truncate">
											{{ currentSectionIndex + 1 }}.{{ index + 1 }}.
											<el-tag size="small" class="card-tag">
												{{ getQuestionTypeName(item.type) }}
											</el-tag>
											<el-tag v-if="item.required" size="small" type="danger">
												Required
											</el-tag>

											<el-tag
												v-if="item.action"
												size="small"
												type="success"
												closable
												@close="handleRemoveAction(index)"
												@click="editAction(index)"
											>
												{{ item.action.name }}
											</el-tag>
										</div>
										<div class="question-meta mt-2">
											<div class="question-text">{{ item.question }}</div>
										</div>
									</div>
								</div>
								<div class="question-actions">
									<el-dropdown
										placement="bottom"
										v-if="
											functionPermission(
												ProjectPermissionEnum.question.update
											) &&
											item.type !== 'image' &&
											item.type != 'video' &&
											item.type != 'page_break'
										"
									>
										<el-button :icon="MoreFilled" link />
										<template #dropdown>
											<el-dropdown-menu>
												<el-dropdown-item
													@click="handleAddContent('video', index)"
												>
													<div class="flex items-center gap-2">
														<Icon
															icon="mdi:video-outline"
															class="drag-icon"
														/>
														<span class="text-xs">Add Video</span>
													</div>
												</el-dropdown-item>
												<el-dropdown-item
													@click="handleAddContent('image', index)"
												>
													<div class="flex items-center gap-2">
														<Icon
															icon="mdi:image-area"
															class="drag-icon"
														/>
														<span class="text-xs">Add Image</span>
													</div>
												</el-dropdown-item>
												<el-dropdown-item
													v-if="
														item.type === 'multiple_choice' ||
														(item.type === 'checkboxes' &&
															setGoToSection)
													"
													@click="openJumpRuleEditor(index)"
													divided
												>
													<div class="flex items-center gap-2">
														<Icon
															icon="mdi:transit-connection-variant"
															class="drag-icon"
														/>
														<span class="text-xs">
															{{
																item.type === 'multiple_choice'
																	? 'Go to Question Based on Answer'
																	: 'Go to Section Based on Answer'
															}}
														</span>
													</div>
												</el-dropdown-item>
												<el-dropdown-item
													@click="openActionEditor(index)"
													divided
												>
													<div class="flex items-center gap-2">
														<Icon
															icon="tabler:math-function"
															class="drag-icon"
														/>
														<span class="text-xs">
															Configure Action
														</span>
													</div>
												</el-dropdown-item>
											</el-dropdown-menu>
										</template>
									</el-dropdown>
									<div class="flex">
										<el-button
											v-if="
												item.type !== 'image' &&
												item.type != 'video' &&
												item.type != 'page_break' &&
												functionPermission(
													ProjectPermissionEnum.question.update
												)
											"
											type="primary"
											link
											@click="editQuestion(index)"
											:icon="Edit"
											class="edit-question-btn"
										/>
									</div>
									<el-button
										type="danger"
										link
										v-if="
											functionPermission(
												ProjectPermissionEnum.question.delete
											)
										"
										@click="removeQuestion(index)"
										:icon="Delete"
										class="delete-question-btn"
									/>
								</div>
							</div>
							<div v-if="item.description" class="question-description mt-2">
								{{ item.description }}
							</div>
							<!-- 显示已上传的文件 -->
							<div
								v-if="item.questionProps && item.questionProps.fileUrl"
								class="mt-3 p-3 rounded-xl border file-upload-display"
							>
								<div class="flex items-center justify-between gap-3">
									<div class="flex items-center gap-2 flex-1 min-w-0">
										<Icon
											:icon="
												item.questionProps.type === 'image'
													? 'mdi:image-area'
													: 'mdi:video-outline'
											"
											class="w-5 h-5 flex-shrink-0"
											:class="
												item.questionProps.type === 'image'
													? 'text-green-500'
													: 'text-orange-500'
											"
										/>
										<span class="text-sm font-medium file-name-text truncate">
											{{ item.questionProps.fileName }}
										</span>
										<el-tag
											size="small"
											:type="
												item.questionProps.type === 'image'
													? 'success'
													: 'warning'
											"
										>
											{{
												item.questionProps.type === 'image'
													? 'Image'
													: 'Video'
											}}
										</el-tag>
									</div>
									<el-button
										type="danger"
										link
										size="small"
										@click="removeFile(index)"
										v-if="
											functionPermission(
												ProjectPermissionEnum.question.delete
											)
										"
										:icon="Delete"
									/>
								</div>
							</div>
							<div
								v-if="item.options && item.options.length > 0"
								class="question-options w-full"
							>
								<div class="options-label">Options:</div>
								<div class="options-list gap-y-2">
									<div
										v-for="(option, optionIndex) in item.options"
										:key="option.id"
										class="option-item w-full flex items-center justify-between"
									>
										<div
											class="option-content flex max-w-[50%] items-center gap-2 flex-1"
										>
											<span class="option-number">
												{{ optionIndex + 1 }}.
											</span>
											<el-tag v-if="option.isOther" type="warning">
												Other
											</el-tag>
											<div v-else class="option-badge truncate">
												{{ option.label }}
											</div>
											<!-- 显示选项的action标签 -->
											<el-tag
												v-if="
													item.type === 'multiple_choice' ||
													(item.type === 'checkboxes' && setGoToSection)
												"
												type="primary"
											>
												→
												{{ getJumpTargetName(item, option.temporaryId) }}
											</el-tag>
											<el-tag
												v-if="option.action"
												type="success"
												closable
												@close="handleRemoveAction(index, optionIndex)"
												@click="editAction(index, optionIndex)"
											>
												{{ option.action.name }}
											</el-tag>
										</div>
										<!-- 选项操作下拉菜单，和问题级别保持一致 -->
										<el-dropdown
											placement="bottom"
											v-if="
												functionPermission(
													ProjectPermissionEnum.question.update
												) &&
												(item.type == 'multiple_choice' ||
													item.type == 'checkboxes')
											"
										>
											<el-button :icon="MoreFilled" link size="small" />
											<template #dropdown>
												<el-dropdown-menu>
													<el-dropdown-item
														@click="
															openActionEditor(index, optionIndex)
														"
													>
														<div class="flex items-center gap-2">
															<Icon
																icon="tabler:math-function"
																class="drag-icon"
															/>
															<span class="text-xs">
																Configure Action
															</span>
														</div>
													</el-dropdown-item>
												</el-dropdown-menu>
											</template>
										</el-dropdown>
									</div>
								</div>
							</div>
						</div>
					</template>
				</div>
			</template>
		</draggable>

		<!-- 空状态 -->
		<div v-if="questions.length === 0" class="empty-questions">
			<el-empty description="No questions yet">
				<template #image>
					<el-icon size="48" color="var(--el-color-info)">
						<Document />
					</el-icon>
				</template>
			</el-empty>
		</div>
		<!-- 跳转规则编辑弹窗 -->
		<JumpRuleEditor
			v-model:visible="jumpRuleEditorVisible"
			:question="currentEditingQuestion"
			:sections="sections"
			@save="handleJumpRulesSave"
		/>

		<ActionConfigDialog
			ref="actionConfigDialogRef"
			:triggerSourceId="actionConfig?.id || ''"
			:triggerType="TriggerTypeEnum.Questionnaire"
			@save-success="onActionSave"
		/>
	</div>
</template>

<script setup lang="ts">
import { ref, watch, nextTick } from 'vue';
import { Delete, Document, Edit, MoreFilled } from '@element-plus/icons-vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import draggable from 'vuedraggable';
import DragIcon from '@assets/svg/publicPage/drag.svg';
import JumpRuleEditor from './JumpRuleEditor.vue';
import QuestionEditor from './QuestionEditor.vue';
import type { Section, JumpRule, QuestionWithJumpRules } from '#/section';
import { QuestionnaireSection } from '#/section';
import { triggerFileUpload } from '@/utils/fileUploadUtils';
import ActionConfigDialog from '@/components/actionTools/ActionConfigDialog.vue';
import { TriggerTypeEnum } from '@/enums/appEnum';
import { functionPermission } from '@/hooks';
import { ProjectPermissionEnum } from '@/enums/permissionEnum';

import { useI18n } from 'vue-i18n';

const { t } = useI18n();

interface QuestionType {
	id: string;
	name: string;
	icon: string;
	isNew?: boolean;
}

interface Props {
	questions: QuestionnaireSection[];
	questionTypes: QuestionType[];
	sections: Section[];
	currentSectionIndex: number;
	setGoToSection?: boolean;
}

const props = defineProps<Props>();

const emits = defineEmits<{
	'remove-question': [index: number];
	'drag-end': [questions: QuestionnaireSection[]];
	'update-jump-rules': [questionIndex: number, rules: JumpRule[]];
	'update-question': [index: number, question: QuestionnaireSection];
	'drag-start': [question: QuestionnaireSection, questionIndex: number];
}>();

// 编辑状态管理 - 使用ID而不是索引
const editingQuestionId = ref<string | null>(null);
const editingQuestion = ref<QuestionnaireSection | null>(null);

// 跳转规则编辑器状态
const jumpRuleEditorVisible = ref(false);
const currentEditingQuestion = ref<QuestionWithJumpRules | null>(null);
const currentEditingIndex = ref(-1);

// 当前问题索引（用于文件上传）
const currentQuestionIndex = ref<number>(-1);

// 拖拽状态
const isDragging = ref(false);
const draggedQuestionId = ref<string | null>(null);

const questionsData = ref([...props.questions]);

// 拖拽组配置
const dragGroup = { name: 'questions', pull: true, put: false };

// 监听 props 变化
watch(
	() => props.questions,
	(newQuestions, oldQuestions) => {
		// 检查是否是真正的变化（避免不必要的更新）
		if (newQuestions.length !== questionsData.value.length) {
			questionsData.value = [...newQuestions];
		} else {
			// 长度相同，检查是否有问题被替换
			const hasChanged = newQuestions.some((q, index) => {
				return q.temporaryId !== questionsData.value[index]?.temporaryId;
			});
			if (hasChanged) {
				questionsData.value = [...newQuestions];
			}
		}
	},
	{ deep: true, immediate: true }
);

// 监听 currentSectionIndex 变化，确保在切换 section 时更新问题列表
watch(
	() => props.currentSectionIndex,
	(newIndex, oldIndex) => {
		// 当 section 切换时，立即同步问题列表
		if (newIndex !== oldIndex && !isDragging.value) {
			questionsData.value = [...props.questions];
		}
	}
);

const removeQuestion = (index: number) => {
	emits('remove-question', index);
};

const editQuestion = (index: number) => {
	const question = questionsData.value[index];
	editingQuestionId.value = question?.temporaryId || null;
	editingQuestion.value = { ...question };
};

// 处理问题更新
const handleUpdateQuestion = (updatedQuestion: QuestionnaireSection) => {
	const index = questionsData.value.findIndex((q) => q.temporaryId === editingQuestionId.value);
	if (index !== -1) {
		questionsData.value[index] = {
			...questionsData.value[index],
			...updatedQuestion,
		};
		editingQuestionId.value = null;
		editingQuestion.value = null;
		handleQuestionDragEnd();
	}
};

// 取消编辑
const cancelEditQuestion = () => {
	editingQuestionId.value = null;
	editingQuestion.value = null;
};

const handleQuestionDragEnd = () => {
	// 只有在非跨组件拖拽时才更新
	// 如果问题还在列表中，说明是正常拖拽（列表内排序）
	if (draggedQuestionId.value) {
		const questionStillExists = questionsData.value.some(
			(q) => q.temporaryId === draggedQuestionId.value
		);
		// 只有在问题还在列表中时才发送 drag-end 事件
		// 如果问题不在列表中，说明是跨组件拖拽，不应该发送 drag-end 事件
		if (questionStillExists) {
			emits('drag-end', questionsData.value);
		}
	} else {
		// 如果没有 draggedQuestionId，可能是列表内排序
		emits('drag-end', questionsData.value);
	}
};

// 处理拖拽开始事件（vuedraggable）
const handleDragStart = (evt: any) => {
	const draggedQuestion = questionsData.value[evt.oldIndex];
	if (draggedQuestion) {
		isDragging.value = true;
		draggedQuestionId.value = draggedQuestion.temporaryId;
		emits('drag-start', draggedQuestion, evt.oldIndex);
	}
};

// 处理问题被移除（跨组件拖拽时）
const handleRemove = (evt: any) => {
	// 当问题被拖到其他组件时，vuedraggable 会触发 remove 事件
	// 注意：此时问题已经从 questionsData 中移除了，但 props.questions 可能还没有更新
	// 我们需要从 questionsData 中移除被拖走的问题，而不是同步整个 props.questions
	// 因为 props.questions 可能还没有更新，同步会导致数据不一致
	if (evt.removed && evt.removed.element) {
		// 问题已经被 vuedraggable 从 questionsData 中移除了
		// 我们只需要确保 questionsData 与当前的 questionsData 保持一致
		// 不需要同步 props.questions，因为父组件会通过 handleQuestionDropToSection 更新
		// 这里不做任何操作，让 vuedraggable 自然处理移除
	}
};

// 处理拖拽结束事件（vuedraggable）
const handleDragEnd = async (evt: any) => {
	// 检查被拖拽的问题是否还在列表中
	if (draggedQuestionId.value) {
		const questionStillExists = questionsData.value.some(
			(q) => q.temporaryId === draggedQuestionId.value
		);

		// 如果问题不在列表中，说明是跨组件拖拽（问题已被移动到其他分区）
		// 此时不应该同步 questionsData，因为父组件会通过 handleQuestionDropToSection 更新 props.questions
		// 我们只需要等待父组件更新完成后再同步
		if (!questionStillExists) {
			// 等待父组件处理完成（通过 nextTick 确保父组件的更新已经完成）
			await nextTick();
			await nextTick(); // 多等待一个 tick，确保父组件完全更新
			// 然后同步 questionsData 到 props.questions
			// 但是要确保只同步当前 section 的问题，而不是所有问题
			questionsData.value = [...props.questions];
		} else {
			// 如果是正常拖拽（列表内排序），确保数据同步
			// 注意：这里不应该发送 drag-end 事件，因为 handleQuestionDragEnd 已经处理了
		}
	} else {
		// 如果没有 draggedQuestionId，确保数据同步
		questionsData.value = [...props.questions];
		await nextTick();
	}

	// 重置拖拽状态
	isDragging.value = false;
	draggedQuestionId.value = null;
};

const getQuestionTypeName = (type: string) => {
	const questionType = props.questionTypes.find((t) => t.id === type);
	return questionType ? questionType.name : type;
};

// 处理添加内容（视频或图片）
const handleAddContent = async (type: 'video' | 'image', questionIndex: number) => {
	currentQuestionIndex.value = questionIndex;

	await triggerFileUpload(type, (result) => {
		// 获取当前问题
		const question = questionsData.value[currentQuestionIndex.value];
		if (question && result.success) {
			// 直接设置 questionProps 为单个文件对象
			question.questionProps = {
				type: type,
				fileName: result.fileName!,
				fileUrl: result.fileUrl!,
				uploadDate: new Date().toISOString(),
			};

			// 触发更新
			emits('update-question', currentQuestionIndex.value, question);
		}
	});

	// 重置状态
	currentQuestionIndex.value = -1;
};

// 移除文件
const removeFile = (questionIndex: number) => {
	const question = questionsData.value[questionIndex];
	if (question && question.questionProps && 'type' in question.questionProps) {
		question.questionProps = undefined; // 移除文件对象
		emits('update-question', questionIndex, question);
		ElMessage.success('File removed successfully');
	}
};

// 打开跳转规则编辑器
const openJumpRuleEditor = (index: number) => {
	const question = questionsData.value[index];
	if (question && (question.type === 'multiple_choice' || question.type === 'checkboxes')) {
		currentEditingQuestion.value = question as QuestionWithJumpRules;
		currentEditingIndex.value = index;
		jumpRuleEditorVisible.value = true;
	}
};

const actionConfig = ref<any>(null);
const actionType = ref<'question' | 'option'>('question');
const openActionEditor = (index: number, optionIndex?: number) => {
	const question = questionsData.value[index];
	if (!question) return;
	currentEditingQuestion.value = question as QuestionWithJumpRules;
	if (optionIndex !== undefined) {
		actionType.value = 'option';
		const option = question.options?.[optionIndex];
		if (option) {
			actionConfig.value = option || '';
			actionConfigDialogRef.value?.open({
				triggerSourceId: option.id,
				triggerType: TriggerTypeEnum.Questionnaire,
			});
		}
	} else {
		actionType.value = 'question';
		if (question) {
			actionConfig.value = question || '';
			actionConfigDialogRef.value?.open({
				triggerSourceId: question.id,
				triggerType: TriggerTypeEnum.Questionnaire,
			});
		}
	}
};

const actionConfigDialogRef = ref<InstanceType<typeof ActionConfigDialog>>();
const onActionSave = (res) => {
	const questionIndex = questionsData.value.findIndex(
		(q) => q.temporaryId === currentEditingQuestion.value?.temporaryId
	);
	if (actionType.value === 'question') {
		if (res.id && questionIndex !== -1) {
			questionsData.value[questionIndex].action = {
				id: res.id,
				name: res.name,
			};
		}
	} else if (actionType.value === 'option') {
		if (res.id && questionIndex !== -1) {
			const option = questionsData.value[questionIndex].options?.find(
				(option) =>
					(option?.temporaryId !== undefined &&
						option?.temporaryId === actionConfig.value?.temporaryId) ||
					(option?.id !== undefined && option?.id === actionConfig.value?.id)
			);
			if (option) {
				option.action = {
					id: res.id,
					name: res.name,
				};
			}
		}
	}
};

const handleRemoveAction = async (index: number, optionIndex?: number) => {
	const question = questionsData.value[index];
	if (!question) return;
	if (optionIndex !== undefined) {
		const option = question.options?.[optionIndex];
		if (option) {
			await removeAction(option.action?.id || '', () => {
				option.action = undefined;
			});
		}
	} else {
		await removeAction(question.action?.id || '', () => {
			question.action = undefined;
		});
	}
};

const editAction = async (index: number, optionIndex?: number) => {
	const question = questionsData.value[index];
	if (!question) return;
	currentEditingQuestion.value = question as QuestionWithJumpRules;
	actionType.value = optionIndex !== undefined ? 'option' : 'question';
	let actionId = '';

	if (optionIndex !== undefined) {
		const option = question.options?.[optionIndex];
		actionId = option?.action?.id || '';
		actionConfig.value = option;
	} else {
		actionId = question.action?.id || '';
		actionConfig.value = question;
	}
	actionConfigDialogRef.value?.open({
		actionId: actionId,
		triggerSourceId: question.id,
		triggerType: TriggerTypeEnum.Questionnaire,
	});
};

const removeAction = async (id, callback) => {
	try {
		ElMessageBox.confirm(
			'This will permanently delete the action. Continue?',
			'Delete Action',
			{
				confirmButtonText: 'Delete',
				cancelButtonText: 'Cancel',
				type: 'warning',
				beforeClose: async (action, instance, done) => {
					if (action === 'confirm') {
						// 显示loading状态
						instance.confirmButtonLoading = true;
						instance.confirmButtonText = 'Activating...';

						try {
							callback && callback();
							done(); // 关闭对话框
						} catch (error) {
							ElMessage.error(t('sys.api.operationFailed'));
							// 恢复按钮状态
							instance.confirmButtonLoading = false;
							instance.confirmButtonText = 'Delete';
						}
					} else {
						done(); // 取消或关闭时直接关闭对话框
					}
				},
			}
		);
	} catch {
		// User cancelled
	}
};

// 处理跳转规则保存
const handleJumpRulesSave = (rules: JumpRule[]) => {
	if (currentEditingIndex.value >= 0) {
		emits('update-jump-rules', currentEditingIndex.value, rules);
		currentEditingQuestion.value = null;
		currentEditingIndex.value = -1;
	}
};

// 获取选项的跳转目标名称
const getJumpTargetName = (question: QuestionnaireSection, optionId: string) => {
	if (!question.jumpRules || question.jumpRules.length === 0) {
		return 'Next';
	}

	const jumpRule = question.jumpRules.find((rule) => rule.optionId === optionId);
	if (jumpRule) {
		// 多选题（checkboxes）只显示section名称
		if (question.type === 'checkboxes') {
			return jumpRule.targetSectionName;
		}

		// 单选题（multiple_choice）：如果有targetQuestionId，显示问题编号；否则显示section名称（兼容旧数据）
		if (jumpRule.targetQuestionId && jumpRule.targetQuestionName) {
			// 查找目标问题所在的section和问题索引
			const targetSection = props.sections.find(
				(s) => s.temporaryId === jumpRule.targetSectionId
			);
			if (targetSection) {
				const questionIndex = targetSection.questions.findIndex(
					(q) => q.temporaryId === jumpRule.targetQuestionId
				);
				const sectionIndex = props.sections.findIndex(
					(s) => s.temporaryId === jumpRule.targetSectionId
				);
				if (questionIndex !== -1 && sectionIndex !== -1) {
					return `Question ${sectionIndex + 1}.${questionIndex + 1}`;
				}
			}
			// 如果找不到索引，至少显示问题名称
			return jumpRule.targetQuestionName;
		}
		// 兼容旧数据：只显示section名称
		return jumpRule.targetSectionName;
	}

	return 'Next';
};
</script>

<style scoped lang="scss">
.questions-list {
	margin-bottom: 1.5rem;
}

.questions-draggable {
	display: flex;
	flex-direction: column;
	gap: 0.75rem;
}

.question-item {
	padding: 0.75rem;
	border: 1px solid var(--el-border-color-light);
	background-color: white;
	transition: all 0.2s ease;
	@apply rounded-xl;
}

.question-item:hover {
	box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
}

.question-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
}

.question-left {
	display: flex;
	align-items: center;
	gap: 0.75rem;
	flex: 1;
	min-width: 0;
}

.drag-handle {
	display: flex;
	align-items: center;
	justify-content: center;
	width: 1.5rem;
	height: 1.5rem;
	cursor: move;
	color: var(--primary-400);
	transition: color 0.2s;
	flex-shrink: 0;
}

.drag-handle:hover {
	color: var(--primary-600);
}

.drag-handle:active {
	cursor: grabbing;
}

.drag-icon {
	width: 1rem;
	height: 1rem;
}

.question-info {
	flex: 1;
	min-width: 0;
}

.question-actions {
	display: flex;
	align-items: center;
	gap: 0.5rem;
	flex-shrink: 0;
}

.edit-question-btn {
	color: var(--primary-600) !important;
	flex-shrink: 0;
}

.edit-question-btn:hover {
	background-color: var(--primary-50) !important;
}

.question-text {
	font-weight: 500;
	color: var(--primary-800);
	word-break: break-word;
}

.question-meta {
	display: flex;
	gap: 0.5rem;
	flex-wrap: wrap;
}

.card-tag {
	background-color: var(--primary-50);
	color: var(--primary-700);
	border-color: var(--primary-200);
	@apply dark:bg-primary-800 dark:text-primary-200 dark:border-primary-600;
}

.question-description {
	font-size: 0.875rem;
	color: var(--el-text-color-regular);
}

.question-options {
	margin-top: 0.5rem;
}

.options-label {
	font-size: 0.75rem;
	font-weight: 500;
	color: var(--primary-700);
	margin-bottom: 0.25rem;
}

.options-list {
	display: flex;
	flex-direction: column;
}

.option-item {
	display: flex;
	align-items: center;
	gap: 0.5rem;
	font-size: 0.75rem;
}

.option-number {
	font-weight: 600;
	color: var(--el-color-primary);
	min-width: 1rem;
	flex-shrink: 0;
}

.option-badge {
	padding: 0.125rem 0.625rem;
	border-radius: 9999px;
	border: 1px solid var(--el-border-color-light);
	font-size: 0.75rem;
	font-weight: 600;
	color: var(--el-text-color-regular);
	background-color: var(--el-fill-color-lighter);
	transition: all 0.2s;
}

.empty-questions {
	text-align: center;
}

.delete-question-btn {
	color: var(--el-color-danger) !important;
	flex-shrink: 0;
}

.delete-question-btn:hover {
	background-color: var(--el-color-danger-light-9) !important;
}

/* 拖拽时的幽灵样式 */
.ghost-question {
	opacity: 0.6;
	background: var(--el-color-primary-light-9);
	border: 1px dashed var(--el-color-primary);
}

/* 深色模式支持 */
.dark .question-item {
	background-color: var(--black);
	border-color: var(--el-border-color-light);
}

.dark .question-item:hover {
	border-color: var(--el-color-primary);
}

.dark .question-text {
	color: var(--el-color-white);
}

.dark .question-description {
	color: var(--primary-300);
}

.dark .drag-handle {
	color: var(--primary-500);
}

.dark .drag-handle:hover {
	color: var(--primary-400);
}

.dark .ghost-question {
	background: var(--primary-700);
	border: 1px dashed var(--primary-400);
}

.dark .options-label {
	color: var(--primary-200);
}

.dark .option-number {
	color: var(--el-color-primary-light-3);
}

.dark .option-badge {
	color: var(--el-text-color-regular);
	background-color: var(--el-fill-color-dark);
	border-color: var(--el-border-color);
}

.dark .empty-questions {
	color: var(--primary-300);
}

.dark .edit-question-btn {
	color: var(--primary-400) !important;
}

.dark .edit-question-btn:hover {
	background-color: var(--primary-700) !important;
}

.file-upload-display {
	background: var(--el-fill-color-lighter);
	border-color: var(--el-border-color-light);
}

html.dark .file-upload-display {
	background: var(--el-fill-color-darker);
	border-color: var(--el-border-color);
}

.file-name-text {
	color: var(--el-text-color-regular);
}

html.dark .file-name-text {
	color: var(--el-text-color-secondary);
}
</style>
