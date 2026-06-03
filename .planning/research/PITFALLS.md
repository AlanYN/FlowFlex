# Domain Pitfalls

**Domain:** Workflow/BPM component enhancements (OW-621)
**Researched:** 2026-06-02
**Codebase:** FlowFlex - Vue 3 + .NET 8 + PostgreSQL + SqlSugar ORM

---

## Critical Pitfalls

Mistakes that cause data corruption, silent failures, or require rewrites.

---

### Pitfall 1: Workflow Duplicate Copies FK References, Not Component Entities

**Applies to:** #3 Workflow Duplicate 深拷贝 Stage Components

**What goes wrong:** The current `DuplicateAsync` in `WorkflowService.cs` (lines 885-906) copies `ChecklistId` and `QuestionnaireId` as plain scalar values from the original Stage. Both the original and duplicated workflow end up pointing to the same Checklist/Questionnaire DB rows. Editing the component in the duplicate silently modifies the original workflow's component.

**Why it happens:** Copying struct-like fields by assignment looks identical to copying entity references. The distinction between "same ID" and "new entity" is invisible at the assignment level.

**Consequences:**
- Editing a Checklist inside the duplicate mutates the original workflow's Checklist
- Deleting the Checklist in the duplicate orphans the original Stage's reference
- No exception is thrown; corruption is discovered long after the fact
- `Stage.ComponentsJson` stores component metadata including these IDs -- copying it verbatim embeds the old IDs into the duplicate's JSON blob

**Prevention:**
- Deep copy means: for each Stage in the duplicate, call `ChecklistService` and `QuestionnaireService` create paths to produce brand-new entities, then assign the new IDs to the duplicated Stage
- After creating new entities, regenerate `ComponentsJson` with the new IDs rather than copying the original JSON string
- Confirm with product whether "deep copy" is the requirement before implementing (PROJECT.md notes this decision is still pending as of 2026-06-02)

**Detection:** Write a test: duplicate a workflow, modify the checklist inside the copy, assert the original checklist is unchanged.

---

### Pitfall 2: Cascade Delete Leaves ComponentsJson Stale (Orphaned IDs)

**Applies to:** #2 已删除 Component 自动从 Stage 中移除

**What goes wrong:** `Stage.ComponentsJson` is a JSONB column storing `StageComponent` objects that contain `ChecklistIds` and `QuestionnaireIds` arrays. When a Checklist or Questionnaire is soft-deleted (`IsValid = false`), the SqlSugar global `IValidFilter` hides the deleted row from future queries -- but the IDs embedded in `ComponentsJson` are never cleaned up. The Stage still references the deleted component.

**Why it happens:** SqlSugar's global filter works at the row-select level. A JSON array inside a column is not a row; the ORM cannot filter it automatically.

**Consequences:**
- Stage component UI renders empty or broken slots for deleted components
- `StageService.GetComponentsAsync` (line 1571) already detects name-count mismatches -- this is a live symptom of this exact problem in the current codebase
- Race condition: load Stage -> delete Checklist -> Stage cache still has stale IDs; cache TTL is 10 minutes (`STAGE_CACHE_DURATION`)

**Prevention:**
- When soft-deleting a Checklist or Questionnaire, query all Stages where the scalar FK column (`ChecklistId` / `QuestionnaireId`) matches, AND query Stages whose `ComponentsJson` contains the ID (PostgreSQL `@>` JSONB operator)
- Wrap the soft-delete and the Stage JSON update in a single DB transaction
- After updating Stage rows, invalidate their cache keys (`STAGE_CACHE_PREFIX`)

**Detection:** Warning sign already present in logs: `GetComponentsAsync` name-count mismatch messages. Component slot appears in UI but loads empty.

---

### Pitfall 3: JSONB answer_json Schema Evolution Breaks Existing Records

**Applies to:** #7 文件上传记录 UploadedBy + UploadDate

**What goes wrong:** `QuestionnaireAnswer.Answer` is a `JToken` JSONB column. Adding `UploadedBy` and `UploadDate` fields to the upload answer structure means all existing records lack these fields. Code that accesses `answer["UploadedBy"]` returns `null` on old records. Code that casts without null-checking, or a DTO with non-nullable `UploadedBy`, throws at runtime or silently deserializes to an empty default.

**Why it happens:** JSONB is schema-free. The ORM stores whatever the application serializes; old records are never migrated automatically.

**Consequences:**
- Null reference exceptions or blank display when viewing upload history for pre-feature file uploads
- `QuestionnaireAnswerParser.cs` (in `ChangeLog/`) parses `answer_json` for change log display -- if it assumes the new fields exist, it produces broken log entries for historical records

