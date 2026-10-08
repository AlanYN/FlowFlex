<template>
    <el-dialog
        v-model="dialogVisible"
        title="Case Stage Permission"
        width="720px"
        :close-on-click-modal="!saving"
        append-to-body
        destroy-on-close
        @close="handleClose"
    >
        <!-- Header: title + stage name -->
        <template #header>
            <div>
                <div class="text-base font-bold">Case Stage Permission</div>
                <div class="text-sm text-gray-500 mt-0.5">{{ stageName }}</div>
            </div>
        </template>

        <div class="space-y-4">
            <!-- Info banner -->
            <div class="rounded-lg border border-gray-200 bg-gray-50 px-4 py-3 text-sm text-gray-500 leading-relaxed">
                This can only narrow down who has access at this stage — it can never grant more access than the case or the workflow stage already allow.
            </div>

            <!-- Top-level inherit checkbox -->
            <el-checkbox v-model="formData.inheritFromWorkflow" class="!font-medium">
                Use same permission as workflow stage runtime
            </el-checkbox>

            <!-- ======== INHERIT MODE: read-only display ======== -->
            <template v-if="formData.inheritFromWorkflow">
                <div class="grid grid-cols-2 gap-4">
                    <!-- View Permission (read-only) -->
                    <div class="space-y-2">
                        <div>
                            <div class="text-sm font-bold text-gray-800">View Permission</div>
                            <div class="text-xs text-gray-400 mt-0.5">Controls who can view the case at this stage</div>
                        </div>
                        <div class="rounded-lg border border-gray-200 bg-gray-50 p-3 space-y-1 min-h-[72px]">
                            <div class="flex items-center gap-1 text-xs font-semibold text-gray-500 uppercase tracking-wide">
                                <span class="i-mdi-account-group text-gray-400" />
                                EFFECTIVE TEAMS
                            </div>
                            <div v-if="inheritViewDisplay.length" class="flex flex-wrap gap-1">
                                <el-tag v-for="name in inheritViewDisplay" :key="name" type="info" size="small">{{ name }}</el-tag>
                            </div>
                            <div v-else class="text-sm text-gray-400">{{ inheritViewEmptyLabel }}</div>
                        </div>
                    </div>

                    <!-- Operate Permission (read-only) -->
                    <div class="space-y-2">
                        <div>
                            <div class="text-sm font-bold text-gray-800">Operate Permission</div>
                            <div class="text-xs text-gray-400 mt-0.5">Controls who can operate on the case at this stage</div>
                        </div>
                        <div class="rounded-lg border border-gray-200 bg-gray-50 p-3 space-y-1 min-h-[72px]">
                            <div class="flex items-center gap-1 text-xs font-semibold text-gray-500 uppercase tracking-wide">
                                <span class="i-mdi-account-group text-gray-400" />
                                EFFECTIVE TEAMS
                            </div>
                            <div v-if="inheritOperateDisplay.length" class="flex flex-wrap gap-1">
                                <el-tag v-for="name in inheritOperateDisplay" :key="name" type="info" size="small">{{ name }}</el-tag>
                            </div>
                            <div v-else class="text-sm text-gray-400">{{ inheritOperateEmptyLabel }}</div>
                        </div>
                    </div>
                </div>

                <!-- Roll Back (read-only, full width) -->
                <div class="border-t border-gray-100 pt-4 space-y-2">
                    <div>
                        <div class="flex items-center gap-1.5 text-sm font-bold text-gray-800">
                            <Icon icon="mdi:restore" class="text-base" />
                            Roll Back Permission
                        </div>
                        <div class="text-xs text-gray-400 mt-0.5">Controls who can reopen this stage and edit it again</div>
                    </div>
                    <div class="rounded-lg border border-gray-200 bg-gray-50 p-3 space-y-1 min-h-[60px]">
                        <div class="flex items-center gap-1 text-xs font-semibold text-gray-500 uppercase tracking-wide">
                            <span class="i-mdi-account-group text-gray-400" />
                            EFFECTIVE TEAMS
                        </div>
                        <div v-if="inheritRollBackDisplay.length" class="flex flex-wrap gap-1">
                            <el-tag v-for="name in inheritRollBackDisplay" :key="name" type="info" size="small">{{ name }}</el-tag>
                        </div>
                        <div v-else class="text-sm text-gray-400">No access</div>
                    </div>
                </div>
            </template>

            <!-- ======== INDEPENDENT MODE ======== -->
            <template v-else>
                <!-- View + Operate side by side -->
                <div class="grid grid-cols-2 gap-4">
                    <!-- ── View Permission ── -->
                    <div class="space-y-3">
                        <div>
                            <div class="text-sm font-bold text-gray-800">View Permission</div>
                            <div class="text-xs text-gray-400 mt-0.5">Controls who can view the case at this stage</div>
                        </div>

                        <el-select
                            v-model="formData.viewPermissionMode"
                            class="w-full"
                            placeholder="Select permission type"
                            @change="handleViewModeChange"
                        >
                            <el-option
                                v-for="opt in viewPermissionOptions"
                                :key="opt.value"
                                :label="opt.label"
                                :value="opt.value"
                            />
                        </el-select>

                        <template v-if="showViewSelector">
                            <div class="text-xs text-gray-500">
                                <template v-if="formData.viewPermissionMode === ViewPermissionModeEnum.VisibleTo">
                                    Only the teams or people you pick below can view it.
                                </template>
                                <template v-else>
                                    Everyone except the teams or people you pick below can view it.
                                </template>
                            </div>

                            <div class="text-xs font-medium text-gray-600">
                                {{ formData.viewPermissionSubjectType === PermissionSubjectTypeEnum.Team ? 'Team' : 'User' }}
                            </div>

                            <el-radio-group v-model="formData.viewPermissionSubjectType" @change="handleViewSubjectTypeChange" class="!gap-2">
                                <el-radio-button :value="PermissionSubjectTypeEnum.Team">User Teams</el-radio-button>
                                <el-radio-button :value="PermissionSubjectTypeEnum.User">Individual Users</el-radio-button>
                            </el-radio-group>

                            <FlowflexUserSelector
                                v-show="formData.viewPermissionSubjectType === PermissionSubjectTypeEnum.Team"
                                v-model="formData.viewTeams"
                                selection-type="team"
                                :clearable="true"
                                :choosable-tree-data="viewChoosableTreeData"
                                @change="handleViewSelectionChange"
                            />
                            <FlowflexUserSelector
                                v-show="formData.viewPermissionSubjectType === PermissionSubjectTypeEnum.User"
                                v-model="formData.viewUsers"
                                selection-type="user"
                                :clearable="true"
                                :choosable-tree-data="viewChoosableTreeData"
                                @change="handleViewSelectionChange"
                            />
                            <p v-if="viewValidationError" class="text-sm text-red-500">{{ viewValidationError }}</p>
                        </template>

                        <!-- Effective teams preview -->
                        <div class="rounded-lg border border-gray-200 bg-gray-50 p-3 space-y-1 min-h-[60px]">
                            <div class="text-xs font-semibold text-gray-500 uppercase tracking-wide">EFFECTIVE TEAMS</div>
                            <div v-if="effectiveViewDisplay.length" class="flex flex-wrap gap-1 mt-1">
                                <el-tag v-for="name in effectiveViewDisplay" :key="name" type="info" size="small">{{ name }}</el-tag>
                            </div>
                            <div v-else class="text-sm text-gray-400">No access</div>
                        </div>
                    </div>

                    <!-- ── Operate Permission ── -->
                    <div class="space-y-3">
                        <div>
                            <div class="text-sm font-bold text-gray-800">Operate Permission</div>
                            <div class="text-xs text-gray-400 mt-0.5">Controls who can operate on the case at this stage</div>
                        </div>

                        <el-checkbox v-model="formData.useSameTeamForOperate" class="!font-medium text-primary">
                            Use same teams and users that have view permission
                        </el-checkbox>

                        <template v-if="formData.useSameTeamForOperate">
                            <div class="rounded-lg border border-gray-200 bg-gray-50 p-3 space-y-1 min-h-[60px]">
                                <div class="text-sm text-gray-500">Same people as View permission.</div>
                                <div class="text-xs font-semibold text-gray-500 uppercase tracking-wide mt-1">EFFECTIVE TEAMS</div>
                                <div v-if="effectiveOperateDisplay.length" class="flex flex-wrap gap-1 mt-1">
                                    <el-tag v-for="name in effectiveOperateDisplay" :key="name" type="info" size="small">{{ name }}</el-tag>
                                </div>
                                <div v-else class="text-sm text-gray-400">No access</div>
                            </div>
                        </template>

                        <template v-else>
                            <el-radio-group v-model="formData.operatePermissionSubjectType" class="!gap-2">
                                <el-radio-button :value="PermissionSubjectTypeEnum.Team">User Teams</el-radio-button>
                                <el-radio-button :value="PermissionSubjectTypeEnum.User">Individual Users</el-radio-button>
                            </el-radio-group>

                            <FlowflexUserSelector
                                v-show="formData.operatePermissionSubjectType === PermissionSubjectTypeEnum.Team"
                                v-model="formData.operateTeams"
                                selection-type="team"
                                :clearable="true"
                                :choosable-tree-data="operateChoosableTreeData"
                            />
                            <FlowflexUserSelector
                                v-show="formData.operatePermissionSubjectType === PermissionSubjectTypeEnum.User"
                                v-model="formData.operateUsers"
                                selection-type="user"
                                :clearable="true"
                                :choosable-tree-data="operateChoosableTreeData"
                            />

                            <div class="rounded-lg border border-gray-200 bg-gray-50 p-3 space-y-1 min-h-[60px]">
                                <div class="text-xs font-semibold text-gray-500 uppercase tracking-wide">EFFECTIVE TEAMS</div>
                                <div v-if="effectiveOperateDisplay.length" class="flex flex-wrap gap-1 mt-1">
                                    <el-tag v-for="name in effectiveOperateDisplay" :key="name" type="info" size="small">{{ name }}</el-tag>
                                </div>
                                <div v-else class="text-sm text-gray-400">No access</div>
                            </div>
                        </template>
                    </div>
                </div>

                <!-- ── Roll Back Permission (full width, no card border) ── -->
                <div class="border-t border-gray-100 pt-4 space-y-3">
                    <div>
                        <div class="flex items-center gap-1.5 text-sm font-bold text-gray-800">
                            <Icon icon="mdi:restore" class="text-base" />
                            Roll Back Permission
                        </div>
                        <div class="text-xs text-gray-400 mt-0.5">Controls who can reopen this stage and edit it again</div>
                    </div>

                    <el-checkbox v-model="formData.rollBackUseSameAsOperate" class="!font-medium text-primary">
                        Use same teams and users that have operate permission
                    </el-checkbox>

                    <template v-if="!formData.rollBackUseSameAsOperate">
                        <div class="text-xs font-medium text-gray-600">Teams</div>

                        <el-radio-group v-model="formData.rollBackPermissionSubjectType" class="!gap-2">
                            <el-radio-button :value="PermissionSubjectTypeEnum.Team">User Teams</el-radio-button>
                            <el-radio-button :value="PermissionSubjectTypeEnum.User">Individual Users</el-radio-button>
                        </el-radio-group>

                        <FlowflexUserSelector
                            v-show="formData.rollBackPermissionSubjectType === PermissionSubjectTypeEnum.Team"
                            v-model="formData.rollBackTeams"
                            selection-type="team"
                            :clearable="true"
                            :choosable-tree-data="rollBackChoosableTreeData"
                        />
                        <FlowflexUserSelector
                            v-show="formData.rollBackPermissionSubjectType === PermissionSubjectTypeEnum.User"
                            v-model="formData.rollBackUsers"
                            selection-type="user"
                            :clearable="true"
                            :choosable-tree-data="rollBackChoosableTreeData"
                        />
                    </template>

                    <!-- Effective teams preview -->
                    <div class="rounded-lg border border-gray-200 bg-gray-50 p-3 space-y-1 min-h-[60px]">
                        <div class="text-xs font-semibold text-gray-500 uppercase tracking-wide">
                            {{ formData.rollBackPermissionSubjectType === PermissionSubjectTypeEnum.User ? 'EFFECTIVE USERS' : 'EFFECTIVE TEAMS' }}
                        </div>
                        <div v-if="effectiveRollBackDisplay.length" class="flex flex-wrap gap-1 mt-1">
                            <el-tag v-for="name in effectiveRollBackDisplay" :key="name" type="info" size="small">{{ name }}</el-tag>
                        </div>
                        <div v-else class="text-sm text-gray-400">No access</div>
                    </div>
                </div>
            </template>
        </div>

        <template #footer>
            <el-button @click="handleClose" :disabled="saving">Cancel</el-button>
            <el-button type="primary" :loading="saving" @click="handleSave">Save Permission</el-button>
        </template>
    </el-dialog>
