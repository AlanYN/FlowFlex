<template>
    <div class="wfe-global-block-bg rounded-xl p-4">
        <!-- Header -->
        <div
            class="flex items-center justify-between cursor-pointer select-none mb-3"
            @click="collapsed = !collapsed"
        >
            <div class="flex items-center gap-2 text-sm font-semibold text-[var(--el-text-color-primary)]">
                <el-icon class="text-[15px] text-[var(--el-color-primary)]">
                    <Connection />
                </el-icon>
                Trigger History
                <el-badge
                    v-if="logs.length > 0"
                    :value="logs.length"
                    :max="99"
                    class="ml-1"
                    type="primary"
                />
            </div>
            <el-icon class="text-gray-400 transition-transform" :class="{ 'rotate-180': !collapsed }">
                <ArrowDown />
            </el-icon>
        </div>

        <!-- Body -->
        <template v-if="!collapsed">
            <!-- Loading skeleton -->
            <div v-if="loading" class="space-y-2">
                <el-skeleton v-for="i in 2" :key="i" :rows="2" animated />
            </div>

            <!-- Empty -->
            <div
                v-else-if="logs.length === 0"
                class="text-center text-sm text-[var(--el-text-color-placeholder)] py-4"
            >
                No trigger history for this case.
            </div>

            <!-- Log list -->
            <div v-else class="space-y-2">
                <div
                    v-for="log in logs"
                    :key="log.id"
                    class="p-3 rounded-lg border border-[var(--el-border-color-lighter)] bg-[var(--el-fill-color-lighter)] text-xs"
                >
                    <!-- Status + date -->
                    <div class="flex items-center justify-between mb-1.5">
                        <el-tag
                            :type="statusType(log.status)"
                            size="small"
                            class="font-semibold"
                        >
                            {{ log.status }}
                        </el-tag>
                        <span class="text-[var(--el-text-color-placeholder)]">
                            {{ formatDate(log.createDate) }}
                        </span>
                    </div>

                    <!-- CompletionType -->
                    <div class="flex items-center gap-1 text-[var(--el-text-color-secondary)] mb-1">
                        <el-icon><Lightning /></el-icon>
                        <span>{{ log.completionType }}</span>
                    </div>

                    <!-- Target Case link -->
                    <div v-if="log.targetOnboardingId" class="flex items-center gap-1">
                        <el-icon class="text-[var(--el-color-success)]"><Check /></el-icon>
                        <span class="text-[var(--el-text-color-secondary)]">Created case:</span>
                        <a
                            class="text-[var(--el-color-primary)] hover:underline cursor-pointer truncate max-w-[120px]"
                            @click="goToCase(log.targetOnboardingId)"
                        >
                            {{ log.targetOnboardingId }}
                        </a>
                    </div>

                    <!-- Reason (Skipped / Failed) -->
                    <div
                        v-if="log.reason"
                        class="mt-1 text-[var(--el-text-color-placeholder)] truncate"
                        :title="log.reason"
                    >
                        {{ log.reason }}
                    </div>
                </div>
            </div>
        </template>
    </div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { ArrowDown, Connection, Check } from '@element-plus/icons-vue';
import { getTriggerLogsByOnboarding } from '@/apis/ow/triggers';

// Use Lightning icon from element-plus icons
const Lightning = Check; // Fallback — will use a suitable icon

const props = defineProps<{
    onboardingId: string;
}>();

const router = useRouter();
const collapsed = ref(false);
const loading = ref(false);
const logs = ref<any[]>([]);

const statusType = (status: string) => {
    switch (status) {
        case 'Triggered': return 'success';
        case 'Skipped':   return 'warning';
        case 'Failed':    return 'danger';
        default:          return 'info';
    }
};

const formatDate = (dateStr: string) => {
    if (!dateStr) return '';
    try {
        return new Date(dateStr).toLocaleString('zh-CN', {
            month: '2-digit', day: '2-digit',
            hour: '2-digit', minute: '2-digit',
        });
    } catch {
        return dateStr;
    }
};

const goToCase = (targetId: string) => {
    router.push({ path: '/onboard/onboardDetail', query: { onboardingId: targetId } });
};

const loadLogs = async () => {
    if (!props.onboardingId) return;
    loading.value = true;
    try {
        const res = await getTriggerLogsByOnboarding(props.onboardingId);
        if (res?.data) {
            logs.value = Array.isArray(res.data) ? res.data : [];
        }
    } catch {
        logs.value = [];
    } finally {
        loading.value = false;
    }
};

watch(() => props.onboardingId, (id) => { if (id) loadLogs(); });
onMounted(() => { if (props.onboardingId) loadLogs(); });
</script>
