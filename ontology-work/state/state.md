# FlowFlex Ontology Work State

## Confirmed Context

- **goal**: 构建 FlowFlex 项目的全景业务本体，作为 AI 辅助编程时的完整上下文，确保 AI 在编程时能准确、不遗漏地理解项目全貌并朝目标前进
- **use_case**: AI 辅助编程、产品设计、产品使用支持
- **current_stage**: 场景 4 - 数据导出（已完成）
- **next_step**: 导入包已生成，可导入 OntologyStudio 使用

## Top Ontology Status

- **status**: confirmed
- **review_entry**: `ontology-work/docs/top-ontology-review.html`
- **source**: 预制模板 `ai-dev-top-ontology-template.json`

## Materials

- 后端代码: `packages/flowFlex-backend`
- 前端代码: `packages/flowFlex-common/src`
- 数据库: MCP `wfe-flowflex-postgres-dev`
- Neo4j: MCP `neo4j-FlowFlex`（直接清空后用）
- 已有本体总结: `flowflex-ontology-neo4j-export-L1.yaml`

## Storage

- **mode**: neo4j (via MCP neo4j-FlowFlex)

## Round Summary

### round-001

- **round_id**: round-001
- **round_goal**: 围绕 FlowFlex 全部核心业务对象，完成最小状态骨架、关键流程、关键规则、关键 SourceArtifact 与关键关联对象的闭环挖掘
- **round_scope**: 全部 21 个核心业务对象
- **round_status**: in_progress
- **data_source_principle**: 最终逻辑和结果以代码检索为依据，YAML 仅作指引

### Current Object Queue

| #   | Object                  | Type           | Status    |
| --- | ----------------------- | -------------- | --------- |
| 1   | Workflow                | 控制/边界型    | completed |
| 2   | Stage                   | 控制/边界型    | completed |
| 3   | Onboarding              | 生命周期主导型 | completed |
| 4   | Checklist               | 控制/边界型    | completed |
| 5   | ChecklistTask           | 执行承载型     | completed |
| 6   | ChecklistTaskCompletion | 执行承载型     | completed |
| 7   | Questionnaire           | 生命周期主导型 | completed |
| 8   | QuestionnaireAnswer     | 执行承载型     | completed |
| 9   | ActionDefinition        | 决策/编排型    | completed |
| 10  | ActionTriggerMapping    | 决策/编排型    | completed |
| 11  | StageCondition          | 决策/编排型    | completed |
| 12  | DynamicData             | 控制/边界型    | completed |
| 13  | StaticFieldValue        | 执行承载型     | completed |
| 14  | Integration             | 生命周期主导型 | completed |
| 15  | EntityMapping           | 控制/边界型    | completed |
| 16  | QuickLink               | 控制/边界型    | completed |
| 17  | Message                 | 生命周期主导型 | completed |
| 18  | InternalNote            | 执行承载型     | completed |
| 19  | OnboardingFile          | 生命周期主导型 | completed |
| 20  | User                    | 生命周期主导型 | completed |
| 21  | UserInvitation          | 生命周期主导型 | completed |

### completed_objects

All 21 objects completed in round-001 object phase.

### candidate_objects

(none discovered yet - will emerge during configuration and flow review rounds)

### required_related_objects

(none - all key objects already in queue)
