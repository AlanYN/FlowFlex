# 原始需求存档

## 2026-07-01 — OW-660: Allow Multiple Conditions on Stage Condition Setup

### 背景

WFE 的 Stage Condition 功能当前限制为一个 Stage 只能配一个 Condition。当用户需要根据不同条件走不同分支时，只能在单个 Condition 内用 AND/OR 逻辑组合所有规则，无法实现"满足条件 A 走分支 1，满足条件 B 走分支 2"的多路由场景。

### 核心需求

- 允许一个 Stage 配置多个 Condition（上限 10 个）
- 每个 Condition 仍包含 Name + Description + Rules + Actions
- 多个 Condition 按优先级（order）评估，first-match-wins
- 全部不满足时走 Stage 级统一 Fallback（Continue to next / Jump to chosen stage）
- 前端 Drawer 改为 Condition 列表管理器（折叠/展开卡片 + 排序 + 新增/删除）

### 设计决策记录

| #   | 决策点                  | 结论                                                                       |
| --- | ----------------------- | -------------------------------------------------------------------------- |
| 1   | 多 condition 同时满足时 | First-match-wins（按 order），代码预留随机策略切换扣子                     |
| 2   | Fallback 归属           | Stage 级别，不再属于单个 condition                                         |
| 3   | Fallback 存储           | `ff_stage.condition_fallback_stage_id`（NULL=Continue to next, 有值=Jump） |
| 4   | 数据迁移                | 旧 condition.fallback_stage_id 搬到 stage 表，旧列保留不删、代码 IsIgnore  |
| 5   | 唯一约束                | 删除，不加新约束，应用层管理                                               |
| 6   | 保存模式                | 前端统一 Save 按钮，背后拆成多个独立请求（复用现有 CRUD）                  |
| 7   | Order 分配              | 后端自动 max(order)+1，排序通过 PATCH reorder 接口                         |
| 8   | 评估逻辑                | 逐条懒加载评估，first-match-wins 后停止                                    |
| 9   | API 返回值              | GET /by-stage 改为返回数组                                                 |
| 10  | Condition 上限          | 10 个（`StageConditionConstants.MaxConditionsPerStage`）                   |
| 11  | 无 active condition 时  | 忽略 fallback，走顺序下一步                                                |
