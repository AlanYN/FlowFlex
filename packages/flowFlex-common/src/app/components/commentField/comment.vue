<template>
	<div class="mention-wrapper">
		<div class="textarea-container">
			<textarea
				ref="textareaRef"
				v-model="textContent"
				@input="handleInput"
				@keydown="handleKeydown"
				@blur="handleBlur"
				@focus="handleFocus"
				@click="handleClick"
				:placeholder="props.placeholder"
				rows="4"
				class="mention-textarea w-full"
				:disabled="props.disabled"
			></textarea>

			<Teleport to="body">
				<div
					v-if="showSuggestions"
					ref="dropdownRef"
					class="suggestions-list"
					:style="{
						position: 'absolute',
						top: `${dropdownPosition.top + 20}px`,
						left: `${dropdownPosition.left}px`,
						width: '200px',
					}"
				>
					<div
						v-for="(user, index) in filteredSuggestions"
						:key="user.key"
						class="suggestion-item"
						:class="{ 'suggestion-item--selected': index === selectedIndex }"
						@click="insertMention(user.value)"
						@mousedown="(e) => handleSuggestionSelect(e, user.value)"
					>
						@{{ user.value }}
					</div>
				</div>
			</Teleport>
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, defineExpose } from 'vue';

interface Props {
	options: any;
	placeholder: string;
	value: string;
	disabled?: boolean;
}

interface User {
	key: string;
	value: string;
}

const props = defineProps<Props>();
const emits = defineEmits(['onInput']);
const textContent = ref(props.value);
const showSuggestions = ref(false);
const cursorPosition = ref(0);
const suggestionList = ref<User[]>(props.options);
const filteredSuggestions = ref<User[]>([]);

// Add these new refs
const dropdownPosition = ref({ top: 0, left: 0 });
const textareaRef = ref<HTMLTextAreaElement | null>(null);

// Add a new ref for the selected index
const selectedIndex = ref(-1);

// Add a new ref for the dropdown element
const dropdownRef = ref<HTMLDivElement | null>(null);

// Add a flag to track if we're selecting an item
const isSelectingItem = ref(false);

// Add a function to calculate dropdown position
const calculateDropdownPosition = (
	textareaRect: DOMRect,
	coords: { left: number; top: number }
) => {
	const dropdownWidth = 200; // Width of dropdown
	const viewportWidth = window.innerWidth;
	const dropdownRight = textareaRect.left + coords.left + dropdownWidth;

	// If dropdown would overflow viewport
	if (dropdownRight > viewportWidth) {
		return {
			top: textareaRect.top + coords.top - textareaRef.value!.scrollTop,
			left: textareaRect.left + coords.left - dropdownWidth,
		};
	}

	// Default position
	return {
		top: textareaRect.top + coords.top - textareaRef.value!.scrollTop,
		left: textareaRect.left + coords.left,
	};
};

const handleInput = (event: Event) => {
	const textarea = event.target as HTMLTextAreaElement;
	const newChar = textarea.value[textarea.selectionStart - 1];
	textContent.value = textarea.value;
	emits('onInput', textarea.value);
	cursorPosition.value = textarea.selectionStart;

	if (newChar === '@') {
		filteredSuggestions.value = suggestionList.value;
		showSuggestions.value = true;
		selectedIndex.value = -1;

		// Calculate caret position and set dropdown position
		if (textareaRef.value) {
			const coords = getCaretCoordinates(textareaRef.value, cursorPosition.value);
			const textareaRect = textareaRef.value.getBoundingClientRect();
			dropdownPosition.value = calculateDropdownPosition(textareaRect, coords);
		}
	} else if (showSuggestions.value) {
		const textBeforeCursor = textContent.value.slice(0, cursorPosition.value);
		const lastAtSymbol = textBeforeCursor.lastIndexOf('@');

		if (lastAtSymbol !== -1) {
			const searchTerm = textBeforeCursor.slice(lastAtSymbol + 1).toLowerCase();
			filteredSuggestions.value = suggestionList.value.filter((user) =>
				user.value.toLowerCase().includes(searchTerm)
			);

			// Hide suggestions if search term includes a space
			if (searchTerm.includes(' ') || filteredSuggestions.value.length === 0) {
				showSuggestions.value = false;
				selectedIndex.value = -1;
			}
		} else {
			showSuggestions.value = false;
			selectedIndex.value = -1;
		}
	}
};

// Add this helper function to calculate caret position
const getCaretCoordinates = (element: HTMLTextAreaElement, position: number) => {
	const div = document.createElement('div');
	const styles = getComputedStyle(element);
	const properties = [
		'boxSizing',
		'width',
		'height',
		'overflowX',
		'overflowY',
		'borderTopWidth',
		'borderRightWidth',
		'borderBottomWidth',
		'borderLeftWidth',
		'paddingTop',
		'paddingRight',
		'paddingBottom',
		'paddingLeft',
		'fontStyle',
		'fontVariant',
		'fontWeight',
		'fontStretch',
		'fontSize',
		'fontSizeAdjust',
		'lineHeight',
		'fontFamily',
		'textAlign',
		'textTransform',
		'textIndent',
		'textDecoration',
		'letterSpacing',
		'wordSpacing',
	];

	div.style.position = 'absolute';
	div.style.top = '0';
	div.style.left = '0';
	div.style.visibility = 'hidden';
	div.style.whiteSpace = 'pre-wrap';
	div.style.wordWrap = 'break-word';

	properties.forEach((prop) => {
		const value = styles.getPropertyValue(prop);
		if (value) {
			div.style.setProperty(prop, value);
		}
	});

	const content = element.value.substring(0, position);
	div.textContent = content;

	const span = document.createElement('span');
	span.textContent = element.value.substring(position) || '.';
	div.appendChild(span);

	document.body.appendChild(div);
	const { offsetLeft: spanLeft, offsetTop: spanTop } = span;
	document.body.removeChild(div);

	return {
		left: spanLeft,
		top: spanTop,
	};
};

