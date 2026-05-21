# Tasks: Action Questionnaire Context — 需求分析阶段

## 任务列表

| Task ID | 任务 | 优先级 | 依赖 | 状态 |
|---------|------|--------|------|------|
| RA-T01 | 确认模块名称和 spec 路径 | P0 | 无 | ✅ completed |
| RA-T02 | 完成上下文扫描和变更性质判断 | P0 | RA-T01 | ✅ completed |
| RA-T03 | 编写用户故事和验收标准 | P0 | RA-T02 | ✅ completed |
| RA-T04 | 输出优先级矩阵（MoSCoW） | P0 | RA-T03 | ✅ completed |
| RA-T05 | 定义范围内 / 范围外边界 | P0 | RA-T03 | ✅ completed |
| RA-T06 | 列出需求阶段关键模糊点待用户确认 | P0 | RA-T03~RA-T05 | ✅ completed |
| RA-T07 | 用户确认 requirements-analysis specs 无歧义 | P0 | RA-T06 | ⏳ pending |
| RA-T08 | 生成 requirements-analysis docs 归档文件 | P0 | RA-T07 | ⏳ pending |

---

## 下一步

等待用户确认以下 requirements-analysis 关键假设：

1. v1 正式以 `questionId` 作为问卷取值主键
2. v1 默认以当前 Case 下所有已完成 Stage 的问卷答案作为 Action 上下文来源
3. 模板变量未命中时，按“替换为空字符串 + warning 日志”方向推进
4. 复杂题型（附件 / 复杂 grid）不纳入本期强承诺范围
