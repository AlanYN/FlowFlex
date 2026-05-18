# Test Design: Plugin Price List

## 测试策略

| 层级 | 框架 | 范围 |
|------|------|------|
| 后端单元测试 | xUnit + Moq | Service 层业务逻辑 |
| 后端集成测试 | xUnit + WebApplicationFactory | API 端到端 |
| 前端测试 | 手动验证 | UI 交互 + API 对接 |

---

## 测试用例

### TC-001: GET — 有数据，写权限

**前置条件**：
- 数据库中存在 caseCode="C00022" 的 Price List 记录
- 当前用户是该 Case 的 Stage Assignee

**步骤**：
1. GET `/api/ow/plugin-price-lists/v1?caseCode=C00022`

**预期结果**：
- HTTP 200
- `data.caseCode` = "C00022"
- `data.data.sections` 非空
- `data.permission` = "write"
- `data.status` = "draft"

---

### TC-002: GET — 有数据，只读权限

**前置条件**：
- 数据库中存在 caseCode="C00022" 的 Price List 记录
- 当前用户有 Case 查看权限但非 Assignee

**步骤**：
1. GET `/api/ow/plugin-price-lists/v1?caseCode=C00022`

**预期结果**：
- HTTP 200
- `data.permission` = "read"
- 其余字段正常返回

---

### TC-003: GET — 无数据，有权限

**前置条件**：
- 数据库中不存在 caseCode="C00099" 的 Price List 记录
- Onboarding 表中存在 case_code="C00099"
- 当前用户有该 Case 的写权限

**步骤**：
1. GET `/api/ow/plugin-price-lists/v1?caseCode=C00099`

**预期结果**：
- HTTP 200
- `data.caseCode` = "C00099"
- `data.data` = null
- `data.permission` = "write"

---

### TC-004: GET — CaseCode 不存在

**前置条件**：
- Onboarding 表中不存在 case_code="C99999"

**步骤**：
1. GET `/api/ow/plugin-price-lists/v1?caseCode=C99999`

**预期结果**：
- HTTP 404
- `message` 包含 "Case not found"

---

### TC-005: GET — 无权限

**前置条件**：
- Onboarding 表中存在 case_code="C00022"
- 当前用户对该 Case 无任何权限

**步骤**：
1. GET `/api/ow/plugin-price-lists/v1?caseCode=C00022`

**预期结果**：
- HTTP 403
- `message` 包含 "Access denied"

---

### TC-006: GET — 未认证

**前置条件**：无 JWT token

**步骤**：
1. GET `/api/ow/plugin-price-lists/v1?caseCode=C00022`（不带 Authorization）

**预期结果**：
- HTTP 401

---

### TC-007: POST Save — 新建

**前置条件**：
- 数据库中不存在 caseCode="C00099" 的 Price List
- 当前用户有写权限

**步骤**：
1. POST `/api/ow/plugin-price-lists/v1` body: `{caseCode: "C00099", data: {sections: [...]}}`

**预期结果**：
- HTTP 200
- `data.id` 非空（雪花 ID）
- `data.savedAt` 为当前时间附近
- 数据库中新增一条记录

---

### TC-008: POST Save — 更新

**前置条件**：
- 数据库中已存在 caseCode="C00022" 的 Price List（status=draft）
- 当前用户有写权限

**步骤**：
1. POST `/api/ow/plugin-price-lists/v1` body: `{caseCode: "C00022", data: {sections: [新数据]}}`

**预期结果**：
- HTTP 200
- 数据库中该记录的 `data` 字段已更新
- `modify_date` 已更新
- `modify_by` 为当前用户

---

### TC-009: POST Save — 已提交不可编辑

**前置条件**：
- 数据库中存在 caseCode="C00022" 的 Price List，status="submitted"
- 当前用户有写权限

**步骤**：
1. POST `/api/ow/plugin-price-lists/v1` body: `{caseCode: "C00022", data: {...}}`

**预期结果**：
- HTTP 400
- `message` 包含 "already submitted" 或类似提示

