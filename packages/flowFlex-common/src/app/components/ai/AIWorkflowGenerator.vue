<template>
	<div class="ai-workflow-assistant">
		<el-card shadow="hover" class="assistant-card">
			<template #header>
				<div class="ai_card-header">
					<div class="header-left">
						<span class="assistant-title">AI Workflow Assistant</span>
						<span v-if="currentAIModel" class="status-indicator">
							<span class="pulse-dot"></span>
							{{
								`${currentAIModel.provider.toLowerCase()} ${
									currentAIModel.modelName
								}`
							}}
						</span>
					</div>
				</div>
			</template>

			<div class="assistant-container">
				<!-- AI Model Setup Guide -->
				<div v-if="!currentAIModel && !isLoadingModel" class="model-setup-guide">
					<div class="guide-content">
						<h3 class="guide-title">AI Model Configuration Required</h3>
						<p class="guide-description">
							To use the AI Workflow Assistant, you need to configure an AI model
							first.
						</p>
						<el-button
							type="primary"
							size="large"
							class="guide-action-btn"
							@click="navigateToAIModelConfig"
						>
							<el-icon><Setting /></el-icon>
							<span>Configure AI Model Now</span>
						</el-button>
					</div>
				</div>

				<!-- Chat Area -->
				<div v-else class="chat-area">
					<!-- Chat Messages -->
					<div class="chat-messages" ref="chatMessagesRef">
						<div
							v-for="(message, index) in chatMessages"
							:key="index"
							class="message-item"
						>
							<!-- User Message -->
							<div v-if="message.type === 'user'" class="user-message">
								<div class="message-content">
									<div class="message-text">{{ message.content }}</div>
									<div class="message-time">
										{{ formatTime(message.timestamp) }}
									</div>
								</div>
								<div class="message-avatar">
									<el-icon><User /></el-icon>
								</div>
							</div>

							<!-- AI Message -->
							<div
								v-else-if="
									message.type === 'ai' &&
									message.content &&
									message.content.trim()
								"
								class="ai-message"
							>
								<div class="message-avatar">
									<el-icon><Star /></el-icon>
								</div>
								<div class="message-content">
									<div
										class="message-text"
										v-html="formatAIMessage(message.content)"
									></div>
									<div class="message-time">
										{{ formatTime(message.timestamp) }}
									</div>
								</div>
							</div>

							<!-- Generation Complete -->
							<div
								v-else-if="message.type === 'generation-complete'"
								class="generation-complete"
							>
								<div class="complete-header">
									<el-icon class="success-icon"><CircleCheckFilled /></el-icon>
									<h4>Generation Complete</h4>
								</div>

								<!-- Workflow Preview -->
								<div class="workflow-preview">
									<div class="workflow-info">
										<div class="workflow-header">
											<el-input
												v-if="message.data?.workflow"
												v-model="message.data.workflow.name"
												class="workflow-name-input"
												placeholder="Enter workflow name"
												@blur="onWorkflowUpdated(message.data!)"
											/>
											<span
												class="status"
												:class="{
													active: message.data?.workflow?.isActive,
												}"
											>
												{{
													message.data?.workflow?.isActive
														? 'Active'
														: 'Inactive'
												}}
											</span>
										</div>
										<el-input
											v-if="message.data?.workflow"
											v-model="message.data.workflow.description"
											type="textarea"
											class="workflow-description-input"
											placeholder="Enter workflow description"
											:rows="2"
											@blur="onWorkflowUpdated(message.data!)"
										/>
										<div class="workflow-stats">
											<span class="stat-item">
												<el-icon><List /></el-icon>
												{{ message.data?.stages?.length || 0 }} stages
											</span>
											<span class="stat-item">
												<el-icon><Clock /></el-icon>
												{{ getTotalDuration(message.data?.stages || []) }}
												days
											</span>
										</div>
									</div>

									<!-- Stages Grid -->
									<div class="stages-grid">
										<div
											v-for="(stage, stageIndex) in message.data?.stages ||
											[]"
											:key="stageIndex"
											class="stage-card"
										>
											<div class="stage- ai_card-header">
												<div class="stage-badge">
													<span class="stage-number">
														{{ stage.order }}
													</span>
												</div>
												<el-button
													size="small"
													type="danger"
													@click="removeStage(message.data!, stageIndex)"
													class="remove-stage-btn"
													circle
												>
													<el-icon><Close /></el-icon>
												</el-button>
											</div>

											<div class="stage-card-content">
												<el-input
													v-model="stage.name"
													size="small"
													class="stage-title-input"
													placeholder="Stage name..."
													@blur="
														onStageUpdated(message.data!, stageIndex)
													"
												/>

												<el-input
													v-model="stage.description"
													type="textarea"
													:rows="2"
													size="small"
													placeholder="Stage description..."
													@blur="
														onStageUpdated(message.data!, stageIndex)
													"
													class="stage-description"
												/>

												<div class="stage-meta-compact">
													<div class="meta-item">
														<span class="meta-label">Team:</span>
														<el-select
															v-model="stage.assignedGroup"
															size="small"
															placeholder="Select"
															class="meta-select"
														>
															<el-option
																label="Sales"
																value="Sales"
															/>
															<el-option label="IT" value="IT" />
															<el-option label="HR" value="HR" />
															<el-option
																label="Finance"
																value="Finance"
															/>
															<el-option
																label="Operations"
																value="Operations"
															/>
														</el-select>
													</div>
													<div class="meta-item">
														<span class="meta-label">Days:</span>
														<el-input-number
															v-model="stage.estimatedDuration"
															size="small"
															:min="1"
															:max="30"
															controls-position="right"
															class="meta-number"
														/>
													</div>
												</div>
											</div>
										</div>
									</div>

									<!-- Add Stage Button -->
									<el-button
										@click="addStage(message.data)"
										type="default"
										class="add-stage-btn"
									>
										<el-icon><Plus /></el-icon>
										Add Stage
									</el-button>

									<!-- Checklists & Questionnaires -->
									<div class="additional-components">
										<div class="component-section">
											<div class="section-header">
												<h6>Checklists</h6>
												<el-button
													size="small"
													link
													@click="toggleChecklistsCollapse"
													class="collapse-toggle"
												>
													<el-icon>
														<ArrowDown v-if="checklistsCollapsed" />
														<ArrowUp v-else />
													</el-icon>
													{{
														checklistsCollapsed ? 'Expand' : 'Collapse'
													}}
												</el-button>
											</div>

											<el-collapse-transition>
												<div
													v-show="!checklistsCollapsed"
													class="checklists-grid"
												>
													<div
														v-for="(checklist, clIndex) in message.data
															?.checklists || []"
														:key="clIndex"
														class="checklist-card"
													>
														<div class="checklist-header">
															<div class="checklist-name-section">
																<el-icon class="stage-icon">
																	<List />
																</el-icon>
																<span class="name-label">
																	checklist:
																</span>
																<el-input
																	v-model="checklist.name"
																	class="checklist-name-input"
																	placeholder="Enter checklist name"
																	@blur="
																		onChecklistUpdated(
																			message.data!,
																			clIndex
																		)
																	"
																/>
															</div>
															<div class="checklist-title-row">
																<div class="checklist-title">
																	<span class="stage-label">
																		stage:
																	</span>
																	<span class="stage-name">
																		{{
																			getStageNameFromChecklist(
																				checklist.name
																			)
																		}}
																	</span>
																</div>
																<div class="card-actions">
																	<el-button
																		size="small"
																		link
																		@click="
																			toggleChecklistTasks(
																				clIndex
																			)
																		"
																		class="expand-btn"
																	>
																		<el-icon>
																			<ArrowDown
																				v-if="
																					isChecklistTasksCollapsed(
																						clIndex
																					)
																				"
																			/>
																			<ArrowUp v-else />
																		</el-icon>
																	</el-button>
																	<el-button
																		size="small"
																		type="danger"
																		@click="
																			removeChecklist(
																				message.data,
																				clIndex
																			)
																		"
																		circle
																	>
																		<el-icon><Close /></el-icon>
																	</el-button>
																</div>
															</div>
														</div>

														<el-collapse-transition>
															<div
																v-show="
																	!isChecklistTasksCollapsed(
																		clIndex
																	)
																"
																class="tasks-list"
															>
																<div
																	v-for="task in checklist.tasks ||
																	[]"
																	:key="task.id"
																	class="task-item"
																>
																	<div class="task-header">
																		<el-checkbox
																			v-model="task.completed"
																			:disabled="true"
																		/>
																		<span
																			class="task-title"
																			:class="{
																				required:
																					task.isRequired,
																			}"
																		>
																			{{ task.title }}
																			<el-tag
																				v-if="
																					task.isRequired
																				"
																				size="small"
																				type="warning"
																			>
																				Required
																			</el-tag>
																		</span>
																	</div>
																	<p class="task-description">
																		{{ task.description }}
																	</p>
																</div>
															</div>
														</el-collapse-transition>
													</div>
												</div>
											</el-collapse-transition>

											<el-button
												size="small"
												type="default"
												@click="addChecklist(message.data)"
											>
												<el-icon><Plus /></el-icon>
												Add Checklist
											</el-button>
										</div>

										<div class="component-section">
											<div class="section-header">
												<h6>Questionnaires</h6>
												<el-button
													size="small"
													link
													@click="toggleQuestionnairesCollapse"
													class="collapse-toggle"
												>
													<el-icon>
														<ArrowDown v-if="questionnairesCollapsed" />
														<ArrowUp v-else />
													</el-icon>
													{{
														questionnairesCollapsed
															? 'Expand'
															: 'Collapse'
													}}
												</el-button>
											</div>

											<el-collapse-transition>
												<div
													v-show="!questionnairesCollapsed"
													class="questionnaires-grid"
												>
													<div
														v-for="(questionnaire, qIndex) in message
															.data?.questionnaires || []"
														:key="qIndex"
														class="questionnaire-card"
													>
														<div class="questionnaire-header">
															<div class="questionnaire-name-section">
																<el-icon class="stage-icon">
																	<Document />
																</el-icon>
																<span class="name-label">
																	questionnaire:
																</span>
																<el-input
																	v-model="questionnaire.name"
																	class="questionnaire-name-input"
																	placeholder="Enter questionnaire name"
																	@blur="
																		onQuestionnaireUpdated(
																			message.data!,
																			qIndex
																		)
																	"
																/>
															</div>
															<div class="questionnaire-title-row">
																<div class="questionnaire-title">
																	<span class="stage-label">
																		stage:
																	</span>
																	<span class="stage-name">
																		{{
																			getStageNameFromQuestionnaire(
																				questionnaire.name
																			)
																		}}
																	</span>
																</div>
																<div class="card-actions">
																	<el-button
																		size="small"
																		link
																		@click="
																			toggleQuestionnaireQuestions(
																				qIndex
																			)
																		"
																		class="expand-btn"
																	>
																		<el-icon>
																			<ArrowDown
																				v-if="
																					isQuestionnaireQuestionsCollapsed(
																						qIndex
																					)
																				"
																			/>
																			<ArrowUp v-else />
																		</el-icon>
																	</el-button>
																	<el-button
																		size="small"
																		type="danger"
																		@click="
																			removeQuestionnaire(
																				message.data,
																				qIndex
																			)
																		"
																		circle
																	>
																		<el-icon><Close /></el-icon>
																	</el-button>
																</div>
															</div>
														</div>

														<el-collapse-transition>
															<div
																v-show="
																	!isQuestionnaireQuestionsCollapsed(
																		qIndex
																	)
																"
																class="questions-list"
															>
																<div
																	v-for="question in questionnaire.questions ||
																	[]"
																	:key="question.id"
																	class="question-item"
																>
																	<div class="question-header">
																		<span
																			class="question-text"
																			:class="{
																				required:
																					question.isRequired,
																			}"
																		>
																			{{ question.question }}
																			<el-tag
																				v-if="
																					question.isRequired
																				"
																				size="small"
																				type="warning"
																			>
																				Required
																			</el-tag>
																		</span>
																		<el-tag
																			size="small"
																			type="info"
																		>
																			{{ question.type }}
																		</el-tag>
																	</div>
																	<div
																		v-if="
																			question.options &&
																			question.options
																				.length > 0
																		"
																		class="question-options"
																	>
																		<el-tag
																			v-for="option in question.options"
																			:key="option"
																			size="small"
																			class="option-tag"
																		>
																			{{ option }}
																		</el-tag>
																	</div>
																</div>
															</div>
														</el-collapse-transition>
													</div>
												</div>
											</el-collapse-transition>

											<el-button
												size="small"
												type="default"
												@click="addQuestionnaire(message.data)"
											>
												<el-icon><Plus /></el-icon>
												Add Questionnaire
											</el-button>
										</div>
									</div>

									<!-- Apply Button -->
									<div class="apply-section">
										<el-button
											type="primary"
											size="large"
											@click="applyWorkflow(message.data!)"
											:loading="applying"
											class="apply-btn"
										>
											<el-icon class="mr-1"><Check /></el-icon>
											Apply Workflow
										</el-button>
									</div>
								</div>
							</div>

							<!-- Workflow Modification Message -->
							<div
								v-else-if="message.type === 'workflow-modification'"
								class="workflow-modification"
							>
								<div class="message-avatar">
									<el-icon><Edit /></el-icon>
								</div>
								<div class="message-content">
									<div class="workflow-header">
										<h4>{{ message.content }}</h4>
									</div>

									<!-- Workflow Info -->
									<div class="workflow-info-card">
										<div class="workflow-details">
											<h5>{{ message.data?.workflow?.name }}</h5>
											<p>{{ message.data?.workflow?.description }}</p>
											<div class="workflow-meta">
												<span
													class="status"
													:class="{
														active: message.data?.workflow?.isActive,
													}"
												>
													{{
														message.data?.workflow?.isActive
															? 'Active'
															: 'Inactive'
													}}
												</span>
												<span class="stage-count">
													{{ message.data?.stages?.length || 0 }} stages
												</span>
											</div>
										</div>
									</div>

									<!-- Stages List -->
									<div
										v-if="
											message.data?.stages && message.data.stages.length > 0
										"
										class="stages-list"
									>
										<h6>Workflow Stages:</h6>
										<div class="stages-container">
											<div
												v-for="(stage, stageIndex) in message.data.stages"
												:key="stageIndex"
												class="stage-card editable"
											>
												<div class="stage-header">
													<span class="stage-number">
														{{ stage.order || stageIndex + 1 }}
													</span>
													<el-input
														v-model="stage.name"
														size="small"
														class="stage-name-input"
														@blur="
															onStageUpdated(
																message.data!,
																stageIndex
															)
														"
													/>
												</div>
												<el-input
													v-model="stage.description"
													type="textarea"
													:rows="2"
													size="small"
													class="stage-description-input"
													@blur="
														onStageUpdated(message.data!, stageIndex)
													"
												/>

												<div class="stage-details">
													<div class="stage-field">
														<label>Assigned Team:</label>
														<el-select
															v-model="stage.assignedGroup"
															size="small"
															@change="
																onStageUpdated(
																	message.data!,
																	stageIndex
																)
															"
														>
															<el-option
																label="Sales"
																value="Sales"
															/>
															<el-option
																label="Finance"
																value="Finance"
															/>
															<el-option
																label="Operations"
																value="Operations"
															/>
														</el-select>
													</div>
													<div class="stage-field">
														<label>Duration (days):</label>
														<el-input-number
															v-model="stage.estimatedDuration"
															size="small"
															:min="1"
															@change="
																onStageUpdated(
																	message.data!,
																	stageIndex
																)
															"
														/>
													</div>
												</div>
											</div>
										</div>

										<!-- Action Buttons -->
										<div class="save-section">
											<el-button
												type="primary"
												@click="saveWorkflowChanges(message.data!)"
												:loading="applying"
											>
												<el-icon><Check /></el-icon>
												Save Changes
											</el-button>
										</div>
									</div>
								</div>
							</div>

							<!-- Workflow Selection Message -->
							<div
								v-else-if="message.type === 'workflow-selection'"
								class="workflow-selection"
							>
								<div class="message-avatar">
									<el-icon><Search /></el-icon>
								</div>
								<div class="message-content">
									<div class="selection-header">
										<h4>{{ message.content }}</h4>
									</div>

									<div class="workflows-list">
										<div
											v-for="(workflow, workflowIndex) in message.data
												?.workflows"
											:key="workflowIndex"
											class="workflow-option"
											@click="selectWorkflowForModification(workflow)"
										>
											<div class="workflow-option-content">
												<h5>{{ workflow.name }}</h5>
												<p>{{ workflow.description }}</p>
												<div class="workflow-option-meta">
													<span
														class="status"
														:class="{ active: workflow.isActive }"
													>
														{{
															workflow.isActive
																? 'Active'
																: 'Inactive'
														}}
													</span>
													<span class="created-date">
														{{
															formatTime(
																new Date(
																	workflow.createdAt ||
																		workflow.createDate
																)
															)
														}}
													</span>
												</div>
											</div>
											<el-icon class="select-icon"><ArrowRight /></el-icon>
										</div>
									</div>
								</div>
							</div>
						</div>

						<!-- Streaming Message -->
						<div v-if="shouldShowStreamingMessage" class="ai-message streaming">
							<div class="message-avatar">
								<el-icon><Star /></el-icon>
							</div>
							<div class="message-content">
								<div class="message-text">
									{{ streamingMessage || 'Processing...' }}
									<span class="typing-indicator">|</span>
								</div>
								<div class="message-time">
									{{ formatTime(new Date()) }}
								</div>
							</div>
						</div>
					</div>

					<!-- Input Area -->
					<div class="input-area">
						<!-- Text Input -->
						<div class="text-input-section">
							<div class="input-with-button">
								<div class="input-container">
									<el-input
										v-model="currentInput"
										type="textarea"
										:rows="3"
										placeholder="Type your response here..."
										@keydown="handleKeydown"
										class="chat-input"
									/>
									<div class="input-bottom-actions">
										<div class="ai-model-selector-bottom">
											<el-select
												v-model="currentAIModel"
												placeholder="Select AI Model"
												size="small"
												class="model-select"
												style="width: 180px"
												value-key="id"
												@change="handleModelChange"
											>
												<el-option
													v-for="model in availableModels"
													:key="model.id"
													:label="`${model.provider.toLowerCase()} ${
														model.modelName
													}`"
													:value="model"
													:disabled="!model.isAvailable"
												>
													<div class="model-option">
														<div class="model-info">
															<span class="model-display">
																{{ model.provider.toLowerCase() }}
																{{ model.modelName }}
															</span>
														</div>
														<div class="model-status">
															<span
																class="status-dot"
																:class="{
																	online: model.isAvailable,
																	offline: !model.isAvailable,
																}"
															></span>
														</div>
													</div>
												</el-option>
											</el-select>
										</div>
									</div>
									<div class="input-right-actions">
										<AIFileAnalyzer
											:model-config="currentAIModel"
											@file-analyzed="handleFileAnalyzed"
											@analysis-complete="handleAnalysisComplete"
											@stream-chunk="handleStreamChunk"
											@analysis-started="handleAnalysisStarted"
										/>
										<el-button
											type="primary"
											@click="sendMessage"
											:disabled="!currentInput.trim() && !uploadedFile"
											size="small"
											class="send-button"
											circle
										>
											<svg
												xmlns="http://www.w3.org/2000/svg"
												viewBox="0 0 1024 1024"
												class="send-icon"
											>
												<path
													fill="currentColor"
													d="m249.6 417.088 319.744 43.072 39.168 310.272L845.12 178.88 249.6 417.088zm-129.024 47.168a32 32 0 0 1-7.68-61.44l777.792-311.04a32 32 0 0 1 41.6 41.6l-310.336 775.68a32 32 0 0 1-61.44-7.808L512 516.992l-391.424-52.736z"
												/>
											</svg>
										</el-button>
									</div>
								</div>
							</div>
						</div>

						<!-- Uploaded File Display -->
						<div v-if="uploadedFile" class="uploaded-file-display">
							<div class="file-info">
								<el-icon
									class="file-icon"
									:class="{
										'pdf-icon': isPDFFile(uploadedFile),
										'word-icon': isWordFile(uploadedFile),
										'image-icon': isImageFile(uploadedFile),
									}"
								>
									<Picture v-if="isImageFile(uploadedFile)" />
									<Document v-else />
								</el-icon>
								<span class="file-name">{{ uploadedFile.name }}</span>
								<span class="file-type-badge" v-if="isPDFFile(uploadedFile)">
									PDF
								</span>
								<span
									class="file-type-badge word"
									v-else-if="isWordFile(uploadedFile)"
								>
									WORD
								</span>
								<el-button
									size="small"
									link
									@click="removeUploadedFile"
									class="remove-file-btn"
								>
									<el-icon><Close /></el-icon>
								</el-button>
							</div>
							<!-- File Preview -->
							<div v-if="uploadedFile" class="file-preview">
								<!-- Image Preview -->
								<div v-if="isImageFile(uploadedFile)" class="image-preview">
									<img
										:src="getFilePreviewUrl(uploadedFile)"
										:alt="uploadedFile.name"
										class="preview-image"
									/>
								</div>

								<!-- PDF Preview -->
								<div
									v-else-if="isPDFFile(uploadedFile)"
									class="document-preview pdf-preview"
								>
									<div class="preview-header">
										<el-icon class="preview-icon"><Document /></el-icon>
										<div class="preview-info">
											<span class="preview-title">PDF Document</span>
											<span class="preview-subtitle">
												{{ formatFileSize(uploadedFile.size) }}
											</span>
										</div>
									</div>
									<div class="preview-description">
										Click "Send to AI Chat" to analyze this PDF document
									</div>
								</div>

								<!-- Word Preview -->
								<div
									v-else-if="isWordFile(uploadedFile)"
									class="document-preview word-preview"
								>
									<div class="preview-header">
										<el-icon class="preview-icon"><Document /></el-icon>
										<div class="preview-info">
											<span class="preview-title">Word Document</span>
											<span class="preview-subtitle">
												{{ formatFileSize(uploadedFile.size) }}
											</span>
										</div>
									</div>
									<div class="preview-description">
										Click "Send to AI Chat" to analyze this Word document
									</div>
								</div>

								<!-- Other File Preview -->
								<div v-else class="document-preview generic-preview">
									<div class="preview-header">
										<el-icon class="preview-icon"><Document /></el-icon>
										<div class="preview-info">
											<span class="preview-title">
												{{ getFileTypeName(uploadedFile) }}
											</span>
											<span class="preview-subtitle">
												{{ formatFileSize(uploadedFile.size) }}
											</span>
										</div>
									</div>
									<div class="preview-description">
										Click "Send to AI Chat" to analyze this document
									</div>
								</div>
							</div>
						</div>
					</div>
				</div>

				<!-- Enhanced Chat History Sidebar -->
				<div class="chat-history" :class="{ collapsed: isHistoryCollapsed }">
					<div class="history-header">
						<div class="header-content">
							<div v-if="!isHistoryCollapsed" class="header-title-section">
								<h4>Chat History</h4>
								<span class="history-count">{{ chatHistory.length }} sessions</span>
							</div>
							<div class="header-actions">
								<el-button
									v-if="!isHistoryCollapsed"
									size="small"
									type="primary"
									@click="startNewChat"
									class="new-chat-btn"
								>
									<el-icon><Plus /></el-icon>
									New Chat
								</el-button>
								<el-dropdown
									v-if="!isHistoryCollapsed && chatHistory.length > 0"
									trigger="click"
									class="history-menu"
								>
									<el-button size="small" link class="menu-btn">
										<el-icon><MoreFilled /></el-icon>
									</el-button>
									<template #dropdown>
										<el-dropdown-menu>
											<el-dropdown-item @click="clearAllHistory">
												<el-icon><Delete /></el-icon>
												Clear All History
											</el-dropdown-item>
										</el-dropdown-menu>
									</template>
								</el-dropdown>
							</div>
						</div>

						<!-- Search Bar -->
						<div
							v-if="!isHistoryCollapsed && chatHistory.length > 0"
							class="history-search"
						>
							<el-input
								v-model="historySearchQuery"
								placeholder="Search chat history..."
								size="small"
								clearable
							>
								<template #prefix>
									<el-icon><Search /></el-icon>
								</template>
							</el-input>
						</div>
					</div>

					<div class="history-list" v-if="!isHistoryCollapsed">
						<!-- Pinned Sessions -->
						<div v-if="pinnedSessions.length > 0" class="pinned-section">
							<div class="section-header">
								<el-icon><Star /></el-icon>
								<span>Pinned</span>
							</div>
							<div
								v-for="session in pinnedSessions"
								:key="session.id"
								:class="[
									'history-item',
									'pinned',
									{ active: currentSessionId === session.id },
								]"
								@click="loadChatSession(session.id)"
								@contextmenu.prevent="showContextMenu($event, session)"
							>
								<div class="item-content">
									<div class="history-title">
										<svg
											v-if="isFileSession(session)"
											xmlns="http://www.w3.org/2000/svg"
											xmlns:xlink="http://www.w3.org/1999/xlink"
											aria-hidden="true"
											role="img"
											class="attachment-icon"
											width="1em"
											height="1em"
											viewBox="0 0 1024 1024"
										>
											<path
												d="M239.08352 319.0784a188.17024 188.17024 0 0 1 376.29952 0v0.16384l4.62848 347.62752v0.08192a112.64 112.64 0 0 1-156.0576 105.63584 112.55808 112.55808 0 0 1-68.97664-105.39008V315.14624a36.864 36.864 0 1 1 73.728 0v352.99328a38.83008 38.83008 0 1 0 77.57824 0v-0.16384l-4.58752-347.58656V320.3072a114.4832 114.4832 0 0 0-228.88448-0.4096l4.5056 347.58656a190.13632 190.13632 0 1 0 380.3136 0l0.4096-334.39744a36.864 36.864 0 1 1 73.728 0.08192l-0.4096 334.31552a263.90528 263.90528 0 0 1-450.43712 186.61376 263.86432 263.86432 0 0 1-77.29152-186.368l-4.54656-347.50464v-1.10592z"
												fill="currentColor"
											/>
										</svg>
										{{ session.title }}
									</div>
									<div class="history-meta">
										<span class="history-time">
											{{ formatRelativeTime(session.timestamp) }}
										</span>
									</div>
								</div>
								<div class="item-actions">
									<el-icon class="pin-icon"><Star /></el-icon>
								</div>
							</div>
						</div>

						<!-- Recent Sessions -->
						<div v-if="filteredHistory.length > 0" class="recent-section">
							<div class="section-header" v-if="pinnedSessions.length > 0">
								<el-icon><Clock /></el-icon>
								<span>Recent</span>
							</div>
							<div
								v-for="session in filteredHistory"
								:key="session.id"
								:class="[
									'history-item',
									{ active: currentSessionId === session.id },
								]"
								@click="loadChatSession(session.id)"
								@contextmenu.prevent="showContextMenu($event, session)"
							>
								<div class="item-content">
									<div class="history-title">
										<svg
											v-if="isFileSession(session)"
											xmlns="http://www.w3.org/2000/svg"
											xmlns:xlink="http://www.w3.org/1999/xlink"
											aria-hidden="true"
											role="img"
											class="attachment-icon"
											width="1em"
											height="1em"
											viewBox="0 0 1024 1024"
										>
											<path
												d="M239.08352 319.0784a188.17024 188.17024 0 0 1 376.29952 0v0.16384l4.62848 347.62752v0.08192a112.64 112.64 0 0 1-156.0576 105.63584 112.55808 112.55808 0 0 1-68.97664-105.39008V315.14624a36.864 36.864 0 1 1 73.728 0v352.99328a38.83008 38.83008 0 1 0 77.57824 0v-0.16384l-4.58752-347.58656V320.3072a114.4832 114.4832 0 0 0-228.88448-0.4096l4.5056 347.58656a190.13632 190.13632 0 1 0 380.3136 0l0.4096-334.39744a36.864 36.864 0 1 1 73.728 0.08192l-0.4096 334.31552a263.90528 263.90528 0 0 1-450.43712 186.61376 263.86432 263.86432 0 0 1-77.29152-186.368l-4.54656-347.50464v-1.10592z"
												fill="currentColor"
											/>
										</svg>
										{{ session.title }}
									</div>
									<div class="history-meta">
										<span class="history-time">
											{{ formatRelativeTime(session.timestamp) }}
										</span>
									</div>
								</div>
								<div class="item-actions">
									<el-dropdown trigger="click" @command="handleSessionAction">
										<el-button size="small" link class="action-btn">
											<el-icon><MoreFilled /></el-icon>
										</el-button>
										<template #dropdown>
											<el-dropdown-menu>
												<el-dropdown-item :command="`rename-${session.id}`">
													<el-icon><Edit /></el-icon>
													Rename
												</el-dropdown-item>
												<el-dropdown-item
													:command="`delete-${session.id}`"
													divided
												>
													<el-icon><Delete /></el-icon>
													Delete
												</el-dropdown-item>
											</el-dropdown-menu>
										</template>
									</el-dropdown>
								</div>
							</div>
						</div>

						<!-- Empty State -->
						<div v-if="chatHistory.length === 0" class="empty-history">
							<div class="empty-icon">
								<el-icon><ChatDotRound /></el-icon>
							</div>
							<p class="empty-title">No chat history</p>
							<p class="empty-subtitle">
								Start a conversation to see your chat history here
							</p>
							<el-button
								size="small"
								type="primary"
								@click="startNewChat"
								class="start-chat-btn"
							>
								<el-icon><Plus /></el-icon>
								Start New Chat
							</el-button>
						</div>

						<!-- No Search Results -->
						<div
							v-else-if="filteredHistory.length === 0 && historySearchQuery"
							class="no-results"
						>
							<div class="empty-icon">
								<el-icon><Search /></el-icon>
							</div>
							<p class="empty-title">No results found</p>
							<p class="empty-subtitle">Try different keywords</p>
						</div>
					</div>
				</div>
			</div>
		</el-card>

		<!-- Rename Session Dialog -->
		<el-dialog v-model="showRenameDialog" title="Rename Chat Session" width="400px">
			<el-input
				v-model="newSessionTitle"
				placeholder="Enter new session title..."
				@keyup.enter="confirmRenameSession"
			/>
			<template #footer>
				<div class="dialog-footer">
					<el-button @click="showRenameDialog = false">Cancel</el-button>
					<el-button type="primary" @click="confirmRenameSession">Rename</el-button>
				</div>
			</template>
		</el-dialog>
	</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, nextTick } from 'vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import {
	Star,
	User,
	CircleCheckFilled,
	List,
	Clock,
	Plus,
	Check,
	Delete,
	ArrowRight,
	ArrowDown,
	ArrowUp,
	ChatDotRound,
	Document,
	Close,
	Picture,
	MoreFilled,
	Search,
	Edit,
	Setting,
} from '@element-plus/icons-vue';
import { createWorkflow, getWorkflowList } from '@/apis/ow';
import { defHttp } from '@/apis/axios';
import { useGlobSetting } from '@/settings';
import {
	sendAIChatMessage,
	streamAIChatMessageNative,
	type AIChatMessage,
} from '@/apis/ai/workflow';
import { getDefaultAIModel, getUserAIModels, type AIModelConfig } from '@/apis/ai/config';
import { useStreamAIWorkflow } from '@/hooks/useStreamAIWorkflow';
import AIFileAnalyzer from './AIFileAnalyzer.vue';
import { useUserStore } from '@/stores/modules/user';

// Types
interface Workflow {
	id?: number;
	name: string;
	description: string;
	isActive: boolean;
}

interface WorkflowStage {
	name: string;
	description: string;
	order: number;
	assignedGroup: string;
	requiredFields: string[];
	estimatedDuration: number;
}

interface ChecklistTask {
	id: string;
	title: string;
	description: string;
	isRequired: boolean;
	completed?: boolean;
	estimatedMinutes?: number;
	category?: string;
}

interface ChecklistItem {
	name: string;
	description: string;
	stageId?: number;
	tasks: ChecklistTask[];
}

interface QuestionnaireQuestion {
	id: string;
	question: string;
	type:
		| 'short_answer'
		| 'paragraph'
		| 'multiple_choice'
		| 'checkboxes'
		| 'dropdown'
		| 'file_upload'
		| 'linear_scale'
		| 'rating'
		| 'multiple_choice_grid'
		| 'checkbox_grid'
		| 'date'
		| 'time'
		| 'short_answer_grid';
	options?: string[];
	isRequired: boolean;
	answer?: any;
	category?: string;
	helpText?: string;
	description?: string;
	min?: number;
	max?: number;
	minLabel?: string;
	maxLabel?: string;
	rows?: Array<{ id: string; label: string }>;
	columns?: Array<{ id: string; label: string }>;
	requireOneResponsePerRow?: boolean;
}

interface QuestionnaireItem {
	name: string;
	description: string;
	stageId?: number;
	questions: QuestionnaireQuestion[];
}

interface ChatMessage {
	id: string;
	type:
		| 'user'
		| 'ai'
		| 'system'
		| 'generation-complete'
		| 'workflow-modification'
		| 'workflow-selection';
	content: string;
	timestamp: Date;
	data?: {
		workflow?: Workflow;
		stages?: WorkflowStage[];
		checklists?: ChecklistItem[];
		questionnaires?: QuestionnaireItem[];
		workflows?: any[]; // For workflow selection
		operationMode?: string; // For modification mode
	};
}

interface ChatSession {
	id: string;
	title: string;
	timestamp: Date;
	messages: ChatMessage[];
	isPinned?: boolean;
	tags?: string[];
}

// Emits
const emit = defineEmits<{
	workflowGenerated: [
		data: {
			generatedWorkflow: Workflow;
			stages: WorkflowStage[];
			operationMode: string;
			selectedWorkflowId?: number;
		},
	];
	'open-ai-config': [];
}>();

// User Store
const userStore = useUserStore();

// Helper function to get storage key with tenant and user isolation
const getChatHistoryStorageKey = () => {
	const userInfo = userStore.getUserInfo;
	const tenantId = userInfo?.tenantId || 'default';
	const userId = userInfo?.userId || 'anonymous';
	return `ai-workflow-chat-history-${tenantId}-${userId}`;
};

// Reactive data
const currentInput = ref('');
const generating = ref(false);
const applying = ref(false);
const streamingMessage = ref('');
const isChatStreaming = ref(false);
const chatMessages = ref<ChatMessage[]>([]);
const chatHistory = ref<ChatSession[]>([]);
const currentSessionId = ref<string>('');
const conversationId = ref<string>('');
const uploadedFile = ref<File | null>(null);
const currentAIModel = ref<AIModelConfig | null>(null);
const availableModels = ref<AIModelConfig[]>([]);
const isLoadingModel = ref(true);

// Workflow modification data
const searchedWorkflows = ref<any[]>([]);
const selectedWorkflow = ref<any | null>(null);
const isSearchingWorkflows = ref(false);

// UI State Management
const isHistoryCollapsed = ref(false);
const historySearchQuery = ref('');
const filteredHistory = computed(() => {
	if (!historySearchQuery.value.trim()) {
		return unpinnedSessions.value;
	}

	const query = historySearchQuery.value.toLowerCase();
	return unpinnedSessions.value.filter((session) => {
		return (
			session.title.toLowerCase().includes(query) ||
			session.messages.some((msg) => msg.content.toLowerCase().includes(query))
		);
	});
});
const showRenameDialog = ref(false);
const renameSessionId = ref('');
const newSessionTitle = ref('');

// Collapse state management
const checklistsCollapsed = ref(false);
const questionnairesCollapsed = ref(false);
const collapsedChecklistTasks = ref<Set<number>>(new Set());
const collapsedQuestionnaireQuestions = ref<Set<number>>(new Set());

// Stream AI Hook
const { isStreaming, startStreaming, streamFileAnalysis, stopStreaming } = useStreamAIWorkflow();

// Computed properties for streaming display
const shouldShowStreamingMessage = computed(() => {
	const isWorkflowStreaming = isStreaming.value;
	const isChatStreamingValue = isChatStreaming.value;

	// For workflow streaming, show if there's streaming message content
	if (isWorkflowStreaming && streamingMessage.value) {
		console.log('Showing workflow streaming message');
		return true;
	}

	// For chat streaming, check only the latest AI message
	if (isChatStreamingValue) {
		const aiMessages = chatMessages.value.filter((msg) => msg.type === 'ai');
		const latestAIMessage = aiMessages[aiMessages.length - 1];

		// Show streaming message only if the latest AI message is empty or doesn't exist
		const hasLatestContent =
			latestAIMessage && latestAIMessage.content && latestAIMessage.content.trim();

		console.log('Chat streaming debug:', {
			isChatStreamingValue,
			aiMessagesCount: aiMessages.length,
			latestAIContent: latestAIMessage?.content || 'none',
			hasLatestContent,
			shouldShow: !hasLatestContent,
		});

		return !hasLatestContent;
	}

	console.log('Not showing streaming message');
	return false;
});

// Computed properties
const canGenerate = computed(() => {
	const hasInput = currentInput.value.trim();
	const hasFile = uploadedFile.value;
	const hasChatHistory = chatMessages.value.some((msg) => msg.type === 'user');
	return hasInput || hasFile || hasChatHistory;
});

const pinnedSessions = computed(() => {
	return chatHistory.value.filter((session) => session.isPinned);
});

const unpinnedSessions = computed(() => {
	return chatHistory.value.filter((session) => !session.isPinned);
});

// Dialog states

// Refs
const chatMessagesRef = ref<HTMLElement>();

// Methods
const generateWorkflow = async () => {
	if (!canGenerate.value) {
		ElMessage.warning('Please describe your workflow or upload a file');
		return;
	}

	// Check input type
	const hasInput = currentInput.value.trim();
	const hasFile = uploadedFile.value;

	generating.value = true;

	// Only add user message when there's new input
	if (hasInput || hasFile) {
		const userMessage: ChatMessage = {
			id: Date.now().toString(),
			type: 'user',
			content:
				currentInput.value ||
				`Uploaded file "${uploadedFile.value?.name}" for workflow analysis`,
			timestamp: new Date(),
		};
		chatMessages.value.push(userMessage);
	}

	// Add system message
	const systemMessage: ChatMessage = {
		id: (Date.now() + 1).toString(),
		type: 'system',
		content: 'AI is generating your workflow in real-time...',
		timestamp: new Date(),
	};
	chatMessages.value.push(systemMessage);

	try {
		const onStreamChunk = (chunk: string) => {
			// 过滤和美化流式消息
			const cleanedMessage = cleanStreamMessage(chunk);
			streamingMessage.value = cleanedMessage;
			scrollToBottom();
		};

		// Track streaming workflow data
		let streamingWorkflowData = {
			workflow: null as any,
			stages: [] as any[],
			checklists: [] as any[],
			questionnaires: [] as any[],
			completeMessageId: '',
			// Store original stage data with checklist and questionnaire
			originalStagesData: [] as any[],
			// Store separate checklists and questionnaires arrays from backend
			backendChecklists: [] as any[],
			backendQuestionnaires: [] as any[],
		};

		const onStreamComplete = (data: any) => {
			// Clear streaming message
			streamingMessage.value = '';

			console.log('onStreamComplete received data:', data);
			console.log('data.stages:', data.stages);
			console.log('data keys:', Object.keys(data));
			console.log('data.checklists:', data.checklists);
			console.log('data.Checklists:', data.Checklists);
			console.log('data.questionnaires:', data.questionnaires);
			console.log('data.Questionnaires:', data.Questionnaires);

			const aiWorkflow = data.generatedWorkflow ||
				data.GeneratedWorkflow || {
					name: 'AI Generated Workflow',
					description: 'Auto-created by AI',
					isActive: true,
				};

			// Handle workflow field case compatibility
			if (aiWorkflow && typeof aiWorkflow === 'object') {
				aiWorkflow.name = aiWorkflow.Name || aiWorkflow.name || 'AI Generated Workflow';
				aiWorkflow.description =
					aiWorkflow.Description || aiWorkflow.description || 'Auto-created by AI';
				aiWorkflow.isActive =
					aiWorkflow.IsActive !== undefined
						? aiWorkflow.IsActive
						: aiWorkflow.isActive !== undefined
						? aiWorkflow.isActive
						: true;
			}

			// Extract checklists and questionnaires from backend response
			// Try multiple possible field names (case variations)
			const backendChecklists =
				data.checklists || data.Checklists || data.checkLists || data.CheckLists || [];
			const backendQuestionnaires =
				data.questionnaires ||
				data.Questionnaires ||
				data.questionNaires ||
				data.QuestionNaires ||
				[];

			console.log('Extracted backendChecklists:', backendChecklists);
			console.log('Extracted backendQuestionnaires:', backendQuestionnaires);

			// Initialize streaming workflow data
			streamingWorkflowData.workflow = aiWorkflow;
			streamingWorkflowData.stages = [];
			streamingWorkflowData.checklists = [];
			streamingWorkflowData.questionnaires = [];
			streamingWorkflowData.originalStagesData = [];
			streamingWorkflowData.backendChecklists = Array.isArray(backendChecklists)
				? backendChecklists
				: [];
			streamingWorkflowData.backendQuestionnaires = Array.isArray(backendQuestionnaires)
				? backendQuestionnaires
				: [];

			// Create initial generation complete message
			const completeMessage: ChatMessage = {
				id: (Date.now() + 2).toString(),
				type: 'generation-complete',
				content: 'Workflow generation in progress...',
				timestamp: new Date(),
				data: {
					workflow: aiWorkflow,
					stages: [],
					checklists: [],
					questionnaires: [],
				},
			};

			streamingWorkflowData.completeMessageId = completeMessage.id;
			chatMessages.value.push(completeMessage);
			scrollToBottom();

			// Process stages one by one with delay
			const stages = data.stages || [];
			processStagesSequentially(stages, 0);

			// Save to history
			saveChatSession();
			scrollToBottom();
		};

		// Process stages sequentially with animation
		const processStagesSequentially = async (stages: any[], currentIndex: number) => {
			if (currentIndex >= stages.length) {
				// All stages processed, now add checklists and questionnaires
				await addChecklistsAndQuestionnaires();
				return;
			}

			// Process current stage
			const stage = stages[currentIndex];
			const processedStage = {
				name: stage?.Name || stage?.name || `Stage ${currentIndex + 1}`,
				description: stage?.Description || stage?.description || '',
				order: Number.isFinite(Number(stage?.Order || stage?.order))
					? Math.trunc(Number(stage?.Order || stage?.order))
					: currentIndex + 1,
				assignedGroup: stage?.AssignedGroup || stage?.assignedGroup || 'General',
				requiredFields: Array.isArray(stage?.RequiredFields || stage?.requiredFields)
					? stage?.RequiredFields || stage?.requiredFields
					: [],
				estimatedDuration:
					Number(stage?.EstimatedDuration || stage?.estimatedDuration) || 1,
			};

			console.log(`Processing stage ${currentIndex + 1}:`, processedStage);
			console.log('Stage checklist data:', stage?.checklist || stage?.Checklist);
			console.log('Stage questionnaire data:', stage?.questionnaire || stage?.Questionnaire);

			// Save original stage data with checklist and questionnaire for later use
			streamingWorkflowData.originalStagesData.push(stage);

			// Add stage to streaming data
			streamingWorkflowData.stages.push(processedStage);

			// Update the generation complete message
			updateGenerationCompleteMessage();

			// Wait for animation effect
			await new Promise((resolve) => setTimeout(resolve, 800));

			// Process next stage
			processStagesSequentially(stages, currentIndex + 1);
		};

		// Generate realistic checklist tasks based on stage name and description
		const generateChecklistTasks = (stage: WorkflowStage): ChecklistTask[] => {
			const stageName = stage.name.toLowerCase();
			const stageDesc = stage.description.toLowerCase();

			// Common task templates based on stage characteristics
			const taskTemplates: { [key: string]: ChecklistTask[] } = {
				// Initial/Assessment stages
				initial: [
					{
						id: 'req-gather',
						title: 'Gather Requirements',
						description: 'Collect and document all necessary requirements',
						isRequired: true,
					},
					{
						id: 'stakeholder-id',
						title: 'Identify Stakeholders',
						description: 'List all key stakeholders and their roles',
						isRequired: true,
					},
					{
						id: 'timeline-est',
						title: 'Estimate Timeline',
						description: 'Create initial timeline estimates',
						isRequired: false,
					},
					{
						id: 'resource-check',
						title: 'Check Resource Availability',
						description: 'Verify required resources are available',
						isRequired: true,
					},
				],

				// Planning stages
				planning: [
					{
						id: 'plan-create',
						title: 'Create Detailed Plan',
						description: 'Develop comprehensive project plan',
						isRequired: true,
					},
					{
						id: 'risk-assess',
						title: 'Risk Assessment',
						description: 'Identify and assess potential risks',
						isRequired: true,
					},
					{
						id: 'budget-approve',
						title: 'Budget Approval',
						description: 'Get budget approval from management',
						isRequired: true,
					},
					{
						id: 'team-assign',
						title: 'Assign Team Members',
						description: 'Assign roles and responsibilities to team members',
						isRequired: true,
					},
				],

				// Design/Development stages
				design: [
					{
						id: 'wireframe',
						title: 'Create Wireframes',
						description: 'Design initial wireframes and mockups',
						isRequired: true,
					},
					{
						id: 'prototype',
						title: 'Build Prototype',
						description: 'Develop working prototype',
						isRequired: false,
					},
					{
						id: 'design-review',
						title: 'Design Review',
						description: 'Conduct design review with stakeholders',
						isRequired: true,
					},
					{
						id: 'spec-finalize',
						title: 'Finalize Specifications',
						description: 'Complete technical specifications',
						isRequired: true,
					},
				],

				// Implementation/Development stages
				implementation: [
					{
						id: 'env-setup',
						title: 'Setup Environment',
						description: 'Configure development/production environment',
						isRequired: true,
					},
					{
						id: 'code-develop',
						title: 'Develop Code',
						description: 'Write and implement code according to specifications',
						isRequired: true,
					},
					{
						id: 'unit-test',
						title: 'Unit Testing',
						description: 'Perform unit testing on developed components',
						isRequired: true,
					},
					{
						id: 'code-review',
						title: 'Code Review',
						description: 'Conduct peer code review',
						isRequired: true,
					},
				],

				// Testing stages
				testing: [
					{
						id: 'test-plan',
						title: 'Create Test Plan',
						description: 'Develop comprehensive test plan',
						isRequired: true,
					},
					{
						id: 'test-cases',
						title: 'Write Test Case',
						description: 'Create detailed test cases',
						isRequired: true,
					},
					{
						id: 'execute-tests',
						title: 'Execute Tests',
						description: 'Run all test cases and document results',
						isRequired: true,
					},
					{
						id: 'bug-fix',
						title: 'Fix Bugs',
						description: 'Address and fix identified issues',
						isRequired: true,
					},
				],

				// Review/Approval stages
				review: [
					{
						id: 'quality-check',
						title: 'Quality Assurance Check',
						description: 'Perform quality assurance review',
						isRequired: true,
					},
					{
						id: 'stakeholder-review',
						title: 'Stakeholder Review',
						description: 'Present to stakeholders for review',
						isRequired: true,
					},
					{
						id: 'feedback-collect',
						title: 'Collect Feedback',
						description: 'Gather and document feedback',
						isRequired: true,
					},
					{
						id: 'approval-get',
						title: 'Get Final Approval',
						description: 'Obtain final approval to proceed',
						isRequired: true,
					},
				],

				// Deployment/Launch stages
				deployment: [
					{
						id: 'deploy-prep',
						title: 'Prepare Deployment',
						description: 'Prepare all deployment materials',
						isRequired: true,
					},
					{
						id: 'backup-create',
						title: 'Create Backup',
						description: 'Create system backup before deployment',
						isRequired: true,
					},
					{
						id: 'deploy-execute',
						title: 'Execute Deployment',
						description: 'Deploy to production environment',
						isRequired: true,
					},
					{
						id: 'smoke-test',
						title: 'Smoke Testing',
						description: 'Perform post-deployment smoke tests',
						isRequired: true,
					},
				],

				// Training/Onboarding stages
				training: [
					{
						id: 'material-prep',
						title: 'Prepare Training Materials',
						description: 'Create training documentation and materials',
						isRequired: true,
					},
					{
						id: 'schedule-training',
						title: 'Schedule Training Sessions',
						description: 'Organize training sessions with users',
						isRequired: true,
					},
					{
						id: 'conduct-training',
						title: 'Conduct Training',
						description: 'Deliver training to end users',
						isRequired: true,
					},
					{
						id: 'support-provide',
						title: 'Provide Support',
						description: 'Offer ongoing support during transition',
						isRequired: true,
					},
				],

				// Default/Generic tasks
				default: [
					{
						id: 'task-plan',
						title: 'Plan Tasks',
						description: `Plan all tasks for ${stage.name}`,
						isRequired: true,
					},
					{
						id: 'resource-allocate',
						title: 'Allocate Resources',
						description: 'Ensure necessary resources are allocated',
						isRequired: true,
					},
					{
						id: 'progress-monitor',
						title: 'Monitor Progress',
						description: 'Track and monitor stage progress',
						isRequired: true,
					},
					{
						id: 'deliverable-complete',
						title: 'Complete Deliverables',
						description: 'Finish all stage deliverables',
						isRequired: true,
					},
				],
			};

			// Determine which template to use based on stage name and description
			let selectedTasks: ChecklistTask[] = [];

			if (
				stageName.includes('initial') ||
				stageName.includes('assessment') ||
				stageName.includes('analysis')
			) {
				selectedTasks = taskTemplates.initial;
			} else if (
				stageName.includes('plan') ||
				stageName.includes('design') ||
				stageDesc.includes('plan')
			) {
				selectedTasks = taskTemplates.planning;
			} else if (
				stageName.includes('design') ||
				stageName.includes('prototype') ||
				stageDesc.includes('design')
			) {
				selectedTasks = taskTemplates.design;
			} else if (
				stageName.includes('implement') ||
				stageName.includes('develop') ||
				stageName.includes('build') ||
				stageDesc.includes('develop')
			) {
				selectedTasks = taskTemplates.implementation;
			} else if (
				stageName.includes('test') ||
				stageName.includes('qa') ||
				stageDesc.includes('test')
			) {
				selectedTasks = taskTemplates.testing;
			} else if (
				stageName.includes('review') ||
				stageName.includes('approval') ||
				stageDesc.includes('review')
			) {
				selectedTasks = taskTemplates.review;
			} else if (
				stageName.includes('deploy') ||
				stageName.includes('launch') ||
				stageName.includes('release')
			) {
				selectedTasks = taskTemplates.deployment;
			} else if (
				stageName.includes('training') ||
				stageName.includes('onboard') ||
				stageDesc.includes('training')
			) {
				selectedTasks = taskTemplates.training;
			} else {
				selectedTasks = taskTemplates.default;
			}

			// Add unique IDs with stage prefix
			return selectedTasks.map((task, index) => ({
				...task,
				id: `${stage.name.toLowerCase().replace(/\s+/g, '-')}-${task.id}-${index}`,
			}));
		};

		// Generate realistic questionnaire questions based on stage name and description
		const generateQuestionnaireQuestions = (stage: WorkflowStage): QuestionnaireQuestion[] => {
			const stageName = stage.name.toLowerCase();
			const stageDesc = stage.description.toLowerCase();

			// Question templates based on stage characteristics
			const questionTemplates: { [key: string]: QuestionnaireQuestion[] } = {
				// Initial/Assessment stages
				initial: [
					{
						id: 'project-scope',
						question: 'What is the scope of this project?',
						type: 'paragraph',
						isRequired: true,
					},
					{
						id: 'success-criteria',
						question: 'What are the success criteria?',
						type: 'paragraph',
						isRequired: true,
					},
					{
						id: 'budget-range',
						question: 'What is the budget range?',
						type: 'dropdown',
						options: ['< $10K', '$10K - $50K', '$50K - $100K', '> $100K'],
						isRequired: true,
					},
					{
						id: 'timeline-preference',
						question: 'What is your preferred timeline?',
						type: 'multiple_choice',
						options: ['1-2 weeks', '1 month', '2-3 months', '6+ months'],
						isRequired: true,
					},
				],

				// Planning stages
				planning: [
					{
						id: 'team-size',
						question: 'How many team members are needed?',
						type: 'short_answer',
						isRequired: true,
					},
					{
						id: 'key-milestones',
						question: 'What are the key milestones?',
						type: 'paragraph',
						isRequired: true,
					},
					{
						id: 'risk-tolerance',
						question: 'What is your risk tolerance level?',
						type: 'linear_scale',
						min: 1,
						max: 5,
						minLabel: 'Low Risk',
						maxLabel: 'High Risk',
						isRequired: true,
					},
					{
						id: 'communication-frequency',
						question: 'How often should progress be reported?',
						type: 'dropdown',
						options: ['Daily', 'Weekly', 'Bi-weekly', 'Monthly'],
						isRequired: false,
					},
				],

				// Design stages
				design: [
					{
						id: 'design-style',
						question: 'What design style do you prefer?',
						type: 'multiple_choice',
						options: ['Modern', 'Classic', 'Minimalist', 'Bold'],
						isRequired: true,
					},
					{
						id: 'target-audience',
						question: 'Who is the target audience?',
						type: 'paragraph',
						isRequired: true,
					},
					{
						id: 'brand-guidelines',
						question: 'Are there existing brand guidelines?',
						type: 'multiple_choice',
						options: ['Yes', 'No', 'Partially'],
						isRequired: true,
					},
					{
						id: 'accessibility-requirements',
						question: 'Are there accessibility requirements?',
						type: 'checkboxes',
						options: [
							'WCAG 2.1 AA',
							'Screen Reader Support',
							'Keyboard Navigation',
							'Color Contrast',
						],
						isRequired: false,
					},
				],

				// Implementation stages
				implementation: [
					{
						id: 'tech-stack',
						question: 'What technology stack should be used?',
						type: 'checkboxes',
						options: ['React', 'Vue', 'Angular', 'Node.js', 'Python', 'Java', '.NET'],
						isRequired: true,
					},
					{
						id: 'performance-requirements',
						question: 'What are the performance requirements?',
						type: 'paragraph',
						isRequired: true,
					},
					{
						id: 'security-level',
						question: 'What security level is required?',
						type: 'multiple_choice',
						options: ['Basic', 'Standard', 'High', 'Enterprise'],
						isRequired: true,
					},
					{
						id: 'integration-needs',
						question: 'What systems need integration?',
						type: 'paragraph',
						isRequired: false,
					},
				],

				// Testing stages
				testing: [
					{
						id: 'test-types',
						question: 'What types of testing are required?',
						type: 'checkboxes',
						options: [
							'Unit Testing',
							'Integration Testing',
							'Performance Testing',
							'Security Testing',
							'User Acceptance Testing',
						],
						isRequired: true,
					},
					{
						id: 'test-environment',
						question: 'What test environment is available?',
						type: 'multiple_choice',
						options: ['Development', 'Staging', 'Production-like', 'Cloud-based'],
						isRequired: true,
					},
					{
						id: 'acceptance-criteria',
						question: 'What are the acceptance criteria?',
						type: 'paragraph',
						isRequired: true,
					},
					{
						id: 'test-data',
						question: 'Is test data available?',
						type: 'multiple_choice',
						options: ['Yes', 'No', 'Partially'],
						isRequired: true,
					},
				],

				// Review stages
				review: [
					{
						id: 'review-criteria',
						question: 'What are the review criteria?',
						type: 'paragraph',
						isRequired: true,
					},
					{
						id: 'reviewers',
						question: 'Who are the key reviewers?',
						type: 'paragraph',
						isRequired: true,
					},
					{
						id: 'approval-process',
						question: 'What is the approval process?',
						type: 'paragraph',
						isRequired: true,
					},
					{
						id: 'feedback-timeline',
						question: 'What is the feedback timeline?',
						type: 'multiple_choice',
						options: ['24 hours', '2-3 days', '1 week', '2 weeks'],
						isRequired: true,
					},
				],

				// Deployment stages
				deployment: [
					{
						id: 'deployment-strategy',
						question: 'What deployment strategy should be used?',
						type: 'multiple_choice',
						options: ['Blue-Green', 'Rolling', 'Canary', 'Big Bang'],
						isRequired: true,
					},
					{
						id: 'rollback-plan',
						question: 'Is there a rollback plan?',
						type: 'multiple_choice',
						options: ['Yes', 'No', 'In Development'],
						isRequired: true,
					},
					{
						id: 'monitoring-setup',
						question: 'What monitoring is needed?',
						type: 'checkboxes',
						options: [
							'Performance Monitoring',
							'Error Tracking',
							'User Analytics',
							'Security Monitoring',
						],
						isRequired: true,
					},
					{
						id: 'maintenance-window-date',
						question: 'What date is the maintenance window?',
						type: 'date',
						isRequired: false,
					},
					{
						id: 'maintenance-window-time',
						question: 'What time is the maintenance window?',
						type: 'time',
						isRequired: false,
					},
				],

				// Training stages
				training: [
					{
						id: 'training-format',
						question: 'What training format is preferred?',
						type: 'multiple_choice',
						options: ['In-person', 'Virtual', 'Self-paced', 'Hybrid'],
						isRequired: true,
					},
					{
						id: 'audience-size',
						question: 'How many people need training?',
						type: 'short_answer',
						isRequired: true,
					},
					{
						id: 'skill-level',
						question: 'Rate the current skill level of the team',
						type: 'rating',
						max: 5,
						isRequired: true,
					},
					{
						id: 'training-materials',
						question: 'What training materials are needed?',
						type: 'checkboxes',
						options: [
							'User Manual',
							'Video Tutorials',
							'Interactive Demos',
							'Quick Reference',
						],
						isRequired: true,
					},
				],

				// Default questions
				default: [
					{
						id: 'stage-objectives',
						question: `What are the main objectives for ${stage.name}?`,
						type: 'paragraph',
						isRequired: true,
					},
					{
						id: 'success-metrics',
						question: 'How will success be measured?',
						type: 'paragraph',
						isRequired: true,
					},
					{
						id: 'dependencies',
						question: 'Are there any dependencies?',
						type: 'paragraph',
						isRequired: false,
					},
					{
						id: 'special-requirements',
						question: 'Are there any special requirements?',
						type: 'paragraph',
						isRequired: false,
					},
				],
			};

			// Determine which template to use
			let selectedQuestions: QuestionnaireQuestion[] = [];

			if (
				stageName.includes('initial') ||
				stageName.includes('assessment') ||
				stageName.includes('analysis')
			) {
				selectedQuestions = questionTemplates.initial;
			} else if (stageName.includes('plan') || stageDesc.includes('plan')) {
				selectedQuestions = questionTemplates.planning;
			} else if (stageName.includes('design') || stageDesc.includes('design')) {
				selectedQuestions = questionTemplates.design;
			} else if (
				stageName.includes('implement') ||
				stageName.includes('develop') ||
				stageName.includes('build')
			) {
				selectedQuestions = questionTemplates.implementation;
			} else if (stageName.includes('test') || stageName.includes('qa')) {
				selectedQuestions = questionTemplates.testing;
			} else if (stageName.includes('review') || stageName.includes('approval')) {
				selectedQuestions = questionTemplates.review;
			} else if (stageName.includes('deploy') || stageName.includes('launch')) {
				selectedQuestions = questionTemplates.deployment;
			} else if (stageName.includes('training') || stageName.includes('onboard')) {
				selectedQuestions = questionTemplates.training;
			} else {
				selectedQuestions = questionTemplates.default;
			}

			// Add unique IDs with stage prefix
			return selectedQuestions.map((question, index) => ({
				...question,
				id: `${stage.name.toLowerCase().replace(/\s+/g, '-')}-${question.id}-${index}`,
			}));
		};

		// Helper function to convert AI task format to frontend format
		const convertTaskToFrontendFormat = (task: any, index: number): ChecklistTask => {
			return {
				id: task.id || `task-${Date.now()}-${index}`,
				title: task.title || task.Title || '',
				description: task.description || task.Description || '',
				isRequired:
					task.isRequired !== undefined
						? task.isRequired
						: task.IsRequired !== undefined
						? task.IsRequired
						: true,
				completed: false,
				estimatedMinutes: task.estimatedMinutes || task.EstimatedMinutes || 60,
				category: task.category || task.Category || 'Execution',
			};
		};

		// Helper function to convert AI question format to frontend format
		const convertQuestionToFrontendFormat = (
			question: any,
			index: number
		): QuestionnaireQuestion => {
			const questionType = question.type || question.Type || 'paragraph';
			// Map AI question types to frontend types
			const typeMapping: Record<string, string> = {
				text: 'short_answer',
				select: 'dropdown',
				multiple_choice: 'multiple_choice',
				checkboxes: 'checkboxes',
			};

			return {
				id: question.id || `question-${Date.now()}-${index}`,
				question: question.question || question.Question || '',
				type: (typeMapping[questionType] || questionType) as QuestionnaireQuestion['type'],
				options: question.options || question.Options || undefined,
				isRequired:
					question.isRequired !== undefined
						? question.isRequired
						: question.IsRequired !== undefined
						? question.IsRequired
						: true,
				category: question.category || question.Category || 'Requirements',
				helpText: question.helpText || question.HelpText,
				description: question.description || question.Description,
			};
		};

		// Add checklists and questionnaires
		const addChecklistsAndQuestionnaires = async () => {
			// Use AI-generated checklists and questionnaires if available, otherwise fallback to templates
			streamingWorkflowData.checklists = streamingWorkflowData.stages.map(
				(stage: WorkflowStage, index: number) => {
					// First try to get checklist from backend separate array (by index)
					const backendChecklist = streamingWorkflowData.backendChecklists[index];

					// Then try to get from original stage data (embedded format)
					const originalStage = streamingWorkflowData.originalStagesData[index];
					const embeddedChecklist = originalStage?.checklist || originalStage?.Checklist;

					// Prefer backend array format, fallback to embedded format, then template
					const aiChecklist = backendChecklist || embeddedChecklist;

					if (aiChecklist) {
						// Handle different data structures
						let tasks: any[] = [];
						let checklistName = '';
						let checklistDescription = '';

						// Backend array format: { GeneratedChecklist: {...}, Tasks: [...] }
						if (backendChecklist) {
							const checklistData =
								backendChecklist.GeneratedChecklist ||
								backendChecklist.generatedChecklist ||
								backendChecklist;
							tasks = backendChecklist.Tasks || backendChecklist.tasks || [];
							checklistName =
								checklistData.Name ||
								checklistData.name ||
								`${stage.name} Checklist`;
							checklistDescription =
								checklistData.Description ||
								checklistData.description ||
								`Essential tasks to complete during the ${stage.name} stage`;
						}
						// Embedded format: { name: "...", tasks: [...] }
						else if (embeddedChecklist) {
							tasks = embeddedChecklist.tasks || embeddedChecklist.Tasks || [];
							checklistName =
								embeddedChecklist.name ||
								embeddedChecklist.Name ||
								`${stage.name} Checklist`;
							checklistDescription =
								embeddedChecklist.description ||
								embeddedChecklist.Description ||
								`Essential tasks to complete during the ${stage.name} stage`;
						}

						if (tasks && Array.isArray(tasks) && tasks.length > 0) {
							// Use AI-generated checklist
							return {
								name: checklistName,
								description: checklistDescription,
								tasks: tasks.map((task: any, taskIndex: number) =>
									convertTaskToFrontendFormat(task, taskIndex)
								),
							};
						}
					}

					// Fallback to template generation
					return {
						name: `${stage.name} Checklist`,
						description: `Essential tasks to complete during the ${stage.name} stage`,
						tasks: generateChecklistTasks(stage),
					};
				}
			);

			updateGenerationCompleteMessage();
			await new Promise((resolve) => setTimeout(resolve, 500));

			// Generate realistic questionnaires with questions
			streamingWorkflowData.questionnaires = streamingWorkflowData.stages.map(
				(stage: WorkflowStage, index: number) => {
					// First try to get questionnaire from backend separate array (by index)
					const backendQuestionnaire = streamingWorkflowData.backendQuestionnaires[index];

					// Then try to get from original stage data (embedded format)
					const originalStage = streamingWorkflowData.originalStagesData[index];
					const embeddedQuestionnaire =
						originalStage?.questionnaire || originalStage?.Questionnaire;

					// Prefer backend array format, fallback to embedded format, then template
					const aiQuestionnaire = backendQuestionnaire || embeddedQuestionnaire;

					if (aiQuestionnaire) {
						// Handle different data structures
						let questions: any[] = [];
						let questionnaireName = '';
						let questionnaireDescription = '';

						// Backend array format: { GeneratedQuestionnaire: {...}, Questions: [...] }
						if (backendQuestionnaire) {
							const questionnaireData =
								backendQuestionnaire.GeneratedQuestionnaire ||
								backendQuestionnaire.generatedQuestionnaire ||
								backendQuestionnaire;
							questions =
								backendQuestionnaire.Questions ||
								backendQuestionnaire.questions ||
								[];
							questionnaireName =
								questionnaireData.Name ||
								questionnaireData.name ||
								`${stage.name} Questionnaire`;
							questionnaireDescription =
								questionnaireData.Description ||
								questionnaireData.description ||
								`Key questions to gather information for the ${stage.name} stage`;
						}
						// Embedded format: { name: "...", questions: [...] }
						else if (embeddedQuestionnaire) {
							questions =
								embeddedQuestionnaire.questions ||
								embeddedQuestionnaire.Questions ||
								[];
							questionnaireName =
								embeddedQuestionnaire.name ||
								embeddedQuestionnaire.Name ||
								`${stage.name} Questionnaire`;
							questionnaireDescription =
								embeddedQuestionnaire.description ||
								embeddedQuestionnaire.Description ||
								`Key questions to gather information for the ${stage.name} stage`;
						}

						if (questions && Array.isArray(questions) && questions.length > 0) {
							// Use AI-generated questionnaire
							return {
								name: questionnaireName,
								description: questionnaireDescription,
								questions: questions.map((question: any, questionIndex: number) =>
									convertQuestionToFrontendFormat(question, questionIndex)
								),
							};
						}
					}

					// Fallback to template generation
					return {
						name: `${stage.name} Questionnaire`,
						description: `Key questions to gather information for the ${stage.name} stage`,
						questions: generateQuestionnaireQuestions(stage),
					};
				}
			);

			updateGenerationCompleteMessage();

			// Final update - mark as completed
			const messageIndex = chatMessages.value.findIndex(
				(msg) => msg.id === streamingWorkflowData.completeMessageId
			);
			if (messageIndex !== -1) {
				chatMessages.value[messageIndex].content =
					'Workflow generation completed successfully!';
			}

			scrollToBottom();
			saveChatSession();
		};

		// Update generation complete message with current data
		const updateGenerationCompleteMessage = () => {
			const messageIndex = chatMessages.value.findIndex(
				(msg) => msg.id === streamingWorkflowData.completeMessageId
			);
			if (messageIndex !== -1) {
				chatMessages.value[messageIndex].data = {
					workflow: streamingWorkflowData.workflow,
					stages: [...streamingWorkflowData.stages],
					checklists: [...streamingWorkflowData.checklists],
					questionnaires: [...streamingWorkflowData.questionnaires],
				};
			}
			scrollToBottom();
		};

		// Use stream response based on input type
		if (uploadedFile.value) {
			await streamFileAnalysis(uploadedFile.value, onStreamChunk, onStreamComplete, {
				id: currentAIModel.value?.id?.toString(),
				provider: currentAIModel.value?.provider,
				modelName: currentAIModel.value?.modelName,
			});
		} else {
			// Build comprehensive description from entire chat history
			let workflowDescription = '';

			// Include current input if available
			if (currentInput.value.trim()) {
				workflowDescription = currentInput.value;
			}

			// Always include chat history context
			if (chatMessages.value.length > 0) {
				const chatContext = chatMessages.value
					.filter((msg) => msg.type === 'user' || msg.type === 'ai')
					.map((msg) => {
						if (msg.type === 'user') {
							return `User: ${msg.content}`;
						} else {
							return `AI: ${msg.content}`;
						}
					})
					.join('\n');

				if (workflowDescription) {
					workflowDescription = `${workflowDescription}\n\nChat History:\n${chatContext}`;
				} else {
					workflowDescription = `Chat History:\n${chatContext}`;
				}
			}

			// If still no description, use a default
			if (!workflowDescription.trim()) {
				workflowDescription = 'Generate a workflow based on our conversation';
			}

			console.log('Final workflow description:', workflowDescription);

			await startStreaming(workflowDescription, onStreamChunk, onStreamComplete, {
				id: currentAIModel.value?.id?.toString(),
				provider: currentAIModel.value?.provider,
				modelName: currentAIModel.value?.modelName,
			});
		}
	} catch (error) {
		console.error('Generation error:', error);
		ElMessage.error('Failed to generate workflow');
		// Remove system message on error
		chatMessages.value = chatMessages.value.filter((msg) => msg.type !== 'system');
		streamingMessage.value = '';
	} finally {
		generating.value = false;
		currentInput.value = '';
		uploadedFile.value = null;
		await scrollToBottom();
	}
};

// Handle workflow modification requests
const handleWorkflowModification = async (messageContent: string) => {
	try {
		// Get available workflows to modify
		const response = await getWorkflowList();
		if (!response.success || !response.data || response.data.length === 0) {
			const noWorkflowMessage: ChatMessage = {
				id: (Date.now() + 2).toString(),
				type: 'ai',
				content:
					'No existing workflows found. Would you like to create a new workflow instead?',
				timestamp: new Date(),
			};
			chatMessages.value.push(noWorkflowMessage);
			await scrollToBottom();
			saveChatSession();
			return;
		}

		const workflows = response.data;

		// If only one workflow, directly show it for modification
		if (workflows.length === 1) {
			const workflowWithStages = await getWorkflowWithStages(workflows[0].id);
			if (workflowWithStages) {
				selectedWorkflow.value = workflowWithStages;

				const modificationMessage: ChatMessage = {
					id: (Date.now() + 2).toString(),
					type: 'workflow-modification',
					content: `Ready to modify workflow: ${workflowWithStages.name}`,
					timestamp: new Date(),
					data: {
						workflow: workflowWithStages,
						stages: workflowWithStages.stages || [],
						operationMode: 'modify',
					},
				};
				chatMessages.value.push(modificationMessage);
				await scrollToBottom();
				saveChatSession();
				return;
			}
		}

		// Multiple workflows found, show selection
		const selectionMessage: ChatMessage = {
			id: (Date.now() + 2).toString(),
			type: 'workflow-selection',
			content: `Found ${workflows.length} workflows. Please select which workflow you'd like to modify:`,
			timestamp: new Date(),
			data: {
				workflows: workflows,
				operationMode: 'modify',
			},
		};
		chatMessages.value.push(selectionMessage);
		await scrollToBottom();
		saveChatSession();
	} catch (error) {
		console.error('Failed to load workflows for modification:', error);
		const errorMessage: ChatMessage = {
			id: (Date.now() + 2).toString(),
			type: 'ai',
			content: 'Failed to load existing workflows. Please try again or contact support.',
			timestamp: new Date(),
		};
		chatMessages.value.push(errorMessage);
		await scrollToBottom();
		saveChatSession();
	}
};

