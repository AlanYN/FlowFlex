<template>
    <el-dialog
        v-model="visible"
        :width="680"
        destroy-on-close
        class="whats-new-detail-dialog"
        @open="onDialogOpen"
    >
        <!-- Dialog header (override default title slot) -->
        <template #header>
            <div class="detail-header">
                <!-- Category Tag + Date row -->
                <div class="flex items-center gap-2 mb-3">
                    <el-tag
                        v-if="props.item"
                        :type="getCategoryTagType(props.item.category)"
                        :style="props.item.category === 'Announcement'
                            ? { '--el-tag-text-color': '#722ED1', '--el-tag-border-color': '#d3adf7', '--el-tag-bg-color': '#f9f0ff' }
                            : {}"
                        size="small"
                        effect="light"
                    >
                        {{ getCategoryLabel(props.item.category) }}
                    </el-tag>
                    <span class="text-xs text-gray-400 dark:text-gray-500">
                        {{ publishDateFormatted }}
                    </span>
                </div>
                <!-- Title -->
                <div class="text-xl font-bold text-gray-900 dark:text-gray-100 leading-snug">
                    {{ props.item?.title ?? '' }}
                </div>
            </div>
        </template>

        <!-- Body -->
        <div class="detail-body">
            <!-- Loading skeleton -->
            <div v-if="loading" class="py-4">
                <el-skeleton :rows="6" animated />
            </div>

            <!-- Rich text content -->
            <div
                v-else
                class="whats-new-rich-content"
                v-html="sanitizedContent"
            />
        </div>
    </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, inject } from 'vue';
import type { Ref } from 'vue';
import DOMPurify from 'dompurify';
import dayjs from 'dayjs';
import type { WhatsNewPanelItem, WhatsNewDetail } from '#/whatsNew';
import { getDetail, markRead } from '@/apis/whatsNew';

// ========================= Props =========================

interface Props {
    item: WhatsNewPanelItem | null;
}
const props = withDefaults(defineProps<Props>(), { item: null });

// ========================= Inject (WhatsNewBell state) =========================

interface WhatsNewStateContext {
    unreadCount: Ref<number>;
    panelItems: Ref<WhatsNewPanelItem[]>;
    selectedItem: Ref<WhatsNewPanelItem | null>;
    decrementUnread: () => void;
    clearAllUnread: () => void;
    markItemAsRead: (id: string) => void;
}
const state = inject<WhatsNewStateContext>('whatsNewState');

// ========================= Internal State =========================

const visible = ref<boolean>(false);
const detail = ref<WhatsNewDetail | null>(null);
const loading = ref<boolean>(false);

// ========================= Exposed API =========================

const open = () => {
    visible.value = true;
};
defineExpose({ open });

// ========================= Computed =========================

const publishDateFormatted = computed(() => {
    const publishTime = detail.value?.publishTime ?? props.item?.publishTime;
    if (!publishTime) return '';
    return dayjs(publishTime).format('MMM D, YYYY');
});

const sanitizedContent = computed(() => {
    if (!detail.value?.content) return '';
    return DOMPurify.sanitize(detail.value.content, {
        ALLOWED_TAGS: [
            'p', 'br', 'strong', 'em', 'u', 's',
            'h1', 'h2', 'h3', 'h4', 'h5', 'h6',
            'ul', 'ol', 'li', 'blockquote', 'pre', 'code',
            'a', 'img', 'span', 'div',
            'table', 'thead', 'tbody', 'tr', 'th', 'td',
        ],
        ALLOWED_ATTR: ['href', 'src', 'alt', 'class', 'style', 'target', 'rel'],
        FORCE_BODY: true,
    });
});

// ========================= Dialog Open Handler =========================

