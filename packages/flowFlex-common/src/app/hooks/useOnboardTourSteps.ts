/**
 * useOnboardTourSteps
 *
 * Encapsulates all Tour-Guide step logic for the Case Detail page (detail.vue).
 * Extracted so detail.vue stays readable and the step definitions are easy to
 * maintain / extend independently.
 *
 * Usage:
 * ```ts
 * const {
 *   tourGuideRef, tourPersistKey, tourSteps,
 *   checklistOrderMap, questionnaireOrderMap, getTourAnchor,
 *   stageDataLoadingWatcher,
 * } = useOnboardTourSteps({ ... })
 * ```
 */

import { ref, computed, watch } from 'vue';
import TourGuide from '@/components/global/TourGuide/index.vue';
import { UseOnboardTourStepsOptions } from '#/onboard';
import { TourStep } from '#/config';

// ─── Composable ───────────────────────────────────────────────────────────────

export function useOnboardTourSteps(options: UseOnboardTourStepsOptions) {
	const {
		userId,
		onboardingId,
		activeStage,
		stageDataLoading,
		sortedComponents,
		questionnairesData,
		getChecklistDataForComponent,
		stageCanCompleted,
		stageName,
		caseName,
	} = options;

	// ── Refs ──────────────────────────────────────────────────────────────

	const tourGuideRef = ref<InstanceType<typeof TourGuide> | null>(null);

	// ── Persist key ───────────────────────────────────────────────────────

	const tourPersistKey = computed(
		() => `${userId.value}_${onboardingId.value}_${activeStage.value}`
	);

	// ── Order maps ────────────────────────────────────────────────────────

	/** Map checklist component indices → 1-based order number */
	const checklistOrderMap = computed(() => {
		let count = 0;
		const map = new Map<number, number>();
		sortedComponents.value.forEach((comp: any, idx: number) => {
			if (comp.key === 'checklist' && comp.checklistIds?.length > 0) {
				map.set(idx, ++count);
			}
		});
		return map;
	});

	/** Map questionnaire component indices → 1-based order number */
	const questionnaireOrderMap = computed(() => {
		let count = 0;
		const map = new Map<number, number>();
		sortedComponents.value.forEach((comp: any, idx: number) => {
			if (comp.key === 'questionnaires' && comp.questionnaireIds?.length > 0) {
				map.set(idx, ++count);
			}
		});
		return map;
	});

	// ── Anchor helper ─────────────────────────────────────────────────────

	function getTourAnchor(component: any, index: number): string {
		switch (component.key) {
			case 'fields':
				return 'stage-fields';
			case 'quickLink':
				return 'stage-quick-link';
			case 'checklist': {
				const order = checklistOrderMap.value.get(index) ?? 1;
				return order === 1 ? 'stage-checklist-first' : `stage-checklist-other-${order}`;
			}
			case 'questionnaires': {
				// data-tour placed on inner div inside template — outer div stays untagged
				return '';
			}
			case 'files':
				return 'stage-files';
			default:
				return '';
		}
	}

	// ── Step builder ─────────────────────────────────────────────────────

	/**
	 * Builds the full Driver.js step list for the current stage.
	 * Reactive: recalculates whenever sortedComponents, activeStage, or
	 * questionnairesData changes.
	 */
	const tourSteps = computed((): TourStep[] => {
		const steps: TourStep[] = [];

		// ── Step 1: Case title / Stage title ─────────────────────────
		steps.push({
			element: '[data-tour="case-title"]',
			title: stageName.value || caseName.value,
			description: `You've been assigned to handle the <strong>${stageName.value}</strong> stage of <strong>${caseName.value}</strong>. Here are all the tasks you need to complete.`,
			side: 'bottom',
			align: 'start',
		});

		// ── Step 2: Case Progress — single step highlighting the whole panel ──
		steps.push({
			element: '[data-tour="progress-bar"]',
			title: 'Case Progress',
			description:
				'This panel shows every stage in the workflow. The highlighted stage is your current responsibility — required stages are tagged in orange.',
			side: 'left',
			align: 'start',
		});

		// ── Dynamic steps per component ───────────────────────────────
		let checklistCount = 0;
		let questionnaireCount = 0;

		for (const component of sortedComponents.value) {
			if (!component.isEnabled) continue;

			// ── Fields ───────────────────────────────────────────────
			if (component.key === 'fields' && component.staticFields?.length > 0) {
				steps.push({
					element: '[data-tour="stage-fields"] .case-component-header',
					title: 'Fill in the Fields',
					description:
						'Fill in these information fields. Content is saved automatically — no extra submission needed.',
					side: 'top',
					align: 'start',
				});

				// Required-field indicator
				// Element Plus adds .is-required to el-form-item for required fields.
				steps.push({
					element:
						'[data-tour="stage-fields"] .el-form-item.is-required .el-form-item__label',
					title: 'Required Fields',
					description:
						'Fields marked with <span style="color:#f56c6c;font-weight:700;font-size:14px">*</span> are mandatory — you must fill them in before completing this stage.',
					side: 'right',
					align: 'start',
				});

				// ── Quick Link ────────────────────────────────────────
			} else if (component.key === 'quickLink') {
				steps.push({
					element: '[data-tour="stage-quick-link"]',
					title: 'Quick Link',
					description:
						'Click this link to jump to an external system and complete the action there. Come back to this page to continue.',
					side: 'top',
					align: 'start',
				});

				// ── Checklist ────────────────────────────────────────
			} else if (component.key === 'checklist' && component.checklistIds?.length > 0) {
				checklistCount++;
				const cklData = getChecklistDataForComponent(component);
				const taskCount =
					cklData?.reduce((acc: number, cl: any) => acc + (cl.tasks?.length || 0), 0) ??
					0;

				if (checklistCount === 1) {
					steps.push({
						element: '[data-tour="stage-checklist-first"] .case-component-header',
						title: 'Task Checklist',
						description: `This is a task checklist containing <strong>${taskCount}</strong> item${
							taskCount !== 1 ? 's' : ''
						}. You need to complete each one.`,
						side: 'top',
						align: 'start',
					});
					steps.push({
						element: '[data-tour="checklist-task-done"]',
						title: 'Mark as Done',
						description:
							'Once you confirm a task is complete, click the <strong>Done</strong> button to check it off.',
						side: 'top',
						align: 'start',
					});
					steps.push({
						element: '[data-tour="checklist-task-details"]',
						title: 'Task Details',
						description:
							'Click <strong>Details</strong> to open the task detail panel where you can add Notes and upload Attachments.',
						side: 'top',
						align: 'start',
						waitForUserClick: true,
						afterUserClick: async () => {
							// Wait for el-dialog's open animation to fully complete
							// before the next step tries to highlight content inside it.
							await new Promise<void>((resolve) => {
								const deadline = Date.now() + 2000;
								const checkOpened = () => {
									// el-dialog adds aria-hidden="false" and the dialog wrapper
									// becomes visible after the "opened" animation event.
									const dlg = document.querySelector<HTMLElement>(
										'.task-details-dialog .el-dialog'
									);
									if (dlg && dlg.getBoundingClientRect().height > 0) {
										// Give the transition a final frame to settle
										setTimeout(resolve, 300);
									} else if (Date.now() < deadline) {
										requestAnimationFrame(checkOpened);
									} else {
										resolve();
									}
								};
								checkOpened();
							});
						},
					});
					// After the user clicks Details and the dialog opens,
					// introduce each section inside it.
					steps.push({
						element: '[data-tour="task-detail-info"]',
						title: 'Task Info',
						description:
							'The task details panel shows the task status and assignee at a glance.',
						side: 'bottom',
						align: 'start',
						lazyElement: true,
					});
					steps.push({
						element: '[data-tour="task-detail-notes"]',
						title: 'Notes',
						description:
							'Add <strong>Notes</strong> here to record any remarks, progress updates, or explanations for this task. Click <strong>Add Note</strong> to create one.',
						side: 'top',
						align: 'start',
						lazyElement: true,
					});
					steps.push({
						element: '[data-tour="task-detail-attachments"]',
						title: 'Attachments',
						description:
							'Upload <strong>Attachments</strong> as supporting evidence — documents, screenshots, or receipts that prove the task is complete.',
						side: 'top',
						align: 'start',
						lazyElement: true,
					});
					steps.push({
						element: '[data-tour="task-detail-changelog"]',
						title: 'Change Log',
						description:
							'The <strong>Change Log</strong> automatically records all status changes and updates to this task, so you always have a full audit trail.',
						side: 'top',
						align: 'start',
						lazyElement: true,
					});
					// After introducing the dialog contents, ask the user to close it
					steps.push({
						element: '.task-details-dialog .el-dialog__headerbtn',
						title: 'Close the Details Panel',
						description:
							'Click the <strong>×</strong> button to close this panel and continue the tour.',
						side: 'bottom',
						align: 'end',
						lazyElement: true,
						waitForUserClick: true,
						afterUserClick: async () => {
							// Wait for el-dialog close animation to finish
							await new Promise<void>((resolve) => {
								const deadline = Date.now() + 1500;
								const checkClosed = () => {
									const dlg = document.querySelector<HTMLElement>(
										'.task-details-dialog .el-dialog'
									);
									if (!dlg || dlg.getBoundingClientRect().height === 0) {
										resolve();
									} else if (Date.now() < deadline) {
										requestAnimationFrame(checkClosed);
									} else {
										resolve();
									}
								};
								// Give the click a frame to start the animation
								setTimeout(checkClosed, 100);
							});
						},
					});
					steps.push({
						element: '[data-tour="checklist-progress"]',
						title: 'Completion Progress',
						description:
							'Once all tasks are checked off, the checklist is automatically marked as complete. Progress updates in real time.',
						side: 'top',
						align: 'start',
					});
				} else {
					// Subsequent checklists: one summary step
					steps.push({
						element: `[data-tour="stage-checklist-other-${checklistCount}"] .case-component-header`,
						title: cklData?.[0]?.name || `Checklist ${checklistCount}`,
						description:
							'Continue completing the remaining checklists the same way — check off tasks and add notes / attachments where needed.',
						side: 'top',
						align: 'start',
					});
				}

				// ── Questionnaire ────────────────────────────────────
			} else if (
				component.key === 'questionnaires' &&
				component.questionnaireIds?.length > 0
			) {
				questionnaireCount++;
				// Only guide the first questionnaire; subsequent ones are skipped.
				if (questionnaireCount > 1) continue;

				const qData = questionnairesData.value.find(
					(q: any) => component.questionnaireIds?.includes(q.id)
				);
				const qName = qData?.name || 'Questionnaire';
				let sections: any[] = [];
				try {
					const structure = qData?.structureJson ? JSON.parse(qData.structureJson) : null;
					sections = structure?.sections ?? [];
				} catch {
					sections = [];
				}
				const sectionCount = sections.length;
				const sectionText =
					sectionCount > 0
						? ` with <strong>${sectionCount}</strong> section${
								sectionCount !== 1 ? 's' : ''
						  }`
						: '';

				// A: Questionnaire header
				steps.push({
					element: '[data-tour="stage-questionnaire-first"] .case-component-header',
					title: qName,
					description: `This is an information collection form${sectionText}. Fill in each section and click <strong>Submit</strong> when done.`,
					side: 'top',
					align: 'start',
				});

				// B: Section navigation dots (only when > 1 section)
				if (sectionCount > 1) {
					steps.push({
						element: '[data-tour="stage-questionnaire-first"] .section-progress',
						title: 'Section Progress',
						description:
							'The questionnaire is split into sections. The dots show which section you are on.',
						side: 'top',
						align: 'start',
					});
					steps.push({
						element: '[data-tour="questionnaire-next-section"]',
						title: 'Next Section',
						description:
							'Click <strong>Next</strong> to move to the next section. The <strong>Submit</strong> button only appears on the last section.',
						side: 'top',
						align: 'start',
					});
				}

				// C: Required question indicator (* mark)
				steps.push({
					element: '[data-tour="stage-questionnaire-first"] .text-red-500',
					title: 'Required Questions',
					description:
						'Questions marked with <span style="color:#f56c6c;font-weight:700;font-size:14px">*</span> are mandatory. All required questions must be answered before you can submit.',
					side: 'right',
					align: 'start',
				});

				// D: Submit button (last section only — nav-right area)
				steps.push({
					element:
						sectionCount > 1
							? '[data-tour="questionnaire-nav-right"]'
							: '[data-tour="stage-questionnaire-first"] .bottom-navigation',
					title: 'Submit the Questionnaire',
					description:
						sectionCount > 1
							? 'After completing all sections, the <strong>Next</strong> button becomes <strong>Submit</strong>. Click it to lock in your answers.'
							: 'Once all required questions are answered, click <strong>Submit</strong> to save your responses. Submitted answers are locked.',
					side: 'top',
					align: 'start',
				});

				// ── Files ─────────────────────────────────────────────
			} else if (component.key === 'files') {
				steps.push({
					element: '[data-tour="stage-files"] .case-component-header',
					title: 'File Uploads',
					description:
						'Drag files into this area or click to upload. Files are automatically linked to the current stage.',
					side: 'top',
					align: 'start',
				});
				// Required indicator — Documents.vue uses .text-red-300 for the * mark
				steps.push({
					element: '[data-tour="stage-files"] .text-red-300',
					title: 'Required Upload',
					description:
						'This file upload section is <span style="color:#f56c6c;font-weight:700">required</span>. You must upload at least one file before you can complete this stage.',
					side: 'right',
					align: 'start',
				});
			}
		}

		// ── Last steps: Save + Complete buttons ───────────────────────
		if (activeStage.value && !stageCanCompleted.value) {
			steps.push({
				element: '[data-tour="save-btn"]',
				title: 'Save Your Work',
				description:
					'Click <strong>Save</strong> to save any changes to fields at any time.',
				side: 'bottom',
				align: 'end',
			});
			steps.push({
				element: '[data-tour="complete-btn"]',
				title: 'Advance to Next Stage',
				description:
					'Once all required items above are complete, click <strong>Complete</strong> to move to the next stage. This stage will be locked after you advance.',
				side: 'bottom',
				align: 'end',
			});
		}

		return steps;
	});

	// ── Stage-data-loading watcher trigger ────────────────────────────────

	/**
	 * Call this in detail.vue to wire up the auto-start trigger.
	 * @param canAutoStart Optional getter — if provided, startTour is only
	 *   called when this returns true. Use to gate auto-start on business
	 *   rules (e.g. "current user is the stage assignee").
	 * Returns the watcher stop handle.
	 */
	function setupTourWatcher(canAutoStart?: () => boolean) {
		return watch(stageDataLoading, async (isLoading) => {
			if (!isLoading && activeStage.value) {
				// Bail out immediately if the caller says auto-start is not allowed.
				if (canAutoStart && !canAutoStart()) return;
				await new Promise((r) => setTimeout(r, 300));
				tourGuideRef.value?.startTour();
			}
		});
	}

	return {
		tourGuideRef,
		tourPersistKey,
		tourSteps,
		checklistOrderMap,
		questionnaireOrderMap,
		getTourAnchor,
		setupTourWatcher,
	};
}