// Clean and beautify streaming messages
const cleanStreamMessage = (message: string): string => {
	if (!message || typeof message !== 'string') {
		return 'Processing...';
	}

	// Remove technical details like "(1948 characters, 0.1s)"
	let cleaned = message.replace(/\(\d+\s+characters?,\s*[\d.]+s?\)\s*\|?/gi, '');

	// Remove redundant progress indicators
	cleaned = cleaned.replace(/\s*\|\s*$/, '');

	// Replace technical messages with user-friendly ones
	const messageMap: Record<string, string> = {
		'Generating workflow': 'Creating your workflow...',
		'Processing workflow': 'Analyzing requirements...',
		'Analyzing workflow': 'Understanding your needs...',
		'Creating stages': 'Building workflow stages...',
		'Generating stages': 'Designing process steps...',
		'Processing stages': 'Organizing workflow structure...',
		'Creating checklists': 'Adding task checklists...',
		'Generating checklists': 'Creating task lists...',
		'Creating questionnaires': 'Preparing data collection forms...',
		'Generating questionnaires': 'Building information gathering tools...',
		'Finalizing workflow': 'Putting finishing touches...',
		'Completing workflow': 'Almost ready...',
	};

	// Replace exact matches
	for (const [key, value] of Object.entries(messageMap)) {
		if (cleaned.toLowerCase().includes(key.toLowerCase())) {
			return value;
		}
	}

	// If message contains "workflow" or "generating", make it more user-friendly
	if (cleaned.toLowerCase().includes('workflow') && cleaned.toLowerCase().includes('generat')) {
		return 'Creating your workflow...';
	}

	// If it's a very short message or contains mostly technical info, use a generic message
	if (cleaned.length < 10 || /^\d+\s*(characters?|chars?|bytes?)/i.test(cleaned)) {
		return 'Processing your request...';
	}

	// Trim and ensure proper capitalization
	cleaned = cleaned.trim();
	if (cleaned && cleaned.length > 0) {
		cleaned = cleaned.charAt(0).toUpperCase() + cleaned.slice(1);
		// Ensure it ends with proper punctuation for a processing message
		if (!cleaned.endsWith('...') && !cleaned.endsWith('.') && !cleaned.endsWith('!')) {
			cleaned += '...';
		}
	}

	return cleaned || 'Processing...';
};