**Prevention:**
- Treat all new JSONB sub-fields as optional with explicit null-coalescing in both C# (`answer["UploadedBy"]?.ToString() ?? "Unknown"`) and the Vue template (`row.uploadedBy ?? ''`)
- Do not add `[Required]` or non-nullable C# types to DTO properties backed by JSONB unless you also run a backfill migration
- Update `QuestionnaireAnswerParser` to handle the absence of the new fields gracefully

**Detection:** Seed a test `QuestionnaireAnswer` row without the new fields and run the display path against it before merging.

---

### Pitfall 4: StageSave Log Removal via Hard Delete Is Irreversible

**Applies to:** #8 移除 StageSave 类型的 Change Log

**What goes wrong:** `OperationType` is stored as the enum `.ToString()` value in `ff_operation_change_log.operation_type`. If removal is implemented as a `DELETE` on existing rows, historical audit records are permanently destroyed. If implemented as a query-time exclusion in `LogAggregationService.GetAggregatedLogsAsync`, the asymmetric filter is invisible to future engineers and easy to accidentally remove.

**Why it happens:** The requirement says "remove StageSave logs" -- engineers often interpret this as deleting records rather than hiding them.

**Consequences:**
- Hard deletion removes audit history that may be needed for support or compliance review
- A silent query-layer exclusion without documentation gets removed by a future engineer who does not know it was intentional

**Prevention:**
- Prefer filtering at the query/display layer (add `StageSave` to an exclusion list in `GetAggregatedLogsAsync`, document it with a comment explaining the intent)
- If DB deletion is required, run it as a one-time migration with an explicit comment -- not application code that runs on every request

**Detection:** Before any removal, run `SELECT COUNT(*) FROM ff_operation_change_log WHERE operation_type = 'StageSave'` in staging to understand blast radius.

---

### Pitfall 5: UpdatedBy Propagation Triggers Full Workflow Validation

**Applies to:** #6 更新 Stage/Component 时同步更新 Workflow 的 UpdatedBy

**What goes wrong:** `StageService` and `WorkflowService` are separate scoped services. Making `StageService` call `WorkflowService.UpdateAsync` to propagate audit fields would trigger the full update path: name-uniqueness validation, team validation (`ValidateTeamSelectionsAsync`), change logging, and cache invalidation -- all for a change that only needs to update three audit columns on the workflow row.

**Why it happens:** `WorkflowService.UpdateAsync` owns all workflow update logic in a single method. There is no lightweight "touch audit fields only" path.

**Consequences:**
- Over-logging: every Stage save would emit a Workflow update change log entry
- Performance hit from unnecessary IDM team validation on every Stage save
- IDM API unavailability could block audit propagation and roll back the Stage save

**Prevention:**
- Add a dedicated `TouchWorkflowAuditAsync(long workflowId)` method on `IWorkflowRepository` that only updates `modify_by`, `modify_date`, `modify_user_id` -- no validation, no change log, no full update path
- Call this from `StageService` after a successful stage update, wrapped in `try/catch` fire-and-forget so audit propagation failure cannot roll back the stage save
- Invalidate the workflow cache key inside `TouchWorkflowAuditAsync`

**Detection:** After implementing, verify workflow `modify_date` changes when a Stage is saved, and verify no extra Workflow-level change log entry is emitted.

---

### Pitfall 6: Comment Count Inflated by Wrong Soft-Delete Column

**Applies to:** #12 Checklist comment 计数只统计 Notes

**What goes wrong:** `ChecklistTaskNoteService.GetNotesCountByTaskIdsAsync` calls `_noteRepository.CountByTaskIdAsync(taskId)` without filtering by `NoteType`. The `ChecklistTaskNote` entity uses `IsDeleted` for soft delete -- not `IsValid`, which is the project-wide convention enforced by SqlSugar's global `IValidFilter`. If `CountByTaskIdAsync` relies on the global filter, it will count deleted notes because their `IsDeleted = true` is never read by the `IsValid` filter.

**Secondary issue:** `NoteType` is not filtered in the count query. If any code path writes `ChecklistTaskNote` records with a system-type `NoteType`, those entries inflate the visible badge count.

**Why it happens:** `ChecklistTaskNote` was implemented independently and chose `IsDeleted` instead of the project-wide `IsValid` soft delete convention.

**Consequences:**
- Badge count shows a higher number than actual user-visible Notes
- Deleting a note may not decrement the count if the wrong column is filtered

**Prevention:**
- `CountByTaskIdAsync` must filter explicitly: `WHERE is_deleted = false` -- do not rely on the global SqlSugar `IValidFilter` for this entity
- Filter to user-visible `NoteType` values only (e.g., `NoteType = 'General'`)
- Document the `IsDeleted` vs `IsValid` deviation in the repository to prevent future confusion

