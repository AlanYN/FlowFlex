<template>
	<div class="dashboard">
		<PageHeader title="Dashboard" description="Dashboard">
			<template #actions>
				<el-button
					type="primary"
					size="default"
					class="page-header-btn page-header-btn-primary"
				>
					View All Cases
				</el-button>
			</template>
		</PageHeader>

		<!-- Row 1: 4 Stats Cards -->
		<div class="row-stats">
			<template v-if="statisticsLoading">
				<el-skeleton v-for="i in 4" :key="i" animated class="stats-skeleton flex flex-col">
					<template #template>
						<el-skeleton-item variant="text" style="width: 60%; height: 16px" />
						<el-skeleton-item
							variant="text"
							style="width: 40%; height: 32px; margin-top: 12px"
						/>
					</template>
				</el-skeleton>
			</template>
			<template v-else-if="statistics">
				<StatsCard
					title="Active Cases"
					:value="statistics.activeCases.value"
					:trend="statistics.activeCases.difference"
				/>
				<StatsCard
					title="Completed This Month"
					:value="statistics.completedThisMonth.value"
					:trend="statistics.completedThisMonth.difference"
				/>
				<StatsCard
					title="Overdue Tasks"
					:value="statistics.overdueTasks.value"
					:trend="statistics.overdueTasks.difference"
				/>
				<StatsCard
					title="Avg. Completion Time"
					:value="statistics.avgCompletionTime.value"
					:trend="statistics.avgCompletionTime.difference"
					trend-suffix="days"
					value-suffix=" days"
				/>
			</template>
			<template v-else>
				<div class="stats-empty">
					<span>No statistics data available</span>
				</div>
			</template>
		</div>

		<!-- Row 2: To-Do List | Message Center -->
		<div class="row-two-equal">
			<TodoList :items="tasks" :loading="tasksLoading" @refresh="fetchTasks" />
			<MessageCenter :messages="messages" :loading="messagesLoading" />
		</div>

		<!-- Row 3: Employee Stats | Department Distribution | Recent Hires -->
		<!-- <div class="row-three-cols">
			<EmployeeStats :stats="employeeStats" :loading="employeeLoading" />
			<DepartmentDistribution :departments="departments" :loading="employeeLoading" />
			<RecentHires :recent-hires="recentHires" :loading="employeeLoading" />
		</div> -->

		<!-- Row 4: Cases Overview | Recent Achievements -->
		<div class="row-one-two">
			<CasesOverview :data="casesOverview" :loading="casesLoading" />
			<RecentAchievements :achievements="achievements" :loading="achievementsLoading" />
		</div>

		<!-- Row 5: Upcoming Deadlines (full width) -->
		<div class="row-full">
			<UpcomingDeadlines :deadlines="deadlines" :loading="deadlinesLoading" />
		</div>
	</div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import PageHeader from '@/components/global/PageHeader/index.vue';
import StatsCard from './components/StatsCard.vue';
import TodoList from './components/TodoList.vue';
import MessageCenter from './components/MessageCenter.vue';
// import EmployeeStats from './components/EmployeeStats.vue';
// import DepartmentDistribution from './components/DepartmentDistribution.vue';
// import RecentHires from './components/RecentHires.vue';
import CasesOverview from './components/CasesOverview.vue';
import RecentAchievements from './components/RecentAchievements.vue';
import UpcomingDeadlines from './components/UpcomingDeadlines.vue';
import {
	getStatistics,
	getTasks,
	getMessages,
	getCasesOverview,
	getAchievements,
	getDeadlines,
} from '@/apis/dashboard';
import type {
	IDashboardStatistics,
	IDashboardTask,
	IDashboardMessage,
	ICasesOverview,
	IAchievement,
	IDeadline,
	// IEmployeeStats,
	// IDepartmentDistribution,
	// IRecentHire,
} from '#/dashboard';

// Statistics
const statistics = ref<IDashboardStatistics | null>(null);
const statisticsLoading = ref(false);

// Tasks
const tasks = ref<IDashboardTask[]>([]);
const tasksLoading = ref(false);

// Messages
const messages = ref<IDashboardMessage[]>([]);
const messagesLoading = ref(false);

