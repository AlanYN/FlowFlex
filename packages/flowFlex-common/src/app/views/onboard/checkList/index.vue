<template>
	<!-- 加载状态 -->
	<checklist-loading v-if="loading" />

	<!-- 主要内容 -->
	<div v-else class="flex h-screen bg-gray-50">
		<!-- 左侧边栏 -->
		<div class="w-64 h-screen">
			<h1
				class="font-semibold text-blue-600 mb-4"
				style="
					font-size: 24px;
					color: var(--primary-500, #2468f2);
					margin: 0;
					font-weight: 700;
					line-height: 40px;
					padding-left: 20px;
					height: 60px;
				"
			>
				Checklists
			</h1>
			<div class="border-gray-200 bg-white rounded-lg">
				<h2 class="p-4 text-lg font-medium text-gray-900 mb-4 p-2 bg-blue-50 rounded">
					Teams
				</h2>
				<div class="p-4 space-y-2">
					<button
						v-for="team in teams"
						:key="team.id"
						@click="selectedTeam = team.id"
						:class="[
							'w-full text-left px-3 py-2 rounded-md text-sm transition-colors',
							selectedTeam === team.id
								? 'bg-gradient-to-r from-blue-100 to-blue-500 text-blue-900 font-medium'
								: 'text-gray-700 hover:bg-gray-100',
						]"
					>
						{{ team.name }}
					</button>
				</div>
			</div>
		</div>

		<!-- 主内容区 -->
		<div class="flex-1 flex flex-col border-gray-200 rounded-lg">
			<!-- 头部 -->
			<div class="p-4" style="padding-top: 0px">
				<div class="flex items-center justify-between mb-6">
					<h1 class="text-xl font-semibold" style="visibility: hidden">Checklists</h1>
					<button
						@click="openCreateDialog"
						class="px-3 py-2 text-sm rounded-md flex items-center gap-1 text-white"
						style="background-color: rgb(37, 99, 235)"
					>
						<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
							<path
								stroke-linecap="round"
								stroke-linejoin="round"
								stroke-width="2"
								d="M12 4v16m8-8H4"
							/>
						</svg>
						New Checklist
					</button>
				</div>
				<div class="bg-blue-50 rounded-lg p-4">
					<div class="flex items-center justify-between mb-3">
						<h2 class="text-lg font-medium text-gray-900">Checklists</h2>
						<div class="relative w-64">
							<svg
								class="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4"
								fill="none"
								stroke="currentColor"
								viewBox="0 0 24 24"
							>
								<path
									stroke-linecap="round"
									stroke-linejoin="round"
									stroke-width="2"
									d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
								/>
							</svg>
							<input
								v-model="searchQuery"
								placeholder="Search checklists..."
								class="pl-10 bg-white border-gray-300 rounded-md w-full px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
							/>
						</div>
					</div>
					<p class="text-sm text-gray-600">
						Task checklists for different teams during the onboarding process
					</p>
				</div>
			</div>

			<!-- 检查清单内容 -->
			<div class="flex-1 p-4 bg-gray-50">
				<div class="space-y-4">
					<div
						v-for="checklist in filteredChecklists"
						:key="checklist.id"
						:class="['shadow-sm border-gray-200 rounded-lg bg-white']"
					>
						<div class="p-0">
							<!-- 检查清单头部 - 整个区域可点击 -->
							<div
								class="p-4 cursor-pointer hover:bg-blue-50 transition-colors"
								@click="toggleExpanded(checklist.id)"
							>
								<div class="flex items-center justify-between">
									<div class="flex-1">
										<div class="flex items-center justify-between mb-2">
											<h3 class="text-base font-medium text-gray-900">
												{{ checklist.name }}
											</h3>
											<div class="flex items-center gap-2">
												<span
													class="inline-flex items-center rounded-full border border-gray-300 px-2.5 py-0.5 text-xs font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 text-gray-700 mr-2 bg-white"
												>
													{{ checklist.team }}
												</span>
												<span
													class="inline-flex items-center rounded-full border border-gray-300 px-2.5 py-0.5 text-xs font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 text-gray-700 mr-2 bg-white"
												>
																									{{
													checklist.totalTasks ||
													(checklist.tasks && checklist.tasks.length) ||
													0
												}}
												items
												</span>
												<div
													class="h-6 w-6 p-0 rounded-md hover:bg-gray-100 flex items-center justify-center"
												>
													<svg
														:class="[
															'w-4 h-4 transition-transform',
															expandedChecklists.includes(
																checklist.id
															)
																? 'rotate-90'
																: '',
														]"
														fill="none"
														stroke="currentColor"
														viewBox="0 0 24 24"
													>
														<path
															stroke-linecap="round"
															stroke-linejoin="round"
															stroke-width="2"
															d="M9 5l7 7-7 7"
														/>
													</svg>
												</div>
											</div>
										</div>
										<p class="text-sm text-gray-600 mb-1">
											{{ checklist.description }}
										</p>
										<div
											v-if="
												checklist.workflowName ||
												checklist.workflow ||
												checklist.stageName ||
												checklist.stage
											"
											class="flex items-center gap-1 text-xs text-gray-500"
										>
											<span
												v-if="checklist.workflowName || checklist.workflow"
											>
												Workflow:
												{{ checklist.workflowName || checklist.workflow }}
											</span>
											<span
												v-if="
													(checklist.workflowName ||
														checklist.workflow) &&
													(checklist.stageName || checklist.stage)
												"
												class="text-gray-400"
											>
												•
											</span>
											<span v-if="checklist.stageName || checklist.stage">
												Stage: {{ checklist.stageName || checklist.stage }}
											</span>
										</div>
									</div>
								</div>
							</div>

							<!-- 任务部分 -->
							<div
								v-if="expandedChecklists.includes(checklist.id)"
								class="p-4 bg-white border-t border-gray-100 rounded-lg"
							>
								<!-- 加载状态 -->
								<div
									v-if="!checklist.tasksLoaded"
									class="flex flex-col justify-center items-center py-8"
								>
									<div class="flex items-center mb-3">
										<svg
											class="animate-spin h-6 w-6 text-blue-500"
											fill="none"
											viewBox="0 0 24 24"
										>
											<circle
												class="opacity-25"
												cx="12"
												cy="12"
												r="10"
												stroke="currentColor"
												stroke-width="4"
											/>
											<path
												class="opacity-75"
												fill="currentColor"
												d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
											/>
										</svg>
										<span class="ml-2 text-gray-600">Loading tasks...</span>
									</div>
									<button
										@click="forceStopLoading(checklist)"
										class="text-xs text-gray-500 hover:text-gray-700 underline"
									>
										Taking too long? Click to skip loading
									</button>
								</div>

								<!-- 任务内容 -->
								<div v-else>
									<div class="flex items-center justify-between mb-4">
										<h4 class="text-sm font-medium text-gray-900">Tasks</h4>
										<div class="flex items-center gap-2">
											<div class="relative">
												<button
													@click="toggleDropdown(checklist.id)"
													:data-checklist-id="checklist.id"
													class="h-8 w-8 p-0 rounded-md hover:bg-gray-100 flex items-center justify-center"
												>
													<svg
														class="w-4 h-4"
														fill="currentColor"
														viewBox="0 0 24 24"
													>
														<circle cx="5" cy="12" r="2" />
														<circle cx="12" cy="12" r="2" />
														<circle cx="19" cy="12" r="2" />
													</svg>
												</button>
												<div
													v-if="activeDropdown === checklist.id"
													:class="getDropdownClasses(checklist.id)"
													class="dropdown-menu absolute w-48 bg-white rounded-md shadow-lg border border-gray-200 z-50"
												>
													<button
														@click="editChecklist(checklist)"
														class="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-50 flex items-center gap-2"
													>
														<svg
															class="w-4 h-4"
															fill="none"
															stroke="currentColor"
															viewBox="0 0 24 24"
														>
															<path
																stroke-linecap="round"
																stroke-linejoin="round"
																stroke-width="2"
																d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
															/>
														</svg>
														Edit Checklist
													</button>
													<button
														@click="deleteChecklistItem(checklist.id)"
														:disabled="deleteLoading"
														class="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-50 flex items-center gap-2 disabled:opacity-50"
													>
														<svg
															v-if="deleteLoading"
															class="w-4 h-4 animate-spin"
															fill="none"
															viewBox="0 0 24 24"
														>
															<circle
																class="opacity-25"
																cx="12"
																cy="12"
																r="10"
																stroke="currentColor"
																stroke-width="4"
															/>
															<path
																class="opacity-75"
																fill="currentColor"
																d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
															/>
														</svg>
														<svg
															v-else
															class="w-4 h-4"
															fill="none"
															stroke="currentColor"
															viewBox="0 0 24 24"
														>
															<path
																stroke-linecap="round"
																stroke-linejoin="round"
																stroke-width="2"
																d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
															/>
														</svg>
														{{
															deleteLoading
																? 'Deleting...'
																: 'Delete Checklist'
														}}
													</button>
													<hr class="my-1" />
													<button
														@click="exportChecklistItem(checklist)"
														:disabled="exportLoading"
														class="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-50 flex items-center gap-2 disabled:opacity-50"
													>
														<svg
															v-if="exportLoading"
															class="w-4 h-4 animate-spin"
															fill="none"
															viewBox="0 0 24 24"
														>
															<circle
																class="opacity-25"
																cx="12"
																cy="12"
																r="10"
																stroke="currentColor"
																stroke-width="4"
															/>
															<path
																class="opacity-75"
																fill="currentColor"
																d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
															/>
														</svg>
														<svg
															v-else
															class="w-4 h-4"
															fill="none"
															stroke="currentColor"
															viewBox="0 0 24 24"
														>
															<path
																stroke-linecap="round"
																stroke-linejoin="round"
																stroke-width="2"
																d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
															/>
														</svg>
														{{
															exportLoading
																? 'Exporting...'
																: 'Export to PDF'
														}}
													</button>
													<button
														@click="duplicateChecklistItem(checklist)"
														:disabled="duplicateLoading"
														class="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-50 flex items-center gap-2 disabled:opacity-50"
													>
														<svg
															v-if="duplicateLoading"
															class="w-4 h-4 animate-spin"
															fill="none"
															viewBox="0 0 24 24"
														>
															<circle
																class="opacity-25"
																cx="12"
																cy="12"
																r="10"
																stroke="currentColor"
																stroke-width="4"
															/>
															<path
																class="opacity-75"
																fill="currentColor"
																d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
															/>
														</svg>
														<svg
															v-else
															class="w-4 h-4"
															fill="none"
															stroke="currentColor"
															viewBox="0 0 24 24"
														>
															<path
																stroke-linecap="round"
																stroke-linejoin="round"
																stroke-width="2"
																d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"
															/>
														</svg>
														{{
															duplicateLoading
																? 'Duplicating...'
																: 'Duplicate'
														}}
													</button>
												</div>
											</div>
											<button
												@click="showAddTaskDialog(checklist)"
												class="h-8 w-8 p-0 rounded-md hover:bg-gray-100 flex items-center justify-center border border-gray-300"
											>
												<svg
													class="w-4 h-4"
													fill="none"
													stroke="currentColor"
													viewBox="0 0 24 24"
												>
													<path
														stroke-linecap="round"
														stroke-linejoin="round"
														stroke-width="2"
														d="M12 4v16m8-8H4"
													/>
												</svg>
											</button>
										</div>
									</div>

									<!-- 添加任务输入框 -->
									<div
										v-if="addingTaskTo === checklist.id"
										class="flex gap-2 mb-4"
									>
										<input
											v-model="newTaskText"
											placeholder="New task..."
											@keypress="handleTaskKeyPress($event, checklist.id)"
											class="flex-1 h-8 text-sm px-3 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
										/>
										<button
											@click="addTask(checklist.id)"
											style="background-color: rgb(37, 99, 235)"
											class="text-white h-8 px-3 text-xs rounded-md"
										>
											Add
										</button>
										<button
											@click="cancelAddTask"
											class="border border-gray-300 hover:bg-gray-50 h-8 px-3 text-xs rounded-md"
										>
											Cancel
										</button>
									</div>

									<!-- 任务列表 -->
									<div v-if="checklist.tasks.length > 0" class="space-y-0">
										<div
											v-for="task in checklist.tasks"
											:key="task.id"
											class="flex items-center gap-3 p-3 hover:bg-gray-50 transition-all duration-200 border border-transparent rounded-lg"
											draggable="true"
											@dragstart="dragStart(checklist.id, task.id, $event)"
											@dragenter.prevent="
												dragEnter(checklist.id, task.id, $event)
											"
											@dragover.prevent="dragOver($event)"
											@dragleave="dragLeave($event)"
											@dragend="dragEnd($event)"
											@drop.prevent="drop(checklist.id, $event)"
										>
											<!-- 排序图标 -->
											<button
												class="h-8 w-8 p-0 text-gray-400 hover:text-gray-600 rounded-md hover:bg-gray-100 flex items-center justify-center cursor-move drag-handle"
											>
												<svg
													xmlns="http://www.w3.org/2000/svg"
													width="24"
													height="24"
													viewBox="0 0 24 24"
													fill="none"
													stroke="currentColor"
													stroke-width="2"
													stroke-linecap="round"
													stroke-linejoin="round"
													class="h-5 w-5 text-muted-foreground"
												>
													<circle cx="9" cy="12" r="1" />
													<circle cx="9" cy="5" r="1" />
													<circle cx="9" cy="19" r="1" />
													<circle cx="15" cy="12" r="1" />
													<circle cx="15" cy="5" r="1" />
													<circle cx="15" cy="19" r="1" />
												</svg>
											</button>

											<!-- 正常显示模式 -->
											<template
												v-if="!(editingTask && editingTask.id === task.id)"
											>
												<span class="flex-1 text-sm text-gray-900">
													{{ task.name }}
												</span>
												<div class="flex items-center gap-1">
													<button
														@click="editTask(checklist.id, task)"
														class="h-8 w-8 p-0 hover:text-gray-700 rounded-md hover:bg-gray-100 flex items-center justify-center"
													>
														<svg
															class="w-4 h-4"
															fill="none"
															stroke="currentColor"
															viewBox="0 0 24 24"
														>
															<path
																stroke-linecap="round"
																stroke-linejoin="round"
																stroke-width="2"
																d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
															/>
														</svg>
													</button>
													<button
														@click="deleteTask(checklist.id, task.id)"
														class="h-8 w-8 p-0 text-red-600 hover:text-red-700 rounded-md hover:bg-gray-100 flex items-center justify-center"
													>
														<svg
															class="w-4 h-4"
															fill="none"
															stroke="currentColor"
															viewBox="0 0 24 24"
														>
															<path
																stroke-linecap="round"
																stroke-linejoin="round"
																stroke-width="2"
																d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
															/>
														</svg>
													</button>
												</div>
											</template>

											<!-- 编辑模式 -->
											<template v-else>
												<div class="flex-1 pr-2">
													<input
														v-model="taskFormData.name"
														class="w-full px-2 py-1 text-sm border border-gray-300 rounded focus:outline-none focus:ring-1 focus:ring-blue-500"
														placeholder="Task name"
													/>
												</div>
												<div class="flex items-center gap-1">
													<button
														@click="saveTaskEdit"
														class="px-3 py-1.5 text-sm rounded-md flex items-center gap-1 text-white"
														style="background-color: rgb(37, 99, 235)"
													>
														<svg
															xmlns="http://www.w3.org/2000/svg"
															width="24"
															height="24"
															viewBox="0 0 24 24"
															fill="none"
															stroke="currentColor"
															stroke-width="2"
															stroke-linecap="round"
															stroke-linejoin="round"
															class="h-4 w-4"
														>
															<path
																d="M15.2 3a2 2 0 0 1 1.4.6l3.8 3.8a2 2 0 0 1 .6 1.4V19a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2z"
															/>
															<path
																d="M17 21v-7a1 1 0 0 0-1-1H8a1 1 0 0 0-1 1v7"
															/>
															<path d="M7 3v4a1 1 0 0 0 1 1h7" />
														</svg>
													</button>
													<button
														@click="cancelTaskEdit"
														class="h-8 w-8 p-0 text-red-600 hover:text-red-700 rounded-md hover:bg-gray-100 flex items-center justify-center"
													>
														<svg
															class="w-4 h-4"
															fill="none"
															stroke="currentColor"
															viewBox="0 0 24 24"
														>
															<path
																stroke-linecap="round"
																stroke-linejoin="round"
																stroke-width="2"
																d="M6 18L18 6M6 6l12 12"
															/>
														</svg>
													</button>
												</div>
											</template>
										</div>
									</div>
									<div v-else class="text-center py-8 text-gray-500">
										<p class="text-sm">
											No tasks added yet. Click the + button to add a task.
										</p>
									</div>
								</div>
							</div>
						</div>
					</div>
				</div>
			</div>
		</div>

		<!-- 创建检查清单对话框 -->
		<div
			v-if="showCreateDialog"
			class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50"
		>
			<div class="bg-white rounded-lg shadow-xl max-w-lg w-full mx-4">
				<div class="p-6 border-gray-200">
					<h3 class="text-lg font-medium text-gray-900">Create New Checklist</h3>
					<p class="text-sm text-gray-600 mt-1">
						Create a new checklist for a specific team in the onboarding process.
					</p>
				</div>
				<div class="p-6 space-y-4">
					<div class="space-y-2">
						<label class="text-sm font-medium text-gray-700">Checklist Name</label>
						<input
							v-model="formData.name"
							placeholder="Enter checklist name"
							class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
						/>
					</div>
					<div class="space-y-2">
						<label class="text-sm font-medium text-gray-700">Description</label>
						<textarea
							v-model="formData.description"
							placeholder="Enter checklist description"
							rows="3"
							class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
						></textarea>
					</div>
					<div class="space-y-2">
						<label class="text-sm font-medium text-gray-700">Team</label>
						<select
							v-model="formData.team"
							class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
						>
							<option value="">Select team</option>
							<option v-for="team in availableTeams" :key="team" :value="team">
								{{ team }}
							</option>
						</select>
					</div>
					<div class="space-y-2">
						<div class="flex items-center justify-between">
							<label class="text-sm font-medium text-gray-700">Workflow & Stage Assignments</label>
							<button
								@click="addAssignment"
								type="button"
								class="text-blue-600 hover:text-blue-800 text-sm font-medium flex items-center gap-1"
							>
								<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
								</svg>
								Add Assignment
							</button>
						</div>
						<div v-if="formData.assignments.length === 0" class="text-sm text-gray-500 italic">
							No assignments yet. Click "Add Assignment" to create one.
						</div>
						<div v-else class="space-y-3">
							<div 
								v-for="(assignment, index) in formData.assignments" 
								:key="`assignment-${index}`"
								class="border border-gray-200 rounded-lg p-4 bg-gray-50"
							>
								<div class="flex items-start justify-between mb-3">
									<h4 class="text-sm font-medium text-gray-900">Assignment {{ index + 1 }}</h4>
									<button
										v-if="formData.assignments.length > 1"
										@click="removeAssignment(index)"
										type="button"
										class="text-red-600 hover:text-red-800 p-1"
									>
										<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
											<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
										</svg>
									</button>
								</div>
								<div class="grid grid-cols-1 md:grid-cols-2 gap-3">
									<div>
										<label class="text-xs font-medium text-gray-700 mb-1 block">Workflow</label>
						<select
											v-model="assignment.workflow"
											@change="handleWorkflowChangeForAssignment(index)"
											class="w-full px-3 py-2 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
						>
							<option value="">Select workflow</option>
							<option
								v-for="workflow in filteredWorkflows"
								:key="workflow.id"
								:value="workflow.name"
							>
								{{ workflow.name }}
							</option>
						</select>
					</div>
									<div>
										<label class="text-xs font-medium text-gray-700 mb-1 block">Stage</label>
						<select
											v-model="assignment.stage"
											:disabled="!assignment.workflow || stagesLoading"
											class="w-full px-3 py-2 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
						>
							<option value="">Select stage</option>
							<option
												v-for="stage in getStagesForAssignment(assignment.workflow)"
								:key="stage.id"
								:value="stage.name"
							>
								{{ stage.name }}
							</option>
						</select>
									</div>
								</div>
							</div>
						</div>
					</div>
				</div>
				<div class="p-6 border-t border-gray-200 flex justify-end gap-3">
					<button
						@click="closeCreateDialog"
						class="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
					>
						Cancel
					</button>
					<button
						@click="createChecklistItem"
						:disabled="!formData.name || !formData.team || createLoading"
						class="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
					>
						<svg
							v-if="createLoading"
							class="animate-spin h-4 w-4"
							fill="none"
							viewBox="0 0 24 24"
						>
							<circle
								class="opacity-25"
								cx="12"
								cy="12"
								r="10"
								stroke="currentColor"
								stroke-width="4"
							/>
							<path
								class="opacity-75"
								fill="currentColor"
								d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
							/>
						</svg>
						{{ createLoading ? 'Creating...' : 'Create Checklist' }}
					</button>
				</div>
			</div>
		</div>

		<!-- 编辑检查清单对话框 -->
		<div
			v-if="showEditDialog"
			class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50"
		>
			<div class="bg-white rounded-lg shadow-xl max-w-lg w-full mx-4">
				<div class="p-6 border-gray-200">
					<h3 class="text-lg font-medium text-gray-900">Edit Checklist</h3>
					<p class="text-sm text-gray-600 mt-1">Update the checklist details</p>
				</div>
				<div class="p-6 space-y-4">
					<div class="space-y-2">
						<label class="text-sm font-medium text-gray-700">Checklist Name</label>
						<input
							v-model="formData.name"
							class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
						/>
					</div>
					<div class="space-y-2">
						<label class="text-sm font-medium text-gray-700">Description</label>
						<textarea
							v-model="formData.description"
							rows="3"
							class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
						></textarea>
					</div>
					<div class="space-y-2">
						<label class="text-sm font-medium text-gray-700">Team</label>
						<select
							v-model="formData.team"
							class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
						>
							<option v-for="team in availableTeams" :key="team" :value="team">
								{{ team }}
							</option>
						</select>
					</div>
					<div class="space-y-2">
						<div class="flex items-center justify-between">
							<label class="text-sm font-medium text-gray-700">Workflow & Stage Assignments</label>
							<button
								@click="addAssignment"
								type="button"
								class="text-blue-600 hover:text-blue-800 text-sm font-medium flex items-center gap-1"
							>
								<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
									<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
								</svg>
								Add Assignment
							</button>
						</div>
						<div v-if="formData.assignments.length === 0" class="text-sm text-gray-500 italic">
							No assignments yet. Click "Add Assignment" to create one.
						</div>
						<div v-else class="space-y-3">
							<div 
								v-for="(assignment, index) in formData.assignments" 
								:key="`assignment-${index}`"
								class="border border-gray-200 rounded-lg p-4 bg-gray-50"
							>
								<div class="flex items-start justify-between mb-3">
									<h4 class="text-sm font-medium text-gray-900">Assignment {{ index + 1 }}</h4>
									<button
										v-if="formData.assignments.length > 1"
										@click="removeAssignment(index)"
										type="button"
										class="text-red-600 hover:text-red-800 p-1"
									>
										<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
											<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
										</svg>
									</button>
								</div>
								<div class="grid grid-cols-1 md:grid-cols-2 gap-3">
									<div>
										<label class="text-xs font-medium text-gray-700 mb-1 block">Workflow</label>
						<select
											v-model="assignment.workflow"
											@change="handleWorkflowChangeForAssignment(index)"
											class="w-full px-3 py-2 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
						>
							<option value="">Select workflow</option>
							<option
								v-for="workflow in filteredWorkflows"
								:key="workflow.id"
								:value="workflow.name"
							>
								{{ workflow.name }}
							</option>
						</select>
					</div>
									<div>
										<label class="text-xs font-medium text-gray-700 mb-1 block">Stage</label>
						<select
											v-model="assignment.stage"
											:disabled="!assignment.workflow || stagesLoading"
											class="w-full px-3 py-2 text-sm border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
						>
							<option value="">Select stage</option>
							<option
												v-for="stage in getStagesForAssignment(assignment.workflow)"
								:key="stage.id"
								:value="stage.name"
							>
								{{ stage.name }}
							</option>
						</select>
									</div>
								</div>
							</div>
						</div>
					</div>
				</div>
				<div class="p-6 border-t border-gray-200 flex justify-end gap-3">
					<button
						@click="closeEditDialog"
						class="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
					>
						Cancel
					</button>
					<button
						@click="saveEditChecklist"
						:disabled="!formData.name || !formData.team || editLoading"
						class="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
					>
						<svg
							v-if="editLoading"
							class="animate-spin h-4 w-4"
							fill="none"
							viewBox="0 0 24 24"
						>
							<circle
								class="opacity-25"
								cx="12"
								cy="12"
								r="10"
								stroke="currentColor"
								stroke-width="4"
							/>
							<path
								class="opacity-75"
								fill="currentColor"
								d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
							/>
						</svg>
						{{ editLoading ? 'Saving...' : 'Save Changes' }}
					</button>
				</div>
			</div>
		</div>
	</div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, shallowRef, watch } from 'vue';