**Detection:** Insert a note, delete it (set `IsDeleted = true`), verify the count decrements. Insert a note with a non-General NoteType, verify it does not appear in the count.

---

## Moderate Pitfalls

---

### Pitfall 7: Permission Debugging -- Token Staleness Looks Like a Code Bug

**Applies to:** #11 User Group 权限排查与修复

**What goes wrong:** The permission chain is `OnboardingPermissionService` -> `PermissionService` -> `WorkflowPermissionService` -> `PermissionHelpers.GetUserTeamIds()` -> JWT claims / IDM API. If a user is added to a UserGroup after their last login, the new group membership is absent from their JWT. The permission check correctly evaluates the token and produces a false negative -- the user appears to lack permission even though the configuration is correct.

`WorkflowPermissionService.CheckWorkflowPermission` logs the exact comparison at Debug level (lines 56-62): WorkflowId, ViewMode, ViewTeams, OperateTeams, UserTeams. This is the fastest diagnostic tool, but Debug logging is typically off in staging.

**Consequences:**
- Engineers trace C# code for a bug that is actually token staleness
- The fix (user re-authenticates) is invisible without knowing to check the token

**Prevention:**
- First diagnostic step: enable Debug logging for `WorkflowPermissionService` and `CasePermissionService`, reproduce the failure, read the log
- Compare `UserTeams` in the log against the user's actual group membership in the IDM admin panel
- Only proceed to code investigation if `UserTeams` correctly reflects the group assignment and permission still fails

**Detection:** The debug log from `WorkflowPermissionService` (lines 56-62) is definitive. If `UserTeams` does not include the expected group, it is a token/IDM issue, not a FlowFlex bug.

---

### Pitfall 8: Scroll-to-Top Has DOM Timing Race with Vue Re-render

**Applies to:** #4 问卷点击 Next 后自动滚动到顶部

**What goes wrong:** Calling `window.scrollTo(0, 0)` synchronously after incrementing `currentSectionIndex` runs before Vue flushes its DOM updates. The scroll fires against the previous section's rendered layout; Vue then renders the new section, and scroll position ends up incorrect.

**Why it happens:** Vue 3 batches DOM updates asynchronously. A reactive state change does not immediately produce new DOM nodes.

**Consequences:**
- Clicking Next appears not to scroll, or scrolls briefly then snaps to an unexpected position
- Only reproducible when questionnaire content is long enough that the Next button is below the fold

**Prevention:**
- Wrap scroll calls in `await nextTick()` after the state change
- If the questionnaire renders inside a scrollable container div (not `window`), scroll the container element -- `window.scrollTo` does nothing if a parent element has `overflow: hidden` or `overflow: auto`

**Detection:** Test on a questionnaire with at least 3 sections where section content exceeds viewport height.

---

### Pitfall 9: Log Text Change Breaks Frontend Tag Mapping

**Applies to:** #1 Checklist Done/Cancel 日志文案优化

**What goes wrong:** `ChecklistLogService` generates `operationDescription` strings in `BuildTaskCompletionDescription` and `BuildTaskUncompletionDescription`. If `ChangeLog.vue`'s `getTagType` function pattern-matches on the description string rather than on `operationType`, renaming the description text breaks the tag display.

**Consequences:**
- Renamed log entries show the wrong tag color or fall through to a default/unknown type
- Historical entries (stored with old text) render differently from new entries in the same table

**Prevention:**
- Only change the human-readable `operationDescription` and `operationTitle` values
- Do not change `OperationType` enum string values (`ChecklistTaskComplete`, `ChecklistTaskUncomplete`) -- these are stored in the DB and used for filtering
- Verify `ChangeLog.vue`'s `getTagType` references `row.operationType` (the enum value), not `row.operationDescription`

**Detection:** After the text change, verify that log entries still show the correct tag color and icon.

---

### Pitfall 10: Seconds-Level Date Display Exposes Timezone Bugs

**Applies to:** #1 日期显示到秒

**What goes wrong:** Log timestamps are stored as `DateTimeOffset.UtcNow`. If the frontend formats them with a new seconds-precision formatter that omits timezone conversion, the time appears wrong to users outside UTC (e.g., shows 3:00:45 AM when the user in UTC+8 expects 11:00:45 PM).

**Prevention:**
- Reuse the existing date formatting utility already used for `ModifyDate` and `CreateDate` -- do not introduce a new formatter
- Seconds precision makes timezone errors much more visible than date-only display, so verify the existing formatter handles timezone before adding seconds

**Detection:** Set the browser to a non-UTC timezone, trigger a task completion, verify the displayed timestamp matches the local clock.

---

### Pitfall 11: Collapsible Panel State Lost on Navigation

**Applies to:** #5 Stage Detail 区域可收缩