// Check if message contains workflow generation keywords
const isGenerateWorkflowIntent = (message: string): boolean => {
	const generateKeywords = [
		// English keywords
		'generate',
		'create',
		'build',
		'make',
		'new workflow',
		'generate workflow',
		'create workflow',
		'build workflow',
		// Chinese keywords
		'生成',
		'创建',
		'制作',
		'构建',
		'新建',
		'新工作流',
		'生成工作流',
		'创建工作流',
		'制作工作流',
		'构建工作流',
	];

	const lowerMessage = message.toLowerCase();
	return generateKeywords.some((keyword) => lowerMessage.includes(keyword.toLowerCase()));
};

// Check if message contains workflow modification keywords
const isModifyWorkflowIntent = (message: string): boolean => {
	const modifyKeywords = [
		// English keywords
		'modify',
		'edit',
		'update',
		'change',
		'adjust',
		'revise',
		'alter',
		'modify workflow',
		'edit workflow',
		'update workflow',
		'change workflow',
		'adjust workflow',
		'revise workflow',
		// Chinese keywords
		'修改',
		'编辑',
		'调整',
		'更新',
		'变更',
		'改变',
		'修订',
		'完善',
		'修改工作流',
		'编辑工作流',
		'调整工作流',
		'更新工作流',
		'变更工作流',
		'改变工作流',
	];

	const lowerMessage = message.toLowerCase();
	return modifyKeywords.some((keyword) => lowerMessage.includes(keyword.toLowerCase()));
};