import {
	getChecklists,
	getChecklistTasks,
	createChecklist,
	updateChecklist,
	deleteChecklist,
	createChecklistTask,
	updateChecklistTask,
	deleteChecklistTask,
	duplicateChecklist,
	formatTaskForApi,
	handleApiError,
} from '@/apis/ow/checklist';
import { getWorkflows, getStagesByWorkflow } from '@/apis/ow';
import { useI18n } from '@/hooks/useI18n';
import { ElMessage, ElMessageBox } from 'element-plus';
import ChecklistLoading from './checklist-loading.vue';

// 响应式数据 - 使用shallowRef优化大数组性能
const checklists = shallowRef([]);
const workflows = shallowRef([]);
const stages = shallowRef([]);
const loading = ref(false);
const error = ref(null);

// 任务编辑相关
const editingTask = ref(null);
const editingTaskChecklistId = ref(null);
const taskFormData = ref({
	name: '',
	description: '',
	estimatedMinutes: 0,
	isRequired: false,
});

const availableTeams = [
	'Sales',
	'IT',
	'Billing',
	'Implementation Team',
	'WISE Support',
	'Accounting',
];

// 团队列表
const teams = ref([
	{ id: 'all', name: 'All' },
	{ id: 'sales', name: 'Sales' },
	{ id: 'implementation', name: 'Implementation Team' },
	{ id: 'accounting', name: 'Accounting' },
	{ id: 'it', name: 'IT' },
	{ id: 'billing', name: 'Billing' },
	{ id: 'wise-support', name: 'WISE Support' },
]);

