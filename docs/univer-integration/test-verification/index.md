# 文件清单 — univer-integration / test-verification

> 最后更新：2026-09-22

---

## 本阶段产出文件

| 文件 | 类型 | 说明 |
|------|------|------|
| `index.md` | 必需 | 本文件，文件清单（无 context，无下游） |

## 关联 specs 文件

| 文件 | 路径 | 说明 |
|------|------|------|
| requirements.md | `specs/univer-integration_20260922000000/test-verification/requirements.md` | 测试范围 + 质量目标 |
| design.md | `specs/univer-integration_20260922000000/test-verification/design.md` | 46 个测试用例 + 4 条问题清单 |
| tasks.md | `specs/univer-integration_20260922000000/test-verification/tasks.md` | 12 个测试任务 |

## 测试用例总览

| 分组 | 数量 | TC ID 范围 |
|------|------|-----------|
| 后端 Service 单元测试 | 12 | TC-BE-01 ~ TC-BE-12 |
| 后端 API 集成测试 | 11 | TC-API-01 ~ TC-API-11 |
| SignalR 协同测试 | 5 | TC-HUB-01 ~ TC-HUB-05 |
| 前端组件测试 | 15 | TC-FE-01 ~ TC-FE-15 |
| 样式回归测试 | 4 | TC-STYLE-01 ~ TC-STYLE-04 |
| **合计** | **47** | — |

## 问题清单

| ID | 严重度 | 描述 |
|----|--------|------|
| ISS-01 | 中 | Tailwind `important` 可能影响 ElPlus 浮层 z-index |
| ISS-02 | 中 | 生产环境 Web Worker MIME 类型需验证 |
| ISS-03 | 低 | 超大文档 JSON（>10MB）内存监控 |
| ISS-04 | 低 | SignalR query string token 被代理截断风险 |

## 阶段状态

- requirements-analysis：✅ completed（2026-09-22）
- interaction-design：✅ completed（2026-09-22）
- technical-design：✅ completed（2026-09-22）
- test-verification：✅ completed（2026-09-22）
