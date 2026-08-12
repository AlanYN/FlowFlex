import { ref, type Ref } from 'vue';
import { TourStep, UseTourGuideOptions } from '#/config';

// ═══════════════════════════════════════════════════════════════════
// Helpers
// ═══════════════════════════════════════════════════════════════════

/**
 * Return the first VISIBLE element matching a selector.
 * `querySelector` also matches elements hidden with `display:none` (collapsed
 * sections, disabled components, etc.). Driver.js cannot highlight those, so
 * we filter them out up-front.
 */
function _findVisibleElement(selector: string): Element | null {
	const nodes = Array.from(document.querySelectorAll<HTMLElement>(selector));
	for (const el of nodes) {
		if (el.getClientRects().length > 0) return el;
	}
	return null;
}

/**
 * Find the scrollable container that contains the element.
 * Prefers the nearest el-scrollbar wrap (custom scroll container), then falls
 * back to the nearest native scrollable ancestor (e.g. .el-drawer__body for
 * the condition editor drawer) so highlighted steps inside dialogs/drawers
 * actually scroll their content into view.
 */
function _findScrollContainer(el: Element): HTMLElement | null {
	let node = el.parentElement;
	while (node) {
		if (node.classList.contains('el-scrollbar__wrap')) return node;
		// Native scroll container: must actually allow vertical scrolling and
		// have overflowing content (otherwise scrolling it is a no-op).
		const overflowY = window.getComputedStyle(node).overflowY;
		if (
			(overflowY === 'auto' || overflowY === 'scroll' || overflowY === 'overlay') &&
			node.scrollHeight > node.clientHeight
		) {
			return node;
		}
		node = node.parentElement;
	}
	return null;
}

/**
 * Whether the step's target element is currently present and visible.
 * disableHighlight steps don't need a target at all.
 */
function _stepElementReady(step: TourStep): boolean {
	return !!step.disableHighlight || _findVisibleElement(step.element) !== null;
}

// ═══════════════════════════════════════════════════════════════════
// Hook
// ═══════════════════════════════════════════════════════════════════