// UI状态
const searchQuery = ref('');
const selectedTeam = ref('all');
const expandedChecklists = ref([]);
const activeDropdown = ref(null);
const addingTaskTo = ref(null);
const newTaskText = ref('');

// 分页和虚拟滚动优化
const pageSize = ref(20); // 每页显示的清单数量
const currentPage = ref(1);

// 防抖搜索
const debouncedSearchQuery = ref('');
let searchTimeout = null;

// 监听搜索输入，添加防抖
watch(searchQuery, (newValue) => {
	if (searchTimeout) {
		clearTimeout(searchTimeout);
	}
	searchTimeout = setTimeout(() => {
		debouncedSearchQuery.value = newValue;
		currentPage.value = 1; // 重置到第一页
	}, 300); // 300ms防抖延迟
});

// 监听 checklists 变化以调试响应式更新
watch(
	checklists,
	(newValue, oldValue) => {
		console.log('Checklists changed:', {
			oldCount: oldValue?.length || 0,
			newCount: newValue?.length || 0,
			timestamp: new Date().toISOString(),
		});
	},
	{ deep: false }
);

// 对话框状态
const showCreateDialog = ref(false);
const showEditDialog = ref(false);
const editingChecklist = ref(null);

// 表单数据
const formData = ref({
	name: '',
	description: '',
	team: '',
	workflow: '',
	stage: '',
	assignments: [],
});

// Loading 状态管理
const createLoading = ref(false);
const editLoading = ref(false);
const deleteLoading = ref(false);
const duplicateLoading = ref(false);
const exportLoading = ref(false);
const stagesLoading = ref(false);

const { t } = useI18n();

// 计算属性 - 优化过滤和排序性能
const filteredChecklists = computed(() => {
	const searchTerm = debouncedSearchQuery.value?.toLowerCase() || '';
	const selectedTeamValue = selectedTeam.value;

	const filtered = checklists.value
		.filter((checklist) => {
			// 优化团队匹配逻辑
			const matchesTeam =
				selectedTeamValue === 'all' ||
				checklist.team === selectedTeamValue ||
				checklist.team.toLowerCase().replace(/\s+/g, '-') === selectedTeamValue ||
				// 添加反向匹配：根据selectedTeamValue找到对应的team name进行匹配
				(() => {
					const selectedTeamObj = teams.value.find((t) => t.id === selectedTeamValue);
					return selectedTeamObj && checklist.team === selectedTeamObj.name;
				})();

			// 优化搜索匹配逻辑
			if (!searchTerm) return matchesTeam;

			const nameMatch = checklist.name.toLowerCase().includes(searchTerm);
			const descMatch = checklist.description?.toLowerCase().includes(searchTerm) || false;

			return matchesTeam && (nameMatch || descMatch);
		})
		.sort((a, b) => {
			// 缓存日期对象避免重复创建
			const dateA =
				a._sortDate || (a._sortDate = new Date(a.createDate || a.createdAt || 0).getTime());
			const dateB =
				b._sortDate || (b._sortDate = new Date(b.createDate || b.createdAt || 0).getTime());
			return dateA - dateB;
		});

	// 分页优化：只返回当前页的数据
	const startIndex = (currentPage.value - 1) * pageSize.value;
	const endIndex = startIndex + pageSize.value;
	const result = filtered.slice(startIndex, endIndex);

	// 调试输出
	console.log('Filtered checklists computed:', {
		totalChecklists: checklists.value.length,
		filteredCount: result.length,
		selectedTeam: selectedTeamValue,
		searchTerm: searchTerm,
		availableTeams: checklists.value
			.map((c) => c.team)
			.filter((team, index, arr) => arr.indexOf(team) === index),
	});

	return result;
});

// 过滤活跃的workflow（排除Inactive状态且过期的）
const filteredWorkflows = computed(() => {
	// 返回所有workflows，如果需要过滤可以在这里添加逻辑
	return workflows.value || [];
});

// 根据选择的workflow过滤stages
const filteredStages = computed(() => {
	if (!formData.value.workflow) return [];
	const selectedWorkflow = filteredWorkflows.value.find(
		(w) => w.name === formData.value.workflow
	);

	if (!selectedWorkflow) return [];

	const filtered = stages.value.filter((stage) => {
		return stage.workflowId && stage.workflowId.toString() === selectedWorkflow.id.toString();
	});

	return filtered;
});

// 拖拽排序相关
const dragItem = ref(null);
const dragOverItem = ref(null);

// 调试函数
const debugDragState = () => {
	console.log('🔍 Current Drag State:', {
		dragItem: dragItem.value,
		dragOverItem: dragOverItem.value,
		timestamp: new Date().toISOString(),
	});
};

const dragStart = (checklistId, taskId, event) => {
	console.log('🚀 Drag Start:', { checklistId, taskId });
	dragItem.value = { checklistId, taskId };

	// dragOverItem 将通过 dragEnter 事件正确设置

	debugDragState();

	// 设置拖拽数据
	event.dataTransfer.effectAllowed = 'move';
	event.dataTransfer.setData('text/plain', taskId);

	// 添加拖拽样式
	const dragElement = event.target.closest('[draggable="true"]');
	if (dragElement) {
		setTimeout(() => {
			dragElement.classList.add('dragging');
		}, 0);
	}
};

const dragEnter = (checklistId, taskId, event) => {
	event.preventDefault(); // 确保preventDefault被调用
	console.log('📍 Drag Enter:', {
		checklistId,
		taskId,
		dragItem: dragItem.value,
		eventTarget: event.target.tagName,
		eventCurrentTarget: event.currentTarget.tagName,
	});

	if (!dragItem.value) {
		console.log('❌ Drag Enter blocked: no drag item');
		return;
	}

	if (dragItem.value.checklistId !== checklistId) {
		console.log('❌ Drag Enter blocked: different checklist');
		return;
	}

	// 不允许拖拽到同一个任务
	if (dragItem.value.taskId === taskId) {
		console.log('⚠️ Drag Enter: same task - skipping');
		return;
	}

	dragOverItem.value = { checklistId, taskId };
	console.log('✅ Drag Over Item set:', dragOverItem.value);
	debugDragState();

	// 移除所有drag-over类
	document.querySelectorAll('.drag-over').forEach((el) => {
		el.classList.remove('drag-over');
	});

	// 添加当前目标的drag-over类
	const targetElement = event.target.closest('[draggable="true"]');
	if (targetElement) {
		targetElement.classList.add('drag-over');
		console.log('🎯 Added drag-over class to target');
	}
};

const dragOver = (event) => {
	if (dragItem.value) {
		event.preventDefault(); // 确保preventDefault被调用
		event.dataTransfer.dropEffect = 'move';
		console.log('🔄 Drag Over - preventDefault called, dropEffect set to move');
	} else {
		console.log('⚠️ Drag Over called but no dragItem');
	}
};