const sendMessage = async () => {
	if (!currentInput.value.trim() && !uploadedFile.value) return;

	const messageContent =
		currentInput.value || `Uploaded file "${uploadedFile.value?.name}" for workflow analysis`;

	const userMessage: ChatMessage = {
		id: Date.now().toString(),
		type: 'user',
		content: messageContent,
		timestamp: new Date(),
	};
	chatMessages.value.push(userMessage);

	// Clear input immediately
	currentInput.value = '';
	uploadedFile.value = null;
	await scrollToBottom();

	console.log('User message added, current messages:', chatMessages.value.length);

	// Check for workflow generation intent first
	if (isGenerateWorkflowIntent(messageContent)) {
		console.log('Detected workflow generation intent, triggering generation...');
		generateWorkflow();
		return;
	}

	// Check for workflow modification intent
	if (isModifyWorkflowIntent(messageContent)) {
		console.log('Detected workflow modification intent, triggering modification flow...');
		handleWorkflowModification(messageContent);
		return;
	}

	// Check for existing workflow modification intent (original logic)
	const modificationIntent = detectWorkflowModificationIntent(messageContent);
	if (modificationIntent.isModification && modificationIntent.keywords.length > 0) {
		console.log('Detected workflow modification intent:', modificationIntent);

		// Search for workflows based on keywords
		for (const keyword of modificationIntent.keywords) {
			const workflows = await searchWorkflows(keyword);
			if (workflows.length > 0) {
				searchedWorkflows.value = workflows;

				// If only one workflow found, automatically select it and show stages
				if (workflows.length === 1) {
					const workflowWithStages = await getWorkflowWithStages(workflows[0].id);
					if (workflowWithStages) {
						selectedWorkflow.value = workflowWithStages;

						// Add a special message showing the workflow and stages
						const workflowMessage: ChatMessage = {
							id: (Date.now() + 2).toString(),
							type: 'workflow-modification',
							content: `Found workflow: ${workflowWithStages.name}`,
							timestamp: new Date(),
							data: {
								workflow: workflowWithStages,
								stages: workflowWithStages.stages || [],
							},
						};
						chatMessages.value.push(workflowMessage);
						await scrollToBottom();
						saveChatSession();
						return; // Don't proceed with normal AI chat
					}
				} else {
					// Multiple workflows found, show selection
					const selectionMessage: ChatMessage = {
						id: (Date.now() + 2).toString(),
						type: 'workflow-selection',
						content: `Found ${workflows.length} related workflows, please select the workflow to modify:`,
						timestamp: new Date(),
						data: {
							workflows: workflows,
						},
					};
					chatMessages.value.push(selectionMessage);
					await scrollToBottom();
					saveChatSession();
					return; // Don't proceed with normal AI chat
				}
				break;
			}
		}

		// If no workflows found
		if (searchedWorkflows.value.length === 0) {
			const noResultMessage: ChatMessage = {
				id: (Date.now() + 2).toString(),
				type: 'ai',
				content: `No matching workflows found. Please check your keywords or create a new workflow.`,
				timestamp: new Date(),
			};
			chatMessages.value.push(noResultMessage);
			await scrollToBottom();
			saveChatSession();
			return;
		}
	}

	// Add streaming AI message placeholder
	const aiMessageId = (Date.now() + 1).toString();
	const aiMessage: ChatMessage = {
		id: aiMessageId,
		type: 'ai',
		content: '',
		timestamp: new Date(),
	};
	chatMessages.value.push(aiMessage);

	// Save session immediately after adding AI message placeholder
	console.log('💾 Saving session after adding AI message placeholder');
	saveChatSession();

	try {
		// Get current AI model configuration if not already loaded
		if (!currentAIModel.value) {
			try {
				const modelResponse = await getDefaultAIModel();
				if (modelResponse.success && modelResponse.data) {
					currentAIModel.value = modelResponse.data;
				}
			} catch (error) {
				console.warn('Failed to get default AI model, using default settings:', error);
			}
		}

		// Prepare chat messages for API with system prompt
		const apiMessages: AIChatMessage[] = [];

		// Add system prompt to establish context
		apiMessages.push({
			role: 'system',
			content:
				'You are an AI Workflow Assistant specialized in helping users create business workflows. Your role is to understand their business processes and help them design structured workflows with clear stages, responsibilities, and requirements. Always focus on workflow planning, process optimization, and business automation. Ask relevant questions about process steps, stakeholders, timelines, and requirements.',
			timestamp: new Date().toISOString(),
		});

		// Add existing chat history
		const historyMessages = chatMessages.value
			.filter((msg) => msg.type === 'user' || msg.type === 'ai')
			.map((msg) => ({
				role: (msg.type === 'user' ? 'user' : 'assistant') as 'user' | 'assistant',
				content: msg.content,
				timestamp: msg.timestamp.toISOString(),
			}));
		apiMessages.push(...historyMessages);

		// Add current message
		apiMessages.push({
			role: 'user',
			content: messageContent,
			timestamp: new Date().toISOString(),
		});

		// Prepare chat request with model configuration
		const chatRequest = {
			messages: apiMessages,
			context: 'workflow_planning',
			sessionId: conversationId.value || undefined,
			mode: 'workflow_planning' as const,
			// Add model configuration if available
			...(currentAIModel.value && {
				modelId: currentAIModel.value.id.toString(),
				modelProvider: currentAIModel.value.provider,
				modelName: currentAIModel.value.modelName,
			}),
		};

		// Try streaming chat first
		try {
			console.log('💬 Attempting to use native stream chat API');
			isChatStreaming.value = true;
			await streamAIChatMessageNative(
				chatRequest,
				(chunk: string) => {
					// Update the AI message content with streaming chunks
					const messageIndex = chatMessages.value.findIndex(
						(msg) => msg.id === aiMessageId
					);
					if (messageIndex !== -1) {
						chatMessages.value[messageIndex].content += chunk;
						scrollToBottom();
					}
				},
				(data: any) => {
					console.log('Stream chat completed:', data);
					isChatStreaming.value = false;
					if (data?.sessionId) {
						conversationId.value = data.sessionId;
					}
					// Save the current chat session to history after stream completes
					saveChatSession();
				},
				(error: any) => {
					console.warn('Native stream chat failed:', error);
					isChatStreaming.value = false;
					throw error;
				}
			);

			// If we reach here, streaming was successful
			console.log('Stream completed successfully');
			return;
		} catch (streamError) {
			console.warn('Stream chat failed, falling back to regular API:', streamError);
			isChatStreaming.value = false;
		}

		// Fallback to regular API if streaming fails
		const response = await sendAIChatMessage(chatRequest);
		console.log('📡 Frontend: Received response:', response);

		// Handle both wrapped and unwrapped response formats
		const actualResponse = (response as any).data || response;

		if (actualResponse.success && actualResponse.response) {
			if (actualResponse.sessionId) {
				conversationId.value = actualResponse.sessionId;
			}

			// Update AI message with complete response
			const messageIndex = chatMessages.value.findIndex((msg) => msg.id === aiMessageId);
			if (messageIndex !== -1) {
				chatMessages.value[messageIndex].content = actualResponse.response.content;
			}
			console.log('📡 Frontend: Added AI message:', actualResponse.response.content);
		} else {
			throw new Error(actualResponse.message || response.message || 'AI response failed');
		}
	} catch (error) {
		console.error('Chat error:', error);
		isChatStreaming.value = false;
		// Update AI message with fallback response
		const messageIndex = chatMessages.value.findIndex((msg) => msg.id === aiMessageId);
		if (messageIndex !== -1) {
			chatMessages.value[messageIndex].content =
				'I understand you want to create a workflow. To help you design the most effective process, could you tell me more details about:\n\n1. What are the main steps involved in this process?\n2. Who are the key stakeholders or team members that need to be involved?\n3. What are the expected outcomes or deliverables?\n4. Are there any specific requirements or constraints I should consider?\n\nThis information will help me create a structured workflow tailored to your needs.';
		}

		ElMessage.warning('AI service temporarily unavailable, using fallback response');
	}

	await scrollToBottom();

	// Save the current chat session to history
	saveChatSession();
};