---

### TC-010: POST Save — 无写权限

**前置条件**：
- 当前用户只有 Case 查看权限（CanOperate=false）

**步骤**：
1. POST `/api/ow/plugin-price-lists/v1` body: `{caseCode: "C00022", data: {...}}`

**预期结果**：
- HTTP 403

---

### TC-011: POST Submit — 成功

**前置条件**：
- 数据库中存在 caseCode="C00022" 的 Price List，status="draft"
- 当前用户有写权限

**步骤**：
1. POST `/api/ow/plugin-price-lists/v1/submit` body: `{caseCode: "C00022"}`

**预期结果**：
- HTTP 200
- `data.status` = "submitted"
- 数据库中 status 已更新为 "submitted"

---

### TC-012: POST Submit — 重复提交

**前置条件**：
- 数据库中存在 caseCode="C00022" 的 Price List，status="submitted"

**步骤**：
1. POST `/api/ow/plugin-price-lists/v1/submit` body: `{caseCode: "C00022"}`

**预期结果**：
- HTTP 400
- `message` 包含 "already submitted"

---

### TC-013: POST Submit — 无数据可提交

**前置条件**：
- 数据库中不存在 caseCode="C00099" 的 Price List

**步骤**：
1. POST `/api/ow/plugin-price-lists/v1/submit` body: `{caseCode: "C00099"}`

**预期结果**：
- HTTP 404
- `message` 包含 "No price list found"

---

### TC-014: 租户隔离

**前置条件**：
- Tenant A 有 caseCode="C00022" 的 Price List
- 当前用户属于 Tenant B

**步骤**：
1. GET `/api/ow/plugin-price-lists/v1?caseCode=C00022`（X-Tenant-Id: tenant-b）

**预期结果**：
- 不返回 Tenant A 的数据（404 或无数据）

---

### TC-015: 前端 — 页面加载（有数据）

**前置条件**：
- 后端有 C00022 的 Price List 数据
- 用户有写权限

**步骤**：
1. 浏览器访问 `/plugins/price-list/index.html?caseCode=C00022`

**预期结果**：
- 页面显示 loading → 数据填充完成
- 所有字段可编辑
- Save/Submit 按钮可见

---

### TC-016: 前端 — 页面加载（只读）

**前置条件**：
- 后端有 C00022 的 Price List 数据
- 用户只有读权限

**步骤**：
1. 浏览器访问 `/plugins/price-list/index.html?caseCode=C00022`

**预期结果**：
- 数据正常显示
- 所有 input/select disabled
- Save/Submit/Add/Delete 按钮隐藏

---

### TC-017: 前端 — 保存成功

**步骤**：
1. 编辑 Price List 内容
2. 点击 Save

**预期结果**：
- 显示 loading
- 保存成功后显示 toast 提示
- 数据持久化（刷新页面后仍在）

---

### TC-018: 前端 — 提交成功

**步骤**：
1. 点击 Submit

**预期结果**：
- 确认弹窗（可选）
- 提交成功后页面切换为只读模式
- 状态显示为 "submitted"

---

## 问题清单

| ID | 问题 | 严重程度 | 状态 |
|----|------|---------|------|
| — | 暂无已知问题 | — | — |

> 问题清单将在测试执行过程中填充。

---

## 覆盖率报告

| 模块 | 测试用例数 | 覆盖的 AC 数 | 覆盖率 |
|------|-----------|-------------|--------|
| GET API | 6 (TC-001~006) | AC-001-1~4, AC-004-1, AC-005-1~2 | 100% |
| POST Save | 4 (TC-007~010) | AC-002-1~4, AC-003-4 | 100% |
| POST Submit | 3 (TC-011~013) | AC-003-1~3 | 100% |
| 租户隔离 | 1 (TC-014) | AC-006-4 | 100% |
| 前端交互 | 4 (TC-015~018) | AC-004-2~3, TR-004-1~7 | 100% |
| **总计** | **18** | **所有 AC** | **100%** |