const dragLeave = (event) => {
	console.log('👋 Drag Leave');
	// 只有当鼠标真正离开元素时才移除样式，但不清除dragOverItem
	const targetElement = event.target.closest('[draggable="true"]');
	if (targetElement && !targetElement.contains(event.relatedTarget)) {
		targetElement.classList.remove('drag-over');
		console.log('🧹 Removed drag-over class on leave (but kept dragOverItem)');
	}
};

const dragEnd = (event) => {
	console.log('🏁 Drag End:', { dragItem: dragItem.value, dragOverItem: dragOverItem.value });

	// 如果有dragOverItem，尝试手动触发drop
	if (dragItem.value && dragOverItem.value) {
		console.log('🔄 Attempting manual drop trigger...');
		setTimeout(() => {
			const mockEvent = {
				preventDefault: () => {},
				stopPropagation: () => {},
				type: 'drop',
				target: { tagName: 'DIV' },
			};
			drop(dragItem.value.checklistId, mockEvent);
		}, 50);
	}

	// 延迟清理，确保drop事件先执行
	setTimeout(() => {
		// 移除所有拖拽相关样式
		document.querySelectorAll('.dragging').forEach((el) => {
			el.classList.remove('dragging');
		});
		document.querySelectorAll('.drag-over').forEach((el) => {
			el.classList.remove('drag-over');
		});

		// 重置拖拽状态
		dragItem.value = null;
		dragOverItem.value = null;
		console.log('🧹 Drag state cleared (delayed)');
	}, 200);
};

const drop = async (checklistId, event) => {
	event.preventDefault(); // 确保preventDefault被调用
	event.stopPropagation(); // 阻止事件冒泡
	console.log('🎯 Drop triggered:', {
		checklistId,
		dragItem: dragItem.value,
		dragOverItem: dragOverItem.value,
		eventType: event.type,
		target: event.target.tagName,
	});

	if (!dragItem.value || !dragOverItem.value) {
		console.log('❌ Drop failed: missing drag items');
		return;
	}

	if (dragItem.value.checklistId !== checklistId) {
		console.log('❌ Drop failed: different checklist');
		return;
	}

	const checklist = checklists.value.find((c) => c.id === checklistId);
	if (!checklist) {
		console.log('❌ Drop failed: checklist not found');
		return;
	}

	// 找到拖拽的起始和目标位置
	const startIndex = checklist.tasks.findIndex((t) => t.id === dragItem.value.taskId);
	const endIndex = checklist.tasks.findIndex((t) => t.id === dragOverItem.value.taskId);

	console.log('📍 Drag positions:', { startIndex, endIndex });

	if (startIndex === -1 || endIndex === -1 || startIndex === endIndex) {
		console.log('❌ Drop failed: invalid positions or same position');
		return;
	}

	// 本地先重新排序
	const tasksCopy = [...checklist.tasks];
	const [itemToMove] = tasksCopy.splice(startIndex, 1);
	tasksCopy.splice(endIndex, 0, itemToMove);

	console.log('🔄 Reordering tasks:', {
		from: startIndex,
		to: endIndex,
		movedTask: itemToMove.name,
	});

	// 更新本地状态
	checklist.tasks = tasksCopy;

	// 强制触发响应式更新
	checklists.value = [...checklists.value];
	console.log('✅ Local state updated');

	try {
		// 更新后端数据 - 为每个任务分配新的顺序号
		const updatePromises = checklist.tasks.map((task, index) => {
			const updatedTask = formatTaskForApi({
				...task,
				checklistId: checklistId,
				order: index,
			});
			return updateChecklistTask(task.id, updatedTask);
		});

		await Promise.all(updatePromises);
		console.log('✅ Task order updated successfully');
		ElMessage.success('Task order updated successfully');
	} catch (err) {
		console.error('❌ Failed to update task order:', err);
		ElMessage.warning('Failed to save new order, but changes are visible locally');
	}
};

// 数据加载方法 - 优化性能
const loadChecklists = async () => {
	try {
		loading.value = true;
		error.value = null;
		console.log('Loading checklists...');
		const response = await getChecklists();
		const checklistData = response.data || response || [];
		console.log('Loaded checklists count:', checklistData.length);

		// 先设置基础数据，不加载任务（懒加载）
		const processedChecklists = checklistData
			.map((checklist) => {
				return {
					...checklist,
					tasks: [], // 初始化为空数组
					tasksLoaded: false, // 标记任务是否已加载
				};
			})
			.sort((a, b) => {
				// 按创建时间升序排序（最早的在前面）
				const dateA = new Date(a.createDate || a.createdAt || 0);
				const dateB = new Date(b.createDate || b.createdAt || 0);
				return dateA.getTime() - dateB.getTime();
			});

		// 使用新的数组引用确保响应式更新
		checklists.value = processedChecklists;
		console.log(
			'Checklists updated successfully, new checklist names:',
			processedChecklists.map((c) => ({ id: c.id, name: c.name }))
		);

		// 移除默认展开，提高初始加载速度
		// 用户可以按需展开需要的清单
	} catch (err) {
		error.value = handleApiError(err);
		console.error('Failed to load checklists:', err);
		// 使用示例数据作为后备
		checklists.value = getSampleData();

		// 默认展开第一个示例清单
		if (checklists.value.length > 0) {
			expandedChecklists.value = [checklists.value[0].id];
		}
	} finally {
		loading.value = false;
	}
};

// 任务加载缓存
const taskLoadingCache = new Map();

// 懒加载单个清单的任务 - 优化版本
const loadChecklistTasks = async (checklistId, forceReload = false) => {
	console.log('Loading tasks for checklist:', checklistId, forceReload ? '(force reload)' : '');
	const checklist = checklists.value.find((c) => c.id === checklistId);
	if (!checklist) {
		console.log('Checklist not found:', checklistId);
		return;
	}
	if (checklist.tasksLoaded && !forceReload) {
		console.log('Tasks already loaded for checklist:', checklistId);
		return;
	}

	// 如果强制重新加载，清除缓存
	if (forceReload) {
		taskLoadingCache.delete(checklistId);
		checklist.tasksLoaded = false;
	}

	// 防止重复加载
	if (taskLoadingCache.has(checklistId)) {
		return taskLoadingCache.get(checklistId);
	}

	// 立即设置加载状态，避免无限加载
	checklist.tasksLoaded = false;

	const loadPromise = (async () => {
		try {
			console.log('Calling getChecklistTasks API for:', checklistId);

			// 添加超时机制
			const timeoutPromise = new Promise((_, reject) => {
				setTimeout(() => reject(new Error('API request timeout')), 10000); // 10秒超时
			});

			const tasks = await Promise.race([getChecklistTasks(checklistId), timeoutPromise]);

			console.log('API response:', tasks);

			const processedTasks = (tasks.data || tasks || []).map((task) => ({
				...task,
				completed: task.isCompleted || task.completed || false,
				estimatedMinutes: task.estimatedHours ? task.estimatedHours * 60 : 0,
			}));

			console.log('Processed tasks:', processedTasks);

			// 使用Object.assign确保响应式更新
			Object.assign(checklist, {
				tasks: processedTasks,
				tasksLoaded: true,
			});

			console.log('Updated checklist:', checklist);

			// 强制触发响应式更新
			checklists.value = [...checklists.value];
			console.log('Tasks loaded successfully for checklist:', checklistId);
			return processedTasks;
		} catch (taskError) {
			console.error(`Failed to load tasks for checklist ${checklistId}:`, taskError);

			// 确保即使出错也要设置tasksLoaded为true，避免无限加载
			Object.assign(checklist, {
				tasks: [],
				tasksLoaded: true,
			});

			// 强制触发响应式更新
			checklists.value = [...checklists.value];

			// 显示用户友好的错误消息
			ElMessage.error(`Failed to load tasks: ${taskError.message || 'Unknown error'}`);
			return [];
		} finally {
			// 清理缓存
			taskLoadingCache.delete(checklistId);
		}
	})();

	taskLoadingCache.set(checklistId, loadPromise);
	return loadPromise;
};

// 优化的workflow和stage加载逻辑
const loadWorkflowsAndStages = async () => {
	try {
		// 加载workflows
		const workflowResponse = await getWorkflows();

		if (workflowResponse.code === '200') {
			workflows.value = workflowResponse.data || [];
		} else {
			workflows.value = [];
			return; // 如果workflows加载失败，直接返回
		}

		// 只为活跃的workflows加载stages，减少API调用
		const activeWorkflows = workflows.value.filter((w) => w.isActive && w.status === 'Active');

		if (activeWorkflows.length === 0) {
			stages.value = [];
			return;
		}

		// 批量加载stages，限制并发数量
		const batchSize = 3; // 限制并发请求数量
		const stageResponses = [];

		for (let i = 0; i < activeWorkflows.length; i += batchSize) {
			const batch = activeWorkflows.slice(i, i + batchSize);
			const batchPromises = batch.map((workflow) =>
				getStagesByWorkflow(workflow.id)
					.then((response) => {
						if (response.code === '200') {
							return { data: response.data || [] };
						} else {
							return { data: [] };
						}
					})
					.catch((err) => {
						console.warn(`Failed to load stages for workflow ${workflow.id}:`, err);
						return { data: [] };
					})
			);

			const batchResults = await Promise.all(batchPromises);
			stageResponses.push(...batchResults);
		}

		// 合并所有stages
		stages.value = stageResponses.reduce((allStages, response) => {
			const stageData = response.data || [];
			return [...allStages, ...stageData];
		}, []);
		// Stages加载完成
	} catch (err) {
		console.error('Failed to load workflows and stages:', err);
		workflows.value = [];
		stages.value = [];
	}
};

const getSampleData = () => [];

// UI交互方法
const toggleExpanded = async (checklistId) => {
	const index = expandedChecklists.value.indexOf(checklistId);
	if (index > -1) {
		// 如果当前已展开，则收起
		expandedChecklists.value.splice(index, 1);
	} else {
		// 如果当前未展开，则先收起所有其他的，再展开当前的（保持只有一个展开）
		expandedChecklists.value = [checklistId];

		// 展开时懒加载任务
		try {
			await loadChecklistTasks(checklistId);
		} catch (error) {
			console.error('Failed to load tasks on expand:', error);
			// 确保即使加载失败也设置为已加载，避免无限加载状态
			const checklist = checklists.value.find((c) => c.id === checklistId);
			if (checklist) {
				checklist.tasksLoaded = true;
				checklist.tasks = [];
				checklists.value = [...checklists.value];
			}
		}
	}
};

// 强制停止加载
const forceStopLoading = (checklist) => {
	console.log('Force stopping loading for checklist:', checklist.id);
	checklist.tasksLoaded = true;
	checklist.tasks = checklist.tasks || [];
	checklists.value = [...checklists.value];
	ElMessage.info('Loading stopped. Tasks may be empty.');
};

const toggleDropdown = (checklistId) => {
	activeDropdown.value = activeDropdown.value === checklistId ? null : checklistId;
};