const applyWorkflow = async (data: any) => {
	applying.value = true;
	try {
		// Create workflow
		const workflowPayload = {
			name: data.workflow.name,
			description: data.workflow.description,
			isActive: data.workflow.isActive,
			isAIGenerated: true,
			status: 'active',
			startDate: new Date().toISOString(),
			useSameTeamForOperate: true,
			stages: data.stages.map((stage: WorkflowStage, index: number) => ({
				name: stage.name,
				description: stage.description,
				order: stage.order || index + 1,
				defaultAssignedGroup: stage.assignedGroup || 'General',
				estimatedDuration: stage.estimatedDuration || 1,
				isActive: true,
				workflowVersion: '1',
				useSameTeamForOperate: true,
			})),
		};

		const response = await createWorkflow(workflowPayload);
		if (!response.success) {
			throw new Error(response.message || 'Create workflow failed');
		}

		const workflowId = response.data;

		// Create checklists and questionnaires using the new backend method
		// Add a small delay to ensure workflow and stages are fully committed to database
		await new Promise((resolve) => setTimeout(resolve, 500));
		try {
			const { apiVersion } = useGlobSetting();

			// Transform checklists to the expected backend format with stage association
			const transformedChecklists = data.checklists.map((checklist, index) => ({
				Success: true,
				Message: `Checklist generated for ${checklist.name}`,
				GeneratedChecklist: {
					Name: checklist.name,
					Description: checklist.description,
					Team: checklist.team || 'Other',
					IsActive: true,
					Assignments: [], // Will be set by backend
				},
				Tasks: checklist.tasks || [],
				ConfidenceScore: 0.85,
				// Add stage association - match by index since they're generated in the same order
				StageName: data.stages[index]?.name || '',
				StageOrder: data.stages[index]?.order || index + 1,
			}));

			// Transform questionnaires to the expected backend format with stage association
			const transformedQuestionnaires = data.questionnaires.map((questionnaire, index) => ({
				Success: true,
				Message: `Questionnaire generated for ${questionnaire.name}`,
				GeneratedQuestionnaire: {
					Name: questionnaire.name,
					Description: questionnaire.description,
					Category: questionnaire.category || 'General',
					IsActive: true,
					Assignments: [], // Will be set by backend
				},
				Questions: questionnaire.questions || [],
				ConfidenceScore: 0.85,
				// Add stage association - match by index since they're generated in the same order
				StageName: data.stages[index]?.name || '',
				StageOrder: data.stages[index]?.order || index + 1,
			}));

			// Add retry mechanism for stage components creation
			const createStageComponentsWithRetry = async (retryCount = 0) => {
				const maxRetries = 3;
				const retryDelay = 1000; // 1 second

				try {
					const createComponentsResponse = await defHttp.post({
						url: `/api/ai/workflows/${apiVersion}/create-stage-components`,
						data: {
							workflowId: workflowId,
							stages: data.stages,
							checklists: transformedChecklists,
							questionnaires: transformedQuestionnaires,
						},
					});

					if (createComponentsResponse.success) {
						console.log('✅ Stage components created successfully');
						return true;
					} else {
						console.warn(
							`⚠️ Failed to create stage components (attempt ${retryCount + 1}):`,
							createComponentsResponse.message
						);

						if (retryCount < maxRetries) {
							console.log(`🔄 Retrying in ${retryDelay}ms...`);
							await new Promise((resolve) => setTimeout(resolve, retryDelay));
							return await createStageComponentsWithRetry(retryCount + 1);
						}
						return false;
					}
				} catch (error) {
					console.warn(
						`⚠️ Error creating stage components (attempt ${retryCount + 1}):`,
						error
					);

					if (retryCount < maxRetries) {
						console.log(`🔄 Retrying in ${retryDelay}ms...`);
						await new Promise((resolve) => setTimeout(resolve, retryDelay));
						return await createStageComponentsWithRetry(retryCount + 1);
					}
					return false;
				}
			};

			await createStageComponentsWithRetry();
		} catch (e) {
			console.warn('⚠️ Failed to create stage components:', e);
		}

		ElMessage.success('Workflow applied successfully!');

		// Emit for parent component navigation
		emit('workflowGenerated', {
			generatedWorkflow: data.workflow,
			stages: data.stages,
			operationMode: 'create',
		});
	} catch (error) {
		console.error('Apply workflow error:', error);
	} finally {
		applying.value = false;
	}
};

// Stage management
const addStage = (data: any) => {
	const newOrder = Math.max(...data.stages.map((s: WorkflowStage) => s.order), 0) + 1;
	data.stages.push({
		name: `New Stage ${newOrder}`,
		description: '',
		order: newOrder,
		assignedGroup: 'General',
		requiredFields: [],
		estimatedDuration: 1,
	});
};

const removeStage = (data: any, index: number) => {
	data.stages.splice(index, 1);
	// Reorder stages
	data.stages.forEach((stage: WorkflowStage, idx: number) => {
		stage.order = idx + 1;
	});
};

// Checklist management
const addChecklist = (data: any) => {
	if (!data.checklists) data.checklists = [];
	data.checklists.push({
		name: `New Checklist ${data.checklists.length + 1}`,
		description: 'New checklist description',
	});
};

const removeChecklist = (data: any, index: number) => {
	data.checklists.splice(index, 1);
};

// Questionnaire management
const addQuestionnaire = (data: any) => {
	if (!data.questionnaires) data.questionnaires = [];
	data.questionnaires.push({
		name: `New Questionnaire ${data.questionnaires.length + 1}`,
		description: 'New questionnaire description',
	});
};

const removeQuestionnaire = (data: any, index: number) => {
	data.questionnaires.splice(index, 1);
};

// File handling (legacy - now handled by AIFileAnalyzer)

const removeUploadedFile = () => {
	// Clean up URL object for image files to avoid memory leaks
	if (uploadedFile.value && isImageFile(uploadedFile.value)) {
		const previewUrl = getFilePreviewUrl(uploadedFile.value);
		if (previewUrl) {
			URL.revokeObjectURL(previewUrl);
		}
	}

	uploadedFile.value = null;
	ElMessage.info('File removed');
};

const isImageFile = (file: File) => {
	const imageTypes = [
		'image/jpeg',
		'image/jpg',
		'image/png',
		'image/gif',
		'image/bmp',
		'image/webp',
		'image/svg+xml',
	];
	return imageTypes.includes(file.type);
};

const isPDFFile = (file: File) => {
	return file.type === 'application/pdf' || file.name.toLowerCase().endsWith('.pdf');
};

const isWordFile = (file: File) => {
	const wordTypes = [
		'application/vnd.openxmlformats-officedocument.wordprocessingml.document', // .docx
		'application/msword', // .doc
	];
	return (
		wordTypes.includes(file.type) ||
		file.name.toLowerCase().endsWith('.docx') ||
		file.name.toLowerCase().endsWith('.doc')
	);
};

// 文件图标获取函数（暂时保留以备将来使用）
// const getFileIcon = (file: File) => {
// 	if (isImageFile(file)) return 'Picture';
// 	if (isPDFFile(file)) return 'Document';
// 	if (isWordFile(file)) return 'Document';
// 	return 'Document';
// };

const getFilePreviewUrl = (file: File): string | undefined => {
	if (isImageFile(file)) {
		return URL.createObjectURL(file);
	}
	return undefined;
};

const getFileTypeName = (file: File): string => {
	const extension = file.name.split('.').pop()?.toUpperCase();
	if (isPDFFile(file)) return 'PDF Document';
	if (isWordFile(file)) return 'Word Document';
	if (isImageFile(file)) return 'Image File';
	return extension ? `${extension} File` : 'Document';
};

const formatFileSize = (bytes: number): string => {
	if (bytes === 0) return '0 Bytes';
	const k = 1024;
	const sizes = ['Bytes', 'KB', 'MB', 'GB'];
	const i = Math.floor(Math.log(bytes) / Math.log(k));
	return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
};

// Chat history management
const saveChatSession = () => {
	console.log('saveChatSession called, chatMessages length:', chatMessages.value.length);
	if (chatMessages.value.length === 0) {
		console.log('No chat messages to save');
		return;
	}

	const sessionId = currentSessionId.value || Date.now().toString();

	// Check if session already exists to preserve custom title
	const existingIndex = chatHistory.value.findIndex((s) => s.id === sessionId);
	let title: string;

	if (existingIndex >= 0) {
		// If session exists, preserve the existing title (user might have renamed it)
		title = chatHistory.value[existingIndex].title;
		console.log('Preserving existing session title:', title);
	} else {
		// If it's a new session, generate title from first user message
		const userMessage = chatMessages.value.find((msg) => msg.type === 'user');
		title = userMessage
			? userMessage.content.slice(0, 50) + (userMessage.content.length > 50 ? '...' : '')
			: 'New Chat';
		console.log('Generated new session title:', title);
	}

	console.log('💾 Creating session:', {
		sessionId,
		title,
		messageCount: chatMessages.value.length,
	});

	const session: ChatSession = {
		id: sessionId,
		title,
		timestamp: new Date(),
		messages: [...chatMessages.value],
	};

	if (existingIndex >= 0) {
		console.log('Updating existing session at index:', existingIndex);
		chatHistory.value[existingIndex] = session;
	} else {
		console.log('Adding new session to history');
		chatHistory.value.unshift(session);
	}

	currentSessionId.value = sessionId;

	console.log('💾 Chat history now has', chatHistory.value.length, 'sessions');

	// Save to localStorage
	saveChatHistoryToStorage();
};

const loadChatSession = (sessionId: string) => {
	const session = chatHistory.value.find((s) => s.id === sessionId);
	if (session) {
		chatMessages.value = [...session.messages];
		currentSessionId.value = sessionId;
		scrollToBottom();
	}
};

const clearChat = () => {
	// Stop any ongoing streaming
	stopStreaming();

	chatMessages.value = [];
	currentSessionId.value = '';
	conversationId.value = '';
	currentInput.value = '';
	uploadedFile.value = null;
	streamingMessage.value = '';
	generating.value = false;
};

const startNewChat = () => {
	// Clear current chat
	clearChat();
	// Collapse history sidebar
	isHistoryCollapsed.value = false;
};

// Enhanced Chat History Methods

// Workflow Search and Modification Methods
const globSetting = useGlobSetting();

const searchWorkflows = async (query: string): Promise<any[]> => {
	try {
		isSearchingWorkflows.value = true;

		const response = await defHttp.post({
			url: `${globSetting.apiProName}/ow/workflows/${globSetting.apiVersion}/query`,
			data: {
				PageIndex: 1,
				PageSize: 10,
				Name: query,
				IsActive: true,
			},
		});

		if (response.success && response.data?.items) {
			return response.data.items;
		}
		return [];
	} catch (error) {
		console.error('Error searching workflows:', error);
		ElMessage.error('Failed to search workflows');
		return [];
	} finally {
		isSearchingWorkflows.value = false;
	}
};

const getWorkflowWithStages = async (workflowId: number): Promise<any | null> => {
	try {
		const response = await defHttp.get({
			url: `${globSetting.apiProName}/ow/workflows/${globSetting.apiVersion}/${workflowId}`,
		});

		if (response.success && response.data) {
			// Get stages for this workflow
			const stagesResponse = await defHttp.get({
				url: `${globSetting.apiProName}/ow/workflows/${globSetting.apiVersion}/${workflowId}/stages`,
			});

			if (stagesResponse.success && stagesResponse.data) {
				response.data.stages = stagesResponse.data;
			}

			return response.data;
		}
		return null;
	} catch (error) {
		console.error('Error getting workflow with stages:', error);
		ElMessage.error('Failed to get workflow details');
		return null;
	}
};

const detectWorkflowModificationIntent = (
	message: string
): { isModification: boolean; keywords: string[] } => {
	const modificationKeywords = ['modify', 'edit', 'update', 'change', 'adjust', 'optimize'];
	const workflowKeywords = ['workflow', 'process', 'flow'];

	const lowerMessage = message.toLowerCase();
	const isModification = modificationKeywords.some((keyword) => lowerMessage.includes(keyword));
	const hasWorkflow = workflowKeywords.some((keyword) => lowerMessage.includes(keyword));

	if (isModification && hasWorkflow) {
		// Extract potential workflow names (simple extraction)
		const words = message.split(/\s+|，|。|、/);
		const keywords = words.filter(
			(word) =>
				word.length > 1 &&
				!modificationKeywords.includes(word) &&
				!workflowKeywords.includes(word)
		);

		return { isModification: true, keywords };
	}

	return { isModification: false, keywords: [] };
};

const selectWorkflowForModification = async (workflow: any) => {
	try {
		const workflowWithStages = await getWorkflowWithStages(workflow.id);
		if (workflowWithStages) {
			selectedWorkflow.value = workflowWithStages;

			// Add a workflow modification message
			const workflowMessage: ChatMessage = {
				id: Date.now().toString(),
				type: 'workflow-modification',
				content: `Selected workflow: ${workflowWithStages.name}`,
				timestamp: new Date(),
				data: {
					workflow: workflowWithStages,
					stages: workflowWithStages.stages || [],
				},
			};
			chatMessages.value.push(workflowMessage);
			await scrollToBottom();
			saveChatSession();
		}
	} catch (error) {
		console.error('Error selecting workflow:', error);
		ElMessage.error('Failed to select workflow');
	}
};

const onStageUpdated = (messageData: any, stageIndex: number) => {
	// Mark the workflow as modified
	if (messageData.workflow) {
		messageData.workflow.isModified = true;
	}
	console.log('Stage updated:', stageIndex, messageData.stages[stageIndex]);
};

const onWorkflowUpdated = (messageData: any) => {
	// Mark the workflow as modified
	if (messageData.workflow) {
		messageData.workflow.isModified = true;
	}
	console.log('Workflow updated:', messageData.workflow);
};

