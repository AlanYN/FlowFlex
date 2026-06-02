# Requirements: OW-621 Workflow Component Enhancements

**Defined:** 2026-06-02
**Core Value:** Workflow 组件功能完善且交互流畅，用户操作日志准确、组件生命周期正确维护、权限配置生效。

## v1.1 Requirements

Requirements for milestone v1.1. Each maps to roadmap phases.

### Change Log & Audit

- [ ] **LOG-01**: Checklist Done 操作日志显示 "Completed the task" + 时间精确到秒 (MM/DD/YYYY HH:mm:ss)
- [ ] **LOG-02**: Checklist Cancel 操作日志显示 "Cancelled the task" + 时间精确到秒
- [ ] **LOG-03**: StageSave 类型的 Change Log 不再写入（移除 LogStageSaveAsync 调用）
- [ ] **LOG-04**: Checklist Task 的 comment 计数只统计 Notes 类型，不计入 Change Log 条目
- [ ] **LOG-05**: 更新 Stage 或 Component 时，所属 Workflow 的 UpdatedBy/UpdateDate 同步更新

### Component Lifecycle

- [ ] **COMP-01**: 删除 Checklist 时，自动清除关联 Stage 的 ChecklistId 引用和 ComponentsJson 中的对应条目
- [ ] **COMP-02**: 删除 Questionnaire 时，自动清除关联 Stage 的 QuestionnaireId 引用和 ComponentsJson 中的对应条目
- [ ] **COMP-03**: Duplicate Workflow 时，新 Stage 的 ComponentsJson、ViewPermissionMode、ViewTeams、OperateTeams 完整复制，并调用 SyncStageMappingsAsync

### Frontend UX

- [ ] **UX-01**: 问卷点击 Next 后页面自动滚动到顶部
- [ ] **UX-02**: Case 状态 Tag 移到 Case Name 右侧（缩短顶部高度）
- [ ] **UX-03**: Stage Detail 区域支持展开/收缩交互，收缩后仅保留关键标题信息

### Data & Validation

- [ ] **DATA-01**: 文件上传后记录上传人 (UploadedBy) 和上传时间 (UploadDate)，前端展示在文件名右侧
- [ ] **DATA-02**: Short Answer Grid 必填校验改为"填入一个单元格即算填写"，允许 Submit
- [ ] **DATA-03**: 创建 Case 选择 Workflow 时，下拉框中的 Active/Inactive 状态与 Workflow 管理页面一致

### Permissions

- [ ] **PERM-01**: User Group 配置 Case 编辑权限后，对应用户可正常编辑 Case（排查并修复权限链路问题）

## Future Requirements

Deferred to future milestones.

### IDM Integration

- **IDM-01**: 创建 Team 时查重限定当前租户（需 IDM 团队配合修复）

### Number Configuration (from v1.0)

- **NCFG-01**: User can set min/max range constraints for Number fields
- **NCFG-02**: User can configure step size for Number fields
- **NCFG-03**: User can toggle between integer-only and decimal input

## Out of Scope

| Feature | Reason |
|---------|--------|
| Team 创建跨租户查重 (#10) | IDM 外部系统 Bug，FlowFlex 无法独立修复 |
| 深度克隆 Component 实体（新建 Checklist/Questionnaire 副本） | 产品确认仅复制引用，不新建实体 |
| 回溯删除历史 StageSave 日志 | 只停止新写入，不清理历史数据 |
| 实时 IDM 同步 | 超出本次范围 |
| Grid 表格 min/max 校验 | 不需要范围约束 |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| LOG-01 | — | Pending |
| LOG-02 | — | Pending |
| LOG-03 | — | Pending |
| LOG-04 | — | Pending |
| LOG-05 | — | Pending |
| COMP-01 | — | Pending |
| COMP-02 | — | Pending |
| COMP-03 | — | Pending |
| UX-01 | — | Pending |
| UX-02 | — | Pending |
| UX-03 | — | Pending |
| DATA-01 | — | Pending |
| DATA-02 | — | Pending |
| DATA-03 | — | Pending |
| PERM-01 | — | Pending |

**Coverage:**
- v1.1 requirements: 15 total
- Mapped to phases: 0
- Unmapped: 15 ⚠️

---
*Requirements defined: 2026-06-02*
*Last updated: 2026-06-02 after initial definition*