// 动态计算下拉菜单位置，避免在页面底部被截断
const getDropdownClasses = (checklistId) => {
	// 基础类名
	let classes = 'right-0 mt-2';

	// 尝试获取触发按钮的位置信息
	try {
		// 查找对应的下拉按钮
		const button = document.querySelector(`[data-checklist-id="${checklistId}"]`);
		if (button) {
			const rect = button.getBoundingClientRect();
			const windowHeight = window.innerHeight;
			const dropdownHeight = 220; // 估算下拉菜单高度（4个菜单项 + 分隔线 + 间距）
			const spaceBelow = windowHeight - rect.bottom;
			const spaceAbove = rect.top;

			// 如果下方空间不足，且上方空间更充足，则向上显示
			if (spaceBelow < dropdownHeight && spaceAbove > spaceBelow) {
				classes = 'right-0 bottom-full mb-2';
			}
		}
	} catch (error) {
		// 如果获取位置失败，使用默认位置
		console.warn('Failed to calculate dropdown position:', error);
	}

	return classes;
};

const showAddTaskDialog = (checklist) => {
	addingTaskTo.value = checklist.id;
	newTaskText.value = '';
};

const cancelAddTask = () => {
	addingTaskTo.value = null;
	newTaskText.value = '';
};

const handleTaskKeyPress = (event, checklistId) => {
	if (event.key === 'Enter') {
		addTask(checklistId);
	}
};

// 任务管理方法
const addTask = async (checklistId) => {
	if (!newTaskText.value.trim()) return;

	try {
		const taskData = formatTaskForApi({
			checklistId: checklistId,
			name: newTaskText.value.trim(),
			description: '',
			isRequired: false,
			order: 0,
		});

		await createChecklistTask(taskData);
		ElMessage.success(t('sys.api.operationSuccess'));

		// 重新加载该清单的任务
		const checklist = checklists.value.find((c) => c.id === checklistId);
		if (checklist) {
			const tasks = await getChecklistTasks(checklistId);
			const processedTasks = (tasks.data || tasks || []).map((task) => ({
				...task,
				completed: task.isCompleted || task.completed || false,
				estimatedMinutes: task.estimatedHours ? task.estimatedHours * 60 : 0,
			}));

			// 使用Object.assign确保响应式更新
			Object.assign(checklist, {
				tasks: processedTasks,
				tasksLoaded: true,
			});

			// 强制触发响应式更新
			checklists.value = [...checklists.value];
		}

		cancelAddTask();
	} catch (err) {
		console.error('Failed to create task:', err);
		ElMessage.error(t('sys.api.operationFailed'));
		// 后备方案：本地添加
		const checklist = checklists.value.find((c) => c.id === checklistId);
		if (checklist) {
			checklist.tasks.push({
				id: Date.now(),
				name: newTaskText.value,
				completed: false,
				estimatedMinutes: 0,
			});
			// 强制触发响应式更新
			checklists.value = [...checklists.value];
		}
		cancelAddTask();
	}
};

const deleteTask = async (checklistId, taskId) => {
	try {
		await ElMessageBox.confirm(
			'Are you sure you want to delete this task? This action cannot be undone.',
			'Confirm Deletion',
			{
				confirmButtonText: 'Delete Task',
				cancelButtonText: 'Cancel',
				type: 'warning',
				customClass: 'custom-confirm-dialog',
				confirmButtonClass: 'el-button--danger',
			}
		);
	} catch {
		return; // 用户取消删除
	}

	try {
		await deleteChecklistTask(taskId, true);
		ElMessage.success('Task deleted successfully');

		// 重新加载该清单的任务
		const checklist = checklists.value.find((c) => c.id === checklistId);
		if (checklist) {
			const tasks = await getChecklistTasks(checklistId);
			const processedTasks = (tasks.data || tasks || []).map((task) => ({
				...task,
				completed: task.isCompleted || task.completed || false,
				estimatedMinutes: task.estimatedHours ? task.estimatedHours * 60 : 0,
			}));

			// 使用Object.assign确保响应式更新
			Object.assign(checklist, {
				tasks: processedTasks,
				tasksLoaded: true,
			});

			// 强制触发响应式更新
			checklists.value = [...checklists.value];
		}
	} catch (err) {
		console.error('Failed to delete task:', err);
		ElMessage.error('Failed to delete task');
		// 后备方案：本地删除
		const checklist = checklists.value.find((c) => c.id === checklistId);
		if (checklist) {
			checklist.tasks = checklist.tasks.filter((t) => t.id !== taskId);
			// 强制触发响应式更新
			checklists.value = [...checklists.value];
		}
	}
};

// Workflow和Stage联动处理
const handleWorkflowChange = async () => {
	// 清空当前选择的stage
	formData.value.stage = '';
	// 根据选择的workflow加载对应的stages
	await loadStagesByWorkflow(formData.value.workflow);
};

const handleWorkflowChangeEdit = async () => {
	// 清空当前选择的stage
	formData.value.stage = '';
	// 根据选择的workflow加载对应的stages
	await loadStagesByWorkflow(formData.value.workflow);
};

// 根据workflow加载stages
const loadStagesByWorkflow = async (workflowName) => {
	if (!workflowName) {
		stages.value = [];
		return;
	}

	try {
		stagesLoading.value = true;
		// 根据workflow名称找到对应的workflow ID
		const selectedWorkflow = workflows.value.find((w) => w.name === workflowName);

		if (!selectedWorkflow) {
			stages.value = [];
			return;
		}

		const response = await getStagesByWorkflow(selectedWorkflow.id);

		if (response.code === '200') {
			// 加载当前workflow的stages
			const workflowStages = response.data || [];

			// 确保每个stage都有workflowId属性
			const stagesWithWorkflowId = workflowStages.map((stage) => ({
				...stage,
				workflowId: selectedWorkflow.id,
			}));

			// 过滤出其他workflow的stages，并与当前workflow的stages合并
			const otherWorkflowStages = stages.value.filter(
				(stage) =>
					stage.workflowId &&
					stage.workflowId.toString() !== selectedWorkflow.id.toString()
			);
			stages.value = [...otherWorkflowStages, ...stagesWithWorkflowId];
			console.log(`Loaded ${workflowStages.length} stages for workflow: ${workflowName}`);
		} else {
			console.warn('Failed to load stages, API response code:', response.code);
		}
	} catch (error) {
		console.warn(`Failed to load stages for workflow ${workflowName}:`, error);
	} finally {
		stagesLoading.value = false;
	}
};

// 清单管理方法
const editChecklist = async (checklist) => {
	editingChecklist.value = checklist;

	// 根据ID查找workflow名称
	let workflowName = '';
	if (checklist.workflowId) {
		const workflow = workflows.value.find(
			(w) => w.id.toString() === checklist.workflowId.toString()
		);
		workflowName = workflow ? workflow.name : '';
	}

	// 加载所有assignments需要的stages
	const uniqueWorkflowIds = new Set();
	
	// 添加单个workflow（如果存在）
	if (workflowName) {
		const workflow = workflows.value.find(w => w.name === workflowName);
		if (workflow) {
			uniqueWorkflowIds.add(workflow.id.toString());
		}
	}
	
	// 添加assignments中的workflows
	(checklist.assignments || []).forEach(assignment => {
		if (assignment.workflowId) {
			uniqueWorkflowIds.add(assignment.workflowId.toString());
		}
	});

	// 为每个唯一的workflow加载stages
	for (const workflowId of uniqueWorkflowIds) {
		const workflow = workflows.value.find(w => w.id.toString() === workflowId);
		if (workflow) {
			await loadStagesByWorkflow(workflow.name);
		}
	}

	// 现在查找stage名称（stages已经加载）
	let stageName = '';
	if (checklist.stageId) {
		const stage = stages.value.find((s) => s.id.toString() === checklist.stageId.toString());
		stageName = stage ? stage.name : '';
		if (stage) {
			console.log(`Found stage: ${stage.name} for checklist: ${checklist.name}`);
		}
	}

	// 处理assignments，转换为前端需要的格式
	const assignments = (checklist.assignments || []).map(assignment => {
		const workflow = workflows.value.find(w => w.id.toString() === assignment.workflowId.toString());
		const stage = stages.value.find(s => s.id.toString() === assignment.stageId.toString());
		
		console.log(`Processing assignment: workflowId=${assignment.workflowId}, stageId=${assignment.stageId}`);
		console.log(`Found workflow: ${workflow ? workflow.name : 'NOT FOUND'}`);
		console.log(`Found stage: ${stage ? stage.name : 'NOT FOUND'}`);
		
		return {
			workflow: workflow ? workflow.name : '',
			stage: stage ? stage.name : ''
		};
	}).filter(assignment => assignment.workflow && assignment.stage);
	
	console.log(`Processed ${assignments.length} valid assignments out of ${(checklist.assignments || []).length} total assignments`);

	formData.value = {
		name: checklist.name,
		description: checklist.description,
		team: checklist.team,
		workflow: workflowName,
		stage: stageName,
		assignments: assignments,
	};

	showEditDialog.value = true;
	activeDropdown.value = null;
};

const deleteChecklistItem = async (checklistId) => {
	try {
		await ElMessageBox.confirm(
			'Are you sure you want to delete this checklist? This action cannot be undone.',
			'Confirm Deletion',
			{
				confirmButtonText: 'Delete Checklist',
				cancelButtonText: 'Cancel',
				type: 'warning',
				customClass: 'custom-confirm-dialog',
				confirmButtonClass: 'el-button--danger',
			}
		);
	} catch {
		return; // 用户取消删除
	}

	deleteLoading.value = true;
	try {
		await deleteChecklist(checklistId, true);
		console.log('Checklist deleted successfully');
		ElMessage.success('Checklist deleted successfully');
		activeDropdown.value = null;

		// 删除成功后立即刷新页面数据
		console.log('Refreshing checklist data after deletion...');
		await loadChecklists();

		// 清空展开状态，避免引用已删除的checklist
		expandedChecklists.value = expandedChecklists.value.filter((id) => id !== checklistId);
	} catch (err) {
		console.error('Failed to delete checklist:', err);

		// 提供更详细的错误信息
		let errorMessage = 'Failed to delete checklist';
		if (err.response?.status === 404) {
			errorMessage = 'Checklist not found or already deleted';
		} else if (err.response?.status === 403) {
			errorMessage = 'You do not have permission to delete this checklist';
		} else if (err.message) {
			errorMessage = `Deletion failed: ${err.message}`;
		}

		ElMessage.error(errorMessage);
		activeDropdown.value = null;

		// 即使删除失败，也刷新一下数据，可能后端已经删除成功了
		console.log('Refreshing checklist data after deletion error...');
		await loadChecklists();

		// 清空展开状态
		expandedChecklists.value = expandedChecklists.value.filter((id) => id !== checklistId);
	} finally {
		deleteLoading.value = false;
	}
};