const onChecklistUpdated = (messageData: any, checklistIndex: number) => {
	// Mark the workflow as modified
	if (messageData.workflow) {
		messageData.workflow.isModified = true;
	}
	console.log('Checklist updated:', messageData.checklists?.[checklistIndex]);
};

const onQuestionnaireUpdated = (messageData: any, questionnaireIndex: number) => {
	// Mark the workflow as modified
	if (messageData.workflow) {
		messageData.workflow.isModified = true;
	}
	console.log('Questionnaire updated:', messageData.questionnaires?.[questionnaireIndex]);
};

// Validate workflow data before saving
const validateWorkflowData = (messageData: any): boolean => {
	if (!messageData.workflow) {
		ElMessage.error('Workflow data is missing');
		return false;
	}

	if (!messageData.workflow.id) {
		ElMessage.error('Workflow ID is missing');
		return false;
	}

	if (!messageData.stages || !Array.isArray(messageData.stages)) {
		ElMessage.error('Stage data is missing or invalid');
		return false;
	}

	// Check if all stages have required fields
	for (const stage of messageData.stages) {
		if (!stage.name || !stage.name.trim()) {
			ElMessage.error('All stages must have a name');
			return false;
		}
		if (!stage.id) {
			ElMessage.error('Stage ID is missing');
			return false;
		}
	}

	return true;
};

const saveWorkflowChanges = async (messageData: any) => {
	if (!validateWorkflowData(messageData)) {
		return;
	}

	try {
		applying.value = true;

		// Update workflow first
		const workflowResponse = await defHttp.put({
			url: `${globSetting.apiProName}/ow/workflows/${globSetting.apiVersion}/${messageData.workflow.id}`,
			data: {
				name: messageData.workflow.name,
				description: messageData.workflow.description,
				isActive: messageData.workflow.isActive,
				useSameTeamForOperate: true,
			},
		});

		if (!workflowResponse.success) {
			throw new Error('Failed to update workflow');
		}

		// Update stages with proper error handling
		for (const stage of messageData.stages) {
			if (stage.id) {
				try {
					// Ensure we have the workflow ID in the stage data
					const stageUpdateData = {
						name: stage.name,
						description: stage.description,
						order: stage.order,
						workflowId: messageData.workflow.id, // Explicitly include workflow ID
						defaultAssignedGroup: stage.assignedGroup || stage.defaultAssignedGroup,
						estimatedDuration: stage.estimatedDuration,
						useSameTeamForOperate: true,
					};

					console.log('Updating stage:', stage.id, 'with data:', stageUpdateData);

					await defHttp.put({
						url: `${globSetting.apiProName}/ow/stages/${globSetting.apiVersion}/${stage.id}`,
						data: stageUpdateData,
					});
				} catch (stageError) {
					console.error(`Error updating stage ${stage.id}:`, stageError);

					// If it's a foreign key constraint error, try different approaches
					if (
						stageError.response?.status === 400 &&
						stageError.response?.data?.message?.includes('Foreign key constraint')
					) {
						console.log(
							'Foreign key constraint detected. Trying alternative approaches...'
						);

						// First, try to refresh the stage data from server
						try {
							const currentStageResponse = await defHttp.get({
								url: `${globSetting.apiProName}/ow/stages/${globSetting.apiVersion}/${stage.id}`,
							});

							if (currentStageResponse.success && currentStageResponse.data) {
								// Use the current server data as base and only update what we need
								const serverStageData = currentStageResponse.data;
								await defHttp.put({
									url: `${globSetting.apiProName}/ow/stages/${globSetting.apiVersion}/${stage.id}`,
									data: {
										...serverStageData,
										name: stage.name,
										description: stage.description,
										order: stage.order,
										estimatedDuration: stage.estimatedDuration,
										// Only update assignedGroup if it's different from server
										defaultAssignedGroup:
											stage.assignedGroup !==
											serverStageData.defaultAssignedGroup
												? stage.assignedGroup
												: serverStageData.defaultAssignedGroup,
										useSameTeamForOperate: true,
									},
								});
							} else {
								throw new Error('Could not fetch current stage data');
							}
						} catch (refreshError) {
							console.log('Could not refresh stage data, trying minimal update...');
							// Last resort: minimal update without assignedGroup
							await defHttp.put({
								url: `${globSetting.apiProName}/ow/stages/${globSetting.apiVersion}/${stage.id}`,
								data: {
									name: stage.name,
									description: stage.description,
									order: stage.order,
									estimatedDuration: stage.estimatedDuration,
									useSameTeamForOperate: true,
								},
							});
						}
					} else {
						throw stageError;
					}
				}
			}
		}

		ElMessage.success('Workflow changes saved successfully');

		// Add success message
		const successMessage: ChatMessage = {
			id: Date.now().toString(),
			type: 'ai',
			content: `Changes to workflow "${messageData.workflow.name}" have been saved successfully!`,
			timestamp: new Date(),
		};
		chatMessages.value.push(successMessage);
		await scrollToBottom();
		saveChatSession();
	} catch (error) {
		console.error('Error saving workflow changes:', error);

		// Provide more specific error messages
		if (error.response?.status === 400) {
			const errorMessage =
				error.response?.data?.message || error.response?.data?.msg || 'Bad request';
			if (errorMessage.includes('Foreign key constraint')) {
				ElMessage.error(
					'Failed to save changes: Data integrity issue. Please refresh and try again.'
				);
			} else {
				ElMessage.error(`Failed to save changes: ${errorMessage}`);
			}
		} else if (error.response?.status === 404) {
			ElMessage.error(
				'Failed to save changes: Workflow or stage not found. Please refresh the page.'
			);
		} else if (error.response?.status === 500) {
			ElMessage.error('Failed to save changes: Server error. Please try again later.');
		} else {
			ElMessage.error('Failed to save changes. Please check your connection and try again.');
		}
	} finally {
		applying.value = false;
	}
};

const formatRelativeTime = (timestamp: Date) => {
	const now = new Date();
	const diff = now.getTime() - timestamp.getTime();
	const minutes = Math.floor(diff / (1000 * 60));
	const hours = Math.floor(diff / (1000 * 60 * 60));
	const days = Math.floor(diff / (1000 * 60 * 60 * 24));

	if (minutes < 1) return 'Just now';
	if (minutes < 60) return `${minutes}m ago`;
	if (hours < 24) return `${hours}h ago`;
	if (days < 7) return `${days}d ago`;
	return timestamp.toLocaleDateString();
};

const handleSessionAction = (command: string) => {
	const [action, sessionId] = command.split('-');
	const session = chatHistory.value.find((s) => s.id === sessionId);

	if (!session) return;

	switch (action) {
		case 'pin':
			togglePinSession(sessionId);
			break;
		case 'rename':
			startRenameSession(sessionId);
			break;
		case 'delete':
			deleteSession(sessionId);
			break;
	}
};

const togglePinSession = (sessionId: string) => {
	const session = chatHistory.value.find((s) => s.id === sessionId);
	if (session) {
		session.isPinned = !session.isPinned;
		saveChatHistoryToStorage();
	}
};

const startRenameSession = (sessionId: string) => {
	const session = chatHistory.value.find((s) => s.id === sessionId);
	if (session) {
		renameSessionId.value = sessionId;
		newSessionTitle.value = session.title;
		showRenameDialog.value = true;
	}
};

const confirmRenameSession = () => {
	const session = chatHistory.value.find((s) => s.id === renameSessionId.value);
	if (session && newSessionTitle.value.trim()) {
		session.title = newSessionTitle.value.trim();
		saveChatHistoryToStorage();
		showRenameDialog.value = false;
		ElMessage.success('Session renamed successfully');
	}
};

const deleteSession = (sessionId: string) => {
	ElMessageBox.confirm(
		'Are you sure you want to delete this chat session? This action cannot be undone.',
		'Delete Chat Session',
		{
			confirmButtonText: 'Delete',
			cancelButtonText: 'Cancel',
			type: 'warning',
		}
	)
		.then(() => {
			const index = chatHistory.value.findIndex((s) => s.id === sessionId);
			if (index >= 0) {
				chatHistory.value.splice(index, 1);
				saveChatHistoryToStorage();

				// If deleted session was current, clear chat
				if (currentSessionId.value === sessionId) {
					clearChat();
				}

				ElMessage.success('Chat session deleted');
			}
		})
		.catch(() => {
			// User cancelled
		});
};

const clearAllHistory = () => {
	ElMessageBox.confirm(
		'Are you sure you want to clear all chat history? This action cannot be undone.',
		'Clear All History',
		{
			confirmButtonText: 'Clear All',
			cancelButtonText: 'Cancel',
			type: 'warning',
		}
	)
		.then(() => {
			chatHistory.value = [];
			saveChatHistoryToStorage();
			clearChat();
			ElMessage.success('All chat history cleared');
		})
		.catch(() => {
			// User cancelled
		});
};

const showContextMenu = (event: MouseEvent, session: ChatSession) => {
	// Context menu functionality can be implemented here
	event.preventDefault();
};

const saveChatHistoryToStorage = () => {
	const storageKey = getChatHistoryStorageKey();
	localStorage.setItem(storageKey, JSON.stringify(chatHistory.value));
	console.log('💾 Chat history saved with key:', storageKey);
};

// File Analysis Handlers
const handleFileAnalyzed = (content: string, fileName: string) => {
	console.log('File analyzed:', fileName, 'Content length:', content.length);

	// Add a user message showing the file was uploaded
	const fileMessage: ChatMessage = {
		id: Date.now().toString(),
		type: 'user',
		content: `Uploaded file: ${fileName}\n\nContent preview:\n${content.substring(0, 500)}${
			content.length > 500 ? '...' : ''
		}`,
		timestamp: new Date(),
	};

	chatMessages.value.push(fileMessage);
	saveChatSession();
	scrollToBottom();

	ElMessage.success(`File "${fileName}" has been analyzed and content extracted`);
};

// Track current AI response message ID
let currentAIMessageId = '';

const handleAnalysisStarted = (fileName: string) => {
	console.log('Analysis started for file:', fileName);

	// Create a new AI message for streaming response
	currentAIMessageId = (Date.now() + 1).toString();
	const aiMessage: ChatMessage = {
		id: currentAIMessageId,
		type: 'ai',
		content: '', // Start with empty content, will be filled by streaming response
		timestamp: new Date(),
	};

	chatMessages.value.push(aiMessage);
	saveChatSession();
	scrollToBottom();
};

const handleStreamChunk = (chunk: string) => {
	// Find current AI message and update content
	const messageIndex = chatMessages.value.findIndex((msg) => msg.id === currentAIMessageId);
	if (messageIndex !== -1) {
		chatMessages.value[messageIndex].content += chunk;
		scrollToBottom();
	}
};

const handleAnalysisComplete = (result: any) => {
	console.log('Analysis complete:', result);

	// Save session
	saveChatSession();
};

// Utility functions
const formatTime = (timestamp: Date) => {
	return timestamp.toLocaleTimeString('en-US', {
		hour: '2-digit',
		minute: '2-digit',
	});
};

// Check if session contains file attachment
const isFileSession = (session: ChatSession): boolean => {
	return session.messages.some(
		(msg) =>
			msg.type === 'user' &&
			(msg.content.includes('📎 Uploaded file:') ||
				msg.content.includes('Uploaded file:') ||
				msg.content.includes('Uploaded file "') ||
				msg.content.includes('for workflow analysis'))
	);
};

const formatAIMessage = (content: string) => {
	return content.replace(/\n/g, '<br>');
};

const getTotalDuration = (stages: WorkflowStage[]) => {
	return stages.reduce((sum, stage) => sum + stage.estimatedDuration, 0);
};

const getStageNameFromChecklist = (checklistName: string) => {
	return checklistName.replace(/ Checklist$/i, '');
};

const getStageNameFromQuestionnaire = (questionnaireName: string) => {
	return questionnaireName.replace(/ Questionnaire$/i, '');
};

const scrollToBottom = async () => {
	await nextTick();
	if (chatMessagesRef.value) {
		chatMessagesRef.value.scrollTop = chatMessagesRef.value.scrollHeight;
	}
};

// Collapse/Expand methods
const toggleChecklistsCollapse = () => {
	checklistsCollapsed.value = !checklistsCollapsed.value;
};

const toggleQuestionnairesCollapse = () => {
	questionnairesCollapsed.value = !questionnairesCollapsed.value;
};

const toggleChecklistTasks = (checklistIndex: number) => {
	if (collapsedChecklistTasks.value.has(checklistIndex)) {
		collapsedChecklistTasks.value.delete(checklistIndex);
	} else {
		collapsedChecklistTasks.value.add(checklistIndex);
	}
};

const toggleQuestionnaireQuestions = (questionnaireIndex: number) => {
	if (collapsedQuestionnaireQuestions.value.has(questionnaireIndex)) {
		collapsedQuestionnaireQuestions.value.delete(questionnaireIndex);
	} else {
		collapsedQuestionnaireQuestions.value.add(questionnaireIndex);
	}
};

const isChecklistTasksCollapsed = (checklistIndex: number) => {
	return collapsedChecklistTasks.value.has(checklistIndex);
};

const isQuestionnaireQuestionsCollapsed = (questionnaireIndex: number) => {
	return collapsedQuestionnaireQuestions.value.has(questionnaireIndex);
};

// Model management
const handleModelChange = (model: AIModelConfig) => {
	currentAIModel.value = model;
	ElMessage.success(`Switched to ${model.provider.toLowerCase()} ${model.modelName}`);
	console.log('Model changed to:', model);
};

// Open AI Model Configuration dialog
const navigateToAIModelConfig = () => {
	console.log('🔧 Opening AI Model Configuration...');
	emit('open-ai-config');
};

// Keyboard event handling
const handleKeydown = (event: KeyboardEvent) => {
	if (event.key === 'Enter') {
		if (event.shiftKey) {
			// Shift+Enter: Allow default behavior (new line)
			return;
		} else {
			// Enter: Send message
			event.preventDefault();
			if ((currentInput.value.trim() || uploadedFile.value) && !generating.value) {
				sendMessage();
			}
		}
	}
};

// Lifecycle
onMounted(async () => {
	// Load available AI models
	try {
		const modelsResponse = await getUserAIModels();
		if (modelsResponse.success && modelsResponse.data) {
			availableModels.value = modelsResponse.data;
			console.log('Loaded available AI models:', modelsResponse.data.length);
		}
	} catch (error) {
		console.warn('Failed to load available AI models:', error);
	}

	// Load default AI model configuration
	try {
		const modelResponse = await getDefaultAIModel();
		if (modelResponse.success && modelResponse.data) {
			currentAIModel.value = modelResponse.data;
			console.log('Loaded default AI model:', modelResponse.data.modelName);
		} else {
			console.log('⚠️ No AI model configured');
		}
	} catch (error) {
		console.warn('Failed to load default AI model:', error);
	}

	// Set loading complete
	isLoadingModel.value = false;

	// Load chat history from localStorage with tenant and user isolation
	const storageKey = getChatHistoryStorageKey();
	const savedHistory = localStorage.getItem(storageKey);
	console.log('📥 Loading chat history with key:', storageKey);
	if (savedHistory) {
		try {
			const parsed = JSON.parse(savedHistory);
			chatHistory.value = parsed.map((session: any) => ({
				...session,
				timestamp: new Date(session.timestamp),
				messages: session.messages.map((msg: any) => ({
					...msg,
					timestamp: new Date(msg.timestamp),
				})),
			}));
			console.log(
				'✅ Chat history loaded successfully:',
				chatHistory.value.length,
				'sessions'
			);
		} catch (e) {
			console.warn('Failed to load chat history:', e);
		}
	} else {
		console.log('ℹ️ No saved chat history found for this user');
	}

	// Add initial AI message if no messages exist
	if (chatMessages.value.length === 0) {
		const initialMessage: ChatMessage = {
			id: 'initial',
			type: 'ai',
			content:
				"Hello! I'm your AI Workflow Assistant. I'm here to help you create the perfect workflow by understanding your specific business needs and requirements.\n\nTo get started, could you tell me what type of process or workflow you're looking to create? For example, it could be employee onboarding, customer support, project approval, or any other business process you have in mind.",
			timestamp: new Date(),
		};
		chatMessages.value.push(initialMessage);
	}
});
</script>

<style scoped lang="scss">
.ai-workflow-assistant {
	width: 100%;
}

.assistant-card {
	display: flex;
	flex-direction: column;
	margin-bottom: 0;
}

.assistant-card .el-card__body {
	padding-bottom: 0 !important;
}

.ai_card-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	font-weight: 600;
}

.header-left {
	display: flex;
	align-items: center;
	gap: 12px;
}

.assistant-title {
	font-size: 18px;
	font-weight: 700;
}

.status-indicator {
	display: flex;
	align-items: center;
	gap: 8px;
	color: var(--el-color-success);
	font-size: 14px;
}

.add-model-btn {
	font-size: 13px;
	padding: 6px 16px;
	border-radius: 8px;
	font-weight: 600;
	transition: all 0.3s ease;
	box-shadow: 0 2px 6px rgba(0, 0, 0, 0.1);
}