**What goes wrong:** Component-local `ref<boolean>` collapse state is destroyed on component unmount. Users who collapse panels to manage a complex Case must re-collapse on every navigation.

**Prevention:**
- Local state is acceptable for the initial implementation -- document the limitation
- If persistence is needed later, use `localStorage` keyed by `onboardingId:stageId:collapsed` -- simpler than a Pinia store and survives navigation without backend changes
- Do not add this to the Pinia case store unless collapse state is needed for cross-component coordination

---

## Minor Pitfalls

---

### Pitfall 12: "At Least One Cell" Validation -- Negation Logic Error

**Applies to:** #9 Short Answer Grid 必填校验改为填一格即可

**What goes wrong:** Changing from "all cells required" to "at least one cell filled" is logically simple but the double-negation form (`cells.every(c => !c.value)`) is easy to get wrong under future modification.

**Prevention:**
- Write the condition as the positive assertion: `const isValid = cells.some(c => c.value?.trim().length > 0)`
- Update both the frontend Vue component validation and the backend FluentValidation rule -- frontend-only validation can be bypassed via direct API calls

---

### Pitfall 13: Workflow Status Filter Inconsistency Between Pages

**Applies to:** #10 创建 Case 时 Workflow 状态与管理页面一致

**What goes wrong:** The Case creation dropdown and the Workflow management page may call different query methods with different filter combinations (`IsActive`, `Status`, `EndDate`). Expired or inactive workflows can appear in the dropdown if its filter is less strict.

**Prevention:**
- Identify the API endpoint used by each page and verify both apply identical filter conditions
- If a shared `GetActiveWorkflowsAsync` repository method exists, both pages should call it -- not construct independent filter expressions

---

## Phase-Specific Warnings

| Feature | Likely Pitfall | Mitigation |
|---------|---------------|------------|
| #3 Workflow Duplicate | FK IDs copied instead of new entities created | Confirm deep copy requirement; create new Checklist/Questionnaire entities |
| #2 Cascade delete | ComponentsJson not updated when entity soft-deleted | Transaction: soft-delete + JSON update + cache invalidation |
| #7 File upload metadata | Null fields on old JSONB records cause runtime errors | Null-coalesce everywhere; update QuestionnaireAnswerParser |
| #11 Permission fix | Token staleness diagnosed as code bug | Enable Debug logging in WorkflowPermissionService first |
| #8 StageSave log removal | Hard delete destroys audit history | Filter at query/display layer; document exclusion with a comment |
| #6 UpdatedBy propagation | Full WorkflowService.UpdateAsync triggers unnecessary validation | Add TouchWorkflowAuditAsync -- audit-only update, no business logic |
| #12 Comment count | IsDeleted vs IsValid inconsistency inflates count | CountByTaskIdAsync must filter on is_deleted explicitly |
| #4 Scroll to top | Scroll fires before Vue re-renders next section | Wrap in await nextTick() |
| #1 Log text change | Frontend getTagType breaks if matched on description strings | Only change description text, not OperationType enum value |
| #1 Date to seconds | New formatter skips timezone conversion | Reuse existing timezone-aware date formatter |

## Sources

Direct codebase reading (confidence: HIGH for all findings):
- `packages/flowFlex-backend/Application/Services/OW/WorkflowService.cs` -- DuplicateAsync lines 847-940
- `packages/flowFlex-backend/Application/Services/OW/StageService.cs` -- ComponentsJson handling lines 346-709, cache TTL line 78
- `packages/flowFlex-backend/Application/Services/OW/ChangeLog/ChecklistLogService.cs` -- BuildTaskCompletionDescription, BuildTaskUncompletionDescription
- `packages/flowFlex-backend/Application/Services/OW/ChangeLog/LogAggregationService.cs` -- GetAggregatedLogsAsync filter logic
- `packages/flowFlex-backend/Application/Services/OW/Permission/WorkflowPermissionService.cs` -- CheckWorkflowPermission debug logging lines 56-62
- `packages/flowFlex-backend/Application/Services/OW/OnboardingServices/OnboardingPermissionService.cs` -- permission chain
- `packages/flowFlex-backend/Application/Services/OW/ChecklistTaskNoteService.cs` -- GetNotesCountByTaskIdsAsync
- `packages/flowFlex-backend/Domain/Entities/OW/Stage.cs` -- ComponentsJson JSONB, ChecklistId/QuestionnaireId scalar FKs
- `packages/flowFlex-backend/Domain/Entities/OW/QuestionnaireAnswer.cs` -- Answer JToken JSONB
- `packages/flowFlex-backend/Domain/Entities/OW/ChecklistTaskNote.cs` -- IsDeleted (not IsValid) soft delete
- `packages/flowFlex-common/src/app/views/onboard/onboardingList/components/ChangeLog.vue` -- getTagType, processedChanges