const insertMention = (username: string) => {
	isSelectingItem.value = true;
	const textBeforeCursor = textContent.value.slice(0, cursorPosition.value);
	const lastAtSymbol = textBeforeCursor.lastIndexOf('@');
	const textAfterCursor = textContent.value.slice(cursorPosition.value);
	console.info(username);

	textContent.value =
		textBeforeCursor.slice(0, lastAtSymbol) + '@' + username + ' ' + textAfterCursor;
	emits(
		'onInput',
		textBeforeCursor.slice(0, lastAtSymbol) + '@' + username + ' ' + textAfterCursor
	);

	showSuggestions.value = false;
	selectedIndex.value = -1;

	// Reset the flag after a short delay
	setTimeout(() => {
		isSelectingItem.value = false;
	}, 10);
};

// Add keyboard navigation handler
const handleKeydown = (event: KeyboardEvent) => {
	if (!showSuggestions.value) return;

	switch (event.key) {
		case 'ArrowDown':
			event.preventDefault();
			selectedIndex.value = Math.min(
				selectedIndex.value + 1,
				filteredSuggestions.value.length - 1
			);
			break;

		case 'ArrowUp':
			event.preventDefault();
			selectedIndex.value = Math.max(selectedIndex.value - 1, 0);
			break;

		case 'Enter':
			event.preventDefault();
			if (selectedIndex.value >= 0) {
				insertMention(filteredSuggestions.value[selectedIndex.value].value);
			}
			break;

		case 'Escape':
			showSuggestions.value = false;
			selectedIndex.value = -1;
			break;
	}
};

// Add blur handler
const handleBlur = (event: FocusEvent) => {
	// Don't hide if we're selecting an item
	if (isSelectingItem.value) {
		return;
	}

	// Check if the click is inside the dropdown
	if (dropdownRef.value?.contains(event.relatedTarget as Node)) {
		return;
	}

	// Small delay to allow click events on suggestions to fire
	// setTimeout(() => {
	// 	showSuggestions.value = false;
	// 	selectedIndex.value = -1;
	// }, 100);
	showSuggestions.value = false;
	selectedIndex.value = -1;
};

const handleClearContent = () => {
	textContent.value = '';
};

// Add a handler for mousedown
const handleSuggestionSelect = (event: MouseEvent, username: string) => {
	event.preventDefault(); // Prevent blur from firing
	insertMention(username);
};

// Add a function to check for @ and show suggestions
const checkForMentionTrigger = () => {
	if (!textareaRef.value) return;

	const cursorPos = textareaRef.value.selectionStart;
	const textBeforeCursor = textContent.value.slice(0, cursorPos);
	const lastAtSymbol = textBeforeCursor.lastIndexOf('@');

	if (lastAtSymbol !== -1) {
		const textAfterAt = textBeforeCursor.slice(lastAtSymbol + 1);
		if (!textAfterAt.includes(' ')) {
			filteredSuggestions.value = suggestionList.value.filter((user) =>
				user.value.toLowerCase().includes(textAfterAt.toLowerCase())
			);
			showSuggestions.value = true;
			selectedIndex.value = -1;

			// Update dropdown position with viewport-relative coordinates
			const coords = getCaretCoordinates(textareaRef.value, cursorPos);
			const textareaRect = textareaRef.value.getBoundingClientRect();

			dropdownPosition.value = {
				top: textareaRect.top + coords.top - textareaRef.value.scrollTop,
				left: textareaRect.left + coords.left,
			};
		}
	}
};

// Add focus and click handlers
const handleFocus = () => {
	checkForMentionTrigger();
};

const handleClick = () => {
	checkForMentionTrigger();
};

defineExpose({ handleClearContent });
</script>

<style lang="scss" scoped>
.mention-wrapper {
	position: relative;
	width: 100%;
}

.textarea-container {
	position: relative;
	width: 100%;
}

.mention-textarea {
	width: 100%;
	padding: 8px;
	border: 1px solid #ccc;
	font-size: 14px;
	resize: none;
	line-height: 1.5;
	@apply rounded-xl;

	&:focus {
		outline: none;
	}
}

.suggestions-list {
	position: fixed;
	background-color: var(--black-400);
	border: 1px solid var(--black-400);
	box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
	max-height: 200px;
	overflow-y: auto;
	z-index: 99999;
	@apply rounded-xl;
}

.suggestion-item {
	padding: 8px;
	cursor: pointer;
	background-color: var(--black-400);
}

.suggestion-item:hover {
	opacity: 0.5;
}

.suggestion-item--selected {
	opacity: 0.5;
}
</style>