// 手动复制任务的辅助函数
const copyTasksManually = async (originalChecklist, newChecklistId) => {
	try {
		// 确保原checklist的任务已加载
		let sourceChecklist = checklists.value.find(
			(c) => c.id.toString() === originalChecklist.id.toString()
		);

		// 如果没有找到或任务未加载，先加载任务
		if (
			!sourceChecklist ||
			!sourceChecklist.tasksLoaded ||
			!sourceChecklist.tasks ||
			sourceChecklist.tasks.length === 0
		) {
			console.log('Source checklist tasks not loaded, loading now...');
			console.log('Original checklist ID:', originalChecklist.id);
			console.log(
				'Available checklists:',
				checklists.value.map((c) => ({
					id: c.id,
					name: c.name,
					tasksCount: c.tasks?.length || 0,
				}))
			);
			await loadChecklistTasks(originalChecklist.id);
			sourceChecklist = checklists.value.find(
				(c) => c.id.toString() === originalChecklist.id.toString()
			);
		}

		// 如果仍然没有找到或没有任务，检查是否直接传入了任务数据
		if (!sourceChecklist || !sourceChecklist.tasks || sourceChecklist.tasks.length === 0) {
			// 检查originalChecklist是否直接包含任务数据
			if (originalChecklist.tasks && originalChecklist.tasks.length > 0) {
				console.log('Using tasks from originalChecklist parameter');
				sourceChecklist = originalChecklist;
			} else {
				console.log('No tasks to copy after loading');
				console.log('Source checklist:', sourceChecklist);
				console.log('Original checklist:', originalChecklist);
				return;
			}
		}

		console.log(
			`Copying ${sourceChecklist.tasks.length} tasks to new checklist ${newChecklistId}`
		);

		// 为每个任务创建新的任务
		const taskPromises = sourceChecklist.tasks.map(async (task, index) => {
			const newTaskData = {
				checklistId: newChecklistId,
				name: task.name,
				description: task.description || '',
				isRequired: task.isRequired !== false,
				estimatedHours: task.estimatedHours || 0,
				order: index,
				taskType: task.taskType || 'Standard',
			};

			try {
				const newTask = await createChecklistTask(newTaskData);
				console.log('Created task:', newTask);
				return newTask;
			} catch (taskError) {
				console.error('Failed to create task:', taskError);
				return null;
			}
		});

		await Promise.all(taskPromises);

		// 重新加载新checklist的任务
		await loadChecklistTasks(newChecklistId);
		console.log('Tasks copied successfully');
	} catch (error) {
		console.error('Failed to copy tasks manually:', error);
	}
};

// 生成唯一的复制名称
const generateUniqueName = (baseName) => {
	const existingNames = checklists.value.map((c) => c.name.toLowerCase());
	let counter = 1;
	let newName = `${baseName}-${counter}`;

	// 持续递增直到找到唯一名称
	while (existingNames.includes(newName.toLowerCase())) {
		counter++;
		newName = `${baseName}-${counter}`;
	}

	return newName;
};

const duplicateChecklistItem = async (checklist) => {
	duplicateLoading.value = true;
	try {
		// 确保任务已加载
		if (!checklist.tasksLoaded || !checklist.tasks || checklist.tasks.length === 0) {
			console.log('Loading tasks for checklist before duplication:', checklist.id);
			await loadChecklistTasks(checklist.id);
		}

		// 获取最新的checklist数据（包含任务）
		const updatedChecklist = checklists.value.find((c) => c.id === checklist.id) || checklist;
		console.log('Duplicating checklist with tasks:', updatedChecklist.tasks?.length || 0);

		// 生成唯一的名称，避免重名问题
		const duplicateName = generateUniqueName(checklist.name);

		// 确保参数符合DuplicateChecklistInputDto接口
		const duplicateData = {
			name: duplicateName,
			description: checklist.description || '',
			team: checklist.team || 'Sales', // 确保team不为空
			copyTasks: true,
			setAsTemplate: false,
		};

		console.log('Duplicate request data:', duplicateData);
		console.log('Original checklist ID:', checklist.id);
		console.log('Original checklist data:', checklist);

		const newChecklist = await duplicateChecklist(checklist.id, duplicateData);
		console.log('Duplicate response:', newChecklist);

		ElMessage.success('Checklist duplicated successfully');
		activeDropdown.value = null;

		// 复制成功后立即刷新页面数据
		console.log('Refreshing checklist data after duplication...');
		await loadChecklists();

		// 查找新创建的checklist并展开它
		const newChecklistItem = checklists.value.find((c) => c.name === duplicateName);
		if (newChecklistItem) {
			// 展开新创建的checklist
			expandedChecklists.value = [newChecklistItem.id];
			// 加载任务
			await loadChecklistTasks(newChecklistItem.id);

			// 检查任务是否被成功复制，如果没有则手动复制
			const updatedNewChecklist = checklists.value.find((c) => c.id === newChecklistItem.id);
			if (
				updatedNewChecklist &&
				(!updatedNewChecklist.tasks || updatedNewChecklist.tasks.length === 0)
			) {
				console.log('Tasks were not copied by backend, manually copying tasks...');
				await copyTasksManually(updatedChecklist, newChecklistItem.id);
			}
		}
	} catch (err) {
		console.error('Failed to duplicate checklist:', err);

		// 提供更详细的错误信息
		let errorMessage = 'Failed to duplicate checklist';
		if (err.response?.status === 500) {
			errorMessage = 'Server error occurred. Please try again.';
		} else if (err.response?.status === 404) {
			errorMessage = 'Checklist not found.';
		} else if (err.response?.status === 400) {
			errorMessage = 'Invalid request parameters.';
		} else if (err.message) {
			errorMessage = `Duplication failed: ${err.message}`;
		}

		ElMessage.error(errorMessage);
		activeDropdown.value = null;

		// 即使复制失败，也刷新一下数据，检查是否有新的checklist被创建
		console.log('Refreshing checklist data after duplication error...');
		await loadChecklists();
	} finally {
		duplicateLoading.value = false;
	}
};

// 导出PDF文件功能
const exportChecklistItem = async (checklist) => {
	exportLoading.value = true;
	try {
		console.log('开始导出PDF，清单ID:', checklist.id);

		// 确保任务已加载
		if (!checklist.tasksLoaded || !checklist.tasks || checklist.tasks.length === 0) {
			console.log('任务未加载，正在加载任务数据...');
			await loadChecklistTasks(checklist.id);
		}

		// 直接使用前端生成PDF（后端暂不支持PDF导出）
		console.log('使用前端生成PDF');
		await exportPdfWithFrontend(checklist);
	} catch (err) {
		console.error('PDF导出失败:', err);
		ElMessage.error(`PDF export failed: ${err.message || 'Unknown error'}`);
		activeDropdown.value = null;
	} finally {
		exportLoading.value = false;
	}
};

// 前端生成PDF的后备方案
const exportPdfWithFrontend = async (checklist) => {
	try {
		console.log('开始前端PDF生成...');

		// 动态导入jsPDF库 - 兼容不同版本
		const jsPDFModule = await import('jspdf');
		console.log('jsPDF模块导入成功:', jsPDFModule);

		// 尝试不同的导入方式
		let jsPDF;
		if (jsPDFModule.jsPDF) {
			jsPDF = jsPDFModule.jsPDF;
		} else if (jsPDFModule.default && jsPDFModule.default.jsPDF) {
			jsPDF = jsPDFModule.default.jsPDF;
		} else if (jsPDFModule.default) {
			jsPDF = jsPDFModule.default;
		} else {
			throw new Error('无法找到jsPDF构造函数');
		}

		// 获取最新的checklist数据（包含任务）
		const updatedChecklist = checklists.value.find((c) => c.id === checklist.id) || checklist;
		console.log('准备导出的清单数据:', updatedChecklist);

		// 创建PDF实例
		const pdf = new jsPDF({
			orientation: 'portrait',
			unit: 'mm',
			format: 'a4',
		});

		console.log('PDF实例创建成功');

		let y = 20;
		const margin = 20;
		const pageWidth = 210; // A4宽度

		// 添加头部背景色和标题
		pdf.setFillColor(52, 71, 103); // 更深的蓝色，匹配设计图
		pdf.rect(0, 0, pageWidth, 30, 'F');

		// 添加白色标题文字
		pdf.setTextColor(255, 255, 255);
		pdf.setFontSize(20);
		pdf.text('UNIS', margin, 20);
		pdf.setFontSize(16);
		pdf.text('Warehousing Solutions', margin + 60, 20);

		// 重置文字颜色为黑色
		pdf.setTextColor(0, 0, 0);
		y = 45;

		// 添加清单名称作为主标题
		pdf.setFontSize(18);
		const checklistName = String(updatedChecklist.name || 'Untitled');
		pdf.text(checklistName, margin, y);
		y += 15;

		// 添加基本信息
		pdf.setFontSize(12);

		const description = String(updatedChecklist.description || 'No description');
		pdf.text(`Description: ${description}`, margin, y);
		y += 8;

		const team = String(updatedChecklist.team || 'No team');
		pdf.text(`Team: ${team}`, margin, y);
		y += 8;

		const workflowName = String(getWorkflowNameForPdf(updatedChecklist));
		pdf.text(`Workflow: ${workflowName}`, margin, y);
		y += 8;

		const stageName = String(getStageNameForPdf(updatedChecklist));
		pdf.text(`Stage: ${stageName}`, margin, y);
		y += 15;

		// 创建任务表格
		const tasks = updatedChecklist.tasks || [];
		console.log('任务数量:', tasks.length);

		if (tasks.length > 0) {
			// 表格头部
			pdf.setFillColor(52, 71, 103); // 与头部保持一致的深蓝色
			pdf.rect(margin, y, pageWidth - 2 * margin, 8, 'F');

			// 表格头部文字 - 两列布局
			pdf.setTextColor(255, 255, 255);
			pdf.setFontSize(12);
			pdf.text('Task', margin + 20, y + 5.5);

			// 绘制表格头部列分隔线
			pdf.setDrawColor(255, 255, 255);
			pdf.setLineWidth(0.1);
			pdf.line(margin + 15, y, margin + 15, y + 8);

			y += 8;
			pdf.setTextColor(0, 0, 0);
			pdf.setFontSize(11);

			// 添加任务行
			tasks.forEach((task, index) => {
				// 检查是否需要新页面
				if (y > 250) {
					pdf.addPage();
					y = 20;

					// 重新添加表格头部
					pdf.setFillColor(52, 71, 103);
					pdf.rect(margin, y, pageWidth - 2 * margin, 8, 'F');
					pdf.setTextColor(255, 255, 255);
					pdf.setFontSize(12);
					pdf.text('Task', margin + 20, y + 5.5);
					pdf.setDrawColor(255, 255, 255);
					pdf.setLineWidth(0.1);
					pdf.line(margin + 15, y, margin + 15, y + 8);
					y += 8;
					pdf.setTextColor(0, 0, 0);
					pdf.setFontSize(11);
				}

				// 绘制表格行背景（交替颜色）
				if (index % 2 === 1) {
					pdf.setFillColor(245, 247, 250); // 更浅的灰色，接近设计图
					pdf.rect(margin, y, pageWidth - 2 * margin, 8, 'F');
				}

				// 绘制表格边框
				pdf.setDrawColor(209, 213, 219); // 更深一点的边框颜色，增强对比度
				pdf.setLineWidth(0.1);

				// 绘制行的边框
				pdf.rect(margin, y, pageWidth - 2 * margin, 8, 'S');

				// 绘制列分隔线
				pdf.line(margin + 15, y, margin + 15, y + 8);

				// 添加序号和任务名称
				const taskName = String(task.name || `Task ${index + 1}`);
				pdf.setTextColor(0, 0, 0);
				pdf.setFontSize(12);
				pdf.text(`${index + 1}`, margin + 6, y + 5.5);
				pdf.text(taskName, margin + 20, y + 5.5);

				y += 8;
			});
		} else {
			// 如果没有任务，显示空状态
			pdf.setFillColor(52, 71, 103);
			pdf.rect(margin, y, pageWidth - 2 * margin, 8, 'F');

			pdf.setTextColor(255, 255, 255);
			pdf.setFontSize(12);
			pdf.text('Task', margin + 20, y + 5.5);

			// 绘制列分隔线
			pdf.setDrawColor(255, 255, 255);
			pdf.setLineWidth(0.1);
			pdf.line(margin + 15, y, margin + 15, y + 8);

			y += 8;

			// 绘制空行边框
			pdf.setDrawColor(209, 213, 219);
			pdf.setLineWidth(0.1);
			pdf.rect(margin, y, pageWidth - 2 * margin, 8, 'S');
			pdf.line(margin + 15, y, margin + 15, y + 8);

			pdf.setTextColor(156, 163, 175); // 灰色文字
			pdf.setFontSize(11);
			pdf.text('No tasks available', margin + 20, y + 5.5);
		}

		console.log('PDF内容添加完成，准备保存...');

		// 生成文件名
		const filename = `${checklistName.replace(/[^\w\s-]/g, '_')}.pdf`;

		// 保存PDF
		pdf.save(filename);

		console.log('PDF保存成功，文件名:', filename);
		ElMessage.success('PDF exported successfully');
		activeDropdown.value = null;
	} catch (frontendErr) {
		console.error('前端PDF生成失败:', frontendErr);
		console.error('错误详情:', frontendErr.stack);

		// 尝试最简单的方案
		await exportBasicPdf(checklist);
	}
};