export function useTourGuide(options: UseTourGuideOptions) {
	const { onComplete, onSkip, getScrollContainer, checkSeenRemote, markSeenRemote } = options;

	// Completion state is driven entirely by the backend — no localStorage cache.
	const isCompleted: Ref<boolean> = ref(false);
	const isRunning = ref(false);
	// In-memory flag: remote check already done this session — skip subsequent requests.
	let _remoteCheckDone = false;

	// Current Driver.js instance so we can destroy it on unmount / stage switch.
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	let activeDriver: any = null;
	// Restores scroll patching + frozen scrollers when the tour ends.
	let unlockScroll: (() => void) | null = null;
	// Cleanup for the current waitForUserClick listener (hoisted so stopTour can clear it).
	let _removeUserClickListenerOuter: (() => void) | null = null;

	async function _runTour(steps: TourStep[]): Promise<boolean> {
		// Never run two tours at the same time (e.g. auto-start racing replay).
		if (isRunning.value) return false;
		if (!steps || steps.length === 0) return false;

		isRunning.value = true;

		const [{ driver }] = await Promise.all([
			import('driver.js'),
			import('driver.js/dist/driver.css'),
		]);

		// Drop steps whose target is missing or not visible (collapsed/disabled).
		// disableHighlight steps only need a visible anchor to decide inclusion.
		//
		// lazyElement steps depend on a preceding waitForUserClick step — the
		// user opens the drawer/dropdown that reveals them. If that prerequisite
		// was itself filtered out (e.g. no condition node on the canvas), the
		// lazy step can never appear either, so it is dropped too; otherwise it
		// would inflate the step counter and end in an un-highlighted popover.
		const activeSteps: TourStep[] = [];
		let hasLiveWaitStep = false;
		for (const step of steps) {
			if (step.disableHighlight) {
				activeSteps.push(step);
				continue;
			}
			const visible = _findVisibleElement(step.element) !== null;
			if (step.lazyElement) {
				// Lazy steps appear after a preceding waitForUserClick step
				// opens the drawer/dropdown (some are themselves wait steps,
				// e.g. the Workflow Chart menu item). Keep them when already
				// visible or when that prerequisite is live.
				if (visible || hasLiveWaitStep) {
					activeSteps.push(step);
					if (step.waitForUserClick && visible) {
						hasLiveWaitStep = true;
					}
				}
			} else if (step.waitForUserClick) {
				if (visible) {
					activeSteps.push(step);
					hasLiveWaitStep = true;
				}
			} else if (visible) {
				activeSteps.push(step);
			}
		}

		if (activeSteps.length === 0) {
			console.warn('[useTourGuide] No tour elements found in DOM. Tour aborted.');
			isRunning.value = false;
			return false;
		}

		const total = activeSteps.length;

		// ═══ Scroll control ═══════════════════════════════════════════════
		// Root cause: Driver.js calls element.scrollIntoView() which walks up the
		// DOM tree and scrolls el-scrollbar containers, pushing the page layout.
		//
		// IMPORTANT: We must NOT set overflow:hidden on el-scrollbar__wrap because
		// Driver.js's SVG overlay uses getBoundingClientRect() of the highlighted
		// element. If the scrollbar container has overflow:hidden, the element's
		// visible rect gets clipped and the highlight box disappears.
		//
		// Strategy: patch scrollIntoView to no-op, freeze unrelated scrollers,
		// then manually scroll ONLY the target's own scroll container.

		const scrollY = window.scrollY;

		// Containers we are allowed to scroll = targets' own scroll ancestors.
		const scrollableContainers = new Set<HTMLElement>();
		activeSteps.forEach((step) => {
			if (step.disableHighlight) return;
			const el = _findVisibleElement(step.element);
			const container = el ? _findScrollContainer(el) : null;
			if (container) scrollableContainers.add(container);
		});

		// Freeze every OTHER el-scrollbar wrap so only the target's container moves.
		const frozenScrollers: Array<{
			el: HTMLElement;
			top: number;
			left: number;
			handler: () => void;
		}> = [];
		document.querySelectorAll<HTMLElement>('.el-scrollbar__wrap').forEach((el) => {
			if (scrollableContainers.has(el)) return;
			const top = el.scrollTop;
			const left = el.scrollLeft;
			const handler = () => {
				el.scrollTop = top;
				el.scrollLeft = left;
			};
			el.addEventListener('scroll', handler, { passive: true });
			frozenScrollers.push({ el, top, left, handler });
		});

		// Patch scrollIntoView — this is what Driver.js calls internally.
		const originalScrollIntoView = Element.prototype.scrollIntoView;
		// eslint-disable-next-line @typescript-eslint/no-empty-function
		Element.prototype.scrollIntoView = function () {};

		// Patch window scroll methods as a secondary safeguard.
		const origScrollTo = window.scrollTo.bind(window);
		const origScrollBy = window.scrollBy.bind(window);
		// eslint-disable-next-line @typescript-eslint/no-explicit-any
		(window as any).scrollTo = () => {};
		// eslint-disable-next-line @typescript-eslint/no-explicit-any
		(window as any).scrollBy = () => {};

		const _unlockScroll = () => {
			Element.prototype.scrollIntoView = originalScrollIntoView;
			// eslint-disable-next-line @typescript-eslint/no-explicit-any
			(window as any).scrollTo = origScrollTo;
			// eslint-disable-next-line @typescript-eslint/no-explicit-any
			(window as any).scrollBy = origScrollBy;
			frozenScrollers.forEach(({ el, handler, top, left }) => {
				el.removeEventListener('scroll', handler);
				el.scrollTop = top;
				el.scrollLeft = left;
			});
			origScrollTo(0, scrollY);
		};
		unlockScroll = _unlockScroll;

		// ═══ Helper: build dot indicators ══════════════════════════════════
		function buildDots(current: number, totalCount: number): string {
			return Array.from({ length: totalCount })
				.map((_, i) => {
					const active = i === current;
					return `<span class="ff-tour-dot ${
						active ? 'ff-tour-dot--active' : ''
					}"></span>`;
				})
				.join('');
		}

		// ═══ Helper: build popover description HTML ═════════════════════════
		// We inject the full footer (dots + nav + skip) inside the description
		// because Driver.js's built-in footer has limited customisation.
		function buildDescription(step: TourStep, index: number): string {
			const isFirst = index === 0;
			const isLast = index === total - 1;
			const dots = buildDots(index, total);
			// waitForUserClick: hide Next/Done, show a hint prompt instead
			const navHtml = step.waitForUserClick
				? `<span class="ff-tour-click-hint">👆 Click the highlighted button to continue</span>`
				: `<div class="ff-tour-nav">
							<button
								class="ff-tour-btn-prev"
								${isFirst ? 'disabled' : ''}
								data-action="prev"
							>←</button>
							<button
								class="ff-tour-btn-next"
								data-action="${isLast ? 'done' : 'next'}"
							>${isLast ? 'Done' : 'Next →'}</button>
						</div>`;

			return `
				<div class="ff-tour-body">${step.description}</div>
				<div class="ff-tour-footer">
					<div class="ff-tour-footer-row1">
						<div class="ff-tour-dots">${dots}</div>
						${navHtml}
					</div>
					<div class="ff-tour-footer-row2">
						<span class="ff-tour-counter">${index + 1} of ${total}</span>
						<button class="ff-tour-btn-skip" data-action="skip">Skip tour</button>
					</div>
				</div>
			`;
		}

		// eslint-disable-next-line @typescript-eslint/no-explicit-any
		let driverInstance: any = null;

		const _finish = () => {
			activeDriver = null;
			unlockScroll = null;
			isRunning.value = false;
			_unlockScroll();
			_removeUserClickListener?.();
			_removeUserClickListener = null;
			_removeUserClickListenerOuter = null;
			if (driverInstance?.isActive?.()) driverInstance.destroy();
		};

		// ═══ Drive ════════════════════════════════════════════════════════
		// Track current step index to invoke beforeHighlight callbacks
		// eslint-disable-next-line prefer-const
		let currentStepIdx = 0;
		// Cleanup handle for the current waitForUserClick listener
		let _removeUserClickListener: (() => void) | null = null;

		/**
		 * Advance to the next step whose target element is actually in the DOM.
		 * Missing-element steps are skipped so we never show an un-highlighted
		 * popover. If no steps remain, the tour finishes quietly.
		 */
		function _advanceFrom(fromIndex: number): void {
			let nextIdx = fromIndex;
			while (nextIdx < activeSteps.length && !_stepElementReady(activeSteps[nextIdx])) {
				nextIdx++;
			}
			if (nextIdx >= activeSteps.length) {
				markCompleted();
				onComplete?.();
				_finish();
				return;
			}
			currentStepIdx = nextIdx;
			driverInstance?.moveTo(nextIdx);
		}

		/**
		 * Poll briefly for the step at `index`'s target to appear in the DOM.
		 * Used after a waitForUserClick step opens a dialog/drawer so lazy
		 * steps aren't skipped just because their content is still mounting.
		 */
		function _waitForStepElement(index: number, timeout = 600): Promise<void> {
			const step = activeSteps[index];
			if (!step || _stepElementReady(step)) return Promise.resolve();
			const deadline = Date.now() + timeout;
			return new Promise<void>((resolve) => {
				const check = () => {
					if (_stepElementReady(step) || Date.now() >= deadline) resolve();
					else requestAnimationFrame(check);
				};
				check();
			});
		}

		/**
		 * Go back to the previous step whose target element is in the DOM.
		 * Skips missing-element steps; stays on the current step if none exist.
		 */
		function _goBackFrom(fromIndex: number): void {
			let prevIdx = fromIndex;
			while (prevIdx >= 0 && !_stepElementReady(activeSteps[prevIdx])) {
				prevIdx--;
			}
			if (prevIdx < 0) return;
			currentStepIdx = prevIdx;
			driverInstance?.moveTo(prevIdx);
		}

		/**
		 * Watch a lazyElement step's target — if it disappears from the DOM
		 * (e.g. the dropdown is closed by an overlay click) automatically fall
		 * back to the nearest preceding non-lazy step so the user can re-open it.
		 * Returns a cleanup function that stops watching.
		 */
		function _watchElementDisappear(targetEl: Element, stepIndex: number): () => void {
			let stopped = false;
			const step = activeSteps[stepIndex];
			// Only apply to lazyElement steps — normal steps' targets are always present.
			if (!step?.lazyElement) return () => {};

			// Find the nearest preceding step whose element is always in DOM
			// (i.e. not lazyElement), so we can fall back to it.
			let fallbackIdx = stepIndex - 1;
			while (fallbackIdx >= 0 && activeSteps[fallbackIdx]?.lazyElement) {
				fallbackIdx--;
			}
			if (fallbackIdx < 0) return () => {};

			const check = () => {
				if (stopped) return;
				// If the target is gone and tour is still on this step, fall back.
				if (!document.contains(targetEl) || _findVisibleElement(step.element) === null) {
					stopped = true;
					_removeUserClickListener?.();
					_removeUserClickListener = null;
					_removeUserClickListenerOuter = null;
					// Small delay so the dropdown close animation finishes first.
					setTimeout(() => {
						if (currentStepIdx === stepIndex) {
							_goBackFrom(fallbackIdx);
						}
					}, 80);
					return;
				}
				requestAnimationFrame(check);
			};
			requestAnimationFrame(check);
			return () => {
				stopped = true;
			};
		}

		/**
		 * Set up a one-shot click listener on the highlighted element.
		 * When the user clicks it, advance to the next step (or finish if last).
		 */
		function _attachUserClickListener(targetEl: Element, stepIndex: number) {
			// Remove any previously registered listener first
			_removeUserClickListener?.();
			_removeUserClickListener = null;
			_removeUserClickListenerOuter = null;

			// For lazy steps (e.g. dropdown menu items): watch for the element
			// disappearing due to an overlay/outside click, and fall back automatically.
			const stopWatching = _watchElementDisappear(targetEl, stepIndex);

			const handler = async (e: Event) => {
				// Don't intercept clicks on the popover itself
				const popover = document.querySelector('.ff-tour-popover');
				if (popover?.contains(e.target as Node)) return;

				stopWatching();

				// Clean up immediately so the real click fires through
				_removeUserClickListener?.();
				_removeUserClickListener = null;
				_removeUserClickListenerOuter = null;

				// Immediately hide the tour overlay + popover so the user doesn't
				// see the tour hovering while we wait for async work (dialog open/close).
				const popoverEl = document.querySelector<HTMLElement>('.driver-popover');
				const overlayEl = document.querySelector<SVGElement>('.driver-overlay');
				const stageEl = document.querySelector<SVGElement>(
					'.driver-stage-wrap, #driver-highlighted-element-stage'
				);
				if (popoverEl) popoverEl.style.visibility = 'hidden';
				if (overlayEl) (overlayEl as unknown as HTMLElement).style.opacity = '0';
				if (stageEl) (stageEl as unknown as HTMLElement).style.opacity = '0';

				// Run afterUserClick hook if defined
				const step = activeSteps[stepIndex];
				if (step?.afterUserClick) {
					await step.afterUserClick();
				}

				// Restore visibility before moveNext re-renders the popover
				if (popoverEl) popoverEl.style.visibility = '';
				if (overlayEl) (overlayEl as unknown as HTMLElement).style.opacity = '';
				if (stageEl) (stageEl as unknown as HTMLElement).style.opacity = '';

				// Advance or finish
				const isLast = stepIndex === activeSteps.length - 1;
				if (isLast) {
					markCompleted();
					onComplete?.();
					_finish();
				} else {
					// Give the next step's target a moment to mount after the
					// dialog/drawer finishes opening, then advance (skipping
					// any steps whose targets never appear).
					await _waitForStepElement(stepIndex + 1);
					_advanceFrom(stepIndex + 1);
				}
			};

			// Use capture phase so we react before other handlers (e.g. Element Plus dialog)
			targetEl.addEventListener('click', handler, { capture: false });
			const cleanup = () => {
				stopWatching();
				targetEl.removeEventListener('click', handler, { capture: false });
			};
			_removeUserClickListener = cleanup;
			_removeUserClickListenerOuter = cleanup;
		}

		// Detect dark mode — the app adds `dark` class on <html>
		const isDark = document.documentElement.classList.contains('dark');

		driverInstance = driver({
			showProgress: false, // we render our own progress
			animate: true,
			overlayOpacity: isDark ? 0.75 : 0.4,
			// Disable Driver.js built-in scroll — the content area uses
			// el-scrollbar (a custom scroll container), not window. We handle
			// scrolling manually in onHighlightStarted below.
			smoothScroll: false,
			allowClose: false, // close only via our Skip button
			overlayColor: '#000',
			stagePadding: 4,
			stageRadius: 8,
			popoverClass: 'ff-tour-popover',
			nextBtnText: '',
			prevBtnText: '',
			doneBtnText: '',
			// Scroll the target element into view within ITS OWN scroll
			// container (left content column or right progress column).
			// Also invoke beforeHighlight if the current step defines one.
			onHighlightStarted: async (highlightedEl: Element | undefined) => {
				// Remove any leftover click listener from the previous step
				_removeUserClickListener?.();
				_removeUserClickListener = null;

				// Call beforeHighlight for the current step (if defined)
				const currentStep = activeSteps[currentStepIdx];
				if (currentStep?.beforeHighlight) {
					const waitFor = await currentStep.beforeHighlight();
					if (waitFor && typeof waitFor === 'string') {
						await new Promise<void>((resolve) => {
							const deadline = Date.now() + 2000;
							const check = () => {
								if (document.querySelector(waitFor)) {
									resolve();
								} else if (Date.now() < deadline) {
									requestAnimationFrame(check);
								} else {
									resolve();
								}
							};
							check();
						});
					}
				}

				if (!highlightedEl) {
					// Target not in the DOM — driver.js would otherwise show a
					// centered popover with no highlight. Skip ahead instead.
					// Defer one frame so driver finishes rendering the dummy
					// overlay before we re-drive to the next available step.
					await new Promise<void>((resolve) => requestAnimationFrame(() => resolve()));
					_advanceFrom(currentStepIdx + 1);
					return;
				}

				const container =
					_findScrollContainer(highlightedEl) ??
					(getScrollContainer ? getScrollContainer() : null);

				if (container) {
					const containerRect = container.getBoundingClientRect();
					const elRect = highlightedEl.getBoundingClientRect();

					// Already fully visible — no need to scroll.
					const fullyVisible =
						elRect.top >= containerRect.top && elRect.bottom <= containerRect.bottom;
					if (!fullyVisible) {
						// Bring the element fully into view with a comfortable margin.
						// For elements taller than the container, align the top instead.
						const relativeTop = elRect.top - containerRect.top + container.scrollTop;
						const relativeBottom =
							elRect.bottom - containerRect.top + container.scrollTop;
						let target = Math.max(0, relativeTop - 80);
						if (elRect.height <= containerRect.height) {
							const bottomAligned = relativeBottom - containerRect.height + 80;
							target = Math.min(target, Math.max(0, bottomAligned));
						}
						container.scrollTop = target;
					}
				}

				// waitForUserClick: attach a click listener so the user must
				// physically click the highlighted element to advance.
				if (currentStep?.waitForUserClick) {
					// Wait one frame so Driver.js finishes rendering the overlay
					// before we attach the listener.
					await new Promise<void>((resolve) => requestAnimationFrame(() => resolve()));
					_attachUserClickListener(highlightedEl, currentStepIdx);
				}
			},
			// eslint-disable-next-line @typescript-eslint/no-explicit-any
			onPopoverRender: (popoverEl: any) => {
				// popoverEl is Driver.js's PopoverDOM — access the wrapper HTMLElement.
				const el: HTMLElement = popoverEl.wrapper ?? popoverEl;

				// Remove any blue border on Driver.js SVG stage elements
				const svgStage = document.querySelector<SVGElement>(
					'#driver-highlighted-element-stage, .driver-stage'
				);
				if (svgStage) {
					svgStage.style.fill = 'transparent';
					svgStage.style.stroke = 'transparent';
				}

				// Hide Driver.js default navigation footer (we render our own).
				const nav = el.querySelector(
					'.driver-popover-navigation-btns'
				) as HTMLElement | null;
				if (nav) nav.style.display = 'none';
				const progress = el.querySelector(
					'.driver-popover-progress-text'
				) as HTMLElement | null;
				if (progress) progress.style.display = 'none';
				const footer = el.querySelector('.driver-popover-footer') as HTMLElement | null;
				if (footer) footer.style.display = 'none';

				// Scroll the active dot into view within the dots container
				// so it is always visible even when there are many steps.
				requestAnimationFrame(() => {
					const dotsContainer = el.querySelector<HTMLElement>('.ff-tour-dots');
					const activeDot = el.querySelector<HTMLElement>('.ff-tour-dot--active');
					if (dotsContainer && activeDot) {
						const containerLeft = dotsContainer.getBoundingClientRect().left;
						const dotLeft = activeDot.getBoundingClientRect().left;
						const offset = dotLeft - containerLeft;
						const center =
							offset - dotsContainer.offsetWidth / 2 + activeDot.offsetWidth / 2;
						dotsContainer.scrollLeft += center;
					}
				});

				// Bind click events on our custom buttons.
				el.addEventListener('click', (e: MouseEvent) => {
					const target = (e.target as HTMLElement).closest(
						'[data-action]'
					) as HTMLElement | null;
					if (!target) return;
					const action = target.dataset.action;

					if (action === 'next') {
						_advanceFrom(currentStepIdx + 1);
					} else if (action === 'prev') {
						_goBackFrom(currentStepIdx - 1);
					} else if (action === 'done') {
						markCompleted();
						onComplete?.();
						_finish();
					} else if (action === 'skip') {
						markCompleted();
						onSkip?.();
						_finish();
					}
				});
			},
			steps: activeSteps.map((step, index) => {
				const stepConfig: Record<string, unknown> = {
					popover: {
						title: step.title ?? '',
						description: buildDescription(step, index),
						side: step.side ?? 'bottom',
						align: step.align ?? 'start',
					},
				};
				if (!step.disableHighlight) {
					stepConfig.element = step.element;
				}
				return stepConfig;
			}),
		});

		activeDriver = driverInstance;

		// Start from the first step whose target is actually present — steps
		// whose targets never appear (e.g. lazyElement steps on a workflow
		// without conditions) must not show an un-highlighted popover.
		const startIndex = activeSteps.findIndex((step) => _stepElementReady(step));
		if (startIndex === -1) {
			_finish();
			return false;
		}
		currentStepIdx = startIndex;
		driverInstance.drive(startIndex);
		return true;
	}

	async function startTour(steps: TourStep[]): Promise<boolean> {
		// Seen state is determined solely by the backend.
		if (checkSeenRemote) {
			// In-memory fast-path: already confirmed seen this session.
			if (isCompleted.value) return false;
			// Remote check (once per session).
			if (!_remoteCheckDone) {
				try {
					const seen = await checkSeenRemote();
					if (seen) {
						isCompleted.value = true;
						return false;
					}
				} catch {
					// The seen check failed (network / HTTP error). Never auto-start the tour
					// in that case: it would re-trigger on every page load while the endpoint
					// is unavailable. The "?" FAB replay is still available.
					isCompleted.value = true;
					return false;
				}
				_remoteCheckDone = true;
			}
			return _runTour(steps);
		}

		// No remote check configured — run unconditionally (useful for forced replays).
		return _runTour(steps);
	}

	async function replayTour(steps: TourStep[]): Promise<boolean> {
		return _runTour(steps);
	}

	/** Immediately stop a running tour and restore scroll behavior. */
	function stopTour(): void {
		_removeUserClickListenerOuter?.();
		_removeUserClickListenerOuter = null;
		if (activeDriver?.isActive?.()) {
			activeDriver.destroy();
		}
		activeDriver = null;
		unlockScroll?.();
		unlockScroll = null;
		isRunning.value = false;
	}

	function markCompleted(): void {
		isCompleted.value = true;

		// Persist to backend so the record survives browser/device changes.
		if (markSeenRemote) {
			markSeenRemote().catch(() => {
				// Swallow errors — tour marking is non-critical
			});
		}
	}

	function resetCompleted(): void {
		isCompleted.value = false;
		// Reset the session flag so the remote check fires again on next startTour call.
		_remoteCheckDone = false;
	}

	return {
		isCompleted,
		isRunning,
		startTour,
		replayTour,
		stopTour,
		markCompleted,
		resetCompleted,
	};
}