.add-model-btn:hover {
	transform: translateY(-1px);
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.add-model-btn .el-icon {
	margin-right: 4px;
}

.pulse-dot {
	width: 8px;
	height: 8px;
	background: var(--el-color-success);
	border-radius: 50%;
	animation: pulse 2s infinite;
}

@keyframes pulse {
	0% {
		opacity: 0.5;
	}
	50% {
		opacity: 1;
	}
	100% {
		opacity: 0.5;
	}
}

.assistant-container {
	display: flex;
	align-items: stretch;
	gap: 1rem;
	height: clamp(500px, calc(100vh - 200px), 800px);
	min-height: 500px;
	max-height: 800px;
}

/* Model Setup Guide */
.model-setup-guide {
	flex: 1;
	display: flex;
	align-items: center;
	justify-content: center;
	height: 100%;
	background: var(--el-fill-color-light);
	border-radius: 12px;
	padding: 2rem;
}

.guide-content {
	text-align: center;
	max-width: 500px;
}

.guide-title {
	font-size: 24px;
	font-weight: 700;
	color: var(--el-text-color-primary);
	margin-bottom: 1rem;
}

.guide-description {
	font-size: 16px;
	color: var(--el-text-color-secondary);
	margin-bottom: 2rem;
	line-height: 1.6;
}

.guide-action-btn {
	font-size: 16px;
	padding: 12px 32px;
	border-radius: 8px;
	font-weight: 600;
	box-shadow: 0 4px 12px rgba(64, 158, 255, 0.3);
	transition: all 0.3s ease;
}

.guide-action-btn:hover {
	transform: translateY(-2px);
	box-shadow: 0 6px 16px rgba(64, 158, 255, 0.4);
}

.guide-action-btn .el-icon {
	margin-right: 8px;
}

/* 针对笔记本电脑屏幕 */
@media (max-height: 900px) {
	.assistant-container {
		height: clamp(500px, calc(100vh - 180px), 500px);
		max-height: 500px;
	}

	.chat-messages {
		max-height: calc(clamp(500px, calc(100vh - 180px), 500px) - 130px);
		min-height: 300px;
	}
}

/* 针对大显示器 */
@media (min-height: 1000px) {
	.assistant-container {
		height: clamp(800px, calc(100vh - 220px), 800px);
		max-height: 800px;
	}

	.chat-messages {
		max-height: calc(clamp(800px, calc(100vh - 220px), 800px) - 150px);
		min-height: 450px;
	}
}

.chat-area {
	flex: 1;
	display: flex;
	flex-direction: column;
	height: 100%;
}

.chat-messages {
	flex: 1;
	overflow-y: auto;
	padding: 1rem;
	display: flex;
	flex-direction: column;
	gap: 1rem;
	max-height: calc(clamp(500px, calc(100vh - 200px), 860px) - 150px);
	min-height: 350px;
}

/* Custom scrollbar styles */
.chat-messages::-webkit-scrollbar {
	width: 8px;
}

.chat-messages::-webkit-scrollbar-track {
	background: var(--el-fill-color-lighter);
	@apply rounded-xl;
}

.chat-messages::-webkit-scrollbar-thumb {
	background: var(--el-border-color);
	transition: background 0.2s ease;
	@apply rounded-xl;
}

.chat-messages::-webkit-scrollbar-thumb:hover {
	background: var(--el-border-color-dark);
}

.message-item {
	animation: fadeInUp 0.3s ease-out;
}

@keyframes fadeInUp {
	from {
		opacity: 0;
		transform: translateY(20px);
	}
	to {
		opacity: 1;
		transform: translateY(0);
	}
}

.user-message {
	display: flex;
	justify-content: flex-end;
	gap: 0.5rem;
}

.user-message .message-content {
	background: var(--el-color-primary);
	color: var(--el-color-white);
	padding: 0.75rem 1rem;
	max-width: 70%;
	@apply rounded-xl;
}

.user-message .message-avatar {
	width: 32px;
	height: 32px;
	background: var(--el-color-primary);
	border-radius: 50%;
	display: flex;
	align-items: center;
	justify-content: center;
	color: var(--el-color-white);
	font-size: 16px;
}

.ai-message {
	display: flex;
	gap: 0.5rem;
}

.ai-message .message-avatar {
	width: 32px;
	height: 32px;
	background: var(--el-color-success);
	border-radius: 50%;
	display: flex;
	align-items: center;
	justify-content: center;
	color: var(--el-color-white);
	font-size: 16px;
}

.ai-message .message-content {
	background: var(--el-fill-color-light);
	padding: 0.75rem 1rem;
	max-width: 70%;
	@apply rounded-xl;
}

.ai-message.streaming .message-content {
	background: linear-gradient(
		90deg,
		var(--el-fill-color-light) 0%,
		var(--el-fill-color) 50%,
		var(--el-fill-color-light) 100%
	);
	background-size: 200% 100%;
	animation: shimmer 2s infinite;
}

@keyframes shimmer {
	0% {
		background-position: -200% 0;
	}
	100% {
		background-position: 200% 0;
	}
}

.typing-indicator {
	animation: blink 1s infinite;
}

@keyframes blink {
	0%,
	50% {
		opacity: 1;
	}
	51%,
	100% {
		opacity: 0;
	}
}

.message-text {
	margin-bottom: 0.25rem;
	line-height: 1.5;
}

.message-time {
	font-size: 12px;
	opacity: 0.7;
}

.thinking-indicator {
	color: var(--el-color-warning);
	font-weight: 500;
	margin-right: 8px;
	animation: fadeInOut 2s infinite;
}

@keyframes fadeInOut {
	0%,
	100% {
		opacity: 0.5;
	}
	50% {
		opacity: 1;
	}
}

.generation-complete {
	background: var(--el-color-success-light-9);
	border: 1px solid var(--el-color-success-light-7);
	padding: 1.5rem;
	margin: 1rem 0;
	@apply rounded-xl;
}

.complete-header {
	display: flex;
	align-items: center;
	gap: 0.5rem;
	margin-bottom: 1rem;
}

.success-icon {
	color: var(--el-color-success);
	font-size: 24px;
}

.complete-header h4 {
	margin: 0;
	color: var(--el-color-success-dark-2);
	font-size: 18px;
	font-weight: 600;
}

.workflow-preview {
	display: flex;
	flex-direction: column;
	gap: 1rem;
}

.workflow-header {
	display: flex;
	align-items: center;
	gap: 1rem;
	margin-bottom: 0.75rem;
}

.workflow-name-input {
	flex: 1;
}

.workflow-name-input .el-input__wrapper {
	font-size: 18px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	border: 1px solid transparent;
	background: transparent;
	transition: all 0.2s ease;
}

.workflow-name-input .el-input__wrapper:hover {
	border-color: var(--el-border-color);
	background: var(--el-fill-color-blank);
}

.workflow-name-input .el-input__wrapper.is-focus {
	border-color: var(--el-color-primary);
	background: var(--el-bg-color);
	box-shadow: 0 0 0 3px rgba(0, 0, 0, 0.05);
}

.workflow-description-input {
	margin-bottom: 1rem;
}

.workflow-description-input .el-textarea__inner {
	color: var(--el-text-color-secondary);
	font-size: 14px;
	line-height: 1.5;
	border: 1px solid transparent;
	background: transparent;
	transition: all 0.2s ease;
}

.workflow-description-input .el-textarea__inner:hover {
	border-color: var(--el-border-color);
	background: var(--el-fill-color-blank);
}

.workflow-description-input .el-textarea__inner:focus {
	border-color: var(--el-color-primary);
	background: var(--el-bg-color);
	box-shadow: 0 0 0 3px rgba(0, 0, 0, 0.05);
}

.status {
	padding: 4px 12px;
	font-size: 12px;
	font-weight: 500;
	background: var(--el-fill-color-lighter);
	color: var(--el-text-color-secondary);
	white-space: nowrap;
	@apply rounded-xl;
}

.status.active {
	background: var(--el-color-success-light-9);
	color: var(--el-color-success);
}

.workflow-stats {
	display: flex;
	gap: 1rem;
}

.stat-item {
	display: flex;
	align-items: center;
	gap: 0.25rem;
	color: var(--el-text-color-secondary);
	font-size: 14px;
}

.stages-grid {
	display: grid;
	grid-template-columns: repeat(3, 1fr);
	gap: 1rem;
	margin-bottom: 1rem;
}

.stage-card {
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-light);
	padding: 1rem;
	display: flex;
	flex-direction: column;
	gap: 0.75rem;
	transition: all 0.2s ease;
	box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
	@apply rounded-xl;
}

.stage-card:hover {
	border-color: var(--el-color-primary);
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.stage- ai_card-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	margin-bottom: 0.5rem;
}

.stage-badge {
	display: flex;
	align-items: center;
	gap: 0.5rem;
}

.stage-number {
	background: var(--el-color-primary);
	color: var(--el-color-white);
	width: 28px;
	height: 28px;
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 14px;
	font-weight: 700;
	box-shadow: 0 2px 4px rgba(0, 0, 0, 0.15);
	@apply rounded-xl;
}

.stage-card-content {
	display: flex;
	flex-direction: column;
	gap: 0.75rem;
}

.stage-title-input {
	font-weight: 600;
}

.stage-description {
	font-size: 13px;
}

.remove-stage-btn {
	width: 28px;
	height: 28px;
	opacity: 0.6;
	transition: opacity 0.2s ease;
	background-color: var(--el-color-info) !important;
	border-color: var(--el-color-info) !important;
}

.remove-stage-btn:hover {
	opacity: 1;
	background-color: var(--el-color-info) !important;
	border-color: var(--el-color-info) !important;
}

.stage-meta-compact {
	display: grid;
	grid-template-columns: 1fr 1fr;
	gap: 0.75rem;
	padding: 0.75rem;
	background: var(--el-fill-color-blank);
	border: 1px solid var(--el-border-color-light);
	@apply rounded-xl;
}

.meta-item {
	display: flex;
	flex-direction: column;
	gap: 0.25rem;
}

.meta-label {
	font-size: 12px;
	font-weight: 600;
	color: var(--el-text-color-secondary);
	text-transform: uppercase;
	letter-spacing: 0.5px;
}

.meta-select {
	width: 100%;
}

.meta-number {
	width: 100%;
}

.add-stage-btn {
	align-self: flex-start;
	border: 2px dashed var(--el-border-color);
	background: var(--el-fill-color-blank);
	color: var(--el-text-color-secondary);
	padding: 0.75rem 1rem;
	transition: all 0.2s ease;
	@apply rounded-xl;
}

.add-stage-btn:hover {
	border-color: var(--el-color-primary);
	color: var(--el-color-primary);
	background: var(--el-fill-color-lighter);
}

.additional-components {
	display: grid;
	grid-template-columns: 1fr 1fr;
	gap: 1rem;
	margin-top: 1rem;
}

.component-section h6 {
	margin: 0 0 0.5rem 0;
	color: var(--el-text-color-regular);
	font-size: 14px;
	font-weight: 600;
}

.component-list {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
	margin-bottom: 0.75rem;
}

.component-item {
	display: flex;
	gap: 0.5rem;
	align-items: center;
}

.component-item .el-input {
	flex: 1;
}

.apply-section {
	margin-top: 1.5rem;
	text-align: center;
}

.apply-btn {
	min-width: 200px;
	height: 48px;
	font-size: 16px;
	font-weight: 600;
	background: var(--el-color-success);
	border: none;
	transition: all 0.3s ease;
	@apply rounded-xl;
}

.apply-btn:hover {
	transform: translateY(-2px);
	box-shadow: 0 8px 25px rgba(0, 0, 0, 0.15);
}

.input-area {
	padding: 1rem 1rem 0 1rem;
	margin: 0;
	display: flex;
	flex-direction: column;
}

.ai-model-selector-bottom {
	display: flex;
	align-items: center;
	gap: 0.75rem;
	flex-shrink: 0;
}

.model-selector-label {
	font-size: 0.875rem;
	font-weight: 500;
	color: var(--el-text-color-regular);
	min-width: 50px;
	flex-shrink: 0;
}

.model-option {
	display: flex;
	justify-content: space-between;
	align-items: center;
	width: 100%;
}

.model-info {
	display: flex;
	align-items: center;
}

.model-display {
	font-size: 0.875rem;
	color: var(--el-text-color-primary);
	font-weight: 400;
}

.model-status {
	display: flex;
	align-items: center;
}

.status-dot {
	width: 8px;
	height: 8px;
	border-radius: 50%;
	background-color: var(--el-color-danger);
}

.status-dot.online {
	background-color: var(--el-color-success);
}

.status-dot.offline {
	background-color: var(--el-color-danger);
}

.file-upload-section {
	display: flex;
	flex-direction: column;
	gap: 8px;
	flex-shrink: 0;
}

.file-analyzer-container {
	display: flex;
	align-items: flex-start;
	gap: 1rem;
}

.generate-workflow-right {
	flex-shrink: 0;
	display: flex;
	align-items: center;
}

.generate-workflow-btn-right {
	min-width: 180px;
	height: 40px;
	font-size: 14px;
	font-weight: 600;
	background: var(--el-color-primary);
	border: none;
	transition: all 0.3s ease;
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
	@apply rounded-xl;
}

.generate-workflow-btn-right:hover {
	transform: translateY(-2px);
	box-shadow: 0 8px 25px rgba(0, 0, 0, 0.15);
}

.generate-workflow-btn-right:disabled {
	opacity: 0.6;
	transform: none;
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.file-upload .el-button {
	color: var(--el-text-color-secondary);
	border: none;
	background: none;
	padding: 0;
}

.file-upload .el-button:hover {
	color: var(--el-color-primary);
}

.uploaded-file-display {
	margin-top: 4px;
}

.file-info {
	display: flex;
	align-items: center;
	gap: 8px;
	padding: 8px 12px;
	background: var(--el-color-info-light-9);
	border: 1px solid var(--el-color-info-light-5);
	font-size: 12px;
	color: var(--el-color-info);
	@apply rounded-xl;
}

.file-icon {
	color: var(--el-color-info);
}

.file-name {
	flex: 1;
	font-weight: 500;
}

.file-type-badge {
	font-size: 10px;
	padding: 2px 6px;
	background: var(--el-color-primary);
	color: var(--el-color-white);
	font-weight: 600;
	margin-left: 8px;
	@apply rounded-xl;
}

.file-type-badge.word {
	background: var(--el-color-primary-light-3);
}

.file-icon.pdf-icon {
	color: var(--el-color-danger);
}

.file-icon.word-icon {
	color: var(--el-color-primary);
}

.file-icon.image-icon {
	color: var(--el-color-success);
}

.remove-file-btn {
	color: var(--el-text-color-secondary);
	padding: 2px;
	min-height: auto;
}

.remove-file-btn:hover {
	color: var(--el-color-danger);
}

.file-preview {
	margin-top: 8px;
}

.image-preview {
	overflow: hidden;
	border: 1px solid var(--el-border-color-light);
	@apply rounded-xl;
}

.preview-image {
	width: 100%;
	max-width: 200px;
	max-height: 150px;
	object-fit: cover;
	display: block;
}

.document-preview {
	padding: 12px;
	border: 1px solid var(--el-border-color-light);
	background: var(--el-fill-color-blank);
	@apply rounded-xl;
}

.document-preview.pdf-preview {
	border-color: var(--el-color-danger-light-7);
	background: var(--el-color-danger-light-9);
}

.document-preview.word-preview {
	border-color: var(--el-color-primary-light-7);
	background: var(--el-color-primary-light-9);
}

.preview-header {
	display: flex;
	align-items: center;
	gap: 8px;
	margin-bottom: 8px;
}

.preview-icon {
	font-size: 24px;
	color: var(--el-text-color-secondary);
}

.pdf-preview .preview-icon {
	color: var(--el-color-danger);
}

.word-preview .preview-icon {
	color: var(--el-color-primary);
}

.preview-info {
	display: flex;
	flex-direction: column;
	gap: 2px;
}

.preview-title {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-regular);
}

.preview-subtitle {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.preview-description {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	font-style: italic;
}

.text-input-section {
	display: flex;
	flex-direction: column;
}

.input-with-button {
	display: flex;
	align-items: stretch;
}

.input-container {
	flex: 1;
	position: relative;
}

.input-bottom-actions {
	position: absolute;
	bottom: 8px;
	left: 12px;
	z-index: 10;
}

.input-bottom-actions .ai-model-selector-bottom {
	display: flex;
	align-items: center;
	gap: 0.5rem;
}

.input-bottom-actions .model-select {
	width: 140px !important;
}

.input-bottom-actions .model-select .el-input__inner {
	font-size: 0.75rem;
	border: none !important;
	background: transparent !important;
	padding: 2px 4px;
	box-shadow: none !important;
	color: var(--el-text-color-secondary);
}

.input-bottom-actions .model-select .el-input__inner:focus {
	border: none !important;
	box-shadow: none !important;
}

.input-right-actions {
	position: absolute;
	bottom: 8px;
	right: 12px;
	z-index: 10;
	display: flex;
	align-items: center;
	gap: 0.5rem;
}

.input-right-actions .send-button {
	width: 32px;
	height: 32px;
	min-width: 32px;
	padding: 0;
	border-radius: 50%;
	background: var(--el-color-primary);
	border: none;
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
	transition: all 0.2s ease;
}

.input-right-actions .send-button:hover {
	transform: translateY(-1px);
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
}

.input-right-actions .send-button:disabled {
	background: var(--el-border-color);
	box-shadow: none;
	transform: none;
}

.input-right-actions .send-button .el-icon {
	font-size: 14px;
	color: var(--el-color-white);
}

.input-right-actions .send-button .send-icon {
	width: 14px;
	height: 14px;
	color: var(--el-color-white);
	transform: rotate(-45deg);
}

.chat-input {
	flex: 1;
}

.chat-input .el-textarea__inner {
	resize: none;
	line-height: 1.5;
	min-height: 70px !important;
	height: 70px !important;
	border: 1px solid var(--el-border-color);
	padding: 12px 80px 12px 16px;
	font-size: 14px;
	transition: all 0.2s ease;
	@apply rounded-xl;
}

.chat-input .el-textarea__inner:focus {
	border-color: var(--el-color-primary);
	box-shadow: 0 0 0 3px rgba(0, 0, 0, 0.05);
}

.chat-history {
	margin-top: -85px;
	width: 320px;
	border-left: 1px solid var(--el-border-color-light);
	display: flex;
	flex-direction: column;
	transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
	overflow: hidden;
}

.chat-history.collapsed {
	width: 60px;
	box-shadow: -1px 0 4px rgba(0, 0, 0, 0.02);
	@apply rounded-xl;
}

.history-header {
	padding: 1.25rem;
	border-bottom: 1px solid var(--el-border-color-lighter);
	backdrop-filter: blur(10px);
	position: relative;
}

.header-content {
	display: flex;
	justify-content: space-between;
	align-items: center;
	gap: 0.75rem;
	margin-bottom: 1rem;
}

.header-title-section {
	display: flex;
	flex-direction: column;
	gap: 0.375rem;
}

.header-title-section h4 {
	margin: 0;
	color: var(--el-text-color-primary);
	font-size: 15px;
	font-weight: 700;
	letter-spacing: -0.025em;
}

.history-count {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	font-weight: 500;
	background: var(--el-fill-color-lighter);
	padding: 2px 8px;
	display: inline-block;
	@apply rounded-xl;
}

.header-actions {
	display: flex;
	align-items: center;
	gap: 0.5rem;
}

.new-chat-btn {
	font-size: 12px;
	padding: 8px 14px;
	background: var(--el-color-primary);
	border: none;
	color: var(--el-color-white);
	transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
	box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
	font-weight: 600;
	@apply rounded-xl;
}

.new-chat-btn:hover {
	transform: translateY(-2px);
	box-shadow: 0 6px 16px rgba(0, 0, 0, 0.2);
	background: var(--el-color-primary-light-3);
}

.menu-btn,
.collapse-btn {
	padding: 6px;
	min-width: 32px;
	height: 32px;
	transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
	border: 1px solid transparent;
	@apply rounded-xl;
}

.menu-btn:hover,
.collapse-btn:hover {
	background: var(--el-fill-color-lighter);
	border-color: var(--el-border-color-light);
	transform: scale(1.05);
}

.history-search {
	margin-top: 1rem;
}

.history-search .el-input {
	@apply rounded-xl;
}

.history-search .el-input__wrapper {
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-light);
	transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
	box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
}

.history-search .el-input__wrapper:hover {
	border-color: var(--el-border-color);
	box-shadow: 0 2px 6px rgba(0, 0, 0, 0.08);
}

.history-search .el-input__wrapper.is-focus {
	border-color: var(--el-color-primary);
	box-shadow: 0 0 0 3px rgba(0, 0, 0, 0.05);
}

.history-list {
	flex: 1;
	overflow-y: auto;
	padding: 1rem 0.75rem;
	height: 0; /* 强制使用flex-grow填充剩余空间 */
}

.section-header {
	display: flex;
	align-items: center;
	gap: 0.5rem;
	padding: 0.75rem 1rem;
	margin-bottom: 0.75rem;
	font-size: 11px;
	font-weight: 700;
	color: var(--el-text-color-secondary);
	text-transform: uppercase;
	letter-spacing: 0.8px;
	border-bottom: 1px solid var(--el-border-color-lighter);
	background: var(--el-fill-color-blank);
	position: relative;
}

.section-header::before {
	content: '';
	position: absolute;
	left: 0;
	top: 0;
	bottom: 0;
	width: 3px;
	background: var(--el-color-primary);
	@apply rounded-xl;
}

.pinned-section,
.recent-section {
	margin-bottom: 1.5rem;
}

.history-item {
	display: flex;
	align-items: center;
	padding: 1rem;
	cursor: pointer;
	transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
	margin-bottom: 0.75rem;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-lighter);
	position: relative;
	box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
	@apply rounded-xl;
}