// 最基本的PDF生成方案
const exportBasicPdf = async (checklist) => {
	try {
		console.log('尝试最基本的PDF生成方案');

		// 创建纯文本内容
		const updatedChecklist = checklists.value.find((c) => c.id === checklist.id) || checklist;

		let content = 'UNIS Checklist Export\n\n';
		content += `Name: ${updatedChecklist.name || 'Untitled'}\n`;
		content += `Description: ${updatedChecklist.description || 'No description'}\n`;
		content += `Team: ${updatedChecklist.team || 'No team'}\n`;
		content += `Workflow: ${getWorkflowNameForPdf(updatedChecklist)}\n`;
		content += `Stage: ${getStageNameForPdf(updatedChecklist)}\n\n`;
		content += 'Tasks:\n';

		const tasks = updatedChecklist.tasks || [];
		if (tasks.length > 0) {
					tasks.forEach((task, index) => {
			content += `${index + 1}. ${task.name || `Task ${index + 1}`}\n`;
		});
		} else {
			content += 'No tasks available\n';
		}

		// 创建文本文件作为后备
		const blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
		const url = URL.createObjectURL(blob);

		const link = document.createElement('a');
		link.href = url;
		link.download = `${(checklist.name || 'checklist').replace(/[^\w\s-]/g, '_')}.txt`;
		link.style.display = 'none';

		document.body.appendChild(link);
		link.click();

		setTimeout(() => {
			document.body.removeChild(link);
			URL.revokeObjectURL(url);
		}, 100);

		console.log('文本文件导出成功');
		ElMessage.info('PDF generation failed, exported as text file instead');
		activeDropdown.value = null;
	} catch (basicErr) {
		console.error('基本导出也失败:', basicErr);

		// 最后的后备方案：打印
		await exportWithPrint(checklist);
	}
};

// 打印方案（最后的后备）
const exportWithPrint = async (checklist) => {
	try {
		// 获取最新的checklist数据（包含任务）
		const updatedChecklist = checklists.value.find((c) => c.id === checklist.id) || checklist;

		// 创建打印窗口
		const printWindow = window.open('', '_blank');
		if (!printWindow) {
			throw new Error('Unable to open print window. Please check popup settings.');
		}

		// 生成PDF内容
		const pdfContent = createPdfContent(updatedChecklist);

		// 写入打印窗口
		printWindow.document.write(pdfContent);
		printWindow.document.close();

		// 等待内容加载完成
		printWindow.onload = () => {
			setTimeout(() => {
				printWindow.print();
				printWindow.close();
			}, 500);
		};

		console.log('打印窗口已打开');
		ElMessage.info('Print dialog opened. You can save as PDF from the print dialog.');
		activeDropdown.value = null;
	} catch (printErr) {
		console.error('打印方案也失败:', printErr);
		throw new Error('All export methods failed');
	}
};

// PDF导出辅助函数
const getWorkflowNameForPdf = (checklist) => {
	if (checklist.workflowName) return checklist.workflowName;
	if (checklist.workflowId) {
		const workflow = workflows.value.find(
			(w) => w.id.toString() === checklist.workflowId.toString()
		);
		return workflow ? workflow.name : 'No workflow specified';
	}
	return 'No workflow specified';
};

const getStageNameForPdf = (checklist) => {
	if (checklist.stageName) return checklist.stageName;
	if (checklist.stageId) {
		const stage = stages.value.find((s) => s.id.toString() === checklist.stageId.toString());
		return stage ? stage.name : 'No stage specified';
	}
	return 'No stage specified';
};

// 创建PDF内容的函数
const createPdfContent = (checklist) => {
	const tasks = checklist.tasks || [];
	console.log('PDF Export - Checklist:', checklist);
	console.log('PDF Export - Tasks:', tasks);

	const tasksHtml =
		tasks.length > 0
			? tasks
					.map(
						(task, index) => `
			<tr>
				<td class="task-cell">${index + 1}</td>
				<td class="task-cell">${task.name || `Task ${index + 1}`}</td>
			</tr>
		`
					)
					.join('')
			: `
			<tr>
				<td class="task-cell" colspan="2" style="text-align: center; color: #9ca3af; font-style: italic;">
					No tasks available
				</td>
			</tr>
		`;

	return `
		<!DOCTYPE html>
		<html>
		<head>
			<meta charset="utf-8">
			<title>${checklist.name} - Checklist</title>
			<style>
				@page {
					size: A4;
					margin: 0;
				}
				
				* {
					margin: 0;
					padding: 0;
					box-sizing: border-box;
				}
				
				body {
					font-family: Arial, sans-serif;
					background: white;
					color: #333;
					line-height: 1.4;
				}
				
				.pdf-container {
					width: 210mm;
					min-height: 297mm;
					padding: 15mm;
					background: white;
				}
				
				.header {
					background: #3b4d66;
					color: white;
					padding: 15px 20px;
					margin: -15mm -15mm 20px -15mm;
					display: flex;
					justify-content: space-between;
					align-items: center;
				}
				
				.header-left {
					font-size: 24px;
					font-weight: bold;
				}
				
				.header-right {
					font-size: 18px;
				}
				
				.title {
					font-size: 24px;
					color: #1f2937;
					margin: 0 0 20px 0;
					font-weight: bold;
				}
				
				.info-section {
					margin-bottom: 25px;
				}
				
				.info-item {
					margin: 6px 0;
					font-size: 14px;
					color: #374151;
				}
				
				.info-label {
					font-weight: bold;
				}
				
				.tasks-table {
					width: 100%;
					border-collapse: collapse;
					margin-top: 15px;
					border: 1px solid #e5e7eb;
				}
				
				.table-header {
					background: #3b4d66;
					color: white;
				}
				
				.header-cell {
					padding: 10px 8px;
					text-align: left;
					font-size: 14px;
					font-weight: bold;
				}
				
				.header-cell:first-child {
					width: 50px;
				}
				
				.task-cell {
					padding: 8px;
					border-bottom: 1px solid #e5e7eb;
					font-size: 12px;
					color: #374151;
				}
				
				@media print {
					body {
						-webkit-print-color-adjust: exact;
						print-color-adjust: exact;
					}
					
					.pdf-container {
						margin: 0;
						padding: 15mm;
					}
				}
			</style>
		</head>
		<body>
			<div class="pdf-container">
				<!-- 头部 -->
				<div class="header">
					<div class="header-left">UNIS</div>
					<div class="header-right">Warehousing Solutions</div>
				</div>

				<!-- 标题 -->
				<h1 class="title">${checklist.name}</h1>

				<!-- 基本信息 -->
				<div class="info-section">
					<div class="info-item">
						<span class="info-label">Description:</span> ${checklist.description || 'No description'}
					</div>
					<div class="info-item">
						<span class="info-label">Team:</span> ${checklist.team || 'No team specified'}
					</div>
					<div class="info-item">
						<span class="info-label">Workflow:</span> ${getWorkflowNameForPdf(checklist)}
					</div>
					<div class="info-item">
						<span class="info-label">Stage:</span> ${getStageNameForPdf(checklist)}
					</div>
				</div>

				<!-- 任务表格 -->
				<table class="tasks-table">
					<thead class="table-header">
						<tr>
							<th class="header-cell" style="width: 50px;">Status</th>
							<th class="header-cell">Task</th>
						</tr>
					</thead>
					<tbody>
						${tasksHtml}
					</tbody>
				</table>
			</div>
		</body>
		</html>
	`;
};

// 对话框管理方法
// 打开创建对话框并设置默认值
const openCreateDialog = async () => {
	showCreateDialog.value = true;
	// 初始化assignments数组，默认包含一个空的assignment
	formData.value.assignments = [
		{
			workflow: '',
			stage: ''
		}
	];
	// 设置默认workflow（只在活跃的workflow中查找）
	const defaultWorkflow = filteredWorkflows.value.find((w) => w.isDefault);
	if (defaultWorkflow) {
		formData.value.workflow = defaultWorkflow.name;
		// 触发workflow变化处理
		await handleWorkflowChange();
	}
};

const closeCreateDialog = () => {
	showCreateDialog.value = false;
	formData.value = {
		name: '',
		description: '',
		team: '',
		workflow: '',
		stage: '',
		assignments: [
			{
				workflow: '',
				stage: ''
			}
		],
	};
};

const createChecklistItem = async () => {
	if (!formData.value.name.trim() || !formData.value.team) return;

	createLoading.value = true;
	try {
		console.log('Creating checklist with data:', formData.value);
		
		// 处理assignments，转换为后端需要的格式
		const assignments = formData.value.assignments.map(assignment => {
			const workflowId = filteredWorkflows.value.find((w) => w.name === assignment.workflow)?.id || '';
			const stageId = stages.value.find((s) => s.name === assignment.stage)?.id || '';
			return {
				workflowId: String(workflowId),
				stageId: String(stageId)
			};
		}).filter(assignment => assignment.workflowId && assignment.stageId);

		const checklistData = {
			name: formData.value.name.trim(),
			description: formData.value.description || '',
			team: formData.value.team,
			type: 'Instance',
			status: 'Active',
			isTemplate: false,
			isActive: true,
			assignments: assignments,
		};

		const newChecklist = await createChecklist(checklistData);
		console.log('Checklist created successfully:', newChecklist);

		ElMessage.success(t('sys.api.operationSuccess'));
		closeCreateDialog();

		// 创建成功后刷新页面数据
		console.log('Refreshing checklist data after creation...');
		await loadChecklists();
	} catch (err) {
		console.error('Failed to create checklist:', err);
		ElMessage.error(t('sys.api.operationFailed'));
		closeCreateDialog();

		// 即使创建失败，也刷新一下数据，可能后端已经创建成功了
		console.log('Refreshing checklist data after creation error...');
		await loadChecklists();
	} finally {
		createLoading.value = false;
	}
};