</template>

<script setup lang="ts">
import { ref, computed, reactive, watch, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
import FlowflexUserSelector from '@/components/form/flowflexUser/index.vue';
import { ViewPermissionModeEnum, PermissionSubjectTypeEnum } from '@/enums/permissionEnum';
import { menuRoles } from '@/stores/modules/menuFunction';
import { updateCaseStagePermission } from '@/apis/ow/onboarding';
import type { FlowflexUser } from '#/golbal';

// ── Props ──────────────────────────────────────────────────────────────
interface Props {
    visible: boolean;
    caseId: string | number;
    stageId: string | number;
    stageName?: string;
    stagePermissionData?: any; // full stage progress object with all MaxStage* and stage* fields
}

const props = withDefaults(defineProps<Props>(), {
    stageName: '',
    stagePermissionData: () => ({}),
});

const emit = defineEmits<{
    'update:visible': [value: boolean];
    'saved': [];
}>();

// ── Store ──────────────────────────────────────────────────────────────
const menuStore = menuRoles();

// ── Dialog visibility ──────────────────────────────────────────────────
const dialogVisible = computed({
    get: () => props.visible,
    set: (v) => emit('update:visible', v),
});

// ── Saving state ───────────────────────────────────────────────────────
const saving = ref(false);

// ── View permission options (NO Private) ──────────────────────────────
const viewPermissionOptions = [
    { label: 'Public', value: ViewPermissionModeEnum.Public },
    { label: 'Visible to', value: ViewPermissionModeEnum.VisibleTo },
    { label: 'Invisible to', value: ViewPermissionModeEnum.InvisibleTo },
];

// ── Form data ──────────────────────────────────────────────────────────
const formData = reactive({
    inheritFromWorkflow: true,
    viewPermissionMode: ViewPermissionModeEnum.Public as number,
    viewPermissionSubjectType: PermissionSubjectTypeEnum.Team as number,
    viewTeams: [] as string[],
    viewUsers: [] as string[],
    useSameTeamForOperate: true,
    operatePermissionSubjectType: PermissionSubjectTypeEnum.Team as number,
    operateTeams: [] as string[],
    operateUsers: [] as string[],
    rollBackUseSameAsOperate: true,
    rollBackPermissionSubjectType: PermissionSubjectTypeEnum.Team as number,
    rollBackTeams: [] as string[],
    rollBackUsers: [] as string[],
});

// ── Validation ─────────────────────────────────────────────────────────
const viewValidationError = ref('');

// ── Tree data for choosable ranges ────────────────────────────────────
const viewChoosableTreeData = ref<FlowflexUser[] | undefined>(undefined);
const operateChoosableTreeData = ref<FlowflexUser[] | undefined>(undefined);
const rollBackChoosableTreeData = ref<FlowflexUser[] | undefined>(undefined);

// ── Helper: resolve team names from store cache ───────────────────────
const resolveTeamNames = (ids: string[]): string[] => {
    if (!ids || ids.length === 0) return [];
    const cache: FlowflexUser[] = (menuStore as any).flowflexUserData || [];
    const nameMap = new Map<string, string>();
    const walk = (nodes: FlowflexUser[]) => {
        nodes.forEach((n) => {
            nameMap.set(n.id, n.name || n.id);
            if (n.children) walk(n.children);
        });
    };
    walk(cache);
    return ids.map((id) => nameMap.get(id) || id);
};

// ── Helper: build filtered tree by ID set ─────────────────────────────
const buildFilteredTree = (fullTree: FlowflexUser[], allowedIds: string[]): FlowflexUser[] => {
    if (!allowedIds || allowedIds.length === 0) return [];
    const idSet = new Set<string>(allowedIds);

    const filterNodes = (nodes: FlowflexUser[]): FlowflexUser[] => {
        const result: FlowflexUser[] = [];
        nodes.forEach((node) => {
            if (node.type === 'team' && idSet.has(node.id)) {
                const cloned: FlowflexUser = { ...node };
                if (cloned.children && cloned.children.length > 0) {
                    cloned.children = filterNodes(cloned.children);
                }
                result.push(cloned);
            } else if (node.type === 'team' && node.children && node.children.length > 0) {
                const filteredChildren = filterNodes(node.children);
                if (filteredChildren.length > 0) {
                    result.push({ ...node, children: filteredChildren });
                }
            }
        });
        return result;
    };

    return filterNodes(fullTree);
};

// ── Inherit mode: effective team display ──────────────────────────────
const inheritViewDisplay = computed(() => {
    const d = props.stagePermissionData;
    if (!d) return [];
    const mode = d.maxStageViewPermissionMode ?? d.stageViewPermissionMode;
    if (mode === ViewPermissionModeEnum.Public || mode === null || mode === undefined) return [];
    return resolveTeamNames(d.maxStageViewTeams || []);
});

const inheritViewEmptyLabel = computed(() => {
    const d = props.stagePermissionData;
    const mode = d?.maxStageViewPermissionMode ?? d?.stageViewPermissionMode;
    if (mode === ViewPermissionModeEnum.Public || mode === null || mode === undefined) {
        return 'All teams (Public)';
    }
    return 'None configured';
});

const inheritOperateDisplay = computed(() => {
    const d = props.stagePermissionData;
    if (!d) return [];
    const mode = d.maxStageOperatePermissionMode ?? d.stageOperatePermissionMode;
    if (mode === ViewPermissionModeEnum.Public || mode === null || mode === undefined) return [];
    return resolveTeamNames(d.maxStageOperateTeams || []);
});

const inheritOperateEmptyLabel = computed(() => {
    const d = props.stagePermissionData;
    const mode = d?.maxStageOperatePermissionMode ?? d?.stageOperatePermissionMode;
    if (mode === ViewPermissionModeEnum.Public || mode === null || mode === undefined) {
        return 'All teams (Public)';
    }
    return 'None configured';
});

const inheritRollBackDisplay = computed(() => {
    const d = props.stagePermissionData;
    if (!d) return [];
    return resolveTeamNames(d.maxStageRollBackTeams || []);
});

// ── Show view selector ─────────────────────────────────────────────────
const showViewSelector = computed(() => {
    return (
        formData.viewPermissionMode === ViewPermissionModeEnum.VisibleTo ||
        formData.viewPermissionMode === ViewPermissionModeEnum.InvisibleTo
    );
});

// ── Initialize form data from stagePermissionData prop ────────────────
const initFormData = () => {
    const d = props.stagePermissionData || {};

    // inheritFromWorkflow: null → treat as true
    formData.inheritFromWorkflow = d.stagePermissionInheritFromWorkflowStage !== false;
    formData.viewPermissionMode = d.stageViewPermissionMode ?? ViewPermissionModeEnum.Public;
    formData.viewPermissionSubjectType = d.stageViewPermissionSubjectType ?? PermissionSubjectTypeEnum.Team;
    formData.viewTeams = [...(d.stageViewTeams || [])];
    formData.viewUsers = [...(d.stageViewUsers || [])];
    formData.useSameTeamForOperate = d.stageUseSameTeamForOperate !== false;
    formData.operatePermissionSubjectType = d.stageOperatePermissionSubjectType ?? PermissionSubjectTypeEnum.Team;
    formData.operateTeams = [...(d.stageOperateTeams || [])];
    formData.operateUsers = [...(d.stageOperateUsers || [])];
    formData.rollBackUseSameAsOperate = d.stageRollBackUseSameAsOperate !== false;
    formData.rollBackPermissionSubjectType = d.stageRollBackPermissionSubjectType ?? PermissionSubjectTypeEnum.Team;
    formData.rollBackTeams = [...(d.stageRollBackTeams || [])];
    formData.rollBackUsers = [...(d.stageRollBackUsers || [])];

    viewValidationError.value = '';
};

// ── Effective teams computed (independent mode) ───────────────────────

// View effective: the teams/users currently selected in view config
const effectiveViewDisplay = computed((): string[] => {
    if (formData.viewPermissionSubjectType === PermissionSubjectTypeEnum.User) {
        return formData.viewUsers.length ? ['(individual users)'] : [];
    }
    return resolveTeamNames(formData.viewTeams);
});

// Operate effective: same as view (if useSameTeamForOperate) or independent operateTeams
const effectiveOperateDisplay = computed((): string[] => {
    if (formData.useSameTeamForOperate) {
        return effectiveViewDisplay.value;
    }
    if (formData.operatePermissionSubjectType === PermissionSubjectTypeEnum.User) {
        return formData.operateUsers.length ? ['(individual users)'] : [];
    }
    return resolveTeamNames(formData.operateTeams);
});

// Roll Back effective
const effectiveRollBackDisplay = computed((): string[] => {
    if (formData.rollBackUseSameAsOperate) {
        return effectiveOperateDisplay.value;
    }
    if (formData.rollBackPermissionSubjectType === PermissionSubjectTypeEnum.User) {
        return formData.rollBackUsers.length ? ['(individual users)'] : [];
    }
    return resolveTeamNames(formData.rollBackTeams);
});

// ── Choosable tree building ────────────────────────────────────────────

// View choosable: based on MaxStageViewTeams (first-layer snapshot constraint)
const buildViewChoosableTree = async () => {
    const d = props.stagePermissionData || {};
    const maxViewTeams: string[] = d.maxStageViewTeams || [];

    if (maxViewTeams.length === 0) {
        // No constraint — allow all
        viewChoosableTreeData.value = undefined;
        return;
    }

    try {
        const fullTree = await menuStore.getFlowflexUserDataWithCache('');
        const treeArr = Array.isArray(fullTree) ? fullTree : [];
        viewChoosableTreeData.value = buildFilteredTree(treeArr, maxViewTeams);
    } catch {
        viewChoosableTreeData.value = undefined;
    }
};

// Operate choosable: MaxStageOperateTeams ∩ Case Stage View Effective Teams
const buildOperateChoosableTree = async () => {
    const d = props.stagePermissionData || {};
    const maxOperateTeams: string[] = d.maxStageOperateTeams || [];

    // Compute current "Case Stage View Effective Teams"
    let viewEffectiveIds: string[] = [];
    if (formData.viewPermissionMode === ViewPermissionModeEnum.Public) {
        // Public: all teams allowed, intersect with maxOperateTeams only
        viewEffectiveIds = maxOperateTeams;
    } else if (formData.viewPermissionMode === ViewPermissionModeEnum.VisibleTo) {
        if (formData.viewPermissionSubjectType === PermissionSubjectTypeEnum.Team) {
            viewEffectiveIds = formData.viewTeams;
        } else {
            // User-based: no team restriction for operate
            viewEffectiveIds = maxOperateTeams;
        }
    } else {
        // InvisibleTo: no simple intersection — use maxOperateTeams as fallback
        viewEffectiveIds = maxOperateTeams;
    }

    // Intersection of maxOperateTeams and viewEffectiveIds
    const intersectionIds = maxOperateTeams.length > 0
        ? viewEffectiveIds.filter((id) => maxOperateTeams.includes(id))
        : viewEffectiveIds;

    if (intersectionIds.length === 0 && maxOperateTeams.length > 0) {
        operateChoosableTreeData.value = [];
        return;
    }

    const limitIds = intersectionIds.length > 0 ? intersectionIds : (maxOperateTeams.length > 0 ? maxOperateTeams : undefined);

    if (!limitIds) {
        operateChoosableTreeData.value = undefined;
        return;
    }

    try {
        const fullTree = await menuStore.getFlowflexUserDataWithCache('');
        const treeArr = Array.isArray(fullTree) ? fullTree : [];
        operateChoosableTreeData.value = buildFilteredTree(treeArr, limitIds);
    } catch {
        operateChoosableTreeData.value = undefined;
    }
};

// Roll Back choosable: effective Case Stage Operate Teams
const buildRollBackChoosableTree = async () => {
    // Effective operate = useSameTeamForOperate → same as view effective; else operateTeams
    let effectiveOperateIds: string[];
    if (formData.useSameTeamForOperate) {
        // Same as view effective
        if (formData.viewPermissionMode === ViewPermissionModeEnum.Public) {
            // All → no restriction
            rollBackChoosableTreeData.value = undefined;
            return;
        } else if (formData.viewPermissionMode === ViewPermissionModeEnum.VisibleTo) {
            effectiveOperateIds = formData.viewPermissionSubjectType === PermissionSubjectTypeEnum.Team
                ? [...formData.viewTeams]
                : [];
        } else {
            effectiveOperateIds = [];
        }
    } else {
        effectiveOperateIds = formData.operatePermissionSubjectType === PermissionSubjectTypeEnum.Team
            ? [...formData.operateTeams]
            : [];
    }

    if (effectiveOperateIds.length === 0) {
        // No operate teams configured — Roll Back choosable range is also empty
        rollBackChoosableTreeData.value = [];
        return;
    }

    try {
        const fullTree = await menuStore.getFlowflexUserDataWithCache('');
        const treeArr = Array.isArray(fullTree) ? fullTree : [];
        rollBackChoosableTreeData.value = buildFilteredTree(treeArr, effectiveOperateIds);
    } catch {
        rollBackChoosableTreeData.value = [];
    }
};

// ── Event handlers ────────────────────────────────────────────────────
const handleViewModeChange = () => {
    viewValidationError.value = '';
    if (!showViewSelector.value) {
        formData.viewTeams = [];
        formData.viewUsers = [];
    }
    buildOperateChoosableTree();
    buildRollBackChoosableTree();
};

const handleViewSubjectTypeChange = () => {
    viewValidationError.value = '';
    buildOperateChoosableTree();
    buildRollBackChoosableTree();
};

const handleViewSelectionChange = () => {
    viewValidationError.value = '';
    buildOperateChoosableTree();
    buildRollBackChoosableTree();
};

// ── Watchers ──────────────────────────────────────────────────────────
watch(
    () => formData.useSameTeamForOperate,
    () => buildRollBackChoosableTree()
);

watch(
    () => [formData.operateTeams, formData.operateUsers],
    () => buildRollBackChoosableTree(),
    { deep: true }
);

watch(
    () => props.stagePermissionData,
    () => {
        initFormData();
        buildViewChoosableTree();
        buildOperateChoosableTree();
        buildRollBackChoosableTree();
    },
    { deep: true }
);

// ── Validate ──────────────────────────────────────────────────────────
const validate = (): boolean => {
    if (!formData.inheritFromWorkflow) {
        if (formData.viewPermissionMode === ViewPermissionModeEnum.VisibleTo) {
            const hasTeams = formData.viewPermissionSubjectType === PermissionSubjectTypeEnum.Team && formData.viewTeams.length > 0;
            const hasUsers = formData.viewPermissionSubjectType === PermissionSubjectTypeEnum.User && formData.viewUsers.length > 0;
            if (!hasTeams && !hasUsers) {
                viewValidationError.value = 'Please select at least one team or user for View permission.';
                return false;
            }
        }
    }
    viewValidationError.value = '';
    return true;
};

// ── Save ──────────────────────────────────────────────────────────────
const handleSave = async () => {
    if (!validate()) return;

    saving.value = true;
    try {
        const payload: Record<string, any> = {
            inheritFromWorkflowStage: formData.inheritFromWorkflow,
        };

        if (!formData.inheritFromWorkflow) {
            payload.viewPermissionMode = formData.viewPermissionMode;
            payload.viewPermissionSubjectType = formData.viewPermissionSubjectType;
            payload.viewTeams = formData.viewPermissionSubjectType === PermissionSubjectTypeEnum.Team ? formData.viewTeams : [];
            payload.viewUsers = formData.viewPermissionSubjectType === PermissionSubjectTypeEnum.User ? formData.viewUsers : [];
            payload.useSameTeamForOperate = formData.useSameTeamForOperate;

            if (!formData.useSameTeamForOperate) {
                payload.operatePermissionSubjectType = formData.operatePermissionSubjectType;
                payload.operateTeams = formData.operatePermissionSubjectType === PermissionSubjectTypeEnum.Team ? formData.operateTeams : [];
                payload.operateUsers = formData.operatePermissionSubjectType === PermissionSubjectTypeEnum.User ? formData.operateUsers : [];
            }

            payload.rollBackInherit = formData.inheritFromWorkflow; // Roll Back inherits together with the top-level flag
            payload.rollBackUseSameAsOperate = formData.rollBackUseSameAsOperate;

            if (!formData.rollBackUseSameAsOperate) {
                payload.rollBackPermissionSubjectType = formData.rollBackPermissionSubjectType;
                payload.rollBackTeams = formData.rollBackPermissionSubjectType === PermissionSubjectTypeEnum.Team ? formData.rollBackTeams : [];
                payload.rollBackUsers = formData.rollBackPermissionSubjectType === PermissionSubjectTypeEnum.User ? formData.rollBackUsers : [];
            }
        }

        await updateCaseStagePermission(props.caseId, props.stageId, payload);
        ElMessage.success('Stage permission saved successfully.');
        emit('saved');
        dialogVisible.value = false;
    } catch (err: any) {
        ElMessage.error(err?.message || 'Failed to save stage permission.');
    } finally {
        saving.value = false;
    }
};

// ── Close ──────────────────────────────────────────────────────────────
const handleClose = () => {
    if (saving.value) return;
    dialogVisible.value = false;
};

// ── Mount ──────────────────────────────────────────────────────────────
onMounted(async () => {
    // Pre-warm the user data cache so resolveTeamNames works synchronously
    await menuStore.getFlowflexUserDataWithCache('');
    initFormData();
    await buildViewChoosableTree();
    await buildOperateChoosableTree();
    await buildRollBackChoosableTree();
});
</script>

<style scoped lang="scss">
:deep(.el-checkbox__label) {
    white-space: normal;
    line-height: 1.5;
}
:deep(.el-radio__label) {
    white-space: normal;
    line-height: 1.5;
}
</style>