// Employee (Mock data - no API yet)
// const employeeStats = ref<IEmployeeStats>({
// 	total: 12,
// 	active: 10,
// 	onLeave: 1,
// 	avgSalary: 93000,
// 	departmentCount: 10,
// });
// const departments = ref<IDepartmentDistribution[]>([
// 	{ name: 'Engineering', count: 2 },
// 	{ name: 'Sales', count: 2 },
// 	{ name: 'Marketing', count: 1 },
// 	{ name: 'Operations', count: 1 },
// 	{ name: 'Finance', count: 1 },
// 	{ name: 'Human Resources', count: 1 },
// ]);
// const recentHires = ref<IRecentHire[]>([]);
// const employeeLoading = ref(false);

// Cases Overview
const casesOverview = ref<ICasesOverview | null>(null);
const casesLoading = ref(false);

// Achievements
const achievements = ref<IAchievement[]>([]);
const achievementsLoading = ref(false);

// Deadlines
const deadlines = ref<IDeadline[]>([]);
const deadlinesLoading = ref(false);

// Fetch functions
async function fetchStatistics() {
	statisticsLoading.value = true;
	try {
		const res = await getStatistics();
		if (res.code === '200') {
			statistics.value = res.data;
		}
	} finally {
		statisticsLoading.value = false;
	}
}

async function fetchTasks() {
	tasksLoading.value = true;
	try {
		const res = await getTasks({ pageSize: 10 });
		if (res.code === '200') {
			tasks.value = res.data.items;
		}
	} finally {
		tasksLoading.value = false;
	}
}

async function fetchMessages() {
	messagesLoading.value = true;
	try {
		const res = await getMessages({ limit: 5 });
		if (res.code === '200') {
			messages.value = res.data.messages;
		}
	} finally {
		messagesLoading.value = false;
	}
}

async function fetchCasesOverview() {
	casesLoading.value = true;
	try {
		const res = await getCasesOverview();
		if (res.code === '200') {
			casesOverview.value = res.data;
		}
	} finally {
		casesLoading.value = false;
	}
}

async function fetchAchievements() {
	achievementsLoading.value = true;
	try {
		const res = await getAchievements({ limit: 5 });
		if (res.code === '200') {
			achievements.value = res.data;
		}
	} finally {
		achievementsLoading.value = false;
	}
}

async function fetchDeadlines() {
	deadlinesLoading.value = true;
	try {
		const res = await getDeadlines({ days: 7 });
		if (res.code === '200') {
			deadlines.value = res.data;
		}
	} finally {
		deadlinesLoading.value = false;
	}
}

onMounted(() => {
	// 并行加载所有模块数据
	fetchStatistics();
	fetchTasks();
	fetchMessages();
	fetchCasesOverview();
	fetchAchievements();
	fetchDeadlines();
});
</script>

<style scoped lang="scss">
.dashboard {
	@apply p-0;
}

/* Row 1: 4 Stats Cards */
.row-stats {
	@apply grid gap-2 mb-2;
	grid-template-columns: repeat(4, 1fr);

	@media (max-width: 1279px) {
		grid-template-columns: repeat(2, 1fr);
	}

	@media (max-width: 639px) {
		grid-template-columns: 1fr;
	}
}

.stats-skeleton {
	@apply rounded-xl p-5;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	min-height: 120px;
}

.stats-empty {
	@apply rounded-xl p-8 text-center col-span-4;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	color: var(--el-text-color-secondary);
}

/* Row 2: TodoList spans 2 columns, MessageCenter 1 column */
.row-two-equal {
	@apply grid gap-2 mb-2;
	grid-template-columns: repeat(3, 1fr);

	> *:first-child {
		grid-column: span 2;
	}

	@media (max-width: 1023px) {
		grid-template-columns: 1fr;

		> *:first-child {
			grid-column: span 1;
		}
	}
}

/* Row 3: Three equal columns */
.row-three-cols {
	@apply grid gap-2 mb-2;
	grid-template-columns: repeat(3, 1fr);

	@media (max-width: 1023px) {
		grid-template-columns: repeat(2, 1fr);

		> *:last-child {
			grid-column: span 2;
		}
	}

	@media (max-width: 639px) {
		grid-template-columns: 1fr;

		> *:last-child {
			grid-column: span 1;
		}
	}
}

/* Row 4: CasesOverview 1 column, RecentAchievements spans 2 columns */
.row-one-two {
	@apply grid gap-2 mb-2;
	grid-template-columns: repeat(3, 1fr);

	> *:last-child {
		grid-column: span 2;
	}

	@media (max-width: 1023px) {
		grid-template-columns: 1fr;

		> *:last-child {
			grid-column: span 1;
		}
	}
}

/* Row 5: Full width */
.row-full {
	@apply mb-0;
}
</style>