const closeEditDialog = () => {
	showEditDialog.value = false;
	editingChecklist.value = null;
	formData.value = {
		name: '',
		description: '',
		team: '',
		workflow: '',
		stage: '',
		assignments: [
			{
				workflow: '',
				stage: ''
			}
		],
	};
};

const saveEditChecklist = async () => {
	if (!formData.value.name.trim() || !formData.value.team || !editingChecklist.value) return;

	editLoading.value = true;
	const originalChecklistId = editingChecklist.value.id;

	try {
		console.log('Updating checklist with data:', formData.value);
		
		// 处理assignments，转换为后端需要的格式
		const assignments = formData.value.assignments.map(assignment => {
			const workflowId = filteredWorkflows.value.find((w) => w.name === assignment.workflow)?.id || '';
			const stageId = stages.value.find((s) => s.name === assignment.stage)?.id || '';
			return {
				workflowId: String(workflowId),
				stageId: String(stageId)
			};
		}).filter(assignment => assignment.workflowId && assignment.stageId);

		const checklistData = {
			name: formData.value.name.trim(),
			description: formData.value.description || '',
			team: formData.value.team,
			type: editingChecklist.value.type || 'Instance',
			status: editingChecklist.value.status || 'Active',
			isTemplate: editingChecklist.value.isTemplate || false,
			isActive: editingChecklist.value.isActive !== false,
			assignments: assignments,
		};

		await updateChecklist(originalChecklistId, checklistData);
		console.log('Checklist updated successfully');

		ElMessage.success('Checklist updated successfully');
		closeEditDialog();

		// 更新成功后立即刷新页面数据
		console.log('Refreshing checklist data after update...');
		console.log('Checklists before refresh:', checklists.value.length);
		await loadChecklists();
		console.log('Checklists after refresh:', checklists.value.length);

		// 验证更新是否生效
		const updatedChecklist = checklists.value.find((c) => c.id === originalChecklistId);
		if (updatedChecklist) {
			console.log('Updated checklist found:', updatedChecklist.name);
		} else {
			console.warn('Updated checklist not found after refresh!');
		}

		// 如果编辑的checklist当前是展开状态，保持展开并强制重新加载任务
		if (expandedChecklists.value.includes(originalChecklistId)) {
			console.log('Force reloading tasks for updated checklist:', originalChecklistId);
			await loadChecklistTasks(originalChecklistId, true);
		}

		console.log('Checklist update and refresh completed');
	} catch (err) {
		console.error('Failed to update checklist:', err);

		// 提供更详细的错误信息
		let errorMessage = 'Failed to update checklist';
		if (err.response?.status === 404) {
			errorMessage = 'Checklist not found';
		} else if (err.response?.status === 403) {
			errorMessage = 'You do not have permission to update this checklist';
		} else if (err.response?.status === 400) {
			errorMessage = 'Invalid checklist data';
		} else if (err.message) {
			errorMessage = `Update failed: ${err.message}`;
		}

		ElMessage.error(errorMessage);
		closeEditDialog();

		// 即使更新失败，也刷新一下数据，可能后端已经更新成功了
		console.log('Refreshing checklist data after update error...');
		await loadChecklists();

		// 如果编辑的checklist当前是展开状态，强制重新加载任务
		if (expandedChecklists.value.includes(originalChecklistId)) {
			await loadChecklistTasks(originalChecklistId, true);
		}
	} finally {
		editLoading.value = false;
	}
};

// 点击外部关闭下拉菜单
const handleClickOutside = (event) => {
	// 检查点击是否在下拉菜单或触发按钮外部
	const target = event.target;
	const isClickInsideDropdown = target.closest('.dropdown-menu');
	const isClickOnTrigger = target.closest('[data-checklist-id]');

	if (!isClickInsideDropdown && !isClickOnTrigger) {
		activeDropdown.value = null;
	}
};

// 任务编辑方法
const editTask = (checklistId, task) => {
	// 如果已经在编辑状态，则先取消之前的编辑
	if (editingTask.value) {
		if (editingTask.value.id === task.id) {
			// 如果点击的是同一个任务，则取消编辑
			editingTask.value = null;
			editingTaskChecklistId.value = null;
			return;
		}
	}

	editingTaskChecklistId.value = checklistId;
	editingTask.value = task;
	taskFormData.value = {
		name: task.name,
		description: task.description || '',
		estimatedMinutes: task.estimatedMinutes || 0,
		isRequired: task.isRequired !== false,
	};
};

const cancelTaskEdit = () => {
	editingTask.value = null;
	editingTaskChecklistId.value = null;
	taskFormData.value = {
		name: '',
		description: '',
		estimatedMinutes: 0,
		isRequired: false,
	};
};

const saveTaskEdit = async () => {
	if (!taskFormData.value.name.trim() || !editingTask.value || !editingTaskChecklistId.value)
		return;

	try {
		const taskData = formatTaskForApi({
			checklistId: editingTaskChecklistId.value,
			id: editingTask.value.id,
			name: taskFormData.value.name.trim(),
			description: taskFormData.value.description || '',
			isRequired: taskFormData.value.isRequired,
			estimatedMinutes: taskFormData.value.estimatedMinutes || 0,
			order: editingTask.value.order || 0,
		});

		await updateChecklistTask(editingTask.value.id, taskData);
		ElMessage.success(t('sys.api.operationSuccess'));

		// 重新加载该清单的任务
		const checklist = checklists.value.find((c) => c.id === editingTaskChecklistId.value);
		if (checklist) {
			const tasks = await getChecklistTasks(editingTaskChecklistId.value);
			const processedTasks = (tasks.data || tasks || []).map((task) => ({
				...task,
				completed: task.isCompleted || task.completed || false,
				estimatedMinutes: task.estimatedHours ? task.estimatedHours * 60 : 0,
			}));

			// 使用Object.assign确保响应式更新
			Object.assign(checklist, {
				tasks: processedTasks,
				tasksLoaded: true,
			});

			// 强制触发响应式更新
			checklists.value = [...checklists.value];
		}

		cancelTaskEdit();
	} catch (err) {
		console.error('Failed to update task:', err);
		ElMessage.error(t('sys.api.operationFailed'));
		// 后备方案：本地更新
		const checklist = checklists.value.find((c) => c.id === editingTaskChecklistId.value);
		if (checklist && editingTask.value) {
			const taskIndex = checklist.tasks.findIndex((t) => t.id === editingTask.value.id);
			if (taskIndex !== -1) {
				checklist.tasks[taskIndex] = {
					...checklist.tasks[taskIndex],
					name: taskFormData.value.name,
					description: taskFormData.value.description,
					estimatedMinutes: taskFormData.value.estimatedMinutes,
					isRequired: taskFormData.value.isRequired,
				};
				// 强制触发响应式更新
				checklists.value = [...checklists.value];
			}
		}
		cancelTaskEdit();
	}
};

onMounted(async () => {
	document.addEventListener('click', handleClickOutside);
	// 并行加载workflows/stages和checklists，提高加载速度
	await Promise.all([loadWorkflowsAndStages(), loadChecklists()]);
});

onUnmounted(() => {
	// 清理事件监听器
	document.removeEventListener('click', handleClickOutside);

	// 清理搜索防抖定时器
	if (searchTimeout) {
		clearTimeout(searchTimeout);
		searchTimeout = null;
	}

	// 清理任务加载缓存
	taskLoadingCache.clear();
});

const addAssignment = () => {
	formData.value.assignments.push({
		workflow: '',
		stage: '',
	});
};

const removeAssignment = (index) => {
	// 确保至少保留一个 assignment
	if (formData.value.assignments.length > 1) {
		formData.value.assignments.splice(index, 1);
	}
};

const handleWorkflowChangeForAssignment = async (index) => {
	// 清空当前选择的stage
	formData.value.assignments[index].stage = '';
	// 根据选择的workflow加载对应的stages
	await loadStagesByWorkflow(formData.value.assignments[index].workflow);
};

const getStagesForAssignment = (workflowName) => {
	if (!workflowName) return [];
	const selectedWorkflow = filteredWorkflows.value.find(
		(w) => w.name === workflowName
	);

	if (!selectedWorkflow) return [];

	const filtered = stages.value.filter((stage) => {
		return stage.workflowId && stage.workflowId.toString() === selectedWorkflow.id.toString();
	});

	return filtered;
};
</script>

<style scoped>
/* 自定义样式 */
.bg-gradient-to-r {
	background: linear-gradient(to right, #e9d5ff, #bfdbfe);
}

/* 拖拽样式 */
.dragging {
	opacity: 0.6;
	background-color: #f3f4f6 !important;
	border: 2px dashed #3b82f6 !important;
	transform: rotate(2deg);
	box-shadow: 0 4px 8px rgba(0, 0, 0, 0.15);
}

.drag-over {
	border: 2px solid #3b82f6 !important;
	background-color: #eff6ff !important;
	transform: scale(1.02);
}

/* 拖拽手柄样式 */
.drag-handle {
	transition: all 0.2s ease;
}

.drag-handle:hover {
	background-color: #e5e7eb !important;
	color: #374151 !important;
}

.dragging .drag-handle {
	color: #3b82f6 !important;
}

/* 下拉菜单样式 */
.dropdown-menu {
	max-height: 250px;
	overflow-y: auto;
	box-shadow:
		0 10px 15px -3px rgba(0, 0, 0, 0.1),
		0 4px 6px -2px rgba(0, 0, 0, 0.05);
}

.dropdown-menu button:hover {
	background-color: #f8fafc;
}
</style>

<style>
/* 自定义确认删除弹窗样式 */
.custom-confirm-dialog {
	border-radius: 8px;
}

.custom-confirm-dialog .el-message-box__header {
	padding: 20px 20px 10px;
}

.custom-confirm-dialog .el-message-box__title {
	font-size: 18px;
	font-weight: 600;
	color: #1f2937;
}

.custom-confirm-dialog .el-message-box__content {
	padding: 10px 20px 20px;
}

.custom-confirm-dialog .el-message-box__message {
	font-size: 14px;
	color: #6b7280;
	line-height: 1.5;
}

.custom-confirm-dialog .el-message-box__btns {
	padding: 10px 20px 20px;
	text-align: right;
}

.custom-confirm-dialog .el-message-box__btns .el-button {
	margin-left: 12px;
	padding: 8px 16px;
	font-size: 14px;
	border-radius: 6px;
}

.custom-confirm-dialog .el-message-box__btns .el-button--default {
	background-color: #ffffff;
	border-color: #d1d5db;
	color: #374151;
}

.custom-confirm-dialog .el-message-box__btns .el-button--default:hover {
	background-color: #f9fafb;
	border-color: #9ca3af;
}

.custom-confirm-dialog .el-message-box__btns .el-button--danger {
	background-color: #ef4444;
	border-color: #ef4444;
	color: #ffffff;
}

.custom-confirm-dialog .el-message-box__btns .el-button--danger:hover {
	background-color: #dc2626;
	border-color: #dc2626;
}
</style>