.history-item::before {
	content: '';
	position: absolute;
	left: 0;
	top: 50%;
	transform: translateY(-50%);
	width: 0;
	height: 60%;
	background: var(--el-color-primary);
	transition: width 0.3s cubic-bezier(0.4, 0, 0.2, 1);
	@apply rounded-xl;
}

.history-item:hover {
	background: var(--el-fill-color-blank);
	border-color: var(--el-border-color-light);
	transform: translateY(-2px);
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
}

.history-item:hover::before {
	width: 4px;
}

.history-item.active {
	background: var(--el-color-primary-light-9);
	border-color: var(--el-color-primary);
	box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);
}

.history-item.active::before {
	width: 4px;
	background: var(--el-color-primary);
}

.history-item.pinned {
	background: var(--el-color-warning-light-9);
	border-color: var(--el-color-warning-light-5);
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.history-item.pinned::before {
	background: var(--el-color-warning);
	width: 4px;
}

.history-item.pinned:hover {
	background: var(--el-color-warning-light-8);
	border-color: var(--el-color-warning);
	box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12);
}

.item-content {
	flex: 1;
	min-width: 0;
}

.history-title {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-primary);
	margin-bottom: 0.375rem;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
	line-height: 1.4;
	letter-spacing: -0.01em;
	display: flex;
	align-items: center;
	gap: 0.5rem;
}

.attachment-icon {
	color: var(--el-text-color-secondary);
	flex-shrink: 0;
	font-size: 14px;
	opacity: 0.8;
	transition: all 0.2s ease;
}

.history-item:hover .attachment-icon {
	color: var(--el-color-primary);
	opacity: 1;
}

.history-meta {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 0.75rem;
}

.history-time {
	font-size: 11px;
	color: var(--el-text-color-secondary);
	font-weight: 500;
	background: var(--el-fill-color-blank);
	padding: 2px 6px;
	@apply rounded-xl;
}

.message-count {
	font-size: 10px;
	color: var(--el-text-color-secondary);
	background: var(--el-fill-color-lighter);
	padding: 3px 8px;
	font-weight: 600;
	border: 1px solid var(--el-border-color-light);
	@apply rounded-xl;
}

.item-actions {
	display: flex;
	align-items: center;
	gap: 0.375rem;
	opacity: 0;
	transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
	transform: translateX(8px);
}

.history-item:hover .item-actions {
	opacity: 1;
	transform: translateX(0);
}

.pin-icon {
	color: var(--el-color-warning);
	font-size: 14px;
	filter: drop-shadow(0 1px 2px rgba(0, 0, 0, 0.15));
}

.action-btn {
	padding: 4px;
	min-width: 24px;
	height: 24px;
	color: var(--el-text-color-secondary);
	transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
	border: 1px solid transparent;
	@apply rounded-xl;
}

.action-btn:hover {
	background: var(--el-fill-color-lighter);
	border-color: var(--el-border-color-light);
	color: var(--el-text-color-primary);
	transform: scale(1.1);
}

.empty-history,
.no-results {
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
	padding: 3rem 1.5rem;
	text-align: center;
}

.empty-icon {
	width: 56px;
	height: 56px;
	border-radius: 50%;
	background: var(--el-fill-color-lighter);
	display: flex;
	align-items: center;
	justify-content: center;
	margin-bottom: 1.5rem;
	border: 1px solid var(--el-border-color-light);
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
}

.empty-icon .el-icon {
	font-size: 28px;
	color: var(--el-text-color-placeholder);
}

.empty-title {
	margin: 0 0 0.75rem 0;
	font-size: 16px;
	font-weight: 700;
	color: var(--el-text-color-primary);
	letter-spacing: -0.025em;
}

.empty-subtitle {
	margin: 0 0 1.5rem 0;
	font-size: 14px;
	color: var(--el-text-color-secondary);
	line-height: 1.5;
	max-width: 200px;
}

.start-chat-btn {
	background: var(--el-color-primary);
	border: none;
	color: var(--el-color-white);
	padding: 10px 20px;
	font-weight: 600;
	font-size: 14px;
	transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
	@apply rounded-xl;
}

.start-chat-btn:hover {
	transform: translateY(-2px);
	box-shadow: 0 6px 20px rgba(0, 0, 0, 0.2);
	background: var(--el-color-primary-light-3);
}

.dialog-footer {
	display: flex;
	justify-content: flex-end;
	gap: 0.5rem;
}

.mr-1 {
	margin-right: 4px;
}

/* Chat history area scrollbar styles */
.history-list::-webkit-scrollbar {
	width: 8px;
}

.history-list::-webkit-scrollbar-track {
	background: var(--el-fill-color-blank);
	margin: 4px 0;
	@apply rounded-xl;
}

.history-list::-webkit-scrollbar-thumb {
	background: var(--el-border-color);
	transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
	border: 1px solid var(--el-border-color-light);
	@apply rounded-xl;
}

.history-list::-webkit-scrollbar-thumb:hover {
	background: var(--el-border-color-dark);
	border-color: var(--el-border-color);
}

/* Responsive Design */
@media (max-width: 768px) {
	.assistant-container {
		flex-direction: column;
	}

	.chat-history {
		width: 100%;
		border: 1px solid var(--el-border-color-light);
		border-bottom: none;
		max-height: 350px;
		box-shadow: 0 -4px 12px rgba(0, 0, 0, 0.05);
		@apply rounded-xl;
	}

	.chat-history.collapsed {
		width: 100%;
		height: 60px;
		max-height: 60px;
		@apply rounded-xl;
	}

	.additional-components {
		grid-template-columns: 1fr;
	}

	.user-message .message-content,
	.ai-message .message-content {
		max-width: 85%;
	}

	.history-item {
		padding: 0.75rem;
		margin-bottom: 0.5rem;
	}

	.section-header {
		padding: 0.5rem 0.75rem;
	}
}

/* Workflow Modification Styles */
.workflow-modification {
	display: flex;
	align-items: flex-start;
	gap: 0.75rem;
	margin-bottom: 1rem;
}

.workflow-modification .message-avatar {
	width: 32px;
	height: 32px;
	border-radius: 50%;
	background: var(--el-color-primary);
	display: flex;
	align-items: center;
	justify-content: center;
	color: var(--el-color-white);
	font-size: 14px;
	flex-shrink: 0;
}

.workflow-modification .message-content {
	flex: 1;
	background: var(--el-fill-color-blank);
	border: 1px solid var(--el-border-color-light);
	padding: 1rem;
	@apply rounded-xl;
}

.workflow-header h4 {
	margin: 0 0 1rem 0;
	color: var(--el-text-color-primary);
	font-size: 16px;
	font-weight: 600;
}

.workflow-info-card {
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-light);
	padding: 1rem;
	margin-bottom: 1rem;
	@apply rounded-xl;
}

.workflow-details h5 {
	margin: 0 0 0.5rem 0;
	color: var(--el-text-color-primary);
	font-size: 18px;
	font-weight: 600;
}

.workflow-details p {
	margin: 0 0 0.75rem 0;
	color: var(--el-text-color-secondary);
	line-height: 1.5;
}

.workflow-meta {
	display: flex;
	gap: 1rem;
	align-items: center;
}

.workflow-meta .status {
	padding: 0.25rem 0.75rem;
	font-size: 12px;
	font-weight: 500;
	background: var(--el-fill-color-lighter);
	color: var(--el-text-color-secondary);
	@apply rounded-xl;
}

.workflow-meta .status.active {
	background: var(--el-color-success-light-9);
	color: var(--el-color-success-dark-2);
}

.stage-count {
	font-size: 12px;
	color: var(--el-text-color-secondary);
	background: var(--el-fill-color-lighter);
	padding: 0.25rem 0.75rem;
	@apply rounded-xl;
}

.stages-list h6 {
	margin: 0 0 1rem 0;
	color: var(--el-text-color-primary);
	font-size: 14px;
	font-weight: 600;
}

.stages-container {
	display: grid;
	grid-template-columns: repeat(3, 1fr);
	gap: 1rem;
	margin-bottom: 1rem;
	width: 100%;
}

.stage-card.editable {
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-light);
	border-radius: 8px;
	padding: 1rem;
	transition: all 0.2s ease;
	min-height: 200px;
	display: flex;
	flex-direction: column;
}

.stage-card.editable:hover {
	border-color: var(--el-color-primary);
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.stage-header {
	display: flex;
	align-items: center;
	gap: 0.5rem;
	margin-bottom: 0.5rem;
}

.stage-number {
	width: 24px;
	height: 24px;
	border-radius: 50%;
	background: var(--el-color-primary);
	color: var(--el-color-white);
	display: flex;
	align-items: center;
	justify-content: center;
	font-size: 12px;
	font-weight: 600;
	flex-shrink: 0;
}

.stage-name-input {
	flex: 1;
}

.stage-description-input {
	margin-bottom: 0.75rem;
	flex: 1;
}

.stage-details {
	display: grid;
	grid-template-columns: 1fr 1fr;
	gap: 0.75rem;
	margin-top: auto;
}

.stage-field {
	display: flex;
	flex-direction: column;
	gap: 0.5rem;
}

.stage-field label {
	font-size: 12px;
	font-weight: 500;
	color: var(--el-text-color-regular);
}

.save-section {
	display: flex;
	justify-content: flex-end;
	gap: 0.5rem;
	padding-top: 1rem;
	border-top: 1px solid var(--el-border-color-light);
}

/* Workflow Selection Styles */
.workflow-selection {
	display: flex;
	align-items: flex-start;
	gap: 0.75rem;
	margin-bottom: 1rem;
}

.workflow-selection .message-avatar {
	width: 32px;
	height: 32px;
	border-radius: 50%;
	background: var(--el-color-primary);
	display: flex;
	align-items: center;
	justify-content: center;
	color: var(--el-color-white);
	font-size: 14px;
	flex-shrink: 0;
}

.workflow-selection .message-content {
	flex: 1;
	background: var(--el-fill-color-blank);
	border: 1px solid var(--el-border-color-light);
	padding: 1rem;
	@apply rounded-xl;
}

.selection-header h4 {
	margin: 0 0 1rem 0;
	color: var(--el-text-color-primary);
	font-size: 16px;
	font-weight: 600;
}

.workflows-list {
	display: flex;
	flex-direction: column;
	gap: 0.75rem;
}

.workflow-option {
	display: flex;
	align-items: center;
	justify-content: space-between;
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-light);
	padding: 1rem;
	cursor: pointer;
	transition: all 0.2s ease;
	@apply rounded-xl;
}

.workflow-option:hover {
	border-color: var(--el-color-primary);
	box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.workflow-option-content {
	flex: 1;
}

.workflow-option-content h5 {
	margin: 0 0 0.5rem 0;
	color: var(--el-text-color-primary);
	font-size: 16px;
	font-weight: 600;
}

.workflow-option-content p {
	margin: 0 0 0.5rem 0;
	color: var(--el-text-color-secondary);
	line-height: 1.4;
}

.workflow-option-meta {
	display: flex;
	gap: 1rem;
	align-items: center;
}

.created-date {
	font-size: 12px;
	color: var(--el-text-color-placeholder);
}

.select-icon {
	color: var(--el-color-primary);
	font-size: 18px;
}

@media (max-width: 768px) {
	.stages-container {
		grid-template-columns: 1fr;
	}

	.stages-grid {
		grid-template-columns: 1fr;
	}

	.stage-details {
		grid-template-columns: 1fr;
	}

	.workflow-option-meta {
		flex-direction: column;
		align-items: flex-start;
		gap: 0.25rem;
	}
}

@media (max-width: 1200px) and (min-width: 769px) {
	.stages-container {
		grid-template-columns: repeat(2, 1fr);
	}

	.stages-grid {
		grid-template-columns: repeat(2, 1fr);
	}
}

/* Checklists and Questionnaires Styles */
.additional-components {
	margin-top: 2rem;
	padding-top: 2rem;
	border-top: 1px solid var(--el-border-color-light);
}

.component-section {
	margin-bottom: 2rem;
}

.section-header {
	display: flex;
	align-items: center;
	justify-content: space-between;
	margin-bottom: 1rem;
}

.component-section h6 {
	margin: 0;
	color: var(--el-text-color-primary);
	font-size: 18px;
	font-weight: 600;
	display: flex;
	align-items: center;
	gap: 0.5rem;
}

.component-section h6::before {
	content: '';
	width: 4px;
	height: 20px;
	background: var(--el-color-primary);
	@apply rounded-xl;
}

.collapse-toggle {
	color: var(--el-text-color-secondary);
	font-size: 14px;
	padding: 4px 8px;
	transition: all 0.2s ease;
}

.collapse-toggle:hover {
	color: var(--el-color-primary);
	background-color: var(--el-fill-color-lighter);
}

/* Checklists Grid */
.checklists-grid {
	display: grid;
	grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
	gap: 1rem;
	margin-bottom: 1rem;
}

.checklist-card {
	background: var(--el-fill-color-blank);
	border: 1px solid var(--el-border-color-light);
	padding: 1rem;
	transition: all 0.2s ease;
	@apply rounded-xl;
}

.checklist-card:hover {
	border-color: var(--el-color-primary);
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.checklist-header {
	display: flex;
	flex-direction: column;
	gap: 0.75rem;
	margin-bottom: 0.75rem;
}

.checklist-name-section {
	display: flex;
	align-items: center;
	gap: 0.5rem;
}

.name-label {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-regular);
	min-width: fit-content;
	white-space: nowrap;
}

.checklist-name-input {
	font-size: 16px;
	font-weight: 600;
}

.checklist-name-input .el-input__wrapper {
	background: transparent;
	border: 1px dashed var(--el-border-color);
	transition: all 0.2s ease;
	@apply rounded-xl;
}

.checklist-name-input .el-input__wrapper:hover {
	border-color: var(--el-color-primary);
	background: var(--el-fill-color-blank);
}

.checklist-name-input .el-input__wrapper.is-focus {
	border-color: var(--el-color-primary);
	border-style: solid;
	background: var(--el-bg-color);
	box-shadow: 0 0 0 2px rgba(0, 0, 0, 0.1);
}

.checklist-title-row {
	display: flex;
	align-items: center;
	justify-content: space-between;
}

.checklist-title {
	display: flex;
	align-items: center;
	gap: 0.5rem;
}

.stage-icon {
	color: var(--el-color-primary);
	font-size: 16px;
}

.stage-label {
	font-size: 14px;
	font-weight: 500;
	color: var(--el-text-color-secondary);
	margin-right: 0.25rem;
}

.stage-name {
	font-weight: 700;
	color: var(--el-color-white);
	font-size: 16px;
	background: var(--el-color-primary);
	padding: 4px 12px;
	@apply rounded-xl;
}

.checklist-type {
	color: var(--el-text-color-secondary);
	font-size: 14px;
	font-weight: 500;
}

.card-actions {
	display: flex;
	align-items: center;
	gap: 0.5rem;
}

.card-actions .el-button--danger.is-circle {
	background-color: var(--el-color-info) !important;
	border-color: var(--el-color-info) !important;
}

.card-actions .el-button--danger.is-circle:hover {
	background-color: var(--el-color-info) !important;
	border-color: var(--el-color-info) !important;
}

.expand-btn {
	color: var(--el-text-color-secondary);
	padding: 4px;
	transition: all 0.2s ease;
}

.expand-btn:hover {
	color: var(--el-color-primary);
	background-color: var(--el-fill-color-lighter);
}

.tasks-list {
	display: flex;
	flex-direction: column;
	gap: 0.75rem;
}

.task-item {
	background: var(--el-bg-color);
	border: 1px solid var(--el-border-color-light);
	padding: 0.75rem;
	@apply rounded-xl;
}

.task-header {
	display: flex;
	align-items: flex-start;
	gap: 0.5rem;
	margin-bottom: 0.5rem;
}

.task-title {
	flex: 1;
	font-size: 14px;
	font-weight: 500;
	color: var(--el-text-color-regular);
	display: flex;
	align-items: center;
	gap: 0.5rem;
}

.task-title .el-tag[type='warning'] {
	background-color: var(--el-color-warning) !important;
}

.task-description {
	margin: 0 0 0.5rem 0;
	font-size: 13px;
	color: var(--el-text-color-secondary);
	line-height: 1.4;
	padding-left: 1.5rem;
}

/* Questionnaires Grid */
.questionnaires-grid {
	display: grid;
	grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
	gap: 1rem;
	margin-bottom: 1rem;
}

.questionnaire-card {
	background: var(--el-color-warning-light-9);
	border: 1px solid var(--el-color-warning-light-5);
	padding: 1rem;
	transition: all 0.2s ease;
	@apply rounded-xl;
}

.questionnaire-card:hover {
	border-color: var(--el-color-warning);
	box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.questionnaire-header {
	display: flex;
	flex-direction: column;
	gap: 0.75rem;
	margin-bottom: 0.75rem;
}

.questionnaire-name-section {
	display: flex;
	align-items: center;
	gap: 0.5rem;
}

.questionnaire-name-input {
	font-size: 16px;
	font-weight: 600;
}

.questionnaire-name-input .el-input__wrapper {
	background: transparent;
	border: 1px dashed var(--el-border-color);
	transition: all 0.2s ease;
	@apply rounded-xl;
}

.questionnaire-name-input .el-input__wrapper:hover {
	border-color: var(--el-color-warning);
	background: var(--el-color-warning-light-9);
}

.questionnaire-name-input .el-input__wrapper.is-focus {
	border-color: var(--el-color-warning);
	border-style: solid;
	background: var(--el-bg-color);
	box-shadow: 0 0 0 2px rgba(0, 0, 0, 0.1);
}

.questionnaire-title-row {
	display: flex;
	align-items: center;
	justify-content: space-between;
}

.questionnaire-title {
	display: flex;
	align-items: center;
	gap: 0.5rem;
}

.questionnaire-type {
	color: var(--el-text-color-secondary);
	font-size: 14px;
	font-weight: 500;
}

.questions-list {
	display: flex;
	flex-direction: column;
	gap: 0.75rem;
}

.question-item {
	background: var(--el-bg-color);
	border: 1px solid var(--el-color-warning-light-5);
	padding: 0.75rem;
	@apply rounded-xl;
}

.question-header {
	display: flex;
	align-items: flex-start;
	justify-content: space-between;
	gap: 0.5rem;
	margin-bottom: 0.5rem;
}

.question-text {
	flex: 1;
	font-size: 14px;
	font-weight: 500;
	color: var(--el-text-color-regular);
	display: flex;
	align-items: center;
	gap: 0.5rem;
}

.question-options {
	display: flex;
	flex-wrap: wrap;
	gap: 0.25rem;
	margin-top: 0.5rem;
}

.option-tag {
	background: var(--el-fill-color-lighter) !important;
	border-color: var(--el-border-color) !important;
	color: var(--el-text-color-secondary) !important;
}

/* Responsive Design for Checklists and Questionnaires */
@media (max-width: 768px) {
	.checklists-grid,
	.questionnaires-grid {
		grid-template-columns: 1fr;
	}

	.task-header,
	.question-header {
		flex-direction: column;
		align-items: flex-start;
		gap: 0.5rem;
	}

	.task-description {
		padding-left: 0;
	}
}

@media (max-width: 1200px) and (min-width: 769px) {
	.checklists-grid,
	.questionnaires-grid {
		grid-template-columns: repeat(2, 1fr);
	}
}
</style>