const onDialogOpen = async () => {
    if (!props.item) return;

    loading.value = true;
    const wasUnread = !props.item.isRead;

    try {
        detail.value = await getDetail(props.item.id);

        // Mark read silently — failure must not affect dialog display (Req 3.7)
        try {
            await markRead(props.item.id);
            if (wasUnread) {
                state?.markItemAsRead(props.item.id);
                state?.decrementUnread();
            }
        } catch {
            // silent — per requirement 3.7
        }
    } catch (e) {
        console.warn('[WhatsNewDetail] Failed to load detail', e);
    } finally {
        loading.value = false;
    }
};

// ========================= Category Helpers =========================

type CategoryType = 'NewFeature' | 'Improvement' | 'BugFix' | 'Announcement';

const CATEGORY_TAG_TYPE: Record<CategoryType, '' | 'primary' | 'success' | 'warning' | 'danger' | 'info'> = {
    NewFeature: 'primary',
    Improvement: 'warning',
    BugFix: 'danger',
    Announcement: '',
};

const CATEGORY_LABEL: Record<CategoryType, string> = {
    NewFeature: 'New Feature',
    Improvement: 'Improvement',
    BugFix: 'Bug Fix',
    Announcement: 'Announcement',
};

const getCategoryTagType = (category: string) => {
    return CATEGORY_TAG_TYPE[category as CategoryType] ?? '';
};

const getCategoryLabel = (category: string) => {
    return CATEGORY_LABEL[category as CategoryType] ?? category;
};
</script>

<style scoped lang="scss">
.detail-header {
    padding-right: 24px; // avoid overlap with built-in close button
}

/* Prose-like styles for rendered rich text */
.whats-new-rich-content {
    max-height: 60vh;
    overflow-y: auto;
    font-size: 14px;
    line-height: 1.7;
    color: var(--el-text-color-primary);

    :deep(h1),
    :deep(h2),
    :deep(h3),
    :deep(h4),
    :deep(h5),
    :deep(h6) {
        font-weight: 600;
        margin-top: 1.2em;
        margin-bottom: 0.5em;
        color: var(--el-text-color-primary);
    }

    :deep(h1) { font-size: 1.5em; }
    :deep(h2) { font-size: 1.25em; }
    :deep(h3) { font-size: 1.1em; }

    :deep(p) {
        margin-bottom: 0.75em;
    }

    :deep(ul),
    :deep(ol) {
        padding-left: 1.5em;
        margin-bottom: 0.75em;
    }

    :deep(li) {
        margin-bottom: 0.25em;
    }

    :deep(blockquote) {
        border-left: 3px solid var(--el-border-color);
        padding-left: 1em;
        margin: 0.75em 0;
        color: var(--el-text-color-secondary);
    }

    :deep(pre) {
        background-color: var(--el-fill-color-light);
        border-radius: 4px;
        padding: 0.75em 1em;
        overflow-x: auto;
        margin-bottom: 0.75em;
    }

    :deep(code) {
        background-color: var(--el-fill-color-light);
        border-radius: 3px;
        padding: 0.1em 0.3em;
        font-size: 0.9em;
        font-family: 'Menlo', 'Monaco', 'Consolas', monospace;
    }

    :deep(pre code) {
        background: none;
        padding: 0;
    }

    :deep(a) {
        color: var(--el-color-primary);
        text-decoration: underline;

        &:hover {
            opacity: 0.8;
        }
    }

    :deep(img) {
        max-width: 100%;
        border-radius: 4px;
        margin: 0.5em 0;
    }

    :deep(table) {
        width: 100%;
        border-collapse: collapse;
        margin-bottom: 0.75em;
        font-size: 0.95em;
    }

    :deep(th),
    :deep(td) {
        border: 1px solid var(--el-border-color);
        padding: 6px 10px;
        text-align: left;
    }

    :deep(th) {
        background-color: var(--el-fill-color-light);
        font-weight: 600;
    }

    :deep(strong) { font-weight: 600; }
    :deep(em) { font-style: italic; }
    :deep(u) { text-decoration: underline; }
    :deep(s) { text-decoration: line-through; }
}
</style>
